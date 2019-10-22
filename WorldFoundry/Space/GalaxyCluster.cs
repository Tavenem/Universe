using NeverFoundry.MathAndScience.Chemistry;
using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.MathAndScience.Randomization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using WorldFoundry.Place;

namespace WorldFoundry.Space
{
    /// <summary>
    /// A large structure of gravitationally-bound galaxies.
    /// </summary>
    [Serializable]
    public class GalaxyCluster : CelestialLocation
    {
        internal static readonly Number Space = new Number(1.5, 24);

        private static readonly List<ChildDefinition> _BaseChildDefinitions = new List<ChildDefinition>
        {
            new ChildDefinition(typeof(GalaxyGroup), GalaxyGroup.Space, new Number(1.8, -70)),
        };

        private protected override string BaseTypeName => "Galaxy Cluster";

        private protected override IEnumerable<ChildDefinition> ChildDefinitions
            => base.ChildDefinitions.Concat(_BaseChildDefinitions);

        internal GalaxyCluster() { }

        internal GalaxyCluster(Location parent, Vector3 position) : base(parent, position) { }

        private GalaxyCluster(
            string id,
            string? name,
            bool isPrepopulated,
            double? albedo,
            Vector3 velocity,
            Orbit? orbit,
            IMaterial? material,
            List<Location>? children)
            : base(
                id,
                name,
                isPrepopulated,
                albedo,
                velocity,
                orbit,
                material,
                children) { }

        private GalaxyCluster(SerializationInfo info, StreamingContext context) : this(
            (string)info.GetValue(nameof(Id), typeof(string)),
            (string?)info.GetValue(nameof(Name), typeof(string)),
            (bool)info.GetValue(nameof(_isPrepopulated), typeof(bool)),
            (double?)info.GetValue(nameof(Albedo), typeof(double?)),
            (Vector3)info.GetValue(nameof(Velocity), typeof(Vector3)),
            (Orbit?)info.GetValue(nameof(Orbit), typeof(Orbit?)),
            (IMaterial?)info.GetValue(nameof(Material), typeof(IMaterial)),
            (List<Location>)info.GetValue(nameof(Children), typeof(List<Location>))) { }

        // General average; 1.0e15–1.0e16 solar masses
        private protected override Number GetMass() => Randomizer.Instance.NextNumber(new Number(2, 45), new Number(2, 46));

        private protected override IShape GetShape() => new Sphere(Randomizer.Instance.NextNumber(new Number(3, 23), new Number(1.5, 24)), Position); // ~1–5 Mpc

        private protected override ISubstanceReference? GetSubstance()
            => Substances.GetMixtureReference(Substances.Mixtures.IntraclusterMedium);
    }
}
