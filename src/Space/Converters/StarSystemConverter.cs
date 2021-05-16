using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Tavenem.DataStorage;
using Tavenem.HugeNumbers;
using Tavenem.Mathematics.HugeNumbers;
using Tavenem.Universe.Place;
using Tavenem.Universe.Space.Stars;

namespace Tavenem.Universe.Space
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
        public override StarSystem? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject
                || !reader.Read()
                || reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException();
            }
            var prop = reader.GetString();
            if (!string.Equals(
                prop,
                "id",
                options.PropertyNameCaseInsensitive
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal))
            {
                throw new JsonException();
            }
            if (!reader.Read()
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
                || reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException();
            }
            prop = reader.GetString();
            if (!string.Equals(
                prop,
                nameof(IIdItem.IdItemTypeName),
                options.PropertyNameCaseInsensitive
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal))
            {
                throw new JsonException();
            }
            if (!reader.Read()
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
                || reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException();
            }
            prop = reader.GetString();
            if (!string.Equals(
                prop,
                "seed",
                options.PropertyNameCaseInsensitive
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal))
            {
                throw new JsonException();
            }
            if (!reader.Read()
                || !reader.TryGetUInt32(out var seed))
            {
                throw new JsonException();
            }

            if (!reader.Read()
                || reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException();
            }
            prop = reader.GetString();
            if (!string.Equals(
                prop,
                nameof(CosmicLocation.StructureType),
                options.PropertyNameCaseInsensitive
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal))
            {
                throw new JsonException();
            }
            if (!reader.Read()
                || !reader.TryGetInt32(out var structureTypeInt)
                || !Enum.IsDefined(typeof(CosmicStructureType), structureTypeInt))
            {
                throw new JsonException();
            }
            _ = (CosmicStructureType)structureTypeInt;

            if (!reader.Read()
                || reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException();
            }
            prop = reader.GetString();
            if (!string.Equals(
                prop,
                nameof(Star.StarType),
                options.PropertyNameCaseInsensitive
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal))
            {
                throw new JsonException();
            }
            if (!reader.Read()
                || !reader.TryGetInt32(out var starTypeInt)
                || !Enum.IsDefined(typeof(StarType), starTypeInt))
            {
                throw new JsonException();
            }
            var starType = (StarType)starTypeInt;

            if (!reader.Read()
                || reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException();
            }
            prop = reader.GetString();
            if (!string.Equals(
                prop,
                nameof(Location.ParentId),
                options.PropertyNameCaseInsensitive
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal))
            {
                throw new JsonException();
            }
            if (!reader.Read())
            {
                throw new JsonException();
            }
            var parentId = reader.GetString();

            if (!reader.Read()
                || reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException();
            }
            prop = reader.GetString();
            if (!string.Equals(
                prop,
                nameof(Location.AbsolutePosition),
                options.PropertyNameCaseInsensitive
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal))
            {
                throw new JsonException();
            }
            if (!reader.Read())
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
                || reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException();
            }
            prop = reader.GetString();
            if (!string.Equals(
                prop,
                nameof(CosmicLocation.Name),
                options.PropertyNameCaseInsensitive
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal))
            {
                throw new JsonException();
            }
            if (!reader.Read())
            {
                throw new JsonException();
            }
            var name = reader.GetString();

            if (!reader.Read()
                || reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException();
            }
            prop = reader.GetString();
            if (!string.Equals(
                prop,
                nameof(CosmicLocation.Velocity),
                options.PropertyNameCaseInsensitive
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal))
            {
                throw new JsonException();
            }
            if (!reader.Read()
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
                || reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException();
            }
            prop = reader.GetString();
            if (!string.Equals(
                prop,
                nameof(CosmicLocation.Orbit),
                options.PropertyNameCaseInsensitive
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal))
            {
                throw new JsonException();
            }
            if (!reader.Read())
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
                || reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException();
            }
            prop = reader.GetString();
            if (!string.Equals(
                prop,
                nameof(Location.Position),
                options.PropertyNameCaseInsensitive
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal))
            {
                throw new JsonException();
            }
            if (!reader.Read()
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
                || reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException();
            }
            prop = reader.GetString();
            if (!string.Equals(
                prop,
                nameof(CosmicLocation.Temperature),
                options.PropertyNameCaseInsensitive
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal))
            {
                throw new JsonException();
            }
            if (!reader.Read())
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
                || reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException();
            }
            prop = reader.GetString();
            if (!string.Equals(
                prop,
                "Radius",
                options.PropertyNameCaseInsensitive
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal))
            {
                throw new JsonException();
            }
            if (!reader.Read()
                || reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException();
            }
            var radius = JsonSerializer.Deserialize<HugeNumber>(ref reader, options);

            if (!reader.Read()
                || reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException();
            }
            prop = reader.GetString();
            if (!string.Equals(
                prop,
                "Mass",
                options.PropertyNameCaseInsensitive
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal))
            {
                throw new JsonException();
            }
            if (!reader.Read()
                || reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException();
            }
            var mass = JsonSerializer.Deserialize<HugeNumber>(ref reader, options);

            if (!reader.Read()
                || reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException();
            }
            prop = reader.GetString();
            if (!string.Equals(
                prop,
                nameof(StarSystem.StarIDs),
                options.PropertyNameCaseInsensitive
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal))
            {
                throw new JsonException();
            }
            if (!reader.Read()
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
                || reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException();
            }
            prop = reader.GetString();
            if (!string.Equals(
                prop,
                nameof(Star.IsPopulationII),
                options.PropertyNameCaseInsensitive
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal))
            {
                throw new JsonException();
            }
            if (!reader.Read())
            {
                throw new JsonException();
            }
            var isPopulationII = reader.GetBoolean();

            if (!reader.Read()
                || reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException();
            }
            prop = reader.GetString();
            if (!string.Equals(
                prop,
                nameof(Star.LuminosityClass),
                options.PropertyNameCaseInsensitive
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal))
            {
                throw new JsonException();
            }
            if (!reader.Read()
                || !reader.TryGetInt32(out var luminosityClassInt)
                || !Enum.IsDefined(typeof(LuminosityClass), luminosityClassInt))
            {
                throw new JsonException();
            }
            var luminosityClass = (LuminosityClass)luminosityClassInt;

            if (!reader.Read()
                || reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException();
            }
            prop = reader.GetString();
            if (!string.Equals(
                prop,
                nameof(Star.SpectralClass),
                options.PropertyNameCaseInsensitive
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal))
            {
                throw new JsonException();
            }
            if (!reader.Read()
                || !reader.TryGetInt32(out var spectralClassInt)
                || !Enum.IsDefined(typeof(SpectralClass), spectralClassInt))
            {
                throw new JsonException();
            }
            var spectralClass = (SpectralClass)spectralClassInt;

            while (reader.TokenType != JsonTokenType.EndObject)
            {
                reader.Read();
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

            writer.WriteString(
                options.PropertyNamingPolicy is null
                    ? "id"
                    : options.PropertyNamingPolicy.ConvertName("id"),
                value.Id);
            writer.WriteString(
                options.PropertyNamingPolicy is null
                    ? nameof(IIdItem.IdItemTypeName)
                    : options.PropertyNamingPolicy.ConvertName(nameof(IIdItem.IdItemTypeName)),
                value.IdItemTypeName);

            writer.WriteNumber(
                options.PropertyNamingPolicy is null
                    ? "seed"
                    : options.PropertyNamingPolicy.ConvertName("seed"),
                value.Seed);

            writer.WriteNumber(
                options.PropertyNamingPolicy is null
                    ? nameof(CosmicLocation.StructureType)
                    : options.PropertyNamingPolicy.ConvertName(nameof(CosmicLocation.StructureType)),
                (int)value.StructureType);

            writer.WriteNumber(
                options.PropertyNamingPolicy is null
                    ? nameof(StarSystem.StarType)
                    : options.PropertyNamingPolicy.ConvertName(nameof(StarSystem.StarType)),
                (int)value.StarType);

            if (value.ParentId is null)
            {
                writer.WriteNull(options.PropertyNamingPolicy is null
                    ? nameof(Location.ParentId)
                    : options.PropertyNamingPolicy.ConvertName(nameof(Location.ParentId)));
            }
            else
            {
                writer.WriteString(
                    options.PropertyNamingPolicy is null
                        ? nameof(Location.ParentId)
                        : options.PropertyNamingPolicy.ConvertName(nameof(Location.ParentId)),
                    value.ParentId);
            }

            if (value.AbsolutePosition is null)
            {
                writer.WriteNull(options.PropertyNamingPolicy is null
                    ? nameof(Location.AbsolutePosition)
                    : options.PropertyNamingPolicy.ConvertName(nameof(Location.AbsolutePosition)));
            }
            else
            {
                writer.WritePropertyName(options.PropertyNamingPolicy is null
                    ? nameof(Location.AbsolutePosition)
                    : options.PropertyNamingPolicy.ConvertName(nameof(Location.AbsolutePosition)));
                JsonSerializer.Serialize(writer, value.AbsolutePosition, options);
            }

            if (value.Name is null)
            {
                writer.WriteNull(options.PropertyNamingPolicy is null
                    ? nameof(CosmicLocation.Name)
                    : options.PropertyNamingPolicy.ConvertName(nameof(CosmicLocation.Name)));
            }
            else
            {
                writer.WriteString(
                    options.PropertyNamingPolicy is null
                        ? nameof(CosmicLocation.Name)
                        : options.PropertyNamingPolicy.ConvertName(nameof(CosmicLocation.Name)),
                    value.Name);
            }

            writer.WritePropertyName(options.PropertyNamingPolicy is null
                ? nameof(CosmicLocation.Velocity)
                : options.PropertyNamingPolicy.ConvertName(nameof(CosmicLocation.Velocity)));
            JsonSerializer.Serialize(writer, value.Velocity, options);

            if (value.Orbit.HasValue)
            {
                writer.WritePropertyName(options.PropertyNamingPolicy is null
                    ? nameof(CosmicLocation.Orbit)
                    : options.PropertyNamingPolicy.ConvertName(nameof(CosmicLocation.Orbit)));
                JsonSerializer.Serialize(writer, value.Orbit.Value, options);
            }
            else
            {
                writer.WriteNull(options.PropertyNamingPolicy is null
                    ? nameof(CosmicLocation.Orbit)
                    : options.PropertyNamingPolicy.ConvertName(nameof(CosmicLocation.Orbit)));
            }

            writer.WritePropertyName(options.PropertyNamingPolicy is null
                ? nameof(Location.Position)
                : options.PropertyNamingPolicy.ConvertName(nameof(Location.Position)));
            JsonSerializer.Serialize(writer, value.Position, options);

            if (value.Material.Temperature.HasValue)
            {
                writer.WriteNumber(
                    options.PropertyNamingPolicy is null
                        ? nameof(IMaterial.Temperature)
                        : options.PropertyNamingPolicy.ConvertName(nameof(IMaterial.Temperature)),
                    value.Material.Temperature.Value);
            }
            else
            {
                writer.WriteNull(options.PropertyNamingPolicy is null
                    ? nameof(IMaterial.Temperature)
                    : options.PropertyNamingPolicy.ConvertName(nameof(IMaterial.Temperature)));
            }

            writer.WritePropertyName(options.PropertyNamingPolicy is null
                ? "Radius"
                : options.PropertyNamingPolicy.ConvertName("Radius"));
            JsonSerializer.Serialize(writer, value.Shape.ContainingRadius, options);

            writer.WritePropertyName(options.PropertyNamingPolicy is null
                ? "Mass"
                : options.PropertyNamingPolicy.ConvertName("Mass"));
            JsonSerializer.Serialize(writer, value.Mass, options);

            writer.WritePropertyName(options.PropertyNamingPolicy is null
                ? nameof(StarSystem.StarIDs)
                : options.PropertyNamingPolicy.ConvertName(nameof(StarSystem.StarIDs)));
            JsonSerializer.Serialize(writer, value.StarIDs, options);

            writer.WriteBoolean(
                options.PropertyNamingPolicy is null
                    ? nameof(StarSystem.IsPopulationII)
                    : options.PropertyNamingPolicy.ConvertName(nameof(StarSystem.IsPopulationII)),
                value.IsPopulationII);

            writer.WriteNumber(
                options.PropertyNamingPolicy is null
                    ? nameof(StarSystem.LuminosityClass)
                    : options.PropertyNamingPolicy.ConvertName(nameof(StarSystem.LuminosityClass)),
                (int)value.LuminosityClass);

            writer.WriteNumber(
                options.PropertyNamingPolicy is null
                    ? nameof(StarSystem.SpectralClass)
                    : options.PropertyNamingPolicy.ConvertName(nameof(StarSystem.SpectralClass)),
                (int)value.SpectralClass);

            writer.WriteEndObject();
        }
    }
}
