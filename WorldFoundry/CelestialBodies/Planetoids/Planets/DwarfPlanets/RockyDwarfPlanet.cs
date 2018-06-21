using Substances;
using System.Collections.Generic;
using System.Numerics;
using WorldFoundry.Space;

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

        private static readonly string planemoClassPrefix = "Rocky";
        /// <summary>
        /// A prefix to the <see cref="CelestialEntity.TypeName"/> for this class of <see cref="Planemo"/>.
        /// </summary>
        public override string PlanemoClassPrefix => planemoClassPrefix;

        /// <summary>
        /// Initializes a new instance of <see cref="RockyDwarfPlanet"/>.
        /// </summary>
        public RockyDwarfPlanet() : base() { }

        /// <summary>
        /// Initializes a new instance of <see cref="RockyDwarfPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="RockyDwarfPlanet"/> is located.
        /// </param>
        public RockyDwarfPlanet(CelestialRegion parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="RockyDwarfPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="RockyDwarfPlanet"/> is located.
        /// </param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="RockyDwarfPlanet"/> during random generation, in kg.
        /// </param>
        public RockyDwarfPlanet(CelestialRegion parent, double maxMass) : base(parent, maxMass) { }

        /// <summary>
        /// Initializes a new instance of <see cref="RockyDwarfPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="RockyDwarfPlanet"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="RockyDwarfPlanet"/>.</param>
        public RockyDwarfPlanet(CelestialRegion parent, Vector3 position) : base(parent, position) { }

        /// <summary>
        /// Initializes a new instance of <see cref="RockyDwarfPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="RockyDwarfPlanet"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="RockyDwarfPlanet"/>.</param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="RockyDwarfPlanet"/> during random generation, in kg.
        /// </param>
        public RockyDwarfPlanet(CelestialRegion parent, Vector3 position, double maxMass) : base(parent, position, maxMass) { }

        /// <summary>
        /// Determines the <see cref="CelestialEntity.Substance"/> of this <see cref="CelestialEntity"/>.
        /// </summary>
        private protected override void GenerateSubstance()
        {
            var crustProportion = GetCrustProportion();

            // rocky core
            var core = new Material(Chemical.Rock, Phase.Solid);

            var crust = GetIcyCrust();

            Substance = new Substance()
            {
                Composition = new LayeredComposite(new List<(IComposition substance, float proportion)>
                {
                    (core, 1 - crustProportion),
                    (crust, crustProportion),
                }),
                Mass = GenerateMass(),
            };
            GenerateShape();
        }
    }
}
