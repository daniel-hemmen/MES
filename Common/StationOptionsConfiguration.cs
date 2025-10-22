

namespace MES.Common;

public class StationOptionsConfiguration
{
    public string Name { get; set; }
    public string IpAddress { get; set; }
    public string Port { get; set; }
    public Dictionary<string, string> Results { get; set; }
}
