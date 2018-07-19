using Substances;
using System;
using System.Collections.Generic;
using System.Numerics;
using WorldFoundry.Space;

namespace WorldFoundry.CelestialBodies.Planetoids.Planets.TerrestrialPlanets
{
    /// <summary>
    /// A terrestrial planet consisting of an unusually high proportion of water, with a mantle
    /// consisting of a form of high-pressure, hot ice, and possibly a supercritical
    /// surface-atmosphere blend.
    /// </summary>
    public class OceanPlanet : TerrestrialPlanet
    {
        private const bool hasFlatSurface = true;
        /// <summary>
        /// Indicates that this <see cref="Planetoid"/>'s surface does not have elevation variations
        /// (i.e. is non-solid). Prevents generation of a height map during <see
        /// cref="Planetoid.Topography"/> generation.
        /// </summary>
        public override bool HasFlatSurface => hasFlatSurface;

        /// <summary>
        /// A factor which multiplies the chance of this <see cref="Planetoid"/> having a strong magnetosphere.
        /// </summary>
        /// <remarks>
        /// The cores of ocean planets are liable to cool more rapidly than other planets of similar
        /// size, reducing the chances of producing the required dynamo effect.
        /// </remarks>
        public override double MagnetosphereChanceFactor => 0.5;

        private const string planemoClassPrefix = "Ocean";
        /// <summary>
        /// A prefix to the <see cref="CelestialEntity.TypeName"/> for this class of <see cref="Planemo"/>.
        /// </summary>
        public override string PlanemoClassPrefix => planemoClassPrefix;

        /// <summary>
        /// Initializes a new instance of <see cref="OceanPlanet"/>.
        /// </summary>
        public OceanPlanet() : base() { }

        /// <summary>
        /// Initializes a new instance of <see cref="OceanPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="OceanPlanet"/> is located.
        /// </param>
        public OceanPlanet(CelestialRegion parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="OceanPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="OceanPlanet"/> is located.
        /// </param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="OceanPlanet"/> during random generation, in kg.
        /// </param>
        public OceanPlanet(CelestialRegion parent, double maxMass) : base(parent, maxMass) { }

        /// <summary>
        /// Initializes a new instance of <see cref="OceanPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="OceanPlanet"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="OceanPlanet"/>.</param>
        public OceanPlanet(CelestialRegion parent, Vector3 position) : base(parent, position) { }

        /// <summary>
        /// Initializes a new instance of <see cref="OceanPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="OceanPlanet"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="OceanPlanet"/>.</param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="OceanPlanet"/> during random generation, in kg.
        /// </param>
        public OceanPlanet(CelestialRegion parent, Vector3 position, double maxMass) : base(parent, position, maxMass) { }

        /// <summary>
        /// Determines the <see cref="CelestialEntity.Substance"/> of this <see cref="CelestialEntity"/>.
        /// </summary>
        private protected override void GenerateSubstance()
        {
            var layers = new List<(IComposition substance, double proportion)>();

            // Iron-nickel core.
            var coreProportion = GetCoreProportion();
            var coreNickel = Math.Round(Randomizer.Static.NextDouble(0.03, 0.15), 4);
            layers.Add((new Composite(new Dictionary<(Chemical chemical, Phase phase), double>
            {
                { (Chemical.Iron, Phase.Solid), 1 - coreNickel },
                { (Chemical.Nickel, Phase.Solid), coreNickel },
            }), coreProportion));

            var crustProportion = GetCrustProportion();

            var mantleProportion = 1 - coreProportion - crustProportion;

            // Thin magma mantle
            var magmaMantle = mantleProportion * 0.1;
            layers.Add((new Material(Chemical.Rock, Phase.Liquid), magmaMantle));

            // Rocky crust with trace elements
            // Metal content varies by approx. +/-15% from standard value in a Gaussian distribution.
            var metals = Math.Round(Randomizer.Static.Normal(MetalProportion, 0.05 * MetalProportion), 4);

            var nickel = Math.Round(Randomizer.Static.NextDouble(0.025, 0.075) * metals, 4);
            var aluminum = Math.Round(Randomizer.Static.NextDouble(0.075, 0.225) * metals, 4);

            var titanium = Math.Round(Randomizer.Static.NextDouble(0.05, 0.3) * metals, 4);

            var iron = metals - nickel - aluminum - titanium;

            var copper = Math.Round(Randomizer.Static.NextDouble(titanium), 4);
            titanium -= copper;

            var lead = titanium > 0 ? Math.Round(Randomizer.Static.NextDouble(titanium), 4) : 0;
            titanium -= lead;

            var uranium = titanium > 0 ? Math.Round(Randomizer.Static.NextDouble(titanium), 4) : 0;
            titanium -= uranium;

            var tin = titanium > 0 ? Math.Round(Randomizer.Static.NextDouble(titanium), 4) : 0;
            titanium -= tin;

            var silver = Math.Round(Randomizer.Static.NextDouble(titanium), 4);
            titanium -= silver;

            var gold = Math.Round(Randomizer.Static.NextDouble(titanium), 4);
            titanium -= gold;

            var platinum = Math.Round(Randomizer.Static.NextDouble(titanium), 4);
            titanium -= platinum;

            var rock = 1 - metals;

            layers.Add((new Composite(new Dictionary<(Chemical chemical, Phase phase), double>
            {
                { (Chemical.Aluminium, Phase.Solid), aluminum },
                { (Chemical.Copper, Phase.Solid), copper },
                { (Chemical.Gold, Phase.Solid), gold },
                { (Chemical.Iron, Phase.Solid), iron },
                { (Chemical.Lead, Phase.Solid), lead },
                { (Chemical.Nickel, Phase.Solid), nickel },
                { (Chemical.Platinum, Phase.Solid), platinum },
                { (Chemical.Rock, Phase.Solid), rock },
                { (Chemical.Silver, Phase.Solid), silver },
                { (Chemical.Tin, Phase.Solid), tin },
                { (Chemical.Titanium, Phase.Solid), titanium },
                { (Chemical.Uranium, Phase.Solid), uranium },
            }), crustProportion));

            // Ice mantle
            var iceMantle = mantleProportion * 0.4;
            layers.Add((new Material(Chemical.Water, Phase.Solid), iceMantle));

            Substance = new Substance() { Composition = new LayeredComposite(layers) };
            Substance.Composition.BalanceProportions();
            GenerateShape();

            // Hydrosphere makes up the bulk of the planet, and therefore is generated as part of Composition.
            HydrosphereProportion = mantleProportion / 2;
            // Surface water is mostly salt water.
            var saltWater = Math.Round(Randomizer.Static.Normal(0.945, 0.015), 3);
            _hydrosphere = new Composite(new Dictionary<(Chemical chemical, Phase phase), double>
            {
                { (Chemical.Water, Phase.Liquid), 1 - saltWater },
                { (Chemical.Water_Salt, Phase.Liquid), saltWater },
            });
        }

        /// <summary>
        /// Generates an appropriate hydrosphere for this <see cref="TerrestrialPlanet"/>.
        /// </summary>
        /// <remarks>
        /// Ocean planets have a thick hydrosphere layer generated as part of the <see cref="CelestialEntity.Substance"/>.
        /// </remarks>
        private protected override void GenerateHydrosphere() => GenerateSubstance();
    }
}
