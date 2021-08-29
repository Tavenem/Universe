using System.Text.Json;
using System.Text.Json.Serialization;
using Tavenem.Chemistry;
using Tavenem.DataStorage;
using Tavenem.Universe.Place;

namespace Tavenem.Universe.Space;

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
    public override CosmicLocation? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var initialReader = reader;

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
        if (string.IsNullOrEmpty(idItemTypeName))
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
        if (string.Equals(
            prop,
            nameof(Star.StarType),
            options.PropertyNameCaseInsensitive
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal))
        {
            var result = new StarConverter().Read(ref initialReader, typeToConvert, options);
            reader = initialReader;
            return result;
        }
        if (string.Equals(
            prop,
            nameof(Planetoid.PlanetType),
            options.PropertyNameCaseInsensitive
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal))
        {
            var result = new PlanetoidConverter().Read(ref initialReader, typeToConvert, options);
            reader = initialReader;
            return result;
        }
        if (!string.Equals(
            prop,
            nameof(CosmicLocation.StructureType),
            options.PropertyNameCaseInsensitive
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal)
            || !reader.Read()
            || !reader.TryGetInt32(out var structureTypeInt)
            || !Enum.IsDefined(typeof(CosmicStructureType), structureTypeInt))
        {
            throw new JsonException();
        }
        var structureType = (CosmicStructureType)structureTypeInt;
        if (structureType is CosmicStructureType.AsteroidField
            or CosmicStructureType.OortCloud)
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

        if (!string.Equals(idItemTypeName, CosmicLocation.CosmicLocationIdItemTypeName))
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

        while (reader.TokenType != JsonTokenType.EndObject)
        {
            reader.Read();
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

        if (value.Material.Temperature.HasValue)
        {
            writer.WriteNumber(
                options.PropertyNamingPolicy is null
                    ? nameof(IMaterial<HugeNumber>.Temperature)
                    : options.PropertyNamingPolicy.ConvertName(nameof(IMaterial<HugeNumber>.Temperature)),
                value.Material.Temperature.Value);
        }
        else
        {
            writer.WriteNull(options.PropertyNamingPolicy is null
                ? nameof(IMaterial<HugeNumber>.Temperature)
                : options.PropertyNamingPolicy.ConvertName(nameof(IMaterial<HugeNumber>.Temperature)));
        }

        writer.WriteEndObject();
    }
}
