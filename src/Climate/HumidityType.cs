namespace Tavenem.Universe.Climate;

/// <summary>
/// Indicates the relative level of humidity in a location. A <see cref="FlagsAttribute"/> <see
/// cref="Enum"/>.
/// </summary>
[Flags]
public enum HumidityType
{
    /// <summary>
    /// Any humidity.
    /// </summary>
    Any = ~0,

    /// <summary>
    /// No humidity level indicated.
    /// </summary>
    None = 0,

    /// <summary>
    /// Less than 125mm annual precipitation.
    /// </summary>
    Superarid = 1 << 0,

    /// <summary>
    /// At least 125mm, but less than 250mm annual precipitation.
    /// </summary>
    Perarid = 1 << 1,

    /// <summary>
    /// At least 250mm, but less than 500mm annual precipitation.
    /// </summary>
    Arid = 1 << 2,

    /// <summary>
    /// At least 500mm, but less than 1000mm annual precipitation.
    /// </summary>
    Semiarid = 1 << 3,

    /// <summary>
    /// At least 1000mm, but less than 2000mm annual precipitation.
    /// </summary>
    Subhumid = 1 << 4,

    /// <summary>
    /// At least 2000mm, but less than 4000mm annual precipitation.
    /// </summary>
    Humid = 1 << 5,

    /// <summary>
    /// At least 4000mm, but less than 8000mm annual precipitation.
    /// </summary>
    Perhumid = 1 << 6,

    /// <summary>
    /// At least 8000mm annual precipitation.
    /// </summary>
    Superhumid = 1 << 7,
}
