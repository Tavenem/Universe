using MongoDB.Bson.Serialization;

namespace WorldFoundry.Bson
{
    /// <summary>
    /// Static helper class for registering the serialization providers and class maps in this
    /// library in the correct order.
    /// </summary>
    public static class BsonRegistration
    {
        /// <summary>
        /// Registers this library's providers and maps in the correct order.
        /// </summary>
        /// <param name="idGenerator">The <see cref="IIdGenerator"/> to use. If left <see
        /// langword="null"/>, <see cref="MongoDB.Bson.Serialization.IdGenerators.GuidGenerator"/>
        /// will be used, corresponding to the default <see cref="IItemIdProvider"/>.</param>
        public static void Register(IIdGenerator? idGenerator = null)
        {
            SerializationProviders.Register();
            if (idGenerator != null)
            {
                ClassMaps.IdGenerator = idGenerator;
            }
            ClassMaps.Register();
        }
    }
}
