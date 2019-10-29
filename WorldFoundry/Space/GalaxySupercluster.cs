using NeverFoundry.MathAndScience.Chemistry;
using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.MathAndScience.Randomization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

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

        private static readonly List<IChildDefinition> _BaseChildDefinitions = new List<IChildDefinition>
        {
            new ChildDefinition<GalaxyCluster>(GalaxyCluster.Space, _ChildDensity / 3),
            new ChildDefinition<GalaxyGroup>(GalaxyGroup.Space, _ChildDensity * 2 / 3),
        };

        private protected override string BaseTypeName => "Galaxy Supercluster";

        private protected override IEnumerable<IChildDefinition> ChildDefinitions => _BaseChildDefinitions;

        internal GalaxySupercluster() { }

        internal GalaxySupercluster(string? parentId, Vector3 position) : base(parentId, position) { }

        private GalaxySupercluster(
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

        private GalaxySupercluster(SerializationInfo info, StreamingContext context) : this(
            (string)info.GetValue(nameof(Id), typeof(string)),
            (string?)info.GetValue(nameof(Name), typeof(string)),
            (bool)info.GetValue(nameof(_isPrepopulated), typeof(bool)),
            (double?)info.GetValue(nameof(Albedo), typeof(double?)),
            (Vector3)info.GetValue(nameof(Velocity), typeof(Vector3)),
            (Orbit?)info.GetValue(nameof(Orbit), typeof(Orbit?)),
            (IMaterial?)info.GetValue(nameof(_material), typeof(IMaterial)),
            (string?)info.GetValue(nameof(ParentId), typeof(string))) { }

        // General average; 1.0e16–1.0e17 solar masses
        private protected override ValueTask<Number> GetMassAsync() => new ValueTask<Number>(Randomizer.Instance.NextNumber(new Number(2, 46), new Number(2, 47)));

        private protected override ValueTask<IShape> GetShapeAsync()
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
                return new ValueTask<IShape>(new Ellipsoid(majorAxis, minorAxis1, minorAxis2, Position));
            }
            else if (chance == 1)
            {
                return new ValueTask<IShape>(new Ellipsoid(majorAxis, minorAxis2, minorAxis1, Position));
            }
            else if (chance == 2)
            {
                return new ValueTask<IShape>(new Ellipsoid(minorAxis1, majorAxis, minorAxis2, Position));
            }
            else if (chance == 3)
            {
                return new ValueTask<IShape>(new Ellipsoid(minorAxis2, majorAxis, minorAxis1, Position));
            }
            else if (chance == 4)
            {
                return new ValueTask<IShape>(new Ellipsoid(minorAxis1, minorAxis2, majorAxis, Position));
            }
            else
            {
                return new ValueTask<IShape>(new Ellipsoid(minorAxis2, minorAxis1, majorAxis, Position));
            }
        }

        private protected override ISubstanceReference? GetSubstance()
            => Substances.GetMixtureReference(Substances.Mixtures.IntraclusterMedium);
    }
}
