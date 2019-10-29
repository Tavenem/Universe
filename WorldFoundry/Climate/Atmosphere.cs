using NeverFoundry.MathAndScience;
using NeverFoundry.MathAndScience.Chemistry;
using NeverFoundry.MathAndScience.Constants.Numbers;
using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Threading.Tasks;
using WorldFoundry.CelestialBodies.Planetoids;

namespace WorldFoundry.Climate
{
    /// <summary>
    /// Represents a planetary atmosphere.
    /// </summary>
    [Serializable]
    public class Atmosphere : ISerializable
    {
        internal const int SnowToRainRatio = 13;

        // The approximate value of 0.05 + e^1.25. Used to calculate MaxPrecipitation.
        private const double ExpFiveFourthsPlusOneTwentieth = 3.5403429574618413761305460296723;

        private const double StandardHeightDensity = 124191.6;

        /// <summary>
        /// A range of acceptable amounts of O2, and list of maximum limits of common
        /// atmospheric gases for acceptable human breathability.
        /// </summary>
        public static SubstanceRequirement[] HumanBreathabilityRequirements { get; } = new SubstanceRequirement[]
        {
            new SubstanceRequirement(Substances.GetChemicalReference(Substances.Chemicals.Oxygen), 0.07m, 0.53m, PhaseType.Gas),
            new SubstanceRequirement(Substances.GetChemicalReference(Substances.Chemicals.Ammonia), maximumProportion: 5e-5m),
            new SubstanceRequirement(Substances.GetChemicalReference(Substances.Chemicals.AmmoniumHydrosulfide), maximumProportion: 1e-6m),
            new SubstanceRequirement(Substances.GetChemicalReference(Substances.Chemicals.CarbonMonoxide), maximumProportion: 5e-5m),
            new SubstanceRequirement(Substances.GetChemicalReference(Substances.Chemicals.CarbonDioxide), maximumProportion: 0.005m),
            new SubstanceRequirement(Substances.GetChemicalReference(Substances.Chemicals.HydrogenSulfide), maximumProportion: 0),
            new SubstanceRequirement(Substances.GetChemicalReference(Substances.Chemicals.Methane), maximumProportion: 0.001m),
            new SubstanceRequirement(Substances.GetChemicalReference(Substances.Chemicals.Ozone), maximumProportion: 1e-7m),
            new SubstanceRequirement(Substances.GetChemicalReference(Substances.Chemicals.Phosphine), maximumProportion: 3e-7m),
            new SubstanceRequirement(Substances.GetChemicalReference(Substances.Chemicals.SulphurDioxide), maximumProportion: 2e-6m),
        };

        /// <summary>
        /// The proportion of precipitation produced by this atmosphere relative to that of Earth.
        /// </summary>
        private double _precipitationFactor;

        /// <summary>
        /// Specifies the average height of this <see cref="Atmosphere"/>, in meters. Read-only;
        /// derived from the properties of the planet and atmosphere.
        /// </summary>
        public double AtmosphericHeight { get; set; }

        /// <summary>
        /// Specifies the atmospheric pressure at the surface of the planetary body, in kPa.
        /// </summary>
        public double AtmosphericPressure { get; private set; }

        /// <summary>
        /// Specifies the average scale height of this <see cref="Atmosphere"/>, in meters.
        /// Read-only; derived from the properties of the planet and atmosphere.
        /// </summary>
        public double AtmosphericScaleHeight { get; private set; }

        /// <summary>
        /// The physical makeup of this atmosphere.
        /// </summary>
        public IMaterial Material { get; private set; }

        private double? _maxPrecipitation;
        /// <summary>
        /// The maximum annual precipitation expected to be produced by this atmosphere, in mm.
        /// </summary>
        public double MaxPrecipitation
            => _maxPrecipitation ??= AveragePrecipitation * ExpFiveFourthsPlusOneTwentieth;

        private double? _maxSnowfall;
        /// <summary>
        /// The maximum annual snowfall expected to be produced by this atmosphere, in mm.
        /// </summary>
        public double MaxSnowfall => _maxSnowfall ??= MaxPrecipitation * SnowToRainRatio;

        /// <summary>
        /// The average annual precipitation expected to be produced by this atmosphere.
        /// </summary>
        internal double AveragePrecipitation { get; private set; }

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
                    if (!_greenhouseFactor.HasValue)
                    {
                        _greenhouseFactor = 1;
                    }
                }
                return _greenhouseFactor.Value;
            }
        }

        private decimal? _waterRatio;
        internal decimal WaterRatio => _waterRatio ??= Material.Constituents.Sum(x => x.substance.Substance.GetWaterProportion() * x.proportion);

        private double? _waterRatioDouble;
        internal double WaterRatioDouble => _waterRatioDouble ??= (double)WaterRatio;

        private double? _wetness;
        /// <summary>
        /// The total mass of water in this atmosphere relative to that of Earth.
        /// </summary>
        internal double Wetness => _wetness ??= (double)((Number)WaterRatio * (Material.Mass / new Number(1.287, 16)));

        /// <summary>
        /// Initializes a new instance of <see cref="Atmosphere"/>.
        /// </summary>
        /// <param name="pressure">The atmospheric pressure at the surface of the planetary body, in kPa.</param>
        internal Atmosphere(double pressure)
        {
            AtmosphericPressure = pressure;
            Material = new Material(0, SinglePoint.Origin); // unused; set correctly during initialization
        }

        private Atmosphere(
            IMaterial material,
            double precipitationFactor,
            double atmosphericHeight,
            double atmosphericPressure,
            double atmosphericScaleHeight,
            double averagePrecipitation)
        {
            Material = material;
            _precipitationFactor = precipitationFactor;
            AtmosphericHeight = atmosphericHeight;
            AtmosphericPressure = atmosphericPressure;
            AtmosphericScaleHeight = atmosphericScaleHeight;
            AveragePrecipitation = averagePrecipitation;
        }

        private Atmosphere(SerializationInfo info, StreamingContext context) : this(
            (IMaterial)info.GetValue(nameof(Material), typeof(IMaterial)),
            (double)info.GetValue(nameof(_precipitationFactor), typeof(double)),
            (double)info.GetValue(nameof(AtmosphericHeight), typeof(double)),
            (double)info.GetValue(nameof(AtmosphericPressure), typeof(double)),
            (double)info.GetValue(nameof(AtmosphericScaleHeight), typeof(double)),
            (double)info.GetValue(nameof(AveragePrecipitation), typeof(double))) { }

        internal static async Task<Atmosphere> GetNewInstanceAsync(Planetoid planet, double pressure, params (ISubstanceReference substance, decimal proportion)[] constituents)
        {
            var instance = new Atmosphere(pressure);
            await instance.InitializeAsync(planet, constituents).ConfigureAwait(false);
            return instance;
        }

        /// <summary>Populates a <see cref="SerializationInfo"></see> with the data needed to
        /// serialize the target object.</summary>
        /// <param name="info">The <see cref="SerializationInfo"></see> to populate with
        /// data.</param>
        /// <param name="context">The destination (see <see cref="StreamingContext"></see>) for this
        /// serialization.</param>
        /// <exception cref="System.Security.SecurityException">The caller does not have the
        /// required permission.</exception>
        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(Material), Material);
            info.AddValue(nameof(_precipitationFactor), _precipitationFactor);
            info.AddValue(nameof(AtmosphericHeight), AtmosphericHeight);
            info.AddValue(nameof(AtmosphericPressure), AtmosphericPressure);
            info.AddValue(nameof(AtmosphericScaleHeight), AtmosphericScaleHeight);
            info.AddValue(nameof(AveragePrecipitation), AveragePrecipitation);
        }

        /// <summary>
        /// Calculates the atmospheric density for the given conditions, in kg/m³.
        /// </summary>
        /// <param name="temperature">A temperature, in K.</param>
        /// <param name="pressure">A pressure, in kPa.</param>
        /// <returns>The atmospheric density for the given conditions, in kg/m³.</returns>
        public static double GetAtmosphericDensity(double temperature, double pressure)
            => pressure * 1000 / (287.058 * temperature);

        internal static Number GetAtmosphericMass(Planetoid planet, double pressure)
            => MathConstants.FourPI * planet.RadiusSquared * pressure * 1000 / planet.SurfaceGravity;

        internal static double GetGreenhouseFactor(double greenhousePotential, double pressure)
            => greenhousePotential.IsNearlyZero()
                ? 1
                : 933835e-6 + (441533e-7 * Math.Exp(179077e-5 * greenhousePotential) * (111169e-5 + Math.Log(pressure)));

        /// <summary>
        /// Calculates the atmospheric density for the given conditions, in kg/m³.
        /// </summary>
        /// <param name="temperature">A temperature, in K.</param>
        /// <param name="elevation">
        /// An elevation above the reference elevation for standard atmospheric pressure (sea
        /// level), in meters.
        /// </param>
        /// <returns>The atmospheric density for the given conditions, in kg/m³.</returns>
        public double GetAtmosphericDensity(Planetoid planet, double temperature, double elevation)
            => GetAtmosphericDensity(temperature, GetAtmosphericPressure(planet, temperature, elevation));

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
            0.235 * GetAtmosphericDensity(temperature, GetAtmosphericPressure(planet, temperature, altitude)) * speed * speed * (double)planet.Shape.ContainingRadius;

        /// <summary>
        /// Calculates the atmospheric pressure at a given elevation, in kPa.
        /// </summary>
        /// <param name="planet">The <see cref="Planetoid"/> on which the calculation is being
        /// made.</param>
        /// <param name="temperature">The temperature at the given elevation, in K.</param>
        /// <param name="elevation">
        /// An elevation above the reference elevation for standard atmospheric pressure (sea
        /// level), in meters.
        /// </param>
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
            => elevation <= 0
            ? AtmosphericPressure
            : AtmosphericPressure * Math.Exp((double)planet.SurfaceGravity * NeverFoundry.MathAndScience.Constants.Doubles.ScienceConstants.MAir * elevation / (NeverFoundry.MathAndScience.Constants.Doubles.ScienceConstants.R * temperature));

        /// <summary>
        /// Sets the atmospheric pressure of this atmosphere to the given <paramref name="value"/>.
        /// </summary>
        /// <param name="planet">The <see cref="Planetoid"/> which this atmosphere surrounds;
        /// required in order to correctly reset the properties dependent on pressure.</param>
        /// <param name="value">The new pressure, in kPa.</param>
        public async Task SetAtmosphericPressureAsync(Planetoid planet, double value)
        {
            AtmosphericPressure = value;
            await ResetPressureDependentPropertiesAsync(planet).ConfigureAwait(false);
        }

        /// <summary>
        /// Determines if this <see cref="Atmosphere"/> meets the given requirements.
        /// </summary>
        /// <param name="requirements">An enumeration of <see cref="SubstanceRequirement"/>
        /// instances.</param>
        /// <returns><see langword="true"/> if this <see cref="Atmosphere"/> meets the requirements;
        /// otherwise <see langword="false"/>.</returns>
        public bool MeetsRequirements(IEnumerable<SubstanceRequirement> requirements)
        {
            var surfaceLayer = Material is LayeredComposite lc && lc.Layers.Count > 0
                ? lc.Layers[0].material
                : Material;
            if (surfaceLayer?.IsEmpty != false)
            {
                return !requirements.Any();
            }
            var pressure = AtmosphericPressure;
            return requirements.All(x => x.IsSatisfiedBy(surfaceLayer, pressure));
        }

        internal void AddToTroposphere(IHomogeneousReference constituent, decimal proportion)
        {
            DifferentiateTroposphere();
            if (Material is LayeredComposite lc
                && lc.Layers.Count > 0)
            {
                AddConstituent(lc.Layers[0].material, constituent, proportion);
            }
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
        internal IEnumerable<SubstanceRequirement> ConvertRequirementsForPressure(IEnumerable<SubstanceRequirement>? requirements)
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

        internal void DifferentiateTroposphere()
        {
            if (!(Material is LayeredComposite))
            {
                // Separate troposphere from upper atmosphere if undifferentiated.
                Material = Material.Split(0.8m);
            }
        }

        internal async Task InitializeAsync(Planetoid planet, (ISubstanceReference substance, decimal proportion)[] constituents)
        {
            if ((constituents?.Length ?? 0) == 0)
            {
                AtmosphericPressure = 0;
            }

            await SetAtmosphericScaleHeightAsync(planet).ConfigureAwait(false);

            var mass = GetAtmosphericMass(planet, AtmosphericPressure);

            planet.InsolationFactor_Equatorial = planet.GetInsolationFactor(mass, AtmosphericScaleHeight);
            var tIF = await planet.GetAverageBlackbodyTemperatureAsync().ConfigureAwait(false) * planet.InsolationFactor_Equatorial;
            SetGreenhouseFactor();
            planet._greenhouseEffect = (tIF * GreenhouseFactor) - await planet.GetAverageBlackbodyTemperatureAsync().ConfigureAwait(false);
            var greenhouseEffect = await planet.GetGreenhouseEffectAsync().ConfigureAwait(false);
            var temperature = tIF + greenhouseEffect;

            await SetAtmosphericHeightAsync(planet, temperature).ConfigureAwait(false);

            var density = GetAtmosphericDensity(temperature, AtmosphericPressure);

            var shape = GetShape(planet);

            Material = new Material(
                density,
                mass,
                shape,
                temperature,
                constituents ?? new (ISubstanceReference substance, decimal proportion)[0]);

            SetPrecipitation(planet);
        }

        internal async Task ResetGreenhouseFactorAsync(Planetoid planet)
        {
            _greenhouseFactor = null;
            await planet.ResetCachedTemperaturesAsync().ConfigureAwait(false);
        }

        internal async Task ResetPressureDependentPropertiesAsync(Planetoid planet)
        {
            await SetAtmosphericScaleHeightAsync(planet).ConfigureAwait(false);
            Material = Material.GetClone((decimal)(GetAtmosphericMass(planet, AtmosphericPressure) / Material.Mass));
            await SetAtmosphericHeightAsync(planet).ConfigureAwait(false);
            Material.Shape = GetShape(planet);
            var temp = await planet.GetAverageSurfaceTemperatureAsync().ConfigureAwait(false);
            Material.Density = GetAtmosphericDensity(temp, AtmosphericPressure);
            if (Material is LayeredComposite lc)
            {
                foreach (var (material, _) in lc.Layers)
                {
                    material.Density = Material.Density;
                }
            }
            SetPrecipitation(planet);
        }

        internal async Task ResetTemperatureDependentPropertiesAsync(Planetoid planet)
        {
            await SetAtmosphericScaleHeightAsync(planet).ConfigureAwait(false);
            await SetAtmosphericHeightAsync(planet).ConfigureAwait(false);
            Material.Shape = GetShape(planet);
            var temp = await planet.GetAverageSurfaceTemperatureAsync().ConfigureAwait(false);
            Material.Density = GetAtmosphericDensity(temp, AtmosphericPressure);
            if (Material is LayeredComposite lc)
            {
                foreach (var (material, _) in lc.Layers)
                {
                    material.Density = Material.Density;
                }
            }
            SetPrecipitation(planet);
        }

        internal void ResetWater(Planetoid planet)
        {
            _waterRatio = null;
            _waterRatioDouble = null;
            _wetness = null;
            SetPrecipitation(planet);
        }

        internal void SetAtmosphericPressure(double value) => AtmosphericPressure = value;

        private void AddConstituent(IMaterial material, IHomogeneousReference constituent, decimal proportion)
        {
            if (material is Material m)
            {
                m.AddConstituent(constituent, proportion);
            }
            else if (material is Composite composite)
            {
                foreach (var component in composite.Components)
                {
                    AddConstituent(component, constituent, proportion);
                }
            }
        }

        /// <summary>
        /// Standard pressure of 101.325 kPa is presumed for a <see cref="SubstanceRequirement"/>.
        /// This method converts the proportional values to reflect <see cref="AtmosphericPressure"/>.
        /// </summary>
        /// <param name="requirement">The <see cref="SubstanceRequirement"/> to convert.</param>
        /// <returns>
        /// A new <see cref="SubstanceRequirement"/> with proportions adjusted for <see cref="AtmosphericPressure"/>.
        /// </returns>
        private SubstanceRequirement ConvertRequirementForPressure(SubstanceRequirement requirement)
        {
            var minActual = requirement.MinimumProportion * NeverFoundry.MathAndScience.Constants.Decimals.ScienceConstants.atm;
            var maxActual = requirement.MaximumProportion.HasValue ? requirement.MaximumProportion * NeverFoundry.MathAndScience.Constants.Decimals.ScienceConstants.atm : null;
            var atm = (decimal)AtmosphericPressure;
            return new SubstanceRequirement(
                requirement.Substance,
                minActual / atm,
                maxActual.HasValue ? maxActual / atm : null,
                requirement.Phase);
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
        private async Task SetAtmosphericHeightAsync(Planetoid planet, double? temp = null)
        {
            temp ??= await planet.GetAverageSurfaceTemperatureAsync().ConfigureAwait(false);
            AtmosphericHeight = Math.Log(0.0005 / AtmosphericPressure) * NeverFoundry.MathAndScience.Constants.Doubles.ScienceConstants.R * temp.Value / (-(double)planet.SurfaceGravity * NeverFoundry.MathAndScience.Constants.Doubles.ScienceConstants.MAir);
        }

        private async Task SetAtmosphericScaleHeightAsync(Planetoid planet)
        {
            var avgBlackbodyTemp = await planet.GetAverageBlackbodyTemperatureAsync().ConfigureAwait(false);
            AtmosphericScaleHeight = AtmosphericPressure * 1000 / (double)planet.SurfaceGravity / GetAtmosphericDensity(avgBlackbodyTemp, AtmosphericPressure);
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
        private void SetGreenhouseFactor() => SetGreenhouseFactor(Material.Constituents);

        private void SetGreenhouseFactor(IEnumerable<(ISubstanceReference substance, decimal proportion)> substances)
            => _greenhouseFactor = GetGreenhouseFactor(
                substances.Sum(x => x.substance.Substance.GreenhousePotential * (double)x.proportion),
                AtmosphericPressure);

        /// <summary>
        /// Calculates the <see cref="Atmosphere"/>'s shape.
        /// </summary>
        /// <param name="planet">The <see cref="Planetoid"/> on which the calculation is being
        /// made.</param>
        private IShape GetShape(Planetoid planet) => new HollowSphere(planet.Shape.ContainingRadius, AtmosphericHeight);

        private void SetPrecipitation(Planetoid planet)
        {
            _precipitationFactor = Wetness * Material.Density * AtmosphericHeight / StandardHeightDensity;
            // An average "year" is a standard astronomical year of 31557600 seconds.
            AveragePrecipitation = _precipitationFactor * 990 * (planet.Orbit.HasValue ? (double)planet.Orbit.Value.Period / 31557600 : 1);
            _maxPrecipitation = null;
            _maxSnowfall = null;
        }
    }
}
