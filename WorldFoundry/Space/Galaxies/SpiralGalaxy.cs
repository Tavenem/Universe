using NeverFoundry.MathAndScience.Chemistry;
using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.MathAndScience.Randomization;
using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace NeverFoundry.WorldFoundry.Space.Galaxies
{
    /// <summary>
    /// A spiral-shaped, gravitationally-bound collection of stars, gas, dust, and dark matter.
    /// </summary>
    [Serializable]
    public class SpiralGalaxy : Galaxy
    {
        private protected override string BaseTypeName => "Spiral Galaxy";

        /// <summary>
        /// Initializes a new instance of <see cref="SpiralGalaxy"/>.
        /// </summary>
        internal SpiralGalaxy() { }

        /// <summary>
        /// Initializes a new instance of <see cref="SpiralGalaxy"/> with the given parameters.
        /// </summary>
        /// <param name="parentId">The id of the location which contains this one.</param>
        /// <param name="position">The initial position of this <see cref="SpiralGalaxy"/>.</param>
        internal SpiralGalaxy(string? parentId, Vector3 position) : base(parentId, position) { }

        private SpiralGalaxy(
            string id,
            string? name,
            string galacticCoreId,
            bool isPrepopulated,
            double? albedo,
            Vector3 velocity,
            Orbit? orbit,
            IMaterial? material,
            string? parentId)
            : base(
                id,
                name,
                galacticCoreId,
                isPrepopulated,
                albedo,
                velocity,
                orbit,
                material,
                parentId) { }

        private SpiralGalaxy(SerializationInfo info, StreamingContext context) : this(
            (string)info.GetValue(nameof(Id), typeof(string)),
            (string?)info.GetValue(nameof(Name), typeof(string)),
            (string)info.GetValue(nameof(_galacticCoreId), typeof(string)),
            (bool)info.GetValue(nameof(_isPrepopulated), typeof(bool)),
            (double?)info.GetValue(nameof(Albedo), typeof(double?)),
            (Vector3)info.GetValue(nameof(Velocity), typeof(Vector3)),
            (Orbit?)info.GetValue(nameof(Orbit), typeof(Orbit?)),
            (IMaterial?)info.GetValue(nameof(_material), typeof(IMaterial)),
            (string)info.GetValue(nameof(ParentId), typeof(string))) { }

        private protected override ValueTask<IShape> GetShapeAsync()
        {
            var radius = Randomizer.Instance.NextNumber(new Number(2.4, 20), new Number(2.5, 21)); // 25000–75000 ly
            var axis = radius * Randomizer.Instance.NormalDistributionSample(0.02, 0.001);
            return new ValueTask<IShape>(new Ellipsoid(radius, axis, Position));
        }
    }
}
