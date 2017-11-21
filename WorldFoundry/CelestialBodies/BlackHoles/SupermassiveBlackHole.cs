using System;
using System.Numerics;
using WorldFoundry.Space;
using WorldFoundry.Utilities;
using WorldFoundry.Utilities.MathUtil.Shapes;

namespace WorldFoundry.CelestialBodies.BlackHoles
{
    /// <summary>
    /// A massive gravitational sigularity, found at the center of large galaxies.
    /// </summary>
    public class SupermassiveBlackHole : BlackHole
    {
        /// <summary>
        /// The base name for this type of <see cref="CelestialEntity"/>.
        /// </summary>
        public new static string BaseTypeName => "Supermassive Black Hole";

        /// <summary>
        /// Initializes a new instance of <see cref="SupermassiveBlackHole"/>.
        /// </summary>
        public SupermassiveBlackHole() { }

        /// <summary>
        /// Initializes a new instance of <see cref="SupermassiveBlackHole"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="SupermassiveBlackHole"/> is located.
        /// </param>
        public SupermassiveBlackHole(CelestialObject parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="SupermassiveBlackHole"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="SupermassiveBlackHole"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="SupermassiveBlackHole"/>.</param>
        public SupermassiveBlackHole(CelestialObject parent, Vector3 position) : base(parent, position) { }

        /// <summary>
        /// Generates the <see cref="Mass"/> of this <see cref="Orbiter"/>.
        /// </summary>
        /// <remarks>
        /// ~10e5–10e10 solar masses
        /// </remarks>
        protected override void GenerateMass() => Mass = Randomizer.Static.NextDouble(2.0e35, 2.0e40);
    }
}
