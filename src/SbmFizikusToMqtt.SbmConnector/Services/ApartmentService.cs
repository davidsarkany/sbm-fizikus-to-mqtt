using Microsoft.Extensions.Options;
using SbmFizikusToMqtt.Domain;
using SbmFizikusToMqtt.SbmConnector.Configurations;
using SbmFizikusToMqtt.SbmConnector.Exceptions;
using SbmFizikusToMqtt.SbmConnector.Extensions;
using SbmFizikusToMqtt.SbmConnector.Interfaces;

namespace SbmFizikusToMqtt.SbmConnector.Services;

internal sealed class ApartmentService(
    ISbmService sbmService,
    ITokenService tokenService,
    IOptions<SbmConfiguration> options) : IApartmentService, IDisposable
{
    private readonly SemaphoreSlim _apartmentIdLock = new(1, 1);
    private int? _apartmentId;

    public async Task<Apartment> GetApartmentInfo(CancellationToken cancellationToken = default)
    {
        var jwtToken = await tokenService.GetToken(cancellationToken);
        double? outdoorTemperature = null;
        double? outdoorHumidity = null;

        // Fetch building access rights when outdoor weather is enabled OR apartment ID not yet cached
        if (options.Value.OutdoorWeatherEnabled || _apartmentId == null)
        {
            var buildingAccessRights =
                await sbmService.GetBuildingAccessRights(jwtToken.AccessToken, cancellationToken);
            if (buildingAccessRights.Length == 0)
                throw new SbmException("No building access rights found for the user.");

            if (options.Value.OutdoorWeatherEnabled)
            {
                outdoorTemperature = buildingAccessRights[0].Temperature;
                outdoorHumidity = buildingAccessRights[0].Humidity;
            }

            // Cache apartment id with thread safety
            if (_apartmentId == null)
            {
                await _apartmentIdLock.WaitAsync(cancellationToken);
                try
                {
                    // Double-check after acquiring lock
                    if (_apartmentId == null)
                    {
                        var buildingId = buildingAccessRights[0].BuildingId;

                        var apartmentList = await sbmService.GetApartmentList(buildingId.ToString(),
                            jwtToken.AccessToken,
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
        }

        var apartmentInfo =
            await sbmService.GetApartmentInfo((int)_apartmentId!, jwtToken.AccessToken, cancellationToken);
        return apartmentInfo.ToApartment(outdoorTemperature, outdoorHumidity);
    }

    public async Task ChangeTemperature(int thermostatId, double temperature,
        CancellationToken cancellationToken = default)
    {
        var jwtToken = await tokenService.GetToken(cancellationToken);
        await sbmService.ChangeTemperature(thermostatId, temperature, jwtToken.AccessToken, cancellationToken);
    }

    public void Dispose()
    {
        _apartmentIdLock.Dispose();
    }
}