namespace WorldFoundry.Climate
{
    /// <summary>
    /// Describes the climate of a location during a particular time.
    /// </summary>
    public class TileClimate
    {
        /// <summary>
        /// The total precipitation in this location during this time, in mm. Counts all forms of
        /// precipitation, including the water-equivalent amount of snowfall (even though snow is
        /// also reported separately).
        /// </summary>
        public float Precipitation { get; internal set; }

        /// <summary>
        /// Whether sea ice is present in this location during this time.
        /// </summary>
        public bool SeaIce { get; internal set; }

        /// <summary>
        /// Whether there is snow cover on the ground in this location during this time.
        /// </summary>
        public bool SnowCover { get; internal set; }

        /// <summary>
        /// The total amount of snow which falls in this location during this time, in mm.
        /// </summary>
        /// <remarks>
        /// This may all fall during a single large snow event, or be divided equally among multiple
        /// snow events.
        /// </remarks>
        public float SnowFall { get; set; }

        /// <summary>
        /// The average temperature in this location during this time, in K.
        /// </summary>
        public float Temperature { get; internal set; }
    }
}
