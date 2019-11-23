using NeverFoundry.MathAndScience.Chemistry;
using NeverFoundry.MathAndScience.Constants.Numbers;
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
    /// A gravitational singularity.
    /// </summary>
    [Serializable]
    public class BlackHole : CelestialLocation
    {
        internal static readonly Number Space = new Number(60000);

        private protected override string BaseTypeName => "Black Hole";

        /// <summary>
        /// Initializes a new instance of <see cref="BlackHole"/>.
        /// </summary>
        internal BlackHole() { }

        /// <summary>
        /// Initializes a new instance of <see cref="BlackHole"/> with the given parameters.
        /// </summary>
        /// <param name="parentId">The id of the location which contains this one.</param>
        /// <param name="position">The initial position of this <see cref="BlackHole"/>.</param>
        internal BlackHole(string? parentId, Vector3 position) : base(parentId, position) { }

        private protected BlackHole(
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

        private BlackHole(SerializationInfo info, StreamingContext context) : this(
            (string)info.GetValue(nameof(Id), typeof(string)),
            (string?)info.GetValue(nameof(Name), typeof(string)),
            (bool)info.GetValue(nameof(_isPrepopulated), typeof(bool)),
            (double?)info.GetValue(nameof(_albedo), typeof(double?)),
            (Vector3)info.GetValue(nameof(Velocity), typeof(Vector3)),
            (Orbit?)info.GetValue(nameof(Orbit), typeof(Orbit)),
            (IMaterial?)info.GetValue(nameof(_material), typeof(IMaterial)),
            (string)info.GetValue(nameof(ParentId), typeof(string)))
        { }

        private protected override ValueTask<Number> GetMassAsync()
            => new ValueTask<Number>(Randomizer.Instance.NextNumber(new Number(6, 30), new Number(4, 31))); // ~3–20 solar masses

        private protected override async ValueTask<(double density, Number mass, IShape shape)> GetMatterAsync()
        {
            var mass = await GetMassAsync().ConfigureAwait(false);

            // The shape given is presumed to refer to the shape of the event horizon.
            var shape = new Sphere(ScienceConstants.TwoG * mass / ScienceConstants.SpeedOfLightSquared, Position);

            return ((double)(mass / shape.Volume), mass, shape);
        }

        private protected override ISubstanceReference? GetSubstance()
            => Substances.All.Fuzzball.GetHomogeneousReference();
    }
}
