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
            Density = GetAtmosphericDensity(Pressure, Temperature);

            // Saturation vapor pressure, and dependent properties, left at 0 above the tropopause.
            // All vapor can be precipitated at that point, without any further fine-grained modeling.
            if (Elevation <= 20000)
            {
                SaturationVaporPressure = GetSaturationVaporPressure(Temperature * Exner(Pressure));
                SaturationHumidity = SaturationVaporPressure / (Season.RWater * Temperature);
                SaturationMixingRatio = GetSaturationMixingRatio(SaturationVaporPressure, Pressure);
            }
        }

        private static float Exner(float pressure) => (float)Math.Pow(pressure / 100, 0.286);

        private static float GetAtmosphericDensity(float pressure, float temperature)
            => pressure * 1000 / (287.058f * temperature);

        private static float GetAtmosphericPressure(Planet planet, float elevation, float temperature)
        {
            float pressure = 0, tb = 288.15f;

            if (elevation <= 0)
            {
                pressure = planet.AtmosphericPressure;
            }
            else if (elevation <= 1000)
            {
                pressure = planet.AtmosphericPressure - 0.0113f * elevation;
            }
            else
            {
                float pb = 0, lb = 0, hb = 0;
                if (elevation <= 11000)
                {
                    pb = 101.325f;
                    lb = -0.0065f;
                    hb = 0;
                }
                else if (elevation <= 20000)
                {
                    pb = 22.6321f;
                    tb = 216.65f;
                    lb = 0f;
                    hb = 11000;
                }
                else if (elevation <= 32000)
                {
                    pb = 5.47489f;
                    tb = 216.65f;
                    lb = 0.001f;
                    hb = 20000;
                }
                else if (elevation <= 47000)
                {
                    pb = 0.86802f;
                    tb = 228.65f;
                    lb = 0.0028f;
                    hb = 32000;
                }
                else if (elevation <= 51000)
                {
                    pb = 0.11091f;
                    tb = 270.65f;
                    lb = 0f;
                    hb = 47000;
                }
                else if (elevation <= 71000)
                {
                    pb = 0.06694f;
                    tb = 270.65f;
                    lb = -0.0028f;
                    hb = 51000;
                }
                else
                {
                    pb = 0.00396f;
                    tb = 214.65f;
                    lb = -0.002f;
                    hb = 71000;
                }

                if (lb == 0)
                {
                    pressure = pb * (float)Math.Exp(-planet.G0MdivR * (elevation - hb) / tb);
                }
                else
                {
                    pressure = pb * (float)Math.Pow(tb / (tb + lb * (elevation - hb)), planet.G0MdivR / lb);
                }
            }

            // adjust for actual temp (vs. std. temp used in formula)
            return pressure * tb / temperature;
        }

        private static float GetSaturationMixingRatio(float vaporPressure, float pressure)
        {
            var vp = vaporPressure / 1000;
            if (vp >= pressure)
            {
                vp = pressure * 0.99999f;
            }
            return 0.6219907f * vp / (pressure - vp);
        }

        private static float GetSaturationVaporPressure(float temperature)
        {
            var a = temperature > Season.freezingPoint
                ? 611.21
                : 611.15;
            var b = temperature > Season.freezingPoint
                ? 18.678
                : 23.036;
            var c = temperature > Season.freezingPoint
                ? 234.5
                : 333.7;
            var d = temperature > Season.freezingPoint
                ? 257.14
                : 279.82;
            var t = temperature - Season.freezingPoint;
            return (float)(a * Math.Exp((b - (t / c)) * (t / (d + t))));
        }
    }
}
