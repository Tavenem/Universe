using NeverFoundry.DataStorage;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NeverFoundry.WorldFoundry.Planet
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

            if (!reader.Read()
                || reader.TokenType != JsonTokenType.PropertyName
                || !reader.ValueTextEquals("ElevationMapPath")
                || !reader.Read())
            {
                throw new JsonException();
            }
            var elevationMapPath = reader.GetString();

            if (!reader.Read()
                || reader.TokenType != JsonTokenType.PropertyName
                || !reader.ValueTextEquals("PrecipitationMapPaths")
                || !reader.Read())
            {
                throw new JsonException();
            }
            string?[]? precipitationMapPaths;
            if (reader.TokenType == JsonTokenType.Null)
            {
                precipitationMapPaths = null;
            }
            else if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw new JsonException();
            }
            else
            {
                precipitationMapPaths = JsonSerializer.Deserialize<string?[]>(ref reader, options);
                if (precipitationMapPaths is null
                    || reader.TokenType != JsonTokenType.EndArray)
                {
                    throw new JsonException();
                }
            }

            if (!reader.Read()
                || reader.TokenType != JsonTokenType.PropertyName
                || !reader.ValueTextEquals("SnowfallMapPaths")
                || !reader.Read())
            {
                throw new JsonException();
            }
            string?[]? snowfallMapPaths;
            if (reader.TokenType == JsonTokenType.Null)
            {
                snowfallMapPaths = null;
            }
            else if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw new JsonException();
            }
            else
            {
                snowfallMapPaths = JsonSerializer.Deserialize<string?[]>(ref reader, options);
                if (snowfallMapPaths is null
                    || reader.TokenType != JsonTokenType.EndArray)
                {
                    throw new JsonException();
                }
            }

            if (!reader.Read()
                || reader.TokenType != JsonTokenType.PropertyName
                || !reader.ValueTextEquals("TemperatureMapSummerPath")
                || !reader.Read())
            {
                throw new JsonException();
            }
            var temperatureMapSummerPath = reader.GetString();

            if (!reader.Read()
                || reader.TokenType != JsonTokenType.PropertyName
                || !reader.ValueTextEquals("TemperatureMapWinterPath")
                || !reader.Read())
            {
                throw new JsonException();
            }
            var temperatureMapWinterPath = reader.GetString();

            while (reader.TokenType != JsonTokenType.EndObject)
            {
                reader.Read();
            }

            return new SurfaceRegion(
                id,
                idItemTypeName,
                shape,
                parentId,
                elevationMapPath,
                precipitationMapPaths,
                snowfallMapPaths,
                temperatureMapSummerPath,
                temperatureMapWinterPath,
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

            if (value._elevationMapPath is null)
            {
                writer.WriteNull("ElevationMapPath");
            }
            else
            {
                writer.WriteString("ElevationMapPath", value._elevationMapPath);
            }

            if (value._precipitationMapPaths is null)
            {
                writer.WriteNull("PrecipitationMapPaths");
            }
            else
            {
                writer.WritePropertyName("PrecipitationMapPaths");
                JsonSerializer.Serialize(writer, value._precipitationMapPaths, options);
            }

            if (value._snowfallMapPaths is null)
            {
                writer.WriteNull("SnowfallMapPaths");
            }
            else
            {
                writer.WritePropertyName("SnowfallMapPaths");
                JsonSerializer.Serialize(writer, value._snowfallMapPaths, options);
            }

            if (value._temperatureMapSummerPath is null)
            {
                writer.WriteNull("TemperatureMapSummerPath");
            }
            else
            {
                writer.WriteString("TemperatureMapSummerPath", value._temperatureMapSummerPath);
            }

            if (value._temperatureMapWinterPath is null)
            {
                writer.WriteNull("TemperatureMapWinterPath");
            }
            else
            {
                writer.WriteString("TemperatureMapWinterPath", value._temperatureMapWinterPath);
            }

            writer.WriteEndObject();
        }
    }
}
