using System;
using WorldFoundry.WorldGrid;

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

        internal AirCell(Planet planet, Tile t, TileClimate tc, int layer)
        {
            Elevation = t.Elevation + LayerHeight * layer;
            Temperature = layer == 0
                ? tc.Temperature
                : Season.GetTemperatureAtElevation(tc.Temperature, Elevation);
            Pressure = GetAtmosphericPressure(planet, Elevation, Temperature);
            Density = Atmosphere.GetAtmosphericDensity(Pressure, Temperature);

            // Saturation vapor pressure, and dependent properties, left at 0 above the tropopause.
            // All vapor can be precipitated at that point, without any further fine-grained modeling.
            if (Elevation <= 20000)
            {
                SaturationVaporPressure = Atmosphere.GetSaturationVaporPressure(Temperature * Exner(Pressure));
                SaturationHumidity = SaturationVaporPressure / (Season.RWater * Temperature);
                SaturationMixingRatio = Atmosphere.GetSaturationMixingRatio(SaturationVaporPressure, Pressure);
            }
        }

        private static float Exner(float pressure) => (float)Math.Pow(pressure / 100, 0.286);

        /// <summary>
        /// Calculates the atmospheric pressure at a given elevation, in kPa.
        /// </summary>
        /// <param name="elevation">
        /// An elevation above the reference elevation for standard atmospheric pressure (sea level),
        /// in meters.
        /// </param>
        /// <param name="temperature">The temperature at the given elevation, in K.</param>
        /// <returns>The atmospheric pressure at the specified height, in kPa.</returns>
        /// <remarks>
        /// In an Earth-like atmosphere, the pressure lapse rate varies considerably in the different
        /// atmospheric layers, but this cannot be easily modeled for arbitrary exoplanetary
        /// atmospheres, so the simple barometric formula is used, which should be "close enough" for
        /// the purposes of this library. Also, this calculation uses the molar mass of air on Earth,
        /// which is clearly not correct for other atmospheres, but is considered "close enough" for
        /// the purposes of this library.
        /// </remarks>
        private static float GetAtmosphericPressure(Planet planet, float elevation, float temperature)
        {
            if (elevation <= 0)
            {
                return planet.AtmosphericPressure;
            }
            else
            {
                return planet.AtmosphericPressure * (float)(-planet.G0 * Utilities.Science.Constants.MolarMass_Air * elevation / (Utilities.Science.Constants.R * (temperature)));
            }
        }
    }
}
