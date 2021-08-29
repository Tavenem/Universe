using Tavenem.Chemistry;

namespace Tavenem.Universe.Space;

public partial class CosmicLocation
{
    private protected const double UniverseAmbientTemperature = 2.73;

    private static readonly List<ChildDefinition> _UniverseChildDefinitions = new()
    {
        new ChildDefinition(_SuperclusterSpace, new HugeNumber(5.8, -26), CosmicStructureType.Supercluster),
    };
    private static readonly Sphere<HugeNumber> _UniverseShape = new(new HugeNumber(1.89214, 33));

    private void ConfigureUniverseInstance() => ReconstituteUniverseInstance();

    private void ReconstituteUniverseInstance() => Material = new Material<HugeNumber>(
        Substances.All.WarmHotIntergalacticMedium,
        _UniverseShape,
        HugeNumber.PositiveInfinity,
        null,
        UniverseAmbientTemperature);
}
