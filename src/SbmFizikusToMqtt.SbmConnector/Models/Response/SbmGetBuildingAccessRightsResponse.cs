namespace SbmFizikusToMqtt.SbmConnector.Models.Response;

internal record SbmGetBuildingAccessRightsResponse
{
    public required int BuildingId { get; init; }

    public required string Country { get; init; }

    public required int PostalCode { get; init; }

    public required string City { get; init; }

    public required string Street { get; init; }

    public required double Temperature { get; init; }

    public required double Humidity { get; init; }

    public required string Name { get; init; }
}