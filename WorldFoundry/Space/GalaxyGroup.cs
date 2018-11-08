using MathAndScience.Numerics;
using MathAndScience.Shapes;
using System.Collections.Generic;
using System.Linq;
using WorldFoundry.Space.Galaxies;

namespace WorldFoundry.Space
{
    /// <summary>
    /// A collection of gravitationally-bound galaxies, mostly small dwarfs orbiting a few large galaxies.
    /// </summary>
    public class GalaxyGroup : CelestialRegion
    {
        internal const double Space = 3.0e23;

        private static readonly List<ChildDefinition> _childDefinitions = new List<ChildDefinition>
        {
            new ChildDefinition(typeof(DwarfGalaxy), DwarfGalaxy.Space, 1.5e-70),
        };

        private protected override string BaseTypeName => "Galaxy Group";

        private protected override IEnumerable<ChildDefinition> ChildDefinitions
            => base.ChildDefinitions.Concat(_childDefinitions);

        /// <summary>
        /// Initializes a new instance of <see cref="GalaxyGroup"/>.
        /// </summary>
        internal GalaxyGroup() { }

        /// <summary>
        /// Initializes a new instance of <see cref="GalaxyGroup"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="GalaxyGroup"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="GalaxyGroup"/>.</param>
        internal GalaxyGroup(CelestialRegion parent, Vector3 position) : base(parent, position) { }

        internal override void PrepopulateRegion()
        {
            if (_isPrepopulated)
            {
                return;
            }
            base.PrepopulateRegion();

            var amount = Randomizer.Instance.Next(1, 6);
            Vector3 position;
            for (var i = 0; i < amount; i++)
            {
                if (TryGetOpenSpace(GalaxySubgroup.Space, out var location))
                {
                    position = location;
                }
                else
                {
                    break;
                }

                var group = new GalaxySubgroup(this, position);
                group.Init();
            }
        }

        // General average; 1.0e14 solar masses
        private protected override double GetMass() => 2.0e44;

        private protected override IShape GetShape() => new Sphere(Randomizer.Instance.NextDouble(1.5e23, 3.0e23), Position); // ~500–1000 kpc
    }
}
