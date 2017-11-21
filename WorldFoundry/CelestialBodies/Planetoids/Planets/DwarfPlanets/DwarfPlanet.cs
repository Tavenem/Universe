using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using WorldFoundry.CelestialBodies.Planetoids.Asteroids;
using WorldFoundry.Climate;
using WorldFoundry.Orbits;
using WorldFoundry.Space;
using WorldFoundry.Substances;
using WorldFoundry.Utilities;

namespace WorldFoundry.CelestialBodies.Planetoids.Planets.DwarfPlanets
{
    /// <summary>
    /// A dwarf planet: a body large enough to form a roughly spherical shape under its own gravity,
    /// but not large enough to clear its orbital "neighborhood" of smaller bodies.
    /// </summary>
    public class DwarfPlanet : Planemo
    {
        /// <summary>
        /// The base name for this type of <see cref="CelestialEntity"/>.
        /// </summary>
        public new static string BaseTypeName => "Dwarf Planet";

        /// <summary>
        /// The maximum mass allowed for this type of <see cref="Planetoid"/> during random
        /// generation, in kg. Null indicates no maximum.
        /// </summary>
        /// <remarks>
        /// An arbitrary limit separating rogue dwarf planets from rogue planets.
        /// Within orbital systems, a calculated value for clearing the neighborhood is used instead.
        /// </remarks>
        protected override double? MaxMass_Type => 2.0e22;

        /// <summary>
        /// The minimum mass allowed for this type of <see cref="Planetoid"/> during random
        /// generation, in kg. Null indicates a minimum of 0.
        /// </summary>
        /// <remarks>
        /// The minimum to achieve hydrostatic equilibrium and be considered a dwarf planet.
        /// </remarks>
        protected override double? MinMass_Type => 3.4e20f;

        /// <summary>
        /// The chance that this <see cref="Planemo"/> will have rings, as a rate between 0.0 and 1.0.
        /// </summary>
        /// <remarks>
        /// There is a low chance of most planets having substantial rings; 10 for <see
        /// cref="DwarfPlanet"/>s.
        /// </remarks>
        protected override float RingChance => 10;

        /// <summary>
        /// Indicates the average density of this type of <see cref="Planetoid"/>, in kg/m³.
        /// </summary>
        protected override double TypeDensity => 2000;

        /// <summary>
        /// Initializes a new instance of <see cref="DwarfPlanet"/>.
        /// </summary>
        public DwarfPlanet() { }

        /// <summary>
        /// Initializes a new instance of <see cref="DwarfPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="DwarfPlanet"/> is located.
        /// </param>
        public DwarfPlanet(CelestialObject parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="DwarfPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="DwarfPlanet"/> is located.
        /// </param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="DwarfPlanet"/> during random generation, in kg.
        /// </param>
        public DwarfPlanet(CelestialObject parent, double maxMass) : base(parent, maxMass) { }

        /// <summary>
        /// Initializes a new instance of <see cref="DwarfPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="DwarfPlanet"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="DwarfPlanet"/>.</param>
        public DwarfPlanet(CelestialObject parent, Vector3 position) : base(parent, position) { }

        /// <summary>
        /// Initializes a new instance of <see cref="DwarfPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="DwarfPlanet"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="DwarfPlanet"/>.</param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="DwarfPlanet"/> during random generation, in kg.
        /// </param>
        public DwarfPlanet(CelestialObject parent, Vector3 position, double maxMass) : base(parent, position, maxMass) { }

        /// <summary>
        /// Adds an appropriate icy crust with the given proportion.
        /// </summary>
        protected virtual void AddIcyCrust(int layer, float crustProportion)
        {
            var dust = (float)Math.Round(Randomizer.Static.NextDouble(), 3);
            var total = dust;

            // 50% chance of not including the following:
            var waterIce = (float)Math.Round(Math.Max(0, Randomizer.Static.NextDouble(-0.5, 0.5)), 3);
            total += waterIce;

            var n2 = (float)Math.Round(Math.Max(0, Randomizer.Static.NextDouble(-0.5, 0.5)), 3);
            total += n2;

            var ch4 = (float)Math.Round(Math.Max(0, Randomizer.Static.NextDouble(-0.5, 0.5)), 3);
            total += ch4;

            var co = (float)Math.Round(Math.Max(0, Randomizer.Static.NextDouble(-0.5, 0.5)), 3);
            total += co;

            var co2 = (float)Math.Round(Math.Max(0, Randomizer.Static.NextDouble(-0.5, 0.5)), 3);
            total += co2;

            var nh3 = (float)Math.Round(Math.Max(0, Randomizer.Static.NextDouble(-0.5, 0.5)), 3);
            total += nh3;

            var ratio = 1.0f / total;
            dust *= ratio;
            waterIce *= ratio;
            n2 *= ratio;
            ch4 *= ratio;
            co *= ratio;
            co2 *= ratio;
            nh3 *= ratio;

            var crust = new Mixture(new MixtureComponent[]
            {
                new MixtureComponent
                {
                    Substance = new Substance(Chemical.Dust, Phase.Solid),
                    Proportion = dust,
                },
            })
            {
                Proportion = crustProportion,
            };
            if (waterIce > 0)
            {
                crust.Components.Add(new MixtureComponent
                {
                    Substance = new Substance(Chemical.Water, Phase.Solid),
                    Proportion = waterIce,
                });
            }
            if (n2 > 0)
            {
                crust.Components.Add(new MixtureComponent
                {
                    Substance = new Substance(Chemical.Nitrogen, Phase.Solid),
                    Proportion = n2,
                });
            }
            if (ch4 > 0)
            {
                crust.Components.Add(new MixtureComponent
                {
                    Substance = new Substance(Chemical.Methane, Phase.Solid),
                    Proportion = ch4,
                });
            }
            if (co > 0)
            {
                crust.Components.Add(new MixtureComponent
                {
                    Substance = new Substance(Chemical.CarbonMonoxide, Phase.Solid),
                    Proportion = co,
                });
            }
            if (co2 > 0)
            {
                crust.Components.Add(new MixtureComponent
                {
                    Substance = new Substance(Chemical.CarbonDioxide, Phase.Solid),
                    Proportion = co2,
                });
            }
            if (nh3 > 0)
            {
                crust.Components.Add(new MixtureComponent
                {
                    Substance = new Substance(Chemical.Ammonia, Phase.Solid),
                    Proportion = nh3,
                });
            }
            Composition.Mixtures.Add(crust);
        }

        /// <summary>
        /// Determines an albedo for this <see cref="CelestialBody"/> (a value between 0 and 1).
        /// </summary>
        protected override void GenerateAlbedo()
        {
            var albedo = (float)Math.Round(Randomizer.Static.NextDouble(0.1, 0.6), 3);
            var surfaceIce = Composition.GetChildAtLastLayer().Components
                .Where(c => c.Substance.Phase == Phase.Solid)
                .Sum(c => c.Proportion);
            Albedo = (albedo * (1.0f - surfaceIce)) + (0.9f * surfaceIce);
        }

        /// <summary>
        /// Generates an atmosphere for this <see cref="Planetoid"/>.
        /// </summary>
        protected override void GenerateAtmosphere()
        {
            // Atmosphere is based solely on the volatile ices present.

            var crust = Composition.GetChildAtLastLayer();

            var water = crust.GetSubstance(Chemical.Water, Phase.Solid).Proportion;
            bool anyIces = water > 0;

            var n2 = crust.GetSubstance(Chemical.Nitrogen, Phase.Solid).Proportion;
            anyIces &= n2 > 0;

            var ch4 = crust.GetSubstance(Chemical.Methane, Phase.Solid).Proportion;
            anyIces &= ch4 > 0;

            var co = crust.GetSubstance(Chemical.CarbonMonoxide, Phase.Solid).Proportion;
            anyIces &= co > 0;

            var co2 = crust.GetSubstance(Chemical.CarbonDioxide, Phase.Solid).Proportion;
            anyIces &= co2 > 0;

            var nh3 = crust.GetSubstance(Chemical.Ammonia, Phase.Solid).Proportion;
            anyIces &= nh3 > 0;

            if (!anyIces)
            {
                return;
            }

            Atmosphere = new Atmosphere(this, (float)Math.Round(Randomizer.Static.NextDouble(2.5)))
            {
                Mixtures = new HashSet<Mixture>()
            };
            var firstLayer = new Mixture
            {
                Components = new HashSet<MixtureComponent>()
            };

            if (water > 0)
            {
                firstLayer.Components.Add(new MixtureComponent
                {
                    Substance = new Substance(Chemical.Water, Phase.Gas),
                    Proportion = water,
                });
            }
            if (n2 > 0)
            {
                firstLayer.Components.Add(new MixtureComponent
                {
                    Substance = new Substance(Chemical.Nitrogen, Phase.Gas),
                    Proportion = n2,
                });
            }
            if (ch4 > 0)
            {
                firstLayer.Components.Add(new MixtureComponent
                {
                    Substance = new Substance(Chemical.Methane, Phase.Gas),
                    Proportion = ch4,
                });
            }
            if (co > 0)
            {
                firstLayer.Components.Add(new MixtureComponent
                {
                    Substance = new Substance(Chemical.CarbonMonoxide, Phase.Gas),
                    Proportion = co,
                });
            }
            if (co2 > 0)
            {
                firstLayer.Components.Add(new MixtureComponent
                {
                    Substance = new Substance(Chemical.CarbonDioxide, Phase.Gas),
                    Proportion = co2,
                });
            }
            if (nh3 > 0)
            {
                firstLayer.Components.Add(new MixtureComponent
                {
                    Substance = new Substance(Chemical.Ammonia, Phase.Gas),
                    Proportion = nh3,
                });
            }

            Atmosphere.Mixtures.Add(firstLayer);
        }

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

            // water-ice mantle
            var mantleProportion = 1.0f - coreProportion - crustProportion;
            var mantleIce = (float)Math.Round(Randomizer.Static.NextDouble(0.2, 1), 3);
            Composition.Mixtures.Add(new Mixture(1, new MixtureComponent[]
            {
                new MixtureComponent
                {
                    Substance = new Substance(Chemical.Water, Phase.Solid),
                    Proportion = mantleIce,
                },
                new MixtureComponent
                {
                    Substance = new Substance(Chemical.Water, Phase.Liquid),
                    Proportion = 1.0f - mantleIce,
                },
            })
            {
                Proportion = mantleProportion,
            });

            AddIcyCrust(2, crustProportion);
        }

        /// <summary>
        /// Generates the <see cref="Mass"/> of this <see cref="Orbiter"/>.
        /// </summary>
        /// <remarks>
        /// The Stern-Levison parameter for neighborhood-clearing is used to determined maximum mass
        /// at which the dwarf planet would not be able to do so at this orbital distance. We set the
        /// maximum at two orders of magnitude less than this (dwarf planets in our solar system all
        /// have masses below 5 orders of magnitude less).
        /// </remarks>
        protected override void GenerateMass()
        {
            var maxMass = MaxMass;
            if (Parent != null)
            {
                maxMass = Math.Min(maxMass, GetSternLevisonLambdaMass() / 100);
                if (maxMass < MinMass)
                {
                    maxMass = MinMass; // sanity check; may result in a "dwarf" planet which *can* clear its neighborhood
                }
            }

            Mass = Math.Round(Randomizer.Static.NextDouble(MinMass, maxMass));
        }

        /// <summary>
        /// Determines an orbit for this <see cref="Orbiter"/>.
        /// </summary>
        /// <param name="orbitedObject">The <see cref="Orbiter"/> which is to be orbited.</param>
        public override void GenerateOrbit(Orbiter orbitedObject)
        {
            if (orbitedObject == null)
            {
                return;
            }

            Orbit.SetOrbit(
                this,
                orbitedObject,
                GetDistanceToTarget(orbitedObject),
                Eccentricity,
                (float)Math.Round(Randomizer.Static.NextDouble(0.9), 4),
                (float)Math.Round(Randomizer.Static.NextDouble(Utilities.MathUtil.Constants.TwoPI), 4),
                (float)Math.Round(Randomizer.Static.NextDouble(Utilities.MathUtil.Constants.TwoPI), 4),
                (float)Math.Round(Randomizer.Static.NextDouble(Utilities.MathUtil.Constants.TwoPI), 4));
        }

        /// <summary>
        /// Generates a new satellite for this <see cref="Planetoid"/> with the specified parameters.
        /// </summary>
        /// <returns>A satellite <see cref="Planetoid"/> with an appropriate orbit.</returns>
        protected override Planetoid GenerateSatellite(double periapsis, float eccentricity, double maxMass)
        {
            Planetoid satellite = null;

            // If the mass limit allows, there is an even chance that the satellite is a smaller dwarf planet.
            if (maxMass > MinMass && Randomizer.Static.NextBoolean())
            {
                satellite = new DwarfPlanet(Parent, maxMass);
            }
            else
            {
                // Otherwise, it is an asteroid, selected from the standard distribution of types.
                double chance = Randomizer.Static.NextDouble();
                if (chance <= 0.75)
                {
                    satellite = new CTypeAsteroid(Parent, maxMass);
                }
                else if (chance <= 0.9)
                {
                    satellite = new STypeAsteroid(Parent, maxMass);
                }
                else
                {
                    satellite = new MTypeAsteroid(Parent, maxMass);
                }
            }

            Orbit.SetOrbit(
                satellite,
                this,
                periapsis,
                eccentricity,
                (float)Math.Round(Randomizer.Static.NextDouble(0.5), 4),
                (float)Math.Round(Randomizer.Static.NextDouble(Utilities.MathUtil.Constants.TwoPI), 4),
                (float)Math.Round(Randomizer.Static.NextDouble(Utilities.MathUtil.Constants.TwoPI), 4),
                (float)Math.Round(Randomizer.Static.NextDouble(Utilities.MathUtil.Constants.TwoPI), 4));

            return satellite;
        }

        /// <summary>
        /// Randomly determines the proportionate amount of the composition devoted to the core of a <see cref="Planemo"/>.
        /// </summary>
        /// <returns>A proportion, from 0.0 to 1.0.</returns>
        public override float GetCoreProportion() => (float)Math.Round(Randomizer.Static.NextDouble(0.2, 0.55), 3);
    }
}
