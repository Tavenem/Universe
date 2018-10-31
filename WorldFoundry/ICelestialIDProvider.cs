namespace WorldFoundry
{
    /// <summary>
    /// Provides a means of generating unique IDs.
    /// </summary>
    public interface ICelestialIDProvider
    {
        /// <summary>
        /// Generates a new, unique ID.
        /// </summary>
        /// <returns>A unique ID, as a string.</returns>
        string GetNewID();
    }
}
