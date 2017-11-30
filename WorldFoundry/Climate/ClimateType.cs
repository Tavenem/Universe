namespace WorldFoundry.Climate
{
    /// <summary>
    /// Indicates the climate of a <see cref="WorldGrids.Tile"/>. Indicative of average temperature, not
    /// just latitude, and may be influenced by elevation.
    /// </summary>
    public enum ClimateType
    {
        None,
        Polar,
        Subpolar,
        Boreal,
        CoolTemperate,
        WarmTemperate,
        Subtropical,
        Tropical,
        Supertropical
    }
}
