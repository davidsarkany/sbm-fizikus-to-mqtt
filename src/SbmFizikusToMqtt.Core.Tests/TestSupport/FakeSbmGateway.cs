using SbmFizikusToMqtt.Core.Domain;
using SbmFizikusToMqtt.Core.Ports;

namespace SbmFizikusToMqtt.Core.Tests.TestSupport;

internal sealed class FakeSbmGateway : ISbmGateway
{
    public Apartment Apartment { get; set; } = TestApartments.Create();

    /// <summary>When set, every call fails with this exception.</summary>
    public Exception? Failure { get; set; }

    public int GetApartmentCalls { get; private set; }

    public List<(int ThermostatId, double Temperature)> TemperatureChanges { get; } = [];

    public Task<Apartment> GetApartmentAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        GetApartmentCalls++;
        return Failure is null ? Task.FromResult(Apartment) : Task.FromException<Apartment>(Failure);
    }

    public Task SetTargetTemperatureAsync(int thermostatId, double temperature, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (Failure is not null)
            return Task.FromException(Failure);

        TemperatureChanges.Add((thermostatId, temperature));
        return Task.CompletedTask;
    }
}