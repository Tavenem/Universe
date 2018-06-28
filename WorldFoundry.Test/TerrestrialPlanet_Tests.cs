using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using WorldFoundry.CelestialBodies.Planetoids.Planets.TerrestrialPlanets;
using WorldFoundry.Climate;

namespace WorldFoundry.Test
{
    [TestClass]
    public class TerrestrialPlanet_Tests
    {
        private static TypeModel _compiledSerializer;
        private const int gridSize = 6;
        private const int numSeasons = 4;

        [ClassInitialize]
        public static void Init(TestContext context) => _compiledSerializer = GetTypeModel(typeof(TerrestrialPlanet));

        [TestMethod]
        public void TerrestrialPlanet_Generate()
        {
            var planetParams = TerrestrialPlanetParams.FromDefaults(gridSize: gridSize);

            var planet = TerrestrialPlanet.DefaultHumanPlanetNewUniverse(planetParams);
            Assert.IsNotNull(planet);

            Console.WriteLine($"Tiles: {planet.Topography.Tiles.Length}");
            Console.WriteLine($"Radius: {planet.Radius / 1000} km");
            Console.WriteLine($"Surface area: {planet.Topography.Tiles.Sum(x => x.Area) / 1000000} km²");
            Console.WriteLine($"Tile area: {planet.Topography.Tiles[13].Area / 1000000} km²");
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

            var stringData = SaveAsString(planet);
            Assert.IsNotNull(stringData);

            planet = LoadFromString(stringData);
            Assert.IsNotNull(planet);
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
        }

        private static void AddClimateString(StringBuilder sb, TerrestrialPlanet planet)
        {
            if (planet.Topography.Tiles[0].BiomeType == BiomeType.None)
            {
                return;
            }

            sb.AppendLine("Climates:");
            sb.AppendFormat("  % Desert (land):         {0}%", planet.Topography.Tiles.Count(t => t.BiomeType == BiomeType.HotDesert || t.BiomeType == BiomeType.ColdDesert || (t.ClimateType == ClimateType.Polar && t.EcologyType == EcologyType.Desert)) * 100.0f / planet.Topography.Tiles.Count(t => t.TerrainType != TerrainType.Water));
            sb.AppendLine();
            sb.AppendFormat("  % Rain Forest (land):    {0}%", planet.Topography.Tiles.Count(t => t.BiomeType == BiomeType.RainForest) * 100.0f / planet.Topography.Tiles.Count(t => t.TerrainType != TerrainType.Water));
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
            sb.AppendFormat("  % Desert (land):         {0}%", planet.Topography.Tiles.Count(t => t.BiomeType == BiomeType.HotDesert || t.BiomeType == BiomeType.ColdDesert || (t.ClimateType == ClimateType.Polar && t.EcologyType == EcologyType.Desert)) * 100.0f / planet.Topography.Tiles.Count(t => t.TerrainType != TerrainType.Water));
            sb.AppendLine();
            sb.AppendFormat("  % Rain Forest (land):    {0}%", planet.Topography.Tiles.Count(t => t.BiomeType == BiomeType.RainForest) * 100.0f / planet.Topography.Tiles.Count(t => t.TerrainType != TerrainType.Water));
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

        static void AddPrecipitationString(StringBuilder sb, TerrestrialPlanet planet, List<Season> seasons, float seasonsInYear)
        {
            sb.AppendLine("Precipitation (annual average, land):");
            var list = planet.Topography.Tiles.Where(x => x.TerrainType != TerrainType.Water)
                .Select(x => seasons.Select(y => y.TileClimates[x.Index].Precipitation).Sum())
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
                seasons.Sum(s => s.TileClimates[1].Precipitation),
                planet.Topography.Tiles[1].TerrainType);
            sb.AppendLine();
            sb.AppendFormat("    [2]:                 {0} mm ({1})",
                seasons.Sum(s => s.TileClimates[2].Precipitation),
                planet.Topography.Tiles[2].TerrainType);
            sb.AppendLine();
            sb.AppendFormat("    [3]:                 {0} mm ({1})",
                seasons.Sum(s => s.TileClimates[3].Precipitation),
                planet.Topography.Tiles[3].TerrainType);
            sb.AppendLine();
            sb.AppendFormat("    [4]:                 {0} mm ({1})",
                seasons.Sum(s => s.TileClimates[4].Precipitation),
                planet.Topography.Tiles[4].TerrainType);
            sb.AppendLine();
        }

        private static void AddTempString(StringBuilder sb, TerrestrialPlanet planet, List<Season> seasons)
        {
            sb.AppendLine("Temp:");
            sb.AppendFormat("  Avg:                     {0} K", seasons.Average(s => s.TileClimates.Average(t => t.Temperature)));
            sb.AppendLine();

            var water = planet.Topography.Tiles.Any(x => x.TerrainType == TerrainType.Water);
            var min = water ? planet.Topography.Tiles
                .Where(x => x.TerrainType == TerrainType.Water)
                .Select(x => seasons.Select(y => y.TileClimates[x.Index].Temperature).Min()).Min()
                : 0;
            sb.AppendFormat("  Min Sea-Level:           {0} K", min);
            sb.AppendLine();

            var avg = water ? planet.Topography.Tiles
                .Where(x => x.TerrainType == TerrainType.Water)
                .Select(x => seasons.Select(y => y.TileClimates[x.Index].Temperature).Average()).Average()
                : 0;
            sb.AppendFormat("  Avg Sea-Level:           {0} K", avg);
            sb.AppendLine();

            sb.AppendFormat("  Max:                     {0} K", seasons.Max(s => s.TileClimates.Max(t => t.Temperature)));
            sb.AppendLine();

            var maxAvg = planet.Topography.Tiles
                .Select(x => seasons.Select(y => y.TileClimates[x.Index].Temperature).Average())
                .Max();
            sb.AppendFormat("  Max Avg:          {0} K", maxAvg);
            sb.AppendLine();

            var (minMaxTemp, minMaxIndex) = water ? planet.Topography.Tiles
                .Where(x => x.TerrainType == TerrainType.Water)
                .Select(x => (temp: seasons.Select(y => y.TileClimates[x.Index].Temperature).Max(), x.Index))
                .OrderBy(x => x.temp)
                .First()
                : (0, 0);
            sb.AppendFormat("  Min Max (water):  {0} K [{1}]", minMaxTemp, minMaxIndex);
            sb.AppendLine();

            sb.AppendFormat("Avg Surface Pressure:      {0} kPa", seasons.Average(s => s.TileClimates.Average(t => t.AtmosphericPressure)));
            sb.AppendLine();
        }

        private static void AddToTypeModel(RuntimeTypeModel typeModel, Type type, IEnumerable<Type> types)
        {
            if (typeModel == null)
            {
                throw new ArgumentNullException(nameof(typeModel));
            }
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (typeModel.IsDefined(type))
            {
                return;
            }

            if (!type.IsPublic)
            {
                return;
            }
            if (type.IsGenericParameter)
            {
                return;
            }

            if (type.HasElementType) // T[]
            {
                AddToTypeModel(typeModel, type.GetElementType(), types);
                return;
            }
            else if (type.IsGenericType) // List<T>
            {
                foreach (var t in type.GetGenericArguments())
                {
                    AddToTypeModel(typeModel, t, types);
                }
                return;
            }

            var metaType = typeModel.Add(type, false);
            metaType.AsReferenceDefault = type.IsClass;

            var i = 1;
            if (types != null)
            {
                foreach (var s in types.Where(x => x.BaseType == type))
                {
                    metaType.AddSubType(i++, s);
                }
            }
            var props = type
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(x => x.GetSetMethod() != null)
                .ToArray();
            Array.Sort(props, (x, y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal));
            foreach (var property in props)
            {
                metaType.AddField(i++, property.Name);

                if (!typeModel.CanSerialize(property.PropertyType))
                {
                    AddToTypeModel(typeModel, property.PropertyType, types);
                }
            }
        }

        private static TypeModel GetTypeModel(Type type, string rootNamespace = null)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (rootNamespace == null)
            {
                var dotIndex = type.Namespace?.IndexOf('.') ?? -1;
                if (dotIndex == -1)
                {
                    rootNamespace = type.Namespace;
                }
                else
                {
                    rootNamespace = type.Namespace.Substring(0, dotIndex);
                }
                if (string.IsNullOrEmpty(rootNamespace))
                {
                    throw new ArgumentException($"Namespace of provided type cannot be empty if no {nameof(rootNamespace)} is provided.", nameof(type));
                }
            }

            var typeModel = TypeModel.Create();

            var types = Assembly.GetAssembly((type))
                .GetTypes().Where(x =>
                    (x.Namespace?.StartsWith(rootNamespace) ?? false)
                    && x.IsPublic
                    && !x.IsInterface
                    && !x.IsAbstract)
                .ToList();
            foreach (var t in types)
            {
                AddToTypeModel(typeModel, t, types);
            }
            return typeModel.Compile();
        }

        private static TerrestrialPlanet LoadFromString(string data)
        {
            TerrestrialPlanet planet;
            using (var ms = new MemoryStream(Convert.FromBase64String(data)))
            {
                planet = _compiledSerializer.Deserialize(ms, null, typeof(TerrestrialPlanet)) as TerrestrialPlanet;
            }
            return planet;
        }

        private static string SaveAsString(TerrestrialPlanet planet)
        {
            string stringData;
            using (var ms = new MemoryStream())
            {
                _compiledSerializer.Serialize(ms, planet);
                stringData = Convert.ToBase64String(ms.ToArray());
            }
            return stringData;
        }
    }
}
