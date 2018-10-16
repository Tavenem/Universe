using MathAndScience.Shapes;
using Substances;
using System.Collections.Generic;
using System.Linq;
using MathAndScience.Numerics;
using WorldFoundry.Place;
using WorldFoundry.Substances;

namespace WorldFoundry.Space
{
    /// <summary>
    /// The Universe is the top-level celestial "object" in a hierarchy.
    /// </summary>
    public class Universe : CelestialRegion
    {
        private static readonly List<ChildDefinition> _childDefinitions = new List<ChildDefinition>
        {
            new ChildDefinition(typeof(GalaxySupercluster), GalaxySupercluster.Space, 5.8e-26),
        };

        private const string _baseTypeName = "Universe";
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
        /// Specifies the velocity of the <see cref="Orbits.Orbiter"/>.
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
        public Universe() { }

        private protected override void GenerateLocation(CelestialRegion parent = null, Vector3? position = null)
            => _location = new Region(this, null, new Sphere(1.89214e33, position ?? Vector3.Zero));

        /// <summary>
        /// Generates the <see cref="CelestialEntity.Substance"/> of this <see cref="CelestialEntity"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A universe is modeled as a sphere with vast a radius, roughly 4 million times the size of
        /// the real observable universe.
        /// </para>
        /// <para>
        /// Approximately 4e18 superclusters might be found in the modeled universe, by volume
        /// (although this would require exhaustive "exploration" to populate so many grid spaces).
        /// This makes the universe effectively infinite in scope, if not in linear dimensions.
        /// </para>
        /// </remarks>
        private protected override void GenerateSubstance()
        {
            Substance = new Substance
            {
                Composition = CosmicSubstances.IntergalacticMedium.GetDeepCopy(),
                Mass = double.PositiveInfinity,
                Temperature = 2.73,
            };
        }
    }
}
