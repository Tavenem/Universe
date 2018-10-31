using MathAndScience.Shapes;
using System;
using MathAndScience.Numerics;
using WorldFoundry.Space;

namespace WorldFoundry.CelestialBodies.Stars
{
    /// <summary>
    /// A red giant star.
    /// </summary>
    public class RedGiant : GiantStar
    {
        private protected override string BaseTypeName => "Red Giant";

        /// <summary>
        /// Initializes a new instance of <see cref="RedGiant"/>.
        /// </summary>
        internal RedGiant() { }

        /// <summary>
        /// Initializes a new instance of <see cref="RedGiant"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="RedGiant"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="RedGiant"/>.</param>
        /// <param name="luminosityClass">
        /// The <see cref="Stars.LuminosityClass"/> of this <see cref="RedGiant"/>.
        /// </param>
        /// <param name="populationII">Set to true if this is to be a Population II <see cref="RedGiant"/>.</param>
        internal RedGiant(
            CelestialRegion parent,
            Vector3 position,
            LuminosityClass? luminosityClass = null,
            bool populationII = false) : base(parent, position, luminosityClass, populationII) { }

        private protected override double GenerateMass(IShape shape)
        {
            if (LuminosityClass == LuminosityClass.Zero
                || LuminosityClass == LuminosityClass.Ia
                || LuminosityClass == LuminosityClass.Ib)
            {
                return Randomizer.Instance.NextDouble(1.592e31, 4.975e31); // Super/hypergiants
            }
            else
            {
                return Randomizer.Instance.NextDouble(5.97e29, 1.592e31); // (Bright)giants
            }
        }

        private protected override SpectralClass GetSpectralClass() => GetSpectralClassFromTemperature(Temperature ?? 0);

        private protected override double GenerateTemperature() => Math.Round(Randomizer.Instance.Normal(3800, 466));
    }
}
