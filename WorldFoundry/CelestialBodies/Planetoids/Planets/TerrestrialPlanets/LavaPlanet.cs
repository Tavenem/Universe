using Substances;
using System;
using System.Collections.Generic;
using System.Numerics;
using WorldFoundry.Space;

namespace WorldFoundry.CelestialBodies.Planetoids.Planets.TerrestrialPlanets
{
    /// <summary>
    /// A terrestrial planet with little to no crust, whether due to a catastrophic collision event,
    /// or severe tidal forces due to a close orbit.
    /// </summary>
    public class LavaPlanet : TerrestrialPlanet
    {
        internal new static string baseTypeName = "Lava Planet";
        /// <summary>
        /// The base name for this type of <see cref="CelestialEntity"/>.
        /// </summary>
        public override string BaseTypeName => baseTypeName;

        internal new static bool canHaveWater = false;
        /// <summary>
        /// Used to allow or prevent water in the composition and atmosphere of a terrestrial planet.
        /// </summary>
        /// <remarks>
        /// Unable to have water due to extreme temperature.
        /// </remarks>
        protected override bool CanHaveWater => canHaveWater;

        internal new static int maxSatellites = 0;
        /// <summary>
        /// The upper limit on the number of satellites this <see cref="Planetoid"/> might have. The
        /// actual number is determined by the orbital characteristics of the satellites it actually has.
        /// </summary>
        /// <remarks>
        /// Lava planets have no satellites; whatever forces have caused their surface trauma should
        /// also inhibit stable satellite orbits.
        /// </remarks>
        public override int MaxSatellites => maxSatellites;

        private static readonly string planemoClassPrefix = "Lava";
        /// <summary>
        /// A prefix to the <see cref="CelestialEntity.TypeName"/> for this class of <see cref="Planemo"/>.
        /// </summary>
        public override string PlanemoClassPrefix => planemoClassPrefix;

        /// <summary>
        /// Initializes a new instance of <see cref="LavaPlanet"/>.
        /// </summary>
        public LavaPlanet() : base() { }

        /// <summary>
        /// Initializes a new instance of <see cref="LavaPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="LavaPlanet"/> is located.
        /// </param>
        public LavaPlanet(CelestialRegion parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="LavaPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="LavaPlanet"/> is located.
        /// </param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="LavaPlanet"/> during random generation, in kg.
        /// </param>
        public LavaPlanet(CelestialRegion parent, double maxMass) : base(parent, maxMass) { }

        /// <summary>
        /// Initializes a new instance of <see cref="LavaPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="LavaPlanet"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="LavaPlanet"/>.</param>
        public LavaPlanet(CelestialRegion parent, Vector3 position) : base(parent, position) { }

        /// <summary>
        /// Initializes a new instance of <see cref="LavaPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="LavaPlanet"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="LavaPlanet"/>.</param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="LavaPlanet"/> during random generation, in kg.
        /// </param>
        public LavaPlanet(CelestialRegion parent, Vector3 position, double maxMass) : base(parent, position, maxMass) { }

        /// <summary>
        /// Determines the <see cref="CelestialEntity.Substance"/> of this <see cref="CelestialEntity"/>.
        /// </summary>
        private protected override void GenerateSubstance()
        {
            var layers = new List<(IComposition substance, float proportion)>();

            // Iron-nickel core.
            var coreProportion = GetCoreProportion();
            var coreNickel = (float)Math.Round(Randomizer.Static.NextDouble(0.03, 0.15), 4);
            layers.Add((new Composite(new Dictionary<(Chemical chemical, Phase phase), float>
            {
                { (Chemical.Iron, Phase.Solid), 1 - coreNickel },
                { (Chemical.Nickel, Phase.Solid), coreNickel },
            }), coreProportion));

            var crustProportion = GetCrustProportion();

            // Molten rock mantle
            var mantleProportion = 1 - coreProportion - crustProportion;
            layers.Add((new Material(Chemical.Rock, Phase.Liquid), mantleProportion));

            // Molten crust with trace elements
            // Metal content varies by approx. +/-15% from standard value in a Gaussian distribution.
            var metals = (float)Math.Round(Randomizer.Static.Normal(MetalProportion, 0.05 * MetalProportion), 4);

            var nickel = (float)Math.Round(Randomizer.Static.NextDouble(0.025, 0.075) * metals, 4);
            var aluminum = (float)Math.Round(Randomizer.Static.NextDouble(0.075, 0.225) * metals, 4);

            var titanium = (float)Math.Round(Randomizer.Static.NextDouble(0.05, 0.3) * metals, 4);

            var iron = metals - nickel - aluminum - titanium;

            var copper = (float)Math.Round(Randomizer.Static.NextDouble(titanium), 4);
            titanium -= copper;

            var silver = (float)Math.Round(Randomizer.Static.NextDouble(titanium), 4);
            titanium -= silver;

            var gold = (float)Math.Round(Randomizer.Static.NextDouble(titanium), 4);
            titanium -= gold;

            var platinum = (float)Math.Round(Randomizer.Static.NextDouble(titanium), 4);
            titanium -= platinum;

            var rock = 1 - metals;

            layers.Add((new Composite(new Dictionary<(Chemical chemical, Phase phase), float>
            {
                { (Chemical.Aluminum, Phase.Liquid), aluminum },
                { (Chemical.Copper, Phase.Liquid), copper },
                { (Chemical.Gold, Phase.Liquid), gold },
                { (Chemical.Iron, Phase.Liquid), iron },
                { (Chemical.Nickel, Phase.Liquid), nickel },
                { (Chemical.Platinum, Phase.Liquid), platinum },
                { (Chemical.Rock, Phase.Liquid), rock },
                { (Chemical.Silver, Phase.Liquid), silver },
                { (Chemical.Titanium, Phase.Liquid), titanium },
            }), crustProportion));

            Substance = new Substance()
            {
                Composition = new LayeredComposite(layers),
                Temperature = (float)Randomizer.Static.NextDouble(974.15, 1574.15),
            };
            GenerateShape();
        }
    }
}
