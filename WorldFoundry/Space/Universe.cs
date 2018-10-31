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

        private protected override string BaseTypeName => "Universe";

        private protected override IEnumerable<ChildDefinition> ChildDefinitions
            => base.ChildDefinitions.Concat(_childDefinitions);

        /// <summary>
        /// Specifies the velocity of the <see cref="Orbits.CelestialEntity"/>.
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
        internal Universe() { }

        /// <summary>
        /// Generates a new universe.
        /// </summary>
        /// <returns>A new <see cref="Universe"/> instance</returns>
        public static Universe New()
        {
            var universe = new Universe();
            universe.Init();
            return universe;
        }

        private protected override void GenerateLocation(CelestialRegion parent = null, Vector3? position = null)
            => Location = new Region(this, null, new Sphere(1.89214e33, position ?? Vector3.Zero));

        // A universe is modeled as a sphere with vast a radius, roughly 4 million times the size of
        // the real observable universe.
        //
        // Approximately 4e18 superclusters might be found in the modeled universe, by volume
        // (although this would require exhaustive "exploration" to populate so much space).
        // This makes the universe effectively infinite in scope, if not in linear dimensions.
        private protected override void GenerateSubstance()
            => Substance = new Substance
            {
                Composition = CosmicSubstances.IntergalacticMedium.GetDeepCopy(),
                Mass = double.PositiveInfinity,
                Temperature = 2.73,
            };
    }
}
