using MathAndScience.Numerics;
using MathAndScience.Shapes;
using Substances;
using System;
using WorldFoundry.CosmicSubstances;
using WorldFoundry.Space;

namespace WorldFoundry.CelestialBodies.BlackHoles
{
    /// <summary>
    /// A gravitational singularity.
    /// </summary>
    public class BlackHole : CelestialBody
    {
        internal const double Space = 60000;

        private protected override string BaseTypeName => "Black Hole";

        /// <summary>
        /// Initializes a new instance of <see cref="BlackHole"/>.
        /// </summary>
        internal BlackHole()  { }

        /// <summary>
        /// Initializes a new instance of <see cref="BlackHole"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="BlackHole"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="BlackHole"/>.</param>
        internal BlackHole(CelestialRegion parent, Vector3 position) : base(parent, position) { }

        private protected virtual double GenerateMass() => Randomizer.Instance.NextDouble(6.0e30, 4.0e31); // ~3–20 solar masses

        private protected override void GenerateSubstance()
        {
            Substance = new Singularity
            {
                Composition = new Material(Chemical.Fuzzball, Phase.Plasma),
                Mass = GenerateMass(),
            };

            // The shape given is presumed to refer to the shape of the event horizon.
            Shape = new Sphere(Math.Round(1.48e-27 * Substance.Mass), Position);
        }
    }
}
