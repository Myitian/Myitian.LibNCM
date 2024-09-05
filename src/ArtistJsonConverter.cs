using System.Text.Json;
using System.Text.Json.Serialization;

namespace Myitian.LibNCM;

public class ArtistJsonConverter : JsonConverter<Artist?>
{
    public override Artist? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return null;
            case JsonTokenType.StartArray:
                break;
            default:
                throw new JsonException();
        }
        Artist artist = new();
        reader.Read();
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
            case JsonTokenType.String:
                break;
            case JsonTokenType.EndArray:
                return artist;
            default:
                throw new JsonException();
        }
        artist.Name = reader.GetString();
        reader.Read();
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
            case JsonTokenType.String:
            case JsonTokenType.Number:
                break;
            case JsonTokenType.EndArray:
                return artist;
            default:
                throw new JsonException();
        }
        artist.ID = StringNumberJsonConverter.Read(ref reader);
        reader.Read();
        while (reader.TokenType != JsonTokenType.EndArray)
        {
            reader.Read();
        }
        return artist;
    }
    public override void Write(Utf8JsonWriter writer, Artist? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }
        writer.WriteStartArray();
        writer.WriteStringValue(value.Name);
        writer.WriteStringValue(value.ID.ToString());
        writer.WriteEndArray();
    }
}
