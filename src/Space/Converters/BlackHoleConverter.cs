using System.Text.Json;
using System.Text.Json.Serialization;
using Tavenem.DataStorage;
using Tavenem.Universe.Place;

namespace Tavenem.Universe.Space;

/// <summary>
/// Converts a <see cref="BlackHole"/> to or from JSON.
/// </summary>
public class BlackHoleConverter : JsonConverter<BlackHole>
{
    /// <summary>Reads and converts the JSON to <see cref="BlackHole"/>.</summary>
    /// <param name="reader">The reader.</param>
    /// <param name="typeToConvert">The type to convert.</param>
    /// <param name="options">An object that specifies serialization options to use.</param>
    /// <returns>The converted value.</returns>
    public override BlackHole? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
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
            || !string.Equals(idItemTypeName, BlackHole.BlackHoleIdItemTypeName))
        {
            throw new JsonException();
        }

        if (!reader.Read()
            || reader.TokenType != JsonTokenType.PropertyName
            || !reader.ValueTextEquals("seed"))
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
        Vector3<HugeNumber>[]? absolutePosition;
        if (reader.TokenType == JsonTokenType.Null)
        {
            absolutePosition = null;
        }
        else
        {
            var vectorArrayConverter = (JsonConverter<Vector3<HugeNumber>[]>)options.GetConverter(typeof(Vector3<HugeNumber>[]));
            absolutePosition = vectorArrayConverter.Read(ref reader, typeof(Vector3<HugeNumber>[]), options);
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
        var velocity = JsonSerializer.Deserialize<Vector3<HugeNumber>>(ref reader, options);
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
        var position = JsonSerializer.Deserialize<Vector3<HugeNumber>>(ref reader, options);
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
            "Supermassive",
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
        var supermassive = reader.GetBoolean();

        while (reader.TokenType != JsonTokenType.EndObject)
        {
            reader.Read();
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

    /// <summary>Writes a <see cref="BlackHole"/> as JSON.</summary>
    /// <param name="writer">The writer to write to.</param>
    /// <param name="value">The value to convert to JSON.</param>
    /// <param name="options">An object that specifies serialization options to use.</param>
    public override void Write(Utf8JsonWriter writer, BlackHole value, JsonSerializerOptions options)
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

        writer.WriteBoolean(
            options.PropertyNamingPolicy is null
                ? "Supermassive"
                : options.PropertyNamingPolicy.ConvertName("Supermassive"),
            value._supermassive);

        writer.WriteEndObject();
    }
}
