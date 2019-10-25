using System;
using System.Runtime.Serialization;
using System.Security.Permissions;
using WorldFoundry.Climate;

namespace WorldFoundry.SurfaceMapping
{
    /// <summary>
    /// A collection of surface and weather maps for a <see
    /// cref="CelestialBodies.Planetoids.Planets.TerrestrialPlanets.TerrestrialPlanet"/>.
    /// </summary>
    [Serializable]
    public struct SurfaceMaps : ISerializable
    {
        private readonly HydrologyMaps _hydrologyMaps;
        private readonly WeatherMaps _weatherMaps;

        /// <summary>
        /// The average normalized elevation of the area, from -1 to 1, where negative values are
        /// below sea level and positive values are above sea level, and 1 is equal to the maximum
        /// elevation of the planet.
        /// <seealso cref="CelestialBodies.Planetoids.Planetoid.MaxElevation"/>
        /// </summary>
        public float AverageElevation { get; }

        /// <summary>
        /// The overall <see cref="BiomeType"/> of the area.
        /// </summary>
        public BiomeType Biome => _weatherMaps.Biome;

        /// <summary>
        /// A two-dimensional array corresponding to points on an equirectangular projected map of a
        /// terrestrial planet's surface. The first index corresponds to the X coordinate, and the
        /// second index corresponds to the Y coordinate. The values represent <see
        /// cref="BiomeType"/>.
        /// </summary>
        public BiomeType[,] BiomeMap => _weatherMaps.BiomeMap;

        /// <summary>
        /// The overall <see cref="ClimateType"/> of the area, based on average annual temperature.
        /// </summary>
        public ClimateType Climate => _weatherMaps.Climate;

        /// <summary>
        /// A two-dimensional array corresponding to points on an equirectangular projected map of a
        /// terrestrial planet's surface. The first index corresponds to the X coordinate, and the
        /// second index corresponds to the Y coordinate. The values represent <see
        /// cref="ClimateType"/>, based on average annual temperature.
        /// </summary>
        public ClimateType[,] ClimateMap => _weatherMaps.ClimateMap;

        /// <summary>
        /// A two-dimensional array corresponding to points on an equirectangular projected map of a
        /// terrestrial planet's surface. The first index corresponds to the X coordinate, and the
        /// second index corresponds to the Y coordinate. The values indicate the elevation of
        /// surface water in the location (i.e. lakes). Values range from 0 to 1, with 1 indicating
        /// the maximum elevation of the planet. Note that the values are depths above ground level,
        /// not absolute elevations above sea level. To obtain the absolute elevation of the water
        /// level at any point, the sum of this map and the elevation map can be taken.
        /// <seealso cref="CelestialBodies.Planetoids.Planetoid.MaxElevation"/>
        /// </summary>
        public float[,] Depth => _hydrologyMaps.Depth;

        /// <summary>
        /// The overall <see cref="EcologyType"/> of the area.
        /// </summary>
        public EcologyType Ecology => _weatherMaps.Ecology;

        /// <summary>
        /// A two-dimensional array corresponding to points on an equirectangular projected map of a
        /// terrestrial planet's surface. The first index corresponds to the X coordinate, and the
        /// second index corresponds to the Y coordinate. The values represent <see
        /// cref="EcologyType"/>.
        /// </summary>
        public EcologyType[,] EcologyMap => _weatherMaps.EcologyMap;

        /// <summary>
        /// A two-dimensional array corresponding to points on an equirectangular projected map of a
        /// terrestrial planet's surface. The first index corresponds to the X coordinate, and the
        /// second index corresponds to the Y coordinate. The values represent normalized elevations
        /// from -1 to 1, where negative values are below sea level and positive values are above
        /// sea level, and 1 is equal to the maximum elevation of the planet.
        /// <seealso cref="CelestialBodies.Planetoids.Planetoid.MaxElevation"/>
        /// </summary>
        public float[,] Elevation { get; }

        /// <summary>
        /// <para>
        /// A two-dimensional array corresponding to points on an equirectangular projected map of a
        /// terrestrial planet's surface. The first index corresponds to the X coordinate, and the
        /// second index corresponds to the Y coordinate. The values represent the flow rate of
        /// surface and/or ground water in that location, in m³/s. Note that the values are not
        /// normalized, unlike those of most surface maps.
        /// </para>
        /// <para>This map does not distinguish between smaller or larger flows (e.g. rivers vs.
        /// streams vs. seeping groundwater). A visualization tool which is displaying flow
        /// information should set a threshold above which flow is displayed, in order to cull the
        /// finer branches of the flow network. Generally speaking, the more distant the view, the
        /// higher the culling threshold should be in order to make flow look more like a map of
        /// distinct "rivers" and less like a dense mesh.
        /// </para>
        /// </summary>
        public float[,] Flow => _hydrologyMaps.Flow;

        /// <summary>
        /// The overall <see cref="HumidityType"/> of the area, based on annual precipitation.
        /// </summary>
        public HumidityType Humidity => _weatherMaps.Humidity;

        /// <summary>
        /// A two-dimensional array corresponding to points on an equirectangular projected map of a
        /// terrestrial planet's surface. The first index corresponds to the X coordinate, and the
        /// second index corresponds to the Y coordinate. The values represent <see
        /// cref="HumidityType"/>, based on annual precipitation.
        /// </summary>
        public HumidityType[,] HumidityMap => _weatherMaps.HumidityMap;

        /// <summary>
        /// A collection of <see cref="PrecipitationMaps"/>.
        /// </summary>
        public PrecipitationMaps[] PrecipitationMaps => _weatherMaps.PrecipitationMaps;

        /// <summary>
        /// A two-dimensional array corresponding to points on an equirectangular projected map of a
        /// terrestrial planet's surface. The first index corresponds to the X coordinate, and the
        /// second index corresponds to the Y coordinate. The values represent the proportion of the
        /// year during which there is persistent sea ice.
        /// </summary>
        public FloatRange[,] SeaIceRangeMap => _weatherMaps.SeaIceRangeMap;

        /// <summary>
        /// The number of seasons into which the year is divided by this set, for the purposes of
        /// precipitation and snowfall reporting.
        /// </summary>
        public int Seasons => _weatherMaps.Seasons;

        /// <summary>
        /// A two-dimensional array corresponding to points on an equirectangular projected map of a
        /// terrestrial planet's surface. The first index corresponds to the X coordinate, and the
        /// second index corresponds to the Y coordinate. The values represent the proportion of the
        /// year during which there is persistent snow cover.
        /// </summary>
        public FloatRange[,] SnowCoverRangeMap => _weatherMaps.SnowCoverRangeMap;

        /// <summary>
        /// A range giving the minimum, maximum, and average temperature throughout the specified
        /// area over the entire period represented by all <see cref="PrecipitationMaps"/>, as a value
        /// between 0 and 1, with 1 indicating the maximum temperature of the planet.
        /// <seealso cref="CelestialBodies.Planetoids.Planetoid.MaxSurfaceTemperature"/>
        /// </summary>
        public FloatRange TemperatureRange => _weatherMaps.TemperatureRange;

        /// <summary>
        /// A two-dimensional array corresponding to points on an equirectangular projected map of a
        /// terrestrial planet's surface. The first index corresponds to the X coordinate, and the
        /// second index corresponds to the Y coordinate. The values represent the temperature
        /// range. Values range from 0 to 1, with 1 indicating the maximum temperature of the
        /// planet.
        /// <seealso cref="CelestialBodies.Planetoids.Planetoid.MaxSurfaceTemperature"/>
        /// </summary>
        public FloatRange[,] TemperatureRangeMap => _weatherMaps.TemperatureRangeMap;

        /// <summary>
        /// A range giving the minimum, maximum, and average precipitation throughout the specified
        /// area over the entire period represented by all <see cref="PrecipitationMaps"/>, as a value
        /// between 0 and 1, with 1 indicating the maximum annual potential precipitation of the
        /// planet's atmosphere.
        /// <seealso cref="Atmosphere.MaxPrecipitation"/>
        /// </summary>
        public FloatRange TotalPrecipitation => _weatherMaps.TotalPrecipitation;

        /// <summary>
        /// A two-dimensional array corresponding to points on an equirectangular projected map of a
        /// terrestrial planet's surface. The first index corresponds to the X coordinate, and the
        /// second index corresponds to the Y coordinate. The values represent the total amount of
        /// precipitation indicated on all contained <see cref="PrecipitationMaps"/>. Values range from 0
        /// to 1, with 1 indicating the maximum annual potential precipitation of the planet's
        /// atmosphere. Will be <see langword="null"/> if no <see cref="PrecipitationMaps"/> are present.
        /// <seealso cref="Atmosphere.MaxPrecipitation"/>
        /// </summary>
        public float[,] TotalPrecipitationMap => _weatherMaps.TotalPrecipitationMap;

        /// <summary>
        /// A range giving the minimum, maximum, and average snowfall throughout the specified area
        /// over the entire period represented by all <see cref="PrecipitationMaps"/>, as a value
        /// between 0 and 1, with 1 indicating the maximum annual potential snowfall of the planet's
        /// atmosphere.
        /// <seealso cref="Atmosphere.MaxSnowfall"/>
        /// </summary>
        public FloatRange TotalSnowfall => _weatherMaps.TotalSnowfall;

        /// <summary>
        /// A two-dimensional array corresponding to points on an equirectangular projected map of a
        /// terrestrial planet's surface. The first index corresponds to the X coordinate, and the
        /// second index corresponds to the Y coordinate. The values represent the total amount of
        /// snowfall indicated on all contained <see cref="PrecipitationMaps"/>. Values range from 0
        /// to 1, with 1 indicating the maximum annual potential snowfall of the planet's
        /// atmosphere. Will be <see langword="null"/> if no <see cref="PrecipitationMaps"/> are
        /// present.
        /// <seealso cref="Atmosphere.MaxSnowfall"/>
        /// </summary>
        public float[,] TotalSnowfallMap => _weatherMaps.TotalSnowfallMap;

        /// <summary>
        /// The length of the "X" (0-index) dimension of the maps.
        /// </summary>
        public int XLength { get; }

        /// <summary>
        /// The length of the "Y" (1-index) dimension of the maps.
        /// </summary>
        public int YLength { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="SurfaceMaps"/>.
        /// </summary>
        /// <param name="xLength">The length of the "X" (0-index) dimension of the maps.</param>
        /// <param name="yLength">The length of the "Y" (1-index) dimension of the maps.</param>
        /// <param name="elevation">An elevation map.</param>
        /// <param name="averageElevation"></param>
        /// <param name="weatherMaps">A <see cref="PrecipitationMaps"/> instance.</param>
        /// <param name="hydrologyMaps">A <see cref="HydrologyMaps"/> instance.</param>
        public SurfaceMaps(
            int xLength,
            int yLength,
            float[,] elevation,
            float averageElevation,
            WeatherMaps weatherMaps,
            HydrologyMaps hydrologyMaps)
        {
            if (elevation.GetLength(0) != xLength)
            {
                throw new ArgumentException($"Length of {nameof(elevation)} was not equal to {nameof(xLength)}", nameof(xLength));
            }
            if (elevation.GetLength(1) != yLength)
            {
                throw new ArgumentException($"Length of {nameof(elevation)} was not equal to {nameof(yLength)}", nameof(yLength));
            }
            if (weatherMaps.XLength != xLength)
            {
                throw new ArgumentException($"{nameof(weatherMaps.XLength)} of {nameof(weatherMaps)} was not equal to {nameof(xLength)}", nameof(xLength));
            }
            if (weatherMaps.YLength != yLength)
            {
                throw new ArgumentException($"{nameof(weatherMaps.YLength)} of {nameof(weatherMaps)} was not equal to {nameof(yLength)}", nameof(yLength));
            }
            if (hydrologyMaps.XLength != xLength)
            {
                throw new ArgumentException($"{nameof(hydrologyMaps.XLength)} of {nameof(hydrologyMaps)} was not equal to {nameof(xLength)}", nameof(xLength));
            }
            if (hydrologyMaps.YLength != yLength)
            {
                throw new ArgumentException($"{nameof(hydrologyMaps.YLength)} of {nameof(hydrologyMaps)} was not equal to {nameof(yLength)}", nameof(yLength));
            }

            XLength = xLength;
            YLength = yLength;

            AverageElevation = averageElevation;
            Elevation = elevation;
            _weatherMaps = weatherMaps;
            _hydrologyMaps = hydrologyMaps;
        }

        private SurfaceMaps(SerializationInfo info, StreamingContext context) : this(
            (int)info.GetValue(nameof(XLength), typeof(int)),
            (int)info.GetValue(nameof(YLength), typeof(int)),
            (float[,])info.GetValue(nameof(Elevation), typeof(float[,])),
            (float)info.GetValue(nameof(AverageElevation), typeof(float)),
            (WeatherMaps)info.GetValue(nameof(_weatherMaps), typeof(WeatherMaps)),
            (HydrologyMaps)info.GetValue(nameof(_hydrologyMaps), typeof(HydrologyMaps))) { }

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
            info.AddValue(nameof(Elevation), Elevation);
            info.AddValue(nameof(AverageElevation), AverageElevation);
            info.AddValue(nameof(_weatherMaps), _weatherMaps);
            info.AddValue(nameof(_hydrologyMaps), _hydrologyMaps);
        }
    }
}
