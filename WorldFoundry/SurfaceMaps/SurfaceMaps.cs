using WorldFoundry.Climate;

namespace WorldFoundry.SurfaceMapping
{
    /// <summary>
    /// A collection of surface and weather maps for a <see
    /// cref="CelestialBodies.Planetoids.Planets.TerrestrialPlanets.TerrestrialPlanet"/>.
    /// </summary>
    public struct SurfaceMaps
    {
        private readonly HydrologyMaps _hydrologyMaps;
        private readonly WeatherMaps _weatherMaps;

        /// <summary>
        /// A two-dimensional array corresponding to points on an equirectangular projected map of a
        /// terrestrial planet's surface. The first index corresponds to the X coordinate, and the
        /// second index corresponds to the Y coordinate. The values represent <see
        /// cref="BiomeType"/>.
        /// </summary>
        public BiomeType[,] Biome => _weatherMaps.Biome;

        /// <summary>
        /// A two-dimensional array corresponding to points on an equirectangular projected map of a
        /// terrestrial planet's surface. The first index corresponds to the X coordinate, and the
        /// second index corresponds to the Y coordinate. The values represent <see
        /// cref="ClimateType"/>, based on average annual temperature.
        /// </summary>
        public ClimateType[,] Climate => _weatherMaps.Climate;

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
        /// A two-dimensional array corresponding to points on an equirectangular projected map of a
        /// terrestrial planet's surface. The first index corresponds to the X coordinate, and the
        /// second index corresponds to the Y coordinate. The values represent <see
        /// cref="EcologyType"/>.
        /// </summary>
        public EcologyType[,] Ecology => _weatherMaps.Ecology;

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
        /// A two-dimensional array corresponding to points on an equirectangular projected map of a
        /// terrestrial planet's surface. The first index corresponds to the X coordinate, and the
        /// second index corresponds to the Y coordinate. The values represent <see
        /// cref="HumidityType"/>, based on annual precipitation.
        /// </summary>
        public HumidityType[,] Humidity => _weatherMaps.Humidity;

        /// <summary>
        /// The maximum flow rate of the <see cref="Flow"/> map, in m³/s.
        /// </summary>
        public double MaxFlow => _hydrologyMaps.MaxFlow;

        /// <summary>
        /// The maximum annual precipitation expected to be produced by this atmosphere, in mm. Not
        /// necessarily the actual maximum precipitation within the given region (use <c><see
        /// cref="TotalPrecipitationRange"/>.Max</c> for that).
        /// </summary>
        public double MaxPrecipitation => _weatherMaps.MaxPrecipitation;

        /// <summary>
        /// The maximum annual snowfall expected to be produced by this atmosphere, in mm. Not
        /// necessarily the actual maximum snowfall within the given region (use <c><see
        /// cref="TotalSnowfallRange"/>.Max</c> for that).
        /// </summary>
        public double MaxSnowfall => _weatherMaps.MaxSnowfall;

        /// <summary>
        /// The approximate maximum surface temperature of the planet, in K. Not necessarily the
        /// actual maximum temperature within the given region (use <c><see
        /// cref="OverallTemperatureRange"/>.Max</c> for that).
        /// </summary>
        public double MaxTemperature => _weatherMaps.MaxTemperature;

        /// <summary>
        /// A range giving the minimum, maximum, and average temperature throughout the specified
        /// area over the entire period represented by all <see cref="PrecipitationMaps"/>, as a value
        /// between 0 and 1, with 1 indicating the maximum temperature of the planet.
        /// <seealso cref="CelestialBodies.Planetoids.Planetoid.MaxSurfaceTemperature"/>
        /// </summary>
        public FloatRange OverallTemperatureRange => _weatherMaps.OverallTemperatureRange;

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
        public FloatRange[,] SeaIceRanges => _weatherMaps.SeaIceRanges;

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
        public FloatRange[,] SnowCoverRanges => _weatherMaps.SnowCoverRanges;

        /// <summary>
        /// A two-dimensional array corresponding to points on an equirectangular projected map of a
        /// terrestrial planet's surface. The first index corresponds to the X coordinate, and the
        /// second index corresponds to the Y coordinate. The values represent the temperature
        /// range. Values range from 0 to 1, with 1 indicating the maximum temperature of the
        /// planet.
        /// <seealso cref="CelestialBodies.Planetoids.Planetoid.MaxSurfaceTemperature"/>
        /// </summary>
        public FloatRange[,] TemperatureRanges => _weatherMaps.TemperatureRanges;

        /// <summary>
        /// A two-dimensional array corresponding to points on an equirectangular projected map of a
        /// terrestrial planet's surface. The first index corresponds to the X coordinate, and the
        /// second index corresponds to the Y coordinate. The values represent the total amount of
        /// precipitation indicated on all contained <see cref="PrecipitationMaps"/>. Values range from 0
        /// to 1, with 1 indicating the maximum annual potential precipitation of the planet's
        /// atmosphere. Will be <see langword="null"/> if no <see cref="PrecipitationMaps"/> are present.
        /// <seealso cref="Atmosphere.MaxPrecipitation"/>
        /// </summary>
        public float[,] TotalPrecipitation => _weatherMaps.TotalPrecipitation;

        /// <summary>
        /// A range giving the minimum, maximum, and average precipitation throughout the specified
        /// area over the entire period represented by all <see cref="PrecipitationMaps"/>, as a value
        /// between 0 and 1, with 1 indicating the maximum annual potential precipitation of the
        /// planet's atmosphere.
        /// <seealso cref="Atmosphere.MaxPrecipitation"/>
        /// </summary>
        public FloatRange TotalPrecipitationRange => _weatherMaps.TotalPrecipitationRange;

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
        public float[,] TotalSnowfall => _weatherMaps.TotalSnowfall;

        /// <summary>
        /// A range giving the minimum, maximum, and average snowfall throughout the specified area
        /// over the entire period represented by all <see cref="PrecipitationMaps"/>, as a value
        /// between 0 and 1, with 1 indicating the maximum annual potential snowfall of the planet's
        /// atmosphere.
        /// <seealso cref="Atmosphere.MaxSnowfall"/>
        /// </summary>
        public FloatRange TotalSnowfallRange => _weatherMaps.TotalSnowfallRange;

        /// <summary>
        /// Initializes a new instance of <see cref="SurfaceMaps"/>.
        /// </summary>
        /// <param name="elevation">An elevation map.</param>
        /// <param name="weatherMaps">A <see cref="PrecipitationMaps"/> instance.</param>
        /// <param name="hydrologyMaps">A <see cref="HydrologyMaps"/> instance.</param>
        public SurfaceMaps(float[,] elevation, WeatherMaps weatherMaps, HydrologyMaps hydrologyMaps)
        {
            Elevation = elevation;
            _weatherMaps = weatherMaps;
            _hydrologyMaps = hydrologyMaps;
        }
    }
}
