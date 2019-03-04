using MathAndScience;
using MathAndScience.Shapes;
using System;
using MathAndScience.Numerics;
using WorldFoundry.CelestialBodies.Planetoids.Planets;
using WorldFoundry.Space;

namespace WorldFoundry.CelestialBodies.Planetoids.Asteroids
{
    /// <summary>
    /// Base class for all asteroids.
    /// </summary>
    public class Asteroid : Planetoid
    {
        internal const double Space = 400000;

        /// <summary>
        /// The name for this type of <see cref="ICelestialLocation"/>.
        /// </summary>
        public override string TypeName
            => Orbit?.OrbitedObject is Planemo ? $"{BaseTypeName} Moon" : BaseTypeName;

        // Above this an object achieves hydrostatic equilibrium, and is considered a dwarf planet
        // rather than an asteroid.
        private protected override double? MaxMassForType => 3.4e20;

        // Below this a body is considered a meteoroid, rather than an asteroid.
        private protected override double? MinMassForType => 5.9e8;

        /// <summary>
        /// Initializes a new instance of <see cref="Asteroid"/>.
        /// </summary>
        internal Asteroid() { }

        /// <summary>
        /// Initializes a new instance of <see cref="Asteroid"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="Asteroid"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="Asteroid"/>.</param>
        internal Asteroid(CelestialRegion? parent, Vector3 position) : base(parent, position) { }

        /// <summary>
        /// Initializes a new instance of <see cref="Asteroid"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="Asteroid"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="Asteroid"/>.</param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="Asteroid"/> during random generation, in kg.
        /// </param>
        internal Asteroid(CelestialRegion? parent, Vector3 position, double maxMass) : base(parent, position, maxMass) { }

        internal override void GenerateOrbit(ICelestialLocation orbitedObject)
        {
            if (orbitedObject == null)
            {
                return;
            }

            WorldFoundry.Space.Orbit.SetOrbit(
                this,
                orbitedObject,
                GetDistanceTo(orbitedObject),
                Math.Round(Randomizer.Instance.NextDouble(0.4), 2),
                Math.Round(Randomizer.Instance.NextDouble(0.5), 4),
                Math.Round(Randomizer.Instance.NextDouble(MathConstants.TwoPI), 4),
                Math.Round(Randomizer.Instance.NextDouble(MathConstants.TwoPI), 4),
                0);
        }

        private protected override (double, IShape) GetMassAndShape()
        {
            var mass = GetMass();
            return (mass, GetShape(mass));
        }

        private protected override double GetMinSatellitePeriapsis() => Shape.ContainingRadius + 20;

        private protected override IShape GetShape(double? mass = null, double? knownRadius = null)
        {
            if (mass == null && knownRadius == null)
            {
                return new SinglePoint(Position);
            }

            var axis = mass == null
                ? knownRadius!.Value
                : Math.Pow(mass.Value * 0.75 / (Density * Math.PI), 1.0 / 3.0);
            var irregularity = Math.Round(Randomizer.Instance.NextDouble(0.5, 1), 2);
            return new Ellipsoid(axis, axis * irregularity, axis / irregularity, Position);
        }

        private protected void SetAsteroidSatelliteOrbit(ICelestialLocation satellite, double periapsis, double eccentricity)
            => WorldFoundry.Space.Orbit.SetOrbit(
                satellite,
                this,
                periapsis,
                eccentricity,
                Math.Round(Randomizer.Instance.NextDouble(0.5), 4),
                Math.Round(Randomizer.Instance.NextDouble(MathConstants.TwoPI), 4),
                Math.Round(Randomizer.Instance.NextDouble(MathConstants.TwoPI), 4),
                Math.Round(Randomizer.Instance.NextDouble(MathConstants.TwoPI), 4));
    }
}
