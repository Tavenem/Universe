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
    /// Base class for the giant stars.
    /// </summary>
    [Serializable]
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
        /// The containing <see cref="Location"/> in which this <see cref="GiantStar"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="GiantStar"/>.</param>
        /// <param name="luminosityClass">
        /// The <see cref="LuminosityClass"/> of this <see cref="GiantStar"/>.
        /// </param>
        /// <param name="populationII">Set to true if this is to be a Population II <see cref="GiantStar"/>.</param>
        internal GiantStar(
            Location parent,
            Vector3 position,
            LuminosityClass? luminosityClass = null,
            bool populationII = false) : base(parent, position, null, luminosityClass, populationII) { }

        private protected GiantStar(
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

        private GiantStar(SerializationInfo info, StreamingContext context) : this(
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

        private protected override double GetLuminosity(Number? temperature = null) => LuminosityClass switch
        {
            LuminosityClass.Zero => 3.846e31 + Randomizer.Instance.PositiveNormalDistributionSample(0, 3.0768e32),
            LuminosityClass.Ia => Randomizer.Instance.NormalDistributionSample(1.923e31, 3.846e29),
            LuminosityClass.Ib => Randomizer.Instance.NormalDistributionSample(3.846e30, 3.846e29),
            LuminosityClass.II => Randomizer.Instance.NormalDistributionSample(3.846e29, 2.3076e29),
            LuminosityClass.III => Randomizer.Instance.NormalDistributionSample(1.5384e29, 4.9998e28),
            _ => 0,
        };

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

        private protected override IMaterial GetMaterial()
        {
            var temperature = GetTemperature();
            var shape = GetShape(temperature);
            var mass = GetMass();

            return GetComposition((double)(mass / shape.Volume), mass, shape, temperature);
        }
    }
}
