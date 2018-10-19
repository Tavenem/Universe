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
        internal const double CosPolarLatitude = 0.04305822778985774;

        /// <summary>
        /// The minimum radius required to achieve hydrostatic equilibrium, in meters.
        /// </summary>
        internal const int MinimumRadius = 600000;

        internal const double PolarLatitude = 1.5277247828211;

        private double? _angleOfRotation;
        /// <summary>
        /// The angle between the Y-axis and the axis of rotation of this <see cref="Planetoid"/>.
        /// Values greater than half Pi indicate clockwise rotation.
        /// </summary>
        /// <remarks>
        /// Note that this is not the same as <see cref="AxialTilt"/>: if the <see cref="Planetoid"/>
        /// is in orbit then <see cref="AxialTilt"/> is relative to the <see cref="Planetoid"/>'s
        /// orbital plane.
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
        /// The atmosphere possessed by this <see cref="Planetoid"/>.
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
        /// Read-only. To set, specify <see cref="AngleOfRotation"/>.
        /// </summary>
        /// <remarks>
        /// If the <see cref="Planetoid"/> isn't orbiting anything, this is the same as <see cref="AngleOfRotation"/>.
        /// </remarks>
        public double AxialTilt => Orbit == null ? AngleOfRotation : AngleOfRotation - Orbit.Inclination;

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

        private Quaternion? _axisRotation;
        /// <summary>
        /// A <see cref="Quaternion"/> representing the rotation of this <see cref="Planetoid"/>'s
        /// <see cref="Axis"/>.
        /// </summary>
        internal Quaternion AxisRotation
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

        private double? _density;
        /// <summary>
        /// The average density of this <see cref="Planetoid"/>, in kg/m³.
        /// </summary>
        public double Density
        {
            get => _density ?? (_density = GetDensity()) ?? DensityForType;
            set
            {
                if (_density == value || (!_density.HasValue && value == DensityForType))
                {
                    return;
                }

                _density = value;
            }
        }

        private const double _densityForType = 0;
        /// <summary>
        /// Indicates the average density of this type of <see cref="Planetoid"/>, in kg/m³.
        /// </summary>
        internal virtual double DensityForType => _densityForType;

        private double? _diurnalTemperatureVariation;
        /// <summary>
        /// The diurnal temperature variation for this body, in K.
        /// </summary>
        public double DiurnalTemperatureVariation
            => _diurnalTemperatureVariation ?? (_diurnalTemperatureVariation = GenerateDiurnalTemperatureVariation()).Value;

        private const int _extremeRotationalPeriod = 1100000;
        private protected virtual int ExtremeRotationalPeriod => _extremeRotationalPeriod;

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

        private const bool _hasFlatSurface = false;
        /// <summary>
        /// Indicates that this <see cref="Planetoid"/>'s surface does not have elevation variations
        /// (i.e. is non-solid). Prevents generation of a height map during <see cref="Terrain"/>
        /// generation.
        /// </summary>
        public virtual bool HasFlatSurface => _hasFlatSurface;

        /// <summary>
        /// A factor which multiplies the chance of this <see cref="Planetoid"/> having a strong magnetosphere.
        /// </summary>
        public virtual double MagnetosphereChanceFactor => 1;

        private double? _maxMass;
        /// <summary>
        /// The maximum mass allowed for this <see cref="Planetoid"/> during random generation, in
        /// kg.
        /// </summary>
        public double MaxMass
        {
            get => (_maxMass ?? MaxMassForType) ?? 0;
            protected set
            {
                if (_maxMass == value || value == MaxMassForType)
                {
                    return;
                }

                _maxMass = value;
            }
        }

        /// <summary>
        /// The maximum mass allowed for this type of <see cref="Planetoid"/> during random
        /// generation, in kg. Null indicates no maximum.
        /// </summary>
        /// <remarks>Null in the base class; subclasses are expected to override.</remarks>
        internal virtual double? MaxMassForType => null;

        private const int _maxRotationalPeriod = 100000;
        private protected virtual int MaxRotationalPeriod => _maxRotationalPeriod;

        internal static int _maxSatellites = 1;
        /// <summary>
        /// The upper limit on the number of satellites this <see cref="Planetoid"/> might have. The
        /// actual number is determined by the orbital characteristics of the satellites it actually has.
        /// </summary>
        /// <remarks>
        /// Set to 1 on the base class; subclasses are expected to set a higher value when appropriate.
        /// </remarks>
        public virtual int MaxSatellites => _maxSatellites;

        private double? _maxSurfaceTemperature;
        /// <summary>
        /// The approximate maximum surface temperature of this <see cref="Planetoid"/>, in K.
        /// </summary>
        /// <remarks>
        /// Gets the equatorial temperature at periapsis, or at the current position if not in orbit.
        /// </remarks>
        public double MaxSurfaceTemperature
            => _maxSurfaceTemperature ?? (_maxSurfaceTemperature = GetMaxSurfaceTemperature()).Value;

        private double? _minMass;
        /// <summary>
        /// The minimum mass allowed for this <see cref="Planetoid"/> during random generation, in
        /// kg.
        /// </summary>
        public double MinMass
        {
            get => (_minMass ?? MinMassForType) ?? 0;
            protected set
            {
                if (_minMass == value || value == MinMassForType)
                {
                    return;
                }

                _minMass = value;
            }
        }

        /// <summary>
        /// The minimum mass allowed for this type of <see cref="Planetoid"/> during random
        /// generation, in kg. Null indicates a minimum of 0.
        /// </summary>
        /// <remarks>Null in the base class; subclasses are expected to override.</remarks>
        internal virtual double? MinMassForType => null;

        private const int _minRotationalPeriod = 8000;
        private protected virtual int MinRotationalPeriod => _minRotationalPeriod;

        private double? _minSatellitePeriapsis;
        /// <summary>
        /// The minimum distance at which a natural satellite may orbit this <see cref="Planetoid"/>.
        /// </summary>
        private protected double MinSatellitePeriapsis
        {
            get => _minSatellitePeriapsis ?? (_minSatellitePeriapsis = GetMinSatellitePeriapsis()).Value;
            set => _minSatellitePeriapsis = value;
        }

        private double? _minSurfaceTemperature;
        /// <summary>
        /// The approximate minimum surface temperature of this <see cref="Planetoid"/>, in K.
        /// </summary>
        /// <remarks>
        /// Gets the polar temperature at apoapsis, or at the current position if not in orbit.
        /// </remarks>
        public double MinSurfaceTemperature
            => _minSurfaceTemperature ?? (_minSurfaceTemperature = GetMinSurfaceTemperature()).Value;

        /// <summary>
        /// The approximate rigidity of this <see cref="Planetoid"/>.
        /// </summary>
        public virtual double Rigidity => 3.0e10;

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

        private Dictionary<Chemical, Resource> _resources;
        /// <summary>
        /// The resources of this <see cref="Planetoid"/>.
        /// </summary>
        public Dictionary<Chemical, Resource> Resources
        {
            get
            {
                if (_resources == null)
                {
                    _resources = new Dictionary<Chemical, Resource>();
                    GenerateResources();
                }
                return _resources;
            }
        }

        /// <summary>
        /// The collection of natural satellites around this <see cref="Planetoid"/>.
        /// </summary>
        /// <remarks>
        /// Unlike <see cref="CelestialRegion.Children"/>, natural satellites are actually siblings
        /// in the local <see cref="CelestialRegion"/> hierarchy, which merely share an orbital relationship.
        /// </remarks>
        public List<Planetoid> Satellites { get; private set; }

        private double? _surfaceTemperature;
        /// <summary>
        /// The current surface temperature of the <see cref="Planetoid"/> at its equator, in K.
        /// </summary>
        public double SurfaceTemperature
            => _surfaceTemperature ?? (_surfaceTemperature = GetCurrentSurfaceTemperature()).Value;

        private Terrain _terrain;
        /// <summary>
        /// The <see cref="Terrain"/> which describes this <see cref="Planetoid"/>'s surface.
        /// </summary>
        public Terrain Terrain => _terrain ?? (_terrain = GetTerrain());

        private double? _eccentricity;
        /// <summary>
        /// The intended eccentricity of the orbit of this <see cref="Planetoid"/>.
        /// </summary>
        private protected double Eccentricity
        {
            get => _eccentricity ?? (_eccentricity = GetEccentricity()).Value;
            set => _eccentricity = value;
        }

        private double? _insolationAreaRatio;
        private double InsolationAreaRatio => _insolationAreaRatio ?? (_insolationAreaRatio = GetInsolationAreaRatio()).Value;

        private double? _insolationFactor_Equatorial;
        /// <summary>
        /// The insolation factor to be used at the equator.
        /// </summary>
        internal double InsolationFactor_Equatorial
        {
            get => _insolationFactor_Equatorial ?? (_insolationFactor_Equatorial = GetInsolationFactor()).Value;
            set => _insolationFactor_Equatorial = value;
        }

        private double? _insolationFactor_Polar;
        /// <summary>
        /// The insolation factor to be used at the predetermined latitude for checking polar temperatures.
        /// </summary>
        private double InsolationFactor_Polar
            => _insolationFactor_Polar ?? (_insolationFactor_Polar = GetInsolationFactor(true)).Value;

        private double? _lapseRateDry;
        /// <summary>
        /// Specifies the dry adiabatic lapse rate within the <see cref="Atmosphere"/> of this <see
        /// cref="Planetoid"/>, in K/m.
        /// </summary>
        private double LapseRateDry
            => _lapseRateDry ?? (_lapseRateDry = GetLapseRateDry()).Value;

        private double? _tropicalEquator;
        private double TropicalEquator => _tropicalEquator ?? (_tropicalEquator = GetTropicalEquator()).Value;

        /// <summary>
        /// Initializes a new instance of <see cref="Planetoid"/>.
        /// </summary>
        public Planetoid() { }

        /// <summary>
        /// Initializes a new instance of <see cref="Planetoid"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="Planetoid"/> is located.
        /// </param>
        public Planetoid(CelestialRegion parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="Planetoid"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="Planetoid"/> is located.
        /// </param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="Planetoid"/> during random generation, in kg.
        /// </param>
        public Planetoid(CelestialRegion parent, double maxMass) : base(parent) => MaxMass = maxMass;

        /// <summary>
        /// Initializes a new instance of <see cref="Planetoid"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="Planetoid"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="Planetoid"/>.</param>
        public Planetoid(CelestialRegion parent, Vector3 position) : base(parent, position) { }

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
        public Planetoid(CelestialRegion parent, Vector3 position, double maxMass) : base(parent, position) => MaxMass = maxMass;

        /// <summary>
        /// Calculates the atmospheric drag on a spherical object within the <see
        /// cref="Atmosphere"/> of this <see cref="Planetoid"/> under given conditions, in N.
        /// </summary>
        /// <param name="density">The density of the atmosphere at the object's location.</param>
        /// <param name="speed">The speed of the object, in m/s.</param>
        /// <returns>The atmospheric drag on the object at the specified height, in N.</returns>
        /// <remarks>
        /// 0.47 is an arbitrary drag coefficient (that of a sphere in a fluid with a Reynolds
        /// number of 10⁴), which may not reflect the actual conditions at all, but the inaccuracy
        /// is accepted since the level of detailed information needed to calculate this value
        /// accurately is not desired in this library.
        /// </remarks>
        public double GetAtmosphericDrag(double density, double speed) =>
            Atmosphere?.GetAtmosphericDrag(this, density, speed) ?? 0;

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
        /// Gets the elevation at the given <paramref name="latitude"/> and <paramref
        /// name="longitude"/>, in meters.
        /// </summary>
        /// <param name="latitude">The latitude at which to determine elevation.</param>
        /// <param name="longitude">The longitude at which to determine elevation.</param>
        /// <returns>The elevation at the given <paramref name="latitude"/> and <paramref
        /// name="longitude"/>, in meters.</returns>
        public double GetElevationAt(double latitude, double longitude)
            => Terrain.GetElevationAt(LatitudeAndLongitudeToVector(latitude, longitude));

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
            return Resources.Select(x => (x.Key, x.Value.GetResourceRichnessAt(position)));
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
        {
            if (Resources.TryGetValue(chemical, out var resource))
            {
                return resource.GetResourceRichnessAt(LatitudeAndLongitudeToVector(latitude, longitude));
            }
            else
            {
                return 0;
            }
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
        internal double GetAtmosphericPressureFromTempAndElevation(double temperature, double elevation)
            => Atmosphere?.GetAtmosphericPressure(this, temperature, elevation) ?? 0;

        /// <summary>
        /// Generates a set of natural satellites for this celestial body.
        /// </summary>
        /// <param name="max">An optional maximum number of satellites to generate.</param>
        internal virtual void GenerateSatellites(int? max = null)
        {
            if (Satellites == null)
            {
                Satellites = new List<Planetoid>();
            }

            if (MaxSatellites <= 0)
            {
                return;
            }

            var minPeriapsis = MinSatellitePeriapsis;
            var maxApoapsis = Orbit == null ? Radius * 100 : Orbit.GetHillSphereRadius() / 3.0;

            while (minPeriapsis <= maxApoapsis && Satellites.Count < MaxSatellites && (!max.HasValue || Satellites.Count < max.Value))
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

                Satellites.Add(satellite);

                minPeriapsis = satellite.Orbit.Apoapsis + satellite.Orbit.GetSphereOfInfluenceRadius();
            }
        }

        /// <summary>
        /// Calculates the Coriolis coefficient for the given <paramref name="latitude"/> on this <see cref="Planetoid"/>.
        /// </summary>
        /// <param name="latitude">A latitude, as an angle in radians from the equator.</param>
        internal double GetCoriolisCoefficient(double latitude) => 2 * AngularVelocity * Math.Sin(latitude);

        /// <summary>
        /// Calculates the Coriolis coefficient for the given <paramref name="position"/> on this
        /// <see cref="Planetoid"/>.
        /// </summary>
        /// <param name="position">A normalized position indicating direction from the center of
        /// this <see cref="Planetoid"/>.</param>
        internal double GetCoriolisCoefficient(Vector3 position) => GetCoriolisCoefficient(VectorToLatitude(position));

        internal double GetHeightForTemperature(double temperature, double surfaceTemp, double elevation)
            => ((surfaceTemp - temperature) / GetLapseRate(surfaceTemp)) - elevation;

        internal double GetInsolationFactor(double atmosphereMass, double atmosphericScaleHeight, bool polar = false)
            => Math.Pow(1320000 * atmosphereMass * (polar ? Math.Pow(0.7, Math.Pow(GetAirMass(atmosphericScaleHeight, CosPolarLatitude), 0.678)) : 0.7) / Mass, 0.25);

        /// <summary>
        /// Determines whether this <see cref="Planetoid"/> will have become
        /// tidally locked to its orbited object in the given timescale.
        /// </summary>
        /// <param name="years">The timescale for tidal locking to have occurred, in years. Usually the approximate age of the local system.</param>
        /// <returns>true if this body will have become tidally locked; false otherwise.</returns>
        internal bool GetIsTidallyLockedAfter(double years)
            => Orbit == null
            ? false
            : Math.Pow(years / 6.0e11 * Mass * Math.Pow(Orbit.OrbitedObject.Mass, 2) / (Radius * Rigidity), 1.0 / 6.0) >= Orbit.SemiMajorAxis;

        internal double GetMaxPolarTemperature() => (SurfaceTemperatureAtPeriapsis * InsolationFactor_Polar) + GreenhouseEffect;

        internal double GetMinEquatorTemperature() => (SurfaceTemperatureAtApoapsis * InsolationFactor_Equatorial) + GreenhouseEffect;

        internal double GetSeasonalLatitude(double latitude)
        {
            var seasonalLatitude = latitude - TropicalEquator;
            if (seasonalLatitude > MathConstants.HalfPI)
            {
                return MathConstants.HalfPI - (seasonalLatitude - MathConstants.HalfPI);
            }
            else if (seasonalLatitude < -MathConstants.HalfPI)
            {
                return -MathConstants.HalfPI - (seasonalLatitude + MathConstants.HalfPI);
            }
            return seasonalLatitude;
        }

        /// <summary>
        /// Calculates the effective surface temperature at the given surface position, including
        /// greenhouse effects, in K.
        /// </summary>
        /// <param name="position">
        /// A hypothetical position for this <see cref="Planet"/> at which its temperature will be
        /// calculated.
        /// </param>
        /// <param name="latitude">
        /// The latitude (relative to the seasonal tropical equator, rather than the rotational
        /// equator) at which temperature will be calculated.
        /// </param>
        /// <returns>The surface temperature, in K.</returns>
        internal double GetSurfaceTemperatureAtPosition(Vector3 position, double latitude)
            => (GetSurfaceTemperatureAtPosition(position) * GetInsolationFactor(latitude)) + GreenhouseEffect;

        /// <summary>
        /// Calculates the effective surface temperature at the given surface position, including
        /// greenhouse effects, in K.
        /// </summary>
        /// <param name="orbitalPosition">
        /// A hypothetical position for this <see cref="Planet"/> at which its temperature will be
        /// calculated.
        /// </param>
        /// <param name="surfacePosition">
        /// The surface position at which temperature will be calculated.
        /// </param>
        /// <returns>The surface temperature, in K.</returns>
        internal double GetSurfaceTemperatureAtPosition(Vector3 orbitalPosition, Vector3 surfacePosition)
            => GetSurfaceTemperatureAtPosition(orbitalPosition, VectorToLatitude(surfacePosition));

        /// <summary>
        /// Calculates the effective surface temperature at the given surface position, including
        /// greenhouse effects, as if this object was at the specified true anomaly in its orbit, in
        /// K. If the body is not in orbit, returns the temperature at its current position.
        /// </summary>
        /// <param name="trueAnomaly">
        /// A true anomaly at which its temperature will be calculated.
        /// </param>
        /// <param name="latitude">
        /// The latitude (relative to the seasonal tropical equator, rather than the rotational
        /// equator) at which temperature will be calculated.
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
        internal double GetSurfaceTemperatureAtTrueAnomaly(double trueAnomaly, double latitude)
            => (GetSurfaceTemperatureAtTrueAnomaly(trueAnomaly) * GetInsolationFactor(latitude)) + GreenhouseEffect;

        /// <summary>
        /// Calculates the effective surface temperature at the given surface position, including
        /// greenhouse effects, as if this object was at the specified true anomaly in its orbit, in
        /// K. If the body is not in orbit, returns the temperature at its current position.
        /// </summary>
        /// <param name="trueAnomaly">
        /// A true anomaly at which its temperature will be calculated.
        /// </param>
        /// <param name="surfacePosition">
        /// The surface position at which temperature will be calculated.
        /// </param>
        /// <returns>The surface temperature, in K.</returns>
        /// <remarks>
        /// The estimation is performed by linear interpolation between the temperature at periapsis
        /// and apoapsis, which is not necessarily accurate for highly elliptical orbits, or bodies
        /// with multiple significant nearby heat sources, but it should be fairly accurate for
        /// bodies in fairly circular orbits around heat sources which are all close to the center
        /// of the orbit, and much faster for successive calls than calculating the temperature at
        /// specific positions precisely.
        /// </remarks>
        internal double GetSurfaceTemperatureAtTrueAnomaly(double trueAnomaly, Vector3 surfacePosition)
            => GetSurfaceTemperatureAtTrueAnomaly(trueAnomaly, VectorToLatitude(surfacePosition));

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

        internal double GetWindFactorAt(double latitude, double elevation)
        {
            var wind = ((Math.Atan2(GetCoriolisCoefficient(latitude), Terrain.GetFrictionCoefficientAt(elevation)) / MathConstants.HalfPI) + 1) / 2;
            return 0.5 + ((wind - 0.5) * 0.5); // lessen impact
        }

        internal double GetWindFactorAt(Vector3 position)
            => GetWindFactorAt(VectorToLatitude(position), Terrain.GetElevationAt(position));

        internal void ResetPressureDependentProperties()
        {
            _averagePolarSurfaceTemperature = null;
            _averageSurfaceTemperature = null;
            _greenhouseEffect = null;
            _insolationFactor_Equatorial = null;
            _insolationFactor_Polar = null;
            _atmosphere?.ResetPressureDependentProperties(this);
        }

        private protected int AddResource(Chemical chemical, double proportion, bool isVein, bool isPerturbation = false, int? seed = null)
        {
            if (_resources == null)
            {
                _resources = new Dictionary<Chemical, Resource>();
            }
            var resource = new Resource(chemical, proportion, isVein, isPerturbation, seed);
            _resources.Add(chemical, resource);
            return resource.Seed;
        }

        private protected void AddResources(IEnumerable<(Chemical substance, double proportion, bool vein)> resources)
        {
            foreach (var (substance, proportion, vein) in resources)
            {
                AddResource(substance, proportion, vein);
            }
        }

        /// <summary>
        /// Determines an angle between the Y-axis and the axis of rotation for this <see cref="Planetoid"/>.
        /// </summary>
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

        private double GenerateDiurnalTemperatureVariation()
        {
            var factor = Math.Pow(1 / InsolationAreaRatio, 0.25);
            return (AverageSurfaceTemperature * factor) + (InsolationFactor_Equatorial * factor);
        }

        private protected virtual void GenerateResources()
            => AddResources(Substance.Composition.GetSurface()
                .GetChemicals(Phase.Solid).Where(x => x.chemical is Metal)
                .Select(x => (x.chemical, x.proportion, true)));

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

        /// <summary>
        /// Generates a new satellite for this <see cref="Planetoid"/> with the specified parameters.
        /// </summary>
        /// <param name="periapsis">The periapsis of the satellite's orbit.</param>
        /// <param name="eccentricity">The eccentricity of the satellite's orbit.</param>
        /// <param name="maxMass">The maximum mass of the satellite.</param>
        /// <returns>A satellite <see cref="Planetoid"/> with an appropriate orbit.</returns>
        /// <remarks>Returns null in the base class; subclasses are expected to override.</remarks>
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

        private double GetAirMass(double atmosphericScaleHeight, double cosLatitude)
        {
            var r = Radius / atmosphericScaleHeight;
            var rCosLat = r * cosLatitude;
            return Math.Sqrt((rCosLat * rCosLat) + (2 * r) + 1) - rCosLat;
        }

        private double GetAngularVelocity() => RotationalPeriod == 0 ? 0 : MathConstants.TwoPI / RotationalPeriod;

        private double GetAverageSurfaceTemperature(bool polar = false)
            => (AverageBlackbodySurfaceTemperature * (polar ? InsolationFactor_Polar : InsolationFactor_Equatorial)) + GreenhouseEffect;

        private protected virtual IComposition GetComposition(double mass, IShape shape) => new Material(Chemical.Rock, Phase.Solid);

        private double GetCurrentSurfaceTemperature() => (BlackbodySurfaceTemperature * InsolationFactor_Equatorial) + GreenhouseEffect;

        private protected virtual double? GetDensity() => null;

        private double GetEccentricity() => Math.Abs(Randomizer.Instance.Normal(0, 0.05));

        /// <summary>
        /// Calculates the total greenhouse effect for this <see cref="Atmosphere"/>, in K.
        /// </summary>
        /// <returns>The total greenhouse effect for this <see cref="Atmosphere"/>, in K.</returns>
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

        private double GetInsolationAreaRatio()
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

        /// <summary>
        /// Calculates the heat added to this <see cref="CelestialBody"/> by insolation at the given
        /// position, in K.
        /// </summary>
        /// <param name="position">
        /// A hypothetical position for this <see cref="CelestialBody"/> at which the heat of
        /// insolation will be calculated.
        /// </param>
        /// <returns>
        /// The heat added to this <see cref="CelestialBody"/> by insolation at the given position,
        /// in K.
        /// </returns>
        /// <remarks>
        /// Rotating bodies radiate some heat as they rotate away from the source. The degree
        /// depends on the speed of the rotation, but is constrained to limits (with very fast
        /// rotation, every spot comes back into an insolated position quickly; and very slow
        /// rotation results in long-term hot/cold hemispheres rather than continuous
        /// heat-shedding). Here, we merely approximate very roughly since accurate calculations
        /// depends on many factors and would involve either circular logic, or extremely extensive
        /// calculus, if we attempted to calculate it accurately.
        /// </remarks>
        private protected override double GetInsolationHeat(Vector3 position)
            => InsolationAreaRatio == 1
                ? base.GetInsolationHeat(position)
                : base.GetInsolationHeat(position) * Math.Pow(InsolationAreaRatio, 0.25);

        private protected virtual double GetInternalTemperature() => 0;

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
        /// Calculates the dry adiabatic lapse rate near the surface of this <see cref="Planetoid"/>, in K/m.
        /// </summary>
        /// <returns>The dry adiabatic lapse rate near the surface of this <see cref="Planetoid"/>, in K/m.</returns>
        private double GetLapseRateDry() => SurfaceGravity / ScienceConstants.CpDryAir;

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
            var gasConstantSurfaceTemp2 = ScienceConstants.RSpecificDryAir * surfaceTemp2;

            var numerator = gasConstantSurfaceTemp2 + (ScienceConstants.DeltaHvapWater * Atmosphere.WaterRatio * surfaceTemp);
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

        private double GetMaxSurfaceTemperature() => (SurfaceTemperatureAtPeriapsis * InsolationFactor_Equatorial) + GreenhouseEffect;

        /// <summary>
        /// Generates an appropriate minimum distance at which a natural satellite may orbit this <see cref="Planetoid"/>.
        /// </summary>
        private protected virtual double GetMinSatellitePeriapsis() => 0;

        private double GetMinSurfaceTemperature() => (SurfaceTemperatureAtApoapsis * InsolationFactor_Polar) + GreenhouseEffect;

        private protected virtual IShape GetShape(double? mass = null, double? knownRadius = null)
            // Gaussian distribution with most values between 1km and 19km.
            => new Ellipsoid(Math.Max(0, Randomizer.Instance.Normal(10000, 4500)), Randomizer.Instance.NextDouble(0.5, 1));

        private Terrain GetTerrain() => new Terrain(this);

        private double GetTropicalEquator()
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
            _tropicalEquator = null;
            ResetCachedTemperatures();
        }

        private void SetAxis()
        {
            var precession = Quaternion.CreateFromYawPitchRoll(AxialPrecession, 0, 0);
            var precessionVector = Vector3.Transform(Vector3.UnitX, precession);
            var q = Quaternion.CreateFromAxisAngle(precessionVector, AngleOfRotation);
            _axis = Vector3.Transform(Vector3.UnitY, q);
            _axisRotation = Quaternion.Conjugate(q);
            _tropicalEquator = null;
            ResetOrbitalProperties();
        }
    }
}
