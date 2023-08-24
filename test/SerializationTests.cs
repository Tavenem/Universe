using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Tavenem.Chemistry;
using Tavenem.DataStorage;
using Tavenem.HugeNumbers;
using Tavenem.Mathematics;
using Tavenem.Time;
using Tavenem.Universe.Chemistry;
using Tavenem.Universe.Place;
using Tavenem.Universe.Space;
using Tavenem.Universe.Space.Planetoids;

namespace Tavenem.Universe.Test;

[TestClass]
public class SerializationTests
{
    private static readonly string[] _TestChildIds = new string[] { "Test_Child_ID" };

    [TestMethod]
    public void CosmicLocationTest()
    {
        var value = CosmicLocation.New(
            CosmicStructureType.Universe,
            null,
            Vector3<HugeNumber>.Zero,
            out _);
        Assert.IsNotNull(value);
        Assert.IsNull(value.Orbit);
        Assert.IsTrue(value.Material.Density.IsFinite());
        Assert.IsTrue(value.Material.Temperature?.IsFinite());
        Assert.IsTrue(value.Material.Temperature?.IsFinite());
        Location location = value;
        IIdItem iIdItem = value;

        var options = new JsonSerializerOptions
        {
            TypeInfoResolver = new UniverseTypeResolver()
        };

        var json = JsonSerializer.Serialize(value);
        Console.WriteLine();
        Console.WriteLine(json);
        var deserialized = JsonSerializer.Deserialize<CosmicLocation>(json);
        Assert.AreEqual(value, deserialized);
        Assert.AreEqual(json, JsonSerializer.Serialize(deserialized));

        json = JsonSerializer.Serialize(location, location.GetType());
        Console.WriteLine();
        Console.WriteLine(json);
        Assert.AreEqual(value, JsonSerializer.Deserialize<CosmicLocation>(json));

        json = JsonSerializer.Serialize(iIdItem, iIdItem.GetType());
        Console.WriteLine();
        Console.WriteLine(json);
        Assert.AreEqual(value, JsonSerializer.Deserialize<CosmicLocation>(json));

        json = JsonSerializer.Serialize(iIdItem, options);
        Console.WriteLine();
        Console.WriteLine(json);
        Assert.AreEqual(value, JsonSerializer.Deserialize<CosmicLocation>(json, options));
        Assert.AreEqual(value, JsonSerializer.Deserialize<Location>(json, options));
        Assert.AreEqual(value, JsonSerializer.Deserialize<IIdItem>(json, options));

        options = new JsonSerializerOptions
        {
            TypeInfoResolver = new UniverseTypeResolver()
        };
        options.TypeInfoResolverChain.Add(UniverseSourceGenerationContext.Default);

        json = JsonSerializer.Serialize(value, UniverseSourceGenerationContext.Default.CosmicLocation);
        Console.WriteLine();
        Console.WriteLine(json);
        deserialized = JsonSerializer.Deserialize(json, UniverseSourceGenerationContext.Default.CosmicLocation);
        Assert.IsNotNull(deserialized);
        Assert.AreEqual(value, deserialized);
        Assert.AreEqual(
            json,
            JsonSerializer.Serialize(deserialized, UniverseSourceGenerationContext.Default.CosmicLocation));

        json = JsonSerializer.Serialize(location, UniverseSourceGenerationContext.Default.CosmicLocation);
        Console.WriteLine();
        Console.WriteLine(json);
        Assert.AreEqual(value, JsonSerializer.Deserialize(json, UniverseSourceGenerationContext.Default.CosmicLocation));

        json = JsonSerializer.Serialize(iIdItem, UniverseSourceGenerationContext.Default.CosmicLocation);
        Console.WriteLine();
        Console.WriteLine(json);
        Assert.AreEqual(
            value,
            JsonSerializer.Deserialize(json, UniverseSourceGenerationContext.Default.CosmicLocation));

        json = JsonSerializer.Serialize(location, UniverseSourceGenerationContext.Default.Location);
        Console.WriteLine();
        Console.WriteLine(json);
        Assert.AreEqual(
            value,
            JsonSerializer.Deserialize(json, UniverseSourceGenerationContext.Default.CosmicLocation));
        Assert.AreEqual(
            value,
            JsonSerializer.Deserialize(json, UniverseSourceGenerationContext.Default.Location));
        Assert.AreEqual(
            value,
            JsonSerializer.Deserialize<IIdItem>(json, options));

        json = JsonSerializer.Serialize(iIdItem, options);
        Console.WriteLine();
        Console.WriteLine(json);
        Assert.AreEqual(
            value,
            JsonSerializer.Deserialize(json, UniverseSourceGenerationContext.Default.CosmicLocation));
        Assert.AreEqual(
            value,
            JsonSerializer.Deserialize(json, UniverseSourceGenerationContext.Default.Location));
        Assert.AreEqual(
            value,
            JsonSerializer.Deserialize<IIdItem>(json, options));
    }

    [TestMethod]
    public async Task EntireUniverseTestAsync()
    {
        var dataStore = new InMemoryDataStore();

        var (planet, children) = await Planetoid.GetPlanetForNewUniverseAsync(dataStore);
        Assert.IsNotNull(planet);

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        var json = JsonSerializer.Serialize(children);

        stopwatch.Stop();

        var bytes = Encoding.UTF8.GetBytes(json);
        Console.WriteLine();
        Console.WriteLine($"Size (reflection): {BytesToString(bytes.Length)}");
        Console.WriteLine($"Serialization time (reflection): {stopwatch.Elapsed}");
        //Console.WriteLine();
        //Console.WriteLine("JSON (reflection):");
        //Console.WriteLine(json);

        stopwatch.Reset();
        stopwatch.Start();

        var deserialized = JsonSerializer.Deserialize<List<CosmicLocation>>(json);

        stopwatch.Stop();

        Assert.IsNotNull(deserialized);
        Assert.IsTrue(children.SequenceEqual(deserialized));

        Console.WriteLine($"Deserialization time (reflection): {stopwatch.Elapsed}");

        stopwatch.Reset();
        stopwatch.Start();

        json = JsonSerializer.Serialize(children, UniverseSourceGenerationContext.Default.ListCosmicLocation);

        stopwatch.Stop();

        bytes = Encoding.UTF8.GetBytes(json);
        Console.WriteLine();
        Console.WriteLine($"Size (source generation): {BytesToString(bytes.Length)}");
        Console.WriteLine($"Serialization time (source generation): {stopwatch.Elapsed}");
        //Console.WriteLine();
        //Console.WriteLine("JSON (source generation):");
        //Console.WriteLine(json);

        stopwatch.Reset();
        stopwatch.Start();

        deserialized = JsonSerializer.Deserialize(json, UniverseSourceGenerationContext.Default.ListCosmicLocation);

        stopwatch.Stop();

        Assert.IsNotNull(deserialized);
        Assert.IsTrue(children.SequenceEqual(deserialized));

        Console.WriteLine($"Deserialization time (source generation): {stopwatch.Elapsed}");
    }

    [TestMethod]
    public void FloatRangeTest()
    {
        var value = FloatRange.Zero;

        var str = value.ToString("r");
        Assert.AreEqual("<0;0>", str);
        Assert.AreEqual(value, FloatRange.Parse(str));

        var json = JsonSerializer.Serialize(value);
        Assert.AreEqual("\"\\u003C0;0\\u003E\"", json);
        Assert.AreEqual(value, JsonSerializer.Deserialize<FloatRange>(json));

        value = new FloatRange(0.25f, 0.75f);

        str = value.ToString("r");
        Assert.AreEqual("<0.25;0.75>", str);
        Assert.AreEqual(value, FloatRange.Parse(str));

        json = JsonSerializer.Serialize(value);
        Assert.AreEqual("\"\\u003C0.25;0.75\\u003E\"", json);
        Assert.AreEqual(value, JsonSerializer.Deserialize<FloatRange>(json));
    }

    [TestMethod]
    public void HabitabilityRequirementsTest()
    {
        var value = new HabitabilityRequirements(
            new SubstanceRequirement[]
            {
                new SubstanceRequirement(
                    Substances.All.Oxygen.GetHomogeneousReference(),
                    0.2m,
                    0.6m,
                    PhaseType.Gas),
            },
            25,
            null,
            400,
            1300,
            null,
            20,
            true);

        var json = JsonSerializer.Serialize(value);
        Console.WriteLine();
        Console.WriteLine(json);
        var deserialized = JsonSerializer.Deserialize<HabitabilityRequirements>(json);
        Assert.IsNotNull(deserialized);
        Assert.AreEqual(value, deserialized);
        Assert.AreEqual(json, JsonSerializer.Serialize(deserialized));

        json = JsonSerializer.Serialize(value, UniverseSourceGenerationContext.Default.HabitabilityRequirements);
        Console.WriteLine();
        Console.WriteLine(json);
        deserialized = JsonSerializer.Deserialize(json, UniverseSourceGenerationContext.Default.HabitabilityRequirements);
        Assert.IsNotNull(deserialized);
        Assert.AreEqual(value, deserialized);
        Assert.AreEqual(
            json,
            JsonSerializer.Serialize(deserialized, UniverseSourceGenerationContext.Default.HabitabilityRequirements));
    }

    [TestMethod]
    public void LocationTest()
    {
        var value = new Location(
            "Test_ID",
            new Sphere<HugeNumber>(new HugeNumber(10)),
            null,
            null,
            null);
        IIdItem iIdItem = value;

        var options = new JsonSerializerOptions
        {
            TypeInfoResolver = new UniverseTypeResolver()
        };

        var json = JsonSerializer.Serialize(value);
        Console.WriteLine();
        Console.WriteLine(json);
        var deserialized = JsonSerializer.Deserialize<Location>(json);
        Assert.IsNotNull(deserialized);
        Assert.AreEqual(value, deserialized);
        Assert.AreEqual(json, JsonSerializer.Serialize(deserialized));

        json = JsonSerializer.Serialize(iIdItem, iIdItem.GetType());
        Console.WriteLine();
        Console.WriteLine(json);
        Assert.AreEqual(value, JsonSerializer.Deserialize<Location>(json));

        json = JsonSerializer.Serialize(iIdItem, options);
        Console.WriteLine();
        Console.WriteLine(json);
        Assert.AreEqual(value, JsonSerializer.Deserialize<Location>(json, options));
        Assert.AreEqual(value, JsonSerializer.Deserialize<IIdItem>(json, options));

        value = new Location(
            "Test_ID",
            new Sphere<HugeNumber>(new HugeNumber(10)),
            "Test_Parent_ID",
            new Vector3<HugeNumber>[] { Vector3<HugeNumber>.Zero, Vector3<HugeNumber>.UnitX },
            "Test Name");
        iIdItem = value;

        json = JsonSerializer.Serialize(value);
        Console.WriteLine();
        Console.WriteLine(json);
        deserialized = JsonSerializer.Deserialize<Location>(json);
        Assert.IsNotNull(deserialized);
        Assert.AreEqual(value, deserialized);
        Assert.AreEqual(json, JsonSerializer.Serialize(deserialized));

        json = JsonSerializer.Serialize(iIdItem, iIdItem.GetType());
        Console.WriteLine();
        Console.WriteLine(json);
        Assert.AreEqual(value, JsonSerializer.Deserialize<Location>(json));

        json = JsonSerializer.Serialize(iIdItem, options);
        Console.WriteLine();
        Console.WriteLine(json);
        Assert.AreEqual(value, JsonSerializer.Deserialize<Location>(json, options));
        Assert.AreEqual(value, JsonSerializer.Deserialize<IIdItem>(json, options));

        options = new JsonSerializerOptions
        {
            TypeInfoResolver = new UniverseTypeResolver()
        };
        options.TypeInfoResolverChain.Add(UniverseSourceGenerationContext.Default);

        json = JsonSerializer.Serialize(value, UniverseSourceGenerationContext.Default.Location);
        Console.WriteLine();
        Console.WriteLine(json);
        deserialized = JsonSerializer.Deserialize(json, UniverseSourceGenerationContext.Default.Location);
        Assert.IsNotNull(deserialized);
        Assert.AreEqual(value, deserialized);
        Assert.AreEqual(
            json,
            JsonSerializer.Serialize(deserialized, UniverseSourceGenerationContext.Default.Location));

        json = JsonSerializer.Serialize(iIdItem, UniverseSourceGenerationContext.Default.Location);
        Console.WriteLine();
        Console.WriteLine(json);
        Assert.AreEqual(
            value,
            JsonSerializer.Deserialize(json, UniverseSourceGenerationContext.Default.Location));

        json = JsonSerializer.Serialize(iIdItem, options);
        Console.WriteLine();
        Console.WriteLine(json);
        Assert.AreEqual(
            value,
            JsonSerializer.Deserialize(json, UniverseSourceGenerationContext.Default.Location));
        Assert.AreEqual(
            value,
            JsonSerializer.Deserialize<IIdItem>(json, options));
    }

    [TestMethod]
    public void OrbitalParametersTest()
    {
        var value = new OrbitalParameters(
            new HugeNumber(10),
            new Vector3<HugeNumber>(HugeNumber.Zero, HugeNumber.One, HugeNumber.NegativeOne),
            new HugeNumber(22, 14),
            0.2,
            0.1,
            0.5,
            0.6,
            0.7);

        var json = JsonSerializer.Serialize(value);
        Console.WriteLine();
        Console.WriteLine(json);
        var deserialized = JsonSerializer.Deserialize<OrbitalParameters>(json);
        Assert.IsNotNull(deserialized);
        Assert.AreEqual(value, deserialized);
        Assert.AreEqual(json, JsonSerializer.Serialize(deserialized));

        json = JsonSerializer.Serialize(value, UniverseSourceGenerationContext.Default.OrbitalParameters);
        Console.WriteLine();
        Console.WriteLine(json);
        deserialized = JsonSerializer.Deserialize(json, UniverseSourceGenerationContext.Default.OrbitalParameters);
        Assert.IsNotNull(deserialized);
        Assert.AreEqual(value, deserialized);
        Assert.AreEqual(
            json,
            JsonSerializer.Serialize(deserialized, UniverseSourceGenerationContext.Default.OrbitalParameters));
    }

    [TestMethod]
    public void OrbitTest()
    {
        var value = new Orbit(
            null,
            new HugeNumber(1, 100),
            Vector3<HugeNumber>.Zero,
            Vector3<HugeNumber>.Zero,
            0.1,
            0.01,
            0.2,
            0.2,
            HugeNumber.One,
            new HugeNumber(1, 10000),
            Vector3<HugeNumber>.UnitX,
            new HugeNumber(1, 10000),
            new HugeNumber(1, 10000),
            new HugeNumber(1, 9000),
            0.4,
            Vector3<HugeNumber>.One,
            new HugeNumber(1, 10000),
            Duration.Zero);

        var json = JsonSerializer.Serialize(value);
        Console.WriteLine();
        Console.WriteLine(json);
        var deserialized = JsonSerializer.Deserialize<Orbit>(json);
        Assert.IsNotNull(deserialized);
        var reserialized = JsonSerializer.Serialize(deserialized);
        Console.WriteLine(reserialized);
        Assert.AreEqual(value, deserialized);
        Assert.AreEqual(json, reserialized);

        json = JsonSerializer.Serialize(value, UniverseSourceGenerationContext.Default.Orbit);
        Console.WriteLine();
        Console.WriteLine(json);
        deserialized = JsonSerializer.Deserialize(json, UniverseSourceGenerationContext.Default.Orbit);
        Assert.IsNotNull(deserialized);
        Assert.AreEqual(value, deserialized);
        Assert.AreEqual(
            json,
            JsonSerializer.Serialize(deserialized, UniverseSourceGenerationContext.Default.Orbit));
    }

    [TestMethod]
    public void PlanetaryRingTest()
    {
        var value = new PlanetaryRing(
            true,
            new HugeNumber(13, 14),
            new HugeNumber(17, 14));

        var json = JsonSerializer.Serialize(value);
        Console.WriteLine();
        Console.WriteLine(json);
        var deserialized = JsonSerializer.Deserialize<PlanetaryRing>(json);
        Assert.IsNotNull(deserialized);
        Assert.AreEqual(value, deserialized);
        Assert.AreEqual(json, JsonSerializer.Serialize(deserialized));

        json = JsonSerializer.Serialize(value, UniverseSourceGenerationContext.Default.PlanetaryRing);
        Console.WriteLine();
        Console.WriteLine(json);
        deserialized = JsonSerializer.Deserialize(json, UniverseSourceGenerationContext.Default.PlanetaryRing);
        Assert.IsNotNull(deserialized);
        Assert.AreEqual(value, deserialized);
        Assert.AreEqual(
            json,
            JsonSerializer.Serialize(deserialized, UniverseSourceGenerationContext.Default.PlanetaryRing));
    }

    [TestMethod]
    public void PlanetoidTest()
    {
        var value = Planetoid.GetPlanetForSunlikeStar(out _);
        Assert.IsNotNull(value);
        CosmicLocation cosmicLocation = value;
        Location location = value;
        IIdItem iIdItem = value;

        var options = new JsonSerializerOptions
        {
            TypeInfoResolver = new UniverseTypeResolver()
        };

        var json = JsonSerializer.Serialize(value);

        var bytes = Encoding.UTF8.GetBytes(json);
        Console.WriteLine();
        Console.WriteLine($"Size: {BytesToString(bytes.Length)}");
        Console.WriteLine();
        Console.WriteLine("JSON:");
        Console.WriteLine(json);

        var deserialized = JsonSerializer.Deserialize<Planetoid>(json);
        Assert.IsNotNull(deserialized);
        Assert.AreEqual(value, deserialized);
        Assert.AreEqual(json, JsonSerializer.Serialize(deserialized));

        json = JsonSerializer.Serialize(cosmicLocation, cosmicLocation.GetType());
        Console.WriteLine();
        Console.WriteLine(json);
        Assert.AreEqual(value, JsonSerializer.Deserialize<Planetoid>(json));

        json = JsonSerializer.Serialize(location, location.GetType());
        Console.WriteLine();
        Console.WriteLine(json);
        Assert.AreEqual(value, JsonSerializer.Deserialize<Planetoid>(json));

        json = JsonSerializer.Serialize(iIdItem, iIdItem.GetType());
        Console.WriteLine();
        Console.WriteLine(json);
        Assert.AreEqual(value, JsonSerializer.Deserialize<Planetoid>(json));

        json = JsonSerializer.Serialize(iIdItem, options);
        Console.WriteLine();
        Console.WriteLine(json);
        Assert.AreEqual(value, JsonSerializer.Deserialize<Planetoid>(json, options));
        Assert.AreEqual(value, JsonSerializer.Deserialize<CosmicLocation>(json, options));
        Assert.AreEqual(value, JsonSerializer.Deserialize<Location>(json, options));
        Assert.AreEqual(value, JsonSerializer.Deserialize<IIdItem>(json, options));

        options = new JsonSerializerOptions
        {
            TypeInfoResolver = new UniverseTypeResolver()
        };
        options.TypeInfoResolverChain.Add(UniverseSourceGenerationContext.Default);

        json = JsonSerializer.Serialize(value, UniverseSourceGenerationContext.Default.Planetoid);
        Console.WriteLine();
        Console.WriteLine(json);
        deserialized = JsonSerializer.Deserialize(json, UniverseSourceGenerationContext.Default.Planetoid);
        Assert.IsNotNull(deserialized);
        Assert.AreEqual(value, deserialized);
        Assert.AreEqual(
            json,
            JsonSerializer.Serialize(deserialized, UniverseSourceGenerationContext.Default.Planetoid));

        json = JsonSerializer.Serialize(cosmicLocation, UniverseSourceGenerationContext.Default.Planetoid);
        Console.WriteLine();
        Console.WriteLine(json);
        Assert.AreEqual(value, JsonSerializer.Deserialize(json, UniverseSourceGenerationContext.Default.Planetoid));

        json = JsonSerializer.Serialize(location, UniverseSourceGenerationContext.Default.Planetoid);
        Console.WriteLine();
        Console.WriteLine(json);
        Assert.AreEqual(value, JsonSerializer.Deserialize(json, UniverseSourceGenerationContext.Default.Planetoid));

        json = JsonSerializer.Serialize(iIdItem, UniverseSourceGenerationContext.Default.Planetoid);
        Console.WriteLine();
        Console.WriteLine(json);
        Assert.AreEqual(
            value,
            JsonSerializer.Deserialize(json, UniverseSourceGenerationContext.Default.Planetoid));

        json = JsonSerializer.Serialize(cosmicLocation, UniverseSourceGenerationContext.Default.CosmicLocation);
        Console.WriteLine();
        Console.WriteLine(json);
        Assert.AreEqual(
            value,
            JsonSerializer.Deserialize(json, UniverseSourceGenerationContext.Default.Planetoid));
        Assert.AreEqual(
            value,
            JsonSerializer.Deserialize(json, UniverseSourceGenerationContext.Default.CosmicLocation));
        Assert.AreEqual(
            value,
            JsonSerializer.Deserialize(json, UniverseSourceGenerationContext.Default.Location));
        Assert.AreEqual(
            value,
            JsonSerializer.Deserialize<IIdItem>(json, options));

        json = JsonSerializer.Serialize(location, UniverseSourceGenerationContext.Default.Location);
        Console.WriteLine();
        Console.WriteLine(json);
        Assert.AreEqual(
            value,
            JsonSerializer.Deserialize(json, UniverseSourceGenerationContext.Default.Planetoid));
        Assert.AreEqual(
            value,
            JsonSerializer.Deserialize(json, UniverseSourceGenerationContext.Default.CosmicLocation));
        Assert.AreEqual(
            value,
            JsonSerializer.Deserialize(json, UniverseSourceGenerationContext.Default.Location));
        Assert.AreEqual(
            value,
            JsonSerializer.Deserialize<IIdItem>(json, options));

        json = JsonSerializer.Serialize(iIdItem, options);
        Console.WriteLine();
        Console.WriteLine(json);
        Assert.AreEqual(
            value,
            JsonSerializer.Deserialize(json, UniverseSourceGenerationContext.Default.Planetoid));
        Assert.AreEqual(
            value,
            JsonSerializer.Deserialize(json, UniverseSourceGenerationContext.Default.CosmicLocation));
        Assert.AreEqual(
            value,
            JsonSerializer.Deserialize(json, UniverseSourceGenerationContext.Default.Location));
        Assert.AreEqual(
            value,
            JsonSerializer.Deserialize<IIdItem>(json, options));
    }

    [TestMethod]
    public void PlanetParamsTest()
    {
        var value = PlanetParams.Earthlike;

        var json = JsonSerializer.Serialize(value);
        Console.WriteLine();
        Console.WriteLine(json);
        var deserialized = JsonSerializer.Deserialize<PlanetParams>(json);
        Assert.IsNotNull(deserialized);
        Assert.AreEqual(value, deserialized);
        Assert.AreEqual(json, JsonSerializer.Serialize(deserialized));

        json = JsonSerializer.Serialize(value, UniverseSourceGenerationContext.Default.PlanetParams);
        Console.WriteLine();
        Console.WriteLine(json);
        deserialized = JsonSerializer.Deserialize(json, UniverseSourceGenerationContext.Default.PlanetParams);
        Assert.IsNotNull(deserialized);
        Assert.AreEqual(value, deserialized);
        Assert.AreEqual(
            json,
            JsonSerializer.Serialize(deserialized, UniverseSourceGenerationContext.Default.PlanetParams));
    }

    [TestMethod]
    public void ResourceTest()
    {
        var value = new Resource(
            Substances.All.Gold.GetReference(),
            1,
            0.01m,
            true);

        var json = JsonSerializer.Serialize(value);
        Console.WriteLine();
        Console.WriteLine(json);
        var deserialized = JsonSerializer.Deserialize<Resource>(json);
        Assert.IsNotNull(deserialized);
        Assert.AreEqual(value, deserialized);
        Assert.AreEqual(json, JsonSerializer.Serialize(deserialized));

        json = JsonSerializer.Serialize(value, UniverseSourceGenerationContext.Default.Resource);
        Console.WriteLine();
        Console.WriteLine(json);
        deserialized = JsonSerializer.Deserialize(json, UniverseSourceGenerationContext.Default.Resource);
        Assert.IsNotNull(deserialized);
        Assert.AreEqual(value, deserialized);
        Assert.AreEqual(
            json,
            JsonSerializer.Serialize(deserialized, UniverseSourceGenerationContext.Default.Resource));
    }

    [TestMethod]
    public void SeedArrayTest()
    {
        var value = new SeedArray();
        for (var i = 0; i < 5; i++)
        {
            value[i] = i;
        }

        var json = JsonSerializer.Serialize(value);
        Console.WriteLine();
        Console.WriteLine(json);
        var deserialized = JsonSerializer.Deserialize<SeedArray>(json);
        Assert.IsNotNull(deserialized);
        Assert.AreEqual(value, deserialized);
        Assert.AreEqual(json, JsonSerializer.Serialize(deserialized));

        json = JsonSerializer.Serialize(value, UniverseSourceGenerationContext.Default.SeedArray);
        Console.WriteLine();
        Console.WriteLine(json);
        deserialized = JsonSerializer.Deserialize(json, UniverseSourceGenerationContext.Default.SeedArray);
        Assert.IsNotNull(deserialized);
        Assert.AreEqual(value, deserialized);
        Assert.AreEqual(
            json,
            JsonSerializer.Serialize(deserialized, UniverseSourceGenerationContext.Default.SeedArray));
    }

    [TestMethod]
    public void StarSystemTest()
    {
        var value = new StarSystem(null, Vector3<HugeNumber>.Zero, out _);
        CosmicLocation cosmicLocation = value;
        Location location = value;
        IIdItem iIdItem = value;

        var options = new JsonSerializerOptions
        {
            TypeInfoResolver = new UniverseTypeResolver()
        };

        var json = JsonSerializer.Serialize(value);
        Console.WriteLine();
        Console.WriteLine(json);
        var deserialized = JsonSerializer.Deserialize<StarSystem>(json);
        Assert.AreEqual(value, deserialized);
        Assert.AreEqual(json, JsonSerializer.Serialize(deserialized));

        json = JsonSerializer.Serialize(cosmicLocation, cosmicLocation.GetType());
        Console.WriteLine();
        Console.WriteLine(json);
        Assert.AreEqual(value, JsonSerializer.Deserialize<StarSystem>(json));

        json = JsonSerializer.Serialize(location, location.GetType());
        Console.WriteLine();
        Console.WriteLine(json);
        Assert.AreEqual(value, JsonSerializer.Deserialize<StarSystem>(json));

        json = JsonSerializer.Serialize(iIdItem, iIdItem.GetType());
        Console.WriteLine();
        Console.WriteLine(json);
        Assert.AreEqual(value, JsonSerializer.Deserialize<StarSystem>(json));

        json = JsonSerializer.Serialize(iIdItem, options);
        Console.WriteLine();
        Console.WriteLine(json);
        Assert.AreEqual(value, JsonSerializer.Deserialize<StarSystem>(json, options));
        Assert.AreEqual(value, JsonSerializer.Deserialize<CosmicLocation>(json, options));
        Assert.AreEqual(value, JsonSerializer.Deserialize<Location>(json, options));
        Assert.AreEqual(value, JsonSerializer.Deserialize<IIdItem>(json, options));

        options = new JsonSerializerOptions
        {
            TypeInfoResolver = new UniverseTypeResolver()
        };
        options.TypeInfoResolverChain.Add(UniverseSourceGenerationContext.Default);

        json = JsonSerializer.Serialize(value, UniverseSourceGenerationContext.Default.StarSystem);
        Console.WriteLine();
        Console.WriteLine(json);
        deserialized = JsonSerializer.Deserialize(json, UniverseSourceGenerationContext.Default.StarSystem);
        Assert.IsNotNull(deserialized);
        Assert.AreEqual(value, deserialized);
        Assert.AreEqual(
            json,
            JsonSerializer.Serialize(deserialized, UniverseSourceGenerationContext.Default.StarSystem));

        json = JsonSerializer.Serialize(cosmicLocation, UniverseSourceGenerationContext.Default.StarSystem);
        Console.WriteLine();
        Console.WriteLine(json);
        Assert.AreEqual(value, JsonSerializer.Deserialize(json, UniverseSourceGenerationContext.Default.StarSystem));

        json = JsonSerializer.Serialize(location, UniverseSourceGenerationContext.Default.StarSystem);
        Console.WriteLine();
        Console.WriteLine(json);
        Assert.AreEqual(value, JsonSerializer.Deserialize(json, UniverseSourceGenerationContext.Default.StarSystem));

        json = JsonSerializer.Serialize(iIdItem, UniverseSourceGenerationContext.Default.StarSystem);
        Console.WriteLine();
        Console.WriteLine(json);
        Assert.AreEqual(
            value,
            JsonSerializer.Deserialize(json, UniverseSourceGenerationContext.Default.StarSystem));

        json = JsonSerializer.Serialize(cosmicLocation, UniverseSourceGenerationContext.Default.CosmicLocation);
        Console.WriteLine();
        Console.WriteLine(json);
        Assert.AreEqual(
            value,
            JsonSerializer.Deserialize(json, UniverseSourceGenerationContext.Default.StarSystem));
        Assert.AreEqual(
            value,
            JsonSerializer.Deserialize(json, UniverseSourceGenerationContext.Default.CosmicLocation));
        Assert.AreEqual(
            value,
            JsonSerializer.Deserialize(json, UniverseSourceGenerationContext.Default.Location));
        Assert.AreEqual(
            value,
            JsonSerializer.Deserialize<IIdItem>(json, options));

        json = JsonSerializer.Serialize(location, UniverseSourceGenerationContext.Default.Location);
        Console.WriteLine();
        Console.WriteLine(json);
        Assert.AreEqual(
            value,
            JsonSerializer.Deserialize(json, UniverseSourceGenerationContext.Default.StarSystem));
        Assert.AreEqual(
            value,
            JsonSerializer.Deserialize(json, UniverseSourceGenerationContext.Default.CosmicLocation));
        Assert.AreEqual(
            value,
            JsonSerializer.Deserialize(json, UniverseSourceGenerationContext.Default.Location));
        Assert.AreEqual(
            value,
            JsonSerializer.Deserialize<IIdItem>(json, options));

        json = JsonSerializer.Serialize(iIdItem, options);
        Console.WriteLine();
        Console.WriteLine(json);
        Assert.AreEqual(
            value,
            JsonSerializer.Deserialize(json, UniverseSourceGenerationContext.Default.StarSystem));
        Assert.AreEqual(
            value,
            JsonSerializer.Deserialize(json, UniverseSourceGenerationContext.Default.CosmicLocation));
        Assert.AreEqual(
            value,
            JsonSerializer.Deserialize(json, UniverseSourceGenerationContext.Default.Location));
        Assert.AreEqual(
            value,
            JsonSerializer.Deserialize<IIdItem>(json, options));
    }

    [TestMethod]
    public void StarTest()
    {
        var value = new Star(null, Vector3<HugeNumber>.Zero);
        CosmicLocation cosmicLocation = value;
        Location location = value;
        IIdItem iIdItem = value;

        var options = new JsonSerializerOptions
        {
            TypeInfoResolver = new UniverseTypeResolver()
        };

        var json = JsonSerializer.Serialize(value);
        Console.WriteLine();
        Console.WriteLine(json);
        var deserialized = JsonSerializer.Deserialize<Star>(json);
        Assert.AreEqual(value, deserialized);
        Assert.AreEqual(json, JsonSerializer.Serialize(deserialized));

        json = JsonSerializer.Serialize(cosmicLocation, cosmicLocation.GetType());
        Console.WriteLine();
        Console.WriteLine(json);
        Assert.AreEqual(value, JsonSerializer.Deserialize<Star>(json));

        json = JsonSerializer.Serialize(location, location.GetType());
        Console.WriteLine();
        Console.WriteLine(json);
        Assert.AreEqual(value, JsonSerializer.Deserialize<Star>(json));

        json = JsonSerializer.Serialize(iIdItem, iIdItem.GetType());
        Console.WriteLine();
        Console.WriteLine(json);
        Assert.AreEqual(value, JsonSerializer.Deserialize<Star>(json));

        json = JsonSerializer.Serialize(iIdItem, options);
        Console.WriteLine();
        Console.WriteLine(json);
        Assert.AreEqual(value, JsonSerializer.Deserialize<Star>(json, options));
        Assert.AreEqual(value, JsonSerializer.Deserialize<CosmicLocation>(json, options));
        Assert.AreEqual(value, JsonSerializer.Deserialize<Location>(json, options));
        Assert.AreEqual(value, JsonSerializer.Deserialize<IIdItem>(json, options));

        value = new Star(
            null,
            Vector3<HugeNumber>.Zero,
            null,
            Space.Stars.SpectralClass.G,
            Space.Stars.LuminosityClass.V);
        cosmicLocation = value;
        location = value;
        iIdItem = value;

        json = JsonSerializer.Serialize(value);
        Console.WriteLine();
        Console.WriteLine(json);
        deserialized = JsonSerializer.Deserialize<Star>(json);
        Assert.AreEqual(value, deserialized);
        Assert.AreEqual(json, JsonSerializer.Serialize(deserialized));

        json = JsonSerializer.Serialize(cosmicLocation, cosmicLocation.GetType());
        Console.WriteLine();
        Console.WriteLine(json);
        Assert.AreEqual(value, JsonSerializer.Deserialize<Star>(json));

        json = JsonSerializer.Serialize(location, location.GetType());
        Console.WriteLine();
        Console.WriteLine(json);
        Assert.AreEqual(value, JsonSerializer.Deserialize<Star>(json));

        json = JsonSerializer.Serialize(iIdItem, iIdItem.GetType());
        Console.WriteLine();
        Console.WriteLine(json);
        Assert.AreEqual(value, JsonSerializer.Deserialize<Star>(json));

        json = JsonSerializer.Serialize(iIdItem, options);
        Console.WriteLine();
        Console.WriteLine(json);
        Assert.AreEqual(value, JsonSerializer.Deserialize<Star>(json, options));
        Assert.AreEqual(value, JsonSerializer.Deserialize<CosmicLocation>(json, options));
        Assert.AreEqual(value, JsonSerializer.Deserialize<Location>(json, options));
        Assert.AreEqual(value, JsonSerializer.Deserialize<IIdItem>(json, options));

        options = new JsonSerializerOptions
        {
            TypeInfoResolver = new UniverseTypeResolver()
        };
        options.TypeInfoResolverChain.Add(UniverseSourceGenerationContext.Default);

        json = JsonSerializer.Serialize(value, UniverseSourceGenerationContext.Default.Star);
        Console.WriteLine();
        Console.WriteLine(json);
        deserialized = JsonSerializer.Deserialize(json, UniverseSourceGenerationContext.Default.Star);
        Assert.IsNotNull(deserialized);
        Assert.AreEqual(value, deserialized);
        Assert.AreEqual(
            json,
            JsonSerializer.Serialize(deserialized, UniverseSourceGenerationContext.Default.Star));

        json = JsonSerializer.Serialize(cosmicLocation, UniverseSourceGenerationContext.Default.Star);
        Console.WriteLine();
        Console.WriteLine(json);
        Assert.AreEqual(value, JsonSerializer.Deserialize(json, UniverseSourceGenerationContext.Default.Star));

        json = JsonSerializer.Serialize(location, UniverseSourceGenerationContext.Default.Star);
        Console.WriteLine();
        Console.WriteLine(json);
        Assert.AreEqual(value, JsonSerializer.Deserialize(json, UniverseSourceGenerationContext.Default.Star));

        json = JsonSerializer.Serialize(iIdItem, UniverseSourceGenerationContext.Default.Star);
        Console.WriteLine();
        Console.WriteLine(json);
        Assert.AreEqual(
            value,
            JsonSerializer.Deserialize(json, UniverseSourceGenerationContext.Default.Star));

        json = JsonSerializer.Serialize(cosmicLocation, UniverseSourceGenerationContext.Default.CosmicLocation);
        Console.WriteLine();
        Console.WriteLine(json);
        Assert.AreEqual(
            value,
            JsonSerializer.Deserialize(json, UniverseSourceGenerationContext.Default.Star));
        Assert.AreEqual(
            value,
            JsonSerializer.Deserialize(json, UniverseSourceGenerationContext.Default.CosmicLocation));
        Assert.AreEqual(
            value,
            JsonSerializer.Deserialize(json, UniverseSourceGenerationContext.Default.Location));
        Assert.AreEqual(
            value,
            JsonSerializer.Deserialize<IIdItem>(json, options));

        json = JsonSerializer.Serialize(location, UniverseSourceGenerationContext.Default.Location);
        Console.WriteLine();
        Console.WriteLine(json);
        Assert.AreEqual(
            value,
            JsonSerializer.Deserialize(json, UniverseSourceGenerationContext.Default.Star));
        Assert.AreEqual(
            value,
            JsonSerializer.Deserialize(json, UniverseSourceGenerationContext.Default.CosmicLocation));
        Assert.AreEqual(
            value,
            JsonSerializer.Deserialize(json, UniverseSourceGenerationContext.Default.Location));
        Assert.AreEqual(
            value,
            JsonSerializer.Deserialize<IIdItem>(json, options));

        json = JsonSerializer.Serialize(iIdItem, options);
        Console.WriteLine();
        Console.WriteLine(json);
        Assert.AreEqual(
            value,
            JsonSerializer.Deserialize(json, UniverseSourceGenerationContext.Default.Star));
        Assert.AreEqual(
            value,
            JsonSerializer.Deserialize(json, UniverseSourceGenerationContext.Default.CosmicLocation));
        Assert.AreEqual(
            value,
            JsonSerializer.Deserialize(json, UniverseSourceGenerationContext.Default.Location));
        Assert.AreEqual(
            value,
            JsonSerializer.Deserialize<IIdItem>(json, options));
    }

    [TestMethod]
    public void SubstanceRequirementTest()
    {
        var value = new SubstanceRequirement(Substances.All.Water.GetHomogeneousReference());

        var json = JsonSerializer.Serialize(value);
        Console.WriteLine();
        Console.WriteLine(json);
        var deserialized = JsonSerializer.Deserialize<SubstanceRequirement>(json);
        Assert.IsNotNull(deserialized);
        Assert.AreEqual(value, deserialized);
        Assert.AreEqual(json, JsonSerializer.Serialize(deserialized));

        value = new SubstanceRequirement(
            Substances.All.Oxygen.GetHomogeneousReference(),
            0.2m,
            0.6m,
            PhaseType.Gas);

        json = JsonSerializer.Serialize(value);
        Console.WriteLine();
        Console.WriteLine(json);
        deserialized = JsonSerializer.Deserialize<SubstanceRequirement>(json);
        Assert.IsNotNull(deserialized);
        Assert.AreEqual(value, deserialized);
        Assert.AreEqual(json, JsonSerializer.Serialize(deserialized));

        json = JsonSerializer.Serialize(value, UniverseSourceGenerationContext.Default.SubstanceRequirement);
        Console.WriteLine();
        Console.WriteLine(json);
        deserialized = JsonSerializer.Deserialize(json, UniverseSourceGenerationContext.Default.SubstanceRequirement);
        Assert.IsNotNull(deserialized);
        Assert.AreEqual(value, deserialized);
        Assert.AreEqual(
            json,
            JsonSerializer.Serialize(deserialized, UniverseSourceGenerationContext.Default.SubstanceRequirement));
    }

    [TestMethod]
    public void SurfaceRegionTest()
    {
        var value = new SurfaceRegion(
            "Test_ID",
            new Sphere<HugeNumber>(new HugeNumber(10)),
            "Test_Parent_ID",
            null);
        Location location = value;
        IIdItem iIdItem = value;

        var options = new JsonSerializerOptions
        {
            TypeInfoResolver = new UniverseTypeResolver()
        };

        var json = JsonSerializer.Serialize(value);
        Console.WriteLine();
        Console.WriteLine(json);
        var deserialized = JsonSerializer.Deserialize<SurfaceRegion>(json);
        Assert.AreEqual(value, deserialized);
        Assert.AreEqual(json, JsonSerializer.Serialize(deserialized));

        json = JsonSerializer.Serialize(location, location.GetType());
        Console.WriteLine();
        Console.WriteLine(json);
        Assert.AreEqual(value, JsonSerializer.Deserialize<SurfaceRegion>(json));

        json = JsonSerializer.Serialize(iIdItem, iIdItem.GetType());
        Console.WriteLine();
        Console.WriteLine(json);
        Assert.AreEqual(value, JsonSerializer.Deserialize<Location>(json));

        json = JsonSerializer.Serialize(iIdItem, options);
        Console.WriteLine();
        Console.WriteLine(json);
        Assert.AreEqual(value, JsonSerializer.Deserialize<Location>(json, options));
        Assert.AreEqual(value, JsonSerializer.Deserialize<IIdItem>(json, options));

        value = new SurfaceRegion(
            "Test_ID",
            new Sphere<HugeNumber>(new HugeNumber(10)),
            "Test_Parent_ID",
            new Vector3<HugeNumber>[] { Vector3<HugeNumber>.Zero, Vector3<HugeNumber>.UnitX });
        location = value;
        iIdItem = value;

        json = JsonSerializer.Serialize(value);
        Console.WriteLine();
        Console.WriteLine(json);
        deserialized = JsonSerializer.Deserialize<SurfaceRegion>(json);
        Assert.AreEqual(value, deserialized);
        Assert.AreEqual(json, JsonSerializer.Serialize(deserialized));

        json = JsonSerializer.Serialize(location, location.GetType());
        Console.WriteLine();
        Console.WriteLine(json);
        Assert.AreEqual(value, JsonSerializer.Deserialize<SurfaceRegion>(json));

        json = JsonSerializer.Serialize(iIdItem, iIdItem.GetType());
        Console.WriteLine();
        Console.WriteLine(json);
        Assert.AreEqual(value, JsonSerializer.Deserialize<Location>(json));

        json = JsonSerializer.Serialize(iIdItem, options);
        Console.WriteLine();
        Console.WriteLine(json);
        Assert.AreEqual(value, JsonSerializer.Deserialize<Location>(json, options));
        Assert.AreEqual(value, JsonSerializer.Deserialize<IIdItem>(json, options));

        options = new JsonSerializerOptions
        {
            TypeInfoResolver = new UniverseTypeResolver()
        };
        options.TypeInfoResolverChain.Add(UniverseSourceGenerationContext.Default);

        json = JsonSerializer.Serialize(value, UniverseSourceGenerationContext.Default.SurfaceRegion);
        Console.WriteLine();
        Console.WriteLine(json);
        deserialized = JsonSerializer.Deserialize(json, UniverseSourceGenerationContext.Default.SurfaceRegion);
        Assert.IsNotNull(deserialized);
        Assert.AreEqual(value, deserialized);
        Assert.AreEqual(
            json,
            JsonSerializer.Serialize(deserialized, UniverseSourceGenerationContext.Default.SurfaceRegion));

        json = JsonSerializer.Serialize(location, UniverseSourceGenerationContext.Default.SurfaceRegion);
        Console.WriteLine();
        Console.WriteLine(json);
        Assert.AreEqual(value, JsonSerializer.Deserialize(json, UniverseSourceGenerationContext.Default.SurfaceRegion));

        json = JsonSerializer.Serialize(iIdItem, UniverseSourceGenerationContext.Default.SurfaceRegion);
        Console.WriteLine();
        Console.WriteLine(json);
        Assert.AreEqual(
            value,
            JsonSerializer.Deserialize(json, UniverseSourceGenerationContext.Default.SurfaceRegion));

        json = JsonSerializer.Serialize(location, UniverseSourceGenerationContext.Default.Location);
        Console.WriteLine();
        Console.WriteLine(json);
        Assert.AreEqual(
            value,
            JsonSerializer.Deserialize(json, UniverseSourceGenerationContext.Default.SurfaceRegion));
        Assert.AreEqual(
            value,
            JsonSerializer.Deserialize(json, UniverseSourceGenerationContext.Default.Location));
        Assert.AreEqual(
            value,
            JsonSerializer.Deserialize<IIdItem>(json, options));

        json = JsonSerializer.Serialize(iIdItem, options);
        Console.WriteLine();
        Console.WriteLine(json);
        Assert.AreEqual(
            value,
            JsonSerializer.Deserialize(json, UniverseSourceGenerationContext.Default.SurfaceRegion));
        Assert.AreEqual(
            value,
            JsonSerializer.Deserialize(json, UniverseSourceGenerationContext.Default.Location));
        Assert.AreEqual(
            value,
            JsonSerializer.Deserialize<IIdItem>(json, options));
    }

    [TestMethod]
    public void TerritoryTest()
    {
        var value = new Territory(
            "Test_ID",
            new Sphere<HugeNumber>(new HugeNumber(10)),
            _TestChildIds);
        Location location = value;
        IIdItem iIdItem = value;

        var options = new JsonSerializerOptions
        {
            TypeInfoResolver = new UniverseTypeResolver()
        };

        var json = JsonSerializer.Serialize(value);
        Console.WriteLine();
        Console.WriteLine(json);
        var deserialized = JsonSerializer.Deserialize<Territory>(json);
        Assert.AreEqual(value, deserialized);
        Assert.AreEqual(json, JsonSerializer.Serialize(deserialized));

        json = JsonSerializer.Serialize(location, location.GetType());
        Console.WriteLine();
        Console.WriteLine(json);
        Assert.AreEqual(value, JsonSerializer.Deserialize<Territory>(json));

        json = JsonSerializer.Serialize(iIdItem, iIdItem.GetType());
        Console.WriteLine();
        Console.WriteLine(json);
        Assert.AreEqual(value, JsonSerializer.Deserialize<Location>(json));

        json = JsonSerializer.Serialize(iIdItem, options);
        Console.WriteLine();
        Console.WriteLine(json);
        Assert.AreEqual(value, JsonSerializer.Deserialize<Location>(json, options));
        Assert.AreEqual(value, JsonSerializer.Deserialize<IIdItem>(json, options));

        value = new Territory(
            "Test_ID",
            new Sphere<HugeNumber>(new HugeNumber(10)),
            _TestChildIds,
            "Test_Parent_ID",
            new Vector3<HugeNumber>[] { Vector3<HugeNumber>.Zero, Vector3<HugeNumber>.UnitX });
        location = value;
        iIdItem = value;

        json = JsonSerializer.Serialize(value);
        Console.WriteLine();
        Console.WriteLine(json);
        deserialized = JsonSerializer.Deserialize<Territory>(json);
        Assert.AreEqual(value, deserialized);
        Assert.AreEqual(json, JsonSerializer.Serialize(deserialized));

        json = JsonSerializer.Serialize(location, location.GetType());
        Console.WriteLine();
        Console.WriteLine(json);
        Assert.AreEqual(value, JsonSerializer.Deserialize<Territory>(json));

        json = JsonSerializer.Serialize(iIdItem, iIdItem.GetType());
        Console.WriteLine();
        Console.WriteLine(json);
        Assert.AreEqual(value, JsonSerializer.Deserialize<Location>(json));

        json = JsonSerializer.Serialize(iIdItem, options);
        Console.WriteLine();
        Console.WriteLine(json);
        Assert.AreEqual(value, JsonSerializer.Deserialize<Location>(json, options));
        Assert.AreEqual(value, JsonSerializer.Deserialize<IIdItem>(json, options));

        options = new JsonSerializerOptions
        {
            TypeInfoResolver = new UniverseTypeResolver()
        };
        options.TypeInfoResolverChain.Add(UniverseSourceGenerationContext.Default);

        json = JsonSerializer.Serialize(value, UniverseSourceGenerationContext.Default.Territory);
        Console.WriteLine();
        Console.WriteLine(json);
        deserialized = JsonSerializer.Deserialize(json, UniverseSourceGenerationContext.Default.Territory);
        Assert.IsNotNull(deserialized);
        Assert.AreEqual(value, deserialized);
        Assert.AreEqual(
            json,
            JsonSerializer.Serialize(deserialized, UniverseSourceGenerationContext.Default.Territory));

        json = JsonSerializer.Serialize(location, UniverseSourceGenerationContext.Default.Territory);
        Console.WriteLine();
        Console.WriteLine(json);
        Assert.AreEqual(value, JsonSerializer.Deserialize(json, UniverseSourceGenerationContext.Default.Territory));

        json = JsonSerializer.Serialize(iIdItem, UniverseSourceGenerationContext.Default.Territory);
        Console.WriteLine();
        Console.WriteLine(json);
        Assert.AreEqual(
            value,
            JsonSerializer.Deserialize(json, UniverseSourceGenerationContext.Default.Territory));

        json = JsonSerializer.Serialize(location, UniverseSourceGenerationContext.Default.Location);
        Console.WriteLine();
        Console.WriteLine(json);
        Assert.AreEqual(
            value,
            JsonSerializer.Deserialize(json, UniverseSourceGenerationContext.Default.Territory));
        Assert.AreEqual(
            value,
            JsonSerializer.Deserialize(json, UniverseSourceGenerationContext.Default.Location));
        Assert.AreEqual(
            value,
            JsonSerializer.Deserialize<IIdItem>(json, options));

        json = JsonSerializer.Serialize(iIdItem, options);
        Console.WriteLine();
        Console.WriteLine(json);
        Assert.AreEqual(
            value,
            JsonSerializer.Deserialize(json, UniverseSourceGenerationContext.Default.Territory));
        Assert.AreEqual(
            value,
            JsonSerializer.Deserialize(json, UniverseSourceGenerationContext.Default.Location));
        Assert.AreEqual(
            value,
            JsonSerializer.Deserialize<IIdItem>(json, options));
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
