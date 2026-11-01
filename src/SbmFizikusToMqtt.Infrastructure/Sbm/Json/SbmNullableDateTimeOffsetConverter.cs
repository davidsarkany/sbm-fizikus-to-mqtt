using System.Text.Json;
using System.Text.Json.Serialization;

namespace SbmFizikusToMqtt.Infrastructure.Sbm.Json;

/// <summary>
///     Reads optional SBM timestamps. SBM's backend renders a missing value as JSON <c>null</c> or as the string
///     <c>"None"</c> (seen in <c>lastMeterSynchronisation</c>); both become <c>null</c> instead of failing the whole
///     response. Any other value must be a valid timestamp, see <see cref="SbmDateTimeOffsetConverter" />.
/// </summary>
internal sealed class SbmNullableDateTimeOffsetConverter : JsonConverter<DateTimeOffset?>
{
    private static readonly SbmDateTimeOffsetConverter Inner = new();

    public override bool HandleNull => true;

    public override DateTimeOffset? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType == JsonTokenType.String && IsMissing(reader.GetString()))
            return null;

        return Inner.Read(ref reader, typeof(DateTimeOffset), options);
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset? value, JsonSerializerOptions options)
    {
        if (value is null)
            writer.WriteNullValue();
        else
            writer.WriteStringValue(value.Value);
    }

    private static bool IsMissing(string? value)
    {
        return string.IsNullOrWhiteSpace(value) || value.Equals("None", StringComparison.Ordinal);
    }
}