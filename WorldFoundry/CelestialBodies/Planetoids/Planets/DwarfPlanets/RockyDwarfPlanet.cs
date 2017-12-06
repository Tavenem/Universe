using System.Numerics;
using WorldFoundry.Space;
using WorldFoundry.Substances;

namespace WorldFoundry.CelestialBodies.Planetoids.Planets.DwarfPlanets
{
    /// <summary>
    /// A rocky dwarf planet without the typical subsurface ice/water mantle.
    /// </summary>
    public class RockyDwarfPlanet : DwarfPlanet
    {
        internal new static double densityForType = 4000;
        /// <summary>
        /// Indicates the average density of this type of <see cref="Planetoid"/>, in kg/m³.
        /// </summary>
        internal override double DensityForType => densityForType;

        private static string planemoClassPrefix = "Rocky";
        /// <summary>
        /// A prefix to the <see cref="CelestialEntity.TypeName"/> for this class of <see cref="Planemo"/>.
        /// </summary>
        public override string PlanemoClassPrefix => planemoClassPrefix;

        /// <summary>
        /// Initializes a new instance of <see cref="RockyDwarfPlanet"/>.
        /// </summary>
        public RockyDwarfPlanet() { }

        /// <summary>
        /// Initializes a new instance of <see cref="RockyDwarfPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="RockyDwarfPlanet"/> is located.
        /// </param>
        public RockyDwarfPlanet(CelestialObject parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="RockyDwarfPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="RockyDwarfPlanet"/> is located.
        /// </param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="RockyDwarfPlanet"/> during random generation, in kg.
        /// </param>
        public RockyDwarfPlanet(CelestialObject parent, double maxMass) : base(parent, maxMass) { }

        /// <summary>
        /// Initializes a new instance of <see cref="RockyDwarfPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="RockyDwarfPlanet"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="RockyDwarfPlanet"/>.</param>
        public RockyDwarfPlanet(CelestialObject parent, Vector3 position) : base(parent, position) { }

        /// <summary>
        /// Initializes a new instance of <see cref="RockyDwarfPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="RockyDwarfPlanet"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="RockyDwarfPlanet"/>.</param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="RockyDwarfPlanet"/> during random generation, in kg.
        /// </param>
        public RockyDwarfPlanet(CelestialObject parent, Vector3 position, double maxMass) : base(parent, position, maxMass) { }

        /// <summary>
        /// Determines the composition of this <see cref="Planetoid"/>.
        /// </summary>
        private protected override void GenerateComposition()
        {
            Composition = new Mixture();

            var crustProportion = GetCrustProportion();

            // rocky core
            Composition.Mixtures.Add(new Mixture(new MixtureComponent[]
            {
                new MixtureComponent
                {
                    Chemical = Chemical.Rock,
                    Phase = Phase.Solid,
                    Proportion = 1,
                },
            })
            {
                Proportion = 1 - crustProportion,
            });

            AddIcyCrust(1, crustProportion);
        }
    }
}
