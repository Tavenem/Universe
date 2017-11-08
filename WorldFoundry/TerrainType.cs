namespace WorldFoundry
{
    /// <summary>
    /// Indicates the type of terrain on a <see cref="WorldGrid.Tile"/>, <see cref="WorldGrid.Corner"/>, or
    /// <see cref="WorldGrid.Edge"/>.
    /// </summary>
    public enum TerrainType
    {
        None = 0,
        Land = 1,
        Water = 2,
        Coast = 3
    }
}
