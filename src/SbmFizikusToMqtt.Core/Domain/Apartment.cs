namespace SbmFizikusToMqtt.Core.Domain;

public sealed record Apartment
{
    public required SystemMode SystemMode { get; init; }
    public required IReadOnlyList<Thermostat> Thermostats { get; init; }
    public required DateTimeOffset LastUpdate { get; init; }
    public required bool RelayConnectionActive { get; init; }
    public required bool ThermostatsConnectionActive { get; init; }
    public double? OutdoorTemperature { get; init; }
    public double? OutdoorHumidity { get; init; }

    /// <summary>True when SBM reports both the relay module and the thermostats as reachable.</summary>
    public bool IsConnected => RelayConnectionActive && ThermostatsConnectionActive;
}