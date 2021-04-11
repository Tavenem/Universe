using System;
using Tavenem.HugeNumbers;
using Tavenem.Mathematics.HugeNumbers;

namespace Tavenem.Universe.Space
{
    /// <summary>
    /// The parameters which describe an orbit.
    /// </summary>
    public struct OrbitalParameters : IEquatable<OrbitalParameters>
    {
        /// <summary>
        /// The angle between the X-axis and the plane of the orbit (at the intersection where the
        /// orbit is rising, in radians). Values will be normalized between zero and 2π.
        /// </summary>
        public double AngleAscending { get; }

        /// <summary>
        /// The angle between the intersection of the X-Z plane through the center of the object
        /// orbited and the orbital plane, and the periapsis, in radians. Values will be normalized
        /// between zero and 2π
        /// </summary>
        public double ArgumentOfPeriapsis { get; }

        /// <summary>
        /// If true, all other parameters are ignored, and a circular orbit will be set based on the
        /// orbiting object's position.
        /// </summary>
        public bool Circular { get; }

        /// <summary>
        /// If true, all parameters other than eccentricity are ignored, and an orbit will
        /// be set based on the orbiting object's position.
        /// </summary>
        public bool FromEccentricity { get; }

        /// <summary>
        /// The eccentricity of this orbit.
        /// </summary>
        public double Eccentricity { get; }

        /// <summary>
        /// The angle between the X-Z plane through the center of the object orbited, and the plane
        /// of the orbit, in radians.
        /// </summary>
        public double Inclination { get; }

        /// <summary>
        /// The mass of the object or system being orbited.
        /// </summary>
        public HugeNumber OrbitedMass { get; }

        /// <summary>
        /// The position of the object or system being orbited at the epoch, as a vector in the same
        /// reference frame as the orbiting object.
        /// </summary>
        public Vector3 OrbitedPosition { get; }

        /// <summary>
        /// The periapsis of this orbit, in meters.
        /// </summary>
        public HugeNumber Periapsis { get; }

        /// <summary>
        /// The current true anomaly of this orbit.
        /// </summary>
        public double TrueAnomaly { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="OrbitalParameters"/>.
        /// </summary>
        /// <param name="orbitedMass">
        /// The mass of the object or system being orbited, in kg.
        /// </param>
        /// <param name="orbitedPosition">
        /// The position of the object or system being orbited at the epoch, as a vector in the same
        /// reference frame as the orbiting object.
        /// </param>
        /// <param name="periapsis">
        /// The distance between the objects at the closest point in the orbit.
        /// </param>
        /// <param name="eccentricity">The degree to which the orbit is non-circular. The absolute
        /// value will be used (i.e. negative values are treated as positives).</param>
        /// <param name="inclination">
        /// The angle between the X-Z plane through the center of the object orbited, and the plane
        /// of the orbit, in radians. Values will be normalized between zero and π.
        /// </param>
        /// <param name="angleAscending">
        /// The angle between the X-axis and the plane of the orbit (at the intersection where the
        /// orbit is rising, in radians). Values will be normalized between zero and 2π.
        /// </param>
        /// <param name="argumentOfPeriapsis">
        /// The angle between the intersection of the X-Z plane through the center of the object
        /// orbited and the orbital plane, and the periapsis, in radians. Values will be normalized
        /// between zero and 2π
        /// </param>
        /// <param name="trueAnomaly">
        /// The angle between periapsis and the current position of this object, from the center of
        /// the object orbited, in radians. Values will be normalized between zero and 2π
        /// </param>
        /// <param name="circular">
        /// If true, all other parameters are ignored, and a circular orbit will be set based on the
        /// orbiting object's position.
        /// </param>
        /// <param name="fromEccentricity">
        /// If true, all parameters other than eccentricity are ignored, and an orbit will
        /// be set based on the orbiting object's position.
        /// </param>
        [System.Text.Json.Serialization.JsonConstructor]
        public OrbitalParameters(
            HugeNumber orbitedMass,
            Vector3 orbitedPosition,
            HugeNumber periapsis,
            double eccentricity,
            double inclination,
            double angleAscending,
            double argumentOfPeriapsis,
            double trueAnomaly,
            bool circular = false,
            bool fromEccentricity = false)
        {
            Circular = circular;
            FromEccentricity = fromEccentricity;
            OrbitedMass = orbitedMass;
            OrbitedPosition = orbitedPosition;
            Periapsis = periapsis;
            Eccentricity = eccentricity;
            Inclination = inclination;
            AngleAscending = angleAscending;
            ArgumentOfPeriapsis = argumentOfPeriapsis;
            TrueAnomaly = trueAnomaly;
        }

        /// <summary>
        /// Gets an <see cref="OrbitalParameters"/> instance which defines a circular orbit.
        /// </summary>
        /// <param name="orbitedMass">
        /// The mass of the object or system being orbited, in kg.
        /// </param>
        /// <param name="orbitedPosition">
        /// The position of the object or system being orbited at the epoch, as a vector in the same
        /// reference frame as the orbiting object.
        /// </param>
        public static OrbitalParameters GetCircular(HugeNumber orbitedMass, Vector3 orbitedPosition)
            => new(
                orbitedMass,
                orbitedPosition,
                HugeNumber.Zero,
                0, 0, 0, 0, 0,
                true,
                false);

        /// <summary>
        /// Gets an <see cref="OrbitalParameters"/> instance which specifies that an orbit is to be
        /// determined based on eccentricity only.
        /// </summary>
        /// <param name="orbitedMass">
        /// The mass of the object or system being orbited, in kg.
        /// </param>
        /// <param name="orbitedPosition">
        /// The position of the object or system being orbited at the epoch, as a vector in the same
        /// reference frame as the orbiting object.
        /// </param>
        /// <param name="eccentricity">
        /// The eccentricity of this orbit.
        /// </param>
        public static OrbitalParameters GetFromEccentricity(HugeNumber orbitedMass, Vector3 orbitedPosition, double eccentricity)
            => new(
                orbitedMass,
                orbitedPosition,
                HugeNumber.Zero,
                eccentricity,
                0, 0, 0, 0,
                false,
                true);

        /// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        /// <see langword="true" /> if the current object is equal to the <paramref name="other" />
        /// parameter; otherwise, <see langword="false" />.
        /// </returns>
        public bool Equals(OrbitalParameters other) => AngleAscending == other.AngleAscending
            && ArgumentOfPeriapsis == other.ArgumentOfPeriapsis
            && Circular == other.Circular
            && FromEccentricity == other.FromEccentricity
            && Eccentricity == other.Eccentricity
            && Inclination == other.Inclination
            && OrbitedMass.Equals(other.OrbitedMass)
            && OrbitedPosition.Equals(other.OrbitedPosition)
            && Periapsis.Equals(other.Periapsis)
            && TrueAnomaly == other.TrueAnomaly;

        /// <summary>Indicates whether this instance and a specified object are equal.</summary>
        /// <param name="obj">The object to compare with the current instance.</param>
        /// <returns>
        /// <see langword="true" /> if <paramref name="obj" /> and this instance are the same type
        /// and represent the same value; otherwise, <see langword="false" />.
        /// </returns>
        public override bool Equals(object? obj) => obj is OrbitalParameters parameters && Equals(parameters);

        /// <summary>Returns the hash code for this instance.</summary>
        /// <returns>A 32-bit signed integer that is the hash code for this instance.</returns>
        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(AngleAscending);
            hash.Add(ArgumentOfPeriapsis);
            hash.Add(Circular);
            hash.Add(FromEccentricity);
            hash.Add(Eccentricity);
            hash.Add(Inclination);
            hash.Add(OrbitedMass);
            hash.Add(OrbitedPosition);
            hash.Add(Periapsis);
            hash.Add(TrueAnomaly);
            return hash.ToHashCode();
        }

        /// <summary>Indicates whether two objects are equal.</summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns>
        /// <see langword="true" /> if <paramref name="left"/> is equal to <paramref
        /// name="right"/>; otherwise, <see langword="false" />.
        /// </returns>
        public static bool operator ==(OrbitalParameters left, OrbitalParameters right) => left.Equals(right);

        /// <summary>Indicates whether two objects are unequal.</summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns>
        /// <see langword="true" /> if <paramref name="left"/> is not equal to <paramref
        /// name="right"/>; otherwise, <see langword="false" />.
        /// </returns>
        public static bool operator !=(OrbitalParameters left, OrbitalParameters right) => !(left == right);
    }
}
