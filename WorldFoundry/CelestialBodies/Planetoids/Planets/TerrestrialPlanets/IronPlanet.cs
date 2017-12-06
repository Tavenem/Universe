using System.Numerics;
using WorldFoundry.Space;

namespace WorldFoundry.CelestialBodies.Planetoids.Planets.TerrestrialPlanets
{
    /// <summary>
    /// A relatively small terrestrial planet consisting of an unusually large iron-nickel core, and
    /// an unusually thin mantle (similar to Mercury).
    /// </summary>
    public class IronPlanet : TerrestrialPlanet
    {
        private const float CoreProportion = 0.4f;

        internal new static string baseTypeName = "Iron Planet";
        /// <summary>
        /// The base name for this type of <see cref="CelestialEntity"/>.
        /// </summary>
        public override string BaseTypeName => baseTypeName;

        internal new static bool canHaveWater = false;
        /// <summary>
        /// Used to allow or prevent water in the composition and atmosphere of a terrestrial planet.
        /// </summary>
        /// <remarks>
        /// Unable to have water due to its high reactivity with iron compounds.
        /// </remarks>
        protected override bool CanHaveWater => canHaveWater;

        /// <summary>
        /// A factor which multiplies the chance of this <see cref="Planetoid"/> having a strong magnetosphere.
        /// </summary>
        /// <remarks>
        /// Iron planets, with their large nickel-iron cores, can much more readily produce the
        /// required dynamo effect.
        /// </remarks>
        public override float MagnetosphereChanceFactor => 5;

        internal new static float maxDensity = 8000;
        private protected override float MaxDensity => maxDensity;

        internal new static float metalProportion = 0.25f;
        /// <summary>
        /// Used to set the proportionate amount of metal in the composition of a terrestrial planet.
        /// </summary>
        private protected override float MetalProportion => metalProportion;

        internal new static float minDensity = 5250;
        private protected override float MinDensity => minDensity;

        private static string planemoClassPrefix = "Iron";
        /// <summary>
        /// A prefix to the <see cref="CelestialEntity.TypeName"/> for this class of <see cref="Planemo"/>.
        /// </summary>
        public override string PlanemoClassPrefix => planemoClassPrefix;

        /// <summary>
        /// Initializes a new instance of <see cref="IronPlanet"/>.
        /// </summary>
        public IronPlanet() { }

        /// <summary>
        /// Initializes a new instance of <see cref="IronPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="IronPlanet"/> is located.
        /// </param>
        public IronPlanet(CelestialObject parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="IronPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="IronPlanet"/> is located.
        /// </param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="IronPlanet"/> during random generation, in kg.
        /// </param>
        public IronPlanet(CelestialObject parent, double maxMass) : base(parent, maxMass) { }

        /// <summary>
        /// Initializes a new instance of <see cref="IronPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="IronPlanet"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="IronPlanet"/>.</param>
        public IronPlanet(CelestialObject parent, Vector3 position) : base(parent, position) { }

        /// <summary>
        /// Initializes a new instance of <see cref="IronPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="IronPlanet"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="IronPlanet"/>.</param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="IronPlanet"/> during random generation, in kg.
        /// </param>
        public IronPlanet(CelestialObject parent, Vector3 position, double maxMass) : base(parent, position, maxMass) { }

        /// <summary>
        /// Randomly determines the proportionate amount of the composition devoted to the core of a <see cref="Planemo"/>.
        /// </summary>
        /// <returns>A proportion, from 0.0 to 1.0.</returns>
        private protected override float GetCoreProportion() => CoreProportion;
    }
}
