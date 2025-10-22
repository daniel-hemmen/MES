using MES.Common;
using MES.Common.Config.Enums;

namespace MES.PLC;

internal class PLCStation
{

    public event Action Completed;

    private PLCClient _client;
    private string _name;
    private List<string> _serialNumbers;
    private int _snArrayIndex = 0;
    private string _currentSerialNumber;
    private int _minCycleTime;
    private int _maxCycleTime;
    private Random _random = new Random();
    private Dictionary<string, string> _results;



    public PLCStation(StationOptionsConfiguration stationOptions, ClientSimulationOptions clientOptions, List<string> serialNumberList)
    {
        _client = new PLCClient(stationOptions.IpAddress, int.Parse(stationOptions.Port), stationOptions.Name);
        _name = clientOptions.StationName;
        _serialNumbers = serialNumberList;
        _snArrayIndex = int.Parse(clientOptions.SerialNumberArrayIndex);
        _minCycleTime = int.Parse(clientOptions.MinCycleTime);
        _maxCycleTime = int.Parse(clientOptions.MaxCycleTime);
        _results = stationOptions.Results;
    }

    public async Task StartAsync()
    {
        if (_snArrayIndex < _serialNumbers.Count)
        {
            string statusResponse = string.Empty;
            _currentSerialNumber = _serialNumbers[_snArrayIndex];
            if (!_client.IsConnected())
            {
                await _client.ConnectAsync();
            }
            await Task.Delay(_random.Next(100, 500)); //Simulate latency in the initial request

            if (_snArrayIndex != 0)
            {
                statusResponse = await ReadWriteAsync(FormatStatusCheckMessage());
            }

            if (EvaluateStatusResponse(statusResponse) || _snArrayIndex == 0)
            {
                await Task.Delay(_random.Next(_minCycleTime, _maxCycleTime)); //Simulate variable cycle time of the physical station
                string updateMessage = FormatUpdateMessage();
                await ReadWriteAsync(updateMessage);
            }
            else
            {
                await Task.Delay(_random.Next(_minCycleTime, _maxCycleTime));
            }

        }

        Completed?.Invoke(); // Notify the coordinator that this station has completed its cycle
    }

    public async Task<string> ReadWriteAsync(string dataToSend)
    {

        return await _client.SendReceiveAsync(dataToSend);

    }

    private string FormatStatusCheckMessage()
    {
        return $"{PLCOperationsEnum.GetStatus}|{_name}|{_currentSerialNumber}";
    }

    private bool EvaluateStatusResponse(string response)
    {
        if (string.IsNullOrEmpty(response))
        {
            return false;
        }

        string[] parts = response.Split('|');

        return parts[0].Trim() == PLCOperationsEnum.Good.ToString();

    }

    private string FormatUpdateMessage()
    {
        string resultString = "";

        foreach (var result in _results)
        {
            string dataType = result.Value.ToLower();

            switch (dataType)
            {
                case "int":
                    resultString += $"|{result.Key}|{GenerateIntResult()}";

                    break;
                case "real":
                    resultString += $"|{result.Key}|{GenerateFloatResult()}";

                    break;
                case "bool":
                    resultString += $"|{result.Key}|{GenerateBoolResult()}";

                    break;
                case "string":
                    resultString += $"|{result.Key}|{GenerateStringResult()}";

                    break;

            }
        }

        if (resultString.Length > 0)
        {
            return $"{PLCOperationsEnum.UpdateStatus}|{_name}|{_currentSerialNumber}|{GeneratePassFailResult()}:{resultString}";
        }

        return $"{PLCOperationsEnum.UpdateStatus}|{_name}|{_currentSerialNumber}|{GeneratePassFailResult()}";
    }

    private int GenerateIntResult()
    {
        return _random.Next(100, 150);
    }

    private float GenerateFloatResult()
    {
        return (float)(_random.Next(5000, 6000) / 100.0);
    }

    private bool GenerateBoolResult()
    {
        return _random.Next(0, 2) == 1;
    }

    private string GenerateStringResult()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, 8)
          .Select(s => s[_random.Next(s.Length)]).ToArray());
    }

    private PLCOperationsEnum GeneratePassFailResult()
    {
        if (_random.Next(1, 100) <= 10)
        {
            return PLCOperationsEnum.Bad;
        }

        return PLCOperationsEnum.Good;
    }

}
