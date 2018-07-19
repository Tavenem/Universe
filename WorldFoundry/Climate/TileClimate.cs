using System;
using WorldFoundry.CelestialBodies.Planetoids.Planets.TerrestrialPlanets;
using WorldFoundry.WorldGrids;

namespace WorldFoundry.Climate
{
    /// <summary>
    /// Describes the climate of a <see cref="Tile"/> during a particular <see cref="Season"/>.
    /// </summary>
    public class TileClimate
    {
        internal const int AirCellLayerHeight = 2000;

        /// <summary>
        /// The number of cells of air above this <see cref="Tile"/> during this <see cref="Season"/>.
        /// </summary>
        internal int AirCellLayers { get; set; }

        /// <summary>
        /// The average atmospheric pressure in this <see cref="Tile"/> during this <see
        /// cref="Season"/>, in kPa.
        /// </summary>
        public float AtmosphericPressure { get; internal set; }

        /// <summary>
        /// The latitude of this <see cref="Tile"/> relative to the tropical equator (versus the true equator).
        /// </summary>
        internal float Latitude { get; set; }

        /// <summary>
        /// The total precipitation in this <see cref="Tile"/> during this <see cref="Season"/>,
        /// in mm. Counts all forms of precipitation, including the water-equivalent amount of
        /// snowfall (even though snow is also reported separately).
        /// </summary>
        public float Precipitation { get; internal set; }

        /// <summary>
        /// The total runoff in this <see cref="Tile"/> during this <see cref="Season"/>, in m³/s.
        /// </summary>
        internal float Runoff { get; set; }

        /// <summary>
        /// The average thickness of sea ice in this <see cref="Tile"/> during this <see
        /// cref="Season"/>, in meters.
        /// </summary>
        public float SeaIce { get; internal set; }

        /// <summary>
        /// The depth of persistent snow cover in this <see cref="Tile"/> during this <see
        /// cref="Season"/>, in mm. Assumes a typical ratio of 1mm water-equivalent = 13mm snow.
        /// </summary>
        /// <remarks>
        /// Snow depth at any given time during the season will depend on the amount of time since
        /// the last snow event, the accumulation during that event, and the snow cover prior to the
        /// event. This number reflects the minimum level which remains unmelted between events, and
        /// at the end of the season.
        /// </remarks>
        public float SnowCover { get; internal set; }

        /// <summary>
        /// The total amount of snow which falls in this <see cref="Tile"/> during this <see
        /// cref="Season"/>, in mm.
        /// </summary>
        /// <remarks>
        /// This may all fall during a single large snow event, or be divided equally among multiple
        /// snow events.
        /// </remarks>
        public float SnowFall { get; set; }

        /// <summary>
        /// The average temperature in this <see cref="Tile"/> during this <see cref="Season"/>,
        /// in K.
        /// </summary>
        public float Temperature { get; internal set; }

        internal static int GetAirCellIndexOfNearlyZeroSaturationVaporPressure(TerrestrialPlanet planet, double elevation, double temperature)
        {
            var height = planet.Atmosphere.GetHeightForTemperature(Atmosphere.TemperatureAtNearlyZeroSaturationVaporPressure, temperature, elevation);
            return (int)Math.Ceiling(height / AirCellLayerHeight);
        }

        internal static double GetSaturationVaporPressure(TerrestrialPlanet planet, double elevation, double temperature)
        {
            var pressure = planet.Atmosphere.GetAtmosphericPressure(temperature, elevation);
            return Atmosphere.GetSaturationVaporPressure(temperature * planet.Atmosphere.Exner(pressure));
        }

        internal static double GetSaturationVaporPressure(int index, TerrestrialPlanet planet, double elevation, double temperature)
        {
            var height = AirCellLayerHeight * index;
            var temperatureAtElevation = index == 0
                ? temperature
                : planet.Atmosphere.GetTemperatureAtElevation(temperature, elevation + height);
            return GetSaturationVaporPressure(planet, elevation + height, temperatureAtElevation);
        }

        internal static double GetSaturationMixingRatio(TerrestrialPlanet planet, TileClimate tc, double elevation)
            => Atmosphere.GetSaturationMixingRatio(GetSaturationVaporPressure(0, planet, elevation, tc.Temperature), tc.AtmosphericPressure);

        internal static double GetAirCellHeight(TileClimate tc)
            => Atmosphere.GetAtmosphericDensity(tc.Temperature, tc.AtmosphericPressure) * AirCellLayerHeight * tc.AirCellLayers;
    }
}
