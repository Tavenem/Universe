using MathAndScience;
using MathAndScience.Numerics;
using MathAndScience.Shapes;
using Substances;
using System;
using System.Collections.Generic;
using System.Linq;
using WorldFoundry.Climate;
using WorldFoundry.Space;
using WorldFoundry.Substances;
using WorldFoundry.WorldGrids;

namespace WorldFoundry.CelestialBodies.Planetoids
{
    /// <summary>
    /// Any non-stellar celestial body, such as a planet or asteroid.
    /// </summary>
    public class Planetoid : CelestialBody
    {
        // polar latitude = 1.5277247828211
        private const double CosPolarLatitude = 0.04305822778985774;

        /// <summary>
        /// The minimum radius required to achieve hydrostatic equilibrium, in meters.
        /// </summary>
        private protected const int MinimumRadius = 600000;

        private WorldGrid _grid;
        private float _normalizedSeaLevel;

        private double? _angleOfRotation;
        /// <summary>
        /// The angle between the Y-axis and the axis of rotation of this <see cref="Planetoid"/>.
        /// Values greater than half Pi indicate clockwise rotation.
        /// </summary>
        /// <remarks>
        /// Note that this is not the same as <see cref="AxialTilt"/>: if the <see
        /// cref="Planetoid"/> is in orbit then <see cref="AxialTilt"/> is relative to the orbital
        /// plane of the <see cref="Planetoid"/>.
        /// </remarks>
        public double AngleOfRotation
        {
            get
            {
                if (!_angleOfRotation.HasValue)
                {
                    GenerateAngleOfRotation();
                }
                return _angleOfRotation ?? 0;
            }
            internal set
            {
                var angle = value;
                while (angle > Math.PI)
                {
                    angle -= Math.PI;
                }
                while (angle < 0)
                {
                    angle += Math.PI;
                }
                _angleOfRotation = angle;
                SetAxis();
            }
        }

        private double? _angularVelocity;
        /// <summary>
        /// The angular velocity of this <see cref="Planetoid"/>, in m/s.
        /// </summary>
        public double AngularVelocity
        {
            get => _angularVelocity ?? (_angularVelocity = GetAngularVelocity()).Value;
            private set => _angularVelocity = value;
        }

        private protected Atmosphere _atmosphere;
        /// <summary>
        /// The atmosphere possessed by this <see cref="Planetoid"/> (may be <see
        /// langword="null"/>).
        /// </summary>
        public Atmosphere Atmosphere
        {
            get
            {
                if (_atmosphere == null)
                {
                    GenerateAtmosphere();
                }
                return _atmosphere;
            }
            protected set => _atmosphere = value;
        }

        private double? _averagePolarSurfaceTemperature;
        /// <summary>
        /// The average surface temperature of this <see cref="Planetoid"/> near its poles
        /// throughout its orbit (or at its current position, if it is not in orbit), in K.
        /// </summary>
        public double AveragePolarSurfaceTemperature
            => _averagePolarSurfaceTemperature ?? (_averagePolarSurfaceTemperature = GetAverageSurfaceTemperature(true)).Value;

        private double? _averageSurfaceTemperature;
        /// <summary>
        /// The average surface temperature of the <see cref="Planetoid"/> at its equator throughout
        /// its orbit (or at its current position, if it is not in orbit), in K.
        /// </summary>
        public override double AverageSurfaceTemperature
            => _averageSurfaceTemperature ?? (_averageSurfaceTemperature = GetAverageSurfaceTemperature()).Value;

        private protected double? _axialPrecession;
        /// <summary>
        /// The angle between the X-axis and the orbital vector at which the first equinox occurs.
        /// </summary>
        public double AxialPrecession
        {
            get
            {
                if (!_axialPrecession.HasValue)
                {
                    GenerateAngleOfRotation();
                }
                return _axialPrecession ?? 0;
            }
            internal set
            {
                var angle = value;
                while (angle > Math.PI)
                {
                    angle -= Math.PI;
                }
                while (angle < 0)
                {
                    angle += Math.PI;
                }
                _axialPrecession = angle;
                SetAxis();
            }
        }

        /// <summary>
        /// The axial tilt of the <see cref="Planetoid"/> relative to its orbital plane, in radians.
        /// Values greater than half Pi indicate clockwise rotation.
        /// </summary>
        /// <remarks>
        /// If the <see cref="Planetoid"/> isn't orbiting anything, this is the same as <see
        /// cref="AngleOfRotation"/>.
        /// </remarks>
        public double AxialTilt
        {
            get => Orbit == null ? AngleOfRotation : AngleOfRotation - Orbit.Inclination;
            set => AngleOfRotation = Orbit == null ? value : value + Orbit.Inclination;
        }

        private Vector3? _axis;
        /// <summary>
        /// A <see cref="Vector3"/> which represents the axis of this <see cref="Planetoid"/>.
        /// Read-only. To set, specify <see cref="AxialPrecession"/> and/or <see cref="AngleOfRotation"/>.
        /// </summary>
        public Vector3 Axis
        {
            get
            {
                if (!_axis.HasValue)
                {
                    SetAxis();
                }
                return _axis ?? Vector3.Zero;
            }
        }

        private double? _density;
        /// <summary>
        /// The average density of this <see cref="Planetoid"/>, in kg/m³.
        /// </summary>
        public double Density
        {
            get => _density ?? (_density = GetDensity()) ?? DensityForType;
            set => _density = value;
        }

        private double? _diurnalTemperatureVariation;
        /// <summary>
        /// The diurnal temperature variation for this body, in K.
        /// </summary>
        public double DiurnalTemperatureVariation
            => _diurnalTemperatureVariation ?? (_diurnalTemperatureVariation = GetDiurnalTemperatureVariation()).Value;

        private bool? _hasMagnetosphere;
        /// <summary>
        /// Indicates whether this <see cref="Planetoid"/> has a strong magnetosphere.
        /// </summary>
        public bool HasMagnetosphere
        {
            get => _hasMagnetosphere ?? (_hasMagnetosphere = GetHasMagnetosphere()).Value;
            protected set => _hasMagnetosphere = value;
        }

        private double? _greenhouseEffect;
        /// <summary>
        /// The total greenhouse effect on this <see cref="Planetoid"/>, in K.
        /// </summary>
        public double GreenhouseEffect
        {
            get => _greenhouseEffect ?? (_greenhouseEffect = GetGreenhouseEffect()).Value;
            internal set => _greenhouseEffect = value;
        }

        private double? _maxElevation;
        /// <summary>
        /// <para>
        /// The maximum elevation of this planet's surface topology, relative to its average
        /// surface, based on the strength of its gravity.
        /// </para>
        /// <para>
        /// Note that local elevations are given relative to sea level, rather than to the average
        /// surface, which means local elevations may exceed this value on planets with low sea
        /// levels, and that planets with high sea levels may have no points with elevations even
        /// close to this value.
        /// </para>
        /// </summary>
        public double MaxElevation
        {
            get => _maxElevation ?? (_maxElevation = GetMaxElevation()).Value;
            set
            {
                _maxElevation = value;
                _seaLevel = _normalizedSeaLevel * value;
            }
        }

        private double? _maxSurfaceTemperature;
        /// <summary>
        /// The approximate maximum surface temperature of this <see cref="Planetoid"/>, in K.
        /// </summary>
        /// <remarks>
        /// Gets the equatorial temperature at periapsis, or at the current position if not in orbit.
        /// </remarks>
        public double MaxSurfaceTemperature
            => _maxSurfaceTemperature ?? (_maxSurfaceTemperature = GetMaxSurfaceTemperature()).Value;

        private double? _minSurfaceTemperature;
        /// <summary>
        /// The approximate minimum surface temperature of this <see cref="Planetoid"/>, in K.
        /// </summary>
        /// <remarks>
        /// Gets the polar temperature at apoapsis, or at the current position if not in orbit.
        /// </remarks>
        public double MinSurfaceTemperature
            => _minSurfaceTemperature ?? (_minSurfaceTemperature = GetMinSurfaceTemperature()).Value;

        private double? _rotationalPeriod;
        /// <summary>
        /// The length of time it takes for this <see cref="Planetoid"/> to rotate once about its axis, in seconds.
        /// </summary>
        public double RotationalPeriod
        {
            get => _rotationalPeriod ?? (_rotationalPeriod = GetRotationalPeriod()).Value;
            protected set
            {
                _rotationalPeriod = value;
                _insolationAreaRatio = null;
                ResetCachedTemperatures();
            }
        }

        private Dictionary<string, Resource> _resources;
        /// <summary>
        /// The resources of this <see cref="Planetoid"/>, as a dictionary of <see cref="Chemical"/>
        /// names along with <see cref="Resource"/> values.
        /// </summary>
        public Dictionary<string, Resource> Resources
        {
            get
            {
                if (_resources == null)
                {
                    _resources = new Dictionary<string, Resource>();
                    GenerateResources();
                }
                return _resources;
            }
        }

        private List<string> _satelliteIDs;
        /// <summary>
        /// The collection of natural satellites around this <see cref="Planetoid"/>.
        /// </summary>
        /// <remarks>
        /// Unlike <see cref="CelestialRegion.Children"/>, natural satellites are actually siblings
        /// in the local <see cref="CelestialRegion"/> hierarchy, which merely share an orbital relationship.
        /// </remarks>
        public IEnumerable<Planetoid> Satellites
            => Parent?.Children.OfType<Planetoid>().Where(x => _satelliteIDs?.Contains(x.ID) == true) ?? Enumerable.Empty<Planetoid>();

        private double? _seaLevel;
        /// <summary>
        /// The elevation of sea level relative to the mean surface elevation of the planet, in
        /// meters.
        /// </summary>
        public double SeaLevel
        {
            get => _seaLevel ?? (_seaLevel = _normalizedSeaLevel * MaxElevation).Value;
            set
            {
                _seaLevel = value;
                _normalizedSeaLevel = (float)(value / MaxElevation);
            }
        }

        private double? _surfaceTemperature;
        /// <summary>
        /// The current surface temperature of the <see cref="Planetoid"/> at its equator, in K.
        /// </summary>
        public double SurfaceTemperature
            => _surfaceTemperature ?? (_surfaceTemperature = GetCurrentSurfaceTemperature()).Value;

        private double? _insolationFactor_Equatorial;
        internal double InsolationFactor_Equatorial
        {
            get => _insolationFactor_Equatorial ?? (_insolationFactor_Equatorial = GetInsolationFactor()).Value;
            set => _insolationFactor_Equatorial = value;
        }

        private double? _summerSolarEquator;
        internal double SummerSolarEquator
            => _summerSolarEquator ?? (_summerSolarEquator = AxialTilt * (2.0 / 3.0)).Value;

        private double? _summerSolsticeTrueAnomaly;
        internal double SummerSolsticeTrueAnomaly
        {
            get
            {
                if (!_summerSolsticeTrueAnomaly.HasValue)
                {
                    _summerSolsticeTrueAnomaly = AxialPrecession + MathConstants.ThreeHalvesPI;
                    if (_summerSolsticeTrueAnomaly >= MathConstants.TwoPI)
                    {
                        _summerSolsticeTrueAnomaly -= MathConstants.TwoPI;
                    }
                }
                return _summerSolsticeTrueAnomaly ?? 0;
            }
        }

        private double? _winterSolarEquator;
        internal double WinterSolarEquator
            => _winterSolarEquator ?? (_winterSolarEquator = -AxialTilt * (2.0 / 3.0)).Value;

        private double? _winterSolsticeTrueAnomaly;
        internal double WinterSolsticeTrueAnomaly
        {
            get
            {
                if (!_winterSolsticeTrueAnomaly.HasValue)
                {
                    _winterSolsticeTrueAnomaly = AxialPrecession + MathConstants.HalfPI;
                    if (_winterSolsticeTrueAnomaly >= MathConstants.TwoPI)
                    {
                        _winterSolsticeTrueAnomaly -= MathConstants.TwoPI;
                    }
                }
                return _winterSolsticeTrueAnomaly ?? 0;
            }
        }

        private Quaternion? _axisRotation;
        /// <summary>
        /// A <see cref="Quaternion"/> representing the rotation of the <see cref="Axis"/> of this
        /// <see cref="Planetoid"/>.
        /// </summary>
        private Quaternion AxisRotation
        {
            get
            {
                if (!_axisRotation.HasValue)
                {
                    SetAxis();
                }
                return _axisRotation ?? Quaternion.Identity;
            }
        }

        private protected virtual double DensityForType => 0;

        private double? _eccentricity;
        private protected double Eccentricity
        {
            get => _eccentricity ?? (_eccentricity = GetEccentricity()).Value;
            set => _eccentricity = value;
        }

        private protected virtual int ExtremeRotationalPeriod => 1100000;

        private protected virtual bool HasFlatSurface => false;

        private double? _insolationAreaRatio;
        private protected double InsolationAreaRatio => _insolationAreaRatio ?? (_insolationAreaRatio = GetInsolationAreaRatio()).Value;

        private double? _insolationFactor_Polar;
        private double InsolationFactor_Polar
            => _insolationFactor_Polar ?? (_insolationFactor_Polar = GetInsolationFactor(true)).Value;

        private double? _lapseRateDry;
        private protected double LapseRateDry
            => _lapseRateDry ?? (_lapseRateDry = SurfaceGravity / ScienceConstants.CpDryAir).Value;

        private protected virtual double MagnetosphereChanceFactor => 1;

        private double? _maxMass;
        private protected double MaxMass
        {
            get => (_maxMass ?? MaxMassForType) ?? 0;
            set => _maxMass = value;
        }

        private protected virtual double? MaxMassForType => null;

        private protected virtual int MaxRotationalPeriod => 100000;

        private protected virtual int MaxSatellites => 1;

        private double? _minMass;
        private protected double MinMass
        {
            get => (_minMass ?? MinMassForType) ?? 0;
            set => _minMass = value;
        }

        private protected virtual double? MinMassForType => null;

        private protected virtual int MinRotationalPeriod => 8000;

        private double? _minSatellitePeriapsis;
        private double MinSatellitePeriapsis
            => _minSatellitePeriapsis ?? (_minSatellitePeriapsis = GetMinSatellitePeriapsis()).Value;

        private FastNoise _noise1;
        private FastNoise Noise1 => _noise1 ?? (_noise1 = new FastNoise(_seed1, 0.01, FastNoise.NoiseType.SimplexFractal, octaves: 6));

        private FastNoise _noise2;
        private FastNoise Noise2 => _noise2 ?? (_noise2 = new FastNoise(_seed2, 0.01, FastNoise.NoiseType.SimplexFractal, octaves: 5));

        private FastNoise _noise3;
        private FastNoise Noise3 => _noise3 ?? (_noise3 = new FastNoise(_seed3, 0.01, FastNoise.NoiseType.SimplexFractal, octaves: 4));

        private protected virtual double Rigidity => 3.0e10;

        private double? _solarEquator;
        private double SolarEquator => _solarEquator ?? (_solarEquator = GetSolarEquator()).Value;

        /// <summary>
        /// Initializes a new instance of <see cref="Planetoid"/>.
        /// </summary>
        internal Planetoid() { }

        /// <summary>
        /// Initializes a new instance of <see cref="Planetoid"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="Planetoid"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="Planetoid"/>.</param>
        internal Planetoid(CelestialRegion parent, Vector3 position) : base(parent, position) { }

        /// <summary>
        /// Initializes a new instance of <see cref="Planetoid"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="Planetoid"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="Planetoid"/>.</param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="Planetoid"/> during random generation, in kg.
        /// </param>
        internal Planetoid(CelestialRegion parent, Vector3 position, double maxMass) : base(parent, position) => MaxMass = maxMass;

        /// <summary>
        /// Calculates the atmospheric drag on a spherical object within the <see
        /// cref="Atmosphere"/> of this <see cref="Planetoid"/> under given conditions, in N.
        /// </summary>
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
        public double GetAtmosphericDrag(double latitude, double longitude, double altitude, double speed) =>
            Atmosphere?.GetAtmosphericDrag(this, GetTemperatureAtElevation(GetSurfaceTemperatureAt(latitude, longitude), altitude), altitude, speed) ?? 0;

        /// <summary>
        /// Calculates the atmospheric pressure at a given <paramref name="latitude"/> and <paramref
        /// name="longitude"/>, in kPa.
        /// </summary>
        /// <param name="latitude">The latitude at which to determine atmospheric pressure.</param>
        /// <param name="longitude">The longitude at which to determine atmospheric pressure.</param>
        /// <returns>The atmospheric pressure at the specified height, in kPa.</returns>
        /// <remarks>
        /// In an Earth-like atmosphere, the pressure lapse rate varies considerably in the different
        /// atmospheric layers, but this cannot be easily modeled for arbitrary exoplanetary
        /// atmospheres, so the simple barometric formula is used, which should be "close enough" for
        /// the purposes of this library. Also, this calculation uses the molar mass of air on Earth,
        /// which is clearly not correct for other atmospheres, but is considered "close enough" for
        /// the purposes of this library.
        /// </remarks>
        public double GetAtmosphericPressure(double latitude, double longitude)
        {
            var elevation = GetElevationAt(latitude, longitude);
            var temp = GetTemperatureAtElevation(
                  (BlackbodySurfaceTemperature * GetInsolationFactor(GetSeasonalLatitude(latitude))) + GreenhouseEffect,
                  elevation);
            return GetAtmosphericPressureFromTempAndElevation(
                GetTemperatureAtElevation(
                    (BlackbodySurfaceTemperature * GetInsolationFactor(GetSeasonalLatitude(latitude))) + GreenhouseEffect,
                    elevation),
                elevation);
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
            => Radius * Math.Atan2(position1.Dot(position2), position1.Cross(position2).Length());

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
        /// Gets a <see cref="WorldGrid"/> depicting the topology of this planet, with the default
        /// level of detail.
        /// </summary>
        /// <returns>A <see cref="WorldGrid"/> instance depicting the topology of this
        /// planet.</returns>
        public virtual WorldGrid GetGrid() => GetGrid(WorldGrid.DefaultGridSize);

        /// <summary>
        /// Gets a <see cref="WorldGrid"/> depicting the topology of this planet, which has at least
        /// the given level of detail.
        /// </summary>
        /// <param name="detailLevel">The minimum level of detail of the grid. Because grid
        /// generating is an expensive and potentially slow process, especially at high detail, the
        /// latest requested grid for each planet is cached. If the cached grid has a higher level
        /// then the specified level, the cached grid will be returned instead of generating a new,
        /// lower-detail grid.</param>
        /// <returns>A <see cref="WorldGrid"/> instance depicting the topology of this
        /// planet.</returns>
        public WorldGrid GetGrid(byte detailLevel)
        {
            if (_grid != null && _grid.GridSize >= detailLevel)
            {
                return _grid;
            }

            _grid = new WorldGrid(this, detailLevel);

            return _grid;
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
        public float GetNormalizedElevationAt(Vector3 position)
        {
            if (MaxElevation.IsZero())
            {
                return 0;
            }

            // The magnitude of the position vector is magnified to increase the surface area of the
            // noise map, thus providing a more diverse range of results without introducing
            // excessive noise (as increasing frequency would).
            var p = position * 100;

            // Initial noise map.
            var baseNoise = Noise1.GetNoise(p.X, p.Y, p.Z);

            // In order to avoid an appearance of excessive uniformity, with all mountains reaching
            // the same height, distributed uniformly over the surface, the initial noise is
            // multiplied by a second, independent noise map. The resulting map will have more
            // randomly distributed high and low points.
            var irregularity1 = Math.Abs(Noise2.GetNoise(p.X, p.Y, p.Z));

            // This process is repeated.
            var irregularity2 = Math.Abs(Noise3.GetNoise(p.X, p.Y, p.Z));

            var e = baseNoise * irregularity1 * irregularity2;

            // The overall value is magnified to compensate for excessive normalization.
            e *= 6;

            return (float)e - _normalizedSeaLevel;
        }

        /// <summary>
        /// Gets the richness of the resources at the given <paramref name="latitude"/> and
        /// <paramref name="longitude"/>.
        /// </summary>
        /// <param name="latitude">The latitude at which to determine resource richness.</param>
        /// <param name="longitude">The longitude at which to determine resource richness.</param>
        /// <returns>The richness of the resources at the given <paramref name="latitude"/> and
        /// <paramref name="longitude"/>, as a collection of values between 0 and 1 for each <see
        /// cref="Chemical"/> present.</returns>
        public IEnumerable<(Chemical chemical, double richness)> GetResourceRichnessAt(double latitude, double longitude)
        {
            var position = LatitudeAndLongitudeToVector(latitude, longitude);
            return Resources.Select(x => (x.Value.Chemical, x.Value.GetResourceRichnessAt(position)));
        }

        /// <summary>
        /// Gets the richness of the given resource at the given <paramref name="latitude"/> and
        /// <paramref name="longitude"/>.
        /// </summary>
        /// <param name="chemical">The chemical resource for which richness will be
        /// determined.</param>
        /// <param name="latitude">The latitude at which to determine resource richness.</param>
        /// <param name="longitude">The longitude at which to determine resource richness.</param>
        /// <returns>The richness of the resources at the given <paramref name="latitude"/> and
        /// <paramref name="longitude"/>, as a collection of values between 0 and 1 for each <see
        /// cref="Chemical"/> present.</returns>
        public double GetResourceRichnessAt(Chemical chemical, double latitude, double longitude)
            => Resources.TryGetValue(chemical.Name, out var resource)
                ? resource.GetResourceRichnessAt(LatitudeAndLongitudeToVector(latitude, longitude))
                : 0;

        /// <summary>
        /// Calculates the slope at the given coordinates, as the ratio of rise over run from the
        /// point to the point 1 arc-second away in the cardinal direction which is at the steepest
        /// angle.
        /// </summary>
        /// <param name="latitude">The latitude of the point.</param>
        /// <param name="latitude">The longitude of the point.</param>
        /// <returns>The slope at the given coordinates.</returns>
        public double GetSlope(double latitude, double longitude)
        {
            var position = LatitudeAndLongitudeToVector(latitude, longitude);
            return GetSlope(position, latitude, longitude, GetNormalizedElevationAt(position));
        }

        /// <summary>
        /// Calculates the effective surface temperature at the given surface position, including
        /// greenhouse effects, in K.
        /// </summary>
        /// <param name="latitude">
        /// The latitude at which temperature will be calculated.
        /// </param>
        /// <param name="longitude">
        /// The longitude at which temperature will be calculated.
        /// </param>
        /// <returns>The surface temperature, in K.</returns>
        public double GetSurfaceTemperatureAt(double latitude, double longitude)
            => GetTemperatureAtElevation(
                  (BlackbodySurfaceTemperature * GetInsolationFactor(GetSeasonalLatitude(latitude))) + GreenhouseEffect,
                  GetElevationAt(latitude, longitude));

        /// <summary>
        /// Calculates the effective surface temperature at the given surface position, including
        /// greenhouse effects, in K.
        /// </summary>
        /// <param name="position">
        /// The surface position at which temperature will be calculated.
        /// </param>
        /// <returns>The surface temperature, in K.</returns>
        public double GetSurfaceTemperatureAtSurfacePosition(Vector3 position)
            => GetSurfaceTemperatureAt(VectorToLatitude(position), VectorToLongitude(position));

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
            => new FloatRange(
                (float)GetTemperatureAtElevation(
                    GetSurfaceTemperatureAtTrueAnomaly(WinterSolsticeTrueAnomaly, GetSeasonalLatitudeFromEquator(latitude, WinterSolarEquator)),
                    elevation),
                (float)GetTemperatureAtElevation(
                    GetSurfaceTemperatureAtTrueAnomaly(SummerSolsticeTrueAnomaly, GetSeasonalLatitudeFromEquator(latitude, SummerSolarEquator)),
                    elevation));

        /// <summary>
        /// Converts latitude and longitude to a <see cref="Vector3"/>.
        /// </summary>
        /// <param name="latitude">A latitude, as an angle in radians from the equator.</param>
        /// <param name="longitude">A longitude, as an angle in radians from the X-axis at 0
        /// rotation.</param>
        /// <returns>A normalized <see cref="Vector3"/> representing a position on the surface of
        /// this <see cref="Planetoid"/>.</returns>
        public Vector3 LatitudeAndLongitudeToVector(double latitude, double longitude)
        {
            var cosLat = Math.Cos(latitude);
            return Vector3.Normalize(Vector3.Transform(
                new Vector3(
                    cosLat * Math.Sin(longitude),
                    Math.Sin(latitude),
                    cosLat * Math.Cos(longitude)),
                Quaternion.Inverse(AxisRotation)));
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
            if (Atmosphere != null)
            {
                Atmosphere.SetAtmosphericPressure(value);
                ResetPressureDependentProperties();
            }
        }

        /// <summary>
        /// Converts a <see cref="Vector3"/> to a latitude, in radians.
        /// </summary>
        /// <param name="v">A vector representing a position on the surface of this <see cref="Planetoid"/>.</param>
        /// <returns>A latitude, as an angle in radians from the equator.</returns>
        public double VectorToLatitude(Vector3 v) => MathConstants.HalfPI - Axis.Angle(v);

        /// <summary>
        /// Converts a <see cref="Vector3"/> to a longitude, in radians.
        /// </summary>
        /// <param name="v">A vector representing a position on the surface of this <see cref="Planetoid"/>.</param>
        /// <returns>A longitude, as an angle in radians from the X-axis at 0 rotation.</returns>
        public double VectorToLongitude(Vector3 v)
        {
            var u = Vector3.Transform(v, AxisRotation);
            return u.X == 0 && u.Z == 0
                ? 0
                : Math.Atan2(u.X, u.Z);
        }

        internal void GenerateSatellites(int? max = null)
        {
            if (MaxSatellites <= 0)
            {
                return;
            }

            var minPeriapsis = MinSatellitePeriapsis;
            var maxApoapsis = Orbit == null ? Radius * 100 : Orbit.GetHillSphereRadius() / 3.0;

            while (minPeriapsis <= maxApoapsis && (_satelliteIDs?.Count ?? 0) < MaxSatellites && (!max.HasValue || (_satelliteIDs?.Count ?? 0) < max.Value))
            {
                var periapsis = Math.Round(Randomizer.Instance.NextDouble(minPeriapsis, maxApoapsis));

                var maxEccentricity = (maxApoapsis - periapsis) / (maxApoapsis + periapsis);
                var eccentricity = Math.Round(Math.Min(Math.Abs(Randomizer.Instance.Normal(0, 0.05)), maxEccentricity), 4);

                var semiLatusRectum = periapsis * (1 + eccentricity);
                var semiMajorAxis = semiLatusRectum / (1 - (eccentricity * eccentricity));

                // Keep mass under the limit where the orbital barycenter would be pulled outside the boundaries of this body.
                var maxMass = Math.Max(0, Mass / ((semiMajorAxis / Radius) - 1));

                var satellite = GenerateSatellite(periapsis, eccentricity, maxMass);
                if (satellite == null)
                {
                    break;
                }

                (_satelliteIDs ?? (_satelliteIDs = new List<string>())).Add(satellite.ID);

                minPeriapsis = satellite.Orbit.Apoapsis + satellite.Orbit.GetSphereOfInfluenceRadius();
            }
        }

        internal double GetInsolationFactor(double atmosphereMass, double atmosphericScaleHeight, bool polar = false)
            => Math.Pow(1320000 * atmosphereMass * (polar ? Math.Pow(0.7, Math.Pow(GetPolarAirMass(atmosphericScaleHeight), 0.678)) : 0.7) / Mass, 0.25);

        internal double GetSeasonalLatitudeFromEquator(double latitude, double solarEquator)
        {
            var seasonalLatitude = latitude + solarEquator;
            if (seasonalLatitude > MathConstants.HalfPI)
            {
                return Math.PI - seasonalLatitude;
            }
            else if (seasonalLatitude < -MathConstants.HalfPI)
            {
                return -seasonalLatitude - Math.PI;
            }
            return seasonalLatitude;
        }

        internal double GetSolarEquator(double trueAnomaly)
            => -AxialTilt * Math.Cos(trueAnomaly) * (2.0 / 3.0);

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
            => (GetSurfaceTemperatureAtTrueAnomaly(trueAnomaly) * GetInsolationFactor(seasonalLatitude)) + GreenhouseEffect;

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
        internal double GetTemperatureAtElevation(double surfaceTemp, double elevation)
        {
            // When outside the atmosphere, use the black body temperature, ignoring atmospheric effects.
            if (Atmosphere == null || elevation >= Atmosphere.AtmosphericHeight)
            {
                return AverageBlackbodySurfaceTemperature;
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

        private protected int AddResource(Chemical chemical, double proportion, bool isVein, bool isPerturbation = false, int? seed = null)
        {
            if (_resources == null)
            {
                _resources = new Dictionary<string, Resource>();
            }
            var resource = new Resource(chemical, proportion, isVein, isPerturbation, seed);
            _resources.Add(chemical.Name, resource);
            return resource.Seed;
        }

        private protected void AddResources(IEnumerable<(Chemical substance, double proportion, bool vein)> resources)
        {
            foreach (var (substance, proportion, vein) in resources)
            {
                AddResource(substance, proportion, vein);
            }
        }

        private protected virtual void GenerateAngleOfRotation()
        {
            _axialPrecession = Math.Round(Randomizer.Instance.NextDouble(MathConstants.TwoPI), 4);
            if (Randomizer.Instance.NextDouble() <= 0.2) // low chance of an extreme tilt
            {
                AngleOfRotation = Math.Round(Randomizer.Instance.NextDouble(MathConstants.QuarterPI, Math.PI), 4);
            }
            else
            {
                AngleOfRotation = Math.Round(Randomizer.Instance.NextDouble(MathConstants.QuarterPI), 4);
            }
        }

        private protected virtual void GenerateAtmosphere() { }

        private protected virtual void GenerateResources()
            => AddResources(Substance.Composition.GetSurface()
                .GetChemicals(Phase.Solid).Where(x => x.chemical is Metal)
                .Select(x => (x.chemical, x.proportion, true)));

        private protected virtual Planetoid GenerateSatellite(double periapsis, double eccentricity, double maxMass) => null;

        private protected override void GenerateSubstance()
        {
            var (mass, shape) = GetMassAndShape();

            Substance = new Substance
            {
                Composition = GetComposition(mass, shape),
                Mass = mass,
                Temperature = GetInternalTemperature(),
            };
            Shape = shape;
        }

        private double GetAngularVelocity() => RotationalPeriod == 0 ? 0 : MathConstants.TwoPI / RotationalPeriod;

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
            => Atmosphere?.GetAtmosphericPressure(this, temperature, elevation) ?? 0;

        private double GetAverageSurfaceTemperature(bool polar = false)
            => (AverageBlackbodySurfaceTemperature * (polar ? InsolationFactor_Polar : InsolationFactor_Equatorial)) + GreenhouseEffect;

        private protected virtual IComposition GetComposition(double mass, IShape shape) => new Material(Chemical.Rock, Phase.Solid);

        private double GetCurrentSurfaceTemperature() => (BlackbodySurfaceTemperature * InsolationFactor_Equatorial) + GreenhouseEffect;

        private protected virtual double? GetDensity() => null;

        private double GetDiurnalTemperatureVariation()
        {
            var factor = Math.Pow(1 / InsolationAreaRatio, 0.25);
            return (AverageSurfaceTemperature * factor) + (InsolationFactor_Equatorial * factor);
        }

        private double GetEccentricity() => Math.Abs(Randomizer.Instance.Normal(0, 0.05));

        private double GetGreenhouseEffect()
            => Math.Max(0, (AverageBlackbodySurfaceTemperature * InsolationFactor_Equatorial * (Atmosphere?.GreenhouseFactor ?? 1)) - AverageBlackbodySurfaceTemperature);

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
            => Randomizer.Instance.NextDouble() <= Mass * 2.88e-19 / RotationalPeriod * MagnetosphereChanceFactor;

        private protected double GetInsolationAreaRatio()
        {
            var period = RotationalPeriod;
            if (period <= 2500)
            {
                return 1;
            }
            else if (period <= 75000)
            {
                return 0.25;
            }
            else if (period <= 150000)
            {
                return 1.0 / 3.0;
            }
            else if (period <= 300000)
            {
                return 0.5;
            }
            else
            {
                return 1;
            }
        }

        private double GetInsolationFactor(bool polar = false)
            => Atmosphere == null ? 0 : GetInsolationFactor(Atmosphere.Mass, Atmosphere.AtmosphericScaleHeight, polar);

        private double GetInsolationFactor(double latitude)
            => InsolationFactor_Polar + ((InsolationFactor_Equatorial - InsolationFactor_Polar) * Math.Cos(latitude * 0.8));

        private protected virtual double GetInternalTemperature() => 0;

        private bool GetIsTidallyLockedAfter(double years)
            => Orbit == null
            ? false
            : Math.Pow(years / 6.0e11 * Mass * Math.Pow(Orbit.OrbitedObject.Mass, 2) / (Radius * Rigidity), 1.0 / 6.0) >= Orbit.SemiMajorAxis;

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
            => Atmosphere.ContainsWaterVapor ? GetLapseRateMoist(surfaceTemp) : LapseRateDry;

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

            var numerator = (ScienceConstants.RSpecificDryAir * surfaceTemp2)
                + (ScienceConstants.DeltaHvapWater * Atmosphere.WaterRatio * surfaceTemp);
            var denominator = (ScienceConstants.CpTimesRSpecificDryAir * surfaceTemp2)
                + (ScienceConstants.DeltaHvapWaterSquared * Atmosphere.WaterRatio * ScienceConstants.RSpecificRatioOfDryAirToWater);

            return SurfaceGravity * (numerator / denominator);
        }

        private protected virtual double GetMass(IShape shape = null)
        {
            var mass = 0.0;
            do
            {
                mass = MinMass + Math.Abs(Randomizer.Instance.Normal(0, (MaxMass - MinMass) / 3.0));
            } while (mass > MaxMass); // Loop rather than using Math.Min to avoid over-representing MaxMass.
            return mass;
        }

        private protected virtual (double, IShape) GetMassAndShape() => (GetMass(), GetShape());

        private double GetMaxElevation()
        {
            if (HasFlatSurface)
            {
                return 0;
            }

            var max = 2e5 / SurfaceGravity;
            var r = new Random(_seed1);
            var d = 0.0;
            for (var i = 0; i < 5; i++)
            {
                d += r.NextDouble() * 0.5;
            }
            d /= 5;
            return max * (0.5 + d);
        }

        private protected double GetMaxPolarTemperature() => (SurfaceTemperatureAtPeriapsis * InsolationFactor_Polar) + GreenhouseEffect;

        private double GetMaxSurfaceTemperature() => (SurfaceTemperatureAtPeriapsis * InsolationFactor_Equatorial) + GreenhouseEffect;

        private protected double GetMinEquatorTemperature() => (SurfaceTemperatureAtApoapsis * InsolationFactor_Equatorial) + GreenhouseEffect;

        private protected virtual double GetMinSatellitePeriapsis() => 0;

        private double GetMinSurfaceTemperature() => (SurfaceTemperatureAtApoapsis * InsolationFactor_Polar) + GreenhouseEffect;

        private double GetPolarAirMass(double atmosphericScaleHeight)
        {
            var r = Radius / atmosphericScaleHeight;
            var rCosLat = r * CosPolarLatitude;
            return Math.Sqrt((rCosLat * rCosLat) + (2 * r) + 1) - rCosLat;
        }

        private protected virtual double GetRotationalPeriod()
        {
            // Check for tidal locking.
            if (Orbit != null)
            {
                // Invent an orbit age. Precision isn't important here, and some inaccuracy and
                // inconsistency between satellites is desirable. The age of the Solar system is used
                // as an arbitrary norm.
                var years = Randomizer.Instance.Lognormal(0, 1) * 4.6e9;
                if (GetIsTidallyLockedAfter(years))
                {
                    return Orbit.Period;
                }
            }

            if (Randomizer.Instance.NextDouble() <= 0.05) // low chance of an extreme period
            {
                return Math.Round(Randomizer.Instance.NextDouble(MaxRotationalPeriod, ExtremeRotationalPeriod));
            }
            else
            {
                return Math.Round(Randomizer.Instance.NextDouble(MinRotationalPeriod, MaxRotationalPeriod));
            }
        }

        private double GetSeasonalLatitude(double latitude)
        {
            var seasonalLatitude = latitude - SolarEquator;
            if (seasonalLatitude > MathConstants.HalfPI)
            {
                return Math.PI - seasonalLatitude;
            }
            else if (seasonalLatitude < -MathConstants.HalfPI)
            {
                return -seasonalLatitude - Math.PI;
            }
            return seasonalLatitude;
        }

        private protected double GetSeasonalLatitude(double latitude, double trueAnomaly)
            => GetSeasonalLatitudeFromEquator(latitude, GetSolarEquator(trueAnomaly));

        private protected virtual IShape GetShape(double? mass = null, double? knownRadius = null)
            // Gaussian distribution with most values between 1km and 19km.
            => new Ellipsoid(Math.Max(0, Randomizer.Instance.Normal(10000, 4500)), Randomizer.Instance.NextDouble(0.5, 1));

        private double GetSlope(Vector3 position, double latitude, double longitude, float elevation)
        {
            const double sec = MathConstants.PIOver180 / 3600;

            // north
            var otherCoords = (lat: latitude + sec, lon: longitude);
            if (otherCoords.lat > Math.PI)
            {
                otherCoords = (MathConstants.TwoPI - otherCoords.lat, (otherCoords.lon + Math.PI) % MathConstants.TwoPI);
            }
            var otherPos = LatitudeAndLongitudeToVector(otherCoords.lat, otherCoords.lon);
            var otherElevation = GetNormalizedElevationAt(otherPos);
            var slope = Math.Abs(elevation - otherElevation) * MaxElevation / GetDistance(position, otherPos);

            // east
            otherCoords = (lat: latitude, lon: (longitude + sec) % MathConstants.TwoPI);
            otherPos = LatitudeAndLongitudeToVector(otherCoords.lat, otherCoords.lon);
            otherElevation = GetNormalizedElevationAt(otherPos);
            slope = Math.Max(slope, Math.Abs(elevation - otherElevation) * MaxElevation / GetDistance(position, otherPos));

            // south
            otherCoords = (lat: latitude - sec, lon: longitude);
            if (otherCoords.lat < -Math.PI)
            {
                otherCoords = (-MathConstants.TwoPI - otherCoords.lat, (otherCoords.lon + Math.PI) % MathConstants.TwoPI);
            }
            otherPos = LatitudeAndLongitudeToVector(otherCoords.lat, otherCoords.lon);
            otherElevation = GetNormalizedElevationAt(otherPos);
            slope = Math.Max(slope, Math.Abs(elevation - otherElevation) * MaxElevation / GetDistance(position, otherPos));

            // west
            otherCoords = (lat: latitude, lon: (longitude - sec) % MathConstants.TwoPI);
            otherPos = LatitudeAndLongitudeToVector(otherCoords.lat, otherCoords.lon);
            otherElevation = GetNormalizedElevationAt(otherPos);
            return Math.Max(slope, Math.Abs(elevation - otherElevation) * MaxElevation / GetDistance(position, otherPos));
        }

        private double GetSolarEquator()
            => -AxialTilt * (Orbit == null ? 1 : Math.Cos(Orbit.TrueAnomaly)) * (2.0 / 3.0);

        private protected override void ResetCachedTemperatures()
        {
            base.ResetCachedTemperatures();
            _averagePolarSurfaceTemperature = null;
            _averageSurfaceTemperature = null;
            _greenhouseEffect = null;
            _insolationFactor_Equatorial = null;
            _insolationFactor_Polar = null;
            _maxSurfaceTemperature = null;
            _minSurfaceTemperature = null;
            _surfaceTemperature = null;
            _atmosphere?.ResetTemperatureDependentProperties(this);
        }

        private protected override void ResetOrbitalProperties()
        {
            _solarEquator = null;
            ResetCachedTemperatures();
        }

        private void ResetPressureDependentProperties()
        {
            _averagePolarSurfaceTemperature = null;
            _averageSurfaceTemperature = null;
            _greenhouseEffect = null;
            _insolationFactor_Equatorial = null;
            _insolationFactor_Polar = null;
            _atmosphere?.ResetPressureDependentProperties(this);
        }

        private void SetAxis()
        {
            var precession = Quaternion.CreateFromYawPitchRoll(AxialPrecession, 0, 0);
            var precessionVector = Vector3.Transform(Vector3.UnitX, precession);
            var q = Quaternion.CreateFromAxisAngle(precessionVector, AngleOfRotation);
            _axis = Vector3.Transform(Vector3.UnitY, q);
            _axisRotation = Quaternion.Conjugate(q);
            _solarEquator = null;
            ResetOrbitalProperties();
        }
    }
}
