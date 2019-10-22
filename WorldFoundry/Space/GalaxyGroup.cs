using NeverFoundry.MathAndScience.Chemistry;
using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.MathAndScience.Randomization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using WorldFoundry.Place;
using WorldFoundry.Space.Galaxies;

namespace WorldFoundry.Space
{
    /// <summary>
    /// A collection of gravitationally-bound galaxies, mostly small dwarfs orbiting a few large galaxies.
    /// </summary>
    [Serializable]
    public class GalaxyGroup : CelestialLocation
    {
        internal static readonly Number Space = new Number(3, 23);

        private static readonly List<ChildDefinition> _ChildDefinitions = new List<ChildDefinition>
        {
            new ChildDefinition(typeof(DwarfGalaxy), DwarfGalaxy.Space, new Number(1.5, -70)),
        };

        private protected override string BaseTypeName => "Galaxy Group";

        private protected override IEnumerable<ChildDefinition> ChildDefinitions
            => base.ChildDefinitions.Concat(_ChildDefinitions);

        internal GalaxyGroup() { }

        internal GalaxyGroup(Location parent, Vector3 position) : base(parent, position) { }

        private GalaxyGroup(
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

        private GalaxyGroup(SerializationInfo info, StreamingContext context) : this(
            (string)info.GetValue(nameof(Id), typeof(string)),
            (string?)info.GetValue(nameof(Name), typeof(string)),
            (bool)info.GetValue(nameof(_isPrepopulated), typeof(bool)),
            (double?)info.GetValue(nameof(Albedo), typeof(double?)),
            (Vector3)info.GetValue(nameof(Velocity), typeof(Vector3)),
            (Orbit?)info.GetValue(nameof(Orbit), typeof(Orbit?)),
            (IMaterial?)info.GetValue(nameof(Material), typeof(IMaterial)),
            (List<Location>)info.GetValue(nameof(Children), typeof(List<Location>))) { }

        internal override void PrepopulateRegion()
        {
            if (_isPrepopulated)
            {
                return;
            }
            base.PrepopulateRegion();

            var amount = Randomizer.Instance.Next(1, 6);
            Vector3 position;
            for (var i = 0; i < amount; i++)
            {
                if (TryGetOpenSpace(GalaxySubgroup.Space, out var location))
                {
                    position = location;
                }
                else
                {
                    break;
                }

                _ = new GalaxySubgroup(this, position);
            }
        }

        // General average; 1.0e14 solar masses
        private protected override Number GetMass() => new Number(2, 44);

        private protected override IShape GetShape() => new Sphere(Randomizer.Instance.NextNumber(new Number(1.5, 23), new Number(3, 23)), Position); // ~500–1000 kpc

        private protected override ISubstanceReference? GetSubstance()
            => Substances.GetMixtureReference(Substances.Mixtures.IntraclusterMedium);
    }
}
