using NeverFoundry.MathAndScience.Numerics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Permissions;
using NeverFoundry.WorldFoundry.Climate;

namespace NeverFoundry.WorldFoundry.CelestialBodies.Planetoids.Planets.TerrestrialPlanets
{
    /// <summary>
    /// A set of parameters which constrains the random generation of a <see cref="TerrestrialPlanet"/>.
    /// </summary>
    [Serializable]
    public struct TerrestrialPlanetParams : ISerializable
    {
        /// <summary>
        /// The default atmospheric pressure, used if none is specified, in kPa.
        /// </summary>
        public const double DefaultAtmosphericPressure = 101.325;

        /// <summary>
        /// The default axial tilt, used if none is specified, in radians.
        /// </summary>
        public const double DefaultAxialTilt = 0.41;

        /// <summary>
        /// The default orbital eccentricity, used if none is specified.
        /// </summary>
        public const double DefaultEccentricity = 0.0167;

        /// <summary>
        /// The default surface gravity, used if none is specified, in m/s².
        /// </summary>
        public const double DefaultSurfaceGravity = 9.807;

        /// <summary>
        /// The default surface temperature, used if none is specified, in K.
        /// </summary>
        public const double DefaultSurfaceTemperature = 289;

        /// <summary>
        /// The default ratio of water coverage, used if none is specified.
        /// </summary>
        public const decimal DefaultWaterRatio = 0.709m;

        /// <summary>
        /// The default mass fraction of water in the atmosphere, used if none is specified.
        /// </summary>
        public const decimal DefaultWaterVaporRatio = 0.0025m;

        /// <summary>
        /// The default planetary radius, used if none is specified, in meters.
        /// </summary>
        public static readonly Number DefaultRadius = new Number(6371000);

        /// <summary>
        /// The default period of revolution, used if none is specified, in seconds.
        /// </summary>
        public static readonly Number DefaultRevolutionPeriod = new Number(31558150);

        /// <summary>
        /// The default period of rotation, used if none is specified, in seconds.
        /// </summary>
        public static readonly Number DefaultRotationalPeriod = new Number(86164);

        /// <summary>
        /// The target atmospheric pressure, in kPa.
        /// </summary>
        public double? AtmosphericPressure { get; }

        /// <summary>
        /// All atmospheric requirements.
        /// </summary>
        public SubstanceRequirement[] AtmosphericRequirements { get; }

        /// <summary>
        /// The target axial tilt, in radians.
        /// </summary>
        public double? AxialTilt { get; }

        /// <summary>
        /// The target orbital eccentricity.
        /// </summary>
        public double? Eccentricity { get; }

        /// <summary>
        /// Indicates whether a strong magnetosphere is required.
        /// </summary>
        public bool? HasMagnetosphere { get; }

        /// <summary>
        /// The number of satellites to place in orbit around the planet.
        /// </summary>
        public byte? NumSatellites { get; }

        /// <summary>
        /// The target radius, in meters.
        /// </summary>
        public Number? Radius { get; }

        /// <summary>
        /// The target revolution period, in seconds.
        /// </summary>
        public Number? RevolutionPeriod { get; }

        /// <summary>
        /// The target rotational period, in seconds.
        /// </summary>
        public Number? RotationalPeriod { get; }

        /// <summary>
        /// The target surface gravity, in m/s².
        /// </summary>
        public double? SurfaceGravity { get; }

        /// <summary>
        /// The target surface temperature, in K.
        /// </summary>
        public double? SurfaceTemperature { get; }

        /// <summary>
        /// The target ratio of water to land on the surface.
        /// </summary>
        public decimal? WaterRatio { get; }

        /// <summary>
        /// The target mass fraction of water in the atmosphere.
        /// </summary>
        public decimal? WaterVaporRatio { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="TerrestrialPlanetParams"/> with the given values.
        /// </summary>
        /// <param name="atmosphericPressure">The target atmospheric pressure, in kPa.</param>
        /// <param name="atmosphericRequirements">All atmospheric requirements.</param>
        /// <param name="axialTilt">The target axial tilt, in radians.</param>
        /// <param name="eccentricity">The target orbital eccentricity.</param>
        /// <param name="hasMagnetosphere">Indicates whether a strong magnetosphere is
        /// required.</param>
        /// <param name="numSatellites">The number of satellites to place in orbit around the planet.</param>
        /// <param name="radius">The target radius, in meters.</param>
        /// <param name="revolutionPeriod">The target revolution period, in seconds.</param>
        /// <param name="rotationalPeriod">The target rotational period, in seconds.</param>
        /// <param name="surfaceGravity">The target surface gravity, in m/s².</param>
        /// <param name="surfaceTemperature">The target surface temperature, in K.</param>
        /// <param name="waterRatio">The target ratio of water to land on the surface.</param>
        /// <param name="waterVaporRatio">The target mass fraction of water in the atmosphere.</param>
        public TerrestrialPlanetParams(
            double? atmosphericPressure = null,
            IEnumerable<SubstanceRequirement>? atmosphericRequirements = null,
            double? axialTilt = null,
            double? eccentricity = null,
            bool? hasMagnetosphere = null,
            byte? numSatellites = null,
            Number? radius = null,
            Number? revolutionPeriod = null,
            Number? rotationalPeriod = null,
            double? surfaceGravity = null,
            double? surfaceTemperature = null,
            decimal? waterRatio = null,
            decimal? waterVaporRatio = null)
        {
            AtmosphericPressure = atmosphericPressure;
            AtmosphericRequirements = atmosphericRequirements?.ToArray() ?? new SubstanceRequirement[0];
            AxialTilt = axialTilt;
            Eccentricity = eccentricity;
            HasMagnetosphere = hasMagnetosphere;
            NumSatellites = numSatellites;
            Radius = radius;
            RevolutionPeriod = revolutionPeriod;
            RotationalPeriod = rotationalPeriod;
            SurfaceGravity = surfaceGravity;
            SurfaceTemperature = surfaceTemperature;
            WaterRatio = waterRatio;
            WaterVaporRatio = waterVaporRatio;
        }

        private TerrestrialPlanetParams(SerializationInfo info, StreamingContext context) : this(
            (double?)info.GetValue(nameof(AtmosphericPressure), typeof(double?)),
            (SubstanceRequirement[])info.GetValue(nameof(AtmosphericRequirements), typeof(SubstanceRequirement[])),
            (double?)info.GetValue(nameof(AxialTilt), typeof(double?)),
            (double?)info.GetValue(nameof(Eccentricity), typeof(double?)),
            (bool?)info.GetValue(nameof(HasMagnetosphere), typeof(bool?)),
            (byte?)info.GetValue(nameof(NumSatellites), typeof(byte?)),
            (Number?)info.GetValue(nameof(Radius), typeof(Number?)),
            (Number?)info.GetValue(nameof(RevolutionPeriod), typeof(Number?)),
            (Number?)info.GetValue(nameof(RotationalPeriod), typeof(Number?)),
            (double?)info.GetValue(nameof(SurfaceGravity), typeof(double?)),
            (double?)info.GetValue(nameof(SurfaceTemperature), typeof(double?)),
            (decimal?)info.GetValue(nameof(WaterRatio), typeof(decimal?)),
            (decimal?)info.GetValue(nameof(WaterVaporRatio), typeof(decimal?))) { }

#pragma warning disable IDE0060 // Remove unused parameter. Reason: bug causes null coalescing assignment to be considered usused.
        /// <summary>
        /// Generates a new instance of <see cref="TerrestrialPlanetParams"/> with either the given or default values.
        /// </summary>
        /// <param name="atmosphericPressure">The target atmospheric pressure, in kPa.</param>
        /// <param name="atmosphericRequirements">All atmospheric requirements.</param>
        /// <param name="axialTilt">The target axial tilt, in radians.</param>
        /// <param name="eccentricity">The target orbital eccentricity.</param>
        /// <param name="hasMagnetosphere">Indicates whether a strong magnetosphere is required.</param>
        /// <param name="numSatellites">The number of satellites to place in orbit around the planet.</param>
        /// <param name="radius">The target radius, in meters.</param>
        /// <param name="revolutionPeriod">The target revolution period, in seconds.</param>
        /// <param name="rotationalPeriod">The target rotational period, in seconds.</param>
        /// <param name="surfaceGravity">The target surface gravity, in m/s².</param>
        /// <param name="surfaceTemperature">The target surface temperature, in K.</param>
        /// <param name="waterRatio">The target ratio of water to land on the surface.</param>
        /// <param name="waterVaporRatio">The target mass fraction of water in the
        /// atmosphere.</param>
        /// <remarks>
        /// Note: any values left <see langword="null"/> will be supplied by the static defaults of
        /// this struct. In order to create a <see cref="TerrestrialPlanetParams"/> instance which
        /// has actual <see langword="null"/> values (indicating no requirement), use the struct
        /// constructor, and supply the static defaults as needed.
        /// </remarks>
        public static TerrestrialPlanetParams FromDefaults(
            double? atmosphericPressure = null,
            IEnumerable<SubstanceRequirement>? atmosphericRequirements = null,
            double? axialTilt = null,
            double? eccentricity = null,
            bool? hasMagnetosphere = true,
            byte? numSatellites = null,
            Number? radius = null,
            Number? revolutionPeriod = null,
            Number? rotationalPeriod = null,
            double? surfaceGravity = null,
            double? surfaceTemperature = null,
            decimal? waterRatio = null,
            decimal? waterVaporRatio = null)
        {
            atmosphericPressure ??= DefaultAtmosphericPressure;
            atmosphericRequirements ??= Atmosphere.HumanBreathabilityRequirements;
            axialTilt ??= DefaultAxialTilt;
            eccentricity ??= DefaultEccentricity;
            hasMagnetosphere = true;
            radius ??= DefaultRadius;
            revolutionPeriod ??= DefaultRevolutionPeriod;
            rotationalPeriod ??= DefaultRotationalPeriod;
            surfaceGravity ??= DefaultSurfaceGravity;
            surfaceTemperature ??= DefaultSurfaceTemperature;
            waterRatio ??= DefaultWaterRatio;
            waterVaporRatio ??= DefaultWaterVaporRatio;
            return new TerrestrialPlanetParams(
                atmosphericPressure,
                atmosphericRequirements,
                axialTilt,
                eccentricity,
                hasMagnetosphere,
                numSatellites,
                radius,
                revolutionPeriod,
                rotationalPeriod,
                surfaceGravity,
                surfaceTemperature,
                waterRatio,
                waterVaporRatio);
        }
#pragma warning restore IDE0060 // Remove unused parameter

        /// <summary>Populates a <see cref="SerializationInfo"></see> with the data needed to
        /// serialize the target object.</summary>
        /// <param name="info">The <see cref="SerializationInfo"></see> to populate with
        /// data.</param>
        /// <param name="context">The destination (see <see cref="StreamingContext"></see>) for this
        /// serialization.</param>
        /// <exception cref="System.Security.SecurityException">The caller does not have the
        /// required permission.</exception>
        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(AtmosphericPressure), AtmosphericPressure);
            info.AddValue(nameof(AtmosphericRequirements), AtmosphericRequirements);
            info.AddValue(nameof(AxialTilt), AxialTilt);
            info.AddValue(nameof(Eccentricity), Eccentricity);
            info.AddValue(nameof(HasMagnetosphere), HasMagnetosphere);
            info.AddValue(nameof(NumSatellites), NumSatellites);
            info.AddValue(nameof(Radius), Radius);
            info.AddValue(nameof(RevolutionPeriod), RevolutionPeriod);
            info.AddValue(nameof(RotationalPeriod), RotationalPeriod);
            info.AddValue(nameof(SurfaceGravity), SurfaceGravity);
            info.AddValue(nameof(SurfaceTemperature), SurfaceTemperature);
            info.AddValue(nameof(WaterRatio), WaterRatio);
            info.AddValue(nameof(WaterVaporRatio), WaterVaporRatio);
        }
    }
}
