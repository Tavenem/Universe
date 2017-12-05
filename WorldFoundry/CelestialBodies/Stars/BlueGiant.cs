using System;
using System.Numerics;
using WorldFoundry.Space;
using WorldFoundry.Utilities;

namespace WorldFoundry.CelestialBodies.Stars
{
    /// <summary>
    /// A blue giant star.
    /// </summary>
    public class BlueGiant : GiantStar
    {
        internal new static string baseTypeName = "Blue Giant";
        /// <summary>
        /// The base name for this type of <see cref="CelestialEntity"/>.
        /// </summary>
        public override string BaseTypeName => baseTypeName;

        private static float chanceOfLife = 0;
        /// <summary>
        /// The chance that this type of <see cref="BioZone"/> and its children will actually have a
        /// biosphere, if it is habitable.
        /// </summary>
        /// <remarks>
        /// 0 for blue giants; although they may have a habitable zone, it is not likely to exist
        /// in the same place long enough for life to develop before the star evolves into another
        /// type, or dies.
        /// </remarks>
        public override float? ChanceOfLife => chanceOfLife;

        /// <summary>
        /// Initializes a new instance of <see cref="BlueGiant"/>.
        /// </summary>
        public BlueGiant() { }

        /// <summary>
        /// Initializes a new instance of <see cref="BlueGiant"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="BlueGiant"/> is located.
        /// </param>
        public BlueGiant(CelestialObject parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="BlueGiant"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="BlueGiant"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="BlueGiant"/>.</param>
        /// <param name="luminosityClass">
        /// The <see cref="Stars.LuminosityClass"/> of this <see cref="BlueGiant"/>.
        /// </param>
        /// <param name="populationII">Set to true if this is to be a Population II <see cref="BlueGiant"/>.</param>
        public BlueGiant(
            CelestialObject parent,
            Vector3 position,
            LuminosityClass? luminosityClass = null,
            bool populationII = false) : base(parent, position, luminosityClass, populationII) { }

        /// <summary>
        /// Generates the <see cref="Mass"/> of this <see cref="Orbiter"/>.
        /// </summary>
        protected override void GenerateMass()
        {
            if (LuminosityClass == LuminosityClass.Zero) // Hypergiants
            {
                // Maxmium possible mass at the current luminosity.
                var eddingtonLimit = (Luminosity / 1.23072e31) * 1.99e30;
                if (eddingtonLimit <= 7.96e31)
                {
                    Mass = eddingtonLimit;
                }
                else
                {
                    Mass = Randomizer.Static.NextDouble(7.96e31, eddingtonLimit);
                }
            }
            else if (LuminosityClass == LuminosityClass.Ia
                || LuminosityClass == LuminosityClass.Ib)
            {
                Mass = Randomizer.Static.NextDouble(9.95e30, 2.0895e32); // Supergiants
            }

            Mass = Randomizer.Static.NextDouble(3.98e30, 1.99e31); // (Bright)giants
        }

        /// <summary>
        /// Randomly determines a <see cref="SpectralClass"/> for this <see cref="Star"/>.
        /// </summary>
        protected override void GenerateSpectralClass() => SpectralClass = GetSpectralClassFromTemperature(Temperature ?? 0);

        /// <summary>
        /// Determines a temperature for this <see cref="ThermalBody"/>, in K.
        /// </summary>
        protected override void GenerateTemperature() => Temperature = (float)Math.Round(10000 + Math.Abs(Randomizer.Static.Normal(0, 13333)));
    }
}
