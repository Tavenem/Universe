using NeverFoundry.MathAndScience;
using NeverFoundry.MathAndScience.Chemistry;
using NeverFoundry.WorldFoundry.Climate;
using NeverFoundry.WorldFoundry.Space;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Transforms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace NeverFoundry.WorldFoundry.Maps
{
    /// <summary>
    /// Static methods related to images with surface map data.
    /// </summary>
    public static class SurfaceMapImage
    {
        /// <summary>
        /// The scale of temperature map values.
        /// </summary>
        public const int TemperatureScaleFactor = 1000;

        private static readonly (double, (double, double, double))[] _ElevationColorProfile = new (double, (double, double, double))[] {
            (-1, (42, 72, 84)),
            (-0.36, (146, 208, 233)),
            (-0.3, (160, 209, 242)),
            (-0.2, (168, 218, 243)),
            (-0.09, (180, 220, 245)),
            (-0.045, (186, 228, 250)),
            (-0.02, (199, 230, 250)),
            (-0.009, (210, 238, 252)),
            (-0.00001, (221, 241, 252)),
            (0, (255, 255, 255)),
            (0.00001, (179, 193, 168)),
            (0.006, (156, 180, 146)),
            (0.01, (175, 192, 158)),
            (0.02, (194, 204, 169)),
            (0.06, (211, 216, 184)),
            (0.12, (231, 231, 193)),
            (0.175, (249, 244, 214)),
            (0.235, (221, 216, 178)),
            (0.3, (196, 188, 149)),
            (0.35, (175, 158, 115)),
            (0.4, (144, 137, 109)),
            (0.47, (121, 112, 95)),
            (0.53, (95, 88, 80)),
            (0.59, (126, 114, 114)),
            (0.65, (152, 143, 144)),
            (0.7, (176, 170, 170)),
            (0.824, (203, 197, 197)),
            (0.94, (227, 225, 226)),
            (1, (255, 255, 255)),
        };
        private static readonly (double, (double, double, double))[] _PrecipitationColorProfile = new (double, (double, double, double))[] {
            (0.01, (200, 215, 100)),
            (0.021, (170, 200, 80)),
            (0.043, (95, 170, 40)),
            (0.085, (30, 160, 25)),
            (0.169, (15, 90, 35)),
            (0.337, (15, 90, 80)),
            (0.674, (5, 110, 75)),
        };
        private static readonly IResampler _Resampler = KnownResamplers.Spline;
        private static readonly double _SinQuarterPi = Math.Sin(MathAndScience.Constants.Doubles.MathConstants.QuarterPI);
        private static readonly double _SinQuarterPiSquared = _SinQuarterPi * _SinQuarterPi;
        private static readonly (double, (double, double, double))[] _TemperatureColorProfile = new (double, (double, double, double))[] {
            (-60, (170, 170, 170)),
            (-40, (255, 255, 255)),
            (-30, (130, 10, 155)),
            (-20, (5, 30, 120)),
            (0, (30, 210, 200)),
            (5, (5, 165, 45)),
            (20, (225, 215, 0)),
            (30, (110, 5, 0)),
            (40, (50, 0, 0)),
        };

        /// <summary>
        /// Produces an average of the given set of images.
        /// </summary>
        /// <param name="images">A set of images.</param>
        /// <returns>A single image that averages the inputs.</returns>
        /// <exception cref="ArgumentException" />
        /// <remarks>
        /// All images must have the same dimensions.
        /// </remarks>
        public static Image<L16> AverageImages(params Image<L16>[] images)
        {
            if (images.Length == 0)
            {
                throw new ArgumentException($"{nameof(images)} cannot be empty");
            }

            if (images.Length == 1)
            {
                return images[0].Clone();
            }

            var yResolution = images[0].Height;
            var xResolution = images[0].Width;
            if (images.Any(x => x.Height != yResolution || x.Width != xResolution))
            {
                throw new ArgumentException($"{nameof(images)} must all have the same dimensions");
            }

            var combined = new Image<L16>(xResolution, yResolution);

            if (images.Length == 2)
            {
                for (var y = 0; y < yResolution; y++)
                {
                    var img1Span = images[0].GetPixelRowSpan(y);
                    var img2Span = images[1].GetPixelRowSpan(y);
                    var combinedSpan = combined.GetPixelRowSpan(y);
                    for (var x = 0; x < xResolution; x++)
                    {
                        combinedSpan[x] = img1Span[x].Average(img2Span[x]);
                    }
                }
                return combined;
            }

            for (var y = 0; y < yResolution; y++)
            {
                var combinedSpan = combined.GetPixelRowSpan(y);
                for (var x = 0; x < xResolution; x++)
                {
                    var sum = 0.0;
                    for (var i = 0; i < images.Length; i++)
                    {
                        sum += images[i][x, y].PackedValue;
                    }
                    combinedSpan[x] = new L16((ushort)Math.Round(sum / images.Length).Clamp(0, ushort.MaxValue));
                }
            }

            return combined;
        }

        /// <summary>
        /// Converts a biome map to an image.
        /// </summary>
        /// <param name="biomeMap">The biome map to convert.</param>
        /// <returns>An image.</returns>
        public static Image<Rgba32> BiomeMapToImage(this BiomeType[][] biomeMap) => SurfaceMapToImage(biomeMap, ToBiomeColor);

        /// <summary>
        /// Converts a biome map to an image.
        /// </summary>
        /// <param name="biomeMap">The biome map to convert.</param>
        /// <param name="planet">The planet being mapped.</param>
        /// <param name="elevationMap">An elevation map.</param>
        /// <param name="showOcean">
        /// If <see langword="true"/> the biome color for the sea will be shown;
        /// otherwise the elevation coloration will be applied to regions below sea level.
        /// </param>
        /// <param name="mapProjection">
        /// <para>
        /// The map projection used in <paramref name="biomeMap"/>.
        /// </para>
        /// <para>
        /// Only required when non-default.
        /// </para>
        /// </param>
        /// <param name="elevationMapProjection">
        /// <para>
        /// The map projection used in <paramref name="elevationMap"/>.
        /// </para>
        /// <para>
        /// Only required when non-default.
        /// </para>
        /// </param>
        /// <param name="hillShading">
        /// <para>
        /// Options for a hill shading effect to be applied to the map.
        /// </para>
        /// <para>
        /// If left <see langword="null"/> no hill shading will be applied.
        /// </para>
        /// </param>
        /// <returns>An image.</returns>
        public static Image<Rgba32> BiomeMapToImage(
            this BiomeType[][] biomeMap,
            Planetoid planet,
            Image<L16> elevationMap,
            bool showOcean = true,
            MapProjectionOptions? mapProjection = null,
            MapProjectionOptions? elevationMapProjection = null,
            HillShadingOptions? hillShading = null) => SurfaceMapToImage(
                biomeMap,
                planet,
                elevationMap,
                (v, _) => v.ToBiomeColor(),
                (v, e) => showOcean
                    ? v.ToBiomeColor()
                    : e.ToElevationColor(),
                mapProjection,
                elevationMapProjection,
                hillShading);

        /// <summary>
        /// Applies a colorization function to an elevation map image.
        /// </summary>
        /// <param name="elevationMap">The elevation map to convert.</param>
        /// <param name="converter">
        /// A function which accepts a <see cref="L16"/> pixel, and returns the <see cref="Rgba32"/>
        /// pixel which should be substituted.
        /// </param>
        /// <returns>An image.</returns>
        public static Image<Rgba32> ElevationMapToImage(
            this Image<L16> elevationMap,
            Func<L16, Rgba32> converter)
        {
            var xLength = elevationMap.Width;
            var yLength = elevationMap.Height;
            var destination = new Image<Rgba32>(xLength, yLength);

            for (var y = 0; y < yLength; y++)
            {
                var sourceRowSpan = elevationMap.GetPixelRowSpan(y);
                var destinationRowSpan = destination.GetPixelRowSpan(y);
                for (var x = 0; x < xLength; x++)
                {
                    destinationRowSpan[x] = converter(sourceRowSpan[x]);
                }
            }
            return destination;
        }

        /// <summary>
        /// Applies a colorization function to an elevation map image.
        /// </summary>
        /// <param name="elevationMap">The elevation map to convert.</param>
        /// <param name="planet">The planet being mapped.</param>
        /// <param name="converter">
        /// A function which accepts a <see cref="L16"/> pixel, and returns the <see cref="Rgba32"/>
        /// pixel which should be substituted.
        /// </param>
        /// <param name="hillShading">
        /// <para>
        /// Options for a hill shading effect to be applied to the map.
        /// </para>
        /// <para>
        /// If left <see langword="null"/> no hill shading will be applied.
        /// </para>
        /// </param>
        /// <returns>An image.</returns>
        public static Image<Rgba32> ElevationMapToImage(
            this Image<L16> elevationMap,
            Planetoid planet,
            Func<L16, Rgba32> converter,
            HillShadingOptions? hillShading = null)
        {
            var xLength = elevationMap.Width;
            var yLength = elevationMap.Height;
            var destination = new Image<Rgba32>(xLength, yLength);

            for (var y = 0; y < yLength; y++)
            {
                var sourceRowSpan = elevationMap.GetPixelRowSpan(y);
                var destinationRowSpan = destination.GetPixelRowSpan(y);
                for (var x = 0; x < xLength; x++)
                {
                    destinationRowSpan[x] = converter(sourceRowSpan[x])
                        .ApplyHillShading(elevationMap, x, y, hillShading, sourceRowSpan[x].GetValueFromPixel_PosNeg() - planet.NormalizedSeaLevel > 0);
                }
            }
            return destination;
        }

        /// <summary>
        /// Applies a colorization function to an elevation map image.
        /// </summary>
        /// <param name="elevationMap">The elevation map to convert.</param>
        /// <param name="planet">The planet being mapped.</param>
        /// <param name="landConverter">
        /// <para>
        /// A function which accepts a <see cref="L16"/> pixel, and returns the <see cref="Rgba32"/>
        /// pixel which should be substituted. Used for pixels whose elevation is non-negative.
        /// </para>
        /// <para>
        /// If left <see langword="null"/> pixels with positive elevations will be unaltered.
        /// </para>
        /// </param>
        /// <param name="oceanConverter">
        /// <para>
        /// A function which accepts a <see cref="L16"/> pixel, and returns the <see cref="Rgba32"/>
        /// pixel which should be substituted. Used for pixels whose elevation is negative.
        /// </para>
        /// <para>
        /// If left <see langword="null"/> pixels with negative elevations will be unaltered.
        /// </para>
        /// </param>
        /// <param name="hillShading">
        /// <para>
        /// Options for a hill shading effect to be applied to the map.
        /// </para>
        /// <para>
        /// If left <see langword="null"/> no hill shading will be applied.
        /// </para>
        /// </param>
        /// <returns>An image.</returns>
        public static Image<Rgba32> ElevationMapToImage(
            this Image<L16> elevationMap,
            Planetoid planet,
            Func<L16, Rgba32> landConverter,
            Func<L16, Rgba32> oceanConverter,
            HillShadingOptions? hillShading = null)
        {
            var xLength = elevationMap.Width;
            var yLength = elevationMap.Height;
            var destination = new Image<Rgba32>(xLength, yLength);

            for (var y = 0; y < yLength; y++)
            {
                var sourceRowSpan = elevationMap.GetPixelRowSpan(y);
                var destinationRowSpan = destination.GetPixelRowSpan(y);
                for (var x = 0; x < xLength; x++)
                {
                    var normalElevation = sourceRowSpan[x].GetValueFromPixel_PosNeg() - planet.NormalizedSeaLevel;
                    if (normalElevation >= 0)
                    {
                        destinationRowSpan[x] = landConverter(sourceRowSpan[x])
                            .ApplyHillShading(elevationMap, x, y, hillShading, true);
                    }
                    else
                    {
                        destinationRowSpan[x] = oceanConverter(sourceRowSpan[x])
                            .ApplyHillShading(elevationMap, x, y, hillShading, false);
                    }
                }
            }
            return destination;
        }

        /// <summary>
        /// Applies standard elevation shading to an elevation map image.
        /// </summary>
        /// <param name="elevationMap">The elevation map to convert.</param>
        /// <param name="planet">The planet being mapped.</param>
        /// <param name="hillShading">
        /// <para>
        /// Options for a hill shading effect to be applied to the map.
        /// </para>
        /// <para>
        /// If left <see langword="null"/> no hill shading will be applied.
        /// </para>
        /// </param>
        /// <returns>An image.</returns>
        public static Image<Rgba32> ElevationMapToImage(
            this Image<L16> elevationMap,
            Planetoid planet,
            HillShadingOptions? hillShading = null) => ElevationMapToImage(
                elevationMap,
                planet,
                e => e.ToElevationColor(planet),
                hillShading);

        /// <summary>
        /// Creates a new map projection from the provided map image.
        /// </summary>
        /// <param name="image">The original map image.</param>
        /// <param name="resolution">The target vertical resolution.</param>
        /// <param name="originalProjection">
        /// <para>
        /// The projection used in the original image.
        /// </para>
        /// <para>
        /// If omitted an equirectangular projection of the full globe is assumed.
        /// </para>
        /// </param>
        /// <param name="newProjection">
        /// <para>
        /// The desired map projection.
        /// </para>
        /// <para>
        /// If omitted an equirectangular projection of the full globe is assumed.
        /// </para>
        /// </param>
        /// <returns>
        /// A new map projection image.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="newProjection"/> specifies latitudes or longitudes not included in
        /// <paramref name="originalProjection"/>
        /// </exception>
        public static Image<L16>? GetMapProjection(
            this Image<L16> image,
            int resolution,
            MapProjectionOptions? originalProjection = null,
            MapProjectionOptions? newProjection = null)
        {
            var originalOptions = originalProjection ?? MapProjectionOptions.Default;
            var xResolution = (int)Math.Floor(resolution * newProjection?.AspectRatio ?? 2);
            if (newProjection?.Range.HasValue != true
                || newProjection.Range!.Value <= 0
                || newProjection.Range.Value >= Math.PI)
            {
                if (originalOptions.Range > 0
                    && originalOptions.Range.Value < Math.PI)
                {
                    throw new ArgumentException($"{nameof(newProjection)} specifies latitudes or longitudes not included in {nameof(originalProjection)}");
                }
                return image.Clone(x => x.Resize(xResolution, resolution, _Resampler));
            }
            var (newNorthLatitude, newWestLongitude, newSouthLatitude, newEastLongitude) = GetBounds(newProjection);
            var originalScale = SurfaceMap.GetScale(image.Height, originalOptions.Range, originalOptions.EqualArea);
            var (originalXMin, originalYMin) = originalOptions.EqualArea
                ? SurfaceMap.GetCylindricalEqualAreaProjectionFromLatLongWithScale(
                    newNorthLatitude, newWestLongitude,
                    image.Width,
                    image.Height,
                    originalScale,
                    originalOptions)
                : SurfaceMap.GetEquirectangularProjectionFromLatLongWithScale(
                    newNorthLatitude, newWestLongitude,
                    image.Width,
                    image.Height,
                    originalScale,
                    originalOptions);
            var (originalXMax, originalYMax) = originalOptions.EqualArea
                ? SurfaceMap.GetCylindricalEqualAreaProjectionFromLatLongWithScale(
                    newSouthLatitude, newEastLongitude,
                    image.Width,
                    image.Height,
                    originalScale,
                    originalOptions)
                : SurfaceMap.GetEquirectangularProjectionFromLatLongWithScale(
                    newSouthLatitude, newEastLongitude,
                    image.Width,
                    image.Height,
                    originalScale,
                    originalOptions);
            if (originalXMin < 0
                || originalYMin < 0
                || originalXMax > image.Width
                || originalYMax > image.Height)
            {
                throw new ArgumentException($"{nameof(newProjection)} specifies latitudes or longitudes not included in {nameof(originalProjection)}");
            }
            return image.Clone(x => x
                .Crop(new Rectangle(originalXMin, originalYMin, originalXMax - originalXMin, originalYMax - originalYMin))
                .Resize(xResolution, resolution, _Resampler));
        }

        /// <summary>
        /// Gets the range of luminance values represented by this image.
        /// </summary>
        /// <param name="image">An image.</param>
        /// <param name="canBeNegative">
        /// Whether converted values should be normalized to the range [-1..1] (rather than [0..1]).
        /// </param>
        /// <returns>
        /// The minimum, maximum, and average luminance values of this image.
        /// </returns>
        public static FloatRange GetRange(this Image<L16> image, bool canBeNegative = false)
        {
            float min, max;
            var minV = ushort.MaxValue;
            var maxV = ushort.MinValue;
            var sum = 0.0f;
            var yLength = image.Height;
            var xLength = image.Width;
            if (canBeNegative)
            {
                for (var y = 0; y < yLength; y++)
                {
                    var span = image.GetPixelRowSpan(y);
                    for (var x = 0; x < xLength; x++)
                    {
                        var value = span[x].PackedValue;
                        minV = Math.Min(minV, value);
                        maxV = Math.Max(maxV, value);
                        sum += value;
                    }
                }
                min = (2.0f * minV / ushort.MaxValue) - 1;
                max = (2.0f * maxV / ushort.MaxValue) - 1;
                sum = (2.0f * sum / ushort.MaxValue) - 1;
            }
            else
            {
                for (var y = 0; y < yLength; y++)
                {
                    var span = image.GetPixelRowSpan(y);
                    for (var x = 0; x < xLength; x++)
                    {
                        var value = span[x].PackedValue;
                        minV = Math.Min(minV, value);
                        maxV = Math.Max(maxV, value);
                        sum += value;
                    }
                }
                min = (float)minV / ushort.MaxValue;
                max = (float)maxV / ushort.MaxValue;
                sum /= ushort.MaxValue;
            }
            return new FloatRange(min, sum / (yLength * xLength), max);
        }

        /// <summary>
        /// Generates a snowfall map from a precipitation map and a temperature map.
        /// </summary>
        /// <param name="precipitationMap">A precipitation map.</param>
        /// <param name="temperatureMap">A temperature map.</param>
        /// <param name="projection">
        /// <para>
        /// The projection used in the existing maps. Used as the projection of the new snowfall
        /// map.
        /// </para>
        /// <para>
        /// If omitted an equirectangular projection of the full globe is assumed.
        /// </para>
        /// </param>
        /// <returns>A snowfall map.</returns>
        public static Image<L16> GetSnowfallMap(
            this Image<L16> precipitationMap,
            Image<L16> temperatureMap,
            MapProjectionOptions? projection = null)
        {
            var options = projection ?? MapProjectionOptions.Default;
            var yLength = precipitationMap.Height;
            var xLength = precipitationMap.Width;
            var tempYLength = temperatureMap.Height;
            var tempXLength = temperatureMap.Width;
            var match = tempXLength == xLength
                && tempYLength == yLength;
            var snowImg = new Image<L16>(xLength, yLength);
            var scale = SurfaceMap.GetScale(yLength, options.Range, options.EqualArea);
            var tScale = SurfaceMap.GetScale(tempYLength, options.Range, options.EqualArea);
            var xToX = new Dictionary<int, int>();
            for (var y = 0; y < yLength; y++)
            {
                var snowSpan = snowImg.GetPixelRowSpan(y);
                var precipSpan = precipitationMap.GetPixelRowSpan(y);
                int tY;
                if (match)
                {
                    tY = y;
                }
                else
                {
                    var lat = options.EqualArea
                        ? SurfaceMap.GetLatitudeOfCylindricalEqualAreaProjection(y, yLength, scale, options)
                        : SurfaceMap.GetLatitudeOfEquirectangularProjection(y, yLength, scale, options);
                    tY = options.EqualArea
                        ? SurfaceMap.GetCylindricalEqualAreaYFromLatWithScale(lat, tempYLength, tScale, options)
                        : SurfaceMap.GetEquirectangularYFromLatWithScale(lat, tempYLength, tScale, options);
                }
                var tSpan = temperatureMap.GetPixelRowSpan(tY);
                for (var x = 0; x < xLength; x++)
                {
                    int tX;
                    if (match)
                    {
                        tX = x;
                    }
                    else if (!xToX.TryGetValue(x, out tX))
                    {
                        var lon = options.EqualArea
                            ? SurfaceMap.GetLongitudeOfCylindricalEqualAreaProjection(x, xLength, scale, options)
                            : SurfaceMap.GetLongitudeOfEquirectangularProjection(x, xLength, scale, options);
                        tX = options.EqualArea
                            ? SurfaceMap.GetCylindricalEqualAreaXFromLonWithScale(lon, tempXLength, tScale, options)
                            : SurfaceMap.GetEquirectangularXFromLonWithScale(lon, tempXLength, tScale, options);
                        xToX.Add(x, tX);
                    }
                    if (tSpan[tX].PackedValue < 17901) // 273.15K / 1000 * ushort.MaxValue
                    {
                        snowSpan[x] = new L16((ushort)Math.Min(ushort.MaxValue, precipSpan[x].PackedValue * Atmosphere.SnowToRainRatio));
                    }
                }
            }
            return snowImg;
        }

        /// <summary>
        /// Gets the temperature at the given position indicated by this map image, in K.
        /// </summary>
        /// <param name="image">A temperature map image.</param>
        /// <param name="latitude">The latitude for which to retrieve a value.</param>
        /// <param name="longitude">The longitude for which to retrieve a value.</param>
        /// <param name="options">The map projection used.</param>
        /// <returns>
        /// The temperature at the given position indicated by this map image, in K.
        /// </returns>
        public static double GetTemperature(this Image<L16> image, double latitude, double longitude, MapProjectionOptions options)
            => image.GetValueFromImage(latitude, longitude, options) * TemperatureScaleFactor;

        /// <summary>
        /// Gets the range of temperatures represented by this map image, in K.
        /// </summary>
        /// <param name="image">A temperature map image.</param>
        /// <returns>
        /// The minimum, maximum, and average temperatures represented by this map image, in K.
        /// </returns>
        public static FloatRange GetTemperatureRange(this Image<L16> image)
        {
            var range = GetRange(image);
            return new FloatRange(
                (float)(range.Min * TemperatureScaleFactor),
                (float)(range.Average * TemperatureScaleFactor),
                (float)(range.Max * TemperatureScaleFactor));
        }

        /// <summary>
        /// Generates a simulated "visual" map, as though viewing the planet from orbit, using the
        /// biome map with additional elevation shading.
        /// </summary>
        /// <param name="maps">The set of <see cref="WeatherMaps"/> from which to generate an image.</param>
        /// <param name="planet">The <see cref="Planetoid"/> for which to generate an image.</param>
        /// <param name="elevationMap">An elevation map.</param>
        /// <param name="temperatureMap">A temperature map.</param>
        /// <param name="mapProjection">
        /// <para>
        /// The map projection used in <paramref name="maps"/>.
        /// </para>
        /// <para>
        /// Only required when non-default.
        /// </para>
        /// </param>
        /// <param name="elevationMapProjection">
        /// <para>
        /// The map projection used in <paramref name="elevationMap"/>.
        /// </para>
        /// <para>
        /// Only required when non-default.
        /// </para>
        /// </param>
        /// <returns>An image.</returns>
        public static Image<Rgba32> GetSatelliteImage(
            this WeatherMaps maps,
            Planetoid planet,
            Image<L16> elevationMap,
            Image<L16> temperatureMap,
            MapProjectionOptions? mapProjection = null,
            MapProjectionOptions? elevationMapProjection = null)
        {
            var elevationRange = planet.GetElevationRange(elevationMap);
            var mp = Substances.All.Water.MeltingPoint ?? 0;
            var projection = mapProjection ?? MapProjectionOptions.Default;
            return maps.BiomeMap.SurfaceMapToImageByLatLon(
                  planet,
                  elevationMap,
                  (v, e, lat, lon) =>
                  {
                      var pixel = v.ToBiomeColor();
                      var elevationFactor = (e * planet.MaxElevation / elevationRange.Max).Clamp(0, 1) * 0.5;
                      var temp = temperatureMap.GetValueFromImage(lat, lon, projection) * TemperatureScaleFactor;
                      return new Rgba32(
                          (byte)Math.Round(NumericExtensions.Lerp(pixel.R, 255, elevationFactor)),
                          (byte)Math.Round(NumericExtensions.Lerp(pixel.G, 255, elevationFactor)),
                          (byte)Math.Round(NumericExtensions.Lerp(pixel.B, 255, elevationFactor)));
                  },
                  (_, e, x, y) =>
                  {
                      var elevationFactor = (-e * planet.MaxElevation / elevationRange.Max).Clamp(0, 1);
                      return new Rgba32(
                          (byte)Math.Round(8.0.Lerp(2, elevationFactor)),
                          (byte)Math.Round(126.0.Lerp(5, elevationFactor)),
                          (byte)Math.Round(199.0.Lerp(20, elevationFactor)));
                  },
                  mapProjection,
                  elevationMapProjection,
                  new HillShadingOptions(true, false));
        }

        /// <summary>
        /// <para>
        /// Applies standard precipitation shading to an precipitation map image.
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
        /// <returns>An image.</returns>
        public static Image<Rgba32> PrecipitationMapToImage(this Image<L16> precipitationMap)
            => SurfaceMapToImage(precipitationMap, v => v.ToPrecipitationColor());

        /// <summary>
        /// <para>
        /// Applies standard precipitation shading to an precipitation map image.
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
        /// <param name="elevationMap">An elevation map.</param>
        /// <param name="mapProjection">
        /// <para>
        /// The map projection used in <paramref name="precipitationMap"/>.
        /// </para>
        /// <para>
        /// Only required when non-default.
        /// </para>
        /// </param>
        /// <param name="elevationMapProjection">
        /// <para>
        /// The map projection used in <paramref name="elevationMap"/>.
        /// </para>
        /// <para>
        /// Only required when non-default.
        /// </para>
        /// </param>
        /// <param name="hillShading">
        /// <para>
        /// Options for a hill shading effect to be applied to the map.
        /// </para>
        /// <para>
        /// If left <see langword="null"/> no hill shading will be applied.
        /// </para>
        /// </param>
        /// <returns>An image.</returns>
        public static Image<Rgba32> PrecipitationMapToImage(
            this Image<L16> precipitationMap,
            Image<L16> elevationMap,
            MapProjectionOptions? mapProjection = null,
            MapProjectionOptions? elevationMapProjection = null,
            HillShadingOptions? hillShading = null)
            => SurfaceMapToImage(
                precipitationMap,
                elevationMap,
                v => v.ToPrecipitationColor(),
                mapProjection,
                elevationMapProjection,
                hillShading);

        /// <summary>
        /// <para>
        /// Applies standard precipitation shading to an precipitation map image.
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
        /// <param name="planet">The planet being mapped.</param>
        /// <param name="elevationMap">An elevation map.</param>
        /// <param name="showOcean">
        /// If <see langword="true"/> precipitation over areas below sea level will be shown;
        /// otherwise elevation coloration will be applied to regions below sea level.
        /// </param>
        /// <param name="mapProjection">
        /// <para>
        /// The map projection used in <paramref name="precipitationMap"/>.
        /// </para>
        /// <para>
        /// Only required when non-default.
        /// </para>
        /// </param>
        /// <param name="elevationMapProjection">
        /// <para>
        /// The map projection used in <paramref name="elevationMap"/>.
        /// </para>
        /// <para>
        /// Only required when non-default.
        /// </para>
        /// </param>
        /// <param name="hillShading">
        /// <para>
        /// Options for a hill shading effect to be applied to the map.
        /// </para>
        /// <para>
        /// If left <see langword="null"/> no hill shading will be applied.
        /// </para>
        /// </param>
        /// <returns>An image.</returns>
        public static Image<Rgba32> PrecipitationMapToImage(
            this Image<L16> precipitationMap,
            Planetoid planet,
            Image<L16> elevationMap,
            bool showOcean = true,
            MapProjectionOptions? mapProjection = null,
            MapProjectionOptions? elevationMapProjection = null,
            HillShadingOptions? hillShading = null)
            => SurfaceMapToImage(
                precipitationMap,
                planet,
                elevationMap,
                (v, _) => v.ToPrecipitationColor(),
                (v, e) => showOcean
                    ? v.ToPrecipitationColor()
                    : e.ToElevationColor(),
                mapProjection,
                elevationMapProjection,
                hillShading);

        /// <summary>
        /// Applies a colorization function to a surface map image.
        /// </summary>
        /// <param name="surfaceMap">The surface map to convert.</param>
        /// <param name="converter">
        /// A function which accepts a <see cref="L16"/> pixel, and returns the <see cref="Rgba32"/>
        /// pixel which should be substituted.
        /// </param>
        /// <returns>An image.</returns>
        public static Image<Rgba32> SurfaceMapToImage(
            this Image<L16> surfaceMap,
            Func<L16, Rgba32> converter)
        {
            var yLength = surfaceMap.Height;
            var xLength = surfaceMap.Width;
            var destination = new Image<Rgba32>(xLength, yLength);

            for (var y = 0; y < yLength; y++)
            {
                var sourceRowSpan = surfaceMap.GetPixelRowSpan(y);
                var destinationRowSpan = destination.GetPixelRowSpan(y);
                for (var x = 0; x < xLength; x++)
                {
                    destinationRowSpan[x] = converter(sourceRowSpan[x]);
                }
            }
            return destination;
        }

        /// <summary>
        /// Applies a colorization function to a surface map image.
        /// </summary>
        /// <param name="surfaceMap">The surface map to convert.</param>
        /// <param name="elevationMap">An elevation map.</param>
        /// <param name="converter">
        /// A function which accepts a <see cref="L16"/> pixel, and returns the <see cref="Rgba32"/>
        /// pixel which should be substituted.
        /// </param>
        /// <param name="mapProjection">
        /// <para>
        /// The map projection used in <paramref name="surfaceMap"/>.
        /// </para>
        /// <para>
        /// Only required when non-default.
        /// </para>
        /// </param>
        /// <param name="elevationMapProjection">
        /// <para>
        /// The map projection used in <paramref name="elevationMap"/>.
        /// </para>
        /// <para>
        /// Only required when non-default.
        /// </para>
        /// </param>
        /// <param name="hillShading">
        /// <para>
        /// Options for a hill shading effect to be applied to the map.
        /// </para>
        /// <para>
        /// If left <see langword="null"/> no hill shading will be applied.
        /// </para>
        /// </param>
        /// <returns>An image.</returns>
        public static Image<Rgba32> SurfaceMapToImage(
            this Image<L16> surfaceMap,
            Image<L16> elevationMap,
            Func<L16, Rgba32> converter,
            MapProjectionOptions? mapProjection = null,
            MapProjectionOptions? elevationMapProjection = null,
            HillShadingOptions? hillShading = null)
        {
            var yLength = surfaceMap.Height;
            var xLength = surfaceMap.Width;
            var elevationYLength = elevationMap.Height;
            var elevationXLength = elevationMap.Width;
            var destination = new Image<Rgba32>(xLength, yLength);
            var matchingSizes = elevationXLength == xLength
                && elevationYLength == yLength;
            var projection = mapProjection ?? MapProjectionOptions.Default;
            var elevationProjection = elevationMapProjection ?? MapProjectionOptions.Default;
            var scale = matchingSizes ? 0 : SurfaceMap.GetScale(yLength, projection.Range, projection.EqualArea);
            var stretch = scale / projection.ScaleFactor;
            var elevationScale = matchingSizes ? 0 : SurfaceMap.GetScale(elevationYLength, elevationProjection.Range, elevationProjection.EqualArea);

            for (var y = 0; y < yLength; y++)
            {
                var sourceRowSpan = surfaceMap.GetPixelRowSpan(y);
                var destinationRowSpan = destination.GetPixelRowSpan(y);
                int elevationY;
                if (matchingSizes || hillShading is null)
                {
                    elevationY = y;
                }
                else if (projection.EqualArea)
                {
                    var latitude = SurfaceMap.GetLatitudeOfCylindricalEqualAreaProjection(y, yLength, scale, projection);
                    elevationY = SurfaceMap.GetCylindricalEqualAreaYFromLatWithScale(latitude, elevationYLength, elevationScale, elevationProjection);
                }
                else
                {
                    var latitude = SurfaceMap.GetLatitudeOfEquirectangularProjection(y, yLength, scale, projection);
                    elevationY = SurfaceMap.GetEquirectangularYFromLatWithScale(latitude, elevationYLength, elevationScale, elevationProjection);
                }
                for (var x = 0; x < xLength; x++)
                {
                    if (hillShading is null)
                    {
                        destinationRowSpan[x] = converter(sourceRowSpan[x]);
                    }
                    else
                    {
                        int elevationX;
                        if (matchingSizes)
                        {
                            elevationX = x;
                        }
                        else if (projection.EqualArea)
                        {
                            var longitude = SurfaceMap.GetLongitudeOfCylindricalEqualAreaProjection(x, xLength, scale, projection);
                            elevationX = SurfaceMap.GetCylindricalEqualAreaXFromLonWithScale(longitude, elevationXLength, elevationScale, elevationProjection);
                        }
                        else
                        {
                            var longitude = SurfaceMap.GetLongitudeOfEquirectangularProjection(x, xLength, stretch, projection);
                            elevationX = SurfaceMap.GetEquirectangularXFromLonWithScale(longitude, elevationXLength, elevationScale, elevationProjection);
                        }

                        destinationRowSpan[x] = converter(sourceRowSpan[x])
                            .ApplyHillShading(elevationMap, elevationX, elevationY, hillShading, true);
                    }
                }
            }
            return destination;
        }

        /// <summary>
        /// Applies a colorization function to a surface map image.
        /// </summary>
        /// <param name="surfaceMap">The surface map to convert.</param>
        /// <param name="planet">The planet being mapped.</param>
        /// <param name="elevationMap">An elevation map.</param>
        /// <param name="landConverter">
        /// <para>
        /// A function which accepts a <see cref="L16"/> pixel, and returns the <see cref="Rgba32"/>
        /// pixel which should be substituted. Used for pixels whose elevation is non-negative.
        /// </para>
        /// <para>
        /// If left <see langword="null"/> pixels with positive elevations will be unaltered.
        /// </para>
        /// </param>
        /// <param name="oceanConverter">
        /// <para>
        /// A function which accepts a <see cref="L16"/> pixel, and returns the <see cref="Rgba32"/>
        /// pixel which should be substituted. Used for pixels whose elevation is negative.
        /// </para>
        /// <para>
        /// If left <see langword="null"/> pixels with negative elevations will be unaltered.
        /// </para>
        /// </param>
        /// <param name="mapProjection">
        /// <para>
        /// The map projection used in <paramref name="surfaceMap"/>.
        /// </para>
        /// <para>
        /// Only required when non-default.
        /// </para>
        /// </param>
        /// <param name="elevationMapProjection">
        /// <para>
        /// The map projection used in <paramref name="elevationMap"/>.
        /// </para>
        /// <para>
        /// Only required when non-default.
        /// </para>
        /// </param>
        /// <param name="hillShading">
        /// <para>
        /// Options for a hill shading effect to be applied to the map.
        /// </para>
        /// <para>
        /// If left <see langword="null"/> no hill shading will be applied.
        /// </para>
        /// </param>
        /// <returns>An image.</returns>
        public static Image<Rgba32> SurfaceMapToImage(
            this Image<L16> surfaceMap,
            Planetoid planet,
            Image<L16> elevationMap,
            Func<L16, Rgba32>? landConverter = null,
            Func<L16, Rgba32>? oceanConverter = null,
            MapProjectionOptions? mapProjection = null,
            MapProjectionOptions? elevationMapProjection = null,
            HillShadingOptions? hillShading = null)
        {
            var yLength = surfaceMap.Height;
            var xLength = surfaceMap.Width;
            var elevationYLength = elevationMap.Height;
            var elevationXLength = elevationMap.Width;
            var destination = new Image<Rgba32>(xLength, yLength);
            var matchingSizes = elevationXLength == xLength
                && elevationYLength == yLength;
            var projection = mapProjection ?? MapProjectionOptions.Default;
            var elevationProjection = elevationMapProjection ?? MapProjectionOptions.Default;
            var scale = matchingSizes ? 0 : SurfaceMap.GetScale(yLength, projection.Range, projection.EqualArea);
            var stretch = scale / projection.ScaleFactor;
            var elevationScale = matchingSizes ? 0 : SurfaceMap.GetScale(elevationYLength, elevationProjection.Range, elevationProjection.EqualArea);

            for (var y = 0; y < yLength; y++)
            {
                var sourceRowSpan = surfaceMap.GetPixelRowSpan(y);
                var destinationRowSpan = destination.GetPixelRowSpan(y);
                int elevationY;
                if (matchingSizes)
                {
                    elevationY = y;
                }
                else if (projection.EqualArea)
                {
                    var latitude = SurfaceMap.GetLatitudeOfCylindricalEqualAreaProjection(y, yLength, scale, projection);
                    elevationY = SurfaceMap.GetCylindricalEqualAreaYFromLatWithScale(latitude, elevationYLength, elevationScale, elevationProjection);
                }
                else
                {
                    var latitude = SurfaceMap.GetLatitudeOfEquirectangularProjection(y, yLength, scale, projection);
                    elevationY = SurfaceMap.GetEquirectangularYFromLatWithScale(latitude, elevationYLength, elevationScale, elevationProjection);
                }
                var elevationSpan = elevationMap.GetPixelRowSpan(elevationY);
                for (var x = 0; x < xLength; x++)
                {
                    int elevationX;
                    if (matchingSizes)
                    {
                        elevationX = x;
                    }
                    else if (projection.EqualArea)
                    {
                        var longitude = SurfaceMap.GetLongitudeOfCylindricalEqualAreaProjection(x, xLength, scale, projection);
                        elevationX = SurfaceMap.GetCylindricalEqualAreaXFromLonWithScale(longitude, elevationXLength, elevationScale, elevationProjection);
                    }
                    else
                    {
                        var longitude = SurfaceMap.GetLongitudeOfEquirectangularProjection(x, xLength, stretch, projection);
                        elevationX = SurfaceMap.GetEquirectangularXFromLonWithScale(longitude, elevationXLength, elevationScale, elevationProjection);
                    }
                    if (landConverter is null && oceanConverter is null)
                    {
                        sourceRowSpan[x].ToRgba32(ref destinationRowSpan[x]);
                        destinationRowSpan[x].ApplyHillShading(elevationMap, x, y, hillShading, true);
                    }

                    var normalElevation = elevationSpan[elevationX].GetValueFromPixel_PosNeg() - planet.NormalizedSeaLevel;
                    if (normalElevation >= 0)
                    {
                        if (landConverter is null)
                        {
                            sourceRowSpan[x].ToRgba32(ref destinationRowSpan[x]);
                            destinationRowSpan[x].ApplyHillShading(elevationMap, elevationX, elevationY, hillShading, true);
                        }
                        else
                        {
                            destinationRowSpan[x] = landConverter(sourceRowSpan[x])
                                .ApplyHillShading(elevationMap, elevationX, elevationY, hillShading, true);
                        }
                    }
                    else
                    {
                        if (oceanConverter is null)
                        {
                            sourceRowSpan[x].ToRgba32(ref destinationRowSpan[x]);
                            destinationRowSpan[x].ApplyHillShading(elevationMap, elevationX, elevationY, hillShading, true);
                        }
                        else
                        {
                            destinationRowSpan[x] = oceanConverter(sourceRowSpan[x])
                                .ApplyHillShading(elevationMap, elevationX, elevationY, hillShading, false);
                        }
                    }
                }
            }
            return destination;
        }

        /// <summary>
        /// Applies a colorization function to a surface map image.
        /// </summary>
        /// <param name="surfaceMap">The surface map to convert.</param>
        /// <param name="planet">The planet being mapped.</param>
        /// <param name="elevationMap">An elevation map.</param>
        /// <param name="landConverter">
        /// <para>
        /// A function which accepts a <see cref="L16"/> pixel and the normalized elevation at that
        /// point, and returns the <see cref="Rgba32"/> pixel which should be substituted. Used for
        /// pixels whose elevation is non-negative.
        /// </para>
        /// <para>
        /// If left <see langword="null"/> pixels with positive elevations will be unaltered.
        /// </para>
        /// </param>
        /// <param name="oceanConverter">
        /// <para>
        /// A function which accepts a <see cref="L16"/> pixel and the normalized elevation at that
        /// point, and returns the <see cref="Rgba32"/> pixel which should be substituted. Used for
        /// pixels whose elevation is negative.
        /// </para>
        /// <para>
        /// If left <see langword="null"/> pixels with negative elevations will be unaltered.
        /// </para>
        /// </param>
        /// <param name="mapProjection">
        /// <para>
        /// The map projection used in <paramref name="surfaceMap"/>.
        /// </para>
        /// <para>
        /// Only required when non-default.
        /// </para>
        /// </param>
        /// <param name="elevationMapProjection">
        /// <para>
        /// The map projection used in <paramref name="elevationMap"/>.
        /// </para>
        /// <para>
        /// Only required when non-default.
        /// </para>
        /// </param>
        /// <param name="hillShading">
        /// <para>
        /// Options for a hill shading effect to be applied to the map.
        /// </para>
        /// <para>
        /// If left <see langword="null"/> no hill shading will be applied.
        /// </para>
        /// </param>
        /// <returns>An image.</returns>
        public static Image<Rgba32> SurfaceMapToImage(
            this Image<L16> surfaceMap,
            Planetoid planet,
            Image<L16> elevationMap,
            Func<L16, double, Rgba32>? landConverter = null,
            Func<L16, double, Rgba32>? oceanConverter = null,
            MapProjectionOptions? mapProjection = null,
            MapProjectionOptions? elevationMapProjection = null,
            HillShadingOptions? hillShading = null)
        {
            var yLength = surfaceMap.Height;
            var xLength = surfaceMap.Width;
            var elevationYLength = elevationMap.Height;
            var elevationXLength = elevationMap.Width;
            var destination = new Image<Rgba32>(xLength, yLength);
            var matchingSizes = elevationXLength == xLength
                && elevationYLength == yLength;
            var projection = mapProjection ?? MapProjectionOptions.Default;
            var elevationProjection = elevationMapProjection ?? MapProjectionOptions.Default;
            var scale = matchingSizes ? 0 : SurfaceMap.GetScale(yLength, projection.Range, projection.EqualArea);
            var stretch = scale / projection.ScaleFactor;
            var elevationScale = matchingSizes ? 0 : SurfaceMap.GetScale(elevationYLength, elevationProjection.Range, elevationProjection.EqualArea);

            for (var y = 0; y < yLength; y++)
            {
                var sourceRowSpan = surfaceMap.GetPixelRowSpan(y);
                var destinationRowSpan = destination.GetPixelRowSpan(y);
                int elevationY;
                if (matchingSizes)
                {
                    elevationY = y;
                }
                else if (projection.EqualArea)
                {
                    var latitude = SurfaceMap.GetLatitudeOfCylindricalEqualAreaProjection(y, yLength, scale, projection);
                    elevationY = SurfaceMap.GetCylindricalEqualAreaYFromLatWithScale(latitude, elevationYLength, elevationScale, elevationProjection);
                }
                else
                {
                    var latitude = SurfaceMap.GetLatitudeOfEquirectangularProjection(y, yLength, scale, projection);
                    elevationY = SurfaceMap.GetEquirectangularYFromLatWithScale(latitude, elevationYLength, elevationScale, elevationProjection);
                }
                var elevationSpan = elevationMap.GetPixelRowSpan(elevationY);
                for (var x = 0; x < xLength; x++)
                {
                    int elevationX;
                    if (matchingSizes)
                    {
                        elevationX = x;
                    }
                    else if (projection.EqualArea)
                    {
                        var longitude = SurfaceMap.GetLongitudeOfCylindricalEqualAreaProjection(x, xLength, scale, projection);
                        elevationX = SurfaceMap.GetCylindricalEqualAreaXFromLonWithScale(longitude, elevationXLength, elevationScale, elevationProjection);
                    }
                    else
                    {
                        var longitude = SurfaceMap.GetLongitudeOfEquirectangularProjection(x, xLength, stretch, projection);
                        elevationX = SurfaceMap.GetEquirectangularXFromLonWithScale(longitude, elevationXLength, elevationScale, elevationProjection);
                    }
                    if (landConverter is null && oceanConverter is null)
                    {
                        sourceRowSpan[x].ToRgba32(ref destinationRowSpan[x]);
                        destinationRowSpan[x].ApplyHillShading(elevationMap, x, y, hillShading, true);
                    }

                    var normalElevation = elevationSpan[elevationX].GetValueFromPixel_PosNeg() - planet.NormalizedSeaLevel;
                    if (normalElevation >= 0)
                    {
                        if (landConverter is null)
                        {
                            sourceRowSpan[x].ToRgba32(ref destinationRowSpan[x]);
                            destinationRowSpan[x].ApplyHillShading(elevationMap, elevationX, elevationY, hillShading, true);
                        }
                        else
                        {
                            destinationRowSpan[x] = landConverter(sourceRowSpan[x], normalElevation)
                                .ApplyHillShading(elevationMap, elevationX, elevationY, hillShading, true);
                        }
                    }
                    else
                    {
                        if (oceanConverter is null)
                        {
                            sourceRowSpan[x].ToRgba32(ref destinationRowSpan[x]);
                            destinationRowSpan[x].ApplyHillShading(elevationMap, elevationX, elevationY, hillShading, true);
                        }
                        else
                        {
                            destinationRowSpan[x] = oceanConverter(sourceRowSpan[x], normalElevation)
                                .ApplyHillShading(elevationMap, elevationX, elevationY, hillShading, false);
                        }
                    }
                }
            }
            return destination;
        }

        /// <summary>
        /// Applies a colorization function to a surface map image.
        /// </summary>
        /// <param name="surfaceMap">The surface map to convert.</param>
        /// <param name="planet">The planet being mapped.</param>
        /// <param name="elevationMap">An elevation map.</param>
        /// <param name="landConverter">
        /// <para>
        /// A function which accepts a <see cref="L16"/> pixel, the normalized elevation at that
        /// point, and the X and Y coordinates of the point, and returns the <see cref="Rgba32"/>
        /// pixel which should be substituted. Used for pixels whose elevation is non-negative.
        /// </para>
        /// <para>
        /// If left <see langword="null"/> pixels with positive elevations will be unaltered.
        /// </para>
        /// </param>
        /// <param name="oceanConverter">
        /// <para>
        /// A function which accepts a <see cref="L16"/> pixel, the normalized elevation at that
        /// point, and the X and Y coordinates of the point, and returns the <see cref="Rgba32"/>
        /// pixel which should be substituted. Used for pixels whose elevation is negative.
        /// </para>
        /// <para>
        /// If left <see langword="null"/> pixels with negative elevations will be unaltered.
        /// </para>
        /// </param>
        /// <param name="mapProjection">
        /// <para>
        /// The map projection used in <paramref name="surfaceMap"/>.
        /// </para>
        /// <para>
        /// Only required when non-default.
        /// </para>
        /// </param>
        /// <param name="elevationMapProjection">
        /// <para>
        /// The map projection used in <paramref name="elevationMap"/>.
        /// </para>
        /// <para>
        /// Only required when non-default.
        /// </para>
        /// </param>
        /// <param name="hillShading">
        /// <para>
        /// Options for a hill shading effect to be applied to the map.
        /// </para>
        /// <para>
        /// If left <see langword="null"/> no hill shading will be applied.
        /// </para>
        /// </param>
        /// <returns>An image.</returns>
        public static Image<Rgba32> SurfaceMapToImageByIndex(
            this Image<L16> surfaceMap,
            Planetoid planet,
            Image<L16> elevationMap,
            Func<L16, double, int, int, Rgba32>? landConverter = null,
            Func<L16, double, int, int, Rgba32>? oceanConverter = null,
            MapProjectionOptions? mapProjection = null,
            MapProjectionOptions? elevationMapProjection = null,
            HillShadingOptions? hillShading = null)
        {
            var yLength = surfaceMap.Height;
            var xLength = surfaceMap.Width;
            var elevationYLength = elevationMap.Height;
            var elevationXLength = elevationMap.Width;
            var destination = new Image<Rgba32>(xLength, yLength);
            var matchingSizes = elevationXLength == xLength
                && elevationYLength == yLength;
            var projection = mapProjection ?? MapProjectionOptions.Default;
            var elevationProjection = elevationMapProjection ?? MapProjectionOptions.Default;
            var scale = matchingSizes ? 0 : SurfaceMap.GetScale(yLength, projection.Range, projection.EqualArea);
            var stretch = scale / projection.ScaleFactor;
            var elevationScale = matchingSizes ? 0 : SurfaceMap.GetScale(elevationYLength, elevationProjection.Range, elevationProjection.EqualArea);

            for (var y = 0; y < yLength; y++)
            {
                var sourceRowSpan = surfaceMap.GetPixelRowSpan(y);
                var destinationRowSpan = destination.GetPixelRowSpan(y);
                int elevationY;
                if (matchingSizes)
                {
                    elevationY = y;
                }
                else if (projection.EqualArea)
                {
                    var latitude = SurfaceMap.GetLatitudeOfCylindricalEqualAreaProjection(y, yLength, scale, projection);
                    elevationY = SurfaceMap.GetCylindricalEqualAreaYFromLatWithScale(latitude, elevationYLength, elevationScale, elevationProjection);
                }
                else
                {
                    var latitude = SurfaceMap.GetLatitudeOfEquirectangularProjection(y, yLength, scale, projection);
                    elevationY = SurfaceMap.GetEquirectangularYFromLatWithScale(latitude, elevationYLength, elevationScale, elevationProjection);
                }
                var elevationSpan = elevationMap.GetPixelRowSpan(elevationY);
                for (var x = 0; x < xLength; x++)
                {
                    int elevationX;
                    if (matchingSizes)
                    {
                        elevationX = x;
                    }
                    else if (projection.EqualArea)
                    {
                        var longitude = SurfaceMap.GetLongitudeOfCylindricalEqualAreaProjection(x, xLength, scale, projection);
                        elevationX = SurfaceMap.GetCylindricalEqualAreaXFromLonWithScale(longitude, elevationXLength, elevationScale, elevationProjection);
                    }
                    else
                    {
                        var longitude = SurfaceMap.GetLongitudeOfEquirectangularProjection(x, xLength, stretch, projection);
                        elevationX = SurfaceMap.GetEquirectangularXFromLonWithScale(longitude, elevationXLength, elevationScale, elevationProjection);
                    }
                    if (landConverter is null && oceanConverter is null)
                    {
                        sourceRowSpan[x].ToRgba32(ref destinationRowSpan[x]);
                        destinationRowSpan[x].ApplyHillShading(elevationMap, x, y, hillShading, true);
                    }

                    var normalElevation = elevationSpan[elevationX].GetValueFromPixel_PosNeg() - planet.NormalizedSeaLevel;
                    if (normalElevation >= 0)
                    {
                        if (landConverter is null)
                        {
                            sourceRowSpan[x].ToRgba32(ref destinationRowSpan[x]);
                            destinationRowSpan[x].ApplyHillShading(elevationMap, elevationX, elevationY, hillShading, true);
                        }
                        else
                        {
                            destinationRowSpan[x] = landConverter(sourceRowSpan[x], normalElevation, x, y)
                                .ApplyHillShading(elevationMap, elevationX, elevationY, hillShading, true);
                        }
                    }
                    else
                    {
                        if (oceanConverter is null)
                        {
                            sourceRowSpan[x].ToRgba32(ref destinationRowSpan[x]);
                            destinationRowSpan[x].ApplyHillShading(elevationMap, elevationX, elevationY, hillShading, true);
                        }
                        else
                        {
                            destinationRowSpan[x] = oceanConverter(sourceRowSpan[x], normalElevation, x, y)
                                .ApplyHillShading(elevationMap, elevationX, elevationY, hillShading, false);
                        }
                    }
                }
            }
            return destination;
        }

        /// <summary>
        /// Applies a colorization function to a surface map image.
        /// </summary>
        /// <param name="surfaceMap">The surface map to convert.</param>
        /// <param name="planet">The planet being mapped.</param>
        /// <param name="elevationMap">An elevation map.</param>
        /// <param name="landConverter">
        /// <para>
        /// A function which accepts a <see cref="L16"/> pixel, the normalized elevation at that
        /// point, and the latitude and longitude of the point, and returns the <see cref="Rgba32"/>
        /// pixel which should be substituted. Used for pixels whose elevation is non-negative.
        /// </para>
        /// <para>
        /// If left <see langword="null"/> pixels with positive elevations will be unaltered.
        /// </para>
        /// </param>
        /// <param name="oceanConverter">
        /// <para>
        /// A function which accepts a <see cref="L16"/> pixel, the normalized elevation at that
        /// point, and the latitude and longitude of the point, and returns the <see cref="Rgba32"/>
        /// pixel which should be substituted. Used for pixels whose elevation is negative.
        /// </para>
        /// <para>
        /// If left <see langword="null"/> pixels with negative elevations will be unaltered.
        /// </para>
        /// </param>
        /// <param name="mapProjection">
        /// <para>
        /// The map projection used in <paramref name="surfaceMap"/>.
        /// </para>
        /// <para>
        /// Only required when non-default.
        /// </para>
        /// </param>
        /// <param name="elevationMapProjection">
        /// <para>
        /// The map projection used in <paramref name="elevationMap"/>.
        /// </para>
        /// <para>
        /// Only required when non-default.
        /// </para>
        /// </param>
        /// <param name="hillShading">
        /// <para>
        /// Options for a hill shading effect to be applied to the map.
        /// </para>
        /// <para>
        /// If left <see langword="null"/> no hill shading will be applied.
        /// </para>
        /// </param>
        /// <returns>An image.</returns>
        public static Image<Rgba32> SurfaceMapToImageByLatLon(
            this Image<L16> surfaceMap,
            Planetoid planet,
            Image<L16> elevationMap,
            Func<L16, double, double, double, Rgba32>? landConverter = null,
            Func<L16, double, double, double, Rgba32>? oceanConverter = null,
            MapProjectionOptions? mapProjection = null,
            MapProjectionOptions? elevationMapProjection = null,
            HillShadingOptions? hillShading = null)
        {
            var yLength = surfaceMap.Height;
            var xLength = surfaceMap.Width;
            var elevationYLength = elevationMap.Height;
            var elevationXLength = elevationMap.Width;
            var destination = new Image<Rgba32>(xLength, yLength);
            var matchingSizes = elevationXLength == xLength
                && elevationYLength == yLength;
            var projection = mapProjection ?? MapProjectionOptions.Default;
            var elevationProjection = elevationMapProjection ?? MapProjectionOptions.Default;
            var scale = matchingSizes ? 0 : SurfaceMap.GetScale(yLength, projection.Range, projection.EqualArea);
            var stretch = scale / projection.ScaleFactor;
            var elevationScale = matchingSizes ? 0 : SurfaceMap.GetScale(elevationYLength, elevationProjection.Range, elevationProjection.EqualArea);

            for (var y = 0; y < yLength; y++)
            {
                var sourceRowSpan = surfaceMap.GetPixelRowSpan(y);
                var destinationRowSpan = destination.GetPixelRowSpan(y);
                int elevationY;
                var latitude = projection.EqualArea
                    ? SurfaceMap.GetLatitudeOfCylindricalEqualAreaProjection(y, yLength, scale, projection)
                    : SurfaceMap.GetLatitudeOfEquirectangularProjection(y, yLength, scale, projection);
                if (matchingSizes)
                {
                    elevationY = y;
                }
                else if (projection.EqualArea)
                {
                    elevationY = SurfaceMap.GetCylindricalEqualAreaYFromLatWithScale(latitude, elevationYLength, elevationScale, elevationProjection);
                }
                else
                {
                    elevationY = SurfaceMap.GetEquirectangularYFromLatWithScale(latitude, elevationYLength, elevationScale, elevationProjection);
                }
                var elevationSpan = elevationMap.GetPixelRowSpan(elevationY);
                for (var x = 0; x < xLength; x++)
                {
                    int elevationX;
                    var longitude = projection.EqualArea
                        ? SurfaceMap.GetLongitudeOfCylindricalEqualAreaProjection(x, xLength, scale, projection)
                        : SurfaceMap.GetLongitudeOfEquirectangularProjection(x, xLength, stretch, projection);
                    if (matchingSizes)
                    {
                        elevationX = x;
                    }
                    else if (projection.EqualArea)
                    {
                        elevationX = SurfaceMap.GetCylindricalEqualAreaXFromLonWithScale(longitude, elevationXLength, elevationScale, elevationProjection);
                    }
                    else
                    {
                        elevationX = SurfaceMap.GetEquirectangularXFromLonWithScale(longitude, elevationXLength, elevationScale, elevationProjection);
                    }

                    if (landConverter is null && oceanConverter is null)
                    {
                        sourceRowSpan[x].ToRgba32(ref destinationRowSpan[x]);
                        destinationRowSpan[x].ApplyHillShading(elevationMap, elevationX, elevationY, hillShading, true);
                    }

                    var normalElevation = elevationSpan[elevationX].GetValueFromPixel_PosNeg() - planet.NormalizedSeaLevel;
                    if (normalElevation >= 0)
                    {
                        if (landConverter is null)
                        {
                            sourceRowSpan[x].ToRgba32(ref destinationRowSpan[x]);
                            destinationRowSpan[x].ApplyHillShading(elevationMap, elevationX, elevationY, hillShading, true);
                        }
                        else
                        {
                            destinationRowSpan[x] = landConverter(sourceRowSpan[x], normalElevation, latitude, longitude)
                                .ApplyHillShading(elevationMap, elevationX, elevationY, hillShading, true);
                        }
                    }
                    else
                    {
                        if (oceanConverter is null)
                        {
                            sourceRowSpan[x].ToRgba32(ref destinationRowSpan[x]);
                            destinationRowSpan[x].ApplyHillShading(elevationMap, elevationX, elevationY, hillShading, true);
                        }
                        else
                        {
                            destinationRowSpan[x] = oceanConverter(sourceRowSpan[x], normalElevation, latitude, longitude)
                                .ApplyHillShading(elevationMap, elevationX, elevationY, hillShading, false);
                        }
                    }
                }
            }
            return destination;
        }

        /// <summary>
        /// Converts a surface map to an image using the average of by each <see
        /// cref="FloatRange"/>.
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
        public static Image<Rgba32> SurfaceMapToImage(this FloatRange[][] surfaceMap, Func<float, Rgba32>? converter = null)
        {
            var xLength = surfaceMap.Length;
            var yLength = xLength == 0 ? 0 : surfaceMap[0].Length;
            var image = new Image<Rgba32>(xLength, yLength);

            for (var y = 0; y < yLength; y++)
            {
                var rowSpan = image.GetPixelRowSpan(y);
                for (var x = 0; x < xLength; x++)
                {
                    if (converter is null)
                    {
                        new L16(DoubleToLuminance(surfaceMap[x][y].Average)).ToRgba32(ref rowSpan[x]);
                    }
                    else
                    {
                        rowSpan[x] = converter(surfaceMap[x][y].Average);
                    }
                }
            }
            return image;
        }

        /// <summary>
        /// Converts a surface map to an image using the average of by each <see
        /// cref="FloatRange"/>.
        /// </summary>
        /// <param name="surfaceMap">The surface map to convert.</param>
        /// <param name="converter">
        /// <para>
        /// A function which accepts a <see cref="double"/> value and its X and Y indexes within the
        /// map and returns the R, G, and B values which should be displayed for that value.
        /// </para>
        /// <para>
        /// If left <see langword="null"/>, a uniform (grayscale) value will be generated with equal
        /// R, G, and B components.
        /// </para>
        /// </param>
        /// <returns>An image.</returns>
        public static Image<Rgba32> SurfaceMapToImageByIndex(this FloatRange[][] surfaceMap, Func<float, int, int, Rgba32> converter)
        {
            var xLength = surfaceMap.Length;
            var yLength = xLength == 0 ? 0 : surfaceMap[0].Length;
            var image = new Image<Rgba32>(xLength, yLength);

            for (var y = 0; y < yLength; y++)
            {
                var rowSpan = image.GetPixelRowSpan(y);
                for (var x = 0; x < xLength; x++)
                {
                    rowSpan[x] = converter(surfaceMap[x][y].Average, x, y);
                }
            }
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
        public static Image<Rgba32> SurfaceMapToImage<T>(this T[][] surfaceMap, Func<T, Rgba32> converter)
        {
            var xLength = surfaceMap.Length;
            var yLength = xLength == 0 ? 0 : surfaceMap[0].Length;
            var image = new Image<Rgba32>(xLength, yLength);

            for (var y = 0; y < yLength; y++)
            {
                var rowSpan = image.GetPixelRowSpan(y);
                for (var x = 0; x < xLength; x++)
                {
                    rowSpan[x] = converter(surfaceMap[x][y]);
                }
            }
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
        public static Image<Rgba32> SurfaceMapToImage<T>(this T[][] surfaceMap, Func<T, int, int, Rgba32> converter)
        {
            var xLength = surfaceMap.Length;
            var yLength = xLength == 0 ? 0 : surfaceMap[0].Length;
            var image = new Image<Rgba32>(xLength, yLength);

            for (var y = 0; y < yLength; y++)
            {
                var rowSpan = image.GetPixelRowSpan(y);
                for (var x = 0; x < xLength; x++)
                {
                    rowSpan[x] = converter(surfaceMap[x][y], x, y);
                }
            }
            return image;
        }

        /// <summary>
        /// Converts a surface map to an image.
        /// </summary>
        /// <param name="surfaceMap">The surface map to convert.</param>
        /// <param name="planet">The planet being mapped.</param>
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
        /// <param name="mapProjection">
        /// <para>
        /// The map projection used in <paramref name="surfaceMap"/>.
        /// </para>
        /// <para>
        /// Only required when non-default.
        /// </para>
        /// </param>
        /// <param name="elevationMapProjection">
        /// <para>
        /// The map projection used in <paramref name="elevationMap"/>.
        /// </para>
        /// <para>
        /// Only required when non-default.
        /// </para>
        /// </param>
        /// <param name="hillShading">
        /// <para>
        /// Options for a hill shading effect to be applied to the map.
        /// </para>
        /// <para>
        /// If left <see langword="null"/> no hill shading will be applied.
        /// </para>
        /// </param>
        /// <returns>An image.</returns>
        public static Image<Rgba32> SurfaceMapToImage<T>(
            this T[][] surfaceMap,
            Planetoid planet,
            Image<L16> elevationMap,
            Func<T, double, Rgba32> landConverter,
            Func<T, double, Rgba32> oceanConverter,
            MapProjectionOptions? mapProjection = null,
            MapProjectionOptions? elevationMapProjection = null,
            HillShadingOptions? hillShading = null)
        {
            var xLength = surfaceMap.Length;
            var yLength = xLength == 0 ? 0 : surfaceMap[0].Length;
            var elevationYLength = elevationMap.Height;
            var elevationXLength = elevationMap.Width;
            var destination = new Image<Rgba32>(xLength, yLength);
            var matchingSizes = elevationXLength == xLength
                && elevationYLength == yLength;
            var projection = mapProjection ?? MapProjectionOptions.Default;
            var elevationProjection = elevationMapProjection ?? MapProjectionOptions.Default;
            var scale = matchingSizes ? 0 : SurfaceMap.GetScale(yLength, projection.Range, projection.EqualArea);
            var stretch = scale / projection.ScaleFactor;
            var elevationScale = matchingSizes ? 0 : SurfaceMap.GetScale(elevationYLength, elevationProjection.Range, elevationProjection.EqualArea);

            for (var y = 0; y < yLength; y++)
            {
                var destinationRowSpan = destination.GetPixelRowSpan(y);
                int elevationY;
                if (matchingSizes)
                {
                    elevationY = y;
                }
                else if (projection.EqualArea)
                {
                    var latitude = SurfaceMap.GetLatitudeOfCylindricalEqualAreaProjection(y, yLength, scale, projection);
                    elevationY = SurfaceMap.GetCylindricalEqualAreaYFromLatWithScale(latitude, elevationYLength, elevationScale, elevationProjection);
                }
                else
                {
                    var latitude = SurfaceMap.GetLatitudeOfEquirectangularProjection(y, yLength, scale, projection);
                    elevationY = SurfaceMap.GetEquirectangularYFromLatWithScale(latitude, elevationYLength, elevationScale, elevationProjection);
                }
                var elevationSpan = elevationMap.GetPixelRowSpan(elevationY);

                for (var x = 0; x < xLength; x++)
                {
                    int elevationX;
                    if (matchingSizes)
                    {
                        elevationX = x;
                    }
                    else if (projection.EqualArea)
                    {
                        var longitude = SurfaceMap.GetLongitudeOfCylindricalEqualAreaProjection(x, xLength, scale, projection);
                        elevationX = SurfaceMap.GetCylindricalEqualAreaXFromLonWithScale(longitude, elevationXLength, elevationScale, elevationProjection);
                    }
                    else
                    {
                        var longitude = SurfaceMap.GetLongitudeOfEquirectangularProjection(x, xLength, stretch, projection);
                        elevationX = SurfaceMap.GetEquirectangularXFromLonWithScale(longitude, elevationXLength, elevationScale, elevationProjection);
                    }

                    var normalElevation = elevationSpan[elevationX].GetValueFromPixel_PosNeg() - planet.NormalizedSeaLevel;
                    if (normalElevation >= 0)
                    {
                        destinationRowSpan[x] = landConverter(surfaceMap[x][y], normalElevation)
                            .ApplyHillShading(elevationMap, elevationX, elevationY, hillShading, true);
                    }
                    else
                    {
                        destinationRowSpan[x] = oceanConverter(surfaceMap[x][y], normalElevation)
                            .ApplyHillShading(elevationMap, elevationX, elevationY, hillShading, false);
                    }
                }
            }
            return destination;
        }

        /// <summary>
        /// Converts a surface map to an image.
        /// </summary>
        /// <param name="surfaceMap">The surface map to convert.</param>
        /// <param name="planet">The planet being mapped.</param>
        /// <param name="elevationMap">An elevation map.</param>
        /// <param name="landConverter">
        /// A function which accepts a value, the normalized elevation at that point, and the X and
        /// Y coordinates of the point, and returns the <see cref="Rgba32"/> pixel which should be
        /// substituted. Used for pixels whose elevation is non-negative.
        /// </param>
        /// <param name="oceanConverter">
        /// A function which accepts a value, the normalized elevation at that point, and the X and
        /// Y coordinates of the point, and returns the <see cref="Rgba32"/> pixel which should be
        /// substituted. Used for pixels whose elevation is negative.
        /// </param>
        /// <param name="mapProjection">
        /// <para>
        /// The map projection used in <paramref name="surfaceMap"/>.
        /// </para>
        /// <para>
        /// Only required when non-default.
        /// </para>
        /// </param>
        /// <param name="elevationMapProjection">
        /// <para>
        /// The map projection used in <paramref name="elevationMap"/>.
        /// </para>
        /// <para>
        /// Only required when non-default.
        /// </para>
        /// </param>
        /// <param name="hillShading">
        /// <para>
        /// Options for a hill shading effect to be applied to the map.
        /// </para>
        /// <para>
        /// If left <see langword="null"/> no hill shading will be applied.
        /// </para>
        /// </param>
        /// <returns>An image.</returns>
        public static Image<Rgba32> SurfaceMapToImageByIndex<T>(
            this T[][] surfaceMap,
            Planetoid planet,
            Image<L16> elevationMap,
            Func<T, double, int, int, Rgba32> landConverter,
            Func<T, double, int, int, Rgba32> oceanConverter,
            MapProjectionOptions? mapProjection = null,
            MapProjectionOptions? elevationMapProjection = null,
            HillShadingOptions? hillShading = null)
        {
            var xLength = surfaceMap.Length;
            var yLength = xLength == 0 ? 0 : surfaceMap[0].Length;
            var elevationYLength = elevationMap.Height;
            var elevationXLength = elevationMap.Width;
            var destination = new Image<Rgba32>(xLength, yLength);
            var matchingSizes = elevationXLength == xLength
                && elevationYLength == yLength;
            var projection = mapProjection ?? MapProjectionOptions.Default;
            var elevationProjection = elevationMapProjection ?? MapProjectionOptions.Default;
            var scale = matchingSizes ? 0 : SurfaceMap.GetScale(yLength, projection.Range, projection.EqualArea);
            var stretch = scale / projection.ScaleFactor;
            var elevationScale = matchingSizes ? 0 : SurfaceMap.GetScale(elevationYLength, elevationProjection.Range, elevationProjection.EqualArea);

            for (var y = 0; y < yLength; y++)
            {
                var destinationRowSpan = destination.GetPixelRowSpan(y);
                int elevationY;
                if (matchingSizes)
                {
                    elevationY = y;
                }
                else if (projection.EqualArea)
                {
                    var latitude = SurfaceMap.GetLatitudeOfCylindricalEqualAreaProjection(y, yLength, scale, projection);
                    elevationY = SurfaceMap.GetCylindricalEqualAreaYFromLatWithScale(latitude, elevationYLength, elevationScale, elevationProjection);
                }
                else
                {
                    var latitude = SurfaceMap.GetLatitudeOfEquirectangularProjection(y, yLength, scale, projection);
                    elevationY = SurfaceMap.GetEquirectangularYFromLatWithScale(latitude, elevationYLength, elevationScale, elevationProjection);
                }
                var elevationSpan = elevationMap.GetPixelRowSpan(elevationY);

                for (var x = 0; x < xLength; x++)
                {
                    int elevationX;
                    if (matchingSizes)
                    {
                        elevationX = x;
                    }
                    else if (projection.EqualArea)
                    {
                        var longitude = SurfaceMap.GetLongitudeOfCylindricalEqualAreaProjection(x, xLength, scale, projection);
                        elevationX = SurfaceMap.GetCylindricalEqualAreaXFromLonWithScale(longitude, elevationXLength, elevationScale, elevationProjection);
                    }
                    else
                    {
                        var longitude = SurfaceMap.GetLongitudeOfEquirectangularProjection(x, xLength, stretch, projection);
                        elevationX = SurfaceMap.GetEquirectangularXFromLonWithScale(longitude, elevationXLength, elevationScale, elevationProjection);
                    }

                    var normalElevation = elevationSpan[elevationX].GetValueFromPixel_PosNeg() - planet.NormalizedSeaLevel;
                    if (normalElevation >= 0)
                    {
                        destinationRowSpan[x] = landConverter(surfaceMap[x][y], normalElevation, x, y)
                            .ApplyHillShading(elevationMap, elevationX, elevationY, hillShading, true);
                    }
                    else
                    {
                        destinationRowSpan[x] = oceanConverter(surfaceMap[x][y], normalElevation, x, y)
                            .ApplyHillShading(elevationMap, elevationX, elevationY, hillShading, false);
                    }
                }
            }
            return destination;
        }

        /// <summary>
        /// Converts a surface map to an image.
        /// </summary>
        /// <param name="surfaceMap">The surface map to convert.</param>
        /// <param name="planet">The planet being mapped.</param>
        /// <param name="elevationMap">An elevation map.</param>
        /// <param name="landConverter">
        /// A function which accepts a value, the normalized elevation at that point, and the
        /// latitude and longitude of the point, and returns the <see cref="Rgba32"/> pixel which
        /// should be substituted. Used for pixels whose elevation is non-negative.
        /// </param>
        /// <param name="oceanConverter">
        /// A function which accepts a value, the normalized elevation at that point, and the
        /// latitude and longitude of the point, and returns the <see cref="Rgba32"/> pixel which
        /// should be substituted. Used for pixels whose elevation is negative.
        /// </param>
        /// <param name="mapProjection">
        /// <para>
        /// The map projection used in <paramref name="surfaceMap"/>.
        /// </para>
        /// <para>
        /// Only required when non-default.
        /// </para>
        /// </param>
        /// <param name="elevationMapProjection">
        /// <para>
        /// The map projection used in <paramref name="elevationMap"/>.
        /// </para>
        /// <para>
        /// Only required when non-default.
        /// </para>
        /// </param>
        /// <param name="hillShading">
        /// <para>
        /// Options for a hill shading effect to be applied to the map.
        /// </para>
        /// <para>
        /// If left <see langword="null"/> no hill shading will be applied.
        /// </para>
        /// </param>
        /// <returns>An image.</returns>
        public static Image<Rgba32> SurfaceMapToImageByLatLon<T>(
            this T[][] surfaceMap,
            Planetoid planet,
            Image<L16> elevationMap,
            Func<T, double, double, double, Rgba32> landConverter,
            Func<T, double, double, double, Rgba32> oceanConverter,
            MapProjectionOptions? mapProjection = null,
            MapProjectionOptions? elevationMapProjection = null,
            HillShadingOptions? hillShading = null)
        {
            var xLength = surfaceMap.Length;
            var yLength = xLength == 0 ? 0 : surfaceMap[0].Length;
            var elevationYLength = elevationMap.Height;
            var elevationXLength = elevationMap.Width;
            var destination = new Image<Rgba32>(xLength, yLength);
            var matchingSizes = elevationXLength == xLength
                && elevationYLength == yLength;
            var projection = mapProjection ?? MapProjectionOptions.Default;
            var elevationProjection = elevationMapProjection ?? MapProjectionOptions.Default;
            var scale = matchingSizes ? 0 : SurfaceMap.GetScale(yLength, projection.Range, projection.EqualArea);
            var stretch = scale / projection.ScaleFactor;
            var elevationScale = matchingSizes ? 0 : SurfaceMap.GetScale(elevationYLength, elevationProjection.Range, elevationProjection.EqualArea);

            for (var y = 0; y < yLength; y++)
            {
                var destinationRowSpan = destination.GetPixelRowSpan(y);
                int elevationY;
                var latitude = projection.EqualArea
                    ? SurfaceMap.GetLatitudeOfCylindricalEqualAreaProjection(y, yLength, scale, projection)
                    : SurfaceMap.GetLatitudeOfEquirectangularProjection(y, yLength, scale, projection);
                if (matchingSizes)
                {
                    elevationY = y;
                }
                else if (projection.EqualArea)
                {
                    elevationY = SurfaceMap.GetCylindricalEqualAreaYFromLatWithScale(latitude, elevationYLength, elevationScale, elevationProjection);
                }
                else
                {
                    elevationY = SurfaceMap.GetEquirectangularYFromLatWithScale(latitude, elevationYLength, elevationScale, elevationProjection);
                }
                var elevationSpan = elevationMap.GetPixelRowSpan(elevationY);

                for (var x = 0; x < xLength; x++)
                {
                    int elevationX;
                    var longitude = projection.EqualArea
                        ? SurfaceMap.GetLongitudeOfCylindricalEqualAreaProjection(x, xLength, scale, projection)
                        : SurfaceMap.GetLongitudeOfEquirectangularProjection(x, xLength, stretch, projection);
                    if (matchingSizes)
                    {
                        elevationX = x;
                    }
                    else if (projection.EqualArea)
                    {
                        elevationX = SurfaceMap.GetCylindricalEqualAreaXFromLonWithScale(longitude, elevationXLength, elevationScale, elevationProjection);
                    }
                    else
                    {
                        elevationX = SurfaceMap.GetEquirectangularXFromLonWithScale(longitude, elevationXLength, elevationScale, elevationProjection);
                    }

                    var normalElevation = elevationSpan[elevationX].GetValueFromPixel_PosNeg() - planet.NormalizedSeaLevel;
                    if (normalElevation >= 0)
                    {
                        destinationRowSpan[x] = landConverter(surfaceMap[x][y], normalElevation, latitude, longitude)
                            .ApplyHillShading(elevationMap, elevationX, elevationY, hillShading, true);
                    }
                    else
                    {
                        destinationRowSpan[x] = oceanConverter(surfaceMap[x][y], normalElevation, latitude, longitude)
                            .ApplyHillShading(elevationMap, elevationX, elevationY, hillShading, false);
                    }
                }
            }
            return destination;
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
        public static Image<Rgba32> TemperatureMapToImage(this Image<L16> temperatureMap) => SurfaceMapToImage(
                temperatureMap,
                v => v.ToTemperatureColor());

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
        /// <param name="elevationMap">An elevation map.</param>
        /// <param name="mapProjection">
        /// <para>
        /// The map projection used in <paramref name="temperatureMap"/>.
        /// </para>
        /// <para>
        /// Only required when non-default.
        /// </para>
        /// </param>
        /// <param name="elevationMapProjection">
        /// <para>
        /// The map projection used in <paramref name="elevationMap"/>.
        /// </para>
        /// <para>
        /// Only required when non-default.
        /// </para>
        /// </param>
        /// <param name="hillShading">
        /// <para>
        /// Options for a hill shading effect to be applied to the map.
        /// </para>
        /// <para>
        /// If left <see langword="null"/> no hill shading will be applied.
        /// </para>
        /// </param>
        /// <returns>An image.</returns>
        public static Image<Rgba32> TemperatureMapToImage(
            this Image<L16> temperatureMap,
            Image<L16> elevationMap,
            MapProjectionOptions? mapProjection = null,
            MapProjectionOptions? elevationMapProjection = null,
            HillShadingOptions? hillShading = null) => SurfaceMapToImage(
                temperatureMap,
                elevationMap,
                v => v.ToTemperatureColor(),
                mapProjection,
                elevationMapProjection,
                hillShading);

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
        /// <param name="planet">The planet being mapped.</param>
        /// <param name="elevationMap">An elevation map.</param>
        /// <param name="showOcean">
        /// If <see langword="true"/> temperature over areas below sea level will be shown;
        /// otherwise the elevation coloration will be applied to regions below sea level.
        /// </param>
        /// <param name="mapProjection">
        /// <para>
        /// The map projection used in <paramref name="temperatureMap"/>.
        /// </para>
        /// <para>
        /// Only required when non-default.
        /// </para>
        /// </param>
        /// <param name="elevationMapProjection">
        /// <para>
        /// The map projection used in <paramref name="elevationMap"/>.
        /// </para>
        /// <para>
        /// Only required when non-default.
        /// </para>
        /// </param>
        /// <param name="hillShading">
        /// <para>
        /// Options for a hill shading effect to be applied to the map.
        /// </para>
        /// <para>
        /// If left <see langword="null"/> no hill shading will be applied.
        /// </para>
        /// </param>
        /// <returns>An image.</returns>
        public static Image<Rgba32> TemperatureMapToImage(
            this Image<L16> temperatureMap,
            Planetoid planet,
            Image<L16> elevationMap,
            bool showOcean = true,
            MapProjectionOptions? mapProjection = null,
            MapProjectionOptions? elevationMapProjection = null,
            HillShadingOptions? hillShading = null) => SurfaceMapToImage(
                temperatureMap,
                planet,
                elevationMap,
                (v, _) => v.ToTemperatureColor(),
                (v, e) => showOcean
                    ? v.ToTemperatureColor()
                    : e.ToElevationColor(),
                mapProjection,
                elevationMapProjection,
                hillShading);

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
        public static Rgba32 ToBiomeColor(this BiomeType value) => value switch
        {
            BiomeType.Polar => new Rgba32(238, 240, 241),
            BiomeType.Tundra => new Rgba32(165, 177, 174),
            BiomeType.Alpine => new Rgba32(218, 222, 223),
            BiomeType.Subalpine => new Rgba32(170, 165, 145),
            BiomeType.LichenWoodland => new Rgba32(29, 47, 14),
            BiomeType.ConiferousForest => new Rgba32(15, 23, 4),
            BiomeType.MixedForest => new Rgba32(34, 45, 15),
            BiomeType.Steppe => new Rgba32(120, 84, 61),
            BiomeType.ColdDesert => new Rgba32(135, 122, 95),
            BiomeType.DeciduousForest => new Rgba32(40, 59, 19),
            BiomeType.Shrubland => new Rgba32(135, 122, 95),
            BiomeType.HotDesert => new Rgba32(203, 162, 108),
            BiomeType.Savanna => new Rgba32(61, 58, 28),
            BiomeType.MonsoonForest => new Rgba32(31, 50, 13),
            BiomeType.RainForest => new Rgba32(25, 48, 9),
            BiomeType.Sea => new Rgba32(2, 5, 20),
            _ => new Rgba32(128, 128, 128),
        };

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
        public static Rgba32 ToElevationColor(this double value) => InterpColorRange(value, _ElevationColorProfile);

        /// <summary>
        /// <para>
        /// Converts an <see cref="L16"/> pixel indicating elevation to an <see cref="Rgba32"/>
        /// pixel.
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
        /// The pixel to convert.
        /// </param>
        /// <param name="planet">The planet being mapped.</param>
        /// <returns>
        /// An <see cref="Rgba32"/> pixel corresponding to the normalized elevation value provided.
        /// </returns>
        public static Rgba32 ToElevationColor(this L16 value, Planetoid planet) => InterpColorRange_PosNeg(
            value,
            v => v - planet.NormalizedSeaLevel,
            _ElevationColorProfile);

        /// <summary>
        /// <para>
        /// Converts an <see cref="L16"/> pixel indicating precipitation to an <see cref="Rgba32"/>
        /// pixel.
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
        /// The pixel to convert.
        /// </param>
        /// <returns>
        /// An <see cref="Rgba32"/> pixel corresponding to the precipitation value provided.
        /// </returns>
        public static Rgba32 ToPrecipitationColor(this L16 value) => InterpColorRange_Pos(value, _PrecipitationColorProfile);

        /// <summary>
        /// <para>
        /// Converts an <see cref="L16"/> pixel indicating temperature to an <see cref="Rgba32"/>
        /// pixel.
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
        /// The pixel to convert.
        /// </param>
        /// <returns>
        /// An <see cref="Rgba32"/> pixel corresponding to the temperature value provided.
        /// </returns>
        public static Rgba32 ToTemperatureColor(this L16 value) => InterpColorRange_Pos(
            value,
            v => (v * TemperatureScaleFactor) - 273.15,
            _TemperatureColorProfile);

        internal static Image<L16> GenerateMapImage(
            Func<double, double, double> func,
            int resolution,
            MapProjectionOptions? options = null,
            bool canBeNegative = false)
        {
            if (resolution > 32767)
            {
                throw new ArgumentOutOfRangeException(nameof(resolution), $"The value of {nameof(resolution)} cannot exceed 32767.");
            }
            var projection = options ?? MapProjectionOptions.Default;
            var xResolution = (int)Math.Floor(resolution * projection.AspectRatio);
            var scale = SurfaceMap.GetScale(resolution, projection.Range, projection.EqualArea);
            var stretch = scale / projection.ScaleFactor;

            var image = new Image<L16>(xResolution, resolution);
            for (var y = 0; y < resolution; y++)
            {
                var latitude = projection.EqualArea
                    ? SurfaceMap.GetLatitudeOfCylindricalEqualAreaProjection(
                        y,
                        resolution,
                        scale,
                        projection)
                    : SurfaceMap.GetLatitudeOfEquirectangularProjection(
                        y,
                        resolution,
                        scale,
                        projection);
                var span = image.GetPixelRowSpan(y);
                for (var x = 0; x < xResolution; x++)
                {
                    var longitude = projection.EqualArea
                        ? SurfaceMap.GetLongitudeOfCylindricalEqualAreaProjection(
                            x,
                            xResolution,
                            scale,
                            projection)
                        : SurfaceMap.GetLongitudeOfEquirectangularProjection(
                            x,
                            xResolution,
                            stretch,
                            projection);
                    span[x] = new L16(DoubleToLuminance(func(latitude, longitude), canBeNegative));
                }
            }
            return image;
        }

        internal static Image<L16> GenerateMapImage(
            Image<L16> otherMap,
            Func<double, double, double, double> func,
            int resolution,
            MapProjectionOptions otherOptions,
            MapProjectionOptions options)
        {
            if (resolution > 32767)
            {
                throw new ArgumentOutOfRangeException(nameof(resolution), $"The value of {nameof(resolution)} cannot exceed 32767.");
            }
            if (!options.Range.HasValue
                || options.Range.Value <= 0
                || options.Range.Value >= Math.PI)
            {
                if (otherOptions.Range > 0
                    && otherOptions.Range.Value < Math.PI)
                {
                    throw new ArgumentException("Target projection specifies latitudes or longitudes not included in original projection");
                }
            }

            var xResolution = (int)Math.Floor(resolution * options.AspectRatio);
            var otherResolution = otherMap.Height;
            var otherXResolution = otherMap.Width;
            var scale = SurfaceMap.GetScale(resolution, options.Range, options.EqualArea);
            var match = otherResolution == resolution
                && otherXResolution == xResolution
                && otherOptions.Range == options.Range;
            var otherScale = match
                ? scale
                : SurfaceMap.GetScale(otherResolution, otherOptions.Range, otherOptions.EqualArea);
            var stretch = scale / options.ScaleFactor;

            var (newNorthLatitude, newWestLongitude, newSouthLatitude, newEastLongitude) = GetBounds(options);
            var (originalXMin, originalYMin) = otherOptions.EqualArea
                ? SurfaceMap.GetCylindricalEqualAreaProjectionFromLatLongWithScale(
                    newNorthLatitude, newWestLongitude,
                    otherXResolution,
                    otherResolution,
                    otherScale,
                    otherOptions)
                : SurfaceMap.GetEquirectangularProjectionFromLatLongWithScale(
                    newNorthLatitude, newWestLongitude,
                    otherXResolution,
                    otherResolution,
                    otherScale,
                    otherOptions);
            var (originalXMax, originalYMax) = otherOptions.EqualArea
                ? SurfaceMap.GetCylindricalEqualAreaProjectionFromLatLongWithScale(
                    newSouthLatitude, newEastLongitude,
                    otherXResolution,
                    otherResolution,
                    otherScale,
                    otherOptions)
                : SurfaceMap.GetEquirectangularProjectionFromLatLongWithScale(
                    newSouthLatitude, newEastLongitude,
                    otherXResolution,
                    otherResolution,
                    otherScale,
                    otherOptions);
            if (originalXMin < 0
                || originalYMin < 0
                || originalXMax > otherXResolution
                || originalYMax > otherResolution)
            {
                throw new ArgumentException("Target projection specifies latitudes or longitudes not included in original projection");
            }

            var image = new Image<L16>(xResolution, resolution);
            var longitudes = new Dictionary<int, double>();
            for (var y = 0; y < resolution; y++)
            {
                var latitude = options.EqualArea
                    ? SurfaceMap.GetLatitudeOfCylindricalEqualAreaProjection(
                        y,
                        resolution,
                        scale,
                        options)
                    : SurfaceMap.GetLatitudeOfEquirectangularProjection(
                        y,
                        resolution,
                        scale,
                        options);
                int otherY;
                if (match)
                {
                    otherY = y;
                }
                else
                {
                    otherY = otherOptions.EqualArea
                        ? SurfaceMap.GetCylindricalEqualAreaYFromLatWithScale(latitude, otherResolution, otherScale, otherOptions)
                        : SurfaceMap.GetEquirectangularYFromLatWithScale(latitude, otherResolution, otherScale, otherOptions);
                }
                var span = image.GetPixelRowSpan(y);
                var otherSpan = otherMap.GetPixelRowSpan(otherY);
                for (var x = 0; x < xResolution; x++)
                {
                    if (!longitudes.TryGetValue(x, out var longitude))
                    {
                        longitude = options.EqualArea
                            ? SurfaceMap.GetLongitudeOfCylindricalEqualAreaProjection(
                                x,
                                xResolution,
                                scale,
                                options)
                            : SurfaceMap.GetLongitudeOfEquirectangularProjection(
                                x,
                                xResolution,
                                stretch,
                                options);
                        longitudes.Add(x, longitude);
                    }
                    int otherX;
                    if (match)
                    {
                        otherX = x;
                    }
                    else
                    {
                        otherX = otherOptions.EqualArea
                            ? SurfaceMap.GetCylindricalEqualAreaXFromLonWithScale(longitude, otherXResolution, otherScale, otherOptions)
                            : SurfaceMap.GetEquirectangularXFromLonWithScale(longitude, otherXResolution, otherScale, otherOptions);
                    }
                    span[x] = new L16(DoubleToLuminance(func(latitude, longitude, otherSpan[otherX].GetValueFromPixel_PosNeg())));
                }
            }
            return image;
        }

        internal static (Image<L16> first, Image<L16> second) GenerateMapImages(
            Image<L16>[] otherMaps,
            Func<double, double, double, (double first, double second)> func,
            int resolution,
            double proportionOfYear,
            MapProjectionOptions otherOptions,
            MapProjectionOptions options)
        {
            if (resolution > 32767)
            {
                throw new ArgumentOutOfRangeException(nameof(resolution), $"The value of {nameof(resolution)} cannot exceed 32767.");
            }
            if (!options.Range.HasValue
                || options.Range.Value <= 0
                || options.Range.Value >= Math.PI)
            {
                if (otherOptions.Range > 0
                    && otherOptions.Range.Value < Math.PI)
                {
                    throw new ArgumentException("Target projection specifies latitudes or longitudes not included in original projection");
                }
            }

            var xResolution = (int)Math.Floor(resolution * options.AspectRatio);
            var noOtherMaps = otherMaps.Length == 0;
            var otherResolution = noOtherMaps ? 0 : otherMaps[0].Height;
            var otherXResolution = noOtherMaps ? 0 : otherMaps[0].Width;
            var scale = SurfaceMap.GetScale(resolution, options.Range);
            var match = otherResolution == resolution
                && otherXResolution == xResolution
                && otherOptions.Range == options.Range;
            var otherScale = match || noOtherMaps
                ? scale
                : SurfaceMap.GetScale(otherResolution, options.Range);
            var stretch = scale / options.ScaleFactor;

            var (newNorthLatitude, newWestLongitude, newSouthLatitude, newEastLongitude) = GetBounds(options);
            var (originalXMin, originalYMin) = otherOptions.EqualArea
                ? SurfaceMap.GetCylindricalEqualAreaProjectionFromLatLongWithScale(
                    newNorthLatitude, newWestLongitude,
                    otherXResolution,
                    otherResolution,
                    otherScale,
                    otherOptions)
                : SurfaceMap.GetEquirectangularProjectionFromLatLongWithScale(
                    newNorthLatitude, newWestLongitude,
                    otherXResolution,
                    otherResolution,
                    otherScale,
                    otherOptions);
            var (originalXMax, originalYMax) = otherOptions.EqualArea
                ? SurfaceMap.GetCylindricalEqualAreaProjectionFromLatLongWithScale(
                    newSouthLatitude, newEastLongitude,
                    otherXResolution,
                    otherResolution,
                    otherScale,
                    otherOptions)
                : SurfaceMap.GetEquirectangularProjectionFromLatLongWithScale(
                    newSouthLatitude, newEastLongitude,
                    otherXResolution,
                    otherResolution,
                    otherScale,
                    otherOptions);
            if (originalXMin < 0
                || originalYMin < 0
                || originalXMax > otherXResolution
                || originalYMax > otherResolution)
            {
                throw new ArgumentException("Target projection specifies latitudes or longitudes not included in original projection");
            }

            var first = new Image<L16>(xResolution, resolution);
            var second = new Image<L16>(xResolution, resolution);
            var longitudes = new Dictionary<int, double>();
            for (var y = 0; y < resolution; y++)
            {
                var latitude = options.EqualArea
                    ? SurfaceMap.GetLatitudeOfCylindricalEqualAreaProjection(
                        y,
                        resolution,
                        scale,
                        options)
                    : SurfaceMap.GetLatitudeOfEquirectangularProjection(
                        y,
                        resolution,
                        scale,
                        options);
                int otherY;
                if (match || noOtherMaps)
                {
                    otherY = y;
                }
                else
                {
                    otherY = otherOptions.EqualArea
                        ? SurfaceMap.GetCylindricalEqualAreaYFromLatWithScale(latitude, otherResolution, otherScale, otherOptions)
                        : SurfaceMap.GetEquirectangularYFromLatWithScale(latitude, otherResolution, otherScale, otherOptions);
                }
                var firstSpan = first.GetPixelRowSpan(y);
                var secondSpan = second.GetPixelRowSpan(y);
                for (var x = 0; x < xResolution; x++)
                {
                    if (!longitudes.TryGetValue(x, out var longitude))
                    {
                        longitude = options.EqualArea
                            ? SurfaceMap.GetLongitudeOfCylindricalEqualAreaProjection(
                                x,
                                xResolution,
                                scale,
                                options)
                            : SurfaceMap.GetLongitudeOfEquirectangularProjection(
                                x,
                                xResolution,
                                stretch,
                                options);
                        longitudes.Add(x, longitude);
                    }
                    int otherX;
                    if (match || noOtherMaps)
                    {
                        otherX = x;
                    }
                    else
                    {
                        otherX = otherOptions.EqualArea
                            ? SurfaceMap.GetCylindricalEqualAreaXFromLonWithScale(longitude, otherXResolution, otherScale, otherOptions)
                            : SurfaceMap.GetEquirectangularXFromLonWithScale(longitude, otherXResolution, otherScale, otherOptions);
                    }
                    var (firstValue, secondValue) = func(
                        latitude, longitude,
                        noOtherMaps
                            ? 0
                            : InterpolateAmongImages(otherMaps, proportionOfYear, otherX, otherY));
                    firstSpan[x] = new L16(DoubleToLuminance(firstValue));
                    secondSpan[x] = new L16(DoubleToLuminance(secondValue));
                }
            }
            return (first, second);
        }

        internal static Image<L16> GenerateZeroMapImage(
            int resolution,
            MapProjectionOptions? options = null,
            bool canBeNegative = false)
        {
            if (resolution > 32767)
            {
                throw new ArgumentOutOfRangeException(nameof(resolution), $"The value of {nameof(resolution)} cannot exceed 32767.");
            }
            var projection = options ?? MapProjectionOptions.Default;
            var xResolution = (int)Math.Floor(resolution * projection.AspectRatio);

            var image = new Image<L16>(xResolution, resolution);
            if (!canBeNegative)
            {
                return image;
            }

            for (var y = 0; y < resolution; y++)
            {
                var span = image.GetPixelRowSpan(y);
                for (var x = 0; x < xResolution; x++)
                {
                    span[x] = new L16(32767);
                }
            }
            return image;
        }

        internal static double GetValueFromImage(this Image<L16> image, double latitude, double longitude, MapProjectionOptions options, bool canBeNegative = false)
        {
            var (x, y) = SurfaceMap.GetProjectionFromLatLong(latitude, longitude, image.Width, image.Height, options);
            return canBeNegative
                ? image[x, y].GetValueFromPixel_PosNeg()
                : image[x, y].GetValueFromPixel_Pos();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        internal static double GetValueFromPixel_Pos(this L16 pixel)
            => (double)pixel.PackedValue / ushort.MaxValue;

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        internal static double GetValueFromPixel_PosNeg(this L16 pixel)
            => (2.0 * pixel.PackedValue / ushort.MaxValue) - 1;

        internal static double InterpolateAmongImages(Image<L16> image1, Image<L16> image2, double weight, int x, int y)
            => image1[x, y].GetValueFromPixel_Pos()
            .Lerp(
                image2[x, y].GetValueFromPixel_Pos(),
                weight);

        internal static double InterpolateAmongImages(Image<L16>[] images, double proportionOfYear, int x, int y)
        {
            if (images.Length == 0)
            {
                return 0;
            }
            if (images.Length == 1)
            {
                return images[0][x, y].GetValueFromPixel_Pos();
            }
            if (images.Length == 2)
            {
                return InterpolateAmongImages(images[0], images[1], proportionOfYear, x, y);
            }

            var proportionPerSeason = 1.0 / images.Length;
            var seasonIndex = (int)Math.Floor(proportionOfYear / proportionPerSeason);
            var nextSeasonIndex = seasonIndex == images.Length - 1 ? 0 : seasonIndex + 1;
            return images[seasonIndex][x, y].GetValueFromPixel_Pos()
                .Lerp(
                    images[nextSeasonIndex][x, y].GetValueFromPixel_Pos(),
                    (proportionOfYear - (seasonIndex * proportionPerSeason)) / proportionPerSeason);
        }

        internal static Image<L16> InterpolateImages(
            Image<L16> image1,
            Image<L16> image2,
            double weight)
        {
            if (image1.Height != image2.Height
                || image1.Width != image2.Width)
            {
                throw new ArgumentException("images must all have the same dimensions");
            }

            var combined = new Image<L16>(image1.Width, image1.Height);
            for (var y = 0; y < image1.Height; y++)
            {
                var img1Span = image1.GetPixelRowSpan(y);
                var img2Span = image2.GetPixelRowSpan(y);
                var combinedSpan = combined.GetPixelRowSpan(y);
                for (var x = 0; x < image1.Width; x++)
                {
                    combinedSpan[x] = img1Span[x].Lerp(img2Span[x], weight);
                }
            }

            return combined;
        }

        private static Rgba32 ApplyHillShading(
            this Rgba32 pixel,
            Image<L16> elevationMap,
            int x, int y,
            HillShadingOptions? options,
            bool land)
        {
            if ((land && options?.ApplyToLand != true)
                || (!land && options?.ApplyToOcean != true)
                || x == 0 || x == elevationMap.Width - 1
                || y == 0 || y == elevationMap.Height - 1)
            {
                return pixel;
            }
            var upLeft = elevationMap[x - 1, y - 1].PackedValue;
            var upRight = elevationMap[x + 1, y - 1].PackedValue;
            var downLeft = elevationMap[x - 1, y + 1].PackedValue;
            var downRight = elevationMap[x + 1, y + 1].PackedValue;
            var dzdx = ((upRight
                + (2 * elevationMap[x + 1, y].PackedValue)
                + downRight
                - upLeft
                - (2 * elevationMap[x - 1, y].PackedValue)
                - downLeft)
                / (4.0 * ushort.MaxValue)).Clamp(-1, 1);
            var dzdy = ((downLeft
                + (2 * elevationMap[x, y + 1].PackedValue)
                + downRight
                - upLeft
                - (2 * elevationMap[x, y - 1].PackedValue)
                - upRight)
                / (4.0 * ushort.MaxValue)).Clamp(0, 1);
            var scale = options!.ScaleIsRelative
                ? options.ScaleFactor * (1 + (options.ScaleFactor * Math.Abs(elevationMap[x, y].GetValueFromPixel_PosNeg())))
                : options.ScaleFactor;
            var slope = Math.Atan(scale * Math.Sqrt((dzdx * dzdx) + (dzdy * dzdy)));
            double aspectTerm;
            if (dzdx.IsNearlyZero())
            {
                aspectTerm = dzdy.IsNearlyZero() || dzdy < 0
                    ? -_SinQuarterPiSquared
                    : _SinQuarterPiSquared;
            }
            else
            {
                var aspect = Math.Atan2(dzdy, -dzdx);
                if (aspect < 0)
                {
                    aspect += MathAndScience.Constants.Doubles.MathConstants.TwoPI;
                }
                aspectTerm = _SinQuarterPi * Math.Cos(MathAndScience.Constants.Doubles.MathConstants.ThreeQuartersPI - aspect);
            }
            var factor = ((_SinQuarterPi * Math.Cos(slope)) + (Math.Sin(slope) * aspectTerm)) * options.ShadeMultiplier;
            return new Rgba32(
                (byte)(pixel.R * factor).Clamp(0, 255),
                (byte)(pixel.G * factor).Clamp(0, 255),
                (byte)(pixel.B * factor).Clamp(0, 255));
        }

        private static L16 Average(this L16 pixel, L16 other) => new((ushort)((pixel.PackedValue + other.PackedValue) / 2));

        private static ushort DoubleToLuminance(double value, bool canBeNegative = false)
        {
            if (canBeNegative)
            {
                value = (value + 1) / 2;
            }
            return (ushort)Math.Round(value * ushort.MaxValue).Clamp(0, ushort.MaxValue);
        }

        private static (
            double northLatitude,
            double westLongitude,
            double southLatitude,
            double eastLongitude) GetBounds(MapProjectionOptions? options = null)
        {
            var projection = options ?? MapProjectionOptions.Default;
            var range = projection.Range ?? 0;
            if (range >= Math.PI || range <= 0)
            {
                return (
                    -MathAndScience.Constants.Doubles.MathConstants.HalfPI,
                    -Math.PI,
                    MathAndScience.Constants.Doubles.MathConstants.HalfPI,
                    Math.PI);
            }
            range /= 2;
            var minLat = LatitudeBounded(projection.CentralParallel - range);
            var maxLat = LatitudeBounded(projection.CentralParallel + range);
            var minLon = LongitudeBounded(projection.CentralMeridian - range);
            var maxLon = LongitudeBounded(projection.CentralMeridian + range);
            if (minLat > maxLat)
            {
                var tmp = minLat;
                minLat = maxLat;
                maxLat = tmp;
            }
            if (minLon > maxLon)
            {
                var tmp = minLon;
                minLon = maxLon;
                maxLon = tmp;
            }
            return (
                minLat,
                minLon,
                maxLat,
                maxLon);
        }

        private static byte InterpColor(double value, double lowValue, double highValue, double lowColor, double highColor)
            => (byte)lowColor.Lerp(highColor, (value - lowValue) / (highValue - lowValue));

        private static Rgba32 InterpColorRange(double value, (double max, (double r, double g, double b) color)[] ranges)
        {
            for (var i = 0; i < ranges.Length; i++)
            {
                if (value <= ranges[i].max)
                {
                    if (i == 0)
                    {
                        return new Rgba32(
                            (byte)ranges[i].color.r.Clamp(0, 255),
                            (byte)ranges[i].color.g.Clamp(0, 255),
                            (byte)ranges[i].color.b.Clamp(0, 255));
                    }
                    return new Rgba32(
                        InterpColor(value, ranges[i - 1].max, ranges[i].max, ranges[i - 1].color.r, ranges[i].color.r),
                        InterpColor(value, ranges[i - 1].max, ranges[i].max, ranges[i - 1].color.g, ranges[i].color.g),
                        InterpColor(value, ranges[i - 1].max, ranges[i].max, ranges[i - 1].color.b, ranges[i].color.b));
                }
            }
            return new Rgba32(
                (byte)ranges[^1].color.r.Clamp(0, 255),
                (byte)ranges[^1].color.g.Clamp(0, 255),
                (byte)ranges[^1].color.b.Clamp(0, 255));
        }

        private static Rgba32 InterpColorRange_Pos(L16 value, (double max, (double r, double g, double b) color)[] ranges)
            => InterpColorRange(value.GetValueFromPixel_Pos(), ranges);

        private static Rgba32 InterpColorRange_Pos(
            L16 value,
            Func<double, double> transform,
            (double max, (double r, double g, double b) color)[] ranges)
            => InterpColorRange(transform(value.GetValueFromPixel_Pos()), ranges);

        private static Rgba32 InterpColorRange_PosNeg(
            L16 value,
            Func<double, double> transform,
            (double max, (double r, double g, double b) color)[] ranges)
            => InterpColorRange(transform(value.GetValueFromPixel_PosNeg()), ranges);

        private static double LatitudeBounded(double value)
        {
            if (value < -MathAndScience.Constants.Doubles.MathConstants.HalfPI)
            {
                value = -Math.PI - value;
            }
            if (value > MathAndScience.Constants.Doubles.MathConstants.HalfPI)
            {
                value = Math.PI - value;
            }
            return value;
        }

        private static L16 Lerp(this L16 pixel, L16 other, double weight)
            => new((ushort)(pixel.PackedValue + (weight * (other.PackedValue - pixel.PackedValue))).Clamp(0, ushort.MaxValue));

        private static double LongitudeBounded(double value)
        {
            if (value < -Math.PI)
            {
                value = -MathAndScience.Constants.Doubles.MathConstants.TwoPI - value;
            }
            if (value > Math.PI)
            {
                value = MathAndScience.Constants.Doubles.MathConstants.TwoPI - value;
            }
            return value;
        }
    }
}
