using MathAndScience.Shapes;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using WorldFoundry.CelestialBodies.Planetoids;

namespace WorldFoundry.Place
{
    /// <summary>
    /// A <see cref="Region"/> on the surface of a <see cref="Planetoid"/>, which can override local
    /// conditions with manually-specified maps.
    /// </summary>
    public class SurfaceRegion : Region
    {
        internal byte[] _depthOverlay;
        internal int _depthOverlayHeight;
        internal int _depthOverlayWidth;
        internal byte[] _elevationOverlay;
        internal int _elevationOverlayHeight;
        internal int _elevationOverlayWidth;
        internal byte[] _flowOverlay;
        internal int _flowOverlayHeight;
        internal int _flowOverlayWidth;
        internal byte[][] _precipitationOverlays;
        internal int _precipitationOverlayHeight;
        internal int _precipitationOverlayWidth;
        internal byte[][] _snowfallOverlays;
        internal int _snowfallOverlayHeight;
        internal int _snowfallOverlayWidth;
        internal byte[] _temperatureOverlaySummer;
        internal int _temperatureOverlayHeightSummer;
        internal int _temperatureOverlayWidthSummer;
        internal byte[] _temperatureOverlayWinter;
        internal int _temperatureOverlayHeightWinter;
        internal int _temperatureOverlayWidthWinter;

        /// <summary>
        /// Initializes a new instance of <see cref="SurfaceRegion"/>.
        /// </summary>
        private protected SurfaceRegion() { }

        /// <summary>
        /// Initializes a new instance of <see cref="SurfaceRegion"/>.
        /// </summary>
        /// <param name="shape">The shape of the region.</param>
        public SurfaceRegion(IShape shape) : base(shape) { }

        /// <summary>
        /// Initializes a new instance of <see cref="SurfaceRegion"/>.
        /// </summary>
        /// <param name="containingRegion">The region in which this one is found.</param>
        /// <param name="shape">The shape of the region.</param>
        public SurfaceRegion(Region containingRegion, IShape shape) : base(containingRegion, shape) { }

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
