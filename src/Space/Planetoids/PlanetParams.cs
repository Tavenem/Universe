using Tavenem.Universe.Chemistry;
using Tavenem.Universe.Climate;

namespace Tavenem.Universe.Space.Planetoids;

/// <summary>
/// A set of parameters which constrains the random generation of a <see cref="Planetoid"/>.
/// </summary>
/// <param name="Albedo">The target albedo.</param>
/// <param name="AtmosphericPressure">The target atmospheric pressure, in kPa.</param>
/// <param name="AtmosphericRequirements">All atmospheric requirements.</param>
/// <param name="AxialTilt">The target axial tilt, in radians.</param>
/// <param name="EarthlikeAtmosphere">
/// Whether the planet is to have an earthlike atmosphere.
/// </param>
/// <param name="Eccentricity">The target orbital eccentricity.</param>
/// <param name="HasMagnetosphere">
/// Indicates whether a strong magnetosphere is required.
/// </param>
/// <param name="HasRings">
/// Indicates whether a ring system is present.
/// </param>
/// <param name="MaxMass">An optional maximum mass for the planet, in kg.</param>
/// <param name="NumSatellites">
/// The number of satellites to place in orbit around the planet.
/// </param>
/// <param name="Radius">The target radius, in meters.</param>
/// <param name="RevolutionPeriod">The target revolution period, in seconds.</param>
/// <param name="RotationalPeriod">The target rotational period, in seconds.</param>
/// <param name="SurfaceGravity">The target surface gravity, in m/s².</param>
/// <param name="SurfaceTemperature">The target surface temperature, in K.</param>
/// <param name="WaterRatio">The target ratio of water to land on the surface.</param>
/// <param name="WaterVaporRatio">
/// The target mass fraction of water in the atmosphere.
/// </param>
public readonly record struct PlanetParams(
    double? Albedo = null,
    double? AtmosphericPressure = null,
    IReadOnlyList<SubstanceRequirement>? AtmosphericRequirements = null,
    double? AxialTilt = null,
    bool EarthlikeAtmosphere = false,
    double? Eccentricity = null,
    bool? HasMagnetosphere = null,
    bool? HasRings = null,
    HugeNumber? MaxMass = null,
    byte? NumSatellites = null,
    HugeNumber? Radius = null,
    HugeNumber? RevolutionPeriod = null,
    HugeNumber? RotationalPeriod = null,
    double? SurfaceGravity = null,
    double? SurfaceTemperature = null,
    decimal? WaterRatio = null,
    decimal? WaterVaporRatio = null)
{
    /// <summary>
    /// The approximate albedo of Earth.
    /// </summary>
    public const double EarthAlbedo = 0.325;

    /// <summary>
    /// The approximate atmospheric pressure of Earth, in kPa.
    /// </summary>
    public const double EarthAtmosphericPressure = 101.325;

    /// <summary>
    /// The approximate axial tilt of Earth, in radians.
    /// </summary>
    public const double EarthAxialTilt = 0.41;

    /// <summary>
    /// The approximate orbital eccentricity of Earth.
    /// </summary>
    public const double EarthEccentricity = 0.0167;

    /// <summary>
    /// The approximate surface gravity of Earth, in m/s².
    /// </summary>
    public const double EarthSurfaceGravity = 9.807;

    /// <summary>
    /// The approximate surface temperature of Earth, in K.
    /// </summary>
    public const double EarthSurfaceTemperature = 289;

    /// <summary>
    /// The approximate ratio of water coverage on Earth.
    /// </summary>
    public const decimal EarthWaterRatio = 0.709m;

    /// <summary>
    /// The approximate mass fraction of water in the atmosphere of Earth.
    /// </summary>
    public const decimal EarthWaterVaporRatio = 0.0025m;

    /// <summary>
    /// The approximate planetary radius of Earth, in meters.
    /// </summary>
    public static readonly HugeNumber EarthRadius = new(6371000);

    /// <summary>
    /// The approximate period of revolution of Earth, in seconds.
    /// </summary>
    public static readonly HugeNumber EarthRevolutionPeriod = new(31558150);

    /// <summary>
    /// The approximate period of rotation of Earth, in seconds.
    /// </summary>
    public static readonly HugeNumber EarthRotationalPeriod = new(86164);

    /// <summary>
    /// <para>
    /// An instance of <see cref="PlanetParams"/> with values for an Earthlike planet.
    /// </para>
    /// <para>
    /// See also <seealso cref="NewEarthlike(double?, double?,
    /// IReadOnlyList{SubstanceRequirement}?, double?, bool, double?, bool, bool?, HugeNumber?, byte?,
    /// HugeNumber?, HugeNumber?, HugeNumber?, double?, double?, decimal?, decimal?)"/>.
    /// </para>
    /// </summary>
    public static readonly PlanetParams Earthlike = NewEarthlike();

    /// <summary>
    /// Generates a new instance of <see cref="PlanetParams"/> with either the given values, or
    /// the values for an Earthlike planet.
    /// </summary>
    /// <param name="albedo">The target surface albedo.</param>
    /// <param name="atmosphericPressure">The target atmospheric pressure, in kPa.</param>
    /// <param name="atmosphericRequirements">All atmospheric requirements.</param>
    /// <param name="axialTilt">The target axial tilt, in radians.</param>
    /// <param name="earthlikeAtmosphere">
    /// Whether the planet is to have an earthlike atmosphere.
    /// </param>
    /// <param name="eccentricity">The target orbital eccentricity.</param>
    /// <param name="hasMagnetosphere">
    /// Indicates whether a strong magnetosphere is required.
    /// </param>
    /// <param name="hasRings">
    /// Indicates whether a ring system is present.
    /// </param>
    /// <param name="maxMass">An optional maximum mass for the planet, in kg.</param>
    /// <param name="numSatellites">
    /// The number of satellites to place in orbit around the planet.
    /// </param>
    /// <param name="radius">The target radius, in meters.</param>
    /// <param name="revolutionPeriod">The target revolution period, in seconds.</param>
    /// <param name="rotationalPeriod">The target rotational period, in seconds.</param>
    /// <param name="surfaceGravity">The target surface gravity, in m/s².</param>
    /// <param name="surfaceTemperature">The target surface temperature, in K.</param>
    /// <param name="waterRatio">The target ratio of water to land on the surface.</param>
    /// <param name="waterVaporRatio">
    /// The target mass fraction of water in the atmosphere.
    /// </param>
    /// <remarks>
    /// Note: any values left <see langword="null"/> will be supplied by the static values of
    /// this struct given for Earth. In order to create a <see cref="PlanetParams"/> instance
    /// which has actual <see langword="null"/> values (indicating no requirement), use the
    /// struct constructor, and supply the static defaults as needed.
    /// </remarks>
    public static PlanetParams NewEarthlike(
        double? albedo = null,
        double? atmosphericPressure = null,
        IReadOnlyList<SubstanceRequirement>? atmosphericRequirements = null,
        double? axialTilt = null,
        bool earthlikeAtmosphere = true,
        double? eccentricity = null,
        bool hasMagnetosphere = true,
        bool? hasRings = null,
        HugeNumber? maxMass = null,
        byte? numSatellites = null,
        HugeNumber? radius = null,
        HugeNumber? revolutionPeriod = null,
        HugeNumber? rotationalPeriod = null,
        double? surfaceGravity = null,
        double? surfaceTemperature = null,
        decimal? waterRatio = null,
        decimal? waterVaporRatio = null) => new(
            albedo ?? EarthAlbedo,
            atmosphericPressure ?? EarthAtmosphericPressure,
            atmosphericRequirements ?? Atmosphere.HumanBreathabilityRequirements,
            axialTilt ?? EarthAxialTilt,
            earthlikeAtmosphere,
            eccentricity ?? EarthEccentricity,
            hasMagnetosphere,
            hasRings,
            maxMass,
            numSatellites,
            radius ?? EarthRadius,
            revolutionPeriod ?? EarthRevolutionPeriod,
            rotationalPeriod ?? EarthRotationalPeriod,
            surfaceGravity ?? EarthSurfaceGravity,
            surfaceTemperature ?? EarthSurfaceTemperature,
            waterRatio ?? EarthWaterRatio,
            waterVaporRatio ?? EarthWaterVaporRatio);

    /// <summary>
    /// Indicates whether the current object is equal to another object of the same type.
    /// </summary>
    /// <param name="other">An object to compare with this object.</param>
    /// <returns>
    /// <see langword="true"/> if the current object is equal to the other parameter; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    public bool Equals(PlanetParams other)
        => Albedo == other.Albedo
        && AtmosphericPressure == other.AtmosphericPressure
        && AxialTilt == other.AxialTilt
        && EarthlikeAtmosphere == other.EarthlikeAtmosphere
        && Eccentricity == other.Eccentricity
        && HasMagnetosphere == other.HasMagnetosphere
        && HasRings == other.HasRings
        && MaxMass == other.MaxMass
        && NumSatellites == other.NumSatellites
        && Radius == other.Radius
        && RevolutionPeriod == other.RevolutionPeriod
        && RotationalPeriod == other.RotationalPeriod
        && SurfaceGravity == other.SurfaceGravity
        && SurfaceTemperature == other.SurfaceTemperature
        && WaterRatio == other.WaterRatio
        && WaterVaporRatio == other.WaterVaporRatio
        && (AtmosphericRequirements is null
            ? other.AtmosphericRequirements is null
            : (other.AtmosphericRequirements is not null
                && AtmosphericRequirements
                    .OrderBy(x => x.Substance.Id)
                    .SequenceEqual(other
                        .AtmosphericRequirements
                        .OrderBy(x => x.Substance.Id))));

    /// <summary>
    /// Indicates whether the current object is equal to another object of the same type.
    /// </summary>
    /// <param name="other">An object to compare with this object.</param>
    /// <returns>
    /// <see langword="true"/> if the current object is equal to the other parameter; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    public bool Equals(PlanetParams? other)
        => other is not null
        && Equals(other.Value);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(Albedo.GetHashCode());
        hashCode.Add(AtmosphericPressure.GetHashCode());
        hashCode.Add(AxialTilt.GetHashCode());
        hashCode.Add(EarthlikeAtmosphere.GetHashCode());
        hashCode.Add(Eccentricity.GetHashCode());
        hashCode.Add(HasMagnetosphere.GetHashCode());
        hashCode.Add(HasRings.GetHashCode());
        hashCode.Add(MaxMass.GetHashCode());
        hashCode.Add(NumSatellites.GetHashCode());
        hashCode.Add(Radius.GetHashCode());
        hashCode.Add(RevolutionPeriod.GetHashCode());
        hashCode.Add(RotationalPeriod.GetHashCode());
        hashCode.Add(SurfaceGravity.GetHashCode());
        hashCode.Add(SurfaceTemperature.GetHashCode());
        hashCode.Add(WaterRatio.GetHashCode());
        hashCode.Add(WaterVaporRatio.GetHashCode());
        hashCode.Add(GetAtmosphericRequirementsHashCode());
        return hashCode.ToHashCode();
    }

    private int GetAtmosphericRequirementsHashCode()
    {
        if (AtmosphericRequirements is null)
        {
            return 0;
        }
        unchecked
        {
            return 367 * AtmosphericRequirements
                .OrderBy(x => x.Substance.Id)
                .Aggregate(0, (a, c) => (a * 397) ^ c.GetHashCode());
        }
    }
}
