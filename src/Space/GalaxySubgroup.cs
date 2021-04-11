using System.Collections.Generic;
using Tavenem.Chemistry;
using Tavenem.HugeNumbers;
using Tavenem.Mathematics.HugeNumbers;
using Tavenem.Randomize;

namespace Tavenem.Universe.Space
{
    public partial class CosmicLocation
    {
        // ~15 dwarf galaxies orbiting a major galaxy; ~50 galaxies total in a group
        private static readonly List<ChildDefinition> _GalaxySubgroupChildDefinitions = new()
        {
            new ChildDefinition(_DwarfGalaxySpace, new HugeNumber(1.25, -69), CosmicStructureType.DwarfGalaxy),
            new ChildDefinition(_GlobularClusterSpace, new HugeNumber(3.75, -69), CosmicStructureType.GlobularCluster), // ~3x
        };
        private protected static readonly HugeNumber _GalaxySubgroupMass = new(3.333, 43); // General average; ~6 in a ~1.0e14 solar mass group
        private protected static readonly HugeNumber _GalaxySubgroupSpace = new(5, 22);

        internal OrbitalParameters? GetGalaxySubgroupChildOrbit() => OrbitalParameters.GetFromEccentricity(
            Mass,
            Vector3.Zero,
            Randomizer.Instance.NextDouble(0.1));

        private CosmicLocation? ConfigureGalaxySubgroupInstance(Vector3 position, out List<CosmicLocation> children, double? ambientTemperature = null, CosmicLocation? child = null)
        {
            Seed = Randomizer.Instance.NextUIntInclusive();
            ReconstituteGalaxySubgroupInstance(position, ambientTemperature ?? UniverseAmbientTemperature);

            if (child is null || !(CosmicStructureType.SpiralGalaxy | CosmicStructureType.EllipticalGalaxy).HasFlag(child.StructureType))
            {
                return Randomizer.Instance.NextDouble() <= 0.7
                   ? New(CosmicStructureType.SpiralGalaxy, this, Vector3.Zero, out children)
                   : New(CosmicStructureType.EllipticalGalaxy, this, Vector3.Zero, out children);
            }

            children = new List<CosmicLocation>();
            return null;
        }

        private void ReconstituteGalaxySubgroupInstance(Vector3 position, double? temperature)
        {
            var randomizer = new Randomizer(Seed);

            var radius = randomizer.NextNumber(new HugeNumber(6.25, 22), new HugeNumber(1.25, 23)); // ~6 in a ~500–1000 kpc group

            Material = new Material(
                Substances.All.IntraclusterMedium.GetReference(),
                _GalaxySubgroupMass,
                new Sphere(radius, position),
                temperature);
        }
    }
}
