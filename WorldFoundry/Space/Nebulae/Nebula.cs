using NeverFoundry.MathAndScience.Chemistry;
using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.MathAndScience.Randomization;
using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace NeverFoundry.WorldFoundry.Space
{
    /// <summary>
    /// A cloud of interstellar gas and dust.
    /// </summary>
    [Serializable]
    public class Nebula : CelestialLocation
    {
        internal static readonly Number Space = new Number(5.5, 18);

        private protected override string BaseTypeName => "Nebula";

        /// <summary>
        /// Initializes a new instance of <see cref="Nebula"/>.
        /// </summary>
        internal Nebula() { }

        /// <summary>
        /// Initializes a new instance of <see cref="Nebula"/> with the given parameters.
        /// </summary>
        /// <param name="parentId">The id of the location which contains this one.</param>
        /// <param name="position">The initial position of this <see cref="Nebula"/>.</param>
        internal Nebula(string? parentId, Vector3 position) : base(parentId, position) { }

        private protected Nebula(
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
                parentId) { }

        private Nebula(SerializationInfo info, StreamingContext context) : this(
            (string)info.GetValue(nameof(Id), typeof(string)),
            (string?)info.GetValue(nameof(Name), typeof(string)),
            (bool)info.GetValue(nameof(_isPrepopulated), typeof(bool)),
            (double?)info.GetValue(nameof(_albedo), typeof(double?)),
            (Vector3)info.GetValue(nameof(Velocity), typeof(Vector3)),
            (Orbit?)info.GetValue(nameof(Orbit), typeof(Orbit?)),
            (IMaterial?)info.GetValue(nameof(_material), typeof(IMaterial)),
            (string)info.GetValue(nameof(ParentId), typeof(string))) { }

        private protected override ValueTask<Number> GetMassAsync() => new ValueTask<Number>(Randomizer.Instance.NextNumber(new Number(1.99, 33), new Number(1.99, 37))); // ~10e3–10e7 solar masses

        // Actual nebulae are irregularly shaped; this is presumed to be a containing shape within
        // which the dust clouds and filaments roughly fit. The radius follows a log-normal
        // distribution, with ~32 ly as the mode, starting at ~16 ly, and cutting off around ~600
        // ly.
        private protected override ValueTask<IShape> GetShapeAsync()
        {
            Number axis;
            do
            {
                axis = new Number(1.5, 17) + (Randomizer.Instance.LogNormalDistributionSample(0, 1) * new Number(1.5, 17));
            } while (axis > Space);
            return new ValueTask<IShape>(new Ellipsoid(
                axis,
                axis * Randomizer.Instance.NextNumber(Number.Half, new Number(15, -1)),
                axis * Randomizer.Instance.NextNumber(Number.Half, new Number(15, -1)),
                Position));
        }

        private protected override ISubstanceReference? GetSubstance()
            => Substances.GetMixtureReference(Substances.Mixtures.MolecularCloud);
    }
}
