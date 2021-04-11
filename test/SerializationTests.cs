using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tavenem.Chemistry;
using Tavenem.DataStorage;
using Tavenem.HugeNumbers;
using Tavenem.Mathematics;
using Tavenem.Mathematics.HugeNumbers;
using Tavenem.Time;
using Tavenem.Universe.Chemistry;
using Tavenem.Universe.Place;
using Tavenem.Universe.Space;
using Tavenem.Universe.Space.Planetoids;

namespace Tavenem.Universe.Test
{
    [TestClass]
    public class SerializationTests
    {
        [TestMethod]
        public void FloatRangeTest()
        {
            var value = FloatRange.Zero;

            var str = value.ToString("r");
            Assert.AreEqual("<0;0>", str);
            Assert.AreEqual(value, FloatRange.Parse(str));

            var json = System.Text.Json.JsonSerializer.Serialize(value);
            Assert.AreEqual("\"\\u003C0;0\\u003E\"", json);
            Assert.AreEqual(value, System.Text.Json.JsonSerializer.Deserialize<FloatRange>(json));

            value = new FloatRange(0.25f, 0.75f);

            str = value.ToString("r");
            Assert.AreEqual("<0.25;0.75>", str);
            Assert.AreEqual(value, FloatRange.Parse(str));

            json = System.Text.Json.JsonSerializer.Serialize(value);
            Assert.AreEqual("\"\\u003C0.25;0.75\\u003E\"", json);
            Assert.AreEqual(value, System.Text.Json.JsonSerializer.Deserialize<FloatRange>(json));
        }

        [TestMethod]
        public void SubstanceRequirementTest()
        {
            var value = new SubstanceRequirement(Substances.All.Water.GetHomogeneousReference());

            var json = System.Text.Json.JsonSerializer.Serialize(value);
            Console.WriteLine();
            Console.WriteLine(json);
            Assert.AreEqual(value, System.Text.Json.JsonSerializer.Deserialize<SubstanceRequirement>(json));

            value = new SubstanceRequirement(Substances.All.Oxygen.GetHomogeneousReference(), 0.2m, 0.6m, PhaseType.Gas);

            json = System.Text.Json.JsonSerializer.Serialize(value);
            Console.WriteLine();
            Console.WriteLine(json);
            Assert.AreEqual(value, System.Text.Json.JsonSerializer.Deserialize<SubstanceRequirement>(json));
        }

        [TestMethod]
        public void LocationTest()
        {
            var value = new Location("Test_ID", Location.LocationIdItemTypeName, new Sphere(new HugeNumber(10)), null, null);

            var json = System.Text.Json.JsonSerializer.Serialize(value);
            Console.WriteLine();
            Console.WriteLine(json);
            var deserialized = System.Text.Json.JsonSerializer.Deserialize<Location>(json);
            Assert.AreEqual(value, deserialized);
            Assert.AreEqual(json, System.Text.Json.JsonSerializer.Serialize(deserialized));

            value = new Location("Test_ID", Location.LocationIdItemTypeName, new Sphere(new HugeNumber(10)), "Test_Parent_ID", new Vector3[] { Vector3.Zero, Vector3.UnitX });

            json = System.Text.Json.JsonSerializer.Serialize(value);
            Console.WriteLine();
            Console.WriteLine(json);
            deserialized = System.Text.Json.JsonSerializer.Deserialize<Location>(json);
            Assert.AreEqual(value, deserialized);
            Assert.AreEqual(json, System.Text.Json.JsonSerializer.Serialize(deserialized));
        }

        [TestMethod]
        public void TerritoryTest()
        {
            var value = new Territory("Test_ID", Territory.TerritoryIdItemTypeName, new Sphere(new HugeNumber(10)), new string[] { "Test_Child_ID" });

            var json = System.Text.Json.JsonSerializer.Serialize(value);
            Console.WriteLine();
            Console.WriteLine(json);
            var deserialized = System.Text.Json.JsonSerializer.Deserialize<Territory>(json);
            Assert.AreEqual(value, deserialized);
            Assert.AreEqual(json, System.Text.Json.JsonSerializer.Serialize(deserialized));

            value = new Territory(
                "Test_ID",
                Territory.TerritoryIdItemTypeName,
                new Sphere(new HugeNumber(10)),
                new string[] { "Test_Child_ID" },
                "Test_Parent_ID",
                new Vector3[] { Vector3.Zero, Vector3.UnitX });

            json = System.Text.Json.JsonSerializer.Serialize(value);
            Console.WriteLine();
            Console.WriteLine(json);
            deserialized = System.Text.Json.JsonSerializer.Deserialize<Territory>(json);
            Assert.AreEqual(value, deserialized);
            Assert.AreEqual(json, System.Text.Json.JsonSerializer.Serialize(deserialized));
        }

        [TestMethod]
        public void OrbitTest()
        {
            var value = new Orbit(
                new HugeNumber(1, 100),
                Vector3.Zero,
                0.1,
                0.01,
                0.2,
                0.2,
                HugeNumber.One,
                new HugeNumber(1, 10000),
                Vector3.UnitX,
                new HugeNumber(1, 10000),
                new HugeNumber(1, 10000),
                new HugeNumber(1, 90000),
                0.4,
                Vector3.One,
                new HugeNumber(1, 10000),
                Duration.Zero);

            var json = System.Text.Json.JsonSerializer.Serialize(value);
            Console.WriteLine();
            Console.WriteLine(json);
            Assert.AreEqual(value, System.Text.Json.JsonSerializer.Deserialize<Orbit>(json));
        }

        [TestMethod]
        public void HabitabilityRequirementsTest()
        {
            var value = new HabitabilityRequirements(
                new SubstanceRequirement[] { new SubstanceRequirement(Substances.All.Oxygen.GetHomogeneousReference(), 0.2m, 0.6m, PhaseType.Gas) },
                25,
                null,
                400,
                1300,
                null,
                20,
                true);

            var json = System.Text.Json.JsonSerializer.Serialize(value);
            Console.WriteLine();
            Console.WriteLine(json);
            Assert.AreEqual(value, System.Text.Json.JsonSerializer.Deserialize<HabitabilityRequirements>(json));
        }

        [TestMethod]
        public void PlanetParamsTest()
        {
            var value = PlanetParams.Earthlike;

            var json = System.Text.Json.JsonSerializer.Serialize(value);
            Console.WriteLine();
            Console.WriteLine(json);
            Assert.AreEqual(value, System.Text.Json.JsonSerializer.Deserialize<PlanetParams>(json));
        }

        [TestMethod]
        public void SurfaceRegionTest()
        {
            var value = new SurfaceRegion(
                "Test_ID",
                SurfaceRegion.SurfaceRegionIdItemTypeName,
                new Sphere(new HugeNumber(10)),
                "Test_Parent_ID",
                null);

            var json = System.Text.Json.JsonSerializer.Serialize(value);
            Console.WriteLine();
            Console.WriteLine(json);
            var deserialized = System.Text.Json.JsonSerializer.Deserialize<SurfaceRegion>(json);
            Assert.AreEqual(value, deserialized);
            Assert.AreEqual(json, System.Text.Json.JsonSerializer.Serialize(deserialized));

            value = new SurfaceRegion(
                "Test_ID",
                SurfaceRegion.SurfaceRegionIdItemTypeName,
                new Sphere(new HugeNumber(10)),
                "Test_Parent_ID",
                new Vector3[] { Vector3.Zero, Vector3.UnitX });

            json = System.Text.Json.JsonSerializer.Serialize(value);
            Console.WriteLine();
            Console.WriteLine(json);
            deserialized = System.Text.Json.JsonSerializer.Deserialize<SurfaceRegion>(json);
            Assert.AreEqual(value, deserialized);
            Assert.AreEqual(json, System.Text.Json.JsonSerializer.Serialize(deserialized));
        }

        [TestMethod]
        public void CosmicLocationTest()
        {
            var value = CosmicLocation.New(CosmicStructureType.Universe, null, Vector3.Zero, out _);

            var json = System.Text.Json.JsonSerializer.Serialize(value);
            Console.WriteLine();
            Console.WriteLine(json);
            var deserialized = System.Text.Json.JsonSerializer.Deserialize<CosmicLocation>(json);
            Assert.AreEqual(value, deserialized);
            Assert.AreEqual(json, System.Text.Json.JsonSerializer.Serialize(deserialized));
        }

        [TestMethod]
        public void AsteroidFieldTest()
        {
            var value = new AsteroidField(null, Vector3.Zero, childOrbit: OrbitalParameters.GetCircular(new HugeNumber(1, 1000), Vector3.Zero));

            var json = System.Text.Json.JsonSerializer.Serialize(value);
            Console.WriteLine();
            Console.WriteLine(json);
            var deserialized = System.Text.Json.JsonSerializer.Deserialize<AsteroidField>(json);
            Assert.AreEqual(value, deserialized);
            Assert.AreEqual(json, System.Text.Json.JsonSerializer.Serialize(deserialized));
        }

        [TestMethod]
        public void BlackHoleTest()
        {
            var value = new BlackHole(null, Vector3.Zero);

            var json = System.Text.Json.JsonSerializer.Serialize(value);
            Console.WriteLine();
            Console.WriteLine(json);
            var deserialized = System.Text.Json.JsonSerializer.Deserialize<BlackHole>(json);
            Assert.AreEqual(value, deserialized);
            Assert.AreEqual(json, System.Text.Json.JsonSerializer.Serialize(deserialized));
        }

        [TestMethod]
        public void StarTest()
        {
            var value = new Star(null, Vector3.Zero);

            var json = System.Text.Json.JsonSerializer.Serialize(value);
            Console.WriteLine();
            Console.WriteLine(json);
            var deserialized = System.Text.Json.JsonSerializer.Deserialize<Star>(json);
            Assert.AreEqual(value, deserialized);
            Assert.AreEqual(json, System.Text.Json.JsonSerializer.Serialize(deserialized));

            value = new Star(null, Vector3.Zero, null, Space.Stars.SpectralClass.G, Space.Stars.LuminosityClass.V);

            json = System.Text.Json.JsonSerializer.Serialize(value);
            Console.WriteLine();
            Console.WriteLine(json);
            deserialized = System.Text.Json.JsonSerializer.Deserialize<Star>(json);
            Assert.AreEqual(value, deserialized);
            Assert.AreEqual(json, System.Text.Json.JsonSerializer.Serialize(deserialized));
        }

        [TestMethod]
        public void StarSystemTest()
        {
            var value = new StarSystem(null, Vector3.Zero, out _);

            var json = System.Text.Json.JsonSerializer.Serialize(value);
            Console.WriteLine();
            Console.WriteLine(json);
            var deserialized = System.Text.Json.JsonSerializer.Deserialize<StarSystem>(json);
            Assert.AreEqual(value, deserialized);
            Assert.AreEqual(json, System.Text.Json.JsonSerializer.Serialize(deserialized));
        }

        [TestMethod]
        public void PlanetoidTest()
        {
            var value = Planetoid.GetPlanetForSunlikeStar(out _);
            Assert.IsNotNull(value);

            var json = System.Text.Json.JsonSerializer.Serialize(value);

            var bytes = Encoding.UTF8.GetBytes(json);
            Console.WriteLine();
            Console.WriteLine($"Size (System.Text.Json): {BytesToString(bytes.Length)}");
            Console.WriteLine();
            Console.WriteLine("JSON (System.Text.Json):");
            Console.WriteLine(json);

            var deserialized = System.Text.Json.JsonSerializer.Deserialize<Planetoid>(json);
            Assert.AreEqual(value, deserialized);
            Assert.AreEqual(json, System.Text.Json.JsonSerializer.Serialize(deserialized));
        }

        [TestMethod]
        public async Task EntireUniverseTestAsync()
        {
            var dataStore = new InMemoryDataStore();

            var (planet, children) = await Planetoid.GetPlanetForNewUniverseAsync(dataStore).ConfigureAwait(false);
            Assert.IsNotNull(planet);

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var json = System.Text.Json.JsonSerializer.Serialize(children);

            stopwatch.Stop();

            var bytes = Encoding.UTF8.GetBytes(json);
            Console.WriteLine();
            Console.WriteLine($"Size (System.Text.Json): {BytesToString(bytes.Length)}");
            Console.WriteLine($"Serialization time (System.Text.Json): {stopwatch.Elapsed}");
            //Console.WriteLine();
            //Console.WriteLine("JSON (System.Text.Json):");
            //Console.WriteLine(json);

            stopwatch.Reset();
            stopwatch.Start();

            var deserialized = System.Text.Json.JsonSerializer.Deserialize<List<CosmicLocation>>(json);

            stopwatch.Stop();

            Assert.IsNotNull(deserialized);
            Assert.IsTrue(children.SequenceEqual(deserialized!));

            Console.WriteLine($"Deserialization time (System.Text.Json): {stopwatch.Elapsed}");
        }

        private static string BytesToString(int numBytes)
        {
            if (numBytes < 1000)
            {
                return $"{numBytes} bytes";
            }
            if (numBytes < 1000000)
            {
                return $"{numBytes / 1000.0:N2} KB";
            }
            if (numBytes < 1000000000)
            {
                return $"{numBytes / 1000000.0:N2} MB";
            }
            return $"{numBytes / 1000000000.0:N2} GB";
        }
    }
}
