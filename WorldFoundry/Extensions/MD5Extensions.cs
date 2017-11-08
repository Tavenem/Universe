using System.Security.Cryptography;
using System.Text;

namespace WorldFoundry.Extensions
{
    internal static class MD5Extensions
    {
        /// <summary>
        /// Computes MD5 hash from a string.
        /// </summary>
        public static string GetHash(this MD5 md5, string input)
        {
            var data = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
            var sb = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                sb.Append(data[i].ToString("x2"));
            }
            return sb.ToString();
        }
    }
}
