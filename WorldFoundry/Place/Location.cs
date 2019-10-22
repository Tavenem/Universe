using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.MathAndScience.Randomization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace WorldFoundry.Place
{
    /// <summary>
    /// A place in a universe, with a location that defines its position, and a shape that defines
    /// its extent.
    /// </summary>
    /// <remarks>
    /// Locations can exist in a hierarchy. Any location may contain other locations, and be
    /// contained by a location in turn. The relative positions of locations within the same
    /// hierarchy can be analyzed using the methods available on this class.
    /// </remarks>
    [Serializable]
    public class Location : IdItem, ISerializable, IEquatable<Location>
    {
        private List<Location>? _children;
        /// <summary>
        /// The child locations contained within this instance.
        /// </summary>
        public IEnumerable<Location> Children => _children ?? Enumerable.Empty<Location>();

        private Location? _parent;
        /// <summary>
        /// The parent location which contains this instance.
        /// </summary>
        public Location? Parent
        {
            get => _parent;
            set
            {
                _parent?._children?.Remove(this);
                _parent = value;
                Position = value?.GetLocalizedPosition(this) ?? Vector3.Zero;
                value?.AddChild(this);
                if (value != null && HasDefinedShape)
                {
                    foreach (var child in value.Children.Where(x => x != this
                        && x.HasDefinedShape
                        && x.Shape.ContainingRadius < Shape.ContainingRadius
                        && Contains(x.Position)))
                    {
                        child.Parent = this;
                    }
                }
            }
        }

        /// <summary>
        /// The position of this location relative to the center of its <see cref="Parent"/>.
        /// </summary>
        public virtual Vector3 Position
        {
            get => Shape.Position;
            set => Shape = Shape.GetCloneAtPosition(value);
        }

        private IShape? _shape;
        /// <summary>
        /// The shape of this location.
        /// </summary>
        public virtual IShape Shape
        {
            get => _shape ?? SinglePoint.Origin;
            set => _shape = value;
        }

        private protected virtual bool HasDefinedShape => !(_shape is null);

        /// <summary>
        /// Initializes a new instance of <see cref="Location"/>.
        /// </summary>
        protected Location() { }

        /// <summary>
        /// Initializes a new instance of <see cref="Location"/>.
        /// </summary>
        /// <param name="position">The position of the location relative to the center of its
        /// containing region.</param>
        public Location(Vector3 position) => Position = position;

        /// <summary>
        /// Initializes a new instance of <see cref="Location"/>.
        /// </summary>
        /// <param name="shape">The shape of the location.</param>
        /// <param name="children">A collection of child locations contained within this
        /// one.</param>
        public Location(IShape shape, List<Location>? children = null)
        {
            Shape = shape;
            _children = children?.Count > 0 ? children : null;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="Location"/>.
        /// </summary>
        /// <param name="parent">The location which contains this one.</param>
        public Location(Location? parent) => Parent = parent;

        /// <summary>
        /// Initializes a new instance of <see cref="Location"/>.
        /// </summary>
        /// <param name="parent">The location which contains this one.</param>
        /// <param name="position">The position of the location relative to the center of its
        /// <paramref name="parent"/>.</param>
        public Location(Location? parent, Vector3 position)
        {
            Position = position;
            Parent = parent;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="Location"/>.
        /// </summary>
        /// <param name="parent">The location which contains this one.</param>
        /// <param name="shape">The shape of the location.</param>
        public Location(Location? parent, IShape shape)
        {
            Shape = shape;
            Parent = parent;
        }

        private protected Location(string id, List<Location>? children)
        {
            Id = id;
            _children = children?.Count > 0 ? children : null;
            if (_children != null)
            {
                foreach (var child in _children)
                {
                    child._parent = this;
                }
            }
        }

        private protected Location(string id, IShape? shape, List<Location>? children)
        {
            Id = id;
            _shape = shape;
            _children = children?.Count > 0 ? children : null;
            if (_children != null)
            {
                foreach (var child in _children)
                {
                    child._parent = this;
                }
            }
        }

        private Location(SerializationInfo info, StreamingContext context) : this(
            (string)info.GetValue(nameof(Id), typeof(string)),
            (IShape?)info.GetValue(nameof(Shape), typeof(IShape)),
            (List<Location>?)info.GetValue(nameof(Children), typeof(List<Location>))) { }

        /// <summary>
        /// Determines whether the specified <see cref="Location"/> is contained within the current
        /// instance.
        /// </summary>
        /// <param name="other">The instance to compare with this one.</param>
        /// <returns>
        /// <see langword="true"/> if the specified <see cref="Location"/> is contained within this
        /// instance; otherwise, <see langword="false"/>.
        /// </returns>
        public virtual bool Contains(Location other)
        {
            if (GetCommonParent(other) != this)
            {
                return false;
            }
            return Shape.Intersects(other.Shape.GetCloneAtPosition(GetLocalizedPosition(other)));
        }

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
        public virtual bool Contains(Vector3 position) => Shape.IsPointWithin(position);

        /// <summary>
        /// Determines whether the specified <see cref="Location"/> instance is equal to this
        /// one.
        /// </summary>
        /// <param name="other">The <see cref="Location"/> instance to compare with this
        /// one.</param>
        /// <returns><see langword="true"/> if the specified <see cref="Location"/> instance
        /// is equal to this once; otherwise, <see langword="false"/>.</returns>
        public bool Equals(Location other)
            => !string.IsNullOrEmpty(Id) && string.Equals(Id, other?.Id, StringComparison.Ordinal);

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns><see langword="true"/> if the specified object is equal to the current object;
        /// otherwise, <see langword="false"/>.</returns>
        public override bool Equals(object obj) => obj is Location other && Equals(other);

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
        /// Gets a flattened enumeration of all descendants of this instance of the given type.
        /// </summary>
        /// <typeparam name="T">The type of child instances to retrieve.</typeparam>
        /// <returns>A flattened <see cref="IEnumerable{T}"/> of all descendant child <see
        /// cref="Location"/> instances of this one which are of the given type.</returns>
        public IEnumerable<T> GetAllChildren<T>() where T : Location
            => GetAllChildren().OfType<T>();

        /// <summary>
        /// Gets a flattened enumeration of all descendants of this instance of the given type.
        /// </summary>
        /// <param name="type">The type of child instances to retrieve.</param>
        /// <returns>A flattened <see cref="IEnumerable{T}"/> of all descendant child <see
        /// cref="Location"/> instances of this one which are of the given type.</returns>
        public IEnumerable<Location> GetAllChildren(Type type)
            => GetAllChildren().Where(x => type.IsAssignableFrom(x.GetType()));

        /// <summary>
        /// Finds a common location which contains both this instance and the given location.
        /// </summary>
        /// <param name="other">The other <see cref="Location"/>.</param>
        /// <returns>
        /// A common location which contains both this and the given location (may be either of
        /// them); or <see langword="null"/> if this instance and the given location do not have a
        /// common parent.
        /// </returns>
        public Location? GetCommonParent(Location? other)
        {
            if (other is null)
            {
                return null;
            }
            if (other == this)
            {
                return this;
            }
            // Handle common cases before performing the expensive calculation.
            if (Parent == other)
            {
                return Parent;
            }
            if (other.Parent == this)
            {
                return other.Parent;
            }
            if (Parent == other.Parent)
            {
                return Parent;
            }
            var secondPath = other.GetPathToLocation().ToList();
            return GetPathToLocation().TakeWhile((o, i) => secondPath.Count > i && o == secondPath[i]).LastOrDefault();
        }

        /// <summary>
        /// Determines the smallest child <see cref="Location"/> at any level of this instance's
        /// descendant hierarchy which fully contains the specified <see cref="Location"/> within
        /// its containing radius.
        /// </summary>
        /// <param name="other">The <see cref="Location"/> whose smallest containing <see
        /// cref="Location"/> is to be determined.</param>
        /// <returns>
        /// The smallest child <see cref="Location"/> at any level of this instance's descendant
        /// hierarchy which fully contains the specified <see cref="Location"/> within its
        /// containing radius, or this instance, if no child contains the position.
        /// </returns>
        public Location? GetContainingChild(Location other)
        {
            var min = Number.PositiveInfinity;
            Location? minItem = null;
            foreach (var item in GetAllChildren()
                .Where(x => x.Position.Distance(x.GetLocalizedPosition(other)) <= x.Shape.ContainingRadius - other.Shape.ContainingRadius))
            {
                if (item.Shape.ContainingRadius < min)
                {
                    min = item.Shape.ContainingRadius;
                    minItem = item;
                }
            }
            return minItem ?? (Contains(other) ? this : null);
        }

        /// <summary>
        /// Determines the smallest child <see cref="Location"/> at any level of this instance's
        /// descendant hierarchy which contains the specified <paramref name="position"/>.
        /// </summary>
        /// <param name="position">The position whose smallest containing <see cref="Location"/> is
        /// to be determined.</param>
        /// <returns>
        /// The smallest child <see cref="Location"/> at any level of this instance's descendant
        /// hierarchy which contains the specified <paramref name="position"/>, or this instance, if
        /// no child contains the position.
        /// </returns>
        public Location? GetContainingChild(Vector3 position)
        {
            var min = Number.PositiveInfinity;
            Location? minItem = null;
            foreach (var item in GetAllChildren()
                .Where(x => x.Shape.IsPointWithin(x.GetLocalizedPosition(this, position))))
            {
                if (item.Shape.ContainingRadius < min)
                {
                    min = item.Shape.ContainingRadius;
                    minItem = item;
                }
            }
            return minItem ?? (Contains(position) ? this : null);
        }

        /// <summary>
        /// Gets the distance from the given <paramref name="position"/> relative to the center of
        /// this instance to the given <paramref name="other"/> <see cref="Location"/>.
        /// </summary>
        /// <param name="position">A <see cref="Vector3"/> representing a position relative to the
        /// center of this location.</param>
        /// <param name="other">Another <see cref="Location"/>.</param>
        /// <returns>The distance between the given <paramref name="position"/> and the given <see
        /// cref="Location"/>, in meters; or <see
        /// cref="NeverFoundry.MathAndScience.Numerics.Number.PositiveInfinity"/>, if they do not
        /// share a common parent.</returns>
        public Number GetDistanceFromPositionTo(Vector3 position, Location other)
        {
            var pos = GetLocalizedPositionOrNull(other);
            return pos.HasValue
                ? Vector3.Distance(position, pos.Value)
                : Number.PositiveInfinity;
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
        /// cref="NeverFoundry.MathAndScience.Numerics.Number.PositiveInfinity"/>, if they do not
        /// share a common parent.</returns>
        public Number GetDistanceFromPositionTo(Vector3 localPosition, Location other, Vector3 otherPosition)
        {
            var pos = GetLocalizedPositionOrNull(other, otherPosition);
            return pos.HasValue
                ? Vector3.Distance(localPosition, pos.Value)
                : Number.PositiveInfinity;
        }

        /// <summary>
        /// Gets the distance from this instance to the given <paramref name="other"/> <see
        /// cref="Location"/>.
        /// </summary>
        /// <param name="other">Another <see cref="Location"/>.</param>
        /// <returns>The distance between this instance and the given <see cref="Location"/>, in
        /// meters; or <see cref="NeverFoundry.MathAndScience.Numerics.Number.PositiveInfinity"/>,
        /// if they do not share a common parent.</returns>
        public Number GetDistanceTo(Location other) => GetLocalizedPositionOrNull(other)?.Length() ?? Number.PositiveInfinity;

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>The hash code for this instance.</returns>
        public override int GetHashCode() => Id.GetHashCode();

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
        public Vector3 GetLocalizedPosition(Location other) => GetLocalizedPosition(other, Vector3.Zero);

        /// <summary>
        /// Attempts to find a random open space within this location with the given radius.
        /// </summary>
        /// <param name="radius">The radius of the space to find.</param>
        /// <param name="position">When this method returns, will be set to the position of the open
        /// space, if one was found; will be <see cref="Vector3.Zero"/> if no space was
        /// found.</param>
        /// <returns><see langword="true"/> if an open space was found; otherwise <see
        /// langword="false"/>.</returns>
        public bool TryGetOpenSpace(Number radius, out Vector3 position)
        {
            Vector3? pos = null;
            var insanityCheck = 0;
            do
            {
                pos = Randomizer.Instance.NextVector3Number(Shape.ContainingRadius);
                var shape = new Sphere(radius, pos.Value);
                if (GetAllChildren().Any(x => x.Shape.GetCloneAtPosition(GetLocalizedPosition(x)).Intersects(shape)))
                {
                    pos = null;
                }
                insanityCheck++;
            } while (!pos.HasValue && insanityCheck < 10000);
            position = pos ?? Vector3.Zero;
            return pos.HasValue;
        }

        /// <summary>Populates a <see cref="SerializationInfo"></see> with the data needed to
        /// serialize the target object.</summary>
        /// <param name="info">The <see cref="SerializationInfo"></see> to populate with
        /// data.</param>
        /// <param name="context">The destination (see <see cref="StreamingContext"></see>) for this
        /// serialization.</param>
        /// <exception cref="System.Security.SecurityException">The caller does not have the
        /// required permission.</exception>
        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(Id), Id);
            info.AddValue(nameof(Shape), _shape);
            info.AddValue(nameof(Children), _children);
        }

        internal Number GetDistanceSquaredTo(Location other) => GetLocalizedPosition(other).LengthSquared();

        [OnDeserialized]
        internal void OnDeserialized(StreamingContext _)
        {
            if (_children != null)
            {
                foreach (var child in _children)
                {
                    child._parent = this;
                }
            }
        }

        private protected void AddChild(Location location) => (_children ??= new List<Location>()).Add(location);

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
        private Vector3? GetLocalizedPositionOrNull(Location other, Vector3 position)
        {
            if (other is null)
            {
                return null;
            }
            if (other == this)
            {
                return position;
            }

            var parent = GetCommonParent(other);
            if (parent is null)
            {
                return null;
            }

            var current = other;
            while (current != parent)
            {
                position += current.Position;
                current = current.Parent!;
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
                target = target.Parent!;
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
        private Vector3? GetLocalizedPositionOrNull(Location other) => GetLocalizedPositionOrNull(other, Vector3.Zero);

#pragma warning disable IDE0060 // Remove unused parameter
        private Stack<Location>? GetPathToLocation(Stack<Location>? path = null)
        {
            (path ??= new Stack<Location>()).Push(this);
            return Parent?.GetPathToLocation(path) ?? path;
        }
#pragma warning restore IDE0060 // Remove unused parameter

        /// <summary>
        /// Indicates whether two <see cref="Location"/> instances are equal.
        /// </summary>
        /// <param name="left">The first instance.</param>
        /// <param name="right">The second instance.</param>
        /// <returns><see langword="true"/> if the instances are equal; otherwise, <see
        /// langword="false"/>.</returns>
        public static bool operator ==(Location? left, Location? right) => EqualityComparer<Location?>.Default.Equals(left, right);

        /// <summary>
        /// Indicates whether two <see cref="Location"/> instances are unequal.
        /// </summary>
        /// <param name="left">The first instance.</param>
        /// <param name="right">The second instance.</param>
        /// <returns><see langword="true"/> if the instances are unequal; otherwise, <see
        /// langword="false"/>.</returns>
        public static bool operator !=(Location? left, Location? right) => !(left == right);
    }
}
