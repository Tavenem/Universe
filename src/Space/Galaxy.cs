using Tavenem.Chemistry;
using Tavenem.Randomize;
using Tavenem.Universe.Space.Planetoids;
using Tavenem.Universe.Space.Stars;

namespace Tavenem.Universe.Space;

public partial class CosmicLocation
{
    private protected static readonly HugeNumber _DwarfGalaxySpace = new(2.5, 18);
    private protected static readonly HugeNumber _GalaxySpace = new(2.5, 22);

    private static readonly HugeNumber _GalaxySystemDensity = new(2.75, -73);
    private static readonly HugeNumber _GalaxyRogueDensity = _GalaxySystemDensity * 3;
    private static readonly HugeNumber _GalaxyMainSequenceDensity = _GalaxySystemDensity * new HugeNumber(9096, -4);
    private static readonly HugeNumber _GalaxyRedDensity = _GalaxyMainSequenceDensity * new HugeNumber(7645, -4);
    private static readonly HugeNumber _GalaxyKDensity = _GalaxyMainSequenceDensity * new HugeNumber(121, -3);
    private static readonly HugeNumber _GalaxyGDensity = _GalaxyMainSequenceDensity * new HugeNumber(76, -3);
    private static readonly HugeNumber _GalaxyFDensity = _GalaxyMainSequenceDensity * new HugeNumber(3, -2);
    private static readonly HugeNumber _GalaxyGiantDensity = _GalaxySystemDensity * new HugeNumber(5, -2);
    private static readonly HugeNumber _GalaxyRedGiantDensity = _GalaxyGiantDensity * new HugeNumber(45, -3);
    private static readonly HugeNumber _GalaxyBlueGiantDensity = _GalaxyGiantDensity * new HugeNumber(35, -3);
    private static readonly HugeNumber _GalaxyYellowGiantDensity = _GalaxyGiantDensity * new HugeNumber(2, -2);

    private static readonly List<ChildDefinition> _GalaxyChildDefinitions = new()
    {
        new PlanetChildDefinition(_GalaxyRogueDensity * 5 / 12, PlanetType.GasGiant),
        new PlanetChildDefinition(_GalaxyRogueDensity * new HugeNumber(25, -2), PlanetType.IceGiant),
        new PlanetChildDefinition(_GalaxyRogueDensity / 6, PlanetType.Terrestrial),
        new PlanetChildDefinition(_GalaxyRogueDensity / 24, PlanetType.Ocean),
        new PlanetChildDefinition(_GalaxyRogueDensity / 24, PlanetType.Iron),
        new PlanetChildDefinition(_GalaxyRogueDensity / 12, PlanetType.Carbon),

        new StarSystemChildDefinition(_GalaxySystemDensity / 6, StarType.BrownDwarf),

        new StarSystemChildDefinition(_GalaxyRedDensity * new HugeNumber(998, -3), SpectralClass.M, LuminosityClass.V),
        new StarSystemChildDefinition(_GalaxyRedDensity * new HugeNumber(2, -3), SpectralClass.M, LuminosityClass.sd),

        new StarSystemChildDefinition(_GalaxyKDensity * new HugeNumber(987, -3), SpectralClass.K, LuminosityClass.V),
        new StarSystemChildDefinition(_GalaxyKDensity * new HugeNumber(1, -2), SpectralClass.K, LuminosityClass.IV),
        new StarSystemChildDefinition(_GalaxyKDensity * new HugeNumber(3, -3), SpectralClass.K, LuminosityClass.sd),

        new StarSystemChildDefinition(_GalaxySystemDensity * new HugeNumber(4, -2), StarType.WhiteDwarf),

        new StarSystemChildDefinition(_GalaxyGDensity * new HugeNumber(992, -3), SpectralClass.G, LuminosityClass.V),
        new StarSystemChildDefinition(_GalaxyGDensity * new HugeNumber(8, -3), SpectralClass.G, LuminosityClass.IV),

        new StarSystemChildDefinition(_GalaxyFDensity * new HugeNumber(982, -3), SpectralClass.F, LuminosityClass.V),
        new StarSystemChildDefinition(_GalaxyFDensity * new HugeNumber(18, -3), SpectralClass.F, LuminosityClass.IV),

        new StarSystemChildDefinition(_GalaxySystemDensity * new HugeNumber(4, -4), StarType.Neutron),

        new ChildDefinition(_BlackHoleSpace, _GalaxySystemDensity * new HugeNumber(4, -4), CosmicStructureType.BlackHole),

        new StarSystemChildDefinition(_GalaxyMainSequenceDensity * new HugeNumber(6, -3), SpectralClass.A, LuminosityClass.V),

        new StarSystemChildDefinition(_GalaxyRedGiantDensity * new HugeNumber(96, -2), StarType.RedGiant),
        new StarSystemChildDefinition(_GalaxyRedGiantDensity * new HugeNumber(18, -3), StarType.RedGiant, null, LuminosityClass.II),
        new StarSystemChildDefinition(_GalaxyRedGiantDensity * new HugeNumber(16, -3), StarType.RedGiant, null, LuminosityClass.Ib),
        new StarSystemChildDefinition(_GalaxyRedGiantDensity * new HugeNumber(55, -4), StarType.RedGiant, null, LuminosityClass.Ia),
        new StarSystemChildDefinition(_GalaxyRedGiantDensity * new HugeNumber(5, -4), StarType.RedGiant, null, LuminosityClass.Zero),

        new StarSystemChildDefinition(_GalaxyBlueGiantDensity * new HugeNumber(95, -2), StarType.BlueGiant),
        new StarSystemChildDefinition(_GalaxyBlueGiantDensity * new HugeNumber(25, -3), StarType.BlueGiant, null, LuminosityClass.II),
        new StarSystemChildDefinition(_GalaxyBlueGiantDensity * new HugeNumber(2, -2), StarType.BlueGiant, null, LuminosityClass.Ib),
        new StarSystemChildDefinition(_GalaxyBlueGiantDensity * new HugeNumber(45, -4), StarType.BlueGiant, null, LuminosityClass.Ia),
        new StarSystemChildDefinition(_GalaxyBlueGiantDensity * new HugeNumber(5, -4), StarType.BlueGiant, null, LuminosityClass.Zero),

        new StarSystemChildDefinition(_GalaxyYellowGiantDensity * new HugeNumber(95, -2), StarType.YellowGiant),
        new StarSystemChildDefinition(_GalaxyYellowGiantDensity * new HugeNumber(2, -2), StarType.YellowGiant, null, LuminosityClass.II),
        new StarSystemChildDefinition(_GalaxyYellowGiantDensity * new HugeNumber(23, -3), StarType.YellowGiant, null, LuminosityClass.Ib),
        new StarSystemChildDefinition(_GalaxyYellowGiantDensity * new HugeNumber(6, -3), StarType.YellowGiant, null, LuminosityClass.Ia),
        new StarSystemChildDefinition(_GalaxyYellowGiantDensity * new HugeNumber(1, -3), StarType.YellowGiant, null, LuminosityClass.Zero),

        new StarSystemChildDefinition(_GalaxyMainSequenceDensity * new HugeNumber(13, -4), SpectralClass.B, LuminosityClass.V),
        new StarSystemChildDefinition(_GalaxyMainSequenceDensity * new HugeNumber(3, -7), SpectralClass.O, LuminosityClass.V),

        new ChildDefinition(_PlanetaryNebulaSpace, _GalaxySystemDensity * new HugeNumber(1.5, -8), CosmicStructureType.PlanetaryNebula),

        new ChildDefinition(_NebulaSpace, _GalaxySystemDensity * new HugeNumber(4, -10), CosmicStructureType.Nebula),

        new ChildDefinition(_NebulaSpace, _GalaxySystemDensity * new HugeNumber(4, -10), CosmicStructureType.HIIRegion),
    };

    private static readonly List<ChildDefinition> _EllipticalGalaxyChildDefinitions = new()
    {
        new PlanetChildDefinition(_GalaxyRogueDensity * 5 / 12, PlanetType.GasGiant),
        new PlanetChildDefinition(_GalaxyRogueDensity * new HugeNumber(25, -2), PlanetType.IceGiant),
        new PlanetChildDefinition(_GalaxyRogueDensity / 6, PlanetType.Terrestrial),
        new PlanetChildDefinition(_GalaxyRogueDensity / 24, PlanetType.Ocean),
        new PlanetChildDefinition(_GalaxyRogueDensity / 24, PlanetType.Iron),
        new PlanetChildDefinition(_GalaxyRogueDensity / 12, PlanetType.Carbon),

        new StarSystemChildDefinition(_GalaxySystemDensity / 6, StarType.BrownDwarf),

        new StarSystemChildDefinition(_GalaxyRedDensity * new HugeNumber(998, -3), SpectralClass.M, LuminosityClass.V),
        new StarSystemChildDefinition(_GalaxyRedDensity * new HugeNumber(2, -3), SpectralClass.M, LuminosityClass.sd),

        new StarSystemChildDefinition(_GalaxyKDensity * new HugeNumber(987, -3), SpectralClass.K, LuminosityClass.V),
        new StarSystemChildDefinition(_GalaxyKDensity * new HugeNumber(1, -2), SpectralClass.K, LuminosityClass.IV),
        new StarSystemChildDefinition(_GalaxyKDensity * new HugeNumber(3, -3), SpectralClass.K, LuminosityClass.sd),

        new StarSystemChildDefinition(_GalaxySystemDensity * new HugeNumber(4, -2), StarType.WhiteDwarf),

        new StarSystemChildDefinition(_GalaxyGDensity * new HugeNumber(992, -3), SpectralClass.G, LuminosityClass.V),
        new StarSystemChildDefinition(_GalaxyGDensity * new HugeNumber(8, -3), SpectralClass.G, LuminosityClass.IV),

        new StarSystemChildDefinition(_GalaxyFDensity * new HugeNumber(982, -3), SpectralClass.F, LuminosityClass.V),
        new StarSystemChildDefinition(_GalaxyFDensity * new HugeNumber(18, -3), SpectralClass.F, LuminosityClass.IV),

        new StarSystemChildDefinition(_GalaxySystemDensity * new HugeNumber(4, -4), StarType.Neutron),

        new ChildDefinition(_BlackHoleSpace, _GalaxySystemDensity * new HugeNumber(4, -4), CosmicStructureType.BlackHole),

        new StarSystemChildDefinition(_GalaxyGiantDensity * new HugeNumber(9997, -4), StarType.RedGiant),
        new StarSystemChildDefinition(_GalaxyGiantDensity * new HugeNumber(3, -4), StarType.RedGiant, null, LuminosityClass.II),

        new ChildDefinition(_PlanetaryNebulaSpace, _GalaxySystemDensity * new HugeNumber(1.5, -8), CosmicStructureType.PlanetaryNebula),
    };

    internal OrbitalParameters GetGalaxyChildOrbit()
        => OrbitalParameters.GetFromEccentricity(Mass, Vector3<HugeNumber>.Zero, Randomizer.Instance.NextDouble(0.1));

    private CosmicLocation? ConfigureGalaxyInstance(
        Vector3<HugeNumber> position,
        double? ambientTemperature = null,
        CosmicLocation? child = null)
    {
        CosmicLocation? newCore = null;
        if (child?.StructureType != CosmicStructureType.BlackHole
            || ((CosmicStructureType.SpiralGalaxy | CosmicStructureType.EllipticalGalaxy).HasFlag(StructureType)
            && child.Mass <= _SupermassiveBlackHoleThreshold))
        {
            newCore = new CosmicLocation(Id, CosmicStructureType.BlackHole);
            newCore.ConfigureBlackHoleInstance(
                Vector3<HugeNumber>.Zero,
                (CosmicStructureType.SpiralGalaxy | CosmicStructureType.EllipticalGalaxy)
                    .HasFlag(StructureType));
        }

        var temperature = ambientTemperature ?? UniverseAmbientTemperature;

        HugeNumber radius;
        HugeNumber axis;
        if (StructureType == CosmicStructureType.SpiralGalaxy)
        {
            radius = Randomizer.Instance.Next(
                new HugeNumber(2.4, 20),
                new HugeNumber(2.5, 21)); // 25000–75000 ly
            axis = radius * Randomizer.Instance.NormalDistributionSample(0.02, 0.001);
        }
        else if (StructureType == CosmicStructureType.EllipticalGalaxy)
        {
            radius = Randomizer.Instance.Next(
                new HugeNumber(1.5, 18),
                new HugeNumber(1.5, 21)); // ~160–160000 ly
            axis = radius * Randomizer.Instance.NormalDistributionSample(0.5, 1);
        }
        else if (StructureType == CosmicStructureType.DwarfGalaxy)
        {
            radius = Randomizer.Instance.Next(
                new HugeNumber(9.5, 18),
                new HugeNumber(2.5, 18)); // ~200–1800 ly
            axis = radius * Randomizer.Instance.NormalDistributionSample(0.02, 1);
        }
        else
        {
            radius = Randomizer.Instance.Next(
                new HugeNumber(1.55, 19),
                new HugeNumber(1.55, 21)); // ~1600–160000 ly
            axis = radius * Randomizer.Instance.NormalDistributionSample(0.02, 0.001);
        }
        var shape = new Ellipsoid<HugeNumber>(radius, axis, position);

        // Randomly determines a factor by which the mass of this galaxy will be
        // multiplied due to the abundance of dark matter.
        var darkMatterMultiplier = Randomizer.Instance.Next(5, 15);

        var coreMass = newCore is null
            ? child!.Mass
            : newCore.Mass;

        var mass = ((shape.Volume * _GalaxySystemDensity * new HugeNumber(1, 30)) + coreMass) * darkMatterMultiplier;

        Material = new Material<HugeNumber>(
            Substances.All.InterstellarMedium,
            shape,
            mass,
            null,
            temperature);

        return newCore;
    }
}
