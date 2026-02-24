using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace SbmFizikusToMqtt.SbmConnector.Models.Requests;

internal sealed record SbmGetBuildingAccessRightsRequest
{
    [SetsRequiredMembers]
    public SbmGetBuildingAccessRightsRequest(string jwtToken)
    {
        Operation = "get_building_access_rights";
        Payload = new PayloadData
        {
            JwtToken = jwtToken
        };
    }

    [JsonPropertyName("operation")] public required string Operation { get; init; }

    [JsonPropertyName("payload")] public required PayloadData Payload { get; init; }

    internal sealed record PayloadData
    {
        [JsonPropertyName("jwt_token")] public required string JwtToken { get; init; }
    }
}