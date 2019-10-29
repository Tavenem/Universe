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
    /// A brown dwarf star.
    /// </summary>
    [Serializable]
    public class BrownDwarf : Star
    {
        // False for brown dwarfs; their habitable zones, if any, are moving targets due to rapid
        // cooling, and intersect soon with severe tidal forces, making it unlikely that life could
        // develop before a planet becomes uninhabitable.
        internal override bool IsHospitable => false;

        private protected override string BaseTypeName => "Brown Dwarf";

        /// <summary>
        /// Initializes a new instance of <see cref="BrownDwarf"/>.
        /// </summary>
        internal BrownDwarf() { }

        /// <summary>
        /// Initializes a new instance of <see cref="BrownDwarf"/> with the given parameters.
        /// </summary>
        /// <param name="parentId">The id of the location which contains this one.</param>
        /// <param name="position">The initial position of this <see cref="BrownDwarf"/>.</param>
        internal BrownDwarf(string? parentId, Vector3 position) : base(parentId, position) { }

        private protected BrownDwarf(
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

        private BrownDwarf(SerializationInfo info, StreamingContext context) : this(
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

        private protected override ValueTask<double> GetLuminosityAsync(Number? temperature = null) => GetLuminosityFromRadiusAsync();

        private protected override ValueTask<Number> GetMassAsync() => new ValueTask<Number>(Randomizer.Instance.NextNumber(new Number(2.468, 28), new Number(1.7088, 29)));

        private protected override async Task GenerateMaterialAsync()
        {
            var temperature = GetTemperature();
            var shape = await GetShapeAsync(temperature).ConfigureAwait(false);
            var mass = await GetMassAsync().ConfigureAwait(false);
            Material = GetComposition((double)(mass / shape.Volume), mass, shape, temperature);
        }

        private protected override ValueTask<IShape> GetShapeAsync()
        {
            var radius = (Number)Randomizer.Instance.NormalDistributionSample(69911000, 3495550);
            var flattening = Randomizer.Instance.NextNumber(Number.Deci);
            return new ValueTask<IShape>(new Ellipsoid(radius, radius * (1 - flattening), Position));
        }

        private protected override ValueTask<SpectralClass> GenerateSpectralClassAsync()
        {
            var chance = Randomizer.Instance.NextDouble();
            if (chance <= 0.29)
            {
                return new ValueTask<SpectralClass>(SpectralClass.M); // 29%
            }
            else if (chance <= 0.79)
            {
                return new ValueTask<SpectralClass>(SpectralClass.L); // 50%
            }
            else if (chance <= 0.99)
            {
                return new ValueTask<SpectralClass>(SpectralClass.T); // 20%
            }
            else
            {
                return new ValueTask<SpectralClass>(SpectralClass.Y); // 1%
            }
        }
    }
}
