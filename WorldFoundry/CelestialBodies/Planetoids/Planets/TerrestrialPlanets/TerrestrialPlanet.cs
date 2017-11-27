using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Troschuetz.Random;
using WorldFoundry.Climate;
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

        private Mixture _hydrosphere;
        /// <summary>
        /// This planet's surface liquids and ices (not necessarily water).
        /// </summary>
        /// <remarks>
        /// Represented as a separate <see cref="Mixture"/> rather than as a top layer of <see
        /// cref="Planetoid.Composition"/> for ease of reference.
        /// </remarks>
        public Mixture Hydrosphere
        {
            get => GetProperty(ref _hydrosphere, GenerateHydrosphere);
            protected set => _hydrosphere = value;
        }

        /// <summary>
        /// Used to set the proportionate amount of metal in the composition of a terrestrial planet.
        /// </summary>
        protected virtual float MetalProportion => 0.05f;

        private float? _surfaceAlbedo;
        /// <summary>
        /// Since the total albedo of a terrestrial planet may change based on surface ice and cloud
        /// cover, the base surface albedo is maintained separately.
        /// </summary>
        public float SurfaceAlbedo
        {
            get => GetProperty(ref _surfaceAlbedo, GenerateAlbedo) ?? 0;
            internal set => _surfaceAlbedo = value;
        }

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

        private void CalculateGasPhaseMix(Chemical chemical, float surfaceTemp, float polarTemp, ref float hydrosphereAtmosphereRatio)
        {
            // Chemical will boil away.
            if (surfaceTemp > chemical.AntoineMaximumTemperature ||
                (surfaceTemp > chemical.AntoineMinimumTemperature &&
                Atmosphere.AtmosphericPressure > chemical.CalculateVaporPressure(surfaceTemp)))
            {
                float gasProportion = Hydrosphere.GetProportion(chemical, Phase.Any);
                if (!TMath.IsZero(gasProportion))
                {
                    Hydrosphere.RemoveComponent(chemical, Phase.Any);
                    Hydrosphere.Proportion -= Hydrosphere.Proportion * gasProportion;
                    hydrosphereAtmosphereRatio = GetHydrosphereAtmosphereRatio();
                    Atmosphere.SetProportion(chemical, Phase.Gas, gasProportion * hydrosphereAtmosphereRatio, true);
                    Atmosphere.ResetPressureDependentProperties();
                }
            }
            // If the gas is present in the atmosphere and it will not boil away, it will condense,
            // and may freeze.
            else if (Atmosphere.ContainsSubstance(chemical, Phase.Any))
            {
                CondenseAtmosphere(chemical, surfaceTemp, polarTemp, ref hydrosphereAtmosphereRatio);
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
        private void CalculatePhases(int counter, ref float hydrosphereAtmosphereRatio)
        {
            var surfaceTemp = Atmosphere.GetSurfaceTemperatureAverageOrbital();
            var polarTemp = Atmosphere.GetSurfaceTemperatureAverageOrbital(true);

            CalculateGasPhaseMix(Chemical.Methane, surfaceTemp, polarTemp, ref hydrosphereAtmosphereRatio);

            CalculateGasPhaseMix(Chemical.CarbonMonoxide, surfaceTemp, polarTemp, ref hydrosphereAtmosphereRatio);

            CalculateGasPhaseMix(Chemical.CarbonDioxide, surfaceTemp, polarTemp, ref hydrosphereAtmosphereRatio);

            CalculateGasPhaseMix(Chemical.Nitrogen, surfaceTemp, polarTemp, ref hydrosphereAtmosphereRatio);

            CalculateGasPhaseMix(Chemical.Oxygen, surfaceTemp, polarTemp, ref hydrosphereAtmosphereRatio);

            // No need to check for ozone, since it is only added to
            // atmospheres on planets with liquid surface water, which means
            // temperatures too high for liquid or solid ozone.

            CalculateGasPhaseMix(Chemical.SulphurDioxide, surfaceTemp, polarTemp, ref hydrosphereAtmosphereRatio);

            // Water is handled differently, since the planet may already have surface water.
            if (Hydrosphere.ContainsSubstance(Chemical.Water, Phase.Any)
                || Hydrosphere.ContainsSubstance(Chemical.Water_Salt, Phase.Any)
                || Atmosphere.ContainsSubstance(Chemical.Water, Phase.Any))
            {
                CalculateWaterPhaseMix(surfaceTemp, polarTemp, hydrosphereAtmosphereRatio);
            }

            var oldAlbedo = Albedo;

            // Ices significantly impact albedo.
            if (Hydrosphere.Components.Count > 0)
            {
                var iceAmount = Math.Min(1, Hydrosphere.Components
                    .Where(x => x.Phase == Phase.Solid)
                    .Sum(x => x.Proportion));
                Albedo = (SurfaceAlbedo * (1.0f - iceAmount)) + (0.9f * iceAmount);
            }

            // Clouds also impact albedo.
            float cloudCover = Math.Min(1, Atmosphere.AtmosphericPressure
                * Atmosphere.GetChildAtFirstLayer().Components
                .Where(x => x.Phase == Phase.Solid || x.Phase == Phase.Liquid)
                .Sum(s => s.Proportion) / 100.0f);
            Albedo = (SurfaceAlbedo * (1.0f - cloudCover)) + (0.9f * cloudCover);

            // An albedo change might significantly change surface temperature, which may require a
            // re-calculation (but not too many). 5K is used as the threshold for re-calculation,
            // which may lead to some inaccuracies, but should avoid over-repeating for small changes.
            if (counter < 10 && Albedo != oldAlbedo &&
                Math.Abs(surfaceTemp - Atmosphere.GetSurfaceTemperatureAverageOrbital()) > 5)
            {
                CalculatePhases(counter + 1, ref hydrosphereAtmosphereRatio);
            }
        }

        private void CalculateWaterPhaseMix(float surfaceTemp, float polarTemp, float hydrosphereAtmosphereRatio)
        {
            var water = Hydrosphere.GetSubstance(Chemical.Water, Phase.Liquid)?.Proportion ?? 0;
            var saltWater = Hydrosphere.GetSubstance(Chemical.Water_Salt, Phase.Liquid)?.Proportion ?? 0;
            var totalWater = water + saltWater;

            var waterVapor = Atmosphere.GetProportion(Chemical.Water, Phase.Gas, true);

            var vaporPressure = Chemical.Water.CalculateVaporPressure(surfaceTemp);

            if (surfaceTemp < Chemical.Water.AntoineMinimumTemperature ||
                (surfaceTemp <= Chemical.Water.AntoineMaximumTemperature &&
                Atmosphere.AtmosphericPressure > vaporPressure))
            {
                CondenseWater(surfaceTemp, polarTemp, totalWater, waterVapor, vaporPressure, ref hydrosphereAtmosphereRatio);
            }
            // This indicates that all water will boil off. If this is true,
            // it is presumed that photodissociation will eventually reduce the amount
            // of water vapor to a trace gas (the H2 will be lost due to atmospheric
            // escape, and the oxygen will be lost to surface oxidation).
            else if (totalWater > 0 || Hydrosphere.ContainsSubstance(Chemical.Water, Phase.Any))
            {
                EvaporateWater(ref hydrosphereAtmosphereRatio);
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

        private void CondenseAtmosphere(Chemical chemical, float surfaceTemp, float polarTemp, ref float hydrosphereAtmosphereRatio)
        {
            var gasProportion = Atmosphere.GetProportion(chemical, Phase.Any, true);
            float iceProportion = 0;

            // Fully precipitate out of the atmosphere as surface ice.
            // Freezing point at 1 ATM; doesn't change enough at typical terrestrial pressures to make further accuracy necessary.
            if (surfaceTemp <= chemical.MeltingPoint)
            {
                iceProportion = gasProportion / hydrosphereAtmosphereRatio;
                if (!TMath.IsZero(iceProportion))
                {
                    SetHydrosphereProportion(chemical, Phase.Solid, iceProportion, ref hydrosphereAtmosphereRatio);
                }
                Atmosphere.RemoveComponent(chemical, Phase.Any, true);
                Atmosphere.RemoveEmptyChildren();
                if (Atmosphere.Mixtures.Count == 0)
                {
                    Atmosphere.Mixtures.Add(new Mixture());
                    Atmosphere.ResetPressureDependentProperties();
                }
            }
            else
            {
                // Create icecaps.
                if (polarTemp <= chemical.MeltingPoint)
                {
                    iceProportion = (gasProportion / hydrosphereAtmosphereRatio) * 0.28f;
                    if (!TMath.IsZero(iceProportion))
                    {
                        SetHydrosphereProportion(chemical, Phase.Solid, iceProportion, ref hydrosphereAtmosphereRatio);
                    }
                }

                var troposphere = Atmosphere.GetChildAtFirstLayer();
                // Separate troposphere from upper atmosphere if undifferentiated.
                if (Atmosphere.Mixtures.Count == 1)
                {
                    Atmosphere.CopyLayer(0, 0.2f);
                }

                // Generate clouds.
                var cloudProportion = gasProportion * 0.2f;
                if (polarTemp <= chemical.MeltingPoint)
                {
                    var halfCloudProportion = cloudProportion / 2;
                    troposphere.AddComponent(chemical, Phase.Liquid, halfCloudProportion);
                    troposphere.AddComponent(chemical, Phase.Solid, halfCloudProportion);
                }
                else
                {
                    troposphere.AddComponent(chemical, Phase.Liquid, cloudProportion);
                }
                Atmosphere.ResetPressureDependentProperties();

                var liquidProportion = (gasProportion / hydrosphereAtmosphereRatio) - iceProportion;
                if (!TMath.IsZero(liquidProportion))
                {
                    SetHydrosphereProportion(chemical, Phase.Liquid, liquidProportion, ref hydrosphereAtmosphereRatio);
                }
            }
        }

        private void CondenseWater(float surfaceTemp, float polarTemp, float totalWater, float waterVapor, float vaporPressure, ref float hydrosphereAtmosphereRatio)
        {
            if (surfaceTemp <= Chemical.Water.MeltingPoint) // Below freezing point; add ice.
            {
                CondenseWaterIce(totalWater, waterVapor, ref hydrosphereAtmosphereRatio);
            }
            else // Above freezing point, but also above vapor pressure; add liquid water.
            {
                CondenseWaterLiquid(polarTemp, ref totalWater, waterVapor, hydrosphereAtmosphereRatio);
            }

            // If no water vapor is present in the atmosphere, generate it based on the hydrosphere.
            if (TMath.IsZero(waterVapor))
            {
                var pressureRatio = Math.Max(float.Epsilon, Math.Min(1, vaporPressure / Atmosphere.AtmosphericPressure));
                // This would represent 100% humidity. Since this is the case, in principle, only at
                // the surface of bodies of water, and should decrease exponentially with altitude,
                // an approximation of 25% average humidity overall is used.
                waterVapor = Math.Max(float.Epsilon, totalWater * pressureRatio);
                waterVapor *= 0.25f;
                if (!TMath.IsZero(waterVapor))
                {
                    Atmosphere.AddComponent(Chemical.Water, Phase.Gas, waterVapor, true);

                    // Also add a corresponding amount of oxygen, if it's not already present.
                    if (CanHaveOxygen)
                    {
                        Atmosphere.SetProportion(Chemical.Oxygen, Phase.Gas, Math.Max(Atmosphere.GetProportion(Chemical.Oxygen, Phase.Gas, true), (float)Math.Round(waterVapor * 0.0001, 5)), true);
                    }

                    Atmosphere.ResetPressureDependentProperties();
                }
            }

            // Add clouds.
            var troposphere = Atmosphere.GetChildAtFirstLayer();
            var clouds = waterVapor * 0.2f;
            if (!TMath.IsZero(clouds))
            {
                // Separate troposphere from upper atmosphere if undifferentiated.
                if (Atmosphere.Mixtures.Count == 1)
                {
                    Atmosphere.CopyLayer(0, 0.2f);
                }

                if (surfaceTemp <= Chemical.Water.MeltingPoint)
                {
                    troposphere.SetProportion(Chemical.Water, Phase.Solid, clouds);
                }
                else if (polarTemp < Chemical.Water.MeltingPoint)
                {
                    var halfClouds = clouds / 2;
                    troposphere.SetProportion(Chemical.Water, Phase.Liquid, halfClouds);
                    troposphere.SetProportion(Chemical.Water, Phase.Solid, halfClouds);
                }
                else
                {
                    troposphere.SetProportion(Chemical.Water, Phase.Liquid, clouds);
                }

                Atmosphere.ResetPressureDependentProperties();
            }
        }

        private void CondenseWaterIce(float totalWater, float waterVapor, ref float hydrosphereAtmosphereRatio)
        {
            float ice = totalWater;

            // A subsurface liquid ocean may persist if it's deep enough.
            if (Hydrosphere.Proportion >= 0.01)
            {
                ice = (0.01f / Hydrosphere.Proportion) * totalWater;
            }

            // No existing water in hydrosphere; add some condensed ice from atmospheric water vapor.
            if (TMath.IsZero(ice))
            {
                ice = waterVapor / hydrosphereAtmosphereRatio;
            }

            if (TMath.IsZero(ice))
            {
                return;
            }

            if (TMath.IsZero(totalWater)) // Change existing water in hydrosphere to ice.
            {
                if (ice < totalWater) // A subsurface ocean is indicated.
                {
                    SetHydrosphereProportion(Chemical.Water, Phase.Solid, ice, ref hydrosphereAtmosphereRatio);
                }
                else // No subsurface ocean indicated; entire hydrosphere will be ice.
                {
                    // Remove any existing water from the hydrosphere.
                    Hydrosphere.RemoveComponent(Chemical.Water, Phase.Liquid);
                    Hydrosphere.RemoveComponent(Chemical.Water_Salt, Phase.Liquid);

                    // Nothing but water in the former hydrosphere.
                    if (Hydrosphere.Components.Count == 0)
                    {
                        Hydrosphere.AddComponent(Chemical.Water, Phase.Solid, 1);
                    }
                    // Something besides water left in the hydrosphere (other deposited liquids and ices).
                    else
                    {
                        Hydrosphere.AddComponent(Chemical.Water, Phase.Solid, ice);
                    }
                    hydrosphereAtmosphereRatio = GetHydrosphereAtmosphereRatio();
                }
            }
            // No existing water in hydrosphere.
            else
            {
                SetHydrosphereProportion(Chemical.Water, Phase.Solid, ice, ref hydrosphereAtmosphereRatio);
            }
        }

        private void CondenseWaterLiquid(float polarTemp, ref float totalWater, float waterVapor, float hydrosphereAtmosphereRatio)
        {
            // Create icecaps.
            var surfaceIce = Hydrosphere.GetProportion(Chemical.Water, Phase.Solid)
                + Hydrosphere.GetProportion(Chemical.Water_Salt, Phase.Solid);
            float iceCaps = 0;
            var meltingIce = surfaceIce;
            if (polarTemp <= Chemical.Water.MeltingPoint)
            {
                iceCaps = totalWater * 0.28f;
                meltingIce = Math.Max(0, surfaceIce - iceCaps);
                if (iceCaps > surfaceIce)
                {
                    totalWater -= iceCaps - surfaceIce;
                }
            }
            var saltWaterProportion = (float)Math.Round(Randomizer.Static.Normal(0.945, 0.015), 3);
            SetHydrosphereProportion(Chemical.Water, Phase.Solid, iceCaps * (1 - saltWaterProportion), ref hydrosphereAtmosphereRatio);
            SetHydrosphereProportion(Chemical.Water_Salt, Phase.Solid, iceCaps * saltWaterProportion, ref hydrosphereAtmosphereRatio);

            // Since the entire surface isn't below freezing, melt surface ice (aside from icecaps).
            if (!TMath.IsZero(meltingIce))
            {
                SetHydrosphereProportion(Chemical.Water_Salt, Phase.Liquid, meltingIce * saltWaterProportion, ref hydrosphereAtmosphereRatio);
                SetHydrosphereProportion(Chemical.Water, Phase.Liquid, meltingIce * (1 - saltWaterProportion), ref hydrosphereAtmosphereRatio);

                totalWater += meltingIce;
            }

            // If liquid water is indicated but the hydrosphere doesn't have any, add it.
            if (TMath.IsZero(totalWater))
            {
                var addedWater = waterVapor / hydrosphereAtmosphereRatio;
                if (!TMath.IsZero(addedWater))
                {
                    SetHydrosphereProportion(Chemical.Water_Salt, Phase.Liquid, addedWater * saltWaterProportion, ref hydrosphereAtmosphereRatio);
                    SetHydrosphereProportion(Chemical.Water, Phase.Liquid, addedWater * (1 - saltWaterProportion), ref hydrosphereAtmosphereRatio);
                }
            }
        }

        private void EvaporateWater(ref float hydrosphereAtmosphereRatio)
        {
            SetHydrosphereProportion(Chemical.Water, Phase.Any, 0, ref hydrosphereAtmosphereRatio);
            SetHydrosphereProportion(Chemical.Water_Salt, Phase.Any, 0, ref hydrosphereAtmosphereRatio);

            var waterVapor = (float)Math.Round(Randomizer.Static.NextDouble(0.001), 4);
            var o2 = (float)Math.Round(waterVapor * 0.0001, 5);
            foreach (var layer in Atmosphere.Mixtures)
            {
                layer.SetProportion(Chemical.Water, Phase.Gas, Math.Max(layer.GetProportion(Chemical.Water, Phase.Gas), waterVapor));

                // Some is added as Oxygen, due to photodissociation.
                if (CanHaveOxygen)
                {
                    layer.SetProportion(Chemical.Oxygen, Phase.Gas, Math.Max(layer.GetProportion(Chemical.Oxygen, Phase.Gas, true), o2));
                }
            }
            Atmosphere.ResetPressureDependentProperties();
        }

        /// <summary>
        /// Determines an albedo for this <see cref="CelestialBody"/> (a value between 0 and 1).
        /// </summary>
        /// <remarks>
        /// Also sets <see cref="SurfaceAlbedo"/> for terrestrial planets.
        /// </remarks>
        protected override void GenerateAlbedo()
        {
            Albedo = (float)Math.Round(Randomizer.Static.NextDouble(0.1, 0.6), 2);
            SurfaceAlbedo = Albedo;
        }

        /// <summary>
        /// Generates an atmosphere for this <see cref="Planetoid"/>.
        /// </summary>
        protected override void GenerateAtmosphere()
        {
            var surfaceTemp = GetTotalTemperatureAverageOrbital();

            // If the planet is not massive enough or too hot to hold onto carbon dioxide gas, it is
            // presumed that it will have a minimal atmosphere of outgassed volatiles (comparable to Mercury).
            var escapeVelocity = Math.Sqrt((Utilities.Science.Constants.TwoG * Mass) / Radius);
            if (Math.Sqrt(566.6137 * surfaceTemp) >= 0.2 * escapeVelocity)
            {
                GenerateAtmosphereTrace(surfaceTemp);
            }
            else
            {
                GenerateAtmosphereThick(surfaceTemp);
            }

            float hydrosphereAtmosphereRatio = GetHydrosphereAtmosphereRatio();
            // Water may be removed, or if not may remove CO2 from the atmosphere, depending on
            // planetary conditions.
            if (Hydrosphere.ContainsSubstance(Chemical.Water, Phase.Any)
                || Hydrosphere.ContainsSubstance(Chemical.Water_Salt, Phase.Any)
                || Atmosphere.ContainsSubstance(Chemical.Water, Phase.Any))
            {
                CalculateWaterPhaseMix(surfaceTemp, surfaceTemp, hydrosphereAtmosphereRatio);
            }

            GenerateLife();

            CalculatePhases(0, ref hydrosphereAtmosphereRatio);

            // If the adjustments have led to the loss of liquid water, then there is no life after
            // all (this may be interpreted as a world which once supported life, but became
            // inhospitable due to the environmental changes that life produced).
            if (!IsHabitable())
            {
                HasBiosphere = false;
            }
        }

        private void GenerateAtmosphereThick(float surfaceTemp)
        {
            float pressure;
            if ((habitabilityRequirements?.MinimumSurfacePressure.HasValue ?? false)
                || (habitabilityRequirements?.MaximumSurfacePressure.HasValue ?? false))
            {
                // If there is a minimum but no maximum, a half-Gaussian distribution with the minimum as both mean and the basis for the sigma is used.
                if (!habitabilityRequirements.Value.MaximumSurfacePressure.HasValue)
                {
                    pressure = (float)Math.Abs(Randomizer.Static.Normal(0, habitabilityRequirements.Value.MinimumSurfacePressure.Value / 3))
                        + habitabilityRequirements.Value.MinimumSurfacePressure.Value;
                }
                else
                {
                    pressure = (float)Randomizer.Static.NextDouble(habitabilityRequirements.Value.MinimumSurfacePressure ?? 0, habitabilityRequirements.Value.MaximumSurfacePressure.Value);
                }
            }
            else
            {
                double factor;
                // Low-gravity planets without magnetospheres are less likely to hold onto the bulk
                // of their atmospheres over long periods.
                if (Mass >= 1.5e24 || HasMagnetosphere)
                {
                    factor = Mass / 1.8e5;
                }
                else
                {
                    factor = Mass / 1.2e6;
                }

                var mass = Math.Max(factor, Randomizer.Static.Lognormal(0, factor * 4));
                pressure = (float)((mass * SurfaceGravity) / (1000 * Utilities.MathUtil.Constants.FourPI * Math.Pow(Radius, 2)));
            }

            // For terrestrial (non-giant) planets, these gases remain at low concentrations due to
            // atmospheric escape.
            var h = (float)Math.Round(Randomizer.Static.NextDouble(0.5e-7, 0.2e-6), 4);
            var he = (float)Math.Round(Randomizer.Static.NextDouble(0.26e-6, 1.0e-5), 4);

            // 50% chance not to have these components at all.
            var ch4 = (float)Math.Max(0, Math.Round(Randomizer.Static.NextDouble(-0.5, 0.5), 4));
            var traceTotal = ch4;

            var co = (float)Math.Max(0, Math.Round(Randomizer.Static.NextDouble(-0.5, 0.5), 4));
            traceTotal += co;

            var so2 = (float)Math.Max(0, Math.Round(Randomizer.Static.NextDouble(-0.5, 0.5), 4));
            traceTotal += so2;

            var trace = TMath.IsZero(traceTotal) ? 0 : (float)Randomizer.Static.NextDouble(1.5e-4, 2.5e-3);
            var traceRatio = TMath.IsZero(traceTotal) ? 0 : trace / traceTotal;
            ch4 *= traceRatio;
            co *= traceRatio;
            so2 *= traceRatio;

            // CO2 makes up the bulk of a thick atmosphere by default (although the presence of water
            // may change this later).
            var co2 = (float)Math.Round(Randomizer.Static.NextDouble(0.97, 0.99) - trace, 4);

            // If there is water on the surface, the water in the air will be determined based on
            // vapor pressure later, and should not be randomly assigned. Otherwise, there is a small
            // chance of water vapor without significant surface water (results of cometary deposits, etc.)
            float waterVapor = 0;
            var surfaceWater = Hydrosphere.ContainsSubstance(Chemical.Water, Phase.Any)
                || Hydrosphere.ContainsSubstance(Chemical.Water_Salt, Phase.Any);
            if (CanHaveWater && !surfaceWater)
            {
                waterVapor = (float)Math.Max(0, Math.Round(Randomizer.Static.NextDouble(-0.05, 0.001), 4));
            }

            // Always at least some oxygen if there is water, planetary composition allowing
            float o2 = 0;
            if (CanHaveOxygen)
            {
                if (!TMath.IsZero(waterVapor))
                {
                    o2 = waterVapor * 0.0001f;
                }
                else if (surfaceWater)
                {
                    o2 = (float)Math.Round(Randomizer.Static.NextDouble(0.002), 5);
                }
            }

            // N2 (largely inert gas) comprises whatever is left after the other components have been
            // determined. This is usually a trace amount, unless CO2 has been reduced to a trace, in
            // which case it will comprise the bulk of the atmosphere.
            var n2 = 1 - (h + he + co2 + waterVapor + o2 + trace);

            // Some portion of the N2 may be Ar instead.
            var ar = (float)Math.Max(0, n2 * Randomizer.Static.NextDouble(-0.02, 0.04));
            n2 -= ar;
            // An even smaller fraction may be Kr.
            var kr = (float)Math.Max(0, n2 * Randomizer.Static.NextDouble(-2.5e-4, 5.0e-4));
            n2 -= kr;
            // An even smaller fraction may be Xe or Ne.
            var xe = (float)Math.Max(0, n2 * Randomizer.Static.NextDouble(-1.8e-5, 3.5e-5));
            n2 -= xe;
            var ne = (float)Math.Max(0, n2 * Randomizer.Static.NextDouble(-1.8e-5, 3.5e-5));
            n2 -= ne;

            Atmosphere = new Atmosphere(this, pressure)
            {
                Mixtures = new HashSet<Mixture>()
            };
            var firstLayer = new Mixture(new MixtureComponent[]
            {
                new MixtureComponent
                {
                    Chemical = Chemical.CarbonDioxide,
                    Phase = Phase.Gas,
                    Proportion = co2,
                },
                new MixtureComponent
                {
                    Chemical = Chemical.Helium,
                    Phase = Phase.Gas,
                    Proportion = he,
                },
                new MixtureComponent
                {
                    Chemical = Chemical.Hydrogen,
                    Phase = Phase.Gas,
                    Proportion = h,
                },
                new MixtureComponent
                {
                    Chemical = Chemical.Nitrogen,
                    Phase = Phase.Gas,
                    Proportion = n2,
                },
            })
            {
                Components = new HashSet<MixtureComponent>(),
                Proportion = 1,
            };
            if (ar > 0)
            {
                firstLayer.Components.Add(new MixtureComponent
                {
                    Chemical = Chemical.Argon,
                    Phase = Phase.Gas,
                    Proportion = ar,
                });
            }
            if (co > 0)
            {
                firstLayer.Components.Add(new MixtureComponent
                {
                    Chemical = Chemical.CarbonMonoxide,
                    Phase = Phase.Gas,
                    Proportion = co,
                });
            }
            if (kr > 0)
            {
                firstLayer.Components.Add(new MixtureComponent
                {
                    Chemical = Chemical.Krypton,
                    Phase = Phase.Gas,
                    Proportion = kr,
                });
            }
            if (ch4 > 0)
            {
                firstLayer.Components.Add(new MixtureComponent
                {
                    Chemical = Chemical.Methane,
                    Phase = Phase.Gas,
                    Proportion = ch4,
                });
            }
            if (o2 > 0)
            {
                firstLayer.Components.Add(new MixtureComponent
                {
                    Chemical = Chemical.Oxygen,
                    Phase = Phase.Gas,
                    Proportion = o2,
                });
            }
            if (so2 > 0)
            {
                firstLayer.Components.Add(new MixtureComponent
                {
                    Chemical = Chemical.SulphurDioxide,
                    Phase = Phase.Gas,
                    Proportion = so2,
                });
            }
            if (waterVapor > 0)
            {
                firstLayer.Components.Add(new MixtureComponent
                {
                    Chemical = Chemical.Water,
                    Phase = Phase.Gas,
                    Proportion = waterVapor,
                });
            }
            if (xe > 0)
            {
                firstLayer.Components.Add(new MixtureComponent
                {
                    Chemical = Chemical.Xenon,
                    Phase = Phase.Gas,
                    Proportion = xe,
                });
            }
            Atmosphere.Mixtures.Add(firstLayer);
        }

        private void GenerateAtmosphereTrace(float surfaceTemp)
        {
            float pressure;
            if ((habitabilityRequirements?.MinimumSurfacePressure.HasValue ?? false)
                || (habitabilityRequirements?.MaximumSurfacePressure.HasValue ?? false))
            {
                // If there is a minimum but no maximum, a half-Gaussian distribution with the minimum as both mean and the basis for the sigma is used.
                if (!habitabilityRequirements.Value.MaximumSurfacePressure.HasValue)
                {
                    pressure = (float)Math.Abs(Randomizer.Static.Normal(0, habitabilityRequirements.Value.MinimumSurfacePressure.Value / 3))
                        + habitabilityRequirements.Value.MinimumSurfacePressure.Value;
                }
                else
                {
                    pressure = (float)Randomizer.Static.NextDouble(habitabilityRequirements.Value.MinimumSurfacePressure ?? 0, habitabilityRequirements.Value.MaximumSurfacePressure.Value);
                }
            }
            else
            {
                pressure = (float)Math.Round(Randomizer.Static.NextDouble(25));
            }

            // For terrestrial (non-giant) planets, these gases remain at low concentrations due to
            // atmospheric escape.
            var h = (float)Math.Round(Randomizer.Static.NextDouble(0.5e-7, 0.2e-6), 4);
            var he = (float)Math.Round(Randomizer.Static.NextDouble(0.26e-6, 1.0e-5), 4);

            // 50% chance not to have these components at all.
            var ch4 = (float)Math.Max(0, Math.Round(Randomizer.Static.NextDouble(-0.5, 0.5), 4));
            var total = ch4;

            var co = (float)Math.Max(0, Math.Round(Randomizer.Static.NextDouble(-0.5, 0.5), 4));
            total += co;

            var so2 = (float)Math.Max(0, Math.Round(Randomizer.Static.NextDouble(-0.5, 0.5), 4));
            total += so2;

            var n2 = (float)Math.Max(0, Math.Round(Randomizer.Static.NextDouble(-0.5, 0.5), 4));
            total += n2;

            // Noble traces: selected as fractions of N2, if present, to avoid over-representation.
            var ar = n2 > 0 ? (float)Math.Max(0, n2 * Randomizer.Static.NextDouble(-0.02, 0.04)) : 0;
            n2 -= ar;
            var kr = n2 > 0 ? (float)Math.Max(0, n2 * Randomizer.Static.NextDouble(-0.02, 0.04)) : 0;
            n2 -= kr;
            var xe = n2 > 0 ? (float)Math.Max(0, n2 * Randomizer.Static.NextDouble(-0.02, 0.04)) : 0;
            n2 -= xe;

            // Carbon monoxide means at least some carbon dioxide, as well.
            var co2 = (float)Math.Round(co > 0
                ? Randomizer.Static.NextDouble(0.5)
                : Math.Max(0, Randomizer.Static.NextDouble(-0.5, 0.5)),
                4);
            total += co2;

            // If there is water on the surface, the water in the air will be determined based on
            // vapor pressure later, and should not be randomly assigned. Otherwise, there is a small
            // chance of water vapor without significant surface water (results of cometary deposits, etc.)
            float waterVapor = 0;
            if (CanHaveWater
                && !Hydrosphere.ContainsSubstance(Chemical.Water, Phase.Any)
                && !Hydrosphere.ContainsSubstance(Chemical.Water_Salt, Phase.Any))
            {
                waterVapor = (float)Math.Max(0, Math.Round(Randomizer.Static.NextDouble(-0.05, 0.001), 4));
            }
            total += waterVapor;

            float o2 = 0;
            if (CanHaveOxygen)
            {
                // Always at least some oxygen if there is water, planetary composition allowing
                o2 = waterVapor > 0
                    ? waterVapor * 0.0001f
                    : (float)Math.Max(0, Math.Round(Randomizer.Static.NextDouble(-0.5, 0.5), 4));
            }
            total += o2;

            var ratio = (1 - h - he) / total;
            ch4 *= ratio;
            co *= ratio;
            so2 *= ratio;
            n2 *= ratio;
            ar *= ratio;
            kr *= ratio;
            xe *= ratio;
            co2 *= ratio;
            waterVapor *= ratio;
            o2 *= ratio;

            // H and He are always assumed to be present in small amounts if a planet has any
            // atmosphere, but without any other gases making up the bulk of the atmosphere, they are
            // presumed lost to atmospheric escape entirely, and no atmosphere at all is indicated.
            if (TMath.IsZero(total))
            {
                Atmosphere = new Atmosphere(this, 0);
            }
            else
            {
                Atmosphere = new Atmosphere(this, pressure)
                {
                    Mixtures = new HashSet<Mixture>()
                };
                var firstLayer = new Mixture(new MixtureComponent[]
                {
                    new MixtureComponent
                    {
                        Chemical = Chemical.Helium,
                        Phase = Phase.Gas,
                        Proportion = he,
                    },
                    new MixtureComponent
                    {
                        Chemical = Chemical.Hydrogen,
                        Phase = Phase.Gas,
                        Proportion = h,
                    },
                })
                {
                    Components = new HashSet<MixtureComponent>(),
                    Proportion = 1,
                };
                if (ar > 0)
                {
                    firstLayer.Components.Add(new MixtureComponent
                    {
                        Chemical = Chemical.Argon,
                        Phase = Phase.Gas,
                        Proportion = ar,
                    });
                }
                if (co2 > 0)
                {
                    firstLayer.Components.Add(new MixtureComponent
                    {
                        Chemical = Chemical.CarbonDioxide,
                        Phase = Phase.Gas,
                        Proportion = co2,
                    });
                }
                if (co > 0)
                {
                    firstLayer.Components.Add(new MixtureComponent
                    {
                        Chemical = Chemical.CarbonMonoxide,
                        Phase = Phase.Gas,
                        Proportion = co,
                    });
                }
                if (kr > 0)
                {
                    firstLayer.Components.Add(new MixtureComponent
                    {
                        Chemical = Chemical.Krypton,
                        Phase = Phase.Gas,
                        Proportion = kr,
                    });
                }
                if (ch4 > 0)
                {
                    firstLayer.Components.Add(new MixtureComponent
                    {
                        Chemical = Chemical.Methane,
                        Phase = Phase.Gas,
                        Proportion = ch4,
                    });
                }
                if (n2 > 0)
                {
                    firstLayer.Components.Add(new MixtureComponent
                    {
                        Chemical = Chemical.Nitrogen,
                        Phase = Phase.Gas,
                        Proportion = n2,
                    });
                }
                if (o2 > 0)
                {
                    firstLayer.Components.Add(new MixtureComponent
                    {
                        Chemical = Chemical.Oxygen,
                        Phase = Phase.Gas,
                        Proportion = o2,
                    });
                }
                if (so2 > 0)
                {
                    firstLayer.Components.Add(new MixtureComponent
                    {
                        Chemical = Chemical.SulphurDioxide,
                        Phase = Phase.Gas,
                        Proportion = so2,
                    });
                }
                if (waterVapor > 0)
                {
                    firstLayer.Components.Add(new MixtureComponent
                    {
                        Chemical = Chemical.Water,
                        Phase = Phase.Gas,
                        Proportion = waterVapor,
                    });
                }
                if (xe > 0)
                {
                    firstLayer.Components.Add(new MixtureComponent
                    {
                        Chemical = Chemical.Xenon,
                        Phase = Phase.Gas,
                        Proportion = xe,
                    });
                }
                Atmosphere.Mixtures.Add(firstLayer);
            }
        }

        private void GenerateHydrosphere()
        {
            if (CanHaveWater)
            {
                var factor = Mass / 8.75e5;
                var mass = Math.Min(factor, Randomizer.Static.Lognormal(0, factor * 4));

                var water = (float)(mass / Mass);
                if (!TMath.IsZero(water))
                {
                    // Surface water is mostly salt water.
                    var saltWater = (float)Math.Round(Randomizer.Static.Normal(0.945, 0.015), 3);
                    Hydrosphere = new Mixture(new MixtureComponent[]
                    {
                        new MixtureComponent
                        {
                            Chemical = Chemical.Water,
                            Phase = Phase.Liquid,
                            Proportion = 1 - saltWater,
                        },
                        new MixtureComponent
                        {
                            Chemical = Chemical.Water_Salt,
                            Phase = Phase.Liquid,
                            Proportion = saltWater,
                        },
                    })
                    {
                        Proportion = water,
                    };
                }
            }
            if (_hydrosphere == null)
            {
                Hydrosphere = new Mixture
                {
                    Components = new HashSet<MixtureComponent>(),
                };
            }
        }

        private float GetHydrosphereAtmosphereRatio() => (float)Math.Max(1, (Hydrosphere.Proportion * Mass) / Atmosphere.AtmosphericMass);

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

        private void ReduceCO2()
        {
            var co2 = Atmosphere.GetProportion(Chemical.CarbonDioxide, Phase.Gas, true);
            if (co2 < 1.0e-3)
            {
                return;
            }

            foreach (var layer in Atmosphere.Mixtures.Where(x =>
                x.ContainsSubstance(Chemical.CarbonDioxide, Phase.Gas)))
            {
                co2 = (float)Randomizer.Static.NextDouble(1.5e-5, 1.0e-3);
                var n2 = layer.GetProportion(Chemical.CarbonDioxide, Phase.Any) - co2;
                layer.RemoveComponent(Chemical.CarbonDioxide, Phase.Liquid);
                layer.RemoveComponent(Chemical.CarbonDioxide, Phase.Solid);
                layer.SetProportion(Chemical.CarbonDioxide, Phase.Gas, co2);

                // Replace the missing CO2 with inert gases.
                // Some portion of the N2 may be Ar instead.
                var ar = (float)Math.Max(layer.GetProportion(Chemical.Argon, Phase.Gas), n2 * Randomizer.Static.NextDouble(-0.02, 0.04));
                layer.SetProportion(Chemical.Argon, Phase.Gas, ar);
                n2 -= ar;

                // An even smaller fraction may be Kr.
                var kr = (float)Math.Max(layer.GetProportion(Chemical.Krypton, Phase.Gas), n2 * Randomizer.Static.NextDouble(-2.5e-4, 5.0e-4));
                layer.SetProportion(Chemical.Krypton, Phase.Gas, kr);
                n2 -= kr;

                // An even smaller fraction may be Xe or Ne.
                var xe = (float)Math.Max(layer.GetProportion(Chemical.Xenon, Phase.Gas), n2 * Randomizer.Static.NextDouble(-1.8e-5, 3.5e-5));
                layer.SetProportion(Chemical.Xenon, Phase.Gas, xe);
                n2 -= xe;

                var ne = (float)Math.Max(layer.GetProportion(Chemical.Neon, Phase.Gas), n2 * Randomizer.Static.NextDouble(-1.8e-5, 3.5e-5));
                layer.SetProportion(Chemical.Neon, Phase.Gas, ne);
                n2 -= ne;

                layer.SetProportion(Chemical.Nitrogen, Phase.Gas, n2);
            }

            // Reduce the pressure to reflect the sequestration (unless it's being forced to a specific value).
            if (habitabilityRequirements == null)
            {
                Atmosphere.AtmosphericPressure -= Atmosphere.AtmosphericPressure * co2;
            }
            Atmosphere.ResetPressureDependentProperties();
        }

        private void SetHydrosphereProportion(Chemical chemical, Phase phase, float proportion, ref float hydrosphereAtmosphereRatio)
        {
            Hydrosphere.Proportion += Hydrosphere.Proportion * (proportion - Hydrosphere.GetProportion(chemical, phase));
            Hydrosphere.SetProportion(chemical, phase, proportion);
            hydrosphereAtmosphereRatio = GetHydrosphereAtmosphereRatio();
        }
    }
}
