using NeverFoundry.MathAndScience.Chemistry;
using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.MathAndScience.Randomization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Permissions;
using WorldFoundry.CelestialBodies.BlackHoles;
using WorldFoundry.CelestialBodies.Planetoids.Planets;
using WorldFoundry.CelestialBodies.Planetoids.Planets.GiantPlanets;
using WorldFoundry.CelestialBodies.Planetoids.Planets.TerrestrialPlanets;
using WorldFoundry.CelestialBodies.Stars;
using WorldFoundry.Place;

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

            new ChildDefinition(typeof(StarSystem), StarSystem.Space, _ChildDensity * new Number(115, -5), typeof(Star), SpectralClass.A, LuminosityClass.V),

            new ChildDefinition(typeof(StarSystem), StarSystem.Space, _RedGiantDensity * new Number(96, -2), typeof(RedGiant)),
            new ChildDefinition(typeof(StarSystem), StarSystem.Space, _RedGiantDensity * new Number(18, -3), typeof(RedGiant), null, LuminosityClass.II),
            new ChildDefinition(typeof(StarSystem), StarSystem.Space, _RedGiantDensity * new Number(16, -3), typeof(RedGiant), null, LuminosityClass.Ib),
            new ChildDefinition(typeof(StarSystem), StarSystem.Space, _RedGiantDensity * new Number(55, -4), typeof(RedGiant), null, LuminosityClass.Ia),
            new ChildDefinition(typeof(StarSystem), StarSystem.Space, _RedGiantDensity * new Number(5, -4), typeof(RedGiant), null, LuminosityClass.Zero),

            new ChildDefinition(typeof(StarSystem), StarSystem.Space, _BlueGiantDensity * new Number(95, -2), typeof(BlueGiant)),
            new ChildDefinition(typeof(StarSystem), StarSystem.Space, _BlueGiantDensity * new Number(25, -3), typeof(BlueGiant), null, LuminosityClass.II),
            new ChildDefinition(typeof(StarSystem), StarSystem.Space, _BlueGiantDensity * new Number(2, -2), typeof(BlueGiant), null, LuminosityClass.Ib),
            new ChildDefinition(typeof(StarSystem), StarSystem.Space, _BlueGiantDensity * new Number(45, -4), typeof(BlueGiant), null, LuminosityClass.Ia),
            new ChildDefinition(typeof(StarSystem), StarSystem.Space, _BlueGiantDensity * new Number(5, -4), typeof(BlueGiant), null, LuminosityClass.Zero),

            new ChildDefinition(typeof(StarSystem), StarSystem.Space, _YellowGiantDensity * new Number(95, -2), typeof(YellowGiant)),
            new ChildDefinition(typeof(StarSystem), StarSystem.Space, _YellowGiantDensity * new Number(2, -2), typeof(YellowGiant), null, LuminosityClass.II),
            new ChildDefinition(typeof(StarSystem), StarSystem.Space, _YellowGiantDensity * new Number(23, -3), typeof(YellowGiant), null, LuminosityClass.Ib),
            new ChildDefinition(typeof(StarSystem), StarSystem.Space, _YellowGiantDensity * new Number(6, -3), typeof(YellowGiant), null, LuminosityClass.Ia),
            new ChildDefinition(typeof(StarSystem), StarSystem.Space, _YellowGiantDensity * new Number(1, -3), typeof(YellowGiant), null, LuminosityClass.Zero),

            new ChildDefinition(typeof(StarSystem), StarSystem.Space, _ChildDensity * new Number(25, -5), typeof(Star), SpectralClass.B, LuminosityClass.V),

            new ChildDefinition(typeof(BlackHole), BlackHole.Space, _ChildDensity * new Number(1, -4)),

            new ChildDefinition(typeof(StarSystem), StarSystem.Space, _ChildDensity * new Number(7, -8), typeof(Star), SpectralClass.O, LuminosityClass.V),

            new ChildDefinition(typeof(PlanetaryNebula), PlanetaryNebula.Space, _ChildDensity * new Number(3, -8)),

            new ChildDefinition(typeof(Nebula), Nebula.Space, _ChildDensity * new Number(2, -10)),

            new ChildDefinition(typeof(HIIRegion), Nebula.Space, _ChildDensity * new Number(2, -10)),
        };

        private protected string? _galacticCoreId;
        /// <summary>
        /// The <see cref="BlackHole"/> which is at the center of this <see cref="Galaxy"/>.
        /// </summary>
        public BlackHole GalacticCore
        {
            get
            {
                _galacticCoreId ??= GetGalacticCore();
                return CelestialChildren.OfType<BlackHole>().FirstOrDefault(x => x.Id == _galacticCoreId);
            }
        }

        private protected override string BaseTypeName => "Galaxy";

        private protected override IEnumerable<ChildDefinition> ChildDefinitions
            => base.ChildDefinitions.Concat(_ChildDefinitions);

        /// <summary>
        /// Initializes a new instance of <see cref="Galaxy"/>.
        /// </summary>
        internal Galaxy() { }

        /// <summary>
        /// Initializes a new instance of <see cref="Galaxy"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="Location"/> in which this <see cref="Galaxy"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="Galaxy"/>.</param>
        internal Galaxy(Location parent, Vector3 position) : base(parent, position) { }

        private protected Galaxy(
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
                isPrepopulated,
                albedo,
                velocity,
                orbit,
                material,
                children)
            => _galacticCoreId = galacticCoreId;

        private Galaxy(SerializationInfo info, StreamingContext context) : this(
            (string)info.GetValue(nameof(Id), typeof(string)),
            (string?)info.GetValue(nameof(Name), typeof(string)),
            (string)info.GetValue(nameof(_galacticCoreId), typeof(string)),
            (bool)info.GetValue(nameof(_isPrepopulated), typeof(bool)),
            (double?)info.GetValue(nameof(Albedo), typeof(double?)),
            (Vector3)info.GetValue(nameof(Velocity), typeof(Vector3)),
            (Orbit?)info.GetValue(nameof(Orbit), typeof(Orbit?)),
            (IMaterial?)info.GetValue(nameof(Material), typeof(IMaterial)),
            (List<Location>)info.GetValue(nameof(Children), typeof(List<Location>))) { }

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
            info.AddValue(nameof(Material), Material);
            info.AddValue(nameof(Children), Children.ToList());
        }

        internal override CelestialLocation? GenerateChild(ChildDefinition definition)
        {
            var child = base.GenerateChild(definition);
            if (child == null)
            {
                return null;
            }

            Space.Orbit.SetOrbit(
                child,
                GalacticCore,
                Randomizer.Instance.NextDouble(0.1));

            // Small chance of satellites for rogue planets.
            if (child is Planemo planemo && Randomizer.Instance.NextDouble() <= 0.2)
            {
                planemo.GenerateSatellites();
            }

            return child;
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
        private protected virtual string GetGalacticCore()
            => new SupermassiveBlackHole(this, Vector3.Zero).Id;

        private protected override (double density, Number mass, IShape shape) GetMatter()
        {
            var shape = GetShape();
            var mass = ((shape.Volume * _ChildDensity * new Number(1, 30)) + GalacticCore.Mass) * GenerateDarkMatterMultiplier();
            return ((double)(mass / shape.Volume), mass, shape);
        }

        private protected override IShape GetShape()
        {
            var radius = Randomizer.Instance.NextNumber(new Number(1.55, 19), new Number(1.55, 21)); // ~1600–160000 ly
            var axis = radius * Randomizer.Instance.NormalDistributionSample(0.02, 0.001);
            return new Ellipsoid(radius, axis, Position);
        }

        private protected override ISubstanceReference? GetSubstance()
            => Substances.GetMixtureReference(Substances.Mixtures.InterstellarMedium);
    }
}
