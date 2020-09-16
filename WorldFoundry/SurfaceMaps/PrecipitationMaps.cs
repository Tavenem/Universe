using Newtonsoft.Json;
using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace NeverFoundry.WorldFoundry.SurfaceMapping
{
    /// <summary>
    /// A set of two-dimensional arrays corresponding to points on an equirectangular projected map
    /// of a terrestrial planet's surface, with information about the planet's weather during a
    /// particular portion of a year.
    /// </summary>
    [Serializable]
    [JsonObject]
    public struct PrecipitationMaps : ISerializable
    {
        /// <summary>
        /// A range giving the minimum, maximum, and average precipitation throughout the specified
        /// area during the period represented by this map, as a value between 0 and 1, with 1
        /// indicating the maximum annual potential precipitation of the planet's atmosphere.
        /// <seealso cref="Climate.Atmosphere.MaxPrecipitation"/>
        /// </summary>
        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public FloatRange Precipitation { get; }

        /// <summary>
        /// <para>
        /// A two-dimensional array corresponding to points on an equirectangular projected map of a
        /// terrestrial planet's surface. The first index corresponds to the X coordinate, and the
        /// second index corresponds to the Y coordinate. The values represent the total amount of
        /// precipitation which falls during the specified period. Values range from 0 to 1, with 1
        /// indicating the maximum annual potential precipitation of the planet's atmosphere.
        /// </para>
        /// <para>
        /// See also <seealso cref="Climate.Atmosphere.MaxPrecipitation"/>.
        /// </para>
        /// </summary>
        public float[][] PrecipitationMap { get; }

        /// <summary>
        /// A range giving the minimum, maximum, and average snowfall throughout the specified
        /// area, as a value between 0 and 1, with 1 indicating the maximum annual potential
        /// snowfall of the planet's atmosphere.
        /// <seealso cref="Climate.Atmosphere.MaxSnowfall"/>
        /// </summary>
        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public FloatRange Snowfall { get; }

        /// <summary>
        /// <para>
        /// A two-dimensional array corresponding to points on an equirectangular projected map of a
        /// terrestrial planet's surface. The first index corresponds to the X coordinate, and the
        /// second index corresponds to the Y coordinate. The values represent the total amount of
        /// snow which falls during the specified period. Values range from 0 to 1, with 1
        /// indicating the maximum potential snowfall of the planet's atmosphere.
        /// </para>
        /// <para>
        /// See also <seealso cref="Climate.Atmosphere.MaxSnowfall"/>.
        /// </para>
        /// </summary>
        public float[][] SnowfallMap { get; }

        /// <summary>
        /// The length of the "X" (0-index) dimension of the maps.
        /// </summary>
        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public int XLength { get; }

        /// <summary>
        /// The length of the "Y" (1-index) dimension of the maps.
        /// </summary>
        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public int YLength { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="PrecipitationMaps"/>.
        /// </summary>
        /// <param name="precipitationMap">
        /// A precipitation map. Must have the same dimensions as the <paramref name="snowfallMap"/> map.
        /// </param>
        /// <param name="snowfallMap">
        /// A snowfall map. Must have the same dimensions as the <paramref name="precipitationMap"/> map.
        /// </param>
        [JsonConstructor]
        [System.Text.Json.Serialization.JsonConstructor]
        public PrecipitationMaps(float[][] precipitationMap, float[][] snowfallMap)
        {
            if (precipitationMap.Length != snowfallMap.Length)
            {
                throw new ArgumentException($"X length of {nameof(precipitationMap)} was not equal to X length of {nameof(snowfallMap)}");
            }

            XLength = precipitationMap.Length;
            YLength = XLength == 0 ? 0 : precipitationMap[0].Length;

            if (XLength != 0 && snowfallMap[0].Length != YLength)
            {
                throw new ArgumentException($"Y length of {nameof(precipitationMap)} was not equal to Y length of {nameof(snowfallMap)}");
            }

            PrecipitationMap = precipitationMap;
            SnowfallMap = snowfallMap;

            var min = 2f;
            var max = -2f;
            var sum = 0f;
            for (var x = 0; x < XLength; x++)
            {
                for (var y = 0; y < YLength; y++)
                {
                    min = Math.Min(min, precipitationMap[x][y]);
                    max = Math.Max(max, precipitationMap[x][y]);
                    sum += precipitationMap[x][y];
                }
            }
            Precipitation = new FloatRange(min, sum / (XLength * YLength), max);

            min = 2f;
            max = -2f;
            sum = 0f;
            for (var x = 0; x < XLength; x++)
            {
                for (var y = 0; y < YLength; y++)
                {
                    min = Math.Min(min, snowfallMap[x][y]);
                    max = Math.Max(max, snowfallMap[x][y]);
                    sum += snowfallMap[x][y];
                }
            }
            Snowfall = new FloatRange(min, sum / (XLength * YLength), max);
        }

        private PrecipitationMaps(SerializationInfo info, StreamingContext context) : this(
            (float[][]?)info.GetValue(nameof(PrecipitationMap), typeof(float[][])) ?? Array.Empty<float[]>(),
            (float[][]?)info.GetValue(nameof(SnowfallMap), typeof(float[][])) ?? Array.Empty<float[]>())
        { }

        /// <summary>Populates a <see cref="SerializationInfo"></see> with the data needed to
        /// serialize the target object.</summary>
        /// <param name="info">The <see cref="SerializationInfo"></see> to populate with
        /// data.</param>
        /// <param name="context">The destination (see <see cref="StreamingContext"></see>) for this
        /// serialization.</param>
        /// <exception cref="System.Security.SecurityException">The caller does not have the
        /// required permission.</exception>
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(PrecipitationMap), PrecipitationMap);
            info.AddValue(nameof(SnowfallMap), SnowfallMap);
        }
    }
}
