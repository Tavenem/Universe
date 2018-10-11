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
        /// <param name="place">A <see cref="Place"/> to test for inclusion.</param>
        public virtual bool Includes(Place place) => Entity == place?.Entity;

        /// <summary>
        /// Determines whether the specified <see cref="Place"/> is equivalent to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>
        /// <see langword="true"/> if the specified <see cref="Place"/> is equivalent to the
        /// current object; otherwise, <see langword="false"/>.
        /// </returns>
        public override bool Matches(Place obj) => obj is Territory territory && base.Matches(territory);

        /// <summary>
        /// Indicates whether this <see cref="Territory"/> overlaps the given <see cref="Place"/>.
        /// </summary>
        /// <param name="place">The <see cref="Place"/> to test for overlap.</param>
        public virtual bool Overlaps(Place place) => Entity == place?.Entity;
    }
}
