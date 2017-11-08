namespace WorldFoundry.Climate
{
    /// <summary>
    /// Indicates the relative level of humidity in a <see cref="WorldGrid.Tile"/>.
    /// </summary>
    public enum HumidityType
    {
        /// <summary>
        /// Indicates an unset value, rather than zero humidity (which is indicated by <see cref="Superarid"/>).
        /// </summary>
        None,
        Superarid,
        Perarid,
        Arid,
        Semiarid,
        Subhumid,
        Humid,
        Perhumid,
        Superhumid
    }
}
