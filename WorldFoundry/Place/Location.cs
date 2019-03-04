using MathAndScience.Numerics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WorldFoundry.Place
{
    /// <summary>
    /// A location in a universe.
    /// </summary>
    public class Location : ILocation, IEquatable<ILocation>
    {
        /// <summary>
        /// The containing region in which this location is found.
        /// </summary>
        public Region? ContainingRegion { get; private protected set; }

        /// <summary>
        /// A unique identifier for this entity.
        /// </summary>
        public string? Id { get; private set; }

        private Vector3 _position;
        /// <summary>
        /// The position of this location relative to the center of its <see cref="ContainingRegion"/>.
        /// </summary>
        public virtual Vector3 Position
        {
            get => _position;
            set => _position = value;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="Location"/>.
        /// </summary>
        private protected Location() { }

        /// <summary>
        /// Initializes a new instance of <see cref="Location"/>.
        /// </summary>
        /// <param name="position">The position of the location relative to the center of its
        /// containing region.</param>
        public Location(Vector3 position) => Position = position;

        /// <summary>
        /// Initializes a new instance of <see cref="Location"/>.
        /// </summary>
        /// <param name="containingRegion">The <see cref="Region"/> which contains this
        /// location.</param>
        /// <param name="position">The position of the location relative to the center of its
        /// <paramref name="containingRegion"/>.</param>
        public Location(Region? containingRegion, Vector3 position)
        {
            Position = position;
            ContainingRegion = containingRegion;
            containingRegion?.AddChild(this);
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns><see langword="true"/> if the specified object is equal to the current object;
        /// otherwise, <see langword="false"/>.</returns>
        public override bool Equals(object obj) => obj is ILocation other && Equals(other);

        /// <summary>
        /// Determines whether the specified <see cref="Location"/> instance is equal to this
        /// one.
        /// </summary>
        /// <param name="other">The <see cref="Location"/> instance to compare with this
        /// one.</param>
        /// <returns><see langword="true"/> if the specified <see cref="Location"/> instance
        /// is equal to this once; otherwise, <see langword="false"/>.</returns>
        public bool Equals(ILocation other)
            => !string.IsNullOrEmpty(Id) && string.Equals(Id, other?.Id, StringComparison.Ordinal);

        /// <summary>
        /// Finds a common <see cref="Region"/> which contains both this and the given location.
        /// </summary>
        /// <param name="other">The other <see cref="Location"/>.</param>
        /// <returns>
        /// A common <see cref="Region"/> which contains both this and the given location (may be
        /// either of them); or <see langword="null"/> if this instance and the given location do
        /// not have a common parent.
        /// </returns>
        public virtual Region? GetCommonContainingRegion(ILocation? other)
        {
            if (!(other is Location loc))
            {
                return null;
            }
            // Handle common cases before performing the expensive calculation.
            if (ContainingRegion == loc)
            {
                return ContainingRegion;
            }
            if (loc.ContainingRegion == this)
            {
                return loc.ContainingRegion;
            }
            if (ContainingRegion == loc.ContainingRegion)
            {
                return ContainingRegion;
            }
            var secondPath = loc.GetPathToLocation().ToList();
            return GetPathToLocation().TakeWhile((o, i) => secondPath.Count > i && o == secondPath[i]).LastOrDefault();
        }

        /// <summary>
        /// Gets the distance from the given <paramref name="position"/> relative to the center of
        /// this instance to the given <paramref name="other"/> <see cref="Location"/>.
        /// </summary>
        /// <param name="position">A <see cref="Vector3"/> representing a position relative to the
        /// center of this location.</param>
        /// <param name="other">Another <see cref="Location"/>.</param>
        /// <returns>The distance between the given <paramref name="position"/> and the given <see
        /// cref="Location"/>, in meters; or <see cref="double.PositiveInfinity"/>, if they do not
        /// share a common parent.</returns>
        public double GetDistanceFromPositionTo(Vector3 position, ILocation other)
        {
            var pos = GetLocalizedPositionOrNull(other);
            return pos.HasValue
                ? Vector3.Distance(position, pos.Value)
                : double.PositiveInfinity;
        }

        /// <summary>
        /// Gets the distance from the given position relative to the center of this instance to the
        /// given <paramref name="other"/> <see cref="Location"/>.
        /// </summary>
        /// <param name="localPosition">A <see cref="Vector3"/> representing a position relative to
        /// the center of this location.</param>
        /// <param name="other">Another <see cref="Location"/>.</param>
        /// <param name="otherPosition">A <see cref="Vector3"/> representing a position relative to
        /// the center of the <paramref name="other"/> location.</param>
        /// <returns>The distance between the given positions, in meters; or <see
        /// cref="double.PositiveInfinity"/>, if they do not share a common parent.</returns>
        public double GetDistanceFromPositionTo(Vector3 localPosition, ILocation other, Vector3 otherPosition)
        {
            var pos = GetLocalizedPositionOrNull(other, otherPosition);
            return pos.HasValue
                ? Vector3.Distance(localPosition, pos.Value)
                : double.PositiveInfinity;
        }

        /// <summary>
        /// Gets the distance from this instance to the given <paramref name="other"/> <see
        /// cref="Location"/>.
        /// </summary>
        /// <param name="other">Another <see cref="Location"/>.</param>
        /// <returns>The distance between this instance and the given <see cref="Location"/>, in
        /// meters; or <see cref="double.PositiveInfinity"/>, if they do not share a common
        /// parent.</returns>
        public double GetDistanceTo(ILocation other) => GetLocalizedPositionOrNull(other)?.Length() ?? double.PositiveInfinity;

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>The hash code for this instance.</returns>
        public override int GetHashCode() => Id?.GetHashCode() ?? 0;

        /// <summary>
        /// Translates the given <paramref name="position"/> relative to the center of the given
        /// <see cref="Location"/> to an equivalent position relative to the center of this
        /// instance.
        /// </summary>
        /// <param name="other">The <see cref="Location"/> in which <paramref name="position"/>
        /// currently represents a point relative to the center.</param>
        /// <param name="position">A position relative to the center of <paramref
        /// name="other"/>.</param>
        /// <returns>
        /// A <see cref="Vector3"/> giving the location of <paramref name="position"/> relative to
        /// the center of this instance; or <see cref="Vector3.Zero"/> if <paramref name="other"/>
        /// is <see langword="null"/> or does not share a common parent with this instance.
        /// </returns>
        public Vector3 GetLocalizedPosition(ILocation other, Vector3 position)
            => GetLocalizedPositionOrNull(other, position) ?? Vector3.Zero;

        /// <summary>
        /// Translates the center of the given <see cref="Location"/>
        /// to an equivalent position relative to the center of this instance.
        /// </summary>
        /// <param name="other">The <see cref="Location"/> whose center is to be translated.</param>
        /// <returns>
        /// A <see cref="Vector3"/> giving the center of the given <see cref="Location"/>
        /// relative to the center of this instance; or <see cref="Vector3.Zero"/> if <paramref
        /// name="other"/> is <see langword="null"/> or does not share a common parent with this
        /// instance.
        /// </returns>
        public Vector3 GetLocalizedPosition(ILocation other) => GetLocalizedPosition(other, Vector3.Zero);

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
        public virtual void SetNewContainingRegion(Region? region)
        {
            Position = region?.GetLocalizedPosition(this) ?? Vector3.Zero;
            ContainingRegion?.RemoveChild(this);
            ContainingRegion = region;
            region?.AddChild(this);
        }

        internal double GetDistanceSquaredTo(ILocation other) => GetLocalizedPosition(other).LengthSquared();

        internal virtual void Init() => Id = ItemIdProvider.DefaultIDProvider.GetNewID();

        /// <summary>
        /// Translates the given <paramref name="position"/> relative to the center of the given
        /// <see cref="Location"/> to an equivalent position relative to the center of this
        /// instance.
        /// </summary>
        /// <param name="other">The <see cref="Location"/> in which <paramref name="position"/>
        /// currently represents a point relative to the center.</param>
        /// <param name="position">A position relative to the center of <paramref
        /// name="other"/>.</param>
        /// <returns>
        /// A <see cref="Vector3"/> giving the location of <paramref name="position"/> relative to
        /// the center of this instance; or <see langword="null"/> if <paramref name="other"/>
        /// is <see langword="null"/> or does not share a common parent with this instance.
        /// </returns>
        private Vector3? GetLocalizedPositionOrNull(ILocation other, Vector3 position)
        {
            if (other == null)
            {
                return null;
            }
            if (other == this)
            {
                return position;
            }

            var parent = GetCommonContainingRegion(other);
            if (parent == null)
            {
                return null;
            }

            var current = other;
            while (current != parent)
            {
                position += current.Position;
                current = current.ContainingRegion!;
            }

            if (current == this)
            {
                return position;
            }

            var targetPosition = Vector3.Zero;
            var target = this;
            while (target != current)
            {
                targetPosition += target.Position;
                target = target.ContainingRegion!;
            }

            return position - targetPosition;
        }

        /// <summary>
        /// Translates the center of the given <see cref="Location"/>
        /// to an equivalent position relative to the center of this instance.
        /// </summary>
        /// <param name="other">The <see cref="Location"/> whose center is to be translated.</param>
        /// <returns>
        /// A <see cref="Vector3"/> giving the center of the given <see cref="Location"/>
        /// relative to the center of this instance; or <see langword="null"/> if <paramref
        /// name="other"/> is <see langword="null"/> or does not share a common parent with this
        /// instance.
        /// </returns>
        private Vector3? GetLocalizedPositionOrNull(ILocation other) => GetLocalizedPositionOrNull(other, Vector3.Zero);

        private protected virtual Stack<Region>? GetPathToLocation(Stack<Region>? path = null)
            => ContainingRegion?.GetPathToLocation(path) ?? path;
    }
}
