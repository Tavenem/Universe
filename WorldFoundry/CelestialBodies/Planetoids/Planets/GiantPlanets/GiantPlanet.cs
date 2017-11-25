using System;
using System.Collections.Generic;
using System.Numerics;
using WorldFoundry.CelestialBodies.Planetoids.Asteroids;
using WorldFoundry.CelestialBodies.Planetoids.Planets.DwarfPlanets;
using WorldFoundry.CelestialBodies.Planetoids.Planets.TerrestrialPlanets;
using WorldFoundry.Climate;
using WorldFoundry.Space;
using WorldFoundry.Substances;
using WorldFoundry.Utilities;

namespace WorldFoundry.CelestialBodies.Planetoids.Planets.GiantPlanets
{
    /// <summary>
    /// A gas giant planet (excluding ice giants, which have their own subclass).
    /// </summary>
    public class GiantPlanet : Planemo
    {
        protected const int density_MinExtreme = 600;
        protected const int density_Min = 1100;
        protected const int density_Max = 1650;

        /// <summary>
        /// The base name for this type of <see cref="CelestialEntity"/>.
        /// </summary>
        public new static string BaseTypeName => "Gas Giant";

        /// <summary>
        /// The maximum mass allowed for this type of <see cref="Planetoid"/> during random
        /// generation, in kg. Null indicates no maximum.
        /// </summary>
        /// <remarks>At around this limit the planet will have sufficient mass to sustain fusion, and become a brown dwarf.</remarks>
        internal new static double? MaxMass_Type => 2.5e28;

        /// <summary>
        /// The upper limit on the number of satellites this <see cref="Planetoid"/> might have. The
        /// actual number is determined by the orbital characteristics of the satellites it actually has.
        /// </summary>
        /// <remarks>
        /// Set to 75 for <see cref="GiantPlanet"/>. For reference, Jupiter has 67 moons, and Saturn
        /// has 62 (non-ring) moons.
        /// </remarks>
        public new static int MaxSatellites => 75;

        /// <summary>
        /// The minimum mass allowed for this type of <see cref="Planetoid"/> during random
        /// generation, in kg. Null indicates a minimum of 0.
        /// </summary>
        /// <remarks>Below this limit the planet will not have sufficient mass to retain hydrogen, and will be a terrestrial planet.</remarks>
        internal new static double? MinMass_Type => 6.0e25;

        /// <summary>
        /// The chance that this <see cref="Planemo"/> will have rings, as a rate between 0.0 and 1.0.
        /// </summary>
        /// <remarks>Giants are almost guaranteed to have rings.</remarks>
        protected new static float RingChance => 90;

        /// <summary>
        /// Initializes a new instance of <see cref="GiantPlanet"/>.
        /// </summary>
        public GiantPlanet() { }

        /// <summary>
        /// Initializes a new instance of <see cref="GiantPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="GiantPlanet"/> is located.
        /// </param>
        public GiantPlanet(CelestialObject parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="GiantPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="GiantPlanet"/> is located.
        /// </param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="GiantPlanet"/> during random generation, in kg.
        /// </param>
        public GiantPlanet(CelestialObject parent, double maxMass) : base(parent, maxMass) { }

        /// <summary>
        /// Initializes a new instance of <see cref="GiantPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="GiantPlanet"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="GiantPlanet"/>.</param>
        public GiantPlanet(CelestialObject parent, Vector3 position) : base(parent, position) { }

        /// <summary>
        /// Initializes a new instance of <see cref="GiantPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="GiantPlanet"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="GiantPlanet"/>.</param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="GiantPlanet"/> during random generation, in kg.
        /// </param>
        public GiantPlanet(CelestialObject parent, Vector3 position, double maxMass) : base(parent, position, maxMass) { }

        /// <summary>
        /// Determines an albedo for this <see cref="CelestialBody"/> (a value between 0 and 1).
        /// </summary>
        protected override void GenerateAlbedo() => Albedo = (float)Math.Round(Randomizer.Static.NextDouble(0.275, 0.35), 3);

        /// <summary>
        /// Generates an atmosphere for this <see cref="Planetoid"/>.
        /// </summary>
        /// <remarks>
        /// Giants have no solid surface, instead the "surface" is arbitrarily defined as the level
        /// where the pressure is 1 MPa.
        /// </remarks>
        protected override void GenerateAtmosphere()
        {
            Atmosphere = new Atmosphere(this, 1000)
            {
                Mixtures = new HashSet<Mixture>()
            };

            var trace = (float)Math.Round(Randomizer.Static.NextDouble(0.025), 4);

            var h = (float)Math.Round(Randomizer.Static.NextDouble(0.75, 0.97), 4);
            var he = 1 - h - trace;

            var ch4 = (float)Math.Round(Randomizer.Static.NextDouble(trace), 4);
            trace -= ch4;

            // 50% chance not to have each of these components
            var c2h6 = (float)Math.Round(Math.Max(0, Randomizer.Static.NextDouble(-0.5, 0.5)), 5);
            var traceTotal = c2h6;
            var nh3 = (float)Math.Round(Math.Max(0, Randomizer.Static.NextDouble(-0.5, 0.5)), 5);
            traceTotal += nh3;
            var waterVapor = (float)Math.Round(Math.Max(0, Randomizer.Static.NextDouble(-0.5, 0.5)), 5);
            traceTotal += waterVapor;

            var surfaceTemp = GetTotalTemperature();

            float water = 0, ice = 0;
            if (surfaceTemp < Chemical.Water.AntoineMinimumTemperature ||
                (surfaceTemp < Chemical.Water.AntoineMaximumTemperature &&
                Chemical.Water.CalculateVaporPressure(surfaceTemp) <= 1000))
            {
                water = (float)Math.Round(Randomizer.Static.NextDouble(), 5);
                ice = (float)Math.Round(Randomizer.Static.NextDouble(), 5);
                traceTotal += water + ice;
            }

            float ch4Liquid = 0, ch4Ice = 0;
            if (surfaceTemp < Chemical.Methane.AntoineMinimumTemperature ||
                (surfaceTemp < Chemical.Methane.AntoineMaximumTemperature &&
                Chemical.Methane.CalculateVaporPressure(surfaceTemp) <= 1000))
            {
                ch4Liquid = (float)Math.Round(Randomizer.Static.NextDouble(), 5);
                ch4Ice = (float)Math.Round(Randomizer.Static.NextDouble(), 5);
                traceTotal += ch4Liquid + ch4Ice;
            }

            float nh3Liquid = 0, nh3Ice = 0;
            if (surfaceTemp < Chemical.Ammonia.AntoineMinimumTemperature ||
                (surfaceTemp < Chemical.Ammonia.AntoineMaximumTemperature &&
                Chemical.Ammonia.CalculateVaporPressure(surfaceTemp) <= 1000))
            {
                nh3Liquid = (float)Math.Round(Randomizer.Static.NextDouble(), 5);
                nh3Ice = (float)Math.Round(Randomizer.Static.NextDouble(), 5);
                traceTotal += nh3Liquid + nh3Ice;
            }

            var nh4sh = (float)Math.Round(Randomizer.Static.NextDouble(), 5);
            traceTotal += nh4sh;

            float ratio = trace / traceTotal;
            c2h6 *= ratio;
            nh3 *= ratio;
            waterVapor *= ratio;
            water *= ratio;
            ice *= ratio;
            ch4Liquid *= ratio;
            ch4Ice *= ratio;
            nh3Liquid *= ratio;
            nh3Ice *= ratio;
            nh4sh *= ratio;

            var firstLayer = new Mixture(new MixtureComponent[]
            {
                new MixtureComponent
                {
                    Substance = new Substance(Chemical.Hydrogen, Phase.Gas),
                    Proportion = h,
                },
                new MixtureComponent
                {
                    Substance = new Substance(Chemical.Helium, Phase.Gas),
                    Proportion = he,
                },
                new MixtureComponent
                {
                    Substance = new Substance(Chemical.Methane, Phase.Gas),
                    Proportion = ch4,
                },
            })
            {
                Proportion = 1,
            };
            if (c2h6 > 0)
            {
                firstLayer.Components.Add(new MixtureComponent
                {
                    Substance = new Substance(Chemical.Ethane, Phase.Gas),
                    Proportion = c2h6,
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
            if (waterVapor > 0)
            {
                firstLayer.Components.Add(new MixtureComponent
                {
                    Substance = new Substance(Chemical.Water, Phase.Gas),
                    Proportion = waterVapor,
                });
            }
            if (water > 0)
            {
                firstLayer.Components.Add(new MixtureComponent
                {
                    Substance = new Substance(Chemical.Water, Phase.Liquid),
                    Proportion = water,
                });
            }
            if (ice > 0)
            {
                firstLayer.Components.Add(new MixtureComponent
                {
                    Substance = new Substance(Chemical.Water, Phase.Solid),
                    Proportion = ice,
                });
            }
            if (ch4Liquid > 0)
            {
                firstLayer.Components.Add(new MixtureComponent
                {
                    Substance = new Substance(Chemical.Methane, Phase.Liquid),
                    Proportion = ch4Liquid,
                });
            }
            if (ch4Ice > 0)
            {
                firstLayer.Components.Add(new MixtureComponent
                {
                    Substance = new Substance(Chemical.Methane, Phase.Solid),
                    Proportion = ch4Ice,
                });
            }
            if (nh3Liquid > 0)
            {
                firstLayer.Components.Add(new MixtureComponent
                {
                    Substance = new Substance(Chemical.Ammonia, Phase.Liquid),
                    Proportion = nh3Liquid,
                });
            }
            if (nh3Ice> 0)
            {
                firstLayer.Components.Add(new MixtureComponent
                {
                    Substance = new Substance(Chemical.Ammonia, Phase.Solid),
                    Proportion = nh3Ice,
                });
            }
            if (nh4sh > 0)
            {
                firstLayer.Components.Add(new MixtureComponent
                {
                    Substance = new Substance(Chemical.AmmoniumHydrosulfide, Phase.Solid),
                    Proportion = nh4sh,
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

            // Iron-nickel inner core.
            var coreProportion = GetCoreProportion();
            var innerCoreProportion = base.GetCoreProportion() * coreProportion;
            var coreNickel = (float)Math.Round(Randomizer.Static.NextDouble(0.03, 0.15), 4);
            Composition.Mixtures.Add(new Mixture(new MixtureComponent[]
            {
                new MixtureComponent
                {
                    Substance = new Substance(Chemical.Iron, Phase.Solid),
                    Proportion = 1 - coreNickel,
                },
                new MixtureComponent
                {
                    Substance = new Substance(Chemical.Nickel, Phase.Solid),
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
                    Substance = new Substance(Chemical.Rock, Phase.Liquid),
                    Proportion = 1,
                },
            })
            {
                Proportion = coreProportion - innerCoreProportion,
            });

            // Metallic hydrogen lower mantle
            var layer = 2;
            var metalH = (float)Math.Round(Math.Max(0, Randomizer.Static.NextDouble(-0.1, 0.55)), 2);
            if (metalH > 0)
            {
                Composition.Mixtures.Add(new Mixture(layer, new MixtureComponent[]
                {
                    new MixtureComponent
                    {
                        Substance = new Substance(Chemical.Hydrogen_Metallic, Phase.Liquid),
                        Proportion = 1,
                    },
                })
                {
                    Proportion = metalH,
                });
                layer++;
            }

            // Supercritical fluid upper layer (blends seamlessly with lower atmosphere)
            var upperLayerProportion = 1 - coreProportion - metalH;
            var water = upperLayerProportion;
            var fluidH = water * 0.71f;
            water -= fluidH;
            var fluidHe = water * 0.24f;
            water -= fluidHe;
            var ne = (float)Math.Round(Randomizer.Static.NextDouble(water), 4);
            water -= ne;
            var ch4 = (float)Math.Round(Randomizer.Static.NextDouble(water), 4);
            water = Math.Max(0, water - ch4);
            var nh4 = (float)Math.Round(Randomizer.Static.NextDouble(water), 4);
            water = Math.Max(0, water - nh4);
            var c2h6 = (float)Math.Round(Randomizer.Static.NextDouble(water), 4);
            water = Math.Max(0, water - c2h6);
            var upperLayer = new Mixture(layer, new MixtureComponent[]
            {
                new MixtureComponent
                {
                    Substance = new Substance(Chemical.Hydrogen, Phase.Liquid),
                    Proportion = 0.71f,
                },
                new MixtureComponent
                {
                    Substance = new Substance(Chemical.Helium, Phase.Liquid),
                    Proportion = 0.24f,
                },
                new MixtureComponent
                {
                    Substance = new Substance(Chemical.Neon, Phase.Liquid),
                    Proportion = ne,
                },
            })
            {
                Proportion = upperLayerProportion,
            };
            if (ch4 > 0)
            {
                upperLayer.Components.Add(new MixtureComponent
                {
                    Substance = new Substance(Chemical.Methane, Phase.Liquid),
                    Proportion = ch4,
                });
            }
            if (nh4 > 0)
            {
                upperLayer.Components.Add(new MixtureComponent
                {
                    Substance = new Substance(Chemical.Ammonia, Phase.Liquid),
                    Proportion = nh4,
                });
            }
            if (c2h6 > 0)
            {
                upperLayer.Components.Add(new MixtureComponent
                {
                    Substance = new Substance(Chemical.Ethane, Phase.Liquid),
                    Proportion = c2h6,
                });
            }
            if (water > 0)
            {
                upperLayer.Components.Add(new MixtureComponent
                {
                    Substance = new Substance(Chemical.Water, Phase.Liquid),
                    Proportion = water,
                });
            }
            Composition.Mixtures.Add(upperLayer);
        }

        /// <summary>
        /// Generates an appropriate density for this <see cref="Planetoid"/>.
        /// </summary>
        /// <remarks>
        /// Relatively low chance of a "puffy" giant (Saturn-like, low-density).
        /// </remarks>
        protected override void GenerateDensity()
        {
            if (Randomizer.Static.NextDouble() <= 0.2)
            {
                Density = Math.Round(Randomizer.Static.NextDouble(density_MinExtreme, density_Min));
            }
            else
            {
                Density = Math.Round(Randomizer.Static.NextDouble(density_Min, density_Max));
            }
        }

        /// <summary>
        /// Generates the <see cref="Mass"/> of this <see cref="Orbiter"/>.
        /// </summary>
        protected override void GenerateMass() => Mass = Math.Round(Randomizer.Static.NextDouble(MinMass, MaxMass));

        /// <summary>
        /// Generates a new satellite for this <see cref="Planetoid"/> with the specified parameters.
        /// </summary>
        /// <returns>A satellite <see cref="Planetoid"/> with an appropriate orbit.</returns>
        protected override Planetoid GenerateSatellite(double periapsis, float eccentricity, double maxMass)
        {
            Planetoid satellite = null;
            double chance;

            // If the mass limit allows, there is an even chance that the satellite is a smaller planet.
            if (maxMass > TerrestrialPlanet.MinMass_Type && Randomizer.Static.NextBoolean())
            {
                // Select from the standard distribution of types.
                chance = Randomizer.Static.NextDouble();

                // Planets with very low orbits are lava planets due to tidal
                // stress (plus a small percentage of others due to impact trauma).

                // The maximum mass and density are used to calculate an outer
                // Roche limit (may not be the actual Roche limit for the body
                // which gets generated).
                if (periapsis < GetRocheLimit(TerrestrialPlanet.maxDensity) * 1.05 || chance <= 0.01)
                {
                    satellite = new LavaPlanet(Parent, maxMass);
                }
                else if (chance <= 0.65) // Most will be standard terrestrial.
                {
                    satellite = new TerrestrialPlanet(Parent, maxMass);
                }
                else if (chance <= 0.75)
                {
                    satellite = new IronPlanet(Parent, maxMass);
                }
                else
                {
                    satellite = new OceanPlanet(Parent, maxMass);
                }
            }

            // Otherwise, if the mass limit allows, there is an even chance that the satellite is a dwarf planet.
            else if (maxMass > DwarfPlanet.MinMass_Type && Randomizer.Static.NextBoolean())
            {
                chance = Randomizer.Static.NextDouble();
                // Dwarf planets with very low orbits are lava planets due to tidal stress (plus a small percentage of others due to impact trauma).
                if (periapsis < GetRocheLimit(DwarfPlanet.TypeDensity) * 1.05 || chance <= 0.01)
                {
                    satellite = new LavaDwarfPlanet(Parent, maxMass);
                }
                else if (chance <= 0.75) // Most will be standard.
                {
                    satellite = new DwarfPlanet(Parent, maxMass);
                }
                else
                {
                    satellite = new RockyDwarfPlanet(Parent, maxMass);
                }
            }

            // Otherwise, it is an asteroid, selected from the standard distribution of types.
            else if (maxMass > 0)
            {
                chance = Randomizer.Static.NextDouble();
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

            if (satellite != null)
            {
                Orbits.Orbit.SetOrbit(
                    satellite,
                    this,
                    periapsis,
                    eccentricity,
                    (float)Math.Round(Randomizer.Static.NextDouble(0.5), 4),
                    (float)Math.Round(Randomizer.Static.NextDouble(Utilities.MathUtil.Constants.TwoPI), 4),
                    (float)Math.Round(Randomizer.Static.NextDouble(Utilities.MathUtil.Constants.TwoPI), 4),
                    (float)Math.Round(Randomizer.Static.NextDouble(Utilities.MathUtil.Constants.TwoPI), 4));
            }

            return satellite;
        }

        /// <summary>
        /// Randomly determines the proportionate amount of the composition devoted to the core of a
        /// <see cref="Planemo"/>.
        /// </summary>
        /// <returns>A proportion, from 0.0 to 1.0.</returns>
        /// <remarks>
        /// Cannot be less than the minimum required to become a gas giant rather than a terrestrial planet.
        /// </remarks>
        public override float GetCoreProportion() => (float)Math.Min(Randomizer.Static.NextDouble(0.02, 0.2), (MinMass_Type ?? 0) / Mass);
    }
}
