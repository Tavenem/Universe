﻿using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Data;
using System.Text;
using System.Text.Json.Serialization;
using Tavenem.Chemistry;
using Tavenem.DataStorage;
using Tavenem.Randomize;
using Tavenem.Time;
using Tavenem.Universe.Chemistry;
using Tavenem.Universe.Climate;
using Tavenem.Universe.Place;
using Tavenem.Universe.Space.Planetoids;

namespace Tavenem.Universe.Space;

/// <summary>
/// Any non-stellar celestial body, such as a planet or asteroid.
/// </summary>
public partial class Planetoid : CosmicLocation
{
    internal const double DefaultTerrestrialMaxDensity = 6000;
    internal const int GiantMaxDensity = 1650;

    /// <summary>
    /// Above this an object achieves hydrostatic equilibrium, and is considered a dwarf planet
    /// rather than an asteroid.
    /// </summary>
    private const double AsteroidMaxMassForType = 3.4e20;
    /// <summary>
    /// Below this a body is considered a meteoroid, rather than an asteroid.
    /// </summary>
    private const double AsteroidMinMassForType = 5.9e8;

    // polar latitude = ~1.476
    private const double CosPolarLatitude = 0.095;
    private const int DensityForDwarf = 2000;
    private const int GiantMinDensity = 1100;
    private const int GiantSubMinDensity = 600;

    /// <summary>
    /// The minimum radius required to achieve hydrostatic equilibrium, in meters.
    /// </summary>
    private const int MinimumRadius = 600000;

    private const PlanetType WaterlessPlanetTypes = PlanetType.Carbon
        | PlanetType.Iron
        | PlanetType.Lava
        | PlanetType.LavaDwarf;

    internal static readonly HugeNumber _GiantSpace = new(2.5, 8);

    private static readonly HugeNumber _AsteroidSpace = new(400000);
    private static readonly HugeNumber _CometSpace = new(25000);

    /// <summary>
    /// The minimum to achieve hydrostatic equilibrium and be considered a dwarf planet.
    /// </summary>
    private static readonly HugeNumber _DwarfMinMassForType = new(3.4, 20);

    private static readonly HugeNumber _DwarfSpace = new(1.5, 6);

    /// <summary>
    /// Below this limit the planet will not have sufficient mass to retain hydrogen, and will
    /// be a terrestrial planet.
    /// </summary>
    private static readonly HugeNumber _GiantMinMassForType = new(6, 25);

    private static readonly HugeNumber _IcyRingDensity = 300;

    /// <summary>
    /// An arbitrary limit separating rogue dwarf planets from rogue planets.
    /// Within orbital systems, a calculated value for clearing the neighborhood is used instead.
    /// </summary>
    private static readonly HugeNumber _DwarfMaxMassForType = new(6, 25);

    /// <summary>
    /// At around this limit the planet will have sufficient mass to sustain fusion, and become
    /// a brown dwarf.
    /// </summary>
    private static readonly HugeNumber _GiantMaxMassForType = new(2.5, 28);

    /// <summary>
    /// At around this limit the planet will have sufficient mass to retain hydrogen, and become
    /// a giant.
    /// </summary>
    private static readonly HugeNumber _TerrestrialMaxMassForType = new(6, 25);

    private static readonly HugeNumber _RockyRingDensity = 1380;

    /// <summary>
    /// An arbitrary limit separating rogue dwarf planets from rogue planets. Within orbital
    /// systems, a calculated value for clearing the neighborhood is used instead.
    /// </summary>
    private static readonly HugeNumber _TerrestrialMinMassForType = new(2, 22);

    private static readonly HugeNumber _TerrestrialSpace = new(1.75, 7);

    private readonly HabitabilityRequirements? _habitabilityRequirements;
    private readonly PlanetParams? _planetParams;

    private double? _averageSurfaceTemperature;
    private double? _diurnalTemperatureVariation;
    private double? _maxSurfaceTemperature;
    private double? _minSurfaceTemperature;

    /// <summary>
    /// The albedo of this planetoid's surface (a value between 0 and 1).
    /// </summary>
    /// <remarks>
    /// This refers to the albedo of the surface of the main body, not including any atmosphere.
    /// </remarks>
    private double _surfaceAlbedo;

    private double? _surfaceTemperature;

    /// <summary>
    /// The average albedo of this <see cref="Planetoid"/> (a value between 0 and 1).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Read-only; set with <see cref="SetAlbedo(double)"/>.
    /// </para>
    /// <para>
    /// This refers to the total albedo of the body, including any atmosphere, not just the surface
    /// albedo of the main body.
    /// </para>
    /// </remarks>
    public double Albedo { get; private set; }

    /// <summary>
    /// The angle between the Y-axis and the axis of rotation of this <see cref="Planetoid"/>.
    /// Values greater than π/2 indicate clockwise rotation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Read-only; set with <see cref="SetAngleOfRotation(double)"/>.
    /// </para>
    /// <para>
    /// Note that this is not the same as axial tilt if the <see cref="Planetoid"/> is in orbit; in
    /// that case axial tilt is relative to the normal of the orbital plane of the <see
    /// cref="Planetoid"/>, not the Y-axis.
    /// </para>
    /// </remarks>
    public double AngleOfRotation { get; private set; }

    private HugeNumber? _angularVelocity;
    /// <summary>
    /// The angular velocity of this <see cref="Planetoid"/>, in radians per second.
    /// </summary>
    /// <remarks>
    /// Read-only. Adjust via <see cref="SetRotationalPeriod(HugeNumber)"/>.
    /// </remarks>
    [JsonIgnore]
    public HugeNumber AngularVelocity
        => _angularVelocity ??= RotationalPeriod == HugeNumber.Zero
        ? HugeNumber.Zero
        : HugeNumberConstants.TwoPi / RotationalPeriod;

    private Atmosphere? _atmosphere;
    /// <summary>
    /// The atmosphere possessed by this <see cref="Planetoid"/>.
    /// </summary>
    [JsonIgnore]
    public Atmosphere Atmosphere
    {
        get => _atmosphere ??= new();
        private set => _atmosphere = value.IsEmpty ? null : value;
    }

    /// <summary>
    /// The composition of this planetoid's atmosphere, if any.
    /// </summary>
    public IReadOnlyDictionary<ISubstanceReference, decimal> AtmosphericComposition
        => _atmosphere?.Material.Constituents
        ?? ReadOnlyDictionary<ISubstanceReference, decimal>.Empty;

    /// <summary>
    /// The atmospheric pressure at the surface of this planetoid, in kPa.
    /// </summary>
    public double AtmosphericPressure => _atmosphere?.AtmosphericPressure ?? 0;

    /// <summary>
    /// The angle between the X-axis and the orbital vector at which the vernal equinox of the
    /// northern hemisphere occurs.
    /// </summary>
    /// <remarks>
    /// Read-only; set with <see cref="SetAxialPrecession(double)"/>.
    /// </remarks>
    public double AxialPrecession { get; private set; }

    private double? _axialTilt;
    /// <summary>
    /// The axial tilt of the <see cref="Planetoid"/> relative to its orbital plane, in radians.
    /// Values greater than π/2 indicate clockwise rotation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Read-only; set with <see cref="SetAxialTilt(double)"/>.
    /// </para>
    /// <para>
    /// If the <see cref="Planetoid"/> isn't orbiting anything, this is the same as the angle of
    /// rotation.
    /// </para>
    /// </remarks>
    [JsonIgnore]
    public double AxialTilt => _axialTilt ??= Orbit.HasValue ? AngleOfRotation - Orbit.Value.Inclination : AngleOfRotation;

    /// <summary>
    /// A <see cref="Vector3"/> which represents the axis of this <see cref="Planetoid"/>.
    /// </summary>
    /// <remarks>
    /// Read-only. Set with <see cref="SetAxialTilt(double)"/> or <see
    /// cref="SetAngleOfRotation(double)"/>.
    /// </remarks>
    [JsonIgnore]
    public Vector3 Axis { get; private set; } = Vector3.UnitY;

    /// <summary>
    /// A <see cref="Quaternion"/> representing the rotation of the axis of this <see
    /// cref="Planetoid"/>.
    /// </summary>
    /// <remarks>
    /// Read-only; set with <see cref="SetAxialTilt(double)"/> or <see
    /// cref="SetAngleOfRotation(double)"/>
    /// </remarks>
    [JsonIgnore]
    public Quaternion AxisRotation { get; private set; } = Quaternion.Identity;

    /// <summary>
    /// The blackbody temperature of this planetoid. Read-only.
    /// </summary>
    /// <remarks>
    /// Determined by proximity to nearby stars, and those stars' properties.
    /// </remarks>
    public double BlackbodyTemperature { get; private set; }

    /// <summary>
    /// Whether or not this planet has a native population of living organisms.
    /// </summary>
    /// <remarks>
    /// The complexity of life is not presumed. If a planet is basically habitable (liquid
    /// surface water), life in at least a single-celled form may be indicated, and may affect
    /// the atmospheric composition.
    /// </remarks>
    public bool HasBiosphere { get; private set; }

    /// <summary>
    /// Whether this planet has a strong magnetosphere.
    /// </summary>
    public bool HasMagnetosphere { get; private set; }

    private IMaterial<HugeNumber>? _hydrosphere;
    /// <summary>
    /// This planet's surface liquids and ices (not necessarily water).
    /// </summary>
    /// <remarks>
    /// Represented as a separate <see cref="IMaterial{HugeNumber}"/> rather than as a top layer of <see
    /// cref="CosmicLocation.Material"/> for ease of reference to both the solid surface
    /// layer, and the hydrosphere.
    /// </remarks>
    public IMaterial<HugeNumber> Hydrosphere
    {
        get => _hydrosphere ??= new Material<HugeNumber>();
        private set => _hydrosphere = value.IsEmpty ? null : value;
    }

    /// <summary>
    /// The type discriminator for this type.
    /// </summary>
    public const string PlanetoidIdItemTypeName = ":Location:CosmicLocation:Planetoid:";
    /// <summary>
    /// A built-in, read-only type discriminator.
    /// </summary>
    [JsonInclude, JsonPropertyOrder(-1)]
    public override string IdItemTypeName
    {
        get => PlanetoidIdItemTypeName;
        set { }
    }

    /// <summary>
    /// Indicates whether this is an asteroid.
    /// </summary>
    [JsonIgnore]
    public bool IsAsteroid => PlanetType.Asteroid.HasFlag(PlanetType);

    /// <summary>
    /// Indicates whether this is a dwarf planet.
    /// </summary>
    [JsonIgnore]
    public bool IsDwarf => PlanetType.AnyDwarf.HasFlag(PlanetType);

    /// <summary>
    /// Indicates whether this is a giant planet (including ice giants).
    /// </summary>
    [JsonIgnore]
    public bool IsGiant => PlanetType.Giant.HasFlag(PlanetType);

    /// <summary>
    /// Whether this planet is inhospitable to life. Read-only.
    /// </summary>
    /// <remarks>
    /// Determined by stellar environment. Typically due to a highly energetic or volatile star,
    /// which either produces a great deal of ionizing radiation, or has a rapidly shifting
    /// habitable zone, or both.
    /// </remarks>
    public bool IsInhospitable { get; private set; }

    /// <summary>
    /// Indicates whether this is a terrestrial planet.
    /// </summary>
    [JsonIgnore]
    public bool IsTerrestrial => PlanetType.AnyTerrestrial.HasFlag(PlanetType);

    private double? _maxElevation;
    /// <summary>
    /// <para>
    /// The maximum elevation of this planet's surface topology, relative to its average
    /// surface, based on the strength of its gravity.
    /// </para>
    /// <para>
    /// Note that this is a theoretical maximum, not the highest actual point on the surface. A
    /// given body's highest point may be substantially lower than its possible maximum. Highest
    /// peaks less than half of the potential maximum are more common than not.
    /// </para>
    /// <para>
    /// Note also that local elevations are given relative to sea level, rather than to the
    /// average surface. This means local elevations may exceed this value on planets with low
    /// sea levels, and that planets with high sea levels may have no points with elevations
    /// even close to this value.
    /// </para>
    /// </summary>
    [JsonIgnore]
    public double MaxElevation => _maxElevation ??= (IsGiant || PlanetType == PlanetType.Ocean ? 0 : 200000 / (double)SurfaceGravity);

    /// <summary>
    /// The elevation of sea level relative to the mean surface elevation of the planet, as a
    /// fraction of <see cref="MaxElevation"/>.
    /// </summary>
    public double NormalizedSeaLevel { get; private set; }

    /// <summary>
    /// The type of <see cref="Planetoid"/>. Read-only. Set with <see
    /// cref="SetPlanetTypeAsync(IDataStore, PlanetType)"/>.
    /// </summary>
    public PlanetType PlanetType { get; private set; }

    private IReadOnlyList<PlanetaryRing>? _rings;
    /// <summary>
    /// The collection of rings around this <see cref="Planetoid"/>.
    /// </summary>
    public IReadOnlyList<PlanetaryRing> Rings => _rings
        ?? [];

    /// <summary>
    /// <para>
    /// The length of time it takes for this planet to rotate once about its axis, in seconds.
    /// </para>
    /// <para>
    /// Read-only. Set with <see cref="SetRotationalPeriod(HugeNumber)"/>.
    /// </para>
    /// </summary>
    public HugeNumber RotationalPeriod { get; private set; }

    private List<Resource>? _resources;
    /// <summary>
    /// The resources of this <see cref="Planetoid"/>.
    /// </summary>
    public IReadOnlyList<Resource> Resources => _resources?.AsReadOnly()
        ?? ReadOnlyCollection<Resource>.Empty;

    private IReadOnlyList<string>? _satelliteIds;
    /// <summary>
    /// The IDs of any natural satellites which orbit this <see cref="Planetoid"/>.
    /// </summary>
    public IReadOnlyList<string> SatelliteIds => _satelliteIds
        ?? [];

    private double? _seaLevel;
    /// <summary>
    /// The elevation of sea level relative to the mean surface elevation of the planet, in
    /// meters.
    /// </summary>
    [JsonIgnore]
    public double SeaLevel => _seaLevel ??= NormalizedSeaLevel * MaxElevation;

    /// <summary>
    /// A value which can be used to deterministically generate random values for this <see
    /// cref="Planetoid"/>.
    /// </summary>
    public uint Seed { get; private set; }

    /// <summary>
    /// A set of seed values which can be used to deterministically generate random values for this
    /// <see cref="Planetoid"/>.
    /// </summary>
    [JsonIgnore]
    public SeedArray SeedArray { get; private set; }

    private double? _summerSolsticeTrueAnomaly;
    /// <summary>
    /// <para>
    /// The true anomaly of this planet's orbit at the summer solstice of the northern
    /// hemisphere.
    /// </para>
    /// <para>
    /// Will be zero for a planet not in orbit.
    /// </para>
    /// </summary>
    [JsonIgnore]
    public double SummerSolsticeTrueAnomaly
        => _summerSolsticeTrueAnomaly ??= (DoubleConstants.HalfPi
        - (Orbit?.LongitudeOfPeriapsis ?? 0))
        % DoubleConstants.TwoPi;

    /// <summary>
    /// The surface temperature of this planetoid at the apoapsis of its orbit.
    /// </summary>
    public double SurfaceTemperatureAtApoapsis { get; private set; }

    /// <summary>
    /// The surface temperature of this planetoid at the periapsis of its orbit.
    /// </summary>
    public double SurfaceTemperatureAtPeriapsis { get; private set; }

    private string? _typeName;
    /// <inheritdoc />
    [JsonIgnore]
    public override string TypeName
    {
        get
        {
            if (string.IsNullOrEmpty(_typeName))
            {
                var sb = new StringBuilder();
                if (!string.IsNullOrEmpty(TypeNamePrefix))
                {
                    sb.Append(TypeNamePrefix).Append(' ');
                }
                sb.Append(BaseTypeName);
                if (!string.IsNullOrEmpty(TypeNameSuffix))
                {
                    sb.Append(' ').Append(TypeNameSuffix);
                }
                _typeName = sb.ToString();
            }
            return _typeName;
        }
    }

    private double? _winterSolsticeTrueAnomaly;
    /// <summary>
    /// <para>
    /// The true anomaly of this planet's orbit at the winter solstice of the northern
    /// hemisphere.
    /// </para>
    /// <para>
    /// Will be zero for a planet not in orbit.
    /// </para>
    /// </summary>
    [JsonIgnore]
    public double WinterSolsticeTrueAnomaly
        => _winterSolsticeTrueAnomaly ??= (DoubleConstants.ThreeHalvesPi
        - (Orbit?.LongitudeOfPeriapsis ?? 0))
        % DoubleConstants.TwoPi;

    /// <summary>
    /// A name for this main type of this planetoid.
    /// </summary>
    protected string BaseTypeName => PlanetType switch
    {
        PlanetType.AsteroidC => "C-Type Asteroid",
        PlanetType.AsteroidM => "M-Type Asteroid",
        PlanetType.AsteroidS => "S-Type Asteroid",
        PlanetType.Comet => "Comet",
        PlanetType.Dwarf => "Dwarf Planet",
        PlanetType.LavaDwarf => "Dwarf Planet",
        PlanetType.RockyDwarf => "Dwarf Planet",
        PlanetType.GasGiant => "Gas Giant",
        PlanetType.IceGiant => "Ice Giant",
        _ => "Planet",
    };

    /// <summary>
    /// A prefix to the <see cref="BaseTypeName"/> for this planetoid.
    /// </summary>
    protected string? TypeNamePrefix => PlanetType switch
    {
        PlanetType.LavaDwarf => "Lava",
        PlanetType.RockyDwarf => "Rocky",
        PlanetType.Terrestrial => "Terrestrial",
        PlanetType.Carbon => "Carbon",
        PlanetType.Iron => "Iron",
        PlanetType.Lava => "Lava",
        PlanetType.Ocean => "Ocean",
        _ => null,
    };

    /// <summary>
    /// A suffix to the <see cref="BaseTypeName"/> for this planetoid.
    /// </summary>
    protected string? TypeNameSuffix => PlanetType switch
    {
        PlanetType.AsteroidC => "c",
        PlanetType.AsteroidM => "m",
        PlanetType.AsteroidS => "s",
        _ => null,
    };

    /// <summary>
    /// The total temperature of this <see cref="Planetoid"/>, not including atmospheric
    /// effects, averaged over its orbit, in K.
    /// </summary>
    internal double AverageBlackbodyTemperature { get; private set; }

    internal double? GreenhouseEffect { get; set; }

    private double? _insolationFactor_Equatorial;
    internal double InsolationFactor_Equatorial
    {
        get => _insolationFactor_Equatorial ??= GetInsolationFactor();
        set => _insolationFactor_Equatorial = value;
    }

    private double? _insolationFactor_Polar;
    private double InsolationFactor_Polar => _insolationFactor_Polar ??= GetInsolationFactor(true);

    private double? _lapseRateDry;
    private double LapseRateDry => _lapseRateDry ??= (double)SurfaceGravity / DoubleConstants.CpDryAir;

    /// <summary>
    /// Initializes a new instance of <see cref="Planetoid"/> with the given parameters.
    /// </summary>
    /// <param name="planetType">The type of planet to generate.</param>
    /// <param name="parent">
    /// The containing parent location for the new planet (if any).
    /// </param>
    /// <param name="star">
    /// <para>
    /// The star the new <see cref="Planetoid"/> will orbit.
    /// </para>
    /// <para>
    /// If omitted, and <paramref name="orbit"/> is also <see langword="null"/>, a star will be
    /// selected at random from among the provided <paramref name="stars"/>.
    /// </para>
    /// </param>
    /// <param name="stars">
    /// The collection of stars in the local system.
    /// </param>
    /// <param name="position">The position for the child.</param>
    /// <param name="satellites">
    /// <para>
    /// When this method returns, will be set to a <see cref="List{T}"/> of <see cref="Planetoid"/>s
    /// containing any satellites generated for the planet during the creation process.
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
    /// <param name="planetParams">An optional set of <see cref="PlanetParams"/>.</param>
    /// <param name="habitabilityRequirements">
    /// An optional set of <see cref="HabitabilityRequirements"/>.
    /// </param>
    /// <param name="satelliteOf">
    /// If not <see langword="null"/>, the <see cref="Planetoid"/> which this one is to orbit as a
    /// satellite.
    /// </param>
    /// <param name="id">An optional <see cref="IIdItem.Id"/> to assign to the new planet.</param>
    public Planetoid(
        PlanetType planetType,
        CosmicLocation? parent,
        Star? star,
        List<Star>? stars,
        Vector3<HugeNumber> position,
        out List<Planetoid> satellites,
        OrbitalParameters? orbit = null,
        PlanetParams? planetParams = null,
        HabitabilityRequirements? habitabilityRequirements = null,
        Planetoid? satelliteOf = null,
        string? id = null) : base(id, parent?.Id, CosmicStructureType.Planetoid)
    {
        PlanetType = planetType;
        _planetParams = planetParams;
        _habitabilityRequirements = habitabilityRequirements;

        if (star is null
            && stars is not null
            && !orbit.HasValue)
        {
            star = Randomizer.Instance.Next(stars);
        }

        satellites = Configure(parent, stars, star, position, satelliteOf is not null, orbit);

        if (parent is not null && !orbit.HasValue && !Orbit.HasValue)
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
                    CosmicStructureType.StarSystem => parent is StarSystem && Position != Vector3<HugeNumber>.Zero
                        ? OrbitalParameters.GetFromEccentricity((star ?? parent).Mass, (star ?? parent).Position, Randomizer.Instance.PositiveNormalDistributionSample(0, 0.05))
                        : null,
                    _ => null,
                };
            }
        }
        if (orbit.HasValue && !Orbit.HasValue)
        {
            Space.Orbit.AssignOrbit(
                this,
                satelliteOf?.Id
                    ?? star?.Id
                    ?? (parent?.Orbit.HasValue == true
                        ? parent?.Id
                        : null),
                orbit.Value);
        }
    }

    /// <summary>
    /// Initialize a new instance of <see cref="Planetoid"/>.
    /// </summary>
    /// <param name="id">
    /// The unique ID of this item.
    /// </param>
    /// <param name="seed">
    /// A value which can be used to deterministically generate random values for this planetoid.
    /// </param>
    /// <param name="planetType">
    /// The <see cref="PlanetType"/> of this planetoid.
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
    /// <param name="albedo">
    /// <para>
    /// The average albedo of this planetoid (a value between 0 and 1).
    /// </para>
    /// <para>
    /// This refers to the total albedo of the body, including any atmosphere, not just the surface
    /// albedo of the main body.
    /// </para>
    /// </param>
    /// <param name="angleOfRotation">
    /// The angle between the Y-axis and the axis of rotation of this planetoid. Values greater than
    /// π/2 indicate clockwise rotation.
    /// </param>
    /// <param name="atmosphericComposition">
    /// The composition of this planetoid's atmosphere, if any.
    /// </param>
    /// <param name="atmosphericPressure">
    /// The atmospheric pressure at the surface of this planetoid, in kPa.
    /// </param>
    /// <param name="axialPrecession">
    /// The angle between the X-axis and the orbital vector at which the vernal equinox of the
    /// northern hemisphere occurs.
    /// </param>
    /// <param name="blackbodyTemperature">
    /// The blackbody temperature of this planetoid.
    /// </param>
    /// <param name="hasBiosphere">
    /// Indicates whether or not this planet has a native population of living organisms.
    /// </param>
    /// <param name="hasMagnetosphere">
    /// Whether this planetoid has a strong magnetosphere.
    /// </param>
    /// <param name="hydrosphere">
    /// This planet's surface liquids and ices (not necessarily water).
    /// </param>
    /// <param name="isInhospitable">
    /// <para>
    /// Whether this planetoid is inhospitable to life.
    /// </para>
    /// <para>
    /// Typically due to a highly energetic or volatile star, which either produces a great deal of
    /// ionizing radiation, or has a rapidly shifting habitable zone, or both.
    /// </para>
    /// </param>
    /// <param name="normalizedSeaLevel">
    /// The elevation of sea level relative to the mean surface elevation of the planet, as a
    /// fraction of <see cref="MaxElevation"/>.
    /// </param>
    /// <param name="resources">The resources of this planetoid.</param>
    /// <param name="rings">
    /// The collection of rings around this planetoid.
    /// </param>
    /// <param name="rotationalPeriod">
    /// The length of time it takes for this planetoid to rotate once about its axis, in seconds.
    /// </param>
    /// <param name="satelliteIds">
    /// The IDs of any natural satellites which orbit this planetoid.
    /// </param>
    /// <param name="surfaceTemperatureAtApoapsis">
    /// The surface temperature of this planetoid at the apoapsis of its orbit.
    /// </param>
    /// <param name="surfaceTemperatureAtPeriapsis">
    /// The surface temperature of this planetoid at the periapsis of its orbit.
    /// </param>
    /// <remarks>
    /// Note: this constructor is most useful for deserialization. Consider using one of the
    /// <c>GetPlanet</c>... static methods to generate a new instance instead.
    /// </remarks>
    [JsonConstructor]
    public Planetoid(
        string id,
        uint seed,
        PlanetType planetType,
        string? parentId,
        Vector3<HugeNumber>[]? absolutePosition,
        string? name,
        Vector3<HugeNumber> velocity,
        Orbit? orbit,
        IMaterial<HugeNumber> material,
        double albedo,
        double angleOfRotation,
        IReadOnlyDictionary<ISubstanceReference, decimal> atmosphericComposition,
        double atmosphericPressure,
        double axialPrecession,
        double blackbodyTemperature,
        bool hasBiosphere,
        bool hasMagnetosphere,
        IMaterial<HugeNumber> hydrosphere,
        bool isInhospitable,
        double normalizedSeaLevel,
        IReadOnlyList<Resource> resources,
        IReadOnlyList<PlanetaryRing> rings,
        HugeNumber rotationalPeriod,
        IReadOnlyList<string> satelliteIds,
        double surfaceTemperatureAtApoapsis,
        double surfaceTemperatureAtPeriapsis)
        : base(
            id,
            CosmicStructureType.Planetoid,
            parentId,
            absolutePosition,
            name,
            velocity,
            orbit,
            material)
    {
        PlanetType = planetType;
        Material = material;
        Albedo = albedo;
        AngleOfRotation = angleOfRotation;
        AxialPrecession = axialPrecession;
        BlackbodyTemperature = blackbodyTemperature;
        HasBiosphere = hasBiosphere;
        HasMagnetosphere = hasMagnetosphere;
        Hydrosphere = hydrosphere;
        IsInhospitable = isInhospitable;
        NormalizedSeaLevel = normalizedSeaLevel;
        _resources = resources.Count == 0
            ? null
            : [.. resources];
        _rings = rings.Count == 0
            ? null
            : rings;
        RotationalPeriod = rotationalPeriod;
        _satelliteIds = satelliteIds.Count == 0
            ? null
            : satelliteIds;
        SurfaceTemperatureAtApoapsis = surfaceTemperatureAtApoapsis;
        SurfaceTemperatureAtPeriapsis = surfaceTemperatureAtPeriapsis;

        AverageBlackbodyTemperature = Orbit.HasValue
            ? ((SurfaceTemperatureAtPeriapsis * (1 + Orbit.Value.Eccentricity)) + (SurfaceTemperatureAtApoapsis * (1 - Orbit.Value.Eccentricity))) / 2
            : BlackbodyTemperature;

        Atmosphere = new(this, atmosphericPressure, atmosphericComposition);

        Seed = seed;
        var randomizer = new Randomizer(Seed);
        var seedArray = new SeedArray();
        for (var i = 0; i < 5; i++)
        {
            seedArray[i] = randomizer.NextInclusive();
        }
        SeedArray = seedArray;

        SetAxis();
    }

    /// <summary>
    /// Offsets the given latitude by the given solar declination.
    /// </summary>
    /// <param name="latitude">A latitude, in radians.</param>
    /// <param name="solarDeclination">A solar declination, in radians.</param>
    /// <returns>The offset latitude, adjusted to within ±π/2.</returns>
    public static double GetSeasonalLatitudeFromDeclination(double latitude, double solarDeclination)
    {
        var seasonalLatitude = latitude + solarDeclination;
        if (seasonalLatitude > DoubleConstants.HalfPi)
        {
            return Math.PI - seasonalLatitude;
        }
        else if (seasonalLatitude < -DoubleConstants.HalfPi)
        {
            return -seasonalLatitude - Math.PI;
        }
        return seasonalLatitude;
    }

    internal static double GetSeasonalProportionFromAnnualProportion(double proportionOfYear, double latitude, double axialTilt)
    {
        if (proportionOfYear > 0.5)
        {
            proportionOfYear = 1 - proportionOfYear;
        }
        proportionOfYear *= 2;
        if (latitude < 0)
        {
            proportionOfYear = 1 - proportionOfYear;
        }

        var absLat = Math.Abs(latitude);
        if (absLat < axialTilt)
        {
            var maximum = 1 - ((axialTilt - absLat) / (axialTilt * 2));
            proportionOfYear = 1 - (Math.Abs(proportionOfYear - maximum) / maximum);
        }

        return proportionOfYear;
    }

    /// <summary>
    /// Removes this location and all contained children, as well as all satellites, from the
    /// given data store.
    /// </summary>
    public override async Task<bool> DeleteAsync(IDataStore dataStore)
    {
        var success = true;
        await foreach (var child in GetSatellitesAsync(dataStore))
        {
            success &= await child.DeleteAsync(dataStore);
        }
        if (!string.IsNullOrEmpty(Orbit?.OrbitedId)
            && await dataStore.GetItemAsync(
                Orbit.Value.OrbitedId,
                UniverseSourceGenerationContext.Default.CosmicLocation) is Planetoid planet)
        {
            planet.RemoveSatellite(Id);
            success &= await dataStore.StoreItemAsync(planet);
        }
        return await base.DeleteAsync(dataStore) && success;
    }

    /// <summary>
    /// <para>
    /// The average surface temperature of the <see cref="Planetoid"/> near its equator
    /// throughout its orbit (or at its current position, if it is not in orbit), in K.
    /// </para>
    /// <para>
    /// Note that this is a calculated value, and does not take any custom temperature maps into
    /// account.
    /// </para>
    /// </summary>
    public double GetAverageSurfaceTemperature()
    {
        if (!_averageSurfaceTemperature.HasValue)
        {
            var avgBlackbodyTemp = AverageBlackbodyTemperature;
            var greenhouseEffect = GetGreenhouseEffect();
            _averageSurfaceTemperature = (avgBlackbodyTemp * InsolationFactor_Equatorial) + greenhouseEffect;
        }
        return _averageSurfaceTemperature.Value;
    }

    /// <summary>
    /// Calculates the distance along the surface at sea level between the two points indicated
    /// by the given normalized position vectors.
    /// </summary>
    /// <param name="position1">The first normalized position vector.</param>
    /// <param name="position2">The first normalized position vector.</param>
    /// <returns>The approximate distance between the points, in meters.</returns>
    /// <remarks>
    /// The distance is calculated as if the <see cref="Planetoid"/> was a sphere using a
    /// great circle formula, which will lead to greater inaccuracy the more ellipsoidal the
    /// shape of the <see cref="Planetoid"/>.
    /// </remarks>
    public double GetDistance(Vector3<HugeNumber> position1, Vector3<HugeNumber> position2)
        => (double)Shape.ContainingRadius * Math.Atan2((double)position1.Dot(position2), (double)position1.Cross(position2).Length());

    /// <summary>
    /// Calculates the distance along the surface at sea level between the two points indicated
    /// by the given normalized position vectors.
    /// </summary>
    /// <param name="position1">The first normalized position vector.</param>
    /// <param name="position2">The first normalized position vector.</param>
    /// <returns>The approximate distance between the points, in meters.</returns>
    /// <remarks>
    /// The distance is calculated as if the <see cref="Planetoid"/> was a sphere using a
    /// great circle formula, which will lead to greater inaccuracy the more ellipsoidal the
    /// shape of the <see cref="Planetoid"/>.
    /// </remarks>
    public double GetDistance(Vector3<double> position1, Vector3<double> position2)
        => (double)Shape.ContainingRadius * Math.Atan2(position1.Dot(position2), position1.Cross(position2).Length());

    /// <summary>
    /// Calculates the distance along the surface at sea level between two points.
    /// </summary>
    /// <param name="latitude1">The latitude of the first point.</param>
    /// <param name="longitude1">The longitude of the first point.</param>
    /// <param name="latitude2">The latitude of the second point.</param>
    /// <param name="longitude2">The longitude of the second point.</param>
    /// <returns>The approximate distance between the points, in meters.</returns>
    /// <remarks>
    /// The distance is calculated as if the <see cref="Planetoid"/> was a sphere using a
    /// great circle formula, which will lead to greater inaccuracy the more ellipsoidal the
    /// shape of the <see cref="Planetoid"/>.
    /// </remarks>
    public double GetDistance(double latitude1, double longitude1, double latitude2, double longitude2)
        => GetDistance(LatitudeAndLongitudeToVector(latitude1, longitude1), LatitudeAndLongitudeToVector(latitude2, longitude2));

    /// <summary>
    /// Get the diurnal temperature variation on this planet, in K.
    /// </summary>
    /// <returns>The diurnal temperature variation on this planet, in K.</returns>
    public double GetDiurnalTemperatureVariation()
    {
        if (!_diurnalTemperatureVariation.HasValue)
        {
            var temp = Temperature;
            var timeFactor = (double)(1 - ((RotationalPeriod - 2500) / 595000)).Clamp(0, 1);
            var blackbodyTemp = AverageBlackbodyTemperature;
            var greenhouseEffect = GetGreenhouseEffect();
            var darkSurfaceTemp = (((blackbodyTemp * InsolationFactor_Equatorial) - temp) * timeFactor)
                + temp
                + greenhouseEffect;
            _diurnalTemperatureVariation = GetAverageSurfaceTemperature() - darkSurfaceTemp;
        }
        return _diurnalTemperatureVariation.Value;
    }

    /// <summary>
    /// Gets the greenhouse effect of this planet's atmosphere.
    /// </summary>
    /// <returns>The greenhouse effect of this planet's atmosphere, in K.</returns>
    public double GetGreenhouseEffect()
    {
        GreenhouseEffect ??= GetGreenhouseEffect(InsolationFactor_Equatorial, Atmosphere.GreenhouseFactor);
        return GreenhouseEffect.Value;
    }

    /// <summary>
    /// Calculates the total illumination on the given position, as well as the light reflected
    /// from any natural satellites, modified according to the angle of incidence at the given
    /// time, in lux (lumens per m²), assuming a single sun-like star at <see
    /// cref="Vector3{TScalar}.Zero"/>.
    /// </summary>
    /// <param name="dataStore">
    /// The <see cref="IDataStore"/> from which to retrieve instances.
    /// </param>
    /// <param name="moment">The time at which to make the calculation.</param>
    /// <param name="latitude">The latitude at which to make the calculation.</param>
    /// <param name="longitude">The longitude at which to make the calculation.</param>
    /// <param name="satellites">Any satellites of this body.</param>
    /// <returns>The total illumination on the body, in lux (lumens per m²).</returns>
    /// <remarks>
    /// <para>
    /// A conversion of 0.0079 W/m² per lux is used, which is roughly accurate for the sun, but
    /// may not be as precise for other stellar bodies.
    /// </para>
    /// <para>
    /// This method modifies total illumination based on an angle of incidence calculated from
    /// the assumed star, or by the body it orbits (in the case of satellites). To get a value
    /// which accounts for the actual stars in the local system, use <see
    /// cref="GetIlluminationAsync(IDataStore, Instant, double, double)"/>.
    /// </para>
    /// </remarks>
    public async ValueTask<double> GetIlluminationAsync(
        IDataStore dataStore,
        Instant moment,
        double latitude,
        double longitude,
        IEnumerable<Planetoid>? satellites = null)
    {
        var position = await GetPositionAtTimeAsync(dataStore, moment);

        var distance = position.Length();
        var (_, eclipticLongitude) = GetEclipticLatLon(position, Vector3<HugeNumber>.Zero);
        var lux = 0.0;

        var (solarRightAscension, solarDeclination) = GetRightAscensionAndDeclination(position, Vector3<HugeNumber>.Zero);
        var longitudeOffset = longitude - solarRightAscension;
        if (longitudeOffset > Math.PI)
        {
            longitudeOffset -= DoubleConstants.TwoPi;
        }

        var sinSolarElevation = (Math.Sin(solarDeclination) * Math.Sin(latitude))
            + (Math.Cos(solarDeclination) * Math.Cos(latitude) * Math.Cos(longitudeOffset));
        var solarElevation = Math.Asin(sinSolarElevation);
        var star = Star.NewSunlike(null, Vector3<HugeNumber>.Zero);
        lux += solarElevation <= 0 || star is null ? 0 : GetLuminousFlux([star]) * sinSolarElevation;

        if (satellites is not null)
        {
            foreach (var satellite in satellites)
            {
                var satellitePosition = await satellite.GetPositionAtTimeAsync(dataStore, moment);
                var satelliteDistance2 = Vector3<HugeNumber>.DistanceSquared(position, satellitePosition);
                var satelliteDistance = satelliteDistance2.Sqrt();

                var (satLat, satLon) = GetEclipticLatLon(position, satellitePosition);

                var phase = 0.0;
                // satellite-centered elongation of the planet from the star (ratio of illuminated
                // surface area to total surface area)
                var le = Math.Acos(Math.Cos(satLat) * Math.Cos(eclipticLongitude - satLon));
                var e = Math.Atan2((double)(satelliteDistance - (distance * Math.Cos(le))), (double)(distance * Math.Sin(le)));
                // fraction of illuminated surface area
                phase = Math.Max(phase, (1 + Math.Cos(e)) / 2);

                // Total light from the satellite is the flux incident on the satellite, reduced
                // according to the proportion lit (vs. shadowed), further reduced according to the
                // albedo, then the distance the light must travel after being reflected.
                lux += star is null
                    ? 0
                    : satellite.GetLuminousFlux([star])
                        * phase
                        * satellite.Albedo
                        / DoubleConstants.FourPi
                        / (double)satelliteDistance2;
            }
        }

        return lux;
    }

    /// <summary>
    /// Calculates the total illumination on the given position from nearby sources of light
    /// (stars in the same system), as well as the light reflected from any natural satellites,
    /// modified according to the angle of incidence at the given time, in lux (lumens per m²).
    /// </summary>
    /// <param name="dataStore">
    /// The <see cref="IDataStore"/> from which instances may be retrieved.
    /// </param>
    /// <param name="moment">The time at which to make the calculation.</param>
    /// <param name="latitude">The latitude at which to make the calculation.</param>
    /// <param name="longitude">The longitude at which to make the calculation.</param>
    /// <returns>The total illumination on the body, in lux (lumens per m²).</returns>
    /// <remarks>
    /// <para>
    /// A conversion of 0.0079 W/m² per lux is used, which is roughly accurate for the sun, but
    /// may not be as precise for other stellar bodies.
    /// </para>
    /// <para>
    /// This method modifies total illumination based on an angle of incidence calculated from
    /// the star orbited by this body, or by the body it orbits (in the case of satellites).
    /// This will be accurate for single-star systems, and will be roughly accurate for binary
    /// or multi-star systems where the secondary stars are either very distant compared to the
    /// main, orbited star (and hence contribute little to overall illumination), or else are
    /// very close to the main star relative to the body (and hence share a similar angle of
    /// incidence). In multi-star systems where the stellar bodies are close enough to the body
    /// to contribute significantly to total illumination, but have significantly different
    /// positions (and hence, angles of incidence), this method's results will be significantly
    /// less accurate. Such systems should be rare, however, as multi-star systems, by default,
    /// are generated in either of the two configurations described above which produce
    /// reasonable results.
    /// </para>
    /// </remarks>
    public async Task<double> GetIlluminationAsync(IDataStore dataStore, Instant moment, double latitude, double longitude)
    {
        var system = await GetStarSystemAsync(dataStore);
        if (system is null)
        {
            return 0;
        }

        var position = await GetPositionAtTimeAsync(dataStore, moment);

        var stars = new List<(Star star, Vector3<HugeNumber> position, HugeNumber distance, double eclipticLongitude)>();
        await foreach (var star in system.GetStarsAsync(dataStore))
        {
            var starPosition = await star.GetPositionAtTimeAsync(dataStore, moment);
            var (_, eclipticLongitude) = GetEclipticLatLon(position, starPosition);
            stars.Add((
                star,
                starPosition,
                Vector3<HugeNumber>.Distance(position, starPosition),
                eclipticLongitude));
        }
        if (stars.Count == 0)
        {
            return 0;
        }
        var lux = 0.0;

        foreach (var (star, starPosition, _, _) in stars)
        {
            var (solarRightAscension, solarDeclination) = GetRightAscensionAndDeclination(position, starPosition);
            var longitudeOffset = longitude - solarRightAscension;
            if (longitudeOffset > Math.PI)
            {
                longitudeOffset -= DoubleConstants.TwoPi;
            }

            var sinSolarElevation = (Math.Sin(solarDeclination) * Math.Sin(latitude))
                + (Math.Cos(solarDeclination) * Math.Cos(latitude) * Math.Cos(longitudeOffset));
            var solarElevation = Math.Asin(sinSolarElevation);
            lux += solarElevation <= 0 ? 0 : GetLuminousFlux(stars.Select(x => x.star)) * sinSolarElevation;
        }

        await foreach (var satellite in GetSatellitesAsync(dataStore))
        {
            var satellitePosition = await satellite.GetPositionAtTimeAsync(dataStore, moment);
            var satelliteDistance2 = Vector3<HugeNumber>.DistanceSquared(position, satellitePosition);
            var satelliteDistance = satelliteDistance2.Sqrt();

            var (satLat, satLon) = GetEclipticLatLon(position, satellitePosition);

            var phase = 0.0;
            foreach (var (star, starPosition, starDistance, eclipticLongitude) in stars)
            {
                // satellite-centered elongation of the planet from the star (ratio of illuminated
                // surface area to total surface area)
                var le = Math.Acos(Math.Cos(satLat) * Math.Cos(eclipticLongitude - satLon));
                var e = Math.Atan2((double)(satelliteDistance - (starDistance * Math.Cos(le))), (double)(starDistance * Math.Sin(le)));
                // fraction of illuminated surface area
                phase = Math.Max(phase, (1 + Math.Cos(e)) / 2);
            }

            // Total light from the satellite is the flux incident on the satellite, reduced
            // according to the proportion lit (vs. shadowed), further reduced according to the
            // albedo, then the distance the light must travel after being reflected.
            lux += satellite.GetLuminousFlux(stars.Select(x => x.star))
                * phase
                * satellite.Albedo
                / DoubleConstants.FourPi
                / (double)satelliteDistance2;
        }

        return lux;
    }

    /// <summary>
    /// Given an initial position, a bearing, and a distance, calculates the final position along
    /// the great circle arc described by the resulting motion.
    /// </summary>
    /// <param name="latitude">An initial latitude.</param>
    /// <param name="longitude">An initial longitude.</param>
    /// <param name="distance">A distance, in meters.</param>
    /// <param name="bearing">A bearing, in radians clockwise from north.</param>
    /// <returns>The destination latitude and longitude.</returns>
    /// <remarks>
    /// <para>
    /// The results are inaccurate for highly ellipsoidal planets, as no compensation is attempted
    /// for the non-spherical shape of the planet.
    /// </para>
    /// <para>
    /// Great circle arcs are the shortest distance between two points on a sphere. Traveling along
    /// a great circle that is not the equator or a meridian requires continually changing one's
    /// compass heading during travel (unlike a rhumb line, which is not the shortest path, but
    /// requires no bearing adjustments).
    /// </para>
    /// <seealso cref="GetLatLonAtDistanceOnRhumbLine(double, double, HugeNumber, double)"/>
    /// </remarks>
    public (double latitude, double longitude) GetLatLonAtDistanceOnGreatCircleArc(
        double latitude,
        double longitude,
        HugeNumber distance,
        double bearing)
    {
        var angularDistance = (double)(distance / Shape.ContainingRadius);
        var sinDistance = Math.Sin(angularDistance);
        var cosDistance = Math.Cos(angularDistance);
        var sinLatitude = Math.Sin(latitude);
        var cosLatitude = Math.Cos(latitude);
        var finalLatitude = Math.Asin((sinLatitude * cosDistance) + (cosLatitude * sinDistance * Math.Cos(bearing)));
        var finalLongitude = longitude + Math.Atan2(Math.Sin(bearing) * sinDistance * cosLatitude, cosDistance - (sinLatitude * Math.Sin(finalLatitude)));
        finalLongitude = ((finalLongitude + DoubleConstants.ThreeHalvesPi) % DoubleConstants.TwoPi) - DoubleConstants.HalfPi;
        return (finalLatitude, finalLongitude);
    }

    /// <summary>
    /// Given an initial position, a bearing, and a distance, calculates the final position
    /// along the rhumb line (loxodrome) described by the resulting motion.
    /// </summary>
    /// <param name="latitude">An initial latitude.</param>
    /// <param name="longitude">An initial longitude.</param>
    /// <param name="distance">A distance, in meters.</param>
    /// <param name="bearing">A bearing, in radians clockwise from north.</param>
    /// <returns>The destination latitude and longitude.</returns>
    /// <remarks>
    /// <para>
    /// The results are inaccurate for highly ellipsoidal planets, as no compensation is
    /// attempted for the non-spherical shape of the planet.
    /// </para>
    /// <para>
    /// Rhumb lines, or loxodromes, are lines along a sphere with constant bearing. A rhumb line
    /// other than the equator or a meridian is not the shortest distance between any two points
    /// on that line (a great circle arc is), but does not require recalculation of bearing
    /// during travel.
    /// </para>
    /// <seealso cref="GetLatLonAtDistanceOnGreatCircleArc(double, double, HugeNumber, double)"/>
    /// </remarks>
    public (double latitude, double longitude) GetLatLonAtDistanceOnRhumbLine(
        double latitude,
        double longitude,
        HugeNumber distance,
        double bearing)
    {
        var angularDistance = (double)(distance / Shape.ContainingRadius);
        var deltaLatitude = angularDistance + Math.Cos(angularDistance);
        var finalLatitude = latitude + deltaLatitude;
        var deltaProjectedLatitude = Math.Log(Math.Tan(DoubleConstants.QuarterPi + (finalLatitude / 2)) / Math.Tan(DoubleConstants.QuarterPi + (latitude / 2)));
        var q = Math.Abs(deltaProjectedLatitude) > new HugeNumber(10, -12) ? deltaLatitude / deltaProjectedLatitude : Math.Cos(latitude);
        var deltaLongitude = angularDistance * Math.Sin(bearing) / q;
        var finalLongitude = longitude + deltaLongitude;
        if (Math.Abs(finalLatitude) > DoubleConstants.HalfPi)
        {
            finalLatitude = finalLatitude > 0 ? Math.PI - finalLatitude : -Math.PI - finalLatitude;
        }
        finalLongitude = ((finalLongitude + DoubleConstants.ThreeHalvesPi) % DoubleConstants.TwoPi) - DoubleConstants.HalfPi;
        return (finalLatitude, finalLongitude);
    }

    /// <summary>
    /// Calculate the time of local sunrise and sunset on the current day, based on the planet's
    /// rotation, as a proportion of a day since midnight.
    /// </summary>
    /// <param name="dataStore">
    /// The <see cref="IDataStore"/> from which to retrieve instances.
    /// </param>
    /// <param name="moment">The time at which to make the calculation.</param>
    /// <param name="latitude">The latitude at which to make the calculation.</param>
    /// <returns>
    /// A pair of <see cref="RelativeDuration"/> instances set to a proportion of a local day
    /// since midnight. If the sun does not rise and set on the given day (e.g. near the poles),
    /// then <see langword="null"/> will be returned for sunrise in the case of a polar night,
    /// and <see langword="null"/> for sunset in the case of a midnight sun.
    /// </returns>
    /// <remarks>
    /// <para>
    /// If this body is in a star system with multiple stars, the sunrise and sunset given will
    /// be for the star closest to the position it orbits. If it is not in orbit, the closest
    /// star is chosen.
    /// </para>
    /// <para>
    /// If there are no stars, or the body is not in a star system, the time of day is given as
    /// if a star existed at <see cref="Vector3{TScalar}.Zero"/> in local space. This might be used for a
    /// planet which is not part of a complete system model.
    /// </para>
    /// </remarks>
    public async Task<(RelativeDuration? sunrise, RelativeDuration? sunset)> GetLocalSunriseAndSunsetAsync(
        IDataStore dataStore,
        Instant moment,
        double latitude)
    {
        var position = await GetPositionAtTimeAsync(dataStore, moment);
        var starPosition = await GetPrimaryStarAsync(dataStore) is Star primaryStar
            ? await primaryStar.GetPositionAtTimeAsync(dataStore, moment)
            : (Vector3<HugeNumber>?)null;

        var (_, solarDeclination) = GetRightAscensionAndDeclination(position, starPosition ?? Vector3<HugeNumber>.Zero);

        var d = Math.Cos(solarDeclination) * Math.Cos(latitude);
        if (d.IsNearlyZero())
        {
            return (solarDeclination < 0) == latitude.IsNearlyZero()
                ? ((RelativeDuration?)RelativeDuration.FromProportionOfDay(0.0), null)
                : (null, RelativeDuration.FromProportionOfDay(0.0));
        }

        var localSecondsFromSolarNoonAtSunriseAndSet = Math.Acos(-Math.Sin(solarDeclination) * Math.Sin(latitude) / d) / AngularVelocity;
        var localSecondsSinceMidnightAtSunrise = ((RotationalPeriod / 2) - localSecondsFromSolarNoonAtSunriseAndSet) % RotationalPeriod;
        var localSecondsSinceMidnightAtSunset = (localSecondsFromSolarNoonAtSunriseAndSet + (RotationalPeriod / 2)) % RotationalPeriod;
        return (RelativeDuration.FromProportionOfDay((double)(localSecondsSinceMidnightAtSunrise / RotationalPeriod)),
            RelativeDuration.FromProportionOfDay((double)(localSecondsSinceMidnightAtSunset / RotationalPeriod)));
    }

    /// <summary>
    /// Gets the time of day at the given <paramref name="moment"/> and <paramref
    /// name="longitude"/>, based on the planet's rotation, as a proportion of a day since
    /// midnight.
    /// </summary>
    /// <param name="dataStore">
    /// The <see cref="IDataStore"/> from which to retrieve instances.
    /// </param>
    /// <param name="moment">The time at which to make the calculation.</param>
    /// <param name="longitude">The longitude at which to make the calculation.</param>
    /// <returns>
    /// A <see cref="RelativeDuration"/> set to a proportion of a local day since midnight.
    /// </returns>
    /// <remarks>
    /// <para>
    /// If this body is in a star system with multiple stars, the time of day given will be
    /// based on the star closest to the position it orbits. If it is not in orbit, the closest
    /// star is chosen.
    /// </para>
    /// <para>
    /// If there are no stars, or the body is not in a star system, the time of day is given as
    /// if a star existed at <see cref="Vector3{TScalar}.Zero"/> in local space. This might be used for a
    /// planet which is not part of a complete system model, or to stand for the local
    /// conventional time for a rogue planet which truly has no local star.
    /// </para>
    /// </remarks>
    public async Task<RelativeDuration> GetLocalTimeOfDayAsync(IDataStore dataStore, Instant moment, double longitude)
    {
        var position = await GetPositionAtTimeAsync(dataStore, moment);
        var starPosition = await GetPrimaryStarAsync(dataStore) is Star primaryStar
            ? await primaryStar.GetPositionAtTimeAsync(dataStore, moment)
            : Vector3<HugeNumber>.Zero;

        var (solarRightAscension, _) = GetRightAscensionAndDeclination(position, starPosition);
        var longitudeOffset = longitude - solarRightAscension;
        if (longitudeOffset > Math.PI)
        {
            longitudeOffset -= DoubleConstants.TwoPi;
        }
        var localSecondsSinceSolarNoon = longitudeOffset / AngularVelocity;

        var localSecondsSinceMidnight = (localSecondsSinceSolarNoon + (RotationalPeriod / 2)) % RotationalPeriod;
        return RelativeDuration.FromProportionOfDay((double)(localSecondsSinceMidnight / RotationalPeriod));
    }

    /// <summary>
    /// Gets the number of seconds difference from solar time at zero longitude at the given
    /// <paramref name="longitude"/>. Values will be positive to the east, and negative to the
    /// west.
    /// </summary>
    /// <param name="longitude">The longitude at which to determine the time offset.</param>
    /// <returns>The number of seconds difference from solar time at zero longitude at the given
    /// <paramref name="longitude"/>. Values will be positive to the east, and negative to the
    /// west.</returns>
    public HugeNumber GetLocalTimeOffset(double longitude)
        => (longitude > Math.PI ? longitude - DoubleConstants.TwoPi : longitude) * RotationalPeriod / HugeNumberConstants.TwoPi;

    /// <summary>
    /// Calculates the total luminous flux incident on this body from nearby sources of light
    /// (stars in the same system), in lumens, assuming a sun-like star at <see
    /// cref="Vector3{TScalar}.Zero"/>.
    /// </summary>
    /// <returns>The total illumination on the body, in lumens.</returns>
    /// <remarks>
    /// <para>
    /// A conversion of 0.0079 W/m² per lumen is used, which is roughly accurate for the sun.
    /// </para>
    /// <para>
    /// To get a value which accounts for the actual stars in the local system, use <see
    /// cref="GetLuminousFluxAsync(IDataStore)"/>.
    /// </para>
    /// </remarks>
    public double GetLuminousFlux()
    {
        var star = Star.NewSunlike(null, Vector3<HugeNumber>.Zero);
        return star is null ? 0 : GetLuminousFlux([star]);
    }

    /// <summary>
    /// Calculates the total luminous flux incident on this body from nearby sources of light
    /// (stars in the same system), in lumens.
    /// </summary>
    /// <param name="dataStore">
    /// The <see cref="IDataStore"/> from which to retrieve instances.
    /// </param>
    /// <returns>The total illumination on the body, in lumens.</returns>
    /// <remarks>
    /// A conversion of 0.0079 W/m² per lumen is used, which is roughly accurate for the sun,
    /// but may not be as precise for other stellar bodies.
    /// </remarks>
    public async Task<double> GetLuminousFluxAsync(IDataStore dataStore)
    {
        var system = await GetStarSystemAsync(dataStore);
        if (system is null)
        {
            return 0;
        }
        var stars = new List<Star>();
        await foreach (var star in system.GetStarsAsync(dataStore))
        {
            stars.Add(star);
        }
        return GetLuminousFlux(stars);
    }

    /// <summary>
    /// <para>
    /// Gets the approximate maximum surface temperature of this <see cref="Planetoid"/>, in K.
    /// </para>
    /// <para>
    /// Note that this is a calculated value, and does not take any custom temperature maps into
    /// account.
    /// </para>
    /// </summary>
    /// <remarks>
    /// Gets the equatorial temperature at periapsis, or at the current position if not in orbit.
    /// </remarks>
    public double GetMaxSurfaceTemperature()
    {
        if (!_maxSurfaceTemperature.HasValue)
        {
            var greenhouseEffect = GetGreenhouseEffect();
            _maxSurfaceTemperature = (SurfaceTemperatureAtPeriapsis * InsolationFactor_Equatorial) + greenhouseEffect;
        }
        return _maxSurfaceTemperature.Value;
    }

    /// <summary>
    /// <para>
    /// Gets the approximate minimum surface temperature of this <see cref="Planetoid"/>, in K.
    /// </para>
    /// <para>
    /// Note that this is a calculated value, and does not take any custom temperature maps into
    /// account.
    /// </para>
    /// </summary>
    /// <remarks>
    /// Gets the polar temperature at apoapsis, or at the current position if not in orbit.
    /// </remarks>
    public double GetMinSurfaceTemperature()
    {
        if (!_minSurfaceTemperature.HasValue)
        {
            var variation = GetDiurnalTemperatureVariation();
            var greenhouseEffect = GetGreenhouseEffect();
            _minSurfaceTemperature = (SurfaceTemperatureAtApoapsis * InsolationFactor_Polar) + greenhouseEffect - variation;
        }
        return _minSurfaceTemperature.Value;
    }

    /// <summary>
    /// Determines the proportion of the current season at the given <paramref name="moment"/>.
    /// </summary>
    /// <param name="numSeasons">The number of seasons.</param>
    /// <param name="moment">The time at which to make the determination.</param>
    /// <returns>>The proportion of the current season, as a value between 0.0 and 1.0, at the
    /// given <paramref name="moment"/>.</returns>
    public double GetProportionOfSeasonAtTime(uint numSeasons, Instant moment)
    {
        var proportionOfYear = GetProportionOfYearAtTime(moment);
        var proportionPerSeason = 1.0 / numSeasons;
        var seasonIndex = Math.Floor(proportionOfYear / proportionPerSeason);
        return (proportionOfYear - (seasonIndex * proportionPerSeason)) / proportionPerSeason;
    }

    /// <summary>
    /// Determines the proportion of a year, starting and ending with midwinter, at the given
    /// <paramref name="moment"/>.
    /// </summary>
    /// <param name="moment">The time at which to make the calculation.</param>
    /// <returns>The proportion of the year, starting and ending with midwinter, at the given
    /// <paramref name="moment"/>.</returns>
    public double GetProportionOfYearAtTime(Instant moment)
    {
        var trueAnomaly = Orbit?.GetTrueAnomalyAtTime(moment) ?? 0;
        return (trueAnomaly - WinterSolsticeTrueAnomaly + DoubleConstants.TwoPi)
            % DoubleConstants.TwoPi
            / DoubleConstants.TwoPi;
    }

    /// <summary>
    /// Gets the richness of the resources at the given <paramref name="latitude"/> and
    /// <paramref name="longitude"/>.
    /// </summary>
    /// <param name="latitude">The latitude at which to determine resource richness.</param>
    /// <param name="longitude">The longitude at which to determine resource richness.</param>
    /// <returns>The richness of the resources at the given <paramref name="latitude"/> and
    /// <paramref name="longitude"/>, as a collection of values between 0 and 1 for each <see
    /// cref="ISubstance"/> present.</returns>
    public IEnumerable<(ISubstanceReference substance, double richness)> GetResourceRichnessAt(double latitude, double longitude)
    {
        var position = LatitudeAndLongitudeToVector(latitude, longitude);
        return Resources.Select(x => (x.Substance, x.GetResourceRichnessAt(position)));
    }

    /// <summary>
    /// Gets the richness of the given resource at the given <paramref name="latitude"/> and
    /// <paramref name="longitude"/>.
    /// </summary>
    /// <param name="substance">The resource for which richness will be determined.</param>
    /// <param name="latitude">The latitude at which to determine resource richness.</param>
    /// <param name="longitude">The longitude at which to determine resource richness.</param>
    /// <returns>The richness of the resources at the given <paramref name="latitude"/> and
    /// <paramref name="longitude"/>, as a collection of values between 0 and 1 for each <see
    /// cref="ISubstance"/> present.</returns>
    public HugeNumber GetResourceRichnessAt(ISubstanceReference substance, double latitude, double longitude)
    {
        var position = LatitudeAndLongitudeToVector(latitude, longitude);
        return Resources
            .Where(x => x.Substance.Equals(substance))
            .Sum(x => x.GetResourceRichnessAt(position));
    }

    /// <summary>
    /// Gets phase information for the given <paramref name="satellite"/>.
    /// </summary>
    /// <param name="dataStore">
    /// The <see cref="IDataStore"/> from which to retrieve instances.
    /// </param>
    /// <param name="moment">The time at which to make the calculation.</param>
    /// <param name="satellite">A natural satellite of this body.</param>
    /// <returns>
    /// <para>
    /// The proportion of the satellite which is currently illuminated, and a boolean value
    /// indicating whether the body is in the waxing half of its cycle (vs. the waning half).
    /// </para>
    /// <para>
    /// Note: the waxing value is only valid when there is just a single star in the system (or
    /// when the satellite's primary is orbiting a binary pair together). When there are
    /// multiple stars, the proportion of the lighted surface is correct, but there is a strong
    /// possibility that the value increases and decreases in a complex pattern which does not
    /// correspond to simple waxing and waning cycles. The returned value in such cases will
    /// always be <see langword="false"/>.
    /// </para>
    /// </returns>
    public async Task<(double phase, bool waxing)> GetSatellitePhaseAsync(IDataStore dataStore, Instant moment, Planetoid satellite)
    {
        var position = await GetPositionAtTimeAsync(dataStore, moment);

        var stars = new List<(Star star, Vector3<HugeNumber> position, HugeNumber distance, double eclipticLongitude)>();
        if (await GetStarSystemAsync(dataStore) is StarSystem system)
        {
            await foreach (var star in system.GetStarsAsync(dataStore))
            {
                var starPosition = await star.GetPositionAtTimeAsync(dataStore, moment);
                var (_, eclipticLongitude) = GetEclipticLatLon(position, starPosition);
                stars.Add((
                    star,
                    starPosition,
                    Vector3<HugeNumber>.Distance(position, starPosition),
                    eclipticLongitude));
            }
        }

        var satellitePosition = await satellite.GetPositionAtTimeAsync(dataStore, moment);
        var phase = 0.0;

        if (stars.Count == 0)
        {
            var (_, eclipticLongitude) = GetEclipticLatLon(position, Vector3<HugeNumber>.Zero);
            var starDistance = position.Length();

            var satelliteDistance = Vector3<HugeNumber>.Distance(position, satellitePosition);
            var (satelliteLatitude, satelliteLongitude) = GetEclipticLatLon(position, satellitePosition);

            // satellite-centered elongation of the planet from the star (ratio of illuminated
            // surface area to total surface area)
            var le = Math.Acos(Math.Cos(satelliteLatitude) * Math.Cos(eclipticLongitude - satelliteLongitude));
            var e = Math.Atan2((double)(satelliteDistance - (starDistance * Math.Cos(le))), (double)(starDistance * Math.Sin(le)));

            // fraction of illuminated surface area
            phase = Math.Max(phase, (1 + Math.Cos(e)) / 2);
        }

        foreach (var (star, starPosition, starDistance, eclipticLongitude) in stars)
        {
            var satelliteDistance = Vector3<HugeNumber>.Distance(position, satellitePosition);
            var (satelliteLatitude, satelliteLongitude) = GetEclipticLatLon(position, satellitePosition);

            // satellite-centered elongation of the planet from the star (ratio of illuminated
            // surface area to total surface area)
            var le = Math.Acos(Math.Cos(satelliteLatitude) * Math.Cos(eclipticLongitude - satelliteLongitude));
            var e = Math.Atan2((double)(satelliteDistance - (starDistance * Math.Cos(le))), (double)(starDistance * Math.Sin(le)));

            // fraction of illuminated surface area
            phase = Math.Max(phase, (1 + Math.Cos(e)) / 2);
        }

        var waxing = false;
        if (stars.Count <= 1)
        {
            var starPosition = stars.Count == 0
                ? Vector3<HugeNumber>.Zero
                : stars[0].position;
            var (planetRightAscension, _) = satellite.GetRightAscensionAndDeclination(satellitePosition, position);
            var (starRightAscension, _) = satellite.GetRightAscensionAndDeclination(satellitePosition, starPosition);
            waxing = (starRightAscension - planetRightAscension + DoubleConstants.TwoPi) % DoubleConstants.TwoPi <= Math.PI;
        }

        return (phase, waxing);
    }

    /// <summary>
    /// Enumerates the natural satellites around this <see cref="Planetoid"/>.
    /// </summary>
    /// <param name="dataStore">
    /// The <see cref="IDataStore"/> from which instances may be retrieved.
    /// </param>
    /// <remarks>
    /// Unlike children, natural satellites are actually siblings in the local <see
    /// cref="Location"/> hierarchy, which merely share an orbital relationship.
    /// </remarks>
    public async IAsyncEnumerable<Planetoid> GetSatellitesAsync(IDataStore dataStore)
    {
        if (_satelliteIds is null)
        {
            yield break;
        }
        foreach (var id in _satelliteIds)
        {
            var satellite = await dataStore.GetItemAsync(id, UniverseSourceGenerationContext.Default.Planetoid);
            if (satellite is not null)
            {
                yield return satellite;
            }
        }
    }

    /// <summary>
    /// Determines the proportion of the seasonal cycle, with 0 indicating winter, and 1
    /// indicating summer, at the given <paramref name="moment"/>.
    /// </summary>
    /// <param name="moment">The time at which to make the calculation.</param>
    /// <param name="latitude">Used to determine hemisphere.</param>
    /// <returns>The proportion of the seasonal cycle, with 0 indicating winter, and 1
    /// indicating summer, at the given <paramref name="moment"/>.</returns>
    public double GetSeasonalProportionAtTime(Instant moment, double latitude)
    {
        var proportionOfYear = GetProportionOfYearAtTime(moment);
        if (proportionOfYear > 0.5)
        {
            proportionOfYear = 1 - proportionOfYear;
        }
        proportionOfYear *= 2;
        if (latitude < 0)
        {
            proportionOfYear = 1 - proportionOfYear;
        }

        if (latitude < AxialTilt)
        {
            var maximum = (AxialTilt - latitude) / (AxialTilt * 2);
            var range = 1 - maximum;
            proportionOfYear = Math.Abs(maximum - proportionOfYear) / range;
        }

        return proportionOfYear;
    }

    /// <summary>
    /// Determines the proportion of the seasonal cycle, with 0 indicating winter, and 1
    /// indicating summer, from the given proportion of a full year, starting and ending at
    /// midwinter.
    /// </summary>
    /// <param name="proportionOfYear">
    /// The proportion of a full year, starting and ending at midwinter, at which to make the
    /// calculation.
    /// </param>
    /// <param name="latitude">Used to determine hemisphere.</param>
    /// <returns>The proportion of the year, with 0 indicating winter, and 1 indicating summer,
    /// at the given proportion of a full year, starting and ending at midwinter.</returns>
    public double GetSeasonalProportionFromAnnualProportion(double proportionOfYear, double latitude)
        => GetSeasonalProportionFromAnnualProportion(proportionOfYear, latitude, AxialTilt);

    /// <summary>
    /// Determines the current season at the given <paramref name="moment"/>.
    /// </summary>
    /// <param name="numSeasons">The number of seasons.</param>
    /// <param name="moment">The time at which to make the determination.</param>
    /// <returns>The 0-based index of the current season at the given <paramref
    /// name="moment"/>.</returns>
    public uint GetSeasonAtTime(uint numSeasons, Instant moment)
        => (uint)Math.Floor(GetProportionOfYearAtTime(moment) * numSeasons);

    /// <summary>
    /// <para>
    /// Calculates the solar declination at the given true anomaly.
    /// </para>
    /// <para>
    /// Always zero for a planet not in orbit.
    /// </para>
    /// </summary>
    /// <param name="trueAnomaly">The true anomaly, in radians.</param>
    /// <returns>The solar declination, in radians.</returns>
    public double GetSolarDeclination(double trueAnomaly)
        => Orbit.HasValue ? Math.Asin(Math.Sin(-AxialTilt) * Math.Sin(Orbit.Value.GetEclipticLongitudeAtTrueAnomaly(trueAnomaly))) : 0;

    /// <summary>
    /// <para>
    /// Gets the surface temperature of the <see cref="Planetoid"/> at its equator, based on its
    /// current position, in K.
    /// </para>
    /// <para>
    /// Note that this is a calculated value, and does not take any custom temperature maps into
    /// account.
    /// </para>
    /// </summary>
    public double GetSurfaceTemperature()
    {
        if (!_surfaceTemperature.HasValue)
        {
            var greenhouseEffect = GetGreenhouseEffect();
            _surfaceTemperature = (BlackbodyTemperature * InsolationFactor_Equatorial) + greenhouseEffect;
        }
        return _surfaceTemperature.Value;
    }

    /// <summary>
    /// Calculates the effective surface temperature at the given surface position, including
    /// greenhouse effects, as if this object was at the specified true anomaly in its orbit, in
    /// K. If the body is not in orbit, returns the temperature at its current position.
    /// </summary>
    /// <param name="trueAnomaly">
    /// A true anomaly at which its temperature will be calculated.
    /// </param>
    /// <param name="seasonalLatitude">
    /// The latitude at which temperature will be calculated, relative to the solar equator at
    /// the time, rather than the rotational equator.
    /// </param>
    /// <returns>The surface temperature, in K.</returns>
    /// <remarks>
    /// The estimation is performed by linear interpolation between the temperature at periapsis
    /// and apoapsis, and between the equatorial and polar insolation levels, which is not
    /// necessarily accurate for highly elliptical orbits, or bodies with multiple significant
    /// nearby heat sources, but it should be fairly accurate for bodies in fairly circular
    /// orbits around heat sources which are all close to the center of the orbit, and much
    /// faster for successive calls than calculating the temperature at specific positions
    /// precisely.
    /// </remarks>
    public double GetSurfaceTemperatureAtTrueAnomaly(double trueAnomaly, double seasonalLatitude)
        => GetSeasonalSurfaceTemperature(GetTemperatureAtTrueAnomaly(trueAnomaly), seasonalLatitude);

    /// <summary>
    /// Adjusts the given surface temperature for elevation.
    /// </summary>
    /// <param name="surfaceTemp">The surface temperature at the location, in K.</param>
    /// <param name="elevation">The elevation, in meters.</param>
    /// <param name="surface">
    /// If <see langword="true"/> the determination is made for a location
    /// on the surface of the planetoid at the given elevation. Otherwise, the calculation is
    /// made for an elevation above the surface.
    /// </param>
    /// <returns>
    /// The temperature of this <see cref="Planetoid"/> at the given elevation, in K.
    /// </returns>
    /// <remarks>
    /// In an Earth-like atmosphere, the temperature lapse rate varies considerably in the
    /// different atmospheric layers, but this cannot be easily modeled for arbitrary
    /// exoplanetary atmospheres, so a simplified formula is used, which should be "close enough"
    /// for low elevations.
    /// </remarks>
    public double GetTemperatureAtElevation(double surfaceTemp, double elevation, bool surface = true)
    {
        // When outside the atmosphere, use the black body temperature, ignoring atmospheric effects.
        if (elevation >= Atmosphere.AtmosphericHeight)
        {
            return AverageBlackbodyTemperature;
        }

        if (elevation <= 0)
        {
            return surfaceTemp;
        }
        else
        {
            var value = surfaceTemp - (elevation * GetLapseRate(surfaceTemp));
            value = surfaceTemp - (elevation * GetLapseRate(value));

            if (!surface
                || Atmosphere.Material.IsEmpty
                || MaxElevation.IsNearlyZero())
            {
                return value;
            }

            // Represent the effect of near-surface atmospheric convection by returning the
            // average of the raw surface temperature and the result, weighted by the elevation.
            var weight = Math.Min(1, elevation * 4 / MaxElevation);

            return surfaceTemp.Lerp(value, weight);
        }
    }

    /// <summary>
    /// Determines if the planet is habitable by a species with the given requirements. Does not
    /// imply that the planet could sustain a large-scale population in the long-term, only that
    /// a member of the species can survive on the surface without artificial aid.
    /// </summary>
    /// <param name="habitabilityRequirements">The collection of <see
    /// cref="HabitabilityRequirements"/>.</param>
    /// <returns>
    /// The <see cref="UninhabitabilityReason"/> indicating the reason(s) the planet is
    /// uninhabitable, if any.
    /// </returns>
    public UninhabitabilityReason IsHabitable(HabitabilityRequirements habitabilityRequirements)
    {
        var reason = UninhabitabilityReason.None;

        if (IsInhospitable)
        {
            reason = UninhabitabilityReason.Inhospitable;
        }

        if (habitabilityRequirements.RequireLiquidWater && !HasLiquidWater())
        {
            reason |= UninhabitabilityReason.NoWater;
        }

        if (habitabilityRequirements.AtmosphericRequirements is not null
            && !Atmosphere.MeetsRequirements(habitabilityRequirements.AtmosphericRequirements))
        {
            reason |= UninhabitabilityReason.UnbreathableAtmosphere;
        }

        // The coldest temp will usually occur at apoapsis for bodies which directly orbit stars
        // (even in multi-star systems, the body would rarely be closer to a companion star even
        // at apoapsis given the orbital selection criteria used in this library). For a moon,
        // the coldest temperature should occur at its parent's own apoapsis, but this is
        // unrelated to the moon's own apses and is effectively impossible to calculate due to
        // the complexities of the potential orbital dynamics, so this special case is ignored.
        if (GetMinEquatorTemperature() < (habitabilityRequirements.MinimumTemperature ?? 0))
        {
            reason |= UninhabitabilityReason.TooCold;
        }

        // To determine if a planet is too hot, the polar temperature at periapsis is used, since
        // this should be the coldest region at its hottest time.
        if (GetMaxPolarTemperature() > (habitabilityRequirements.MaximumTemperature ?? double.PositiveInfinity))
        {
            reason |= UninhabitabilityReason.TooHot;
        }

        if (Atmosphere.AtmosphericPressure < (habitabilityRequirements.MinimumPressure ?? 0))
        {
            reason |= UninhabitabilityReason.LowPressure;
        }

        if (Atmosphere.AtmosphericPressure > (habitabilityRequirements.MaximumPressure ?? double.PositiveInfinity))
        {
            reason |= UninhabitabilityReason.HighPressure;
        }

        if (SurfaceGravity < (habitabilityRequirements.MinimumGravity ?? 0))
        {
            reason |= UninhabitabilityReason.LowGravity;
        }

        if (SurfaceGravity > (habitabilityRequirements.MaximumGravity ?? double.PositiveInfinity))
        {
            reason |= UninhabitabilityReason.HighGravity;
        }

        return reason;
    }

    /// <summary>
    /// Converts latitude and longitude to a <see cref="Vector3{TScalar}"/>.
    /// </summary>
    /// <param name="latitude">A latitude, as an angle in radians from the equator.</param>
    /// <param name="longitude">A longitude, as an angle in radians from the X-axis at 0
    /// rotation.</param>
    /// <returns>
    /// A normalized <see cref="Vector3{TScalar}"/> representing
    /// a position on the surface of this <see cref="Planetoid"/>.
    /// </returns>
    /// <remarks>
    /// If the planet's axis has never been set, it is treated as vertical for the purpose of
    /// this calculation, but is not permanently set to such an axis.
    /// </remarks>
    public Vector3<double> LatitudeAndLongitudeToDoubleVector(double latitude, double longitude)
    {
        var cosLat = Math.Cos(latitude);
        var rot = AxisRotation;
        return Vector3<double>.Normalize(
            Vector3<double>.Transform(
                new Vector3<double>(
                    cosLat * Math.Sin(longitude),
                    Math.Sin(latitude),
                    cosLat * Math.Cos(longitude)),
                Quaternion<double>.Inverse(rot)));
    }

    /// <summary>
    /// Converts latitude and longitude to a <see cref="Vector3{TScalar}"/>.
    /// </summary>
    /// <param name="latitude">A latitude, as an angle in radians from the equator.</param>
    /// <param name="longitude">A longitude, as an angle in radians from the X-axis at 0
    /// rotation.</param>
    /// <returns>A normalized <see cref="Vector3{TScalar}"/> representing a position on the surface of
    /// this <see cref="Planetoid"/>.</returns>
    /// <remarks>
    /// If the planet's axis has never been set, it is treated as vertical for the purpose of
    /// this calculation, but is not permanently set to such an axis.
    /// </remarks>
    public Vector3<HugeNumber> LatitudeAndLongitudeToVector(double latitude, double longitude)
    {
        var v = LatitudeAndLongitudeToDoubleVector(latitude, longitude);
        return new Vector3<HugeNumber>(v.X, v.Y, v.Z);
    }

    /// <summary>
    /// Removes a resource from this planet's collection.
    /// </summary>
    /// <param name="index">The index of the resource to remove.</param>
    public void RemoveResource(int index)
    {
        if (_resources is null
            || _resources.Count <= index)
        {
            return;
        }
        _resources.RemoveAt(index);
    }

    /// <summary>
    /// Removes a ring from this planet's collection.
    /// </summary>
    /// <param name="index">The index of the ring to remove.</param>
    public void RemoveRing(int index)
    {
        if (_rings is null
            || _rings.Count <= index)
        {
            return;
        }
        var rings = ((_rings as ImmutableList<PlanetaryRing>)
            ?? [.. _rings])
            .RemoveAt(index);
        _rings = rings.Count == 0
            ? null
            : rings;
    }

    /// <summary>
    /// Removes a satellite from this planet's collection.
    /// </summary>
    /// <param name="id">The <see cref="IIdItem.Id"/> of the satellite to remove.</param>
    public void RemoveSatellite(string id)
    {
        if (_satelliteIds is null)
        {
            return;
        }
        var ids = ((_satelliteIds as ImmutableList<string>)
            ?? [.. _satelliteIds])
            .Remove(id);
        _satelliteIds = ids.Count == 0
            ? null
            : ids;
    }

    /// <summary>
    /// Sets this planet's albedo.
    /// </summary>
    /// <param name="value"></param>
    /// <remarks>
    /// Note that even small changes can drastically change the hydrosphere and atmosphere of a
    /// planet due to the change in effective surface temperature.
    /// </remarks>
    public void SetAlbedo(double value)
    {
        if (PlanetType == PlanetType.Comet
            || IsAsteroid
            || IsGiant
            || !IsTerrestrial
            || Albedo.IsNearlyEqualTo(value))
        {
            Albedo = value;
            return;
        }

        var surfaceTemp = GetAverageSurfaceTemperature();
        Albedo = value;

        // An albedo change might significantly alter surface temperature. 5K is used as the
        // threshold for re-calculation, which may lead to some inaccuracies, but should avoid
        // over-calculation for small changes.
        Atmosphere.ResetTemperatureDependentProperties(this);
        if (Math.Abs(surfaceTemp - GetAverageSurfaceTemperature()) > 5)
        {
            var adjustedAtmosphericPressure = CalculatePhases(1, Atmosphere.AtmosphericPressure);
            FractionHydrosphere(GetAverageSurfaceTemperature());

            if (_planetParams?.AtmosphericPressure.HasValue != true && _habitabilityRequirements is null)
            {
                SetAtmosphericPressure(Math.Max(0, adjustedAtmosphericPressure));
                Atmosphere.ResetPressureDependentProperties(this);
            }
        }
    }

    /// <summary>
    /// Sets the angle between the Y-axis and the axis of rotation of this planet. Values greater
    /// than π/2 indicate clockwise rotation.
    /// </summary>
    /// <param name="angle">The angle to assign, in radians.</param>
    public void SetAngleOfRotation(double angle)
    {
        while (angle > Math.PI)
        {
            angle -= Math.PI;
        }
        while (angle < 0)
        {
            angle += Math.PI;
        }
        AngleOfRotation = angle;
        SetAxis();
    }

    /// <summary>
    /// Sets the atmospheric pressure of this <see cref="Planetoid"/>, in kPa.
    /// </summary>
    /// <param name="value">An atmospheric pressure in kPa.</param>
    /// <remarks>
    /// Has no effect if this <see cref="Planetoid"/> has no atmosphere.
    /// </remarks>
    public void SetAtmosphericPressure(double value)
    {
        Atmosphere.SetAtmosphericPressure(value);
        ResetPressureDependentProperties();
    }

    /// <summary>
    /// Sets the angle between the X-axis and the orbital vector at which the vernal equinox of the
    /// northern hemisphere occurs on this planet.
    /// </summary>
    /// <param name="value">The angle to assign, in radians.</param>
    public void SetAxialPrecession(double value)
    {
        AxialPrecession = value;
        SetAxis();
        if (Orbit.HasValue)
        {
            Space.Orbit.AssignOrbit(
                this,
                Orbit.Value.OrbitedId,
                Orbit.Value.GetOrbitalParameters());
        }
    }

    /// <summary>
    /// Sets the axial tilt of the <see cref="Planetoid"/> relative to its orbital plane, in
    /// radians. Values greater than half Pi indicate clockwise rotation.
    /// </summary>
    /// <param name="value">An angle, in radians.</param>
    /// <remarks>
    /// If the <see cref="Planetoid"/> isn't orbiting anything, this is the same as the angle of
    /// rotation.
    /// </remarks>
    public void SetAxialTilt(double value) => SetAngleOfRotation(Orbit.HasValue ? value + Orbit.Value.Inclination : value);

    /// <summary>
    /// Sets whether this planet has a native population of living organisms.
    /// </summary>
    /// <param name="value">
    /// <para>
    /// Whether this planet has a native population of living organisms.
    /// </para>
    /// <para>
    /// The complexity of life is not presumed. If a planet is basically habitable (liquid
    /// surface water), life in at least a single-celled form may be indicated, and may affect
    /// the atmospheric composition.
    /// </para>
    /// </param>
    /// <remarks>
    /// Note that changing this value can drastically change the atmosphere of a terrestrial planet.
    /// </remarks>
    public void SetHasBiosphere(bool value)
    {
        if (HasBiosphere == value)
        {
            return;
        }
        HasBiosphere = value;
        if (value
            && IsTerrestrial
            && PlanetType != PlanetType.Comet
            && !IsAsteroid
            && !IsGiant)
        {
            GenerateLifeEffects();
        }
    }

    /// <summary>
    /// Sets whether this planet has a strong magnetosphere.
    /// </summary>
    /// <param name="value">Whether this planet has a strong magnetosphere.</param>
    /// <remarks>
    /// Note that changing this value can drastically change the atmosphere of a terrestrial planet.
    /// </remarks>
    public void SetHasMagnetosphere(bool value)
    {
        if (HasMagnetosphere == value)
        {
            return;
        }
        HasMagnetosphere = value;
        if (IsTerrestrial
            && PlanetType != PlanetType.Comet
            && !IsAsteroid
            && !IsGiant
            && AverageBlackbodyTemperature < GetTempForThinAtmosphere())
        {
            GenerateAtmosphere();
        }
    }

    /// <summary>
    /// Sets the elevation of sea level relative to the mean surface elevation of this planet, as a
    /// fraction of <see cref="MaxElevation"/>.
    /// </summary>
    /// <param name="dataStore">
    /// The <see cref="IDataStore"/> from which to retrieve instances.
    /// </param>
    /// <param name="value">
    /// The elevation of sea level relative to the mean surface elevation of this planet, as a
    /// fraction of <see cref="MaxElevation"/>.
    /// </param>
    /// <remarks>
    /// Note that changing this value can drastically change the planet's atmosphere, temperature,
    /// and other properties.
    /// </remarks>
    public async Task SetNormalizedSeaLevelAsync(
        IDataStore dataStore,
        double value)
    {
        if (NormalizedSeaLevel.IsNearlyEqualTo(value))
        {
            NormalizedSeaLevel = value;
            return;
        }

        NormalizedSeaLevel = value;
        const double HalfVolume = 85183747862278.266; // empirical sum of random map pixel columns with 0 sea level if (ratio >= 1)
        var seawater = Substances.All.Seawater.GetHomogeneousReference();
        HugeNumber mass;
        if (value <= 0)
        {
            mass = HugeNumber.Zero;
        }
        else if (value >= 1)
        {
            mass = new HollowSphere<HugeNumber>(
                Shape.ContainingRadius,
                Shape.ContainingRadius + SeaLevel).Volume * (seawater.Homogeneous.DensityLiquid ?? 0);
        }
        else
        {
            var volume = value > 0
                ? HalfVolume + (HalfVolume * value)
                : HalfVolume - (HalfVolume * -value);
            mass = volume
                * MaxElevation
                * (seawater.Homogeneous.DensityLiquid ?? 0);
        }
        if (!mass.IsPositive())
        {
            Hydrosphere = new Material<HugeNumber>();
            return;
        }

        double surfaceTemp;
        if (_planetParams?.SurfaceTemperature.HasValue == true)
        {
            surfaceTemp = _planetParams!.Value.SurfaceTemperature!.Value;
        }
        else if (_habitabilityRequirements?.MinimumTemperature.HasValue == true)
        {
            surfaceTemp = _habitabilityRequirements!.Value.MaximumTemperature.HasValue
                ? (_habitabilityRequirements!.Value.MinimumTemperature!.Value
                    + _habitabilityRequirements!.Value.MaximumTemperature.Value)
                    / 2
                : _habitabilityRequirements!.Value.MinimumTemperature!.Value;
        }
        else
        {
            surfaceTemp = BlackbodyTemperature;
        }

        // Surface water is mostly salt water.
        var seawaterProportion = (decimal)Randomizer.Instance.NormalDistributionSample(0.945, 0.015);
        var waterProportion = 1 - seawaterProportion;
        var water = Substances.All.Water.GetHomogeneousReference();
        var density = ((seawater.Homogeneous.DensityLiquid ?? 0) * (double)seawaterProportion) + ((water.Homogeneous.DensityLiquid ?? 0) * (double)waterProportion);

        var outerRadius = (3 * ((mass / density) + new Sphere<HugeNumber>(Material.Shape.ContainingRadius).Volume) / HugeNumberConstants.FourPi).Cbrt();
        var shape = new HollowSphere<HugeNumber>(
            Material.Shape.ContainingRadius,
            outerRadius,
            Material.Shape.Position);
        var avgDepth = (double)(outerRadius - Material.Shape.ContainingRadius) / 2;
        double avgTemp;
        if (avgDepth > 1000)
        {
            avgTemp = 277;
        }
        else if (avgDepth < 200)
        {
            avgTemp = surfaceTemp;
        }
        else
        {
            avgTemp = surfaceTemp.Lerp(277, (avgDepth - 200) / 800);
        }

        Hydrosphere = new Material<HugeNumber>(
            shape,
            mass,
            density,
            avgTemp,
            (seawater, seawaterProportion),
            (water, waterProportion));

        FractionHydrosphere(surfaceTemp);

        if (Material.GetSurface() is Material<HugeNumber> material)
        {
            material.AddConstituents(CosmicSubstances.WetPlanetaryCrustConstituents);
        }

        var stars = new List<Star>();
        Star? star = null;
        if (!string.IsNullOrEmpty(Orbit?.OrbitedId)
            && await GetParentAsync(dataStore) is StarSystem system)
        {
            await foreach (var item in system.GetStarsAsync(dataStore))
            {
                if (Orbit.Value.OrbitedId.Equals(item.Id))
                {
                    star = item;
                }
                stars.Add(item);
            }
        }

        if (star is not null
            && (_planetParams?.SurfaceTemperature.HasValue == true
            || _habitabilityRequirements?.MinimumTemperature.HasValue == true
            || _habitabilityRequirements?.MaximumTemperature.HasValue == true))
        {
            CorrectSurfaceTemperature(stars, star, surfaceTemp);
        }
        else
        {
            GenerateAtmosphere();
        }

        GenerateResources();
    }

    /// <summary>
    /// Sets this planet's type.
    /// </summary>
    /// <param name="dataStore">
    /// The <see cref="IDataStore"/> from which to retrieve instances.
    /// </param>
    /// <param name="type">The type to set.</param>
    /// <remarks>
    /// Note that changing this value will drastically change every aspect of the planet.
    /// </remarks>
    public async Task SetPlanetTypeAsync(IDataStore dataStore, PlanetType type)
    {
        if (PlanetType == type)
        {
            return;
        }

        PlanetType = type;
        var stars = new List<Star>();
        Star? star = null;
        var parent = await GetParentAsync(dataStore);
        if (parent is StarSystem system)
        {
            await foreach (var item in system.GetStarsAsync(dataStore))
            {
                if (!string.IsNullOrEmpty(Orbit?.OrbitedId)
                    && Orbit.Value.OrbitedId.Equals(item.Id))
                {
                    star = item;
                }
                stars.Add(item);
            }
        }
        var sanityCheck = 0;
        do
        {
            Configure(
                parent as CosmicLocation,
                stars,
                star,
                Position,
                true,
                Orbit?.GetOrbitalParameters());
            sanityCheck++;
            if (!_habitabilityRequirements.HasValue
                || IsHabitable(_habitabilityRequirements.Value) == UninhabitabilityReason.None)
            {
                break;
            }
        } while (sanityCheck <= 100);
    }

    /// <summary>
    /// Sets the length of time it takes for this <see cref="Planetoid"/> to rotate once about
    /// its axis, in seconds.
    /// </summary>
    /// <param name="value">A <see cref="HugeNumber"/> value.</param>
    public void SetRotationalPeriod(HugeNumber value)
    {
        RotationalPeriod = value;
        _angularVelocity = null;
        ResetCachedTemperatures();
    }

    /// <summary>
    /// Sets the elevation of sea level relative to the mean surface elevation of this planet, in
    /// meters.
    /// </summary>
    /// <param name="dataStore">
    /// The <see cref="IDataStore"/> from which to retrieve instances.
    /// </param>
    /// <param name="value">
    /// The elevation of sea level relative to the mean surface elevation of this planet, in meters.
    /// </param>
    /// <remarks>
    /// Note that changing this value can drastically change the planet's atmosphere, temperature,
    /// and other properties.
    /// </remarks>
    public Task SetSeaLevelAsync(
        IDataStore dataStore,
        double value) => SetNormalizedSeaLevelAsync(dataStore, value / MaxElevation);

    /// <summary>
    /// Converts a <see cref="Vector3{TScalar}"/> to a latitude, in radians.
    /// </summary>
    /// <param name="v">A vector representing a position on the surface of this <see cref="Planetoid"/>.</param>
    /// <returns>A latitude, as an angle in radians from the equator.</returns>
    public double VectorToLatitude(Vector3<HugeNumber> v) => VectorToLatitude((Vector3)v);

    /// <summary>
    /// Converts a <see cref="Vector3{TScalar}"/> to a latitude, in radians.
    /// </summary>
    /// <param name="v">A vector representing a position on the surface of this <see cref="Planetoid"/>.</param>
    /// <returns>A latitude, as an angle in radians from the equator.</returns>
    public double VectorToLatitude(Vector3 v) => DoubleConstants.HalfPi - (double)Axis.Angle(v);

    /// <summary>
    /// Converts a <see cref="Vector3{TScalar}"/> to a longitude, in radians.
    /// </summary>
    /// <param name="v">A vector representing a position on the surface of this <see cref="Planetoid"/>.</param>
    /// <returns>A longitude, as an angle in radians from the X-axis at 0 rotation.</returns>
    public double VectorToLongitude(Vector3<HugeNumber> v) => VectorToLongitude((Vector3)v);

    /// <summary>
    /// Converts a <see cref="Vector3{TScalar}"/> to a longitude, in radians.
    /// </summary>
    /// <param name="v">A vector representing a position on the surface of this <see cref="Planetoid"/>.</param>
    /// <returns>A longitude, as an angle in radians from the X-axis at 0 rotation.</returns>
    public double VectorToLongitude(Vector3 v)
    {
        var u = Vector3.Transform(v, AxisRotation);
        return u.X.IsNearlyZero() && u.Z.IsNearlyZero()
            ? 0
            : Math.Atan2(u.X, u.Z);
    }

    internal static HugeNumber GetSpaceForType(PlanetType type) => type switch
    {
        PlanetType.AsteroidC => _AsteroidSpace,
        PlanetType.AsteroidM => _AsteroidSpace,
        PlanetType.AsteroidS => _AsteroidSpace,
        PlanetType.Comet => _CometSpace,
        PlanetType.Dwarf => _DwarfSpace,
        PlanetType.LavaDwarf => _DwarfSpace,
        PlanetType.RockyDwarf => _DwarfSpace,
        PlanetType.GasGiant => _GiantSpace,
        PlanetType.IceGiant => _GiantSpace,
        _ => _TerrestrialSpace,
    };

    internal double GetInsolationFactor(HugeNumber atmosphereMass, double atmosphericScaleHeight, bool polar = false)
        => (double)HugeNumber.Pow(1320000
            * atmosphereMass
            * (polar
                ? Math.Pow(0.7, Math.Pow(GetPolarAirMass(atmosphericScaleHeight), 0.678))
                : 0.7)
            / Mass
            , new HugeNumber(25, -2));

    /// <summary>
    /// Approximates the radius of the orbiting body's mutual Hill sphere with another orbiting
    /// body in orbit around the same primary, in meters.
    /// </summary>
    /// <remarks>
    /// Assumes the semimajor axis of both orbits is identical for the purposes of the
    /// calculation, which obviously would not be the case, but generates reasonably close
    /// estimates in the absence of actual values.
    /// </remarks>
    /// <param name="otherMass">
    /// The mass of another celestial body presumed to be orbiting the same primary as this one.
    /// </param>
    /// <returns>The radius of the orbiting body's Hill sphere, in meters.</returns>
    internal HugeNumber GetMutualHillSphereRadius(HugeNumber otherMass)
        => Orbit?.GetMutualHillSphereRadius(Mass, otherMass) ?? HugeNumber.Zero;

    internal override async ValueTask ResetOrbitAsync(IDataStore dataStore)
    {
        _axialTilt = null;

        var stars = new List<Star>();
        if (Orbit.HasValue)
        {
            var system = await GetStarSystemAsync(dataStore);
            if (system is not null)
            {
                await foreach (var star in system.GetStarsAsync(dataStore))
                {
                    stars.Add(star);
                }
            }
        }

        ResetAllCachedTemperatures(stars);
    }

    private (double latitude, double longitude) GetEclipticLatLon(Vector3<HugeNumber> position, Vector3<HugeNumber> otherPosition)
    {
        var precessionQ = Quaternion<HugeNumber>.CreateFromYawPitchRoll(AxialPrecession, 0, 0);
        var p = Vector3<HugeNumber>.Transform(position - otherPosition, precessionQ) * -1;
        var r = p.Length();
        var lat = Math.Asin((double)(p.Z / r));
        if (lat >= Math.PI)
        {
            lat = DoubleConstants.TwoPi - lat;
        }
        if (lat == Math.PI)
        {
            lat = 0;
        }
        var lon = Math.Acos((double)(p.X / (r * Math.Cos(lat))));
        return (lat, lon);
    }

    private double GetGreenhouseEffect(double insolationFactor, double greenhouseFactor)
        => Math.Max(0, (AverageBlackbodyTemperature * insolationFactor * greenhouseFactor) - AverageBlackbodyTemperature);

    private double GetInsolationFactor(bool polar = false)
        => GetInsolationFactor(Atmosphere.Material.Mass, Atmosphere.AtmosphericScaleHeight, polar);

    private double GetInsolationFactor(double latitude)
        => InsolationFactor_Polar + ((InsolationFactor_Equatorial - InsolationFactor_Polar)
        * (0.5 + (Math.Cos(Math.Max(0, (Math.Abs(2 * latitude)
            * (DoubleConstants.HalfPi + AxialTilt)
            / DoubleConstants.HalfPi) - AxialTilt)) / 2)));

    /// <summary>
    /// Calculates the adiabatic lapse rate for this <see cref="Planetoid"/>, after determining
    /// whether to use the dry or moist based on the presence of water vapor, in K/m.
    /// </summary>
    /// <param name="surfaceTemp">The surface temperature at the location, in K.</param>
    /// <returns>The adiabatic lapse rate for this <see cref="Planetoid"/>, in K/m.</returns>
    /// <remarks>
    /// Uses the specific heat and gas constant of dry air on Earth, which is clearly not
    /// correct for other atmospheres, but is considered "close enough" for the purposes of this
    /// library.
    /// </remarks>
    private double GetLapseRate(double surfaceTemp)
        => Atmosphere.WaterRatio > 0 ? GetLapseRateMoist(surfaceTemp) : LapseRateDry;

    /// <summary>
    /// Calculates the moist adiabatic lapse rate near the surface of this <see
    /// cref="Planetoid"/>, in K/m.
    /// </summary>
    /// <param name="surfaceTemp">The surface temperature at the location, in K.</param>
    /// <returns>
    /// The moist adiabatic lapse rate near the surface of this <see cref="Planetoid"/>, in K/m.
    /// </returns>
    /// <remarks>
    /// Uses the specific heat and gas constant of dry air on Earth, which is clearly not
    /// correct for other atmospheres, but is considered "close enough" for the purposes of this
    /// library.
    /// </remarks>
    private double GetLapseRateMoist(double surfaceTemp)
    {
        var surfaceTemp2 = surfaceTemp * surfaceTemp;

        var numerator = (DoubleConstants.RSpecificDryAir * surfaceTemp2) + (Atmosphere.HvE * surfaceTemp);
        var denominator = (Constants.CpTimesRSpecificDryAir * surfaceTemp2) + Atmosphere.Hv2RsE;

        return (double)SurfaceGravity * (numerator / denominator);
    }

    private double GetLuminousFlux(IEnumerable<Star> stars)
    {
        var sum = 0.0;
        foreach (var star in stars)
        {
            sum += (double)(star.Luminosity / (HugeNumberConstants.FourPi * GetDistanceSquaredTo(star))) / 0.0079;
        }
        return sum;
    }

    private double GetMaxPolarTemperature()
    {
        var greenhouseEffect = GetGreenhouseEffect();
        var temp = SurfaceTemperatureAtPeriapsis;
        return (temp * InsolationFactor_Polar) + greenhouseEffect;
    }

    private double GetMinEquatorTemperature()
    {
        var variation = GetDiurnalTemperatureVariation();
        var greenhouseEffect = GetGreenhouseEffect();
        return (SurfaceTemperatureAtApoapsis * InsolationFactor_Equatorial) + greenhouseEffect - variation;
    }

    private double GetPolarAirMass(double atmosphericScaleHeight)
    {
        var r = (double)Shape.ContainingRadius / atmosphericScaleHeight;
        var rCosLat = r * CosPolarLatitude;
        return Math.Sqrt((rCosLat * rCosLat) + (2 * r) + 1) - rCosLat;
    }

    private async ValueTask<Star?> GetPrimaryStarAsync(IDataStore dataStore)
    {
        if (Orbit.HasValue
            && !string.IsNullOrEmpty(Orbit.Value.OrbitedId)
            && await dataStore.GetItemAsync(
                Orbit.Value.OrbitedId,
                UniverseSourceGenerationContext.Default.Star) is Star orbitedStar)
        {
            return orbitedStar;
        }

        var system = await GetStarSystemAsync(dataStore);
        if (system is null)
        {
            return null;
        }

        var stars = new List<Star>();
        await foreach (var star in system.GetStarsAsync(dataStore))
        {
            stars.Add(star);
        }
        if (stars.Count == 0)
        {
            return null;
        }

        if (stars.Count == 1)
        {
            return stars[0];
        }

        Star? primaryStar = null;
        var minDistance = HugeNumber.PositiveInfinity;
        foreach (var star in stars)
        {
            HugeNumber starDistance;
            if (Orbit.HasValue)
            {
                starDistance = Orbit.Value.OrbitedPosition.DistanceSquared(star.ParentId == ParentId
                    ? star.Position
                    : LocalizePosition(star, AbsolutePosition));
            }
            else
            {
                starDistance = Position.DistanceSquared(star.ParentId == ParentId
                    ? star.Position
                    : LocalizePosition(star, AbsolutePosition));
            }
            if (primaryStar is null || starDistance < minDistance)
            {
                primaryStar = star;
                minDistance = starDistance;
            }
        }

        return primaryStar;
    }

    private (double rightAscension, double declination) GetRightAscensionAndDeclination(
        Vector3<HugeNumber> position,
        Vector3<HugeNumber> otherPosition)
    {
        var rot = AxisRotation;
        var equatorialPosition = Vector3<HugeNumber>.Transform(position - otherPosition, rot);
        var r = equatorialPosition.Length();
        var mPos = equatorialPosition.Y != HugeNumber.Zero
            && equatorialPosition.Y.Sign() == r.Sign();
        var n = (double)(equatorialPosition.Z / r);
        var declination = Math.Asin(n);
        if (declination > Math.PI)
        {
            declination -= DoubleConstants.TwoPi;
        }
        var cosDeclination = Math.Cos(declination);
        if (cosDeclination.IsNearlyZero())
        {
            return (0, declination);
        }
        var rightAscension = mPos
            ? Math.Acos(1 / cosDeclination)
            : DoubleConstants.TwoPi - Math.Acos(1 / cosDeclination);
        if (rightAscension > Math.PI)
        {
            rightAscension -= DoubleConstants.TwoPi;
        }
        return (rightAscension, declination);
    }

    private async ValueTask<StarSystem?> GetStarSystemAsync(IDataStore dataStore)
    {
        var parent = await GetParentAsync(dataStore);
        if (parent is StarSystem system)
        {
            return system;
        }
        // Allow a second level of containment for asteroid fields and Oort clouds. Custom
        // containment scenarios with deeply nested entities are not considered.
        if (parent is not null)
        {
            parent = await parent.GetParentAsync(dataStore);
            if (parent is StarSystem parentSystem)
            {
                return parentSystem;
            }
        }
        return null;
    }

    private double GetSeasonalSurfaceTemperature(double blackbodyTemperature, double seasonalLatitude)
    {
        var greenhouseEffect = GetGreenhouseEffect();
        var temp = (blackbodyTemperature * GetInsolationFactor(seasonalLatitude)) + greenhouseEffect;
        if (Atmosphere.Material.IsEmpty)
        {
            return temp;
        }
        // Represent the effect of atmospheric convection by returning the average of the raw
        // result and the equatorial result, weighted by the distance to the equator.
        var equatorialTemp = (blackbodyTemperature * InsolationFactor_Equatorial) + greenhouseEffect;
        var weight = Math.Sin(2.5 * Math.Sqrt(Math.Abs(seasonalLatitude))) / 1.75;

        return temp.Lerp(equatorialTemp, weight);
    }

    /// <summary>
    /// Calculates the total average temperature of the location as if this object was at the
    /// specified position, including ambient heat of its parent and radiated heat from all
    /// sibling objects, in K.
    /// </summary>
    /// <param name="position">
    /// A hypothetical position for this location at which its temperature will be calculated.
    /// </param>
    /// <param name="stars">THe stars in the local system.</param>
    /// <returns>
    /// The total average temperature of the location at the given position, in K.
    /// </returns>
    private double GetTemperatureAtPosition(Vector3<HugeNumber> position, List<Star>? stars)
    {
        // Calculate the heat added to this location by insolation at the given position.
        var insolationHeat = 0.0;
        if (Albedo < 1 && stars?.Count > 0)
        {
            var sum = 0.0;
            foreach (var star in stars)
            {
                sum += Math.Pow(star.Luminosity / (double)position.DistanceSquared(star.Position), 0.25);

                //sum += star.Temperature
                //    * (double)HugeNumber.Sqrt(star.Shape.ContainingRadius / (2 * position.Distance(star.Position)));
            }

            var areaRatio = 1;
            if (RotationalPeriod > 2500)
            {
                if (RotationalPeriod <= 75000)
                {
                    areaRatio = 4;
                }
                else if (RotationalPeriod <= 150000)
                {
                    areaRatio = 3;
                }
                else if (RotationalPeriod <= 300000)
                {
                    areaRatio = 2;
                }
            }

            insolationHeat = sum * Math.Pow((1 - Albedo)
                / (DoubleConstants.FourPi
                * DoubleConstants.sigma
                * areaRatio), 0.25);
        }

        return Temperature + insolationHeat;
    }

    /// <summary>
    /// Estimates the total average temperature of the location as if this object was at the
    /// specified true anomaly in its orbit, including ambient heat of its parent and radiated
    /// heat from all sibling objects, in K. If the body is not in orbit, returns the
    /// temperature at its current position.
    /// </summary>
    /// <param name="trueAnomaly">
    /// A true anomaly at which its temperature will be calculated.
    /// </param>
    /// <returns>
    /// The total average temperature of the location at the given position, in K.
    /// </returns>
    /// <remarks>
    /// The estimation is performed by linear interpolation between the temperature at periapsis
    /// and apoapsis, which is not necessarily accurate for highly elliptical orbits, or bodies
    /// with multiple significant nearby heat sources, but it should be fairly accurate for
    /// bodies in fairly circular orbits around heat sources which are all close to the center
    /// of the orbit, and much faster for successive calls than calculating the temperature at
    /// specific positions precisely.
    /// </remarks>
    private double GetTemperatureAtTrueAnomaly(double trueAnomaly)
        => Orbit.HasValue
        ? SurfaceTemperatureAtPeriapsis.Lerp(SurfaceTemperatureAtApoapsis, trueAnomaly <= Math.PI ? trueAnomaly / Math.PI : 2 - (trueAnomaly / Math.PI))
        : BlackbodyTemperature;

    private bool HasLiquidWater()
    {
        var maxTemp = GetMaxSurfaceTemperature();
        var minTemp = GetMinSurfaceTemperature();
        var avgTemp = GetAverageSurfaceTemperature();
        var pressure = Atmosphere.AtmosphericPressure;
        // Liquid water is checked at the min, max, and avg surface temperatures of the world,
        // under the assumption that if liquid water exists anywhere on the world, it is likely
        // to be found at at least one of those values, even if one or more are too extreme
        // (e.g. polar icecaps below freezing, or an equator above boiling).
        return Hydrosphere.Contains(Substances.All.Water.GetHomogeneousReference(), PhaseType.Liquid, maxTemp, pressure)
            || Hydrosphere.Contains(Substances.All.Seawater.GetHomogeneousReference(), PhaseType.Liquid, maxTemp, pressure)
            || Hydrosphere.Contains(Substances.All.Water.GetHomogeneousReference(), PhaseType.Liquid, minTemp, pressure)
            || Hydrosphere.Contains(Substances.All.Seawater.GetHomogeneousReference(), PhaseType.Liquid, minTemp, pressure)
            || Hydrosphere.Contains(Substances.All.Water.GetHomogeneousReference(), PhaseType.Liquid, avgTemp, pressure)
            || Hydrosphere.Contains(Substances.All.Seawater.GetHomogeneousReference(), PhaseType.Liquid, avgTemp, pressure);
    }

    private void ResetAllCachedTemperatures(List<Star> stars)
    {
        SetTemperatures(stars);

        ResetCachedTemperatures();
    }

    private void ResetCachedTemperatures()
    {
        _averageSurfaceTemperature = null;
        GreenhouseEffect = null;
        _insolationFactor_Equatorial = null;
        _insolationFactor_Polar = null;
        _maxSurfaceTemperature = null;
        _minSurfaceTemperature = null;
        _surfaceTemperature = null;
        _atmosphere?.ResetTemperatureDependentProperties(this);
    }

    private void ResetPressureDependentProperties()
    {
        _averageSurfaceTemperature = null;
        GreenhouseEffect = null;
        _insolationFactor_Equatorial = null;
        _insolationFactor_Polar = null;
        Atmosphere.ResetPressureDependentProperties(this);
    }

    private void SetAxis()
    {
        var precessionQ = Quaternion.CreateFromYawPitchRoll((float)AxialPrecession, 0, 0);
        var precessionVector = Vector3.Transform(Vector3.UnitX, precessionQ);
        var q = Quaternion.CreateFromAxisAngle(precessionVector, (float)AngleOfRotation);
        Axis = Vector3.Transform(Vector3.UnitY, q);
        AxisRotation = Quaternion.Conjugate(q);

        ResetCachedTemperatures();
    }

    private void SetTemperatures(List<Star>? stars)
    {
        BlackbodyTemperature = GetTemperatureAtPosition(Position, stars);

        if (!Orbit.HasValue)
        {
            SurfaceTemperatureAtApoapsis = BlackbodyTemperature;
            SurfaceTemperatureAtPeriapsis = BlackbodyTemperature;
        }
        else
        {
            // Actual position doesn't matter for temperature, only distance.
            SurfaceTemperatureAtApoapsis = Orbit.Value.Apoapsis.IsInfinity()
                ? BlackbodyTemperature
                : GetTemperatureAtPosition(Orbit.Value.OrbitedPosition + (Vector3<HugeNumber>.UnitX * Orbit.Value.Apoapsis), stars);

            // Actual position doesn't matter for temperature, only distance.
            SurfaceTemperatureAtPeriapsis = GetTemperatureAtPosition(Orbit.Value.OrbitedPosition + (Vector3<HugeNumber>.UnitX * Orbit.Value.Periapsis), stars);
        }

        AverageBlackbodyTemperature = Orbit.HasValue
            ? ((SurfaceTemperatureAtPeriapsis * (1 + Orbit.Value.Eccentricity)) + (SurfaceTemperatureAtApoapsis * (1 - Orbit.Value.Eccentricity))) / 2
            : BlackbodyTemperature;
    }
}
