using System;
using System.Numerics;
using WorldFoundry.Space;
using WorldFoundry.Utilities;
using WorldFoundry.Utilities.MathUtil.Shapes;

namespace WorldFoundry.CelestialBodies.BlackHoles
{
    /// <summary>
    /// A gravitational singularity.
    /// </summary>
    public class BlackHole : CelestialBody
    {
        internal new const string baseTypeName = "Black Hole";
        /// <summary>
        /// The base name for this type of <see cref="CelestialEntity"/>.
        /// </summary>
        public override string BaseTypeName => baseTypeName;

        /// <summary>
        /// Initializes a new instance of <see cref="BlackHole"/>.
        /// </summary>
        public BlackHole() { }

        /// <summary>
        /// Initializes a new instance of <see cref="BlackHole"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="BlackHole"/> is located.
        /// </param>
        public BlackHole(CelestialObject parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="BlackHole"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="BlackHole"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="BlackHole"/>.</param>
        public BlackHole(CelestialObject parent, Vector3 position) : base(parent, position) { }

        /// <summary>
        /// Generates the <see cref="Mass"/> of this <see cref="Orbiter"/>.
        /// </summary>
        /// <remarks>
        /// ~3–20 solar masses
        /// </remarks>
        protected override void GenerateMass() => Mass = Randomizer.Static.NextDouble(6.0e30, 4.0e31);

        /// <summary>
        /// Generates the <see cref="Utilities.MathUtil.Shapes.Shape"/> of this <see cref="CelestialEntity"/>.
        /// </summary>
        /// <remarks>
        /// Black holes are strange objects with zero volume and infinite density. The shape given is
        /// presumed to refer to the shape of the event horizon.
        /// </remarks>
        protected override void GenerateShape() => Shape = new Sphere(Math.Round(1.48e-27 * Mass));

        /// <summary>
        /// Calculates the average surface gravity of this <see cref="Orbiter"/>, in N.
        /// </summary>
        /// <returns>The average surface gravity of this <see cref="Orbiter"/>, in N.</returns>
        /// <remarks>
        /// The gravity at the event horizon of a black hole is infinite.
        /// </remarks>
        protected override void GenerateSurfaceGravity() => SurfaceGravity = double.PositiveInfinity;
    }
}
