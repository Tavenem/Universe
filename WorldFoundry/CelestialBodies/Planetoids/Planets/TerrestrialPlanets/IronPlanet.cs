using System.Numerics;
using WorldFoundry.Space;

namespace WorldFoundry.CelestialBodies.Planetoids.Planets.TerrestrialPlanets
{
    /// <summary>
    /// A relatively small terrestrial planet consisting of an unusually large iron-nickel core, and
    /// an unusually thin mantle (similar to Mercury).
    /// </summary>
    public class IronPlanet : TerrestrialPlanet
    {
        /// <summary>
        /// Initializes a new instance of <see cref="IronPlanet"/>.
        /// </summary>
        public IronPlanet() { }

        /// <summary>
        /// Initializes a new instance of <see cref="IronPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="IronPlanet"/> is located.
        /// </param>
        public IronPlanet(CelestialObject parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="IronPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="IronPlanet"/> is located.
        /// </param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="IronPlanet"/> during random generation, in kg.
        /// </param>
        public IronPlanet(CelestialObject parent, double maxMass) : base(parent, maxMass) { }

        /// <summary>
        /// Initializes a new instance of <see cref="IronPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="IronPlanet"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="IronPlanet"/>.</param>
        public IronPlanet(CelestialObject parent, Vector3 position) : base(parent, position) { }

        /// <summary>
        /// Initializes a new instance of <see cref="IronPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="IronPlanet"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="IronPlanet"/>.</param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="IronPlanet"/> during random generation, in kg.
        /// </param>
        public IronPlanet(CelestialObject parent, Vector3 position, double maxMass) : base(parent, position, maxMass) { }
    }
}
