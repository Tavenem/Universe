using NeverFoundry.MathAndScience;
using NeverFoundry.MathAndScience.Numerics.Decimals;
using NeverFoundry.WorldFoundry.Climate;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace NeverFoundry.WorldFoundry.SurfaceMapping
{
    /// <summary>
    /// Static methods related to images with surface map data.
    /// </summary>
    public static class SurfaceMapImage
    {
        /// <summary>
        /// <para>
        /// Converts an elevation map to an image.
        /// </para>
        /// <para>
        /// Positive values are rendered as a reddish color, with 0 being rendered as white and 1
        /// being rendered as a dark red. Negative values are rendered as a bluish color, with 0
        /// being rendered as white and 1 being rendered as a deep blue.
        /// </para>
        /// <para>
        /// The color scale is logarithmic, in order to better emphasize differences at low
        /// elevations without drowning out differences at extremes.
        /// </para>
        /// </summary>
        /// <param name="elevationMap">The elevation map to convert.</param>
        /// <returns>An image.</returns>
        public static Bitmap ElevationMapToImage(this double[][] elevationMap) => SurfaceMapToImage(elevationMap, ToElevationColor);

        /// <summary>
        /// Converts an image to a surface map.
        /// </summary>
        /// <param name="image">The image to convert.</param>
        /// <param name="max">Then this method returns, will be set to the maximum value in the map.</param>
        /// <param name="destWidth">An optional width to which the image will be stretched. If left
        /// <see langword="null"/> the source width will be retained.</param>
        /// <param name="destHeight">An optional height to which the image will be stretched. If
        /// left <see langword="null"/> the source height will be retained.</param>
        /// <returns>A surface map.</returns>
        public static double[][] ImageToDoubleSurfaceMap(this Bitmap? image, out double max, int? destWidth = null, int? destHeight = null)
        {
            max = 0;
            if (image is null)
            {
                return new double[0][];
            }
            max = -1;

            var img = image;
            var resized = false;
            if (destWidth.HasValue || destHeight.HasValue)
            {
                img = image.Resize(destWidth ?? image.Width, destHeight ?? image.Height);
                resized = true;
            }
            var surfaceMap = new double[img.Width][];
            for (var i = 0; i < surfaceMap.Length; i++)
            {
                surfaceMap[i] = new double[img.Height];
            }

            var data = img.LockBits(new Rectangle(0, 0, img.Width, img.Height), ImageLockMode.ReadOnly, img.PixelFormat);
            var pixelSize = Image.GetPixelFormatSize(data.PixelFormat) / 8;
            var length = data.Height * data.Stride;
            var bytes = new byte[length];
            System.Runtime.InteropServices.Marshal.Copy(data.Scan0, bytes, 0, length);

            byte r, g, b;
            for (var y = 0; y < img.Height; y++)
            {
                for (var x = 0; x < img.Width; x++)
                {
                    var pos = (y * data.Stride) + (x * pixelSize);
                    if (pixelSize == 4)
                    {
                        if (BitConverter.IsLittleEndian)
                        {
                            b = bytes[pos + 1];
                            g = bytes[pos + 2];
                            r = bytes[pos + 3];
                        }
                        else
                        {
                            r = bytes[pos];
                            g = bytes[pos + 1];
                            b = bytes[pos + 2];
                        }
                    }
                    else if (BitConverter.IsLittleEndian)
                    {
                        b = bytes[pos];
                        g = bytes[pos + 1];
                        r = bytes[pos + 2];
                    }
                    else
                    {
                        r = bytes[pos];
                        g = bytes[pos + 1];
                        b = bytes[pos + 2];
                    }
                    surfaceMap[x][y] = RgbToDouble(r, g, b);
                    max = Math.Max(max, Math.Abs(surfaceMap[x][y]));
                }
            }

            img.UnlockBits(data);
            if (resized)
            {
                img.Dispose();
            }

            return surfaceMap;
        }

        /// <summary>
        /// Converts a byte array containing an image to a surface map.
        /// </summary>
        /// <param name="bytes">The byte array containing an image to convert.</param>
        /// <param name="max">Then this method returns, will be set to the maximum value in the map.</param>
        /// <param name="destWidth">An optional width to which the image will be stretched. If left
        /// <see langword="null"/> the source width will be retained.</param>
        /// <param name="destHeight">An optional height to which the image will be stretched. If
        /// left <see langword="null"/> the source height will be retained.</param>
        /// <returns>A surface map.</returns>
        public static double[][] ImageToDoubleSurfaceMap(this byte[]? bytes, out double max, int? destWidth = null, int? destHeight = null)
            => ImageToDoubleSurfaceMap(ToImage(bytes), out max, destWidth, destHeight);

        /// <summary>
        /// Converts an image to a surface map.
        /// </summary>
        /// <param name="image">The image to convert.</param>
        /// <param name="destWidth">An optional width to which the image will be stretched. If left
        /// <see langword="null"/> the source width will be retained.</param>
        /// <param name="destHeight">An optional height to which the image will be stretched. If
        /// left <see langword="null"/> the source height will be retained.</param>
        /// <returns>A surface map.</returns>
        public static float[][] ImageToFloatSurfaceMap(this Bitmap? image, int? destWidth = null, int? destHeight = null)
        {
            if (image is null)
            {
                return new float[0][];
            }

            var img = image;
            var resized = false;
            if (destWidth.HasValue || destHeight.HasValue)
            {
                img = image.Resize(destWidth ?? image.Width, destHeight ?? image.Height);
                resized = true;
            }
            var surfaceMap = new float[img.Width][];
            for (var i = 0; i < surfaceMap.Length; i++)
            {
                surfaceMap[i] = new float[img.Height];
            }

            var data = img.LockBits(new Rectangle(0, 0, img.Width, img.Height), ImageLockMode.ReadOnly, img.PixelFormat);
            var pixelSize = Image.GetPixelFormatSize(data.PixelFormat) / 8;
            var length = data.Height * data.Stride;
            var bytes = new byte[length];
            System.Runtime.InteropServices.Marshal.Copy(data.Scan0, bytes, 0, length);

            byte r, g, b;
            for (var y = 0; y < img.Height; y++)
            {
                for (var x = 0; x < img.Width; x++)
                {
                    var pos = (y * data.Stride) + (x * pixelSize);
                    if (pixelSize == 4)
                    {
                        if (BitConverter.IsLittleEndian)
                        {
                            b = bytes[pos + 1];
                            g = bytes[pos + 2];
                            r = bytes[pos + 3];
                        }
                        else
                        {
                            r = bytes[pos];
                            g = bytes[pos + 1];
                            b = bytes[pos + 2];
                        }
                    }
                    else if (BitConverter.IsLittleEndian)
                    {
                        b = bytes[pos];
                        g = bytes[pos + 1];
                        r = bytes[pos + 2];
                    }
                    else
                    {
                        r = bytes[pos];
                        g = bytes[pos + 1];
                        b = bytes[pos + 2];
                    }
                    surfaceMap[x][y] = RgbToFloat(r, g, b);
                }
            }

            img.UnlockBits(data);
            if (resized)
            {
                img.Dispose();
            }

            return surfaceMap;
        }

        /// <summary>
        /// Converts a byte array containing an image to a surface map.
        /// </summary>
        /// <param name="bytes">The byte array containing an image to convert.</param>
        /// <param name="destWidth">An optional width to which the image will be stretched. If left
        /// <see langword="null"/> the source width will be retained.</param>
        /// <param name="destHeight">An optional height to which the image will be stretched. If
        /// left <see langword="null"/> the source height will be retained.</param>
        /// <returns>A surface map.</returns>
        public static float[][] ImageToFloatSurfaceMap(this byte[]? bytes, int? destWidth = null, int? destHeight = null)
            => ImageToFloatSurfaceMap(ToImage(bytes), destWidth, destHeight);

        /// <summary>
        /// Converts a pair of images to a surface map.
        /// </summary>
        /// <param name="imageMin">The image with the minimum values to convert.</param>
        /// <param name="imageMax">The image with the maximum values to convert.</param>
        /// <param name="destWidth">An optional width to which the image will be stretched. If left
        /// <see langword="null"/> the source width will be retained.</param>
        /// <param name="destHeight">An optional height to which the image will be stretched. If
        /// left <see langword="null"/> the source height will be retained.</param>
        /// <returns>A surface map.</returns>
        public static FloatRange[][] ImagesToFloatRangeSurfaceMap(this Bitmap? imageMin, Bitmap? imageMax, int? destWidth = null, int? destHeight = null)
        {
            if (imageMin is null && imageMax is null)
            {
                return new FloatRange[0][];
            }

            if (imageMin is null)
            {
                var max = ImageToFloatSurfaceMap(imageMax!, destWidth, destHeight);
                var result = new FloatRange[max.Length][];
                for (var x = 0; x < max.Length; x++)
                {
                    result[x] = new FloatRange[max[x].Length];
                    for (var y = 0; y < max[x].Length; y++)
                    {
                        result[x][y] = new FloatRange(max[x][y]);
                    }
                }
                return result;
            }
            if (imageMax is null)
            {
                var min = ImageToFloatSurfaceMap(imageMin!, destWidth, destHeight);
                var result = new FloatRange[min.Length][];
                for (var x = 0; x < min.Length; x++)
                {
                    result[x] = new FloatRange[min[x].Length];
                    for (var y = 0; y < min[x].Length; y++)
                    {
                        result[x][y] = new FloatRange(min[x][y]);
                    }
                }
                return result;
            }

            var imgMin = imageMin;
            var imgMax = imageMax;
            var minResized = false;
            var maxResized = false;
            if (destWidth.HasValue || destHeight.HasValue)
            {
                imgMin = imageMin.Resize(destWidth ?? imageMin.Width, destHeight ?? imageMin.Height);
                imgMax = imageMax.Resize(destWidth ?? imageMax.Width, destHeight ?? imageMax.Height);
                minResized = true;
                maxResized = true;
            }
            else if (imageMax.Width != imageMin.Width || imageMax.Height != imageMin.Height)
            {
                imgMax = imageMax.Resize(imageMin.Width, imageMin.Height);
                maxResized = true;
            }

            var xLength = imgMin.Width;
            var yLength = imgMin.Height;
            var minMap = new float[xLength][];
            for (var i = 0; i < minMap.Length; i++)
            {
                minMap[i] = new float[yLength];
            }
            var dataMin = imgMin.LockBits(new Rectangle(0, 0, xLength, yLength), ImageLockMode.ReadOnly, imgMin.PixelFormat);
            var pixelSizeMin = Image.GetPixelFormatSize(dataMin.PixelFormat) / 8;
            var lengthMin = dataMin.Height * dataMin.Stride;
            var bytesMin = new byte[lengthMin];
            System.Runtime.InteropServices.Marshal.Copy(dataMin.Scan0, bytesMin, 0, lengthMin);
            byte r, g, b;
            for (var y = 0; y < yLength; y++)
            {
                for (var x = 0; x < xLength; x++)
                {
                    var pos = (y * dataMin.Stride) + (x * pixelSizeMin);
                    if (pixelSizeMin == 4)
                    {
                        if (BitConverter.IsLittleEndian)
                        {
                            b = bytesMin[pos + 1];
                            g = bytesMin[pos + 2];
                            r = bytesMin[pos + 3];
                        }
                        else
                        {
                            r = bytesMin[pos];
                            g = bytesMin[pos + 1];
                            b = bytesMin[pos + 2];
                        }
                    }
                    else if (BitConverter.IsLittleEndian)
                    {
                        b = bytesMin[pos];
                        g = bytesMin[pos + 1];
                        r = bytesMin[pos + 2];
                    }
                    else
                    {
                        r = bytesMin[pos];
                        g = bytesMin[pos + 1];
                        b = bytesMin[pos + 2];
                    }
                    minMap[x][y] = RgbToFloat(r, g, b);
                }
            }
            imgMin.UnlockBits(dataMin);
            if (minResized)
            {
                imgMin.Dispose();
            }

            var maxMap = new float[xLength][];
            for (var i = 0; i < maxMap.Length; i++)
            {
                maxMap[i] = new float[yLength];
            }
            var dataMax = imgMax.LockBits(new Rectangle(0, 0, xLength, yLength), ImageLockMode.ReadOnly, imgMax.PixelFormat);
            var pixelSizeMax = Image.GetPixelFormatSize(dataMax.PixelFormat) / 8;
            var lengthMax = dataMax.Height * dataMax.Stride;
            var bytesMax = new byte[lengthMax];
            System.Runtime.InteropServices.Marshal.Copy(dataMax.Scan0, bytesMax, 0, lengthMax);
            for (var y = 0; y < yLength; y++)
            {
                for (var x = 0; x < xLength; x++)
                {
                    var pos = (y * dataMax.Stride) + (x * pixelSizeMax);
                    if (pixelSizeMax == 4)
                    {
                        if (BitConverter.IsLittleEndian)
                        {
                            b = bytesMax[pos + 1];
                            g = bytesMax[pos + 2];
                            r = bytesMax[pos + 3];
                        }
                        else
                        {
                            r = bytesMax[pos];
                            g = bytesMax[pos + 1];
                            b = bytesMax[pos + 2];
                        }
                    }
                    else if (BitConverter.IsLittleEndian)
                    {
                        b = bytesMax[pos];
                        g = bytesMax[pos + 1];
                        r = bytesMax[pos + 2];
                    }
                    else
                    {
                        r = bytesMax[pos];
                        g = bytesMax[pos + 1];
                        b = bytesMax[pos + 2];
                    }
                    maxMap[x][y] = RgbToFloat(r, g, b);
                }
            }
            imgMax.UnlockBits(dataMax);
            if (maxResized)
            {
                imgMax.Dispose();
            }

            var surfaceMap = new FloatRange[xLength][];
            for (var x = 0; x < xLength; x++)
            {
                surfaceMap[x] = new FloatRange[yLength];
                for (var y = 0; y < yLength; y++)
                {
                    surfaceMap[x][y] = new FloatRange(minMap[x][y], maxMap[x][y]);
                }
            }
            return surfaceMap;
        }

        /// <summary>
        /// Converts a pair of byte arrays containing images to a surface map.
        /// </summary>
        /// <param name="imageMin">The byte arrays containing an image with the minimum values to convert.</param>
        /// <param name="imageMax">The byte arrays containing an image with the maximum values to convert.</param>
        /// <param name="destWidth">An optional width to which the image will be stretched. If left
        /// <see langword="null"/> the source width will be retained.</param>
        /// <param name="destHeight">An optional height to which the image will be stretched. If
        /// left <see langword="null"/> the source height will be retained.</param>
        /// <returns>A surface map.</returns>
        public static FloatRange[][] ImagesToFloatRangeSurfaceMap(this byte[]? imageMin, byte[]? imageMax, int? destWidth = null, int? destHeight = null)
        {
            if (imageMin is null && imageMax is null)
            {
                return new FloatRange[0][];
            }

            if (imageMin is null)
            {
                var max = ImageToFloatSurfaceMap(imageMax!, destWidth, destHeight);
                var result = new FloatRange[max.Length][];
                for (var x = 0; x < max.Length; x++)
                {
                    result[x] = new FloatRange[max[x].Length];
                    for (var y = 0; y < max[x].Length; y++)
                    {
                        result[x][y] = new FloatRange(max[x][y]);
                    }
                }
                return result;
            }
            if (imageMax is null)
            {
                var min = ImageToFloatSurfaceMap(imageMin!, destWidth, destHeight);
                var result = new FloatRange[min.Length][];
                for (var x = 0; x < min.Length; x++)
                {
                    result[x] = new FloatRange[min[x].Length];
                    for (var y = 0; y < min[x].Length; y++)
                    {
                        result[x][y] = new FloatRange(min[x][y]);
                    }
                }
                return result;
            }

            return ImagesToFloatRangeSurfaceMap(ToImage(imageMin), ToImage(imageMax), destWidth, destHeight);
        }

        /// <summary>
        /// <para>
        /// Converts a precipitation map to an image.
        /// </para>
        /// <para>
        /// Values are rendered as a greenish color, with low values being yellow, tending towards
        /// darker green as they increase.
        /// </para>
        /// <para>
        /// The color gradations follow an increasing scale, with each color shade representing
        /// double the amount of precipitation as the previous rank.
        /// </para>
        /// </summary>
        /// <param name="precipitationMap">The precipitation map to convert.</param>
        /// <param name="maxPrecipitation">
        /// The maximum precipitation of the planet, in mm.
        /// </param>
        /// <returns>An image.</returns>
        public static Bitmap PrecipitationMapToImage(this float[][] precipitationMap, double maxPrecipitation)
            => SurfaceMapToImage(precipitationMap, v => (v * maxPrecipitation).ToPrecipitationColor());

        /// <summary>
        /// Converts a surface map to an image.
        /// </summary>
        /// <param name="surfaceMap">The surface map to convert.</param>
        /// <param name="converter">
        /// <para>
        /// A function which accepts a <see cref="double"/> value and returns the R, G, and B values
        /// which should be displayed for that value.
        /// </para>
        /// <para>
        /// If left <see langword="null"/>, a uniform (grayscale) value will be generated with equal
        /// R, G, and B components.
        /// </para>
        /// </param>
        /// <returns>An image.</returns>
        public static Bitmap SurfaceMapToImage(this float[][] surfaceMap, Func<float, (byte, byte, byte)>? converter = null)
        {
            var xLength = surfaceMap.Length;
            var yLength = xLength == 0 ? 0 : surfaceMap[0].Length;
            var image = new Bitmap(xLength, yLength);
            var data = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, image.PixelFormat);
            var pixelSize = Image.GetPixelFormatSize(data.PixelFormat) / 8;
            var length = data.Height * data.Stride;
            var bytes = new byte[length];
            System.Runtime.InteropServices.Marshal.Copy(data.Scan0, bytes, 0, length);

            for (var y = 0; y < image.Height; y++)
            {
                for (var x = 0; x < image.Width; x++)
                {
                    var pos = (y * data.Stride) + (x * pixelSize);
                    byte r, g, b;
                    if (converter is null)
                    {
                        var rgb = FloatToRgb(surfaceMap[x][y]);
                        r = rgb;
                        g = rgb;
                        b = rgb;
                    }
                    else
                    {
                        (r, g, b) = converter(surfaceMap[x][y]);
                    }
                    if (pixelSize == 4)
                    {
                        if (BitConverter.IsLittleEndian)
                        {
                            bytes[pos] = b;
                            bytes[pos + 1] = g;
                            bytes[pos + 2] = r;
                            bytes[pos + 3] = 255;
                        }
                        else
                        {
                            bytes[pos] = 255;
                            bytes[pos + 1] = r;
                            bytes[pos + 2] = g;
                            bytes[pos + 3] = b;
                        }
                    }
                    else if (BitConverter.IsLittleEndian)
                    {
                        bytes[pos] = b;
                        bytes[pos + 1] = g;
                        bytes[pos + 2] = r;
                    }
                    else
                    {
                        bytes[pos] = r;
                        bytes[pos + 1] = g;
                        bytes[pos + 2] = b;
                    }
                }
            }

            System.Runtime.InteropServices.Marshal.Copy(bytes, 0, data.Scan0, length);
            image.UnlockBits(data);
            return image;
        }

        /// <summary>
        /// Converts a surface map to an image.
        /// </summary>
        /// <param name="surfaceMap">The surface map to convert.</param>
        /// <param name="converter">
        /// <para>
        /// A function which accepts a <see cref="double"/> value and returns the R, G, and B values
        /// which should be displayed for that value.
        /// </para>
        /// <para>
        /// If left <see langword="null"/>, a uniform (grayscale) value will be generated with equal
        /// R, G, and B components.
        /// </para>
        /// </param>
        /// <returns>An image.</returns>
        public static Bitmap SurfaceMapToImage(this double[][] surfaceMap, Func<double, (byte, byte, byte)>? converter = null)
        {
            var xLength = surfaceMap.Length;
            var yLength = xLength == 0 ? 0 : surfaceMap[0].Length;
            var image = new Bitmap(xLength, yLength);
            var data = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, image.PixelFormat);
            var pixelSize = Image.GetPixelFormatSize(data.PixelFormat) / 8;
            var length = data.Height * data.Stride;
            var bytes = new byte[length];
            System.Runtime.InteropServices.Marshal.Copy(data.Scan0, bytes, 0, length);

            for (var y = 0; y < image.Height; y++)
            {
                for (var x = 0; x < image.Width; x++)
                {
                    var pos = (y * data.Stride) + (x * pixelSize);
                    byte r, g, b;
                    if (converter is null)
                    {
                        var rgb = DoubleToRgb(surfaceMap[x][y]);
                        r = rgb;
                        g = rgb;
                        b = rgb;
                    }
                    else
                    {
                        (r, g, b) = converter(surfaceMap[x][y]);
                    }
                    if (pixelSize == 4)
                    {
                        if (BitConverter.IsLittleEndian)
                        {
                            bytes[pos] = b;
                            bytes[pos + 1] = g;
                            bytes[pos + 2] = r;
                            bytes[pos + 3] = 255;
                        }
                        else
                        {
                            bytes[pos] = 255;
                            bytes[pos + 1] = r;
                            bytes[pos + 2] = g;
                            bytes[pos + 3] = b;
                        }
                    }
                    else if (BitConverter.IsLittleEndian)
                    {
                        bytes[pos] = b;
                        bytes[pos + 1] = g;
                        bytes[pos + 2] = r;
                    }
                    else
                    {
                        bytes[pos] = r;
                        bytes[pos + 1] = g;
                        bytes[pos + 2] = b;
                    }
                }
            }

            System.Runtime.InteropServices.Marshal.Copy(bytes, 0, data.Scan0, length);
            image.UnlockBits(data);
            return image;
        }

        /// <summary>
        /// Converts a surface map to an image.
        /// </summary>
        /// <param name="surfaceMap">The surface map to convert.</param>
        /// <param name="converter">
        /// A function which accepts a value and returns the R, G, and B values which should be
        /// displayed for that value.
        /// </param>
        /// <returns>An image.</returns>
        public static Bitmap SurfaceMapToImage<T>(this T[][] surfaceMap, Func<T, (byte, byte, byte)> converter)
        {
            var xLength = surfaceMap.Length;
            var yLength = xLength == 0 ? 0 : surfaceMap[0].Length;
            var image = new Bitmap(xLength, yLength);
            var data = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, image.PixelFormat);
            var pixelSize = Image.GetPixelFormatSize(data.PixelFormat) / 8;
            var length = data.Height * data.Stride;
            var bytes = new byte[length];
            System.Runtime.InteropServices.Marshal.Copy(data.Scan0, bytes, 0, length);

            for (var y = 0; y < image.Height; y++)
            {
                for (var x = 0; x < image.Width; x++)
                {
                    var pos = (y * data.Stride) + (x * pixelSize);
                    var (r, g, b) = converter(surfaceMap[x][y]);
                    if (pixelSize == 4)
                    {
                        if (BitConverter.IsLittleEndian)
                        {
                            bytes[pos] = b;
                            bytes[pos + 1] = g;
                            bytes[pos + 2] = r;
                            bytes[pos + 3] = 255;
                        }
                        else
                        {
                            bytes[pos] = 255;
                            bytes[pos + 1] = r;
                            bytes[pos + 2] = g;
                            bytes[pos + 3] = b;
                        }
                    }
                    else if (BitConverter.IsLittleEndian)
                    {
                        bytes[pos] = b;
                        bytes[pos + 1] = g;
                        bytes[pos + 2] = r;
                    }
                    else
                    {
                        bytes[pos] = r;
                        bytes[pos + 1] = g;
                        bytes[pos + 2] = b;
                    }
                }
            }

            System.Runtime.InteropServices.Marshal.Copy(bytes, 0, data.Scan0, length);
            image.UnlockBits(data);
            return image;
        }

        /// <summary>
        /// Converts a surface map to an image.
        /// </summary>
        /// <param name="surfaceMap">The surface map to convert.</param>
        /// <param name="converter">
        /// A function which accepts a value and its X and Y indexes within the map, and returns the
        /// R, G, and B values which should be displayed for that value.
        /// </param>
        /// <returns>An image.</returns>
        public static Bitmap SurfaceMapToImage<T>(this T[][] surfaceMap, Func<T, int, int, (byte, byte, byte)> converter)
        {
            var xLength = surfaceMap.Length;
            var yLength = xLength == 0 ? 0 : surfaceMap[0].Length;
            var image = new Bitmap(xLength, yLength);
            var data = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, image.PixelFormat);
            var pixelSize = Image.GetPixelFormatSize(data.PixelFormat) / 8;
            var length = data.Height * data.Stride;
            var bytes = new byte[length];
            System.Runtime.InteropServices.Marshal.Copy(data.Scan0, bytes, 0, length);

            for (var y = 0; y < image.Height; y++)
            {
                for (var x = 0; x < image.Width; x++)
                {
                    var pos = (y * data.Stride) + (x * pixelSize);
                    var (r, g, b) = converter(surfaceMap[x][y], x, y);
                    if (pixelSize == 4)
                    {
                        if (BitConverter.IsLittleEndian)
                        {
                            bytes[pos] = b;
                            bytes[pos + 1] = g;
                            bytes[pos + 2] = r;
                            bytes[pos + 3] = 255;
                        }
                        else
                        {
                            bytes[pos] = 255;
                            bytes[pos + 1] = r;
                            bytes[pos + 2] = g;
                            bytes[pos + 3] = b;
                        }
                    }
                    else if (BitConverter.IsLittleEndian)
                    {
                        bytes[pos] = b;
                        bytes[pos + 1] = g;
                        bytes[pos + 2] = r;
                    }
                    else
                    {
                        bytes[pos] = r;
                        bytes[pos + 1] = g;
                        bytes[pos + 2] = b;
                    }
                }
            }

            System.Runtime.InteropServices.Marshal.Copy(bytes, 0, data.Scan0, length);
            image.UnlockBits(data);
            return image;
        }

        /// <summary>
        /// <para>
        /// Converts a surface map to an image.
        /// </para>
        /// <para>
        /// This overload generates a single map using the average provided by each <see
        /// cref="FloatRange"/>.
        /// </para>
        /// </summary>
        /// <param name="surfaceMap">The surface map to convert.</param>
        /// <param name="converter">
        /// <para>
        /// A function which accepts a <see cref="double"/> value and returns the R, G, and B values
        /// which should be displayed for that value.
        /// </para>
        /// <para>
        /// If left <see langword="null"/>, a uniform (grayscale) value will be generated with equal
        /// R, G, and B components.
        /// </para>
        /// </param>
        /// <returns>An image.</returns>
        public static Bitmap SurfaceMapToImage(this FloatRange[][] surfaceMap, Func<float, (byte, byte, byte)>? converter = null)
        {
            var xLength = surfaceMap.Length;
            var yLength = xLength == 0 ? 0 : surfaceMap[0].Length;
            var image = new Bitmap(xLength, yLength);
            var data = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, image.PixelFormat);
            var pixelSize = Image.GetPixelFormatSize(data.PixelFormat) / 8;
            var length = data.Height * data.Stride;
            var bytes = new byte[length];
            System.Runtime.InteropServices.Marshal.Copy(data.Scan0, bytes, 0, length);

            for (var y = 0; y < image.Height; y++)
            {
                for (var x = 0; x < image.Width; x++)
                {
                    var pos = (y * data.Stride) + (x * pixelSize);
                    byte r, g, b;
                    if (converter is null)
                    {
                        var rgb = FloatToRgb(surfaceMap[x][y].Average);
                        r = rgb;
                        g = rgb;
                        b = rgb;
                    }
                    else
                    {
                        (r, g, b) = converter(surfaceMap[x][y].Average);
                    }
                    if (pixelSize == 4)
                    {
                        if (BitConverter.IsLittleEndian)
                        {
                            bytes[pos] = b;
                            bytes[pos + 1] = g;
                            bytes[pos + 2] = r;
                            bytes[pos + 3] = 255;
                        }
                        else
                        {
                            bytes[pos] = 255;
                            bytes[pos + 1] = r;
                            bytes[pos + 2] = g;
                            bytes[pos + 3] = b;
                        }
                    }
                    else if (BitConverter.IsLittleEndian)
                    {
                        bytes[pos] = b;
                        bytes[pos + 1] = g;
                        bytes[pos + 2] = r;
                    }
                    else
                    {
                        bytes[pos] = r;
                        bytes[pos + 1] = g;
                        bytes[pos + 2] = b;
                    }
                }
            }

            System.Runtime.InteropServices.Marshal.Copy(bytes, 0, data.Scan0, length);
            image.UnlockBits(data);
            return image;
        }

        /// <summary>
        /// Converts a <see cref="FloatRange"/> surface map to a pair of grayscale images
        /// representing the minimum and maximum values.
        /// </summary>
        /// <param name="surfaceMap">The surface map to convert.</param>
        /// <param name="converter">
        /// <para>
        /// A function which accepts a <see cref="double"/> value and returns the R, G, and B values
        /// which should be displayed for that value.
        /// </para>
        /// <para>
        /// If left <see langword="null"/>, a uniform (grayscale) value will be generated with equal
        /// R, G, and B components.
        /// </para>
        /// </param>
        /// <returns>
        /// A pair of grayscale images representing the minimum and maximum values.
        /// </returns>
        public static (Bitmap min, Bitmap max) SurfaceMapToImages(this FloatRange[][] surfaceMap, Func<float, (byte, byte, byte)>? converter = null)
        {
            var xLength = surfaceMap.Length;
            var yLength = xLength == 0 ? 0 : surfaceMap[0].Length;
            var imageMin = new Bitmap(xLength, yLength);
            var imageMax = new Bitmap(xLength, yLength);
            var dataMin = imageMin.LockBits(new Rectangle(0, 0, imageMin.Width, imageMin.Height), ImageLockMode.ReadOnly, imageMin.PixelFormat);
            var dataMax = imageMax.LockBits(new Rectangle(0, 0, imageMax.Width, imageMax.Height), ImageLockMode.ReadOnly, imageMax.PixelFormat);
            var pixelSize = Image.GetPixelFormatSize(dataMin.PixelFormat) / 8;
            var length = dataMin.Height * dataMin.Stride;
            var bytesMin = new byte[length];
            var bytesMax = new byte[length];
            System.Runtime.InteropServices.Marshal.Copy(dataMin.Scan0, bytesMin, 0, length);
            System.Runtime.InteropServices.Marshal.Copy(dataMax.Scan0, bytesMax, 0, length);

            for (var y = 0; y < imageMin.Height; y++)
            {
                for (var x = 0; x < imageMin.Width; x++)
                {
                    var pos = (y * dataMin.Stride) + (x * pixelSize);

                    byte rMin, gMin, bMin, rMax, gMax, bMax;
                    if (converter is null)
                    {
                        var rgb = FloatToRgb(surfaceMap[x][y].Min);
                        rMin = rgb;
                        gMin = rgb;
                        bMin = rgb;

                        rgb = FloatToRgb(surfaceMap[x][y].Max);
                        rMax = rgb;
                        gMax = rgb;
                        bMax = rgb;
                    }
                    else
                    {
                        (rMin, gMin, bMin) = converter(surfaceMap[x][y].Min);
                        (rMax, gMax, bMax) = converter(surfaceMap[x][y].Max);
                    }
                    if (pixelSize == 4)
                    {
                        if (BitConverter.IsLittleEndian)
                        {
                            bytesMin[pos] = bMin;
                            bytesMin[pos + 1] = gMin;
                            bytesMin[pos + 2] = rMin;
                            bytesMin[pos + 3] = 255;

                            bytesMax[pos] = bMax;
                            bytesMax[pos + 1] = gMax;
                            bytesMax[pos + 2] = rMax;
                            bytesMax[pos + 3] = 255;
                        }
                        else
                        {
                            bytesMin[pos] = 255;
                            bytesMin[pos + 1] = rMin;
                            bytesMin[pos + 2] = gMin;
                            bytesMin[pos + 3] = bMin;

                            bytesMax[pos] = 255;
                            bytesMax[pos + 1] = rMax;
                            bytesMax[pos + 2] = gMax;
                            bytesMax[pos + 3] = bMax;
                        }
                    }
                    else if (BitConverter.IsLittleEndian)
                    {
                        bytesMin[pos] = bMin;
                        bytesMin[pos + 1] = gMin;
                        bytesMin[pos + 2] = rMin;

                        bytesMax[pos] = bMax;
                        bytesMax[pos + 1] = gMax;
                        bytesMax[pos + 2] = rMax;
                    }
                    else
                    {
                        bytesMin[pos] = rMin;
                        bytesMin[pos + 1] = gMin;
                        bytesMin[pos + 2] = bMin;

                        bytesMax[pos] = rMax;
                        bytesMax[pos + 1] = gMax;
                        bytesMax[pos + 2] = bMax;
                    }
                }
            }

            System.Runtime.InteropServices.Marshal.Copy(bytesMin, 0, dataMin.Scan0, length);
            System.Runtime.InteropServices.Marshal.Copy(bytesMax, 0, dataMax.Scan0, length);
            imageMin.UnlockBits(dataMin);
            imageMax.UnlockBits(dataMax);
            return (imageMin, imageMax);
        }

        /// <summary>
        /// Converts a surface map to an image.
        /// </summary>
        /// <param name="surfaceMap">The surface map to convert.</param>
        /// <param name="elevationMap">An elevation map.</param>
        /// <param name="landConverter">
        /// <para>
        /// A function which accepts a <see cref="float"/> value and the elevation at that point,
        /// and returns the R, G, and B values which should be displayed for that value. Used for
        /// pixels whose elevation is non-negative.
        /// </para>
        /// <para>
        /// If left <see langword="null"/>, a uniform (grayscale) value will be generated with equal
        /// R, G, and B components.
        /// </para>
        /// </param>
        /// <param name="oceanConverter">
        /// <para>
        /// A function which accepts a <see cref="float"/> value and the elevation at that point,
        /// and returns the R, G, and B values which should be displayed for that value. Used for
        /// pixels whose elevation is negative.
        /// </para>
        /// <para>
        /// If left <see langword="null"/>, a uniform (grayscale) value will be generated with equal
        /// R, G, and B components.
        /// </para>
        /// </param>
        /// <param name="applyHillShadingToLand">
        /// If <see langword="true"/> a hill shading effect will be applied to areas with
        /// non-negative elevation.
        /// </param>
        /// <param name="applyHillShadingToOcean">
        /// If <see langword="true"/> a hill shading effect will be applied to areas with negative
        /// elevation.
        /// </param>
        /// <param name="hillScaleFactor">
        /// <para>
        /// An arbitrary factor which increases the apparent shading of slopes. Adjust as needed to
        /// give the appearance of sharper shading on low-slope maps, or decrease to reduce harsh
        /// shading on high-slope maps.
        /// </para>
        /// <para>
        /// Values between 0 and 1 decrease the degree of shading. Values greater than 1 increase
        /// it.
        /// </para>
        /// <para>
        /// Negative values are not allowed; these are truncated to 0 (no shading).
        /// </para>
        /// </param>
        /// <param name="hillScaleIsRelative">
        /// If <see langword="true"/> the <paramref name="hillScaleFactor"/> will increase with
        /// distance from sea level. Otherwise a uniform value will be used everywhere.
        /// </param>
        /// <param name="hillShadeMultiplier">
        /// An arbitrary value by which to multiply the hill shading's effect. Values less than 1
        /// are permitted but would be better accomplished by reducing <paramref
        /// name="hillScaleFactor"/>. Values greater than 1 have the effect of brightening the side
        /// of slopes facing the imagined light source, which can reduce the darkening effect of
        /// applying hill shading to a map. Values less than zero are treated as zero, and are the
        /// same as if hill shading was not applied (aside from the performance cost).
        /// </param>
        /// <returns>An image.</returns>
        public static Bitmap SurfaceMapToImage(
            this float[][] surfaceMap,
            double[][] elevationMap,
            Func<float, double, (byte, byte, byte)>? landConverter = null,
            Func<float, double, (byte, byte, byte)>? oceanConverter = null,
            bool applyHillShadingToLand = false,
            bool applyHillShadingToOcean = false,
            double hillScaleFactor = 1,
            bool hillScaleIsRelative = false,
            double hillShadeMultiplier = 1)
        {
            var xLength = surfaceMap.Length;
            var yLength = xLength == 0 ? 0 : surfaceMap[0].Length;
            var image = new Bitmap(xLength, yLength);
            var data = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, image.PixelFormat);
            var pixelSize = Image.GetPixelFormatSize(data.PixelFormat) / 8;
            var length = data.Height * data.Stride;
            var bytes = new byte[length];
            System.Runtime.InteropServices.Marshal.Copy(data.Scan0, bytes, 0, length);

            var hillShadeMap = new float[0][];
            if (applyHillShadingToLand || applyHillShadingToOcean)
            {
                hillShadeMap = SurfaceMap.GetHillShadeMap(elevationMap, hillScaleFactor, hillScaleIsRelative);
                hillShadeMultiplier = Math.Max(0, hillShadeMultiplier);
            }

            for (var y = 0; y < image.Height; y++)
            {
                for (var x = 0; x < image.Width; x++)
                {
                    var pos = (y * data.Stride) + (x * pixelSize);
                    byte r, g, b;
                    if (elevationMap[x][y] >= 0)
                    {
                        if (landConverter is null)
                        {
                            var rgb = FloatToRgb(surfaceMap[x][y]);
                            r = rgb;
                            g = rgb;
                            b = rgb;
                        }
                        else
                        {
                            (r, g, b) = landConverter(surfaceMap[x][y], elevationMap[x][y]);
                        }
                        if (applyHillShadingToLand)
                        {
                            var factor = hillShadeMap[x][y] * hillShadeMultiplier;
                            r = (byte)(r * factor).Clamp(0, 255);
                            g = (byte)(g * factor).Clamp(0, 255);
                            b = (byte)(b * factor).Clamp(0, 255);
                        }
                    }
                    else
                    {
                        if (oceanConverter is null)
                        {
                            var rgb = FloatToRgb(surfaceMap[x][y]);
                            r = rgb;
                            g = rgb;
                            b = rgb;
                        }
                        else
                        {
                            (r, g, b) = oceanConverter(surfaceMap[x][y], elevationMap[x][y]);
                        }
                        if (applyHillShadingToOcean)
                        {
                            var factor = hillShadeMap[x][y] * hillShadeMultiplier;
                            r = (byte)(r * factor).Clamp(0, 255);
                            g = (byte)(g * factor).Clamp(0, 255);
                            b = (byte)(b * factor).Clamp(0, 255);
                        }
                    }
                    if (pixelSize == 4)
                    {
                        if (BitConverter.IsLittleEndian)
                        {
                            bytes[pos] = b;
                            bytes[pos + 1] = g;
                            bytes[pos + 2] = r;
                            bytes[pos + 3] = 255;
                        }
                        else
                        {
                            bytes[pos] = 255;
                            bytes[pos + 1] = r;
                            bytes[pos + 2] = g;
                            bytes[pos + 3] = b;
                        }
                    }
                    else if (BitConverter.IsLittleEndian)
                    {
                        bytes[pos] = b;
                        bytes[pos + 1] = g;
                        bytes[pos + 2] = r;
                    }
                    else
                    {
                        bytes[pos] = r;
                        bytes[pos + 1] = g;
                        bytes[pos + 2] = b;
                    }
                }
            }

            System.Runtime.InteropServices.Marshal.Copy(bytes, 0, data.Scan0, length);
            image.UnlockBits(data);
            return image;
        }

        /// <summary>
        /// Converts a surface map to an image.
        /// </summary>
        /// <param name="surfaceMap">The surface map to convert.</param>
        /// <param name="elevationMap">An elevation map.</param>
        /// <param name="landConverter">
        /// <para>
        /// A function which accepts a <see cref="double"/> value and the elevation at that point,
        /// and returns the R, G, and B values which should be displayed for that value. Used for
        /// pixels whose elevation is non-negative.
        /// </para>
        /// <para>
        /// If left <see langword="null"/>, a uniform (grayscale) value will be generated with equal
        /// R, G, and B components.
        /// </para>
        /// </param>
        /// <param name="oceanConverter">
        /// <para>
        /// A function which accepts a <see cref="double"/> value and the elevation at that point,
        /// and returns the R, G, and B values which should be displayed for that value. Used for
        /// pixels whose elevation is negative.
        /// </para>
        /// <para>
        /// If left <see langword="null"/>, a uniform (grayscale) value will be generated with equal
        /// R, G, and B components.
        /// </para>
        /// </param>
        /// <param name="applyHillShadingToLand">
        /// If <see langword="true"/> a hill shading effect will be applied to areas with
        /// non-negative elevation.
        /// </param>
        /// <param name="applyHillShadingToOcean">
        /// If <see langword="true"/> a hill shading effect will be applied to areas with negative
        /// elevation.
        /// </param>
        /// <param name="hillScaleFactor">
        /// <para>
        /// An arbitrary factor which increases the apparent shading of slopes. Adjust as needed to
        /// give the appearance of sharper shading on low-slope maps, or decrease to reduce harsh
        /// shading on high-slope maps.
        /// </para>
        /// <para>
        /// Values between 0 and 1 decrease the degree of shading. Values greater than 1 increase
        /// it.
        /// </para>
        /// <para>
        /// Negative values are not allowed; these are truncated to 0 (no shading).
        /// </para>
        /// </param>
        /// <param name="hillScaleIsRelative">
        /// If <see langword="true"/> the <paramref name="hillScaleFactor"/> will increase with
        /// distance from sea level. Otherwise a uniform value will be used everywhere.
        /// </param>
        /// <param name="hillShadeMultiplier">
        /// An arbitrary value by which to multiply the hill shading's effect. Values less than 1
        /// are permitted but would be better accomplished by reducing <paramref
        /// name="hillScaleFactor"/>. Values greater than 1 have the effect of brightening the side
        /// of slopes facing the imagined light source, which can reduce the darkening effect of
        /// applying hill shading to a map. Values less than zero are treated as zero, and are the
        /// same as if hill shading was not applied (aside from the performance cost).
        /// </param>
        /// <returns>An image.</returns>
        public static Bitmap SurfaceMapToImage(
            this double[][] surfaceMap,
            double[][] elevationMap,
            Func<double, double, (byte, byte, byte)>? landConverter = null,
            Func<double, double, (byte, byte, byte)>? oceanConverter = null,
            bool applyHillShadingToLand = false,
            bool applyHillShadingToOcean = false,
            double hillScaleFactor = 1,
            bool hillScaleIsRelative = false,
            double hillShadeMultiplier = 1)
        {
            var xLength = surfaceMap.Length;
            var yLength = xLength == 0 ? 0 : surfaceMap[0].Length;
            var image = new Bitmap(xLength, yLength);
            var data = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, image.PixelFormat);
            var pixelSize = Image.GetPixelFormatSize(data.PixelFormat) / 8;
            var length = data.Height * data.Stride;
            var bytes = new byte[length];
            System.Runtime.InteropServices.Marshal.Copy(data.Scan0, bytes, 0, length);

            var hillShadeMap = new float[0][];
            if (applyHillShadingToLand || applyHillShadingToOcean)
            {
                hillShadeMap = SurfaceMap.GetHillShadeMap(elevationMap, hillScaleFactor, hillScaleIsRelative);
                hillShadeMultiplier = Math.Max(0, hillShadeMultiplier);
            }

            for (var y = 0; y < image.Height; y++)
            {
                for (var x = 0; x < image.Width; x++)
                {
                    var pos = (y * data.Stride) + (x * pixelSize);
                    byte r, g, b;
                    if (elevationMap[x][y] >= 0)
                    {
                        if (landConverter is null)
                        {
                            var rgb = DoubleToRgb(surfaceMap[x][y]);
                            r = rgb;
                            g = rgb;
                            b = rgb;
                        }
                        else
                        {
                            (r, g, b) = landConverter(surfaceMap[x][y], elevationMap[x][y]);
                        }
                        if (applyHillShadingToLand)
                        {
                            var factor = hillShadeMap[x][y] * hillShadeMultiplier;
                            r = (byte)(r * factor).Clamp(0, 255);
                            g = (byte)(g * factor).Clamp(0, 255);
                            b = (byte)(b * factor).Clamp(0, 255);
                        }
                    }
                    else
                    {
                        if (oceanConverter is null)
                        {
                            var rgb = DoubleToRgb(surfaceMap[x][y]);
                            r = rgb;
                            g = rgb;
                            b = rgb;
                        }
                        else
                        {
                            (r, g, b) = oceanConverter(surfaceMap[x][y], elevationMap[x][y]);
                        }
                        if (applyHillShadingToOcean)
                        {
                            var factor = hillShadeMap[x][y] * hillShadeMultiplier;
                            r = (byte)(r * factor).Clamp(0, 255);
                            g = (byte)(g * factor).Clamp(0, 255);
                            b = (byte)(b * factor).Clamp(0, 255);
                        }
                    }
                    if (pixelSize == 4)
                    {
                        if (BitConverter.IsLittleEndian)
                        {
                            bytes[pos] = b;
                            bytes[pos + 1] = g;
                            bytes[pos + 2] = r;
                            bytes[pos + 3] = 255;
                        }
                        else
                        {
                            bytes[pos] = 255;
                            bytes[pos + 1] = r;
                            bytes[pos + 2] = g;
                            bytes[pos + 3] = b;
                        }
                    }
                    else if (BitConverter.IsLittleEndian)
                    {
                        bytes[pos] = b;
                        bytes[pos + 1] = g;
                        bytes[pos + 2] = r;
                    }
                    else
                    {
                        bytes[pos] = r;
                        bytes[pos + 1] = g;
                        bytes[pos + 2] = b;
                    }
                }
            }

            System.Runtime.InteropServices.Marshal.Copy(bytes, 0, data.Scan0, length);
            image.UnlockBits(data);
            return image;
        }

        /// <summary>
        /// Converts a surface map to an image.
        /// </summary>
        /// <param name="surfaceMap">The surface map to convert.</param>
        /// <param name="elevationMap">An elevation map.</param>
        /// <param name="landConverter">
        /// A function which accepts a value and the elevation at that point, and returns the R, G,
        /// and B values which should be displayed for that value. Used for pixels whose elevation
        /// is non-negative.
        /// </param>
        /// <param name="oceanConverter">
        /// A function which accepts a value and the elevation at that point, and returns the R, G,
        /// and B values which should be displayed for that value. Used for pixels whose elevation
        /// is negative.
        /// </param>
        /// <param name="applyHillShadingToLand">
        /// If <see langword="true"/> a hill shading effect will be applied to areas with
        /// non-negative elevation.
        /// </param>
        /// <param name="applyHillShadingToOcean">
        /// If <see langword="true"/> a hill shading effect will be applied to areas with negative
        /// elevation.
        /// </param>
        /// <param name="hillScaleFactor">
        /// <para>
        /// An arbitrary factor which increases the apparent shading of slopes. Adjust as needed to
        /// give the appearance of sharper shading on low-slope maps, or decrease to reduce harsh
        /// shading on high-slope maps.
        /// </para>
        /// <para>
        /// Values between 0 and 1 decrease the degree of shading. Values greater than 1 increase
        /// it.
        /// </para>
        /// <para>
        /// Negative values are not allowed; these are truncated to 0 (no shading).
        /// </para>
        /// </param>
        /// <param name="hillScaleIsRelative">
        /// If <see langword="true"/> the <paramref name="hillScaleFactor"/> will increase with
        /// distance from sea level. Otherwise a uniform value will be used everywhere.
        /// </param>
        /// <param name="hillShadeMultiplier">
        /// An arbitrary value by which to multiply the hill shading's effect. Values less than 1
        /// are permitted but would be better accomplished by reducing <paramref
        /// name="hillScaleFactor"/>. Values greater than 1 have the effect of brightening the side
        /// of slopes facing the imagined light source, which can reduce the darkening effect of
        /// applying hill shading to a map. Values less than zero are treated as zero, and are the
        /// same as if hill shading was not applied (aside from the performance cost).
        /// </param>
        /// <returns>An image.</returns>
        public static Bitmap SurfaceMapToImage<T>(
            this T[][] surfaceMap,
            double[][] elevationMap,
            Func<T, double, (byte, byte, byte)> landConverter,
            Func<T, double, (byte, byte, byte)> oceanConverter,
            bool applyHillShadingToLand = false,
            bool applyHillShadingToOcean = false,
            double hillScaleFactor = 1,
            bool hillScaleIsRelative = false,
            double hillShadeMultiplier = 1)
        {
            var xLength = surfaceMap.Length;
            var yLength = xLength == 0 ? 0 : surfaceMap[0].Length;
            var image = new Bitmap(xLength, yLength);
            var data = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, image.PixelFormat);
            var pixelSize = Image.GetPixelFormatSize(data.PixelFormat) / 8;
            var length = data.Height * data.Stride;
            var bytes = new byte[length];
            System.Runtime.InteropServices.Marshal.Copy(data.Scan0, bytes, 0, length);

            var hillShadeMap = new float[0][];
            if (applyHillShadingToLand || applyHillShadingToOcean)
            {
                hillShadeMap = SurfaceMap.GetHillShadeMap(elevationMap, hillScaleFactor, hillScaleIsRelative);
                hillShadeMultiplier = Math.Max(0, hillShadeMultiplier);
            }

            for (var y = 0; y < image.Height; y++)
            {
                for (var x = 0; x < image.Width; x++)
                {
                    var pos = (y * data.Stride) + (x * pixelSize);
                    byte r, g, b;
                    if (elevationMap[x][y] >= 0)
                    {
                        (r, g, b) = landConverter(surfaceMap[x][y], elevationMap[x][y]);
                        if (applyHillShadingToLand)
                        {
                            var factor = hillShadeMap[x][y] * hillShadeMultiplier;
                            r = (byte)(r * factor).Clamp(0, 255);
                            g = (byte)(g * factor).Clamp(0, 255);
                            b = (byte)(b * factor).Clamp(0, 255);
                        }
                    }
                    else
                    {
                        (r, g, b) = oceanConverter(surfaceMap[x][y], elevationMap[x][y]);
                        if (applyHillShadingToOcean)
                        {
                            var factor = hillShadeMap[x][y] * hillShadeMultiplier;
                            r = (byte)(r * factor).Clamp(0, 255);
                            g = (byte)(g * factor).Clamp(0, 255);
                            b = (byte)(b * factor).Clamp(0, 255);
                        }
                    }
                    if (pixelSize == 4)
                    {
                        if (BitConverter.IsLittleEndian)
                        {
                            bytes[pos] = b;
                            bytes[pos + 1] = g;
                            bytes[pos + 2] = r;
                            bytes[pos + 3] = 255;
                        }
                        else
                        {
                            bytes[pos] = 255;
                            bytes[pos + 1] = r;
                            bytes[pos + 2] = g;
                            bytes[pos + 3] = b;
                        }
                    }
                    else if (BitConverter.IsLittleEndian)
                    {
                        bytes[pos] = b;
                        bytes[pos + 1] = g;
                        bytes[pos + 2] = r;
                    }
                    else
                    {
                        bytes[pos] = r;
                        bytes[pos + 1] = g;
                        bytes[pos + 2] = b;
                    }
                }
            }

            System.Runtime.InteropServices.Marshal.Copy(bytes, 0, data.Scan0, length);
            image.UnlockBits(data);
            return image;
        }

        /// <summary>
        /// Converts a surface map to an image.
        /// </summary>
        /// <param name="surfaceMap">The surface map to convert.</param>
        /// <param name="elevationMap">An elevation map.</param>
        /// <param name="landConverter">
        /// A function which accepts a value and the elevation at that point, and its X and Y
        /// indexes within the map, and returns the R, G, and B values which should be displayed for
        /// that value. Used for pixels whose elevation is non-negative.
        /// </param>
        /// <param name="oceanConverter">
        /// A function which accepts a value and the elevation at that point, and its X and Y
        /// indexes within the map, and returns the R, G, and B values which should be displayed for
        /// that value. Used for pixels whose elevation is negative.
        /// </param>
        /// <param name="applyHillShadingToLand">
        /// If <see langword="true"/> a hill shading effect will be applied to areas with
        /// non-negative elevation.
        /// </param>
        /// <param name="applyHillShadingToOcean">
        /// If <see langword="true"/> a hill shading effect will be applied to areas with negative
        /// elevation.
        /// </param>
        /// <param name="hillScaleFactor">
        /// <para>
        /// An arbitrary factor which increases the apparent shading of slopes. Adjust as needed to
        /// give the appearance of sharper shading on low-slope maps, or decrease to reduce harsh
        /// shading on high-slope maps.
        /// </para>
        /// <para>
        /// Values between 0 and 1 decrease the degree of shading. Values greater than 1 increase
        /// it.
        /// </para>
        /// <para>
        /// Negative values are not allowed; these are truncated to 0 (no shading).
        /// </para>
        /// </param>
        /// <param name="hillScaleIsRelative">
        /// If <see langword="true"/> the <paramref name="hillScaleFactor"/> will increase with
        /// distance from sea level. Otherwise a uniform value will be used everywhere.
        /// </param>
        /// <param name="hillShadeMultiplier">
        /// An arbitrary value by which to multiply the hill shading's effect. Values less than 1
        /// are permitted but would be better accomplished by reducing <paramref
        /// name="hillScaleFactor"/>. Values greater than 1 have the effect of brightening the side
        /// of slopes facing the imagined light source, which can reduce the darkening effect of
        /// applying hill shading to a map. Values less than zero are treated as zero, and are the
        /// same as if hill shading was not applied (aside from the performance cost).
        /// </param>
        /// <returns>An image.</returns>
        public static Bitmap SurfaceMapToImage<T>(
            this T[][] surfaceMap,
            double[][] elevationMap,
            Func<T, double, int, int, (byte, byte, byte)> landConverter,
            Func<T, double, int, int, (byte, byte, byte)> oceanConverter,
            bool applyHillShadingToLand = false,
            bool applyHillShadingToOcean = false,
            double hillScaleFactor = 1,
            bool hillScaleIsRelative = false,
            double hillShadeMultiplier = 1)
        {
            var xLength = surfaceMap.Length;
            var yLength = xLength == 0 ? 0 : surfaceMap[0].Length;
            var image = new Bitmap(xLength, yLength);
            var data = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, image.PixelFormat);
            var pixelSize = Image.GetPixelFormatSize(data.PixelFormat) / 8;
            var length = data.Height * data.Stride;
            var bytes = new byte[length];
            System.Runtime.InteropServices.Marshal.Copy(data.Scan0, bytes, 0, length);

            var hillShadeMap = new float[0][];
            if (applyHillShadingToLand || applyHillShadingToOcean)
            {
                hillShadeMap = SurfaceMap.GetHillShadeMap(elevationMap, hillScaleFactor, hillScaleIsRelative);
                hillShadeMultiplier = Math.Max(0, hillShadeMultiplier);
            }

            for (var y = 0; y < image.Height; y++)
            {
                for (var x = 0; x < image.Width; x++)
                {
                    var pos = (y * data.Stride) + (x * pixelSize);
                    byte r, g, b;
                    if (elevationMap[x][y] >= 0)
                    {
                        (r, g, b) = landConverter(surfaceMap[x][y], elevationMap[x][y], x, y);
                        if (applyHillShadingToLand)
                        {
                            var factor = hillShadeMap[x][y] * hillShadeMultiplier;
                            r = (byte)(r * factor).Clamp(0, 255);
                            g = (byte)(g * factor).Clamp(0, 255);
                            b = (byte)(b * factor).Clamp(0, 255);
                        }
                    }
                    else
                    {
                        (r, g, b) = oceanConverter(surfaceMap[x][y], elevationMap[x][y], x, y);
                        if (applyHillShadingToOcean)
                        {
                            var factor = hillShadeMap[x][y] * hillShadeMultiplier;
                            r = (byte)(r * factor).Clamp(0, 255);
                            g = (byte)(g * factor).Clamp(0, 255);
                            b = (byte)(b * factor).Clamp(0, 255);
                        }
                    }
                    if (pixelSize == 4)
                    {
                        if (BitConverter.IsLittleEndian)
                        {
                            bytes[pos] = b;
                            bytes[pos + 1] = g;
                            bytes[pos + 2] = r;
                            bytes[pos + 3] = 255;
                        }
                        else
                        {
                            bytes[pos] = 255;
                            bytes[pos + 1] = r;
                            bytes[pos + 2] = g;
                            bytes[pos + 3] = b;
                        }
                    }
                    else if (BitConverter.IsLittleEndian)
                    {
                        bytes[pos] = b;
                        bytes[pos + 1] = g;
                        bytes[pos + 2] = r;
                    }
                    else
                    {
                        bytes[pos] = r;
                        bytes[pos + 1] = g;
                        bytes[pos + 2] = b;
                    }
                }
            }

            System.Runtime.InteropServices.Marshal.Copy(bytes, 0, data.Scan0, length);
            image.UnlockBits(data);
            return image;
        }

        /// <summary>
        /// <para>
        /// Converts a temperature map to an image.
        /// </para>
        /// <para>
        /// Cool temperatures are rendered as a bluish color, with extreme values becoming purplish,
        /// then white. Moderate temperatures are rendered as greens. Warm temperatures are rendered
        /// as yellows, tending towards darker red as they increase.
        /// </para>
        /// <para>
        /// The color gradations do not follow a logical scale. Color transitions are selected
        /// arbitrarily to "look right" based on common weather mapping palettes.
        /// </para>
        /// </summary>
        /// <param name="temperatureMap">The temperature map to convert.</param>
        /// <returns>An image.</returns>
        public static Bitmap TemperatureMapToImage(this float[][] temperatureMap) => SurfaceMapToImage(temperatureMap, ToTemperatureColor);

        /// <summary>
        /// <para>
        /// Converts a temperature map to an image.
        /// </para>
        /// <para>
        /// This overload generates a single map based on the average value indicated by the <see
        /// cref="FloatRange"/> values. To instead generate multiple maps, see <see
        /// cref="TemperatureMapToImages(FloatRange[][])"/>.
        /// </para>
        /// <para>
        /// Cool temperatures are rendered as a bluish color, with extreme values becoming purplish,
        /// then white. Moderate temperatures are rendered as greens. Warm temperatures are rendered
        /// as yellows, tending towards darker red as they increase.
        /// </para>
        /// <para>
        /// The color gradations do not follow a logical scale. Color transitions are selected
        /// arbitrarily to "look right" based on common weather mapping palettes.
        /// </para>
        /// </summary>
        /// <param name="temperatureMap">The temperature map to convert.</param>
        /// <returns>An image.</returns>
        public static Bitmap TemperatureMapToImage(this FloatRange[][] temperatureMap) => SurfaceMapToImage(temperatureMap, ToTemperatureColor);

        /// <summary>
        /// <para>
        /// Converts a temperature map to a pair of images representing the minimum and maximum
        /// values.
        /// </para>
        /// <para>
        /// Cool temperatures are rendered as a bluish color, with extreme values becoming purplish,
        /// then white. Moderate temperatures are rendered as greens. Warm temperatures are rendered
        /// as yellows, tending towards darker red as they increase.
        /// </para>
        /// <para>
        /// The color gradations do not follow a logical scale. Color transitions are selected
        /// arbitrarily to "look right" based on common weather mapping palettes.
        /// </para>
        /// </summary>
        /// <param name="temperatureMap">The temperature map to convert.</param>
        /// <returns>A pair of images representing the minimum and maximum values.</returns>
        public static (Bitmap min, Bitmap max) TemperatureMapToImages(this FloatRange[][] temperatureMap) => SurfaceMapToImages(temperatureMap, ToTemperatureColor);

        /// <summary>
        /// Converts a value to an RGB color based on its biome.
        /// </summary>
        /// <param name="value">
        /// The biome to render.
        /// </param>
        /// <returns>
        /// Bytes representing the red, green, and blue values of a pixel corresponding to the
        /// biome value provided.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Polar => (240, 250, 255)
        /// </para>
        /// <para>
        /// Tundra => (160, 240, 215)
        /// </para>
        /// <para>
        /// Alpine => (200, 215, 230)
        /// </para>
        /// <para>
        /// Subalpine => (125, 140, 160)
        /// </para>
        /// <para>
        /// LichenWoodland => (45, 100, 90)
        /// </para>
        /// <para>
        /// ConiferousForest => (65, 80, 70)
        /// </para>
        /// <para>
        /// MixedForest => (52, 95, 55)
        /// </para>
        /// <para>
        /// Steppe => (140, 160, 100)
        /// </para>
        /// <para>
        /// ColdDesert => (150, 130, 100)
        /// </para>
        /// <para>
        /// DeciduousForest => (40, 110, 40)
        /// </para>
        /// <para>
        /// Shrubland => (90, 110, 70)
        /// </para>
        /// <para>
        /// HotDesert => (210, 205, 115)
        /// </para>
        /// <para>
        /// Savanna => (120, 180, 60)
        /// </para>
        /// <para>
        /// MonsoonForest => (20, 135, 80)
        /// </para>
        /// <para>
        /// RainForest => (35, 145, 35)
        /// </para>
        /// <para>
        /// Sea => (120, 180, 240)
        /// </para>
        /// <para>
        /// All others => (170, 170, 170)
        /// </para>
        /// </remarks>
        public static (byte, byte, byte) ToBiomeColor(this BiomeType value) => value switch
        {
            BiomeType.Polar => (240, 250, 255),
            BiomeType.Tundra => (160, 240, 215),
            BiomeType.Alpine => (200, 215, 230),
            BiomeType.Subalpine => (125, 140, 160),
            BiomeType.LichenWoodland => (45, 100, 90),
            BiomeType.ConiferousForest => (65, 80, 70),
            BiomeType.MixedForest => (52, 95, 55),
            BiomeType.Steppe => (140, 160, 100),
            BiomeType.ColdDesert => (150, 130, 100),
            BiomeType.DeciduousForest => (40, 110, 40),
            BiomeType.Shrubland => (90, 110, 70),
            BiomeType.HotDesert => (210, 205, 115),
            BiomeType.Savanna => (120, 180, 60),
            BiomeType.MonsoonForest => (20, 135, 80),
            BiomeType.RainForest => (35, 145, 35),
            BiomeType.Sea => (120, 180, 240),
            _ => (170, 170, 170),
        };

        /// <summary>
        /// Converts a <see cref="Bitmap"/> to a byte array.
        /// </summary>
        /// <param name="image">A <see cref="Bitmap"/>.</param>
        /// <returns>A byte array.</returns>
        public static byte[]? ToByteArray(this Bitmap? image)
        {
            if (image is null)
            {
                return new byte[0];
            }

            using var stream = new MemoryStream();
            image.Save(stream, ImageFormat.Bmp);
            return stream.ToArray();
        }

        /// <summary>
        /// <para>
        /// Converts a value to an RGB color based on its elevation.
        /// </para>
        /// <para>
        /// Positive values are rendered as a reddish color, with 0 being rendered as white and 1
        /// being rendered as a dark red. Negative values are rendered as a bluish color, with 0
        /// being rendered as white and 1 being rendered as a deep blue.
        /// </para>
        /// <para>
        /// The color scale is logarithmic, in order to better emphasize differences at low
        /// elevations without drowning out differences at extremes.
        /// </para>
        /// </summary>
        /// <param name="value">
        /// The normalized elevation to render. Values between 0 and 1 (inclusive) are expected.
        /// </param>
        /// <returns>
        /// Bytes representing the red, green, and blue values of a pixel corresponding to the
        /// normalized elevation value provided.
        /// </returns>
        public static (byte r, byte g, byte b) ToElevationColor(this double value) => InterpColorRange(value,
            new (double, (double, double, double))[] {
                (-0.9, (0, 10, 100)),
                (-0.09, (0, 105, 178)),
                (-0.009, (0, 153, 217)),
                (-0.0009, (0, 177, 236)),
                (-0.00009, (0, 200, 255)),
                (0, (255, 255, 255)),
                (0.002, (255, 255, 195)),
                (0.008, (245, 225, 170)),
                (0.025, (240, 195, 145)),
                (0.1, (225, 135, 95)),
                (0.4, (200, 20, 5)),
                (0.9, (50, 0, 0)),
            });

        /// <summary>
        /// Converts a byte array containing an image to a <see cref="Bitmap"/>.
        /// </summary>
        /// <param name="bytes">A byte array containing an image.</param>
        /// <returns>A <see cref="Bitmap"/>.</returns>
        public static Bitmap? ToImage(this byte[]? bytes)
        {
            if (bytes is null)
            {
                return null;
            }
            using var stream = new MemoryStream(bytes);
            return new Bitmap(stream);
        }

        /// <summary>
        /// <para>
        /// Converts a value to an RGB color based on its precipitation.
        /// </para>
        /// <para>
        /// Values are rendered as a greenish color, with low values being yellow, tending towards
        /// darker green as they increase.
        /// </para>
        /// <para>
        /// The color gradations follow an increasing scale, with each color shade representing
        /// double the amount of precipitation as the previous rank.
        /// </para>
        /// </summary>
        /// <param name="value">
        /// The amount of precipitation to render, in mm.
        /// </param>
        /// <returns>
        /// Bytes representing the red, green, and blue values of a pixel corresponding to the
        /// precipitation value provided.
        /// </returns>
        public static (byte, byte, byte) ToPrecipitationColor(this double value) => InterpColorRange(value,
            new (double, (double, double, double))[] {
                (125, (200, 215, 100)),
                (250, (170, 200, 80)),
                (500, (95, 170, 40)),
                (1000, (30, 160, 25)),
                (2000, (15, 90, 35)),
                (4000, (15, 90, 80)),
                (8000, (5, 110, 75)),
            });

        /// <summary>
        /// <para>
        /// Converts a value to an RGB color based on its temperature.
        /// </para>
        /// <para>
        /// Cool temperatures are rendered as a bluish color, with extreme values becoming purplish,
        /// then white. Moderate temperatures are rendered as greens. Warm temperatures are rendered
        /// as yellows, tending towards darker red as they increase.
        /// </para>
        /// <para>
        /// The color gradations do not follow a logical scale. Color transitions are selected
        /// arbitrarily to "look right" based on common weather mapping palettes.
        /// </para>
        /// </summary>
        /// <param name="value">
        /// The temperature to render, in K.
        /// </param>
        /// <returns>
        /// Bytes representing the red, green, and blue values of a pixel corresponding to the
        /// temperature value provided.
        /// </returns>
        public static (byte, byte, byte) ToTemperatureColor(this float value) => InterpColorRange(value - 273.15,
            new (double, (double, double, double))[] {
                (-60, (170, 170, 170)),
                (-40, (255, 255, 255)),
                (-30, (130, 10, 155)),
                (-20, (5, 30, 120)),
                (0, (30, 210, 200)),
                (5, (5, 165, 45)),
                (20, (225, 215, 0)),
                (30, (110, 5, 0)),
                (40, (50, 0, 0)),
            });

        private static byte DoubleToRgb(double value)
        {
            var normalized = (value + 1) / 2;
            return (byte)Math.Round(normalized * 255).Clamp(0, 255);
        }

        private static byte FloatToRgb(float value) => (byte)Math.Round(value * 255).Clamp(0, 255);

        private static byte InterpColor(double value, double lowValue, double highValue, double lowColor, double highColor)
            => (byte)lowColor.Lerp(highColor, (value - lowValue) / (highValue - lowValue));

        private static (byte r, byte g, byte b) InterpColorRange(double value, (double max, (double r, double g, double b) color)[] ranges)
        {
            for (var i = 0; i < ranges.Length; i++)
            {
                if (value <= ranges[i].max)
                {
                    if (i == 0)
                    {
                        return ((byte)ranges[i].color.r.Clamp(0, 255), (byte)ranges[i].color.g.Clamp(0, 255), (byte)ranges[i].color.b.Clamp(0, 255));
                    }
                    return (InterpColor(value, ranges[i - 1].max, ranges[i].max, ranges[i - 1].color.r, ranges[i].color.r),
                        InterpColor(value, ranges[i - 1].max, ranges[i].max, ranges[i - 1].color.g, ranges[i].color.g),
                        InterpColor(value, ranges[i - 1].max, ranges[i].max, ranges[i - 1].color.b, ranges[i].color.b));
                }
            }
            return ((byte)ranges[^1].color.r.Clamp(0, 255), (byte)ranges[^1].color.g.Clamp(0, 255), (byte)ranges[^1].color.b.Clamp(0, 255));
        }

        private static Bitmap Resize(this Bitmap image, int width, int height)
        {
            var rectangle = new Rectangle(0, 0, width, height);
            var resized = new Bitmap(width, height);
            resized.SetResolution(image.HorizontalResolution, image.VerticalResolution);
            using (var g = Graphics.FromImage(resized))
            {
                g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                using var attr = new ImageAttributes();
                attr.SetWrapMode(System.Drawing.Drawing2D.WrapMode.TileFlipXY);
                g.DrawImage(image, rectangle, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, attr);
            }

            return resized;
        }

        private static double RgbToDouble(byte r, byte g, byte b)
        {
            var result = (r + b + g) / 765.0f; // 3 * 255
            result = (result * 2) - 1;
            return result.Clamp(0, 1);
        }

        private static float RgbToFloat(byte r, byte g, byte b)
        {
            var result = (r + b + g) / 765.0f; // 3 * 255
            return result.Clamp(0, 1);
        }
    }
}
