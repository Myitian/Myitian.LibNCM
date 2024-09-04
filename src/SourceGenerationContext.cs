using System.Text.Json.Serialization;

namespace Myitian.LibNCM;
[JsonSourceGenerationOptions(
    WriteIndented = false,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    NumberHandling = JsonNumberHandling.AllowReadingFromString)]
[JsonSerializable(typeof(NCMMetadata))]
public partial class SourceGenerationContext : JsonSerializerContext
{
}
