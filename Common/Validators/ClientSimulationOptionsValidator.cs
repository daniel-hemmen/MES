using MES.Common.Extensions;

namespace MES.Common.Validators
{
    public class ClientSimulationOptionsValidator : OptionsValidator
    {
        public static void Validate(List<StationOptionsConfiguration> stationOptionsConfigs, params IEnumerable<ClientSimulationOptions> clientSimulationOptionsConfigs)
        {
            var validationMessages = new List<string>();

            if (clientSimulationOptionsConfigs.Select(config => config.StationName).HasDuplicates(out var duplicateNames))
            {
                validationMessages.Add(FormatMessage("Duplicate station names found", duplicateNames));
            }

            if (!TryValidateSerialNumberArrayIndex(clientSimulationOptionsConfigs.Select(config => config.SerialNumberArrayIndex), out var indicesValidatonMessages))
            {
                validationMessages.Add(FormatMessage($"SerialNumberArrayIndex values must be consecutive and in order starting from 0", indicesValidatonMessages));
            }

            foreach (var clientSimulationOptionConfig in clientSimulationOptionsConfigs)
            {
                if (!TryValidate(clientSimulationOptionConfig, stationOptionsConfigs, out var clientSimulationConfigValidationMessages))
                {
                    validationMessages.AddRange(clientSimulationConfigValidationMessages);
                }
            }

            ThrowInvalidConfigurationException("ClientSimulationConfig.json", validationMessages);
        }

        private static bool TryValidate(ClientSimulationOptions clientSimulationOptions, List<StationOptionsConfiguration> stationOptions, out List<string> validationMessages)
        {
            validationMessages = [];

            if (!TryValidateStationName(clientSimulationOptions.StationName, [.. stationOptions.Select(so => so.Name)], out var nameValidationMessages))
            {
                validationMessages.AddRange(nameValidationMessages);
            }

            if (!TryValidateCycleTimes(clientSimulationOptions.StationName, clientSimulationOptions.MinCycleTime, clientSimulationOptions.MaxCycleTime, out var cycleTimeValidationMessages))
            {
                validationMessages.AddRange(cycleTimeValidationMessages);
            }

            return validationMessages.Count == 0;
        }

        private static bool TryValidateSerialNumberArrayIndex(IEnumerable<string> serialNumberArrayIndices, out List<string> validationMessages)
        {
            validationMessages = [];

            for (int expectedIndex = 0; expectedIndex < serialNumberArrayIndices.Count(); expectedIndex++)
            {
                if (!IsNonNegativeInteger(serialNumberArrayIndices.ElementAt(expectedIndex), out int resultIndex))
                {
                    validationMessages.Add($"Index {resultIndex} is not a valid non-negative integer");
                }

                if (expectedIndex != resultIndex)
                {
                    validationMessages.Add($"Found {resultIndex} at position {expectedIndex}.");
                }
            }

            return validationMessages.Count == 0;
        }


        private static bool TryValidateStationName(string stationName, List<string> stationOptions, out List<string> validationMessages)
        {
            validationMessages = [];

            if (!stationOptions.Contains(stationName))
            {
                validationMessages.Add($"No matching station found for client simulation station name '{stationName}'.");
            }

            if (!TryValidateProperty("Station name", stationName, out var nameValidationMessage))
            {
                validationMessages.Add(nameValidationMessage);
            }

            return validationMessages.Count == 0;
        }

        private static bool TryValidateCycleTimes(string stationName, string minCycleTime, string maxCycleTime, out List<string> validationMessages)
        {
            validationMessages = [];

            var minIsValid = IsNonNegativeInteger(minCycleTime, out var parsedMinCycleTime);
            var maxIsValid = IsNonNegativeInteger(maxCycleTime, out var parsedMaxCycleTime);

            if (!minIsValid)
            {
                validationMessages.Add($"{stationName} MinCycleTime '{minCycleTime}' is not a valid non-negative integer.");
            }

            if (!maxIsValid)
            {
                validationMessages.Add($"{stationName} MaxCycleTime '{maxCycleTime}' is not a valid non-negative integer.");
            }

            if (minIsValid && maxIsValid && parsedMinCycleTime > parsedMaxCycleTime)
            {
                validationMessages.Add($"{stationName} MinCycleTime ({parsedMinCycleTime}) cannot be greater than MaxCycleTime ({parsedMaxCycleTime}).");
            }

            return validationMessages.Count == 0;
        }

        private static bool IsNonNegativeInteger(string value, out int result) => int.TryParse(value, out result) && result >= 0;
    }
}
