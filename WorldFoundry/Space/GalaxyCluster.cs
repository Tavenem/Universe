using NeverFoundry.MathAndScience.Chemistry;
using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.MathAndScience.Randomization;
using System.Collections.Generic;

namespace NeverFoundry.WorldFoundry.Space
{
    public partial class CosmicLocation
    {
        private static readonly List<ChildDefinition> _GalaxyClusterChildDefinitions = new()
        {
            new ChildDefinition(_GalaxyGroupSpace, new Number(1.415, -72), CosmicStructureType.GalaxyGroup), // ~20 groups = ~1000 galaxies
        };
        private protected static readonly Number _GalaxyClusterSpace = new(1.5, 24);

        private void ConfigureGalaxyClusterInstance(Vector3 position, double? ambientTemperature = null)
        {
            Seed = Randomizer.Instance.NextUIntInclusive();
            ReconstituteGalaxyClusterInstance(position, ambientTemperature ?? UniverseAmbientTemperature);
        }

        private void ReconstituteGalaxyClusterInstance(Vector3 position, double? temperature)
        {
            var randomizer = new Randomizer(Seed);

            var mass = randomizer.NextNumber(new Number(2, 45), new Number(2, 46)); // General average; 1.0e15–1.0e16 solar masses

            var radius = randomizer.NextNumber(new Number(3, 23), new Number(1.5, 24)); // ~1–5 Mpc

            Material = new Material(
                Substances.All.IntraclusterMedium.GetReference(),
                mass,
                new Sphere(radius, position),
                temperature);
        }
    }
}
