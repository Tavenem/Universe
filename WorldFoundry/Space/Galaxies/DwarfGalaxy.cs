using System.Numerics;
using WorldFoundry.CelestialBodies.BlackHoles;
using WorldFoundry.Utilities;
using WorldFoundry.Utilities.MathUtil.Shapes;

namespace WorldFoundry.Space.Galaxies
{
    /// <summary>
    /// A small, gravitationally-bound collection of stars, gas, dust, and dark matter.
    /// </summary>
    public class DwarfGalaxy : Galaxy
    {
        internal new static string baseTypeName = "Dwarf Galaxy";
        /// <summary>
        /// The base name for this type of <see cref="CelestialEntity"/>.
        /// </summary>
        public override string BaseTypeName => baseTypeName;

        /// <summary>
        /// Initializes a new instance of <see cref="DwarfGalaxy"/>.
        /// </summary>
        public DwarfGalaxy() { }

        /// <summary>
        /// Initializes a new instance of <see cref="DwarfGalaxy"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="DwarfGalaxy"/> is located.
        /// </param>
        public DwarfGalaxy(CelestialObject parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="DwarfGalaxy"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="DwarfGalaxy"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="DwarfGalaxy"/>.</param>
        public DwarfGalaxy(CelestialObject parent, Vector3 position) : base(parent, position) { }

        /// <summary>
        /// Generates the central gravitational object of this <see cref="Galaxy"/>, which all others orbit.
        /// </summary>
        /// <remarks>
        /// The cores of dwarf galaxies are ordinary black holes, not super-massive.
        /// </remarks>
        private protected override void GenerateGalacticCore() => GalacticCore = new BlackHole(this);

        /// <summary>
        /// Generates the <see cref="Shape"/> of this <see cref="CelestialEntity"/>.
        /// </summary>
        private protected override void GenerateShape()
        {
            var radius = Randomizer.Static.NextDouble(9.5e18, 2.5e18); // ~200–1800 ly
            var axis = radius * Randomizer.Static.Normal(0.02, 1);
            Shape = new Ellipsoid(radius, axis);
        }
    }
}
