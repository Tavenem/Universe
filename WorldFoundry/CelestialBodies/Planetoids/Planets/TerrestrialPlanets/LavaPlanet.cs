using Substances;
using System;
using System.Collections.Generic;
using MathAndScience.Numerics;
using WorldFoundry.Space;

namespace WorldFoundry.CelestialBodies.Planetoids.Planets.TerrestrialPlanets
{
    /// <summary>
    /// A terrestrial planet with little to no crust, whether due to a catastrophic collision event,
    /// or severe tidal forces due to a close orbit.
    /// </summary>
    public class LavaPlanet : TerrestrialPlanet
    {
        private protected override bool CanHaveWater => false;

        private protected override int MaxSatellites => 0;

        private protected override string PlanemoClassPrefix => "Lava";

        /// <summary>
        /// Initializes a new instance of <see cref="LavaPlanet"/>.
        /// </summary>
        internal LavaPlanet() { }

        /// <summary>
        /// Initializes a new instance of <see cref="LavaPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="LavaPlanet"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="LavaPlanet"/>.</param>
        internal LavaPlanet(CelestialRegion parent, Vector3 position) : base(parent, position) { }

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
        internal LavaPlanet(CelestialRegion parent, Vector3 position, double maxMass) : base(parent, position, maxMass) { }

        private protected override IEnumerable<(IComposition, double)> GetCrust()
        {
            // Molten crust with trace elements
            // Metal content varies by approx. +/-15% from standard value in a Gaussian distribution.
            var metals = Math.Round(Randomizer.Instance.Normal(MetalProportion, 0.05 * MetalProportion), 4);

            var nickel = Math.Round(Randomizer.Instance.NextDouble(0.025, 0.075) * metals, 4);
            var aluminum = Math.Round(Randomizer.Instance.NextDouble(0.075, 0.225) * metals, 4);

            var titanium = Math.Round(Randomizer.Instance.NextDouble(0.05, 0.3) * metals, 4);

            var iron = metals - nickel - aluminum - titanium;

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
                (Chemical.Gold, Phase.Solid, gold),
                (Chemical.Iron, Phase.Solid, iron),
                (Chemical.Lead, Phase.Solid, lead),
                (Chemical.Nickel, Phase.Solid, nickel),
                (Chemical.Platinum, Phase.Solid, platinum),
                (Chemical.Rock, Phase.Liquid, rock),
                (Chemical.Silver, Phase.Solid, silver),
                (Chemical.Sulfur, Phase.Solid, sulfur),
                (Chemical.Tin, Phase.Solid, tin),
                (Chemical.Titanium, Phase.Solid, titanium),
                (Chemical.Uranium, Phase.Solid, uranium)),
                1);
        }

        private protected override double GetInternalTemperature() => Randomizer.Instance.NextDouble(974.15, 1574.15);
    }
}
