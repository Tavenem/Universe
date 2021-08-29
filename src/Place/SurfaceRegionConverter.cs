using System.Text.Json;
using System.Text.Json.Serialization;
using Tavenem.DataStorage;

namespace Tavenem.Universe.Place;

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
    public override SurfaceRegion? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
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
            || !string.Equals(idItemTypeName, SurfaceRegion.SurfaceRegionIdItemTypeName))
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
            nameof(Location.Shape),
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
        var shape = JsonSerializer.Deserialize<IShape<HugeNumber>>(ref reader, options);
        if (shape is null)
        {
            throw new JsonException();
        }
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

        while (reader.TokenType != JsonTokenType.EndObject)
        {
            reader.Read();
        }

        return new SurfaceRegion(
            id,
            idItemTypeName,
            shape,
            parentId,
            absolutePosition);
    }

    /// <summary>Writes a <see cref="SurfaceRegion"/> as JSON.</summary>
    /// <param name="writer">The writer to write to.</param>
    /// <param name="value">The value to convert to JSON.</param>
    /// <param name="options">An object that specifies serialization options to use.</param>
    public override void Write(Utf8JsonWriter writer, SurfaceRegion value, JsonSerializerOptions options)
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

        writer.WritePropertyName(options.PropertyNamingPolicy is null
            ? nameof(Location.Shape)
            : options.PropertyNamingPolicy.ConvertName(nameof(Location.Shape)));
        JsonSerializer.Serialize(writer, value.Shape, value.Shape.GetType(), options);

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

        writer.WriteEndObject();
    }
}
