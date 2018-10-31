using System;

namespace WorldFoundry
{
    /// <summary>
    /// Provides a means of generating unique IDs.
    /// </summary>
    public class CelestialIDProvider : ICelestialIDProvider
    {
        /// <summary>
        /// The current default <see cref="ICelestialIDProvider"/> instance. Starts as a static
        /// instance of this class.
        /// <seealso cref="Instance"/>
        /// </summary>
        public static ICelestialIDProvider DefaultIDProvider { get; set; } = Instance;

        /// <summary>
        /// A static instance of the <see cref="CelestialIDProvider"/> class.
        /// </summary>
        public static CelestialIDProvider Instance => new CelestialIDProvider();

        /// <summary>
        /// Generates a new, unique ID.
        /// </summary>
        /// <returns>A unique ID, as a string.</returns>
        public string GetNewID() => Guid.NewGuid().ToString("B");
    }
}
