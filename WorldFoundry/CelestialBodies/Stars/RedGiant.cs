using System;
using System.Runtime.Serialization;
using NeverFoundry.WorldFoundry.Space;
using NeverFoundry.MathAndScience.Chemistry;
using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.MathAndScience.Randomization;
using System.Threading.Tasks;

namespace NeverFoundry.WorldFoundry.CelestialBodies.Stars
{
    /// <summary>
    /// A red giant star.
    /// </summary>
    [Serializable]
    public class RedGiant : GiantStar
    {
        private protected override string BaseTypeName => "Red Giant";

        /// <summary>
        /// Initializes a new instance of <see cref="RedGiant"/>.
        /// </summary>
        internal RedGiant() { }

        /// <summary>
        /// Initializes a new instance of <see cref="RedGiant"/> with the given parameters.
        /// </summary>
        /// <param name="parentId">The id of the location which contains this one.</param>
        /// <param name="position">The initial position of this <see cref="RedGiant"/>.</param>
        internal RedGiant(string? parentId, Vector3 position) : base(parentId, position) { }

        private protected RedGiant(
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
                parentId) { }

        private RedGiant(SerializationInfo info, StreamingContext context) : this(
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
            (IMaterial?)info.GetValue(nameof(_material), typeof(IMaterial)),
            (string)info.GetValue(nameof(ParentId), typeof(string))) { }

        private protected override ValueTask<Number> GetMassAsync()
        {
            if (LuminosityClass == LuminosityClass.Zero
                || LuminosityClass == LuminosityClass.Ia
                || LuminosityClass == LuminosityClass.Ib)
            {
                return new ValueTask<Number>(Randomizer.Instance.NextNumber(new Number(1.592, 31), new Number(4.975, 31))); // Super/hypergiants
            }
            else
            {
                return new ValueTask<Number>(Randomizer.Instance.NextNumber(new Number(5.97, 29), new Number(1.592, 31))); // (Bright)giants
            }
        }

        private protected override async ValueTask<SpectralClass> GenerateSpectralClassAsync()
            => GetSpectralClassFromTemperature(await GetTemperatureAsync().ConfigureAwait(false) ?? 0);

        private protected override double? GetTemperature()
            => Randomizer.Instance.NormalDistributionSample(3800, 466);
    }
}
