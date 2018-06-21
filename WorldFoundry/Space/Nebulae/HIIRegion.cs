using MathAndScience.MathUtil.Shapes;
using Substances;
using System;
using System.Collections.Generic;
using System.Numerics;
using WorldFoundry.CelestialBodies.Stars;
using WorldFoundry.Substances;

namespace WorldFoundry.Space
{
    /// <summary>
    /// A charged cloud of interstellar gas and dust.
    /// </summary>
    public class HIIRegion : Nebula
    {
        internal new static string baseTypeName = "HII Region";
        /// <summary>
        /// The base name for this type of <see cref="CelestialEntity"/>.
        /// </summary>
        public override string BaseTypeName => baseTypeName;

        public static double childDensity = 6.0e-50;
        /// <summary>
        /// The average number of children within the grid per m³.
        /// </summary>
        public override double ChildDensity => childDensity;

        internal static IList<(Type type,float proportion, object[] constructorParameters)> childPossibilities =
            new List<(Type type,float proportion, object[] constructorParameters)>
            {
                (typeof(StarSystem), 0.9998f, new object[]{ typeof(Star), SpectralClass.B, LuminosityClass.V }),
                (typeof(StarSystem), 0.0002f, new object[]{ typeof(Star), SpectralClass.O, LuminosityClass.V }),
            };
        /// <summary>
        /// The types of children this region of space might have.
        /// </summary>
        public override IList<(Type type,float proportion, object[] constructorParameters)> ChildPossibilities => childPossibilities;

        /// <summary>
        /// Initializes a new instance of <see cref="HIIRegion"/>.
        /// </summary>
        public HIIRegion() : base() { }

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
                Mass = Randomizer.Static.NextDouble(1.99e33, 1.99e37), // ~10e3–10e7 solar masses
                Temperature = 10000,
            };

            // Actual nebulae are irregularly shaped; this is presumed to be a containing shape within
            // which the dust clouds and filaments roughly fit. The radius follows a log-normal
            // distribution, with  ~20 ly as the mode, starting at ~10 ly, and cutting off around ~600 ly.
            var axis = 0.0;
            do
            {
                axis = Math.Round(1.0e17 + Randomizer.Static.Lognormal(0, 1) * 1.0e17);
            } while (axis > 5.5e18);
            SetShape(new Ellipsoid(
                axis,
                Math.Round(axis * Randomizer.Static.NextDouble(0.5, 1.5)),
                Math.Round(axis * Randomizer.Static.NextDouble(0.5, 1.5))));
        }
    }
}
