using NeverFoundry.MathAndScience.Chemistry;
using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.MathAndScience.Randomization;
using NeverFoundry.WorldFoundry.Space.Galaxies;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Threading.Tasks;

namespace NeverFoundry.WorldFoundry.Space
{
    /// <summary>
    /// A collection of dwarf galaxies and globular clusters orbiting a large main galaxy.
    /// </summary>
    [Serializable]
    public class GalaxySubgroup : CelestialLocation
    {
        internal static readonly Number Space = new Number(2.5, 22);

        private static readonly Number _ChildDensity = new Number(1, -70);

        private static readonly List<IChildDefinition> _ChildDefinitions = new List<IChildDefinition>
        {
            new ChildDefinition<DwarfGalaxy>(DwarfGalaxy.Space, _ChildDensity * new Number(26, -2)),
            new ChildDefinition<GlobularCluster>(GlobularCluster.Space, _ChildDensity * new Number(74, -2)),
        };

        private string? _mainGalaxyId;
        private Galaxy? _mainGalaxy;

        private protected override string BaseTypeName => "Galaxy Subgroup";

        private protected override IEnumerable<IChildDefinition> ChildDefinitions => _ChildDefinitions;

        /// <summary>
        /// Initializes a new instance of <see cref="GalaxySubgroup"/>.
        /// </summary>
        internal GalaxySubgroup() { }

        /// <summary>
        /// Initializes a new instance of <see cref="GalaxySubgroup"/> with the given parameters.
        /// </summary>
        /// <param name="parentId">The id of the location which contains this one.</param>
        /// <param name="position">The initial position of this <see cref="GalaxySubgroup"/>.</param>
        internal GalaxySubgroup(string? parentId, Vector3 position) : base(parentId, position) { }

        private GalaxySubgroup(
            string id,
            string? name,
            string mainGalaxyId,
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
                parentId) => _mainGalaxyId = mainGalaxyId;

        private GalaxySubgroup(SerializationInfo info, StreamingContext context) : this(
            (string)info.GetValue(nameof(Id), typeof(string)),
            (string?)info.GetValue(nameof(Name), typeof(string)),
            (string)info.GetValue(nameof(_mainGalaxyId), typeof(string)),
            (bool)info.GetValue(nameof(_isPrepopulated), typeof(bool)),
            (double?)info.GetValue(nameof(_albedo), typeof(double?)),
            (Vector3)info.GetValue(nameof(Velocity), typeof(Vector3)),
            (Orbit?)info.GetValue(nameof(Orbit), typeof(Orbit?)),
            (IMaterial?)info.GetValue(nameof(_material), typeof(IMaterial)),
            (string)info.GetValue(nameof(ParentId), typeof(string)))
        { }

        /// <summary>
        /// Gets the main <see cref="Galaxy"/> around which the other objects in this <see
        /// cref="GalaxySubgroup"/> orbit.
        /// </summary>
        /// <returns>
        /// The main <see cref="Galaxy"/> around which the other objects in this <see
        /// cref="GalaxySubgroup"/> orbit.
        /// </returns>
        public async Task<Galaxy?> GetMainGalaxyAsync()
        {
            if (_mainGalaxy is null)
            {
                if (string.IsNullOrEmpty(_mainGalaxyId))
                {
                    var core = await GenerateMainGalaxyAsync().ConfigureAwait(false);
                    if (core != null)
                    {
                        await core.SaveAsync().ConfigureAwait(false);
                    }
                    _mainGalaxyId = core?.Id;
                    _mainGalaxy = core;
                }
                else
                {
                    _mainGalaxy = await DataStore.GetItemAsync<Galaxy>(_mainGalaxyId).ConfigureAwait(false);
                }
            }
            return _mainGalaxy;
        }

        /// <summary>Populates a <see cref="SerializationInfo"></see> with the data needed to
        /// serialize the target object.</summary>
        /// <param name="info">The <see cref="SerializationInfo"></see> to populate with
        /// data.</param>
        /// <param name="context">The destination (see <see cref="StreamingContext"></see>) for this
        /// serialization.</param>
        /// <exception cref="System.Security.SecurityException">The caller does not have the
        /// required permission.</exception>
        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(Id), Id);
            info.AddValue(nameof(Name), Name);
            info.AddValue(nameof(_mainGalaxyId), _mainGalaxyId);
            info.AddValue(nameof(_isPrepopulated), _isPrepopulated);
            info.AddValue(nameof(_albedo), _albedo);
            info.AddValue(nameof(Velocity), Velocity);
            info.AddValue(nameof(Orbit), _orbit);
            info.AddValue(nameof(_material), Material);
            info.AddValue(nameof(ParentId), ParentId);
        }

        /// <summary>
        /// Randomly determines the main <see cref="Galaxy"/> of this <see cref="GalaxySubgroup"/>,
        /// which all other objects orbit.
        /// </summary>
        /// <remarks>70% of large galaxies are spirals.</remarks>
        private async Task<Galaxy?> GenerateMainGalaxyAsync()
            => Randomizer.Instance.NextDouble() <= 0.7
                ? await GetNewInstanceAsync<SpiralGalaxy>(this, Vector3.Zero).ConfigureAwait(false)
                : (Galaxy?)await GetNewInstanceAsync<EllipticalGalaxy>(this, Vector3.Zero).ConfigureAwait(false);

        // The main galaxy is expected to comprise the bulk of the mass
        private protected override async ValueTask<Number> GetMassAsync()
        {
            var g = await GetMainGalaxyAsync().ConfigureAwait(false);
            return (g?.Mass ?? Number.Zero) * new Number(125, -2);
        }

        private protected override async ValueTask<IShape> GetShapeAsync()
        {
            var g = await GetMainGalaxyAsync().ConfigureAwait(false);
            return g is null
                ? new Sphere(Number.Zero, Position)
                : new Sphere(g.Shape.ContainingRadius * 10, Position);
        }

        private protected override ISubstanceReference? GetSubstance()
            => Substances.GetMixtureReference(Substances.Mixtures.IntraclusterMedium);

        private protected override async Task InitializeAsync()
        {
            await GetMainGalaxyAsync().ConfigureAwait(false);
            await base.InitializeAsync().ConfigureAwait(false);
        }

        private protected override async Task InitializeChildAsync(CelestialLocation child)
        {
            if (!string.IsNullOrEmpty(_mainGalaxyId))
            {
                var g = await GetMainGalaxyAsync().ConfigureAwait(false);
                await WorldFoundry.Space.Orbit.SetOrbitAsync(
                      child,
                      g,
                      Randomizer.Instance.NextDouble(0.1)).ConfigureAwait(false);
            }
        }
    }
}
