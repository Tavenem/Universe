using Substances;

namespace WorldFoundry.Substances
{
    public class Singularity : Substance
    {
        /// <summary>
        /// Calculates the average force of gravity at the surface of this object, in N.
        /// </summary>
        /// <remarks>
        /// Always infinity for a <see cref="Singularity"/>.
        /// </remarks>
        public override double GetSurfaceGravity() => double.PositiveInfinity;
    }
}
