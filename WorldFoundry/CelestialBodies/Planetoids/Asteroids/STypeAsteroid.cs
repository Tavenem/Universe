using System;
using System.Numerics;
using WorldFoundry.Space;
using WorldFoundry.Substances;
using WorldFoundry.Utilities;

namespace WorldFoundry.CelestialBodies.Planetoids.Asteroids
{
    /// <summary>
    /// A silicate asteroid (rocky with significant metal content).
    /// </summary>
    public class STypeAsteroid : Asteroid
    {
        /// <summary>
        /// The base name for this type of <see cref="CelestialEntity"/>.
        /// </summary>
        public new static string BaseTypeName => "S-Type Asteroid";

        /// <summary>
        /// An optional string which is placed before a <see cref="CelestialEntity"/>'s <see cref="CelestialEntity.Designation"/>.
        /// </summary>
        protected override string DesignatorPrefix => "s";

        /// <summary>
        /// Indicates the average density of this type of <see cref="Planetoid"/>, in kg/m³.
        /// </summary>
        protected override double TypeDensity => 2710;

        /// <summary>
        /// Initializes a new instance of <see cref="STypeAsteroid"/>.
        /// </summary>
        public STypeAsteroid() { }

        /// <summary>
        /// Initializes a new instance of <see cref="STypeAsteroid"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="STypeAsteroid"/> is located.
        /// </param>
        public STypeAsteroid(CelestialObject parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="STypeAsteroid"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="STypeAsteroid"/> is located.
        /// </param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="STypeAsteroid"/> during random generation, in kg.
        /// </param>
        public STypeAsteroid(CelestialObject parent, double maxMass) : base(parent, maxMass) { }

        /// <summary>
        /// Initializes a new instance of <see cref="STypeAsteroid"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="STypeAsteroid"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="STypeAsteroid"/>.</param>
        public STypeAsteroid(CelestialObject parent, Vector3 position) : base(parent, position) { }

        /// <summary>
        /// Initializes a new instance of <see cref="STypeAsteroid"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="STypeAsteroid"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="STypeAsteroid"/>.</param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="STypeAsteroid"/> during random generation, in kg.
        /// </param>
        public STypeAsteroid(CelestialObject parent, Vector3 position, double maxMass) : base(parent, position, maxMass) { }

        /// <summary>
        /// Determines an albedo for this <see cref="CelestialBody"/> (a value between 0 and 1).
        /// </summary>
        protected override void GenerateAlbedo() => Albedo = (float)Math.Round(Randomizer.Static.NextDouble(0.1, 0.22), 2);

        /// <summary>
        /// Determines the composition of this <see cref="Planetoid"/>.
        /// </summary>
        protected override void GenerateComposition()
        {
            var iron = 0.568f;

            var nickel = (float)Math.Round(Randomizer.Static.NextDouble(0.03, 0.15), 3);
            iron -= nickel;

            var gold = (float)Math.Round(Randomizer.Static.NextDouble(0.005), 3);

            var platinum = 0.005f - gold;

            Composition = new Mixture(new MixtureComponent[]
            {
                new MixtureComponent
                {
                    Substance = new Substance(Chemical.Rock, Phase.Solid),
                    Proportion = 0.427f,
                },
                new MixtureComponent
                {
                    Substance = new Substance(Chemical.Iron, Phase.Solid),
                    Proportion = iron,
                },
                new MixtureComponent
                {
                    Substance = new Substance(Chemical.Nickel, Phase.Solid),
                    Proportion = nickel,
                },
                new MixtureComponent
                {
                    Substance = new Substance(Chemical.Gold, Phase.Solid),
                    Proportion = gold,
                },
                new MixtureComponent
                {
                    Substance = new Substance(Chemical.Platinum, Phase.Solid),
                    Proportion = platinum,
                },
            });
        }

        /// <summary>
        /// Generates a new satellite for this <see cref="Planetoid"/> with the specified parameters.
        /// </summary>
        /// <returns>A satellite <see cref="Planetoid"/> with an appropriate orbit.</returns>
        protected override Planetoid GenerateSatellite(double periapsis, float eccentricity, double maxMass)
        {
            var satellite = new STypeAsteroid(Parent, maxMass);
            SetAsteroidSatelliteOrbit(satellite, periapsis, eccentricity);
            return satellite;
        }
    }
}
