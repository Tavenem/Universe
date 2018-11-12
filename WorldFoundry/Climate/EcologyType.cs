namespace WorldFoundry.Climate
{
    /// <summary>
    /// Describes the ecology of a location.
    /// </summary>
    public enum EcologyType
    {
        /// <summary>
        /// Indicates an unset value, rather than having no growth (which is indicated by <see
        /// cref="Desert"/> or <see cref="Sea"/>).
        /// </summary>
        None = 0,
#pragma warning disable CS1591
        Desert = 1,
        Ice = 2,
        DryTundra = 3,
        MoistTundra = 4,
        WetTundra = 5,
        RainTundra = 6,
        DesertScrub = 7,
        DryScrub = 8,
        Steppe = 9,
        ThornScrub = 10,
        ThornWoodland = 11,
        VeryDryForest = 12,
        DryForest = 13,
        MoistForest = 14,
        WetForest = 15,
        RainForest = 16,
        Sea = 17,
#pragma warning restore CS1591
    }
}
