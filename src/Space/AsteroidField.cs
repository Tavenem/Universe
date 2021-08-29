using System.Text.Json.Serialization;
using Tavenem.Chemistry;
using Tavenem.Randomize;
using Tavenem.Universe.Place;
using Tavenem.Universe.Space.Planetoids;

namespace Tavenem.Universe.Space;

/// <summary>
/// A region of space with a high concentration of asteroids.
/// </summary>
[JsonConverter(typeof(AsteroidFieldConverter))]
public class AsteroidField : CosmicLocation
{
    internal static readonly HugeNumber _AsteroidFieldSpace = new(3.15, 12);
    internal static readonly HugeNumber _OortCloudSpace = new(7.5, 15);

    private static readonly HugeNumber _AsteroidFieldChildDensity = new(13, -31);
    private static readonly List<ChildDefinition> _AsteroidFieldChildDefinitions = new()
    {
        new PlanetChildDefinition(_AsteroidFieldChildDensity * new HugeNumber(74, -2), PlanetType.AsteroidC),
        new PlanetChildDefinition(_AsteroidFieldChildDensity * new HugeNumber(14, -2), PlanetType.AsteroidS),
        new PlanetChildDefinition(_AsteroidFieldChildDensity * HugeNumberConstants.Deci, PlanetType.AsteroidM),
        new PlanetChildDefinition(_AsteroidFieldChildDensity * new HugeNumber(2, -2), PlanetType.Comet),
        new PlanetChildDefinition(_AsteroidFieldChildDensity * new HugeNumber(3, -10), PlanetType.Dwarf),
        new PlanetChildDefinition(_AsteroidFieldChildDensity * new HugeNumber(1, -10), PlanetType.RockyDwarf),
    };

    private static readonly HugeNumber _OortCloudChildDensity = new(8.31, -38);
    private static readonly List<ChildDefinition> _OortCloudChildDefinitions = new()
    {
        new PlanetChildDefinition(_OortCloudChildDensity * new HugeNumber(85, -2), PlanetType.Comet),
        new PlanetChildDefinition(_OortCloudChildDensity * new HugeNumber(11, -2), PlanetType.AsteroidC),
        new PlanetChildDefinition(_OortCloudChildDensity * new HugeNumber(25, -3), PlanetType.AsteroidS),
        new PlanetChildDefinition(_OortCloudChildDensity * new HugeNumber(15, -3), PlanetType.AsteroidM),
    };

    internal OrbitalParameters? _childOrbitalParameters;
    internal bool _toroidal;

    /// <summary>
    /// The type discriminator for this type.
    /// </summary>
    public const string AsteroidFieldIdItemTypeName = ":Location:CosmicLocation:AsteroidField:";
    /// <summary>
    /// A built-in, read-only type discriminator.
    /// </summary>
    public override string IdItemTypeName => AsteroidFieldIdItemTypeName;

    internal HugeNumber MajorRadius
    {
        get
        {
            if (Material.Shape is HollowSphere<HugeNumber> hollowSphere)
            {
                return hollowSphere.OuterRadius;
            }
            if (Material.Shape is Torus<HugeNumber> torus)
            {
                return torus.MajorRadius;
            }
            if (Material.Shape is Ellipsoid<HugeNumber> ellipsoid)
            {
                return ellipsoid.AxisX;
            }
            return HugeNumber.Zero;
        }
    }

    internal HugeNumber MinorRadius
    {
        get
        {
            if (Material.Shape is HollowSphere<HugeNumber> hollowSphere)
            {
                return hollowSphere.InnerRadius;
            }
            if (Material.Shape is Torus<HugeNumber> torus)
            {
                return torus.MinorRadius;
            }
            return HugeNumber.Zero;
        }
    }

    private protected override IEnumerable<ChildDefinition> ChildDefinitions => StructureType == CosmicStructureType.OortCloud
        ? _OortCloudChildDefinitions
        : _AsteroidFieldChildDefinitions;

    /// <summary>
    /// Initializes a new instance of <see cref="AsteroidField"/> with the given parameters.
    /// </summary>
    /// <param name="parent">
    /// The containing parent location for which to generate a child.
    /// </param>
    /// <param name="position">The position for the child.</param>
    /// <param name="orbit">
    /// <para>
    /// An optional orbit to assign to the child.
    /// </para>
    /// <para>
    /// Depending on the parameters, may override <paramref name="position"/>.
    /// </para>
    /// </param>
    /// <param name="oort">
    /// If <see langword="true"/>, generates an Oort cloud. Otherwise, generates an asteroid field.
    /// </param>
    /// <param name="majorRadius">
    /// <para>
    /// The major radius of the field.
    /// </para>
    /// <para>
    /// In the case of an Oort cloud, this should refer instead to the radius of the star system.
    /// </para>
    /// </param>
    /// <param name="minorRadius">
    /// The minor radius of the field.
    /// </param>
    /// <param name="childOrbit">
    /// The orbital parameters to assign to any new child instances (if any).
    /// </param>
    public AsteroidField(
        CosmicLocation? parent,
        Vector3<HugeNumber> position,
        OrbitalParameters? orbit = null,
        bool oort = false,
        HugeNumber? majorRadius = null,
        HugeNumber? minorRadius = null,
        OrbitalParameters? childOrbit = null) : base(parent?.Id, oort ? CosmicStructureType.OortCloud : CosmicStructureType.AsteroidField)
    {
        _childOrbitalParameters = childOrbit;

        if (oort)
        {
            Configure(parent, position, majorRadius);
        }
        else
        {
            Configure(parent, position, majorRadius, minorRadius);
        }

        if (parent is not null && !orbit.HasValue)
        {
            if (parent is AsteroidField asteroidField)
            {
                orbit = asteroidField.GetChildOrbit();
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
                    CosmicStructureType.StarSystem => parent is StarSystem && Position != Vector3<HugeNumber>.Zero
                        ? OrbitalParameters.GetFromEccentricity(parent.Mass, parent.Position, Randomizer.Instance.PositiveNormalDistributionSample(0, 0.05))
                        : null,
                    _ => null,
                };
            }
        }
        if (orbit.HasValue)
        {
            Space.Orbit.AssignOrbit(this, orbit.Value);
        }
    }

    private AsteroidField(string? parentId, CosmicStructureType structureType = CosmicStructureType.AsteroidField) : base(parentId, structureType) { }

    internal AsteroidField(
        string id,
        uint seed,
        CosmicStructureType structureType,
        string? parentId,
        Vector3<HugeNumber>[]? absolutePosition,
        string? name,
        Vector3<HugeNumber> velocity,
        Orbit? orbit,
        Vector3<HugeNumber> position,
        double? temperature,
        HugeNumber majorRadius,
        HugeNumber minorRadius,
        bool toroidal,
        OrbitalParameters? childOrbitalParameters) : base(
            id,
            seed,
            structureType,
            parentId,
            absolutePosition,
            name,
            velocity,
            orbit)
    {
        _toroidal = toroidal;
        _childOrbitalParameters = childOrbitalParameters;
        Reconstitute(position, temperature, majorRadius, minorRadius);
    }

    /// <summary>
    /// Generates a new <see cref="AsteroidField"/> as the containing parent location of the
    /// given <paramref name="child"/> location.
    /// </summary>
    /// <param name="child">The child location for which to generate a parent.</param>
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
    /// <param name="oort">
    /// If <see langword="true"/>, generates an Oort cloud. Otherwise, generates an asteroid field.
    /// </param>
    /// <param name="majorRadius">
    /// <para>
    /// The major radius of the field.
    /// </para>
    /// <para>
    /// In the case of an Oort cloud, this should refer instead to the radius of the star system.
    /// </para>
    /// </param>
    /// <param name="minorRadius">
    /// The minor radius of the field.
    /// </param>
    /// <param name="childOrbit">
    /// The orbital parameters to assign to any new child instances (if any).
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
        Vector3<HugeNumber>? position = null,
        OrbitalParameters? orbit = null,
        bool oort = false,
        HugeNumber? majorRadius = null,
        HugeNumber? minorRadius = null,
        OrbitalParameters? childOrbit = null)
    {
        var instance = oort
            ? new AsteroidField(null, CosmicStructureType.OortCloud)
            : new AsteroidField(null);
        instance._childOrbitalParameters = childOrbit;
        child.AssignParent(instance);

        switch (instance.StructureType)
        {
            case CosmicStructureType.AsteroidField:
                instance.Configure(null, Vector3<HugeNumber>.Zero, majorRadius, minorRadius);
                break;
            case CosmicStructureType.OortCloud:
                instance.Configure(null, Vector3<HugeNumber>.Zero, majorRadius);
                break;
        }

        if (!position.HasValue)
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
                    CosmicStructureType.StarSystem => StarSystem._StarSystemSpace,
                    CosmicStructureType.AsteroidField => _AsteroidFieldSpace,
                    CosmicStructureType.OortCloud => _OortCloudSpace,
                    CosmicStructureType.BlackHole => BlackHole._BlackHoleSpace,
                    CosmicStructureType.Star => StarSystem._StarSystemSpace,
                    CosmicStructureType.Planetoid => Planetoid._GiantSpace,
                    _ => HugeNumber.Zero,
                };
                position = instance.GetOpenSpace(space, new List<Location>());
            }
        }
        if (position.HasValue)
        {
            child.Position = position.Value;
        }

        if (!child.Orbit.HasValue)
        {
            orbit ??= instance.GetChildOrbit();
            if (orbit.HasValue)
            {
                Space.Orbit.AssignOrbit(child, orbit.Value);
            }
        }

        return instance;
    }

    private void Configure(CosmicLocation? parent, Vector3<HugeNumber> position, HugeNumber? majorRadius = null, HugeNumber? minorRadius = null)
    {
        if (StructureType == CosmicStructureType.OortCloud)
        {
            majorRadius = majorRadius.HasValue ? majorRadius.Value + _OortCloudSpace : _OortCloudSpace;
            minorRadius = majorRadius.HasValue ? majorRadius.Value + new HugeNumber(3, 15) : new HugeNumber(3, 15);
        }
        else if (position != Vector3<HugeNumber>.Zero || parent is not StarSystem || !majorRadius.HasValue)
        {
            majorRadius ??= Randomizer.Instance.Next<HugeNumber>(new HugeNumber(1.5, 11), _AsteroidFieldSpace);
            minorRadius = HugeNumber.Zero;
        }
        else
        {
            _toroidal = true;
            majorRadius ??= HugeNumber.Zero;
            minorRadius ??= HugeNumber.Zero;
        }

        Seed = Randomizer.Instance.NextUIntInclusive();
        Reconstitute(
            position,
            parent?.Material.Temperature ?? UniverseAmbientTemperature,
            majorRadius.Value,
            minorRadius.Value);
    }

    private void Reconstitute(Vector3<HugeNumber> position, double? temperature, HugeNumber majorRadius, HugeNumber minorRadius)
    {
        if (StructureType == CosmicStructureType.OortCloud)
        {
            Material = new Material<HugeNumber>(
                Substances.All.InterplanetaryMedium,
                new HollowSphere<HugeNumber>(
                    minorRadius,
                    majorRadius,
                    position),
                new HugeNumber(3, 25),
                null,
                temperature);
            return;
        }

        var randomizer = new Randomizer(Seed);

        var shape = _toroidal
            ? new Torus<HugeNumber>(majorRadius, minorRadius, position)
            : (IShape<HugeNumber>)new Ellipsoid<HugeNumber>(
                majorRadius,
                randomizer.Next(HugeNumberConstants.Half, new HugeNumber(15, -1)) * majorRadius,
                randomizer.Next(HugeNumberConstants.Half, new HugeNumber(15, -1)) * majorRadius,
                position);

        Material = new Material<HugeNumber>(
            Substances.All.InterplanetaryMedium,
            shape,
            shape.Volume * new HugeNumber(7, -8),
            null,
            temperature);
    }

    internal OrbitalParameters? GetChildOrbit()
    {
        if (_childOrbitalParameters.HasValue)
        {
            return _childOrbitalParameters;
        }

        if (Orbit.HasValue)
        {
            return OrbitalParameters.GetFromEccentricity(
                Orbit.Value.OrbitedMass,
                Orbit.Value.OrbitedPosition,
                Orbit.Value.Eccentricity);
        }

        return null;
    }
}
