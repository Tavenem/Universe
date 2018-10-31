using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Text;
using WorldFoundry.CelestialBodies.Planetoids.Planets.TerrestrialPlanets;
using WorldFoundry.Climate;
using WorldFoundry.WorldGrids;

namespace WorldFoundry.Test
{
    [TestClass]
    public class TerrestrialPlanet_Tests
    {
        private const int GridSize = 6;
        private const int NumSeasons = 12;

        [TestMethod]
        public void TerrestrialPlanet_Generate()
        {
            var planetParams = TerrestrialPlanetParams.FromDefaults(gridSize: GridSize);

            var planet = TerrestrialPlanet.DefaultHumanPlanetNewUniverse(planetParams);
            Assert.IsNotNull(planet);

            var grid = planet.GetGrid();
            Assert.IsNotNull(grid);

            Console.WriteLine($"Tiles: {grid.Tiles.Length.ToString()}");
            Console.WriteLine($"Radius: {(planet.Radius / 1000).ToString("N0")} km");
            Console.WriteLine($"Surface area: {(grid.Tiles.Sum(x => x.Area) / 1e6).ToString("N0")} km²");
            Console.WriteLine($"Tile area: {((grid.Tiles.Length > 12 ? grid.Tiles[12].Area : grid.Tiles[11].Area) / 1e6).ToString("N0")} km²");
            Console.WriteLine($"Equatorial tiles: {grid.Tiles.Count(x => x.Corners.Any(c => grid.Corners[c].Latitude <= 0) && x.Corners.Any(c => grid.Corners[c].Latitude >= 0)).ToString()}");
        }

        [TestMethod]
        public void TerrestrialPlanet_Generate_NoOutput()
        {
            var planetParams = TerrestrialPlanetParams.FromDefaults(gridSize: GridSize);

            var planet = TerrestrialPlanet.DefaultHumanPlanetNewUniverse(planetParams);
            Assert.IsNotNull(planet);

            var grid = planet.GetGrid();
            Assert.IsNotNull(grid);
        }

        [TestMethod]
        public void TerrestrialPlanet_Generate_WithSeasons()
        {
            var planetParams = TerrestrialPlanetParams.FromDefaults(gridSize: GridSize);

            var planet = TerrestrialPlanet.DefaultHumanPlanetNewUniverse(planetParams);
            Assert.IsNotNull(planet);

            var grid = planet.GetGrid();
            Assert.IsNotNull(grid);

            Console.WriteLine($"Tiles: {grid.Tiles.Length.ToString()}");
            Console.WriteLine($"Tile Area: {Math.Round((grid.Tiles.Length > 12 ? grid.Tiles[12].Area : grid.Tiles[11].Area) / 1e6).ToString("N0")}km²");
            Console.WriteLine();

            planet.SetClimate(grid, NumSeasons);

            var sb = new StringBuilder();

            AddTempString(sb, grid);
            sb.AppendLine();
            AddSimpleTerrainString(sb, planet, grid);
            sb.AppendLine();
            AddSimpleClimateString(sb, grid);
            sb.AppendLine();
            AddPrecipitationString(sb, grid);

            Console.WriteLine(sb.ToString());
        }

        [TestMethod]
        public void TerrestrialPlanet_Generate_WithSeasons_NoOutput()
        {
            var planetParams = TerrestrialPlanetParams.FromDefaults(gridSize: GridSize);

            var planet = TerrestrialPlanet.DefaultHumanPlanetNewUniverse(planetParams);
            Assert.IsNotNull(planet);

            var grid = planet.GetGrid();
            Assert.IsNotNull(grid);

            planet.SetClimate(grid, NumSeasons);
        }

        private static void AddSimpleClimateString(StringBuilder sb, WorldGrid grid)
        {
            if (grid.Tiles[0].BiomeType == BiomeType.None)
            {
                return;
            }

            var landTiles = grid.Tiles.Count(t => t.Elevation > 0);
            sb.AppendLine("Climates:");
            var desert = grid.Tiles.Count(t => t.BiomeType == BiomeType.HotDesert || t.BiomeType == BiomeType.ColdDesert || (t.ClimateType == ClimateType.Polar && t.EcologyType == EcologyType.Desert)) * 100.0 / landTiles;
            sb.AppendFormat("  Desert (all):            {0}% ({1})", Math.Round(desert, 2), Math.Round(desert - 30, 2));
            sb.AppendLine();
            var nonPolarDesert = grid.Tiles.Count(t => t.BiomeType == BiomeType.HotDesert || t.BiomeType == BiomeType.ColdDesert) * 100.0 / landTiles;
            sb.AppendFormat("  Desert (non-polar):      {0}% ({1})", Math.Round(nonPolarDesert, 2), Math.Round(nonPolarDesert - 14, 2));
            sb.AppendLine();
            var polar = grid.Tiles.Count(t => t.ClimateType == ClimateType.Polar) * 100.0 / grid.Tiles.Length;
            sb.AppendFormat("  Polar:                   {0}% ({1})", Math.Round(polar, 2), Math.Round(polar - 20, 2));
            sb.AppendLine();
            sb.AppendFormat("  Tundra:                  {0}%", Math.Round(grid.Tiles.Count(t => t.BiomeType == BiomeType.Tundra) * 100.0 / landTiles, 2));
            sb.AppendLine();
            sb.AppendFormat("  Boreal:                  {0}%", Math.Round(grid.Tiles.Count(t => t.Elevation > 0 && t.ClimateType == ClimateType.Boreal) * 100.0 / landTiles, 2));
            sb.AppendLine();
            sb.AppendFormat("    Lichen Woodland:       {0}%", Math.Round(grid.Tiles.Count(t => t.BiomeType == BiomeType.LichenWoodland) * 100.0 / landTiles, 2));
            sb.AppendLine();
            sb.AppendFormat("    Coniferous Forest:     {0}%", Math.Round(grid.Tiles.Count(t => t.BiomeType == BiomeType.ConiferousForest) * 100.0 / landTiles, 2));
            sb.AppendLine();
            sb.AppendFormat("  Cool Temperate:          {0}%", Math.Round(grid.Tiles.Count(t => t.Elevation > 0 && t.ClimateType == ClimateType.CoolTemperate) * 100.0 / landTiles, 2));
            sb.AppendLine();
            sb.AppendFormat("    Cold Desert:           {0}%", Math.Round(grid.Tiles.Count(t => t.BiomeType == BiomeType.ColdDesert) * 100.0 / landTiles, 2));
            sb.AppendLine();
            sb.AppendFormat("    Steppe:                {0}%", Math.Round(grid.Tiles.Count(t => t.BiomeType == BiomeType.Steppe) * 100.0 / landTiles, 2));
            sb.AppendLine();
            sb.AppendFormat("    Mixed Forest:          {0}%", Math.Round(grid.Tiles.Count(t => t.BiomeType == BiomeType.MixedForest) * 100.0 / landTiles, 2));
            sb.AppendLine();
            sb.AppendFormat("  Warm Temperate:          {0}%", Math.Round(grid.Tiles.Count(t => t.Elevation > 0 && t.ClimateType == ClimateType.WarmTemperate) * 100.0 / landTiles, 2));
            sb.AppendLine();
            sb.AppendFormat("    Desert:                {0}%", Math.Round(grid.Tiles.Count(t => t.ClimateType == ClimateType.WarmTemperate && t.BiomeType == BiomeType.HotDesert) * 100.0 / landTiles, 2));
            sb.AppendLine();
            sb.AppendFormat("    Shrubland:             {0}%", Math.Round(grid.Tiles.Count(t => t.BiomeType == BiomeType.Shrubland) * 100.0 / landTiles, 2));
            sb.AppendLine();
            sb.AppendFormat("    Deciduous Forest:      {0}%", Math.Round(grid.Tiles.Count(t => t.BiomeType == BiomeType.DeciduousForest) * 100.0 / landTiles, 2));
            sb.AppendLine();
            sb.AppendFormat("  Tropical:                {0}%", Math.Round(grid.Tiles.Count(t => t.Elevation > 0 && (t.ClimateType == ClimateType.Subtropical || t.ClimateType == ClimateType.Tropical || t.ClimateType == ClimateType.Supertropical)) * 100.0 / landTiles, 2));
            sb.AppendLine();
            sb.AppendFormat("    Desert:                {0}%", Math.Round(grid.Tiles.Count(t => t.ClimateType != ClimateType.WarmTemperate && t.BiomeType == BiomeType.HotDesert) * 100.0 / landTiles, 2));
            sb.AppendLine();
            sb.AppendFormat("    Savanna:               {0}%", Math.Round(grid.Tiles.Count(t => t.BiomeType == BiomeType.Savanna) * 100.0 / landTiles, 2));
            sb.AppendLine();
            sb.AppendFormat("    Monsoon Forest:        {0}%", Math.Round(grid.Tiles.Count(t => t.BiomeType == BiomeType.MonsoonForest) * 100.0 / landTiles, 2));
            sb.AppendLine();
            var rainforest = grid.Tiles.Count(t => t.BiomeType == BiomeType.RainForest) * 100.0 / landTiles;
            sb.AppendFormat("    Rain Forest:           {0}% ({1})", Math.Round(rainforest, 2), Math.Round(rainforest - 6, 2));
            sb.AppendLine();
        }

        private static void AddPrecipitationString(StringBuilder sb, WorldGrid grid)
        {
            sb.AppendLine("Precipitation (annual average, land):");
            var list = grid.Tiles.Where(x => x.Elevation > 0)
                .Select(x => x.Precipitation)
                .ToList();
            if (list.Count > 0)
            {
                list.Sort();
                var avg = list.Average();
                sb.AppendFormat("  Avg:                     {0}mm ({1})", Math.Round(avg), Math.Round(avg - 990, 2));
                sb.AppendLine();
                var avg90 = list.Take((int)Math.Floor(list.Count * 0.9)).Average();
                sb.AppendFormat("  Avg (<=P90):             {0}mm ({1})", Math.Round(avg90), Math.Round(avg90 - 990, 2));
                sb.AppendLine();
                var temperates = grid.Tiles
                    .Where(t => t.Elevation > 0 && (t.ClimateType == ClimateType.CoolTemperate || t.ClimateType == ClimateType.WarmTemperate));
                if (temperates.Any())
                {
                    var avgT = temperates.Select(t => t.Precipitation).Average();
                    sb.AppendFormat("  Avg (Temperate):         {0}mm ({1})", Math.Round(avgT), Math.Round(avgT - 1100, 2));
                }
                else
                {
                    sb.AppendFormat("  Avg (Temperate):         0mm (-1100)");
                }
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
            else
            {
                sb.AppendLine("  No land.");
            }
        }

        private static void AddTempString(StringBuilder sb, WorldGrid grid)
        {
            sb.AppendLine("Temp:");
            var avg = grid.Tiles.Average(x => x.Temperature.Average);
            sb.AppendFormat("  Avg:                     {0} K ({1})", Math.Round(avg), Math.Round(avg - TerrestrialPlanetParams.DefaultSurfaceTemperature, 2));
            sb.AppendLine();

            var water = grid.Tiles.Any(x => x.Elevation <= 0);
            var min = water ? grid.Tiles
                .Where(x => x.Elevation <= 0)
                .Min(x => x.Temperature.Min)
                : 0;
            sb.AppendFormat("  Min Sea-Level:           {0} K", Math.Round(min));
            sb.AppendLine();

            var avgSeaLevel = water ? grid.Tiles
                .Where(x => x.Elevation <= 0)
                .Average(x => x.Temperature.Average)
                : 0;
            sb.AppendFormat("  Avg Sea-Level:           {0} K", Math.Round(avgSeaLevel));
            sb.AppendLine();

            sb.AppendFormat("  Max:                     {0} K", Math.Round(grid.Tiles.Max(x => x.Temperature.Max)));
            sb.AppendLine();

            var maxAvg = grid.Tiles.Max(x => x.Temperature.Average);
            sb.AppendFormat("  Max Avg:          {0} K", Math.Round(maxAvg));
            sb.AppendLine();

            var minMaxTemp = water ? grid.Tiles
                .Where(x => x.Elevation <= 0)
                .Select(x => x.Temperature.Max)
                .OrderBy(x => x)
                .First()
                : 0;
            sb.AppendFormat("  Min Max (water):  {0} K", Math.Round(minMaxTemp));
            sb.AppendLine();
        }

        private static void AddSimpleTerrainString(StringBuilder sb, TerrestrialPlanet planet, WorldGrid grid)
        {
            sb.AppendFormat("Sea Level:                 {0}m", Math.Round(planet.SeaLevel));
            sb.AppendLine();
            sb.AppendFormat("Avg Land Elevation:        {0}m", Math.Round(grid.Tiles.Where(t => t.Elevation > 0).Average(t => t.Elevation)));
            sb.AppendLine();
            sb.AppendFormat("Max Elevation:             {0}m / {1}m", Math.Round(grid.Tiles.Max(t => t.Elevation)), Math.Round(planet.MaxElevation));
            sb.AppendLine();
        }

#pragma warning disable RCS1213 // Remove unused member declaration.
        private static void AddTerrainString(StringBuilder sb, TerrestrialPlanet planet, WorldGrid grid)
        {
            var land = grid.Tiles.Count(t => t.Elevation > 0 && t.Corners.All(c => grid.Corners[c].Elevation > 0)) * 100.0 / grid.Tiles.Length;
            var water = grid.Tiles.Count(t => t.Elevation <= 0 && t.Corners.All(c => grid.Corners[c].Elevation <= 0)) * 100.0 / grid.Tiles.Length;
            sb.AppendFormat("% Land:                    {0}%", Math.Round(land, 2));
            sb.AppendLine();
            sb.AppendFormat("% Coast:                   {0}%", Math.Round(100 - (land + water), 2));
            sb.AppendLine();
            sb.AppendFormat("% Water:                   {0}%", Math.Round(water, 2));
            sb.AppendLine();
            sb.AppendFormat("Sea Level:                 {0}m", Math.Round(planet.SeaLevel));
            sb.AppendLine();
            sb.AppendFormat("Max Elevation:             {0}m", Math.Round(planet.MaxElevation));
            sb.AppendLine();
            var min = grid.Tiles.Min(t => t.Elevation + planet.SeaLevel);
            sb.AppendFormat("Abs. Min:                  {0}m", Math.Round(min));
            sb.AppendLine();
            var max = grid.Tiles.Max(t => t.Elevation + planet.SeaLevel);
            sb.AppendFormat("Abs. Max:                  {0}m", Math.Round(max));
            sb.AppendLine();
            sb.AppendFormat("Range:                     {0}%", Math.Round((max - min) * 50.0 / planet.MaxElevation, 2));
            sb.AppendLine();
            sb.AppendFormat("Balance:                   {0}", Math.Round(Math.Min(max, -min) / Math.Max(max, -min), 2));
            sb.AppendLine();
            sb.AppendFormat("Noise:                     {0}%", Math.Round(grid.Tiles.Count(t =>
                t.Corners.Select(c => grid.Corners[c]).Any(c => Math.Abs(c.Elevation - t.Elevation) > planet.MaxElevation / 10)) * 100.0 / grid.Tiles.Length, 2));
            sb.AppendLine();
            sb.AppendLine("Land Elevation:");
            var list = grid.Tiles.Where(t => t.Elevation > 0).OrderBy(t => t.Elevation).ToList();
            sb.AppendFormat("  Min:                     {0}m", Math.Round(list.Min(t => t.Elevation)));
            sb.AppendLine();
            sb.AppendFormat("  Avg:                     {0}m", Math.Round(list.Average(t => t.Elevation)));
            sb.AppendLine();
            sb.AppendFormat("  Avg (<=P90):             {0}m", Math.Round(list.Take((int)Math.Floor(list.Count * 0.9)).Average(t => t.Elevation)));
            sb.AppendLine();
            sb.AppendFormat("  P10:                     {0}m", Math.Round(list.Skip((int)Math.Floor(list.Count * 0.1)).First().Elevation));
            sb.AppendLine();
            sb.AppendFormat("  P20:                     {0}m", Math.Round(list.Skip((int)Math.Floor(list.Count * 0.2)).First().Elevation));
            sb.AppendLine();
            sb.AppendFormat("  P30:                     {0}m", Math.Round(list.Skip((int)Math.Floor(list.Count * 0.3)).First().Elevation));
            sb.AppendLine();
            sb.AppendFormat("  P40:                     {0}m", Math.Round(list.Skip((int)Math.Floor(list.Count * 0.4)).First().Elevation));
            sb.AppendLine();
            sb.AppendFormat("  P50:                     {0}m", Math.Round(list.Skip((int)Math.Floor(list.Count * 0.5)).First().Elevation));
            sb.AppendLine();
            sb.AppendFormat("  P60:                     {0}m", Math.Round(list.Skip((int)Math.Floor(list.Count * 0.6)).First().Elevation));
            sb.AppendLine();
            sb.AppendFormat("  P70:                     {0}m", Math.Round(list.Skip((int)Math.Floor(list.Count * 0.7)).First().Elevation));
            sb.AppendLine();
            sb.AppendFormat("  P80:                     {0}m", Math.Round(list.Skip((int)Math.Floor(list.Count * 0.8)).First().Elevation));
            sb.AppendLine();
            sb.AppendFormat("  P90:                     {0}m", Math.Round(list.Skip((int)Math.Floor(list.Count * 0.9)).First().Elevation));
            sb.AppendLine();
            sb.AppendFormat("  Max:                     {0}m", Math.Round(grid.Tiles.Max(t => t.Elevation)));
            sb.AppendLine();
        }
#pragma warning restore RCS1213 // Remove unused member declaration.
    }
}
