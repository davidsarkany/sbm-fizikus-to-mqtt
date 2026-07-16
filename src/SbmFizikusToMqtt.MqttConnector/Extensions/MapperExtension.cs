using SbmFizikusToMqtt.Domain;
using SbmFizikusToMqtt.MqttConnector.Models;

namespace SbmFizikusToMqtt.MqttConnector.Extensions;

internal static class MapperExtension
{
    public static MqttThermostat ToMqttThermostat(this Thermostat thermostat, string systemMode)
    {
        return new MqttThermostat
        {
            Id = thermostat.Id,
            Name = thermostat.Name,
            Temperature = thermostat.Temperature,
            TargetTemperature = thermostat.TargetTemperature,
            Humidity = thermostat.Humidity,
            LastUpdate = thermostat.LastUpdate,
            SystemMode = thermostat.Active ? systemMode : "idle"
        };
    }

    public static MqttApartment ToMqttApartment(this Apartment apartment)
    {
        return new MqttApartment
        {
            SystemMode = apartment.SystemMode,
            LastUpdate = apartment.LastUpdate,
            OutdoorTemperature = apartment.OutdoorTemperature,
            OutdoorHumidity = apartment.OutdoorHumidity
        };
    }
}