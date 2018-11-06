using MathAndScience.Shapes;
using MathAndScience.Numerics;
using WorldFoundry.CelestialBodies.BlackHoles;

namespace WorldFoundry.Space.Galaxies
{
    /// <summary>
    /// A small, gravitationally-bound collection of stars, gas, dust, and dark matter.
    /// </summary>
    public class DwarfGalaxy : Galaxy
    {
        internal const double Space = 2.5e18;

        private protected override string BaseTypeName => "Dwarf Galaxy";

        /// <summary>
        /// Initializes a new instance of <see cref="DwarfGalaxy"/>.
        /// </summary>
        internal DwarfGalaxy() { }

        /// <summary>
        /// Initializes a new instance of <see cref="DwarfGalaxy"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="DwarfGalaxy"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="DwarfGalaxy"/>.</param>
        internal DwarfGalaxy(CelestialRegion parent, Vector3 position) : base(parent, position) { }

        /// <summary>
        /// Generates the central gravitational object of this <see cref="Galaxy"/>, which all others orbit.
        /// </summary>
        /// <remarks>
        /// The cores of dwarf galaxies are ordinary black holes, not super-massive.
        /// </remarks>
        private protected override string GetGalacticCore() => new BlackHole(this, Vector3.Zero).Id;

        private protected override IShape GetShape()
        {
            var radius = Randomizer.Instance.NextDouble(9.5e18, 2.5e18); // ~200–1800 ly
            var axis = radius * Randomizer.Instance.Normal(0.02, 1);
            return new Ellipsoid(radius, axis, Position);
        }
    }
}
