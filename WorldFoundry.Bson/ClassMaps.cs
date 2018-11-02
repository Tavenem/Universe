using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using WorldFoundry.Space;

namespace WorldFoundry.Bson
{
    /// <summary>
    /// Static class for registering BSON class maps.
    /// </summary>
    public static class ClassMaps
    {
        /// <summary>
        /// <para>
        /// The <see cref="IIdGenerator"/> to use. By default, <see cref="GuidGenerator"/> will be
        /// used, corresponding to the default <see cref="IIdProvider"/>.
        /// </para>
        /// <para>
        /// Must be set <i>before</i> calling <see cref="Register"/>.
        /// </para>
        /// </summary>
        public static IIdGenerator IdGenerator { get; set; } = GuidGenerator.Instance;

        /// <summary>
        /// Register all requisite class maps.
        /// </summary>
        public static void Register()
        {
            BsonClassMap.RegisterClassMap<ICelestialLocation>(cm =>
            {
                cm.AutoMap();
                cm.SetIsRootClass(true);
                cm.MapIdMember(x => x.Id).SetIdGenerator(IdGenerator);
            });
        }
    }
}
