using System.Text.Json.Serialization;

namespace SbmFizikusToMqtt.Core.Publishing;

internal sealed record ApartmentInfoPayload
{
    [JsonPropertyName("system_mode")] public required string SystemMode { get; init; }

    /// <summary>Omitted when SBM does not report it, and in the stale payload before any successful refresh.</summary>
    [JsonPropertyName("last_update")] public DateTimeOffset? LastUpdate { get; init; }

    /// <summary>
    ///     SBM connectivity, false also while SBM cannot be refreshed; feeds the entities' second availability topic.
    /// </summary>
    [JsonPropertyName("connected")] public required bool Connected { get; init; }

    [JsonPropertyName("outdoor_temperature")] public double? OutdoorTemperature { get; init; }

    [JsonPropertyName("outdoor_humidity")] public double? OutdoorHumidity { get; init; }
}

internal sealed record DevicePayload
{
    [JsonPropertyName("id")] public required int Id { get; init; }

    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public required string? Name { get; init; }

    // The measurements are always present and null when SBM does not report them: the Home Assistant templates
    // then render "None", which MQTT entities show as unknown.

    [JsonPropertyName("temperature")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public required double? Temperature { get; init; }

    [JsonPropertyName("humidity")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public required double? Humidity { get; init; }

    [JsonPropertyName("target_temperature")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public required double? TargetTemperature { get; init; }

    /// <summary>Operating state: the apartment's mode while the zone is active, otherwise <c>idle</c>.</summary>
    [JsonPropertyName("system_mode")] public required string SystemMode { get; init; }

    [JsonPropertyName("last_update")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public required DateTimeOffset? LastUpdate { get; init; }
}

[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(ApartmentInfoPayload))]
[JsonSerializable(typeof(DevicePayload))]
internal sealed partial class StatePayloadJsonContext : JsonSerializerContext;