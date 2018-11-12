namespace WorldFoundry.Climate
{
    /// <summary>
    /// Indicates the relative level of humidity in a location.
    /// </summary>
    public enum HumidityType
    {
        /// <summary>
        /// Indicates an unset value, rather than zero humidity (which is indicated by <see cref="Superarid"/>).
        /// </summary>
        None = 0,
#pragma warning disable CS1591
        Superarid = 1,
        Perarid = 2,
        Arid = 3,
        Semiarid = 4,
        Subhumid = 5,
        Humid = 6,
        Perhumid = 7,
        Superhumid = 8,
#pragma warning restore CS1591
    }
}
