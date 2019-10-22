using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using WorldFoundry.Place;
using WorldFoundry.Space;
using NeverFoundry.MathAndScience.Chemistry;
using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.MathAndScience.Randomization;

namespace WorldFoundry.CelestialBodies.Stars
{
    /// <summary>
    /// A yellow giant star.
    /// </summary>
    [Serializable]
    public class YellowGiant : GiantStar
    {
        // False for yellow giants; although they may have a habitable zone, it is not likely to
        // exist in the same place long enough for life to develop before the star evolves into
        // another type, or dies.
        internal override bool IsHospitable => false;

        private protected override string BaseTypeName => "Yellow Giant";

        /// <summary>
        /// Initializes a new instance of <see cref="YellowGiant"/>.
        /// </summary>
        internal YellowGiant() { }

        /// <summary>
        /// Initializes a new instance of <see cref="YellowGiant"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="Location"/> in which this <see cref="YellowGiant"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="YellowGiant"/>.</param>
        /// <param name="luminosityClass">
        /// The <see cref="LuminosityClass"/> of this <see cref="YellowGiant"/>.
        /// </param>
        /// <param name="populationII">Set to true if this is to be a Population II <see cref="YellowGiant"/>.</param>
        internal YellowGiant(
            Location parent,
            Vector3 position,
            LuminosityClass? luminosityClass = null,
            bool populationII = false) : base(parent, position, luminosityClass, populationII) { }

        private protected YellowGiant(
            string id,
            string? name,
            bool isPrepopulated,
            double? luminosity,
            LuminosityClass? luminosityClass,
            bool isPopulationII,
            SpectralClass? spectralClass,
            double? albedo,
            Vector3 velocity,
            Orbit? orbit,
            IMaterial? material,
            List<Location>? children)
            : base(
                id,
                name,
                isPrepopulated,
                luminosity,
                luminosityClass,
                isPopulationII,
                spectralClass,
                albedo,
                velocity,
                orbit,
                material,
                children) { }

        private YellowGiant(SerializationInfo info, StreamingContext context) : this(
            (string)info.GetValue(nameof(Id), typeof(string)),
            (string?)info.GetValue(nameof(Name), typeof(string)),
            (bool)info.GetValue(nameof(_isPrepopulated), typeof(bool)),
            (double?)info.GetValue(nameof(Luminosity), typeof(double?)),
            (LuminosityClass?)info.GetValue(nameof(LuminosityClass), typeof(LuminosityClass?)),
            (bool)info.GetValue(nameof(IsPopulationII), typeof(bool)),
            (SpectralClass?)info.GetValue(nameof(SpectralClass), typeof(SpectralClass?)),
            (double?)info.GetValue(nameof(Albedo), typeof(double?)),
            (Vector3)info.GetValue(nameof(Velocity), typeof(Vector3)),
            (Orbit?)info.GetValue(nameof(Orbit), typeof(Orbit?)),
            (IMaterial?)info.GetValue(nameof(Material), typeof(IMaterial)),
            (List<Location>)info.GetValue(nameof(Children), typeof(List<Location>))) { }

        private protected override Number GetMass()
        {
            if (LuminosityClass == LuminosityClass.Zero)
            {
                return Randomizer.Instance.NextNumber(new Number(1, 31), new Number(8.96, 31)); // Hypergiants
            }
            else if (LuminosityClass == LuminosityClass.Ia
                || LuminosityClass == LuminosityClass.Ib)
            {
                return Randomizer.Instance.NextNumber(new Number(5.97, 31), new Number(6.97, 31)); // Supergiants
            }
            else
            {
                return Randomizer.Instance.NextNumber(new Number(5.97, 29), new Number(1.592, 31)); // (Bright)giants
            }
        }

        private protected override SpectralClass GetSpectralClass() => GetSpectralClassFromTemperature(Temperature ?? 0);

        private protected override double? GetTemperature()
            => Randomizer.Instance.NormalDistributionSample(7600, 800);
    }
}
