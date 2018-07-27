using Substances;
using System;
using System.Collections.Generic;
using System.Numerics;
using WorldFoundry.Space;

namespace WorldFoundry.CelestialBodies.Planetoids.Planets.DwarfPlanets
{
    /// <summary>
    /// A hot, rocky dwarf planet with a molten rock mantle; usually the result of tidal stress.
    /// </summary>
    public class LavaDwarfPlanet : DwarfPlanet
    {
        internal new static double _densityForType = 4000;
        /// <summary>
        /// Indicates the average density of this type of <see cref="Planetoid"/>, in kg/m³.
        /// </summary>
        internal override double DensityForType => _densityForType;

        internal new static int _maxSatellites = 0;
        /// <summary>
        /// The upper limit on the number of satellites this <see cref="Planetoid"/> might have. The
        /// actual number is determined by the orbital characteristics of the satellites it actually has.
        /// </summary>
        /// <remarks>
        /// Lava planets have no satellites; whatever forces have caused their surface trauma should
        /// also inhibit stable satellite orbits.
        /// </remarks>
        public override int MaxSatellites => _maxSatellites;

        private const string _planemoClassPrefix = "Lava";
        /// <summary>
        /// A prefix to the <see cref="CelestialEntity.TypeName"/> for this class of <see cref="Planemo"/>.
        /// </summary>
        public override string PlanemoClassPrefix => _planemoClassPrefix;

        /// <summary>
        /// Initializes a new instance of <see cref="LavaDwarfPlanet"/>.
        /// </summary>
        public LavaDwarfPlanet() { }

        /// <summary>
        /// Initializes a new instance of <see cref="LavaDwarfPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="LavaDwarfPlanet"/> is located.
        /// </param>
        public LavaDwarfPlanet(CelestialRegion parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="LavaDwarfPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="LavaDwarfPlanet"/> is located.
        /// </param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="LavaDwarfPlanet"/> during random generation, in kg.
        /// </param>
        public LavaDwarfPlanet(CelestialRegion parent, double maxMass) : base(parent, maxMass) { }

        /// <summary>
        /// Initializes a new instance of <see cref="LavaDwarfPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="LavaDwarfPlanet"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="LavaDwarfPlanet"/>.</param>
        public LavaDwarfPlanet(CelestialRegion parent, Vector3 position) : base(parent, position) { }

        /// <summary>
        /// Initializes a new instance of <see cref="LavaDwarfPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="LavaDwarfPlanet"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="LavaDwarfPlanet"/>.</param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="LavaDwarfPlanet"/> during random generation, in kg.
        /// </param>
        public LavaDwarfPlanet(CelestialRegion parent, Vector3 position, double maxMass) : base(parent, position, maxMass) { }

        /// <summary>
        /// Determines the <see cref="CelestialEntity.Substance"/> of this <see cref="CelestialEntity"/>.
        /// </summary>
        private protected override void GenerateSubstance()
        {
            // rocky core
            var coreProportion = GetCoreProportion();
            var core = new Material(Chemical.Rock, Phase.Solid);

            var crustProportion = GetCrustProportion();

            // molten rock mantle
            var mantleProportion = 1.0 - coreProportion - crustProportion;
            var mantle = new Material(Chemical.Rock, Phase.Liquid);

            // rocky crust
            // 50% chance of dust
            var dust = Math.Round(Math.Max(0, Randomizer.Static.NextDouble(-0.5, 0.5)), 3);
            IComposition crust = null;
            if (dust > 0)
            {
                crust = new Composite(
                    (Chemical.Rock, Phase.Solid, 1 - dust),
                    (Chemical.Dust, Phase.Solid, dust));
            }
            else
            {
                crust = new Material(Chemical.Rock, Phase.Solid);
            }

            Substance = new Substance()
            {
                Composition = new LayeredComposite(
                    (core, coreProportion),
                    (mantle, mantleProportion),
                    (crust, crustProportion)),
                Mass = GenerateMass(),
            };
            GenerateShape();
        }
    }
}
