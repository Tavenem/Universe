using Substances;
using System;
using System.Collections.Generic;
using System.Numerics;
using WorldFoundry.Space;

namespace WorldFoundry.CelestialBodies.Planetoids.Asteroids
{
    /// <summary>
    /// A silicate asteroid (rocky with significant metal content).
    /// </summary>
    public class STypeAsteroid : Asteroid
    {
        private const string baseTypeName = "S-Type Asteroid";
        /// <summary>
        /// The base name for this type of <see cref="CelestialEntity"/>.
        /// </summary>
        public override string BaseTypeName => baseTypeName;

        private const double densityForType = 2710;
        /// <summary>
        /// Indicates the average density of this type of <see cref="Planetoid"/>, in kg/m³.
        /// </summary>
        internal override double DensityForType => densityForType;

        /// <summary>
        /// An optional string which is placed before a <see cref="CelestialEntity"/>'s <see cref="CelestialEntity.Designation"/>.
        /// </summary>
        protected override string DesignatorPrefix => "s";

        /// <summary>
        /// Initializes a new instance of <see cref="STypeAsteroid"/>.
        /// </summary>
        public STypeAsteroid() : base() { }

        /// <summary>
        /// Initializes a new instance of <see cref="STypeAsteroid"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="STypeAsteroid"/> is located.
        /// </param>
        public STypeAsteroid(CelestialRegion parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="STypeAsteroid"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="STypeAsteroid"/> is located.
        /// </param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="STypeAsteroid"/> during random generation, in kg.
        /// </param>
        public STypeAsteroid(CelestialRegion parent, double maxMass) : base(parent, maxMass) { }

        /// <summary>
        /// Initializes a new instance of <see cref="STypeAsteroid"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="STypeAsteroid"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="STypeAsteroid"/>.</param>
        public STypeAsteroid(CelestialRegion parent, Vector3 position) : base(parent, position) { }

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
        public STypeAsteroid(CelestialRegion parent, Vector3 position, double maxMass) : base(parent, position, maxMass) { }

        /// <summary>
        /// Determines an albedo for this <see cref="CelestialBody"/> (a value between 0 and 1).
        /// </summary>
        private protected override void GenerateAlbedo() => Albedo = Math.Round(Randomizer.Static.NextDouble(0.1, 0.22), 2);

        /// <summary>
        /// Determines the <see cref="CelestialEntity.Substance"/> of this <see cref="CelestialEntity"/>.
        /// </summary>
        private protected override void GenerateSubstance()
        {
            var iron = 0.568;

            var nickel = Math.Round(Randomizer.Static.NextDouble(0.03, 0.15), 3);
            iron -= nickel;

            var gold = Math.Round(Randomizer.Static.NextDouble(0.005), 3);

            Substance = new Substance
            {
                Composition = new Composite(
                    (Chemical.Rock, Phase.Solid, 0.427),
                    (Chemical.Iron, Phase.Solid, iron),
                    (Chemical.Nickel, Phase.Solid, nickel),
                    (Chemical.Gold, Phase.Solid, gold),
                    (Chemical.Platinum, Phase.Solid, 0.005 - gold)),
                Mass = GenerateMass(),
            };
            GenerateShape();
        }

        /// <summary>
        /// Generates a new satellite for this <see cref="Planetoid"/> with the specified parameters.
        /// </summary>
        /// <returns>A satellite <see cref="Planetoid"/> with an appropriate orbit.</returns>
        private protected override Planetoid GenerateSatellite(double periapsis, double eccentricity, double maxMass)
        {
            var satellite = new STypeAsteroid(Parent, maxMass);
            SetAsteroidSatelliteOrbit(satellite, periapsis, eccentricity);
            return satellite;
        }
    }
}
