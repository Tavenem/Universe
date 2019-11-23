using NeverFoundry.MathAndScience;
using NeverFoundry.MathAndScience.Chemistry;
using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.MathAndScience.Randomization;
using System;
using System.Collections.Generic;

namespace NeverFoundry.WorldFoundry.CelestialBodies.Planetoids.Planets.GiantPlanets
{
    /// <summary>
    /// An ice giant planet, such as Neptune or Uranus.
    /// </summary>
    [Serializable]
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
        /// <param name="parentId">The id of the location which contains this one.</param>
        /// <param name="position">The initial position of this <see cref="IceGiant"/>.</param>
        internal IceGiant(string? parentId, Vector3 position) : base(parentId, position) { }

        /// <summary>
        /// Initializes a new instance of <see cref="IceGiant"/> with the given parameters.
        /// </summary>
        /// <param name="parentId">The id of the location which contains this one.</param>
        /// <param name="position">The initial position of this <see cref="IceGiant"/>.</param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="IceGiant"/> during random generation, in kg.
        /// </param>
        internal IceGiant(string? parentId, Vector3 position, Number maxMass) : base(parentId, position, maxMass) { }

        // No "puffy" ice giants.
        private protected override double GetDensity() => Randomizer.Instance.NextDouble(MinDensity, MaxDensity);

        private protected override IEnumerable<(IMaterial, decimal)> GetMantle(
            IShape planetShape,
            Number mantleProportion,
            Number crustProportion,
            Number planetMass,
            IShape coreShape,
            double coreTemp)
        {
            var mantleBoundaryDepth = planetShape.ContainingRadius * crustProportion;
            var mantleBoundaryTemp = (double)(mantleBoundaryDepth * new Number(115, -2));

            var innerTemp = coreTemp;

            var innerBoundary = planetShape.ContainingRadius;
            var mantleTotalDepth = (innerBoundary * mantleProportion) - coreShape.ContainingRadius;

            var mantleMass = planetMass * mantleProportion;

            var diamond = 1m;
            var water = Math.Max(0, Randomizer.Instance.NextDecimal(diamond));
            diamond -= water;
            var nh4 = Math.Max(0, Randomizer.Instance.NextDecimal(diamond));
            diamond -= nh4;
            var ch4 = Math.Max(0, Randomizer.Instance.NextDecimal(diamond));
            diamond -= ch4;

            // Liquid diamond mantle
            if (diamond > 0)
            {
                var diamondMass = mantleMass * (Number)diamond;

                var diamondBoundary = innerBoundary + (mantleTotalDepth * mantleProportion);
                var diamondShape = new HollowSphere(
                    innerBoundary,
                    diamondBoundary,
                    planetShape.Position);
                innerBoundary = diamondBoundary;

                var diamondBoundaryTemp = innerTemp.Lerp(mantleBoundaryTemp, (double)diamond);
                var diamondTemp = (diamondBoundaryTemp + innerTemp) / 2;
                innerTemp = diamondTemp;

                yield return (new Material(
                    Substances.All.Diamond.GetChemicalReference(),
                    (double)(diamondMass / diamondShape.Volume),
                    diamondMass,
                    diamondShape,
                    diamondTemp),
                    diamond);
            }

            // Supercritical water-ammonia ocean layer (blends seamlessly with lower atmosphere)
            var upperLayerProportion = 1 - diamond;

            var upperLayerMass = mantleMass * (Number)upperLayerProportion;

            var upperLayerBoundary = planetShape.ContainingRadius + mantleBoundaryDepth;
            var upperLayerShape = new HollowSphere(
                innerBoundary,
                upperLayerBoundary,
                planetShape.Position);

            var upperLayerTemp = (mantleBoundaryTemp + innerTemp) / 2;

            var components = new List<(ISubstanceReference, decimal)>();
            if (ch4 > 0 || nh4 > 0)
            {
                components.Add((Substances.All.Water.GetChemicalReference(), water));
                if (ch4 > 0)
                {
                    components.Add((Substances.All.Methane.GetChemicalReference(), ch4));
                }
                if (nh4 > 0)
                {
                    components.Add((Substances.All.Ammonia.GetChemicalReference(), nh4));
                }
            }
            else
            {
                components.Add((Substances.All.Water.GetChemicalReference(), 1));
            }

            yield return (new Material(
                (double)(upperLayerMass / upperLayerShape.Volume),
                upperLayerMass,
                upperLayerShape,
                upperLayerTemp,
                components.ToArray()),
                upperLayerProportion);
        }
    }
}
