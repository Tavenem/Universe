namespace WorldFoundry.Place
{
    /// <summary>
    /// A <see cref="Place"/> which describes a region.
    /// </summary>
    public class Territory : Place
    {
        /// <summary>
        /// Gets a deep clone of this <see cref="Territory"/>.
        /// </summary>
        public Territory GetDeepCopy() => GetDeepClone() as Territory;

        /// <summary>
        /// Indicates whether this <see cref="Territory"/> includes the given <see cref="Place"/>.
        /// </summary>
        public virtual bool Includes(Place place) => Entity == place?.Entity;

        /// <summary>
        /// Indicates whether this <see cref="Territory"/> overlaps the given <see cref="Place"/>.
        /// </summary>
        public virtual bool Overlaps(Place place) => Entity == place?.Entity;
    }
}
