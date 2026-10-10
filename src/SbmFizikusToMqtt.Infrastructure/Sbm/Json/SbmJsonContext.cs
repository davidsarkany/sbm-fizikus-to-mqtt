using System.Text.Json.Serialization;
using SbmFizikusToMqtt.Infrastructure.Sbm.Models;

namespace SbmFizikusToMqtt.Infrastructure.Sbm.Json;

[JsonSourceGenerationOptions(
    NumberHandling = JsonNumberHandling.AllowReadingFromString,
    Converters = [typeof(SbmDateTimeOffsetConverter)])]
[JsonSerializable(typeof(SbmRequest<SbmLoginPayload>))]
[JsonSerializable(typeof(SbmRequest<SbmTokenPayload>))]
[JsonSerializable(typeof(SbmRequest<SbmApartmentAccessRightsPayload>))]
[JsonSerializable(typeof(SbmRequest<SbmApartmentDataPayload>))]
[JsonSerializable(typeof(SbmRequest<SbmThermostatSettingsPayload>))]
[JsonSerializable(typeof(SbmTokenResponse))]
[JsonSerializable(typeof(IReadOnlyList<SbmBuildingAccessRight>))]
[JsonSerializable(typeof(IReadOnlyList<SbmApartmentAccessRight>))]
[JsonSerializable(typeof(SbmApartmentDataResponse))]
[JsonSerializable(typeof(SbmThermostatSettingsResponse))]
internal sealed partial class SbmJsonContext : JsonSerializerContext;