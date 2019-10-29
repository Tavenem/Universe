using NeverFoundry.MathAndScience.Numerics;
using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace NeverFoundry.WorldFoundry.CelestialBodies.Planetoids.Planets
{
    /// <summary>
    /// Contains information about a planetary ring (usually one of a collection that makes up a ring system).
    /// </summary>
    [Serializable]
    public struct PlanetaryRing : ISerializable
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
        public PlanetaryRing(bool icy, Number innerRadius, Number outerRadius)
        {
            Icy = icy;
            InnerRadius = innerRadius;
            OuterRadius = outerRadius;
        }

        private PlanetaryRing(SerializationInfo info, StreamingContext context) : this(
            (bool)info.GetValue(nameof(Icy), typeof(bool)),
            (Number)info.GetValue(nameof(InnerRadius), typeof(Number)),
            (Number)info.GetValue(nameof(OuterRadius), typeof(Number))) { }

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
    }
}
