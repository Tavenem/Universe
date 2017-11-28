using System;
using System.Numerics;
using WorldFoundry.Space;
using WorldFoundry.Substances;
using WorldFoundry.Utilities;

namespace WorldFoundry.CelestialBodies.Planetoids.Planets.TerrestrialPlanets
{
    /// <summary>
    /// A terrestrial planet consisting of an unusually high proportion of water, with a mantle
    /// consisting of a form of high-pressure, hot ice, and possibly a supercritical
    /// surface-atmosphere blend.
    /// </summary>
    public class OceanPlanet : TerrestrialPlanet
    {
        internal new const string baseTypeName = "Ocean Planet";
        /// <summary>
        /// The base name for this type of <see cref="CelestialEntity"/>.
        /// </summary>
        public override string BaseTypeName => baseTypeName;

        /// <summary>
        /// A factor which multiplies the chance of this <see cref="Planetoid"/> having a strong magnetosphere.
        /// </summary>
        /// <remarks>
        /// The cores of ocean planets are liable to cool more rapidly than other planets of similar
        /// size, reducing the chances of producing the required dynamo effect.
        /// </remarks>
        public override float MagnetosphereChanceFactor => 0.5f;

        private const string planemoClassPrefix = "Ocean";
        /// <summary>
        /// A prefix to the <see cref="CelestialEntity.TypeName"/> for this class of <see cref="Planemo"/>.
        /// </summary>
        public override string PlanemoClassPrefix => planemoClassPrefix;

        /// <summary>
        /// Initializes a new instance of <see cref="OceanPlanet"/>.
        /// </summary>
        public OceanPlanet() { }

        /// <summary>
        /// Initializes a new instance of <see cref="OceanPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="OceanPlanet"/> is located.
        /// </param>
        public OceanPlanet(CelestialObject parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="OceanPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="OceanPlanet"/> is located.
        /// </param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="OceanPlanet"/> during random generation, in kg.
        /// </param>
        public OceanPlanet(CelestialObject parent, double maxMass) : base(parent, maxMass) { }

        /// <summary>
        /// Initializes a new instance of <see cref="OceanPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="OceanPlanet"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="OceanPlanet"/>.</param>
        public OceanPlanet(CelestialObject parent, Vector3 position) : base(parent, position) { }

        /// <summary>
        /// Initializes a new instance of <see cref="OceanPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="OceanPlanet"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="OceanPlanet"/>.</param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="OceanPlanet"/> during random generation, in kg.
        /// </param>
        public OceanPlanet(CelestialObject parent, Vector3 position, double maxMass) : base(parent, position, maxMass) { }

        /// <summary>
        /// Determines the composition of this <see cref="Planetoid"/>.
        /// </summary>
        protected override void GenerateComposition()
        {
            Composition = new Mixture();

            // Iron-nickel core.
            var coreProportion = GetCoreProportion();
            var coreNickel = (float)Math.Round(Randomizer.Static.NextDouble(0.03, 0.15), 4);
            Composition.Mixtures.Add(new Mixture(new MixtureComponent[]
            {
                new MixtureComponent
                {
                    Chemical = Chemical.Iron,
                    Phase = Phase.Solid,
                    Proportion = 1 - coreNickel,
                },
                new MixtureComponent
                {
                    Chemical = Chemical.Nickel,
                    Phase = Phase.Solid,
                    Proportion = coreNickel,
                },
            })
            {
                Proportion = coreProportion,
            });

            var crustProportion = GetCrustProportion();
            var mantleProportion = 1 - coreProportion - crustProportion;

            // Thin magma mantle
            var magmaMantle = mantleProportion * 0.1f;
            Composition.Mixtures.Add(new Mixture(1, new MixtureComponent[]
            {
                new MixtureComponent
                {
                    Chemical = Chemical.Rock,
                    Phase = Phase.Liquid,
                    Proportion = 1,
                },
            })
            {
                Proportion = magmaMantle,
            });

            // Rocky crust with trace elements
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

            Composition.Mixtures.Add(new Mixture(2, new MixtureComponent[]
            {
                new MixtureComponent
                {
                    Chemical = Chemical.Aluminum,
                    Phase = Phase.Solid,
                    Proportion = aluminum,
                },
                new MixtureComponent
                {
                    Chemical = Chemical.Copper,
                    Phase = Phase.Solid,
                    Proportion = copper,
                },
                new MixtureComponent
                {
                    Chemical = Chemical.Gold,
                    Phase = Phase.Solid,
                    Proportion = gold,
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
                    Chemical = Chemical.Platinum,
                    Phase = Phase.Solid,
                    Proportion = platinum,
                },
                new MixtureComponent
                {
                    Chemical = Chemical.Rock,
                    Phase = Phase.Solid,
                    Proportion = rock,
                },
                new MixtureComponent
                {
                    Chemical = Chemical.Silver,
                    Phase = Phase.Solid,
                    Proportion = silver,
                },
                new MixtureComponent
                {
                    Chemical = Chemical.Titanium,
                    Phase = Phase.Solid,
                    Proportion = titanium,
                },
            })
            {
                Proportion = crustProportion,
            });

            // Ice mantle
            var iceMantle = mantleProportion * 0.4f;
            Composition.Mixtures.Add(new Mixture(3, new MixtureComponent[]
            {
                new MixtureComponent
                {
                    Chemical = Chemical.Water,
                    Phase = Phase.Solid,
                    Proportion = 1,
                },
            })
            {
                Proportion = iceMantle,
            });
            Composition.BalanceProportions(children: true);

            // Hydrosphere makes up the bulk of the planet, and therefore is generated as part of Composition.
            var water = mantleProportion / 2;
            // Surface water is mostly salt water.
            var saltWater = (float)Math.Round(Randomizer.Static.Normal(0.945, 0.015), 3);
            Hydrosphere = new Mixture(new MixtureComponent[]
            {
                new MixtureComponent
                {
                    Chemical = Chemical.Water,
                    Phase = Phase.Liquid,
                    Proportion = 1 - saltWater,
                },
                new MixtureComponent
                {
                    Chemical = Chemical.Water_Salt,
                    Phase = Phase.Liquid,
                    Proportion = saltWater,
                },
            })
            {
                Proportion = water,
            };
        }

        /// <summary>
        /// Generates an appropriate hydrosphere for this <see cref="TerrestrialPlanet"/>.
        /// </summary>
        /// <remarks>
        /// Ocean planets have a thick hydrosphere layer generated as part of the <see cref="Planetoid.Composition"/>.
        /// </remarks>
        protected override void GenerateHydrosphere() => GenerateComposition();
    }
}
