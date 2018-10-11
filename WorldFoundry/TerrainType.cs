namespace WorldFoundry
{
    /// <summary>
    /// Indicates the type of terrain on a <see cref="WorldGrids.Tile"/>, <see cref="WorldGrids.Corner"/>, or
    /// <see cref="WorldGrids.Edge"/>.
    /// </summary>
    public enum TerrainType
    {
#pragma warning disable CS1591
        None = 0,
        Land = 1,
        Water = 2,
        Coast = 3
#pragma warning restore CS1591
    }
}
