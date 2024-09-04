using System.Text.Json;
using System.Text.Json.Serialization;

namespace Myitian.LibNCM;

public class StringNumberJsonConverter : JsonConverter<long?>
{
    public override long? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                string? s = reader.GetString();
                if (string.IsNullOrWhiteSpace(s))
                    goto case JsonTokenType.Null;
                else
                    return long.Parse(s);
            case JsonTokenType.Null:
                return null;
            case JsonTokenType.Number:
                return reader.GetInt64();
            default:
                throw new JsonException();
        }
    }

    public override void Write(Utf8JsonWriter writer, long? value, JsonSerializerOptions options)
    {
        if (value is null)
            writer.WriteNullValue();
        else
            writer.WriteNumberValue(value.Value);
    }
}
