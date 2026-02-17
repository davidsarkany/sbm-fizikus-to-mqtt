using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace SbmFizikusToMqtt.SbmConnector.Models.Requests;

internal sealed record SbmTokenRequest
{
    [SetsRequiredMembers]
    public SbmTokenRequest(string username, string passwordHash)
    {
        Operation = "get_jw_token";
        Payload = new PayloadData
        {
            UsernameOrEmail = username,
            PasswordHash = passwordHash
        };
    }

    [JsonPropertyName("operation")] public required string Operation { get; init; } = "get_jw_token";

    [JsonPropertyName("payload")] public required PayloadData Payload { get; init; }

    internal sealed class PayloadData
    {
        [JsonPropertyName("username_or_email")]
        public required string UsernameOrEmail { get; init; }

        [JsonPropertyName("pwhash")] public required string PasswordHash { get; init; }
    }
}