using Substances;
using System;
using System.Collections.Generic;
using MathAndScience.Numerics;
using WorldFoundry.CelestialBodies.Planetoids.Asteroids;
using WorldFoundry.CelestialBodies.Planetoids.Planets.DwarfPlanets;
using WorldFoundry.Space;
using MathAndScience.Shapes;

namespace WorldFoundry.CelestialBodies.Planetoids.Planets.TerrestrialPlanets
{
    /// <summary>
    /// A terrestrial planet with an unusually high concentration of carbon, rather than silicates,
    /// including such features as naturally-occurring steel, and diamond volcanoes.
    /// </summary>
    public class CarbonPlanet : TerrestrialPlanet
    {
        private protected override bool CanHaveOxygen => false;

        private protected override bool CanHaveWater => false;

        private protected override string PlanemoClassPrefix => "Carbon";

        /// <summary>
        /// Initializes a new instance of <see cref="CarbonPlanet"/>.
        /// </summary>
        internal CarbonPlanet() { }

        /// <summary>
        /// Initializes a new instance of <see cref="CarbonPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="CarbonPlanet"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="CarbonPlanet"/>.</param>
        internal CarbonPlanet(CelestialRegion parent, Vector3 position) : base(parent, position) { }

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
        internal CarbonPlanet(CelestialRegion parent, Vector3 position, double maxMass) : base(parent, position, maxMass) { }

        private protected override Planetoid GenerateSatellite(double periapsis, double eccentricity, double maxMass)
        {
            Planetoid satellite = null;
            var chance = Randomizer.Instance.NextDouble();

            // If the mass limit allows, there is an even chance that the satellite is a smaller planet.
            if (maxMass > _minMassForType && Randomizer.Instance.NextBoolean())
            {
                // Select from the standard distribution of types.

                // Planets with very low orbits are lava planets due to tidal stress (plus a small
                // percentage of others due to impact trauma).

                // The maximum mass and density are used to calculate an outer Roche limit (may not
                // be the actual Roche limit for the body which gets generated).
                if (periapsis < GetRocheLimit(_maxDensity) * 1.05 || chance <= 0.01)
                {
                    satellite = new LavaPlanet(Parent, Vector3.Zero, maxMass);
                }
                else if (chance <= 0.45) // Most will be standard terrestrial.
                {
                    satellite = new TerrestrialPlanet(Parent, Vector3.Zero, maxMass);
                }
                else if (chance <= 0.77)
                {
                    satellite = new CarbonPlanet(Parent, Vector3.Zero, maxMass);
                }
                else
                {
                    satellite = new OceanPlanet(Parent, Vector3.Zero, maxMass);
                }
            }

            // Otherwise, if the mass limit allows, there is an even chance that the satellite is a dwarf planet.
            else if (maxMass > DwarfPlanet._minMassForType && Randomizer.Instance.NextBoolean())
            {
                // Dwarf planets with very low orbits are lava planets due to tidal stress (plus a small percentage of others due to impact trauma).
                if (periapsis < GetRocheLimit(DwarfPlanet._densityForType) * 1.05 || chance <= 0.01)
                {
                    satellite = new LavaDwarfPlanet(Parent, Vector3.Zero, maxMass);
                }
                else if (chance <= 0.75) // Most will be standard.
                {
                    satellite = new DwarfPlanet(Parent, Vector3.Zero, maxMass);
                }
                else
                {
                    satellite = new RockyDwarfPlanet(Parent, Vector3.Zero, maxMass);
                }
            }

            // Otherwise, it is an asteroid, selected from the standard distribution of types.
            else if (maxMass > 0)
            {
                if (chance <= 0.75)
                {
                    satellite = new CTypeAsteroid(Parent, Vector3.Zero, maxMass);
                }
                else if (chance <= 0.9)
                {
                    satellite = new STypeAsteroid(Parent, Vector3.Zero, maxMass);
                }
                else
                {
                    satellite = new MTypeAsteroid(Parent, Vector3.Zero, maxMass);
                }
            }

            if (satellite != null)
            {
                Orbit.SetOrbit(
                    satellite,
                    this,
                    periapsis,
                    eccentricity,
                    Math.Round(Randomizer.Instance.NextDouble(0.5), 4),
                    Math.Round(Randomizer.Instance.NextDouble(Math.PI * 2), 4),
                    Math.Round(Randomizer.Instance.NextDouble(Math.PI * 2), 4),
                    Math.Round(Randomizer.Instance.NextDouble(Math.PI * 2), 4));
            }

            return satellite;
        }

        private protected override IEnumerable<(IComposition, double)> GetCore(double mass)
        {
            // Iron/steel-nickel core (some steel forms naturally in the carbon-rich environment).
            var coreNickel = Math.Round(Randomizer.Instance.NextDouble(0.03, 0.15), 4);
            var coreSteel = Math.Round(Randomizer.Instance.NextDouble(1 - coreNickel), 4);
            yield return (new Composite(
                (Chemical.Iron, Phase.Solid, 1 - coreNickel - coreSteel),
                (Chemical.Nickel, Phase.Solid, coreNickel),
                (Chemical.Steel, Phase.Solid, coreSteel)),
                1);
        }

        private protected override double GetCoreProportion(double mass) => 0.4;

        private protected override IEnumerable<(IComposition, double)> GetCrust()
        {
            // Rocky crust with trace elements
            // Metal content varies by approx. +/-15% from standard value in a Gaussian distribution.
            var metals = Math.Round(Randomizer.Instance.Normal(MetalProportion, 0.05 * MetalProportion), 4);

            var nickel = Math.Round(Randomizer.Instance.NextDouble(0.025, 0.075) * metals, 4);
            var aluminum = Math.Round(Randomizer.Instance.NextDouble(0.075, 0.225) * metals, 4);

            var titanium = Math.Round(Randomizer.Instance.NextDouble(0.05, 0.3) * metals, 4);

            var iron = metals - nickel - aluminum - titanium;
            var diamond = Math.Round(iron * Randomizer.Instance.NextDouble(0.01, 0.05), 4);
            iron -= diamond;
            var steel = Math.Round(Randomizer.Instance.NextDouble(iron), 4);
            iron -= steel;

            var copper = titanium > 0 ? Math.Round(Randomizer.Instance.NextDouble(titanium), 4) : 0;
            titanium -= copper;

            var lead = titanium > 0 ? Math.Round(Randomizer.Instance.NextDouble(titanium), 4) : 0;
            titanium -= lead;

            var uranium = titanium > 0 ? Math.Round(Randomizer.Instance.NextDouble(titanium), 4) : 0;
            titanium -= uranium;

            var tin = titanium > 0 ? Math.Round(Randomizer.Instance.NextDouble(titanium), 4) : 0;
            titanium -= tin;

            var silver = titanium > 0 ? Math.Round(Randomizer.Instance.NextDouble(titanium), 4) : 0;
            titanium -= silver;

            var gold = titanium > 0 ? Math.Round(Randomizer.Instance.NextDouble(titanium), 4) : 0;
            titanium -= gold;

            var platinum = titanium > 0 ? Math.Round(Randomizer.Instance.NextDouble(titanium), 4) : 0;
            titanium -= platinum;

            var sulfur = Math.Round(Randomizer.Instance.Normal(3.5e-5, 0.05 * 3.5e-5), 4);

            var rock = 1 - metals - sulfur;

            yield return (new Composite(
                (Chemical.Aluminium, Phase.Solid, aluminum),
                (Chemical.Copper, Phase.Solid, copper),
                (Chemical.Diamond, Phase.Solid, diamond),
                (Chemical.Gold, Phase.Solid, gold),
                (Chemical.Iron, Phase.Solid, iron),
                (Chemical.Lead, Phase.Solid, lead),
                (Chemical.Nickel, Phase.Solid, nickel),
                (Chemical.Platinum, Phase.Solid, platinum),
                (Chemical.Rock, Phase.Solid, rock),
                (Chemical.Silver, Phase.Solid, silver),
                (Chemical.Steel, Phase.Solid, steel),
                (Chemical.Sulfur, Phase.Solid, sulfur),
                (Chemical.Tin, Phase.Solid, tin),
                (Chemical.Titanium, Phase.Solid, titanium),
                (Chemical.Uranium, Phase.Solid, uranium)),
                1);
        }

        private protected override IEnumerable<(IComposition, double)> GetMantle(IShape shape, double proportion)
        {
            // Molten rock mantle, with a significant amount of compressed carbon in form of diamond
            var diamond = Math.Round(Randomizer.Instance.NextDouble(0.01, 0.05), 4);
            yield return (new Composite(
                (Chemical.Diamond, Phase.Solid, diamond),
                (Chemical.Rock, Phase.Liquid, 1 - diamond)),
                1);
        }
    }
}
