using System;

namespace WorldFoundry
{
    /// <summary>
    /// Provides a means of generating unique IDs.
    /// </summary>
    public class ItemIdProvider : IItemIdProvider
    {
        /// <summary>
        /// The current default <see cref="IItemIdProvider"/> instance. Starts as a static
        /// instance of this class.
        /// <seealso cref="Instance"/>
        /// </summary>
        public static IItemIdProvider DefaultIDProvider { get; set; } = Instance;

        /// <summary>
        /// A static instance of the <see cref="ItemIdProvider"/> class.
        /// </summary>
        public static ItemIdProvider Instance => new ItemIdProvider();

        /// <summary>
        /// Generates a new, unique ID.
        /// </summary>
        /// <returns>A unique ID, as a string.</returns>
        public string GetNewID() => Guid.NewGuid().ToString("B");
    }
}
