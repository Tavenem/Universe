using MathAndScience.MathUtil.Shapes;
using Substances;
using System;
using System.Collections.Generic;
using System.Numerics;
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

        private readonly double? majorRadius;
        private readonly double? minorRadius;

        private const string baseTypeName = "Asteroid Field";
        /// <summary>
        /// The base name for this type of <see cref="CelestialEntity"/>.
        /// </summary>
        public override string BaseTypeName => baseTypeName;

        private const double childDensity = 5.8e-26;
        /// <summary>
        /// The average number of children within the grid per m³.
        /// </summary>
        public override double ChildDensity => childDensity;

        internal static IList<(Type type, float proportion, object[] constructorParameters)> childPossibilities =
            new List<(Type type, float proportion, object[] constructorParameters)>
            {
                (typeof(CTypeAsteroid), 0.74f, null),
                (typeof(STypeAsteroid), 0.14f, null),
                (typeof(MTypeAsteroid), 0.1f, null),
                (typeof(Comet), 0.0199999996f, null),
                (typeof(DwarfPlanet), 3.0e-10f, null),
                (typeof(RockyDwarfPlanet), 1.0e-10f, null),
            };
        /// <summary>
        /// The types of children this region of space might have.
        /// </summary>
        public override IList<(Type type, float proportion, object[] constructorParameters)> ChildPossibilities => childPossibilities;

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
        public AsteroidField() : base() { }

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
            this.majorRadius = majorRadius;
            this.minorRadius = minorRadius;
            GenerateSubstance();
        }

        /// <summary>
        /// Generates a child of the specified type within this <see cref="CelestialRegion"/>.
        /// </summary>
        /// <param name="type">
        /// The type of child to generate. Does not need to be one of this object's usual child
        /// types, but must be a subclass of <see cref="Orbiter"/>.
        /// </param>
        /// <param name="position">
        /// The location at which to generate the child. If null, a randomly-selected free space will
        /// be selected.
        /// </param>
        /// <param name="constructorParameters">
        /// A list of parameters with which the child's constructor will be called. May be null.
        /// </param>
        public override Orbiter GenerateChildOfType(Type type, Vector3? position, object[] constructorParameters)
        {
            var child = base.GenerateChildOfType(type, position, constructorParameters);

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

            Shape shape = null;
            if (Parent == null || !(Parent is StarSystem) || Position != Vector3.Zero)
            {
                var axis = majorRadius ?? Randomizer.Static.NextDouble(1.5e11, 3.15e12);
                shape = new Ellipsoid(axis, Randomizer.Static.NextDouble(0.5, 1.5) * axis, Randomizer.Static.NextDouble(0.5, 1.5) * axis);
            }
            else
            {
                shape = new Torus(majorRadius ?? 0, minorRadius ?? 0);
            }

            Substance.Mass = shape.Volume * 7.0e-8;

            SetShape(shape);
        }
    }
}
