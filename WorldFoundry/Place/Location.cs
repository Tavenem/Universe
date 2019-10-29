using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.MathAndScience.Randomization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Threading.Tasks;
using WorldFoundry.Space;

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
    public class Location : IdItem, ISerializable
    {
        private Location? _parent;

        /// <summary>
        /// The id of the parent location which contains this instance, if any.
        /// </summary>
        public string? ParentId { get; private set; }

        /// <summary>
        /// The position of this location relative to the center of its parent.
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
        public Location(IShape shape) => Shape = shape;

        /// <summary>
        /// Initializes a new instance of <see cref="Location"/>.
        /// </summary>
        /// <param name="parentId">The id of the location which contains this one.</param>
        public Location(string? parentId) => ParentId = parentId;

        /// <summary>
        /// Initializes a new instance of <see cref="Location"/>.
        /// </summary>
        /// <param name="parentId">The id of the location which contains this one.</param>
        /// <param name="position">The position of the location relative to the center of its
        /// parent.</param>
        public Location(string? parentId, Vector3 position)
        {
            Position = position;
            ParentId = parentId;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="Location"/>.
        /// </summary>
        /// <param name="parentId">The id of the location which contains this one.</param>
        /// <param name="shape">The shape of the location.</param>
        public Location(string? parentId, IShape shape)
        {
            Shape = shape;
            ParentId = parentId;
        }

        private protected Location(string id, string? parentId)
        {
            Id = id;
            ParentId = parentId;
        }

        private protected Location(string id, IShape? shape, string? parentId)
        {
            Id = id;
            _shape = shape;
            ParentId = parentId;
        }

        private Location(SerializationInfo info, StreamingContext context) : this(
            (string)info.GetValue(nameof(Id), typeof(string)),
            (IShape?)info.GetValue(nameof(Shape), typeof(IShape)),
            (string?)info.GetValue(nameof(ParentId), typeof(string))) { }

        /// <summary>
        /// Determines whether the specified <see cref="Location"/> is contained within the current
        /// instance.
        /// </summary>
        /// <param name="other">The instance to compare with this one.</param>
        /// <returns>
        /// <see langword="true"/> if the specified <see cref="Location"/> is contained within this
        /// instance; otherwise, <see langword="false"/>.
        /// </returns>
        public virtual async ValueTask<bool> ContainsAsync(Location other)
        {
            if (await GetCommonParentAsync(other).ConfigureAwait(false) != this)
            {
                return false;
            }
            return Shape.Intersects(other.Shape.GetCloneAtPosition(await GetLocalizedPositionAsync(other).ConfigureAwait(false)));
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
        public virtual ValueTask<bool> ContainsAsync(Vector3 position) => new ValueTask<bool>(Shape.IsPointWithin(position));

        /// <summary>
        /// Removes this location and all contained children from the data store.
        /// </summary>
        public override async Task DeleteAsync()
        {
            await foreach (var child in GetChildrenAsync())
            {
                await child.DeleteAsync().ConfigureAwait(false);
            }
            await base.DeleteAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Gets a flattened enumeration of all descendants of this instance.
        /// </summary>
        /// <returns>A flattened <see cref="IAsyncEnumerable{T}"/> of all descendant child <see
        /// cref="Location"/> instances of this one.</returns>
        public async IAsyncEnumerable<Location> GetAllChildrenAsync()
        {
            await foreach (var child in GetChildrenAsync())
            {
                yield return child;

                await foreach (var sub in child.GetAllChildrenAsync())
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
        public IAsyncEnumerable<T> GetAllChildrenAsync<T>() where T : Location
            => GetAllChildrenAsync().OfType<T>();

        /// <summary>
        /// Gets a flattened enumeration of all descendants of this instance of the given type.
        /// </summary>
        /// <param name="type">The type of child instances to retrieve.</param>
        /// <returns>A flattened <see cref="IEnumerable{T}"/> of all descendant child <see
        /// cref="Location"/> instances of this one which are of the given type.</returns>
        public IAsyncEnumerable<Location> GetAllChildrenAsync(Type type)
            => GetAllChildrenAsync().Where(x => type.IsAssignableFrom(x.GetType()));

        /// <summary>
        /// Enumerates the children of this instance.
        /// </summary>
        /// <returns>An <see cref="IEnumerable{T}"/> of child <see cref="Location"/> instances of
        /// this one.</returns>
        public virtual IAsyncEnumerable<Location> GetChildrenAsync() => DataStore.GetItemsWhereAsync<Location>(x => x.ParentId == Id);

        /// <summary>
        /// Finds a common location which contains both this instance and the given location.
        /// </summary>
        /// <param name="other">The other <see cref="Location"/>.</param>
        /// <returns>
        /// A common location which contains both this and the given location (may be either of
        /// them); or <see langword="null"/> if this instance and the given location do not have a
        /// common parent.
        /// </returns>
        public async Task<Location?> GetCommonParentAsync(Location? other)
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
            if (string.Equals(ParentId, other.Id, StringComparison.Ordinal))
            {
                return other;
            }
            if (string.Equals(other.ParentId, Id, StringComparison.Ordinal))
            {
                return this;
            }
            if (string.Equals(ParentId, other.ParentId, StringComparison.Ordinal))
            {
                return await GetParentAsync().ConfigureAwait(false);
            }
            var secondPath = (await other.GetPathToLocationAsync().ConfigureAwait(false)).ToList();
            return (await GetPathToLocationAsync().ConfigureAwait(false)).TakeWhile((o, i) => secondPath.Count > i && o == secondPath[i]).LastOrDefault();
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
        public async Task<Location?> GetContainingChildAsync(Location other)
        {
            var min = Number.PositiveInfinity;
            Location? minItem = null;
            await foreach (var item in GetAllChildrenAsync()
                .WhereAwait(async x => x.Position.Distance(await x.GetLocalizedPositionAsync(other).ConfigureAwait(false)) <= x.Shape.ContainingRadius - other.Shape.ContainingRadius))
            {
                if (item.Shape.ContainingRadius < min)
                {
                    min = item.Shape.ContainingRadius;
                    minItem = item;
                }
            }
            return minItem ?? (await ContainsAsync(other).ConfigureAwait(false) ? this : null);
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
        public async Task<Location?> GetContainingChildAsync(Vector3 position)
        {
            var min = Number.PositiveInfinity;
            Location? minItem = null;
            await foreach (var item in GetAllChildrenAsync()
                .WhereAwait(async x => x.Shape.IsPointWithin(await x.GetLocalizedPositionAsync(this, position).ConfigureAwait(false))))
            {
                if (item.Shape.ContainingRadius < min)
                {
                    min = item.Shape.ContainingRadius;
                    minItem = item;
                }
            }
            return minItem ?? (await ContainsAsync(position).ConfigureAwait(false) ? this : null);
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
        public async Task<Number> GetDistanceFromPositionToAsync(Vector3 position, Location other)
        {
            var pos = await GetLocalizedPositionOrNullAsync(other).ConfigureAwait(false);
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
        public async Task<Number> GetDistanceFromPositionToAsync(Vector3 localPosition, Location other, Vector3 otherPosition)
        {
            var pos = await GetLocalizedPositionOrNullAsync(other, otherPosition).ConfigureAwait(false);
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
        public async Task<Number> GetDistanceToAsync(Location other) => (await GetLocalizedPositionOrNullAsync(other).ConfigureAwait(false))?.Length() ?? Number.PositiveInfinity;

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
        public async Task<Vector3> GetLocalizedPositionAsync(Location other, Vector3 position)
            => await GetLocalizedPositionOrNullAsync(other, position).ConfigureAwait(false) ?? Vector3.Zero;

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
        public Task<Vector3> GetLocalizedPositionAsync(Location other) => GetLocalizedPositionAsync(other, Vector3.Zero);

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
            info.AddValue(nameof(ParentId), ParentId);
        }

        /// <summary>
        /// Gets the parent location which contains this one, if any.
        /// </summary>
        /// <returns>The parent location which contains this one, if any.</returns>
        public async Task<Location?> GetParentAsync()
        {
            _parent ??= await DataStore.GetItemAsync<Location>(ParentId).ConfigureAwait(false);
            return _parent;
        }

        /// <summary>
        /// Sets the id of the parent location which contains this one, if any.
        /// </summary>
        /// <param name="id">The id of the parent location which contains this one. May be <see
        /// langword="null"/>.</param>
        public async Task SetParentAsync(string? id)
        {
            ParentId = id;
            _parent = null;
            var parent = await GetParentAsync().ConfigureAwait(false);
            _parent = parent;
            if (parent is null)
            {
                Position = Vector3.Zero;
            }
            else
            {
                Position = await parent.GetLocalizedPositionAsync(this).ConfigureAwait(false);
                if (HasDefinedShape)
                {
                    await foreach (var child in parent.GetChildrenAsync()
                        .Where(x => x != this
                            && x.HasDefinedShape
                            && x.Shape.ContainingRadius < Shape.ContainingRadius)
                        .WhereAwait(x => ContainsAsync(x.Position)))
                    {
                        await child.SetParentAsync(Id).ConfigureAwait(false);
                    }
                }
            }
        }

        internal async Task<Number> GetDistanceSquaredToAsync(Location other) => (await GetLocalizedPositionAsync(other).ConfigureAwait(false)).LengthSquared();

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
        private async Task<Vector3?> GetLocalizedPositionOrNullAsync(Location other, Vector3 position)
        {
            if (other is null)
            {
                return null;
            }
            if (other == this)
            {
                return position;
            }

            var parent = await GetCommonParentAsync(other).ConfigureAwait(false);
            if (parent is null)
            {
                return null;
            }

            var current = other;
            while (current != parent)
            {
                position += current.Position;
                current = (await current.GetParentAsync().ConfigureAwait(false))!;
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
                target = (await target.GetParentAsync().ConfigureAwait(false))!;
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
        private Task<Vector3?> GetLocalizedPositionOrNullAsync(Location other) => GetLocalizedPositionOrNullAsync(other, Vector3.Zero);

        private async Task<Stack<Location>?> GetPathToLocationAsync(Stack<Location>? path = null)
        {
            (path ??= new Stack<Location>()).Push(this);
            var parent = await GetParentAsync().ConfigureAwait(false);
            if (parent is null)
            {
                return path;
            }
            else
            {
                return await parent.GetPathToLocationAsync(path).ConfigureAwait(false) ?? path;
            }
        }

        /// <summary>
        /// Attempts to find a randomly positioned open space within this location with the given
        /// radius.
        /// </summary>
        /// <param name="radius">The radius of the space to find.</param>
        /// <returns>An open space within this location with the given radius; or <see
        /// langword="null"/> if no such open position could be found.</returns>
        private protected async Task<Vector3?> GetOpenSpaceAsync(Number radius)
        {
            Vector3? pos = null;
            var insanityCheck = 0;
            do
            {
                pos = Randomizer.Instance.NextVector3Number(Shape.ContainingRadius);
                var shape = new Sphere(radius, pos.Value);
                if (await GetAllChildrenAsync().AnyAwaitAsync(async x => x.Shape.GetCloneAtPosition(await GetLocalizedPositionAsync(x).ConfigureAwait(false)).Intersects(shape)).ConfigureAwait(false))
                {
                    pos = null;
                }
                insanityCheck++;
            } while (!pos.HasValue && insanityCheck < 10000);
            return pos;
        }
    }
}
