using System.Text.Json.Serialization;

namespace Myitian.LibNCM;
[JsonSourceGenerationOptions(
    WriteIndented = false,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(NCMMetadata))]
public partial class SourceGenerationContext : JsonSerializerContext
{
}
