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
    /// A collection of gravitationally-bound galaxies, mostly small dwarfs orbiting a few large galaxies.
    /// </summary>
    public class GalaxyGroup : CelestialObject
    {
        internal new static string baseTypeName = "Galaxy Group";
        /// <summary>
        /// The base name for this type of <see cref="CelestialEntity"/>.
        /// </summary>
        public override string BaseTypeName => baseTypeName;

        public static double childDensity = 1.5e-70;
        /// <summary>
        /// The average number of children within the grid per m³.
        /// </summary>
        public override double ChildDensity => childDensity;

        internal static IDictionary<Type, (float proportion, object[] constructorParameters)> childPossibilities =
            new Dictionary<Type, (float proportion, object[] constructorParameters)>
            {
                { typeof(DwarfGalaxy), (1, null) },
            };
        /// <summary>
        /// The types of children this region of space might have.
        /// </summary>
        [NotMapped]
        public override IDictionary<Type, (float proportion, object[] constructorParameters)> ChildPossibilities => childPossibilities;

        /// <summary>
        /// Initializes a new instance of <see cref="GalaxyGroup"/>.
        /// </summary>
        public GalaxyGroup() { }

        /// <summary>
        /// Initializes a new instance of <see cref="GalaxyGroup"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="GalaxyGroup"/> is located.
        /// </param>
        public GalaxyGroup(CelestialObject parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="GalaxyGroup"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="GalaxyGroup"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="GalaxyGroup"/>.</param>
        public GalaxyGroup(CelestialObject parent, Vector3 position) : base(parent, position) { }

        private void GenerateChildren()
        {
            var amount = Randomizer.Static.Next(1, 6);
            Vector3 position;
            var counter = 0;
            for (int i = 0; i < amount; i++)
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
        /// Generates the <see cref="Mass"/> of this <see cref="Orbiter"/>.
        /// </summary>
        /// <remarks>
        /// General average; 1.0e14 solar masses.
        /// </remarks>
        private protected override void GenerateMass() => Mass = 2.0e44;

        /// <summary>
        /// Generates the <see cref="Shape"/> of this <see cref="CelestialEntity"/>.
        /// </summary>
        /// <remarks>
        /// ~500–1000 kpc
        /// </remarks>
        private protected override void GenerateShape() => SetShape(new Sphere(Randomizer.Static.NextDouble(1.5e23, 3.0e23)));

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
                GetGridSpace(Vector3.Zero, true).Populated = true;
                GenerateChildren();
            }

            base.PopulateRegion(position);
        }
    }
}
