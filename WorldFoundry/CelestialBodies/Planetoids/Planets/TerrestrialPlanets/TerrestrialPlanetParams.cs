using System.Collections.Generic;
using WorldFoundry.Climate;
using WorldFoundry.Substances;
using WorldFoundry.WorldGrids;

namespace WorldFoundry.CelestialBodies.Planetoids.Planets.TerrestrialPlanets
{
    public class TerrestrialPlanetParams
    {
        /// <summary>
        /// The default atmospheric pressure, used if none is specified, in kPa.
        /// </summary>
        public const float DefaultAtmosphericPressure = 101.325f;

        /// <summary>
        /// The default axial tilt, used if none is specified, in radians.
        /// </summary>
        public const float DefaultAxialTilt = 0.41f;

        /// <summary>
        /// The default orbital eccentricity, used if none is specified.
        /// </summary>
        public const float DefaultEccentricity = 0.0167f;

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
        public const float DefaultSurfaceGravity = 9.807f;

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
        public const float DefaultSurfaceTemperature = 289;

        /// <summary>
        /// The default ratio of water coverage, used if none is specified.
        /// </summary>
        public const float DefaultWaterRatio = 0.65f;

        /// <summary>
        /// The target atmospheric pressure, in kPa.
        /// </summary>
        public float? AtmosphericPressure { get; set; }

        /// <summary>
        /// All atmospheric requirements.
        /// </summary>
        public ICollection<ComponentRequirement> AtmosphericRequirements { get; set; }

        /// <summary>
        /// The target axial tilt, in radians.
        /// </summary>
        public float? AxialTilt { get; set; }

        /// <summary>
        /// The target orbital eccentricity.
        /// </summary>
        public float? Eccentricity { get; set; }

        /// <summary>
        /// The target grid size (level of detail).
        /// </summary>
        public int? GridSize { get; set; }

        /// <summary>
        /// Indicates whether a strong magnetosphere is required.
        /// </summary>
        public bool? HasMagnetosphere { get; set; }

        /// <summary>
        /// The target radius, in meters.
        /// </summary>
        public int? Radius { get; set; }

        /// <summary>
        /// The target revolution period, in seconds.
        /// </summary>
        public double? RevolutionPeriod { get; set; }

        /// <summary>
        /// The target rotational period, in seconds.
        /// </summary>
        public double? RotationalPeriod { get; set; }

        /// <summary>
        /// The target surface gravity, in m/s².
        /// </summary>
        public float? SurfaceGravity { get; set; }

        /// <summary>
        /// The target surface temperature, in K.
        /// </summary>
        public float? SurfaceTemperature { get; set; }

        /// <summary>
        /// The target ratio of water to land on the surface.
        /// </summary>
        public float? WaterRatio { get; set; }

        /// <summary>
        /// Initializes a new instance of <see cref="TerrestrialPlanetParams"/> with the given values.
        /// </summary>
        public TerrestrialPlanetParams(
            float? atmosphericPressure = null,
            List<ComponentRequirement> atmosphericRequirements = null,
            float? axialTilt = null,
            float? eccentricity = null,
            int? gridSize = null,
            bool? hasMagnetosphere = null,
            int? radius = null,
            double? revolutionPeriod = null,
            double? rotationalPeriod = null,
            float? surfaceGravity = null,
            float? surfaceTemperature = null,
            float? waterRatio = null)
        {
            AtmosphericPressure = atmosphericPressure;
            AtmosphericRequirements = Atmosphere.HumanBreathabilityRequirements;
            AxialTilt = axialTilt;
            Eccentricity = eccentricity;
            GridSize = gridSize;
            HasMagnetosphere = hasMagnetosphere;
            Radius = radius;
            RevolutionPeriod = revolutionPeriod;
            RotationalPeriod = rotationalPeriod;
            SurfaceGravity = surfaceGravity;
            SurfaceTemperature = surfaceTemperature;
            WaterRatio = waterRatio;
        }

        /// <summary>
        /// Generates a new instance of <see cref="TerrestrialPlanetParams"/> with either the given or default values.
        /// </summary>
        public static TerrestrialPlanetParams FromDefaults(
            float? atmosphericPressure = DefaultAtmosphericPressure,
            List<ComponentRequirement> atmosphericRequirements = null,
            float? axialTilt = DefaultAxialTilt,
            float? eccentricity = DefaultEccentricity,
            int? gridSize = WorldGrid.DefaultGridSize,
            bool? hasMagnetosphere = true,
            int? radius = DefaultRadius,
            double? revolutionPeriod = DefaultRevolutionPeriod,
            double? rotationalPeriod = DefaultRotationalPeriod,
            float? surfaceGravity = DefaultSurfaceGravity,
            float? surfaceTemperature = DefaultSurfaceTemperature,
            float? waterRatio = DefaultWaterRatio)
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
                gridSize,
                hasMagnetosphere,
                radius,
                revolutionPeriod,
                rotationalPeriod,
                surfaceGravity,
                surfaceTemperature,
                waterRatio);
        }
    }
}
