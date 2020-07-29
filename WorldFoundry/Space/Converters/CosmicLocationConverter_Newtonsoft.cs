using NeverFoundry.DataStorage;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.WorldFoundry.Place;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics.CodeAnalysis;

namespace NeverFoundry.WorldFoundry.Space.NewtonsoftJson
{
    /// <summary>
    /// Converts a <see cref="CosmicLocation"/> to and from JSON.
    /// </summary>
    public class CosmicLocationConverter : JsonConverter
    {
        /// <summary>
        /// Determines whether this instance can convert the specified object type.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <returns>
        /// <see langword="true"/> if this instance can convert the specified object type;
        /// otherwise, <see langword="false"/>.
        /// </returns>
        public override bool CanConvert(Type objectType) => objectType == typeof(CosmicLocation);

        /// <summary>
        /// Reads the JSON representation of the object.
        /// </summary>
        /// <param name="reader">The <see cref="JsonReader" /> to read from.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="existingValue">The existing value of object being read.</param>
        /// <param name="serializer">The calling serializer.</param>
        /// <returns>The object value.</returns>
        [return: MaybeNull]
        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            var jObj = JObject.Load(reader);

            string id;
            if (!jObj.TryGetValue(nameof(IIdItem.Id), out var idToken)
                || idToken.Type != JTokenType.String)
            {
                throw new JsonException();
            }
            else
            {
                id = idToken.Value<string>();
            }

            uint seed;
            if (!jObj.TryGetValue("seed", out var seedToken)
                || seedToken.Type != JTokenType.Integer)
            {
                throw new JsonException();
            }
            else
            {
                seed = seedToken.Value<uint>();
            }

            CosmicStructureType structureType;
            if (jObj.TryGetValue(nameof(CosmicLocation.StructureType), out var structureTypeToken))
            {
                if (structureTypeToken.Type == JTokenType.Integer)
                {
                    structureType = (CosmicStructureType)structureTypeToken.Value<int>();

                    if (structureType == CosmicStructureType.AsteroidField
                        || structureType == CosmicStructureType.OortCloud)
                    {
                        return new AsteroidFieldConverter().FromJObj(jObj);
                    }
                    if (structureType == CosmicStructureType.BlackHole)
                    {
                        return new BlackHoleConverter().FromJObj(jObj);
                    }
                    if (structureType == CosmicStructureType.StarSystem)
                    {
                        return new StarSystemConverter().FromJObj(jObj);
                    }
                }
                else
                {
                    throw new JsonException();
                }
            }
            else if (jObj.TryGetValue(nameof(Star.StarType), out _))
            {
                return new StarConverter().FromJObj(jObj);
            }
            else if (jObj.TryGetValue(nameof(Planetoid.PlanetType), out _))
            {
                return new PlanetoidConverter().FromJObj(jObj);
            }
            else
            {
                throw new JsonException();
            }

            string idItemTypeName;
            if (!jObj.TryGetValue(nameof(IIdItem.IdItemTypeName), out var idItemTypeNameToken)
                || idItemTypeNameToken.Type != JTokenType.String)
            {
                throw new JsonException();
            }
            else
            {
                idItemTypeName = idItemTypeNameToken.Value<string>();
            }
            if (string.IsNullOrEmpty(idItemTypeName)
                || !string.Equals(idItemTypeName, CosmicLocation.CosmicLocationIdItemTypeName))
            {
                throw new JsonException();
            }

            string? parentId;
            if (!jObj.TryGetValue(nameof(CosmicLocation.Name), out var parentIdToken))
            {
                throw new JsonException();
            }
            if (parentIdToken.Type == JTokenType.Null)
            {
                parentId = null;
            }
            else if (parentIdToken.Type == JTokenType.String)
            {
                parentId = parentIdToken.Value<string>();
            }
            else
            {
                throw new JsonException();
            }

            Vector3[]? absolutePosition;
            if (!jObj.TryGetValue(nameof(Location.AbsolutePosition), out var absolutePositionToken))
            {
                throw new JsonException();
            }
            if (absolutePositionToken.Type == JTokenType.Null)
            {
                absolutePosition = null;
            }
            else if (absolutePositionToken.Type == JTokenType.Array
                && absolutePositionToken is JArray absolutePositionArray)
            {
                absolutePosition = absolutePositionArray.ToObject<Vector3[]>();
            }
            else
            {
                throw new JsonException();
            }

            string? name;
            if (!jObj.TryGetValue(nameof(CosmicLocation.Name), out var nameToken))
            {
                throw new JsonException();
            }
            if (nameToken.Type == JTokenType.Null)
            {
                name = null;
            }
            else if (nameToken.Type == JTokenType.String)
            {
                name = nameToken.Value<string>();
            }
            else
            {
                throw new JsonException();
            }

            Vector3 velocity;
            if (!jObj.TryGetValue(nameof(CosmicLocation.Velocity), out var velocityToken)
                || velocityToken.Type != JTokenType.Object)
            {
                throw new JsonException();
            }
            else
            {
                velocity = velocityToken.ToObject<Vector3>();
            }

            Orbit? orbit;
            if (!jObj.TryGetValue(nameof(CosmicLocation.Orbit), out var orbitToken))
            {
                throw new JsonException();
            }
            if (orbitToken.Type == JTokenType.Null)
            {
                orbit = null;
            }
            else if (orbitToken.Type == JTokenType.Object)
            {
                orbit = orbitToken.ToObject<Orbit>();
            }
            else
            {
                throw new JsonException();
            }

            Vector3 position;
            if (!jObj.TryGetValue(nameof(Location.Position), out var positionToken)
                || positionToken.Type != JTokenType.Object)
            {
                throw new JsonException();
            }
            else
            {
                position = positionToken.ToObject<Vector3>();
            }

            double? temperature;
            if (!jObj.TryGetValue(nameof(CosmicLocation.Temperature), out var temperatureToken))
            {
                throw new JsonException();
            }
            if (temperatureToken.Type == JTokenType.Null)
            {
                temperature = null;
            }
            else if (temperatureToken.Type == JTokenType.Float)
            {
                temperature = temperatureToken.Value<double>();
            }
            else
            {
                throw new JsonException();
            }

            return new CosmicLocation(
                id,
                seed,
                structureType,
                parentId,
                absolutePosition,
                name,
                velocity,
                orbit,
                position,
                temperature);
        }

        /// <summary>
        /// Writes the JSON representation of the object.
        /// </summary>
        /// <param name="writer">The <see cref="JsonWriter" /> to write to.</param>
        /// <param name="value">The value.</param>
        /// <param name="serializer">The calling serializer.</param>
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value is null)
            {
                writer.WriteNull();
                return;
            }

            if (value is AsteroidField asteroidField)
            {
                new AsteroidFieldConverter().WriteJson(writer, asteroidField, serializer);
                return;
            }
            if (value is BlackHole blackHole)
            {
                new BlackHoleConverter().WriteJson(writer, blackHole, serializer);
                return;
            }
            if (value is Planetoid planetoid)
            {
                new PlanetoidConverter().WriteJson(writer, planetoid, serializer);
                return;
            }
            if (value is Star star)
            {
                new StarConverter().WriteJson(writer, star, serializer);
                return;
            }
            if (value is StarSystem starSystem)
            {
                new StarSystemConverter().WriteJson(writer, starSystem, serializer);
                return;
            }

            if (!(value is CosmicLocation location))
            {
                writer.WriteNull();
                return;
            }

            writer.WriteStartObject();

            writer.WritePropertyName(nameof(IIdItem.Id));
            writer.WriteValue(location.Id);

            writer.WritePropertyName(nameof(IIdItem.IdItemTypeName));
            writer.WriteValue(location.IdItemTypeName);

            writer.WritePropertyName("seed");
            writer.WriteValue(location._seed);

            writer.WritePropertyName(nameof(CosmicLocation.StructureType));
            writer.WriteValue((int)location.StructureType);

            writer.WritePropertyName(nameof(Location.ParentId));
            writer.WriteValue(location.ParentId);

            writer.WritePropertyName(nameof(Location.AbsolutePosition));
            if (location.AbsolutePosition is null)
            {
                writer.WriteNull();
            }
            else
            {
                serializer.Serialize(writer, location.AbsolutePosition, typeof(Vector3[]));
            }

            writer.WritePropertyName(nameof(CosmicLocation.Name));
            writer.WriteValue(location.Name);

            writer.WritePropertyName(nameof(CosmicLocation.Velocity));
            serializer.Serialize(writer, location.Velocity, typeof(Vector3));

            writer.WritePropertyName(nameof(CosmicLocation.Orbit));
            if (location.Orbit.HasValue)
            {
                serializer.Serialize(writer, location.Orbit.Value, typeof(Orbit));
            }
            else
            {
                writer.WriteNull();
            }

            writer.WritePropertyName(nameof(Location.Position));
            serializer.Serialize(writer, location.Position, typeof(Vector3));

            writer.WritePropertyName(nameof(MathAndScience.Chemistry.IMaterial.Temperature));
            writer.WriteValue(location.Material.Temperature);

            writer.WriteEndObject();
        }
    }
}
