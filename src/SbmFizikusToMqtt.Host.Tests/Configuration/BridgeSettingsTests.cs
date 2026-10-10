using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SbmFizikusToMqtt.Host.Configuration;

namespace SbmFizikusToMqtt.Host.Tests.Configuration;

/// <summary>
///     Binding and validation of the settings with the source generators that Native AOT needs.
///     WebApplicationFactory hides startup exceptions behind an ObjectDisposedException, so these run on the
///     service provider directly.
/// </summary>
public sealed class BridgeSettingsTests
{
    private static readonly Dictionary<string, string?> ValidSettings = new()
    {
        ["SbmConnector:Username"] = "user@example.com",
        ["SbmConnector:Password"] = "secret",
        ["SbmConnector:BaseUrl"] = "https://sbm.example.com",
        ["MqttConnector:MqttServer:Host"] = "mqtt.local"
    };

    [Fact]
    public void AddBridge_EverySettingConfigured_BindsEveryValue()
    {
        // Arrange
        using var provider = BuildProvider(new Dictionary<string, string?>
        {
            ["SbmConnector:PollingIntervalSeconds"] = "30",
            ["SbmConnector:OutdoorWeatherEnabled"] = "true",
            ["MqttConnector:MqttServer:Port"] = "8883",
            ["MqttConnector:MqttServer:Username"] = "mqtt-user",
            ["MqttConnector:MqttServer:Password"] = "mqtt-secret",
            ["MqttConnector:MqttServer:ClientId"] = "client",
            ["PublisherConfiguration:SbmTopic"] = "sbm",
            ["PublisherConfiguration:HomeAssistantTopic"] = "ha",
            ["PublisherConfiguration:ApartmentSystemModeDiscoveryEnabled"] = "true",
            ["PublisherConfiguration:ApartmentOutdoorTemperatureDiscoveryEnabled"] = "true",
            ["PublisherConfiguration:ApartmentOutdoorHumidityDiscoveryEnabled"] = "true",
            ["PublisherConfiguration:ThermostatTemperatureDiscoveryEnabled"] = "true",
            ["PublisherConfiguration:ThermostatTargetTemperatureDiscoveryEnabled"] = "true",
            ["PublisherConfiguration:ThermostatHumidityDiscoveryEnabled"] = "true",
            ["PublisherConfiguration:ThermostatSystemModeDiscoveryEnabled"] = "true",
            ["PublisherConfiguration:ClimateDiscoveryEnabled"] = "false"
        });

        // Act
        var sbm = provider.GetRequiredService<IOptions<SbmConnectorSettings>>().Value;
        var mqtt = provider.GetRequiredService<IOptions<MqttServerSettings>>().Value;
        var publisher = provider.GetRequiredService<IOptions<PublisherSettings>>().Value;

        // Assert
        Assert.Equal(("user@example.com", "secret", "https://sbm.example.com", 30, true),
            (sbm.Username, sbm.Password, sbm.BaseUrl, sbm.PollingIntervalSeconds, sbm.OutdoorWeatherEnabled));
        Assert.Equal(("mqtt.local", 8883, "mqtt-user", "mqtt-secret", "client"),
            (mqtt.Host, mqtt.Port, mqtt.Username, mqtt.Password, mqtt.ClientId));
        Assert.Equal(("sbm", "ha"), (publisher.SbmTopic, publisher.HomeAssistantTopic));
        Assert.Equal((true, true, true),
            (publisher.ApartmentSystemModeDiscoveryEnabled, publisher.ApartmentOutdoorTemperatureDiscoveryEnabled,
                publisher.ApartmentOutdoorHumidityDiscoveryEnabled));
        Assert.Equal((true, true, true, true, false),
            (publisher.ThermostatTemperatureDiscoveryEnabled, publisher.ThermostatTargetTemperatureDiscoveryEnabled,
                publisher.ThermostatHumidityDiscoveryEnabled, publisher.ThermostatSystemModeDiscoveryEnabled,
                publisher.ClimateDiscoveryEnabled));
    }

    [Fact]
    public void AddBridge_RequiredSettingsOnly_PassesValidationWithDefaults()
    {
        // Arrange
        using var provider = BuildProvider();

        // Act
        provider.GetRequiredService<IStartupValidator>().Validate();

        // Assert
        var sbm = provider.GetRequiredService<IOptions<SbmConnectorSettings>>().Value;
        var mqtt = provider.GetRequiredService<IOptions<MqttServerSettings>>().Value;
        var publisher = provider.GetRequiredService<IOptions<PublisherSettings>>().Value;
        Assert.Equal(120, sbm.PollingIntervalSeconds);
        Assert.Equal((1883, "sbm-fizikus-mqtt"), (mqtt.Port, mqtt.ClientId));
        Assert.Equal(("sbm_fizikus", "homeassistant", true),
            (publisher.SbmTopic, publisher.HomeAssistantTopic, publisher.ClimateDiscoveryEnabled));
    }

    [Theory]
    [InlineData("SbmConnector:Username", "", "Username")]
    [InlineData("SbmConnector:Password", "", "Password")]
    [InlineData("SbmConnector:BaseUrl", "not a url", "BaseUrl")]
    [InlineData("SbmConnector:PollingIntervalSeconds", "0", "PollingIntervalSeconds")]
    [InlineData("MqttConnector:MqttServer:Host", "", "Host")]
    [InlineData("MqttConnector:MqttServer:Port", "70000", "Port")]
    [InlineData("MqttConnector:MqttServer:ClientId", "", "ClientId")]
    [InlineData("PublisherConfiguration:SbmTopic", "", "SbmTopic")]
    [InlineData("PublisherConfiguration:HomeAssistantTopic", "", "HomeAssistantTopic")]
    public void AddBridge_InvalidSetting_FailsStartupValidation(string key, string value, string member)
    {
        // Arrange
        using var provider = BuildProvider(new Dictionary<string, string?> { [key] = value });

        // Act
        var exception = Record.Exception(() => provider.GetRequiredService<IStartupValidator>().Validate());

        // Assert
        var validation = Assert.IsType<OptionsValidationException>(exception);
        var failure = Assert.Single(validation.Failures);
        Assert.StartsWith($"{member}: ", failure, StringComparison.Ordinal);
    }

    private static ServiceProvider BuildProvider(Dictionary<string, string?>? overrides = null)
    {
        var settings = new Dictionary<string, string?>(ValidSettings);
        foreach (var (key, value) in overrides ?? [])
            settings[key] = value;

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        return new ServiceCollection().AddLogging().AddBridge(configuration).BuildServiceProvider();
    }
}