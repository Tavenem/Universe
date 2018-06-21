using Substances;
using System;
using System.Linq;

namespace WorldFoundry.Substances
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
        public static float GetCloudCover(this IComposition composition)
        {
            if (composition is LayeredComposite lc)
            {
                float clouds = 0;
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

        /// <summary>
        /// Determines the greenhouse potential of all components in this <see cref="IComposition"/>.
        /// </summary>
        public static float GetGreenhousePotential(this IComposition composition)
        {
            if (composition is LayeredComposite lc)
            {
                float value = 0;
                foreach (var (substance, proportion) in lc.Layers)
                {
                    value += substance.GetGreenhousePotential();
                }
                return value;
            }
            else if (composition is Composite c)
            {
                return c.Components
                    .Where(x => x.Key.chemical.GreenhousePotential > 0)
                    .Sum(x => x.Key.chemical.GreenhousePotential * x.Value);
            }
            else if (composition is Material m)
            {
                return m.Chemical.GreenhousePotential;
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// Gets the surface layer of this <see cref="IComposition"/> (itself, if it is not layered).
        /// </summary>
        public static IComposition GetSurface(this IComposition composition)
        {
            if (composition is LayeredComposite lc)
            {
                if (lc.Layers.Count > 0)
                {
                    return lc.Layers[0].substance.GetSurface();
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return composition;
            }
        }
    }
}
