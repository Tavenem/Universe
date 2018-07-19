using Antmicro.Migrant;
using Antmicro.Migrant.Customization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
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
        private static Serializer _serializer;
        private static Settings _serializerSettings;
        private const int gridSize = 6;
        private const double gridTileRadius = 160000;
        private const int maxGridSize = 7;
        private const int numSeasons = 4;

        [ClassInitialize]
        public static void Init(TestContext context)
        {
            _serializer = new Serializer();
            _serializer.ForObject<Type>().SetSurrogate(x => Activator.CreateInstance(typeof(TypeSurrogate<>).MakeGenericType(new[] { x })));
            _serializer.ForSurrogate<ITypeSurrogate>().SetObject(x => x.Restore());
            _serializerSettings = new Settings(versionTolerance: VersionToleranceLevel.AllowAssemblyVersionChange | VersionToleranceLevel.AllowFieldAddition | VersionToleranceLevel.AllowFieldRemoval);
        }

        [TestMethod]
        public void TerrestrialPlanet_Generate()
        {
            //var planetParams = TerrestrialPlanetParams.FromDefaults(gridSize: gridSize);
            var planetParams = TerrestrialPlanetParams.FromDefaults(gridTileRadius: gridTileRadius, maxGridSize: maxGridSize);

            var planet = TerrestrialPlanet.DefaultHumanPlanetNewUniverse(planetParams);
            Assert.IsNotNull(planet);

            Console.WriteLine($"Tiles: {planet.Topography.Tiles.Length}");
            Console.WriteLine($"Radius: {planet.Radius / 1000} km");
            Console.WriteLine($"Surface area: {planet.Topography.Tiles.Sum(x => x.Area) / 1000000} km²");
            Console.WriteLine($"Tile area: {(planet.Topography.Tiles.Length > 12 ? planet.Topography.Tiles[12].Area : planet.Topography.Tiles[11].Area) / 1000000} km²");
        }

        [TestMethod]
        public void TerrestrialPlanet_Generate_WithSeasons()
        {
            var planetParams = TerrestrialPlanetParams.FromDefaults(gridSize: gridSize);

            var planet = TerrestrialPlanet.DefaultHumanPlanetNewUniverse(planetParams);
            Assert.IsNotNull(planet);

            Console.WriteLine($"Tiles: {planet.Topography.Tiles.Length}");

            var seasons = planet.GetSeasons(numSeasons)?.ToList();
            Assert.IsNotNull(seasons);
            Assert.AreEqual(numSeasons, seasons.Count);
            Assert.IsNotNull(planet.Seasons);
            Assert.AreEqual(numSeasons, planet.Seasons.Count);

            var sb = new StringBuilder();

            AddTempString(sb, planet, seasons);
            AddSimpleClimateString(sb, planet);
            AddPrecipitationString(sb, planet, seasons, numSeasons);

            Console.WriteLine(sb.ToString());
        }

        [TestMethod]
        public void TerrestrialPlanet_Save()
        {
            var planetParams = TerrestrialPlanetParams.FromDefaults(gridSize: gridSize);

            var planet = TerrestrialPlanet.DefaultHumanPlanetNewUniverse(planetParams);
            Assert.IsNotNull(planet);

            var stringData = SaveAsString(planet);
            Assert.IsNotNull(stringData);

            Console.WriteLine($"Saved size: {Encoding.Unicode.GetByteCount(stringData)}");
        }

        [TestMethod]
        public void TerrestrialPlanet_Save_Load()
        {
            var planetParams = TerrestrialPlanetParams.FromDefaults(gridSize: gridSize);

            var planet = TerrestrialPlanet.DefaultHumanPlanetNewUniverse(planetParams);
            Assert.IsNotNull(planet);
            Assert.IsNotNull(planet.PlanetParams);

            var stringData = SaveAsString(planet);
            Assert.IsNotNull(stringData);

            planet = LoadFromString(stringData);
            Assert.IsNotNull(planet);
            Assert.IsNotNull(planet.PlanetParams);
            Assert.AreEqual(gridSize, planet.PlanetParams.GridSize);
        }

        [TestMethod]
        public void TerrestrialPlanet_Save_WithSeasons()
        {
            var planetParams = TerrestrialPlanetParams.FromDefaults(gridSize: gridSize);

            var planet = TerrestrialPlanet.DefaultHumanPlanetNewUniverse(planetParams);
            Assert.IsNotNull(planet);
            planet.GetSeasons(numSeasons).ToList();

            var stringData = SaveAsString(planet);
            Assert.IsNotNull(stringData);

            Console.WriteLine($"Saved size: {Encoding.Unicode.GetByteCount(stringData)}");
        }

        [TestMethod]
        public void TerrestrialPlanet_Save_Load_WithSeasons()
        {
            var planetParams = TerrestrialPlanetParams.FromDefaults(gridSize: gridSize);

            var planet = TerrestrialPlanet.DefaultHumanPlanetNewUniverse(planetParams);
            Assert.IsNotNull(planet);
            planet.GetSeasons(numSeasons).ToList();

            var stringData = SaveAsString(planet);
            Assert.IsNotNull(stringData);

            planet = LoadFromString(stringData);
            Assert.IsNotNull(planet);

            Assert.IsNotNull(planet.Seasons);
            Assert.AreEqual(numSeasons, planet.Seasons.Count);
        }

        private static void AddClimateString(StringBuilder sb, TerrestrialPlanet planet)
        {
            if (planet.Topography.Tiles[0].BiomeType == BiomeType.None)
            {
                return;
            }

            sb.AppendLine("Climates:");
            sb.AppendFormat("  % Desert (land):         {0}%", planet.Topography.Tiles.Count(t => t.BiomeType == BiomeType.HotDesert || t.BiomeType == BiomeType.ColdDesert || (t.ClimateType == ClimateType.Polar && t.EcologyType == EcologyType.Desert)) * 100.0 / planet.Topography.Tiles.Count(t => t.TerrainType != TerrainType.Water));
            sb.AppendLine();
            sb.AppendFormat("  % Rain Forest (land):    {0}%", planet.Topography.Tiles.Count(t => t.BiomeType == BiomeType.RainForest) * 100.0 / planet.Topography.Tiles.Count(t => t.TerrainType != TerrainType.Water));
            sb.AppendLine();
            sb.AppendFormat("  Polar:                   {0}", planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.Polar));
            sb.AppendLine();
            sb.AppendFormat("    Desert:                {0}", planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.Polar && t.EcologyType == EcologyType.Desert));
            sb.AppendLine();
            sb.AppendFormat("    Ice:                   {0}", planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.Polar && t.EcologyType == EcologyType.Ice));
            sb.AppendLine();
            sb.AppendFormat("  Subpolar:                {0}", planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.Subpolar));
            sb.AppendLine();
            sb.AppendFormat("    Dry Tundra:            {0}", planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.Subpolar && t.EcologyType == EcologyType.DryTundra));
            sb.AppendLine();
            sb.AppendFormat("    Moist Tundra:          {0}", planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.Subpolar && t.EcologyType == EcologyType.MoistTundra));
            sb.AppendLine();
            sb.AppendFormat("    Wet Tundra:            {0}", planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.Subpolar && t.EcologyType == EcologyType.WetTundra));
            sb.AppendLine();
            sb.AppendFormat("    Rain Tundra:           {0}", planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.Subpolar && t.EcologyType == EcologyType.RainTundra));
            sb.AppendLine();
            sb.AppendFormat("  Boreal:                  {0}", planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.Boreal));
            sb.AppendLine();
            sb.AppendFormat("    Desert:                {0}", planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.Boreal && t.EcologyType == EcologyType.Desert));
            sb.AppendLine();
            sb.AppendFormat("    Dry Scrub:             {0}", planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.Boreal && t.EcologyType == EcologyType.DryScrub));
            sb.AppendLine();
            sb.AppendFormat("    Moist Forest:          {0}", planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.Boreal && t.EcologyType == EcologyType.MoistForest));
            sb.AppendLine();
            sb.AppendFormat("    Wet Forest:            {0}", planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.Boreal && t.EcologyType == EcologyType.WetForest));
            sb.AppendLine();
            sb.AppendFormat("    Rain Forest:           {0}", planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.Boreal && t.EcologyType == EcologyType.RainForest));
            sb.AppendLine();
            sb.AppendFormat("  Cool Temperate:          {0}", planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.CoolTemperate));
            sb.AppendLine();
            sb.AppendFormat("    Desert:                {0}", planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.CoolTemperate && t.EcologyType == EcologyType.Desert));
            sb.AppendLine();
            sb.AppendFormat("    Desert Scrub:          {0}", planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.CoolTemperate && t.EcologyType == EcologyType.DesertScrub));
            sb.AppendLine();
            sb.AppendFormat("    Steppe:                {0}", planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.CoolTemperate && t.EcologyType == EcologyType.Steppe));
            sb.AppendLine();
            sb.AppendFormat("    Moist Forest:          {0}", planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.CoolTemperate && t.EcologyType == EcologyType.MoistForest));
            sb.AppendLine();
            sb.AppendFormat("    Wet Forest:            {0}", planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.CoolTemperate && t.EcologyType == EcologyType.WetForest));
            sb.AppendLine();
            sb.AppendFormat("    Rain Forest:           {0}", planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.CoolTemperate && t.EcologyType == EcologyType.RainForest));
            sb.AppendLine();
            sb.AppendFormat("  Warm Temperate:          {0}", planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.WarmTemperate));
            sb.AppendLine();
            sb.AppendFormat("    Desert:                {0}", planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.WarmTemperate && t.EcologyType == EcologyType.Desert));
            sb.AppendLine();
            sb.AppendFormat("    Desert Scrub:          {0}", planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.WarmTemperate && t.EcologyType == EcologyType.DesertScrub));
            sb.AppendLine();
            sb.AppendFormat("    Thorn Scrub:           {0}", planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.WarmTemperate && t.EcologyType == EcologyType.ThornScrub));
            sb.AppendLine();
            sb.AppendFormat("    Dry Forest:            {0}", planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.WarmTemperate && t.EcologyType == EcologyType.DryForest));
            sb.AppendLine();
            sb.AppendFormat("    Moist Forest:          {0}", planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.WarmTemperate && t.EcologyType == EcologyType.MoistForest));
            sb.AppendLine();
            sb.AppendFormat("    Wet Forest:            {0}", planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.WarmTemperate && t.EcologyType == EcologyType.WetForest));
            sb.AppendLine();
            sb.AppendFormat("    Rain Forest:           {0}", planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.WarmTemperate && t.EcologyType == EcologyType.RainForest));
            sb.AppendLine();
            sb.AppendFormat("  Subtropical:             {0}", planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.Subtropical));
            sb.AppendLine();
            sb.AppendFormat("    Desert:                {0}", planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.Subtropical && t.EcologyType == EcologyType.Desert));
            sb.AppendLine();
            sb.AppendFormat("    Desert Scrub:          {0}", planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.Subtropical && t.EcologyType == EcologyType.DesertScrub));
            sb.AppendLine();
            sb.AppendFormat("    Thorn Woodland:        {0}", planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.Subtropical && t.EcologyType == EcologyType.ThornWoodland));
            sb.AppendLine();
            sb.AppendFormat("    Dry Forest:            {0}", planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.Subtropical && t.EcologyType == EcologyType.DryForest));
            sb.AppendLine();
            sb.AppendFormat("    Moist Forest:          {0}", planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.Subtropical && t.EcologyType == EcologyType.MoistForest));
            sb.AppendLine();
            sb.AppendFormat("    Wet Forest:            {0}", planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.Subtropical && t.EcologyType == EcologyType.WetForest));
            sb.AppendLine();
            sb.AppendFormat("    Rain Forest:           {0}", planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.Subtropical && t.EcologyType == EcologyType.RainForest));
            sb.AppendLine();
            sb.AppendFormat("  Tropical:                {0}", planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.Tropical));
            sb.AppendLine();
            sb.AppendFormat("    Desert:                {0}", planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.Tropical && t.EcologyType == EcologyType.Desert));
            sb.AppendLine();
            sb.AppendFormat("    Desert Scrub:          {0}", planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.Tropical && t.EcologyType == EcologyType.DesertScrub));
            sb.AppendLine();
            sb.AppendFormat("    Thorn Woodland:        {0}", planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.Tropical && t.EcologyType == EcologyType.ThornWoodland));
            sb.AppendLine();
            sb.AppendFormat("    Very Dry Forest:       {0}", planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.Tropical && t.EcologyType == EcologyType.VeryDryForest));
            sb.AppendLine();
            sb.AppendFormat("    Dry Forest:            {0}", planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.Tropical && t.EcologyType == EcologyType.DryForest));
            sb.AppendLine();
            sb.AppendFormat("    Moist Forest:          {0}", planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.Tropical && t.EcologyType == EcologyType.MoistForest));
            sb.AppendLine();
            sb.AppendFormat("    Wet Forest:            {0}", planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.Tropical && t.EcologyType == EcologyType.WetForest));
            sb.AppendLine();
            sb.AppendFormat("    Rain Forest:           {0}", planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.Tropical && t.EcologyType == EcologyType.RainForest));
            sb.AppendLine();
        }

        private static void AddSimpleClimateString(StringBuilder sb, TerrestrialPlanet planet)
        {
            if (planet.Topography.Tiles[0].BiomeType == BiomeType.None)
            {
                return;
            }

            sb.AppendLine("Climates:");
            sb.AppendFormat("  % Desert (land):         {0}%", planet.Topography.Tiles.Count(t => t.BiomeType == BiomeType.HotDesert || t.BiomeType == BiomeType.ColdDesert || (t.ClimateType == ClimateType.Polar && t.EcologyType == EcologyType.Desert)) * 100.0 / planet.Topography.Tiles.Count(t => t.TerrainType != TerrainType.Water));
            sb.AppendLine();
            sb.AppendFormat("  % Rain Forest (land):    {0}%", planet.Topography.Tiles.Count(t => t.BiomeType == BiomeType.RainForest) * 100.0 / planet.Topography.Tiles.Count(t => t.TerrainType != TerrainType.Water));
            sb.AppendLine();
            sb.AppendFormat("  Polar:                   {0}", planet.Topography.Tiles.Count(t => t.BiomeType == BiomeType.Polar));
            sb.AppendLine();
            sb.AppendFormat("  Tundra:                  {0}", planet.Topography.Tiles.Count(t => t.BiomeType == BiomeType.Tundra));
            sb.AppendLine();
            sb.AppendFormat("  Boreal:                  {0}", planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.Boreal));
            sb.AppendLine();
            sb.AppendFormat("    Lichen Woodland:       {0}", planet.Topography.Tiles.Count(t => t.BiomeType == BiomeType.LichenWoodland));
            sb.AppendLine();
            sb.AppendFormat("    Coniferous Forest:     {0}", planet.Topography.Tiles.Count(t => t.BiomeType == BiomeType.ConiferousForest));
            sb.AppendLine();
            sb.AppendFormat("  Cool Temperate:          {0}", planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.CoolTemperate));
            sb.AppendLine();
            sb.AppendFormat("    Cold Desert:           {0}", planet.Topography.Tiles.Count(t => t.BiomeType == BiomeType.ColdDesert));
            sb.AppendLine();
            sb.AppendFormat("    Steppe:                {0}", planet.Topography.Tiles.Count(t => t.BiomeType == BiomeType.Steppe));
            sb.AppendLine();
            sb.AppendFormat("    Mixed Forest:          {0}", planet.Topography.Tiles.Count(t => t.BiomeType == BiomeType.MixedForest));
            sb.AppendLine();
            sb.AppendFormat("  Warm Temperate:          {0}", planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.WarmTemperate));
            sb.AppendLine();
            sb.AppendFormat("    Shrubland:             {0}", planet.Topography.Tiles.Count(t => t.BiomeType == BiomeType.Shrubland));
            sb.AppendLine();
            sb.AppendFormat("    Deciduous Forest:      {0}", planet.Topography.Tiles.Count(t => t.BiomeType == BiomeType.DeciduousForest));
            sb.AppendLine();
            sb.AppendFormat("  Tropical:                {0}", planet.Topography.Tiles.Count(t => t.ClimateType == ClimateType.Subtropical || t.ClimateType == ClimateType.Tropical));
            sb.AppendLine();
            sb.AppendFormat("    Desert:                {0}", planet.Topography.Tiles.Count(t => t.BiomeType == BiomeType.HotDesert));
            sb.AppendLine();
            sb.AppendFormat("    Savanna:               {0}", planet.Topography.Tiles.Count(t => t.BiomeType == BiomeType.Savanna));
            sb.AppendLine();
            sb.AppendFormat("    Monsoon Forest:        {0}", planet.Topography.Tiles.Count(t => t.BiomeType == BiomeType.MonsoonForest));
            sb.AppendLine();
            sb.AppendFormat("    Rain Forest:           {0}", planet.Topography.Tiles.Count(t => t.BiomeType == BiomeType.RainForest));
            sb.AppendLine();
        }

        static void AddPrecipitationString(StringBuilder sb, TerrestrialPlanet planet, List<Season> seasons, double seasonsInYear)
        {
            sb.AppendLine("Precipitation (annual average, land):");
            var list = planet.Topography.Tiles.Where(x => x.TerrainType != TerrainType.Water)
                .Select(x => x.Precipitation)
                .ToList();
            if (list.Any())
            {
                list.Sort();
                sb.AppendFormat("  Avg:                     {0} mm", list.Count == 0 ? 0 : list.Average());
                sb.AppendLine();
                sb.AppendFormat("  Avg (<=P90):             {0} mm", list.Count == 0 ? 0 : list.Take((int)Math.Floor(list.Count * 0.9)).Average());
                sb.AppendLine();
                sb.AppendFormat("  P10:                     {0} mm", list.Count == 0 ? 0 : list.Skip((int)Math.Floor(list.Count * 0.1)).First());
                sb.AppendLine();
                sb.AppendFormat("  Q1:                      {0} mm", list.Count == 0 ? 0 : list.Skip((int)Math.Floor(list.Count * 0.25)).First());
                sb.AppendLine();
                sb.AppendFormat("  Q2:                      {0} mm", list.Count == 0 ? 0 : list.Skip((int)Math.Floor(list.Count * 0.5)).First());
                sb.AppendLine();
                sb.AppendFormat("  Q3:                      {0} mm", list.Count == 0 ? 0 : list.Skip((int)Math.Floor(list.Count * 0.75)).First());
                sb.AppendLine();
                sb.AppendFormat("  P90:                     {0} mm", list.Count == 0 ? 0 : list.Skip((int)Math.Floor(list.Count * 0.9)).First());
                sb.AppendLine();
                sb.AppendFormat("  Max:                     {0} mm", list.Count == 0 ? 0 : list.Last());
                sb.AppendLine();
            }
            else
            {
                sb.AppendLine("  No land.");
            }

            sb.AppendLine("  Selected Tiles:");
            sb.AppendFormat("    [1]:                 {0} mm ({1})",
                planet.Topography.Tiles[1].Precipitation,
                planet.Topography.Tiles[1].TerrainType);
            sb.AppendLine();
            sb.AppendFormat("    [2]:                 {0} mm ({1})",
                planet.Topography.Tiles[2].Precipitation,
                planet.Topography.Tiles[2].TerrainType);
            sb.AppendLine();
            sb.AppendFormat("    [3]:                 {0} mm ({1})",
                planet.Topography.Tiles[3].Precipitation,
                planet.Topography.Tiles[3].TerrainType);
            sb.AppendLine();
            sb.AppendFormat("    [4]:                 {0} mm ({1})",
                planet.Topography.Tiles[4].Precipitation,
                planet.Topography.Tiles[4].TerrainType);
            sb.AppendLine();
        }

        private static void AddTempString(StringBuilder sb, TerrestrialPlanet planet, List<Season> seasons)
        {
            sb.AppendLine("Temp:");
            sb.AppendFormat("  Avg:                     {0} K", planet.Topography.Tiles.Average(x => x.Temperature.Avg));
            sb.AppendLine();

            var water = planet.Topography.Tiles.Any(x => x.TerrainType == TerrainType.Water);
            var min = water ? planet.Topography.Tiles
                .Where(x => x.TerrainType == TerrainType.Water)
                .Min(x => x.Temperature.Min)
                : 0;
            sb.AppendFormat("  Min Sea-Level:           {0} K", min);
            sb.AppendLine();

            var avg = water ? planet.Topography.Tiles
                .Where(x => x.TerrainType == TerrainType.Water)
                .Average(x => x.Temperature.Avg)
                : 0;
            sb.AppendFormat("  Avg Sea-Level:           {0} K", avg);
            sb.AppendLine();

            sb.AppendFormat("  Max:                     {0} K", planet.Topography.Tiles.Max(x => x.Temperature.Max));
            sb.AppendLine();

            var maxAvg = planet.Topography.Tiles.Max(x => x.Temperature.Avg);
            sb.AppendFormat("  Max Avg:          {0} K", maxAvg);
            sb.AppendLine();

            var (minMaxTemp, minMaxIndex) = water ? planet.Topography.Tiles
                .Where(x => x.TerrainType == TerrainType.Water)
                .Select(x => (temp: x.Temperature.Max, x.Index))
                .OrderBy(x => x.temp)
                .First()
                : (0, 0);
            sb.AppendFormat("  Min Max (water):  {0} K [{1}]", minMaxTemp, minMaxIndex);
            sb.AppendLine();

            sb.AppendFormat("Avg Surface Pressure:      {0} kPa", planet.Topography.Tiles.Average(x => x.AtmosphericPressure.Avg));
            sb.AppendLine();
        }

        private static TerrestrialPlanet LoadFromString(string data)
        {
            TerrestrialPlanet planet;
            using (var ms = new MemoryStream(Convert.FromBase64String(data)))
            {
                planet = _serializer.Deserialize<TerrestrialPlanet>(ms);
            }
            return planet;
        }

        private static string SaveAsString(TerrestrialPlanet planet)
        {
            string stringData;
            using (var ms = new MemoryStream())
            {
                _serializer.Serialize(planet, ms);

                stringData = Convert.ToBase64String(ms.ToArray());
            }
            return stringData;
        }
    }
}
