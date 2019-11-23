using NeverFoundry.MathAndScience.Chemistry;
using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.MathAndScience.Randomization;
using NeverFoundry.WorldFoundry.CelestialBodies.Planetoids;
using NeverFoundry.WorldFoundry.CelestialBodies.Planetoids.Asteroids;
using NeverFoundry.WorldFoundry.CelestialBodies.Planetoids.Planets.DwarfPlanets;
using NeverFoundry.WorldFoundry.Place;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Threading.Tasks;

namespace NeverFoundry.WorldFoundry.Space.AsteroidFields
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

        private static readonly List<IChildDefinition> _ChildDefinitions = new List<IChildDefinition>
        {
            new ChildDefinition<CTypeAsteroid>(Asteroid.Space, _ChildDensity * new Number(74, -2)),
            new ChildDefinition<STypeAsteroid>(Asteroid.Space, _ChildDensity * new Number(14, -2)),
            new ChildDefinition<MTypeAsteroid>(Asteroid.Space, _ChildDensity * Number.Deci),
            new ChildDefinition<Comet>(Comet.Space, _ChildDensity * new Number(2, -2)),
            new ChildDefinition<DwarfPlanet>(DwarfPlanet.Space, _ChildDensity * new Number(3, -10)),
            new ChildDefinition<DwarfPlanet>(DwarfPlanet.Space, _ChildDensity * new Number(1, -10)),
        };

        private protected Number? _majorRadius, _minorRadius;

        private protected string? _starId;

        /// <summary>
        /// The name for this type of <see cref="CelestialLocation"/>.
        /// </summary>
        public override string TypeName => Orbit?.OrbitedObjectId is null ? BaseTypeName : AsteroidBeltTypeName;

        private protected override IEnumerable<IChildDefinition> ChildDefinitions => _ChildDefinitions;

        private protected override string BaseTypeName => "Asteroid Field";

        /// <summary>
        /// Initializes a new instance of <see cref="AsteroidField"/>.
        /// </summary>
        internal AsteroidField() { }

        /// <summary>
        /// Initializes a new instance of <see cref="AsteroidField"/> with the given parameters.
        /// </summary>
        /// <param name="parentId">The id of the location which contains this one.</param>
        /// <param name="position">The initial position of this <see cref="AsteroidField"/>.</param>
        internal AsteroidField(string? parentId, Vector3 position) : base(parentId, position) { }

        private protected AsteroidField(
            string id,
            string? name,
            bool isPrepopulated,
            double? albedo,
            Vector3 velocity,
            Orbit? orbit,
            IMaterial? material,
            Number? majorRadius,
            Number? minorRadius,
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
        {
            _majorRadius = majorRadius;
            _minorRadius = minorRadius;
        }

        private AsteroidField(SerializationInfo info, StreamingContext context) : this(
            (string)info.GetValue(nameof(Id), typeof(string)),
            (string?)info.GetValue(nameof(Name), typeof(string)),
            (bool)info.GetValue(nameof(_isPrepopulated), typeof(bool)),
            (double?)info.GetValue(nameof(_albedo), typeof(double?)),
            (Vector3)info.GetValue(nameof(Velocity), typeof(Vector3)),
            (Orbit?)info.GetValue(nameof(Orbit), typeof(Orbit?)),
            (IMaterial?)info.GetValue(nameof(_material), typeof(IMaterial)),
            (Number?)info.GetValue(nameof(_majorRadius), typeof(Number?)),
            (Number?)info.GetValue(nameof(_minorRadius), typeof(Number?)),
            (string)info.GetValue(nameof(ParentId), typeof(string)))
        { }

        /// <summary>
        /// Gets a new <see cref="AsteroidField"/> instance.
        /// </summary>
        /// <param name="parent">The location which contains the new one.</param>
        /// <param name="position">The position of the new location relative to the center of its
        /// <paramref name="parent"/>.</param>
        /// <param name="majorRadius">
        /// The length of the major radius of this <see cref="AsteroidField"/>, in meters.
        /// </param>
        /// <param name="minorRadius">
        /// The length of the minor radius of this <see cref="AsteroidField"/>, in meters.
        /// </param>
        /// <param name="orbit">The orbit to set for the new <see cref="AsteroidField"/>, if
        /// any.</param>
        /// <returns>A new <see cref="AsteroidField"/> instance, or <see langword="null"/> if no
        /// instance could be generated with the given parameters.</returns>
        public static async Task<AsteroidField?> GetNewInstanceAsync(
            Location? parent,
            Vector3 position,
            Number majorRadius,
            Number? minorRadius = null,
            OrbitalParameters? orbit = null)
        {
            var instance = new AsteroidField(parent?.Id, position);
            if (instance != null)
            {
                instance._majorRadius = majorRadius;
                instance._minorRadius = minorRadius;
                await instance.GenerateMaterialAsync().ConfigureAwait(false);
                if (orbit.HasValue)
                {
                    await Space.Orbit.SetOrbitAsync(instance, orbit.Value).ConfigureAwait(false);
                }
                await instance.InitializeBaseAsync(parent).ConfigureAwait(false);
            }
            return instance;
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
            info.AddValue(nameof(_isPrepopulated), _isPrepopulated);
            info.AddValue(nameof(_albedo), _albedo);
            info.AddValue(nameof(Velocity), Velocity);
            info.AddValue(nameof(Orbit), _orbit);
            info.AddValue(nameof(_material), Material);
            info.AddValue(nameof(_majorRadius), _majorRadius);
            info.AddValue(nameof(_minorRadius), _minorRadius);
            info.AddValue(nameof(ParentId), ParentId);
        }

        private protected override async ValueTask<(double density, Number mass, IShape shape)> GetMatterAsync()
        {
            var shape = await GetShapeAsync().ConfigureAwait(false);
            var mass = shape.Volume * new Number(7, -8);
            return ((double)(mass / shape.Volume), mass, shape);
        }

        private protected override ValueTask<IShape> GetShapeAsync() => GetShapeAsync(_majorRadius, _minorRadius);

        private protected async ValueTask<IShape> GetShapeAsync(Number? majorRadius, Number? minorRadius)
        {
            IShape shape;
            if (Position != Vector3.Zero || !((await GetParentAsync().ConfigureAwait(false)) is StarSystem))
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
            => Substances.All.InterplanetaryMedium.GetReference();

        private protected override async Task InitializeChildAsync(CelestialLocation child)
        {
            if (Orbit.HasValue)
            {
                var orbited = await Orbit.Value.GetOrbitedObjectAsync().ConfigureAwait(false);
                if (orbited != null)
                {
                    await child.GenerateOrbitAsync(orbited).ConfigureAwait(false);
                }
            }
        }
    }
}
