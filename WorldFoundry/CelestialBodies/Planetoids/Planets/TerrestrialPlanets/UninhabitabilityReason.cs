using System;

namespace NeverFoundry.WorldFoundry.CelestialBodies.Planetoids.Planets.TerrestrialPlanets
{
    /// <summary>
    /// The reason(s) why a <see cref="TerrestrialPlanet"/> does not meet an inhabitability
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
        /// The planet fails to meet the requirement for an unspecified reason.
        /// </summary>
        Other = 1,

        /// <summary>
        /// The planet does not have liquid water.
        /// </summary>
        NoWater = 2,

        /// <summary>
        /// The planet does not have a suitable atmosphere.
        /// </summary>
        UnbreathableAtmosphere = 4,

        /// <summary>
        /// The planet is too cold.
        /// </summary>
        TooCold = 8,

        /// <summary>
        /// The planet is too hot.
        /// </summary>
        TooHot = 16,

        /// <summary>
        /// The planet's atmospheric pressure is too low.
        /// </summary>
        LowPressure = 32,

        /// <summary>
        /// The planet's atmospheric pressure is too high.
        /// </summary>
        HighPressure = 64,

        /// <summary>
        /// The planet's gravity is too low.
        /// </summary>
        LowGravity = 128,

        /// <summary>
        /// The planet's gravity is too high.
        /// </summary>
        HighGravity = 256,
    }
}
