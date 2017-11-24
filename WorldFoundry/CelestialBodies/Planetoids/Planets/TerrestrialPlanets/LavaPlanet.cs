using System.Numerics;
using WorldFoundry.Space;

namespace WorldFoundry.CelestialBodies.Planetoids.Planets.TerrestrialPlanets
{
    /// <summary>
    /// A terrestrial planet with little to no crust, whether due to a catastrophic collision event,
    /// or severe tidal forces due to a close orbit.
    /// </summary>
    public class LavaPlanet : TerrestrialPlanet
    {
        /// <summary>
        /// Initializes a new instance of <see cref="LavaPlanet"/>.
        /// </summary>
        public LavaPlanet() { }

        /// <summary>
        /// Initializes a new instance of <see cref="LavaPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="LavaPlanet"/> is located.
        /// </param>
        public LavaPlanet(CelestialObject parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="LavaPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="LavaPlanet"/> is located.
        /// </param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="LavaPlanet"/> during random generation, in kg.
        /// </param>
        public LavaPlanet(CelestialObject parent, double maxMass) : base(parent, maxMass) { }

        /// <summary>
        /// Initializes a new instance of <see cref="LavaPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="LavaPlanet"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="LavaPlanet"/>.</param>
        public LavaPlanet(CelestialObject parent, Vector3 position) : base(parent, position) { }

        /// <summary>
        /// Initializes a new instance of <see cref="LavaPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="LavaPlanet"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="LavaPlanet"/>.</param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="LavaPlanet"/> during random generation, in kg.
        /// </param>
        public LavaPlanet(CelestialObject parent, Vector3 position, double maxMass) : base(parent, position, maxMass) { }
    }
}
