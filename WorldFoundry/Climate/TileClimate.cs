using System.Collections.Generic;
using System.Linq;
using WorldFoundry.WorldGrids;

namespace WorldFoundry.Climate
{
    /// <summary>
    /// Describes the climate of a <see cref="Tile"/> during a particular <see cref="Season"/>.
    /// </summary>
    public class TileClimate : DataItem, IIndexedItem
    {
        internal List<AirCell> airCellList;
        /// <summary>
        /// The cells of air above this <see cref="Tile"/> during this <see cref="Season"/>.
        /// </summary>
        internal ICollection<AirCell> AirCells { get; set; }

        /// <summary>
        /// The average atmospheric pressure in this <see cref="Tile"/> during this <see
        /// cref="Season"/>, in kPa.
        /// </summary>
        public float AtmosphericPressure { get; internal set; }

        /// <summary>
        /// The index of this item.
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// The total precipitation in this <see cref="Tile"/> during this <see cref="Season"/>,
        /// in mm. Counts all forms of precipitation, including the water-equivalent amount of
        /// snowfall (even though snow is also reported separately).
        /// </summary>
        public float Precipitation { get; internal set; }

        /// <summary>
        /// The total runoff in this <see cref="Tile"/> during this <see cref="Season"/>, in m³/s.
        /// </summary>
        internal float Runoff { get; set; }

        /// <summary>
        /// The average thickness of sea ice in this <see cref="Tile"/> during this <see
        /// cref="Season"/>, in meters.
        /// </summary>
        public float SeaIce { get; internal set; }

        /// <summary>
        /// The depth of persistent snow cover in this <see cref="Tile"/> during this <see
        /// cref="Season"/>, in mm. Assumes a typical ratio of 1mm water-equivalent = 13mm snow.
        /// </summary>
        /// <remarks>
        /// Snow depth at any given time during the season will depend on the amount of time since
        /// the last snow event, the accumulation during that event, and the snow cover prior to the
        /// event. This number reflects the minimum level which remains unmelted between events, and
        /// at the end of the season.
        /// </remarks>
        public float SnowCover { get; internal set; }

        /// <summary>
        /// The total amount of snow which falls in this <see cref="Tile"/> during this <see
        /// cref="Season"/>, in mm.
        /// </summary>
        /// <remarks>
        /// This may all fall during a single large snow event, or be divided equally among multiple
        /// snow events.
        /// </remarks>
        public float SnowFall { get; set; }

        /// <summary>
        /// The total amount of snow which falls in this <see cref="Tile"/> during this <see
        /// cref="Season"/>, in water-equivalent mm.
        /// </summary>
        internal float SnowFallWaterEquivalent { get; set; }

        /// <summary>
        /// The average temperature in this <see cref="Tile"/> during this <see cref="Season"/>,
        /// in K.
        /// </summary>
        public float Temperature { get; internal set; }

        /// <summary>
        /// The direction of the prevailing wind in this <see cref="Tile"/> during this <see
        /// cref="Season"/>, as an angle in radians from north.
        /// </summary>
        public float WindDirection { get; internal set; }

        /// <summary>
        /// The average speed of the prevailing wind in this <see cref="Tile"/> during this <see
        /// cref="Season"/>, in m/s.
        /// </summary>
        public float WindSpeed { get; internal set; }

        private TileClimate() { }

        /// <summary>
        /// Initializes a new instance of <see cref="TileClimate"/>.
        /// </summary>
        public TileClimate(int index) => Index = index;

        /// <summary>
        /// Retrieves the <see cref="AirCell"/> with the given index.
        /// </summary>
        /// <param name="index">The 0-based index to the <see cref="AirCell"/> to be retrieved.</param>
        /// <returns>The <see cref="AirCell"/> with the given index.</returns>
        internal AirCell GetAirCell(int index) => index == -1 ? null : AirCells.FirstOrDefault(x => x.Index == index);
    }
}
