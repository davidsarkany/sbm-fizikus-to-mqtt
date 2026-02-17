using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace SbmFizikusToMqtt.SbmConnector.Models.Requests;

internal sealed record SbmApartmentInfoRequest
{
    [SetsRequiredMembers]
    public SbmApartmentInfoRequest(int apartmentId, string jwtToken)
    {
        Operation = "get_apartment_data";
        Payload = new PayloadData
        {
            ApartmentId = apartmentId,
            JwtToken = jwtToken
        };
    }

    [JsonPropertyName("operation")] public required string Operation { get; init; }

    [JsonPropertyName("payload")] public required PayloadData Payload { get; init; }

    internal sealed record PayloadData
    {
        [JsonPropertyName("apartment_id")] public required int ApartmentId { get; init; }

        [JsonPropertyName("jwt_token")] public required string JwtToken { get; init; }
    }
}