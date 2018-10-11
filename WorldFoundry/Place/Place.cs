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

        /// <summary>
        /// Determines whether the specified <see cref="Place"/> is equivalent to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>
        /// <see langword="true"/> if the specified <see cref="Place"/> is equivalent to the
        /// current object; otherwise, <see langword="false"/>.
        /// </returns>
        public virtual bool Matches(Place obj) => obj != null && Entity == obj.Entity;
    }
}
