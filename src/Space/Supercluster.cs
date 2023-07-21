using Tavenem.Chemistry;
using Tavenem.Randomize;

namespace Tavenem.Universe.Space;

public partial class CosmicLocation
{
    // ~100000 galaxies total
    private static readonly List<ChildDefinition> _SuperclusterChildDefinitions = new()
    {
        new ChildDefinition(_GalaxyClusterSpace, new HugeNumber(2.563, -77), CosmicStructureType.GalaxyCluster),
        new ChildDefinition(_GalaxyGroupSpace, new HugeNumber(5.126, -77), CosmicStructureType.GalaxyGroup),
    };
    private protected static readonly HugeNumber _SuperclusterSpace = new(9.4607, 25);

    private void ConfigureSuperclusterInstance(Vector3<HugeNumber> position, double? ambientTemperature = null)
        => Material = new Material<HugeNumber>(
            Substances.All.IntraclusterMedium,
            GetSuperclusterShape(position),
            Randomizer.Instance.Next(
                new HugeNumber(2, 46),
                new HugeNumber(2, 47)), // General average; 1.0e16–1.0e17 solar masses
            null,
            ambientTemperature ?? UniverseAmbientTemperature);

    private static IShape<HugeNumber> GetSuperclusterShape(Vector3<HugeNumber> position)
    {
        // May be filaments (narrow in two dimensions), or walls/sheets (narrow in one dimension).
        var majorAxis = Randomizer.Instance.Next(
            new HugeNumber(9.4607, 23),
            new HugeNumber(9.4607, 25));
        var minorAxis1 = majorAxis
            * Randomizer.Instance.Next(
                new HugeNumber(2, -2),
                new HugeNumber(15, -2));
        var minorAxis2 = Randomizer.Instance.NextBool()
            ? minorAxis1 // Filament
            : majorAxis // Wall/sheet
                * Randomizer.Instance.Next(
                    new HugeNumber(3, -2),
                    new HugeNumber(8, -2));
        return Randomizer.Instance.Next(6) switch
        {
            0 => new Ellipsoid<HugeNumber>(majorAxis, minorAxis1, minorAxis2, position),
            1 => new Ellipsoid<HugeNumber>(majorAxis, minorAxis2, minorAxis1, position),
            2 => new Ellipsoid<HugeNumber>(minorAxis1, majorAxis, minorAxis2, position),
            3 => new Ellipsoid<HugeNumber>(minorAxis2, majorAxis, minorAxis1, position),
            4 => new Ellipsoid<HugeNumber>(minorAxis1, minorAxis2, majorAxis, position),
            _ => new Ellipsoid<HugeNumber>(minorAxis2, minorAxis1, majorAxis, position)
        };
    }
}
