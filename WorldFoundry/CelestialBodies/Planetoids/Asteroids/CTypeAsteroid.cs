using Substances;
using System;
using MathAndScience.Numerics;
using WorldFoundry.Space;
using MathAndScience.Shapes;

namespace WorldFoundry.CelestialBodies.Planetoids.Asteroids
{
    /// <summary>
    /// A carbonaceous asteroid (mostly rock).
    /// </summary>
    public class CTypeAsteroid : Asteroid
    {
        private protected override string BaseTypeName => "C-Type Asteroid";

        private protected override double DensityForType => 1380;

        private protected override string DesignatorPrefix => "c";

        /// <summary>
        /// Initializes a new instance of <see cref="CTypeAsteroid"/>.
        /// </summary>
        internal CTypeAsteroid() { }

        /// <summary>
        /// Initializes a new instance of <see cref="CTypeAsteroid"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="CTypeAsteroid"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="CTypeAsteroid"/>.</param>
        internal CTypeAsteroid(CelestialRegion? parent, Vector3 position) : base(parent, position) { }

        /// <summary>
        /// Initializes a new instance of <see cref="CTypeAsteroid"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="CTypeAsteroid"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="CTypeAsteroid"/>.</param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="CTypeAsteroid"/> during random generation, in kg.
        /// </param>
        internal CTypeAsteroid(CelestialRegion? parent, Vector3 position, double maxMass) : base(parent, position, maxMass) { }

        private protected override void GenerateAlbedo() => Albedo = Randomizer.Instance.NextDouble(0.03, 0.1);

        private protected override Planetoid? GenerateSatellite(double periapsis, double eccentricity, double maxMass)
        {
            var satellite = new CTypeAsteroid(ContainingCelestialRegion, Vector3.Zero, maxMass);
            SetAsteroidSatelliteOrbit(satellite, periapsis, eccentricity);
            return satellite;
        }

        private protected override IComposition GetComposition(double mass, IShape shape)
        {
            var rock = 1.0;

            var clay = Math.Round(Randomizer.Instance.NextDouble(0.1, 0.2), 3);
            rock -= clay;

            var ice = Math.Round(Randomizer.Instance.NextDouble(0.22), 3);
            rock -= ice;

            return new Composite(
                (Chemical.Rock, Phase.Solid, rock),
                (Chemical.Clay, Phase.Solid, clay),
                (Chemical.Water, Phase.Solid, ice));
        }
    }
}
