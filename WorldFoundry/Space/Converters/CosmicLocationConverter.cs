using NeverFoundry.DataStorage;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.WorldFoundry.Place;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NeverFoundry.WorldFoundry.Space
{
    /// <summary>
    /// Converts a <see cref="CosmicLocation"/> to or from JSON.
    /// </summary>
    public class CosmicLocationConverter : JsonConverter<CosmicLocation>
    {
        /// <summary>Reads and converts the JSON to <see cref="CosmicLocation"/>.</summary>
        /// <param name="reader">The reader.</param>
        /// <param name="typeToConvert">The type to convert.</param>
        /// <param name="options">An object that specifies serialization options to use.</param>
        /// <returns>The converted value.</returns>
        [return: MaybeNull]
        public override CosmicLocation Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var initialReader = reader;

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
                || reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException();
            }
            if (reader.ValueTextEquals(nameof(Star.StarType)))
            {
                var result = new StarConverter().Read(ref initialReader, typeToConvert, options);
                reader = initialReader;
                return result;
            }
            if (reader.ValueTextEquals(nameof(Planetoid.PlanetType)))
            {
                var result = new PlanetoidConverter().Read(ref initialReader, typeToConvert, options);
                reader = initialReader;
                return result;
            }
            if (!reader.ValueTextEquals(nameof(CosmicLocation.StructureType))
                || !reader.Read()
                || !reader.TryGetInt32(out var structureTypeInt)
                || !Enum.IsDefined(typeof(CosmicStructureType), structureTypeInt))
            {
                throw new JsonException();
            }
            var structureType = (CosmicStructureType)structureTypeInt;
            if (structureType == CosmicStructureType.AsteroidField
                || structureType == CosmicStructureType.OortCloud)
            {
                var result = new AsteroidFieldConverter().Read(ref initialReader, typeToConvert, options);
                reader = initialReader;
                return result;
            }
            if (structureType == CosmicStructureType.BlackHole)
            {
                var result = new BlackHoleConverter().Read(ref initialReader, typeToConvert, options);
                reader = initialReader;
                return result;
            }
            if (structureType == CosmicStructureType.StarSystem)
            {
                var result = new StarSystemConverter().Read(ref initialReader, typeToConvert, options);
                reader = initialReader;
                return result;
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
                || reader.TokenType != JsonTokenType.EndObject)
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

        /// <summary>Writes a <see cref="CosmicLocation"/> as JSON.</summary>
        /// <param name="writer">The writer to write to.</param>
        /// <param name="value">The value to convert to JSON.</param>
        /// <param name="options">An object that specifies serialization options to use.</param>
        public override void Write(Utf8JsonWriter writer, CosmicLocation value, JsonSerializerOptions options)
        {
            if (value is AsteroidField asteroidField)
            {
                new AsteroidFieldConverter().Write(writer, asteroidField, options);
                return;
            }
            if (value is BlackHole blackHole)
            {
                new BlackHoleConverter().Write(writer, blackHole, options);
                return;
            }
            if (value is Planetoid planetoid)
            {
                new PlanetoidConverter().Write(writer, planetoid, options);
                return;
            }
            if (value is Star star)
            {
                new StarConverter().Write(writer, star, options);
                return;
            }
            if (value is StarSystem starSystem)
            {
                new StarSystemConverter().Write(writer, starSystem, options);
                return;
            }

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

            writer.WriteEndObject();
        }
    }
}
