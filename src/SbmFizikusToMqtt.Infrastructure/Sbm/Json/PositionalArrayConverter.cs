using System.Text.Json;
using System.Text.Json.Serialization;

namespace SbmFizikusToMqtt.Infrastructure.Sbm.Json;

/// <summary>
///     Base for SBM records that are sent as positional JSON arrays (e.g. <c>[24, 702, "BP-1", true]</c>).
///     Responses are only read, never written.
/// </summary>
internal abstract class PositionalArrayConverter<T> : JsonConverter<T>
{
    protected abstract int Length { get; }

    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException($"Expected an array for {typeof(T).Name} but found {reader.TokenType}");

        using var document = JsonDocument.ParseValue(ref reader);
        var elements = document.RootElement.EnumerateArray().ToList();
        if (elements.Count != Length)
            throw new JsonException($"Expected {Length} elements for {typeof(T).Name} but found {elements.Count}");

        try
        {
            return Map(elements);
        }
        catch (Exception ex) when (ex is InvalidOperationException or FormatException)
        {
            throw new JsonException($"Unexpected element type in {typeof(T).Name}", ex);
        }
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        throw new NotSupportedException($"{typeof(T).Name} is only read from SBM responses");
    }

    protected abstract T Map(IReadOnlyList<JsonElement> elements);

    protected static string RequiredString(JsonElement element)
    {
        var value = element.GetString();
        return string.IsNullOrWhiteSpace(value) ? throw new FormatException("Empty string element") : value;
    }
}