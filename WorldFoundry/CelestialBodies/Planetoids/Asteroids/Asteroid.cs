using WorldFoundry.CelestialBodies.Planetoids.Planets;
using WorldFoundry.Place;
using WorldFoundry.Space;
using NeverFoundry.MathAndScience.Constants.Numbers;
using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.MathAndScience.Randomization;
using System;

namespace WorldFoundry.CelestialBodies.Planetoids.Asteroids
{
    /// <summary>
    /// Base class for all asteroids.
    /// </summary>
    [Serializable]
    public class Asteroid : Planetoid
    {
        internal static readonly Number Space = new Number(400000);

        /// <summary>
        /// The name for this type of <see cref="CelestialLocation"/>.
        /// </summary>
        public override string TypeName
            => Orbit?.OrbitedObject is Planemo ? $"{BaseTypeName} Moon" : BaseTypeName;

        // Above this an object achieves hydrostatic equilibrium, and is considered a dwarf planet
        // rather than an asteroid.
        private protected override Number? MaxMassForType => new Number(3.4, 20);

        // Below this a body is considered a meteoroid, rather than an asteroid.
        private protected override Number? MinMassForType => new Number(5.9, 8);

        /// <summary>
        /// Initializes a new instance of <see cref="Asteroid"/>.
        /// </summary>
        internal Asteroid() { }

        /// <summary>
        /// Initializes a new instance of <see cref="Asteroid"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="Location"/> in which this <see cref="Asteroid"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="Asteroid"/>.</param>
        internal Asteroid(Location? parent, Vector3 position) : base(parent, position) { }

        /// <summary>
        /// Initializes a new instance of <see cref="Asteroid"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="Location"/> in which this <see cref="Asteroid"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="Asteroid"/>.</param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="Asteroid"/> during random generation, in kg.
        /// </param>
        internal Asteroid(Location? parent, Vector3 position, Number maxMass) : base(parent, position, maxMass) { }

        internal override void GenerateOrbit(CelestialLocation orbitedObject)
        {
            if (orbitedObject == null)
            {
                return;
            }

            WorldFoundry.Space.Orbit.SetOrbit(
                this,
                orbitedObject,
                GetDistanceTo(orbitedObject),
                Randomizer.Instance.NextDouble(0.4),
                Randomizer.Instance.NextDouble(0.5),
                Randomizer.Instance.NextDouble(NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.TwoPI),
                Randomizer.Instance.NextDouble(NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.TwoPI),
                0);
        }

        private protected override (double density, Number mass, IShape shape) GetMatter()
        {
            var density = GetDensity();
            var mass = GetMass();
            return (density, mass, GetShape(density, mass));
        }

        private protected override Number GetMinSatellitePeriapsis() => Shape.ContainingRadius + 20;

        private protected IShape GetShape(Number density, Number mass)
        {
            var axis = (mass * new Number(75, -2) / (density * MathConstants.PI)).CubeRoot();
            var irregularity = Randomizer.Instance.NextNumber(Number.Half, Number.One);
            return new Ellipsoid(axis, axis * irregularity, axis / irregularity, Position);
        }

        private protected void SetAsteroidSatelliteOrbit(CelestialLocation satellite, Number periapsis, double eccentricity)
            => WorldFoundry.Space.Orbit.SetOrbit(
                satellite,
                this,
                periapsis,
                eccentricity,
                Randomizer.Instance.NextDouble(0.5),
                Randomizer.Instance.NextDouble(NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.TwoPI),
                Randomizer.Instance.NextDouble(NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.TwoPI),
                Randomizer.Instance.NextDouble(NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.TwoPI));
    }
}
