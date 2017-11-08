namespace WorldFoundry.Climate
{
    /// <summary>
    /// Describes the ecology of a <see cref="WorldGrid.Tile"/>.
    /// </summary>
    public enum EcologyType
    {
        /// <summary>
        /// Indicates an unset value, rather than having no growth (which is indicated by <see
        /// cref="Desert"/> or <see cref="Sea"/>).
        /// </summary>
        None,
        Desert,
        Ice,
        DryTundra,
        MoistTundra,
        WetTundra,
        RainTundra,
        DesertScrub,
        DryScrub,
        Steppe,
        ThornScrub,
        ThornWoodland,
        VeryDryForest,
        DryForest,
        MoistForest,
        WetForest,
        RainForest,
        Sea
    }
}
