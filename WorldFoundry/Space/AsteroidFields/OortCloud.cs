using MathAndScience.MathUtil.Shapes;
using Substances;
using System;
using System.Collections.Generic;
using System.Numerics;
using WorldFoundry.CelestialBodies.Planetoids;
using WorldFoundry.CelestialBodies.Planetoids.Asteroids;
using WorldFoundry.CelestialBodies.Stars;
using WorldFoundry.Orbits;
using WorldFoundry.Substances;

namespace WorldFoundry.Space.AsteroidFields
{
    /// <summary>
    /// A shell surrounding a star with a high concentration of cometary bodies.
    /// </summary>
    public class OortCloud : AsteroidField
    {
        private readonly double? starSystemRadius;

        private const string baseTypeName = "Oort Cloud";
        /// <summary>
        /// The base name for this type of <see cref="CelestialEntity"/>.
        /// </summary>
        public override string BaseTypeName => baseTypeName;

        internal new static IList<(Type type, float proportion, object[] constructorParameters)> childPossibilities =
            new List<(Type type, float proportion, object[] constructorParameters)>
            {
                (typeof(Comet), 0.85f, null),
                (typeof(CTypeAsteroid), 0.11f, null),
                (typeof(STypeAsteroid), 0.025f, null),
                (typeof(MTypeAsteroid), 0.015f, null),
            };
        /// <summary>
        /// The types of children this region of space might have.
        /// </summary>
        public override IList<(Type type, float proportion, object[] constructorParameters)> ChildPossibilities => childPossibilities;

        private const double childDensity = 8.31e-38;
        /// <summary>
        /// The average number of children within the grid per m³.
        /// </summary>
        public override double ChildDensity => childDensity;

        /// <summary>
        /// Initializes a new instance of <see cref="OortCloud"/>.
        /// </summary>
        public OortCloud() : base() { }

        /// <summary>
        /// Initializes a new instance of <see cref="OortCloud"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="OortCloud"/> is located.
        /// </param>
        public OortCloud(CelestialRegion parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="OortCloud"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="OortCloud"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="OortCloud"/>.</param>
        public OortCloud(CelestialRegion parent, Vector3 position) : base(parent, position) { }

        /// <summary>
        /// Initializes a new instance of <see cref="OortCloud"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="OortCloud"/> is located.
        /// </param>
        /// <param name="star">The star around which this <see cref="OortCloud"/> is formed.</param>
        /// <param name="starSystemRadius">
        /// The outer radius of the <see cref="StarSystem"/> in which this <see cref="OortCloud"/> is located.
        /// </param>
        public OortCloud(CelestialRegion parent, Star star, double starSystemRadius) : base(parent)
        {
            Star = star;
            this.starSystemRadius = starSystemRadius;
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
        /// An optional list of parameters with which to call the child's constructor. May be null.
        /// </param>
        public override Orbiter GenerateChildOfType(Type type, Vector3? position, object[] constructorParameters)
        {
            var child = base.GenerateChildOfType(type, position, constructorParameters);

            if (Star != null)
            {
                child.GenerateOrbit(Star);
            }

            return child;
        }

        /// <summary>
        /// Generates the <see cref="CelestialEntity.Substance"/> of this <see cref="CelestialEntity"/>.
        /// </summary>
        private protected override void GenerateSubstance()
        {
            Substance = new Substance
            {
                Composition = CosmicSubstances.InterplanetaryMedium.GetDeepCopy(),
                Mass = 3.0e25,
            };
            SetShape(new HollowSphere(3.0e15 + (starSystemRadius ?? 0), 7.5e15 + (starSystemRadius ?? 0)));
        }
    }
}
