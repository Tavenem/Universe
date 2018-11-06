using MathAndScience.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using MathAndScience.Numerics;
using WorldFoundry.CelestialBodies.BlackHoles;
using WorldFoundry.CelestialBodies.Planetoids.Planets;
using WorldFoundry.CelestialBodies.Planetoids.Planets.GiantPlanets;
using WorldFoundry.CelestialBodies.Planetoids.Planets.TerrestrialPlanets;
using WorldFoundry.CelestialBodies.Stars;

namespace WorldFoundry.Space.Galaxies
{
    /// <summary>
    /// A gravitationally-bound collection of stars, gas, dust, and dark matter.
    /// </summary>
    public class Galaxy : CelestialRegion
    {
        private const double ChildDensity = 4e-50;
        private const double RogueDensity = ChildDensity * 0.6;
        private const double RedDensity = ChildDensity * 0.1437;
        private const double KDensity = ChildDensity * 0.023;
        private const double GDensity = ChildDensity * 0.0145;
        private const double FDensity = ChildDensity * 0.0057;
        private const double RedGiantDensity = ChildDensity * 0.001;
        private const double BlueGiantDensity = ChildDensity * 0.0008;
        private const double YellowGiantDensity = ChildDensity * 0.0004;

        private static readonly List<ChildDefinition> _childDefinitions = new List<ChildDefinition>
        {
            new ChildDefinition(typeof(GiantPlanet), GiantPlanet.Space, RogueDensity * 5 / 12),
            new ChildDefinition(typeof(IceGiant), GiantPlanet.Space, RogueDensity * 0.25),
            new ChildDefinition(typeof(TerrestrialPlanet), TerrestrialPlanet.Space, RogueDensity / 6),
            new ChildDefinition(typeof(OceanPlanet), TerrestrialPlanet.Space, RogueDensity / 24),
            new ChildDefinition(typeof(IronPlanet), TerrestrialPlanet.Space, RogueDensity / 24),
            new ChildDefinition(typeof(CarbonPlanet), TerrestrialPlanet.Space, RogueDensity / 12),

            new ChildDefinition(typeof(StarSystem), StarSystem.Space, ChildDensity * 0.19, typeof(BrownDwarf)),

            new ChildDefinition(typeof(StarSystem), StarSystem.Space, RedDensity * 0.998, typeof(Star), SpectralClass.M, LuminosityClass.V),
            new ChildDefinition(typeof(StarSystem), StarSystem.Space, RedDensity * 0.002, typeof(Star), SpectralClass.M, LuminosityClass.sd),

            new ChildDefinition(typeof(StarSystem), StarSystem.Space, KDensity * 0.987, typeof(Star), SpectralClass.K, LuminosityClass.V),
            new ChildDefinition(typeof(StarSystem), StarSystem.Space, KDensity * 0.01, typeof(Star), SpectralClass.K, LuminosityClass.IV),
            new ChildDefinition(typeof(StarSystem), StarSystem.Space, KDensity * 0.003, typeof(Star), SpectralClass.K, LuminosityClass.sd),

            new ChildDefinition(typeof(StarSystem), StarSystem.Space, ChildDensity * 0.018, typeof(WhiteDwarf)),

            new ChildDefinition(typeof(StarSystem), StarSystem.Space, GDensity * 0.992, typeof(Star), SpectralClass.G, LuminosityClass.V),
            new ChildDefinition(typeof(StarSystem), StarSystem.Space, GDensity * 0.008, typeof(Star), SpectralClass.G, LuminosityClass.IV),

            new ChildDefinition(typeof(StarSystem), StarSystem.Space, FDensity * 0.982, typeof(Star), SpectralClass.F, LuminosityClass.V),
            new ChildDefinition(typeof(StarSystem), StarSystem.Space, FDensity * 0.018, typeof(Star), SpectralClass.F, LuminosityClass.IV),

            new ChildDefinition(typeof(StarSystem), StarSystem.Space, ChildDensity * 0.0014, typeof(NeutronStar)),

            new ChildDefinition(typeof(StarSystem), StarSystem.Space, ChildDensity * 0.00115, typeof(Star), SpectralClass.A, LuminosityClass.V),

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

            new ChildDefinition(typeof(StarSystem), StarSystem.Space, ChildDensity * 0.00025, typeof(Star), SpectralClass.B, LuminosityClass.V),

            new ChildDefinition(typeof(BlackHole), BlackHole.Space, ChildDensity * 0.0001),

            new ChildDefinition(typeof(StarSystem), StarSystem.Space, ChildDensity * 7e-8, typeof(Star), SpectralClass.O, LuminosityClass.V),

            new ChildDefinition(typeof(PlanetaryNebula), PlanetaryNebula.Space, ChildDensity * 3e-8),

            new ChildDefinition(typeof(Nebula), Nebula.Space, ChildDensity * 2e-10),

            new ChildDefinition(typeof(HIIRegion), Nebula.Space, ChildDensity * 2e-10),
        };

        private string _galacticCore;
        /// <summary>
        /// The <see cref="BlackHole"/> which is at the center of this <see cref="Galaxy"/>.
        /// </summary>
        public BlackHole GalacticCore
        {
            get
            {
                if (_galacticCore == null)
                {
                    _galacticCore = GetGalacticCore();
                }
                return CelestialChildren.OfType<BlackHole>().FirstOrDefault(x => x.Id == _galacticCore);
            }
        }

        private protected override string BaseTypeName => "Galaxy";

        private protected override IEnumerable<ChildDefinition> ChildDefinitions
            => base.ChildDefinitions.Concat(_childDefinitions);

        /// <summary>
        /// Initializes a new instance of <see cref="Galaxy"/>.
        /// </summary>
        internal Galaxy() { }

        /// <summary>
        /// Initializes a new instance of <see cref="Galaxy"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="Galaxy"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="Galaxy"/>.</param>
        internal Galaxy(CelestialRegion parent, Vector3 position) : base(parent, position) { }

        internal override ICelestialLocation GenerateChild(ChildDefinition definition)
        {
            var child = base.GenerateChild(definition);

            Space.Orbit.SetOrbit(
                child,
                GalacticCore,
                Randomizer.Instance.NextDouble(0.1));

            // Small chance of satellites for rogue planets.
            if (child is Planemo planemo && Randomizer.Instance.NextDouble() <= 0.2)
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
        private protected virtual double GenerateDarkMatterMultiplier() => Randomizer.Instance.NextDouble(5, 15);

        /// <summary>
        /// Generates the central gravitational object of this <see cref="Galaxy"/>, which all others orbit.
        /// </summary>
        private protected virtual string GetGalacticCore() => new SupermassiveBlackHole(this, Vector3.Zero).Id;

        // Produces a rough approximation of the mass of all children, plus the galactic core, plus
        // an additional high proportion of dark matter.
        private protected override double GetMass()
            => Math.Round(((Shape.Volume * ChildDensity * 1.0e30) + GalacticCore.Mass) * GenerateDarkMatterMultiplier());

        private protected override IShape GetShape()
        {
            var radius = Randomizer.Instance.NextDouble(1.55e19, 1.55e21); // ~1600–160000 ly
            var axis = radius * Randomizer.Instance.Normal(0.02, 0.001);
            return new Ellipsoid(radius, axis, Position);
        }
    }
}
