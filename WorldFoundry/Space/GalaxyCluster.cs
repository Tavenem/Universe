using MathAndScience.Shapes;
using Substances;
using System;
using System.Collections.Generic;
using System.Numerics;
using WorldFoundry.Substances;

namespace WorldFoundry.Space
{
    /// <summary>
    /// A large structure of gravitationally-bound galaxies.
    /// </summary>
    public class GalaxyCluster : CelestialRegion
    {
        private const string _baseTypeName = "Galaxy Cluster";
        /// <summary>
        /// The base name for this type of <see cref="CelestialEntity"/>.
        /// </summary>
        public override string BaseTypeName => _baseTypeName;

        private const double _childDensity = 1.8e-70;
        /// <summary>
        /// The average number of children within the grid per m³.
        /// </summary>
        public override double ChildDensity => _childDensity;

        internal static IList<(Type type,double proportion, object[] constructorParameters)> _childPossibilities =
            new List<(Type type,double proportion, object[] constructorParameters)>
            {
                (typeof(GalaxyGroup), 1, null),
            };
        /// <summary>
        /// The types of children this region of space might have.
        /// </summary>
        public override IList<(Type type,double proportion, object[] constructorParameters)> ChildPossibilities => _childPossibilities;

        /// <summary>
        /// Initializes a new instance of <see cref="GalaxyCluster"/>.
        /// </summary>
        public GalaxyCluster() { }

        /// <summary>
        /// Initializes a new instance of <see cref="GalaxyCluster"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="GalaxyCluster"/> is located.
        /// </param>
        public GalaxyCluster(CelestialRegion parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="GalaxyCluster"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="GalaxyCluster"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="GalaxyCluster"/>.</param>
        public GalaxyCluster(CelestialRegion parent, Vector3 position) : base(parent, position) { }

        /// <summary>
        /// Generates the <see cref="CelestialEntity.Substance"/> of this <see cref="CelestialEntity"/>.
        /// </summary>
        private protected override void GenerateSubstance()
        {
            Substance = new Substance
            {
                Composition = CosmicSubstances.IntraclusterMedium.GetDeepCopy(),
                Mass = Randomizer.Static.NextDouble(2.0e45, 2.0e46), // general average; 1.0e15–1.0e16 solar masses
            };
            SetShape(new Sphere(Randomizer.Static.NextDouble(3.0e23, 1.5e24))); // ~1–5 Mpc
        }
    }
}
