using System;
using System.Numerics;
using WorldFoundry.Space;

namespace WorldFoundry.CelestialBodies.Planetoids.Planets.TerrestrialPlanets
{
    /// <summary>
    /// A primarily rocky planet, relatively small in comparison to gas and ice giants.
    /// </summary>
    public class TerrestrialPlanet : Planemo
    {
        internal const float maxDensity = 6000;

        /// <summary>
        /// The minimum mass allowed for this type of <see cref="Planetoid"/> during random
        /// generation, in kg. Null indicates a minimum of 0.
        /// </summary>
        /// <remarks>
        /// An arbitrary limit separating rogue dwarf planets from rogue planets. Within orbital
        /// systems, a calculated value for clearing the neighborhood is used instead.
        /// </remarks>
        internal new static double? MinMass_Type => 2.0e22;

        /// <summary>
        /// The chance that this <see cref="Planemo"/> will have rings, as a rate between 0.0 and 1.0.
        /// </summary>
        /// <remarks>
        /// There is a low chance of most planets having substantial rings; 10 for <see
        /// cref="TerrestrialPlanet"/>s.
        /// </remarks>
        protected new static float RingChance => 10;

        /// <summary>
        /// Initializes a new instance of <see cref="TerrestrialPlanet"/>.
        /// </summary>
        public TerrestrialPlanet() { }

        /// <summary>
        /// Initializes a new instance of <see cref="TerrestrialPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="TerrestrialPlanet"/> is located.
        /// </param>
        public TerrestrialPlanet(CelestialObject parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="TerrestrialPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="TerrestrialPlanet"/> is located.
        /// </param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="TerrestrialPlanet"/> during random generation, in kg.
        /// </param>
        public TerrestrialPlanet(CelestialObject parent, double maxMass) : base(parent, maxMass) { }

        /// <summary>
        /// Initializes a new instance of <see cref="TerrestrialPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="TerrestrialPlanet"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="TerrestrialPlanet"/>.</param>
        public TerrestrialPlanet(CelestialObject parent, Vector3 position) : base(parent, position) { }

        /// <summary>
        /// Initializes a new instance of <see cref="TerrestrialPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="TerrestrialPlanet"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="TerrestrialPlanet"/>.</param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="TerrestrialPlanet"/> during random generation, in kg.
        /// </param>
        public TerrestrialPlanet(CelestialObject parent, Vector3 position, double maxMass) : base(parent, position, maxMass) { }

        /// <summary>
        /// Calculates the mass required to produce the given surface gravity.
        /// </summary>
        /// <param name="gravity">The desired surface gravity, in kg/m².</param>
        /// <returns>The mass required to produce the given surface gravity, in kg.</returns>
        private double GetMassForSurfaceGravity(float gravity) => (gravity * Math.Pow(Radius, 2)) / Utilities.Science.Constants.G;

        /// <summary>
        /// Calculates the radius required to produce the given surface gravity.
        /// </summary>
        /// <param name="gravity">The desired surface gravity, in kg/m².</param>
        /// <returns>The radius required to produce the given surface gravity, in meters.</returns>
        public float GetRadiusForSurfaceGravity(float gravity) => (float)((gravity * Utilities.MathUtil.Constants.FourThirdsPI) / (Utilities.Science.Constants.G * Density));
    }
}
