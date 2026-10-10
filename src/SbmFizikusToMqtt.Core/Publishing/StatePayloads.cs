using System.Text.Json.Serialization;

namespace SbmFizikusToMqtt.Core.Publishing;

internal sealed record ApartmentInfoPayload
{
    [JsonPropertyName("system_mode")] public required string SystemMode { get; init; }

    [JsonPropertyName("last_update")] public required DateTimeOffset LastUpdate { get; init; }

    /// <summary>SBM connectivity; feeds the entities' second availability topic.</summary>
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

    [JsonPropertyName("temperature")] public required double Temperature { get; init; }

    [JsonPropertyName("humidity")] public required double Humidity { get; init; }

    [JsonPropertyName("target_temperature")] public required double TargetTemperature { get; init; }

    /// <summary>Operating state: the apartment's mode while the zone is active, otherwise <c>idle</c>.</summary>
    [JsonPropertyName("system_mode")] public required string SystemMode { get; init; }

    [JsonPropertyName("last_update")] public required DateTimeOffset LastUpdate { get; init; }
}

[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(ApartmentInfoPayload))]
[JsonSerializable(typeof(DevicePayload))]
internal sealed partial class StatePayloadJsonContext : JsonSerializerContext;