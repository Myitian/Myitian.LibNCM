using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;

namespace Myitian.LibNCM
{
    public class NCM
    {
        private static readonly byte[] _magicHeader = { 0x43, 0x54, 0x45, 0x4E, 0x46, 0x44, 0x41, 0x4D };
        private static readonly byte[] _rc4KeyKey = { 0x68, 0x7A, 0x48, 0x52, 0x41, 0x6D, 0x73, 0x6F, 0x35, 0x6B, 0x49, 0x6E, 0x62, 0x61, 0x78, 0x57 };
        private static readonly byte[] _metadataKey = { 0x23, 0x31, 0x34, 0x6C, 0x6A, 0x6B, 0x5F, 0x21, 0x5C, 0x5D, 0x26, 0x30, 0x55, 0x3C, 0x27, 0x28 };
        public const string Padding0 = "neteasecloudmusic";
        public const string Padding1 = "163 key(Don't modify):";
        public const string Padding2 = "music:";
        public byte[] MagicHeader
        {
            get
            {
                byte[] bytes = new byte[_magicHeader.Length];
                _magicHeader.CopyTo(bytes, 0);
                return bytes;
            }
        }
        public byte[] RC4KeyKey
        {
            get
            {
                byte[] bytes = new byte[_rc4KeyKey.Length];
                _rc4KeyKey.CopyTo(bytes, 0);
                return bytes;
            }
        }
        public byte[] MetadataKey
        {
            get
            {
                byte[] bytes = new byte[_metadataKey.Length];
                _metadataKey.CopyTo(bytes, 0);
                return bytes;
            }
        }

        private static readonly Aes aes = Aes.Create();

        private byte[] _gap0 = new byte[2];
        private byte[] _gap1 = new byte[9];
        private byte[] _rc4Key = new byte[112];
        private byte[] _sBox;
        private NCMMetadata _metadata = new NCMMetadata();
        private byte[] _imageCover = new byte[0];
        private byte[] _musicData = new byte[0];

        public byte[] Gap0
        {
            get => _gap0;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException();
                }
                if (value.Length != 2)
                {
                    throw new ArgumentOutOfRangeException("Gap1 length must be 2");
                }
                _gap0 = value;
            }
        }
        public byte[] Gap1
        {
            get => _gap1;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException();
                }
                if (value.Length != 9)
                {
                    throw new ArgumentOutOfRangeException("Gap1 length must be 9");
                }
                _gap1 = value;
            }
        }
        public byte[] RC4Key
        {
            get => _rc4Key;
            set
            {
                _rc4Key = value ?? throw new ArgumentNullException();
                _sBox = GenerateSBox(_rc4Key);
            }
        }
        public byte[] SBox
        {
            get
            {
                if (_sBox == null)
                {
                    _sBox = GenerateSBox(_rc4Key);
                }
                byte[] bytes = new byte[_sBox.Length];
                _sBox.CopyTo(bytes, 0);
                return bytes;
            }
        }
        public NCMMetadata Metadata { get => _metadata; set => _metadata = value ?? throw new ArgumentNullException(); }
        public byte[] ImageCover { get => _imageCover; set => _imageCover = value ?? throw new ArgumentNullException(); }
        public byte[] MusicData { get => _musicData; set => _musicData = value ?? throw new ArgumentNullException(); }

        /// <summary>
        /// Reads a chuck from <paramref name="stream"/>.
        /// </summary>
        private static byte[] ReadChunk(Stream stream)
        {
            byte[] chunk = new byte[4];
            stream.Read(chunk, 0, 4);
            // It should be a unsigned int, but I don't think the chuck size can reach int.MaxValue
            int len = Util.ToInt32(chunk, 0);
            chunk = new byte[len];
            stream.Read(chunk, 0, len);
            return chunk;
        }
        /// <summary>
        /// Writes a chuck to <paramref name="stream"/>.
        /// </summary>
        private static void WriteChunk(Stream stream, byte[] data)
        {
            stream.Write(Util.GetLEBytes(data.Length), 0, 4);
            stream.Write(data, 0, data.Length);
        }
        /// <summary>
        /// Performs AES decryption on the <paramref name="data"/> with the given <paramref name="key"/>.
        /// </summary>
        /// <param name="data">Data to decrypt</param>
        /// <param name="key">AES key</param>
        /// <param name="result">AES decryption result</param>
        /// <returns>Returns the actual count of bytes decrypted.</returns>
        private static int AesDecrypt(byte[] data, byte[] key, out byte[] result)
        {
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = key;
            using (MemoryStream stream = new MemoryStream(data))
            {
                using (CryptoStream cs = new CryptoStream(stream, aes.CreateDecryptor(), CryptoStreamMode.Read))
                {
                    result = new byte[data.Length];
                    int len = cs.Read(result, 0, result.Length);
                    return len;
                }
            }
        }
        /// <summary>
        /// Performs AES encryption on the <paramref name="data"/> with the given <paramref name="key"/>.
        /// </summary>
        /// <param name="data">Data to encrypt</param>
        /// <param name="key">AES key</param>
        /// <returns>Returns the AES encryption result</returns>
        private static byte[] AesEncrypt(byte[] data, byte[] key)
        {
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = key;
            using (MemoryStream stream = new MemoryStream())
            {
                using (CryptoStream cs = new CryptoStream(stream, aes.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(data, 0, data.Length);
                }
                return stream.ToArray();
            }
        }
        private static byte[] GenerateSBox(byte[] key)
        {
            byte[] result = new byte[256];
            int i;
            for (i = 0; i < 256; i++)
            {
                result[i] = (byte)i;
            }
            byte swap, c;
            byte last_byte = 0;
            byte key_offset = 0;
            for (i = 0; i < 256; i++)
            {
                swap = result[i];
                c = (byte)(swap + last_byte + key[key_offset++]);
                if (key_offset >= key.Length)
                {
                    key_offset = 0;
                }
                result[i] = result[c];
                result[c] = swap;
                last_byte = c;
            }
            return result;
        }


        public NCM()
        {

        }
        public NCM(byte[] coreKey, NCMMetadata metadata, byte[] imageCover, byte[] musicData)
        {
            _rc4Key = coreKey;
            _metadata = metadata;
            _imageCover = imageCover;
            _musicData = musicData;
        }

        #region Read NCM
        public static NCM ReadBytes(byte[] bytes)
        {
            using (MemoryStream ms = new MemoryStream(bytes))
            {
                return ReadStream(ms);
            }
        }
        public static NCM ReadFile(string path)
        {
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                return ReadStream(fs);
            }
        }
        public static NCM ReadFile(string path, FileShare share)
        {
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, share))
            {
                return ReadStream(fs);
            }
        }
        public static NCM ReadStream(Stream stream)
        {
            NCM ncm = new NCM();
            int i;

            // Section 0: Magic Header
            byte[] magicHeader = new byte[8];
            stream.Read(magicHeader, 0, 8);
            if (!Util.ByteArrayContentEquals(magicHeader, _magicHeader))
            {
                throw new InvalidDataException("Not a valid NCM file!");
            }

            // Section 1: Gap (Unknown Data)
            stream.Read(ncm._gap0, 0, 2);

            // Section 2: RC4 Key (Process: Raw Data -> XOR -> AES Decrypt -> Data)
            byte[] rc4KeyChunk = ReadChunk(stream);
            for (i = 0; i < rc4KeyChunk.Length; i++)
            {
                rc4KeyChunk[i] ^= 0x64;
            }
            int decryptedRC4KeyLength = AesDecrypt(rc4KeyChunk, _rc4KeyKey, out byte[] decryptedRC4Key);
            ncm._rc4Key = new byte[decryptedRC4KeyLength - 17]; // Skip "neteasecloudmusic"
            Array.Copy(decryptedRC4Key, 17, ncm._rc4Key, 0, ncm._rc4Key.Length);
            // KSA - Generate S-Box
            ncm._sBox = GenerateSBox(ncm._rc4Key);

            // Section 3: Metadata (Process: Raw Data -> XOR -> Base64 Decode -> AES Decrypt -> JSON Deserialize -> Data)
            byte[] metadataChunk = ReadChunk(stream);
            for (i = 0; i < metadataChunk.Length; i++)
            {
                metadataChunk[i] ^= 0x63;
            }
            // Skip "163 key(Don't modify):"
            byte[] encryptedMetadata = Convert.FromBase64String(Encoding.ASCII.GetString(metadataChunk, 22, metadataChunk.Length - 22));
            int decryptedMetadataLength = AesDecrypt(encryptedMetadata, _metadataKey, out byte[] decryptedMetadata);
            // Deserialize Metadata JSON
            DataContractJsonSerializer d = new DataContractJsonSerializer(typeof(NCMMetadata));
            using (MemoryStream ms = new MemoryStream(decryptedMetadata, 6, decryptedMetadataLength - 6)) // Skip "music:"
            {
                ncm._metadata = d.ReadObject(ms) as NCMMetadata;
            }

            // Section 4: Gap (Image Cover CRC (?) & Unknown Data)
            // According to others, there is a CRC of the cover image, but I can't match
            // the data here with the CRC32 of the cover image in the next data chuck.
            stream.Read(ncm._gap1, 0, 9);

            // Section 5: Image Cover 
            ncm._imageCover = ReadChunk(stream);

            // Section 6: Music Data (RC4 Decrypt)
            int n = 0x8000;
            using (MemoryStream ms = new MemoryStream())
            {
                while (n > 1)
                {
                    byte[] chunk = new byte[n];
                    n = stream.Read(chunk, 0, n);
                    for (i = 0; i < n; i++)
                    {
                        int j = (i + 1) & 0xff;
                        chunk[i] ^= ncm._sBox[(ncm._sBox[j] + ncm._sBox[(ncm._sBox[j] + j) & 0xff]) & 0xff];
                    }
                    ms.Write(chunk, 0, n);
                }
                ncm._musicData = ms.ToArray();
            }
            return ncm;
        }
        #endregion Read NCM

        #region Write NCM
        public static byte[] WriteBytes(NCM ncm) => ncm.WriteBytes();
        public static void WriteFile(string path, NCM ncm) => ncm.WriteFile(path);
        public static void WriteFile(string path, FileShare share, NCM ncm) => ncm.WriteFile(path, share);
        public static void WriteStream(Stream stream, NCM ncm) => ncm.WriteStream(stream);

        public byte[] WriteBytes()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                WriteStream(ms);
                return ms.ToArray();
            }
        }
        public void WriteFile(string path)
        {
            using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                WriteStream(fs);
            }
        }
        public void WriteFile(string path, FileShare share)
        {
            using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write, share))
            {
                WriteStream(fs);
            }
        }
        public void WriteStream(Stream stream)
        {
            int i;

            // Section 0: Magic Header
            stream.Write(_magicHeader, 0, 8);

            // Section 1: Gap (Unknown Data)
            stream.Write(_gap0, 0, 2);

            // Section 2: RC4 Key
            byte[] rc4KeyChuck = new byte[_rc4Key.Length + 17];
            Encoding.ASCII.GetBytes(Padding0).CopyTo(rc4KeyChuck, 0);
            _rc4Key.CopyTo(rc4KeyChuck, 17);
            byte[] encryptedRC4KeyChuck = AesEncrypt(rc4KeyChuck, _rc4KeyKey);
            for (i = 0; i < encryptedRC4KeyChuck.Length; i++)
            {
                encryptedRC4KeyChuck[i] ^= 0x64;
            }
            WriteChunk(stream, encryptedRC4KeyChuck);
            // KSA
            _sBox = GenerateSBox(_rc4Key);

            // Section 3: Metadata
            DataContractJsonSerializer d = new DataContractJsonSerializer(typeof(NCMMetadata));
            byte[] encryptedMetadata;
            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(Encoding.ASCII.GetBytes(Padding2), 0, 6);
                d.WriteObject(ms, _metadata);
                encryptedMetadata = AesEncrypt(ms.ToArray(), _metadataKey);
            }
            byte[] metadataChunk = Encoding.ASCII.GetBytes(Padding1 + Convert.ToBase64String(encryptedMetadata));
            for (i = 0; i < metadataChunk.Length; i++)
            {
                metadataChunk[i] ^= 0x63;
            }
            WriteChunk(stream, metadataChunk);

            // Section 4: Gap
            stream.Write(_gap1, 0, 9);

            // Section 5: Image Cover 
            WriteChunk(stream, _imageCover);

            // Section 6: Music Data 
            int n = 0x8000;
            using (MemoryStream ms = new MemoryStream(_musicData))
            {
                while (n > 1)
                {
                    byte[] chunk = new byte[n];
                    n = ms.Read(chunk, 0, n);
                    for (i = 0; i < n; i++)
                    {
                        int j = (i + 1) & 0xff;
                        chunk[i] ^= _sBox[(_sBox[j] + _sBox[(_sBox[j] + j) & 0xff]) & 0xff];
                    }
                    stream.Write(chunk, 0, n);
                }
            }
        }
        #endregion Write NCM

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Encrypted Netease Cloud Music Format");

            sb.Append(Environment.NewLine);
            sb.Append("MusicName:".PadRight(18));
            sb.Append(_metadata.MusicName);

            sb.Append(Environment.NewLine);
            sb.Append("Artists:".PadRight(18));
            sb.Append(string.Join("/", _metadata.Artist.Select(a => a[0])));

            sb.Append(Environment.NewLine);
            sb.Append("Album:".PadRight(18));
            sb.Append(_metadata.Album);

            sb.Append(Environment.NewLine);
            sb.Append("Cover:".PadRight(18));
            sb.Append(_metadata.AlbumPic);

            sb.Append(Environment.NewLine);
            sb.Append("Bitrate:".PadRight(18));
            sb.Append(_metadata.Bitrate);

            sb.Append(Environment.NewLine);
            sb.Append("Duration:".PadRight(18));
            sb.Append(_metadata.Duration);

            sb.Append(Environment.NewLine);
            sb.Append("Format:".PadRight(18));
            sb.Append(_metadata.Format);

            sb.Append(Environment.NewLine);
            sb.Append("RC4 Key:".PadRight(18));
            sb.Append(Util.ByteArrayToString(_rc4Key, 16));

            sb.Append(Environment.NewLine);
            sb.Append("Cover (Embedded):".PadRight(18));
            sb.Append(Util.ByteArrayToString(_imageCover, 16));

            sb.Append(Environment.NewLine);
            sb.Append("Music Data:".PadRight(18));
            sb.Append(Util.ByteArrayToString(_musicData, 16));

            return sb.ToString();
        }
    }
}
