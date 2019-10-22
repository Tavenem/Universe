using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Permissions;
using WorldFoundry.CelestialBodies.Stars;
using WorldFoundry.Climate;
using WorldFoundry.Place;
using WorldFoundry.Space;
using NeverFoundry;
using NeverFoundry.MathAndScience;
using NeverFoundry.MathAndScience.Chemistry;
using NeverFoundry.MathAndScience.Constants.Numbers;
using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.MathAndScience.Randomization;
using NeverFoundry.MathAndScience.Time;

namespace WorldFoundry.CelestialBodies.Planetoids
{
    /// <summary>
    /// Any non-stellar celestial body, such as a planet or asteroid.
    /// </summary>
    [Serializable]
    public class Planetoid : CelestialLocation
    {
        // polar latitude = 1.5277247828211
        private const double CosPolarLatitude = 0.04305822778985773816101110431352;

        /// <summary>
        /// The minimum radius required to achieve hydrostatic equilibrium, in meters.
        /// </summary>
        private protected const int MinimumRadius = 600000;
        private const double Second = NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.PIOver180 / 3600;
        private const double ThirtySixthPI = NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.PI / 36;

        /// <summary>
        /// Hadley values are a pure function of latitude, and do not vary with any property of the
        /// planet, atmosphere, or season. Since the calculation is relatively expensive, retrieved
        /// values can be stored for the lifetime of the program for future retrieval for the same
        /// (or very similar) location.
        /// </summary>
        private static readonly Dictionary<double, double> _HadleyValues = new Dictionary<double, double>();

        private static readonly double _LowTemp = CelestialSubstances.WaterMeltingPoint - 16;

        private protected double _normalizedSeaLevel;
        private protected int _seed1;
        private protected int _seed2;
        private protected int _seed3;
        private protected int _seed4;
        private protected int _seed5;

        private protected double? _angleOfRotation;
        /// <summary>
        /// The angle between the Y-axis and the axis of rotation of this <see cref="Planetoid"/>.
        /// Values greater than half Pi indicate clockwise rotation. Read-only; set via <see
        /// cref="AxialTilt"/>.
        /// </summary>
        /// <remarks>
        /// Note that this is not the same as <see cref="AxialTilt"/> if the <see cref="Planetoid"/>
        /// is in orbit; in that case <see cref="AxialTilt"/> is relative to the normal of the
        /// orbital plane of the <see cref="Planetoid"/>, not the Y-axis.
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
        }

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
        public Atmosphere Atmosphere
        {
            get
            {
                if (_atmosphere is null)
                {
                    GenerateAtmosphere();
                    if (_atmosphere is null)
                    {
                        _atmosphere = new Atmosphere(this, 0);
                    }
                }
                return _atmosphere;
            }
        }

        private double? _averagePolarSurfaceTemperature;
        /// <summary>
        /// The average surface temperature of this <see cref="Planetoid"/> near its poles
        /// throughout its orbit (or at its current position, if it is not in orbit), in K.
        /// </summary>
        public double AveragePolarSurfaceTemperature
            => _averagePolarSurfaceTemperature ?? (_averagePolarSurfaceTemperature = GetAverageTemperature(true)).Value;

        private double? _averageSurfaceTemperature;
        /// <summary>
        /// The average surface temperature of the <see cref="Planetoid"/> at its equator throughout
        /// its orbit (or at its current position, if it is not in orbit), in K.
        /// </summary>
        public override double AverageSurfaceTemperature
            => _averageSurfaceTemperature ??= GetAverageTemperature();

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
            get => Orbit.HasValue ? AngleOfRotation - Orbit.Value.Inclination : AngleOfRotation;
            set => SetAngleOfRotation(Orbit.HasValue ? value + Orbit.Value.Inclination : value);
        }

        private System.Numerics.Vector3? _axis;
        /// <summary>
        /// A <see cref="Vector3"/> which represents the axis of this <see cref="Planetoid"/>.
        /// Read-only. To set, specify <see cref="AxialPrecession"/> and/or <see cref="AngleOfRotation"/>.
        /// </summary>
        public System.Numerics.Vector3 Axis
        {
            get
            {
                if (!_axis.HasValue)
                {
                    SetAxis();
                }
                return _axis ?? System.Numerics.Vector3.Zero;
            }
        }

        private double? _diurnalTemperatureVariation;
        /// <summary>
        /// The diurnal temperature variation for this body, in K.
        /// </summary>
        public double DiurnalTemperatureVariation
            => _diurnalTemperatureVariation ??= GetDiurnalTemperatureVariation();

        private protected bool? _hasMagnetosphere;
        /// <summary>
        /// Indicates whether this <see cref="Planetoid"/> has a strong magnetosphere.
        /// </summary>
        public bool HasMagnetosphere
        {
            get => _hasMagnetosphere ??= GetHasMagnetosphere();
            set => _hasMagnetosphere = value;
        }

        internal double? _greenhouseEffect;
        /// <summary>
        /// The total greenhouse effect on this <see cref="Planetoid"/>, in K. Read-only; determined
        /// by the properties of the <see cref="Atmosphere"/>.
        /// </summary>
        public double GreenhouseEffect => _greenhouseEffect ??= GetGreenhouseEffect();

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

        private double? _maxSurfaceTemperature;
        /// <summary>
        /// The approximate maximum surface temperature of this <see cref="Planetoid"/>, in K.
        /// </summary>
        /// <remarks>
        /// Gets the equatorial temperature at periapsis, or at the current position if not in orbit.
        /// </remarks>
        public double MaxSurfaceTemperature => _maxSurfaceTemperature ??= GetMaxSurfaceTemperature();

        private double? _minSurfaceTemperature;
        /// <summary>
        /// The approximate minimum surface temperature of this <see cref="Planetoid"/>, in K.
        /// </summary>
        /// <remarks>
        /// Gets the polar temperature at apoapsis, or at the current position if not in orbit.
        /// </remarks>
        public double MinSurfaceTemperature => _minSurfaceTemperature ??= GetMinSurfaceTemperature();

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
        public Number RotationalPeriod
        {
            get => _rotationalPeriod ??= GetRotationalPeriod();
            set
            {
                _rotationalPeriod = value;
                _angularVelocity = null;
                ResetCachedTemperatures();
            }
        }

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

        private protected List<string>? _satelliteIDs;
        /// <summary>
        /// The collection of natural satellites around this <see cref="Planetoid"/>.
        /// </summary>
        /// <remarks>
        /// Unlike <see cref="Location.Children"/>, natural satellites are actually siblings
        /// in the local <see cref="Location"/> hierarchy, which merely share an orbital relationship.
        /// </remarks>
        public IEnumerable<Planetoid> Satellites
            => CelestialParent is null || _satelliteIDs is null
            ? Enumerable.Empty<Planetoid>()
            : CelestialParent.GetAllChildren<Planetoid>()
                .Where(x => _satelliteIDs!.Contains(x.Id));

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

        private double? _surfaceTemperature;
        /// <summary>
        /// The current surface temperature of the <see cref="Planetoid"/> at its equator, in K.
        /// </summary>
        public double SurfaceTemperature => _surfaceTemperature ??= GetCurrentSurfaceTemperature();

        private double? _insolationFactor_Equatorial;
        internal double InsolationFactor_Equatorial
        {
            get => _insolationFactor_Equatorial ??= GetInsolationFactor();
            set => _insolationFactor_Equatorial = value;
        }

        private double? _summerSolsticeTrueAnomaly;
        internal double SummerSolsticeTrueAnomaly
            => _summerSolsticeTrueAnomaly ??= (AxialPrecession + NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.HalfPI) % NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.TwoPI;

        private double? _winterSolsticeTrueAnomaly;
        internal double WinterSolsticeTrueAnomaly
            => _winterSolsticeTrueAnomaly ??= (AxialPrecession + NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.ThreeHalvesPI) % NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.TwoPI;

        private System.Numerics.Quaternion? _axisRotation;
        /// <summary>
        /// A <see cref="System.Numerics.Quaternion"/> representing the rotation of the <see
        /// cref="Axis"/> of this <see cref="Planetoid"/>.
        /// </summary>
        private System.Numerics.Quaternion AxisRotation
        {
            get
            {
                if (!_axisRotation.HasValue)
                {
                    SetAxis();
                }
                return _axisRotation ?? System.Numerics.Quaternion.Identity;
            }
        }

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
        private protected double LapseRateDry => _lapseRateDry ??= (double)SurfaceGravity / NeverFoundry.MathAndScience.Constants.Doubles.ScienceConstants.CpDryAir;

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
        /// <param name="parent">
        /// The containing <see cref="Location"/> in which this <see cref="Planetoid"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="Planetoid"/>.</param>
        internal Planetoid(Location? parent, Vector3 position) : base(parent, position) => Init();

        /// <summary>
        /// Initializes a new instance of <see cref="Planetoid"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="Location"/> in which this <see cref="Planetoid"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="Planetoid"/>.</param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="Planetoid"/> during random generation, in kg.
        /// </param>
        internal Planetoid(Location? parent, Vector3 position, Number maxMass) : base(parent, position) => MaxMass = maxMass;

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
            List<Location>? children)
            : base(
                id,
                name,
                isPrepopulated,
                albedo,
                velocity,
                orbit,
                material,
                children)
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
        }

        private Planetoid(SerializationInfo info, StreamingContext context) : this(
            (string)info.GetValue(nameof(Id), typeof(string)),
            (string?)info.GetValue(nameof(Name), typeof(string)),
            (bool)info.GetValue(nameof(_isPrepopulated), typeof(bool)),
            (double?)info.GetValue(nameof(Albedo), typeof(double?)),
            (Vector3)info.GetValue(nameof(Velocity), typeof(Vector3)),
            (double)info.GetValue(nameof(_normalizedSeaLevel), typeof(double)),
            (int)info.GetValue(nameof(_seed1), typeof(int)),
            (int)info.GetValue(nameof(_seed2), typeof(int)),
            (int)info.GetValue(nameof(_seed3), typeof(int)),
            (int)info.GetValue(nameof(_seed4), typeof(int)),
            (int)info.GetValue(nameof(_seed5), typeof(int)),
            (double?)info.GetValue(nameof(AngleOfRotation), typeof(double?)),
            (Atmosphere?)info.GetValue(nameof(Atmosphere), typeof(Atmosphere)),
            (double?)info.GetValue(nameof(AxialPrecession), typeof(double?)),
            (bool?)info.GetValue(nameof(HasMagnetosphere), typeof(bool?)),
            (double?)info.GetValue(nameof(MaxElevation), typeof(double?)),
            (Number?)info.GetValue(nameof(RotationalOffset), typeof(Number?)),
            (Number?)info.GetValue(nameof(RotationalPeriod), typeof(Number?)),
            (List<Resource>?)info.GetValue(nameof(Resources), typeof(List<Resource>)),
            (List<string>?)info.GetValue(nameof(Satellites), typeof(List<string>)),
            (List<SurfaceRegion>?)info.GetValue(nameof(SurfaceRegions), typeof(List<SurfaceRegion>)),
            (Number?)info.GetValue(nameof(MaxMass), typeof(Number?)),
            (Orbit?)info.GetValue(nameof(Orbit), typeof(Orbit?)),
            (IMaterial?)info.GetValue(nameof(Material), typeof(IMaterial)),
            (List<Location>)info.GetValue(nameof(Children), typeof(List<Location>))) { }

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
        /// <param name="radius">The radius of the region, in meters.</param>
        /// <returns>This instance.</returns>
        public Planetoid AddSurfaceRegion(Vector3 position, Number radius)
        {
            (_surfaceRegions ??= new List<SurfaceRegion>()).Add(new SurfaceRegion(this, position, radius));
            return this;
        }

        /// <summary>
        /// Adds a <see cref="SurfaceRegion"/> instance to this instance's collection. Returns this
        /// instance.
        /// </summary>
        /// <param name="latitude">The latitude of the center of the region.</param>
        /// <param name="longitude">The longitude of the center of the region.</param>
        /// <param name="radius">The radius of the region, in meters.</param>
        /// <returns>This instance.</returns>
        public Planetoid AddSurfaceRegion(double latitude, double longitude, Number radius)
        {
            var position = LatitudeAndLongitudeToVector(latitude, longitude);
            (_surfaceRegions ??= new List<SurfaceRegion>()).Add(new SurfaceRegion(
                this,
                new Vector3(
                    position.X,
                    position.Y,
                    position.Z),
                radius));
            return this;
        }

        /// <summary>
        /// Calculates the atmospheric density for the given conditions, in kg/m³.
        /// </summary>
        /// <param name="time">The time at which to make the calculation.</param>
        /// <param name="latitude">The latitude of the object.</param>
        /// <param name="longitude">The longitude of the object.</param>
        /// <param name="altitude">The altitude of the object.</param>
        /// <returns>The atmospheric density for the given conditions, in kg/m³.</returns>
        public double GetAtmosphericDensity(Duration time, double latitude, double longitude, double altitude)
            => Atmosphere.GetAtmosphericDensity(this, GetTemperatureAtElevation(GetSurfaceTemperatureAt(time, latitude, longitude), altitude), altitude);

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
        public double GetAtmosphericDrag(Duration time, double latitude, double longitude, double altitude, double speed) =>
            Atmosphere.GetAtmosphericDrag(this, GetTemperatureAtElevation(GetSurfaceTemperatureAt(time, latitude, longitude), altitude), altitude, speed);

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
        public double GetAtmosphericPressure(Duration time, double latitude, double longitude)
        {
            var elevation = GetElevationAt(latitude, longitude);
            var trueAnomaly = Orbit?.GetTrueAnomalyAtTime(time) ?? 0;
            return GetAtmosphericPressureFromTempAndElevation(
                GetTemperatureAtElevation(
                    (BlackbodyTemperature * GetInsolationFactor(GetSeasonalLatitude(latitude, trueAnomaly))) + GreenhouseEffect,
                    elevation),
                elevation);
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
        public double GetIllumination(Duration time, double latitude, double longitude)
        {
            var pos = GetPositionAtTime(time);
            var stellarOrbiter = Orbit.HasValue
                ? Orbit.Value.OrbitedObject is Star
                    ? this
                    : Orbit.Value.OrbitedObject.Orbit?.OrbitedObject is Star && Orbit.Value.OrbitedObject is Planetoid planet
                        ? planet
                        : null
                : null;
            var stellarOrbit = stellarOrbiter?.Orbit;
            var starPos = stellarOrbit?.OrbitedObject.GetPositionAtTime(time) ?? Vector3.Zero;

            var (solarRightAscension, solarDeclination) = GetRightAscensionAndDeclination(pos, starPos);
            var longitudeOffset = longitude - solarRightAscension;
            if (longitudeOffset > Math.PI)
            {
                longitudeOffset -= NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.TwoPI;
            }

            var sinSolarElevation = (Math.Sin(solarDeclination) * Math.Sin(latitude))
                + (Math.Cos(solarDeclination) * Math.Cos(latitude) * Math.Cos(longitudeOffset));
            var solarElevation = Math.Asin(sinSolarElevation);
            var lux = solarElevation <= 0 ? 0 : GetLuminousFlux() * sinSolarElevation;

            var starDist = Vector3.Distance(pos, starPos);
            var (_, starLon) = GetEclipticLatLon(pos, starPos);
            foreach (var satellite in Satellites)
            {
                var satPos = satellite.GetPositionAtTime(time);
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
                lux += satellite.GetLuminousFlux() * phase * satellite.Albedo / NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.FourPI / (double)satDist2;
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
        /// <seealso cref="GetLatLonAtDistanceOnRhumbLine(NeverFoundry.MathAndScience.Numerics.Number,
        /// NeverFoundry.MathAndScience.Numerics.Number, NeverFoundry.MathAndScience.Numerics.Number,
        /// NeverFoundry.MathAndScience.Numerics.Number)"/>
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
            finalLongitude = ((finalLongitude + NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.ThreeHalvesPI) % NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.TwoPI) - NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.HalfPI;
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
        /// <seealso
        /// cref="GetLatLonAtDistanceOnGreatCircleArc(NeverFoundry.MathAndScience.Numerics.Number,
        /// NeverFoundry.MathAndScience.Numerics.Number, NeverFoundry.MathAndScience.Numerics.Number,
        /// NeverFoundry.MathAndScience.Numerics.Number)"/>
        /// </remarks>
        public (double latitude, double longitude) GetLatLonAtDistanceOnRhumbLine(double latitude, double longitude, Number distance, double bearing)
        {
            var angularDistance = (double)(distance / Shape.ContainingRadius);
            var deltaLatitude = angularDistance + Math.Cos(angularDistance);
            var finalLatitude = latitude + deltaLatitude;
            var deltaProjectedLatitude = Math.Log(Math.Tan(NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.QuarterPI + (finalLatitude / 2)) / Math.Tan(NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.QuarterPI + (latitude / 2)));
            var q = Math.Abs(deltaProjectedLatitude) > new Number(10, -12) ? deltaLatitude / deltaProjectedLatitude : Math.Cos(latitude);
            var deltaLongitude = angularDistance * Math.Sin(bearing) / q;
            var finalLongitude = longitude + deltaLongitude;
            if (Math.Abs(finalLatitude) > NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.HalfPI)
            {
                finalLatitude = finalLatitude > 0 ? Math.PI - finalLatitude : -Math.PI - finalLatitude;
            }
            finalLongitude = ((finalLongitude + NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.ThreeHalvesPI) % NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.TwoPI) - NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.HalfPI;
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
        public (RelativeDuration? sunrise, RelativeDuration? sunset) GetLocalSunriseAndSunset(Duration time, double latitude)
        {
            var pos = GetPositionAtTime(time);
            var starPos = Orbit.HasValue
                ? Orbit.Value.OrbitedObject is Star
                    ? Orbit.Value.OrbitedObject.GetPositionAtTime(time)
                    : Orbit.Value.OrbitedObject.Orbit?.OrbitedObject is Star && Orbit.Value.OrbitedObject is Planetoid
                        ? Orbit.Value.OrbitedObject.Orbit.Value.OrbitedObject.GetPositionAtTime(time)
                        : Vector3.Zero
                : Vector3.Zero;

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
        public RelativeDuration GetLocalTimeOfDay(Duration time, double longitude)
        {
            var pos = GetPositionAtTime(time);
            var starPos = Orbit.HasValue
                ? Orbit.Value.OrbitedObject is Star
                    ? Orbit.Value.OrbitedObject.GetPositionAtTime(time)
                    : Orbit.Value.OrbitedObject.Orbit?.OrbitedObject is Star && Orbit.Value.OrbitedObject is Planetoid
                        ? Orbit.Value.OrbitedObject.Orbit.Value.OrbitedObject.GetPositionAtTime(time)
                        : Vector3.Zero
                : Vector3.Zero;

            var (solarRightAscension, _) = GetRightAscensionAndDeclination(pos, starPos);
            var longitudeOffset = longitude - solarRightAscension;
            if (longitudeOffset > Math.PI)
            {
                longitudeOffset -= NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.TwoPI;
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
            => (longitude > Math.PI ? longitude - NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.TwoPI : longitude) * RotationalPeriod / MathConstants.TwoPI;

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
        public double GetNormalizedElevationAt(NeverFoundry.MathAndScience.Numerics.Doubles.Vector3 position)
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
            info.AddValue(nameof(Albedo), _albedo);
            info.AddValue(nameof(Velocity), Velocity);
            info.AddValue(nameof(_normalizedSeaLevel), _normalizedSeaLevel);
            info.AddValue(nameof(_seed1), _seed1);
            info.AddValue(nameof(_seed2), _seed2);
            info.AddValue(nameof(_seed3), _seed3);
            info.AddValue(nameof(_seed4), _seed4);
            info.AddValue(nameof(_seed5), _seed5);
            info.AddValue(nameof(AngleOfRotation), _angleOfRotation);
            info.AddValue(nameof(Atmosphere), _atmosphere);
            info.AddValue(nameof(AxialPrecession), _axialPrecession);
            info.AddValue(nameof(HasMagnetosphere), _hasMagnetosphere);
            info.AddValue(nameof(MaxElevation), _maxElevation);
            info.AddValue(nameof(RotationalOffset), _rotationalOffset);
            info.AddValue(nameof(RotationalPeriod), _rotationalPeriod);
            info.AddValue(nameof(Resources), _resources);
            info.AddValue(nameof(Satellites), _satelliteIDs);
            info.AddValue(nameof(SurfaceRegions), _surfaceRegions);
            info.AddValue(nameof(MaxMass), _maxMass);
            info.AddValue(nameof(Orbit), _orbit);
            info.AddValue(nameof(Material), _material);
            info.AddValue(nameof(Children), Children.ToList());
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
            => ((Orbit?.GetTrueAnomalyAtTime(time) ?? 0) - WinterSolsticeTrueAnomaly + NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.TwoPI) % NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.TwoPI / NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.TwoPI;

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
        public (Number phase, bool waxing) GetSatellitePhase(Duration time, Planetoid satellite)
        {
            if (!Satellites.Contains(satellite) || !satellite.Orbit.HasValue || satellite.Orbit.Value.OrbitedObject != this)
            {
                return (Number.Zero, false);
            }

            var pos = GetPositionAtTime(time);

            var starPos = Orbit.HasValue
                ? Orbit.Value.OrbitedObject is Star
                    ? Orbit.Value.OrbitedObject.GetPositionAtTime(time)
                    : Orbit.Value.OrbitedObject.Orbit?.OrbitedObject is Star && Orbit.Value.OrbitedObject is Planetoid
                        ? Orbit.Value.OrbitedObject.Orbit.Value.OrbitedObject.GetPositionAtTime(time)
                        : Vector3.Zero
                : Vector3.Zero;
            var starDist = Vector3.Distance(pos, starPos);
            var (_, starLon) = GetEclipticLatLon(pos, starPos);

            var satellitePosition = satellite.GetPositionAtTime(time);
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
            var (starRightAscension, _) = satellite.GetRightAscensionAndDeclination(satellitePosition, Orbit?.OrbitedObject is Star star ? star.GetPositionAtTime(time) : Vector3.Zero);

            return (phase, (starRightAscension - planetRightAscension + NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.TwoPI) % NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.TwoPI <= Math.PI);
        }

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
        public uint GetSeasonAtTime(uint numSeasons, Duration time)
        {
            var proportionOfYear = GetProportionOfYearAtTime(time);
            var proportionPerSeason = 1.0 / numSeasons;
            return (uint)Math.Floor(proportionOfYear / proportionPerSeason);
        }

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
        public double GetSurfaceTemperatureAt(Duration time, double latitude, double longitude)
            => GetTemperatureAtElevation(
                  (BlackbodyTemperature * GetInsolationFactor(GetSeasonalLatitude(latitude, Orbit?.GetTrueAnomalyAtTime(time) ?? 0))) + GreenhouseEffect,
                  GetElevationAt(latitude, longitude));

        /// <summary>
        /// Calculates the effective surface temperature at the given surface position, including
        /// greenhouse effects, in K.
        /// </summary>
        /// <param name="time">The time at which to make the calculation.</param>
        /// <param name="position">
        /// The surface position at which temperature will be calculated.
        /// </param>
        /// <returns>The surface temperature, in K.</returns>
        public double GetSurfaceTemperatureAtSurfacePosition(Duration time, Vector3 position)
            => GetSurfaceTemperatureAt(time, VectorToLatitude(position), VectorToLongitude(position));

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
            var axialTilt = AxialTilt;
            return new FloatRange(
                  (float)GetTemperatureAtElevation(
                      GetSurfaceTemperatureAtTrueAnomaly(WinterSolsticeTrueAnomaly, GetSeasonalLatitudeFromDeclination(latitude, -axialTilt)),
                      elevation),
                  (float)GetTemperatureAtElevation(
                      GetSurfaceTemperatureAtTrueAnomaly(SummerSolsticeTrueAnomaly, GetSeasonalLatitudeFromDeclination(latitude, axialTilt)),
                      elevation));
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
                return surfaceTemp - (elevation * GetLapseRate(surfaceTemp));
            }
        }

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
            var v = NeverFoundry.MathAndScience.Numerics.Doubles.Vector3.Normalize(
                NeverFoundry.MathAndScience.Numerics.Doubles.Vector3.Transform(
                    new NeverFoundry.MathAndScience.Numerics.Doubles.Vector3(
                        cosLat * Math.Sin(longitude),
                        Math.Sin(latitude),
                        cosLat * Math.Cos(longitude)),
                    NeverFoundry.MathAndScience.Numerics.Doubles.Quaternion.Inverse(AxisRotation)));
            return new Vector3(v.X, v.Y, v.Z);
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
        /// Converts a <see cref="Vector3"/> to a latitude, in radians.
        /// </summary>
        /// <param name="v">A vector representing a position on the surface of this <see cref="Planetoid"/>.</param>
        /// <returns>A latitude, as an angle in radians from the equator.</returns>
        public double VectorToLatitude(Vector3 v)
            => NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.HalfPI - (double)((Vector3)Axis).Angle(v);

        /// <summary>
        /// Converts a <see cref="Vector3"/> to a longitude, in radians.
        /// </summary>
        /// <param name="v">A vector representing a position on the surface of this <see cref="Planetoid"/>.</param>
        /// <returns>A longitude, as an angle in radians from the X-axis at 0 rotation.</returns>
        public double VectorToLongitude(Vector3 v)
        {
            var u = Vector3.Transform(v, AxisRotation);
            return u.X.IsZero && u.Z.IsZero
                ? 0
                : Math.Atan2((double)u.X, (double)u.Z);
        }

        internal void GenerateSatellites()
        {
            if (MaxSatellites <= 0)
            {
                return;
            }

            var minPeriapsis = MinSatellitePeriapsis;
            var maxApoapsis = Orbit.HasValue ? GetHillSphereRadius() / 3 : Shape.ContainingRadius * 100;

            while (minPeriapsis <= maxApoapsis && (_satelliteIDs?.Count ?? 0) < MaxSatellites)
            {
                var periapsis = Randomizer.Instance.NextNumber(minPeriapsis, maxApoapsis);

                var maxEccentricity = (double)((maxApoapsis - periapsis) / (maxApoapsis + periapsis));
                var eccentricity = Randomizer.Instance.PositiveNormalDistributionSample(0, 0.05, maximum: maxEccentricity);

                var semiLatusRectum = periapsis * (1 + eccentricity);
                var semiMajorAxis = semiLatusRectum / (1 - (eccentricity * eccentricity));

                // Keep mass under the limit where the orbital barycenter would be pulled outside the boundaries of this body.
                var maxMass = Number.Max(0, Mass / ((semiMajorAxis / Shape.ContainingRadius) - 1));

                var satellite = GenerateSatellite(periapsis, eccentricity, maxMass);
                if (satellite is null)
                {
                    break;
                }

                (_satelliteIDs ??= new List<string>()).Add(satellite.Id);

                minPeriapsis = (satellite.Orbit?.Apoapsis ?? 0) + satellite.GetSphereOfInfluenceRadius();
            }
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

        internal double GetInsolationFactor(Number atmosphereMass, double atmosphericScaleHeight, bool polar = false)
            => (double)Number.Pow(1320000
                * atmosphereMass
                * (polar
                    ? Math.Pow(0.7, Math.Pow(GetPolarAirMass(atmosphericScaleHeight), 0.678))
                    : 0.7)
                / Mass
                , new Number(25, -2));

        internal double GetPrecipitation(NeverFoundry.MathAndScience.Numerics.Doubles.Vector3 position, double seasonalLatitude, float temperature, float proportionOfYear, out double snow)
            => GetPrecipitation(position.X, position.Y, position.Z, seasonalLatitude, temperature, proportionOfYear, out snow);

        internal double GetPrecipitation(double x, double y, double z, double seasonalLatitude, float temperature, float proportionOfYear, out double snow)
        {
            snow = 0;

            var avgPrecipitation = Atmosphere.AveragePrecipitation * proportionOfYear;

            x *= 1000;
            y *= 1000;
            z *= 1000;

            // Noise map with smooth, broad areas. Random range ~-0.4-1.
            var r1 = 0.3 + (Noise5.GetNoise(x, y, z) * 0.7);

            // More detailed noise map. Random range of ~-1-1 adjusted to ~0.8-1.
            var r2 = (Noise4.GetNoise(x, y, z) * 0.1) + 0.9;

            // Combined map is noise with broad similarity over regions, and minor local
            // diversity, with range of ~-0.3-1.
            var r = r1 * r2;

            // Hadley cells scale by ~10 around the equator, ~-0.5 ±15º lat, ~1 ±40º lat, and ~-1
            // ±75º lat; this creates the ITCZ, the subtropical deserts, the temperate zone, and
            // the polar deserts.
            var roundedAbsLatitude = Math.Round(Math.Max(0, Math.Abs(seasonalLatitude) - ThirtySixthPI), 3);
            if (!_HadleyValues.TryGetValue(roundedAbsLatitude, out var hadleyValue))
            {
                hadleyValue = Math.Cos((1.25 * Math.PI * roundedAbsLatitude) + Math.PI) + Math.Max(0, (1 / (1.5 * (roundedAbsLatitude + 0.05))) - 2.5);
                _HadleyValues.Add(roundedAbsLatitude, hadleyValue);
            }

            // Relative humidity is the Hadley cell value added to the random value, and cut off
            // below 0. Range 0-~11.
            var relativeHumidity = Math.Max(0, r + hadleyValue);

            // In the range up to -16K below freezing, the value is scaled down; below that range it is
            // cut off completely; above it is unchanged.
            relativeHumidity *= ((temperature - _LowTemp) / 16).Clamp(0, 1);

            if (relativeHumidity <= 0)
            {
                return 0;
            }

            // Allow extreme spikes within the ITCZ.
            if (roundedAbsLatitude < Math.PI / 16)
            {
                relativeHumidity *= Math.Max(1, 1.4 + (r2 - 0.9));
            }

            var precipitation = avgPrecipitation * relativeHumidity;

            if (temperature <= CelestialSubstances.WaterMeltingPoint)
            {
                snow = precipitation * Atmosphere.SnowToRainRatio;
            }

            return precipitation;
        }

        internal double GetSeasonalLatitudeFromDeclination(double latitude, double solarDeclination)
        {
            var seasonalLatitude = latitude + (solarDeclination * 2 / 3);
            if (seasonalLatitude > NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.HalfPI)
            {
                return Math.PI - seasonalLatitude;
            }
            else if (seasonalLatitude < -NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.HalfPI)
            {
                return -seasonalLatitude - Math.PI;
            }
            return seasonalLatitude;
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
        internal double GetSurfaceTemperatureAtTrueAnomaly(double trueAnomaly, double seasonalLatitude)
            => (GetTemperatureAtTrueAnomaly(trueAnomaly) * GetInsolationFactor(seasonalLatitude)) + GreenhouseEffect;

        /// <summary>
        /// Converts latitude and longitude to a <see cref="Vector3"/>.
        /// </summary>
        /// <param name="latitude">A latitude, as an angle in radians from the equator.</param>
        /// <param name="longitude">A longitude, as an angle in radians from the X-axis at 0
        /// rotation.</param>
        /// <returns>A normalized <see cref="Vector3"/> representing a position on the surface of
        /// this <see cref="Planetoid"/>.</returns>
        internal NeverFoundry.MathAndScience.Numerics.Doubles.Vector3 LatitudeAndLongitudeToDoubleVector(double latitude, double longitude)
        {
            var cosLat = Math.Cos(latitude);
            return NeverFoundry.MathAndScience.Numerics.Doubles.Vector3.Normalize(
                NeverFoundry.MathAndScience.Numerics.Doubles.Vector3.Transform(
                    new NeverFoundry.MathAndScience.Numerics.Doubles.Vector3(
                        cosLat * Math.Sin(longitude),
                        Math.Sin(latitude),
                        cosLat * Math.Cos(longitude)),
                    NeverFoundry.MathAndScience.Numerics.Doubles.Quaternion.Inverse(AxisRotation)));
        }

        internal void ResetGreenhouseEffect() => ResetCachedTemperatures();

        /// <summary>
        /// Converts a <see cref="Vector3"/> to a latitude, in radians.
        /// </summary>
        /// <param name="v">A vector representing a position on the surface of this <see cref="Planetoid"/>.</param>
        /// <returns>A latitude, as an angle in radians from the equator.</returns>
        internal float VectorToFloatLatitude(System.Numerics.Vector3 v)
        {
            if (!_axis.HasValue)
            {
                SetAxis();
            }
            return (float)(NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.HalfPI - Math.Atan2(
                System.Numerics.Vector3.Cross(_axis!.Value, v).Length(),
                System.Numerics.Vector3.Dot(_axis.Value, v)));
        }

        /// <summary>
        /// Converts a <see cref="Vector3"/> to a longitude, in radians.
        /// </summary>
        /// <param name="v">A vector representing a position on the surface of this <see cref="Planetoid"/>.</param>
        /// <returns>A longitude, as an angle in radians from the X-axis at 0 rotation.</returns>
        internal float VectorToFloatLongitude(System.Numerics.Vector3 v)
        {
            if (!_axisRotation.HasValue)
            {
                SetAxis();
            }
            var u = System.Numerics.Vector3.Transform(v, _axisRotation!.Value);
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

        private protected virtual void GenerateAngleOfRotation()
        {
            _axialPrecession = Randomizer.Instance.NextDouble(NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.TwoPI);
            if (Randomizer.Instance.NextDouble() <= 0.2) // low chance of an extreme tilt
            {
                _angleOfRotation = Randomizer.Instance.NextDouble(NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.QuarterPI, Math.PI);
            }
            else
            {
                _angleOfRotation = Randomizer.Instance.NextDouble(NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.QuarterPI);
            }
            SetAxis();
        }

        private protected virtual void GenerateAtmosphere() { }

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

        private protected virtual Planetoid? GenerateSatellite(Number periapsis, double eccentricity, Number maxMass) => null;

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
            => (AverageBlackbodyTemperature * (polar ? InsolationFactor_Polar : InsolationFactor_Equatorial)) + GreenhouseEffect;

        private double GetCurrentSurfaceTemperature() => (BlackbodyTemperature * InsolationFactor_Equatorial) + GreenhouseEffect;

        private protected override double GetDensity() => DensityForType;

        private double GetDiurnalTemperatureVariation()
        {
            var temp = Temperature ?? 0;
            var timeFactor = (double)(1 - ((RotationalPeriod - 2500) / 595000)).Clamp(0, 1);
            var darkSurfaceTemp = (((AverageBlackbodyTemperature * InsolationFactor_Equatorial) - temp) * timeFactor)
                + temp
                + GreenhouseEffect;
            return AverageSurfaceTemperature - darkSurfaceTemp;
        }

        private double GetEccentricity()
            => _orbit.HasValue
            ? _orbit.Value.Eccentricity
            : Math.Abs(Randomizer.Instance.NormalDistributionSample(0, 0.05));

        private (double latitude, double longitude) GetEclipticLatLon(Vector3 position, Vector3 otherPosition)
        {
            var precession = Quaternion.CreateFromYawPitchRoll(AxialPrecession, 0, 0);
            var p = Vector3.Transform(position - otherPosition, precession) * -1;
            var r = p.Length();
            var lat = Math.Asin((double)(p.Z / r));
            if (lat >= Math.PI)
            {
                lat = NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.TwoPI - lat;
            }
            if (lat == Math.PI)
            {
                lat = 0;
            }
            var lon = Math.Acos((double)(p.X / (r * Math.Cos(lat))));
            return (lat, lon);
        }

        private protected double GetGreenhouseEffect(double insolationFactor, double greenhouseFactor)
            => Math.Max(0, (AverageBlackbodyTemperature * insolationFactor * greenhouseFactor) - AverageBlackbodyTemperature);

        private double GetGreenhouseEffect()
            => GetGreenhouseEffect(InsolationFactor_Equatorial, Atmosphere.GreenhouseFactor);

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
            => InsolationFactor_Polar + ((InsolationFactor_Equatorial - InsolationFactor_Polar) * Math.Cos(latitude * 0.7));

        private protected virtual double GetInternalTemperature() => 0;

        private bool GetIsTidallyLockedAfter(Number years)
            => Orbit.HasValue
            && Number.Pow(years / new Number(6, 11)
                * Mass
                * Orbit.Value.OrbitedObject.Mass.Square()
                / (Shape.ContainingRadius * Rigidity)
                , Number.One / new Number(6)) >= Orbit.Value.SemiMajorAxis;

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

            var numerator = (NeverFoundry.MathAndScience.Constants.Doubles.ScienceConstants.RSpecificDryAir * surfaceTemp2)
                + (NeverFoundry.MathAndScience.Constants.Doubles.ScienceConstants.DeltaHvapWater * Atmosphere.WaterRatioDouble * surfaceTemp);
            var denominator = (NeverFoundry.MathAndScience.Constants.Doubles.ScienceConstants.CpTimesRSpecificDryAir * surfaceTemp2)
                + (NeverFoundry.MathAndScience.Constants.Doubles.ScienceConstants.DeltaHvapWaterSquared * Atmosphere.WaterRatioDouble * NeverFoundry.MathAndScience.Constants.Doubles.ScienceConstants.RSpecificRatioOfDryAirToWater);

            return (double)SurfaceGravity * (numerator / denominator);
        }

        private protected override Number GetMass()
        {
            var minMass = (double)MinMass;
            var maxMass = (double)MaxMass;
            return Randomizer.Instance.PositiveNormalDistributionSample(minMass, (maxMass - minMass) / 3, maximum: maxMass);
        }

        private protected override (double density, Number mass, IShape shape) GetMatter()
            => (GetDensity(), GetMass(), GetShape());

        private double GetMaxElevation()
        {
            if (HasFlatSurface)
            {
                return 0;
            }

            return 200000 / (double)SurfaceGravity;
        }

        private protected double GetMaxPolarTemperature() => (SurfaceTemperatureAtPeriapsis * InsolationFactor_Polar) + GreenhouseEffect;

        private double GetMaxSurfaceTemperature() => (SurfaceTemperatureAtPeriapsis * InsolationFactor_Equatorial) + GreenhouseEffect;

        private protected double GetMinEquatorTemperature() => (SurfaceTemperatureAtApoapsis * InsolationFactor_Equatorial) + GreenhouseEffect - DiurnalTemperatureVariation;

        private protected virtual Number GetMinSatellitePeriapsis() => Number.Zero;

        private double GetMinSurfaceTemperature() => (SurfaceTemperatureAtApoapsis * InsolationFactor_Polar) + GreenhouseEffect - DiurnalTemperatureVariation;

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
            var equatorialPosition = Vector3.Transform(position - otherPosition, AxisRotation);
            var r = equatorialPosition.Length();
            var mPos = !equatorialPosition.Y.IsZero
                && equatorialPosition.Y.Sign() == r.Sign();
            var n = (double)(equatorialPosition.Z / r);
            var declination = Math.Asin(n);
            if (declination > Math.PI)
            {
                declination -= NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.TwoPI;
            }
            var cosDeclination = Math.Cos(declination);
            if (cosDeclination.IsNearlyZero())
            {
                return (0, declination);
            }
            var rightAscension = mPos
                ? Math.Acos(1 / cosDeclination)
                : NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.TwoPI - Math.Acos(1 / cosDeclination);
            if (rightAscension > Math.PI)
            {
                rightAscension -= NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.TwoPI;
            }
            return (rightAscension, declination);
        }

        private protected virtual Number GetRotationalPeriod()
        {
            // Check for tidal locking.
            if (Orbit.HasValue)
            {
                // Invent an orbit age. Precision isn't important here, and some inaccuracy and
                // inconsistency between satellites is desirable. The age of the Solar system is used
                // as an arbitrary norm.
                var years = Randomizer.Instance.LogisticDistributionSample(0, 1) * new Number(4.6, 9);
                if (GetIsTidallyLockedAfter(years))
                {
                    return Orbit.Value.Period;
                }
            }

            if (Randomizer.Instance.NextDouble() <= 0.05) // low chance of an extreme period
            {
                return Randomizer.Instance.NextNumber(MaxRotationalPeriod, ExtremeRotationalPeriod);
            }
            else
            {
                return Randomizer.Instance.NextNumber(MinRotationalPeriod, MaxRotationalPeriod);
            }
        }

        private protected double GetSeasonalLatitude(double latitude, double trueAnomaly)
            => GetSeasonalLatitudeFromDeclination(latitude, GetSolarDeclination(trueAnomaly));

        private protected override IShape GetShape()
            // Gaussian distribution with most values between 1km and 19km.
            => new Ellipsoid(Randomizer.Instance.NormalDistributionSample(10000, 4500, minimum: 0), Randomizer.Instance.NextNumber(Number.Half, 1), Position);

        private double GetSlope(Vector3 position, double latitude, double longitude, double elevation)
        {
            // north
            var otherCoords = (lat: latitude + Second, lon: longitude);
            if (otherCoords.lat > Math.PI)
            {
                otherCoords = (NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.TwoPI - otherCoords.lat, (otherCoords.lon + Math.PI) % NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.TwoPI);
            }
            var otherPos = LatitudeAndLongitudeToVector(otherCoords.lat, otherCoords.lon);
            var otherElevation = GetNormalizedElevationAt(otherPos);
            var slope = Math.Abs(elevation - otherElevation) * MaxElevation / GetDistance(position, otherPos);

            // east
            otherCoords = (lat: latitude, lon: (longitude + Second) % NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.TwoPI);
            otherPos = LatitudeAndLongitudeToVector(otherCoords.lat, otherCoords.lon);
            otherElevation = GetNormalizedElevationAt(otherPos);
            slope = Math.Max(slope, Math.Abs(elevation - otherElevation) * MaxElevation / GetDistance(position, otherPos));

            // south
            otherCoords = (lat: latitude - Second, lon: longitude);
            if (otherCoords.lat < -Math.PI)
            {
                otherCoords = (-NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.TwoPI - otherCoords.lat, (otherCoords.lon + Math.PI) % NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.TwoPI);
            }
            otherPos = LatitudeAndLongitudeToVector(otherCoords.lat, otherCoords.lon);
            otherElevation = GetNormalizedElevationAt(otherPos);
            slope = Math.Max(slope, Math.Abs(elevation - otherElevation) * MaxElevation / GetDistance(position, otherPos));

            // west
            otherCoords = (lat: latitude, lon: (longitude - Second) % NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.TwoPI);
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

        private void ResetPressureDependentProperties()
        {
            _averagePolarSurfaceTemperature = null;
            _averageSurfaceTemperature = null;
            _greenhouseEffect = null;
            _insolationFactor_Equatorial = null;
            _insolationFactor_Polar = null;
            _atmosphere?.ResetPressureDependentProperties(this);
        }

        private protected void SetAngleOfRotation(double angle)
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
            SetAxis();
        }

        private void SetAxis()
        {
            var precession = System.Numerics.Quaternion.CreateFromYawPitchRoll((float)AxialPrecession, 0, 0);
            var precessionVector = System.Numerics.Vector3.Transform(System.Numerics.Vector3.UnitX, precession);
            var q = System.Numerics.Quaternion.CreateFromAxisAngle(precessionVector, (float)AngleOfRotation);
            _axis = System.Numerics.Vector3.Transform(System.Numerics.Vector3.UnitY, q);
            _axisRotation = System.Numerics.Quaternion.Conjugate(q);

            ResetCachedTemperatures();
        }
    }
}
