using Tavenem.Chemistry;
using Tavenem.Randomize;
using Tavenem.Universe.Space.Stars;

namespace Tavenem.Universe.Space;

public partial class CosmicLocation
{
    private static readonly HugeNumber _HIIRegionChildDensity = new(6, -50);
    private static readonly List<ChildDefinition> _HIIRegionChildDefinitions = new()
    {
        new StarSystemChildDefinition(_HIIRegionChildDensity * new HugeNumber(9998, -4), SpectralClass.B, LuminosityClass.V),
        new StarSystemChildDefinition(_HIIRegionChildDensity * new HugeNumber(2, -4), SpectralClass.O, LuminosityClass.V),
    };
    private protected static readonly HugeNumber _NebulaSpace = new(5.5, 18);

    private void ConfigureNebulaInstance(Vector3<HugeNumber> position, double? ambientTemperature = null)
    {
        // Actual nebulae are irregularly shaped; this is presumed to be a containing shape within
        // which the dust clouds and filaments roughly fit.

        HugeNumber factor;
        if (StructureType == CosmicStructureType.HIIRegion)
        {
            // The radius of an HII region follows a log-normal distribution, with ~20 ly as the
            // mode, starting at ~10 ly, and cutting off around ~600 ly.
            factor = new HugeNumber(1, 17);
        }
        else
        {
            // The radius of other nebulae follows a log-normal distribution, with ~32 ly as the
            // mode, starting at ~16 ly, and cutting off around ~600 ly.
            factor = new HugeNumber(1.5, 17);
        }

        HugeNumber axis;
        do
        {
            axis = factor + (Randomizer.Instance.LogNormalDistributionSample(0, 1) * factor);
        } while (axis > _NebulaSpace);

        Material = new Material<HugeNumber>(
            StructureType == CosmicStructureType.HIIRegion
                ? Substances.All.IonizedCloud
                : Substances.All.MolecularCloud,
            new Ellipsoid<HugeNumber>(
                axis,
                axis * Randomizer.Instance.Next(HugeNumberConstants.Half, new HugeNumber(15, -1)),
                axis * Randomizer.Instance.Next(HugeNumberConstants.Half, new HugeNumber(15, -1)),
                position),
            Randomizer.Instance.Next(
                new HugeNumber(1.99, 33),
                new HugeNumber(1.99, 37)),
            null,
            StructureType == CosmicStructureType.HIIRegion
                ? 10000
                : ambientTemperature ?? UniverseAmbientTemperature);
    }
}
