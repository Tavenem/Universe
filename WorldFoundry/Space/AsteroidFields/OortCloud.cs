using MathAndScience.Shapes;
using Substances;
using System.Collections.Generic;
using System.Linq;
using MathAndScience.Numerics;
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
        private const double ChildDensity = 8.31e-38;

        private static readonly List<ChildDefinition> _childDefinitions = new List<ChildDefinition>
        {
            new ChildDefinition(typeof(Comet), Comet.Space, ChildDensity * 0.85),
            new ChildDefinition(typeof(CTypeAsteroid), Asteroid.Space, ChildDensity * 0.11),
            new ChildDefinition(typeof(STypeAsteroid), Asteroid.Space, ChildDensity * 0.025),
            new ChildDefinition(typeof(MTypeAsteroid), Asteroid.Space, ChildDensity * 0.015),
        };

        private readonly double? _starSystemRadius;

        private const string _baseTypeName = "Oort Cloud";
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
        /// Initializes a new instance of <see cref="OortCloud"/>.
        /// </summary>
        public OortCloud() { }

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
            _starSystemRadius = starSystemRadius;
            GenerateSubstance();
        }

        internal override Orbiter GenerateChild(ChildDefinition definition)
        {
            var child = base.GenerateChild(definition);

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
            Shape = new HollowSphere(3.0e15 + (_starSystemRadius ?? 0), 7.5e15 + (_starSystemRadius ?? 0));
        }
    }
}
