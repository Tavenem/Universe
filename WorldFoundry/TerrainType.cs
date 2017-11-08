namespace WorldFoundry
{
    /// <summary>
    /// Indicates the type of terrain on a <see cref="Grid.Tile"/>, <see cref="Grid.Corner"/>, or
    /// <see cref="Grid.Edge"/>.
    /// </summary>
    public enum TerrainType
    {
        None = 0,
        Land = 1,
        Water = 2,
        Coast = 3
    }
}
