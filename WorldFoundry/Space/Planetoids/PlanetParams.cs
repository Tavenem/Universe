using NeverFoundry.MathAndScience;
using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.WorldFoundry.Climate;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace NeverFoundry.WorldFoundry.Space.Planetoids
{
    /// <summary>
    /// A set of parameters which constrains the random generation of a <see cref="Planetoid"/>.
    /// </summary>
    [Serializable]
    [JsonObject]
    public struct PlanetParams : ISerializable, IEquatable<PlanetParams>
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
        public static readonly Number EarthRadius = new Number(6371000);

        /// <summary>
        /// The approximate period of revolution of Earth, in seconds.
        /// </summary>
        public static readonly Number EarthRevolutionPeriod = new Number(31558150);

        /// <summary>
        /// The approximate period of rotation of Earth, in seconds.
        /// </summary>
        public static readonly Number EarthRotationalPeriod = new Number(86164);

        /// <summary>
        /// <para>
        /// An instance of <see cref="PlanetParams"/> with values for an Earthlike planet.
        /// </para>
        /// <para>
        /// See also <seealso cref="NewEarthlike(double?, double?,
        /// IReadOnlyList{SubstanceRequirement}?, double?, bool, double?, bool, Number?, byte?,
        /// Number?, Number?, Number?, double?, double?, decimal?, decimal?)"/>.
        /// </para>
        /// </summary>
        public static readonly PlanetParams Earthlike = NewEarthlike();

        /// <summary>
        /// The target albedo.
        /// </summary>
        public double? Albedo { get; }

        /// <summary>
        /// The target atmospheric pressure, in kPa.
        /// </summary>
        public double? AtmosphericPressure { get; }

        /// <summary>
        /// Any atmospheric requirements.
        /// </summary>
        [JsonProperty(TypeNameHandling = TypeNameHandling.None)]
        public IReadOnlyList<SubstanceRequirement> AtmosphericRequirements { get; }

        /// <summary>
        /// The target axial tilt, in radians.
        /// </summary>
        public double? AxialTilt { get; }

        /// <summary>
        /// Whether the planet is to have an earthlike atmosphere.
        /// </summary>
        public bool EarthlikeAtmosphere { get; }

        /// <summary>
        /// The target orbital eccentricity.
        /// </summary>
        public double? Eccentricity { get; }

        /// <summary>
        /// Indicates whether a strong magnetosphere is required.
        /// </summary>
        public bool? HasMagnetosphere { get; }

        /// <summary>
        /// An optional maximum mass for the planet, in kg.
        /// </summary>
        public Number? MaxMass { get; }

        /// <summary>
        /// The number of satellites to place in orbit around the planet.
        /// </summary>
        public byte? NumSatellites { get; }

        /// <summary>
        /// The target radius, in meters.
        /// </summary>
        public Number? Radius { get; }

        /// <summary>
        /// The target revolution period, in seconds.
        /// </summary>
        public Number? RevolutionPeriod { get; }

        /// <summary>
        /// The target rotational period, in seconds.
        /// </summary>
        public Number? RotationalPeriod { get; }

        /// <summary>
        /// The target surface gravity, in m/s².
        /// </summary>
        public double? SurfaceGravity { get; }

        /// <summary>
        /// The target surface temperature, in K.
        /// </summary>
        public double? SurfaceTemperature { get; }

        /// <summary>
        /// The target ratio of water to land on the surface.
        /// </summary>
        public decimal? WaterRatio { get; }

        /// <summary>
        /// The target mass fraction of water in the atmosphere.
        /// </summary>
        public decimal? WaterVaporRatio { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="PlanetParams"/> with the given values.
        /// </summary>
        /// <param name="albedo">The target albedo.</param>
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
        [JsonConstructor]
        [System.Text.Json.Serialization.JsonConstructor]
        public PlanetParams(
            double? albedo = null,
            double? atmosphericPressure = null,
            IReadOnlyList<SubstanceRequirement>? atmosphericRequirements = null,
            double? axialTilt = null,
            bool earthlikeAtmosphere = false,
            double? eccentricity = null,
            bool? hasMagnetosphere = null,
            Number? maxMass = null,
            byte? numSatellites = null,
            Number? radius = null,
            Number? revolutionPeriod = null,
            Number? rotationalPeriod = null,
            double? surfaceGravity = null,
            double? surfaceTemperature = null,
            decimal? waterRatio = null,
            decimal? waterVaporRatio = null)
        {
            Albedo = albedo.HasValue
                ? albedo.Value.Clamp(0, 1)
                : (double?)null;
            AtmosphericPressure = atmosphericPressure;
            AtmosphericRequirements = atmosphericRequirements?.ToArray() ?? new SubstanceRequirement[0];
            AxialTilt = axialTilt;
            EarthlikeAtmosphere = earthlikeAtmosphere;
            Eccentricity = eccentricity;
            HasMagnetosphere = hasMagnetosphere;
            NumSatellites = numSatellites;
            Radius = radius;
            RevolutionPeriod = revolutionPeriod;
            RotationalPeriod = rotationalPeriod;
            SurfaceGravity = surfaceGravity;
            SurfaceTemperature = surfaceTemperature;
            WaterRatio = waterRatio;
            WaterVaporRatio = waterVaporRatio;
            MaxMass = maxMass;
        }

        private PlanetParams(SerializationInfo info, StreamingContext context) : this(
            (double?)info.GetValue(nameof(Albedo), typeof(double?)),
            (double?)info.GetValue(nameof(AtmosphericPressure), typeof(double?)),
            (IReadOnlyList<SubstanceRequirement>?)info.GetValue(nameof(AtmosphericRequirements), typeof(IReadOnlyList<SubstanceRequirement>)) ?? ImmutableList<SubstanceRequirement>.Empty,
            (double?)info.GetValue(nameof(AxialTilt), typeof(double?)),
            (bool?)info.GetValue(nameof(EarthlikeAtmosphere), typeof(bool)) ?? default,
            (double?)info.GetValue(nameof(Eccentricity), typeof(double?)),
            (bool?)info.GetValue(nameof(HasMagnetosphere), typeof(bool?)),
            (Number?)info.GetValue(nameof(MaxMass), typeof(Number?)),
            (byte?)info.GetValue(nameof(NumSatellites), typeof(byte?)),
            (Number?)info.GetValue(nameof(Radius), typeof(Number?)),
            (Number?)info.GetValue(nameof(RevolutionPeriod), typeof(Number?)),
            (Number?)info.GetValue(nameof(RotationalPeriod), typeof(Number?)),
            (double?)info.GetValue(nameof(SurfaceGravity), typeof(double?)),
            (double?)info.GetValue(nameof(SurfaceTemperature), typeof(double?)),
            (decimal?)info.GetValue(nameof(WaterRatio), typeof(decimal?)),
            (decimal?)info.GetValue(nameof(WaterVaporRatio), typeof(decimal?)))
        { }

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
            Number? maxMass = null,
            byte? numSatellites = null,
            Number? radius = null,
            Number? revolutionPeriod = null,
            Number? rotationalPeriod = null,
            double? surfaceGravity = null,
            double? surfaceTemperature = null,
            decimal? waterRatio = null,
            decimal? waterVaporRatio = null) => new PlanetParams(
                albedo ?? EarthAlbedo,
                atmosphericPressure ?? EarthAtmosphericPressure,
                atmosphericRequirements ?? Atmosphere.HumanBreathabilityRequirements,
                axialTilt ?? EarthAxialTilt,
                earthlikeAtmosphere,
                eccentricity ?? EarthEccentricity,
                hasMagnetosphere,
                maxMass,
                numSatellites,
                radius ?? EarthRadius,
                revolutionPeriod ?? EarthRevolutionPeriod,
                rotationalPeriod ?? EarthRotationalPeriod,
                surfaceGravity ?? EarthSurfaceGravity,
                surfaceTemperature ?? EarthSurfaceTemperature,
                waterRatio ?? EarthWaterRatio,
                waterVaporRatio ?? EarthWaterVaporRatio);

        /// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        /// <see langword="true" /> if the current object is equal to the <paramref name="other" />
        /// parameter; otherwise, <see langword="false" />.
        /// </returns>
        public bool Equals(PlanetParams other) => Albedo == other.Albedo
            && AtmosphericPressure == other.AtmosphericPressure
            && AxialTilt == other.AxialTilt
            && EarthlikeAtmosphere == other.EarthlikeAtmosphere
            && Eccentricity == other.Eccentricity
            && HasMagnetosphere == other.HasMagnetosphere
            && MaxMass == other.MaxMass
            && NumSatellites == other.NumSatellites
            && Radius == other.Radius
            && RevolutionPeriod == other.RevolutionPeriod
            && RotationalPeriod == other.RotationalPeriod
            && SurfaceGravity == other.SurfaceGravity
            && SurfaceTemperature == other.SurfaceTemperature
            && WaterRatio == other.WaterRatio
            && WaterVaporRatio == other.WaterVaporRatio
            && AtmosphericRequirements.OrderBy(x => x.GetHashCode()).SequenceEqual(other.AtmosphericRequirements.OrderBy(x => x.GetHashCode()));

        /// <summary>Indicates whether this instance and a specified object are equal.</summary>
        /// <param name="obj">The object to compare with the current instance.</param>
        /// <returns>
        /// <see langword="true" /> if <paramref name="obj" /> and this instance are the same type
        /// and represent the same value; otherwise, <see langword="false" />.
        /// </returns>
        public override bool Equals(object? obj) => obj is PlanetParams planetParams && Equals(planetParams);

        /// <summary>Returns the hash code for this instance.</summary>
        /// <returns>A 32-bit signed integer that is the hash code for this instance.</returns>
        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(Albedo);
            hash.Add(AtmosphericPressure);
            hash.Add(AtmosphericRequirements);
            hash.Add(AxialTilt);
            hash.Add(EarthlikeAtmosphere);
            hash.Add(Eccentricity);
            hash.Add(HasMagnetosphere);
            hash.Add(MaxMass);
            hash.Add(NumSatellites);
            hash.Add(Radius);
            hash.Add(RevolutionPeriod);
            hash.Add(RotationalPeriod);
            hash.Add(SurfaceGravity);
            hash.Add(SurfaceTemperature);
            hash.Add(WaterRatio);
            hash.Add(WaterVaporRatio);
            return hash.ToHashCode();
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
            info.AddValue(nameof(Albedo), Albedo);
            info.AddValue(nameof(AtmosphericPressure), AtmosphericPressure);
            info.AddValue(nameof(AtmosphericRequirements), AtmosphericRequirements);
            info.AddValue(nameof(AxialTilt), AxialTilt);
            info.AddValue(nameof(EarthlikeAtmosphere), EarthlikeAtmosphere);
            info.AddValue(nameof(Eccentricity), Eccentricity);
            info.AddValue(nameof(HasMagnetosphere), HasMagnetosphere);
            info.AddValue(nameof(MaxMass), MaxMass);
            info.AddValue(nameof(NumSatellites), NumSatellites);
            info.AddValue(nameof(Radius), Radius);
            info.AddValue(nameof(RevolutionPeriod), RevolutionPeriod);
            info.AddValue(nameof(RotationalPeriod), RotationalPeriod);
            info.AddValue(nameof(SurfaceGravity), SurfaceGravity);
            info.AddValue(nameof(SurfaceTemperature), SurfaceTemperature);
            info.AddValue(nameof(WaterRatio), WaterRatio);
            info.AddValue(nameof(WaterVaporRatio), WaterVaporRatio);
        }

        /// <summary>Indicates whether two objects are equal.</summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns>
        /// <see langword="true" /> if <paramref name="left"/> is equal to <paramref
        /// name="right"/>; otherwise, <see langword="false" />.
        /// </returns>
        public static bool operator ==(PlanetParams left, PlanetParams right) => left.Equals(right);

        /// <summary>Indicates whether two objects are unequal.</summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns>
        /// <see langword="true" /> if <paramref name="left"/> is not equal to <paramref
        /// name="right"/>; otherwise, <see langword="false" />.
        /// </returns>
        public static bool operator !=(PlanetParams left, PlanetParams right) => !(left == right);
    }
}
