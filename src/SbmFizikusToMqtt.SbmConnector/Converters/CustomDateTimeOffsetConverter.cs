using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SbmFizikusToMqtt.SbmConnector.Converters;

internal sealed class CustomDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var stringValue = reader.GetString();
        if (stringValue is null) throw new JsonException("DateTimeOffset string value is null");
        stringValue = stringValue.Replace(' ', 'T');
        return DateTimeOffset.Parse(stringValue, CultureInfo.InvariantCulture);
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString("yyyy-MM-ddTHH:mm:ss.ffffffzzz"));
    }
}