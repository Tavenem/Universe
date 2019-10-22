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
    /// A blue giant star.
    /// </summary>
    [Serializable]
    public class BlueGiant : GiantStar
    {
        private static readonly Number _MinHypergiantMass = new Number(7.96, 31);

        // False for blue giants; although they may have a habitable zone, it is not likely to
        // exist in the same place long enough for life to develop before the star evolves into
        // another type, or dies.
        internal override bool IsHospitable => false;

        private protected override string BaseTypeName => "Blue Giant";

        /// <summary>
        /// Initializes a new instance of <see cref="BlueGiant"/>.
        /// </summary>
        internal BlueGiant() { }

        /// <summary>
        /// Initializes a new instance of <see cref="BlueGiant"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="Location"/> in which this <see cref="BlueGiant"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="BlueGiant"/>.</param>
        /// <param name="luminosityClass">
        /// The <see cref="LuminosityClass"/> of this <see cref="BlueGiant"/>.
        /// </param>
        /// <param name="populationII">Set to true if this is to be a Population II <see cref="BlueGiant"/>.</param>
        internal BlueGiant(
            Location parent,
            Vector3 position,
            LuminosityClass? luminosityClass = null,
            bool populationII = false) : base(parent, position, luminosityClass, populationII) { }

        private protected BlueGiant(
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

        private BlueGiant(SerializationInfo info, StreamingContext context) : this(
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
            if (LuminosityClass == LuminosityClass.Zero) // Hypergiants
            {
                // Maxmium possible mass at the current luminosity.
                var eddingtonLimit = (Number)(Luminosity / 1.23072e31 * 1.99e30);
                if (eddingtonLimit <= _MinHypergiantMass)
                {
                    return eddingtonLimit;
                }
                else
                {
                    return Randomizer.Instance.NextNumber(_MinHypergiantMass, eddingtonLimit);
                }
            }
            else if (LuminosityClass == LuminosityClass.Ia
                || LuminosityClass == LuminosityClass.Ib)
            {
                return Randomizer.Instance.NextNumber(new Number(9.95, 30), new Number(2.0895, 32)); // Supergiants
            }
            else
            {
                return Randomizer.Instance.NextNumber(new Number(3.98, 30), new Number(1.99, 31)); // (Bright)giants
            }
        }

        private protected override SpectralClass GetSpectralClass() => GetSpectralClassFromTemperature(Temperature ?? 0);

        private protected override double? GetTemperature()
            => Randomizer.Instance.PositiveNormalDistributionSample(10000, 13333);
    }
}
