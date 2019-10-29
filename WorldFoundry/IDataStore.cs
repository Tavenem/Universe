using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NeverFoundry.WorldFoundry
{
    /// <summary>
    /// An interface which allows <see cref="IdItem"/> instances to be stored and retrieved.
    /// </summary>
    public interface IDataStore
    {
        /// <summary>
        /// Gets the <see cref="IdItem"/> with the given <paramref name="id"/>.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="IdItem"/> to retrieve.</typeparam>
        /// <param name="id">The unique id of the item to retrieve.</param>
        /// <returns>The item with the given id, or <see langword="null"/> if no item was found with
        /// that id.</returns>
        Task<T?> GetItemAsync<T>(string id) where T : IdItem;

        /// <summary>
        /// Enumerates all items in the data store of the given type.
        /// </summary>
        /// <typeparam name="T">The type of items to enumerate.</typeparam>
        /// <returns>An <see cref="IAsyncEnumerable{T}"/> of items in the data store of the given
        /// type.</returns>
        IAsyncEnumerable<T> GetItemsAsync<T>() where T : IdItem;

        /// <summary>
        /// Enumerates all items in the data store of the given type which satisfy the given
        /// condition.
        /// </summary>
        /// <typeparam name="T">The type of items to enumerate.</typeparam>
        /// <param name="condition">A condition which items must satisfy.</param>
        /// <returns>An <see cref="IAsyncEnumerable{T}"/> of items in the data store of the given
        /// type.</returns>
        IAsyncEnumerable<T> GetItemsWhereAsync<T>(Func<T, bool> condition) where T : IdItem;

        /// <summary>
        /// Removes the stored item with the given id.
        /// </summary>
        /// <param name="id">The id of the item to remove.</param>
        /// <remarks>Has no effect if there is no stored item with the given id.</remarks>
        Task RemoveItemAsync(string id);

        /// <summary>
        /// Stores the given <paramref name="item"/>.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="IdItem"/> to store.</typeparam>
        Task SetItemAsync<T>(T item) where T : IdItem;
    }
}
