using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeverFoundry.MathAndScience.Constants.Numbers;
using NeverFoundry.WorldFoundry.Climate;
using NeverFoundry.WorldFoundry.Space;
using NeverFoundry.WorldFoundry.Space.Planetoids;
using NeverFoundry.WorldFoundry.SurfaceMapping;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeverFoundry.WorldFoundry.Test
{
    [TestClass]
    public class TerrestrialPlanetTests
    {
        private const int MapResolution = 180;
        private const int Seasons = 12;

        [TestMethod]
        public async Task EarthlikePlanetAsync()
        {
            // First run to ensure timed runs do not include any one-time initialization costs.
            var planet = Planetoid.GetPlanetForSunlikeStar(out _);
            Assert.IsNotNull(planet);

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            planet = Planetoid.GetPlanetForSunlikeStar(out _);

            stopwatch.Stop();

            Assert.IsNotNull(planet);

            Console.WriteLine($"Planet generation time: {stopwatch.Elapsed:s'.'FFF} s");
            Console.WriteLine($"Radius: {planet!.Shape.ContainingRadius / 1000:N0} km");
            Console.WriteLine($"Surface area: {planet!.Shape.ContainingRadius.Square() * MathConstants.FourPI / 1000000:N0} km²");

            stopwatch.Restart();

            using (var elevationMap = await planet
                .GetElevationMapAsync()
                .ConfigureAwait(false))
            {
                using var temperatureMap = await planet
                    .GetTemperatureMapAsync()
                    .ConfigureAwait(false);

                using var precipitationMap = await planet
                    .GetPrecipitationMapAsync(Seasons)
                    .ConfigureAwait(false);

                WeatherMaps climateMaps;
                using var winterTemperatureMap = await planet
                    .GetTemperatureMapWinterAsync()
                    .ConfigureAwait(false);
                using var summerTemperatureMap = await planet
                    .GetTemperatureMapSummerAsync()
                    .ConfigureAwait(false);
                climateMaps = new WeatherMaps(
                    planet,
                    elevationMap,
                    winterTemperatureMap,
                    summerTemperatureMap,
                    precipitationMap,
                    MapResolution);
            }

            stopwatch.Stop();

            Console.WriteLine($"Equirectangular surface map generation time: {stopwatch.Elapsed:s'.'FFF} s");

            var projection = new MapProjectionOptions(equalArea: true);

            stopwatch.Restart();

            using var elevationMapEA = await planet
                .GetElevationMapProjectionAsync(MapResolution, projection)
                .ConfigureAwait(false);

            using var temperatureMapEA = await planet
                .GetTemperatureMapProjectionAsync(MapResolution, projection)
                .ConfigureAwait(false);

            using var precipitationMapEA = await planet
                .GetPrecipitationMapProjectionAsync(MapResolution, Seasons, projection)
                .ConfigureAwait(false);

            WeatherMaps climateMapsEA;
            using (var winterTemperatureMapEA = await planet
                .GetTemperatureMapProjectionWinterAsync(MapResolution, projection)
                .ConfigureAwait(false))
            {
                using var summerTemperatureMapEA = await planet
                    .GetTemperatureMapProjectionSummerAsync(MapResolution, projection)
                    .ConfigureAwait(false);
                climateMapsEA = new WeatherMaps(
                    planet,
                    elevationMapEA,
                    winterTemperatureMapEA,
                    summerTemperatureMapEA,
                    precipitationMapEA,
                    MapResolution,
                    projection);
            }

            stopwatch.Stop();

            Console.WriteLine($"Cylindrical equal-area surface map generation time: {stopwatch.Elapsed:s'.'FFF} s");

            var normalizedSeaLevel = planet.SeaLevel / planet.MaxElevation;
            var elevationRange = elevationMapEA.GetElevationRange(planet);
            var landCoords = 0;
            if (planet.Hydrosphere?.IsEmpty == false)
            {
                for (var x = 0; x < elevationMapEA.Width; x++)
                {
                    for (var y = 0; y < elevationMapEA.Height; y++)
                    {
                        var value = (2.0 * elevationMapEA[x, y].PackedValue / ushort.MaxValue) - 1;
                        if (value - normalizedSeaLevel > 0)
                        {
                            landCoords++;
                        }
                    }
                }
            }
            var sb = new StringBuilder();
            AddTempString(sb, temperatureMapEA);
            sb.AppendLine();
            AddTerrainString(sb, planet, elevationMapEA, landCoords);
            sb.AppendLine();
            AddClimateString(sb, elevationMapEA, normalizedSeaLevel, landCoords, climateMapsEA);
            sb.AppendLine();
            AddPrecipitationString(sb, planet, elevationMapEA, precipitationMapEA, normalizedSeaLevel, landCoords, climateMapsEA);
            Console.WriteLine(sb.ToString());
        }

        private static void AddClimateString(
            StringBuilder sb,
            Image<L16> elevationMap,
            double normalizedSeaLevel,
            int landCoords,
            WeatherMaps maps)
        {
            if (maps.BiomeMap[0][0] == BiomeType.None)
            {
                return;
            }

            var biomes = new Dictionary<BiomeType, int>();
            for (var x = 0; x < maps.XLength; x++)
            {
                for (var y = 0; y < maps.YLength; y++)
                {
                    if (biomes.ContainsKey(maps.BiomeMap[x][y]))
                    {
                        biomes[maps.BiomeMap[x][y]]++;
                    }
                    else
                    {
                        biomes[maps.BiomeMap[x][y]] = 1;
                    }
                }
            }

            var deserts = 0;
            var warmDeserts = 0;
            var tropicalDeserts = 0;
            for (var x = 0; x < maps.XLength; x++)
            {
                for (var y = 0; y < maps.YLength; y++)
                {
                    if (maps.BiomeMap[x][y] == BiomeType.HotDesert
                        || maps.BiomeMap[x][y] == BiomeType.ColdDesert)
                    {
                        deserts++;
                        if (maps.BiomeMap[x][y] == BiomeType.HotDesert)
                        {
                            if (maps.ClimateMap[x][y] == ClimateType.WarmTemperate)
                            {
                                warmDeserts++;
                            }
                            else
                            {
                                tropicalDeserts++;
                            }
                        }
                    }
                }
            }

            var climates = new Dictionary<ClimateType, int>();
            for (var x = 0; x < maps.XLength; x++)
            {
                for (var y = 0; y < maps.YLength; y++)
                {
                    if ((2.0 * elevationMap[x, y].PackedValue / ushort.MaxValue) - 1 - normalizedSeaLevel <= 0)
                    {
                        continue;
                    }
                    if (climates.ContainsKey(maps.ClimateMap[x][y]))
                    {
                        climates[maps.ClimateMap[x][y]]++;
                    }
                    else
                    {
                        climates[maps.ClimateMap[x][y]] = 1;
                    }
                }
            }

            sb.AppendLine("Climates:");
            var desert = deserts * 100.0 / landCoords;
            sb.AppendFormat("  Desert:                  {0}% ({1:+0.##;-0.##;on-targ\\et})", Math.Round(desert, 2), Math.Round(desert - 14, 2));
            sb.AppendLine();
            var polar = (climates.TryGetValue(ClimateType.Polar, out var value) ? value : 0) * 100.0 / landCoords;
            sb.AppendFormat("  Polar:                   {0}% ({1:+0.##;-0.##;on-targ\\et})", Math.Round(polar, 2), Math.Round(polar - 20, 2));
            sb.AppendLine();
            sb.AppendFormat("  Tundra:                  {0}%", Math.Round((biomes.TryGetValue(BiomeType.Tundra, out value) ? value : 0) * 100.0 / landCoords, 2));
            sb.AppendLine();
            var alpine = (biomes.TryGetValue(BiomeType.Alpine, out value) ? value : 0) * 100.0 / landCoords;
            sb.AppendFormat("  Alpine:                  {0}% ({1:+0.##;-0.##;on-targ\\et})", Math.Round(alpine, 2), Math.Round(alpine - 3, 2));
            sb.AppendLine();
            sb.AppendFormat("  Subalpine:               {0}%", Math.Round((biomes.TryGetValue(BiomeType.Subalpine, out value) ? value : 0) * 100.0 / landCoords, 2));
            sb.AppendLine();
            var boreal = climates.TryGetValue(ClimateType.Boreal, out value) ? value : 0;
            sb.AppendFormat("  Boreal:                  {0}%", Math.Round(boreal * 100.0 / landCoords, 2));
            sb.AppendLine();
            sb.AppendFormat("    Lichen Woodland:       {0}% ({1}%)",
                boreal == 0 ? 0 : Math.Round((biomes.TryGetValue(BiomeType.LichenWoodland, out value) ? value : 0) * 100.0 / boreal, 2),
                Math.Round((biomes.TryGetValue(BiomeType.LichenWoodland, out value) ? value : 0) * 100.0 / landCoords, 2));
            sb.AppendLine();
            sb.AppendFormat("    Coniferous Forest:     {0}% ({1}%)",
                boreal == 0 ? 0 : Math.Round((biomes.TryGetValue(BiomeType.ConiferousForest, out value) ? value : 0) * 100.0 / boreal, 2),
                Math.Round((biomes.TryGetValue(BiomeType.ConiferousForest, out value) ? value : 0) * 100.0 / landCoords, 2));
            sb.AppendLine();
            var coolTemperate = climates.TryGetValue(ClimateType.CoolTemperate, out value) ? value : 0;
            sb.AppendFormat("  Cool Temperate:          {0}%", Math.Round(coolTemperate * 100.0 / landCoords, 2));
            sb.AppendLine();
            sb.AppendFormat("    Cold Desert:           {0}% ({1}%)",
                coolTemperate == 0 ? 0 : Math.Round((biomes.TryGetValue(BiomeType.ColdDesert, out value) ? value : 0) * 100.0 / coolTemperate, 2),
                Math.Round((biomes.TryGetValue(BiomeType.ColdDesert, out value) ? value : 0) * 100.0 / landCoords, 2));
            sb.AppendLine();
            sb.AppendFormat("    Steppe:                {0}% ({1}%)",
                coolTemperate == 0 ? 0 : Math.Round((biomes.TryGetValue(BiomeType.Steppe, out value) ? value : 0) * 100.0 / coolTemperate, 2),
                Math.Round((biomes.TryGetValue(BiomeType.Steppe, out value) ? value : 0) * 100.0 / landCoords, 2));
            sb.AppendLine();
            sb.AppendFormat("    Mixed Forest:          {0}% ({1}%)",
                coolTemperate == 0 ? 0 : Math.Round((biomes.TryGetValue(BiomeType.MixedForest, out value) ? value : 0) * 100.0 / coolTemperate, 2),
                Math.Round((biomes.TryGetValue(BiomeType.MixedForest, out value) ? value : 0) * 100.0 / landCoords, 2));
            sb.AppendLine();
            var warmTemperate = climates.TryGetValue(ClimateType.WarmTemperate, out value) ? value : 0;
            sb.AppendFormat("  Warm Temperate:          {0}%", Math.Round(warmTemperate * 100.0 / landCoords, 2));
            sb.AppendLine();
            sb.AppendFormat("    Desert:                {0}% ({1}%)",
                warmTemperate == 0 ? 0 : Math.Round(warmDeserts * 100.0 / warmTemperate, 2),
                Math.Round(warmDeserts * 100.0 / landCoords, 2));
            sb.AppendLine();
            sb.AppendFormat("    Shrubland:             {0}% ({1}%)",
                warmTemperate == 0 ? 0 : Math.Round((biomes.TryGetValue(BiomeType.Shrubland, out value) ? value : 0) * 100.0 / warmTemperate, 2),
                Math.Round((biomes.TryGetValue(BiomeType.Shrubland, out value) ? value : 0) * 100.0 / landCoords, 2));
            sb.AppendLine();
            sb.AppendFormat("    Deciduous Forest:      {0}% ({1}%)",
                warmTemperate == 0 ? 0 : Math.Round((biomes.TryGetValue(BiomeType.DeciduousForest, out value) ? value : 0) * 100.0 / warmTemperate, 2),
                Math.Round((biomes.TryGetValue(BiomeType.DeciduousForest, out value) ? value : 0) * 100.0 / landCoords, 2));
            sb.AppendLine();
            var tropical = 0;
            tropical += climates.TryGetValue(ClimateType.Subtropical, out value) ? value : 0;
            tropical += climates.TryGetValue(ClimateType.Tropical, out value) ? value : 0;
            tropical += climates.TryGetValue(ClimateType.Supertropical, out value) ? value : 0;
            sb.AppendFormat("  Tropical:                {0}%", Math.Round(tropical * 100.0 / landCoords, 2));
            sb.AppendLine();
            sb.AppendFormat("    Desert:                {0}% ({1}%)",
                tropical == 0 ? 0 : Math.Round(tropicalDeserts * 100.0 / tropical, 2),
                Math.Round(tropicalDeserts * 100.0 / landCoords, 2));
            sb.AppendLine();
            sb.AppendFormat("    Savanna:               {0}% ({1}%)",
                tropical == 0 ? 0 : Math.Round((biomes.TryGetValue(BiomeType.Savanna, out value) ? value : 0) * 100.0 / tropical, 2),
                Math.Round((biomes.TryGetValue(BiomeType.Savanna, out value) ? value : 0) * 100.0 / landCoords, 2));
            sb.AppendLine();
            sb.AppendFormat("    Monsoon Forest:        {0}% ({1}%)",
                tropical == 0 ? 0 : Math.Round((biomes.TryGetValue(BiomeType.MonsoonForest, out value) ? value : 0) * 100.0 / tropical, 2),
                Math.Round((biomes.TryGetValue(BiomeType.MonsoonForest, out value) ? value : 0) * 100.0 / landCoords, 2));
            sb.AppendLine();
            var rainforest = (biomes.TryGetValue(BiomeType.RainForest, out value) ? value : 0) * 100.0 / landCoords;
            sb.AppendFormat("    Rain Forest:           {0}% ({1}%) ({2:+0.##;-0.##;on-targ\\et})",
                Math.Round(rainforest, 2),
                tropical == 0 ? 0 : Math.Round((biomes.TryGetValue(BiomeType.RainForest, out value) ? value : 0) * 100.0 / tropical, 2),
                Math.Round(rainforest - 6, 2));
            sb.AppendLine();
        }

        private static void AddPrecipitationString(
            StringBuilder sb,
            Planetoid planet,
            Image<L16> elevationMap,
            Image<L16> precipitationMap,
            double normalizedSeaLevel,
            int landCoords,
            WeatherMaps maps)
        {
            sb.Append("Max precip: ")
                .Append(Math.Round(planet.Atmosphere.MaxPrecipitation, 3))
                .AppendLine("mm/hr");

            sb.AppendLine("Precipitation (average, land):");
            if (landCoords == 0)
            {
                sb.AppendLine("  No land.");
                return;
            }

            var n = 0;
            var temperate = 0.0;
            var list = new List<double>();
            for (var x = 0; x < maps.XLength; x++)
            {
                for (var y = 0; y < maps.YLength; y++)
                {
                    if (((double)elevationMap[x, y].PackedValue / ushort.MaxValue) - normalizedSeaLevel < 0)
                    {
                        continue;
                    }

                    var precipitation = (double)precipitationMap[x, y].PackedValue / ushort.MaxValue * planet.Atmosphere.MaxPrecipitation;

                    list.Add(precipitation);

                    if (maps.ClimateMap[x][y] == ClimateType.CoolTemperate
                        || maps.ClimateMap[x][y] == ClimateType.WarmTemperate)
                    {
                        temperate += precipitation;
                        n++;
                    }
                }
            }
            list.Sort();
            if (n == 0)
            {
                temperate = 0;
            }
            else
            {
                temperate /= n;
            }

            var avg = list.Average();
            sb.AppendFormat("  Avg:                     {0}mm/hr ({1:+0.##;-0.##;on-targ\\et})", Math.Round(avg, 3), Math.Round(avg - 0.11293634496919917864476386036961, 3));
            sb.AppendLine();
            var avg90 = list.Take((int)Math.Floor(list.Count * 0.9)).Average();
            sb.AppendFormat("  Avg (<=P90):             {0}mm/hr ({1:+0.##;-0.##;on-targ\\et})", Math.Round(avg90, 3), Math.Round(avg90 - 0.11293634496919917864476386036961, 3));
            sb.AppendLine();
            var avgList = planet.Atmosphere.AveragePrecipitation;
            sb.AppendFormat("  Avg (listed):            {0}mm/hr ({1:+0.##;-0.##;on-targ\\et})", Math.Round(avgList, 3), Math.Round(avgList - 0.11293634496919917864476386036961, 3));
            sb.AppendLine();
            sb.AppendFormat("  Avg (Temperate):         {0}mm/hr ({1:+0.##;-0.##;on-targ\\et})", Math.Round(temperate, 3), Math.Round(temperate - 0.12548482774355464293862651152179, 3));
            sb.AppendLine();

            sb.AppendFormat("  Min:                     {0}mm/hr", Math.Round(list[0], 3));
            sb.AppendLine();
            sb.AppendFormat("  P10:                     {0}mm/hr", Math.Round(list.Skip((int)Math.Floor(list.Count * 0.1)).First(), 3));
            sb.AppendLine();
            sb.AppendFormat("  P20:                     {0}mm/hr", Math.Round(list.Skip((int)Math.Floor(list.Count * 0.2)).First(), 3));
            sb.AppendLine();
            sb.AppendFormat("  P30:                     {0}mm/hr", Math.Round(list.Skip((int)Math.Floor(list.Count * 0.3)).First(), 3));
            sb.AppendLine();
            sb.AppendFormat("  P40:                     {0}mm/hr", Math.Round(list.Skip((int)Math.Floor(list.Count * 0.4)).First(), 3));
            sb.AppendLine();
            sb.AppendFormat("  P50:                     {0}mm/hr", Math.Round(list.Skip((int)Math.Floor(list.Count * 0.5)).First(), 3));
            sb.AppendLine();
            sb.AppendFormat("  P60:                     {0}mm/hr", Math.Round(list.Skip((int)Math.Floor(list.Count * 0.6)).First(), 3));
            sb.AppendLine();
            sb.AppendFormat("  P70:                     {0}mm/hr", Math.Round(list.Skip((int)Math.Floor(list.Count * 0.7)).First(), 3));
            sb.AppendLine();
            sb.AppendFormat("  P80:                     {0}mm/hr", Math.Round(list.Skip((int)Math.Floor(list.Count * 0.8)).First(), 3));
            sb.AppendLine();
            sb.AppendFormat("  P90:                     {0}mm/hr", Math.Round(list.Skip((int)Math.Floor(list.Count * 0.9)).First(), 3));
            sb.AppendLine();
            var max = list.Last();
            sb.AppendFormat("  Max:                     {0}mm/hr ({1:+0.##;-0.##;on-targ\\et})", Math.Round(max), Math.Round(max - 1.3542094455852156057494866529774, 3));

            sb.AppendLine();
        }

        private static void AddTempString(StringBuilder sb, Image<L16> temperatureMap)
        {
            sb.AppendLine("Temp:");
            var range = temperatureMap.GetTemperatureRange();
            sb.AppendFormat("  Avg:                     {0} K ({1:+0.##;-0.##;on-targ\\et})", Math.Round(range.Average), Math.Round(range.Average - (float)PlanetParams.EarthSurfaceTemperature, 2));
            sb.AppendLine();
            sb.AppendFormat("  Min:                     {0} K", Math.Round(range.Min));
            sb.AppendLine();
            sb.AppendFormat("  Max:                     {0} K", Math.Round(range.Max));
            sb.AppendLine();
        }

        private static void AddTerrainString(StringBuilder sb, Planetoid planet, Image<L16> elevationMap, int landCoords)
        {
            sb.AppendFormat("Sea Level:                 {0}m", Math.Round(planet.SeaLevel));
            sb.AppendLine();

            var elevationRange = elevationMap.GetElevationRange(planet);
            if (planet.Hydrosphere?.IsEmpty == false)
            {
                var totalCoords = (decimal)(elevationMap.Width * elevationMap.Height);
                var landProportion = landCoords / totalCoords;
                sb.AppendFormat("Land proportion:           {0}", Math.Round(landProportion, 2));
                sb.AppendLine();
                sb.AppendFormat("Water proportion:          {0}", Math.Round(1 - landProportion, 2));
                sb.AppendLine();
            }

            sb.AppendFormat("Avg Elevation:             {0}m", Math.Round(elevationRange.Average));
            sb.AppendLine();
            sb.AppendFormat("Min Elevation:             {0}m / {1}m", Math.Round(elevationRange.Min), Math.Round(planet.MaxElevation));
            sb.AppendLine();
            sb.AppendFormat("Max Elevation:             {0}m / {1}m", Math.Round(elevationRange.Max), Math.Round(planet.MaxElevation));
            sb.AppendLine();
        }
    }
}
