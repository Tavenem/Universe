using Substances;
using System;
using System.Collections.Generic;
using MathAndScience.Numerics;
using WorldFoundry.Space;
using MathAndScience.Shapes;

namespace WorldFoundry.CelestialBodies.Planetoids.Planets.TerrestrialPlanets
{
    /// <summary>
    /// A terrestrial planet consisting of an unusually high proportion of water, with a mantle
    /// consisting of a form of high-pressure, hot ice, and possibly a supercritical
    /// surface-atmosphere blend.
    /// </summary>
    public class OceanPlanet : TerrestrialPlanet
    {
        private const bool _hasFlatSurface = true;
        /// <summary>
        /// Indicates that this <see cref="Planetoid"/>'s surface does not have elevation variations
        /// (i.e. is non-solid). Prevents generation of a height map during <see
        /// cref="Planetoid.Terrain"/> generation.
        /// </summary>
        public override bool HasFlatSurface => _hasFlatSurface;

        /// <summary>
        /// A factor which multiplies the chance of this <see cref="Planetoid"/> having a strong magnetosphere.
        /// </summary>
        /// <remarks>
        /// The cores of ocean planets are liable to cool more rapidly than other planets of similar
        /// size, reducing the chances of producing the required dynamo effect.
        /// </remarks>
        public override double MagnetosphereChanceFactor => 0.5;

        private const string _planemoClassPrefix = "Ocean";
        /// <summary>
        /// A prefix to the <see cref="CelestialEntity.TypeName"/> for this class of <see cref="Planemo"/>.
        /// </summary>
        public override string PlanemoClassPrefix => _planemoClassPrefix;

        /// <summary>
        /// Initializes a new instance of <see cref="OceanPlanet"/>.
        /// </summary>
        public OceanPlanet() { }

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
        /// Generates an appropriate hydrosphere for this <see cref="TerrestrialPlanet"/>.
        /// </summary>
        /// <remarks>
        /// Ocean planets have a thick hydrosphere layer generated as part of the <see cref="CelestialEntity.Substance"/>.
        /// </remarks>
        private protected override void GenerateHydrosphere() => GenerateSubstance();

        private protected override IEnumerable<(IComposition, double)> GetCrust()
        {
            yield break;
        }

        private protected override double GetCrustProportion(IShape shape) => 0;

        private protected override IEnumerable<(IComposition, double)> GetMantle(IShape shape, double proportion)
        {
            var crustProportion = base.GetCrustProportion(shape);

            var mantleProportion = 1 - crustProportion;

            // Hydrosphere makes up the bulk of the planet, and is generated here based on composition.
            HydrosphereProportion = mantleProportion;
            // Surface water is mostly salt water.
            var saltWater = Math.Round(Randomizer.Instance.Normal(0.945, 0.015), 3);
            _hydrosphere = new Composite(
                (Chemical.Water, Phase.Liquid, 1 - saltWater),
                (Chemical.Water_Salt, Phase.Liquid, saltWater));

            // Thin magma mantle
            var magmaMantle = mantleProportion * 0.2;
            yield return (new Material(Chemical.Rock, Phase.Liquid), magmaMantle);

            // Rocky crust with trace elements
            // Metal content varies by approx. +/-15% from standard value in a Gaussian distribution.
            var metals = Math.Round(Randomizer.Instance.Normal(MetalProportion, 0.05 * MetalProportion), 4);

            var nickel = Math.Round(Randomizer.Instance.NextDouble(0.025, 0.075) * metals, 4);
            var aluminum = Math.Round(Randomizer.Instance.NextDouble(0.075, 0.225) * metals, 4);

            var titanium = Math.Round(Randomizer.Instance.NextDouble(0.05, 0.3) * metals, 4);

            var iron = metals - nickel - aluminum - titanium;

            var copper = Math.Round(Randomizer.Instance.NextDouble(titanium), 4);
            titanium -= copper;

            var lead = titanium > 0 ? Math.Round(Randomizer.Instance.NextDouble(titanium), 4) : 0;
            titanium -= lead;

            var uranium = titanium > 0 ? Math.Round(Randomizer.Instance.NextDouble(titanium), 4) : 0;
            titanium -= uranium;

            var tin = titanium > 0 ? Math.Round(Randomizer.Instance.NextDouble(titanium), 4) : 0;
            titanium -= tin;

            var silver = Math.Round(Randomizer.Instance.NextDouble(titanium), 4);
            titanium -= silver;

            var gold = Math.Round(Randomizer.Instance.NextDouble(titanium), 4);
            titanium -= gold;

            var platinum = Math.Round(Randomizer.Instance.NextDouble(titanium), 4);
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
                (Chemical.Rock, Phase.Solid, rock),
                (Chemical.Silver, Phase.Solid, silver),
                (Chemical.Sulfur, Phase.Solid, sulfur),
                (Chemical.Tin, Phase.Solid, tin),
                (Chemical.Titanium, Phase.Solid, titanium),
                (Chemical.Uranium, Phase.Solid, uranium)),
                crustProportion);

            // Ice mantle
            yield return (new Material(Chemical.Water, Phase.Solid), mantleProportion - magmaMantle);
        }
    }
}
