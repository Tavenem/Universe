using MathAndScience.Shapes;
using Substances;
using System;
using System.Collections.Generic;
using System.Numerics;
using WorldFoundry.CelestialBodies.BlackHoles;
using WorldFoundry.CelestialBodies.Planetoids.Planets.GiantPlanets;
using WorldFoundry.CelestialBodies.Planetoids.Planets.TerrestrialPlanets;
using WorldFoundry.CelestialBodies.Stars;
using WorldFoundry.Substances;

namespace WorldFoundry.Space.Galaxies
{
    /// <summary>
    /// An elliptical, gravitationally-bound collection of stars, gas, dust, and dark matter.
    /// </summary>
    public class EllipticalGalaxy : Galaxy
    {
        private const string _baseTypeName = "Elliptical Galaxy";
        /// <summary>
        /// The base name for this type of <see cref="CelestialEntity"/>.
        /// </summary>
        public override string BaseTypeName => _baseTypeName;

        internal new static IList<(Type type, double proportion, object[] constructorParameters)> _childPossibilities =
            new List<(Type type, double proportion, object[] constructorParameters)>
            {
                // Rogue planets, 60% overall.
                (typeof(GiantPlanet), 0.25, null),
                (typeof(IceGiant), 0.15, null),
                (typeof(TerrestrialPlanet), 0.1, null),
                (typeof(IronPlanet), 0.025, null),
                (typeof(CarbonPlanet), 0.025, null),
                (typeof(OceanPlanet), 0.05, null),

                // Brown dwarfs, 19%.
                (typeof(StarSystem), 0.19, new object[]{ typeof(BrownDwarf) }),

                // Red dwarfs, 14.44% overall.
                (typeof(StarSystem), 0.1441, new object[]{ typeof(Star), SpectralClass.M, LuminosityClass.V }),
                (typeof(StarSystem), 0.0003, new object[]{ typeof(Star), SpectralClass.M, LuminosityClass.sd }),

                // K-type main sequence stars, 2.4% overall.
                (typeof(StarSystem), 0.0237, new object[]{ typeof(Star), SpectralClass.K, LuminosityClass.V }),
                (typeof(StarSystem), 0.00024, new object[]{ typeof(Star), SpectralClass.K, LuminosityClass.IV }),
                (typeof(StarSystem), 0.00006, new object[]{ typeof(Star), SpectralClass.K, LuminosityClass.sd }),

                // White dwarfs, 1.8%.
                (typeof(StarSystem), 0.018, new object[]{ typeof(WhiteDwarf) }),

                // G-type main sequence stars, 1.5% overall.
                (typeof(StarSystem), 0.01488, new object[]{ typeof(Star), SpectralClass.G, LuminosityClass.V }),
                (typeof(StarSystem), 0.00012, new object[]{ typeof(Star), SpectralClass.G, LuminosityClass.IV }),

                // F-type main sequence stars, 0.6% overall.
                (typeof(StarSystem), 0.0059, new object[]{ typeof(Star), SpectralClass.F, LuminosityClass.V }),
                (typeof(StarSystem), 0.0001, new object[]{ typeof(Star), SpectralClass.F, LuminosityClass.IV }),

                // Neutron stars, 0.14%.
                (typeof(StarSystem), 0.0014, new object[]{ typeof(NeutronStar) }),

                // Red giants, 0.109997% overall.
                (typeof(StarSystem), 0.00106997, new object[]{ typeof(RedGiant) }),
                (typeof(StarSystem), 0.00003, new object[]{ typeof(RedGiant), null, LuminosityClass.II }),

                // Black holes, 0.01%.
                (typeof(BlackHole), 0.0001, null),

                // Planetary nebulae, 0.000003%.
                (typeof(PlanetaryNebula), 3.0e-8, null),
            };
        /// <summary>
        /// The types of children this region of space might have.
        /// </summary>
        public override IList<(Type type, double proportion, object[] constructorParameters)> ChildPossibilities => _childPossibilities;

        /// <summary>
        /// Initializes a new instance of <see cref="EllipticalGalaxy"/>.
        /// </summary>
        public EllipticalGalaxy() { }

        /// <summary>
        /// Initializes a new instance of <see cref="EllipticalGalaxy"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="EllipticalGalaxy"/> is located.
        /// </param>
        public EllipticalGalaxy(CelestialRegion parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="EllipticalGalaxy"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="EllipticalGalaxy"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="EllipticalGalaxy"/>.</param>
        public EllipticalGalaxy(CelestialRegion parent, Vector3 position) : base(parent, position) { }

        /// <summary>
        /// Generates the <see cref="CelestialEntity.Substance"/> of this <see cref="CelestialEntity"/>.
        /// </summary>
        private protected override void GenerateSubstance()
        {
            Substance = new Substance { Composition = CosmicSubstances.InterstellarMedium.GetDeepCopy() };

            var radius = Randomizer.Static.NextDouble(1.5e18, 1.5e21); // ~160–160000 ly
            var axis = radius * Randomizer.Static.Normal(0.5, 1);
            var shape = new Ellipsoid(radius, axis);

            Substance.Mass = GenerateMass(shape);

            SetShape(shape);
        }
    }
}
