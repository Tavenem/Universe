using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Numerics;
using WorldFoundry.Utilities;
using WorldFoundry.Utilities.MathUtil.Shapes;

namespace WorldFoundry.Space
{
    /// <summary>
    /// A large structure of gravitationally-bound galaxies.
    /// </summary>
    public class GalaxyCluster : CelestialObject
    {
        internal new static string baseTypeName = "Galaxy Cluster";
        /// <summary>
        /// The base name for this type of <see cref="CelestialEntity"/>.
        /// </summary>
        public override string BaseTypeName => baseTypeName;

        public static double childDensity = 1.8e-70;
        /// <summary>
        /// The average number of children within the grid per m³.
        /// </summary>
        public override double ChildDensity => childDensity;

        internal static IDictionary<Type, (float proportion, object[] constructorParameters)> childPossibilities =
            new Dictionary<Type, (float proportion, object[] constructorParameters)>
            {
                { typeof(GalaxyGroup), (1, null) },
            };
        /// <summary>
        /// The types of children this region of space might have.
        /// </summary>
        [NotMapped]
        public override IDictionary<Type, (float proportion, object[] constructorParameters)> ChildPossibilities => childPossibilities;

        /// <summary>
        /// Initializes a new instance of <see cref="GalaxyCluster"/>.
        /// </summary>
        public GalaxyCluster() { }

        /// <summary>
        /// Initializes a new instance of <see cref="GalaxyCluster"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="GalaxyCluster"/> is located.
        /// </param>
        public GalaxyCluster(CelestialObject parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="GalaxyCluster"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="GalaxyCluster"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="GalaxyCluster"/>.</param>
        public GalaxyCluster(CelestialObject parent, Vector3 position) : base(parent, position) { }

        /// <summary>
        /// Generates the <see cref="Mass"/> of this <see cref="Orbiter"/>.
        /// </summary>
        /// <remarks>
        /// General average; 1.0e15–1.0e16 solar masses.
        /// </remarks>
        private protected override void GenerateMass() => Mass = Randomizer.Static.NextDouble(2.0e45, 2.0e46);

        /// <summary>
        /// Generates the <see cref="Shape"/> of this <see cref="CelestialEntity"/>.
        /// </summary>
        /// <remarks>
        /// ~1–5 Mpc
        /// </remarks>
        private protected override void GenerateShape() => Shape = new Sphere(Randomizer.Static.NextDouble(3.0e23, 1.5e24));
    }
}
