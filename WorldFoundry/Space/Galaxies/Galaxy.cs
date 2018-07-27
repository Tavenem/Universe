using MathAndScience.MathUtil.Shapes;
using Substances;
using System;
using System.Collections.Generic;
using System.Numerics;
using WorldFoundry.CelestialBodies.BlackHoles;
using WorldFoundry.CelestialBodies.Planetoids.Planets;
using WorldFoundry.CelestialBodies.Planetoids.Planets.GiantPlanets;
using WorldFoundry.CelestialBodies.Planetoids.Planets.TerrestrialPlanets;
using WorldFoundry.CelestialBodies.Stars;
using WorldFoundry.Orbits;
using WorldFoundry.Substances;

namespace WorldFoundry.Space.Galaxies
{
    /// <summary>
    /// A gravitationally-bound collection of stars, gas, dust, and dark matter.
    /// </summary>
    public class Galaxy : CelestialRegion
    {
        private const string _baseTypeName = "Galaxy";
        /// <summary>
        /// The base name for this type of <see cref="CelestialEntity"/>.
        /// </summary>
        public override string BaseTypeName => _baseTypeName;

        private const double _childDensity = 4.0e-50;
        /// <summary>
        /// The average number of children within the grid per m³.
        /// </summary>
        public override double ChildDensity => _childDensity;

        internal static IList<(Type type, double proportion, object[] constructorParameters)> _childPossibilities =
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

                // Red dwarfs, 14.37% overall.
                (typeof(StarSystem), 0.1434, new object[]{ typeof(Star), SpectralClass.M, LuminosityClass.V }),
                (typeof(StarSystem), 0.0003, new object[]{ typeof(Star), SpectralClass.M, LuminosityClass.sd }),

                // K-type main sequence stars, 2.3% overall.
                (typeof(StarSystem), 0.0227, new object[]{ typeof(Star), SpectralClass.K, LuminosityClass.V }),
                (typeof(StarSystem), 0.00024, new object[]{ typeof(Star), SpectralClass.K, LuminosityClass.IV }),
                (typeof(StarSystem), 0.00006, new object[]{ typeof(Star), SpectralClass.K, LuminosityClass.sd }),

                // White dwarfs, 1.8%.
                (typeof(StarSystem), 0.018, new object[]{ typeof(WhiteDwarf) }),

                // G-type main sequence stars, 1.45% overall.
                (typeof(StarSystem), 0.01438, new object[]{ typeof(Star), SpectralClass.G, LuminosityClass.V }),
                (typeof(StarSystem), 0.00012, new object[]{ typeof(Star), SpectralClass.G, LuminosityClass.IV }),

                // F-type main sequence stars, 0.57% overall.
                (typeof(StarSystem), 0.0056, new object[]{ typeof(Star), SpectralClass.F, LuminosityClass.V }),
                (typeof(StarSystem), 0.0001, new object[]{ typeof(Star), SpectralClass.F, LuminosityClass.IV }),

                // Neutron stars, 0.14%.
                (typeof(StarSystem), 0.0014, new object[]{ typeof(NeutronStar) }),

                // A-type main sequence stars, 0.115%.
                (typeof(StarSystem), 0.00115, new object[]{ typeof(Star), SpectralClass.A, LuminosityClass.V }),

                // Red giants, 0.1% overall.
                (typeof(StarSystem), 0.00096, new object[]{ typeof(RedGiant) }),
                (typeof(StarSystem), 0.000018, new object[]{ typeof(RedGiant), null, LuminosityClass.II }),
                (typeof(StarSystem), 0.000016, new object[]{ typeof(RedGiant), null, LuminosityClass.Ib }),
                (typeof(StarSystem), 0.0000055, new object[]{ typeof(RedGiant), null, LuminosityClass.Ia }),
                (typeof(StarSystem), 0.0000005, new object[]{ typeof(RedGiant), null, LuminosityClass.Zero }),

                // Blue giants, 0.08% overall.
                (typeof(StarSystem), 0.00076, new object[]{ typeof(BlueGiant) }),
                (typeof(StarSystem), 0.00002, new object[]{ typeof(BlueGiant), null, LuminosityClass.II }),
                (typeof(StarSystem), 0.000016, new object[]{ typeof(BlueGiant), null, LuminosityClass.Ib }),
                (typeof(StarSystem), 0.0000036, new object[]{ typeof(BlueGiant), null, LuminosityClass.Ia }),
                (typeof(StarSystem), 0.0000004, new object[]{ typeof(BlueGiant), null, LuminosityClass.Zero }),

                // Yellow giants, 0.04% overall.
                (typeof(StarSystem), 0.00038, new object[]{ typeof(YellowGiant) }),
                (typeof(StarSystem), 0.000008, new object[]{ typeof(YellowGiant), null, LuminosityClass.II }),
                (typeof(StarSystem), 0.0000092, new object[]{ typeof(YellowGiant), null, LuminosityClass.Ib }),
                (typeof(StarSystem), 0.0000024, new object[]{ typeof(YellowGiant), null, LuminosityClass.Ia }),
                (typeof(StarSystem), 0.0000004, new object[]{ typeof(YellowGiant), null, LuminosityClass.Zero }),

                // B-type main sequence stars, 0.02499%.
                (typeof(StarSystem), 0.0002499, new object[]{ typeof(Star), SpectralClass.B, LuminosityClass.V }),

                // Black holes, 0.01%.
                (typeof(BlackHole), 0.0001, null),

                // O-type main sequence stars, 0.00000696%.
                (typeof(StarSystem), 6.96e-8, new object[]{ typeof(Star), SpectralClass.O, LuminosityClass.V }),

                // Planetary nebulae, 0.000003%.
                (typeof(PlanetaryNebula), 3.0e-8, null),

                // Nebulae, 0.00000002%.
                (typeof(PlanetaryNebula), 2.0e-10, null),

                // HII regions, 0.00000002%.
                (typeof(HIIRegion), 2.0e-10, null),
            };
        /// <summary>
        /// The types of children this region of space might have.
        /// </summary>
        public override IList<(Type type, double proportion, object[] constructorParameters)> ChildPossibilities => _childPossibilities;

        private BlackHole _galacticCore;
        /// <summary>
        /// The <see cref="BlackHole"/> which is at the center of this <see cref="Galaxy"/>.
        /// </summary>
        public BlackHole GalacticCore
        {
            get => GetProperty(ref _galacticCore, GenerateGalacticCore);
            protected set => _galacticCore = value;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="Galaxy"/>.
        /// </summary>
        public Galaxy() { }

        /// <summary>
        /// Initializes a new instance of <see cref="Galaxy"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="Galaxy"/> is located.
        /// </param>
        public Galaxy(CelestialRegion parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="Galaxy"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="Galaxy"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="Galaxy"/>.</param>
        public Galaxy(CelestialRegion parent, Vector3 position) : base(parent, position) { }

        /// <summary>
        /// Generates a child of the specified type within this <see cref="CelestialRegion"/>.
        /// </summary>
        /// <param name="type">
        /// The type of child to generate. Does not need to be one of this object's usual child
        /// types, but must be a subclass of <see cref="Orbiter"/>.
        /// </param>
        /// <param name="position">
        /// The location at which to generate the child. If null, a randomly-selected free space will
        /// be selected.
        /// </param>
        /// <param name="constructorParameters">
        /// An optional list of parameters with which to call the child's constructor. May be null.
        /// </param>
        public override Orbiter GenerateChildOfType(Type type, Vector3? position, object[] constructorParameters)
        {
            var child = base.GenerateChildOfType(type, position, constructorParameters);

            Orbit.SetOrbit(
                child,
                GalacticCore,
                Math.Round(Randomizer.Static.NextDouble(0.1), 3));

            // Small chance of satellites for rogue planets.
            if (child is Planemo planemo && Randomizer.Static.NextDouble() <= 0.2)
            {
                planemo.GenerateSatellites();
            }

            return child;
        }

        /// <summary>
        /// Randomly determines a factor by which the mass of this <see cref="Galaxy"/> will be
        /// multiplied due to the abundance of dark matter.
        /// </summary>
        /// <returns>
        /// A factor by which the mass of this <see cref="Galaxy"/> will be multiplied due to the
        /// abundance of dark matter.
        /// </returns>
        private protected virtual double GenerateDarkMatterMultiplier() => Randomizer.Static.NextDouble(5, 15);

        /// <summary>
        /// Generates the central gravitational object of this <see cref="Galaxy"/>, which all others orbit.
        /// </summary>
        private protected virtual void GenerateGalacticCore() => GalacticCore = new SupermassiveBlackHole(this);

        /// <summary>
        /// Produces a rough approximation of the mass of all children, plus the galactic core, plus
        /// an additional high proportion of dark matter.
        /// </summary>
        /// <param name="shape">The shape of the <see cref="Galaxy"/>.</param>
        private protected double GenerateMass(Shape shape) => Math.Round(((shape.Volume * ChildDensity * 1.0e30) + GalacticCore.Mass) * GenerateDarkMatterMultiplier());

        /// <summary>
        /// Generates the <see cref="CelestialEntity.Substance"/> of this <see cref="CelestialEntity"/>.
        /// </summary>
        private protected override void GenerateSubstance()
        {
            Substance = new Substance { Composition = CosmicSubstances.InterstellarMedium.GetDeepCopy() };

            var radius = Randomizer.Static.NextDouble(1.55e19, 1.55e21); // ~1600–160000 ly
            var axis = radius * Randomizer.Static.Normal(0.02, 0.001);
            var shape = new Ellipsoid(radius, axis);

            Substance.Mass = GenerateMass(shape);

            SetShape(shape);
        }
    }
}
