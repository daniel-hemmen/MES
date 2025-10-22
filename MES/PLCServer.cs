using MES.Common;
using MES.Common.Config.Enums;
using MES.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;
using System.Reflection;

namespace MES;

internal class PLCServer : IDisposable
{
    private IPAddress _ipAddress;
    private int _port;
    private TcpListener _listener;
    private TcpClient _client;
    private String _name;
    private Dictionary<string, string> _results = new Dictionary<string, string>();
    private string _dbPath;
    private ILogger<PLCServer> _logger;
    private readonly IServiceProvider _serviceProvider;


    public PLCServer(StationOptionsConfiguration options, string dbPath, ILogger<PLCServer> logger, IServiceProvider serviceProvider)
    {
        _port = int.Parse(options.Port);
        _ipAddress = IPAddress.Parse(options.IpAddress);
        _name = options.Name;
        _results = options.Results;
        _dbPath = dbPath;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }


    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _listener = new TcpListener(_ipAddress, _port);
            _listener.Start(1);
            _logger.LogInformation($"{_name} server started and listening on {_ipAddress.ToString()}:{_port}");

            while (!cancellationToken.IsCancellationRequested)
            {
                //Dispose of previous client if it's still connected
                _client?.Close();

                _client = await _listener.AcceptTcpClientAsync(cancellationToken);
                _logger.LogInformation($"{_name} server accepted a connection.");
                await HandleClientAsync(_client, cancellationToken);

            }

        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation($"{_name} server: operation canceled.");
        }
        catch (Exception e)
        {

            if (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogError($"{_name} server encountered an issue starting the server. {e.Message}");
            }
        }
        finally
        {
            _client?.Close();
        }
    }

    public async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[1024];

        try
        {
            using NetworkStream stream = client.GetStream();
            while (!cancellationToken.IsCancellationRequested)
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken); //Listen for data from client
                if (bytesRead == 0)
                {
                    _logger.LogInformation($"{_name} server: client disconnected.");
                    break;
                }

                string receivedData = System.Text.Encoding.ASCII.GetString(buffer, 0, bytesRead); //Read data from client
                _logger.LogInformation($"{_name} server received: {receivedData}");
                string parsedResponse = await ParseRequest(receivedData); //Process the data and prepare a response

                byte[] response = System.Text.Encoding.ASCII.GetBytes(parsedResponse);
                try
                {
                    _logger.LogInformation($"{_name} server sent: {parsedResponse}");
                    await stream.WriteAsync(response, 0, response.Length, cancellationToken); //Send response to client
                }
                catch (Exception e)
                {

                    _logger.LogError($"{_name} server encountered an error replying to client. {e.Message}");
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation($"{_name} server: Read operation canceled.");
        }
        catch (Exception e)
        {

            _logger.LogError($"{_name} server encountered an issue reading from client. {e.Message}");
        }


    }

    private async Task<string> ParseRequest(string request)
    {
        /* Received data is formatted as follows:
         * 
         * GetStatus|Station Name|Current Serial Number
         * 
         * If the update request has process data (measuremnts, test results, etc) the format is:
         * UpdateStatus|Station Name|Current Serial Number|Pass/Fail:Process Result|Process Result|...
         * 
         * If the update does not have process data the format is:
         * UpdateStatus|Station Name|Current Serial Number|Pass/Fail
         * 
         */


        string[] parts = request.Split('|');

        if (parts.Length < 3)
        {
            return $"{_name} received invalid request format: {request}";
        }
        string operation = parts[0].Trim();
        string stationName = parts[1].Trim();
        string serialNumber = parts[2].Trim();

        if (operation == PLCOperationsEnum.GetStatus.ToString())
        {
            bool status = await GetStatus(serialNumber);
            if (status)
            {
                return $"{PLCOperationsEnum.Good}|{_name}|{serialNumber}";
            }
            return $"{PLCOperationsEnum.Bad}|{_name}|{serialNumber}";
        }

        if (operation == PLCOperationsEnum.UpdateStatus.ToString())
        {

            string status = parts[3].Trim();
            await UpdateStatus(request);
            return $"{PLCOperationsEnum.UpdateStatus}|{_name}|{status}";
        }

        return $"{_name} received unknown operation: {operation}";
    }

    private async Task<bool> GetStatus(string serialNumber)
    {
        var dbLogger = _serviceProvider.GetRequiredService<ILogger<PartDataRepository>>();
        using var repository = new PartDataRepository(_dbPath, dbLogger);
        PartData? part = await repository.GetPartDataBySerialNumberAsync(serialNumber);
        _logger.LogInformation($"{_name} server checked status for Serial Number: {serialNumber}, Status: {part?.Status}");
        return part != null && part.Status == PLCOperationsEnum.Good.ToString();
    }

    private async Task UpdateStatus(string request)
    {
        string[] parts = request.Split('|', ':');
        string operation = parts[0].Trim();
        string stationName = parts[1].Trim();
        string serialNumber = parts[2].Trim();
        string status = parts[3].Trim();
        var dbLogger = _serviceProvider.GetRequiredService<ILogger<PartDataRepository>>();
        using var repository = new PartDataRepository(_dbPath, dbLogger);

        PartData part = new PartData
        {
            SerialNumber = serialNumber,
            Status = status,
            LastStationComplete = stationName,
            Timestamp = DateTime.Now
        };

        if (parts.Length == 4) // If the request does not contain any process data, just update the status and return
        {
            await repository.UpdatePartDataAsync(part);
            return;
        }

        var properties = typeof(PartData).GetProperties(BindingFlags.Public | BindingFlags.Instance); // Get all public instance properties of the PartData class

        foreach (var result in _results) // Loop through each expected result defined in the configuration
        {
            string dataType = result.Value.ToLower(); // Get the data type of the result (int, real, bool, string)
            string key = result.Key; // Get the key of the result (e.g., VisionMeasurement, PasteDispenseWeight, etc.)
            int index = Array.IndexOf(parts, key); // Find the index of the key in the parts array

            if (index != -1 && index + 1 < parts.Length) // If the key is found and there is a value following it
            {
                foreach (var property in properties) // Loop through each property of the PartData class
                {
                    string propertyName = property.Name; // Get the name of the property
                    string value = parts[index + 1].Trim(); // Get the value associated with the key
                    if (propertyName == key) // If the property name matches the key
                    {
                        switch (dataType) // Set the property value based on the data type
                        {
                            case "int":
                                int intValue;
                                if (int.TryParse(value, out intValue))
                                {
                                    property.SetValue(part, intValue);
                                }
                                break;
                            case "real":
                                float floatValue;
                                if (float.TryParse(value, out floatValue))
                                {
                                    property.SetValue(part, floatValue);
                                }
                                break;
                            case "bool":
                                bool boolValue;
                                if (bool.TryParse(value, out boolValue))
                                {
                                    property.SetValue(part, boolValue);
                                }
                                break;
                            case "string":
                                property.SetValue(part, value);
                                break;
                        }
                    }
                }
            }
        }

        await repository.UpdatePartDataAsync(part);
    }

    public void Dispose()
    {
        _client?.Close();
        _listener?.Stop();
        _logger.LogInformation($"{_name} server has been stopped.");
    }
}
