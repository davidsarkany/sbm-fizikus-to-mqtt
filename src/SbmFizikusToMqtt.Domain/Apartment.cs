namespace SbmFizikusToMqtt.Domain;

public sealed record Apartment
{
    public required string SystemMode { get; init; }
    public required IEnumerable<Thermostat> Thermostats { get; init; }
    public required DateTimeOffset LastUpdate { get; init; }
    public required bool RelayConnectionActive { get; init; }
    public required bool ThermostatsConnectionActive { get; init; }
}