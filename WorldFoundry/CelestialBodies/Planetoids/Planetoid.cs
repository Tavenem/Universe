using NeverFoundry;
using NeverFoundry.MathAndScience;
using NeverFoundry.MathAndScience.Chemistry;
using NeverFoundry.MathAndScience.Constants.Numbers;
using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.MathAndScience.Randomization;
using NeverFoundry.MathAndScience.Time;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Threading.Tasks;
using NeverFoundry.WorldFoundry.CelestialBodies.Stars;
using NeverFoundry.WorldFoundry.Climate;
using NeverFoundry.WorldFoundry.Place;
using NeverFoundry.WorldFoundry.Space;
using NeverFoundry.WorldFoundry.SurfaceMapping;

namespace NeverFoundry.WorldFoundry.CelestialBodies.Planetoids
{
    /// <summary>
    /// Any non-stellar celestial body, such as a planet or asteroid.
    /// </summary>
    [Serializable]
    public class Planetoid : CelestialLocation
    {
        // polar latitude = ~1.476
        private const double CosPolarLatitude = 0.095;

        /// <summary>
        /// The minimum radius required to achieve hydrostatic equilibrium, in meters.
        /// </summary>
        private protected const int MinimumRadius = 600000;
        private const double Second = MathAndScience.Constants.Doubles.MathConstants.PIOver180 / 3600;
        private const double ThirtySixthPI = MathAndScience.Constants.Doubles.MathConstants.PI / 36;

        /// <summary>
        /// Hadley values are a pure function of latitude, and do not vary with any property of the
        /// planet, atmosphere, or season. Since the calculation is relatively expensive, retrieved
        /// values can be stored for the lifetime of the program for future retrieval for the same
        /// (or very similar) location.
        /// </summary>
        private static readonly Dictionary<double, double> _HadleyValues = new Dictionary<double, double>();

        private static readonly double _LowTemp = CelestialSubstances.WaterMeltingPoint - 16;

        internal double? _greenhouseEffect;

        private double? _averagePolarSurfaceTemperature;
        private double? _averageSurfaceTemperature;
        private protected byte[]? _depthMap;
        private double? _diurnalTemperatureVariation;
        private protected byte[]? _elevationMap;
        private protected byte[]? _flowMap;
        private double? _maxSurfaceTemperature;
        private double? _minSurfaceTemperature;
        private protected double _normalizedSeaLevel;
        private protected byte[][]? _precipitationMaps;
        private protected List<string>? _satelliteIDs;
        private protected byte[][]? _snowfallMaps;
        private protected int _seed1;
        private protected int _seed2;
        private protected int _seed3;
        private protected int _seed4;
        private protected int _seed5;
        private double? _surfaceTemperature;
        private protected byte[]? _temperatureMapSummer;
        private protected byte[]? _temperatureMapWinter;

        private protected double? _angleOfRotation;
        /// <summary>
        /// The angle between the Y-axis and the axis of rotation of this <see cref="Planetoid"/>.
        /// Values greater than π/2 indicate clockwise rotation. Read-only; set with <see
        /// cref="SetAngleOfRotationAsync(double)"/>.
        /// </summary>
        /// <remarks>
        /// Note that this is not the same as axial tilt if the <see cref="Planetoid"/>
        /// is in orbit; in that case axial tilt is relative to the normal of the orbital plane of
        /// the <see cref="Planetoid"/>, not the Y-axis.
        /// </remarks>
        public double AngleOfRotation => _angleOfRotation ?? 0;

        private Number? _angularVelocity;
        /// <summary>
        /// The angular velocity of this <see cref="Planetoid"/>, in radians per second. Read-only;
        /// set via <see cref="RotationalPeriod"/>.
        /// </summary>
        public Number AngularVelocity
            => _angularVelocity ??= RotationalPeriod.IsZero ? Number.Zero : MathConstants.TwoPI / RotationalPeriod;

        private protected Atmosphere? _atmosphere;
        /// <summary>
        /// The atmosphere possessed by this <see cref="Planetoid"/>.
        /// </summary>
        public Atmosphere Atmosphere => _atmosphere ??= new Atmosphere(0);

        private protected double? _axialPrecession;
        /// <summary>
        /// The angle between the X-axis and the orbital vector at which the first equinox occurs.
        /// Read-only. Set by defining an <see cref="CelestialLocation.Orbit"/> and using <see
        /// cref="SetAxialTiltAsync(double)"/> or <see cref="SetAngleOfRotationAsync(double)"/>.
        /// </summary>
        public double AxialPrecession => _axialPrecession ?? 0;

        /// <summary>
        /// The axial tilt of the <see cref="Planetoid"/> relative to its orbital plane, in radians.
        /// Values greater than π/2 indicate clockwise rotation. Read-only; set with <see
        /// cref="SetAxialTiltAsync(double)"/>
        /// </summary>
        /// <remarks>
        /// If the <see cref="Planetoid"/> isn't orbiting anything, this is the same as the angle of
        /// rotation.
        /// </remarks>
        public double AxialTilt => Orbit.HasValue ? AngleOfRotation - Orbit.Value.Inclination : AngleOfRotation;

        private System.Numerics.Vector3? _axis;
        /// <summary>
        /// A <see cref="System.Numerics.Vector3"/> which represents the axis of this <see
        /// cref="Planetoid"/>. Read-only. Set with <see cref="SetAxialTiltAsync(double)"/> or <see
        /// cref="SetAngleOfRotationAsync(double)"/>.
        /// </summary>
        public System.Numerics.Vector3 Axis => _axis ?? System.Numerics.Vector3.UnitY;

        private System.Numerics.Quaternion? _axisRotation;
        /// <summary>
        /// A <see cref="System.Numerics.Quaternion"/> representing the rotation of the axis of this
        /// <see cref="Planetoid"/>. Read-only; set with <see cref="SetAxialTiltAsync(double)"/> or
        /// <see cref="SetAngleOfRotationAsync(double)"/>"/>
        /// </summary>
        public System.Numerics.Quaternion AxisRotation => _axisRotation ?? System.Numerics.Quaternion.Identity;

        private protected bool? _hasMagnetosphere;
        /// <summary>
        /// Indicates whether this <see cref="Planetoid"/> has a strong magnetosphere.
        /// </summary>
        public bool HasMagnetosphere
        {
            get => _hasMagnetosphere ??= GetHasMagnetosphere();
            set => _hasMagnetosphere = value;
        }

        private protected double? _maxElevation;
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
        public double MaxElevation => _maxElevation ??= GetMaxElevation();

        private protected double? _maxFlow;
        /// <summary>
        /// The maximum flow rate of waterways on this planetoid, in m³/s.
        /// </summary>
        public double MaxFlow
        {
            get => _maxFlow ?? 350000;
            set => _maxFlow = value.IsNearlyEqualTo(350000) ? (double?)null : value;
        }

        private protected Number? _rotationalOffset;
        /// <summary>
        /// The amount of seconds after the beginning of time of the orbited body's transit at the
        /// prime meridian, if <see cref="RotationalPeriod"/> was unchanged (i.e. solar noon, on a
        /// planet which orbits a star).
        /// </summary>
        public Number RotationalOffset => _rotationalOffset ??= Randomizer.Instance.NextNumber(RotationalPeriod);

        private protected Number? _rotationalPeriod;
        /// <summary>
        /// The length of time it takes for this <see cref="Planetoid"/> to rotate once about its axis, in seconds.
        /// </summary>
        public Number RotationalPeriod => _rotationalPeriod ??= Number.Zero;

        private protected List<Resource>? _resources;
        /// <summary>
        /// The resources of this <see cref="Planetoid"/>.
        /// </summary>
        public IEnumerable<Resource> Resources
        {
            get
            {
                if (_resources is null)
                {
                    _resources = new List<Resource>();
                    GenerateResources();
                }
                return _resources;
            }
        }

        private double? _seaLevel;
        /// <summary>
        /// The elevation of sea level relative to the mean surface elevation of the planet, in
        /// meters.
        /// </summary>
        public double SeaLevel
        {
            get => _seaLevel ??= _normalizedSeaLevel * MaxElevation;
            set
            {
                _seaLevel = value;
                _normalizedSeaLevel = value / MaxElevation;
            }
        }

        private protected List<SurfaceRegion>? _surfaceRegions;
        /// <summary>
        /// The collection of <see cref="SurfaceRegion"/> instances which describe the surface of
        /// this <see cref="Planetoid"/>.
        /// </summary>
        public IEnumerable<SurfaceRegion> SurfaceRegions
            => _surfaceRegions ?? Enumerable.Empty<SurfaceRegion>();

        internal bool HasDepthMap => _depthMap != null;

        internal bool HasElevationMap => _elevationMap != null;

        internal bool HasFlowMap => _flowMap != null;

        internal bool HasHydrologyMaps
            => _depthMap != null
            && _flowMap != null;

        internal bool HasPrecipitationMap => _precipitationMaps != null;

        internal bool HasSnowfallMap => _snowfallMaps != null;

        internal bool HasTemperatureMap => _temperatureMapSummer != null || _temperatureMapWinter != null;

        internal bool HasAllWeatherMaps
            => _precipitationMaps != null
            && _snowfallMaps != null
            && _temperatureMapSummer != null
            && _temperatureMapWinter != null;

        internal bool HasAnyWeatherMaps
            => _precipitationMaps != null
            || _snowfallMaps != null
            || _temperatureMapSummer != null
            || _temperatureMapWinter != null;

        private double? _insolationFactor_Equatorial;
        internal double InsolationFactor_Equatorial
        {
            get => _insolationFactor_Equatorial ??= GetInsolationFactor();
            set => _insolationFactor_Equatorial = value;
        }

        internal int MappedSeasons => _precipitationMaps?.Length ?? 0;

        private double? _summerSolsticeTrueAnomaly;
        internal double SummerSolsticeTrueAnomaly
            => _summerSolsticeTrueAnomaly ??= (AxialPrecession + MathAndScience.Constants.Doubles.MathConstants.HalfPI) % MathAndScience.Constants.Doubles.MathConstants.TwoPI;

        private double? _winterSolsticeTrueAnomaly;
        internal double WinterSolsticeTrueAnomaly
            => _winterSolsticeTrueAnomaly ??= (AxialPrecession + MathAndScience.Constants.Doubles.MathConstants.ThreeHalvesPI) % MathAndScience.Constants.Doubles.MathConstants.TwoPI;

        private protected virtual double DensityForType => 2000;

        private double? _eccentricity;
        private protected double Eccentricity
        {
            get => _eccentricity ??= GetEccentricity();
            set => _eccentricity = value;
        }

        private protected virtual Number ExtremeRotationalPeriod => new Number(1100000);

        private protected virtual bool HasFlatSurface => false;

        private double? _insolationFactor_Polar;
        private double InsolationFactor_Polar => _insolationFactor_Polar ??= GetInsolationFactor(true);

        private double? _lapseRateDry;
        private protected double LapseRateDry => _lapseRateDry ??= (double)SurfaceGravity / MathAndScience.Constants.Doubles.ScienceConstants.CpDryAir;

        private protected virtual Number MagnetosphereChanceFactor => Number.One;

        private protected Number? _maxMass;
        private protected Number MaxMass
        {
            get => (_maxMass ?? MaxMassForType) ?? 0;
            set => _maxMass = value;
        }

        private protected virtual Number? MaxMassForType => null;

        private protected virtual Number MaxRotationalPeriod => new Number(100000);

        private protected virtual int MaxSatellites => 1;

        private protected Number MinMass => MinMassForType ?? 0;

        private protected virtual Number? MinMassForType => null;

        private protected virtual Number MinRotationalPeriod => new Number(8000);

        private Number? _minSatellitePeriapsis;
        private Number MinSatellitePeriapsis => _minSatellitePeriapsis ??= GetMinSatellitePeriapsis();

        private FastNoise? _noise1;
        private FastNoise Noise1 => _noise1 ??= new FastNoise(_seed1, 0.01, FastNoise.NoiseType.SimplexFractal, octaves: 6);

        private FastNoise? _noise2;
        private FastNoise Noise2 => _noise2 ??= new FastNoise(_seed2, 0.01, FastNoise.NoiseType.SimplexFractal, octaves: 5);

        private FastNoise? _noise3;
        private FastNoise Noise3 => _noise3 ??= new FastNoise(_seed3, 0.01, FastNoise.NoiseType.SimplexFractal, octaves: 4);

        private FastNoise? _noise4;
        private FastNoise Noise4 => _noise4 ??= new FastNoise(_seed4, 0.01, FastNoise.NoiseType.SimplexFractal, octaves: 3);

        private FastNoise? _noise5;
        private FastNoise Noise5 => _noise5 ??= new FastNoise(_seed5, 0.004, FastNoise.NoiseType.Simplex);

        private protected virtual Number Rigidity => new Number(3, 10);

        /// <summary>
        /// Initializes a new instance of <see cref="Planetoid"/>.
        /// </summary>
        internal Planetoid() => Init();

        /// <summary>
        /// Initializes a new instance of <see cref="Planetoid"/> with the given parameters.
        /// </summary>
        /// <param name="parentId">The id of the location which contains this one.</param>
        /// <param name="position">The initial position of this <see cref="Planetoid"/>.</param>
        internal Planetoid(string? parentId, Vector3 position) : base(parentId, position) => Init();

        /// <summary>
        /// Initializes a new instance of <see cref="Planetoid"/> with the given parameters.
        /// </summary>
        /// <param name="parentId">The id of the location which contains this one.</param>
        /// <param name="position">The initial position of this <see cref="Planetoid"/>.</param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="Planetoid"/> during random generation, in kg.
        /// </param>
        internal Planetoid(string? parentId, Vector3 position, Number maxMass) : base(parentId, position) => MaxMass = maxMass;

        private protected Planetoid(
            string id,
            string? name,
            bool isPrepopulated,
            double? albedo,
            Vector3 velocity,
            double normalizedSeaLevel,
            int seed1,
            int seed2,
            int seed3,
            int seed4,
            int seed5,
            double? angleOfRotation,
            Atmosphere? atmosphere,
            double? axialPrecession,
            bool? hasMagnetosphere,
            double? maxElevation,
            Number? rotationalOffset,
            Number? rotationalPeriod,
            List<Resource>? resources,
            List<string>? satelliteIds,
            List<SurfaceRegion>? surfaceRegions,
            Number? maxMass,
            Orbit? orbit,
            IMaterial? material,
            string? parentId,
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
                name,
                isPrepopulated,
                albedo,
                velocity,
                orbit,
                material,
                parentId)
        {
            _normalizedSeaLevel = normalizedSeaLevel;
            _seed1 = seed1;
            _seed2 = seed2;
            _seed3 = seed3;
            _seed4 = seed4;
            _seed5 = seed5;
            _angleOfRotation = angleOfRotation;
            _atmosphere = atmosphere;
            _axialPrecession = axialPrecession;
            _hasMagnetosphere = hasMagnetosphere;
            _maxElevation = maxElevation;
            _rotationalOffset = rotationalOffset;
            _rotationalPeriod = rotationalPeriod;
            _resources = resources;
            _satelliteIDs = satelliteIds;
            _surfaceRegions = surfaceRegions;
            _maxMass = maxMass;
            _depthMap = depthMap;
            _elevationMap = elevationMap;
            _flowMap = flowMap;
            _precipitationMaps = precipitationMaps;
            _snowfallMaps = snowfallMaps;
            _temperatureMapSummer = temperatureMapSummer;
            _temperatureMapWinter = temperatureMapWinter;
            _maxFlow = maxFlow;
        }

        private Planetoid(SerializationInfo info, StreamingContext context) : this(
            (string)info.GetValue(nameof(Id), typeof(string)),
            (string?)info.GetValue(nameof(Name), typeof(string)),
            (bool)info.GetValue(nameof(_isPrepopulated), typeof(bool)),
            (double?)info.GetValue(nameof(_albedo), typeof(double?)),
            (Vector3)info.GetValue(nameof(Velocity), typeof(Vector3)),
            (double)info.GetValue(nameof(_normalizedSeaLevel), typeof(double)),
            (int)info.GetValue(nameof(_seed1), typeof(int)),
            (int)info.GetValue(nameof(_seed2), typeof(int)),
            (int)info.GetValue(nameof(_seed3), typeof(int)),
            (int)info.GetValue(nameof(_seed4), typeof(int)),
            (int)info.GetValue(nameof(_seed5), typeof(int)),
            (double?)info.GetValue(nameof(_angleOfRotation), typeof(double?)),
            (Atmosphere?)info.GetValue(nameof(Atmosphere), typeof(Atmosphere)),
            (double?)info.GetValue(nameof(_axialPrecession), typeof(double?)),
            (bool?)info.GetValue(nameof(HasMagnetosphere), typeof(bool?)),
            (double?)info.GetValue(nameof(MaxElevation), typeof(double?)),
            (Number?)info.GetValue(nameof(RotationalOffset), typeof(Number?)),
            (Number?)info.GetValue(nameof(RotationalPeriod), typeof(Number?)),
            (List<Resource>?)info.GetValue(nameof(Resources), typeof(List<Resource>)),
            (List<string>?)info.GetValue(nameof(_satelliteIDs), typeof(List<string>)),
            (List<SurfaceRegion>?)info.GetValue(nameof(SurfaceRegions), typeof(List<SurfaceRegion>)),
            (Number?)info.GetValue(nameof(MaxMass), typeof(Number?)),
            (Orbit?)info.GetValue(nameof(Orbit), typeof(Orbit?)),
            (IMaterial?)info.GetValue(nameof(_material), typeof(IMaterial)),
            (string)info.GetValue(nameof(ParentId), typeof(string)),
            (byte[])info.GetValue(nameof(_depthMap), typeof(byte[])),
            (byte[])info.GetValue(nameof(_elevationMap), typeof(byte[])),
            (byte[])info.GetValue(nameof(_flowMap), typeof(byte[])),
            (byte[][])info.GetValue(nameof(_precipitationMaps), typeof(byte[][])),
            (byte[][])info.GetValue(nameof(_snowfallMaps), typeof(byte[][])),
            (byte[])info.GetValue(nameof(_temperatureMapSummer), typeof(byte[])),
            (byte[])info.GetValue(nameof(_temperatureMapWinter), typeof(byte[])),
            (double?)info.GetValue(nameof(_maxFlow), typeof(double?))) { }

        /// <summary>
        /// Gets a new instance of the indicated <see cref="Planetoid"/> type.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="Planetoid"/> to generate.</typeparam>
        /// <param name="parentId">The id of the location which contains the new one.</param>
        /// <param name="position">The position of the new location relative to the center of its
        /// parent.</param>
        /// <param name="maxMass">The maximum mass allowed for the new <see
        /// cref="Planetoid"/>.</param>
        /// <param name="orbit">The orbit to set for the new <see cref="Planetoid"/>, if
        /// any.</param>
        /// <returns>A new instance of the indicated <see cref="Planetoid"/> type, or <see
        /// langword="null"/> if no instance could be generated with the given parameters.</returns>
        public static async Task<T?> GetNewInstanceAsync<T>(string? parentId, Vector3 position, Number maxMass, OrbitalParameters? orbit = null) where T : Planetoid
        {
            var instance = typeof(T).InvokeMember(
                null,
                BindingFlags.CreateInstance | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                null,
                new object?[] { parentId, position }) as T;
            if (instance != null)
            {
                instance.MaxMass = maxMass;
                await instance.GenerateMaterialAsync().ConfigureAwait(false);
                if (orbit.HasValue)
                {
                    await Space.Orbit.SetOrbitAsync(instance, orbit.Value).ConfigureAwait(false);
                }
                await instance.InitializeBaseAsync(parentId).ConfigureAwait(false);
            }
            return instance;
        }

        /// <summary>
        /// Gets a new instance of the indicated <see cref="Planetoid"/> type.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="Planetoid"/> to generate.</typeparam>
        /// <param name="parent">The location which contains the new one.</param>
        /// <param name="position">The position of the new location relative to the center of its
        /// <paramref name="parent"/>.</param>
        /// <param name="star">The star the new <see cref="Planetoid"/> will orbit.</param>
        /// <returns>A new instance of the indicated <see cref="Planetoid"/> type, or <see
        /// langword="null"/> if no instance could be generated with the given parameters.</returns>
        public static async Task<T?> GetNewInstanceAsync<T>(Location? parent, Vector3 position, Star star) where T : Planetoid
        {
            var instance = typeof(T).InvokeMember(
                null,
                BindingFlags.CreateInstance | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                null,
                new object?[] { parent?.Id, position }) as T;
            if (instance != null)
            {
                await instance.GenerateMaterialAsync().ConfigureAwait(false);
                await instance.GenerateOrbitAsync(star).ConfigureAwait(false);
                await instance.InitializeBaseAsync(parent).ConfigureAwait(false);
            }
            return instance;
        }

        /// <summary>
        /// Gets a new instance of the indicated <see cref="Planetoid"/> type.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="Planetoid"/> to generate.</typeparam>
        /// <param name="parentId">The id of the location which contains the new one.</param>
        /// <param name="position">The position of the new location relative to the center of its
        /// parent.</param>
        /// <param name="star">The star the new <see cref="Planetoid"/> will orbit.</param>
        /// <returns>A new instance of the indicated <see cref="Planetoid"/> type, or <see
        /// langword="null"/> if no instance could be generated with the given parameters.</returns>
        public static async Task<T?> GetNewInstanceAsync<T>(string? parentId, Vector3 position, Star star) where T : Planetoid
        {
            var instance = typeof(T).InvokeMember(
                null,
                BindingFlags.CreateInstance | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                null,
                new object?[] { parentId, position }) as T;
            if (instance != null)
            {
                await instance.GenerateMaterialAsync().ConfigureAwait(false);
                await instance.GenerateOrbitAsync(star).ConfigureAwait(false);
                await instance.InitializeBaseAsync(parentId).ConfigureAwait(false);
            }
            return instance;
        }

        private static byte[] GetByteArray(Bitmap image)
        {
            using var stream = new MemoryStream();
            image.Save(stream, System.Drawing.Imaging.ImageFormat.Bmp);
            return stream.ToArray();
        }

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
        /// Removes this location and all contained children from the data store, as well as all
        /// satellites.
        /// </summary>
        public override async Task DeleteAsync()
        {
            await foreach (var satellite in GetSatellitesAsync())
            {
                await satellite.DeleteAsync().ConfigureAwait(false);
            }
            await base.DeleteAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Calculates the atmospheric density for the given conditions, in kg/m³.
        /// </summary>
        /// <param name="time">The time at which to make the calculation.</param>
        /// <param name="latitude">The latitude of the object.</param>
        /// <param name="longitude">The longitude of the object.</param>
        /// <param name="altitude">The altitude of the object.</param>
        /// <returns>The atmospheric density for the given conditions, in kg/m³.</returns>
        public async Task<double> GetAtmosphericDensityAsync(Duration time, double latitude, double longitude, double altitude)
        {
            var surfaceTemp = await GetSurfaceTemperatureAtLatLonAsync(time, latitude, longitude).ConfigureAwait(false);
            var tempAtElevation = await GetTemperatureAtElevationAsync(surfaceTemp, altitude).ConfigureAwait(false);
            return Atmosphere.GetAtmosphericDensity(this, tempAtElevation, altitude);
        }

        /// <summary>
        /// Calculates the atmospheric drag on a spherical object within the <see
        /// cref="Atmosphere"/> of this <see cref="Planetoid"/> under given conditions, in N.
        /// </summary>
        /// <param name="time">The time at which to make the calculation.</param>
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
        public async Task<double> GetAtmosphericDragAsync(Duration time, double latitude, double longitude, double altitude, double speed)
        {
            var surfaceTemp = await GetSurfaceTemperatureAtLatLonAsync(time, latitude, longitude).ConfigureAwait(false);
            var tempAtElevation = await GetTemperatureAtElevationAsync(surfaceTemp, altitude).ConfigureAwait(false);
            return Atmosphere.GetAtmosphericDrag(this, tempAtElevation, altitude, speed);
        }

        /// <summary>
        /// Calculates the atmospheric pressure at a given <paramref name="latitude"/> and <paramref
        /// name="longitude"/>, at the given true anomaly of the planet's orbit, in kPa.
        /// </summary>
        /// <param name="time">The time at which to make the calculation.</param>
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
        public async Task<double> GetAtmosphericPressureAsync(Duration time, double latitude, double longitude)
        {
            var elevation = GetElevationAt(latitude, longitude);
            var trueAnomaly = Orbit?.GetTrueAnomalyAtTime(time) ?? 0;
            var blackbodyTemperature = await GetBlackbodyTemperatureAsync().ConfigureAwait(false);
            var greenhouseEffect = await GetGreenhouseEffectAsync().ConfigureAwait(false);
            var lat = GetSeasonalLatitude(latitude, trueAnomaly);
            var tempAtElevation = await GetTemperatureAtElevationAsync(
                (blackbodyTemperature * GetInsolationFactor(lat)) + greenhouseEffect,
                elevation).ConfigureAwait(false);
            return GetAtmosphericPressureFromTempAndElevation(tempAtElevation, elevation);
        }

        /// <summary>
        /// Get the average surface temperature of this <see cref="Planetoid"/> near its poles
        /// throughout its orbit (or at its current position, if it is not in orbit), in K.
        /// </summary>
        public async Task<double> GetAveragePolarSurfaceTemperatureAsync()
        {
            _averagePolarSurfaceTemperature ??= await GetAverageTemperatureAsync(true).ConfigureAwait(false);
            return _averagePolarSurfaceTemperature.Value;
        }

        /// <summary>
        /// The average surface temperature of the <see cref="Planetoid"/> near its equator
        /// throughout its orbit (or at its current position, if it is not in orbit), in K.
        /// </summary>
        public override async Task<double> GetAverageSurfaceTemperatureAsync()
        {
            _averageSurfaceTemperature ??= await GetAverageTemperatureAsync().ConfigureAwait(false);
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
        public SurfaceRegion GetContainingSurfaceRegion(Vector3 position)
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
        public SurfaceRegion GetContainingSurfaceRegion(Location other)
            => SurfaceRegions.Where(x => Vector3.Distance(x.Position, other.Position) <= x.Shape.ContainingRadius - other.Shape.ContainingRadius)
            .ItemWithMin(x => x.Shape.ContainingRadius);

        /// <summary>
        /// Gets the stored hydrology depth map image for this region, if any.
        /// </summary>
        /// <returns>The stored hydrology depth map image for this region, if any.</returns>
        public Bitmap? GetDepthMap() => SurfaceRegion.GetMapImage(_depthMap);

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
        /// Gets the elevation at the given <paramref name="position"/>, in meters.
        /// </summary>
        /// <param name="position">The position at which to determine elevation.</param>
        /// <returns>The elevation at the given <paramref name="position"/>, in meters.</returns>
        public double GetElevationAt(Vector3 position)
            => GetNormalizedElevationAt(position) * MaxElevation;

        /// <summary>
        /// Gets the elevation at the given <paramref name="latitude"/> and <paramref
        /// name="longitude"/>, in meters.
        /// </summary>
        /// <param name="latitude">The latitude at which to determine elevation.</param>
        /// <param name="longitude">The longitude at which to determine elevation.</param>
        /// <returns>The elevation at the given <paramref name="latitude"/> and <paramref
        /// name="longitude"/>, in meters.</returns>
        public double GetElevationAt(double latitude, double longitude)
            => GetElevationAt(LatitudeAndLongitudeToVector(latitude, longitude));

        /// <summary>
        /// Gets the stored elevation map image for this region, if any.
        /// </summary>
        /// <returns>The stored elevation map image for this region, if any.</returns>
        public Bitmap? GetElevationMap() => SurfaceRegion.GetMapImage(_elevationMap);

        /// <summary>
        /// Gets the stored hydrology flow map image for this region, if any.
        /// </summary>
        /// <returns>The stored hydrology flow map image for this region, if any.</returns>
        public Bitmap? GetFlowMap() => SurfaceRegion.GetMapImage(_flowMap);

        /// <summary>
        /// Gets the greenhouse effect of this planet's atmosphere.
        /// </summary>
        /// <returns></returns>
        public async Task<double> GetGreenhouseEffectAsync()
        {
            _greenhouseEffect ??= await GetGreenhouseEffectAsync(InsolationFactor_Equatorial, Atmosphere.GreenhouseFactor).ConfigureAwait(false);
            return _greenhouseEffect.Value;
        }

        /// <summary>
        /// Calculates the total illumination on the given position from nearby sources of light
        /// (stars in the same system), as well as the light reflected from any natural satellites,
        /// modified according to the angle of incidence at the given time, in lux (lumens per m²).
        /// </summary>
        /// <param name="time">The time at which to make the calculation.</param>
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
        public async Task<double> GetIlluminationAsync(Duration time, double latitude, double longitude)
        {
            var pos = await GetPositionAtTimeAsync(time).ConfigureAwait(false);
            Planetoid? stellarOrbiter = null;
            if (Orbit.HasValue)
            {
                var orbited = Orbit.HasValue ? await Orbit.Value.GetOrbitedObjectAsync().ConfigureAwait(false) : null;
                if (orbited is Star)
                {
                    stellarOrbiter = this;
                }
                else if (orbited is Planetoid planet && planet.Orbit.HasValue)
                {
                    var otherOrbited = await planet.Orbit!.Value.GetOrbitedObjectAsync().ConfigureAwait(false);
                    if (otherOrbited is Star)
                    {
                        stellarOrbiter = planet;
                    }
                }
            }
            var stellarOrbit = stellarOrbiter?.Orbit;
            var starPos = Vector3.Zero;
            if (stellarOrbit.HasValue)
            {
                var stellarOrbited = await stellarOrbit.Value.GetOrbitedObjectAsync().ConfigureAwait(false);
                if (stellarOrbited != null)
                {
                    starPos = await stellarOrbited.GetPositionAtTimeAsync(time).ConfigureAwait(false);
                }
            }

            var (solarRightAscension, solarDeclination) = GetRightAscensionAndDeclination(pos, starPos);
            var longitudeOffset = longitude - solarRightAscension;
            if (longitudeOffset > Math.PI)
            {
                longitudeOffset -= MathAndScience.Constants.Doubles.MathConstants.TwoPI;
            }

            var sinSolarElevation = (Math.Sin(solarDeclination) * Math.Sin(latitude))
                + (Math.Cos(solarDeclination) * Math.Cos(latitude) * Math.Cos(longitudeOffset));
            var solarElevation = Math.Asin(sinSolarElevation);
            var lux = solarElevation <= 0 ? 0 : (await GetLuminousFluxAsync().ConfigureAwait(false)) * sinSolarElevation;

            var starDist = Vector3.Distance(pos, starPos);
            var (_, starLon) = GetEclipticLatLon(pos, starPos);
            await foreach (var satellite in GetSatellitesAsync())
            {
                var satPos = await satellite.GetPositionAtTimeAsync(time).ConfigureAwait(false);
                var satDist2 = Vector3.DistanceSquared(pos, satPos);
                var satDist = satDist2.Sqrt();

                var (satLat, satLon) = GetEclipticLatLon(pos, satPos);

                // satellite-centered elongation of the planet from the star (ratio of illuminated
                // surface area to total surface area)
                var le = Math.Acos(Math.Cos(satLat) * Math.Cos(starLon - satLon));
                var e = Math.Atan2((double)(satDist - (starDist * Math.Cos(le))), (double)(starDist * Math.Sin(le)));
                // fraction of illuminated surface area
                var phase = (1 + Math.Cos(e)) / 2;

                // Total light from the satellite is the flux incident on the satellite, reduced
                // according to the proportion lit (vs. shadowed), further reduced according to the
                // albedo, then the distance the light must travel after being reflected.
                lux += (await satellite.GetLuminousFluxAsync().ConfigureAwait(false))
                    * phase
                    * await satellite.GetAlbedoAsync().ConfigureAwait(false)
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
            var position = LatitudeAndLongitudeToVector(latitude, longitude);
            var elevation = GetNormalizedElevationAt(position);

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
        /// <param name="time">The time at which to make the calculation.</param>
        /// <param name="latitude">The latitude at which to make the calculation.</param>
        /// <returns>A pair of <see cref="RelativeDuration"/> instances set to a proportion of a
        /// local day since midnight. If the sun does not rise and set on the given day (e.g. near
        /// the poles), then <see langword="null"/> will be returned for sunrise in the case of a
        /// polar night, and <see langword="null"/> for sunset in the case of a midnight sun.</returns>
        public async Task<(RelativeDuration? sunrise, RelativeDuration? sunset)> GetLocalSunriseAndSunsetAsync(Duration time, double latitude)
        {
            var pos = await GetPositionAtTimeAsync(time).ConfigureAwait(false);
            var starPos = Vector3.Zero;
            if (Orbit.HasValue)
            {
                var orbited = await Orbit.Value.GetOrbitedObjectAsync().ConfigureAwait(false);
                if (orbited is Star)
                {
                    starPos = await orbited.GetPositionAtTimeAsync(time).ConfigureAwait(false);
                }
                else if (orbited is Planetoid && orbited.Orbit.HasValue)
                {
                    var otherOrbited = await orbited.Orbit.Value.GetOrbitedObjectAsync().ConfigureAwait(false);
                    if (otherOrbited is Star)
                    {
                        starPos = await otherOrbited.GetPositionAtTimeAsync(time).ConfigureAwait(false);
                    }
                }
            }

            var (_, solarDeclination) = GetRightAscensionAndDeclination(pos, starPos);

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
        /// Gets the time of day at the given <paramref name="time"/> and <paramref
        /// name="longitude"/>, based on the planet's rotation, as a proportion of a day since
        /// midnight.
        /// </summary>
        /// <param name="time">The time at which to make the calculation.</param>
        /// <param name="longitude">The longitude at which to make the calculation.</param>
        /// <returns>A <see cref="RelativeDuration"/> set to a proportion of a local day since
        /// midnight.</returns>
        public async Task<RelativeDuration> GetLocalTimeOfDayAsync(Duration time, double longitude)
        {
            var pos = await GetPositionAtTimeAsync(time).ConfigureAwait(false);
            var starPos = Vector3.Zero;
            if (Orbit.HasValue)
            {
                var orbited = await Orbit.Value.GetOrbitedObjectAsync().ConfigureAwait(false);
                if (orbited is Star)
                {
                    starPos = await orbited.GetPositionAtTimeAsync(time).ConfigureAwait(false);
                }
                else if (orbited is Planetoid && orbited.Orbit.HasValue)
                {
                    var otherOrbited = await orbited.Orbit.Value.GetOrbitedObjectAsync().ConfigureAwait(false);
                    if (otherOrbited is Star)
                    {
                        starPos = await otherOrbited.GetPositionAtTimeAsync(time).ConfigureAwait(false);
                    }
                }
            }

            var (solarRightAscension, _) = GetRightAscensionAndDeclination(pos, starPos);
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
        /// Gets the approximate maximum surface temperature of this <see cref="Planetoid"/>, in K.
        /// </summary>
        /// <remarks>
        /// Gets the equatorial temperature at periapsis, or at the current position if not in orbit.
        /// </remarks>
        public async Task<double> GetMaxSurfaceTemperatureAsync()
        {
            if (!_maxSurfaceTemperature.HasValue)
            {
                var greenhouseEffect = await GetGreenhouseEffectAsync().ConfigureAwait(false);
                var temp = await GetTemperatureAtPeriapsisAsync().ConfigureAwait(false);
                _maxSurfaceTemperature = (temp * InsolationFactor_Equatorial) + greenhouseEffect;
            }
            return _maxSurfaceTemperature.Value;
        }

        /// <summary>
        /// Gets the approximate minimum surface temperature of this <see cref="Planetoid"/>, in K.
        /// </summary>
        /// <remarks>
        /// Gets the polar temperature at apoapsis, or at the current position if not in orbit.
        /// </remarks>
        public async Task<double> GetMinSurfaceTemperatureAsync()
        {
            if (!_minSurfaceTemperature.HasValue)
            {
                var variation = await GetDiurnalTemperatureVariationAsync().ConfigureAwait(false);
                var greenhouseEffect = await GetGreenhouseEffectAsync().ConfigureAwait(false);
                var temp = await GetTemperatureAtApoapsisAsync().ConfigureAwait(false);
                _minSurfaceTemperature = (temp * InsolationFactor_Polar) + greenhouseEffect - variation;
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
            => GetNormalizedElevationAt((double)position.X, (double)position.Y, (double)position.Z);

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
            info.AddValue(nameof(Name), Name);
            info.AddValue(nameof(_isPrepopulated), _isPrepopulated);
            info.AddValue(nameof(_albedo), _albedo);
            info.AddValue(nameof(Velocity), Velocity);
            info.AddValue(nameof(_normalizedSeaLevel), _normalizedSeaLevel);
            info.AddValue(nameof(_seed1), _seed1);
            info.AddValue(nameof(_seed2), _seed2);
            info.AddValue(nameof(_seed3), _seed3);
            info.AddValue(nameof(_seed4), _seed4);
            info.AddValue(nameof(_seed5), _seed5);
            info.AddValue(nameof(_angleOfRotation), _angleOfRotation);
            info.AddValue(nameof(Atmosphere), _atmosphere);
            info.AddValue(nameof(_axialPrecession), _axialPrecession);
            info.AddValue(nameof(HasMagnetosphere), _hasMagnetosphere);
            info.AddValue(nameof(MaxElevation), _maxElevation);
            info.AddValue(nameof(RotationalOffset), _rotationalOffset);
            info.AddValue(nameof(RotationalPeriod), _rotationalPeriod);
            info.AddValue(nameof(Resources), _resources);
            info.AddValue(nameof(_satelliteIDs), _satelliteIDs);
            info.AddValue(nameof(SurfaceRegions), _surfaceRegions);
            info.AddValue(nameof(MaxMass), _maxMass);
            info.AddValue(nameof(Orbit), _orbit);
            info.AddValue(nameof(_material), _material);
            info.AddValue(nameof(ParentId), ParentId);
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
                maps[i] = SurfaceRegion.GetMapImage(_precipitationMaps[i])!;
            }
            return maps;
        }

        /// <summary>
        /// Determines the proportion of the current season at the given <paramref name="time"/>.
        /// </summary>
        /// <param name="numSeasons">The number of seasons.</param>
        /// <param name="time">The time at which to make the determination.</param>
        /// <returns>>The proportion of the current season, as a value between 0.0 and 1.0, at the
        /// given <paramref name="time"/>.</returns>
        public double GetProportionOfSeasonAtTime(uint numSeasons, Duration time)
        {
            var proportionOfYear = GetProportionOfYearAtTime(time);
            var proportionPerSeason = 1.0 / numSeasons;
            var seasonIndex = Math.Floor(proportionOfYear / proportionPerSeason);
            return (proportionOfYear - (seasonIndex * proportionPerSeason)) / proportionPerSeason;
        }

        /// <summary>
        /// Determines the proportion of a year, starting and ending with midwinter, at the given
        /// <paramref name="time"/>.
        /// </summary>
        /// <param name="time">The time at which to make the calculation.</param>
        /// <returns>The proportion of the year, starting and ending with midwinter, at the given
        /// <paramref name="time"/>.</returns>
        public double GetProportionOfYearAtTime(Duration time)
            => ((Orbit?.GetTrueAnomalyAtTime(time) ?? 0) - WinterSolsticeTrueAnomaly + MathAndScience.Constants.Doubles.MathConstants.TwoPI) % MathAndScience.Constants.Doubles.MathConstants.TwoPI / MathAndScience.Constants.Doubles.MathConstants.TwoPI;

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
        /// <param name="time">The time at which to make the calculation.</param>
        /// <param name="satellite">A natural satellite of this body. If the specified body is not
        /// one of this one's satellites, the return value will always be <c>(0.0, <see
        /// langword="false"/>)</c>.</param>
        /// <returns>The proportion of the satellite which is currently illuminated, and a boolean
        /// value indicating whether the body is in the waxing half of its cycle (vs. the waning
        /// half).</returns>
        public async Task<(Number phase, bool waxing)> GetSatellitePhaseAsync(Duration time, Planetoid satellite)
        {
            if (_satelliteIDs?.Contains(satellite.Id) != true || !satellite.Orbit.HasValue)
            {
                return (Number.Zero, false);
            }
            var satOrbited = await satellite.Orbit.Value.GetOrbitedObjectAsync().ConfigureAwait(false);
            if (satOrbited != this)
            {
                return (Number.Zero, false);
            }

            var pos = await GetPositionAtTimeAsync(time).ConfigureAwait(false);

            var starPos = Vector3.Zero;
            var orbited = Orbit.HasValue
                ? await Orbit.Value.GetOrbitedObjectAsync().ConfigureAwait(false)
                : null;
            if (orbited is Star)
            {
                starPos = await orbited.GetPositionAtTimeAsync(time).ConfigureAwait(false);
            }
            else if (orbited is Planetoid && orbited.Orbit.HasValue)
            {
                var otherOrbited = await orbited.Orbit.Value.GetOrbitedObjectAsync().ConfigureAwait(false);
                if (otherOrbited is Star)
                {
                    starPos = await otherOrbited.GetPositionAtTimeAsync(time).ConfigureAwait(false);
                }
            }
            var starDist = Vector3.Distance(pos, starPos);
            var (_, starLon) = GetEclipticLatLon(pos, starPos);

            var satellitePosition = await satellite.GetPositionAtTimeAsync(time).ConfigureAwait(false);
            var satDist2 = Vector3.DistanceSquared(pos, satellitePosition);
            var satDist = satDist2.Sqrt();
            var (satLat, satLon) = GetEclipticLatLon(pos, satellitePosition);

            // satellite-centered elongation of the planet from the star (ratio of illuminated
            // surface area to total surface area)
            var le = Math.Acos(Math.Cos(satLat) * Math.Cos(starLon - satLon));
            var e = Math.Atan2((double)(satDist - (starDist * Math.Cos(le))), (double)(starDist * Math.Sin(le)));
            // fraction of illuminated surface area
            var phase = (Number)((1 + Math.Cos(e)) / 2);

            var (planetRightAscension, _) = satellite.GetRightAscensionAndDeclination(satellitePosition, pos);
            var (starRightAscension, _) = satellite.GetRightAscensionAndDeclination(
                satellitePosition,
                orbited is Star star
                    ? await star.GetPositionAtTimeAsync(time).ConfigureAwait(false)
                    : Vector3.Zero);

            return (phase, (starRightAscension - planetRightAscension + MathAndScience.Constants.Doubles.MathConstants.TwoPI) % MathAndScience.Constants.Doubles.MathConstants.TwoPI <= Math.PI);
        }

        /// <summary>
        /// Enumerates the natural satellites around this <see cref="Planetoid"/>.
        /// </summary>
        /// <remarks>
        /// Unlike children, natural satellites are actually siblings in the local <see
        /// cref="Location"/> hierarchy, which merely share an orbital relationship.
        /// </remarks>
        public IAsyncEnumerable<Planetoid> GetSatellitesAsync() => _satelliteIDs is null
            ? AsyncEnumerable.Empty<Planetoid>()
            : DataStore.GetItemsWhereAsync<Planetoid>(x => _satelliteIDs.Contains(x.Id));

        /// <summary>
        /// Determines the proportion of a year, with 0 indicating winter, and 1 indicating summer,
        /// at the given <paramref name="time"/>.
        /// </summary>
        /// <param name="time">The time at which to make the calculation.</param>
        /// <param name="latitude">Used to determine hemisphere.</param>
        /// <returns>The proportion of the year, with 0 indicating winter, and 1 indicating summer,
        /// at the given <paramref name="time"/>.</returns>
        public double GetSeasonalProportionAtTime(Duration time, double latitude)
        {
            var proportionOfYear = GetProportionOfYearAtTime(time);
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
        /// Determines the current season at the given <paramref name="time"/>.
        /// </summary>
        /// <param name="numSeasons">The number of seasons.</param>
        /// <param name="time">The time at which to make the determination.</param>
        /// <returns>The 0-based index of the current season at the given <paramref
        /// name="time"/>.</returns>
        public uint GetSeasonAtTime(uint numSeasons, Duration time) => (uint)Math.Floor(GetProportionOfYearAtTime(time) * numSeasons);

        /// <summary>
        /// Calculates the slope at the given coordinates, as the ratio of rise over run from the
        /// point to the point 1 arc-second away in the cardinal direction which is at the steepest
        /// angle.
        /// </summary>
        /// <param name="latitude">The latitude of the point.</param>
        /// <param name="longitude">The longitude of the point.</param>
        /// <returns>The slope at the given coordinates.</returns>
        public double GetSlope(double latitude, double longitude)
        {
            var position = LatitudeAndLongitudeToVector(latitude, longitude);
            return GetSlope(position, latitude, longitude, GetNormalizedElevationAt(position));
        }

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
                maps[i] = SurfaceRegion.GetMapImage(_snowfallMaps[i])!;
            }
            return maps;
        }

        /// <summary>
        /// Gets the current surface temperature of the <see cref="Planetoid"/> at its equator, in K.
        /// </summary>
        public async Task<double> GetSurfaceTemperatureAsync()
        {
            _surfaceTemperature ??= await GetCurrentSurfaceTemperatureAsync().ConfigureAwait(false);
            return _surfaceTemperature.Value;
        }

        /// <summary>
        /// Calculates the effective surface temperature at the given surface position, including
        /// greenhouse effects, in K.
        /// </summary>
        /// <param name="time">The time at which to make the calculation.</param>
        /// <param name="latitude">
        /// The latitude at which temperature will be calculated.
        /// </param>
        /// <param name="longitude">
        /// The longitude at which temperature will be calculated.
        /// </param>
        /// <returns>The surface temperature, in K.</returns>
        public async Task<double> GetSurfaceTemperatureAtLatLonAsync(Duration time, double latitude, double longitude)
        {
            var blackbodyTemperature = await GetBlackbodyTemperatureAsync().ConfigureAwait(false);
            var greenhouseEffect = await GetGreenhouseEffectAsync().ConfigureAwait(false);
            var lat = GetSeasonalLatitude(latitude, Orbit?.GetTrueAnomalyAtTime(time) ?? 0);
            return await GetTemperatureAtElevationAsync(
                (blackbodyTemperature * GetInsolationFactor(lat)) + greenhouseEffect,
                GetElevationAt(latitude, longitude)).ConfigureAwait(false);
        }

        /// <summary>
        /// Calculates the effective surface temperature at the given surface position, including
        /// greenhouse effects, in K.
        /// </summary>
        /// <param name="time">The time at which to make the calculation.</param>
        /// <param name="position">
        /// The surface position at which temperature will be calculated.
        /// </param>
        /// <returns>The surface temperature, in K.</returns>
        public Task<double> GetSurfaceTemperatureAtSurfacePositionAsync(Duration time, Vector3 position)
            => GetSurfaceTemperatureAtLatLonAsync(time, VectorToLatitude(position), VectorToLongitude(position));

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
        public async Task<FloatRange> GetSurfaceTemperatureRangeAtAsync(double latitude, double elevation)
        {
            var temp = await GetSurfaceTemperatureAtTrueAnomalyAsync(WinterSolsticeTrueAnomaly, GetSeasonalLatitudeFromDeclination(latitude, -AxialTilt)).ConfigureAwait(false);
            var min = await GetTemperatureAtElevationAsync(temp, elevation).ConfigureAwait(false);
            temp = await GetSurfaceTemperatureAtTrueAnomalyAsync(SummerSolsticeTrueAnomaly, GetSeasonalLatitudeFromDeclination(latitude, AxialTilt)).ConfigureAwait(false);
            var max = await GetTemperatureAtElevationAsync(temp, elevation).ConfigureAwait(false);
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
        public async Task<double> GetTemperatureAtElevationAsync(double surfaceTemp, double elevation)
        {
            // When outside the atmosphere, use the black body temperature, ignoring atmospheric effects.
            if (elevation >= Atmosphere.AtmosphericHeight)
            {
                return await GetAverageBlackbodyTemperatureAsync().ConfigureAwait(false);
            }

            if (elevation <= 0)
            {
                return surfaceTemp;
            }
            else
            {
                return surfaceTemp - (elevation * GetLapseRate(surfaceTemp));
            }
        }

        /// <summary>
        /// Gets the stored temperature map image for this region at the summer solstice, if any.
        /// </summary>
        /// <returns>The stored temperature map image for this region at the summer solstice, if
        /// any.</returns>
        public Bitmap? GetTemperatureMapSummer() => SurfaceRegion.GetMapImage(_temperatureMapSummer ?? _temperatureMapWinter);

        /// <summary>
        /// Gets the stored temperature map image for this region at the winter solstice, if any.
        /// </summary>
        /// <returns>The stored temperature map image for this region at the winter solstice, if
        /// any.</returns>
        public Bitmap? GetTemperatureMapWinter() => SurfaceRegion.GetMapImage(_temperatureMapWinter ?? _temperatureMapSummer);

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
            var cosLat = Math.Cos(latitude);
            var rot = _axisRotation ?? System.Numerics.Quaternion.Identity;
            var v = MathAndScience.Numerics.Doubles.Vector3.Normalize(
                MathAndScience.Numerics.Doubles.Vector3.Transform(
                    new MathAndScience.Numerics.Doubles.Vector3(
                        cosLat * Math.Sin(longitude),
                        Math.Sin(latitude),
                        cosLat * Math.Cos(longitude)),
                    MathAndScience.Numerics.Doubles.Quaternion.Inverse(rot)));
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
        public void LoadFlowMap(Bitmap image)
        {
            if (image is null)
            {
                _flowMap = null;
                return;
            }
            _flowMap = GetByteArray(image);
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
        public async Task SetAtmosphericPressureAsync(double value)
        {
            Atmosphere.SetAtmosphericPressure(value);
            await ResetPressureDependentPropertiesAsync().ConfigureAwait(false);
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
        public Task SetAxialTiltAsync(double value) => SetAngleOfRotationAsync(Orbit.HasValue ? value + Orbit.Value.Inclination : value);

        /// <summary>
        /// Sets the length of time it takes for this <see cref="Planetoid"/> to rotate once about
        /// its axis, in seconds.
        /// </summary>
        /// <param name="value">A <see cref="Number"/> value.</param>
        public async Task SetRotationalPeriodAsync(Number value)
        {
            _rotationalPeriod = value;
            _angularVelocity = null;
            await ResetCachedTemperaturesAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Converts a <see cref="Vector3"/> to a latitude, in radians.
        /// </summary>
        /// <param name="v">A vector representing a position on the surface of this <see cref="Planetoid"/>.</param>
        /// <returns>A latitude, as an angle in radians from the equator.</returns>
        /// <remarks>
        /// If the planet's axis has never been set, it is treated as vertical for the purpose of
        /// this calculation, but is not permanently set to such an axis.
        /// </remarks>
        public double VectorToLatitude(Vector3 v)
        {
            var axis = _axis ?? Vector3.UnitY;
            return MathAndScience.Constants.Doubles.MathConstants.HalfPI - (double)axis.Angle(v);
        }

        /// <summary>
        /// Converts a <see cref="Vector3"/> to a longitude, in radians.
        /// </summary>
        /// <param name="v">A vector representing a position on the surface of this <see cref="Planetoid"/>.</param>
        /// <returns>A longitude, as an angle in radians from the X-axis at 0 rotation.</returns>
        /// <remarks>
        /// If the planet's axis has never been set, it is treated as vertical for the purpose of
        /// this calculation, but is not permanently set to such an axis.
        /// </remarks>
        public double VectorToLongitude(Vector3 v)
        {
            var rot = _axisRotation ?? System.Numerics.Quaternion.Identity;
            var u = Vector3.Transform(v, rot);
            return u.X.IsZero && u.Z.IsZero
                ? 0
                : Math.Atan2((double)u.X, (double)u.Z);
        }

        internal async Task GenerateSatellitesAsync()
        {
            if (_satelliteIDs != null || MaxSatellites <= 0)
            {
                return;
            }

            var minPeriapsis = MinSatellitePeriapsis;
            var maxApoapsis = Orbit.HasValue ? (await GetHillSphereRadiusAsync().ConfigureAwait(false)) / 3 : Shape.ContainingRadius * 100;

            while (minPeriapsis <= maxApoapsis && (_satelliteIDs?.Count ?? 0) < MaxSatellites)
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

                var satellite = await GenerateSatelliteAsync(periapsis, eccentricity, maxMass).ConfigureAwait(false);
                if (satellite is null)
                {
                    break;
                }
                await satellite.SaveAsync().ConfigureAwait(false);

                (_satelliteIDs ??= new List<string>()).Add(satellite.Id);

                minPeriapsis = (satellite.Orbit?.Apoapsis ?? 0) + await satellite.GetSphereOfInfluenceRadiusAsync().ConfigureAwait(false);
            }
        }

        internal float[,] GetDepthMap(int width, int height)
            => SurfaceRegion.GetMapFromImage(_depthMap, width, height);

        internal double[,] GetElevationMap(int width, int height)
        {
            using var image = SurfaceRegion.GetMapImage(_elevationMap);
            if (image is null)
            {
                return new double[width, height];
            }
            return image.ImageToDoubleSurfaceMap(width, height);
        }

        internal double GetElevationNoiseAt(double x, double y, double z)
        {
            if (MaxElevation.IsNearlyZero())
            {
                return 0;
            }

            // The magnitude of the position vector is magnified to increase the surface area of the
            // noise map, thus providing a more diverse range of results without introducing
            // excessive noise (as increasing frequency would).
            x *= 100;
            y *= 100;
            z *= 100;

            // Initial noise map.
            var baseNoise = Noise1.GetNoise(x, y, z);

            // In order to avoid an appearance of excessive uniformity, with all mountains reaching
            // the same height, distributed uniformly over the surface, the initial noise is
            // multiplied by a second, independent noise map. The resulting map will have more
            // randomly distributed high and low points.
            var irregularity1 = Math.Abs(Noise2.GetNoise(x, y, z));

            // This process is repeated.
            var irregularity2 = Math.Abs(Noise3.GetNoise(x, y, z));

            var e = baseNoise * irregularity1 * irregularity2;

            // The overall value is magnified to compensate for excessive normalization.
            return e * 2.71;
        }

        internal float[,] GetFlowMap(int width, int height)
            => SurfaceRegion.GetMapFromImage(_flowMap, width, height);

        internal double GetInsolationFactor(Number atmosphereMass, double atmosphericScaleHeight, bool polar = false)
            => (double)Number.Pow(1320000
                * atmosphereMass
                * (polar
                    ? Math.Pow(0.7, Math.Pow(GetPolarAirMass(atmosphericScaleHeight), 0.678))
                    : 0.7)
                / Mass
                , new Number(25, -2));

        internal double GetPrecipitation(MathAndScience.Numerics.Doubles.Vector3 position, double seasonalLatitude, float temperature, float proportionOfYear, out double snow)
            => GetPrecipitation(position.X, position.Y, position.Z, seasonalLatitude, temperature, proportionOfYear, out snow);

        internal double GetPrecipitation(double x, double y, double z, double seasonalLatitude, float temperature, float proportionOfYear, out double snow)
        {
            snow = 0;

            var avgPrecipitation = Atmosphere.AveragePrecipitation * proportionOfYear;

            x *= 1000;
            y *= 1000;
            z *= 1000;

            // Noise map with smooth, broad areas. Random range ~-1-2.
            var r1 = 0.5 + (Noise5.GetNoise(x, y, z) * 1.5);

            // More detailed noise map. Random range of ~-1-1 adjusted to ~0.8-1.
            var r2 = (Noise4.GetNoise(x, y, z) * 0.1) + 0.9;

            // Combined map is noise with broad similarity over regions, and minor local
            // diversity.
            var r = r1 * r2;

            // Hadley cells scale by ~6 around the equator, ~-1 ±15º lat, ~1 ±40º lat, and ~-1
            // ±75º lat; this creates the ITCZ, the subtropical deserts, the temperate zone, and
            // the polar deserts.
            var roundedAbsLatitude = Math.Round(Math.Max(0, Math.Abs(seasonalLatitude) - ThirtySixthPI), 3);
            if (!_HadleyValues.TryGetValue(roundedAbsLatitude, out var hadleyValue))
            {
                hadleyValue = Math.Cos((1.25 * Math.PI * roundedAbsLatitude) + Math.PI) + Math.Max(0, (1 / (10 * (roundedAbsLatitude + 0.015))) - 2);
                _HadleyValues.Add(roundedAbsLatitude, hadleyValue);
            }

            // Relative humidity is the Hadley cell value added to the random value. Range ~-2-~8.
            var relativeHumidity = r + hadleyValue;

            // In the range betwen 0 and 16K below freezing, the value is scaled down; below that
            // range it is cut off completely; above it is unchanged.
            relativeHumidity *= ((temperature - _LowTemp) / 16).Clamp(0, 1);

            // More intense in the tropics.
            if (roundedAbsLatitude < Math.PI / 8)
            {
                relativeHumidity += r1 * ((MathAndScience.Constants.Doubles.MathConstants.EighthPI - roundedAbsLatitude) / MathAndScience.Constants.Doubles.MathConstants.EighthPI);

                // Extreme spikes within the ITCZ.
                if (roundedAbsLatitude < Math.PI / 16
                    && relativeHumidity > 0)
                {
                    relativeHumidity *= 1 + ((r2 - 0.9) * 40);
                }
            }

            if (relativeHumidity <= 0)
            {
                return 0;
            }

            var precipitation = avgPrecipitation * relativeHumidity;

            if (temperature <= CelestialSubstances.WaterMeltingPoint)
            {
                snow = precipitation * Atmosphere.SnowToRainRatio;
            }

            return precipitation;
        }

        internal float[][,] GetPrecipitationMaps(int width, int height)
        {
            var mapImages = GetPrecipitationMaps();
            var maps = new float[mapImages.Length][,];
            for (var i = 0; i < mapImages.Length; i++)
            {
                maps[i] = mapImages[i].ImageToFloatSurfaceMap(width, height);
                mapImages[i].Dispose();
            }
            return maps;
        }

        internal double GetSeasonalLatitudeFromDeclination(double latitude, double solarDeclination)
        {
            var seasonalLatitude = latitude + (solarDeclination * 2 / 3);
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

        internal float[][,] GetSnowfallMaps(int width, int height)
        {
            var mapImages = GetSnowfallMaps();
            var maps = new float[mapImages.Length][,];
            for (var i = 0; i < mapImages.Length; i++)
            {
                maps[i] = mapImages[i].ImageToFloatSurfaceMap(width, height);
                mapImages[i].Dispose();
            }
            return maps;
        }

        internal double GetSolarDeclination(double trueAnomaly)
            => Orbit.HasValue ? Math.Asin(Math.Sin(AxialTilt) * Math.Sin(Orbit.Value.GetEclipticLongitudeAtTrueAnomaly(trueAnomaly))) : 0;

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
        internal async Task<double> GetSurfaceTemperatureAtTrueAnomalyAsync(double trueAnomaly, double seasonalLatitude)
        {
            var greenhouseEffect = await GetGreenhouseEffectAsync().ConfigureAwait(false);
            var temp = await GetTemperatureAtTrueAnomalyAsync(trueAnomaly).ConfigureAwait(false);
            return (temp * GetInsolationFactor(seasonalLatitude)) + greenhouseEffect;
        }

        internal FloatRange[,] GetTemperatureMap(int width, int height)
        {
            using var winter = GetTemperatureMapWinter();
            using var summer = GetTemperatureMapSummer();
            if (winter is null || summer is null)
            {
                return new FloatRange[width, height];
            }
            return winter.ImagesToFloatRangeSurfaceMap(summer, width, height);
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
        internal MathAndScience.Numerics.Doubles.Vector3 LatitudeAndLongitudeToDoubleVector(double latitude, double longitude)
        {
            var cosLat = Math.Cos(latitude);
            var rot = _axisRotation ?? System.Numerics.Quaternion.Identity;
            return MathAndScience.Numerics.Doubles.Vector3.Normalize(
                MathAndScience.Numerics.Doubles.Vector3.Transform(
                    new MathAndScience.Numerics.Doubles.Vector3(
                        cosLat * Math.Sin(longitude),
                        Math.Sin(latitude),
                        cosLat * Math.Cos(longitude)),
                    MathAndScience.Numerics.Doubles.Quaternion.Inverse(rot)));
        }

        internal override async Task ResetCachedTemperaturesAsync()
        {
            await base.ResetCachedTemperaturesAsync().ConfigureAwait(false);
            _averagePolarSurfaceTemperature = null;
            _averageSurfaceTemperature = null;
            _greenhouseEffect = null;
            _insolationFactor_Equatorial = null;
            _insolationFactor_Polar = null;
            _maxSurfaceTemperature = null;
            _minSurfaceTemperature = null;
            _surfaceTemperature = null;
            if (_atmosphere != null)
            {
                await _atmosphere.ResetTemperatureDependentPropertiesAsync(this).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Converts a <see cref="Vector3"/> to a latitude, in radians.
        /// </summary>
        /// <param name="v">A vector representing a position on the surface of this <see cref="Planetoid"/>.</param>
        /// <returns>A latitude, as an angle in radians from the equator.</returns>
        /// <remarks>
        /// If the planet's axis has never been set, it is treated as vertical for the purpose of
        /// this calculation, but is not permanently set to such an axis.
        /// </remarks>
        internal float VectorToFloatLatitude(System.Numerics.Vector3 v)
        {
            var axis = _axis ?? System.Numerics.Vector3.UnitY;
            return (float)(MathAndScience.Constants.Doubles.MathConstants.HalfPI - Math.Atan2(
                System.Numerics.Vector3.Cross(axis, v).Length(),
                System.Numerics.Vector3.Dot(axis, v)));
        }

        /// <summary>
        /// Converts a <see cref="Vector3"/> to a longitude, in radians.
        /// </summary>
        /// <param name="v">A vector representing a position on the surface of this <see cref="Planetoid"/>.</param>
        /// <returns>A longitude, as an angle in radians from the X-axis at 0 rotation.</returns>
        /// <remarks>
        /// If the planet's axis has never been set, it is treated as vertical for the purpose of
        /// this calculation, but is not permanently set to such an axis.
        /// </remarks>
        internal float VectorToFloatLongitude(System.Numerics.Vector3 v)
        {
            var u = System.Numerics.Vector3.Transform(v, _axisRotation ?? System.Numerics.Quaternion.Identity);
            return u.X.IsNearlyZero() && u.Z.IsNearlyZero()
                ? 0f
                : (float)Math.Atan2(u.X, u.Z);
        }

        private protected int AddResource(ISubstanceReference substance, decimal proportion, bool isVein, bool isPerturbation = false, int? seed = null)
        {
            var resource = new Resource(substance, proportion, isVein, isPerturbation, seed);
            (_resources ??= new List<Resource>()).Add(resource);
            return resource.Seed;
        }

        private protected void AddResources(IEnumerable<(ISubstanceReference substance, decimal proportion, bool vein)> resources)
        {
            foreach (var (substance, proportion, vein) in resources)
            {
                AddResource(substance, proportion, vein);
            }
        }

        private protected virtual async Task GenerateAngleOfRotationAsync()
        {
            if (!_axialPrecession.HasValue || !_angleOfRotation.HasValue)
            {
                _axialPrecession = Randomizer.Instance.NextDouble(MathAndScience.Constants.Doubles.MathConstants.TwoPI);
                if (Randomizer.Instance.NextDouble() <= 0.2) // low chance of an extreme tilt
                {
                    _angleOfRotation = Randomizer.Instance.NextDouble(MathAndScience.Constants.Doubles.MathConstants.QuarterPI, Math.PI);
                }
                else
                {
                    _angleOfRotation = Randomizer.Instance.NextDouble(MathAndScience.Constants.Doubles.MathConstants.QuarterPI);
                }
            }
            await SetAxisAsync().ConfigureAwait(false);
        }

        private protected virtual Task GenerateAtmosphereAsync() => Task.CompletedTask;

        private protected virtual void GenerateResources()
        {
            AddResources(Material.GetSurface()
                  .Constituents.Where(x => x.substance.IsGemstone() || x.substance.Substance.IsMetalOre())
                  .Select(x => (x.substance, x.proportion, true))
                  ?? Enumerable.Empty<(ISubstanceReference, decimal, bool)>());
            AddResources(Material.GetSurface()
                  .Constituents.Where(x => x.substance.Substance.IsHydrocarbon())
                  .Select(x => (x.substance, x.proportion, false))
                  ?? Enumerable.Empty<(ISubstanceReference, decimal, bool)>());

            // Also add halite (rock salt) as a resource, despite not being an ore or gem.
            AddResources(Material.GetSurface()
                  .Constituents.Where(x => x.substance.Equals(Substances.GetChemicalReference(Substances.Chemicals.SodiumChloride)))
                  .Select(x => (x.substance, x.proportion, false))
                  ?? Enumerable.Empty<(ISubstanceReference, decimal, bool)>());

            // A magnetosphere is presumed to indicate tectonic, and hence volcanic, activity.
            // This, in turn, indicates elemental sulfur at the surface.
            if (HasMagnetosphere)
            {
                var sulfurProportion = (decimal)Randomizer.Instance.NormalDistributionSample(3.5e-5, 1.75e-6);
                if (sulfurProportion > 0)
                {
                    AddResource(Substances.GetChemicalReference(Substances.Chemicals.Sulfur), sulfurProportion, false);
                }
            }
        }

        private protected virtual Task<Planetoid?> GenerateSatelliteAsync(Number periapsis, double eccentricity, Number maxMass) => Task.FromResult((Planetoid?)null);

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

        private async Task<double> GetAverageTemperatureAsync(bool polar = false)
        {
            var avgBlackbodyTemp = await GetAverageBlackbodyTemperatureAsync().ConfigureAwait(false);
            var greenhouseEffect = await GetGreenhouseEffectAsync().ConfigureAwait(false);
            return (avgBlackbodyTemp * (polar ? InsolationFactor_Polar : InsolationFactor_Equatorial)) + greenhouseEffect;
        }

        private async Task<double> GetCurrentSurfaceTemperatureAsync()
        {
            var blackbodyTemperature = await GetBlackbodyTemperatureAsync().ConfigureAwait(false);
            var greenhouseEffect = await GetGreenhouseEffectAsync().ConfigureAwait(false);
            return (blackbodyTemperature * InsolationFactor_Equatorial) + greenhouseEffect;
        }

        private protected override double GetDensity() => DensityForType;

        private async Task<double> GetDiurnalTemperatureVariationAsync()
        {
            if (!_diurnalTemperatureVariation.HasValue)
            {
                var temp = await GetTemperatureAsync().ConfigureAwait(false) ?? 0;
                var timeFactor = (double)(1 - ((RotationalPeriod - 2500) / 595000)).Clamp(0, 1);
                var blackbodyTemp = await GetAverageBlackbodyTemperatureAsync().ConfigureAwait(false);
                var greenhouseEffect = await GetGreenhouseEffectAsync().ConfigureAwait(false);
                var darkSurfaceTemp = (((blackbodyTemp * InsolationFactor_Equatorial) - temp) * timeFactor)
                    + temp
                    + greenhouseEffect;
                _diurnalTemperatureVariation = await GetAverageSurfaceTemperatureAsync().ConfigureAwait(false) - darkSurfaceTemp;
            }
            return _diurnalTemperatureVariation.Value;
        }

        private double GetEccentricity()
            => _orbit.HasValue
            ? _orbit.Value.Eccentricity
            : Math.Abs(Randomizer.Instance.NormalDistributionSample(0, 0.05));

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

        private protected async Task<double> GetGreenhouseEffectAsync(double insolationFactor, double greenhouseFactor)
        {
            var avgBlackbodyTemp = await GetAverageBlackbodyTemperatureAsync().ConfigureAwait(false);
            return Math.Max(0, (avgBlackbodyTemp * insolationFactor * greenhouseFactor) - avgBlackbodyTemp);
        }

        /// <summary>
        /// Determines whether this <see cref="Planetoid"/> has a strong magnetosphere.
        /// </summary>
        /// <remarks>
        /// The presence of a magnetosphere is dependent on the rotational period, and on size: fast
        /// spin of a large body (with a hot interior) produces a dynamo effect. Slow spin of a small
        /// (cooled) body does not. Mass is divided by 3e24, and rotational period by the number of
        /// seconds in an Earth year, which simplifies to multiplying mass by 2.88e-19.
        /// </remarks>
        private protected virtual bool GetHasMagnetosphere()
            => Randomizer.Instance.NextNumber() <= Mass * new Number(2.88, -19) / RotationalPeriod * MagnetosphereChanceFactor;

        private double GetInsolationFactor(bool polar = false)
            => GetInsolationFactor(Atmosphere.Material.Mass, Atmosphere.AtmosphericScaleHeight, polar);

        private double GetInsolationFactor(double latitude)
            => InsolationFactor_Polar + ((InsolationFactor_Equatorial - InsolationFactor_Polar)
            * Math.Cos(Math.Max(0, (Math.Abs(latitude) * (MathAndScience.Constants.Doubles.MathConstants.HalfPI + AxialTilt) / MathAndScience.Constants.Doubles.MathConstants.HalfPI) - AxialTilt)));

        private protected virtual double GetInternalTemperature() => 0;

        private async Task<bool> GetIsTidallyLockedAfterAsync(Number years)
        {
            var orbited = Orbit.HasValue
                ? await Orbit.Value.GetOrbitedObjectAsync().ConfigureAwait(false)
                : null;
            return orbited is null
                ? false
                : Number.Pow(years / new Number(6, 11)
                * Mass
                * orbited.Mass.Square()
                / (Shape.ContainingRadius * Rigidity)
                , Number.One / new Number(6)) >= Orbit!.Value.SemiMajorAxis;
        }

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

            var numerator = (MathAndScience.Constants.Doubles.ScienceConstants.RSpecificDryAir * surfaceTemp2)
                + (MathAndScience.Constants.Doubles.ScienceConstants.DeltaHvapWater * Atmosphere.WaterRatioDouble * surfaceTemp);
            var denominator = (MathAndScience.Constants.Doubles.ScienceConstants.CpTimesRSpecificDryAir * surfaceTemp2)
                + (MathAndScience.Constants.Doubles.ScienceConstants.DeltaHvapWaterSquared * Atmosphere.WaterRatioDouble * MathAndScience.Constants.Doubles.ScienceConstants.RSpecificRatioOfDryAirToWater);

            return (double)SurfaceGravity * (numerator / denominator);
        }

        private protected override ValueTask<Number> GetMassAsync()
        {
            var minMass = (double)MinMass;
            var maxMass = (double)MaxMass;
            return new ValueTask<Number>(Randomizer.Instance.PositiveNormalDistributionSample(minMass, (maxMass - minMass) / 3, maximum: maxMass));
        }

        private protected override async ValueTask<(double density, Number mass, IShape shape)> GetMatterAsync()
            => (GetDensity(), await GetMassAsync().ConfigureAwait(false), await GetShapeAsync().ConfigureAwait(false));

        private double GetMaxElevation()
        {
            if (HasFlatSurface)
            {
                return 0;
            }

            return 200000 / (double)SurfaceGravity;
        }

        private protected async Task<double> GetMaxPolarTemperatureAsync()
        {
            var greenhouseEffect = await GetGreenhouseEffectAsync().ConfigureAwait(false);
            var temp = await GetTemperatureAtPeriapsisAsync().ConfigureAwait(false);
            return (temp * InsolationFactor_Polar) + greenhouseEffect;
        }

        private protected async Task<double> GetMinEquatorTemperatureAsync()
        {
            var variation = await GetDiurnalTemperatureVariationAsync().ConfigureAwait(false);
            var greenhouseEffect = await GetGreenhouseEffectAsync().ConfigureAwait(false);
            var temp = await GetTemperatureAtApoapsisAsync().ConfigureAwait(false);
            return (temp * InsolationFactor_Equatorial) + greenhouseEffect - variation;
        }

        private protected virtual Number GetMinSatellitePeriapsis() => Number.Zero;

        private double GetNormalizedElevationAt(double x, double y, double z)
        {
            if (MaxElevation.IsNearlyZero())
            {
                return 0;
            }

            var e = GetElevationNoiseAt(x, y, z);

            // Get the value offset from sea level.
            var n = e - _normalizedSeaLevel;

            // Skew ocean locations deeper, to avoid extended shallow shores.
            if (n < 0 && n > -0.37)
            {
                // The dropoff is initially rapid.
                n *= 9 - (Math.Abs(0.0125 + n) / (n >= -0.0125 ? 0.0015625 : 0.0359375));

                // Values between the ocean shelf and sea floor are skewed even more towards the
                // floor. Oceans are typically shallow near the shore, then become deep rapidly, and
                // remain at about the same depth throughout, with occasional trenches.
                if (n < -0.025)
                {
                    n *= 1 + ((0.37 + n) / 0.37);
                }
            }

            return n;
        }

        private double GetPolarAirMass(double atmosphericScaleHeight)
        {
            var r = (double)Shape.ContainingRadius / atmosphericScaleHeight;
            var rCosLat = r * CosPolarLatitude;
            return Math.Sqrt((rCosLat * rCosLat) + (2 * r) + 1) - rCosLat;
        }

        private (double rightAscension, double declination) GetRightAscensionAndDeclination(Vector3 position, Vector3 otherPosition)
        {
            var rot = _axisRotation ?? System.Numerics.Quaternion.Identity;
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

        private protected double GetSeasonalLatitude(double latitude, double trueAnomaly)
            => GetSeasonalLatitudeFromDeclination(latitude, GetSolarDeclination(trueAnomaly));

        private protected override ValueTask<IShape> GetShapeAsync()
            // Gaussian distribution with most values between 1km and 19km.
            => new ValueTask<IShape>(new Ellipsoid(Randomizer.Instance.NormalDistributionSample(10000, 4500, minimum: 0), Randomizer.Instance.NextNumber(Number.Half, 1), Position));

        private double GetSlope(Vector3 position, double latitude, double longitude, double elevation)
        {
            // north
            var otherCoords = (lat: latitude + Second, lon: longitude);
            if (otherCoords.lat > Math.PI)
            {
                otherCoords = (MathAndScience.Constants.Doubles.MathConstants.TwoPI - otherCoords.lat, (otherCoords.lon + Math.PI) % MathAndScience.Constants.Doubles.MathConstants.TwoPI);
            }
            var otherPos = LatitudeAndLongitudeToVector(otherCoords.lat, otherCoords.lon);
            var otherElevation = GetNormalizedElevationAt(otherPos);
            var slope = Math.Abs(elevation - otherElevation) * MaxElevation / GetDistance(position, otherPos);

            // east
            otherCoords = (lat: latitude, lon: (longitude + Second) % MathAndScience.Constants.Doubles.MathConstants.TwoPI);
            otherPos = LatitudeAndLongitudeToVector(otherCoords.lat, otherCoords.lon);
            otherElevation = GetNormalizedElevationAt(otherPos);
            slope = Math.Max(slope, Math.Abs(elevation - otherElevation) * MaxElevation / GetDistance(position, otherPos));

            // south
            otherCoords = (lat: latitude - Second, lon: longitude);
            if (otherCoords.lat < -Math.PI)
            {
                otherCoords = (-MathAndScience.Constants.Doubles.MathConstants.TwoPI - otherCoords.lat, (otherCoords.lon + Math.PI) % MathAndScience.Constants.Doubles.MathConstants.TwoPI);
            }
            otherPos = LatitudeAndLongitudeToVector(otherCoords.lat, otherCoords.lon);
            otherElevation = GetNormalizedElevationAt(otherPos);
            slope = Math.Max(slope, Math.Abs(elevation - otherElevation) * MaxElevation / GetDistance(position, otherPos));

            // west
            otherCoords = (lat: latitude, lon: (longitude - Second) % MathAndScience.Constants.Doubles.MathConstants.TwoPI);
            otherPos = LatitudeAndLongitudeToVector(otherCoords.lat, otherCoords.lon);
            otherElevation = GetNormalizedElevationAt(otherPos);
            return Math.Max(slope, Math.Abs(elevation - otherElevation) * MaxElevation / GetDistance(position, otherPos));
        }

        private protected override ISubstanceReference? GetSubstance() => CelestialSubstances.ChondriticRock;

        private protected override double? GetTemperature() => GetInternalTemperature();

        private void Init()
        {
            _seed1 = Randomizer.Instance.NextInclusive();
            _seed2 = Randomizer.Instance.NextInclusive();
            _seed3 = Randomizer.Instance.NextInclusive();
            _seed4 = Randomizer.Instance.NextInclusive();
            _seed5 = Randomizer.Instance.NextInclusive();
        }

        private protected override async Task InitializeAsync()
        {
            await base.InitializeAsync().ConfigureAwait(false);
            await GenerateAngleOfRotationAsync().ConfigureAwait(false);
            await SetRotationalPeriodAsync().ConfigureAwait(false);
            await GenerateAtmosphereAsync().ConfigureAwait(false);
            if (_atmosphere is null)
            {
                _atmosphere = await Atmosphere.GetNewInstanceAsync(this, 0).ConfigureAwait(false);
            }

            // Small chance of satellites for rogue planets.
            if (Randomizer.Instance.NextDouble() <= 0.2)
            {
                await GenerateSatellitesAsync().ConfigureAwait(false);
            }
            else if (Orbit.HasValue)
            {
                var orbited = await Orbit.Value.GetOrbitedObjectAsync().ConfigureAwait(false);
                if (orbited is Star)
                {
                    await GenerateSatellitesAsync().ConfigureAwait(false);
                }
            }
        }

        private async Task ResetPressureDependentPropertiesAsync()
        {
            _averagePolarSurfaceTemperature = null;
            _averageSurfaceTemperature = null;
            _greenhouseEffect = null;
            _insolationFactor_Equatorial = null;
            _insolationFactor_Polar = null;
            if (_atmosphere != null)
            {
                await _atmosphere.ResetPressureDependentPropertiesAsync(this).ConfigureAwait(false);
            }
        }

        private protected async Task SetAngleOfRotationAsync(double angle)
        {
            while (angle > Math.PI)
            {
                angle -= Math.PI;
            }
            while (angle < 0)
            {
                angle += Math.PI;
            }
            _angleOfRotation = angle;
            await SetAxisAsync().ConfigureAwait(false);
        }

        private async Task SetAxisAsync()
        {
            var precessionQ = System.Numerics.Quaternion.CreateFromYawPitchRoll((float)AxialPrecession, 0, 0);
            var precessionVector = System.Numerics.Vector3.Transform(System.Numerics.Vector3.UnitX, precessionQ);
            var q = System.Numerics.Quaternion.CreateFromAxisAngle(precessionVector, (float)AngleOfRotation);
            _axis = System.Numerics.Vector3.Transform(System.Numerics.Vector3.UnitY, q);
            _axisRotation = System.Numerics.Quaternion.Conjugate(q);

            await ResetCachedTemperaturesAsync().ConfigureAwait(false);
        }

        private protected async Task SetRotationalPeriodAsync()
        {
            if (_rotationalPeriod.HasValue)
            {
                return;
            }

            // Check for tidal locking.
            if (Orbit.HasValue)
            {
                // Invent an orbit age. Precision isn't important here, and some inaccuracy and
                // inconsistency between satellites is desirable. The age of the Solar system is used
                // as an arbitrary norm.
                var years = Randomizer.Instance.LogisticDistributionSample(0, 1) * new Number(4.6, 9);
                if (await GetIsTidallyLockedAfterAsync(years).ConfigureAwait(false))
                {
                    _rotationalPeriod = Orbit.Value.Period;
                    return;
                }
            }

            if (Randomizer.Instance.NextDouble() <= 0.05) // low chance of an extreme period
            {
                _rotationalPeriod = Randomizer.Instance.NextNumber(MaxRotationalPeriod, ExtremeRotationalPeriod);
            }
            else
            {
                _rotationalPeriod = Randomizer.Instance.NextNumber(MinRotationalPeriod, MaxRotationalPeriod);
            }
        }
    }
}
