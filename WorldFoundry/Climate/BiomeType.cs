namespace WorldFoundry.Climate
{
    /// <summary>
    /// Describes the general biome of a location (a less specific grouping than a
    /// <see cref="ClimateType"/>-<see cref="EcologyType"/> combination).
    /// </summary>
    public enum BiomeType
    {
        /// <summary>
        /// Indicates an unset value, rather than having no growth (which is indicated by the
        /// appropriate biome type).
        /// </summary>
        None = 0,
#pragma warning disable CS1591
        Polar = 1,
        Tundra = 2,
        LichenWoodland = 3,
        ConiferousForest = 4,
        MixedForest = 5,
        Steppe = 6,
        ColdDesert = 7,
        DeciduousForest = 8,
        Shrubland = 9,
        HotDesert = 10,
        Savanna = 11,
        MonsoonForest = 12,
        RainForest = 13,
        Sea = 14,
#pragma warning restore CS1591
    }
}
