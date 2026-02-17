using SbmFizikusToMqtt.SbmConnector.Models.Response;

namespace SbmFizikusToMqtt.SbmConnector.Interfaces;

internal interface ISbmService
{
    public Task<SbmTokenResponse> GetToken(string username, string password,
        CancellationToken cancellationToken = default);

    public Task<SbmGetBuildingAccessRightsResponse[]> GetBuildingAccessRights(string token,
        CancellationToken cancellationToken = default);

    public Task<SbmApartmentListResponse[]> GetApartmentList(string buildingId, string token,
        CancellationToken cancellationToken = default);

    public Task<SbmApartmentInfoResponse> GetApartmentInfo(int apartmentId, string token,
        CancellationToken cancellationToken = default);

    public Task<SbmChangeTemperatureResponse> ChangeTemperature(int thermostatId, double temperature, string token,
        CancellationToken cancellationToken = default);
}