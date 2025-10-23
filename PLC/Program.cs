using MES.Common;
using MES.Common.Validators;
using System.Configuration;
using System.Text.Json;

namespace MES.PLC;

internal class Program
{
    private static readonly string _clientConfigPath = Path.Combine(AppContext.BaseDirectory, "Config", "ClientSimulationConfig.json");
    private static readonly string _stationConfigPath = Path.Combine(AppContext.BaseDirectory, "Config", "StationConfig.json");
    private static readonly JsonSerializerOptions _serializerOptions = new() { PropertyNameCaseInsensitive = true };

    static async Task Main(string[] args)
    {
        var clientSimulationOptions = GetOptions<ClientSimulationOptions>(_clientConfigPath);
        var stationOptions = GetOptions<StationOptionsConfiguration>(_stationConfigPath);

        StationOptionsValidator.Validate(stationOptions);
        ClientSimulationOptionsValidator.Validate(stationOptions, clientSimulationOptions);

        // Create a serial number generator for the simulation
        var serialGen = new SerialNumberGenerator("AA", 0, 6, stationOptions.Count);

        /* Create a PLC station for each station defined in the configuration file and add it to a list. The station is
         * what simulates the physical station on the manufacturing line.
         */
        List<PLCStation> stations = [];

        foreach (var stationOption in stationOptions)
        {
            var clientSimulationOption = clientSimulationOptions.Single(c => c.StationName == stationOption.Name);
            stations.Add(new PLCStation(stationOption, clientSimulationOption, serialGen.serialNumbers));
        }

        // Create a coordinator to manage the PLC stations
        var coordinator = new Coordinator(stations);


        while (true)
        {
            serialGen.GenerateSerialNumbers(); // Generate a new set of serial numbers for each run
            await coordinator.Coordinate(); // Start the coordination of PLC stations
            coordinator.Reset(); // Reset the coordinator for the next run
        }
    }

    private static List<T> GetOptions<T>(string filePath)
    {
        var file = File.ReadAllText(filePath);
        var options = JsonSerializer.Deserialize<List<T>>(file, _serializerOptions) ?? throw new ConfigurationErrorsException($"Could not deserialize {typeof(T).Name}");

        return options;
    }
}
