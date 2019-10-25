using NeverFoundry.MathAndScience.Constants.Numbers;
using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Permissions;
using WorldFoundry.CelestialBodies.Planetoids;
using WorldFoundry.SurfaceMapping;

namespace WorldFoundry.Place
{
    /// <summary>
    /// A <see cref="Location"/> on the surface of a <see cref="Planetoid"/>, which can override
    /// local conditions with manually-specified maps.
    /// </summary>
    [Serializable]
    public class SurfaceRegion : Location
    {
        private byte[]? _depthMap;
        private byte[]? _elevationMap;
        private byte[]? _flowMap;
        private byte[][]? _precipitationMaps;
        private byte[][]? _snowfallMaps;
        private byte[]? _temperatureMapSummer;
        private byte[]? _temperatureMapWinter;

        internal bool HasDepthMap => _depthMap != null;

        internal bool HasElevationMap => _elevationMap != null;

        internal bool HasFlowMap => _flowMap != null;

        internal bool HasHydrologyMaps
            => _depthMap != null
            && _flowMap != null;

        internal bool HasPrecipitationMap => _precipitationMaps != null;

        internal bool HasSnowfallMap => _snowfallMaps != null;

        internal bool HasTemperatureMap => _temperatureMapSummer != null || _temperatureMapWinter != null;

        internal bool HasAllWeatherMaps
            => _precipitationMaps != null
            && _snowfallMaps != null
            && _temperatureMapSummer != null
            && _temperatureMapWinter != null;

        internal bool HasAnyWeatherMaps
            => _precipitationMaps != null
            || _snowfallMaps != null
            || _temperatureMapSummer != null
            || _temperatureMapWinter != null;

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
            : base(new Frustum(2, position * (planet.Shape.ContainingRadius + planet.Atmosphere.AtmosphericHeight), Number.Min(latitudeRange, MathConstants.PI), 0)) { }

        private SurfaceRegion(
            string id,
            IShape shape,
            List<Location>? children,
            byte[]? depthMap,
            byte[]? elevationMap,
            byte[]? flowMap,
            byte[][]? precipitationMaps,
            byte[][]? snowfallMaps,
            byte[]? temperatureMapSummer,
            byte[]? temperatureMapWinter) : base(id, shape, children)
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
            (string)info.GetValue(nameof(Id), typeof(string)),
            (IShape)info.GetValue(nameof(Shape), typeof(IShape)),
            (List<Location>)info.GetValue(nameof(Children), typeof(List<Location>)),
            (byte[]?)info.GetValue(nameof(_depthMap), typeof(byte[])),
            (byte[]?)info.GetValue(nameof(_elevationMap), typeof(byte[])),
            (byte[]?)info.GetValue(nameof(_flowMap), typeof(byte[])),
            (byte[][]?)info.GetValue(nameof(_precipitationMaps), typeof(byte[][])),
            (byte[][]?)info.GetValue(nameof(_snowfallMaps), typeof(byte[][])),
            (byte[]?)info.GetValue(nameof(_temperatureMapSummer), typeof(byte[])),
            (byte[]?)info.GetValue(nameof(_temperatureMapWinter), typeof(byte[]))) { }

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
            info.AddValue(nameof(Children), Children.ToList());
            info.AddValue(nameof(_depthMap), _depthMap);
            info.AddValue(nameof(_elevationMap), _elevationMap);
            info.AddValue(nameof(_flowMap), _flowMap);
            info.AddValue(nameof(_precipitationMaps), _precipitationMaps);
            info.AddValue(nameof(_snowfallMaps), _snowfallMaps);
            info.AddValue(nameof(_temperatureMapSummer), _temperatureMapSummer);
            info.AddValue(nameof(_temperatureMapWinter), _temperatureMapWinter);
        }

        internal static float[,] GetMapFromImage(byte[]? bytes, int width, int height)
        {
            using var image = GetMapImage(bytes);
            if (image is null)
            {
                return new float[width, height];
            }
            return image.ImageToFloatSurfaceMap(width, height);
        }

        internal static Bitmap? GetMapImage(byte[]? bytes)
        {
            if (bytes is null)
            {
                return null;
            }
            using var stream = new MemoryStream(bytes);
            return new Bitmap(stream);
        }

        private static byte[] GetByteArray(Bitmap image)
        {
            using var stream = new MemoryStream();
            image.Save(stream, System.Drawing.Imaging.ImageFormat.Bmp);
            return stream.ToArray();
        }

        /// <summary>
        /// Gets the stored hydrology depth map image for this region, if any.
        /// </summary>
        /// <returns>The stored hydrology depth map image for this region, if any.</returns>
        public Bitmap? GetDepthMap() => GetMapImage(_depthMap);

        /// <summary>
        /// Gets the stored elevation map image for this region, if any.
        /// </summary>
        /// <returns>The stored elevation map image for this region, if any.</returns>
        public Bitmap? GetElevationMap() => GetMapImage(_elevationMap);

        /// <summary>
        /// Gets the stored hydrology flow map image for this region, if any.
        /// </summary>
        /// <returns>The stored hydrology flow map image for this region, if any.</returns>
        public Bitmap? GetFlowMap() => GetMapImage(_flowMap);

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
                maps[i] = GetMapImage(_precipitationMaps[i])!;
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
                maps[i] = GetMapImage(_snowfallMaps[i])!;
            }
            return maps;
        }

        /// <summary>
        /// Gets the stored temperature map image for this region at the summer solstice, if any.
        /// </summary>
        /// <returns>The stored temperature map image for this region at the summer solstice, if
        /// any.</returns>
        public Bitmap? GetTemperatureMapSummer() => GetMapImage(_temperatureMapSummer ?? _temperatureMapWinter);

        /// <summary>
        /// Gets the stored temperature map image for this region at the winter solstice, if any.
        /// </summary>
        /// <returns>The stored temperature map image for this region at the winter solstice, if
        /// any.</returns>
        public Bitmap? GetTemperatureMapWinter() => GetMapImage(_temperatureMapWinter ?? _temperatureMapSummer);

        /// <summary>
        /// Loads an image as the hydrology depth map for this region.
        /// </summary>
        /// <param name="image">The image to load.</param>
        public void LoadDepthMap(Bitmap image)
        {
            if (image is null)
            {
                _depthMap = null;
                return;
            }
            _depthMap = GetByteArray(image);
        }

        /// <summary>
        /// Loads an image as the elevation map for this region.
        /// </summary>
        /// <param name="image">The image to load.</param>
        public void LoadElevationMap(Bitmap image)
        {
            if (image is null)
            {
                _elevationMap = null;
                return;
            }
            _elevationMap = GetByteArray(image);
        }

        /// <summary>
        /// Loads an image as the hydrology flow map for this region.
        /// </summary>
        /// <param name="image">The image to load.</param>
        public void LoadFlowMap(Bitmap image)
        {
            if (image is null)
            {
                _flowMap = null;
                return;
            }
            _flowMap = GetByteArray(image);
        }

        /// <summary>
        /// Loads a set of images as the precipitation maps for this region.
        /// </summary>
        /// <param name="images">The images to load. The set is presumed to be evenly spaced over the
        /// course of a year.</param>
        public void LoadPrecipitationMaps(IEnumerable<Bitmap> images)
        {
            if (images?.Any() != true)
            {
                _precipitationMaps = null;
                return;
            }
            _precipitationMaps = images.Select(GetByteArray).ToArray();
        }

        /// <summary>
        /// Loads a set of images as the snowfall maps for this region.
        /// </summary>
        /// <param name="images">The images to load. The set is presumed to be evenly spaced over the
        /// course of a year.</param>
        public void LoadSnowfallMaps(IEnumerable<Bitmap> images)
        {
            if (images?.Any() != true)
            {
                _snowfallMaps = null;
                return;
            }
            _snowfallMaps = images.Select(GetByteArray).ToArray();
        }

        /// <summary>
        /// Loads an image as the temperature map for this region, applying the same map to both
        /// summer and winter.
        /// </summary>
        /// <param name="image">The image to load.</param>
        public void LoadTemperatureMap(Bitmap image)
        {
            if (image is null)
            {
                _temperatureMapSummer = null;
                _temperatureMapWinter = null;
                return;
            }
            _temperatureMapSummer = GetByteArray(image);
            _temperatureMapWinter = _temperatureMapSummer;
        }

        /// <summary>
        /// Loads an image as the temperature map for this region at the summer solstice.
        /// </summary>
        /// <param name="image">The image to load.</param>
        public void LoadTemperatureMapSummer(Bitmap image)
        {
            if (image is null)
            {
                _temperatureMapSummer = null;
                return;
            }
            _temperatureMapSummer = GetByteArray(image);
        }

        /// <summary>
        /// Loads an image as the temperature map for this region at the winter solstice.
        /// </summary>
        /// <param name="image">The image to load.</param>
        public void LoadTemperatureMapWinter(Bitmap image)
        {
            if (image is null)
            {
                _temperatureMapWinter = null;
                return;
            }
            _temperatureMapWinter = GetByteArray(image);
        }

        internal float[,] GetDepthMap(int width, int height)
            => GetMapFromImage(_depthMap, width, height);

        internal double[,] GetElevationMap(int width, int height)
        {
            using var image = GetMapImage(_elevationMap);
            if (image is null)
            {
                return new double[width, height];
            }
            return image.ImageToDoubleSurfaceMap(width, height);
        }

        internal float[,] GetFlowMap(int width, int height)
            => GetMapFromImage(_flowMap, width, height);

        internal float[][,] GetPrecipitationMaps(int width, int height)
        {
            var mapImages = GetPrecipitationMaps();
            var maps = new float[mapImages.Length][,];
            for (var i = 0; i < mapImages.Length; i++)
            {
                maps[i] = mapImages[i].ImageToFloatSurfaceMap(width, height);
                mapImages[i].Dispose();
            }
            return maps;
        }

        internal float[][,] GetSnowfallMaps(int width, int height)
        {
            var mapImages = GetSnowfallMaps();
            var maps = new float[mapImages.Length][,];
            for (var i = 0; i < mapImages.Length; i++)
            {
                maps[i] = mapImages[i].ImageToFloatSurfaceMap(width, height);
                mapImages[i].Dispose();
            }
            return maps;
        }

        internal FloatRange[,] GetTemperatureMap(int width, int height)
        {
            using var winter = GetTemperatureMapWinter();
            using var summer = GetTemperatureMapSummer();
            if (winter is null || summer is null)
            {
                return new FloatRange[width, height];
            }
            return winter.ImagesToFloatRangeSurfaceMap(summer, width, height);
        }
    }
}
