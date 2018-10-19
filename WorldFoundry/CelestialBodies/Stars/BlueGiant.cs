using MathAndScience.Shapes;
using System;
using MathAndScience.Numerics;
using WorldFoundry.Space;

namespace WorldFoundry.CelestialBodies.Stars
{
    /// <summary>
    /// A blue giant star.
    /// </summary>
    public class BlueGiant : GiantStar
    {
        private const string _baseTypeName = "Blue Giant";
        /// <summary>
        /// The base name for this type of <see cref="CelestialEntity"/>.
        /// </summary>
        public override string BaseTypeName => _baseTypeName;

        /// <summary>
        /// If <see langword="false"/> this type of <see cref="CelestialEntity"/> and its children
        /// cannot support life.
        /// </summary>
        /// <remarks>
        /// <see langword="false"/> for blue giants; although they may have a habitable zone, it is not likely to exist
        /// in the same place long enough for life to develop before the star evolves into another
        /// type, or dies.
        /// </remarks>
        public override bool IsHospitable => false;

        /// <summary>
        /// Initializes a new instance of <see cref="BlueGiant"/>.
        /// </summary>
        public BlueGiant() { }

        /// <summary>
        /// Initializes a new instance of <see cref="BlueGiant"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="BlueGiant"/> is located.
        /// </param>
        public BlueGiant(CelestialRegion parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="BlueGiant"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="BlueGiant"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="BlueGiant"/>.</param>
        /// <param name="luminosityClass">
        /// The <see cref="Stars.LuminosityClass"/> of this <see cref="BlueGiant"/>.
        /// </param>
        /// <param name="populationII">Set to true if this is to be a Population II <see cref="BlueGiant"/>.</param>
        public BlueGiant(
            CelestialRegion parent,
            Vector3 position,
            LuminosityClass? luminosityClass = null,
            bool populationII = false) : base(parent, position, luminosityClass, populationII) { }

        /// <summary>
        /// Generates the mass of this <see cref="Star"/>.
        /// </summary>
        /// <param name="shape">The shape of the <see cref="Star"/>.</param>
        private protected override double GenerateMass(IShape shape)
        {
            if (LuminosityClass == LuminosityClass.Zero) // Hypergiants
            {
                // Maxmium possible mass at the current luminosity.
                var eddingtonLimit = Luminosity / 1.23072e31 * 1.99e30;
                if (eddingtonLimit <= 7.96e31)
                {
                    return eddingtonLimit;
                }
                else
                {
                    return Randomizer.Instance.NextDouble(7.96e31, eddingtonLimit);
                }
            }
            else if (LuminosityClass == LuminosityClass.Ia
                || LuminosityClass == LuminosityClass.Ib)
            {
                return Randomizer.Instance.NextDouble(9.95e30, 2.0895e32); // Supergiants
            }
            else
            {
                return Randomizer.Instance.NextDouble(3.98e30, 1.99e31); // (Bright)giants
            }
        }

        /// <summary>
        /// Randomly determines a <see cref="SpectralClass"/> for this <see cref="Star"/>.
        /// </summary>
        private protected override SpectralClass GetSpectralClass() => GetSpectralClassFromTemperature(Temperature ?? 0);

        /// <summary>
        /// Determines a temperature for this <see cref="Star"/>, in K.
        /// </summary>
        private protected override double GenerateTemperature() => Math.Round(10000 + Math.Abs(Randomizer.Instance.Normal(0, 13333)));
    }
}
