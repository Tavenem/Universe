using System;

namespace NeverFoundry.WorldFoundry.Space.Planetoids
{
    /// <summary>
    /// The reason(s) why a <see cref="Planetoid"/> does not meet an inhabitability
    /// requirement. A <see cref="FlagsAttribute"/> <see cref="Enum"/>.
    /// </summary>
    [Flags]
    public enum UninhabitabilityReason
    {
        /// <summary>
        /// No reason; the planet meets the requirement.
        /// </summary>
        None = 0,

        /// <summary>
        /// <para>
        /// The planet's star system is inhospitable to life.
        /// </para>
        /// <para>
        /// Typically due to a highly energetic or volatile star.
        /// </para>
        /// </summary>
        Inhospitable = 1,

        /// <summary>
        /// The planet does not have liquid water.
        /// </summary>
        NoWater = 1 << 1,

        /// <summary>
        /// The planet does not have a suitable atmosphere.
        /// </summary>
        UnbreathableAtmosphere = 1 << 2,

        /// <summary>
        /// The planet is too cold.
        /// </summary>
        TooCold = 1 << 3,

        /// <summary>
        /// The planet is too hot.
        /// </summary>
        TooHot = 1 << 4,

        /// <summary>
        /// The planet's atmospheric pressure is too low.
        /// </summary>
        LowPressure = 1 << 5,

        /// <summary>
        /// The planet's atmospheric pressure is too high.
        /// </summary>
        HighPressure = 1 << 6,

        /// <summary>
        /// The planet's gravity is too low.
        /// </summary>
        LowGravity = 1 << 7,

        /// <summary>
        /// The planet's gravity is too high.
        /// </summary>
        HighGravity = 1 << 8,
    }
}
