using System;
using System.Numerics;
using WorldFoundry.CelestialBodies.Planetoids.Planets;
using WorldFoundry.Orbits;
using WorldFoundry.Space;
using WorldFoundry.Utilities;

namespace WorldFoundry.CelestialBodies.Planetoids.Asteroids
{
    /// <summary>
    /// Base class for all asteroids.
    /// </summary>
    public class Asteroid : Planetoid
    {
        /// <summary>
        /// The maximum mass allowed for this type of <see cref="Planetoid"/> during random
        /// generation, in kg. Null indicates no maximum.
        /// </summary>
        /// <remarks>
        /// Above this an object achieves hydrostatic equilibrium, and is considered a dwarf planet
        /// rather than an asteroid.
        /// </remarks>
        protected override double? MaxMass_Type => 3.4e20;

        /// <summary>
        /// The minimum mass allowed for this type of <see cref="Planetoid"/> during random
        /// generation, in kg. Null indicates a minimum of 0.
        /// </summary>
        /// <remarks>Below this a body is considered a meteoroid, rather than an asteroid.</remarks>
        protected override double? MinMass_Type => 5.9e8;

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
        /// The containing <see cref="CelestialObject"/> in which this <see cref="Asteroid"/> is located.
        /// </param>
        public Asteroid(CelestialObject parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="Asteroid"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="Asteroid"/> is located.
        /// </param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="Asteroid"/> during random generation, in kg.
        /// </param>
        public Asteroid(CelestialObject parent, double maxMass) : base(parent, maxMass) { }

        /// <summary>
        /// Initializes a new instance of <see cref="Asteroid"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="Asteroid"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="Asteroid"/>.</param>
        public Asteroid(CelestialObject parent, Vector3 position) : base(parent, position) { }

        /// <summary>
        /// Initializes a new instance of <see cref="Asteroid"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="Asteroid"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="Asteroid"/>.</param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="Asteroid"/> during random generation, in kg.
        /// </param>
        public Asteroid(CelestialObject parent, Vector3 position, double maxMass) : base(parent, position, maxMass) { }

        /// <summary>
        /// Generates the <see cref="Mass"/> of this <see cref="Orbiter"/>.
        /// </summary>
        /// <remarks>
        /// One half of a Gaussian distribution, with <see cref="Planetoid.MinMass"/> as the mean,
        /// constrained to <see cref="Planetoid.MaxMass"/> as a hard limit at 3-sigma.
        /// </remarks>
        protected override void GenerateMass()
        {
            do
            {
                Mass = MinMass + Math.Abs(Randomizer.Static.Normal(0, (MaxMass - MinMass) / 3.0));
            } while (Mass > MaxMass); // Loop rather than using Math.Min to avoid over-representing MaxMass.
        }

        /// <summary>
        /// Generates an appropriate minimum distance at which a natural satellite may orbit this <see cref="Planetoid"/>.
        /// </summary>
        protected override void GenerateMinSatellitePeriapsis() => MinSatellitePeriapsis = Radius + 20;

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
                (float)Math.Round(Randomizer.Static.NextDouble(0.4), 2),
                (float)Math.Round(Randomizer.Static.NextDouble(0.5), 4),
                (float)Math.Round(Randomizer.Static.NextDouble(Utilities.MathUtil.Constants.TwoPI), 4),
                (float)Math.Round(Randomizer.Static.NextDouble(Utilities.MathUtil.Constants.TwoPI), 4),
                0);
        }

        /// <summary>
        /// Generates the <see cref="Utilities.MathUtil.Shapes.Shape"/> of this <see cref="CelestialEntity"/>.
        /// </summary>
        protected override void GenerateShape()
        {
            var axisA = (float)Math.Pow((Mass * 0.75) / (Density * Math.PI), 1.0 / 3.0);
            var irregularity = (float)Math.Round(Randomizer.Static.NextDouble(0.5, 1), 2);
            Shape = new Utilities.MathUtil.Shapes.Ellipsoid(axisA, axisA * irregularity, axisA / irregularity);
        }

        /// <summary>
        /// Sets an appropriate orbit for the satellite of an asteroid.
        /// </summary>
        protected void SetAsteroidSatelliteOrbit(Orbiter satellite, double periapsis, float eccentricity)
            => Orbit.SetOrbit(
                satellite,
                this,
                periapsis,
                eccentricity,
                (float)Math.Round(Randomizer.Static.NextDouble(0.5), 4),
                (float)Math.Round(Randomizer.Static.NextDouble(Utilities.MathUtil.Constants.TwoPI), 4),
                (float)Math.Round(Randomizer.Static.NextDouble(Utilities.MathUtil.Constants.TwoPI), 4),
                (float)Math.Round(Randomizer.Static.NextDouble(Utilities.MathUtil.Constants.TwoPI), 4));
    }
}
