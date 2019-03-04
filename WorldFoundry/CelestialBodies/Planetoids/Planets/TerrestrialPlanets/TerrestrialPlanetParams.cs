using Substances;
using System.Collections.Generic;
using System.Linq;
using WorldFoundry.Climate;

namespace WorldFoundry.CelestialBodies.Planetoids.Planets.TerrestrialPlanets
{
    /// <summary>
    /// A set of parameters which constrains the random generation of a <see cref="TerrestrialPlanet"/>.
    /// </summary>
    public struct TerrestrialPlanetParams
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
        /// The default planetary radius, used if none is specified, in meters.
        /// </summary>
        public const int DefaultRadius = 6371000;

        /// <summary>
        /// The default period of revolution, used if none is specified, in seconds.
        /// </summary>
        public const double DefaultRevolutionPeriod = 31558150;

        /// <summary>
        /// The default period of rotation, used if none is specified, in seconds.
        /// </summary>
        public const double DefaultRotationalPeriod = 86164;

        /// <summary>
        /// The default surface gravity, used if none is specified, in m/s².
        /// </summary>
        public const double DefaultSurfaceGravity = 9.807;

        /// <summary>
        /// The default surface temperature, used if none is specified, in K.
        /// </summary>
        /// <remarks>
        /// This target overshoots the average surface temperature of Earth considerably, since
        /// Earth's topography means that many locations have significantly reduced temperatures than
        /// they would if they had been at sea level (consider a mountaintop). Through
        /// experimentation, this value was determined to result in planets whose actual average
        /// surface temperature, after taking topography into account, is close to 289.15K.
        /// </remarks>
        public const double DefaultSurfaceTemperature = 289;

        /// <summary>
        /// The default ratio of water coverage, used if none is specified.
        /// </summary>
        public const double DefaultWaterRatio = 0.65;

        /// <summary>
        /// The default mass fraction of water in the atmosphere, used if none is specified.
        /// </summary>
        public const double DefaultWaterVaporRatio = 0.0025;

        /// <summary>
        /// The target atmospheric pressure, in kPa.
        /// </summary>
        public double? AtmosphericPressure { get; }

        /// <summary>
        /// All atmospheric requirements.
        /// </summary>
        public Requirement[] AtmosphericRequirements { get; }

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
        public int? Radius { get; }

        /// <summary>
        /// The target revolution period, in seconds.
        /// </summary>
        public double? RevolutionPeriod { get; }

        /// <summary>
        /// The target rotational period, in seconds.
        /// </summary>
        public double? RotationalPeriod { get; }

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
        public double? WaterRatio { get; }

        /// <summary>
        /// The target mass fraction of water in the atmosphere.
        /// </summary>
        public double? WaterVaporRatio { get; }

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
            IEnumerable<Requirement>? atmosphericRequirements = null,
            double? axialTilt = null,
            double? eccentricity = null,
            bool? hasMagnetosphere = null,
            byte? numSatellites = null,
            int? radius = null,
            double? revolutionPeriod = null,
            double? rotationalPeriod = null,
            double? surfaceGravity = null,
            double? surfaceTemperature = null,
            double? waterRatio = null,
            double? waterVaporRatio = null)
        {
            AtmosphericPressure = atmosphericPressure;
            AtmosphericRequirements = atmosphericRequirements?.ToArray() ?? new Requirement[0];
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
        /// <param name="waterVaporRatio">The target mass fraction of water in the atmosphere.</param>
        public static TerrestrialPlanetParams FromDefaults(
            double? atmosphericPressure = DefaultAtmosphericPressure,
            IEnumerable<Requirement>? atmosphericRequirements = null,
            double? axialTilt = DefaultAxialTilt,
            double? eccentricity = DefaultEccentricity,
            bool? hasMagnetosphere = true,
            byte? numSatellites = null,
            int? radius = DefaultRadius,
            double? revolutionPeriod = DefaultRevolutionPeriod,
            double? rotationalPeriod = DefaultRotationalPeriod,
            double? surfaceGravity = DefaultSurfaceGravity,
            double? surfaceTemperature = DefaultSurfaceTemperature,
            double? waterRatio = DefaultWaterRatio,
            double? waterVaporRatio = DefaultWaterVaporRatio)
        {
            if (atmosphericRequirements == null)
            {
                atmosphericRequirements = Atmosphere.HumanBreathabilityRequirements;
            }
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
    }
}
