using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Numerics;
using WorldFoundry.CelestialBodies.BlackHoles;
using WorldFoundry.CelestialBodies.Planetoids.Planets.GiantPlanets;
using WorldFoundry.CelestialBodies.Planetoids.Planets.TerrestrialPlanets;
using WorldFoundry.CelestialBodies.Stars;
using WorldFoundry.Utilities;
using WorldFoundry.Utilities.MathUtil.Shapes;

namespace WorldFoundry.Space.Galaxies
{
    /// <summary>
    /// An elliptical, gravitationally-bound collection of stars, gas, dust, and dark matter.
    /// </summary>
    public class EllipticalGalaxy : Galaxy
    {
        internal new static string baseTypeName = "Elliptical Galaxy";
        /// <summary>
        /// The base name for this type of <see cref="CelestialEntity"/>.
        /// </summary>
        public override string BaseTypeName => baseTypeName;

        internal new static IDictionary<Type, (float proportion, object[] constructorParameters)> childPossibilities =
            new Dictionary<Type, (float proportion, object[] constructorParameters)>
            {
                // Rogue planets, 60% overall.
                { typeof(GiantPlanet), (0.25f, null) },
                { typeof(IceGiant), (0.15f, null) },
                { typeof(TerrestrialPlanet), (0.1f, null) },
                { typeof(IronPlanet), (0.025f, null) },
                { typeof(CarbonPlanet), (0.025f, null) },
                { typeof(OceanPlanet), (0.05f, null) },

                // Brown dwarfs, 19%.
                { typeof(StarSystem), (0.19f, new object[]{ typeof(BrownDwarf) }) },

                // Red dwarfs, 14.44% overall.
                { typeof(StarSystem), (0.1441f, new object[]{ typeof(Star), SpectralClass.M, LuminosityClass.V }) },
                { typeof(StarSystem), (0.0003f, new object[]{ typeof(Star), SpectralClass.M, LuminosityClass.sd }) },

                // K-type main sequence stars, 2.4% overall.
                { typeof(StarSystem), (0.0237f, new object[]{ typeof(Star), SpectralClass.K, LuminosityClass.V }) },
                { typeof(StarSystem), (0.00024f, new object[]{ typeof(Star), SpectralClass.K, LuminosityClass.IV }) },
                { typeof(StarSystem), (0.00006f, new object[]{ typeof(Star), SpectralClass.K, LuminosityClass.sd }) },

                // White dwarfs, 1.8%.
                { typeof(StarSystem), (0.018f, new object[]{ typeof(WhiteDwarf) }) },

                // G-type main sequence stars, 1.5% overall.
                { typeof(StarSystem), (0.01488f, new object[]{ typeof(Star), SpectralClass.G, LuminosityClass.V }) },
                { typeof(StarSystem), (0.00012f, new object[]{ typeof(Star), SpectralClass.G, LuminosityClass.IV }) },

                // F-type main sequence stars, 0.6% overall.
                { typeof(StarSystem), (0.0059f, new object[]{ typeof(Star), SpectralClass.F, LuminosityClass.V }) },
                { typeof(StarSystem), (0.0001f, new object[]{ typeof(Star), SpectralClass.F, LuminosityClass.IV }) },

                // Neutron stars, 0.14%.
                { typeof(StarSystem), (0.0014f, new object[]{ typeof(NeutronStar) }) },

                // Red giants, 0.109997% overall.
                { typeof(StarSystem), (0.00106997f, new object[]{ typeof(RedGiant) }) },
                { typeof(StarSystem), (0.00003f, new object[]{ typeof(RedGiant), null, LuminosityClass.II }) },

                // Black holes, 0.01%.
                { typeof(BlackHole), (0.0001f, null) },

                // Planetary nebulae, 0.000003%.
                { typeof(PlanetaryNebula), (3.0e-8f, null) },
            };
        /// <summary>
        /// The types of children this region of space might have.
        /// </summary>
        [NotMapped]
        public override IDictionary<Type, (float proportion, object[] constructorParameters)> ChildPossibilities => childPossibilities;

        /// <summary>
        /// Initializes a new instance of <see cref="EllipticalGalaxy"/>.
        /// </summary>
        public EllipticalGalaxy() { }

        /// <summary>
        /// Initializes a new instance of <see cref="EllipticalGalaxy"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="EllipticalGalaxy"/> is located.
        /// </param>
        public EllipticalGalaxy(CelestialObject parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="EllipticalGalaxy"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="EllipticalGalaxy"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="EllipticalGalaxy"/>.</param>
        public EllipticalGalaxy(CelestialObject parent, Vector3 position) : base(parent, position) { }

        /// <summary>
        /// Generates the <see cref="Shape"/> of this <see cref="CelestialEntity"/>.
        /// </summary>
        private protected override void GenerateShape()
        {
            var radius = Randomizer.Static.NextDouble(1.5e18, 1.5e21); // ~160–160000 ly
            var axis = radius * Randomizer.Static.Normal(0.5, 1);
            Shape = new Ellipsoid(radius, axis);
        }
    }
}
