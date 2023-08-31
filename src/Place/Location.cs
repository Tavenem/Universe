using System.Text.Json.Serialization;
using Tavenem.DataStorage;
using Tavenem.Randomize;
using Tavenem.Universe.Space;

namespace Tavenem.Universe.Place;

/// <summary>
/// A place in a universe, with a location that defines its position, and a shape that defines
/// its extent.
/// </summary>
/// <remarks>
/// Locations can exist in a hierarchy. Any location may contain other locations, and be
/// contained by a location in turn. The relative positions of locations within the same
/// hierarchy can be analyzed using the methods available on this class.
/// </remarks>
[JsonPolymorphic(UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToNearestAncestor)]
[JsonDerivedType(typeof(Location), LocationIdItemTypeName)]
[JsonDerivedType(typeof(CosmicLocation), CosmicLocation.CosmicLocationIdItemTypeName)]
[JsonDerivedType(typeof(Planetoid), Planetoid.PlanetoidIdItemTypeName)]
[JsonDerivedType(typeof(Star), Star.StarIdItemTypeName)]
[JsonDerivedType(typeof(StarSystem), StarSystem.StarSystemIdItemTypeName)]
[JsonDerivedType(typeof(SurfaceRegion), SurfaceRegion.SurfaceRegionIdItemTypeName)]
[JsonDerivedType(typeof(Territory), Territory.TerritoryIdItemTypeName)]
public class Location : IdItem
{
    private Location? _parent;

    /// <summary>
    /// <para>
    /// The position of this location, as a set of relative positions starting with the position
    /// of its outermost containing parent within the universe, down to the relative position of
    /// its most immediate parent.
    /// </para>
    /// <para>
    /// The location's own relative <see cref="Position"/> is not included, and should be
    /// retrieved from that property.
    /// </para>
    /// <para>
    /// May be <see langword="null"/> for a location with no containing parent, or whose parent
    /// is the universe itself (i.e. there is no intermediate container).
    /// </para>
    /// </summary>
    public Vector3<HugeNumber>[]? AbsolutePosition { get; private set; }

    /// <summary>
    /// A string that uniquely identifies this <see cref="Location"/> for display purposes. Includes
    /// a description of its type, and its <see cref="IIdItem.Id"/>.
    /// </summary>
    public string Designation => $"{TypeName} {Id}";

    /// <summary>
    /// The type discriminator for this type.
    /// </summary>
    public const string LocationIdItemTypeName = ":Location:";
    /// <summary>
    /// A built-in, read-only type discriminator.
    /// </summary>
    [JsonInclude, JsonPropertyOrder(-1)]
    public override string IdItemTypeName
    {
        get => LocationIdItemTypeName;
        set { }
    }

    /// <summary>
    /// An optional name for this <see cref="Location"/>.
    /// </summary>
    /// <remarks>
    /// Not every <see cref="Location"/> must have a name. They may be uniquely identified by their
    /// <see cref="IIdItem.Id"/>, instead.
    /// </remarks>
    public string? Name { get; set; }

    /// <summary>
    /// The id of the parent location which contains this instance, if any.
    /// </summary>
    public string? ParentId { get; private set; }

    /// <summary>
    /// The position of this location relative to the center of its parent.
    /// </summary>
    [JsonIgnore]
    public virtual Vector3<HugeNumber> Position
    {
        get => Shape.Position;
        internal set => Shape = Shape.GetCloneAtPosition(value);
    }

    private protected HugeNumber? _radiusSquared;
    /// <summary>
    /// The containing radius of the location, squared.
    /// </summary>
    /// <remarks>
    /// The value is calculated on first request and cached, but not persisted.
    /// </remarks>
    [JsonIgnore]
    public HugeNumber RadiusSquared => _radiusSquared ??= Shape.ContainingRadius.Square();

    /// <summary>
    /// The shape of this location.
    /// </summary>
    public virtual IShape<HugeNumber> Shape { get; private protected set; } = SinglePoint<HugeNumber>.Origin;

    /// <summary>
    /// The <see cref="Location"/>'s <see cref="Name"/>, if it has one; otherwise its <see
    /// cref="Designation"/>.
    /// </summary>
    [JsonIgnore]
    public string Title => Name ?? Designation;

    /// <summary>
    /// The name for this type of location.
    /// </summary>
    [JsonIgnore]
    public virtual string TypeName => "Location";

    /// <summary>
    /// Initializes a new instance of <see cref="Location"/>.
    /// </summary>
    /// <param name="shape">The shape of the location.</param>
    public Location(IShape<HugeNumber> shape) => Shape = shape;

    /// <summary>
    /// Initializes a new instance of <see cref="Location"/>.
    /// </summary>
    public Location() : this(SinglePoint<HugeNumber>.Origin) { }

    /// <summary>
    /// Initializes a new instance of <see cref="Location"/>.
    /// </summary>
    /// <param name="position">
    /// The position of the location relative to the center of its containing region.
    /// </param>
    public Location(Vector3<HugeNumber> position) : this(new SinglePoint<HugeNumber>(position)) { }

    /// <summary>
    /// Initializes a new instance of <see cref="Location"/>.
    /// </summary>
    /// <param name="id">
    /// <para>
    /// An optional <see cref="IIdItem.Id"/> to assign to this instance.
    /// </para>
    /// <para>
    /// If <see langword="null"/> a random <see cref="IIdItem.Id"/> will be generated.
    /// </para>
    /// </param>
    /// <param name="parentId">The id of the location which contains this one.</param>
    public Location(string? id, string? parentId)
    {
        ParentId = parentId;
        if (!string.IsNullOrEmpty(id))
        {
            Id = id;
        }
    }

    /// <summary>
    /// Initializes a new instance of <see cref="Location"/>.
    /// </summary>
    /// <param name="parentId">The id of the location which contains this one.</param>
    public Location(string? parentId) => ParentId = parentId;

    /// <summary>
    /// Initializes a new instance of <see cref="Location"/>.
    /// </summary>
    /// <param name="parentId">The id of the location which contains this one.</param>
    /// <param name="shape">The shape of the location.</param>
    public Location(string? parentId, IShape<HugeNumber> shape) : this(parentId) => Shape = shape;

    /// <summary>
    /// Initializes a new instance of <see cref="Location"/>.
    /// </summary>
    /// <param name="parentId">The id of the location which contains this one.</param>
    /// <param name="position">The position of the location relative to the center of its
    /// parent.</param>
    public Location(string? parentId, Vector3<HugeNumber> position)
        : this(parentId, new SinglePoint<HugeNumber>(position)) { }

    /// <summary>
    /// Initializes a new instance of <see cref="Location"/>.
    /// </summary>
    /// <param name="parent">The location which contains this one.</param>
    public Location(Location parent) : this(parent.Id) { }

    /// <summary>
    /// Initializes a new instance of <see cref="Location"/>.
    /// </summary>
    /// <param name="parent">The location which contains this one.</param>
    /// <param name="shape">The shape of the location.</param>
    /// <remarks>
    /// Automatically sets <see cref="AbsolutePosition"/> based on the <see
    /// cref="AbsolutePosition"/> and <see cref="Position"/> of the <paramref name="parent"/>.
    /// </remarks>
    public Location(Location parent, IShape<HugeNumber> shape) : this(parent.Id, shape)
    {
        AbsolutePosition = new Vector3<HugeNumber>[parent.AbsolutePosition is null ? 1 : parent.AbsolutePosition.Length + 1];
        if (parent.AbsolutePosition is not null)
        {
            for (var i = 0; i < parent.AbsolutePosition.Length; i++)
            {
                AbsolutePosition[i] = parent.AbsolutePosition[i];
            }
        }
        AbsolutePosition[^1] = parent.Position;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="Location"/>.
    /// </summary>
    /// <param name="parent">The location which contains this one.</param>
    /// <param name="position">The position of the location relative to the center of its
    /// parent.</param>
    public Location(Location parent, Vector3<HugeNumber> position)
        : this(parent, new SinglePoint<HugeNumber>(position)) { }

    /// <summary>
    /// Initializes a new instance of <see cref="Location"/>.
    /// </summary>
    /// <param name="id">The unique ID of this item.</param>
    /// <param name="shape">The shape of the location.</param>
    /// <param name="parentId">The ID of the location which contains this one.</param>
    /// <param name="absolutePosition">
    /// <para>
    /// The position of this location, as a set of relative positions starting with the position
    /// of its outermost containing parent within the universe, down to the relative position of
    /// its most immediate parent.
    /// </para>
    /// <para>
    /// The location's own relative <see cref="Position"/> is not expected to be included.
    /// </para>
    /// <para>
    /// May be <see langword="null"/> for a location with no containing parent, or whose parent
    /// is the universe itself (i.e. there is no intermediate container).
    /// </para>
    /// </param>
    /// <param name="name">
    /// An optional name for this location.
    /// </param>
    /// <remarks>
    /// Note: this constructor is most useful for deserialization. The other constructors are more
    /// suited to creating a new instance, as they will automatically generate an appropriate ID.
    /// </remarks>
    [JsonConstructor]
    public Location(
        string id,
        IShape<HugeNumber> shape,
        string? parentId,
        Vector3<HugeNumber>[]? absolutePosition,
        string? name) : base(id)
    {
        Shape = shape;
        ParentId = parentId;
        AbsolutePosition = absolutePosition;
        Name = name;
    }

    private protected Location(
        string id,
        string? parentId,
        Vector3<HugeNumber>[]? absolutePosition,
        string? name) : base(id)
    {
        ParentId = parentId;
        AbsolutePosition = absolutePosition;
        Name = name;
    }

    /// <summary>
    /// Translates the given position relative to the first absolute position, into a position
    /// relative to the final element of the second absolute position.
    /// </summary>
    /// <param name="firstAbsolutePosition">An absolute position. <seealso
    /// cref="AbsolutePosition"/>.</param>
    /// <param name="position">A final position relative to the first absolute position.</param>
    /// <param name="secondAbsolutePosition">Another absolute position. <seealso
    /// cref="AbsolutePosition"/>.</param>
    /// <returns>
    /// <para>
    /// A <see cref="Vector3{TScalar}"/> giving the location of the first absolute position relative to
    /// the final element of the second absolute position.
    /// </para>
    /// <para>
    /// If the first absolute position is <see langword="null"/> it is treated as an outermost
    /// container, and the position is given relative to its center.
    /// </para>
    /// <para>
    /// If the second absolute position is <see langword="null"/> it is treated as an outermost
    /// container, and the position is given relative to its center.
    /// </para>
    /// </returns>
    internal static Vector3<HugeNumber> LocalizePosition(Vector3<HugeNumber>[]? firstAbsolutePosition, Vector3<HugeNumber> position, Vector3<HugeNumber>[]? secondAbsolutePosition)
    {
        if (firstAbsolutePosition is null && secondAbsolutePosition is null)
        {
            return position;
        }
        if (secondAbsolutePosition is null)
        {
            return firstAbsolutePosition!.Aggregate((acc, x) => acc + x) + position;
        }
        if (firstAbsolutePosition is null)
        {
            return position - secondAbsolutePosition!.Aggregate((acc, x) => acc + x);
        }
        if (firstAbsolutePosition == secondAbsolutePosition)
        {
            return position;
        }

        var commonAbsolutePosition = GetCommonAbsolutePosition(firstAbsolutePosition, secondAbsolutePosition);
        Vector3<HugeNumber> otherPosition;
        Vector3<HugeNumber> targetPosition;
        if (commonAbsolutePosition is null)
        {
            otherPosition = secondAbsolutePosition.Aggregate((acc, x) => acc + x);
            targetPosition = firstAbsolutePosition.Aggregate((acc, x) => acc + x) + position;
        }
        else
        {
            otherPosition = Vector3<HugeNumber>.Zero;
            var i = commonAbsolutePosition.Length;
            for (; i < secondAbsolutePosition.Length; i++)
            {
                otherPosition += secondAbsolutePosition[i];
            }

            targetPosition = Vector3<HugeNumber>.Zero;
            i = commonAbsolutePosition.Length;
            for (; i < firstAbsolutePosition.Length; i++)
            {
                targetPosition += firstAbsolutePosition[i];
            }
            targetPosition += position;
        }
        return targetPosition - otherPosition;
    }

    /// <summary>
    /// Translates the position of the given <paramref name="location"/> into a position
    /// relative to the final element of the second absolute position.
    /// </summary>
    /// <param name="location">A <see cref="Location"/>.</param>
    /// <param name="absolutePosition">An absolute position. <seealso
    /// cref="AbsolutePosition"/>.</param>
    /// <returns>
    /// <para>
    /// A <see cref="Vector3{TScalar}"/> giving the location relative to the final element of the given
    /// absolute position.
    /// </para>
    /// <para>
    /// If the absolute position is <see langword="null"/> it is treated as an outermost
    /// container, and the position is given relative to its center.
    /// </para>
    /// </returns>
    internal static Vector3<HugeNumber> LocalizePosition(Location location, Vector3<HugeNumber>[]? absolutePosition)
        => LocalizePosition(location.AbsolutePosition, location.Position, absolutePosition);

    private static Vector3<HugeNumber>[]? GetCommonAbsolutePosition(Vector3<HugeNumber>[]? firstAbsolutePosition, Vector3<HugeNumber>[]? secondAbsolutePosition)
    {
        if (secondAbsolutePosition is null)
        {
            return null;
        }
        if (firstAbsolutePosition is null)
        {
            return null;
        }
        if (firstAbsolutePosition == secondAbsolutePosition)
        {
            return firstAbsolutePosition;
        }
        return firstAbsolutePosition
            .TakeWhile((o, i) => secondAbsolutePosition.Length > i && o == secondAbsolutePosition[i])
            .ToArray();
    }

    /// <summary>
    /// Attempts to find an open space within a location, with the given radius, in a random
    /// direction, as close as possible to the given point.
    /// </summary>
    /// <param name="position">
    /// The position closest to which an open space is to be found.
    /// </param>
    /// <param name="radius">The radius of the space to find.</param>
    /// <param name="children">The current children of the location.</param>
    /// <returns>
    /// The center point of an open space within the location with the given radius; or <see langword="null"/> if
    /// no such open position could be found.
    /// </returns>
    public static Vector3<HugeNumber>? GetNearestOpenSpace(Vector3<HugeNumber> position, HugeNumber radius, List<Location> children)
    {
        var insanityCheck = 0;
        Vector3<HugeNumber>? pos;
        var distance = HugeNumber.Zero;
        do
        {
            var rot = Randomizer.Instance.NextQuaternion<HugeNumber>();
            var direction = Vector3<HugeNumber>.UnitX.Transform(rot);
            pos = position + (direction.Normalize() * distance);
            var shape = new Sphere<HugeNumber>(radius, pos.Value);
            var any = false;
            foreach (var child in children)
            {
                if (child.Shape.Intersects(shape))
                {
                    any = true;
                    break;
                }
            }
            if (any)
            {
                pos = null;
                distance += radius;
            }
            insanityCheck++;
        } while (!pos.HasValue && insanityCheck < 1000);
        return pos.HasValue
            ? position + pos.Value
            : (Vector3<HugeNumber>?)null;
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
    public bool Contains(Location other)
    {
        var commonAbsolutePosition = GetCommonAbsolutePosition(other);
        if (commonAbsolutePosition is null
            || commonAbsolutePosition.Length == 0
            || commonAbsolutePosition[^1] != Position)
        {
            return false;
        }
        return Shape.Intersects(other.Shape.GetCloneAtPosition(LocalizePosition(other)));
    }

    /// <summary>
    /// Determines whether the specified <paramref name="position"/> is contained within the
    /// current instance.
    /// </summary>
    /// <param name="position">a <see cref="Vector3{TScalar}"/> to check for inclusion in this <see
    /// cref="Location"/>.</param>
    /// <returns>
    /// <see langword="true"/> if the specified <paramref name="position"/> is contained within
    /// this instance; otherwise, <see langword="false"/>.
    /// </returns>
    public bool Contains(Vector3<HugeNumber> position) => Shape.IsPointWithin(position);

    /// <summary>
    /// Removes this location and all contained children from the given data store.
    /// </summary>
    public virtual async Task<bool> DeleteAsync(IDataStore dataStore)
    {
        var childrenSuccess = true;
        await foreach (var child in GetChildrenAsync(dataStore))
        {
            childrenSuccess &= await child.DeleteAsync(dataStore).ConfigureAwait(false);
        }
        return childrenSuccess && await dataStore.RemoveItemAsync(this).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets a flattened enumeration of all descendants of this instance.
    /// </summary>
    /// <param name="dataStore">
    /// The <see cref="IDataStore"/> from which to retrieve instances.
    /// </param>
    /// <returns>
    /// A flattened <see cref="IAsyncEnumerable{T}"/> of all descendant child <see
    /// cref="Location"/> instances of this one.
    /// </returns>
    public async IAsyncEnumerable<Location> GetAllChildrenAsync(IDataStore dataStore)
    {
        await foreach (var child in GetChildrenAsync(dataStore))
        {
            yield return child;

            await foreach (var sub in child.GetAllChildrenAsync(dataStore))
            {
                yield return sub;
            }
        }
    }

    /// <summary>
    /// Gets a flattened enumeration of all descendants of this instance of the given type.
    /// </summary>
    /// <typeparam name="T">The type of child instances to retrieve.</typeparam>
    /// <param name="dataStore">
    /// The <see cref="IDataStore"/> from which to retrieve instances.
    /// </param>
    /// <returns>
    /// A flattened <see cref="IEnumerable{T}"/> of all descendant child <see cref="Location"/>
    /// instances of this one which are of the given type.
    /// </returns>
    public async IAsyncEnumerable<T> GetAllChildrenAsync<T>(IDataStore dataStore) where T : Location
    {
        await foreach (var item in GetAllChildrenAsync(dataStore))
        {
            if (item is T child)
            {
                yield return child;
            }
        }
    }

    /// <summary>
    /// Gets a flattened enumeration of all descendants of this instance of the given type.
    /// </summary>
    /// <param name="dataStore">
    /// The <see cref="IDataStore"/> from which to retrieve instances.
    /// </param>
    /// <param name="type">The type of child instances to retrieve.</param>
    /// <returns>
    /// A flattened <see cref="IEnumerable{T}"/> of all descendant child <see cref="Location"/>
    /// instances of this one which are of the given type.
    /// </returns>
    public async IAsyncEnumerable<Location> GetAllChildrenAsync(IDataStore dataStore, Type type)
    {
        await foreach (var item in GetAllChildrenAsync(dataStore))
        {
            if (type.IsAssignableFrom(item.GetType()))
            {
                yield return item;
            }
        }
    }

    /// <summary>
    /// Enumerates the children of this instance.
    /// </summary>
    /// <param name="dataStore">
    /// The <see cref="IDataStore"/> from which to retrieve instances.
    /// </param>
    /// <returns>
    /// An <see cref="IEnumerable{T}"/> of child <see cref="Location"/> instances of this one.
    /// </returns>
    public virtual IAsyncEnumerable<Location> GetChildrenAsync(IDataStore dataStore)
        => dataStore.Query<Location>().Where(x => x.ParentId == Id).AsAsyncEnumerable();

    /// <summary>
    /// Enumerates the children of this instance of the given type.
    /// </summary>
    /// <param name="dataStore">
    /// The <see cref="IDataStore"/> from which to retrieve instances.
    /// </param>
    /// <returns>
    /// An <see cref="IEnumerable{T}"/> of child <see cref="Location"/> instances of this one.
    /// </returns>
    public virtual IAsyncEnumerable<T> GetChildrenAsync<T>(IDataStore dataStore) where T : Location
        => dataStore.Query<T>().Where(x => x.ParentId == Id).AsAsyncEnumerable();

    /// <summary>
    /// Determines the smallest child <see cref="Location"/> at any level of this instance's
    /// descendant hierarchy which fully contains the specified <see cref="Location"/> within
    /// its containing radius.
    /// </summary>
    /// <param name="dataStore">
    /// The <see cref="IDataStore"/> from which to retrieve instances.
    /// </param>
    /// <param name="other">
    /// The <see cref="Location"/> whose smallest containing <see cref="Location"/> is to be
    /// determined.
    /// </param>
    /// <returns>
    /// The smallest child <see cref="Location"/> at any level of this instance's descendant
    /// hierarchy which fully contains the specified <see cref="Location"/> within its
    /// containing radius, or this instance, if no child contains the position.
    /// </returns>
    public async Task<Location?> GetContainingChildAsync(IDataStore dataStore, Location other)
    {
        var min = HugeNumber.PositiveInfinity;
        Location? minItem = null;
        await foreach (var item in GetAllChildrenAsync(dataStore))
        {
            if (item.Position.Distance(item.LocalizePosition(other)) > item.Shape.ContainingRadius - other.Shape.ContainingRadius)
            {
                continue;
            }
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
    /// <param name="dataStore">
    /// The <see cref="IDataStore"/> from which to retrieve instances.
    /// </param>
    /// <param name="position">
    /// The position whose smallest containing <see cref="Location"/> is to be determined.
    /// </param>
    /// <returns>
    /// The smallest child <see cref="Location"/> at any level of this instance's descendant
    /// hierarchy which contains the specified <paramref name="position"/>, or this instance, if
    /// no child contains the position.
    /// </returns>
    public async Task<Location?> GetContainingChildAsync(IDataStore dataStore, Vector3<HugeNumber> position)
    {
        var min = HugeNumber.PositiveInfinity;
        Location? minItem = null;
        await foreach (var item in GetAllChildrenAsync(dataStore))
        {
            if (!item.Shape.IsPointWithin(item.GetLocalPosition(this, position)))
            {
                continue;
            }
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
    /// <param name="position">A <see cref="Vector3{TScalar}"/> representing a position relative to the
    /// center of this location.</param>
    /// <param name="other">Another <see cref="Location"/>.</param>
    /// <returns>The distance between the given <paramref name="position"/> and the given <see
    /// cref="Location"/>, in meters; or <see
    /// cref="HugeNumber.PositiveInfinity"/>, if they do not
    /// share a common parent.</returns>
    public HugeNumber GetDistanceFromPositionTo(Vector3<HugeNumber> position, Location other)
        => other.LocalizePosition(this, position).Distance(other.Position);

    /// <summary>
    /// Gets the distance from the given position relative to the center of this instance to the
    /// given <paramref name="other"/> <see cref="Location"/>.
    /// </summary>
    /// <param name="localPosition">A <see cref="Vector3{TScalar}"/> representing a position relative to
    /// the center of this location.</param>
    /// <param name="other">Another <see cref="Location"/>.</param>
    /// <param name="otherPosition">A <see cref="Vector3{TScalar}"/> representing a position relative to
    /// the center of the <paramref name="other"/> location.</param>
    /// <returns>The distance between the given positions, in meters; or <see
    /// cref="HugeNumber.PositiveInfinity"/>, if they do not
    /// share a common parent.</returns>
    public HugeNumber GetDistanceFromPositionTo(Vector3<HugeNumber> localPosition, Location other, Vector3<HugeNumber> otherPosition)
        => other.GetLocalPosition(this, localPosition).Distance(otherPosition);

    /// <summary>
    /// Gets the distance from this instance to the given <paramref name="other"/> <see
    /// cref="Location"/>.
    /// </summary>
    /// <param name="other">Another <see cref="Location"/>.</param>
    /// <returns>The distance between this instance and the given <see cref="Location"/>, in
    /// meters; or <see cref="HugeNumber.PositiveInfinity"/>,
    /// if they do not share a common parent.</returns>
    public HugeNumber GetDistanceTo(Location other) => LocalizePosition(other).Distance(Position);

    /// <summary>
    /// Attempts to find a randomly positioned open space within this location with the given
    /// radius.
    /// </summary>
    /// <param name="radius">The radius of the space to find.</param>
    /// <param name="children">The current children of this location.</param>
    /// <returns>
    /// The center point of an open space within this location with the given radius; or <see langword="null"/> if
    /// no such open position could be found.
    /// </returns>
    public Vector3<HugeNumber>? GetOpenSpace(HugeNumber radius, List<Location> children)
    {
        var insanityCheck = 0;
        Vector3<HugeNumber>? pos;
        do
        {
            pos = Randomizer.Instance.NextVector3(Shape.ContainingRadius - radius);
            var shape = new Sphere<HugeNumber>(radius, pos.Value);
            var any = false;
            foreach (var child in children)
            {
                if (child.Shape.Intersects(shape))
                {
                    any = true;
                    break;
                }
            }
            if (any)
            {
                pos = null;
            }
            insanityCheck++;
        } while (!pos.HasValue && insanityCheck < 100);
        return pos;
    }

    /// <summary>
    /// Gets the parent location which contains this one, if any.
    /// </summary>
    /// <param name="dataStore">
    /// The <see cref="IDataStore"/> from which to retrieve instances.
    /// </param>
    /// <returns>The parent location which contains this one, if any.</returns>
    public async ValueTask<Location?> GetParentAsync(IDataStore dataStore)
    {
        _parent ??= await dataStore.GetItemAsync<Location>(ParentId).ConfigureAwait(false);
        return _parent;
    }

    /// <summary>
    /// Sets the id of the parent location which contains this one, if any.
    /// </summary>
    /// <param name="dataStore">
    /// The <see cref="IDataStore"/> from which to retrieve instances.
    /// </param>
    /// <param name="parent">
    /// The parent location which contains this one. May be <see langword="null"/>.
    /// </param>
    public async Task SetParentAsync(IDataStore dataStore, Location? parent)
    {
        AssignParent(parent);
        await ResetPosition(dataStore).ConfigureAwait(false);
    }

    /// <summary>
    /// Sets the <see cref="Position"/> of this location relative to the center of its parent.
    /// </summary>
    /// <param name="dataStore">
    /// The <see cref="IDataStore"/> from which to retrieve instances.
    /// </param>
    /// <param name="position">The new position.</param>
    public async Task SetPositionAsync(IDataStore dataStore, Vector3<HugeNumber> position)
    {
        if (Position != position)
        {
            AssignPosition(position);
            await ResetPosition(dataStore).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Sets the <see cref="Shape"/> of this location.
    /// </summary>
    /// <param name="dataStore">
    /// The <see cref="IDataStore"/> from which to retrieve instances.
    /// </param>
    /// <param name="shape">The new shape.</param>
    public virtual Task SetShapeAsync(IDataStore dataStore, IShape<HugeNumber> shape)
    {
        Shape = shape;
        _radiusSquared = null;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Returns a string that represents the object.
    /// </summary>
    /// <returns>A string that represents the object.</returns>
    public override string ToString() => string.IsNullOrEmpty(Name)
        ? Designation
        : $"{TypeName} {Name}";

    internal void AssignParent(Location? parent)
    {
        ParentId = parent?.Id;
        _parent = parent;
        AssignPosition(parent is null
            ? Vector3<HugeNumber>.Zero
            : parent.LocalizePosition(this));
        if (AbsolutePosition is not null
            && parent is null)
        {
            AbsolutePosition = null;
        }
        if (parent is not null)
        {
            UpdateParentPosition(parent.Position);
        }
    }

    internal HugeNumber GetDistanceSquaredTo(Location other) => LocalizePosition(other).LengthSquared();

    /// <summary>
    /// Translates the given position relative to the given <paramref name="location"/>'s center
    /// into a position relative to this location's center.
    /// </summary>
    /// <param name="location">A <see cref="Location"/>.</param>
    /// <param name="position">A position relative to the given <paramref name="location"/>'s
    /// center.</param>
    /// <returns>
    /// <para>
    /// A <see cref="Vector3{TScalar}"/> giving the location relative to this
    /// location's center.
    /// </para>
    /// </returns>
    internal Vector3<HugeNumber> GetLocalPosition(Location location, Vector3<HugeNumber> position)
    {
        var result = new Vector3<HugeNumber>[(AbsolutePosition?.Length ?? 0) + 1];
        AbsolutePosition?.CopyTo(result, 0);
        result[^1] = Position;

        return LocalizePosition(location.AbsolutePosition, position, result);
    }

    /// <summary>
    /// Translates the position of the given <paramref name="location"/> into a position
    /// relative to the final element of this location's absolute position.
    /// </summary>
    /// <param name="location">A <see cref="Location"/>.</param>
    /// <returns>
    /// <para>
    /// A <see cref="Vector3{TScalar}"/> giving the location relative to the final element of this
    /// location's absolute position.
    /// </para>
    /// </returns>
    internal Vector3<HugeNumber> LocalizePosition(Location location)
        => LocalizePosition(location.AbsolutePosition, location.Position, AbsolutePosition);

    /// <summary>
    /// Translates the given position relative to the given <paramref name="location"/>'s center
    /// into a position relative to the final element of this location's absolute position.
    /// </summary>
    /// <param name="location">A <see cref="Location"/>.</param>
    /// <param name="position">A position relative to the given <paramref name="location"/>'s
    /// center.</param>
    /// <returns>
    /// <para>
    /// A <see cref="Vector3{TScalar}"/> giving the location relative to the final element of this
    /// location's absolute position.
    /// </para>
    /// </returns>
    internal Vector3<HugeNumber> LocalizePosition(Location location, Vector3<HugeNumber> position)
        => LocalizePosition(location.AbsolutePosition, position, AbsolutePosition);

    internal void UpdateParentPosition(Vector3<HugeNumber> parentPosition)
    {
        if (AbsolutePosition is null
            || AbsolutePosition.Length == 0)
        {
            AbsolutePosition = new Vector3<HugeNumber>[] { parentPosition };
        }
        else
        {
            AbsolutePosition[^1] = parentPosition;
        }
    }

    private protected virtual void AssignPosition(Vector3<HugeNumber> position)
        => Shape = Shape.GetCloneAtPosition(position);

    private protected Vector3<HugeNumber>[]? GetCommonAbsolutePosition(Location? other)
    {
        if (other is null)
        {
            return null;
        }
        if (other == this)
        {
            return AbsolutePosition;
        }
        // Handle common cases before performing the expensive calculation.
        if (string.Equals(ParentId, other.Id, StringComparison.Ordinal))
        {
            var result = new Vector3<HugeNumber>[(other.AbsolutePosition?.Length ?? 0) + 1];
            other.AbsolutePosition?.CopyTo(result, 0);
            result[^1] = other.Position;
            return result;
        }
        if (string.Equals(other.ParentId, Id, StringComparison.Ordinal))
        {
            var result = new Vector3<HugeNumber>[(AbsolutePosition?.Length ?? 0) + 1];
            AbsolutePosition?.CopyTo(result, 0);
            result[^1] = Position;
            return result;
        }
        if (string.Equals(ParentId, other.ParentId, StringComparison.Ordinal))
        {
            return AbsolutePosition;
        }
        if (AbsolutePosition is null
            || other.AbsolutePosition is null)
        {
            return null;
        }
        return AbsolutePosition
            .TakeWhile((o, i) => other.AbsolutePosition.Length > i && o == other.AbsolutePosition[i])
            .ToArray();
    }

    private protected virtual ValueTask ResetPosition(IDataStore dataStore) => new();
}
