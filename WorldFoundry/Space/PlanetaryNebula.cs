using MathAndScience.Shapes;
using Substances;
using System;
using System.Numerics;
using WorldFoundry.CelestialBodies.Stars;
using WorldFoundry.Substances;

namespace WorldFoundry.Space
{
    /// <summary>
    /// The remnants of a red giant, which have left behind an ionized gas cloud surrounding a white
    /// dwarf star.
    /// </summary>
    /// <remarks>Not actually a nebula. Gets its name from a quirk of history.</remarks>
    public class PlanetaryNebula : CelestialRegion
    {
        private const string _baseTypeName = "Planetary Nebula";
        /// <summary>
        /// The base name for this type of <see cref="CelestialEntity"/>.
        /// </summary>
        public override string BaseTypeName => _baseTypeName;

        /// <summary>
        /// Initializes a new instance of <see cref="PlanetaryNebula"/>.
        /// </summary>
        public PlanetaryNebula() { }

        /// <summary>
        /// Initializes a new instance of <see cref="PlanetaryNebula"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="PlanetaryNebula"/> is located.
        /// </param>
        public PlanetaryNebula(CelestialRegion parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="PlanetaryNebula"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="PlanetaryNebula"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="PlanetaryNebula"/>.</param>
        public PlanetaryNebula(CelestialRegion parent, Vector3 position) : base(parent, position) { }

        private void GenerateChildren() => new StarSystem(this, Vector3.Zero, typeof(WhiteDwarf));

        /// <summary>
        /// Generates the <see cref="CelestialEntity.Substance"/> of this <see cref="CelestialEntity"/>.
        /// </summary>
        /// <remarks>
        /// Actual planetary nebulae are spherical only 20% of the time, but the shapes are irregular
        /// and not considered critical to model precisely, especially given their extremely
        /// attenuated nature. Instead, a ~1 ly sphere is used.
        /// </remarks>
        private protected override void GenerateSubstance()
        {
            Substance = new Substance
            {
                Composition = CosmicSubstances.StellarMaterial.GetDeepCopy(),
                Mass = Math.Round(Randomizer.Static.NextDouble(1.99e29, 1.99e30)), // ~0.1–1 solar mass.
                Temperature = 10000,
            };
            SetShape(new Sphere(9.5e15));
        }

        /// <summary>
        /// Generates an appropriate population of child objects in local space, in an area around
        /// the given position.
        /// </summary>
        /// <param name="position">The location around which to generate child objects.</param>
        /// <remarks>
        /// Planetary nebulae have all their immediate children generated at once, the first time
        /// this method is called.
        /// </remarks>
        public override void PopulateRegion(Vector3 position)
        {
            if (!IsGridSpacePopulated(Vector3.Zero))
            {
                GridSpaces[Vector3.Zero] = true;
                GenerateChildren();
            }
        }
    }
}
