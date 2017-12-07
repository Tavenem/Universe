using System.ComponentModel.DataAnnotations.Schema;
using WorldFoundry.CelestialBodies.Planetoids.Planets.TerrestrialPlanets;
using WorldFoundry.WorldGrids;

namespace WorldFoundry.Climate
{
    /// <summary>
    /// Models a cell of air above a <see cref="Tile"/> during a <see cref="Season"/>.
    /// </summary>
    /// <remarks>
    /// Only <see cref="AbsoluteHumidity"/> is persisted. All other values are used only for internal
    /// calculations during <see cref="Season"/> generation, and can be discarded after the
    /// generation process is complete.
    /// </remarks>
    public class AirCell : DataItem, IIndexedItem
    {
        internal const float LayerHeight = 2000;

        internal float AbsoluteHumidity { get; set; }

        [NotMapped]
        internal float Density { get; set; }

        /// <summary>
        /// The index of this item.
        /// </summary>
        public int Index { get; }

        [NotMapped]
        internal float Pressure { get; set; }

        [NotMapped]
        internal float RelativeHumidity { get; set; }

        [NotMapped]
        internal float SaturationHumidity { get; set; }

        [NotMapped]
        internal float SaturationMixingRatio { get; set; }

        [NotMapped]
        internal float SaturationVaporPressure { get; set; }

        [NotMapped]
        internal float Temperature { get; set; }

        /// <summary>
        /// Initializes a new instance of <see cref="AirCell"/>.
        /// </summary>
        public AirCell() { }

        internal AirCell(TerrestrialPlanet planet, Tile t, TileClimate tc, int layer)
        {
            var height = LayerHeight * layer;
            Temperature = layer == 0
                ? tc.Temperature
                : planet.Atmosphere.GetTemperatureAtElevation(tc.Temperature, height);
            Pressure = planet.Atmosphere.GetAtmosphericPressure(Temperature, t.Elevation + height);
            Density = Atmosphere.GetAtmosphericDensity(Temperature, Pressure);
            SaturationVaporPressure = Atmosphere.GetSaturationVaporPressure(Temperature * planet.Atmosphere.Exner(Pressure));
            SaturationHumidity = SaturationVaporPressure / (Utilities.Science.Constants.SpecificGasConstantOfWater * Temperature);
            SaturationMixingRatio = Atmosphere.GetSaturationMixingRatio(SaturationVaporPressure, Pressure);
        }
    }
}
