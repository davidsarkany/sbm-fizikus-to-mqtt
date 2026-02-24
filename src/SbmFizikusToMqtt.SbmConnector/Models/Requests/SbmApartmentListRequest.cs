using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace SbmFizikusToMqtt.SbmConnector.Models.Requests;

internal sealed record SbmApartmentListRequest
{
    [SetsRequiredMembers]
    public SbmApartmentListRequest(string buildingId, string jwtToken)
    {
        Operation = "get_apartment_access_rights";
        Payload = new PayloadData { BuildingId = buildingId, JwtToken = jwtToken };
    }

    [JsonPropertyName("operation")] public required string Operation { get; init; }

    [JsonPropertyName("payload")] public required PayloadData Payload { get; init; }

    internal sealed record PayloadData
    {
        [JsonPropertyName("building_id")] public required string BuildingId { get; init; }

        [JsonPropertyName("jwt_token")] public required string JwtToken { get; init; }
    }
}