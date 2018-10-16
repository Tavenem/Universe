using ExtensionLib;
using System;
using System.Collections.Generic;
using System.Linq;
using MathAndScience.Numerics;
using WorldFoundry.Space;

namespace WorldFoundry.Place
{
    /// <summary>
    /// A location in a universe.
    /// </summary>
    public class Location
    {
        /// <summary>
        /// The <see cref="Space.CelestialEntity"/> which represents this location (if any).
        /// </summary>
        public CelestialEntity CelestialEntity { get; set; }

        private readonly Lazy<List<Location>> _children = new Lazy<List<Location>>();
        /// <summary>
        /// The child locations contained within this one.
        /// </summary>
        public IEnumerable<Location> Children => _children.Value;

        /// <summary>
        /// The parent location in which this one is found.
        /// </summary>
        public Location Parent { get; private protected set; }

        /// <summary>
        /// The position of this location relative to the center of its <see cref="Parent"/>.
        /// </summary>
        public virtual Vector3 Position { get; set; }

        /// <summary>
        /// Initializes a new instance of <see cref="Location"/>.
        /// </summary>
        private protected Location() { }

        /// <summary>
        /// Initializes a new instance of <see cref="Location"/>.
        /// </summary>
        /// <param name="celestialEntity">The <see cref="Space.CelestialEntity"/> which represents
        /// this location (may be <see langword="null"/>).</param>
        /// <param name="position">The position of the location relative to the center of its parent
        /// entity.</param>
        public Location(CelestialEntity celestialEntity, Vector3 position)
        {
            CelestialEntity = celestialEntity;
            Position = position;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="Location"/>.
        /// </summary>
        /// <param name="celestialEntity">The <see cref="Space.CelestialEntity"/> which represents
        /// this location (may be <see langword="null"/>).</param>
        /// <param name="parent">The parent location in which this one is found.</param>
        /// <param name="position">The position of the location relative to the center of its parent
        /// <paramref name="parent"/>.</param>
        public Location(CelestialEntity celestialEntity, Location parent, Vector3 position)
        {
            CelestialEntity = celestialEntity;
            Position = position;
            Parent = parent;
            Parent?.AddChild(this);
        }

        /// <summary>
        /// Determines whether the specified <see cref="Location"/> is contained within the current
        /// instance.
        /// </summary>
        /// <param name="other">The instance to compare with this one.</param>
        /// <returns>
        /// <see langword="true"/> if the specified <see cref="Location"/> is contained within this
        /// instance; otherwise, <see langword="false"/>.
        /// </returns>
        public virtual bool Contains(Location other) => GetCommonParent(other) == this;

        /// <summary>
        /// Determines whether the specified <paramref name="position"/> is contained within the
        /// current instance.
        /// </summary>
        /// <param name="position">a <see cref="Vector3"/> to check for inclusion in this <see
        /// cref="Location"/>.</param>
        /// <returns>
        /// <see langword="true"/> if the specified <paramref name="position"/> is contained within
        /// this instance; otherwise, <see langword="false"/>.
        /// </returns>
        public virtual bool Contains(Vector3 position) => Position == position;

        /// <summary>
        /// Gets a flattened enumeration of all descendants of this instance.
        /// </summary>
        /// <returns>A flattened <see cref="IEnumerable{T}"/> of all descendant child <see
        /// cref="Location"/> instances of this one.</returns>
        public IEnumerable<Location> GetAllChildren()
        {
            foreach (var child in Children)
            {
                yield return child;

                foreach (var sub in child.GetAllChildren())
                {
                    yield return sub;
                }
            }
        }

        /// <summary>
        /// Finds a common <see cref="Location"/> which contains both this and the given location.
        /// </summary>
        /// <param name="other">The other <see cref="Location"/>.</param>
        /// <returns>
        /// A common <see cref="Location"/> which contains both this and the given locations (may be
        /// either of them); or <see langword="null"/> if this instance and the given location do
        /// not have a common parent.
        /// </returns>
        public Location GetCommonParent(Location other)
        {
            if (other == null)
            {
                return null;
            }
            // Handle common cases before performing the expensive calculation.
            if (other == this)
            {
                return this;
            }
            if (Parent == other)
            {
                return other;
            }
            if (other.Parent == this)
            {
                return this;
            }
            if (Parent == other.Parent)
            {
                return Parent;
            }
            var secondPath = other.GetPathToLocation().ToList();
            return GetPathToLocation().TakeWhile((o, i) => secondPath.Count > i && o == secondPath[i]).LastOrDefault();
        }

        /// <summary>
        /// Determines the nearest containing <see cref="Region"/> in this instance's parent
        /// hierarchy which contains this instance.
        /// </summary>
        /// <returns>
        /// The nearest containing <see cref="Region"/> in this instance's parent hierarchy which
        /// contains this instance; or <see langword="null"/> if this instance is not contained in
        /// any parent <see cref="Region"/>.
        /// </returns>
        public Region GetContainingParent()
            => Parent?.GetContainingParent(Parent.GetLocalizedPosition(this));

        /// <summary>
        /// Determines the nearest containing <see cref="Region"/> in this instance's parent
        /// hierarchy which contains the specified <paramref name="position"/>.
        /// </summary>
        /// <param name="position">The position whose containing <see cref="Region"/> is to be
        /// determined.</param>
        /// <returns>
        /// The nearest containing <see cref="Region"/> in this instance's parent hierarchy which
        /// contains the specified <paramref name="position"/>; or <see langword="null"/> if the
        /// position is not contained in any parent <see cref="Region"/>.
        /// </returns>
        public virtual Region GetContainingParent(Vector3 position)
            => Parent?.GetContainingParent(Parent.GetLocalizedPosition(this, position));

        /// <summary>
        /// Gets the distance from the given <paramref name="position"/> relative to the center of
        /// this instance to the given <paramref name="other"/> <see cref="Location"/>.
        /// </summary>
        /// <param name="position">A <see cref="Vector3"/> representing a position relative to the
        /// center of this location.</param>
        /// <param name="other">Another <see cref="Location"/>.</param>
        /// <returns>The distance between the given <paramref name="position"/> and the given <see
        /// cref="Location"/>, in meters; or zero, if they do not share a common parent.</returns>
        public double GetDistanceFromPositionTo(Vector3 position, Location other)
            => Vector3.Distance(position, GetLocalizedPosition(other));

        /// <summary>
        /// Gets the distance from this instance to the given <paramref name="other"/> <see
        /// cref="Location"/>.
        /// </summary>
        /// <param name="other">Another <see cref="Location"/>.</param>
        /// <returns>The distance between this instance and the given <see cref="Location"/>, in
        /// meters; or zero, if they do not share a common parent.</returns>
        public double GetDistanceTo(Location other) => GetLocalizedPosition(other).Length();

        /// <summary>
        /// Gets the distance from this instance to the location of the given <paramref
        /// name="celestialEntity"/>.
        /// </summary>
        /// <param name="celestialEntity">A <see cref="Space.CelestialEntity"/>.</param>
        /// <returns>The distance between this instance and the given <paramref
        /// name="celestialEntity"/>, in meters; or zero, if they do not share a common
        /// parent.</returns>
        public double GetDistanceTo(CelestialEntity celestialEntity) => GetDistanceTo(celestialEntity.Location);

        /// <summary>
        /// Gets a deep clone of this <see cref="Place"/>.
        /// </summary>
        public virtual Location GetDeepClone() => new Location(CelestialEntity, Parent, Position);

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
        public Vector3 GetLocalizedPosition(Location other, Vector3 position)
        {
            if (other == null)
            {
                return Vector3.Zero;
            }
            if (other == this)
            {
                return position;
            }

            var parent = GetCommonParent(other);
            if (parent == null)
            {
                return Vector3.Zero;
            }

            var current = other;
            while (current != parent)
            {
                position += current.Position;
                current = current.Parent;
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
                target = target.Parent;
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
        /// relative to the center of this instance; or <see cref="Vector3.Zero"/> if <paramref
        /// name="other"/>
        /// is <see langword="null"/> or does not share a common parent with this instance.
        /// </returns>
        public Vector3 GetLocalizedPosition(Location other) => GetLocalizedPosition(other, Vector3.Zero);

        /// <summary>
        /// Determines the smallest child <see cref="Region"/> at any level of this instance's
        /// descendant hierarchy which contains the specified <paramref name="position"/>.
        /// </summary>
        /// <param name="position">The position whose smallest containing <see cref="Region"/> is to
        /// be determined.</param>
        /// <returns>
        /// The smallest child <see cref="Region"/> at any level of this instance's descendant
        /// hierarchy which contains the specified <paramref name="position"/>, or this instance, if
        /// no child contains the position.
        /// </returns>
        public Region GetContainingChild(Vector3 position)
            => GetAllChildren().OfType<Region>()
            .Where(x => x.Shape.IsPointWithin(x.GetLocalizedPosition(this, position)))
            .ItemWithMin(x => x.Shape.ContainingRadius)
            ?? (this is Region region && Contains(position) ? region : null);

        /// <summary>
        /// Determines the smallest child <see cref="Region"/> at any level of this instance's
        /// descendant hierarchy which fully contains the specified <see cref="Location"/> within
        /// its containing radius.
        /// </summary>
        /// <param name="other">The <see cref="Location"/> whose smallest containing <see
        /// cref="Region"/> is to be determined.</param>
        /// <returns>
        /// The smallest child <see cref="Region"/> at any level of this instance's descendant
        /// hierarchy which fully contains the specified <see cref="Location"/> within its
        /// containing radius, or this instance, if no child contains the position.
        /// </returns>
        public Region GetContainingChild(Location other)
            => GetAllChildren().OfType<Region>()
            .Where(x => Vector3.Distance(x.Position, x.GetLocalizedPosition(other)) <= x.Shape.ContainingRadius - (other is Region r ? r.Shape.ContainingRadius : 0))
            .ItemWithMin(x => x.Shape.ContainingRadius)
            ?? (this is Region region && Contains(other) ? region : null);

        /// <summary>
        /// Performs the behind-the-scenes work necessary to transfer a <see cref="Location"/>
        /// to a new <see cref="Parent"/> in the hierarchy.
        /// </summary>
        /// <param name="location">The <see cref="Location"/> which will be the new parent of this
        /// instance; or <see langword="null"/> to clear <see cref="Parent"/>.</param>
        /// <remarks>
        /// If the new parent is part of the same hierarchy as this instance, its current absolute
        /// position will be preserved, and translated into the correct local relative <see
        /// cref="Position"/>. Otherwise, they will be reset to <see cref="Vector3.Zero"/>.
        /// </remarks>
        public void SetNewParent(Location location)
        {
            Position = location?.GetLocalizedPosition(this) ?? Vector3.Zero;
            if (Parent?._children.IsValueCreated == true)
            {
                Parent._children.Value.Remove(this);
            }
            Parent = location;
            Parent.AddChild(this);
        }

        internal void AddChild(Location location)
        {
            if (location is Region region)
            {
                foreach (var child in Children.Where(x => region.Contains(x.Position) && (!(x is Region r) || r.Shape.ContainingRadius < region.Shape.ContainingRadius)))
                {
                    child.SetNewParent(region);
                }
            }
            _children.Value.Add(location);
        }

        private Stack<Location> GetPathToLocation(Stack<Location> path = null)
        {
            (path ?? (path = new Stack<Location>())).Push(this);
            return Parent?.GetPathToLocation(path) ?? path;
        }
    }
}
