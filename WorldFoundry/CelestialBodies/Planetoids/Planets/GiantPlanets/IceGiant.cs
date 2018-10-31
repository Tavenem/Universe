using Substances;
using System;
using System.Collections.Generic;
using MathAndScience.Numerics;
using WorldFoundry.Space;
using MathAndScience.Shapes;

namespace WorldFoundry.CelestialBodies.Planetoids.Planets.GiantPlanets
{
    /// <summary>
    /// An ice giant planet, such as Neptune or Uranus.
    /// </summary>
    public class IceGiant : GiantPlanet
    {
        private protected override string BaseTypeName => "Ice Giant";

        // Set to 40 for IceGiant. For reference, Uranus has 27 moons, and Neptune has
        // 14 moons.
        private protected override int MaxSatellites => 40;

        /// <summary>
        /// Initializes a new instance of <see cref="IceGiant"/>.
        /// </summary>
        internal IceGiant() { }

        /// <summary>
        /// Initializes a new instance of <see cref="IceGiant"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="IceGiant"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="IceGiant"/>.</param>
        internal IceGiant(CelestialRegion parent, Vector3 position) : base(parent, position) { }

        /// <summary>
        /// Initializes a new instance of <see cref="IceGiant"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="IceGiant"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="IceGiant"/>.</param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="IceGiant"/> during random generation, in kg.
        /// </param>
        internal IceGiant(CelestialRegion parent, Vector3 position, double maxMass) : base(parent, position, maxMass) { }

        // No "puffy" ice giants.
        private protected override double? GetDensity() => Math.Round(Randomizer.Instance.NextDouble(MinDensity, MaxDensity));

        private protected override IEnumerable<(IComposition, double)> GetMantle(IShape shape, double proportion)
        {
            var diamond = 1.0;
            var water = Math.Max(0, Randomizer.Instance.NextDouble(diamond));
            diamond -= water;
            var nh4 = Math.Max(0, Randomizer.Instance.NextDouble(diamond));
            diamond -= nh4;
            var ch4 = Math.Max(0, Randomizer.Instance.NextDouble(diamond));
            diamond -= ch4;

            // Liquid diamond mantle
            if (!Troschuetz.Random.TMath.IsZero(diamond))
            {
                yield return (new Material(Chemical.Diamond, Phase.Liquid), diamond);
            }

            // Supercritical water-ammonia ocean layer (blends seamlessly with lower atmosphere)
            IComposition upperLayer = null;
            if (ch4 > 0 || nh4 > 0)
            {
                upperLayer = new Composite((Chemical.Water, Phase.Liquid, water));
                var composite = upperLayer as Composite;
                if (ch4 > 0)
                {
                    composite.Components[(Chemical.Methane, Phase.Liquid)] = ch4;
                }
                if (nh4 > 0)
                {
                    composite.Components[(Chemical.Ammonia, Phase.Liquid)] = nh4;
                }
            }
            else
            {
                upperLayer = new Material(Chemical.Water, Phase.Liquid);
            }
            upperLayer.BalanceProportions();
            yield return (upperLayer, 1 - diamond);
        }
    }
}
