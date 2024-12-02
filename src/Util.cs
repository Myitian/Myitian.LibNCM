using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Text;

namespace Myitian.LibNCM;

public static class Util
{
    public static bool TryToBase64Chars(ReadOnlySpan<byte> bytes, Span<char> chars, out int charsWritten)
    {
#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP
        byte[] sharedByteBuffer = ArrayPool<byte>.Shared.Rent(bytes.Length);
        char[] sharedCharBuffer = ArrayPool<char>.Shared.Rent(chars.Length);
        try
        {
            int offset = sharedCharBuffer.Length - chars.Length;
            int n = Convert.ToBase64CharArray(sharedByteBuffer, 0, bytes.Length, sharedCharBuffer, offset);
            if (n > chars.Length)
            {
                charsWritten = 0;
                return false;
            }
            else
            {
                sharedCharBuffer.AsSpan(offset, n).CopyTo(chars);
                charsWritten = n;
                return true;
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(sharedByteBuffer);
            ArrayPool<char>.Shared.Return(sharedCharBuffer);
        }
#else
        return Convert.TryToBase64Chars(bytes, chars, out charsWritten);
#endif
    }
    public static bool TryFromBase64Chars(ReadOnlySpan<char> chars, Span<byte> bytes, out int bytesWritten)
    {
#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP
        char[] sharedBuffer = ArrayPool<char>.Shared.Rent(chars.Length);
        try
        {
            chars.CopyTo(sharedBuffer);
            byte[] buffer = Convert.FromBase64CharArray(sharedBuffer, 0, chars.Length);
            if (buffer.Length > bytes.Length)
            {
                bytesWritten = 0;
                return false;
            }
            else
            {
                buffer.CopyTo(bytes);
                bytesWritten = buffer.Length;
                return true;
            }
        }
        finally
        {
            ArrayPool<char>.Shared.Return(sharedBuffer);
        }
#else
        return Convert.TryFromBase64Chars(chars, bytes, out bytesWritten);
#endif
    }
#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP
    internal static unsafe int GetChars(this Encoding encoding, ReadOnlySpan<byte> bytes, Span<char> chars)
    {
        fixed (byte* bytesPtr = bytes)
        fixed (char* charsPtr = chars)
        {
            return encoding.GetChars(bytesPtr, bytes.Length, charsPtr, chars.Length);
        }
    }
    internal static unsafe int GetBytes(this Encoding encoding, ReadOnlySpan<char> chars, Span<byte> bytes)
    {
        fixed (char* charsPtr = chars)
        fixed (byte* bytesPtr = bytes)
        {
            return encoding.GetBytes(charsPtr, chars.Length, bytesPtr, bytes.Length);
        }
    }
    internal static int Read(this Stream stream, Span<byte> buffer)
    {
        byte[] sharedBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length);
        try
        {
            int numRead = stream.Read(sharedBuffer, 0, buffer.Length);
            new ReadOnlySpan<byte>(sharedBuffer, 0, numRead).CopyTo(buffer);
            return numRead;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(sharedBuffer);
        }
    }
    internal static void Write(this Stream stream, ReadOnlySpan<byte> buffer)
    {
        byte[] sharedBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length);
        try
        {
            buffer.CopyTo(sharedBuffer);
            stream.Write(sharedBuffer, 0, buffer.Length);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(sharedBuffer);
        }
    }
#endif
#if !NET7_0_OR_GREATER
    internal static void ReadExactly(this Stream stream, Span<byte> buffer)
    {
        stream.ReadAtLeastCore(buffer, buffer.Length, true);
    }
    internal static int ReadAtLeast(this Stream stream, Span<byte> buffer, int minimumBytes, bool throwOnEndOfStream = true)
    {
        if (minimumBytes < 0)
            throw new ArgumentOutOfRangeException(nameof(minimumBytes), "minimumBytes is below zero");
        if (minimumBytes > buffer.Length)
            throw new ArgumentOutOfRangeException(nameof(minimumBytes), "minimumBytes is larger than buffer length");

        return stream.ReadAtLeastCore(buffer, minimumBytes, throwOnEndOfStream);
    }
    private static int ReadAtLeastCore(this Stream stream, Span<byte> buffer, int minimumBytes, bool throwOnEndOfStream)
    {
        Debug.Assert(minimumBytes <= buffer.Length);

        int totalRead = 0;
        while (totalRead < minimumBytes)
        {
            int read = stream.Read(buffer[totalRead..]);
            if (read == 0)
            {
                if (throwOnEndOfStream)
                    throw new EndOfStreamException();
                return totalRead;
            }
            totalRead += read;
        }
        return totalRead;
    }
#endif
    public static void Write(Stream stream, int value)
    {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
        Span<byte> buffer = stackalloc byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(buffer, value);
#else
        ReadOnlySpan<byte> buffer = [
            (byte)value,
            (byte)(value >> 8),
            (byte)(value >> 16),
            (byte)(value >> 24)
            ];
#endif
        stream.Write(buffer);
    }
    public static void Write(Span<byte> bytes, int value)
    {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
        BinaryPrimitives.WriteInt32LittleEndian(bytes, value);
#else
        bytes[0] = (byte)value;
        bytes[1] = (byte)(value >> 8);
        bytes[2] = (byte)(value >> 16);
        bytes[3] = (byte)(value >> 24);
#endif
    }
    public static int ReadI32(Stream stream)
    {
        Span<byte> bytes = stackalloc byte[4];
        stream.ReadExactly(bytes);
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
        return BinaryPrimitives.ReadInt32LittleEndian(bytes);
#else
        return bytes[0]
            | (bytes[1] << 8)
            | (bytes[2] << 16)
            | (bytes[3] << 24);
#endif
    }
    public static int ReadI32(ReadOnlySpan<byte> bytes)
    {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
        return BinaryPrimitives.ReadInt32LittleEndian(bytes);
#else
        return bytes[0]
            | (bytes[1] << 8)
            | (bytes[2] << 16)
            | (bytes[3] << 24);
#endif
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
