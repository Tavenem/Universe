﻿using NeverFoundry.DataStorage;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NeverFoundry.WorldFoundry.Place
{
    /// <summary>
    /// Converts a <see cref="SurfaceRegion"/> to or from JSON.
    /// </summary>
    public class SurfaceRegionConverter : JsonConverter<SurfaceRegion>
    {
        /// <summary>Reads and converts the JSON to <see cref="SurfaceRegion"/>.</summary>
        /// <param name="reader">The reader.</param>
        /// <param name="typeToConvert">The type to convert.</param>
        /// <param name="options">An object that specifies serialization options to use.</param>
        /// <returns>The converted value.</returns>
        [return: MaybeNull]
        public override SurfaceRegion Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject
                || !reader.Read()
                || reader.TokenType != JsonTokenType.PropertyName
                || !reader.ValueTextEquals("id")
                || !reader.Read()
                || reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException();
            }
            var id = reader.GetString();
            if (id is null)
            {
                throw new JsonException();
            }

            if (!reader.Read()
                || reader.TokenType != JsonTokenType.PropertyName
                || !reader.ValueTextEquals(nameof(IIdItem.IdItemTypeName))
                || !reader.Read()
                || reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException();
            }
            var idItemTypeName = reader.GetString();
            if (string.IsNullOrEmpty(idItemTypeName)
                || !string.Equals(idItemTypeName, SurfaceRegion.SurfaceRegionIdItemTypeName))
            {
                throw new JsonException();
            }

            if (!reader.Read()
                || reader.TokenType != JsonTokenType.PropertyName
                || !reader.ValueTextEquals(nameof(Location.Shape))
                || !reader.Read()
                || reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException();
            }
            var shape = JsonSerializer.Deserialize<IShape>(ref reader, options);
            if (shape is null)
            {
                throw new JsonException();
            }
            if (reader.TokenType != JsonTokenType.EndObject)
            {
                throw new JsonException();
            }

            if (!reader.Read()
                || reader.TokenType != JsonTokenType.PropertyName
                || !reader.ValueTextEquals(nameof(Location.ParentId))
                || !reader.Read())
            {
                throw new JsonException();
            }
            var parentId = reader.GetString();

            if (!reader.Read()
                || reader.TokenType != JsonTokenType.PropertyName
                || !reader.ValueTextEquals(nameof(Location.AbsolutePosition))
                || !reader.Read())
            {
                throw new JsonException();
            }
            Vector3[]? absolutePosition;
            if (reader.TokenType == JsonTokenType.Null)
            {
                absolutePosition = null;
            }
            else
            {
                var vectorArrayConverter = (JsonConverter<Vector3[]>)options.GetConverter(typeof(Vector3[]));
                absolutePosition = vectorArrayConverter.Read(ref reader, typeof(Vector3[]), options);
            }

            byte[]? depthMap;
            if (!reader.Read()
                || reader.TokenType != JsonTokenType.PropertyName
                || !reader.ValueTextEquals("depthMap")
                || !reader.Read())
            {
                throw new JsonException();
            }
            if (reader.TokenType == JsonTokenType.Null)
            {
                depthMap = null;
            }
            else
            {
                depthMap = reader.GetBytesFromBase64();
            }

            byte[]? elevationMap;
            if (!reader.Read()
                || reader.TokenType != JsonTokenType.PropertyName
                || !reader.ValueTextEquals("elevationMap")
                || !reader.Read())
            {
                throw new JsonException();
            }
            if (reader.TokenType == JsonTokenType.Null)
            {
                elevationMap = null;
            }
            else
            {
                elevationMap = reader.GetBytesFromBase64();
            }

            byte[]? flowMap;
            if (!reader.Read()
                || reader.TokenType != JsonTokenType.PropertyName
                || !reader.ValueTextEquals("flowMap")
                || !reader.Read())
            {
                throw new JsonException();
            }
            if (reader.TokenType == JsonTokenType.Null)
            {
                flowMap = null;
            }
            else
            {
                flowMap = reader.GetBytesFromBase64();
            }

            double? maxFlow;
            if (!reader.Read()
                || reader.TokenType != JsonTokenType.PropertyName
                || !reader.ValueTextEquals("maxFlow")
                || !reader.Read()
                || reader.TokenType != JsonTokenType.Number)
            {
                throw new JsonException();
            }
            if (reader.TokenType == JsonTokenType.Null)
            {
                maxFlow = null;
            }
            else
            {
                maxFlow = reader.GetDouble();
            }

            if (!reader.Read()
                || reader.TokenType != JsonTokenType.PropertyName
                || !reader.ValueTextEquals("precipitationMaps")
                || !reader.Read())
            {
                throw new JsonException();
            }
            byte[][]? precipitationMaps;
            if (reader.TokenType == JsonTokenType.Null)
            {
                precipitationMaps = null;
            }
            else
            {
                var byteArrayConverter = (JsonConverter<byte[][]>)options.GetConverter(typeof(byte[][]));
                precipitationMaps = byteArrayConverter.Read(ref reader, typeof(byte[][]), options);
            }

            if (!reader.Read()
                || reader.TokenType != JsonTokenType.PropertyName
                || !reader.ValueTextEquals("snowfallMaps")
                || !reader.Read())
            {
                throw new JsonException();
            }
            byte[][]? snowfallMaps;
            if (reader.TokenType == JsonTokenType.Null)
            {
                snowfallMaps = null;
            }
            else
            {
                var byteArrayConverter = (JsonConverter<byte[][]>)options.GetConverter(typeof(byte[][]));
                snowfallMaps = byteArrayConverter.Read(ref reader, typeof(byte[][]), options);
            }

            byte[]? temperatureMapSummer;
            if (!reader.Read()
                || reader.TokenType != JsonTokenType.PropertyName
                || !reader.ValueTextEquals("temperatureMapSummer")
                || !reader.Read())
            {
                throw new JsonException();
            }
            if (reader.TokenType == JsonTokenType.Null)
            {
                temperatureMapSummer = null;
            }
            else
            {
                temperatureMapSummer = reader.GetBytesFromBase64();
            }

            byte[]? temperatureMapWinter;
            if (!reader.Read()
                || reader.TokenType != JsonTokenType.PropertyName
                || !reader.ValueTextEquals("temperatureMapWinter")
                || !reader.Read())
            {
                throw new JsonException();
            }
            if (reader.TokenType == JsonTokenType.Null)
            {
                temperatureMapWinter = null;
            }
            else
            {
                temperatureMapWinter = reader.GetBytesFromBase64();
            }

            while (reader.TokenType != JsonTokenType.EndObject)
            {
                reader.Read();
            }

            return new SurfaceRegion(
                id,
                idItemTypeName,
                shape,
                parentId,
                depthMap,
                elevationMap,
                flowMap,
                maxFlow,
                precipitationMaps,
                snowfallMaps,
                temperatureMapSummer,
                temperatureMapWinter,
                absolutePosition);
        }

        /// <summary>Writes a <see cref="SurfaceRegion"/> as JSON.</summary>
        /// <param name="writer">The writer to write to.</param>
        /// <param name="value">The value to convert to JSON.</param>
        /// <param name="options">An object that specifies serialization options to use.</param>
        public override void Write(Utf8JsonWriter writer, SurfaceRegion value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            writer.WriteString("id", value.Id);
            writer.WriteString(nameof(IIdItem.IdItemTypeName), value.IdItemTypeName);

            writer.WritePropertyName(nameof(Location.Shape));
            JsonSerializer.Serialize(writer, value.Shape, value.Shape.GetType(), options);

            if (value.ParentId is null)
            {
                writer.WriteNull(nameof(Location.ParentId));
            }
            else
            {
                writer.WriteString(nameof(Location.ParentId), value.ParentId);
            }

            if (value.AbsolutePosition is null)
            {
                writer.WriteNull(nameof(Location.AbsolutePosition));
            }
            else
            {
                writer.WritePropertyName(nameof(Location.AbsolutePosition));
                JsonSerializer.Serialize(writer, value.AbsolutePosition, options);
            }

            if (value._depthMap is null)
            {
                writer.WriteNull("depthMap");
            }
            else
            {
                writer.WriteBase64String("depthMap", value._depthMap);
            }

            if (value._elevationMap is null)
            {
                writer.WriteNull("elevationMap");
            }
            else
            {
                writer.WriteBase64String("elevationMap", value._elevationMap);
            }

            if (value._flowMap is null)
            {
                writer.WriteNull("flowMap");
            }
            else
            {
                writer.WriteBase64String("flowMap", value._flowMap);
            }

            if (value._maxFlow is null)
            {
                writer.WriteNull("maxFlow");
            }
            else
            {
                writer.WriteNumber("maxFlow", value._maxFlow.Value);
            }

            if (value._precipitationMaps is null)
            {
                writer.WriteNull("precipitationMaps");
            }
            else
            {
                writer.WritePropertyName("precipitationMaps");
                JsonSerializer.Serialize(writer, value._precipitationMaps, options);
            }

            if (value._snowfallMaps is null)
            {
                writer.WriteNull("snowfallMaps");
            }
            else
            {
                writer.WritePropertyName("snowfallMaps");
                JsonSerializer.Serialize(writer, value._snowfallMaps, options);
            }

            if (value._temperatureMapSummer is null)
            {
                writer.WriteNull("temperatureMapSummer");
            }
            else
            {
                writer.WriteBase64String("temperatureMapSummer", value._temperatureMapSummer);
            }

            if (value._temperatureMapWinter is null)
            {
                writer.WriteNull("temperatureMapWinter");
            }
            else
            {
                writer.WriteBase64String("temperatureMapWinter", value._temperatureMapWinter);
            }

            writer.WriteEndObject();
        }
    }
}