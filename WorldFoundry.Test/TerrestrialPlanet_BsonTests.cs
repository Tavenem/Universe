using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using System;
using WorldFoundry.Bson;
using WorldFoundry.CelestialBodies.Planetoids.Planets.TerrestrialPlanets;
using WorldFoundry.SurfaceMapping;

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

            var maps = planet.GetSurfaceMaps(SurfaceMapResolution, steps: NumSeasons);

            var bson = maps.ToBson();
            Console.WriteLine($"Saved size: {bson.Length.ToString("N")} bytes");
            Console.WriteLine();

            //var json = maps.ToJson();
            //Console.Write("JSON: ");
            //Console.WriteLine(json);

            var result = BsonSerializer.Deserialize<SurfaceMaps>(bson);
            CollectionAssert.AreEqual(maps.Biome, result.Biome);
            CollectionAssert.AreEqual(maps.Climate, result.Climate);
            CollectionAssert.AreEqual(maps.Depth, result.Depth);
            CollectionAssert.AreEqual(maps.Ecology, result.Ecology);
            CollectionAssert.AreEqual(maps.Elevation, result.Elevation);
            CollectionAssert.AreEqual(maps.Flow, result.Flow);
            CollectionAssert.AreEqual(maps.Humidity, result.Humidity);
            Assert.AreEqual(maps.OverallTemperatureRange, result.OverallTemperatureRange);
            CollectionAssert.AreEqual(maps.SeaIceRanges, result.SeaIceRanges);
            CollectionAssert.AreEqual(maps.SnowCoverRanges, result.SnowCoverRanges);
            CollectionAssert.AreEqual(maps.TemperatureRanges, result.TemperatureRanges);
            CollectionAssert.AreEqual(maps.TotalPrecipitation, result.TotalPrecipitation);
            Assert.AreEqual(maps.TotalPrecipitationRange, result.TotalPrecipitationRange);
            Assert.AreEqual(maps.PrecipitationMaps.Length, result.PrecipitationMaps.Length);
            for (var i = 0; i < maps.PrecipitationMaps.Length; i++)
            {
                CollectionAssert.AreEqual(maps.PrecipitationMaps[i].Precipitation, result.PrecipitationMaps[i].Precipitation);
                CollectionAssert.AreEqual(maps.PrecipitationMaps[i].Snowfall, result.PrecipitationMaps[i].Snowfall);
            }
        }
    }
}
