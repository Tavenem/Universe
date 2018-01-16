using System.Collections.Generic;
using System.Linq;

namespace WorldFoundry.WorldGrids
{
    public static class IndexedItemExtensions
    {
        internal static void SetIndexedArray<T>(this ICollection<T> collection, ref T[] array) where T : IIndexedItem
        {
            array = new T[collection.Count];
            for (int i = 0; i < collection.Count; i++)
            {
                array[i] = collection.FirstOrDefault(x => x.Index == i);
            }
        }
    }
}
