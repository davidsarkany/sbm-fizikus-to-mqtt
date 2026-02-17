using SbmFizikusToMqtt.Domain;

namespace SbmFizikusToMqtt.SbmConnector.Interfaces;

public interface IApartmentService
{
    Task<Apartment> GetApartmentInfo(CancellationToken cancellationToken = default);
    Task ChangeTemperature(int thermostatId, double temperature, CancellationToken cancellationToken = default);
}