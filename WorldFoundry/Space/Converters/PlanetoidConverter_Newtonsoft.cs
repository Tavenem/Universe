using NeverFoundry.DataStorage;
using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.WorldFoundry.Place;
using NeverFoundry.WorldFoundry.Space.Planetoids;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace NeverFoundry.WorldFoundry.Space.NewtonsoftJson
{
    /// <summary>
    /// Converts a <see cref="Planetoid"/> to and from JSON.
    /// </summary>
    public class PlanetoidConverter : JsonConverter
    {
        /// <summary>
        /// Determines whether this instance can convert the specified object type.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <returns>
        /// <see langword="true"/> if this instance can convert the specified object type;
        /// otherwise, <see langword="false"/>.
        /// </returns>
        public override bool CanConvert(Type objectType) => objectType == typeof(Planetoid);

        /// <summary>
        /// Reads the JSON representation of the object.
        /// </summary>
        /// <param name="reader">The <see cref="JsonReader" /> to read from.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="existingValue">The existing value of object being read.</param>
        /// <param name="serializer">The calling serializer.</param>
        /// <returns>The object value.</returns>
        [return: MaybeNull]
        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
            => FromJObj(JObject.Load(reader));

        /// <summary>
        /// Writes the JSON representation of the object.
        /// </summary>
        /// <param name="writer">The <see cref="JsonWriter" /> to write to.</param>
        /// <param name="value">The value.</param>
        /// <param name="serializer">The calling serializer.</param>
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value is null || value is not Planetoid location)
            {
                writer.WriteNull();
                return;
            }

            writer.WriteStartObject();

            writer.WritePropertyName("id");
            writer.WriteValue(location.Id);

            writer.WritePropertyName(nameof(IIdItem.IdItemTypeName));
            writer.WriteValue(location.IdItemTypeName);

            writer.WritePropertyName("seed");
            writer.WriteValue(location.Seed);

            writer.WritePropertyName(nameof(Planetoid.PlanetType));
            writer.WriteValue((int)location.PlanetType);

            writer.WritePropertyName(nameof(Location.ParentId));
            writer.WriteValue(location.ParentId);

            writer.WritePropertyName(nameof(Location.AbsolutePosition));
            if (location.AbsolutePosition is null)
            {
                writer.WriteNull();
            }
            else
            {
                serializer.Serialize(writer, location.AbsolutePosition, typeof(Vector3[]));
            }

            writer.WritePropertyName(nameof(CosmicLocation.Name));
            writer.WriteValue(location.Name);

            writer.WritePropertyName(nameof(CosmicLocation.Velocity));
            serializer.Serialize(writer, location.Velocity, typeof(Vector3));

            writer.WritePropertyName(nameof(CosmicLocation.Orbit));
            if (location.Orbit.HasValue)
            {
                serializer.Serialize(writer, location.Orbit.Value, typeof(Orbit));
            }
            else
            {
                writer.WriteNull();
            }

            writer.WritePropertyName(nameof(Location.Position));
            serializer.Serialize(writer, location.Position, typeof(Vector3));

            writer.WritePropertyName(nameof(MathAndScience.Chemistry.IMaterial.Temperature));
            writer.WriteValue(location.Material.Temperature);

            writer.WritePropertyName(nameof(Planetoid.AngleOfRotation));
            writer.WriteValue(location.AngleOfRotation);

            writer.WritePropertyName(nameof(Planetoid.RotationalPeriod));
            serializer.Serialize(writer, location.RotationalPeriod, typeof(Number));

            writer.WritePropertyName("SatelliteIDs");
            if (location._satelliteIDs is null)
            {
                writer.WriteNull();
            }
            else
            {
                writer.WriteStartArray();
                foreach (var item in location._satelliteIDs)
                {
                    writer.WriteValue(item);
                }
                writer.WriteEndArray();
            }

            writer.WritePropertyName(nameof(Planetoid.Rings));
            if (location._rings is null)
            {
                writer.WriteNull();
            }
            else
            {
                serializer.Serialize(writer, location._rings, typeof(List<PlanetaryRing>));
            }

            writer.WritePropertyName("BlackbodyTemperature");
            writer.WriteValue(location._blackbodyTemperature);

            writer.WritePropertyName("SurfaceTemperatureAtApoapsis");
            writer.WriteValue(location._surfaceTemperatureAtApoapsis);

            writer.WritePropertyName("SurfaceTemperatureAtPeriapsis");
            writer.WriteValue(location._surfaceTemperatureAtPeriapsis);

            writer.WritePropertyName(nameof(Planetoid.IsInhospitable));
            writer.WriteValue(location.IsInhospitable);

            writer.WritePropertyName("Earthlike");
            writer.WriteValue(location._earthlike);

            writer.WritePropertyName(nameof(PlanetParams));
            if (location._earthlike || location._planetParams is null)
            {
                writer.WriteNull();
            }
            else
            {
                serializer.Serialize(writer, location._planetParams, typeof(PlanetParams));
            }

            writer.WritePropertyName(nameof(HabitabilityRequirements));
            if (location._earthlike || location._habitabilityRequirements is null)
            {
                writer.WriteNull();
            }
            else
            {
                serializer.Serialize(writer, location._habitabilityRequirements, typeof(HabitabilityRequirements));
            }

            writer.WritePropertyName("ElevationMapPath");
            writer.WriteValue(location._elevationMapPath);

            writer.WritePropertyName("PrecipitationMapPaths");
            if (location._precipitationMapPaths is null)
            {
                writer.WriteNull();
            }
            else
            {
                writer.WriteStartArray();
                foreach (var item in location._precipitationMapPaths)
                {
                    writer.WriteValue(item);
                }
                writer.WriteEndArray();
            }

            writer.WritePropertyName("SnowfallMapPaths");
            if (location._snowfallMapPaths is null)
            {
                writer.WriteNull();
            }
            else
            {
                writer.WriteStartArray();
                foreach (var item in location._snowfallMapPaths)
                {
                    writer.WriteValue(item);
                }
                writer.WriteEndArray();
            }

            writer.WritePropertyName("TemperatureMapSummerPath");
            writer.WriteValue(location._temperatureMapSummerPath);

            writer.WritePropertyName("TemperatureMapWinterPath");
            writer.WriteValue(location._temperatureMapWinterPath);

            writer.WriteEndObject();
        }

        [return: MaybeNull]
        internal static object FromJObj(JObject jObj)
        {
            string id;
            if (!jObj.TryGetValue("id", out var idToken)
                || idToken.Type != JTokenType.String)
            {
                throw new JsonException();
            }
            else
            {
                id = idToken.Value<string>();
            }

            string idItemTypeName;
            if (!jObj.TryGetValue(nameof(IIdItem.IdItemTypeName), out var idItemTypeNameToken)
                || idItemTypeNameToken.Type != JTokenType.String)
            {
                throw new JsonException();
            }
            else
            {
                idItemTypeName = idItemTypeNameToken.Value<string>();
            }
            if (string.IsNullOrEmpty(idItemTypeName)
                || !string.Equals(idItemTypeName, Planetoid.PlanetoidIdItemTypeName))
            {
                throw new JsonException();
            }

            uint seed;
            if (!jObj.TryGetValue("seed", out var seedToken)
                || seedToken.Type != JTokenType.Integer)
            {
                throw new JsonException();
            }
            else
            {
                seed = seedToken.Value<uint>();
            }

            if (!jObj.TryGetValue(nameof(Planetoid.PlanetType), out var planetTypeToken)
                || planetTypeToken.Type != JTokenType.Integer)
            {
                throw new JsonException();
            }
            var planetType = (PlanetType)planetTypeToken.Value<int>();

            string? parentId;
            if (!jObj.TryGetValue(nameof(CosmicLocation.ParentId), out var parentIdToken))
            {
                throw new JsonException();
            }
            if (parentIdToken.Type == JTokenType.Null)
            {
                parentId = null;
            }
            else if (parentIdToken.Type == JTokenType.String)
            {
                parentId = parentIdToken.Value<string>();
            }
            else
            {
                throw new JsonException();
            }

            Vector3[]? absolutePosition;
            if (!jObj.TryGetValue(nameof(Location.AbsolutePosition), out var absolutePositionToken))
            {
                throw new JsonException();
            }
            if (absolutePositionToken.Type == JTokenType.Null)
            {
                absolutePosition = null;
            }
            else if (absolutePositionToken.Type == JTokenType.Array
                && absolutePositionToken is JArray absolutePositionArray)
            {
                absolutePosition = absolutePositionArray.ToObject<Vector3[]>();
            }
            else
            {
                throw new JsonException();
            }

            string? name;
            if (!jObj.TryGetValue(nameof(CosmicLocation.Name), out var nameToken))
            {
                throw new JsonException();
            }
            if (nameToken.Type == JTokenType.Null)
            {
                name = null;
            }
            else if (nameToken.Type == JTokenType.String)
            {
                name = nameToken.Value<string>();
            }
            else
            {
                throw new JsonException();
            }

            Vector3 velocity;
            if (!jObj.TryGetValue(nameof(CosmicLocation.Velocity), out var velocityToken)
                || velocityToken.Type != JTokenType.Object)
            {
                throw new JsonException();
            }
            else
            {
                velocity = velocityToken.ToObject<Vector3>();
            }

            Orbit? orbit;
            if (!jObj.TryGetValue(nameof(CosmicLocation.Orbit), out var orbitToken))
            {
                throw new JsonException();
            }
            if (orbitToken.Type == JTokenType.Null)
            {
                orbit = null;
            }
            else if (orbitToken.Type == JTokenType.Object)
            {
                orbit = orbitToken.ToObject<Orbit>();
            }
            else
            {
                throw new JsonException();
            }

            Vector3 position;
            if (!jObj.TryGetValue(nameof(Location.Position), out var positionToken)
                || positionToken.Type != JTokenType.Object)
            {
                throw new JsonException();
            }
            else
            {
                position = positionToken.ToObject<Vector3>();
            }

            double? temperature;
            if (!jObj.TryGetValue(nameof(CosmicLocation.Temperature), out var temperatureToken))
            {
                throw new JsonException();
            }
            if (temperatureToken.Type == JTokenType.Null)
            {
                temperature = null;
            }
            else if (temperatureToken.Type == JTokenType.Float)
            {
                temperature = temperatureToken.Value<double>();
            }
            else
            {
                throw new JsonException();
            }

            if (!jObj.TryGetValue(nameof(Planetoid.AngleOfRotation), out var angleOfRotationToken)
                || angleOfRotationToken.Type != JTokenType.Float)
            {
                throw new JsonException();
            }
            var angleOfRotation = angleOfRotationToken.Value<double>();

            if (!jObj.TryGetValue(nameof(Planetoid.RotationalPeriod), out var rotationalPeriodToken)
                || rotationalPeriodToken.Type != JTokenType.String)
            {
                throw new JsonException();
            }
            var rotationalPeriod = rotationalPeriodToken.ToObject<Number>();

            if (!jObj.TryGetValue("SatelliteIDs", out var satelliteIDsToken))
            {
                throw new JsonException();
            }
            List<string>? satelliteIds;
            if (satelliteIDsToken.Type == JTokenType.Null)
            {
                satelliteIds = null;
            }
            else if (satelliteIDsToken.Type != JTokenType.Array
                || satelliteIDsToken is not JArray satelliteIdsArray)
            {
                throw new JsonException();
            }
            else
            {
                satelliteIds = satelliteIdsArray.ToObject<List<string>>();
            }

            if (!jObj.TryGetValue(nameof(Planetoid.Rings), out var ringsToken))
            {
                throw new JsonException();
            }
            List<PlanetaryRing>? rings;
            if (ringsToken.Type == JTokenType.Null)
            {
                rings = null;
            }
            else if (ringsToken.Type != JTokenType.Array
                || ringsToken is not JArray ringsArray)
            {
                throw new JsonException();
            }
            else
            {
                rings = ringsArray.ToObject<List<PlanetaryRing>>();
            }

            if (!jObj.TryGetValue("BlackbodyTemperature", out var blackbodyTemperatureToken)
                || blackbodyTemperatureToken.Type != JTokenType.Float)
            {
                throw new JsonException();
            }
            var blackbodyTemperature = blackbodyTemperatureToken.Value<double>();

            if (!jObj.TryGetValue("SurfaceTemperatureAtApoapsis", out var surfaceTemperatureAtApoapsisToken)
                || surfaceTemperatureAtApoapsisToken.Type != JTokenType.Float)
            {
                throw new JsonException();
            }
            var surfaceTemperatureAtApoapsis = surfaceTemperatureAtApoapsisToken.Value<double>();

            if (!jObj.TryGetValue("SurfaceTemperatureAtPeriapsis", out var surfaceTemperatureAtPeriapsisToken)
                || surfaceTemperatureAtPeriapsisToken.Type != JTokenType.Float)
            {
                throw new JsonException();
            }
            var surfaceTemperatureAtPeriapsis = surfaceTemperatureAtPeriapsisToken.Value<double>();

            if (!jObj.TryGetValue(nameof(Planetoid.IsInhospitable), out var isInhospitableToken)
                || isInhospitableToken.Type != JTokenType.Boolean)
            {
                throw new JsonException();
            }
            var isInhospitable = isInhospitableToken.Value<bool>();

            if (!jObj.TryGetValue("Earthlike", out var earthlikeToken)
                || earthlikeToken.Type != JTokenType.Boolean)
            {
                throw new JsonException();
            }
            var earthlike = earthlikeToken.Value<bool>();

            if (!jObj.TryGetValue(nameof(PlanetParams), out var planetParamsToken))
            {
                throw new JsonException();
            }
            PlanetParams? planetParams;
            if (planetParamsToken.Type == JTokenType.Null)
            {
                planetParams = null;
            }
            else
            {
                planetParams = planetParamsToken.ToObject<PlanetParams>();
            }

            if (!jObj.TryGetValue(nameof(HabitabilityRequirements), out var habitabilityRequirementsToken))
            {
                throw new JsonException();
            }
            HabitabilityRequirements? habitabilityRequirements;
            if (habitabilityRequirementsToken.Type == JTokenType.Null)
            {
                habitabilityRequirements = null;
            }
            else
            {
                habitabilityRequirements = habitabilityRequirementsToken.ToObject<HabitabilityRequirements>();
            }

            if (!jObj.TryGetValue("ElevationMapPath", out var elevationMapToken))
            {
                throw new JsonException();
            }
            string? elevationMapPath;
            if (elevationMapToken.Type == JTokenType.Null)
            {
                elevationMapPath = null;
            }
            else if (elevationMapToken.Type == JTokenType.String)
            {
                elevationMapPath = elevationMapToken.Value<string>();
            }
            else
            {
                throw new JsonException();
            }

            if (!jObj.TryGetValue("PrecipitationMapPaths", out var precipitationMapsToken))
            {
                throw new JsonException();
            }
            string?[]? precipitationMapPaths;
            if (precipitationMapsToken.Type == JTokenType.Null)
            {
                precipitationMapPaths = null;
            }
            else
            {
                precipitationMapPaths = precipitationMapsToken.ToObject<string?[]>();
            }

            if (!jObj.TryGetValue("SnowfallMapPaths", out var snowfallMapsToken))
            {
                throw new JsonException();
            }
            string?[]? snowfallMapPaths;
            if (snowfallMapsToken.Type == JTokenType.Null)
            {
                snowfallMapPaths = null;
            }
            else
            {
                snowfallMapPaths = snowfallMapsToken.ToObject<string?[]>();
            }

            if (!jObj.TryGetValue("TemperatureMapSummerPath", out var temperatureMapSummerToken))
            {
                throw new JsonException();
            }
            string? temperatureMapSummerPath;
            if (temperatureMapSummerToken.Type == JTokenType.Null)
            {
                temperatureMapSummerPath = null;
            }
            else if (temperatureMapSummerToken.Type == JTokenType.String)
            {
                temperatureMapSummerPath = temperatureMapSummerToken.Value<string>();
            }
            else
            {
                throw new JsonException();
            }

            if (!jObj.TryGetValue("TemperatureMapWinterPath", out var temperatureMapWinterToken))
            {
                throw new JsonException();
            }
            string? temperatureMapWinterPath;
            if (temperatureMapWinterToken.Type == JTokenType.Null)
            {
                temperatureMapWinterPath = null;
            }
            else if (temperatureMapWinterToken.Type == JTokenType.String)
            {
                temperatureMapWinterPath = temperatureMapWinterToken.Value<string>();
            }
            else
            {
                throw new JsonException();
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
                satelliteIds,
                rings,
                blackbodyTemperature,
                surfaceTemperatureAtApoapsis,
                surfaceTemperatureAtPeriapsis,
                isInhospitable,
                earthlike,
                planetParams,
                habitabilityRequirements,
                elevationMapPath,
                precipitationMapPaths,
                snowfallMapPaths,
                temperatureMapSummerPath,
                temperatureMapWinterPath);
        }
    }
}
