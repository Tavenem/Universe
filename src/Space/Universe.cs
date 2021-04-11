using System.Collections.Generic;
using Tavenem.Chemistry;
using Tavenem.HugeNumbers;
using Tavenem.Mathematics.HugeNumbers;

namespace Tavenem.Universe.Space
{
    public partial class CosmicLocation
    {
        private protected const double UniverseAmbientTemperature = 2.73;

        private static readonly List<ChildDefinition> _UniverseChildDefinitions = new()
        {
            new ChildDefinition(_SuperclusterSpace, new HugeNumber(5.8, -26), CosmicStructureType.Supercluster),
        };
        private static readonly Sphere _UniverseShape = new(new HugeNumber(1.89214, 33));

        private void ConfigureUniverseInstance() => ReconstituteUniverseInstance();

        private void ReconstituteUniverseInstance() => Material = new Material(
            Substances.All.WarmHotIntergalacticMedium.GetReference(),
            HugeNumber.PositiveInfinity,
            _UniverseShape,
            UniverseAmbientTemperature);
    }
}
