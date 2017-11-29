using System;
using System.Numerics;
using WorldFoundry.Space;
using WorldFoundry.Utilities;

namespace WorldFoundry.CelestialBodies.Stars
{
    /// <summary>
    /// A red giant star.
    /// </summary>
    public class RedGiant : GiantStar
    {
        internal new const string baseTypeName = "Red Giant";
        /// <summary>
        /// The base name for this type of <see cref="CelestialEntity"/>.
        /// </summary>
        public override string BaseTypeName => baseTypeName;

        /// <summary>
        /// Initializes a new instance of <see cref="RedGiant"/>.
        /// </summary>
        public RedGiant() { }

        /// <summary>
        /// Initializes a new instance of <see cref="RedGiant"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="RedGiant"/> is located.
        /// </param>
        public RedGiant(CelestialObject parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="RedGiant"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="RedGiant"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="RedGiant"/>.</param>
        /// <param name="luminosityClass">
        /// The <see cref="Stars.LuminosityClass"/> of this <see cref="RedGiant"/>.
        /// </param>
        /// <param name="populationII">Set to true if this is to be a Population II <see cref="RedGiant"/>.</param>
        public RedGiant(
            CelestialObject parent,
            Vector3 position,
            LuminosityClass? luminosityClass = null,
            bool? populationII = null) : base(parent, position, luminosityClass, populationII) { }

        /// <summary>
        /// Generates the <see cref="Mass"/> of this <see cref="Orbiter"/>.
        /// </summary>
        protected override void GenerateMass()
        {
            if (LuminosityClass == LuminosityClass.Zero
                || LuminosityClass == LuminosityClass.Ia
                || LuminosityClass == LuminosityClass.Ib)
            {
                Mass = Randomizer.Static.NextDouble(1.592e31, 4.975e31); // Super/hypergiants
            }

            Mass = Randomizer.Static.NextDouble(5.97e29, 1.592e31); // (Bright)giants
        }

        /// <summary>
        /// Randomly determines a <see cref="SpectralClass"/> for this <see cref="Star"/>.
        /// </summary>
        protected override void GenerateSpectralClass() => SpectralClass = GetSpectralClassFromTemperature(Temperature ?? 0);

        /// <summary>
        /// Determines a temperature for this <see cref="ThermalBody"/>, in K.
        /// </summary>
        protected override void GenerateTemperature() => Temperature = (float)Math.Round(Randomizer.Static.Normal(3800, 466));
    }
}
