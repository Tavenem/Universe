using Tavenem.Chemistry;
using Tavenem.Randomize;

namespace Tavenem.Universe.Space;

public partial class CosmicLocation
{
    // ~15 dwarf galaxies orbiting a major galaxy; ~50 galaxies total in a group
    private static readonly List<ChildDefinition> _GalaxySubgroupChildDefinitions =
    [
        new ChildDefinition(_DwarfGalaxySpace, new HugeNumber(1.25, -69), CosmicStructureType.DwarfGalaxy),
        new ChildDefinition(_GlobularClusterSpace, new HugeNumber(3.75, -69), CosmicStructureType.GlobularCluster), // ~3x
    ];
    private protected static readonly HugeNumber _GalaxySubgroupMass = new(3.333, 43); // General average; ~6 in a ~1.0e14 solar mass group
    private protected static readonly HugeNumber _GalaxySubgroupSpace = new(5, 22);

    internal OrbitalParameters? GetGalaxySubgroupChildOrbit() => OrbitalParameters.GetFromEccentricity(
        Mass,
        Vector3<HugeNumber>.Zero,
        Randomizer.Instance.NextDouble(0.1));

    private CosmicLocation? ConfigureGalaxySubgroupInstance(
        Vector3<HugeNumber> position,
        out List<CosmicLocation> children,
        double? ambientTemperature = null,
        CosmicLocation? child = null)
    {
        Material = new Material<HugeNumber>(
            Substances.All.IntraclusterMedium,
            new Sphere<HugeNumber>(
                Randomizer.Instance.Next(
                    new HugeNumber(6.25, 22),
                    new HugeNumber(1.25, 23)), // ~6 in a ~500–1000 kpc group
                position),
            _GalaxySubgroupMass,
            null,
            ambientTemperature ?? UniverseAmbientTemperature);

        if (child is null
            || !(CosmicStructureType.SpiralGalaxy | CosmicStructureType.EllipticalGalaxy)
                .HasFlag(child.StructureType))
        {
            return Randomizer.Instance.NextDouble() <= 0.7
                ? New(CosmicStructureType.SpiralGalaxy, this, Vector3<HugeNumber>.Zero, out children)
                : New(CosmicStructureType.EllipticalGalaxy, this, Vector3<HugeNumber>.Zero, out children);
        }

        children = [];
        return null;
    }
}
