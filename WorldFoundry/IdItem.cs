using System;

namespace WorldFoundry
{
    /// <summary>
    /// An item with an ID.
    /// </summary>
    public abstract class IdItem : IEquatable<IdItem>
    {
        /// <summary>
        /// The ID of this item.
        /// </summary>
        public string Id { get; private protected set; }

        /// <summary>
        /// Initializes a new instance of <see cref="IdItem"/>.
        /// </summary>
        protected IdItem() => Id = Guid.NewGuid().ToString();

        /// <summary>
        /// Determines whether the specified <see cref="IdItem"/> instance is equal to this one.
        /// </summary>
        /// <param name="other">The <see cref="IdItem"/> instance to compare with this one.</param>
        /// <returns><see langword="true"/> if the specified <see cref="IdItem"/> instance is equal
        /// to this once; otherwise, <see langword="false"/>.</returns>
        public bool Equals(IdItem other)
            => !string.IsNullOrEmpty(Id) && string.Equals(Id, other?.Id, StringComparison.Ordinal);

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns><see langword="true"/> if the specified object is equal to the current object;
        /// otherwise, <see langword="false"/>.</returns>
        public override bool Equals(object obj) => obj is IdItem other && Equals(other);

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>The hash code for this instance.</returns>
        public override int GetHashCode() => Id.GetHashCode();

        /// <summary>Returns a string equivalent of this instance.</summary>
        /// <returns>A string equivalent of this instance.</returns>
        public override string ToString() => Id;
    }
}
