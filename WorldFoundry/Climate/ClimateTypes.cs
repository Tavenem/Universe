namespace NeverFoundry.WorldFoundry.Climate
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
                    return BiomeType.Polar;
                case ClimateType.Subpolar:
                    return BiomeType.Tundra;
                case ClimateType.Boreal:
                    if (humidityType <= HumidityType.Perarid)
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
                    else if (humidityType <= HumidityType.Semiarid)
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
        /// <param name="temperature">An average annual surface temperature, in K.</param>
        /// <param name="annualPrecipitation">An amount of annual precipitation, in mm.</param>
        /// <param name="elevation">The elevation of the location above sea level.</param>
        /// <returns>The <see cref="BiomeType"/> associated with the given conditions.</returns>
        public static BiomeType GetBiomeType(double temperature, double annualPrecipitation, double elevation)
            => GetBiomeType(GetClimateType(temperature), GetHumidityType(annualPrecipitation), elevation);

        /// <summary>
        /// Gets the <see cref="ClimateType"/> associated with the given average annual surface
        /// temperature.
        /// </summary>
        /// <param name="temperature">An average annual surface temperature, in K.</param>
        /// <returns>The <see cref="ClimateType"/> associated with the given average annual surface
        /// temperature.</returns>
        public static ClimateType GetClimateType(double temperature)
        {
            if (temperature <= CelestialSubstances.WaterMeltingPoint + 1.5)
            {
                return ClimateType.Polar;
            }
            else if (temperature <= CelestialSubstances.WaterMeltingPoint + 3)
            {
                return ClimateType.Subpolar;
            }
            else if (temperature <= CelestialSubstances.WaterMeltingPoint + 6)
            {
                return ClimateType.Boreal;
            }
            else if (temperature <= CelestialSubstances.WaterMeltingPoint + 12)
            {
                return ClimateType.CoolTemperate;
            }
            else if (temperature <= CelestialSubstances.WaterMeltingPoint + 18)
            {
                return ClimateType.WarmTemperate;
            }
            else if (temperature <= CelestialSubstances.WaterMeltingPoint + 24)
            {
                return ClimateType.Subtropical;
            }
            else if (temperature <= CelestialSubstances.WaterMeltingPoint + 68)
            {
                return ClimateType.Tropical;
            }
            else
            {
                return ClimateType.Supertropical;
            }
        }

        /// <summary>
        /// Gets the <see cref="EcologyType"/> associated with the given conditions.
        /// </summary>
        /// <param name="climateType">The climate type of the location.</param>
        /// <param name="humidityType">The humidity type of the location.</param>
        /// <param name="elevation">The elevation of the location above sea level.</param>
        /// <returns>The <see cref="EcologyType"/> associated with the given conditions.</returns>
        public static EcologyType GetEcologyType(ClimateType climateType, HumidityType humidityType, double elevation)
        {
            if (elevation <= 0)
            {
                return EcologyType.Sea;
            }

            switch (climateType)
            {
                case ClimateType.Polar:
                    if (humidityType <= HumidityType.Perarid)
                    {
                        return EcologyType.Desert;
                    }
                    else
                    {
                        return EcologyType.Ice;
                    }
                case ClimateType.Subpolar:
                    if (humidityType == HumidityType.Superarid)
                    {
                        return EcologyType.DryTundra;
                    }
                    else if (humidityType == HumidityType.Perarid)
                    {
                        return EcologyType.MoistTundra;
                    }
                    else if (humidityType == HumidityType.Arid)
                    {
                        return EcologyType.WetTundra;
                    }
                    else
                    {
                        return EcologyType.RainTundra;
                    }
                case ClimateType.Boreal:
                    if (humidityType == HumidityType.Superarid)
                    {
                        return EcologyType.Desert;
                    }
                    else if (humidityType == HumidityType.Perarid)
                    {
                        return EcologyType.DryScrub;
                    }
                    else if (humidityType == HumidityType.Arid)
                    {
                        return EcologyType.MoistForest;
                    }
                    else if (humidityType == HumidityType.Semiarid)
                    {
                        return EcologyType.WetForest;
                    }
                    else
                    {
                        return EcologyType.RainForest;
                    }
                case ClimateType.CoolTemperate:
                    if (humidityType == HumidityType.Superarid)
                    {
                        return EcologyType.Desert;
                    }
                    else if (humidityType == HumidityType.Perarid)
                    {
                        return EcologyType.DesertScrub;
                    }
                    else if (humidityType == HumidityType.Arid)
                    {
                        return EcologyType.Steppe;
                    }
                    else if (humidityType == HumidityType.Semiarid)
                    {
                        return EcologyType.MoistForest;
                    }
                    else if (humidityType == HumidityType.Subhumid)
                    {
                        return EcologyType.WetForest;
                    }
                    else
                    {
                        return EcologyType.RainForest;
                    }
                case ClimateType.WarmTemperate:
                    if (humidityType == HumidityType.Superarid)
                    {
                        return EcologyType.Desert;
                    }
                    else if (humidityType == HumidityType.Perarid)
                    {
                        return EcologyType.DesertScrub;
                    }
                    else if (humidityType == HumidityType.Arid)
                    {
                        return EcologyType.ThornScrub;
                    }
                    else if (humidityType == HumidityType.Semiarid)
                    {
                        return EcologyType.DryForest;
                    }
                    else if (humidityType == HumidityType.Subhumid)
                    {
                        return EcologyType.MoistForest;
                    }
                    else if (humidityType == HumidityType.Humid)
                    {
                        return EcologyType.WetForest;
                    }
                    else
                    {
                        return EcologyType.RainForest;
                    }
                case ClimateType.Subtropical:
                    if (humidityType == HumidityType.Superarid)
                    {
                        return EcologyType.Desert;
                    }
                    else if (humidityType == HumidityType.Perarid)
                    {
                        return EcologyType.DesertScrub;
                    }
                    else if (humidityType == HumidityType.Arid)
                    {
                        return EcologyType.ThornWoodland;
                    }
                    else if (humidityType == HumidityType.Semiarid)
                    {
                        return EcologyType.DryForest;
                    }
                    else if (humidityType == HumidityType.Subhumid)
                    {
                        return EcologyType.MoistForest;
                    }
                    else if (humidityType == HumidityType.Humid)
                    {
                        return EcologyType.WetForest;
                    }
                    else
                    {
                        return EcologyType.RainForest;
                    }
                case ClimateType.Tropical:
                    if (humidityType == HumidityType.Superarid)
                    {
                        return EcologyType.Desert;
                    }
                    else if (humidityType == HumidityType.Perarid)
                    {
                        return EcologyType.DesertScrub;
                    }
                    else if (humidityType == HumidityType.Arid)
                    {
                        return EcologyType.ThornWoodland;
                    }
                    else if (humidityType == HumidityType.Semiarid)
                    {
                        return EcologyType.VeryDryForest;
                    }
                    else if (humidityType == HumidityType.Subhumid)
                    {
                        return EcologyType.DryForest;
                    }
                    else if (humidityType == HumidityType.Humid)
                    {
                        return EcologyType.MoistForest;
                    }
                    else if (humidityType == HumidityType.Perhumid)
                    {
                        return EcologyType.WetForest;
                    }
                    else
                    {
                        return EcologyType.RainForest;
                    }
                default:
                    return EcologyType.Desert;
            }
        }

        /// <summary>
        /// Gets the <see cref="EcologyType"/> associated with the given conditions.
        /// </summary>
        /// <param name="temperature">An average annual surface temperature, in K.</param>
        /// <param name="annualPrecipitation">An amount of annual precipitation, in mm.</param>
        /// <param name="elevation">The elevation of the location above sea level.</param>
        /// <returns>The <see cref="EcologyType"/> associated with the given conditions.</returns>
        public static EcologyType GetEcologyType(double temperature, double annualPrecipitation, double elevation)
            => GetEcologyType(GetClimateType(temperature), GetHumidityType(annualPrecipitation), elevation);

        /// <summary>
        /// Gets the <see cref="HumidityType"/> associated with the given amount of precipitation.
        /// </summary>
        /// <param name="annualPrecipitation">An amount of annual precipitation, in mm.</param>
        /// <returns>The <see cref="HumidityType"/> associated with the given amount of
        /// precipitation.</returns>
        public static HumidityType GetHumidityType(double annualPrecipitation)
        {
            if (annualPrecipitation < 125)
            {
                return HumidityType.Superarid;
            }
            else if (annualPrecipitation < 250)
            {
                return HumidityType.Perarid;
            }
            else if (annualPrecipitation < 500)
            {
                return HumidityType.Arid;
            }
            else if (annualPrecipitation < 1000)
            {
                return HumidityType.Semiarid;
            }
            else if (annualPrecipitation < 2000)
            {
                return HumidityType.Subhumid;
            }
            else if (annualPrecipitation < 4000)
            {
                return HumidityType.Humid;
            }
            else if (annualPrecipitation < 8000)
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
