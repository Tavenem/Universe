using MathAndScience.Shapes;
using Substances;
using System.Collections.Generic;
using System.Linq;
using MathAndScience.Numerics;
using WorldFoundry.Place;
using WorldFoundry.Space.Galaxies;
using WorldFoundry.Substances;

namespace WorldFoundry.Space
{
    /// <summary>
    /// A collection of gravitationally-bound galaxies, mostly small dwarfs orbiting a few large galaxies.
    /// </summary>
    public class GalaxyGroup : CelestialRegion
    {
        /// <summary>
        /// The radius of the maximum space required by this type of <see cref="CelestialEntity"/>,
        /// in meters.
        /// </summary>
        public const double Space = 3.0e23;

        private static readonly List<ChildDefinition> _childDefinitions = new List<ChildDefinition>
        {
            new ChildDefinition(typeof(DwarfGalaxy), DwarfGalaxy.Space, 1.5e-70),
        };

        private const string _baseTypeName = "Galaxy Group";
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

        internal override void PrepopulateRegion()
        {
            if (_isPrepopulated)
            {
                return;
            }
            base.PrepopulateRegion();

            if (!(Location is Region region))
            {
                return;
            }

            var amount = Randomizer.Instance.Next(1, 6);
            Vector3 position;
            for (var i = 0; i < amount; i++)
            {
                if (region.TryGetOpenSpace(GalaxySubgroup.Space, out var location))
                {
                    position = location;
                }
                else
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
            Shape = new Sphere(Randomizer.Instance.NextDouble(1.5e23, 3.0e23)); // ~500–1000 kpc
        }
    }
}
