using System;
using System.Numerics;
using WorldFoundry.Space;
using WorldFoundry.Substances;
using WorldFoundry.Utilities;

namespace WorldFoundry.CelestialBodies.Planetoids.Planets.DwarfPlanets
{
    /// <summary>
    /// A hot, rocky dwarf planet with a molten rock mantle; usually the result of tidal stress.
    /// </summary>
    public class LavaDwarfPlanet : DwarfPlanet
    {
        /// <summary>
        /// A prefix to the <see cref="CelestialEntity.TypeName"/> for this class of <see cref="Planemo"/>.
        /// </summary>
        public override string PlanemoClassPrefix => "Lava";

        /// <summary>
        /// The upper limit on the number of satellites this <see cref="Planetoid"/> might have. The
        /// actual number is determined by the orbital characteristics of the satellites it actually has.
        /// </summary>
        /// <remarks>
        /// Lava planets have no satellites; whatever forces have caused their surface trauma should
        /// also inhibit stable satellite orbits.
        /// </remarks>
        public override int MaxSatellites => 0;

        /// <summary>
        /// Indicates the average density of this type of <see cref="Planetoid"/>, in kg/m³.
        /// </summary>
        /// <remarks>Higher than usual for a dwarf planet due to lack of water-ice mantle.</remarks>
        protected override double TypeDensity => 4000;

        /// <summary>
        /// Initializes a new instance of <see cref="LavaDwarfPlanet"/>.
        /// </summary>
        public LavaDwarfPlanet() { }

        /// <summary>
        /// Initializes a new instance of <see cref="LavaDwarfPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="LavaDwarfPlanet"/> is located.
        /// </param>
        public LavaDwarfPlanet(CelestialObject parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="LavaDwarfPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="LavaDwarfPlanet"/> is located.
        /// </param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="LavaDwarfPlanet"/> during random generation, in kg.
        /// </param>
        public LavaDwarfPlanet(CelestialObject parent, double maxMass) : base(parent, maxMass) { }

        /// <summary>
        /// Initializes a new instance of <see cref="LavaDwarfPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="LavaDwarfPlanet"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="LavaDwarfPlanet"/>.</param>
        public LavaDwarfPlanet(CelestialObject parent, Vector3 position) : base(parent, position) { }

        /// <summary>
        /// Initializes a new instance of <see cref="LavaDwarfPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="LavaDwarfPlanet"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="LavaDwarfPlanet"/>.</param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="LavaDwarfPlanet"/> during random generation, in kg.
        /// </param>
        public LavaDwarfPlanet(CelestialObject parent, Vector3 position, double maxMass) : base(parent, position, maxMass) { }

        /// <summary>
        /// Determines the composition of this <see cref="Planetoid"/>.
        /// </summary>
        protected override void GenerateComposition()
        {
            Composition = new Mixture();

            // rocky core
            var coreProportion = GetCoreProportion();
            Composition.Mixtures.Add(new Mixture(new MixtureComponent[]
            {
                new MixtureComponent
                {
                    Substance = new Substance(Chemical.Rock, Phase.Solid),
                    Proportion = 1,
                },
            })
            {
                Proportion = coreProportion,
            });

            var crustProportion = GetCrustProportion();

            // molten rock mantle
            var mantleProportion = 1.0f - coreProportion - crustProportion;
            Composition.Mixtures.Add(new Mixture(1, new MixtureComponent[]
            {
                new MixtureComponent
                {
                    Substance = new Substance(Chemical.Rock, Phase.Liquid),
                    Proportion = 1,
                },
            })
            {
                Proportion = mantleProportion,
            });

            // rocky crust
            // 50% chance of dust
            var dust = (float)Math.Round(Math.Max(0, Randomizer.Static.NextDouble(-0.5, 0.5)), 3);
            var crust = new Mixture(2, new MixtureComponent[]
            {
                new MixtureComponent
                {
                    Substance = new Substance(Chemical.Rock, Phase.Solid),
                    Proportion = 1.0f - dust,
                },
            })
            {
                Proportion = crustProportion,
            };
            if (dust > 0)
            {
                crust.Components.Add(new MixtureComponent
                {
                    Substance = new Substance(Chemical.Dust, Phase.Solid),
                    Proportion = dust,
                });
            }
            Composition.Mixtures.Add(crust);
        }
    }
}
