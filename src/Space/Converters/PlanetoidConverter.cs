using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Tavenem.DataStorage;
using Tavenem.HugeNumbers;
using Tavenem.Mathematics.HugeNumbers;
using Tavenem.Universe.Place;
using Tavenem.Universe.Space.Planetoids;

namespace Tavenem.Universe.Space
{
    /// <summary>
    /// Converts a <see cref="Planetoid"/> to or from JSON.
    /// </summary>
    public class PlanetoidConverter : JsonConverter<Planetoid>
    {
        /// <summary>Reads and converts the JSON to <see cref="Planetoid"/>.</summary>
        /// <param name="reader">The reader.</param>
        /// <param name="typeToConvert">The type to convert.</param>
        /// <param name="options">An object that specifies serialization options to use.</param>
        /// <returns>The converted value.</returns>
        public override Planetoid? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
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
                || !string.Equals(idItemTypeName, Planetoid.PlanetoidIdItemTypeName))
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
                nameof(Planetoid.PlanetType),
                options.PropertyNameCaseInsensitive
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal))
            {
                throw new JsonException();
            }
            if (!reader.Read()
                || !reader.TryGetInt32(out var planetTypeInt)
                || !Enum.IsDefined(typeof(PlanetType), planetTypeInt))
            {
                throw new JsonException();
            }
            var planetType = (PlanetType)planetTypeInt;

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
                nameof(Planetoid.AngleOfRotation),
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
            var angleOfRotation = reader.GetDouble();

            if (!reader.Read()
                || reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException();
            }
            prop = reader.GetString();
            if (!string.Equals(
                prop,
                nameof(Planetoid.RotationalPeriod),
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
            var rotationalPeriod = JsonSerializer.Deserialize<HugeNumber>(ref reader, options);

            if (!reader.Read()
                || reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException();
            }
            prop = reader.GetString();
            if (!string.Equals(
                prop,
                "SatelliteIDs",
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
            List<string>? satelliteIDs;
            if (reader.TokenType == JsonTokenType.Null)
            {
                satelliteIDs = null;
            }
            else if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw new JsonException();
            }
            else
            {
                satelliteIDs = JsonSerializer.Deserialize<List<string>>(ref reader, options);
                if (satelliteIDs is null
                    || reader.TokenType != JsonTokenType.EndArray)
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
                nameof(Planetoid.Rings),
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
            List<PlanetaryRing>? rings;
            if (reader.TokenType == JsonTokenType.Null)
            {
                rings = null;
            }
            else if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw new JsonException();
            }
            else
            {
                rings = JsonSerializer.Deserialize<List<PlanetaryRing>>(ref reader, options);
                if (rings is null
                    || reader.TokenType != JsonTokenType.EndArray)
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
                "BlackbodyTemperature",
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
            var blackbodyTemperature = reader.GetDouble();

            if (!reader.Read()
                || reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException();
            }
            prop = reader.GetString();
            if (!string.Equals(
                prop,
                "SurfaceTemperatureAtApoapsis",
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
            var surfaceTemperatureAtApoapsis = reader.GetDouble();

            if (!reader.Read()
                || reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException();
            }
            prop = reader.GetString();
            if (!string.Equals(
                prop,
                "SurfaceTemperatureAtPeriapsis",
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
            var surfaceTemperatureAtPeriapsis = reader.GetDouble();

            if (!reader.Read()
                || reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException();
            }
            prop = reader.GetString();
            if (!string.Equals(
                prop,
                nameof(Planetoid.IsInhospitable),
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
            var isInhospitable = reader.GetBoolean();

            if (!reader.Read()
                || reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException();
            }
            prop = reader.GetString();
            if (!string.Equals(
                prop,
                "Earthlike",
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
            var earthlike = reader.GetBoolean();

            if (!reader.Read()
                || reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException();
            }
            prop = reader.GetString();
            if (!string.Equals(
                prop,
                nameof(PlanetParams),
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
            PlanetParams? planetParams;
            if (reader.TokenType == JsonTokenType.Null)
            {
                planetParams = null;
            }
            else if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException();
            }
            else
            {
                planetParams = JsonSerializer.Deserialize<PlanetParams>(ref reader, options);
                if (planetParams is null
                    || reader.TokenType != JsonTokenType.EndObject)
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
                nameof(HabitabilityRequirements),
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
            HabitabilityRequirements? habitabilityRequirements;
            if (reader.TokenType == JsonTokenType.Null)
            {
                habitabilityRequirements = null;
            }
            else if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException();
            }
            else
            {
                habitabilityRequirements = JsonSerializer.Deserialize<HabitabilityRequirements>(ref reader, options);
                if (habitabilityRequirements is null
                    || reader.TokenType != JsonTokenType.EndObject)
                {
                    throw new JsonException();
                }
            }

            while (reader.TokenType != JsonTokenType.EndObject)
            {
                reader.Read();
            }

            return new Planetoid(
                id,
                seed,
                planetType,
                parentId,
                absolutePosition,
                name,
                velocity,
                orbit,
                position,
                temperature,
                angleOfRotation,
                rotationalPeriod,
                satelliteIDs,
                rings,
                blackbodyTemperature,
                surfaceTemperatureAtApoapsis,
                surfaceTemperatureAtPeriapsis,
                isInhospitable,
                earthlike,
                planetParams,
                habitabilityRequirements);
        }

        /// <summary>Writes a <see cref="Planetoid"/> as JSON.</summary>
        /// <param name="writer">The writer to write to.</param>
        /// <param name="value">The value to convert to JSON.</param>
        /// <param name="options">An object that specifies serialization options to use.</param>
        public override void Write(Utf8JsonWriter writer, Planetoid value, JsonSerializerOptions options)
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
                    ? nameof(Planetoid.PlanetType)
                    : options.PropertyNamingPolicy.ConvertName(nameof(Planetoid.PlanetType)),
                (int)value.PlanetType);

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
                        ? nameof(Tavenem.Chemistry.HugeNumbers.IMaterial.Temperature)
                        : options.PropertyNamingPolicy.ConvertName(nameof(Tavenem.Chemistry.HugeNumbers.IMaterial.Temperature)),
                    value.Material.Temperature.Value);
            }
            else
            {
                writer.WriteNull(options.PropertyNamingPolicy is null
                    ? nameof(Tavenem.Chemistry.HugeNumbers.IMaterial.Temperature)
                    : options.PropertyNamingPolicy.ConvertName(nameof(Tavenem.Chemistry.HugeNumbers.IMaterial.Temperature)));
            }

            writer.WriteNumber(
                options.PropertyNamingPolicy is null
                    ? nameof(Planetoid.AngleOfRotation)
                    : options.PropertyNamingPolicy.ConvertName(nameof(Planetoid.AngleOfRotation)),
                value.AngleOfRotation);

            writer.WritePropertyName(options.PropertyNamingPolicy is null
                ? nameof(Planetoid.RotationalPeriod)
                : options.PropertyNamingPolicy.ConvertName(nameof(Planetoid.RotationalPeriod)));
            JsonSerializer.Serialize(writer, value.RotationalPeriod, options);

            if (value._satelliteIDs is null)
            {
                writer.WriteNull(options.PropertyNamingPolicy is null
                    ? "SatelliteIDs"
                    : options.PropertyNamingPolicy.ConvertName("SatelliteIDs"));
            }
            else
            {
                writer.WritePropertyName(options.PropertyNamingPolicy is null
                    ? "SatelliteIDs"
                    : options.PropertyNamingPolicy.ConvertName("SatelliteIDs"));
                JsonSerializer.Serialize(writer, value._satelliteIDs, options);
            }

            if (value._rings is null)
            {
                writer.WriteNull(options.PropertyNamingPolicy is null
                    ? nameof(Planetoid.Rings)
                    : options.PropertyNamingPolicy.ConvertName(nameof(Planetoid.Rings)));
            }
            else
            {
                writer.WritePropertyName(options.PropertyNamingPolicy is null
                    ? nameof(Planetoid.Rings)
                    : options.PropertyNamingPolicy.ConvertName(nameof(Planetoid.Rings)));
                JsonSerializer.Serialize(writer, value._rings, options);
            }

            writer.WriteNumber(
                options.PropertyNamingPolicy is null
                    ? "BlackbodyTemperature"
                    : options.PropertyNamingPolicy.ConvertName("BlackbodyTemperature"),
                value._blackbodyTemperature);
            writer.WriteNumber(
                options.PropertyNamingPolicy is null
                    ? "SurfaceTemperatureAtApoapsis"
                    : options.PropertyNamingPolicy.ConvertName("SurfaceTemperatureAtApoapsis"),
                value._surfaceTemperatureAtApoapsis);
            writer.WriteNumber(
                options.PropertyNamingPolicy is null
                    ? "SurfaceTemperatureAtPeriapsis"
                    : options.PropertyNamingPolicy.ConvertName("SurfaceTemperatureAtPeriapsis"),
                value._surfaceTemperatureAtPeriapsis);
            writer.WriteBoolean(
                options.PropertyNamingPolicy is null
                    ? nameof(Planetoid.IsInhospitable)
                    : options.PropertyNamingPolicy.ConvertName(nameof(Planetoid.IsInhospitable)),
                value.IsInhospitable);
            writer.WriteBoolean(
                options.PropertyNamingPolicy is null
                    ? "Earthlike"
                    : options.PropertyNamingPolicy.ConvertName("Earthlike"),
                value._earthlike);

            if (value._earthlike || value._planetParams is null)
            {
                writer.WriteNull(options.PropertyNamingPolicy is null
                    ? nameof(PlanetParams)
                    : options.PropertyNamingPolicy.ConvertName(nameof(PlanetParams)));
            }
            else
            {
                writer.WritePropertyName(options.PropertyNamingPolicy is null
                    ? nameof(PlanetParams)
                    : options.PropertyNamingPolicy.ConvertName(nameof(PlanetParams)));
                JsonSerializer.Serialize(writer, value._planetParams, options);
            }

            if (value._earthlike || value._habitabilityRequirements is null)
            {
                writer.WriteNull(options.PropertyNamingPolicy is null
                    ? nameof(HabitabilityRequirements)
                    : options.PropertyNamingPolicy.ConvertName(nameof(HabitabilityRequirements)));
            }
            else
            {
                writer.WritePropertyName(options.PropertyNamingPolicy is null
                    ? nameof(HabitabilityRequirements)
                    : options.PropertyNamingPolicy.ConvertName(nameof(HabitabilityRequirements)));
                JsonSerializer.Serialize(writer, value._habitabilityRequirements, options);
            }

            writer.WriteEndObject();
        }
    }
}
