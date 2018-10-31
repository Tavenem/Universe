using MathAndScience.Numerics;
using WorldFoundry.Space;

namespace WorldFoundry.CelestialBodies.BlackHoles
{
    /// <summary>
    /// A massive gravitational singularity, found at the center of large galaxies.
    /// </summary>
    public class SupermassiveBlackHole : BlackHole
    {
        private protected override string BaseTypeName => "Supermassive Black Hole";

        /// <summary>
        /// Initializes a new instance of <see cref="SupermassiveBlackHole"/>.
        /// </summary>
        internal SupermassiveBlackHole() { }

        /// <summary>
        /// Initializes a new instance of <see cref="SupermassiveBlackHole"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="SupermassiveBlackHole"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="SupermassiveBlackHole"/>.</param>
        internal SupermassiveBlackHole(CelestialRegion parent, Vector3 position) : base(parent, position) { }

        private protected override double GenerateMass() => Randomizer.Instance.NextDouble(2.0e35, 2.0e40); // ~10e5–10e10 solar masses
    }
}
