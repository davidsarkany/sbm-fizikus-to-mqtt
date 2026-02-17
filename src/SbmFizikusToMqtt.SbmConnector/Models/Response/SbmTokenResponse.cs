using System.Text.Json.Serialization;

namespace SbmFizikusToMqtt.SbmConnector.Models.Response;

internal sealed record SbmTokenResponse
{
    [JsonPropertyName("token")] public required string AccessToken { get; init; }

    [JsonPropertyName("expiration")] public required DateTimeOffset Expiration { get; init; }

    [JsonPropertyName("refresh_token")] public required string RefreshToken { get; init; }

    [JsonPropertyName("refresh_token_expiration")]
    public required DateTimeOffset RefreshTokenExpiration { get; init; }

    [JsonPropertyName("rights")] public required List<string> Rights { get; init; }
}