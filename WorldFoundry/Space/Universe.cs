using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Numerics;
using WorldFoundry.Utilities.MathUtil.Shapes;

namespace WorldFoundry.Space
{
    /// <summary>
    /// The Universe is the top-level celestial "object" in a hierarchy.
    /// </summary>
    public class Universe : CelestialObject
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

        internal static IDictionary<Type, (float proportion, object[] constructorParameters)> childPossibilities =
            new Dictionary<Type, (float proportion, object[] constructorParameters)>
            {
                { typeof(GalaxySupercluster), (1, null) },
            };
        /// <summary>
        /// The types of children this region of space might have.
        /// </summary>
        [NotMapped]
        public override IDictionary<Type, (float proportion, object[] constructorParameters)> ChildPossibilities => childPossibilities;

        /// <summary>
        /// Specifies the velocity of the <see cref="Orbiter"/>.
        /// </summary>
        /// <remarks>
        /// The universe has no velocity. This will always return <see cref="Vector3.Zero"/>, and
        /// setting it will have no effect.
        /// </remarks>
        [NotMapped]
        public override Vector3 Velocity
        {
            get => Vector3.Zero;
            set { }
        }

        /// <summary>
        /// Initializes a new instance of <see cref="Universe"/>.
        /// </summary>
        public Universe() { }

        /// <summary>
        /// Determines whether this <see cref="CelestialObject"/> contains the <see cref="Position"/> of
        /// the specified <see cref="CelestialObject"/>.
        /// </summary>
        /// <param name="other">The <see cref="CelestialObject"/> to test for inclusion within this one.</param>
        /// <returns>
        /// True if this <see cref="CelestialObject"/> contains the <see cref="Position"/> of the specified one.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="other"/> cannot be null.</exception>
        /// <remarks>
        /// The universe contains everything, removing the need for any calculations.
        /// </remarks>
        internal override bool ContainsObject(CelestialObject other) => true;

        /// <summary>
        /// Generates the <see cref="Mass"/> of this <see cref="Orbiter"/>.
        /// </summary>
        /// <remarks>
        /// The mass of the universe is infinite.
        /// </remarks>
        protected override void GenerateMass() => Mass = double.PositiveInfinity;

        /// <summary>
        /// Generates the <see cref="Shape"/> of this <see cref="CelestialEntity"/>.
        /// </summary>
        /// <remarks>
        /// A universe is modeled as a sphere with vast a radius, roughly 4 million times the size of
        /// the real observable universe.
        ///
        /// Approximately 4e18 superclusters might be found in the modeled universe, by volume
        /// (although this would require exhaustive "exploration" to populate so many grid spaces).
        /// This makes the universe effectively infinite in scope, if not in linear dimensions.
        /// </remarks>
        protected override void GenerateShape() => Shape = new Sphere(1.89214e33);

        /// <summary>
        /// Determines a temperature for this <see cref="ThermalBody"/>, in K.
        /// </summary>
        /// <remarks>The ambient temperature of the universe is 2.73 K.</remarks>
        protected override void GenerateTemperature() => Temperature = 2.73f;
    }
}
