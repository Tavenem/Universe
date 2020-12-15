using NeverFoundry.MathAndScience.Chemistry;
using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.MathAndScience.Randomization;
using NeverFoundry.WorldFoundry.Space.Stars;
using System.Collections.Generic;

namespace NeverFoundry.WorldFoundry.Space
{
    public partial class CosmicLocation
    {
        private static readonly Number _HIIRegionChildDensity = new(6, -50);
        private static readonly List<ChildDefinition> _HIIRegionChildDefinitions = new()
        {
            new StarSystemChildDefinition(_HIIRegionChildDensity * new Number(9998, -4), SpectralClass.B, LuminosityClass.V),
            new StarSystemChildDefinition(_HIIRegionChildDensity * new Number(2, -4), SpectralClass.O, LuminosityClass.V),
        };
        private protected static readonly Number _NebulaSpace = new(5.5, 18);

        private void ConfigureNebulaInstance(Vector3 position, double? ambientTemperature = null)
        {
            Seed = Randomizer.Instance.NextUIntInclusive();
            ReconstituteNebulaInstance(
                position,
                StructureType == CosmicStructureType.HIIRegion
                    ? 10000
                    : ambientTemperature ?? UniverseAmbientTemperature);
        }

        private void ReconstituteNebulaInstance(Vector3 position, double? temperature)
        {
            var randomizer = new Randomizer(Seed);

            // Actual nebulae are irregularly shaped; this is presumed to be a containing shape within
            // which the dust clouds and filaments roughly fit.

            Number factor;
            if (StructureType == CosmicStructureType.HIIRegion)
            {
                // The radius of an HII region follows a log-normal distribution, with  ~20 ly as the
                // mode, starting at ~10 ly, and cutting off around ~600 ly.
                factor = new Number(1, 17);
            }
            else
            {
                // The radius of other nebulae follows a log-normal distribution, with ~32 ly as the
                // mode, starting at ~16 ly, and cutting off around ~600 ly.
                factor = new Number(1.5, 17);
            }

            Number axis;
            do
            {
                axis = factor + (randomizer.LogNormalDistributionSample(0, 1) * factor);
            } while (axis > _NebulaSpace);
            var shape = new Ellipsoid(
                axis,
                axis * randomizer.NextNumber(Number.Half, new Number(15, -1)),
                axis * randomizer.NextNumber(Number.Half, new Number(15, -1)),
                position);

            var mass = randomizer.NextNumber(new Number(1.99, 33), new Number(1.99, 37));

            Material = new Material(
                StructureType == CosmicStructureType.HIIRegion
                    ? Substances.All.IonizedCloud.GetReference()
                    : Substances.All.MolecularCloud.GetReference(),
                mass,
                shape,
                temperature);
        }
    }
}
