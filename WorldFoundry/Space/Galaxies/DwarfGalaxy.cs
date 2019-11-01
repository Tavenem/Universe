using NeverFoundry.MathAndScience.Chemistry;
using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.MathAndScience.Randomization;
using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using NeverFoundry.WorldFoundry.CelestialBodies.BlackHoles;

namespace NeverFoundry.WorldFoundry.Space.Galaxies
{
    /// <summary>
    /// A small, gravitationally-bound collection of stars, gas, dust, and dark matter.
    /// </summary>
    [Serializable]
    public class DwarfGalaxy : Galaxy
    {
        internal static readonly Number Space = new Number(2.5, 18);

        private protected override string BaseTypeName => "Dwarf Galaxy";

        /// <summary>
        /// Initializes a new instance of <see cref="DwarfGalaxy"/>.
        /// </summary>
        internal DwarfGalaxy() { }

        /// <summary>
        /// Initializes a new instance of <see cref="DwarfGalaxy"/> with the given parameters.
        /// </summary>
        /// <param name="parentId">The id of the location which contains this one.</param>
        /// <param name="position">The initial position of this <see cref="DwarfGalaxy"/>.</param>
        internal DwarfGalaxy(string? parentId, Vector3 position) : base(parentId, position) { }

        private DwarfGalaxy(
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

        private DwarfGalaxy(SerializationInfo info, StreamingContext context) : this(
            (string)info.GetValue(nameof(Id), typeof(string)),
            (string?)info.GetValue(nameof(Name), typeof(string)),
            (string)info.GetValue(nameof(_galacticCoreId), typeof(string)),
            (bool)info.GetValue(nameof(_isPrepopulated), typeof(bool)),
            (double?)info.GetValue(nameof(_albedo), typeof(double?)),
            (Vector3)info.GetValue(nameof(Velocity), typeof(Vector3)),
            (Orbit?)info.GetValue(nameof(Orbit), typeof(Orbit?)),
            (IMaterial?)info.GetValue(nameof(_material), typeof(IMaterial)),
            (string)info.GetValue(nameof(ParentId), typeof(string))) { }

        /// <summary>
        /// Generates the central gravitational object of this <see cref="Galaxy"/>, which all others orbit.
        /// </summary>
        /// <remarks>
        /// The cores of dwarf galaxies are ordinary black holes, not super-massive.
        /// </remarks>
        private protected override Task<BlackHole?> GenerateGalacticCoreAsync()
            => GetNewInstanceAsync<BlackHole>(this, Vector3.Zero);

        private protected override ValueTask<IShape> GetShapeAsync()
        {
            var radius = Randomizer.Instance.NextNumber(new Number(9.5, 18), new Number(2.5, 18)); // ~200–1800 ly
            var axis = radius * Randomizer.Instance.NormalDistributionSample(0.02, 1);
            return new ValueTask<IShape>(new Ellipsoid(radius, axis, Position));
        }
    }
}
