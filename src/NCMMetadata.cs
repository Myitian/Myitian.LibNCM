using System.Text.Json.Serialization;

namespace Myitian.LibNCM;

public class NCMMetadata
{
    [JsonConverter(typeof(StringNumberJsonConverter))]
    [JsonPropertyName("musicId")]
    public long? MusicId { get; set; }

    [JsonPropertyName("musicName")]
    public string? MusicName { get; set; }

    [JsonPropertyName("artist")]
    public List<Artist>? Artist { get; set; }

    [JsonConverter(typeof(StringNumberJsonConverter))]
    [JsonPropertyName("albumId")]
    public long? AlbumId { get; set; }

    [JsonPropertyName("album")]
    public string? Album { get; set; }

    [JsonConverter(typeof(StringNumberJsonConverter))]
    [JsonPropertyName("albumPicDocId")]
    public long? AlbumPicDocId { get; set; }

    [JsonPropertyName("albumPic")]
    public string? AlbumPic { get; set; }

    [JsonConverter(typeof(StringNumberJsonConverter))]
    [JsonPropertyName("bitrate")]
    public long? Bitrate { get; set; }

    [JsonPropertyName("mp3DocId")]
    public string? Mp3DocId { get; set; }

    [JsonConverter(typeof(StringNumberJsonConverter))]
    [JsonPropertyName("duration")]
    public long? Duration { get; set; }

    [JsonPropertyName("alias")]
    public List<string>? Alias { get; set; }

    [JsonConverter(typeof(StringNumberJsonConverter))]
    [JsonPropertyName("mvId")]
    public long? MvId { get; set; }

    [JsonPropertyName("transNames")]
    public List<string>? TransNames { get; set; }

    [JsonPropertyName("format")]
    public string? Format { get; set; }

    [JsonConverter(typeof(StringNumberJsonConverter))]
    [JsonPropertyName("flag")]
    public long? Flag { get; set; }

    [JsonPropertyName("fee")]
    public long? Fee { get; set; }

    [JsonPropertyName("volumeDelta")]
    public double? VolumeDelta { get; set; }

    [JsonPropertyName("privilege")]
    public Privilege? Privilege { get; set; }
}

public class Privilege
{

    [JsonConverter(typeof(StringNumberJsonConverter))]
    [JsonPropertyName("flag")]
    public long? Flag { get; set; }
}

[JsonConverter(typeof(ArtistJsonConverter))]
public class Artist
{
    public string? Name { get; set; }
    public long? ID { get; set; }
}