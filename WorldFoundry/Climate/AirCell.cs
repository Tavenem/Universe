using System;
using WorldFoundry.CelestialBodies.Planetoids.Planets.TerrestrialPlanets;
using WorldFoundry.WorldGrids;

namespace WorldFoundry.Climate
{
    internal class AirCell
    {
        internal const float LayerHeight = 2000;

        internal float AbsoluteHumidity { get; set; }
        internal float Density { get; set; }
        internal float Elevation { get; set; }
        internal float Pressure { get; set; }
        internal float RelativeHumidity { get; set; }
        internal float SaturationHumidity { get; set; }
        internal float SaturationMixingRatio { get; set; }
        internal float SaturationVaporPressure { get; set; }
        internal float Temperature { get; set; }

        internal AirCell(TerrestrialPlanet planet, Tile t, TileClimate tc, int layer)
        {
            Elevation = t.Elevation + LayerHeight * layer;
            Temperature = layer == 0
                ? tc.Temperature
                : planet.Atmosphere.GetTemperatureAtElevation(tc.Temperature, Elevation);
            Pressure = planet.Atmosphere.GetAtmosphericPressure(Elevation, Temperature);
            Density = Atmosphere.GetAtmosphericDensity(Pressure, Temperature);

            // Saturation vapor pressure, and dependent properties, left at 0 above the tropopause.
            // All vapor can be precipitated at that point, without any further fine-grained modeling.
            if (Elevation <= 20000)
            {
                SaturationVaporPressure = Atmosphere.GetSaturationVaporPressure(Temperature * Exner(Pressure));
                SaturationHumidity = SaturationVaporPressure / (Utilities.Science.Constants.SpecificGasConstantOfWater * Temperature);
                SaturationMixingRatio = Atmosphere.GetSaturationMixingRatio(SaturationVaporPressure, Pressure);
            }
        }

        private static float Exner(float pressure) => (float)Math.Pow(pressure / 100, 0.286);
    }
}
