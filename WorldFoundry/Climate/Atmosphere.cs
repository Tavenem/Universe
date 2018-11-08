using MathAndScience;
using MathAndScience.Shapes;
using Substances;
using System;
using System.Collections.Generic;
using System.Linq;
using Troschuetz.Random;
using WorldFoundry.CelestialBodies.Planetoids;
using WorldFoundry.CosmicSubstances;

namespace WorldFoundry.Climate
{
    /// <summary>
    /// Represents a planetary atmosphere, represented as a <see cref="Substance"/>.
    /// </summary>
    public class Atmosphere : Substance
    {
        internal const int SnowToRainRatio = 13;
        private const double StandardHeightDensity = 124191.6;

        private static readonly List<Requirement> _humanBreathabilityRequirements = new List<Requirement>()
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
        /// <summary>
        /// A range of acceptable amounts of O2, and list of maximum limits of common
        /// atmospheric gases for acceptable human breathability.
        /// </summary>
        public static IEnumerable<Requirement> HumanBreathabilityRequirements => _humanBreathabilityRequirements;

        internal static readonly double TemperatureAtNearlyZeroSaturationVaporPressure = GetTemperatureAtSaturationVaporPressure(TMath.Tolerance);

        internal double _averageSeaLevelDensity;

        /// <summary>
        /// The proportion of precipitation produced by this atmosphere relative to that of Earth.
        /// </summary>
        private double _precipitationFactor;

        /// <summary>
        /// Specifies the average height of this <see cref="Atmosphere"/>, in meters. Read-only;
        /// derived from the properties of the planet and atmosphere.
        /// </summary>
        public double AtmosphericHeight { get; private set; }

        private double _atmosphericPressure;
        /// <summary>
        /// Specifies the atmospheric pressure at the surface of the planetary body, in kPa.
        /// </summary>
        public double AtmosphericPressure => Composition.IsEmpty ? 0 : _atmosphericPressure;

        /// <summary>
        /// Specifies the average scale height of this <see cref="Atmosphere"/>, in meters.
        /// Read-only; derived from the properties of the planet and atmosphere.
        /// </summary>
        public double AtmosphericScaleHeight { get; private set; }

        private double? _maxPrecipitation;
        /// <summary>
        /// The maximum annual precipitation expected to be produced by this atmosphere.
        /// </summary>
        public double MaxPrecipitation
            => _maxPrecipitation ?? (_maxPrecipitation = AveragePrecipitation * (0.05 + Math.Exp(1.25))).Value;

        private double? _maxSnowfall;
        /// <summary>
        /// The maximum annual snowfall expected to be produced by this atmosphere.
        /// </summary>
        public double MaxSnowfall
            => _maxSnowfall ?? (_maxSnowfall = MaxPrecipitation * SnowToRainRatio).Value;

        /// <summary>
        /// The average annual precipitation expected to be produced by this atmosphere.
        /// </summary>
        internal double AveragePrecipitation { get; private set; }

        private bool? _containsWaterVapor;
        internal bool ContainsWaterVapor
            => _containsWaterVapor ?? (_containsWaterVapor = Composition?.ContainsSubstance(Chemical.Water, Phase.Gas) ?? false).Value;

        private double? _greenhouseFactor;
        /// <summary>
        /// The total greenhouse factor for this <see cref="Atmosphere"/>.
        /// </summary>
        internal double GreenhouseFactor
        {
            get
            {
                if (!_greenhouseFactor.HasValue)
                {
                    SetGreenhouseFactor();
                }
                return _greenhouseFactor.Value;
            }
        }

        private double? _waterRatio;
        internal double WaterRatio
            => _waterRatio ?? (_waterRatio = Composition?.GetProportion(Chemical.Water, Phase.Gas) ?? 0).Value;

        private double? _wetness;
        /// <summary>
        /// The total mass of water in this atmosphere relative to that of Earth.
        /// </summary>
        internal double Wetness
            => _wetness ?? (_wetness = WaterRatio * Mass / 1.287e16).Value;

        /// <summary>
        /// Initializes a new instance of <see cref="Atmosphere"/>.
        /// </summary>
        private Atmosphere() { }

        /// <summary>
        /// Initializes a new instance of <see cref="Atmosphere"/>.
        /// </summary>
        /// <param name="planet">The <see cref="Planetoid"/> this <see cref="Atmosphere"/> surrounds.</param>
        /// <param name="composition">The <see cref="IComposition"/> which defines this <see cref="Atmosphere"/>.</param>
        /// <param name="pressure">The atmospheric pressure at the surface of the planetary body, in kPa.</param>
        public Atmosphere(Planetoid planet, IComposition composition, double pressure)
        {
            Composition = composition;
            _atmosphericPressure = pressure;
            SetAtmosphericScaleHeight(planet);
            Mass = GetAtmosphericMass(planet);
            SetGreenhouseFactor();

            planet.InsolationFactor_Equatorial = planet.GetInsolationFactor(Mass, AtmosphericScaleHeight);
            var tIF = planet.AverageBlackbodySurfaceTemperature * planet.InsolationFactor_Equatorial;
            planet._greenhouseEffect = (tIF * _greenhouseFactor.Value) - planet.AverageBlackbodySurfaceTemperature;
            var temp = tIF + planet.GreenhouseEffect;
            SetAtmosphericHeight(planet, temp);
            SetPrecipitation(planet);
            Shape = GetShape(planet);

            _averageSeaLevelDensity = GetAtmosphericDensity(temp, AtmosphericPressure);
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

        private static double GetTemperatureAtSaturationVaporPressure(double satVaporPressure)
            => (237.3 / ((7.5 / Math.Log10(satVaporPressure / 611.15)) - 1)) + Chemical.Water.MeltingPoint;

        /// <summary>
        /// Calculates the atmospheric drag on a spherical object within this <see
        /// cref="Atmosphere"/> under given conditions, in N.
        /// </summary>
        /// <param name="planet">The <see cref="Planetoid"/> on which the calculation is being
        /// made.</param>
        /// <param name="temperature">The surface temperature at the object's location.</param>
        /// <param name="altitude">The altitude of the object.</param>
        /// <param name="speed">The speed of the object, in m/s.</param>
        /// <returns>The atmospheric drag on the object at the specified height, in N.</returns>
        /// <remarks>
        /// 0.47 is an arbitrary drag coefficient (that of a sphere in a fluid with a Reynolds
        /// number of 10⁴), which may not reflect the actual conditions at all, but the inaccuracy
        /// is accepted since the level of detailed information needed to calculate this value
        /// accurately is not desired in this library.
        /// </remarks>
        public double GetAtmosphericDrag(Planetoid planet, double temperature, double altitude, double speed) =>
            0.235 * GetAtmosphericDensity(temperature, GetAtmosphericPressure(planet, temperature, altitude)) * speed * speed * planet.Shape.ContainingRadius;

        /// <summary>
        /// Calculates the atmospheric pressure at a given elevation, in kPa.
        /// </summary>
        /// <param name="planet">The <see cref="Planetoid"/> on which the calculation is being
        /// made.</param>
        /// <param name="elevation">
        /// An elevation above the reference elevation for standard atmospheric pressure (sea
        /// level), in meters.
        /// </param>
        /// <param name="temperature">The temperature at the given elevation, in K.</param>
        /// <returns>The atmospheric pressure at the specified height, in kPa.</returns>
        /// <remarks>
        /// In an Earth-like atmosphere, the pressure lapse rate varies considerably in the
        /// different atmospheric layers, but this cannot be easily modeled for arbitrary
        /// exoplanetary atmospheres, so the simple barometric formula is used, which should be
        /// "close enough" for the purposes of this library. Also, this calculation uses the molar
        /// mass of air on Earth, which is clearly not correct for other atmospheres, but is
        /// considered "close enough" for the purposes of this library.
        /// </remarks>
        public double GetAtmosphericPressure(Planetoid planet, double temperature, double elevation)
        {
            if (elevation <= 0)
            {
                return AtmosphericPressure;
            }
            else
            {
                return AtmosphericPressure * Math.Exp(planet.SurfaceGravity * ScienceConstants.MAir * elevation / (ScienceConstants.R * temperature));
            }
        }

        /// <summary>
        /// Sets the atmospheric pressure of this atmosphere to the given <paramref name="value"/>.
        /// </summary>
        /// <param name="planet">The <see cref="Planetoid"/> which this atmosphere surrounds;
        /// required in order to correctly reset the properties dependent on pressure.</param>
        /// <param name="value">The new pressure, in kPa.</param>
        public void SetAtmosphericPressure(Planetoid planet, double value)
        {
            _atmosphericPressure = value;
            ResetPressureDependentProperties(planet);
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

        internal void AddToTroposphere(Chemical chemical, Phase phase, double proportion)
        {
            DifferentiateTroposphere();
            var layers = ((LayeredComposite)Composition).Layers.ToList();
            layers[0] = (layers[0].substance.AddComponent(chemical, phase, proportion), layers[0].proportion);
            Composition = new LayeredComposite(layers);
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
        internal IEnumerable<Requirement> ConvertRequirementsForPressure(IEnumerable<Requirement> requirements)
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

        /// <summary>
        /// Performs the Exner function on the given pressure.
        /// </summary>
        /// <param name="pressure">A pressure, in kPa.</param>
        /// <returns>The non-dimensionalized pressure.</returns>
        internal double Exner(double pressure) => Math.Pow(pressure / AtmosphericPressure, ScienceConstants.RSpecificOverCpDryAir);

        internal void DifferentiateTroposphere()
        {
            if (!(Composition is LayeredComposite lc))
            {
                // Separate troposphere from upper atmosphere if undifferentiated.
                Composition = Composition.Split(0.8, 0.2);
            }
        }

        internal void ResetGreenhouseFactor(Planetoid planet)
        {
            _greenhouseFactor = null;
            planet.ResetGreenhouseEffect();
        }

        internal void ResetPressureDependentProperties(Planetoid planet)
        {
            SetAtmosphericScaleHeight(planet);
            Mass = GetAtmosphericMass(planet);
            SetAtmosphericHeight(planet);
            Shape = GetShape(planet);
            _averageSeaLevelDensity = GetAtmosphericDensity(planet.AverageSurfaceTemperature, AtmosphericPressure);
            SetPrecipitation(planet);
        }

        internal void ResetTemperatureDependentProperties(Planetoid planet)
        {
            SetAtmosphericScaleHeight(planet);
            SetAtmosphericHeight(planet);
            Shape = GetShape(planet);
            _averageSeaLevelDensity = GetAtmosphericDensity(planet.AverageSurfaceTemperature, AtmosphericPressure);
            SetPrecipitation(planet);
        }

        internal void ResetWater(Planetoid planet)
        {
            _containsWaterVapor = null;
            _waterRatio = null;
            _wetness = null;
            SetPrecipitation(planet);
        }

        internal void SetAtmosphericPressure(double value) => _atmosphericPressure = value;

        /// <summary>
        /// Standard pressure of 101.325 kPa is presumed for a <see cref="Requirement"/>.
        /// This method converts the proportional values to reflect <see cref="AtmosphericPressure"/>.
        /// </summary>
        /// <param name="requirement">The <see cref="Requirement"/> to convert.</param>
        /// <returns>
        /// A new <see cref="Requirement"/> with proportions adjusted for <see cref="AtmosphericPressure"/>.
        /// </returns>
        private Requirement ConvertRequirementForPressure(Requirement requirement)
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
        /// Calculates the total height of the <see cref="Atmosphere"/>, defined as the point at
        /// which atmospheric pressure is roughly equal to 0.5 Pa, in meters (the mesopause on Earth
        /// is around this pressure).
        /// </summary>
        /// <param name="planet">The <see cref="Planetoid"/> on which the calculation is being
        /// made.</param>
        /// <param name="temp">An optional temperature to supply for the surface of the
        /// planet.</param>
        /// <returns>The height of the atmosphere, in meters.</returns>
        /// <remarks>
        /// Uses the molar mass of air on Earth, which is clearly not correct for other atmospheres,
        /// but is considered "close enough" for the purposes of this library.
        /// </remarks>
        private void SetAtmosphericHeight(Planetoid planet, double? temp = null)
            => AtmosphericHeight = Math.Log(5e-4 / AtmosphericPressure) * ScienceConstants.R * (temp ?? planet.AverageSurfaceTemperature) / (-planet.SurfaceGravity * ScienceConstants.MAir);

        private double GetAtmosphericMass(Planetoid planet)
            => MathConstants.FourPI * planet.RadiusSquared * AtmosphericPressure * 1000 / planet.SurfaceGravity;

        private void SetAtmosphericScaleHeight(Planetoid planet)
            => AtmosphericScaleHeight = AtmosphericPressure * 1000 / (planet.SurfaceGravity * GetAtmosphericDensity(planet.AverageBlackbodySurfaceTemperature, AtmosphericPressure));

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
        private void SetGreenhouseFactor()
        {
            var total = Composition.GetGreenhousePotential();
            _greenhouseFactor = TMath.IsZero(total)
                ? 1
                : 0.933835 + (0.0441533 * Math.Exp(1.79077 * total) * (1.11169 + Math.Log(AtmosphericPressure)));
        }

        /// <summary>
        /// Calculates the <see cref="Atmosphere"/>'s shape.
        /// </summary>
        /// <param name="planet">The <see cref="Planetoid"/> on which the calculation is being
        /// made.</param>
        private IShape GetShape(Planetoid planet) => new HollowSphere(planet.Shape.ContainingRadius, AtmosphericHeight);

        private void SetPrecipitation(Planetoid planet)
        {
            _precipitationFactor = Wetness * AtmosphericHeight * _averageSeaLevelDensity / StandardHeightDensity;
            // An average "year" is a standard astronomical year of 31557600 seconds.
            AveragePrecipitation = _precipitationFactor * 990 * (planet.Orbit.HasValue ? planet.Orbit.Value.Period / 31557600 : 1);
            _maxPrecipitation = null;
            _maxSnowfall = null;
        }
    }
}
