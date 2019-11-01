using NeverFoundry.MathAndScience;
using System;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Permissions;
using NeverFoundry.WorldFoundry.CelestialBodies.Planetoids;
using NeverFoundry.WorldFoundry.Climate;
using NeverFoundry.MathAndScience.Numerics;

namespace NeverFoundry.WorldFoundry.SurfaceMapping
{
    /// <summary>
    /// A collection of weather maps providing yearlong climate data.
    /// </summary>
    [Serializable]
    public struct WeatherMaps : ISerializable
    {
        /// <summary>
        /// The overall <see cref="BiomeType"/> of the area.
        /// </summary>
        public BiomeType Biome { get; }

        /// <summary>
        /// A two-dimensional array corresponding to points on an equirectangular projected map of a
        /// terrestrial planet's surface. The first index corresponds to the X coordinate, and the
        /// second index corresponds to the Y coordinate. The values represent <see
        /// cref="BiomeType"/>.
        /// </summary>
        public BiomeType[,] BiomeMap { get; }

        /// <summary>
        /// The overall <see cref="ClimateType"/> of the area, based on average annual temperature.
        /// </summary>
        public ClimateType Climate { get; }

        /// <summary>
        /// A two-dimensional array corresponding to points on an equirectangular projected map of a
        /// terrestrial planet's surface. The first index corresponds to the X coordinate, and the
        /// second index corresponds to the Y coordinate. The values represent <see
        /// cref="ClimateType"/>, based on average annual temperature.
        /// </summary>
        public ClimateType[,] ClimateMap { get; }

        /// <summary>
        /// The overall <see cref="EcologyType"/> of the area.
        /// </summary>
        public EcologyType Ecology { get; }

        /// <summary>
        /// A two-dimensional array corresponding to points on an equirectangular projected map of a
        /// terrestrial planet's surface. The first index corresponds to the X coordinate, and the
        /// second index corresponds to the Y coordinate. The values represent <see
        /// cref="EcologyType"/>.
        /// </summary>
        public EcologyType[,] EcologyMap { get; }

        /// <summary>
        /// The overall <see cref="HumidityType"/> of the area, based on annual precipitation.
        /// </summary>
        public HumidityType Humidity { get; }

        /// <summary>
        /// A two-dimensional array corresponding to points on an equirectangular projected map of a
        /// terrestrial planet's surface. The first index corresponds to the X coordinate, and the
        /// second index corresponds to the Y coordinate. The values represent <see
        /// cref="HumidityType"/>, based on annual precipitation.
        /// </summary>
        public HumidityType[,] HumidityMap { get; }

        /// <summary>
        /// A collection of <see cref="PrecipitationMaps"/>.
        /// </summary>
        public PrecipitationMaps[] PrecipitationMaps { get; }

        /// <summary>
        /// A two-dimensional array corresponding to points on an equirectangular projected map of a
        /// terrestrial planet's surface. The first index corresponds to the X coordinate, and the
        /// second index corresponds to the Y coordinate. The values represent the proportion of the
        /// year during which there is persistent sea ice.
        /// </summary>
        public FloatRange[,] SeaIceRangeMap { get; }

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
        public FloatRange[,] SnowCoverRangeMap { get; }

        /// <summary>
        /// A range giving the minimum, maximum, and average temperature throughout the specified
        /// area over the full year, in K.
        /// </summary>
        public FloatRange TemperatureRange { get; }

        /// <summary>
        /// A two-dimensional array corresponding to points on an equirectangular projected map of a
        /// terrestrial planet's surface. The first index corresponds to the X coordinate, and the
        /// second index corresponds to the Y coordinate. The values represent the temperature
        /// range, in K.
        /// </summary>
        public FloatRange[,] TemperatureRangeMap { get; }

        /// <summary>
        /// A range giving the minimum, maximum, and average precipitation throughout the specified
        /// area over the entire year, in mm.
        /// </summary>
        public FloatRange TotalPrecipitation { get; }

        /// <summary>
        /// A two-dimensional array corresponding to points on an equirectangular projected map of a
        /// terrestrial planet's surface. The first index corresponds to the X coordinate, and the
        /// second index corresponds to the Y coordinate. The values represent the total annual
        /// precipitation, as values between 0 and 1, with 1 indicating the maximum annual potential
        /// precipitation of the planet's atmosphere. Will be <see langword="null"/> if no <see
        /// cref="PrecipitationMaps"/> are present.
        /// </summary>
        public float[,] TotalPrecipitationMap { get; }

        /// <summary>
        /// A two-dimensional array corresponding to points on an equirectangular projected map of a
        /// terrestrial planet's surface. The first index corresponds to the X coordinate, and the
        /// second index corresponds to the Y coordinate. The values represent the total annual
        /// snowfall, as values between 0 and 1, with 1 indicating the maximum annual potential
        /// snowfall of the planet's atmosphere. Will be <see langword="null"/> if no <see
        /// cref="PrecipitationMaps"/> are present.
        /// </summary>
        public float[,] TotalSnowfallMap { get; }

        /// <summary>
        /// A range giving the minimum, maximum, and average snowfall throughout the specified area
        /// over the entire year, in mm.
        /// </summary>
        public FloatRange TotalSnowfall { get; }

        /// <summary>
        /// The length of the "X" (0-index) dimension of the maps.
        /// </summary>
        public int XLength { get; }

        /// <summary>
        /// The length of the "Y" (1-index) dimension of the maps.
        /// </summary>
        public int YLength { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="WeatherMaps"/>.
        /// </summary>
        /// <param name="xLength">The length of the "X" (0-index) dimension of the maps.</param>
        /// <param name="yLength">The length of the "Y" (1-index) dimension of the maps.</param>
        /// <param name="biome">A biome.</param>
        /// <param name="biomeMap">A biome map.</param>
        /// <param name="climate">A climate.</param>
        /// <param name="climateMap">A climate map.</param>
        /// <param name="ecology">An ecology.</param>
        /// <param name="ecologyMap">An ecology map.</param>
        /// <param name="humidity">A humidity.</param>
        /// <param name="humidityMap">A humidity map.</param>
        /// <param name="totalPrecipitation">A range giving the minimum, maximum, and average
        /// precipitation throughout the specified area over the entire year, in mm.</param>
        /// <param name="totalSnowfall">A range giving the minimum, maximum, and average snowfall
        /// throughout the specified area over the entire year, in mm.</param>
        /// <param name="seaIceRanges">A sea ice range map.</param>
        /// <param name="snowCoverRanges">A snow cover range map.</param>
        /// <param name="precipitationMaps">A set of weather maps.</param>
        /// <param name="temperatureRanges">A temperature range map.</param>
        /// <param name="totalPrecipitationMap">A total precipitation map.</param>
        public WeatherMaps(
            int xLength,
            int yLength,
            BiomeType biome,
            BiomeType[,] biomeMap,
            ClimateType climate,
            ClimateType[,] climateMap,
            EcologyType ecology,
            EcologyType[,] ecologyMap,
            HumidityType humidity,
            HumidityType[,] humidityMap,
            FloatRange totalPrecipitation,
            FloatRange totalSnowfall,
            FloatRange[,] seaIceRanges,
            FloatRange[,] snowCoverRanges,
            PrecipitationMaps[] precipitationMaps,
            FloatRange[,] temperatureRanges,
            float[,] totalPrecipitationMap)
        {
            if (biomeMap.GetLength(0) != xLength)
            {
                throw new ArgumentException($"Length of {nameof(biomeMap)} was not equal to {nameof(xLength)}", nameof(xLength));
            }
            if (biomeMap.GetLength(1) != yLength)
            {
                throw new ArgumentException($"Length of {nameof(biomeMap)} was not equal to {nameof(yLength)}", nameof(yLength));
            }
            if (climateMap.GetLength(0) != xLength)
            {
                throw new ArgumentException($"Length of {nameof(climateMap)} was not equal to {nameof(xLength)}", nameof(xLength));
            }
            if (climateMap.GetLength(1) != yLength)
            {
                throw new ArgumentException($"Length of {nameof(climateMap)} was not equal to {nameof(yLength)}", nameof(yLength));
            }
            if (ecologyMap.GetLength(0) != xLength)
            {
                throw new ArgumentException($"Length of {nameof(ecologyMap)} was not equal to {nameof(xLength)}", nameof(xLength));
            }
            if (ecologyMap.GetLength(1) != yLength)
            {
                throw new ArgumentException($"Length of {nameof(ecologyMap)} was not equal to {nameof(yLength)}", nameof(yLength));
            }
            if (humidityMap.GetLength(0) != xLength)
            {
                throw new ArgumentException($"Length of {nameof(humidityMap)} was not equal to {nameof(xLength)}", nameof(xLength));
            }
            if (humidityMap.GetLength(1) != yLength)
            {
                throw new ArgumentException($"Length of {nameof(humidityMap)} was not equal to {nameof(yLength)}", nameof(yLength));
            }
            if (seaIceRanges.GetLength(0) != xLength)
            {
                throw new ArgumentException($"Length of {nameof(seaIceRanges)} was not equal to {nameof(xLength)}", nameof(xLength));
            }
            if (seaIceRanges.GetLength(1) != yLength)
            {
                throw new ArgumentException($"Length of {nameof(seaIceRanges)} was not equal to {nameof(yLength)}", nameof(yLength));
            }
            if (snowCoverRanges.GetLength(0) != xLength)
            {
                throw new ArgumentException($"Length of {nameof(snowCoverRanges)} was not equal to {nameof(xLength)}", nameof(xLength));
            }
            if (snowCoverRanges.GetLength(1) != yLength)
            {
                throw new ArgumentException($"Length of {nameof(snowCoverRanges)} was not equal to {nameof(yLength)}", nameof(yLength));
            }
            if (precipitationMaps.Length > 0 && precipitationMaps[0].XLength != xLength)
            {
                throw new ArgumentException($"{nameof(SurfaceMapping.PrecipitationMaps.XLength)} of {nameof(precipitationMaps)} was not equal to {nameof(xLength)}", nameof(xLength));
            }
            if (precipitationMaps.Length > 0 && precipitationMaps[0].YLength != yLength)
            {
                throw new ArgumentException($"{nameof(SurfaceMapping.PrecipitationMaps.YLength)} of {nameof(precipitationMaps)} was not equal to {nameof(yLength)}", nameof(yLength));
            }
            if (temperatureRanges.GetLength(0) != xLength)
            {
                throw new ArgumentException($"Length of {nameof(temperatureRanges)} was not equal to {nameof(xLength)}", nameof(xLength));
            }
            if (temperatureRanges.GetLength(1) != yLength)
            {
                throw new ArgumentException($"Length of {nameof(temperatureRanges)} was not equal to {nameof(yLength)}", nameof(yLength));
            }
            if (totalPrecipitationMap.GetLength(0) != xLength)
            {
                throw new ArgumentException($"Length of {nameof(totalPrecipitationMap)} was not equal to {nameof(xLength)}", nameof(xLength));
            }
            if (totalPrecipitationMap.GetLength(1) != yLength)
            {
                throw new ArgumentException($"Length of {nameof(totalPrecipitationMap)} was not equal to {nameof(yLength)}", nameof(yLength));
            }

            XLength = xLength;
            YLength = yLength;

            Biome = biome;
            BiomeMap = biomeMap;
            Climate = climate;
            ClimateMap = climateMap;
            Ecology = ecology;
            EcologyMap = ecologyMap;
            Humidity = humidity;
            HumidityMap = humidityMap;
            TotalPrecipitation = totalPrecipitation;
            TotalSnowfall = totalSnowfall;
            SeaIceRangeMap = seaIceRanges;
            SnowCoverRangeMap = snowCoverRanges;
            PrecipitationMaps = precipitationMaps;
            TemperatureRangeMap = temperatureRanges;
            TotalPrecipitationMap = totalPrecipitationMap;

            Seasons = precipitationMaps.Length;

            if (TemperatureRangeMap == null)
            {
                TemperatureRange = FloatRange.Zero;
            }
            else
            {
                var min = 2f;
                var max = -2f;
                var avgSum = 0f;
                for (var x = 0; x < xLength; x++)
                {
                    for (var y = 0; y < yLength; y++)
                    {
                        min = Math.Min(min, TemperatureRangeMap[x, y].Min);
                        max = Math.Max(max, TemperatureRangeMap[x, y].Max);
                        avgSum += TemperatureRangeMap[x, y].Average;
                    }
                }
                TemperatureRange = new FloatRange(min, avgSum / (xLength * yLength), max);
            }

            if (precipitationMaps.Length == 0)
            {
                TotalSnowfallMap = SurfaceMap.GetInitializedArray(xLength, yLength, 0f);
            }
            else
            {
                TotalSnowfallMap = new float[xLength, yLength];
                for (var x = 0; x < xLength; x++)
                {
                    for (var y = 0; y < yLength; y++)
                    {
                        TotalSnowfallMap[x, y] = precipitationMaps.Sum(c => c.SnowfallMap[x, y]);
                    }
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of <see cref="WeatherMaps"/>.
        /// </summary>
        /// <param name="planet">The planet being mapped.</param>
        /// <param name="xLength">The length of the "X" (0-index) dimension of the maps.</param>
        /// <param name="yLength">The length of the "Y" (1-index) dimension of the maps.</param>
        /// <param name="elevationMap">An elevation map.</param>
        /// <param name="precipitationMaps">A set of weather maps.</param>
        /// <param name="temperatureRanges">A temperature range map.</param>
        /// <param name="resolution">The vertical resolution of the projection.</param>
        /// <param name="centralMeridian">The longitude of the central meridian of the projection,
        /// in radians.</param>
        /// <param name="centralParallel">The latitude of the central parallel of the projection, in
        /// radians.</param>
        /// <param name="standardParallels">The latitude of the standard parallels (north and south
        /// of the equator) where the scale of the projection is 1:1, in radians. Zero indicates the
        /// equator (the plate carrée projection). It does not matter whether the positive or
        /// negative latitude is provided, if it is non-zero. If <see langword="null"/>, the
        /// <paramref name="centralParallel"/> will be used.</param>
        /// <param name="range">If provided, indicates the latitude range (north and south of
        /// <paramref name="centralParallel"/>) shown on the projection, in radians. If not
        /// provided, or if equal to zero or greater than π, indicates that the entire globe is
        /// shown.</param>
        /// <param name="averageElevation">The average elevation of the area. If left <see
        /// langword="null"/> it will be calculated.</param>
        public WeatherMaps(
            Planetoid planet,
            int xLength,
            int yLength,
            double[,] elevationMap,
            PrecipitationMaps[] precipitationMaps,
            FloatRange[,] temperatureRanges,
            int resolution,
            double? centralMeridian = null,
            double? centralParallel = null,
            double? standardParallels = null,
            double? range = null,
            double? averageElevation = null)
        {
            if (elevationMap.GetLength(0) != xLength)
            {
                throw new ArgumentException($"Length of {nameof(elevationMap)} was not equal to {nameof(xLength)}", nameof(xLength));
            }
            if (elevationMap.GetLength(1) != yLength)
            {
                throw new ArgumentException($"Length of {nameof(elevationMap)} was not equal to {nameof(yLength)}", nameof(yLength));
            }
            if (precipitationMaps.Length > 0 && precipitationMaps[0].XLength != xLength)
            {
                throw new ArgumentException($"{nameof(SurfaceMapping.PrecipitationMaps.XLength)} of {nameof(precipitationMaps)} was not equal to {nameof(xLength)}", nameof(xLength));
            }
            if (precipitationMaps.Length > 0 && precipitationMaps[0].YLength != yLength)
            {
                throw new ArgumentException($"{nameof(SurfaceMapping.PrecipitationMaps.YLength)} of {nameof(precipitationMaps)} was not equal to {nameof(yLength)}", nameof(yLength));
            }
            if (temperatureRanges.GetLength(0) != xLength)
            {
                throw new ArgumentException($"Length of {nameof(temperatureRanges)} was not equal to {nameof(xLength)}", nameof(xLength));
            }
            if (temperatureRanges.GetLength(1) != yLength)
            {
                throw new ArgumentException($"Length of {nameof(temperatureRanges)} was not equal to {nameof(yLength)}", nameof(yLength));
            }

            XLength = xLength;
            YLength = yLength;

            double avgElevation;
            if (averageElevation.HasValue)
            {
                avgElevation = averageElevation.Value * planet.MaxElevation;
            }
            else
            {
                avgElevation = 0;
                var totalElevation = 0.0;
                for (var x = 0; x < xLength; x++)
                {
                    for (var y = 0; y < yLength; y++)
                    {
                        totalElevation += elevationMap[x, y];
                    }
                }
                avgElevation = totalElevation * planet.MaxElevation / (xLength * yLength);
            }

            PrecipitationMaps = precipitationMaps;
            TemperatureRangeMap = temperatureRanges;

            Seasons = precipitationMaps?.Length ?? 0;

            var min = float.MaxValue;
            var max = float.MinValue;
            var avgSum = 0f;
            for (var x = 0; x < xLength; x++)
            {
                for (var y = 0; y < yLength; y++)
                {
                    min = Math.Min(min, temperatureRanges[x, y].Min);
                    max = Math.Max(max, temperatureRanges[x, y].Max);
                    avgSum += temperatureRanges[x, y].Average;
                }
            }
            TemperatureRange = new FloatRange(min, avgSum / (xLength * yLength), max);

            BiomeMap = new BiomeType[xLength, yLength];
            ClimateMap = new ClimateType[xLength, yLength];
            EcologyMap = new EcologyType[xLength, yLength];
            var humidityMap = new HumidityType[xLength, yLength];
            TotalPrecipitationMap = new float[xLength, yLength];

            if ((precipitationMaps?.Length ?? 0) == 0)
            {
                Biome = BiomeType.None;
                BiomeMap = SurfaceMap.GetInitializedArray(xLength, yLength, BiomeType.None);
                Climate = ClimateType.None;
                ClimateMap = SurfaceMap.GetInitializedArray(xLength, yLength, ClimateType.None);
                Ecology = EcologyType.None;
                EcologyMap = SurfaceMap.GetInitializedArray(xLength, yLength, EcologyType.None);
                Humidity = HumidityType.None;
                HumidityMap = SurfaceMap.GetInitializedArray(xLength, yLength, HumidityType.None);
                SeaIceRangeMap = SurfaceMap.GetInitializedArray(xLength, yLength, FloatRange.Zero);
                SnowCoverRangeMap = SurfaceMap.GetInitializedArray(xLength, yLength, FloatRange.Zero);
                TotalPrecipitationMap = SurfaceMap.GetInitializedArray(xLength, yLength, 0f);
                TotalPrecipitation = FloatRange.Zero;
                TotalSnowfallMap = SurfaceMap.GetInitializedArray(xLength, yLength, 0f);
                TotalSnowfall = FloatRange.Zero;
            }
            else
            {
                for (var x = 0; x < xLength; x++)
                {
                    for (var y = 0; y < yLength; y++)
                    {
                        TotalPrecipitationMap[x, y] = precipitationMaps.Sum(c => c.PrecipitationMap[x, y]);
                        ClimateMap[x, y] = ClimateTypes.GetClimateType(temperatureRanges[x, y].Average);
                        humidityMap[x, y] = ClimateTypes.GetHumidityType(TotalPrecipitationMap[x, y] * planet.Atmosphere.MaxPrecipitation);
                        BiomeMap[x, y] = ClimateTypes.GetBiomeType(ClimateMap[x, y], humidityMap[x, y], elevationMap[x, y]);
                        EcologyMap[x, y] = ClimateTypes.GetEcologyType(ClimateMap[x, y], humidityMap[x, y], elevationMap[x, y]);
                    }
                }

                TotalPrecipitation = new FloatRange(
                    (float)(precipitationMaps.Min(x => x.Precipitation.Min) * planet.Atmosphere.MaxPrecipitation),
                    (float)(precipitationMaps.Average(x => x.Precipitation.Average) * planet.Atmosphere.MaxPrecipitation),
                    (float)(precipitationMaps.Max(x => x.Precipitation.Max) * planet.Atmosphere.MaxPrecipitation));
                Climate = ClimateTypes.GetClimateType(TemperatureRange.Average);
                Humidity = ClimateTypes.GetHumidityType(TotalPrecipitation.Average * planet.Atmosphere.MaxPrecipitation);
                Biome = ClimateTypes.GetBiomeType(Climate, Humidity, avgElevation);
                Ecology = ClimateTypes.GetEcologyType(Climate, Humidity, avgElevation);

                TotalSnowfallMap = new float[xLength, yLength];
                for (var x = 0; x < xLength; x++)
                {
                    for (var y = 0; y < yLength; y++)
                    {
                        TotalSnowfallMap[x, y] = precipitationMaps.Sum(c => c.SnowfallMap[x, y]);
                    }
                }
                TotalSnowfall = new FloatRange(
                    (float)(precipitationMaps.Min(x => x.Snowfall.Min) * planet.Atmosphere.MaxSnowfall),
                    (float)(precipitationMaps.Average(x => x.Snowfall.Average) * planet.Atmosphere.MaxSnowfall),
                    (float)(precipitationMaps.Max(x => x.Snowfall.Max) * planet.Atmosphere.MaxSnowfall));

                if (temperatureRanges == null)
                {
                    SeaIceRangeMap = SurfaceMap.GetInitializedArray(xLength, yLength, FloatRange.Zero);
                    SnowCoverRangeMap = SurfaceMap.GetInitializedArray(xLength, yLength, FloatRange.Zero);
                }
                else
                {
                    SeaIceRangeMap = SurfaceMap.GetSurfaceMap(
                        (lat, _, x, y) =>
                        {
                            if (elevationMap[x, y] > 0 || temperatureRanges[x, y].Min > CelestialSubstances.SeawaterMeltingPoint)
                            {
                                return FloatRange.Zero;
                            }
                            if (temperatureRanges[x, y].Max < CelestialSubstances.SeawaterMeltingPoint)
                            {
                                return FloatRange.ZeroToOne;
                            }

                            var freezeProportion = temperatureRanges[x, y].Min.InverseLerp(temperatureRanges[x, y].Max, (float)CelestialSubstances.SeawaterMeltingPoint);
                            if (float.IsNaN(freezeProportion))
                            {
                                return FloatRange.Zero;
                            }
                            // Freezes more than melts; never fully melts.
                            if (freezeProportion >= 0.5f)
                            {
                                return FloatRange.ZeroToOne;
                            }

                            var meltStart = freezeProportion / 2;
                            var iceMeltFinish = freezeProportion;
                            var snowMeltFinish = freezeProportion * 3 / 4;
                            var freezeStart = 1 - (freezeProportion / 2);
                            if (lat < 0)
                            {
                                iceMeltFinish += 0.5f;
                                if (iceMeltFinish > 1)
                                {
                                    iceMeltFinish--;
                                }

                                snowMeltFinish += 0.5f;
                                if (snowMeltFinish > 1)
                                {
                                    snowMeltFinish--;
                                }

                                freezeStart -= 0.5f;
                            }
                            return new FloatRange(freezeStart, iceMeltFinish);
                        },
                        resolution,
                        centralMeridian,
                        centralParallel,
                        standardParallels,
                        range);
                    SnowCoverRangeMap = SurfaceMap.GetSurfaceMap(
                        (lat, _, x, y) =>
                        {
                            if (elevationMap[x, y] <= 0
                                || humidityMap[x, y] <= HumidityType.Perarid
                                || temperatureRanges[x, y].Min > CelestialSubstances.SeawaterMeltingPoint)
                            {
                                return FloatRange.Zero;
                            }
                            if (temperatureRanges[x, y].Max < CelestialSubstances.SeawaterMeltingPoint)
                            {
                                return FloatRange.ZeroToOne;
                            }

                            var freezeProportion = temperatureRanges[x, y].Min.InverseLerp(temperatureRanges[x, y].Max, (float)CelestialSubstances.SeawaterMeltingPoint);
                            if (float.IsNaN(freezeProportion))
                            {
                                return FloatRange.Zero;
                            }
                            // Freezes more than melts; never fully melts.
                            if (freezeProportion >= 0.5f)
                            {
                                return FloatRange.ZeroToOne;
                            }

                            var meltStart = freezeProportion / 2;
                            var iceMeltFinish = freezeProportion;
                            var snowMeltFinish = freezeProportion * 3 / 4;
                            var freezeStart = 1 - (freezeProportion / 2);
                            if (lat < 0)
                            {
                                iceMeltFinish += 0.5f;
                                if (iceMeltFinish > 1)
                                {
                                    iceMeltFinish--;
                                }

                                snowMeltFinish += 0.5f;
                                if (snowMeltFinish > 1)
                                {
                                    snowMeltFinish--;
                                }

                                freezeStart -= 0.5f;
                            }
                            return new FloatRange(freezeStart, snowMeltFinish);
                        },
                        resolution,
                        centralMeridian,
                        centralParallel,
                        standardParallels,
                        range);
                }
            }
            HumidityMap = humidityMap;
        }

        private WeatherMaps(SerializationInfo info, StreamingContext context) : this(
            (int)info.GetValue(nameof(XLength), typeof(int)),
            (int)info.GetValue(nameof(YLength), typeof(int)),
            (BiomeType)info.GetValue(nameof(Biome), typeof(BiomeType)),
            (BiomeType[,])info.GetValue(nameof(BiomeMap), typeof(BiomeType[,])),
            (ClimateType)info.GetValue(nameof(Climate), typeof(ClimateType)),
            (ClimateType[,])info.GetValue(nameof(ClimateMap), typeof(ClimateType[,])),
            (EcologyType)info.GetValue(nameof(Ecology), typeof(EcologyType)),
            (EcologyType[,])info.GetValue(nameof(EcologyMap), typeof(EcologyType[,])),
            (HumidityType)info.GetValue(nameof(Humidity), typeof(HumidityType)),
            (HumidityType[,])info.GetValue(nameof(HumidityMap), typeof(HumidityType[,])),
            (FloatRange)info.GetValue(nameof(TotalPrecipitation), typeof(FloatRange)),
            (FloatRange)info.GetValue(nameof(TotalSnowfall), typeof(FloatRange)),
            (FloatRange[,])info.GetValue(nameof(SeaIceRangeMap), typeof(FloatRange[,])),
            (FloatRange[,])info.GetValue(nameof(SnowCoverRangeMap), typeof(FloatRange[,])),
            (PrecipitationMaps[])info.GetValue(nameof(PrecipitationMaps), typeof(PrecipitationMaps[])),
            (FloatRange[,])info.GetValue(nameof(TemperatureRangeMap), typeof(FloatRange[,])),
            (float[,])info.GetValue(nameof(TotalPrecipitationMap), typeof(float[,]))) { }

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
            info.AddValue(nameof(XLength), XLength);
            info.AddValue(nameof(YLength), YLength);
            info.AddValue(nameof(Biome), Biome);
            info.AddValue(nameof(BiomeMap), BiomeMap);
            info.AddValue(nameof(Climate), Climate);
            info.AddValue(nameof(ClimateMap), ClimateMap);
            info.AddValue(nameof(Ecology), Ecology);
            info.AddValue(nameof(EcologyMap), EcologyMap);
            info.AddValue(nameof(Humidity), Humidity);
            info.AddValue(nameof(HumidityMap), HumidityMap);
            info.AddValue(nameof(TotalPrecipitation), TotalPrecipitation);
            info.AddValue(nameof(TotalSnowfall), TotalSnowfall);
            info.AddValue(nameof(SeaIceRangeMap), SeaIceRangeMap);
            info.AddValue(nameof(SnowCoverRangeMap), SnowCoverRangeMap);
            info.AddValue(nameof(PrecipitationMaps), PrecipitationMaps);
            info.AddValue(nameof(TemperatureRangeMap), TemperatureRangeMap);
            info.AddValue(nameof(TotalPrecipitationMap), TotalPrecipitationMap);
        }
    }
}
