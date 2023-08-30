using System.Collections.Immutable;
using System.Text.Json.Serialization;
using Tavenem.Chemistry;
using Tavenem.DataStorage;
using Tavenem.Randomize;
using Tavenem.Universe.Place;
using Tavenem.Universe.Space.Planetoids;
using Tavenem.Universe.Space.Stars;

namespace Tavenem.Universe.Space;

/// <summary>
/// A region of space containing a system of stars, and the bodies which orbit that system.
/// </summary>
public class StarSystem : CosmicLocation
{
    private static readonly HugeNumber _MaxClosePeriod = new HugeNumber(36000) + (3 * 1.732e7);

    internal static readonly HugeNumber _StarSystemSpace = new(3.5, 16);

    /// <summary>
    /// The type discriminator for this type.
    /// </summary>
    public const string StarSystemIdItemTypeName = ":Location:CosmicLocation:StarSystem:";
    /// <summary>
    /// A built-in, read-only type discriminator.
    /// </summary>
    [JsonInclude, JsonPropertyOrder(-1)]
    public override string IdItemTypeName
    {
        get => StarSystemIdItemTypeName;
        set { }
    }

    /// <summary>
    /// True if the primary <see cref="Star"/> in this system is a Population II <see
    /// cref="Star"/>; false if it is a Population I <see cref="Star"/>.
    /// </summary>
    public bool IsPopulationII { get; private set; }

    /// <summary>
    /// The <see cref="Stars.LuminosityClass"/> of the primary <see cref="Star"/> in this
    /// system.
    /// </summary>
    public LuminosityClass LuminosityClass { get; private set; }

    /// <summary>
    /// The <see cref="Stars.SpectralClass"/> of the primary <see cref="Star"/> in this
    /// system.
    /// </summary>
    public SpectralClass SpectralClass { get; private set; }

    /// <summary>
    /// The IDs of the stars in this system.
    /// </summary>
    public IReadOnlyList<string> StarIDs { get; private set; }

    /// <summary>
    /// The type of the primary <see cref="Star"/> in this system.
    /// </summary>
    public StarType StarType { get; private set; }

    /// <inheritdoc />
    [JsonIgnore]
    public override string TypeName => "Star System";

    /// <summary>
    /// Initializes a new instance of <see cref="StarSystem"/> with the given parameters.
    /// </summary>
    /// <param name="parent">
    /// The containing parent location for which to generate a child.
    /// </param>
    /// <param name="position">The position for the child.</param>
    /// <param name="children">
    /// <para>
    /// When this method returns, will be set to a <see cref="List{T}"/> of <see
    /// cref="CosmicLocation"/>s containing any child objects generated for the location during
    /// the creation process.
    /// </para>
    /// <para>
    /// This list may be useful, for instance, to ensure that these additional objects are also
    /// persisted to data storage.
    /// </para>
    /// </param>
    /// <param name="orbit">
    /// <para>
    /// An optional orbit to assign to the child.
    /// </para>
    /// <para>
    /// Depending on the parameters, may override <paramref name="position"/>.
    /// </para>
    /// </param>
    /// <param name="starType">The type of the primary star.</param>
    /// <param name="spectralClass">The <see cref="SpectralClass"/> of the primary star.</param>
    /// <param name="luminosityClass">
    /// The <see cref="LuminosityClass"/> of the primary star.
    /// </param>
    /// <param name="populationII">
    /// Set to true if the primary star is to be a Population II star.
    /// </param>
    /// <param name="allowBinary">
    /// Whether a multiple-star system will be permitted.
    /// </param>
    /// <param name="sunlike">
    /// <para>
    /// If <see langword="true"/>, the system must have a single star similar to Sol, Earth's
    /// sun.
    /// </para>
    /// <para>
    /// Overrides the values of <paramref name="luminosityClass"/>, <paramref
    /// name="spectralClass"/>, <paramref name="populationII"/>, and <paramref
    /// name="allowBinary"/> if set to <see langword="true"/>.
    /// </para>
    /// </param>
    public StarSystem(
        CosmicLocation? parent,
        Vector3<HugeNumber> position,
        out List<CosmicLocation> children,
        OrbitalParameters? orbit = null,
        StarType starType = StarType.MainSequence,
        SpectralClass? spectralClass = null,
        LuminosityClass? luminosityClass = null,
        bool populationII = false,
        bool allowBinary = true,
        bool sunlike = false) : base(parent?.Id, CosmicStructureType.StarSystem)
    {
        StarIDs = ImmutableList<string>.Empty;
        children = Configure(parent, position, starType, spectralClass, luminosityClass, populationII, null, allowBinary, sunlike);

        if (parent is not null && !orbit.HasValue)
        {
            if (parent.StructureType is CosmicStructureType.AsteroidField
                or CosmicStructureType.OortCloud)
            {
                orbit = parent.GetAsteroidChildOrbit();
            }
            else
            {
                orbit = parent.StructureType switch
                {
                    CosmicStructureType.GalaxySubgroup => Position == Vector3<HugeNumber>.Zero
                        ? null
                        : parent.GetGalaxySubgroupChildOrbit(),
                    CosmicStructureType.SpiralGalaxy
                        or CosmicStructureType.EllipticalGalaxy
                        or CosmicStructureType.DwarfGalaxy => Position == Vector3<HugeNumber>.Zero
                        ? null
                        : parent.GetGalaxyChildOrbit(),
                    CosmicStructureType.GlobularCluster => Position == Vector3<HugeNumber>.Zero
                        ? null
                        : parent.GetGlobularClusterChildOrbit(),
                    CosmicStructureType.Nebula => null,
                    CosmicStructureType.HIIRegion => null,
                    CosmicStructureType.PlanetaryNebula => null,
                    CosmicStructureType.StarSystem => null,
                    _ => null,
                };
            }
        }
        if (orbit.HasValue)
        {
            Space.Orbit.AssignOrbit(this, null, orbit.Value);
        }
    }

    private StarSystem(string? parentId) : base(parentId, CosmicStructureType.StarSystem) => StarIDs = ImmutableList<string>.Empty;

    /// <summary>
    /// Initialize a new instance of <see cref="StarSystem"/>.
    /// </summary>
    /// <param name="id">
    /// The unique ID of this item.
    /// </param>
    /// <param name="starType">
    /// The type of the primary <see cref="Star"/> in this system.
    /// </param>
    /// <param name="parentId">
    /// The ID of the location which contains this one.
    /// </param>
    /// <param name="absolutePosition">
    /// <para>
    /// The position of this location, as a set of relative positions starting with the position of
    /// its outermost containing parent within the universe, down to the relative position of its
    /// most immediate parent.
    /// </para>
    /// <para>
    /// The location's own relative <see cref="Location.Position"/> is not expected to be included.
    /// </para>
    /// <para>
    /// May be <see langword="null"/> for a location with no containing parent, or whose parent is
    /// the universe itself (i.e. there is no intermediate container).
    /// </para>
    /// </param>
    /// <param name="name">
    /// An optional name for this <see cref="CosmicLocation"/>.
    /// </param>
    /// <param name="velocity">
    /// The velocity of the <see cref="CosmicLocation"/> in m/s.
    /// </param>
    /// <param name="orbit">
    /// The orbit occupied by this <see cref="CosmicLocation"/> (may be <see langword="null"/>).
    /// </param>
    /// <param name="material">The physical material which comprises this location.</param>
    /// <param name="starIds">
    /// The IDs of the stars in this system.
    /// </param>
    /// <param name="isPopulationII">
    /// Whether the primary <see cref="Star"/> in this system is to be a Population II.
    /// </param>
    /// <param name="luminosityClass">
    /// The <see cref="Stars.LuminosityClass"/> of the primary <see cref="Star"/> in this system.
    /// </param>
    /// <param name="spectralClass">
    /// The <see cref="Stars.SpectralClass"/> of the primary <see cref="Star"/> in this system.
    /// </param>
    /// <remarks>
    /// Note: this constructor is most useful for deserialization. Consider using another
    /// constructor to generate a new instance instead.
    /// </remarks>
    [JsonConstructor]
    public StarSystem(
        string id,
        StarType starType,
        string? parentId,
        Vector3<HugeNumber>[]? absolutePosition,
        string? name,
        Vector3<HugeNumber> velocity,
        Orbit? orbit,
        IMaterial<HugeNumber> material,
        IReadOnlyList<string> starIds,
        bool isPopulationII,
        LuminosityClass luminosityClass,
        SpectralClass spectralClass)
        : base(
            id,
            CosmicStructureType.StarSystem,
            parentId,
            absolutePosition,
            name,
            velocity,
            orbit,
            material)
    {
        StarIDs = starIds;
        StarType = starType;
        IsPopulationII = isPopulationII;
        LuminosityClass = luminosityClass;
        SpectralClass = spectralClass;
    }

    /// <summary>
    /// Generates a new <see cref="StarSystem"/> as the containing parent location of the
    /// given <paramref name="child"/> location.
    /// </summary>
    /// <param name="child">The child location for which to generate a parent.</param>
    /// <param name="children">
    /// <para>
    /// When this method returns, will be set to a <see cref="List{T}"/> of <see
    /// cref="CosmicLocation"/>s containing any child objects generated for the location during
    /// the creation process.
    /// </para>
    /// <para>
    /// This list may be useful, for instance, to ensure that these additional objects are also
    /// persisted to data storage.
    /// </para>
    /// </param>
    /// <param name="position">
    /// An optional position for the child within the new containing parent. If no position is
    /// given, one is randomly determined.
    /// </param>
    /// <param name="orbit">
    /// <para>
    /// An optional orbit for the child to follow in the new parent.
    /// </para>
    /// <para>
    /// Depending on the type of parent location generated, the child may also be placed in a
    /// randomly-determined orbit if none is given explicitly, usually based on its position.
    /// </para>
    /// </param>
    /// <param name="starType">The type of the primary star.</param>
    /// <param name="spectralClass">The <see cref="SpectralClass"/> of the primary star.</param>
    /// <param name="luminosityClass">
    /// The <see cref="LuminosityClass"/> of the primary star.
    /// </param>
    /// <param name="populationII">
    /// Set to true if the primary star is to be a Population II star.
    /// </param>
    /// <param name="allowBinary">
    /// Whether a multiple-star system will be permitted.
    /// </param>
    /// <returns>
    /// <para>
    /// The generated containing parent. Also sets the <see cref="Location.ParentId"/> of the
    /// <paramref name="child"/> accordingly.
    /// </para>
    /// <para>
    /// If no parent could be generated, returns <see langword="null"/>.
    /// </para>
    /// </returns>
    public static CosmicLocation? GetParentForChild(
        CosmicLocation child,
        out List<CosmicLocation> children,
        Vector3<HugeNumber>? position = null,
        OrbitalParameters? orbit = null,
        StarType starType = StarType.MainSequence,
        SpectralClass? spectralClass = null,
        LuminosityClass? luminosityClass = null,
        bool populationII = false,
        bool allowBinary = true)
    {
        var instance = new StarSystem(null);
        child.AssignParent(instance);

        children = new List<CosmicLocation>();

        children.AddRange(instance.Configure(null, Vector3<HugeNumber>.Zero, starType, spectralClass, luminosityClass, populationII, child, allowBinary));

        // Stars, planetoids, and oort clouds will have their place in the system assigned during configuration.
        if (!position.HasValue && child.StructureType != CosmicStructureType.Planetoid)
        {
            if (child.StructureType == CosmicStructureType.Universe)
            {
                position = Vector3<HugeNumber>.Zero;
            }
            else
            {
                var space = child.StructureType switch
                {
                    CosmicStructureType.Supercluster => _SuperclusterSpace,
                    CosmicStructureType.GalaxyCluster => _GalaxyClusterSpace,
                    CosmicStructureType.GalaxyGroup => _GalaxyGroupSpace,
                    CosmicStructureType.GalaxySubgroup => _GalaxySubgroupSpace,
                    CosmicStructureType.SpiralGalaxy => _GalaxySpace,
                    CosmicStructureType.EllipticalGalaxy => _GalaxySpace,
                    CosmicStructureType.DwarfGalaxy => _DwarfGalaxySpace,
                    CosmicStructureType.GlobularCluster => _GlobularClusterSpace,
                    CosmicStructureType.Nebula => _NebulaSpace,
                    CosmicStructureType.HIIRegion => _NebulaSpace,
                    CosmicStructureType.PlanetaryNebula => _PlanetaryNebulaSpace,
                    CosmicStructureType.StarSystem => _StarSystemSpace,
                    CosmicStructureType.AsteroidField => _AsteroidFieldSpace,
                    CosmicStructureType.OortCloud => _OortCloudSpace,
                    CosmicStructureType.BlackHole => _BlackHoleSpace,
                    _ => HugeNumber.Zero,
                };
                position = instance.GetOpenSpace(space, children.ConvertAll(x => x as Location));
            }
        }
        if (position.HasValue)
        {
            child.Position = position.Value;
        }

        if (!child.Orbit.HasValue)
        {
            if (!orbit.HasValue)
            {
                orbit = OrbitalParameters.GetFromEccentricity(instance.Mass, instance.Position, Randomizer.Instance.PositiveNormalDistributionSample(0, 0.05));
            }
            if (orbit.HasValue)
            {
                Space.Orbit.AssignOrbit(child, null, orbit.Value);
            }
        }

        return instance;
    }

    /// <summary>
    /// Adds a planet to the given star in this system, if possible.
    /// </summary>
    /// <param name="dataStore">
    /// The <see cref="IDataStore"/> from which to retrieve instances.
    /// </param>
    /// <param name="star">The <see cref="Star"/> to which a planet should be added.</param>
    /// <param name="planetType">
    /// The type of planet to generate, or <see cref="PlanetType.None"/> to generate a type based on
    /// the conditions of the system and the assigned orbit.
    /// </param>
    /// <returns>
    /// A <see cref="List{T}"/> of all <see cref="CosmicLocation"/>s which were generated. These
    /// locations will not be automatically persisted to the <paramref name="dataStore"/>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The new planet will only be added with an orbit inside the current innermost planet's orbit,
    /// or outside the current outermost planet's orbit. In other words, a new planet will not be
    /// assigned an orbit between existing planets' orbits.
    /// </para>
    /// <para>
    /// It is possible that no planet will be added, if no stable orbit can be found.
    /// </para>
    /// </remarks>
    public async Task<List<CosmicLocation>> AddPlanetAsync(
        IDataStore dataStore,
        Star? star = null,
        PlanetType planetType = PlanetType.None)
    {
        var stars = new List<Star>();
        await foreach (var child in GetStarsAsync(dataStore))
        {
            stars.Add(child);
        }
        star ??= Randomizer.Instance.Next(stars);
        if (star is null)
        {
            return new();
        }

        var (numGiants, numIceGiants, numTerrestrial) = star.GetNumPlanets();
        var (minPeriapsis, maxApoapsis) = GetApsesLimits(stars, star);

        // The maximum mass and density are used to calculate an outer Roche limit (may not be
        // the actual Roche limit for the body which gets generated).
        var minGiantPeriapsis = HugeNumber.Max(minPeriapsis ?? 0, star.GetRocheLimit(Planetoid.GiantMaxDensity));
        var minTerrestrialPeriapsis = HugeNumber.Max(
            minPeriapsis ?? 0,
            star.GetRocheLimit(Planetoid.DefaultTerrestrialMaxDensity));

        // If the calculated minimum and maximum orbits indicates that no stable orbits are
        // possible, eliminate the indicated type of planet.
        if (maxApoapsis.HasValue && minGiantPeriapsis > maxApoapsis)
        {
            numGiants = 0;
            numIceGiants = 0;
        }
        if (maxApoapsis.HasValue && minTerrestrialPeriapsis > maxApoapsis)
        {
            numTerrestrial = 0;
        }

        var hadTerrestrial = false;
        Planetoid? innerPlanet = null;
        Planetoid? outerPlanet = null;
        await foreach (var child in GetChildrenAsync<Planetoid>(dataStore))
        {
            if (!child.Orbit.HasValue
                || child.Orbit.Value.OrbitedId != star.Id)
            {
                continue;
            }

            if (child.PlanetType == PlanetType.IceGiant)
            {
                numIceGiants--;
            }
            else if (child.PlanetType == PlanetType.GasGiant)
            {
                numGiants--;
            }
            else
            {
                hadTerrestrial = true;
                numTerrestrial--;
            }

            if (innerPlanet is null
                || outerPlanet is null)
            {
                innerPlanet = child;
                outerPlanet = child;
            }
            else
            {
                if (child.Orbit.Value.Periapsis < innerPlanet.Orbit!.Value.Periapsis)
                {
                    innerPlanet = child;
                }
                if (child.Orbit.Value.Apoapsis > outerPlanet.Orbit!.Value.Apoapsis)
                {
                    outerPlanet = child;
                }
            }
        }

        // Generate planets one at a time until the specified number have been generated.
        var planetarySystemInfo = new PlanetarySystemInfo
        {
            InnerPlanet = innerPlanet,
            MedianOrbit = innerPlanet is null || outerPlanet is null
                ? HugeNumber.Zero
                : innerPlanet.Orbit!.Value.Periapsis
                    + ((outerPlanet.Orbit!.Value.Apoapsis - innerPlanet.Orbit!.Value.Periapsis) / 2),
            NumTerrestrials = numTerrestrial,
            NumGiants = numGiants,
            NumIceGiants = numIceGiants,
            OuterPlanet = outerPlanet,
            TotalGiants = numGiants + numIceGiants,
        };

        var addedChildren = GeneratePlanet(
            star,
            stars,
            minTerrestrialPeriapsis,
            minGiantPeriapsis,
            maxApoapsis,
            ref planetarySystemInfo,
            planetType);

        // Systems with terrestrial planets are also likely to have debris disks (Kuiper belts)
        // outside the orbit of the most distant planet.
        if (!hadTerrestrial && planetarySystemInfo.TotalTerrestrials > 0)
        {
            var belt = GenerateDebrisDisc(stars, star, planetarySystemInfo.OuterPlanet!, maxApoapsis);
            if (belt is not null)
            {
                addedChildren.Add(belt);
            }
        }

        return addedChildren;
    }

    /// <summary>
    /// Adds a <see cref="Star"/> to this system.
    /// </summary>
    /// <param name="star">The <see cref="Star"/> to add.</param>
    public void AddStar(Star star) => StarIDs = ImmutableList<string>.Empty.AddRange(StarIDs).Add(star.Id);

    /// <summary>
    /// Adds new stars to this system.
    /// </summary>
    /// <param name="dataStore">
    /// The <see cref="IDataStore"/> from which to retrieve instances.
    /// </param>
    /// <param name="min">
    /// <para>
    /// An optional minimum number of stars to generate.
    /// </para>
    /// <para>
    /// It is not guaranteed that this number is generated, if conditions preclude generation of the
    /// specified number. This value merely overrides the usual maximum for the total number of
    /// stars which would normally be generated for a system.
    /// </para>
    /// </param>
    /// <param name="max">
    /// An optional maximum number of stars to generate.
    /// </param>
    /// <returns>
    /// A <see cref="List{T}"/> of the <see cref="Star"/>s which were generated. These stars will
    /// not be automatically persisted to the <paramref name="dataStore"/>, but they will be added
    /// to the system's <see cref="StarIDs"/> collection.
    /// </returns>
    public async Task<List<Star>> AddStarsAsync(
        IDataStore dataStore,
        byte? min = null,
        byte? max = null)
    {
        Star? primary = null;
        var currentStars = new List<Star>();
        var newStars = new List<Star>();
        var companions = new List<(Star star, HugeNumber totalApoapsis)>();
        await foreach (var star in GetStarsAsync(dataStore))
        {
            if (!star.Orbit.HasValue)
            {
                primary = star;
            }
            currentStars.Add(star);
        }

        primary ??= currentStars.FirstOrDefault();
        if (primary is null)
        {
            primary = new Star(StarType.MainSequence, this, Vector3<HugeNumber>.Zero);
            if (primary is not null)
            {
                newStars.Add(primary);
            }
        }

        if (primary is null)
        {
            return newStars;
        }

        var amount = GenerateNumCompanions(primary);
        amount -= currentStars.Count;
        if (min.HasValue)
        {
            amount = Math.Max(amount, min.Value);
        }
        if (max.HasValue)
        {
            amount = Math.Min(amount, max.Value);
        }
        amount -= newStars.Count;
        if (amount <= 0)
        {
            return newStars;
        }

        newStars.AddRange(AddCompanionStars(primary, currentStars, amount)
            .Select(x => x.star));
        StarIDs = ImmutableList<string>
            .Empty
            .AddRange(StarIDs)
            .AddRange(newStars.Select(x => x.Id));
        return newStars;
    }

    /// <summary>
    /// Enumerates the child stars of this instance.
    /// </summary>
    /// <param name="dataStore">
    /// The <see cref="IDataStore"/> from which to retrieve instances.
    /// </param>
    /// <returns>
    /// An <see cref="IEnumerable{T}"/> of child <see cref="Star"/> instances of this <see
    /// cref="StarSystem"/>.
    /// </returns>
    public async IAsyncEnumerable<Star> GetStarsAsync(IDataStore dataStore)
    {
        foreach (var id in StarIDs)
        {
            var star = await dataStore.GetItemAsync<Star>(id).ConfigureAwait(false);
            if (star is not null)
            {
                yield return star;
            }
        }
    }

    /// <summary>
    /// Removes a star from this system.
    /// </summary>
    /// <param name="dataStore">
    /// The <see cref="IDataStore"/> from which to retrieve instances.
    /// </param>
    /// <param name="id">The ID of the star to remove.</param>
    /// <returns>
    /// A <see cref="List{T}"/> of the <see cref="CosmicLocation"/> instances affected by the
    /// change.
    /// </returns>
    /// <remarks>
    /// <para>
    /// All objects in the system which previously orbited the removed star will have their orbit
    /// set to <see langword="null"/>.
    /// </para>
    /// <para>
    /// Removing the primary star will cause the convenience properties of this system (e.g. <see
    /// cref="LuminosityClass"/>) to update to those of the remaining star which previously orbited
    /// the primary with the shortest period, which will become the new primary. The new primary
    /// will be moved to the center of the system, and all orbits which previously referred to the
    /// old primary will be recalculated for the new. If there are no remaining stars after removing
    /// the old primary, the properties of this system will be set to the defaults (e.g. <see
    /// cref="LuminosityClass.None"/>).
    /// </para>
    /// <para>
    /// Removing a star other than the primary which has anything in orbit around it will cause
    /// those objects to instead assume the orbit of the deleted star, if they are stars, or to have
    /// no orbit, if not.
    /// </para>
    /// <para>
    /// This does not persist any changes to the given <paramref name="dataStore"/>. It is used
    /// merely to retrieve and update location instances affected by the change.
    /// </para>
    /// </remarks>
    public async Task<List<CosmicLocation>> RemoveStarAsync(IDataStore dataStore, string id)
    {
        StarIDs = ImmutableList<string>.Empty.AddRange(StarIDs).Remove(id);
        var removed = await dataStore.GetItemAsync<Star>(id);
        if (removed is null)
        {
            return new();
        }

        var affected = new List<CosmicLocation>();

        Star? newPrimary = null;
        var stars = new List<Star>();
        var deorbited = new List<Star>();
        await foreach (var star in GetStarsAsync(dataStore))
        {
            stars.Add(star);
            if (star.Orbit?.OrbitedId?.Equals(id) == true)
            {
                deorbited.Add(star);
            }
        }
        if (deorbited.Count > 0)
        {
            deorbited.Sort((x, y) => (x.Orbit?.Apoapsis ?? HugeNumber.Zero).CompareTo(y.Orbit?.Apoapsis ?? HugeNumber.Zero));
            newPrimary = deorbited.FirstOrDefault();
        }

        if (newPrimary is null)
        {
            IsPopulationII = false;
            LuminosityClass = LuminosityClass.None;
            SpectralClass = SpectralClass.None;
            StarType = StarType.None;
        }
        else
        {
            SetPropertiesForPrimary(newPrimary);
            await newPrimary.SetPositionAsync(dataStore, Vector3<HugeNumber>.Zero);
        }

        await foreach (var child in GetChildrenAsync<CosmicLocation>(dataStore))
        {
            if (!child.Orbit.HasValue
                || string.IsNullOrEmpty(child.Orbit.Value.OrbitedId))
            {
                continue;
            }
            if (child.Orbit.Value.OrbitedId.Equals(id))
            {
                if (newPrimary is null)
                {
                    await child.SetOrbitAsync(dataStore, null);
                }
                else
                {
                    Space.Orbit.AssignOrbit(
                        child,
                        newPrimary.Id,
                        child.Orbit.Value.GetOrbitalParameters() with
                        {
                            OrbitedMass = newPrimary.Mass,
                            OrbitedPosition = newPrimary.Position,
                        });
                    await child.ResetOrbitAsync(dataStore);
                }
                affected.Add(child);
                if (child is Planetoid planetoid)
                {
                    affected.AddRange(planetoid.ConfigureStellarProperties(
                        this,
                        stars,
                        temperatureCorrection: false));
                }
            }
            else if (newPrimary is not null
                && child.Orbit.Value.OrbitedId.Equals(newPrimary.Id))
            {
                Space.Orbit.AssignOrbit(
                    child,
                    newPrimary.Id,
                    child.Orbit.Value.GetOrbitalParameters() with
                    {
                        OrbitedPosition = newPrimary.Position,
                    });
                await child.ResetOrbitAsync(dataStore);
                affected.Add(child);
            }
        }

        return affected;
    }

    internal void SetPropertiesForPrimary(Star primary)
    {
        IsPopulationII = primary.IsPopulationII;
        LuminosityClass = primary.LuminosityClass;
        SpectralClass = primary.SpectralClass;
        StarType = primary.StarType;
    }

    /// <summary>
    /// Single-planet orbital distance may follow a log-normal distribution, with the peak at 0.3
    /// AU (this does not conform to current observations exactly, but extreme biases in current
    /// observations make adequate overall distributions difficult to guess, and the
    /// approximation used is judged reasonably close). In multi-planet systems, migration and
    /// resonances result in a more widely-distributed system.
    /// </summary>
    /// <param name="star">The <see cref="Star"/> around which the planet will orbit.</param>
    /// <param name="isGiant">Whether this is to be a giant planet (including ice giants).</param>
    /// <param name="minTerrestrialPeriapsis">The minimum periapsis for a terrestrial planet.</param>
    /// <param name="minGiantPeriapsis">The minimum periapsis for a giant planet.</param>
    /// <param name="maxApoapsis">The maximum apoapsis.</param>
    /// <param name="innerPlanet">The current innermost planet.</param>
    /// <param name="outerPlanet">The current outermost planet.</param>
    /// <param name="medianOrbit">The median orbit among the current planets.</param>
    /// <param name="totalGiants">The number of giant planets this <see cref="StarSystem"/> is to have.</param>
    /// <returns>The chosen periapsis, or null if no valid orbit is available.</returns>
    private static HugeNumber? ChoosePlanetPeriapsis(
        Star star,
        bool isGiant,
        HugeNumber minTerrestrialPeriapsis,
        HugeNumber minGiantPeriapsis,
        HugeNumber? maxApoapsis,
        Planetoid? innerPlanet,
        Planetoid? outerPlanet,
        HugeNumber medianOrbit,
        int totalGiants)
    {
        HugeNumber? periapsis = null;

        // If this is the first planet, the orbit is selected based on the number of giants the
        // system is to have.
        if (innerPlanet is null)
        {
            // Evaluates to ~0.3 AU if there is only 1 giant, ~5 AU if there are 4 giants (as
            // would be the case for the Solar system), and ~8 AU if there are 6 giants.
            var mean = 7.48e11 - ((4 - Math.Max(1, totalGiants)) * 2.34e11);
            var min = isGiant ? (double)minGiantPeriapsis : (double)minTerrestrialPeriapsis;
            var max = maxApoapsis.HasValue ? (double)maxApoapsis.Value : (double?)null;
            if (!max.HasValue || min < max)
            {
                periapsis = min > mean * 1.25
                    ? min
                    : Randomizer.Instance.NormalDistributionSample(mean, mean / 3, min, max);
            }
        }
        // If there are already any planets and this planet is a giant, it is placed in a higher
        // orbit, never a lower one.
        else if (innerPlanet is not null && isGiant)
        {
            // Forces reassignment to a higher orbit below.
            periapsis = medianOrbit;
        }
        // Terrestrial planets may be in either lower or higher orbits, with lower ones being
        // more likely.
        else if (Randomizer.Instance.NextDouble() <= 0.75)
        {
            periapsis = medianOrbit / 2;
        }
        else
        {
            periapsis = medianOrbit;
        }

        if (outerPlanet is not null)
        {
            var otherMass = isGiant ? new HugeNumber(1.25, 28) : new HugeNumber(3, 25);
            if (periapsis < medianOrbit)
            {
                // Inner orbital spacing is by an average of 21.7 mutual Hill radii, with a
                // standard deviation of 9.5. An average planetary mass is used for the
                // calculation since the planet hasn't been generated yet, which should produce
                // reasonable values.
                var spacing = innerPlanet!.GetMutualHillSphereRadius(otherMass)
                    * Randomizer.Instance.NormalDistributionSample(21.7, 9.5, minimum: 1);
                periapsis = innerPlanet.Orbit!.Value.Periapsis - spacing;
                if (periapsis < (isGiant ? minGiantPeriapsis : minTerrestrialPeriapsis))
                {
                    periapsis = medianOrbit; // Force reassignment below.
                }
            }
            if (periapsis >= medianOrbit)
            {
                // For all terrestrial planets, and giant planets within a 200 day period,
                // orbital spacing is by an average of 21.7 mutual Hill radii, with a standard
                // deviation of 9.5. An average planetary mass is used for the calculation since
                // the planet hasn't been generated yet, which should produce reasonable values.
                var outerPeriod = (double)outerPlanet.Orbit!.Value.Period;
                if (!isGiant || outerPeriod <= 1.728e7)
                {
                    var spacing = outerPlanet.GetMutualHillSphereRadius(otherMass)
                        * Randomizer.Instance.NormalDistributionSample(21.7, 9.5, minimum: 1);
                    periapsis = outerPlanet.Orbit.Value.Apoapsis + spacing;
                    if (periapsis > maxApoapsis)
                    {
                        return null;
                    }
                }
                // Beyond 200 days, a Gaussian distribution of mean-motion resonance with a mean
                // of 2.2 is used to determine orbital spacing for giant planets.
                else
                {
                    var newPeriod = (HugeNumber)Randomizer.Instance.NormalDistributionSample(outerPeriod * 2.2, outerPeriod);

                    // Assuming no eccentricity and an average mass, calculate a periapsis from
                    // the selected period, but set their mutual Hill sphere radius as a minimum separation.
                    periapsis = HugeNumber.Max(outerPlanet.Orbit.Value.Apoapsis
                        + outerPlanet.GetMutualHillSphereRadius(otherMass),
                        ((newPeriod / HugeNumberConstants.TwoPi).Square() * HugeNumberConstants.G * (star.Mass + otherMass)).Cbrt());
                }
            }
        }

        return periapsis;
    }

    private static int GenerateNumCompanions(Star primary)
    {
        var chance = Randomizer.Instance.NextDouble();
        if (primary.StarType == StarType.BrownDwarf)
        {
            return 0;
        }
        else if (primary.StarType == StarType.WhiteDwarf)
        {
            if (chance <= 4.0 / 9.0)
            {
                return 1;
            }
        }
        else if (primary.IsGiant || primary.StarType == StarType.Neutron)
        {
            if (chance <= 0.0625)
            {
                return 2;
            }
            else if (chance <= 0.4375)
            {
                return 1;
            }
        }
        else
        {
            switch (primary.SpectralClass)
            {
                case SpectralClass.A:
                    if (chance <= 0.065)
                    {
                        return 2;
                    }
                    else if (chance <= 0.435)
                    {
                        return 1;
                    }
                    break;
                case SpectralClass.B:
                    if (chance <= 0.8)
                    {
                        return 1;
                    }
                    break;
                case SpectralClass.O:
                    if (chance <= 2.0 / 3.0)
                    {
                        return 1;
                    }
                    break;
                default:
                    if (chance <= 0.01)
                    {
                        return 3;
                    }
                    else if (chance <= 0.03)
                    {
                        return 2;
                    }
                    else if (chance <= 0.3)
                    {
                        return 1;
                    }
                    break;
            }
        }
        return 0;
    }

    /// <summary>
    /// Planets can orbit stably in a multiple-star system between the stars in a range up to
    /// ~33% of an orbiting star's Hill sphere, and ~33% of the distance to an orbited star's
    /// nearest orbiting star's Hill sphere. Alternatively, planets may orbit beyond the sphere
    /// of influence of a close companion, provided they are still not beyond the limits towards
    /// further orbiting stars.
    /// </summary>
    /// <param name="stars">The stars in the system.</param>
    /// <param name="star">The <see cref="Star"/> whose apses' limits are to be calculated.</param>
    private static (HugeNumber? minPeriapsis, HugeNumber? maxApoapsis) GetApsesLimits(List<Star> stars, Star star)
    {
        HugeNumber? maxApoapsis = null;
        HugeNumber? minPeriapsis = null;
        if (star.Orbit is not null)
        {
            maxApoapsis = star.GetHillSphereRadius() * 1 / 3;
        }

        foreach (var entity in stars)
        {
            if (!entity.Orbit.HasValue || entity.Orbit.Value.OrbitedPosition != star.Position)
            {
                continue;
            }

            // If a star is orbiting within ~100 AU, it is considered too close for planets to
            // orbit in between, and orbits are only considered around them as a pair.
            if (entity.Orbit!.Value.Periapsis <= new HugeNumber(1.5, 13))
            {
                minPeriapsis = entity.GetHillSphereRadius() * 20;
                // Clear the maxApoapsis if it's within this outer orbit.
                if (maxApoapsis.HasValue && maxApoapsis < minPeriapsis)
                {
                    maxApoapsis = null;
                }
            }
            else
            {
                var candidateMaxApoapsis = (entity.Orbit.Value.Periapsis - entity.GetHillSphereRadius()) * HugeNumberConstants.Third;
                if (maxApoapsis < candidateMaxApoapsis)
                {
                    candidateMaxApoapsis = maxApoapsis.Value;
                }
                if (!minPeriapsis.HasValue || candidateMaxApoapsis > minPeriapsis)
                {
                    maxApoapsis = candidateMaxApoapsis;
                }
            }
        }

        return (minPeriapsis, maxApoapsis);
    }

    /// <summary>
    /// Generates a close period. Close periods are about 100 days, in a normal distribution
    /// constrained to 3-sigma.
    /// </summary>
    private static HugeNumber GetClosePeriod()
    {
        var count = 0;
        double value;
#pragma warning disable IDE1006 // Naming Styles
        const int mu = 36000;
        const double sigma = 1.732e7;
#pragma warning restore IDE1006 // Naming Styles
        const double Min = mu - (3 * sigma);
        const double Max = mu + (3 * sigma);
        // loop rather than constraining to limits in order to avoid over-representing the limits
        do
        {
            value = Randomizer.Instance.NormalDistributionSample(mu, sigma);
            if (value is >= Min and <= Max)
            {
                return value;
            }
            count++;
        } while (count < 100); // sanity check; should not be reached due to the nature of a normal distribution
        return value;
    }

    private static SpectralClass GetSpectralClassForCompanionStar(Star primary)
    {
        var chance = Randomizer.Instance.NextDouble();
        if (primary.SpectralClass == SpectralClass.O)
        {
            if (chance <= 0.2133)
            {
                return SpectralClass.O; // 80%
            }
            else if (chance <= 0.4267)
            {
                return SpectralClass.B; // 80%
            }
            else if (chance <= 0.5734)
            {
                return SpectralClass.A; // 55%
            }
            else if (chance <= 0.7201)
            {
                return SpectralClass.F; // 55%
            }
            else if (chance <= 0.8268)
            {
                return SpectralClass.G; // 40%
            }
            else if (chance <= 0.9335)
            {
                return SpectralClass.K; // 40%
            }
            else
            {
                return SpectralClass.M; // 25%
            }
        }
        else if (primary.SpectralClass == SpectralClass.B)
        {
            if (chance <= 0.2712)
            {
                return SpectralClass.B; // 80%
            }
            else if (chance <= 0.4576)
            {
                return SpectralClass.A; // 55%
            }
            else if (chance <= 0.6440)
            {
                return SpectralClass.F; // 55%
            }
            else if (chance <= 0.7796)
            {
                return SpectralClass.G; // 40%
            }
            else if (chance <= 0.9152)
            {
                return SpectralClass.K; // 40%
            }
            else
            {
                return SpectralClass.M; // 25%
            }
        }
        else if (primary.SpectralClass == SpectralClass.A)
        {
            if (chance <= 0.2558)
            {
                return SpectralClass.A; // 55%
            }
            else if (chance <= 0.5116)
            {
                return SpectralClass.F; // 55%
            }
            else if (chance <= 0.6976)
            {
                return SpectralClass.G; // 40%
            }
            else if (chance <= 0.8836)
            {
                return SpectralClass.K; // 40%
            }
            else
            {
                return SpectralClass.M; // 25%
            }
        }
        else if (primary.SpectralClass == SpectralClass.F)
        {
            if (chance <= 0.3438)
            {
                return SpectralClass.F; // 55%
            }
            else if (chance <= 0.5938)
            {
                return SpectralClass.G; // 40%
            }
            else if (chance <= 0.8438)
            {
                return SpectralClass.K; // 40%
            }
            else
            {
                return SpectralClass.M; // 25%
            }
        }
        else if (primary.SpectralClass == SpectralClass.G)
        {
            if (chance <= 0.3810)
            {
                return SpectralClass.G; // 40%
            }
            else if (chance <= 0.7619)
            {
                return SpectralClass.K; // 40%
            }
            else
            {
                return SpectralClass.M; // 25%
            }
        }
        else if (primary.SpectralClass == SpectralClass.K)
        {
            if (chance <= 0.6154)
            {
                return SpectralClass.K; // 40%
            }
            else
            {
                return SpectralClass.M; // 25%
            }
        }
        else
        {
            return SpectralClass.M;
        }
    }

    private (Star star, HugeNumber totalApoapsis)? AddCompanionStar(
        List<(Star star, HugeNumber totalApoapsis)> companions,
        Star orbited,
        HugeNumber? orbitedTotalApoapsis,
        HugeNumber period,
        CosmicLocation? child = null)
    {
        Star? star;

        // 20% chance that a white dwarf has a twin, and that a neutron star has a white dwarf companion.
        if ((orbited.StarType == StarType.WhiteDwarf || orbited.StarType == StarType.Neutron)
            && Randomizer.Instance.NextDouble() <= 0.2)
        {
            if (child is Star candidate && candidate.StarType == StarType.WhiteDwarf)
            {
                star = candidate;
            }
            else
            {
                star = new Star(StarType.WhiteDwarf, this, Vector3<HugeNumber>.Zero);
            }
        }
        // There is a chance that a giant will have a giant companion.
        else if (orbited.IsGiant)
        {
            var chance = Randomizer.Instance.NextDouble();
            // Bright, super, and hypergiants are not generated as companions; if these exist in
            // the system, they are expected to be the primary.
            if (chance <= 0.25)
            {
                if (child is Star candidate
                    && candidate.StarType == StarType.RedGiant
                    && candidate.LuminosityClass == LuminosityClass.III)
                {
                    star = candidate;
                }
                else
                {
                    star = new Star(StarType.RedGiant, this, Vector3<HugeNumber>.Zero, luminosityClass: LuminosityClass.III);
                }
            }
            else if (chance <= 0.45)
            {
                if (child is Star candidate
                    && candidate.StarType == StarType.BlueGiant
                    && candidate.LuminosityClass == LuminosityClass.III)
                {
                    star = candidate;
                }
                else
                {
                    star = new Star(StarType.BlueGiant, this, Vector3<HugeNumber>.Zero, luminosityClass: LuminosityClass.III);
                }
            }
            else if (chance <= 0.55)
            {
                if (child is Star candidate
                    && candidate.StarType == StarType.YellowGiant
                    && candidate.LuminosityClass == LuminosityClass.III)
                {
                    star = candidate;
                }
                else
                {
                    star = new Star(StarType.YellowGiant, this, Vector3<HugeNumber>.Zero, luminosityClass: LuminosityClass.III);
                }
            }
            else if (child is Star candidate)
            {
                star = candidate;
            }
            else
            {
                star = new Star(this, Vector3<HugeNumber>.Zero, spectralClass: GetSpectralClassForCompanionStar(orbited));
            }
        }
        else if (child is Star candidate)
        {
            star = candidate;
        }
        else
        {
            star = new Star(this, Vector3<HugeNumber>.Zero, spectralClass: GetSpectralClassForCompanionStar(orbited));
        }

        if (star is not null)
        {
            // Eccentricity tends to be low but increase with longer periods.
            var eccentricity = Math.Abs((double)(Randomizer.Instance.NormalDistributionSample(0, 0.0001) * (period / new HugeNumber(3.1536, 9))));

            // Assuming an effective 2-body system, the period lets us determine the semi-major axis.
            var semiMajorAxis = ((period / HugeNumberConstants.TwoPi).Square() * HugeNumberConstants.G * (orbited.Mass + star.Mass)).Cbrt();

            Space.Orbit.AssignOrbit(
                star,
                orbited,
                (1 - eccentricity) * semiMajorAxis,
                eccentricity,
                Randomizer.Instance.NextDouble(Math.PI),
                Randomizer.Instance.NextDouble(DoubleConstants.TwoPi),
                Randomizer.Instance.NextDouble(DoubleConstants.TwoPi),
                Randomizer.Instance.NextDouble(DoubleConstants.TwoPi));

            var companion = (
                star,
                orbitedTotalApoapsis.HasValue
                    ? orbitedTotalApoapsis.Value + star.Orbit!.Value.Apoapsis
                    : star.Orbit!.Value.Apoapsis);
            companions.Add(companion);
            return companion;
        }
        return null;
    }

    private List<(Star star, HugeNumber totalApoapsis)> AddCompanionStars(
        Star primary,
        List<Star> currentStars,
        int amount,
        CosmicLocation? child = null)
    {
        var newCompanions = new List<(Star star, HugeNumber totalApoapsis)>();
        if (amount <= 0)
        {
            return newCompanions;
        }

        HugeNumber GetTotalApoapsis(Star star)
        {
            var total = HugeNumber.Zero;
            if (!star.Orbit.HasValue)
            {
                return total;
            }
            total += star.Orbit.Value.Apoapsis;
            if (string.IsNullOrEmpty(star.Orbit.Value.OrbitedId))
            {
                return total;
            }
            var orbitedStar = currentStars?.Find(x => x.Id.Equals(star.Orbit.Value.OrbitedId));
            if (orbitedStar is null)
            {
                return total;
            }
            return total + GetTotalApoapsis(orbitedStar);
        }

        var allStars = new List<(Star star, HugeNumber totalApoapsis)>();
        foreach (var currentStar in currentStars)
        {
            allStars.Add((currentStar, GetTotalApoapsis(currentStar)));
        }
        allStars.Sort((x, y) => x.totalApoapsis.CompareTo(y.totalApoapsis));

        (Star star, HugeNumber totalApoapsis)? companion = null;

        while (true)
        {
            // Each new companion star will either orbit the primary, or form a close binary pair
            // with a star that does orbit the primary. Once the primary has at least two companion
            // stars in orbit (a close binary companion and a star with a longer period), stars
            // which orbit the primary will receive a close binary companion before adding any new
            // stars with longer orbits around the primary.

            (Star star, HugeNumber totalApoapsis)? orbited = null;
            List<(Star star, HugeNumber totalApoapsis)>? orbiters = null;
            foreach (var (star, totalApoapsis) in allStars)
            {
                orbiters = allStars
                    .Where(y
                        => y.star.Orbit.HasValue
                        && !string.IsNullOrEmpty(y.star.Orbit.Value.OrbitedId)
                        && y.star.Orbit.Value.OrbitedId.Equals(star.Id))
                    .ToList();
                if (orbiters.Count < 2)
                {
                    orbited = (star, totalApoapsis);
                    break;
                }
                orbiters = null;
            }
            if (!orbited.HasValue)
            {
                orbited = (primary, HugeNumber.Zero);
            }

            // Most periods are about 50 years, in a log normal distribution. There is a chance of a
            // close binary, however.
            bool close;
            if (orbited.Value.totalApoapsis.IsZero())
            {
                if (orbiters is null
                    || orbiters.Count == 0)
                {
                    close = Randomizer.Instance.NextDouble() <= 0.2;
                }
                else if (orbiters.Count >= 2)
                {
                    close = false;
                }
                else
                {
                    close = orbiters[0].totalApoapsis > _MaxClosePeriod;
                }
            }
            else if (orbiters is null
                || orbiters.Count == 0)
            {
                close = Randomizer.Instance.NextBool();
            }
            else
            {
                close = orbiters[0].totalApoapsis > _MaxClosePeriod;
            }

            var companionPeriod = close
                ? GetClosePeriod()
                : Randomizer.Instance.LogNormalDistributionSample(0, 1) * new HugeNumber(1.5768, 9);
            if (!close
                && orbited.Value.totalApoapsis.IsPositive()
                && orbiters?.Count > 0)
            {
                // Long periods after the first are shifted out to avoid being too close to the
                // next-closest star's orbit. This is not necessarily true of all systems in
                // reality, but the orbital mechanics of such systems are chaotic. To simplify orbit
                // models, in this library only systems which could be imagined to be stable over
                // short timescales are produced.
                companionPeriod += Space.Orbit.GetHillSphereRadius(
                    orbiters[^1].star.Mass,
                    orbiters[^1].star.Orbit!.Value.OrbitedMass,
                    orbiters[^1].star.Orbit!.Value.SemiMajorAxis,
                    orbiters[^1].star.Orbit!.Value.Eccentricity) * 20;
            }

            companion = AddCompanionStar(
                newCompanions,
                orbited.Value.star,
                null,
                companionPeriod,
                child);
            if (!companion.HasValue)
            {
                return newCompanions;
            }
            allStars.Add(companion.Value);
            if (companion.Value.star == child)
            {
                child = null;
            }

            amount--;
            if (amount <= 0)
            {
                return newCompanions;
            }
        }
    }

    private List<CosmicLocation> Configure(
        CosmicLocation? parent,
        Vector3<HugeNumber> position,
        StarType starType = StarType.MainSequence,
        SpectralClass? spectralClass = null,
        LuminosityClass? luminosityClass = null,
        bool populationII = false,
        CosmicLocation? child = null,
        bool allowBinary = true,
        bool sunlike = false)
    {
        var addedChildren = new List<CosmicLocation>();
        var stars = new List<Star>();
        Star? primary = null;
        var childIsPrimary = false;
        if (child is Star candidate
            && starType.HasFlag(candidate.StarType)
            && (!spectralClass.HasValue || candidate.SpectralClass == spectralClass.Value)
            && (!luminosityClass.HasValue || candidate.LuminosityClass == luminosityClass.Value)
            && candidate.IsPopulationII == populationII)
        {
            primary = candidate;
            childIsPrimary = true;
            stars.Add(candidate);
        }
        else if (sunlike)
        {
            primary = Star.NewSunlike(this, Vector3<HugeNumber>.Zero);
            if (primary is not null)
            {
                addedChildren.Add(primary);
                stars.Add(primary);
            }
        }
        else
        {
            primary = new Star(starType, this, Vector3<HugeNumber>.Zero, null, spectralClass, luminosityClass, populationII);
            if (primary is not null)
            {
                addedChildren.Add(primary);
                stars.Add(primary);
            }
        }

        if (primary is null)
        {
            return addedChildren;
        }

        SetPropertiesForPrimary(primary);

        var numCompanions = !allowBinary || sunlike ? 0 : GenerateNumCompanions(primary);
        var companions = AddCompanionStars(
            primary,
            stars,
            numCompanions,
            childIsPrimary ? null : child);
        if (companions.Any(x => x.star == child))
        {
            child = null;
        }
        foreach (var (star, _) in companions)
        {
            stars.Add(star);
            if (star != child)
            {
                addedChildren.Add(star);
            }
        }

        var outerApoapsis = numCompanions == 0 ? HugeNumber.Zero : companions.Max(x => x.totalApoapsis);

        // The Shape of a StarSystem depends on the configuration of the Stars (with ~75000
        // AU extra space, or roughly 150% the outer limit for a potential Oort cloud). This
        // should give plenty of breathing room for any objects with high eccentricity to
        // stay within the system's local space, while not placing the objects of interest
        // (stars, planets) too close together in the center of local space.
        var radius = new HugeNumber(1.125, 16) + outerApoapsis;

        // The mass of the stellar bodies is presumed to be at least 99% of the total, so it is used
        // as a close-enough approximation, plus a bit of extra.
        var mass = numCompanions == 0
            ? primary.Mass * new HugeNumber(1001, -3)
            : (primary.Mass + companions.Sum(s => s.star.Mass)) * new HugeNumber(1001, -3);

        Material = new Material<HugeNumber>(
            Substances.All.InterplanetaryMedium,
            new Sphere<HugeNumber>(radius, position),
            mass,
            null,
            parent?.Material.Temperature ?? UniverseAmbientTemperature);

        StarIDs = ImmutableList<string>.Empty
            .AddRange(StarIDs)
            .AddRange(stars.Select(x => x.Id));

        // All single and close-binary systems are presumed to have Oort clouds. Systems with
        // higher multiplicity are presumed to disrupt any Oort clouds.
        if (child?.StructureType == CosmicStructureType.OortCloud
            || stars.Count == 1
            || (stars.Count == 2 && outerApoapsis < new HugeNumber(1.5, 13)))
        {
            if (child?.StructureType == CosmicStructureType.OortCloud)
            {
                child.Position = Vector3<HugeNumber>.Zero;
            }
            else
            {
                var cloud = NewOortCloud(this, Shape.ContainingRadius);
                if (cloud is not null)
                {
                    addedChildren.Add(cloud);
                }
            }
        }

        foreach (var star in stars)
        {
            addedChildren.AddRange(GeneratePlanetsForStar(stars, star, child));
        }

        return addedChildren;
    }

    private CosmicLocation? GenerateDebrisDisc(List<Star> stars, Star star, Planetoid outerPlanet, HugeNumber? maxApoapsis)
    {
        var outerApoapsis = outerPlanet.Orbit!.Value.Apoapsis;
        var innerRadius = outerApoapsis + (outerPlanet.GetMutualHillSphereRadius(new HugeNumber(3, 25)) * Randomizer.Instance.NormalDistributionSample(21.7, 9.5));
        var width = (stars.Count > 1 || Randomizer.Instance.NextBool())
            ? Randomizer.Instance.Next<HugeNumber>(new HugeNumber(3, 12), new HugeNumber(4.5, 12))
            : Randomizer.Instance.LogNormalDistributionSample(0, 1) * new HugeNumber(7.5, 12);
        if (maxApoapsis.HasValue)
        {
            width = HugeNumber.Min(width, maxApoapsis.Value - innerRadius);
        }
        // Cannot be so wide that it overlaps the outermost planet's orbit.
        width = HugeNumber.Min(width, (innerRadius - outerApoapsis) * new HugeNumber(9, -1));
        if (width > 0)
        {
            var radius = width / 2;
            return NewAsteroidField(
                this,
                star,
                star.Position,
                null,
                innerRadius + radius,
                radius);
        }

        return null;
    }

    private List<CosmicLocation> GeneratePlanet(
        Star star,
        List<Star> stars,
        HugeNumber minTerrestrialPeriapsis,
        HugeNumber minGiantPeriapsis,
        HugeNumber? maxApoapsis,
        ref PlanetarySystemInfo planetarySystemInfo,
        PlanetType planetType,
        Planetoid? planet = null)
    {
        var addedChildren = new List<CosmicLocation>();

        if (planet is not null)
        {
            planetarySystemInfo.Periapsis = planet.Orbit?.Periapsis ?? 0;

            if (planet.PlanetType == PlanetType.IceGiant)
            {
                planetarySystemInfo.NumIceGiants--;
            }
            else if (planet.PlanetType == PlanetType.Giant)
            {
                planetarySystemInfo.NumGiants--;
            }
            else
            {
                planetarySystemInfo.NumTerrestrials--;
                planetarySystemInfo.TotalTerrestrials++;
            }
        }
        else
        {
            planet = GetPlanet(
                star,
                stars,
                minTerrestrialPeriapsis,
                minGiantPeriapsis,
                maxApoapsis,
                ref planetarySystemInfo,
                out var satellites,
                planetType);
            addedChildren.AddRange(satellites);
        }

        if (planet is null)
        {
            return addedChildren;
        }
        else
        {
            addedChildren.Add(planet);
        }

        if (planet.IsGiant)
        {
            // Giants may get Trojan asteroid fields at their L4 & L5 Lagrangian points.
            if (Randomizer.Instance.NextBool())
            {
                addedChildren.AddRange(GenerateTrojans(star, planet, planetarySystemInfo.Periapsis!.Value));
            }
            // There is a chance of an inner-system asteroid belt inside the orbit of a giant.
            if (planetarySystemInfo.Periapsis < planetarySystemInfo.MedianOrbit && Randomizer.Instance.NextDouble() <= 0.2)
            {
                var separation = planetarySystemInfo.Periapsis!.Value - (planet.GetMutualHillSphereRadius(new HugeNumber(3, 25)) * Randomizer.Instance.NormalDistributionSample(21.7, 9.5));
                var belt = NewAsteroidField(
                    this,
                    star,
                    star.Position,
                    null,
                    separation * HugeNumberConstants.Deci,
                    separation * new HugeNumber(8, -1));
                if (belt is not null)
                {
                    addedChildren.Add(belt);
                }
            }
        }

        if (planetarySystemInfo.InnerPlanet is null)
        {
            planetarySystemInfo.InnerPlanet = planet;
            planetarySystemInfo.OuterPlanet = planet;
        }
        else if (planetarySystemInfo.Periapsis < planetarySystemInfo.MedianOrbit)
        {
            planetarySystemInfo.InnerPlanet = planet;
        }
        else
        {
            planetarySystemInfo.OuterPlanet = planet;
        }

        planetarySystemInfo.MedianOrbit = planetarySystemInfo.InnerPlanet.Orbit!.Value.Periapsis
            + ((planetarySystemInfo.OuterPlanet!.Orbit!.Value.Apoapsis - planetarySystemInfo.InnerPlanet.Orbit!.Value.Periapsis) / 2);

        return addedChildren;
    }

    private List<CosmicLocation> GeneratePlanetsForStar(List<Star> stars, Star star, CosmicLocation? child = null)
    {
        var addedChildren = new List<CosmicLocation>();

        Planetoid? pregenPlanet = null;
        if (child is Planetoid candidate
            && (PlanetType.AnyTerrestrial.HasFlag(candidate.PlanetType)
            || PlanetType.Giant.HasFlag(candidate.PlanetType)))
        {
            pregenPlanet = candidate;
        }

        var (numGiants, numIceGiants, numTerrestrial) = star.GetNumPlanets();
        if (numGiants + numIceGiants + numTerrestrial == 0 && pregenPlanet is null)
        {
            return addedChildren;
        }

        var (minPeriapsis, maxApoapsis) = GetApsesLimits(stars, star);

        // The maximum mass and density are used to calculate an outer Roche limit (may not be
        // the actual Roche limit for the body which gets generated).
        var minGiantPeriapsis = HugeNumber.Max(minPeriapsis ?? 0, star.GetRocheLimit(Planetoid.GiantMaxDensity));
        var minTerrestrialPeriapsis = HugeNumber.Max(
            minPeriapsis ?? 0,
            star.GetRocheLimit(Planetoid.DefaultTerrestrialMaxDensity));

        // If the calculated minimum and maximum orbits indicates that no stable orbits are
        // possible, eliminate the indicated type of planet.
        if (maxApoapsis.HasValue && minGiantPeriapsis > maxApoapsis)
        {
            numGiants = 0;
            numIceGiants = 0;
        }
        if (maxApoapsis.HasValue && minTerrestrialPeriapsis > maxApoapsis)
        {
            numTerrestrial = 0;
        }

        // Generate planets one at a time until the specified number have been generated.
        var planetarySystemInfo = new PlanetarySystemInfo
        {
            NumTerrestrials = numTerrestrial,
            NumGiants = numGiants,
            NumIceGiants = numIceGiants,
            TotalGiants = numGiants + numIceGiants,
        };
        while (planetarySystemInfo.NumTerrestrials + planetarySystemInfo.NumGiants + planetarySystemInfo.NumIceGiants > 0 || pregenPlanet is not null)
        {
            addedChildren.AddRange(GeneratePlanet(
                star,
                stars,
                minTerrestrialPeriapsis,
                minGiantPeriapsis,
                maxApoapsis,
                ref planetarySystemInfo,
                PlanetType.None,
                pregenPlanet));
            pregenPlanet = null;
        }

        // Systems with terrestrial planets are also likely to have debris disks (Kuiper belts)
        // outside the orbit of the most distant planet.
        if (planetarySystemInfo.TotalTerrestrials > 0)
        {
            var belt = GenerateDebrisDisc(stars, star, planetarySystemInfo.OuterPlanet!, maxApoapsis);
            if (belt is not null)
            {
                addedChildren.Add(belt);
            }
        }

        return addedChildren;
    }

    private Planetoid? GenerateTerrestrialPlanet(
        Star star,
        List<Star> stars,
        HugeNumber periapsis,
        out List<Planetoid> satellites,
        PlanetType planetType = PlanetType.None)
    {
        // Planets with very low orbits are lava planets due to tidal stress (plus a small
        // percentage of others due to impact trauma).

        // The maximum mass and density are used to calculate an outer Roche limit (may not be
        // the actual Roche limit for the body which gets generated).
        var chance = Randomizer.Instance.NextDouble();
        var position = star.Position + (Vector3<HugeNumber>.UnitX * periapsis);
        var rocheLimit = star.GetRocheLimit(Planetoid.DefaultTerrestrialMaxDensity);
        if (periapsis < rocheLimit * new HugeNumber(105, -2)
            || planetType == PlanetType.Lava
            || ((planetType == PlanetType.None
            || (PlanetType.Lava & planetType) == PlanetType.Lava)
            && (chance <= 0.01)))
        {
            return new Planetoid(PlanetType.Lava, this, star, stars, position, out satellites);
        }
        // Planets with close orbits may be iron planets.
        else if (planetType == PlanetType.Iron
            || ((planetType == PlanetType.None
            || (PlanetType.Iron & planetType) == PlanetType.Iron)
            && periapsis < rocheLimit * 200
            && chance <= 0.5))
        {
            return new Planetoid(PlanetType.Iron, this, star, stars, position, out satellites);
        }
        // Late-stage stars and brown dwarfs may have carbon planets.
        else if (planetType == PlanetType.Carbon
            || ((planetType == PlanetType.None
            || (PlanetType.Carbon & planetType) == PlanetType.Carbon)
            && ((star.StarType == StarType.Neutron && chance <= 0.2)
            || (star.StarType == StarType.BrownDwarf && chance <= 0.75))))
        {
            return new Planetoid(PlanetType.Carbon, this, star, stars, position, out satellites);
        }
        // Chance of an ocean planet.
        else if (planetType == PlanetType.Ocean
            || ((planetType == PlanetType.None
            || (PlanetType.Ocean & planetType) == PlanetType.Ocean)
            && chance <= 0.25))
        {
            return new Planetoid(PlanetType.Ocean, this, star, stars, position, out satellites);
        }
        else if (planetType is not PlanetType.None
            and not PlanetType.Terrestrial)
        {
            return new Planetoid(planetType, this, star, stars, position, out satellites);
        }
        else
        {
            return new Planetoid(PlanetType.Terrestrial, this, star, stars, position, out satellites);
        }
    }

    private List<CosmicLocation> GenerateTrojans(Star star, Planetoid planet, HugeNumber periapsis)
    {
        var addedChildren = new List<CosmicLocation>();

        var doubleHillRadius = planet.GetHillSphereRadius() * 2;
        var trueAnomaly = planet.Orbit!.Value.TrueAnomaly + DoubleConstants.ThirdPi; // +60°
        while (trueAnomaly > DoubleConstants.TwoPi)
        {
            trueAnomaly -= DoubleConstants.TwoPi;
        }
        var field = NewAsteroidField(
            this,
            planet,
            -Vector3<HugeNumber>.UnitZ * periapsis,
            new OrbitalParameters(
                star.Mass,
                star.Position,
                periapsis,
                planet.Orbit.Value.Eccentricity,
                planet.Orbit.Value.Inclination,
                Randomizer.Instance.NextDouble(DoubleConstants.TwoPi),
                Randomizer.Instance.NextDouble(DoubleConstants.TwoPi),
                trueAnomaly),
            doubleHillRadius);
        if (field is not null)
        {
            addedChildren.Add(field);
        }

        trueAnomaly = planet.Orbit.Value.TrueAnomaly - DoubleConstants.ThirdPi; // -60°
        while (trueAnomaly < 0)
        {
            trueAnomaly += DoubleConstants.TwoPi;
        }
        field = NewAsteroidField(
            this,
            planet,
            Vector3<HugeNumber>.UnitZ * periapsis,
            new OrbitalParameters(
                star.Mass,
                star.Position,
                periapsis,
                planet.Orbit.Value.Eccentricity,
                planet.Orbit.Value.Inclination,
                Randomizer.Instance.NextDouble(DoubleConstants.TwoPi),
                Randomizer.Instance.NextDouble(DoubleConstants.TwoPi),
                trueAnomaly),
            doubleHillRadius);
        if (field is not null)
        {
            addedChildren.Add(field);
        }

        return addedChildren;
    }

    private Planetoid? GetPlanet(
        Star star,
        List<Star> stars,
        HugeNumber minTerrestrialPeriapsis,
        HugeNumber minGiantPeriapsis,
        HugeNumber? maxApoapsis,
        ref PlanetarySystemInfo planetarySystemInfo,
        out List<Planetoid> satellites,
        PlanetType planetType = PlanetType.None)
    {
        var isGiant = (PlanetType.Giant & planetType) != PlanetType.None;
        if (isGiant)
        {
            planetarySystemInfo.NumGiants--;
        }
        var isIceGiant = planetType == PlanetType.IceGiant;
        if (isIceGiant)
        {
            planetarySystemInfo.NumIceGiants--;
        }
        // If this is the first planet generated, and there are to be any
        // giants, generate a giant first.
        if (planetType == PlanetType.None
            && planetarySystemInfo.InnerPlanet is null
            && planetarySystemInfo.TotalGiants > 0)
        {
            if (planetarySystemInfo.NumGiants > 0)
            {
                isGiant = true;
                planetarySystemInfo.NumGiants--;
            }
            else
            {
                isGiant = true;
                isIceGiant = true;
                planetarySystemInfo.NumIceGiants--;
            }
        }
        // Otherwise, select the type to generate on this pass randomly.
        else if (planetType == PlanetType.None)
        {
            var chance = Randomizer.Instance.NextDouble();
            if (planetarySystemInfo.NumGiants > 0 && (planetarySystemInfo.NumTerrestrials + planetarySystemInfo.NumIceGiants == 0 || chance <= 0.333333))
            {
                isGiant = true;
                planetarySystemInfo.NumGiants--;
            }
            else if (planetarySystemInfo.NumIceGiants > 0 && (planetarySystemInfo.NumTerrestrials == 0 || chance <= (planetarySystemInfo.NumGiants > 0 ? 0.666666 : 0.5)))
            {
                isGiant = true;
                isIceGiant = true;
                planetarySystemInfo.NumIceGiants--;
            }
            // If a terrestrial planet is to be generated, the exact type will be determined later.
            else
            {
                planetarySystemInfo.NumTerrestrials--;
            }
        }
        else if (!isGiant)
        {
            planetarySystemInfo.NumTerrestrials--;
        }

        planetarySystemInfo.Periapsis = ChoosePlanetPeriapsis(
            star,
            isGiant,
            minTerrestrialPeriapsis,
            minGiantPeriapsis,
            maxApoapsis,
            planetarySystemInfo.InnerPlanet,
            planetarySystemInfo.OuterPlanet,
            planetarySystemInfo.MedianOrbit,
            planetarySystemInfo.TotalGiants);

        satellites = new List<Planetoid>();
        // If there is no room left for outer orbits, drop this planet and try again (until there
        // are none left to assign).
        if (!planetarySystemInfo.Periapsis.HasValue || planetarySystemInfo.Periapsis.Value.IsNaN())
        {
            return null;
        }

        // Now that a periapsis has been chosen, assign it as the position of giants.
        // (Terrestrials get their positions set during construction, below).
        Planetoid? planet;
        if (isGiant)
        {
            if (isIceGiant)
            {
                planet = new Planetoid(PlanetType.IceGiant, this, star, stars, star.Position + (Vector3<HugeNumber>.UnitX * planetarySystemInfo.Periapsis.Value), out satellites);
            }
            else
            {
                planet = new Planetoid(PlanetType.GasGiant, this, star, stars, star.Position + (Vector3<HugeNumber>.UnitX * planetarySystemInfo.Periapsis.Value), out satellites);
            }
        }
        else
        {
            planet = GenerateTerrestrialPlanet(
                star,
                stars,
                planetarySystemInfo.Periapsis.Value,
                out satellites,
                planetType);
            planetarySystemInfo.TotalTerrestrials++;
        }

        return planet;
    }

    private struct PlanetarySystemInfo
    {
        public Planetoid? InnerPlanet;
        public Planetoid? OuterPlanet;
        public HugeNumber MedianOrbit;
        public int NumTerrestrials;
        public int NumGiants;
        public int NumIceGiants;
        public HugeNumber? Periapsis;
        public int TotalTerrestrials;
        public int TotalGiants;
    }
}
