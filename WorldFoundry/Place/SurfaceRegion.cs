using NeverFoundry.MathAndScience.Constants.Numbers;
using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.WorldFoundry.Space;
using NeverFoundry.WorldFoundry.SurfaceMapping;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace NeverFoundry.WorldFoundry.Place
{
    /// <summary>
    /// A <see cref="Location"/> on the surface of a <see cref="Planetoid"/>, which can override
    /// local conditions with manually-specified maps.
    /// </summary>
    [Serializable]
    [JsonObject]
    [System.Text.Json.Serialization.JsonConverter(typeof(SurfaceRegionConverter))]
    public class SurfaceRegion : Location
    {
        [JsonProperty(PropertyName = "depthMap")]
        internal byte[]? _depthMap;

        [JsonProperty(PropertyName = "elevationMap")]
        internal byte[]? _elevationMap;

        [JsonProperty(PropertyName = "flowMap")]
        internal byte[]? _flowMap;

        [JsonProperty(PropertyName = "precipitationMaps")]
        internal byte[][]? _precipitationMaps;

        [JsonProperty(PropertyName = "snowfallMaps")]
        internal byte[][]? _snowfallMaps;

        [JsonProperty(PropertyName = "temperatureMapSummer")]
        internal byte[]? _temperatureMapSummer;

        [JsonProperty(PropertyName = "temperatureMapWinter")]
        internal byte[]? _temperatureMapWinter;

        /// <summary>
        /// Whether any custom precipitation maps, snowfall maps, and temperature maps for both
        /// summer and winter have been supplied for this region.
        /// </summary>
        [JsonIgnore]
        public bool HasAllWeatherMaps
            => _precipitationMaps != null
            && _snowfallMaps != null
            && _temperatureMapSummer != null
            && _temperatureMapWinter != null;

        /// <summary>
        /// Whether any custom precipitation maps, snowfall maps, or temperature maps for either
        /// summer and winter have been supplied for this region.
        /// </summary>
        [JsonIgnore]
        public bool HasAnyWeatherMaps
            => _precipitationMaps != null
            || _snowfallMaps != null
            || _temperatureMapSummer != null
            || _temperatureMapWinter != null;

        /// <summary>
        /// Whether a custom depth map has been supplied for this region.
        /// </summary>
        [JsonIgnore]
        public bool HasDepthMap => _depthMap != null;

        /// <summary>
        /// Whether a custom elevation map has been supplied for this region.
        /// </summary>
        [JsonIgnore]
        public bool HasElevationMap => _elevationMap != null;

        /// <summary>
        /// Whether a custom flor map has been supplied for this region.
        /// </summary>
        [JsonIgnore]
        public bool HasFlowMap => _flowMap != null;

        /// <summary>
        /// Whether a custom depth map and flow map have been supplied for this region.
        /// </summary>
        [JsonIgnore]
        public bool HasHydrologyMaps
            => _depthMap != null
            && _flowMap != null;

        /// <summary>
        /// Whether any custom precipitation maps have been supplied for this region.
        /// </summary>
        [JsonIgnore]
        public bool HasPrecipitationMap => _precipitationMaps != null;

        /// <summary>
        /// Whether any custom snowfall maps have been supplied for this region.
        /// </summary>
        [JsonIgnore]
        public bool HasSnowfallMap => _snowfallMaps != null;

        /// <summary>
        /// Whether a custom summer temperature map has been supplied for this region.
        /// </summary>
        [JsonIgnore]
        public bool HasSummerTemperatureMap => _temperatureMapSummer != null;

        /// <summary>
        /// Whether any custom temperature maps have been supplied for this region.
        /// </summary>
        [JsonIgnore]
        public bool HasTemperatureMap => _temperatureMapSummer != null || _temperatureMapWinter != null;

        /// <summary>
        /// Whether a custom winter temperature map has been supplied for this region.
        /// </summary>
        [JsonIgnore]
        public bool HasWinterTemperatureMap => _temperatureMapWinter != null;

        /// <summary>
        /// Initializes a new instance of <see cref="SurfaceRegion"/>.
        /// </summary>
        /// <param name="planet">The planet on which this region is found.</param>
        /// <param name="position">The normalized position vector of this region.</param>
        /// <param name="latitudeRange">
        /// <para>
        /// The range of latitudes encompassed by this region, as an angle (in radians).
        /// </para>
        /// <para>
        /// Maximum value is π (a full hemisphere, which produces the full globe when combined with
        /// the 2:1 aspect ratio of the equirectangular projection).
        /// </para>
        /// </param>
        public SurfaceRegion(Planetoid planet, Vector3 position, Number latitudeRange)
            : base(planet.Id, new Frustum(2, position * (planet.Shape.ContainingRadius + planet.Atmosphere.AtmosphericHeight), Number.Min(latitudeRange, MathConstants.PI), 0)) { }

        /// <summary>
        /// Initializes a new instance of <see cref="SurfaceRegion"/>.
        /// </summary>
        /// <param name="id">The unique ID of this item.</param>
        /// <param name="shape">The shape of the location.</param>
        /// <param name="parentId">The ID of the location which contains this one.</param>
        /// <param name="depthMap">
        /// A byte array containing an image representing a depth map.
        /// </param>
        /// <param name="elevationMap">
        /// A byte array containing an image representing an elevation map.
        /// </param>
        /// <param name="flowMap">
        /// A byte array containing an image representing a flow map.
        /// </param>
        /// <param name="precipitationMaps">
        /// An array of byte arrays, each containing an image representing a precipitation map.
        /// </param>
        /// <param name="snowfallMaps">
        /// An array of byte arrays, each containing an image representing a snowfall map.
        /// </param>
        /// <param name="temperatureMapSummer">
        /// A byte array containing an image representing a summer temperature map.
        /// </param>
        /// <param name="temperatureMapWinter">
        /// A byte array containing an image representing a winter temperature map.
        /// </param>
        /// <param name="absolutePosition">
        /// <para>
        /// The position of this location, as a set of relative positions starting with the position
        /// of its outermost containing parent within the universe, down to the relative position of
        /// its most immediate parent.
        /// </para>
        /// <para>
        /// The location's own relative <see cref="Location.Position"/> is not expected to be
        /// included.
        /// </para>
        /// <para>
        /// May be <see langword="null"/> for a location with no containing parent, or whose parent
        /// is the universe itself (i.e. there is no intermediate container).
        /// </para>
        /// </param>
        /// <remarks>
        /// Note: this constructor is most useful for deserializers. The other constructors are more
        /// suited to creating a new instance, as they will automatically generate an appropriate
        /// ID.
        /// </remarks>
        [JsonConstructor]
        public SurfaceRegion(
            string id,
            IShape shape,
            string? parentId,
            byte[]? depthMap,
            byte[]? elevationMap,
            byte[]? flowMap,
            byte[][]? precipitationMaps,
            byte[][]? snowfallMaps,
            byte[]? temperatureMapSummer,
            byte[]? temperatureMapWinter,
            Vector3[]? absolutePosition = null) : base(id, shape, parentId, absolutePosition)
        {
            _depthMap = depthMap;
            _elevationMap = elevationMap;
            _flowMap = flowMap;
            _precipitationMaps = precipitationMaps;
            _snowfallMaps = snowfallMaps;
            _temperatureMapSummer = temperatureMapSummer;
            _temperatureMapWinter = temperatureMapWinter;
        }

        private SurfaceRegion(SerializationInfo info, StreamingContext context) : this(
            (string?)info.GetValue(nameof(Id), typeof(string)) ?? string.Empty,
            (IShape?)info.GetValue(nameof(Shape), typeof(IShape)) ?? SinglePoint.Origin,
            (string?)info.GetValue(nameof(ParentId), typeof(string)) ?? string.Empty,
            (byte[]?)info.GetValue(nameof(_depthMap), typeof(byte[])),
            (byte[]?)info.GetValue(nameof(_elevationMap), typeof(byte[])),
            (byte[]?)info.GetValue(nameof(_flowMap), typeof(byte[])),
            (byte[][]?)info.GetValue(nameof(_precipitationMaps), typeof(byte[][])),
            (byte[][]?)info.GetValue(nameof(_snowfallMaps), typeof(byte[][])),
            (byte[]?)info.GetValue(nameof(_temperatureMapSummer), typeof(byte[])),
            (byte[]?)info.GetValue(nameof(_temperatureMapWinter), typeof(byte[])),
            (Vector3[]?)info.GetValue(nameof(AbsolutePosition), typeof(Vector3[])))
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
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(Id), Id);
            info.AddValue(nameof(Shape), Shape);
            info.AddValue(nameof(ParentId), ParentId);
            info.AddValue(nameof(_depthMap), _depthMap);
            info.AddValue(nameof(_elevationMap), _elevationMap);
            info.AddValue(nameof(_flowMap), _flowMap);
            info.AddValue(nameof(_precipitationMaps), _precipitationMaps);
            info.AddValue(nameof(_snowfallMaps), _snowfallMaps);
            info.AddValue(nameof(_temperatureMapSummer), _temperatureMapSummer);
            info.AddValue(nameof(_temperatureMapWinter), _temperatureMapWinter);
            info.AddValue(nameof(AbsolutePosition), AbsolutePosition);
        }

        /// <summary>
        /// Gets the stored hydrology depth map image for this region, if any.
        /// </summary>
        /// <returns>The stored hydrology depth map image for this region, if any.</returns>
        public Bitmap? GetDepthMap() => _depthMap.ToImage();

        /// <summary>
        /// Gets the stored elevation map image for this region, if any.
        /// </summary>
        /// <returns>The stored elevation map image for this region, if any.</returns>
        public Bitmap? GetElevationMap() => _elevationMap.ToImage();

        /// <summary>
        /// Gets the stored hydrology flow map image for this region, if any.
        /// </summary>
        /// <returns>The stored hydrology flow map image for this region, if any.</returns>
        public Bitmap? GetFlowMap() => _flowMap.ToImage();

        /// <summary>
        /// Gets the stored set of precipitation map images for this region, if any.
        /// </summary>
        /// <returns>The stored set of precipitation map images for this region, if any.</returns>
        public Bitmap[] GetPrecipitationMaps()
        {
            if (_precipitationMaps is null)
            {
                return new Bitmap[0];
            }
            var maps = new Bitmap[_precipitationMaps.Length];
            for (var i = 0; i < _precipitationMaps.Length; i++)
            {
                maps[i] = _precipitationMaps[i].ToImage()!;
            }
            return maps;
        }

        /// <summary>
        /// Gets the stored set of snowfall map images for this region, if any.
        /// </summary>
        /// <returns>The stored set of snowfall map images for this region, if any.</returns>
        public Bitmap[] GetSnowfallMaps()
        {
            if (_snowfallMaps is null)
            {
                return new Bitmap[0];
            }
            var maps = new Bitmap[_snowfallMaps.Length];
            for (var i = 0; i < _snowfallMaps.Length; i++)
            {
                maps[i] = _snowfallMaps[i].ToImage()!;
            }
            return maps;
        }

        /// <summary>
        /// Gets the stored temperature map image for this region at the summer solstice, if any.
        /// </summary>
        /// <returns>The stored temperature map image for this region at the summer solstice, if
        /// any.</returns>
        public Bitmap? GetTemperatureMapSummer() => (_temperatureMapSummer ?? _temperatureMapWinter).ToImage();

        /// <summary>
        /// Gets the stored temperature map image for this region at the winter solstice, if any.
        /// </summary>
        /// <returns>The stored temperature map image for this region at the winter solstice, if
        /// any.</returns>
        public Bitmap? GetTemperatureMapWinter() => (_temperatureMapWinter ?? _temperatureMapSummer).ToImage();

        /// <summary>
        /// Loads an image as the hydrology depth map for this region.
        /// </summary>
        /// <param name="image">The image to load.</param>
        public void SetDepthMap(Bitmap image)
        {
            if (image is null)
            {
                _depthMap = null;
                return;
            }
            _depthMap = image.ToByteArray();
        }

        /// <summary>
        /// Loads an image as the elevation map for this region.
        /// </summary>
        /// <param name="image">The image to load.</param>
        public void SetElevationMap(Bitmap image)
        {
            if (image is null)
            {
                _elevationMap = null;
                return;
            }
            _elevationMap = image.ToByteArray();
        }

        /// <summary>
        /// Loads an image as the hydrology flow map for this region.
        /// </summary>
        /// <param name="image">The image to load.</param>
        public void SetFlowMap(Bitmap image)
        {
            if (image is null)
            {
                _flowMap = null;
                return;
            }
            _flowMap = image.ToByteArray();
        }

        /// <summary>
        /// Loads a set of images as the precipitation maps for this region.
        /// </summary>
        /// <param name="images">The images to load. The set is presumed to be evenly spaced over the
        /// course of a year.</param>
        public void SetPrecipitationMaps(IEnumerable<Bitmap> images)
        {
            var list = images.ToList();
            if (list.Count == 0)
            {
                _precipitationMaps = null;
                return;
            }
            _precipitationMaps = new byte[list.Count][];
            for (var i = 0; i < list.Count; i++)
            {
                _precipitationMaps[i] = list[i].ToByteArray()!;
            }
        }

        /// <summary>
        /// Loads a set of images as the snowfall maps for this region.
        /// </summary>
        /// <param name="images">The images to load. The set is presumed to be evenly spaced over the
        /// course of a year.</param>
        public void SetSnowfallMaps(IEnumerable<Bitmap> images)
        {
            var list = images.ToList();
            if (list.Count == 0)
            {
                _snowfallMaps = null;
                return;
            }
            _snowfallMaps = new byte[list.Count][];
            for (var i = 0; i < list.Count; i++)
            {
                _snowfallMaps[i] = list[i].ToByteArray()!;
            }
        }

        /// <summary>
        /// Loads an image as the temperature map for this region, applying the same map to both
        /// summer and winter.
        /// </summary>
        /// <param name="image">The image to load.</param>
        public void SetTemperatureMap(Bitmap image)
        {
            if (image is null)
            {
                _temperatureMapSummer = null;
                _temperatureMapWinter = null;
                return;
            }
            _temperatureMapSummer = image.ToByteArray();
            _temperatureMapWinter = _temperatureMapSummer;
        }

        /// <summary>
        /// Loads an image as the temperature map for this region at the summer solstice.
        /// </summary>
        /// <param name="image">The image to load.</param>
        public void SetTemperatureMapSummer(Bitmap image)
        {
            if (image is null)
            {
                _temperatureMapSummer = null;
                return;
            }
            _temperatureMapSummer = image.ToByteArray();
        }

        /// <summary>
        /// Loads an image as the temperature map for this region at the winter solstice.
        /// </summary>
        /// <param name="image">The image to load.</param>
        public void SetTemperatureMapWinter(Bitmap image)
        {
            if (image is null)
            {
                _temperatureMapWinter = null;
                return;
            }
            _temperatureMapWinter = image.ToByteArray();
        }

        internal float[][] GetDepthMap(int width, int height)
            => _depthMap.ImageToFloatSurfaceMap(width, height);

        internal double[][] GetElevationMap(int width, int height)
            => _elevationMap.ImageToDoubleSurfaceMap(width, height);

        internal float[][] GetFlowMap(int width, int height)
            => _flowMap.ImageToFloatSurfaceMap(width, height);

        internal float[][][] GetPrecipitationMaps(int width, int height)
        {
            var mapImages = GetPrecipitationMaps();
            var maps = new float[mapImages.Length][][];
            for (var i = 0; i < mapImages.Length; i++)
            {
                maps[i] = mapImages[i].ImageToFloatSurfaceMap(width, height);
                mapImages[i].Dispose();
            }
            return maps;
        }

        internal float[][][] GetSnowfallMaps(int width, int height)
        {
            var mapImages = GetSnowfallMaps();
            var maps = new float[mapImages.Length][][];
            for (var i = 0; i < mapImages.Length; i++)
            {
                maps[i] = mapImages[i].ImageToFloatSurfaceMap(width, height);
                mapImages[i].Dispose();
            }
            return maps;
        }

        internal FloatRange[][] GetTemperatureMap(int width, int height)
            => _temperatureMapWinter.ImagesToFloatRangeSurfaceMap(_temperatureMapSummer, width, height);
    }
}
