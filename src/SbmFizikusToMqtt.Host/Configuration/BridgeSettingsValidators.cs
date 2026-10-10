using Microsoft.Extensions.Options;

namespace SbmFizikusToMqtt.Host.Configuration;

// Validates the DataAnnotations attributes of the settings with generated code, which unlike
// ValidateDataAnnotations() needs no reflection and so works with Native AOT.

[OptionsValidator]
internal sealed partial class SbmConnectorSettingsValidator : IValidateOptions<SbmConnectorSettings>;

[OptionsValidator]
internal sealed partial class MqttServerSettingsValidator : IValidateOptions<MqttServerSettings>;

[OptionsValidator]
internal sealed partial class PublisherSettingsValidator : IValidateOptions<PublisherSettings>;