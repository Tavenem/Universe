using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeverFoundry.MathAndScience.Constants.Numbers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NeverFoundry.WorldFoundry.CelestialBodies.Planetoids.Planets.TerrestrialPlanets;
using NeverFoundry.WorldFoundry.Climate;
using NeverFoundry.WorldFoundry.SurfaceMapping;

namespace NeverFoundry.WorldFoundry.Test
{
    [TestClass]
    public class TerrestrialPlanet_Tests
    {
        private const int NumSeasons = 12;
        private const int SurfaceMapResolution = 90;

        [TestMethod]
        public async Task TerrestrialPlanet_GenerateAsync()
        {
            var planetParams = TerrestrialPlanetParams.FromDefaults();

            var planet = await TerrestrialPlanet.GetPlanetForNewUniverseAsync(planetParams).ConfigureAwait(false);
            Assert.IsNotNull(planet);

            Console.WriteLine($"Radius: {(planet!.Shape.ContainingRadius / 1000).ToString()} km");
            Console.WriteLine($"Surface area: {(planet!.Shape.ContainingRadius.Square() * MathConstants.FourPI / 1000000).ToString()} km²");
        }

        [TestMethod]
        public async Task TerrestrialPlanet_Generate_NoOutputAsync()
        {
            var planetParams = TerrestrialPlanetParams.FromDefaults();

            var planet = await TerrestrialPlanet.GetPlanetForNewUniverseAsync(planetParams).ConfigureAwait(false);
            Assert.IsNotNull(planet);
        }

        [TestMethod]
        public async Task TerrestrialPlanet_Generate_WithSeasonsAsync()
        {
            var planetParams = TerrestrialPlanetParams.FromDefaults();

            var planet = await TerrestrialPlanet.GetPlanetForNewUniverseAsync(planetParams).ConfigureAwait(false);
            Assert.IsNotNull(planet);

            var maps = await planet!.GetSurfaceMapsAsync(SurfaceMapResolution, steps: NumSeasons).ConfigureAwait(false);

            var sb = new StringBuilder();

            AddTempString(sb, planet!, maps);
            sb.AppendLine();
            AddTerrainString(sb, planet!, maps);
            sb.AppendLine();
            AddClimateString(sb, planet!, maps);
            sb.AppendLine();
            AddPrecipitationString(sb, planet!, maps);

            Console.WriteLine(sb.ToString());
        }

        [TestMethod]
        public async Task TerrestrialPlanet_Generate_WithSeasons_NoOutputAsync()
        {
            var planetParams = TerrestrialPlanetParams.FromDefaults();

            var planet = await TerrestrialPlanet.GetPlanetForNewUniverseAsync(planetParams).ConfigureAwait(false);
            Assert.IsNotNull(planet);

            _ = await planet!.GetSurfaceMapsAsync(SurfaceMapResolution, steps: NumSeasons).ConfigureAwait(false);
        }

        private static void AddClimateString(StringBuilder sb, TerrestrialPlanet planet, SurfaceMaps maps)
        {
            if (maps.BiomeMap[0, 0] == BiomeType.None)
            {
                return;
            }

            var totalCoords = maps.XLength * maps.YLength;
            var landCoords = 0;
            if (planet.Hydrosphere?.IsEmpty == false)
            {
                for (var x = 0; x < maps.XLength; x++)
                {
                    for (var y = 0; y < maps.YLength; y++)
                    {
                        if (maps.Elevation[x, y] > 0)
                        {
                            landCoords++;
                        }
                    }
                }
            }
            else
            {
                landCoords = totalCoords;
            }

            var biomes = new Dictionary<BiomeType, int>();
            for (var x = 0; x < maps.XLength; x++)
            {
                for (var y = 0; y < maps.YLength; y++)
                {
                    if (biomes.ContainsKey(maps.BiomeMap[x, y]))
                    {
                        biomes[maps.BiomeMap[x, y]]++;
                    }
                    else
                    {
                        biomes[maps.BiomeMap[x, y]] = 1;
                    }
                }
            }

            var allDeserts = 0;
            var deserts = 0;
            var warmDeserts = 0;
            var tropicalDeserts = 0;
            for (var x = 0; x < maps.XLength; x++)
            {
                for (var y = 0; y < maps.YLength; y++)
                {
                    if (maps.BiomeMap[x, y] == BiomeType.HotDesert
                        || maps.BiomeMap[x, y] == BiomeType.ColdDesert
                        || (maps.ClimateMap[x, y] == ClimateType.Polar
                        && maps.EcologyMap[x, y] == EcologyType.Desert))
                    {
                        allDeserts++;
                        if (maps.BiomeMap[x, y] == BiomeType.HotDesert
                            || maps.BiomeMap[x, y] == BiomeType.ColdDesert)
                        {
                            deserts++;
                        }
                        if (maps.BiomeMap[x, y] == BiomeType.HotDesert
                            && maps.ClimateMap[x, y] == ClimateType.WarmTemperate)
                        {
                            warmDeserts++;
                        }
                        if (maps.BiomeMap[x, y] == BiomeType.HotDesert
                            && maps.ClimateMap[x, y] != ClimateType.WarmTemperate)
                        {
                            tropicalDeserts++;
                        }
                    }
                }
            }

            var climates = new Dictionary<ClimateType, int>();
            for (var x = 0; x < maps.XLength; x++)
            {
                for (var y = 0; y < maps.YLength; y++)
                {
                    if (maps.Elevation[x, y] < 0)
                    {
                        continue;
                    }
                    if (climates.ContainsKey(maps.ClimateMap[x, y]))
                    {
                        climates[maps.ClimateMap[x, y]]++;
                    }
                    else
                    {
                        climates[maps.ClimateMap[x, y]] = 1;
                    }
                }
            }

            sb.AppendLine("Climates:");
            var desert = allDeserts * 100.0 / landCoords;
            sb.AppendFormat("  Desert (all):            {0}% ({1})", Math.Round(desert, 2), Math.Round(desert - 30, 2));
            sb.AppendLine();
            var nonPolarDesert = deserts * 100.0 / landCoords;
            sb.AppendFormat("  Desert (non-polar):      {0}% ({1})", Math.Round(nonPolarDesert, 2), Math.Round(nonPolarDesert - 14, 2));
            sb.AppendLine();
            var polar = (climates.TryGetValue(ClimateType.Polar, out var value) ? value : 0) * 100.0 / landCoords;
            sb.AppendFormat("  Polar:                   {0}% ({1})", Math.Round(polar, 2), Math.Round(polar - 25, 2));
            sb.AppendLine();
            sb.AppendFormat("  Tundra:                  {0}%", Math.Round((biomes.TryGetValue(BiomeType.Tundra, out value) ? value : 0) * 100.0 / landCoords, 2));
            sb.AppendLine();
            var boreal = climates.TryGetValue(ClimateType.Boreal, out value) ? value : 0;
            sb.AppendFormat("  Boreal:                  {0}%", Math.Round(boreal * 100.0 / landCoords, 2));
            sb.AppendLine();
            sb.AppendFormat("    Lichen Woodland:       {0}% ({1}%)",
                Math.Round((biomes.TryGetValue(BiomeType.LichenWoodland, out value) ? value : 0) * 100.0 / boreal, 2),
                Math.Round((biomes.TryGetValue(BiomeType.LichenWoodland, out value) ? value : 0) * 100.0 / landCoords, 2));
            sb.AppendLine();
            sb.AppendFormat("    Coniferous Forest:     {0}% ({1}%)",
                Math.Round((biomes.TryGetValue(BiomeType.ConiferousForest, out value) ? value : 0) * 100.0 / boreal, 2),
                Math.Round((biomes.TryGetValue(BiomeType.ConiferousForest, out value) ? value : 0) * 100.0 / landCoords, 2));
            sb.AppendLine();
            var coolTemperate = climates.TryGetValue(ClimateType.CoolTemperate, out value) ? value : 0;
            sb.AppendFormat("  Cool Temperate:          {0}%", Math.Round(coolTemperate * 100.0 / landCoords, 2));
            sb.AppendLine();
            sb.AppendFormat("    Cold Desert:           {0}% ({1}%)",
                Math.Round((biomes.TryGetValue(BiomeType.ColdDesert, out value) ? value : 0) * 100.0 / coolTemperate, 2),
                Math.Round((biomes.TryGetValue(BiomeType.ColdDesert, out value) ? value : 0) * 100.0 / landCoords, 2));
            sb.AppendLine();
            sb.AppendFormat("    Steppe:                {0}% ({1}%)",
                Math.Round((biomes.TryGetValue(BiomeType.Steppe, out value) ? value : 0) * 100.0 / coolTemperate, 2),
                Math.Round((biomes.TryGetValue(BiomeType.Steppe, out value) ? value : 0) * 100.0 / landCoords, 2));
            sb.AppendLine();
            sb.AppendFormat("    Mixed Forest:          {0}% ({1}%)",
                Math.Round((biomes.TryGetValue(BiomeType.MixedForest, out value) ? value : 0) * 100.0 / coolTemperate, 2),
                Math.Round((biomes.TryGetValue(BiomeType.MixedForest, out value) ? value : 0) * 100.0 / landCoords, 2));
            sb.AppendLine();
            var warmTemperate = climates.TryGetValue(ClimateType.WarmTemperate, out value) ? value : 0;
            sb.AppendFormat("  Warm Temperate:          {0}%", Math.Round(warmTemperate * 100.0 / landCoords, 2));
            sb.AppendLine();
            sb.AppendFormat("    Desert:                {0}% ({1}%)",
                Math.Round(warmDeserts * 100.0 / warmTemperate, 2),
                Math.Round(warmDeserts * 100.0 / landCoords, 2));
            sb.AppendLine();
            sb.AppendFormat("    Shrubland:             {0}% ({1}%)",
                Math.Round((biomes.TryGetValue(BiomeType.Shrubland, out value) ? value : 0) * 100.0 / warmTemperate, 2),
                Math.Round((biomes.TryGetValue(BiomeType.Shrubland, out value) ? value : 0) * 100.0 / landCoords, 2));
            sb.AppendLine();
            sb.AppendFormat("    Deciduous Forest:      {0}% ({1}%)",
                Math.Round((biomes.TryGetValue(BiomeType.DeciduousForest, out value) ? value : 0) * 100.0 / warmTemperate, 2),
                Math.Round((biomes.TryGetValue(BiomeType.DeciduousForest, out value) ? value : 0) * 100.0 / landCoords, 2));
            sb.AppendLine();
            var tropical = 0;
            tropical += climates.TryGetValue(ClimateType.Subtropical, out value) ? value : 0;
            tropical += climates.TryGetValue(ClimateType.Tropical, out value) ? value : 0;
            tropical += climates.TryGetValue(ClimateType.Supertropical, out value) ? value : 0;
            sb.AppendFormat("  Tropical:                {0}%", Math.Round(tropical * 100.0 / landCoords, 2));
            sb.AppendLine();
            sb.AppendFormat("    Desert:                {0}% ({1}%)",
                Math.Round(tropicalDeserts * 100.0 / tropical, 2),
                Math.Round(tropicalDeserts * 100.0 / landCoords, 2));
            sb.AppendLine();
            sb.AppendFormat("    Savanna:               {0}% ({1}%)",
                Math.Round((biomes.TryGetValue(BiomeType.Savanna, out value) ? value : 0) * 100.0 / tropical, 2),
                Math.Round((biomes.TryGetValue(BiomeType.Savanna, out value) ? value : 0) * 100.0 / landCoords, 2));
            sb.AppendLine();
            sb.AppendFormat("    Monsoon Forest:        {0}% ({1}%)",
                Math.Round((biomes.TryGetValue(BiomeType.MonsoonForest, out value) ? value : 0) * 100.0 / tropical, 2),
                Math.Round((biomes.TryGetValue(BiomeType.MonsoonForest, out value) ? value : 0) * 100.0 / landCoords, 2));
            sb.AppendLine();
            var rainforest = (biomes.TryGetValue(BiomeType.RainForest, out value) ? value : 0) * 100.0 / landCoords;
            sb.AppendFormat("    Rain Forest:           {0}% ({1}%) ({2})",
                Math.Round(rainforest, 2),
                Math.Round((biomes.TryGetValue(BiomeType.RainForest, out value) ? value : 0) * 100.0 / tropical, 2),
                Math.Round(rainforest - 6, 2));
            sb.AppendLine();
        }

        private static void AddPrecipitationString(StringBuilder sb, TerrestrialPlanet planet, SurfaceMaps maps)
        {
            sb.AppendLine("Precipitation (annual average, land):");
            var landCoords = new List<(int x, int y)>();
            for (var x = 0; x < maps.XLength; x++)
            {
                for (var y = 0; y < maps.YLength; y++)
                {
                    if (maps.Elevation[x, y] > 0)
                    {
                        landCoords.Add((x, y));
                    }
                }
            }
            if (landCoords.Count == 0)
            {
                sb.AppendLine("  No land.");
                return;
            }

            var list = landCoords
                .Select(x => maps.TotalPrecipitationMap[x.x, x.y] * planet.Atmosphere.MaxPrecipitation)
                .ToList();

            list.Sort();
            var avg = list.Average();
            sb.AppendFormat("  Avg:                     {0}mm ({1})", Math.Round(avg), Math.Round(avg - 990, 2));
            sb.AppendLine();
            var avg90 = list.Take((int)Math.Floor(list.Count * 0.9)).Average();
            sb.AppendFormat("  Avg (<=P90):             {0}mm ({1})", Math.Round(avg90), Math.Round(avg90 - 990, 2));
            sb.AppendLine();
            var avgList = planet.Atmosphere.MaxPrecipitation / 3.5403429574618413761305460296723;
            sb.AppendFormat("  Avg (listed):            {0}mm ({1})", Math.Round(avgList), Math.Round(avgList - 990, 2));
            sb.AppendLine();

            var n = 0;
            var temperate = 0.0;
            for (var x = 0; x < maps.XLength; x++)
            {
                for (var y = 0; y < maps.YLength; y++)
                {
                    if (maps.ClimateMap[x, y] == ClimateType.CoolTemperate
                        || maps.ClimateMap[x, y] == ClimateType.WarmTemperate)
                    {
                        temperate += maps.TotalPrecipitationMap[x, y];
                        n++;
                    }
                }
            }
            temperate /= n;
            temperate *= planet.Atmosphere.MaxPrecipitation;
            sb.AppendFormat("  Avg (Temperate):         {0}mm ({1})", Math.Round(temperate), Math.Round(temperate - 1100, 2));
            sb.AppendLine();

            sb.AppendFormat("  Min:                     {0}mm", Math.Round(list[0]));
            sb.AppendLine();
            sb.AppendFormat("  P10:                     {0}mm", Math.Round(list.Skip((int)Math.Floor(list.Count * 0.1)).First()));
            sb.AppendLine();
            sb.AppendFormat("  P20:                     {0}mm", Math.Round(list.Skip((int)Math.Floor(list.Count * 0.2)).First()));
            sb.AppendLine();
            sb.AppendFormat("  P30:                     {0}mm", Math.Round(list.Skip((int)Math.Floor(list.Count * 0.3)).First()));
            sb.AppendLine();
            sb.AppendFormat("  P40:                     {0}mm", Math.Round(list.Skip((int)Math.Floor(list.Count * 0.4)).First()));
            sb.AppendLine();
            sb.AppendFormat("  P50:                     {0}mm", Math.Round(list.Skip((int)Math.Floor(list.Count * 0.5)).First()));
            sb.AppendLine();
            sb.AppendFormat("  P60:                     {0}mm", Math.Round(list.Skip((int)Math.Floor(list.Count * 0.6)).First()));
            sb.AppendLine();
            sb.AppendFormat("  P70:                     {0}mm", Math.Round(list.Skip((int)Math.Floor(list.Count * 0.7)).First()));
            sb.AppendLine();
            sb.AppendFormat("  P80:                     {0}mm", Math.Round(list.Skip((int)Math.Floor(list.Count * 0.8)).First()));
            sb.AppendLine();
            sb.AppendFormat("  P90:                     {0}mm", Math.Round(list.Skip((int)Math.Floor(list.Count * 0.9)).First()));
            sb.AppendLine();
            var max = list.Last();
            sb.AppendFormat("  Max:                     {0}mm ({1})", Math.Round(max), Math.Round(max - 11871, 2));

            sb.AppendLine();
        }

        private static void AddTempString(StringBuilder sb, TerrestrialPlanet planet, SurfaceMaps maps)
        {
            sb.AppendLine("Temp:");
            var avg = maps.TemperatureRange.Average;
            sb.AppendFormat("  Avg:                     {0} K ({1})", Math.Round(avg), Math.Round(avg - (float)TerrestrialPlanetParams.DefaultSurfaceTemperature, 2));
            sb.AppendLine();

            var water = planet.Hydrosphere?.IsEmpty == false;
            var seaLevelCoords = new List<(int x, int y)>();
            if (water)
            {
                for (var x = 0; x < maps.XLength; x++)
                {
                    for (var y = 0; y < maps.YLength; y++)
                    {
                        if (maps.Elevation[x, y] <= 0)
                        {
                            seaLevelCoords.Add((x, y));
                        }
                    }
                }
                var min = seaLevelCoords.Min(x => maps.TemperatureRangeMap[x.x, x.y].Min);
                sb.AppendFormat("  Min Sea-Level:           {0} K", Math.Round(min));
                sb.AppendLine();

                var avgSeaLevel = seaLevelCoords.Average(x => maps.TemperatureRangeMap[x.x, x.y].Average);
                sb.AppendFormat("  Avg Sea-Level:           {0} K", Math.Round(avgSeaLevel));
                sb.AppendLine();
            }

            sb.AppendFormat("  Max:                     {0} K", Math.Round(maps.TemperatureRange.Max));
            sb.AppendLine();

            var maxAvg = 0.0f;
            for (var x = 0; x < maps.XLength; x++)
            {
                for (var y = 0; y < maps.YLength; y++)
                {
                    maxAvg = Math.Max(maxAvg, maps.TemperatureRangeMap[x, y].Average);
                }
            }
            sb.AppendFormat("  Max Avg:          {0} K", Math.Round(maxAvg));
            sb.AppendLine();

            if (water)
            {
                var minMaxTemp = seaLevelCoords.Min(x => maps.TemperatureRangeMap[x.x, x.y].Max);
                sb.AppendFormat("  Min Max (water):  {0} K", Math.Round(minMaxTemp));
            }
            sb.AppendLine();
        }

        private static void AddTerrainString(StringBuilder sb, TerrestrialPlanet planet, SurfaceMaps maps)
        {
            sb.AppendFormat("Sea Level:                 {0}m", Math.Round(planet.SeaLevel));
            sb.AppendLine();

            var avg = 0.0;
            if (planet.Hydrosphere?.IsEmpty == false)
            {
                var landCoords = new List<(int x, int y)>();
                for (var x = 0; x < maps.XLength; x++)
                {
                    for (var y = 0; y < maps.YLength; y++)
                    {
                        if (maps.Elevation[x, y] > 0)
                        {
                            landCoords.Add((x, y));
                        }
                    }
                }
                avg = landCoords.Average(x => maps.Elevation[x.x, x.y]) * planet.MaxElevation;
            }
            else
            {
                avg = maps.AverageElevation * planet.MaxElevation;
            }

            sb.AppendFormat("Avg Land Elevation:        {0}m", Math.Round(avg));
            sb.AppendLine();

            var max = 0.0;
            for (var x = 0; x < maps.XLength; x++)
            {
                for (var y = 0; y < maps.YLength; y++)
                {
                    max = Math.Max(max, maps.Elevation[x, y]);
                }
            }
            max *= planet.MaxElevation;

            sb.AppendFormat("Max Elevation:             {0}m / {1}m", Math.Round(max), Math.Round(planet.MaxElevation));
            sb.AppendLine();
        }
    }
}
