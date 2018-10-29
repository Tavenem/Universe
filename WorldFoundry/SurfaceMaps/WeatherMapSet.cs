using System.Linq;

namespace WorldFoundry.SurfaceMaps
{
    /// <summary>
    /// A collection of <see cref="WeatherMaps"/> along with supplemental weather maps providing
    /// yearlong climate data.
    /// </summary>
    public struct WeatherMapSet
    {
        /// <summary>
        /// A range giving the minimum, maximum, and average temperature throughout the specified
        /// area over the entire period represented by all <see cref="WeatherMaps"/>, as a value
        /// between 0 and 1, with 1 indicating the maximum temperature of the planet.
        /// <seealso cref="CelestialBodies.Planetoids.Planetoid.MaxSurfaceTemperature"/>
        /// </summary>
        public FloatRange OverallTemperatureRange { get; }

        /// <summary>
        /// A two-dimensional array corresponding to points on an equirectangular projected map of a
        /// terrestrial planet's surface. The first index corresponds to the X coordinate, and the
        /// second index corresponds to the Y coordinate. The values represent the proportion of the
        /// year during which there is persistent sea ice.
        /// </summary>
        public FloatRange[,] SeaIceRanges { get; }

        /// <summary>
        /// A two-dimensional array corresponding to points on an equirectangular projected map of a
        /// terrestrial planet's surface. The first index corresponds to the X coordinate, and the
        /// second index corresponds to the Y coordinate. The values represent the proportion of the
        /// year during which there is persistent snow cover.
        /// </summary>
        public FloatRange[,] SnowCoverRanges { get; }

        /// <summary>
        /// A two-dimensional array corresponding to points on an equirectangular projected map of a
        /// terrestrial planet's surface. The first index corresponds to the X coordinate, and the
        /// second index corresponds to the Y coordinate. The values represent the temperature
        /// range. Values range from 0 to 1, with 1 indicating the maximum temperature of the
        /// planet.
        /// <seealso cref="Planetoid.MaxSurfaceTemperature"/>
        /// </summary>
        public FloatRange[,] TemperatureRanges { get; }

        /// <summary>
        /// A two-dimensional array corresponding to points on an equirectangular projected map of a
        /// terrestrial planet's surface. The first index corresponds to the X coordinate, and the
        /// second index corresponds to the Y coordinate. The values represent the total amount of
        /// precipitation indicated on all contained <see cref="WeatherMaps"/>. Values range from 0
        /// to 1, with 1 indicating the maximum annual potential precipitation of the planet's
        /// atmosphere. Will be <see langword="null"/> if no <see cref="WeatherMaps"/> are present.
        /// <seealso cref="Climate.Atmosphere.MaxPrecipitation"/>
        /// </summary>
        public float[,] TotalPrecipitation { get; }

        /// <summary>
        /// A range giving the minimum, maximum, and average precipitation throughout the specified
        /// area over the entire period represented by all <see cref="WeatherMaps"/>, as a value
        /// between 0 and 1, with 1 indicating the maximum annual potential precipitation of the
        /// planet's atmosphere.
        /// <seealso cref="Climate.Atmosphere.MaxPrecipitation"/>
        /// </summary>
        public FloatRange TotalPrecipitationRange { get; }

        /// <summary>
        /// A collection of <see cref="WeatherMaps"/>.
        /// </summary>
        public WeatherMaps[] WeatherMaps { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="WeatherMapSet"/>.
        /// </summary>
        /// <param name="seaIceRanges">A sea ice range map.</param>
        /// <param name="snowCoverRanges">A snow cover range map.</param>
        /// <param name="temperatureRanges">A temperature range map.</param>
        /// <param name="weatherMaps">A set of weather maps.</param>
        public WeatherMapSet(
            FloatRange[,] seaIceRanges,
            FloatRange[,] snowCoverRanges,
            FloatRange[,] temperatureRanges,
            FloatRange overallTemperatureRange,
            WeatherMaps[] weatherMaps)
        {
            OverallTemperatureRange = overallTemperatureRange;
            SeaIceRanges = seaIceRanges;
            SnowCoverRanges = snowCoverRanges;
            TemperatureRanges = temperatureRanges;
            WeatherMaps = weatherMaps;

            if (weatherMaps.Length == 0)
            {
                TotalPrecipitation = null;
                TotalPrecipitationRange = FloatRange.Zero;
            }
            else
            {
                var xLength = weatherMaps[0].Precipitation.GetLength(0);
                var yLength = weatherMaps[0].Precipitation.GetLength(1);
                TotalPrecipitation = new float[xLength, yLength];
                for (var x = 0; x < xLength; x++)
                {
                    for (var y = 0; y < yLength; y++)
                    {
                        TotalPrecipitation[x, y] = weatherMaps.Sum(c => c.Precipitation[x, y]);
                    }
                }
                TotalPrecipitationRange = new FloatRange(
                    weatherMaps.Min(x => x.PrecipitationRange.Min),
                    weatherMaps.Average(x => x.PrecipitationRange.Average),
                    weatherMaps.Max(x => x.PrecipitationRange.Max));
            }
        }
    }
}
