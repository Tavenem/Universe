using NeverFoundry.WorldFoundry.Climate;
using Newtonsoft.Json;
using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace NeverFoundry.WorldFoundry.SurfaceMapping
{
    /// <summary>
    /// A collection of surface and weather maps for a <see cref="Space.Planetoid"/>.
    /// </summary>
    [Serializable]
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public struct SurfaceMaps : ISerializable
    {
        /// <summary>
        /// <para>
        /// The average normalized elevation of the area, from -1 to 1, where negative values are
        /// below sea level and positive values are above sea level, and 1 is equal to the maximum
        /// elevation of the planet.
        /// </para>
        /// <para>
        /// See also <seealso cref="Space.Planetoid.MaxElevation"/>.
        /// </para>
        /// </summary>
        [JsonProperty]
        public double AverageElevation { get; }

        /// <summary>
        /// The overall <see cref="BiomeType"/> of the area.
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public BiomeType Biome => WeatherMaps.Biome;

        /// <summary>
        /// A two-dimensional array corresponding to points on an equirectangular projected map of a
        /// terrestrial planet's surface. The first index corresponds to the X coordinate, and the
        /// second index corresponds to the Y coordinate. The values represent <see
        /// cref="BiomeType"/>.
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public BiomeType[][] BiomeMap => WeatherMaps.BiomeMap;

        /// <summary>
        /// The overall <see cref="ClimateType"/> of the area, based on average annual temperature.
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public ClimateType Climate => WeatherMaps.Climate;

        /// <summary>
        /// A two-dimensional array corresponding to points on an equirectangular projected map of a
        /// terrestrial planet's surface. The first index corresponds to the X coordinate, and the
        /// second index corresponds to the Y coordinate. The values represent <see
        /// cref="ClimateType"/>, based on average annual temperature.
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public ClimateType[][] ClimateMap => WeatherMaps.ClimateMap;

        /// <summary>
        /// A two-dimensional array corresponding to points on an equirectangular projected map of a
        /// terrestrial planet's surface. The first index corresponds to the X coordinate, and the
        /// second index corresponds to the Y coordinate. The values indicate the elevation of
        /// surface water in the location (i.e. lakes). Values range from 0 to 1, with 1 indicating
        /// the maximum elevation of the planet. Note that the values are depths above ground level,
        /// not absolute elevations above sea level. To obtain the absolute elevation of the water
        /// level at any point, the sum of this map and the elevation map can be taken.
        /// <seealso cref="Space.Planetoid.MaxElevation"/>
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public float[][] Depth => HydrologyMaps.Depth;

        /// <summary>
        /// The overall <see cref="EcologyType"/> of the area.
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public EcologyType Ecology => WeatherMaps.Ecology;

        /// <summary>
        /// A two-dimensional array corresponding to points on an equirectangular projected map of a
        /// terrestrial planet's surface. The first index corresponds to the X coordinate, and the
        /// second index corresponds to the Y coordinate. The values represent <see
        /// cref="EcologyType"/>.
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public EcologyType[][] EcologyMap => WeatherMaps.EcologyMap;

        /// <summary>
        /// A two-dimensional array corresponding to points on an equirectangular projected map of a
        /// terrestrial planet's surface. The first index corresponds to the X coordinate, and the
        /// second index corresponds to the Y coordinate. The values represent normalized elevations
        /// from -1 to 1, where negative values are below sea level and positive values are above
        /// sea level, and 1 is equal to the maximum elevation of the planet.
        /// <seealso cref="Space.Planetoid.MaxElevation"/>
        /// </summary>
        [JsonProperty]
        public double[][] Elevation { get; }

        /// <summary>
        /// <para>
        /// A two-dimensional array corresponding to points on an equirectangular projected map of a
        /// terrestrial planet's surface. The first index corresponds to the X coordinate, and the
        /// second index corresponds to the Y coordinate. The values represent the flow rate of
        /// surface and/or ground water in that location, in m³/s. Note that the values are not
        /// normalized, unlike those of most surface maps.
        /// </para>
        /// <para>This map does not distinguish between smaller or larger flows (e.g. rivers vs.
        /// streams vs. seeping groundwater). A visualization tool which is displaying flow
        /// information should set a threshold above which flow is displayed, in order to cull the
        /// finer branches of the flow network. Generally speaking, the more distant the view, the
        /// higher the culling threshold should be in order to make flow look more like a map of
        /// distinct "rivers" and less like a dense mesh.
        /// </para>
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public float[][] Flow => HydrologyMaps.Flow;

        /// <summary>
        /// The overall <see cref="HumidityType"/> of the area, based on annual precipitation.
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public HumidityType Humidity => WeatherMaps.Humidity;

        /// <summary>
        /// A two-dimensional array corresponding to points on an equirectangular projected map of a
        /// terrestrial planet's surface. The first index corresponds to the X coordinate, and the
        /// second index corresponds to the Y coordinate. The values represent <see
        /// cref="HumidityType"/>, based on annual precipitation.
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public HumidityType[][] HumidityMap => WeatherMaps.HumidityMap;

        /// <summary>
        /// The <see cref="SurfaceMapping.HydrologyMaps"/> associated with this instance.
        /// </summary>
        [JsonProperty]
        public HydrologyMaps HydrologyMaps { get; }

        /// <summary>
        /// The maximum elevation of the mapped area, in meters.
        /// </summary>
        [JsonProperty]
        public double MaxElevation { get; }

        /// <summary>
        /// A collection of <see cref="SurfaceMapping.PrecipitationMaps"/>.
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public PrecipitationMaps[] PrecipitationMaps => WeatherMaps.PrecipitationMaps;

        /// <summary>
        /// A two-dimensional array corresponding to points on an equirectangular projected map of a
        /// terrestrial planet's surface. The first index corresponds to the X coordinate, and the
        /// second index corresponds to the Y coordinate. The values represent the proportion of the
        /// year during which there is persistent sea ice.
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public FloatRange[][] SeaIceRangeMap => WeatherMaps.SeaIceRangeMap;

        /// <summary>
        /// The number of seasons into which the year is divided by this set, for the purposes of
        /// precipitation and snowfall reporting.
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public int Seasons => WeatherMaps.Seasons;

        /// <summary>
        /// A two-dimensional array corresponding to points on an equirectangular projected map of a
        /// terrestrial planet's surface. The first index corresponds to the X coordinate, and the
        /// second index corresponds to the Y coordinate. The values represent the proportion of the
        /// year during which there is persistent snow cover.
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public FloatRange[][] SnowCoverRangeMap => WeatherMaps.SnowCoverRangeMap;

        /// <summary>
        /// A range giving the minimum, maximum, and average temperature throughout the specified
        /// area over the entire period represented by all <see cref="PrecipitationMaps"/>.
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public FloatRange TemperatureRange => WeatherMaps.TemperatureRange;

        /// <summary>
        /// A two-dimensional array corresponding to points on an equirectangular projected map of a
        /// terrestrial planet's surface. The first index corresponds to the X coordinate, and the
        /// second index corresponds to the Y coordinate. The values represent the temperature
        /// range.
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public FloatRange[][] TemperatureRangeMap => WeatherMaps.TemperatureRangeMap;

        /// <summary>
        /// A range giving the minimum, maximum, and average precipitation throughout the specified
        /// area over the entire period represented by all <see cref="PrecipitationMaps"/>, as a value
        /// between 0 and 1, with 1 indicating the maximum annual potential precipitation of the
        /// planet's atmosphere.
        /// <seealso cref="Atmosphere.MaxPrecipitation"/>
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public FloatRange TotalPrecipitation => WeatherMaps.TotalPrecipitation;

        /// <summary>
        /// A two-dimensional array corresponding to points on an equirectangular projected map of a
        /// terrestrial planet's surface. The first index corresponds to the X coordinate, and the
        /// second index corresponds to the Y coordinate. The values represent the total amount of
        /// precipitation indicated on all contained <see cref="PrecipitationMaps"/>. Values range from 0
        /// to 1, with 1 indicating the maximum annual potential precipitation of the planet's
        /// atmosphere. Will be <see langword="null"/> if no <see cref="PrecipitationMaps"/> are present.
        /// <seealso cref="Atmosphere.MaxPrecipitation"/>
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public float[][] TotalPrecipitationMap => WeatherMaps.TotalPrecipitationMap;

        /// <summary>
        /// A range giving the minimum, maximum, and average snowfall throughout the specified area
        /// over the entire period represented by all <see cref="PrecipitationMaps"/>, as a value
        /// between 0 and 1, with 1 indicating the maximum annual potential snowfall of the planet's
        /// atmosphere.
        /// <seealso cref="Atmosphere.MaxSnowfall"/>
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public FloatRange TotalSnowfall => WeatherMaps.TotalSnowfall;

        /// <summary>
        /// A two-dimensional array corresponding to points on an equirectangular projected map of a
        /// terrestrial planet's surface. The first index corresponds to the X coordinate, and the
        /// second index corresponds to the Y coordinate. The values represent the total amount of
        /// snowfall indicated on all contained <see cref="PrecipitationMaps"/>. Values range from 0
        /// to 1, with 1 indicating the maximum annual potential snowfall of the planet's
        /// atmosphere. Will be <see langword="null"/> if no <see cref="PrecipitationMaps"/> are
        /// present.
        /// <seealso cref="Atmosphere.MaxSnowfall"/>
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public float[][] TotalSnowfallMap => WeatherMaps.TotalSnowfallMap;

        /// <summary>
        /// The length of the "X" (0-index) dimension of the maps.
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public int XLength { get; }

        /// <summary>
        /// The length of the "Y" (1-index) dimension of the maps.
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public int YLength { get; }

        /// <summary>
        /// The <see cref="SurfaceMapping.WeatherMaps"/> associated with this instance.
        /// </summary>
        [JsonProperty]
        public WeatherMaps WeatherMaps { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="SurfaceMaps"/>.
        /// </summary>
        /// <param name="elevation">An elevation map.</param>
        /// <param name="averageElevation">
        /// The average normalized elevation of the area, from -1 to 1, where negative values are
        /// below sea level and positive values are above sea level, and 1 is equal to the maximum
        /// elevation of the area.
        /// </param>
        /// <param name="maxElevation">
        /// The maximum elevation of the mapped area, in meters.
        /// </param>
        /// <param name="weatherMaps">A <see cref="PrecipitationMaps"/> instance.</param>
        /// <param name="hydrologyMaps">A <see cref="SurfaceMapping.HydrologyMaps"/> instance.</param>
        [JsonConstructor]
        [System.Text.Json.Serialization.JsonConstructor]
        public SurfaceMaps(
            double[][] elevation,
            double averageElevation,
            double maxElevation,
            WeatherMaps weatherMaps,
            HydrologyMaps hydrologyMaps)
        {
            XLength = elevation.Length;
            YLength = XLength == 0 ? 0 : elevation[0].Length;

            if (weatherMaps.XLength != XLength)
            {
                throw new ArgumentException($"X length of {nameof(weatherMaps)} was not equal to X length of {nameof(elevation)}");
            }
            if (weatherMaps.YLength != YLength)
            {
                throw new ArgumentException($"Y length of {nameof(weatherMaps)} was not equal to Y length of {nameof(elevation)}");
            }

            if (hydrologyMaps.XLength != XLength)
            {
                throw new ArgumentException($"X length of {nameof(hydrologyMaps)} was not equal to X length of {nameof(elevation)}");
            }
            if (hydrologyMaps.YLength != YLength)
            {
                throw new ArgumentException($"Y length of {nameof(hydrologyMaps)} was not equal to Y length of {nameof(elevation)}");
            }

            AverageElevation = averageElevation;
            Elevation = elevation;
            MaxElevation = maxElevation;
            WeatherMaps = weatherMaps;
            HydrologyMaps = hydrologyMaps;
        }

        private SurfaceMaps(SerializationInfo info, StreamingContext context) : this(
            (double[][]?)info.GetValue(nameof(Elevation), typeof(double[][])) ?? new double[0][],
            (double?)info.GetValue(nameof(AverageElevation), typeof(double)) ?? default,
            (double?)info.GetValue(nameof(MaxElevation), typeof(double)) ?? default,
            (WeatherMaps?)info.GetValue(nameof(WeatherMaps), typeof(WeatherMaps)) ?? default,
            (HydrologyMaps?)info.GetValue(nameof(HydrologyMaps), typeof(HydrologyMaps)) ?? default)
        { }

        /// <summary>Populates a <see cref="SerializationInfo"></see> with the data needed to
        /// serialize the target object.</summary>
        /// <param name="info">The <see cref="SerializationInfo"></see> to populate with
        /// data.</param>
        /// <param name="context">The destination (see <see cref="StreamingContext"></see>) for this
        /// serialization.</param>
        /// <exception cref="System.Security.SecurityException">The caller does not have the
        /// required permission.</exception>
        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(Elevation), Elevation);
            info.AddValue(nameof(AverageElevation), AverageElevation);
            info.AddValue(nameof(MaxElevation), MaxElevation);
            info.AddValue(nameof(WeatherMaps), WeatherMaps);
            info.AddValue(nameof(HydrologyMaps), HydrologyMaps);
        }
    }
}
