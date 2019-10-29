using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Threading.Tasks;

namespace WorldFoundry.Place
{
    /// <summary>
    /// A collection of locations which define a conceptually unified area (though they may not form
    /// a contiguous region).
    /// </summary>
    [Serializable]
    public class Territory : Location
    {
        private List<string>? _childIds;
        /// <summary>
        /// The ids of the child locations contained within this instance.
        /// </summary>
        public IEnumerable<string> ChildIds => _childIds ?? Enumerable.Empty<string>();

        /// <summary>
        /// Initializes a new instance of <see cref="Territory"/>.
        /// </summary>
        public Territory() { }

        /// <summary>
        /// Initializes a new instance of <see cref="Territory"/>.
        /// </summary>
        /// <param name="shape">The shape of the location.</param>
        public Territory(IShape shape) : base(shape) { }

        private Territory(string id, IShape? shape, List<string>? childIds = null, string? parentId = null) : base(id, shape, parentId)
            => _childIds = childIds;

        private Territory(SerializationInfo info, StreamingContext context) : this(
            (string)info.GetValue(nameof(Id), typeof(string)),
            (IShape)info.GetValue(nameof(Shape), typeof(IShape)),
            (List<string>?)info.GetValue(nameof(_childIds), typeof(List<string>)),
            (string?)info.GetValue(nameof(ParentId), typeof(string))) { }

        /// <summary>
        /// Adds the given <paramref name="locations"/> to this instance.
        /// </summary>
        /// <param name="locations">The <see cref="Location"/> instances to add.</param>
        /// <returns>This instance.</returns>
        public async Task<Territory> AddLocationsAsync(IEnumerable<Location> locations)
        {
            foreach (var location in locations)
            {
                (_childIds ??= new List<string>()).Add(location.Id);
            }
            await CalculateShapeAsync().ConfigureAwait(false);
            return this;
        }

        /// <summary>
        /// Adds the given <paramref name="locations"/> to this instance.
        /// </summary>
        /// <param name="locations">One or more <see cref="Location"/> instances to add.</param>
        /// <returns>This instance.</returns>
        public Task<Territory> AddLocationsAsync(params Location[] locations)
            => AddLocationsAsync(locations.AsEnumerable());

        /// <summary>
        /// Removes this location and all contained children from the data store.
        /// </summary>
        public override async Task DeleteAsync()
        {
            await foreach (var child in base.GetChildrenAsync())
            {
                await child.DeleteAsync().ConfigureAwait(false);
            }
            await base.DeleteAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Enumerates the children of this instance.
        /// </summary>
        /// <returns>An <see cref="IEnumerable{T}"/> of child <see cref="Location"/> instances of
        /// this one.</returns>
        public override IAsyncEnumerable<Location> GetChildrenAsync() => _childIds is null
            ? AsyncEnumerable.Empty<Location>()
            : DataStore.GetItemsWhereAsync<Location>(x => _childIds.Contains(x.Id));

        /// <summary>
        /// Determines whether the specified <see cref="Location"/> is contained within the current
        /// instance.
        /// </summary>
        /// <param name="other">The instance to compare with this one.</param>
        /// <returns>
        /// <see langword="true"/> if the specified <see cref="Location"/> is contained within this
        /// instance; otherwise, <see langword="false"/>.
        /// </returns>
        public override async ValueTask<bool> ContainsAsync(Location other)
        {
            if (await GetCommonParentAsync(other).ConfigureAwait(false) != this)
            {
                return false;
            }
            return await GetChildrenAsync().AnyAwaitAsync(x => x.ContainsAsync(other)).ConfigureAwait(false);
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
        public override ValueTask<bool> ContainsAsync(Vector3 position)
            => GetChildrenAsync().AnyAsync(x => x.Shape.IsPointWithin(position));

        /// <summary>Populates a <see cref="SerializationInfo"></see> with the data needed to
        /// serialize the target object.</summary>
        /// <param name="info">The <see cref="SerializationInfo"></see> to populate with
        /// data.</param>
        /// <param name="context">The destination (see <see cref="StreamingContext"></see>) for this
        /// serialization.</param>
        /// <exception cref="System.Security.SecurityException">The caller does not have the
        /// required permission.</exception>
        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(Id), Id);
            info.AddValue(nameof(Shape), Shape);
            info.AddValue(nameof(_childIds), _childIds);
            info.AddValue(nameof(ParentId), ParentId);
        }

        private async Task CalculateShapeAsync()
        {
            if ((_childIds?.Count ?? 0) == 0)
            {
                return;
            }

            var locations = await GetChildrenAsync()
                .Select(x => (position: x.Position, radius: x.Shape.ContainingRadius))
                .ToListAsync()
                .ConfigureAwait(false);

            var center = Vector3.Zero;
            foreach (var (position, _) in locations)
            {
                center += position;
            }
            center /= locations.Count;
            Shape = new Sphere(locations.Max(x => Vector3.Distance(x.position, center) + x.radius), center);
        }
    }
}
