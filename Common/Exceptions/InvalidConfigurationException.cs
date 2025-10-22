namespace MES.Common.Exceptions;

public class InvalidConfigurationException : Exception
{
    public InvalidConfigurationException() : base("Data type is invalid.")
    { }
    public InvalidConfigurationException(string message) : base(message)
    { }
}
