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
using NeverFoundry.WorldFoundry.WorldGrids;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Threading.Tasks;

namespace NeverFoundry.WorldFoundry.Space
{
    /// <summary>
    /// Any non-stellar celestial body, such as a planet or asteroid.
    /// </summary>
    [Serializable]
    [Newtonsoft.Json.JsonConverter(typeof(NewtonsoftJson.PlanetoidConverter))]
    [System.Text.Json.Serialization.JsonConverter(typeof(PlanetoidConverter))]
    public class Planetoid : CosmicLocation
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

        private const double Second = MathAndScience.Constants.Doubles.MathConstants.PIOver180 / 3600;
        private const double SixteenthPI = Math.PI / 16;

        internal static readonly Number GiantSpace = new Number(2.5, 8);

        private static readonly Number _AsteroidSpace = new Number(400000);
        private static readonly Number _CometSpace = new Number(25000);

        /// <summary>
        /// The minimum to achieve hydrostatic equilibrium and be considered a dwarf planet.
        /// </summary>
        private static readonly Number _DwarfMinMassForType = new Number(3.4, 20);

        private static readonly Number _DwarfSpace = new Number(1.5, 6);

        /// <summary>
        /// Below this limit the planet will not have sufficient mass to retain hydrogen, and will
        /// be a terrestrial planet.
        /// </summary>
        private static readonly Number _GiantMinMassForType = new Number(6, 25);

        /// <summary>
        /// Hadley values are a pure function of latitude, and do not vary with any property of the
        /// planet, atmosphere, or season. Since the calculation is relatively expensive, retrieved
        /// values can be stored for the lifetime of the program for future retrieval for the same
        /// (or very similar) location.
        /// </summary>
        private static readonly Dictionary<double, double> _HadleyValues = new Dictionary<double, double>();

        private static readonly Number _IcyRingDensity = 300;
        private static readonly double _LowTemp = (Substances.All.Water.MeltingPoint ?? 0) - 16;

        /// <summary>
        /// An arbitrary limit separating rogue dwarf planets from rogue planets.
        /// Within orbital systems, a calculated value for clearing the neighborhood is used instead.
        /// </summary>
        private static readonly Number _DwarfMaxMassForType = new Number(6, 25);

        /// <summary>
        /// At around this limit the planet will have sufficient mass to sustain fusion, and become
        /// a brown dwarf.
        /// </summary>
        private static readonly Number _GiantMaxMassForType = new Number(2.5, 28);

        /// <summary>
        /// At around this limit the planet will have sufficient mass to retain hydrogen, and become
        /// a giant.
        /// </summary>
        private static readonly Number _TerrestrialMaxMassForType = new Number(6, 25);

        private static readonly Number _RockyRingDensity = 1380;

        /// <summary>
        /// An arbitrary limit separating rogue dwarf planets from rogue planets. Within orbital
        /// systems, a calculated value for clearing the neighborhood is used instead.
        /// </summary>
        private static readonly Number _TerrestrialMinMassForType = new Number(2, 22);

        private static readonly Number _TerrestrialSpace = new Number(1.75, 7);

        internal readonly bool _earthlike;
        internal readonly HabitabilityRequirements? _habitabilityRequirements;
        internal readonly PlanetParams? _planetParams;

        internal double _blackbodyTemperature;
        internal byte[]? _depthMap;
        internal byte[]? _elevationMap;
        internal byte[]? _flowMap;
        internal byte[][]? _precipitationMaps;
        internal List<string>? _satelliteIDs;
        internal byte[][]? _snowfallMaps;
        internal double _surfaceTemperatureAtApoapsis;
        internal double _surfaceTemperatureAtPeriapsis;
        internal byte[]? _temperatureMapSummer;
        internal byte[]? _temperatureMapWinter;

        private double? _averagePolarSurfaceTemperature;
        private double? _averageSurfaceTemperature;
        private double? _diurnalTemperatureVariation;
        private double? _maxSurfaceTemperature;
        private double? _minSurfaceTemperature;
        private double _normalizedSeaLevel;
        private int _seed1, _seed2, _seed3, _seed4, _seed5, _seed6;
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

        /// <summary>
        /// The axial tilt of the <see cref="Planetoid"/> relative to its orbital plane, in radians.
        /// Values greater than π/2 indicate clockwise rotation. Read-only; set with <see
        /// cref="SetAxialTilt(double)"/>
        /// </summary>
        /// <remarks>
        /// If the <see cref="Planetoid"/> isn't orbiting anything, this is the same as the angle of
        /// rotation.
        /// </remarks>
        public double AxialTilt => Orbit.HasValue ? AngleOfRotation - Orbit.Value.Inclination : AngleOfRotation;

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
        /// Indicates whether this <see cref="Planetoid"/> has a strong magnetosphere.
        /// </summary>
        public bool HasMagnetosphere { get; private set; }

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

        internal double? _maxFlow;
        /// <summary>
        /// The maximum flow rate of waterways on this planetoid, in m³/s.
        /// </summary>
        public double MaxFlow
        {
            get => _maxFlow ?? 350000;
            set => _maxFlow = value.IsNearlyEqualTo(350000) ? (double?)null : value;
        }

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

        private readonly List<Resource> _resources = new List<Resource>();
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

        internal List<SurfaceRegion>? _surfaceRegions;
        /// <summary>
        /// The collection of <see cref="SurfaceRegion"/> instances which describe the surface of
        /// this <see cref="Planetoid"/>.
        /// </summary>
        public IEnumerable<SurfaceRegion> SurfaceRegions => _surfaceRegions ?? Enumerable.Empty<SurfaceRegion>();

        /// <summary>
        /// The total temperature of this <see cref="Planetoid"/>, not including atmosphereic
        /// effects, averaged over its orbit, in K.
        /// </summary>
        internal double AverageBlackbodyTemperature { get; private set; }

        internal double? GreenhouseEffect { get; set; }

        internal bool HasElevationMap => _elevationMap != null;

        internal bool HasHydrologyMaps
            => _depthMap != null
            && _flowMap != null
            && _maxFlow.HasValue;

        internal bool HasPrecipitationMap => _precipitationMaps != null;

        internal bool HasSnowfallMap => _snowfallMaps != null;

        internal bool HasTemperatureMap => _temperatureMapSummer != null || _temperatureMapWinter != null;

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

        private FastNoise? _noise6;
        private FastNoise Noise6 => _noise6 ??= new FastNoise(_seed6, 1.0, FastNoise.NoiseType.Simplex);

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
            bool satellite = false) : base(parent?.Id, CosmicStructureType.Planetoid)
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

            satellites = Configure(parent, stars, star, position, satellite, orbit);

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
            List<SurfaceRegion>? surfaceRegions,
            byte[]? depthMap,
            byte[]? elevationMap,
            byte[]? flowMap,
            byte[][]? precipitationMaps,
            byte[][]? snowfallMaps,
            byte[]? temperatureMapSummer,
            byte[]? temperatureMapWinter,
            double? maxFlow)
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
            _surfaceRegions = surfaceRegions;
            _depthMap = depthMap;
            _elevationMap = elevationMap;
            _flowMap = flowMap;
            _precipitationMaps = precipitationMaps;
            _snowfallMaps = snowfallMaps;
            _temperatureMapSummer = temperatureMapSummer;
            _temperatureMapWinter = temperatureMapWinter;
            _maxFlow = maxFlow;

            AverageBlackbodyTemperature = Orbit.HasValue
                ? ((_surfaceTemperatureAtPeriapsis * (1 + Orbit.Value.Eccentricity)) + (_surfaceTemperatureAtApoapsis * (1 - Orbit.Value.Eccentricity))) / 2
                : _blackbodyTemperature;

            var reconstitution = ReconstituteMaterial(
                position,
                temperature,
                Orbit?.SemiMajorAxis ?? 0);
            ReconstituteHydrosphere(reconstitution);
            GenerateAtmosphere(reconstitution);
            GenerateResources(reconstitution);
        }

        private Planetoid(SerializationInfo info, StreamingContext context) : this(
            (string?)info.GetValue(nameof(Id), typeof(string)) ?? string.Empty,
            (uint?)info.GetValue(nameof(_seed), typeof(uint)) ?? default,
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
            (List<SurfaceRegion>?)info.GetValue(nameof(SurfaceRegions), typeof(List<SurfaceRegion>)),
            (byte[]?)info.GetValue(nameof(_depthMap), typeof(byte[])) ?? default,
            (byte[]?)info.GetValue(nameof(_elevationMap), typeof(byte[])) ?? default,
            (byte[]?)info.GetValue(nameof(_flowMap), typeof(byte[])) ?? default,
            (byte[][]?)info.GetValue(nameof(_precipitationMaps), typeof(byte[][])) ?? default,
            (byte[][]?)info.GetValue(nameof(_snowfallMaps), typeof(byte[][])) ?? default,
            (byte[]?)info.GetValue(nameof(_temperatureMapSummer), typeof(byte[])) ?? default,
            (byte[]?)info.GetValue(nameof(_temperatureMapWinter), typeof(byte[])) ?? default,
            (double?)info.GetValue(nameof(_maxFlow), typeof(double?)))
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
        /// <returns>A planet with the given parameters.</returns>
        public static Planetoid? GetPlanetForSunlikeStar(
            out List<CosmicLocation> children,
            PlanetType planetType = PlanetType.Terrestrial,
            OrbitalParameters? orbit = null,
            PlanetParams? planetParams = null,
            HabitabilityRequirements? habitabilityRequirements = null)
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

        private static byte[] GetByteArray(Bitmap image)
        {
            using var stream = new MemoryStream();
            image.Save(stream, System.Drawing.Imaging.ImageFormat.Bmp);
            return stream.ToArray();
        }

        private static double GetDensity(Reconstitution reconstitution, PlanetType planetType)
        {
            if (planetType == PlanetType.GasGiant)
            {
                // Relatively low chance of a "puffy" giant (Saturn-like, low-density).
                return reconstitution.GetDouble(5) <= 0.2
                    ? reconstitution.GetDouble(6)
                    : reconstitution.GetDouble(7);
            }
            if (planetType == PlanetType.IceGiant)
            {
                // No "puffy" ice giants.
                return reconstitution.GetDouble(7);
            }
            if (planetType == PlanetType.Iron)
            {
                return reconstitution.GetDouble(8);
            }
            if (PlanetType.AnyTerrestrial.HasFlag(planetType))
            {
                return reconstitution.GetDouble(9);
            }

            return planetType switch
            {
                PlanetType.Dwarf => DensityForDwarf,
                PlanetType.LavaDwarf => 4000,
                PlanetType.RockyDwarf => 4000,
                _ => 2000,
            };
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

        /// <summary>
        /// Adds a <see cref="SurfaceRegion"/> instance to this instance's collection. Returns this
        /// instance.
        /// </summary>
        /// <param name="value">A <see cref="SurfaceRegion"/> instance.</param>
        /// <returns>This instance.</returns>
        public Planetoid AddSurfaceRegion(SurfaceRegion value)
        {
            (_surfaceRegions ??= new List<SurfaceRegion>()).Add(value);
            return this;
        }

        /// <summary>
        /// Adds a <see cref="SurfaceRegion"/> instance to this instance's collection. Returns this
        /// instance.
        /// </summary>
        /// <param name="position">The normalized position vector of the center of the
        /// region.</param>
        /// <param name="latitudeRange">
        /// <para>
        /// The range of latitudes encompassed by this region, as an angle (in radians).
        /// </para>
        /// <para>
        /// Maximum value is π (a full hemisphere, which produces the full globe when combined with
        /// the 2:1 aspect ratio of the equirectangular projection).
        /// </para>
        /// </param>
        /// <returns>This instance.</returns>
        public Planetoid AddSurfaceRegion(Vector3 position, Number latitudeRange)
        {
            (_surfaceRegions ??= new List<SurfaceRegion>()).Add(new SurfaceRegion(this, position, latitudeRange));
            return this;
        }

        /// <summary>
        /// Adds a <see cref="SurfaceRegion"/> instance to this instance's collection. Returns this
        /// instance.
        /// </summary>
        /// <param name="latitude">The latitude of the center of the region.</param>
        /// <param name="longitude">The longitude of the center of the region.</param>
        /// <param name="latitudeRange">
        /// <para>
        /// The range of latitudes encompassed by this region, as an angle (in radians).
        /// </para>
        /// <para>
        /// Maximum value is π (a full hemisphere, which produces the full globe when combined with
        /// the 2:1 aspect ratio of the equirectangular projection).
        /// </para>
        /// </param>
        /// <returns>This instance.</returns>
        public Planetoid AddSurfaceRegion(double latitude, double longitude, Number latitudeRange)
        {
            var position = LatitudeAndLongitudeToVector(latitude, longitude);
            (_surfaceRegions ??= new List<SurfaceRegion>()).Add(new SurfaceRegion(
                this,
                new Vector3(
                    position.X,
                    position.Y,
                    position.Z),
                latitudeRange));
            return this;
        }

        /// <summary>
        /// Adds a <see cref="SurfaceRegion"/> instance to this instance's collection. Returns this
        /// instance.
        /// </summary>
        /// <param name="latitude1">The latitude of the northwest corner of the region.</param>
        /// <param name="longitude1">The longitude of the northwest corner of the region.</param>
        /// <param name="latitude2">The latitude of the southeast corner of the region.</param>
        /// <param name="longitude2">The longitude of the southeast corner of the region.</param>
        /// <returns>This instance.</returns>
        public Planetoid AddSurfaceRegion(double latitude1, double longitude1, double latitude2, double longitude2)
        {
            var latitudeRange = latitude1 - latitude2;
            var centerLat = latitude1 + ((latitude1 - latitude2) / 2);
            var centerLon = longitude1 + ((longitude1 - longitude2) / 2);
            var position = LatitudeAndLongitudeToVector(centerLat, centerLon);
            (_surfaceRegions ??= new List<SurfaceRegion>()).Add(new SurfaceRegion(
                this,
                new Vector3(
                    position.X,
                    position.Y,
                    position.Z),
                Math.Abs(latitudeRange)));
            return this;
        }

        /// <summary>
        /// Removes this location and all contained children, as well as all satellites, from the
        /// given data store.
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
        /// <returns>The atmospheric density for the given conditions, in kg/m³.</returns>
        public double GetAtmosphericDensity(Instant moment, double latitude, double longitude, double altitude)
        {
            var surfaceTemp = GetSurfaceTemperatureAtLatLon(moment, latitude, longitude);
            var tempAtElevation = GetTemperatureAtElevation(surfaceTemp, altitude);
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
        /// <returns>The atmospheric drag on the object at the specified height, in N.</returns>
        /// <remarks>
        /// 0.47 is an arbitrary drag coefficient (that of a sphere in a fluid with a Reynolds
        /// number of 10⁴), which may not reflect the actual conditions at all, but the inaccuracy
        /// is accepted since the level of detailed information needed to calculate this value
        /// accurately is not desired in this library.
        /// </remarks>
        public double GetAtmosphericDrag(Instant moment, double latitude, double longitude, double altitude, double speed)
        {
            var surfaceTemp = GetSurfaceTemperatureAtLatLon(moment, latitude, longitude);
            var tempAtElevation = GetTemperatureAtElevation(surfaceTemp, altitude);
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
        /// <returns>The atmospheric pressure at the specified height, in kPa.</returns>
        /// <remarks>
        /// In an Earth-like atmosphere, the pressure lapse rate varies considerably in the
        /// different atmospheric layers, but this cannot be easily modeled for arbitrary
        /// exoplanetary atmospheres, so the simple barometric formula is used, which should be
        /// "close enough" for the purposes of this library. Also, this calculation uses the molar
        /// mass of air on Earth, which is clearly not correct for other atmospheres, but is
        /// considered "close enough" for the purposes of this library.
        /// </remarks>
        public double GetAtmosphericPressure(Instant moment, double latitude, double longitude)
        {
            var elevation = GetElevationAt(latitude, longitude);
            var tempAtElevation = GetTemperatureAtElevation(
                GetSurfaceTemperature(
                    _blackbodyTemperature,
                    latitude,
                    Orbit?.GetTrueAnomalyAtTime(moment) ?? 0),
                elevation);
            return GetAtmosphericPressureFromTempAndElevation(tempAtElevation, elevation);
        }

        /// <summary>
        /// Get the average surface temperature of this <see cref="Planetoid"/> near its poles
        /// throughout its orbit (or at its current position, if it is not in orbit), in K.
        /// </summary>
        public double GetAveragePolarSurfaceTemperature()
        {
            _averagePolarSurfaceTemperature ??= GetAverageTemperature(true);
            return _averagePolarSurfaceTemperature.Value;
        }

        /// <summary>
        /// The average surface temperature of the <see cref="Planetoid"/> near its equator
        /// throughout its orbit (or at its current position, if it is not in orbit), in K.
        /// </summary>
        public double GetAverageSurfaceTemperature()
        {
            _averageSurfaceTemperature ??= GetAverageTemperature();
            return _averageSurfaceTemperature.Value;
        }

        /// <summary>
        /// Determines the smallest child <see cref="SurfaceRegion"/> at any level of this
        /// instance's descendant hierarchy which contains the specified <paramref
        /// name="position"/>.
        /// </summary>
        /// <param name="position">The position whose smallest containing <see
        /// cref="SurfaceRegion"/> is to be determined.</param>
        /// <returns>
        /// The smallest <see cref="SurfaceRegion"/> at any level of this instance's descendant
        /// hierarchy which contains the specified <paramref name="position"/>, or <see
        /// langword="null"/>, if no region contains the position.
        /// </returns>
        public SurfaceRegion? GetContainingSurfaceRegion(Vector3 position)
            => SurfaceRegions.Where(x => x.Shape.IsPointWithin(position))
            .ItemWithMin(x => x.Shape.ContainingRadius);

        /// <summary>
        /// Determines the smallest child <see cref="SurfaceRegion"/> at any level of this
        /// instance's descendant hierarchy which fully contains the specified <see
        /// cref="Location"/> within its containing radius.
        /// </summary>
        /// <param name="other">The <see cref="Location"/> whose smallest containing <see
        /// cref="SurfaceRegion"/> is to be determined.</param>
        /// <returns>
        /// The smallest <see cref="SurfaceRegion"/> at any level of this instance's descendant
        /// hierarchy which fully contains the specified <see cref="Location"/> within its
        /// containing radius, or <see langword="null"/>, if no region contains the position.
        /// </returns>
        public SurfaceRegion? GetContainingSurfaceRegion(Location other)
            => SurfaceRegions.Where(x => Vector3.Distance(x.Position, other.Position) <= x.Shape.ContainingRadius - other.Shape.ContainingRadius)
            .ItemWithMin(x => x.Shape.ContainingRadius);

        /// <summary>
        /// Gets the stored hydrology depth map image for this region, if any.
        /// </summary>
        /// <returns>The stored hydrology depth map image for this region, if any.</returns>
        public Bitmap? GetDepthMap() => _depthMap.ToImage();

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
        /// Gets the elevation at the given <paramref name="position"/>, in meters.
        /// </summary>
        /// <param name="position">The position at which to determine elevation.</param>
        /// <returns>The elevation at the given <paramref name="position"/>, in meters.</returns>
        public double GetElevationAt(Vector3 position) => GetElevationAt(VectorToLatitude(position), VectorToLongitude(position));

        /// <summary>
        /// Gets the elevation at the given <paramref name="latitude"/> and <paramref
        /// name="longitude"/>, in meters.
        /// </summary>
        /// <param name="latitude">The latitude at which to determine elevation.</param>
        /// <param name="longitude">The longitude at which to determine elevation.</param>
        /// <returns>The elevation at the given <paramref name="latitude"/> and <paramref
        /// name="longitude"/>, in meters.</returns>
        public double GetElevationAt(double latitude, double longitude)
            => GetNormalizedElevationAt(latitude, longitude) * MaxElevation;

        /// <summary>
        /// Gets the stored elevation map image for this region, if any.
        /// </summary>
        /// <returns>The stored elevation map image for this region, if any.</returns>
        public Bitmap? GetElevationMap() => _elevationMap.ToImage();

        /// <summary>
        /// Gets the stored hydrology flow map image for this region, if any.
        /// </summary>
        /// <returns>The stored hydrology flow map image for this region, if any.</returns>
        public Bitmap? GetFlowMap() => _flowMap.ToImage();

        /// <summary>
        /// Gets the greenhouse effect of this planet's atmosphere.
        /// </summary>
        /// <returns></returns>
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
        /// Determines if the given position is mountainous (see Remarks).
        /// </summary>
        /// <param name="latitude">The latitude of the position to check.</param>
        /// <param name="longitude">The longitude of the position to check.</param>
        /// <returns><see langword="true"/> if the given position is mountainous; otherwise
        /// <see langword="false"/>.</returns>
        /// <remarks>
        /// "Mountainous" is defined as having a maximum elevation greater than 8.5% of the maximum
        /// elevation of this planet, or a maximum elevation greater than 5% of the maximum and a
        /// slope greater than 0.035, or a maximum elevation greater than 3.5% of the maximum and a
        /// slope greater than 0.0875.
        /// </remarks>
        public bool GetIsMountainous(double latitude, double longitude)
        {
            var position = LatitudeAndLongitudeToDoubleVector(latitude, longitude);
            var elevation = GetNormalizedElevationAt(latitude, longitude);

            if (elevation < 0.035)
            {
                return false;
            }
            if (elevation > 0.085)
            {
                return true;
            }
            var slope = GetSlope(position, latitude, longitude, elevation);
            if (elevation > 0.05)
            {
                return slope > 0.035;
            }
            return slope > 0.0875;
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
        /// Gets the approximate maximum surface temperature of this <see cref="Planetoid"/>, in K.
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
        /// Gets the approximate minimum surface temperature of this <see cref="Planetoid"/>, in K.
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

        /// <summary>
        /// Gets the elevation at the given <paramref name="position"/>, as a normalized value
        /// between -1 and 1, where 1 is the maximum elevation of the planet. Negative values are
        /// below sea level.
        /// <seealso cref="MaxElevation"/>
        /// </summary>
        /// <param name="position">A normalized position vector representing a direction from the
        /// center of the <see cref="Planetoid"/>.</param>
        /// <returns>The elevation at the given <paramref name="position"/>, as a normalized value
        /// between -1 and 1, where 1 is the maximum elevation of the planet. Negative values are
        /// below sea level.</returns>
        public double GetNormalizedElevationAt(Vector3 position)
            => GetNormalizedElevationAt((double)position.X, (double)position.Y, (double)position.Z);

        /// <summary>
        /// Gets the elevation at the given <paramref name="position"/>, as a normalized value
        /// between -1 and 1, where 1 is the maximum elevation of the planet. Negative values are
        /// below sea level.
        /// <seealso cref="MaxElevation"/>
        /// </summary>
        /// <param name="position">A normalized position vector representing a direction from the
        /// center of the <see cref="Planetoid"/>.</param>
        /// <returns>The elevation at the given <paramref name="position"/>, as a normalized value
        /// between -1 and 1, where 1 is the maximum elevation of the planet. Negative values are
        /// below sea level.</returns>
        public double GetNormalizedElevationAt(MathAndScience.Numerics.Doubles.Vector3 position)
            => GetNormalizedElevationAt(position.X, position.Y, position.Z);

        /// <summary>
        /// Gets the elevation at the given <paramref name="position"/>, as a normalized value
        /// between -1 and 1, where 1 is the maximum elevation of the planet. Negative values are
        /// below sea level.
        /// <seealso cref="MaxElevation"/>
        /// </summary>
        /// <param name="position">A normalized position vector representing a direction from the
        /// center of the <see cref="Planetoid"/>.</param>
        /// <returns>The elevation at the given <paramref name="position"/>, as a normalized value
        /// between -1 and 1, where 1 is the maximum elevation of the planet. Negative values are
        /// below sea level.</returns>
        public double GetNormalizedElevationAt(System.Numerics.Vector3 position)
            => GetNormalizedElevationAt(position.X, position.Y, position.Z);

        /// <summary>
        /// <para>
        /// Gets the elevation at the given <paramref name="latitude"/> and <paramref
        /// name="longitude"/>, as a normalized value between -1 and 1, where 1 is the maximum
        /// elevation of the planet. Negative values are below sea level.
        /// </para>
        /// <para>
        /// See also <seealso cref="MaxElevation"/>.
        /// </para>
        /// </summary>
        /// <param name="latitude">The latitude at which to determine elevation.</param>
        /// <param name="longitude">The longitude at which to determine elevation.</param>
        /// <returns>
        /// The elevation at the given <paramref name="latitude"/> and <paramref name="longitude"/>,
        /// as a normalized value between -1 and 1, where 1 is the maximum elevation of the planet.
        /// Negative values are below sea level.
        /// </returns>
        public double GetNormalizedElevationAt(double latitude, double longitude)
            => GetNormalizedElevationAt(LatitudeAndLongitudeToVector(latitude, longitude));

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
            info.AddValue(nameof(_seed), _seed);
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
            info.AddValue(nameof(SurfaceRegions), _surfaceRegions);
            info.AddValue(nameof(_depthMap), _depthMap);
            info.AddValue(nameof(_elevationMap), _elevationMap);
            info.AddValue(nameof(_flowMap), _flowMap);
            info.AddValue(nameof(_precipitationMaps), _precipitationMaps);
            info.AddValue(nameof(_snowfallMaps), _snowfallMaps);
            info.AddValue(nameof(_temperatureMapSummer), _temperatureMapSummer);
            info.AddValue(nameof(_temperatureMapWinter), _temperatureMapWinter);
            info.AddValue(nameof(_maxFlow), _maxFlow);
        }

        /// <summary>
        /// Determines the average precipitation at the given <paramref name="position"/> under the
        /// given conditions, over the given duration, in mm.
        /// </summary>
        /// <param name="moment">
        /// The beginning of the period during which precipitation is to be determined.
        /// </param>
        /// <param name="position">
        /// The position at which to determine precipitation.
        /// </param>
        /// <param name="proportionOfYear">
        /// The proportion of the year over which to determine precipitation.
        /// </param>
        /// <returns>
        /// <para>
        /// The average precipitation at the given <paramref name="position"/> and time of year, in
        /// mm, along with the amount of snow which falls.
        /// </para>
        /// <para>
        /// Note that this amount <i>replaces</i> any precipitation that would have fallen as rain;
        /// the return value is to be considered a water-equivalent total value which is equal to
        /// the snow.
        /// </para>
        /// </returns>
        public (double precipitation, double snow) GetPrecipitation(Instant moment, Vector3 position, float proportionOfYear)
        {
            var trueAnomaly = Orbit?.GetTrueAnomalyAtTime(moment) ?? 0;
            var seasonalLatitude = Math.Abs(GetSeasonalLatitude(VectorToLatitude(position), trueAnomaly));
            var temp = GetSurfaceTemperatureAtTrueAnomaly(trueAnomaly, seasonalLatitude);
            temp = GetTemperatureAtElevation(temp, GetElevationAt(position));
            var precipitation = GetPrecipitation(
                (double)position.X,
                (double)position.Y,
                (double)position.Z,
                seasonalLatitude,
                (float)temp,
                proportionOfYear,
                out var snow);
            return (precipitation, snow);
        }

        /// <summary>
        /// Determines the average precipitation at the given <paramref name="latitude"/> and
        /// <paramref name="longitude"/> under the given conditions, over the given duration, in mm.
        /// </summary>
        /// <param name="moment">
        /// The beginning of the period during which precipitation is to be determined.
        /// </param>
        /// <param name="latitude">
        /// The latitude at which to determine precipitation.
        /// </param>
        /// <param name="longitude">
        /// The longitude at which to determine precipitation.
        /// </param>
        /// <param name="proportionOfYear">
        /// The proportion of the year over which to determine precipitation.
        /// </param>
        /// <returns>
        /// <para>
        /// The average precipitation at the given <paramref name="latitude"/> and <paramref
        /// name="longitude"/> and time of year, in mm, along with the amount of snow which falls.
        /// </para>
        /// <para>
        /// Note that this amount <i>replaces</i> any precipitation that would have fallen as rain;
        /// the return value is to be considered a water-equivalent total value which is equal to
        /// the snow.
        /// </para>
        /// </returns>
        public (double precipitation, double snow) GetPrecipitation(Instant moment, double latitude, double longitude, float proportionOfYear)
        {
            var position = LatitudeAndLongitudeToVector(latitude, longitude);
            var trueAnomaly = Orbit?.GetTrueAnomalyAtTime(moment) ?? 0;
            var seasonalLatitude = Math.Abs(GetSeasonalLatitude(latitude, trueAnomaly));
            var temp = GetSurfaceTemperatureAtTrueAnomaly(trueAnomaly, seasonalLatitude);
            temp = GetTemperatureAtElevation(temp, GetElevationAt(position));
            var precipitation = GetPrecipitation(
                (double)position.X,
                (double)position.Y,
                (double)position.Z,
                seasonalLatitude,
                (float)temp,
                proportionOfYear,
                out var snow);
            return (precipitation, snow);
        }

        /// <summary>
        /// Gets the stored set of precipitation map images for this region, if any.
        /// </summary>
        /// <returns>The stored set of precipitation map images for this region, if any.</returns>
        public Bitmap[] GetPrecipitationMaps()
        {
            if (_precipitationMaps is null)
            {
                return new Bitmap[0];
            }
            var maps = new Bitmap[_precipitationMaps.Length];
            for (var i = 0; i < _precipitationMaps.Length; i++)
            {
                maps[i] = _precipitationMaps[i].ToImage()!;
            }
            return maps;
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
        /// one of this one's satellites, the return value will always be <c>(0.0, <see
        /// langword="false"/>)</c>.</param>
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
        /// Determines the proportion of a year, with 0 indicating winter, and 1 indicating summer,
        /// at the given <paramref name="moment"/>.
        /// </summary>
        /// <param name="moment">The time at which to make the calculation.</param>
        /// <param name="latitude">Used to determine hemisphere.</param>
        /// <returns>The proportion of the year, with 0 indicating winter, and 1 indicating summer,
        /// at the given <paramref name="moment"/>.</returns>
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
            return proportionOfYear;
        }

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
        /// Calculates the slope at the given coordinates, as the ratio of rise over run from the
        /// point to the point 1 arc-second away in the cardinal direction which is at the steepest
        /// angle.
        /// </summary>
        /// <param name="latitude">The latitude of the point.</param>
        /// <param name="longitude">The longitude of the point.</param>
        /// <returns>The slope at the given coordinates.</returns>
        public double GetSlope(double latitude, double longitude)
            => GetSlope(LatitudeAndLongitudeToDoubleVector(latitude, longitude), latitude, longitude, GetNormalizedElevationAt(latitude, longitude));

        /// <summary>
        /// Gets the stored set of snowfall map images for this region, if any.
        /// </summary>
        /// <returns>The stored set of snowfall map images for this region, if any.</returns>
        public Bitmap[] GetSnowfallMaps()
        {
            if (_snowfallMaps is null)
            {
                return new Bitmap[0];
            }
            var maps = new Bitmap[_snowfallMaps.Length];
            for (var i = 0; i < _snowfallMaps.Length; i++)
            {
                maps[i] = _snowfallMaps[i].ToImage()!;
            }
            return maps;
        }

        /// <summary>
        /// Gets the current surface temperature of the <see cref="Planetoid"/> at its equator, in K.
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
        /// Calculates the effective surface temperature at the given surface position, including
        /// greenhouse effects, in K.
        /// </summary>
        /// <param name="moment">The time at which to make the calculation.</param>
        /// <param name="latitude">
        /// The latitude at which temperature will be calculated.
        /// </param>
        /// <param name="longitude">
        /// The longitude at which temperature will be calculated.
        /// </param>
        /// <returns>The surface temperature, in K.</returns>
        public double GetSurfaceTemperatureAtLatLon(Instant moment, double latitude, double longitude)
            => GetTemperatureAtElevation(
                GetSurfaceTemperature(_blackbodyTemperature, latitude, Orbit?.GetTrueAnomalyAtTime(moment) ?? 0),
                GetElevationAt(latitude, longitude));

        /// <summary>
        /// Calculates the effective surface temperature at the given surface position, including
        /// greenhouse effects, in K.
        /// </summary>
        /// <param name="moment">The time at which to make the calculation.</param>
        /// <param name="position">
        /// The surface position at which temperature will be calculated.
        /// </param>
        /// <returns>The surface temperature, in K.</returns>
        public double GetSurfaceTemperatureAtSurfacePosition(Instant moment, Vector3 position)
            => GetSurfaceTemperatureAtLatLon(moment, VectorToLatitude(position), VectorToLongitude(position));

        /// <summary>
        /// Calculates the range of temperatures at the given <paramref name="latitude"/> and
        /// <paramref name="elevation"/>, from winter to summer, in K.
        /// </summary>
        /// <param name="latitude">The latitude at which to calculate the temperature range, in
        /// radians.</param>
        /// <param name="elevation">The elevation at which to calculate the temperature range, in
        /// meters.</param>
        /// <returns>A <see cref="FloatRange"/> giving the range of temperatures at the given
        /// <paramref name="latitude"/> and <paramref name="elevation"/>, from winter to summer, in
        /// K.</returns>
        public FloatRange GetSurfaceTemperatureRangeAt(double latitude, double elevation)
        {
            var temp = GetSurfaceTemperatureAtTrueAnomaly(WinterSolsticeTrueAnomaly, GetSeasonalLatitudeFromDeclination(latitude, -AxialTilt));
            var min = GetTemperatureAtElevation(temp, elevation);
            temp = GetSurfaceTemperatureAtTrueAnomaly(SummerSolsticeTrueAnomaly, GetSeasonalLatitudeFromDeclination(latitude, AxialTilt));
            var max = GetTemperatureAtElevation(temp, elevation);
            return new FloatRange((float)min, (float)max);
        }

        /// <summary>
        /// Calculates the temperature of this <see cref="Planetoid"/> at the given elevation, in K.
        /// </summary>
        /// <param name="surfaceTemp">The surface temperature at the location, in K.</param>
        /// <param name="elevation">The elevation, in meters.</param>
        /// <returns>
        /// The temperature of this <see cref="Planetoid"/> at the given elevation, in K.
        /// </returns>
        /// <remarks>
        /// In an Earth-like atmosphere, the temperature lapse rate varies considerably in the
        /// different atmospheric layers, but this cannot be easily modeled for arbitrary
        /// exoplanetary atmospheres, so a simplified formula is used, which should be "close enough"
        /// for low elevations.
        /// </remarks>
        public double GetTemperatureAtElevation(double surfaceTemp, double elevation)
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
                var firstGuess = surfaceTemp - (elevation * GetLapseRate(surfaceTemp));
                return surfaceTemp - (elevation * GetLapseRate(firstGuess));
            }
        }

        /// <summary>
        /// Gets the stored temperature map image for this region at the summer solstice, if any.
        /// </summary>
        /// <returns>The stored temperature map image for this region at the summer solstice, if
        /// any.</returns>
        public Bitmap? GetTemperatureMapSummer() => (_temperatureMapSummer ?? _temperatureMapWinter).ToImage();

        /// <summary>
        /// Gets the stored temperature map image for this region at the winter solstice, if any.
        /// </summary>
        /// <returns>The stored temperature map image for this region at the winter solstice, if
        /// any.</returns>
        public Bitmap? GetTemperatureMapWinter() => (_temperatureMapWinter ?? _temperatureMapSummer).ToImage();

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
        /// Loads an image as the hydrology depth overlay for this region.
        /// </summary>
        /// <param name="image">The image to load.</param>
        public void LoadDepthMap(Bitmap image)
        {
            if (image is null)
            {
                _depthMap = null;
                return;
            }
            _depthMap = GetByteArray(image);
        }

        /// <summary>
        /// Loads an image as the elevation overlay for this region.
        /// </summary>
        /// <param name="image">The image to load.</param>
        public void LoadElevationMap(Bitmap image)
        {
            if (image is null)
            {
                _elevationMap = null;
                return;
            }
            _elevationMap = GetByteArray(image);
        }

        /// <summary>
        /// Loads an image as the hydrology flow overlay for this region.
        /// </summary>
        /// <param name="image">The image to load.</param>
        /// <param name="maxFlow">
        /// The maximum flow rate of waterways on this map, in m³/s.
        /// </param>
        public void LoadFlowMap(Bitmap image, double maxFlow)
        {
            if (image is null)
            {
                _flowMap = null;
                return;
            }
            _flowMap = GetByteArray(image);
            MaxFlow = maxFlow;
        }

        /// <summary>
        /// Loads a set of images as the precipitation overlays for this region.
        /// </summary>
        /// <param name="images">The images to load. The set is presumed to be evenly spaced over the
        /// course of a year.</param>
        public void LoadPrecipitationMaps(IEnumerable<Bitmap> images)
        {
            if (images?.Any() != true)
            {
                _precipitationMaps = null;
                return;
            }
            _precipitationMaps = images.Select(GetByteArray).ToArray();
        }

        /// <summary>
        /// Loads a set of images as the snowfall overlays for this region.
        /// </summary>
        /// <param name="images">The images to load. The set is presumed to be evenly spaced over the
        /// course of a year.</param>
        public void LoadSnowfallMaps(IEnumerable<Bitmap> images)
        {
            if (images?.Any() != true)
            {
                _snowfallMaps = null;
                return;
            }
            _snowfallMaps = images.Select(GetByteArray).ToArray();
        }

        /// <summary>
        /// Loads an image as the temperature overlay for this region, applying the same map to both
        /// summer and winter.
        /// </summary>
        /// <param name="image">The image to load.</param>
        public void LoadTemperatureMap(Bitmap image)
        {
            if (image is null)
            {
                _temperatureMapSummer = null;
                _temperatureMapWinter = null;
                return;
            }
            _temperatureMapSummer = GetByteArray(image);
            _temperatureMapWinter = _temperatureMapSummer;
        }

        /// <summary>
        /// Loads an image as the temperature overlay for this region at the summer solstice.
        /// </summary>
        /// <param name="image">The image to load.</param>
        public void LoadTemperatureMapSummer(Bitmap image)
        {
            if (image is null)
            {
                _temperatureMapSummer = null;
                return;
            }
            _temperatureMapSummer = GetByteArray(image);
        }

        /// <summary>
        /// Loads an image as the temperature overlay for this region at the winter solstice.
        /// </summary>
        /// <param name="image">The image to load.</param>
        public void LoadTemperatureMapWinter(Bitmap image)
        {
            if (image is null)
            {
                _temperatureMapWinter = null;
                return;
            }
            _temperatureMapWinter = GetByteArray(image);
        }

        /// <summary>
        /// Removes a <see cref="SurfaceRegion"/> instance from this instance's collection, if
        /// found. Returns this instance.
        /// </summary>
        /// <param name="value">A <see cref="SurfaceRegion"/> instance.</param>
        /// <returns>This instance.</returns>
        public Planetoid RemoveSurfaceRegion(SurfaceRegion value)
        {
            _surfaceRegions?.Remove(value);
            return this;
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

        internal float[][] GetDepthMap(int width, int height)
            => _depthMap.ImageToFloatSurfaceMap(width, height);

        internal double[][] GetElevationMap(int width, int height, out double max)
        {
            max = 0;
            using var image = _elevationMap.ToImage();
            if (image is null)
            {
                return new double[0][];
            }
            return image.ImageToDoubleSurfaceMap(out max, width, height);
        }

        internal double GetElevationNoiseAt(MathAndScience.Numerics.Doubles.Vector3 position)
            => GetElevationNoiseAt(position.X, position.Y, position.Z);

        internal double GetElevationNoiseAt(double x, double y, double z)
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
            var scaledBaseNoise = baseNoise * (0.25 + (mountains * 0.0625));

            // Modify the mountain map to indicate mountains only in random areas, instead of
            // uniformly across the globe.
            //mountains *= (Noise3.GetNoise(x, y, z) + 0.25).Clamp(0, 1);
            mountains *= (Noise3.GetNoise(x, y, z) + 1).Clamp(0, 1);

            // Multiply with itself to produce predominantly low values with high (and low)
            // extremes, and scale to the typical maximum height of mountains, with a degree of
            // randomness borrowed from the base noise function.
            mountains = Math.CopySign(mountains * mountains * (0.525 + (baseNoise * 0.13125)), mountains);

            // The value with the greatest magnitude is returned, resulting in mostly broad,
            // low-lying areas, interrupted by occasional high mountain ranges and low trenches.
            return scaledBaseNoise + mountains;
        }

        internal float[][] GetFlowMap(int width, int height)
            => _flowMap.ImageToFloatSurfaceMap(width, height);

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

        internal double GetPrecipitation(MathAndScience.Numerics.Doubles.Vector3 position, double seasonalLatitude, float temperature, float proportionOfYear, out double snow)
            => GetPrecipitation(position.X, position.Y, position.Z, seasonalLatitude, temperature, proportionOfYear, out snow);

        internal double GetPrecipitation(double x, double y, double z, double seasonalLatitude, float temperature, float proportionOfYear, out double snow)
        {
            snow = 0;

            var avgPrecipitation = Atmosphere.AveragePrecipitation * proportionOfYear;

            // Noise map with smooth, broad areas. Random range ~-1-2.
            var r1 = 0.5 + (Noise4.GetNoise(x, y, z) * 1.5);

            // More detailed noise map. Random range of ~-0.9-0.9 adjusted to ~0.45-1.25.
            var r2 = (Noise5.GetNoise(x, y, z) * 0.4) + 0.85;

            // Combined map is noise with broad similarity over regions, and minor local
            // diversity. Range ~-1.05-2.1.
            var r = r1 * r2;

            // Hadley cells scale by ~2.25 around the equator, ~-1.1 ±15º lat, ~1.1 ±40º lat, and ~0
            // ±86º lat; this creates the ITCZ, the subtropical deserts, the temperate zone, and
            // the polar deserts.
            var roundedAbsLatitude = Math.Round(Math.Max(0, Math.Abs(seasonalLatitude)), 3);
            if (!_HadleyValues.TryGetValue(roundedAbsLatitude, out var hadleyValue))
            {
                hadleyValue = 0.25 + (2 / (1 + (2 * roundedAbsLatitude)) * Math.Cos(MathAndScience.Constants.Doubles.MathConstants.ThreePI / (roundedAbsLatitude + 0.75)));
                _HadleyValues.Add(roundedAbsLatitude, hadleyValue);
            }

            // Noise map with very smooth, broad areas. Random range of ~0.5-1.
            var r3 = 0.75 + (Noise6.GetNoise(x, y, z) * 0.25);

            // Relative humidity is the Hadley cell value added to the random value. Range ~-2.15-~4.35.
            var relativeHumidity = r + (hadleyValue * r3);

            // In the range betwen 0 and 16K below freezing, the value is scaled down; below that
            // range it is cut off completely; above it is unchanged.
            relativeHumidity *= ((temperature - _LowTemp) / 16).Clamp(0, 1);

            // More intense in the tropics.
            if (roundedAbsLatitude < MathAndScience.Constants.Doubles.MathConstants.EighthPI)
            {
                // Range ~-3.16-~6.45.
                relativeHumidity += r * ((MathAndScience.Constants.Doubles.MathConstants.EighthPI - roundedAbsLatitude) / MathAndScience.Constants.Doubles.MathConstants.EighthPI);

                // Extreme spikes within the ITCZ. Range ~-3.16-~71.
                if (relativeHumidity > 0 && roundedAbsLatitude < SixteenthPI)
                {
                    relativeHumidity *= r2
                        * (1 + ((SixteenthPI - roundedAbsLatitude) / SixteenthPI
                        * (Math.Max(0, r2 - 0.85) / 0.4)
                        * 11));
                }
            }

            if (relativeHumidity <= 0)
            {
                return 0;
            }

            var precipitation = avgPrecipitation * relativeHumidity;

            if (temperature <= Substances.All.Water.MeltingPoint)
            {
                snow = precipitation * Atmosphere.SnowToRainRatio;
            }

            return precipitation;
        }

        internal float[][][] GetPrecipitationMaps(int width, int height)
        {
            var mapImages = GetPrecipitationMaps();
            var maps = new float[mapImages.Length][][];
            for (var i = 0; i < mapImages.Length; i++)
            {
                maps[i] = mapImages[i].ImageToFloatSurfaceMap(width, height);
                mapImages[i].Dispose();
            }
            return maps;
        }

        internal double GetSeasonalLatitudeFromDeclination(double latitude, double solarDeclination)
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

        internal float[][][] GetSnowfallMaps(int width, int height)
        {
            var mapImages = GetSnowfallMaps();
            var maps = new float[mapImages.Length][][];
            for (var i = 0; i < mapImages.Length; i++)
            {
                maps[i] = mapImages[i].ImageToFloatSurfaceMap(width, height);
                mapImages[i].Dispose();
            }
            return maps;
        }

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
            => GetSeasonalSurfaceTemperature(GetTemperatureAtTrueAnomaly(trueAnomaly), seasonalLatitude, trueAnomaly);

        internal FloatRange[][] GetTemperatureMap(int width, int height)
        {
            using var winter = GetTemperatureMapWinter();
            using var summer = GetTemperatureMapSummer();
            if (winter is null || summer is null)
            {
                return new FloatRange[0][];
            }
            return winter.ImagesToFloatRangeSurfaceMap(summer, width, height);
        }

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
            Reconstitution reconstitution,
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
                    reconstitution,
                    sub,
                    proportionInHydrosphere,
                    vaporProportion,
                    ref adjustedAtmosphericPressure);
            }

            if (isWater && _planetParams?.EarthlikeAtmosphere != true)
            {
                CheckCO2Reduction(reconstitution, vaporPressure);
            }

            return adjustedAtmosphericPressure;
        }

        private double CalculatePhases(Reconstitution reconstitution, int counter, double adjustedAtmosphericPressure)
        {
            var surfaceTemp = GetAverageSurfaceTemperature();

            // Despite the theoretical possibility of an atmosphere cold enough to precipitate some
            // of the noble gases, or hydrogen, they are ignored and presumed to exist always as
            // trace atmospheric gases, never surface liquids or ices, or in large enough quantities
            // to form precipitation.

            var methane = Substances.All.Methane.GetHomogeneousReference();
            adjustedAtmosphericPressure = CalculateGasPhaseMix(reconstitution, methane, surfaceTemp, adjustedAtmosphericPressure);

            var carbonMonoxide = Substances.All.CarbonMonoxide.GetHomogeneousReference();
            adjustedAtmosphericPressure = CalculateGasPhaseMix(reconstitution, carbonMonoxide, surfaceTemp, adjustedAtmosphericPressure);

            var carbonDioxide = Substances.All.CarbonDioxide.GetHomogeneousReference();
            adjustedAtmosphericPressure = CalculateGasPhaseMix(reconstitution, carbonDioxide, surfaceTemp, adjustedAtmosphericPressure);

            var nitrogen = Substances.All.Nitrogen.GetHomogeneousReference();
            adjustedAtmosphericPressure = CalculateGasPhaseMix(reconstitution, nitrogen, surfaceTemp, adjustedAtmosphericPressure);

            var oxygen = Substances.All.Oxygen.GetHomogeneousReference();
            adjustedAtmosphericPressure = CalculateGasPhaseMix(reconstitution, oxygen, surfaceTemp, adjustedAtmosphericPressure);

            // No need to check for ozone, since it is only added to atmospheres on planets with
            // liquid surface water, which means temperatures too high for liquid or solid ozone.

            var sulphurDioxide = Substances.All.SulphurDioxide.GetHomogeneousReference();
            adjustedAtmosphericPressure = CalculateGasPhaseMix(reconstitution, sulphurDioxide, surfaceTemp, adjustedAtmosphericPressure);

            // Water is handled differently, since the planet may already have surface water.
            if (counter > 0) // Not performed on first pass, since it was already done.
            {
                var water = Substances.All.Water.GetHomogeneousReference();
                var seawater = Substances.All.Seawater.GetHomogeneousReference();
                if (Hydrosphere.Contains(water)
                    || Hydrosphere.Contains(seawater)
                    || Atmosphere.Material.Contains(water))
                {
                    adjustedAtmosphericPressure = CalculateGasPhaseMix(reconstitution, water, surfaceTemp, adjustedAtmosphericPressure);
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
                    adjustedAtmosphericPressure = CalculatePhases(reconstitution, counter + 1, adjustedAtmosphericPressure);
                }
            }

            return adjustedAtmosphericPressure;
        }

        private void CheckCO2Reduction(Reconstitution reconstitution, double vaporPressure)
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
                    co2 = reconstitution.GetDecimal(62);

                    // Replace most of the CO2 with inert gases.
                    var nitrogen = Substances.All.Nitrogen.GetHomogeneousReference();
                    var n2 = Atmosphere.Material.GetProportion(nitrogen) + Atmosphere.Material.GetProportion(carbonDioxide) - co2;
                    Atmosphere.Material.AddConstituent(carbonDioxide, co2);

                    // Some portion of the N2 may be Ar instead.
                    var argon = Substances.All.Argon.GetHomogeneousReference();
                    var ar = Math.Max(Atmosphere.Material.GetProportion(argon), n2 * reconstitution.GetDecimal(63));
                    Atmosphere.Material.AddConstituent(argon, ar);
                    n2 -= ar;

                    // An even smaller fraction may be Kr.
                    var krypton = Substances.All.Krypton.GetHomogeneousReference();
                    var kr = Math.Max(Atmosphere.Material.GetProportion(krypton), n2 * reconstitution.GetDecimal(64));
                    Atmosphere.Material.AddConstituent(krypton, kr);
                    n2 -= kr;

                    // An even smaller fraction may be Xe or Ne.
                    var xenon = Substances.All.Xenon.GetHomogeneousReference();
                    var xe = Math.Max(Atmosphere.Material.GetProportion(xenon), n2 * reconstitution.GetDecimal(65));
                    Atmosphere.Material.AddConstituent(xenon, xe);
                    n2 -= xe;

                    var neon = Substances.All.Neon.GetHomogeneousReference();
                    var ne = Math.Max(Atmosphere.Material.GetProportion(neon), n2 * reconstitution.GetDecimal(66));
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
                    Atmosphere.ResetWater(this);
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
                        Atmosphere.ResetWater(this);

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
            OrbitalParameters? orbit)
        {
            _seed = Randomizer.Instance.NextUIntInclusive();

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
                eccentricity = Randomizer.Instance.NextDouble();
            }
            else if (IsAsteroid)
            {
                eccentricity = Randomizer.Instance.NextDouble(0.4);
            }
            else
            {
                eccentricity = Randomizer.Instance.PositiveNormalDistributionSample(0, 0.05);
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

            var reconstitution = ReconstituteMaterial(
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
                    var years = Randomizer.Instance.LogisticDistributionSample(0, 1) * new Number(4.6, 9);

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
                    if (Randomizer.Instance.NextDouble() <= 0.05) // low chance of an extreme period
                    {
                        RotationalPeriod = Randomizer.Instance.NextNumber(
                            rotationalPeriodLimit,
                            IsTerrestrial ? new Number(22000000) : new Number(1100000));
                    }
                    else
                    {
                        RotationalPeriod = Randomizer.Instance.NextNumber(
                            IsTerrestrial ? new Number(40000) : new Number(8000),
                            rotationalPeriodLimit);
                    }
                }
            }

            GenerateOrbit(
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
            else if (Randomizer.Instance.NextDouble() <= 0.2) // low chance of an extreme tilt
            {
                AngleOfRotation = Randomizer.Instance.NextDouble(MathAndScience.Constants.Doubles.MathConstants.QuarterPI, Math.PI);
            }
            else
            {
                AngleOfRotation = Randomizer.Instance.NextDouble(MathAndScience.Constants.Doubles.MathConstants.QuarterPI);
            }
            SetAxis();

            SetTemperatures(stars);

            var surfaceTemp = ReconstituteHydrosphere(reconstitution);

            if (star is not null
                && (_planetParams?.SurfaceTemperature.HasValue == true
                || _habitabilityRequirements?.MinimumTemperature.HasValue == true
                || _habitabilityRequirements?.MaximumTemperature.HasValue == true))
            {
                CorrectSurfaceTemperature(reconstitution, stars, star, surfaceTemp);
            }
            else
            {
                GenerateAtmosphere(reconstitution);
            }

            GenerateResources(reconstitution);

            var satellites = satellite
                ? new List<Planetoid>()
                : GenerateSatellites(parent, stars);

            SetRings();

            return satellites;
        }

        private void CorrectSurfaceTemperature(
            Reconstitution reconstitution,
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
                    GenerateOrbit(star, Orbit.Value.Eccentricity, semiMajorAxis, Orbit.Value.TrueAnomaly);
                }
                ResetAllCachedTemperatures(stars);

                // Reset hydrosphere to negate effects of runaway evaporation or freezing.
                Hydrosphere = originalHydrosphere;

                if (newAtmosphere)
                {
                    GenerateAtmosphere(reconstitution);
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
            Reconstitution reconstitution,
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
                Atmosphere.ResetWater(this);
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
                var waterVapor = Math.Min(gasProportion, reconstitution.GetDecimal(67));
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

        private void GenerateAtmosphere_Dwarf(Reconstitution reconstitution)
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
            Atmosphere = new Atmosphere(this, reconstitution.GetDouble(12), components.ToArray());

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

        private void GenerateAtmosphere_Giant(Reconstitution reconstitution)
        {
            var trace = reconstitution.GetDecimal(29);

            var h = reconstitution.GetDecimal(30);
            var he = 1 - h - trace;

            var ch4 = reconstitution.GetDecimal(31) * trace;
            trace -= ch4;

            // 50% chance not to have each of these components
            var c2h6 = Math.Max(0, reconstitution.GetDecimal(32));
            var traceTotal = c2h6;
            var nh3 = Math.Max(0, reconstitution.GetDecimal(33));
            traceTotal += nh3;
            var waterVapor = Math.Max(0, reconstitution.GetDecimal(34));
            traceTotal += waterVapor;

            var nh4sh = reconstitution.GetDecimal(35);
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

        private void GenerateAtmosphere_SmallBody(Reconstitution reconstitution)
        {
            var dust = 1.0m;

            var water = reconstitution.GetDecimal(29);
            dust -= water;

            var co = reconstitution.GetDecimal(30);
            dust -= co;

            if (dust < 0)
            {
                water -= 0.1m;
                dust += 0.1m;
            }

            var co2 = reconstitution.GetDecimal(31);
            dust -= co2;

            var nh3 = reconstitution.GetDecimal(32);
            dust -= nh3;

            var ch4 = reconstitution.GetDecimal(33);
            dust -= ch4;

            var h2s = reconstitution.GetDecimal(34);
            dust -= h2s;

            var so2 = reconstitution.GetDecimal(35);
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

        private void GenerateAtmosphere(Reconstitution reconstitution)
        {
            if (PlanetType == PlanetType.Comet
                || IsAsteroid)
            {
                GenerateAtmosphere_SmallBody(reconstitution);
                return;
            }

            if (IsGiant)
            {
                GenerateAtmosphere_Giant(reconstitution);
                return;
            }

            if (!IsTerrestrial)
            {
                GenerateAtmosphere_Dwarf(reconstitution);
                return;
            }

            if (AverageBlackbodyTemperature >= GetTempForThinAtmosphere())
            {
                GenerateAtmosphereTrace(reconstitution);
            }
            else
            {
                GenerateAtmosphereThick(reconstitution);
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
                    reconstitution,
                    water,
                    surfaceTemp,
                    adjustedAtmosphericPressure);

                // Recalculate temperatures based on the new atmosphere.
                ResetCachedTemperatures();

                FractionHydrophere(GetAverageSurfaceTemperature());

                // Recalculate the phases of water based on the new temperature.
                adjustedAtmosphericPressure = CalculateGasPhaseMix(
                    reconstitution,
                    water,
                    GetAverageSurfaceTemperature(),
                    adjustedAtmosphericPressure);

                // If life alters the greenhouse potential, temperature and water phase must be
                // recalculated once again.
                if (GenerateLife(reconstitution))
                {
                    adjustedAtmosphericPressure = CalculateGasPhaseMix(
                        reconstitution,
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
                        Atmosphere.ResetWater(this);
                    }
                    modified = true;
                }
            }
            if (modified)
            {
                Atmosphere.ResetGreenhouseFactor();
                ResetCachedTemperatures();
            }

            adjustedAtmosphericPressure = CalculatePhases(reconstitution, 0, adjustedAtmosphericPressure);
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

        private void GenerateAtmosphereThick(Reconstitution reconstitution)
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
                pressure = reconstitution.GetDouble(13);
            }
            else
            {
                Number mass;
                // Low-gravity planets without magnetospheres are less likely to hold onto the bulk
                // of their atmospheres over long periods.
                if (Mass >= 1.5e24 || HasMagnetosphere)
                {
                    mass = Mass / reconstitution.GetDouble(14);
                }
                else
                {
                    mass = Mass / reconstitution.GetDouble(15);
                }

                pressure = (double)(mass * SurfaceGravity / (1000 * MathConstants.FourPI * RadiusSquared));
            }

            // For terrestrial (non-giant) planets, these gases remain at low concentrations due to
            // atmospheric escape.
            var h = _planetParams?.EarthlikeAtmosphere == true ? 3.8e-8m : reconstitution.GetDecimal(42);
            var he = _planetParams?.EarthlikeAtmosphere == true ? 7.24e-6m : reconstitution.GetDecimal(43);

            // 50% chance not to have these components at all.
            var ch4 = _planetParams?.EarthlikeAtmosphere == true ? 2.9e-6m : Math.Max(0, reconstitution.GetDecimal(44));
            var traceTotal = ch4;

            var co = _planetParams?.EarthlikeAtmosphere == true ? 2.5e-7m : Math.Max(0, reconstitution.GetDecimal(45));
            traceTotal += co;

            var so2 = _planetParams?.EarthlikeAtmosphere == true ? 1e-7m : Math.Max(0, reconstitution.GetDecimal(46));
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
                trace = reconstitution.GetDecimal(47);
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
            var co2 = _planetParams?.EarthlikeAtmosphere == true ? 5.3e-4m : reconstitution.GetDecimal(48) - trace;

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
                    waterVapor = Math.Max(0, reconstitution.GetDecimal(49));
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
                    o2 = reconstitution.GetDecimal(50);
                }
            }

            var o3 = _planetParams?.EarthlikeAtmosphere == true ? o2 * 4.5e-5m : 0;

            // N2 (largely inert gas) comprises whatever is left after the other components have been
            // determined. This is usually a trace amount, unless CO2 has been reduced to a trace, in
            // which case it will comprise the bulk of the atmosphere.
            var n2 = 1 - (h + he + co2 + waterVapor + o2 + o3 + trace);

            // Some portion of the N2 may be Ar instead.
            var ar = _planetParams?.EarthlikeAtmosphere == true ? 1.288e-3m : Math.Max(0, n2 * reconstitution.GetDecimal(51));
            n2 -= ar;
            // An even smaller fraction may be Kr.
            var kr = _planetParams?.EarthlikeAtmosphere == true ? 3.3e-6m : Math.Max(0, n2 * reconstitution.GetDecimal(52));
            n2 -= kr;
            // An even smaller fraction may be Xe or Ne.
            var xe = _planetParams?.EarthlikeAtmosphere == true ? 8.7e-8m : Math.Max(0, n2 * reconstitution.GetDecimal(53));
            n2 -= xe;
            var ne = _planetParams?.EarthlikeAtmosphere == true ? 1.267e-5m : Math.Max(0, n2 * reconstitution.GetDecimal(54));
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

        private void GenerateAtmosphereTrace(Reconstitution reconstitution)
        {
            // For terrestrial (non-giant) planets, these gases remain at low concentrations due to
            // atmospheric escape.
            var h = reconstitution.GetDecimal(29);
            var he = reconstitution.GetDecimal(30);

            // 50% chance not to have these components at all.
            var ch4 = Math.Max(0, reconstitution.GetDecimal(31));
            var total = ch4;

            var co = Math.Max(0, reconstitution.GetDecimal(32));
            total += co;

            var so2 = Math.Max(0, reconstitution.GetDecimal(33));
            total += so2;

            var n2 = Math.Max(0, reconstitution.GetDecimal(34));
            total += n2;

            // Noble traces: selected as fractions of N2, if present, to avoid over-representation.
            var ar = n2 > 0 ? Math.Max(0, n2 * reconstitution.GetDecimal(35)) : 0;
            n2 -= ar;
            var kr = n2 > 0 ? Math.Max(0, n2 * reconstitution.GetDecimal(36)) : 0;
            n2 -= kr;
            var xe = n2 > 0 ? Math.Max(0, n2 * reconstitution.GetDecimal(37)) : 0;
            n2 -= xe;

            // Carbon monoxide means at least some carbon dioxide, as well.
            var co2 = co > 0
                ? reconstitution.GetDecimal(38)
                : Math.Max(0, reconstitution.GetDecimal(39));
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
                waterVapor = Math.Max(0, reconstitution.GetDecimal(40));
            }
            total += waterVapor;

            var o2 = 0.0m;
            if (PlanetType != PlanetType.Carbon)
            {
                // Always at least some oxygen if there is water, planetary composition allowing
                o2 = waterVapor > 0
                    ? waterVapor * 1e-4m
                    : Math.Max(0, reconstitution.GetDecimal(41));
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
                Atmosphere = new Atmosphere(this, reconstitution.GetDouble(12), components.ToArray());
            }
        }

        private Planetoid? GenerateGiantSatellite(CosmicLocation? parent, List<Star> stars, Number periapsis, double eccentricity, Number maxMass)
        {
            var orbit = new OrbitalParameters(
                Mass,
                Position,
                periapsis,
                eccentricity,
                Randomizer.Instance.NextDouble(0.5),
                Randomizer.Instance.NextDouble(MathAndScience.Constants.Doubles.MathConstants.TwoPI),
                Randomizer.Instance.NextDouble(MathAndScience.Constants.Doubles.MathConstants.TwoPI),
                Randomizer.Instance.NextDouble(MathAndScience.Constants.Doubles.MathConstants.TwoPI));
            double chance;

            // If the mass limit allows, there is an even chance that the satellite is a smaller planet.
            if (maxMass > _TerrestrialMinMassForType && Randomizer.Instance.NextBool())
            {
                // Select from the standard distribution of types.
                chance = Randomizer.Instance.NextDouble();

                // Planets with very low orbits are lava planets due to tidal
                // stress (plus a small percentage of others due to impact trauma).

                // The maximum mass and density are used to calculate an outer
                // Roche limit (may not be the actual Roche limit for the body
                // which gets generated).
                if (periapsis < GetRocheLimit(DefaultTerrestrialMaxDensity) * new Number(105, -2) || chance <= 0.01)
                {
                    return new Planetoid(PlanetType.Lava, parent, null, stars, Vector3.Zero, out _, orbit, new PlanetParams(maxMass: maxMass), satellite: true);
                }
                else if (chance <= 0.65) // Most will be standard terrestrial.
                {
                    return new Planetoid(PlanetType.Terrestrial, parent, null, stars, Vector3.Zero, out _, orbit, new PlanetParams(maxMass: maxMass), satellite: true);
                }
                else if (chance <= 0.75)
                {
                    return new Planetoid(PlanetType.Iron, parent, null, stars, Vector3.Zero, out _, orbit, new PlanetParams(maxMass: maxMass), satellite: true);
                }
                else
                {
                    return new Planetoid(PlanetType.Ocean, parent, null, stars, Vector3.Zero, out _, orbit, new PlanetParams(maxMass: maxMass), satellite: true);
                }
            }

            // Otherwise, if the mass limit allows, there is an even chance that the satellite is a dwarf planet.
            else if (maxMass > _DwarfMinMassForType && Randomizer.Instance.NextBool())
            {
                chance = Randomizer.Instance.NextDouble();
                // Dwarf planets with very low orbits are lava planets due to tidal stress (plus a small percentage of others due to impact trauma).
                if (periapsis < GetRocheLimit(DensityForDwarf) * new Number(105, -2) || chance <= 0.01)
                {
                    return new Planetoid(PlanetType.LavaDwarf, parent, null, stars, Vector3.Zero, out _, orbit, new PlanetParams(maxMass: maxMass), satellite: true);
                }
                else if (chance <= 0.75) // Most will be standard.
                {
                    return new Planetoid(PlanetType.Dwarf, parent, null, stars, Vector3.Zero, out _, orbit, new PlanetParams(maxMass: maxMass), satellite: true);
                }
                else
                {
                    return new Planetoid(PlanetType.RockyDwarf, parent, null, stars, Vector3.Zero, out _, orbit, new PlanetParams(maxMass: maxMass), satellite: true);
                }
            }

            // Otherwise, it is an asteroid, selected from the standard distribution of types.
            else if (maxMass > 0)
            {
                chance = Randomizer.Instance.NextDouble();
                if (chance <= 0.75)
                {
                    return new Planetoid(PlanetType.AsteroidC, parent, null, stars, Vector3.Zero, out _, orbit, new PlanetParams(maxMass: maxMass), satellite: true);
                }
                else if (chance <= 0.9)
                {
                    return new Planetoid(PlanetType.AsteroidS, parent, null, stars, Vector3.Zero, out _, orbit, new PlanetParams(maxMass: maxMass), satellite: true);
                }
                else
                {
                    return new Planetoid(PlanetType.AsteroidM, parent, null, stars, Vector3.Zero, out _, orbit, new PlanetParams(maxMass: maxMass), satellite: true);
                }
            }

            return null;
        }

        private void GenerateHydrocarbons(Reconstitution reconstitution)
        {
            // It is presumed that it is statistically likely that the current eon is not the first
            // with life, and therefore that some fossilized hydrocarbon deposits exist.
            var coal = reconstitution.GetDecimal(56);

            AddResource(Substances.All.Anthracite.GetReference(), coal, false);
            AddResource(Substances.All.BituminousCoal.GetReference(), coal, false);

            var petroleum = reconstitution.GetDecimal(57);
            var petroleumSeed = AddResource(Substances.All.Petroleum.GetReference(), petroleum, false);

            // Natural gas is predominantly, though not exclusively, found with petroleum deposits.
            AddResource(Substances.All.NaturalGas.GetReference(), petroleum, false, true, petroleumSeed);
        }

        private void GenerateHydrosphere(Reconstitution reconstitution, double surfaceTemp)
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
                ratio = (decimal)((reconstitution.GetDouble(16) * MaxElevation / 3) + (MaxElevation * 2));
            }
            else
            {
                ratio = reconstitution.GetDecimal(68);
            }

            var mass = Number.Zero;
            var seawater = Substances.All.Seawater.GetHomogeneousReference();

            if (ratio <= 0)
            {
                SeaLevel = -MaxElevation * 1.1;
            }
            else if (ratio >= 1 && (IsGiant || PlanetType == PlanetType.Ocean || MaxElevation.IsNearlyZero()))
            {
                SeaLevel = MaxElevation * (double)ratio;
                mass = new HollowSphere(Shape.ContainingRadius, Shape.ContainingRadius + SeaLevel).Volume * (seawater.Homogeneous.DensityLiquid ?? 0);
            }
            else
            {
                var grid = new WorldGrid(this, WorldGrid.DefaultGridSize);
                var seaLevel = 0.0;
                if (ratio == 0.5m)
                {
                    SeaLevel = 0;
                }
                else
                {
                    // Midway between the elevations of the first two tiles beyond the amount indicated by
                    // the ratio when ordered by elevation.
                    seaLevel = grid.Tiles
                        .OrderBy(t => t.Elevation)
                        .Skip((int)Math.Floor(grid.Tiles.Length * ratio))
                        .Take(2)
                        .Average(t => t.Elevation);
                    SeaLevel = seaLevel * MaxElevation;
                }
                var fiveSidedArea = WorldGrid.GridAreas[WorldGrid.DefaultGridSize].fiveSided;
                var sixSidedArea = WorldGrid.GridAreas[WorldGrid.DefaultGridSize].sixSided;
                mass = grid.Tiles
                    .Where(t => t.Elevation - seaLevel < 0)
                    .Sum(x => (x.EdgeCount == 5 ? fiveSidedArea : sixSidedArea) * GetNormalizedElevationAt(x.Vector))
                    * -MaxElevation
                    * RadiusSquared
                    * (seawater.Homogeneous.DensityLiquid ?? 0);
            }

            if (!mass.IsPositive)
            {
                Hydrosphere = MathAndScience.Chemistry.Material.Empty;
                return;
            }

            // Surface water is mostly salt water.
            var seawaterProportion = reconstitution.GetDecimal(69);
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
        private bool GenerateLife(Reconstitution reconstitution)
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

            GenerateHydrocarbons(reconstitution);

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
            var o2 = reconstitution.GetDecimal(55);
            var oxygen = Substances.All.Oxygen.GetHomogeneousReference();
            Atmosphere.Material.AddConstituent(oxygen, o2);

            // Calculate ozone based on level of free oxygen.
            var o3 = o2 * 4.5e-5m;
            var ozone = Substances.All.Ozone.GetHomogeneousReference();
            if (!(Atmosphere.Material is Composite lc) || lc.Components.Count < 3)
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
                    Atmosphere.ResetWater(this);
                }

                Atmosphere.Material.AddConstituent(methane, ch4 * 0.001m);

                Atmosphere.ResetGreenhouseFactor();
                ResetCachedTemperatures();
                return true;
            }

            return false;
        }

        private void GenerateOrbit(
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
                    Randomizer.Instance.NextDouble(Math.PI),
                    Randomizer.Instance.NextDouble(MathAndScience.Constants.Doubles.MathConstants.TwoPI),
                    Randomizer.Instance.NextDouble(MathAndScience.Constants.Doubles.MathConstants.TwoPI),
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
                    Randomizer.Instance.NextDouble(0.5),
                    Randomizer.Instance.NextDouble(MathAndScience.Constants.Doubles.MathConstants.TwoPI),
                    Randomizer.Instance.NextDouble(MathAndScience.Constants.Doubles.MathConstants.TwoPI),
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
                    Randomizer.Instance.NextDouble(0.9),
                    Randomizer.Instance.NextDouble(MathAndScience.Constants.Doubles.MathConstants.TwoPI),
                    Randomizer.Instance.NextDouble(MathAndScience.Constants.Doubles.MathConstants.TwoPI),
                    Randomizer.Instance.NextDouble(MathAndScience.Constants.Doubles.MathConstants.TwoPI));
                return;
            }

            var ta = Randomizer.Instance.NextDouble(MathAndScience.Constants.Doubles.MathConstants.TwoPI);
            if (_planetParams?.RevolutionPeriod.HasValue == true)
            {
                GenerateOrbit(orbitedObject, eccentricity, semiMajorAxis, ta);
            }
            else
            {
                Space.Orbit.AssignOrbit(
                    this,
                    orbitedObject,
                    GetDistanceTo(orbitedObject),
                    eccentricity,
                    Randomizer.Instance.NextDouble(0.9),
                    Randomizer.Instance.NextDouble(MathAndScience.Constants.Doubles.MathConstants.TwoPI),
                    Randomizer.Instance.NextDouble(MathAndScience.Constants.Doubles.MathConstants.TwoPI),
                    Randomizer.Instance.NextDouble(MathAndScience.Constants.Doubles.MathConstants.TwoPI));
            }
        }

        private void GenerateOrbit(CosmicLocation orbitedObject, double eccentricity, Number semiMajorAxis, double trueAnomaly) => Space.Orbit.AssignOrbit(
            this,
            orbitedObject,
            (1 - eccentricity) * semiMajorAxis,
            eccentricity,
            Randomizer.Instance.NextDouble(0.9),
            Randomizer.Instance.NextDouble(MathAndScience.Constants.Doubles.MathConstants.TwoPI),
            Randomizer.Instance.NextDouble(MathAndScience.Constants.Doubles.MathConstants.TwoPI),
            trueAnomaly);

        private void GenerateResources(Reconstitution reconstitution)
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
                var sulfurProportion = reconstitution.GetDecimal(58);
                if (sulfurProportion > 0)
                {
                    AddResource(Substances.All.Sulfur.GetHomogeneousReference(), sulfurProportion, false);
                }
            }

            if (IsTerrestrial)
            {
                var beryl = reconstitution.GetDecimal(59);
                var emerald = beryl * 1.58e-4m;
                var corundum = reconstitution.GetDecimal(60);
                var ruby = corundum * 1.58e-4m;
                var sapphire = corundum * 5.7e-3m;

                var diamond = PlanetType == PlanetType.Carbon
                    ? 0 // Carbon planets have diamond in the crust, which will have been added earlier.
                    : reconstitution.GetDecimal(61);

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

        private Planetoid? GenerateSatellite(CosmicLocation? parent, List<Star> stars, Number periapsis, double eccentricity, Number maxMass)
        {
            if (PlanetType == PlanetType.GasGiant
                || PlanetType == PlanetType.IceGiant)
            {
                return GenerateGiantSatellite(parent, stars, periapsis, eccentricity, maxMass);
            }
            if (PlanetType == PlanetType.AsteroidC)
            {
                return new Planetoid(PlanetType.AsteroidC, parent, null, stars, Vector3.Zero, out _, GetAsteroidSatelliteOrbit(periapsis, eccentricity), new PlanetParams(maxMass: maxMass), satellite: true);
            }
            if (PlanetType == PlanetType.AsteroidM)
            {
                return new Planetoid(PlanetType.AsteroidM, parent, null, stars, Vector3.Zero, out _, GetAsteroidSatelliteOrbit(periapsis, eccentricity), new PlanetParams(maxMass: maxMass), satellite: true);
            }
            if (PlanetType == PlanetType.AsteroidS)
            {
                return new Planetoid(PlanetType.AsteroidS, parent, null, stars, Vector3.Zero, out _, GetAsteroidSatelliteOrbit(periapsis, eccentricity), new PlanetParams(maxMass: maxMass), satellite: true);
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
                Randomizer.Instance.NextDouble(0.5),
                Randomizer.Instance.NextDouble(MathAndScience.Constants.Doubles.MathConstants.TwoPI),
                Randomizer.Instance.NextDouble(MathAndScience.Constants.Doubles.MathConstants.TwoPI),
                Randomizer.Instance.NextDouble(MathAndScience.Constants.Doubles.MathConstants.TwoPI));
            double chance;

            // If the mass limit allows, there is an even chance that the satellite is a smaller planet.
            if (maxMass > _TerrestrialMinMassForType && Randomizer.Instance.NextBool())
            {
                // Select from the standard distribution of types.
                chance = Randomizer.Instance.NextDouble();

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
                    return new Planetoid(PlanetType.Lava, parent, null, stars, Vector3.Zero, out _, orbit, new PlanetParams(maxMass: maxMass), satellite: true);
                }
                else if (chance <= terrestrialChance)
                {
                    return new Planetoid(PlanetType.Terrestrial, parent, null, stars, Vector3.Zero, out _, orbit, new PlanetParams(maxMass: maxMass), satellite: true);
                }
                else if (PlanetType == PlanetType.Carbon && chance <= 0.77) // Carbon planets alone have a chance for carbon satellites.
                {
                    return new Planetoid(PlanetType.Carbon, parent, null, stars, Vector3.Zero, out _, orbit, new PlanetParams(maxMass: maxMass), satellite: true);
                }
                else if (IsGiant && chance <= 0.75)
                {
                    return new Planetoid(PlanetType.Iron, parent, null, stars, Vector3.Zero, out _, orbit, new PlanetParams(maxMass: maxMass), satellite: true);
                }
                else
                {
                    return new Planetoid(PlanetType.Ocean, parent, null, stars, Vector3.Zero, out _, orbit, new PlanetParams(maxMass: maxMass), satellite: true);
                }
            }

            // Otherwise, if the mass limit allows, there is an even chance that the satellite is a dwarf planet.
            else if (maxMass > _DwarfMinMassForType && Randomizer.Instance.NextBool())
            {
                chance = Randomizer.Instance.NextDouble();
                // Dwarf planets with very low orbits are lava planets due to tidal stress (plus a small percentage of others due to impact trauma).
                if (periapsis < GetRocheLimit(DensityForDwarf) * new Number(105, -2) || chance <= 0.01)
                {
                    return new Planetoid(PlanetType.LavaDwarf, parent, null, stars, Vector3.Zero, out _, orbit, new PlanetParams(maxMass: maxMass), satellite: true);
                }
                else if (chance <= 0.75) // Most will be standard.
                {
                    return new Planetoid(PlanetType.Dwarf, parent, null, stars, Vector3.Zero, out _, orbit, new PlanetParams(maxMass: maxMass), satellite: true);
                }
                else
                {
                    return new Planetoid(PlanetType.RockyDwarf, parent, null, stars, Vector3.Zero, out _, orbit, new PlanetParams(maxMass: maxMass), satellite: true);
                }
            }

            // Otherwise, it is an asteroid, selected from the standard distribution of types.
            else if (maxMass > 0)
            {
                chance = Randomizer.Instance.NextDouble();
                if (chance <= 0.75)
                {
                    return new Planetoid(PlanetType.AsteroidC, parent, null, stars, Vector3.Zero, out _, orbit, new PlanetParams(maxMass: maxMass), satellite: true);
                }
                else if (chance <= 0.9)
                {
                    return new Planetoid(PlanetType.AsteroidS, parent, null, stars, Vector3.Zero, out _, orbit, new PlanetParams(maxMass: maxMass), satellite: true);
                }
                else
                {
                    return new Planetoid(PlanetType.AsteroidM, parent, null, stars, Vector3.Zero, out _, orbit, new PlanetParams(maxMass: maxMass), satellite: true);
                }
            }

            return null;
        }

        private List<Planetoid> GenerateSatellites(CosmicLocation? parent, List<Star> stars)
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
                var periapsis = Randomizer.Instance.NextNumber(minPeriapsis, maxApoapsis);

                var maxEccentricity = (double)((maxApoapsis - periapsis) / (maxApoapsis + periapsis));
                var eccentricity = maxEccentricity < 0.01
                    ? Randomizer.Instance.NextDouble(0, maxEccentricity)
                    : Randomizer.Instance.PositiveNormalDistributionSample(0, 0.05, maximum: maxEccentricity);

                var semiLatusRectum = periapsis * (1 + eccentricity);
                var semiMajorAxis = semiLatusRectum / (1 - (eccentricity * eccentricity));

                // Keep mass under the limit where the orbital barycenter would be pulled outside the boundaries of this body.
                var maxMass = Number.Max(0, Mass / ((semiMajorAxis / Shape.ContainingRadius) - 1));

                var satellite = GenerateSatellite(parent, stars, periapsis, eccentricity, maxMass);
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

        private List<(ISubstanceReference, decimal)> GetAsteroidComposition(Reconstitution reconstitution)
        {
            var substances = new List<(ISubstanceReference, decimal)>();

            if (PlanetType == PlanetType.AsteroidM)
            {
                var ironNickel = 0.95m;

                var rock = reconstitution.GetDecimal(0);
                ironNickel -= rock;

                var gold = reconstitution.GetDecimal(1);

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
                var gold = reconstitution.GetDecimal(2);

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

                var clay = reconstitution.GetDecimal(3);
                rock -= clay;

                var ice = reconstitution.GetDecimal(4);
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

        private OrbitalParameters GetAsteroidSatelliteOrbit(Number periapsis, double eccentricity)
            => new OrbitalParameters(
                Mass,
                Position,
                periapsis,
                eccentricity,
                Randomizer.Instance.NextDouble(0.5),
                Randomizer.Instance.NextDouble(MathAndScience.Constants.Doubles.MathConstants.TwoPI),
                Randomizer.Instance.NextDouble(MathAndScience.Constants.Doubles.MathConstants.TwoPI),
                Randomizer.Instance.NextDouble(MathAndScience.Constants.Doubles.MathConstants.TwoPI));

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

        private double GetAverageTemperature(bool polar = false)
        {
            var avgBlackbodyTemp = AverageBlackbodyTemperature;
            var greenhouseEffect = GetGreenhouseEffect();
            return (avgBlackbodyTemp * (polar ? InsolationFactor_Polar : InsolationFactor_Equatorial)) + greenhouseEffect;
        }

        private IMaterial GetComposition(Reconstitution reconstitution, double density, Number mass, IShape shape, double? temperature)
        {
            var coreProportion = PlanetType switch
            {
                PlanetType.Dwarf or PlanetType.LavaDwarf or PlanetType.RockyDwarf => reconstitution.GetNumber(6),
                PlanetType.Carbon or PlanetType.Iron => new Number(4, -1),
                _ => new Number(15, -2),
            };

            var crustProportion = IsGiant
                ? Number.Zero
                // Smaller planemos have thicker crusts due to faster proto-planetary cooling.
                : 400000 / Number.Pow(shape.ContainingRadius, new Number(16, -1));

            var coreLayers = IsGiant
                ? GetCore_Giant(reconstitution, shape, coreProportion, mass).ToList()
                : GetCore(reconstitution, shape, coreProportion, crustProportion, mass).ToList();
            var topCoreLayer = coreLayers.Last();
            var coreShape = topCoreLayer.Shape;
            var coreTemp = topCoreLayer.Temperature ?? 0;

            var mantleProportion = 1 - (coreProportion + crustProportion);
            var mantleLayers = GetMantle(reconstitution, shape, mantleProportion, crustProportion, mass, coreShape, coreTemp).ToList();
            if (mantleLayers.Count == 0
                && mantleProportion.IsPositive)
            {
                crustProportion += mantleProportion;
            }

            var crustLayers = GetCrust(reconstitution, shape, crustProportion, mass).ToList();
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
            Reconstitution reconstitution,
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
                var coreSteel = reconstitution.GetDecimal(5);
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

        private IEnumerable<IMaterial> GetCore_Giant(
            Reconstitution reconstitution,
            IShape planetShape,
            Number coreProportion,
            Number planetMass)
        {
            var coreMass = planetMass * coreProportion;

            var coreTemp = (double)(planetShape.ContainingRadius / 3);

            var innerCoreProportion = Number.Min(reconstitution.GetNumber(7), _GiantMinMassForType / coreMass);
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

        private IEnumerable<IMaterial> GetCrust(
            Reconstitution reconstitution,
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
                foreach (var item in GetCrust_RockyDwarf(reconstitution, planetShape, crustProportion, planetMass))
                {
                    yield return item;
                }
                yield break;
            }
            else if (PlanetType == PlanetType.LavaDwarf)
            {
                foreach (var item in GetCrust_LavaDwarf(reconstitution, planetShape, crustProportion, planetMass))
                {
                    yield return item;
                }
                yield break;
            }
            else if (PlanetType == PlanetType.Carbon)
            {
                foreach (var item in GetCrust_Carbon(reconstitution, planetShape, crustProportion, planetMass))
                {
                    yield return item;
                }
                yield break;
            }
            else if (IsTerrestrial)
            {
                foreach (var item in GetCrust_Terrestrial(reconstitution, planetShape, crustProportion, planetMass))
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

            var dust = reconstitution.GetDecimal(4);
            var total = dust;

            // 50% chance of not including the following:
            var waterIce = Math.Max(0, reconstitution.GetDecimal(5));
            total += waterIce;

            var n2 = Math.Max(0, reconstitution.GetDecimal(6));
            total += n2;

            var ch4 = Math.Max(0, reconstitution.GetDecimal(7));
            total += ch4;

            var co = Math.Max(0, reconstitution.GetDecimal(8));
            total += co;

            var co2 = Math.Max(0, reconstitution.GetDecimal(9));
            total += co2;

            var nh3 = Math.Max(0, reconstitution.GetDecimal(10));
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

        private IEnumerable<IMaterial> GetCrust_Carbon(
            Reconstitution reconstitution,
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

            var aluminium = reconstitution.GetDecimal(11);
            var iron = reconstitution.GetDecimal(12);
            var titanium = reconstitution.GetDecimal(13);

            var chalcopyrite = reconstitution.GetDecimal(14); // copper
            graphite -= chalcopyrite;
            var chromite = reconstitution.GetDecimal(15);
            graphite -= chromite;
            var sphalerite = reconstitution.GetDecimal(16); // zinc
            graphite -= sphalerite;
            var galena = reconstitution.GetDecimal(17); // lead
            graphite -= galena;
            var uraninite = reconstitution.GetDecimal(18);
            graphite -= uraninite;
            var cassiterite = reconstitution.GetDecimal(19); // tin
            graphite -= cassiterite;
            var cinnabar = reconstitution.GetDecimal(20); // mercury
            graphite -= cinnabar;
            var acanthite = reconstitution.GetDecimal(21); // silver
            graphite -= acanthite;
            var sperrylite = reconstitution.GetDecimal(22); // platinum
            graphite -= sperrylite;
            var gold = reconstitution.GetDecimal(23);
            graphite -= gold;

            var bauxite = aluminium * 1.57m;
            graphite -= bauxite;

            var hematiteIron = iron * 3 / 4 * reconstitution.GetDecimal(24);
            var hematite = hematiteIron * 2.88m;
            graphite -= hematite;
            var magnetite = (iron - hematiteIron) * 4.14m;
            graphite -= magnetite;

            var ilmenite = titanium * 2.33m;
            graphite -= ilmenite;

            var coal = graphite * reconstitution.GetDecimal(25);
            graphite -= coal * 2;
            var oil = graphite * reconstitution.GetDecimal(26);
            graphite -= oil;
            var gas = graphite * reconstitution.GetDecimal(27);
            graphite -= gas;
            var diamond = graphite * reconstitution.GetDecimal(28);
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

        private IEnumerable<IMaterial> GetCrust_LavaDwarf(
            Reconstitution reconstitution,
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
            var dust = Math.Max(0, reconstitution.GetDecimal(5));
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

        private IEnumerable<IMaterial> GetCrust_RockyDwarf(
            Reconstitution reconstitution,
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
            var dust = Math.Max(0, reconstitution.GetDecimal(5));
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

        private IEnumerable<IMaterial> GetCrust_Terrestrial(
            Reconstitution reconstitution,
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

            var aluminium = reconstitution.GetDecimal(11);
            var iron = reconstitution.GetDecimal(12);
            var titanium = reconstitution.GetDecimal(13);

            var chalcopyrite = reconstitution.GetDecimal(14); // copper
            rock -= chalcopyrite;
            var chromite = reconstitution.GetDecimal(15);
            rock -= chromite;
            var sphalerite = reconstitution.GetDecimal(16); // zinc
            rock -= sphalerite;
            var galena = reconstitution.GetDecimal(17); // lead
            rock -= galena;
            var uraninite = reconstitution.GetDecimal(18);
            rock -= uraninite;
            var cassiterite = reconstitution.GetDecimal(19); // tin
            rock -= cassiterite;
            var cinnabar = reconstitution.GetDecimal(20); // mercury
            rock -= cinnabar;
            var acanthite = reconstitution.GetDecimal(21); // silver
            rock -= acanthite;
            var sperrylite = reconstitution.GetDecimal(22); // platinum
            rock -= sperrylite;
            var gold = reconstitution.GetDecimal(23);
            rock -= gold;

            var bauxite = aluminium * 1.57m;
            rock -= bauxite;

            var hematiteIron = iron * 3 / 4 * reconstitution.GetDecimal(24);
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
            Reconstitution reconstitution,
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
                foreach (var item in GetMantle_Giant(reconstitution, planetShape, mantleProportion, crustProportion, planetMass, coreShape, coreTemp))
                {
                    yield return item;
                }
                yield break;
            }
            else if (PlanetType == PlanetType.IceGiant)
            {
                foreach (var item in GetMantle_IceGiant(reconstitution, planetShape, mantleProportion, crustProportion, planetMass, coreShape, coreTemp))
                {
                    yield return item;
                }
                yield break;
            }
            else if (PlanetType == PlanetType.Carbon)
            {
                foreach (var item in GetMantle_Carbon(reconstitution, planetShape, mantleProportion, crustProportion, planetMass, coreShape, coreTemp))
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

        private IEnumerable<IMaterial> GetMantle_Carbon(
            Reconstitution reconstitution,
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
            var lowerLayer = Number.Max(0, reconstitution.GetNumber(8)) / mantleProportion;
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

        private IEnumerable<IMaterial> GetMantle_Giant(
            Reconstitution reconstitution,
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
            var metalH = Number.Max(Number.Zero, reconstitution.GetNumber(8)) / mantleProportion;
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

            var uLP = (decimal)upperLayerProportion;
            var water = uLP;
            var fluidH = water * 0.71m;
            water -= fluidH;
            var fluidHe = water * 0.24m;
            water -= fluidHe;
            var ne = reconstitution.GetDecimal(6) * water;
            water -= ne;
            var ch4 = reconstitution.GetDecimal(7) * water;
            water = Math.Max(0, water - ch4);
            var nh4 = reconstitution.GetDecimal(8) * water;
            water = Math.Max(0, water - nh4);
            var c2h6 = reconstitution.GetDecimal(9) * water;
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

        private IEnumerable<IMaterial> GetMantle_IceGiant(
            Reconstitution reconstitution,
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
            var water = Math.Max(0, reconstitution.GetDecimal(6) * diamond);
            diamond -= water;
            var nh4 = Math.Max(0, reconstitution.GetDecimal(7) * diamond);
            diamond -= nh4;
            var ch4 = Math.Max(0, reconstitution.GetDecimal(8) * diamond);
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

        private Number GetMass(PlanetType planetType, Number semiMajorAxis, Number? maxMass, double gravity, IShape? shape)
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

        private void GenerateMaterial(
            Reconstitution reconstitution,
            double? temperature,
            Vector3 position,
            Number semiMajorAxis)
        {
            if (PlanetType == PlanetType.Comet)
            {
                Material = new Material(
                    CelestialSubstances.CometNucleus,
                    reconstitution.GetDouble(1),
                    // Gaussian distribution with most values between 1km and 19km.
                    new Ellipsoid(
                        reconstitution.GetDouble(2),
                        reconstitution.GetNumber(0),
                        position),
                    temperature);
                return;
            }

            if (IsAsteroid)
            {
                var mass = reconstitution.GetDouble(3);

                var asteroidDensity = PlanetType switch
                {
                    PlanetType.AsteroidC => 1380,
                    PlanetType.AsteroidM => 5320,
                    PlanetType.AsteroidS => 2710,
                    _ => 2000,
                };

                var axis = (mass * new Number(75, -2) / (asteroidDensity * MathConstants.PI)).CubeRoot();
                var irregularity = reconstitution.GetNumber(1);
                var shape = new Ellipsoid(axis, axis * irregularity, axis / irregularity, position);

                var substances = GetAsteroidComposition(reconstitution);
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
                temperature = reconstitution.GetDouble(4);
            }

            var density = GetDensity(reconstitution, PlanetType);

            double? gravity = null;
            if (_planetParams?.SurfaceGravity.HasValue == true)
            {
                gravity = _planetParams!.Value.SurfaceGravity!.Value;
            }
            else if (_habitabilityRequirements?.MinimumGravity.HasValue == true
                || _habitabilityRequirements?.MaximumGravity.HasValue == true)
            {
                gravity = reconstitution.GetDouble(10);
            }

            if (_planetParams?.Radius.HasValue == true)
            {
                var radius = Number.Max(MinimumRadius, _planetParams!.Value.Radius!.Value);
                var flattening = reconstitution.GetNumber(2);
                var shape = new Ellipsoid(radius, radius * (1 - flattening), position);

                var mass = gravity.HasValue
                    ? GetMass(PlanetType, semiMajorAxis, _planetParams?.MaxMass, gravity.Value, shape)
                    : reconstitution.GetNumber(5);

                Material = GetComposition(reconstitution, density, mass, shape, temperature);
            }
            else if (gravity.HasValue)
            {
                var radius = Number.Max(MinimumRadius, Number.Min(GetRadiusForSurfaceGravity(gravity.Value), GetRadiusForMass(density, GetMaxMassForType(PlanetType))));
                var flattening = reconstitution.GetNumber(2);
                var shape = new Ellipsoid(radius, radius * (1 - flattening), position);

                var mass = GetMass(PlanetType, semiMajorAxis, _planetParams?.MaxMass, gravity.Value, shape);

                Material = GetComposition(reconstitution, density, mass, shape, temperature);
            }
            else
            {
                Number mass;
                if (IsGiant)
                {
                    mass = reconstitution.GetNumber(3);
                }
                else if (IsDwarf)
                {
                    mass = reconstitution.GetNumber(4);
                }
                else
                {
                    mass = reconstitution.GetNumber(5);
                }

                // An approximate radius as if the shape was a sphere is determined, which is no less
                // than the minimum required for hydrostatic equilibrium.
                var radius = Number.Max(MinimumRadius, GetRadiusForMass(density, mass));
                var flattening = reconstitution.GetNumber(2);
                var shape = new Ellipsoid(radius, radius * (1 - flattening), position);

                Material = GetComposition(reconstitution, density, mass, shape, temperature);
            }
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

        private double GetNormalizedElevationAt(double x, double y, double z)
        {
            if (MaxElevation.IsNearlyZero())
            {
                return 0;
            }

            var e = GetElevationNoiseAt(x, y, z);

            // Get the value offset from sea level.
            var n = e - _normalizedSeaLevel;

            // Skew land lower, to avoid extended mountainous regions; and ocean locations deeper,
            // to avoid extended shallow shores. The scaling is initially sharp, and reduces towards
            // nothing. Landmasses are typically dominated by plains, and ascend rapidly towards
            // mountain ranges. Oceans are typically shallow near the shore, then become deep
            // rapidly, and remain at about the same depth throughout, with occasional trenches.
            n *= 0.5 * ((n * n) + 1);

            return n;
        }

        private double GetPolarAirMass(double atmosphericScaleHeight)
        {
            var r = (double)Shape.ContainingRadius / atmosphericScaleHeight;
            var rCosLat = r * CosPolarLatitude;
            return Math.Sqrt((rCosLat * rCosLat) + (2 * r) + 1) - rCosLat;
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

        private double GetSeasonalLatitude(double latitude, double trueAnomaly)
            => GetSeasonalLatitudeFromDeclination(latitude, GetSolarDeclination(trueAnomaly));

        private double GetSlope(MathAndScience.Numerics.Doubles.Vector3 position, double latitude, double longitude, double elevation)
        {
            // north
            var otherCoords = (lat: latitude + Second, lon: longitude);
            if (otherCoords.lat > Math.PI)
            {
                otherCoords = (MathAndScience.Constants.Doubles.MathConstants.TwoPI - otherCoords.lat, (otherCoords.lon + Math.PI) % MathAndScience.Constants.Doubles.MathConstants.TwoPI);
            }
            var otherPos = LatitudeAndLongitudeToDoubleVector(otherCoords.lat, otherCoords.lon);
            var otherElevation = GetNormalizedElevationAt(otherCoords.lat, otherCoords.lon);
            var slope = Math.Abs(elevation - otherElevation) * MaxElevation / GetDistance(position, otherPos);

            // east
            otherCoords = (lat: latitude, lon: (longitude + Second) % MathAndScience.Constants.Doubles.MathConstants.TwoPI);
            otherPos = LatitudeAndLongitudeToDoubleVector(otherCoords.lat, otherCoords.lon);
            otherElevation = GetNormalizedElevationAt(otherPos.X, otherPos.Y, otherPos.Z);
            slope = Math.Max(slope, Math.Abs(elevation - otherElevation) * MaxElevation / GetDistance(position, otherPos));

            // south
            otherCoords = (lat: latitude - Second, lon: longitude);
            if (otherCoords.lat < -Math.PI)
            {
                otherCoords = (-MathAndScience.Constants.Doubles.MathConstants.TwoPI - otherCoords.lat, (otherCoords.lon + Math.PI) % MathAndScience.Constants.Doubles.MathConstants.TwoPI);
            }
            otherPos = LatitudeAndLongitudeToDoubleVector(otherCoords.lat, otherCoords.lon);
            otherElevation = GetNormalizedElevationAt(otherPos.X, otherPos.Y, otherPos.Z);
            slope = Math.Max(slope, Math.Abs(elevation - otherElevation) * MaxElevation / GetDistance(position, otherPos));

            // west
            otherCoords = (lat: latitude, lon: (longitude - Second) % MathAndScience.Constants.Doubles.MathConstants.TwoPI);
            otherPos = LatitudeAndLongitudeToDoubleVector(otherCoords.lat, otherCoords.lon);
            otherElevation = GetNormalizedElevationAt(otherPos.X, otherPos.Y, otherPos.Z);
            return Math.Max(slope, Math.Abs(elevation - otherElevation) * MaxElevation / GetDistance(position, otherPos));
        }

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

        private double GetSurfaceTemperature(double blackbodyTemperature, double latitude, double trueAnomaly)
            => GetSeasonalSurfaceTemperature(blackbodyTemperature, GetSeasonalLatitude(latitude, trueAnomaly), trueAnomaly);

        private double GetSeasonalSurfaceTemperature(double blackbodyTemperature, double seasonalLatitude, double trueAnomaly)
        {
            var greenhouseEffect = GetGreenhouseEffect();
            var temp = (blackbodyTemperature * GetInsolationFactor(seasonalLatitude)) + greenhouseEffect;
            if (Atmosphere.Material.IsEmpty)
            {
                return temp;
            }
            // Represent the effect of atmospheric convection by resturning the average of the raw
            // result and the equatorial result, weighted by the distance to the equator.
            var seasonalEquatorialLatitide = GetSeasonalLatitude(0, trueAnomaly);
            var equatorialTemp = (blackbodyTemperature * InsolationFactor_Equatorial) + greenhouseEffect;
            var latitudeFactor = Math.Abs(seasonalLatitude - seasonalEquatorialLatitide) / (MathAndScience.Constants.Doubles.MathConstants.HalfPI - AxialTilt) * Math.PI;
            var weight = Math.Sin(latitudeFactor) / 3;
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
            => _surfaceTemperatureAtPeriapsis.Lerp(_surfaceTemperatureAtApoapsis, trueAnomaly <= Math.PI ? trueAnomaly / Math.PI : 2 - (trueAnomaly / Math.PI));

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

        private double ReconstituteHydrosphere(Reconstitution reconstitution)
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

            GenerateHydrosphere(reconstitution, surfaceTemp);

            HasMagnetosphere = _planetParams?.HasMagnetosphere.HasValue == true
                ? _planetParams!.Value.HasMagnetosphere!.Value
                : reconstitution.GetNumber(9) <= Mass * new Number(2.88, -19) / RotationalPeriod * (PlanetType switch
                {
                    PlanetType.Iron => new Number(5),
                    PlanetType.Ocean => Number.Half,
                    _ => Number.One,
                });

            return surfaceTemp;
        }

        private Reconstitution ReconstituteMaterial(
            Vector3 position,
            double? temperature,
            Number semiMajorAxis)
        {
            var reconstitution = new Reconstitution(
                _seed,
                ParentId,
                PlanetType,
                semiMajorAxis,
                _planetParams,
                _habitabilityRequirements);

            _seed1 = reconstitution.GetInt(0);
            _seed2 = reconstitution.GetInt(1);
            _seed3 = reconstitution.GetInt(2);
            _seed4 = reconstitution.GetInt(3);
            _seed5 = reconstitution.GetInt(4);
            _seed6 = reconstitution.GetInt(5);

            AxialPrecession = reconstitution.GetDouble(0);

            GenerateMaterial(
                reconstitution,
                temperature,
                position,
                semiMajorAxis);

            if (_planetParams.HasValue && _planetParams.Value.Albedo.HasValue)
            {
                _surfaceAlbedo = _planetParams.Value.Albedo.Value;
            }
            else
            {
                _surfaceAlbedo = reconstitution.GetDouble(11);
            }
            Albedo = _surfaceAlbedo;

            return reconstitution;
        }

        private void ResetAllCachedTemperatures(List<Star> stars)
        {
            SetTemperatures(stars);

            ResetCachedTemperatures();
        }

        private void ResetCachedTemperatures()
        {
            _averagePolarSurfaceTemperature = null;
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
            _averagePolarSurfaceTemperature = null;
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

        private void SetRings()
        {
            if (PlanetType == PlanetType.Comet
                || IsAsteroid
                || IsDwarf)
            {
                return;
            }

            var ringChance = IsGiant ? 0.9 : 0.1;
            if (Randomizer.Instance.NextDouble() > ringChance)
            {
                return;
            }

            var innerLimit = (Number)Atmosphere.AtmosphericHeight;

            var outerLimit_Icy = GetRingDistance(_IcyRingDensity);
            if (Orbit != null)
            {
                outerLimit_Icy = Number.Min(outerLimit_Icy, GetHillSphereRadius() / 3);
            }
            if (innerLimit >= outerLimit_Icy)
            {
                return;
            }

            var outerLimit_Rocky = GetRingDistance(_RockyRingDensity);
            if (Orbit != null)
            {
                outerLimit_Rocky = Number.Min(outerLimit_Rocky, GetHillSphereRadius() / 3);
            }

            var numRings = IsGiant
                ? (int)Math.Round(Randomizer.Instance.PositiveNormalDistributionSample(1, 1), MidpointRounding.AwayFromZero)
                : (int)Math.Round(Randomizer.Instance.PositiveNormalDistributionSample(1, 0.1667), MidpointRounding.AwayFromZero);
            for (var i = 0; i < numRings && innerLimit <= outerLimit_Icy; i++)
            {
                if (innerLimit < outerLimit_Rocky && Randomizer.Instance.NextBool())
                {
                    var innerRadius = Randomizer.Instance.NextNumber(innerLimit, outerLimit_Rocky);

                    (_rings ??= new List<PlanetaryRing>()).Add(new PlanetaryRing(false, innerRadius, outerLimit_Rocky));

                    outerLimit_Rocky = innerRadius;
                    if (outerLimit_Rocky <= outerLimit_Icy)
                    {
                        outerLimit_Icy = innerRadius;
                    }
                }
                else
                {
                    var innerRadius = Randomizer.Instance.NextNumber(innerLimit, outerLimit_Icy);

                    (_rings ??= new List<PlanetaryRing>()).Add(new PlanetaryRing(true, innerRadius, outerLimit_Icy));

                    outerLimit_Icy = innerRadius;
                    if (outerLimit_Icy <= outerLimit_Rocky)
                    {
                        outerLimit_Rocky = innerRadius;
                    }
                }
            }
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

        private class Reconstitution
        {
            private readonly Dictionary<int, decimal> _decimals = new Dictionary<int, decimal>();
            private readonly Dictionary<int, double> _doubles = new Dictionary<int, double>();
            private readonly Dictionary<int, int> _ints = new Dictionary<int, int>();
            private readonly Dictionary<int, Number> _numbers = new Dictionary<int, Number>();
            private readonly string? _parentId;
            private readonly PlanetParams? _planetParams;
            private readonly PlanetType _planetType;
            private readonly HabitabilityRequirements? _habitabilityRequirements;
            private readonly Randomizer _randomizer;
            private readonly Number _semiMajorAxis;

            private int _decimalIndex = -1;
            private int _doubleIndex = -1;
            private int _intIndex = -1;
            private int _numberIndex = -1;

            public Reconstitution(
                uint seed,
                string? parentId,
                PlanetType planetType,
                Number semiMajorAxis,
                PlanetParams? planetParams,
                HabitabilityRequirements? habitabilityRequirements)
            {
                _habitabilityRequirements = habitabilityRequirements;
                _parentId = parentId;
                _planetParams = planetParams;
                _planetType = planetType;
                _randomizer = new Randomizer(seed);
                _semiMajorAxis = semiMajorAxis;
            }

            public decimal GetDecimal(int index)
            {
                if (!_decimals.TryGetValue(index, out var value))
                {
                    while (_decimalIndex < index)
                    {
                        _decimalIndex++;
                        decimal? v = null;
                        switch (_decimalIndex)
                        {
                            case 0: // Asteroid M rock proportion
                                v = _randomizer.NextDecimal(0.2m);
                                break;
                            case 1: // Asteroid M gold proportion
                                v = _randomizer.NextDecimal(0.05m);
                                break;
                            case 2: // Asteroid S gold proportion
                                v = _randomizer.NextDecimal(0.005m);
                                break;
                            case 3: // Asteroid C clay proportion
                                v = _randomizer.NextDecimal(0.1m, 0.2m);
                                break;
                            case 4: // Asteroid C ice proportion; other dwarf water dust proportion
                                v = PlanetType.AnyDwarf.HasFlag(_planetType)
                                    ? _randomizer.NextDecimal()
                                    : _randomizer.NextDecimal(0.22m);
                                break;
                            case 5: // Carbon planet core steel; rocky/lava dwarf dust proportion; other dwarf water ice proportion
                                v = PlanetType.AnyDwarf.HasFlag(_planetType)
                                    ? _randomizer.NextDecimal(-0.5m, 0.5m)
                                    : _randomizer.NextDecimal(0.945m);
                                break;
                            case 6: // Giant mantle Ne proportion; ice giant mantle water proportion; dwarf N2 proportion
                            case 7: // Giant mantle CH4 proportion; ice giant mantle NH4 proportion; dwarf CH4 proportion
                            case 8: // Giant mantle NH4 proportion; ice giant mantle CH4 proportion; dwarf CO proportion
                            case 9: // Giant mantle C2H6 proportion; dwarf CO2 proportion
                                v = PlanetType.AnyDwarf.HasFlag(_planetType)
                                    ? _randomizer.NextDecimal(-0.5m, 0.5m)
                                    : _randomizer.NextDecimal();
                                break;
                            case 10: // dwarf NH3 proportion 
                                v = _randomizer.NextDecimal(-0.5m, 0.5m);
                                break;
                            case 11: // Terrestrial planet aluminium proportion
                                v = (decimal)_randomizer.NormalDistributionSample(0.026, 4e-3, minimum: 0);
                                break;
                            case 12: // Terrestrial planet iron proportion
                                v = (decimal)_randomizer.NormalDistributionSample(1.67e-2, 2.75e-3, minimum: 0);
                                break;
                            case 13: // Terrestrial planet titanium proportion
                                v = (decimal)_randomizer.NormalDistributionSample(5.7e-3, 9e-4, minimum: 0);
                                break;
                            case 14: // Terrestrial planet chalcopyrite (copper) proportion
                                v = (decimal)_randomizer.NormalDistributionSample(1.1e-3, 1.8e-4, minimum: 0);
                                break;
                            case 15: // Terrestrial planet chromite proportion
                                v = (decimal)_randomizer.NormalDistributionSample(5.5e-4, 9e-5, minimum: 0);
                                break;
                            case 16: // Terrestrial planet sphalerite (zinc) proportion
                                v = (decimal)_randomizer.NormalDistributionSample(8.1e-5, 1.3e-5, minimum: 0);
                                break;
                            case 17: // Terrestrial planet galena (lead) proportion
                                v = (decimal)_randomizer.NormalDistributionSample(2e-5, 3.3e-6, minimum: 0);
                                break;
                            case 18: // Terrestrial planet uraninite proportion
                                v = (decimal)_randomizer.NormalDistributionSample(7.15e-6, 1.1e-6, minimum: 0);
                                break;
                            case 19: // Terrestrial planet cassiterite (tin) proportion
                                v = (decimal)_randomizer.NormalDistributionSample(6.7e-6, 1.1e-6, minimum: 0);
                                break;
                            case 20: // Terrestrial planet cinnabar (mercury) proportion
                                v = (decimal)_randomizer.NormalDistributionSample(1.35e-7, 2.3e-8, minimum: 0);
                                break;
                            case 21: // Terrestrial planet acanthite (silver) proportion
                                v = (decimal)_randomizer.NormalDistributionSample(5e-8, 8.3e-9, minimum: 0);
                                break;
                            case 22: // Terrestrial planet sperrylite (platinum) proportion
                                v = (decimal)_randomizer.NormalDistributionSample(1.17e-8, 2e-9, minimum: 0);
                                break;
                            case 23: // Terrestrial planet gold proportion
                                v = (decimal)_randomizer.NormalDistributionSample(2.75e-9, 4.6e-10, minimum: 0);
                                break;
                            case 24: // Terrestrial planet hematite proportion factor
                                v = (decimal)_randomizer.NormalDistributionSample(1, 0.167, minimum: 0);
                                break;
                            case 25: // Carbon planet coal proportion
                            case 26: // Carbon planet oil proportion
                            case 27: // Carbon planet gas proportion
                                v = (decimal)_randomizer.NormalDistributionSample(0.25, 0.042, minimum: 0);
                                break;
                            case 28: // Carbon planet diamond proportion
                                v = (decimal)_randomizer.NormalDistributionSample(0.125, 0.021, minimum: 0);
                                break;
                            case 29:
                                // Giant atmospheric trace proportion
                                if (PlanetType.Giant.HasFlag(_planetType))
                                {
                                    v = _randomizer.NextDecimal(0.025m);
                                }
                                // Small body atmosphere water proportion
                                else if (PlanetType.AnyDwarf.HasFlag(_planetType))
                                {
                                    v = _randomizer.NextDecimal(0.75m, 0.9m);
                                }
                                // Trace atmosphere H proportion
                                else
                                {
                                    v = _randomizer.NextDecimal(5e-8m, 2e-7m);
                                }
                                break;
                            case 30:
                                // Giant atmosphere H proportion
                                if (PlanetType.Giant.HasFlag(_planetType))
                                {
                                    v = _randomizer.NextDecimal(0.75m, 0.97m);
                                }
                                // Small body atmosphere CO proportion
                                else if (PlanetType.AnyDwarf.HasFlag(_planetType))
                                {
                                    v = _randomizer.NextDecimal(0.05m, 0.15m);
                                }
                                // Trace atmosphere He proportion
                                else
                                {
                                    v = _randomizer.NextDecimal(2.6e-7m, 1e-5m);
                                }
                                break;
                            case 31:
                                // Giant atmosphere CH4 proportion
                                if (PlanetType.Giant.HasFlag(_planetType))
                                {
                                    v = _randomizer.NextDecimal();
                                }
                                // Small body atmosphere CO2 proportion
                                else if (PlanetType.AnyDwarf.HasFlag(_planetType))
                                {
                                    v = _randomizer.NextDecimal(0.01m);
                                }
                                // Trace atmosphere CH4 proportion
                                else
                                {
                                    v = _randomizer.NextDecimal(-0.5m, 0.5m);
                                }
                                break;
                            case 32: // Giant atmosphere C2H6 proportion; small body atmosphere NH3 proportion; trace atmosphere CO proportion
                            case 33: // Giant atmosphere NH3 proportion; small body atmosphere CH4 proportion; trace atmosphere SO2 proportion
                            case 34: // Giant atmosphere water proportion; small body atmosphere H2S proportion; trace atmosphere N2 proportion
                                v = PlanetType.AnyDwarf.HasFlag(_planetType)
                                    ? _randomizer.NextDecimal(0.01m)
                                    : _randomizer.NextDecimal(-0.5m, 0.5m);
                                break;
                            case 35:
                                // Giant atmosphere NH4SH proportion
                                if (PlanetType.Giant.HasFlag(_planetType))
                                {
                                    v = _randomizer.NextDecimal();
                                }
                                // Small body atmosphere SO2 proportion
                                else if (PlanetType.AnyDwarf.HasFlag(_planetType))
                                {
                                    v = _randomizer.NextDecimal(0.001m);
                                }
                                // Trace atmosphere Ar proportion
                                else
                                {
                                    v = _randomizer.NextDecimal(-0.02m, 0.04m);
                                }
                                break;
                            case 36: // Trace atmosphere Kr proportion
                            case 37: // Trace atmosphere Xe proportion
                                v = _randomizer.NextDecimal(-0.02m, 0.04m);
                                break;
                            case 38: // Trace atmosphere CO2 with CO proportion
                                v = _randomizer.NextDecimal(0.5m);
                                break;
                            case 39: // Trace atmosphere CO2 without CO proportion
                                v = _randomizer.NextDecimal(-0.5m, 0.5m);
                                break;
                            case 40: // Trace atmosphere water proportion
                                v = _randomizer.NextDecimal(-0.05m, 0.001m);
                                break;
                            case 41: // Trace atmosphere O2 proportion
                                v = _randomizer.NextDecimal(-0.05m, 0.5m);
                                break;
                            case 42: // Thick atmosphere H proportion
                                v = _randomizer.NextDecimal(1e-8m, 2e-7m);
                                break;
                            case 43: // Thick atmosphere He proportion
                                v = _randomizer.NextDecimal(2.6e-7m, 1e-5m);
                                break;
                            case 44: // Thick atmosphere CH4 proportion
                            case 45: // Thick atmosphere CO proportion
                            case 46: // Thick atmosphere SO2 proportion
                                v = _randomizer.NextDecimal(-0.5m, 0.5m);
                                break;
                            case 47: // Thick atmosphere trace proportion
                                v = _randomizer.NextDecimal(1e-6m, 2.5e-4m);
                                break;
                            case 48: // Thick atmosphere CO2 proportion
                                v = _randomizer.NextDecimal(0.97m, 0.99m);
                                break;
                            case 49: // Thick atmosphere water proportion
                                v = _randomizer.NextDecimal(-0.05m, 0.001m);
                                break;
                            case 50: // Thick atmosphere O2 proportion
                                v = _randomizer.NextDecimal(0.002m);
                                break;
                            case 51: // Thick atmosphere Ar proportion
                                v = _randomizer.NextDecimal(-0.02m, 0.04m);
                                break;
                            case 52: // Thick atmosphere Kr proportion
                                v = _randomizer.NextDecimal(-2.5e-4m, 5.0e-4m);
                                break;
                            case 53: // Thick atmosphere Xe proportion
                            case 54: // Thick atmosphere Ne proportion
                                v = _randomizer.NextDecimal(-1.8e-5m, 3.5e-5m);
                                break;
                            case 55: // Biosphere O2 proportion
                                v = _randomizer.NextDecimal(0.2m, 0.25m);
                                break;
                            case 56: // Coal proportion
                                v = (decimal)_randomizer.NormalDistributionSample(1e-13, 1.7e-14);
                                break;
                            case 57: // Petroleum proportion
                                v = (decimal)_randomizer.NormalDistributionSample(1e-8, 1.6e-9);
                                break;
                            case 58: // Sulfur proportion
                                v = (decimal)_randomizer.NormalDistributionSample(3.5e-5, 1.75e-6);
                                break;
                            case 59: // Beryl proportion
                                v = (decimal)_randomizer.NormalDistributionSample(4e-6, 6.7e-7, minimum: 0);
                                break;
                            case 60: // Corundum proportion
                                v = (decimal)_randomizer.NormalDistributionSample(2.6e-4, 4e-5, minimum: 0);
                                break;
                            case 61: // Diamond proportion
                                v = (decimal)_randomizer.NormalDistributionSample(1.5e-7, 2.5e-8, minimum: 0);
                                break;
                            case 62: // CO2 reduction: CO2 proportion
                                v = _randomizer.NextDecimal(15e-6m, 0.001m);
                                break;
                            case 63: // CO2 reduction: Ar proportion
                                v = _randomizer.NextDecimal(-0.02m, 0.04m);
                                break;
                            case 64: // CO2 reduction: Kr proportion
                                v = _randomizer.NextDecimal(-25e-5m, 0.0005m);
                                break;
                            case 65: // CO2 reduction: Xe proportion
                            case 66: // CO2 reduction: Ne proportion
                                v = _randomizer.NextDecimal(-18e-6m, 35e-6m);
                                break;
                            case 67: // Evaporated water vapor
                                v = _randomizer.NextDecimal(0.001m);
                                break;
                            case 68: // Water ratio
                                v = _randomizer.NextDecimal();
                                break;
                            case 69: // Seawater proportion
                                v = (decimal)_randomizer.NormalDistributionSample(0.945, 0.015);
                                break;
                        }
                        if (v.HasValue)
                        {
                            _decimals[_decimalIndex] = v.Value;
                        }
                    }
                    if (_decimals.ContainsKey(index))
                    {
                        return _decimals[index];
                    }
                    throw new IndexOutOfRangeException("Index invalid for this value type");
                }
                return value;
            }

            public double GetDouble(int index)
            {
                if (!_doubles.TryGetValue(index, out var value))
                {
                    while (_doubleIndex < index)
                    {
                        _doubleIndex++;
                        double? v = null;
                        switch (_doubleIndex)
                        {
                            case 0: // AxialPrecession
                                v = _randomizer.NextDouble(MathAndScience.Constants.Doubles.MathConstants.TwoPI);
                                break;
                            case 1: // Comet density
                                v = _randomizer.NextDouble(300, 700);
                                break;
                            case 2: // Comet radius
                                v = _randomizer.NormalDistributionSample(10000, 4500, minimum: 0);
                                break;
                            case 3: // Asteroid mass
                                var doubleMaxMass = _planetParams.HasValue && _planetParams.Value.MaxMass.HasValue
                                    ? (double)_planetParams.Value.MaxMass.Value
                                    : AsteroidMaxMassForType;
                                v = _randomizer.PositiveNormalDistributionSample(AsteroidMinMassForType, (doubleMaxMass - AsteroidMinMassForType) / 3, maximum: doubleMaxMass);
                                break;
                            case 4: // Lava temperature
                                v = _randomizer.NextDouble(974, 1574);
                                break;
                            case 5: // Gas giant density type
                                v = _randomizer.NextDouble();
                                break;
                            case 6: // Gas giant puffy density
                                v = _randomizer.NextDouble(GiantSubMinDensity, GiantMinDensity);
                                break;
                            case 7: // Gas giant density
                                v = _randomizer.NextDouble(GiantMinDensity, GiantMaxDensity);
                                break;
                            case 8: // Iron planet density
                                v = _randomizer.NextDouble(5250, 8000);
                                break;
                            case 9: // Terrestrial planet density
                                v = _randomizer.NextDouble(3750, DefaultTerrestrialMaxDensity);
                                break;
                            case 10: // Specified gravity
                                double maxGravity;
                                if (_habitabilityRequirements?.MaximumGravity.HasValue == true)
                                {
                                    maxGravity = _habitabilityRequirements!.Value.MaximumGravity!.Value;
                                }
                                else // Determine the maximum gravity the planet could have by calculating from its maximum mass.
                                {
                                    var density = GetDensity(this, _planetType);
                                    var max = _planetParams?.MaxMass ?? GetMaxMassForType(_planetType);
                                    var maxVolume = max / density;
                                    var maxRadius = (maxVolume / MathConstants.FourThirdsPI).CubeRoot();
                                    maxGravity = (double)(ScienceConstants.G * max / (maxRadius * maxRadius));
                                }
                                v = _randomizer.NextDouble(_habitabilityRequirements?.MinimumGravity ?? 0, maxGravity);
                                break;
                            case 11: // Surface albedo
                                if (_planetType == PlanetType.Comet)
                                {
                                    v = _randomizer.NextDouble(0.025, 0.055);
                                }
                                else if (_planetType == PlanetType.AsteroidC)
                                {
                                    v = _randomizer.NextDouble(0.03, 0.1);
                                }
                                else if (_planetType == PlanetType.AsteroidM)
                                {
                                    v = _randomizer.NextDouble(0.1, 0.2);
                                }
                                else if (_planetType == PlanetType.AsteroidS)
                                {
                                    v = _randomizer.NextDouble(0.1, 0.22);
                                }
                                else if (PlanetType.Giant.HasFlag(_planetType))
                                {
                                    v = _randomizer.NextDouble(0.275, 0.35);
                                }
                                else
                                {
                                    v = _randomizer.NextDouble(0.1, 0.6);
                                }
                                break;
                            case 12: // Dwarf/trace atmopsheric pressure
                                v = PlanetType.AnyDwarf.HasFlag(_planetType)
                                    ? _randomizer.NextDouble(2.5)
                                    : _randomizer.NextDouble(25);
                                break;
                            case 13: // Thick atmopsheric bounded pressure
                                // If there is a minimum but no maximum, a half-Gaussian distribution with the minimum as both mean and the basis for the sigma is used.
                                if (_habitabilityRequirements.HasValue
                                    && _habitabilityRequirements.Value.MinimumPressure.HasValue)
                                {
                                    if (!_habitabilityRequirements.Value.MaximumPressure.HasValue)
                                    {
                                        v = _habitabilityRequirements.Value.MinimumPressure.Value
                                            + Math.Abs(_randomizer.NormalDistributionSample(0, _habitabilityRequirements.Value.MinimumPressure.Value / 3));
                                    }
                                    else
                                    {
                                        v = _randomizer.NextDouble(_habitabilityRequirements.Value.MinimumPressure ?? 0, _habitabilityRequirements.Value.MaximumPressure.Value);
                                    }
                                }
                                else
                                {
                                    v = 0;
                                }
                                break;
                            case 14: // Thick atmopsheric high mass pressure
                                v = _randomizer.NormalDistributionSample(1158568, 38600, minimum: 579300, maximum: 1737900);
                                break;
                            case 15: // Thick atmopsheric low mass pressure
                                v = _randomizer.NormalDistributionSample(7723785, 258000, minimum: 3862000, maximum: 11586000);
                                break;
                            case 16: // Ocean planet water ratio
                                v = _randomizer.NormalDistributionSample();
                                break;
                        }
                        if (v.HasValue)
                        {
                            _doubles[_doubleIndex] = v.Value;
                        }
                    }
                    if (_doubles.ContainsKey(index))
                    {
                        return _doubles[index];
                    }
                    throw new IndexOutOfRangeException("Index invalid for this value type");
                }
                return value;
            }

            public int GetInt(int index)
            {
                if (!_ints.TryGetValue(index, out var value))
                {
                    while (_intIndex < index)
                    {
                        _intIndex++;
                        int? v = _intIndex switch
                        {
                            // _seed1-6
                            <= 5 => _randomizer.NextInclusive(),
                            _ => null,
                        };
                        if (v.HasValue)
                        {
                            _ints[_intIndex] = v.Value;
                        }
                    }
                    if (_ints.ContainsKey(index))
                    {
                        return _ints[index];
                    }
                    throw new IndexOutOfRangeException("Index invalid for this value type");
                }
                return value;
            }

            public Number GetNumber(int index)
            {
                if (!_numbers.TryGetValue(index, out var value))
                {
                    while (_numberIndex < index)
                    {
                        _numberIndex++;
                        Number? v = null;
                        switch (_numberIndex)
                        {
                            case 0: // Comet axis
                                v = _randomizer.NextNumber(Number.Half, 1);
                                break;
                            case 1: // Asteroid irregularity
                                v = _randomizer.NextNumber(Number.Half, Number.One);
                                break;
                            case 2: // Planet flattening
                                v = _randomizer.NextNumber(Number.Deci);
                                break;
                            case 3: // Giant mass
                                v = _randomizer.NextNumber(_GiantMinMassForType, _planetParams?.MaxMass ?? _GiantMaxMassForType);
                                break;
                            case 4: // Dwarf mass
                                var maxMass = _planetParams?.MaxMass;
                                if (!string.IsNullOrEmpty(_parentId))
                                {
                                    var sternLevisonLambdaMass = (Number.Pow(_semiMajorAxis, new Number(15, -1)) / new Number(2.5, -28)).Sqrt();
                                    maxMass = Number.Min(_planetParams?.MaxMass ?? _DwarfMaxMassForType, sternLevisonLambdaMass / 100);
                                    if (maxMass < _DwarfMinMassForType)
                                    {
                                        maxMass = _DwarfMinMassForType; // sanity check; may result in a "dwarf" planet which *can* clear its neighborhood
                                    }
                                }
                                v = _randomizer.NextNumber(_DwarfMinMassForType, maxMass ?? _DwarfMaxMassForType);
                                break;
                            case 5: // Mass for unspecified gravity
                                var min = Number.Zero;
                                if (!PlanetType.AnyDwarf.HasFlag(_planetType))
                                {
                                    // Stern-Levison parameter for neighborhood-clearing used to determined minimum mass
                                    // at which the planet would be able to do so at this orbital distance. We set the
                                    // minimum at two orders of magnitude more than this (planets in our solar system
                                    // all have masses above 5 orders of magnitude more). Note that since lambda is
                                    // proportional to the square of mass, it is multiplied by 10 to obtain a difference
                                    // of 2 orders of magnitude, rather than by 100.
                                    var sternLevisonLambdaMass = (Number.Pow(_semiMajorAxis, new Number(15, -1)) / new Number(2.5, -28)).Sqrt();
                                    min = Number.Max(min, sternLevisonLambdaMass * 10);

                                    // sanity check; may result in a "planet" which *can't* clear its neighborhood
                                    if (_planetParams.HasValue
                                        && _planetParams.Value.MaxMass.HasValue
                                        && min > _planetParams.Value.MaxMass.Value)
                                    {
                                        min = _planetParams.Value.MaxMass.Value;
                                    }
                                }
                                v = _planetParams.HasValue && _planetParams.Value.MaxMass.HasValue
                                    ? _randomizer.NextNumber(min, _planetParams.Value.MaxMass.Value)
                                    : min;
                                break;
                            case 6: // Dwarf core proportion
                                v = _randomizer.NextNumber(new Number(2, -1), new Number(55, -2));
                                break;
                            case 7: // Giant inner core proportion
                                v = _randomizer.NextNumber(new Number(2, -2), new Number(2, -1));
                                break;
                            case 8: // Giant metallic hydrogen lower mantle; carbon planet Molten silicon carbide lower mantle
                                v = _randomizer.NextNumber(-Number.Deci, new Number(55, -2));
                                break;
                            case 9: // Magnetosphere chance
                                v = _randomizer.NextNumber();
                                break;
                        }
                        if (v.HasValue)
                        {
                            _numbers[_numberIndex] = v.Value;
                        }
                    }
                    if (_numbers.ContainsKey(index))
                    {
                        return _numbers[index];
                    }
                    throw new IndexOutOfRangeException("Index invalid for this value type");
                }
                return value;
            }
        }
    }
}
