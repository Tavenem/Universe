using NeverFoundry.MathAndScience.Chemistry;
using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.MathAndScience.Randomization;
using NeverFoundry.WorldFoundry.Place;
using System.Collections.Generic;

namespace NeverFoundry.WorldFoundry.Space
{
    public partial class CosmicLocation
    {
        private protected static readonly Number _GalaxyGroupMass = new(2, 44); // General average; 1.0e14 solar masses
        private protected static readonly Number _GalaxyGroupSpace = new(3, 23);

        private List<CosmicLocation> ConfigureGalaxyGroupInstance(Vector3 position, double? ambientTemperature = null, CosmicLocation? child = null)
        {
            Seed = Randomizer.Instance.NextUIntInclusive();
            ReconstituteGalaxyGroupInstance(position, ambientTemperature ?? UniverseAmbientTemperature);

            var amount = Randomizer.Instance.Next(1, 6);
            if (child is not null && child.StructureType == CosmicStructureType.GalaxySubgroup)
            {
                amount--;
            }

            var subgroups = new List<Location>();
            var children = new List<CosmicLocation>();
            for (var i = 0; i < amount; i++)
            {
                var location = GetNearestOpenSpace(Vector3.Zero, _GalaxySubgroupSpace, subgroups);
                if (!location.HasValue)
                {
                    break;
                }

                var subgroup = New(CosmicStructureType.GalaxySubgroup, this, location.Value, out var subgroupChildren);
                if (subgroup != null)
                {
                    subgroups.Add(subgroup);
                    children.Add(subgroup);
                    children.AddRange(subgroupChildren);
                }
            }
            return children;
        }

        private void ReconstituteGalaxyGroupInstance(Vector3 position, double? temperature)
        {
            var randomizer = new Randomizer(Seed);

            var radius = randomizer.NextNumber(new Number(1.5, 23), new Number(3, 23)); // ~500–1000 kpc

            Material = new Material(
                Substances.All.IntraclusterMedium.GetReference(),
                _GalaxyGroupMass,
                new Sphere(radius, position),
                temperature);
        }
    }
}
