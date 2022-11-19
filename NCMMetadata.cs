using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Myitian.LibNCM
{
    [DataContract]
    public class NCMMetadata
    {
        [DataMember(Name = "format", EmitDefaultValue = false, Order = 0)]
        public string Format { get; set; }

        [DataMember(Name = "musicId", EmitDefaultValue = false, Order = 1)]
        public int MusicId { get; set; }

        [DataMember(Name = "musicName", EmitDefaultValue = false, Order = 2)]
        public string MusicName { get; set; }

        [DataMember(Name = "artist", EmitDefaultValue = false, Order = 3)]
        public List<List<dynamic>> Artist { get; set; }

        [DataMember(Name = "album", EmitDefaultValue = false, Order = 4)]
        public string Album { get; set; }

        [DataMember(Name = "albumId", EmitDefaultValue = false, Order = 5)]
        public int AlbumId { get; set; }

        [DataMember(Name = "albumPicDocId", EmitDefaultValue = false, Order = 6)]
        public long AlbumPicDocId { get; set; }

        [DataMember(Name = "albumPic", EmitDefaultValue = false, Order = 7)]
        public string AlbumPic { get; set; }

        [DataMember(Name = "mvId", EmitDefaultValue = false, Order = 8)]
        public int MvId { get; set; }

        [DataMember(Name = "flag", EmitDefaultValue = false, Order = 9)]
        public int Flag { get; set; }

        [DataMember(Name = "bitrate", EmitDefaultValue = false, Order = 10)]
        public int Bitrate { get; set; }

        [DataMember(Name = "mp3DocId", EmitDefaultValue = false, Order = 11)]
        public string Mp3DocId { get; set; }

        [DataMember(Name = "duration", EmitDefaultValue = false, Order = 12)]
        public int Duration { get; set; }

        [DataMember(Name = "alias", EmitDefaultValue = false, Order = 13)]
        public List<string> Alias { get; set; }

        [DataMember(Name = "transNames", EmitDefaultValue = false, Order = 14)]
        public List<string> TransNames { get; set; }
    }
}
