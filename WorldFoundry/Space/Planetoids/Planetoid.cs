using NeverFoundry.DataStorage;
using NeverFoundry.MathAndScience;
using NeverFoundry.MathAndScience.Chemistry;
using NeverFoundry.MathAndScience.Constants.Numbers;
using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.MathAndScience.Randomization;
using NeverFoundry.MathAndScience.Time;
using NeverFoundry.WorldFoundry.Climate;
using NeverFoundry.WorldFoundry.Place;
using NeverFoundry.WorldFoundry.Space.Planetoids;
using NeverFoundry.WorldFoundry.SurfaceMapping;
using NeverFoundry.WorldFoundry.Utilities;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Number = NeverFoundry.MathAndScience.Numerics.Number;

namespace NeverFoundry.WorldFoundry.Space
{
    /// <summary>
    /// Any non-stellar celestial body, such as a planet or asteroid.
    /// </summary>
    [Serializable]
    [Newtonsoft.Json.JsonConverter(typeof(NewtonsoftJson.PlanetoidConverter))]
    [System.Text.Json.Serialization.JsonConverter(typeof(PlanetoidConverter))]
    public class Planetoid : CosmicLocation, IDisposable
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
        private const int DefaultMapResolution = 320;
        private const int DefaultSeasons = 4;
        private const int DensityForDwarf = 2000;
        private const int GiantMinDensity = 1100;
        private const int GiantSubMinDensity = 600;

        /// <summary>
        /// The minimum radius required to achieve hydrostatic equilibrium, in meters.
        /// </summary>
        private const int MinimumRadius = 600000;

        private const double ArcticLatitudeRange = Math.PI / 16;
        private const double ArcticLatitude = MathAndScience.Constants.Doubles.MathConstants.HalfPI - ArcticLatitudeRange;
        private const double FifthPI = Math.PI / 5;
        private const double EighthPI = Math.PI / 8;

        internal static readonly Number GiantSpace = new(2.5, 8);

        private static readonly Number _AsteroidSpace = new(400000);
        private static readonly Number _CometSpace = new(25000);

        /// <summary>
        /// The minimum to achieve hydrostatic equilibrium and be considered a dwarf planet.
        /// </summary>
        private static readonly Number _DwarfMinMassForType = new(3.4, 20);

        private static readonly Number _DwarfSpace = new(1.5, 6);

        /// <summary>
        /// Below this limit the planet will not have sufficient mass to retain hydrogen, and will
        /// be a terrestrial planet.
        /// </summary>
        private static readonly Number _GiantMinMassForType = new(6, 25);

        private static readonly Number _IcyRingDensity = 300;
        private static readonly double _LowTemp = (Substances.All.Water.MeltingPoint ?? 0) - 48;

        /// <summary>
        /// An arbitrary limit separating rogue dwarf planets from rogue planets.
        /// Within orbital systems, a calculated value for clearing the neighborhood is used instead.
        /// </summary>
        private static readonly Number _DwarfMaxMassForType = new(6, 25);

        /// <summary>
        /// At around this limit the planet will have sufficient mass to sustain fusion, and become
        /// a brown dwarf.
        /// </summary>
        private static readonly Number _GiantMaxMassForType = new(2.5, 28);

        /// <summary>
        /// At around this limit the planet will have sufficient mass to retain hydrogen, and become
        /// a giant.
        /// </summary>
        private static readonly Number _TerrestrialMaxMassForType = new(6, 25);

        private static readonly Number _RockyRingDensity = 1380;

        /// <summary>
        /// An arbitrary limit separating rogue dwarf planets from rogue planets. Within orbital
        /// systems, a calculated value for clearing the neighborhood is used instead.
        /// </summary>
        private static readonly Number _TerrestrialMinMassForType = new(2, 22);

        private static readonly Number _TerrestrialSpace = new(1.75, 7);

        internal readonly bool _earthlike;
        internal readonly HabitabilityRequirements? _habitabilityRequirements;
        internal readonly PlanetParams? _planetParams;

        internal double _blackbodyTemperature;
        internal Image? _elevationMap;
        internal string? _elevationMapPath;
        internal double _normalizedSeaLevel;
        internal Image?[]? _precipitationMaps;
        internal string?[]? _precipitationMapPaths;
        internal List<string>? _satelliteIDs;
        internal Image?[]? _snowfallMaps;
        internal string?[]? _snowfallMapPaths;
        internal double _surfaceTemperatureAtApoapsis;
        internal double _surfaceTemperatureAtPeriapsis;
        internal Image? _temperatureMapSummer;
        internal string? _temperatureMapSummerPath;
        internal Image? _temperatureMapWinter;
        internal string? _temperatureMapWinterPath;

        private double? _averageSurfaceTemperature;
        private bool _disposedValue;
        private double? _diurnalTemperatureVariation;
        private double? _maxSurfaceTemperature;
        private double? _minSurfaceTemperature;
        private int _seed1, _seed2, _seed3, _seed4, _seed5;
        private double _surfaceAlbedo;
        private double? _surfaceTemperature;

        /// <summary>
        /// The average albedo of this <see cref="Planetoid"/> (a value between 0 and 1).
        /// </summary>
        /// <remarks>
        /// This refers to the total albedo of the body, including any atmosphere, not just the
        /// surface albedo of the main body.
        /// </remarks>
        public double Albedo { get; private set; }

        /// <summary>
        /// The angle between the Y-axis and the axis of rotation of this <see cref="Planetoid"/>.
        /// Values greater than π/2 indicate clockwise rotation. Read-only; set with <see
        /// cref="SetAngleOfRotation(double)"/>.
        /// </summary>
        /// <remarks>
        /// Note that this is not the same as axial tilt if the <see cref="Planetoid"/>
        /// is in orbit; in that case axial tilt is relative to the normal of the orbital plane of
        /// the <see cref="Planetoid"/>, not the Y-axis.
        /// </remarks>
        public double AngleOfRotation { get; private set; }

        private Number? _angularVelocity;
        /// <summary>
        /// The angular velocity of this <see cref="Planetoid"/>, in radians per second. Read-only;
        /// set via <see cref="RotationalPeriod"/>.
        /// </summary>
        public Number AngularVelocity
            => _angularVelocity ??= RotationalPeriod.IsZero ? Number.Zero : MathConstants.TwoPI / RotationalPeriod;

        private Atmosphere? _atmosphere;
        /// <summary>
        /// The atmosphere possessed by this <see cref="Planetoid"/>.
        /// </summary>
        public Atmosphere Atmosphere
        {
            get => _atmosphere ?? new Atmosphere(0);
            private set => _atmosphere = value;
        }

        /// <summary>
        /// The angle between the X-axis and the orbital vector at which the vernal equinox of the
        /// northern hemisphere occurs. Read-only.
        /// </summary>
        public double AxialPrecession { get; private set; }

        private double? _axialTilt;
        /// <summary>
        /// The axial tilt of the <see cref="Planetoid"/> relative to its orbital plane, in radians.
        /// Values greater than π/2 indicate clockwise rotation. Read-only; set with <see
        /// cref="SetAxialTilt(double)"/>
        /// </summary>
        /// <remarks>
        /// If the <see cref="Planetoid"/> isn't orbiting anything, this is the same as the angle of
        /// rotation.
        /// </remarks>
        public double AxialTilt => _axialTilt ??= Orbit.HasValue ? AngleOfRotation - Orbit.Value.Inclination : AngleOfRotation;

        /// <summary>
        /// A <see cref="System.Numerics.Vector3"/> which represents the axis of this <see
        /// cref="Planetoid"/>. Read-only. Set with <see cref="SetAxialTilt(double)"/> or <see
        /// cref="SetAngleOfRotation(double)"/>.
        /// </summary>
        public System.Numerics.Vector3 Axis { get; private set; } = System.Numerics.Vector3.UnitY;

        /// <summary>
        /// A <see cref="System.Numerics.Quaternion"/> representing the rotation of the axis of this
        /// <see cref="Planetoid"/>. Read-only; set with <see cref="SetAxialTilt(double)"/> or
        /// <see cref="SetAngleOfRotation(double)"/>"/>
        /// </summary>
        public System.Numerics.Quaternion AxisRotation { get; private set; } = System.Numerics.Quaternion.Identity;

        /// <summary>
        /// Indicates whether or not this planet has a native population of living organisms.
        /// </summary>
        /// <remarks>
        /// The complexity of life is not presumed. If a planet is basically habitable (liquid
        /// surface water), life in at least a single-celled form may be indicated, and may affect
        /// the atmospheric composition.
        /// </remarks>
        public bool HasBiosphere { get; set; }

        /// <summary>
        /// Whether an elevation map has been assigned.
        /// </summary>
        public bool HasElevationMap => _elevationMap != null;

        /// <summary>
        /// Indicates whether this <see cref="Planetoid"/> has a strong magnetosphere.
        /// </summary>
        public bool HasMagnetosphere { get; private set; }

        /// <summary>
        /// Whether any precipitation maps have been assigned.
        /// </summary>
        public bool HasPrecipitationMap => _precipitationMaps != null;

        /// <summary>
        /// Whether any snowfall maps have been assigned.
        /// </summary>
        public bool HasSnowfallMap => _snowfallMaps != null;

        /// <summary>
        /// Whether any temperature maps have been assigned.
        /// </summary>
        public bool HasTemperatureMap => _temperatureMapSummer != null || _temperatureMapWinter != null;

        /// <summary>
        /// Whether a summer temperature map has been assigned.
        /// </summary>
        public bool HasTemperatureMapSummer => _temperatureMapSummer != null;

        /// <summary>
        /// Whether a winter temperature map has been assigned.
        /// </summary>
        public bool HasTemperatureMapWinter => _temperatureMapWinter != null;

        /// <summary>
        /// This planet's surface liquids and ices (not necessarily water).
        /// </summary>
        /// <remarks>
        /// Represented as a separate <see cref="IMaterial"/> rather than as a top layer of <see
        /// cref="CosmicLocation.Material"/> for ease of reference to both the solid surface
        /// layer, and the hydrosphere.
        /// </remarks>
        public IMaterial Hydrosphere { get; private set; } = MathAndScience.Chemistry.Material.Empty;

        /// <summary>
        /// The type discriminator for this type.
        /// </summary>
        public const string PlanetoidIdItemTypeName = ":Location:CosmicLocation:Planetoid:";
        /// <summary>
        /// A built-in, read-only type discriminator.
        /// </summary>
        public override string IdItemTypeName => PlanetoidIdItemTypeName;

        /// <summary>
        /// Indicates whether this is an asteroid.
        /// </summary>
        public bool IsAsteroid => PlanetType.Asteroid.HasFlag(PlanetType);

        /// <summary>
        /// Indicates whether this is a dwarf planet.
        /// </summary>
        public bool IsDwarf => PlanetType.AnyDwarf.HasFlag(PlanetType);

        /// <summary>
        /// Indicates whether this is a giant planet (including ice giants).
        /// </summary>
        public bool IsGiant => PlanetType.Giant.HasFlag(PlanetType);

        /// <summary>
        /// <para>
        /// Whether this planet is inhospitable to life.
        /// </para>
        /// <para>
        /// Typically due to a highly energetic or volatile star, which either produces a great deal
        /// of ionizing radiation, or has a rapidly shifting habitable zone, or both.
        /// </para>
        /// </summary>
        public bool IsInhospitable { get; private set; }

        /// <summary>
        /// Indicates whether this is a terrestrial planet.
        /// </summary>
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
        public double MaxElevation => _maxElevation ??= (IsGiant || PlanetType == PlanetType.Ocean ? 0 : 200000 / (double)SurfaceGravity);

        /// <summary>
        /// The type of <see cref="Planetoid"/>.
        /// </summary>
        public PlanetType PlanetType { get; }

        internal List<PlanetaryRing>? _rings;
        /// <summary>
        /// The collection of rings around this <see cref="Planetoid"/>.
        /// </summary>
        public IEnumerable<PlanetaryRing> Rings => _rings ?? Enumerable.Empty<PlanetaryRing>();

        /// <summary>
        /// The amount of seconds after the beginning of time of the orbited body's transit at the
        /// prime meridian, if <see cref="RotationalPeriod"/> was unchanged (i.e. solar noon, on a
        /// planet which orbits a star).
        /// </summary>
        public Number RotationalOffset { get; private set; }

        /// <summary>
        /// The length of time it takes for this <see cref="Planetoid"/> to rotate once about its axis, in seconds.
        /// </summary>
        public Number RotationalPeriod { get; private set; }

        private readonly List<Resource> _resources = new();
        /// <summary>
        /// The resources of this <see cref="Planetoid"/>.
        /// </summary>
        public IEnumerable<Resource> Resources => _resources;

        private double? _seaLevel;
        /// <summary>
        /// The elevation of sea level relative to the mean surface elevation of the planet, in
        /// meters.
        /// </summary>
        public double SeaLevel
        {
            get => _seaLevel ??= _normalizedSeaLevel * MaxElevation;
            private set
            {
                _seaLevel = value;
                _normalizedSeaLevel = value / MaxElevation;
            }
        }

        /// <summary>
        /// The total temperature of this <see cref="Planetoid"/>, not including atmosphereic
        /// effects, averaged over its orbit, in K.
        /// </summary>
        internal double AverageBlackbodyTemperature { get; private set; }

        internal double? GreenhouseEffect { get; set; }

        internal bool HasAllWeatherMaps
            => _precipitationMaps != null
            && _snowfallMaps != null
            && _temperatureMapSummer != null
            && _temperatureMapWinter != null;

        private double? _insolationFactor_Equatorial;
        internal double InsolationFactor_Equatorial
        {
            get => _insolationFactor_Equatorial ??= GetInsolationFactor();
            set => _insolationFactor_Equatorial = value;
        }

        internal int MappedSeasons => _precipitationMaps?.Length ?? 0;

        private double? _summerSolsticeTrueAnomaly;
        internal double SummerSolsticeTrueAnomaly
            => _summerSolsticeTrueAnomaly ??= (MathAndScience.Constants.Doubles.MathConstants.HalfPI
            - (Orbit?.LongitudeOfPeriapsis ?? 0))
            % MathAndScience.Constants.Doubles.MathConstants.TwoPI;

        private double? _winterSolsticeTrueAnomaly;
        internal double WinterSolsticeTrueAnomaly
            => _winterSolsticeTrueAnomaly ??= (MathAndScience.Constants.Doubles.MathConstants.ThreeHalvesPI
            - (Orbit?.LongitudeOfPeriapsis ?? 0))
            % MathAndScience.Constants.Doubles.MathConstants.TwoPI;

        private protected override string BaseTypeName => PlanetType switch
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

        private bool CanHaveWater => PlanetType switch
        {
            PlanetType.Carbon => false,
            PlanetType.Iron => false,
            PlanetType.Lava => false,
            PlanetType.LavaDwarf => false,
            _ => true,
        };

        private double? _insolationFactor_Polar;
        private double InsolationFactor_Polar => _insolationFactor_Polar ??= GetInsolationFactor(true);

        private double? _lapseRateDry;
        private double LapseRateDry => _lapseRateDry ??= (double)SurfaceGravity / MathAndScience.Constants.Doubles.ScienceConstants.CpDryAir;

        private FastNoise? _noise1;
        private FastNoise Noise1 => _noise1 ??= new FastNoise(_seed1, 0.8, FastNoise.NoiseType.SimplexFractal, octaves: 6);

        private FastNoise? _noise2;
        private FastNoise Noise2 => _noise2 ??= new FastNoise(_seed2, 0.6, FastNoise.NoiseType.SimplexFractal, FastNoise.FractalType.Billow, octaves: 6);

        private FastNoise? _noise3;
        private FastNoise Noise3 => _noise3 ??= new FastNoise(_seed3, 1.2, FastNoise.NoiseType.Simplex);

        private FastNoise? _noise4;
        private FastNoise Noise4 => _noise4 ??= new FastNoise(_seed4, 1.0, FastNoise.NoiseType.Simplex);

        private FastNoise? _noise5;
        private FastNoise Noise5 => _noise5 ??= new FastNoise(_seed5, 3.0, FastNoise.NoiseType.SimplexFractal, octaves: 3);

        private protected override string? TypeNamePrefix => PlanetType switch
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

        private protected override string? TypeNameSuffix => PlanetType switch
        {
            PlanetType.AsteroidC => "c",
            PlanetType.AsteroidM => "m",
            PlanetType.AsteroidS => "s",
            _ => null,
        };

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
        /// When this method returns, will be set to a <see cref="List{T}"/> of <see
        /// cref="Planetoid"/>s containing any satellites generated for the planet during the
        /// creation process.
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
        /// <param name="satellite">
        /// If <see langword="true"/>, indicates that this <see cref="Planetoid"/> is being
        /// generated as a satellite of another.
        /// </param>
        /// <param name="seed">
        /// A value used to seed the random generator.
        /// </param>
        public Planetoid(
            PlanetType planetType,
            CosmicLocation? parent,
            Star? star,
            List<Star> stars,
            Vector3 position,
            out List<Planetoid> satellites,
            OrbitalParameters? orbit = null,
            PlanetParams? planetParams = null,
            HabitabilityRequirements? habitabilityRequirements = null,
            bool satellite = false,
            uint? seed = null) : base(parent?.Id, CosmicStructureType.Planetoid)
        {
            PlanetType = planetType;
            _planetParams = planetParams;
            _habitabilityRequirements = habitabilityRequirements;
            if (planetParams.HasValue
                && habitabilityRequirements.HasValue
                && planetParams.Value.Equals(PlanetParams.Earthlike)
                && habitabilityRequirements.Value.Equals(HabitabilityRequirements.HumanHabitabilityRequirements))
            {
                _earthlike = true;
            }

            if (star is null && !orbit.HasValue)
            {
                star = Randomizer.Instance.Next(stars);
            }

            satellites = Configure(parent, stars, star, position, satellite, orbit, seed);

            if (parent is not null && !orbit.HasValue && !Orbit.HasValue)
            {
                if (parent is AsteroidField asteroidField)
                {
                    orbit = asteroidField.GetChildOrbit();
                }
                else
                {
                    orbit = parent.StructureType switch
                    {
                        CosmicStructureType.GalaxySubgroup => Position.IsZero() ? null : parent.GetGalaxySubgroupChildOrbit(),
                        CosmicStructureType.SpiralGalaxy
                            or CosmicStructureType.EllipticalGalaxy
                            or CosmicStructureType.DwarfGalaxy => Position.IsZero() ? (OrbitalParameters?)null : parent.GetGalaxyChildOrbit(),
                        CosmicStructureType.GlobularCluster => Position.IsZero() ? (OrbitalParameters?)null : parent.GetGlobularClusterChildOrbit(),
                        CosmicStructureType.StarSystem => parent is StarSystem && !Position.IsZero()
                            ? OrbitalParameters.GetFromEccentricity(parent.Mass, parent.Position, Randomizer.Instance.PositiveNormalDistributionSample(0, 0.05))
                            : (OrbitalParameters?)null,
                        _ => null,
                    };
                }
            }
            if (orbit.HasValue && !Orbit.HasValue)
            {
                Space.Orbit.AssignOrbit(this, orbit.Value);
            }
        }

        internal Planetoid(
            string id,
            uint seed,
            PlanetType planetType,
            string? parentId,
            Vector3[]? absolutePosition,
            string? name,
            Vector3 velocity,
            Orbit? orbit,
            Vector3 position,
            double? temperature,
            double angleOfRotation,
            Number rotationalPeriod,
            List<string>? satelliteIds,
            List<PlanetaryRing>? rings,
            double blackbodyTemperature,
            double surfaceTemperatureAtApoapsis,
            double surfaceTemperatureAtPeriapsis,
            bool isInhospitable,
            bool earthlike,
            PlanetParams? planetParams,
            HabitabilityRequirements? habitabilityRequirements,
            string? elevationMapPath,
            string?[]? precipitationMapPaths,
            string?[]? snowfallMapPaths,
            string? temperatureMapSummerPath,
            string? temperatureMapWinterPath)
            : base(
                id,
                seed,
                CosmicStructureType.Planetoid,
                parentId,
                absolutePosition,
                name,
                velocity,
                orbit)
        {
            PlanetType = planetType;
            AngleOfRotation = angleOfRotation;
            RotationalPeriod = rotationalPeriod;
            _satelliteIDs = satelliteIds;
            _rings = rings;
            _blackbodyTemperature = blackbodyTemperature;
            _surfaceTemperatureAtApoapsis = surfaceTemperatureAtApoapsis;
            _surfaceTemperatureAtPeriapsis = surfaceTemperatureAtPeriapsis;
            IsInhospitable = isInhospitable;
            _earthlike = earthlike;
            _planetParams = earthlike ? PlanetParams.Earthlike : planetParams;
            _habitabilityRequirements = earthlike ? HabitabilityRequirements.HumanHabitabilityRequirements : habitabilityRequirements;
            _elevationMapPath = elevationMapPath;
            _precipitationMapPaths = precipitationMapPaths;
            _snowfallMapPaths = snowfallMapPaths;
            _temperatureMapSummerPath = temperatureMapSummerPath;
            _temperatureMapWinterPath = temperatureMapWinterPath;

            AverageBlackbodyTemperature = Orbit.HasValue
                ? ((_surfaceTemperatureAtPeriapsis * (1 + Orbit.Value.Eccentricity)) + (_surfaceTemperatureAtApoapsis * (1 - Orbit.Value.Eccentricity))) / 2
                : _blackbodyTemperature;

            var rehydrator = GetRehydrator(seed);
            ReconstituteMaterial(
                rehydrator,
                position,
                temperature,
                Orbit?.SemiMajorAxis ?? 0);
            ReconstituteHydrosphere(rehydrator);
            GenerateAtmosphere(rehydrator);
            GenerateResources(rehydrator);
        }

        private Planetoid(SerializationInfo info, StreamingContext context) : this(
            (string?)info.GetValue(nameof(Id), typeof(string)) ?? string.Empty,
            (uint?)info.GetValue(nameof(Seed), typeof(uint)) ?? default,
            (PlanetType?)info.GetValue(nameof(PlanetType), typeof(PlanetType)) ?? PlanetType.Comet,
            (string?)info.GetValue(nameof(ParentId), typeof(string)) ?? string.Empty,
            (Vector3[]?)info.GetValue(nameof(AbsolutePosition), typeof(Vector3[])),
            (string?)info.GetValue(nameof(Name), typeof(string)),
            (Vector3?)info.GetValue(nameof(Velocity), typeof(Vector3)) ?? default,
            (Orbit?)info.GetValue(nameof(Orbit), typeof(Orbit?)),
            (Vector3?)info.GetValue(nameof(Position), typeof(Vector3)) ?? default,
            (double?)info.GetValue(nameof(Temperature), typeof(double?)),
            (double?)info.GetValue(nameof(AngleOfRotation), typeof(double)) ?? default,
            (Number?)info.GetValue(nameof(RotationalPeriod), typeof(Number)) ?? Number.Zero,
            (List<string>?)info.GetValue(nameof(_satelliteIDs), typeof(List<string>)),
            (List<PlanetaryRing>?)info.GetValue(nameof(Rings), typeof(List<PlanetaryRing>)),
            (double?)info.GetValue(nameof(_blackbodyTemperature), typeof(double)) ?? default,
            (double?)info.GetValue(nameof(_surfaceTemperatureAtApoapsis), typeof(double)) ?? default,
            (double?)info.GetValue(nameof(_surfaceTemperatureAtPeriapsis), typeof(double)) ?? default,
            (bool?)info.GetValue(nameof(IsInhospitable), typeof(bool)) ?? default,
            (bool?)info.GetValue(nameof(_earthlike), typeof(bool)) ?? default,
            (PlanetParams?)info.GetValue(nameof(_planetParams), typeof(PlanetParams?)),
            (HabitabilityRequirements?)info.GetValue(nameof(_habitabilityRequirements), typeof(HabitabilityRequirements?)),
            (string?)info.GetValue(nameof(_elevationMapPath), typeof(string)),
            (string?[]?)info.GetValue(nameof(_precipitationMapPaths), typeof(string?[])),
            (string?[]?)info.GetValue(nameof(_snowfallMapPaths), typeof(string?[])),
            (string?)info.GetValue(nameof(_temperatureMapSummerPath), typeof(string)),
            (string?)info.GetValue(nameof(_temperatureMapWinterPath), typeof(string)))
        { }

        /// <summary>
        /// Generates a new <see cref="Planetoid"/> instance in a new <see cref="StarSystem"/>.
        /// </summary>
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
        /// <param name="planetType">The type of planet to generate.</param>
        /// <param name="starSystemDefinition">
        /// <para>
        /// Any requirements the newly created <see cref="StarSystem"/> should meet.
        /// </para>
        /// <para>
        /// If omitted, a system with a single star similar to Sol, Earth's sun, will be generated.
        /// </para>
        /// </param>
        /// <param name="parent">
        /// The containing parent location for the new system (if any).
        /// </param>
        /// <param name="position">
        /// <para>
        /// The position for new system.
        /// </para>
        /// <para>
        /// If omitted, the system will be placed at <see cref="Vector3.Zero"/>.
        /// </para>
        /// </param>
        /// <param name="orbit">
        /// An optional orbit to assign to the child.
        /// </param>
        /// <param name="planetParams">
        /// A set of <see cref="PlanetParams"/>. If omitted, earthlike values will be used.
        /// </param>
        /// <param name="habitabilityRequirements">
        /// An optional set of <see cref="HabitabilityRequirements"/>. If omitted, human
        /// habiltability requirements will be used.
        /// </param>
        /// <returns>A planet with the given parameters.</returns>
        public static Planetoid? GetPlanetForNewStar(
            out List<CosmicLocation> children,
            PlanetType planetType = PlanetType.Terrestrial,
            StarSystemChildDefinition? starSystemDefinition = null,
            CosmicLocation? parent = null,
            Vector3? position = null,
            OrbitalParameters? orbit = null,
            PlanetParams? planetParams = null,
            HabitabilityRequirements? habitabilityRequirements = null)
        {
            var system = starSystemDefinition is null
                ? new StarSystem(parent, position ?? Vector3.Zero, out children, sunlike: true)
                : starSystemDefinition.GetStarSystem(parent, position ?? Vector3.Zero, out children);
            if (system is null)
            {
                return null;
            }

            var pParams = planetParams ?? PlanetParams.Earthlike;
            var requirements = habitabilityRequirements ?? HabitabilityRequirements.HumanHabitabilityRequirements;
            var sanityCheck = 0;
            Planetoid? planet;
            List<Planetoid> childSatellites;
            do
            {
                planet = new Planetoid(
                    planetType,
                    system,
                    null,
                    children.OfType<Star>().ToList(),
                    new Vector3(new Number(15209, 7), Number.Zero, Number.Zero),
                    out childSatellites,
                    orbit,
                    pParams,
                    requirements);
                sanityCheck++;
                if (planet.IsHabitable(requirements) == UninhabitabilityReason.None)
                {
                    break;
                }
                else
                {
                    planet = null;
                }
            } while (sanityCheck <= 100);
            if (planet is not null)
            {
                // Clear pregenerated planets whose orbits are too close to this one.
                if (planet.Orbit.HasValue)
                {
                    var planetOrbitalPath = new Torus(
                        (planet.Orbit.Value.Apoapsis + planet.Orbit.Value.Periapsis) / 2,
                        Number.Min(
                            (planet.Orbit.Value.Apoapsis + planet.Orbit.Value.Periapsis) / 2,
                            (Number.Abs(planet.Orbit.Value.Apoapsis - planet.Orbit.Value.Periapsis) / 2) + planet.Orbit.Value.GetSphereOfInfluenceRadius(planet.Mass)));
                    children.RemoveAll(x => x is Planetoid p
                        && p.Orbit.HasValue
                        && planetOrbitalPath.Intersects(new Torus(
                            (p.Orbit.Value.Apoapsis + p.Orbit.Value.Periapsis) / 2,
                            Number.Min(
                                (p.Orbit.Value.Apoapsis + p.Orbit.Value.Periapsis) / 2,
                                (Number.Abs(p.Orbit.Value.Apoapsis - p.Orbit.Value.Periapsis) / 2) + p.Orbit.Value.GetSphereOfInfluenceRadius(p.Mass)))));
                }
                children.Add(system);
                children.AddRange(childSatellites);
            }
            return planet;
        }

        /// <summary>
        /// <para>
        /// Generates a new <see cref="Planetoid"/> instance with no containing parent location, but
        /// assuming a star with sunlike characteristics.
        /// </para>
        /// <para>
        /// This method is intended to be useful when a complete hierarchy of cosmic entities is not
        /// expected to be generated (i.e. a <see cref="StarSystem"/> with <see cref="Star"/>s).
        /// Instead, the characteristics of the planet are determined with the assumption that a
        /// host star system exists, without actually defining such an entity.
        /// </para>
        /// </summary>
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
        /// <param name="planetType">The type of planet to generate.</param>
        /// <param name="orbit">
        /// An optional orbit to assign to the child.
        /// </param>
        /// <param name="planetParams">
        /// A set of <see cref="PlanetParams"/>. If omitted, earthlike values will be used.
        /// </param>
        /// <param name="habitabilityRequirements">
        /// An optional set of <see cref="HabitabilityRequirements"/>. If omitted, human
        /// habiltability requirements will be used.
        /// </param>
        /// <param name="seed">
        /// A value used to seed the random generator.
        /// </param>
        /// <returns>A planet with the given parameters.</returns>
        public static Planetoid? GetPlanetForSunlikeStar(
            out List<CosmicLocation> children,
            PlanetType planetType = PlanetType.Terrestrial,
            OrbitalParameters? orbit = null,
            PlanetParams? planetParams = null,
            HabitabilityRequirements? habitabilityRequirements = null,
            uint? seed = null)
        {
            var pParams = planetParams ?? PlanetParams.Earthlike;
            var requirements = habitabilityRequirements ?? HabitabilityRequirements.HumanHabitabilityRequirements;

            children = new List<CosmicLocation>();

            var fakeStar = Star.NewSunlike(null, Vector3.Zero);
            if (fakeStar is null)
            {
                return null;
            }

            var sanityCheck = 0;
            Planetoid? planet;
            List<Planetoid> childSatellites;
            do
            {
                planet = new Planetoid(
                    planetType,
                    null,
                    fakeStar,
                    new List<Star> { fakeStar },
                    new Vector3(new Number(15209, 7), Number.Zero, Number.Zero),
                    out childSatellites,
                    orbit,
                    pParams,
                    requirements,
                    false,
                    seed);
                sanityCheck++;
                if (planet.IsHabitable(requirements) == UninhabitabilityReason.None)
                {
                    break;
                }
                else
                {
                    planet = null;
                }
            } while (sanityCheck <= 100);
            if (planet is not null)
            {
                children.AddRange(childSatellites);
            }
            return planet;
        }

        /// <summary>
        /// Given a star, generates a terrestrial planet with the given parameters, and puts the
        /// planet in orbit around the star.
        /// </summary>
        /// <param name="dataStore">
        /// The <see cref="IDataStore"/> from which to retrieve instances.
        /// </param>
        /// <param name="star">
        /// <para>
        /// A star which the new planet will orbit, at a distance suitable for habitability.
        /// </para>
        /// <para>
        /// Note: if the star system already has planets in orbit around the given star, the newly
        /// created planet may be placed into an unrealistically close orbit to another body,
        /// especially if such an orbit is required in order to satisfy any temperature
        /// requirements. For more realistic results, you may wish to generate your target planet
        /// and system together with <see cref="GetPlanetForNewStar(out List{CosmicLocation},
        /// PlanetType, StarSystemChildDefinition?, CosmicLocation?, Vector3?, OrbitalParameters?,
        /// PlanetParams?, HabitabilityRequirements?)"/>. That method not only generates a planet
        /// and star system according to provided specifications, but ensures that any additional
        /// planets generated for the system take up orbits which are in accordance with the
        /// initial, target planet.
        /// </para>
        /// </param>
        /// <param name="planetParams">
        /// A set of <see cref="PlanetParams"/>. If omitted, earthlike values will be used.
        /// </param>
        /// <param name="habitabilityRequirements">
        /// An optional set of <see cref="HabitabilityRequirements"/>. If omitted, human
        /// habiltability requirements will be used.
        /// </param>
        /// <returns>
        /// <para>
        /// A planet with the given parameters. May be <see langword="null"/> if no planet could be
        /// generated.
        /// </para>
        /// <para>
        /// Also, a <see cref="List{T}"/> of <see cref="Planetoid"/>s containing any satellites
        /// generated during the creation process.
        /// </para>
        /// </returns>
        public static async Task<(Planetoid? planet, List<Planetoid> children)> GetPlanetForStar(
            IDataStore dataStore,
            Star star,
            PlanetParams? planetParams = null,
            HabitabilityRequirements? habitabilityRequirements = null)
        {
            var stars = new List<Star>();
            var parent = await star.GetParentAsync(dataStore).ConfigureAwait(false);
            if (parent is StarSystem system)
            {
                await foreach (var item in system.GetStarsAsync(dataStore))
                {
                    stars.Add(item);
                }
            }
            else
            {
                stars.Add(star);
            }

            var pParams = planetParams ?? PlanetParams.Earthlike;
            var requirements = habitabilityRequirements ?? HabitabilityRequirements.HumanHabitabilityRequirements;
            var sanityCheck = 0;
            Planetoid? planet;
            List<Planetoid> childSatellites;
            do
            {
                planet = new Planetoid(
                    PlanetType.Terrestrial,
                    parent as CosmicLocation,
                    star,
                    stars,
                    parent is StarSystem sys
                        ? Vector3.UnitX * Randomizer.Instance.NextNumber(sys.Shape.ContainingRadius)
                        : Randomizer.Instance.NextVector3Number(Number.Zero, parent?.Shape.ContainingRadius ?? Number.MaxValue),
                    out childSatellites,
                    null,
                    pParams,
                    requirements);
                sanityCheck++;
                if (planet.IsHabitable(requirements) == UninhabitabilityReason.None)
                {
                    break;
                }
                else
                {
                    planet = null;
                }
            } while (sanityCheck <= 100);
            var satellites = planet is null ? new List<Planetoid>() : childSatellites;
            return (planet, satellites);
        }

        /// <summary>
        /// Given a galaxy, generates a terrestrial planet with the given parameters, orbiting a
        /// Sol-like star in a new system in the given galaxy.
        /// </summary>
        /// <param name="dataStore">
        /// The <see cref="IDataStore"/> from which to retrieve instances.
        /// </param>
        /// <param name="galaxy">A galaxy in which to situate the new planet.</param>
        /// <param name="planetParams">
        /// A set of <see cref="PlanetParams"/>. If omitted, earthlike values will be used.
        /// </param>
        /// <param name="habitabilityRequirements">
        /// An optional set of <see cref="HabitabilityRequirements"/>. If omitted, human
        /// habiltability requirements will be used.
        /// </param>
        /// <returns>
        /// <para>
        /// A planet with the given parameters. May be <see langword="null"/> if no planet could be
        /// generated.
        /// </para>
        /// <para>
        /// Also, a <see cref="List{T}"/> of <see cref="CosmicLocation"/>s containing any child locations
        /// generated during the creation process.
        /// </para>
        /// </returns>
        public static async Task<(Planetoid? planet, List<CosmicLocation> children)> GetPlanetForGalaxyAsync(
            IDataStore dataStore,
            CosmicLocation galaxy,
            PlanetParams? planetParams = null,
            HabitabilityRequirements? habitabilityRequirements = null)
        {
            var children = new List<CosmicLocation>();

            if (!CosmicStructureType.Galaxy.HasFlag(galaxy.StructureType))
            {
                return (null, children);
            }

            var galaxyChildren = new List<Location>();
            await foreach (var item in galaxy.GetChildrenAsync(dataStore))
            {
                galaxyChildren.Add(item);
            }

            var pos = galaxy.GetOpenSpace(StarSystem.StarSystemSpace, galaxyChildren);
            if (!pos.HasValue)
            {
                return (null, children);
            }

            var planet = GetPlanetForNewStar(
                out children,
                parent: galaxy,
                position: pos,
                planetParams: planetParams,
                habitabilityRequirements: habitabilityRequirements);
            return (planet, children);
        }

        /// <summary>
        /// Given a universe, generates a terrestrial planet with the given parameters, orbiting a
        /// Sol-like star in a new spiral galaxy in the given universe.
        /// </summary>
        /// <param name="dataStore">
        /// The <see cref="IDataStore"/> from which to retrieve instances.
        /// </param>
        /// <param name="universe">A universe in which to situate the new planet.</param>
        /// <param name="planetParams">
        /// A set of <see cref="PlanetParams"/>. If omitted, earthlike values will be used.
        /// </param>
        /// <param name="habitabilityRequirements">
        /// An optional set of <see cref="HabitabilityRequirements"/>. If omitted, human
        /// habiltability requirements will be used.
        /// </param>
        /// <returns>
        /// <para>
        /// A planet with the given parameters. May be <see langword="null"/> if no planet could be
        /// generated.
        /// </para>
        /// <para>
        /// Also, a <see cref="List{T}"/> of <see cref="CosmicLocation"/>s containing any child locations
        /// generated during the creation process.
        /// </para>
        /// </returns>
        public static async Task<(Planetoid? planet, List<CosmicLocation> children)> GetPlanetForUniverseAsync(
            IDataStore dataStore,
            CosmicLocation universe,
            PlanetParams? planetParams = null,
            HabitabilityRequirements? habitabilityRequirements = null)
        {
            var children = new List<CosmicLocation>();

            if (universe.StructureType != CosmicStructureType.Universe)
            {
                return (null, children);
            }

            var (gsc, gscSub) = await universe.GenerateChildAsync(dataStore, CosmicStructureType.Supercluster).ConfigureAwait(false);
            if (gsc is null)
            {
                return (null, children);
            }
            children.Add(gsc);
            children.AddRange(gscSub);

            var (gc, gcSub) = await gsc.GenerateChildAsync(dataStore, CosmicStructureType.GalaxyCluster).ConfigureAwait(false);
            if (gc is null)
            {
                return (null, children);
            }
            children.Add(gc);
            children.AddRange(gcSub);

            CosmicLocation? galaxy = null;
            var sanityCheck = 0;
            while (galaxy is null && sanityCheck < 100)
            {
                sanityCheck++;
                var (gg, ggSub) = await gc.GenerateChildAsync(dataStore, CosmicStructureType.GalaxyGroup).ConfigureAwait(false);
                if (gg is null || ggSub is null)
                {
                    continue;
                }
                galaxy = ggSub.Find(x => x.StructureType == CosmicStructureType.SpiralGalaxy);
                if (galaxy is not null)
                {
                    children.Add(gg);
                    children.AddRange(ggSub);
                }
            }
            if (galaxy is null)
            {
                return (null, children);
            }

            var (planet, satellites) = await GetPlanetForGalaxyAsync(dataStore, galaxy, planetParams, habitabilityRequirements).ConfigureAwait(false);
            children.AddRange(satellites);
            return (planet, children);
        }

        /// <summary>
        /// Generates a terrestrial planet with the given parameters, orbiting a Sol-like star in a
        /// spiral galaxy in a new universe.
        /// </summary>
        /// <param name="dataStore">
        /// The <see cref="IDataStore"/> from which to retrieve instances.
        /// </param>
        /// <param name="planetParams">
        /// A set of <see cref="PlanetParams"/>. If omitted, earthlike values will be used.
        /// </param>
        /// <param name="habitabilityRequirements">
        /// An optional set of <see cref="HabitabilityRequirements"/>. If omitted, human
        /// habiltability requirements will be used.
        /// </param>
        /// <returns>
        /// <para>
        /// A planet with the given parameters. May be <see langword="null"/> if no planet could be
        /// generated.
        /// </para>
        /// <para>
        /// Also, a <see cref="List{T}"/> of <see cref="CosmicLocation"/>s containing any child locations
        /// generated during the creation process.
        /// </para>
        /// </returns>
        public static async Task<(Planetoid? planet, List<CosmicLocation> children)> GetPlanetForNewUniverseAsync(
            IDataStore dataStore,
            PlanetParams? planetParams = null,
            HabitabilityRequirements? habitabilityRequirements = null)
        {
            var universe = New(CosmicStructureType.Universe, null, Vector3.Zero, out var children);
            if (universe is null)
            {
                return (null, new List<CosmicLocation>());
            }
            var (planet, subChildren) = await GetPlanetForUniverseAsync(dataStore, universe, planetParams, habitabilityRequirements).ConfigureAwait(false);
            children.Add(universe);
            children.AddRange(subChildren);
            return (planet, children);
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
        /// Performs application-defined tasks associated with freeing, releasing, or resetting
        /// unmanaged resources.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _elevationMap?.Dispose();
                    _temperatureMapSummer?.Dispose();
                    _temperatureMapWinter?.Dispose();

                    if (_precipitationMaps is not null)
                    {
                        for (var i = 0; i < _precipitationMaps.Length; i++)
                        {
                            _precipitationMaps[i]?.Dispose();
                        }
                    }

                    if (_snowfallMaps is not null)
                    {
                        for (var i = 0; i < _snowfallMaps.Length; i++)
                        {
                            _snowfallMaps[i]?.Dispose();
                        }
                    }
                }

                _disposedValue = true;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting
        /// unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Stores an image as the elevation map for this planet.
        /// </summary>
        /// <param name="image">The image to load.</param>
        /// <param name="mapLoader">
        /// <para>
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used to store the
        /// image.
        /// </para>
        /// <para>
        /// If <see langword="null"/> the image will not be stored, and will only be available while
        /// this region persists in memory.
        /// </para>
        /// </param>
        public async Task AssignElevationMapAsync(Image? image, ISurfaceMapLoader? mapLoader = null)
        {
            if (image is null)
            {
                _elevationMap = null;
                _elevationMapPath = null;
                return;
            }
            if (mapLoader is null)
            {
                _elevationMap = image;
                _elevationMapPath = null;
            }
            else
            {
                _elevationMapPath = await mapLoader.SaveAsync(image, Id, "elevation").ConfigureAwait(false);
                if (!string.IsNullOrEmpty(_elevationMapPath))
                {
                    _elevationMap = image;
                }
            }
        }

        /// <summary>
        /// Stores an image as a precipitation map for this planet, adding it to any existing
        /// collection.
        /// </summary>
        /// <param name="image">The image to load.</param>
        /// <param name="mapLoader">
        /// <para>
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used to store the
        /// image.
        /// </para>
        /// <para>
        /// If <see langword="null"/> the image will not be stored, and will only be available while
        /// this region persists in memory.
        /// </para>
        /// </param>
        public async Task AssignPrecipitationMapAsync(Image? image, ISurfaceMapLoader? mapLoader = null)
        {
            if (image is null)
            {
                _precipitationMaps = null;
                _precipitationMapPaths = null;
                return;
            }

            if (mapLoader is not null)
            {
                var path = await mapLoader.SaveAsync(image, Id, $"precipitation_{(_precipitationMaps?.Length ?? -1) + 1}").ConfigureAwait(false);
                if (string.IsNullOrEmpty(path))
                {
                    return;
                }
                else if (_precipitationMapPaths is null)
                {
                    _precipitationMapPaths = new string?[] { path };
                }
                else
                {
                    var old = _precipitationMapPaths;
                    _precipitationMapPaths = new string?[old.Length + 1];
                    Array.Copy(old, _precipitationMapPaths, old.Length);
                    _precipitationMapPaths[old.Length] = path;
                }
            }

            if (_precipitationMaps is null)
            {
                _precipitationMaps = new Image?[] { image };
            }
            else
            {
                var old = _precipitationMaps;
                _precipitationMaps = new Image?[old.Length + 1];
                Array.Copy(old, _precipitationMaps, old.Length);
                _precipitationMaps[old.Length] = image;
            }
        }

        /// <summary>
        /// Stores a set of images as the precipitation maps for this planet.
        /// </summary>
        /// <param name="images">
        /// The images to load. The set is presumed to be evenly spaced over the course of a year.
        /// </param>
        /// <param name="mapLoader">
        /// <para>
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used to store the
        /// image.
        /// </para>
        /// <para>
        /// If <see langword="null"/> the image will not be stored, and will only be available while
        /// this region persists in memory.
        /// </para>
        /// </param>
        public async Task AssignPrecipitationMapsAsync(IEnumerable<Image> images, ISurfaceMapLoader? mapLoader = null)
        {
            var list = images.ToList();
            if (list.Count == 0)
            {
                _precipitationMaps = null;
                _precipitationMapPaths = null;
                return;
            }
            _precipitationMaps = new Image?[list.Count];
            if (mapLoader is null)
            {
                _precipitationMapPaths = null;
            }
            else
            {
                _precipitationMapPaths = new string?[list.Count];
            }
            for (var i = 0; i < list.Count; i++)
            {
                if (mapLoader is null)
                {
                    _precipitationMaps[i] = list[i];
                }
                else
                {
                    var path = await mapLoader.SaveAsync(list[i], Id, $"precipitation_{i}").ConfigureAwait(false);
                    if (!string.IsNullOrEmpty(path))
                    {
                        _precipitationMaps[i] = list[i];
                        _precipitationMapPaths![i] = path;
                    }
                }
            }
        }

        /// <summary>
        /// Stores an image as a snowfall map for this planet, adding it to any existing
        /// collection.
        /// </summary>
        /// <param name="image">The image to load.</param>
        /// <param name="mapLoader">
        /// <para>
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used to store the
        /// image.
        /// </para>
        /// <para>
        /// If <see langword="null"/> the image will not be stored, and will only be available while
        /// this region persists in memory.
        /// </para>
        /// </param>
        public async Task AssignSnowfallMapAsync(Image? image, ISurfaceMapLoader? mapLoader = null)
        {
            if (image is null)
            {
                _snowfallMaps = null;
                _snowfallMapPaths = null;
                return;
            }

            if (mapLoader is not null)
            {
                var path = await mapLoader.SaveAsync(image, Id, $"snowfall_{(_snowfallMaps?.Length ?? -1) + 1}").ConfigureAwait(false);
                if (string.IsNullOrEmpty(path))
                {
                    return;
                }
                else if (_snowfallMapPaths is null)
                {
                    _snowfallMapPaths = new string?[] { path };
                }
                else
                {
                    var old = _snowfallMapPaths;
                    _snowfallMapPaths = new string?[old.Length + 1];
                    Array.Copy(old, _snowfallMapPaths, old.Length);
                    _snowfallMapPaths[old.Length] = path;
                }
            }

            if (_snowfallMaps is null)
            {
                _snowfallMaps = new Image?[] { image };
            }
            else
            {
                var old = _snowfallMaps;
                _snowfallMaps = new Image?[old.Length + 1];
                Array.Copy(old, _snowfallMaps, old.Length);
                _snowfallMaps[old.Length] = image;
            }
        }

        /// <summary>
        /// Stores a set of images as the snowfall maps for this planet.
        /// </summary>
        /// <param name="images">
        /// The images to load. The set is presumed to be evenly spaced over the course of a year.
        /// </param>
        /// <param name="mapLoader">
        /// <para>
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used to store the
        /// image.
        /// </para>
        /// <para>
        /// If <see langword="null"/> the image will not be stored, and will only be available while
        /// this region persists in memory.
        /// </para>
        /// </param>
        public async Task AssignSnowfallMapsAsync(IEnumerable<Image> images, ISurfaceMapLoader? mapLoader = null)
        {
            var list = images.ToList();
            if (list.Count == 0)
            {
                _snowfallMaps = null;
                _snowfallMapPaths = null;
                return;
            }
            _snowfallMaps = new Image?[list.Count];
            if (mapLoader is null)
            {
                _snowfallMapPaths = null;
            }
            else
            {
                _snowfallMapPaths = new string?[list.Count];
            }
            for (var i = 0; i < list.Count; i++)
            {
                if (mapLoader is null)
                {
                    _snowfallMaps[i] = list[i];
                }
                else
                {
                    var path = await mapLoader.SaveAsync(list[i], Id, $"snowfall_{i}").ConfigureAwait(false);
                    if (!string.IsNullOrEmpty(path))
                    {
                        _snowfallMaps[i] = list[i];
                        _snowfallMapPaths![i] = path;
                    }
                }
            }
        }

        /// <summary>
        /// Stores an image as the temperature map for this planet, applying the same map to both
        /// summer and winter.
        /// </summary>
        /// <param name="image">The image to load.</param>
        /// <param name="mapLoader">
        /// <para>
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used to store the
        /// image.
        /// </para>
        /// <para>
        /// If <see langword="null"/> the image will not be stored, and will only be available while
        /// this region persists in memory.
        /// </para>
        /// </param>
        public async Task AssignTemperatureMapAsync(Image? image, ISurfaceMapLoader? mapLoader = null)
        {
            if (image is null)
            {
                _temperatureMapSummer = null;
                _temperatureMapSummerPath = null;
                _temperatureMapWinter = null;
                _temperatureMapWinterPath = null;
                return;
            }

            if (mapLoader is null)
            {
                _temperatureMapSummer = image;
                _temperatureMapSummerPath = null;
                _temperatureMapWinter = _temperatureMapSummer;
                _temperatureMapWinterPath = null;
            }
            else
            {
                _temperatureMapSummerPath = await mapLoader.SaveAsync(image, Id, "temperature_summer").ConfigureAwait(false);
                if (!string.IsNullOrEmpty(_temperatureMapSummerPath))
                {
                    _temperatureMapSummer = image;
                    _temperatureMapWinter = _temperatureMapSummer;
                    _temperatureMapWinterPath = _temperatureMapSummerPath;
                }
            }
        }

        /// <summary>
        /// Stores an image as the temperature map for this planet at the summer solstice.
        /// </summary>
        /// <param name="image">The image to load.</param>
        /// <param name="mapLoader">
        /// <para>
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used to store the
        /// image.
        /// </para>
        /// <para>
        /// If <see langword="null"/> the image will not be stored, and will only be available while
        /// this region persists in memory.
        /// </para>
        /// </param>
        public async Task AssignTemperatureMapSummerAsync(Image? image, ISurfaceMapLoader? mapLoader = null)
        {
            if (image is null)
            {
                _temperatureMapSummer = null;
                _temperatureMapSummerPath = null;
                return;
            }

            if (mapLoader is null)
            {
                _temperatureMapSummer = image;
                _temperatureMapSummerPath = null;
            }
            else
            {
                _temperatureMapSummerPath = await mapLoader.SaveAsync(image, Id, "temperature_summer").ConfigureAwait(false);
                if (!string.IsNullOrEmpty(_temperatureMapSummerPath))
                {
                    _temperatureMapSummer = image;
                }
            }
        }

        /// <summary>
        /// Stores an image as the temperature map for this planet at the winter solstice.
        /// </summary>
        /// <param name="image">The image to load.</param>
        /// <param name="mapLoader">
        /// <para>
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used to store the
        /// image.
        /// </para>
        /// <para>
        /// If <see langword="null"/> the image will not be stored, and will only be available while
        /// this region persists in memory.
        /// </para>
        /// </param>
        public async Task AssignTemperatureMapWinterAsync(Image? image, ISurfaceMapLoader? mapLoader = null)
        {
            if (image is null)
            {
                _temperatureMapWinter = null;
                _temperatureMapWinterPath = null;
                return;
            }

            if (mapLoader is null)
            {
                _temperatureMapWinter = image;
                _temperatureMapWinterPath = null;
            }
            else
            {
                _temperatureMapWinterPath = await mapLoader.SaveAsync(image, Id, "temperature_winter").ConfigureAwait(false);
                if (!string.IsNullOrEmpty(_temperatureMapWinterPath))
                {
                    _temperatureMapWinter = image;
                }
            }
        }

        /// <summary>
        /// Clears the elevation map for this planet, and removes it from storage.
        /// </summary>
        /// <param name="mapLoader">
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used to remove the
        /// image.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the image was removed; or <see langword="false"/> if the
        /// operation fails.
        /// </returns>
        public async Task<bool> ClearElevationMapAsync(ISurfaceMapLoader mapLoader)
        {
            if (string.IsNullOrEmpty(_elevationMapPath))
            {
                return true;
            }
            var success = await mapLoader.RemoveAsync(_elevationMapPath).ConfigureAwait(false);
            if (success)
            {
                _elevationMapPath = null;
            }
            return success;
        }

        /// <summary>
        /// Clears all this planet's map images, and removes them from storage.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> if the images were all removed; or <see langword="false"/> if the
        /// operation fails for any iamge.
        /// </returns>
        public async Task<bool> ClearMapsAsync(ISurfaceMapLoader mapLoader)
        {
            var elevationSuccess = ClearElevationMapAsync(mapLoader);
            var precipitationSuccess = ClearPrecipitationMapsAsync(mapLoader);
            var snowfallSuccess = ClearSnowfallMapsAsync(mapLoader);
            var temperatureSuccess = ClearTemperatureMapAsync(mapLoader);
            var successes = await Task.WhenAll(elevationSuccess, precipitationSuccess, snowfallSuccess, temperatureSuccess)
                .ConfigureAwait(false);
            return successes.All(x => x);
        }

        /// <summary>
        /// Clears the set of precipitation map images for this planet, and removes them from
        /// storage.
        /// </summary>
        /// <param name="mapLoader">
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used to retrieve the
        /// image.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the images were removed; or <see langword="false"/> if the
        /// operation fails for any image.
        /// </returns>
        public async Task<bool> ClearPrecipitationMapsAsync(ISurfaceMapLoader mapLoader)
        {
            if (_precipitationMapPaths is null)
            {
                return true;
            }
            var success = true;
            for (var i = 0; i < _precipitationMapPaths.Length; i++)
            {
                if (string.IsNullOrEmpty(_precipitationMapPaths[i]))
                {
                    continue;
                }
                var mapSuccess = await mapLoader.RemoveAsync(_precipitationMapPaths[i]).ConfigureAwait(false);
                if (mapSuccess)
                {
                    _precipitationMapPaths[i] = null;
                }
                success &= mapSuccess;
            }
            if (success)
            {
                _precipitationMapPaths = null;
            }
            return success;
        }

        /// <summary>
        /// Clears the set of images as the snowfall maps for this planet, and removes them from
        /// storage.
        /// </summary>
        /// <param name="mapLoader">
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used to retrieve the
        /// image.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the images were removed; or <see langword="false"/> if the
        /// operation fails for any image.
        /// </returns>
        public async Task<bool> ClearSnowfallMapsAsync(ISurfaceMapLoader mapLoader)
        {
            if (_snowfallMapPaths is null)
            {
                return true;
            }
            var success = true;
            for (var i = 0; i < _snowfallMapPaths.Length; i++)
            {
                if (string.IsNullOrEmpty(_snowfallMapPaths[i]))
                {
                    continue;
                }
                var mapSuccess = await mapLoader.RemoveAsync(_snowfallMapPaths[i]).ConfigureAwait(false);
                if (mapSuccess)
                {
                    _snowfallMapPaths[i] = null;
                }
                success &= mapSuccess;
            }
            if (success)
            {
                _snowfallMapPaths = null;
            }
            return success;
        }

        /// <summary>
        /// Clears the temperature map(s) for this planet, and removes them from storage.
        /// </summary>
        /// <param name="mapLoader">
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used to retrieve the
        /// image.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the image(s) were removed; or <see langword="false"/> if the
        /// operation fails for any image.
        /// </returns>
        public async Task<bool> ClearTemperatureMapAsync(ISurfaceMapLoader mapLoader)
        {
            var success = true;
            if (!string.IsNullOrEmpty(_temperatureMapSummerPath))
            {
                success = await mapLoader.RemoveAsync(_temperatureMapSummerPath).ConfigureAwait(false);
                if (success)
                {
                    _temperatureMapSummerPath = null;
                }
            }

            if (!string.IsNullOrEmpty(_temperatureMapWinterPath))
            {
                var mapSuccess = await mapLoader.RemoveAsync(_temperatureMapWinterPath).ConfigureAwait(false);
                if (mapSuccess)
                {
                    _temperatureMapWinterPath = null;
                }
                success &= mapSuccess;
            }
            return success;
        }

        /// <summary>
        /// <para>
        /// Removes this location and all contained children, as well as all satellites, from the
        /// given data store.
        /// </para>
        /// <para>
        /// Note: for planets, it may be necessary to call <see
        /// cref="ClearMapsAsync(ISurfaceMapLoader)"/> prior to deletion, in order to ensure that
        /// any stored maps are also removed.
        /// </para>
        /// </summary>
        public override async Task<bool> DeleteAsync(IDataStore dataStore)
        {
            var childrenSuccess = true;
            await foreach (var child in GetSatellitesAsync(dataStore))
            {
                childrenSuccess &= await child.DeleteAsync(dataStore).ConfigureAwait(false);
            }
            return childrenSuccess && await base.DeleteAsync(dataStore).ConfigureAwait(false);
        }

        /// <summary>
        /// Calculates the atmospheric density for the given conditions, in kg/m³.
        /// </summary>
        /// <param name="moment">The time at which to make the calculation.</param>
        /// <param name="latitude">The latitude of the object.</param>
        /// <param name="longitude">The longitude of the object.</param>
        /// <param name="altitude">The altitude of the object.</param>
        /// <param name="surface">
        /// If <see langword="true"/> the determination is made for a location
        /// on the surface of the planetoid at the given elevation. Otherwise, the calculation is
        /// made for an elevation above the surface.
        /// </param>
        /// <param name="mapLoader">
        /// <para>
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used to load any stored
        /// map image.
        /// </para>
        /// <para>
        /// If <see langword="null"/> no stored map will be used. Even if one exists, a random map
        /// will be generated and kept in memory only.
        /// </para>
        /// </param>
        /// <returns>The atmospheric density for the given conditions, in kg/m³.</returns>
        public async Task<double> GetAtmosphericDensityAsync(
            Instant moment,
            double latitude,
            double longitude,
            double altitude,
            bool surface = true,
            ISurfaceMapLoader? mapLoader = null)
        {
            var surfaceTemp = await GetSurfaceTemperatureAsync(moment, latitude, longitude, mapLoader)
                .ConfigureAwait(false);
            var tempAtElevation = GetTemperatureAtElevation(surfaceTemp, altitude, surface);
            return Atmosphere.GetAtmosphericDensity(this, tempAtElevation, altitude);
        }

        /// <summary>
        /// Calculates the atmospheric drag on a spherical object within the <see
        /// cref="Atmosphere"/> of this <see cref="Planetoid"/> under given conditions, in N.
        /// </summary>
        /// <param name="moment">The time at which to make the calculation.</param>
        /// <param name="latitude">The latitude of the object.</param>
        /// <param name="longitude">The longitude of the object.</param>
        /// <param name="altitude">The altitude of the object.</param>
        /// <param name="speed">The speed of the object, in m/s.</param>
        /// <param name="surface">
        /// If <see langword="true"/> the determination is made for a location
        /// on the surface of the planetoid at the given elevation. Otherwise, the calculation is
        /// made for an elevation above the surface.
        /// </param>
        /// <param name="mapLoader">
        /// <para>
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used to load any stored
        /// map image.
        /// </para>
        /// <para>
        /// If <see langword="null"/> no stored map will be used. Even if one exists, a random map
        /// will be generated and kept in memory only.
        /// </para>
        /// </param>
        /// <returns>The atmospheric drag on the object at the specified height, in N.</returns>
        /// <remarks>
        /// 0.47 is an arbitrary drag coefficient (that of a sphere in a fluid with a Reynolds
        /// number of 10⁴), which may not reflect the actual conditions at all, but the inaccuracy
        /// is accepted since the level of detailed information needed to calculate this value
        /// accurately is not desired in this library.
        /// </remarks>
        public async Task<double> GetAtmosphericDragAsync(
            Instant moment,
            double latitude,
            double longitude,
            double altitude,
            double speed,
            bool surface = true,
            ISurfaceMapLoader? mapLoader = null)
        {
            var surfaceTemp = await GetSurfaceTemperatureAsync(moment, latitude, longitude, mapLoader)
                .ConfigureAwait(false);
            var tempAtElevation = GetTemperatureAtElevation(surfaceTemp, altitude, surface);
            return Atmosphere.GetAtmosphericDrag(this, tempAtElevation, altitude, speed);
        }

        /// <summary>
        /// Calculates the atmospheric pressure at a given <paramref name="latitude"/> and <paramref
        /// name="longitude"/>, at the given true anomaly of the planet's orbit, in kPa.
        /// </summary>
        /// <param name="moment">The time at which to make the calculation.</param>
        /// <param name="latitude">The latitude at which to determine atmospheric pressure.</param>
        /// <param name="longitude">The longitude at which to determine atmospheric
        /// pressure.</param>
        /// <param name="surface">
        /// If <see langword="true"/> the determination is made for a location
        /// on the surface of the planetoid at the given elevation. Otherwise, the calculation is
        /// made for an elevation above the surface.
        /// </param>
        /// <param name="mapLoader">
        /// <para>
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used to load any stored
        /// map image.
        /// </para>
        /// <para>
        /// If <see langword="null"/> no stored map will be used. Even if one exists, a random map
        /// will be generated and kept in memory only.
        /// </para>
        /// </param>
        /// <returns>The atmospheric pressure at the specified height, in kPa.</returns>
        /// <remarks>
        /// In an Earth-like atmosphere, the pressure lapse rate varies considerably in the
        /// different atmospheric layers, but this cannot be easily modeled for arbitrary
        /// exoplanetary atmospheres, so the simple barometric formula is used, which should be
        /// "close enough" for the purposes of this library. Also, this calculation uses the molar
        /// mass of air on Earth, which is clearly not correct for other atmospheres, but is
        /// considered "close enough" for the purposes of this library.
        /// </remarks>
        public async Task<double> GetAtmosphericPressureAsync(
            Instant moment,
            double latitude,
            double longitude,
            bool surface = true,
            ISurfaceMapLoader? mapLoader = null)
        {
            var elevation = await GetElevationAtAsync(latitude, longitude, mapLoader).ConfigureAwait(false);
            var surfaceTemp = await GetSurfaceTemperatureAsync(moment, latitude, longitude, mapLoader)
                .ConfigureAwait(false);
            var tempAtElevation = GetTemperatureAtElevation(surfaceTemp, elevation, surface);
            return GetAtmosphericPressureFromTempAndElevation(tempAtElevation, elevation);
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
        public double GetDistance(Vector3 position1, Vector3 position2)
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
        public double GetDistance(MathAndScience.Numerics.Doubles.Vector3 position1, MathAndScience.Numerics.Doubles.Vector3 position2)
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
        /// Gets the elevation at the given <paramref name="latitude"/> and <paramref
        /// name="longitude"/>, in meters.
        /// </summary>
        /// <param name="latitude">The latitude at which to determine elevation.</param>
        /// <param name="longitude">The longitude at which to determine elevation.</param>
        /// <param name="mapLoader">
        /// <para>
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used to load any stored
        /// map image.
        /// </para>
        /// <para>
        /// If <see langword="null"/> no stored map will be used. Even if one exists, a random map
        /// will be generated and kept in memory only.
        /// </para>
        /// </param>
        /// <returns>
        /// The elevation at the given <paramref name="latitude"/> and <paramref name="longitude"/>,
        /// in meters.
        /// </returns>
        public async Task<double> GetElevationAtAsync(double latitude, double longitude, ISurfaceMapLoader? mapLoader = null)
        {
            using var map = await GetElevationMapAsync(mapLoader)
                .ConfigureAwait(false);
            return map.GetElevation(this, latitude, longitude, MapProjectionOptions.Default);
        }

        /// <summary>
        /// Gets the elevation at the given <paramref name="position"/>, in meters.
        /// </summary>
        /// <param name="position">The longitude at which to determine elevation.</param>
        /// <param name="mapLoader">
        /// <para>
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used to load any stored
        /// map image.
        /// </para>
        /// <para>
        /// If <see langword="null"/> no stored map will be used. Even if one exists, a random map
        /// will be generated and kept in memory only.
        /// </para>
        /// </param>
        /// <returns>
        /// The elevation at the given <paramref name="position"/>, in meters.
        /// </returns>
        public Task<double> GetElevationAtAsync(Vector3 position, ISurfaceMapLoader? mapLoader = null)
            => GetElevationAtAsync(VectorToLatitude(position), VectorToLongitude(position), mapLoader);

        /// <summary>
        /// Gets the stored elevation map image for this planet, if any.
        /// </summary>
        /// <returns>The stored elevation map image for this planet, if any.</returns>
        public Image? GetElevationMap() => _elevationMap;

        /// <summary>
        /// Gets or generates an elevation map image for this planet.
        /// </summary>
        /// <param name="mapLoader">
        /// <para>
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used.
        /// </para>
        /// <para>
        /// If <see langword="null"/> no stored map will be used, and any generated map will not be
        /// saved.
        /// </para>
        /// </param>
        /// <returns>An elevation map image for this planet.</returns>
        /// <remarks>
        /// If a map exists, it will be returned at its native resolution. If one does not already
        /// exist, a new one will be generated at a default resolution.
        /// </remarks>
        public async Task<Image<L16>> GetElevationMapAsync(ISurfaceMapLoader? mapLoader = null)
        {
            if (_elevationMap is null && HasElevationMap && mapLoader is not null)
            {
                await LoadElevationMapAsync(mapLoader).ConfigureAwait(false);
            }
            if (_elevationMap is null)
            {
                _elevationMap = SurfaceMapImage.GenerateElevationMap(this, DefaultMapResolution);
                if (mapLoader is not null)
                {
                    await AssignElevationMapAsync(_elevationMap, mapLoader).ConfigureAwait(false);
                }
            }
            return _elevationMap.CloneAs<L16>();
        }

        /// <summary>
        /// Produces an elevation map projection.
        /// </summary>
        /// <param name="resolution">The vertical resolution of the projection.</param>
        /// <param name="options">
        /// <para>
        /// The map projection options used.
        /// </para>
        /// <para>
        /// If left <see langword="null"/> an equirectangular projection of the full globe is
        /// produced.
        /// </para>
        /// </param>
        /// <param name="mapLoader">
        /// <para>
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used.
        /// </para>
        /// <para>
        /// If <see langword="null"/> no stored map will be used, and any generated map will not be
        /// saved.
        /// </para>
        /// </param>
        /// <returns>
        /// A projected map of elevation. Pixel luminosity indicates elevation relative to <see
        /// cref="MaxElevation"/>, with values below the midpoint indicating elevations below the
        /// mean surface.
        /// </returns>
        public async Task<Image<L16>> GetElevationMapProjectionAsync(
            int resolution,
            MapProjectionOptions? options = null,
            ISurfaceMapLoader? mapLoader = null)
        {
            using var map = await GetElevationMapAsync(mapLoader).ConfigureAwait(false);
            return SurfaceMapImage.GetMapImage(
                map,
                resolution,
                options);
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
        /// This method modifies total illumination based on an angle on incidence calculated from
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
            var system = await GetStarSystemAsync(dataStore).ConfigureAwait(false);
            if (system is null)
            {
                return 0;
            }

            var pos = GetPositionAtTime(moment);

            var stars = new List<(Star star, Vector3 position, Number distance, double eclipticLongitude)>();
            await foreach (var star in system.GetStarsAsync(dataStore))
            {
                var starPosition = star.GetPositionAtTime(moment);
                var (_, eclipticLongitude) = GetEclipticLatLon(pos, starPosition);
                stars.Add((
                    star,
                    starPosition,
                    Vector3.Distance(pos, starPosition),
                    eclipticLongitude));
            }
            if (stars.Count == 0)
            {
                return 0;
            }
            var lux = 0.0;

            foreach (var (star, starPosition, _, _) in stars)
            {
                var (solarRightAscension, solarDeclination) = GetRightAscensionAndDeclination(pos, starPosition);
                var longitudeOffset = longitude - solarRightAscension;
                if (longitudeOffset > Math.PI)
                {
                    longitudeOffset -= MathAndScience.Constants.Doubles.MathConstants.TwoPI;
                }

                var sinSolarElevation = (Math.Sin(solarDeclination) * Math.Sin(latitude))
                    + (Math.Cos(solarDeclination) * Math.Cos(latitude) * Math.Cos(longitudeOffset));
                var solarElevation = Math.Asin(sinSolarElevation);
                lux += solarElevation <= 0 ? 0 : GetLuminousFlux(stars.Select(x => x.star)) * sinSolarElevation;
            }

            await foreach (var satellite in GetSatellitesAsync(dataStore))
            {
                var satPos = satellite.GetPositionAtTime(moment);
                var satDist2 = Vector3.DistanceSquared(pos, satPos);
                var satDist = satDist2.Sqrt();

                var (satLat, satLon) = GetEclipticLatLon(pos, satPos);

                var phase = 0.0;
                foreach (var (star, starPosition, starDistance, eclipticLongitude) in stars)
                {
                    // satellite-centered elongation of the planet from the star (ratio of illuminated
                    // surface area to total surface area)
                    var le = Math.Acos(Math.Cos(satLat) * Math.Cos(eclipticLongitude - satLon));
                    var e = Math.Atan2((double)(satDist - (starDistance * Math.Cos(le))), (double)(starDistance * Math.Sin(le)));
                    // fraction of illuminated surface area
                    phase = Math.Max(phase, (1 + Math.Cos(e)) / 2);
                }

                // Total light from the satellite is the flux incident on the satellite, reduced
                // according to the proportion lit (vs. shadowed), further reduced according to the
                // albedo, then the distance the light must travel after being reflected.
                lux += satellite.GetLuminousFlux(stars.Select(x => x.star))
                    * phase
                    * satellite.Albedo
                    / MathAndScience.Constants.Doubles.MathConstants.FourPI
                    / (double)satDist2;
            }

            return lux;
        }

        /// <summary>
        /// Given an initial position, a bearing, and a distance, calculates the final position
        /// along the great circle arc described by the resulting motion.
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
        /// Great circle arcs are the shortest distance between two points on a sphere. Traveling
        /// along a great circle that is not the equator or a meridian requires continually changing
        /// one's compass heading during travel (unlike a rhumb line, which is not the shortest
        /// path, but requires no bearing adjustements).
        /// </para>
        /// <seealso cref="GetLatLonAtDistanceOnRhumbLine(double, double, Number, double)"/>
        /// </remarks>
        public (double latitude, double longitude) GetLatLonAtDistanceOnGreatCircleArc(double latitude, double longitude, Number distance, double bearing)
        {
            var angularDistance = (double)(distance / Shape.ContainingRadius);
            var sinDist = Math.Sin(angularDistance);
            var cosDist = Math.Cos(angularDistance);
            var sinLat = Math.Sin(latitude);
            var cosLat = Math.Cos(latitude);
            var finalLatitude = Math.Asin((sinLat * cosDist) + (cosLat * sinDist * Math.Cos(bearing)));
            var finalLongitude = longitude + Math.Atan2(Math.Sin(bearing) * sinDist * cosLat, cosDist - (sinLat * Math.Sin(finalLatitude)));
            finalLongitude = ((finalLongitude + MathAndScience.Constants.Doubles.MathConstants.ThreeHalvesPI) % MathAndScience.Constants.Doubles.MathConstants.TwoPI) - MathAndScience.Constants.Doubles.MathConstants.HalfPI;
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
        /// <seealso cref="GetLatLonAtDistanceOnGreatCircleArc(double, double, Number, double)"/>
        /// </remarks>
        public (double latitude, double longitude) GetLatLonAtDistanceOnRhumbLine(double latitude, double longitude, Number distance, double bearing)
        {
            var angularDistance = (double)(distance / Shape.ContainingRadius);
            var deltaLatitude = angularDistance + Math.Cos(angularDistance);
            var finalLatitude = latitude + deltaLatitude;
            var deltaProjectedLatitude = Math.Log(Math.Tan(MathAndScience.Constants.Doubles.MathConstants.QuarterPI + (finalLatitude / 2)) / Math.Tan(MathAndScience.Constants.Doubles.MathConstants.QuarterPI + (latitude / 2)));
            var q = Math.Abs(deltaProjectedLatitude) > new Number(10, -12) ? deltaLatitude / deltaProjectedLatitude : Math.Cos(latitude);
            var deltaLongitude = angularDistance * Math.Sin(bearing) / q;
            var finalLongitude = longitude + deltaLongitude;
            if (Math.Abs(finalLatitude) > MathAndScience.Constants.Doubles.MathConstants.HalfPI)
            {
                finalLatitude = finalLatitude > 0 ? Math.PI - finalLatitude : -Math.PI - finalLatitude;
            }
            finalLongitude = ((finalLongitude + MathAndScience.Constants.Doubles.MathConstants.ThreeHalvesPI) % MathAndScience.Constants.Doubles.MathConstants.TwoPI) - MathAndScience.Constants.Doubles.MathConstants.HalfPI;
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
        /// If this body is in a star system with multiple stars, the sunrise and sunset given will
        /// be for the star closest to the position it orbits. If it is not in orbit, the closest
        /// star is chosen.
        /// </remarks>
        public async Task<(RelativeDuration? sunrise, RelativeDuration? sunset)> GetLocalSunriseAndSunsetAsync(IDataStore dataStore, Instant moment, double latitude)
        {
            var primaryStar = await GetPrimaryStarAsync(dataStore).ConfigureAwait(false);
            if (primaryStar is null)
            {
                return (null, null);
            }

            var position = GetPositionAtTime(moment);
            var starPosition = primaryStar.GetPositionAtTime(moment);

            var (_, solarDeclination) = GetRightAscensionAndDeclination(position, starPosition);

            var d = Math.Cos(solarDeclination) * Math.Cos(latitude);
            if (d.IsNearlyZero())
            {
                return (solarDeclination < 0) == latitude.IsNearlyZero()
                    ? ((RelativeDuration?)RelativeDuration.FromProportionOfDay(Number.Zero), (RelativeDuration?)null)
                    : ((RelativeDuration?)null, RelativeDuration.FromProportionOfDay(Number.Zero));
            }

            var localSecondsFromSolarNoonAtSunriseAndSet = Math.Acos(-Math.Sin(solarDeclination) * Math.Sin(latitude) / d) / AngularVelocity;
            var localSecondsSinceMidnightAtSunrise = ((RotationalPeriod / 2) - localSecondsFromSolarNoonAtSunriseAndSet) % RotationalPeriod;
            var localSecondsSinceMidnightAtSunset = (localSecondsFromSolarNoonAtSunriseAndSet + (RotationalPeriod / 2)) % RotationalPeriod;
            return (RelativeDuration.FromProportionOfDay(localSecondsSinceMidnightAtSunrise / RotationalPeriod),
                RelativeDuration.FromProportionOfDay(localSecondsSinceMidnightAtSunset / RotationalPeriod));
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
        /// If this body is in a star system with multiple stars, the time of day given will be
        /// based on the star closest to the position it orbits. If it is not in orbit, the closest
        /// star is chosen. If there are no stars, or the body is not in a star system, the time of
        /// day is always midnight (relative duration of zero).
        /// </remarks>
        public async Task<RelativeDuration> GetLocalTimeOfDayAsync(IDataStore dataStore, Instant moment, double longitude)
        {
            var primaryStar = await GetPrimaryStarAsync(dataStore).ConfigureAwait(false);
            if (primaryStar is null)
            {
                return RelativeDuration.Zero;
            }

            var position = GetPositionAtTime(moment);
            var starPosition = primaryStar.GetPositionAtTime(moment);

            var (solarRightAscension, _) = GetRightAscensionAndDeclination(position, starPosition);
            var longitudeOffset = longitude - solarRightAscension;
            if (longitudeOffset > Math.PI)
            {
                longitudeOffset -= MathAndScience.Constants.Doubles.MathConstants.TwoPI;
            }
            var localSecondsSinceSolarNoon = longitudeOffset / AngularVelocity;

            var localSecondsSinceMidnight = (localSecondsSinceSolarNoon + (RotationalPeriod / 2)) % RotationalPeriod;
            return RelativeDuration.FromProportionOfDay(localSecondsSinceMidnight / RotationalPeriod);
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
        public Number GetLocalTimeOffset(double longitude)
            => (longitude > Math.PI ? longitude - MathAndScience.Constants.Doubles.MathConstants.TwoPI : longitude) * RotationalPeriod / MathConstants.TwoPI;

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
            var system = await GetStarSystemAsync(dataStore).ConfigureAwait(false);
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
                _maxSurfaceTemperature = (_surfaceTemperatureAtPeriapsis * InsolationFactor_Equatorial) + greenhouseEffect;
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
                _minSurfaceTemperature = (_surfaceTemperatureAtApoapsis * InsolationFactor_Polar) + greenhouseEffect - variation;
            }
            return _minSurfaceTemperature.Value;
        }

        /// <summary>Populates a <see cref="SerializationInfo"></see> with the data needed to
        /// serialize the target object.</summary>
        /// <param name="info">The <see cref="SerializationInfo"></see> to populate with
        /// data.</param>
        /// <param name="context">The destination (see <see cref="StreamingContext"></see>) for this
        /// serialization.</param>
        /// <exception cref="System.Security.SecurityException">The caller does not have the
        /// required permission.</exception>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(Id), Id);
            info.AddValue(nameof(Seed), Seed);
            info.AddValue(nameof(PlanetType), PlanetType);
            info.AddValue(nameof(ParentId), ParentId);
            info.AddValue(nameof(AbsolutePosition), AbsolutePosition);
            info.AddValue(nameof(Name), Name);
            info.AddValue(nameof(Velocity), Velocity);
            info.AddValue(nameof(Orbit), Orbit);
            info.AddValue(nameof(Position), Position);
            info.AddValue(nameof(Temperature), Material.Temperature);
            info.AddValue(nameof(AngleOfRotation), AngleOfRotation);
            info.AddValue(nameof(RotationalPeriod), RotationalPeriod);
            info.AddValue(nameof(_satelliteIDs), _satelliteIDs);
            info.AddValue(nameof(Rings), _rings);
            info.AddValue(nameof(_blackbodyTemperature), _blackbodyTemperature);
            info.AddValue(nameof(_surfaceTemperatureAtApoapsis), _surfaceTemperatureAtApoapsis);
            info.AddValue(nameof(_surfaceTemperatureAtPeriapsis), _surfaceTemperatureAtPeriapsis);
            info.AddValue(nameof(IsInhospitable), IsInhospitable);
            info.AddValue(nameof(_earthlike), _earthlike);
            if (_earthlike)
            {
                info.AddValue(nameof(_planetParams), null);
                info.AddValue(nameof(_habitabilityRequirements), null);
            }
            else
            {
                info.AddValue(nameof(_planetParams), _planetParams);
                info.AddValue(nameof(_habitabilityRequirements), _habitabilityRequirements);
            }
            info.AddValue(nameof(_elevationMapPath), _elevationMapPath);
            info.AddValue(nameof(_precipitationMapPaths), _precipitationMapPaths);
            info.AddValue(nameof(_snowfallMapPaths), _snowfallMapPaths);
            info.AddValue(nameof(_temperatureMapSummerPath), _temperatureMapSummerPath);
            info.AddValue(nameof(_temperatureMapWinterPath), _temperatureMapWinterPath);
        }

        /// <summary>
        /// Gets the precipitation at the given <paramref name="latitude"/> and <paramref
        /// name="longitude"/>, at the given time, in mm/hr.
        /// </summary>
        /// <param name="moment">
        /// The moment at which precipitation is to be determined.
        /// </param>
        /// <param name="latitude">The latitude at which to determine precipitation.</param>
        /// <param name="longitude">The longitude at which to determine precipitation.</param>
        /// <param name="mapLoader">
        /// <para>
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used to load any stored
        /// map images.
        /// </para>
        /// <para>
        /// If <see langword="null"/> no stored maps will be used. Even if one exists, random maps
        /// will be generated and kept in memory only.
        /// </para>
        /// </param>
        /// <returns>
        /// The precipitation at the given <paramref name="latitude"/> and <paramref
        /// name="longitude"/>, in mm/hr.
        /// </returns>
        public async Task<double> GetPrecipitationAtAsync(
            Instant moment,
            double latitude,
            double longitude,
            ISurfaceMapLoader? mapLoader = null)
        {
            using var map = await GetPrecipitationMapAsync(
                GetProportionOfYearAtTime(moment),
                DefaultSeasons,
                mapLoader)
                .ConfigureAwait(false);
            return map.GetPrecipitation(this, latitude, longitude, MapProjectionOptions.Default);
        }

        /// <summary>
        /// Gets the precipitation at the given <paramref name="position"/>, at the given time, in
        /// mm/hr.
        /// </summary>
        /// <param name="moment">
        /// The moment at which precipitation is to be determined.
        /// </param>
        /// <param name="position">
        /// The position at which to determine precipitation.
        /// </param>
        /// <param name="mapLoader">
        /// <para>
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used to load any stored
        /// map images.
        /// </para>
        /// <para>
        /// If <see langword="null"/> no stored maps will be used. Even if one exists, random maps
        /// will be generated and kept in memory only.
        /// </para>
        /// </param>
        /// <returns>
        /// The precipitation at the given <paramref name="position"/>, in mm/hr.
        /// </returns>
        public Task<double> GetPrecipitationAtAsync(
            Instant moment,
            Vector3 position,
            ISurfaceMapLoader? mapLoader = null)
        {
            var latitude = VectorToLatitude(position);
            var longitude = VectorToLongitude(position);
            return GetPrecipitationAtAsync(moment, latitude, longitude, mapLoader);
        }

        /// <summary>
        /// Gets or generates a precipitation map image for this planet.
        /// </summary>
        /// <param name="steps">
        /// <para>
        /// The number of maps to generate internally (representing evenly spaced "seasons" during a
        /// year, starting and ending at the winter solstice in the northern hemisphere), before
        /// averaging them into a single image.
        /// </para>
        /// <para>
        /// If stored maps exist, they will be used and this parameter will be ignored.
        /// </para>
        /// </param>
        /// <param name="mapLoader">
        /// <para>
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used.
        /// </para>
        /// <para>
        /// If <see langword="null"/> no stored maps will be used, and any generated maps will not
        /// be saved.
        /// </para>
        /// </param>
        /// <returns>A precipitation map image for this planet.</returns>
        /// <remarks>
        /// If maps exist, they will be returned at their native resolutions. If maps do not already
        /// exist, new ones will be generated at a default resolution.
        /// </remarks>
        public async Task<Image<L16>> GetPrecipitationMapAsync(int steps = 1, ISurfaceMapLoader? mapLoader = null)
        {
            if ((_precipitationMaps is null || _precipitationMaps.Length == 0) && HasPrecipitationMap && mapLoader is not null)
            {
                await LoadPrecipitationMapsAsync(mapLoader).ConfigureAwait(false);
            }
            if (_precipitationMaps is null
                || _precipitationMaps.Length == 0
                || _precipitationMaps.Any(x => x is null))
            {
                var winterMap = await GetTemperatureMapWinterAsync(mapLoader).ConfigureAwait(false);
                var summerMap = await GetTemperatureMapSummerAsync(mapLoader).ConfigureAwait(false);
                var (precipitationMaps, snowfallMaps) = SurfaceMapImage.GeneratePrecipitationMaps(this, winterMap, summerMap, DefaultMapResolution, steps);
                _precipitationMaps = precipitationMaps;
                if (mapLoader is not null)
                {
                    await AssignPrecipitationMapsAsync(precipitationMaps, mapLoader).ConfigureAwait(false);
                }
                if (_snowfallMaps is null && !HasSnowfallMap)
                {
                    _snowfallMaps = snowfallMaps;
                    if (mapLoader is not null)
                    {
                        await AssignSnowfallMapsAsync(snowfallMaps, mapLoader).ConfigureAwait(false);
                    }
                }
            }
            return SurfaceMapImage.AverageImages(_precipitationMaps!);
        }

        /// <summary>
        /// Produces a precipitation map projection.
        /// </summary>
        /// <param name="resolution">The vertical resolution of the projection.</param>
        /// <param name="steps">
        /// <para>
        /// The number of maps to generate internally (representing evenly spaced "seasons" during a year,
        /// starting and ending at the winter solstice in the northern hemisphere), before averaging
        /// them into a single image.
        /// </para>
        /// <para>
        /// If stored maps exist, they will be used and this parameter will be ignored.
        /// </para>
        /// </param>
        /// <param name="options">
        /// <para>
        /// The map projection options used.
        /// </para>
        /// <para>
        /// If left <see langword="null"/> an equirectangular projection of the full globe is
        /// produced.
        /// </para>
        /// </param>
        /// <param name="mapLoader">
        /// <para>
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used.
        /// </para>
        /// <para>
        /// If <see langword="null"/> no stored maps will be used, and any generated maps will not
        /// be saved.
        /// </para>
        /// </param>
        /// <returns>
        /// A projected map of precipitation. Pixel luminosity indicates precipitation in mm/hr,
        /// relative to the <see cref="Atmosphere.MaxPrecipitation"/> of this planet's <see
        /// cref="Atmosphere"/>.
        /// </returns>
        public async Task<Image<L16>> GetPrecipitationMapProjectionAsync(
            int resolution,
            int steps = 1,
            MapProjectionOptions? options = null,
            ISurfaceMapLoader? mapLoader = null)
        {
            using var map = await GetPrecipitationMapAsync(steps, mapLoader).ConfigureAwait(false);
            return SurfaceMapImage.GetMapImage(
                map,
                resolution,
                options);
        }

        /// <summary>
        /// Gets or generates a precipitation map image for this planet at the given proportion of a
        /// year.
        /// </summary>
        /// <param name="proportionOfYear">
        /// The proportion of a full year at which the map is to be generated, assuming a year
        /// begins and ends at the winter solstice in the northern hemisphere.
        /// </param>
        /// <param name="steps">
        /// <para>
        /// The number of maps to generate internally (representing evenly spaced "seasons" during a
        /// year, starting and ending at the winter solstice in the northern hemisphere), before
        /// interpolating them into a single image.
        /// </para>
        /// <para>
        /// If stored maps exist, they will be used and this parameter will be ignored.
        /// </para>
        /// </param>
        /// <param name="mapLoader">
        /// <para>
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used.
        /// </para>
        /// <para>
        /// If <see langword="null"/> no stored maps will be used, and any generated maps will not
        /// be saved.
        /// </para>
        /// </param>
        /// <returns>
        /// A precipitation map image for this planet at the given proportion of a year. Pixel
        /// luminosity indicates precipitation in mm/hr, relative to the <see
        /// cref="Atmosphere.MaxPrecipitation"/> of this planet's <see cref="Atmosphere"/>.
        /// </returns>
        /// <remarks>
        /// <para>
        /// If maps exist, they will be returned at their native resolutions. If maps do not already
        /// exist, new ones will be generated at a default resolution.
        /// </para>
        /// <para>
        /// Note: if you will be getting multiple images, it is more efficient to use the <see
        /// cref="GetPrecipitationMapsAsync(int, ISurfaceMapLoader?)"/> method to get the
        /// entire set at once.
        /// </para>
        /// </remarks>
        public async Task<Image<L16>> GetPrecipitationMapAsync(double proportionOfYear, int steps = 1, ISurfaceMapLoader? mapLoader = null)
        {
            if ((_precipitationMaps is null || _precipitationMaps.Length == 0) && HasPrecipitationMap && mapLoader is not null)
            {
                await LoadPrecipitationMapsAsync(mapLoader).ConfigureAwait(false);
            }
            if (_precipitationMaps is null
                || _precipitationMaps.Length == 0
                || _precipitationMaps.Any(x => x is null))
            {
                var winterMap = await GetTemperatureMapWinterAsync(mapLoader).ConfigureAwait(false);
                var summerMap = await GetTemperatureMapSummerAsync(mapLoader).ConfigureAwait(false);
                var (precipitationMaps, snowfallMaps) = SurfaceMapImage.GeneratePrecipitationMaps(this, winterMap, summerMap, DefaultMapResolution, steps);
                _precipitationMaps = precipitationMaps;
                if (mapLoader is not null)
                {
                    await AssignPrecipitationMapsAsync(precipitationMaps, mapLoader).ConfigureAwait(false);
                }
                if (_snowfallMaps is null && !HasSnowfallMap)
                {
                    _snowfallMaps = snowfallMaps;
                    if (mapLoader is not null)
                    {
                        await AssignSnowfallMapsAsync(snowfallMaps, mapLoader).ConfigureAwait(false);
                    }
                }
            }
            var proportionPerSeason = 1.0 / steps;
            var proportionPerMap = 1.0 / _precipitationMaps.Length;
            var season = (int)Math.Floor(proportionOfYear / proportionPerMap).Clamp(0, _precipitationMaps.Length - 1);
            var nextSeason = season == _precipitationMaps.Length - 1
                ? 0
                : season + 1;
            var weight = proportionOfYear % proportionPerMap;
            if (weight.IsNearlyZero())
            {
                return _precipitationMaps[season]!.CloneAs<L16>();
            }
            else
            {
                return SurfaceMapImage.InterpolateImages(
                    _precipitationMaps[season]!,
                    _precipitationMaps[nextSeason]!,
                    weight);
            }
        }

        /// <summary>
        /// Produces a precipitation map projection at the given proportion of a year.
        /// </summary>
        /// <param name="resolution">The vertical resolution of the projection.</param>
        /// <param name="proportionOfYear">
        /// The proportion of a full year at which the map is to be generated, assuming a year
        /// begins and ends at the winter solstice in the northern hemisphere.
        /// </param>
        /// <param name="steps">
        /// <para>
        /// The number of maps to generate internally (representing evenly spaced "seasons" during a
        /// year, starting and ending at the winter solstice in the northern hemisphere), before
        /// interpolating them into a single image.
        /// </para>
        /// <para>
        /// If stored maps exist, they will be used and this parameter will be ignored.
        /// </para>
        /// </param>
        /// <param name="options">
        /// <para>
        /// The map projection options used.
        /// </para>
        /// <para>
        /// If left <see langword="null"/> an equirectangular projection of the full globe is
        /// produced.
        /// </para>
        /// </param>
        /// <param name="mapLoader">
        /// <para>
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used.
        /// </para>
        /// <para>
        /// If <see langword="null"/> no stored maps will be used, and any generated maps will not
        /// be saved.
        /// </para>
        /// </param>
        /// <returns>
        /// A projected map of precipitation at the given proportion of a year. Pixel luminosity
        /// indicates precipitation in mm/hr, relative to the <see
        /// cref="Atmosphere.MaxPrecipitation"/> of this planet's <see cref="Atmosphere"/>.
        /// </returns>
        /// <remarks>
        /// Note: if you will be getting multiple images, it is more efficient to use the <see
        /// cref="GetPrecipitationMapsProjectionAsync(int, int, MapProjectionOptions?,
        /// ISurfaceMapLoader?)"/> method to get the entire set at once.
        /// </remarks>
        public async Task<Image<L16>> GetPrecipitationMapProjectionAsync(
            int resolution,
            double proportionOfYear,
            int steps = 1,
            MapProjectionOptions? options = null,
            ISurfaceMapLoader? mapLoader = null)
        {
            using var map = await GetPrecipitationMapAsync(proportionOfYear, steps, mapLoader).ConfigureAwait(false);
            return SurfaceMapImage.GetMapImage(
                map,
                resolution,
                options);
        }

        /// <summary>
        /// Gets the stored set of precipitation map images for this planet, if any.
        /// </summary>
        /// <returns>The stored set of precipitation map images for this planet, if any.</returns>
        public Image?[] GetPrecipitationMaps()
        {
            if (_precipitationMaps is null)
            {
                return Array.Empty<Image?>();
            }
            var maps = new Image?[_precipitationMaps.Length];
            for (var i = 0; i < _precipitationMaps.Length; i++)
            {
                maps[i] = _precipitationMaps[i];
            }
            return maps;
        }

        /// <summary>
        /// Gets or generates a set of precipitation map images for this planet.
        /// </summary>
        /// <param name="steps">
        /// <para>
        /// The number of maps to generate (representing evenly spaced "seasons" during a year,
        /// starting and ending at the winter solstice in the northern hemisphere).
        /// </para>
        /// <para>
        /// If stored maps exist but in a different number, they will be interpolated.
        /// </para>
        /// </param>
        /// <param name="mapLoader">
        /// <para>
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used.
        /// </para>
        /// <para>
        /// If <see langword="null"/> no stored maps will be used, and any generated maps will not
        /// be saved.
        /// </para>
        /// </param>
        /// <returns>
        /// A set of precipitation map images for this planet. Pixel luminosity indicates
        /// precipitation in mm/hr, relative to the <see cref="Atmosphere.MaxPrecipitation"/> of
        /// this planet's <see cref="Atmosphere"/>.
        /// </returns>
        /// <remarks>
        /// If maps exist, they will be returned at their native resolutions. If maps do not already
        /// exist, new ones will be generated at a default resolution.
        /// </remarks>
        public async Task<Image<L16>[]> GetPrecipitationMapsAsync(int steps, ISurfaceMapLoader? mapLoader = null)
        {
            if (steps == 1)
            {
                var map = await GetPrecipitationMapAsync(steps, mapLoader).ConfigureAwait(false);
                return new[] { map };
            }
            if ((_precipitationMaps is null || _precipitationMaps.Length == 0) && HasPrecipitationMap && mapLoader is not null)
            {
                await LoadPrecipitationMapsAsync(mapLoader).ConfigureAwait(false);
            }
            if (_precipitationMaps is null
                || _precipitationMaps.Length == 0
                || _precipitationMaps.Any(x => x is null))
            {
                var winterMap = await GetTemperatureMapWinterAsync(mapLoader).ConfigureAwait(false);
                var summerMap = await GetTemperatureMapSummerAsync(mapLoader).ConfigureAwait(false);
                var (precipitationMaps, snowfallMaps) = SurfaceMapImage.GeneratePrecipitationMaps(this, winterMap, summerMap, DefaultMapResolution, steps);
                _precipitationMaps = precipitationMaps;
                if (mapLoader is not null)
                {
                    await AssignPrecipitationMapsAsync(precipitationMaps, mapLoader).ConfigureAwait(false);
                }
                if (_snowfallMaps is null && !HasSnowfallMap)
                {
                    _snowfallMaps = snowfallMaps;
                    if (mapLoader is not null)
                    {
                        await AssignSnowfallMapsAsync(snowfallMaps, mapLoader).ConfigureAwait(false);
                    }
                }
            }
            var maps = new Image<L16>[steps];
            if (_precipitationMaps.Length == steps)
            {
                for (var i = 0; i < steps; i++)
                {
                    maps[i] = _precipitationMaps[i]!.CloneAs<L16>();
                }
                return maps;
            }
            var proportionOfYear = 0.0;
            var proportionPerSeason = 1.0 / steps;
            var proportionPerMap = 1.0 / _precipitationMaps.Length;
            for (var i = 0; i < steps; i++)
            {
                var season = (int)Math.Floor(proportionOfYear / proportionPerMap).Clamp(0, _precipitationMaps.Length - 1);
                var nextSeason = season == _precipitationMaps.Length - 1
                    ? 0
                    : season + 1;
                var weight = proportionOfYear % proportionPerMap;
                if (weight.IsNearlyZero())
                {
                    maps[i] = _precipitationMaps[season]!.CloneAs<L16>();
                }
                else
                {
                    maps[i] = SurfaceMapImage.InterpolateImages(
                        _precipitationMaps[season]!,
                        _precipitationMaps[nextSeason]!,
                        weight);
                }
                proportionOfYear += proportionPerSeason;
            }
            return maps;
        }

        /// <summary>
        /// Produces a set of precipitation map projections.
        /// </summary>
        /// <param name="resolution">The vertical resolution of the projection.</param>
        /// <param name="steps">
        /// <para>
        /// The number of maps to generate (representing evenly spaced "seasons" during a year,
        /// starting and ending at the winter solstice in the northern hemisphere).
        /// </para>
        /// <para>
        /// If stored maps exist but in a different number, they will be interpolated.
        /// </para>
        /// </param>
        /// <param name="options">
        /// <para>
        /// The map projection options used.
        /// </para>
        /// <para>
        /// If left <see langword="null"/> an equirectangular projection of the full globe is
        /// produced.
        /// </para>
        /// </param>
        /// <param name="mapLoader">
        /// <para>
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used.
        /// </para>
        /// <para>
        /// If <see langword="null"/> no stored maps will be used, and any generated maps will not
        /// be saved.
        /// </para>
        /// </param>
        /// <returns>
        /// A set of projected maps of precipitation. Pixel luminosity indicates precipitation in
        /// mm/hr, relative to the <see cref="Atmosphere.MaxPrecipitation"/> of this planet's <see
        /// cref="Atmosphere"/>.
        /// </returns>
        public async Task<Image<L16>[]> GetPrecipitationMapsProjectionAsync(
            int resolution,
            int steps,
            MapProjectionOptions? options = null,
            ISurfaceMapLoader? mapLoader = null)
        {
            var maps = await GetPrecipitationMapsAsync(steps, mapLoader).ConfigureAwait(false);
            var newMaps = new Image<L16>[maps.Length];
            for (var i = 0; i < steps; i++)
            {
                newMaps[i] = SurfaceMapImage.GetMapImage(
                    maps[i],
                    resolution,
                    options);
                maps[i].Dispose();
            }
            return newMaps;
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
            return (trueAnomaly - WinterSolsticeTrueAnomaly + MathAndScience.Constants.Doubles.MathConstants.TwoPI)
                % MathAndScience.Constants.Doubles.MathConstants.TwoPI
                / MathAndScience.Constants.Doubles.MathConstants.TwoPI;
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
        public Number GetResourceRichnessAt(ISubstanceReference substance, double latitude, double longitude)
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
        /// <param name="satellite">A natural satellite of this body. If the specified body is not
        /// one of this one's satellites, the return value will always be <code>(0.0, <see
        /// langword="false"/>)</code>.</param>
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
            if (_satelliteIDs?.Contains(satellite.Id) != true || !satellite.Orbit.HasValue)
            {
                return (0, false);
            }

            var system = await GetStarSystemAsync(dataStore).ConfigureAwait(false);
            if (system is null)
            {
                return (0, false);
            }

            var position = GetPositionAtTime(moment);

            var stars = new List<(Star star, Vector3 position, Number distance, double eclipticLongitude)>();
            await foreach (var star in system.GetStarsAsync(dataStore))
            {
                var starPosition = star.GetPositionAtTime(moment);
                var (_, eclipticLongitude) = GetEclipticLatLon(position, starPosition);
                stars.Add((
                    star,
                    starPosition,
                    Vector3.Distance(position, starPosition),
                    eclipticLongitude));
            }
            if (stars.Count == 0)
            {
                return (0, false);
            }

            var satellitePosition = satellite.GetPositionAtTime(moment);
            var phase = 0.0;
            foreach (var (star, starPosition, starDist, eclipticLongitude) in stars)
            {
                var satDist = Vector3.Distance(position, satellitePosition);
                var (satLat, satLon) = GetEclipticLatLon(position, satellitePosition);

                // satellite-centered elongation of the planet from the star (ratio of illuminated
                // surface area to total surface area)
                var le = Math.Acos(Math.Cos(satLat) * Math.Cos(eclipticLongitude - satLon));
                var e = Math.Atan2((double)(satDist - (starDist * Math.Cos(le))), (double)(starDist * Math.Sin(le)));
                // fraction of illuminated surface area
                phase = Math.Max(phase, (1 + Math.Cos(e)) / 2);
            }

            var waxing = false;
            if (stars.Count == 1)
            {
                var starPosition = stars[0].position;
                var (planetRightAscension, _) = satellite.GetRightAscensionAndDeclination(satellitePosition, position);
                var (starRightAscension, _) = satellite.GetRightAscensionAndDeclination(satellitePosition, starPosition);
                waxing = (starRightAscension - planetRightAscension + MathAndScience.Constants.Doubles.MathConstants.TwoPI) % MathAndScience.Constants.Doubles.MathConstants.TwoPI <= Math.PI;
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
            if (_satelliteIDs is null)
            {
                yield break;
            }
            foreach (var id in _satelliteIDs)
            {
                var satellite = await dataStore.GetItemAsync<Planetoid>(id).ConfigureAwait(false);
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
        /// Gets the snowfall at the given <paramref name="latitude"/> and <paramref
        /// name="longitude"/>, at the given time, in mm/hr.
        /// </summary>
        /// <param name="moment">
        /// The moment at which snowfall is to be determined.
        /// </param>
        /// <param name="latitude">The latitude at which to determine snowfall.</param>
        /// <param name="longitude">The longitude at which to determine snowfall.</param>
        /// <param name="mapLoader">
        /// <para>
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used to load any stored
        /// map images.
        /// </para>
        /// <para>
        /// If <see langword="null"/> no stored maps will be used. Even if one exists, random maps
        /// will be generated and kept in memory only.
        /// </para>
        /// </param>
        /// <returns>
        /// The snowfall at the given <paramref name="latitude"/> and <paramref name="longitude"/>,
        /// in mm/hr.
        /// </returns>
        public async Task<double> GetSnowfallAtAsync(
            Instant moment,
            double latitude,
            double longitude,
            ISurfaceMapLoader? mapLoader = null)
        {
            using var map = await GetSnowfallMapAsync(
                GetProportionOfYearAtTime(moment),
                DefaultSeasons,
                mapLoader)
                .ConfigureAwait(false);
            return map.GetSnowfall(this, latitude, longitude, MapProjectionOptions.Default);
        }

        /// <summary>
        /// Gets the snowfall at the given <paramref name="position"/>, at the given time, in mm/hr.
        /// </summary>
        /// <param name="moment">
        /// The moment at which snowfall is to be determined.
        /// </param>
        /// <param name="position">
        /// The position at which to determine snowfall.
        /// </param>
        /// <param name="mapLoader">
        /// <para>
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used to load any stored
        /// map images.
        /// </para>
        /// <para>
        /// If <see langword="null"/> no stored maps will be used. Even if one exists, random maps
        /// will be generated and kept in memory only.
        /// </para>
        /// </param>
        /// <returns>
        /// The snowfall at the given <paramref name="position"/>, in mm/hr.
        /// </returns>
        public Task<double> GetSnowfallAtAsync(
            Instant moment,
            Vector3 position,
            ISurfaceMapLoader? mapLoader = null)
        {
            var latitude = VectorToLatitude(position);
            var longitude = VectorToLongitude(position);
            return GetSnowfallAtAsync(moment, latitude, longitude, mapLoader);
        }

        /// <summary>
        /// Gets or generates a snowfall map image for this planet.
        /// </summary>
        /// <param name="steps">
        /// <para>
        /// The number of maps to generate internally (representing evenly spaced "seasons" during a
        /// year, starting and ending at the winter solstice in the northern hemisphere), before
        /// averaging them into a single image.
        /// </para>
        /// <para>
        /// If stored maps exist, they will be used and this parameter will be ignored.
        /// </para>
        /// </param>
        /// <param name="mapLoader">
        /// <para>
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used.
        /// </para>
        /// <para>
        /// If <see langword="null"/> no stored maps will be used, and any generated maps will not
        /// be saved.
        /// </para>
        /// </param>
        /// <returns>
        /// A snowfall map image for this planet. Pixel luminosity indicates snowfall in mm/hr,
        /// relative to the <see cref="Atmosphere.MaxSnowfall"/> of this planet's <see
        /// cref="Atmosphere"/>.
        /// </returns>
        /// <remarks>
        /// If maps exist, they will be returned at their native resolutions. If maps do not already
        /// exist, new ones will be generated at a default resolution.
        /// </remarks>
        public async Task<Image<L16>> GetSnowfallMapAsync(int steps = 1, ISurfaceMapLoader? mapLoader = null)
        {
            var maps = await GetSnowfallMapsAsync(steps, mapLoader).ConfigureAwait(false);
            var map = SurfaceMapImage.AverageImages(maps);
            for (var i = 0; i < maps.Length; i++)
            {
                maps[i].Dispose();
            }
            return map;
        }

        /// <summary>
        /// Produces a snowfall map projection.
        /// </summary>
        /// <param name="resolution">The vertical resolution of the projection.</param>
        /// <param name="steps">
        /// <para>
        /// The number of maps to generate internally (representing evenly spaced "seasons" during a year,
        /// starting and ending at the winter solstice in the northern hemisphere), before averaging
        /// them into a single image.
        /// </para>
        /// <para>
        /// If stored maps exist, they will be used and this parameter will be ignored.
        /// </para>
        /// </param>
        /// <param name="options">
        /// <para>
        /// The map projection options used.
        /// </para>
        /// <para>
        /// If left <see langword="null"/> an equirectangular projection of the full globe is
        /// produced.
        /// </para>
        /// </param>
        /// <param name="mapLoader">
        /// <para>
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used.
        /// </para>
        /// <para>
        /// If <see langword="null"/> no stored maps will be used, and any generated maps will not
        /// be saved.
        /// </para>
        /// </param>
        /// <returns>
        /// A projected map of snowfall. Pixel luminosity indicates precipitation in mm/hr, relative
        /// to the <see cref="Atmosphere.MaxSnowfall"/> of this planet's <see cref="Atmosphere"/>.
        /// </returns>
        public async Task<Image<L16>> GetSnowfallMapProjectionAsync(
            int resolution,
            int steps = 1,
            MapProjectionOptions? options = null,
            ISurfaceMapLoader? mapLoader = null)
        {
            using var map = await GetSnowfallMapAsync(steps, mapLoader).ConfigureAwait(false);
            return SurfaceMapImage.GetMapImage(
                map,
                resolution,
                options);
        }

        /// <summary>
        /// Gets or generates a snowfall map image for this planet at the given proportion of a
        /// year.
        /// </summary>
        /// <param name="proportionOfYear">
        /// The proportion of a full year at which the map is to be generated, assuming a year
        /// begins and ends at the winter solstice in the northern hemisphere.
        /// </param>
        /// <param name="steps">
        /// <para>
        /// The number of maps to generate internally (representing evenly spaced "seasons" during a
        /// year, starting and ending at the winter solstice in the northern hemisphere), before
        /// interpolating them into a single image.
        /// </para>
        /// <para>
        /// If stored maps exist, they will be used and this parameter will be ignored.
        /// </para>
        /// </param>
        /// <param name="mapLoader">
        /// <para>
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used.
        /// </para>
        /// <para>
        /// If <see langword="null"/> no stored maps will be used, and any generated maps will not
        /// be saved.
        /// </para>
        /// </param>
        /// <returns>
        /// A snowfall map image for this planet at the given proportion of a year. Pixel luminosity
        /// indicates snowfall in mm/hr, relative to the <see cref="Atmosphere.MaxSnowfall"/> of
        /// this planet's <see cref="Atmosphere"/>.
        /// </returns>
        /// <remarks>
        /// <para>
        /// If maps exist, they will be returned at their native resolutions. If maps do not already
        /// exist, new ones will be generated at a default resolution.
        /// </para>
        /// <para>
        /// Note: if you will be getting multiple images, it is more efficient to use the <see
        /// cref="GetSnowfallMapsAsync(int, ISurfaceMapLoader?)"/> method to get the entire set
        /// at once.
        /// </para>
        /// </remarks>
        public async Task<Image<L16>> GetSnowfallMapAsync(double proportionOfYear, int steps = 1, ISurfaceMapLoader? mapLoader = null)
        {
            var maps = await GetSnowfallMapsAsync(steps, mapLoader).ConfigureAwait(false);
            var proportionPerMap = 1.0 / maps.Length;
            var season = (int)Math.Floor(proportionOfYear / proportionPerMap).Clamp(0, maps.Length - 1);
            var nextSeason = season == maps.Length - 1
                ? 0
                : season + 1;
            var weight = proportionOfYear % proportionPerMap;
            var map = weight.IsNearlyZero()
                ? maps[season]!.CloneAs<L16>()
                : SurfaceMapImage.InterpolateImages(maps[season]!, maps[nextSeason]!, weight);
            for (var i = 0; i < maps.Length; i++)
            {
                maps[i].Dispose();
            }
            return map;
        }

        /// <summary>
        /// Produces a snowfall map projection at the given proportion of a year.
        /// </summary>
        /// <param name="resolution">The vertical resolution of the projection.</param>
        /// <param name="proportionOfYear">
        /// The proportion of a full year at which the map is to be generated, assuming a year
        /// begins and ends at the winter solstice in the northern hemisphere.
        /// </param>
        /// <param name="steps">
        /// <para>
        /// The number of maps to generate internally (representing evenly spaced "seasons" during a
        /// year, starting and ending at the winter solstice in the northern hemisphere), before
        /// interpolating them into a single image.
        /// </para>
        /// <para>
        /// If stored maps exist, they will be used and this parameter will be ignored.
        /// </para>
        /// </param>
        /// <param name="options">
        /// <para>
        /// The map projection options used.
        /// </para>
        /// <para>
        /// If left <see langword="null"/> an equirectangular projection of the full globe is
        /// produced.
        /// </para>
        /// </param>
        /// <param name="mapLoader">
        /// <para>
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used.
        /// </para>
        /// <para>
        /// If <see langword="null"/> no stored maps will be used, and any generated maps will not
        /// be saved.
        /// </para>
        /// </param>
        /// <returns>
        /// A projected map of snowfall at the given proportion of a year. Pixel luminosity
        /// indicates precipitation in mm/hr, relative to the <see cref="Atmosphere.MaxSnowfall"/>
        /// of this planet's <see cref="Atmosphere"/>.
        /// </returns>
        /// <remarks>
        /// Note: if you will be getting multiple images, it is more efficient to use the <see
        /// cref="GetSnowfallMapProjectionsAsync(int, int, MapProjectionOptions?,
        /// ISurfaceMapLoader?)"/> method to get the entire set at once.
        /// </remarks>
        public async Task<Image<L16>> GetSnowfallMapProjectionAsync(
            int resolution,
            double proportionOfYear,
            int steps = 1,
            MapProjectionOptions? options = null,
            ISurfaceMapLoader? mapLoader = null)
        {
            using var map = await GetSnowfallMapAsync(proportionOfYear, steps, mapLoader).ConfigureAwait(false);
            return SurfaceMapImage.GetMapImage(
                map,
                resolution,
                options);
        }

        /// <summary>
        /// Gets the stored set of snowfall map images for this planet, if any.
        /// </summary>
        /// <returns>The stored set of snowfall map images for this planet, if any.</returns>
        public Image?[] GetSnowfallMaps()
        {
            if (_snowfallMaps is null)
            {
                return Array.Empty<Image?>();
            }
            var maps = new Image?[_snowfallMaps.Length];
            for (var i = 0; i < _snowfallMaps.Length; i++)
            {
                maps[i] = _snowfallMaps[i];
            }
            return maps;
        }

        /// <summary>
        /// Gets or generates a set of snowfall map images for this planet.
        /// </summary>
        /// <param name="steps">
        /// <para>
        /// The number of maps to generate (representing evenly spaced "seasons" during a year,
        /// starting and ending at the winter solstice in the northern hemisphere).
        /// </para>
        /// <para>
        /// If stored maps exist but in a different number, they will be interpolated.
        /// </para>
        /// </param>
        /// <param name="mapLoader">
        /// <para>
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used.
        /// </para>
        /// <para>
        /// If <see langword="null"/> no stored maps will be used, and any generated maps will not
        /// be saved.
        /// </para>
        /// </param>
        /// <returns>
        /// A set of snowfall map images for this planet. Pixel luminosity indicates snowfall in
        /// mm/hr, relative to the <see cref="Atmosphere.MaxSnowfall"/> of this planet's <see
        /// cref="Atmosphere"/>.
        /// </returns>
        /// <remarks>
        /// If maps exist, they will be returned at their native resolutions. If maps do not already
        /// exist, new ones will be generated at a default resolution.
        /// </remarks>
        public async Task<Image<L16>[]> GetSnowfallMapsAsync(int steps, ISurfaceMapLoader? mapLoader = null)
        {
            if (steps == 1)
            {
                var map = await GetSnowfallMapAsync(steps, mapLoader).ConfigureAwait(false);
                return new[] { map };
            }
            if ((_snowfallMaps is null || _snowfallMaps.Length == 0) && HasSnowfallMap && mapLoader is not null)
            {
                await LoadSnowfallMapsAsync(mapLoader).ConfigureAwait(false);
            }
            if (_snowfallMaps is null
                || _snowfallMaps.Length == 0
                || _snowfallMaps.Any(x => x is null))
            {
                if (_precipitationMaps is not null
                    && _precipitationMaps.Length > 0
                    && !_precipitationMaps.Any(x => x is null))
                {
                    var propYear = 0.0;
                    var propPerSeason = 1.0 / steps;
                    _snowfallMaps = new Image[_precipitationMaps.Length];
                    for (var i = 0; i < _precipitationMaps.Length; i++)
                    {
                        using var tImg = await GetTemperatureMapAsync(propYear, mapLoader).ConfigureAwait(false);
                        using var img = _precipitationMaps[i]!.CloneAs<L16>();
                        _snowfallMaps[i] = img.GetSnowfallMap(tImg);
                        propYear += propPerSeason;
                    }
                    if (mapLoader is not null)
                    {
                        await AssignSnowfallMapsAsync(_snowfallMaps!, mapLoader).ConfigureAwait(false);
                    }
                }
                else
                {
                    var winterMap = await GetTemperatureMapWinterAsync(mapLoader).ConfigureAwait(false);
                    var summerMap = await GetTemperatureMapSummerAsync(mapLoader).ConfigureAwait(false);
                    var (precipitationMaps, snowfallMaps) = SurfaceMapImage.GeneratePrecipitationMaps(this, winterMap, summerMap, DefaultMapResolution, steps);
                    _snowfallMaps = snowfallMaps;
                    if (mapLoader is not null)
                    {
                        await AssignSnowfallMapsAsync(snowfallMaps, mapLoader).ConfigureAwait(false);
                    }
                    if (_precipitationMaps is null && !HasPrecipitationMap)
                    {
                        _precipitationMaps = precipitationMaps;
                        if (mapLoader is not null)
                        {
                            await AssignPrecipitationMapsAsync(precipitationMaps, mapLoader).ConfigureAwait(false);
                        }
                    }
                }
            }
            var maps = new Image<L16>[steps];
            if (_snowfallMaps.Length == steps)
            {
                for (var i = 0; i < steps; i++)
                {
                    maps[i] = _snowfallMaps[i]!.CloneAs<L16>();
                }
                return maps;
            }
            var proportionOfYear = 0.0;
            var proportionPerSeason = 1.0 / steps;
            var proportionPerMap = 1.0 / _snowfallMaps.Length;
            for (var i = 0; i < steps; i++)
            {
                var season = (int)Math.Floor(proportionOfYear / proportionPerMap).Clamp(0, _snowfallMaps.Length - 1);
                var nextSeason = season == _snowfallMaps.Length - 1
                    ? 0
                    : season + 1;
                var weight = proportionOfYear % proportionPerMap;
                if (weight.IsNearlyZero())
                {
                    maps[i] = _snowfallMaps[season]!.CloneAs<L16>();
                }
                else
                {
                    maps[i] = SurfaceMapImage.InterpolateImages(_snowfallMaps[season]!, _snowfallMaps[nextSeason]!, weight);
                }
                proportionOfYear += proportionPerSeason;
            }
            return maps;
        }

        /// <summary>
        /// Produces a set of snowfall map projections.
        /// </summary>
        /// <param name="resolution">The vertical resolution of the projection.</param>
        /// <param name="steps">
        /// <para>
        /// The number of maps to generate (representing evenly spaced "seasons" during a year,
        /// starting and ending at the winter solstice in the northern hemisphere).
        /// </para>
        /// <para>
        /// If stored maps exist but in a different number, they will be interpolated.
        /// </para>
        /// </param>
        /// <param name="options">
        /// <para>
        /// The map projection options used.
        /// </para>
        /// <para>
        /// If left <see langword="null"/> an equirectangular projection of the full globe is
        /// produced.
        /// </para>
        /// </param>
        /// <param name="mapLoader">
        /// <para>
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used.
        /// </para>
        /// <para>
        /// If <see langword="null"/> no stored map will be used, and any generated map will not be
        /// saved.
        /// </para>
        /// </param>
        /// <returns>
        /// A set of projected maps of snowfall. Pixel luminosity indicates snowfall in mm/hr,
        /// relative to the <see cref="Atmosphere.MaxSnowfall"/> of this planet's <see
        /// cref="Atmosphere"/>.
        /// </returns>
        public async Task<Image<L16>[]> GetSnowfallMapProjectionsAsync(
            int resolution,
            int steps,
            MapProjectionOptions? options = null,
            ISurfaceMapLoader? mapLoader = null)
        {
            var maps = await GetSnowfallMapsAsync(steps, mapLoader).ConfigureAwait(false);
            var newMaps = new Image<L16>[maps.Length];
            for (var i = 0; i < steps; i++)
            {
                newMaps[i] = SurfaceMapImage.GetMapImage(
                    maps[i],
                    resolution,
                    options);
                maps[i].Dispose();
            }
            return newMaps;
        }

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
                _surfaceTemperature = (_blackbodyTemperature * InsolationFactor_Equatorial) + greenhouseEffect;
            }
            return _surfaceTemperature.Value;
        }

        /// <summary>
        /// Calculates the surface temperature at the given position, in K.
        /// </summary>
        /// <param name="moment">The time at which to make the calculation.</param>
        /// <param name="latitude">
        /// The latitude at which to calculate the temperature, in radians.
        /// </param>
        /// <param name="longitude">
        /// The latitude at which to calculate the temperature, in radians.
        /// </param>
        /// <param name="mapLoader">
        /// <para>
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used to load any stored
        /// map image.
        /// </para>
        /// <para>
        /// If <see langword="null"/> no stored map will be used. Even if one exists, a random map
        /// will be generated and kept in memory only.
        /// </para>
        /// </param>
        /// <returns>The surface temperature, in K.</returns>
        public async Task<double> GetSurfaceTemperatureAsync(
            Instant moment,
            double latitude,
            double longitude,
            ISurfaceMapLoader? mapLoader = null)
        {
            using var map = await GetTemperatureMapAsync(
                GetProportionOfYearAtTime(moment),
                mapLoader)
                .ConfigureAwait(false);
            return map.GetTemperature(latitude, longitude, MapProjectionOptions.Default);
        }

        /// <summary>
        /// Calculates the surface temperature at the given position, in K.
        /// </summary>
        /// <param name="moment">The time at which to make the calculation.</param>
        /// <param name="position">
        /// The surface position at which temperature will be calculated.
        /// </param>
        /// <param name="mapLoader">
        /// <para>
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used to load any stored
        /// map image.
        /// </para>
        /// <para>
        /// If <see langword="null"/> no stored map will be used. Even if one exists, a random map
        /// will be generated and kept in memory only.
        /// </para>
        /// </param>
        /// <returns>The surface temperature, in K.</returns>
        public Task<double> GetSurfaceTemperatureAsync(Instant moment, Vector3 position, ISurfaceMapLoader? mapLoader = null)
            => GetSurfaceTemperatureAsync(moment, VectorToLatitude(position), VectorToLongitude(position), mapLoader);

        /// <summary>
        /// Calculates the range of temperatures at the given <paramref name="latitude"/> and
        /// <paramref name="longitude"/>, in K.
        /// </summary>
        /// <param name="latitude">
        /// The latitude at which to calculate the temperature range, in radians.
        /// </param>
        /// <param name="longitude">
        /// The latitude at which to calculate the temperature range, in radians.
        /// </param>
        /// <param name="mapLoader">
        /// <para>
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used to load any stored
        /// map image.
        /// </para>
        /// <para>
        /// If <see langword="null"/> no stored map will be used. Even if one exists, a random map
        /// will be generated and kept in memory only.
        /// </para>
        /// </param>
        /// <returns>
        /// A <see cref="FloatRange"/> giving the range of temperatures at the given <paramref
        /// name="latitude"/> and <paramref name="longitude"/>, in K.
        /// </returns>
        public async Task<FloatRange> GetSurfaceTemperatureAsync(
            double latitude,
            double longitude,
            ISurfaceMapLoader? mapLoader = null)
        {
            using var winterMap = await GetTemperatureMapWinterAsync(
                mapLoader)
                .ConfigureAwait(false);
            using var summerMap = await GetTemperatureMapWinterAsync(
                mapLoader)
                .ConfigureAwait(false);
            var winterTemperature = winterMap.GetTemperature(latitude, longitude, MapProjectionOptions.Default);
            var summerTemperature = winterMap.GetTemperature(latitude, longitude, MapProjectionOptions.Default);
            return new FloatRange(
                (float)Math.Min(winterTemperature, summerTemperature),
                (float)Math.Max(winterTemperature, summerTemperature));
        }

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

                // Represent the effect of near-surface atmospheric convection by resturning the
                // average of the raw surface temperature and the result, weighted by the elevation.
                var weight = Math.Min(1, elevation * 4 / MaxElevation);

                return surfaceTemp.Lerp(value, weight);
            }
        }

        /// <summary>
        /// Gets or generates a temperature map image for this planet.
        /// </summary>
        /// <param name="mapLoader">
        /// <para>
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used.
        /// </para>
        /// <para>
        /// If <see langword="null"/> no stored maps will be used, and any generated maps will not
        /// be saved.
        /// </para>
        /// </param>
        /// <returns>
        /// A temperature map image for this planet. Pixel luminosity indicates temperature relative
        /// to 5000K.
        /// </returns>
        /// <remarks>
        /// If maps exist, the result will be at the maximum of their native resolutions. If maps do
        /// not already exist, new ones will be generated at a default resolution.
        /// </remarks>
        public async Task<Image<L16>> GetTemperatureMapAsync(ISurfaceMapLoader? mapLoader = null)
        {
            if ((_temperatureMapWinter is null || _temperatureMapSummer is null)
                && HasTemperatureMap
                && mapLoader is not null)
            {
                await LoadTemperatureMapAsync(mapLoader).ConfigureAwait(false);
            }
            if (_temperatureMapWinter is null || _temperatureMapSummer is null)
            {
                var elevationMap = await GetElevationMapAsync(mapLoader)
                    .ConfigureAwait(false);
                (_temperatureMapWinter, _temperatureMapSummer) = SurfaceMapImage.GenerateTemperatureMaps(this, elevationMap, DefaultMapResolution);
                if (mapLoader is not null)
                {
                    await AssignTemperatureMapWinterAsync(_temperatureMapWinter, mapLoader).ConfigureAwait(false);
                    await AssignTemperatureMapWinterAsync(_temperatureMapSummer, mapLoader).ConfigureAwait(false);
                }
            }
            return SurfaceMapImage.GenerateTemperatureMap(_temperatureMapWinter, _temperatureMapSummer);
        }

        /// <summary>
        /// Gets or generates a temperature map image for this planet at the given proportion of a
        /// year.
        /// </summary>
        /// <param name="proportionOfYear">
        /// The proportion of a full year at which the map is to be generated, assuming a year
        /// begins and ends at the winter solstice in the northern hemisphere.
        /// </param>
        /// <param name="mapLoader">
        /// <para>
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used.
        /// </para>
        /// <para>
        /// If <see langword="null"/> no stored maps will be used, and any generated maps will not
        /// be saved.
        /// </para>
        /// </param>
        /// <returns>
        /// A temperature map image for this planet at the given proportion of a year. Pixel
        /// luminosity indicates temperature relative to 5000K.
        /// </returns>
        /// <remarks>
        /// If maps exist, the result will be at the maximum of their native resolutions. If maps do
        /// not already exist, new ones will be generated at a default resolution.
        /// </remarks>
        public async Task<Image<L16>> GetTemperatureMapAsync(double proportionOfYear, ISurfaceMapLoader? mapLoader = null)
        {
            if ((_temperatureMapWinter is null || _temperatureMapSummer is null)
                && HasTemperatureMap
                && mapLoader is not null)
            {
                await LoadTemperatureMapAsync(mapLoader).ConfigureAwait(false);
            }
            if (_temperatureMapWinter is null || _temperatureMapSummer is null)
            {
                var elevationMap = await GetElevationMapAsync(mapLoader)
                    .ConfigureAwait(false);
                (_temperatureMapWinter, _temperatureMapSummer) = SurfaceMapImage.GenerateTemperatureMaps(this, elevationMap, DefaultMapResolution);
                if (mapLoader is not null)
                {
                    await AssignTemperatureMapWinterAsync(_temperatureMapWinter, mapLoader).ConfigureAwait(false);
                    await AssignTemperatureMapWinterAsync(_temperatureMapSummer, mapLoader).ConfigureAwait(false);
                }
            }
            return SurfaceMapImage.InterpolateImages(_temperatureMapWinter, _temperatureMapSummer, proportionOfYear);
        }

        /// <summary>
        /// Produces a temperature map projection.
        /// </summary>
        /// <param name="resolution">The vertical resolution of the projection.</param>
        /// <param name="options">
        /// <para>
        /// The map projection options used.
        /// </para>
        /// <para>
        /// If left <see langword="null"/> an equirectangular projection of the full globe is
        /// produced.
        /// </para>
        /// </param>
        /// <param name="mapLoader">
        /// <para>
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used.
        /// </para>
        /// <para>
        /// If <see langword="null"/> no stored map will be used, and any generated map will not be
        /// saved.
        /// </para>
        /// </param>
        /// <returns>
        /// A projected map of temperature. Pixel luminosity indicates temperature relative to
        /// 5000K.
        /// </returns>
        public async Task<Image<L16>> GetTemperatureMapProjectionAsync(
            int resolution,
            MapProjectionOptions? options = null,
            ISurfaceMapLoader? mapLoader = null)
        {
            using var map = await GetTemperatureMapAsync(mapLoader).ConfigureAwait(false);
            return SurfaceMapImage.GetMapImage(
                map,
                resolution,
                options);
        }

        /// <summary>
        /// Produces a temperature map projection at the given proportion of a year.
        /// </summary>
        /// <param name="resolution">The vertical resolution of the projection.</param>
        /// <param name="proportionOfYear">
        /// The proportion of a full year at which the map is to be generated, assuming a year
        /// begins and ends at the winter solstice in the northern hemisphere.
        /// </param>
        /// <param name="options">
        /// <para>
        /// The map projection options used.
        /// </para>
        /// <para>
        /// If left <see langword="null"/> an equirectangular projection of the full globe is
        /// produced.
        /// </para>
        /// </param>
        /// <param name="mapLoader">
        /// <para>
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used.
        /// </para>
        /// <para>
        /// If <see langword="null"/> no stored map will be used, and any generated map will not be
        /// saved.
        /// </para>
        /// </param>
        /// <returns>
        /// A projected map of temperature at the given proportion of a year. Pixel luminosity
        /// indicates temperature relative to 5000K.
        /// </returns>
        public async Task<Image<L16>> GetTemperatureMapProjectionAsync(
            int resolution,
            double proportionOfYear,
            MapProjectionOptions? options = null,
            ISurfaceMapLoader? mapLoader = null)
        {
            using var map = await GetTemperatureMapAsync(proportionOfYear, mapLoader).ConfigureAwait(false);
            return SurfaceMapImage.GetMapImage(
                map,
                resolution,
                options);
        }

        /// <summary>
        /// Produces a temperature map projection of the summer solstice in the northern hemisphere.
        /// </summary>
        /// <param name="resolution">The vertical resolution of the projection.</param>
        /// <param name="options">
        /// <para>
        /// The map projection options used.
        /// </para>
        /// <para>
        /// If left <see langword="null"/> an equirectangular projection of the full globe is
        /// produced.
        /// </para>
        /// </param>
        /// <param name="mapLoader">
        /// <para>
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used.
        /// </para>
        /// <para>
        /// If <see langword="null"/> no stored map will be used, and any generated map will not be
        /// saved.
        /// </para>
        /// </param>
        /// <returns>
        /// A projected map of temperature at the summer solstice in the northern hemisphere. Pixel
        /// luminosity indicates temperature relative to 5000K.
        /// </returns>
        public async Task<Image<L16>> GetTemperatureMapProjectionSummerAsync(
            int resolution,
            MapProjectionOptions? options = null,
            ISurfaceMapLoader? mapLoader = null)
        {
            using var map = await GetTemperatureMapSummerAsync(mapLoader).ConfigureAwait(false);
            return SurfaceMapImage.GetMapImage(
                map,
                resolution,
                options);
        }

        /// <summary>
        /// Produces a temperature map projection of the winter solstice in the northern hemisphere.
        /// </summary>
        /// <param name="resolution">The vertical resolution of the projection.</param>
        /// <param name="options">
        /// <para>
        /// The map projection options used.
        /// </para>
        /// <para>
        /// If left <see langword="null"/> an equirectangular projection of the full globe is
        /// produced.
        /// </para>
        /// </param>
        /// <param name="mapLoader">
        /// <para>
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used.
        /// </para>
        /// <para>
        /// If <see langword="null"/> no stored map will be used, and any generated map will not be
        /// saved.
        /// </para>
        /// </param>
        /// <returns>
        /// A projected map of temperature at the winter solstice in the northern hemisphere. Pixel
        /// luminosity indicates temperature relative to 5000K.
        /// </returns>
        public async Task<Image<L16>> GetTemperatureMapProjectionWinterAsync(
            int resolution,
            MapProjectionOptions? options = null,
            ISurfaceMapLoader? mapLoader = null)
        {
            using var map = await GetTemperatureMapWinterAsync(mapLoader).ConfigureAwait(false);
            return SurfaceMapImage.GetMapImage(
                map,
                resolution,
                options);
        }

        /// <summary>
        /// Gets the stored temperature map image for this planet at the summer solstice of the
        /// northern hemisphere, if any.
        /// </summary>
        /// <returns>
        /// The stored temperature map image for this planet at the summer solstice of the northern
        /// hemisphere, if any.
        /// </returns>
        public Image? GetTemperatureMapSummer() => _temperatureMapSummer ?? _temperatureMapWinter;

        /// <summary>
        /// Gets or generates a temperature map image for this planet at the summer solstice in the
        /// northern hemisphere.
        /// </summary>
        /// <param name="mapLoader">
        /// <para>
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used.
        /// </para>
        /// <para>
        /// If <see langword="null"/> no stored maps will be used, and any generated maps will not
        /// be saved.
        /// </para>
        /// </param>
        /// <returns>
        /// A temperature map image for this planet at the summer solstice in the northern
        /// hemisphere. Pixel luminosity indicates temperature relative to 5000K.
        /// </returns>
        /// <remarks>
        /// If a map exists, it will be returned at its native resolution. If a map does not already
        /// exist, a new one will be generated at a default resolution.
        /// </remarks>
        public async Task<Image<L16>> GetTemperatureMapSummerAsync(ISurfaceMapLoader? mapLoader = null)
        {
            if (_temperatureMapSummer is null && HasTemperatureMap && mapLoader is not null)
            {
                await LoadTemperatureMapAsync(mapLoader).ConfigureAwait(false);
            }
            if (_temperatureMapSummer is null)
            {
                var elevationMap = await GetElevationMapAsync(mapLoader)
                    .ConfigureAwait(false);
                (_temperatureMapWinter, _temperatureMapSummer) = SurfaceMapImage.GenerateTemperatureMaps(this, elevationMap, DefaultMapResolution);
                if (mapLoader is not null)
                {
                    await AssignTemperatureMapWinterAsync(_temperatureMapWinter, mapLoader).ConfigureAwait(false);
                    await AssignTemperatureMapWinterAsync(_temperatureMapSummer, mapLoader).ConfigureAwait(false);
                }
            }
            return _temperatureMapSummer.CloneAs<L16>();
        }

        /// <summary>
        /// Gets the stored temperature map image for this planet at the winter solstice, if any.
        /// </summary>
        /// <returns>The stored temperature map image for this planet at the winter solstice, if
        /// any.</returns>
        public Image? GetTemperatureMapWinter() => _temperatureMapWinter ?? _temperatureMapSummer;

        /// <summary>
        /// Gets or generates a temperature map image for this planet at the winter solstice in the
        /// northern hemisphere.
        /// </summary>
        /// <param name="mapLoader">
        /// <para>
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used.
        /// </para>
        /// <para>
        /// If <see langword="null"/> no stored maps will be used, and any generated maps will not
        /// be saved.
        /// </para>
        /// </param>
        /// <returns>
        /// A temperature map image for this planet at the winter solstice in the northern
        /// hemisphere. Pixel luminosity indicates temperature relative to 5000K.
        /// </returns>
        /// <remarks>
        /// If a map exists, it will be returned at its native resolution. If a map does not already
        /// exist, a new one will be generated at a default resolution.
        /// </remarks>
        public async Task<Image<L16>> GetTemperatureMapWinterAsync(ISurfaceMapLoader? mapLoader = null)
        {
            if (_temperatureMapWinter is null && HasTemperatureMap && mapLoader is not null)
            {
                await LoadTemperatureMapAsync(mapLoader).ConfigureAwait(false);
            }
            if (_temperatureMapWinter is null)
            {
                var elevationMap = await GetElevationMapAsync(mapLoader)
                    .ConfigureAwait(false);
                (_temperatureMapWinter, _temperatureMapSummer) = SurfaceMapImage.GenerateTemperatureMaps(this, elevationMap, DefaultMapResolution);
                if (mapLoader is not null)
                {
                    await AssignTemperatureMapWinterAsync(_temperatureMapWinter, mapLoader).ConfigureAwait(false);
                    await AssignTemperatureMapWinterAsync(_temperatureMapSummer, mapLoader).ConfigureAwait(false);
                }
            }
            return _temperatureMapWinter.CloneAs<L16>();
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

            if (habitabilityRequirements.AtmosphericRequirements != null
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
        /// Converts latitude and longitude to a <see cref="Vector3"/>.
        /// </summary>
        /// <param name="latitude">A latitude, as an angle in radians from the equator.</param>
        /// <param name="longitude">A longitude, as an angle in radians from the X-axis at 0
        /// rotation.</param>
        /// <returns>A normalized <see cref="Vector3"/> representing a position on the surface of
        /// this <see cref="Planetoid"/>.</returns>
        /// <remarks>
        /// If the planet's axis has never been set, it is treated as vertical for the purpose of
        /// this calculation, but is not permanently set to such an axis.
        /// </remarks>
        public Vector3 LatitudeAndLongitudeToVector(double latitude, double longitude)
        {
            var v = LatitudeAndLongitudeToDoubleVector(latitude, longitude);
            return new Vector3(v.X, v.Y, v.Z);
        }

        /// <summary>
        /// Loads the elevation map for this planet from storage.
        /// </summary>
        /// <param name="mapLoader">
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used to retrieve the
        /// image.
        /// </param>
        public async Task LoadElevationMapAsync(ISurfaceMapLoader mapLoader)
        {
            if (!string.IsNullOrEmpty(_elevationMapPath))
            {
                _elevationMap = await mapLoader.LoadAsync(_elevationMapPath).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Loads a set of images as the precipitation maps for this planet from storage.
        /// </summary>
        /// <param name="mapLoader">
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used to retrieve the
        /// image.
        /// </param>
        public async Task LoadPrecipitationMapsAsync(ISurfaceMapLoader mapLoader)
        {
            if (_precipitationMapPaths is null)
            {
                return;
            }
            _precipitationMaps = new Image?[_precipitationMapPaths.Length];
            for (var i = 0; i < _precipitationMapPaths.Length; i++)
            {
                if (!string.IsNullOrEmpty(_precipitationMapPaths[i]))
                {
                    _precipitationMaps[i] = await mapLoader.LoadAsync(_precipitationMapPaths[i]).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Loads a set of images as the snowfall maps for this planet from storage.
        /// </summary>
        /// <param name="mapLoader">
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used to retrieve the
        /// image.
        /// </param>
        public async Task LoadSnowfallMapsAsync(ISurfaceMapLoader mapLoader)
        {
            if (_snowfallMapPaths is null)
            {
                return;
            }
            _snowfallMaps = new Image?[_snowfallMapPaths.Length];
            for (var i = 0; i < _snowfallMapPaths.Length; i++)
            {
                if (!string.IsNullOrEmpty(_snowfallMapPaths[i]))
                {
                    _snowfallMaps[i] = await mapLoader.LoadAsync(_snowfallMapPaths[i]).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Loads the temperature map(s) for this planet from storage.
        /// </summary>
        /// <param name="mapLoader">
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used to retrieve the
        /// image.
        /// </param>
        public async Task LoadTemperatureMapAsync(ISurfaceMapLoader mapLoader)
        {
            if (!string.IsNullOrEmpty(_temperatureMapSummerPath))
            {
                _temperatureMapSummer = await mapLoader.LoadAsync(_temperatureMapSummerPath).ConfigureAwait(false);
                if (string.IsNullOrEmpty(_temperatureMapWinterPath))
                {
                    _temperatureMapWinter = _temperatureMapSummer;
                }
            }

            if (!string.IsNullOrEmpty(_temperatureMapWinterPath))
            {
                _temperatureMapWinter = await mapLoader.LoadAsync(_temperatureMapWinterPath).ConfigureAwait(false);
                if (_temperatureMapSummer is null)
                {
                    _temperatureMapSummer = _temperatureMapWinter;
                }
            }
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
        /// Sets the length of time it takes for this <see cref="Planetoid"/> to rotate once about
        /// its axis, in seconds.
        /// </summary>
        /// <param name="value">A <see cref="Number"/> value.</param>
        public void SetRotationalPeriod(Number value)
        {
            RotationalPeriod = value;
            _angularVelocity = null;
            ResetCachedTemperatures();
        }

        /// <summary>
        /// Converts a <see cref="Vector3"/> to a latitude, in radians.
        /// </summary>
        /// <param name="v">A vector representing a position on the surface of this <see cref="Planetoid"/>.</param>
        /// <returns>A latitude, as an angle in radians from the equator.</returns>
        public double VectorToLatitude(Vector3 v) => VectorToLatitude((System.Numerics.Vector3)v);

        /// <summary>
        /// Converts a <see cref="Vector3"/> to a latitude, in radians.
        /// </summary>
        /// <param name="v">A vector representing a position on the surface of this <see cref="Planetoid"/>.</param>
        /// <returns>A latitude, as an angle in radians from the equator.</returns>
        public double VectorToLatitude(System.Numerics.Vector3 v) => MathAndScience.Constants.Doubles.MathConstants.HalfPI - (double)Axis.Angle(v);

        /// <summary>
        /// Converts a <see cref="Vector3"/> to a longitude, in radians.
        /// </summary>
        /// <param name="v">A vector representing a position on the surface of this <see cref="Planetoid"/>.</param>
        /// <returns>A longitude, as an angle in radians from the X-axis at 0 rotation.</returns>
        public double VectorToLongitude(Vector3 v) => VectorToLongitude((System.Numerics.Vector3)v);

        /// <summary>
        /// Converts a <see cref="Vector3"/> to a longitude, in radians.
        /// </summary>
        /// <param name="v">A vector representing a position on the surface of this <see cref="Planetoid"/>.</param>
        /// <returns>A longitude, as an angle in radians from the X-axis at 0 rotation.</returns>
        public double VectorToLongitude(System.Numerics.Vector3 v)
        {
            var u = System.Numerics.Vector3.Transform(v, AxisRotation);
            return u.X.IsNearlyZero() && u.Z.IsNearlyZero()
                ? 0
                : Math.Atan2(u.X, u.Z);
        }

        internal static double GetSeasonalLatitudeFromDeclination(double latitude, double solarDeclination)
        {
            var seasonalLatitude = latitude + solarDeclination;
            if (seasonalLatitude > MathAndScience.Constants.Doubles.MathConstants.HalfPI)
            {
                return Math.PI - seasonalLatitude;
            }
            else if (seasonalLatitude < -MathAndScience.Constants.Doubles.MathConstants.HalfPI)
            {
                return -seasonalLatitude - Math.PI;
            }
            return seasonalLatitude;
        }

        internal static Number GetSpaceForType(PlanetType type) => type switch
        {
            PlanetType.AsteroidC => _AsteroidSpace,
            PlanetType.AsteroidM => _AsteroidSpace,
            PlanetType.AsteroidS => _AsteroidSpace,
            PlanetType.Comet => _CometSpace,
            PlanetType.Dwarf => _DwarfSpace,
            PlanetType.LavaDwarf => _DwarfSpace,
            PlanetType.RockyDwarf => _DwarfSpace,
            PlanetType.GasGiant => GiantSpace,
            PlanetType.IceGiant => GiantSpace,
            _ => _TerrestrialSpace,
        };

        internal double GetInsolationFactor(Number atmosphereMass, double atmosphericScaleHeight, bool polar = false)
            => (double)Number.Pow(1320000
                * atmosphereMass
                * (polar
                    ? Math.Pow(0.7, Math.Pow(GetPolarAirMass(atmosphericScaleHeight), 0.678))
                    : 0.7)
                / Mass
                , new Number(25, -2));

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
        internal Number GetMutualHillSphereRadius(Number otherMass)
            => Orbit?.GetMutualHillSphereRadius(Mass, otherMass) ?? Number.Zero;

        internal double GetElevationNoise(Vector3 position)
            => GetElevationNoise((double)position.X, (double)position.Y, (double)position.Z);

        internal double GetElevationNoise(MathAndScience.Numerics.Doubles.Vector3 position)
            => GetElevationNoise(position.X, position.Y, position.Z);

        internal double GetPrecipitationNoise(
            Vector3 position,
            double latitude,
            double seasonalLatitude,
            double temperature,
            out double snow)
            => GetPrecipitationNoise((double)position.X, (double)position.Y, (double)position.Z, latitude, seasonalLatitude, temperature, out snow);

        internal double GetPrecipitationNoise(
            MathAndScience.Numerics.Doubles.Vector3 position,
            double latitude,
            double seasonalLatitude,
            double temperature,
            out double snow)
            => GetPrecipitationNoise(position.X, position.Y, position.Z, latitude, seasonalLatitude, temperature, out snow);

        internal double GetSolarDeclination(double trueAnomaly)
            => Orbit.HasValue ? Math.Asin(Math.Sin(-AxialTilt) * Math.Sin(Orbit.Value.GetEclipticLongitudeAtTrueAnomaly(trueAnomaly))) : 0;

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
        internal double GetSurfaceTemperatureAtTrueAnomaly(double trueAnomaly, double seasonalLatitude)
            => GetSeasonalSurfaceTemperature(GetTemperatureAtTrueAnomaly(trueAnomaly), seasonalLatitude);

        internal MathAndScience.Numerics.Doubles.Vector3 LatitudeAndLongitudeToDoubleVector(double latitude, double longitude)
        {
            var cosLat = Math.Cos(latitude);
            var rot = AxisRotation;
            return MathAndScience.Numerics.Doubles.Vector3.Normalize(
                MathAndScience.Numerics.Doubles.Vector3.Transform(
                    new MathAndScience.Numerics.Doubles.Vector3(
                        cosLat * Math.Sin(longitude),
                        Math.Sin(latitude),
                        cosLat * Math.Cos(longitude)),
                    MathAndScience.Numerics.Doubles.Quaternion.Inverse(rot)));
        }

        internal override async ValueTask ResetOrbitAsync(IDataStore dataStore)
        {
            _axialTilt = null;

            var stars = new List<Star>();
            if (Orbit.HasValue)
            {
                var system = await GetStarSystemAsync(dataStore).ConfigureAwait(false);
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

        private static IEnumerable<IMaterial> GetCore_Giant(
            Rehydrator rehydrator,
            IShape planetShape,
            Number coreProportion,
            Number planetMass)
        {
            var coreMass = planetMass * coreProportion;

            var coreTemp = (double)(planetShape.ContainingRadius / 3);

            var innerCoreProportion = Number.Min(rehydrator.NextNumber(12, new Number(2, -2), new Number(2, -1)), _GiantMinMassForType / coreMass);
            var innerCoreMass = coreMass * innerCoreProportion;
            var innerCoreRadius = planetShape.ContainingRadius * coreProportion * innerCoreProportion;
            var innerCoreShape = new Sphere(innerCoreRadius, planetShape.Position);
            yield return new Material(
                Substances.All.IronNickelAlloy.GetHomogeneousReference(),
                (double)(innerCoreMass / innerCoreShape.Volume),
                innerCoreMass,
                innerCoreShape,
                coreTemp);

            // Molten rock outer core.
            var outerCoreMass = coreMass - innerCoreMass;
            var outerCoreShape = new HollowSphere(innerCoreRadius, planetShape.ContainingRadius * coreProportion, planetShape.Position);
            yield return new Material(
                CelestialSubstances.ChondriticRock,
                (double)(outerCoreMass / outerCoreShape.Volume),
                outerCoreMass,
                outerCoreShape,
                coreTemp);
        }

        private static IEnumerable<IMaterial> GetCrust_Carbon(
            Rehydrator rehydrator,
            IShape planetShape,
            Number crustProportion,
            Number planetMass)
        {
            var crustMass = planetMass * crustProportion;

            var shape = new HollowSphere(
                planetShape.ContainingRadius - (planetShape.ContainingRadius * crustProportion),
                planetShape.ContainingRadius,
                planetShape.Position);

            // Carbonaceous crust of graphite, diamond, and hydrocarbons, with trace minerals

            var graphite = 1m;

            var aluminium = (decimal)rehydrator.NormalDistributionSample(14, 0.026, 4e-3, minimum: 0);
            var iron = (decimal)rehydrator.NormalDistributionSample(15, 1.67e-2, 2.75e-3, minimum: 0);
            var titanium = (decimal)rehydrator.NormalDistributionSample(16, 5.7e-3, 9e-4, minimum: 0);

            var chalcopyrite = (decimal)rehydrator.NormalDistributionSample(17, 1.1e-3, 1.8e-4, minimum: 0); // copper
            graphite -= chalcopyrite;
            var chromite = (decimal)rehydrator.NormalDistributionSample(18, 5.5e-4, 9e-5, minimum: 0);
            graphite -= chromite;
            var sphalerite = (decimal)rehydrator.NormalDistributionSample(19, 8.1e-5, 1.3e-5, minimum: 0); // zinc
            graphite -= sphalerite;
            var galena = (decimal)rehydrator.NormalDistributionSample(20, 2e-5, 3.3e-6, minimum: 0); // lead
            graphite -= galena;
            var uraninite = (decimal)rehydrator.NormalDistributionSample(21, 7.15e-6, 1.1e-6, minimum: 0);
            graphite -= uraninite;
            var cassiterite = (decimal)rehydrator.NormalDistributionSample(22, 6.7e-6, 1.1e-6, minimum: 0); // tin
            graphite -= cassiterite;
            var cinnabar = (decimal)rehydrator.NormalDistributionSample(23, 1.35e-7, 2.3e-8, minimum: 0); // mercury
            graphite -= cinnabar;
            var acanthite = (decimal)rehydrator.NormalDistributionSample(24, 5e-8, 8.3e-9, minimum: 0); // silver
            graphite -= acanthite;
            var sperrylite = (decimal)rehydrator.NormalDistributionSample(25, 1.17e-8, 2e-9, minimum: 0); // platinum
            graphite -= sperrylite;
            var gold = (decimal)rehydrator.NormalDistributionSample(26, 2.75e-9, 4.6e-10, minimum: 0);
            graphite -= gold;

            var bauxite = aluminium * 1.57m;
            graphite -= bauxite;

            var hematiteIron = iron * 3 / 4 * (decimal)rehydrator.NormalDistributionSample(27, 1, 0.167, minimum: 0);
            var hematite = hematiteIron * 2.88m;
            graphite -= hematite;
            var magnetite = (iron - hematiteIron) * 4.14m;
            graphite -= magnetite;

            var ilmenite = titanium * 2.33m;
            graphite -= ilmenite;

            var coal = graphite * (decimal)rehydrator.NormalDistributionSample(28, 0.25, 0.042, minimum: 0);
            graphite -= coal * 2;
            var oil = graphite * (decimal)rehydrator.NormalDistributionSample(29, 0.25, 0.042, minimum: 0);
            graphite -= oil;
            var gas = graphite * (decimal)rehydrator.NormalDistributionSample(30, 0.25, 0.042, minimum: 0);
            graphite -= gas;
            var diamond = graphite * (decimal)rehydrator.NormalDistributionSample(31, 0.125, 0.021, minimum: 0);
            graphite -= diamond;

            var components = new List<(ISubstanceReference, decimal)>();
            if (graphite > 0)
            {
                components.Add((Substances.All.AmorphousCarbon.GetHomogeneousReference(), graphite));
            }
            if (coal > 0)
            {
                components.Add((Substances.All.Anthracite.GetReference(), coal));
                components.Add((Substances.All.BituminousCoal.GetReference(), coal));
            }
            if (oil > 0)
            {
                components.Add((Substances.All.Petroleum.GetReference(), oil));
            }
            if (gas > 0)
            {
                components.Add((Substances.All.NaturalGas.GetReference(), gas));
            }
            if (diamond > 0)
            {
                components.Add((Substances.All.Diamond.GetHomogeneousReference(), diamond));
            }

            if (chalcopyrite > 0)
            {
                components.Add((Substances.All.Chalcopyrite.GetHomogeneousReference(), chalcopyrite));
            }
            if (chromite > 0)
            {
                components.Add((Substances.All.Chromite.GetHomogeneousReference(), chromite));
            }
            if (sphalerite > 0)
            {
                components.Add((Substances.All.Sphalerite.GetHomogeneousReference(), sphalerite));
            }
            if (galena > 0)
            {
                components.Add((Substances.All.Galena.GetHomogeneousReference(), galena));
            }
            if (uraninite > 0)
            {
                components.Add((Substances.All.Uraninite.GetHomogeneousReference(), uraninite));
            }
            if (cassiterite > 0)
            {
                components.Add((Substances.All.Cassiterite.GetHomogeneousReference(), cassiterite));
            }
            if (cinnabar > 0)
            {
                components.Add((Substances.All.Cinnabar.GetHomogeneousReference(), cinnabar));
            }
            if (acanthite > 0)
            {
                components.Add((Substances.All.Acanthite.GetHomogeneousReference(), acanthite));
            }
            if (sperrylite > 0)
            {
                components.Add((Substances.All.Sperrylite.GetHomogeneousReference(), sperrylite));
            }
            if (gold > 0)
            {
                components.Add((Substances.All.Gold.GetHomogeneousReference(), gold));
            }
            if (bauxite > 0)
            {
                components.Add((Substances.All.Bauxite.GetReference(), bauxite));
            }
            if (hematite > 0)
            {
                components.Add((Substances.All.Hematite.GetHomogeneousReference(), hematite));
            }
            if (magnetite > 0)
            {
                components.Add((Substances.All.Magnetite.GetHomogeneousReference(), magnetite));
            }
            if (ilmenite > 0)
            {
                components.Add((Substances.All.Ilmenite.GetHomogeneousReference(), ilmenite));
            }

            yield return new Material(
                (double)(crustMass / shape.Volume),
                crustMass,
                shape,
                null,
                components.ToArray());
        }

        private static IEnumerable<IMaterial> GetCrust_LavaDwarf(
            Rehydrator rehydrator,
            IShape planetShape,
            Number crustProportion,
            Number planetMass)
        {
            var crustMass = planetMass * crustProportion;

            var shape = new HollowSphere(
                planetShape.ContainingRadius - (planetShape.ContainingRadius * crustProportion),
                planetShape.ContainingRadius,
                planetShape.Position);

            // rocky crust
            // 50% chance of dust
            var dust = Math.Max(0, rehydrator.NextDecimal(13, -0.5m, 0.5m));
            var rock = 1 - dust;

            var components = new List<(ISubstanceReference, decimal)>();
            foreach (var (material, proportion) in CelestialSubstances.DryPlanetaryCrustConstituents)
            {
                components.Add((material, proportion * rock));
            }
            if (dust > 0)
            {
                components.Add((Substances.All.CosmicDust.GetHomogeneousReference(), dust));
            }
            yield return new Material(
                components,
                (double)(crustMass / shape.Volume),
                crustMass,
                shape);
        }

        private static IEnumerable<IMaterial> GetCrust_RockyDwarf(
            Rehydrator rehydrator,
            IShape planetShape,
            Number crustProportion,
            Number planetMass)
        {
            var crustMass = planetMass * crustProportion;

            var shape = new HollowSphere(
                planetShape.ContainingRadius - (planetShape.ContainingRadius * crustProportion),
                planetShape.ContainingRadius,
                planetShape.Position);

            // rocky crust
            // 50% chance of dust
            var dust = Math.Max(0, rehydrator.NextDecimal(13, -0.5m, 0.5m));
            var rock = 1 - dust;

            var components = new List<(ISubstanceReference, decimal)>();
            foreach (var (material, proportion) in CelestialSubstances.DryPlanetaryCrustConstituents)
            {
                components.Add((material, proportion * rock));
            }
            if (dust > 0)
            {
                components.Add((Substances.All.CosmicDust.GetHomogeneousReference(), dust));
            }
            yield return new Material(
                components,
                (double)(crustMass / shape.Volume),
                crustMass,
                shape);
        }

        private static IEnumerable<IMaterial> GetCrust_Terrestrial(
            Rehydrator rehydrator,
            IShape planetShape,
            Number crustProportion,
            Number planetMass)
        {
            var crustMass = planetMass * crustProportion;

            var shape = new HollowSphere(
                planetShape.ContainingRadius - (planetShape.ContainingRadius * crustProportion),
                planetShape.ContainingRadius,
                planetShape.Position);

            // Rocky crust with trace minerals

            var rock = 1m;

            var aluminium = (decimal)rehydrator.NormalDistributionSample(12, 0.026, 4e-3, minimum: 0);
            var iron = (decimal)rehydrator.NormalDistributionSample(13, 1.67e-2, 2.75e-3, minimum: 0);
            var titanium = (decimal)rehydrator.NormalDistributionSample(14, 5.7e-3, 9e-4, minimum: 0);

            var chalcopyrite = (decimal)rehydrator.NormalDistributionSample(15, 1.1e-3, 1.8e-4, minimum: 0); // copper
            rock -= chalcopyrite;
            var chromite = (decimal)rehydrator.NormalDistributionSample(16, 5.5e-4, 9e-5, minimum: 0);
            rock -= chromite;
            var sphalerite = (decimal)rehydrator.NormalDistributionSample(17, 8.1e-5, 1.3e-5, minimum: 0); // zinc
            rock -= sphalerite;
            var galena = (decimal)rehydrator.NormalDistributionSample(18, 2e-5, 3.3e-6, minimum: 0); // lead
            rock -= galena;
            var uraninite = (decimal)rehydrator.NormalDistributionSample(19, 7.15e-6, 1.1e-6, minimum: 0);
            rock -= uraninite;
            var cassiterite = (decimal)rehydrator.NormalDistributionSample(20, 6.7e-6, 1.1e-6, minimum: 0); // tin
            rock -= cassiterite;
            var cinnabar = (decimal)rehydrator.NormalDistributionSample(21, 1.35e-7, 2.3e-8, minimum: 0); // mercury
            rock -= cinnabar;
            var acanthite = (decimal)rehydrator.NormalDistributionSample(22, 5e-8, 8.3e-9, minimum: 0); // silver
            rock -= acanthite;
            var sperrylite = (decimal)rehydrator.NormalDistributionSample(23, 1.17e-8, 2e-9, minimum: 0); // platinum
            rock -= sperrylite;
            var gold = (decimal)rehydrator.NormalDistributionSample(24, 2.75e-9, 4.6e-10, minimum: 0);
            rock -= gold;

            var bauxite = aluminium * 1.57m;
            rock -= bauxite;

            var hematiteIron = iron * 3 / 4 * (decimal)rehydrator.NormalDistributionSample(25, 1, 0.167, minimum: 0);
            var hematite = hematiteIron * 2.88m;
            rock -= hematite;
            var magnetite = (iron - hematiteIron) * 4.14m;
            rock -= magnetite;

            var ilmenite = titanium * 2.33m;
            rock -= ilmenite;

            var components = new List<(ISubstanceReference, decimal)>();
            foreach (var (material, proportion) in CelestialSubstances.DryPlanetaryCrustConstituents)
            {
                components.Add((material, proportion * rock));
            }

            if (chalcopyrite > 0)
            {
                components.Add((Substances.All.Chalcopyrite.GetHomogeneousReference(), chalcopyrite));
            }
            if (chromite > 0)
            {
                components.Add((Substances.All.Chromite.GetHomogeneousReference(), chromite));
            }
            if (sphalerite > 0)
            {
                components.Add((Substances.All.Sphalerite.GetHomogeneousReference(), sphalerite));
            }
            if (galena > 0)
            {
                components.Add((Substances.All.Galena.GetHomogeneousReference(), galena));
            }
            if (uraninite > 0)
            {
                components.Add((Substances.All.Uraninite.GetHomogeneousReference(), uraninite));
            }
            if (cassiterite > 0)
            {
                components.Add((Substances.All.Cassiterite.GetHomogeneousReference(), cassiterite));
            }
            if (cinnabar > 0)
            {
                components.Add((Substances.All.Cinnabar.GetHomogeneousReference(), cinnabar));
            }
            if (acanthite > 0)
            {
                components.Add((Substances.All.Acanthite.GetHomogeneousReference(), acanthite));
            }
            if (sperrylite > 0)
            {
                components.Add((Substances.All.Sperrylite.GetHomogeneousReference(), sperrylite));
            }
            if (gold > 0)
            {
                components.Add((Substances.All.Gold.GetHomogeneousReference(), gold));
            }
            if (bauxite > 0)
            {
                components.Add((Substances.All.Bauxite.GetReference(), bauxite));
            }
            if (hematite > 0)
            {
                components.Add((Substances.All.Hematite.GetHomogeneousReference(), hematite));
            }
            if (magnetite > 0)
            {
                components.Add((Substances.All.Magnetite.GetHomogeneousReference(), magnetite));
            }
            if (ilmenite > 0)
            {
                components.Add((Substances.All.Ilmenite.GetHomogeneousReference(), ilmenite));
            }

            yield return new Material(
                (double)(crustMass / shape.Volume),
                crustMass,
                shape,
                null,
                components.ToArray());
        }

        private static double GetDensity(Rehydrator rehydrator, PlanetType planetType)
        {
            if (planetType == PlanetType.GasGiant)
            {
                // Relatively low chance of a "puffy" giant (Saturn-like, low-density).
                return rehydrator.NextDouble(7) <= 0.2
                    ? rehydrator.NextDouble(8, GiantSubMinDensity, GiantMinDensity)
                    : rehydrator.NextDouble(8, GiantMinDensity, GiantMaxDensity);
            }
            if (planetType == PlanetType.IceGiant)
            {
                // No "puffy" ice giants.
                return rehydrator.NextDouble(7, GiantMinDensity, GiantMaxDensity);
            }
            if (planetType == PlanetType.Iron)
            {
                return rehydrator.NextDouble(7, 5250, 8000);
            }
            if (PlanetType.AnyTerrestrial.HasFlag(planetType))
            {
                return rehydrator.NextDouble(8, 3750, DefaultTerrestrialMaxDensity);
            }

            return planetType switch
            {
                PlanetType.Dwarf => DensityForDwarf,
                PlanetType.LavaDwarf => 4000,
                PlanetType.RockyDwarf => 4000,
                _ => 2000,
            };
        }

        private static IEnumerable<IMaterial> GetMantle_Carbon(
            Rehydrator rehydrator,
            IShape planetShape,
            Number mantleProportion,
            Number crustProportion,
            Number planetMass,
            IShape coreShape,
            double coreTemp)
        {
            var mantleBoundaryDepth = planetShape.ContainingRadius * crustProportion;
            var mantleBoundaryTemp = (double)(mantleBoundaryDepth * new Number(115, -2));

            var innerTemp = coreTemp;

            var innerBoundary = planetShape.ContainingRadius;
            var mantleTotalDepth = (innerBoundary * mantleProportion) - coreShape.ContainingRadius;

            var mantleMass = planetMass * mantleProportion;

            // Molten silicon carbide lower mantle
            var lowerLayer = Number.Max(0, rehydrator.NextNumber(13, -Number.Deci, new Number(55, -2))) / mantleProportion;
            if (lowerLayer.IsPositive)
            {
                var lowerLayerMass = mantleMass * lowerLayer;

                var lowerLayerBoundary = innerBoundary + (mantleTotalDepth * mantleProportion);
                var lowerLayerShape = new HollowSphere(
                    innerBoundary,
                    lowerLayerBoundary,
                    planetShape.Position);
                innerBoundary = lowerLayerBoundary;

                var lowerLayerBoundaryTemp = innerTemp.Lerp(mantleBoundaryTemp, (double)lowerLayer);
                var lowerLayerTemp = (lowerLayerBoundaryTemp + innerTemp) / 2;
                innerTemp = lowerLayerTemp;

                yield return new Material(
                    Substances.All.SiliconCarbide.GetHomogeneousReference(),
                    (double)(lowerLayerMass / lowerLayerShape.Volume),
                    lowerLayerMass,
                    lowerLayerShape,
                    lowerLayerTemp);
            }

            // Diamond upper layer
            var upperLayerProportion = 1 - lowerLayer;

            var upperLayerMass = mantleMass * upperLayerProportion;

            var upperLayerBoundary = planetShape.ContainingRadius + mantleBoundaryDepth;
            var upperLayerShape = new HollowSphere(
                innerBoundary,
                upperLayerBoundary,
                planetShape.Position);

            var upperLayerTemp = (mantleBoundaryTemp + innerTemp) / 2;

            yield return new Material(
                Substances.All.Diamond.GetHomogeneousReference(),
                (double)(upperLayerMass / upperLayerShape.Volume),
                upperLayerMass,
                upperLayerShape,
                upperLayerTemp);
        }

        private static IEnumerable<IMaterial> GetMantle_Giant(
            Rehydrator rehydrator,
            IShape planetShape,
            Number mantleProportion,
            Number crustProportion,
            Number planetMass,
            IShape coreShape,
            double coreTemp)
        {
            var mantleBoundaryDepth = planetShape.ContainingRadius * crustProportion;
            var mantleBoundaryTemp = (double)mantleBoundaryDepth * 1.15;

            var innerTemp = coreTemp;

            var innerBoundary = planetShape.ContainingRadius;
            var mantleTotalDepth = (innerBoundary * mantleProportion) - coreShape.ContainingRadius;

            var mantleMass = planetMass * mantleProportion;

            // Metallic hydrogen lower mantle
            var metalH = Number.Max(Number.Zero, rehydrator.NextNumber(13, -Number.Deci, new Number(55, -2))) / mantleProportion;
            if (metalH.IsPositive)
            {
                var metalHMass = mantleMass * metalH;

                var metalHBoundary = innerBoundary + (mantleTotalDepth * mantleProportion);
                var metalHShape = new HollowSphere(
                    innerBoundary,
                    metalHBoundary,
                    planetShape.Position);
                innerBoundary = metalHBoundary;

                var metalHBoundaryTemp = innerTemp.Lerp(mantleBoundaryTemp, (double)metalH);
                var metalHTemp = (metalHBoundaryTemp + innerTemp) / 2;
                innerTemp = metalHTemp;

                yield return new Material(
                    Substances.All.MetallicHydrogen.GetHomogeneousReference(),
                    (double)(metalHMass / metalHShape.Volume),
                    metalHMass,
                    metalHShape,
                    metalHTemp);
            }

            // Supercritical fluid upper layer (blends seamlessly with lower atmosphere)
            var upperLayerProportion = 1 - metalH;

            var upperLayerMass = mantleMass * upperLayerProportion;

            var upperLayerBoundary = planetShape.ContainingRadius + mantleBoundaryDepth;
            var upperLayerShape = new HollowSphere(
                innerBoundary,
                upperLayerBoundary,
                planetShape.Position);

            var upperLayerTemp = (mantleBoundaryTemp + innerTemp) / 2;

            var water = (decimal)upperLayerProportion;
            var fluidH = water * 0.71m;
            water -= fluidH;
            var fluidHe = water * 0.24m;
            water -= fluidHe;
            var ne = rehydrator.NextDecimal(14) * water;
            water -= ne;
            var ch4 = rehydrator.NextDecimal(15) * water;
            water = Math.Max(0, water - ch4);
            var nh4 = rehydrator.NextDecimal(16) * water;
            water = Math.Max(0, water - nh4);
            var c2h6 = rehydrator.NextDecimal(17) * water;
            water = Math.Max(0, water - c2h6);

            var components = new List<(ISubstanceReference, decimal)>()
            {
                (Substances.All.Hydrogen.GetHomogeneousReference(), 0.71m),
                (Substances.All.Helium.GetHomogeneousReference(), 0.24m),
                (Substances.All.Neon.GetHomogeneousReference(), ne),
            };
            if (ch4 > 0)
            {
                components.Add((Substances.All.Methane.GetHomogeneousReference(), ch4));
            }
            if (nh4 > 0)
            {
                components.Add((Substances.All.Ammonia.GetHomogeneousReference(), nh4));
            }
            if (c2h6 > 0)
            {
                components.Add((Substances.All.Ethane.GetHomogeneousReference(), c2h6));
            }
            if (water > 0)
            {
                components.Add((Substances.All.Water.GetHomogeneousReference(), water));
            }

            yield return new Material(
                (double)(upperLayerMass / upperLayerShape.Volume),
                upperLayerMass,
                upperLayerShape,
                upperLayerTemp,
                components.ToArray());
        }

        private static IEnumerable<IMaterial> GetMantle_IceGiant(
            Rehydrator rehydrator,
            IShape planetShape,
            Number mantleProportion,
            Number crustProportion,
            Number planetMass,
            IShape coreShape,
            double coreTemp)
        {
            var mantleBoundaryDepth = planetShape.ContainingRadius * crustProportion;
            var mantleBoundaryTemp = (double)(mantleBoundaryDepth * new Number(115, -2));

            var innerTemp = coreTemp;

            var innerBoundary = planetShape.ContainingRadius;
            var mantleTotalDepth = (innerBoundary * mantleProportion) - coreShape.ContainingRadius;

            var mantleMass = planetMass * mantleProportion;

            var diamond = 1m;
            var water = Math.Max(0, rehydrator.NextDecimal(13) * diamond);
            diamond -= water;
            var nh4 = Math.Max(0, rehydrator.NextDecimal(14) * diamond);
            diamond -= nh4;
            var ch4 = Math.Max(0, rehydrator.NextDecimal(15) * diamond);
            diamond -= ch4;

            // Liquid diamond mantle
            if (diamond > 0)
            {
                var diamondMass = mantleMass * (Number)diamond;

                var diamondBoundary = innerBoundary + (mantleTotalDepth * mantleProportion);
                var diamondShape = new HollowSphere(
                    innerBoundary,
                    diamondBoundary,
                    planetShape.Position);
                innerBoundary = diamondBoundary;

                var diamondBoundaryTemp = innerTemp.Lerp(mantleBoundaryTemp, (double)diamond);
                var diamondTemp = (diamondBoundaryTemp + innerTemp) / 2;
                innerTemp = diamondTemp;

                yield return new Material(
                    Substances.All.Diamond.GetHomogeneousReference(),
                    (double)(diamondMass / diamondShape.Volume),
                    diamondMass,
                    diamondShape,
                    diamondTemp);
            }

            // Supercritical water-ammonia ocean layer (blends seamlessly with lower atmosphere)
            var upperLayerProportion = 1 - diamond;

            var upperLayerMass = mantleMass * (Number)upperLayerProportion;

            var upperLayerBoundary = planetShape.ContainingRadius + mantleBoundaryDepth;
            var upperLayerShape = new HollowSphere(
                innerBoundary,
                upperLayerBoundary,
                planetShape.Position);

            var upperLayerTemp = (mantleBoundaryTemp + innerTemp) / 2;

            var components = new List<(ISubstanceReference, decimal)>();
            if (ch4 > 0 || nh4 > 0)
            {
                components.Add((Substances.All.Water.GetHomogeneousReference(), water));
                if (ch4 > 0)
                {
                    components.Add((Substances.All.Methane.GetHomogeneousReference(), ch4));
                }
                if (nh4 > 0)
                {
                    components.Add((Substances.All.Ammonia.GetHomogeneousReference(), nh4));
                }
            }
            else
            {
                components.Add((Substances.All.Water.GetHomogeneousReference(), 1));
            }

            yield return new Material(
                (double)(upperLayerMass / upperLayerShape.Volume),
                upperLayerMass,
                upperLayerShape,
                upperLayerTemp,
                components.ToArray());
        }

        private static Number GetMass(PlanetType planetType, Number semiMajorAxis, Number? maxMass, double gravity, IShape? shape)
        {
            var min = Number.Zero;
            if (!PlanetType.AnyDwarf.HasFlag(planetType))
            {
                // Stern-Levison parameter for neighborhood-clearing used to determined minimum mass
                // at which the planet would be able to do so at this orbital distance. We set the
                // minimum at two orders of magnitude more than this (planets in our solar system
                // all have masses above 5 orders of magnitude more). Note that since lambda is
                // proportional to the square of mass, it is multiplied by 10 to obtain a difference
                // of 2 orders of magnitude, rather than by 100.
                var sternLevisonLambdaMass = (Number.Pow(semiMajorAxis, new Number(15, -1)) / new Number(2.5, -28)).Sqrt();
                min = Number.Max(min, sternLevisonLambdaMass * 10);
                if (min > maxMass && maxMass.HasValue)
                {
                    min = maxMass.Value; // sanity check; may result in a "planet" which *can't* clear its neighborhood
                }
            }

            var mass = shape is null ? Number.Zero : gravity * shape.ContainingRadius * shape.ContainingRadius / ScienceConstants.G;
            return Number.Max(min, maxMass.HasValue ? Number.Min(maxMass.Value, mass) : mass);
        }

        private static Number GetMaxMassForType(PlanetType planetType) => planetType switch
        {
            PlanetType.Dwarf => _DwarfMaxMassForType,
            PlanetType.LavaDwarf => _DwarfMaxMassForType,
            PlanetType.RockyDwarf => _DwarfMaxMassForType,
            PlanetType.GasGiant => _GiantMaxMassForType,
            PlanetType.IceGiant => _GiantMaxMassForType,
            _ => _TerrestrialMaxMassForType,
        };

        private static Number GetRadiusForMass(Number density, Number mass) => (mass / density / MathConstants.FourThirdsPI).CubeRoot();

        private int AddResource(ISubstanceReference substance, decimal proportion, bool isVein, bool isPerturbation = false, int? seed = null)
        {
            var resource = new Resource(substance, proportion, isVein, isPerturbation, seed);
            _resources.Add(resource);
            return resource.Seed;
        }

        private void AddResources(IEnumerable<(ISubstanceReference substance, decimal proportion, bool vein)> resources)
        {
            foreach (var (substance, proportion, vein) in resources)
            {
                AddResource(substance, proportion, vein);
            }
        }

        private double CalculateGasPhaseMix(
            Rehydrator rehydrator,
            HomogeneousReference substance,
            double surfaceTemp,
            double adjustedAtmosphericPressure)
        {
            var proportionInHydrosphere = Hydrosphere.GetProportion(substance);
            var water = Substances.All.Water.GetHomogeneousReference();
            var isWater = substance.Equals(water);
            if (isWater)
            {
                proportionInHydrosphere = Hydrosphere.GetProportion(x =>
                    x.Equals(Substances.All.Seawater.GetHomogeneousReference())
                    || x.Equals(water));
            }

            var vaporProportion = Atmosphere.Material.GetProportion(substance);

            var sub = substance.Homogeneous;
            var vaporPressure = sub.GetVaporPressure(surfaceTemp) ?? 0;

            if (surfaceTemp < sub.AntoineMinimumTemperature
                || (surfaceTemp <= sub.AntoineMaximumTemperature
                && Atmosphere.AtmosphericPressure > vaporPressure))
            {
                CondenseAtmosphericComponent(
                    sub,
                    surfaceTemp,
                    proportionInHydrosphere,
                    vaporProportion,
                    vaporPressure,
                    ref adjustedAtmosphericPressure);
            }
            // This indicates that the chemical will fully boil off.
            else if (proportionInHydrosphere > 0)
            {
                EvaporateAtmosphericComponent(
                    rehydrator,
                    sub,
                    proportionInHydrosphere,
                    vaporProportion,
                    ref adjustedAtmosphericPressure);
            }

            if (isWater && _planetParams?.EarthlikeAtmosphere != true)
            {
                CheckCO2Reduction(rehydrator, vaporPressure);
            }

            return adjustedAtmosphericPressure;
        }

        private double CalculatePhases(Rehydrator rehydrator, int counter, double adjustedAtmosphericPressure)
        {
            var surfaceTemp = GetAverageSurfaceTemperature();

            // Despite the theoretical possibility of an atmosphere cold enough to precipitate some
            // of the noble gases, or hydrogen, they are ignored and presumed to exist always as
            // trace atmospheric gases, never surface liquids or ices, or in large enough quantities
            // to form precipitation.

            var methane = Substances.All.Methane.GetHomogeneousReference();
            adjustedAtmosphericPressure = CalculateGasPhaseMix(rehydrator, methane, surfaceTemp, adjustedAtmosphericPressure);

            var carbonMonoxide = Substances.All.CarbonMonoxide.GetHomogeneousReference();
            adjustedAtmosphericPressure = CalculateGasPhaseMix(rehydrator, carbonMonoxide, surfaceTemp, adjustedAtmosphericPressure);

            var carbonDioxide = Substances.All.CarbonDioxide.GetHomogeneousReference();
            adjustedAtmosphericPressure = CalculateGasPhaseMix(rehydrator, carbonDioxide, surfaceTemp, adjustedAtmosphericPressure);

            var nitrogen = Substances.All.Nitrogen.GetHomogeneousReference();
            adjustedAtmosphericPressure = CalculateGasPhaseMix(rehydrator, nitrogen, surfaceTemp, adjustedAtmosphericPressure);

            var oxygen = Substances.All.Oxygen.GetHomogeneousReference();
            adjustedAtmosphericPressure = CalculateGasPhaseMix(rehydrator, oxygen, surfaceTemp, adjustedAtmosphericPressure);

            // No need to check for ozone, since it is only added to atmospheres on planets with
            // liquid surface water, which means temperatures too high for liquid or solid ozone.

            var sulphurDioxide = Substances.All.SulphurDioxide.GetHomogeneousReference();
            adjustedAtmosphericPressure = CalculateGasPhaseMix(rehydrator, sulphurDioxide, surfaceTemp, adjustedAtmosphericPressure);

            // Water is handled differently, since the planet may already have surface water.
            if (counter > 0) // Not performed on first pass, since it was already done.
            {
                var water = Substances.All.Water.GetHomogeneousReference();
                var seawater = Substances.All.Seawater.GetHomogeneousReference();
                if (Hydrosphere.Contains(water)
                    || Hydrosphere.Contains(seawater)
                    || Atmosphere.Material.Contains(water))
                {
                    adjustedAtmosphericPressure = CalculateGasPhaseMix(rehydrator, water, surfaceTemp, adjustedAtmosphericPressure);
                }
            }

            // Ices and clouds significantly impact albedo.
            var pressure = adjustedAtmosphericPressure;
            var iceAmount = (double)Math.Min(1,
                Hydrosphere.GetSurface()?.GetOverallDoubleValue(x => (double)x.SeparateByPhase(surfaceTemp, pressure, PhaseType.Solid).First().proportion) ?? 0);
            var cloudCover = Atmosphere.AtmosphericPressure
                * (double)Atmosphere.Material.GetOverallDoubleValue(x => (double)x.SeparateByPhase(surfaceTemp, pressure, PhaseType.Solid | PhaseType.Liquid).First().proportion) / 100;
            var reflectiveSurface = Math.Max(iceAmount, cloudCover);
            if (_planetParams.HasValue && _planetParams.Value.Albedo.HasValue)
            {
                _surfaceAlbedo = ((Albedo - (0.9 * reflectiveSurface)) / (1 - reflectiveSurface)).Clamp(0, 1);
            }
            else
            {
                Albedo = ((_surfaceAlbedo * (1 - reflectiveSurface)) + (0.9 * reflectiveSurface)).Clamp(0, 1);
                Atmosphere.ResetTemperatureDependentProperties(this);

                // An albedo change might significantly alter surface temperature, which may require a
                // re-calculation (but not too many). 5K is used as the threshold for re-calculation,
                // which may lead to some inaccuracies, but should avoid over-repetition for small changes.
                if (counter < 10 && Math.Abs(surfaceTemp - GetAverageSurfaceTemperature()) > 5)
                {
                    adjustedAtmosphericPressure = CalculatePhases(rehydrator, counter + 1, adjustedAtmosphericPressure);
                }
            }

            return adjustedAtmosphericPressure;
        }

        private void CheckCO2Reduction(Rehydrator rehydrator, double vaporPressure)
        {
            // At least 1% humidity leads to a reduction of CO2 to a trace gas, by a presumed
            // carbon-silicate cycle.

            var water = Substances.All.Water.GetHomogeneousReference();
            var air = Atmosphere.Material.GetCore();
            if ((double)(air?.GetProportion(water) ?? 0) * Atmosphere.AtmosphericPressure >= 0.01 * vaporPressure)
            {
                var carbonDioxide = Substances.All.CarbonDioxide.GetHomogeneousReference();
                var co2 = air?.GetProportion(carbonDioxide) ?? 0;
                if (co2 >= 1e-3m) // reduce CO2 if not already trace
                {
                    co2 = rehydrator.NextDecimal(62, 15e-6m, 0.001m);

                    // Replace most of the CO2 with inert gases.
                    var nitrogen = Substances.All.Nitrogen.GetHomogeneousReference();
                    var n2 = Atmosphere.Material.GetProportion(nitrogen) + Atmosphere.Material.GetProportion(carbonDioxide) - co2;
                    Atmosphere.Material.AddConstituent(carbonDioxide, co2);

                    // Some portion of the N2 may be Ar instead.
                    var argon = Substances.All.Argon.GetHomogeneousReference();
                    var ar = Math.Max(Atmosphere.Material.GetProportion(argon), n2 * rehydrator.NextDecimal(63, -0.02m, 0.04m));
                    Atmosphere.Material.AddConstituent(argon, ar);
                    n2 -= ar;

                    // An even smaller fraction may be Kr.
                    var krypton = Substances.All.Krypton.GetHomogeneousReference();
                    var kr = Math.Max(Atmosphere.Material.GetProportion(krypton), n2 * rehydrator.NextDecimal(64, -25e-5m, 0.0005m));
                    Atmosphere.Material.AddConstituent(krypton, kr);
                    n2 -= kr;

                    // An even smaller fraction may be Xe or Ne.
                    var xenon = Substances.All.Xenon.GetHomogeneousReference();
                    var xe = Math.Max(Atmosphere.Material.GetProportion(xenon), n2 * rehydrator.NextDecimal(65, -18e-6m, 35e-6m));
                    Atmosphere.Material.AddConstituent(xenon, xe);
                    n2 -= xe;

                    var neon = Substances.All.Neon.GetHomogeneousReference();
                    var ne = Math.Max(Atmosphere.Material.GetProportion(neon), n2 * rehydrator.NextDecimal(66, -18e-6m, 35e-6m));
                    Atmosphere.Material.AddConstituent(neon, ne);
                    n2 -= ne;

                    Atmosphere.Material.AddConstituent(nitrogen, n2);

                    Atmosphere.ResetGreenhouseFactor();
                    ResetCachedTemperatures();
                }
            }
        }

        private void CondenseAtmosphericComponent(
            IHomogeneous substance,
            double surfaceTemp,
            decimal proportionInHydrosphere,
            decimal vaporProportion,
            double vaporPressure,
            ref double adjustedAtmosphericPressure)
        {
            var water = Substances.All.Water.GetHomogeneousReference();

            // Fully precipitate out of the atmosphere when below the freezing point.
            if (!substance.MeltingPoint.HasValue || surfaceTemp < substance.MeltingPoint.Value)
            {
                vaporProportion = 0;

                Atmosphere.Material.RemoveConstituent(substance);

                if (Atmosphere.Material.Constituents.Count == 0)
                {
                    adjustedAtmosphericPressure = 0;
                }

                if (substance.Equals(water))
                {
                    Atmosphere.ResetWater();
                }
            }
            else
            {
                // Adjust vapor present in the atmosphere based on the vapor pressure.
                var pressureRatio = (vaporPressure / Atmosphere.AtmosphericPressure).Clamp(0, 1);
                if (substance.Equals(water) && _planetParams?.WaterVaporRatio.HasValue == true)
                {
                    vaporProportion = _planetParams!.Value.WaterVaporRatio!.Value;
                }
                else
                {
                    // This would represent 100% humidity. Since this is the case, in principle, only at the
                    // surface of bodies of liquid, and should decrease exponentially with altitude, an
                    // approximation of 25% average humidity overall is used.
                    vaporProportion = (proportionInHydrosphere + vaporProportion) * (decimal)pressureRatio;
                    vaporProportion *= 0.25m;
                }
                if (vaporProportion > 0)
                {
                    var previousGasFraction = 0m;
                    var gasFraction = vaporProportion;
                    Atmosphere.Material.AddConstituent(substance, vaporProportion);

                    if (substance.Equals(water))
                    {
                        Atmosphere.ResetWater();

                        // For water, also add a corresponding amount of oxygen, if it's not already present.
                        if (_planetParams?.EarthlikeAtmosphere != true && PlanetType != PlanetType.Carbon)
                        {
                            var oxygen = Substances.All.Oxygen.GetHomogeneousReference();
                            var o2 = Atmosphere.Material.GetProportion(oxygen);
                            previousGasFraction += o2;
                            o2 = Math.Max(o2, vaporProportion * 0.0001m);
                            gasFraction += o2;
                            Atmosphere.Material.AddConstituent(oxygen, o2);
                        }
                    }

                    adjustedAtmosphericPressure += adjustedAtmosphericPressure * (double)(gasFraction - previousGasFraction);

                    // At least some precipitation will occur. Ensure a troposphere.
                    Atmosphere.DifferentiateTroposphere();
                }
            }

            var hydro = proportionInHydrosphere;
            var hydrosphereAtmosphereRatio = Atmosphere.Material.IsEmpty ? 0 : GetHydrosphereAtmosphereRatio();
            hydro = Math.Max(hydro, hydrosphereAtmosphereRatio <= 0 ? vaporProportion : vaporProportion / hydrosphereAtmosphereRatio);
            if (hydro > proportionInHydrosphere)
            {
                Hydrosphere.GetSurface().AddConstituent(substance, hydro);
            }
        }

        private List<Planetoid> Configure(
            CosmicLocation? parent,
            List<Star> stars,
            Star? star,
            Vector3 position,
            bool satellite,
            OrbitalParameters? orbit,
            uint? seed = null)
        {
            var rehydrator = GetRehydrator(seed);

            IsInhospitable = stars.Any(x => !x.IsHospitable);

            double eccentricity;
            if (_planetParams?.Eccentricity.HasValue == true)
            {
                eccentricity = _planetParams!.Value.Eccentricity!.Value;
            }
            else if (orbit.HasValue)
            {
                eccentricity = orbit.Value.Circular ? 0 : orbit.Value.Eccentricity;
            }
            else if (PlanetType == PlanetType.Comet)
            {
                eccentricity = rehydrator.NextDouble(33);
            }
            else if (IsAsteroid)
            {
                eccentricity = rehydrator.NextDouble(33, 0.4);
            }
            else
            {
                eccentricity = rehydrator.PositiveNormalDistributionSample(33, 0, 0.05);
            }

            Number semiMajorAxis;
            if (_planetParams?.RevolutionPeriod.HasValue == true
                && (orbit.HasValue || star is not null))
            {
                var orbitedMass = orbit.HasValue ? orbit.Value.OrbitedMass : star?.Mass;
                semiMajorAxis = Space.Orbit.GetSemiMajorAxisForPeriod(Mass, orbitedMass!.Value, _planetParams!.Value.RevolutionPeriod!.Value);
                position = position.IsZero()
                    ? Vector3.UnitX * semiMajorAxis
                    : position.Normalize() * semiMajorAxis;
            }
            else if (orbit.HasValue)
            {
                var periapsis = orbit.Value.Circular ? position.Distance(orbit.Value.OrbitedPosition) : orbit.Value.Periapsis;
                semiMajorAxis = eccentricity == 1
                    ? periapsis
                    : periapsis * (1 + eccentricity) / (1 - (eccentricity * eccentricity));
                position = position.IsZero()
                    ? Vector3.UnitX * periapsis
                    : position.Normalize() * periapsis;
            }
            else
            {
                var distance = star is null
                    ? position.Length()
                    : star.Position.Distance(position);
                semiMajorAxis = distance * ((1 + eccentricity) / (1 - eccentricity));
            }

            ReconstituteMaterial(
                rehydrator,
                position,
                parent?.Material.Temperature ?? UniverseAmbientTemperature,
                semiMajorAxis);

            if (_planetParams?.RotationalPeriod.HasValue == true)
            {
                RotationalPeriod = Number.Max(0, _planetParams!.Value.RotationalPeriod!.Value);
            }
            else
            {
                // Check for tidal locking.
                var rotationalPeriodSet = false;
                if (orbit.HasValue)
                {
                    // Invent an orbit age. Precision isn't important here, and some inaccuracy and
                    // inconsistency between satellites is desirable. The age of the Solar system is used
                    // as an arbitrary norm.
                    var years = rehydrator.LogisticDistributionSample(34, 0, 1) * new Number(4.6, 9);

                    var rigidity = PlanetType == PlanetType.Comet ? new Number(4, 9) : new Number(3, 10);
                    if (Number.Pow(years / new Number(6, 11)
                        * Mass
                        * orbit.Value.OrbitedMass.Square()
                        / (Shape.ContainingRadius * rigidity)
                        , Number.One / new Number(6)) >= semiMajorAxis)
                    {
                        RotationalPeriod = MathConstants.TwoPI * Number.Sqrt(semiMajorAxis.Cube() / (ScienceConstants.G * (orbit.Value.OrbitedMass + Mass)));
                        rotationalPeriodSet = true;
                    }
                }
                if (!rotationalPeriodSet)
                {
                    var rotationalPeriodLimit = IsTerrestrial ? new Number(6500000) : new Number(100000);
                    if (rehydrator.NextDouble(35) <= 0.05) // low chance of an extreme period
                    {
                        RotationalPeriod = rehydrator.NextNumber(
                            36,
                            rotationalPeriodLimit,
                            IsTerrestrial ? new Number(22000000) : new Number(1100000));
                    }
                    else
                    {
                        RotationalPeriod = rehydrator.NextNumber(
                            36,
                            IsTerrestrial ? new Number(40000) : new Number(8000),
                            rotationalPeriodLimit);
                    }
                }
            }

            GenerateOrbit(
                rehydrator,
                orbit,
                star,
                eccentricity,
                semiMajorAxis);

            if (_planetParams?.AxialTilt.HasValue == true)
            {
                var axialTilt = _planetParams!.Value.AxialTilt!.Value;
                if (Orbit.HasValue)
                {
                    axialTilt += Orbit.Value.Inclination;
                }
                while (axialTilt > Math.PI)
                {
                    axialTilt -= Math.PI;
                }
                while (axialTilt < 0)
                {
                    axialTilt += Math.PI;
                }
                AngleOfRotation = axialTilt;
            }
            else if (rehydrator.NextDouble(42) <= 0.2) // low chance of an extreme tilt
            {
                AngleOfRotation = rehydrator.NextDouble(43, MathAndScience.Constants.Doubles.MathConstants.QuarterPI, Math.PI);
            }
            else
            {
                AngleOfRotation = rehydrator.NextDouble(43, MathAndScience.Constants.Doubles.MathConstants.QuarterPI);
            }
            SetAxis();

            SetTemperatures(stars);

            var surfaceTemp = ReconstituteHydrosphere(rehydrator);

            if (star is not null
                && (_planetParams?.SurfaceTemperature.HasValue == true
                || _habitabilityRequirements?.MinimumTemperature.HasValue == true
                || _habitabilityRequirements?.MaximumTemperature.HasValue == true))
            {
                CorrectSurfaceTemperature(rehydrator, stars, star, surfaceTemp);
            }
            else
            {
                GenerateAtmosphere(rehydrator);
            }

            GenerateResources(rehydrator);

            var index = SetRings(rehydrator);

            return satellite
                ? new List<Planetoid>()
                : GenerateSatellites(rehydrator, parent, stars, index);
        }

        private void CorrectSurfaceTemperature(
            Rehydrator rehydrator,
            List<Star> stars,
            Star star,
            double surfaceTemp)
        {
            // Convert the target average surface temperature to an estimated target equatorial
            // surface temperature, for which orbit/luminosity requirements can be calculated.
            var targetEquatorialTemp = surfaceTemp * 1.06;
            // Use the typical average elevation to determine average surface
            // temperature, since the average temperature at sea level is not the same
            // as the average overall surface temperature.
            var avgElevation = MaxElevation * 0.04;
            var totalTargetEffectiveTemp = targetEquatorialTemp + (avgElevation * LapseRateDry);

            var greenhouseEffect = 30.0; // naïve initial guess, corrected if possible with param values
            if (_planetParams?.AtmosphericPressure.HasValue == true
                && (_planetParams?.WaterVaporRatio.HasValue == true
                || _planetParams?.WaterRatio.HasValue == true))
            {
                var pressure = _planetParams!.Value.AtmosphericPressure!.Value;

                var vaporRatio = _planetParams?.WaterVaporRatio.HasValue == true
                    ? (double)_planetParams!.Value.WaterVaporRatio!.Value
                    : (Substances.All.Water.GetVaporPressure(totalTargetEffectiveTemp) ?? 0) / pressure * 0.25;
                greenhouseEffect = GetGreenhouseEffect(
                    GetInsolationFactor(Atmosphere.GetAtmosphericMass(this, pressure), 0), // scale height will be ignored since this isn't a polar calculation
                    Atmosphere.GetGreenhouseFactor(Substances.All.Water.GreenhousePotential * vaporRatio, pressure));
            }
            var targetEffectiveTemp = totalTargetEffectiveTemp - greenhouseEffect;

            var currentTargetTemp = targetEffectiveTemp;

            // Due to atmospheric effects, the target is likely to be missed considerably on the
            // first attempt, since the calculations for orbit/luminosity will not be able to
            // account for greenhouse warming. By determining the degree of over/undershoot, the
            // target can be adjusted. This is repeated until the real target is approached to
            // within an acceptable tolerance, but not to excess.
            var count = 0;
            double prevDelta;
            var delta = 0.0;
            var originalHydrosphere = Hydrosphere.GetClone();
            var newAtmosphere = true;
            do
            {
                prevDelta = delta;

                // Orbital distance averaged over time (mean anomaly) = semi-major axis * (1 + eccentricity^2 / 2).
                // This allows calculation of the correct distance/orbit for an average
                // orbital temperature (rather than the temperature at the current position).
                if (_planetParams?.RevolutionPeriod.HasValue == true)
                {
                    // Do not attempt a correction on the first pass; the albedo delta due to
                    // atmospheric effects will not yet have a meaningful value.
                    if (Albedo != _surfaceAlbedo)
                    {
                        var albedoDelta = Albedo - _surfaceAlbedo;
                        _surfaceAlbedo = GetSurfaceAlbedoForTemperature(star, currentTargetTemp - Temperature);
                        Albedo = _surfaceAlbedo + albedoDelta;
                    }
                }
                else
                {
                    var semiMajorAxis = GetDistanceForTemperature(star, currentTargetTemp - Temperature) / (1 + (Orbit!.Value.Eccentricity * Orbit.Value.Eccentricity / 2));
                    GenerateOrbit(rehydrator, star, Orbit.Value.Eccentricity, semiMajorAxis, Orbit.Value.TrueAnomaly);
                }
                ResetAllCachedTemperatures(stars);

                // Reset hydrosphere to negate effects of runaway evaporation or freezing.
                Hydrosphere = originalHydrosphere;

                if (newAtmosphere)
                {
                    GenerateAtmosphere(rehydrator);
                    newAtmosphere = false;
                }

                if (_planetParams?.SurfaceTemperature.HasValue == true)
                {
                    delta = targetEquatorialTemp - GetTemperatureAtElevation(GetAverageSurfaceTemperature(), avgElevation);
                }
                else if (_habitabilityRequirements.HasValue)
                {
                    var tooCold = false;
                    if (_habitabilityRequirements.Value.MinimumTemperature.HasValue)
                    {
                        var coolestEquatorialTemp = GetMinEquatorTemperature();
                        if (coolestEquatorialTemp < _habitabilityRequirements.Value.MinimumTemperature)
                        {
                            delta = _habitabilityRequirements.Value.MaximumTemperature.HasValue
                                ? _habitabilityRequirements.Value.MaximumTemperature.Value - coolestEquatorialTemp
                                : _habitabilityRequirements.Value.MinimumTemperature.Value - coolestEquatorialTemp;
                            tooCold = true;
                        }
                    }
                    if (!tooCold && _habitabilityRequirements.Value.MaximumTemperature.HasValue)
                    {
                        var warmestPolarTemp = GetMaxPolarTemperature();
                        if (warmestPolarTemp > _habitabilityRequirements.Value.MaximumTemperature)
                        {
                            delta = _habitabilityRequirements!.Value.MaximumTemperature.Value - warmestPolarTemp;
                        }
                    }
                }

                // Avoid oscillation by reducing deltas which bounce around zero.
                var deltaAdjustment = prevDelta != 0 && Math.Sign(delta) != Math.Sign(prevDelta)
                    ? delta / 2
                    : 0;
                // If the corrections are resulting in a runaway drift in the wrong direction,
                // reset by deleting the atmosphere and targeting the original temp; do not
                // reset the count, to prevent this from repeating indefinitely.
                if (prevDelta != 0
                    && (delta >= 0) == (prevDelta >= 0)
                    && Math.Abs(delta) > Math.Abs(prevDelta))
                {
                    newAtmosphere = true;
                    ResetCachedTemperatures();
                    currentTargetTemp = targetEffectiveTemp;
                }
                else
                {
                    currentTargetTemp = Math.Max(0, currentTargetTemp + (delta - deltaAdjustment));
                }

                count++;
            } while (count < 10 && Math.Abs(delta) > 0.5);
        }

        private void EvaporateAtmosphericComponent(
            Rehydrator rehydrator,
            IHomogeneous substance,
            decimal hydrosphereProportion,
            decimal vaporProportion,
            ref double adjustedAtmosphericPressure)
        {
            if (hydrosphereProportion <= 0)
            {
                return;
            }

            var water = Substances.All.Water.GetHomogeneousReference();
            if (substance.Equals(water))
            {
                Hydrosphere = Hydrosphere.GetHomogenized();
                Atmosphere.ResetWater();
            }

            var gasProportion = Atmosphere.Material.Mass.IsZero ? 0 : hydrosphereProportion * GetHydrosphereAtmosphereRatio();
            var previousGasProportion = vaporProportion;

            Hydrosphere.GetSurface().RemoveConstituent(substance);
            if (Hydrosphere.IsEmpty)
            {
                SeaLevel = -MaxElevation * 1.1;
            }

            if (substance.Equals(water))
            {
                var seawater = Substances.All.Seawater.GetHomogeneousReference();
                Hydrosphere.GetSurface().RemoveConstituent(seawater);
                if (Hydrosphere.IsEmpty)
                {
                    SeaLevel = -MaxElevation * 1.1;
                }

                // It is presumed that photodissociation will eventually reduce the amount of water
                // vapor to a trace gas (the H2 will be lost due to atmospheric escape, and the
                // oxygen will be lost to surface oxidation).
                var waterVapor = Math.Min(gasProportion, rehydrator.NextDecimal(61, 0.001m));
                gasProportion = waterVapor;

                var oxygen = Substances.All.Oxygen.GetHomogeneousReference();
                previousGasProportion += Atmosphere.Material.GetProportion(oxygen);
                var o2 = gasProportion * 0.0001m;
                gasProportion += o2;

                Atmosphere.Material.AddConstituent(substance, waterVapor);
                if (PlanetType != PlanetType.Carbon)
                {
                    Atmosphere.Material.AddConstituent(oxygen, o2);
                }
            }
            else
            {
                Atmosphere.Material.AddConstituent(substance, gasProportion);
            }

            adjustedAtmosphericPressure += adjustedAtmosphericPressure * (double)(gasProportion - previousGasProportion);
        }

        private void FractionHydrophere(double temperature)
        {
            if (Hydrosphere.IsEmpty)
            {
                return;
            }

            var seawater = Substances.All.Seawater.GetHomogeneousReference();
            var water = Substances.All.Water.GetHomogeneousReference();

            var seawaterProportion = Hydrosphere.GetProportion(seawater);
            var waterProportion = 1 - seawaterProportion;

            var depth = SeaLevel + (MaxElevation / 2);
            if (depth > 0)
            {
                var stateTop = Substances.All.Seawater.MeltingPoint <= temperature
                    ? PhaseType.Liquid
                    : PhaseType.Solid;

                double tempBottom;
                if (depth > 1000)
                {
                    tempBottom = 277;
                }
                else if (depth < 200)
                {
                    tempBottom = temperature;
                }
                else
                {
                    tempBottom = temperature.Lerp(277, (depth - 200) / 800);
                }

                var stateBottom = Substances.All.Seawater.MeltingPoint <= tempBottom
                    ? PhaseType.Liquid
                    : PhaseType.Solid;

                // subsurface ocean indicated
                if (stateTop != stateBottom)
                {
                    var topProportion = 1000 / depth;
                    var bottomProportion = 1 - topProportion;
                    var bottomOuterRadius = Hydrosphere.Shape.ContainingRadius * bottomProportion;
                    Hydrosphere = new Composite(
                        new IMaterial[]
                        {
                        new Material(
                            Hydrosphere.Density,
                            Hydrosphere.Mass * bottomProportion,
                            new HollowSphere(Material.Shape.ContainingRadius, bottomOuterRadius, Material.Shape.Position),
                            277,
                            (seawater, seawaterProportion),
                            (water, waterProportion)),
                        new Material(
                            Hydrosphere.Density,
                            Hydrosphere.Mass * topProportion,
                            new HollowSphere(bottomOuterRadius, Hydrosphere.Shape.ContainingRadius, Material.Shape.Position),
                            (277 + temperature) / 2,
                            (seawater, seawaterProportion),
                            (water, waterProportion)),
                        },
                        Hydrosphere.Shape,
                        Hydrosphere.Density,
                        Hydrosphere.Mass);
                    return;
                }
            }

            var avgDepth = (double)(Hydrosphere.Shape.ContainingRadius - Material.Shape.ContainingRadius) / 2;
            double avgTemp;
            if (avgDepth > 1000)
            {
                avgTemp = 277;
            }
            else if (avgDepth < 200)
            {
                avgTemp = temperature;
            }
            else
            {
                avgTemp = temperature.Lerp(277, (avgDepth - 200) / 800);
            }

            Hydrosphere = new Material(
                Hydrosphere.Density,
                Hydrosphere.Mass,
                Hydrosphere.Shape,
                avgTemp,
                (seawater, seawaterProportion),
                (water, waterProportion));
        }

        private void GenerateAtmosphere_Dwarf(Rehydrator rehydrator)
        {
            // Atmosphere is based solely on the volatile ices present.
            var crust = Material.GetSurface();

            var water = crust.GetProportion(Substances.All.Water.GetHomogeneousReference());
            var anyIces = water > 0;

            var n2 = crust.GetProportion(Substances.All.Nitrogen.GetHomogeneousReference());
            anyIces &= n2 > 0;

            var ch4 = crust.GetProportion(Substances.All.Methane.GetHomogeneousReference());
            anyIces &= ch4 > 0;

            var co = crust.GetProportion(Substances.All.CarbonMonoxide.GetHomogeneousReference());
            anyIces &= co > 0;

            var co2 = crust.GetProportion(Substances.All.CarbonDioxide.GetHomogeneousReference());
            anyIces &= co2 > 0;

            var nh3 = crust.GetProportion(Substances.All.Ammonia.GetHomogeneousReference());
            anyIces &= nh3 > 0;

            if (!anyIces)
            {
                return;
            }

            var components = new List<(ISubstanceReference, decimal)>();
            if (water > 0)
            {
                components.Add((Substances.All.Water.GetHomogeneousReference(), water));
            }
            if (n2 > 0)
            {
                components.Add((Substances.All.Nitrogen.GetHomogeneousReference(), n2));
            }
            if (ch4 > 0)
            {
                components.Add((Substances.All.Methane.GetHomogeneousReference(), ch4));
            }
            if (co > 0)
            {
                components.Add((Substances.All.CarbonMonoxide.GetHomogeneousReference(), co));
            }
            if (co2 > 0)
            {
                components.Add((Substances.All.CarbonDioxide.GetHomogeneousReference(), co2));
            }
            if (nh3 > 0)
            {
                components.Add((Substances.All.Ammonia.GetHomogeneousReference(), nh3));
            }
            Atmosphere = new Atmosphere(this, rehydrator.NextDouble(47, 2.5), components.ToArray());

            var ice = Atmosphere.Material.GetOverallDoubleValue(x =>
                (double)x.SeparateByPhase(
                    Material.Temperature ?? 0,
                    Atmosphere.AtmosphericPressure,
                    PhaseType.Solid)
                .First().proportion);
            if (_planetParams.HasValue && _planetParams.Value.Albedo.HasValue)
            {
                _surfaceAlbedo = ((Albedo - (0.9 * ice)) / (1 - ice)).Clamp(0, 1);
            }
            else
            {
                Albedo = ((_surfaceAlbedo * (1 - ice)) + (0.9 * ice)).Clamp(0, 1);
            }
        }

        private void GenerateAtmosphere_Giant(Rehydrator rehydrator)
        {
            var trace = rehydrator.NextDecimal(47, 0.025m);

            var h = rehydrator.NextDecimal(48, 0.75m, 0.97m);
            var he = 1 - h - trace;

            var ch4 = rehydrator.NextDecimal(49) * trace;
            trace -= ch4;

            // 50% chance not to have each of these components
            var c2h6 = Math.Max(0, rehydrator.NextDecimal(50, -0.5m, 0.5m));
            var traceTotal = c2h6;
            var nh3 = Math.Max(0, rehydrator.NextDecimal(51, -0.5m, 0.5m));
            traceTotal += nh3;
            var waterVapor = Math.Max(0, rehydrator.NextDecimal(52, -0.5m, 0.5m));
            traceTotal += waterVapor;

            var nh4sh = rehydrator.NextDecimal(53);
            traceTotal += nh4sh;

            var ratio = trace / traceTotal;
            c2h6 *= ratio;
            nh3 *= ratio;
            waterVapor *= ratio;
            nh4sh *= ratio;

            var components = new List<(ISubstanceReference, decimal)>()
                {
                    (Substances.All.Hydrogen.GetHomogeneousReference(), h),
                    (Substances.All.Helium.GetHomogeneousReference(), he),
                    (Substances.All.Methane.GetHomogeneousReference(), ch4),
                };
            if (c2h6 > 0)
            {
                components.Add((Substances.All.Ethane.GetHomogeneousReference(), c2h6));
            }
            if (nh3 > 0)
            {
                components.Add((Substances.All.Ammonia.GetHomogeneousReference(), nh3));
            }
            if (waterVapor > 0)
            {
                components.Add((Substances.All.Water.GetHomogeneousReference(), waterVapor));
            }
            if (nh4sh > 0)
            {
                components.Add((Substances.All.AmmoniumHydrosulfide.GetHomogeneousReference(), nh4sh));
            }
            Atmosphere = new Atmosphere(this, 1000, components.ToArray());
        }

        private void GenerateAtmosphere_SmallBody(Rehydrator rehydrator)
        {
            var dust = 1.0m;

            var water = rehydrator.NextDecimal(47, 0.75m, 0.9m);
            dust -= water;

            var co = rehydrator.NextDecimal(48, 0.05m, 0.15m);
            dust -= co;

            if (dust < 0)
            {
                water -= 0.1m;
                dust += 0.1m;
            }

            var co2 = rehydrator.NextDecimal(49, 0.01m);
            dust -= co2;

            var nh3 = rehydrator.NextDecimal(50, 0.01m);
            dust -= nh3;

            var ch4 = rehydrator.NextDecimal(51, 0.01m);
            dust -= ch4;

            var h2s = rehydrator.NextDecimal(52, 0.01m);
            dust -= h2s;

            var so2 = rehydrator.NextDecimal(53, 0.001m);
            dust -= so2;

            Atmosphere = new Atmosphere(
                this,
                1e-8,
                (Substances.All.Water.GetHomogeneousReference(), water),
                (Substances.All.CosmicDust.GetHomogeneousReference(), dust),
                (Substances.All.CarbonMonoxide.GetHomogeneousReference(), co),
                (Substances.All.CarbonDioxide.GetHomogeneousReference(), co2),
                (Substances.All.Ammonia.GetHomogeneousReference(), nh3),
                (Substances.All.Methane.GetHomogeneousReference(), ch4),
                (Substances.All.HydrogenSulfide.GetHomogeneousReference(), h2s),
                (Substances.All.SulphurDioxide.GetHomogeneousReference(), so2));
        }

        private void GenerateAtmosphere(Rehydrator rehydrator)
        {
            if (PlanetType == PlanetType.Comet
                || IsAsteroid)
            {
                GenerateAtmosphere_SmallBody(rehydrator);
                return;
            }

            if (IsGiant)
            {
                GenerateAtmosphere_Giant(rehydrator);
                return;
            }

            if (!IsTerrestrial)
            {
                GenerateAtmosphere_Dwarf(rehydrator);
                return;
            }

            if (AverageBlackbodyTemperature >= GetTempForThinAtmosphere())
            {
                GenerateAtmosphereTrace(rehydrator);
            }
            else
            {
                GenerateAtmosphereThick(rehydrator);
            }

            var adjustedAtmosphericPressure = Atmosphere.AtmosphericPressure;

            var water = Substances.All.Water.GetHomogeneousReference();
            var seawater = Substances.All.Seawater.GetHomogeneousReference();
            // Water may be removed, or if not may remove CO2 from the atmosphere, depending on
            // planetary conditions.
            if (Hydrosphere.Contains(water)
                || Hydrosphere.Contains(seawater)
                || Atmosphere.Material.Contains(water))
            {
                // First calculate water phases at effective temp, to establish a baseline
                // for the presence of water and its effect on CO2.
                // If a desired temp has been established, use that instead.
                double surfaceTemp;
                if (_planetParams?.SurfaceTemperature.HasValue == true)
                {
                    surfaceTemp = _planetParams!.Value.SurfaceTemperature!.Value;
                }
                else if (_habitabilityRequirements?.MinimumTemperature.HasValue == true)
                {
                    surfaceTemp = _habitabilityRequirements!.Value.MaximumTemperature.HasValue
                        ? (_habitabilityRequirements!.Value.MinimumTemperature!.Value
                            + _habitabilityRequirements!.Value.MaximumTemperature!.Value)
                            / 2
                        : _habitabilityRequirements!.Value.MinimumTemperature!.Value;
                }
                else
                {
                    surfaceTemp = AverageBlackbodyTemperature;
                }
                adjustedAtmosphericPressure = CalculateGasPhaseMix(
                    rehydrator,
                    water,
                    surfaceTemp,
                    adjustedAtmosphericPressure);

                // Recalculate temperatures based on the new atmosphere.
                ResetCachedTemperatures();

                FractionHydrophere(GetAverageSurfaceTemperature());

                // Recalculate the phases of water based on the new temperature.
                adjustedAtmosphericPressure = CalculateGasPhaseMix(
                    rehydrator,
                    water,
                    GetAverageSurfaceTemperature(),
                    adjustedAtmosphericPressure);

                // If life alters the greenhouse potential, temperature and water phase must be
                // recalculated once again.
                if (GenerateLife(rehydrator))
                {
                    adjustedAtmosphericPressure = CalculateGasPhaseMix(
                        rehydrator,
                        water,
                        GetAverageSurfaceTemperature(),
                        adjustedAtmosphericPressure);
                    ResetCachedTemperatures();
                    FractionHydrophere(GetAverageSurfaceTemperature());
                }
            }
            else
            {
                // Recalculate temperature based on the new atmosphere.
                ResetCachedTemperatures();
            }

            var modified = false;
            foreach (var requirement in Atmosphere.ConvertRequirementsForPressure(_habitabilityRequirements?.AtmosphericRequirements)
                .Concat(Atmosphere.ConvertRequirementsForPressure(_planetParams?.AtmosphericRequirements)))
            {
                var proportion = Atmosphere.Material.GetProportion(requirement.Substance);
                if (proportion < requirement.MinimumProportion
                    || (requirement.MaximumProportion.HasValue && proportion > requirement.MaximumProportion.Value))
                {
                    Atmosphere.Material.AddConstituent(
                        requirement.Substance,
                        requirement.MaximumProportion.HasValue
                            ? (requirement.MinimumProportion + requirement.MaximumProportion.Value) / 2
                            : requirement.MinimumProportion);
                    if (requirement.Substance.Equals(water))
                    {
                        Atmosphere.ResetWater();
                    }
                    modified = true;
                }
            }
            if (modified)
            {
                Atmosphere.ResetGreenhouseFactor();
                ResetCachedTemperatures();
            }

            adjustedAtmosphericPressure = CalculatePhases(rehydrator, 0, adjustedAtmosphericPressure);
            FractionHydrophere(GetAverageSurfaceTemperature());

            if (_planetParams?.AtmosphericPressure.HasValue != true && _habitabilityRequirements is null)
            {
                SetAtmosphericPressure(Math.Max(0, adjustedAtmosphericPressure));
                Atmosphere.ResetPressureDependentProperties(this);
            }

            // If the adjustments have led to the loss of liquid water, then there is no life after
            // all (this may be interpreted as a world which once supported life, but became
            // inhospitable due to the environmental changes that life produced).
            if (!HasLiquidWater())
            {
                HasBiosphere = false;
            }
        }

        private void GenerateAtmosphereThick(Rehydrator rehydrator)
        {
            double pressure;
            if (_planetParams?.AtmosphericPressure.HasValue == true)
            {
                pressure = Math.Max(0, _planetParams!.Value.AtmosphericPressure!.Value);
            }
            else if (_planetParams?.EarthlikeAtmosphere == true)
            {
                pressure = PlanetParams.EarthAtmosphericPressure;
            }
            else if (_habitabilityRequirements?.MinimumPressure.HasValue == true
                || _habitabilityRequirements?.MaximumPressure.HasValue == true)
            {
                // If there is a minimum but no maximum, a half-Gaussian distribution with the minimum as both mean and the basis for the sigma is used.
                if (_habitabilityRequirements.HasValue
                    && _habitabilityRequirements.Value.MinimumPressure.HasValue)
                {
                    if (!_habitabilityRequirements.Value.MaximumPressure.HasValue)
                    {
                        pressure = _habitabilityRequirements.Value.MinimumPressure.Value
                            + Math.Abs(rehydrator.NormalDistributionSample(47, 0, _habitabilityRequirements.Value.MinimumPressure.Value / 3));
                    }
                    else
                    {
                        pressure = rehydrator.NextDouble(47, _habitabilityRequirements.Value.MinimumPressure ?? 0, _habitabilityRequirements.Value.MaximumPressure.Value);
                    }
                }
                else
                {
                    pressure = 0;
                }
            }
            else
            {
                Number mass;
                // Low-gravity planets without magnetospheres are less likely to hold onto the bulk
                // of their atmospheres over long periods.
                if (Mass >= 1.5e24 || HasMagnetosphere)
                {
                    mass = Mass / rehydrator.NormalDistributionSample(47, 1158568, 38600, minimum: 579300, maximum: 1737900);
                }
                else
                {
                    mass = Mass / rehydrator.NormalDistributionSample(47, 7723785, 258000, minimum: 3862000, maximum: 11586000);
                }

                pressure = (double)(mass * SurfaceGravity / (1000 * MathConstants.FourPI * RadiusSquared));
            }

            // For terrestrial (non-giant) planets, these gases remain at low concentrations due to
            // atmospheric escape.
            var h = _planetParams?.EarthlikeAtmosphere == true ? 3.8e-8m : rehydrator.NextDecimal(48, 1e-8m, 2e-7m);
            var he = _planetParams?.EarthlikeAtmosphere == true ? 7.24e-6m : rehydrator.NextDecimal(49, 2.6e-7m, 1e-5m);

            // 50% chance not to have these components at all.
            var ch4 = _planetParams?.EarthlikeAtmosphere == true ? 2.9e-6m : Math.Max(0, rehydrator.NextDecimal(50, -0.5m, 0.5m));
            var traceTotal = ch4;

            var co = _planetParams?.EarthlikeAtmosphere == true ? 2.5e-7m : Math.Max(0, rehydrator.NextDecimal(51, -0.5m, 0.5m));
            traceTotal += co;

            var so2 = _planetParams?.EarthlikeAtmosphere == true ? 1e-7m : Math.Max(0, rehydrator.NextDecimal(52, -0.5m, 0.5m));
            traceTotal += so2;

            decimal trace;
            if (_planetParams?.EarthlikeAtmosphere == true)
            {
                trace = traceTotal;
            }
            else if (traceTotal == 0)
            {
                trace = 0;
            }
            else
            {
                trace = rehydrator.NextDecimal(53, 1e-6m, 2.5e-4m);
            }
            if (_planetParams?.EarthlikeAtmosphere != true)
            {
                var traceRatio = traceTotal == 0 ? 0 : trace / traceTotal;
                ch4 *= traceRatio;
                co *= traceRatio;
                so2 *= traceRatio;
            }

            // CO2 makes up the bulk of a thick atmosphere by default (although the presence of water
            // may change this later).
            var co2 = _planetParams?.EarthlikeAtmosphere == true ? 5.3e-4m : rehydrator.NextDecimal(54, 0.97m, 0.99m) - trace;

            // If there is water on the surface, the water in the air will be determined based on
            // vapor pressure later, and should not be randomly assigned. Otherwise, there is a small
            // chance of water vapor without significant surface water (results of cometary deposits, etc.)
            var waterVapor = _planetParams?.EarthlikeAtmosphere == true ? PlanetParams.EarthWaterVaporRatio : 0.0m;
            var surfaceWater = false;
            if (_planetParams?.EarthlikeAtmosphere != true)
            {
                var water = Substances.All.Water.GetHomogeneousReference();
                var seawater = Substances.All.Seawater.GetHomogeneousReference();
                surfaceWater = Hydrosphere.Contains(water) || Hydrosphere.Contains(seawater);
                if (CanHaveWater && !surfaceWater)
                {
                    waterVapor = Math.Max(0, rehydrator.NextDecimal(55, -0.05m, 0.001m));
                }
            }

            // Always at least some oxygen if there is water, planetary composition allowing
            var o2 = _planetParams?.EarthlikeAtmosphere == true ? 0.23133m : 0.0m;
            if (_planetParams?.EarthlikeAtmosphere != true && PlanetType != PlanetType.Carbon)
            {
                if (waterVapor != 0)
                {
                    o2 = waterVapor * 0.0001m;
                }
                else if (surfaceWater)
                {
                    o2 = rehydrator.NextDecimal(56, 0.002m);
                }
            }

            var o3 = _planetParams?.EarthlikeAtmosphere == true ? o2 * 4.5e-5m : 0;

            // N2 (largely inert gas) comprises whatever is left after the other components have been
            // determined. This is usually a trace amount, unless CO2 has been reduced to a trace, in
            // which case it will comprise the bulk of the atmosphere.
            var n2 = 1 - (h + he + co2 + waterVapor + o2 + o3 + trace);

            // Some portion of the N2 may be Ar instead.
            var ar = _planetParams?.EarthlikeAtmosphere == true ? 1.288e-3m : Math.Max(0, n2 * rehydrator.NextDecimal(57, -0.02m, 0.04m));
            n2 -= ar;
            // An even smaller fraction may be Kr.
            var kr = _planetParams?.EarthlikeAtmosphere == true ? 3.3e-6m : Math.Max(0, n2 * rehydrator.NextDecimal(58, -2.5e-4m, 5.0e-4m));
            n2 -= kr;
            // An even smaller fraction may be Xe or Ne.
            var xe = _planetParams?.EarthlikeAtmosphere == true ? 8.7e-8m : Math.Max(0, n2 * rehydrator.NextDecimal(59, -1.8e-5m, 3.5e-5m));
            n2 -= xe;
            var ne = _planetParams?.EarthlikeAtmosphere == true ? 1.267e-5m : Math.Max(0, n2 * rehydrator.NextDecimal(60, -1.8e-5m, 3.5e-5m));
            n2 -= ne;

            var components = new List<(ISubstanceReference, decimal)>()
            {
                (Substances.All.CarbonDioxide.GetHomogeneousReference(), co2),
                (Substances.All.Helium.GetHomogeneousReference(), he),
                (Substances.All.Hydrogen.GetHomogeneousReference(), h),
                (Substances.All.Nitrogen.GetHomogeneousReference(), n2),
            };
            if (ar > 0)
            {
                components.Add((Substances.All.Argon.GetHomogeneousReference(), ar));
            }
            if (co > 0)
            {
                components.Add((Substances.All.CarbonMonoxide.GetHomogeneousReference(), co));
            }
            if (kr > 0)
            {
                components.Add((Substances.All.Krypton.GetHomogeneousReference(), kr));
            }
            if (ch4 > 0)
            {
                components.Add((Substances.All.Methane.GetHomogeneousReference(), ch4));
            }
            if (o2 > 0)
            {
                components.Add((Substances.All.Oxygen.GetHomogeneousReference(), o2));
            }
            if (o3 > 0)
            {
                components.Add((Substances.All.Ozone.GetHomogeneousReference(), o3));
            }
            if (so2 > 0)
            {
                components.Add((Substances.All.SulphurDioxide.GetHomogeneousReference(), so2));
            }
            if (waterVapor > 0)
            {
                components.Add((Substances.All.Water.GetHomogeneousReference(), waterVapor));
            }
            if (xe > 0)
            {
                components.Add((Substances.All.Xenon.GetHomogeneousReference(), xe));
            }
            Atmosphere = new Atmosphere(this, pressure, components.ToArray());
        }

        private void GenerateAtmosphereTrace(Rehydrator rehydrator)
        {
            // For terrestrial (non-giant) planets, these gases remain at low concentrations due to
            // atmospheric escape.
            var h = rehydrator.NextDecimal(47, 5e-8m, 2e-7m);
            var he = rehydrator.NextDecimal(48, 2.6e-7m, 1e-5m);

            // 50% chance not to have these components at all.
            var ch4 = Math.Max(0, rehydrator.NextDecimal(49, -0.5m, 0.5m));
            var total = ch4;

            var co = Math.Max(0, rehydrator.NextDecimal(50, -0.5m, 0.5m));
            total += co;

            var so2 = Math.Max(0, rehydrator.NextDecimal(51, -0.5m, 0.5m));
            total += so2;

            var n2 = Math.Max(0, rehydrator.NextDecimal(52, -0.5m, 0.5m));
            total += n2;

            // Noble traces: selected as fractions of N2, if present, to avoid over-representation.
            var ar = n2 > 0 ? Math.Max(0, n2 * rehydrator.NextDecimal(53, -0.02m, 0.04m)) : 0;
            n2 -= ar;
            var kr = n2 > 0 ? Math.Max(0, n2 * rehydrator.NextDecimal(54, -0.02m, 0.04m)) : 0;
            n2 -= kr;
            var xe = n2 > 0 ? Math.Max(0, n2 * rehydrator.NextDecimal(55, -0.02m, 0.04m)) : 0;
            n2 -= xe;

            // Carbon monoxide means at least some carbon dioxide, as well.
            var co2 = co > 0
                ? rehydrator.NextDecimal(56, 0.5m)
                : Math.Max(0, rehydrator.NextDecimal(56, -0.5m, 0.5m));
            total += co2;

            // If there is water on the surface, the water in the air will be determined based on
            // vapor pressure later, and should not be randomly assigned. Otherwise, there is a small
            // chance of water vapor without significant surface water (results of cometary deposits, etc.)
            var waterVapor = 0.0m;
            var water = Substances.All.Water.GetHomogeneousReference();
            var seawater = Substances.All.Seawater.GetHomogeneousReference();
            if (CanHaveWater
                && !Hydrosphere.Contains(water)
                && !Hydrosphere.Contains(seawater))
            {
                waterVapor = Math.Max(0, rehydrator.NextDecimal(57, -0.05m, 0.001m));
            }
            total += waterVapor;

            var o2 = 0.0m;
            if (PlanetType != PlanetType.Carbon)
            {
                // Always at least some oxygen if there is water, planetary composition allowing
                o2 = waterVapor > 0
                    ? waterVapor * 1e-4m
                    : Math.Max(0, rehydrator.NextDecimal(58, -0.05m, 0.5m));
            }
            total += o2;

            var ratio = total == 0 ? 0 : (1 - h - he) / total;
            ch4 *= ratio;
            co *= ratio;
            so2 *= ratio;
            n2 *= ratio;
            ar *= ratio;
            kr *= ratio;
            xe *= ratio;
            co2 *= ratio;
            waterVapor *= ratio;
            o2 *= ratio;

            // H and He are always assumed to be present in small amounts if a planet has any
            // atmosphere, but without any other gases making up the bulk of the atmosphere, they are
            // presumed lost to atmospheric escape entirely, and no atmosphere at all is indicated.
            if (total == 0)
            {
                Atmosphere = new Atmosphere(this, 0);
            }
            else
            {
                var components = new List<(ISubstanceReference, decimal)>()
                {
                    (Substances.All.Helium.GetHomogeneousReference(), he),
                    (Substances.All.Hydrogen.GetHomogeneousReference(), h),
                };
                if (ar > 0)
                {
                    components.Add((Substances.All.Argon.GetHomogeneousReference(), ar));
                }
                if (co2 > 0)
                {
                    components.Add((Substances.All.CarbonDioxide.GetHomogeneousReference(), co2));
                }
                if (co > 0)
                {
                    components.Add((Substances.All.CarbonMonoxide.GetHomogeneousReference(), co));
                }
                if (kr > 0)
                {
                    components.Add((Substances.All.Krypton.GetHomogeneousReference(), kr));
                }
                if (ch4 > 0)
                {
                    components.Add((Substances.All.Methane.GetHomogeneousReference(), ch4));
                }
                if (n2 > 0)
                {
                    components.Add((Substances.All.Nitrogen.GetHomogeneousReference(), n2));
                }
                if (o2 > 0)
                {
                    components.Add((Substances.All.Oxygen.GetHomogeneousReference(), o2));
                }
                if (so2 > 0)
                {
                    components.Add((Substances.All.SulphurDioxide.GetHomogeneousReference(), so2));
                }
                if (waterVapor > 0)
                {
                    components.Add((Substances.All.Water.GetHomogeneousReference(), waterVapor));
                }
                if (xe > 0)
                {
                    components.Add((Substances.All.Xenon.GetHomogeneousReference(), xe));
                }
                Atmosphere = new Atmosphere(this, rehydrator.NextDouble(59, 25), components.ToArray());
            }
        }

        private Planetoid? GenerateGiantSatellite(
            Rehydrator rehydrator,
            ref ulong index,
            CosmicLocation? parent,
            List<Star> stars,
            Number periapsis,
            double eccentricity,
            Number maxMass)
        {
            var orbit = new OrbitalParameters(
                Mass,
                Position,
                periapsis,
                eccentricity,
                rehydrator.NextDouble(index++, 0.5),
                rehydrator.NextDouble(index++, MathAndScience.Constants.Doubles.MathConstants.TwoPI),
                rehydrator.NextDouble(index++, MathAndScience.Constants.Doubles.MathConstants.TwoPI),
                rehydrator.NextDouble(index++, MathAndScience.Constants.Doubles.MathConstants.TwoPI));
            double chance;

            // If the mass limit allows, there is an even chance that the satellite is a smaller planet.
            if (maxMass > _TerrestrialMinMassForType && rehydrator.NextBool(index++))
            {
                // Select from the standard distribution of types.
                chance = rehydrator.NextDouble(index++);

                // Planets with very low orbits are lava planets due to tidal
                // stress (plus a small percentage of others due to impact trauma).

                // The maximum mass and density are used to calculate an outer
                // Roche limit (may not be the actual Roche limit for the body
                // which gets generated).
                if (periapsis < GetRocheLimit(DefaultTerrestrialMaxDensity) * new Number(105, -2) || chance <= 0.01)
                {
                    return new Planetoid(
                        PlanetType.Lava,
                        parent,
                        null,
                        stars,
                        Vector3.Zero,
                        out _,
                        orbit,
                        new PlanetParams(maxMass: maxMass),
                        null,
                        true,
                        rehydrator.NextUIntInclusive(index++));
                }
                else if (chance <= 0.65) // Most will be standard terrestrial.
                {
                    return new Planetoid(
                        PlanetType.Terrestrial,
                        parent,
                        null,
                        stars,
                        Vector3.Zero,
                        out _,
                        orbit,
                        new PlanetParams(maxMass: maxMass),
                        null,
                        true,
                        rehydrator.NextUIntInclusive(index++));
                }
                else if (chance <= 0.75)
                {
                    return new Planetoid(
                        PlanetType.Iron,
                        parent,
                        null,
                        stars,
                        Vector3.Zero,
                        out _,
                        orbit,
                        new PlanetParams(maxMass: maxMass),
                        null,
                        true,
                        rehydrator.NextUIntInclusive(index++));
                }
                else
                {
                    return new Planetoid(
                        PlanetType.Ocean,
                        parent,
                        null,
                        stars,
                        Vector3.Zero,
                        out _,
                        orbit,
                        new PlanetParams(maxMass: maxMass),
                        null,
                        true,
                        rehydrator.NextUIntInclusive(index++));
                }
            }

            // Otherwise, if the mass limit allows, there is an even chance that the satellite is a dwarf planet.
            else if (maxMass > _DwarfMinMassForType && rehydrator.NextBool(index++))
            {
                chance = rehydrator.NextDouble(index++);
                // Dwarf planets with very low orbits are lava planets due to tidal stress (plus a small percentage of others due to impact trauma).
                if (periapsis < GetRocheLimit(DensityForDwarf) * new Number(105, -2) || chance <= 0.01)
                {
                    return new Planetoid(
                        PlanetType.LavaDwarf,
                        parent,
                        null,
                        stars,
                        Vector3.Zero,
                        out _,
                        orbit,
                        new PlanetParams(maxMass: maxMass),
                        null,
                        true,
                        rehydrator.NextUIntInclusive(index++));
                }
                else if (chance <= 0.75) // Most will be standard.
                {
                    return new Planetoid(
                        PlanetType.Dwarf,
                        parent,
                        null,
                        stars,
                        Vector3.Zero,
                        out _,
                        orbit,
                        new PlanetParams(maxMass: maxMass),
                        null,
                        true,
                        rehydrator.NextUIntInclusive(index++));
                }
                else
                {
                    return new Planetoid(
                        PlanetType.RockyDwarf,
                        parent,
                        null,
                        stars,
                        Vector3.Zero,
                        out _,
                        orbit,
                        new PlanetParams(maxMass: maxMass),
                        null,
                        true,
                        rehydrator.NextUIntInclusive(index++));
                }
            }

            // Otherwise, it is an asteroid, selected from the standard distribution of types.
            else if (maxMass > 0)
            {
                chance = rehydrator.NextDouble(index++);
                if (chance <= 0.75)
                {
                    return new Planetoid(
                        PlanetType.AsteroidC,
                        parent,
                        null,
                        stars,
                        Vector3.Zero,
                        out _,
                        orbit,
                        new PlanetParams(maxMass: maxMass),
                        null,
                        true,
                        rehydrator.NextUIntInclusive(index++));
                }
                else if (chance <= 0.9)
                {
                    return new Planetoid(
                        PlanetType.AsteroidS,
                        parent,
                        null,
                        stars,
                        Vector3.Zero,
                        out _,
                        orbit,
                        new PlanetParams(maxMass: maxMass),
                        null,
                        true,
                        rehydrator.NextUIntInclusive(index++));
                }
                else
                {
                    return new Planetoid(
                        PlanetType.AsteroidM,
                        parent,
                        null,
                        stars,
                        Vector3.Zero,
                        out _,
                        orbit,
                        new PlanetParams(maxMass: maxMass),
                        null,
                        true,
                        rehydrator.NextUIntInclusive(index++));
                }
            }

            return null;
        }

        private void GenerateHydrocarbons(Rehydrator rehydrator)
        {
            // It is presumed that it is statistically likely that the current eon is not the first
            // with life, and therefore that some fossilized hydrocarbon deposits exist.
            var coal = (decimal)rehydrator.NormalDistributionSample(67, 1e-13, 1.7e-14);

            AddResource(Substances.All.Anthracite.GetReference(), coal, false);
            AddResource(Substances.All.BituminousCoal.GetReference(), coal, false);

            var petroleum = (decimal)rehydrator.NormalDistributionSample(68, 1e-8, 1.6e-9);
            var petroleumSeed = AddResource(Substances.All.Petroleum.GetReference(), petroleum, false);

            // Natural gas is predominantly, though not exclusively, found with petroleum deposits.
            AddResource(Substances.All.NaturalGas.GetReference(), petroleum, false, true, petroleumSeed);
        }

        private void GenerateHydrosphere(Rehydrator rehydrator, double surfaceTemp)
        {
            // Most terrestrial planets will (at least initially) have a hydrosphere layer (oceans,
            // icecaps, etc.). This might be removed later, depending on the planet's conditions.

            if (!CanHaveWater || !IsTerrestrial)
            {
                SeaLevel = -MaxElevation * 1.1;
                return;
            }

            decimal ratio;
            if (_planetParams.HasValue && _planetParams.Value.WaterRatio.HasValue)
            {
                ratio = _planetParams.Value.WaterRatio.Value;
            }
            else if (PlanetType == PlanetType.Ocean)
            {
                ratio = (decimal)(1 + rehydrator.NormalDistributionSample(44, 1, 0.2));
            }
            else
            {
                ratio = rehydrator.NextDecimal(44);
            }

            var mass = Number.Zero;
            var seawater = Substances.All.Seawater.GetHomogeneousReference();

            if (ratio <= 0)
            {
                SeaLevel = -MaxElevation * 1.1;
            }
            else if (ratio >= 1)
            {
                SeaLevel = MaxElevation * (double)ratio;
                mass = new HollowSphere(Shape.ContainingRadius, Shape.ContainingRadius + SeaLevel).Volume * (seawater.Homogeneous.DensityLiquid ?? 0);
            }
            else
            {
                var seaLevel = 0.0;
                SeaLevel = 0;
                const double randomMapElevationFactor = 0.33975352675545284; // proportion of MaxElevation of a random elevation map * 1/(e-1)
                var variance = ratio == 0.5m
                    ? 0
                    : (Math.Exp(Math.Abs(((double)ratio) - 0.5)) - 1) * randomMapElevationFactor;
                if (ratio != 0.5m)
                {
                    seaLevel = ratio > 0.5m
                        ? variance
                        : -variance;
                    SeaLevel = seaLevel * MaxElevation;
                }
                const double halfVolume = 85183747862278.266; // empirical sum of random map pixel columns with 0 sea level
                var volume = seaLevel > 0
                    ? halfVolume + (halfVolume * variance)
                    : halfVolume - (halfVolume * variance);
                mass = volume
                    * MaxElevation
                    * (seawater.Homogeneous.DensityLiquid ?? 0);
            }

            if (!mass.IsPositive)
            {
                Hydrosphere = MathAndScience.Chemistry.Material.Empty;
                return;
            }

            // Surface water is mostly salt water.
            var seawaterProportion = (decimal)rehydrator.NormalDistributionSample(45, 0.945, 0.015);
            var waterProportion = 1 - seawaterProportion;
            var water = Substances.All.Water.GetHomogeneousReference();
            var density = ((seawater.Homogeneous.DensityLiquid ?? 0) * (double)seawaterProportion) + ((water.Homogeneous.DensityLiquid ?? 0) * (double)waterProportion);

            var outerRadius = (3 * ((mass / density) + new Sphere(Material.Shape.ContainingRadius).Volume) / MathConstants.FourPI).CubeRoot();
            var shape = new HollowSphere(
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

            Hydrosphere = new Material(
                density,
                mass,
                shape,
                avgTemp,
                (seawater, seawaterProportion),
                (water, waterProportion));

            FractionHydrophere(surfaceTemp);

            if (Material.GetSurface() is Material material)
            {
                material.AddConstituents(CelestialSubstances.WetPlanetaryCrustConstituents);
            }
        }

        /// <summary>
        /// Determines whether this planet is capable of sustaining life, and whether or not it
        /// actually does. If so, the atmosphere may be adjusted.
        /// </summary>
        /// <returns>
        /// True if the atmosphere's greenhouse potential is adjusted; false if not.
        /// </returns>
        private bool GenerateLife(Rehydrator rehydrator)
        {
            if (IsInhospitable || !HasLiquidWater())
            {
                HasBiosphere = false;
                return false;
            }

            // If the planet already has a biosphere, there is nothing left to do.
            if (HasBiosphere)
            {
                return false;
            }

            HasBiosphere = true;

            GenerateHydrocarbons(rehydrator);

            // If the habitable zone is a subsurface ocean, no further adjustments occur.
            if (Hydrosphere is Composite)
            {
                return false;
            }

            if (_planetParams?.EarthlikeAtmosphere == true)
            {
                return false;
            }

            // If there is a habitable surface layer, it is presumed that an initial population of a
            // cyanobacteria analogue will produce a significant amount of free oxygen, which in turn
            // will transform most CH4 to CO2 and H2O, and also produce an ozone layer.
            var o2 = rehydrator.NextDecimal(69, 0.2m, 0.25m);
            var oxygen = Substances.All.Oxygen.GetHomogeneousReference();
            Atmosphere.Material.AddConstituent(oxygen, o2);

            // Calculate ozone based on level of free oxygen.
            var o3 = o2 * 4.5e-5m;
            var ozone = Substances.All.Ozone.GetHomogeneousReference();
            if (Atmosphere.Material is not Composite lc || lc.Components.Count < 3)
            {
                Atmosphere.DifferentiateTroposphere(); // First ensure troposphere is differentiated.
                (Atmosphere.Material as Composite)?.CopyComponent(1, 0.01m);
            }
            (Atmosphere.Material as Composite)?.Components[2].AddConstituent(ozone, o3);

            // Convert most methane to CO2 and H2O.
            var methane = Substances.All.Methane.GetHomogeneousReference();
            var ch4 = Atmosphere.Material.GetProportion(methane);
            if (ch4 != 0)
            {
                // The levels of CO2 and H2O are not adjusted; it is presumed that the levels already
                // determined for them take the amounts derived from CH4 into account. If either gas
                // is entirely missing, however, it is added.
                var carbonDioxide = Substances.All.CarbonDioxide.GetHomogeneousReference();
                if (Atmosphere.Material.GetProportion(carbonDioxide) <= 0)
                {
                    Atmosphere.Material.AddConstituent(carbonDioxide, ch4 / 3);
                }

                if (Atmosphere.Material.GetProportion(Substances.All.Water) <= 0)
                {
                    Atmosphere.Material.AddConstituent(Substances.All.Water, ch4 * 2 / 3);
                    Atmosphere.ResetWater();
                }

                Atmosphere.Material.AddConstituent(methane, ch4 * 0.001m);

                Atmosphere.ResetGreenhouseFactor();
                ResetCachedTemperatures();
                return true;
            }

            return false;
        }

        private void GenerateMaterial(
            Rehydrator rehydrator,
            double? temperature,
            Vector3 position,
            Number semiMajorAxis)
        {
            if (PlanetType == PlanetType.Comet)
            {
                Material = new Material(
                    CelestialSubstances.CometNucleus,
                    rehydrator.NextDouble(7, 300, 700),
                    // Gaussian distribution with most values between 1km and 19km.
                    new Ellipsoid(
                        rehydrator.NormalDistributionSample(8, 10000, 4500, minimum: 0),
                        rehydrator.NextNumber(9, Number.Half, 1),
                        position),
                    temperature);
                return;
            }

            if (IsAsteroid)
            {
                var doubleMaxMass = _planetParams.HasValue && _planetParams.Value.MaxMass.HasValue
                    ? (double)_planetParams.Value.MaxMass.Value
                    : AsteroidMaxMassForType;
                var mass = rehydrator.PositiveNormalDistributionSample(
                    7,
                    AsteroidMinMassForType,
                    (doubleMaxMass - AsteroidMinMassForType) / 3,
                    doubleMaxMass);

                var asteroidDensity = PlanetType switch
                {
                    PlanetType.AsteroidC => 1380,
                    PlanetType.AsteroidM => 5320,
                    PlanetType.AsteroidS => 2710,
                    _ => 2000,
                };

                var axis = (mass * new Number(75, -2) / (asteroidDensity * MathConstants.PI)).CubeRoot();
                var irregularity = rehydrator.NextNumber(8, Number.Half, Number.One);
                var shape = new Ellipsoid(axis, axis * irregularity, axis / irregularity, position);

                var substances = GetAsteroidComposition(rehydrator);
                Material = new Material(
                    substances,
                    asteroidDensity,
                    mass,
                    shape,
                    temperature);
                return;
            }

            if (PlanetType == PlanetType.Lava
                || PlanetType == PlanetType.LavaDwarf)
            {
                temperature = rehydrator.NextDouble(7, 974, 1574);
            }

            var density = GetDensity(rehydrator, PlanetType);

            double? gravity = null;
            if (_planetParams?.SurfaceGravity.HasValue == true)
            {
                gravity = _planetParams!.Value.SurfaceGravity!.Value;
            }
            else if (_habitabilityRequirements?.MinimumGravity.HasValue == true
                || _habitabilityRequirements?.MaximumGravity.HasValue == true)
            {
                double maxGravity;
                if (_habitabilityRequirements?.MaximumGravity.HasValue == true)
                {
                    maxGravity = _habitabilityRequirements!.Value.MaximumGravity!.Value;
                }
                else // Determine the maximum gravity the planet could have by calculating from its maximum mass.
                {
                    var max = _planetParams?.MaxMass ?? GetMaxMassForType(PlanetType);
                    var maxVolume = max / density;
                    var maxRadius = (maxVolume / MathConstants.FourThirdsPI).CubeRoot();
                    maxGravity = (double)(ScienceConstants.G * max / (maxRadius * maxRadius));
                }
                gravity = rehydrator.NextDouble(9, _habitabilityRequirements?.MinimumGravity ?? 0, maxGravity);
            }

            Number MassFromUnknownGravity(Rehydrator rehydrator, Number semiMajorAxis, ulong index)
            {
                var min = Number.Zero;
                if (!PlanetType.AnyDwarf.HasFlag(PlanetType))
                {
                    // Stern-Levison parameter for neighborhood-clearing used to determined minimum mass
                    // at which the planet would be able to do so at this orbital distance. We set the
                    // minimum at two orders of magnitude more than this (planets in our solar system
                    // all have masses above 5 orders of magnitude more). Note that since lambda is
                    // proportional to the square of mass, it is multiplied by 10 to obtain a difference
                    // of 2 orders of magnitude, rather than by 100.
                    var sternLevisonLambdaMass = (Number.Pow(semiMajorAxis, new Number(15, -1)) / new Number(2.5, -28)).Sqrt();
                    min = Number.Max(min, sternLevisonLambdaMass * 10);

                    // sanity check; may result in a "planet" which *can't* clear its neighborhood
                    if (_planetParams.HasValue
                        && _planetParams.Value.MaxMass.HasValue
                        && min > _planetParams.Value.MaxMass.Value)
                    {
                        min = _planetParams.Value.MaxMass.Value;
                    }
                }
                return _planetParams.HasValue && _planetParams.Value.MaxMass.HasValue
                    ? rehydrator.NextNumber(index, min, _planetParams.Value.MaxMass.Value)
                    : min;
            }

            if (_planetParams?.Radius.HasValue == true)
            {
                var radius = Number.Max(MinimumRadius, _planetParams!.Value.Radius!.Value);
                var flattening = rehydrator.NextNumber(10, Number.Deci);
                var shape = new Ellipsoid(radius, radius * (1 - flattening), position);

                Number mass;
                if (gravity.HasValue)
                {
                    mass = GetMass(PlanetType, semiMajorAxis, _planetParams?.MaxMass, gravity.Value, shape);
                }
                else
                {
                    mass = MassFromUnknownGravity(rehydrator, semiMajorAxis, 11);
                }

                Material = GetComposition(rehydrator, density, mass, shape, temperature);
            }
            else if (gravity.HasValue)
            {
                var radius = Number.Max(MinimumRadius, Number.Min(GetRadiusForSurfaceGravity(gravity.Value), GetRadiusForMass(density, GetMaxMassForType(PlanetType))));
                var flattening = rehydrator.NextNumber(10, Number.Deci);
                var shape = new Ellipsoid(radius, radius * (1 - flattening), position);

                var mass = GetMass(PlanetType, semiMajorAxis, _planetParams?.MaxMass, gravity.Value, shape);

                Material = GetComposition(rehydrator, density, mass, shape, temperature);
            }
            else
            {
                Number mass;
                if (IsGiant)
                {
                    mass = rehydrator.NextNumber(10, _GiantMinMassForType, _planetParams?.MaxMass ?? _GiantMaxMassForType);
                }
                else if (IsDwarf)
                {
                    var maxMass = _planetParams?.MaxMass;
                    if (!string.IsNullOrEmpty(ParentId))
                    {
                        var sternLevisonLambdaMass = (Number.Pow(semiMajorAxis, new Number(15, -1)) / new Number(2.5, -28)).Sqrt();
                        maxMass = Number.Min(_planetParams?.MaxMass ?? _DwarfMaxMassForType, sternLevisonLambdaMass / 100);
                        if (maxMass < _DwarfMinMassForType)
                        {
                            maxMass = _DwarfMinMassForType; // sanity check; may result in a "dwarf" planet which *can* clear its neighborhood
                        }
                    }
                    mass = rehydrator.NextNumber(10, _DwarfMinMassForType, maxMass ?? _DwarfMaxMassForType);
                }
                else
                {
                    mass = MassFromUnknownGravity(rehydrator, semiMajorAxis, 10);
                }

                // An approximate radius as if the shape was a sphere is determined, which is no less
                // than the minimum required for hydrostatic equilibrium.
                var radius = Number.Max(MinimumRadius, GetRadiusForMass(density, mass));
                var flattening = rehydrator.NextNumber(11, Number.Deci);
                var shape = new Ellipsoid(radius, radius * (1 - flattening), position);

                Material = GetComposition(rehydrator, density, mass, shape, temperature);
            }
        }

        private void GenerateOrbit(
            Rehydrator rehydrator,
            OrbitalParameters? orbit,
            CosmicLocation? orbitedObject,
            double eccentricity,
            Number semiMajorAxis)
        {
            if (orbit.HasValue)
            {
                Space.Orbit.AssignOrbit(this, orbit.Value);
                return;
            }

            if (orbitedObject is null)
            {
                return;
            }

            if (PlanetType == PlanetType.Comet)
            {
                Space.Orbit.AssignOrbit(
                    this,
                    orbitedObject,

                    // Current distance is presumed to be apoapsis for comets, which are presumed to
                    // originate in an Oort cloud, and have eccentricities which may either leave
                    // them there, or send them into the inner solar system.
                    (1 - eccentricity) / (1 + eccentricity) * GetDistanceTo(orbitedObject),

                    eccentricity,
                    rehydrator.NextDouble(37, Math.PI),
                    rehydrator.NextDouble(38, MathAndScience.Constants.Doubles.MathConstants.TwoPI),
                    rehydrator.NextDouble(39, MathAndScience.Constants.Doubles.MathConstants.TwoPI),
                    Math.PI);
                return;
            }

            if (IsAsteroid)
            {
                Space.Orbit.AssignOrbit(
                    this,
                    orbitedObject,
                    GetDistanceTo(orbitedObject),
                    eccentricity,
                    rehydrator.NextDouble(37, 0.5),
                    rehydrator.NextDouble(38, MathAndScience.Constants.Doubles.MathConstants.TwoPI),
                    rehydrator.NextDouble(39, MathAndScience.Constants.Doubles.MathConstants.TwoPI),
                    0);
                return;
            }

            if (!IsTerrestrial)
            {
                Space.Orbit.AssignOrbit(
                    this,
                    orbitedObject,
                    GetDistanceTo(orbitedObject),
                    eccentricity,
                    rehydrator.NextDouble(37, 0.9),
                    rehydrator.NextDouble(38, MathAndScience.Constants.Doubles.MathConstants.TwoPI),
                    rehydrator.NextDouble(39, MathAndScience.Constants.Doubles.MathConstants.TwoPI),
                    rehydrator.NextDouble(40, MathAndScience.Constants.Doubles.MathConstants.TwoPI));
                return;
            }

            var ta = rehydrator.NextDouble(37, MathAndScience.Constants.Doubles.MathConstants.TwoPI);
            if (_planetParams?.RevolutionPeriod.HasValue == true)
            {
                GenerateOrbit(rehydrator, orbitedObject, eccentricity, semiMajorAxis, ta);
            }
            else
            {
                Space.Orbit.AssignOrbit(
                    this,
                    orbitedObject,
                    GetDistanceTo(orbitedObject),
                    eccentricity,
                    rehydrator.NextDouble(38, 0.9),
                    rehydrator.NextDouble(39, MathAndScience.Constants.Doubles.MathConstants.TwoPI),
                    rehydrator.NextDouble(40, MathAndScience.Constants.Doubles.MathConstants.TwoPI),
                    rehydrator.NextDouble(41, MathAndScience.Constants.Doubles.MathConstants.TwoPI));
            }
        }

        private void GenerateOrbit(
            Rehydrator rehydrator,
            CosmicLocation orbitedObject,
            double eccentricity,
            Number semiMajorAxis,
            double trueAnomaly) => Space.Orbit.AssignOrbit(
            this,
            orbitedObject,
            (1 - eccentricity) * semiMajorAxis,
            eccentricity,
            rehydrator.NextDouble(38, 0.9),
            rehydrator.NextDouble(39, MathAndScience.Constants.Doubles.MathConstants.TwoPI),
            rehydrator.NextDouble(40, MathAndScience.Constants.Doubles.MathConstants.TwoPI),
            trueAnomaly);

        private void GenerateResources(Rehydrator rehydrator)
        {
            AddResources(Material.GetSurface()
                  .Constituents.Where(x => x.Key.Substance.IsGemstone || x.Key.Substance.IsMetalOre())
                  .Select(x => (x.Key, x.Value, true))
                  ?? Enumerable.Empty<(ISubstanceReference, decimal, bool)>());
            AddResources(Material.GetSurface()
                  .Constituents.Where(x => x.Key.Substance.IsHydrocarbon())
                  .Select(x => (x.Key, x.Value, false))
                  ?? Enumerable.Empty<(ISubstanceReference, decimal, bool)>());

            // Also add halite (rock salt) as a resource, despite not being an ore or gem.
            AddResources(Material.GetSurface()
                  .Constituents.Where(x => x.Key.Equals(Substances.All.SodiumChloride.GetHomogeneousReference()))
                  .Select(x => (x.Key, x.Value, false))
                  ?? Enumerable.Empty<(ISubstanceReference, decimal, bool)>());

            // A magnetosphere is presumed to indicate tectonic, and hence volcanic, activity.
            // This, in turn, indicates elemental sulfur at the surface.
            if (HasMagnetosphere)
            {
                var sulfurProportion = (decimal)rehydrator.NormalDistributionSample(70, 3.5e-5, 1.75e-6);
                if (sulfurProportion > 0)
                {
                    AddResource(Substances.All.Sulfur.GetHomogeneousReference(), sulfurProportion, false);
                }
            }

            if (IsTerrestrial)
            {
                var beryl = (decimal)rehydrator.NormalDistributionSample(71, 4e-6, 6.7e-7, minimum: 0);
                var emerald = beryl * 1.58e-4m;
                var corundum = (decimal)rehydrator.NormalDistributionSample(72, 2.6e-4, 4e-5, minimum: 0);
                var ruby = corundum * 1.58e-4m;
                var sapphire = corundum * 5.7e-3m;

                var diamond = PlanetType == PlanetType.Carbon
                    ? 0 // Carbon planets have diamond in the crust, which will have been added earlier.
                    : (decimal)rehydrator.NormalDistributionSample(73, 1.5e-7, 2.5e-8, minimum: 0);

                if (beryl > 0)
                {
                    AddResource(Substances.All.Beryl.GetHomogeneousReference(), beryl, true);
                }
                if (emerald > 0)
                {
                    AddResource(Substances.All.Emerald.GetHomogeneousReference(), emerald, true);
                }
                if (corundum > 0)
                {
                    AddResource(Substances.All.Corundum.GetHomogeneousReference(), corundum, true);
                }
                if (ruby > 0)
                {
                    AddResource(Substances.All.Ruby.GetHomogeneousReference(), ruby, true);
                }
                if (sapphire > 0)
                {
                    AddResource(Substances.All.Sapphire.GetHomogeneousReference(), sapphire, true);
                }
                if (diamond > 0)
                {
                    AddResource(Substances.All.Diamond.GetHomogeneousReference(), diamond, true);
                }
            }
        }

        private Planetoid? GenerateSatellite(
            Rehydrator rehydrator,
            ref ulong index,
            CosmicLocation? parent,
            List<Star> stars,
            Number periapsis,
            double eccentricity,
            Number maxMass)
        {
            if (PlanetType == PlanetType.GasGiant
                || PlanetType == PlanetType.IceGiant)
            {
                return GenerateGiantSatellite(rehydrator, ref index, parent, stars, periapsis, eccentricity, maxMass);
            }
            if (PlanetType == PlanetType.AsteroidC)
            {
                return new Planetoid(
                    PlanetType.AsteroidC,
                    parent,
                    null,
                    stars,
                    Vector3.Zero,
                    out _,
                    GetAsteroidSatelliteOrbit(rehydrator, ref index, periapsis, eccentricity),
                    new PlanetParams(maxMass: maxMass),
                    null,
                    true,
                    rehydrator.NextUIntInclusive(index++));
            }
            if (PlanetType == PlanetType.AsteroidM)
            {
                return new Planetoid(
                    PlanetType.AsteroidM,
                    parent,
                    null,
                    stars,
                    Vector3.Zero,
                    out _,
                    GetAsteroidSatelliteOrbit(rehydrator, ref index, periapsis, eccentricity),
                    new PlanetParams(maxMass: maxMass),
                    null,
                    true,
                    rehydrator.NextUIntInclusive(index++));
            }
            if (PlanetType == PlanetType.AsteroidS)
            {
                return new Planetoid(
                    PlanetType.AsteroidS,
                    parent,
                    null,
                    stars,
                    Vector3.Zero,
                    out _,
                    GetAsteroidSatelliteOrbit(rehydrator, ref index, periapsis, eccentricity),
                    new PlanetParams(maxMass: maxMass),
                    null,
                    true,
                    rehydrator.NextUIntInclusive(index++));
            }
            if (PlanetType == PlanetType.Comet)
            {
                return null;
            }
            var orbit = new OrbitalParameters(
                Mass,
                Position,
                periapsis,
                eccentricity,
                rehydrator.NextDouble(index++, 0.5),
                rehydrator.NextDouble(index++, MathAndScience.Constants.Doubles.MathConstants.TwoPI),
                rehydrator.NextDouble(index++, MathAndScience.Constants.Doubles.MathConstants.TwoPI),
                rehydrator.NextDouble(index++, MathAndScience.Constants.Doubles.MathConstants.TwoPI));
            double chance;

            // If the mass limit allows, there is an even chance that the satellite is a smaller planet.
            if (maxMass > _TerrestrialMinMassForType && rehydrator.NextBool(index++))
            {
                // Select from the standard distribution of types.
                chance = rehydrator.NextDouble(index++);

                // Planets with very low orbits are lava planets due to tidal
                // stress (plus a small percentage of others due to impact trauma).

                // Most will be standard terrestrial.
                double terrestrialChance;
                if (PlanetType == PlanetType.Carbon)
                {
                    terrestrialChance = 0.45;
                }
                else if (IsGiant)
                {
                    terrestrialChance = 0.65;
                }
                else
                {
                    terrestrialChance = 0.77;
                }

                // The maximum mass and density are used to calculate an outer
                // Roche limit (may not be the actual Roche limit for the body
                // which gets generated).
                if (periapsis < GetRocheLimit(DefaultTerrestrialMaxDensity) * new Number(105, -2) || chance <= 0.01)
                {
                    return new Planetoid(
                        PlanetType.Lava,
                        parent,
                        null,
                        stars,
                        Vector3.Zero,
                        out _,
                        orbit,
                        new PlanetParams(maxMass: maxMass),
                        null,
                        true,
                        rehydrator.NextUIntInclusive(index++));
                }
                else if (chance <= terrestrialChance)
                {
                    return new Planetoid(
                        PlanetType.Terrestrial,
                        parent,
                        null,
                        stars,
                        Vector3.Zero,
                        out _,
                        orbit,
                        new PlanetParams(maxMass: maxMass),
                        null,
                        true,
                        rehydrator.NextUIntInclusive(index++));
                }
                else if (PlanetType == PlanetType.Carbon && chance <= 0.77) // Carbon planets alone have a chance for carbon satellites.
                {
                    return new Planetoid(
                        PlanetType.Carbon,
                        parent,
                        null,
                        stars,
                        Vector3.Zero,
                        out _,
                        orbit,
                        new PlanetParams(maxMass: maxMass),
                        null,
                        true,
                        rehydrator.NextUIntInclusive(index++));
                }
                else if (IsGiant && chance <= 0.75)
                {
                    return new Planetoid(
                        PlanetType.Iron,
                        parent,
                        null,
                        stars,
                        Vector3.Zero,
                        out _,
                        orbit,
                        new PlanetParams(maxMass: maxMass),
                        null,
                        true,
                        rehydrator.NextUIntInclusive(index++));
                }
                else
                {
                    return new Planetoid(
                        PlanetType.Ocean,
                        parent,
                        null,
                        stars,
                        Vector3.Zero,
                        out _,
                        orbit,
                        new PlanetParams(maxMass: maxMass),
                        null,
                        true,
                        rehydrator.NextUIntInclusive(index++));
                }
            }

            // Otherwise, if the mass limit allows, there is an even chance that the satellite is a dwarf planet.
            else if (maxMass > _DwarfMinMassForType && rehydrator.NextBool(index++))
            {
                chance = rehydrator.NextDouble(index++);
                // Dwarf planets with very low orbits are lava planets due to tidal stress (plus a small percentage of others due to impact trauma).
                if (periapsis < GetRocheLimit(DensityForDwarf) * new Number(105, -2) || chance <= 0.01)
                {
                    return new Planetoid(
                        PlanetType.LavaDwarf,
                        parent,
                        null,
                        stars,
                        Vector3.Zero,
                        out _,
                        orbit,
                        new PlanetParams(maxMass: maxMass),
                        null,
                        true,
                        rehydrator.NextUIntInclusive(index++));
                }
                else if (chance <= 0.75) // Most will be standard.
                {
                    return new Planetoid(
                        PlanetType.Dwarf,
                        parent,
                        null,
                        stars,
                        Vector3.Zero,
                        out _,
                        orbit,
                        new PlanetParams(maxMass: maxMass),
                        null,
                        true,
                        rehydrator.NextUIntInclusive(index++));
                }
                else
                {
                    return new Planetoid(
                        PlanetType.RockyDwarf,
                        parent,
                        null,
                        stars,
                        Vector3.Zero,
                        out _,
                        orbit,
                        new PlanetParams(maxMass: maxMass),
                        null,
                        true,
                        rehydrator.NextUIntInclusive(index++));
                }
            }

            // Otherwise, it is an asteroid, selected from the standard distribution of types.
            else if (maxMass > 0)
            {
                chance = rehydrator.NextDouble(index++);
                if (chance <= 0.75)
                {
                    return new Planetoid(
                        PlanetType.AsteroidC,
                        parent,
                        null,
                        stars,
                        Vector3.Zero,
                        out _,
                        orbit,
                        new PlanetParams(maxMass: maxMass),
                        null,
                        true,
                        rehydrator.NextUIntInclusive(index++));
                }
                else if (chance <= 0.9)
                {
                    return new Planetoid(
                        PlanetType.AsteroidS,
                        parent,
                        null,
                        stars,
                        Vector3.Zero,
                        out _,
                        orbit,
                        new PlanetParams(maxMass: maxMass),
                        null,
                        true,
                        rehydrator.NextUIntInclusive(index++));
                }
                else
                {
                    return new Planetoid(
                        PlanetType.AsteroidM,
                        parent,
                        null,
                        stars,
                        Vector3.Zero,
                        out _,
                        orbit,
                        new PlanetParams(maxMass: maxMass),
                        null,
                        true,
                        rehydrator.NextUIntInclusive(index++));
                }
            }

            return null;
        }

        private List<Planetoid> GenerateSatellites(Rehydrator rehydrator, CosmicLocation? parent, List<Star> stars, ulong index)
        {
            var addedSatellites = new List<Planetoid>();

            int maxSatellites;
            if (_planetParams?.NumSatellites.HasValue == true)
            {
                maxSatellites = _planetParams!.Value.NumSatellites!.Value;
            }
            else
            {
                maxSatellites = PlanetType switch
                {
                    // 5 for most Planemos. For reference, Pluto has 5 moons, the most of any planemo in the
                    // Solar System apart from the giants. No others are known to have more than 2.
                    PlanetType.Terrestrial => 5,
                    PlanetType.Carbon => 5,
                    PlanetType.Iron => 5,
                    PlanetType.Ocean => 5,
                    PlanetType.Dwarf => 5,
                    PlanetType.RockyDwarf => 5,

                    // Lava planets are too unstable for satellites.
                    PlanetType.Lava => 0,
                    PlanetType.LavaDwarf => 0,

                    // Set to 75 for Giant. For reference, Jupiter has 67 moons, and Saturn has 62
                    // (non-ring) moons.
                    PlanetType.GasGiant => 75,

                    // Set to 40 for IceGiant. For reference, Uranus has 27 moons, and Neptune has 14 moons.
                    PlanetType.IceGiant => 40,

                    _ => 1,
                };
            }

            if (_satelliteIDs != null || maxSatellites <= 0)
            {
                return addedSatellites;
            }

            var minPeriapsis = Shape.ContainingRadius + 20;
            var maxApoapsis = Orbit.HasValue ? GetHillSphereRadius() / 3 : Shape.ContainingRadius * 100;

            while (minPeriapsis <= maxApoapsis && (_satelliteIDs?.Count ?? 0) < maxSatellites)
            {
                var periapsis = rehydrator.NextNumber(index++, minPeriapsis, maxApoapsis);

                var maxEccentricity = (double)((maxApoapsis - periapsis) / (maxApoapsis + periapsis));
                var eccentricity = maxEccentricity < 0.01
                    ? rehydrator.NextDouble(index++, 0, maxEccentricity)
                    : rehydrator.PositiveNormalDistributionSample(index++, 0, 0.05, maximum: maxEccentricity);

                var semiLatusRectum = periapsis * (1 + eccentricity);
                var semiMajorAxis = semiLatusRectum / (1 - (eccentricity * eccentricity));

                // Keep mass under the limit where the orbital barycenter would be pulled outside the boundaries of this body.
                var maxMass = Number.Max(0, Mass / ((semiMajorAxis / Shape.ContainingRadius) - 1));

                var satellite = GenerateSatellite(rehydrator, ref index, parent, stars, periapsis, eccentricity, maxMass);
                if (satellite is null)
                {
                    break;
                }
                addedSatellites.Add(satellite);

                (_satelliteIDs ??= new List<string>()).Add(satellite.Id);

                minPeriapsis = (satellite.Orbit?.Apoapsis ?? 0) + satellite.GetSphereOfInfluenceRadius();
            }

            return addedSatellites;
        }

        /// <summary>
        /// Calculates the surface albedo this <see cref="Planetoid"/> would need in order to have
        /// the given effective temperature at its average distance from the given <paramref
        /// name="star"/> (assuming it is either orbiting the star or not in orbit at all, and that
        /// the current difference between its surface and total albedo remained constant).
        /// </summary>
        /// <remarks>
        /// The effects of other nearby stars are ignored.
        /// </remarks>
        /// <param name="star">
        /// The <see cref="Star"/> for which the calculation is to be made.
        /// </param>
        /// <param name="temperature">The desired temperature, in K.</param>
        private double GetSurfaceAlbedoForTemperature(Star star, double temperature)
        {
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

            var averageDistanceSq = Orbit.HasValue
                ? ((Orbit.Value.Apoapsis + Orbit.Value.Periapsis) / 2).Square()
                : Position.DistanceSquared(star.Position);

            var albedo = 1 - (averageDistanceSq
                * Math.Pow(temperature - Temperature, 4)
                * MathAndScience.Constants.Doubles.MathConstants.FourPI
                * MathAndScience.Constants.Doubles.ScienceConstants.sigma
                * areaRatio
                / star.Luminosity);

            var delta = Albedo - _surfaceAlbedo;

            return Math.Max(0, (double)albedo - delta);
        }

        private List<(ISubstanceReference, decimal)> GetAsteroidComposition(Rehydrator rehydrator)
        {
            var substances = new List<(ISubstanceReference, decimal)>();

            if (PlanetType == PlanetType.AsteroidM)
            {
                var ironNickel = 0.95m;

                var rock = rehydrator.NextDecimal(9, 0.2m);
                ironNickel -= rock;

                var gold = rehydrator.NextDecimal(10, 0.05m);

                var platinum = 0.05m - gold;

                foreach (var (material, proportion) in CelestialSubstances.ChondriticRockMixture_NoMetal)
                {
                    substances.Add((material, proportion * rock));
                }
                substances.Add((Substances.All.IronNickelAlloy.GetHomogeneousReference(), ironNickel));
                substances.Add((Substances.All.Gold.GetHomogeneousReference(), gold));
                substances.Add((Substances.All.Platinum.GetHomogeneousReference(), platinum));
            }
            else if (PlanetType == PlanetType.AsteroidS)
            {
                var gold = rehydrator.NextDecimal(9, 0.005m);

                foreach (var (material, proportion) in CelestialSubstances.ChondriticRockMixture_NoMetal)
                {
                    substances.Add((material, proportion * 0.427m));
                }
                substances.Add((Substances.All.IronNickelAlloy.GetHomogeneousReference(), 0.568m));
                substances.Add((Substances.All.Gold.GetHomogeneousReference(), gold));
                substances.Add((Substances.All.Platinum.GetHomogeneousReference(), 0.005m - gold));
            }
            else
            {
                var rock = 1m;

                var clay = rehydrator.NextDecimal(9, 0.1m, 0.2m);
                rock -= clay;

                var ice = PlanetType.AnyDwarf.HasFlag(PlanetType)
                    ? rehydrator.NextDecimal(10)
                    : rehydrator.NextDecimal(10, 0.22m);
                rock -= ice;

                foreach (var (material, proportion) in CelestialSubstances.ChondriticRockMixture)
                {
                    substances.Add((material, proportion * rock));
                }
                substances.Add((Substances.All.BallClay.GetReference(), clay));
                substances.Add((Substances.All.Water.GetHomogeneousReference(), ice));
            }

            return substances;
        }

        private OrbitalParameters GetAsteroidSatelliteOrbit(Rehydrator rehydrator, ref ulong index, Number periapsis, double eccentricity)
            => new(
                Mass,
                Position,
                periapsis,
                eccentricity,
                rehydrator.NextDouble(index++, 0.5),
                rehydrator.NextDouble(index++, MathAndScience.Constants.Doubles.MathConstants.TwoPI),
                rehydrator.NextDouble(index++, MathAndScience.Constants.Doubles.MathConstants.TwoPI),
                rehydrator.NextDouble(index++, MathAndScience.Constants.Doubles.MathConstants.TwoPI));

        /// <summary>
        /// Calculates the atmospheric pressure at a given elevation, in kPa.
        /// </summary>
        /// <param name="temperature">The temperature at the given elevation, in K.</param>
        /// <param name="elevation">
        /// An elevation above the reference elevation for standard atmospheric pressure (sea level),
        /// in meters.
        /// </param>
        /// <returns>The atmospheric pressure at the specified height, in kPa.</returns>
        /// <remarks>
        /// In an Earth-like atmosphere, the pressure lapse rate varies considerably in the different
        /// atmospheric layers, but this cannot be easily modeled for arbitrary exoplanetary
        /// atmospheres, so the simple barometric formula is used, which should be "close enough" for
        /// the purposes of this library. Also, this calculation uses the molar mass of air on Earth,
        /// which is clearly not correct for other atmospheres, but is considered "close enough" for
        /// the purposes of this library.
        /// </remarks>
        private double GetAtmosphericPressureFromTempAndElevation(double temperature, double elevation)
            => Atmosphere.GetAtmosphericPressure(this, temperature, elevation);

        private IMaterial GetComposition(Rehydrator rehydrator, double density, Number mass, IShape shape, double? temperature)
        {
            var coreProportion = PlanetType switch
            {
                PlanetType.Dwarf or PlanetType.LavaDwarf or PlanetType.RockyDwarf => rehydrator
                    .NextNumber(12, new Number(2, -1), new Number(55, -2)),
                PlanetType.Carbon or PlanetType.Iron => new Number(4, -1),
                _ => new Number(15, -2),
            };

            var crustProportion = IsGiant
                ? Number.Zero
                // Smaller planemos have thicker crusts due to faster proto-planetary cooling.
                : 400000 / Number.Pow(shape.ContainingRadius, new Number(16, -1));

            var coreLayers = IsGiant
                ? GetCore_Giant(rehydrator, shape, coreProportion, mass).ToList()
                : GetCore(rehydrator, shape, coreProportion, crustProportion, mass).ToList();
            var topCoreLayer = coreLayers.Last();
            var coreShape = topCoreLayer.Shape;
            var coreTemp = topCoreLayer.Temperature ?? 0;

            var mantleProportion = 1 - (coreProportion + crustProportion);
            var mantleLayers = GetMantle(rehydrator, shape, mantleProportion, crustProportion, mass, coreShape, coreTemp).ToList();
            if (mantleLayers.Count == 0
                && mantleProportion.IsPositive)
            {
                crustProportion += mantleProportion;
            }

            var crustLayers = GetCrust(rehydrator, shape, crustProportion, mass).ToList();
            if (crustLayers.Count == 0
                && crustProportion.IsPositive)
            {
                if (mantleLayers.Count == 0)
                {
                    var ratio = 1 / coreProportion;
                    foreach (var layer in coreLayers)
                    {
                        layer.Mass *= ratio;
                    }
                }
                else
                {
                    var ratio = 1 / (coreProportion + mantleProportion);
                    foreach (var layer in coreLayers)
                    {
                        layer.Mass *= ratio;
                    }
                    foreach (var layer in mantleLayers)
                    {
                        layer.Mass *= ratio;
                    }
                }
            }

            var layers = new List<IMaterial>();
            layers.AddRange(coreLayers);
            layers.AddRange(mantleLayers);
            layers.AddRange(crustLayers);
            return new Composite(
                layers,
                shape,
                density,
                mass,
                temperature);
        }

        private IEnumerable<IMaterial> GetCore(
            Rehydrator rehydrator,
            IShape planetShape,
            Number coreProportion,
            Number crustProportion,
            Number planetMass)
        {
            var coreMass = planetMass * coreProportion;

            var coreRadius = planetShape.ContainingRadius * coreProportion;
            var shape = new Sphere(coreRadius, planetShape.Position);

            var mantleBoundaryDepth = planetShape.ContainingRadius * crustProportion;

            (ISubstanceReference, decimal)[] coreConstituents;
            if (PlanetType == PlanetType.Carbon)
            {
                // Iron/steel-nickel core (some steel forms naturally in the carbon-rich environment).
                var coreSteel = rehydrator.NextDecimal(12, 0.945m);
                coreConstituents = new (ISubstanceReference, decimal)[]
                {
                    (Substances.All.Iron.GetHomogeneousReference(), 0.945m - coreSteel),
                    (Substances.All.CarbonSteel.GetHomogeneousReference(), coreSteel),
                    (Substances.All.Nickel.GetHomogeneousReference(), 0.055m),
                };
            }
            else
            {
                coreConstituents = new (ISubstanceReference, decimal)[] { (Substances.All.IronNickelAlloy.GetHomogeneousReference(), 1) };
            }

            yield return new Material(
                (double)(coreMass / shape.Volume),
                coreMass,
                shape,
                (double)((mantleBoundaryDepth * new Number(115, -2)) + (planetShape.ContainingRadius - coreRadius - mantleBoundaryDepth)),
                coreConstituents);
        }

        private IEnumerable<IMaterial> GetCrust(
            Rehydrator rehydrator,
            IShape planetShape,
            Number crustProportion,
            Number planetMass)
        {
            if (IsGiant)
            {
                yield break;
            }
            else if (PlanetType == PlanetType.RockyDwarf)
            {
                foreach (var item in GetCrust_RockyDwarf(rehydrator, planetShape, crustProportion, planetMass))
                {
                    yield return item;
                }
                yield break;
            }
            else if (PlanetType == PlanetType.LavaDwarf)
            {
                foreach (var item in GetCrust_LavaDwarf(rehydrator, planetShape, crustProportion, planetMass))
                {
                    yield return item;
                }
                yield break;
            }
            else if (PlanetType == PlanetType.Carbon)
            {
                foreach (var item in GetCrust_Carbon(rehydrator, planetShape, crustProportion, planetMass))
                {
                    yield return item;
                }
                yield break;
            }
            else if (IsTerrestrial)
            {
                foreach (var item in GetCrust_Terrestrial(rehydrator, planetShape, crustProportion, planetMass))
                {
                    yield return item;
                }
                yield break;
            }

            var crustMass = planetMass * crustProportion;

            var shape = new HollowSphere(
                planetShape.ContainingRadius - (planetShape.ContainingRadius * crustProportion),
                planetShape.ContainingRadius,
                planetShape.Position);

            var dust = rehydrator.NextDecimal(13);
            var total = dust;

            // 50% chance of not including the following:
            var waterIce = Math.Max(0, rehydrator.NextDecimal(14, -0.5m, 0.5m));
            total += waterIce;

            var n2 = Math.Max(0, rehydrator.NextDecimal(15, -0.5m, 0.5m));
            total += n2;

            var ch4 = Math.Max(0, rehydrator.NextDecimal(16, -0.5m, 0.5m));
            total += ch4;

            var co = Math.Max(0, rehydrator.NextDecimal(17, -0.5m, 0.5m));
            total += co;

            var co2 = Math.Max(0, rehydrator.NextDecimal(18, -0.5m, 0.5m));
            total += co2;

            var nh3 = Math.Max(0, rehydrator.NextDecimal(19, -0.5m, 0.5m));
            total += nh3;

            var ratio = 1 / total;
            dust *= ratio;
            waterIce *= ratio;
            n2 *= ratio;
            ch4 *= ratio;
            co *= ratio;
            co2 *= ratio;
            nh3 *= ratio;

            var components = new List<(ISubstanceReference, decimal)>()
            {
                (Substances.All.CosmicDust.GetHomogeneousReference(), dust),
            };
            if (waterIce > 0)
            {
                components.Add((Substances.All.Water.GetHomogeneousReference(), waterIce));
            }
            if (n2 > 0)
            {
                components.Add((Substances.All.Nitrogen.GetHomogeneousReference(), n2));
            }
            if (ch4 > 0)
            {
                components.Add((Substances.All.Methane.GetHomogeneousReference(), ch4));
            }
            if (co > 0)
            {
                components.Add((Substances.All.CarbonMonoxide.GetHomogeneousReference(), co));
            }
            if (co2 > 0)
            {
                components.Add((Substances.All.CarbonDioxide.GetHomogeneousReference(), co2));
            }
            if (nh3 > 0)
            {
                components.Add((Substances.All.Ammonia.GetHomogeneousReference(), nh3));
            }
            yield return new Material(
                components,
                (double)(crustMass / shape.Volume),
                crustMass,
                shape);
        }

        /// <summary>
        /// Calculates the distance (in meters) this <see cref="Planetoid"/> would have to be
        /// from a <see cref="Star"/> in order to have the given effective temperature.
        /// </summary>
        /// <remarks>
        /// The effects of other nearby stars are ignored.
        /// </remarks>
        /// <param name="star">The <see cref="Star"/> for which the calculation is to be made.</param>
        /// <param name="temperature">The desired temperature, in K.</param>
        private Number GetDistanceForTemperature(Star star, double temperature)
        {
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

            return Math.Sqrt(star.Luminosity * (1 - Albedo)
                / (Math.Pow(temperature - Temperature, 4)
                * MathAndScience.Constants.Doubles.MathConstants.FourPI
                * MathAndScience.Constants.Doubles.ScienceConstants.sigma
                * areaRatio));
        }

        private (double latitude, double longitude) GetEclipticLatLon(Vector3 position, Vector3 otherPosition)
        {
            var precessionQ = Quaternion.CreateFromYawPitchRoll(AxialPrecession, 0, 0);
            var p = Vector3.Transform(position - otherPosition, precessionQ) * -1;
            var r = p.Length();
            var lat = Math.Asin((double)(p.Z / r));
            if (lat >= Math.PI)
            {
                lat = MathAndScience.Constants.Doubles.MathConstants.TwoPI - lat;
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

        private decimal GetHydrosphereAtmosphereRatio() => Math.Min(1, (decimal)(Hydrosphere.Mass / Atmosphere.Material.Mass));

        private double GetInsolationFactor(bool polar = false)
            => GetInsolationFactor(Atmosphere.Material.Mass, Atmosphere.AtmosphericScaleHeight, polar);

        private double GetInsolationFactor(double latitude)
            => InsolationFactor_Polar + ((InsolationFactor_Equatorial - InsolationFactor_Polar)
            * (0.5 + (Math.Cos(Math.Max(0, (Math.Abs(2 * latitude)
                * (MathAndScience.Constants.Doubles.MathConstants.HalfPI + AxialTilt)
                / MathAndScience.Constants.Doubles.MathConstants.HalfPI) - AxialTilt)) / 2)));

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

            var numerator = (MathAndScience.Constants.Doubles.ScienceConstants.RSpecificDryAir * surfaceTemp2) + (Atmosphere.HvE * surfaceTemp);
            var denominator = (MathAndScience.Constants.Doubles.ScienceConstants.CpTimesRSpecificDryAir * surfaceTemp2) + Atmosphere.Hv2RsE;

            return (double)SurfaceGravity * (numerator / denominator);
        }

        private double GetLuminousFlux(IEnumerable<Star> stars)
        {
            var sum = 0.0;
            foreach (var star in stars)
            {
                sum += (double)(star.Luminosity / (MathConstants.FourPI * GetDistanceSquaredTo(star))) / 0.0079;
            }
            return sum;
        }

        private IEnumerable<IMaterial> GetMantle(
            Rehydrator rehydrator,
            IShape planetShape,
            Number mantleProportion,
            Number crustProportion,
            Number planetMass,
            IShape coreShape,
            double coreTemp)
        {
            if (PlanetType == PlanetType.RockyDwarf)
            {
                yield break;
            }
            else if (PlanetType == PlanetType.GasGiant)
            {
                foreach (var item in GetMantle_Giant(rehydrator, planetShape, mantleProportion, crustProportion, planetMass, coreShape, coreTemp))
                {
                    yield return item;
                }
                yield break;
            }
            else if (PlanetType == PlanetType.IceGiant)
            {
                foreach (var item in GetMantle_IceGiant(rehydrator, planetShape, mantleProportion, crustProportion, planetMass, coreShape, coreTemp))
                {
                    yield return item;
                }
                yield break;
            }
            else if (PlanetType == PlanetType.Carbon)
            {
                foreach (var item in GetMantle_Carbon(rehydrator, planetShape, mantleProportion, crustProportion, planetMass, coreShape, coreTemp))
                {
                    yield return item;
                }
                yield break;
            }

            var mantleBoundaryDepth = planetShape.ContainingRadius * crustProportion;
            var mantleBoundaryTemp = (double)mantleBoundaryDepth * 1.15;
            var mantleTemp = (mantleBoundaryTemp + coreTemp) / 2;

            var shape = new HollowSphere(coreShape.ContainingRadius, planetShape.ContainingRadius * mantleProportion, planetShape.Position);

            var mantleMass = planetMass * mantleProportion;

            yield return new Material(
                PlanetType switch
                {
                    PlanetType.Dwarf => Substances.All.Water.GetHomogeneousReference(),
                    _ => Substances.All.Peridotite.GetReference(),
                },
                (double)(mantleMass / shape.Volume),
                mantleMass,
                shape,
                mantleTemp);
        }

        private double GetMaxPolarTemperature()
        {
            var greenhouseEffect = GetGreenhouseEffect();
            var temp = _surfaceTemperatureAtPeriapsis;
            return (temp * InsolationFactor_Polar) + greenhouseEffect;
        }

        private double GetMinEquatorTemperature()
        {
            var variation = GetDiurnalTemperatureVariation();
            var greenhouseEffect = GetGreenhouseEffect();
            return (_surfaceTemperatureAtApoapsis * InsolationFactor_Equatorial) + greenhouseEffect - variation;
        }

        private double GetElevationNoise(double x, double y, double z)
        {
            if (MaxElevation.IsNearlyZero())
            {
                return 0;
            }

            // Initial noise map: a simple fractal noise map.
            var baseNoise = Noise1.GetNoise(x, y, z);

            // Mountain noise map: a more ridged map.
            var mountains = (-Noise2.GetNoise(x, y, z) - 0.25) * 4 / 3;

            // Scale the base noise to the typical average height of continents, with a degree of
            // randomness borrowed from the mountain noise function.
            var scaledBaseNoise = (baseNoise * (0.25 + (mountains * 0.0625))) - 0.04;

            // Modify the mountain map to indicate mountains only in random areas, instead of
            // uniformly across the globe.
            mountains *= (Noise3.GetNoise(x, y, z) + 1).Clamp(0, 1);

            // Multiply with itself to produce predominantly low values with high (and low)
            // extremes, and scale to the typical maximum height of mountains, with a degree of
            // randomness borrowed from the base noise function.
            mountains = Math.CopySign(mountains * mountains * (0.525 + (baseNoise * 0.13125)), mountains);

            // The combined value is returned, resulting in mostly broad, low-lying areas,
            // interrupted by occasional high mountain ranges and low trenches.
            return scaledBaseNoise + mountains;
        }

        private double GetPolarAirMass(double atmosphericScaleHeight)
        {
            var r = (double)Shape.ContainingRadius / atmosphericScaleHeight;
            var rCosLat = r * CosPolarLatitude;
            return Math.Sqrt((rCosLat * rCosLat) + (2 * r) + 1) - rCosLat;
        }

        private double GetPrecipitationNoise(
            double x,
            double y,
            double z,
            double latitude,
            double seasonalLatitude,
            double temperature,
            out double snow)
        {
            snow = 0;

            // Noise map with smooth, broad areas. Random range ~0.5-2.
            var r1 = 1.25 + (Noise4.GetNoise(x, y, z) * 0.75);

            // More detailed noise map. Random range of ~0-1.35.
            var r2 = 0.675 + (Noise5.GetNoise(x, y, z) * 0.75);

            // Combined map is noise with broad similarity over regions, and minor local
            // diversity. Range ~0.5-3.35.
            var r = r1 * r2;

            // Hadley cells alter local conditions.
            var absLatitude = Math.Abs(latitude);
            var absSeasonalLatitude = Math.Abs((latitude + seasonalLatitude) / 2);
            var hadleyValue = 0.0;

            // The polar deserts above ~±10º result in almost no precipitation
            if (absLatitude > ArcticLatitude)
            {
                // Range ~-3-~0.
                hadleyValue = -3 * ((absLatitude - ArcticLatitude) / ArcticLatitudeRange);
            }

            // The horse latitudes create the subtropical deserts between ~±35º-30º
            if (absLatitude < FifthPI)
            {
                // Range ~-3-0.
                hadleyValue = 2 * (r1 - 2) * ((FifthPI - absLatitude) / FifthPI);

                // The ITCZ increases in intensity towards the thermal equator
                if (absSeasonalLatitude < EighthPI)
                {
                    // Range 0-~33.5.
                    hadleyValue += 10 * r * ((EighthPI - absSeasonalLatitude) / EighthPI).Cube();
                }
            }

            // Relative humidity is the Hadley cell value added to the random value. Range ~-2.5-~36.85.
            var relativeHumidity = r + hadleyValue;

            // In the range betwen 32K and 48K below freezing, the value is scaled down; below that
            // range it is cut off completely; above it is unchanged.
            relativeHumidity *= ((temperature - _LowTemp) / 16).Clamp(0, 1);

            if (relativeHumidity <= 0)
            {
                return 0;
            }

            var precipitation = Atmosphere.AveragePrecipitation * relativeHumidity;

            if (temperature <= Substances.All.Water.MeltingPoint)
            {
                snow = precipitation * Atmosphere.SnowToRainRatio;
            }

            return precipitation;
        }

        private async ValueTask<Star?> GetPrimaryStarAsync(IDataStore dataStore)
        {
            var system = await GetStarSystemAsync(dataStore).ConfigureAwait(false);
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
            var minDistance = Number.PositiveInfinity;
            foreach (var star in stars)
            {
                Number starDistance;
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

        private Number GetRadiusForSurfaceGravity(double gravity) => (Mass * ScienceConstants.G / gravity).Sqrt();

        private Rehydrator GetRehydrator(uint? seed = null)
        {
            Seed = seed ?? Randomizer.Instance.NextUIntInclusive();

            var rehydrator = new Rehydrator(Seed);
            _seed1 = rehydrator.NextInclusive(0);
            _seed2 = rehydrator.NextInclusive(1);
            _seed3 = rehydrator.NextInclusive(2);
            _seed4 = rehydrator.NextInclusive(3);
            _seed5 = rehydrator.NextInclusive(4);

            return rehydrator;
        }

        private (double rightAscension, double declination) GetRightAscensionAndDeclination(Vector3 position, Vector3 otherPosition)
        {
            var rot = AxisRotation;
            var equatorialPosition = Vector3.Transform(position - otherPosition, rot);
            var r = equatorialPosition.Length();
            var mPos = !equatorialPosition.Y.IsZero
                && equatorialPosition.Y.Sign() == r.Sign();
            var n = (double)(equatorialPosition.Z / r);
            var declination = Math.Asin(n);
            if (declination > Math.PI)
            {
                declination -= MathAndScience.Constants.Doubles.MathConstants.TwoPI;
            }
            var cosDeclination = Math.Cos(declination);
            if (cosDeclination.IsNearlyZero())
            {
                return (0, declination);
            }
            var rightAscension = mPos
                ? Math.Acos(1 / cosDeclination)
                : MathAndScience.Constants.Doubles.MathConstants.TwoPI - Math.Acos(1 / cosDeclination);
            if (rightAscension > Math.PI)
            {
                rightAscension -= MathAndScience.Constants.Doubles.MathConstants.TwoPI;
            }
            return (rightAscension, declination);
        }

        /// <summary>
        /// Calculates the approximate outer distance at which rings of the given density may be
        /// found, in meters.
        /// </summary>
        /// <param name="density">The density of the rings, in kg/m³.</param>
        /// <returns>The approximate outer distance at which rings of the given density may be
        /// found, in meters.</returns>
        private Number GetRingDistance(Number density)
            => new Number(126, -2)
            * Shape.ContainingRadius
            * (Material.Density / density).CubeRoot();

        private Number GetSphereOfInfluenceRadius() => Orbit?.GetSphereOfInfluenceRadius(Mass) ?? Number.Zero;

        private async ValueTask<StarSystem?> GetStarSystemAsync(IDataStore dataStore)
        {
            var parent = await GetParentAsync(dataStore).ConfigureAwait(false);
            if (parent is StarSystem system)
            {
                return system;
            }
            // Allow a second level of containment for asteroid fields and oort clouds. Custom
            // containment scenarios with deeply nested entities are not considered.
            if (parent is not null)
            {
                parent = await parent.GetParentAsync(dataStore).ConfigureAwait(false);
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
            // Represent the effect of atmospheric convection by resturning the average of the raw
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
        private double GetTemperatureAtPosition(Vector3 position, List<Star> stars)
        {
            // Calculate the heat added to this location by insolation at the given position.
            var insolationHeat = 0.0;
            if (Albedo < 1 && stars.Count > 0)
            {
                var sum = 0.0;
                foreach (var star in stars)
                {
                    sum += Math.Pow(star.Luminosity / (double)position.DistanceSquared(star.Position), 0.25);

                    //sum += star.Temperature
                    //    * (double)Number.Sqrt(star.Shape.ContainingRadius / (2 * position.Distance(star.Position)));
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
                    / (MathAndScience.Constants.Doubles.MathConstants.FourPI
                    * MathAndScience.Constants.Doubles.ScienceConstants.sigma
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
            ? _surfaceTemperatureAtPeriapsis.Lerp(_surfaceTemperatureAtApoapsis, trueAnomaly <= Math.PI ? trueAnomaly / Math.PI : 2 - (trueAnomaly / Math.PI))
            : _blackbodyTemperature;

        /// <summary>
        /// Calculates the temperature at which this <see cref="Planetoid"/> will retain only
        /// a minimal atmosphere of out-gassed volatiles (comparable to Mercury).
        /// </summary>
        /// <returns>A temperature, in K.</returns>
        /// <remarks>
        /// If the planet is not massive enough or too hot to hold onto carbon dioxide gas, it is
        /// presumed that it will have a minimal atmosphere of out-gassed volatiles (comparable to Mercury).
        /// </remarks>
        private double GetTempForThinAtmosphere() => (double)(ScienceConstants.TwoG * Mass * new Number(70594833834763, -18) / Shape.ContainingRadius);

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

        private double ReconstituteHydrosphere(Rehydrator rehydrator)
        {
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
                surfaceTemp = _blackbodyTemperature;
            }

            GenerateHydrosphere(rehydrator, surfaceTemp);

            HasMagnetosphere = _planetParams?.HasMagnetosphere.HasValue == true
                ? _planetParams!.Value.HasMagnetosphere!.Value
                : rehydrator.NextNumber(46) <= Mass * new Number(2.88, -19) / RotationalPeriod * (PlanetType switch
                {
                    PlanetType.Iron => new Number(5),
                    PlanetType.Ocean => Number.Half,
                    _ => Number.One,
                });

            return surfaceTemp;
        }

        private void ReconstituteMaterial(
            Rehydrator rehydrator,
            Vector3 position,
            double? temperature,
            Number semiMajorAxis)
        {
            AxialPrecession = rehydrator.NextDouble(6, MathAndScience.Constants.Doubles.MathConstants.TwoPI);

            GenerateMaterial(
                rehydrator,
                temperature,
                position,
                semiMajorAxis);

            if (_planetParams.HasValue && _planetParams.Value.Albedo.HasValue)
            {
                _surfaceAlbedo = _planetParams.Value.Albedo.Value;
            }
            else if (PlanetType == PlanetType.Comet)
            {
                _surfaceAlbedo = rehydrator.NextDouble(32, 0.025, 0.055);
            }
            else if (PlanetType == PlanetType.AsteroidC)
            {
                _surfaceAlbedo = rehydrator.NextDouble(32, 0.03, 0.1);
            }
            else if (PlanetType == PlanetType.AsteroidM)
            {
                _surfaceAlbedo = rehydrator.NextDouble(32, 0.1, 0.2);
            }
            else if (PlanetType == PlanetType.AsteroidS)
            {
                _surfaceAlbedo = rehydrator.NextDouble(32, 0.1, 0.22);
            }
            else if (PlanetType.Giant.HasFlag(PlanetType))
            {
                _surfaceAlbedo = rehydrator.NextDouble(32, 0.275, 0.35);
            }
            else
            {
                _surfaceAlbedo = rehydrator.NextDouble(32, 0.1, 0.6);
            }
            Albedo = _surfaceAlbedo;
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

        private void SetAngleOfRotation(double angle)
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

        private void SetAxis()
        {
            var precessionQ = System.Numerics.Quaternion.CreateFromYawPitchRoll((float)AxialPrecession, 0, 0);
            var precessionVector = System.Numerics.Vector3.Transform(System.Numerics.Vector3.UnitX, precessionQ);
            var q = System.Numerics.Quaternion.CreateFromAxisAngle(precessionVector, (float)AngleOfRotation);
            Axis = System.Numerics.Vector3.Transform(System.Numerics.Vector3.UnitY, q);
            AxisRotation = System.Numerics.Quaternion.Conjugate(q);

            ResetCachedTemperatures();
        }

        private ulong SetRings(Rehydrator rehydrator)
        {
            if (PlanetType == PlanetType.Comet
                || IsAsteroid
                || IsDwarf)
            {
                return 71;
            }

            var ringChance = IsGiant ? 0.9 : 0.1;
            if (rehydrator.NextDouble(70) > ringChance)
            {
                return 71;
            }

            var innerLimit = (Number)Atmosphere.AtmosphericHeight;

            var outerLimit_Icy = GetRingDistance(_IcyRingDensity);
            if (Orbit != null)
            {
                outerLimit_Icy = Number.Min(outerLimit_Icy, GetHillSphereRadius() / 3);
            }
            if (innerLimit >= outerLimit_Icy)
            {
                return 71;
            }

            var outerLimit_Rocky = GetRingDistance(_RockyRingDensity);
            if (Orbit != null)
            {
                outerLimit_Rocky = Number.Min(outerLimit_Rocky, GetHillSphereRadius() / 3);
            }

            var numRings = IsGiant
                ? (int)Math.Round(rehydrator.PositiveNormalDistributionSample(71, 1, 1), MidpointRounding.AwayFromZero)
                : (int)Math.Round(rehydrator.PositiveNormalDistributionSample(72, 1, 0.1667), MidpointRounding.AwayFromZero);
            var index = 73UL;
            for (var i = 0; i < numRings && innerLimit <= outerLimit_Icy; i++)
            {
                if (innerLimit < outerLimit_Rocky && rehydrator.NextBool(index++))
                {
                    var innerRadius = rehydrator.NextNumber(index++, innerLimit, outerLimit_Rocky);

                    (_rings ??= new List<PlanetaryRing>()).Add(new PlanetaryRing(false, innerRadius, outerLimit_Rocky));

                    outerLimit_Rocky = innerRadius;
                    if (outerLimit_Rocky <= outerLimit_Icy)
                    {
                        outerLimit_Icy = innerRadius;
                    }
                }
                else
                {
                    var innerRadius = rehydrator.NextNumber(index++, innerLimit, outerLimit_Icy);

                    (_rings ??= new List<PlanetaryRing>()).Add(new PlanetaryRing(true, innerRadius, outerLimit_Icy));

                    outerLimit_Icy = innerRadius;
                    if (outerLimit_Icy <= outerLimit_Rocky)
                    {
                        outerLimit_Rocky = innerRadius;
                    }
                }
            }
            return index;
        }

        private void SetTemperatures(List<Star> stars)
        {
            _blackbodyTemperature = GetTemperatureAtPosition(Position, stars);

            if (!Orbit.HasValue)
            {
                _surfaceTemperatureAtApoapsis = _blackbodyTemperature;
                _surfaceTemperatureAtPeriapsis = _blackbodyTemperature;
            }
            else
            {
                // Actual position doesn't matter for temperature, only distance.
                _surfaceTemperatureAtApoapsis = Orbit.Value.Apoapsis.IsInfinite
                    ? _blackbodyTemperature
                    : GetTemperatureAtPosition(Orbit.Value.OrbitedPosition + (Vector3.UnitX * Orbit.Value.Apoapsis), stars);

                // Actual position doesn't matter for temperature, only distance.
                _surfaceTemperatureAtPeriapsis = GetTemperatureAtPosition(Orbit.Value.OrbitedPosition + (Vector3.UnitX * Orbit.Value.Periapsis), stars);
            }

            AverageBlackbodyTemperature = Orbit.HasValue
                ? ((_surfaceTemperatureAtPeriapsis * (1 + Orbit.Value.Eccentricity)) + (_surfaceTemperatureAtApoapsis * (1 - Orbit.Value.Eccentricity))) / 2
                : _blackbodyTemperature;
        }
    }
}
