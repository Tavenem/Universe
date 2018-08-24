using MathAndScience.MathUtil;
using MathAndScience.MathUtil.Shapes;
using MathAndScience.Science;
using Substances;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Troschuetz.Random;
using WorldFoundry.CelestialBodies;
using WorldFoundry.CelestialBodies.Planetoids.Planets.TerrestrialPlanets;
using WorldFoundry.Substances;

namespace WorldFoundry.Climate
{
    /// <summary>
    /// Represents a planetary atmosphere, represented as a <see cref="Substance"/>.
    /// </summary>
    public class Atmosphere : Substance
    {
        internal static readonly double TemperatureAtNearlyZeroSaturationVaporPressure = GetTemperatureAtSaturationVaporPressure(TMath.Tolerance);

        /// <summary>
        /// Specifies the average height of this <see cref="Atmosphere"/>, in meters.
        /// </summary>
        public double AtmosphericHeight => Shape.ContainingRadius;

        private double _atmosphericPressure;
        /// <summary>
        /// Specifies the atmospheric pressure at the surface of the planetary body, in kPa.
        /// </summary>
        public double AtmosphericPressure
        {
            get => Composition.IsEmpty() ? 0 : _atmosphericPressure;
            set
            {
                if (_atmosphericPressure != value)
                {
                    _atmosphericPressure = value;
                    ResetPressureDependentProperties();
                }
            }
        }

        private double? _atmosphericScaleHeight;
        /// <summary>
        /// Specifies the average scale height of this <see cref="Atmosphere"/>, in meters.
        /// </summary>
        public double AtmosphericScaleHeight
            => _atmosphericScaleHeight ?? (_atmosphericScaleHeight = GetAtmosphericScaleHeight()).Value;

        /// <summary>
        /// The <see cref="CelestialBodies.CelestialBody"/> this <see cref="Atmosphere"/> surrounds.
        /// </summary>
        public CelestialBody CelestialBody { get; }

        private double? _dryLapseRate;
        /// <summary>
        /// Specifies the dry adiabatic lapse rate within this <see cref="Atmosphere"/>, in K/m.
        /// </summary>
        public double DryLapseRate
            => _dryLapseRate ?? (_dryLapseRate = GetLapseRateDry()).Value;

        private double? _greenhouseEffect;
        /// <summary>
        /// The total greenhouse effect for this <see cref="Atmosphere"/>, in K.
        /// </summary>
        public double GreenhouseEffect
            => _greenhouseEffect ?? (_greenhouseEffect = GetGreenhouseEffect()).Value;

        private double? _greenhouseFactor;
        /// <summary>
        /// The total greenhouse factor for this <see cref="Atmosphere"/>.
        /// </summary>
        internal double GreenhouseFactor
            => _greenhouseFactor ?? (_greenhouseFactor = GetGreenhouseFactor()).Value;

        private double? _insolationFactor_Equatorial;
        /// <summary>
        /// The insolation factor to be used at the equator.
        /// </summary>
        internal double InsolationFactor_Equatorial
            => _insolationFactor_Equatorial ?? (_insolationFactor_Equatorial = GetInsolationFactor()).Value;

        private double? _insolationFactor_Polar;
        /// <summary>
        /// The insolation factor to be used at the predetermined latitude for checking polar temperatures.
        /// </summary>
        private double InsolationFactor_Polar
            => _insolationFactor_Polar ?? (_insolationFactor_Polar = GetInsolationFactor(true)).Value;

        /// <summary>
        /// Initializes a new instance of <see cref="Atmosphere"/>.
        /// </summary>
        public Atmosphere() { }

        /// <summary>
        /// Initializes a new instance of <see cref="Atmosphere"/> with the given parameters.
        /// </summary>
        /// <param name="body">The <see cref="CelestialBodies.CelestialBody"/> this <see cref="Atmosphere"/> surrounds.</param>
        /// <param name="composition">The <see cref="IComposition"/> which defines this <see cref="Atmosphere"/>.</param>
        /// <param name="pressure">The atmospheric pressure at the surface of the planetary body, in kPa.</param>
        public Atmosphere(CelestialBody body, IComposition composition, double pressure)
        {
            CelestialBody = body;
            Composition = composition;
            AtmosphericPressure = pressure;
            Mass = GetAtmosphericMass();
            Shape = GetShape();
        }

        /// <summary>
        /// Calculates the atmospheric density for the given conditions, in kg/m³.
        /// </summary>
        /// <param name="pressure">A pressure, in kPa.</param>
        /// <param name="temperature">A temperature, in K.</param>
        /// <returns>The atmospheric density for the given conditions, in kg/m³.</returns>
        internal static double GetAtmosphericDensity(double temperature, double pressure)
            => pressure * 1000 / (287.058 * temperature);

        /// <summary>
        /// Calculates the saturation mixing ratio of water under the given conditions.
        /// </summary>
        /// <param name="vaporPressure">A vapor pressure, in Pa.</param>
        /// <param name="pressure">The total pressure, in kPa.</param>
        /// <returns>The saturation mixing ratio of water under the given conditions.</returns>
        internal static double GetSaturationMixingRatio(double vaporPressure, double pressure)
        {
            var vp = vaporPressure / 1000;
            if (vp >= pressure)
            {
                vp = pressure * 0.99999;
            }
            return 0.6219907 * vp / (pressure - vp);
        }

        /// <summary>
        /// Calculates the saturation vapor pressure of water at the given temperature, in Pa.
        /// </summary>
        /// <param name="temperature">A temperature, in K.</param>
        /// <returns>The saturation vapor pressure of water at the given temperature, in Pa.</returns>
        internal static double GetSaturationVaporPressure(double temperature)
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
            return a * Math.Exp((b - (t / c)) * (t / (d + t)));
        }

        internal static double GetTemperatureAtSaturationVaporPressure(double satVaporPressure)
            => (237.3 / ((7.5 / Math.Log10(satVaporPressure / 611.15)) - 1)) + Chemical.Water.MeltingPoint;

        internal void CalculateGasPhaseMix(
            bool _canHaveOxygen,
            Chemical chemical,
            double surfaceTemp,
            double polarTemp,
            ref double hydrosphereAtmosphereRatio,
            ref double adjustedAtmosphericPressure)
        {
            if (!(CelestialBody is TerrestrialPlanet planet))
            {
                return;
            }

            var hydrosphere = planet.Hydrosphere;
            var proportionInHydrosphere = hydrosphere.GetProportion(chemical, Phase.Any);
            if (chemical == Chemical.Water)
            {
                proportionInHydrosphere += hydrosphere.GetProportion(Chemical.Water_Salt, Phase.Any);
            }

            var vaporProportion = Composition.GetProportion(chemical, Phase.Gas);

            var vaporPressure = Chemical.Water.GetVaporPressure(surfaceTemp);

            if (surfaceTemp < Chemical.Water.AntoineMinimumTemperature
                || (surfaceTemp <= Chemical.Water.AntoineMaximumTemperature
                && AtmosphericPressure > vaporPressure))
            {
                CondenseAtmosphericComponent(
                    _canHaveOxygen,
                    chemical,
                    surfaceTemp,
                    polarTemp,
                    proportionInHydrosphere,
                    vaporProportion,
                    vaporPressure,
                    ref hydrosphereAtmosphereRatio,
                    ref adjustedAtmosphericPressure);
            }
            // This indicates that the chemical will fully boil off.
            else if (proportionInHydrosphere > 0)
            {
                EvaporateAtmosphericComponent(
                    _canHaveOxygen,
                    chemical,
                    proportionInHydrosphere,
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
        /// <param name="_canHaveOxygen">Whether oxygen is allowed.</param>
        /// <param name="surfaceTemp">The effective surface temperature, in K.</param>
        /// <param name="hydrosphereAtmosphereRatio">The mass ratio of hydrosphere to atmosphere.</param>
        /// <param name="adjustedAtmosphericPressure">The effective atmospheric pressure, in kPa.</param>
        /// <remarks>
        /// Despite the theoretical possibility of an atmosphere cold enough to precipitate some of
        /// the noble gases, or hydrogen, they are ignored and presumed to exist always as trace
        /// atmospheric gases, never surface liquids or ices, or in large enough quantities to form clouds.
        /// </remarks>
        internal void CalculatePhases(
            bool _canHaveOxygen,
            double surfaceTemp,
            ref double hydrosphereAtmosphereRatio,
            ref double adjustedAtmosphericPressure)
        {
            var polarTemp = GetSurfaceTemperatureAverageOrbital(true);

            CalculateGasPhaseMix(_canHaveOxygen, Chemical.Methane, surfaceTemp, polarTemp, ref hydrosphereAtmosphereRatio, ref adjustedAtmosphericPressure);
            CalculateGasPhaseMix(_canHaveOxygen, Chemical.CarbonMonoxide, surfaceTemp, polarTemp, ref hydrosphereAtmosphereRatio, ref adjustedAtmosphericPressure);
            CalculateGasPhaseMix(_canHaveOxygen, Chemical.CarbonDioxide, surfaceTemp, polarTemp, ref hydrosphereAtmosphereRatio, ref adjustedAtmosphericPressure);
            CalculateGasPhaseMix(_canHaveOxygen, Chemical.Nitrogen, surfaceTemp, polarTemp, ref hydrosphereAtmosphereRatio, ref adjustedAtmosphericPressure);
            CalculateGasPhaseMix(_canHaveOxygen, Chemical.Oxygen, surfaceTemp, polarTemp, ref hydrosphereAtmosphereRatio, ref adjustedAtmosphericPressure);

            // No need to check for ozone, since it is only added to atmospheres on planets with
            // liquid surface water, which means temperatures too high for liquid or solid ozone.

            CalculateGasPhaseMix(_canHaveOxygen, Chemical.SulphurDioxide, surfaceTemp, polarTemp, ref hydrosphereAtmosphereRatio, ref adjustedAtmosphericPressure);

            // Water is handled differently, since the planet may already have surface water.
            if (!(CelestialBody is TerrestrialPlanet planet))
            {
                return;
            }
            var hydrosphere = planet.Hydrosphere;
            if (hydrosphere.ContainsSubstance(Chemical.Water, Phase.Any)
                || hydrosphere.ContainsSubstance(Chemical.Water_Salt, Phase.Any)
                || Composition.ContainsSubstance(Chemical.Water, Phase.Any))
            {
                CalculateGasPhaseMix(_canHaveOxygen, Chemical.Water, surfaceTemp, polarTemp, ref hydrosphereAtmosphereRatio, ref adjustedAtmosphericPressure);
            }
        }

        /// <summary>
        /// At least 1% humidity leads to a reduction of CO2 to a trace gas, by a presumed
        /// carbon-silicate cycle.
        /// </summary>
        /// <param name="vaporPressure">The vapor pressure of water.</param>
        private void CheckCO2Reduction(double vaporPressure)
        {
            if (Composition is LayeredComposite layeredComposite
                && layeredComposite.Layers?.Count > 0
                && layeredComposite.Layers[0].substance.GetProportion(Chemical.Water, Phase.Gas) * AtmosphericPressure >= 0.01 * vaporPressure)
            {
                var co2 = layeredComposite.GetProportion(Chemical.CarbonDioxide, Phase.Gas);
                if (co2 < 1.0e-3)
                {
                    return;
                }

                for (var i = 0; i < layeredComposite.Layers.Count; i++)
                {
                    if (!layeredComposite.Layers[i].substance.ContainsSubstance(Chemical.CarbonDioxide, Phase.Gas))
                    {
                        continue;
                    }

                    var component = layeredComposite.Layers[i].substance;

                    co2 = Randomizer.Static.NextDouble(1.5e-5, 1.0e-3);

                    // Replace most of the CO2 with inert gases.
                    var n2 = component.GetProportion(Chemical.Nitrogen, Phase.Gas) + component.GetProportion(Chemical.CarbonDioxide, Phase.Any) - co2;
                    component = component.RemoveComponent(Chemical.CarbonDioxide, Phase.Liquid);
                    component = component.RemoveComponent(Chemical.CarbonDioxide, Phase.Solid);
                    component = component.AddComponent(Chemical.CarbonDioxide, Phase.Gas, co2);

                    // Some portion of the N2 may be Ar instead.
                    var ar = Math.Max(component.GetProportion(Chemical.Argon, Phase.Gas), n2 * Randomizer.Static.NextDouble(-0.02, 0.04));
                    component = component.AddComponent(Chemical.Argon, Phase.Gas, ar);
                    n2 -= ar;

                    // An even smaller fraction may be Kr.
                    var kr = Math.Max(component.GetProportion(Chemical.Krypton, Phase.Gas), n2 * Randomizer.Static.NextDouble(-2.5e-4, 5.0e-4));
                    component = component.AddComponent(Chemical.Krypton, Phase.Gas, kr);
                    n2 -= kr;

                    // An even smaller fraction may be Xe or Ne.
                    var xe = Math.Max(component.GetProportion(Chemical.Xenon, Phase.Gas), n2 * Randomizer.Static.NextDouble(-1.8e-5, 3.5e-5));
                    component = component.AddComponent(Chemical.Xenon, Phase.Gas, xe);
                    n2 -= xe;

                    var ne = Math.Max(component.GetProportion(Chemical.Neon, Phase.Gas), n2 * Randomizer.Static.NextDouble(-1.8e-5, 3.5e-5));
                    component = component.AddComponent(Chemical.Neon, Phase.Gas, ne);
                    n2 -= ne;

                    component = component.AddComponent(Chemical.Nitrogen, Phase.Gas, n2);

                    layeredComposite.Layers[i] = (component, layeredComposite.Layers[i].proportion);
                }
                ResetGreenhouseFactor();
            }
        }

        private void CondenseAtmosphericComponent(
            bool _canHaveOxygen,
            Chemical chemical,
            double surfaceTemp,
            double polarTemp,
            double proportionInHydrosphere,
            double vaporProportion,
            double vaporPressure,
            ref double hydrosphereAtmosphereRatio,
            ref double adjustedAtmosphericPressure)
        {
            if (surfaceTemp <= Chemical.Water.MeltingPoint) // Below freezing point; add ice.
            {
                CondenseAtmosphericIce(
                    chemical,
                    surfaceTemp,
                    proportionInHydrosphere,
                    ref vaporProportion,
                    ref hydrosphereAtmosphereRatio,
                    ref adjustedAtmosphericPressure);
            }
            else // Above freezing point, but also above vapor pressure; add liquid.
            {
                CondenseAtmosphericLiquid(
                    chemical,
                    polarTemp,
                    ref proportionInHydrosphere,
                    vaporProportion,
                    hydrosphereAtmosphereRatio);
            }

            // Adjust vapor present in the atmosphere based on the vapor pressure.
            var pressureRatio = Math.Max(0, Math.Min(1, vaporPressure / AtmosphericPressure));
            // This would represent 100% humidity. Since this is the case, in principle, only at the
            // surface of bodies of liquid, and should decrease exponentially with altitude, an
            // approximation of 25% average humidity overall is used.
            vaporProportion = (proportionInHydrosphere + vaporProportion) * pressureRatio;
            vaporProportion *= 0.25;
            if (!TMath.IsZero(vaporProportion))
            {
                double previousGasFraction = 0;
                var gasFraction = vaporProportion;
                Composition = Composition.AddComponent(chemical, Phase.Gas, vaporProportion);

                // For water, also add a corresponding amount of oxygen, if it's not already present.
                if (chemical == Chemical.Water && _canHaveOxygen)
                {
                    var o2 = Composition.GetProportion(Chemical.Oxygen, Phase.Gas);
                    previousGasFraction += o2;
                    o2 = Math.Max(o2, Math.Round(vaporProportion * 0.0001, 5));
                    gasFraction += o2;
                    Composition = Composition.AddComponent(Chemical.Oxygen, Phase.Gas, o2);
                }

                adjustedAtmosphericPressure += adjustedAtmosphericPressure * (gasFraction - previousGasFraction);
            }

            // Add clouds.
            var clouds = vaporProportion * 0.2;
            if (!TMath.IsZero(clouds))
            {
                var troposphere = GetTroposphere();
                if (surfaceTemp <= chemical.MeltingPoint)
                {
                    troposphere.AddComponent(chemical, Phase.Solid, clouds);
                }
                else if (polarTemp < chemical.MeltingPoint)
                {
                    var halfClouds = clouds / 2;
                    troposphere.AddComponent(chemical, Phase.Liquid, halfClouds);
                    troposphere.AddComponent(chemical, Phase.Solid, halfClouds);
                }
                else
                {
                    troposphere.AddComponent(chemical, Phase.Liquid, clouds);
                }
            }
        }

        private void CondenseAtmosphericIce(
            Chemical chemical,
            double surfaceTemp,
            double proportionInHydrosphere,
            ref double vaporProportion,
            ref double hydrosphereAtmosphereRatio,
            ref double adjustedAtmosphericPressure)
        {
            if (!(CelestialBody is TerrestrialPlanet planet))
            {
                return;
            }

            var ice = proportionInHydrosphere;

            // A subsurface liquid water ocean may persist if it's deep enough.

            if (chemical == Chemical.Water && planet.HydrosphereProportion >= 0.01)
            {
                ice = 0.01 / planet.HydrosphereProportion * proportionInHydrosphere;
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
                    Composition = Composition.RemoveComponent(chemical, Phase.Any);
                    if (Composition.IsEmpty())
                    {
                        adjustedAtmosphericPressure = 0;
                    }
                }
            }

            if (TMath.IsZero(ice))
            {
                return;
            }

            if (!TMath.IsZero(proportionInHydrosphere)) // Change existing hydrosphere to ice.
            {
                // If a subsurface ocean is indicated, ensure differentiation in the correct proportions.
                if (ice < proportionInHydrosphere)
                {
                    if (planet.Hydrosphere is LayeredComposite layeredComposite)
                    {
                        layeredComposite.Layers[0] = (layeredComposite.Layers[0].substance, 1 - ice);
                        layeredComposite.Layers[1] = (layeredComposite.Layers[1].substance, ice);
                    }
                    else
                    {
                        planet.Hydrosphere = planet.Hydrosphere.Split(1 - ice, ice);
                    }
                }

                // Convert hydrosphere to ice; surface only if a subsurface ocean is indicated.
                if (ice < proportionInHydrosphere && planet.Hydrosphere is LayeredComposite lc1)
                {
                    lc1.SetLayerPhase(lc1.Layers.Count - 1, chemical, Phase.Solid);
                }
                else
                {
                    planet.Hydrosphere.SetPhase(chemical, Phase.Solid);
                }
                if (chemical == Chemical.Water) // Also remove salt water when removing water.
                {
                    if (ice < proportionInHydrosphere && planet.Hydrosphere is LayeredComposite lc2)
                    {
                        lc2.SetLayerPhase(lc2.Layers.Count - 1, Chemical.Water_Salt, Phase.Solid);
                    }
                    else
                    {
                        planet.Hydrosphere.SetPhase(Chemical.Water_Salt, Phase.Solid);
                    }
                }

                hydrosphereAtmosphereRatio = planet.GetHydrosphereAtmosphereRatio();
            }
            else // Chemical not yet present in hydrosphere.
            {
                SetHydrosphereProportion(chemical, Phase.Solid, ice, ref hydrosphereAtmosphereRatio);
            }
        }

        private void CondenseAtmosphericLiquid(
            Chemical chemical,
            double polarTemp,
            ref double proportionInHydrosphere,
            double vaporProportion,
            double hydrosphereAtmosphereRatio)
        {
            if (!(CelestialBody is TerrestrialPlanet planet))
            {
                return;
            }

            // If the hydrosphere was a surface of water ice with a subsurface ocean, melt the
            // surface and return to a single layer.
            if (chemical == Chemical.Water && planet.Hydrosphere is LayeredComposite layeredComposite)
            {
                layeredComposite.SetPhase(Chemical.Water, Phase.Liquid);
                layeredComposite.SetPhase(Chemical.Water_Salt, Phase.Liquid);
                planet.Hydrosphere = layeredComposite.Homogenize();
            }

            var saltWaterProportion = chemical == Chemical.Water ? Math.Round(Randomizer.Static.Normal(0.945, 0.015), 3) : 0;
            var liquidProportion = 1 - saltWaterProportion;

            // If there is no liquid on the surface, condense from the atmosphere.
            if (TMath.IsZero(proportionInHydrosphere))
            {
                var addedLiquid = vaporProportion / hydrosphereAtmosphereRatio;
                if (!TMath.IsZero(addedLiquid))
                {
                    SetHydrosphereProportion(chemical, Phase.Liquid, addedLiquid * liquidProportion, ref hydrosphereAtmosphereRatio);
                    if (chemical == Chemical.Water)
                    {
                        SetHydrosphereProportion(Chemical.Water_Salt, Phase.Liquid, addedLiquid * saltWaterProportion, ref hydrosphereAtmosphereRatio);
                    }
                    proportionInHydrosphere += addedLiquid;
                }
            }

            // Create icecaps.
            if (polarTemp <= Chemical.Water.MeltingPoint)
            {
                var iceCaps = proportionInHydrosphere * 0.28;
                SetHydrosphereProportion(chemical, Phase.Solid, iceCaps * liquidProportion, ref hydrosphereAtmosphereRatio);
                if (chemical == Chemical.Water)
                {
                    SetHydrosphereProportion(Chemical.Water_Salt, Phase.Solid, iceCaps * saltWaterProportion, ref hydrosphereAtmosphereRatio);
                }
            }
        }

        /// <summary>
        /// Standard pressure of 101.325 kPa is presumed for a <see cref="Requirement"/>.
        /// This method converts the proportional values to reflect <see cref="AtmosphericPressure"/>.
        /// </summary>
        /// <param name="requirement">The <see cref="Requirement"/> to convert.</param>
        /// <returns>
        /// A new <see cref="Requirement"/> with proportions adjusted for <see cref="AtmosphericPressure"/>.
        /// </returns>
        public Requirement ConvertRequirementForPressure(Requirement requirement)
        {
            var minActual = requirement.MinimumProportion * ScienceConstants.atm;
            var maxActual = requirement.MaximumProportion.HasValue ? requirement.MaximumProportion * ScienceConstants.atm : null;
            return new Requirement
            {
                Chemical = requirement.Chemical,
                MaximumProportion = maxActual.HasValue ? maxActual / AtmosphericPressure : null,
                MinimumProportion = minActual / AtmosphericPressure,
                Phase = requirement.Phase,
            };
        }

        /// <summary>
        /// Standard pressure of 101.325 kPa is presumed for <see cref="Requirement"/>s.
        /// This method converts the proportional values to reflect <see cref="AtmosphericPressure"/>.
        /// </summary>
        /// <param name="requirements">The <see cref="Requirement"/>s to convert.</param>
        /// <returns>
        /// An <see cref="IEnumerable{T}"/> of <see cref="Requirement"/>s with proportions
        /// adjusted for <see cref="AtmosphericPressure"/>.
        /// </returns>
        public IEnumerable<Requirement> ConvertRequirementsForPressure(IEnumerable<Requirement> requirements)
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
            bool _canHaveOxygen,
            Chemical chemical,
            double hydrosphereProportion,
            double vaporProportion,
            ref double hydrosphereAtmosphereRatio,
            ref double adjustedAtmosphericPressure)
        {
            if (!(CelestialBody is TerrestrialPlanet planet) || TMath.IsZero(hydrosphereProportion))
            {
                return;
            }

            if (chemical == Chemical.Water)
            {
                planet.Hydrosphere.Homogenize();
            }

            var gasProportion = hydrosphereProportion * hydrosphereAtmosphereRatio;
            var previousGasProportion = vaporProportion;

            SetHydrosphereProportion(chemical, Phase.Any, 0, ref hydrosphereAtmosphereRatio);

            if (chemical == Chemical.Water)
            {
                SetHydrosphereProportion(Chemical.Water_Salt, Phase.Any, 0, ref hydrosphereAtmosphereRatio);

                // It is presumed that photodissociation will eventually reduce the amount of water
                // vapor to a trace gas (the H2 will be lost due to atmospheric escape, and the
                // oxygen will be lost to surface oxidation).
                var waterVapor = Math.Min(gasProportion, Math.Round(Randomizer.Static.NextDouble(0.001), 4));
                gasProportion = waterVapor;

                previousGasProportion += Composition.GetProportion(Chemical.Oxygen, Phase.Gas);
                var o2 = Math.Round(gasProportion * 0.0001, 5);
                gasProportion += o2;

                if (Composition is LayeredComposite lc)
                {
                    for (var i = 0; i < lc.Layers.Count; i++)
                    {
                        lc.AddToLayer(i, chemical, Phase.Gas, Math.Max(lc.Layers[i].substance.GetProportion(chemical, Phase.Gas), waterVapor));

                        // Some is added as oxygen, due to photodissociation.
                        if (_canHaveOxygen)
                        {
                            lc.AddToLayer(i, Chemical.Oxygen, Phase.Gas, Math.Max(lc.Layers[i].substance.GetProportion(Chemical.Oxygen, Phase.Gas), o2));
                        }
                    }
                }
                else
                {
                    Composition = Composition.AddComponent(chemical, Phase.Gas, Math.Max(Composition.GetProportion(chemical, Phase.Gas), waterVapor));
                    if (_canHaveOxygen)
                    {
                        Composition = Composition.AddComponent(Chemical.Oxygen, Phase.Gas, Math.Max(Composition.GetProportion(Chemical.Oxygen, Phase.Gas), o2));
                    }
                }
            }
            else
            {
                Composition = Composition.AddComponent(chemical, Phase.Gas, gasProportion);
            }

            adjustedAtmosphericPressure += adjustedAtmosphericPressure * (gasProportion - previousGasProportion);
        }

        /// <summary>
        /// Performs the Exner function on the given pressure.
        /// </summary>
        /// <param name="pressure">A pressure, in kPa.</param>
        /// <returns>The non-dimensionalized pressure.</returns>
        internal double Exner(double pressure) => Math.Pow(pressure / AtmosphericPressure, ScienceConstants.RSpecificOverCpDryAir);

        /// <summary>
        /// Calculates the air mass coefficient at the given latitude and elevation.
        /// </summary>
        /// <param name="latitude">A latitude, in radians.</param>
        /// <param name="elevation">An elevation, in meters.</param>
        /// <param name="cosLatitude">
        /// Optionally, the cosine of the given latitude. If omitted, it will be calculated.
        /// </param>
        /// <returns>The air mass coefficient at the given latitude.</returns>
        private double GetAirMass(double latitude, double elevation, double? cosLatitude = null)
        {
            var r = CelestialBody.Radius / AtmosphericScaleHeight;
            var cosLat = cosLatitude ?? Math.Cos(latitude);
            var c = elevation / AtmosphericScaleHeight;
            var rPlusCcosLat = (r + c) * cosLat;
            return Math.Sqrt((rPlusCcosLat * rPlusCcosLat) + (((2 * r) + 1 + c) * (1 - c))) - rPlusCcosLat;
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
        public double GetAtmosphericDrag(double density, double speed) =>
            0.235 * density * speed * speed * CelestialBody.Radius;

        /// <summary>
        /// Calculates the total height of the <see cref="Atmosphere"/>, defined as the point at
        /// which atmospheric pressure is roughly equal to 0.01 Pa, in meters.
        /// </summary>
        /// <returns>The height of the atmosphere, in meters.</returns>
        /// <remarks>
        /// Uses the molar mass of air on Earth, which is clearly not correct for other atmospheres,
        /// but is considered "close enough" for the purposes of this library.
        /// </remarks>
        private double GetAtmosphericHeight()
            => Math.Log(1.0e-5 / AtmosphericPressure) * ScienceConstants.R * GetSurfaceTemperatureAverageOrbital() / (-CelestialBody.SurfaceGravity * ScienceConstants.MAir);

        /// <summary>
        /// Calculates the total mass of this <see cref="Atmosphere"/>, in kg.
        /// </summary>
        /// <returns>The total mass of this <see cref="Atmosphere"/>, in kg.</returns>
        private double GetAtmosphericMass()
            => MathConstants.FourPI * CelestialBody.RadiusSquared * AtmosphericPressure * 1000 / CelestialBody.SurfaceGravity;

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
        internal double GetAtmosphericPressure(double temperature, double elevation)
        {
            if (elevation <= 0)
            {
                return AtmosphericPressure;
            }
            else
            {
                return AtmosphericPressure * Math.Exp(CelestialBody.SurfaceGravity * ScienceConstants.MAir * elevation / (ScienceConstants.R * temperature));
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
        private double GetAtmosphericScaleHeight()
            => AtmosphericPressure * 1000 / (CelestialBody.SurfaceGravity * GetAtmosphericDensity(CelestialBody.GetTotalTemperature(), AtmosphericPressure));

        /// <summary>
        /// Calculates the total greenhouse effect for this <see cref="Atmosphere"/>, in K.
        /// </summary>
        /// <returns>The total greenhouse effect for this <see cref="Atmosphere"/>, in K.</returns>
        private double GetGreenhouseEffect()
        {
            var avgTemp = CelestialBody.GetTotalTemperatureAverageOrbital();
            return (avgTemp * InsolationFactor_Equatorial * GreenhouseFactor) - avgTemp;
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
        private double GetGreenhouseFactor()
        {
            var total = Composition.GetGreenhousePotential();
            if (TMath.IsZero(total))
            {
                return 1;
            }
            else
            {
                return 0.933835 + (0.0441533 * Math.Exp(1.79077 * total) * (1.11169 + Math.Log(AtmosphericPressure)));
            }
        }

        internal double GetHeightForTemperature(double temperature, double surfaceTemp, double elevation)
            => ((surfaceTemp - temperature) / GetLapseRate(surfaceTemp)) - elevation;

        /// <summary>
        /// Calculates the insolation factor to be used at the predetermined latitude for checking temperatures.
        /// </summary>
        /// <param name="polar">Whether or not to get the polar value.</param>
        /// <returns>
        /// The insolation factor to be used at the predetermined latitude for checking temperatures.
        /// </returns>
        private double GetInsolationFactor(bool polar = false)
        {
            var airMass = GetAirMass(polar ? CelestialBody.PolarLatitude : 0, 0, polar ? CelestialBody.CosPolarLatitude : 1);

            var atmMassRatio = Mass / CelestialBody.Mass;
            return Math.Pow(1320000 * atmMassRatio * Math.Pow(0.7, Math.Pow(airMass, 0.678)), 0.25);
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
        private double GetLapseRate(double surfaceTemp) => Composition.ContainsSubstance(Chemical.Water, Phase.Gas) ? GetLapseRateMoist(surfaceTemp) : DryLapseRate;

        /// <summary>
        /// Calculates the dry adiabatic lapse rate near the surface of this <see cref="Atmosphere"/>, in K/m.
        /// </summary>
        /// <returns>The dry adiabatic lapse rate near the surface of this <see cref="Atmosphere"/>, in K/m.</returns>
        private double GetLapseRateDry() => CelestialBody.SurfaceGravity / ScienceConstants.CpDryAir;

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
        private double GetLapseRateMoist(double surfaceTemp)
        {
            var surfaceTemp2 = surfaceTemp * surfaceTemp;
            var gasConstantSurfaceTemp2 = ScienceConstants.RSpecificDryAir * surfaceTemp2;

            var waterRatio = Composition.GetProportion(Chemical.Water, Phase.Gas);

            var numerator = gasConstantSurfaceTemp2 + (ScienceConstants.DeltaHvapWater * waterRatio * surfaceTemp);
            var denominator = (ScienceConstants.CpTimesRSpecificDryAir * surfaceTemp2)
                + (ScienceConstants.DeltaHvapWaterSquared * waterRatio * ScienceConstants.RSpecificRatioOfDryAirToWater);

            return CelestialBody.SurfaceGravity * (numerator / denominator);
        }

        /// <summary>
        /// Calculates the <see cref="Atmosphere"/>'s shape.
        /// </summary>
        private Shape GetShape() => new HollowSphere(CelestialBody.Radius, GetAtmosphericHeight());

        /// <summary>
        /// Calculates the effective surface temperature, including greenhouse effects, in K.
        /// </summary>
        /// <param name="polar">
        /// If true, calculates the approximate temperature at the <see cref="CelestialBody"/>'s poles.
        /// </param>
        /// <returns>The surface temperature, in K.</returns>
        internal double GetSurfaceTemperature(bool polar = false)
            => (CelestialBody.GetTotalTemperature() * (polar ? InsolationFactor_Polar : InsolationFactor_Equatorial)) + GreenhouseEffect;

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
        internal double GetSurfaceTemperatureAtPosition(Vector3 position, bool polar = false)
            => (CelestialBody.GetTotalTemperatureFromPosition(position) * (polar ? InsolationFactor_Polar : InsolationFactor_Equatorial)) + GreenhouseEffect;

        /// <summary>
        /// Returns the effective surface temperature, including greenhouse effects, as if the body
        /// was at apoapsis, in K.
        /// </summary>
        /// <param name="polar">
        /// If true, gets the approximate temperature at the <see cref="CelestialBody"/>'s poles.
        /// </param>
        /// <returns>The effective surface temperature at apoapsis, in K.</returns>
        internal double GetSurfaceTemperatureAtApoapsis(bool polar = false)
            => (CelestialBody.TotalTemperatureAtApoapsis * (polar ? InsolationFactor_Polar : InsolationFactor_Equatorial)) + GreenhouseEffect;

        /// <summary>
        /// Returns the effective surface temperature, including greenhouse effects, as if the body
        /// was at periapsis, in K.
        /// </summary>
        /// <param name="polar">
        /// If true, gets the approximate temperature at the <see cref="CelestialBody"/>'s poles.
        /// </param>
        /// <returns>The effective surface temperature at periapsis, in K.</returns>
        internal double GetSurfaceTemperatureAtPeriapsis(bool polar = false)
            => (CelestialBody.TotalTemperatureAtPeriapsis * (polar ? InsolationFactor_Polar : InsolationFactor_Equatorial)) + GreenhouseEffect;

        /// <summary>
        /// Returns the effective surface temperature, including greenhouse effects, averaged between
        /// periapsis and apoapsis, in K.
        /// </summary>
        /// <param name="polar">
        /// If true, gets the approximate temperature at the <see cref="CelestialBody"/>'s poles.
        /// </param>
        /// <returns>The average effective surface temperature.</returns>
        internal double GetSurfaceTemperatureAverageOrbital(bool polar = false)
        {
            // If the body is not actually in orbit, return its current temperature.
            if (CelestialBody.Orbit == null)
            {
                return GetSurfaceTemperature(polar);
            }
            else
            {
                return (GetSurfaceTemperatureAtPeriapsis(polar) + GetSurfaceTemperatureAtApoapsis(polar)) / 2.0;
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
        internal double GetTemperatureAtElevation(double surfaceTemp, double elevation)
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
                return surfaceTemp - (elevation * GetLapseRate(surfaceTemp));
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
        internal IComposition GetTroposphere()
        {
            if (Composition is LayeredComposite lc)
            {
                return lc.Layers[0].substance;
            }
            else
            {
                // Separate troposphere from upper atmosphere if undifferentiated.
                Composition = Composition.Split(0.8, 0.2);

                return (Composition as LayeredComposite)?.Layers[0].substance;
            }
        }

        /// <summary>
        /// Determines if this <see cref="Atmosphere"/> meets the given requirements.
        /// </summary>
        /// <param name="requirements">An enumeration of <see cref="Requirement"/>s.</param>
        /// <returns>true if this <see cref="Atmosphere"/> meets the requirements; false otherwise.</returns>
        public bool MeetsRequirements(IEnumerable<Requirement> requirements)
        {
            var surfaceLayer = (Composition is LayeredComposite lc) ? lc.Layers[0].substance : Composition;
            return surfaceLayer
                .GetFailedRequirements(requirements)
                .All(x => x == FailureType.None);
        }

        internal void ResetGreenhouseFactor() => _greenhouseFactor = null;

        internal void ResetPressureDependentProperties()
        {
            _atmosphericScaleHeight = null;
            _greenhouseEffect = null;
            _insolationFactor_Polar = null;
            _insolationFactor_Equatorial = null;
            Mass = GetAtmosphericMass();
            Shape = GetShape();
        }

        internal void ResetTemperatureDependentProperties()
        {
            _atmosphericScaleHeight = null;
            _greenhouseEffect = null;
            _insolationFactor_Polar = null;
            _insolationFactor_Equatorial = null;
            Shape = GetShape();
        }

        private void SetHydrosphereProportion(
            Chemical chemical,
            Phase phase,
            double proportion,
            ref double hydrosphereAtmosphereRatio)
        {
            if (!(CelestialBody is TerrestrialPlanet planet))
            {
                return;
            }

            var newTotalProportion = proportion;
            if (planet.Hydrosphere is LayeredComposite lc)
            {
                newTotalProportion = lc.Layers[lc.Layers.Count - 1].proportion * proportion;
            }
            planet.HydrosphereProportion +=
                planet.HydrosphereProportion * (newTotalProportion - planet.Hydrosphere.GetProportion(chemical, phase));

            if (planet.Hydrosphere is LayeredComposite lc2)
            {
                lc2.AddToLayer(lc2.Layers.Count - 1, chemical, phase, proportion);
            }
            else
            {
                planet.Hydrosphere.SetProportion(chemical, phase, proportion);
            }

            if (CelestialBody is TerrestrialPlanet t)
            {
                hydrosphereAtmosphereRatio = t.GetHydrosphereAtmosphereRatio();
            }
        }

        /// <summary>
        /// A range of acceptable amounts of O2, and list of maximum limits of common
        /// atmospheric gases for acceptable human breathability.
        /// </summary>
        public static List<Requirement> HumanBreathabilityRequirements = new List<Requirement>()
        {
            new Requirement { Chemical = Chemical.Oxygen, MinimumProportion = 0.07, MaximumProportion = 0.53, Phase = Phase.Gas },
            new Requirement { Chemical = Chemical.Ammonia, MaximumProportion = 0.00005 },
            new Requirement { Chemical = Chemical.AmmoniumHydrosulfide, MaximumProportion = 0.000001 },
            new Requirement { Chemical = Chemical.CarbonMonoxide, MaximumProportion = 0.00005 },
            new Requirement { Chemical = Chemical.CarbonDioxide, MaximumProportion = 0.005 },
            new Requirement { Chemical = Chemical.HydrogenSulfide, MaximumProportion = 0.0 },
            new Requirement { Chemical = Chemical.Methane, MaximumProportion = 0.001 },
            new Requirement { Chemical = Chemical.Ozone, MaximumProportion = 0.0000001 },
            new Requirement { Chemical = Chemical.Phosphine, MaximumProportion = 0.0000003 },
            new Requirement { Chemical = Chemical.SulphurDioxide, MaximumProportion = 0.000002 },
        };
    }
}
