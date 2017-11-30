namespace WorldFoundry
{
    /// <summary>
    /// Indicates the type of terrain on a <see cref="WorldGrids.Tile"/>, <see cref="WorldGrids.Corner"/>, or
    /// <see cref="WorldGrids.Edge"/>.
    /// </summary>
    public enum TerrainType
    {
        None = 0,
        Land = 1,
        Water = 2,
        Coast = 3
    }
}
