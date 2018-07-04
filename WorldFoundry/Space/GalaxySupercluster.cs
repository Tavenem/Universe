using MathAndScience.MathUtil.Shapes;
using Substances;
using System;
using System.Collections.Generic;
using System.Numerics;
using WorldFoundry.Substances;

namespace WorldFoundry.Space
{
    /// <summary>
    /// The largest structure in the universe: a massive collection of galaxy groups and clusters.
    /// </summary>
    public class GalaxySupercluster : CelestialRegion
    {
        private const string baseTypeName = "Galaxy Supercluster";
        /// <summary>
        /// The base name for this type of <see cref="CelestialEntity"/>.
        /// </summary>
        public override string BaseTypeName => baseTypeName;

        private const double childDensity = 1.0e-73;
        /// <summary>
        /// The average number of children within the grid per m³.
        /// </summary>
        public override double ChildDensity => childDensity;

        internal static IList<(Type type, float proportion, object[] constructorParameters)> childPossibilities =
            new List<(Type type, float proportion, object[] constructorParameters)>
            {
                (typeof(GalaxyCluster), 1.0f / 3.0f, null),
                (typeof(GalaxyGroup), 2.0f / 3.0f, null),
            };
        /// <summary>
        /// The types of children this region of space might have.
        /// </summary>
        public override IList<(Type type, float proportion, object[] constructorParameters)> ChildPossibilities => childPossibilities;

        /// <summary>
        /// Initializes a new instance of <see cref="GalaxySupercluster"/>.
        /// </summary>
        public GalaxySupercluster() : base() { }

        /// <summary>
        /// Initializes a new instance of <see cref="GalaxySupercluster"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="GalaxySupercluster"/> is located.
        /// </param>
        public GalaxySupercluster(CelestialRegion parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="GalaxySupercluster"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="GalaxySupercluster"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="GalaxySupercluster"/>.</param>
        public GalaxySupercluster(CelestialRegion parent, Vector3 position) : base(parent, position) { }

        /// <summary>
        /// Generates the <see cref="CelestialEntity.Substance"/> of this <see cref="CelestialEntity"/>.
        /// </summary>
        /// <remarks>
        /// May be filaments (narrow in two dimensions), or walls/sheets (narrow in one dimension).
        /// </remarks>
        private protected override void GenerateSubstance()
        {
            Substance = new Substance
            {
                Composition = CosmicSubstances.IntergalacticMedium.GetDeepCopy(),
                Mass = Randomizer.Static.NextDouble(2.0e46, 2.0e47), // General average; 1.0e16–1.0e17 solar masses
            };
            var majorAxis = Randomizer.Static.NextDouble(9.4607e23, 9.4607e25);
            var minorAxis1 = majorAxis * Randomizer.Static.NextDouble(0.02, 0.15);
            double minorAxis2;
            if (Randomizer.Static.NextBoolean()) // Filament
            {
                minorAxis2 = minorAxis1;
            }
            else // Wall/sheet
            {
                minorAxis2 = majorAxis * Randomizer.Static.NextDouble(0.3, 0.8);
            }
            var chance = Randomizer.Static.Next(6);
            if (chance == 0)
            {
                SetShape(new Ellipsoid(majorAxis, minorAxis1, minorAxis2));
            }
            else if (chance == 1)
            {
                SetShape(new Ellipsoid(majorAxis, minorAxis2, minorAxis1));
            }
            else if (chance == 2)
            {
                SetShape(new Ellipsoid(minorAxis1, majorAxis, minorAxis2));
            }
            else if (chance == 3)
            {
                SetShape(new Ellipsoid(minorAxis2, majorAxis, minorAxis1));
            }
            else if (chance == 4)
            {
                SetShape(new Ellipsoid(minorAxis1, minorAxis2, majorAxis));
            }
            else
            {
                SetShape(new Ellipsoid(minorAxis2, minorAxis1, majorAxis));
            }
        }
    }
}
