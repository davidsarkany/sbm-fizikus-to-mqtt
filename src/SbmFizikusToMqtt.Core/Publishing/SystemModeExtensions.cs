using SbmFizikusToMqtt.Core.Domain;

namespace SbmFizikusToMqtt.Core.Publishing;

internal static class SystemModeExtensions
{
    public static string ToWireValue(this SystemMode systemMode)
    {
        return systemMode switch
        {
            SystemMode.Heating => "heating",
            SystemMode.Cooling => "cooling",
            _ => "unknown"
        };
    }
}