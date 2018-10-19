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
            Console.WriteLine($"Radius: {(planet.Radius / 1000).ToString()} km");
            Console.WriteLine($"Surface area: {(planet.Grid.Tiles.Sum(x => x.Area) / 1000000).ToString()} km²");
            Console.WriteLine($"Tile area: {((planet.Grid.Tiles.Length > 12 ? planet.Grid.Tiles[12].Area : planet.Grid.Tiles[11].Area) / 1000000).ToString()} km²");
        }

        [TestMethod]
        public void TerrestrialPlanet_Generate_WithSeasons()
        {
            var planetParams = TerrestrialPlanetParams.FromDefaults(gridSize: GridSize);

            var planet = TerrestrialPlanet.DefaultHumanPlanetNewUniverse(planetParams);
            Assert.IsNotNull(planet);

            Console.WriteLine($"Tiles: {planet.Grid.Tiles.Length.ToString()}");

            var seasons = planet.GetSeasons(NumSeasons)?.ToList();
            Assert.IsNotNull(seasons);
            Assert.AreEqual(NumSeasons, seasons.Count);
            Assert.IsNotNull(planet.Seasons);
            Assert.AreEqual(NumSeasons, planet.Seasons.Count);

            var sb = new StringBuilder();

            AddTempString(sb, planet);
            AddTerrainString(sb, planet);
            AddSimpleClimateString(sb, planet);
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

            sb.AppendLine("Climates:");
            sb.AppendFormat("  % Desert (land):         {0}%", planet.Grid.Tiles.Count(t => t.BiomeType == BiomeType.HotDesert || t.BiomeType == BiomeType.ColdDesert || (t.ClimateType == ClimateType.Polar && t.EcologyType == EcologyType.Desert)) * 100.0 / planet.Grid.Tiles.Count(t => t.TerrainType != TerrainType.Water));
            sb.AppendLine();
            sb.AppendFormat("  % Rain Forest (land):    {0}%", planet.Grid.Tiles.Count(t => t.BiomeType == BiomeType.RainForest) * 100.0 / planet.Grid.Tiles.Count(t => t.TerrainType != TerrainType.Water));
            sb.AppendLine();
            sb.AppendFormat("  Polar:                   {0}", planet.Grid.Tiles.Count(t => t.BiomeType == BiomeType.Polar));
            sb.AppendLine();
            sb.AppendFormat("  Tundra:                  {0}", planet.Grid.Tiles.Count(t => t.BiomeType == BiomeType.Tundra));
            sb.AppendLine();
            sb.AppendFormat("  Boreal:                  {0}", planet.Grid.Tiles.Count(t => t.ClimateType == ClimateType.Boreal));
            sb.AppendLine();
            sb.AppendFormat("    Lichen Woodland:       {0}", planet.Grid.Tiles.Count(t => t.BiomeType == BiomeType.LichenWoodland));
            sb.AppendLine();
            sb.AppendFormat("    Coniferous Forest:     {0}", planet.Grid.Tiles.Count(t => t.BiomeType == BiomeType.ConiferousForest));
            sb.AppendLine();
            sb.AppendFormat("  Cool Temperate:          {0}", planet.Grid.Tiles.Count(t => t.ClimateType == ClimateType.CoolTemperate));
            sb.AppendLine();
            sb.AppendFormat("    Cold Desert:           {0}", planet.Grid.Tiles.Count(t => t.BiomeType == BiomeType.ColdDesert));
            sb.AppendLine();
            sb.AppendFormat("    Steppe:                {0}", planet.Grid.Tiles.Count(t => t.BiomeType == BiomeType.Steppe));
            sb.AppendLine();
            sb.AppendFormat("    Mixed Forest:          {0}", planet.Grid.Tiles.Count(t => t.BiomeType == BiomeType.MixedForest));
            sb.AppendLine();
            sb.AppendFormat("  Warm Temperate:          {0}", planet.Grid.Tiles.Count(t => t.ClimateType == ClimateType.WarmTemperate));
            sb.AppendLine();
            sb.AppendFormat("    Shrubland:             {0}", planet.Grid.Tiles.Count(t => t.BiomeType == BiomeType.Shrubland));
            sb.AppendLine();
            sb.AppendFormat("    Deciduous Forest:      {0}", planet.Grid.Tiles.Count(t => t.BiomeType == BiomeType.DeciduousForest));
            sb.AppendLine();
            sb.AppendFormat("  Tropical:                {0}", planet.Grid.Tiles.Count(t => t.ClimateType == ClimateType.Subtropical || t.ClimateType == ClimateType.Tropical));
            sb.AppendLine();
            sb.AppendFormat("    Desert:                {0}", planet.Grid.Tiles.Count(t => t.BiomeType == BiomeType.HotDesert));
            sb.AppendLine();
            sb.AppendFormat("    Savanna:               {0}", planet.Grid.Tiles.Count(t => t.BiomeType == BiomeType.Savanna));
            sb.AppendLine();
            sb.AppendFormat("    Monsoon Forest:        {0}", planet.Grid.Tiles.Count(t => t.BiomeType == BiomeType.MonsoonForest));
            sb.AppendLine();
            sb.AppendFormat("    Rain Forest:           {0}", planet.Grid.Tiles.Count(t => t.BiomeType == BiomeType.RainForest));
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
                sb.AppendFormat("  Avg:                     {0} mm", list.Average());
                sb.AppendLine();
                sb.AppendFormat("  Avg (<=P90):             {0} mm", list.Take((int)Math.Floor(list.Count * 0.9)).Average());
                sb.AppendLine();
                sb.AppendFormat("  P10:                     {0} mm", list.Skip((int)Math.Floor(list.Count * 0.1)).First());
                sb.AppendLine();
                sb.AppendFormat("  Q1:                      {0} mm", list.Skip((int)Math.Floor(list.Count * 0.25)).First());
                sb.AppendLine();
                sb.AppendFormat("  Q2:                      {0} mm", list.Skip((int)Math.Floor(list.Count * 0.5)).First());
                sb.AppendLine();
                sb.AppendFormat("  Q3:                      {0} mm", list.Skip((int)Math.Floor(list.Count * 0.75)).First());
                sb.AppendLine();
                sb.AppendFormat("  P90:                     {0} mm", list.Skip((int)Math.Floor(list.Count * 0.9)).First());
                sb.AppendLine();
                sb.AppendFormat("  Max:                     {0} mm", list.Last());
                sb.AppendLine();
            }
            else
            {
                sb.AppendLine("  No land.");
            }

            sb.AppendLine("  Selected Tiles:");
            sb.AppendFormat("    [1]:                 {0} mm ({1})",
                planet.Grid.Tiles[1].Precipitation,
                planet.Grid.Tiles[1].TerrainType);
            sb.AppendLine();
            sb.AppendFormat("    [2]:                 {0} mm ({1})",
                planet.Grid.Tiles[2].Precipitation,
                planet.Grid.Tiles[2].TerrainType);
            sb.AppendLine();
            sb.AppendFormat("    [3]:                 {0} mm ({1})",
                planet.Grid.Tiles[3].Precipitation,
                planet.Grid.Tiles[3].TerrainType);
            sb.AppendLine();
            sb.AppendFormat("    [4]:                 {0} mm ({1})",
                planet.Grid.Tiles[4].Precipitation,
                planet.Grid.Tiles[4].TerrainType);
            sb.AppendLine();
        }

        private static void AddTempString(StringBuilder sb, TerrestrialPlanet planet)
        {
            sb.AppendLine("Temp:");
            sb.AppendFormat("  Avg:                     {0} K", planet.Grid.Tiles.Average(x => x.Temperature.Avg));
            sb.AppendLine();

            var water = planet.Grid.Tiles.Any(x => x.TerrainType == TerrainType.Water);
            var min = water ? planet.Grid.Tiles
                .Where(x => x.TerrainType == TerrainType.Water)
                .Min(x => x.Temperature.Min)
                : 0;
            sb.AppendFormat("  Min Sea-Level:           {0} K", min);
            sb.AppendLine();

            var avg = water ? planet.Grid.Tiles
                .Where(x => x.TerrainType == TerrainType.Water)
                .Average(x => x.Temperature.Avg)
                : 0;
            sb.AppendFormat("  Avg Sea-Level:           {0} K", avg);
            sb.AppendLine();

            sb.AppendFormat("  Max:                     {0} K", planet.Grid.Tiles.Max(x => x.Temperature.Max));
            sb.AppendLine();

            var maxAvg = planet.Grid.Tiles.Max(x => x.Temperature.Avg);
            sb.AppendFormat("  Max Avg:          {0} K", maxAvg);
            sb.AppendLine();

            var (minMaxTemp, minMaxIndex) = water ? planet.Grid.Tiles
                .Where(x => x.TerrainType == TerrainType.Water)
                .Select(x => (temp: x.Temperature.Max, x.Index))
                .OrderBy(x => x.temp)
                .First()
                : (0, 0);
            sb.AppendFormat("  Min Max (water):  {0} K [{1}]", minMaxTemp, minMaxIndex);
            sb.AppendLine();

            sb.AppendFormat("Avg Surface Pressure:      {0} kPa", planet.Grid.Tiles.Average(x => x.AtmosphericPressure.Avg));
            sb.AppendLine();
        }

        private static void AddTerrainString(StringBuilder sb, TerrestrialPlanet planet)
        {
            if (planet.Grid.Tiles[0].BiomeType == BiomeType.None)
            {
                return;
            }

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
            sb.AppendLine("Elevation:");
            sb.AppendFormat("  Min:                     {0}m", Math.Round(planet.Grid.Tiles.Min(t => t.Elevation)));
            sb.AppendLine();
            sb.AppendFormat("  Avg:                     {0}m", Math.Round(planet.Grid.Tiles.Average(t => t.Elevation)));
            sb.AppendLine();
            var list = planet.Grid.Tiles.OrderBy(t => t.Elevation).ToList();
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
