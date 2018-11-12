using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;

namespace WorldFoundry.SurfaceMapping
{
    /// <summary>
    /// Static methods related to images with surface map data.
    /// </summary>
    public static class SurfaceMapImage
    {
        /// <summary>
        /// Converts an image to a surface map.
        /// </summary>
        /// <param name="image">The image to convert.</param>
        /// <param name="zeroNormal"><see langword="true"/> if the map's values range from 0 to 1
        /// (the default, true for most maps); <see langword="false"/> of they range from -1 to 1
        /// (the case for elevation maps, or overlays).</param>
        /// <returns>A surface map.</returns>
        public static float[,] ImageToSurfaceMap(this Image<Rgba32> image, bool zeroNormal = true)
        {
            var surfaceMap = new float[image.Width, image.Height];
            for (var y = 0; y < image.Height; y++)
            {
                var pixelRow = image.GetPixelRowSpan(y);
                for (var x = 0; x < image.Width; x++)
                {
                    surfaceMap[x, y] = Rgba32ToFloat(pixelRow[x], zeroNormal);
                }
            }
            return surfaceMap;
        }

        /// <summary>
        /// Converts an image to a surface map overlay.
        /// </summary>
        /// <param name="image">The image to convert.</param>
        /// <param name="destWidth">An optional width to which the image will be stretched. If left
        /// <see langword="null"/> the source width will be retained.</param>
        /// <param name="destHeight">An optional height to which the image will be stretched. If
        /// left <see langword="null"/> the source height will be retained.</param>
        /// <returns>A surface map overlay.</returns>
        public static float[,] ImageToOverlay(this Image<Rgba32> image, int? destWidth = null, int? destHeight = null)
        {
            if (destWidth.HasValue || destHeight.HasValue)
            {
                image = image.Clone(x => x.Resize(destWidth ?? image.Width, destHeight ?? image.Height));
            }
            var surfaceMap = new float[image.Width, image.Height];
            for (var y = 0; y < image.Height; y++)
            {
                var pixelRow = image.GetPixelRowSpan(y);
                for (var x = 0; x < image.Width; x++)
                {
                    surfaceMap[x, y] = Rgba32ToFloat(pixelRow[x], false);
                }
            }
            return surfaceMap;
        }

        /// <summary>
        /// Get a composite (flattened) surface map from a base map and an overlay.
        /// </summary>
        /// <param name="baseMap">A surface map.</param>
        /// <param name="overlay">A surface map overlay.</param>
        /// <param name="zeroNormal"><see langword="true"/> if <paramref name="baseMap"/>'s values
        /// range from 0 to 1 (the default, true for most maps); <see langword="false"/> of they
        /// range from -1 to 1 (the case for elevation maps).</param>
        /// <returns>A composite (flattened) surface map.</returns>
        /// <exception cref="ArgumentException">The maps must have the same dimensions.</exception>
        public static float[,] GetCompositeSurfaceMap(float[,] baseMap, float[,] overlay, bool zeroNormal = true)
        {
            var xLength = baseMap.GetLength(0);
            var yLength = baseMap.GetLength(1);
            if (overlay.GetLength(0) != xLength || overlay.GetLength(1) != yLength)
            {
                throw new ArgumentException($"{nameof(overlay)} must have the same dimensions as {nameof(baseMap)}.");
            }
            var final = new float[xLength, yLength];
            for (var x = 0; x < xLength; x++)
            {
                for (var y = 0; y < yLength; y++)
                {
                    // Overlay value is doubled for a map with a -1 to 1 range, since it's modifying
                    // a value whose range is twice that of a normal map. This allows, e.g.,
                    // modifying a 1 all the way down to a -1.
                    var mod = zeroNormal ? overlay[x, y] : overlay[x, y] * 2;
                    final[x, y] = baseMap[x, y] + mod;
                }
            }
            return final;
        }

        /// <summary>
        /// Get a composite (flattened) surface map from a base map and an overlay.
        /// </summary>
        /// <param name="baseMap">A surface map.</param>
        /// <param name="overlayMin">A surface map overlay for the minimum values.</param>
        /// <param name="overlayMax">A surface map overlay for the maximum values.</param>
        /// <param name="zeroNormal"><see langword="true"/> if <paramref name="baseMap"/>'s values
        /// range from 0 to 1 (the default, true for most maps); <see langword="false"/> of they
        /// range from -1 to 1 (the case for elevation maps).</param>
        /// <returns>A composite (flattened) surface map.</returns>
        /// <exception cref="ArgumentException">The maps must have the same dimensions.</exception>
        public static FloatRange[,] GetCompositeSurfaceMap(FloatRange[,] baseMap, float[,] overlayMin, float[,] overlayMax, bool zeroNormal = true)
        {
            var xLength = baseMap.GetLength(0);
            var yLength = baseMap.GetLength(1);
            if (overlayMin.GetLength(0) != xLength || overlayMin.GetLength(1) != yLength
                || overlayMax.GetLength(0) != xLength || overlayMax.GetLength(1) != yLength)
            {
                throw new ArgumentException($"{nameof(overlayMin)} and {nameof(overlayMax)} must have the same dimensions as {nameof(baseMap)}.");
            }
            var final = new FloatRange[xLength, yLength];
            for (var x = 0; x < xLength; x++)
            {
                for (var y = 0; y < yLength; y++)
                {
                    // Overlay values are doubled for a map with a -1 to 1 range, since they're
                    // modifying a value whose range is twice that of a normal map. This allows,
                    // e.g., modifying a 1 all the way down to a -1.
                    var modMin = zeroNormal ? overlayMin[x, y] : overlayMin[x, y] * 2;
                    var modMax = zeroNormal ? overlayMax[x, y] : overlayMax[x, y] * 2;
                    final[x, y] = new FloatRange(baseMap[x, y].Min + modMin, baseMap[x, y].Max + modMax);
                }
            }
            return final;
        }

        /// <summary>
        /// Converts a surface map to a grayscale image.
        /// </summary>
        /// <param name="surfaceMap">The surface map to convert.</param>
        /// <param name="zeroNormal"><see langword="true"/> if the map's values range from 0 to 1
        /// (the default, true for most maps); <see langword="false"/> of they range from -1 to 1
        /// (the case for elevation maps).</param>
        /// <returns>A grayscale image.</returns>
        public static Image<Rgba32> SurfaceMapToImage(this float[,] surfaceMap, bool zeroNormal = true)
        {
            var xLength = surfaceMap.GetLength(0);
            var yLength = surfaceMap.GetLength(1);
            using (var image = new Image<Rgba32>(xLength, yLength))
            {
                for (var y = 0; y < xLength; y++)
                {
                    var pixelRow = image.GetPixelRowSpan(y);
                    for (var x = 0; x < xLength; x++)
                    {
                        pixelRow[x] = FloatToRgba32(surfaceMap[x, y], zeroNormal);
                    }
                }
                return image;
            }
        }

        private static Rgba32 FloatToRgba32(float value, bool zeroNormal = true)
        {
            var normalized = zeroNormal ? value : (value + 1) / 2;
            return new Rgba32(normalized, normalized, normalized, 1);
        }

        private static float Rgba32ToFloat(Rgba32 value, bool zeroNormal = true)
        {
            var avg = (value.R + value.G + value.B) / 3f;
            var result = avg / 255;
            if (!zeroNormal)
            {
                result = (result * 2) - 1;
            }
            return result;
        }
    }
}
