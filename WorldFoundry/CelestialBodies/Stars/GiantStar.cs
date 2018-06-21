using System;
using System.Numerics;
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
        public GiantStar() : base() { }

        /// <summary>
        /// Initializes a new instance of <see cref="GiantStar"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="GiantStar"/> is located.
        /// </param>
        public GiantStar(CelestialRegion parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="GiantStar"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="GiantStar"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="GiantStar"/>.</param>
        /// <param name="luminosityClass">
        /// The <see cref="Stars.LuminosityClass"/> of this <see cref="GiantStar"/>.
        /// </param>
        /// <param name="populationII">Set to true if this is to be a Population II <see cref="GiantStar"/>.</param>
        public GiantStar(
            CelestialRegion parent,
            Vector3 position,
            LuminosityClass? luminosityClass = null,
            bool populationII = false) : base(parent, position, null, luminosityClass, populationII) { }

        /// <summary>
        /// Randomly determines a <see cref="Star.Luminosity"/> for this <see cref="Star"/>.
        /// </summary>
        private protected override void GenerateLuminosity()
        {
            switch (LuminosityClass)
            {
                case LuminosityClass.Zero:
                    Luminosity = 3.846e31 + Math.Abs(Randomizer.Static.Normal(0, 3.0768e32));
                    break;
                case LuminosityClass.Ia:
                    Luminosity = Randomizer.Static.Normal(1.923e31, 3.846e29);
                    break;
                case LuminosityClass.Ib:
                    Luminosity = Randomizer.Static.Normal(3.846e30, 3.846e29);
                    break;
                case LuminosityClass.II:
                    Luminosity = Randomizer.Static.Normal(3.846e29, 2.3076e29);
                    break;
                case LuminosityClass.III:
                    Luminosity = Randomizer.Static.Normal(1.5384e29, 4.9998e28);
                    break;
            }
        }

        /// <summary>
        /// Randomly determines a <see cref="LuminosityClass"/> for this <see cref="Star"/>.
        /// </summary>
        private protected override void GenerateLuminosityClass()
        {
            if (Randomizer.Static.NextDouble() <= 0.05)
            {
                var chance = Randomizer.Static.NextDouble();
                if (chance <= 0.01)
                {
                    LuminosityClass = LuminosityClass.Zero; // 0.05% overall
                }
                else if (chance <= 0.14)
                {
                    LuminosityClass = LuminosityClass.Ia; // 0.65% overall
                }
                else if (chance <= 0.50)
                {
                    LuminosityClass = LuminosityClass.Ib; // 1.8% overall
                }
                else
                {
                    LuminosityClass = LuminosityClass.II; // 2.5% overall
                }
            }
            else
            {
                LuminosityClass = LuminosityClass.III;
            }
        }
    }
}
