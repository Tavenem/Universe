namespace WorldFoundry.SurfaceMaps
{
    /// <summary>
    /// A set of two-dimensional arrays corresponding to points on an equirectangular projected map
    /// of a terrestrial planet's surface, with information about the planet's weather.
    /// </summary>
    public struct WeatherMaps
    {
        /// <summary>
        /// A two-dimensional array corresponding to points on an equirectangular projected map of a
        /// terrestrial planet's surface. The first index corresponds to the X coordinate, and the
        /// second index corresponds to the Y coordinate. The values represent the total amount of
        /// precipitation which falls during the specified period. Values range from 0 to 1, with 1
        /// indicating the maximum annual potential precipitation of the planet's atmosphere.
        /// <seealso cref="Climate.Atmosphere.MaxPrecipitation"/>
        /// </summary>
        public float[,] Precipitation { get; }

        /// <summary>
        /// A range giving the minimum, maximum, and average precipitation throughout the specified
        /// area, as a value between 0 and 1, with 1 indicating the maximum annual potential
        /// precipitation of the planet's atmosphere.
        /// <seealso cref="Climate.Atmosphere.MaxPrecipitation"/>
        /// </summary>
        public FloatRange PrecipitationRange { get; }

        /// <summary>
        /// A two-dimensional array corresponding to points on an equirectangular projected map of a
        /// terrestrial planet's surface. The first index corresponds to the X coordinate, and the
        /// second index corresponds to the Y coordinate. The values indicate where sea ice is
        /// present during the specified period.
        /// </summary>
        public bool[,] SeaIce { get; }

        /// <summary>
        /// A two-dimensional array corresponding to points on an equirectangular projected map of a
        /// terrestrial planet's surface. The first index corresponds to the X coordinate, and the
        /// second index corresponds to the Y coordinate. The values indicate where persistent snow
        /// cover is present during the specified period.
        /// </summary>
        public bool[,] SnowCover { get; }

        /// <summary>
        /// A two-dimensional array corresponding to points on an equirectangular projected map of a
        /// terrestrial planet's surface. The first index corresponds to the X coordinate, and the
        /// second index corresponds to the Y coordinate. The values represent the total amount of
        /// snow which falls during the specified period. Values range from 0 to 1, with 1
        /// indicating the maximum potential snowfall of the planet's atmosphere.
        /// <seealso cref="Climate.Atmosphere.MaxSnowfall"/>
        /// </summary>
        public float[,] Snowfall { get; }

        /// <summary>
        /// A range giving the minimum, maximum, and average snowfall throughout the specified
        /// area, as a value between 0 and 1, with 1 indicating the maximum annual potential
        /// snowfall of the planet's atmosphere.
        /// <seealso cref="Climate.Atmosphere.MaxSnowfall"/>
        /// </summary>
        public FloatRange SnowfallRange { get; }

        /// <summary>
        /// A two-dimensional array corresponding to points on an equirectangular projected map of a
        /// terrestrial planet's surface. The first index corresponds to the X coordinate, and the
        /// second index corresponds to the Y coordinate. The values represent the average
        /// temperature during the specified period. Values range from 0 to 1, with 1 indicating the
        /// maximum temperature of the planet.
        /// <seealso cref="CelestialBodies.Planetoids.Planetoid.MaxSurfaceTemperature"/>
        /// </summary>
        public float[,] Temperature { get; }

        /// <summary>
        /// A range giving the minimum, maximum, and average temperature throughout the specified
        /// area, as a value between 0 and 1, with 1 indicating the maximum temperature of the
        /// planet.
        /// <seealso cref="CelestialBodies.Planetoids.Planetoid.MaxSurfaceTemperature"/>
        /// </summary>
        public FloatRange TemperatureRange { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="WeatherMaps"/>.
        /// </summary>
        /// <param name="precipitation">A precipitation map.</param>
        /// <param name="seaIce">A sea ice map.</param>
        /// <param name="snowCover">A snow cover map.</param>
        /// <param name="snowfall">A snowfall map.</param>
        /// <param name="temperature">A temperature map.</param>
        public WeatherMaps(
            float[,] precipitation,
            FloatRange precipitationRange,
            bool[,] seaIce,
            bool[,] snowCover,
            float[,] snowfall,
            FloatRange snowfallRange,
            float[,] temperature,
            FloatRange temperatureRange)
        {
            Precipitation = precipitation;
            PrecipitationRange = precipitationRange;
            SeaIce = seaIce;
            SnowCover = snowCover;
            Snowfall = snowfall;
            SnowfallRange = snowfallRange;
            Temperature = temperature;
            TemperatureRange = temperatureRange;
        }
    }
}
