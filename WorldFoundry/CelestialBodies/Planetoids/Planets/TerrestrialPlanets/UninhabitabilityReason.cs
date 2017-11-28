using System;

namespace WorldFoundry.CelestialBodies.Planetoids.Planets.TerrestrialPlanets
{
    [Flags]
    public enum UninhabitabilityReason
    {
#pragma warning disable CS1591
        None = 0,
        Other = 1,
        NoWater = 2,
        UnbreathableAtmosphere = 4,
        TooCold = 8,
        TooHot = 16,
        LowPressure = 32,
        HighPressure = 64,
        LowGravity = 128,
        HighGravity = 256
#pragma warning restore CS1591
    }
}
