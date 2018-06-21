using Substances;
using System;
using System.Collections.Generic;
using System.Numerics;
using WorldFoundry.CelestialBodies.Planetoids.Asteroids;
using WorldFoundry.CelestialBodies.Planetoids.Planets.DwarfPlanets;
using WorldFoundry.Space;
using WorldFoundry.Substances;

namespace WorldFoundry.CelestialBodies.Planetoids.Planets.TerrestrialPlanets
{
    /// <summary>
    /// A terrestrial planet with an unusually high concentration or carbon, rather than silicates,
    /// including such features as naturally-occurring steel, and diamond volcanoes.
    /// </summary>
    public class CarbonPlanet : TerrestrialPlanet
    {
        private const float CoreProportion = 0.4f;

        internal new static string baseTypeName = "Carbon Planet";
        /// <summary>
        /// The base name for this type of <see cref="CelestialEntity"/>.
        /// </summary>
        public override string BaseTypeName => baseTypeName;

        internal new static bool canHaveOxygen = false;
        /// <summary>
        /// Used to allow or prevent oxygen in the composition and atmosphere of a terrestrial planet.
        /// </summary>
        /// <remarks>
        /// Unable to have oxygen due to its high reactivity with carbon compounds.
        /// </remarks>
        protected override bool CanHaveOxygen => canHaveOxygen;

        internal new static bool canHaveWater = false;
        /// <summary>
        /// Used to allow or prevent water in the composition and atmosphere of a terrestrial planet.
        /// </summary>
        /// <remarks>
        /// Unable to have water due to its high reactivity with carbon compounds.
        /// </remarks>
        protected override bool CanHaveWater => canHaveWater;

        private static readonly string planemoClassPrefix = "Carbon";
        /// <summary>
        /// A prefix to the <see cref="CelestialEntity.TypeName"/> for this class of <see cref="Planemo"/>.
        /// </summary>
        public override string PlanemoClassPrefix => planemoClassPrefix;

        /// <summary>
        /// Initializes a new instance of <see cref="CarbonPlanet"/>.
        /// </summary>
        public CarbonPlanet() : base() { }

        /// <summary>
        /// Initializes a new instance of <see cref="CarbonPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="CarbonPlanet"/> is located.
        /// </param>
        public CarbonPlanet(CelestialRegion parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="CarbonPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="CarbonPlanet"/> is located.
        /// </param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="CarbonPlanet"/> during random generation, in kg.
        /// </param>
        public CarbonPlanet(CelestialRegion parent, double maxMass) : base(parent, maxMass) { }

        /// <summary>
        /// Initializes a new instance of <see cref="CarbonPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="CarbonPlanet"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="CarbonPlanet"/>.</param>
        public CarbonPlanet(CelestialRegion parent, Vector3 position) : base(parent, position) { }

        /// <summary>
        /// Initializes a new instance of <see cref="CarbonPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="CarbonPlanet"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="CarbonPlanet"/>.</param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="CarbonPlanet"/> during random generation, in kg.
        /// </param>
        public CarbonPlanet(CelestialRegion parent, Vector3 position, double maxMass) : base(parent, position, maxMass) { }

        /// <summary>
        /// Determines the <see cref="CelestialEntity.Substance"/> of this <see cref="CelestialEntity"/>.
        /// </summary>
        private protected override void GenerateSubstance()
        {
            var layers = new List<(IComposition substance, float proportion)>();

            // Iron/steel-nickel core (some steel forms naturally in the carbon-rich environment).
            var coreProportion = GetCoreProportion();
            var coreNickel = (float)Math.Round(Randomizer.Static.NextDouble(0.03, 0.15), 4);
            var coreSteel = (float)Math.Round(Randomizer.Static.NextDouble(1 - coreNickel), 4);
            layers.Add((new Composite(new Dictionary<(Chemical chemical, Phase phase), float>
            {
                { (Chemical.Iron, Phase.Solid), 1 - coreNickel - coreSteel },
                { (Chemical.Nickel, Phase.Solid), coreNickel },
                { (Chemical.Steel, Phase.Solid), coreSteel },
            }), coreProportion));

            var crustProportion = GetCrustProportion();

            // Molten rock mantle, with a significant amount of compressed carbon in form of diamond
            var mantleProportion = 1 - coreProportion - crustProportion;
            var diamond = (float)Math.Round(Randomizer.Static.NextDouble(0.01, 0.05), 4);
            layers.Add((new Composite(new Dictionary<(Chemical chemical, Phase phase), float>
            {
                { (Chemical.Diamond, Phase.Solid), diamond },
                { (Chemical.Rock, Phase.Liquid), 1 - diamond },
            }), mantleProportion));

            // Rocky crust with trace elements
            // Metal content varies by approx. +/-15% from standard value in a Gaussian distribution.
            var metals = (float)Math.Round(Randomizer.Static.Normal(MetalProportion, 0.05 * MetalProportion), 4);

            var nickel = (float)Math.Round(Randomizer.Static.NextDouble(0.025, 0.075) * metals, 4);
            var aluminum = (float)Math.Round(Randomizer.Static.NextDouble(0.075, 0.225) * metals, 4);

            var titanium = (float)Math.Round(Randomizer.Static.NextDouble(0.05, 0.3) * metals, 4);

            var iron = metals - nickel - aluminum - titanium;
            diamond = (float)Math.Round(iron * Randomizer.Static.NextDouble(0.01, 0.05), 4);
            iron -= diamond;
            var steel = (float)Math.Round(Randomizer.Static.NextDouble(iron), 4);
            iron -= steel;

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
                { (Chemical.Aluminum, Phase.Solid), aluminum },
                { (Chemical.Copper, Phase.Solid), copper },
                { (Chemical.Diamond, Phase.Solid), diamond },
                { (Chemical.Gold, Phase.Solid), gold },
                { (Chemical.Iron, Phase.Solid), iron },
                { (Chemical.Nickel, Phase.Solid), nickel },
                { (Chemical.Platinum, Phase.Solid), platinum },
                { (Chemical.Rock, Phase.Solid), rock },
                { (Chemical.Silver, Phase.Solid), silver },
                { (Chemical.Steel, Phase.Solid), steel },
                { (Chemical.Titanium, Phase.Solid), titanium },
            }), crustProportion));

            Substance = new Substance() { Composition = new LayeredComposite(layers) };
            GenerateShape();
        }

        /// <summary>
        /// Generates a new satellite for this <see cref="Planetoid"/> with the specified parameters.
        /// </summary>
        /// <returns>A satellite <see cref="Planetoid"/> with an appropriate orbit.</returns>
        private protected override Planetoid GenerateSatellite(double periapsis, float eccentricity, double maxMass)
        {
            Planetoid satellite = null;
            var chance = Randomizer.Static.NextDouble();

            // If the mass limit allows, there is an even chance that the satellite is a smaller planet.
            if (maxMass > minMassForType && Randomizer.Static.NextBoolean())
            {
                // Select from the standard distribution of types.

                // Planets with very low orbits are lava planets due to tidal stress (plus a small
                // percentage of others due to impact trauma).

                // The maximum mass and density are used to calculate an outer Roche limit (may not
                // be the actual Roche limit for the body which gets generated).
                if (periapsis < GetRocheLimit(maxDensity) * 1.05 || chance <= 0.01)
                {
                    satellite = new LavaPlanet(Parent, maxMass);
                }
                else if (chance <= 0.45) // Most will be standard terrestrial.
                {
                    satellite = new TerrestrialPlanet(Parent, maxMass);
                }
                else if (chance <= 0.77)
                {
                    satellite = new CarbonPlanet(Parent, maxMass);
                }
                else
                {
                    satellite = new OceanPlanet(Parent, maxMass);
                }
            }

            // Otherwise, if the mass limit allows, there is an even chance that the satellite is a dwarf planet.
            else if (maxMass > DwarfPlanet.minMassForType && Randomizer.Static.NextBoolean())
            {
                // Dwarf planets with very low orbits are lava planets due to tidal stress (plus a small percentage of others due to impact trauma).
                if (periapsis < GetRocheLimit(DwarfPlanet.densityForType) * 1.05 || chance <= 0.01)
                {
                    satellite = new LavaDwarfPlanet(Parent, maxMass);
                }
                else if (chance <= 0.75) // Most will be standard.
                {
                    satellite = new DwarfPlanet(Parent, maxMass);
                }
                else
                {
                    satellite = new RockyDwarfPlanet(Parent, maxMass);
                }
            }

            // Otherwise, it is an asteroid, selected from the standard distribution of types.
            else if (maxMass > 0)
            {
                if (chance <= 0.75)
                {
                    satellite = new CTypeAsteroid(Parent, maxMass);
                }
                else if (chance <= 0.9)
                {
                    satellite = new STypeAsteroid(Parent, maxMass);
                }
                else
                {
                    satellite = new MTypeAsteroid(Parent, maxMass);
                }
            }

            if (satellite != null)
            {
                Orbits.Orbit.SetOrbit(
                    satellite,
                    this,
                    periapsis,
                    eccentricity,
                    (float)Math.Round(Randomizer.Static.NextDouble(0.5), 4),
                    (float)Math.Round(Randomizer.Static.NextDouble(Math.PI * 2), 4),
                    (float)Math.Round(Randomizer.Static.NextDouble(Math.PI * 2), 4),
                    (float)Math.Round(Randomizer.Static.NextDouble(Math.PI * 2), 4));
            }

            return satellite;
        }
    }
}
