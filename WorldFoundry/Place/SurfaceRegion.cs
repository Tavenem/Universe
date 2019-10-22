using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.Permissions;
using WorldFoundry.CelestialBodies.Planetoids;

namespace WorldFoundry.Place
{
    /// <summary>
    /// A <see cref="Location"/> on the surface of a <see cref="Planetoid"/>, which can override
    /// local conditions with manually-specified maps.
    /// </summary>
    [Serializable]
    public class SurfaceRegion : Location
    {
        internal byte[]? _depthOverlay;
        internal int _depthOverlayHeight;
        internal int _depthOverlayWidth;
        internal byte[]? _elevationOverlay;
        internal int _elevationOverlayHeight;
        internal int _elevationOverlayWidth;
        internal byte[]? _flowOverlay;
        internal int _flowOverlayHeight;
        internal int _flowOverlayWidth;
        internal byte[][]? _precipitationOverlays;
        internal int _precipitationOverlayHeight;
        internal int _precipitationOverlayWidth;
        internal byte[][]? _snowfallOverlays;
        internal int _snowfallOverlayHeight;
        internal int _snowfallOverlayWidth;
        internal byte[]? _temperatureOverlaySummer;
        internal int _temperatureOverlayHeightSummer;
        internal int _temperatureOverlayWidthSummer;
        internal byte[]? _temperatureOverlayWinter;
        internal int _temperatureOverlayHeightWinter;
        internal int _temperatureOverlayWidthWinter;

        /// <summary>
        /// Initializes a new instance of <see cref="SurfaceRegion"/>.
        /// </summary>
        /// <param name="planet">The planet on which this region is found.</param>
        /// <param name="position">The normalized position vector of this region.</param>
        /// <param name="radius">The radius of this region, in meters.</param>
        public SurfaceRegion(Planetoid planet, Vector3 position, Number radius) : base(new Sphere(radius, position * planet.Shape.ContainingRadius)) { }

        private SurfaceRegion(
            string id,
            IShape shape,
            List<Location>? children,
            byte[]? depthOverlay,
            int depthOverlayHeight,
            int depthOverlayWidth,
            byte[]? elevationOverlay,
            int elevationOverlayHeight,
            int elevationOverlayWidth,
            byte[]? flowOverlay,
            int flowOverlayHeight,
            int flowOverlayWidth,
            byte[][]? precipitationOverlays,
            int precipitationOverlayHeight,
            int precipitationOverlayWidth,
            byte[][]? snowfallOverlays,
            int snowfallOverlayHeight,
            int snowfallOverlayWidth,
            byte[]? temperatureOverlaySummer,
            int temperatureOverlayHeightSummer,
            int temperatureOverlayWidthSummer,
            byte[]? temperatureOverlayWinter,
            int temperatureOverlayHeightWinter,
            int temperatureOverlayWidthWinter) : base(id, shape, children)
        {
            _depthOverlay = depthOverlay;
            _depthOverlayHeight = depthOverlayHeight;
            _depthOverlayWidth = depthOverlayWidth;
            _elevationOverlay = elevationOverlay;
            _elevationOverlayHeight = elevationOverlayHeight;
            _elevationOverlayWidth = elevationOverlayWidth;
            _flowOverlay = flowOverlay;
            _flowOverlayHeight = flowOverlayHeight;
            _flowOverlayWidth = flowOverlayWidth;
            _precipitationOverlays = precipitationOverlays;
            _precipitationOverlayHeight = precipitationOverlayHeight;
            _precipitationOverlayWidth = precipitationOverlayWidth;
            _snowfallOverlays = snowfallOverlays;
            _snowfallOverlayHeight = snowfallOverlayHeight;
            _snowfallOverlayWidth = snowfallOverlayWidth;
            _temperatureOverlaySummer = temperatureOverlaySummer;
            _temperatureOverlayHeightSummer = temperatureOverlayHeightSummer;
            _temperatureOverlayWidthSummer = temperatureOverlayWidthSummer;
            _temperatureOverlayWinter = temperatureOverlayWinter;
            _temperatureOverlayHeightWinter = temperatureOverlayHeightWinter;
            _temperatureOverlayWidthWinter = temperatureOverlayWidthWinter;
        }

        private SurfaceRegion(SerializationInfo info, StreamingContext context) : this(
            (string)info.GetValue(nameof(Id), typeof(string)),
            (IShape)info.GetValue(nameof(Shape), typeof(IShape)),
            (List<Location>)info.GetValue(nameof(Children), typeof(List<Location>)),
            (byte[]?)info.GetValue(nameof(_depthOverlay), typeof(byte[])),
            (int)info.GetValue(nameof(_depthOverlayHeight), typeof(int)),
            (int)info.GetValue(nameof(_depthOverlayWidth), typeof(int)),
            (byte[]?)info.GetValue(nameof(_elevationOverlay), typeof(byte[])),
            (int)info.GetValue(nameof(_elevationOverlayHeight), typeof(int)),
            (int)info.GetValue(nameof(_elevationOverlayWidth), typeof(int)),
            (byte[]?)info.GetValue(nameof(_flowOverlay), typeof(byte[])),
            (int)info.GetValue(nameof(_flowOverlayHeight), typeof(int)),
            (int)info.GetValue(nameof(_flowOverlayWidth), typeof(int)),
            (byte[][]?)info.GetValue(nameof(_precipitationOverlays), typeof(byte[][])),
            (int)info.GetValue(nameof(_precipitationOverlayHeight), typeof(int)),
            (int)info.GetValue(nameof(_precipitationOverlayWidth), typeof(int)),
            (byte[][]?)info.GetValue(nameof(_snowfallOverlays), typeof(byte[][])),
            (int)info.GetValue(nameof(_snowfallOverlayHeight), typeof(int)),
            (int)info.GetValue(nameof(_snowfallOverlayWidth), typeof(int)),
            (byte[]?)info.GetValue(nameof(_temperatureOverlaySummer), typeof(byte[])),
            (int)info.GetValue(nameof(_temperatureOverlayHeightSummer), typeof(int)),
            (int)info.GetValue(nameof(_temperatureOverlayWidthSummer), typeof(int)),
            (byte[]?)info.GetValue(nameof(_temperatureOverlayWinter), typeof(byte[])),
            (int)info.GetValue(nameof(_temperatureOverlayHeightWinter), typeof(int)),
            (int)info.GetValue(nameof(_temperatureOverlayWidthWinter), typeof(int))) { }

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
            info.AddValue(nameof(_depthOverlay), _depthOverlay);
            info.AddValue(nameof(_depthOverlayHeight), _depthOverlayHeight);
            info.AddValue(nameof(_depthOverlayWidth), _depthOverlayWidth);
            info.AddValue(nameof(_elevationOverlay), _elevationOverlay);
            info.AddValue(nameof(_elevationOverlayHeight), _elevationOverlayHeight);
            info.AddValue(nameof(_elevationOverlayWidth), _elevationOverlayWidth);
            info.AddValue(nameof(_flowOverlay), _flowOverlay);
            info.AddValue(nameof(_flowOverlayHeight), _flowOverlayHeight);
            info.AddValue(nameof(_flowOverlayWidth), _flowOverlayWidth);
            info.AddValue(nameof(_precipitationOverlays), _precipitationOverlays);
            info.AddValue(nameof(_precipitationOverlayHeight), _precipitationOverlayHeight);
            info.AddValue(nameof(_precipitationOverlayWidth), _precipitationOverlayWidth);
            info.AddValue(nameof(_snowfallOverlays), _snowfallOverlays);
            info.AddValue(nameof(_snowfallOverlayHeight), _snowfallOverlayHeight);
            info.AddValue(nameof(_snowfallOverlayWidth), _snowfallOverlayWidth);
            info.AddValue(nameof(_temperatureOverlaySummer), _temperatureOverlaySummer);
            info.AddValue(nameof(_temperatureOverlayHeightSummer), _temperatureOverlayHeightSummer);
            info.AddValue(nameof(_temperatureOverlayWidthSummer), _temperatureOverlayWidthSummer);
            info.AddValue(nameof(_temperatureOverlayWinter), _temperatureOverlayWinter);
            info.AddValue(nameof(_temperatureOverlayHeightWinter), _temperatureOverlayHeightWinter);
            info.AddValue(nameof(_temperatureOverlayWidthWinter), _temperatureOverlayWidthWinter);
        }

        /// <summary>
        /// Loads an image as the hydrology depth overlay for this region.
        /// </summary>
        /// <param name="image">The image to load.</param>
        public void LoadDepthOverlay(Image<Rgba32> image)
        {
            if (image == null)
            {
                _depthOverlay = null;
                return;
            }
            _depthOverlayHeight = image.Height;
            _depthOverlayWidth = image.Width;
            _depthOverlay = MemoryMarshal.AsBytes(image.GetPixelSpan()).ToArray();
        }

        /// <summary>
        /// Loads an image as the elevation overlay for this region.
        /// </summary>
        /// <param name="image">The image to load.</param>
        public void LoadElevationOverlay(Image<Rgba32> image)
        {
            if (image == null)
            {
                _elevationOverlay = null;
                return;
            }
            _elevationOverlayHeight = image.Height;
            _elevationOverlayWidth = image.Width;
            _elevationOverlay = MemoryMarshal.AsBytes(image.GetPixelSpan()).ToArray();
        }

        /// <summary>
        /// Loads an image as the hydrology flow overlay for this region.
        /// </summary>
        /// <param name="image">The image to load.</param>
        public void LoadFlowOverlay(Image<Rgba32> image)
        {
            if (image == null)
            {
                _flowOverlay = null;
                return;
            }
            _flowOverlayHeight = image.Height;
            _flowOverlayWidth = image.Width;
            _flowOverlay = MemoryMarshal.AsBytes(image.GetPixelSpan()).ToArray();
        }

        /// <summary>
        /// Loads a set of images as the precipitation overlays for this region.
        /// </summary>
        /// <param name="images">The images to load. The set is presumed to be evenly spaced over the
        /// course of a year.</param>
        public void LoadPrecipitationOverlays(IEnumerable<Image<Rgba32>> images)
        {
            if (!images.Any())
            {
                _precipitationOverlays = null;
                return;
            }
            var first = images.First();
            _precipitationOverlayHeight = first.Height;
            _precipitationOverlayWidth = first.Width;
            _precipitationOverlays = images.Select(x => MemoryMarshal.AsBytes(x.GetPixelSpan()).ToArray()).ToArray();
        }

        /// <summary>
        /// Loads a set of images as the snowfall overlays for this region.
        /// </summary>
        /// <param name="images">The images to load. The set is presumed to be evenly spaced over the
        /// course of a year.</param>
        public void LoadSnowfallOverlays(IEnumerable<Image<Rgba32>> images)
        {
            if (!images.Any())
            {
                _snowfallOverlays = null;
                return;
            }
            var first = images.First();
            _snowfallOverlayHeight = first.Height;
            _snowfallOverlayWidth = first.Width;
            _snowfallOverlays = images.Select(x => MemoryMarshal.AsBytes(x.GetPixelSpan()).ToArray()).ToArray();
        }

        /// <summary>
        /// Loads an image as the temperature overlay for this region, applying the same map to both
        /// summer and winter.
        /// </summary>
        /// <param name="image">The image to load.</param>
        public void LoadTemperatureOverlay(Image<Rgba32> image)
        {
            if (image == null)
            {
                _temperatureOverlaySummer = null;
                _temperatureOverlayWinter = null;
                return;
            }
            _temperatureOverlayHeightSummer = image.Height;
            _temperatureOverlayWidthSummer = image.Width;
            _temperatureOverlaySummer = MemoryMarshal.AsBytes(image.GetPixelSpan()).ToArray();
            _temperatureOverlayHeightWinter = image.Height;
            _temperatureOverlayWidthWinter = image.Width;
            _temperatureOverlayWinter = _temperatureOverlaySummer;
        }

        /// <summary>
        /// Loads an image as the temperature overlay for this region at the summer solstice.
        /// </summary>
        /// <param name="image">The image to load.</param>
        public void LoadTemperatureOverlaySummer(Image<Rgba32> image)
        {
            if (image == null)
            {
                _temperatureOverlaySummer = null;
                return;
            }
            _temperatureOverlayHeightSummer = image.Height;
            _temperatureOverlayWidthSummer = image.Width;
            _temperatureOverlaySummer = MemoryMarshal.AsBytes(image.GetPixelSpan()).ToArray();
        }

        /// <summary>
        /// Loads an image as the temperature overlay for this region at the winter solstice.
        /// </summary>
        /// <param name="image">The image to load.</param>
        public void LoadTemperatureOverlayWinter(Image<Rgba32> image)
        {
            if (image == null)
            {
                _temperatureOverlayWinter = null;
                return;
            }
            _temperatureOverlayHeightWinter = image.Height;
            _temperatureOverlayWidthWinter = image.Width;
            _temperatureOverlayWinter = MemoryMarshal.AsBytes(image.GetPixelSpan()).ToArray();
        }
    }
}
