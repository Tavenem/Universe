using MathAndScience.MathUtil.Shapes;
using Substances;
using System;
using System.Collections.Generic;
using System.Numerics;
using WorldFoundry.Orbits;
using WorldFoundry.Space.Galaxies;
using WorldFoundry.Substances;

namespace WorldFoundry.Space
{
    /// <summary>
    /// A collection of dwarf galaxies and globular clusters orbiting a large main galaxy.
    /// </summary>
    public class GalaxySubgroup : CelestialRegion
    {
        private const string _baseTypeName = "Galaxy Subgroup";
        /// <summary>
        /// The base name for this type of <see cref="CelestialEntity"/>.
        /// </summary>
        public override string BaseTypeName => _baseTypeName;

        private const double _childDensity = 1.0e-70;
        /// <summary>
        /// The average number of children within the grid per m³.
        /// </summary>
        public override double ChildDensity => _childDensity;

        internal static IList<(Type type,double proportion, object[] constructorParameters)> _childPossibilities =
            new List<(Type type,double proportion, object[] constructorParameters)>
            {
                (typeof(DwarfGalaxy), 0.26, null),
                (typeof(GlobularCluster), 0.74, null),
            };
        /// <summary>
        /// The types of children this region of space might have.
        /// </summary>
        public override IList<(Type type,double proportion, object[] constructorParameters)> ChildPossibilities => _childPossibilities;

        private Galaxy _mainGalaxy;
        /// <summary>
        /// The main <see cref="Galaxy"/> around which the other objects in this <see
        /// cref="GalaxySubgroup"/> orbit.
        /// </summary>
        public Galaxy MainGalaxy
        {
            get => GetProperty(ref _mainGalaxy, GenerateMainGalaxy);
            private set => _mainGalaxy = value;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="GalaxySubgroup"/>.
        /// </summary>
        public GalaxySubgroup() { }

        /// <summary>
        /// Initializes a new instance of <see cref="GalaxySubgroup"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="GalaxySubgroup"/> is located.
        /// </param>
        public GalaxySubgroup(CelestialRegion parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="GalaxySubgroup"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="GalaxySubgroup"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="GalaxySubgroup"/>.</param>
        public GalaxySubgroup(CelestialRegion parent, Vector3 position) : base(parent, position) { }

        /// <summary>
        /// Generates a child of the specified type within this <see cref="CelestialRegion"/>.
        /// </summary>
        /// <param name="type">
        /// The type of child to generate. Does not need to be one of this object's usual child
        /// types, but must be a subclass of <see cref="Orbiter"/>.
        /// </param>
        /// <param name="position">
        /// The location at which to generate the child. If null, a randomly-selected free space will
        /// be selected.
        /// </param>
        /// <param name="constructorParameters">
        /// An optional list of parameters with which to call the child's constructor. May be null.
        /// </param>
        public override Orbiter GenerateChildOfType(Type type, Vector3? position, object[] constructorParameters)
        {
            var child = base.GenerateChildOfType(type, position, constructorParameters);

            Orbit.SetOrbit(
                child,
                MainGalaxy,
                Math.Round(Randomizer.Static.NextDouble(0.1), 3));

            return child;
        }

        /// <summary>
        /// Randomly determines the main <see cref="Galaxy"/> of this <see cref="GalaxySubgroup"/>,
        /// which all other objects orbit.
        /// </summary>
        /// <remarks>70% of large galaxies are spirals.</remarks>
        private void GenerateMainGalaxy()
        {
            if (Randomizer.Static.NextDouble() <= 0.7)
            {
                MainGalaxy = new SpiralGalaxy(this);
            }
            else
            {
                MainGalaxy = new EllipticalGalaxy(this);
            }
        }

        /// <summary>
        /// Generates the <see cref="CelestialEntity.Substance"/> of this <see cref="CelestialEntity"/>.
        /// </summary>
        private protected override void GenerateSubstance()
        {
            Substance = new Substance
            {
                Composition = CosmicSubstances.IntraclusterMedium.GetDeepCopy(),
                Mass = MainGalaxy.Mass * 1.25, // the main galaxy is expected to comprise the bulk of the mass
            };
            SetShape(new Sphere(MainGalaxy.Radius * 10));
        }
    }
}
