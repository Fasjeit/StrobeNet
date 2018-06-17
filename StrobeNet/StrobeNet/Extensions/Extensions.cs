namespace StrobeNet.Extensions
{
    using System;
    using System.Linq;

    public static class Extensions
    {
        public static byte[] ToByteArray(this string hex)
        {
            return Enumerable.Range(0, hex.Length).Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(hex.Substring(x, 2), 16)).ToArray();
        }

        public static string ToHexString(this byte[] array)
        {
            return BitConverter.ToString(array).Replace("-", "");
        }
    }
}
