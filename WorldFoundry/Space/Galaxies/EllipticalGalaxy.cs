using NeverFoundry.MathAndScience.Chemistry;
using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.MathAndScience.Randomization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using NeverFoundry.WorldFoundry.CelestialBodies.BlackHoles;
using NeverFoundry.WorldFoundry.CelestialBodies.Planetoids.Planets.GiantPlanets;
using NeverFoundry.WorldFoundry.CelestialBodies.Planetoids.Planets.TerrestrialPlanets;
using NeverFoundry.WorldFoundry.CelestialBodies.Stars;

namespace NeverFoundry.WorldFoundry.Space.Galaxies
{
    /// <summary>
    /// An elliptical, gravitationally-bound collection of stars, gas, dust, and dark matter.
    /// </summary>
    [Serializable]
    public class EllipticalGalaxy : Galaxy
    {
        private static readonly Number _ChildDensity = new Number(4, -50);
        private static readonly Number _RogueDensity = _ChildDensity * new Number(6, -1);
        private static readonly Number _RedDensity = _ChildDensity * new Number(1444, -4);
        private static readonly Number _KDensity = _ChildDensity * new Number(24, -3);
        private static readonly Number _GDensity = _ChildDensity * new Number(15, -3);
        private static readonly Number _FDensity = _ChildDensity * new Number(6, -3);
        private static readonly Number _RedGiantDensity = _ChildDensity * new Number(1, -3);

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

            new StarSystemChildDefinition<RedGiant>(_RedGiantDensity * new Number(9997, -4)),
            new StarSystemChildDefinition<RedGiant>(_RedGiantDensity * new Number(3, -4), null, LuminosityClass.II),

            new ChildDefinition<BlackHole>(BlackHole.Space, _ChildDensity * new Number(1, -4)),

            new ChildDefinition<PlanetaryNebula>(PlanetaryNebula.Space, _ChildDensity * new Number(3, -8)),
        };

        private protected override string BaseTypeName => "Elliptical Galaxy";

        private protected override IEnumerable<IChildDefinition> ChildDefinitions => _ChildDefinitions;

        /// <summary>
        /// Initializes a new instance of <see cref="EllipticalGalaxy"/>.
        /// </summary>
        internal EllipticalGalaxy() { }

        /// <summary>
        /// Initializes a new instance of <see cref="EllipticalGalaxy"/> with the given parameters.
        /// </summary>
        /// <param name="parentId">The id of the location which contains this one.</param>
        /// <param name="position">The initial position of this <see cref="EllipticalGalaxy"/>.</param>
        internal EllipticalGalaxy(string? parentId, Vector3 position) : base(parentId, position) { }

        private EllipticalGalaxy(
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
                galacticCoreId,
                isPrepopulated,
                albedo,
                velocity,
                orbit,
                material,
                parentId) { }

        private EllipticalGalaxy(SerializationInfo info, StreamingContext context) : this(
            (string)info.GetValue(nameof(Id), typeof(string)),
            (string?)info.GetValue(nameof(Name), typeof(string)),
            (string)info.GetValue(nameof(_galacticCoreId), typeof(string)),
            (bool)info.GetValue(nameof(_isPrepopulated), typeof(bool)),
            (double?)info.GetValue(nameof(Albedo), typeof(double?)),
            (Vector3)info.GetValue(nameof(Velocity), typeof(Vector3)),
            (Orbit?)info.GetValue(nameof(Orbit), typeof(Orbit?)),
            (IMaterial?)info.GetValue(nameof(_material), typeof(IMaterial)),
            (string)info.GetValue(nameof(ParentId), typeof(string))) { }

        private protected override ValueTask<IShape> GetShapeAsync()
        {
            var radius = Randomizer.Instance.NextNumber(new Number(1.5, 18), new Number(1.5, 21)); // ~160–160000 ly
            var axis = radius * Randomizer.Instance.NormalDistributionSample(0.5, 1);
            return new ValueTask<IShape>(new Ellipsoid(radius, axis, Position));
        }
    }
}
