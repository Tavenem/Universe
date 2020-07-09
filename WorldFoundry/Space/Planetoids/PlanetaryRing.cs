using NeverFoundry.MathAndScience.Numerics;
using Newtonsoft.Json;
using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace NeverFoundry.WorldFoundry.Space.Planetoids
{
    /// <summary>
    /// Contains information about a planetary ring (usually one of a collection that makes up a ring system).
    /// </summary>
    [Serializable]
    [JsonObject]
    public struct PlanetaryRing : ISerializable, IEquatable<PlanetaryRing>
    {
        /// <summary>
        /// Indicates that the <see cref="PlanetaryRing"/> is icy, rather than rocky.
        /// </summary>
        public bool Icy { get; }

        /// <summary>
        /// The inner radius of the <see cref="PlanetaryRing"/>, in m.
        /// </summary>
        public Number InnerRadius { get; }

        /// <summary>
        /// The outer radius of the <see cref="PlanetaryRing"/>, in m.
        /// </summary>
        public Number OuterRadius { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="PlanetaryRing"/>.
        /// </summary>
        /// <param name="icy">Whether the ring is icy, rather than rocky.</param>
        /// <param name="innerRadius">The inner radius of the ring, in m.</param>
        /// <param name="outerRadius">The outer radius of the ring, in m.</param>
        [JsonConstructor]
        [System.Text.Json.Serialization.JsonConstructor]
        public PlanetaryRing(bool icy, Number innerRadius, Number outerRadius)
        {
            Icy = icy;
            InnerRadius = innerRadius;
            OuterRadius = outerRadius;
        }

        private PlanetaryRing(SerializationInfo info, StreamingContext context) : this(
            (bool?)info.GetValue(nameof(Icy), typeof(bool)) ?? default,
            (Number?)info.GetValue(nameof(InnerRadius), typeof(Number)) ?? default,
            (Number?)info.GetValue(nameof(OuterRadius), typeof(Number)) ?? default)
        { }

        /// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        /// <see langword="true" /> if the current object is equal to the <paramref name="other" />
        /// parameter; otherwise, <see langword="false" />.
        /// </returns>
        public bool Equals(PlanetaryRing other) => Icy == other.Icy
            && InnerRadius.Equals(other.InnerRadius)
            && OuterRadius.Equals(other.OuterRadius);

        /// <summary>Indicates whether this instance and a specified object are equal.</summary>
        /// <param name="obj">The object to compare with the current instance.</param>
        /// <returns>
        /// <see langword="true" /> if <paramref name="obj" /> and this instance are the same type
        /// and represent the same value; otherwise, <see langword="false" />.
        /// </returns>
        public override bool Equals(object? obj) => obj is PlanetaryRing ring && Equals(ring);

        /// <summary>Returns the hash code for this instance.</summary>
        /// <returns>A 32-bit signed integer that is the hash code for this instance.</returns>
        public override int GetHashCode() => HashCode.Combine(Icy, InnerRadius, OuterRadius);

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
            info.AddValue(nameof(Icy), Icy);
            info.AddValue(nameof(InnerRadius), InnerRadius);
            info.AddValue(nameof(OuterRadius), OuterRadius);
        }

        /// <summary>Indicates whether two objects are equal.</summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns>
        /// <see langword="true" /> if <paramref name="left"/> is equal to <paramref
        /// name="right"/>; otherwise, <see langword="false" />.
        /// </returns>
        public static bool operator ==(PlanetaryRing left, PlanetaryRing right) => left.Equals(right);

        /// <summary>Indicates whether two objects are unequal.</summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns>
        /// <see langword="true" /> if <paramref name="left"/> is not equal to <paramref
        /// name="right"/>; otherwise, <see langword="false" />.
        /// </returns>
        public static bool operator !=(PlanetaryRing left, PlanetaryRing right) => !(left == right);
    }
}
