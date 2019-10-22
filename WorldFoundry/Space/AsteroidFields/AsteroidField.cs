using NeverFoundry.MathAndScience.Chemistry;
using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.MathAndScience.Randomization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Permissions;
using WorldFoundry.CelestialBodies.Planetoids;
using WorldFoundry.CelestialBodies.Planetoids.Asteroids;
using WorldFoundry.CelestialBodies.Planetoids.Planets.DwarfPlanets;
using WorldFoundry.CelestialBodies.Stars;
using WorldFoundry.Place;

namespace WorldFoundry.Space.AsteroidFields
{
    /// <summary>
    /// A region of space with a high concentration of asteroids.
    /// </summary>
    /// <remarks>
    /// Asteroid fields are unusual in that they never have children of their own. Instead, the
    /// children they generate are placed inside their parent object. This allows star systems to
    /// define asteroid belts and fields which generate individual children only as needed, but when
    /// those individual bodies are created, they are placed in appropriate orbits within the solar
    /// system, rather than maintaining a static position within the asteroid field's own local
    /// space.
    /// </remarks>
    [Serializable]
    public class AsteroidField : CelestialLocation
    {
        private const string AsteroidBeltTypeName = "Asteroid Belt";

        private static readonly Number _ChildDensity = new Number(5.8, -26);

        private static readonly List<ChildDefinition> _ChildDefinitions = new List<ChildDefinition>
        {
            new ChildDefinition(typeof(CTypeAsteroid), Asteroid.Space, _ChildDensity * new Number(74, -2)),
            new ChildDefinition(typeof(STypeAsteroid), Asteroid.Space, _ChildDensity * new Number(14, -2)),
            new ChildDefinition(typeof(MTypeAsteroid), Asteroid.Space, _ChildDensity * Number.Deci),
            new ChildDefinition(typeof(Comet), Comet.Space, _ChildDensity * new Number(2, -2)),
            new ChildDefinition(typeof(DwarfPlanet), DwarfPlanet.Space, _ChildDensity * new Number(3, -10)),
            new ChildDefinition(typeof(DwarfPlanet), DwarfPlanet.Space, _ChildDensity * new Number(1, -10)),
        };

        private protected Number? _majorRadius, _minorRadius;

        private protected string? _starId;
        /// <summary>
        /// The star around which this <see cref="AsteroidField"/> orbits, if any.
        /// </summary>
        public Star? Star => string.IsNullOrEmpty(_starId)
            ? null
            : CelestialParent?.CelestialChildren.OfType<Star>().FirstOrDefault(x => x.Id == _starId);

        /// <summary>
        /// The name for this type of <see cref="CelestialLocation"/>.
        /// </summary>
        public override string TypeName => Star == null ? BaseTypeName : AsteroidBeltTypeName;

        private protected override IEnumerable<ChildDefinition> ChildDefinitions
            => base.ChildDefinitions.Concat(_ChildDefinitions);

        private protected override string BaseTypeName => "Asteroid Field";

        /// <summary>
        /// Initializes a new instance of <see cref="AsteroidField"/>.
        /// </summary>
        internal AsteroidField() { }

        /// <summary>
        /// Initializes a new instance of <see cref="AsteroidField"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="Location"/> in which this <see cref="AsteroidField"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="AsteroidField"/>.</param>
        internal AsteroidField(Location parent, Vector3 position) : base(parent, position) { }

        /// <summary>
        /// Initializes a new instance of <see cref="AsteroidField"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="Location"/> in which this <see cref="AsteroidField"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="AsteroidField"/>.</param>
        /// <param name="star">
        /// The star around which this <see cref="AsteroidField"/> orbits, if any.
        /// </param>
        /// <param name="majorRadius">
        /// The length of the major radius of this <see cref="AsteroidField"/>, in meters.
        /// </param>
        /// <param name="minorRadius">
        /// The length of the minor radius of this <see cref="AsteroidField"/>, in meters.
        /// </param>
        public AsteroidField(Location parent, Vector3 position, Star star, Number majorRadius, Number? minorRadius = null) : base(parent, position)
        {
            _starId = star.Id;
            _majorRadius = majorRadius;
            _minorRadius = minorRadius;
        }

        private protected AsteroidField(
            string id,
            string? name,
            string starId,
            bool isPrepopulated,
            double? albedo,
            Vector3 velocity,
            Orbit? orbit,
            IMaterial? material,
            Number? majorRadius,
            Number? minorRadius,
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
        {
            _starId = starId;
            _majorRadius = majorRadius;
            _minorRadius = minorRadius;
        }

        private AsteroidField(SerializationInfo info, StreamingContext context) : this(
            (string)info.GetValue(nameof(Id), typeof(string)),
            (string?)info.GetValue(nameof(Name), typeof(string)),
            (string)info.GetValue(nameof(_starId), typeof(string)),
            (bool)info.GetValue(nameof(_isPrepopulated), typeof(bool)),
            (double?)info.GetValue(nameof(Albedo), typeof(double?)),
            (Vector3)info.GetValue(nameof(Velocity), typeof(Vector3)),
            (Orbit?)info.GetValue(nameof(Orbit), typeof(Orbit?)),
            (IMaterial?)info.GetValue(nameof(Material), typeof(IMaterial)),
            (Number?)info.GetValue(nameof(_majorRadius), typeof(Number?)),
            (Number?)info.GetValue(nameof(_minorRadius), typeof(Number?)),
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
            info.AddValue(nameof(_starId), _starId);
            info.AddValue(nameof(_isPrepopulated), _isPrepopulated);
            info.AddValue(nameof(Albedo), _albedo);
            info.AddValue(nameof(Velocity), Velocity);
            info.AddValue(nameof(Orbit), _orbit);
            info.AddValue(nameof(Material), Material);
            info.AddValue(nameof(_majorRadius), _majorRadius);
            info.AddValue(nameof(_minorRadius), _minorRadius);
            info.AddValue(nameof(Children), Children.ToList());
        }

        internal override CelestialLocation? GenerateChild(ChildDefinition definition)
        {
            var child = base.GenerateChild(definition);
            if (child is null)
            {
                return child;
            }

            if (Orbit.HasValue)
            {
                child.GenerateOrbit(Orbit.Value.OrbitedObject);
            }
            else if (Star != null)
            {
                child.GenerateOrbit(Star);
            }

            if (child is Planetoid p)
            {
                p.GenerateSatellites();
            }

            return child;
        }

        private protected override (double density, Number mass, IShape shape) GetMatter()
        {
            var shape = GetShape();
            var mass = shape.Volume * new Number(7, -8);
            return ((double)(mass / shape.Volume), mass, shape);
        }

        private protected override IShape GetShape() => GetShape(null, null);

        private protected IShape GetShape(Number? majorRadius, Number? minorRadius)
        {
            IShape shape;
            if (!(CelestialParent is StarSystem) || Position != Vector3.Zero)
            {
                var axis = majorRadius ?? Randomizer.Instance.NextNumber(new Number(1.5, 11), new Number(3.15, 12));
                shape = new Ellipsoid(axis, Randomizer.Instance.NextNumber(Number.Half, new Number(15, -1)) * axis, Randomizer.Instance.NextNumber(Number.Half, new Number(15, -1)) * axis, Position);
            }
            else
            {
                shape = new Torus(majorRadius ?? 0, minorRadius ?? 0, Position);
            }
            _majorRadius = null;
            _minorRadius = null;
            return shape;
        }

        private protected override ISubstanceReference? GetSubstance()
            => Substances.GetMixtureReference(Substances.Mixtures.InterplanetaryMedium);
    }
}
