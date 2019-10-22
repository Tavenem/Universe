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
    /// The largest structure in the universe: a massive collection of galaxy groups and clusters.
    /// </summary>
    [Serializable]
    public class GalaxySupercluster : CelestialLocation
    {
        internal static readonly Number Space = new Number(9.4607, 25);

        private static readonly Number _ChildDensity = new Number(1, -73);

        private static readonly List<ChildDefinition> _BaseChildDefinitions = new List<ChildDefinition>
        {
            new ChildDefinition(typeof(GalaxyCluster), GalaxyCluster.Space, _ChildDensity / 3),
            new ChildDefinition(typeof(GalaxyGroup), GalaxyGroup.Space, _ChildDensity * 2 / 3),
        };

        private protected override string BaseTypeName => "Galaxy Supercluster";

        private protected override IEnumerable<ChildDefinition> ChildDefinitions
            => base.ChildDefinitions.Concat(_BaseChildDefinitions);

        internal GalaxySupercluster() { }

        internal GalaxySupercluster(Location parent, Vector3 position) : base(parent, position) { }

        private GalaxySupercluster(
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

        private GalaxySupercluster(SerializationInfo info, StreamingContext context) : this(
            (string)info.GetValue(nameof(Id), typeof(string)),
            (string?)info.GetValue(nameof(Name), typeof(string)),
            (bool)info.GetValue(nameof(_isPrepopulated), typeof(bool)),
            (double?)info.GetValue(nameof(Albedo), typeof(double?)),
            (Vector3)info.GetValue(nameof(Velocity), typeof(Vector3)),
            (Orbit?)info.GetValue(nameof(Orbit), typeof(Orbit?)),
            (IMaterial?)info.GetValue(nameof(Material), typeof(IMaterial)),
            (List<Location>)info.GetValue(nameof(Children), typeof(List<Location>))) { }

        // General average; 1.0e16–1.0e17 solar masses
        private protected override Number GetMass() => Randomizer.Instance.NextNumber(new Number(2, 46), new Number(2, 47));

        private protected override IShape GetShape()
        {
            // May be filaments (narrow in two dimensions), or walls/sheets (narrow in one dimension).
            var majorAxis = Randomizer.Instance.NextNumber(new Number(9.4607, 23), new Number(9.4607, 25));
            var minorAxis1 = majorAxis * Randomizer.Instance.NextNumber(new Number(2, -2), new Number(15, -2));
            Number minorAxis2;
            if (Randomizer.Instance.NextBool()) // Filament
            {
                minorAxis2 = minorAxis1;
            }
            else // Wall/sheet
            {
                minorAxis2 = majorAxis * Randomizer.Instance.NextNumber(new Number(3, -2), new Number(8, -2));
            }
            var chance = Randomizer.Instance.Next(6);
            if (chance == 0)
            {
                return new Ellipsoid(majorAxis, minorAxis1, minorAxis2, Position);
            }
            else if (chance == 1)
            {
                return new Ellipsoid(majorAxis, minorAxis2, minorAxis1, Position);
            }
            else if (chance == 2)
            {
                return new Ellipsoid(minorAxis1, majorAxis, minorAxis2, Position);
            }
            else if (chance == 3)
            {
                return new Ellipsoid(minorAxis2, majorAxis, minorAxis1, Position);
            }
            else if (chance == 4)
            {
                return new Ellipsoid(minorAxis1, minorAxis2, majorAxis, Position);
            }
            else
            {
                return new Ellipsoid(minorAxis2, minorAxis1, majorAxis, Position);
            }
        }

        private protected override ISubstanceReference? GetSubstance()
            => Substances.GetMixtureReference(Substances.Mixtures.IntraclusterMedium);
    }
}
