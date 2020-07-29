﻿using NeverFoundry.DataStorage;
using NeverFoundry.MathAndScience.Chemistry;
using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.WorldFoundry.Place;
using NeverFoundry.WorldFoundry.Space.Stars;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NeverFoundry.WorldFoundry.Space
{
    /// <summary>
    /// Converts a <see cref="StarSystem"/> to or from JSON.
    /// </summary>
    public class StarSystemConverter : JsonConverter<StarSystem>
    {
        /// <summary>Reads and converts the JSON to <see cref="StarSystem"/>.</summary>
        /// <param name="reader">The reader.</param>
        /// <param name="typeToConvert">The type to convert.</param>
        /// <param name="options">An object that specifies serialization options to use.</param>
        /// <returns>The converted value.</returns>
        [return: MaybeNull]
        public override StarSystem Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
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
                || !reader.ValueTextEquals(nameof(IIdItem.IdItemTypeName))
                || !reader.Read()
                || reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException();
            }
            var idItemTypeName = reader.GetString();
            if (string.IsNullOrEmpty(idItemTypeName)
                || !string.Equals(idItemTypeName, StarSystem.StarSystemIdItemTypeName))
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
            _ = (CosmicStructureType)structureTypeInt;

            if (!reader.Read()
                || reader.TokenType != JsonTokenType.PropertyName
                || !reader.ValueTextEquals(nameof(Star.StarType))
                || !reader.Read()
                || !reader.TryGetInt32(out var starTypeInt)
                || !Enum.IsDefined(typeof(StarType), starTypeInt))
            {
                throw new JsonException();
            }
            var starType = (StarType)starTypeInt;

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
                || !reader.ValueTextEquals("Radius")
                || !reader.Read()
                || reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException();
            }
            var radius = JsonSerializer.Deserialize<Number>(ref reader, options);

            if (!reader.Read()
                || reader.TokenType != JsonTokenType.PropertyName
                || !reader.ValueTextEquals("Mass")
                || !reader.Read()
                || reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException();
            }
            var mass = JsonSerializer.Deserialize<Number>(ref reader, options);

            if (!reader.Read()
                || reader.TokenType != JsonTokenType.PropertyName
                || !reader.ValueTextEquals(nameof(StarSystem.StarIDs))
                || !reader.Read()
                || reader.TokenType != JsonTokenType.StartArray)
            {
                throw new JsonException();
            }
            var starIDs = JsonSerializer.Deserialize<IReadOnlyList<string>>(ref reader, options);
            if (starIDs is null
                || reader.TokenType != JsonTokenType.EndArray)
            {
                throw new JsonException();
            }

            if (!reader.Read()
                || reader.TokenType != JsonTokenType.PropertyName
                || !reader.ValueTextEquals(nameof(Star.IsPopulationII))
                || !reader.Read())
            {
                throw new JsonException();
            }
            var isPopulationII = reader.GetBoolean();

            if (!reader.Read()
                || reader.TokenType != JsonTokenType.PropertyName
                || !reader.ValueTextEquals(nameof(Star.LuminosityClass))
                || !reader.Read()
                || !reader.TryGetInt32(out var luminosityClassInt)
                || !Enum.IsDefined(typeof(LuminosityClass), luminosityClassInt))
            {
                throw new JsonException();
            }
            var luminosityClass = (LuminosityClass)luminosityClassInt;

            if (!reader.Read()
                || reader.TokenType != JsonTokenType.PropertyName
                || !reader.ValueTextEquals(nameof(Star.SpectralClass))
                || !reader.Read()
                || !reader.TryGetInt32(out var spectralClassInt)
                || !Enum.IsDefined(typeof(SpectralClass), spectralClassInt))
            {
                throw new JsonException();
            }
            var spectralClass = (SpectralClass)spectralClassInt;

            if (!reader.Read()
                || reader.TokenType != JsonTokenType.EndObject)
            {
                throw new JsonException();
            }

            return new StarSystem(
                id,
                seed,
                starType,
                parentId,
                absolutePosition,
                name,
                velocity,
                orbit,
                position,
                temperature,
                radius,
                mass,
                starIDs,
                isPopulationII,
                luminosityClass,
                spectralClass);
        }

        /// <summary>Writes a <see cref="StarSystem"/> as JSON.</summary>
        /// <param name="writer">The writer to write to.</param>
        /// <param name="value">The value to convert to JSON.</param>
        /// <param name="options">An object that specifies serialization options to use.</param>
        public override void Write(Utf8JsonWriter writer, StarSystem value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            writer.WriteString(nameof(IIdItem.Id), value.Id);
            writer.WriteString(nameof(IIdItem.IdItemTypeName), value.IdItemTypeName);

            writer.WriteNumber("seed", value._seed);

            writer.WriteNumber(nameof(CosmicLocation.StructureType), (int)value.StructureType);

            writer.WriteNumber(nameof(StarSystem.StarType), (int)value.StarType);

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
                writer.WriteNumber(nameof(IMaterial.Temperature), value.Material.Temperature.Value);
            }
            else
            {
                writer.WriteNull(nameof(IMaterial.Temperature));
            }

            writer.WritePropertyName("Radius");
            JsonSerializer.Serialize(writer, value.Shape.ContainingRadius, options);

            writer.WritePropertyName("Mass");
            JsonSerializer.Serialize(writer, value.Mass, options);

            writer.WritePropertyName(nameof(StarSystem.StarIDs));
            JsonSerializer.Serialize(writer, value.StarIDs, options);

            writer.WriteBoolean(nameof(StarSystem.IsPopulationII), value.IsPopulationII);

            writer.WriteNumber(nameof(StarSystem.LuminosityClass), (int)value.LuminosityClass);

            writer.WriteNumber(nameof(StarSystem.SpectralClass), (int)value.SpectralClass);

            writer.WriteEndObject();
        }
    }
}
