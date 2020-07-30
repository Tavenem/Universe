using NeverFoundry.DataStorage;
using NeverFoundry.MathAndScience.Chemistry;
using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.WorldFoundry.Place;
using NeverFoundry.WorldFoundry.Space.Planetoids;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NeverFoundry.WorldFoundry.Space
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
        [return: MaybeNull]
        public override Planetoid Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
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
                || !string.Equals(idItemTypeName, Planetoid.PlanetoidIdItemTypeName))
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
                || !reader.ValueTextEquals(nameof(Planetoid.PlanetType))
                || !reader.Read()
                || !reader.TryGetInt32(out var planetTypeInt)
                || !Enum.IsDefined(typeof(PlanetType), planetTypeInt))
            {
                throw new JsonException();
            }
            var planetType = (PlanetType)planetTypeInt;

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
            else
            {
                temperature = reader.GetDouble();
            }

            if (!reader.Read()
                || reader.TokenType != JsonTokenType.PropertyName
                || !reader.ValueTextEquals(nameof(Planetoid.AngleOfRotation))
                || !reader.Read())
            {
                throw new JsonException();
            }
            var angleOfRotation = reader.GetDouble();

            if (!reader.Read()
                || reader.TokenType != JsonTokenType.PropertyName
                || !reader.ValueTextEquals(nameof(Planetoid.RotationalPeriod))
                || !reader.Read()
                || reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException();
            }
            var rotationalPeriod = JsonSerializer.Deserialize<Number>(ref reader, options);

            if (!reader.Read()
                || reader.TokenType != JsonTokenType.PropertyName
                || !reader.ValueTextEquals("SatelliteIDs")
                || !reader.Read())
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
                || reader.TokenType != JsonTokenType.PropertyName
                || !reader.ValueTextEquals(nameof(Planetoid.Rings))
                || !reader.Read())
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
                || reader.TokenType != JsonTokenType.PropertyName
                || !reader.ValueTextEquals("BlackbodyTemperature")
                || !reader.Read())
            {
                throw new JsonException();
            }
            var blackbodyTemperature = reader.GetDouble();

            if (!reader.Read()
                || reader.TokenType != JsonTokenType.PropertyName
                || !reader.ValueTextEquals("SurfaceTemperatureAtApoapsis")
                || !reader.Read())
            {
                throw new JsonException();
            }
            var surfaceTemperatureAtApoapsis = reader.GetDouble();

            if (!reader.Read()
                || reader.TokenType != JsonTokenType.PropertyName
                || !reader.ValueTextEquals("SurfaceTemperatureAtPeriapsis")
                || !reader.Read())
            {
                throw new JsonException();
            }
            var surfaceTemperatureAtPeriapsis = reader.GetDouble();

            if (!reader.Read()
                || reader.TokenType != JsonTokenType.PropertyName
                || !reader.ValueTextEquals(nameof(Planetoid.IsInhospitable))
                || !reader.Read())
            {
                throw new JsonException();
            }
            var isInhospitable = reader.GetBoolean();

            if (!reader.Read()
                || reader.TokenType != JsonTokenType.PropertyName
                || !reader.ValueTextEquals("Earthlike")
                || !reader.Read())
            {
                throw new JsonException();
            }
            var earthlike = reader.GetBoolean();

            if (!reader.Read()
                || reader.TokenType != JsonTokenType.PropertyName
                || !reader.ValueTextEquals(nameof(PlanetParams))
                || !reader.Read())
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
                || reader.TokenType != JsonTokenType.PropertyName
                || !reader.ValueTextEquals(nameof(HabitabilityRequirements))
                || !reader.Read())
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

            if (!reader.Read()
                || reader.TokenType != JsonTokenType.PropertyName
                || !reader.ValueTextEquals("SurfaceRegions")
                || !reader.Read())
            {
                throw new JsonException();
            }
            List<SurfaceRegion>? surfaceRegions;
            if (reader.TokenType == JsonTokenType.Null)
            {
                surfaceRegions = null;
            }
            else if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw new JsonException();
            }
            else
            {
                surfaceRegions = JsonSerializer.Deserialize<List<SurfaceRegion>>(ref reader, options);
                if (surfaceRegions is null
                    || reader.TokenType != JsonTokenType.EndArray)
                {
                    throw new JsonException();
                }
            }

            if (!reader.Read()
                || reader.TokenType != JsonTokenType.PropertyName
                || !reader.ValueTextEquals("DepthMap")
                || !reader.Read())
            {
                throw new JsonException();
            }
            byte[]? depthMap;
            if (reader.TokenType == JsonTokenType.Null)
            {
                depthMap = null;
            }
            else
            {
                depthMap = reader.GetBytesFromBase64();
            }

            if (!reader.Read()
                || reader.TokenType != JsonTokenType.PropertyName
                || !reader.ValueTextEquals("ElevationMap")
                || !reader.Read())
            {
                throw new JsonException();
            }
            byte[]? elevationMap;
            if (reader.TokenType == JsonTokenType.Null)
            {
                elevationMap = null;
            }
            else
            {
                elevationMap = reader.GetBytesFromBase64();
            }

            if (!reader.Read()
                || reader.TokenType != JsonTokenType.PropertyName
                || !reader.ValueTextEquals("FlowMap")
                || !reader.Read())
            {
                throw new JsonException();
            }
            byte[]? flowMap;
            if (reader.TokenType == JsonTokenType.Null)
            {
                flowMap = null;
            }
            else
            {
                flowMap = reader.GetBytesFromBase64();
            }

            if (!reader.Read()
                || reader.TokenType != JsonTokenType.PropertyName
                || !reader.ValueTextEquals("PrecipitationMaps")
                || !reader.Read())
            {
                throw new JsonException();
            }
            byte[][]? precipitationMaps;
            if (reader.TokenType == JsonTokenType.Null)
            {
                precipitationMaps = null;
            }
            else if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw new JsonException();
            }
            else
            {
                var maps = new List<byte[]>();
                reader.Read();
                while (reader.TokenType != JsonTokenType.EndArray)
                {
                    maps.Add(reader.GetBytesFromBase64());
                    reader.Read();
                }
                precipitationMaps = maps.ToArray();
            }

            if (!reader.Read()
                || reader.TokenType != JsonTokenType.PropertyName
                || !reader.ValueTextEquals("SnowfallMaps")
                || !reader.Read())
            {
                throw new JsonException();
            }
            byte[][]? snowfallMaps;
            if (reader.TokenType == JsonTokenType.Null)
            {
                snowfallMaps = null;
            }
            else if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw new JsonException();
            }
            else
            {
                var maps = new List<byte[]>();
                reader.Read();
                while (reader.TokenType != JsonTokenType.EndArray)
                {
                    maps.Add(reader.GetBytesFromBase64());
                    reader.Read();
                }
                snowfallMaps = maps.ToArray();
            }

            if (!reader.Read()
                || reader.TokenType != JsonTokenType.PropertyName
                || !reader.ValueTextEquals("TemperatureMapSummer")
                || !reader.Read())
            {
                throw new JsonException();
            }
            byte[]? temperatureMapSummer;
            if (reader.TokenType == JsonTokenType.Null)
            {
                temperatureMapSummer = null;
            }
            else
            {
                temperatureMapSummer = reader.GetBytesFromBase64();
            }

            if (!reader.Read()
                || reader.TokenType != JsonTokenType.PropertyName
                || !reader.ValueTextEquals("TemperatureMapWinter")
                || !reader.Read())
            {
                throw new JsonException();
            }
            byte[]? temperatureMapWinter;
            if (reader.TokenType == JsonTokenType.Null)
            {
                temperatureMapWinter = null;
            }
            else
            {
                temperatureMapWinter = reader.GetBytesFromBase64();
            }

            if (!reader.Read()
                || reader.TokenType != JsonTokenType.PropertyName
                || !reader.ValueTextEquals("MaxFlow")
                || !reader.Read())
            {
                throw new JsonException();
            }
            double? maxFlow;
            if (reader.TokenType == JsonTokenType.Null)
            {
                maxFlow = null;
            }
            else
            {
                maxFlow = reader.GetDouble();
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
                habitabilityRequirements,
                surfaceRegions,
                depthMap,
                elevationMap,
                flowMap,
                precipitationMaps,
                snowfallMaps,
                temperatureMapSummer,
                temperatureMapWinter,
                maxFlow);
        }

        /// <summary>Writes a <see cref="Planetoid"/> as JSON.</summary>
        /// <param name="writer">The writer to write to.</param>
        /// <param name="value">The value to convert to JSON.</param>
        /// <param name="options">An object that specifies serialization options to use.</param>
        public override void Write(Utf8JsonWriter writer, Planetoid value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            writer.WriteString("id", value.Id);
            writer.WriteString(nameof(IIdItem.IdItemTypeName), value.IdItemTypeName);

            writer.WriteNumber("seed", value._seed);

            writer.WriteNumber(nameof(Planetoid.PlanetType), (int)value.PlanetType);

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

            writer.WriteNumber(nameof(Planetoid.AngleOfRotation), value.AngleOfRotation);

            writer.WritePropertyName(nameof(Planetoid.RotationalPeriod));
            JsonSerializer.Serialize(writer, value.RotationalPeriod, options);

            if (value._satelliteIDs is null)
            {
                writer.WriteNull("SatelliteIDs");
            }
            else
            {
                writer.WritePropertyName("SatelliteIDs");
                JsonSerializer.Serialize(writer, value._satelliteIDs, options);
            }

            if (value._rings is null)
            {
                writer.WriteNull(nameof(Planetoid.Rings));
            }
            else
            {
                writer.WritePropertyName(nameof(Planetoid.Rings));
                JsonSerializer.Serialize(writer, value._rings, options);
            }

            writer.WriteNumber("BlackbodyTemperature", value._blackbodyTemperature);
            writer.WriteNumber("SurfaceTemperatureAtApoapsis", value._surfaceTemperatureAtApoapsis);
            writer.WriteNumber("SurfaceTemperatureAtPeriapsis", value._surfaceTemperatureAtPeriapsis);
            writer.WriteBoolean(nameof(Planetoid.IsInhospitable), value.IsInhospitable);
            writer.WriteBoolean("Earthlike", value._earthlike);

            if (value._earthlike || value._planetParams is null)
            {
                writer.WriteNull(nameof(PlanetParams));
            }
            else
            {
                writer.WritePropertyName(nameof(PlanetParams));
                JsonSerializer.Serialize(writer, value._planetParams, options);
            }

            if (value._earthlike || value._habitabilityRequirements is null)
            {
                writer.WriteNull(nameof(HabitabilityRequirements));
            }
            else
            {
                writer.WritePropertyName(nameof(HabitabilityRequirements));
                JsonSerializer.Serialize(writer, value._habitabilityRequirements, options);
            }

            if (value._surfaceRegions is null)
            {
                writer.WriteNull("SurfaceRegions");
            }
            else
            {
                writer.WritePropertyName("SurfaceRegions");
                JsonSerializer.Serialize(writer, value._surfaceRegions, options);
            }

            if (value._depthMap is null)
            {
                writer.WriteNull("DepthMap");
            }
            else
            {
                writer.WriteBase64String("DepthMap", value._depthMap);
            }

            if (value._elevationMap is null)
            {
                writer.WriteNull("ElevationMap");
            }
            else
            {
                writer.WriteBase64String("ElevationMap", value._elevationMap);
            }

            if (value._flowMap is null)
            {
                writer.WriteNull("FlowMap");
            }
            else
            {
                writer.WriteBase64String("FlowMap", value._flowMap);
            }

            if (value._precipitationMaps is null)
            {
                writer.WriteNull("PrecipitationMaps");
            }
            else
            {
                writer.WritePropertyName("PrecipitationMaps");
                JsonSerializer.Serialize(writer, value._precipitationMaps, options);
            }

            if (value._snowfallMaps is null)
            {
                writer.WriteNull("SnowfallMaps");
            }
            else
            {
                writer.WritePropertyName("SnowfallMaps");
                JsonSerializer.Serialize(writer, value._snowfallMaps, options);
            }

            if (value._temperatureMapSummer is null)
            {
                writer.WriteNull("TemperatureMapSummer");
            }
            else
            {
                writer.WriteBase64String("TemperatureMapSummer", value._temperatureMapSummer);
            }

            if (value._temperatureMapWinter is null)
            {
                writer.WriteNull("TemperatureMapWinter");
            }
            else
            {
                writer.WriteBase64String("TemperatureMapWinter", value._temperatureMapWinter);
            }

            if (value._maxFlow.HasValue)
            {
                writer.WriteNumber("MaxFlow", value._maxFlow.Value);
            }
            else
            {
                writer.WriteNull("MaxFlow");
            }

            writer.WriteEndObject();
        }
    }
}
