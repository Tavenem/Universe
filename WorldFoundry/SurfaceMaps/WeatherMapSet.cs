using System.Linq;
using WorldFoundry.CelestialBodies.Planetoids.Planets.TerrestrialPlanets;
using WorldFoundry.Climate;

namespace WorldFoundry.SurfaceMaps
{
    /// <summary>
    /// A collection of <see cref="WeatherMaps"/> along with supplemental weather maps providing
    /// yearlong climate data.
    /// </summary>
    public struct WeatherMapSet
    {
        /// <summary>
        /// A two-dimensional array corresponding to points on an equirectangular projected map of a
        /// terrestrial planet's surface. The first index corresponds to the X coordinate, and the
        /// second index corresponds to the Y coordinate. The values represent <see
        /// cref="ClimateType"/>, based on average annual temperature.
        /// </summary>
        public ClimateType[,] Climate { get; }

        /// <summary>
        /// A two-dimensional array corresponding to points on an equirectangular projected map of a
        /// terrestrial planet's surface. The first index corresponds to the X coordinate, and the
        /// second index corresponds to the Y coordinate. The values represent <see
        /// cref="HumidityType"/>, based on annual precipitation.
        /// </summary>
        public HumidityType[,] Humidity { get; }

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
        /// <seealso cref="Atmosphere.MaxPrecipitation"/>
        /// </summary>
        public float[,] TotalPrecipitation { get; }

        /// <summary>
        /// A range giving the minimum, maximum, and average precipitation throughout the specified
        /// area over the entire period represented by all <see cref="WeatherMaps"/>, as a value
        /// between 0 and 1, with 1 indicating the maximum annual potential precipitation of the
        /// planet's atmosphere.
        /// <seealso cref="Atmosphere.MaxPrecipitation"/>
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
        /// <param name="overallTemperatureRange">An overall temperature range map.</param>
        /// <param name="weatherMaps">A set of weather maps.</param>
        public WeatherMapSet(
            TerrestrialPlanet planet,
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

            if (TemperatureRanges == null)
            {
                Climate = null;
            }
            else
            {
                var xLength = TemperatureRanges.GetLength(0);
                var yLength = TemperatureRanges.GetLength(1);

                Climate = new ClimateType[xLength, yLength];
                for (var x = 0; x < xLength; x++)
                {
                    for (var y = 0; y < yLength; y++)
                    {
                        Climate[x, y] = ClimateTypes.GetClimateType(TemperatureRanges[x, y].Average);
                    }
                }
            }

            if (weatherMaps.Length == 0)
            {
                Humidity = null;
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

                Humidity = new HumidityType[xLength, yLength];
                for (var x = 0; x < xLength; x++)
                {
                    for (var y = 0; y < yLength; y++)
                    {
                        Humidity[x, y] = ClimateTypes.GetHumidityType(TotalPrecipitation[x, y] * planet.Atmosphere.MaxPrecipitation);
                    }
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of <see cref="WeatherMapSet"/>.
        /// </summary>
        /// <param name="seaIceRanges">A sea ice range map.</param>
        /// <param name="snowCoverRanges">A snow cover range map.</param>
        /// <param name="temperatureRanges">A temperature range map.</param>
        /// <param name="weatherMaps">A set of weather maps.</param>
        /// <param name="overallTemperatureRange">An overall temperature range map.</param>
        /// <param name="humidity">A humidity map.</param>
        public WeatherMapSet(
            FloatRange[,] seaIceRanges,
            FloatRange[,] snowCoverRanges,
            FloatRange[,] temperatureRanges,
            FloatRange overallTemperatureRange,
            WeatherMaps[] weatherMaps,
            HumidityType[,] humidity)
        {
            Humidity = humidity;
            OverallTemperatureRange = overallTemperatureRange;
            SeaIceRanges = seaIceRanges;
            SnowCoverRanges = snowCoverRanges;
            TemperatureRanges = temperatureRanges;
            WeatherMaps = weatherMaps;

            if (TemperatureRanges == null)
            {
                Climate = null;
            }
            else
            {
                var xLength = TemperatureRanges.GetLength(0);
                var yLength = TemperatureRanges.GetLength(1);

                Climate = new ClimateType[xLength, yLength];
                for (var x = 0; x < xLength; x++)
                {
                    for (var y = 0; y < yLength; y++)
                    {
                        Climate[x, y] = ClimateTypes.GetClimateType(TemperatureRanges[x, y].Average);
                    }
                }
            }

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
