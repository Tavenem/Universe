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
            if (value is null || !(value is Planetoid location))
            {
                writer.WriteNull();
                return;
            }

            writer.WriteStartObject();

            writer.WritePropertyName(nameof(IIdItem.Id));
            writer.WriteValue(location.Id);

            writer.WritePropertyName(nameof(IIdItem.IdItemTypeName));
            writer.WriteValue(location.IdItemTypeName);

            writer.WritePropertyName("seed");
            writer.WriteValue(location._seed);

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

            writer.WritePropertyName("SurfaceRegions");
            if (location._surfaceRegions is null)
            {
                writer.WriteNull();
            }
            else
            {
                serializer.Serialize(writer, location._surfaceRegions, typeof(List<SurfaceRegion>));
            }

            writer.WritePropertyName("DepthMap");
            if (location._depthMap is null)
            {
                writer.WriteNull();
            }
            else
            {
                writer.WriteValue(location._depthMap);
            }

            writer.WritePropertyName("ElevationMap");
            if (location._elevationMap is null)
            {
                writer.WriteNull();
            }
            else
            {
                writer.WriteValue(location._elevationMap);
            }

            writer.WritePropertyName("FlowMap");
            if (location._flowMap is null)
            {
                writer.WriteNull();
            }
            else
            {
                writer.WriteValue(location._flowMap);
            }

            writer.WritePropertyName("PrecipitationMaps");
            if (location._precipitationMaps is null)
            {
                writer.WriteNull();
            }
            else
            {
                writer.WriteStartArray();
                foreach (var item in location._precipitationMaps)
                {
                    writer.WriteValue(item);
                }
                writer.WriteEndArray();
            }

            writer.WritePropertyName("SnowfallMaps");
            if (location._snowfallMaps is null)
            {
                writer.WriteNull();
            }
            else
            {
                writer.WriteStartArray();
                foreach (var item in location._snowfallMaps)
                {
                    writer.WriteValue(item);
                }
                writer.WriteEndArray();
            }

            writer.WritePropertyName("TemperatureMapSummer");
            if (location._temperatureMapSummer is null)
            {
                writer.WriteNull();
            }
            else
            {
                writer.WriteValue(location._temperatureMapSummer);
            }

            writer.WritePropertyName("TemperatureMapWinter");
            if (location._temperatureMapWinter is null)
            {
                writer.WriteNull();
            }
            else
            {
                writer.WriteValue(location._temperatureMapWinter);
            }

            writer.WritePropertyName("MaxFlow");
            if (location._maxFlow.HasValue)
            {
                writer.WriteValue(location._maxFlow.Value);
            }
            else
            {
                writer.WriteNull();
            }

            writer.WriteEndObject();
        }

        [return: MaybeNull]
        internal object FromJObj(JObject jObj)
        {
            string id;
            if (!jObj.TryGetValue(nameof(IIdItem.Id), out var idToken)
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
            if (!jObj.TryGetValue(nameof(CosmicLocation.Name), out var parentIdToken))
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
                || !(satelliteIDsToken is JArray satelliteIdsArray))
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
                || !(ringsToken is JArray ringsArray))
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

            if (!jObj.TryGetValue("SurfaceRegions", out var surfaceRegionsToken))
            {
                throw new JsonException();
            }
            List<SurfaceRegion>? surfaceRegions;
            if (surfaceRegionsToken.Type == JTokenType.Null)
            {
                surfaceRegions = null;
            }
            else if (surfaceRegionsToken.Type != JTokenType.Array
                || !(surfaceRegionsToken is JArray surfaceRegionsArray))
            {
                throw new JsonException();
            }
            else
            {
                surfaceRegions = surfaceRegionsArray.ToObject<List<SurfaceRegion>>();
            }

            if (!jObj.TryGetValue("DepthMap", out var depthMapToken))
            {
                throw new JsonException();
            }
            byte[]? depthMap;
            if (depthMapToken.Type == JTokenType.Null)
            {
                depthMap = null;
            }
            else
            {
                depthMap = depthMapToken.ToObject<byte[]>();
            }

            if (!jObj.TryGetValue("ElevationMap", out var elevationMapToken))
            {
                throw new JsonException();
            }
            byte[]? elevationMap;
            if (elevationMapToken.Type == JTokenType.Null)
            {
                elevationMap = null;
            }
            else
            {
                elevationMap = elevationMapToken.ToObject<byte[]>();
            }

            if (!jObj.TryGetValue("FlowMap", out var flowMapToken))
            {
                throw new JsonException();
            }
            byte[]? flowMap;
            if (flowMapToken.Type == JTokenType.Null)
            {
                flowMap = null;
            }
            else
            {
                flowMap = flowMapToken.ToObject<byte[]>();
            }

            if (!jObj.TryGetValue("PrecipitationMaps", out var precipitationMapsToken))
            {
                throw new JsonException();
            }
            byte[][]? precipitationMaps;
            if (precipitationMapsToken.Type == JTokenType.Null)
            {
                precipitationMaps = null;
            }
            else
            {
                precipitationMaps = precipitationMapsToken.ToObject<byte[][]>();
            }

            if (!jObj.TryGetValue("SnowfallMaps", out var snowfallMapsToken))
            {
                throw new JsonException();
            }
            byte[][]? snowfallMaps;
            if (snowfallMapsToken.Type == JTokenType.Null)
            {
                snowfallMaps = null;
            }
            else
            {
                snowfallMaps = snowfallMapsToken.ToObject<byte[][]>();
            }

            if (!jObj.TryGetValue("TemperatureMapSummer", out var temperatureMapSummerToken))
            {
                throw new JsonException();
            }
            byte[]? temperatureMapSummer;
            if (temperatureMapSummerToken.Type == JTokenType.Null)
            {
                temperatureMapSummer = null;
            }
            else
            {
                temperatureMapSummer = temperatureMapSummerToken.ToObject<byte[]>();
            }

            if (!jObj.TryGetValue("TemperatureMapWinter", out var temperatureMapWinterToken))
            {
                throw new JsonException();
            }
            byte[]? temperatureMapWinter;
            if (temperatureMapWinterToken.Type == JTokenType.Null)
            {
                temperatureMapWinter = null;
            }
            else
            {
                temperatureMapWinter = temperatureMapWinterToken.ToObject<byte[]>();
            }

            double? maxFlow;
            if (!jObj.TryGetValue("MaxFlow", out var maxFlowToken))
            {
                throw new JsonException();
            }
            if (maxFlowToken.Type == JTokenType.Null)
            {
                maxFlow = null;
            }
            else if (maxFlowToken.Type == JTokenType.Float)
            {
                maxFlow = maxFlowToken.Value<double>();
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
    }
}
