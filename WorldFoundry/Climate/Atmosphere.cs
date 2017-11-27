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
            set => _atmosphericPressure = value;
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

        private float? _polarInsolationFactor;
        /// <summary>
        /// The insolation factor to be used at the predetermined latitude for checking polar temperatures.
        /// </summary>
        private float PolarInsolationFactor
        {
            get
            {
                if (!_polarInsolationFactor.HasValue)
                {
                    _polarInsolationFactor = GetPolarInsolationFactor();
                }
                return _polarInsolationFactor.Value;
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
        internal static float GetAtmosphericDensity(float pressure, float temperature)
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
            var a = temperature > Season.freezingPoint
                ? 611.21
                : 611.15;
            var b = temperature > Season.freezingPoint
                ? 18.678
                : 23.036;
            var c = temperature > Season.freezingPoint
                ? 234.5
                : 333.7;
            var d = temperature > Season.freezingPoint
                ? 257.14
                : 279.82;
            var t = temperature - Season.freezingPoint;
            return (float)(a * Math.Exp((b - (t / c)) * (t / (d + t))));
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
            foreach (var requirement in requirements)
            {
                yield return ConvertRequirementForPressure(requirement);
            }
        }

        /// <summary>
        /// Performs the Exner function on the given pressure.
        /// </summary>
        /// <param name="pressure">A pressure, in kPa.</param>
        /// <returns>The non-dimensionalized pressure.</returns>
        internal float Exner(float pressure) => (float)Math.Pow(pressure / AtmosphericPressure, Utilities.Science.Constants.SpecificGasConstantDivSpecificHeat_DryAir);

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
            0.5f * density * speed * speed * 0.47f * CelestialBody.Radius;

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
            => (float)((Math.Log(1.0e-5) * Utilities.Science.Constants.R * GetSurfaceTemperatureAverageOrbital()) / (AtmosphericPressure * -CelestialBody.SurfaceGravity * Utilities.Science.Constants.MolarMass_Air));

        /// <summary>
        /// Calculates the total mass of this <see cref="Atmosphere"/>, in kg.
        /// </summary>
        /// <returns>The total mass of this <see cref="Atmosphere"/>, in kg.</returns>
        private double GetAtmosphericMass()
            => (Utilities.MathUtil.Constants.FourPI * Math.Pow(CelestialBody.Radius, 2) * AtmosphericPressure * 1000) / CelestialBody.SurfaceGravity;

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
        internal float GetAtmosphericPressure(float elevation, float temperature)
        {
            if (elevation <= 0)
            {
                return AtmosphericPressure;
            }
            else
            {
                return AtmosphericPressure * (float)(-CelestialBody.SurfaceGravity * Utilities.Science.Constants.MolarMass_Air * elevation / (Utilities.Science.Constants.R * (temperature)));
            }
        }

        /// <summary>
        /// Calculates the scale height of the <see cref="Atmosphere"/>, in meters.
        /// </summary>
        /// <returns>The scale height of the atmosphere, in meters.</returns>
        /// <remarks>
        private float GetAtmosphericScaleHeight()
            => (float)((AtmosphericPressure * 1000) / (CelestialBody.SurfaceGravity * GetAtmosphericDensity(AtmosphericPressure, GetSurfaceTemperatureAverageOrbital() * GreenhouseFactor)));

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
        /// Calculates the total greenhouse temperature multiplier for this <see cref="Atmosphere"/>.
        /// </summary>
        /// <returns>The total greenhouse temperature multiplier for this <see cref="Atmosphere"/>.</returns>
        private float GetGreenhouseFactor()
        {
            var total = 0.0;
            if (Mixtures != null)
            {
                foreach (var mixture in Mixtures)
                {
                    total += mixture.Components?
                        .Where(c => c.Chemical.GreenhousePotential > 0)
                        .Sum(c => c.Chemical.GreenhousePotential * 0.36 * Math.Exp(c.Proportion * AtmosphericPressure)) ?? 0;
                }
            }
            if (TMath.IsZero(total))
            {
                return 1;
            }
            else
            {
                return (float)Math.Pow(1 / (1 - 0.5 * total), 0.25);
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
        private float GetLapseRateDry() => (float)(CelestialBody.SurfaceGravity / Utilities.Science.Constants.SpecificHeat_DryAir);

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
            var gasConstantSurfaceTemp2 = Utilities.Science.Constants.SpecificGasConstant_DryAir * surfaceTemp2;

            var waterRatio = GetProportion(Chemical.Water, Phase.Gas, true);

            var numerator = gasConstantSurfaceTemp2 + Utilities.Science.Constants.HeatOfVaporization_Water * waterRatio * surfaceTemp;
            var denominator = Utilities.Science.Constants.SpecificHeatTimesSpecificGasConstant_DryAir * surfaceTemp2
                + Utilities.Science.Constants.HeatOfVaporization_Water_Squared * waterRatio * Utilities.Science.Constants.SpecificGasConstant_Ratio_DryAirToWater;

            return (float)(CelestialBody.SurfaceGravity * (numerator / denominator));
        }

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
        /// Calculates the insolation factor to be used at the predetermined latitude for checking polar temperatures.
        /// </summary>
        /// <returns>
        /// The insolation factor to be used at the predetermined latitude for checking polar temperatures.
        /// </returns>
        private float GetPolarInsolationFactor()
        {
            var airMass = GetAirMass(CelestialBody.polarLatitude, 0, CelestialBody.cosPolarLatitude);

            var atmMassRatio = AtmosphericMass / CelestialBody.Mass;
            return (float)Math.Pow(1320000 * atmMassRatio * Math.Pow(0.7, Math.Pow(airMass, 0.678)), 0.25);
        }

        /// <summary>
        /// Calculates the greenhouse effect of this <see cref="Atmosphere"/> and returns the
        /// effective surface temperature, in K.
        /// </summary>
        /// <param name="polar">
        /// If true, calculates the approximate temperature at the <see cref="CelestialBody"/>'s poles.
        /// </param>
        /// <returns>The surface temperature, in K.</returns>
        internal float GetSurfaceTemperature(bool polar = false)
        {
            var baseTemp = CelestialBody.GetTotalTemperature();
            var greenhouseEffect = (baseTemp * GreenhouseFactor) - baseTemp;
            return (polar ? baseTemp * PolarInsolationFactor : baseTemp) + greenhouseEffect;

        }

        /// <summary>
        /// Calculates the greenhouse effect of the atmosphere and returns the effective surface
        /// temperature, as if the body was at apoapsis, in K.
        /// </summary>
        /// <param name="polar">
        /// If true, calculates the approximate temperature at the <see cref="CelestialBody"/>'s poles.
        /// </param>
        /// <returns>The effective surface temperature at apoapsis, in K.</returns>
        internal float GetSurfaceTemperatureAtApoapsis(bool polar = false)
        {
            var baseTemp = CelestialBody.GetTotalTemperatureAtApoapsis();
            var greenhouseEffect = (baseTemp * GreenhouseFactor) - baseTemp;
            return (polar ? baseTemp * PolarInsolationFactor : baseTemp) + greenhouseEffect;
        }

        /// <summary>
        /// Calculates the greenhouse effect of the atmosphere and returns the effective surface
        /// temperature, as if the body was at periapsis, in K.
        /// </summary>
        /// <param name="polar">
        /// If true, calculates the approximate temperature at the <see cref="CelestialBody"/>'s poles.
        /// </param>
        /// <returns>The effective surface temperature at periapsis, in K.</returns>
        internal float GetSurfaceTemperatureAtPeriapsis(bool polar = false)
        {
            var baseTemp = CelestialBody.GetTotalTemperatureAtPeriapsis();
            var greenhouseEffect = (baseTemp * GreenhouseFactor) - baseTemp;
            return (polar ? baseTemp * PolarInsolationFactor : baseTemp) + greenhouseEffect;
        }

        /// <summary>
        /// Calculates the greenhouse effect of the atmosphere and returns the
        /// effective surface temperature, averaged between periapsis
        /// and apoapsis, in K.
        /// </summary>
        /// <param name="polar">
        /// If true, calculates the approximate temperature at the <see cref="CelestialBody"/>'s poles.
        /// </param>
        /// <returns>The average effective surface temperature.</returns>
        internal float GetSurfaceTemperatureAverageOrbital(bool polar = false)
        {
            // Only bother calculating twice if the body is actually in orbit.
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

        internal void ResetPressureDependentProperties()
        {
            _atmosphericHeight = null;
            _atmosphericMass = null;
            _atmosphericScaleHeight = null;
            _greenhouseFactor = null;
            _polarInsolationFactor = null;
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
