using System;
using System.Text;

namespace Myitian.LibNCM
{
    public static class Util
    {
        public static byte[] GetLEBytes(int i)
        {
            byte[] b = BitConverter.GetBytes(i);
            if (!BitConverter.IsLittleEndian) Array.Reverse(b);
            return b;
        }
        public static int ToInt32(byte[] bytes, int startIndex, bool isLittleEndian = true)
        {
            return BitConverter.IsLittleEndian == isLittleEndian ?

                BitConverter.ToInt32(bytes, startIndex)
                :
                BitConverter.ToInt32(new byte[] { bytes[startIndex + 3], bytes[startIndex + 2], bytes[startIndex + 1], bytes[startIndex] }, 0);
        }

        /// <summary>
        /// Compares two byte arrays.
        /// </summary>
        /// <returns>Returns <see langword="true"/> if <paramref name="bytes0"/> and <paramref name="bytes1"/> are equal in length and have the same content; otherwise returns <see langword="false"/>.</returns>
        public static bool ByteArrayContentEquals(byte[] bytes0, byte[] bytes1)
        {
            if (bytes0 != null && bytes1 != null && bytes0.Length == bytes1.Length)
            {
                for (int i = 0; i < bytes0.Length; i++)
                {
                    if (bytes0[i] != bytes1[i])
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }
        /// <summary>
        /// A Python-Bytes/ByteArray-stringification-like ToString method with length limit.
        /// </summary>
        /// <param name="bytes">Bytes to stringify</param>
        /// <param name="limit">Maximum number of bytes to stringify</param>
        /// <returns>Returns stringified <paramref name="bytes"/></returns>
        public static string ByteArrayToString(byte[] bytes, int limit)
        {
            StringBuilder sb = new StringBuilder(limit);
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
}
