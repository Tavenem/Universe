using Substances;
using System;
using MathAndScience.Numerics;
using WorldFoundry.Space;
using MathAndScience.Shapes;

namespace WorldFoundry.CelestialBodies.Planetoids.Asteroids
{
    /// <summary>
    /// A silicate asteroid (rocky with significant metal content).
    /// </summary>
    public class STypeAsteroid : Asteroid
    {
        private protected override string BaseTypeName => "S-Type Asteroid";

        private protected override double DensityForType => 2710;

        private protected override string DesignatorPrefix => "s";

        /// <summary>
        /// Initializes a new instance of <see cref="STypeAsteroid"/>.
        /// </summary>
        internal STypeAsteroid() { }

        /// <summary>
        /// Initializes a new instance of <see cref="STypeAsteroid"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="STypeAsteroid"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="STypeAsteroid"/>.</param>
        internal STypeAsteroid(CelestialRegion parent, Vector3 position) : base(parent, position) { }

        /// <summary>
        /// Initializes a new instance of <see cref="STypeAsteroid"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="STypeAsteroid"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="STypeAsteroid"/>.</param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="STypeAsteroid"/> during random generation, in kg.
        /// </param>
        internal STypeAsteroid(CelestialRegion parent, Vector3 position, double maxMass) : base(parent, position, maxMass) { }

        private protected override void GenerateAlbedo() => Albedo = Randomizer.Instance.NextDouble(0.1, 0.22);

        private protected override Planetoid GenerateSatellite(double periapsis, double eccentricity, double maxMass)
        {
            var satellite = new STypeAsteroid(Parent, Vector3.Zero, maxMass);
            SetAsteroidSatelliteOrbit(satellite, periapsis, eccentricity);
            return satellite;
        }

        private protected override IComposition GetComposition(double mass, IShape shape)
        {
            var iron = 0.568;

            var nickel = Math.Round(Randomizer.Instance.NextDouble(0.03, 0.15), 3);
            iron -= nickel;

            var gold = Math.Round(Randomizer.Instance.NextDouble(0.005), 3);

            return new Composite(
                (Chemical.Rock, Phase.Solid, 0.427),
                (Chemical.Iron, Phase.Solid, iron),
                (Chemical.Nickel, Phase.Solid, nickel),
                (Chemical.Gold, Phase.Solid, gold),
                (Chemical.Platinum, Phase.Solid, 0.005 - gold));
        }
    }
}
