namespace WorldFoundry.Bson
{
    /// <summary>
    /// Static helper class for registering the serialization providers in this library in the
    /// correct order.
    /// </summary>
    public static class SerializationProviders
    {
        /// <summary>
        /// Registers this library's providers in the correct order.
        /// </summary>
        public static void Register()
        {
            Substances.Bson.BsonRegistration.Register();
            BsonFoundry.SerializationProviders.Register("WorldFoundry.");
        }
    }
}
