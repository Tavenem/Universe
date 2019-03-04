using MathAndScience.Numerics;

namespace WorldFoundry.Place
{
    /// <summary>
    /// A location in a universe.
    /// </summary>
    public interface ILocation
    {
        /// <summary>
        /// The containing region in which this location is found.
        /// </summary>
        Region? ContainingRegion { get; }

        /// <summary>
        /// A unique identifier for this entity.
        /// </summary>
        string? Id { get; }

        /// <summary>
        /// The position of this location relative to the center of its <see cref="ContainingRegion"/>.
        /// </summary>
        Vector3 Position { get; set; }

        /// <summary>
        /// Finds a common <see cref="Region"/> which contains both this and the given location.
        /// </summary>
        /// <param name="other">The other <see cref="ILocation"/>.</param>
        /// <returns>
        /// A common <see cref="Region"/> which contains both this and the given location (may be
        /// either of them); or <see langword="null"/> if this instance and the given location do
        /// not have a common parent.
        /// </returns>
        Region? GetCommonContainingRegion(ILocation? other);

        /// <summary>
        /// Gets the distance from the given <paramref name="position"/> relative to the center of
        /// this instance to the given <paramref name="other"/> <see cref="ILocation"/>.
        /// </summary>
        /// <param name="position">A <see cref="Vector3"/> representing a position relative to the
        /// center of this location.</param>
        /// <param name="other">Another <see cref="ILocation"/>.</param>
        /// <returns>The distance between the given <paramref name="position"/> and the given <see
        /// cref="ILocation"/>, in meters; or zero, if they do not share a common parent.</returns>
        double GetDistanceFromPositionTo(Vector3 position, ILocation other);

        /// <summary>
        /// Gets the distance from this instance to the given <paramref name="other"/> <see
        /// cref="ILocation"/>.
        /// </summary>
        /// <param name="other">Another <see cref="ILocation"/>.</param>
        /// <returns>The distance between this instance and the given <see cref="ILocation"/>, in
        /// meters; or zero, if they do not share a common parent.</returns>
        double GetDistanceTo(ILocation other);

        /// <summary>
        /// Translates the center of the given <see cref="ILocation"/>
        /// to an equivalent position relative to the center of this instance.
        /// </summary>
        /// <param name="other">The <see cref="ILocation"/> whose center is to be translated.</param>
        /// <returns>
        /// A <see cref="Vector3"/> giving the center of the given <see cref="ILocation"/>
        /// relative to the center of this instance; or <see cref="Vector3.Zero"/> if <paramref
        /// name="other"/>
        /// is <see langword="null"/> or does not share a common parent with this instance.
        /// </returns>
        Vector3 GetLocalizedPosition(ILocation other);

        /// <summary>
        /// Translates the given <paramref name="position"/> relative to the center of the given
        /// <see cref="ILocation"/> to an equivalent position relative to the center of this
        /// instance.
        /// </summary>
        /// <param name="other">The <see cref="ILocation"/> in which <paramref name="position"/>
        /// currently represents a point relative to the center.</param>
        /// <param name="position">A position relative to the center of <paramref
        /// name="other"/>.</param>
        /// <returns>
        /// A <see cref="Vector3"/> giving the location of <paramref name="position"/> relative to
        /// the center of this instance; or <see cref="Vector3.Zero"/> if <paramref name="other"/>
        /// is <see langword="null"/> or does not share a common parent with this instance.
        /// </returns>
        Vector3 GetLocalizedPosition(ILocation other, Vector3 position);

        /// <summary>
        /// Performs the behind-the-scenes work necessary to transfer a <see cref="Location"/>
        /// to a new <see cref="ContainingRegion"/>.
        /// </summary>
        /// <param name="region">The <see cref="Region"/> which will be the new containing region of
        /// this instance; or <see langword="null"/> to clear <see
        /// cref="ContainingRegion"/>.</param>
        /// <remarks>
        /// If the new containing region is part of the same hierarchy as this instance, its current
        /// absolute position will be preserved, and translated into the correct local relative <see
        /// cref="Position"/>. Otherwise, they will be reset to <see cref="Vector3.Zero"/>.
        /// </remarks>
        void SetNewContainingRegion(Region? region);
    }
}