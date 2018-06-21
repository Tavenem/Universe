using MathAndScience.MathUtil.Shapes;
using Substances;
using System;
using System.Collections.Generic;
using System.Numerics;
using WorldFoundry.Substances;

namespace WorldFoundry.Space
{
    /// <summary>
    /// The Universe is the top-level celestial "object" in a hierarchy.
    /// </summary>
    public class Universe : CelestialRegion
    {
        internal new static string baseTypeName = "Universe";
        /// <summary>
        /// The base name for this type of <see cref="CelestialEntity"/>.
        /// </summary>
        public override string BaseTypeName => baseTypeName;

        public static double childDensity = 1.5e-82;
        /// <summary>
        /// The average number of children within the grid per m³.
        /// </summary>
        public override double ChildDensity => childDensity;

        internal static IList<(Type type,float proportion, object[] constructorParameters)> childPossibilities =
            new List<(Type type,float proportion, object[] constructorParameters)>
            {
                (typeof(GalaxySupercluster), 1, null),
            };
        /// <summary>
        /// The types of children this region of space might have.
        /// </summary>
        public override IList<(Type type,float proportion, object[] constructorParameters)> ChildPossibilities => childPossibilities;

        /// <summary>
        /// Specifies the velocity of the <see cref="Orbiter"/>.
        /// </summary>
        /// <remarks>
        /// The universe has no velocity. This will always return <see cref="Vector3.Zero"/>, and
        /// setting it will have no effect.
        /// </remarks>
        public override Vector3 Velocity
        {
            get => Vector3.Zero;
            set { }
        }

        /// <summary>
        /// Initializes a new instance of <see cref="Universe"/>.
        /// </summary>
        public Universe() : base() { }

        /// <summary>
        /// Determines whether this <see cref="CelestialRegion"/> contains the <see cref="Position"/> of
        /// the specified <see cref="CelestialRegion"/>.
        /// </summary>
        /// <param name="other">The <see cref="CelestialRegion"/> to test for inclusion within this one.</param>
        /// <returns>
        /// True if this <see cref="CelestialRegion"/> contains the <see cref="Position"/> of the specified one.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="other"/> cannot be null.</exception>
        /// <remarks>
        /// The universe contains everything, removing the need for any calculations.
        /// </remarks>
        internal override bool ContainsObject(CelestialRegion other) => true;

        /// <summary>
        /// Generates the <see cref="CelestialEntity.Substance"/> of this <see cref="CelestialEntity"/>.
        /// </summary>
        /// <remarks>
        /// A universe is modeled as a sphere with vast a radius, roughly 4 million times the size of
        /// the real observable universe.
        ///
        /// Approximately 4e18 superclusters might be found in the modeled universe, by volume
        /// (although this would require exhaustive "exploration" to populate so many grid spaces).
        /// This makes the universe effectively infinite in scope, if not in linear dimensions.
        /// </remarks>
        private protected override void GenerateSubstance()
        {
            Substance = new Substance
            {
                Composition = CosmicSubstances.IntergalacticMedium.GetDeepCopy(),
                Mass = double.PositiveInfinity,
                Temperature = 2.73f,
            };
            SetShape(new Sphere(1.89214e33));
        }
    }
}
