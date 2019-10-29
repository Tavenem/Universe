using System;

namespace NeverFoundry.WorldFoundry.Climate
{
    /// <summary>
    /// Describes the ecology of a location. A <see cref="FlagsAttribute"/> <see cref="Enum"/>.
    /// </summary>
    public enum EcologyType
    {
        /// <summary>
        /// Any ecology.
        /// </summary>
        Any = ~0,

        /// <summary>
        /// No ecology indicated.
        /// </summary>
        None = 0,

        /// <summary>
        /// A very arid ecology.
        /// </summary>
        Desert = 1 << 0,

        /// <summary>
        /// A humid, polar ecology.
        /// </summary>
        Ice = 1 << 1,

        /// <summary>
        /// A very arid, subpolar ecology.
        /// </summary>
        DryTundra = 1 << 2,

        /// <summary>
        /// A moderately humid, subpolar ecology.
        /// </summary>
        MoistTundra = 1 << 3,

        /// <summary>
        /// A humid subpolar ecology.
        /// </summary>
        WetTundra = 1 << 4,

        /// <summary>
        /// A very humid, subpolar ecology.
        /// </summary>
        RainTundra = 1 << 5,

        /// <summary>
        /// An arid ecology.
        /// </summary>
        DesertScrub = 1 << 6,

        /// <summary>
        /// An arid, boreal ecology.
        /// </summary>
        DryScrub = 1 << 7,

        /// <summary>
        /// An arid, cool temperate ecology.
        /// </summary>
        Steppe = 1 << 8,

        /// <summary>
        /// An arid, warm temperate ecology.
        /// </summary>
        ThornScrub = 1 << 9,

        /// <summary>
        /// An arid, tropical or subtropical ecology.
        /// </summary>
        ThornWoodland = 1 << 10,

        /// <summary>
        /// A semiarid, tropical ecology.
        /// </summary>
        VeryDryForest = 1 << 11,

        /// <summary>
        /// A semiarid, forest ecology.
        /// </summary>
        DryForest = 1 << 12,

        /// <summary>
        /// A semihumid, forest ecology.
        /// </summary>
        MoistForest = 1 << 13,

        /// <summary>
        /// A humid, forest ecology.
        /// </summary>
        WetForest = 1 << 14,

        /// <summary>
        /// A very humid, tropical ecology.
        /// </summary>
        RainForest = 1 << 15,

        /// <summary>
        /// Open water.
        /// </summary>
        Sea = 1 << 16,
    }
}
