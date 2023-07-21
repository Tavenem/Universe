using Tavenem.Chemistry;
using Tavenem.Randomize;
using Tavenem.Universe.Space.Planetoids;

namespace Tavenem.Universe.Space;

/// <summary>
/// A region of space with a high concentration of asteroids.
/// </summary>
public partial class CosmicLocation
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
    private static readonly HugeNumber _AsteroidFieldDensity = new(7, -8);

    private static readonly HugeNumber _OortCloudChildDensity = new(8.31, -38);
    private static readonly List<ChildDefinition> _OortCloudChildDefinitions = new()
    {
        new PlanetChildDefinition(_OortCloudChildDensity * new HugeNumber(85, -2), PlanetType.Comet),
        new PlanetChildDefinition(_OortCloudChildDensity * new HugeNumber(11, -2), PlanetType.AsteroidC),
        new PlanetChildDefinition(_OortCloudChildDensity * new HugeNumber(25, -3), PlanetType.AsteroidS),
        new PlanetChildDefinition(_OortCloudChildDensity * new HugeNumber(15, -3), PlanetType.AsteroidM),
    };
    private static readonly HugeNumber _OortCloudInnerRadius = new(3, 15);
    private static readonly HugeNumber _OortCloudMass = new(3, 25);

    /// <summary>
    /// Gets a new asteroid field with the given parameters.
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
    /// <param name="majorRadius">
    /// The major radius of the field.
    /// </param>
    /// <param name="minorRadius">
    /// The minor radius of the field.
    /// </param>
    public static CosmicLocation NewAsteroidField(
        CosmicLocation? parent,
        Vector3<HugeNumber> position,
        OrbitalParameters? orbit = null,
        HugeNumber? majorRadius = null,
        HugeNumber? minorRadius = null)
    {
        var instance = new CosmicLocation(parent?.Id, CosmicStructureType.AsteroidField);
        instance.ConfigureAsteroidFieldInstance(position, parent, majorRadius, minorRadius);

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
                    CosmicStructureType.GalaxySubgroup => instance.Position == Vector3<HugeNumber>.Zero
                        ? null
                        : parent.GetGalaxySubgroupChildOrbit(),
                    CosmicStructureType.SpiralGalaxy
                        or CosmicStructureType.EllipticalGalaxy
                        or CosmicStructureType.DwarfGalaxy => instance.Position == Vector3<HugeNumber>.Zero
                        ? null
                        : parent.GetGalaxyChildOrbit(),
                    CosmicStructureType.GlobularCluster => instance.Position == Vector3<HugeNumber>.Zero
                        ? null
                        : parent.GetGlobularClusterChildOrbit(),
                    CosmicStructureType.StarSystem => parent is StarSystem && instance.Position != Vector3<HugeNumber>.Zero
                        ? OrbitalParameters.GetFromEccentricity(parent.Mass, parent.Position, Randomizer.Instance.PositiveNormalDistributionSample(0, 0.05))
                        : null,
                    _ => null,
                };
            }
        }
        if (orbit.HasValue)
        {
            Space.Orbit.AssignOrbit(instance, orbit.Value);
        }

        return instance;
    }

    /// <summary>
    /// Gets a new Oort cloud with the given parameters.
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
    /// <param name="radius">
    /// The radius of the star system.
    /// </param>
    public static CosmicLocation NewOortCloud(
        CosmicLocation? parent,
        Vector3<HugeNumber> position,
        OrbitalParameters? orbit = null,
        HugeNumber? radius = null)
    {
        var instance = new CosmicLocation(parent?.Id, CosmicStructureType.AsteroidField);
        instance.ConfigureOortCloudInstance(position, parent, radius);

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
                    CosmicStructureType.GalaxySubgroup => instance.Position == Vector3<HugeNumber>.Zero
                        ? null
                        : parent.GetGalaxySubgroupChildOrbit(),
                    CosmicStructureType.SpiralGalaxy
                        or CosmicStructureType.EllipticalGalaxy
                        or CosmicStructureType.DwarfGalaxy => instance.Position == Vector3<HugeNumber>.Zero
                        ? null
                        : parent.GetGalaxyChildOrbit(),
                    CosmicStructureType.GlobularCluster => instance.Position == Vector3<HugeNumber>.Zero
                        ? null
                        : parent.GetGlobularClusterChildOrbit(),
                    CosmicStructureType.StarSystem => parent is StarSystem && instance.Position != Vector3<HugeNumber>.Zero
                        ? OrbitalParameters.GetFromEccentricity(parent.Mass, parent.Position, Randomizer.Instance.PositiveNormalDistributionSample(0, 0.05))
                        : null,
                    _ => null,
                };
            }
        }
        if (orbit.HasValue)
        {
            Space.Orbit.AssignOrbit(instance, orbit.Value);
        }

        return instance;
    }

    internal OrbitalParameters? GetAsteroidChildOrbit()
    {
        if (Orbit.HasValue)
        {
            return OrbitalParameters.GetFromEccentricity(
                Orbit.Value.OrbitedMass,
                Orbit.Value.OrbitedPosition,
                Orbit.Value.Eccentricity);
        }

        return null;
    }

    private void ConfigureAsteroidFieldInstance(
        Vector3<HugeNumber> position,
        CosmicLocation? parent,
        HugeNumber? majorRadius = null,
        HugeNumber? minorRadius = null)
    {
        IShape<HugeNumber> shape;
        if (position != Vector3<HugeNumber>.Zero
            || parent is not StarSystem
            || !majorRadius.HasValue)
        {
            majorRadius ??= Randomizer.Instance.Next(new HugeNumber(1.5, 11), _AsteroidFieldSpace);
            shape = new Ellipsoid<HugeNumber>(
                majorRadius.Value,
                Randomizer.Instance.Next(
                    HugeNumberConstants.Half,
                    new HugeNumber(15, -1)) * majorRadius.Value,
                Randomizer.Instance.Next(
                    HugeNumberConstants.Half,
                    new HugeNumber(15, -1)) * majorRadius.Value,
                position);
        }
        else
        {
            shape = new Torus<HugeNumber>(majorRadius ?? HugeNumber.Zero, minorRadius ?? HugeNumber.Zero, position);
        }

        Material = new Material<HugeNumber>(
            Substances.All.InterplanetaryMedium,
            shape,
            shape.Volume * _AsteroidFieldDensity,
            null,
            parent?.Material.Temperature ?? UniverseAmbientTemperature);
    }

    private void ConfigureOortCloudInstance(
        Vector3<HugeNumber> position,
        CosmicLocation? parent,
        HugeNumber? radius = null) => Material = new Material<HugeNumber>(
            Substances.All.InterplanetaryMedium,
            new HollowSphere<HugeNumber>(
                _OortCloudInnerRadius + (radius ?? HugeNumber.Zero),
                _OortCloudSpace + (radius ?? HugeNumber.Zero),
                position),
            _OortCloudMass,
            null,
            parent?.Material.Temperature ?? UniverseAmbientTemperature);
}
