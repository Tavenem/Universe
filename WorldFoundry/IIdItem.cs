using System.Threading.Tasks;

namespace NeverFoundry.WorldFoundry
{
    /// <summary>
    /// An item with an ID.
    /// </summary>
    public interface IIdItem
    {
        /// <summary>
        /// The ID of this item.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Removes this item from the data store.
        /// </summary>
        public virtual Task DeleteAsync() => DataStore.RemoveItemAsync(Id);

        /// <summary>
        /// Saves this item to the data store.
        /// </summary>
        public virtual Task SaveAsync() => DataStore.SetItemAsync(this);
    }
}