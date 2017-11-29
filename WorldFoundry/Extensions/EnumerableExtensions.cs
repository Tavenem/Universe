using System;
using System.Collections.Generic;
using System.Linq;

namespace WorldFoundry.Extensions
{
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Returns the first element of the sequence that satisfies a condition or null if no such
        /// element is found.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">An <see cref="IEnumerable{T}"/> to return an element from.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>
        /// null if <paramref name="source"/> is empty or if no element passes the test specified by
        /// <paramref name="predicate"/>; otherwise, the first element in <paramref name="source"/>
        /// that passes the test specified by <paramref name="predicate"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="predicate"/> is null.
        /// </exception>
        public static TSource? FirstOrNull<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate) where TSource : struct
        {
            source.FirstOrDefault(x => x.Equals(source));
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }
            foreach (var item in source)
            {
                if (predicate(item))
                {
                    return item;
                }
            }
            return null;
        }
    }
}
