﻿using MathAndScience.Shapes;
using Substances;
using System.Collections.Generic;
using System.Linq;
using MathAndScience.Numerics;
using WorldFoundry.Substances;

namespace WorldFoundry.Space
{
    /// <summary>
    /// The largest structure in the universe: a massive collection of galaxy groups and clusters.
    /// </summary>
    public class GalaxySupercluster : CelestialRegion
    {
        /// <summary>
        /// The radius of the maximum space required by this type of <see cref="CelestialEntity"/>,
        /// in meters.
        /// </summary>
        public const double Space = 9.4607e25;

        private const double ChildDensity = 1.0e-73;

        private static readonly List<ChildDefinition> _childDefinitions = new List<ChildDefinition>
        {
            new ChildDefinition(typeof(GalaxyCluster), GalaxyCluster.Space, ChildDensity / 3),
            new ChildDefinition(typeof(GalaxyGroup), GalaxyGroup.Space, ChildDensity * 2 / 3),
        };

        private const string _baseTypeName = "Galaxy Supercluster";
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
        /// Initializes a new instance of <see cref="GalaxySupercluster"/>.
        /// </summary>
        public GalaxySupercluster() { }

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
                Mass = Randomizer.Instance.NextDouble(2.0e46, 2.0e47), // General average; 1.0e16–1.0e17 solar masses
            };
            var majorAxis = Randomizer.Instance.NextDouble(9.4607e23, 9.4607e25);
            var minorAxis1 = majorAxis * Randomizer.Instance.NextDouble(0.02, 0.15);
            double minorAxis2;
            if (Randomizer.Instance.NextBoolean()) // Filament
            {
                minorAxis2 = minorAxis1;
            }
            else // Wall/sheet
            {
                minorAxis2 = majorAxis * Randomizer.Instance.NextDouble(0.3, 0.8);
            }
            var chance = Randomizer.Instance.Next(6);
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
