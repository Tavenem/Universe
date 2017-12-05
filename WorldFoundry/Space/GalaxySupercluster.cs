using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Numerics;
using WorldFoundry.Utilities;
using WorldFoundry.Utilities.MathUtil.Shapes;

namespace WorldFoundry.Space
{
    /// <summary>
    /// The largest structure in the universe: a massive collection of galaxy groups and clusters.
    /// </summary>
    public class GalaxySupercluster : CelestialObject
    {
        internal new static string baseTypeName = "Galaxy Supercluster";
        /// <summary>
        /// The base name for this type of <see cref="CelestialEntity"/>.
        /// </summary>
        public override string BaseTypeName => baseTypeName;

        public static double childDensity = 1.0e-73;
        /// <summary>
        /// The average number of children within the grid per m³.
        /// </summary>
        public override double ChildDensity => childDensity;

        internal static IDictionary<Type, (float proportion, object[] constructorParameters)> childPossibilities =
            new Dictionary<Type, (float proportion, object[] constructorParameters)>
            {
                { typeof(GalaxyCluster), (1.0f / 3.0f, null) },
                { typeof(GalaxyGroup), (2.0f / 3.0f, null) },
            };
        /// <summary>
        /// The types of children this region of space might have.
        /// </summary>
        [NotMapped]
        public override IDictionary<Type, (float proportion, object[] constructorParameters)> ChildPossibilities => childPossibilities;

        /// <summary>
        /// Initializes a new instance of <see cref="GalaxySupercluster"/>.
        /// </summary>
        public GalaxySupercluster() { }

        /// <summary>
        /// Initializes a new instance of <see cref="GalaxySupercluster"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="GalaxySupercluster"/> is located.
        /// </param>
        public GalaxySupercluster(CelestialObject parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="GalaxySupercluster"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="GalaxySupercluster"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="GalaxySupercluster"/>.</param>
        public GalaxySupercluster(CelestialObject parent, Vector3 position) : base(parent, position) { }

        /// <summary>
        /// Generates the <see cref="Mass"/> of this <see cref="Orbiter"/>.
        /// </summary>
        /// <remarks>
        /// General average; 1.0e16–1.0e17 solar masses.
        /// </remarks>
        protected override void GenerateMass() => Mass = Randomizer.Static.NextDouble(2.0e46, 2.0e47);

        /// <summary>
        /// Generates the <see cref="Shape"/> of this <see cref="CelestialEntity"/>.
        /// </summary>
        /// <remarks>
        /// May be filaments (narrow in two dimensions), or walls/sheets (narrow in one dimension).
        /// </remarks>
        protected override void GenerateShape()
        {
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
                Shape = new Ellipsoid(majorAxis, minorAxis1, minorAxis2);
            }
            else if (chance == 1)
            {
                Shape = new Ellipsoid(majorAxis, minorAxis2, minorAxis1);
            }
            else if (chance == 2)
            {
                Shape = new Ellipsoid(minorAxis1, majorAxis, minorAxis2);
            }
            else if (chance == 3)
            {
                Shape = new Ellipsoid(minorAxis2, majorAxis, minorAxis1);
            }
            else if (chance == 4)
            {
                Shape = new Ellipsoid(minorAxis1, minorAxis2, majorAxis);
            }
            else
            {
                Shape = new Ellipsoid(minorAxis2, minorAxis1, majorAxis);
            }
        }
    }
}
