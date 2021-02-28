using NeverFoundry.MathAndScience;
using NeverFoundry.MathAndScience.Numerics;
using System;

namespace NeverFoundry.WorldFoundry.Maps
{
    /// <summary>
    /// Static methods to assist with producing equirectangular projections that map the surface of
    /// a planet.
    /// </summary>
    public static class SurfaceMap
    {
        /// <summary>
        /// Gets a specific value from a range which varies over the course of a year.
        /// </summary>
        /// <param name="range">The range being interpolated.</param>
        /// <param name="proportionOfYear">
        /// The proportion of the year, starting and ending with midwinter, at which the calculation
        /// is to be performed.
        /// </param>
        /// <returns>The specific value from a range which varies over the course of a
        /// year.</returns>
        public static float GetAnnualRangeValue(FloatRange range, float proportionOfYear)
            => range.Min.Lerp(range.Max, proportionOfYear);

        /// <summary>
        /// Determines whether the given proportion of a year falls within the range indicated.
        /// </summary>
        /// <param name="range">The range being interpolated.</param>
        /// <param name="proportionOfYear">
        /// The proportion of the year, starting and ending with midwinter, at which the calculation
        /// is to be performed.
        /// </param>
        /// <returns><see langword="true"/> if the range indicates a positive result for the given
        /// proportion of a year; otherwise <see langword="false"/>.</returns>
        public static bool GetAnnualRangeIsPositiveAtTime(
            FloatRange range,
            float proportionOfYear) => !range.IsZero && (range.Min > range.Max
            ? proportionOfYear >= range.Min || proportionOfYear <= range.Max
            : proportionOfYear >= range.Min && proportionOfYear <= range.Max);

        /// <summary>
        /// Calculates the approximate area of a point on a map projection with the given
        /// characteristics, by transforming the point and its nearest neighbors to latitude and
        /// longitude, calculating the midpoints between them, and calculating the area of the
        /// region enclosed within those midpoints.
        /// </summary>
        /// <param name="radius">The radius of the planet.</param>
        /// <param name="x">The x coordinate of a point on a map projection, with zero as the
        /// westernmost point.</param>
        /// <param name="y">The y coordinate of a point on a map projection, with zero as the
        /// northernmost point.</param>
        /// <param name="resolution">The vertical resolution of the projection.</param>
        /// <param name="options">
        /// The map projection options used to generate the map used.
        /// </param>
        /// <returns>The area of the given point, in m².</returns>
        public static Number GetAreaOfPoint(
            Number radius,
            int x, int y,
            int resolution,
            MapProjectionOptions options) => GetAreaOfPointFromRadiusSquared(
                radius.Square(),
                x, y,
                (int)Math.Floor(resolution * options.AspectRatio),
                resolution,
                options);

        /// <summary>
        /// Calculates the x and y coordinates on a map projection that correspond to a given
        /// latitude and longitude, where 0,0 is at the top, left and is the northwestern-most point
        /// on the map.
        /// </summary>
        /// <param name="latitude">
        /// The latitude to convert, in radians, where negative values indicate the northern
        /// hemisphere.
        /// </param>
        /// <param name="longitude">
        /// The longitude to convert, in radians, where negative values indicate the western
        /// hemisphere.
        /// </param>
        /// <param name="xResolution">The horizontal resolution of the projection.</param>
        /// <param name="yResolution">The vertical resolution of the projection.</param>
        /// <param name="options">
        /// The map projection options used.
        /// </param>
        /// <returns>
        /// The X and Y coordinates of the given position on the projected map, with (0,0) as the
        /// northwest corner.
        /// </returns>
        public static (int x, int y) GetProjectionFromLatLong(
            double latitude,
            double longitude,
            int xResolution,
            int yResolution,
            MapProjectionOptions? options = null)
            => options?.EqualArea == true
            ? GetCylindricalEqualAreaProjectionFromLatLongWithScale(
                latitude, longitude,
                xResolution,
                yResolution,
                GetScale(yResolution, options?.Range, true),
                options ?? MapProjectionOptions.Default)
            : GetEquirectangularProjectionFromLatLongWithScale(
                latitude, longitude,
                xResolution,
                yResolution,
                GetScale(yResolution, options?.Range),
                options ?? MapProjectionOptions.Default);

        /// <summary>
        /// Calculates the x and y coordinates on a cylindrical equal-area projection that
        /// correspond to a given latitude and longitude, where 0,0 is at the top, left and is the
        /// northwestern-most point on the map.
        /// </summary>
        /// <param name="latitude">The latitude to convert, in radians.</param>
        /// <param name="longitude">The longitude to convert.</param>
        /// <param name="resolution">The vertical resolution of the projection.</param>
        /// <param name="options">
        /// The map projection options used.
        /// </param>
        /// <returns>
        /// The latitude and longitude of the given coordinates, in radians.
        /// </returns>
        public static (int x, int y) GetCylindricalEqualAreaProjectionFromLatLong(
            double latitude,
            double longitude,
            int resolution,
            MapProjectionOptions? options = null)
            => GetCylindricalEqualAreaProjectionFromLatLongWithScale(
                latitude, longitude,
                resolution,
                GetScale(resolution, options?.Range, true),
                options ?? MapProjectionOptions.Default);

        /// <summary>
        /// Calculates the x and y coordinates on an equirectangular projection that correspond to a
        /// given latitude and longitude, where 0,0 is at the top, left and is the northwestern-most
        /// point on the map.
        /// </summary>
        /// <param name="latitude">The latitude to convert, in radians.</param>
        /// <param name="longitude">The longitude to convert.</param>
        /// <param name="resolution">The vertical resolution of the projection.</param>
        /// <param name="options">
        /// The map projection options used.
        /// </param>
        /// <returns>
        /// The latitude and longitude of the given coordinates, in radians.
        /// </returns>
        public static (int x, int y) GetEquirectangularProjectionFromLatLong(
            double latitude,
            double longitude,
            int resolution,
            MapProjectionOptions? options = null)
            => GetEquirectangularProjectionFromLatLongWithScale(
                latitude, longitude,
                resolution,
                GetScale(resolution, options?.Range),
                options ?? MapProjectionOptions.Default);

        /// <summary>
        /// Calculates the latitude and longitude that correspond to a set of coordinates from a
        /// cylindrical equal-area projection.
        /// </summary>
        /// <param name="x">The x coordinate of a point on an cylindrical equal-area projection,
        /// with zero as the westernmost point.</param>
        /// <param name="y">The y coordinate of a point on an cylindrical equal-area projection,
        /// with zero as the northernmost point.</param>
        /// <param name="resolution">The vertical resolution of the projection.</param>
        /// <param name="options">
        /// The map projection options used.
        /// </param>
        /// <returns>
        /// The latitude and longitude of the given coordinates, in radians.
        /// </returns>
        public static (double latitude, double longitude) GetLatLonOfCylindricalEqualAreaProjection(
            int x, int y,
            int resolution,
            MapProjectionOptions? options = null)
        {
            var projection = options ?? new MapProjectionOptions(equalArea: true);
            return GetLatLonOfCylindricalEqualAreaProjectionFromAdjustedCoordinates(
                  x, y,
                  (int)Math.Floor(resolution * projection.AspectRatio),
                  resolution,
                  GetScale(resolution, projection.Range, true),
                  projection);
        }

        /// <summary>
        /// Calculates the latitude and longitude that correspond to a set of coordinates from a map
        /// projection.
        /// </summary>
        /// <param name="x">
        /// The x coordinate of a point on a map projection, with zero as the westernmost point.
        /// </param>
        /// <param name="y">
        /// The y coordinate of a point on a map projection, with zero as the northernmost point.
        /// </param>
        /// <param name="resolution">The vertical resolution of the projection.</param>
        /// <param name="options">
        /// The map projection options used.
        /// </param>
        /// <returns>
        /// The latitude and longitude of the given coordinates, in radians, with negative values
        /// representing the northern and western hemispheres.
        /// </returns>
        public static (double latitude, double longitude) GetLatLonForMapProjection(
            int x, int y,
            int resolution,
            MapProjectionOptions? options = null) => options?.EqualArea == true
            ? GetLatLonOfCylindricalEqualAreaProjectionFromAdjustedCoordinates(
                x, y,
                (int)Math.Floor(resolution * (options?.AspectRatio ?? Math.PI)),
                resolution,
                GetScale(resolution, options?.Range, true),
                options ?? MapProjectionOptions.Default)
            : GetLatLonOfEquirectangularProjectionFromAdjustedCoordinates(
                x, y,
                resolution,
                GetScale(resolution, options?.Range),
                options ?? MapProjectionOptions.Default);

        /// <summary>
        /// Calculates the latitude and longitude that correspond to a set of coordinates from an
        /// equirectangular projection.
        /// </summary>
        /// <param name="x">The x coordinate of a point on an equirectangular projection, with zero
        /// as the westernmost point.</param>
        /// <param name="y">The y coordinate of a point on an equirectangular projection, with zero
        /// as the northernmost point.</param>
        /// <param name="resolution">The vertical resolution of the projection.</param>
        /// <param name="options">
        /// The map projection options used.
        /// </param>
        /// <returns>
        /// The latitude and longitude of the given coordinates, in radians.
        /// </returns>
        public static (double latitude, double longitude) GetLatLonOfEquirectangularProjection(
            int x, int y,
            int resolution,
            MapProjectionOptions? options = null)
            => GetLatLonOfEquirectangularProjectionFromAdjustedCoordinates(
                x, y,
                resolution,
                GetScale(resolution, options?.Range),
                options ?? MapProjectionOptions.Default);

        /// <summary>
        /// Calculates the approximate distance by which the given point is separated from its
        /// neighbors on a map projection with the given characteristics, by transforming the point
        /// and its nearest neighbors to latitude and longitude, and averaging the distances between
        /// them.
        /// </summary>
        /// <param name="radius">The radius of the planet.</param>
        /// <param name="x">The x coordinate of a point on a map projection, with zero as the
        /// westernmost point.</param>
        /// <param name="y">The y coordinate of a point on a map projection, with zero as the
        /// northernmost point.</param>
        /// <param name="resolution">The vertical resolution of the projection.</param>
        /// <param name="options">
        /// The map projection options used.
        /// </param>
        /// <returns>The radius of the given point, in meters.</returns>
        public static Number GetSeparationOfPoint(
            Number radius,
            int x, int y,
            int resolution,
            MapProjectionOptions? options = null)
            => GetSeparationOfPointFromRadiusSquared(
                radius.Square(),
                x, y,
                resolution,
                options ?? MapProjectionOptions.Default);

        internal static Number GetAreaOfPointFromRadiusSquared(
            Number radiusSquared,
            int x, int y,
            int xResolution,
            int yResolution,
            MapProjectionOptions options)
        {
            // Calculations don't work at the top or bottom edge, so adjust by 1 pixel.
            if (y == 0)
            {
                y = 1;
            }
            if (y == yResolution - 1)
            {
                y = yResolution - 2;
            }

            // left: x - 1, y
            var left = x == 0
                ? xResolution - 1
                : x - 1;
            // up: x, y - 1
            var up = y - 1;
            // right: x + 1, y
            var right = x == xResolution - 1
                ? 0
                : x + 1;
            // down: x, y + 1
            var down = y + 1;

            double latCenter, lonCenter, lonLeft, lonRight, latUp, latDown;
            var scale = GetScale(yResolution, options.Range, options.EqualArea);
            if (options.EqualArea)
            {
                (latCenter, lonCenter) = GetLatLonOfCylindricalEqualAreaProjectionFromAdjustedCoordinates(
                    x,
                    y,
                    xResolution,
                    yResolution,
                    scale,
                    options);
                (_, lonLeft) = GetLatLonOfCylindricalEqualAreaProjectionFromAdjustedCoordinates(
                    left,
                    y,
                    xResolution,
                    yResolution,
                    scale,
                    options);
                (_, lonRight) = GetLatLonOfCylindricalEqualAreaProjectionFromAdjustedCoordinates(
                    right,
                    y,
                    xResolution,
                    yResolution,
                    scale,
                    options);
                (latUp, _) = GetLatLonOfCylindricalEqualAreaProjectionFromAdjustedCoordinates(
                    x,
                    up,
                    xResolution,
                    yResolution,
                    scale,
                    options);
                (latDown, _) = GetLatLonOfCylindricalEqualAreaProjectionFromAdjustedCoordinates(
                    x,
                    down,
                    xResolution,
                    yResolution,
                    scale,
                    options);
            }
            else
            {
                (latCenter, lonCenter) = GetLatLonOfEquirectangularProjectionFromAdjustedCoordinates(x, y, yResolution, scale, options);
                (_, lonLeft) = GetLatLonOfEquirectangularProjectionFromAdjustedCoordinates(left, y, yResolution, scale, options);
                (_, lonRight) = GetLatLonOfEquirectangularProjectionFromAdjustedCoordinates(right, y, yResolution, scale, options);
                (latUp, _) = GetLatLonOfEquirectangularProjectionFromAdjustedCoordinates(x, up, yResolution, scale, options);
                (latDown, _) = GetLatLonOfEquirectangularProjectionFromAdjustedCoordinates(x, down, yResolution, scale, options);
            }

            var lonLeftBorder = (lonLeft + lonCenter) / 2;
            var lonRightBorder = (lonRight + lonCenter) / 2;
            var latTopBorder = (latUp + latCenter) / 2;
            var latBottomBorder = (latDown + latCenter) / 2;

            return radiusSquared
                * (Math.Abs(Math.Sin(latBottomBorder) - Math.Sin(latTopBorder))
                * Math.Abs(lonRightBorder - lonLeftBorder));
        }

        internal static (int x, int y) GetCylindricalEqualAreaProjectionFromLatLongWithScale(
            double latitude, double longitude,
            int yResolution,
            double scale,
            MapProjectionOptions options)
            => GetCylindricalEqualAreaProjectionFromLatLongWithScale(
                latitude, longitude,
                (int)Math.Floor(yResolution * options.AspectRatio),
                yResolution,
                scale,
                options);

        internal static (int x, int y) GetCylindricalEqualAreaProjectionFromLatLongWithScale(
            double latitude, double longitude,
            int xResolution,
            int yResolution,
            double scale,
            MapProjectionOptions options)
            => ((int)Math.Round(((longitude - options.CentralMeridian) * options.AspectRatio / scale) + (xResolution / 2))
            .Clamp(0, xResolution - 1),
            (int)Math.Round((yResolution / 2) + ((latitude - options.CentralParallel) * Math.PI / scale))
            .Clamp(0, yResolution - 1));

        internal static int GetCylindricalEqualAreaXFromLonWithScale(
            double longitude,
            int xResolution,
            double scale,
            MapProjectionOptions options)
            => (int)Math.Round(((longitude - options.CentralMeridian) * options.AspectRatio / scale) + (xResolution / 2))
            .Clamp(0, xResolution - 1);

        internal static int GetCylindricalEqualAreaYFromLatWithScale(
            double latitude,
            int yResolution,
            double scale,
            MapProjectionOptions options)
            => (int)Math.Round((yResolution / 2) + ((latitude - options.CentralParallel) * Math.PI / scale))
            .Clamp(0, yResolution - 1);

        internal static (int x, int y) GetEquirectangularProjectionFromLatLongWithScale(
            double latitude, double longitude,
            int resolution,
            double scale,
            MapProjectionOptions options)
            => GetEquirectangularProjectionFromLatLongWithScale(
                latitude, longitude,
                resolution * 2,
                resolution,
                scale,
                options);

        internal static (int x, int y) GetEquirectangularProjectionFromLatLongWithScale(
            double latitude, double longitude,
            int xResolution,
            int yResolution,
            double scale,
            MapProjectionOptions options)
            => ((int)Math.Round(((longitude - options.CentralMeridian) * options.ScaleFactor / scale) + yResolution)
            .Clamp(0, xResolution - 1)
            ,(int)Math.Round(((latitude - options.CentralParallel) / scale) + (yResolution / 2))
            .Clamp(0, yResolution - 1));

        internal static int GetEquirectangularXFromLonWithScale(
            double longitude,
            int xResolution,
            double scale,
            MapProjectionOptions options)
            => (int)Math.Round(((longitude - options.CentralMeridian) * options.ScaleFactor / scale) + (xResolution / 2))
            .Clamp(0, xResolution - 1);

        internal static int GetEquirectangularYFromLatWithScale(
            double latitude,
            int yResolution,
            double scale,
            MapProjectionOptions options)
            => (int)Math.Round(((latitude - options.CentralParallel) / scale) + (yResolution / 2))
            .Clamp(0, yResolution - 1);

        internal static double GetLatitudeOfCylindricalEqualAreaProjection(
            long y,
            int yResolution,
            double scale,
            MapProjectionOptions options)
            => ((y - (yResolution / 2)) * scale / Math.PI) + options.CentralParallel;

        internal static double GetLongitudeOfCylindricalEqualAreaProjection(
            long x,
            int xResolution,
            double scale,
            MapProjectionOptions options)
            => ((x - (xResolution / 2)) * scale / options.AspectRatio) + options.CentralMeridian;

        internal static double GetLatitudeOfEquirectangularProjection(
            int y,
            int yResolution,
            double scale,
            MapProjectionOptions options)
            => ((y - (yResolution / 2)) * scale) + options.CentralParallel;

        internal static double GetLongitudeOfEquirectangularProjection(
            int x,
            int xResolution,
            double stretch,
            MapProjectionOptions options)
            => ((x - (xResolution / 2)) * stretch) + options.CentralMeridian;

        internal static double GetScale(int resolution, double? range = null, bool equalArea = false)
        {
            if (equalArea)
            {
                return range < Math.PI && !range.Value.IsNearlyZero()
                    ? Math.PI * range.Value / resolution
                    : MathAndScience.Constants.Doubles.MathConstants.PISquared / resolution;
            }
            else
            {
                return range < Math.PI && !range.Value.IsNearlyZero()
                    ? range.Value / resolution
                    : Math.PI / resolution;
            }
        }

        internal static Number GetSeparationOfPointFromRadiusSquared(
            Number radiusSquared,
            int x, int y,
            int resolution,
            MapProjectionOptions options)
        {
            var xResolution = (int)Math.Floor(resolution * options.AspectRatio);

            // Calculations are invalid at the top or bottom, so adjust by 1 pixel.
            if (y == 0)
            {
                y = 1;
            }
            if (y == resolution - 1)
            {
                y = resolution - 2;
            }
            // left: x - 1, y
            var left = x == 0
                ? 1
                : x - 1;
            // up: x, y - 1
            var up = y == 0
                ? resolution - 1
                : y - 1;
            // right: x + 1, y
            var right = x == xResolution - 1
                ? 0
                : x + 1;
            // down: x, y + 1
            var down = y == resolution - 1
                ? resolution - 2
                : y + 1;

            double latCenter, lonLeft, lonRight, latUp, latDown;
            var scale = GetScale(resolution, options.Range, options.EqualArea);
            if (options.EqualArea)
            {
                (latCenter, _) = GetLatLonOfCylindricalEqualAreaProjectionFromAdjustedCoordinates(
                    x,
                    y,
                    xResolution,
                    resolution,
                    scale,
                    options);
                (_, lonLeft) = GetLatLonOfCylindricalEqualAreaProjectionFromAdjustedCoordinates(
                    left,
                    y,
                    xResolution,
                    resolution,
                    scale,
                    options);
                (_, lonRight) = GetLatLonOfCylindricalEqualAreaProjectionFromAdjustedCoordinates(
                    right,
                    y,
                    xResolution,
                    resolution,
                    scale,
                    options);
                (latUp, _) = GetLatLonOfCylindricalEqualAreaProjectionFromAdjustedCoordinates(
                    x,
                    up,
                    xResolution,
                    resolution,
                    scale,
                    options);
                (latDown, _) = GetLatLonOfCylindricalEqualAreaProjectionFromAdjustedCoordinates(
                    x,
                    down,
                    xResolution,
                    resolution,
                    scale,
                    options);
            }
            else
            {
                (latCenter, _) = GetLatLonOfEquirectangularProjectionFromAdjustedCoordinates(x, y, resolution, scale, options);
                (_, lonLeft) = GetLatLonOfEquirectangularProjectionFromAdjustedCoordinates(left, y, resolution, scale, options);
                (_, lonRight) = GetLatLonOfEquirectangularProjectionFromAdjustedCoordinates(right, y, resolution, scale, options);
                (latUp, _) = GetLatLonOfEquirectangularProjectionFromAdjustedCoordinates(x, up, resolution, scale, options);
                (latDown, _) = GetLatLonOfEquirectangularProjectionFromAdjustedCoordinates(x, down, resolution, scale, options);
            }

            var latTopBorder = (latUp + latCenter) / 2;
            var latBottomBorder = (latDown + latCenter) / 2;

            return radiusSquared * ((Math.Abs(Math.Sin(latBottomBorder) - Math.Sin(latTopBorder)) + Math.Abs(lonRight - lonLeft)) / 2);
        }

        private static (double latitude, double longitude) GetLatLonOfCylindricalEqualAreaProjectionFromAdjustedCoordinates(
            long x, long y,
            int xResolution,
            int yResolution,
            double scale,
            MapProjectionOptions options)
            => (((y - (yResolution / 2)) * scale / Math.PI) + options.CentralParallel,
            ((x - (xResolution / 2)) * scale / options.AspectRatio) + options.CentralMeridian);

        private static (double latitude, double longitude) GetLatLonOfEquirectangularProjectionFromAdjustedCoordinates(
            long x, long y,
            int yResolution,
            double scale,
            MapProjectionOptions options)
            => (((y - (yResolution / 2)) * scale) + options.CentralParallel,
            ((x - yResolution) * scale / options.ScaleFactor) + options.CentralMeridian);
    }
}
