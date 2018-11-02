using System;
using MathAndScience.Numerics;
using WorldFoundry.Space;

namespace WorldFoundry.CelestialBodies.Stars
{
    /// <summary>
    /// Base class for the giant stars.
    /// </summary>
    public class GiantStar : Star
    {
        /// <summary>
        /// Initializes a new instance of <see cref="GiantStar"/>.
        /// </summary>
        internal GiantStar() { }

        /// <summary>
        /// Initializes a new instance of <see cref="GiantStar"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="GiantStar"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="GiantStar"/>.</param>
        /// <param name="luminosityClass">
        /// The <see cref="LuminosityClass"/> of this <see cref="GiantStar"/>.
        /// </param>
        /// <param name="populationII">Set to true if this is to be a Population II <see cref="GiantStar"/>.</param>
        internal GiantStar(
            CelestialRegion parent,
            Vector3 position,
            LuminosityClass? luminosityClass = null,
            bool populationII = false) : base(parent, position, null, luminosityClass, populationII) { }

        private protected override double GetLuminosity()
        {
            switch (LuminosityClass)
            {
                case LuminosityClass.Zero:
                    return 3.846e31 + Math.Abs(Randomizer.Instance.Normal(0, 3.0768e32));
                case LuminosityClass.Ia:
                    return Randomizer.Instance.Normal(1.923e31, 3.846e29);
                case LuminosityClass.Ib:
                    return Randomizer.Instance.Normal(3.846e30, 3.846e29);
                case LuminosityClass.II:
                    return Randomizer.Instance.Normal(3.846e29, 2.3076e29);
                case LuminosityClass.III:
                    return Randomizer.Instance.Normal(1.5384e29, 4.9998e28);
                default:
                    return 0;
            }
        }

        private protected override LuminosityClass GetLuminosityClass()
        {
            if (Randomizer.Instance.NextDouble() <= 0.05)
            {
                var chance = Randomizer.Instance.NextDouble();
                if (chance <= 0.01)
                {
                    return LuminosityClass.Zero; // 0.05% overall
                }
                else if (chance <= 0.14)
                {
                    return LuminosityClass.Ia; // 0.65% overall
                }
                else if (chance <= 0.50)
                {
                    return LuminosityClass.Ib; // 1.8% overall
                }
                else
                {
                    return LuminosityClass.II; // 2.5% overall
                }
            }
            else
            {
                return LuminosityClass.III;
            }
        }
    }
}
