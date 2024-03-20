using System.Collections.ObjectModel;
using Tavenem.Chemistry;
using Tavenem.Universe.Chemistry;
using Tavenem.Universe.Space;

namespace Tavenem.Universe.Climate;

/// <summary>
/// Represents a planetary atmosphere.
/// </summary>
public class Atmosphere
{
    /// <summary>
    /// The ratio by which precipitation is multiplied to obtain an equivalent amount of
    /// snowfall.
    /// </summary>
    public const int SnowToRainRatio = 13;

    private const double AveragePrecipitationDivisor = 0.11293634496919917864476386036961;

    // The approximate value of 2 * (0.05 + e^1.25). Used to calculate MaxPrecipitation.
    private const double MaxPrecipitationFactor = 7.0806859149236827522610920593445;

    private const double StandardHeightDensity = 124191.6;

    private static readonly HugeNumber _WetnessMassDivisor = new(1.287, 16);

    /// <summary>
    /// A range of acceptable amounts of O2, and list of maximum limits of common
    /// atmospheric gases for acceptable human breathability.
    /// </summary>
    public static SubstanceRequirement[] HumanBreathabilityRequirements { get; } =
    [
        new SubstanceRequirement(Substances.All.Oxygen.GetHomogeneousReference(), 0.07m, 0.53m, PhaseType.Gas),
        new SubstanceRequirement(Substances.All.Ammonia.GetHomogeneousReference(), MaximumProportion: 5e-5m),
        new SubstanceRequirement(Substances.All.AmmoniumHydrosulfide.GetHomogeneousReference(), MaximumProportion: 1e-6m),
        new SubstanceRequirement(Substances.All.CarbonMonoxide.GetHomogeneousReference(), MaximumProportion: 5e-5m),
        new SubstanceRequirement(Substances.All.CarbonDioxide.GetHomogeneousReference(), MaximumProportion: 0.005m),
        new SubstanceRequirement(Substances.All.HydrogenSulfide.GetHomogeneousReference(), MaximumProportion: 0),
        new SubstanceRequirement(Substances.All.Methane.GetHomogeneousReference(), MaximumProportion: 0.001m),
        new SubstanceRequirement(Substances.All.Ozone.GetHomogeneousReference(), MaximumProportion: 1e-7m),
        new SubstanceRequirement(Substances.All.Phosphine.GetHomogeneousReference(), MaximumProportion: 3e-7m),
        new SubstanceRequirement(Substances.All.SulphurDioxide.GetHomogeneousReference(), MaximumProportion: 2e-6m),
    ];

    /// <summary>
    /// Specifies the average height of this <see cref="Atmosphere"/>, in meters.
    /// </summary>
    /// <remarks>
    /// Read-only. Derived from the properties of the planet and atmosphere.
    /// </remarks>
    public double AtmosphericHeight { get; private set; }

    /// <summary>
    /// Specifies the atmospheric pressure at the surface of the planetary body, in kPa.
    /// </summary>
    /// <remarks>
    /// Read-only. Set with the <see cref="SetAtmosphericPressure(Planetoid, double)"/> method, or
    /// the <see cref="Planetoid.SetAtmosphericPressure(double)"/> method on the <see
    /// cref="Planetoid"/> this atmosphere surrounds.
    /// </remarks>
    public double AtmosphericPressure { get; private set; }

    /// <summary>
    /// Specifies the average scale height of this <see cref="Atmosphere"/>, in meters.
    /// </summary>
    /// <remarks>
    /// Read-only. Derived from the properties of the planet and atmosphere.
    /// </remarks>
    public double AtmosphericScaleHeight { get; private set; }

    /// <summary>
    /// The average precipitation expected to be produced by this atmosphere, in mm/hr.
    /// </summary>
    /// <remarks>
    /// Read-only. Set with <see cref="SetAveragePrecipitation(double)"/>.
    /// </remarks>
    public double AveragePrecipitation { get; private set; }

    /// <summary>
    /// Whether this atmosphere is an empty instance.
    /// </summary>
    public bool IsEmpty => AtmosphericPressure == 0 || _material?.IsEmpty != false;

    private IMaterial<HugeNumber>? _material;
    /// <summary>
    /// The physical makeup of this atmosphere.
    /// </summary>
    /// <remarks>
    /// The properties of this instance should not be directly modified. Doing so would not produce
    /// the implied effects upon other properties of this or related entities. For instance,
    /// altering the <see cref="IMaterial{TScalar}.Constituents"/> directly to add water would not
    /// produce any adjustment to the <see cref="AveragePrecipitation"/>.
    /// </remarks>
    public IMaterial<HugeNumber> Material
    {
        get => _material ??= new Material<HugeNumber>();
        private set => _material = value.IsEmpty ? null : value;
    }

    private double? _maxPrecipitation;
    /// <summary>
    /// The maximum precipitation expected to be produced by this atmosphere, in mm/hr.
    /// </summary>
    /// <remarks>
    /// Read-only. Derived from <see cref="AveragePrecipitation"/>.
    /// </remarks>
    public double MaxPrecipitation
        => _maxPrecipitation ??= AveragePrecipitation * MaxPrecipitationFactor;

    private double? _maxSnowfall;
    /// <summary>
    /// The maximum annual snowfall expected to be produced by this atmosphere, in mm.
    /// </summary>
    /// <remarks>
    /// Read-only. Derived from <see cref="AveragePrecipitation"/>.
    /// </remarks>
    public double MaxSnowfall => _maxSnowfall ??= MaxPrecipitation * SnowToRainRatio;

    private double? _greenhouseFactor;
    /// <summary>
    /// The total greenhouse factor for this <see cref="Atmosphere"/>.
    /// </summary>
    /// <remarks>
    /// Read-only. Derived from the constituents of the atmosphere and the <see
    /// cref="AtmosphericPressure"/>.
    /// </remarks>
    internal double GreenhouseFactor
    {
        get
        {
            if (!_greenhouseFactor.HasValue)
            {
                SetGreenhouseFactor(null);
                if (!_greenhouseFactor.HasValue)
                {
                    _greenhouseFactor = 1;
                }
            }
            return _greenhouseFactor.Value;
        }
    }

    private double? _hv2RsE;
    internal double Hv2RsE
        => _hv2RsE ??= Constants.DeltaHvapWaterSquared * WaterRatioDouble * Constants.RSpecificRatioOfDryAirToWater;

    private double? _hvE;
    internal double HvE
        => _hvE ??= DoubleConstants.DeltaHvapWater * WaterRatioDouble;

    private decimal? _waterRatio;
    internal decimal WaterRatio => _waterRatio ??= Material.Constituents.Sum(x => x.Key.Substance.GetWaterProportion() * x.Value);

    private double? _waterRatioDouble;
    internal double WaterRatioDouble => _waterRatioDouble ??= (double)WaterRatio;

    private double? _wetness;
    /// <summary>
    /// The total mass of water in this atmosphere relative to that of Earth.
    /// </summary>
    /// <remarks>
    /// Read-only. Derived from the proportion of water in the atmosphere, and its mass.
    /// </remarks>
    internal double Wetness => _wetness ??= (double)((HugeNumber)WaterRatio * (Material.Mass / _WetnessMassDivisor));

    internal Atmosphere() { }

    internal Atmosphere(
        Planetoid planet,
        double pressure,
        IReadOnlyDictionary<ISubstanceReference, decimal>? constituents)
    {
        if ((constituents?.Count ?? 0) == 0
            || pressure <= 0)
        {
            return;
        }

        AtmosphericPressure = pressure;

        SetAtmosphericScaleHeight(planet);

        var mass = GetAtmosphericMass(planet, AtmosphericPressure);

        planet.InsolationFactor_Equatorial = planet.GetInsolationFactor(mass, AtmosphericScaleHeight);
        var tIF = planet.AverageBlackbodyTemperature * planet.InsolationFactor_Equatorial;
        SetGreenhouseFactor(
            constituents?.Select(x => (x.Key, x.Value))
            ?? []);
        planet.GreenhouseEffect = (tIF * GreenhouseFactor) - planet.AverageBlackbodyTemperature;
        var greenhouseEffect = planet.GetGreenhouseEffect();
        var temperature = tIF + greenhouseEffect;

        SetAtmosphericHeight(planet, temperature);

        var density = GetAtmosphericDensity(temperature, AtmosphericPressure);

        var shape = GetShape(planet);

        Material = new Material<HugeNumber>(
            constituents ?? ReadOnlyDictionary<ISubstanceReference, decimal>.Empty,
            density,
            mass,
            shape,
            temperature);

        SetPrecipitation();
    }

    internal Atmosphere(
        Planetoid planet,
        double pressure,
        params (ISubstanceReference substance, decimal proportion)[] constituents) : this(
            planet,
            pressure,
            constituents.ToDictionary(
                x => x.substance,
                x => x.proportion))
    { }

    /// <summary>
    /// Calculates the atmospheric density for the given conditions, in kg/m³.
    /// </summary>
    /// <param name="temperature">A temperature, in K.</param>
    /// <param name="pressure">A pressure, in kPa.</param>
    /// <returns>The atmospheric density for the given conditions, in kg/m³.</returns>
    public static double GetAtmosphericDensity(double temperature, double pressure)
        => pressure * 1000 / (287.058 * temperature);

    internal static HugeNumber GetAtmosphericMass(Planetoid planet, double pressure)
        => HugeNumberConstants.FourPi * planet.RadiusSquared * pressure * 1000 / planet.SurfaceGravity;

    internal static double GetGreenhouseFactor(double greenhousePotential, double pressure)
        => greenhousePotential.IsNearlyZero()
            ? 1
            : 933835e-6 + (441533e-7 * Math.Exp(179077e-5 * greenhousePotential) * (111169e-5 + Math.Log(pressure)));

    /// <summary>
    /// Calculates the atmospheric density for the given conditions, in kg/m³.
    /// </summary>
    /// <param name="planet">The <see cref="Planetoid"/> on which the calculation is being
    /// made.</param>
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
    public double GetAtmosphericDrag(Planetoid planet, double temperature, double altitude, double speed)
        => 0.235
        * GetAtmosphericDensity(temperature, GetAtmosphericPressure(planet, temperature, altitude))
        * speed
        * speed
        * (double)planet.Shape.ContainingRadius;

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
        : AtmosphericPressure * Math.Exp((double)planet.SurfaceGravity * DoubleConstants.MAir * elevation / (DoubleConstants.R * temperature));

    /// <summary>
    /// Sets the atmospheric pressure of this atmosphere to the given <paramref name="value"/>.
    /// </summary>
    /// <param name="planet">The <see cref="Planetoid"/> which this atmosphere surrounds;
    /// required in order to correctly reset the properties dependent on pressure.</param>
    /// <param name="value">The new pressure, in kPa.</param>
    public void SetAtmosphericPressure(Planetoid planet, double value)
    {
        AtmosphericPressure = value;
        ResetPressureDependentProperties(planet);
    }

    /// <summary>
    /// Sets the average precipitation expected to be produced by this atmosphere, in mm/hr.
    /// </summary>
    /// <param name="value">
    /// The average precipitation expected to be produced by this atmosphere, in mm/hr.
    /// </param>
    /// <remarks>
    /// Note that changing this value changes the proportion of water in the atmosphere, from which
    /// the value is ultimately derived. A value which cannot be supported (because it would require
    /// a proportion of water less than zero or greater than 1) results in the closest possible
    /// value instead.
    /// </remarks>
    public void SetAveragePrecipitation(double value)
    {
        var waterRatio = Math.Clamp((decimal)(value * StandardHeightDensity * AveragePrecipitationDivisor
            / Material.Density * AtmosphericHeight * (double)(Material.Mass / _WetnessMassDivisor)), 0, 1);
        Material.Add(Substances.All.Water, waterRatio);
        ResetWater();
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
        var surfaceLayer = Material.GetCore();
        if (surfaceLayer?.IsEmpty != false)
        {
            return !requirements.Any();
        }
        var pressure = AtmosphericPressure;
        return requirements.All(x => x.IsSatisfiedBy(surfaceLayer, pressure));
    }

    internal void AddToTroposphere(HomogeneousReference constituent, decimal proportion)
    {
        DifferentiateTroposphere();
        if (Material is Composite<HugeNumber> lc
            && lc.Components.Count > 0)
        {
            lc.Components[0].Add(constituent, proportion);
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
        if (requirements is null)
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
        if (Material is not Composite<HugeNumber>)
        {
            // Separate troposphere from upper atmosphere if undifferentiated.
            Material = Material.Split(new HugeNumber(8, -1));
        }
    }

    internal void ResetGreenhouseFactor() => _greenhouseFactor = null;

    internal void ResetPressureDependentProperties(Planetoid planet)
    {
        SetAtmosphericScaleHeight(planet);
        if (Material.Mass.IsPositive())
        {
            Material = Material.GetClone(GetAtmosphericMass(planet, AtmosphericPressure) / Material.Mass);
        }
        SetAtmosphericHeight(planet);
        Material.Shape = GetShape(planet);
        var temp = planet.GetAverageSurfaceTemperature();
        Material.Density = GetAtmosphericDensity(temp, AtmosphericPressure);
        if (Material is Composite<HugeNumber> lc)
        {
            foreach (var material in lc.Components)
            {
                material.Density = Material.Density;
            }
        }
        SetPrecipitation();
    }

    internal void ResetTemperatureDependentProperties(Planetoid planet)
    {
        SetAtmosphericScaleHeight(planet);
        SetAtmosphericHeight(planet);
        Material.Shape = GetShape(planet);
        var temp = planet.GetAverageSurfaceTemperature();
        Material.Density = GetAtmosphericDensity(temp, AtmosphericPressure);
        if (Material is Composite<HugeNumber> lc)
        {
            foreach (var material in lc.Components)
            {
                material.Density = Material.Density;
            }
        }
        SetPrecipitation();
    }

    internal void ResetWater()
    {
        _waterRatio = null;
        _waterRatioDouble = null;
        _wetness = null;
        _hv2RsE = null;
        _hvE = null;
        SetPrecipitation();
    }

    internal void SetAtmosphericPressure(double value) => AtmosphericPressure = value;

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
        var minActual = requirement.MinimumProportion * DecimalConstants.atm;
        var maxActual = requirement.MaximumProportion.HasValue ? requirement.MaximumProportion * DecimalConstants.atm : null;
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
    private void SetAtmosphericHeight(Planetoid planet, double? temp = null)
    {
        temp ??= planet.GetAverageSurfaceTemperature();
        AtmosphericHeight = Math.Log(0.0005 / AtmosphericPressure) * DoubleConstants.R * temp.Value / (-(double)planet.SurfaceGravity * DoubleConstants.MAir);
    }

    private void SetAtmosphericScaleHeight(Planetoid planet)
        => AtmosphericScaleHeight = AtmosphericPressure * 1000 / (double)planet.SurfaceGravity / GetAtmosphericDensity(planet.AverageBlackbodyTemperature, AtmosphericPressure);

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
    private void SetGreenhouseFactor(IEnumerable<(ISubstanceReference substance, decimal proportion)>? components)
    {
        if (components is null)
        {
            _greenhouseFactor = GetGreenhouseFactor(Material.GetOverallValue(x => x.GreenhousePotential), AtmosphericPressure);
        }
        else
        {
            _greenhouseFactor = GetGreenhouseFactor(components.Sum(x => x.substance.Substance.GreenhousePotential * (double)x.proportion), AtmosphericPressure);
        }
    }

    /// <summary>
    /// Calculates the <see cref="Atmosphere"/>'s shape.
    /// </summary>
    /// <param name="planet">The <see cref="Planetoid"/> on which the calculation is being
    /// made.</param>
    private IShape<HugeNumber> GetShape(Planetoid planet)
        => new HollowSphere<HugeNumber>(planet.Shape.ContainingRadius, AtmosphericHeight);

    private void SetPrecipitation()
    {
        // 990 mm/yr
        AveragePrecipitation = Wetness * Material.Density * AtmosphericHeight / StandardHeightDensity * AveragePrecipitationDivisor;
        _maxPrecipitation = null;
        _maxSnowfall = null;
    }
}
