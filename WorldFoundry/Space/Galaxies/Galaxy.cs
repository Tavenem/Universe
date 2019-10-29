using NeverFoundry.MathAndScience.Chemistry;
using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.MathAndScience.Randomization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Threading.Tasks;
using WorldFoundry.CelestialBodies.BlackHoles;
using WorldFoundry.CelestialBodies.Planetoids.Planets.GiantPlanets;
using WorldFoundry.CelestialBodies.Planetoids.Planets.TerrestrialPlanets;
using WorldFoundry.CelestialBodies.Stars;

namespace WorldFoundry.Space.Galaxies
{
    /// <summary>
    /// A gravitationally-bound collection of stars, gas, dust, and dark matter.
    /// </summary>
    [Serializable]
    public class Galaxy : CelestialLocation
    {
        private static readonly Number _ChildDensity = new Number(4, -50);
        private static readonly Number _RogueDensity = _ChildDensity * new Number(6, -1);
        private static readonly Number _RedDensity = _ChildDensity * new Number(1437, -4);
        private static readonly Number _KDensity = _ChildDensity * new Number(23, -3);
        private static readonly Number _GDensity = _ChildDensity * new Number(145, -4);
        private static readonly Number _FDensity = _ChildDensity * new Number(57, -4);
        private static readonly Number _RedGiantDensity = _ChildDensity * new Number(1, -3);
        private static readonly Number _BlueGiantDensity = _ChildDensity * new Number(8, -4);
        private static readonly Number _YellowGiantDensity = _ChildDensity * new Number(4, -4);

        private static readonly List<IChildDefinition> _ChildDefinitions = new List<IChildDefinition>
        {
            new ChildDefinition<GiantPlanet>(GiantPlanet.Space, _RogueDensity * 5 / 12),
            new ChildDefinition<IceGiant>(GiantPlanet.Space, _RogueDensity * new Number(25, -2)),
            new ChildDefinition<TerrestrialPlanet>(TerrestrialPlanet.Space, _RogueDensity / 6),
            new ChildDefinition<OceanPlanet>(TerrestrialPlanet.Space, _RogueDensity / 24),
            new ChildDefinition<IronPlanet>(TerrestrialPlanet.Space, _RogueDensity / 24),
            new ChildDefinition<CarbonPlanet>(TerrestrialPlanet.Space, _RogueDensity / 12),

            new StarSystemChildDefinition<BrownDwarf>(_ChildDensity * new Number(19, -2)),

            new StarSystemChildDefinition<Star>(_RedDensity * new Number(998, -3), SpectralClass.M, LuminosityClass.V),
            new StarSystemChildDefinition<Star>(_RedDensity * new Number(2, -3), SpectralClass.M, LuminosityClass.sd),

            new StarSystemChildDefinition<Star>(_KDensity * new Number(987, -3), SpectralClass.K, LuminosityClass.V),
            new StarSystemChildDefinition<Star>(_KDensity * new Number(1, -2), SpectralClass.K, LuminosityClass.IV),
            new StarSystemChildDefinition<Star>(_KDensity * new Number(3, -3), SpectralClass.K, LuminosityClass.sd),

            new StarSystemChildDefinition<WhiteDwarf>(_ChildDensity * new Number(18, -3)),

            new StarSystemChildDefinition<Star>(_GDensity * new Number(992, -3), SpectralClass.G, LuminosityClass.V),
            new StarSystemChildDefinition<Star>(_GDensity * new Number(8, -3), SpectralClass.G, LuminosityClass.IV),

            new StarSystemChildDefinition<Star>(_FDensity * new Number(982, -3), SpectralClass.F, LuminosityClass.V),
            new StarSystemChildDefinition<Star>(_FDensity * new Number(18, -3), SpectralClass.F, LuminosityClass.IV),

            new StarSystemChildDefinition<NeutronStar>(_ChildDensity * new Number(14, -4)),

            new StarSystemChildDefinition<Star>(_ChildDensity * new Number(115, -5), SpectralClass.A, LuminosityClass.V),

            new StarSystemChildDefinition<RedGiant>(_RedGiantDensity * new Number(96, -2)),
            new StarSystemChildDefinition<RedGiant>(_RedGiantDensity * new Number(18, -3), null, LuminosityClass.II),
            new StarSystemChildDefinition<RedGiant>(_RedGiantDensity * new Number(16, -3), null, LuminosityClass.Ib),
            new StarSystemChildDefinition<RedGiant>(_RedGiantDensity * new Number(55, -4), null, LuminosityClass.Ia),
            new StarSystemChildDefinition<RedGiant>(_RedGiantDensity * new Number(5, -4), null, LuminosityClass.Zero),

            new StarSystemChildDefinition<BlueGiant>(_BlueGiantDensity * new Number(95, -2)),
            new StarSystemChildDefinition<BlueGiant>(_BlueGiantDensity * new Number(25, -3), null, LuminosityClass.II),
            new StarSystemChildDefinition<BlueGiant>(_BlueGiantDensity * new Number(2, -2), null, LuminosityClass.Ib),
            new StarSystemChildDefinition<BlueGiant>(_BlueGiantDensity * new Number(45, -4), null, LuminosityClass.Ia),
            new StarSystemChildDefinition<BlueGiant>(_BlueGiantDensity * new Number(5, -4), null, LuminosityClass.Zero),

            new StarSystemChildDefinition<YellowGiant>(_YellowGiantDensity * new Number(95, -2)),
            new StarSystemChildDefinition<YellowGiant>(_YellowGiantDensity * new Number(2, -2), null, LuminosityClass.II),
            new StarSystemChildDefinition<YellowGiant>(_YellowGiantDensity * new Number(23, -3), null, LuminosityClass.Ib),
            new StarSystemChildDefinition<YellowGiant>(_YellowGiantDensity * new Number(6, -3), null, LuminosityClass.Ia),
            new StarSystemChildDefinition<YellowGiant>(_YellowGiantDensity * new Number(1, -3), null, LuminosityClass.Zero),

            new StarSystemChildDefinition<Star>(_ChildDensity * new Number(25, -5), SpectralClass.B, LuminosityClass.V),

            new ChildDefinition<BlackHole>(BlackHole.Space, _ChildDensity * new Number(1, -4)),

            new StarSystemChildDefinition<Star>(_ChildDensity * new Number(7, -8), SpectralClass.O, LuminosityClass.V),

            new ChildDefinition<PlanetaryNebula>(PlanetaryNebula.Space, _ChildDensity * new Number(3, -8)),

            new ChildDefinition<Nebula>(Nebula.Space, _ChildDensity * new Number(2, -10)),

            new ChildDefinition<HIIRegion>(Nebula.Space, _ChildDensity * new Number(2, -10)),
        };

        private protected string? _galacticCoreId;
        private protected BlackHole? _galacticCore;

        private protected override string BaseTypeName => "Galaxy";

        private protected override IEnumerable<IChildDefinition> ChildDefinitions => _ChildDefinitions;

        /// <summary>
        /// Initializes a new instance of <see cref="Galaxy"/>.
        /// </summary>
        internal Galaxy() { }

        /// <summary>
        /// Initializes a new instance of <see cref="Galaxy"/> with the given parameters.
        /// </summary>
        /// <param name="parentId">The id of the location which contains this one.</param>
        /// <param name="position">The initial position of this <see cref="Galaxy"/>.</param>
        internal Galaxy(string? parentId, Vector3 position) : base(parentId, position) { }

        private protected Galaxy(
            string id,
            string? name,
            string galacticCoreId,
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
            => _galacticCoreId = galacticCoreId;

        private Galaxy(SerializationInfo info, StreamingContext context) : this(
            (string)info.GetValue(nameof(Id), typeof(string)),
            (string?)info.GetValue(nameof(Name), typeof(string)),
            (string)info.GetValue(nameof(_galacticCoreId), typeof(string)),
            (bool)info.GetValue(nameof(_isPrepopulated), typeof(bool)),
            (double?)info.GetValue(nameof(Albedo), typeof(double?)),
            (Vector3)info.GetValue(nameof(Velocity), typeof(Vector3)),
            (Orbit?)info.GetValue(nameof(Orbit), typeof(Orbit?)),
            (IMaterial?)info.GetValue(nameof(_material), typeof(IMaterial)),
            (string)info.GetValue(nameof(ParentId), typeof(string))) { }

        /// <summary>
        /// Gets the <see cref="BlackHole"/> which is at the center of this <see cref="Galaxy"/>.
        /// </summary>
        /// <returns>
        /// The <see cref="BlackHole"/> which is at the center of this <see cref="Galaxy"/>.
        /// </returns>
        public async Task<BlackHole?> GetGalacticCoreAsync()
        {
            if (_galacticCore is null)
            {
                if (string.IsNullOrEmpty(_galacticCoreId))
                {
                    var core = await GenerateGalacticCoreAsync().ConfigureAwait(false);
                    if (core != null)
                    {
                        await core.SaveAsync().ConfigureAwait(false);
                    }
                    _galacticCoreId = core?.Id;
                    _galacticCore = core;
                }
                else
                {
                    _galacticCore = await DataStore.GetItemAsync<BlackHole>(_galacticCoreId).ConfigureAwait(false);
                }
            }
            return _galacticCore;
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
            info.AddValue(nameof(_galacticCoreId), _galacticCoreId);
            info.AddValue(nameof(_isPrepopulated), _isPrepopulated);
            info.AddValue(nameof(Albedo), _albedo);
            info.AddValue(nameof(Velocity), Velocity);
            info.AddValue(nameof(Orbit), _orbit);
            info.AddValue(nameof(_material), Material);
            info.AddValue(nameof(ParentId), ParentId);
        }

        /// <summary>
        /// Randomly determines a factor by which the mass of this <see cref="Galaxy"/> will be
        /// multiplied due to the abundance of dark matter.
        /// </summary>
        /// <returns>
        /// A factor by which the mass of this <see cref="Galaxy"/> will be multiplied due to the
        /// abundance of dark matter.
        /// </returns>
        private protected virtual Number GenerateDarkMatterMultiplier() => Randomizer.Instance.NextNumber(5, 15);

        /// <summary>
        /// Generates the central gravitational object of this <see cref="Galaxy"/>, which all others orbit.
        /// </summary>
        private protected virtual async Task<BlackHole?> GenerateGalacticCoreAsync()
            => await GetNewInstanceAsync<SupermassiveBlackHole>(this, Vector3.Zero).ConfigureAwait(false);

        private protected override async ValueTask<(double density, Number mass, IShape shape)> GetMatterAsync()
        {
            var shape = await GetShapeAsync().ConfigureAwait(false);
            var core = await GetGalacticCoreAsync().ConfigureAwait(false);
            var mass = ((shape.Volume * _ChildDensity * new Number(1, 30)) + (core?.Mass ?? 0)) * GenerateDarkMatterMultiplier();
            return ((double)(mass / shape.Volume), mass, shape);
        }

        private protected override ValueTask<IShape> GetShapeAsync()
        {
            var radius = Randomizer.Instance.NextNumber(new Number(1.55, 19), new Number(1.55, 21)); // ~1600–160000 ly
            var axis = radius * Randomizer.Instance.NormalDistributionSample(0.02, 0.001);
            return new ValueTask<IShape>(new Ellipsoid(radius, axis, Position));
        }

        private protected override ISubstanceReference? GetSubstance()
            => Substances.GetMixtureReference(Substances.Mixtures.InterstellarMedium);

        private protected override async Task InitializeAsync()
        {
            await GetGalacticCoreAsync().ConfigureAwait(false);
            await base.InitializeAsync().ConfigureAwait(false);
        }

        private protected override async Task InitializeChildAsync(CelestialLocation child)
        {
            if (!string.IsNullOrEmpty(_galacticCoreId))
            {
                var core = await GetGalacticCoreAsync().ConfigureAwait(false);
                await Space.Orbit.SetOrbitAsync(
                      child,
                      core,
                      Randomizer.Instance.NextDouble(0.1)).ConfigureAwait(false);
            }
        }
    }
}
