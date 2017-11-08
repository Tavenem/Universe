using System.Security.Cryptography;
using Troschuetz.Random;
using WorldFoundry.Extensions;

namespace WorldFoundry.Utilities
{
    internal class Randomizer
    {
        internal static TRandom Static = new TRandom();

        public TRandom Random { get; set; }

        public Randomizer() => Random = new TRandom();

        public Randomizer(string seed) => Random = new TRandom(GetSeed(seed));

        public static int GetSeed(string seed)
        {
            int s;
            using (var md5 = MD5.Create())
            {
                s = md5.GetHash(seed).HexToInt();
            }
            return s;
        }
    }
}
