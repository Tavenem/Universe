using MathAndScience.MathUtil.Shapes;
using System;
using System.Numerics;
using WorldFoundry.Space;

namespace WorldFoundry.CelestialBodies.Stars
{
    /// <summary>
    /// A red giant star.
    /// </summary>
    public class RedGiant : GiantStar
    {
        internal new static string baseTypeName = "Red Giant";
        /// <summary>
        /// The base name for this type of <see cref="CelestialEntity"/>.
        /// </summary>
        public override string BaseTypeName => baseTypeName;

        /// <summary>
        /// Initializes a new instance of <see cref="RedGiant"/>.
        /// </summary>
        public RedGiant() : base() { }

        /// <summary>
        /// Initializes a new instance of <see cref="RedGiant"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="RedGiant"/> is located.
        /// </param>
        public RedGiant(CelestialRegion parent) : base(parent) { }

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
        public RedGiant(
            CelestialRegion parent,
            Vector3 position,
            LuminosityClass? luminosityClass = null,
            bool populationII = false) : base(parent, position, luminosityClass, populationII) { }

        /// <summary>
        /// Generates the mass of this <see cref="Star"/>.
        /// </summary>
        private protected override double GenerateMass(Shape shape)
        {
            if (LuminosityClass == LuminosityClass.Zero
                || LuminosityClass == LuminosityClass.Ia
                || LuminosityClass == LuminosityClass.Ib)
            {
                return Randomizer.Static.NextDouble(1.592e31, 4.975e31); // Super/hypergiants
            }
            else
            {
                return Randomizer.Static.NextDouble(5.97e29, 1.592e31); // (Bright)giants
            }
        }

        /// <summary>
        /// Randomly determines a <see cref="SpectralClass"/> for this <see cref="Star"/>.
        /// </summary>
        private protected override void GenerateSpectralClass() => SpectralClass = GetSpectralClassFromTemperature(Temperature ?? 0);

        /// <summary>
        /// Determines a temperature for this <see cref="Star"/>, in K.
        /// </summary>
        private protected override float GenerateTemperature() => (float)Math.Round(Randomizer.Static.Normal(3800, 466));
    }
}
