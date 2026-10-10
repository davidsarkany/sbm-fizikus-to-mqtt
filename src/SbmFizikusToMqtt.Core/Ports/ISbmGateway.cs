using SbmFizikusToMqtt.Core.Domain;

namespace SbmFizikusToMqtt.Core.Ports;

/// <summary>Access to the SBM Fizikus cloud.</summary>
public interface ISbmGateway
{
    Task<Apartment> GetApartmentAsync(CancellationToken cancellationToken);

    Task SetTargetTemperatureAsync(int thermostatId, double temperature, CancellationToken cancellationToken);
}