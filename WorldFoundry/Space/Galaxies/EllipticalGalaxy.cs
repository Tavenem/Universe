using MathAndScience.Shapes;
using Substances;
using System.Collections.Generic;
using System.Linq;
using MathAndScience.Numerics;
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
        private const double ChildDensity = 4e-50;
        private const double RogueDensity = ChildDensity * 0.6;
        private const double RedDensity = ChildDensity * 0.1444;
        private const double KDensity = ChildDensity * 0.024;
        private const double GDensity = ChildDensity * 0.015;
        private const double FDensity = ChildDensity * 0.006;
        private const double RedGiantDensity = ChildDensity * 0.001;

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

            new ChildDefinition(typeof(StarSystem), StarSystem.Space, RedGiantDensity * 0.9997, typeof(RedGiant)),
            new ChildDefinition(typeof(StarSystem), StarSystem.Space, RedGiantDensity * 0.0003, typeof(RedGiant), null, LuminosityClass.II),

            new ChildDefinition(typeof(BlackHole), BlackHole.Space, ChildDensity * 0.0001),

            new ChildDefinition(typeof(PlanetaryNebula), PlanetaryNebula.Space, ChildDensity * 3e-8),
        };

        private const string _baseTypeName = "Elliptical Galaxy";
        /// <summary>
        /// The base name for this type of <see cref="CelestialEntity"/>.
        /// </summary>
        public override string BaseTypeName => _baseTypeName;

        /// <summary>
        /// The types of children found in this region.
        /// </summary>
        public override IEnumerable<ChildDefinition> ChildDefinitions
            => base.ChildDefinitions.Concat(_childDefinitions);

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

            var radius = Randomizer.Instance.NextDouble(1.5e18, 1.5e21); // ~160–160000 ly
            var axis = radius * Randomizer.Instance.Normal(0.5, 1);
            Shape = new Ellipsoid(radius, axis);

            Substance.Mass = GenerateMass(Shape);
        }
    }
}
