using Antmicro.Migrant;
using Antmicro.Migrant.Customization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Text;
using WorldFoundry.CelestialBodies.Planetoids.Planets.TerrestrialPlanets;

namespace WorldFoundry.Test
{
    [TestClass]
    public class TerrestrialPlanet_UnusedTests
    {
        private static Serializer Serializer;
        private static Settings SerializerSettings;
        private const int GridSize = 6;
        private const int NumSeasons = 12;

        [ClassInitialize]
        public static void Init(TestContext _)
        {
            Serializer = new Serializer();
            Serializer.ForObject<Type>().SetSurrogate(x => Activator.CreateInstance(typeof(TypeSurrogate<>).MakeGenericType(new[] { x })));
            Serializer.ForSurrogate<ITypeSurrogate>().SetObject(x => x.Restore());
            SerializerSettings = new Settings(versionTolerance: VersionToleranceLevel.AllowAssemblyVersionChange | VersionToleranceLevel.AllowFieldAddition | VersionToleranceLevel.AllowFieldRemoval);
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

            var grid = planet.GetGrid(GridSize);
            Assert.IsNotNull(grid);

            planet.SetClimate(grid, NumSeasons);

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

            var grid = planet.GetGrid(GridSize);
            Assert.IsNotNull(grid);

            planet.SetClimate(grid, NumSeasons);

            var stringData = SaveAsString(planet);
            Assert.IsNotNull(stringData);

            planet = LoadFromString(stringData);
            Assert.IsNotNull(planet);
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
