using NeverFoundry.MathAndScience.Chemistry;
using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.MathAndScience.Randomization;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace NeverFoundry.WorldFoundry.Space
{
    /// <summary>
    /// A large structure of gravitationally-bound galaxies.
    /// </summary>
    [Serializable]
    public class GalaxyCluster : CelestialLocation
    {
        internal static readonly Number Space = new Number(1.5, 24);

        private static readonly List<IChildDefinition> _BaseChildDefinitions = new List<IChildDefinition>
        {
            new ChildDefinition<GalaxyGroup>(GalaxyGroup.Space, new Number(1.8, -70)),
        };

        private protected override string BaseTypeName => "Galaxy Cluster";

        private protected override IEnumerable<IChildDefinition> ChildDefinitions => _BaseChildDefinitions;

        internal GalaxyCluster() { }

        internal GalaxyCluster(string? parentId, Vector3 position) : base(parentId, position) { }

        private GalaxyCluster(
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

        private GalaxyCluster(SerializationInfo info, StreamingContext context) : this(
            (string)info.GetValue(nameof(Id), typeof(string)),
            (string?)info.GetValue(nameof(Name), typeof(string)),
            (bool)info.GetValue(nameof(_isPrepopulated), typeof(bool)),
            (double?)info.GetValue(nameof(_albedo), typeof(double?)),
            (Vector3)info.GetValue(nameof(Velocity), typeof(Vector3)),
            (Orbit?)info.GetValue(nameof(Orbit), typeof(Orbit?)),
            (IMaterial?)info.GetValue(nameof(_material), typeof(IMaterial)),
            (string)info.GetValue(nameof(ParentId), typeof(string)))
        { }

        // General average; 1.0e15–1.0e16 solar masses
        private protected override ValueTask<Number> GetMassAsync()
            => new ValueTask<Number>(Randomizer.Instance.NextNumber(new Number(2, 45), new Number(2, 46)));

        private protected override ValueTask<IShape> GetShapeAsync()
            => new ValueTask<IShape>(new Sphere(Randomizer.Instance.NextNumber(new Number(3, 23), new Number(1.5, 24)), Position)); // ~1–5 Mpc

        private protected override ISubstanceReference? GetSubstance()
            => Substances.All.IntraclusterMedium.GetReference();
    }
}
