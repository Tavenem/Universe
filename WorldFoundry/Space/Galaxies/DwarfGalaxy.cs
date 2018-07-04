using MathAndScience.MathUtil.Shapes;
using Substances;
using System.Numerics;
using WorldFoundry.CelestialBodies.BlackHoles;
using WorldFoundry.Substances;

namespace WorldFoundry.Space.Galaxies
{
    /// <summary>
    /// A small, gravitationally-bound collection of stars, gas, dust, and dark matter.
    /// </summary>
    public class DwarfGalaxy : Galaxy
    {
        private const string baseTypeName = "Dwarf Galaxy";
        /// <summary>
        /// The base name for this type of <see cref="CelestialEntity"/>.
        /// </summary>
        public override string BaseTypeName => baseTypeName;

        /// <summary>
        /// Initializes a new instance of <see cref="DwarfGalaxy"/>.
        /// </summary>
        public DwarfGalaxy() : base() { }

        /// <summary>
        /// Initializes a new instance of <see cref="DwarfGalaxy"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="DwarfGalaxy"/> is located.
        /// </param>
        public DwarfGalaxy(CelestialRegion parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="DwarfGalaxy"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="DwarfGalaxy"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="DwarfGalaxy"/>.</param>
        public DwarfGalaxy(CelestialRegion parent, Vector3 position) : base(parent, position) { }

        /// <summary>
        /// Generates the central gravitational object of this <see cref="Galaxy"/>, which all others orbit.
        /// </summary>
        /// <remarks>
        /// The cores of dwarf galaxies are ordinary black holes, not super-massive.
        /// </remarks>
        private protected override void GenerateGalacticCore() => GalacticCore = new BlackHole(this);

        /// <summary>
        /// Generates the <see cref="CelestialEntity.Substance"/> of this <see cref="CelestialEntity"/>.
        /// </summary>
        private protected override void GenerateSubstance()
        {
            Substance = new Substance { Composition = CosmicSubstances.InterstellarMedium.GetDeepCopy() };

            var radius = Randomizer.Static.NextDouble(9.5e18, 2.5e18); // ~200–1800 ly
            var axis = radius * Randomizer.Static.Normal(0.02, 1);
            var shape = new Ellipsoid(radius, axis);

            Substance.Mass = GenerateMass(shape);

            SetShape(shape);
        }
    }
}
