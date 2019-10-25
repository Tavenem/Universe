using NeverFoundry.MathAndScience;
using System;
using System.Drawing;
using System.Drawing.Imaging;

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
        /// <param name="destWidth">An optional width to which the image will be stretched. If left
        /// <see langword="null"/> the source width will be retained.</param>
        /// <param name="destHeight">An optional height to which the image will be stretched. If
        /// left <see langword="null"/> the source height will be retained.</param>
        /// <returns>A surface map.</returns>
        public static double[,] ImageToDoubleSurfaceMap(this Bitmap image, int? destWidth = null, int? destHeight = null)
        {
            var img = image;
            var resized = false;
            if (destWidth.HasValue || destHeight.HasValue)
            {
                img = image.Resize(destWidth ?? image.Width, destHeight ?? image.Height);
                resized = true;
            }
            var surfaceMap = new double[img.Width, img.Height];

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
                    b = bytes[pos];
                    g = bytes[pos + 1];
                    r = bytes[pos + 2];
                    surfaceMap[x, y] = RgbToDouble(r, g, b);
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
        /// Converts an image to a surface map.
        /// </summary>
        /// <param name="image">The image to convert.</param>
        /// <param name="destWidth">An optional width to which the image will be stretched. If left
        /// <see langword="null"/> the source width will be retained.</param>
        /// <param name="destHeight">An optional height to which the image will be stretched. If
        /// left <see langword="null"/> the source height will be retained.</param>
        /// <returns>A surface map.</returns>
        public static float[,] ImageToFloatSurfaceMap(this Bitmap image, int? destWidth = null, int? destHeight = null)
        {
            var img = image;
            var resized = false;
            if (destWidth.HasValue || destHeight.HasValue)
            {
                img = image.Resize(destWidth ?? image.Width, destHeight ?? image.Height);
                resized = true;
            }
            var surfaceMap = new float[img.Width, img.Height];

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
                    b = bytes[pos];
                    g = bytes[pos + 1];
                    r = bytes[pos + 2];
                    surfaceMap[x, y] = RgbToFloat(r, g, b);
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
        /// Converts a pair of images to a surface map.
        /// </summary>
        /// <param name="imageMin">The image with the minimum values to convert.</param>
        /// <param name="imageMax">The image with the maximum values to convert.</param>
        /// <param name="destWidth">An optional width to which the image will be stretched. If left
        /// <see langword="null"/> the source width will be retained.</param>
        /// <param name="destHeight">An optional height to which the image will be stretched. If
        /// left <see langword="null"/> the source height will be retained.</param>
        /// <returns>A surface map.</returns>
        public static FloatRange[,] ImagesToFloatRangeSurfaceMap(this Bitmap imageMin, Bitmap imageMax, int? destWidth = null, int? destHeight = null)
        {
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
            var minMap = new float[xLength, yLength];
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
                    b = bytesMin[pos];
                    g = bytesMin[pos + 1];
                    r = bytesMin[pos + 2];
                    minMap[x, y] = RgbToFloat(r, g, b);
                }
            }
            imgMin.UnlockBits(dataMin);
            if (minResized)
            {
                imgMin.Dispose();
            }

            var maxMap = new float[xLength, yLength];
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
                    b = bytesMax[pos];
                    g = bytesMax[pos + 1];
                    r = bytesMax[pos + 2];
                    maxMap[x, y] = RgbToFloat(r, g, b);
                }
            }
            imgMax.UnlockBits(dataMax);
            if (maxResized)
            {
                imgMax.Dispose();
            }

            var surfaceMap = new FloatRange[xLength, yLength];
            for (var y = 0; y < yLength; y++)
            {
                for (var x = 0; x < xLength; x++)
                {
                    surfaceMap[x, y] = new FloatRange(minMap[x, y], maxMap[x, y]);
                }
            }
            return surfaceMap;
        }

        /// <summary>
        /// Converts a surface map to a grayscale image.
        /// </summary>
        /// <param name="surfaceMap">The surface map to convert.</param>
        /// <returns>A grayscale image.</returns>
        public static Bitmap SurfaceMapToImage(this float[,] surfaceMap)
        {
            var xLength = surfaceMap.GetLength(0);
            var yLength = surfaceMap.GetLength(1);
            var image = new Bitmap(xLength, yLength);
            var data = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, image.PixelFormat);
            var pixelSize = Image.GetPixelFormatSize(data.PixelFormat) / 8;
            var length = data.Height * data.Stride;
            var bytes = new byte[length];
            System.Runtime.InteropServices.Marshal.Copy(data.Scan0, bytes, 0, length);

            byte rgb;
            for (var y = 0; y < image.Height; y++)
            {
                for (var x = 0; x < image.Width; x++)
                {
                    var pos = (y * data.Stride) + (x * pixelSize);
                    rgb = FloatToRgb(surfaceMap[x, y]);
                    bytes[pos] = rgb;
                    bytes[pos + 1] = rgb;
                    bytes[pos + 2] = rgb;
                }
            }

            System.Runtime.InteropServices.Marshal.Copy(bytes, 0, data.Scan0, length);
            image.UnlockBits(data);
            return image;
        }

        /// <summary>
        /// Converts a surface map to a grayscale image.
        /// </summary>
        /// <param name="surfaceMap">The surface map to convert.</param>
        /// <returns>A grayscale image.</returns>
        public static Bitmap SurfaceMapToImage(this double[,] surfaceMap)
        {
            var xLength = surfaceMap.GetLength(0);
            var yLength = surfaceMap.GetLength(1);
            var image = new Bitmap(xLength, yLength);
            var data = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, image.PixelFormat);
            var pixelSize = Image.GetPixelFormatSize(data.PixelFormat) / 8;
            var length = data.Height * data.Stride;
            var bytes = new byte[length];
            System.Runtime.InteropServices.Marshal.Copy(data.Scan0, bytes, 0, length);

            byte rgb;
            for (var y = 0; y < image.Height; y++)
            {
                for (var x = 0; x < image.Width; x++)
                {
                    var pos = (y * data.Stride) + (x * pixelSize);
                    rgb = DoubleToRgb(surfaceMap[x, y]);
                    bytes[pos] = rgb;
                    bytes[pos + 1] = rgb;
                    bytes[pos + 2] = rgb;
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
        /// <returns>A pair of grayscale images representing the minimum and maximum
        /// values.</returns>
        public static (Bitmap min, Bitmap max) SurfaceMapToImages(this FloatRange[,] surfaceMap)
        {
            var xLength = surfaceMap.GetLength(0);
            var yLength = surfaceMap.GetLength(1);
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

            byte rgb;
            for (var y = 0; y < imageMin.Height; y++)
            {
                for (var x = 0; x < imageMin.Width; x++)
                {
                    var pos = (y * dataMin.Stride) + (x * pixelSize);

                    rgb = FloatToRgb(surfaceMap[x, y].Min);
                    bytesMin[pos] = rgb;
                    bytesMin[pos + 1] = rgb;
                    bytesMin[pos + 2] = rgb;

                    rgb = FloatToRgb(surfaceMap[x, y].Max);
                    bytesMax[pos] = rgb;
                    bytesMax[pos + 1] = rgb;
                    bytesMax[pos + 2] = rgb;
                }
            }

            System.Runtime.InteropServices.Marshal.Copy(bytesMin, 0, dataMin.Scan0, length);
            System.Runtime.InteropServices.Marshal.Copy(bytesMax, 0, dataMax.Scan0, length);
            imageMin.UnlockBits(dataMin);
            imageMax.UnlockBits(dataMax);
            return (imageMin, imageMax);
        }

        private static byte DoubleToRgb(double value)
        {
            var normalized = (value + 1) / 2;
            return (byte)Math.Round(normalized * 255).Clamp(0, 255);
        }

        private static byte FloatToRgb(float value) => (byte)Math.Round(value * 255).Clamp(0, 255);

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
