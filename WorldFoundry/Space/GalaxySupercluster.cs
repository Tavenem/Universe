using MathAndScience.Numerics;
using MathAndScience.Shapes;
using System.Collections.Generic;
using System.Linq;

namespace WorldFoundry.Space
{
    /// <summary>
    /// The largest structure in the universe: a massive collection of galaxy groups and clusters.
    /// </summary>
    public class GalaxySupercluster : CelestialRegion
    {
        internal const double Space = 9.4607e25;

        private const double ChildDensity = 1.0e-73;

        private static readonly List<ChildDefinition> _childDefinitions = new List<ChildDefinition>
        {
            new ChildDefinition(typeof(GalaxyCluster), GalaxyCluster.Space, ChildDensity / 3),
            new ChildDefinition(typeof(GalaxyGroup), GalaxyGroup.Space, ChildDensity * 2 / 3),
        };

        private protected override string BaseTypeName => "Galaxy Supercluster";

        private protected override IEnumerable<ChildDefinition> ChildDefinitions
            => base.ChildDefinitions.Concat(_childDefinitions);

        /// <summary>
        /// Initializes a new instance of <see cref="GalaxySupercluster"/>.
        /// </summary>
        internal GalaxySupercluster() { }

        /// <summary>
        /// Initializes a new instance of <see cref="GalaxySupercluster"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="GalaxySupercluster"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="GalaxySupercluster"/>.</param>
        internal GalaxySupercluster(CelestialRegion parent, Vector3 position) : base(parent, position) { }

        // General average; 1.0e16–1.0e17 solar masses
        private protected override double GetMass() => Randomizer.Instance.NextDouble(2.0e46, 2.0e47);

        private protected override IShape GetShape()
        {
            // May be filaments (narrow in two dimensions), or walls/sheets (narrow in one dimension).
            var majorAxis = Randomizer.Instance.NextDouble(9.4607e23, 9.4607e25);
            var minorAxis1 = majorAxis * Randomizer.Instance.NextDouble(0.02, 0.15);
            double minorAxis2;
            if (Randomizer.Instance.NextBoolean()) // Filament
            {
                minorAxis2 = minorAxis1;
            }
            else // Wall/sheet
            {
                minorAxis2 = majorAxis * Randomizer.Instance.NextDouble(0.3, 0.8);
            }
            var chance = Randomizer.Instance.Next(6);
            if (chance == 0)
            {
                return new Ellipsoid(majorAxis, minorAxis1, minorAxis2, Position);
            }
            else if (chance == 1)
            {
                return new Ellipsoid(majorAxis, minorAxis2, minorAxis1, Position);
            }
            else if (chance == 2)
            {
                return new Ellipsoid(minorAxis1, majorAxis, minorAxis2, Position);
            }
            else if (chance == 3)
            {
                return new Ellipsoid(minorAxis2, majorAxis, minorAxis1, Position);
            }
            else if (chance == 4)
            {
                return new Ellipsoid(minorAxis1, minorAxis2, majorAxis, Position);
            }
            else
            {
                return new Ellipsoid(minorAxis2, minorAxis1, majorAxis, Position);
            }
        }
    }
}
