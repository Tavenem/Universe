using WorldFoundry.Space;

namespace WorldFoundry.Place
{
    /// <summary>
    /// A reference to a specific place within a universe.
    /// </summary>
    public class Place
    {
        /// <summary>
        /// The <see cref="CelestialEntity"/> where this <see cref="Place"/> is located.
        /// </summary>
        public CelestialEntity Entity { get; set; }

        /// <summary>
        /// Gets a deep clone of this <see cref="Place"/>.
        /// </summary>
        public virtual Place GetDeepClone() => new Place { Entity = Entity };
    }
}
