using MathAndScience.Shapes;
using System;
using MathAndScience.Numerics;
using WorldFoundry.Space;

namespace WorldFoundry.CelestialBodies.Stars
{
    /// <summary>
    /// A yellow giant star.
    /// </summary>
    public class YellowGiant : GiantStar
    {
        private protected override string BaseTypeName => "Yellow Giant";

        // False for yellow giants; although they may have a habitable zone, it is not likely to
        // exist in the same place long enough for life to develop before the star evolves into
        // another type, or dies.
        private protected override bool IsHospitable => false;

        /// <summary>
        /// Initializes a new instance of <see cref="YellowGiant"/>.
        /// </summary>
        internal YellowGiant() { }

        /// <summary>
        /// Initializes a new instance of <see cref="YellowGiant"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="YellowGiant"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="YellowGiant"/>.</param>
        /// <param name="luminosityClass">
        /// The <see cref="Stars.LuminosityClass"/> of this <see cref="YellowGiant"/>.
        /// </param>
        /// <param name="populationII">Set to true if this is to be a Population II <see cref="YellowGiant"/>.</param>
        internal YellowGiant(
            CelestialRegion parent,
            Vector3 position,
            LuminosityClass? luminosityClass = null,
            bool populationII = false) : base(parent, position, luminosityClass, populationII) { }

        private protected override double GenerateMass(IShape shape)
        {
            if (LuminosityClass == LuminosityClass.Zero)
            {
                return Randomizer.Instance.NextDouble(1.0e31, 8.96e31); // Hypergiants
            }
            else if (LuminosityClass == LuminosityClass.Ia
                || LuminosityClass == LuminosityClass.Ib)
            {
                return Randomizer.Instance.NextDouble(5.97e31, 6.97e31); // Supergiants
            }
            else
            {
                return Randomizer.Instance.NextDouble(5.97e29, 1.592e31); // (Bright)giants
            }
        }

        private protected override SpectralClass GetSpectralClass() => GetSpectralClassFromTemperature(Temperature ?? 0);

        private protected override double GenerateTemperature() => Math.Round(Randomizer.Instance.Normal(7600, 800));
    }
}
