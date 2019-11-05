using NeverFoundry.MathAndScience.Chemistry;
using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.MathAndScience.Randomization;
using NeverFoundry.WorldFoundry.Space;
using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace NeverFoundry.WorldFoundry.CelestialBodies.Stars
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
        /// <param name="parentId">The id of the location which contains this one.</param>
        /// <param name="position">The initial position of this <see cref="BlueGiant"/>.</param>
        internal BlueGiant(string? parentId, Vector3 position) : base(parentId, position) { }

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
            string? parentId)
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
                parentId)
        { }

        private BlueGiant(SerializationInfo info, StreamingContext context) : this(
            (string)info.GetValue(nameof(Id), typeof(string)),
            (string?)info.GetValue(nameof(Name), typeof(string)),
            (bool)info.GetValue(nameof(_isPrepopulated), typeof(bool)),
            (double?)info.GetValue(nameof(Luminosity), typeof(double?)),
            (LuminosityClass?)info.GetValue(nameof(LuminosityClass), typeof(LuminosityClass?)),
            (bool)info.GetValue(nameof(IsPopulationII), typeof(bool)),
            (SpectralClass?)info.GetValue(nameof(SpectralClass), typeof(SpectralClass?)),
            (double?)info.GetValue(nameof(_albedo), typeof(double?)),
            (Vector3)info.GetValue(nameof(Velocity), typeof(Vector3)),
            (Orbit?)info.GetValue(nameof(Orbit), typeof(Orbit?)),
            (IMaterial?)info.GetValue(nameof(_material), typeof(IMaterial)),
            (string)info.GetValue(nameof(ParentId), typeof(string)))
        { }

        private protected override ValueTask<Number> GetMassAsync()
        {
            if (LuminosityClass == LuminosityClass.Zero) // Hypergiants
            {
                // Maxmium possible mass at the current luminosity.
                var eddingtonLimit = (Number)(Luminosity / 1.23072e31 * 1.99e30);
                if (eddingtonLimit <= _MinHypergiantMass)
                {
                    return new ValueTask<Number>(eddingtonLimit);
                }
                else
                {
                    return new ValueTask<Number>(Randomizer.Instance.NextNumber(_MinHypergiantMass, eddingtonLimit));
                }
            }
            else if (LuminosityClass == LuminosityClass.Ia
                || LuminosityClass == LuminosityClass.Ib)
            {
                return new ValueTask<Number>(Randomizer.Instance.NextNumber(new Number(9.95, 30), new Number(2.0895, 32))); // Supergiants
            }
            else
            {
                return new ValueTask<Number>(Randomizer.Instance.NextNumber(new Number(3.98, 30), new Number(1.99, 31))); // (Bright)giants
            }
        }

        private protected override async ValueTask<SpectralClass> GenerateSpectralClassAsync()
            => GetSpectralClassFromTemperature(await GetTemperatureAsync().ConfigureAwait(false) ?? 0);

        private protected override double? GetTemperature()
            => Randomizer.Instance.PositiveNormalDistributionSample(10000, 13333);
    }
}
