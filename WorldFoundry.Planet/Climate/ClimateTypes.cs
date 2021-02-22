using NeverFoundry.MathAndScience.Chemistry;

namespace NeverFoundry.WorldFoundry.Planet.Climate
{
    /// <summary>
    /// Static methods related to climate.
    /// </summary>
    public static class ClimateTypes
    {
        /// <summary>
        /// Gets the <see cref="BiomeType"/> associated with the given conditions.
        /// </summary>
        /// <param name="climateType">The climate type of the location.</param>
        /// <param name="humidityType">The humidity type of the location.</param>
        /// <param name="elevation">The elevation of the location above sea level.</param>
        /// <returns>The <see cref="BiomeType"/> associated with the given conditions.</returns>
        public static BiomeType GetBiomeType(ClimateType climateType, HumidityType humidityType, double elevation)
        {
            if (elevation <= 0)
            {
                return BiomeType.Sea;
            }

            switch (climateType)
            {
                case ClimateType.Polar:
                    return elevation >= 0.15 ? BiomeType.Alpine : BiomeType.Polar;
                case ClimateType.Subpolar:
                    return elevation >= 0.15 ? BiomeType.Subalpine : BiomeType.Tundra;
                case ClimateType.Boreal:
                    if (humidityType <= HumidityType.Arid)
                    {
                        return BiomeType.LichenWoodland;
                    }
                    else
                    {
                        return BiomeType.ConiferousForest;
                    }
                case ClimateType.CoolTemperate:
                    if (humidityType <= HumidityType.Perarid)
                    {
                        return BiomeType.ColdDesert;
                    }
                    else if (humidityType == HumidityType.Arid)
                    {
                        return BiomeType.Steppe;
                    }
                    else
                    {
                        return BiomeType.MixedForest;
                    }
                case ClimateType.WarmTemperate:
                    if (humidityType <= HumidityType.Perarid)
                    {
                        return BiomeType.HotDesert;
                    }
                    else if (humidityType <= HumidityType.Arid)
                    {
                        return BiomeType.Shrubland;
                    }
                    else
                    {
                        return BiomeType.DeciduousForest;
                    }
                case ClimateType.Subtropical:
                    if (humidityType <= HumidityType.Perarid)
                    {
                        return BiomeType.HotDesert;
                    }
                    else if (humidityType == HumidityType.Arid)
                    {
                        return BiomeType.Savanna;
                    }
                    else if (humidityType <= HumidityType.Subhumid)
                    {
                        return BiomeType.MonsoonForest;
                    }
                    else
                    {
                        return BiomeType.RainForest;
                    }
                case ClimateType.Tropical:
                    if (humidityType <= HumidityType.Perarid)
                    {
                        return BiomeType.HotDesert;
                    }
                    else if (humidityType <= HumidityType.Semiarid)
                    {
                        return BiomeType.Savanna;
                    }
                    else if (humidityType == HumidityType.Subhumid)
                    {
                        return BiomeType.MonsoonForest;
                    }
                    else
                    {
                        return BiomeType.RainForest;
                    }
                default:
                    return BiomeType.HotDesert;
            }
        }

        /// <summary>
        /// Gets the <see cref="BiomeType"/> associated with the given conditions.
        /// </summary>
        /// <param name="temperature">The annual surface temperature, in K.</param>
        /// <param name="precipitation">A rate of precipitation, in mm/hr.</param>
        /// <param name="elevation">The elevation of the location above sea level.</param>
        /// <returns>The <see cref="BiomeType"/> associated with the given conditions.</returns>
        public static BiomeType GetBiomeType(FloatRange temperature, double precipitation, double elevation)
            => GetBiomeType(GetClimateType(temperature), GetHumidityType(precipitation), elevation);

        /// <summary>
        /// Gets the <see cref="ClimateType"/> associated with the given maximum, and average annual
        /// surface temperature.
        /// </summary>
        /// <param name="temperature">The annual surface temperature, in K.</param>
        /// <returns>
        /// The <see cref="ClimateType"/> associated with the given average annual surface
        /// temperature.
        /// </returns>
        public static ClimateType GetClimateType(FloatRange temperature)
        {
            if (temperature.Max < Substances.All.Water.MeltingPoint)
            {
                return ClimateType.Polar;
            }
            else if (temperature.Max < 280.15)
            {
                return ClimateType.Subpolar;
            }
            else if (temperature.Min <= 263.15 && temperature.Max < 288.15)
            {
                return ClimateType.Boreal;
            }
            else if (temperature.Min <= 283.15)
            {
                return temperature.Max <= 295.15
                    ? ClimateType.CoolTemperate
                    : ClimateType.WarmTemperate;
            }
            else if (temperature.Min < 291.15)
            {
                return ClimateType.Subtropical;
            }
            else if (temperature.Average <= 341.15)
            {
                return ClimateType.Tropical;
            }
            else
            {
                return ClimateType.Supertropical;
            }
        }

        /// <summary>
        /// Gets the <see cref="HumidityType"/> associated with the given amount of precipitation.
        /// </summary>
        /// <param name="precipitation">A rate of precipitation, in mm/hr.</param>
        /// <returns>
        /// The <see cref="HumidityType"/> associated with the given amount of precipitation.
        /// </returns>
        public static HumidityType GetHumidityType(double precipitation)
        {
            if (precipitation < 0.01425963951631302760666210358202) // 125 mm/yr
            {
                return HumidityType.Superarid;
            }
            else if (precipitation < 0.02851927903262605521332420716404) // 250 mm/yr
            {
                return HumidityType.Perarid;
            }
            else if (precipitation < 0.05703855806525211042664841432809) // 500 mm/yr
            {
                return HumidityType.Arid;
            }
            else if (precipitation < 0.11407711613050422085329682865617) // 1000 mm/yr
            {
                return HumidityType.Semiarid;
            }
            else if (precipitation < 0.22815423226100844170659365731234) // 2000 mm/yr
            {
                return HumidityType.Subhumid;
            }
            else if (precipitation < 0.45630846452201688341318731462469) // 4000 mm/yr
            {
                return HumidityType.Humid;
            }
            else if (precipitation < 0.91261692904403376682637462924937) // 8000 mm/yr
            {
                return HumidityType.Perhumid;
            }
            else
            {
                return HumidityType.Superhumid;
            }
        }
    }
}
