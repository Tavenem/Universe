using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Numerics;
using WorldFoundry.Space.Galaxies;
using WorldFoundry.Utilities;
using WorldFoundry.Utilities.MathUtil.Shapes;

namespace WorldFoundry.Space
{
    /// <summary>
    /// A collection of dwarf galaxies and globular clusters orbiting a large main galaxy.
    /// </summary>
    public class GalaxySubgroup : CelestialObject
    {
        internal new static string baseTypeName = "Galaxy Subgroup";
        /// <summary>
        /// The base name for this type of <see cref="CelestialEntity"/>.
        /// </summary>
        public override string BaseTypeName => baseTypeName;

        public static double childDensity = 1.0e-70;
        /// <summary>
        /// The average number of children within the grid per m³.
        /// </summary>
        public override double ChildDensity => childDensity;

        internal static IDictionary<Type, (float proportion, object[] constructorParameters)> childPossibilities =
            new Dictionary<Type, (float proportion, object[] constructorParameters)>
            {
                { typeof(DwarfGalaxy), (0.26f, null) },
                { typeof(GlobularCluster), (0.74f, null) },
            };
        /// <summary>
        /// The types of children this region of space might have.
        /// </summary>
        [NotMapped]
        public override IDictionary<Type, (float proportion, object[] constructorParameters)> ChildPossibilities => childPossibilities;

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
        /// The containing <see cref="CelestialObject"/> in which this <see cref="GalaxySubgroup"/> is located.
        /// </param>
        public GalaxySubgroup(CelestialObject parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="GalaxySubgroup"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="GalaxySubgroup"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="GalaxySubgroup"/>.</param>
        public GalaxySubgroup(CelestialObject parent, Vector3 position) : base(parent, position) { }

        /// <summary>
        /// Generates a child of the specified type within this <see cref="CelestialObject"/>.
        /// </summary>
        /// <param name="type">
        /// The type of child to generate. Does not need to be one of this object's usual child
        /// types, but must be a subclass of <see cref="CelestialObject"/> or <see cref="CelestialBody"/>.
        /// </param>
        /// <param name="position">
        /// The location at which to generate the child. If null, a randomly-selected free space will
        /// be selected.
        /// </param>
        /// <param name="orbitParameters">
        /// An optional list of parameters which describe the child's orbit. May be null.
        /// </param>
        public override BioZone GenerateChildOfType(Type type, Vector3? position, object[] constructorParameters)
        {
            var child = base.GenerateChildOfType(type, position, constructorParameters);

            Orbits.Orbit.SetOrbit(
                child,
                MainGalaxy,
                (float)Math.Round(Randomizer.Static.NextDouble(0.1), 3));

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
        /// Generates the <see cref="Mass"/> of this <see cref="Orbiter"/>.
        /// </summary>
        /// <remarks>
        /// The main galaxy is expected to comprise the bulk of the mass.
        /// </remarks>
        private protected override void GenerateMass() => Mass = MainGalaxy.Mass * 1.25;

        /// <summary>
        /// Generates the <see cref="Shape"/> of this <see cref="CelestialEntity"/>.
        /// </summary>
        private protected override void GenerateShape() => Shape = new Sphere(MainGalaxy.Radius * 10);
    }
}
