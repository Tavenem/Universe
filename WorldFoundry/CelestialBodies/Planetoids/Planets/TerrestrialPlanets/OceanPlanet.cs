using System.Numerics;
using WorldFoundry.Space;

namespace WorldFoundry.CelestialBodies.Planetoids.Planets.TerrestrialPlanets
{
    /// <summary>
    /// A terrestrial planet consisting of an unusually high proportion of water, with a mantle
    /// consisting of a form of high-pressure, hot ice, and possibly a supercritical
    /// surface-atmosphere blend.
    /// </summary>
    public class OceanPlanet : TerrestrialPlanet
    {
        /// <summary>
        /// Initializes a new instance of <see cref="OceanPlanet"/>.
        /// </summary>
        public OceanPlanet() { }

        /// <summary>
        /// Initializes a new instance of <see cref="OceanPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="OceanPlanet"/> is located.
        /// </param>
        public OceanPlanet(CelestialObject parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="OceanPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="OceanPlanet"/> is located.
        /// </param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="OceanPlanet"/> during random generation, in kg.
        /// </param>
        public OceanPlanet(CelestialObject parent, double maxMass) : base(parent, maxMass) { }

        /// <summary>
        /// Initializes a new instance of <see cref="OceanPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="OceanPlanet"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="OceanPlanet"/>.</param>
        public OceanPlanet(CelestialObject parent, Vector3 position) : base(parent, position) { }

        /// <summary>
        /// Initializes a new instance of <see cref="OceanPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="OceanPlanet"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="OceanPlanet"/>.</param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="OceanPlanet"/> during random generation, in kg.
        /// </param>
        public OceanPlanet(CelestialObject parent, Vector3 position, double maxMass) : base(parent, position, maxMass) { }
    }
}
