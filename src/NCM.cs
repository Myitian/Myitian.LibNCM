using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Myitian.LibNCM;

public class NCM
{
    public static ReadOnlySpan<byte> DefaultMagicHeader => "CTENFDAM"u8;
    public static ReadOnlySpan<byte> DefaultKeyOfRC4Key => "hzHRAmso5kInbaxW"u8;
    public static ReadOnlySpan<byte> DefaultMetadataKey => @"#14ljk_!\]&0U<'("u8;
    public static ReadOnlySpan<byte> DefaultPadding0 => "neteasecloudmusic"u8;
    public static ReadOnlySpan<byte> DefaultPadding1 => "163 key(Don't modify):"u8;
    public static ReadOnlySpan<byte> DefaultPadding2 => "music:"u8;

    private static readonly Aes aes = Aes.Create();

    private readonly byte[] _sBox =
#if NET5_0_OR_GREATER
        GC.AllocateUninitializedArray<byte>(256);
#else
        new byte[2];
#endif


    public Memory<byte> MagicHeader { get; set; } = new(DefaultMagicHeader.ToArray());
    public Memory<byte> KeyOfRC4Key { get; set; } = new(DefaultKeyOfRC4Key.ToArray());
    public Memory<byte> MetadataKey { get; set; } = new(DefaultMetadataKey.ToArray());
    public Memory<byte> Padding0 { get; set; } = new(DefaultPadding0.ToArray());
    public Memory<byte> Padding1 { get; set; } = new(DefaultPadding1.ToArray());
    public Memory<byte> Padding2 { get; set; } = new(DefaultPadding2.ToArray());
    public Memory<byte> Gap0 { get; set; } =
#if NET5_0_OR_GREATER
        GC.AllocateUninitializedArray<byte>(2);
#else
        new byte[2];
#endif
    public Memory<byte> Gap1 { get; set; } =
#if NET5_0_OR_GREATER
    GC.AllocateUninitializedArray<byte>(9);
#else
        new byte[2];
#endif
    public Memory<byte> RC4Key { get; set; } =
#if NET5_0_OR_GREATER
    GC.AllocateUninitializedArray<byte>(112);
#else
        new byte[2];
#endif
    public NCMMetadata? Metadata { get; set; } = null;
    public Memory<byte> CoverImage { get; set; } = Memory<byte>.Empty;
    public Memory<byte> MusicData { get; set; } = Memory<byte>.Empty;

    public ReadOnlySpan<byte> SBox
    {
        get
        {
            GenerateSBox(RC4Key.Span, _sBox);
            return _sBox;
        }
    }

    private static byte[] ReadChunkArray(Stream stream)
    {
        int len = Util.ReadI32(stream);
#if NET5_0_OR_GREATER
        byte[] chunk = GC.AllocateUninitializedArray<byte>(len);
#else
        byte[] chunk = new byte[len];
#endif
        stream.ReadExactly(chunk);
        return chunk;
    }
    private static PooledArray<byte> ReadChunk(Stream stream)
    {
        int len = Util.ReadI32(stream);
        PooledArray<byte> chunk = new(len);
        stream.ReadExactly(chunk);
        return chunk;
    }
    private static byte[] ReadChunkArray(ReadOnlySpan<byte> buffer)
    {
        int len = Util.ReadI32(buffer);
#if NET5_0_OR_GREATER
        byte[] chunk = GC.AllocateUninitializedArray<byte>(len);
#else
        byte[] chunk = new byte[len];
#endif
        buffer.Slice(4, len).CopyTo(chunk);
        return chunk;
    }
    private static PooledArray<byte> ReadChunk(ReadOnlySpan<byte> buffer)
    {
        int len = Util.ReadI32(buffer);
        PooledArray<byte> chunk = new(len);
        buffer.Slice(4, len).CopyTo(chunk);
        return chunk;
    }
    private static void WriteChunk(Stream stream, ReadOnlySpan<byte> data)
    {
        Util.Write(stream, data.Length);
        stream.Write(data);
    }
    private static int WriteChunk(Span<byte> buffer, ReadOnlySpan<byte> data)
    {
        Util.Write(buffer, data.Length);
        data.CopyTo(buffer[4..]);
        return data.Length + 4;
    }
    private static byte[] AesDecrypt(ReadOnlySpan<byte> data, byte[] key)
    {
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.PKCS7;
        aes.Key = key;
#if NET6_0_OR_GREATER
        return aes.DecryptEcb(data, PaddingMode.PKCS7);
#else
        using MemoryStream ms = new();
        using CryptoStream cs = new(ms, aes.CreateDecryptor(key, null), CryptoStreamMode.Write);
        cs.Write(data);
        return ms.ToArray();
#endif
    }
    private static byte[] AesEncrypt(ReadOnlySpan<byte> data, byte[] key)
    {
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.PKCS7;
        aes.Key = key;
#if NET6_0_OR_GREATER
        return aes.EncryptEcb(data, PaddingMode.PKCS7);
#else
        using MemoryStream ms = new();
        using CryptoStream cs = new(ms, aes.CreateEncryptor(key, null), CryptoStreamMode.Write);
        cs.Write(data);
        return ms.ToArray();
#endif
    }
    private static void GenerateSBox(ReadOnlySpan<byte> key, Span<byte> result)
    {
        if (key.Length == 0)
            throw new ArgumentException("empty key", nameof(key));
        if (result.Length < 256)
            throw new ArgumentException("result span too small", nameof(result));
        int i;
        for (i = 0; i < 256; i++)
            result[i] = (byte)i;

        byte swap, c;
        byte last_byte = 0;
        byte key_offset = 0;
        for (i = 0; i < 256; i++)
        {
            swap = result[i];
            c = (byte)(swap + last_byte + key[key_offset++]);
            if (key_offset >= key.Length)
                key_offset = 0;
            result[i] = result[c];
            result[c] = swap;
            last_byte = c;
        }
    }

    #region 读
    public static NCM Create(byte[] bytes)
    {
        using MemoryStream ms = new(bytes);
        return Create(ms);
    }
    public static NCM Create(string path)
    {
        using FileStream fs = new(path, FileMode.Open, FileAccess.Read);
        return Create(fs);
    }
    public static NCM Create(string path, FileShare share)
    {
        using FileStream fs = new(path, FileMode.Open, FileAccess.Read, share);
        return Create(fs);
    }
    public static NCM Create(Stream stream)
    {
        NCM ncm = new();
        ncm.ReadFromStream(stream);
        return ncm;
    }
    public static NCM Create(ReadOnlySpan<byte> buffer)
    {
        NCM ncm = new();
        ncm.ReadFromSpan(buffer);
        return ncm;
    }
    public void ReadFromStream(Stream stream)
    {
        // 第 0 部分：魔术头
        {
            Span<byte> magicHeader = stackalloc byte[8];
            stream.ReadExactly(magicHeader);
            if (!magicHeader.SequenceEqual(MagicHeader.Span))
                throw new InvalidDataException("Not a valid NCM file!");
        }
        // 第 1 部分：间隔（未知数据）
        {
            stream.ReadExactly(Gap0.Span);
        }
        // 第 2 部分：RC4 密钥（原始数据 -> XOR -> AES 解密 -> 数据）
        {
            using PooledArray<byte> array = ReadChunk(stream);
            Span<byte> rc4KeyChunk = array;
            for (int i = 0; i < rc4KeyChunk.Length; i++)
                rc4KeyChunk[i] ^= 0x64;
            ReadOnlySpan<byte> decryptedRC4Key = AesDecrypt(rc4KeyChunk, KeyOfRC4Key.ToArray());
            // 跳过 "neteasecloudmusic"
            if (!decryptedRC4Key.StartsWith(Padding0.Span))
                throw new InvalidDataException("Not a valid NCM file! Padding0 is not correct.");
            ReadOnlySpan<byte> rc4key = decryptedRC4Key[Padding0.Length..];
            if (RC4Key.Length == rc4key.Length)
                rc4key.CopyTo(RC4Key.Span);
            else
                RC4Key = new(rc4key.ToArray());
        }
        // 第 3 部分：元数据（原始数据 -> XOR -> Base64 解码 -> AES 解密 -> JSON 反序列化 -> 数据)
        {
            using PooledArray<byte> array = ReadChunk(stream);
            Span<byte> metadataChunk = array;
            for (int i = 0; i < metadataChunk.Length; i++)
                metadataChunk[i] ^= 0x63;
            // 跳过 "163 key(Don't modify):"
            if (!metadataChunk.StartsWith(Padding1.Span))
                throw new InvalidDataException("Not a valid NCM file! Padding1 is not correct.");
            ReadOnlySpan<byte> base64Metadata = metadataChunk[Padding1.Length..];
            Span<char> base64Chars = stackalloc char[base64Metadata.Length];
            Encoding.ASCII.GetChars(base64Metadata, base64Chars);
            Span<byte> encryptedMetadata = stackalloc byte[base64Chars.Length * 3 / 4 + 1];
            Util.TryFromBase64Chars(base64Chars, encryptedMetadata, out int len);
            ReadOnlySpan<byte> decryptedMetadata = AesDecrypt(encryptedMetadata[..len], MetadataKey.ToArray());
            // 跳过 "music:"
            if (!decryptedMetadata.StartsWith(Padding2.Span))
                throw new InvalidDataException("Not a valid NCM file! Padding2 is not correct.");
            ReadOnlySpan<byte> jsonMetadata = decryptedMetadata[Padding2.Length..];
            Metadata = JsonSerializer.Deserialize(jsonMetadata, SourceGenerationContext.Default.NCMMetadata);
        }
        // 第 4 部分：间隔（未知数据）
        {
            stream.ReadExactly(Gap1.Span);
        }
        // 第 5 部分：封面
        {
            CoverImage = ReadChunkArray(stream);
        }
        // 第 6 部分：音乐数据（RC4 解密）
        {
            ReadOnlySpan<byte> sBox = SBox;
            using MemoryStream ms = new();
            Span<byte> chunk = stackalloc byte[0x8000];
            int n = 0x8000;
            while (n > 0)
            {
                n = stream.ReadAtLeast(chunk, n, false);
                for (int i = 0; i < n; i++)
                {
                    int j = (i + 1) & 0xff;
                    chunk[i] ^= sBox[(sBox[j] + sBox[(sBox[j] + j) & 0xff]) & 0xff];
                }
                ms.Write(chunk[..n]);
            }
            MusicData = ms.ToArray();
        }
    }
    public void ReadFromSpan(ReadOnlySpan<byte> buffer)
    {
        // 第 0 部分：魔术头
        {
            if (!buffer[..8].SequenceEqual(MagicHeader.Span))
                throw new InvalidDataException("Not a valid NCM file!");
            buffer = buffer[8..];
        }
        // 第 1 部分：间隔（未知数据）
        {
            int len = Gap0.Length;
            buffer[..len].CopyTo(Gap0.Span);
            buffer = buffer[len..];
        }
        // 第 2 部分：RC4 密钥（原始数据 -> XOR -> AES 解密 -> 数据）
        {
            using PooledArray<byte> array = ReadChunk(buffer);
            Span<byte> rc4KeyChunk = array;
            buffer = buffer[(4 + rc4KeyChunk.Length)..];
            for (int i = 0; i < rc4KeyChunk.Length; i++)
                rc4KeyChunk[i] ^= 0x64;
            ReadOnlySpan<byte> decryptedRC4Key = AesDecrypt(rc4KeyChunk, KeyOfRC4Key.ToArray());
            // 跳过 "neteasecloudmusic"
            if (!decryptedRC4Key.StartsWith(Padding0.Span))
                throw new InvalidDataException("Not a valid NCM file! Padding0 is not correct.");
            ReadOnlySpan<byte> rc4key = decryptedRC4Key[Padding0.Length..];
            if (RC4Key.Length == rc4key.Length)
                rc4key.CopyTo(RC4Key.Span);
            else
                RC4Key = new(rc4key.ToArray());
        }
        // 第 3 部分：元数据（原始数据 -> XOR -> Base64 解码 -> AES 解密 -> JSON 反序列化 -> 数据)
        {
            using PooledArray<byte> array = ReadChunk(buffer);
            Span<byte> metadataChunk = array;
            buffer = buffer[(4 + metadataChunk.Length)..];
            for (int i = 0; i < metadataChunk.Length; i++)
                metadataChunk[i] ^= 0x63;
            // 跳过 "163 key(Don't modify):"
            if (!metadataChunk.StartsWith(Padding1.Span))
                throw new InvalidDataException("Not a valid NCM file! Padding1 is not correct.");
            ReadOnlySpan<byte> base64Metadata = metadataChunk[Padding1.Length..];
            Span<char> base64Chars = stackalloc char[base64Metadata.Length];
            Encoding.ASCII.GetChars(base64Metadata, base64Chars);
            Span<byte> encryptedMetadata = stackalloc byte[base64Chars.Length * 3 / 4 + 1];
            Util.TryFromBase64Chars(base64Chars, encryptedMetadata, out int len);
            ReadOnlySpan<byte> decryptedMetadata = AesDecrypt(encryptedMetadata[..len], MetadataKey.ToArray());
            // 跳过 "music:"
            if (!decryptedMetadata.StartsWith(Padding2.Span))
                throw new InvalidDataException("Not a valid NCM file! Padding2 is not correct.");
            ReadOnlySpan<byte> jsonMetadata = decryptedMetadata[Padding2.Length..];
            Metadata = JsonSerializer.Deserialize(jsonMetadata, SourceGenerationContext.Default.NCMMetadata);
        }
        // 第 4 部分：间隔（未知数据）
        {
            int len = Gap1.Length;
            buffer[..len].CopyTo(Gap1.Span);
            buffer = buffer[len..];
        }
        // 第 5 部分：封面
        {
            CoverImage = ReadChunkArray(buffer);
            buffer = buffer[(4 + CoverImage.Length)..];
        }
        // 第 6 部分：音乐数据（RC4 解密）
        {
            ReadOnlySpan<byte> sBox = SBox;
            byte[] musicData = new byte[buffer.Length];
            for (int i = 0; i < buffer.Length; i++)
            {
                int j = (i + 1) & 0xff;
                musicData[i] = (byte)(buffer[i] ^ sBox[(sBox[j] + sBox[(sBox[j] + j) & 0xff]) & 0xff]);
            }
            MusicData = musicData;
        }
    }
    #endregion 读

    #region 写
    public byte[] WriteAsBytes()
    {
        using MemoryStream ms = new();
        WriteToStream(ms);
        return ms.ToArray();
    }
    public void WriteToFile(string path)
    {
        using FileStream fs = new(path, FileMode.Create, FileAccess.Write);
        WriteToStream(fs);
    }
    public void WriteToFile(string path, FileShare share)
    {
        using FileStream fs = new(path, FileMode.Create, FileAccess.Write, share);
        WriteToStream(fs);
    }
    public void WriteToStream(Stream stream)
    {
        // 第 0 部分：魔术头
        {
            stream.Write(MagicHeader.Span);
        }
        // 第 1 部分：间隔（未知数据）
        {
            stream.Write(Gap0.Span);
        }
        // 第 2 部分：RC4 密钥
        {
            Span<byte> rc4KeyChuck = stackalloc byte[RC4Key.Length + Padding0.Length];
            Padding0.Span.CopyTo(rc4KeyChuck);
            RC4Key.Span.CopyTo(rc4KeyChuck[Padding0.Length..]);
            Span<byte> encryptedRC4KeyChuck = AesEncrypt(rc4KeyChuck, KeyOfRC4Key.ToArray());
            for (int i = 0; i < encryptedRC4KeyChuck.Length; i++)
                encryptedRC4KeyChuck[i] ^= 0x64;
            WriteChunk(stream, encryptedRC4KeyChuck);
        }
        // 第 3 部分：元数据
        {
            ReadOnlySpan<byte> encryptedMetadata;
            using (MemoryStream ms = new())
            {
                ms.Write(Padding2.Span);
                if (Metadata is null)
                    ms.Write("{}"u8);
                else
                    JsonSerializer.Serialize(ms, Metadata, SourceGenerationContext.Default.NCMMetadata);
                encryptedMetadata = AesEncrypt(ms.ToArray(), MetadataKey.ToArray());
            }
            Span<char> base64Chars = stackalloc char[encryptedMetadata.Length * 4 / 3 + 3];
            Util.TryToBase64Chars(encryptedMetadata, base64Chars, out int len);
            Span<byte> metadataChunk = stackalloc byte[len + Padding1.Length];
            Padding1.Span.CopyTo(metadataChunk);
            Encoding.ASCII.GetBytes(base64Chars[..len], metadataChunk[Padding1.Length..]);
            for (int i = 0; i < metadataChunk.Length; i++)
                metadataChunk[i] ^= 0x63;
            WriteChunk(stream, metadataChunk);
        }
        // 第 4 部分：间隔（未知数据）
        {
            stream.Write(Gap1.Span);
        }
        // 第 5 部分：封面
        {
            WriteChunk(stream, CoverImage.Span);
        }
        // 第 6 部分：音乐数据（RC4 加密）
        {
            ReadOnlySpan<byte> sBox = SBox;
            ReadOnlySpan<byte> musicData = MusicData.Span;
            Span<byte> chunk = stackalloc byte[0x1000];
            int n = 0x1000;
            while (musicData.Length > 0)
            {
                n = Math.Min(n, musicData.Length);
                musicData[..n].CopyTo(chunk);
                musicData = musicData[n..];
                for (int i = 0; i < n; i++)
                {
                    int j = (i + 1) & 0xff;
                    chunk[i] ^= sBox[(sBox[j] + sBox[(sBox[j] + j) & 0xff]) & 0xff];
                }
                stream.Write(chunk[..n]);
            }
        }
    }
    public int WriteToSpan(Span<byte> buffer)
    {
        int length = 0;
        // 第 0 部分：魔术头
        {
            MagicHeader.Span.CopyTo(buffer);
            buffer = buffer[MagicHeader.Length..];
            length += MagicHeader.Length;
        }
        // 第 1 部分：间隔（未知数据）
        {
            Gap0.Span.CopyTo(buffer);
            buffer = buffer[Gap0.Length..];
            length += Gap0.Length;
        }
        // 第 2 部分：RC4 密钥
        {
            Span<byte> rc4KeyChuck = stackalloc byte[RC4Key.Length + Padding0.Length];
            Padding0.Span.CopyTo(rc4KeyChuck);
            RC4Key.Span.CopyTo(rc4KeyChuck[Padding0.Length..]);
            Span<byte> encryptedRC4KeyChuck = AesEncrypt(rc4KeyChuck, KeyOfRC4Key.ToArray());
            for (int i = 0; i < encryptedRC4KeyChuck.Length; i++)
                encryptedRC4KeyChuck[i] ^= 0x64;
            int len = WriteChunk(buffer, encryptedRC4KeyChuck);
            buffer = buffer[len..];
            length += len;
        }
        // 第 3 部分：元数据
        {
            ReadOnlySpan<byte> encryptedMetadata;
            using (MemoryStream ms = new())
            {
                ms.Write(Padding2.Span);
                if (Metadata is null)
                    ms.Write("{}"u8);
                else
                    JsonSerializer.Serialize(ms, Metadata, SourceGenerationContext.Default.NCMMetadata);
                encryptedMetadata = AesEncrypt(ms.ToArray(), MetadataKey.ToArray());
            }
            Span<char> base64Chars = stackalloc char[encryptedMetadata.Length * 4 / 3 + 3];
            Util.TryToBase64Chars(encryptedMetadata, base64Chars, out int len);
            Span<byte> metadataChunk = stackalloc byte[len + Padding1.Length];
            Padding1.Span.CopyTo(metadataChunk);
            Encoding.ASCII.GetBytes(base64Chars[..len], metadataChunk[Padding1.Length..]);
            for (int i = 0; i < metadataChunk.Length; i++)
                metadataChunk[i] ^= 0x63;
            len = WriteChunk(buffer, metadataChunk);
            buffer = buffer[len..];
            length += len;
        }
        // 第 4 部分：间隔（未知数据）
        {
            Gap0.Span.CopyTo(buffer);
            buffer = buffer[Gap1.Length..];
            length += Gap0.Length;
        }
        // 第 5 部分：封面
        {
            int len = WriteChunk(buffer, CoverImage.Span);
            buffer = buffer[len..];
            length += len;
        }
        // 第 6 部分：音乐数据（RC4 加密）
        {
            ReadOnlySpan<byte> sBox = SBox;
            ReadOnlySpan<byte> musicData = MusicData.Span;
            for (int i = 0; i < musicData.Length; i++)
            {
                int j = (i + 1) & 0xff;
                buffer[i] = (byte)(musicData[i] ^ sBox[(sBox[j] + sBox[(sBox[j] + j) & 0xff]) & 0xff]);
            }
            length += musicData.Length;
        }
        return length;
    }
    #endregion 写

    public override string ToString()
    {
        StringBuilder sb = new();
        sb.AppendLine("Encrypted Netease Cloud Music Format");

        sb.Append("MusicName:".PadRight(18));
        sb.AppendLine(Metadata?.MusicName);

        sb.Append("Artists:".PadRight(18));
        sb.AppendLine(string.Join("/", Metadata?.Artist?.Select(a => a.Name) ?? []));

        sb.Append("Album:".PadRight(18));
        sb.AppendLine(Metadata?.Album);

        sb.Append("Cover:".PadRight(18));
        sb.AppendLine(Metadata?.AlbumPic);

        sb.Append("Bitrate:".PadRight(18));
        sb.AppendLine(Metadata?.Bitrate?.ToString() ?? "null");

        sb.Append("Duration:".PadRight(18));
        sb.AppendLine(Metadata?.Duration?.ToString() ?? "null");

        sb.Append("Format:".PadRight(18));
        sb.AppendLine(Metadata?.Format);

        sb.Append("RC4 Key:".PadRight(18));
        sb.AppendLine(Util.BytesToString(RC4Key.Span, 16));

        sb.Append("Cover (Embedded):".PadRight(18));
        sb.AppendLine(Util.BytesToString(CoverImage.Span, 16));

        sb.Append("Music Data:".PadRight(18));
        sb.AppendLine(Util.BytesToString(MusicData.Span, 16));

        return sb.ToString();
    }
}
