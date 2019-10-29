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
        /// <param name="parentId">The id of the location which contains this one.</param>
        /// <param name="position">The initial position of this <see cref="GiantStar"/>.</param>
        internal GiantStar(string? parentId, Vector3 position) : base(parentId, position) { }

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
            (IMaterial?)info.GetValue(nameof(_material), typeof(IMaterial)),
            (string)info.GetValue(nameof(ParentId), typeof(string))) { }

        private protected override ValueTask<double> GetLuminosityAsync(Number? temperature = null) => LuminosityClass switch
        {
            LuminosityClass.Zero => new ValueTask<double>(3.846e31 + Randomizer.Instance.PositiveNormalDistributionSample(0, 3.0768e32)),
            LuminosityClass.Ia => new ValueTask<double>(Randomizer.Instance.NormalDistributionSample(1.923e31, 3.846e29)),
            LuminosityClass.Ib => new ValueTask<double>(Randomizer.Instance.NormalDistributionSample(3.846e30, 3.846e29)),
            LuminosityClass.II => new ValueTask<double>(Randomizer.Instance.NormalDistributionSample(3.846e29, 2.3076e29)),
            LuminosityClass.III => new ValueTask<double>(Randomizer.Instance.NormalDistributionSample(1.5384e29, 4.9998e28)),
            _ => new ValueTask<double>(0),
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

        private protected override async Task GenerateMaterialAsync()
        {
            var temperature = GetTemperature();
            var shape = await GetShapeAsync(temperature).ConfigureAwait(false);
            var mass = await GetMassAsync().ConfigureAwait(false);

            Material = GetComposition((double)(mass / shape.Volume), mass, shape, temperature);
        }
    }
}
