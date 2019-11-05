using NeverFoundry.MathAndScience.Chemistry;
using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.MathAndScience.Randomization;
using NeverFoundry.WorldFoundry.Space;
using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace NeverFoundry.WorldFoundry.CelestialBodies.BlackHoles
{
    /// <summary>
    /// A massive gravitational singularity, found at the center of large galaxies.
    /// </summary>
    [Serializable]
    public class SupermassiveBlackHole : BlackHole
    {
        private protected override string BaseTypeName => "Supermassive Black Hole";

        /// <summary>
        /// Initializes a new instance of <see cref="SupermassiveBlackHole"/>.
        /// </summary>
        internal SupermassiveBlackHole() { }

        /// <summary>
        /// Initializes a new instance of <see cref="SupermassiveBlackHole"/> with the given parameters.
        /// </summary>
        /// <param name="parentId">The id of the location which contains this one.</param>
        /// <param name="position">The initial position of this <see cref="SupermassiveBlackHole"/>.</param>
        internal SupermassiveBlackHole(string? parentId, Vector3 position) : base(parentId, position) { }

        private SupermassiveBlackHole(
            string id,
            string? name,
            bool isPrepopulated,
            double? albedo,
            Vector3 velocity,
            Orbit? orbit,
            IMaterial? material,
            string? parentId)
            : base(
                id,
                name,
                isPrepopulated,
                albedo,
                velocity,
                orbit,
                material,
                parentId)
        { }

        private SupermassiveBlackHole(SerializationInfo info, StreamingContext context) : this(
            (string)info.GetValue(nameof(Id), typeof(string)),
            (string?)info.GetValue(nameof(Name), typeof(string)),
            (bool)info.GetValue(nameof(_isPrepopulated), typeof(bool)),
            (double?)info.GetValue(nameof(_albedo), typeof(double?)),
            (Vector3)info.GetValue(nameof(Velocity), typeof(Vector3?)),
            (Orbit?)info.GetValue(nameof(Orbit), typeof(Orbit?)),
            (IMaterial?)info.GetValue(nameof(_material), typeof(IMaterial)),
            (string)info.GetValue(nameof(ParentId), typeof(string)))
        { }

        private protected override ValueTask<Number> GetMassAsync()
            => new ValueTask<Number>(Randomizer.Instance.NextNumber(new Number(2, 35), new Number(2, 40))); // ~10e5–10e10 solar masses
    }
}
