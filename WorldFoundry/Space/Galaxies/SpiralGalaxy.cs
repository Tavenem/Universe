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
        private const string _baseTypeName = "Spiral Galaxy";
        /// <summary>
        /// The base name for this type of <see cref="CelestialEntity"/>.
        /// </summary>
        public override string BaseTypeName => _baseTypeName;

        /// <summary>
        /// Initializes a new instance of <see cref="SpiralGalaxy"/>.
        /// </summary>
        public SpiralGalaxy() { }

        /// <summary>
        /// Initializes a new instance of <see cref="SpiralGalaxy"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="SpiralGalaxy"/> is located.
        /// </param>
        public SpiralGalaxy(CelestialRegion parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="SpiralGalaxy"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="SpiralGalaxy"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="SpiralGalaxy"/>.</param>
        public SpiralGalaxy(CelestialRegion parent, Vector3 position) : base(parent, position) { }

        /// <summary>
        /// Generates the <see cref="CelestialEntity.Substance"/> of this <see cref="CelestialEntity"/>.
        /// </summary>
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
