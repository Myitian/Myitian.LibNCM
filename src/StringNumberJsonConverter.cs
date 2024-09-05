using System.Text.Json;
using System.Text.Json.Serialization;

namespace Myitian.LibNCM;

public class StringNumberJsonConverter : JsonConverter<long?>
{
    public const long MaxSafeInteger = 9007199254740991;
    public const long MinSafeInteger = -9007199254740991;

    public static long? Read(ref Utf8JsonReader reader)
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
    public override long? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return Read(ref reader);
    }
    public override void Write(Utf8JsonWriter writer, long? value, JsonSerializerOptions options)
    {
        if (value is >= MinSafeInteger and <= MaxSafeInteger)
            writer.WriteNumberValue(value.Value);
        else
            writer.WriteStringValue(value.ToString());
    }
}
