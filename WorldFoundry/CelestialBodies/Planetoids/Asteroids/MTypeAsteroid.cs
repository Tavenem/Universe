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
        private const string _baseTypeName = "M-Type Asteroid";
        /// <summary>
        /// The base name for this type of <see cref="CelestialEntity"/>.
        /// </summary>
        public override string BaseTypeName => _baseTypeName;

        private const double _densityForType = 5320;
        /// <summary>
        /// Indicates the average density of this type of <see cref="Planetoid"/>, in kg/m³.
        /// </summary>
        internal override double DensityForType => _densityForType;

        /// <summary>
        /// An optional string which is placed before a <see cref="CelestialEntity"/>'s <see cref="CelestialEntity.Designation"/>.
        /// </summary>
        protected override string DesignatorPrefix => "m";

        /// <summary>
        /// Initializes a new instance of <see cref="MTypeAsteroid"/>.
        /// </summary>
        public MTypeAsteroid() { }

        /// <summary>
        /// Initializes a new instance of <see cref="MTypeAsteroid"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="MTypeAsteroid"/> is located.
        /// </param>
        public MTypeAsteroid(CelestialRegion parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="MTypeAsteroid"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="MTypeAsteroid"/> is located.
        /// </param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="MTypeAsteroid"/> during random generation, in kg.
        /// </param>
        public MTypeAsteroid(CelestialRegion parent, double maxMass) : base(parent, maxMass) { }

        /// <summary>
        /// Initializes a new instance of <see cref="MTypeAsteroid"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="MTypeAsteroid"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="MTypeAsteroid"/>.</param>
        public MTypeAsteroid(CelestialRegion parent, Vector3 position) : base(parent, position) { }

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
        public MTypeAsteroid(CelestialRegion parent, Vector3 position, double maxMass) : base(parent, position, maxMass) { }

        /// <summary>
        /// Determines an albedo for this <see cref="CelestialBody"/> (a value between 0 and 1).
        /// </summary>
        private protected override void GenerateAlbedo() => Albedo = Randomizer.Instance.NextDouble(0.1, 0.2);

        /// <summary>
        /// Generates a new satellite for this <see cref="Planetoid"/> with the specified parameters.
        /// </summary>
        /// <param name="periapsis">The periapsis of the satellite's orbit.</param>
        /// <param name="eccentricity">The eccentricity of the satellite's orbit.</param>
        /// <param name="maxMass">The maximum mass of the satellite.</param>
        /// <returns>A satellite <see cref="Planetoid"/> with an appropriate orbit.</returns>
        private protected override Planetoid GenerateSatellite(double periapsis, double eccentricity, double maxMass)
        {
            var satellite = new MTypeAsteroid(Parent, maxMass);
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
