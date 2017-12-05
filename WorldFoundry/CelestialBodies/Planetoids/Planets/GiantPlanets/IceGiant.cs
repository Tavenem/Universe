using System;
using System.Numerics;
using WorldFoundry.Space;
using WorldFoundry.Substances;
using WorldFoundry.Utilities;

namespace WorldFoundry.CelestialBodies.Planetoids.Planets.GiantPlanets
{
    /// <summary>
    /// An ice giant planet, such as Neptune or Uranus.
    /// </summary>
    public class IceGiant : GiantPlanet
    {
        internal new static string baseTypeName = "Ice Giant";
        /// <summary>
        /// The base name for this type of <see cref="CelestialEntity"/>.
        /// </summary>
        public override string BaseTypeName => baseTypeName;

        internal new static int maxSatellites = 40;
        /// <summary>
        /// The upper limit on the number of satellites this <see cref="Planetoid"/> might have. The
        /// actual number is determined by the orbital characteristics of the satellites it actually has.
        /// </summary>
        /// <remarks>
        /// Set to 40 for <see cref="IceGiant"/>. For reference, Uranus has 27 moons, and Neptune has
        /// 14 moons.
        /// </remarks>
        public override int MaxSatellites => maxSatellites;

        /// <summary>
        /// Initializes a new instance of <see cref="IceGiant"/>.
        /// </summary>
        public IceGiant() { }

        /// <summary>
        /// Initializes a new instance of <see cref="IceGiant"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="IceGiant"/> is located.
        /// </param>
        public IceGiant(CelestialObject parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="IceGiant"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="IceGiant"/> is located.
        /// </param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="IceGiant"/> during random generation, in kg.
        /// </param>
        public IceGiant(CelestialObject parent, double maxMass) : base(parent, maxMass) { }

        /// <summary>
        /// Initializes a new instance of <see cref="IceGiant"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="IceGiant"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="IceGiant"/>.</param>
        public IceGiant(CelestialObject parent, Vector3 position) : base(parent, position) { }

        /// <summary>
        /// Initializes a new instance of <see cref="IceGiant"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="IceGiant"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="IceGiant"/>.</param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="IceGiant"/> during random generation, in kg.
        /// </param>
        public IceGiant(CelestialObject parent, Vector3 position, double maxMass) : base(parent, position, maxMass) { }

        /// <summary>
        /// Determines the composition of this <see cref="Planetoid"/>.
        /// </summary>
        protected override void GenerateComposition()
        {
            Composition = new Mixture();

            // Iron-nickel inner core.
            var coreProportion = GetCoreProportion();
            var innerCoreProportion = base.GetCoreProportion() * coreProportion;
            var coreNickel = (float)Math.Round(Randomizer.Static.NextDouble(0.03, 0.15), 4);
            Composition.Mixtures.Add(new Mixture(new MixtureComponent[]
            {
                new MixtureComponent
                {
                    Chemical = Chemical.Iron,
                    Phase = Phase.Solid,
                    Proportion = 1 - coreNickel,
                },
                new MixtureComponent
                {
                    Chemical = Chemical.Nickel,
                    Phase = Phase.Solid,
                    Proportion = coreNickel,
                },
            })
            {
                Proportion = innerCoreProportion,
            });

            // Molten rock outer core.
            Composition.Mixtures.Add(new Mixture(1, new MixtureComponent[]
            {
                new MixtureComponent
                {
                    Chemical = Chemical.Rock,
                    Phase = Phase.Liquid,
                    Proportion = 1,
                },
            })
            {
                Proportion = coreProportion - innerCoreProportion,
            });

            var layer = 2;
            var diamond = 1 - coreProportion;
            var water = (float)Math.Round(Math.Max(0, Randomizer.Static.NextDouble(diamond)), 4);
            diamond -= water;
            var nh4 = (float)Math.Round(Math.Max(0, Randomizer.Static.NextDouble(diamond)), 4);
            diamond -= nh4;
            var ch4 = (float)Math.Round(Math.Max(0, Randomizer.Static.NextDouble(diamond)), 4);
            diamond -= ch4;

            // Liquid diamond mantle
            if (!Troschuetz.Random.TMath.IsZero(diamond))
            {
                Composition.Mixtures.Add(new Mixture(layer, new MixtureComponent[]
                {
                    new MixtureComponent
                    {
                        Chemical = Chemical.Diamond,
                        Phase = Phase.Liquid,
                        Proportion = 1,
                    },
                })
                {
                    Proportion = diamond,
                });
                layer++;
            }

            // Supercritical water-ammonia ocean layer (blends seamlessly with lower atmosphere)
            var upperLayer = new Mixture(layer, new MixtureComponent[]
            {
                new MixtureComponent
                {
                    Chemical = Chemical.Water,
                    Phase = Phase.Liquid,
                    Proportion = water,
                },
            })
            {
                Proportion = 1 - coreProportion - diamond,
            };
            if (ch4 > 0)
            {
                upperLayer.Components.Add(new MixtureComponent
                {
                    Chemical = Chemical.Methane,
                    Phase = Phase.Liquid,
                    Proportion = ch4,
                });
            }
            if (nh4 > 0)
            {
                upperLayer.Components.Add(new MixtureComponent
                {
                    Chemical = Chemical.Ammonia,
                    Phase = Phase.Liquid,
                    Proportion = nh4,
                });
            }
            upperLayer.BalanceProportions();
            Composition.Mixtures.Add(upperLayer);
        }

        /// <summary>
        /// Generates an appropriate density for this <see cref="Planetoid"/>.
        /// </summary>
        /// <remarks>
        /// No "puffy" ice giants.
        /// </remarks>
        protected override void GenerateDensity() => Density = Math.Round(Randomizer.Static.NextDouble(MinDensity, MaxDensity));
    }
}
