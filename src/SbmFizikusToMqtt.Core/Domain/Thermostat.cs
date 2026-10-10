namespace SbmFizikusToMqtt.Core.Domain;

public sealed record Thermostat
{
    public required int Id { get; init; }
    public required string? Name { get; init; }
    public required double Temperature { get; init; }
    public required double Humidity { get; init; }
    public required double TargetTemperature { get; init; }
    public required double DewPoint { get; init; }

    /// <summary>True while the zone is calling for heating or cooling.</summary>
    public required bool Active { get; init; }

    public required DateTimeOffset LastUpdate { get; init; }
}