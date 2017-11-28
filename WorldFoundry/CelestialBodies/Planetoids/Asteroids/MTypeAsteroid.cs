using System;
using System.Numerics;
using WorldFoundry.Space;
using WorldFoundry.Substances;
using WorldFoundry.Utilities;

namespace WorldFoundry.CelestialBodies.Planetoids.Asteroids
{
    /// <summary>
    /// A metallic asteroid (mostly iron-nickel, with some rock and other heavy metals).
    /// </summary>
    public class MTypeAsteroid : Asteroid
    {
        internal new const string baseTypeName = "M-Type Asteroid";
        /// <summary>
        /// The base name for this type of <see cref="CelestialEntity"/>.
        /// </summary>
        public override string BaseTypeName => baseTypeName;

        /// <summary>
        /// An optional string which is placed before a <see cref="CelestialEntity"/>'s <see cref="CelestialEntity.Designation"/>.
        /// </summary>
        protected override string DesignatorPrefix => "m";

        private const double typeDensity = 5320;
        /// <summary>
        /// Indicates the average density of this type of <see cref="Planetoid"/>, in kg/m³.
        /// </summary>
        internal override double TypeDensity => typeDensity;

        /// <summary>
        /// Initializes a new instance of <see cref="MTypeAsteroid"/>.
        /// </summary>
        public MTypeAsteroid() { }

        /// <summary>
        /// Initializes a new instance of <see cref="MTypeAsteroid"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="MTypeAsteroid"/> is located.
        /// </param>
        public MTypeAsteroid(CelestialObject parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="MTypeAsteroid"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="MTypeAsteroid"/> is located.
        /// </param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="MTypeAsteroid"/> during random generation, in kg.
        /// </param>
        public MTypeAsteroid(CelestialObject parent, double maxMass) : base(parent, maxMass) { }

        /// <summary>
        /// Initializes a new instance of <see cref="MTypeAsteroid"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="MTypeAsteroid"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="MTypeAsteroid"/>.</param>
        public MTypeAsteroid(CelestialObject parent, Vector3 position) : base(parent, position) { }

        /// <summary>
        /// Initializes a new instance of <see cref="MTypeAsteroid"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="MTypeAsteroid"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="MTypeAsteroid"/>.</param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="MTypeAsteroid"/> during random generation, in kg.
        /// </param>
        public MTypeAsteroid(CelestialObject parent, Vector3 position, double maxMass) : base(parent, position, maxMass) { }

        /// <summary>
        /// Determines an albedo for this <see cref="CelestialBody"/> (a value between 0 and 1).
        /// </summary>
        protected override void GenerateAlbedo() => Albedo = (float)Math.Round(Randomizer.Static.NextDouble(0.1, 0.2), 2);

        /// <summary>
        /// Determines the composition of this <see cref="Planetoid"/>.
        /// </summary>
        protected override void GenerateComposition()
        {
            var iron = 0.95f;

            var nickel = (float)Math.Round(Randomizer.Static.NextDouble(0.05, 0.25), 3);
            iron -= nickel;

            var rock = (float)Math.Round(Randomizer.Static.NextDouble(0.2), 3);
            iron -= rock;

            var gold = (float)Math.Round(Randomizer.Static.NextDouble(0.05), 3);

            var platinum = 0.05f - gold;

            Composition = new Mixture(new MixtureComponent[]
            {
                new MixtureComponent
                {
                    Chemical = Chemical.Rock,
                    Phase = Phase.Solid,
                    Proportion = rock,
                },
                new MixtureComponent
                {
                    Chemical = Chemical.Iron,
                    Phase = Phase.Solid,
                    Proportion = iron,
                },
                new MixtureComponent
                {
                    Chemical = Chemical.Nickel,
                    Phase = Phase.Solid,
                    Proportion = nickel,
                },
                new MixtureComponent
                {
                    Chemical = Chemical.Gold,
                    Phase = Phase.Solid,
                    Proportion = gold,
                },
                new MixtureComponent
                {
                    Chemical = Chemical.Platinum,
                    Phase = Phase.Solid,
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
            var satellite = new MTypeAsteroid(Parent, maxMass);
            SetAsteroidSatelliteOrbit(satellite, periapsis, eccentricity);
            return satellite;
        }
    }
}
