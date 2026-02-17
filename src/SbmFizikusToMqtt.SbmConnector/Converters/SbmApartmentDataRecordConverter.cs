using System.Text.Json;
using System.Text.Json.Serialization;
using SbmFizikusToMqtt.SbmConnector.Models.Response;

namespace SbmFizikusToMqtt.SbmConnector.Converters;

internal sealed class SbmApartmentDataRecordConverter : JsonConverter<SbmApartmentListResponse>
{
    public override SbmApartmentListResponse Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        ValidateTokenType(reader, JsonTokenType.StartArray, "Expected start of array token");

        var buildingId = ReadInt32(ref reader, "Expected number for buildingId");
        var apartmentId = ReadInt32(ref reader, "Expected number for apartmentId");
        var name = ReadString(ref reader, "Expected string for name", "Name cannot be null");
        var isOnline = ReadBoolean(ref reader, "Expected boolean for isOnline");

        reader.Read();
        ValidateTokenType(reader, JsonTokenType.EndArray, "Expected end of array token");

        return new SbmApartmentListResponse
        {
            BuildingId = buildingId,
            ApartmentId = apartmentId,
            Name = name,
            IsOnline = isOnline
        };
    }

    public override void Write(
        Utf8JsonWriter writer,
        SbmApartmentListResponse value,
        JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue(value.BuildingId);
        writer.WriteNumberValue(value.ApartmentId);
        writer.WriteStringValue(value.Name);
        writer.WriteBooleanValue(value.IsOnline);
        writer.WriteEndArray();
    }

    private static void ValidateTokenType(Utf8JsonReader reader, JsonTokenType expectedType, string errorMessage)
    {
        if (reader.TokenType != expectedType) throw new JsonException(errorMessage);
    }

    private static int ReadInt32(ref Utf8JsonReader reader, string errorMessage)
    {
        reader.Read();
        if (reader.TokenType != JsonTokenType.Number) throw new JsonException(errorMessage);
        return reader.GetInt32();
    }

    private static string ReadString(ref Utf8JsonReader reader, string typeErrorMessage, string nullErrorMessage)
    {
        reader.Read();
        if (reader.TokenType != JsonTokenType.String) throw new JsonException(typeErrorMessage);
        return reader.GetString() ?? throw new JsonException(nullErrorMessage);
    }

    private static bool ReadBoolean(ref Utf8JsonReader reader, string errorMessage)
    {
        reader.Read();
        if (reader.TokenType is not JsonTokenType.True and not JsonTokenType.False)
            throw new JsonException(errorMessage);
        return reader.GetBoolean();
    }
}