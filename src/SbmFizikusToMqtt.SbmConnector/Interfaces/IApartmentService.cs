using SbmFizikusToMqtt.Domain;

namespace SbmFizikusToMqtt.SbmConnector.Interfaces;

public interface IApartmentService
{
    public Task<Apartment> GetApartmentInfo(CancellationToken cancellationToken = default);
    public Task ChangeTemperature(int thermostatId, double temperature, CancellationToken cancellationToken = default);
}