using MathAndScience.MathUtil.Shapes;
using Substances;
using System;
using System.Numerics;
using WorldFoundry.Space;
using WorldFoundry.Substances;

namespace WorldFoundry.CelestialBodies.BlackHoles
{
    /// <summary>
    /// A gravitational singularity.
    /// </summary>
    public class BlackHole : CelestialBody
    {
        internal new static string baseTypeName = "Black Hole";
        /// <summary>
        /// The base name for this type of <see cref="CelestialEntity"/>.
        /// </summary>
        public override string BaseTypeName => baseTypeName;

        /// <summary>
        /// Initializes a new instance of <see cref="BlackHole"/>.
        /// </summary>
        public BlackHole() : base()  { }

        /// <summary>
        /// Initializes a new instance of <see cref="BlackHole"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="BlackHole"/> is located.
        /// </param>
        public BlackHole(CelestialRegion parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="BlackHole"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="BlackHole"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="BlackHole"/>.</param>
        public BlackHole(CelestialRegion parent, Vector3 position) : base(parent, position) { }

        /// <summary>
        /// Generates the mass of this <see cref="BlackHole"/>.
        /// </summary>
        /// <remarks>
        /// ~3–20 solar masses
        /// </remarks>
        private protected virtual double GenerateMass() => Randomizer.Static.NextDouble(6.0e30, 4.0e31);

        /// <summary>
        /// Generates the <see cref="CelestialEntity.Substance"/> of this <see cref="CelestialEntity"/>.
        /// </summary>
        /// <remarks>
        /// Black holes are strange objects with zero volume and infinite density. The shape given is
        /// presumed to refer to the shape of the event horizon.
        /// </remarks>
        private protected override void GenerateSubstance()
        {
            Substance = new Singularity
            {
                Composition = new Material(CosmicSubstances.Fuzzball, Phase.Plasma),
                Mass = GenerateMass(),
            };
            SetShape(new Sphere(Math.Round(1.48e-27 * Substance.Mass)));
        }
    }
}
