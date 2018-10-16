namespace WorldFoundry.Climate
{
    /// <summary>
    /// Describes the climate of a location during a particular time.
    /// </summary>
    public class TileClimate
    {
        /// <summary>
        /// The avergae atmospheric pressure in this location during this time, in kPa.
        /// </summary>
        public float AtmosphericPressure { get; internal set; }

        /// <summary>
        /// The total precipitation in this location during this time, in mm. Counts all forms of
        /// precipitation, including the water-equivalent amount of snowfall (even though snow is
        /// also reported separately).
        /// </summary>
        public float Precipitation { get; internal set; }

        /// <summary>
        /// The total runoff in this location during this time, in m³/s.
        /// </summary>
        internal float Runoff { get; set; }

        /// <summary>
        /// The average thickness of sea ice in this location during this time, in meters.
        /// </summary>
        public float SeaIce { get; internal set; }

        /// <summary>
        /// The depth of persistent snow cover in this location during this time, in mm. Assumes a
        /// typical ratio of 1mm water-equivalent = 13mm snow.
        /// </summary>
        /// <remarks>
        /// Snow depth at any given time during the season will depend on the amount of time since
        /// the last snow event, the accumulation during that event, and the snow cover prior to the
        /// event. This number reflects the minimum level which remains unmelted between events, and
        /// at the end of the season.
        /// </remarks>
        public float SnowCover { get; internal set; }

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
