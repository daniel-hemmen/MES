
using MES.Common;
using MES.Common.Validators;
using MES.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace MES;

internal class PLCServerFactory : IPLCServerFactory
{
    private List<StationOptionsConfiguration> _stationOptions;
    private string _connectionString;
    private readonly ILogger<PLCServer> _logger;
    private readonly IServiceProvider _serviceProvider;

    public PLCServerFactory(ILogger<PLCServer> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;

    }
    public List<PLCServer> CreateServers()
    {
        List<PLCServer> servers = new List<PLCServer>();
        LoadStationConfig();
        var dbLogger = _serviceProvider.GetRequiredService<ILogger<PartDataRepository>>();
        foreach (StationOptionsConfiguration option in _stationOptions)
        {
            servers.Add(new PLCServer(option, _connectionString, _logger, _serviceProvider));
        }
        return servers;
    }

    private void LoadStationConfig()
    {
        _stationOptions = new List<StationOptionsConfiguration>();

        JsonSerializerOptions jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        try
        {
            string optionsConfigPath = Path.Combine(AppContext.BaseDirectory, "Config", "StationConfig.json");
            _stationOptions = JsonSerializer.Deserialize<List<StationOptionsConfiguration>>(File.ReadAllText(optionsConfigPath), jsonOptions);
            StationOptionsValidator.Validate(_stationOptions);
            _connectionString = DbConnectionHelper.GetConnectionString();

        }
        catch (Exception e)
        {

            Console.WriteLine($"Error loading configuration: {e.Message}");

        }
    }
}
