using NeverFoundry.MathAndScience.Chemistry;
using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using WorldFoundry.CelestialBodies.Planetoids;
using WorldFoundry.CelestialBodies.Planetoids.Asteroids;
using WorldFoundry.CelestialBodies.Stars;
using WorldFoundry.Place;

namespace WorldFoundry.Space.AsteroidFields
{
    /// <summary>
    /// A shell surrounding a star with a high concentration of cometary bodies.
    /// </summary>
    [Serializable]
    public class OortCloud : AsteroidField
    {
        private static readonly Number _ChildDensity = new Number(8.31, -38);

        private static readonly List<ChildDefinition> _ChildDefinitions = new List<ChildDefinition>
        {
            new ChildDefinition(typeof(Comet), Comet.Space, _ChildDensity * new Number(85, -2)),
            new ChildDefinition(typeof(CTypeAsteroid), Asteroid.Space, _ChildDensity * new Number(11, -2)),
            new ChildDefinition(typeof(STypeAsteroid), Asteroid.Space, _ChildDensity * new Number(25, -3)),
            new ChildDefinition(typeof(MTypeAsteroid), Asteroid.Space, _ChildDensity * new Number(15, -3)),
        };

        private protected override string BaseTypeName => "Oort Cloud";

        private protected override IEnumerable<ChildDefinition> ChildDefinitions
            => base.ChildDefinitions.Concat(_ChildDefinitions);

        /// <summary>
        /// Initializes a new instance of <see cref="OortCloud"/>.
        /// </summary>
        internal OortCloud() { }

        /// <summary>
        /// Initializes a new instance of <see cref="OortCloud"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="Location"/> in which this <see cref="OortCloud"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="OortCloud"/>.</param>
        internal OortCloud(Location parent, Vector3 position) : base(parent, position) { }

        /// <summary>
        /// Initializes a new instance of <see cref="OortCloud"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="Location"/> in which this <see cref="OortCloud"/> is located.
        /// </param>
        /// <param name="star">The star around which this <see cref="OortCloud"/> is formed.</param>
        /// <param name="starSystemRadius">
        /// The outer radius of the <see cref="StarSystem"/> in which this <see cref="OortCloud"/> is located.
        /// </param>
        public OortCloud(Location parent, Star star, Number starSystemRadius) : base(parent, Vector3.Zero)
        {
            _starId = star.Id;
            _majorRadius = new Number(7.5, 15) + starSystemRadius;
            _minorRadius = new Number(3, 15) + starSystemRadius;
        }

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
        public OortCloud(Location parent, Vector3 position, Star star, Number majorRadius, Number? minorRadius = null) : base(parent, position, star, majorRadius, minorRadius) { }

        private OortCloud(
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
                starId,
                isPrepopulated,
                albedo,
                velocity,
                orbit,
                material,
                majorRadius,
                minorRadius,
                children) { }

        private OortCloud(SerializationInfo info, StreamingContext context) : this(
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

        internal override CelestialLocation? GenerateChild(ChildDefinition definition)
        {
            var child = base.GenerateChild(definition);

            if (Star != null && child != null)
            {
                child.GenerateOrbit(Star);
            }

            return child;
        }

        private protected override Number GetMass() => new Number(3, 25);

        private protected override (double density, Number mass, IShape shape) GetMatter()
        {
            var mass = GetMass();
            var shape = GetShape();
            return ((double)(mass / shape.Volume), mass, shape);
        }

        private protected override IShape GetShape()
        {
            var shape = new HollowSphere(_minorRadius ?? new Number(3, 15), _majorRadius ?? new Number(7.5, 15), Position);
            _majorRadius = null;
            _minorRadius = null;
            return shape;
        }
    }
}
