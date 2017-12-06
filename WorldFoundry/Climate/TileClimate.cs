using System.Collections.Generic;
using System.Linq;
using WorldFoundry.WorldGrids;

namespace WorldFoundry.Climate
{
    /// <summary>
    /// Indicates the climate of a <see cref="WorldGrids.Tile"/> during a particular <see cref="Season"/>.
    /// </summary>
    public class TileClimate : IIndexedItem
    {
        internal List<AirCell> airCellList;
        /// <summary>
        /// The cells of air above this <see cref="TileClimate"/> during this <see cref="Season"/>.
        /// </summary>
        public ICollection<AirCell> AirCells { get; internal set; }

        /// <summary>
        /// The average atmospheric pressure in this <see cref="WorldGrids.Tile"/> during this <see
        /// cref="Season"/>, in kPa.
        /// </summary>
        public float AtmosphericPressure { get; internal set; }

        /// <summary>
        /// The index of this item.
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// The total precipitation in this <see cref="WorldGrids.Tile"/> during this <see cref="Season"/>,
        /// in mm. Counts all forms of precipitation, including the water-equivalent amount of
        /// snowfall (even though snow is also reported separately).
        /// </summary>
        public float Precipitation { get; internal set; }

        internal float Runoff { get; set; }

        /// <summary>
        /// The average thickness of sea ice in this <see cref="WorldGrids.Tile"/> during this <see
        /// cref="Season"/>, in meters.
        /// </summary>
        public float SeaIce { get; internal set; }

        /// <summary>
        /// The total amount of snow which falls in this <see cref="WorldGrids.Tile"/> during this <see
        /// cref="Season"/>, in mm. Assumes a typical ratio of 1mm water-equivalent = 13mm snow.
        /// </summary>
        public float Snow { get; internal set; }

        internal float SnowFall { get; set; }

        /// <summary>
        /// The average temperature in this <see cref="WorldGrids.Tile"/> during this <see cref="Season"/>,
        /// in K.
        /// </summary>
        public float Temperature { get; internal set; }

        /// <summary>
        /// The direction of the prevailing wind in this <see cref="WorldGrids.Tile"/> during this <see
        /// cref="Season"/>, as an angle in radians from north.
        /// </summary>
        public float WindDirection { get; internal set; }

        /// <summary>
        /// The average speed of the prevailing wind in this <see cref="WorldGrids.Tile"/> during this <see
        /// cref="Season"/>, in m/s.
        /// </summary>
        public float WindSpeed { get; internal set; }

        /// <summary>
        /// Retrieves the <see cref="AirCell"/> with the given index.
        /// </summary>
        /// <param name="index">The 0-based index to the <see cref="AirCell"/> to be retrieved.</param>
        /// <returns>The <see cref="AirCell"/> with the given index.</returns>
        public AirCell GetAirCell(int index) => AirCells.FirstOrDefault(x => x.Index == index);
    }
}
