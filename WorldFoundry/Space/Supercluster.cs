using NeverFoundry.MathAndScience.Chemistry;
using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.MathAndScience.Randomization;
using System.Collections.Generic;

namespace NeverFoundry.WorldFoundry.Space
{
    public partial class CosmicLocation
    {
        // ~100000 galaxies total
        private static readonly List<ChildDefinition> _SuperclusterChildDefinitions = new()
        {
            new ChildDefinition(_GalaxyClusterSpace, new Number(2.563, -77), CosmicStructureType.GalaxyCluster),
            new ChildDefinition(_GalaxyGroupSpace, new Number(5.126, -77), CosmicStructureType.GalaxyGroup),
        };
        private protected static readonly Number _SuperclusterSpace = new(9.4607, 25);

        private void ConfigureSuperclusterInstance(Vector3 position, double? ambientTemperature = null)
        {
            Seed = Randomizer.Instance.NextUIntInclusive();
            ReconstituteSuperclusterInstance(position, ambientTemperature ?? UniverseAmbientTemperature);
        }

        private static IShape GetSuperclusterShape(Vector3 position, Randomizer randomizer)
        {
            // May be filaments (narrow in two dimensions), or walls/sheets (narrow in one dimension).
            var majorAxis = randomizer.NextNumber(new Number(9.4607, 23), new Number(9.4607, 25));
            var minorAxis1 = majorAxis * randomizer.NextNumber(new Number(2, -2), new Number(15, -2));
            Number minorAxis2;
            if (randomizer.NextBool()) // Filament
            {
                minorAxis2 = minorAxis1;
            }
            else // Wall/sheet
            {
                minorAxis2 = majorAxis * randomizer.NextNumber(new Number(3, -2), new Number(8, -2));
            }
            var chance = randomizer.Next(6);
            if (chance == 0)
            {
                return new Ellipsoid(majorAxis, minorAxis1, minorAxis2, position);
            }
            else if (chance == 1)
            {
                return new Ellipsoid(majorAxis, minorAxis2, minorAxis1, position);
            }
            else if (chance == 2)
            {
                return new Ellipsoid(minorAxis1, majorAxis, minorAxis2, position);
            }
            else if (chance == 3)
            {
                return new Ellipsoid(minorAxis2, majorAxis, minorAxis1, position);
            }
            else if (chance == 4)
            {
                return new Ellipsoid(minorAxis1, minorAxis2, majorAxis, position);
            }
            else
            {
                return new Ellipsoid(minorAxis2, minorAxis1, majorAxis, position);
            }
        }

        private void ReconstituteSuperclusterInstance(Vector3 position, double? temperature)
        {
            var randomizer = new Randomizer(Seed);

            var mass = randomizer.NextNumber(new Number(2, 46), new Number(2, 47)); // General average; 1.0e16–1.0e17 solar masses

            var shape = GetSuperclusterShape(position, randomizer);

            Material = new Material(
                Substances.All.IntraclusterMedium.GetReference(),
                mass,
                shape,
                temperature);
        }
    }
}
