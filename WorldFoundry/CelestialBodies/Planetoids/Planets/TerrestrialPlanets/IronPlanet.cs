using MathAndScience.Numerics;
using WorldFoundry.Space;

namespace WorldFoundry.CelestialBodies.Planetoids.Planets.TerrestrialPlanets
{
    /// <summary>
    /// A relatively small terrestrial planet consisting of an unusually large iron-nickel core, and
    /// an unusually thin mantle (similar to Mercury).
    /// </summary>
    public class IronPlanet : TerrestrialPlanet
    {
        private protected override bool CanHaveWater => false;

        private protected override double MagnetosphereChanceFactor => 5;

        private protected override double MaxDensity => 8000;

        private protected override double MetalProportion => 0.25;

        private protected override double MinDensity => 5250;

        private protected override string PlanemoClassPrefix => "Iron";

        /// <summary>
        /// Initializes a new instance of <see cref="IronPlanet"/>.
        /// </summary>
        internal IronPlanet() { }

        /// <summary>
        /// Initializes a new instance of <see cref="IronPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="IronPlanet"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="IronPlanet"/>.</param>
        internal IronPlanet(CelestialRegion parent, Vector3 position) : base(parent, position) { }

        /// <summary>
        /// Initializes a new instance of <see cref="IronPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="IronPlanet"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="IronPlanet"/>.</param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="IronPlanet"/> during random generation, in kg.
        /// </param>
        internal IronPlanet(CelestialRegion parent, Vector3 position, double maxMass) : base(parent, position, maxMass) { }

        private protected override double GetCoreProportion(double mass) => 0.4;
    }
}
