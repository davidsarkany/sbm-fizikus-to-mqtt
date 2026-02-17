using System.Text.Json;
using System.Text.Json.Serialization;
using SbmFizikusToMqtt.SbmConnector.Models.Response;

namespace SbmFizikusToMqtt.SbmConnector.Converters;

internal sealed class SbmBuildingAccessRightsResponseConverter : JsonConverter<SbmGetBuildingAccessRightsResponse>
{
    private const int ExpectedArrayLength = 8;

    public override SbmGetBuildingAccessRightsResponse? Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType is not JsonTokenType.StartArray)
            throw new JsonException($"Expected {JsonTokenType.StartArray} but found {reader.TokenType}");

        var elements = new List<JsonElement>(ExpectedArrayLength);
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            elements.Add(JsonElement.ParseValue(ref reader));

        if (elements.Count != ExpectedArrayLength)
            throw new JsonException($"Expected array length of {ExpectedArrayLength}, but got {elements.Count}");

        return MapElementsToResponse(elements);
    }

    public override void Write(Utf8JsonWriter writer, SbmGetBuildingAccessRightsResponse value,
        JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue(value.BuildingId);
        writer.WriteStringValue(value.Country);
        writer.WriteNumberValue(value.PostalCode);
        writer.WriteStringValue(value.City);
        writer.WriteStringValue(value.Street);
        writer.WriteNumberValue(value.Temperature);
        writer.WriteNumberValue(value.Humidity);
        writer.WriteStringValue(value.Name);
        writer.WriteEndArray();
    }

    private static SbmGetBuildingAccessRightsResponse MapElementsToResponse(List<JsonElement> elements)
    {
        try
        {
            return new SbmGetBuildingAccessRightsResponse
            {
                BuildingId = elements[0].GetInt32(),
                Country = ExtractNonEmptyString(elements[1], nameof(SbmGetBuildingAccessRightsResponse.Country)),
                PostalCode = elements[2].GetInt32(),
                City = ExtractNonEmptyString(elements[3], nameof(SbmGetBuildingAccessRightsResponse.City)),
                Street = ExtractNonEmptyString(elements[4], nameof(SbmGetBuildingAccessRightsResponse.Street)),
                Temperature = elements[5].GetDouble(),
                Humidity = elements[6].GetDouble(),
                Name = ExtractNonEmptyString(elements[7], nameof(SbmGetBuildingAccessRightsResponse.Name))
            };
        }
        catch (Exception ex)
        {
            throw new JsonException("Failed to convert array values to expected types. Check the JSON response format.",
                ex);
        }
    }

    private static string ExtractNonEmptyString(JsonElement element, string fieldName)
    {
        var value = element.GetString();
        if (string.IsNullOrWhiteSpace(value))
            throw new JsonException($"Field '{fieldName}' cannot be empty or whitespace");
        return value;
    }
}