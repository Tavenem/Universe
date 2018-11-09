using MathAndScience;
using System;
using System.Linq;
using WorldFoundry.CelestialBodies.Planetoids.Planets.TerrestrialPlanets;
using WorldFoundry.Climate;

namespace WorldFoundry.SurfaceMaps
{
    /// <summary>
    /// A collection of weather maps providing yearlong climate data.
    /// </summary>
    public struct WeatherMaps
    {
        /// <summary>
        /// A two-dimensional array corresponding to points on an equirectangular projected map of a
        /// terrestrial planet's surface. The first index corresponds to the X coordinate, and the
        /// second index corresponds to the Y coordinate. The values represent <see
        /// cref="BiomeType"/>.
        /// </summary>
        public BiomeType[,] Biome { get; }

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
        /// cref="EcologyType"/>.
        /// </summary>
        public EcologyType[,] Ecology { get; }

        /// <summary>
        /// A two-dimensional array corresponding to points on an equirectangular projected map of a
        /// terrestrial planet's surface. The first index corresponds to the X coordinate, and the
        /// second index corresponds to the Y coordinate. The values represent <see
        /// cref="HumidityType"/>, based on annual precipitation.
        /// </summary>
        public HumidityType[,] Humidity { get; }

        /// <summary>
        /// The maximum annual precipitation expected to be produced by this atmosphere, in mm. Not
        /// necessarily the actual maximum precipitation within the given region (use <c><see
        /// cref="TotalPrecipitationRange"/>.Max</c> for that).
        /// </summary>
        public double MaxPrecipitation { get; }

        /// <summary>
        /// The maximum annual snowfall expected to be produced by this atmosphere, in mm. Not
        /// necessarily the actual maximum snowfall within the given region (use <c><see
        /// cref="TotalSnowfallRange"/>.Max</c> for that).
        /// </summary>
        public double MaxSnowfall { get; }

        /// <summary>
        /// The approximate maximum surface temperature of the planet, in K. Not necessarily the
        /// actual maximum temperature within the given region (use <c><see
        /// cref="OverallTemperatureRange"/>.Max</c> for that).
        /// </summary>
        public double MaxTemperature { get; }

        /// <summary>
        /// A range giving the minimum, maximum, and average temperature throughout the specified
        /// area over the full year, in K.
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
        /// The number of seasons into which the year is divided by this set, for the purposes of
        /// precipitation and snowfall reporting.
        /// </summary>
        public int Seasons { get; }

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
        /// range, in K.
        /// </summary>
        public FloatRange[,] TemperatureRanges { get; }

        /// <summary>
        /// A two-dimensional array corresponding to points on an equirectangular projected map of a
        /// terrestrial planet's surface. The first index corresponds to the X coordinate, and the
        /// second index corresponds to the Y coordinate. The values represent the total annual
        /// precipitation, as values between 0 and 1, with 1 indicating the maximum annual potential
        /// precipitation of the planet's atmosphere. Will be <see langword="null"/> if no <see
        /// cref="PrecipitationMaps"/> are present.
        /// <seealso cref="MaxPrecipitation"/>
        /// </summary>
        public float[,] TotalPrecipitation { get; }

        /// <summary>
        /// A range giving the minimum, maximum, and average precipitation throughout the specified
        /// area over the entire year, in mm.
        /// </summary>
        public FloatRange TotalPrecipitationRange { get; }

        /// <summary>
        /// A two-dimensional array corresponding to points on an equirectangular projected map of a
        /// terrestrial planet's surface. The first index corresponds to the X coordinate, and the
        /// second index corresponds to the Y coordinate. The values represent the total annual
        /// snowfall, as values between 0 and 1, with 1 indicating the maximum annual potential
        /// snowfall of the planet's atmosphere. Will be <see langword="null"/> if no <see
        /// cref="PrecipitationMaps"/> are present.
        /// <seealso cref="MaxSnowfall"/>
        /// </summary>
        public float[,] TotalSnowfall { get; }

        /// <summary>
        /// A range giving the minimum, maximum, and average snowfall throughout the specified area
        /// over the entire year, in mm.
        /// </summary>
        public FloatRange TotalSnowfallRange { get; }

        /// <summary>
        /// A collection of <see cref="PrecipitationMaps"/>.
        /// </summary>
        public PrecipitationMaps[] PrecipitationMaps { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="WeatherMaps"/>.
        /// </summary>
        /// <param name="biome">A biome map.</param>
        /// <param name="climate">A climate map.</param>
        /// <param name="ecology">An ecology map.</param>
        /// <param name="humidity">A humidity map.</param>
        /// <param name="maxPrecipitation">The maximum annual precipitation expected to be produced
        /// by this atmosphere, in mm.</param>
        /// <param name="maxTemperature">The approximate maximum surface temperature of the
        /// planet, in K.</param>
        /// <param name="seaIceRanges">A sea ice range map.</param>
        /// <param name="snowCoverRanges">A snow cover range map.</param>
        /// <param name="precipitationMaps">A set of weather maps.</param>
        /// <param name="temperatureRanges">A temperature range map.</param>
        /// <param name="totalPrecipitation">A total precipitation map.</param>
        public WeatherMaps(
            BiomeType[,] biome,
            ClimateType[,] climate,
            EcologyType[,] ecology,
            HumidityType[,] humidity,
            double maxPrecipitation,
            double maxTemperature,
            FloatRange[,] seaIceRanges,
            FloatRange[,] snowCoverRanges,
            PrecipitationMaps[] precipitationMaps,
            FloatRange[,] temperatureRanges,
            float[,] totalPrecipitation)
        {
            Biome = biome;
            Climate = climate;
            Ecology = ecology;
            Humidity = humidity;
            MaxPrecipitation = maxPrecipitation;
            MaxTemperature = maxTemperature;
            SeaIceRanges = seaIceRanges;
            SnowCoverRanges = snowCoverRanges;
            PrecipitationMaps = precipitationMaps;
            TemperatureRanges = temperatureRanges;
            TotalPrecipitation = totalPrecipitation;

            MaxSnowfall = maxPrecipitation * Atmosphere.SnowToRainRatio;
            Seasons = precipitationMaps?.Length ?? 0;

            if (TemperatureRanges == null)
            {
                OverallTemperatureRange = FloatRange.Zero;
            }
            else
            {
                var xLength = TemperatureRanges.GetLength(0);
                var yLength = TemperatureRanges.GetLength(1);

                var min = 2f;
                var max = -2f;
                var avgSum = 0f;
                for (var x = 0; x < xLength; x++)
                {
                    for (var y = 0; y < yLength; y++)
                    {
                        min = Math.Min(min, TemperatureRanges[x, y].Min);
                        max = Math.Max(max, TemperatureRanges[x, y].Max);
                        avgSum += TemperatureRanges[x, y].Average;
                    }
                }
                OverallTemperatureRange = new FloatRange(min, avgSum / (xLength * yLength), max);
            }

            if (precipitationMaps.Length == 0)
            {
                TotalPrecipitationRange = FloatRange.Zero;
                TotalSnowfall = null;
                TotalSnowfallRange = FloatRange.Zero;
            }
            else
            {
                var xLength = precipitationMaps[0].Precipitation.GetLength(0);
                var yLength = precipitationMaps[0].Precipitation.GetLength(1);

                TotalPrecipitationRange = new FloatRange(
                    (float)(precipitationMaps.Min(x => x.PrecipitationRange.Min) * maxPrecipitation),
                    (float)(precipitationMaps.Average(x => x.PrecipitationRange.Average) * maxPrecipitation),
                    (float)(precipitationMaps.Max(x => x.PrecipitationRange.Max) * maxPrecipitation));

                TotalSnowfall = new float[xLength, yLength];
                for (var x = 0; x < xLength; x++)
                {
                    for (var y = 0; y < yLength; y++)
                    {
                        TotalSnowfall[x, y] = precipitationMaps.Sum(c => c.Snowfall[x, y]);
                    }
                }
                TotalSnowfallRange = new FloatRange(
                    (float)(precipitationMaps.Min(x => x.SnowfallRange.Min) * MaxSnowfall),
                    (float)(precipitationMaps.Average(x => x.SnowfallRange.Average) * MaxSnowfall),
                    (float)(precipitationMaps.Max(x => x.SnowfallRange.Max) * MaxSnowfall));
            }
        }

        /// <summary>
        /// Gets the amount of precipitation at the given position at the given proportion of the
        /// year, as a value between 0 and 1, with 1 indicating the maximum potential precipitation
        /// of the planet's atmosphere. The amount is over a period of time dictated by the number
        /// of <see cref="Seasons"/>, with the midpoint at the time specified by <paramref
        /// name="proportionOfYear"/>.
        /// <seealso cref="MaxPrecipitation"/>
        /// </summary>
        /// <param name="x">The x coordinate of the position to check.</param>
        /// <param name="y">The y coordinate of the position to check.</param>
        /// <param name="proportionOfYear">The proportion of a year at which to check.</param>
        /// <returns>A value between 0 and 1, with 1 indicating the maximum potential precipitation
        /// of the planet's atmosphere.</returns>
        public float GetNormalizedPrecipitation(int x, int y, double proportionOfYear)
            => InterpolateAmongWeatherMaps(proportionOfYear, map => map.Precipitation[x, y]);

        /// <summary>
        /// Gets the amount of snowfall at the given position at the given proportion of the year,
        /// as a value between 0 and 1, with 1 indicating the maximum potential snowfall of the
        /// planet's atmosphere. The amount is over a period of time dictated by the number
        /// of <see cref="Seasons"/>, with the midpoint at the time specified by <paramref
        /// name="proportionOfYear"/>.
        /// <seealso cref="MaxSnowfall"/>
        /// </summary>
        /// <param name="x">The x coordinate of the position to check.</param>
        /// <param name="y">The y coordinate of the position to check.</param>
        /// <param name="proportionOfYear">The proportion of a year at which to check.</param>
        /// <returns>A value between 0 and 1, with 1 indicating the maximum potential snowfall of
        /// the planet's atmosphere.</returns>
        public float GetNormalizedSnowfall(int x, int y, double proportionOfYear)
            => InterpolateAmongWeatherMaps(proportionOfYear, map => map.Snowfall[x, y]);

        /// <summary>
        /// Gets the temperature at the given position at the given proportion of the year, as a
        /// value between 0 and 1, with 1 indicating the maximum temperature of the planet.
        /// <seealso cref="MaxTemperature"/>
        /// </summary>
        /// <param name="x">The x coordinate of the position to check.</param>
        /// <param name="y">The y coordinate of the position to check.</param>
        /// <param name="proportionOfYear">The proportion of a year at which to check.</param>
        /// <returns>The temperature at the given position at the given proportion of the year, as a
        /// value between 0 and 1, with 1 indicating the maximum temperature of the
        /// planet.</returns>
        public double GetNormalizedTemperature(int x, int y, double proportionOfYear)
            => GetTemperature(x, y, proportionOfYear) / MaxTemperature;

        /// <summary>
        /// Gets the amount of precipitation at the given position at the given proportion of the
        /// year, in mm. The amount is over a period of time dictated by the number of <see
        /// cref="Seasons"/>, with the midpoint at the time specified by <paramref
        /// name="proportionOfYear"/>.
        /// </summary>
        /// <param name="x">The x coordinate of the position to check.</param>
        /// <param name="y">The y coordinate of the position to check.</param>
        /// <param name="proportionOfYear">The proportion of a year at which to check.</param>
        /// <returns>The amount of precipitation at the given position at the given proportion of
        /// the year, in mm.</returns>
        public double GetPrecipitation(int x, int y, double proportionOfYear)
            => GetNormalizedPrecipitation(x, y, proportionOfYear) * MaxPrecipitation;

        /// <summary>
        /// Gets the precipitation range over this area at the given proportion of the year, in mm.
        /// </summary>
        /// <param name="proportionOfYear">The proportion of a year at which to check.</param>
        /// <returns>The precipitation range over this area at the given proportion of the year, in
        /// mm.</returns>
        public FloatRange GetPrecipitationRange(double proportionOfYear)
        {
            if (Seasons == 0)
            {
                return FloatRange.Zero;
            }
            return new FloatRange(
                (float)(InterpolateAmongWeatherMaps(proportionOfYear, map => map.PrecipitationRange.Min) * MaxPrecipitation),
                (float)(InterpolateAmongWeatherMaps(proportionOfYear, map => map.PrecipitationRange.Average) * MaxPrecipitation),
                (float)(InterpolateAmongWeatherMaps(proportionOfYear, map => map.PrecipitationRange.Max) * MaxPrecipitation));
        }

        /// <summary>
        /// Determines whether the given position has sea ice at the given proportion of the year.
        /// </summary>
        /// <param name="x">The x coordinate of the position to check.</param>
        /// <param name="y">The y coordinate of the position to check.</param>
        /// <param name="proportionOfYear">The proportion of a year at which to
        /// check.</param>
        /// <returns><see langword="true"/> if there is sea ice; otherwise <see
        /// langword="false"/>.</returns>
        public bool GetSeaIce(int x, int y, double proportionOfYear)
            => !SeaIceRanges[x, y].IsZero
            && (SeaIceRanges[x, y].Min > SeaIceRanges[x, y].Max
                ? proportionOfYear >= SeaIceRanges[x, y].Min || proportionOfYear <= SeaIceRanges[x, y].Max
                : proportionOfYear >= SeaIceRanges[x, y].Min && proportionOfYear <= SeaIceRanges[x, y].Max);

        /// <summary>
        /// Determines whether the given position has snow cover at the given proportion of the year.
        /// </summary>
        /// <param name="x">The x coordinate of the position to check.</param>
        /// <param name="y">The y coordinate of the position to check.</param>
        /// <param name="proportionOfYear">The proportion of a year at which to
        /// check.</param>
        /// <returns><see langword="true"/> if there is snow cover; otherwise <see
        /// langword="false"/>.</returns>
        public bool GetSnowCover(int x, int y, double proportionOfYear)
            => !SnowCoverRanges[x, y].IsZero
            && (SnowCoverRanges[x, y].Min > SnowCoverRanges[x, y].Max
                ? proportionOfYear >= SnowCoverRanges[x, y].Min || proportionOfYear <= SnowCoverRanges[x, y].Max
                : proportionOfYear >= SnowCoverRanges[x, y].Min && proportionOfYear <= SnowCoverRanges[x, y].Max);

        /// <summary>
        /// Gets the amount of snowfall at the given position at the given proportion of the
        /// year, in mm. The amount is over a period of time dictated by the number of <see
        /// cref="Seasons"/>, with the midpoint at the time specified by <paramref
        /// name="proportionOfYear"/>.
        /// </summary>
        /// <param name="x">The x coordinate of the position to check.</param>
        /// <param name="y">The y coordinate of the position to check.</param>
        /// <param name="proportionOfYear">The proportion of a year at which to check.</param>
        /// <returns>The amount of snowfall at the given position at the given proportion of
        /// the year, in mm.</returns>
        public double GetSnowfall(int x, int y, double proportionOfYear)
            => GetNormalizedSnowfall(x, y, proportionOfYear) * MaxSnowfall;

        /// <summary>
        /// Gets the snowfall range over this area at the given proportion of the year, in mm.
        /// </summary>
        /// <param name="proportionOfYear">The proportion of a year at which to check.</param>
        /// <returns>The snowfall range over this area at the given proportion of the year, in
        /// mm.</returns>
        public FloatRange GetSnowfallRange(double proportionOfYear)
        {
            if (Seasons == 0)
            {
                return FloatRange.Zero;
            }
            return new FloatRange(
                (float)(InterpolateAmongWeatherMaps(proportionOfYear, map => map.SnowfallRange.Min) * MaxPrecipitation),
                (float)(InterpolateAmongWeatherMaps(proportionOfYear, map => map.SnowfallRange.Average) * MaxPrecipitation),
                (float)(InterpolateAmongWeatherMaps(proportionOfYear, map => map.SnowfallRange.Max) * MaxPrecipitation));
        }

        /// <summary>
        /// Gets the temperature at the given position at the given proportion of the year, in K.
        /// </summary>
        /// <param name="x">The x coordinate of the position to check.</param>
        /// <param name="y">The y coordinate of the position to check.</param>
        /// <param name="proportionOfYear">The proportion of a year at which to check.</param>
        /// <returns>The temperature at the given position at the given proportion of the year, in
        /// K.</returns>
        public double GetTemperature(int x, int y, double proportionOfYear)
            => MathUtility.Lerp(TemperatureRanges[x, y].Min, TemperatureRanges[x, y].Max, GetProportionOfSummerFromYear(proportionOfYear));

        /// <summary>
        /// Gets the temperature range over this area at the given proportion of the year, in K.
        /// </summary>
        /// <param name="proportionOfYear">The proportion of a year at which to check.</param>
        /// <returns>The temperature range over this area at the given proportion of the year, in
        /// K.</returns>
        public FloatRange GetTemperatureRange(double proportionOfYear)
        {
            var xLength = TemperatureRanges.GetLength(0);
            var yLength = TemperatureRanges.GetLength(1);
            var min = 2.0;
            var max = -2.0;
            var sum = 0.0;
            for (var x = 0; x < xLength; x++)
            {
                for (var y = 0; y < yLength; y++)
                {
                    var t = MathUtility.Lerp(TemperatureRanges[x, y].Min, TemperatureRanges[x, y].Max, GetProportionOfSummerFromYear(proportionOfYear));
                    min = Math.Min(min, t);
                    max = Math.Max(max, t);
                    sum += t;
                }
            }
            return new FloatRange((float)min, (float)(sum / (xLength * yLength)), (float)max);
        }

        private double GetProportionOfSummerFromYear(double proportionOfYear) => 1 - (Math.Abs(0.5 - proportionOfYear) / 0.5);

        private float InterpolateAmongWeatherMaps(double proportionOfYear, Func<PrecipitationMaps, float> getValueFromMap)
        {
            if (Seasons == 0)
            {
                return 0;
            }
            var proportionPerSeason = 1.0 / PrecipitationMaps.Length;
            var seasonIndex = (int)Math.Floor(proportionOfYear / proportionPerSeason);
            var nextSeasonIndex = seasonIndex == PrecipitationMaps.Length - 1 ? 0 : seasonIndex + 1;
            var weight = (proportionOfYear - (seasonIndex * proportionPerSeason)) / proportionPerSeason;
            return (float)MathUtility.Lerp(getValueFromMap.Invoke(PrecipitationMaps[seasonIndex]), getValueFromMap.Invoke(PrecipitationMaps[nextSeasonIndex]), weight);
        }
    }
}
