using System;
using Troschuetz.Random;

namespace WorldFoundry.Utilities
{
    internal class Randomizer
    {
        internal static TRandom Static = new TRandom();

        public TRandom Random { get; set; }

        public Randomizer() => Random = new TRandom();

        public Randomizer(Guid id) => Random = new TRandom(id.GetHashCode());
    }
}
