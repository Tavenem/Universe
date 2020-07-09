using NeverFoundry.MathAndScience.Chemistry;
using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using System.Collections.Generic;

namespace NeverFoundry.WorldFoundry.Space
{
    public partial class CosmicLocation
    {
        private protected const double UniverseAmbientTemperature = 2.73;

        private static readonly List<ChildDefinition> _UniverseChildDefinitions = new List<ChildDefinition>
        {
            new ChildDefinition(_SuperclusterSpace, new Number(5.8, -26), CosmicStructureType.Supercluster),
        };
        private static readonly Sphere _UniverseShape = new Sphere(new Number(1.89214, 33));

        private void ConfigureUniverseInstance() => ReconstituteUniverseInstance();

        private void ReconstituteUniverseInstance() => Material = new Material(
            Substances.All.WarmHotIntergalacticMedium.GetReference(),
            Number.PositiveInfinity,
            _UniverseShape,
            UniverseAmbientTemperature);
    }
}
