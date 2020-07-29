using NeverFoundry.DataStorage;
using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Threading.Tasks;

namespace NeverFoundry.WorldFoundry.Place
{
    /// <summary>
    /// A collection of locations which define a conceptually unified area (though they may not form
    /// a contiguous region).
    /// </summary>
    [Serializable]
    [JsonObject]
    public class Territory : Location
    {
        /// <summary>
        /// The ids of the child locations contained within this instance.
        /// </summary>
        [JsonProperty(TypeNameHandling = TypeNameHandling.None)]
        public IReadOnlyList<string> ChildIds { get; private set; }

        /// <summary>
        /// The type discriminator for this type.
        /// </summary>
        public const string TerritoryIdItemTypeName = ":Location:Territory:";
        /// <summary>
        /// A built-in, read-only type discriminator.
        /// </summary>
        public override string IdItemTypeName => TerritoryIdItemTypeName;

        /// <summary>
        /// Initializes a new instance of <see cref="Territory"/>.
        /// </summary>
        public Territory() => ChildIds = ImmutableList<string>.Empty;

        /// <summary>
        /// Initializes a new instance of <see cref="Territory"/>.
        /// </summary>
        /// <param name="shape">The shape of the location.</param>
        public Territory(IShape shape) : base(shape) => ChildIds = ImmutableList<string>.Empty;

        /// <summary>
        /// Initializes a new instance of <see cref="Territory"/>.
        /// </summary>
        /// <param name="position">
        /// The position of the location relative to the center of its containing region.
        /// </param>
        public Territory(Vector3 position) : base(position) => ChildIds = ImmutableList<string>.Empty;

        /// <summary>
        /// Initializes a new instance of <see cref="Territory"/>.
        /// </summary>
        /// <param name="id">The unique ID of this item.</param>
        /// <param name="idItemTypeName">The type discriminator.</param>
        /// <param name="shape">The shape of the location.</param>
        /// <param name="childIds">
        /// The ids of the child locations contained within this instance.
        /// </param>
        /// <param name="parentId">The ID of the location which contains this one.</param>
        /// <param name="absolutePosition">
        /// <para>
        /// The position of this location, as a set of relative positions starting with the position
        /// of its outermost containing parent within the universe, down to the relative position of
        /// its most immediate parent.
        /// </para>
        /// <para>
        /// The location's own relative <see cref="Location.Position"/> is not expected to be included.
        /// </para>
        /// <para>
        /// May be <see langword="null"/> for a location with no containing parent, or whose parent
        /// is the universe itself (i.e. there is no intermediate container).
        /// </para>
        /// </param>
        /// <remarks>
        /// Note: this constructor is most useful for deserializers. The other constructors are more
        /// suited to creating a new instance, as they will automatically generate an appropriate ID.
        /// </remarks>
        [JsonConstructor]
        [System.Text.Json.Serialization.JsonConstructor]
        public Territory(
            string id,
#pragma warning disable IDE0060 // Remove unused parameter: Used by deserializers.
            string idItemTypeName,
#pragma warning restore IDE0060 // Remove unused parameter
            IShape shape,
            IReadOnlyList<string> childIds,
            string? parentId = null,
            Vector3[]? absolutePosition = null) : base(id, shape, parentId, absolutePosition)
            => ChildIds = childIds;

        private Territory(SerializationInfo info, StreamingContext context) : this(
            (string?)info.GetValue(nameof(Id), typeof(string)) ?? string.Empty,
            TerritoryIdItemTypeName,
            (IShape?)info.GetValue(nameof(Shape), typeof(IShape)) ?? SinglePoint.Origin,
            (IReadOnlyList<string>?)info.GetValue(nameof(ChildIds), typeof(IReadOnlyList<string>)) ?? ImmutableList<string>.Empty,
            (string?)info.GetValue(nameof(ParentId), typeof(string)),
            (Vector3[]?)info.GetValue(nameof(AbsolutePosition), typeof(Vector3[])))
        { }

        /// <summary>
        /// Adds the given <paramref name="locations"/> to this instance.
        /// </summary>
        /// <param name="dataStore">
        /// The <see cref="IDataStore"/> from which to retrieve instances.
        /// </param>
        /// <param name="locations">The <see cref="Location"/> instances to add.</param>
        /// <returns>This instance.</returns>
        public async ValueTask<Territory> AddLocationsAsync(IDataStore dataStore, IEnumerable<Location> locations)
        {
            var list = ImmutableList<string>.Empty.AddRange(ChildIds);
            foreach (var location in locations)
            {
                list = list.Add(location.Id);
            }
            if (list.Count > ChildIds.Count)
            {
                ChildIds = list;
                await CalculateShapeAsync(dataStore).ConfigureAwait(false);
            }
            return this;
        }

        /// <summary>
        /// Adds the given <paramref name="locations"/> to this instance.
        /// </summary>
        /// <param name="dataStore">
        /// The <see cref="IDataStore"/> from which to retrieve instances.
        /// </param>
        /// <param name="locations">One or more <see cref="Location"/> instances to add.</param>
        /// <returns>This instance.</returns>
        public ValueTask<Territory> AddLocationsAsync(IDataStore dataStore, params Location[] locations)
            => AddLocationsAsync(dataStore, locations.AsEnumerable());

        /// <summary>
        /// Enumerates the children of this instance.
        /// </summary>
        /// <param name="dataStore">
        /// The <see cref="IDataStore"/> from which to retrieve instances.
        /// </param>
        /// <returns>
        /// An <see cref="IEnumerable{T}"/> of child <see cref="Location"/> instances of this one.
        /// </returns>
        public override async IAsyncEnumerable<Location> GetChildrenAsync(IDataStore dataStore)
        {
            foreach (var id in ChildIds)
            {
                var result = await dataStore.GetItemAsync<Location>(id).ConfigureAwait(false);
                if (result is not null)
                {
                    yield return result;
                }
            }
        }

        /// <summary>
        /// Determines whether the specified <see cref="Location"/> is contained within the current
        /// instance.
        /// </summary>
        /// <param name="dataStore">
        /// The <see cref="IDataStore"/> from which to retrieve instances.
        /// </param>
        /// <param name="other">The instance to compare with this one.</param>
        /// <returns>
        /// <see langword="true"/> if the specified <see cref="Location"/> is contained within this
        /// instance; otherwise, <see langword="false"/>.
        /// </returns>
        public async ValueTask<bool> AnyRegionContainsAsync(IDataStore dataStore, Location other)
        {
            if (!Contains(other))
            {
                return false;
            }
            await foreach (var child in GetChildrenAsync(dataStore))
            {
                if (child.Contains(other))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Determines whether the specified <paramref name="position"/> is contained within the
        /// current instance.
        /// </summary>
        /// <param name="dataStore">
        /// The <see cref="IDataStore"/> from which to retrieve instances.
        /// </param>
        /// <param name="position">a <see cref="Vector3"/> to check for inclusion in this <see
        /// cref="Location"/>.</param>
        /// <returns>
        /// <see langword="true"/> if the specified <paramref name="position"/> is contained within
        /// this instance; otherwise, <see langword="false"/>.
        /// </returns>
        public async ValueTask<bool> AnyRegionContainsAsync(IDataStore dataStore, Vector3 position)
        {
            if (!Contains(position))
            {
                return false;
            }
            await foreach (var child in GetChildrenAsync(dataStore))
            {
                if (child.Shape.IsPointWithin(position))
                {
                    return true;
                }
            }
            return false;
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
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(Id), Id);
            info.AddValue(nameof(Shape), Shape);
            info.AddValue(nameof(ChildIds), ChildIds);
            info.AddValue(nameof(ParentId), ParentId);
            info.AddValue(nameof(AbsolutePosition), AbsolutePosition);
        }

        private async Task CalculateShapeAsync(IDataStore dataStore)
        {
            if (ChildIds.Count == 0)
            {
                return;
            }

            var locations = new List<(Vector3 position, Number radius)>();
            await foreach (var child in GetChildrenAsync(dataStore))
            {
                locations.Add((child.Position, child.Shape.ContainingRadius));
            }

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
