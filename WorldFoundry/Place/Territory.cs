namespace WorldFoundry.Place
{
    /// <summary>
    /// A <see cref="Place"/> which describes a region.
    /// </summary>
    public class Territory : Place
    {
        /// <summary>
        /// Indicates whether this <see cref="Territory"/> includes the given <see cref="Place"/>.
        /// </summary>
        public virtual bool Includes(Place place) => Entity == place?.Entity;
    }
}
