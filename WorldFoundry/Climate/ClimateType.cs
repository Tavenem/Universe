namespace WorldFoundry.Climate
{
    /// <summary>
    /// Indicates the climate of a location. Indicative of average temperature, not just latitude,
    /// and may be influenced by elevation.
    /// </summary>
    public enum ClimateType
    {
#pragma warning disable CS1591
        None = 0,
        Polar = 1,
        Subpolar = 2,
        Boreal = 3,
        CoolTemperate = 4,
        WarmTemperate = 5,
        Subtropical = 6,
        Tropical = 7,
        Supertropical = 8,
#pragma warning restore CS1591
    }
}
