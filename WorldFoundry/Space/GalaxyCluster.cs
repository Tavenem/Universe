using MathAndScience.Numerics;
using MathAndScience.Shapes;
using System.Collections.Generic;
using System.Linq;

namespace WorldFoundry.Space
{
    /// <summary>
    /// A large structure of gravitationally-bound galaxies.
    /// </summary>
    public class GalaxyCluster : CelestialRegion
    {
        internal const double Space = 1.5e24;

        private static readonly List<ChildDefinition> _childDefinitions = new List<ChildDefinition>
        {
            new ChildDefinition(typeof(GalaxyGroup), GalaxyGroup.Space, 1.8e-70),
        };

        private protected override string BaseTypeName => "Galaxy Cluster";

        private protected override IEnumerable<ChildDefinition> ChildDefinitions
            => base.ChildDefinitions.Concat(_childDefinitions);

        /// <summary>
        /// Initializes a new instance of <see cref="GalaxyCluster"/>.
        /// </summary>
        internal GalaxyCluster() { }

        /// <summary>
        /// Initializes a new instance of <see cref="GalaxyCluster"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="GalaxyCluster"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="GalaxyCluster"/>.</param>
        internal GalaxyCluster(CelestialRegion parent, Vector3 position) : base(parent, position) { }

        // General average; 1.0e15–1.0e16 solar masses
        private protected override double GetMass() => Randomizer.Instance.NextDouble(2.0e45, 2.0e46);

        private protected override IShape GetShape() => new Sphere(Randomizer.Instance.NextDouble(3.0e23, 1.5e24), Position); // ~1–5 Mpc
    }
}
