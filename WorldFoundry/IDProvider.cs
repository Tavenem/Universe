using System;

namespace WorldFoundry
{
    /// <summary>
    /// Provides a means of generating unique IDs.
    /// </summary>
    public class IdProvider : IIdProvider
    {
        /// <summary>
        /// The current default <see cref="IIdProvider"/> instance. Starts as a static
        /// instance of this class.
        /// <seealso cref="Instance"/>
        /// </summary>
        public static IIdProvider DefaultIDProvider { get; set; } = Instance;

        /// <summary>
        /// A static instance of the <see cref="IdProvider"/> class.
        /// </summary>
        public static IdProvider Instance => new IdProvider();

        /// <summary>
        /// Generates a new, unique ID.
        /// </summary>
        /// <returns>A unique ID, as a string.</returns>
        public string GetNewID() => Guid.NewGuid().ToString("B");
    }
}
