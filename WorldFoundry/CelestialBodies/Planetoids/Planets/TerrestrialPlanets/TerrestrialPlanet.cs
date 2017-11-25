using System;
using System.Linq;
using System.Numerics;
using Troschuetz.Random;
using WorldFoundry.Space;
using WorldFoundry.Substances;
using WorldFoundry.Utilities;

namespace WorldFoundry.CelestialBodies.Planetoids.Planets.TerrestrialPlanets
{
    /// <summary>
    /// A primarily rocky planet, relatively small in comparison to gas and ice giants.
    /// </summary>
    public class TerrestrialPlanet : Planemo
    {
        internal const float density_Max = 6000;

        /// <summary>
        /// The base name for this type of <see cref="CelestialEntity"/>.
        /// </summary>
        public new static string BaseTypeName => "Terrestrial Planet";

        /// <summary>
        /// Used to allow or prevent oxygen in the atmosphere of a terrestrial planet.
        /// </summary>
        /// <remarks>
        /// True by default, but subclasses may hide this with false when their particular natures
        /// make the presence of significant amounts of oxygen impossible.
        /// </remarks>
        protected static bool CanHaveOxygen => true;

        /// <summary>
        /// Used to allow or prevent water in the composition and atmosphere of a terrestrial planet.
        /// </summary>
        /// <remarks>
        /// True by default, but subclasses may hide this with false when their particular natures
        /// make the presence of significant amounts of water impossible.
        /// </remarks>
        protected static bool CanHaveWater => true;

        /// <summary>
        /// The maximum mass allowed for this type of <see cref="Planetoid"/> during random
        /// generation, in kg. Null indicates no maximum.
        /// </summary>
        /// <remarks>
        /// At around this limit the planet will have sufficient mass to retain hydrogen, and become
        /// a giant.
        /// </remarks>
        internal new static double? MaxMass_Type => 6.0e25;

        /// <summary>
        /// The minimum mass allowed for this type of <see cref="Planetoid"/> during random
        /// generation, in kg. Null indicates a minimum of 0.
        /// </summary>
        /// <remarks>
        /// An arbitrary limit separating rogue dwarf planets from rogue planets. Within orbital
        /// systems, a calculated value for clearing the neighborhood is used instead.
        /// </remarks>
        internal new static double? MinMass_Type => 2.0e22;

        /// <summary>
        /// A prefix to the <see cref="CelestialEntity.TypeName"/> for this class of <see cref="Planemo"/>.
        /// </summary>
        public new static string PlanemoClassPrefix => "Terrestrial";

        /// <summary>
        /// The chance that this <see cref="Planemo"/> will have rings, as a rate between 0.0 and 1.0.
        /// </summary>
        /// <remarks>
        /// There is a low chance of most planets having substantial rings; 10 for <see
        /// cref="TerrestrialPlanet"/>s.
        /// </remarks>
        protected new static float RingChance => 10;

        /// <summary>
        /// The <see cref="TerrestrialPlanets.HabitabilityRequirements"/> specified during this <see
        /// cref="TerrestrialPlanet"/>'s creation.
        /// </summary>
        private HabitabilityRequirements? habitabilityRequirements;

        /// <summary>
        /// Indicates whether or not this planet has a native population of living organisms.
        /// </summary>
        /// <remarks>
        /// The complexity of life is not presumed. If a planet is basically habitable (liquid
        /// surface water), life in at least a single-celled form may be indicated, and may affect
        /// the atmospheric composition.
        /// </remarks>
        public bool HasBiosphere { get; set; }

        /// <summary>
        /// Indicates that this planet's outermost composition layer is composed of some mixture of
        /// liquids and ices (not necessarily water), rather than a rocky crust.
        /// </summary>
        public bool HasHydrosphere { get; set; }

        /// <summary>
        /// Used to set the proportionate amount of metal in the composition of a terrestrial planet.
        /// </summary>
        protected virtual float MetalProportion => 0.05f;

        /// <summary>
        /// Since the total albedo of a terrestrial planet may change based on surface ice and cloud
        /// cover, the base surface albedo is maintained separately.
        /// </summary>
        public float SurfaceAlbedo { get; set; }

        /// <summary>
        /// Initializes a new instance of <see cref="TerrestrialPlanet"/>.
        /// </summary>
        public TerrestrialPlanet() { }

        /// <summary>
        /// Initializes a new instance of <see cref="TerrestrialPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="TerrestrialPlanet"/> is located.
        /// </param>
        public TerrestrialPlanet(CelestialObject parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="TerrestrialPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="TerrestrialPlanet"/> is located.
        /// </param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="TerrestrialPlanet"/> during random generation, in kg.
        /// </param>
        public TerrestrialPlanet(CelestialObject parent, double maxMass) : base(parent, maxMass) { }

        /// <summary>
        /// Initializes a new instance of <see cref="TerrestrialPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="TerrestrialPlanet"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="TerrestrialPlanet"/>.</param>
        public TerrestrialPlanet(CelestialObject parent, Vector3 position) : base(parent, position) { }

        /// <summary>
        /// Initializes a new instance of <see cref="TerrestrialPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="TerrestrialPlanet"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="TerrestrialPlanet"/>.</param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="TerrestrialPlanet"/> during random generation, in kg.
        /// </param>
        public TerrestrialPlanet(CelestialObject parent, Vector3 position, double maxMass) : base(parent, position, maxMass) { }

        private void AddToHydrosphere(Substance substance, float proportion, ref float crustAtmosphereRatio)
        {
            var surface = Composition.GetChildAtLastLayer();
            if (HasHydrosphere)
            {
                surface.AddComponent(substance, proportion);
            }
            else
            {
                var layerProportion = surface.Proportion * proportion;
                surface.Proportion -= layerProportion;
                Composition.Mixtures.Add(new Mixture(surface.Layer + 1, new MixtureComponent[]
                {
                    new MixtureComponent
                    {
                        Substance = substance,
                        Proportion = 1,
                    },
                })
                {
                    Proportion = layerProportion,
                });

                HasHydrosphere = true;

                crustAtmosphereRatio = GetCrustAtmosphereRatio(surface);
            }
        }

        private void BoilSurfaceIntoAtmosphere(Chemical chemical, ref float crustAtmosphereRatio)
        {
            var surface = Composition.GetChildAtLastLayer();
            float gasProportion = 0;

            var match = surface.GetSubstance(chemical, Phase.Liquid);
            if (match != null)
            {
                gasProportion += match.Proportion;
                surface.RemoveComponent(chemical, Phase.Liquid);
            }

            match = surface.GetSubstance(chemical, Phase.Solid);
            if (match != null)
            {
                gasProportion += match.Proportion;
                surface.RemoveComponent(chemical, Phase.Solid);
            }

            if (surface.Components.Count == 0)
            {
                Composition.Mixtures.Remove(surface);
                HasHydrosphere = false;
                crustAtmosphereRatio = GetCrustAtmosphereRatio(Composition.GetChildAtLastLayer());
            }

            if (gasProportion > 0)
            {
                Atmosphere.AddComponent(new Substance(chemical, Phase.Gas), gasProportion * crustAtmosphereRatio, true);
            }
        }

        private void CalculateGasPhaseMix(Chemical chemical, float surfaceTemp, float polarTemp, ref float crustAtmosphereRatio)
        {
            // Substance will boil away.
            if (surfaceTemp > chemical.AntoineMaximumTemperature ||
                (surfaceTemp > chemical.AntoineMinimumTemperature &&
                Atmosphere.AtmosphericPressure > chemical.CalculateVaporPressure(surfaceTemp)))
            {
                BoilSurfaceIntoAtmosphere(chemical, ref crustAtmosphereRatio);
            }
            // If the gas is present in the atmosphere and it will not boil away, it will condense,
            // and may freeze.
            else if (Atmosphere.ContainsSubstance(chemical, Phase.Any))
            {
                CondenseAtmosphere(chemical, surfaceTemp, polarTemp, ref crustAtmosphereRatio);
            }
        }

        /// <summary>
        /// Adjusts the phase of various atmospheric and surface substances depending on the surface
        /// temperature of the body.
        /// </summary>
        /// <remarks>
        /// Despite the theoretical possibility of an atmosphere cold enough to precipitate some of
        /// the noble gases, or hydrogen, they are ignored and presumed to exist always as trace
        /// atmospheric gases, never surface liquids or ices, or in large enough quantities to form clouds.
        /// </remarks>
        private void CalculatePhases(int counter)
        {
            var surfaceTemp = Atmosphere.GetSurfaceTemperatureAverageOrbital();
            var polarTemp = Atmosphere.GetSurfaceTemperatureAverageOrbital(true);

            var surface = Composition.GetChildAtLastLayer();
            var crustAtmosphereRatio = GetCrustAtmosphereRatio(surface);

            CalculateGasPhaseMix(Chemical.Methane, surfaceTemp, polarTemp, ref crustAtmosphereRatio);

            CalculateGasPhaseMix(Chemical.CarbonMonoxide, surfaceTemp, polarTemp, ref crustAtmosphereRatio);

            CalculateGasPhaseMix(Chemical.CarbonDioxide, surfaceTemp, polarTemp, ref crustAtmosphereRatio);

            CalculateGasPhaseMix(Chemical.Nitrogen, surfaceTemp, polarTemp, ref crustAtmosphereRatio);

            CalculateGasPhaseMix(Chemical.Oxygen, surfaceTemp, polarTemp, ref crustAtmosphereRatio);

            // No need to check for ozone, since it is only added to
            // atmospheres on planets with liquid surface water, which means
            // temperatures too high for liquid or solid ozone.

            CalculateGasPhaseMix(Chemical.SulphurDioxide, surfaceTemp, polarTemp, ref crustAtmosphereRatio);

            // Water is handled differently, since the planet may already have surface water.
            if (surface.ContainsSubstance(Chemical.Water, Phase.Liquid)
                || surface.ContainsSubstance(Chemical.Water, Phase.Solid)
                || surface.ContainsSubstance(Chemical.Water_Salt, Phase.Liquid)
                || Atmosphere.ContainsSubstance(Chemical.Water, Phase.Gas))
            {
                CalculateWaterPhaseMix(surfaceTemp, polarTemp, crustAtmosphereRatio);
            }

            var oldAlbedo = Albedo;

            // Ices significantly impact albedo.
            if (HasHydrosphere)
            {
                var iceAmount = Math.Min(1, surface.Components
                    .Where(x => x.Substance.Phase == Phase.Solid)
                    .Sum(x => x.Proportion));
                Albedo = (SurfaceAlbedo * (1.0f - iceAmount)) + (0.9f * iceAmount);
            }

            // Clouds also impact albedo.
            float cloudCover = Math.Min(1, Atmosphere.AtmosphericPressure
                * Atmosphere.GetChildAtFirstLayer().Components
                .Where(x => x.Substance.Phase == Phase.Solid || x.Substance.Phase == Phase.Liquid)
                .Sum(s => s.Proportion) / 100.0f);
            Albedo = (SurfaceAlbedo * (1.0f - cloudCover)) + (0.9f * cloudCover);

            // An albedo change might significantly change surface temperature, which may require a
            // re-calculation (but not too many). 5K is used as the threshold for re-calculation,
            // which may lead to some inaccuracies, but should avoid over-repeating for small changes.
            if (counter < 10 && Albedo != oldAlbedo &&
                Math.Abs(surfaceTemp - Atmosphere.GetSurfaceTemperatureAverageOrbital()) > 5)
            {
                CalculatePhases(counter + 1);
            }
        }

        private void CalculateWaterPhaseMix(float surfaceTemp, float polarTemp, float crustAtmosphereRatio)
        {
            var surface = Composition.GetChildAtLastLayer();

            var water = surface.GetSubstance(Chemical.Water, Phase.Liquid)?.Proportion ?? 0;
            var saltWater = surface.GetSubstance(Chemical.Water_Salt, Phase.Liquid)?.Proportion ?? 0;
            var totalWater = water + saltWater;

            var waterVapor = Atmosphere.GetSubstanceProportionInAllChildren(Chemical.Water, Phase.Gas);

            var vaporPressure = Chemical.Water.CalculateVaporPressure(surfaceTemp);

            // Create icecaps.
            if (polarTemp <= Chemical.Water.MeltingPoint)
            {
                var iceProportion = totalWater * 0.28f;
                if (!TMath.IsZero(iceProportion))
                {
                    AddToHydrosphere(new Substance(Chemical.Water, Phase.Solid), iceProportion, ref crustAtmosphereRatio);
                }
            }

            if (surfaceTemp < Chemical.Water.AntoineMinimumTemperature ||
                (surfaceTemp <= Chemical.Water.AntoineMaximumTemperature &&
                Atmosphere.AtmosphericPressure > vaporPressure))
            {
                CondenseWater(surfaceTemp, polarTemp, ref crustAtmosphereRatio, surface, totalWater, waterVapor, vaporPressure);
            }
            // This indicates that all water will boil off. If this is true,
            // it is presumed that photodissociation will eventually reduce the amount
            // of water vapor to a trace gas (the H2 will be lost due to atmospheric
            // escape, and the oxygen will be lost to surface oxidation).
            else if (totalWater > 0 || surface.ContainsSubstance(Chemical.Water, Phase.Solid))
            {
                EvaporateWater(totalWater);
            }

            // At least 1% humidity leads to a reduction of CO2 to a trace gas,
            // by a presumed carbon-silicate cycle.
            var humidity = (Atmosphere.GetChildAtFirstLayer().GetSubstance(Chemical.Water, Phase.Gas)?.Proportion ?? 0 *
                Atmosphere.AtmosphericPressure) / vaporPressure;
            if (humidity >= 0.01)
            {
                ReduceCO2();
            }
        }

        private void ReduceCO2()
        {
            var co2 = Atmosphere.GetSubstanceProportionInAllChildren(Chemical.CarbonDioxide, Phase.Gas);
            if (co2 < 1.0e-3)
            {
                return;
            }

            // Reduce the pressure to reflect the sequestration (unless it's being forced to a specific value).
            if (habitabilityRequirements == null)
            {
                Atmosphere.AtmosphericPressure -= Atmosphere.AtmosphericPressure * co2;
            }

            foreach (var layer in Atmosphere.Mixtures.Where(x =>
                x.ContainsSubstance(Chemical.CarbonDioxide, Phase.Gas)))
            {
                var balance = layer.GetSubstance(Chemical.CarbonDioxide, Phase.Gas)?.Proportion ?? 0;
                layer.RemoveComponent(Chemical.CarbonDioxide, Phase.Gas);
                var liquid = layer.GetSubstance(Chemical.CarbonDioxide, Phase.Liquid);
                if (liquid != null)
                {
                    balance += liquid.Proportion;
                    layer.RemoveComponent(Chemical.CarbonDioxide, Phase.Liquid);
                }
                var ice = layer.GetSubstance(Chemical.CarbonDioxide, Phase.Solid);
                if (ice != null)
                {
                    balance += ice.Proportion;
                    layer.RemoveComponent(Chemical.CarbonDioxide, Phase.Solid);
                }

                co2 = (float)Randomizer.Static.NextDouble(1.5e-5, 1.0e-3);
                layer.SubstanceComponents.Add(Substance.CarbonDioxide, co2);
                balance -= co2;

                // Replace the missing CO2 with inert gases.
                float n2 = balance;
                // Some portion of the N2 may be Ar instead.
                float ar = (float)Math.Max(0, n2 * random.NextDouble(-0.02, 0.04));
                if (layer.ContainsSubstance(Substance.Argon))
                {
                    ar = Math.Max(ar, layer.SubstanceComponents[Substance.Argon]);
                    layer.SubstanceComponents[Substance.Argon] = ar;
                }
                else if (ar > 0)
                {
                    layer.SubstanceComponents.Add(Substance.Argon, ar);
                }

                n2 -= ar;
                // An even smaller fraction may be Kr.
                float kr = (float)Math.Max(0, n2 * random.NextDouble(-2.5e-4, 5.0e-4));
                if (layer.ContainsSubstance(Substance.Krypton))
                {
                    kr = Math.Max(kr, layer.SubstanceComponents[Substance.Krypton]);
                    layer.SubstanceComponents[Substance.Krypton] = kr;
                }
                else if (kr > 0)
                {
                    layer.SubstanceComponents.Add(Substance.Krypton, kr);
                }

                n2 -= kr;
                // An even smaller fraction may be Xe or Ne.
                float xe = (float)Math.Max(0, n2 * random.NextDouble(-1.8e-5, 3.5e-5));
                if (layer.ContainsSubstance(Substance.Xenon))
                {
                    xe = Math.Max(xe, layer.SubstanceComponents[Substance.Xenon]);
                    layer.SubstanceComponents[Substance.Xenon] = xe;
                }
                else if (xe > 0)
                {
                    layer.SubstanceComponents.Add(Substance.Xenon, xe);
                }

                n2 -= xe;
                float ne = (float)Math.Max(0, n2 * random.NextDouble(-1.8e-5, 3.5e-5));
                if (layer.ContainsSubstance(Substance.Neon))
                {
                    ne = Math.Max(ne, layer.SubstanceComponents[Substance.Neon]);
                    layer.SubstanceComponents[Substance.Neon] = ne;
                }
                else if (ne > 0)
                {
                    layer.SubstanceComponents.Add(Substance.Neon, ne);
                }

                n2 -= ne;
                if (layer.ContainsSubstance(Substance.Nitrogen))
                {
                    n2 = Math.Max(n2, layer.SubstanceComponents[Substance.Nitrogen]);
                    layer.SubstanceComponents[Substance.Nitrogen] = n2;
                }
                else if (n2 > 0)
                {
                    layer.SubstanceComponents.Add(Substance.Nitrogen, n2);
                }

                layer.BalanceProportionsForValue(1);
            }
        }

        private void EvaporateWater(float totalWater)
        {
            Layers.Last().SubstanceComponents.Remove(Substance.WaterSalt);
            Layers.Last().SubstanceComponents.Remove(Substance.Water);

            // Include any ice.
            if (Layers.Last().ContainsSubstance(Substance.WaterIce))
            {
                totalWater += Layers.Last().SubstanceComponents[Substance.WaterIce];
                Layers.Last().SubstanceComponents.Remove(Substance.WaterIce);
            }

            // If nothing is left in the hydrosphere, remove it.
            if (Layers.Last().SubstanceComponents.Count == 0)
            {
                Layers.RemoveAt(Layers.Count - 1);
                HasHydrosphere = false;
            }

            var waterVapor = (float)Math.Round(random.NextDouble(0, 0.001), 4);
            Atmosphere.AddSubstance(Substance.Oxygen, (float)Math.Round(waterVapor * 0.0001, 5));
            if (Atmosphere.AtmosphericLayers[0].ContainsSubstance(Substance.WaterVapor))
            {
                Atmosphere.AtmosphericLayers[0].SubstanceComponents[Substance.WaterVapor] =
                    Math.Max(Atmosphere.AtmosphericLayers[0].SubstanceComponents[Substance.WaterVapor], waterVapor);
            }
            else
            {
                Atmosphere.AtmosphericLayers[0].SubstanceComponents.Add(Substance.WaterVapor, waterVapor);
            }
            // Some is added as Oxygen, due to the photodissociation.
            float o2 = (float)Math.Round(waterVapor * 0.0001, 5);
            if (Atmosphere.AtmosphericLayers[0].ContainsSubstance(Substance.Oxygen))
            {
                Atmosphere.AtmosphericLayers[0].SubstanceComponents[Substance.Oxygen] =
                    Math.Max(Atmosphere.AtmosphericLayers[0].SubstanceComponents[Substance.Oxygen], o2);
            }
            else
            {
                Atmosphere.AtmosphericLayers[0].SubstanceComponents.Add(Substance.Oxygen, o2);
            }

            return waterVapor;
        }

        private void CondenseWater(float surfaceTemp, float polarTemp, ref float crustAtmosphereRatio, Mixture surface, float totalWater, float waterVapor, float vaporPressure)
        {
            float ice = 0;
            if (surfaceTemp <= Chemical.Water.MeltingPoint) // Below freezing point; add ice.
            {
                ice = totalWater;

                // A subsurface liquid ocean may persist if it's deep enough.
                if (surface.Proportion >= 0.01)
                {
                    ice = (0.01f / surface.Proportion) * totalWater;
                }

                // No existing water in hydrosphere; add some condensed ice from atmospheric water vapor.
                if (TMath.IsZero(ice))
                {
                    ice = waterVapor / crustAtmosphereRatio;
                }

                if (!TMath.IsZero(ice))
                {
                    if (TMath.IsZero(totalWater)) // Change existing water in hydrosphere to ice.
                    {
                        if (ice < totalWater) // A subsurface ocean is indicated.
                        {
                            var layerProportion = surface.Proportion;

                            // Remaining subsurface ocean is diminished.
                            surface.Proportion -= layerProportion * ice;

                            // New ice layer.
                            Composition.Mixtures.Add(new Mixture(surface.Layer + 1, new MixtureComponent[]
                            {
                                    new MixtureComponent
                                    {
                                        Substance = new Substance(Chemical.Water, Phase.Solid),
                                        Proportion = 1,
                                    },
                            })
                            {
                                Proportion = layerProportion * ice,
                            });
                        }
                        else // No subsurface ocean indicated; entire hydrosphere will be ice.
                        {
                            // Remove any existing water from the hydrosphere.
                            surface.RemoveComponent(Chemical.Water, Phase.Liquid);
                            surface.RemoveComponent(Chemical.Water_Salt, Phase.Liquid);

                            // Nothing but water in the former hydrosphere.
                            if (surface.Components.Count == 0)
                            {
                                // If the previous layer is made entirely of ice, add to it,
                                // and remove the former hydrosphere layer.
                                var subSurface = Composition.GetChildAtLayer(surface.Layer - 1);
                                if (subSurface.Components.Count == 1 &&
                                    subSurface.ContainsSubstance(Chemical.Water, Phase.Solid))
                                {
                                    subSurface.Proportion += surface.Proportion;
                                    Composition.Mixtures.Remove(surface);
                                    surface = subSurface;
                                }
                                // Otherwise the former hydrosphere layer is filled entirely with ice.
                                else
                                {
                                    surface.Components.Add(new MixtureComponent
                                    {
                                        Substance = new Substance(Chemical.Water, Phase.Solid),
                                        Proportion = 1,
                                    });
                                }
                            }
                            // Something besides water left in the hydrosphere (other deposited liquids and ices).
                            else
                            {
                                surface.BalanceProportionsForValue();
                                surface.AddComponent(new Substance(Chemical.Water, Phase.Solid), ice);
                            }
                        }
                    }
                    // No existing water in hydrosphere.
                    else
                    {
                        AddToHydrosphere(new Substance(Chemical.Water, Phase.Solid), ice, ref crustAtmosphereRatio);
                    }
                }
            }
            else // Above freezing point, but also above vapor pressure: liquid water.
            {
                var saltWaterProportion = (float)Math.Round(Randomizer.Static.Normal(0.945, 0.015), 3);

                // Since it isn't below freezing, melt any ice.
                var surfaceIce = surface.GetSubstance(Chemical.Water, Phase.Solid);
                if (surfaceIce != null)
                {
                    ice = surfaceIce.Proportion;
                    surface.RemoveComponent(Chemical.Water, Phase.Solid);

                    var surfaceSaltWater = surface.GetSubstance(Chemical.Water_Salt, Phase.Liquid);
                    if (surfaceSaltWater != null)
                    {
                        surfaceSaltWater.Proportion += ice * saltWaterProportion;
                    }
                    else
                    {
                        surface.Components.Add(new MixtureComponent
                        {
                            Substance = new Substance(Chemical.Water_Salt, Phase.Liquid),
                            Proportion = ice * saltWaterProportion,
                        });
                    }

                    var surfaceWater = surface.GetSubstance(Chemical.Water, Phase.Liquid);
                    if (surfaceWater != null)
                    {
                        surfaceWater.Proportion += ice * (1 - saltWaterProportion);
                    }
                    else
                    {
                        surface.Components.Add(new MixtureComponent
                        {
                            Substance = new Substance(Chemical.Water, Phase.Liquid),
                            Proportion = ice * (1 - saltWaterProportion),
                        });
                    }

                    totalWater += ice;
                    ice = 0;
                }

                // If liquid water is indicated, and the planet doesn't already have water in its
                // hydrosphere, add it.
                if (TMath.IsZero(totalWater))
                {
                    var addedWater = waterVapor / crustAtmosphereRatio;
                    if (!TMath.IsZero(addedWater))
                    {
                        if (HasHydrosphere)
                        {
                            surface.BalanceProportionsForValue(1 - addedWater);
                            surface.Components.Add(new MixtureComponent
                            {
                                Substance = new Substance(Chemical.Water_Salt, Phase.Liquid),
                                Proportion = addedWater * saltWaterProportion,
                            });
                            surface.Components.Add(new MixtureComponent
                            {
                                Substance = new Substance(Chemical.Water, Phase.Liquid),
                                Proportion = addedWater * (1 - saltWaterProportion),
                            });
                        }
                        else
                        {
                            var layerProportion = surface.Proportion * addedWater;
                            surface.Proportion -= layerProportion;
                            Composition.Mixtures.Add(new Mixture(surface.Layer + 1, new MixtureComponent[]
                            {
                                    new MixtureComponent
                                    {
                                        Substance = new Substance(Chemical.Water_Salt, Phase.Liquid),
                                        Proportion = saltWaterProportion,
                                    },
                                    new MixtureComponent
                                    {
                                        Substance = new Substance(Chemical.Water, Phase.Liquid),
                                        Proportion = 1 - saltWaterProportion,
                                    },
                            })
                            {
                                Proportion = layerProportion,
                            });
                            HasHydrosphere = true;
                        }
                    }
                }
            }

            // If no water vapor is present in the atmosphere,
            // generate it based on the hydrosphere.
            if (waterVapor == 0)
            {
                float pressureRatio = Math.Max(float.Epsilon,
                    Math.Min(1, vaporPressure / Atmosphere.AtmosphericPressure));
                // This would represent 100% humidity. Since this is the
                // case, in principle, only at the surface of bodies of
                // water, and should decrease exponentially with altitude,
                // an approximation of 25% average humidity overall is used.
                waterVapor = Math.Max(float.Epsilon, totalWater * pressureRatio);
                waterVapor *= 0.25F;
                if (waterVapor > 0)
                {
                    Atmosphere.AtmosphericLayers[0].AddComponent(Substance.WaterVapor, waterVapor);
                }

                // Also add a corresponding level of oxygen, if it's not already present.
                if (waterVapor > 0 && CanHaveOxygen && !Atmosphere.ContainsSubstance(Substance.Oxygen))
                {
                    Atmosphere.AddSubstance(Substance.Oxygen, (float)Math.Round(waterVapor * 0.0001, 5));
                }
            }

            // Add clouds.
            float clouds = waterVapor * 0.2F;
            if (clouds > 0)
            {
                if (Atmosphere.AtmosphericLayers.Count == 1)
                {
                    Atmosphere.CopyLayer(0, 0.9F);
                }
                // Only snowclouds if the temp is too low; otherwise rainclouds also.
                if (surfaceTemp >= Substance.Water.MeltingPoint)
                {
                    if (Atmosphere.AtmosphericLayers[0].ContainsSubstance(Substance.Water))
                    {
                        Atmosphere.AtmosphericLayers[0].SubstanceComponents[Substance.Water] = clouds * 0.5F;
                    }
                    else
                    {
                        Atmosphere.AtmosphericLayers[0].SubstanceComponents.Add(Substance.Water, clouds * 0.5F);
                    }
                }
                else if (polarTemp < Substance.Water.MeltingPoint)
                {
                    if (Atmosphere.AtmosphericLayers[0].ContainsSubstance(Substance.WaterIce))
                    {
                        Atmosphere.AtmosphericLayers[0].SubstanceComponents[Substance.WaterIce] = clouds * 0.5F;
                    }
                    else
                    {
                        Atmosphere.AtmosphericLayers[0].SubstanceComponents.Add(Substance.WaterIce, clouds * 0.5F);
                    }
                }
            }
        }

        private void CondenseAtmosphere(Chemical chemical, float surfaceTemp, float polarTemp, ref float crustAtmosphereRatio)
        {
            var gasProportion = Atmosphere.GetSubstanceProportionInAllChildren(chemical, Phase.Gas);
            var troposphere = Atmosphere.GetChildAtFirstLayer();

            // Generate clouds.
            var cloudProportion = gasProportion * 0.2f;
            // Snow clouds alone if the temp is below freezing; otherwise rainclouds also.
            if (surfaceTemp >= chemical.MeltingPoint)
            {
                // Separate troposphere from upper atmosphere if undifferentiated.
                if (Atmosphere.Mixtures.Count == 1)
                {
                    Atmosphere.CopyLayer(0, 0.2f);
                }

                troposphere.AddComponent(new Substance(chemical, Phase.Liquid), cloudProportion * 0.5f);
            }
            troposphere.AddComponent(new Substance(chemical, Phase.Solid), cloudProportion * 0.5f);

            // Freezing point at 1 atm; doesn't change enough at typical terrestrial pressures to make further accuracy necessary.
            float iceProportion = 0;
            if (surfaceTemp < chemical.MeltingPoint)
            {
                iceProportion = gasProportion / crustAtmosphereRatio;
                if (iceProportion > 0)
                {
                    AddToHydrosphere(new Substance(chemical, Phase.Solid), iceProportion, ref crustAtmosphereRatio);
                }
            }
            else
            {
                // Create icecaps.
                if (polarTemp < chemical.MeltingPoint)
                {
                    iceProportion = (gasProportion / crustAtmosphereRatio) * 0.28f;
                    if (!TMath.IsZero(iceProportion))
                    {
                        AddToHydrosphere(new Substance(chemical, Phase.Solid), iceProportion, ref crustAtmosphereRatio);
                    }
                }

                var liquidProportion = (gasProportion / crustAtmosphereRatio) - iceProportion;
                if (!TMath.IsZero(liquidProportion))
                {
                    AddToHydrosphere(new Substance(chemical, Phase.Liquid), liquidProportion, ref crustAtmosphereRatio);
                }
            }
        }

        private float GetCrustAtmosphereRatio(Mixture surface) => (float)Math.Max(1, (surface.Proportion * Mass) / Atmosphere.AtmosphericMass);

        /// <summary>
        /// Calculates the mass required to produce the given surface gravity.
        /// </summary>
        /// <param name="gravity">The desired surface gravity, in kg/m².</param>
        /// <returns>The mass required to produce the given surface gravity, in kg.</returns>
        private double GetMassForSurfaceGravity(float gravity) => (gravity * Math.Pow(Radius, 2)) / Utilities.Science.Constants.G;

        /// <summary>
        /// Calculates the radius required to produce the given surface gravity.
        /// </summary>
        /// <param name="gravity">The desired surface gravity, in kg/m².</param>
        /// <returns>The radius required to produce the given surface gravity, in meters.</returns>
        public float GetRadiusForSurfaceGravity(float gravity) => (float)((gravity * Utilities.MathUtil.Constants.FourThirdsPI) / (Utilities.Science.Constants.G * Density));
    }
}
