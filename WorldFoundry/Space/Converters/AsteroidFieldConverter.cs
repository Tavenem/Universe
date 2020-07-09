using NeverFoundry.DataStorage;
using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.WorldFoundry.Place;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NeverFoundry.WorldFoundry.Space
{
    /// <summary>
    /// Converts a <see cref="AsteroidField"/> to or from JSON.
    /// </summary>
    public class AsteroidFieldConverter : JsonConverter<AsteroidField>
    {
        /// <summary>Reads and converts the JSON to <see cref="AsteroidField"/>.</summary>
        /// <param name="reader">The reader.</param>
        /// <param name="typeToConvert">The type to convert.</param>
        /// <param name="options">An object that specifies serialization options to use.</param>
        /// <returns>The converted value.</returns>
        [return: MaybeNull]
        public override AsteroidField Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject
                || !reader.Read()
                || reader.TokenType != JsonTokenType.PropertyName
                || !reader.ValueTextEquals(nameof(IIdItem.Id))
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
                || !reader.ValueTextEquals("seed")
                || !reader.Read()
                || !reader.TryGetUInt32(out var seed))
            {
                throw new JsonException();
            }

            if (!reader.Read()
                || reader.TokenType != JsonTokenType.PropertyName
                || !reader.ValueTextEquals(nameof(CosmicLocation.StructureType))
                || !reader.Read()
                || !reader.TryGetInt32(out var structureTypeInt)
                || !Enum.IsDefined(typeof(CosmicStructureType), structureTypeInt))
            {
                throw new JsonException();
            }
            var structureType = (CosmicStructureType)structureTypeInt;

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
                || !reader.ValueTextEquals(nameof(CosmicLocation.Name))
                || !reader.Read())
            {
                throw new JsonException();
            }
            var name = reader.GetString();

            if (!reader.Read()
                || reader.TokenType != JsonTokenType.PropertyName
                || !reader.ValueTextEquals(nameof(CosmicLocation.Velocity))
                || !reader.Read()
                || reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException();
            }
            var velocity = JsonSerializer.Deserialize<Vector3>(ref reader, options);
            if (reader.TokenType != JsonTokenType.EndObject)
            {
                throw new JsonException();
            }

            if (!reader.Read()
                || reader.TokenType != JsonTokenType.PropertyName
                || !reader.ValueTextEquals(nameof(CosmicLocation.Orbit))
                || !reader.Read())
            {
                throw new JsonException();
            }
            Orbit? orbit;
            if (reader.TokenType == JsonTokenType.Null)
            {
                orbit = null;
            }
            else
            {
                if (reader.TokenType != JsonTokenType.StartObject)
                {
                    throw new JsonException();
                }
                else
                {
                    orbit = JsonSerializer.Deserialize<Orbit>(ref reader, options);
                }
                if (reader.TokenType != JsonTokenType.EndObject)
                {
                    throw new JsonException();
                }
            }

            if (!reader.Read()
                || reader.TokenType != JsonTokenType.PropertyName
                || !reader.ValueTextEquals(nameof(Location.Position))
                || !reader.Read()
                || reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException();
            }
            var position = JsonSerializer.Deserialize<Vector3>(ref reader, options);
            if (reader.TokenType != JsonTokenType.EndObject)
            {
                throw new JsonException();
            }

            if (!reader.Read()
                || reader.TokenType != JsonTokenType.PropertyName
                || !reader.ValueTextEquals(nameof(CosmicLocation.Temperature))
                || !reader.Read())
            {
                throw new JsonException();
            }
            double? temperature;
            if (reader.TokenType == JsonTokenType.Null)
            {
                temperature = null;
            }
            else if (reader.TokenType != JsonTokenType.Number)
            {
                throw new JsonException();
            }
            else
            {
                temperature = reader.GetDouble();
            }

            if (!reader.Read()
                || reader.TokenType != JsonTokenType.PropertyName
                || !reader.ValueTextEquals(nameof(AsteroidField.MajorRadius))
                || !reader.Read()
                || reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException();
            }
            var majorRadius = JsonSerializer.Deserialize<Number>(ref reader, options);

            if (!reader.Read()
                || reader.TokenType != JsonTokenType.PropertyName
                || !reader.ValueTextEquals(nameof(AsteroidField.MinorRadius))
                || !reader.Read()
                || reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException();
            }
            var minorRadius = JsonSerializer.Deserialize<Number>(ref reader, options);

            if (!reader.Read()
                || reader.TokenType != JsonTokenType.PropertyName
                || !reader.ValueTextEquals("Toroidal")
                || !reader.Read())
            {
                throw new JsonException();
            }
            var toroidal = reader.GetBoolean();

            if (!reader.Read()
                || reader.TokenType != JsonTokenType.PropertyName
                || !reader.ValueTextEquals("ChildOrbitalParameters")
                || !reader.Read())
            {
                throw new JsonException();
            }
            OrbitalParameters? childOrbitalParameters;
            if (reader.TokenType == JsonTokenType.Null)
            {
                childOrbitalParameters = null;
            }
            else
            {
                if (reader.TokenType != JsonTokenType.StartObject)
                {
                    throw new JsonException();
                }
                else
                {
                    childOrbitalParameters = JsonSerializer.Deserialize<OrbitalParameters>(ref reader, options);
                }
                if (reader.TokenType != JsonTokenType.EndObject)
                {
                    throw new JsonException();
                }
            }

            if (!reader.Read()
                || reader.TokenType != JsonTokenType.EndObject)
            {
                throw new JsonException();
            }

            return new AsteroidField(
                id,
                seed,
                structureType,
                parentId,
                absolutePosition,
                name,
                velocity,
                orbit,
                position,
                temperature,
                majorRadius,
                minorRadius,
                toroidal,
                childOrbitalParameters);
        }

        /// <summary>Writes a <see cref="AsteroidField"/> as JSON.</summary>
        /// <param name="writer">The writer to write to.</param>
        /// <param name="value">The value to convert to JSON.</param>
        /// <param name="options">An object that specifies serialization options to use.</param>
        public override void Write(Utf8JsonWriter writer, AsteroidField value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            writer.WriteString(nameof(IIdItem.Id), value.Id);

            writer.WriteNumber("seed", value._seed);

            writer.WriteNumber(nameof(CosmicLocation.StructureType), (int)value.StructureType);

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

            if (value.Name is null)
            {
                writer.WriteNull(nameof(CosmicLocation.Name));
            }
            else
            {
                writer.WriteString(nameof(CosmicLocation.Name), value.Name);
            }

            writer.WritePropertyName(nameof(CosmicLocation.Velocity));
            JsonSerializer.Serialize(writer, value.Velocity, options);

            if (value.Orbit.HasValue)
            {
                writer.WritePropertyName(nameof(CosmicLocation.Orbit));
                JsonSerializer.Serialize(writer, value.Orbit.Value, options);
            }
            else
            {
                writer.WriteNull(nameof(CosmicLocation.Orbit));
            }

            writer.WritePropertyName(nameof(Location.Position));
            JsonSerializer.Serialize(writer, value.Position, options);

            if (value.Material.Temperature.HasValue)
            {
                writer.WriteNumber(nameof(MathAndScience.Chemistry.IMaterial.Temperature), value.Material.Temperature.Value);
            }
            else
            {
                writer.WriteNull(nameof(MathAndScience.Chemistry.IMaterial.Temperature));
            }

            writer.WritePropertyName(nameof(AsteroidField.MajorRadius));
            JsonSerializer.Serialize(writer, value.MajorRadius, options);

            writer.WritePropertyName(nameof(AsteroidField.MinorRadius));
            JsonSerializer.Serialize(writer, value.MinorRadius, options);

            writer.WriteBoolean("Toroidal", value._toroidal);

            if (value._childOrbitalParameters.HasValue)
            {
                writer.WritePropertyName("ChildOrbitalParameters");
                JsonSerializer.Serialize(writer, value._childOrbitalParameters.Value, options);
            }
            else
            {
                writer.WriteNull("ChildOrbitalParameters");
            }

            writer.WriteEndObject();
        }
    }
}
