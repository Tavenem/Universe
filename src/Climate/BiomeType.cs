namespace Tavenem.Universe.Climate;

/// <summary>
/// Describes the general biome of a location. A <see cref="FlagsAttribute"/> <see cref="Enum"/>.
/// </summary>
[Flags]
public enum BiomeType
{
    /// <summary>
    /// Any biome.
    /// </summary>
    Any = ~0,

    /// <summary>
    /// No biome indicated.
    /// </summary>
    None = 0,

    /// <summary>
    /// A polar biome.
    /// </summary>
    Polar = 1 << 0,

    /// <summary>
    /// A subpolar biome.
    /// </summary>
    Tundra = 1 << 1,

    /// <summary>
    /// An alpine biome.
    /// </summary>
    Alpine = 1 << 2,

    /// <summary>
    /// A subalpine biome.
    /// </summary>
    Subalpine = 1 << 3,

    /// <summary>
    /// An arid, boreal biome.
    /// </summary>
    LichenWoodland = 1 << 4,

    /// <summary>
    /// A humid, boreal biome.
    /// </summary>
    ConiferousForest = 1 << 5,

    /// <summary>
    /// A humid, cool temperate biome.
    /// </summary>
    MixedForest = 1 << 6,

    /// <summary>
    /// An arid, cool temperate biome.
    /// </summary>
    Steppe = 1 << 7,

    /// <summary>
    /// A cool, desert biome.
    /// </summary>
    ColdDesert = 1 << 8,

    /// <summary>
    /// A humid, warm temperate biome.
    /// </summary>
    DeciduousForest = 1 << 9,

    /// <summary>
    /// A moderately arid, warm temperate biome.
    /// </summary>
    Shrubland = 1 << 10,

    /// <summary>
    /// A hot, desert biome.
    /// </summary>
    HotDesert = 1 << 11,

    /// <summary>
    /// An arid, tropical or subtropical biome.
    /// </summary>
    Savanna = 1 << 12,

    /// <summary>
    /// A humid, tropical or subtropical biome.
    /// </summary>
    MonsoonForest = 1 << 13,

    /// <summary>
    /// A humid, tropical biome.
    /// </summary>
    RainForest = 1 << 14,

    /// <summary>
    /// Open water.
    /// </summary>
    Sea = 1 << 15,
}
