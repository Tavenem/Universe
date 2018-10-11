using MathAndScience.Shapes;
using Substances;
using System;
using System.Numerics;
using WorldFoundry.Substances;

namespace WorldFoundry.Space
{
    /// <summary>
    /// A cloud of interstellar gas and dust.
    /// </summary>
    public class Nebula : CelestialRegion
    {
        private const string _baseTypeName = "Nebula";
        /// <summary>
        /// The base name for this type of <see cref="CelestialEntity"/>.
        /// </summary>
        public override string BaseTypeName => _baseTypeName;

        /// <summary>
        /// Initializes a new instance of <see cref="Nebula"/>.
        /// </summary>
        public Nebula() { }

        /// <summary>
        /// Initializes a new instance of <see cref="Nebula"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="Nebula"/> is located.
        /// </param>
        public Nebula(CelestialRegion parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="Nebula"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="Nebula"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="Nebula"/>.</param>
        public Nebula(CelestialRegion parent, Vector3 position) : base(parent, position) { }

        /// <summary>
        /// Generates the <see cref="CelestialEntity.Substance"/> of this <see cref="CelestialEntity"/>.
        /// </summary>
        private protected override void GenerateSubstance()
        {
            Substance = new Substance
            {
                Composition = CosmicSubstances.StellarMaterial.GetDeepCopy(),
                Mass = Randomizer.Static.NextDouble(1.99e33, 1.99e37), // ~10e3–10e7 solar masses
            };

            // Actual nebulae are irregularly shaped; this is presumed to be a containing shape within
            // which the dust clouds and filaments roughly fit. The radius follows a log-normal
            // distribution, with ~32 ly as the mode, starting at ~16 ly, and cutting off around ~600 ly.
            var axis = 0.0;
            do
            {
                axis = Math.Round(1.5e17 + (Randomizer.Static.Lognormal(0, 1) * 1.5e17));
            } while (axis > 5.5e18);
            SetShape(new Ellipsoid(
                axis,
                Math.Round(axis * Randomizer.Static.NextDouble(0.5, 1.5)),
                Math.Round(axis * Randomizer.Static.NextDouble(0.5, 1.5))));
        }
    }
}
