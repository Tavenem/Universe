using System;

namespace WorldFoundry.Substances
{
    /// <summary>
    /// Indicates the reason(s) a <see cref="SubstanceRequirement"/> failed.
    /// </summary>
    [Flags]
    public enum SubstanceRequirementFailureType
    {
#pragma warning disable CS1591
        None = 0,
        Other = 1,
        Missing = 2,
        TooLittle = 4,
        TooMuch = 8,
        WrongPhase = 16,
#pragma warning restore CS1591
    }
}
