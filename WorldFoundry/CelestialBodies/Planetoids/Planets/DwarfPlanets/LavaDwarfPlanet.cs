using Substances;
using System;
using MathAndScience.Numerics;
using WorldFoundry.Space;
using System.Collections.Generic;
using MathAndScience.Shapes;

namespace WorldFoundry.CelestialBodies.Planetoids.Planets.DwarfPlanets
{
    /// <summary>
    /// A hot, rocky dwarf planet with a molten rock mantle; usually the result of tidal stress.
    /// </summary>
    public class LavaDwarfPlanet : DwarfPlanet
    {
        private protected override double DensityForType => 4000;

        private protected override int MaxSatellites => 0;

        private protected override string PlanemoClassPrefix => "Lava";

        /// <summary>
        /// Initializes a new instance of <see cref="LavaDwarfPlanet"/>.
        /// </summary>
        internal LavaDwarfPlanet() { }

        /// <summary>
        /// Initializes a new instance of <see cref="LavaDwarfPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="LavaDwarfPlanet"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="LavaDwarfPlanet"/>.</param>
        internal LavaDwarfPlanet(CelestialRegion parent, Vector3 position) : base(parent, position) { }

        /// <summary>
        /// Initializes a new instance of <see cref="LavaDwarfPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="LavaDwarfPlanet"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="LavaDwarfPlanet"/>.</param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="LavaDwarfPlanet"/> during random generation, in kg.
        /// </param>
        internal LavaDwarfPlanet(CelestialRegion parent, Vector3 position, double maxMass) : base(parent, position, maxMass) { }

        private protected override IEnumerable<(IComposition, double)> GetCrust()
        {
            // rocky crust
            // 50% chance of dust
            var dust = Math.Round(Math.Max(0, Randomizer.Instance.NextDouble(-0.5, 0.5)), 3);
            if (dust > 0)
            {
                yield return (new Composite(
                    (Chemical.Rock, Phase.Solid, 1 - dust),
                    (Chemical.Dust, Phase.Solid, dust)),
                    1);
            }
            else
            {
                yield return (new Material(Chemical.Rock, Phase.Solid), 1);
            }
        }

        private protected override IEnumerable<(IComposition, double)> GetMantle(IShape shape, double proportion)
        {
            yield return (new Material(Chemical.Rock, Phase.Liquid), 1);
        }
    }
}
