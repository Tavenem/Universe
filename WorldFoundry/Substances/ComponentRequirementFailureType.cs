using System;

namespace WorldFoundry.Substances
{
    /// <summary>
    /// Indicates the reason(s) a <see cref="ComponentRequirement"/> failed.
    /// </summary>
    [Flags]
    public enum ComponentRequirementFailureType
    {
#pragma warning disable CS1591
        None = 0,
        Other = 1,
        TooLittle = 2,
        TooMuch = 4,
        WrongPhase = 8,
#pragma warning restore CS1591
    }
}
