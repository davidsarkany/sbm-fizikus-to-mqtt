using SbmFizikusToMqtt.Domain;
using SbmFizikusToMqtt.MqttConnector.Domain;

namespace SbmFizikusToMqtt.HomeAssistantAutoDiscovery.Interfaces;

internal interface IThermostatDiscoveryStrategy
{
    public MqttMessage CreatePayload(Thermostat thermostat);
}