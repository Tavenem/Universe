using System;

namespace NeverFoundry.WorldFoundry.Climate
{
    /// <summary>
    /// Indicates the climate of a location. Indicative of average temperature, not just latitude,
    /// and may be influenced by elevation. A <see cref="FlagsAttribute"/> <see cref="Enum"/>.
    /// </summary>
    [Flags]
    public enum ClimateType
    {
#pragma warning disable RCS1157 // Composite enum value contains undefined flag.
        /// <summary>
        /// Any climate.
        /// </summary>
        Any = ~0,
#pragma warning restore RCS1157 // Composite enum value contains undefined flag.

        /// <summary>
        /// No climate indicated.
        /// </summary>
        None = 0,

        /// <summary>
        /// Average annual temperature &lt;= 1.5K over the melting point of water.
        /// </summary>
        Polar = 1 << 0,

        /// <summary>
        /// Average annual temperature &gt; 1.5K and &lt;= 3K over the melting point of water.
        /// </summary>
        Subpolar = 1 << 1,

        /// <summary>
        /// Average annual temperature &gt; 3K and &lt;= 6K over the melting point of water.
        /// </summary>
        Boreal = 1 << 2,

        /// <summary>
        /// Average annual temperature &gt; 6K and &lt;= 12K over the melting point of water.
        /// </summary>
        CoolTemperate = 1 << 3,

        /// <summary>
        /// Average annual temperature &gt; 12K and &lt;= 18K over the melting point of water.
        /// </summary>
        WarmTemperate = 1 << 4,

        /// <summary>
        /// Average annual temperature &gt; 18K and &lt;= 24K over the melting point of water.
        /// </summary>
        Subtropical = 1 << 5,

        /// <summary>
        /// Average annual temperature &gt; 24K and &lt;= 68K over the melting point of water.
        /// </summary>
        Tropical = 1 << 6,

        /// <summary>
        /// Average annual temperature &gt; 68K over the melting point of water.
        /// </summary>
        Supertropical = 1 << 7,
    }
}
