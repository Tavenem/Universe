using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using System;
using WorldFoundry.Bson;
using WorldFoundry.CelestialBodies.Planetoids.Planets.TerrestrialPlanets;
using WorldFoundry.SurfaceMaps;

namespace WorldFoundry.Test
{
    [TestClass]
    public class TerrestrialPlanet_BsonTests
    {
        private const int NumSeasons = 12;
        private const int SurfaceMapResolution = 90;

        [ClassInitialize]
        public static void Init(TestContext _) => BsonRegistration.Register();

        [TestMethod]
        public void TerrestrialPlanet_Save()
        {
            var planetParams = TerrestrialPlanetParams.FromDefaults();

            var planet = TerrestrialPlanet.GetPlanetForNewUniverse(planetParams);
            Assert.IsNotNull(planet);

            var bson = planet.ToBson();
            Console.WriteLine($"Saved size: {bson.Length.ToString("N")} bytes");
            Console.WriteLine();

            //var json = planet.ToJson();
            //Console.Write("JSON: ");
            //Console.WriteLine(json);

            var result = BsonSerializer.Deserialize<TerrestrialPlanet>(bson);
            Assert.AreEqual(planet.Id, result.Id);
        }

        [TestMethod]
        public void TerrestrialPlanet_Save_SurfaceMaps()
        {
            var planetParams = TerrestrialPlanetParams.FromDefaults();

            var planet = TerrestrialPlanet.GetPlanetForNewUniverse(planetParams);
            Assert.IsNotNull(planet);

            var maps = planet.GetSurfaceMapSet(SurfaceMapResolution, steps: NumSeasons);

            var bson = maps.ToBson();
            Console.WriteLine($"Saved size: {bson.Length.ToString("N")} bytes");
            Console.WriteLine();

            //var json = maps.ToJson();
            //Console.Write("JSON: ");
            //Console.WriteLine(json);

            var result = BsonSerializer.Deserialize<TerrestrialSurfaceMapSet>(bson);
            Assert.AreEqual(maps, result);
        }
    }
}
