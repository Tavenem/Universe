using System;
using System.Numerics;
using WorldFoundry.Space;
using WorldFoundry.Utilities;

namespace WorldFoundry.CelestialBodies.Stars
{
    /// <summary>
    /// A yellow giant star.
    /// </summary>
    public class YellowGiant : GiantStar
    {
        internal new const string baseTypeName = "Yellow Giant";
        /// <summary>
        /// The base name for this type of <see cref="CelestialEntity"/>.
        /// </summary>
        public override string BaseTypeName => baseTypeName;

        private const float chanceOfLife = 0;
        /// <summary>
        /// The chance that this type of <see cref="BioZone"/> and its children will actually have a
        /// biosphere, if it is habitable.
        /// </summary>
        /// <remarks>
        /// 0 for yellow giants; although they may have a habitable zone, it is not likely to exist
        /// in the same place long enough for life to develop before the star evolves into another
        /// type, or dies.
        /// </remarks>
        public override float? ChanceOfLife => chanceOfLife;

        /// <summary>
        /// Initializes a new instance of <see cref="YellowGiant"/>.
        /// </summary>
        public YellowGiant() { }

        /// <summary>
        /// Initializes a new instance of <see cref="YellowGiant"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="YellowGiant"/> is located.
        /// </param>
        public YellowGiant(CelestialObject parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="YellowGiant"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="YellowGiant"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="YellowGiant"/>.</param>
        /// <param name="luminosityClass">
        /// The <see cref="Stars.LuminosityClass"/> of this <see cref="YellowGiant"/>.
        /// </param>
        /// <param name="populationII">Set to true if this is to be a Population II <see cref="YellowGiant"/>.</param>
        public YellowGiant(
            CelestialObject parent,
            Vector3 position,
            LuminosityClass? luminosityClass = null,
            bool populationII = false) : base(parent, position, luminosityClass, populationII) { }

        /// <summary>
        /// Generates the <see cref="Mass"/> of this <see cref="Orbiter"/>.
        /// </summary>
        protected override void GenerateMass()
        {
            if (LuminosityClass == LuminosityClass.Zero)
            {
                Mass = Randomizer.Static.NextDouble(1.0e31, 8.96e31); // Hypergiants
            }
            else if (LuminosityClass == LuminosityClass.Ia
                || LuminosityClass == LuminosityClass.Ib)
            {
                Mass = Randomizer.Static.NextDouble(5.97e31, 6.97e31); // Supergiants
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
        protected override void GenerateTemperature() => Temperature = (float)Math.Round(Randomizer.Static.Normal(7600, 800));
    }
}
