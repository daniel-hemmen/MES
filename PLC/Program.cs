using MES.Common;
using MES.Common.Validators;
using System.Text.Json;

namespace MES.PLC;

internal class Program
{
    static async Task Main(string[] args)
    {
        List<ClientSimulationOptions> clientSimulationOptions;
        List<StationOptionsConfiguration> stationOptions;

        JsonSerializerOptions jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        //Read station configuration and client configuration from the configuration files and verify the values are valid
        try
        {
            string clientConfigPath = Path.Combine(AppContext.BaseDirectory, "Config", "ClientSimulationConfig.json");
            string optionsConfigPath = Path.Combine(AppContext.BaseDirectory, "Config", "StationConfig.json");
            clientSimulationOptions = JsonSerializer.Deserialize<List<ClientSimulationOptions>>(File.ReadAllText(clientConfigPath), jsonOptions);
            stationOptions = JsonSerializer.Deserialize<List<StationOptionsConfiguration>>(File.ReadAllText(optionsConfigPath), jsonOptions);

            StationOptionsValidator.Validate(stationOptions);
            ClientSimulationOptionsValidator.ValidateClientSimulationConfig(clientSimulationOptions, stationOptions);
        }
        catch (Exception e)
        {

            Console.WriteLine($"Error loading configuration: {e.Message}");
            Console.ReadKey();
            return;
        }

        // Create a serial number generator for the simulation
        SerialNumberGenerator serialGen = new SerialNumberGenerator("AA", 0, 6, stationOptions.Count);

        /* Create a PLC station for each station defined in the configuration file and add it to a list. The station is
         * what simulates the physical station on the manufacturing line.
         */
        List<PLCStation> stations = new List<PLCStation>();

        foreach (var stationOption in stationOptions)
        {
            var clientSimulationOption = clientSimulationOptions
                .Find(c => c.StationName == stationOption.Name);

            stations.Add(new PLCStation(stationOption, clientSimulationOption, serialGen.serialNumbers));
        }

        // Create a coordinator to manage the PLC stations
        Coordinator coordinator = new Coordinator(stations);


        while (true)
        {
            serialGen.GenerateSerialNumbers(); // Generate a new set of serial numbers for each run
            await coordinator.Coordinate(); // Start the coordination of PLC stations
            coordinator.Reset(); // Reset the coordinator for the next run
        }

    }
}
