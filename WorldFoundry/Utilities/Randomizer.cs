using System;
using System.Collections.Generic;
using Troschuetz.Random;

namespace WorldFoundry
{
    internal static class Randomizer
    {
        internal static TRandom Static = new TRandom();

        /// <summary>
        /// Returns a random item from given list, according to a uniform distribution.
        /// </summary>
        /// <typeparam name="TGen">The type of the random numbers generator.</typeparam>
        /// <typeparam name="TItem">The type of the elements of the list.</typeparam>
        /// <param name="generator">The generator from which random numbers are drawn.</param>
        /// <param name="list">The list from which an item should be randomly picked.</param>
        /// <returns>A random item belonging to given list.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="list"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="list"/> is empty.</exception>
        public static TItem Choice<TGen, TItem>(this TGen generator, HashSet<TItem> list) where TGen : class, IGenerator
        {
            if (list == null)
            {
                throw new ArgumentNullException($"{nameof(list)} cannot be null.");
            }
            if (list.Count == 0)
            {
                throw new ArgumentException($"{nameof(list)} cannot be empty.");
            }

            var index = generator.Next(list.Count);
            var enumerator = list.GetEnumerator();
            var c = 0;
            while (c < index)
            {
                enumerator.MoveNext();
                c++;
            }
            return enumerator.Current;
        }
    }
}
