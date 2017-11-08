using System;
using System.Text;

namespace WorldFoundry.Extensions
{
    internal static class StringExtensions
    {
        private static string alpha = "abcdefghijklmnopqrstuvwxyz";

        /// <summary>
        /// Gets a string of the given length comprised of randomly selected lower-case letters.
        /// </summary>
        public static string GetRandomLetters(int length)
        {
            var sb = new StringBuilder();
            var r = new Random();
            for (int i = 0; i < length; i++)
            {
                sb.Append(alpha[r.Next(26)]);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Converts a string of hexadecimal characters to an int.
        /// </summary>
        public static int HexToInt(this string str)
        {
            int n = 0;
            for (int i = 0; i < str.Length; i++)
            {
                n *= 16;
                n += str[i];
                if (str[i] >= 'a')
                {
                    n += 10 - 'a';
                }
                else if (str[i] >= 'A')
                {
                    n += 10 - 'A';
                }
                else
                {
                    n -= '0';
                }
            }
            return n;
        }
    }
}
