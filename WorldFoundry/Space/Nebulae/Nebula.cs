using MathAndScience.Shapes;
using Substances;
using MathAndScience.Numerics;
using WorldFoundry.Substances;

namespace WorldFoundry.Space
{
    /// <summary>
    /// A cloud of interstellar gas and dust.
    /// </summary>
    public class Nebula : CelestialRegion
    {
        internal const double Space = 5.5e18;

        private protected override string BaseTypeName => "Nebula";

        /// <summary>
        /// Initializes a new instance of <see cref="Nebula"/>.
        /// </summary>
        internal Nebula() { }

        /// <summary>
        /// Initializes a new instance of <see cref="Nebula"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="Nebula"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="Nebula"/>.</param>
        internal Nebula(CelestialRegion parent, Vector3 position) : base(parent, position) { }

        private protected override double GetMass() => Randomizer.Instance.NextDouble(1.99e33, 1.99e37); // ~10e3–10e7 solar masses

        // Actual nebulae are irregularly shaped; this is presumed to be a containing shape within
        // which the dust clouds and filaments roughly fit. The radius follows a log-normal
        // distribution, with ~32 ly as the mode, starting at ~16 ly, and cutting off around ~600
        // ly.
        private protected override IShape GetShape()
        {
            var axis = 0.0;
            do
            {
                axis = 1.5e17 + (Randomizer.Instance.Lognormal(0, 1) * 1.5e17);
            } while (axis > Space);
            return new Ellipsoid(
                axis,
                axis * Randomizer.Instance.NextDouble(0.5, 1.5),
                axis * Randomizer.Instance.NextDouble(0.5, 1.5),
                Position);
        }
    }
}
