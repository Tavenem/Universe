using MathAndScience.Shapes;
using Substances;
using System.Collections.Generic;
using System.Linq;
using MathAndScience.Numerics;
using WorldFoundry.Space.Galaxies;
using WorldFoundry.Substances;

namespace WorldFoundry.Space
{
    /// <summary>
    /// A collection of dwarf galaxies and globular clusters orbiting a large main galaxy.
    /// </summary>
    public class GalaxySubgroup : CelestialRegion
    {
        internal const double Space = 2.5e22;

        private const double ChildDensity = 1.0e-70;

        private static readonly List<ChildDefinition> _childDefinitions = new List<ChildDefinition>
        {
            new ChildDefinition(typeof(DwarfGalaxy), DwarfGalaxy.Space, ChildDensity * 0.26),
            new ChildDefinition(typeof(GlobularCluster), GlobularCluster.Space, ChildDensity * 0.74),
        };

        private Galaxy _mainGalaxy;
        /// <summary>
        /// The main <see cref="Galaxy"/> around which the other objects in this <see
        /// cref="GalaxySubgroup"/> orbit.
        /// </summary>
        public Galaxy MainGalaxy => _mainGalaxy ?? (_mainGalaxy = GetMainGalaxy());

        private protected override string BaseTypeName => "Galaxy Subgroup";

        private protected override IEnumerable<ChildDefinition> ChildDefinitions
            => base.ChildDefinitions.Concat(_childDefinitions);

        /// <summary>
        /// Initializes a new instance of <see cref="GalaxySubgroup"/>.
        /// </summary>
        internal GalaxySubgroup() { }

        /// <summary>
        /// Initializes a new instance of <see cref="GalaxySubgroup"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="GalaxySubgroup"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="GalaxySubgroup"/>.</param>
        internal GalaxySubgroup(CelestialRegion parent, Vector3 position) : base(parent, position) { }

        internal override ICelestialLocation GenerateChild(ChildDefinition definition)
        {
            var child = base.GenerateChild(definition);

            WorldFoundry.Space.Orbit.SetOrbit(
                child,
                MainGalaxy,
                Randomizer.Instance.NextDouble(0.1));

            return child;
        }

        internal override void PrepopulateRegion()
        {
            if (_isPrepopulated)
            {
                return;
            }
            base.PrepopulateRegion();

            GetMainGalaxy();
        }

        /// <summary>
        /// Randomly determines the main <see cref="Galaxy"/> of this <see cref="GalaxySubgroup"/>,
        /// which all other objects orbit.
        /// </summary>
        /// <remarks>70% of large galaxies are spirals.</remarks>
        private Galaxy GetMainGalaxy()
            => Randomizer.Instance.NextDouble() <= 0.7
                ? new SpiralGalaxy(this, Vector3.Zero)
                : (Galaxy)new EllipticalGalaxy(this, Vector3.Zero);

        // The main galaxy is expected to comprise the bulk of the mass
        private protected override double GetMass() => MainGalaxy.Mass * 1.25;

        private protected override IShape GetShape() => new Sphere(MainGalaxy.Radius * 10, Position);
    }
}
