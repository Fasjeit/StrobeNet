namespace StrobeNet.Extensions
{
    using System;
    using System.Linq;

    /// <summary>
    /// Array convert extensions
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Converts hex string to byte array
        /// </summary>
        /// <param name="hex">String to decode</param>
        public static byte[] ToByteArray(this string hex)
        {
            return Enumerable.Range(0, hex.Length).Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(hex.Substring(x, 2), 16)).ToArray();
        }

        /// <summary>
        /// Converts hex string to <see cref="Span{T}"/>
        /// </summary>
        /// <param name="hex">String to decode</param>
        public static Span<byte> ToSpan(this string hex)
        {
            return hex.ToByteArray().AsSpan();
        }

        /// <summary>
        /// Convers byte array to hex string
        /// </summary>
        /// <param name="array">Array to endoce</param>
        public static string ToHexString(this byte[] array)
        {
            return BitConverter.ToString(array).Replace("-", "");
        }

        /// <summary>
        /// Convers <see cref="Span{T}"/> to hex string
        /// </summary>
        /// <param name="array">Array to endoce</param>
        public static string ToHexString(this Span<byte> array)
        {
            return array.ToArray().ToHexString();
        }
    }
}