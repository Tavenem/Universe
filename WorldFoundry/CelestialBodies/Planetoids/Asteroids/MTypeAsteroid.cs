using Substances;
using System;
using MathAndScience.Numerics;
using WorldFoundry.Space;
using MathAndScience.Shapes;

namespace WorldFoundry.CelestialBodies.Planetoids.Asteroids
{
    /// <summary>
    /// A metallic asteroid (mostly iron-nickel, with some rock and other heavy metals).
    /// </summary>
    public class MTypeAsteroid : Asteroid
    {
        private protected override string BaseTypeName => "M-Type Asteroid";

        private protected override double DensityForType => 5320;

        private protected override string DesignatorPrefix => "m";

        /// <summary>
        /// Initializes a new instance of <see cref="MTypeAsteroid"/>.
        /// </summary>
        internal MTypeAsteroid() { }

        /// <summary>
        /// Initializes a new instance of <see cref="MTypeAsteroid"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="MTypeAsteroid"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="MTypeAsteroid"/>.</param>
        internal MTypeAsteroid(CelestialRegion parent, Vector3 position) : base(parent, position) { }

        /// <summary>
        /// Initializes a new instance of <see cref="MTypeAsteroid"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="MTypeAsteroid"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="MTypeAsteroid"/>.</param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="MTypeAsteroid"/> during random generation, in kg.
        /// </param>
        internal MTypeAsteroid(CelestialRegion parent, Vector3 position, double maxMass) : base(parent, position, maxMass) { }

        private protected override void GenerateAlbedo() => Albedo = Randomizer.Instance.NextDouble(0.1, 0.2);

        private protected override Planetoid GenerateSatellite(double periapsis, double eccentricity, double maxMass)
        {
            var satellite = new MTypeAsteroid(ContainingCelestialRegion, Vector3.Zero, maxMass);
            SetAsteroidSatelliteOrbit(satellite, periapsis, eccentricity);
            return satellite;
        }

        private protected override IComposition GetComposition(double mass, IShape shape)
        {
            var iron = 0.95;

            var nickel = Math.Round(Randomizer.Instance.NextDouble(0.05, 0.25), 3);
            iron -= nickel;

            var rock = Math.Round(Randomizer.Instance.NextDouble(0.2), 3);
            iron -= rock;

            var gold = Math.Round(Randomizer.Instance.NextDouble(0.05), 3);

            var platinum = 0.05 - gold;

            return new Composite(
                (Chemical.Rock, Phase.Solid, rock),
                (Chemical.Iron, Phase.Solid, iron),
                (Chemical.Nickel, Phase.Solid, nickel),
                (Chemical.Gold, Phase.Solid, gold),
                (Chemical.Platinum, Phase.Solid, platinum));
        }
    }
}
