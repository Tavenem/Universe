using MathAndScience.Shapes;
using Substances;
using System.Collections.Generic;
using System.Linq;
using MathAndScience.Numerics;
using WorldFoundry.CelestialBodies.Planetoids;
using WorldFoundry.CelestialBodies.Planetoids.Asteroids;
using WorldFoundry.CelestialBodies.Planetoids.Planets.DwarfPlanets;
using WorldFoundry.CelestialBodies.Stars;
using WorldFoundry.Substances;
using WorldFoundry.CelestialBodies;

namespace WorldFoundry.Space.AsteroidFields
{
    /// <summary>
    /// A region of space with a high concentration of asteroids.
    /// </summary>
    /// <remarks>
    /// Asteroid fields are unusual <see cref="CelestialRegion"/>s in that they never have children
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

        /// <summary>
        /// The star around which this <see cref="AsteroidField"/> orbits, if any.
        /// </summary>
        public Star Star { get; protected set; }

        /// <summary>
        /// The name for this type of <see cref="ICelestialLocation"/>.
        /// </summary>
        public override string TypeName => Star == null ? BaseTypeName : AsteroidBeltTypeName;

        private protected override IEnumerable<ChildDefinition> ChildDefinitions
            => base.ChildDefinitions.Concat(_childDefinitions);

        private protected override string BaseTypeName => "Asteroid Field";

        /// <summary>
        /// Initializes a new instance of <see cref="AsteroidField"/>.
        /// </summary>
        internal AsteroidField() { }

        /// <summary>
        /// Initializes a new instance of <see cref="AsteroidField"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="AsteroidField"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="AsteroidField"/>.</param>
        internal AsteroidField(CelestialRegion parent, Vector3 position) : base(parent, position) { }

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
            _shape = GetShape(majorRadius, minorRadius);
        }

        internal override ICelestialLocation GenerateChild(ChildDefinition definition)
        {
            var child = base.GenerateChild(definition);
            if (!(child is CelestialBody body))
            {
                return child;
            }

            if (Orbit.HasValue)
            {
                body.GenerateOrbit(Orbit.Value.OrbitedObject);
            }
            else if (Star != null)
            {
                body.GenerateOrbit(Star);
            }

            if (body is Planetoid p)
            {
                p.GenerateSatellites();
            }

            return child;
        }

        private protected override double GetMass() => Shape.Volume * 7.0e-8;

        private protected override IShape GetShape() => GetShape(null, null);

        private protected IShape GetShape(double? majorRadius, double? minorRadius)
        {
            if (ContainingCelestialRegion == null || !(ContainingCelestialRegion is StarSystem) || Position != Vector3.Zero)
            {
                var axis = majorRadius ?? Randomizer.Instance.NextDouble(1.5e11, 3.15e12);
                return new Ellipsoid(axis, Randomizer.Instance.NextDouble(0.5, 1.5) * axis, Randomizer.Instance.NextDouble(0.5, 1.5) * axis, Position);
            }
            else
            {
                return new Torus(majorRadius ?? 0, minorRadius ?? 0, Position);
            }
        }
    }
}
