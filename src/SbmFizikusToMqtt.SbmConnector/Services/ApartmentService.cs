using SbmFizikusToMqtt.Domain;
using SbmFizikusToMqtt.SbmConnector.Exceptions;
using SbmFizikusToMqtt.SbmConnector.Extensions;
using SbmFizikusToMqtt.SbmConnector.Interfaces;

namespace SbmFizikusToMqtt.SbmConnector.Services;

internal sealed class ApartmentService(ISbmService sbmService, ITokenService tokenService) : IApartmentService
{
    private readonly SemaphoreSlim _apartmentIdLock = new(1, 1);
    private int? _apartmentId;

    public async Task<Apartment> GetApartmentInfo(CancellationToken cancellationToken = default)
    {
        var jwtToken = await tokenService.GetToken(cancellationToken);

        // Cache apartment id with thread safety
        if (_apartmentId == null)
        {
            await _apartmentIdLock.WaitAsync(cancellationToken);
            try
            {
                // Double-check after acquiring lock
                if (_apartmentId == null)
                {
                    var buildingAccessRights =
                        await sbmService.GetBuildingAccessRights(jwtToken.AccessToken, cancellationToken);
                    if (buildingAccessRights.Length == 0)
                        throw new SbmException("No building access rights found for the user.");
                    var buildingId = buildingAccessRights[0].BuildingId;

                    var apartmentList = await sbmService.GetApartmentList(buildingId.ToString(), jwtToken.AccessToken,
                        cancellationToken);
                    if (apartmentList.Length == 0) throw new SbmException("No apartment found for the user.");
                    _apartmentId = apartmentList[0].ApartmentId;
                }
            }
            finally
            {
                _apartmentIdLock.Release();
            }
        }

        var apartmentInfo =
            await sbmService.GetApartmentInfo((int)_apartmentId, jwtToken.AccessToken, cancellationToken);
        return apartmentInfo.ToApartment();
    }

    public async Task ChangeTemperature(int thermostatId, double temperature,
        CancellationToken cancellationToken = default)
    {
        var jwtToken = await tokenService.GetToken(cancellationToken);
        await sbmService.ChangeTemperature(thermostatId, temperature, jwtToken.AccessToken, cancellationToken);
    }
}