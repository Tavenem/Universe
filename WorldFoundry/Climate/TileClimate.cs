using System.Collections.Generic;

namespace WorldFoundry.Climate
{
    /// <summary>
    /// Indicates the climate of a <see cref="WorldGrid.Tile"/> during a particular <see cref="Season"/>.
    /// </summary>
    public class TileClimate
    {
        internal List<AirCell> AirCells { get; set; } = new List<AirCell>();

        /// <summary>
        /// The average atmospheric pressure in this <see cref="WorldGrid.Tile"/> during this <see
        /// cref="Season"/>, in kPa.
        /// </summary>
        public float AtmosphericPressure { get; internal set; }

        /// <summary>
        /// The total precipitation in this <see cref="WorldGrid.Tile"/> during this <see cref="Season"/>,
        /// in mm. Counts all forms of precipitation, including the water-equivalent amount of
        /// snowfall (even though snow is also reported separately).
        /// </summary>
        public float Precipitation { get; internal set; }

        internal float Runoff { get; set; }

        /// <summary>
        /// The average thickness of sea ice in this <see cref="WorldGrid.Tile"/> during this <see
        /// cref="Season"/>, in meters.
        /// </summary>
        public float SeaIce { get; internal set; }

        /// <summary>
        /// The total amount of snow which falls in this <see cref="WorldGrid.Tile"/> during this <see
        /// cref="Season"/>, in mm. Assumes a typical ratio of 1mm water-equivalent = 13mm snow.
        /// </summary>
        public float Snow { get; internal set; }

        internal float SnowFall { get; set; }

        /// <summary>
        /// The average temperature in this <see cref="WorldGrid.Tile"/> during this <see cref="Season"/>,
        /// in K.
        /// </summary>
        public float Temperature { get; internal set; }

        /// <summary>
        /// The direction of the prevailing wind in this <see cref="WorldGrid.Tile"/> during this <see
        /// cref="Season"/>, as an angle in radians from north.
        /// </summary>
        public float WindDirection { get; internal set; }

        /// <summary>
        /// The average speed of the prevailing wind in this <see cref="WorldGrid.Tile"/> during this <see
        /// cref="Season"/>, in m/s.
        /// </summary>
        public float WindSpeed { get; internal set; }
    }
}
