using Tavenem.Chemistry;
using Tavenem.Randomize;

namespace Tavenem.Universe.Space;

public partial class CosmicLocation
{
    private static readonly List<ChildDefinition> _GalaxyClusterChildDefinitions = new()
    {
        new ChildDefinition(_GalaxyGroupSpace, new HugeNumber(1.415, -72), CosmicStructureType.GalaxyGroup), // ~20 groups = ~1000 galaxies
    };
    private protected static readonly HugeNumber _GalaxyClusterSpace = new(1.5, 24);

    private void ConfigureGalaxyClusterInstance(Vector3<HugeNumber> position, double? ambientTemperature = null)
    {
        Seed = Randomizer.Instance.NextUIntInclusive();
        ReconstituteGalaxyClusterInstance(position, ambientTemperature ?? UniverseAmbientTemperature);
    }

    private void ReconstituteGalaxyClusterInstance(Vector3<HugeNumber> position, double? temperature)
    {
        var randomizer = new Randomizer(Seed);

        var mass = randomizer.Next(new HugeNumber(2, 45), new HugeNumber(2, 46)); // General average; 1.0e15–1.0e16 solar masses

        var radius = randomizer.Next(new HugeNumber(3, 23), new HugeNumber(1.5, 24)); // ~1–5 Mpc

        Material = new Material<HugeNumber>(
            Substances.All.IntraclusterMedium,
            new Sphere<HugeNumber>(radius, position),
            mass,
            null,
            temperature);
    }
}
