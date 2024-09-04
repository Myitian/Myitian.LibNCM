using System.Text;

namespace Myitian.LibNCM;

public static class Util
{
    public static void Write(Stream stream, int value)
    {
        ReadOnlySpan<byte> buffer = [
            (byte)(value >> 24),
            (byte)(value >> 16),
            (byte)(value >> 8),
            (byte)value
            ];
        stream.Write(buffer);
    }
    public static int ReadI32(Stream stream)
    {
        Span<byte> bytes = stackalloc byte[4];
        stream.ReadExactly(bytes);
        return
            (bytes[0] << 24) |
            (bytes[1] << 16) |
            (bytes[2] << 8) |
            bytes[3];
    }
    /// <summary>
    /// A Python-Bytes/ByteArray-stringification-like ToString method with length limit.
    /// </summary>
    /// <param name="bytes">Bytes to stringify</param>
    /// <param name="limit">Maximum number of bytes to stringify</param>
    /// <returns>Returns stringified <paramref name="bytes"/></returns>
    public static string BytesToString(Span<byte> bytes, int limit)
    {
        StringBuilder sb = new(limit);
        sb.Append("b\"");
        for (int i = 0; i < bytes.Length; i++)
        {
            if (i == limit)
            {
                return sb.Append($"\" ... and {bytes.Length - limit} more").ToString();
            }
            byte b = bytes[i];
            switch (b)
            {
                case 0x09:
                    sb.Append("\\t");
                    break;
                case 0x0A:
                    sb.Append("\\n");
                    break;
                case 0x0D:
                    sb.Append("\\r");
                    break;
                default:
                    if (0x1F < b && b < 0x7F)
                    {
                        sb.Append((char)b);
                    }
                    else
                    {
                        sb.Append("\\x");
                        sb.Append(Convert.ToString(b, 16).PadLeft(2, '0'));
                    }
                    break;
            }
        }
        return sb.Append('"').ToString();
    }
}
