using System;
using System.Collections.Generic;
using System.Numerics;
using WorldFoundry.CelestialBodies.Planetoids;
using WorldFoundry.CelestialBodies.Planetoids.Asteroids;
using WorldFoundry.CelestialBodies.Stars;
using WorldFoundry.Utilities.MathUtil.Shapes;

namespace WorldFoundry.Space.AsteroidFields
{
    /// <summary>
    /// A shell surrounding a star with a high concentration of cometary bodies.
    /// </summary>
    public class OortCloud : AsteroidField
    {
        /// <summary>
        /// The base name for this type of <see cref="CelestialEntity"/>.
        /// </summary>
        public new static string BaseTypeName => "Oort Cloud";

        /// <summary>
        /// The types of children this region of space might have.
        /// </summary>
        public new static IDictionary<Type, float> ChildPossibilities => new Dictionary<Type, float>
        {
            { typeof(Comet), 0.85f },
            { typeof(CTypeAsteroid), 0.11f },
            { typeof(STypeAsteroid), 0.025f },
            { typeof(MTypeAsteroid), 0.015f },
        };

        /// <summary>
        /// The average number of children within the grid per m³.
        /// </summary>
        public override double ChildDensity => 8.31e-38;

        /// <summary>
        /// Initializes a new instance of <see cref="OortCloud"/>.
        /// </summary>
        public OortCloud() { }

        /// <summary>
        /// Initializes a new instance of <see cref="OortCloud"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="OortCloud"/> is located.
        /// </param>
        public OortCloud(CelestialObject parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="OortCloud"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="OortCloud"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="OortCloud"/>.</param>
        public OortCloud(CelestialObject parent, Vector3 position) : base(parent, position) { }

        /// <summary>
        /// Initializes a new instance of <see cref="OortCloud"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="OortCloud"/> is located.
        /// </param>
        public OortCloud(CelestialObject parent, Star star, float starSystemRadius) : base(parent)
        {
            Star = star;
            GenerateShape(starSystemRadius);
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

            if (Star != null)
            {
                child.GenerateOrbit(Star);
            }

            return child;
        }

        /// <summary>
        /// Generates the <see cref="Mass"/> of this <see cref="Orbiter"/>.
        /// </summary>
        protected override void GenerateMass() => Mass = 3.0e25;

        private void GenerateShape(float? starSystemRadius) => Shape = new HollowSphere(3.0e15f + (starSystemRadius ?? 0), 7.5e15f + (starSystemRadius ?? 0));

        /// <summary>
        /// Generates the <see cref="Utilities.MathUtil.Shapes.Shape"/> of this <see cref="CelestialEntity"/>.
        /// </summary>
        protected override void GenerateShape() => GenerateShape(null);
    }
}
