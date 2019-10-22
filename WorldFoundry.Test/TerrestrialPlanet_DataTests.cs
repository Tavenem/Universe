using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.Text;
using WorldFoundry.CelestialBodies.Planetoids.Planets.TerrestrialPlanets;
using WorldFoundry.SurfaceMapping;

namespace WorldFoundry.Test
{
    [TestClass]
    public class TerrestrialPlanet_DataTests
    {
        private const int NumSeasons = 12;
        private const int SurfaceMapResolution = 90;

        private static readonly JsonSerializerSettings _JsonSerializerSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto };

        [TestMethod]
        public void TerrestrialPlanet_Save()
        {
            var planetParams = TerrestrialPlanetParams.FromDefaults();

            var planet = TerrestrialPlanet.GetPlanetForNewUniverse(planetParams);
            Assert.IsNotNull(planet);

            var json = JsonConvert.SerializeObject(planet, _JsonSerializerSettings);
            var bytes = Encoding.UTF8.GetBytes(json);
            Console.WriteLine($"Saved size: {bytes.Length.ToString("N")} bytes");
            Console.WriteLine();

            //Console.Write("JSON: ");
            //Console.WriteLine(json);

            var result = JsonConvert.DeserializeObject<TerrestrialPlanet>(json, _JsonSerializerSettings);
            Assert.AreEqual(planet!.Id, result.Id);
        }

        [TestMethod]
        public void TerrestrialPlanet_Save_SurfaceMaps()
        {
            var planetParams = TerrestrialPlanetParams.FromDefaults();

            var planet = TerrestrialPlanet.GetPlanetForNewUniverse(planetParams);
            Assert.IsNotNull(planet);

            var maps = planet!.GetSurfaceMaps(SurfaceMapResolution, steps: NumSeasons);

            var json = JsonConvert.SerializeObject(maps);
            var bytes = Encoding.UTF8.GetBytes(json);
            Console.WriteLine($"Saved size: {bytes.Length.ToString("N")} bytes");
            Console.WriteLine();

            //Console.Write("JSON: ");
            //Console.WriteLine(json);

            var result = JsonConvert.DeserializeObject<SurfaceMaps>(json);
            CollectionAssert.AreEqual(maps.BiomeMap, result.BiomeMap);
            CollectionAssert.AreEqual(maps.ClimateMap, result.ClimateMap);
            CollectionAssert.AreEqual(maps.Depth, result.Depth);
            CollectionAssert.AreEqual(maps.EcologyMap, result.EcologyMap);
            CollectionAssert.AreEqual(maps.Elevation, result.Elevation);
            CollectionAssert.AreEqual(maps.Flow, result.Flow);
            CollectionAssert.AreEqual(maps.HumidityMap, result.HumidityMap);
            Assert.AreEqual(maps.TemperatureRange, result.TemperatureRange);
            CollectionAssert.AreEqual(maps.SeaIceRangeMap, result.SeaIceRangeMap);
            CollectionAssert.AreEqual(maps.SnowCoverRangeMap, result.SnowCoverRangeMap);
            CollectionAssert.AreEqual(maps.TemperatureRangeMap, result.TemperatureRangeMap);
            CollectionAssert.AreEqual(maps.TotalPrecipitationMap, result.TotalPrecipitationMap);
            Assert.AreEqual(maps.TotalPrecipitation, result.TotalPrecipitation);
            Assert.AreEqual(maps.PrecipitationMaps.Length, result.PrecipitationMaps.Length);
            for (var i = 0; i < maps.PrecipitationMaps.Length; i++)
            {
                CollectionAssert.AreEqual(maps.PrecipitationMaps[i].PrecipitationMap, result.PrecipitationMaps[i].PrecipitationMap);
                CollectionAssert.AreEqual(maps.PrecipitationMaps[i].SnowfallMap, result.PrecipitationMaps[i].SnowfallMap);
            }
        }
    }
}
