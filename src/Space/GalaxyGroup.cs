﻿using Tavenem.Chemistry;
using Tavenem.Randomize;
using Tavenem.Universe.Place;

namespace Tavenem.Universe.Space;

public partial class CosmicLocation
{
    private protected static readonly HugeNumber _GalaxyGroupMass = new(2, 44); // General average; 1.0e14 solar masses
    private protected static readonly HugeNumber _GalaxyGroupSpace = new(3, 23);

    private List<CosmicLocation> ConfigureGalaxyGroupInstance(Vector3<HugeNumber> position, double? ambientTemperature = null, CosmicLocation? child = null)
    {
        Seed = Randomizer.Instance.NextUIntInclusive();
        ReconstituteGalaxyGroupInstance(position, ambientTemperature ?? UniverseAmbientTemperature);

        var amount = Randomizer.Instance.Next(1, 6);
        if (child?.StructureType == CosmicStructureType.GalaxySubgroup)
        {
            amount--;
        }

        var subgroups = new List<Location>();
        var children = new List<CosmicLocation>();
        for (var i = 0; i < amount; i++)
        {
            var location = GetNearestOpenSpace(Vector3<HugeNumber>.Zero, _GalaxySubgroupSpace, subgroups);
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

    private void ReconstituteGalaxyGroupInstance(Vector3<HugeNumber> position, double? temperature)
    {
        var randomizer = new Randomizer(Seed);

        var radius = randomizer.Next(new HugeNumber(1.5, 23), new HugeNumber(3, 23)); // ~500–1000 kpc

        Material = new Material<HugeNumber>(
            Substances.All.IntraclusterMedium,
            new Sphere<HugeNumber>(radius, position),
            _GalaxyGroupMass,
            null,
            temperature);
    }
}
