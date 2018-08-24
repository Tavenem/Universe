using MathAndScience.MathUtil;
using MathAndScience.MathUtil.Shapes;
using Substances;
using System;
using System.Numerics;
using WorldFoundry.CelestialBodies.Planetoids.Planets;
using WorldFoundry.Orbits;
using WorldFoundry.Space;

namespace WorldFoundry.CelestialBodies.Planetoids.Asteroids
{
    /// <summary>
    /// Base class for all asteroids.
    /// </summary>
    public class Asteroid : Planetoid
    {
        private const double _maxMassForType = 3.4e20;
        /// <summary>
        /// The maximum mass allowed for this type of <see cref="Planetoid"/> during random
        /// generation, in kg. Null indicates no maximum.
        /// </summary>
        /// <remarks>
        /// Above this an object achieves hydrostatic equilibrium, and is considered a dwarf planet
        /// rather than an asteroid.
        /// </remarks>
        internal override double? MaxMassForType => _maxMassForType;

        private const double _minMassForType = 5.9e8;
        /// <summary>
        /// The minimum mass allowed for this type of <see cref="Planetoid"/> during random
        /// generation, in kg. Null indicates a minimum of 0.
        /// </summary>
        /// <remarks>Below this a body is considered a meteoroid, rather than an asteroid.</remarks>
        internal override double? MinMassForType => _minMassForType;

        /// <summary>
        /// The name for this type of <see cref="CelestialEntity"/>.
        /// </summary>
        public override string TypeName
        {
            get
            {
                if (Orbit?.OrbitedObject is Planemo)
                {
                    return $"{BaseTypeName} Moon";
                }
                else
                {
                    return BaseTypeName;
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of <see cref="Asteroid"/>.
        /// </summary>
        public Asteroid() { }

        /// <summary>
        /// Initializes a new instance of <see cref="Asteroid"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="Asteroid"/> is located.
        /// </param>
        public Asteroid(CelestialRegion parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="Asteroid"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="Asteroid"/> is located.
        /// </param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="Asteroid"/> during random generation, in kg.
        /// </param>
        public Asteroid(CelestialRegion parent, double maxMass) : base(parent, maxMass) { }

        /// <summary>
        /// Initializes a new instance of <see cref="Asteroid"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="Asteroid"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="Asteroid"/>.</param>
        public Asteroid(CelestialRegion parent, Vector3 position) : base(parent, position) { }

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
        public Asteroid(CelestialRegion parent, Vector3 position, double maxMass) : base(parent, position, maxMass) { }

        /// <summary>
        /// Generates the mass of this <see cref="Asteroid"/>.
        /// </summary>
        /// <remarks>
        /// One half of a Gaussian distribution, with <see cref="Planetoid.MinMass"/> as the mean,
        /// constrained to <see cref="Planetoid.MaxMass"/> as a hard limit at 3-sigma.
        /// </remarks>
        private protected double GenerateMass()
        {
            var mass = 0.0;
            do
            {
                mass = MinMass + Math.Abs(Randomizer.Static.Normal(0, (MaxMass - MinMass) / 3.0));
            } while (mass > MaxMass); // Loop rather than using Math.Min to avoid over-representing MaxMass.
            return mass;
        }

        /// <summary>
        /// Generates an appropriate minimum distance at which a natural satellite may orbit this <see cref="Planetoid"/>.
        /// </summary>
        private protected override void GenerateMinSatellitePeriapsis() => MinSatellitePeriapsis = Radius + 20;

        /// <summary>
        /// Determines an orbit for this <see cref="Orbiter"/>.
        /// </summary>
        /// <param name="orbitedObject">The <see cref="Orbiter"/> which is to be orbited.</param>
        public override void GenerateOrbit(Orbiter orbitedObject)
        {
            if (orbitedObject == null)
            {
                return;
            }

            Orbit.SetOrbit(
                this,
                orbitedObject,
                GetDistanceToTarget(orbitedObject),
                Math.Round(Randomizer.Static.NextDouble(0.4), 2),
                Math.Round(Randomizer.Static.NextDouble(0.5), 4),
                Math.Round(Randomizer.Static.NextDouble(MathConstants.TwoPI), 4),
                Math.Round(Randomizer.Static.NextDouble(MathConstants.TwoPI), 4),
                0);
        }

        /// <summary>
        /// Generates the shape of this <see cref="Asteroid"/>.
        /// </summary>
        private protected void GenerateShape()
        {
            var axis = Math.Pow(Mass * 0.75 / (Density * Math.PI), 1.0 / 3.0);
            var irregularity = Math.Round(Randomizer.Static.NextDouble(0.5, 1), 2);
            SetShape(new Ellipsoid(axis, irregularity));
        }

        /// <summary>
        /// Sets an appropriate orbit for the satellite of an asteroid.
        /// </summary>
        /// <param name="satellite">The satellite whose orbit is to be set.</param>
        /// <param name="periapsis">The periapsis of the orbit.</param>
        /// <param name="eccentricity">The eccentricity of the orbit.</param>
        private protected void SetAsteroidSatelliteOrbit(Orbiter satellite, double periapsis, double eccentricity)
            => Orbit.SetOrbit(
                satellite,
                this,
                periapsis,
                eccentricity,
                Math.Round(Randomizer.Static.NextDouble(0.5), 4),
                Math.Round(Randomizer.Static.NextDouble(MathConstants.TwoPI), 4),
                Math.Round(Randomizer.Static.NextDouble(MathConstants.TwoPI), 4),
                Math.Round(Randomizer.Static.NextDouble(MathConstants.TwoPI), 4));
    }
}
