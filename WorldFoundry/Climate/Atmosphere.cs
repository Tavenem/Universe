using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Troschuetz.Random;
using WorldFoundry.CelestialBodies;
using WorldFoundry.CelestialBodies.Planetoids.Planets.TerrestrialPlanets;
using WorldFoundry.Substances;
using WorldFoundry.Utilities;

namespace WorldFoundry.Climate
{
    /// <summary>
    /// Represents a planetary atmosphere, represented as a <see cref="Mixture"/>.
    /// </summary>
    public class Atmosphere : Mixture
    {
        private float? _atmosphericHeight;
        /// <summary>
        /// Specifies the average height of this <see cref="Atmosphere"/>, in meters.
        /// </summary>
        public float AtmosphericHeight
        {
            get
            {
                if (!_atmosphericHeight.HasValue)
                {
                    _atmosphericHeight = GetAtmosphericHeight();
                }
                return _atmosphericHeight.Value;
            }
        }

        private double? _atmosphericMass;
        /// <summary>
        /// Specifies the total mass of this <see cref="Atmosphere"/>, in kg.
        /// </summary>
        public double AtmosphericMass
        {
            get
            {
                if (!_atmosphericMass.HasValue)
                {
                    _atmosphericMass = GetAtmosphericMass();
                }
                return _atmosphericMass.Value;
            }
        }

        private float _atmosphericPressure;
        /// <summary>
        /// Specifies the atmospheric pressure at the surface of the planetary body, in kPa.
        /// </summary>
        public float AtmosphericPressure
        {
            get => Mixtures.All(x => x.Components.Count == 0) ? 0 : _atmosphericPressure;
            set
            {
                if (_atmosphericPressure != value)
                {
                    _atmosphericPressure = value;
                    ResetPressureDependentProperties();
                }
            }
        }

        private float? _atmosphericScaleHeight;
        /// <summary>
        /// Specifies the average scale height of this <see cref="Atmosphere"/>, in meters.
        /// </summary>
        public float AtmosphericScaleHeight
        {
            get
            {
                if (!_atmosphericScaleHeight.HasValue)
                {
                    _atmosphericScaleHeight = GetAtmosphericScaleHeight();
                }
                return _atmosphericScaleHeight.Value;
            }
        }

        /// <summary>
        /// The <see cref="CelestialBodies.CelestialBody"/> this <see cref="Atmosphere"/> surrounds.
        /// </summary>
        public CelestialBody CelestialBody { get; private set; }

        private float? _dryLapseRate;
        /// <summary>
        /// Specifies the dry adiabatic lapse rate within this <see cref="Atmosphere"/>, in K/m.
        /// </summary>
        public float DryLapseRate
        {
            get
            {
                if (!_dryLapseRate.HasValue)
                {
                    _dryLapseRate = GetLapseRateDry();
                }
                return _dryLapseRate.Value;
            }
        }

        private float? _greenhouseEffect;
        /// <summary>
        /// The total greenhouse effect for this <see cref="Atmosphere"/>, in K.
        /// </summary>
        internal float GreenhouseEffect
        {
            get
            {
                if (!_greenhouseEffect.HasValue)
                {
                    _greenhouseEffect = GetGreenhouseEffect();
                }
                return _greenhouseEffect.Value;
            }
        }

        private float? _greenhouseFactor;
        /// <summary>
        /// The total greenhouse factor for this <see cref="Atmosphere"/>.
        /// </summary>
        internal float GreenhouseFactor
        {
            get
            {
                if (!_greenhouseFactor.HasValue)
                {
                    _greenhouseFactor = GetGreenhouseFactor();
                }
                return _greenhouseFactor.Value;
            }
        }

        private float? _insolationFactor_Equatorial;
        /// <summary>
        /// The insolation factor to be used at the equator.
        /// </summary>
        private float InsolationFactor_Equatorial
        {
            get
            {
                if (!_insolationFactor_Equatorial.HasValue)
                {
                    _insolationFactor_Equatorial = GetInsolationFactor();
                }
                return _insolationFactor_Equatorial.Value;
            }
        }

        private float? _insolationFactor_Polar;
        /// <summary>
        /// The insolation factor to be used at the predetermined latitude for checking polar temperatures.
        /// </summary>
        private float InsolationFactor_Polar
        {
            get
            {
                if (!_insolationFactor_Polar.HasValue)
                {
                    _insolationFactor_Polar = GetInsolationFactor(true);
                }
                return _insolationFactor_Polar.Value;
            }
        }

        /// <summary>
        /// Initializes a new instance of <see cref="Atmosphere"/>.
        /// </summary>
        public Atmosphere() { }

        /// <summary>
        /// Initializes a new instance of <see cref="Atmosphere"/> with the given parameters.
        /// </summary>
        /// <param name="body">The <see cref="CelestialBodies.CelestialBody"/> this <see cref="Atmosphere"/> surrounds.</param>
        public Atmosphere(CelestialBody body) => CelestialBody = body;

        /// <summary>
        /// Initializes a new instance of <see cref="Atmosphere"/> with the given parameters.
        /// </summary>
        /// <param name="body">The <see cref="CelestialBodies.CelestialBody"/> this <see cref="Atmosphere"/> surrounds.</param>
        /// <param name="pressure">The atmospheric pressure at the surface of the planetary body, in kPa.</param>
        public Atmosphere(CelestialBody body, float pressure) : this(body) => AtmosphericPressure = pressure;

        /// <summary>
        /// Calculates the atmospheric density for the given conditions, in kg/m³.
        /// </summary>
        /// <param name="pressure">A pressure, in kPa.</param>
        /// <param name="temperature">A temperature, in K.</param>
        /// <returns>The atmospheric density for the given conditions, in kg/m³.</returns>
        internal static float GetAtmosphericDensity(float temperature, float pressure)
            => pressure * 1000 / (287.058f * temperature);

        /// <summary>
        /// Calculates the saturation mixing ratio of water under the given conditions.
        /// </summary>
        /// <param name="vaporPressure">A vapor pressure, in Pa.</param>
        /// <param name="pressure">The total pressure, in kPa.</param>
        /// <returns>The saturation mixing ratio of water under the given conditions.</returns>
        internal static float GetSaturationMixingRatio(float vaporPressure, float pressure)
        {
            var vp = vaporPressure / 1000;
            if (vp >= pressure)
            {
                vp = pressure * 0.99999f;
            }
            return 0.6219907f * vp / (pressure - vp);
        }

        /// <summary>
        /// Calculates the saturation vapor pressure of water at the given temperature, in Pa.
        /// </summary>
        /// <param name="temperature">A temperature, in K.</param>
        /// <returns>The saturation vapor pressure of water at the given temperature, in Pa.</returns>
        internal static float GetSaturationVaporPressure(float temperature)
        {
            var a = temperature > Chemical.Water.MeltingPoint
                ? 611.21
                : 611.15;
            var b = temperature > Chemical.Water.MeltingPoint
                ? 18.678
                : 23.036;
            var c = temperature > Chemical.Water.MeltingPoint
                ? 234.5
                : 333.7;
            var d = temperature > Chemical.Water.MeltingPoint
                ? 257.14
                : 279.82;
            var t = temperature - Chemical.Water.MeltingPoint;
            return (float)(a * Math.Exp((b - (t / c)) * (t / (d + t))));
        }

        internal void CalculateGasPhaseMix(
            Mixture hydrosphere,
            Mixture hydrosphereSurface,
            bool canHaveOxygen,
            Chemical chemical,
            float surfaceTemp,
            float polarTemp,
            ref float hydrosphereAtmosphereRatio,
            ref float adjustedAtmosphericPressure)
        {
            var hydrosphereProportion = hydrosphere.GetProportion(chemical, Phase.Any, hydrosphere.Mixtures?.Count > 0);
            if (chemical == Chemical.Water)
            {
                hydrosphereProportion += hydrosphere.GetProportion(Chemical.Water_Salt, Phase.Any, hydrosphere.Mixtures?.Count > 0);
            }

            var vaporProportion = GetProportion(chemical, Phase.Gas, true);

            var vaporPressure = Chemical.Water.CalculateVaporPressure(surfaceTemp);

            if (surfaceTemp < Chemical.Water.AntoineMinimumTemperature ||
                (surfaceTemp <= Chemical.Water.AntoineMaximumTemperature &&
                AtmosphericPressure > vaporPressure))
            {
                CondenseAtmosphericComponent(
                    hydrosphere,
                    hydrosphereSurface,
                    canHaveOxygen,
                    chemical,
                    surfaceTemp,
                    polarTemp,
                    hydrosphereProportion,
                    vaporProportion,
                    vaporPressure,
                    ref hydrosphereAtmosphereRatio,
                    ref adjustedAtmosphericPressure);
            }
            // This indicates that the chemical will fully boil off.
            else if (hydrosphereProportion > 0)
            {
                EvaporateAtmosphericComponent(
                    hydrosphere,
                    hydrosphereSurface,
                    canHaveOxygen,
                    chemical,
                    hydrosphereProportion,
                    vaporProportion,
                    ref hydrosphereAtmosphereRatio,
                    ref adjustedAtmosphericPressure);
            }

            if (chemical == Chemical.Water)
            {
                CheckCO2Reduction(vaporPressure);
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
        internal void CalculatePhases(
            Mixture hydrosphere,
            Mixture hydrosphereSurface,
            bool canHaveOxygen,
            float surfaceTemp,
            ref float hydrosphereAtmosphereRatio,
            ref float adjustedAtmosphericPressure)
        {
            var polarTemp = GetSurfaceTemperatureAverageOrbital(true);

            CalculateGasPhaseMix(hydrosphere, hydrosphereSurface, canHaveOxygen, Chemical.Methane, surfaceTemp, polarTemp, ref hydrosphereAtmosphereRatio, ref adjustedAtmosphericPressure);

            CalculateGasPhaseMix(hydrosphere, hydrosphereSurface, canHaveOxygen, Chemical.CarbonMonoxide, surfaceTemp, polarTemp, ref hydrosphereAtmosphereRatio, ref adjustedAtmosphericPressure);

            CalculateGasPhaseMix(hydrosphere, hydrosphereSurface, canHaveOxygen, Chemical.CarbonDioxide, surfaceTemp, polarTemp, ref hydrosphereAtmosphereRatio, ref adjustedAtmosphericPressure);

            CalculateGasPhaseMix(hydrosphere, hydrosphereSurface, canHaveOxygen, Chemical.Nitrogen, surfaceTemp, polarTemp, ref hydrosphereAtmosphereRatio, ref adjustedAtmosphericPressure);

            CalculateGasPhaseMix(hydrosphere, hydrosphereSurface, canHaveOxygen, Chemical.Oxygen, surfaceTemp, polarTemp, ref hydrosphereAtmosphereRatio, ref adjustedAtmosphericPressure);

            // No need to check for ozone, since it is only added to atmospheres on planets with
            // liquid surface water, which means temperatures too high for liquid or solid ozone.

            CalculateGasPhaseMix(hydrosphere, hydrosphereSurface, canHaveOxygen, Chemical.SulphurDioxide, surfaceTemp, polarTemp, ref hydrosphereAtmosphereRatio, ref adjustedAtmosphericPressure);

            // Water is handled differently, since the planet may already have surface water.
            if (hydrosphere.ContainsSubstance(Chemical.Water, Phase.Any)
                || hydrosphere.ContainsSubstance(Chemical.Water_Salt, Phase.Any)
                || ContainsSubstance(Chemical.Water, Phase.Any))
            {
                CalculateGasPhaseMix(hydrosphere, hydrosphereSurface, canHaveOxygen, Chemical.Water, surfaceTemp, polarTemp, ref hydrosphereAtmosphereRatio, ref adjustedAtmosphericPressure);
            }
        }

        /// <remarks>
        /// At least 1% humidity leads to a reduction of CO2 to a trace gas, by a presumed
        /// carbon-silicate cycle.
        /// </remarks>
        private void CheckCO2Reduction(float vaporPressure)
        {
            if ((GetChildAtFirstLayer().GetComponent(Chemical.Water, Phase.Gas)?.Proportion ?? 0) *
                AtmosphericPressure >= 0.01 * vaporPressure)
            {
                var co2 = GetProportion(Chemical.CarbonDioxide, Phase.Gas, true);
                if (co2 < 1.0e-3)
                {
                    return;
                }

                foreach (var layer in Mixtures.Where(x =>
                    x.ContainsSubstance(Chemical.CarbonDioxide, Phase.Gas)))
                {
                    co2 = (float)Randomizer.Static.NextDouble(1.5e-5, 1.0e-3);

                    // Replace most of the CO2 with inert gases.
                    var n2 = layer.GetProportion(Chemical.Nitrogen, Phase.Gas) + layer.GetProportion(Chemical.CarbonDioxide, Phase.Any) - co2;
                    layer.RemoveComponent(Chemical.CarbonDioxide, Phase.Liquid);
                    layer.RemoveComponent(Chemical.CarbonDioxide, Phase.Solid);
                    layer.SetProportion(Chemical.CarbonDioxide, Phase.Gas, co2);

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
                ResetGreenhouseFactor();
            }
        }

        private void CondenseAtmosphericComponent(
            Mixture hydrosphere,
            Mixture hydrosphereSurface,
            bool canHaveOxygen,
            Chemical chemical,
            float surfaceTemp,
            float polarTemp,
            float hydrosphereProportion,
            float vaporProportion,
            float vaporPressure,
            ref float hydrosphereAtmosphereRatio,
            ref float adjustedAtmosphericPressure)
        {
            if (surfaceTemp <= Chemical.Water.MeltingPoint) // Below freezing point; add ice.
            {
                CondenseAtmosphericIce(
                    hydrosphere,
                    hydrosphereSurface,
                    chemical,
                    surfaceTemp,
                    hydrosphereProportion,
                    ref vaporProportion,
                    ref hydrosphereAtmosphereRatio,
                    ref adjustedAtmosphericPressure);
            }
            else // Above freezing point, but also above vapor pressure; add liquid.
            {
                CondenseAtmosphericLiquid(
                    hydrosphere,
                    hydrosphereSurface,
                    chemical,
                    polarTemp,
                    ref hydrosphereProportion,
                    vaporProportion,
                    hydrosphereAtmosphereRatio);
            }

            // Adjust vapor present in the atmosphere based on the vapor pressure.
            var pressureRatio = Math.Min(1, vaporPressure / AtmosphericPressure);
            // This would represent 100% humidity. Since this is the case, in principle, only at the
            // surface of bodies of liquid, and should decrease exponentially with altitude, an
            // approximation of 25% average humidity overall is used.
            vaporProportion = (hydrosphereProportion + vaporProportion) * pressureRatio;
            vaporProportion *= 0.25f;
            if (!TMath.IsZero(vaporProportion))
            {
                float previousGasFraction = 0;
                var gasFraction = vaporProportion;
                AddComponent(chemical, Phase.Gas, vaporProportion, true);

                // For water, also add a corresponding amount of oxygen, if it's not already present.
                if (chemical == Chemical.Water && canHaveOxygen)
                {
                    var o2 = GetProportion(Chemical.Oxygen, Phase.Gas, true);
                    previousGasFraction += o2;
                    o2 = (float)Math.Max(o2, Math.Round(vaporProportion * 0.0001, 5));
                    gasFraction += o2;
                    SetProportion(Chemical.Oxygen, Phase.Gas, o2, true);
                }

                adjustedAtmosphericPressure += adjustedAtmosphericPressure * (gasFraction - previousGasFraction);
            }

            // Add clouds.
            var clouds = vaporProportion * 0.2f;
            if (!TMath.IsZero(clouds))
            {
                var troposphere = GetTroposphere();
                if (surfaceTemp <= chemical.MeltingPoint)
                {
                    troposphere.SetProportion(chemical, Phase.Solid, clouds);
                }
                else if (polarTemp < chemical.MeltingPoint)
                {
                    var halfClouds = clouds / 2;
                    troposphere.SetProportion(chemical, Phase.Liquid, halfClouds);
                    troposphere.SetProportion(chemical, Phase.Solid, halfClouds);
                }
                else
                {
                    troposphere.SetProportion(chemical, Phase.Liquid, clouds);
                }
            }
        }

        private void CondenseAtmosphericIce(
            Mixture hydrosphere,
            Mixture hydrosphereSurface,
            Chemical chemical,
            float surfaceTemp,
            float hydrosphereProportion,
            ref float vaporProportion,
            ref float hydrosphereAtmosphereRatio,
            ref float adjustedAtmosphericPressure)
        {
            var ice = hydrosphereProportion;

            // A subsurface liquid water ocean may persist if it's deep enough.
            if (chemical == Chemical.Water && hydrosphere.Proportion >= 0.01)
            {
                ice = (0.01f / hydrosphere.Proportion) * hydrosphereProportion;
            }

            // Liquid fully precipitates out of the atmosphere when below the minimum; if no liquid
            // exists in the hydrosphere yet, condensed ice is added from atmospheric vapor, though
            // the latter is not removed.
            if (surfaceTemp < chemical.AntoineMinimumTemperature || ice == 0)
            {
                ice += vaporProportion / hydrosphereAtmosphereRatio;

                if (surfaceTemp < chemical.AntoineMinimumTemperature)
                {
                    vaporProportion = 0;
                    RemoveComponent(chemical, Phase.Any, true);
                    RemoveEmptyChildren();
                    if (Mixtures.Count == 0)
                    {
                        Mixtures.Add(new Mixture());
                        adjustedAtmosphericPressure = 0;
                    }
                }
            }

            if (TMath.IsZero(ice))
            {
                return;
            }

            if (!TMath.IsZero(hydrosphereProportion)) // Change existing hydrosphere to ice.
            {
                if (ice < hydrosphereProportion) // A subsurface ocean is indicated.
                {
                    if ((hydrosphere.Mixtures?.Count ?? 0) == 0)
                    {
                        hydrosphere.CopyLayer(0, ice);
                    }
                    else
                    {
                        hydrosphere.GetChildAtFirstLayer().Proportion = 1 - ice;
                        hydrosphere.GetChildAtLastLayer().Proportion = ice;
                    }
                }

                // Convert entire hydrosphere surface to ice.
                // Remove any existing liquid from the hydrosphere.
                hydrosphereSurface.RemoveComponent(chemical, Phase.Liquid);
                if (chemical == Chemical.Water) // Also remove salt water when removing water.
                {
                    hydrosphereSurface.RemoveComponent(Chemical.Water_Salt, Phase.Liquid);
                }

                // Nothing but the chemical in the former hydrosphere.
                if (hydrosphereSurface.Components.Count == 0)
                {
                    hydrosphereSurface.AddComponent(chemical, Phase.Solid, 1);
                }
                // Something besides the chemical left in the hydrosphere (other deposited liquids and ices).
                else
                {
                    hydrosphereSurface.AddComponent(chemical, Phase.Solid, ice);
                }
                if (CelestialBody is TerrestrialPlanet t)
                {
                    hydrosphereAtmosphereRatio = t.GetHydrosphereAtmosphereRatio();
                }
            }
            else // Chemical not yet present in hydrosphere.
            {
                SetHydrosphereProportion(hydrosphere, hydrosphereSurface, chemical, Phase.Solid, ice, ref hydrosphereAtmosphereRatio);
            }
        }

        private void CondenseAtmosphericLiquid(
            Mixture hydrosphere,
            Mixture hydrosphereSurface,
            Chemical chemical,
            float polarTemp,
            ref float hydrosphereProportion,
            float vaporProportion,
            float hydrosphereAtmosphereRatio)
        {
            // If the hydrosphere was a surface of water ice with a subsurface ocean, melt the
            // surface and return to a single layer.
            if (chemical == Chemical.Water && hydrosphere.Mixtures?.Count > 0)
            {
                var ice = hydrosphereSurface.GetProportion(Chemical.Water, Phase.Solid);
                hydrosphereSurface.RemoveComponent(Chemical.Water, Phase.Solid);
                SetHydrosphereProportion(hydrosphere, hydrosphereSurface, Chemical.Water_Salt, Phase.Liquid, ice, ref hydrosphereAtmosphereRatio);

                var saltIce = hydrosphereSurface.GetProportion(Chemical.Water_Salt, Phase.Solid);
                hydrosphereSurface.RemoveComponent(Chemical.Water_Salt, Phase.Solid);
                SetHydrosphereProportion(hydrosphere, hydrosphereSurface, Chemical.Water_Salt, Phase.Liquid, saltIce, ref hydrosphereAtmosphereRatio);

                hydrosphere.AbsorbLayers();
            }

            var saltWaterProportion = chemical == Chemical.Water ? (float)Math.Round(Randomizer.Static.Normal(0.945, 0.015), 3) : 0;
            var liquidProportion = 1 - saltWaterProportion;

            // If there is no liquid on the surface, condense from the atmosphere.
            if (TMath.IsZero(hydrosphereProportion))
            {
                var addedLiquid = vaporProportion / hydrosphereAtmosphereRatio;
                if (!TMath.IsZero(addedLiquid))
                {
                    SetHydrosphereProportion(hydrosphere, hydrosphereSurface, chemical, Phase.Liquid, addedLiquid * liquidProportion, ref hydrosphereAtmosphereRatio);
                    if (chemical == Chemical.Water)
                    {
                        SetHydrosphereProportion(hydrosphere, hydrosphereSurface, Chemical.Water_Salt, Phase.Liquid, addedLiquid * saltWaterProportion, ref hydrosphereAtmosphereRatio);
                    }
                    hydrosphereProportion += addedLiquid;
                }
            }

            // Create icecaps.
            if (polarTemp <= Chemical.Water.MeltingPoint)
            {
                var iceCaps = hydrosphereProportion * 0.28f;
                SetHydrosphereProportion(hydrosphere, hydrosphereSurface, chemical, Phase.Solid, iceCaps * liquidProportion, ref hydrosphereAtmosphereRatio);
                if (chemical == Chemical.Water)
                {
                    SetHydrosphereProportion(hydrosphere, hydrosphereSurface, Chemical.Water_Salt, Phase.Solid, iceCaps * saltWaterProportion, ref hydrosphereAtmosphereRatio);
                }
            }
        }

        /// <summary>
        /// Standard pressure of 101.325 kPa is presumed for a <see cref="ComponentRequirement"/>.
        /// This method converts the proportional values to reflect <see cref="AtmosphericPressure"/>.
        /// </summary>
        /// <param name="requirement">The <see cref="ComponentRequirement"/> to convert.</param>
        /// <returns>
        /// A new <see cref="ComponentRequirement"/> with proportions adjusted for <see cref="AtmosphericPressure"/>.
        /// </returns>
        public ComponentRequirement ConvertRequirementForPressure(ComponentRequirement requirement)
        {
            float minActual = requirement.MinimumProportion * Utilities.Science.Constants.StandardAtmosphericPressure;
            float? maxActual = requirement.MaximumProportion.HasValue ? requirement.MaximumProportion * Utilities.Science.Constants.StandardAtmosphericPressure : null;
            return new ComponentRequirement
            {
                Chemical = requirement.Chemical,
                MaximumProportion = maxActual.HasValue ? maxActual / AtmosphericPressure : null,
                MinimumProportion = minActual / AtmosphericPressure,
                Phase = requirement.Phase,
            };
        }

        /// <summary>
        /// Standard pressure of 101.325 kPa is presumed for <see cref="ComponentRequirement"/>s.
        /// This method converts the proportional values to reflect <see cref="AtmosphericPressure"/>.
        /// </summary>
        /// <param name="requirements">The <see cref="ComponentRequirement"/>s to convert.</param>
        /// <returns>
        /// An <see cref="IEnumerable{T}"/> of <see cref="ComponentRequirement"/>s with proportions
        /// adjusted for <see cref="AtmosphericPressure"/>.
        /// </returns>
        public IEnumerable<ComponentRequirement> ConvertRequirementsForPressure(IEnumerable<ComponentRequirement> requirements)
        {
            if (requirements == null)
            {
                yield break;
            }
            else
            {
                foreach (var requirement in requirements)
                {
                    yield return ConvertRequirementForPressure(requirement);
                }
            }
        }

        private void EvaporateAtmosphericComponent(
            Mixture hydrosphere,
            Mixture hydrosphereSurface,
            bool canHaveOxygen,
            Chemical chemical,
            float hydrosphereProportion,
            float vaporProportion,
            ref float hydrosphereAtmosphereRatio,
            ref float adjustedAtmosphericPressure)
        {
            if (TMath.IsZero(hydrosphereProportion))
            {
                return;
            }

            if (chemical == Chemical.Water)
            {
                hydrosphere.AbsorbLayers();
            }

            var gasProportion = hydrosphereProportion * hydrosphereAtmosphereRatio;
            float previousGasProportion = vaporProportion;

            SetHydrosphereProportion(hydrosphere, hydrosphereSurface, chemical, Phase.Any, 0, ref hydrosphereAtmosphereRatio);

            if (chemical == Chemical.Water)
            {
                SetHydrosphereProportion(hydrosphere, hydrosphereSurface, Chemical.Water_Salt, Phase.Any, 0, ref hydrosphereAtmosphereRatio);

                // It is presumed that photodissociation will eventually reduce the amount of water
                // vapor to a trace gas (the H2 will be lost due to atmospheric escape, and the
                // oxygen will be lost to surface oxidation).
                var waterVapor = Math.Min(gasProportion, (float)Math.Round(Randomizer.Static.NextDouble(0.001), 4));
                gasProportion = waterVapor;

                previousGasProportion += GetProportion(Chemical.Oxygen, Phase.Gas, true);
                var o2 = (float)Math.Round(gasProportion * 0.0001, 5);
                gasProportion += o2;

                foreach (var layer in Mixtures)
                {
                    layer.SetProportion(chemical, Phase.Gas, Math.Max(layer.GetProportion(chemical, Phase.Gas), waterVapor));

                    // Some is added as oxygen, due to photodissociation.
                    if (canHaveOxygen)
                    {
                        layer.SetProportion(Chemical.Oxygen, Phase.Gas, Math.Max(layer.GetProportion(Chemical.Oxygen, Phase.Gas), o2));
                    }
                }
            }
            else
            {
                SetProportion(chemical, Phase.Gas, gasProportion, true);
            }

            adjustedAtmosphericPressure += adjustedAtmosphericPressure * (gasProportion - previousGasProportion);
        }

        /// <summary>
        /// Performs the Exner function on the given pressure.
        /// </summary>
        /// <param name="pressure">A pressure, in kPa.</param>
        /// <returns>The non-dimensionalized pressure.</returns>
        internal float Exner(float pressure) => (float)Math.Pow(pressure / AtmosphericPressure, Utilities.Science.Constants.SpecificGasConstantDivSpecificHeatOfDryAir);

        /// <summary>
        /// Calculates the air mass coefficient at the given latitude and elevation.
        /// </summary>
        /// <param name="latitude">A latitude, in radians.</param>
        /// <param name="elevation">An elevation, in meters.</param>
        /// <param name="cosLatitude">
        /// Optionally, the cosine of the given latitude. If omitted, it will be calculated.
        /// </param>
        /// <returns>The air mass coefficient at the given latitude.</returns>
        private float GetAirMass(double latitude, float elevation, double? cosLatitude = null)
        {
            var r = CelestialBody.Radius / AtmosphericScaleHeight;
            var cosLat = cosLatitude ?? Math.Cos(latitude);
            var c = elevation / AtmosphericScaleHeight;
            var rPlusCcosLat = (r + c) * cosLat;
            return (float)(Math.Sqrt(rPlusCcosLat * rPlusCcosLat + (2 * r + 1 + c) * (1 - c)) - rPlusCcosLat);
        }

        /// <summary>
        /// Calculates the atmospheric drag on a spherical object within this <see
        /// cref="Atmosphere"/> under given conditions, in N.
        /// </summary>
        /// <param name="density">The density of the atmosphere at the object's location.</param>
        /// <param name="speed">The speed of the object, in m/s.</param>
        /// <returns>The atmospheric drag on the object at the specified height, in N.</returns>
        /// <remarks>
        /// 0.47 is an arbitrary drag coefficient (that of a sphere in a fluid with a Reynolds number
        /// of 10⁴), which may not reflect the actual conditions at all, but the inaccuracy is
        /// accepted since the level of detailed information needed to calculate this value
        /// accurately is not desired in this library.
        /// </remarks>
        public float GetAtmosphericDrag(float density, float speed) =>
            (float)(0.235 * density * speed * speed * CelestialBody.Radius);

        /// <summary>
        /// Calculates the total height of the <see cref="Atmosphere"/>, defined as the point at
        /// which atmospheric pressure is roughly equal to 0.01 Pa, in meters.
        /// </summary>
        /// <returns>The height of the atmosphere, in meters.</returns>
        /// <remarks>
        /// Uses the molar mass of air on Earth, which is clearly not correct for other atmospheres,
        /// but is considered "close enough" for the purposes of this library.
        /// </remarks>
        private float GetAtmosphericHeight()
            => (float)((Math.Log(1.0e-5) * Utilities.Science.Constants.R * GetSurfaceTemperatureAverageOrbital()) / (AtmosphericPressure * -CelestialBody.SurfaceGravity * Utilities.Science.Constants.MolarMassOfAir));

        /// <summary>
        /// Calculates the total mass of this <see cref="Atmosphere"/>, in kg.
        /// </summary>
        /// <returns>The total mass of this <see cref="Atmosphere"/>, in kg.</returns>
        private double GetAtmosphericMass()
            => (Utilities.MathUtil.Constants.FourPI * CelestialBody.RadiusSquared * AtmosphericPressure * 1000) / CelestialBody.SurfaceGravity;

        /// <summary>
        /// Calculates the atmospheric pressure at a given elevation, in kPa.
        /// </summary>
        /// <param name="elevation">
        /// An elevation above the reference elevation for standard atmospheric pressure (sea level),
        /// in meters.
        /// </param>
        /// <param name="temperature">The temperature at the given elevation, in K.</param>
        /// <returns>The atmospheric pressure at the specified height, in kPa.</returns>
        /// <remarks>
        /// In an Earth-like atmosphere, the pressure lapse rate varies considerably in the different
        /// atmospheric layers, but this cannot be easily modeled for arbitrary exoplanetary
        /// atmospheres, so the simple barometric formula is used, which should be "close enough" for
        /// the purposes of this library. Also, this calculation uses the molar mass of air on Earth,
        /// which is clearly not correct for other atmospheres, but is considered "close enough" for
        /// the purposes of this library.
        /// </remarks>
        internal float GetAtmosphericPressure(float temperature, float elevation)
        {
            if (elevation <= 0)
            {
                return AtmosphericPressure;
            }
            else
            {
                return AtmosphericPressure * (float)(-CelestialBody.SurfaceGravity * Utilities.Science.Constants.MolarMassOfAir * elevation / (Utilities.Science.Constants.R * (temperature)));
            }
        }

        /// <summary>
        /// Calculates the scale height of the <see cref="Atmosphere"/>, in meters.
        /// </summary>
        /// <returns>The scale height of the atmosphere, in meters.</returns>
        /// <remarks>
        /// It reduces accuracy to use the effective temperature of the body, rather than the
        /// adjusted temperature with air mass and greenhouse effect taken into account, but there is
        /// a circular dependency among those properties and this one. Since scale height is judged
        /// to be both the least accurate and the least sensitive of the properties, it uses the
        /// effective temperature, allowing the more sensitive and accurate properties to be
        /// calculated with the more accurate temperature values.
        /// </remarks>
        private float GetAtmosphericScaleHeight()
            => (float)((AtmosphericPressure * 1000) / (CelestialBody.SurfaceGravity * GetAtmosphericDensity(CelestialBody.GetTotalTemperature(), AtmosphericPressure)));

        /// <summary>
        /// Determines the proportion of cloud cover provided by this <see cref="Atmosphere"/>.
        /// </summary>
        /// <returns>
        /// A value from 0.0 to 1.0, representing the proportion of the <see cref="Atmosphere"/>
        /// filled with clouds.
        /// </returns>
        public float GetCloudCover()
        {
            float clouds = 0;
            if (Mixtures != null)
            {
                foreach (var mixture in Mixtures)
                {
                    clouds = Math.Max(clouds, mixture.Components?
                        .Where(c => c.Phase == Phase.Liquid || c.Phase == Phase.Solid)
                        .Sum(c => c.Proportion) ?? 0);
                }
            }
            return clouds;
        }

        /// <summary>
        /// Calculates the total greenhouse effect for this <see cref="Atmosphere"/>, in K.
        /// </summary>
        /// <returns>The total greenhouse effect for this <see cref="Atmosphere"/>, in K.</returns>
        private float GetGreenhouseEffect()
        {
            var avgTemp = CelestialBody.GetTotalTemperatureAverageOrbital();
            return avgTemp * InsolationFactor_Equatorial * GreenhouseFactor - avgTemp;
        }

        /// <summary>
        /// Calculates the total greenhouse temperature multiplier for this <see cref="Atmosphere"/>.
        /// </summary>
        /// <returns>The total greenhouse temperature multiplier for this <see cref="Atmosphere"/>.</returns>
        /// <remarks>
        /// This uses an empirically-derived formula which fits the greenhouse effect for Venus,
        /// Earth and Mars based on calculated effective surface temperatures versus observed surface
        /// temperatures, and should provide good results for greenhouse gas concentrations and
        /// atmospheric pressures in the vicinity of the extremes represented by those three planets.
        /// A true formula for greenhouse effect is unknown; only the relative difference in
        /// temperature based on changing proportions in an existing atmosphere is well-studied, and
        /// relies on unique formulas for each gas, which is impractical for this library.
        /// </remarks>
        private float GetGreenhouseFactor()
        {
            var total = Mixtures?.Sum(m => (m.Components?
                    .Where(c => c.Chemical.GreenhousePotential > 0)
                    .Sum(c => c.Chemical.GreenhousePotential * c.Proportion) ?? 0)
                    * m.Proportion) ?? 0;
            if (TMath.IsZero(total))
            {
                return 1;
            }
            else
            {
                return (float)(0.933835 + 0.0441533 * Math.Exp(1.79077 * total) * (1.11169 + Math.Log(AtmosphericPressure)));
            }
        }

        /// <summary>
        /// Calculates the insolation factor to be used at the predetermined latitude for checking polar temperatures.
        /// </summary>
        /// <returns>
        /// The insolation factor to be used at the predetermined latitude for checking polar temperatures.
        /// </returns>
        private float GetInsolationFactor(bool polar = false)
        {
            var airMass = GetAirMass(polar ? CelestialBody.PolarLatitude : 0, 0, polar ? CelestialBody.CosPolarLatitude : 1);

            var atmMassRatio = AtmosphericMass / CelestialBody.Mass;
            return (float)Math.Pow(1320000 * atmMassRatio * Math.Pow(0.7, Math.Pow(airMass, 0.678)), 0.25);
        }

        /// <summary>
        /// Accepts an enumeration of <see cref="ComponentRequirement"/>s, and yields them back
        /// along with the reason(s) each one has failed, if any.
        /// </summary>
        /// <param name="requirements">An enumeration of <see cref="ComponentRequirement"/> s.</param>
        /// <returns>
        /// The enumeration of <see cref="ComponentRequirement"/> s, along with the reason(s) each one
        /// has failed, if any.
        /// </returns>
        public override IEnumerable<(ComponentRequirement, ComponentRequirementFailureType)> GetFailedRequirements(IEnumerable<ComponentRequirement> requirements)
        {
            var surfaceLayer = Mixtures.Count > 0 ? GetChildAtFirstLayer() : this;
            foreach (var requirement in ConvertRequirementsForPressure(requirements))
            {
                yield return (requirement, surfaceLayer.MeetsRequirement(requirement));
            }
        }

        /// <summary>
        /// Calculates the adiabatic lapse rate for this <see cref="Atmosphere"/>, after determining
        /// whether to use the dry or moist based on the presence of water vapor, in K/m.
        /// </summary>
        /// <param name="surfaceTemp">The surface temperature at the location, in K.</param>
        /// <returns>The adiabatic lapse rate for this <see cref="Atmosphere"/>, in K/m.</returns>
        /// <remarks>
        /// Uses the specific heat and gas constant of dry air on Earth, which is clearly not correct
        /// for other atmospheres, but is considered "close enough" for the purposes of this library.
        /// </remarks>
        private float GetLapseRate(float surfaceTemp) => ContainsSubstance(Chemical.Water, Phase.Gas) ? GetLapseRateMoist(surfaceTemp) : DryLapseRate;

        /// <summary>
        /// Calculates the dry adiabatic lapse rate near the surface of this <see cref="Atmosphere"/>, in K/m.
        /// </summary>
        /// <returns>The dry adiabatic lapse rate near the surface of this <see cref="Atmosphere"/>, in K/m.</returns>
        private float GetLapseRateDry() => (float)(CelestialBody.SurfaceGravity / Utilities.Science.Constants.SpecificHeatOfDryAir);

        /// <summary>
        /// Calculates the moist adiabatic lapse rate near the surface of this <see
        /// cref="Atmosphere"/>, in K/m.
        /// </summary>
        /// <param name="surfaceTemp">The surface temperature at the location, in K.</param>
        /// <returns>
        /// The moist adiabatic lapse rate near the surface of this <see cref="Atmosphere"/>, in K/m.
        /// </returns>
        /// <remarks>
        /// Uses the specific heat and gas constant of dry air on Earth, which is clearly not correct
        /// for other atmospheres, but is considered "close enough" for the purposes of this library.
        /// </remarks>
        private float GetLapseRateMoist(float surfaceTemp)
        {
            var surfaceTemp2 = surfaceTemp * surfaceTemp;
            var gasConstantSurfaceTemp2 = Utilities.Science.Constants.SpecificGasConstantOfDryAir * surfaceTemp2;

            var waterRatio = GetProportion(Chemical.Water, Phase.Gas, true);

            var numerator = gasConstantSurfaceTemp2 + Utilities.Science.Constants.HeatOfVaporizationOfWater * waterRatio * surfaceTemp;
            var denominator = Utilities.Science.Constants.SpecificHeatTimesSpecificGasConstant_DryAir * surfaceTemp2
                + Utilities.Science.Constants.HeatOfVaporizationOfWaterSquared * waterRatio * Utilities.Science.Constants.SpecificGasConstantRatioOfDryAirToWater;

            return (float)(CelestialBody.SurfaceGravity * (numerator / denominator));
        }

        /// <summary>
        /// Calculates the effective surface temperature, including greenhouse effects, in K.
        /// </summary>
        /// <param name="polar">
        /// If true, calculates the approximate temperature at the <see cref="CelestialBody"/>'s poles.
        /// </param>
        /// <returns>The surface temperature, in K.</returns>
        internal float GetSurfaceTemperature(bool polar = false)
            => CelestialBody.GetTotalTemperature() * (polar ? InsolationFactor_Polar : InsolationFactor_Equatorial) + GreenhouseEffect;

        /// <summary>
        /// Calculates the effective surface temperature, including greenhouse effects, in K.
        /// </summary>
        /// <param name="position">
        /// A hypothetical position for this <see cref="CelestialBody"/> at which its temperature
        /// will be calculated.
        /// </param>
        /// <param name="polar">
        /// If true, calculates the approximate temperature at the <see cref="CelestialBody"/>'s poles.
        /// </param>
        /// <returns>The surface temperature, in K.</returns>
        internal float GetSurfaceTemperatureAtPosition(Vector3 position, bool polar = false)
            => CelestialBody.GetTotalTemperatureFromPosition(position) * (polar ? InsolationFactor_Polar : InsolationFactor_Equatorial) + GreenhouseEffect;

        /// <summary>
        /// Returns the effective surface temperature, including greenhouse effects, as if the body
        /// was at apoapsis, in K.
        /// </summary>
        /// <param name="polar">
        /// If true, gets the approximate temperature at the <see cref="CelestialBody"/>'s poles.
        /// </param>
        /// <returns>The effective surface temperature at apoapsis, in K.</returns>
        internal float GetSurfaceTemperatureAtApoapsis(bool polar = false)
            => CelestialBody.TotalTemperatureAtApoapsis * (polar ? InsolationFactor_Polar : InsolationFactor_Equatorial) + GreenhouseEffect;

        /// <summary>
        /// Returns the effective surface temperature, including greenhouse effects, as if the body
        /// was at periapsis, in K.
        /// </summary>
        /// <param name="polar">
        /// If true, gets the approximate temperature at the <see cref="CelestialBody"/>'s poles.
        /// </param>
        /// <returns>The effective surface temperature at periapsis, in K.</returns>
        internal float GetSurfaceTemperatureAtPeriapsis(bool polar = false)
            => CelestialBody.TotalTemperatureAtPeriapsis * (polar ? InsolationFactor_Polar : InsolationFactor_Equatorial) + GreenhouseEffect;

        /// <summary>
        /// Returns the effective surface temperature, including greenhouse effects, averaged between
        /// periapsis and apoapsis, in K.
        /// </summary>
        /// <param name="polar">
        /// If true, gets the approximate temperature at the <see cref="CelestialBody"/>'s poles.
        /// </param>
        /// <returns>The average effective surface temperature.</returns>
        internal float GetSurfaceTemperatureAverageOrbital(bool polar = false)
        {
            // If the body is not actually in orbit, return its current temperature.
            if (CelestialBody.Orbit == null)
            {
                return GetSurfaceTemperature(polar);
            }
            else
            {
                return (GetSurfaceTemperatureAtPeriapsis(polar) + GetSurfaceTemperatureAtApoapsis(polar)) / 2.0f;
            }
        }

        /// <summary>
        /// Calculates the temperature of this <see cref="Atmosphere"/> at the given elevation, in K.
        /// </summary>
        /// <param name="surfaceTemp">The surface temperature at the location, in K.</param>
        /// <param name="elevation">The elevation, in meters.</param>
        /// <returns>
        /// The temperature of this <see cref="Atmosphere"/> at the given elevation, in K.
        /// </returns>
        /// <remarks>
        /// In an Earth-like atmosphere, the temperature lapse rate varies considerably in the
        /// different atmospheric layers, but this cannot be easily modeled for arbitrary
        /// exoplanetary atmospheres, so a simplified formula is used, which should be "close enough"
        /// for low elevations.
        /// </remarks>
        internal float GetTemperatureAtElevation(float surfaceTemp, float elevation)
        {
            // When outside the atmosphere, use the black body temperature, ignoring atmospheric effects.
            if (elevation >= AtmosphericHeight)
            {
                return CelestialBody.GetTotalTemperature();
            }

            if (elevation <= 0)
            {
                return surfaceTemp;
            }
            else
            {
                return surfaceTemp - elevation * GetLapseRate(surfaceTemp);
            }
        }

        /// <summary>
        /// Gets the troposphere of this <see cref="Atmosphere"/>.
        /// </summary>
        /// <returns>The troposphere of this <see cref="Atmosphere"/>.</returns>
        /// <remarks>
        /// If the <see cref="Atmosphere"/> doesn't yet have differentiated layers, they are first
        /// separated before returning the lowest layer as the troposphere.
        /// </remarks>
        internal Mixture GetTroposphere()
        {
            var troposphere = GetChildAtFirstLayer();

            // Separate troposphere from upper atmosphere if undifferentiated.
            if (Mixtures.Count == 1)
            {
                CopyLayer(0, 0.2f);
            }

            return troposphere;
        }

        /// <summary>
        /// Determines if this <see cref="Atmosphere"/> meets the given requirements.
        /// </summary>
        /// <param name="requirements">An enumeration of <see cref="ComponentRequirement"/>s.</param>
        /// <returns>true if this <see cref="Atmosphere"/> meets the requirements; false otherwise.</returns>
        public bool MeetsRequirements(IEnumerable<ComponentRequirement> requirements)
            => GetFailedRequirements(requirements).All(x => x.Item2 == ComponentRequirementFailureType.None);

        internal void ResetGreenhouseFactor() => _greenhouseFactor = null;

        internal void ResetPressureDependentProperties()
        {
            _atmosphericHeight = null;
            _atmosphericMass = null;
            _atmosphericScaleHeight = null;
            _greenhouseFactor = null;
            _greenhouseEffect = null;
            _insolationFactor_Polar = null;
            _insolationFactor_Equatorial = null;
        }

        internal void ResetTemperatureDependentProperties()
        {
            _atmosphericHeight = null;
            _atmosphericScaleHeight = null;
            _greenhouseEffect = null;
            _insolationFactor_Polar = null;
            _insolationFactor_Equatorial = null;
        }

        private void SetHydrosphereProportion(
            Mixture hydrosphere,
            Mixture hydrosphereSurface,
            Chemical chemical,
            Phase phase,
            float proportion,
            ref float hydrosphereAtmosphereRatio)
        {
            var newTotalProportion = hydrosphere.Mixtures?.Count > 0 ? hydrosphereSurface.Proportion * proportion : proportion;
            hydrosphere.Proportion += hydrosphere.Proportion * (newTotalProportion - hydrosphere.GetProportion(chemical, phase, hydrosphere.Mixtures?.Count > 0));
            hydrosphereSurface.SetProportion(chemical, phase, proportion);
            if (CelestialBody is TerrestrialPlanet t)
            {
                hydrosphereAtmosphereRatio = t.GetHydrosphereAtmosphereRatio();
            }
        }

        /// <summary>
        /// A range of acceptable amounts of O2, and list of maximum limits of common
        /// atmospheric gases for acceptable human breathability.
        /// </summary>
        public static List<ComponentRequirement> HumanBreathabilityRequirements = new List<ComponentRequirement>()
        {
            new ComponentRequirement { Chemical = Chemical.Oxygen, MinimumProportion = 0.07f, MaximumProportion = 0.53f, Phase = Phase.Gas },
            new ComponentRequirement { Chemical = Chemical.Ammonia, MaximumProportion = 0.00005f },
            new ComponentRequirement { Chemical = Chemical.AmmoniumHydrosulfide, MaximumProportion = 0.000001f },
            new ComponentRequirement { Chemical = Chemical.CarbonMonoxide, MaximumProportion = 0.00005f },
            new ComponentRequirement { Chemical = Chemical.CarbonDioxide, MaximumProportion = 0.005f },
            new ComponentRequirement { Chemical = Chemical.HydrogenSulfide, MaximumProportion = 0.0f },
            new ComponentRequirement { Chemical = Chemical.Methane, MaximumProportion = 0.001f },
            new ComponentRequirement { Chemical = Chemical.Ozone, MaximumProportion = 0.0000001f },
            new ComponentRequirement { Chemical = Chemical.Phosphine, MaximumProportion = 0.0000003f },
            new ComponentRequirement { Chemical = Chemical.SulphurDioxide, MaximumProportion = 0.000002f },
        };
    }
}
