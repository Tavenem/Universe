using MathAndScience.Shapes;
using Substances;
using System;
using System.Collections.Generic;
using System.Numerics;
using WorldFoundry.CelestialBodies.BlackHoles;
using WorldFoundry.CelestialBodies.Stars;
using WorldFoundry.Substances;

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
        private const string _baseTypeName = "Globular Cluster";
        /// <summary>
        /// The base name for this type of <see cref="CelestialEntity"/>.
        /// </summary>
        public override string BaseTypeName => _baseTypeName;

        private const double _childDensity = 1.3e-17;
        /// <summary>
        /// The average number of children within the grid per m³.
        /// </summary>
        /// <remarks>Globular clusters are far more dense than galaxies.</remarks>
        public override double ChildDensity => _childDensity;

        internal new static IList<(Type type,double proportion, object[] constructorParameters)> _childPossibilities =
            new List<(Type type,double proportion, object[] constructorParameters)>
            {
                // Brown dwarfs, 47%.
                (typeof(StarSystem), 0.47, new object[]{ typeof(BrownDwarf), true }),

                // Red dwarfs, 36% overall.
                (typeof(StarSystem), 0.3592, new object[]{ typeof(Star), SpectralClass.M, LuminosityClass.V, true }),
                (typeof(StarSystem), 0.0008, new object[]{ typeof(Star), SpectralClass.M, LuminosityClass.sd, true }),

                // K-type main sequence stars, 5.8% overall.
                (typeof(StarSystem), 0.05735, new object[]{ typeof(Star), SpectralClass.K, LuminosityClass.V, true }),
                (typeof(StarSystem), 0.0004, new object[]{ typeof(Star), SpectralClass.K, LuminosityClass.IV, true }),
                (typeof(StarSystem), 0.00025, new object[]{ typeof(Star), SpectralClass.K, LuminosityClass.sd, true }),

                // White dwarfs, 4.8%.
                (typeof(StarSystem), 0.048, new object[]{ typeof(WhiteDwarf), true }),

                // G-type main sequence stars, 3.7% overall.
                (typeof(StarSystem), 0.0365, new object[]{ typeof(Star), SpectralClass.G, LuminosityClass.V, true }),
                (typeof(StarSystem), 0.0005, new object[]{ typeof(Star), SpectralClass.G, LuminosityClass.IV, true }),

                // F-type main sequence stars, 1.425% overall.
                (typeof(StarSystem), 0.014, new object[]{ typeof(Star), SpectralClass.F, LuminosityClass.V, true }),
                (typeof(StarSystem), 0.00025, new object[]{ typeof(Star), SpectralClass.F, LuminosityClass.IV, true }),

                // Neutron stars, 0.35%.
                (typeof(StarSystem), 0.0035, new object[]{ typeof(NeutronStar), true }),

                // A-type main sequence stars, 0.29%.
                (typeof(StarSystem), 0.0029, new object[]{ typeof(Star), SpectralClass.A, LuminosityClass.V, true }),

                // Red giants, 0.25% overall.
                (typeof(StarSystem), 0.0024, new object[]{ typeof(RedGiant), true }),
                (typeof(StarSystem), 4.0e-5, new object[]{ typeof(RedGiant), null, LuminosityClass.II, true }),
                (typeof(StarSystem), 4.0e-5, new object[]{ typeof(RedGiant), null, LuminosityClass.Ib, true }),
                (typeof(StarSystem), 1.375e-5, new object[]{ typeof(RedGiant), null, LuminosityClass.Ia, true }),
                (typeof(StarSystem), 1.25e-6, new object[]{ typeof(RedGiant), null, LuminosityClass.Zero, true }),

                // Blue giants, 0.2% overall.
                (typeof(StarSystem), 0.0019, new object[]{ typeof(BlueGiant), true }),
                (typeof(StarSystem), 5.0e-5, new object[]{ typeof(BlueGiant), null, LuminosityClass.II, true }),
                (typeof(StarSystem), 4.0e-5, new object[]{ typeof(BlueGiant), null, LuminosityClass.Ib, true }),
                (typeof(StarSystem), 9.0e-6, new object[]{ typeof(BlueGiant), null, LuminosityClass.Ia, true }),
                (typeof(StarSystem), 1.0e-6, new object[]{ typeof(BlueGiant), null, LuminosityClass.Zero, true }),

                // Yellow giants, 0.1% overall.
                (typeof(StarSystem), 0.00095, new object[]{ typeof(YellowGiant), true }),
                (typeof(StarSystem), 2.0e-5, new object[]{ typeof(YellowGiant), null, LuminosityClass.II, true }),
                (typeof(StarSystem), 2.3e-5, new object[]{ typeof(YellowGiant), null, LuminosityClass.Ib, true }),
                (typeof(StarSystem), 6.0e-6, new object[]{ typeof(YellowGiant), null, LuminosityClass.Ia, true }),
                (typeof(StarSystem), 1.0e-6, new object[]{ typeof(YellowGiant), null, LuminosityClass.Zero, true }),

                // B-type main sequence stars, 0.0599875%.
                (typeof(StarSystem), 0.000599875, new object[]{ typeof(Star), SpectralClass.B, LuminosityClass.V, true }),

                // Black holes, 0.025%.
                (typeof(BlackHole), 0.00025, null),

                // O-type main sequence stars, 1.25e-5%.
                (typeof(StarSystem), 1.25e-5, new object[]{ typeof(Star), SpectralClass.O, LuminosityClass.V, true }),
            };
        /// <summary>
        /// The types of children this region of space might have.
        /// </summary>
        public override IList<(Type type,double proportion, object[] constructorParameters)> ChildPossibilities => _childPossibilities;

        /// <summary>
        /// Initializes a new instance of <see cref="GlobularCluster"/>.
        /// </summary>
        public GlobularCluster() { }

        /// <summary>
        /// Initializes a new instance of <see cref="GlobularCluster"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="GlobularCluster"/> is located.
        /// </param>
        public GlobularCluster(CelestialRegion parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="GlobularCluster"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="GlobularCluster"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="GlobularCluster"/>.</param>
        public GlobularCluster(CelestialRegion parent, Vector3 position) : base(parent, position) { }

        /// <summary>
        /// Generates the central gravitational object of this <see cref="Galaxy"/>, which all others orbit.
        /// </summary>
        /// <remarks>
        /// The cores of globular clusters are ordinary black holes, not super-massive.
        /// </remarks>
        private protected override void GenerateGalacticCore() => GalacticCore = new BlackHole(this);

        /// <summary>
        /// Generates the <see cref="CelestialEntity.Substance"/> of this <see cref="CelestialEntity"/>.
        /// </summary>
        private protected override void GenerateSubstance()
        {
            Substance = new Substance { Composition = CosmicSubstances.InterstellarMedium.GetDeepCopy() };

            var radius = Randomizer.Static.NextDouble(8.0e6, 2.1e7);
            var axis = radius * Randomizer.Static.Normal(0.02, 1);
            var shape = new Ellipsoid(radius, axis);

            Substance.Mass = GenerateMass(shape);

            SetShape(shape);
        }
    }
}
