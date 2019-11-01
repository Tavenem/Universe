using NeverFoundry.MathAndScience.Chemistry;
using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.MathAndScience.Randomization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using NeverFoundry.WorldFoundry.Space.Galaxies;

namespace NeverFoundry.WorldFoundry.Space
{
    /// <summary>
    /// A collection of gravitationally-bound galaxies, mostly small dwarfs orbiting a few large galaxies.
    /// </summary>
    [Serializable]
    public class GalaxyGroup : CelestialLocation
    {
        internal static readonly Number Space = new Number(3, 23);

        private static readonly List<IChildDefinition> _ChildDefinitions = new List<IChildDefinition>
        {
            new ChildDefinition<DwarfGalaxy>(DwarfGalaxy.Space, new Number(1.5, -70)),
        };

        private protected override string BaseTypeName => "Galaxy Group";

        private protected override IEnumerable<IChildDefinition> ChildDefinitions => _ChildDefinitions;

        internal GalaxyGroup() { }

        internal GalaxyGroup(string? parentId, Vector3 position) : base(parentId, position) { }

        private GalaxyGroup(
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

        private GalaxyGroup(SerializationInfo info, StreamingContext context) : this(
            (string)info.GetValue(nameof(Id), typeof(string)),
            (string?)info.GetValue(nameof(Name), typeof(string)),
            (bool)info.GetValue(nameof(_isPrepopulated), typeof(bool)),
            (double?)info.GetValue(nameof(_albedo), typeof(double?)),
            (Vector3)info.GetValue(nameof(Velocity), typeof(Vector3)),
            (Orbit?)info.GetValue(nameof(Orbit), typeof(Orbit?)),
            (IMaterial?)info.GetValue(nameof(_material), typeof(IMaterial)),
            (string)info.GetValue(nameof(ParentId), typeof(string))) { }

        // General average; 1.0e14 solar masses
        private protected override ValueTask<Number> GetMassAsync() => new ValueTask<Number>(new Number(2, 44));

        private protected override ValueTask<IShape> GetShapeAsync()
            => new ValueTask<IShape>(new Sphere(Randomizer.Instance.NextNumber(new Number(1.5, 23), new Number(3, 23)), Position)); // ~500–1000 kpc

        private protected override ISubstanceReference? GetSubstance()
            => Substances.GetMixtureReference(Substances.Mixtures.IntraclusterMedium);

        private protected override async Task PrepopulateRegionAsync()
        {
            if (_isPrepopulated)
            {
                return;
            }
            _isPrepopulated = true;

            var amount = Randomizer.Instance.Next(1, 6);
            for (var i = 0; i < amount; i++)
            {
                var location = await GetOpenSpaceAsync(GalaxySubgroup.Space).ConfigureAwait(false);
                if (location.HasValue)
                {
                    var subgroup = await GetNewInstanceAsync<GalaxySubgroup>(this, location.Value).ConfigureAwait(false);
                    if (subgroup != null)
                    {
                        await subgroup.SaveAsync().ConfigureAwait(false);
                    }
                }
                else
                {
                    break;
                }
            }
        }
    }
}
