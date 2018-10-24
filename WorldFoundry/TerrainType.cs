namespace WorldFoundry
{
    /// <summary>
    /// Indicates the type of terrain on a <see cref="WorldGrids.Tile"/>, <see cref="WorldGrids.Corner"/>, or
    /// <see cref="WorldGrids.Edge"/>.
    /// </summary>
    public enum TerrainType
    {
        /// <summary>
        /// The center and all corners of this tile are above sea level.
        /// </summary>
        Land = 0,

        /// <summary>
        /// The center and all corners of this tile are below sea level.
        /// </summary>
        Water = 1,

        /// <summary>
        /// The center and at least one corner of this tile are on opposite sides of sea level.
        /// </summary>
        Coast = 2,
    }
}
