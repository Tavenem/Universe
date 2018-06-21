using Substances;
using System;
using System.Collections.Generic;
using System.Numerics;
using WorldFoundry.Space;

namespace WorldFoundry.CelestialBodies.Planetoids.Asteroids
{
    /// <summary>
    /// A carbonaceous asteroid (mostly rock).
    /// </summary>
    public class CTypeAsteroid : Asteroid
    {
        internal new static string baseTypeName = "C-Type Asteroid";
        /// <summary>
        /// The base name for this type of <see cref="CelestialEntity"/>.
        /// </summary>
        public override string BaseTypeName => baseTypeName;

        private static readonly double densityForType = 1380;
        /// <summary>
        /// Indicates the average density of this type of <see cref="Planetoid"/>, in kg/m³.
        /// </summary>
        internal override double DensityForType => densityForType;

        /// <summary>
        /// An optional string which is placed before a <see cref="CelestialEntity"/>'s <see cref="CelestialEntity.Designation"/>.
        /// </summary>
        protected override string DesignatorPrefix => "c";

        /// <summary>
        /// Initializes a new instance of <see cref="CTypeAsteroid"/>.
        /// </summary>
        public CTypeAsteroid() : base() { }

        /// <summary>
        /// Initializes a new instance of <see cref="CTypeAsteroid"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="CTypeAsteroid"/> is located.
        /// </param>
        public CTypeAsteroid(CelestialRegion parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="CTypeAsteroid"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="CTypeAsteroid"/> is located.
        /// </param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="CTypeAsteroid"/> during random generation, in kg.
        /// </param>
        public CTypeAsteroid(CelestialRegion parent, double maxMass) : base(parent, maxMass) { }

        /// <summary>
        /// Initializes a new instance of <see cref="CTypeAsteroid"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="CTypeAsteroid"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="CTypeAsteroid"/>.</param>
        public CTypeAsteroid(CelestialRegion parent, Vector3 position) : base(parent, position) { }

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
        public CTypeAsteroid(CelestialRegion parent, Vector3 position, double maxMass) : base(parent, position, maxMass) { }

        /// <summary>
        /// Determines an albedo for this <see cref="CelestialBody"/> (a value between 0 and 1).
        /// </summary>
        private protected override void GenerateAlbedo() => Albedo = (float)Math.Round(Randomizer.Static.NextDouble(0.03, 0.1), 2);

        /// <summary>
        /// Determines the <see cref="CelestialEntity.Substance"/> of this <see cref="CelestialEntity"/>.
        /// </summary>
        private protected override void GenerateSubstance()
        {
            var rock = 1.0f;

            var clay = (float)Math.Round(Randomizer.Static.NextDouble(0.1, 0.2), 3);
            rock -= clay;

            var ice = (float)Math.Round(Randomizer.Static.NextDouble(0.22), 3);
            rock -= ice;

            Substance = new Substance
            {
                Composition = new Composite(new Dictionary<(Chemical chemical, Phase phase), float>
                {
                    { (Chemical.Rock, Phase.Solid), rock },
                    { (Chemical.Clay, Phase.Solid), clay },
                    { (Chemical.Water, Phase.Solid), ice },
                }),
                Mass = GenerateMass(),
            };
            GenerateShape();
        }

        /// <summary>
        /// Generates a new satellite for this <see cref="Planetoid"/> with the specified parameters.
        /// </summary>
        /// <returns>A satellite <see cref="Planetoid"/> with an appropriate orbit.</returns>
        private protected override Planetoid GenerateSatellite(double periapsis, float eccentricity, double maxMass)
        {
            var satellite = new CTypeAsteroid(Parent, maxMass);
            SetAsteroidSatelliteOrbit(satellite, periapsis, eccentricity);
            return satellite;
        }
    }
}
