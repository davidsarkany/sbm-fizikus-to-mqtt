using System.Text.Json.Nodes;
using SbmFizikusToMqtt.Core.Domain;
using SbmFizikusToMqtt.Core.Options;
using SbmFizikusToMqtt.Core.Ports;
using SbmFizikusToMqtt.Core.Publishing;

namespace SbmFizikusToMqtt.Core.Discovery;

/// <summary>
///     Builds Home Assistant device-based discovery configs. Unique ids are kept identical to v2 so existing
///     entities, their ids and their history survive the upgrade.
/// </summary>
internal sealed class DiscoveryBuilder(MqttTopics topics, DiscoveryOptions options)
{
    private const string SensorPlatform = "sensor";
    private const string ClimatePlatform = "climate";
    private const string ClimateKey = "climate";

    // v2 hard-coded this prefix regardless of the configured topic; changing it would orphan existing entities.
    private const string UniqueIdPrefix = "sbm_fizikus";

    private const string Manufacturer = "SBM-Verting";
    private const string OriginName = "sbm-fizikus-to-mqtt";
    private const string SupportUrl = "https://github.com/davidsarkany/sbm-fizikus-to-mqtt";

    private static readonly SensorDefinition[] ApartmentSensors =
    [
        new("system_mode", "System mode", "system_mode", o => o.ApartmentSystemModeEnabled,
            EntityCategory: "diagnostic"),
        new("outdoor_temperature", "Outdoor temperature", "outdoor_temperature",
            o => o.ApartmentOutdoorTemperatureEnabled, "temperature", Unit: "°C"),
        new("outdoor_humidity", "Outdoor humidity", "outdoor_humidity",
            o => o.ApartmentOutdoorHumidityEnabled, "humidity", Unit: "%")
    ];

    private static readonly SensorDefinition[] ThermostatSensors =
    [
        new("temperature", "Temperature", "temperature", o => o.ThermostatTemperatureEnabled,
            "temperature", "measurement", "°C"),
        new("target_temperature", "Target temperature", "target_temperature",
            o => o.ThermostatTargetTemperatureEnabled, "temperature", "measurement", "°C",
            UniqueIdSuffix: "temperature_target"),
        new("humidity", "Humidity", "humidity", o => o.ThermostatHumidityEnabled,
            "humidity", "measurement", "%"),
        new("system_mode", "System mode", "system_mode", o => o.ThermostatSystemModeEnabled,
            EntityCategory: "diagnostic")
    ];

    private string ApartmentDeviceIdentifier => $"{topics.NodeId}_apartment";

    public IReadOnlyList<DiscoveryDevice> Build(Apartment apartment)
    {
        var devices = new List<DiscoveryDevice>(apartment.Thermostats.Count + 1) { BuildApartmentDevice() };
        devices.AddRange(apartment.Thermostats.Select(BuildThermostatDevice));
        return devices;
    }

    public MqttMessage ToDeviceMessage(DiscoveryDevice device)
    {
        // A device without enabled entities is removed instead of being published empty.
        if (device.Components.Count == 0)
            return CreateDiscoveryMessage(device.Topic, string.Empty);

        var components = new JsonObject();
        foreach (var component in device.Components)
        {
            var config = new JsonObject { ["platform"] = component.Platform };
            foreach (var (key, value) in component.Config)
                config[key] = value?.DeepClone();
            components[component.Key] = config;
        }

        // A component config with only the platform removes a previously published component.
        foreach (var disabled in device.DisabledComponents)
            components[disabled.Key] = new JsonObject { ["platform"] = disabled.Platform };

        var payload = new JsonObject
        {
            ["device"] = device.Device.DeepClone(),
            ["origin"] = BuildOrigin(),
            ["components"] = components
        };

        return CreateDiscoveryMessage(device.Topic, payload.ToJsonString());
    }

    private DiscoveryDevice BuildApartmentDevice()
    {
        var device = new JsonObject
        {
            ["identifiers"] = new JsonArray(ApartmentDeviceIdentifier),
            ["name"] = "SBM Fizikus",
            ["manufacturer"] = Manufacturer,
            ["model"] = "Fizikus"
        };

        var (components, disabled) = BuildSensors(ApartmentSensors, topics.ApartmentInfo,
            suffix => $"{UniqueIdPrefix}-apartment_info_{suffix}");
        return new DiscoveryDevice(topics.ApartmentDiscovery, device, components, disabled);
    }

    private DiscoveryDevice BuildThermostatDevice(Thermostat thermostat)
    {
        var device = new JsonObject
        {
            ["identifiers"] = new JsonArray($"{topics.NodeId}_thermostat_{thermostat.Id}"),
            ["name"] = thermostat.Name ?? $"Thermostat {thermostat.Id}",
            ["manufacturer"] = Manufacturer,
            ["model"] = "Fizikus thermostat",
            ["via_device"] = ApartmentDeviceIdentifier
        };

        var (components, disabled) = BuildSensors(ThermostatSensors, topics.Device(thermostat.Id),
            suffix => $"{UniqueIdPrefix}-{thermostat.Id}_{suffix}");

        if (options.ClimateEnabled)
            components.Insert(0, new DiscoveryComponent(ClimateKey, ClimatePlatform, BuildClimate(thermostat.Id)));
        else
            disabled.Insert(0, new DisabledDiscoveryComponent(ClimateKey, ClimatePlatform));

        return new DiscoveryDevice(topics.ThermostatDiscovery(thermostat.Id), device, components, disabled);
    }

    private (List<DiscoveryComponent> Components, List<DisabledDiscoveryComponent> Disabled) BuildSensors(
        IEnumerable<SensorDefinition> definitions, string stateTopic, Func<string, string> uniqueId)
    {
        var components = new List<DiscoveryComponent>();
        var disabled = new List<DisabledDiscoveryComponent>();

        foreach (var definition in definitions)
        {
            if (!definition.IsEnabled(options))
            {
                disabled.Add(new DisabledDiscoveryComponent(definition.Key, SensorPlatform));
                continue;
            }

            var config = new JsonObject
            {
                ["unique_id"] = uniqueId(definition.UniqueIdSuffix ?? definition.Key),
                ["name"] = definition.Name,
                ["state_topic"] = stateTopic,
                ["value_template"] = "{{ value_json." + definition.ValueField + " }}"
            };
            AddIfNotNull(config, "device_class", definition.DeviceClass);
            AddIfNotNull(config, "state_class", definition.StateClass);
            AddIfNotNull(config, "unit_of_measurement", definition.Unit);
            AddIfNotNull(config, "entity_category", definition.EntityCategory);
            AddAvailability(config);

            components.Add(new DiscoveryComponent(definition.Key, SensorPlatform, config));
        }

        return (components, disabled);
    }

    private JsonObject BuildClimate(int thermostatId)
    {
        var deviceTopic = topics.Device(thermostatId);
        var config = new JsonObject
        {
            ["unique_id"] = $"{UniqueIdPrefix}-{thermostatId}_climate",
            // null: the climate entity is the device's main feature and takes the device (room) name.
            ["name"] = null,
            ["action_topic"] = deviceTopic,
            ["action_template"] = "{{ value_json.system_mode }}",
            ["current_temperature_topic"] = deviceTopic,
            ["current_temperature_template"] = "{{ value_json.temperature }}",
            ["current_humidity_topic"] = deviceTopic,
            ["current_humidity_template"] = "{{ value_json.humidity }}",
            ["temperature_state_topic"] = deviceTopic,
            ["temperature_state_template"] = "{{ value_json.target_temperature }}",
            ["temperature_command_topic"] = topics.DeviceSet(thermostatId),
            ["temperature_command_template"] = "{{ {'id': " + thermostatId + ", 'value': value } | to_json }}",
            ["temperature_unit"] = "C",
            ["temp_step"] = TargetTemperatureLimits.Step,
            ["min_temp"] = TargetTemperatureLimits.Minimum,
            ["max_temp"] = TargetTemperatureLimits.Maximum,
            ["precision"] = 0.1,
            ["modes"] = new JsonArray("heat", "cool"),
            ["mode_state_topic"] = topics.ApartmentInfo,
            ["mode_state_template"] =
                "{{ value_json.system_mode | replace('cooling', 'cool') | replace('heating', 'heat') }}"
        };
        AddAvailability(config);
        return config;
    }

    /// <summary>Entities are available only while the bridge runs and SBM reports the system as connected.</summary>
    private void AddAvailability(JsonObject config)
    {
        config["availability"] = new JsonArray(
            new JsonObject
            {
                ["topic"] = topics.BridgeState,
                ["value_template"] = "{{ value_json.state }}"
            },
            new JsonObject
            {
                ["topic"] = topics.ApartmentInfo,
                ["value_template"] = "{{ 'online' if value_json.connected else 'offline' }}"
            });
        config["availability_mode"] = "all";
    }

    private JsonObject BuildOrigin()
    {
        return new JsonObject
        {
            ["name"] = OriginName,
            ["sw_version"] = options.SoftwareVersion,
            ["support_url"] = SupportUrl
        };
    }

    private static void AddIfNotNull(JsonObject config, string key, string? value)
    {
        if (value is not null)
            config[key] = value;
    }

    private static MqttMessage CreateDiscoveryMessage(string topic, string payload)
    {
        return new MqttMessage { Topic = topic, Payload = payload, Retain = true, Qos = MqttQos.AtLeastOnce };
    }

    private sealed record SensorDefinition(
        string Key,
        string Name,
        string ValueField,
        Func<DiscoveryOptions, bool> IsEnabled,
        string? DeviceClass = null,
        string? StateClass = null,
        string? Unit = null,
        string? EntityCategory = null,
        string? UniqueIdSuffix = null);
}