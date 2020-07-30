﻿using NeverFoundry.DataStorage;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.WorldFoundry.Place;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics.CodeAnalysis;

namespace NeverFoundry.WorldFoundry.Space.NewtonsoftJson
{
    /// <summary>
    /// Converts a <see cref="BlackHole"/> to and from JSON.
    /// </summary>
    public class BlackHoleConverter : JsonConverter
    {
        /// <summary>
        /// Determines whether this instance can convert the specified object type.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <returns>
        /// <see langword="true"/> if this instance can convert the specified object type;
        /// otherwise, <see langword="false"/>.
        /// </returns>
        public override bool CanConvert(Type objectType) => objectType == typeof(BlackHole);

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
            => FromJObj(JObject.Load(reader));

        /// <summary>
        /// Writes the JSON representation of the object.
        /// </summary>
        /// <param name="writer">The <see cref="JsonWriter" /> to write to.</param>
        /// <param name="value">The value.</param>
        /// <param name="serializer">The calling serializer.</param>
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value is null || !(value is BlackHole location))
            {
                writer.WriteNull();
                return;
            }

            writer.WriteStartObject();

            writer.WritePropertyName("id");
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

            writer.WritePropertyName("Supermassive");
            writer.WriteValue(location._supermassive);

            writer.WriteEndObject();
        }

        [return: MaybeNull]
        internal object FromJObj(JObject jObj)
        {
            string id;
            if (!jObj.TryGetValue("id", out var idToken)
                || idToken.Type != JTokenType.String)
            {
                throw new JsonException();
            }
            else
            {
                id = idToken.Value<string>();
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
                || !string.Equals(idItemTypeName, BlackHole.BlackHoleIdItemTypeName))
            {
                throw new JsonException();
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

            if (!jObj.TryGetValue(nameof(CosmicLocation.StructureType), out var structureTypeToken)
                || structureTypeToken.Type != JTokenType.Integer)
            {
                throw new JsonException();
            }
            else
            {
                _ = (CosmicStructureType)structureTypeToken.Value<int>();
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

            bool supermassive;
            if (!jObj.TryGetValue("Supermassive", out var toroidalToken)
                || toroidalToken.Type != JTokenType.Boolean)
            {
                throw new JsonException();
            }
            else
            {
                supermassive = toroidalToken.Value<bool>();
            }

            return new BlackHole(
                id,
                seed,
                parentId,
                absolutePosition,
                name,
                velocity,
                orbit,
                position,
                supermassive);
        }
    }
}
