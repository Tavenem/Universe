using Tavenem.Chemistry;

namespace Tavenem.Universe.Space;

public partial class CosmicLocation
{
    /// <summary>
    /// The ambient temperature of the universe, in K.
    /// </summary>
    public const double UniverseAmbientTemperature = 2.73;

    /// <summary>
    /// The average density of the universe, in kg/m³.
    /// </summary>
    public const double UniverseDensity = 5e-27;

    private static readonly List<ChildDefinition> _UniverseChildDefinitions =
    [
        new ChildDefinition(_SuperclusterSpace, new HugeNumber(5.8, -26), CosmicStructureType.Supercluster),
    ];
    private static readonly Sphere<HugeNumber> _UniverseShape = new(new HugeNumber(1.89214, 33));

    private void ConfigureUniverseInstance() => Material = new Material<HugeNumber>(
        Substances.All.WarmHotIntergalacticMedium,
        _UniverseShape,
        HugeNumber.PositiveInfinity,
        UniverseDensity,
        UniverseAmbientTemperature);
}
