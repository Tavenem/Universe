using System;
using System.Collections.Generic;
using System.Linq;
using Troschuetz.Random;
using WorldFoundry.CelestialBodies;
using WorldFoundry.Substances;

namespace WorldFoundry.Climate
{
    /// <summary>
    /// Represents a planetary atmosphere, represented as a <see cref="Mixture"/>.
    /// </summary>
    public class Atmosphere : Mixture
    {
        /// <summary>
        /// Specifies the atmospheric pressure at the surface of the planetary body (in kPa).
        /// </summary>
        public float AtmosphericPressure { get; internal set; }

        /// <summary>
        /// The <see cref="CelestialBodies.CelestialBody"/> this <see cref="Atmosphere"/> surrounds.
        /// </summary>
        public CelestialBody CelestialBody { get; private set; }

        private float? _greenhouseTemperatureMultiplier;
        /// <summary>
        /// The total greenhouse temperature multiplier for this <see cref="Atmosphere"/>.
        /// </summary>
        internal float GreenhouseTemperatureMultiplier
        {
            get
            {
                if (!_greenhouseTemperatureMultiplier.HasValue)
                {
                    _greenhouseTemperatureMultiplier = GetGreenhouseTemperatureMultiplier();
                }
                return _greenhouseTemperatureMultiplier.Value;
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
        /// Calculates atmospheric density for a given temperature and pressure (in kg/m³).
        /// </summary>
        /// <param name="temperature">A temperature, in K.</param>
        /// <param name="pressure">A pressure, in kPa.</param>
        /// <returns>The atmospheric density for the given temperature and pressure (in kg/m³).</returns>
        public static float GetAtmosphericDensity(float temperature, float pressure)
            => pressure * 1000 / (287.058f * temperature);

        /// <summary>
        /// Standard pressure of 101.325 kPa is presumed for a <see cref="SubstanceRequirement"/>.
        /// This method converts the proportional values to reflect <see cref="AtmosphericPressure"/>.
        /// </summary>
        /// <param name="requirement">The <see cref="SubstanceRequirement"/> to convert.</param>
        /// <returns>
        /// A new <see cref="SubstanceRequirement"/> with proportions adjusted for <see cref="AtmosphericPressure"/>.
        /// </returns>
        public SubstanceRequirement ConvertRequirementForPressure(SubstanceRequirement requirement)
        {
            float minActual = requirement.MinimumProportion * Utilities.Science.Constants.StandardAtmosphericPressure;
            float? maxActual = requirement.MaximumProportion.HasValue ? requirement.MaximumProportion * Utilities.Science.Constants.StandardAtmosphericPressure : null;
            return new SubstanceRequirement
            {
                Chemical = requirement.Chemical,
                MaximumProportion = maxActual.HasValue ? maxActual / AtmosphericPressure : null,
                MinimumProportion = minActual / AtmosphericPressure,
                Phase = requirement.Phase,
            };
        }

        /// <summary>
        /// Standard pressure of 101.325 kPa is presumed for <see cref="SubstanceRequirement"/>s.
        /// This method converts the proportional values to reflect <see cref="AtmosphericPressure"/>.
        /// </summary>
        /// <param name="requirements">The <see cref="SubstanceRequirement"/>s to convert.</param>
        /// <returns>
        /// An <see cref="IEnumerable{T}"/> of <see cref="SubstanceRequirement"/>s with proportions
        /// adjusted for <see cref="AtmosphericPressure"/>.
        /// </returns>
        public IEnumerable<SubstanceRequirement> ConvertRequirementsForPressure(IEnumerable<SubstanceRequirement> requirements)
        {
            foreach (var requirement in requirements)
            {
                yield return ConvertRequirementForPressure(requirement);
            }
        }

        /// <summary>
        /// Calculates the greenhouse effect of the atmosphere and returns the effective surface
        /// temperature (in K), as if the body was at apoapsis.
        /// </summary>
        /// <param name="polar">
        /// If true, calculates the approximate temperature at the <see cref="CelestialBody"/>'s poles.
        /// </param>
        /// <returns>The effective surface temperature at apoapsis.</returns>
        public float GetApoapsisTemperature(bool polar = false) =>
            CelestialBody.GetTotalTemperatureAtApoapsis(polar) * GreenhouseTemperatureMultiplier;

        /// <summary>
        /// Calculates the atmospheric density at a given elevation (in kg/m³).
        /// </summary>
        /// <param name="elevation">An elevation above the <see cref="CelestialBody"/>'s surface (in meters).</param>
        /// <param name="temperature">
        /// An optional temperature, in K. If omitted, the temperature at the given elevation is calculated.
        /// </param>
        /// <param name="temperature">
        /// An optional pressure, in kPa. If omitted, the pressure at the given elevation is calculated.
        /// </param>
        /// <returns>The atmospheric density at the specified height (in kg/m³).</returns>
        /// <remarks>
        /// Uses the molar mass of air on Earth, which is clearly not correct for other atmospheres,
        /// but is considered "close enough" for the purposes of this library.
        /// </remarks>
        private float GetAtmosphericDensityAtElevation(float elevation, float? temperature = null, float? pressure = null)
        {
            var temp = temperature ?? GetTemperatureAtElevation(elevation);
            return (pressure ?? GetAtmosphericPressureAtElevation(elevation, temp)) * 1000 / (287.058f * temp);
        }

        /// <summary>
        /// Calculates the total height of the <see cref="Atmosphere"/>, defined as the point at
        /// which atmospheric pressure is roughly equal to 1 Pa.
        /// </summary>
        /// <returns>The height of the atmosphere.</returns>
        /// <remarks>
        /// Uses the molar mass of air on Earth, which is clearly not correct for other atmospheres,
        /// but is considered "close enough" for the purposes of this library.
        /// </remarks>
        private float GetAtmosphericHeight()
            => (float)((Math.Log(0.001) * Utilities.Science.Constants.R * GetSurfaceTemperature()) / (-CelestialBody.SurfaceGravity * Utilities.Science.Constants.MolarMass_Air));

        /// <summary>
        /// Calculates the total mass of this <see cref="Atmosphere"/> (in kg).
        /// </summary>
        /// <returns>The total mass of this <see cref="Atmosphere"/>, in kg.</returns>
        internal double GetAtmosphericMass()
            => (Utilities.MathUtil.Constants.FourPI * Math.Pow(CelestialBody.Radius, 2) * AtmosphericPressure * 1000) / CelestialBody.SurfaceGravity;

        /// <summary>
        /// Calculates the atmospheric pressure at a given elevation (in kPa).
        /// </summary>
        /// <param name="elevation">
        /// An elevation above the <see cref="CelestialBody"/>'s surface (in meters).
        /// </param>
        /// <param name="temperature">
        /// An optional temperature, in K. If omitted, the temperature at the given elevation is calculated.
        /// </param>
        /// <returns>The atmospheric pressure (in kPa) at the specified height.</returns>
        /// <remarks>
        /// Uses the molar mass of air on Earth, which is clearly not correct for other atmospheres,
        /// but is considered "close enough" for the purposes of this library.
        /// </remarks>
        private float GetAtmosphericPressureAtElevation(float elevation, float? temperature = null)
        {
            if (elevation <= 0)
            {
                return AtmosphericPressure;
            }
            else
            {
                return AtmosphericPressure * (float)Math.Exp(-CelestialBody.SurfaceGravity * Utilities.Science.Constants.MolarMass_Air * elevation / (Utilities.Science.Constants.R * (temperature ?? GetTemperatureAtElevation(elevation))));
            }
        }

        /// <summary>
        /// Calculates the greenhouse effect of the atmosphere and returns the
        /// effective surface temperature (in K), averaged between periapsis
        /// and apoapsis.
        /// </summary>
        /// <param name="polar">
        /// If true, calculates the approximate temperature at the <see cref="CelestialBody"/>'s poles.
        /// </param>
        /// <returns>The average effective surface temperature.</returns>
        public float GetAverageOrbitalTemperature(bool polar = false)
        {
            // Only bother calculating twice if the body is actually in orbit.
            if (CelestialBody.Orbit == null)
            {
                return GetSurfaceTemperature();
            }
            else
            {
                return (GetPeriapsisTemperature(polar) + GetApoapsisTemperature(polar)) / 2.0f;
            }
        }

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
            foreach (var mixture in Mixtures)
            {
                clouds = Math.Max(clouds, mixture.Components
                    .Where(c => c.Substance.Phase == Phase.Liquid || c.Substance.Phase == Phase.Solid)
                    .Sum(c => c.Proportion));
            }
            return clouds;
        }

        /// <summary>
        /// Calculates the atmospheric drag on a spherical object at a given elevation (in N).
        /// </summary>
        /// <param name="elevation">An elevation above the body's surface (in meters).</param>
        /// <param name="speed">The speed of the object (in m/s).</param>
        /// <returns>The atmospheric drag on the object (in N) at the specified height.</returns>
        /// <remarks>
        /// 0.47 is an arbitrary drag coefficient (that of a sphere in a fluid with a Reynolds number of 10⁴),
        /// which may not reflect the actual conditions at all, but the inaccuracy is accepted since the
        /// level of detailed information needed to calculate this value accurately is not desired
        /// in this library.
        /// </remarks>
        public float GetDragAtElevation(float elevation, float speed) =>
            0.5f * GetAtmosphericDensityAtElevation(elevation) * speed * speed * 0.47f * CelestialBody.Radius;

        /// <summary>
        /// Calculates the dry adiabatic lapse rate near the surface of this <see cref="Atmosphere"/> (in K/m).
        /// </summary>
        /// <returns>The dry adiabatic lapse rate near the surface of this <see cref="Atmosphere"/>, in K/m.</returns>
        private float GetDryLapseRate() => (float)(CelestialBody.SurfaceGravity / Utilities.Science.Constants.SpecificHeat_DryAir);

        /// <summary>
        /// Calculates the total greenhouse temperature multiplier for this <see cref="Atmosphere"/>.
        /// </summary>
        /// <returns>The total greenhouse temperature multiplier for this <see cref="Atmosphere"/>.</returns>
        private float GetGreenhouseTemperatureMultiplier()
        {
            var total = 0.0;
            foreach (var mixture in Mixtures)
            {
                total += mixture.Components
                    .Where(c => c.Substance.Chemical.GreenhousePotential > 0)
                    .Sum(c => c.Substance.Chemical.GreenhousePotential * 0.27 * Math.Sqrt(c.Proportion * AtmosphericPressure));
            }
            if (TMath.IsZero(total))
            {
                return 1;
            }
            else
            {
                return (float)Math.Pow((total * 0.75) + 1, 0.25);
            }
        }

        /// <summary>
        /// Calculates the adiabatic lapse rate for this <see cref="Atmosphere"/>, after determining
        /// whether to use the dry or moist based on the presence of water vapor (in K/m).
        /// </summary>
        /// <returns>The adiabatic lapse rate for this <see cref="Atmosphere"/>, in K/m.</returns>
        /// <remarks>
        /// Uses the specific heat and gas constant of dry air on Earth, which is clearly not correct
        /// for other atmospheres, but is considered "close enough" for the purposes of this library.
        /// </remarks>
        private float GetLapseRate() => ContainsSubstance(Chemical.Water, Phase.Gas) ? GetMoistLapseRate() : GetDryLapseRate();

        /// <summary>
        /// Calculates the moist adiabatic lapse rate near the surface of this <see
        /// cref="Atmosphere"/> (in K/m).
        /// </summary>
        /// <returns>
        /// The moist adiabatic lapse rate near the surface of this <see cref="Atmosphere"/>, in K/m.
        /// </returns>
        /// <remarks>
        /// Uses the specific heat and gas constant of dry air on Earth, which is clearly not correct
        /// for other atmospheres, but is considered "close enough" for the purposes of this library.
        /// </remarks>
        private float GetMoistLapseRate()
        {
            var surfaceTemp = GetSurfaceTemperature();
            var surfaceTemp2 = surfaceTemp * surfaceTemp;
            var gasConstantSurfaceTemp2 = Utilities.Science.Constants.SpecificGasConstant_DryAir * surfaceTemp2;

            var waterRatio = GetSubstanceProportionInAllChildren(Chemical.Water, Phase.Gas);

            var numerator = gasConstantSurfaceTemp2 + Utilities.Science.Constants.HeatOfVaporization_Water * waterRatio * surfaceTemp;
            var denominator = Utilities.Science.Constants.SpecificHeatTimesSpecificGasConstant_DryAir * surfaceTemp2
                + Utilities.Science.Constants.HeatOfVaporization_Water_Squared * waterRatio * Utilities.Science.Constants.SpecificGasConstant_Ratio_DryAirToWater;

            return (float)(CelestialBody.SurfaceGravity * (numerator / denominator));
        }

        /// <summary>
        /// Calculates the greenhouse effect of the atmosphere and returns the effective surface
        /// temperature (in K), as if the body was at periapsis.
        /// </summary>
        /// <param name="polar">
        /// If true, calculates the approximate temperature at the <see cref="CelestialBody"/>'s poles.
        /// </param>
        /// <returns>The effective surface temperature at periapsis.</returns>
        public float GetPeriapsisTemperature(bool polar = false) =>
            CelestialBody.GetTotalTemperatureAtPeriapsis(polar) * GreenhouseTemperatureMultiplier;

        /// <summary>
        /// Calculates the greenhouse effect of this <see cref="Atmosphere"/> and returns the
        /// effective surface temperature at the poles (in K).
        /// </summary>
        /// <returns>The surface temperature at the poles, in K.</returns>
        public float GetPolarSurfaceTemperature() => CelestialBody.GetTotalPolarTemperature() * GreenhouseTemperatureMultiplier;

        /// <summary>
        /// Calculates the greenhouse effect of this <see cref="Atmosphere"/> and returns the
        /// effective surface temperature (in K).
        /// </summary>
        /// <returns>The surface temperature, in K.</returns>
        public float GetSurfaceTemperature() => CelestialBody.GetTotalTemperature() * GreenhouseTemperatureMultiplier;

        /// <summary>
        /// Calculates the temperature of this <see cref="Atmosphere"/> at the given elevation (in K).
        /// </summary>
        /// <returns>
        /// The temperature of this <see cref="Atmosphere"/> at the given elevation, in K.
        /// </returns>
        /// <remarks>
        /// In an Earth-like atmosphere, the temperature lapse rate varies considerably in the
        /// different atmospheric layers, but this cannot be easily modeled for arbitrary
        /// exoplanetary atmospheres, so a simplified formula is used, which should be "close enough"
        /// for low elevations.
        /// </remarks>
        internal float GetTemperatureAtElevation(float elevation)
        {
            // When outside the atmosphere, use the black body temperature, ignoring atmospheric effects.
            if (elevation >= GetAtmosphericHeight())
            {
                return CelestialBody.GetTotalTemperature();
            }

            var surfaceTemp = GetSurfaceTemperature();
            if (elevation <= 0)
            {
                return surfaceTemp;
            }
            else
            {
                return surfaceTemp - elevation * GetLapseRate();
            }
        }

        /// <summary>
        /// A range of acceptable amounts of O2, and list of maximum limits of common
        /// atmospheric gases for acceptable human breathability.
        /// </summary>
        public static List<SubstanceRequirement> HumanBreathabilityRequirements = new List<SubstanceRequirement>()
        {
            new SubstanceRequirement { Chemical = Chemical.Oxygen, MinimumProportion = 0.07f, MaximumProportion = 0.53f, Phase = Phase.Gas },
            new SubstanceRequirement { Chemical = Chemical.Ammonia, MaximumProportion = 0.00005f },
            new SubstanceRequirement { Chemical = Chemical.AmmoniumHydrosulfide, MaximumProportion = 0.000001f },
            new SubstanceRequirement { Chemical = Chemical.CarbonMonoxide, MaximumProportion = 0.00005f },
            new SubstanceRequirement { Chemical = Chemical.CarbonDioxide, MaximumProportion = 0.005f },
            new SubstanceRequirement { Chemical = Chemical.HydrogenSulfide, MaximumProportion = 0.0f },
            new SubstanceRequirement { Chemical = Chemical.Methane, MaximumProportion = 0.001f },
            new SubstanceRequirement { Chemical = Chemical.Ozone, MaximumProportion = 0.0000001f },
            new SubstanceRequirement { Chemical = Chemical.Phosphine, MaximumProportion = 0.0000003f },
            new SubstanceRequirement { Chemical = Chemical.SulphurDioxide, MaximumProportion = 0.000002f },
        };
    }
}
