using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WorldFoundry
{
    /// <summary>
    /// Static methods for storing and retrieving <see cref="IdItem"/> instances.
    /// </summary>
    public static class DataStore
    {
        /// <summary>
        /// <para>
        /// The current underlying data store.
        /// </para>
        /// <para>
        /// Can be set to any implementation of <see cref="IDataStore"/>.
        /// </para>
        /// </summary>
        public static IDataStore Instance = new InMemoryDataStore();

        /// <summary>
        /// Gets the <see cref="IdItem"/> with the given <paramref name="id"/>.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="IdItem"/> to retrieve.</typeparam>
        /// <param name="id">The unique id of the item to retrieve.</param>
        /// <returns>The item with the given id, or <see langword="null"/> if no item was found with
        /// that id.</returns>
        public static async Task<T?> GetItemAsync<T>(string? id) where T : IdItem
            => string.IsNullOrEmpty(id) ? null : await Instance.GetItemAsync<T>(id).ConfigureAwait(false);

        /// <summary>
        /// Enumerates all items in the data store of the given type.
        /// </summary>
        /// <typeparam name="T">The type of items to enumerate.</typeparam>
        /// <returns>An <see cref="IAsyncEnumerable{T}"/> of items in the data store of the given
        /// type.</returns>
        public static IAsyncEnumerable<T> GetItemsAsync<T>() where T : IdItem
            => Instance.GetItemsAsync<T>();

        /// <summary>
        /// Enumerates all items in the data store of the given type which satisfy the given
        /// condition.
        /// </summary>
        /// <typeparam name="T">The type of items to enumerate.</typeparam>
        /// <param name="condition">A condition which items must satisfy.</param>
        /// <returns>An <see cref="IAsyncEnumerable{T}"/> of items in the data store of the given
        /// type.</returns>
        public static IAsyncEnumerable<T> GetItemsWhereAsync<T>(Func<T, bool> condition) where T : IdItem
            => Instance.GetItemsWhereAsync<T>(condition);

        /// <summary>
        /// Removes the stored item with the given id.
        /// </summary>
        /// <param name="id">The id of the item to remove.</param>
        /// <remarks>Has no effect if there is no stored item with the given id.</remarks>
        public static Task RemoveItemAsync(string id)
            => Instance.RemoveItemAsync(id);

        /// <summary>
        /// Stores the given <paramref name="item"/>.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="IdItem"/> to store.</typeparam>
        public static Task SetItemAsync<T>(T item) where T : IdItem
            => Instance.SetItemAsync<T>(item);
    }
}
