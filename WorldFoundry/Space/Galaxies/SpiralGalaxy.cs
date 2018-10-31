using MathAndScience.Shapes;
using Substances;
using MathAndScience.Numerics;
using WorldFoundry.Substances;

namespace WorldFoundry.Space.Galaxies
{
    /// <summary>
    /// A spiral-shaped, gravitationally-bound collection of stars, gas, dust, and dark matter.
    /// </summary>
    public class SpiralGalaxy : Galaxy
    {
        private protected override string BaseTypeName => "Spiral Galaxy";

        /// <summary>
        /// Initializes a new instance of <see cref="SpiralGalaxy"/>.
        /// </summary>
        internal SpiralGalaxy() { }

        /// <summary>
        /// Initializes a new instance of <see cref="SpiralGalaxy"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="SpiralGalaxy"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="SpiralGalaxy"/>.</param>
        internal SpiralGalaxy(CelestialRegion parent, Vector3 position) : base(parent, position) { }

        private protected override void GenerateSubstance()
        {
            Substance = new Substance { Composition = CosmicSubstances.InterstellarMedium.GetDeepCopy() };

            var radius = Randomizer.Instance.NextDouble(2.4e20, 2.5e21); // 25000–75000 ly
            var axis = radius * Randomizer.Instance.Normal(0.02, 0.001);
            Shape = new Ellipsoid(radius, axis);

            Substance.Mass = GenerateMass(Shape);
        }
    }
}
