using WorldFoundry.Climate;

namespace WorldFoundry.SurfaceMaps
{
    /// <summary>
    /// A collection of surface and weather maps for a <see
    /// cref="CelestialBodies.Planetoids.Planets.TerrestrialPlanets.TerrestrialPlanet"/>.
    /// </summary>
    public struct TerrestrialSurfaceMapSet
    {
        private HydrologyMaps _hydrologyMaps;
        private WeatherMapSet _weatherMapSet;

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
        public ClimateType[,] Climate => _weatherMapSet.Climate;

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
        public EcologyType[,] Ecology { get; }

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
        public HumidityType[,] Humidity => _weatherMapSet.Humidity;

        /// <summary>
        /// A range giving the minimum, maximum, and average temperature throughout the specified
        /// area over the entire period represented by all <see cref="WeatherMaps"/>, as a value
        /// between 0 and 1, with 1 indicating the maximum temperature of the planet.
        /// <seealso cref="CelestialBodies.Planetoids.Planetoid.MaxSurfaceTemperature"/>
        /// </summary>
        public FloatRange OverallTemperatureRange => _weatherMapSet.OverallTemperatureRange;

        /// <summary>
        /// A two-dimensional array corresponding to points on an equirectangular projected map of a
        /// terrestrial planet's surface. The first index corresponds to the X coordinate, and the
        /// second index corresponds to the Y coordinate. The values represent the proportion of the
        /// year during which there is persistent sea ice.
        /// </summary>
        public FloatRange[,] SeaIceRanges => _weatherMapSet.SeaIceRanges;

        /// <summary>
        /// A two-dimensional array corresponding to points on an equirectangular projected map of a
        /// terrestrial planet's surface. The first index corresponds to the X coordinate, and the
        /// second index corresponds to the Y coordinate. The values represent the proportion of the
        /// year during which there is persistent snow cover.
        /// </summary>
        public FloatRange[,] SnowCoverRanges => _weatherMapSet.SnowCoverRanges;

        /// <summary>
        /// A two-dimensional array corresponding to points on an equirectangular projected map of a
        /// terrestrial planet's surface. The first index corresponds to the X coordinate, and the
        /// second index corresponds to the Y coordinate. The values represent the temperature
        /// range. Values range from 0 to 1, with 1 indicating the maximum temperature of the
        /// planet.
        /// <seealso cref="Planetoid.MaxSurfaceTemperature"/>
        /// </summary>
        public FloatRange[,] TemperatureRanges => _weatherMapSet.TemperatureRanges;

        /// <summary>
        /// A two-dimensional array corresponding to points on an equirectangular projected map of a
        /// terrestrial planet's surface. The first index corresponds to the X coordinate, and the
        /// second index corresponds to the Y coordinate. The values represent the total amount of
        /// precipitation indicated on all contained <see cref="WeatherMaps"/>. Values range from 0
        /// to 1, with 1 indicating the maximum annual potential precipitation of the planet's
        /// atmosphere. Will be <see langword="null"/> if no <see cref="WeatherMaps"/> are present.
        /// <seealso cref="Atmosphere.MaxPrecipitation"/>
        /// </summary>
        public float[,] TotalPrecipitation => _weatherMapSet.TotalPrecipitation;

        /// <summary>
        /// A range giving the minimum, maximum, and average precipitation throughout the specified
        /// area over the entire period represented by all <see cref="WeatherMaps"/>, as a value
        /// between 0 and 1, with 1 indicating the maximum annual potential precipitation of the
        /// planet's atmosphere.
        /// <seealso cref="Atmosphere.MaxPrecipitation"/>
        /// </summary>
        public FloatRange TotalPrecipitationRange => _weatherMapSet.TotalPrecipitationRange;

        /// <summary>
        /// A collection of <see cref="WeatherMaps"/>.
        /// </summary>
        public WeatherMaps[] WeatherMaps => _weatherMapSet.WeatherMaps;

        /// <summary>
        /// Initializes a new instance of <see cref="TerrestrialSurfaceMapSet"/>.
        /// </summary>
        /// <param name="elevationMap">An elevation map.</param>
        /// <param name="weatherMapSet">A <see cref="WeatherMapSet"/> instance.</param>
        /// <param name="hydrologyMaps">A <see cref="HydrologyMaps"/> instance.</param>
        public TerrestrialSurfaceMapSet(float[,] elevationMap, WeatherMapSet weatherMapSet, HydrologyMaps hydrologyMaps)
        {
            Elevation = elevationMap;
            _weatherMapSet = weatherMapSet;
            _hydrologyMaps = hydrologyMaps;

            if (_weatherMapSet.Climate == null || _weatherMapSet.Humidity == null)
            {
                Biome = null;
                Ecology = null;
            }
            else
            {
                var xLength = _weatherMapSet.Climate.GetLength(0);
                var yLength = _weatherMapSet.Climate.GetLength(1);

                Biome = new BiomeType[xLength, yLength];
                Ecology = new EcologyType[xLength, yLength];
                for (var x = 0; x < xLength; x++)
                {
                    for (var y = 0; y < yLength; y++)
                    {
                        Biome[x, y] = ClimateTypes.GetBiomeType(_weatherMapSet.Climate[x, y], _weatherMapSet.Humidity[x, y], elevationMap[x, y]);
                        Ecology[x, y] = ClimateTypes.GetEcologyType(_weatherMapSet.Climate[x, y], _weatherMapSet.Humidity[x, y], elevationMap[x, y]);
                    }
                }
            }
        }
    }
}
