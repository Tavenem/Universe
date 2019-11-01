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
    /// A neutron star.
    /// </summary>
    [Serializable]
    public class NeutronStar : Star
    {
        // False for neutron stars, due to their excessive ionizing radiation, which makes the
        // development of life nearby highly unlikely.
        internal override bool IsHospitable => false;

        private protected override string BaseTypeName => "Neutron Star";

        private protected override string DesignatorPrefix => "X";

        /// <summary>
        /// Initializes a new instance of <see cref="NeutronStar"/>.
        /// </summary>
        internal NeutronStar() { }

        /// <summary>
        /// Initializes a new instance of <see cref="NeutronStar"/> with the given parameters.
        /// </summary>
        /// <param name="parentId">The id of the location which contains this one.</param>
        /// <param name="position">The initial position of this <see cref="NeutronStar"/>.</param>
        internal NeutronStar(string? parentId, Vector3 position) : base(parentId, position) { }

        private protected NeutronStar(
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

        private NeutronStar(SerializationInfo info, StreamingContext context) : this(
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
            (string)info.GetValue(nameof(ParentId), typeof(string))) { }

        private protected override ValueTask<double> GetLuminosityAsync(Number? temperature = null) => GetLuminosityFromRadiusAsync();

        private protected override LuminosityClass GetLuminosityClass() => LuminosityClass.Other;

        private protected override ValueTask<Number> GetMassAsync()
            => new ValueTask<Number>(Randomizer.Instance.NormalDistributionSample(4.4178e30, 5.174e29)); // between 1.44 and 3 times solar mass

        private protected override async Task GenerateMaterialAsync()
        {
            if (_material is null)
            {
                var shape = await GetShapeAsync().ConfigureAwait(false);
                var mass = await GetMassAsync().ConfigureAwait(false);
                Material = GetComposition((double)(mass / shape.Volume), mass, shape, GetTemperature());
            }
        }

        private protected override ValueTask<IShape> GetShapeAsync()
        {
            var radius = Randomizer.Instance.NextNumber(1000, 2000);
            var flattening = (Number)Randomizer.Instance.NormalDistributionSample(0.15, 0.05, minimum: 0);
            return new ValueTask<IShape>(new Ellipsoid(radius, radius * (1 - flattening), Position));
        }

        private protected override ValueTask<SpectralClass> GenerateSpectralClassAsync() => new ValueTask<SpectralClass>(SpectralClass.Other);

        private protected override ISubstanceReference? GetSubstance()
            => Substances.GetHomogeneousSubstanceReference(Substances.HomogeneousSubstances.NeutronDegenerateMatter);

        private protected override double? GetTemperature()
            => Randomizer.Instance.NormalDistributionSample(600000, 133333);
    }
}
