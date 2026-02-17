using SbmFizikusToMqtt.SbmConnector.Models.Response;

namespace SbmFizikusToMqtt.SbmConnector.Interfaces;

internal interface ISbmService
{
    Task<SbmTokenResponse> GetToken(string username, string password,
        CancellationToken cancellationToken = default);

    Task<SbmGetBuildingAccessRightsResponse[]> GetBuildingAccessRights(string token,
        CancellationToken cancellationToken = default);

    Task<SbmApartmentListResponse[]> GetApartmentList(string buildingId, string token,
        CancellationToken cancellationToken = default);

    Task<SbmApartmentInfoResponse> GetApartmentInfo(int apartmentId, string token,
        CancellationToken cancellationToken = default);

    Task<SbmChangeTemperatureResponse> ChangeTemperature(int thermostatId, double temperature, string token,
        CancellationToken cancellationToken = default);
}