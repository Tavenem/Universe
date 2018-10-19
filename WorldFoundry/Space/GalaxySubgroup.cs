using MathAndScience.Shapes;
using Substances;
using System.Collections.Generic;
using System.Linq;
using MathAndScience.Numerics;
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
        /// <summary>
        /// The radius of the maximum space required by this type of <see cref="CelestialEntity"/>,
        /// in meters.
        /// </summary>
        public const double Space = 2.5e22;

        private const double ChildDensity = 1.0e-70;

        private static readonly List<ChildDefinition> _childDefinitions = new List<ChildDefinition>
        {
            new ChildDefinition(typeof(DwarfGalaxy), DwarfGalaxy.Space, ChildDensity * 0.26),
            new ChildDefinition(typeof(GlobularCluster), GlobularCluster.Space, ChildDensity * 0.74),
        };

        private const string _baseTypeName = "Galaxy Subgroup";
        /// <summary>
        /// The base name for this type of <see cref="CelestialEntity"/>.
        /// </summary>
        public override string BaseTypeName => _baseTypeName;

        /// <summary>
        /// The types of children found in this region.
        /// </summary>
        public override IEnumerable<ChildDefinition> ChildDefinitions
            => base.ChildDefinitions.Concat(_childDefinitions);

        private Galaxy _mainGalaxy;
        /// <summary>
        /// The main <see cref="Galaxy"/> around which the other objects in this <see
        /// cref="GalaxySubgroup"/> orbit.
        /// </summary>
        public Galaxy MainGalaxy => _mainGalaxy ?? (_mainGalaxy = GetMainGalaxy());

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

        internal override Orbiter GenerateChild(ChildDefinition definition)
        {
            var child = base.GenerateChild(definition);

            Orbit.SetOrbit(
                child,
                MainGalaxy,
                Randomizer.Instance.NextDouble(0.1));

            return child;
        }

        internal override void PrepopulateRegion()
        {
            if (_isPrepopulated)
            {
                return;
            }
            base.PrepopulateRegion();

            GetMainGalaxy();
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
            Shape = new Sphere(MainGalaxy.Radius * 10);
        }

        /// <summary>
        /// Randomly determines the main <see cref="Galaxy"/> of this <see cref="GalaxySubgroup"/>,
        /// which all other objects orbit.
        /// </summary>
        /// <remarks>70% of large galaxies are spirals.</remarks>
        private Galaxy GetMainGalaxy()
            => Randomizer.Instance.NextDouble() <= 0.7
                ? new SpiralGalaxy(this)
                : (Galaxy)new EllipticalGalaxy(this);
    }
}
