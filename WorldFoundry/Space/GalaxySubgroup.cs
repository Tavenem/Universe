using NeverFoundry.MathAndScience.Chemistry;
using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.MathAndScience.Randomization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Permissions;
using WorldFoundry.Place;
using WorldFoundry.Space.Galaxies;

namespace WorldFoundry.Space
{
    /// <summary>
    /// A collection of dwarf galaxies and globular clusters orbiting a large main galaxy.
    /// </summary>
    [Serializable]
    public class GalaxySubgroup : CelestialLocation
    {
        internal static readonly Number Space = new Number(2.5, 22);

        private static readonly Number _ChildDensity = new Number(1, -70);

        private static readonly List<ChildDefinition> _ChildDefinitions = new List<ChildDefinition>
        {
            new ChildDefinition(typeof(DwarfGalaxy), DwarfGalaxy.Space, _ChildDensity * new Number(26, -2)),
            new ChildDefinition(typeof(GlobularCluster), GlobularCluster.Space, _ChildDensity * new Number(74, -2)),
        };

        private string? _mainGalaxyId;
        /// <summary>
        /// The main <see cref="Galaxy"/> around which the other objects in this <see
        /// cref="GalaxySubgroup"/> orbit.
        /// </summary>
        public Galaxy MainGalaxy
        {
            get
            {
                _mainGalaxyId ??= GetMainGalaxy();
                return CelestialChildren.OfType<Galaxy>().FirstOrDefault(x => x.Id == _mainGalaxyId);
            }
        }

        private protected override string BaseTypeName => "Galaxy Subgroup";

        private protected override IEnumerable<ChildDefinition> ChildDefinitions
            => base.ChildDefinitions.Concat(_ChildDefinitions);

        /// <summary>
        /// Initializes a new instance of <see cref="GalaxySubgroup"/>.
        /// </summary>
        internal GalaxySubgroup() { }

        /// <summary>
        /// Initializes a new instance of <see cref="GalaxySubgroup"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="Location"/> in which this <see cref="GalaxySubgroup"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="GalaxySubgroup"/>.</param>
        internal GalaxySubgroup(Location parent, Vector3 position) : base(parent, position) { }

        private GalaxySubgroup(
            string id,
            string? name,
            string mainGalaxyId,
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
                children) => _mainGalaxyId = mainGalaxyId;

        private GalaxySubgroup(SerializationInfo info, StreamingContext context) : this(
            (string)info.GetValue(nameof(Id), typeof(string)),
            (string?)info.GetValue(nameof(Name), typeof(string)),
            (string)info.GetValue(nameof(_mainGalaxyId), typeof(string)),
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
            info.AddValue(nameof(_mainGalaxyId), _mainGalaxyId);
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
            if (child is null)
            {
                return null;
            }

            WorldFoundry.Space.Orbit.SetOrbit(
                child,
                MainGalaxy,
                Randomizer.Instance.NextDouble(0.1));

            return child;
        }

        internal override void PrepopulateRegion()
        {
            if (_isPrepopulated)
            {
                return;
            }
            base.PrepopulateRegion();

            _mainGalaxyId ??= GetMainGalaxy();
        }

        /// <summary>
        /// Randomly determines the main <see cref="Galaxy"/> of this <see cref="GalaxySubgroup"/>,
        /// which all other objects orbit.
        /// </summary>
        /// <remarks>70% of large galaxies are spirals.</remarks>
        private string GetMainGalaxy()
            => (Randomizer.Instance.NextDouble() <= 0.7
                ? new SpiralGalaxy(this, Vector3.Zero)
                : (Galaxy)new EllipticalGalaxy(this, Vector3.Zero)).Id;

        // The main galaxy is expected to comprise the bulk of the mass
        private protected override Number GetMass() => MainGalaxy.Mass * new Number(125, -2);

        private protected override IShape GetShape() => new Sphere(MainGalaxy.Shape.ContainingRadius * 10, Position);

        private protected override ISubstanceReference? GetSubstance()
            => Substances.GetMixtureReference(Substances.Mixtures.IntraclusterMedium);
    }
}
