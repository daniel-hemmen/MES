using MES.Common.Extensions;
using System.Net;

namespace MES.Common.Validators;

public class StationOptionsValidator : OptionsValidator
{
    private enum SupportedDataTypes
    {
        String = 0,
        Integer,
        Real,
        Bool
    }

    public static void Validate(params IEnumerable<StationOptionsConfiguration> stationOptionsConfiguration)
    {
        var validationMessages = new List<string>();

        if (stationOptionsConfiguration.Select(config => config.Name).HasDuplicates(out var duplicateNames))
        {
            validationMessages.Add(FormatMessage("Duplicate station names found", duplicateNames));
        }

        if (stationOptionsConfiguration.Select(config => (config.IpAddress, config.Port)).HasDuplicates(out var duplicateIpPortCombinations))
        {
            validationMessages.Add(FormatMessage("Duplicate IP address and Port combinations found", duplicateIpPortCombinations.Select(value => $"{value.IpAddress}:{value.Port}")));
        }

        foreach (var stationConfig in stationOptionsConfiguration)
        {
            if (!TryValidate(stationConfig, out var stationConfigValidationMessages))
            {
                stationConfigValidationMessages.AddRange(stationConfigValidationMessages);
            }
        }

        if (validationMessages.Count == 0)
            return;

        ThrowInvalidConfigurationException("ServerStationConfig.json", validationMessages);
    }

    private static bool TryValidate(StationOptionsConfiguration stationOptions, out List<string> validationMessages)
    {
        validationMessages = [];

        if (!TryValidateProperty("Station name", stationOptions.Name, out var nameValidationMessage))
        {
            validationMessages.Add(nameValidationMessage);
        }

        if (!TryValidateIpAddress(stationOptions.IpAddress, out var ipValidationMessage))
        {
            validationMessages.Add(ipValidationMessage);
        }

        if (!TryValidatePort(stationOptions.Port, out var portValidationMessage))
        {
            validationMessages.Add(portValidationMessage);
        }

        if (!TryValidateResults(out var resultsValidationMessages, stationOptions.Results))
        {
            validationMessages.AddRange(resultsValidationMessages);
        }

        return true;
    }

    private static bool TryValidateIpAddress(string ipAddress, out string validationMessage)
    {
        validationMessage = string.Empty;

        if (!IPAddress.TryParse(ipAddress, out _))
        {
            validationMessage = $"IP address '{ipAddress}' is not valid.";

            return false;
        }

        return true;
    }

    private static bool TryValidatePort(string port, out string validationMessage)
    {
        validationMessage = string.Empty;

        if (!int.TryParse(port, out _))
        {
            validationMessage = $"Port '{port}' is not a valid integer.";

            return false;
        }

        return true;
    }

    private static bool TryValidateResults(out List<string> validationMessages, params IEnumerable<KeyValuePair<string, string>> results)
    {
        validationMessages = [];

        foreach (var result in results)
        {
            if (!TryValidateResult(result, out var resultValidationMessages))
            {
                validationMessages.AddRange(resultValidationMessages);
            }
        }

        return validationMessages.Count == 0;
    }

    private static bool TryValidateResult(KeyValuePair<string, string> result, out List<string> validationMessages)
    {
        validationMessages = [];

        if (!TryValidateProperty("Result name", result.Key, out var nameValidationMessage))
        {
            validationMessages.Add(nameValidationMessage);
        }

        if (!TryValidateDataType(result.Value, out var dataTypeValidationMessage))
        {
            validationMessages.Add(dataTypeValidationMessage);
        }

        return validationMessages.Count == 0;
    }

    private static bool TryValidateDataType(string dataType, out string validationMessage)
    {
        validationMessage = string.Empty;

        if (!Enum.TryParse<SupportedDataTypes>(dataType, ignoreCase: true, out _))
        {
            validationMessage = $"Data type '{dataType}' is not valid. Valid data types are: {string.Join(", ", Enum.GetNames<SupportedDataTypes>())}.";

            return false;
        }

        return true;
    }
}
