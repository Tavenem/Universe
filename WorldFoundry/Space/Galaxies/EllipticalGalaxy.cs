using NeverFoundry.MathAndScience.Chemistry;
using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.MathAndScience.Randomization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using WorldFoundry.CelestialBodies.BlackHoles;
using WorldFoundry.CelestialBodies.Planetoids.Planets.GiantPlanets;
using WorldFoundry.CelestialBodies.Planetoids.Planets.TerrestrialPlanets;
using WorldFoundry.CelestialBodies.Stars;
using WorldFoundry.Place;

namespace WorldFoundry.Space.Galaxies
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

        private static readonly List<ChildDefinition> _ChildDefinitions = new List<ChildDefinition>
        {
            new ChildDefinition(typeof(GiantPlanet), GiantPlanet.Space, _RogueDensity * 5 / 12),
            new ChildDefinition(typeof(IceGiant), GiantPlanet.Space, _RogueDensity * new Number(25, -2)),
            new ChildDefinition(typeof(TerrestrialPlanet), TerrestrialPlanet.Space, _RogueDensity / 6),
            new ChildDefinition(typeof(OceanPlanet), TerrestrialPlanet.Space, _RogueDensity / 24),
            new ChildDefinition(typeof(IronPlanet), TerrestrialPlanet.Space, _RogueDensity / 24),
            new ChildDefinition(typeof(CarbonPlanet), TerrestrialPlanet.Space, _RogueDensity / 12),

            new ChildDefinition(typeof(StarSystem), StarSystem.Space, _ChildDensity * new Number(19, -2), typeof(BrownDwarf)),

            new ChildDefinition(typeof(StarSystem), StarSystem.Space, _RedDensity * new Number(998, -3), typeof(Star), SpectralClass.M, LuminosityClass.V),
            new ChildDefinition(typeof(StarSystem), StarSystem.Space, _RedDensity * new Number(2, -3), typeof(Star), SpectralClass.M, LuminosityClass.sd),

            new ChildDefinition(typeof(StarSystem), StarSystem.Space, _KDensity * new Number(987, -3), typeof(Star), SpectralClass.K, LuminosityClass.V),
            new ChildDefinition(typeof(StarSystem), StarSystem.Space, _KDensity * new Number(1, -2), typeof(Star), SpectralClass.K, LuminosityClass.IV),
            new ChildDefinition(typeof(StarSystem), StarSystem.Space, _KDensity * new Number(3, -3), typeof(Star), SpectralClass.K, LuminosityClass.sd),

            new ChildDefinition(typeof(StarSystem), StarSystem.Space, _ChildDensity * new Number(18, -3), typeof(WhiteDwarf)),

            new ChildDefinition(typeof(StarSystem), StarSystem.Space, _GDensity * new Number(992, -3), typeof(Star), SpectralClass.G, LuminosityClass.V),
            new ChildDefinition(typeof(StarSystem), StarSystem.Space, _GDensity * new Number(8, -3), typeof(Star), SpectralClass.G, LuminosityClass.IV),

            new ChildDefinition(typeof(StarSystem), StarSystem.Space, _FDensity * new Number(982, -3), typeof(Star), SpectralClass.F, LuminosityClass.V),
            new ChildDefinition(typeof(StarSystem), StarSystem.Space, _FDensity * new Number(18, -3), typeof(Star), SpectralClass.F, LuminosityClass.IV),

            new ChildDefinition(typeof(StarSystem), StarSystem.Space, _ChildDensity * new Number(14, -4), typeof(NeutronStar)),

            new ChildDefinition(typeof(StarSystem), StarSystem.Space, _RedGiantDensity * new Number(9997, -4), typeof(RedGiant)),
            new ChildDefinition(typeof(StarSystem), StarSystem.Space, _RedGiantDensity * new Number(3, -4), typeof(RedGiant), null, LuminosityClass.II),

            new ChildDefinition(typeof(BlackHole), BlackHole.Space, _ChildDensity * new Number(1, -4)),

            new ChildDefinition(typeof(PlanetaryNebula), PlanetaryNebula.Space, _ChildDensity * new Number(3, -8)),
        };

        private protected override string BaseTypeName => "Elliptical Galaxy";

        private protected override IEnumerable<ChildDefinition> ChildDefinitions
            => base.ChildDefinitions.Concat(_ChildDefinitions);

        /// <summary>
        /// Initializes a new instance of <see cref="EllipticalGalaxy"/>.
        /// </summary>
        internal EllipticalGalaxy() { }

        /// <summary>
        /// Initializes a new instance of <see cref="EllipticalGalaxy"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="Location"/> in which this <see cref="EllipticalGalaxy"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="EllipticalGalaxy"/>.</param>
        internal EllipticalGalaxy(Location parent, Vector3 position) : base(parent, position) { }

        private EllipticalGalaxy(
            string id,
            string? name,
            string galacticCoreId,
            bool isPrepopulated,
            double? albedo,
            Vector3 velocity,
            Orbit? orbit,
            IMaterial? material,
            List<Location>? children)
            : base(
                id,
                name,
                galacticCoreId,
                isPrepopulated,
                albedo,
                velocity,
                orbit,
                material,
                children) { }

        private EllipticalGalaxy(SerializationInfo info, StreamingContext context) : this(
            (string)info.GetValue(nameof(Id), typeof(string)),
            (string?)info.GetValue(nameof(Name), typeof(string)),
            (string)info.GetValue(nameof(_galacticCoreId), typeof(string)),
            (bool)info.GetValue(nameof(_isPrepopulated), typeof(bool)),
            (double?)info.GetValue(nameof(Albedo), typeof(double?)),
            (Vector3)info.GetValue(nameof(Velocity), typeof(Vector3)),
            (Orbit?)info.GetValue(nameof(Orbit), typeof(Orbit?)),
            (IMaterial?)info.GetValue(nameof(Material), typeof(IMaterial)),
            (List<Location>)info.GetValue(nameof(Children), typeof(List<Location>))) { }

        private protected override IShape GetShape()
        {
            var radius = Randomizer.Instance.NextNumber(new Number(1.5, 18), new Number(1.5, 21)); // ~160–160000 ly
            var axis = radius * Randomizer.Instance.NormalDistributionSample(0.5, 1);
            return new Ellipsoid(radius, axis, Position);
        }
    }
}
