using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SbmFizikusToMqtt.Infrastructure.Sbm.Json;

/// <summary>
///     Reads SBM timestamps such as <c>2026-10-10 12:45:11.578277</c> or <c>2026-10-11 12:45:26.411281+00:00</c>.
///     Values without an offset are UTC: real responses showed <c>lastStateUpdate</c> matching the login time in UTC.
/// </summary>
internal sealed class SbmDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString() ?? throw new JsonException("Timestamp is null");

        if (!DateTimeOffset.TryParse(value.Replace(' ', 'T'), CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal, out var result))
            throw new JsonException($"Invalid timestamp '{value}'");

        return result;
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value);
    }
}