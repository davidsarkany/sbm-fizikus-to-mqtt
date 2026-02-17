using System.Text.Json.Serialization;
using SbmFizikusToMqtt.SbmConnector.Converters;

namespace SbmFizikusToMqtt.SbmConnector.Models.Response;

[JsonConverter(typeof(SbmApartmentDataRecordConverter))]
internal sealed record SbmApartmentListResponse
{
    public required int BuildingId { get; init; }
    public required int ApartmentId { get; init; }
    public required string Name { get; init; } = string.Empty;
    public required bool IsOnline { get; init; }
}