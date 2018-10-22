using Antmicro.Migrant;
using Antmicro.Migrant.Customization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Text;
using WorldFoundry.CelestialBodies.Planetoids.Planets.TerrestrialPlanets;
using WorldFoundry.Climate;

namespace WorldFoundry.Test
{
    [TestClass]
    public class TerrestrialPlanet_Tests
    {
        private static Serializer Serializer;
        private static Settings SerializerSettings;
        private const int GridSize = 6;
        private const double GridTileRadius = 160000;
        private const int MaxGridSize = 7;
        private const int NumSeasons = 4;

#pragma warning disable RCS1163 // Unused parameter.
        [ClassInitialize]
        public static void Init(TestContext context)
        {
            Serializer = new Serializer();
            Serializer.ForObject<Type>().SetSurrogate(x => Activator.CreateInstance(typeof(TypeSurrogate<>).MakeGenericType(new[] { x })));
            Serializer.ForSurrogate<ITypeSurrogate>().SetObject(x => x.Restore());
            SerializerSettings = new Settings(versionTolerance: VersionToleranceLevel.AllowAssemblyVersionChange | VersionToleranceLevel.AllowFieldAddition | VersionToleranceLevel.AllowFieldRemoval);
        }
#pragma warning restore RCS1163 // Unused parameter.

        [TestMethod]
        public void TerrestrialPlanet_Generate()
        {
            //var planetParams = TerrestrialPlanetParams.FromDefaults(gridSize: gridSize);
            var planetParams = TerrestrialPlanetParams.FromDefaults(gridTileRadius: GridTileRadius, maxGridSize: MaxGridSize);

            var planet = TerrestrialPlanet.DefaultHumanPlanetNewUniverse(planetParams);
            Assert.IsNotNull(planet);

            Console.WriteLine($"Tiles: {planet.Grid.Tiles.Length.ToString()}");
            Console.WriteLine($"Radius: {(planet.Radius / 1000).ToString("N0")} km");
            Console.WriteLine($"Surface area: {(planet.Grid.Tiles.Sum(x => x.Area) / 1e6).ToString("N0")} km²");
            Console.WriteLine($"Tile area: {((planet.Grid.Tiles.Length > 12 ? planet.Grid.Tiles[12].Area : planet.Grid.Tiles[11].Area) / 1e6).ToString("N0")} km²");
        }

        [TestMethod]
        public void TerrestrialPlanet_Generate_WithSeasons()
        {
            var planetParams = TerrestrialPlanetParams.FromDefaults(gridSize: GridSize);

            var planet = TerrestrialPlanet.DefaultHumanPlanetNewUniverse(planetParams);
            Assert.IsNotNull(planet);

            Console.WriteLine($"Tiles: {planet.Grid.Tiles.Length.ToString()}");
            Console.WriteLine($"Tile Area: {Math.Round((planet.Grid.Tiles.Length > 12 ? planet.Grid.Tiles[12].Area : planet.Grid.Tiles[11].Area) / 1e6).ToString("N0")}km²");
            Console.WriteLine();

            var seasons = planet.GetSeasons(NumSeasons)?.ToList();
            Assert.IsNotNull(seasons);
            Assert.AreEqual(NumSeasons, seasons.Count);
            Assert.IsNotNull(planet.Seasons);
            Assert.AreEqual(NumSeasons, planet.Seasons.Count);

            var sb = new StringBuilder();

            AddTempString(sb, planet);
            sb.AppendLine();
            AddSimpleTerrainString(sb, planet);
            sb.AppendLine();
            AddSimpleClimateString(sb, planet);
            sb.AppendLine();
            AddPrecipitationString(sb, planet);

            Console.WriteLine(sb.ToString());
        }

        [TestMethod]
        public void TerrestrialPlanet_Save()
        {
            var planetParams = TerrestrialPlanetParams.FromDefaults(gridSize: GridSize);

            var planet = TerrestrialPlanet.DefaultHumanPlanetNewUniverse(planetParams);
            Assert.IsNotNull(planet);

            var stringData = SaveAsString(planet);
            Assert.IsNotNull(stringData);

            Console.WriteLine($"Saved size: {Encoding.Unicode.GetByteCount(stringData).ToString()}");
        }

        [TestMethod]
        public void TerrestrialPlanet_Save_Load()
        {
            var planetParams = TerrestrialPlanetParams.FromDefaults(gridSize: GridSize);

            var planet = TerrestrialPlanet.DefaultHumanPlanetNewUniverse(planetParams);
            Assert.IsNotNull(planet);
            Assert.IsNotNull(planet.PlanetParams);

            var stringData = SaveAsString(planet);
            Assert.IsNotNull(stringData);

            planet = LoadFromString(stringData);
            Assert.IsNotNull(planet);
            Assert.IsNotNull(planet.PlanetParams);
            Assert.AreEqual(GridSize, planet.PlanetParams.GridSize);
        }

        [TestMethod]
        public void TerrestrialPlanet_Save_WithSeasons()
        {
            var planetParams = TerrestrialPlanetParams.FromDefaults(gridSize: GridSize);

            var planet = TerrestrialPlanet.DefaultHumanPlanetNewUniverse(planetParams);
            Assert.IsNotNull(planet);
            planet.GetSeasons(NumSeasons).ToList();

            var stringData = SaveAsString(planet);
            Assert.IsNotNull(stringData);

            Console.WriteLine($"Saved size: {Encoding.Unicode.GetByteCount(stringData).ToString()}");
        }

        [TestMethod]
        public void TerrestrialPlanet_Save_Load_WithSeasons()
        {
            var planetParams = TerrestrialPlanetParams.FromDefaults(gridSize: GridSize);

            var planet = TerrestrialPlanet.DefaultHumanPlanetNewUniverse(planetParams);
            Assert.IsNotNull(planet);
            planet.GetSeasons(NumSeasons).ToList();

            var stringData = SaveAsString(planet);
            Assert.IsNotNull(stringData);

            planet = LoadFromString(stringData);
            Assert.IsNotNull(planet);

            Assert.IsNotNull(planet.Seasons);
            Assert.AreEqual(NumSeasons, planet.Seasons.Count);
        }

#pragma warning disable RCS1213 // Remove unused member declaration.
        private static void AddClimateString(StringBuilder sb, TerrestrialPlanet planet)
        {
            if (planet.Grid.Tiles[0].BiomeType == BiomeType.None)
            {
                return;
            }

            sb.AppendLine("Climates:");
            sb.AppendFormat("  % Desert (land):         {0}%", planet.Grid.Tiles.Count(t => t.BiomeType == BiomeType.HotDesert || t.BiomeType == BiomeType.ColdDesert || (t.ClimateType == ClimateType.Polar && t.EcologyType == EcologyType.Desert)) * 100.0 / planet.Grid.Tiles.Count(t => t.TerrainType != TerrainType.Water));
            sb.AppendLine();
            sb.AppendFormat("  % Rain Forest (land):    {0}%", planet.Grid.Tiles.Count(t => t.BiomeType == BiomeType.RainForest) * 100.0 / planet.Grid.Tiles.Count(t => t.TerrainType != TerrainType.Water));
            sb.AppendLine();
            sb.AppendFormat("  Polar:                   {0}", planet.Grid.Tiles.Count(t => t.ClimateType == ClimateType.Polar));
            sb.AppendLine();
            sb.AppendFormat("    Desert:                {0}", planet.Grid.Tiles.Count(t => t.ClimateType == ClimateType.Polar && t.EcologyType == EcologyType.Desert));
            sb.AppendLine();
            sb.AppendFormat("    Ice:                   {0}", planet.Grid.Tiles.Count(t => t.ClimateType == ClimateType.Polar && t.EcologyType == EcologyType.Ice));
            sb.AppendLine();
            sb.AppendFormat("  Subpolar:                {0}", planet.Grid.Tiles.Count(t => t.ClimateType == ClimateType.Subpolar));
            sb.AppendLine();
            sb.AppendFormat("    Dry Tundra:            {0}", planet.Grid.Tiles.Count(t => t.ClimateType == ClimateType.Subpolar && t.EcologyType == EcologyType.DryTundra));
            sb.AppendLine();
            sb.AppendFormat("    Moist Tundra:          {0}", planet.Grid.Tiles.Count(t => t.ClimateType == ClimateType.Subpolar && t.EcologyType == EcologyType.MoistTundra));
            sb.AppendLine();
            sb.AppendFormat("    Wet Tundra:            {0}", planet.Grid.Tiles.Count(t => t.ClimateType == ClimateType.Subpolar && t.EcologyType == EcologyType.WetTundra));
            sb.AppendLine();
            sb.AppendFormat("    Rain Tundra:           {0}", planet.Grid.Tiles.Count(t => t.ClimateType == ClimateType.Subpolar && t.EcologyType == EcologyType.RainTundra));
            sb.AppendLine();
            sb.AppendFormat("  Boreal:                  {0}", planet.Grid.Tiles.Count(t => t.ClimateType == ClimateType.Boreal));
            sb.AppendLine();
            sb.AppendFormat("    Desert:                {0}", planet.Grid.Tiles.Count(t => t.ClimateType == ClimateType.Boreal && t.EcologyType == EcologyType.Desert));
            sb.AppendLine();
            sb.AppendFormat("    Dry Scrub:             {0}", planet.Grid.Tiles.Count(t => t.ClimateType == ClimateType.Boreal && t.EcologyType == EcologyType.DryScrub));
            sb.AppendLine();
            sb.AppendFormat("    Moist Forest:          {0}", planet.Grid.Tiles.Count(t => t.ClimateType == ClimateType.Boreal && t.EcologyType == EcologyType.MoistForest));
            sb.AppendLine();
            sb.AppendFormat("    Wet Forest:            {0}", planet.Grid.Tiles.Count(t => t.ClimateType == ClimateType.Boreal && t.EcologyType == EcologyType.WetForest));
            sb.AppendLine();
            sb.AppendFormat("    Rain Forest:           {0}", planet.Grid.Tiles.Count(t => t.ClimateType == ClimateType.Boreal && t.EcologyType == EcologyType.RainForest));
            sb.AppendLine();
            sb.AppendFormat("  Cool Temperate:          {0}", planet.Grid.Tiles.Count(t => t.ClimateType == ClimateType.CoolTemperate));
            sb.AppendLine();
            sb.AppendFormat("    Desert:                {0}", planet.Grid.Tiles.Count(t => t.ClimateType == ClimateType.CoolTemperate && t.EcologyType == EcologyType.Desert));
            sb.AppendLine();
            sb.AppendFormat("    Desert Scrub:          {0}", planet.Grid.Tiles.Count(t => t.ClimateType == ClimateType.CoolTemperate && t.EcologyType == EcologyType.DesertScrub));
            sb.AppendLine();
            sb.AppendFormat("    Steppe:                {0}", planet.Grid.Tiles.Count(t => t.ClimateType == ClimateType.CoolTemperate && t.EcologyType == EcologyType.Steppe));
            sb.AppendLine();
            sb.AppendFormat("    Moist Forest:          {0}", planet.Grid.Tiles.Count(t => t.ClimateType == ClimateType.CoolTemperate && t.EcologyType == EcologyType.MoistForest));
            sb.AppendLine();
            sb.AppendFormat("    Wet Forest:            {0}", planet.Grid.Tiles.Count(t => t.ClimateType == ClimateType.CoolTemperate && t.EcologyType == EcologyType.WetForest));
            sb.AppendLine();
            sb.AppendFormat("    Rain Forest:           {0}", planet.Grid.Tiles.Count(t => t.ClimateType == ClimateType.CoolTemperate && t.EcologyType == EcologyType.RainForest));
            sb.AppendLine();
            sb.AppendFormat("  Warm Temperate:          {0}", planet.Grid.Tiles.Count(t => t.ClimateType == ClimateType.WarmTemperate));
            sb.AppendLine();
            sb.AppendFormat("    Desert:                {0}", planet.Grid.Tiles.Count(t => t.ClimateType == ClimateType.WarmTemperate && t.EcologyType == EcologyType.Desert));
            sb.AppendLine();
            sb.AppendFormat("    Desert Scrub:          {0}", planet.Grid.Tiles.Count(t => t.ClimateType == ClimateType.WarmTemperate && t.EcologyType == EcologyType.DesertScrub));
            sb.AppendLine();
            sb.AppendFormat("    Thorn Scrub:           {0}", planet.Grid.Tiles.Count(t => t.ClimateType == ClimateType.WarmTemperate && t.EcologyType == EcologyType.ThornScrub));
            sb.AppendLine();
            sb.AppendFormat("    Dry Forest:            {0}", planet.Grid.Tiles.Count(t => t.ClimateType == ClimateType.WarmTemperate && t.EcologyType == EcologyType.DryForest));
            sb.AppendLine();
            sb.AppendFormat("    Moist Forest:          {0}", planet.Grid.Tiles.Count(t => t.ClimateType == ClimateType.WarmTemperate && t.EcologyType == EcologyType.MoistForest));
            sb.AppendLine();
            sb.AppendFormat("    Wet Forest:            {0}", planet.Grid.Tiles.Count(t => t.ClimateType == ClimateType.WarmTemperate && t.EcologyType == EcologyType.WetForest));
            sb.AppendLine();
            sb.AppendFormat("    Rain Forest:           {0}", planet.Grid.Tiles.Count(t => t.ClimateType == ClimateType.WarmTemperate && t.EcologyType == EcologyType.RainForest));
            sb.AppendLine();
            sb.AppendFormat("  Subtropical:             {0}", planet.Grid.Tiles.Count(t => t.ClimateType == ClimateType.Subtropical));
            sb.AppendLine();
            sb.AppendFormat("    Desert:                {0}", planet.Grid.Tiles.Count(t => t.ClimateType == ClimateType.Subtropical && t.EcologyType == EcologyType.Desert));
            sb.AppendLine();
            sb.AppendFormat("    Desert Scrub:          {0}", planet.Grid.Tiles.Count(t => t.ClimateType == ClimateType.Subtropical && t.EcologyType == EcologyType.DesertScrub));
            sb.AppendLine();
            sb.AppendFormat("    Thorn Woodland:        {0}", planet.Grid.Tiles.Count(t => t.ClimateType == ClimateType.Subtropical && t.EcologyType == EcologyType.ThornWoodland));
            sb.AppendLine();
            sb.AppendFormat("    Dry Forest:            {0}", planet.Grid.Tiles.Count(t => t.ClimateType == ClimateType.Subtropical && t.EcologyType == EcologyType.DryForest));
            sb.AppendLine();
            sb.AppendFormat("    Moist Forest:          {0}", planet.Grid.Tiles.Count(t => t.ClimateType == ClimateType.Subtropical && t.EcologyType == EcologyType.MoistForest));
            sb.AppendLine();
            sb.AppendFormat("    Wet Forest:            {0}", planet.Grid.Tiles.Count(t => t.ClimateType == ClimateType.Subtropical && t.EcologyType == EcologyType.WetForest));
            sb.AppendLine();
            sb.AppendFormat("    Rain Forest:           {0}", planet.Grid.Tiles.Count(t => t.ClimateType == ClimateType.Subtropical && t.EcologyType == EcologyType.RainForest));
            sb.AppendLine();
            sb.AppendFormat("  Tropical:                {0}", planet.Grid.Tiles.Count(t => t.ClimateType == ClimateType.Tropical));
            sb.AppendLine();
            sb.AppendFormat("    Desert:                {0}", planet.Grid.Tiles.Count(t => t.ClimateType == ClimateType.Tropical && t.EcologyType == EcologyType.Desert));
            sb.AppendLine();
            sb.AppendFormat("    Desert Scrub:          {0}", planet.Grid.Tiles.Count(t => t.ClimateType == ClimateType.Tropical && t.EcologyType == EcologyType.DesertScrub));
            sb.AppendLine();
            sb.AppendFormat("    Thorn Woodland:        {0}", planet.Grid.Tiles.Count(t => t.ClimateType == ClimateType.Tropical && t.EcologyType == EcologyType.ThornWoodland));
            sb.AppendLine();
            sb.AppendFormat("    Very Dry Forest:       {0}", planet.Grid.Tiles.Count(t => t.ClimateType == ClimateType.Tropical && t.EcologyType == EcologyType.VeryDryForest));
            sb.AppendLine();
            sb.AppendFormat("    Dry Forest:            {0}", planet.Grid.Tiles.Count(t => t.ClimateType == ClimateType.Tropical && t.EcologyType == EcologyType.DryForest));
            sb.AppendLine();
            sb.AppendFormat("    Moist Forest:          {0}", planet.Grid.Tiles.Count(t => t.ClimateType == ClimateType.Tropical && t.EcologyType == EcologyType.MoistForest));
            sb.AppendLine();
            sb.AppendFormat("    Wet Forest:            {0}", planet.Grid.Tiles.Count(t => t.ClimateType == ClimateType.Tropical && t.EcologyType == EcologyType.WetForest));
            sb.AppendLine();
            sb.AppendFormat("    Rain Forest:           {0}", planet.Grid.Tiles.Count(t => t.ClimateType == ClimateType.Tropical && t.EcologyType == EcologyType.RainForest));
            sb.AppendLine();
        }
#pragma warning restore RCS1213 // Remove unused member declaration.

        private static void AddSimpleClimateString(StringBuilder sb, TerrestrialPlanet planet)
        {
            if (planet.Grid.Tiles[0].BiomeType == BiomeType.None)
            {
                return;
            }

            var landTiles = planet.Grid.Tiles.Count(t => t.TerrainType != TerrainType.Water);
            sb.AppendLine("Climates:");
            var desert = planet.Grid.Tiles.Count(t => t.BiomeType == BiomeType.HotDesert || t.BiomeType == BiomeType.ColdDesert || (t.ClimateType == ClimateType.Polar && t.EcologyType == EcologyType.Desert)) * 100.0 / landTiles;
            sb.AppendFormat("  Desert (all):            {0}% ({1})", Math.Round(desert, 2), Math.Round(desert - 30, 2));
            sb.AppendLine();
            var nonPolarDesert = planet.Grid.Tiles.Count(t => t.BiomeType == BiomeType.HotDesert || t.BiomeType == BiomeType.ColdDesert) * 100.0 / landTiles;
            sb.AppendFormat("  Desert (non-polar):      {0}% ({1})", Math.Round(nonPolarDesert, 2), Math.Round(nonPolarDesert - 14, 2));
            sb.AppendLine();
            var polar = planet.Grid.Tiles.Count(t => t.ClimateType == ClimateType.Polar) * 100.0 / planet.Grid.Tiles.Length;
            sb.AppendFormat("  Polar:                   {0}% ({1})", Math.Round(polar, 2), Math.Round(polar - 20, 2));
            sb.AppendLine();
            sb.AppendFormat("  Tundra:                  {0}%", Math.Round(planet.Grid.Tiles.Count(t => t.BiomeType == BiomeType.Tundra) * 100.0 / landTiles, 2));
            sb.AppendLine();
            sb.AppendFormat("  Boreal:                  {0}%", Math.Round(planet.Grid.Tiles.Count(t => t.TerrainType != TerrainType.Water && t.ClimateType == ClimateType.Boreal) * 100.0 / landTiles, 2));
            sb.AppendLine();
            sb.AppendFormat("    Lichen Woodland:       {0}%", Math.Round(planet.Grid.Tiles.Count(t => t.BiomeType == BiomeType.LichenWoodland) * 100.0 / landTiles, 2));
            sb.AppendLine();
            sb.AppendFormat("    Coniferous Forest:     {0}%", Math.Round(planet.Grid.Tiles.Count(t => t.BiomeType == BiomeType.ConiferousForest) * 100.0 / landTiles, 2));
            sb.AppendLine();
            sb.AppendFormat("  Cool Temperate:          {0}%", Math.Round(planet.Grid.Tiles.Count(t => t.TerrainType != TerrainType.Water && t.ClimateType == ClimateType.CoolTemperate) * 100.0 / landTiles, 2));
            sb.AppendLine();
            sb.AppendFormat("    Cold Desert:           {0}%", Math.Round(planet.Grid.Tiles.Count(t => t.BiomeType == BiomeType.ColdDesert) * 100.0 / landTiles, 2));
            sb.AppendLine();
            sb.AppendFormat("    Steppe:                {0}%", Math.Round(planet.Grid.Tiles.Count(t => t.BiomeType == BiomeType.Steppe) * 100.0 / landTiles, 2));
            sb.AppendLine();
            sb.AppendFormat("    Mixed Forest:          {0}%", Math.Round(planet.Grid.Tiles.Count(t => t.BiomeType == BiomeType.MixedForest) * 100.0 / landTiles, 2));
            sb.AppendLine();
            sb.AppendFormat("  Warm Temperate:          {0}%", Math.Round(planet.Grid.Tiles.Count(t => t.TerrainType != TerrainType.Water && t.ClimateType == ClimateType.WarmTemperate) * 100.0 / landTiles, 2));
            sb.AppendLine();
            sb.AppendFormat("    Desert:                {0}%", Math.Round(planet.Grid.Tiles.Count(t => t.ClimateType == ClimateType.WarmTemperate && t.BiomeType == BiomeType.HotDesert) * 100.0 / landTiles, 2));
            sb.AppendLine();
            sb.AppendFormat("    Shrubland:             {0}%", Math.Round(planet.Grid.Tiles.Count(t => t.BiomeType == BiomeType.Shrubland) * 100.0 / landTiles, 2));
            sb.AppendLine();
            sb.AppendFormat("    Deciduous Forest:      {0}%", Math.Round(planet.Grid.Tiles.Count(t => t.BiomeType == BiomeType.DeciduousForest) * 100.0 / landTiles, 2));
            sb.AppendLine();
            sb.AppendFormat("  Tropical:                {0}%", Math.Round(planet.Grid.Tiles.Count(t => t.TerrainType != TerrainType.Water && (t.ClimateType == ClimateType.Subtropical || t.ClimateType == ClimateType.Tropical || t.ClimateType == ClimateType.Supertropical)) * 100.0 / landTiles, 2));
            sb.AppendLine();
            sb.AppendFormat("    Desert:                {0}%", Math.Round(planet.Grid.Tiles.Count(t => t.ClimateType != ClimateType.WarmTemperate && t.BiomeType == BiomeType.HotDesert) * 100.0 / landTiles, 2));
            sb.AppendLine();
            sb.AppendFormat("    Savanna:               {0}%", Math.Round(planet.Grid.Tiles.Count(t => t.BiomeType == BiomeType.Savanna) * 100.0 / landTiles, 2));
            sb.AppendLine();
            sb.AppendFormat("    Monsoon Forest:        {0}%", Math.Round(planet.Grid.Tiles.Count(t => t.BiomeType == BiomeType.MonsoonForest) * 100.0 / landTiles, 2));
            sb.AppendLine();
            var rainforest = planet.Grid.Tiles.Count(t => t.BiomeType == BiomeType.RainForest) * 100.0 / landTiles;
            sb.AppendFormat("    Rain Forest:           {0}% ({1})", Math.Round(rainforest, 2), Math.Round(rainforest - 6, 2));
            sb.AppendLine();
        }

        private static void AddPrecipitationString(StringBuilder sb, TerrestrialPlanet planet)
        {
            sb.AppendLine("Precipitation (annual average, land):");
            var list = planet.Grid.Tiles.Where(x => x.TerrainType != TerrainType.Water)
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
                var avgT = planet.Grid.Tiles
                    .Where(t => t.TerrainType != TerrainType.Water && (t.ClimateType == ClimateType.CoolTemperate || t.ClimateType == ClimateType.WarmTemperate))
                    .Select(t => t.Precipitation).Average();
                sb.AppendFormat("  Avg (Temperate):         {0}mm ({1})", Math.Round(avgT), Math.Round(avgT - 1100, 2));
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

        private static void AddTempString(StringBuilder sb, TerrestrialPlanet planet)
        {
            sb.AppendLine("Temp:");
            var avg = planet.Grid.Tiles.Average(x => x.Temperature.Avg);
            sb.AppendFormat("  Avg:                     {0} K ({1})", Math.Round(avg), Math.Round(avg - planet.PlanetParams.SurfaceTemperature.Value, 2));
            sb.AppendLine();

            var water = planet.Grid.Tiles.Any(x => x.TerrainType == TerrainType.Water);
            var min = water ? planet.Grid.Tiles
                .Where(x => x.TerrainType == TerrainType.Water)
                .Min(x => x.Temperature.Min)
                : 0;
            sb.AppendFormat("  Min Sea-Level:           {0} K", Math.Round(min));
            sb.AppendLine();

            var avgSeaLevel = water ? planet.Grid.Tiles
                .Where(x => x.TerrainType == TerrainType.Water)
                .Average(x => x.Temperature.Avg)
                : 0;
            sb.AppendFormat("  Avg Sea-Level:           {0} K", Math.Round(avgSeaLevel));
            sb.AppendLine();

            sb.AppendFormat("  Max:                     {0} K", Math.Round(planet.Grid.Tiles.Max(x => x.Temperature.Max)));
            sb.AppendLine();

            var maxAvg = planet.Grid.Tiles.Max(x => x.Temperature.Avg);
            sb.AppendFormat("  Max Avg:          {0} K", Math.Round(maxAvg));
            sb.AppendLine();

            var (minMaxTemp, minMaxIndex) = water ? planet.Grid.Tiles
                .Where(x => x.TerrainType == TerrainType.Water)
                .Select(x => (temp: x.Temperature.Max, x.Index))
                .OrderBy(x => x.temp)
                .First()
                : (0, 0);
            sb.AppendFormat("  Min Max (water):  {0} K [{1}]", Math.Round(minMaxTemp), minMaxIndex);
            sb.AppendLine();

            sb.AppendFormat("Avg Surface Pressure:      {0} kPa", Math.Round(planet.Grid.Tiles.Average(x => x.AtmosphericPressure.Avg), 3));
            sb.AppendLine();
        }

        private static void AddSimpleTerrainString(StringBuilder sb, TerrestrialPlanet planet)
        {
            sb.AppendFormat("Sea Level:                 {0}m", Math.Round(planet.Terrain.SeaLevel));
            sb.AppendLine();
            sb.AppendFormat("Avg Land Elevation:        {0}m", Math.Round(planet.Grid.Tiles.Where(t => t.Elevation > 0).Average(t => t.Elevation)));
            sb.AppendLine();
            sb.AppendFormat("Max Elevation:             {0}m / {1}m", Math.Round(planet.Grid.Tiles.Max(t => t.Elevation)), Math.Round(planet.Terrain.MaxElevation));
            sb.AppendLine();
        }

#pragma warning disable RCS1213 // Remove unused member declaration.
        private static void AddTerrainString(StringBuilder sb, TerrestrialPlanet planet)
        {
            sb.AppendFormat("% Land:                    {0}%", Math.Round(planet.Grid.Tiles.Count(t => t.TerrainType == TerrainType.Land) * 100.0 / planet.Grid.Tiles.Length), 2);
            sb.AppendLine();
            sb.AppendFormat("% Coast:                   {0}%", Math.Round(planet.Grid.Tiles.Count(t => t.TerrainType == TerrainType.Coast) * 100.0 / planet.Grid.Tiles.Length), 2);
            sb.AppendLine();
            sb.AppendFormat("% Water:                   {0}%", Math.Round(planet.Grid.Tiles.Count(t => t.TerrainType == TerrainType.Water) * 100.0 / planet.Grid.Tiles.Length), 2);
            sb.AppendLine();
            sb.AppendFormat("Sea Level:                 {0}m", Math.Round(planet.Terrain.SeaLevel));
            sb.AppendLine();
            sb.AppendFormat("Max Elevation:             {0}m", Math.Round(planet.Terrain.MaxElevation));
            sb.AppendLine();
            var min = planet.Grid.Tiles.Min(t => t.Elevation + planet.Terrain.SeaLevel);
            sb.AppendFormat("Abs. Min:                  {0}m", Math.Round(min));
            sb.AppendLine();
            var max = planet.Grid.Tiles.Max(t => t.Elevation + planet.Terrain.SeaLevel);
            sb.AppendFormat("Abs. Max:                  {0}m", Math.Round(max));
            sb.AppendLine();
            sb.AppendFormat("Range:                     {0}%", Math.Round((max - min) * 50.0 / planet.Terrain.MaxElevation, 2));
            sb.AppendLine();
            sb.AppendFormat("Balance:                   {0}", Math.Round(Math.Min(max, -min) / Math.Max(max, -min), 2));
            sb.AppendLine();
            sb.AppendFormat("Noise:                     {0}%", Math.Round(planet.Grid.Tiles.Count(t =>
                t.Corners.Select(c => planet.Grid.Corners[c]).Any(c => Math.Abs(c.Elevation - t.Elevation) > planet.Terrain.MaxElevation / 10)) * 100.0 / planet.Grid.Tiles.Length, 2));
            sb.AppendLine();
            sb.AppendLine("Land Elevation:");
            var list = planet.Grid.Tiles.Where(t => t.Elevation > 0).OrderBy(t => t.Elevation).ToList();
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
            sb.AppendFormat("  Max:                     {0}m", Math.Round(planet.Grid.Tiles.Max(t => t.Elevation)));
            sb.AppendLine();
        }
#pragma warning restore RCS1213 // Remove unused member declaration.

        private static TerrestrialPlanet LoadFromString(string data)
        {
            TerrestrialPlanet planet;
            using (var ms = new MemoryStream(Convert.FromBase64String(data)))
            {
                planet = Serializer.Deserialize<TerrestrialPlanet>(ms);
            }
            return planet;
        }

        private static string SaveAsString(TerrestrialPlanet planet)
        {
            string stringData;
            using (var ms = new MemoryStream())
            {
                Serializer.Serialize(planet, ms);

                stringData = Convert.ToBase64String(ms.ToArray());
            }
            return stringData;
        }
    }
}
