using Tavenem.Chemistry;
using Tavenem.DataStorage;
using Tavenem.Randomize;
using Tavenem.Universe.Chemistry;
using Tavenem.Universe.Climate;
using Tavenem.Universe.Space.Planetoids;

namespace Tavenem.Universe.Space;

public partial class Planetoid
{
    /// <summary>
    /// Generates a set of rings for this planetoid.
    /// </summary>
    /// <param name="min">
    /// <para>
    /// An optional minimum number of rings to generate.
    /// </para>
    /// <para>
    /// It is not guaranteed that this number is generated, if conditions preclude generation of the
    /// specified number. This value merely overrides the usual maximum for the total number of
    /// rings which would normally be generated for a planetoid.
    /// </para>
    /// </param>
    /// <param name="max">
    /// An optional maximum number of rings to generate.
    /// </param>
    private void GenerateRings(
        byte? min = null,
        byte? max = null)
    {
        if ((!min.HasValue
            || min.Value <= 0)
            && (_planetParams?.HasRings == false
            || PlanetType == PlanetType.Comet
            || IsAsteroid
            || IsDwarf))
        {
            return;
        }

        if ((!min.HasValue
            || min.Value <= 0)
            && _planetParams?.HasRings != true)
        {
            var ringChance = IsGiant ? 0.9 : 0.1;
            if (Randomizer.Instance.NextDouble() > ringChance)
            {
                return;
            }
        }

        var innerLimit = (HugeNumber)Atmosphere.AtmosphericHeight;

        var outerLimit_Icy = GetRingDistance(_IcyRingDensity);
        if (Orbit is not null)
        {
            outerLimit_Icy = HugeNumber.Min(outerLimit_Icy, GetHillSphereRadius() / 3);
        }
        if (innerLimit >= outerLimit_Icy)
        {
            return;
        }

        var outerLimit_Rocky = GetRingDistance(_RockyRingDensity);
        if (Orbit is not null)
        {
            outerLimit_Rocky = HugeNumber.Min(outerLimit_Rocky, GetHillSphereRadius() / 3);
        }

        var numRings = IsGiant
            ? (int)Math.Round(Randomizer.Instance.PositiveNormalDistributionSample(1, 1), MidpointRounding.AwayFromZero)
            : (int)Math.Round(Randomizer.Instance.PositiveNormalDistributionSample(1, 0.1667), MidpointRounding.AwayFromZero);
        if (min.HasValue)
        {
            numRings = Math.Max(min.Value, numRings);
        }
        if (max.HasValue)
        {
            numRings = Math.Min(max.Value, numRings);
        }
        if (numRings <= 0)
        {
            return;
        }
        var rings = _rings?.ToList() ?? new();
        for (var i = 0; i < numRings && innerLimit <= outerLimit_Icy; i++)
        {
            if (innerLimit < outerLimit_Rocky && Randomizer.Instance.NextBool())
            {
                var innerRadius = Randomizer.Instance.Next(innerLimit, outerLimit_Rocky);

                rings.Add(new PlanetaryRing(false, innerRadius, outerLimit_Rocky));

                outerLimit_Rocky = innerRadius;
                if (outerLimit_Rocky <= outerLimit_Icy)
                {
                    outerLimit_Icy = innerRadius;
                }
            }
            else
            {
                var innerRadius = Randomizer.Instance.Next(innerLimit, outerLimit_Icy);

                rings.Add(new PlanetaryRing(true, innerRadius, outerLimit_Icy));

                outerLimit_Icy = innerRadius;
                if (outerLimit_Icy <= outerLimit_Rocky)
                {
                    outerLimit_Rocky = innerRadius;
                }
            }
        }
        _rings = rings.Count == 0
            ? null
            : rings.AsReadOnly();
    }

    /// <summary>
    /// Generates a single satellite for this planetoid.
    /// </summary>
    /// <param name="parent">
    /// The parent location for this one.
    /// </param>
    /// <param name="stars">
    /// The set of stars in this planetoid's star system.
    /// </param>
    /// <returns>
    /// The satellite which was generated; or <see langword="null"/> if no satellite could be
    /// generated.
    /// </returns>
    public Planetoid? GenerateSatellite(
        CosmicLocation? parent,
        List<Star> stars) => GenerateSatellites(parent, stars, 1, 1)
        .FirstOrDefault();

    /// <summary>
    /// Generates a single satellite for this planetoid.
    /// </summary>
    /// <param name="dataStore">
    /// The <see cref="IDataStore"/> from which to retrieve instances.
    /// </param>
    /// <returns>
    /// The satellite which was generated; or <see langword="null"/> if no satellite could be
    /// generated.
    /// </returns>
    public async Task<Planetoid?> GenerateSatelliteAsync(IDataStore dataStore)
    {
        var satellites = await GenerateSatellitesAsync(dataStore, 1, 1);
        return satellites.FirstOrDefault();
    }

    /// <summary>
    /// Generates a set of satellites for this planetoid.
    /// </summary>
    /// <param name="parent">
    /// The parent location for this one.
    /// </param>
    /// <param name="stars">
    /// The set of stars in this planetoid's star system.
    /// </param>
    /// <param name="min">
    /// <para>
    /// An optional minimum number of satellites to generate.
    /// </para>
    /// <para>
    /// It is not guaranteed that this number is generated, if conditions preclude generation of the
    /// specified number. This value merely overrides the usual maximum for the total number of
    /// satellites which would normally be generated for a planetoid.
    /// </para>
    /// </param>
    /// <param name="max">
    /// An optional maximum number of satellites to generate.
    /// </param>
    /// <returns>
    /// A list of the satellites which were generated.
    /// </returns>
    public List<Planetoid> GenerateSatellites(
        CosmicLocation? parent,
        List<Star> stars,
        byte? min = null,
        byte? max = null)
    {
        var addedSatellites = new List<Planetoid>();
        if (max == 0)
        {
            return addedSatellites;
        }

        int maxSatellites;
        if (_planetParams?.NumSatellites.HasValue == true)
        {
            maxSatellites = _planetParams!.Value.NumSatellites!.Value;
        }
        else
        {
            maxSatellites = PlanetType switch
            {
                // 5 for most Planemos. For reference, Pluto has 5 moons, the most of any planemo in the
                // Solar System apart from the giants. No others are known to have more than 2.
                PlanetType.Terrestrial => 5,
                PlanetType.Carbon => 5,
                PlanetType.Iron => 5,
                PlanetType.Ocean => 5,
                PlanetType.Dwarf => 5,
                PlanetType.RockyDwarf => 5,

                // Lava planets are too unstable for satellites.
                PlanetType.Lava => 0,
                PlanetType.LavaDwarf => 0,

                // Set to 75 for Giant. For reference, Jupiter has 67 moons, and Saturn has 62
                // (non-ring) moons.
                PlanetType.GasGiant => 75,

                // Set to 40 for IceGiant. For reference, Uranus has 27 moons, and Neptune has 14 moons.
                PlanetType.IceGiant => 40,

                _ => 1,
            };
        }

        if (min.HasValue)
        {
            maxSatellites = Math.Max(
                maxSatellites,
                (_satelliteIds?.Count ?? 0) + min.Value);
        }

        if (maxSatellites <= 0)
        {
            return addedSatellites;
        }

        var minPeriapsis = Shape.ContainingRadius + 20;
        var maxApoapsis = Orbit.HasValue ? GetHillSphereRadius() / 3 : Shape.ContainingRadius * 100;

        var satelliteIds = _satelliteIds?.ToList() ?? new();
        while (minPeriapsis <= maxApoapsis && satelliteIds.Count < maxSatellites)
        {
            var periapsis = Randomizer.Instance.Next(minPeriapsis, maxApoapsis);

            var maxEccentricity = (double)((maxApoapsis - periapsis) / (maxApoapsis + periapsis));
            var eccentricity = maxEccentricity < 0.01
                ? Randomizer.Instance.NextDouble(0, maxEccentricity)
                : Randomizer.Instance.PositiveNormalDistributionSample(0, 0.05, maximum: maxEccentricity);

            var semiLatusRectum = periapsis * (1 + eccentricity);
            var semiMajorAxis = semiLatusRectum / (1 - (eccentricity * eccentricity));

            // Keep mass under the limit where the orbital barycenter would be pulled outside the boundaries of this body.
            var maxMass = HugeNumber.Max(0, Mass / ((semiMajorAxis / Shape.ContainingRadius) - 1));

            var satellite = GenerateSatellite(parent, stars, periapsis, eccentricity, maxMass);
            if (satellite is null)
            {
                break;
            }
            addedSatellites.Add(satellite);

            satelliteIds.Add(satellite.Id);

            if (max.HasValue
                && addedSatellites.Count >= max)
            {
                break;
            }

            minPeriapsis = (satellite.Orbit?.Apoapsis ?? 0) + satellite.GetSphereOfInfluenceRadius();
        }
        _satelliteIds = satelliteIds.Count == 0
            ? null
            : satelliteIds.AsReadOnly();

        return addedSatellites;
    }

    /// <summary>
    /// Generates a set of satellites for this planetoid.
    /// </summary>
    /// <param name="dataStore">
    /// The <see cref="IDataStore"/> from which to retrieve instances.
    /// </param>
    /// <param name="min">
    /// <para>
    /// An optional minimum number of satellites to generate.
    /// </para>
    /// <para>
    /// It is not guaranteed that this number is generated, if conditions preclude generation of the
    /// specified number. This value merely overrides the usual maximum for the total number of
    /// satellites which would normally be generated for a planetoid.
    /// </para>
    /// </param>
    /// <param name="max">
    /// An optional maximum number of satellites to generate.
    /// </param>
    /// <returns>
    /// A list of the satellites which were generated.
    /// </returns>
    public async Task<List<Planetoid>> GenerateSatellitesAsync(
        IDataStore dataStore,
        byte? min = null,
        byte? max = null)
    {
        var parent = await GetParentAsync(dataStore) as CosmicLocation;
        var starSystem = parent is StarSystem parentStarSystem
            ? parentStarSystem
            : await GetStarSystemAsync(dataStore);
        if (starSystem is null)
        {
            return new List<Planetoid>();
        }

        var stars = new List<Star>();
        await foreach (var star in starSystem.GetStarsAsync(dataStore))
        {
            stars.Add(star);
        }

        return GenerateSatellites(parent, stars, min, max);
    }

    private static IEnumerable<IMaterial<HugeNumber>> GetCore_Giant(
        IShape<HugeNumber> planetShape,
        HugeNumber coreProportion,
        HugeNumber planetMass)
    {
        var coreMass = planetMass * coreProportion;

        var coreTemp = (double)(planetShape.ContainingRadius / 3);

        var innerCoreProportion = HugeNumber.Min(
            Randomizer.Instance.Next(
                new HugeNumber(2, -2),
                new HugeNumber(2, -1)),
            _GiantMinMassForType / coreMass);
        var innerCoreMass = coreMass * innerCoreProportion;
        var innerCoreRadius = planetShape.ContainingRadius * coreProportion * innerCoreProportion;
        var innerCoreShape = new Sphere<HugeNumber>(innerCoreRadius, planetShape.Position);
        yield return new Material<HugeNumber>(
            Substances.All.IronNickelAlloy.GetHomogeneousReference(),
            innerCoreShape,
            innerCoreMass,
            null,
            coreTemp);

        // Molten rock outer core.
        var outerCoreMass = coreMass - innerCoreMass;
        var outerCoreShape = new HollowSphere<HugeNumber>(innerCoreRadius, planetShape.ContainingRadius * coreProportion, planetShape.Position);
        yield return new Material<HugeNumber>(
            CosmicSubstances.ChondriticRock,
            outerCoreShape,
            outerCoreMass,
            null,
            coreTemp);
    }

    private static IEnumerable<IMaterial<HugeNumber>> GetCrust_Carbon(
        IShape<HugeNumber> planetShape,
        HugeNumber crustProportion,
        HugeNumber planetMass)
    {
        var crustMass = planetMass * crustProportion;

        var shape = new HollowSphere<HugeNumber>(
            planetShape.ContainingRadius - (planetShape.ContainingRadius * crustProportion),
            planetShape.ContainingRadius,
            planetShape.Position);

        // Carbonaceous crust of graphite, diamond, and hydrocarbons, with trace minerals

        var graphite = 1m;

        var aluminium = (decimal)Randomizer.Instance.NormalDistributionSample(0.026, 4e-3, minimum: 0);
        var iron = (decimal)Randomizer.Instance.NormalDistributionSample(1.67e-2, 2.75e-3, minimum: 0);
        var titanium = (decimal)Randomizer.Instance.NormalDistributionSample(5.7e-3, 9e-4, minimum: 0);

        var chalcopyrite = (decimal)Randomizer.Instance.NormalDistributionSample(1.1e-3, 1.8e-4, minimum: 0); // copper
        graphite -= chalcopyrite;
        var chromite = (decimal)Randomizer.Instance.NormalDistributionSample(5.5e-4, 9e-5, minimum: 0);
        graphite -= chromite;
        var sphalerite = (decimal)Randomizer.Instance.NormalDistributionSample(8.1e-5, 1.3e-5, minimum: 0); // zinc
        graphite -= sphalerite;
        var galena = (decimal)Randomizer.Instance.NormalDistributionSample(2e-5, 3.3e-6, minimum: 0); // lead
        graphite -= galena;
        var uraninite = (decimal)Randomizer.Instance.NormalDistributionSample(7.15e-6, 1.1e-6, minimum: 0);
        graphite -= uraninite;
        var cassiterite = (decimal)Randomizer.Instance.NormalDistributionSample(6.7e-6, 1.1e-6, minimum: 0); // tin
        graphite -= cassiterite;
        var cinnabar = (decimal)Randomizer.Instance.NormalDistributionSample(1.35e-7, 2.3e-8, minimum: 0); // mercury
        graphite -= cinnabar;
        var acanthite = (decimal)Randomizer.Instance.NormalDistributionSample(5e-8, 8.3e-9, minimum: 0); // silver
        graphite -= acanthite;
        var sperrylite = (decimal)Randomizer.Instance.NormalDistributionSample(1.17e-8, 2e-9, minimum: 0); // platinum
        graphite -= sperrylite;
        var gold = (decimal)Randomizer.Instance.NormalDistributionSample(2.75e-9, 4.6e-10, minimum: 0);
        graphite -= gold;

        var bauxite = aluminium * 1.57m;
        graphite -= bauxite;

        var hematiteIron = iron * 3 / 4 * (decimal)Randomizer.Instance.NormalDistributionSample(1, 0.167, minimum: 0);
        var hematite = hematiteIron * 2.88m;
        graphite -= hematite;
        var magnetite = (iron - hematiteIron) * 4.14m;
        graphite -= magnetite;

        var ilmenite = titanium * 2.33m;
        graphite -= ilmenite;

        var coal = graphite * (decimal)Randomizer.Instance.NormalDistributionSample(0.25, 0.042, minimum: 0);
        graphite -= coal * 2;
        var oil = graphite * (decimal)Randomizer.Instance.NormalDistributionSample(0.25, 0.042, minimum: 0);
        graphite -= oil;
        var gas = graphite * (decimal)Randomizer.Instance.NormalDistributionSample(0.25, 0.042, minimum: 0);
        graphite -= gas;
        var diamond = graphite * (decimal)Randomizer.Instance.NormalDistributionSample(0.125, 0.021, minimum: 0);
        graphite -= diamond;

        var components = new List<(ISubstanceReference, decimal)>();
        if (graphite > 0)
        {
            components.Add((Substances.All.AmorphousCarbon.GetHomogeneousReference(), graphite));
        }
        if (coal > 0)
        {
            components.Add((Substances.All.Anthracite.GetReference(), coal));
            components.Add((Substances.All.BituminousCoal.GetReference(), coal));
        }
        if (oil > 0)
        {
            components.Add((Substances.All.Petroleum.GetReference(), oil));
        }
        if (gas > 0)
        {
            components.Add((Substances.All.NaturalGas.GetReference(), gas));
        }
        if (diamond > 0)
        {
            components.Add((Substances.All.Diamond.GetHomogeneousReference(), diamond));
        }

        if (chalcopyrite > 0)
        {
            components.Add((Substances.All.Chalcopyrite.GetHomogeneousReference(), chalcopyrite));
        }
        if (chromite > 0)
        {
            components.Add((Substances.All.Chromite.GetHomogeneousReference(), chromite));
        }
        if (sphalerite > 0)
        {
            components.Add((Substances.All.Sphalerite.GetHomogeneousReference(), sphalerite));
        }
        if (galena > 0)
        {
            components.Add((Substances.All.Galena.GetHomogeneousReference(), galena));
        }
        if (uraninite > 0)
        {
            components.Add((Substances.All.Uraninite.GetHomogeneousReference(), uraninite));
        }
        if (cassiterite > 0)
        {
            components.Add((Substances.All.Cassiterite.GetHomogeneousReference(), cassiterite));
        }
        if (cinnabar > 0)
        {
            components.Add((Substances.All.Cinnabar.GetHomogeneousReference(), cinnabar));
        }
        if (acanthite > 0)
        {
            components.Add((Substances.All.Acanthite.GetHomogeneousReference(), acanthite));
        }
        if (sperrylite > 0)
        {
            components.Add((Substances.All.Sperrylite.GetHomogeneousReference(), sperrylite));
        }
        if (gold > 0)
        {
            components.Add((Substances.All.Gold.GetHomogeneousReference(), gold));
        }
        if (bauxite > 0)
        {
            components.Add((Substances.All.Bauxite.GetReference(), bauxite));
        }
        if (hematite > 0)
        {
            components.Add((Substances.All.Hematite.GetHomogeneousReference(), hematite));
        }
        if (magnetite > 0)
        {
            components.Add((Substances.All.Magnetite.GetHomogeneousReference(), magnetite));
        }
        if (ilmenite > 0)
        {
            components.Add((Substances.All.Ilmenite.GetHomogeneousReference(), ilmenite));
        }

        yield return new Material<HugeNumber>(
            shape,
            crustMass,
            null,
            null,
            components.ToArray());
    }

    private static IEnumerable<IMaterial<HugeNumber>> GetCrust_LavaDwarf(
        IShape<HugeNumber> planetShape,
        HugeNumber crustProportion,
        HugeNumber planetMass)
    {
        var crustMass = planetMass * crustProportion;

        var shape = new HollowSphere<HugeNumber>(
            planetShape.ContainingRadius - (planetShape.ContainingRadius * crustProportion),
            planetShape.ContainingRadius,
            planetShape.Position);

        // rocky crust
        // 50% chance of dust
        var dust = Math.Max(0, Randomizer.Instance.NextDecimal(-0.5m, 0.5m));
        var rock = 1 - dust;

        var components = new List<(ISubstanceReference, decimal)>();
        foreach (var (material, proportion) in CosmicSubstances.DryPlanetaryCrustConstituents)
        {
            components.Add((material, proportion * rock));
        }
        if (dust > 0)
        {
            components.Add((Substances.All.CosmicDust.GetHomogeneousReference(), dust));
        }
        yield return new Material<HugeNumber>(
            components,
            shape,
            crustMass);
    }

    private static IEnumerable<IMaterial<HugeNumber>> GetCrust_RockyDwarf(
        IShape<HugeNumber> planetShape,
        HugeNumber crustProportion,
        HugeNumber planetMass)
    {
        var crustMass = planetMass * crustProportion;

        var shape = new HollowSphere<HugeNumber>(
            planetShape.ContainingRadius - (planetShape.ContainingRadius * crustProportion),
            planetShape.ContainingRadius,
            planetShape.Position);

        // rocky crust
        // 50% chance of dust
        var dust = Math.Max(0, Randomizer.Instance.NextDecimal(-0.5m, 0.5m));
        var rock = 1 - dust;

        var components = new List<(ISubstanceReference, decimal)>();
        foreach (var (material, proportion) in CosmicSubstances.DryPlanetaryCrustConstituents)
        {
            components.Add((material, proportion * rock));
        }
        if (dust > 0)
        {
            components.Add((Substances.All.CosmicDust.GetHomogeneousReference(), dust));
        }
        yield return new Material<HugeNumber>(
            components,
            shape,
            crustMass);
    }

    private static IEnumerable<IMaterial<HugeNumber>> GetCrust_Terrestrial(
        IShape<HugeNumber> planetShape,
        HugeNumber crustProportion,
        HugeNumber planetMass)
    {
        var crustMass = planetMass * crustProportion;

        var shape = new HollowSphere<HugeNumber>(
            planetShape.ContainingRadius - (planetShape.ContainingRadius * crustProportion),
            planetShape.ContainingRadius,
            planetShape.Position);

        // Rocky crust with trace minerals

        var rock = 1m;

        var aluminium = (decimal)Randomizer.Instance.NormalDistributionSample(0.026, 4e-3, minimum: 0);
        var iron = (decimal)Randomizer.Instance.NormalDistributionSample(1.67e-2, 2.75e-3, minimum: 0);
        var titanium = (decimal)Randomizer.Instance.NormalDistributionSample(5.7e-3, 9e-4, minimum: 0);

        var chalcopyrite = (decimal)Randomizer.Instance.NormalDistributionSample(1.1e-3, 1.8e-4, minimum: 0); // copper
        rock -= chalcopyrite;
        var chromite = (decimal)Randomizer.Instance.NormalDistributionSample(5.5e-4, 9e-5, minimum: 0);
        rock -= chromite;
        var sphalerite = (decimal)Randomizer.Instance.NormalDistributionSample(8.1e-5, 1.3e-5, minimum: 0); // zinc
        rock -= sphalerite;
        var galena = (decimal)Randomizer.Instance.NormalDistributionSample(2e-5, 3.3e-6, minimum: 0); // lead
        rock -= galena;
        var uraninite = (decimal)Randomizer.Instance.NormalDistributionSample(7.15e-6, 1.1e-6, minimum: 0);
        rock -= uraninite;
        var cassiterite = (decimal)Randomizer.Instance.NormalDistributionSample(6.7e-6, 1.1e-6, minimum: 0); // tin
        rock -= cassiterite;
        var cinnabar = (decimal)Randomizer.Instance.NormalDistributionSample(1.35e-7, 2.3e-8, minimum: 0); // mercury
        rock -= cinnabar;
        var acanthite = (decimal)Randomizer.Instance.NormalDistributionSample(5e-8, 8.3e-9, minimum: 0); // silver
        rock -= acanthite;
        var sperrylite = (decimal)Randomizer.Instance.NormalDistributionSample(1.17e-8, 2e-9, minimum: 0); // platinum
        rock -= sperrylite;
        var gold = (decimal)Randomizer.Instance.NormalDistributionSample(2.75e-9, 4.6e-10, minimum: 0);
        rock -= gold;

        var bauxite = aluminium * 1.57m;
        rock -= bauxite;

        var hematiteIron = iron * 3 / 4 * (decimal)Randomizer.Instance.NormalDistributionSample(1, 0.167, minimum: 0);
        var hematite = hematiteIron * 2.88m;
        rock -= hematite;
        var magnetite = (iron - hematiteIron) * 4.14m;
        rock -= magnetite;

        var ilmenite = titanium * 2.33m;
        rock -= ilmenite;

        var components = new List<(ISubstanceReference, decimal)>();
        foreach (var (material, proportion) in CosmicSubstances.DryPlanetaryCrustConstituents)
        {
            components.Add((material, proportion * rock));
        }

        if (chalcopyrite > 0)
        {
            components.Add((Substances.All.Chalcopyrite.GetHomogeneousReference(), chalcopyrite));
        }
        if (chromite > 0)
        {
            components.Add((Substances.All.Chromite.GetHomogeneousReference(), chromite));
        }
        if (sphalerite > 0)
        {
            components.Add((Substances.All.Sphalerite.GetHomogeneousReference(), sphalerite));
        }
        if (galena > 0)
        {
            components.Add((Substances.All.Galena.GetHomogeneousReference(), galena));
        }
        if (uraninite > 0)
        {
            components.Add((Substances.All.Uraninite.GetHomogeneousReference(), uraninite));
        }
        if (cassiterite > 0)
        {
            components.Add((Substances.All.Cassiterite.GetHomogeneousReference(), cassiterite));
        }
        if (cinnabar > 0)
        {
            components.Add((Substances.All.Cinnabar.GetHomogeneousReference(), cinnabar));
        }
        if (acanthite > 0)
        {
            components.Add((Substances.All.Acanthite.GetHomogeneousReference(), acanthite));
        }
        if (sperrylite > 0)
        {
            components.Add((Substances.All.Sperrylite.GetHomogeneousReference(), sperrylite));
        }
        if (gold > 0)
        {
            components.Add((Substances.All.Gold.GetHomogeneousReference(), gold));
        }
        if (bauxite > 0)
        {
            components.Add((Substances.All.Bauxite.GetReference(), bauxite));
        }
        if (hematite > 0)
        {
            components.Add((Substances.All.Hematite.GetHomogeneousReference(), hematite));
        }
        if (magnetite > 0)
        {
            components.Add((Substances.All.Magnetite.GetHomogeneousReference(), magnetite));
        }
        if (ilmenite > 0)
        {
            components.Add((Substances.All.Ilmenite.GetHomogeneousReference(), ilmenite));
        }

        yield return new Material<HugeNumber>(
            shape,
            crustMass,
            null,
            null,
            components.ToArray());
    }

    private static double GetDensity(PlanetType planetType)
    {
        if (planetType == PlanetType.GasGiant)
        {
            // Relatively low chance of a "puffy" giant (Saturn-like, low-density).
            return Randomizer.Instance.NextDouble() <= 0.2
                ? Randomizer.Instance.NextDouble(GiantSubMinDensity, GiantMinDensity)
                : Randomizer.Instance.NextDouble(GiantMinDensity, GiantMaxDensity);
        }
        if (planetType == PlanetType.IceGiant)
        {
            // No "puffy" ice giants.
            return Randomizer.Instance.NextDouble(GiantMinDensity, GiantMaxDensity);
        }
        if (planetType == PlanetType.Iron)
        {
            return Randomizer.Instance.NextDouble(5250, 8000);
        }
        if (PlanetType.AnyTerrestrial.HasFlag(planetType))
        {
            return Randomizer.Instance.NextDouble(3750, DefaultTerrestrialMaxDensity);
        }

        return planetType switch
        {
            PlanetType.Dwarf => DensityForDwarf,
            PlanetType.LavaDwarf => 4000,
            PlanetType.RockyDwarf => 4000,
            _ => 2000,
        };
    }

    private static IEnumerable<IMaterial<HugeNumber>> GetMantle_Carbon(
        IShape<HugeNumber> planetShape,
        HugeNumber mantleProportion,
        HugeNumber crustProportion,
        HugeNumber planetMass,
        IShape<HugeNumber> coreShape,
        double coreTemp)
    {
        var mantleBoundaryDepth = planetShape.ContainingRadius * crustProportion;
        var mantleBoundaryTemp = (double)(mantleBoundaryDepth * new HugeNumber(115, -2));

        var innerTemp = coreTemp;

        var innerBoundary = planetShape.ContainingRadius;
        var mantleTotalDepth = (innerBoundary * mantleProportion) - coreShape.ContainingRadius;

        var mantleMass = planetMass * mantleProportion;

        // Molten silicon carbide lower mantle
        var lowerLayer = HugeNumber.Max(
            0,
            Randomizer.Instance.Next(
                -HugeNumberConstants.Deci,
                new HugeNumber(55, -2)))
            / mantleProportion;
        if (lowerLayer.IsPositive())
        {
            var lowerLayerMass = mantleMass * lowerLayer;

            var lowerLayerBoundary = innerBoundary + (mantleTotalDepth * mantleProportion);
            var lowerLayerShape = new HollowSphere<HugeNumber>(
                innerBoundary,
                lowerLayerBoundary,
                planetShape.Position);
            innerBoundary = lowerLayerBoundary;

            var lowerLayerBoundaryTemp = innerTemp.Lerp(mantleBoundaryTemp, (double)lowerLayer);
            var lowerLayerTemp = (lowerLayerBoundaryTemp + innerTemp) / 2;
            innerTemp = lowerLayerTemp;

            yield return new Material<HugeNumber>(
                Substances.All.SiliconCarbide.GetHomogeneousReference(),
                lowerLayerShape,
                lowerLayerMass,
                null,
                lowerLayerTemp);
        }

        // Diamond upper layer
        var upperLayerProportion = 1 - lowerLayer;

        var upperLayerMass = mantleMass * upperLayerProportion;

        var upperLayerBoundary = planetShape.ContainingRadius + mantleBoundaryDepth;
        var upperLayerShape = new HollowSphere<HugeNumber>(
            innerBoundary,
            upperLayerBoundary,
            planetShape.Position);

        var upperLayerTemp = (mantleBoundaryTemp + innerTemp) / 2;

        yield return new Material<HugeNumber>(
            Substances.All.Diamond.GetHomogeneousReference(),
            upperLayerShape,
            upperLayerMass,
            null,
            upperLayerTemp);
    }

    private static IEnumerable<IMaterial<HugeNumber>> GetMantle_Giant(
        IShape<HugeNumber> planetShape,
        HugeNumber mantleProportion,
        HugeNumber crustProportion,
        HugeNumber planetMass,
        IShape<HugeNumber> coreShape,
        double coreTemp)
    {
        var mantleBoundaryDepth = planetShape.ContainingRadius * crustProportion;
        var mantleBoundaryTemp = (double)mantleBoundaryDepth * 1.15;

        var innerTemp = coreTemp;

        var innerBoundary = planetShape.ContainingRadius;
        var mantleTotalDepth = (innerBoundary * mantleProportion) - coreShape.ContainingRadius;

        var mantleMass = planetMass * mantleProportion;

        // Metallic hydrogen lower mantle
        var metalH = HugeNumber.Max(
            HugeNumber.Zero,
            Randomizer.Instance.Next(
                -HugeNumberConstants.Deci,
                new HugeNumber(55, -2)))
            / mantleProportion;
        if (metalH.IsPositive())
        {
            var metalHMass = mantleMass * metalH;

            var metalHBoundary = innerBoundary + (mantleTotalDepth * mantleProportion);
            var metalHShape = new HollowSphere<HugeNumber>(
                innerBoundary,
                metalHBoundary,
                planetShape.Position);
            innerBoundary = metalHBoundary;

            var metalHBoundaryTemp = innerTemp.Lerp(mantleBoundaryTemp, (double)metalH);
            var metalHTemp = (metalHBoundaryTemp + innerTemp) / 2;
            innerTemp = metalHTemp;

            yield return new Material<HugeNumber>(
                Substances.All.MetallicHydrogen.GetHomogeneousReference(),
                metalHShape,
                metalHMass,
                null,
                metalHTemp);
        }

        // Supercritical fluid upper layer (blends seamlessly with lower atmosphere)
        var upperLayerProportion = 1 - metalH;

        var upperLayerMass = mantleMass * upperLayerProportion;

        var upperLayerBoundary = planetShape.ContainingRadius + mantleBoundaryDepth;
        var upperLayerShape = new HollowSphere<HugeNumber>(
            innerBoundary,
            upperLayerBoundary,
            planetShape.Position);

        var upperLayerTemp = (mantleBoundaryTemp + innerTemp) / 2;

        var water = (decimal)upperLayerProportion;
        var fluidH = water * 0.71m;
        water -= fluidH;
        var fluidHe = water * 0.24m;
        water -= fluidHe;
        var ne = Randomizer.Instance.NextDecimal() * water;
        water -= ne;
        var ch4 = Randomizer.Instance.NextDecimal() * water;
        water = Math.Max(0, water - ch4);
        var nh4 = Randomizer.Instance.NextDecimal() * water;
        water = Math.Max(0, water - nh4);
        var c2h6 = Randomizer.Instance.NextDecimal() * water;
        water = Math.Max(0, water - c2h6);

        var components = new List<(ISubstanceReference, decimal)>()
        {
            (Substances.All.Hydrogen.GetHomogeneousReference(), 0.71m),
            (Substances.All.Helium.GetHomogeneousReference(), 0.24m),
            (Substances.All.Neon.GetHomogeneousReference(), ne),
        };
        if (ch4 > 0)
        {
            components.Add((Substances.All.Methane.GetHomogeneousReference(), ch4));
        }
        if (nh4 > 0)
        {
            components.Add((Substances.All.Ammonia.GetHomogeneousReference(), nh4));
        }
        if (c2h6 > 0)
        {
            components.Add((Substances.All.Ethane.GetHomogeneousReference(), c2h6));
        }
        if (water > 0)
        {
            components.Add((Substances.All.Water.GetHomogeneousReference(), water));
        }

        yield return new Material<HugeNumber>(
            upperLayerShape,
            upperLayerMass,
            null,
            upperLayerTemp,
            components.ToArray());
    }

    private static IEnumerable<IMaterial<HugeNumber>> GetMantle_IceGiant(
        IShape<HugeNumber> planetShape,
        HugeNumber mantleProportion,
        HugeNumber crustProportion,
        HugeNumber planetMass,
        IShape<HugeNumber> coreShape,
        double coreTemp)
    {
        var mantleBoundaryDepth = planetShape.ContainingRadius * crustProportion;
        var mantleBoundaryTemp = (double)(mantleBoundaryDepth * new HugeNumber(115, -2));

        var innerTemp = coreTemp;

        var innerBoundary = planetShape.ContainingRadius;
        var mantleTotalDepth = (innerBoundary * mantleProportion) - coreShape.ContainingRadius;

        var mantleMass = planetMass * mantleProportion;

        var diamond = 1m;
        var water = Math.Max(0, Randomizer.Instance.NextDecimal() * diamond);
        diamond -= water;
        var nh4 = Math.Max(0, Randomizer.Instance.NextDecimal() * diamond);
        diamond -= nh4;
        var ch4 = Math.Max(0, Randomizer.Instance.NextDecimal() * diamond);
        diamond -= ch4;

        // Liquid diamond mantle
        if (diamond > 0)
        {
            var diamondMass = mantleMass * (HugeNumber)diamond;

            var diamondBoundary = innerBoundary + (mantleTotalDepth * mantleProportion);
            var diamondShape = new HollowSphere<HugeNumber>(
                innerBoundary,
                diamondBoundary,
                planetShape.Position);
            innerBoundary = diamondBoundary;

            var diamondBoundaryTemp = innerTemp.Lerp(mantleBoundaryTemp, (double)diamond);
            var diamondTemp = (diamondBoundaryTemp + innerTemp) / 2;
            innerTemp = diamondTemp;

            yield return new Material<HugeNumber>(
                Substances.All.Diamond.GetHomogeneousReference(),
                diamondShape,
                diamondMass,
                null,
                diamondTemp);
        }

        // Supercritical water-ammonia ocean layer (blends seamlessly with lower atmosphere)
        var upperLayerProportion = 1 - diamond;

        var upperLayerMass = mantleMass * (HugeNumber)upperLayerProportion;

        var upperLayerBoundary = planetShape.ContainingRadius + mantleBoundaryDepth;
        var upperLayerShape = new HollowSphere<HugeNumber>(
            innerBoundary,
            upperLayerBoundary,
            planetShape.Position);

        var upperLayerTemp = (mantleBoundaryTemp + innerTemp) / 2;

        var components = new List<(ISubstanceReference, decimal)>();
        if (ch4 > 0 || nh4 > 0)
        {
            components.Add((Substances.All.Water.GetHomogeneousReference(), water));
            if (ch4 > 0)
            {
                components.Add((Substances.All.Methane.GetHomogeneousReference(), ch4));
            }
            if (nh4 > 0)
            {
                components.Add((Substances.All.Ammonia.GetHomogeneousReference(), nh4));
            }
        }
        else
        {
            components.Add((Substances.All.Water.GetHomogeneousReference(), 1));
        }

        yield return new Material<HugeNumber>(
            upperLayerShape,
            upperLayerMass,
            null,
            upperLayerTemp,
            components.ToArray());
    }

    private static HugeNumber GetMass(PlanetType planetType, HugeNumber semiMajorAxis, HugeNumber? maxMass, double gravity, IShape<HugeNumber>? shape)
    {
        var min = HugeNumber.Zero;
        if (!PlanetType.AnyDwarf.HasFlag(planetType))
        {
            // Stern-Levison parameter for neighborhood-clearing used to determined minimum mass
            // at which the planet would be able to do so at this orbital distance. We set the
            // minimum at two orders of magnitude more than this (planets in our solar system
            // all have masses above 5 orders of magnitude more). Note that since lambda is
            // proportional to the square of mass, it is multiplied by 10 to obtain a difference
            // of 2 orders of magnitude, rather than by 100.
            var sternLevisonLambdaMass = (HugeNumber.Pow(semiMajorAxis, new HugeNumber(15, -1)) / new HugeNumber(2.5, -28)).Sqrt();
            min = HugeNumber.Max(min, sternLevisonLambdaMass * 10);
            if (min > maxMass && maxMass.HasValue)
            {
                min = maxMass.Value; // sanity check; may result in a "planet" which *can't* clear its neighborhood
            }
        }

        var mass = shape is null ? HugeNumber.Zero : gravity * shape.ContainingRadius * shape.ContainingRadius / HugeNumberConstants.G;
        return HugeNumber.Max(min, maxMass.HasValue ? HugeNumber.Min(maxMass.Value, mass) : mass);
    }

    private static HugeNumber GetMaxMassForType(PlanetType planetType) => planetType switch
    {
        PlanetType.Dwarf => _DwarfMaxMassForType,
        PlanetType.LavaDwarf => _DwarfMaxMassForType,
        PlanetType.RockyDwarf => _DwarfMaxMassForType,
        PlanetType.GasGiant => _GiantMaxMassForType,
        PlanetType.IceGiant => _GiantMaxMassForType,
        _ => _TerrestrialMaxMassForType,
    };

    private static HugeNumber GetRadiusForMass(HugeNumber density, HugeNumber mass)
        => (mass / density / HugeNumberConstants.FourThirdsPi).Cbrt();

    private int AddResource(ISubstanceReference substance, decimal proportion, bool isVein, bool isPerturbation = false, int? seed = null)
    {
        var resource = new Resource(
            substance,
            seed ?? unchecked((int)SeedGenerator.GetNewSeed()),
            proportion,
            isVein,
            isPerturbation);
        (_resources ??= new()).Add(resource);
        return resource.Seed;
    }

    private void AddResources(IEnumerable<(ISubstanceReference substance, decimal proportion, bool vein)> resources)
    {
        foreach (var (substance, proportion, vein) in resources)
        {
            AddResource(substance, proportion, vein);
        }
    }

    private double CalculateGasPhaseMix(
        HomogeneousReference substance,
        double surfaceTemp,
        double adjustedAtmosphericPressure)
    {
        var proportionInHydrosphere = Hydrosphere.GetProportion(substance);
        var water = Substances.All.Water.GetHomogeneousReference();
        var isWater = substance.Equals(water);
        if (isWater)
        {
            proportionInHydrosphere = Hydrosphere.GetProportion(x =>
                x.Equals(Substances.All.Seawater.GetHomogeneousReference())
                || x.Equals(water));
        }

        var vaporProportion = Atmosphere.Material.GetProportion(substance);

        var sub = substance.Homogeneous;
        var vaporPressure = sub.GetVaporPressure(surfaceTemp) ?? 0;

        if (surfaceTemp < sub.AntoineMinimumTemperature
            || (surfaceTemp <= sub.AntoineMaximumTemperature
            && Atmosphere.AtmosphericPressure > vaporPressure))
        {
            CondenseAtmosphericComponent(
                sub,
                surfaceTemp,
                proportionInHydrosphere,
                vaporProportion,
                vaporPressure,
                ref adjustedAtmosphericPressure);
        }
        // This indicates that the chemical will fully boil off.
        else if (proportionInHydrosphere > 0)
        {
            EvaporateAtmosphericComponent(
                sub,
                proportionInHydrosphere,
                vaporProportion,
                ref adjustedAtmosphericPressure);
        }

        if (isWater && _planetParams?.EarthlikeAtmosphere != true)
        {
            CheckCO2Reduction(vaporPressure);
        }

        return adjustedAtmosphericPressure;
    }

    private double CalculatePhases(int counter, double adjustedAtmosphericPressure)
    {
        var surfaceTemp = GetAverageSurfaceTemperature();

        // Despite the theoretical possibility of an atmosphere cold enough to precipitate some
        // of the noble gases, or hydrogen, they are ignored and presumed to exist always as
        // trace atmospheric gases, never surface liquids or ices, or in large enough quantities
        // to form precipitation.

        var methane = Substances.All.Methane.GetHomogeneousReference();
        adjustedAtmosphericPressure = CalculateGasPhaseMix(methane, surfaceTemp, adjustedAtmosphericPressure);

        var carbonMonoxide = Substances.All.CarbonMonoxide.GetHomogeneousReference();
        adjustedAtmosphericPressure = CalculateGasPhaseMix(carbonMonoxide, surfaceTemp, adjustedAtmosphericPressure);

        var carbonDioxide = Substances.All.CarbonDioxide.GetHomogeneousReference();
        adjustedAtmosphericPressure = CalculateGasPhaseMix(carbonDioxide, surfaceTemp, adjustedAtmosphericPressure);

        var nitrogen = Substances.All.Nitrogen.GetHomogeneousReference();
        adjustedAtmosphericPressure = CalculateGasPhaseMix(nitrogen, surfaceTemp, adjustedAtmosphericPressure);

        var oxygen = Substances.All.Oxygen.GetHomogeneousReference();
        adjustedAtmosphericPressure = CalculateGasPhaseMix(oxygen, surfaceTemp, adjustedAtmosphericPressure);

        // No need to check for ozone, since it is only added to atmospheres on planets with
        // liquid surface water, which means temperatures too high for liquid or solid ozone.

        var sulfurDioxide = Substances.All.SulphurDioxide.GetHomogeneousReference();
        adjustedAtmosphericPressure = CalculateGasPhaseMix(sulfurDioxide, surfaceTemp, adjustedAtmosphericPressure);

        // Water is handled differently, since the planet may already have surface water.
        if (counter > 0) // Not performed on first pass, since it was already done.
        {
            var water = Substances.All.Water.GetHomogeneousReference();
            var seawater = Substances.All.Seawater.GetHomogeneousReference();
            if (Hydrosphere.Contains(water)
                || Hydrosphere.Contains(seawater)
                || Atmosphere.Material.Contains(water))
            {
                adjustedAtmosphericPressure = CalculateGasPhaseMix(water, surfaceTemp, adjustedAtmosphericPressure);
            }
        }

        // Ices and clouds significantly impact albedo.
        var pressure = adjustedAtmosphericPressure;
        var iceAmount = (double)Math.Min(1,
            Hydrosphere.GetSurface()?.GetOverallValue(x => (double)x.SeparateByPhase(surfaceTemp, pressure, PhaseType.Solid).First().proportion) ?? 0);
        var cloudCover = Atmosphere.AtmosphericPressure
            * (double)Atmosphere.Material.GetOverallValue(x => (double)x.SeparateByPhase(surfaceTemp, pressure, PhaseType.Solid | PhaseType.Liquid).First().proportion) / 100;
        var reflectiveSurface = Math.Max(iceAmount, cloudCover);
        if (_planetParams.HasValue && _planetParams.Value.Albedo.HasValue)
        {
            _surfaceAlbedo = ((Albedo - (0.9 * reflectiveSurface)) / (1 - reflectiveSurface)).Clamp(0, 1);
        }
        else
        {
            Albedo = ((_surfaceAlbedo * (1 - reflectiveSurface)) + (0.9 * reflectiveSurface)).Clamp(0, 1);
            Atmosphere.ResetTemperatureDependentProperties(this);

            // An albedo change might significantly alter surface temperature, which may require a
            // re-calculation (but not too many). 5K is used as the threshold for re-calculation,
            // which may lead to some inaccuracies, but should avoid over-repetition for small changes.
            if (counter < 10 && Math.Abs(surfaceTemp - GetAverageSurfaceTemperature()) > 5)
            {
                adjustedAtmosphericPressure = CalculatePhases(counter + 1, adjustedAtmosphericPressure);
            }
        }

        return adjustedAtmosphericPressure;
    }

    private void CheckCO2Reduction(double vaporPressure)
    {
        // At least 1% humidity leads to a reduction of CO2 to a trace gas, by a presumed
        // carbon-silicate cycle.

        var water = Substances.All.Water.GetHomogeneousReference();
        var air = Atmosphere.Material.GetCore();
        if ((double)(air?.GetProportion(water) ?? 0) * Atmosphere.AtmosphericPressure >= 0.01 * vaporPressure)
        {
            var carbonDioxide = Substances.All.CarbonDioxide.GetHomogeneousReference();
            var co2 = air?.GetProportion(carbonDioxide) ?? 0;
            if (co2 >= 1e-3m) // reduce CO2 if not already trace
            {
                co2 = Randomizer.Instance.NextDecimal(15e-6m, 0.001m);

                // Replace most of the CO2 with inert gases.
                var nitrogen = Substances.All.Nitrogen.GetHomogeneousReference();
                var n2 = Atmosphere.Material.GetProportion(nitrogen) + Atmosphere.Material.GetProportion(carbonDioxide) - co2;
                Atmosphere.Material.Add(carbonDioxide, co2);

                // Some portion of the N2 may be Ar instead.
                var argon = Substances.All.Argon.GetHomogeneousReference();
                var ar = Math.Max(Atmosphere.Material.GetProportion(argon), n2 * Randomizer.Instance.NextDecimal(-0.02m, 0.04m));
                Atmosphere.Material.Add(argon, ar);
                n2 -= ar;

                // An even smaller fraction may be Kr.
                var krypton = Substances.All.Krypton.GetHomogeneousReference();
                var kr = Math.Max(Atmosphere.Material.GetProportion(krypton), n2 * Randomizer.Instance.NextDecimal(-25e-5m, 0.0005m));
                Atmosphere.Material.Add(krypton, kr);
                n2 -= kr;

                // An even smaller fraction may be Xe or Ne.
                var xenon = Substances.All.Xenon.GetHomogeneousReference();
                var xe = Math.Max(Atmosphere.Material.GetProportion(xenon), n2 * Randomizer.Instance.NextDecimal(-18e-6m, 35e-6m));
                Atmosphere.Material.Add(xenon, xe);
                n2 -= xe;

                var neon = Substances.All.Neon.GetHomogeneousReference();
                var ne = Math.Max(Atmosphere.Material.GetProportion(neon), n2 * Randomizer.Instance.NextDecimal(-18e-6m, 35e-6m));
                Atmosphere.Material.Add(neon, ne);
                n2 -= ne;

                Atmosphere.Material.Add(nitrogen, n2);

                Atmosphere.ResetGreenhouseFactor();
                ResetCachedTemperatures();
            }
        }
    }

    private void CondenseAtmosphericComponent(
        IHomogeneous substance,
        double surfaceTemp,
        decimal proportionInHydrosphere,
        decimal vaporProportion,
        double vaporPressure,
        ref double adjustedAtmosphericPressure)
    {
        var water = Substances.All.Water.GetHomogeneousReference();

        // Fully precipitate out of the atmosphere when below the freezing point.
        if (!substance.MeltingPoint.HasValue || surfaceTemp < substance.MeltingPoint.Value)
        {
            vaporProportion = 0;

            Atmosphere.Material.Remove(substance);

            if (Atmosphere.Material.Constituents.Count == 0)
            {
                adjustedAtmosphericPressure = 0;
            }

            if (substance.Equals(water))
            {
                Atmosphere.ResetWater();
            }
        }
        else
        {
            // Adjust vapor present in the atmosphere based on the vapor pressure.
            var pressureRatio = (vaporPressure / Atmosphere.AtmosphericPressure).Clamp(0, 1);
            if (substance.Equals(water) && _planetParams?.WaterVaporRatio.HasValue == true)
            {
                vaporProportion = _planetParams!.Value.WaterVaporRatio!.Value;
            }
            else
            {
                // This would represent 100% humidity. Since this is the case, in principle, only at the
                // surface of bodies of liquid, and should decrease exponentially with altitude, an
                // approximation of 25% average humidity overall is used.
                vaporProportion = (proportionInHydrosphere + vaporProportion) * (decimal)pressureRatio;
                vaporProportion *= 0.25m;
            }
            if (vaporProportion > 0)
            {
                var previousGasFraction = 0m;
                var gasFraction = vaporProportion;
                Atmosphere.Material.Add(substance, vaporProportion);

                if (substance.Equals(water))
                {
                    Atmosphere.ResetWater();

                    // For water, also add a corresponding amount of oxygen, if it's not already present.
                    if (_planetParams?.EarthlikeAtmosphere != true && PlanetType != PlanetType.Carbon)
                    {
                        var oxygen = Substances.All.Oxygen.GetHomogeneousReference();
                        var o2 = Atmosphere.Material.GetProportion(oxygen);
                        previousGasFraction += o2;
                        o2 = Math.Max(o2, vaporProportion * 0.0001m);
                        gasFraction += o2;
                        Atmosphere.Material.Add(oxygen, o2);
                    }
                }

                adjustedAtmosphericPressure += adjustedAtmosphericPressure * (double)(gasFraction - previousGasFraction);

                // At least some precipitation will occur. Ensure a troposphere.
                Atmosphere.DifferentiateTroposphere();
            }
        }

        var hydro = proportionInHydrosphere;
        var hydrosphereAtmosphereRatio = Atmosphere.Material.IsEmpty ? 0 : GetHydrosphereAtmosphereRatio();
        hydro = Math.Max(hydro, hydrosphereAtmosphereRatio <= 0 ? vaporProportion : vaporProportion / hydrosphereAtmosphereRatio);
        if (hydro > proportionInHydrosphere)
        {
            Hydrosphere.GetSurface().Add(substance, hydro);
        }
    }

    private List<Planetoid> Configure(
        CosmicLocation? parent,
        List<Star> stars,
        Star? star,
        Vector3<HugeNumber> position,
        bool satellite,
        OrbitalParameters? orbit)
    {
        Seed = SeedGenerator.GetNewSeed();
        var randomizer = new Randomizer(Seed);
        var seedArray = new SeedArray();
        for (var i = 0; i < 5; i++)
        {
            seedArray[i] = randomizer.NextInclusive();
        }
        SeedArray = seedArray;

        IsInhospitable = stars.Any(x => !x.IsHospitable);

        double eccentricity;
        if (_planetParams?.Eccentricity.HasValue == true)
        {
            eccentricity = _planetParams!.Value.Eccentricity!.Value;
        }
        else if (orbit.HasValue)
        {
            eccentricity = orbit.Value.Circular ? 0 : orbit.Value.Eccentricity;
        }
        else if (PlanetType == PlanetType.Comet)
        {
            eccentricity = Randomizer.Instance.NextDouble();
        }
        else if (IsAsteroid)
        {
            eccentricity = Randomizer.Instance.NextDouble(0.4);
        }
        else
        {
            eccentricity = Randomizer.Instance.PositiveNormalDistributionSample(0, 0.05);
        }

        HugeNumber semiMajorAxis;
        if (_planetParams?.RevolutionPeriod.HasValue == true
            && (orbit.HasValue || star is not null))
        {
            var orbitedMass = orbit.HasValue ? orbit.Value.OrbitedMass : star?.Mass;
            semiMajorAxis = Space.Orbit.GetSemiMajorAxisForPeriod(Mass, orbitedMass!.Value, _planetParams!.Value.RevolutionPeriod!.Value);
            position = position == Vector3<HugeNumber>.Zero
                ? Vector3<HugeNumber>.UnitX * semiMajorAxis
                : position.Normalize() * semiMajorAxis;
        }
        else if (orbit.HasValue)
        {
            var periapsis = orbit.Value.Circular ? position.Distance(orbit.Value.OrbitedPosition) : orbit.Value.Periapsis;
            semiMajorAxis = eccentricity == 1
                ? periapsis
                : periapsis * (1 + eccentricity) / (1 - (eccentricity * eccentricity));
            position = position == Vector3<HugeNumber>.Zero
                ? Vector3<HugeNumber>.UnitX * periapsis
                : position.Normalize() * periapsis;
        }
        else
        {
            var distance = star is null
                ? position.Length()
                : star.Position.Distance(position);
            semiMajorAxis = distance * ((1 + eccentricity) / (1 - eccentricity));
        }

        AxialPrecession = Randomizer.Instance.NextDouble(DoubleConstants.TwoPi);

        GenerateMaterial(
            parent?.Material.Temperature ?? UniverseAmbientTemperature,
            position,
            semiMajorAxis);

        if (_planetParams.HasValue && _planetParams.Value.Albedo.HasValue)
        {
            _surfaceAlbedo = _planetParams.Value.Albedo.Value;
        }
        else if (PlanetType == PlanetType.Comet)
        {
            _surfaceAlbedo = Randomizer.Instance.NextDouble(0.025, 0.055);
        }
        else if (PlanetType == PlanetType.AsteroidC)
        {
            _surfaceAlbedo = Randomizer.Instance.NextDouble(0.03, 0.1);
        }
        else if (PlanetType == PlanetType.AsteroidM)
        {
            _surfaceAlbedo = Randomizer.Instance.NextDouble(0.1, 0.2);
        }
        else if (PlanetType == PlanetType.AsteroidS)
        {
            _surfaceAlbedo = Randomizer.Instance.NextDouble(0.1, 0.22);
        }
        else if (PlanetType.Giant.HasFlag(PlanetType))
        {
            _surfaceAlbedo = Randomizer.Instance.NextDouble(0.275, 0.35);
        }
        else
        {
            _surfaceAlbedo = Randomizer.Instance.NextDouble(0.1, 0.6);
        }
        Albedo = _surfaceAlbedo;

        if (_planetParams?.RotationalPeriod.HasValue == true)
        {
            RotationalPeriod = HugeNumber.Max(0, _planetParams!.Value.RotationalPeriod!.Value);
        }
        else
        {
            // Check for tidal locking.
            var rotationalPeriodSet = false;
            if (orbit.HasValue)
            {
                // Invent an orbit age. Precision isn't important here, and some inaccuracy and
                // inconsistency between satellites is desirable. The age of the Solar system is used
                // as an arbitrary norm.
                var years = Randomizer.Instance.LogisticDistributionSample(0, 1) * new HugeNumber(4.6, 9);

                var rigidity = PlanetType == PlanetType.Comet ? new HugeNumber(4, 9) : new HugeNumber(3, 10);
                if (HugeNumber.Pow(years / new HugeNumber(6, 11)
                    * Mass
                    * orbit.Value.OrbitedMass.Square()
                    / (Shape.ContainingRadius * rigidity)
                    , HugeNumber.One / new HugeNumber(6)) >= semiMajorAxis)
                {
                    RotationalPeriod = HugeNumberConstants.TwoPi * HugeNumber.Sqrt(semiMajorAxis.Cube() / (HugeNumberConstants.G * (orbit.Value.OrbitedMass + Mass)));
                    rotationalPeriodSet = true;
                }
            }
            if (!rotationalPeriodSet)
            {
                var rotationalPeriodLimit = IsTerrestrial ? new HugeNumber(6500000) : new HugeNumber(100000);
                if (Randomizer.Instance.NextDouble() <= 0.05) // low chance of an extreme period
                {
                    RotationalPeriod = Randomizer.Instance.Next(
                        rotationalPeriodLimit,
                        IsTerrestrial ? new HugeNumber(22000000) : new HugeNumber(1100000));
                }
                else
                {
                    RotationalPeriod = Randomizer.Instance.Next(
                        IsTerrestrial ? new HugeNumber(40000) : new HugeNumber(8000),
                        rotationalPeriodLimit);
                }
            }
        }

        GenerateOrbit(
            orbit,
            star,
            eccentricity,
            semiMajorAxis);

        if (_planetParams?.AxialTilt.HasValue == true)
        {
            var axialTilt = _planetParams!.Value.AxialTilt!.Value;
            if (Orbit.HasValue)
            {
                axialTilt += Orbit.Value.Inclination;
            }
            while (axialTilt > Math.PI)
            {
                axialTilt -= Math.PI;
            }
            while (axialTilt < 0)
            {
                axialTilt += Math.PI;
            }
            AngleOfRotation = axialTilt;
        }
        else if (Randomizer.Instance.NextDouble() <= 0.2) // low chance of an extreme tilt
        {
            AngleOfRotation = Randomizer.Instance.NextDouble(DoubleConstants.QuarterPi, Math.PI);
        }
        else
        {
            AngleOfRotation = Randomizer.Instance.NextDouble(DoubleConstants.QuarterPi);
        }
        SetAxis();

        SetTemperatures(stars);

        double surfaceTemp;
        if (_planetParams?.SurfaceTemperature.HasValue == true)
        {
            surfaceTemp = _planetParams!.Value.SurfaceTemperature!.Value;
        }
        else if (_habitabilityRequirements?.MinimumTemperature.HasValue == true)
        {
            surfaceTemp = _habitabilityRequirements!.Value.MaximumTemperature.HasValue
                ? (_habitabilityRequirements!.Value.MinimumTemperature!.Value
                    + _habitabilityRequirements!.Value.MaximumTemperature.Value)
                    / 2
                : _habitabilityRequirements!.Value.MinimumTemperature!.Value;
        }
        else
        {
            surfaceTemp = BlackbodyTemperature;
        }

        GenerateHydrosphere(surfaceTemp);

        HasMagnetosphere = _planetParams?.HasMagnetosphere.HasValue == true
            ? _planetParams!.Value.HasMagnetosphere!.Value
            : Randomizer.Instance.Next() <= Mass * new HugeNumber(2.88, -19) / RotationalPeriod * (PlanetType switch
            {
                PlanetType.Iron => new HugeNumber(5),
                PlanetType.Ocean => HugeNumberConstants.Half,
                _ => HugeNumber.One,
            });

        if (star is not null
            && (_planetParams?.SurfaceTemperature.HasValue == true
            || _habitabilityRequirements?.MinimumTemperature.HasValue == true
            || _habitabilityRequirements?.MaximumTemperature.HasValue == true))
        {
            CorrectSurfaceTemperature(stars, star, surfaceTemp);
        }
        else
        {
            GenerateAtmosphere();
        }

        GenerateResources();

        GenerateRings();

        return satellite
            ? new List<Planetoid>()
            : GenerateSatellites(parent, stars);
    }

    private void CorrectSurfaceTemperature(
        List<Star> stars,
        Star star,
        double surfaceTemp)
    {
        // Convert the target average surface temperature to an estimated target equatorial
        // surface temperature, for which orbit/luminosity requirements can be calculated.
        var targetEquatorialTemp = surfaceTemp * 1.06;
        // Use the typical average elevation to determine average surface
        // temperature, since the average temperature at sea level is not the same
        // as the average overall surface temperature.
        var avgElevation = MaxElevation * 0.04;
        var totalTargetEffectiveTemp = targetEquatorialTemp + (avgElevation * LapseRateDry);

        var greenhouseEffect = 30.0; // naïve initial guess, corrected if possible with param values
        if (_planetParams?.AtmosphericPressure.HasValue == true
            && (_planetParams?.WaterVaporRatio.HasValue == true
            || _planetParams?.WaterRatio.HasValue == true))
        {
            var pressure = _planetParams!.Value.AtmosphericPressure!.Value;

            var vaporRatio = _planetParams?.WaterVaporRatio.HasValue == true
                ? (double)_planetParams!.Value.WaterVaporRatio!.Value
                : (Substances.All.Water.GetVaporPressure(totalTargetEffectiveTemp) ?? 0) / pressure * 0.25;
            greenhouseEffect = GetGreenhouseEffect(
                GetInsolationFactor(Atmosphere.GetAtmosphericMass(this, pressure), 0), // scale height will be ignored since this isn't a polar calculation
                Atmosphere.GetGreenhouseFactor(Substances.All.Water.GreenhousePotential * vaporRatio, pressure));
        }
        var targetEffectiveTemp = totalTargetEffectiveTemp - greenhouseEffect;

        var currentTargetTemp = targetEffectiveTemp;

        // Due to atmospheric effects, the target is likely to be missed considerably on the
        // first attempt, since the calculations for orbit/luminosity will not be able to
        // account for greenhouse warming. By determining the degree of over/undershoot, the
        // target can be adjusted. This is repeated until the real target is approached to
        // within an acceptable tolerance, but not to excess.
        var count = 0;
        double prevDelta;
        var delta = 0.0;
        var originalHydrosphere = Hydrosphere.GetClone();
        var newAtmosphere = true;
        do
        {
            prevDelta = delta;

            // Orbital distance averaged over time (mean anomaly) = semi-major axis * (1 + eccentricity^2 / 2).
            // This allows calculation of the correct distance/orbit for an average
            // orbital temperature (rather than the temperature at the current position).
            if (_planetParams?.RevolutionPeriod.HasValue == true)
            {
                // Do not attempt a correction on the first pass; the albedo delta due to
                // atmospheric effects will not yet have a meaningful value.
                if (Albedo != _surfaceAlbedo)
                {
                    var albedoDelta = Albedo - _surfaceAlbedo;
                    _surfaceAlbedo = GetSurfaceAlbedoForTemperature(star, currentTargetTemp - Temperature);
                    Albedo = _surfaceAlbedo + albedoDelta;
                }
            }
            else
            {
                var semiMajorAxis = GetDistanceForTemperature(star, currentTargetTemp - Temperature) / (1 + (Orbit!.Value.Eccentricity * Orbit.Value.Eccentricity / 2));
                GenerateOrbit(star, Orbit.Value.Eccentricity, semiMajorAxis, Orbit.Value.TrueAnomaly);
            }
            ResetAllCachedTemperatures(stars);

            // Reset hydrosphere to negate effects of runaway evaporation or freezing.
            Hydrosphere = originalHydrosphere;

            if (newAtmosphere)
            {
                GenerateAtmosphere();
                newAtmosphere = false;
            }

            if (_planetParams?.SurfaceTemperature.HasValue == true)
            {
                delta = targetEquatorialTemp - GetTemperatureAtElevation(GetAverageSurfaceTemperature(), avgElevation);
            }
            else if (_habitabilityRequirements.HasValue)
            {
                var tooCold = false;
                if (_habitabilityRequirements.Value.MinimumTemperature.HasValue)
                {
                    var coolestEquatorialTemp = GetMinEquatorTemperature();
                    if (coolestEquatorialTemp < _habitabilityRequirements.Value.MinimumTemperature)
                    {
                        delta = _habitabilityRequirements.Value.MaximumTemperature.HasValue
                            ? _habitabilityRequirements.Value.MaximumTemperature.Value - coolestEquatorialTemp
                            : _habitabilityRequirements.Value.MinimumTemperature.Value - coolestEquatorialTemp;
                        tooCold = true;
                    }
                }
                if (!tooCold && _habitabilityRequirements.Value.MaximumTemperature.HasValue)
                {
                    var warmestPolarTemp = GetMaxPolarTemperature();
                    if (warmestPolarTemp > _habitabilityRequirements.Value.MaximumTemperature)
                    {
                        delta = _habitabilityRequirements!.Value.MaximumTemperature.Value - warmestPolarTemp;
                    }
                }
            }

            // Avoid oscillation by reducing deltas which bounce around zero.
            var deltaAdjustment = prevDelta != 0 && Math.Sign(delta) != Math.Sign(prevDelta)
                ? delta / 2
                : 0;
            // If the corrections are resulting in a runaway drift in the wrong direction,
            // reset by deleting the atmosphere and targeting the original temp; do not
            // reset the count, to prevent this from repeating indefinitely.
            if (prevDelta != 0
                && (delta >= 0) == (prevDelta >= 0)
                && Math.Abs(delta) > Math.Abs(prevDelta))
            {
                newAtmosphere = true;
                ResetCachedTemperatures();
                currentTargetTemp = targetEffectiveTemp;
            }
            else
            {
                currentTargetTemp = Math.Max(0, currentTargetTemp + (delta - deltaAdjustment));
            }

            count++;
        } while (count < 10 && Math.Abs(delta) > 0.5);
    }

    private void EvaporateAtmosphericComponent(
        IHomogeneous substance,
        decimal hydrosphereProportion,
        decimal vaporProportion,
        ref double adjustedAtmosphericPressure)
    {
        if (hydrosphereProportion <= 0)
        {
            return;
        }

        var water = Substances.All.Water.GetHomogeneousReference();
        if (substance.Equals(water))
        {
            Hydrosphere = Hydrosphere.GetHomogenized();
            Atmosphere.ResetWater();
        }

        var gasProportion = Atmosphere.Material.Mass == HugeNumber.Zero
            ? 0
            : hydrosphereProportion * GetHydrosphereAtmosphereRatio();
        var previousGasProportion = vaporProportion;

        Hydrosphere.GetSurface().Remove(substance);
        if (Hydrosphere.IsEmpty)
        {
            NormalizedSeaLevel = -1.1;
        }

        if (substance.Equals(water))
        {
            var seawater = Substances.All.Seawater.GetHomogeneousReference();
            Hydrosphere.GetSurface().Remove(seawater);
            if (Hydrosphere.IsEmpty)
            {
                NormalizedSeaLevel = -1.1;
            }

            // It is presumed that photodissociation will eventually reduce the amount of water
            // vapor to a trace gas (the H2 will be lost due to atmospheric escape, and the
            // oxygen will be lost to surface oxidation).
            var waterVapor = Math.Min(gasProportion, Randomizer.Instance.NextDecimal(0.001m));
            gasProportion = waterVapor;

            var oxygen = Substances.All.Oxygen.GetHomogeneousReference();
            previousGasProportion += Atmosphere.Material.GetProportion(oxygen);
            var o2 = gasProportion * 0.0001m;
            gasProportion += o2;

            Atmosphere.Material.Add(substance, waterVapor);
            if (PlanetType != PlanetType.Carbon)
            {
                Atmosphere.Material.Add(oxygen, o2);
            }
        }
        else
        {
            Atmosphere.Material.Add(substance, gasProportion);
        }

        adjustedAtmosphericPressure += adjustedAtmosphericPressure * (double)(gasProportion - previousGasProportion);
    }

    private void FractionHydrosphere(double temperature)
    {
        if (Hydrosphere.IsEmpty)
        {
            return;
        }

        var seawater = Substances.All.Seawater.GetHomogeneousReference();
        var water = Substances.All.Water.GetHomogeneousReference();

        var seawaterProportion = Hydrosphere.GetProportion(seawater);
        var waterProportion = 1 - seawaterProportion;

        var depth = SeaLevel + (MaxElevation / 2);
        if (depth > 0)
        {
            var stateTop = Substances.All.Seawater.MeltingPoint <= temperature
                ? PhaseType.Liquid
                : PhaseType.Solid;

            double tempBottom;
            if (depth > 1000)
            {
                tempBottom = 277;
            }
            else if (depth < 200)
            {
                tempBottom = temperature;
            }
            else
            {
                tempBottom = temperature.Lerp(277, (depth - 200) / 800);
            }

            var stateBottom = Substances.All.Seawater.MeltingPoint <= tempBottom
                ? PhaseType.Liquid
                : PhaseType.Solid;

            // subsurface ocean indicated
            if (stateTop != stateBottom)
            {
                var topProportion = 1000 / depth;
                var bottomProportion = 1 - topProportion;
                var bottomOuterRadius = Hydrosphere.Shape.ContainingRadius * bottomProportion;
                Hydrosphere = new Composite<HugeNumber>(
                    new IMaterial<HugeNumber>[]
                    {
                        new Material<HugeNumber>(
                            new HollowSphere<HugeNumber>(
                                Material.Shape.ContainingRadius,
                                bottomOuterRadius,
                                Material.Shape.Position),
                            Hydrosphere.Mass * bottomProportion,
                            Hydrosphere.Density,
                            277,
                            (seawater, seawaterProportion),
                            (water, waterProportion)),
                        new Material<HugeNumber>(
                            new HollowSphere<HugeNumber>(
                                bottomOuterRadius,
                                Hydrosphere.Shape.ContainingRadius,
                                Material.Shape.Position),
                            Hydrosphere.Mass * topProportion,
                            Hydrosphere.Density,
                            (277 + temperature) / 2,
                            (seawater, seawaterProportion),
                            (water, waterProportion)),
                    },
                    Hydrosphere.Shape,
                    Hydrosphere.Mass,
                    Hydrosphere.Density);
                return;
            }
        }

        var avgDepth = (double)(Hydrosphere.Shape.ContainingRadius - Material.Shape.ContainingRadius) / 2;
        double avgTemp;
        if (avgDepth > 1000)
        {
            avgTemp = 277;
        }
        else if (avgDepth < 200)
        {
            avgTemp = temperature;
        }
        else
        {
            avgTemp = temperature.Lerp(277, (avgDepth - 200) / 800);
        }

        Hydrosphere = new Material<HugeNumber>(
            Hydrosphere.Shape,
            Hydrosphere.Mass,
            Hydrosphere.Density,
            avgTemp,
            (seawater, seawaterProportion),
            (water, waterProportion));
    }

    private void GenerateAtmosphere()
    {
        if (PlanetType == PlanetType.Comet
            || IsAsteroid)
        {
            GenerateAtmosphere_SmallBody();
            return;
        }

        if (IsGiant)
        {
            GenerateAtmosphere_Giant();
            return;
        }

        if (!IsTerrestrial)
        {
            GenerateAtmosphere_Dwarf();
            return;
        }

        if (AverageBlackbodyTemperature >= GetTempForThinAtmosphere())
        {
            GenerateAtmosphereTrace();
        }
        else
        {
            GenerateAtmosphereThick();
        }

        var adjustedAtmosphericPressure = Atmosphere.AtmosphericPressure;

        var water = Substances.All.Water.GetHomogeneousReference();
        var seawater = Substances.All.Seawater.GetHomogeneousReference();
        // Water may be removed, or if not may remove CO2 from the atmosphere, depending on
        // planetary conditions.
        if (Hydrosphere.Contains(water)
            || Hydrosphere.Contains(seawater)
            || Atmosphere.Material.Contains(water))
        {
            // First calculate water phases at effective temp, to establish a baseline
            // for the presence of water and its effect on CO2.
            // If a desired temp has been established, use that instead.
            double surfaceTemp;
            if (_planetParams?.SurfaceTemperature.HasValue == true)
            {
                surfaceTemp = _planetParams!.Value.SurfaceTemperature!.Value;
            }
            else if (_habitabilityRequirements?.MinimumTemperature.HasValue == true)
            {
                surfaceTemp = _habitabilityRequirements!.Value.MaximumTemperature.HasValue
                    ? (_habitabilityRequirements!.Value.MinimumTemperature!.Value
                        + _habitabilityRequirements!.Value.MaximumTemperature!.Value)
                        / 2
                    : _habitabilityRequirements!.Value.MinimumTemperature!.Value;
            }
            else
            {
                surfaceTemp = AverageBlackbodyTemperature;
            }
            adjustedAtmosphericPressure = CalculateGasPhaseMix(
                water,
                surfaceTemp,
                adjustedAtmosphericPressure);

            // Recalculate temperatures based on the new atmosphere.
            ResetCachedTemperatures();

            FractionHydrosphere(GetAverageSurfaceTemperature());

            // Recalculate the phases of water based on the new temperature.
            adjustedAtmosphericPressure = CalculateGasPhaseMix(
                water,
                GetAverageSurfaceTemperature(),
                adjustedAtmosphericPressure);

            // If life alters the greenhouse potential, temperature and water phase must be
            // recalculated once again.
            if (GenerateLife())
            {
                adjustedAtmosphericPressure = CalculateGasPhaseMix(
                    water,
                    GetAverageSurfaceTemperature(),
                    adjustedAtmosphericPressure);
                ResetCachedTemperatures();
                FractionHydrosphere(GetAverageSurfaceTemperature());
            }
        }
        else
        {
            // Recalculate temperature based on the new atmosphere.
            ResetCachedTemperatures();
        }

        var modified = false;
        foreach (var requirement in Atmosphere.ConvertRequirementsForPressure(_habitabilityRequirements?.AtmosphericRequirements)
            .Concat(Atmosphere.ConvertRequirementsForPressure(_planetParams?.AtmosphericRequirements)))
        {
            var proportion = Atmosphere.Material.GetProportion(requirement.Substance);
            if (proportion < requirement.MinimumProportion
                || (requirement.MaximumProportion.HasValue && proportion > requirement.MaximumProportion.Value))
            {
                Atmosphere.Material.Add(
                    requirement.Substance,
                    requirement.MaximumProportion.HasValue
                        ? (requirement.MinimumProportion + requirement.MaximumProportion.Value) / 2
                        : requirement.MinimumProportion);
                if (requirement.Substance.Equals(water))
                {
                    Atmosphere.ResetWater();
                }
                modified = true;
            }
        }
        if (modified)
        {
            Atmosphere.ResetGreenhouseFactor();
            ResetCachedTemperatures();
        }

        adjustedAtmosphericPressure = CalculatePhases(0, adjustedAtmosphericPressure);
        FractionHydrosphere(GetAverageSurfaceTemperature());

        if (_planetParams?.AtmosphericPressure.HasValue != true && _habitabilityRequirements is null)
        {
            SetAtmosphericPressure(Math.Max(0, adjustedAtmosphericPressure));
            Atmosphere.ResetPressureDependentProperties(this);
        }

        // If the adjustments have led to the loss of liquid water, then there is no life after
        // all (this may be interpreted as a world which once supported life, but became
        // inhospitable due to the environmental changes that life produced).
        if (!HasLiquidWater())
        {
            HasBiosphere = false;
        }
    }

    private void GenerateAtmosphere_Dwarf()
    {
        // Atmosphere is based solely on the volatile ices present.
        var crust = Material.GetSurface();

        var water = crust.GetProportion(Substances.All.Water.GetHomogeneousReference());
        var anyIces = water > 0;

        var n2 = crust.GetProportion(Substances.All.Nitrogen.GetHomogeneousReference());
        anyIces &= n2 > 0;

        var ch4 = crust.GetProportion(Substances.All.Methane.GetHomogeneousReference());
        anyIces &= ch4 > 0;

        var co = crust.GetProportion(Substances.All.CarbonMonoxide.GetHomogeneousReference());
        anyIces &= co > 0;

        var co2 = crust.GetProportion(Substances.All.CarbonDioxide.GetHomogeneousReference());
        anyIces &= co2 > 0;

        var nh3 = crust.GetProportion(Substances.All.Ammonia.GetHomogeneousReference());
        anyIces &= nh3 > 0;

        if (!anyIces)
        {
            return;
        }

        var components = new List<(ISubstanceReference, decimal)>();
        if (water > 0)
        {
            components.Add((Substances.All.Water.GetHomogeneousReference(), water));
        }
        if (n2 > 0)
        {
            components.Add((Substances.All.Nitrogen.GetHomogeneousReference(), n2));
        }
        if (ch4 > 0)
        {
            components.Add((Substances.All.Methane.GetHomogeneousReference(), ch4));
        }
        if (co > 0)
        {
            components.Add((Substances.All.CarbonMonoxide.GetHomogeneousReference(), co));
        }
        if (co2 > 0)
        {
            components.Add((Substances.All.CarbonDioxide.GetHomogeneousReference(), co2));
        }
        if (nh3 > 0)
        {
            components.Add((Substances.All.Ammonia.GetHomogeneousReference(), nh3));
        }
        Atmosphere = new Atmosphere(this, Randomizer.Instance.NextDouble(2.5), components.ToArray());

        var ice = Atmosphere.Material.GetOverallValue(x =>
            (double)x.SeparateByPhase(
                Material.Temperature ?? 0,
                Atmosphere.AtmosphericPressure,
                PhaseType.Solid)
            .First().proportion);
        if (_planetParams.HasValue && _planetParams.Value.Albedo.HasValue)
        {
            _surfaceAlbedo = ((Albedo - (0.9 * ice)) / (1 - ice)).Clamp(0, 1);
        }
        else
        {
            Albedo = ((_surfaceAlbedo * (1 - ice)) + (0.9 * ice)).Clamp(0, 1);
        }
    }

    private void GenerateAtmosphere_Giant()
    {
        var trace = Randomizer.Instance.NextDecimal(0.025m);

        var h = Randomizer.Instance.NextDecimal(0.75m, 0.97m);
        var he = 1 - h - trace;

        var ch4 = Randomizer.Instance.NextDecimal() * trace;
        trace -= ch4;

        // 50% chance not to have each of these components
        var c2h6 = Math.Max(0, Randomizer.Instance.NextDecimal(-0.5m, 0.5m));
        var traceTotal = c2h6;
        var nh3 = Math.Max(0, Randomizer.Instance.NextDecimal(-0.5m, 0.5m));
        traceTotal += nh3;
        var waterVapor = Math.Max(0, Randomizer.Instance.NextDecimal(-0.5m, 0.5m));
        traceTotal += waterVapor;

        var nh4sh = Randomizer.Instance.NextDecimal();
        traceTotal += nh4sh;

        var ratio = trace / traceTotal;
        c2h6 *= ratio;
        nh3 *= ratio;
        waterVapor *= ratio;
        nh4sh *= ratio;

        var components = new List<(ISubstanceReference, decimal)>()
            {
                (Substances.All.Hydrogen.GetHomogeneousReference(), h),
                (Substances.All.Helium.GetHomogeneousReference(), he),
                (Substances.All.Methane.GetHomogeneousReference(), ch4),
            };
        if (c2h6 > 0)
        {
            components.Add((Substances.All.Ethane.GetHomogeneousReference(), c2h6));
        }
        if (nh3 > 0)
        {
            components.Add((Substances.All.Ammonia.GetHomogeneousReference(), nh3));
        }
        if (waterVapor > 0)
        {
            components.Add((Substances.All.Water.GetHomogeneousReference(), waterVapor));
        }
        if (nh4sh > 0)
        {
            components.Add((Substances.All.AmmoniumHydrosulfide.GetHomogeneousReference(), nh4sh));
        }
        Atmosphere = new Atmosphere(this, 1000, components.ToArray());
    }

    private void GenerateAtmosphere_SmallBody()
    {
        var dust = 1.0m;

        var water = Randomizer.Instance.NextDecimal(0.75m, 0.9m);
        dust -= water;

        var co = Randomizer.Instance.NextDecimal(0.05m, 0.15m);
        dust -= co;

        if (dust < 0)
        {
            water -= 0.1m;
            dust += 0.1m;
        }

        var co2 = Randomizer.Instance.NextDecimal(0.01m);
        dust -= co2;

        var nh3 = Randomizer.Instance.NextDecimal(0.01m);
        dust -= nh3;

        var ch4 = Randomizer.Instance.NextDecimal(0.01m);
        dust -= ch4;

        var h2s = Randomizer.Instance.NextDecimal(0.01m);
        dust -= h2s;

        var so2 = Randomizer.Instance.NextDecimal(0.001m);
        dust -= so2;

        Atmosphere = new Atmosphere(
            this,
            1e-8,
            (Substances.All.Water.GetHomogeneousReference(), water),
            (Substances.All.CosmicDust.GetHomogeneousReference(), dust),
            (Substances.All.CarbonMonoxide.GetHomogeneousReference(), co),
            (Substances.All.CarbonDioxide.GetHomogeneousReference(), co2),
            (Substances.All.Ammonia.GetHomogeneousReference(), nh3),
            (Substances.All.Methane.GetHomogeneousReference(), ch4),
            (Substances.All.HydrogenSulfide.GetHomogeneousReference(), h2s),
            (Substances.All.SulphurDioxide.GetHomogeneousReference(), so2));
    }

    private void GenerateAtmosphereThick()
    {
        double pressure;
        if (_planetParams?.AtmosphericPressure.HasValue == true)
        {
            pressure = Math.Max(0, _planetParams!.Value.AtmosphericPressure!.Value);
        }
        else if (_planetParams?.EarthlikeAtmosphere == true)
        {
            pressure = PlanetParams.EarthAtmosphericPressure;
        }
        else if (_habitabilityRequirements?.MinimumPressure.HasValue == true
            || _habitabilityRequirements?.MaximumPressure.HasValue == true)
        {
            // If there is a minimum but no maximum, a half-Gaussian distribution with the minimum as both mean and the basis for the sigma is used.
            if (_habitabilityRequirements.HasValue
                && _habitabilityRequirements.Value.MinimumPressure.HasValue)
            {
                if (!_habitabilityRequirements.Value.MaximumPressure.HasValue)
                {
                    pressure = _habitabilityRequirements.Value.MinimumPressure.Value
                        + Math.Abs(Randomizer.Instance.NormalDistributionSample(0, _habitabilityRequirements.Value.MinimumPressure.Value / 3));
                }
                else
                {
                    pressure = Randomizer.Instance.NextDouble(_habitabilityRequirements.Value.MinimumPressure ?? 0, _habitabilityRequirements.Value.MaximumPressure.Value);
                }
            }
            else
            {
                pressure = 0;
            }
        }
        else
        {
            HugeNumber mass;
            // Low-gravity planets without magnetospheres are less likely to hold onto the bulk
            // of their atmospheres over long periods.
            if (Mass >= 1.5e24 || HasMagnetosphere)
            {
                mass = Mass / Randomizer.Instance.NormalDistributionSample(1158568, 38600, minimum: 579300, maximum: 1737900);
            }
            else
            {
                mass = Mass / Randomizer.Instance.NormalDistributionSample(7723785, 258000, minimum: 3862000, maximum: 11586000);
            }

            pressure = (double)(mass * SurfaceGravity / (1000 * HugeNumberConstants.FourPi * RadiusSquared));
        }
        if (pressure <= 0)
        {
            _atmosphere = null;
            return;
        }

        // For terrestrial (non-giant) planets, these gases remain at low concentrations due to
        // atmospheric escape.
        var h = _planetParams?.EarthlikeAtmosphere == true ? 3.8e-8m : Randomizer.Instance.NextDecimal(1e-8m, 2e-7m);
        var he = _planetParams?.EarthlikeAtmosphere == true ? 7.24e-6m : Randomizer.Instance.NextDecimal(2.6e-7m, 1e-5m);

        // 50% chance not to have these components at all.
        var ch4 = _planetParams?.EarthlikeAtmosphere == true ? 2.9e-6m : Math.Max(0, Randomizer.Instance.NextDecimal(-0.5m, 0.5m));
        var traceTotal = ch4;

        var co = _planetParams?.EarthlikeAtmosphere == true ? 2.5e-7m : Math.Max(0, Randomizer.Instance.NextDecimal(-0.5m, 0.5m));
        traceTotal += co;

        var so2 = _planetParams?.EarthlikeAtmosphere == true ? 1e-7m : Math.Max(0, Randomizer.Instance.NextDecimal(-0.5m, 0.5m));
        traceTotal += so2;

        decimal trace;
        if (_planetParams?.EarthlikeAtmosphere == true)
        {
            trace = traceTotal;
        }
        else if (traceTotal == 0)
        {
            trace = 0;
        }
        else
        {
            trace = Randomizer.Instance.NextDecimal(1e-6m, 2.5e-4m);
        }
        if (_planetParams?.EarthlikeAtmosphere != true)
        {
            var traceRatio = traceTotal == 0 ? 0 : trace / traceTotal;
            ch4 *= traceRatio;
            co *= traceRatio;
            so2 *= traceRatio;
        }

        // CO2 makes up the bulk of a thick atmosphere by default (although the presence of water
        // may change this later).
        var co2 = _planetParams?.EarthlikeAtmosphere == true ? 5.3e-4m : Randomizer.Instance.NextDecimal(0.97m, 0.99m) - trace;

        // If there is water on the surface, the water in the air will be determined based on
        // vapor pressure later, and should not be randomly assigned. Otherwise, there is a small
        // chance of water vapor without significant surface water (results of cometary deposits, etc.)
        var waterVapor = _planetParams?.EarthlikeAtmosphere == true ? PlanetParams.EarthWaterVaporRatio : 0.0m;
        var surfaceWater = false;
        if (_planetParams?.EarthlikeAtmosphere != true)
        {
            var water = Substances.All.Water.GetHomogeneousReference();
            var seawater = Substances.All.Seawater.GetHomogeneousReference();
            surfaceWater = Hydrosphere.Contains(water) || Hydrosphere.Contains(seawater);
            if (!WaterlessPlanetTypes.HasFlag(PlanetType) && !surfaceWater)
            {
                waterVapor = Math.Max(0, Randomizer.Instance.NextDecimal(-0.05m, 0.001m));
            }
        }

        // Always at least some oxygen if there is water, planetary composition allowing
        var o2 = _planetParams?.EarthlikeAtmosphere == true ? 0.23133m : 0.0m;
        if (_planetParams?.EarthlikeAtmosphere != true && PlanetType != PlanetType.Carbon)
        {
            if (waterVapor != 0)
            {
                o2 = waterVapor * 0.0001m;
            }
            else if (surfaceWater)
            {
                o2 = Randomizer.Instance.NextDecimal(0.002m);
            }
        }

        var o3 = _planetParams?.EarthlikeAtmosphere == true ? o2 * 4.5e-5m : 0;

        // N2 (largely inert gas) comprises whatever is left after the other components have been
        // determined. This is usually a trace amount, unless CO2 has been reduced to a trace, in
        // which case it will comprise the bulk of the atmosphere.
        var n2 = 1 - (h + he + co2 + waterVapor + o2 + o3 + trace);

        // Some portion of the N2 may be Ar instead.
        var ar = _planetParams?.EarthlikeAtmosphere == true ? 1.288e-3m : Math.Max(0, n2 * Randomizer.Instance.NextDecimal(-0.02m, 0.04m));
        n2 -= ar;
        // An even smaller fraction may be Kr.
        var kr = _planetParams?.EarthlikeAtmosphere == true ? 3.3e-6m : Math.Max(0, n2 * Randomizer.Instance.NextDecimal(-2.5e-4m, 5.0e-4m));
        n2 -= kr;
        // An even smaller fraction may be Xe or Ne.
        var xe = _planetParams?.EarthlikeAtmosphere == true ? 8.7e-8m : Math.Max(0, n2 * Randomizer.Instance.NextDecimal(-1.8e-5m, 3.5e-5m));
        n2 -= xe;
        var ne = _planetParams?.EarthlikeAtmosphere == true ? 1.267e-5m : Math.Max(0, n2 * Randomizer.Instance.NextDecimal(-1.8e-5m, 3.5e-5m));
        n2 -= ne;

        var components = new List<(ISubstanceReference, decimal)>()
        {
            (Substances.All.CarbonDioxide.GetHomogeneousReference(), co2),
            (Substances.All.Helium.GetHomogeneousReference(), he),
            (Substances.All.Hydrogen.GetHomogeneousReference(), h),
            (Substances.All.Nitrogen.GetHomogeneousReference(), n2),
        };
        if (ar > 0)
        {
            components.Add((Substances.All.Argon.GetHomogeneousReference(), ar));
        }
        if (co > 0)
        {
            components.Add((Substances.All.CarbonMonoxide.GetHomogeneousReference(), co));
        }
        if (kr > 0)
        {
            components.Add((Substances.All.Krypton.GetHomogeneousReference(), kr));
        }
        if (ch4 > 0)
        {
            components.Add((Substances.All.Methane.GetHomogeneousReference(), ch4));
        }
        if (o2 > 0)
        {
            components.Add((Substances.All.Oxygen.GetHomogeneousReference(), o2));
        }
        if (o3 > 0)
        {
            components.Add((Substances.All.Ozone.GetHomogeneousReference(), o3));
        }
        if (so2 > 0)
        {
            components.Add((Substances.All.SulphurDioxide.GetHomogeneousReference(), so2));
        }
        if (waterVapor > 0)
        {
            components.Add((Substances.All.Water.GetHomogeneousReference(), waterVapor));
        }
        if (xe > 0)
        {
            components.Add((Substances.All.Xenon.GetHomogeneousReference(), xe));
        }
        if (ne > 0)
        {
            components.Add((Substances.All.Neon.GetHomogeneousReference(), ne));
        }
        Atmosphere = new Atmosphere(this, pressure, components.ToArray());
    }

    private void GenerateAtmosphereTrace()
    {
        // For terrestrial (non-giant) planets, these gases remain at low concentrations due to
        // atmospheric escape.
        var h = Randomizer.Instance.NextDecimal(5e-8m, 2e-7m);
        var he = Randomizer.Instance.NextDecimal(2.6e-7m, 1e-5m);

        // 50% chance not to have these components at all.
        var ch4 = Math.Max(0, Randomizer.Instance.NextDecimal(-0.5m, 0.5m));
        var total = ch4;

        var co = Math.Max(0, Randomizer.Instance.NextDecimal(-0.5m, 0.5m));
        total += co;

        var so2 = Math.Max(0, Randomizer.Instance.NextDecimal(-0.5m, 0.5m));
        total += so2;

        var n2 = Math.Max(0, Randomizer.Instance.NextDecimal(-0.5m, 0.5m));
        total += n2;

        // Noble traces: selected as fractions of N2, if present, to avoid over-representation.
        var ar = n2 > 0 ? Math.Max(0, n2 * Randomizer.Instance.NextDecimal(-0.02m, 0.04m)) : 0;
        n2 -= ar;
        var kr = n2 > 0 ? Math.Max(0, n2 * Randomizer.Instance.NextDecimal(-0.02m, 0.04m)) : 0;
        n2 -= kr;
        var xe = n2 > 0 ? Math.Max(0, n2 * Randomizer.Instance.NextDecimal(-0.02m, 0.04m)) : 0;
        n2 -= xe;

        // Carbon monoxide means at least some carbon dioxide, as well.
        var co2 = co > 0
            ? Randomizer.Instance.NextDecimal(0.5m)
            : Math.Max(0, Randomizer.Instance.NextDecimal(-0.5m, 0.5m));
        total += co2;

        // If there is water on the surface, the water in the air will be determined based on
        // vapor pressure later, and should not be randomly assigned. Otherwise, there is a small
        // chance of water vapor without significant surface water (results of cometary deposits, etc.)
        var waterVapor = 0.0m;
        var water = Substances.All.Water.GetHomogeneousReference();
        var seawater = Substances.All.Seawater.GetHomogeneousReference();
        if (!WaterlessPlanetTypes.HasFlag(PlanetType)
            && !Hydrosphere.Contains(water)
            && !Hydrosphere.Contains(seawater))
        {
            waterVapor = Math.Max(0, Randomizer.Instance.NextDecimal(-0.05m, 0.001m));
        }
        total += waterVapor;

        var o2 = 0.0m;
        if (PlanetType != PlanetType.Carbon)
        {
            // Always at least some oxygen if there is water, planetary composition allowing
            o2 = waterVapor > 0
                ? waterVapor * 1e-4m
                : Math.Max(0, Randomizer.Instance.NextDecimal(-0.05m, 0.5m));
        }
        total += o2;

        var ratio = total == 0 ? 0 : (1 - h - he) / total;
        ch4 *= ratio;
        co *= ratio;
        so2 *= ratio;
        n2 *= ratio;
        ar *= ratio;
        kr *= ratio;
        xe *= ratio;
        co2 *= ratio;
        waterVapor *= ratio;
        o2 *= ratio;

        // H and He are always assumed to be present in small amounts if a planet has any
        // atmosphere, but without any other gases making up the bulk of the atmosphere, they are
        // presumed lost to atmospheric escape entirely, and no atmosphere at all is indicated.
        if (total == 0)
        {
            _atmosphere = null;
        }
        else
        {
            var components = new List<(ISubstanceReference, decimal)>()
            {
                (Substances.All.Helium.GetHomogeneousReference(), he),
                (Substances.All.Hydrogen.GetHomogeneousReference(), h),
            };
            if (ar > 0)
            {
                components.Add((Substances.All.Argon.GetHomogeneousReference(), ar));
            }
            if (co2 > 0)
            {
                components.Add((Substances.All.CarbonDioxide.GetHomogeneousReference(), co2));
            }
            if (co > 0)
            {
                components.Add((Substances.All.CarbonMonoxide.GetHomogeneousReference(), co));
            }
            if (kr > 0)
            {
                components.Add((Substances.All.Krypton.GetHomogeneousReference(), kr));
            }
            if (ch4 > 0)
            {
                components.Add((Substances.All.Methane.GetHomogeneousReference(), ch4));
            }
            if (n2 > 0)
            {
                components.Add((Substances.All.Nitrogen.GetHomogeneousReference(), n2));
            }
            if (o2 > 0)
            {
                components.Add((Substances.All.Oxygen.GetHomogeneousReference(), o2));
            }
            if (so2 > 0)
            {
                components.Add((Substances.All.SulphurDioxide.GetHomogeneousReference(), so2));
            }
            if (waterVapor > 0)
            {
                components.Add((Substances.All.Water.GetHomogeneousReference(), waterVapor));
            }
            if (xe > 0)
            {
                components.Add((Substances.All.Xenon.GetHomogeneousReference(), xe));
            }
            Atmosphere = new Atmosphere(this, Randomizer.Instance.NextDouble(25), components.ToArray());
        }
    }

    private Planetoid? GenerateGiantSatellite(
        CosmicLocation? parent,
        List<Star> stars,
        HugeNumber periapsis,
        double eccentricity,
        HugeNumber maxMass)
    {
        var orbit = new OrbitalParameters(
            Mass,
            Position,
            periapsis,
            eccentricity,
            Randomizer.Instance.NextDouble(0.5),
            Randomizer.Instance.NextDouble(DoubleConstants.TwoPi),
            Randomizer.Instance.NextDouble(DoubleConstants.TwoPi),
            Randomizer.Instance.NextDouble(DoubleConstants.TwoPi));
        double chance;

        // If the mass limit allows, there is an even chance that the satellite is a smaller planet.
        if (maxMass > _TerrestrialMinMassForType && Randomizer.Instance.NextBool())
        {
            // Select from the standard distribution of types.
            chance = Randomizer.Instance.NextDouble();

            // Planets with very low orbits are lava planets due to tidal
            // stress (plus a small percentage of others due to impact trauma).

            // The maximum mass and density are used to calculate an outer
            // Roche limit (may not be the actual Roche limit for the body
            // which gets generated).
            if (periapsis < GetRocheLimit(DefaultTerrestrialMaxDensity) * new HugeNumber(105, -2) || chance <= 0.01)
            {
                return new Planetoid(
                    PlanetType.Lava,
                    parent,
                    null,
                    stars,
                    Vector3<HugeNumber>.Zero,
                    out _,
                    orbit,
                    new PlanetParams(MaxMass: maxMass),
                    null,
                    true);
            }
            else if (chance <= 0.65) // Most will be standard terrestrial.
            {
                return new Planetoid(
                    PlanetType.Terrestrial,
                    parent,
                    null,
                    stars,
                    Vector3<HugeNumber>.Zero,
                    out _,
                    orbit,
                    new PlanetParams(MaxMass: maxMass),
                    null,
                    true);
            }
            else if (chance <= 0.75)
            {
                return new Planetoid(
                    PlanetType.Iron,
                    parent,
                    null,
                    stars,
                    Vector3<HugeNumber>.Zero,
                    out _,
                    orbit,
                    new PlanetParams(MaxMass: maxMass),
                    null,
                    true);
            }
            else
            {
                return new Planetoid(
                    PlanetType.Ocean,
                    parent,
                    null,
                    stars,
                    Vector3<HugeNumber>.Zero,
                    out _,
                    orbit,
                    new PlanetParams(MaxMass: maxMass),
                    null,
                    true);
            }
        }

        // Otherwise, if the mass limit allows, there is an even chance that the satellite is a dwarf planet.
        else if (maxMass > _DwarfMinMassForType && Randomizer.Instance.NextBool())
        {
            chance = Randomizer.Instance.NextDouble();
            // Dwarf planets with very low orbits are lava planets due to tidal stress (plus a small percentage of others due to impact trauma).
            if (periapsis < GetRocheLimit(DensityForDwarf) * new HugeNumber(105, -2) || chance <= 0.01)
            {
                return new Planetoid(
                    PlanetType.LavaDwarf,
                    parent,
                    null,
                    stars,
                    Vector3<HugeNumber>.Zero,
                    out _,
                    orbit,
                    new PlanetParams(MaxMass: maxMass),
                    null,
                    true);
            }
            else if (chance <= 0.75) // Most will be standard.
            {
                return new Planetoid(
                    PlanetType.Dwarf,
                    parent,
                    null,
                    stars,
                    Vector3<HugeNumber>.Zero,
                    out _,
                    orbit,
                    new PlanetParams(MaxMass: maxMass),
                    null,
                    true);
            }
            else
            {
                return new Planetoid(
                    PlanetType.RockyDwarf,
                    parent,
                    null,
                    stars,
                    Vector3<HugeNumber>.Zero,
                    out _,
                    orbit,
                    new PlanetParams(MaxMass: maxMass),
                    null,
                    true);
            }
        }

        // Otherwise, it is an asteroid, selected from the standard distribution of types.
        else if (maxMass > 0)
        {
            chance = Randomizer.Instance.NextDouble();
            if (chance <= 0.75)
            {
                return new Planetoid(
                    PlanetType.AsteroidC,
                    parent,
                    null,
                    stars,
                    Vector3<HugeNumber>.Zero,
                    out _,
                    orbit,
                    new PlanetParams(MaxMass: maxMass),
                    null,
                    true);
            }
            else if (chance <= 0.9)
            {
                return new Planetoid(
                    PlanetType.AsteroidS,
                    parent,
                    null,
                    stars,
                    Vector3<HugeNumber>.Zero,
                    out _,
                    orbit,
                    new PlanetParams(MaxMass: maxMass),
                    null,
                    true);
            }
            else
            {
                return new Planetoid(
                    PlanetType.AsteroidM,
                    parent,
                    null,
                    stars,
                    Vector3<HugeNumber>.Zero,
                    out _,
                    orbit,
                    new PlanetParams(MaxMass: maxMass),
                    null,
                    true);
            }
        }

        return null;
    }

    private void GenerateHydrocarbons()
    {
        // It is presumed that it is statistically likely that the current eon is not the first
        // with life, and therefore that some fossilized hydrocarbon deposits exist.
        var coal = (decimal)Randomizer.Instance.NormalDistributionSample(1e-13, 1.7e-14);

        AddResource(Substances.All.Anthracite.GetReference(), coal, false);
        AddResource(Substances.All.BituminousCoal.GetReference(), coal, false);

        var petroleum = (decimal)Randomizer.Instance.NormalDistributionSample(1e-8, 1.6e-9);
        var petroleumSeed = AddResource(Substances.All.Petroleum.GetReference(), petroleum, false);

        // Natural gas is predominantly, though not exclusively, found with petroleum deposits.
        AddResource(Substances.All.NaturalGas.GetReference(), petroleum, false, true, petroleumSeed);
    }

    private void GenerateHydrosphere(double surfaceTemp)
    {
        // Most terrestrial planets will (at least initially) have a hydrosphere layer (oceans,
        // icecaps, etc.). This might be removed later, depending on the planet's conditions.

        if (WaterlessPlanetTypes.HasFlag(PlanetType)
            || !IsTerrestrial)
        {
            NormalizedSeaLevel = -1.1;
            return;
        }

        decimal ratio;
        if (_planetParams.HasValue && _planetParams.Value.WaterRatio.HasValue)
        {
            ratio = _planetParams.Value.WaterRatio.Value;
        }
        else if (PlanetType == PlanetType.Ocean)
        {
            ratio = (decimal)(1 + Randomizer.Instance.NormalDistributionSample(1, 0.2));
        }
        else
        {
            ratio = Randomizer.Instance.NextDecimal();
        }

        var mass = HugeNumber.Zero;
        var seawater = Substances.All.Seawater.GetHomogeneousReference();

        if (ratio <= 0)
        {
            NormalizedSeaLevel = -1.1;
        }
        else if (ratio >= 1)
        {
            NormalizedSeaLevel = (double)ratio;
            mass = new HollowSphere<HugeNumber>(
                Shape.ContainingRadius,
                Shape.ContainingRadius + SeaLevel).Volume * (seawater.Homogeneous.DensityLiquid ?? 0);
        }
        else
        {
            var seaLevel = 0.0;
            NormalizedSeaLevel = 0;
            const double RandomMapElevationFactor = 0.33975352675545284; // proportion of MaxElevation of a random elevation map * 1/(e-1)
            var variance = ratio == 0.5m
                ? 0
                : (Math.Exp(Math.Abs(((double)ratio) - 0.5)) - 1) * RandomMapElevationFactor;
            if (ratio != 0.5m)
            {
                seaLevel = ratio > 0.5m
                    ? variance
                    : -variance;
                NormalizedSeaLevel = seaLevel;
            }
            const double HalfVolume = 85183747862278.266; // empirical sum of random map pixel columns with 0 sea level
            var volume = seaLevel > 0
                ? HalfVolume + (HalfVolume * variance)
                : HalfVolume - (HalfVolume * variance);
            mass = volume
                * MaxElevation
                * (seawater.Homogeneous.DensityLiquid ?? 0);
        }

        if (!mass.IsPositive())
        {
            Hydrosphere = new Material<HugeNumber>();
            return;
        }

        // Surface water is mostly salt water.
        var seawaterProportion = (decimal)Randomizer.Instance.NormalDistributionSample(0.945, 0.015);
        var waterProportion = 1 - seawaterProportion;
        var water = Substances.All.Water.GetHomogeneousReference();
        var density = ((seawater.Homogeneous.DensityLiquid ?? 0) * (double)seawaterProportion) + ((water.Homogeneous.DensityLiquid ?? 0) * (double)waterProportion);

        var outerRadius = (3 * ((mass / density) + new Sphere<HugeNumber>(Material.Shape.ContainingRadius).Volume) / HugeNumberConstants.FourPi).Cbrt();
        var shape = new HollowSphere<HugeNumber>(
            Material.Shape.ContainingRadius,
            outerRadius,
            Material.Shape.Position);
        var avgDepth = (double)(outerRadius - Material.Shape.ContainingRadius) / 2;
        double avgTemp;
        if (avgDepth > 1000)
        {
            avgTemp = 277;
        }
        else if (avgDepth < 200)
        {
            avgTemp = surfaceTemp;
        }
        else
        {
            avgTemp = surfaceTemp.Lerp(277, (avgDepth - 200) / 800);
        }

        Hydrosphere = new Material<HugeNumber>(
            shape,
            mass,
            density,
            avgTemp,
            (seawater, seawaterProportion),
            (water, waterProportion));

        FractionHydrosphere(surfaceTemp);

        if (Material.GetSurface() is Material<HugeNumber> material)
        {
            material.AddConstituents(CosmicSubstances.WetPlanetaryCrustConstituents);
        }
    }

    /// <summary>
    /// Determines whether this planet is capable of sustaining life, and whether or not it
    /// actually does. If so, the atmosphere may be adjusted.
    /// </summary>
    /// <returns>
    /// True if the atmosphere's greenhouse potential is adjusted; false if not.
    /// </returns>
    private bool GenerateLife()
    {
        if (IsInhospitable || !HasLiquidWater())
        {
            HasBiosphere = false;
            return false;
        }

        // If the planet already has a biosphere, there is nothing left to do.
        if (HasBiosphere)
        {
            return false;
        }

        HasBiosphere = true;

        GenerateHydrocarbons();

        // If the habitable zone is a subsurface ocean, no further adjustments occur.
        if (Hydrosphere is Composite<HugeNumber>)
        {
            return false;
        }

        if (_planetParams?.EarthlikeAtmosphere == true)
        {
            return false;
        }

        // If there is a habitable surface layer, it is presumed that an initial population of a
        // cyanobacteria analogue will produce a significant amount of free oxygen, which in turn
        // will transform most CH4 to CO2 and H2O, and also produce an ozone layer.
        var o2 = Randomizer.Instance.NextDecimal(0.2m, 0.25m);
        var oxygen = Substances.All.Oxygen.GetHomogeneousReference();
        Atmosphere.Material.Add(oxygen, o2);

        // Calculate ozone based on level of free oxygen.
        var o3 = o2 * 4.5e-5m;
        var ozone = Substances.All.Ozone.GetHomogeneousReference();
        if (Atmosphere.Material is not Composite<HugeNumber> lc || lc.Components.Count < 3)
        {
            Atmosphere.DifferentiateTroposphere(); // First ensure troposphere is differentiated.
            (Atmosphere.Material as Composite<HugeNumber>)?.CopyComponent(1, HugeNumberConstants.Centi);
        }
        (Atmosphere.Material as Composite<HugeNumber>)?.Components[2].Add(ozone, o3);

        // Convert most methane to CO2 and H2O.
        var methane = Substances.All.Methane.GetHomogeneousReference();
        var ch4 = Atmosphere.Material.GetProportion(methane);
        if (ch4 != 0)
        {
            // The levels of CO2 and H2O are not adjusted; it is presumed that the levels already
            // determined for them take the amounts derived from CH4 into account. If either gas
            // is entirely missing, however, it is added.
            var carbonDioxide = Substances.All.CarbonDioxide.GetHomogeneousReference();
            if (Atmosphere.Material.GetProportion(carbonDioxide) <= 0)
            {
                Atmosphere.Material.Add(carbonDioxide, ch4 / 3);
            }

            if (Atmosphere.Material.GetProportion(Substances.All.Water) <= 0)
            {
                Atmosphere.Material.Add(Substances.All.Water, ch4 * 2 / 3);
                Atmosphere.ResetWater();
            }

            Atmosphere.Material.Add(methane, ch4 * 0.001m);

            Atmosphere.ResetGreenhouseFactor();
            ResetCachedTemperatures();
            return true;
        }

        return false;
    }

    private void GenerateMaterial(
        double? temperature,
        Vector3<HugeNumber> position,
        HugeNumber semiMajorAxis)
    {
        if (PlanetType == PlanetType.Comet)
        {
            Material = new Material<HugeNumber>(
                CosmicSubstances.CometNucleus,
                // Gaussian distribution with most values between 1km and 19km.
                new Ellipsoid<HugeNumber>(
                    Randomizer.Instance.NormalDistributionSample(10000, 4500, minimum: 0),
                    Randomizer.Instance.Next(HugeNumberConstants.Half, 1),
                    position),
                Randomizer.Instance.NextDouble(300, 700),
                null,
                temperature);
            return;
        }

        if (IsAsteroid)
        {
            var doubleMaxMass = _planetParams.HasValue && _planetParams.Value.MaxMass.HasValue
                ? (double)_planetParams.Value.MaxMass.Value
                : AsteroidMaxMassForType;
            var mass = Randomizer.Instance.PositiveNormalDistributionSample(
                AsteroidMinMassForType,
                (doubleMaxMass - AsteroidMinMassForType) / 3,
                doubleMaxMass);

            var asteroidDensity = PlanetType switch
            {
                PlanetType.AsteroidC => 1380,
                PlanetType.AsteroidM => 5320,
                PlanetType.AsteroidS => 2710,
                _ => 2000,
            };

            var axis = (mass * new HugeNumber(75, -2) / (asteroidDensity * HugeNumber.Pi)).Cbrt();
            var irregularity = Randomizer.Instance.Next(HugeNumberConstants.Half, HugeNumber.One);
            var shape = new Ellipsoid<HugeNumber>(axis, axis * irregularity, axis / irregularity, position);

            var substances = GetAsteroidComposition();
            Material = new Material<HugeNumber>(
                substances,
                shape,
                mass,
                asteroidDensity,
                temperature);
            return;
        }

        if (PlanetType is PlanetType.Lava
            or PlanetType.LavaDwarf)
        {
            temperature = Randomizer.Instance.NextDouble(974, 1574);
        }

        var density = GetDensity(PlanetType);

        double? gravity = null;
        if (_planetParams?.SurfaceGravity.HasValue == true)
        {
            gravity = _planetParams!.Value.SurfaceGravity!.Value;
        }
        else if (_habitabilityRequirements?.MinimumGravity.HasValue == true
            || _habitabilityRequirements?.MaximumGravity.HasValue == true)
        {
            double maxGravity;
            if (_habitabilityRequirements?.MaximumGravity.HasValue == true)
            {
                maxGravity = _habitabilityRequirements!.Value.MaximumGravity!.Value;
            }
            else // Determine the maximum gravity the planet could have by calculating from its maximum mass.
            {
                var max = _planetParams?.MaxMass ?? GetMaxMassForType(PlanetType);
                var maxVolume = max / density;
                var maxRadius = (maxVolume / HugeNumberConstants.FourThirdsPi).Cbrt();
                maxGravity = (double)(HugeNumberConstants.G * max / (maxRadius * maxRadius));
            }
            gravity = Randomizer.Instance.NextDouble(_habitabilityRequirements?.MinimumGravity ?? 0, maxGravity);
        }

        HugeNumber MassFromUnknownGravity(HugeNumber semiMajorAxis)
        {
            var min = HugeNumber.Zero;
            if (!PlanetType.AnyDwarf.HasFlag(PlanetType))
            {
                // Stern-Levison parameter for neighborhood-clearing used to determined minimum mass
                // at which the planet would be able to do so at this orbital distance. We set the
                // minimum at two orders of magnitude more than this (planets in our solar system
                // all have masses above 5 orders of magnitude more). Note that since lambda is
                // proportional to the square of mass, it is multiplied by 10 to obtain a difference
                // of 2 orders of magnitude, rather than by 100.
                var sternLevisonLambdaMass = (HugeNumber.Pow(semiMajorAxis, new HugeNumber(15, -1)) / new HugeNumber(2.5, -28)).Sqrt();
                min = HugeNumber.Max(min, sternLevisonLambdaMass * 10);

                // sanity check; may result in a "planet" which *can't* clear its neighborhood
                if (_planetParams.HasValue
                    && _planetParams.Value.MaxMass.HasValue
                    && min > _planetParams.Value.MaxMass.Value)
                {
                    min = _planetParams.Value.MaxMass.Value;
                }
            }
            return _planetParams.HasValue && _planetParams.Value.MaxMass.HasValue
                ? Randomizer.Instance.Next(min, _planetParams.Value.MaxMass.Value)
                : min;
        }

        if (_planetParams?.Radius.HasValue == true)
        {
            var radius = HugeNumber.Max(MinimumRadius, _planetParams!.Value.Radius!.Value);
            var flattening = Randomizer.Instance.Next(HugeNumberConstants.Deci);
            var shape = new Ellipsoid<HugeNumber>(radius, radius * (1 - flattening), position);

            HugeNumber mass;
            if (gravity.HasValue)
            {
                mass = GetMass(PlanetType, semiMajorAxis, _planetParams?.MaxMass, gravity.Value, shape);
            }
            else
            {
                mass = MassFromUnknownGravity(semiMajorAxis);
            }

            Material = GetComposition(density, mass, shape, temperature);
        }
        else if (gravity.HasValue)
        {
            var radius = HugeNumber.Max(MinimumRadius, HugeNumber.Min(GetRadiusForSurfaceGravity(gravity.Value), GetRadiusForMass(density, GetMaxMassForType(PlanetType))));
            var flattening = Randomizer.Instance.Next(HugeNumberConstants.Deci);
            var shape = new Ellipsoid<HugeNumber>(radius, radius * (1 - flattening), position);

            var mass = GetMass(PlanetType, semiMajorAxis, _planetParams?.MaxMass, gravity.Value, shape);

            Material = GetComposition(density, mass, shape, temperature);
        }
        else
        {
            HugeNumber mass;
            if (IsGiant)
            {
                mass = Randomizer.Instance.Next(_GiantMinMassForType, _planetParams?.MaxMass ?? _GiantMaxMassForType);
            }
            else if (IsDwarf)
            {
                var maxMass = _planetParams?.MaxMass;
                if (!string.IsNullOrEmpty(ParentId))
                {
                    var sternLevisonLambdaMass = (HugeNumber.Pow(semiMajorAxis, new HugeNumber(15, -1)) / new HugeNumber(2.5, -28)).Sqrt();
                    maxMass = HugeNumber.Min(_planetParams?.MaxMass ?? _DwarfMaxMassForType, sternLevisonLambdaMass / 100);
                    if (maxMass < _DwarfMinMassForType)
                    {
                        maxMass = _DwarfMinMassForType; // sanity check; may result in a "dwarf" planet which *can* clear its neighborhood
                    }
                }
                mass = Randomizer.Instance.Next(_DwarfMinMassForType, maxMass ?? _DwarfMaxMassForType);
            }
            else
            {
                mass = MassFromUnknownGravity(semiMajorAxis);
            }

            // An approximate radius as if the shape was a sphere is determined, which is no less
            // than the minimum required for hydrostatic equilibrium.
            var radius = HugeNumber.Max(MinimumRadius, GetRadiusForMass(density, mass));
            var flattening = Randomizer.Instance.Next(HugeNumberConstants.Deci);
            var shape = new Ellipsoid<HugeNumber>(radius, radius * (1 - flattening), position);

            Material = GetComposition(density, mass, shape, temperature);
        }
    }

    private void GenerateOrbit(
        OrbitalParameters? orbit,
        CosmicLocation? orbitedObject,
        double eccentricity,
        HugeNumber semiMajorAxis)
    {
        if (orbit.HasValue)
        {
            Space.Orbit.AssignOrbit(this, orbit.Value);
            return;
        }

        if (orbitedObject is null)
        {
            return;
        }

        if (PlanetType == PlanetType.Comet)
        {
            Space.Orbit.AssignOrbit(
                this,
                orbitedObject,

                // Current distance is presumed to be apoapsis for comets, which are presumed to
                // originate in an Oort cloud, and have eccentricities which may either leave
                // them there, or send them into the inner solar system.
                (1 - eccentricity) / (1 + eccentricity) * GetDistanceTo(orbitedObject),

                eccentricity,
                Randomizer.Instance.NextDouble(Math.PI),
                Randomizer.Instance.NextDouble(DoubleConstants.TwoPi),
                Randomizer.Instance.NextDouble(DoubleConstants.TwoPi),
                Math.PI);
            return;
        }

        if (IsAsteroid)
        {
            Space.Orbit.AssignOrbit(
                this,
                orbitedObject,
                GetDistanceTo(orbitedObject),
                eccentricity,
                Randomizer.Instance.NextDouble(0.5),
                Randomizer.Instance.NextDouble(DoubleConstants.TwoPi),
                Randomizer.Instance.NextDouble(DoubleConstants.TwoPi),
                0);
            return;
        }

        if (!IsTerrestrial)
        {
            Space.Orbit.AssignOrbit(
                this,
                orbitedObject,
                GetDistanceTo(orbitedObject),
                eccentricity,
                Randomizer.Instance.NextDouble(0.9),
                Randomizer.Instance.NextDouble(DoubleConstants.TwoPi),
                Randomizer.Instance.NextDouble(DoubleConstants.TwoPi),
                Randomizer.Instance.NextDouble(DoubleConstants.TwoPi));
            return;
        }

        var ta = Randomizer.Instance.NextDouble(DoubleConstants.TwoPi);
        if (_planetParams?.RevolutionPeriod.HasValue == true)
        {
            GenerateOrbit(orbitedObject, eccentricity, semiMajorAxis, ta);
        }
        else
        {
            Space.Orbit.AssignOrbit(
                this,
                orbitedObject,
                GetDistanceTo(orbitedObject),
                eccentricity,
                Randomizer.Instance.NextDouble(0.9),
                Randomizer.Instance.NextDouble(DoubleConstants.TwoPi),
                Randomizer.Instance.NextDouble(DoubleConstants.TwoPi),
                Randomizer.Instance.NextDouble(DoubleConstants.TwoPi));
        }
    }

    private void GenerateOrbit(
        CosmicLocation orbitedObject,
        double eccentricity,
        HugeNumber semiMajorAxis,
        double trueAnomaly) => Space.Orbit.AssignOrbit(
        this,
        orbitedObject,
        (1 - eccentricity) * semiMajorAxis,
        eccentricity,
        Randomizer.Instance.NextDouble(0.9),
        Randomizer.Instance.NextDouble(DoubleConstants.TwoPi),
        Randomizer.Instance.NextDouble(DoubleConstants.TwoPi),
        trueAnomaly);

    private void GenerateResources()
    {
        AddResources(Material.GetSurface()
                .Constituents.Where(x => x.Key.Substance.Categories?.Contains(Substances.Category_Gem) == true
                    || x.Key.Substance.IsMetalOre())
                .Select(x => (x.Key, x.Value, true))
                ?? Enumerable.Empty<(ISubstanceReference, decimal, bool)>());
        AddResources(Material.GetSurface()
                .Constituents.Where(x => x.Key.Substance.IsHydrocarbon())
                .Select(x => (x.Key, x.Value, false))
                ?? Enumerable.Empty<(ISubstanceReference, decimal, bool)>());

        // Also add halite (rock salt) as a resource, despite not being an ore or gem.
        AddResources(Material.GetSurface()
                .Constituents.Where(x => x.Key.Equals(Substances.All.SodiumChloride.GetHomogeneousReference()))
                .Select(x => (x.Key, x.Value, false))
                ?? Enumerable.Empty<(ISubstanceReference, decimal, bool)>());

        // A magnetosphere is presumed to indicate tectonic, and hence volcanic, activity.
        // This, in turn, indicates elemental sulfur at the surface.
        if (HasMagnetosphere)
        {
            var sulfurProportion = (decimal)Randomizer.Instance.NormalDistributionSample(3.5e-5, 1.75e-6);
            if (sulfurProportion > 0)
            {
                AddResource(Substances.All.Sulfur.GetHomogeneousReference(), sulfurProportion, false);
            }
        }

        if (IsTerrestrial)
        {
            var beryl = (decimal)Randomizer.Instance.NormalDistributionSample(4e-6, 6.7e-7, minimum: 0);
            var emerald = beryl * 1.58e-4m;
            var corundum = (decimal)Randomizer.Instance.NormalDistributionSample(2.6e-4, 4e-5, minimum: 0);
            var ruby = corundum * 1.58e-4m;
            var sapphire = corundum * 5.7e-3m;

            var diamond = PlanetType == PlanetType.Carbon
                ? 0 // Carbon planets have diamond in the crust, which will have been added earlier.
                : (decimal)Randomizer.Instance.NormalDistributionSample(1.5e-7, 2.5e-8, minimum: 0);

            if (beryl > 0)
            {
                AddResource(Substances.All.Beryl.GetHomogeneousReference(), beryl, true);
            }
            if (emerald > 0)
            {
                AddResource(Substances.All.Emerald.GetHomogeneousReference(), emerald, true);
            }
            if (corundum > 0)
            {
                AddResource(Substances.All.Corundum.GetHomogeneousReference(), corundum, true);
            }
            if (ruby > 0)
            {
                AddResource(Substances.All.Ruby.GetHomogeneousReference(), ruby, true);
            }
            if (sapphire > 0)
            {
                AddResource(Substances.All.Sapphire.GetHomogeneousReference(), sapphire, true);
            }
            if (diamond > 0)
            {
                AddResource(Substances.All.Diamond.GetHomogeneousReference(), diamond, true);
            }
        }
    }

    private Planetoid? GenerateSatellite(
        CosmicLocation? parent,
        List<Star> stars,
        HugeNumber periapsis,
        double eccentricity,
        HugeNumber maxMass)
    {
        if (PlanetType is PlanetType.GasGiant
            or PlanetType.IceGiant)
        {
            return GenerateGiantSatellite(parent, stars, periapsis, eccentricity, maxMass);
        }
        if (PlanetType == PlanetType.AsteroidC)
        {
            return new Planetoid(
                PlanetType.AsteroidC,
                parent,
                null,
                stars,
                Vector3<HugeNumber>.Zero,
                out _,
                GetAsteroidSatelliteOrbit(periapsis, eccentricity),
                new PlanetParams(MaxMass: maxMass),
                null,
                true);
        }
        if (PlanetType == PlanetType.AsteroidM)
        {
            return new Planetoid(
                PlanetType.AsteroidM,
                parent,
                null,
                stars,
                Vector3<HugeNumber>.Zero,
                out _,
                GetAsteroidSatelliteOrbit(periapsis, eccentricity),
                new PlanetParams(MaxMass: maxMass),
                null,
                true);
        }
        if (PlanetType == PlanetType.AsteroidS)
        {
            return new Planetoid(
                PlanetType.AsteroidS,
                parent,
                null,
                stars,
                Vector3<HugeNumber>.Zero,
                out _,
                GetAsteroidSatelliteOrbit(periapsis, eccentricity),
                new PlanetParams(MaxMass: maxMass),
                null,
                true);
        }
        if (PlanetType == PlanetType.Comet)
        {
            return null;
        }
        var orbit = new OrbitalParameters(
            Mass,
            Position,
            periapsis,
            eccentricity,
            Randomizer.Instance.NextDouble(0.5),
            Randomizer.Instance.NextDouble(DoubleConstants.TwoPi),
            Randomizer.Instance.NextDouble(DoubleConstants.TwoPi),
            Randomizer.Instance.NextDouble(DoubleConstants.TwoPi));
        double chance;

        // If the mass limit allows, there is an even chance that the satellite is a smaller planet.
        if (maxMass > _TerrestrialMinMassForType && Randomizer.Instance.NextBool())
        {
            // Select from the standard distribution of types.
            chance = Randomizer.Instance.NextDouble();

            // Planets with very low orbits are lava planets due to tidal
            // stress (plus a small percentage of others due to impact trauma).

            // Most will be standard terrestrial.
            double terrestrialChance;
            if (PlanetType == PlanetType.Carbon)
            {
                terrestrialChance = 0.45;
            }
            else if (IsGiant)
            {
                terrestrialChance = 0.65;
            }
            else
            {
                terrestrialChance = 0.77;
            }

            // The maximum mass and density are used to calculate an outer
            // Roche limit (may not be the actual Roche limit for the body
            // which gets generated).
            if (periapsis < GetRocheLimit(DefaultTerrestrialMaxDensity) * new HugeNumber(105, -2) || chance <= 0.01)
            {
                return new Planetoid(
                    PlanetType.Lava,
                    parent,
                    null,
                    stars,
                    Vector3<HugeNumber>.Zero,
                    out _,
                    orbit,
                    new PlanetParams(MaxMass: maxMass),
                    null,
                    true);
            }
            else if (chance <= terrestrialChance)
            {
                return new Planetoid(
                    PlanetType.Terrestrial,
                    parent,
                    null,
                    stars,
                    Vector3<HugeNumber>.Zero,
                    out _,
                    orbit,
                    new PlanetParams(MaxMass: maxMass),
                    null,
                    true);
            }
            else if (PlanetType == PlanetType.Carbon && chance <= 0.77) // Carbon planets alone have a chance for carbon satellites.
            {
                return new Planetoid(
                    PlanetType.Carbon,
                    parent,
                    null,
                    stars,
                    Vector3<HugeNumber>.Zero,
                    out _,
                    orbit,
                    new PlanetParams(MaxMass: maxMass),
                    null,
                    true);
            }
            else if (IsGiant && chance <= 0.75)
            {
                return new Planetoid(
                    PlanetType.Iron,
                    parent,
                    null,
                    stars,
                    Vector3<HugeNumber>.Zero,
                    out _,
                    orbit,
                    new PlanetParams(MaxMass: maxMass),
                    null,
                    true);
            }
            else
            {
                return new Planetoid(
                    PlanetType.Ocean,
                    parent,
                    null,
                    stars,
                    Vector3<HugeNumber>.Zero,
                    out _,
                    orbit,
                    new PlanetParams(MaxMass: maxMass),
                    null,
                    true);
            }
        }

        // Otherwise, if the mass limit allows, there is an even chance that the satellite is a dwarf planet.
        else if (maxMass > _DwarfMinMassForType && Randomizer.Instance.NextBool())
        {
            chance = Randomizer.Instance.NextDouble();
            // Dwarf planets with very low orbits are lava planets due to tidal stress (plus a small percentage of others due to impact trauma).
            if (periapsis < GetRocheLimit(DensityForDwarf) * new HugeNumber(105, -2) || chance <= 0.01)
            {
                return new Planetoid(
                    PlanetType.LavaDwarf,
                    parent,
                    null,
                    stars,
                    Vector3<HugeNumber>.Zero,
                    out _,
                    orbit,
                    new PlanetParams(MaxMass: maxMass),
                    null,
                    true);
            }
            else if (chance <= 0.75) // Most will be standard.
            {
                return new Planetoid(
                    PlanetType.Dwarf,
                    parent,
                    null,
                    stars,
                    Vector3<HugeNumber>.Zero,
                    out _,
                    orbit,
                    new PlanetParams(MaxMass: maxMass),
                    null,
                    true);
            }
            else
            {
                return new Planetoid(
                    PlanetType.RockyDwarf,
                    parent,
                    null,
                    stars,
                    Vector3<HugeNumber>.Zero,
                    out _,
                    orbit,
                    new PlanetParams(MaxMass: maxMass),
                    null,
                    true);
            }
        }

        // Otherwise, it is an asteroid, selected from the standard distribution of types.
        else if (maxMass > 0)
        {
            chance = Randomizer.Instance.NextDouble();
            if (chance <= 0.75)
            {
                return new Planetoid(
                    PlanetType.AsteroidC,
                    parent,
                    null,
                    stars,
                    Vector3<HugeNumber>.Zero,
                    out _,
                    orbit,
                    new PlanetParams(MaxMass: maxMass),
                    null,
                    true);
            }
            else if (chance <= 0.9)
            {
                return new Planetoid(
                    PlanetType.AsteroidS,
                    parent,
                    null,
                    stars,
                    Vector3<HugeNumber>.Zero,
                    out _,
                    orbit,
                    new PlanetParams(MaxMass: maxMass),
                    null,
                    true);
            }
            else
            {
                return new Planetoid(
                    PlanetType.AsteroidM,
                    parent,
                    null,
                    stars,
                    Vector3<HugeNumber>.Zero,
                    out _,
                    orbit,
                    new PlanetParams(MaxMass: maxMass),
                    null,
                    true);
            }
        }

        return null;
    }

    private List<(ISubstanceReference, decimal)> GetAsteroidComposition()
    {
        var substances = new List<(ISubstanceReference, decimal)>();

        if (PlanetType == PlanetType.AsteroidM)
        {
            var ironNickel = 0.95m;

            var rock = Randomizer.Instance.NextDecimal(0.2m);
            ironNickel -= rock;

            var gold = Randomizer.Instance.NextDecimal(0.05m);

            var platinum = 0.05m - gold;

            foreach (var (material, proportion) in CosmicSubstances._ChondriticRockMixture_NoMetal)
            {
                substances.Add((material, proportion * rock));
            }
            substances.Add((Substances.All.IronNickelAlloy.GetHomogeneousReference(), ironNickel));
            substances.Add((Substances.All.Gold.GetHomogeneousReference(), gold));
            substances.Add((Substances.All.Platinum.GetHomogeneousReference(), platinum));
        }
        else if (PlanetType == PlanetType.AsteroidS)
        {
            var gold = Randomizer.Instance.NextDecimal(0.005m);

            foreach (var (material, proportion) in CosmicSubstances._ChondriticRockMixture_NoMetal)
            {
                substances.Add((material, proportion * 0.427m));
            }
            substances.Add((Substances.All.IronNickelAlloy.GetHomogeneousReference(), 0.568m));
            substances.Add((Substances.All.Gold.GetHomogeneousReference(), gold));
            substances.Add((Substances.All.Platinum.GetHomogeneousReference(), 0.005m - gold));
        }
        else
        {
            var rock = 1m;

            var clay = Randomizer.Instance.NextDecimal(0.1m, 0.2m);
            rock -= clay;

            var ice = PlanetType.AnyDwarf.HasFlag(PlanetType)
                ? Randomizer.Instance.NextDecimal()
                : Randomizer.Instance.NextDecimal(0.22m);
            rock -= ice;

            foreach (var (material, proportion) in CosmicSubstances._ChondriticRockMixture)
            {
                substances.Add((material, proportion * rock));
            }
            substances.Add((Substances.All.BallClay.GetReference(), clay));
            substances.Add((Substances.All.Water.GetHomogeneousReference(), ice));
        }

        return substances;
    }

    private OrbitalParameters GetAsteroidSatelliteOrbit(HugeNumber periapsis, double eccentricity) => new(
        Mass,
        Position,
        periapsis,
        eccentricity,
        Randomizer.Instance.NextDouble(0.5),
        Randomizer.Instance.NextDouble(DoubleConstants.TwoPi),
        Randomizer.Instance.NextDouble(DoubleConstants.TwoPi),
        Randomizer.Instance.NextDouble(DoubleConstants.TwoPi));

    private Composite<HugeNumber> GetComposition(double density, HugeNumber mass, IShape<HugeNumber> shape, double? temperature)
    {
        var coreProportion = PlanetType switch
        {
            PlanetType.Dwarf
                or PlanetType.LavaDwarf
                or PlanetType.RockyDwarf => Randomizer.Instance.Next(
                new HugeNumber(2, -1),
                new HugeNumber(55, -2)),
            PlanetType.Carbon or PlanetType.Iron => new HugeNumber(4, -1),
            _ => new HugeNumber(15, -2),
        };

        var crustProportion = IsGiant
            ? HugeNumber.Zero
            // Smaller planemos have thicker crusts due to faster proto-planetary cooling.
            : 400000 / HugeNumber.Pow(shape.ContainingRadius, new HugeNumber(16, -1));

        var coreLayers = IsGiant
            ? GetCore_Giant(shape, coreProportion, mass).ToList()
            : GetCore(shape, coreProportion, crustProportion, mass).ToList();
        var topCoreLayer = coreLayers.Last();
        var coreShape = topCoreLayer.Shape;
        var coreTemp = topCoreLayer.Temperature ?? 0;

        var mantleProportion = 1 - (coreProportion + crustProportion);
        var mantleLayers = GetMantle(shape, mantleProportion, crustProportion, mass, coreShape, coreTemp).ToList();
        if (mantleLayers.Count == 0
            && mantleProportion.IsPositive())
        {
            crustProportion += mantleProportion;
        }

        var crustLayers = GetCrust(shape, crustProportion, mass).ToList();
        if (crustLayers.Count == 0
            && crustProportion.IsPositive())
        {
            if (mantleLayers.Count == 0)
            {
                var ratio = 1 / coreProportion;
                foreach (var layer in coreLayers)
                {
                    layer.Mass *= ratio;
                }
            }
            else
            {
                var ratio = 1 / (coreProportion + mantleProportion);
                foreach (var layer in coreLayers)
                {
                    layer.Mass *= ratio;
                }
                foreach (var layer in mantleLayers)
                {
                    layer.Mass *= ratio;
                }
            }
        }

        var layers = new List<IMaterial<HugeNumber>>();
        layers.AddRange(coreLayers);
        layers.AddRange(mantleLayers);
        layers.AddRange(crustLayers);
        return new Composite<HugeNumber>(
            layers,
            shape,
            mass,
            density,
            temperature);
    }

    private IEnumerable<IMaterial<HugeNumber>> GetCore(
        IShape<HugeNumber> planetShape,
        HugeNumber coreProportion,
        HugeNumber crustProportion,
        HugeNumber planetMass)
    {
        var coreMass = planetMass * coreProportion;

        var coreRadius = planetShape.ContainingRadius * coreProportion;
        var shape = new Sphere<HugeNumber>(coreRadius, planetShape.Position);

        var mantleBoundaryDepth = planetShape.ContainingRadius * crustProportion;

        (ISubstanceReference, decimal)[] coreConstituents;
        if (PlanetType == PlanetType.Carbon)
        {
            // Iron/steel-nickel core (some steel forms naturally in the carbon-rich environment).
            var coreSteel = Randomizer.Instance.NextDecimal(0.945m);
            coreConstituents = new (ISubstanceReference, decimal)[]
            {
                (Substances.All.Iron.GetHomogeneousReference(), 0.945m - coreSteel),
                (Substances.All.CarbonSteel.GetHomogeneousReference(), coreSteel),
                (Substances.All.Nickel.GetHomogeneousReference(), 0.055m),
            };
        }
        else
        {
            coreConstituents = new (ISubstanceReference, decimal)[] { (Substances.All.IronNickelAlloy.GetHomogeneousReference(), 1) };
        }

        yield return new Material<HugeNumber>(
            shape,
            coreMass,
            null,
            (double)((mantleBoundaryDepth * new HugeNumber(115, -2)) + (planetShape.ContainingRadius - coreRadius - mantleBoundaryDepth)),
            coreConstituents);
    }

    private IEnumerable<IMaterial<HugeNumber>> GetCrust(
        IShape<HugeNumber> planetShape,
        HugeNumber crustProportion,
        HugeNumber planetMass)
    {
        if (IsGiant)
        {
            yield break;
        }
        else if (PlanetType == PlanetType.RockyDwarf)
        {
            foreach (var item in GetCrust_RockyDwarf(planetShape, crustProportion, planetMass))
            {
                yield return item;
            }
            yield break;
        }
        else if (PlanetType == PlanetType.LavaDwarf)
        {
            foreach (var item in GetCrust_LavaDwarf(planetShape, crustProportion, planetMass))
            {
                yield return item;
            }
            yield break;
        }
        else if (PlanetType == PlanetType.Carbon)
        {
            foreach (var item in GetCrust_Carbon(planetShape, crustProportion, planetMass))
            {
                yield return item;
            }
            yield break;
        }
        else if (IsTerrestrial)
        {
            foreach (var item in GetCrust_Terrestrial(planetShape, crustProportion, planetMass))
            {
                yield return item;
            }
            yield break;
        }

        var crustMass = planetMass * crustProportion;

        var shape = new HollowSphere<HugeNumber>(
            planetShape.ContainingRadius - (planetShape.ContainingRadius * crustProportion),
            planetShape.ContainingRadius,
            planetShape.Position);

        var dust = Randomizer.Instance.NextDecimal();
        var total = dust;

        // 50% chance of not including the following:
        var waterIce = Math.Max(0, Randomizer.Instance.NextDecimal(-0.5m, 0.5m));
        total += waterIce;

        var n2 = Math.Max(0, Randomizer.Instance.NextDecimal(-0.5m, 0.5m));
        total += n2;

        var ch4 = Math.Max(0, Randomizer.Instance.NextDecimal(-0.5m, 0.5m));
        total += ch4;

        var co = Math.Max(0, Randomizer.Instance.NextDecimal(-0.5m, 0.5m));
        total += co;

        var co2 = Math.Max(0, Randomizer.Instance.NextDecimal(-0.5m, 0.5m));
        total += co2;

        var nh3 = Math.Max(0, Randomizer.Instance.NextDecimal(-0.5m, 0.5m));
        total += nh3;

        var ratio = 1 / total;
        dust *= ratio;
        waterIce *= ratio;
        n2 *= ratio;
        ch4 *= ratio;
        co *= ratio;
        co2 *= ratio;
        nh3 *= ratio;

        var components = new List<(ISubstanceReference, decimal)>()
        {
            (Substances.All.CosmicDust.GetHomogeneousReference(), dust),
        };
        if (waterIce > 0)
        {
            components.Add((Substances.All.Water.GetHomogeneousReference(), waterIce));
        }
        if (n2 > 0)
        {
            components.Add((Substances.All.Nitrogen.GetHomogeneousReference(), n2));
        }
        if (ch4 > 0)
        {
            components.Add((Substances.All.Methane.GetHomogeneousReference(), ch4));
        }
        if (co > 0)
        {
            components.Add((Substances.All.CarbonMonoxide.GetHomogeneousReference(), co));
        }
        if (co2 > 0)
        {
            components.Add((Substances.All.CarbonDioxide.GetHomogeneousReference(), co2));
        }
        if (nh3 > 0)
        {
            components.Add((Substances.All.Ammonia.GetHomogeneousReference(), nh3));
        }
        yield return new Material<HugeNumber>(
            components,
            shape,
            crustMass);
    }

    /// <summary>
    /// Calculates the distance (in meters) this <see cref="Planetoid"/> would have to be
    /// from a <see cref="Star"/> in order to have the given effective temperature.
    /// </summary>
    /// <remarks>
    /// The effects of other nearby stars are ignored.
    /// </remarks>
    /// <param name="star">The <see cref="Star"/> for which the calculation is to be made.</param>
    /// <param name="temperature">The desired temperature, in K.</param>
    private HugeNumber GetDistanceForTemperature(Star star, double temperature)
    {
        var areaRatio = 1;
        if (RotationalPeriod > 2500)
        {
            if (RotationalPeriod <= 75000)
            {
                areaRatio = 4;
            }
            else if (RotationalPeriod <= 150000)
            {
                areaRatio = 3;
            }
            else if (RotationalPeriod <= 300000)
            {
                areaRatio = 2;
            }
        }

        return Math.Sqrt(star.Luminosity * (1 - Albedo)
            / (Math.Pow(temperature - Temperature, 4)
            * DoubleConstants.FourPi
            * DoubleConstants.sigma
            * areaRatio));
    }

    private decimal GetHydrosphereAtmosphereRatio() => Math.Min(1, (decimal)(Hydrosphere.Mass / Atmosphere.Material.Mass));

    private IEnumerable<IMaterial<HugeNumber>> GetMantle(
        IShape<HugeNumber> planetShape,
        HugeNumber mantleProportion,
        HugeNumber crustProportion,
        HugeNumber planetMass,
        IShape<HugeNumber> coreShape,
        double coreTemp)
    {
        if (PlanetType == PlanetType.RockyDwarf)
        {
            yield break;
        }
        else if (PlanetType == PlanetType.GasGiant)
        {
            foreach (var item in GetMantle_Giant(planetShape, mantleProportion, crustProportion, planetMass, coreShape, coreTemp))
            {
                yield return item;
            }
            yield break;
        }
        else if (PlanetType == PlanetType.IceGiant)
        {
            foreach (var item in GetMantle_IceGiant(planetShape, mantleProportion, crustProportion, planetMass, coreShape, coreTemp))
            {
                yield return item;
            }
            yield break;
        }
        else if (PlanetType == PlanetType.Carbon)
        {
            foreach (var item in GetMantle_Carbon(planetShape, mantleProportion, crustProportion, planetMass, coreShape, coreTemp))
            {
                yield return item;
            }
            yield break;
        }

        var mantleBoundaryDepth = planetShape.ContainingRadius * crustProportion;
        var mantleBoundaryTemp = (double)mantleBoundaryDepth * 1.15;
        var mantleTemp = (mantleBoundaryTemp + coreTemp) / 2;

        var shape = new HollowSphere<HugeNumber>(coreShape.ContainingRadius, planetShape.ContainingRadius * mantleProportion, planetShape.Position);

        var mantleMass = planetMass * mantleProportion;

        yield return new Material<HugeNumber>(
            PlanetType switch
            {
                PlanetType.Dwarf => Substances.All.Water.GetHomogeneousReference(),
                _ => Substances.All.Peridotite.GetReference(),
            },
            shape,
            mantleMass,
            null,
            mantleTemp);
    }

    private HugeNumber GetRadiusForSurfaceGravity(double gravity) => (Mass * HugeNumberConstants.G / gravity).Sqrt();

    /// <summary>
    /// Calculates the approximate outer distance at which rings of the given density may be
    /// found, in meters.
    /// </summary>
    /// <param name="density">The density of the rings, in kg/m³.</param>
    /// <returns>The approximate outer distance at which rings of the given density may be
    /// found, in meters.</returns>
    private HugeNumber GetRingDistance(HugeNumber density)
        => new HugeNumber(126, -2)
        * Shape.ContainingRadius
        * (Material.Density / density).Cbrt();

    private HugeNumber GetSphereOfInfluenceRadius() => Orbit?.GetSphereOfInfluenceRadius(Mass) ?? HugeNumber.Zero;

    /// <summary>
    /// Calculates the surface albedo this <see cref="Planetoid"/> would need in order to have
    /// the given effective temperature at its average distance from the given <paramref
    /// name="star"/> (assuming it is either orbiting the star or not in orbit at all, and that
    /// the current difference between its surface and total albedo remained constant).
    /// </summary>
    /// <remarks>
    /// The effects of other nearby stars are ignored.
    /// </remarks>
    /// <param name="star">
    /// The <see cref="Star"/> for which the calculation is to be made.
    /// </param>
    /// <param name="temperature">The desired temperature, in K.</param>
    private double GetSurfaceAlbedoForTemperature(Star star, double temperature)
    {
        var areaRatio = 1;
        if (RotationalPeriod > 2500)
        {
            if (RotationalPeriod <= 75000)
            {
                areaRatio = 4;
            }
            else if (RotationalPeriod <= 150000)
            {
                areaRatio = 3;
            }
            else if (RotationalPeriod <= 300000)
            {
                areaRatio = 2;
            }
        }

        var averageDistanceSq = Orbit.HasValue
            ? ((Orbit.Value.Apoapsis + Orbit.Value.Periapsis) / 2).Square()
            : Position.DistanceSquared(star.Position);

        var albedo = 1 - (averageDistanceSq
            * Math.Pow(temperature - Temperature, 4)
            * DoubleConstants.FourPi
            * DoubleConstants.sigma
            * areaRatio
            / star.Luminosity);

        var delta = Albedo - _surfaceAlbedo;

        return Math.Max(0, (double)albedo - delta);
    }

    /// <summary>
    /// Calculates the temperature at which this <see cref="Planetoid"/> will retain only
    /// a minimal atmosphere of out-gassed volatiles (comparable to Mercury).
    /// </summary>
    /// <returns>A temperature, in K.</returns>
    /// <remarks>
    /// If the planet is not massive enough or too hot to hold onto carbon dioxide gas, it is
    /// presumed that it will have a minimal atmosphere of out-gassed volatiles (comparable to Mercury).
    /// </remarks>
    private double GetTempForThinAtmosphere()
        => (double)(HugeNumberConstants.TwoG * Mass * new HugeNumber(70594833834763, -18) / Shape.ContainingRadius);
}
