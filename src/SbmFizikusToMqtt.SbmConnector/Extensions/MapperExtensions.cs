using SbmFizikusToMqtt.Domain;
using SbmFizikusToMqtt.SbmConnector.Models.Response;

namespace SbmFizikusToMqtt.SbmConnector.Extensions;

internal static class MapperExtensions
{
    internal static Apartment ToApartment(this SbmApartmentInfoResponse apartmentInfo)
    {
        return new Apartment
        {
            SystemMode = OperationModeToString(apartmentInfo.OperationMode),
            Thermostats = apartmentInfo.Thermostats.Select(x => x.ToThermostat()),
            LastUpdate = apartmentInfo.LastStateUpdate,
            RelayConnectionActive = apartmentInfo.CommunicationActiveRelayModule,
            ThermostatsConnectionActive = apartmentInfo.CommunicationActiveThermostats
        };
    }

    private static Thermostat ToThermostat(this SbmApartmentInfoResponse.Thermostat thermostat)
    {
        return new Thermostat
        {
            Id = thermostat.Id,
            Name = thermostat.Name,
            Temperature = thermostat.MeasuredTempDegC,
            Humidity = thermostat.MeasuredHumPerc,
            TargetTemperature = thermostat.TemperatureSetpointDegC,
            DewPoint = thermostat.DewPointDegC,
            Active = thermostat.Active,
            LastUpdate = thermostat.LastStateUpdate
        };
    }

    private static string OperationModeToString(int operationMode)
    {
        return (OperationMode)operationMode switch
        {
            OperationMode.Heating => "heating",
            OperationMode.Cooling => "cooling",
            _ => "unknown"
        };
    }
}

internal enum OperationMode
{
    Heating = 0,
    Cooling = 1
}