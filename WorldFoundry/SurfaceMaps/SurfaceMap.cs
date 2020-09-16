using NeverFoundry.MathAndScience;
using NeverFoundry.MathAndScience.Constants.Numbers;
using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.MathAndScience.Time;
using NeverFoundry.WorldFoundry.Place;
using NeverFoundry.WorldFoundry.Space;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NeverFoundry.WorldFoundry.SurfaceMapping
{
    /// <summary>
    /// Static methods to assist with producing equirectangular projections that map the surface of
    /// a planet.
    /// </summary>
    public static class SurfaceMap
    {
        private static double? _SinQuarterPi;
        private static double SinQuarterPi => _SinQuarterPi ??= Math.Sin(MathAndScience.Constants.Doubles.MathConstants.QuarterPI);
        private static double? _SinQuarterPiSquared;
        private static double SinQuarterPiSquared => _SinQuarterPiSquared ??= SinQuarterPi * SinQuarterPi;

        /// <summary>
        /// Gets a specific value from a range which varies over the course of a year.
        /// </summary>
        /// <param name="planet">The mapped planet.</param>
        /// <param name="range">The range being interpolated.</param>
        /// <param name="moment">The time at which the calculation is to be performed.</param>
        /// <returns>The specific value from a range which varies over the course of a
        /// year.</returns>
        public static float GetAnnualRangeValue(
            this Planetoid planet,
            FloatRange range,
            Instant moment)
            => range.Min.Lerp(range.Max, (float)planet.GetProportionOfYearAtTime(moment));

        /// <summary>
        /// Determines whether the given <paramref name="moment"/> falls within the range indicated.
        /// </summary>
        /// <param name="planet">The mapped planet.</param>
        /// <param name="range">The range being interpolated.</param>
        /// <param name="moment">The time at which the determination is to be performed.</param>
        /// <returns><see langword="true"/> if the range indicates a positive result for the given
        /// <paramref name="moment"/>; otherwise <see langword="false"/>.</returns>
        public static bool GetAnnualRangeIsPositiveAtTime(
            this Planetoid planet,
            FloatRange range,
            Instant moment)
        {
            var proportionOfYear = (float)planet.GetProportionOfYearAtTime(moment);
            return !range.IsZero
            && (range.Min > range.Max
                ? proportionOfYear >= range.Min || proportionOfYear <= range.Max
                : proportionOfYear >= range.Min && proportionOfYear <= range.Max);
        }

        /// <summary>
        /// Gets the value for a <paramref name="position"/> in a <paramref name="region"/> at a
        /// given <paramref name="moment"/> from a set of ranges.
        /// </summary>
        /// <param name="planet">The mapped planet.</param>
        /// <param name="region">The mapped region.</param>
        /// <param name="position">A position relative to the center of <paramref
        /// name="region"/>.</param>
        /// <param name="ranges">A set of ranges.</param>
        /// <param name="moment">The time at which the calculation is to be performed.</param>
        /// <param name="equalArea">
        /// If <see langword="true"/> the projection will be a cylindrical equal-area projection.
        /// Otherwise, an equirectangular projection will be used.
        /// </param>
        /// <returns>The value for a <paramref name="position"/> in a <paramref name="region"/> at a
        /// given <paramref name="moment"/> from a set of ranges.</returns>
        public static float GetAnnualValueFromLocalPosition(
            this Planetoid planet,
            SurfaceRegion region,
            Vector3 position,
            FloatRange[,] ranges,
            Instant moment,
            bool equalArea = false)
        {
            int x, y;
            if (equalArea)
            {
                (x, y) = GetCylindricalEqualAreaProjectionFromLocalPosition(
                    planet,
                    region,
                    position,
                    ranges.GetLength(0));
            }
            else
            {
                (x, y) = GetEquirectangularProjectionFromLocalPosition(
                    planet,
                    region,
                    position,
                    ranges.GetLength(0));
            }
            return planet.GetAnnualRangeValue(
                ranges[x, y],
                moment);
        }

        /// <summary>
        /// Determines whether the given <paramref name="moment"/> falls within the range indicated
        /// for a <paramref name="position"/> in a <paramref name="region"/>.
        /// </summary>
        /// <param name="planet">The mapped planet.</param>
        /// <param name="region">The mapped region.</param>
        /// <param name="position">A position relative to the center of <paramref
        /// name="region"/>.</param>
        /// <param name="ranges">A set of ranges.</param>
        /// <param name="moment">The time at which the determination is to be performed.</param>
        /// <param name="equalArea">
        /// If <see langword="true"/> the projection will be a cylindrical equal-area projection.
        /// Otherwise, an equirectangular projection will be used.
        /// </param>
        /// <returns><see langword="true"/> if the given <paramref name="moment"/> falls within the
        /// range indicated for a <paramref name="position"/> in a <paramref name="region"/>;
        /// otherwise <see langword="false"/>.</returns>
        public static bool GetAnnualRangeIsPositiveAtTimeAndLocalPosition(
            this Planetoid planet,
            SurfaceRegion region,
            Vector3 position,
            FloatRange[,] ranges,
            Instant moment,
            bool equalArea = false)
        {
            int x, y;
            if (equalArea)
            {
                (x, y) = GetCylindricalEqualAreaProjectionFromLocalPosition(
                    planet,
                    region,
                    position,
                    ranges.GetLength(0));
            }
            else
            {
                (x, y) = GetEquirectangularProjectionFromLocalPosition(
                    planet,
                    region,
                    position,
                    ranges.GetLength(0));
            }
            return planet.GetAnnualRangeIsPositiveAtTime(ranges[x, y], moment);
        }

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
        /// <param name="centralMeridian">The longitude of the central meridian of the projection,
        /// in radians.</param>
        /// <param name="centralParallel">The latitude of the central parallel of the projection, in
        /// radians.</param>
        /// <param name="standardParallels">The latitude of the standard parallels (north and south
        /// of the equator) where the scale of the projection is 1:1, in radians. Zero indicates the
        /// equator. It does not matter whether the positive or negative latitude is provided, if it
        /// is non-zero. If <see langword="null"/>, the <paramref name="centralParallel"/> will be
        /// used.</param>
        /// <param name="range">If provided, indicates the latitude range (north and south of
        /// <paramref name="centralParallel"/>) shown on the projection, in radians. If not
        /// provided, or if equal to zero or greater than π, indicates that the entire globe is
        /// shown.</param>
        /// <param name="equalArea">
        /// If <see langword="true"/> the projection will be a cylindrical equal-area projection.
        /// Otherwise, an equirectangular projection will be used.
        /// </param>
        /// <returns>The area of the given point, in m².</returns>
        public static Number GetAreaOfPoint(
            Number radius,
            int x, int y,
            int resolution,
            double? centralMeridian = null,
            double? centralParallel = null,
            double? standardParallels = null,
            double? range = null,
            bool equalArea = false)
        {
            int xResolution;
            var mapScaleFactor = 0.0;
            if (equalArea)
            {
                mapScaleFactor = Math.Cos(standardParallels ?? centralParallel ?? 0);
                var aspectRatio = Math.PI * mapScaleFactor * mapScaleFactor;
                xResolution = (int)Math.Round(resolution * aspectRatio);
            }
            else
            {
                xResolution = resolution * 2;
            }
            return GetAreaOfPointFromRadiusSquared(
                  radius.Square(),
                  x, y,
                  xResolution,
                  resolution,
                  mapScaleFactor,
                  centralMeridian,
                  centralParallel,
                  standardParallels,
                  range,
                  equalArea);
        }

        /// <summary>
        /// Calculates the approximate area of a point on a map projection with the given
        /// characteristics, by transforming the point and its nearest neighbors to latitude and
        /// longitude, calculating the midpoints between them, and calculating the area of the
        /// region enclosed within those midpoints.
        /// </summary>
        /// <param name="planet">The mapped planet.</param>
        /// <param name="region">The mapped region.</param>
        /// <param name="x">The x coordinate of a point on a map projection, with zero as the
        /// westernmost point.</param>
        /// <param name="y">The y coordinate of a point on a map projection, with zero as the
        /// northernmost point.</param>
        /// <param name="resolution">The vertical resolution of the projection.</param>
        /// <param name="equalArea">
        /// If <see langword="true"/> the projection will be a cylindrical equal-area projection.
        /// Otherwise, an equirectangular projection will be used.
        /// </param>
        /// <returns>The area of the given point, in m².</returns>
        public static Number GetAreaOfLocalPoint(
            this Planetoid planet,
            SurfaceRegion region,
            int x, int y,
            int resolution,
            bool equalArea = false)
        {
            var centralParallel = planet.VectorToLatitude(region.Position);
            int xResolution;
            var mapScaleFactor = 0.0;
            if (equalArea)
            {
                mapScaleFactor = Math.Cos(centralParallel);
                var aspectRatio = Math.PI * mapScaleFactor * mapScaleFactor;
                xResolution = (int)Math.Round(resolution * aspectRatio);
            }
            else
            {
                xResolution = resolution * 2;
            }
            return GetAreaOfPointFromRadiusSquared(
                  planet.RadiusSquared,
                  x,
                  y,
                  xResolution,
                  resolution,
                  mapScaleFactor,
                  planet.VectorToLongitude(region.Position),
                  centralParallel,
                  range: (double)((Frustum)region.Shape).FieldOfViewAngle,
                  equalArea: equalArea);
        }

        /// <summary>
        /// Produces a map projection of an elevation map of the specified region.
        /// </summary>
        /// <param name="planet">The planet being mapped.</param>
        /// <param name="resolution">The vertical resolution of the projection.</param>
        /// <param name="max">
        /// Then this method returns, will be set to the maximum elevation in the mapped region, in
        /// meters.
        /// </param>
        /// <param name="centralMeridian">The longitude of the central meridian of the projection,
        /// in radians.</param>
        /// <param name="centralParallel">The latitude of the central parallel of the projection, in
        /// radians.</param>
        /// <param name="standardParallels">The latitude of the standard parallels (north and south
        /// of the equator) where the scale of the projection is 1:1, in radians. Zero indicates the
        /// equator. It does not matter whether the positive or negative latitude is provided, if it
        /// is non-zero. If <see langword="null"/>, the <paramref name="centralParallel"/> will be
        /// used.</param>
        /// <param name="range">If provided, indicates the latitude range (north and south of
        /// <paramref name="centralParallel"/>) shown on the projection, in radians. If not
        /// provided, or if equal to zero or greater than π, indicates that the entire globe is
        /// shown.</param>
        /// <param name="equalArea">
        /// If <see langword="true"/> the projection will be a cylindrical equal-area projection.
        /// Otherwise, an equirectangular projection will be used.
        /// </param>
        /// <returns>
        /// A two-dimensional array of <see cref="float"/> values corresponding to points on an
        /// equirectangular projected map of the surface. The first index corresponds to the X
        /// coordinate, and the second index corresponds to the Y coordinate. The values are
        /// normalized elevations from -1 to 1, where negative values are below sea level and
        /// positive values are above sea level, and 1 is equal to the maximum elevation of this
        /// <see cref="Planetoid"/>.
        /// <seealso cref="Planetoid.MaxElevation"/>
        /// </returns>
        public static double[][] GetElevationMap(
            this Planetoid planet,
            int resolution,
            out double max,
            double? centralMeridian = null,
            double? centralParallel = null,
            double? standardParallels = null,
            double? range = null,
            bool equalArea = false)
        {
            if (planet.HasElevationMap)
            {
                var map = planet.GetElevationMap(resolution * 2, resolution, out var maxNormalized);
                max = maxNormalized * planet.MaxElevation;
                return map;
            }
            else
            {
                int xResolution;
                var mapScaleFactor = 0.0;
                if (equalArea)
                {
                    mapScaleFactor = Math.Cos(standardParallels ?? centralParallel ?? 0);
                    var aspectRatio = Math.PI * mapScaleFactor * mapScaleFactor;
                    xResolution = (int)Math.Round(resolution * aspectRatio);
                }
                else
                {
                    xResolution = resolution * 2;
                }
                return GetElevationMap(
                    planet,
                    xResolution,
                    resolution,
                    mapScaleFactor,
                    out max,
                    centralMeridian,
                    centralParallel,
                    standardParallels,
                    range,
                    equalArea);
            }
        }

        /// <summary>
        /// Produces a map projection of an elevation map of the specified <paramref
        /// name="region"/>, taking into account any overlay.
        /// </summary>
        /// <param name="planet">The planet being mapped.</param>
        /// <param name="region">The region being mapped.</param>
        /// <param name="resolution">The vertical resolution of the projection.</param>
        /// <param name="max">
        /// Then this method returns, will be set to the maximum elevation in the mapped region, in
        /// meters.
        /// </param>
        /// <param name="equalArea">
        /// If <see langword="true"/> the projection will be a cylindrical equal-area projection.
        /// Otherwise, an equirectangular projection will be used.
        /// </param>
        /// <returns>
        /// A two-dimensional array of <see cref="float"/> values corresponding to points on an
        /// equirectangular projected map of the surface. The first index corresponds to the X
        /// coordinate, and the second index corresponds to the Y coordinate. The values are
        /// normalized elevations from -1 to 1, where negative values are below sea level and
        /// positive values are above sea level, and 1 is equal to the maximum elevation of the
        /// planet.
        /// <seealso cref="Planetoid.MaxElevation"/>
        /// </returns>
        public static double[][] GetElevationMap(
            this Planetoid planet,
            SurfaceRegion region,
            int resolution,
            out double max,
            bool equalArea = false)
        {
            if (region.HasElevationMap)
            {
                var map = region.GetElevationMap(resolution * 2, resolution, out var maxNormalized);
                max = maxNormalized * planet.MaxElevation;
                return map;
            }
            return planet.GetElevationMap(
                resolution,
                out max,
                planet.VectorToLongitude(region.Position),
                planet.VectorToLatitude(region.Position),
                range: (double)((Frustum)region.Shape).FieldOfViewAngle,
                equalArea: equalArea);
        }

        /// <summary>
        /// Calculates the x and y coordinates on a cylindrical equal-area projection that
        /// correspond to a given latitude and longitude, where 0,0 is at the top, left and is the
        /// northwestern-most point on the map.
        /// </summary>
        /// <param name="latitude">The latitude to convert, in radians.</param>
        /// <param name="longitude">The longitude to convert.</param>
        /// <param name="resolution">The vertical resolution of the projection.</param>
        /// <param name="centralMeridian">The longitude of the central meridian of the projection,
        /// in radians.</param>
        /// <param name="centralParallel">The latitude of the central parallel of the projection, in
        /// radians.</param>
        /// <param name="standardParallels">The latitude of the standard parallels (north and south
        /// of the equator) where the scale of the projection is 1:1, in radians. Zero indicates the
        /// equator (the Lambert projection). It does not matter whether the positive or negative
        /// latitude is provided, if it is non-zero. If <see langword="null"/>, the <paramref
        /// name="centralParallel"/> will be used.</param>
        /// <param name="range">If provided, indicates the latitude range (north and south of
        /// <paramref name="centralParallel"/>) shown on the projection, in radians. If not
        /// provided, or if equal to zero or greater than π, indicates that the entire globe is
        /// shown.</param>
        /// <returns>
        /// The latitude and longitude of the given coordinates, in radians.
        /// </returns>
        public static (int x, int y) GetCylindricalEqualAreaProjectionFromLatLong(
            double latitude,
            double longitude,
            int resolution,
            double? centralMeridian = null,
            double? centralParallel = null,
            double? standardParallels = null,
            double? range = null)
            => GetCylindricalEqualAreaProjectionFromLatLongWithScale(
                latitude, longitude,
                resolution,
                range.HasValue && range.Value < Math.PI && !range.Value.IsNearlyZero()
                    ? 1 / (resolution * range.Value)
                    : 1 / resolution,
                centralMeridian,
                centralParallel,
                standardParallels);

        /// <summary>
        /// Calculates the x and y coordinates on a cylindrical equal-area projection that
        /// correspond to a given <paramref name="position"/> relative to the center of the
        /// specified mapped <paramref name="region"/>, where 0,0 is at the top, left and is the
        /// northwestern-most point on the map.
        /// </summary>
        /// <param name="planet">The planet being mapped.</param>
        /// <param name="region">The region being mapped.</param>
        /// <param name="position">A position relative to the center of <paramref
        /// name="region"/>.</param>
        /// <param name="resolution">The vertical resolution of the projection.</param>
        /// <returns>
        /// The latitude and longitude of the given coordinates, in radians.
        /// </returns>
        public static (int x, int y) GetCylindricalEqualAreaProjectionFromLocalPosition(
            Planetoid planet,
            SurfaceRegion region,
            Vector3 position,
            int resolution)
        {
            var pos = region.Position + position;
            return GetCylindricalEqualAreaProjectionFromLatLong(
                planet.VectorToLatitude(pos),
                planet.VectorToLongitude(pos),
                resolution,
                planet.VectorToLongitude(region.Position),
                planet.VectorToLatitude(region.Position),
                range: (double)((Frustum)region.Shape).FieldOfViewAngle);
        }

        /// <summary>
        /// Calculates the x and y coordinates on a cylindrical equal-area projection that
        /// correspond to a given latitude and longitude, where 0,0 is at the top, left and is the
        /// northwestern-most point on the map.
        /// </summary>
        /// <param name="planet">The planet being mapped.</param>
        /// <param name="region">The region being mapped.</param>
        /// <param name="latitude">The latitude to convert, in radians.</param>
        /// <param name="longitude">The longitude to convert.</param>
        /// <param name="resolution">The vertical resolution of the projection.</param>
        /// <returns>
        /// The latitude and longitude of the given coordinates, in radians.
        /// </returns>
        public static (int x, int y) GetCylindricalEqualAreaProjectionFromLocalPosition(
            Planetoid planet,
            SurfaceRegion region,
            double latitude,
            double longitude,
            int resolution)
            => GetCylindricalEqualAreaProjectionFromLatLong(
                latitude,
                longitude,
                resolution,
                planet.VectorToLongitude(region.Position),
                planet.VectorToLatitude(region.Position),
                range: (double)((Frustum)region.Shape).FieldOfViewAngle);

        /// <summary>
        /// Calculates the x and y coordinates on an equirectangular projection that correspond to a
        /// given latitude and longitude, where 0,0 is at the top, left and is the northwestern-most
        /// point on the map.
        /// </summary>
        /// <param name="latitude">The latitude to convert, in radians.</param>
        /// <param name="longitude">The longitude to convert.</param>
        /// <param name="resolution">The vertical resolution of the projection.</param>
        /// <param name="centralMeridian">The longitude of the central meridian of the projection,
        /// in radians.</param>
        /// <param name="centralParallel">The latitude of the central parallel of the projection, in
        /// radians.</param>
        /// <param name="standardParallels">The latitude of the standard parallels (north and south
        /// of the equator) where the scale of the projection is 1:1, in radians. Zero indicates the
        /// equator (the plate carrée projection). It does not matter whether the positive or
        /// negative latitude is provided, if it is non-zero. If <see langword="null"/>, the
        /// <paramref name="centralParallel"/> will be used.</param>
        /// <param name="range">If provided, indicates the latitude range (north and south of
        /// <paramref name="centralParallel"/>) shown on the projection, in radians. If not
        /// provided, or if equal to zero or greater than π, indicates that the entire globe is
        /// shown.</param>
        /// <returns>
        /// The latitude and longitude of the given coordinates, in radians.
        /// </returns>
        public static (int x, int y) GetEquirectangularProjectionFromLatLong(
            double latitude,
            double longitude,
            int resolution,
            double? centralMeridian = null,
            double? centralParallel = null,
            double? standardParallels = null,
            double? range = null)
            => GetEquirectangularProjectionFromLatLongWithScale(
                latitude, longitude,
                resolution,
                range.HasValue && range.Value < Math.PI && !range.Value.IsNearlyZero()
                    ? MathAndScience.Constants.Doubles.MathConstants.PISquared / (resolution * range.Value)
                    : Math.PI / resolution,
                centralMeridian,
                centralParallel,
                standardParallels);

        /// <summary>
        /// Calculates the x and y coordinates on an equirectangular projection that correspond to a
        /// given <paramref name="position"/> relative to the center of the specified mapped
        /// <paramref name="region"/>, where 0,0 is at the top, left and is the northwestern-most
        /// point on the map.
        /// </summary>
        /// <param name="planet">The planet being mapped.</param>
        /// <param name="region">The region being mapped.</param>
        /// <param name="position">A position relative to the center of <paramref
        /// name="region"/>.</param>
        /// <param name="resolution">The vertical resolution of the projection.</param>
        /// <returns>
        /// The latitude and longitude of the given coordinates, in radians.
        /// </returns>
        public static (int x, int y) GetEquirectangularProjectionFromLocalPosition(
            Planetoid planet,
            SurfaceRegion region,
            Vector3 position,
            int resolution)
        {
            var pos = region.Position + position;
            return GetEquirectangularProjectionFromLatLong(
                planet.VectorToLatitude(pos),
                planet.VectorToLongitude(pos),
                resolution,
                planet.VectorToLongitude(region.Position),
                planet.VectorToLatitude(region.Position),
                range: (double)((Frustum)region.Shape).FieldOfViewAngle);
        }

        /// <summary>
        /// Calculates the x and y coordinates on an equirectangular projection that correspond to a
        /// given latitude and longitude, where 0,0 is at the top, left and is the northwestern-most
        /// point on the map.
        /// </summary>
        /// <param name="planet">The planet being mapped.</param>
        /// <param name="region">The region being mapped.</param>
        /// <param name="latitude">The latitude to convert, in radians.</param>
        /// <param name="longitude">The longitude to convert.</param>
        /// <param name="resolution">The vertical resolution of the projection.</param>
        /// <returns>
        /// The latitude and longitude of the given coordinates, in radians.
        /// </returns>
        public static (int x, int y) GetEquirectangularProjectionFromLocalPosition(
            Planetoid planet,
            SurfaceRegion region,
            double latitude,
            double longitude,
            int resolution)
            => GetEquirectangularProjectionFromLatLong(
                latitude,
                longitude,
                resolution,
                planet.VectorToLongitude(region.Position),
                planet.VectorToLatitude(region.Position),
                range: (double)((Frustum)region.Shape).FieldOfViewAngle);

        /// <summary>
        /// Produces a relief map from the given elevation map.
        /// </summary>
        /// <param name="elevationMap">The elevation map.</param>
        /// <param name="scaleFactor">
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
        /// <param name="scaleIsRelative">
        /// If <see langword="true"/> the <paramref name="scaleFactor"/> will increase with distance
        /// from sea level. Otherwise a uniform value will be used everywhere.
        /// </param>
        /// <returns>
        /// A relief map with a map projection of the specified region indicating the
        /// elevation. Each value in the array indicates the shade of the pixel at that coordinate,
        /// with 0 indicating white, and 1 indicating black.
        /// </returns>
        /// <remarks>
        /// Bear in mind that the calculations required to produce this data are expensive, and the
        /// method may take prohibitively long to complete for large resolutions. Callers should
        /// strongly consider generating low-resolution maps, then using standard enlargement
        /// techniques or tools to expand the results to fit the intended view or texture size.
        /// Unlike photographic images, which can lose clarity with excessive expansion, this type
        /// of elevation data is likely to be nearly as accurate when interpolating between
        /// low-resolution data points as when explicitly calculating values for each
        /// high-resolution point, since this data will nearly always follow relatively smooth local
        /// gradients. A degree of smoothing may even benefit the map, to mitigate aliasing.
        /// </remarks>
        public static float[][] GetHillShadeMap(
            double[][] elevationMap,
            double scaleFactor = 1,
            bool scaleIsRelative = false)
        {
            var xResolution = elevationMap.Length;
            if (xResolution == 0)
            {
                return Array.Empty<float[]>();
            }
            var yResolution = elevationMap[0].Length;

            var hillshadeMap = new float[xResolution][];
            scaleFactor = Math.Max(0, scaleFactor);
            for (var x = 0; x < xResolution; x++)
            {
                hillshadeMap[x] = new float[yResolution];
                if (x == 0 || x == xResolution - 1)
                {
                    continue;
                }
                for (var y = 0; y < yResolution; y++)
                {
                    if (y == 0 || y == yResolution - 1)
                    {
                        continue;
                    }
                    var dzdx = (elevationMap[x + 1][y - 1] + (2 * elevationMap[x + 1][y]) + elevationMap[x + 1][y + 1]
                        - (elevationMap[x - 1][y - 1] + (2 * elevationMap[x - 1][y]) + elevationMap[x - 1][y + 1]))
                        / 8;
                    var dzdy = (elevationMap[x - 1][y + 1] + (2 * elevationMap[x][y + 1]) + elevationMap[x + 1][y + 1]
                        - (elevationMap[x - 1][y - 1] + (2 * elevationMap[x][y - 1]) + elevationMap[x + 1][y - 1]))
                        / 8;
                    var scale = scaleIsRelative ? scaleFactor * (1 + (scaleFactor * Math.Abs(elevationMap[x][y]))) : scaleFactor;
                    var slope = Math.Atan(scale * Math.Sqrt((dzdx * dzdx) + (dzdy * dzdy)));
                    double aspectTerm;
                    if (dzdx.IsNearlyZero())
                    {
                        if (dzdy.IsNearlyZero() || dzdy < 0)
                        {
                            aspectTerm = -SinQuarterPiSquared;
                        }
                        else
                        {
                            aspectTerm = SinQuarterPiSquared;
                        }
                    }
                    else
                    {
                        var aspect = Math.Atan2(dzdy, -dzdx);
                        if (aspect < 0)
                        {
                            aspect += MathAndScience.Constants.Doubles.MathConstants.TwoPI;
                        }
                        aspectTerm = SinQuarterPi * Math.Cos(MathAndScience.Constants.Doubles.MathConstants.ThreeQuartersPI - aspect);
                    }
                    hillshadeMap[x][y] = (float)((SinQuarterPi * Math.Cos(slope)) + (Math.Sin(slope) * aspectTerm));
                }
            }
            return hillshadeMap;
        }

        /// <summary>
        /// Produces a relief map with a map projection of the specified region indicating the
        /// elevation.
        /// </summary>
        /// <param name="planet">The planet being mapped.</param>
        /// <param name="resolution">The vertical resolution of the projection.</param>
        /// <param name="scaleFactor">
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
        /// <param name="centralMeridian">The longitude of the central meridian of the projection,
        /// in radians.</param>
        /// <param name="centralParallel">The latitude of the central parallel of the projection, in
        /// radians.</param>
        /// <param name="standardParallels">The latitude of the standard parallels (north and south
        /// of the equator) where the scale of the projection is 1:1, in radians. Zero indicates the
        /// equator (the plate carrée projection). It does not matter whether the positive or
        /// negative latitude is provided, if it is non-zero. If <see langword="null"/>, the
        /// <paramref name="centralParallel"/> will be used.</param>
        /// <param name="range">If provided, indicates the latitude range (north and south of
        /// <paramref name="centralParallel"/>) shown on the projection, in radians. If not
        /// provided, or if equal to zero or greater than π, indicates that the entire globe is
        /// shown.</param>
        /// <param name="elevationMap">The elevation map for this region. If left <see
        /// langword="null"/> one will be generated. A pre-generated map <i>must</i> share the same
        /// projection parameters (<paramref name="resolution"/>, <paramref
        /// name="centralMeridian"/>, etc.). If it does not, a new one will be generated
        /// anyway.</param>
        /// <param name="equalArea">
        /// If <see langword="true"/> the projection will be a cylindrical equal-area projection.
        /// Otherwise, an equirectangular projection will be used.
        /// </param>
        /// <param name="scaleIsRelative">
        /// If <see langword="true"/> the <paramref name="scaleFactor"/> will increase with distance
        /// from sea level. Otherwise a uniform value will be used everywhere.
        /// </param>
        /// <returns>
        /// A relief map with a map projection of the specified region indicating the
        /// elevation. Each value in the array indicates the shade of the pixel at that coordinate,
        /// with 0 indicating white, and 1 indicating black.
        /// </returns>
        /// <remarks>
        /// Bear in mind that the calculations required to produce this data are expensive, and the
        /// method may take prohibitively long to complete for large resolutions. Callers should
        /// strongly consider generating low-resolution maps, then using standard enlargement
        /// techniques or tools to expand the results to fit the intended view or texture size.
        /// Unlike photographic images, which can lose clarity with excessive expansion, this type
        /// of elevation data is likely to be nearly as accurate when interpolating between
        /// low-resolution data points as when explicitly calculating values for each
        /// high-resolution point, since this data will nearly always follow relatively smooth local
        /// gradients. A degree of smoothing may even benefit the map, to mitigate aliasing.
        /// </remarks>
        public static float[][] GetHillShadeMap(
            this Planetoid planet,
            int resolution,
            double scaleFactor = 1,
            double? centralMeridian = null,
            double? centralParallel = null,
            double? standardParallels = null,
            double? range = null,
            double[][]? elevationMap = null,
            bool equalArea = false,
            bool scaleIsRelative = false)
        {
            if (planet is null)
            {
                throw new ArgumentNullException(nameof(planet));
            }
            if (resolution > int.MaxValue / 2)
            {
                throw new ArgumentOutOfRangeException(nameof(resolution), $"The value of {nameof(resolution)} cannot exceed half of int.MaxValue ({int.MaxValue / 2}).");
            }
            int xResolution;
            var mapScaleFactor = 0.0;
            if (equalArea)
            {
                mapScaleFactor = Math.Cos(standardParallels ?? centralParallel ?? 0);
                var aspectRatio = Math.PI * mapScaleFactor * mapScaleFactor;
                xResolution = (int)Math.Round(resolution * aspectRatio);
            }
            else
            {
                xResolution = resolution * 2;
            }

            return GetHillShadeMap(
                planet,
                xResolution,
                resolution,
                mapScaleFactor,
                scaleFactor,
                centralMeridian,
                centralParallel,
                standardParallels,
                range,
                elevationMap,
                equalArea,
                scaleIsRelative);
        }

        /// <summary>
        /// Produces a relief map with a map projection of the specified <paramref name="region"/>
        /// indicating the elevation.
        /// </summary>
        /// <param name="planet">The planet being mapped.</param>
        /// <param name="region">The region being mapped.</param>
        /// <param name="resolution">The vertical resolution of the projection.</param>
        /// <param name="scaleFactor">
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
        /// <param name="elevationMap">The elevation map for this region. If left <see
        /// langword="null"/> one will be generated. A pre-generated map <i>must</i> share the same
        /// projection parameters (<paramref name="resolution"/>). If it does not, a new one will be
        /// generated anyway.</param>
        /// <param name="equalArea">
        /// If <see langword="true"/> the projection will be a cylindrical equal-area projection.
        /// Otherwise, an equirectangular projection will be used.
        /// </param>
        /// <param name="scaleIsRelative">
        /// If <see langword="true"/> the <paramref name="scaleFactor"/> will increase with distance
        /// from sea level. Otherwise a uniform value will be used everywhere.
        /// </param>
        /// <returns>
        /// A relief map with an equirectangular projection of the specified region indicating the
        /// elevation. Each value in the array indicates the shade of the pixel at that coordinate,
        /// with 0 indicating white, and 1 indicating black.
        /// </returns>
        /// <remarks>
        /// Bear in mind that the calculations required to produce this data are expensive, and the
        /// method may take prohibitively long to complete for large resolutions. Callers should
        /// strongly consider generating low-resolution maps, then using standard enlargement
        /// techniques or tools to expand the results to fit the intended view or texture size.
        /// Unlike photographic images, which can lose clarity with excessive expansion, this type
        /// of elevation data is likely to be nearly as accurate when interpolating between
        /// low-resolution data points as when explicitly calculating values for each
        /// high-resolution point, since this data will nearly always follow relatively smooth local
        /// gradients. A degree of smoothing may even benefit the map, to mitigate aliasing.
        /// </remarks>
        public static float[][] GetHillShadeMap(
            this Planetoid planet,
            SurfaceRegion region,
            int resolution,
            double scaleFactor = 1,
            double[][]? elevationMap = null,
            bool equalArea = false,
            bool scaleIsRelative = false)
        {
            var centralParallel = planet.VectorToLatitude(region.Position);
            int xResolution;
            if (equalArea)
            {
                var mapScaleFactor = Math.Cos(centralParallel);
                var aspectRatio = Math.PI * mapScaleFactor * mapScaleFactor;
                xResolution = (int)Math.Round(resolution * aspectRatio);
            }
            else
            {
                xResolution = resolution * 2;
            }
            if (elevationMap is null || elevationMap.Length != xResolution || (resolution > 0 && elevationMap[0].Length != resolution))
            {
                elevationMap = planet.GetElevationMap(region, resolution, out _, equalArea);
            }
            return GetHillShadeMap(elevationMap, scaleFactor, scaleIsRelative);
        }

        /// <summary>
        /// Produces a set of map projections of the specified region describing the hydrology.
        /// </summary>
        /// <param name="planet">The planet being mapped.</param>
        /// <param name="resolution">The vertical resolution of the projection.</param>
        /// <param name="centralMeridian">The longitude of the central meridian of the projection,
        /// in radians.</param>
        /// <param name="centralParallel">The latitude of the central parallel of the projection, in
        /// radians.</param>
        /// <param name="standardParallels">The latitude of the standard parallels (north and south
        /// of the equator) where the scale of the projection is 1:1, in radians. Zero indicates the
        /// equator. It does not matter whether the positive or negative latitude is provided, if it
        /// is non-zero. If <see langword="null"/>, the <paramref name="centralParallel"/> will be
        /// used.</param>
        /// <param name="range">If provided, indicates the latitude range (north and south of
        /// <paramref name="centralParallel"/>) shown on the projection, in radians. If not
        /// provided, or if equal to zero or greater than π, indicates that the entire globe is
        /// shown.</param>
        /// <param name="elevationMap">The elevation map for this region. If left <see
        /// langword="null"/> one will be generated. A pre-generated map <i>must</i> share the same
        /// projection parameters (<paramref name="resolution"/>, <paramref
        /// name="centralMeridian"/>, etc.). If it does not, a new one will be generated
        /// anyway.</param>
        /// <param name="maxElevation">
        /// The maximum elevation of the mapped region, in meters. If left <see langword="null"/> a
        /// new elevation map will be generated and the value calculated.
        /// </param>
        /// <param name="precipitationMap">An annual precipitation map for this region. If left <see
        /// langword="null"/> one will be generated. Note that pre-generating a precipitation map
        /// will usually be significantly more efficient and accurate than allowing the method to
        /// create one, since the quality of weather maps depends significantly on the number of
        /// steps used to generate it. A pre-generated map <i>must</i> share the same projection
        /// parameters (<paramref name="resolution"/>, <paramref name="centralMeridian"/>, etc.). If
        /// it does not, a new one will be generated anyway.</param>
        /// <param name="averageElevation">The average elevation of the area. If left <see
        /// langword="null"/> it will be calculated.</param>
        /// <param name="equalArea">
        /// If <see langword="true"/> the projection will be a cylindrical equal-area projection.
        /// Otherwise, an equirectangular projection will be used.
        /// </param>
        /// <returns>A <see cref="HydrologyMaps"/> instance with a set of equirectangular
        /// projections of the specified region describing the hydrology.</returns>
        /// <remarks>
        /// Bear in mind that the calculations required to produce this hydrology data are
        /// expensive, and the method may take prohibitively long to complete for large resolutions.
        /// Callers should strongly consider generating low-resolution maps, then using standard
        /// enlargement techniques or tools to expand the results to fit the intended view or
        /// texture size. Unlike photographic images, which can lose clarity with excessive
        /// expansion, this type of hydrology data is likely to be nearly as accurate when
        /// interpolating between low-resolution data points as when explicitly calculating values
        /// for each high-resolution point, since this data will nearly always follow relatively
        /// smooth local gradients. A degree of smoothing may even benefit the flow map when used
        /// for the purpose of displaying rivers, to mitigate aliasing.
        /// </remarks>
        public static HydrologyMaps GetHydrologyMaps(
            this Planetoid planet,
            int resolution,
            double? centralMeridian = null,
            double? centralParallel = null,
            double? standardParallels = null,
            double? range = null,
            double[][]? elevationMap = null,
            double? maxElevation = null,
            float[][]? precipitationMap = null,
            double? averageElevation = null,
            bool equalArea = false)
        {
            if (planet is null)
            {
                throw new ArgumentNullException(nameof(planet));
            }
            if (resolution > int.MaxValue / 2)
            {
                throw new ArgumentOutOfRangeException(nameof(resolution), $"The value of {nameof(resolution)} cannot exceed half of int.MaxValue ({int.MaxValue / 2}).");
            }

            int xResolution;
            var mapScaleFactor = 0.0;
            if (equalArea)
            {
                mapScaleFactor = Math.Cos(standardParallels ?? centralParallel ?? 0);
                var aspectRatio = Math.PI * mapScaleFactor * mapScaleFactor;
                xResolution = (int)Math.Round(resolution * aspectRatio);
            }
            else
            {
                xResolution = resolution * 2;
            }
            return GetHydrologyMaps(
                planet,
                xResolution,
                resolution,
                mapScaleFactor,
                centralMeridian,
                centralParallel,
                standardParallels,
                range,
                elevationMap,
                maxElevation,
                precipitationMap,
                averageElevation,
                equalArea);
        }

        /// <summary>
        /// Produces a set of map projections of the specified <paramref name="region"/>
        /// describing the hydrology, taking into account any overlays.
        /// </summary>
        /// <param name="planet">The planet being mapped.</param>
        /// <param name="region">The region being mapped.</param>
        /// <param name="resolution">The vertical resolution of the projection.</param>
        /// <param name="elevationMap">The elevation map for this region. If left <see
        /// langword="null"/> one will be generated. A pre-generated map <i>must</i> share the same
        /// projection parameters (<paramref name="resolution"/>). If it does not, a new one will be
        /// generated anyway.</param>
        /// <param name="maxElevation">
        /// The maximum elevation of the mapped region, in meters. If left <see langword="null"/> a
        /// new elevation map will be generated and the value calculated.
        /// </param>
        /// <param name="precipitationMap">An annual precipitation map for this region. If left <see
        /// langword="null"/> one will be generated. Note that pre-generating a precipitation map
        /// will usually be significantly more efficient and accurate than allowing the method to
        /// create one, since the quality of weather maps depends significantly on the number of
        /// steps used to generate it. A pre-generated map <i>must</i> share the same projection
        /// parameters (<paramref name="resolution"/>). If it does not, a new one will be generated
        /// anyway.</param>
        /// <param name="averageElevation">The average elevation of the area. If left <see
        /// langword="null"/> it will be calculated.</param>
        /// <param name="equalArea">
        /// If <see langword="true"/> the projection will be a cylindrical equal-area projection.
        /// Otherwise, an equirectangular projection will be used.
        /// </param>
        /// <returns>A <see cref="HydrologyMaps"/> instance with a set of equirectangular
        /// projections of the region describing the hydrology, taking into account any
        /// overlay.</returns>
        /// <remarks>
        /// Bear in mind that the calculations required to produce this hydrology data are
        /// expensive, and the method may take prohibitively long to complete for large resolutions.
        /// Callers should strongly consider generating low-resolution maps, then using standard
        /// enlargement techniques or tools to expand the results to fit the intended view or
        /// texture size. Unlike photographic images, which can lose clarity with excessive
        /// expansion, this type of hydrology data is likely to be nearly as accurate when
        /// interpolating between low-resolution data points as when explicitly calculating values
        /// for each high-resolution point, since this data will nearly always follow relatively
        /// smooth local gradients. A degree of smoothing may even benefit the flow map when used
        /// for the purpose of displaying rivers, to mitigate aliasing.
        /// </remarks>
        public static HydrologyMaps GetHydrologyMaps(
            this Planetoid planet,
            SurfaceRegion region,
            int resolution,
            double[][]? elevationMap = null,
            double? maxElevation = null,
            float[][]? precipitationMap = null,
            double? averageElevation = null,
            bool equalArea = false)
        {
            var centralParallel = planet.VectorToLatitude(region.Position);
            int xResolution;
            var mapScaleFactor = 0.0;
            if (equalArea)
            {
                mapScaleFactor = Math.Cos(centralParallel);
                var aspectRatio = Math.PI * mapScaleFactor * mapScaleFactor;
                xResolution = (int)Math.Round(resolution * aspectRatio);
            }
            else
            {
                xResolution = resolution * 2;
            }
            return GetHydrologyMaps(
                planet,
                region,
                xResolution,
                resolution,
                mapScaleFactor,
                centralParallel,
                elevationMap,
                maxElevation,
                precipitationMap,
                averageElevation,
                equalArea);
        }

        /// <summary>
        /// Calculates the latitude and longitude that correspond to a set of coordinates from a
        /// cylindrical equal-area projection.
        /// </summary>
        /// <param name="x">The x coordinate of a point on an cylindrical equal-area projection,
        /// with zero as the westernmost point.</param>
        /// <param name="y">The y coordinate of a point on an cylindrical equal-area projection,
        /// with zero as the northernmost point.</param>
        /// <param name="resolution">The vertical resolution of the projection.</param>
        /// <param name="centralMeridian">The longitude of the central meridian of the projection,
        /// in radians.</param>
        /// <param name="centralParallel">The latitude of the central parallel of the projection, in
        /// radians.</param>
        /// <param name="standardParallels">The latitude of the standard parallels (north and south
        /// of the equator) where the scale of the projection is 1:1, in radians. Zero indicates the
        /// equator (the Lambert projection). It does not matter whether the positive or negative
        /// latitude is provided, if it is non-zero. If <see langword="null"/>, the <paramref
        /// name="centralParallel"/> will be used.</param>
        /// <param name="range">If provided, indicates the latitude range (north and south of
        /// <paramref name="centralParallel"/>) shown on the projection, in radians. If not
        /// provided, or if equal to zero or greater than π, indicates that the entire globe is
        /// shown.</param>
        /// <returns>
        /// The latitude and longitude of the given coordinates, in radians.
        /// </returns>
        public static (double latitude, double longitude) GetLatLonOfCylindricalEqualAreaProjection(
            int x, int y,
            int resolution,
            double? centralMeridian = null,
            double? centralParallel = null,
            double? standardParallels = null,
            double? range = null)
        {
            var scaleFactor = Math.Cos(standardParallels ?? centralParallel ?? 0);
            var aspectRatio = Math.PI * scaleFactor * scaleFactor;
            return GetLatLonOfCylindricalEqualAreaProjectionFromAdjustedCoordinates(
                  x, y,
                  (int)Math.Round(resolution * aspectRatio),
                  resolution,
                  range.HasValue && range.Value < Math.PI && !range.Value.IsNearlyZero()
                      ? 2.0 / (resolution * range.Value)
                      : 2.0 / resolution,
                  scaleFactor,
                  centralMeridian);
        }

        /// <summary>
        /// Calculates the latitude and longitude that correspond to a set of coordinates from an
        /// equirectangular projection.
        /// </summary>
        /// <param name="x">The x coordinate of a point on an equirectangular projection, with zero
        /// as the westernmost point.</param>
        /// <param name="y">The y coordinate of a point on an equirectangular projection, with zero
        /// as the northernmost point.</param>
        /// <param name="resolution">The vertical resolution of the projection.</param>
        /// <param name="centralMeridian">The longitude of the central meridian of the projection,
        /// in radians.</param>
        /// <param name="centralParallel">The latitude of the central parallel of the projection, in
        /// radians.</param>
        /// <param name="standardParallels">The latitude of the standard parallels (north and south
        /// of the equator) where the scale of the projection is 1:1, in radians. Zero indicates the
        /// equator (the plate carrée projection). It does not matter whether the positive or
        /// negative latitude is provided, if it is non-zero. If <see langword="null"/>, the
        /// <paramref name="centralParallel"/> will be used.</param>
        /// <param name="range">If provided, indicates the latitude range (north and south of
        /// <paramref name="centralParallel"/>) shown on the projection, in radians. If not
        /// provided, or if equal to zero or greater than π, indicates that the entire globe is
        /// shown.</param>
        /// <returns>
        /// The latitude and longitude of the given coordinates, in radians.
        /// </returns>
        public static (double latitude, double longitude) GetLatLonOfEquirectangularProjection(
            int x, int y,
            int resolution,
            double? centralMeridian = null,
            double? centralParallel = null,
            double? standardParallels = null,
            double? range = null)
            => GetLatLonOfEquirectangularProjectionFromAdjustedCoordinates(
                x, y,
                resolution,
                range.HasValue && range.Value < Math.PI && !range.Value.IsNearlyZero()
                    ? MathAndScience.Constants.Doubles.MathConstants.PISquared / (resolution * range.Value)
                    : Math.PI / resolution,
                centralMeridian,
                centralParallel,
                standardParallels);

        /// <summary>
        /// Calculates the latitude and longitude that correspond to a set of coordinates from a
        /// cylindrical equal-area projection.
        /// </summary>
        /// <param name="planet">The planet being mapped.</param>
        /// <param name="region">The region being mapped.</param>
        /// <param name="x">The x coordinate of a point on a cylindrical equal-area projection, with
        /// zero as the westernmost point.</param>
        /// <param name="y">The y coordinate of a point on a cylindrical equal-area projection, with
        /// zero as the northernmost point.</param>
        /// <param name="resolution">The vertical resolution of the projection.</param>
        /// <returns>
        /// The latitude and longitude of the given coordinates, in radians.
        /// </returns>
        public static (double latitude, double longitude) GetLatLonOfCylindricalEqualAreaProjectionFromLocalPosition(
            Planetoid planet,
            SurfaceRegion region,
            int x, int y,
            int resolution)
            => GetLatLonOfCylindricalEqualAreaProjection(
                x, y,
                resolution,
                planet.VectorToLongitude(region.Position),
                planet.VectorToLatitude(region.Position),
                range: (double)((Frustum)region.Shape).FieldOfViewAngle);

        /// <summary>
        /// Calculates the latitude and longitude that correspond to a set of coordinates from an
        /// equirectangular projection.
        /// </summary>
        /// <param name="planet">The planet being mapped.</param>
        /// <param name="region">The region being mapped.</param>
        /// <param name="x">The x coordinate of a point on an equirectangular projection, with zero
        /// as the westernmost point.</param>
        /// <param name="y">The y coordinate of a point on an equirectangular projection, with zero
        /// as the northernmost point.</param>
        /// <param name="resolution">The vertical resolution of the projection.</param>
        /// <returns>
        /// The latitude and longitude of the given coordinates, in radians.
        /// </returns>
        public static (double latitude, double longitude) GetLatLonOfEquirectangularProjectionFromLocalPosition(
            Planetoid planet,
            SurfaceRegion region,
            int x, int y,
            int resolution)
            => GetLatLonOfEquirectangularProjection(
                x, y,
                resolution,
                planet.VectorToLongitude(region.Position),
                planet.VectorToLatitude(region.Position),
                range: (double)((Frustum)region.Shape).FieldOfViewAngle);

        /// <summary>
        /// Calculates the position that corresponds to a set of coordinates from a cylindrical
        /// equal-area projection.
        /// </summary>
        /// <param name="planet">The planet being mapped.</param>
        /// <param name="region">The region being mapped.</param>
        /// <param name="x">The x coordinate of a point on a cylindrical equal-area projection, with
        /// zero as the westernmost point.</param>
        /// <param name="y">The y coordinate of a point on a cylindrical equal-area projection, with
        /// zero as the northernmost point.</param>
        /// <param name="resolution">The vertical resolution of the projection.</param>
        /// <returns>
        /// The local position of the given coordinates, in radians.
        /// </returns>
        public static Vector3 GetLocalPositionFromCylindricalEqualAreaProjection(
            Planetoid planet,
            SurfaceRegion region,
            int x, int y,
            int resolution)
        {
            var (lat, lon) = GetLatLonOfCylindricalEqualAreaProjection(
                x, y,
                resolution,
                planet.VectorToLongitude(region.Position),
                planet.VectorToLatitude(region.Position),
                range: (double)((Frustum)region.Shape).FieldOfViewAngle);
            return planet.LatitudeAndLongitudeToVector(lat, lon) - region.Position;
        }

        /// <summary>
        /// Calculates the position that corresponds to a set of coordinates from an equirectangular
        /// projection.
        /// </summary>
        /// <param name="planet">The planet being mapped.</param>
        /// <param name="region">The region being mapped.</param>
        /// <param name="x">The x coordinate of a point on an equirectangular projection, with zero
        /// as the westernmost point.</param>
        /// <param name="y">The y coordinate of a point on an equirectangular projection, with zero
        /// as the northernmost point.</param>
        /// <param name="resolution">The vertical resolution of the projection.</param>
        /// <returns>
        /// The local position of the given coordinates, in radians.
        /// </returns>
        public static Vector3 GetLocalPositionFromEquirectangularProjection(
            Planetoid planet,
            SurfaceRegion region,
            int x, int y,
            int resolution)
        {
            var (lat, lon) = GetLatLonOfEquirectangularProjection(
                x, y,
                resolution,
                planet.VectorToLongitude(region.Position),
                planet.VectorToLatitude(region.Position),
                range: (double)((Frustum)region.Shape).FieldOfViewAngle);
            return planet.LatitudeAndLongitudeToVector(lat, lon) - region.Position;
        }

        /// <summary>
        /// Gets the precipitation value for a <paramref name="position"/> in a <paramref
        /// name="region"/> at a given <paramref name="moment"/> from a set of maps.
        /// </summary>
        /// <param name="planet">The mapped planet.</param>
        /// <param name="region">The mapped region.</param>
        /// <param name="position">A position relative to the center of <paramref
        /// name="region"/>.</param>
        /// <param name="maps">A set of precipitation maps.</param>
        /// <param name="moment">The time at which the calculation is to be performed.</param>
        /// <param name="equalArea">
        /// If <see langword="true"/> the projection will be a cylindrical equal-area projection.
        /// Otherwise, an equirectangular projection will be used.
        /// </param>
        /// <returns>The precipitation value for a <paramref name="position"/> in a <paramref
        /// name="region"/> at a given <paramref name="moment"/> from a set of maps.</returns>
        public static double GetPrecipitationFromLocalPosition(
            this Planetoid planet,
            SurfaceRegion region,
            Vector3 position,
            PrecipitationMaps[] maps,
            Instant moment,
            bool equalArea = false)
        {
            if (maps.Length == 0)
            {
                return 0;
            }

            int x, y;
            if (equalArea)
            {
                (x, y) = GetCylindricalEqualAreaProjectionFromLocalPosition(
                    planet,
                    region,
                    position,
                    maps[0].PrecipitationMap.GetLength(0));
            }
            else
            {
                (x, y) = GetEquirectangularProjectionFromLocalPosition(
                    planet,
                    region,
                    position,
                    maps[0].PrecipitationMap.GetLength(0));
            }
            return InterpolateAmongWeatherMaps(maps, planet.GetProportionOfYearAtTime(moment), map => map.PrecipitationMap[x][y]);
        }

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
        /// <param name="centralMeridian">The longitude of the central meridian of the projection,
        /// in radians.</param>
        /// <param name="centralParallel">The latitude of the central parallel of the projection, in
        /// radians.</param>
        /// <param name="standardParallels">The latitude of the standard parallels (north and south
        /// of the equator) where the scale of the projection is 1:1, in radians. Zero indicates the
        /// equator. It does not matter whether the positive or negative latitude is provided, if it
        /// is non-zero. If <see langword="null"/>, the <paramref name="centralParallel"/> will be
        /// used.</param>
        /// <param name="range">If provided, indicates the latitude range (north and south of
        /// <paramref name="centralParallel"/>) shown on the projection, in radians. If not
        /// provided, or if equal to zero or greater than π, indicates that the entire globe is
        /// shown.</param>
        /// <param name="equalArea">
        /// If <see langword="true"/> the projection will be a cylindrical equal-area projection.
        /// Otherwise, an equirectangular projection will be used.
        /// </param>
        /// <returns>The radius of the given point, in meters.</returns>
        public static Number GetSeparationOfPoint(
            Number radius,
            int x, int y,
            int resolution,
            double? centralMeridian = null,
            double? centralParallel = null,
            double? standardParallels = null,
            double? range = null,
            bool equalArea = false)
            => GetSeparationOfPointFromRadiusSquared(
                radius.Square(),
                x, y,
                resolution,
                centralMeridian,
                centralParallel,
                standardParallels,
                range,
                equalArea);

        /// <summary>
        /// Calculates the approximate distance by which the given point is separated from its
        /// neighbors on a map projection with the given characteristics, by transforming the point
        /// and its nearest neighbors to latitude and longitude, and averaging the distances between
        /// them.
        /// </summary>
        /// <param name="planet">The mapped planet.</param>
        /// <param name="region">The mapped region.</param>
        /// <param name="x">The x coordinate of a point on a map projection, with zero as the
        /// westernmost point.</param>
        /// <param name="y">The y coordinate of a point on a map projection, with zero as the
        /// northernmost point.</param>
        /// <param name="resolution">The vertical resolution of the projection.</param>
        /// <param name="equalArea">
        /// If <see langword="true"/> the projection will be a cylindrical equal-area projection.
        /// Otherwise, an equirectangular projection will be used.
        /// </param>
        /// <returns>The area of the given point, in m².</returns>
        public static Number GetSeparationOfPoint(
            this Planetoid planet,
            SurfaceRegion region,
            int x, int y,
            int resolution,
            bool equalArea = false)
            => GetSeparationOfPointFromRadiusSquared(
                planet.RadiusSquared,
                x, y,
                resolution,
                planet.VectorToLongitude(region.Position),
                planet.VectorToLatitude(region.Position),
                range: (double)((Frustum)region.Shape).FieldOfViewAngle,
                equalArea: equalArea);

        /// <summary>
        /// Gets the snowfall value for a <paramref name="position"/> in a <paramref name="region"/>
        /// at a given <paramref name="moment"/> from a set of maps.
        /// </summary>
        /// <param name="planet">The mapped planet.</param>
        /// <param name="region">The mapped region.</param>
        /// <param name="position">A position relative to the center of <paramref
        /// name="region"/>.</param>
        /// <param name="maps">A set of precipitation maps.</param>
        /// <param name="moment">The time at which the calculation is to be performed.</param>
        /// <param name="equalArea">
        /// If <see langword="true"/> the projection will be a cylindrical equal-area projection.
        /// Otherwise, an equirectangular projection will be used.
        /// </param>
        /// <returns>The snowfall value for a <paramref name="position"/> in a <paramref name="region"/>
        /// at a given <paramref name="moment"/> from a set of maps.</returns>
        public static double GetSnowfallFromLocalPosition(
            this Planetoid planet,
            SurfaceRegion region,
            Vector3 position,
            PrecipitationMaps[] maps,
            Instant moment,
            bool equalArea = false)
        {
            if (maps.Length == 0)
            {
                return 0;
            }

            int x, y;
            if (equalArea)
            {
                (x, y) = GetCylindricalEqualAreaProjectionFromLocalPosition(
                    planet,
                    region,
                    position,
                    maps[0].PrecipitationMap.GetLength(0));
            }
            else
            {
                (x, y) = GetEquirectangularProjectionFromLocalPosition(
                    planet,
                    region,
                    position,
                    maps[0].PrecipitationMap.GetLength(0));
            }
            return InterpolateAmongWeatherMaps(maps, planet.GetProportionOfYearAtTime(moment), map => map.SnowfallMap[x][y]);
        }

        /// <summary>
        /// <para>
        /// Produces a set of equirectangular projections of the specified region describing the
        /// surface and climate.
        /// </para>
        /// <para>
        /// This method is more efficient than generating each map separately.
        /// </para>
        /// </summary>
        /// <param name="planet">The planet being mapped.</param>
        /// <param name="resolution">The vertical resolution of the projection.</param>
        /// <param name="centralMeridian">The longitude of the central meridian of the projection,
        /// in radians.</param>
        /// <param name="centralParallel">The latitude of the central parallel of the projection, in
        /// radians.</param>
        /// <param name="standardParallels">The latitude of the standard parallels (north and south
        /// of the equator) where the scale of the projection is 1:1, in radians. Zero indicates the
        /// equator (the plate carrée projection). It does not matter whether the positive or
        /// negative latitude is provided, if it is non-zero. If <see langword="null"/>, the
        /// <paramref name="centralParallel"/> will be used.</param>
        /// <param name="range">If provided, indicates the latitude range (north and south of
        /// <paramref name="centralParallel"/>) shown on the projection, in radians. If not
        /// provided, or if equal to zero or greater than π, indicates that the entire globe is
        /// shown.</param>
        /// <param name="equalArea">
        /// If <see langword="true"/> the projection will be a cylindrical equal-area projection.
        /// Otherwise, an equirectangular projection will be used.
        /// </param>
        /// <param name="steps">The number of weather map sets which will be generated, at equal
        /// times throughout the course of one solar year. The first step will be offset so that its
        /// midpoint occurs at the winter solstice. The greater the number of sets (and thus, the
        /// shorter the time span represented by each step), the more accurate the results will be,
        /// at the cost of increased processing time. If zero is passed, the return value will be
        /// empty.</param>
        /// <returns>A <see cref="SurfaceMaps"/> instance.</returns>
        /// <remarks>
        /// Bear in mind that the calculations required to produce this map data are expensive, and
        /// the method may take prohibitively long to complete for large resolutions. Callers should
        /// strongly consider generating low-resolution maps, then using standard enlargement
        /// techniques or tools to expand the results to fit the intended view or texture size.
        /// Unlike photographic images, which can lose clarity with excessive expansion, this type
        /// of data is likely to be nearly as accurate when interpolating between low-resolution
        /// data points as when explicitly calculating values for each high-resolution point, since
        /// this data will nearly always follow relatively smooth local gradients.
        /// </remarks>
        public static SurfaceMaps GetSurfaceMaps(
            this Planetoid planet,
            int resolution,
            double? centralMeridian = null,
            double? centralParallel = null,
            double? standardParallels = null,
            double? range = null,
            bool equalArea = false,
            int steps = 12)
        {
            int xResolution;
            var mapScaleFactor = 0.0;
            if (equalArea)
            {
                mapScaleFactor = Math.Cos(standardParallels ?? centralParallel ?? 0);
                var aspectRatio = Math.PI * mapScaleFactor * mapScaleFactor;
                xResolution = (int)Math.Round(resolution * aspectRatio);
            }
            else
            {
                xResolution = resolution * 2;
            }

            var elevationMap = GetElevationMap(
                planet,
                xResolution,
                resolution,
                mapScaleFactor,
                out var maxElevation,
                centralMeridian,
                centralParallel,
                standardParallels,
                range,
                equalArea);
            var totalElevation = 0.0;

            for (var x = 0; x < xResolution; x++)
            {
                for (var y = 0; y < resolution; y++)
                {
                    totalElevation += elevationMap[x][y];
                }
            }
            var averageElevation = totalElevation / (xResolution * resolution);

            var weatherMapSet = GetWeatherMaps(
                planet,
                xResolution,
                resolution,
                mapScaleFactor,
                centralMeridian,
                centralParallel,
                standardParallels,
                range,
                steps,
                elevationMap,
                averageElevation,
                maxElevation,
                equalArea);
            var hydrologyMaps = GetHydrologyMaps(
                planet,
                xResolution,
                resolution,
                mapScaleFactor,
                centralMeridian,
                centralParallel,
                standardParallels,
                range,
                elevationMap,
                maxElevation,
                weatherMapSet.TotalPrecipitationMap,
                averageElevation,
                equalArea);
            return new SurfaceMaps(elevationMap, averageElevation, maxElevation, weatherMapSet, hydrologyMaps);
        }

        /// <summary>
        /// <para>
        /// Produces a set of map projections of the specified <paramref name="region"/> describing
        /// the surface and climate, taking into account any overlays.
        /// </para>
        /// <para>
        /// This method is more efficient than generating each map separately.
        /// </para>
        /// </summary>
        /// <param name="planet">The planet being mapped.</param>
        /// <param name="region">The region being mapped.</param>
        /// <param name="resolution">The vertical resolution of the projection.</param>
        /// <param name="equalArea">
        /// If <see langword="true"/> the projection will be a cylindrical equal-area projection.
        /// Otherwise, an equirectangular projection will be used.
        /// </param>
        /// <param name="steps">The number of weather map sets which will be generated, at equal
        /// times throughout the course of one solar year. The first step will be offset so that its
        /// midpoint occurs at the winter solstice. The greater the number of sets (and thus, the
        /// shorter the time span represented by each step), the more accurate the results will be,
        /// at the cost of increased processing time. If zero is passed, the return value will be
        /// empty.</param>
        /// <returns>A <see cref="SurfaceMaps"/> instance.</returns>
        /// <remarks>
        /// Bear in mind that the calculations required to produce this map data are expensive, and
        /// the method may take prohibitively long to complete for large resolutions. Callers should
        /// strongly consider generating low-resolution maps, then using standard enlargement
        /// techniques or tools to expand the results to fit the intended view or texture size.
        /// Unlike photographic images, which can lose clarity with excessive expansion, this type
        /// of data is likely to be nearly as accurate when interpolating between low-resolution
        /// data points as when explicitly calculating values for each high-resolution point, since
        /// this data will nearly always follow relatively smooth local gradients.
        /// </remarks>
        public static SurfaceMaps GetSurfaceMaps(
            this Planetoid planet,
            SurfaceRegion region,
            int resolution,
            bool equalArea = false,
            int steps = 12)
        {
            var centralParallel = planet.VectorToLatitude(region.Position);

            var elevationMap = planet.GetElevationMap(region, resolution, out var maxElevation, equalArea);
            var totalElevation = 0.0;

            int xResolution;
            var mapScaleFactor = 0.0;
            if (equalArea)
            {
                mapScaleFactor = Math.Cos(centralParallel);
                var aspectRatio = Math.PI * mapScaleFactor * mapScaleFactor;
                xResolution = (int)Math.Round(resolution * aspectRatio);
            }
            else
            {
                xResolution = resolution * 2;
            }

            for (var x = 0; x < xResolution; x++)
            {
                for (var y = 0; y < resolution; y++)
                {
                    totalElevation += elevationMap[x][y];
                }
            }
            var averageElevation = totalElevation / (xResolution * resolution);

            var weatherMapSet = planet.GetWeatherMaps(
                region,
                xResolution,
                resolution,
                mapScaleFactor,
                centralParallel,
                steps,
                elevationMap,
                averageElevation,
                maxElevation,
                equalArea);
            var hydrologyMaps = planet.GetHydrologyMaps(
                region,
                xResolution,
                resolution,
                mapScaleFactor,
                centralParallel,
                elevationMap,
                maxElevation,
                weatherMapSet.TotalPrecipitationMap,
                averageElevation,
                equalArea);
            return new SurfaceMaps(elevationMap, averageElevation, maxElevation, weatherMapSet, hydrologyMaps);
        }

        /// <summary>
        /// Gets a specific temperature value from a temperature range.
        /// </summary>
        /// <param name="planet">The mapped planet.</param>
        /// <param name="range">The range being interpolated.</param>
        /// <param name="moment">The time at which the calculation is to be performed.</param>
        /// <param name="latitude">The latitude at which the calculation is being performed (used to
        /// determine hemisphere, and thus season).</param>
        /// <returns>The specific temperature value from a temperature range.</returns>
        public static float GetTemperatureRangeValue(
            this Planetoid planet,
            FloatRange range,
            Instant moment,
            double latitude)
            => range.Min.Lerp(range.Max, (float)planet.GetSeasonalProportionAtTime(moment, latitude));

        /// <summary>
        /// Gets the temperature value for a <paramref name="position"/> in a <paramref
        /// name="region"/> at a given <paramref name="moment"/> from a set of temperature ranges.
        /// </summary>
        /// <param name="planet">The mapped planet.</param>
        /// <param name="region">The mapped region.</param>
        /// <param name="position">A position relative to the center of <paramref
        /// name="region"/>.</param>
        /// <param name="temperatureRanges">A set of temperature ranges.</param>
        /// <param name="moment">The time at which the calculation is to be performed.</param>
        /// <param name="equalArea">
        /// If <see langword="true"/> the projection will be a cylindrical equal-area projection.
        /// Otherwise, an equirectangular projection will be used.
        /// </param>
        /// <returns>The temperature value for a <paramref name="position"/> in a <paramref
        /// name="region"/> at a given <paramref name="moment"/> from a set of temperature
        /// ranges.</returns>
        public static float GetTemperatureFromLocalPosition(
            this Planetoid planet,
            SurfaceRegion region,
            Vector3 position,
            FloatRange[][] temperatureRanges,
            Instant moment,
            bool equalArea = false)
        {
            int x, y;
            if (equalArea)
            {
                (x, y) = GetCylindricalEqualAreaProjectionFromLocalPosition(
                    planet,
                    region,
                    position,
                    temperatureRanges.GetLength(0));
            }
            else
            {
                (x, y) = GetEquirectangularProjectionFromLocalPosition(
                    planet,
                    region,
                    position,
                    temperatureRanges.GetLength(0));
            }
            return planet.GetTemperatureRangeValue(
                temperatureRanges[x][y],
                moment,
                planet.VectorToLatitude(region.Position + position));
        }

        /// <summary>
        /// Gets the total precipitation value at a given <paramref name="moment"/> from a set of
        /// maps.
        /// </summary>
        /// <param name="planet">The mapped planet.</param>
        /// <param name="maps">A set of precipitation maps.</param>
        /// <param name="moment">The time at which the calculation is to be performed.</param>
        /// <returns>The total precipitation value at a given <paramref name="moment"/> from a set of
        /// maps.</returns>
        public static FloatRange GetTotalPrecipitationAtTime(
            this Planetoid planet,
            PrecipitationMaps[] maps,
            Instant moment) => maps.Length == 0
            ? FloatRange.Zero
            : InterpolateAmongWeatherMaps(maps, planet.GetProportionOfYearAtTime(moment), map => map.Precipitation);

        /// <summary>
        /// Gets the value for a <paramref name="position"/> in a <paramref name="region"/> from a
        /// set of values.
        /// </summary>
        /// <param name="planet">The mapped planet.</param>
        /// <param name="region">The mapped region.</param>
        /// <param name="position">A position relative to the center of <paramref
        /// name="region"/>.</param>
        /// <param name="values">A set of values.</param>
        /// <param name="equalArea">
        /// If <see langword="true"/> the projection will be a cylindrical equal-area projection.
        /// Otherwise, an equirectangular projection will be used.
        /// </param>
        /// <returns>The value for a <paramref name="position"/> in a <paramref name="region"/> from
        /// a set of values.</returns>
        public static T GetValueFromLocalPosition<T>(
            this Planetoid planet,
            SurfaceRegion region,
            Vector3 position,
            T[,] values,
            bool equalArea = false)
        {
            int x, y;
            if (equalArea)
            {
                (x, y) = GetCylindricalEqualAreaProjectionFromLocalPosition(
                    planet,
                    region,
                    position,
                    values.GetLength(0));
            }
            else
            {
                (x, y) = GetEquirectangularProjectionFromLocalPosition(
                    planet,
                    region,
                    position,
                    values.GetLength(0));
            }
            return values[x, y];
        }

        /// <summary>
        /// <para>
        /// Produces a set of map projections of the specified region describing the climate.
        /// </para>
        /// </summary>
        /// <param name="planet">The planet being mapped.</param>
        /// <param name="resolution">The vertical resolution of the projection.</param>
        /// <param name="centralMeridian">The longitude of the central meridian of the projection,
        /// in radians.</param>
        /// <param name="centralParallel">The latitude of the central parallel of the projection, in
        /// radians.</param>
        /// <param name="standardParallels">The latitude of the standard parallels (north and south
        /// of the equator) where the scale of the projection is 1:1, in radians. Zero indicates the
        /// equator. It does not matter whether the positive or negative latitude is provided, if it
        /// is non-zero. If <see langword="null"/>, the <paramref name="centralParallel"/> will be
        /// used.</param>
        /// <param name="range">If provided, indicates the latitude range (north and south of
        /// <paramref name="centralParallel"/>) shown on the projection, in radians. If not
        /// provided, or if equal to zero or greater than π, indicates that the entire globe is
        /// shown.</param>
        /// <param name="steps">The number of map sets which will be generated, at equal times
        /// throughout the course of one solar year. The first step will be offset so that its
        /// midpoint occurs at the winter solstice. The greater the number of sets (and thus, the
        /// shorter the time span represented by each step), the more accurate the results will be,
        /// at the cost of increased processing time. Values less than 1 will be treated as
        /// 1.</param>
        /// <param name="elevationMap">The elevation map for this region. If left <see
        /// langword="null"/> one will be generated. A pre-generated map <i>must</i> share the same
        /// projection parameters (<paramref name="resolution"/>, <paramref
        /// name="centralMeridian"/>, etc.). If it does not, a new one will be generated
        /// anyway.</param>
        /// <param name="averageElevation">The average elevation of the area. If left <see
        /// langword="null"/> it will be calculated.</param>
        /// <param name="maxElevation">
        /// The maximum elevation of the mapped region, in meters. If left <see langword="null"/> a
        /// new elevation map will be generated and the value calculated.
        /// </param>
        /// <param name="equalArea">
        /// If <see langword="true"/> the projection will be a cylindrical equal-area projection.
        /// Otherwise, an equirectangular projection will be used.
        /// </param>
        /// <returns>A <see cref="WeatherMaps"/> instance with an array of <see
        /// cref="PrecipitationMaps"/> instances equal to the number of <paramref name="steps"/>
        /// specified, starting with the "season" whose midpoint is at the winter solstice of the
        /// northern hemisphere.</returns>
        /// <remarks>
        /// Bear in mind that the calculations required to produce this weather data are expensive,
        /// and the method may take prohibitively long to complete for large resolutions. Callers
        /// should strongly consider generating low-resolution maps, then using standard enlargement
        /// techniques or tools to expand the results to fit the intended view or texture size.
        /// Unlike photographic images, which can lose clarity with excessive expansion, this type
        /// of weather data is likely to be nearly as accurate when interpolating between
        /// low-resolution data points as when explicitly calculating values for each
        /// high-resolution point, since this data will nearly always follow relatively smooth local
        /// gradients.
        /// </remarks>
        public static WeatherMaps GetWeatherMaps(
            this Planetoid planet,
            int resolution,
            double? centralMeridian = null,
            double? centralParallel = null,
            double? standardParallels = null,
            double? range = null,
            int steps = 12,
            double[][]? elevationMap = null,
            double? averageElevation = null,
            double? maxElevation = null,
            bool equalArea = false)
        {
            int xResolution;
            var mapScaleFactor = 0.0;
            if (equalArea)
            {
                mapScaleFactor = Math.Cos(standardParallels ?? centralParallel ?? 0);
                var aspectRatio = Math.PI * mapScaleFactor * mapScaleFactor;
                xResolution = (int)Math.Round(resolution * aspectRatio);
            }
            else
            {
                xResolution = resolution * 2;
            }

            return GetWeatherMaps(
                planet,
                xResolution,
                resolution,
                mapScaleFactor,
                centralMeridian,
                centralParallel,
                standardParallels,
                range,
                steps,
                elevationMap,
                averageElevation,
                maxElevation,
                equalArea);
        }

        /// <summary>
        /// <para>
        /// Produces a set of map projections of the specified <paramref name="region"/>
        /// describing the climate, taking into account any overlays.
        /// </para>
        /// </summary>
        /// <param name="planet">The planet being mapped.</param>
        /// <param name="region">The region being mapped.</param>
        /// <param name="resolution">The vertical resolution of the projection.</param>
        /// <param name="steps">The number of map sets which will be generated, at equal times
        /// throughout the course of one solar year. The first step will be offset so that its
        /// midpoint occurs at the winter solstice. The greater the number of sets (and thus, the
        /// shorter the time span represented by each step), the more accurate the results will be,
        /// at the cost of increased processing time. Values less than 1 will be treated as
        /// 1.</param>
        /// <param name="elevationMap">The elevation map for this region. If left <see
        /// langword="null"/> one will be generated. A pre-generated map <i>must</i> share the same
        /// projection parameters (<paramref name="resolution"/>). If it does not, a new one will be
        /// generated anyway.</param>
        /// <param name="averageElevation">The average elevation of the area. If left <see
        /// langword="null"/> it will be calculated.</param>
        /// <param name="maxElevation">
        /// The maximum elevation of the mapped region, in meters. If left <see langword="null"/> a
        /// new elevation map will be generated and the value calculated.
        /// </param>
        /// <param name="equalArea">
        /// If <see langword="true"/> the projection will be a cylindrical equal-area projection.
        /// Otherwise, an equirectangular projection will be used.
        /// </param>
        /// <returns>A <see cref="WeatherMaps"/> instance with an array of <see
        /// cref="PrecipitationMaps"/> instances equal to the number of <paramref name="steps"/>
        /// specified, starting with the "season" whose midpoint is at the winter solstice of the
        /// northern hemisphere.</returns>
        /// <remarks>
        /// Bear in mind that the calculations required to produce this weather data are expensive,
        /// and the method may take prohibitively long to complete for large resolutions. Callers
        /// should strongly consider generating low-resolution maps, then using standard enlargement
        /// techniques or tools to expand the results to fit the intended view or texture size.
        /// Unlike photographic images, which can lose clarity with excessive expansion, this type
        /// of weather data is likely to be nearly as accurate when interpolating between
        /// low-resolution data points as when explicitly calculating values for each
        /// high-resolution point, since this data will nearly always follow relatively smooth local
        /// gradients.
        /// </remarks>
        public static WeatherMaps GetWeatherMaps(
            this Planetoid planet,
            SurfaceRegion region,
            int resolution,
            int steps = 12,
            double[][]? elevationMap = null,
            double? averageElevation = null,
            double? maxElevation = null,
            bool equalArea = false)
        {
            var latitude = planet.VectorToLatitude(region.Position);

            int xResolution;
            var mapScaleFactor = 0.0;
            if (equalArea)
            {
                mapScaleFactor = Math.Cos(latitude);
                var aspectRatio = Math.PI * mapScaleFactor * mapScaleFactor;
                xResolution = (int)Math.Round(resolution * aspectRatio);
            }
            else
            {
                xResolution = resolution * 2;
            }

            return GetWeatherMaps(
                planet,
                region,
                xResolution,
                resolution,
                mapScaleFactor,
                latitude,
                steps,
                elevationMap,
                averageElevation,
                maxElevation,
                equalArea);
        }

        internal static (int x, int y) GetCylindricalEqualAreaProjectionFromLatLongWithScale(
            double latitude, double longitude,
            int yResolution,
            double scale,
            double? centralMeridian = null,
            double? centralParallel = null,
            double? standardParallels = null)
        {
            var scaleFactor = Math.Cos(standardParallels ?? centralParallel ?? 0);
            var aspectRatio = Math.PI * scaleFactor * scaleFactor;
            var xResolution = (int)Math.Round(yResolution * aspectRatio);
            var x = (int)Math.Round(((longitude - (centralMeridian ?? 0)) * scaleFactor / scale) + (xResolution / 2))
                .Clamp(0, xResolution - 1);
            var y = (int)Math.Round((yResolution / 2) - (Math.Sin(latitude) / scaleFactor / scale))
                .Clamp(0, yResolution - 1);
            return (x, y);
        }

        internal static (int x, int y) GetEquirectangularProjectionFromLatLongWithScale(
            double latitude, double longitude,
            int resolution,
            double scale,
            double? centralMeridian = null,
            double? centralParallel = null,
            double? standardParallels = null)
        {
            var x = (int)Math.Round(((longitude - (centralMeridian ?? 0)) * Math.Cos(standardParallels ?? centralParallel ?? 0) / scale) + resolution)
                .Clamp(0, (resolution * 2) - 1);
            var y = (int)Math.Round((resolution / 2) - ((latitude - (centralParallel ?? 0)) / scale))
                .Clamp(0, resolution - 1);
            return (x, y);
        }

        internal static T[][] GetInitializedArray<T>(int xLength, int yLength, T defaultValue)
        {
            var arr = new T[xLength][];
            for (var x = 0; x < xLength; x++)
            {
                arr[x] = new T[yLength];
                for (var y = 0; y < yLength; y++)
                {
                    arr[x][y] = defaultValue;
                }
            }
            return arr;
        }

        internal static T[][] GetSurfaceMap<T>(
            Func<double, double, long, long, T> func,
            int xResolution,
            int yResolution,
            double mapScaleFactor,
            double? centralMeridian = null,
            double? centralParallel = null,
            double? standardParallels = null,
            double? range = null,
            bool equalArea = false)
        {
            if (yResolution > 32767)
            {
                throw new ArgumentOutOfRangeException(nameof(yResolution), $"The value of {nameof(yResolution)} cannot exceed 32767.");
            }

            var map = new T[xResolution][];
            if (equalArea)
            {
                var scale = range.HasValue && range.Value < Math.PI && !range.Value.IsNearlyZero()
                    ? 2.0 / (yResolution * range.Value)
                    : 2.0 / yResolution;
                for (var x = 0; x < xResolution; x++)
                {
                    map[x] = new T[yResolution];
                    for (var y = 0; y < yResolution; y++)
                    {
                        var (latitude, longitude) = GetLatLonOfCylindricalEqualAreaProjectionFromAdjustedCoordinates(x, y, xResolution, yResolution, scale, mapScaleFactor, centralMeridian);
                        map[x][y] = func(latitude, longitude, x, y);
                    }
                }
            }
            else
            {
                var scale = range.HasValue && range.Value < Math.PI && !range.Value.IsNearlyZero()
                    ? MathAndScience.Constants.Doubles.MathConstants.PISquared / (yResolution * range.Value)
                    : Math.PI / yResolution;
                for (var x = 0; x < xResolution; x++)
                {
                    map[x] = new T[yResolution];
                    for (var y = 0; y < yResolution; y++)
                    {
                        var (latitude, longitude) = GetLatLonOfEquirectangularProjectionFromAdjustedCoordinates(x, y, yResolution, scale, centralMeridian, centralParallel, standardParallels);
                        map[x][y] = func(latitude, longitude, x, y);
                    }
                }
            }
            return map;
        }

        private static Number GetAreaOfPointFromRadiusSquared(
            Number radiusSquared,
            int x, int y,
            int xResolution,
            int yResolution,
            double mapScaleFactor,
            double? centralMeridian = null,
            double? centralParallel = null,
            double? standardParallels = null,
            double? range = null,
            bool equalArea = false)
        {
            // left: x - 1, y
            var left = x == 0
                ? xResolution - 1
                : x - 1;
            // up: x, y - 1
            var up = y == 0
                ? yResolution - 1
                : y - 1;
            // right: x + 1, y
            var right = x == yResolution - 1
                ? 0
                : x + 1;
            // down: x, y + 1
            var down = y == yResolution - 1
                ? 0
                : y + 1;

            double latCenter, lonCenter, lonLeft, lonRight, latUp, latDown;
            if (equalArea)
            {
                var scale = range.HasValue && range.Value < Math.PI && !range.Value.IsNearlyZero()
                    ? 2.0 / (yResolution * range.Value)
                    : 2.0 / yResolution;
                (latCenter, lonCenter) = GetLatLonOfCylindricalEqualAreaProjectionFromAdjustedCoordinates(x, y, xResolution, yResolution, scale, mapScaleFactor, centralMeridian);
                (_, lonLeft) = GetLatLonOfCylindricalEqualAreaProjectionFromAdjustedCoordinates(left, y, xResolution, yResolution, scale, mapScaleFactor, centralMeridian);
                (_, lonRight) = GetLatLonOfCylindricalEqualAreaProjectionFromAdjustedCoordinates(right, y, xResolution, yResolution, scale, mapScaleFactor, centralMeridian);
                (latUp, _) = GetLatLonOfCylindricalEqualAreaProjectionFromAdjustedCoordinates(x, up, xResolution, yResolution, scale, mapScaleFactor, centralMeridian);
                (latDown, _) = GetLatLonOfCylindricalEqualAreaProjectionFromAdjustedCoordinates(x, down, xResolution, yResolution, scale, mapScaleFactor, centralMeridian);
            }
            else
            {
                var scale = range.HasValue && range.Value < Math.PI && !range.Value.IsNearlyZero()
                    ? MathAndScience.Constants.Doubles.MathConstants.PISquared / (yResolution * range.Value)
                    : Math.PI / yResolution;
                (latCenter, lonCenter) = GetLatLonOfEquirectangularProjectionFromAdjustedCoordinates(x, y, yResolution, scale, centralMeridian, centralParallel, standardParallels);
                (_, lonLeft) = GetLatLonOfEquirectangularProjectionFromAdjustedCoordinates(left, y, yResolution, scale, centralMeridian, centralParallel, standardParallels);
                (_, lonRight) = GetLatLonOfEquirectangularProjectionFromAdjustedCoordinates(right, y, yResolution, scale, centralMeridian, centralParallel, standardParallels);
                (latUp, _) = GetLatLonOfEquirectangularProjectionFromAdjustedCoordinates(x, up, yResolution, scale, centralMeridian, centralParallel, standardParallels);
                (latDown, _) = GetLatLonOfEquirectangularProjectionFromAdjustedCoordinates(x, down, yResolution, scale, centralMeridian, centralParallel, standardParallels);
            }

            var lonLeftBorder = (lonLeft + lonCenter) / 2;
            var lonRightBorder = (lonRight + lonCenter) / 2;
            var latTopBorder = (latUp + latCenter) / 2;
            var latBottomBorder = (latDown + latCenter) / 2;

            return radiusSquared
                * (Math.Abs(Math.Sin(latBottomBorder) - Math.Sin(latTopBorder))
                * Math.Abs(lonRightBorder - lonLeftBorder));
        }

        private static double[][] GetElevationMap(
            this Planetoid planet,
            int xResolution,
            int resolution,
            double mapScaleFactor,
            out double max,
            double? centralMeridian = null,
            double? centralParallel = null,
            double? standardParallels = null,
            double? range = null,
            bool equalArea = false)
        {
            double maxNormalized;
            double[][] map;
            if (planet.HasElevationMap)
            {
                map = planet.GetElevationMap(resolution * 2, resolution, out maxNormalized);
            }
            else
            {
                map = GetSurfaceMap(
                    (lat, lon, _, __) => planet.GetNormalizedElevationAt(planet.LatitudeAndLongitudeToDoubleVector(lat, lon)),
                    xResolution,
                    resolution,
                    mapScaleFactor,
                    centralMeridian,
                    centralParallel,
                    standardParallels,
                    range,
                    equalArea);
                maxNormalized = 0;
                for (var x = 0; x < xResolution; x++)
                {
                    for (var y = 0; y < resolution; y++)
                    {
                        maxNormalized = Math.Max(maxNormalized, Math.Abs(map[x][y]));
                    }
                }
                var scale = 1 / maxNormalized;
                for (var x = 0; x < xResolution; x++)
                {
                    for (var y = 0; y < resolution; y++)
                    {
                        map[x][y] *= scale;
                    }
                }
            }
            max = maxNormalized * planet.MaxElevation;
            return map;
        }

        private static float[][] GetHillShadeMap(
            this Planetoid planet,
            int xResolution,
            int resolution,
            double mapScaleFactor,
            double scaleFactor = 1,
            double? centralMeridian = null,
            double? centralParallel = null,
            double? standardParallels = null,
            double? range = null,
            double[][]? elevationMap = null,
            bool equalArea = false,
            bool scaleIsRelative = false)
        {
            if (planet is null)
            {
                throw new ArgumentNullException(nameof(planet));
            }
            if (resolution > int.MaxValue / 2)
            {
                throw new ArgumentOutOfRangeException(nameof(resolution), $"The value of {nameof(resolution)} cannot exceed half of int.MaxValue ({int.MaxValue / 2}).");
            }

            if (elevationMap is null || elevationMap.Length != xResolution || (resolution > 0 && elevationMap[0].Length != resolution))
            {
                elevationMap = planet.GetElevationMap(
                    xResolution,
                    resolution,
                    mapScaleFactor,
                    out _,
                    centralMeridian,
                    centralParallel,
                    standardParallels,
                    range,
                    equalArea);
            }

            return GetHillShadeMap(elevationMap, scaleFactor, scaleIsRelative);
        }

        private static HydrologyMaps GetHydrologyMaps(
            this Planetoid planet,
            int xResolution,
            int resolution,
            double mapScaleFactor,
            double? centralMeridian = null,
            double? centralParallel = null,
            double? standardParallels = null,
            double? range = null,
            double[][]? elevationMap = null,
            double? maxElevation = null,
            float[][]? precipitationMap = null,
            double? averageElevation = null,
            bool equalArea = false)
        {
            float[][] depthMap, flowMap;
            if (planet.HasHydrologyMaps)
            {
                depthMap = planet.GetDepthMap(xResolution, resolution);
                flowMap = planet.GetFlowMap(xResolution, resolution);
                return new HydrologyMaps(depthMap, flowMap, planet.MaxFlow);
            }

            if (elevationMap is null || !maxElevation.HasValue || elevationMap.Length != xResolution || (resolution > 0 && elevationMap[0].Length != resolution))
            {
                elevationMap = planet.GetElevationMap(
                    xResolution,
                    resolution,
                    mapScaleFactor,
                    out var maxE,
                    centralMeridian,
                    centralParallel,
                    standardParallels,
                    range,
                    equalArea);
                maxElevation = maxE;
            }
            if (precipitationMap is null || precipitationMap.Length != xResolution || (resolution > 0 && precipitationMap[0].Length != resolution))
            {
                precipitationMap = GetWeatherMaps(
                    planet,
                    xResolution,
                    resolution,
                    mapScaleFactor,
                    centralMeridian,
                    centralParallel,
                    standardParallels,
                    range,
                    1,
                    elevationMap,
                    averageElevation,
                    maxElevation,
                    equalArea)
                    .TotalPrecipitationMap;
            }

            // Set each point's drainage to be its neighbor with the lowest elevation.
            // Only consider points above sea level.
            var drainage = new (int x, int y)[xResolution][];
            for (var i = 0; i < drainage.Length; i++)
            {
                drainage[i] = new (int x, int y)[resolution];
            }
            for (var x = 0; x < xResolution; x++)
            {
                for (var y = 0; y < resolution; y++)
                {
                    if (elevationMap[x][y] <= 0)
                    {
                        drainage[x][y] = (x, y);
                        continue;
                    }

                    var dX = -1;
                    var dY = -1;
                    for (var x1 = x - 1; x1 <= x + 1; x1++)
                    {
                        int xN;
                        if (x1 == -1)
                        {
                            xN = xResolution - 1;
                        }
                        else if (x1 == xResolution)
                        {
                            xN = 0;
                        }
                        else
                        {
                            xN = x1;
                        }

                        for (var y1 = y - 1; y1 <= y + 1; y1++)
                        {
                            int yN;
                            if (y1 == -1)
                            {
                                yN = resolution - 1;
                            }
                            else if (y1 == resolution)
                            {
                                yN = 0;
                            }
                            else
                            {
                                yN = y1;
                            }

                            if (dX == -1 || elevationMap[xN][yN] < elevationMap[dX][dY])
                            {
                                dX = xN;
                                dY = yN;
                            }
                        }
                    }
                    drainage[x][y] = (dX, dY);
                }
            }

            // Find the final drainage point of each point (the last drainage point in the chain,
            // which has itself as its own drainage point). Keep track of final drainage points
            // which are above sea level.
            var finalDrainage = new (int x, int y)[xResolution][];
            for (var i = 0; i < finalDrainage.Length; i++)
            {
                finalDrainage[i] = new (int x, int y)[resolution];
            }
            var lakes = new Dictionary<(int x, int y), HashSet<(int x, int y)>>();
            for (var x = 0; x < xResolution; x++)
            {
                for (var y = 0; y < resolution; y++)
                {
                    finalDrainage[x][y] = FindFinalDrainage(x, y);
                    if (finalDrainage[x][y] != (x, y) && elevationMap[finalDrainage[x][y].x][finalDrainage[x][y].y] > 0)
                    {
                        if (!lakes.ContainsKey((finalDrainage[x][y].x, finalDrainage[x][y].y)))
                        {
                            lakes[(finalDrainage[x][y].x, finalDrainage[x][y].y)] = new HashSet<(int x, int y)> { (x, y) };
                        }
                        lakes[(finalDrainage[x][y].x, finalDrainage[x][y].y)].Add((x, y));
                    }
                }
            }
            (int x, int y) FindFinalDrainage(int x, int y)
            {
                if (drainage[x][y] == (x, y))
                {
                    return (x, y);
                }
                else
                {
                    return FindFinalDrainage(drainage[x][y].x, drainage[x][y].y);
                }
            }

            // Find the depth of every lake: the elevation of the lowest point which drains to
            // that point, but has a neighbor which does not.
            var lakeDepths = new Dictionary<(int x, int y), float>();
            foreach (var ((xL, yL), points) in lakes)
            {
                var lowX = -1;
                var lowY = -1;
                foreach (var (x, y) in points)
                {
                    var found = false;
                    for (var x1 = x - 1; x1 <= x + 1; x1++)
                    {
                        int xN;
                        if (x1 == -1)
                        {
                            xN = xResolution - 1;
                        }
                        else if (x1 == xResolution)
                        {
                            xN = 0;
                        }
                        else
                        {
                            xN = x1;
                        }

                        if (found)
                        {
                            break;
                        }
                        for (var y1 = y - 1; y1 <= y + 1; y1++)
                        {
                            if (x1 == x && y1 == y)
                            {
                                continue;
                            }
                            int yN;
                            if (y1 == -1)
                            {
                                yN = resolution - 1;
                            }
                            else if (y1 == resolution)
                            {
                                yN = 0;
                            }
                            else
                            {
                                yN = y1;
                            }

                            if (finalDrainage[xN][yN] != finalDrainage[x][y]
                                && (lowX == -1 || elevationMap[x][y] < elevationMap[lowX][lowY]))
                            {
                                lowX = x;
                                lowY = y;
                                found = true;
                                break;
                            }
                        }
                    }
                }
                lakeDepths.Add((xL, yL), lowX == -1 ? 0 : (float)(elevationMap[lowX][lowY] - elevationMap[xL][yL]));
            }

            double? area = null;
            var halfResolution = resolution / 2;
            var areaMap = new double[halfResolution + 1];
            for (var y = 0; y <= halfResolution; y++)
            {
                areaMap[y] = -1;
            }
            var flows = new List<(int, int, float)>();
            for (var x = 0; x < xResolution; x++)
            {
                for (var y = 0; y < resolution; y++)
                {
                    if (!precipitationMap[x][y].IsNearlyZero())
                    {
                        var effectiveY = !equalArea && y > halfResolution
                            ? halfResolution - (y - halfResolution)
                            : y;
                        if (equalArea && !area.HasValue)
                        {
                            area = (double)GetAreaOfPointFromRadiusSquared(
                                planet.RadiusSquared,
                                x,
                                y,
                                xResolution,
                                resolution,
                                mapScaleFactor,
                                centralMeridian,
                                centralParallel,
                                standardParallels,
                                range,
                                equalArea);
                        }
                        else if (!equalArea && areaMap[effectiveY] < 0)
                        {
                            areaMap[effectiveY] = (double)GetAreaOfPointFromRadiusSquared(
                                planet.RadiusSquared,
                                x,
                                y,
                                xResolution,
                                resolution,
                                mapScaleFactor,
                                centralMeridian,
                                centralParallel,
                                standardParallels,
                                range,
                                equalArea);
                        }
                        var runoff = (float)(precipitationMap[x][y]
                            * planet.Atmosphere.MaxPrecipitation
                            * 0.001
                            * (equalArea ? area!.Value : areaMap[effectiveY])
                            / (double)(planet.Orbit?.Period ?? 31557600));
                        if (drainage[x][y] != (x, y))
                        {
                            flows.Add((drainage[x][y].x, drainage[x][y].y, runoff));
                        }
                    }
                }
            }

            var maxFlow = 0.0;
            flowMap = new float[xResolution][];
            for (var i = 0; i < flowMap.Length; i++)
            {
                flowMap[i] = new float[resolution];
            }
            while (flows.Count > 0)
            {
                var newFlows = new List<(int, int, float)>();
                foreach (var (x, y, flow) in flows)
                {
                    flowMap[x][y] += flow;
                    maxFlow = Math.Max(maxFlow, flowMap[x][y]);
                    if (drainage[x][y] != (x, y))
                    {
                        newFlows.Add((drainage[x][y].x, drainage[x][y].y, flow));
                    }
                }
                flows = newFlows;
            }
            for (var x = 0; x < xResolution; x++)
            {
                for (var y = 0; y < resolution; y++)
                {
                    flowMap[x][y] = maxFlow.IsNearlyZero() ? 0 : (float)(flowMap[x][y] / maxFlow);
                }
            }

            // For each lake point which has any inflow, set all points which drain to it to its
            // depth. Lakes with no inflow are either merely local low points with no actual water
            // flow, or are single-point depressions whose runoff is presumed to move through a
            // subsurface water table until reaching a point where more extended flow begins. This
            // avoids single-point fills with no outflows that would otherwise fill every localized
            // low point.
            depthMap = new float[xResolution][];
            for (var i = 0; i < depthMap.Length; i++)
            {
                depthMap[i] = new float[resolution];
            }
            var elevationFactor = maxElevation.Value / planet.MaxElevation;
            foreach (var ((xL, yL), points) in lakes)
            {
                if (flowMap[xL][yL].IsNearlyZero())
                {
                    continue;
                }
                var depth = (float)(lakeDepths[(xL, yL)] * elevationFactor);
                depthMap[xL][yL] = depth;
                foreach (var (x, y) in points)
                {
                    depthMap[x][y] = depth;
                }
            }

            return new HydrologyMaps(depthMap, flowMap, maxFlow);
        }

        private static HydrologyMaps GetHydrologyMaps(
            this Planetoid planet,
            SurfaceRegion region,
            int xResolution,
            int resolution,
            double mapScaleFactor,
            double centralParallel,
            double[][]? elevationMap = null,
            double? maxElevation = null,
            float[][]? precipitationMap = null,
            double? averageElevation = null,
            bool equalArea = false)
        {
            if (elevationMap is null || !maxElevation.HasValue || elevationMap.Length != xResolution || (resolution > 0 && elevationMap[0].Length != resolution))
            {
                elevationMap = planet.GetElevationMap(region, resolution, out var maxE, equalArea);
                maxElevation = maxE;
            }
            if (precipitationMap is null || precipitationMap.Length != xResolution || (resolution > 0 && precipitationMap[0].Length != resolution))
            {
                precipitationMap = planet.GetWeatherMaps(region, xResolution, resolution, mapScaleFactor, centralParallel, 1, elevationMap, averageElevation, maxElevation, equalArea)
                    .TotalPrecipitationMap;
            }
            if (region.HasHydrologyMaps)
            {
                return new HydrologyMaps(
                    region.GetDepthMap(xResolution, resolution),
                    region.GetFlowMap(xResolution, resolution),
                    region._maxFlow ?? 0);
            }
            var hydrologyMaps = planet.GetHydrologyMaps(
                xResolution,
                resolution,
                mapScaleFactor,
                planet.VectorToLongitude(region.Position),
                centralParallel,
                range: (double)(region.Shape.ContainingRadius / planet.RadiusSquared),
                elevationMap: elevationMap,
                maxElevation: maxElevation,
                precipitationMap: precipitationMap,
                averageElevation: averageElevation,
                equalArea: equalArea);
            if (!region.HasDepthMap && (!region.HasFlowMap || !region._maxFlow.HasValue))
            {
                return hydrologyMaps;
            }
            var depth = region.HasDepthMap ? region.GetDepthMap(xResolution, resolution) : hydrologyMaps.Depth;
            var flow = region.HasFlowMap && region._maxFlow.HasValue
                ? region.GetFlowMap(xResolution, resolution)
                : hydrologyMaps.Flow;
            var maxFlow = region.HasFlowMap && region._maxFlow.HasValue
                ? region._maxFlow.Value
                : hydrologyMaps.MaxFlow;
            return new HydrologyMaps(depth, flow, maxFlow);
        }

        private static (double latitude, double longitude) GetLatLonOfCylindricalEqualAreaProjectionFromAdjustedCoordinates(
            long x, long y,
            int xResolution,
            int yResolution,
            double scale,
            double scaleFactor,
            double? centralMeridian)
            => (Math.Asin((((yResolution / 2) - y) * scale * scaleFactor).Clamp(-1, 1)),
            ((x - (xResolution / 2)) * scale / scaleFactor) + (centralMeridian ?? 0));

        private static (double latitude, double longitude) GetLatLonOfEquirectangularProjectionFromAdjustedCoordinates(
            long x, long y,
            int resolution,
            double scale,
            double? centralMeridian,
            double? centralParallel,
            double? standardParallels = null)
            => ((((resolution / 2) - y) * scale) + (centralParallel ?? 0),
            ((x - resolution) * scale / Math.Cos(standardParallels ?? centralParallel ?? 0)) + (centralMeridian ?? 0));

        private static Number GetSeparationOfPointFromRadiusSquared(
            Number radiusSquared,
            int x, int y,
            int resolution,
            double? centralMeridian,
            double? centralParallel,
            double? standardParallels = null,
            double? range = null,
            bool equalArea = false)
        {
            int xResolution;
            var mapScaleFactor = 0.0;
            if (equalArea)
            {
                mapScaleFactor = Math.Cos(standardParallels ?? centralParallel ?? 0);
                var aspectRatio = Math.PI * mapScaleFactor * mapScaleFactor;
                xResolution = (int)Math.Round(resolution * aspectRatio);
            }
            else
            {
                xResolution = resolution * 2;
            }

            // left: x - 1, y
            var left = x == 0
                ? xResolution - 1
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
                ? 0
                : y + 1;

            double latCenter, lonLeft, lonRight, latUp, latDown;
            if (equalArea)
            {
                var scale = range.HasValue && range.Value < Math.PI && !range.Value.IsNearlyZero()
                    ? 2.0 / (resolution * range.Value)
                    : 2.0 / resolution;
                (latCenter, _) = GetLatLonOfCylindricalEqualAreaProjectionFromAdjustedCoordinates(x, y, xResolution, resolution, scale, mapScaleFactor, centralMeridian);
                (_, lonLeft) = GetLatLonOfCylindricalEqualAreaProjectionFromAdjustedCoordinates(left, y, xResolution, resolution, scale, mapScaleFactor, centralMeridian);
                (_, lonRight) = GetLatLonOfCylindricalEqualAreaProjectionFromAdjustedCoordinates(right, y, xResolution, resolution, scale, mapScaleFactor, centralMeridian);
                (latUp, _) = GetLatLonOfCylindricalEqualAreaProjectionFromAdjustedCoordinates(x, up, xResolution, resolution, scale, mapScaleFactor, centralMeridian);
                (latDown, _) = GetLatLonOfCylindricalEqualAreaProjectionFromAdjustedCoordinates(x, down, xResolution, resolution, scale, mapScaleFactor, centralMeridian);
            }
            else
            {
                var scale = range.HasValue && range.Value < Math.PI && !range.Value.IsNearlyZero()
                    ? MathAndScience.Constants.Doubles.MathConstants.PISquared / (resolution * range.Value)
                    : Math.PI / resolution;
                (latCenter, _) = GetLatLonOfEquirectangularProjectionFromAdjustedCoordinates(x, y, resolution, scale, centralMeridian, centralParallel, standardParallels);
                (_, lonLeft) = GetLatLonOfEquirectangularProjectionFromAdjustedCoordinates(left, y, resolution, scale, centralMeridian, centralParallel, standardParallels);
                (_, lonRight) = GetLatLonOfEquirectangularProjectionFromAdjustedCoordinates(right, y, resolution, scale, centralMeridian, centralParallel, standardParallels);
                (latUp, _) = GetLatLonOfEquirectangularProjectionFromAdjustedCoordinates(x, up, resolution, scale, centralMeridian, centralParallel, standardParallels);
                (latDown, _) = GetLatLonOfEquirectangularProjectionFromAdjustedCoordinates(x, down, resolution, scale, centralMeridian, centralParallel, standardParallels);
            }

            var latTopBorder = (latUp + latCenter) / 2;
            var latBottomBorder = (latDown + latCenter) / 2;

            return radiusSquared
                * ((Math.Abs(Math.Sin(latBottomBorder) - Math.Sin(latTopBorder)) + Math.Abs(lonRight - lonLeft)) / 2);
        }

        private static WeatherMaps GetWeatherMaps(
            this Planetoid planet,
            int xResolution,
            int yResolution,
            double mapScaleFactor,
            double? centralMeridian = null,
            double? centralParallel = null,
            double? standardParallels = null,
            double? range = null,
            int steps = 12,
            double[][]? elevationMap = null,
            double? averageElevation = null,
            double? maxElevation = null,
            bool equalArea = false)
        {
            if (planet is null)
            {
                throw new ArgumentNullException(nameof(planet));
            }
            if (yResolution > int.MaxValue / 2)
            {
                throw new ArgumentOutOfRangeException(nameof(yResolution), $"The value of {nameof(yResolution)} cannot exceed half of int.MaxValue ({int.MaxValue / 2}).");
            }
            if (steps > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(steps), $"The value of {nameof(steps)} cannot exceed int.MaxValue ({int.MaxValue}).");
            }
            steps = Math.Max(1, steps);

            var total = xResolution * yResolution;

            if (elevationMap is null || !maxElevation.HasValue || elevationMap.Length != xResolution || (yResolution > 0 && elevationMap[0].Length != yResolution))
            {
                elevationMap = planet.GetElevationMap(
                    xResolution,
                    yResolution,
                    mapScaleFactor,
                    out var maxE,
                    centralMeridian,
                    centralParallel,
                    standardParallels,
                    range,
                    equalArea);
                maxElevation = maxE;
            }

            float[][][] precipitationMaps, snowfallMaps;
            PrecipitationMaps[] precipMaps;
            if (planet.HasAllWeatherMaps)
            {
                precipitationMaps = planet.GetPrecipitationMaps(xResolution, yResolution);
                snowfallMaps = planet.GetSnowfallMaps(xResolution, yResolution);
                precipMaps = new PrecipitationMaps[precipitationMaps.Length];
                for (var i = 0; i < precipitationMaps.Length; i++)
                {
                    precipMaps[i] = new PrecipitationMaps(precipitationMaps[i], snowfallMaps[i]);
                }
                return new WeatherMaps(
                    planet,
                    elevationMap,
                    maxElevation.Value,
                    precipMaps,
                    planet.GetTemperatureMap(xResolution, yResolution),
                    centralMeridian,
                    centralParallel,
                    standardParallels,
                    range,
                    equalArea,
                    averageElevation);
            }

            FloatRange[][] temperatureRanges;
            if (planet.HasTemperatureMap)
            {
                temperatureRanges = planet.GetTemperatureMap(xResolution, yResolution);
            }
            else
            {
                var tilt = planet.AxialTilt;
                var winterTrueAnomaly = planet.WinterSolsticeTrueAnomaly;
                var summerTrueAnomaly = planet.SummerSolsticeTrueAnomaly;
                var winterLatitudes = new Dictionary<double, double>();
                var summerLatitudes = new Dictionary<double, double>();
                var latitudeTemperatures = new Dictionary<double, double>();
                var elevationTemperatures = new Dictionary<(double, int), float>();
                temperatureRanges = GetSurfaceMap(
                    (lat, _, x, y) =>
                    {
                        var roundedElevation = (int)Math.Round(Math.Max(0, elevationMap![x][y] * maxElevation.Value) / 100) * 100;

                        if (!winterLatitudes.TryGetValue(lat, out var winterLat))
                        {
                            winterLat = Math.Abs(Planetoid.GetSeasonalLatitudeFromDeclination(lat, tilt));
                            winterLatitudes.Add(lat, winterLat);
                        }
                        if (!latitudeTemperatures.TryGetValue(winterLat, out var winterTemp))
                        {
                            winterTemp = planet.GetSurfaceTemperatureAtTrueAnomaly(winterTrueAnomaly, winterLat);
                            latitudeTemperatures.Add(winterLat, winterTemp);
                        }
                        if (!elevationTemperatures.TryGetValue((winterTemp, roundedElevation), out var winterTempAtElevation))
                        {
                            winterTempAtElevation = (float)planet.GetTemperatureAtElevation(winterTemp, roundedElevation);
                            elevationTemperatures.Add((winterTemp, roundedElevation), winterTempAtElevation);
                        }

                        if (!summerLatitudes.TryGetValue(lat, out var summerLat))
                        {
                            summerLat = Math.Abs(Planetoid.GetSeasonalLatitudeFromDeclination(lat, -tilt));
                            summerLatitudes.Add(lat, summerLat);
                        }
                        if (!latitudeTemperatures.TryGetValue(summerLat, out var summerTemp))
                        {
                            summerTemp = planet.GetSurfaceTemperatureAtTrueAnomaly(summerTrueAnomaly, summerLat);
                            latitudeTemperatures.Add(summerLat, summerTemp);
                        }
                        if (!elevationTemperatures.TryGetValue((summerTemp, roundedElevation), out var summerTempAtElevation))
                        {
                            summerTempAtElevation = (float)planet.GetTemperatureAtElevation(summerTemp, roundedElevation);
                            elevationTemperatures.Add((summerTemp, roundedElevation), summerTempAtElevation);
                        }

                        return winterTempAtElevation > summerTempAtElevation
                            ? new FloatRange(summerTempAtElevation, winterTempAtElevation)
                            : new FloatRange(winterTempAtElevation, summerTempAtElevation);
                    },
                    xResolution,
                    yResolution,
                    mapScaleFactor,
                    centralMeridian,
                    centralParallel,
                    standardParallels,
                    range,
                    equalArea);
            }

            if (planet.HasPrecipitationMap && planet.HasSnowfallMap && planet.MappedSeasons >= steps)
            {
                precipitationMaps = planet.GetPrecipitationMaps(xResolution, yResolution);
                snowfallMaps = planet.GetSnowfallMaps(xResolution, yResolution);
                precipMaps = new PrecipitationMaps[precipitationMaps.Length];
                var step = planet.MappedSeasons == steps ? 1 : Math.Max(1, (int)Math.Floor(planet.MappedSeasons / (double)steps));
                for (var i = 0; i < precipitationMaps.Length; i += step)
                {
                    precipMaps[i] = new PrecipitationMaps(precipitationMaps[i], snowfallMaps[i]);
                }
            }
            else
            {
                var proportionOfYear = 1f / steps;
                var proportionOfYearAtMidpoint = 0f;
                var proportionOfSummerAtMidpoint = 0f;
                var trueAnomaly = planet.WinterSolsticeTrueAnomaly;
                var trueAnomalyPerSeason = MathAndScience.Constants.Doubles.MathConstants.TwoPI / steps;

                precipMaps = new PrecipitationMaps[steps];
                for (var i = 0; i < steps; i++)
                {
                    var solarDeclination = planet.GetSolarDeclination(trueAnomaly);

                    // Precipitation & snowfall
                    var snowfallMap = new float[xResolution][];
                    for (var j = 0; j < snowfallMap.Length; j++)
                    {
                        snowfallMap[j] = new float[yResolution];
                    }
                    var precipMap = GetSurfaceMap(
                        (lat, lon, x, y) =>
                        {
                            if (planet.Atmosphere.MaxPrecipitation.IsNearlyZero())
                            {
                                snowfallMap[x][y] = 0;
                                return 0;
                            }
                            else
                            {
                                var precipitation = planet.GetPrecipitation(
                                    planet.LatitudeAndLongitudeToDoubleVector(lat, lon),
                                    Planetoid.GetSeasonalLatitudeFromDeclination(lat, solarDeclination),
                                    temperatureRanges[x][y].Min.Lerp(temperatureRanges[x][y].Max, lat < 0 ? 1 - proportionOfSummerAtMidpoint : proportionOfSummerAtMidpoint),
                                    proportionOfYear,
                                    out var snow);
                                snowfallMap[x][y] = (float)(snow / planet.Atmosphere.MaxSnowfall);
                                return (float)(precipitation / planet.Atmosphere.MaxPrecipitation);
                            }
                        },
                        xResolution,
                        yResolution,
                        mapScaleFactor,
                        centralMeridian,
                        centralParallel,
                        standardParallels,
                        range,
                        equalArea);
                    precipMaps[i] = new PrecipitationMaps(precipMap, snowfallMap);

                    proportionOfYearAtMidpoint += proportionOfYear;
                    proportionOfSummerAtMidpoint = 1 - (Math.Abs(0.5f - proportionOfYearAtMidpoint) / 0.5f);
                    trueAnomaly += trueAnomalyPerSeason;
                    if (trueAnomaly >= MathAndScience.Constants.Doubles.MathConstants.TwoPI)
                    {
                        trueAnomaly -= MathAndScience.Constants.Doubles.MathConstants.TwoPI;
                    }
                }
            }

            return new WeatherMaps(
                planet,
                elevationMap,
                maxElevation.Value,
                precipMaps,
                temperatureRanges,
                centralMeridian,
                centralParallel,
                standardParallels,
                range,
                equalArea,
                averageElevation);
        }

        private static WeatherMaps GetWeatherMaps(
            this Planetoid planet,
            SurfaceRegion region,
            int xResolution,
            int resolution,
            double mapScaleFactor,
            double latitude,
            int steps = 12,
            double[][]? elevationMap = null,
            double? averageElevation = null,
            double? maxElevation = null,
            bool equalArea = false)
        {
            var longitude = planet.VectorToLongitude(region.Position);
            var range = (double)((Frustum)region.Shape).FieldOfViewAngle;

            if (elevationMap is null || !maxElevation.HasValue || elevationMap.Length != xResolution || (resolution > 0 && elevationMap[0].Length != resolution))
            {
                elevationMap = planet.GetElevationMap(region, resolution, out var maxE, equalArea);
                maxElevation = maxE;
            }

            float[][][] precipitationMaps, snowfallMaps;
            PrecipitationMaps[] precipMaps;

            if (region.HasAllWeatherMaps)
            {
                precipitationMaps = region.GetPrecipitationMaps(xResolution, resolution);
                snowfallMaps = region.GetSnowfallMaps(xResolution, resolution);
                precipMaps = new PrecipitationMaps[precipitationMaps.Length];
                for (var i = 0; i < precipitationMaps.Length; i++)
                {
                    precipMaps[i] = new PrecipitationMaps(precipitationMaps[i], snowfallMaps[i]);
                }
                return new WeatherMaps(
                    planet,
                    elevationMap,
                    maxElevation.Value,
                    precipMaps,
                    region.GetTemperatureMap(xResolution, resolution),
                    longitude,
                    latitude,
                    range: range,
                    equalArea: equalArea,
                    averageElevation: averageElevation);
            }

            var weatherMaps = planet.GetWeatherMaps(
                xResolution,
                resolution,
                mapScaleFactor,
                longitude,
                latitude,
                range: range,
                steps: steps,
                elevationMap: elevationMap,
                averageElevation: averageElevation,
                maxElevation: maxElevation,
                equalArea: equalArea);
            if (!region.HasAnyWeatherMaps)
            {
                return weatherMaps;
            }

            precipitationMaps = region.HasPrecipitationMap
                ? region.GetPrecipitationMaps(xResolution, resolution)
                : weatherMaps.PrecipitationMaps.Select(x => x.PrecipitationMap).ToArray();
            snowfallMaps = region.HasSnowfallMap
                ? region.GetSnowfallMaps(xResolution, resolution)
                : weatherMaps.PrecipitationMaps.Select(x => x.SnowfallMap).ToArray();
            precipMaps = new PrecipitationMaps[precipitationMaps.Length];
            for (var i = 0; i < precipitationMaps.Length; i++)
            {
                precipMaps[i] = new PrecipitationMaps(precipitationMaps[i], snowfallMaps[i]);
            }

            var tempRanges = region.HasTemperatureMap
                ? region.GetTemperatureMap(xResolution, resolution)
                : weatherMaps.TemperatureRangeMap;

            return new WeatherMaps(
                planet,
                elevationMap,
                maxElevation.Value,
                precipMaps,
                tempRanges,
                longitude,
                latitude,
                range: range,
                equalArea: equalArea,
                averageElevation: averageElevation);
        }

        private static float InterpolateAmongWeatherMaps(PrecipitationMaps[] maps, double proportionOfYear, Func<PrecipitationMaps, float> getValueFromMap)
        {
            if (maps.Length == 0)
            {
                return 0;
            }
            var proportionPerSeason = 1.0 / maps.Length;
            var seasonIndex = (int)Math.Floor(proportionOfYear / proportionPerSeason);
            var nextSeasonIndex = seasonIndex == maps.Length - 1 ? 0 : seasonIndex + 1;
            var weight = (float)((proportionOfYear - (seasonIndex * proportionPerSeason)) / proportionPerSeason);
            return getValueFromMap.Invoke(maps[seasonIndex]).Lerp(getValueFromMap.Invoke(maps[nextSeasonIndex]), weight);
        }

        private static FloatRange InterpolateAmongWeatherMaps(PrecipitationMaps[] maps, double proportionOfYear, Func<PrecipitationMaps, FloatRange> getValueFromMap)
        {
            if (maps.Length == 0)
            {
                return FloatRange.Zero;
            }
            var proportionPerSeason = 1.0 / maps.Length;
            var seasonIndex = (int)Math.Floor(proportionOfYear / proportionPerSeason);
            var weight = (float)((proportionOfYear - (seasonIndex * proportionPerSeason)) / proportionPerSeason);
            var value1 = getValueFromMap.Invoke(maps[seasonIndex]);
            var value2 = getValueFromMap.Invoke(maps[seasonIndex == maps.Length - 1 ? 0 : seasonIndex + 1]);
            return new FloatRange(
                value1.Min.Lerp(value2.Min, weight),
                value1.Average.Lerp(value2.Average, weight),
                value1.Max.Lerp(value2.Max, weight));
        }
    }
}
