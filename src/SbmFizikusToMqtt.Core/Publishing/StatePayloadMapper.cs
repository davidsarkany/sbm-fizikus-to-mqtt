using System.Text.Json;
using SbmFizikusToMqtt.Core.Domain;
using SbmFizikusToMqtt.Core.Ports;

namespace SbmFizikusToMqtt.Core.Publishing;

/// <summary>Maps an apartment snapshot to its retained MQTT state messages.</summary>
internal sealed class StatePayloadMapper(MqttTopics topics)
{
    private const string IdleSystemMode = "idle";

    public IReadOnlyList<MqttMessage> Map(Apartment apartment)
    {
        var systemMode = apartment.SystemMode.ToWireValue();
        var messages = new List<MqttMessage>(apartment.Thermostats.Count + 1)
        {
            CreateMessage(topics.ApartmentInfo, JsonSerializer.Serialize(new ApartmentInfoPayload
            {
                SystemMode = systemMode,
                LastUpdate = apartment.LastUpdate,
                Connected = apartment.IsConnected,
                OutdoorTemperature = apartment.OutdoorTemperature,
                OutdoorHumidity = apartment.OutdoorHumidity
            }, StatePayloadJsonContext.Default.ApartmentInfoPayload))
        };

        foreach (var thermostat in apartment.Thermostats)
        {
            messages.Add(CreateMessage(topics.Device(thermostat.Id), JsonSerializer.Serialize(new DevicePayload
            {
                Id = thermostat.Id,
                Name = thermostat.Name,
                Temperature = thermostat.Temperature,
                Humidity = thermostat.Humidity,
                TargetTemperature = thermostat.TargetTemperature,
                SystemMode = thermostat.Active ? systemMode : IdleSystemMode,
                LastUpdate = thermostat.LastUpdate
            }, StatePayloadJsonContext.Default.DevicePayload)));
        }

        return messages;
    }

    private static MqttMessage CreateMessage(string topic, string payload)
    {
        return new MqttMessage { Topic = topic, Payload = payload, Retain = true };
    }
}