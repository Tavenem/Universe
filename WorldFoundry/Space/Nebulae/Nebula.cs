using System;
using System.Numerics;
using WorldFoundry.Utilities;
using WorldFoundry.Utilities.MathUtil.Shapes;

namespace WorldFoundry.Space
{
    /// <summary>
    /// A cloud of interstellar gas and dust.
    /// </summary>
    public class Nebula : CelestialObject
    {
        internal new const string baseTypeName = "Nebula";
        /// <summary>
        /// The base name for this type of <see cref="CelestialEntity"/>.
        /// </summary>
        public override string BaseTypeName => baseTypeName;

        /// <summary>
        /// Initializes a new instance of <see cref="Nebula"/>.
        /// </summary>
        public Nebula() { }

        /// <summary>
        /// Initializes a new instance of <see cref="Nebula"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="Nebula"/> is located.
        /// </param>
        public Nebula(CelestialObject parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="Nebula"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="Nebula"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="Nebula"/>.</param>
        public Nebula(CelestialObject parent, Vector3 position) : base(parent, position) { }

        /// <summary>
        /// Generates the <see cref="Mass"/> of this <see cref="Orbiter"/>.
        /// </summary>
        /// <remarks>
        /// ~10e3–10e7 solar masses.
        /// </remarks>
        protected override void GenerateMass() => Mass = Randomizer.Static.NextDouble(1.99e33, 1.99e37);

        /// <summary>
        /// Generates the <see cref="Shape"/> of this <see cref="CelestialEntity"/>.
        /// </summary>
        /// <remarks>
        /// Actual nebulae are irregularly shaped; this is presumed to be a containing shape within
        /// which the dust clouds and filaments roughly fit. The radius follows a log-normal
        /// distribution, with ~32 ly as the mode, starting at ~16 ly, and cutting off around ~600 ly.
        /// </remarks>
        protected override void GenerateShape()
        {
            var axis = 0.0;
            do
            {
                axis = Math.Round(1.5e17 + Randomizer.Static.Lognormal(0, 1.5e17));
            } while (axis > 5.5e18);
            Shape = new Ellipsoid(
                axis,
                Math.Round(axis * Randomizer.Static.NextDouble(0.5, 1.5)),
                Math.Round(axis * Randomizer.Static.NextDouble(0.5, 1.5)));
        }
    }
}
