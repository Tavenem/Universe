using MathAndScience.Shapes;
using Substances;
using System.Collections.Generic;
using System.Linq;
using MathAndScience.Numerics;
using WorldFoundry.CelestialBodies.Planetoids;
using WorldFoundry.CelestialBodies.Planetoids.Asteroids;
using WorldFoundry.CelestialBodies.Planetoids.Planets.DwarfPlanets;
using WorldFoundry.CelestialBodies.Stars;
using WorldFoundry.Orbits;
using WorldFoundry.Substances;

namespace WorldFoundry.Space.AsteroidFields
{
    /// <summary>
    /// A region of space with a high concentration of asteroids.
    /// </summary>
    /// <remarks>
    /// Asteroid fields are unusual <see cref="CelestialRegion"/> s in that they never have children
    /// of their own. Instead, the children they generate are placed inside their parent object. This
    /// allows star systems to define asteroid belts and fields which generate individual children
    /// only as needed, but when those individual bodies are created, they are placed in appropriate
    /// orbits within the solar system, rather than maintaining a static position within the asteroid
    /// field's own local space.
    /// </remarks>
    public class AsteroidField : CelestialRegion
    {
        private const string AsteroidBeltTypeName = "Asteroid Belt";
        private const double ChildDensity = 5.8e-26;

        private static readonly List<ChildDefinition> _childDefinitions = new List<ChildDefinition>
        {
            new ChildDefinition(typeof(CTypeAsteroid), Asteroid.Space, ChildDensity * 0.74),
            new ChildDefinition(typeof(STypeAsteroid), Asteroid.Space, ChildDensity * 0.14),
            new ChildDefinition(typeof(MTypeAsteroid), Asteroid.Space, ChildDensity * 0.1),
            new ChildDefinition(typeof(Comet), Comet.Space, ChildDensity * 0.02),
            new ChildDefinition(typeof(DwarfPlanet), DwarfPlanet.Space, ChildDensity * 3e-10),
            new ChildDefinition(typeof(DwarfPlanet), DwarfPlanet.Space, ChildDensity * 1e-10),
        };

        private readonly double? _majorRadius;
        private readonly double? _minorRadius;

        private const string _baseTypeName = "Asteroid Field";
        /// <summary>
        /// The base name for this type of <see cref="CelestialEntity"/>.
        /// </summary>
        public override string BaseTypeName => _baseTypeName;

        /// <summary>
        /// The types of children found in this region.
        /// </summary>
        public override IEnumerable<ChildDefinition> ChildDefinitions
            => base.ChildDefinitions.Concat(_childDefinitions);

        /// <summary>
        /// The star around which this <see cref="AsteroidField"/> orbits, if any.
        /// </summary>
        public Star Star { get; protected set; }

        /// <summary>
        /// The name for this type of <see cref="CelestialEntity"/>.
        /// </summary>
        public override string TypeName => Star == null ? BaseTypeName : AsteroidBeltTypeName;

        /// <summary>
        /// Initializes a new instance of <see cref="AsteroidField"/>.
        /// </summary>
        public AsteroidField() { }

        /// <summary>
        /// Initializes a new instance of <see cref="AsteroidField"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="AsteroidField"/> is located.
        /// </param>
        public AsteroidField(CelestialRegion parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="AsteroidField"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="AsteroidField"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="AsteroidField"/>.</param>
        public AsteroidField(CelestialRegion parent, Vector3 position) : base(parent, position) { }

        /// <summary>
        /// Initializes a new instance of <see cref="AsteroidField"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="AsteroidField"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="AsteroidField"/>.</param>
        /// <param name="majorRadius">
        /// The length of the major radius of this <see cref="AsteroidField"/>, in meters.
        /// </param>
        /// <param name="minorRadius">
        /// The length of the minor radius of this <see cref="AsteroidField"/>, in meters.
        /// </param>
        /// <param name="star">
        /// The star around which this <see cref="AsteroidField"/> orbits, if any.
        /// </param>
        public AsteroidField(CelestialRegion parent, Vector3 position, Star star, double majorRadius, double? minorRadius = null) : base(parent, position)
        {
            Star = star;
            _majorRadius = majorRadius;
            _minorRadius = minorRadius;
            GenerateSubstance();
        }

        internal override Orbiter GenerateChild(ChildDefinition definition)
        {
            var child = base.GenerateChild(definition);

            if (Orbit != null)
            {
                child.GenerateOrbit(Orbit.OrbitedObject);
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

        /// <summary>
        /// Generates the <see cref="CelestialEntity.Substance"/> of this <see cref="CelestialEntity"/>.
        /// </summary>
        private protected override void GenerateSubstance()
        {
            Substance = new Substance { Composition = CosmicSubstances.InterplanetaryMedium.GetDeepCopy() };

            if (Parent == null || !(Parent is StarSystem) || Position != Vector3.Zero)
            {
                var axis = _majorRadius ?? Randomizer.Instance.NextDouble(1.5e11, 3.15e12);
                Shape = new Ellipsoid(axis, Randomizer.Instance.NextDouble(0.5, 1.5) * axis, Randomizer.Instance.NextDouble(0.5, 1.5) * axis);
            }
            else
            {
                Shape = new Torus(_majorRadius ?? 0, _minorRadius ?? 0);
            }

            Substance.Mass = Shape.Volume * 7.0e-8;
        }
    }
}
