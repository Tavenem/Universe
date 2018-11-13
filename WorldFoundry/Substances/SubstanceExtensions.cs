using Substances;
using System;
using System.Linq;

namespace WorldFoundry.CosmicSubstances
{
    /// <summary>
    /// A set of <see cref="WorldFoundry"/>-specific extensions to <see cref="Substances"/> classes.
    /// </summary>
    public static class SubstanceExtensions
    {
        /// <summary>
        /// Determines the proportion of liquids and solids in this <see cref="IComposition"/>. If
        /// layered, the highest proportion among all layers is given, since cloud cover is cumulative.
        /// </summary>
        /// <param name="composition">This <see cref="IComposition"/> instance.</param>
        public static double GetCloudCover(this IComposition composition)
        {
            if (composition is LayeredComposite lc)
            {
                double clouds = 0;
                foreach (var (substance, proportion) in lc.Layers)
                {
                    clouds = Math.Max(clouds, substance.GetChemicals(Phase.Liquid | Phase.Solid).Sum(x => x.proportion));
                }
                return clouds;
            }
            else
            {
                return composition.GetChemicals(Phase.Liquid | Phase.Solid).Sum(x => x.proportion);
            }
        }
    }
}
