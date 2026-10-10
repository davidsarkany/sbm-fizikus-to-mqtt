using SbmFizikusToMqtt.Core.Domain;
using SbmFizikusToMqtt.Core.Options;
using SbmFizikusToMqtt.Core.Publishing;

namespace SbmFizikusToMqtt.Core.Tests.TestSupport;

/// <summary>Deterministic test data modelled on the anonymized real SBM responses.</summary>
internal static class TestApartments
{
    public static readonly DateTimeOffset LastUpdate = new(2026, 10, 10, 12, 45, 11, TimeSpan.Zero);

    public static readonly MqttTopics Topics = new(new TopicOptions
    {
        SbmTopic = "sbm_fizikus",
        HomeAssistantTopic = "homeassistant"
    });

    public static Apartment Create(
        bool connected = true,
        bool withOutdoorData = true,
        SystemMode systemMode = SystemMode.Heating,
        params Thermostat[] thermostats)
    {
        return new Apartment
        {
            SystemMode = systemMode,
            LastUpdate = LastUpdate,
            RelayConnectionActive = connected,
            ThermostatsConnectionActive = connected,
            OutdoorTemperature = withOutdoorData ? 19.9 : null,
            OutdoorHumidity = withOutdoorData ? 49.9 : null,
            Thermostats = thermostats.Length > 0 ? thermostats : [LivingRoom(), Unnamed()]
        };
    }

    public static Thermostat LivingRoom(bool active = true, string? name = "living-room")
    {
        return new Thermostat
        {
            Id = 1763,
            Name = name,
            Temperature = 24.8,
            Humidity = 60,
            TargetTemperature = 23,
            DewPoint = 15.9,
            Active = active,
            LastUpdate = LastUpdate.AddSeconds(29)
        };
    }

    public static Thermostat Unnamed()
    {
        return new Thermostat
        {
            Id = 1764,
            Name = null,
            Temperature = 24.6,
            Humidity = 55,
            TargetTemperature = 22.5,
            DewPoint = 15.9,
            Active = false,
            LastUpdate = LastUpdate.AddSeconds(29)
        };
    }
}