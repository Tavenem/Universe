using MathAndScience.MathUtil.Shapes;
using Substances;
using System;
using System.Collections.Generic;
using System.Numerics;
using WorldFoundry.Space.Galaxies;
using WorldFoundry.Substances;

namespace WorldFoundry.Space
{
    /// <summary>
    /// A collection of gravitationally-bound galaxies, mostly small dwarfs orbiting a few large galaxies.
    /// </summary>
    public class GalaxyGroup : CelestialRegion
    {
        private const string _baseTypeName = "Galaxy Group";
        /// <summary>
        /// The base name for this type of <see cref="CelestialEntity"/>.
        /// </summary>
        public override string BaseTypeName => _baseTypeName;

        private const double _childDensity = 1.5e-70;
        /// <summary>
        /// The average number of children within the grid per m³.
        /// </summary>
        public override double ChildDensity => _childDensity;

        internal static IList<(Type type, double proportion, object[] constructorParameters)> _childPossibilities =
            new List<(Type type, double proportion, object[] constructorParameters)>
            {
                (typeof(DwarfGalaxy), 1, null),
            };
        /// <summary>
        /// The types of children this region of space might have.
        /// </summary>
        public override IList<(Type type, double proportion, object[] constructorParameters)> ChildPossibilities => _childPossibilities;

        /// <summary>
        /// Initializes a new instance of <see cref="GalaxyGroup"/>.
        /// </summary>
        public GalaxyGroup() { }

        /// <summary>
        /// Initializes a new instance of <see cref="GalaxyGroup"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="GalaxyGroup"/> is located.
        /// </param>
        public GalaxyGroup(CelestialRegion parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="GalaxyGroup"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="GalaxyGroup"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="GalaxyGroup"/>.</param>
        public GalaxyGroup(CelestialRegion parent, Vector3 position) : base(parent, position) { }

        private void GenerateChildren()
        {
            var amount = Randomizer.Static.Next(1, 6);
            Vector3 position;
            var counter = 0;
            for (var i = 0; i < amount; i++)
            {
                // Pick a random spot, but make sure it isn't outside local space, and not already occupied.

                // Use a sanity check counter in case the region is overcrowded by early children and
                // the full number will not easily fit.
                counter = 0;
                do
                {
                    position = new Vector3(
                        (float)Math.Round(Randomizer.Static.NextDouble(LocalSpaceScale), 4),
                        (float)Math.Round(Randomizer.Static.NextDouble(LocalSpaceScale), 4),
                        (float)Math.Round(Randomizer.Static.NextDouble(LocalSpaceScale), 4));
                    counter++;
                } while ((position.Length() > LocalSpaceScale || IsGridSpacePopulated(PositionToGridCoords(position))) && counter < 100);
                if (counter >= 100)
                {
                    break;
                }
                new GalaxySubgroup(this, position);
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
                Mass = 2.0e44, // general average; 1.0e14 solar masses
            };
            SetShape(new Sphere(Randomizer.Static.NextDouble(1.5e23, 3.0e23))); // ~500–1000 kpc
        }

        /// <summary>
        /// Generates an appropriate population of child objects in local space, in an area around
        /// the given position.
        /// </summary>
        /// <param name="position">The location around which to generate child objects.</param>
        /// <remarks>
        /// Galaxy groups have their primary subgroups generated all at once, the first time this
        /// method is called.
        /// </remarks>
        public override void PopulateRegion(Vector3 position)
        {
            if (!IsGridSpacePopulated(Vector3.Zero))
            {
                GridSpaces[Vector3.Zero] = true;
                GenerateChildren();
            }

            base.PopulateRegion(position);
        }
    }
}
