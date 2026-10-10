using SbmFizikusToMqtt.Core.Domain;
using SbmFizikusToMqtt.Infrastructure.Sbm.Models;

namespace SbmFizikusToMqtt.Infrastructure.Sbm;

internal static class SbmApartmentDataResponseExtensions
{
    public static Apartment ToApartment(this SbmApartmentDataResponse response, double? outdoorTemperature,
        double? outdoorHumidity)
    {
        return new Apartment
        {
            SystemMode = response.OperationMode switch
            {
                0 => SystemMode.Heating,
                1 => SystemMode.Cooling,
                _ => SystemMode.Unknown
            },
            LastUpdate = response.LastStateUpdate,
            RelayConnectionActive = response.CommunicationActiveRelayModule,
            ThermostatsConnectionActive = response.CommunicationActiveThermostats,
            OutdoorTemperature = outdoorTemperature,
            OutdoorHumidity = outdoorHumidity,
            Thermostats = response.Thermostats.Select(x => new Thermostat
            {
                Id = x.Id,
                Name = x.Name,
                Temperature = x.MeasuredTemperature,
                Humidity = x.MeasuredHumidity,
                TargetTemperature = x.TemperatureSetpoint,
                DewPoint = x.DewPoint,
                Active = x.Active,
                LastUpdate = x.LastStateUpdate
            }).ToList()
        };
    }
}