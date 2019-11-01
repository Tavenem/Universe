﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NeverFoundry.WorldFoundry
{
    /// <summary>
    /// An in-memory data store for <see cref="IdItem"/> instances.
    /// </summary>
    public class InMemoryDataStore : IDataStore
    {
        private readonly Dictionary<string, IdItem> _data = new Dictionary<string, IdItem>();

        /// <summary>
        /// Gets the <see cref="IdItem" /> with the given <paramref name="id" />.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="IdItem" /> to retrieve.</typeparam>
        /// <param name="id">The unique id of the item to retrieve.</param>
        /// <returns>The item with the given id, or <see langword="null" /> if no item was found with
        /// that id.</returns>
        public Task<T?> GetItemAsync<T>(string id) where T : IdItem
            => Task.FromResult(_data.TryGetValue(id, out var item) ? item as T : null);

        /// <summary>
        /// Enumerates all items in the data store of the given type.
        /// </summary>
        /// <typeparam name="T">The type of items to enumerate.</typeparam>
        /// <returns>An <see cref="IAsyncEnumerable{T}"/> of items in the data store of the given
        /// type.</returns>
        public IAsyncEnumerable<T> GetItemsAsync<T>() where T : IdItem
            => _data.Values.OfType<T>().ToAsyncEnumerable();

        /// <summary>
        /// Enumerates all items in the data store of the given type which satisfy the given
        /// condition.
        /// </summary>
        /// <typeparam name="T">The type of items to enumerate.</typeparam>
        /// <param name="condition">A condition which items must satisfy.</param>
        /// <returns>An <see cref="IAsyncEnumerable{T}"/> of items in the data store of the given
        /// type.</returns>
        public IAsyncEnumerable<T> GetItemsWhereAsync<T>(Func<T, bool> condition) where T : IdItem
            => _data.Values.OfType<T>().Where(x => condition.Invoke(x)).ToAsyncEnumerable();

        /// <summary>
        /// Removes the stored item with the given id.
        /// </summary>
        /// <param name="id">The id of the item to remove.</param>
        /// <remarks>Has no effect if there is no stored item with the given id.</remarks>
        public Task RemoveItemAsync(string id)
        {
            if (!string.IsNullOrEmpty(id))
            {
                _data.Remove(id);
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Stores the given <paramref name="item" />.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="IdItem" /> to store.</typeparam>
        public Task SetItemAsync<T>(T item) where T : IdItem
        {
            _data[item.Id] = item;
            return Task.CompletedTask;
        }
    }
}