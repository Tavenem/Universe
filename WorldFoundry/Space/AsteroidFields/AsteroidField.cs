using System;
using System.Collections.Generic;
using System.Numerics;
using WorldFoundry.CelestialBodies.Planetoids;
using WorldFoundry.CelestialBodies.Planetoids.Asteroids;
using WorldFoundry.CelestialBodies.Planetoids.Planets.DwarfPlanets;
using WorldFoundry.CelestialBodies.Stars;
using WorldFoundry.Utilities;
using WorldFoundry.Utilities.MathUtil.Shapes;

namespace WorldFoundry.Space.AsteroidFields
{
    /// <summary>
    /// A region of space with a high concentration of asteroids.
    /// </summary>
    /// <remarks>
    /// Asteroid fields are unusual <see cref="CelestialObject"/> s in that they never have children
    /// of their own. Instead, the children they generate are placed inside their parent object. This
    /// allows star systems to define asteroid belts and fields which generate individual children
    /// only as needed, but when those individual bodies are created, they are placed in appropriate
    /// orbits within the solar system, rather than maintaining a static position within the asteroid
    /// field's own local space.
    /// </remarks>
    public class AsteroidField : CelestialObject
    {
        public static string AsteroidBeltTypeName => "Asteroid Belt";

        internal new const string baseTypeName = "Asteroid Field";
        /// <summary>
        /// The base name for this type of <see cref="CelestialEntity"/>.
        /// </summary>
        public override string BaseTypeName => baseTypeName;

        internal new static IDictionary<Type, float> childPossibilities = new Dictionary<Type, float>
        {
            { typeof(CTypeAsteroid), 0.74f },
            { typeof(STypeAsteroid), 0.14f },
            { typeof(MTypeAsteroid), 0.1f },
            { typeof(Comet), 0.0199999996f },
            { typeof(DwarfPlanet), 3.0e-10f },
            { typeof(RockyDwarfPlanet), 1.0e-10f },
        };
        /// <summary>
        /// The types of children this region of space might have.
        /// </summary>
        public override IDictionary<Type, float> ChildPossibilities => childPossibilities;

        /// <summary>
        /// The average number of children within the grid per m³.
        /// </summary>
        public override double ChildDensity => 5.8e-26;

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
        /// The containing <see cref="CelestialObject"/> in which this <see cref="AsteroidField"/> is located.
        /// </param>
        public AsteroidField(CelestialObject parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="AsteroidField"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="AsteroidField"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="AsteroidField"/>.</param>
        public AsteroidField(CelestialObject parent, Vector3 position) : base(parent, position) { }

        /// <summary>
        /// Initializes a new instance of <see cref="AsteroidField"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="AsteroidField"/> is located.
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
        public AsteroidField(CelestialObject parent, Vector3 position, Star star, double majorRadius, double? minorRadius = null) : base(parent, position)
        {
            Star = star;
            GenerateShape(majorRadius, minorRadius);
        }

        /// <summary>
        /// Generates a child of the specified type within this <see cref="CelestialObject"/>.
        /// </summary>
        /// <param name="type">
        /// The type of child to generate. Does not need to be one of this object's usual child
        /// types, but must be a subclass of <see cref="CelestialObject"/> or <see cref="CelestialBody"/>.
        /// </param>
        /// <param name="position">
        /// The location at which to generate the child. If null, a randomly-selected free space will
        /// be selected.
        /// </param>
        /// <param name="orbitParameters">
        /// An optional list of parameters which describe the child's orbit. May be null.
        /// </param>
        public override BioZone GenerateChildOfType(Type type, Vector3? position, List<object> orbitParameters = null)
        {
            var child = base.GenerateChildOfType(type, position, orbitParameters);

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
        /// Generates the <see cref="Mass"/> of this <see cref="Orbiter"/>.
        /// </summary>
        protected override void GenerateMass() => Mass = Shape.GetVolume() * 7.0e-8;

        private void GenerateShape(double? majorRadius, double? minorRadius)
        {
            if (Parent == null || !(Parent is StarSystem) || Position != Vector3.Zero)
            {
                var axis = majorRadius ?? Randomizer.Static.NextDouble(1.5e11, 3.15e12);
                Shape = new Ellipsoid(axis, Randomizer.Static.NextDouble(0.5, 1.5) * axis, Randomizer.Static.NextDouble(0.5, 1.5) * axis);
            }
            else
            {
                Shape = new Torus(majorRadius ?? 0, minorRadius ?? 0);
            }
        }

        /// <summary>
        /// Generates the <see cref="Utilities.MathUtil.Shapes.Shape"/> of this <see cref="CelestialEntity"/>.
        /// </summary>
        protected override void GenerateShape() => GenerateShape(null, null);
    }
}
