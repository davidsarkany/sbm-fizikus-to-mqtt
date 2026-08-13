using System.Text.Json;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Protocol;
using SbmFizikusToMqtt.Domain;
using SbmFizikusToMqtt.HomeAssistantAutoDiscovery.Interfaces;
using SbmFizikusToMqtt.MqttConnector.Configurations;
using SbmFizikusToMqtt.MqttConnector.Extensions;
using SbmFizikusToMqtt.MqttConnector.Interfaces;
using SbmFizikusToMqtt.MqttConnector.Models;

namespace SbmFizikusToMqtt.MqttConnector.Services;

internal sealed class MqttPublisher(
    IMqttClient mqttClient,
    IAutoDiscoveryGenerator autoDiscoveryGenerator,
    IOptionsMonitor<MqttConnectorPublisherConfiguration> mqttConnectorPublisherConfiguration)
    : IMqttPublisher
{
    public async Task Publish(Apartment apartment, CancellationToken cancellationToken = default)
    {
        var state = apartment is { RelayConnectionActive: true, ThermostatsConnectionActive: true };

        // Publish a state message first
        var stateTask = PublishState(state, cancellationToken);

        // Prepare all thermostat messages
        var thermostatTasks = apartment.Thermostats
            .Select(x => x.ToMqttThermostat(apartment.SystemMode))
            .Select(mqttThermostat => PublishThermostat(mqttThermostat, cancellationToken))
            .ToList();

        // Publish apartment info
        var apartmentInfoTask = PublishApartmentInfo(apartment.ToMqttApartment(), cancellationToken);

        // Wait for all publishing tasks to complete in parallel
        var publishTasks = new List<Task>(thermostatTasks.Count + 2) { stateTask, apartmentInfoTask };
        publishTasks.AddRange(thermostatTasks);
        await Task.WhenAll(publishTasks);
    }

    private async Task PublishThermostat(MqttThermostat thermostat, CancellationToken cancellationToken = default)
    {
        var message = new MqttApplicationMessageBuilder()
            .WithTopic($"{mqttConnectorPublisherConfiguration.CurrentValue.SbmTopic}/devices/{thermostat.Id}")
            .WithPayload(JsonSerializer.Serialize(thermostat))
            .Build();

        await mqttClient.PublishAsync(message, cancellationToken);
    }

    private async Task PublishApartmentInfo(MqttApartment apartmentInfo, CancellationToken cancellationToken = default)
    {
        var message = new MqttApplicationMessageBuilder()
            .WithTopic($"{mqttConnectorPublisherConfiguration.CurrentValue.SbmTopic}/apartment_info")
            .WithPayload(JsonSerializer.Serialize(apartmentInfo))
            .Build();

        await mqttClient.PublishAsync(message, cancellationToken);
    }

    public async Task PublishHomeAssistantAutoDiscovery(Apartment apartment,
        CancellationToken cancellationToken = default)
    {
        var discoveryMessages = autoDiscoveryGenerator.Generate(apartment);
        var publishTasks = discoveryMessages.Select(discoveryMessage =>
        {
            var mqttMessage = new MqttApplicationMessageBuilder()
                .WithTopic(discoveryMessage.Topic)
                .WithPayload(discoveryMessage.Payload)
                .WithRetainFlag()
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();
            return mqttClient.PublishAsync(mqttMessage, cancellationToken);
        });

        await Task.WhenAll(publishTasks);
    }

    private async Task PublishState(bool online, CancellationToken cancellationToken = default)
    {
        var statePayload = online ? BridgeStatePayloads.Online : BridgeStatePayloads.Offline;
        var message = new MqttApplicationMessageBuilder()
            .WithTopic($"{mqttConnectorPublisherConfiguration.CurrentValue.SbmTopic}/bridge/state")
            .WithPayload(statePayload)
            .Build();

        await mqttClient.PublishAsync(message, cancellationToken);
    }
}