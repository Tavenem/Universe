using MathAndScience.Shapes;
using Substances;
using System;
using System.Collections.Generic;
using System.Linq;
using MathAndScience.Numerics;
using WorldFoundry.CelestialBodies.Stars;
using WorldFoundry.Substances;

namespace WorldFoundry.Space
{
    /// <summary>
    /// A charged cloud of interstellar gas and dust.
    /// </summary>
    public class HIIRegion : Nebula
    {
        private const double ChildDensity = 6.0e-50;

        private static readonly List<ChildDefinition> _childDefinitions = new List<ChildDefinition>
        {
            new ChildDefinition(typeof(StarSystem), StarSystem.Space, ChildDensity * 0.9998, typeof(Star), SpectralClass.B, LuminosityClass.V),
            new ChildDefinition(typeof(StarSystem), StarSystem.Space, ChildDensity * 0.0002, typeof(Star), SpectralClass.O, LuminosityClass.V),
        };

        private const string _baseTypeName = "HII Region";
        /// <summary>
        /// The base name for this type of <see cref="CelestialEntity"/>.
        /// </summary>
        public override string BaseTypeName => _baseTypeName;

        /// <summary>
        /// The types of children found in this region.
        /// </summary>
        public override IEnumerable<ChildDefinition> ChildDefinitions
            => base.ChildDefinitions.Concat(_childDefinitions);

        /// <summary>
        /// Initializes a new instance of <see cref="HIIRegion"/>.
        /// </summary>
        public HIIRegion() { }

        /// <summary>
        /// Initializes a new instance of <see cref="HIIRegion"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="HIIRegion"/> is located.
        /// </param>
        public HIIRegion(CelestialRegion parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="HIIRegion"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="HIIRegion"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="HIIRegion"/>.</param>
        public HIIRegion(CelestialRegion parent, Vector3 position) : base(parent, position) { }

        /// <summary>
        /// Generates the <see cref="CelestialEntity.Substance"/> of this <see cref="CelestialEntity"/>.
        /// </summary>
        private protected override void GenerateSubstance()
        {
            Substance = new Substance
            {
                Composition = CosmicSubstances.StellarMaterial.GetDeepCopy(),
                Mass = Randomizer.Instance.NextDouble(1.99e33, 1.99e37), // ~10e3–10e7 solar masses
                Temperature = 10000,
            };

            // Actual nebulae are irregularly shaped; this is presumed to be a containing shape within
            // which the dust clouds and filaments roughly fit. The radius follows a log-normal
            // distribution, with  ~20 ly as the mode, starting at ~10 ly, and cutting off around ~600 ly.
            var axis = 0.0;
            do
            {
                axis = Math.Round(1.0e17 + (Randomizer.Instance.Lognormal(0, 1) * 1.0e17));
            } while (axis > Space);
            Shape = new Ellipsoid(
                axis,
                Math.Round(axis * Randomizer.Instance.NextDouble(0.5, 1.5)),
                Math.Round(axis * Randomizer.Instance.NextDouble(0.5, 1.5)));
        }
    }
}
