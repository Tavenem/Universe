﻿namespace WorldFoundry.Climate
{
    /// <summary>
    /// Describes the general biome of a <see cref="WorldGrids.Tile"/> (a less specific grouping than a
    /// <see cref="ClimateType"/>-<see cref="EcologyType"/> combination).
    /// </summary>
    public enum BiomeType
    {
        /// <summary>
        /// Indicates an unset value, rather than having no growth (which is indicated by the
        /// appropriate biome type).
        /// </summary>
        None,
#pragma warning disable CS1591
        Polar,
        Tundra,
        LichenWoodland,
        ConiferousForest,
        MixedForest,
        Steppe,
        ColdDesert,
        DeciduousForest,
        Shrubland,
        HotDesert,
        Savanna,
        MonsoonForest,
        RainForest,
        Sea
#pragma warning restore CS1591
    }
}
