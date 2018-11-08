using MathAndScience.Numerics;
using MathAndScience.Shapes;
using System.Collections.Generic;
using System.Linq;
using WorldFoundry.CelestialBodies.BlackHoles;
using WorldFoundry.CelestialBodies.Stars;

namespace WorldFoundry.Space.Galaxies
{
    /// <summary>
    /// A small, dense collection of stars.
    /// </summary>
    /// <remarks>
    /// Technically these are not galaxies, but they are most easily modeled with the same mechanics,
    /// making it simplest to inherit from the <see cref="Galaxy"/> base class.
    /// </remarks>
    public class GlobularCluster : Galaxy
    {
        internal const double Space = 2.1e7;

        private const double ChildDensity = 1.3e-17;
        private const double RedDensity = ChildDensity * 0.36;
        private const double KDensity = ChildDensity * 0.023;
        private const double GDensity = ChildDensity * 0.037;
        private const double FDensity = ChildDensity * 0.01425;
        private const double RedGiantDensity = ChildDensity * 0.0025;
        private const double BlueGiantDensity = ChildDensity * 0.002;
        private const double YellowGiantDensity = ChildDensity * 0.001;

        private static readonly List<ChildDefinition> _childDefinitions = new List<ChildDefinition>
        {
            new ChildDefinition(typeof(StarSystem), StarSystem.Space, ChildDensity * 0.47, typeof(BrownDwarf)),

            new ChildDefinition(typeof(StarSystem), StarSystem.Space, RedDensity * 0.998, typeof(Star), SpectralClass.M, LuminosityClass.V),
            new ChildDefinition(typeof(StarSystem), StarSystem.Space, RedDensity * 0.002, typeof(Star), SpectralClass.M, LuminosityClass.sd),

            new ChildDefinition(typeof(StarSystem), StarSystem.Space, KDensity * 0.989, typeof(Star), SpectralClass.K, LuminosityClass.V),
            new ChildDefinition(typeof(StarSystem), StarSystem.Space, KDensity * 0.007, typeof(Star), SpectralClass.K, LuminosityClass.IV),
            new ChildDefinition(typeof(StarSystem), StarSystem.Space, KDensity * 0.004, typeof(Star), SpectralClass.K, LuminosityClass.sd),

            new ChildDefinition(typeof(StarSystem), StarSystem.Space, ChildDensity * 0.048, typeof(WhiteDwarf)),

            new ChildDefinition(typeof(StarSystem), StarSystem.Space, GDensity * 0.986, typeof(Star), SpectralClass.G, LuminosityClass.V),
            new ChildDefinition(typeof(StarSystem), StarSystem.Space, GDensity * 0.014, typeof(Star), SpectralClass.G, LuminosityClass.IV),

            new ChildDefinition(typeof(StarSystem), StarSystem.Space, FDensity * 0.982, typeof(Star), SpectralClass.F, LuminosityClass.V),
            new ChildDefinition(typeof(StarSystem), StarSystem.Space, FDensity * 0.018, typeof(Star), SpectralClass.F, LuminosityClass.IV),

            new ChildDefinition(typeof(StarSystem), StarSystem.Space, ChildDensity * 0.0035, typeof(NeutronStar)),

            new ChildDefinition(typeof(StarSystem), StarSystem.Space, ChildDensity * 0.0029, typeof(Star), SpectralClass.A, LuminosityClass.V),

            new ChildDefinition(typeof(StarSystem), StarSystem.Space, RedGiantDensity * 0.96, typeof(RedGiant)),
            new ChildDefinition(typeof(StarSystem), StarSystem.Space, RedGiantDensity * 0.018, typeof(RedGiant), null, LuminosityClass.II),
            new ChildDefinition(typeof(StarSystem), StarSystem.Space, RedGiantDensity * 0.016, typeof(RedGiant), null, LuminosityClass.Ib),
            new ChildDefinition(typeof(StarSystem), StarSystem.Space, RedGiantDensity * 0.0055, typeof(RedGiant), null, LuminosityClass.Ia),
            new ChildDefinition(typeof(StarSystem), StarSystem.Space, RedGiantDensity * 0.0005, typeof(RedGiant), null, LuminosityClass.Zero),

            new ChildDefinition(typeof(StarSystem), StarSystem.Space, BlueGiantDensity * 0.95, typeof(BlueGiant)),
            new ChildDefinition(typeof(StarSystem), StarSystem.Space, BlueGiantDensity * 0.025, typeof(BlueGiant), null, LuminosityClass.II),
            new ChildDefinition(typeof(StarSystem), StarSystem.Space, BlueGiantDensity * 0.02, typeof(BlueGiant), null, LuminosityClass.Ib),
            new ChildDefinition(typeof(StarSystem), StarSystem.Space, BlueGiantDensity * 0.0045, typeof(BlueGiant), null, LuminosityClass.Ia),
            new ChildDefinition(typeof(StarSystem), StarSystem.Space, BlueGiantDensity * 0.0005, typeof(BlueGiant), null, LuminosityClass.Zero),

            new ChildDefinition(typeof(StarSystem), StarSystem.Space, YellowGiantDensity * 0.95, typeof(YellowGiant)),
            new ChildDefinition(typeof(StarSystem), StarSystem.Space, YellowGiantDensity * 0.02, typeof(YellowGiant), null, LuminosityClass.II),
            new ChildDefinition(typeof(StarSystem), StarSystem.Space, YellowGiantDensity * 0.023, typeof(YellowGiant), null, LuminosityClass.Ib),
            new ChildDefinition(typeof(StarSystem), StarSystem.Space, YellowGiantDensity * 0.006, typeof(YellowGiant), null, LuminosityClass.Ia),
            new ChildDefinition(typeof(StarSystem), StarSystem.Space, YellowGiantDensity * 0.001, typeof(YellowGiant), null, LuminosityClass.Zero),

            new ChildDefinition(typeof(StarSystem), StarSystem.Space, ChildDensity * 0.0006, typeof(Star), SpectralClass.B, LuminosityClass.V),

            new ChildDefinition(typeof(BlackHole), BlackHole.Space, ChildDensity * 0.00025),

            new ChildDefinition(typeof(StarSystem), StarSystem.Space, ChildDensity * 1.25e-5, typeof(Star), SpectralClass.O, LuminosityClass.V),
        };

        private protected override string BaseTypeName => "Globular Cluster";

        private protected override IEnumerable<ChildDefinition> ChildDefinitions
            => base.ChildDefinitions.Concat(_childDefinitions);

        /// <summary>
        /// Initializes a new instance of <see cref="GlobularCluster"/>.
        /// </summary>
        internal GlobularCluster() { }

        /// <summary>
        /// Initializes a new instance of <see cref="GlobularCluster"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="GlobularCluster"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="GlobularCluster"/>.</param>
        internal GlobularCluster(CelestialRegion parent, Vector3 position) : base(parent, position) { }

        /// <summary>
        /// Generates the central gravitational object of this <see cref="Galaxy"/>, which all others orbit.
        /// </summary>
        /// <remarks>
        /// The cores of globular clusters are ordinary black holes, not super-massive.
        /// </remarks>
        private protected override string GetGalacticCore()
        {
            var core = new BlackHole(this, Vector3.Zero);
            core.Init();
            return core.Id;
        }

        private protected override IShape GetShape()
        {
            var radius = Randomizer.Instance.NextDouble(8.0e6, 2.1e7);
            var axis = radius * Randomizer.Instance.Normal(0.02, 1);
            return new Ellipsoid(radius, axis, Position);
        }
    }
}
