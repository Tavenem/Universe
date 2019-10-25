using NeverFoundry.MathAndScience;
using NeverFoundry.MathAndScience.Constants.Numbers;
using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.MathAndScience.Time;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using WorldFoundry.CelestialBodies.Planetoids;
using WorldFoundry.Place;

namespace WorldFoundry.SurfaceMapping
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
        /// <param name="planet">The mapped planet.</param>
        /// <param name="range">The range being interpolated.</param>
        /// <param name="time">The time at which the calculation is to be performed.</param>
        /// <returns>The specific value from a range which varies over the course of a
        /// year.</returns>
        public static float GetAnnualRangeValue(
            this Planetoid planet,
            FloatRange range,
            Duration time)
            => range.Min.Lerp(range.Max, (float)planet.GetProportionOfYearAtTime(time));

        /// <summary>
        /// Determines whether the given <paramref name="time"/> falls within the range indicated.
        /// </summary>
        /// <param name="planet">The mapped planet.</param>
        /// <param name="range">The range being interpolated.</param>
        /// <param name="time">The time at which the determination is to be performed.</param>
        /// <returns><see langword="true"/> if the range indicates a positive result for the given
        /// <paramref name="time"/>; otherwise <see langword="false"/>.</returns>
        public static bool GetAnnualRangeIsPositiveAtTime(
            this Planetoid planet,
            FloatRange range,
            Duration time)
        {
            var proportionOfYear = (float)planet.GetProportionOfYearAtTime(time);
            return !range.IsZero
            && (range.Min > range.Max
                ? proportionOfYear >= range.Min || proportionOfYear <= range.Max
                : proportionOfYear >= range.Min && proportionOfYear <= range.Max);
        }

        /// <summary>
        /// Gets the value for a <paramref name="position"/> in a <paramref name="region"/> at a
        /// given <paramref name="time"/> from a set of ranges.
        /// </summary>
        /// <param name="planet">The mapped planet.</param>
        /// <param name="region">The mapped region.</param>
        /// <param name="position">A position relative to the center of <paramref
        /// name="region"/>.</param>
        /// <param name="ranges">A set of ranges.</param>
        /// <param name="time">The time at which the calculation is to be performed.</param>
        /// <returns>The value for a <paramref name="position"/> in a <paramref name="region"/> at a
        /// given <paramref name="time"/> from a set of ranges.</returns>
        public static float GetAnnualValueFromLocalPosition(
            this Planetoid planet,
            SurfaceRegion region,
            Vector3 position,
            FloatRange[,] ranges,
            Duration time)
        {
            var (x, y) = GetEquirectangularProjectionFromLocalPosition(
                planet,
                region,
                position,
                ranges.GetLength(0));
            return planet.GetAnnualRangeValue(
                ranges[x, y],
                time);
        }

        /// <summary>
        /// Determines whether the given <paramref name="time"/> falls within the range indicated
        /// for a <paramref name="position"/> in a <paramref name="region"/>.
        /// </summary>
        /// <param name="planet">The mapped planet.</param>
        /// <param name="region">The mapped region.</param>
        /// <param name="position">A position relative to the center of <paramref
        /// name="region"/>.</param>
        /// <param name="ranges">A set of ranges.</param>
        /// <param name="time">The time at which the determination is to be performed.</param>
        /// <returns><see langword="true"/> if the given <paramref name="time"/> falls within the
        /// range indicated for a <paramref name="position"/> in a <paramref name="region"/>;
        /// otherwise <see langword="false"/>.</returns>
        public static bool GetAnnualRangeIsPositiveAtTimeAndLocalPosition(
            this Planetoid planet,
            SurfaceRegion region,
            Vector3 position,
            FloatRange[,] ranges,
            Duration time)
        {
            var (x, y) = GetEquirectangularProjectionFromLocalPosition(
                planet,
                region,
                position,
                ranges.GetLength(0));
            return planet.GetAnnualRangeIsPositiveAtTime(ranges[x, y], time);
        }

        /// <summary>
        /// Calculates the approximate area of a point on an equirectangular projection with the
        /// given characteristics, by transforming the point and its nearest neighbors to latitude
        /// and longitude, calculating the midpoints between them, and calculating the area of the
        /// region enclosed within those midpoints.
        /// </summary>
        /// <param name="radius">The radius of the planet.</param>
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
        /// <returns>The area of the given point, in m².</returns>
        public static Number GetAreaOfPoint(
            Number radius,
            int x, int y,
            int resolution,
            double? centralMeridian = null,
            double? centralParallel = null,
            double? standardParallels = null,
            double? range = null)
            => GetAreaOfPointFromRadiusSquared(
                radius.Square(),
                x, y,
                resolution,
                centralMeridian,
                centralParallel,
                standardParallels,
                range);

        /// <summary>
        /// Calculates the approximate area of a point on an equirectangular projection with the
        /// given characteristics, by transforming the point and its nearest neighbors to latitude
        /// and longitude, calculating the midpoints between them, and calculating the area of the
        /// region enclosed within those midpoints.
        /// </summary>
        /// <param name="planet">The mapped planet.</param>
        /// <param name="region">The mapped region.</param>
        /// <param name="x">The x coordinate of a point on an equirectangular projection, with zero
        /// as the westernmost point.</param>
        /// <param name="y">The y coordinate of a point on an equirectangular projection, with zero
        /// as the northernmost point.</param>
        /// <param name="resolution">The vertical resolution of the projection.</param>
        /// <returns>The area of the given point, in m².</returns>
        public static Number GetAreaOfLocalPoint(
            this Planetoid planet,
            SurfaceRegion region,
            int x, int y,
            int resolution)
            => GetAreaOfPointFromRadiusSquared(
                planet.RadiusSquared,
                x,
                y,
                resolution,
                planet.VectorToLongitude(region.Position),
                planet.VectorToLatitude(region.Position),
                range: (double)((Frustum)region.Shape).FieldOfViewAngle);

        /// <summary>
        /// Produces an equirectangular projection of an elevation map of the specified region.
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
        /// <returns>
        /// A two-dimensional array of <see cref="float"/> values corresponding to points on an
        /// equirectangular projected map of the surface. The first index corresponds to the X
        /// coordinate, and the second index corresponds to the Y coordinate. The values are
        /// normalized elevations from -1 to 1, where negative values are below sea level and
        /// positive values are above sea level, and 1 is equal to the maximum elevation of this
        /// <see cref="Planetoid"/>.
        /// <seealso cref="Planetoid.MaxElevation"/>
        /// </returns>
        public static double[,] GetElevationMap(
            this Planetoid planet,
            int resolution,
            double? centralMeridian = null,
            double? centralParallel = null,
            double? standardParallels = null,
            double? range = null)
            => planet.HasElevationMap
            ? planet.GetElevationMap(resolution * 2, resolution)
            : GetSurfaceMap(
                (lat, lon, _, __) => planet.GetNormalizedElevationAt(planet.LatitudeAndLongitudeToDoubleVector(lat, lon)),
                resolution,
                centralMeridian,
                centralParallel,
                standardParallels,
                range);

        /// <summary>
        /// Produces an equirectangular projection of an elevation map of the specified <paramref
        /// name="region"/>, taking into account any overlay.
        /// </summary>
        /// <param name="planet">The planet being mapped.</param>
        /// <param name="region">The region being mapped.</param>
        /// <param name="resolution">The vertical resolution of the projection.</param>
        /// <returns>
        /// A two-dimensional array of <see cref="float"/> values corresponding to points on an
        /// equirectangular projected map of the surface. The first index corresponds to the X
        /// coordinate, and the second index corresponds to the Y coordinate. The values are
        /// normalized elevations from -1 to 1, where negative values are below sea level and
        /// positive values are above sea level, and 1 is equal to the maximum elevation of the
        /// planet.
        /// <seealso cref="Planetoid.MaxElevation"/>
        /// </returns>
        public static double[,] GetElevationMap(
            this Planetoid planet,
            SurfaceRegion region,
            int resolution)
            => region.HasElevationMap
            ? region.GetElevationMap(resolution * 2, resolution)
            : planet.GetElevationMap(
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
                range.HasValue && range.Value < MathConstants.PI && !range.Value.IsNearlyZero()
                    ? NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.PISquared / (resolution * range.Value)
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
                (long)x - resolution,
                (long)y - (resolution / 2),
                range.HasValue && range.Value < Math.PI && !range.Value.IsNearlyZero()
                    ? NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.PISquared / (resolution * range.Value)
                    : Math.PI / resolution,
                centralMeridian,
                centralParallel,
                standardParallels);

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
        public static (double latitude, double longitude) GetLatLonFromLocalPosition(
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
        /// Produces a set of equirectangular projections of the specified region describing the
        /// hydrology.
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
        /// <param name="elevationMap">The elevation map for this region. If left <see
        /// langword="null"/> one will be generated. A pre-generated map <i>must</i> share the same
        /// projection parameters (<paramref name="resolution"/>, <paramref
        /// name="centralMeridian"/>, etc.). If it does not, a new one will be generated
        /// anyway.</param>
        /// <param name="precipitationMap">An annual precipitation map for this region. If left <see
        /// langword="null"/> one will be generated. Note that pre-generating a precipitation map
        /// will usually be significantly more efficient and accurate than allowing the method to
        /// create one, since the quality of weather maps depends significantly on the number of
        /// steps used to generate it. A pre-generated map <i>must</i> share the same projection
        /// parameters (<paramref name="resolution"/>, <paramref name="centralMeridian"/>, etc.). If
        /// it does not, a new one will be generated anyway.</param>
        /// <param name="averageElevation">The average elevation of the area. If left <see
        /// langword="null"/> it will be calculated.</param>
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
            double[,]? elevationMap = null,
            float[,]? precipitationMap = null,
            double? averageElevation = null)
        {
            if (planet == null)
            {
                throw new ArgumentNullException(nameof(planet));
            }
            if (resolution > int.MaxValue / 2)
            {
                throw new ArgumentOutOfRangeException(nameof(resolution), $"The value of {nameof(resolution)} cannot exceed half of int.MaxValue ({(int.MaxValue / 2).ToString()}).");
            }
            var doubleResolution = resolution * 2;

            float[,] depthMap, flowMap;
            if (planet.HasHydrologyMaps)
            {
                depthMap = planet.GetDepthMap(doubleResolution, resolution);
                flowMap = planet.GetFlowMap(doubleResolution, resolution);
                return new HydrologyMaps(doubleResolution, resolution, depthMap, flowMap);
            }

            if (elevationMap == null || elevationMap.GetLength(0) != doubleResolution || elevationMap.GetLength(1) != resolution)
            {
                elevationMap = planet.GetElevationMap(resolution, centralMeridian, centralParallel, standardParallels, range);
            }
            if (precipitationMap == null || precipitationMap.GetLength(0) != doubleResolution || precipitationMap.GetLength(1) != resolution)
            {
                precipitationMap = GetWeatherMaps(planet, resolution, centralMeridian, centralParallel, standardParallels, range, 1, elevationMap, averageElevation).TotalPrecipitationMap;
            }

            // Set each point's drainage to be its neighbor with the lowest elevation.
            // Only consider points above sea level.
            var drainage = new (int x, int y)[doubleResolution, resolution];
            for (var x = 0; x < doubleResolution; x++)
            {
                for (var y = 0; y < resolution; y++)
                {
                    if (elevationMap[x, y] <= 0)
                    {
                        drainage[x, y] = (x, y);
                        continue;
                    }

                    var dX = -1;
                    var dY = -1;
                    for (var x1 = x - 1; x1 <= x + 1; x1++)
                    {
                        var xN = x1 == -1
                            ? doubleResolution - 1
                            : x1 == doubleResolution
                                ? 0
                                : x1;
                        for (var y1 = y - 1; y1 <= y + 1; y1++)
                        {
                            var yN = y1 == -1
                                ? resolution - 1
                                : y1 == resolution
                                    ? 0
                                    : y1;
                            if (dX == -1 || elevationMap[xN, yN] < elevationMap[dX, dY])
                            {
                                dX = xN;
                                dY = yN;
                            }
                        }
                    }
                    drainage[x, y] = (dX, dY);
                }
            }

            // Find the final drainage point of each point (the last drainage point in the chain,
            // which has itself as its own drainage point). Keep track of final drainage points
            // which are above sea level.
            var finalDrainage = new (int x, int y)[doubleResolution, resolution];
            var lakes = new Dictionary<(int x, int y), HashSet<(int x, int y)>>();
            for (var x = 0; x < doubleResolution; x++)
            {
                for (var y = 0; y < resolution; y++)
                {
                    finalDrainage[x, y] = FindFinalDrainage(x, y);
                    if (finalDrainage[x, y] != (x, y) && elevationMap[finalDrainage[x, y].x, finalDrainage[x, y].y] > 0)
                    {
                        if (!lakes.ContainsKey((finalDrainage[x, y].x, finalDrainage[x, y].y)))
                        {
                            lakes[(finalDrainage[x, y].x, finalDrainage[x, y].y)] = new HashSet<(int x, int y)>();
                        }
                        lakes[(finalDrainage[x, y].x, finalDrainage[x, y].y)].Add((x, y));
                    }
                }
            }
            (int x, int y) FindFinalDrainage(int x, int y)
            {
                if (drainage[x, y] == (x, y))
                {
                    return (x, y);
                }
                else
                {
                    return FindFinalDrainage(drainage[x, y].x, drainage[x, y].y);
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
                        var xN = x1 == -1
                            ? doubleResolution - 1
                            : x1 == doubleResolution
                                ? 0
                                : x1;
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
                            var yN = y1 == -1
                                ? resolution - 1
                                : y1 == resolution
                                    ? 0
                                    : y1;
                            if (finalDrainage[xN, yN] != finalDrainage[x, y]
                                && (lowX == -1 || elevationMap[x, y] < elevationMap[lowX, lowY]))
                            {
                                lowX = x;
                                lowY = y;
                                found = true;
                                break;
                            }
                        }
                    }
                }
                lakeDepths.Add((xL, yL), lowX == -1 ? 0 : (float)(elevationMap[lowX, lowY] - elevationMap[xL, yL]));
            }

            var areaMap = new double[doubleResolution, resolution];
            for (var x = 0; x < doubleResolution; x++)
            {
                for (var y = 0; y < resolution; y++)
                {
                    areaMap[x, y] = -1;
                }
            }

            var flows = new List<(int, int, float)>();
            for (var x = 0; x < doubleResolution; x++)
            {
                for (var y = 0; y < resolution; y++)
                {
                    if (!precipitationMap[x, y].IsNearlyZero())
                    {
                        if (areaMap[x, y] < 0)
                        {
                            areaMap[x, y] = (double)GetAreaOfPointFromRadiusSquared(planet.RadiusSquared, x, y, resolution, centralMeridian, centralParallel, standardParallels, range);
                        }
                        var runoff = (float)(precipitationMap[x, y] * planet.Atmosphere.MaxPrecipitation * 0.001 * areaMap[x, y] / (double)(planet.Orbit?.Period ?? 31557600));
                        if (drainage[x, y] != (x, y))
                        {
                            flows.Add((drainage[x, y].x, drainage[x, y].y, runoff));
                        }
                    }
                }
            }

            var maxFlow = 0.0;
            flowMap = new float[doubleResolution, resolution];
            while (flows.Count > 0)
            {
                var newFlows = new List<(int, int, float)>();
                foreach (var (x, y, flow) in flows)
                {
                    flowMap[x, y] += flow;
                    maxFlow = Math.Max(maxFlow, flowMap[x, y]);
                    if (drainage[x, y] != (x, y))
                    {
                        newFlows.Add((drainage[x, y].x, drainage[x, y].y, flow));
                    }
                }
                flows = newFlows;
            }
            for (var x = 0; x < doubleResolution; x++)
            {
                for (var y = 0; y < resolution; y++)
                {
                    flowMap[x, y] = (float)(flowMap[x, y] / maxFlow);
                }
            }

            // For each lake point which has any inflow, set all points which drain to it to its
            // depth. Lakes with no inflow are either merely local low points with no actual water
            // flow, or are single-point depressions whose runoff is presumed to move through a
            // subsurface water table until reaching a point where more extended flow begins. This
            // avoids single-point fills with no outflows that would otherwise fill every localized
            // low point.
            depthMap = new float[doubleResolution, resolution];
            foreach (var ((xL, yL), points) in lakes)
            {
                if (flowMap[xL, yL].IsNearlyZero())
                {
                    continue;
                }
                depthMap[xL, yL] = lakeDepths[(xL, yL)];
                foreach (var (x, y) in points)
                {
                    depthMap[xL, yL] = lakeDepths[(xL, yL)];
                }
            }

            if (maxFlow > planet.MaxFlow)
            {
                planet.MaxFlow = maxFlow;
            }

            return new HydrologyMaps(doubleResolution, resolution, depthMap, flowMap);
        }

        /// <summary>
        /// Produces a set of equirectangular projections of the specified <paramref name="region"/>
        /// describing the hydrology, taking into account any overlays.
        /// </summary>
        /// <param name="planet">The planet being mapped.</param>
        /// <param name="region">The region being mapped.</param>
        /// <param name="resolution">The vertical resolution of the projection.</param>
        /// <param name="elevationMap">The elevation map for this region. If left <see
        /// langword="null"/> one will be generated. A pre-generated map <i>must</i> share the same
        /// projection parameters (<paramref name="resolution"/>). If it does not, a new one will be
        /// generated anyway.</param>
        /// <param name="precipitationMap">An annual precipitation map for this region. If left <see
        /// langword="null"/> one will be generated. Note that pre-generating a precipitation map
        /// will usually be significantly more efficient and accurate than allowing the method to
        /// create one, since the quality of weather maps depends significantly on the number of
        /// steps used to generate it. A pre-generated map <i>must</i> share the same projection
        /// parameters (<paramref name="resolution"/>). If it does not, a new one will be generated
        /// anyway.</param>
        /// <param name="averageElevation">The average elevation of the area. If left <see
        /// langword="null"/> it will be calculated.</param>
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
            double[,]? elevationMap = null,
            float[,]? precipitationMap = null,
            double? averageElevation = null)
        {
            var doubleResolution = resolution * 2;
            if (elevationMap == null || elevationMap.GetLength(0) != doubleResolution || elevationMap.GetLength(1) != resolution)
            {
                elevationMap = planet.GetElevationMap(region, resolution);
            }
            if (precipitationMap == null || precipitationMap.GetLength(0) != doubleResolution || precipitationMap.GetLength(1) != resolution)
            {
                precipitationMap = planet.GetWeatherMaps(region, resolution, 1, elevationMap, averageElevation).TotalPrecipitationMap;
            }
            if (region.HasHydrologyMaps)
            {
                return new HydrologyMaps(
                    doubleResolution,
                    resolution,
                    region.GetDepthMap(doubleResolution, resolution),
                    region.GetFlowMap(doubleResolution, resolution));
            }
            var hydrologyMaps = planet.GetHydrologyMaps(
                  resolution,
                  planet.VectorToLongitude(region.Position),
                  planet.VectorToLatitude(region.Position),
                  range: (double)(region.Shape.ContainingRadius / planet.RadiusSquared),
                  elevationMap: elevationMap,
                  precipitationMap: precipitationMap,
                  averageElevation: averageElevation);
            if (!region.HasDepthMap && !region.HasFlowMap)
            {
                return hydrologyMaps;
            }
            var depth = region.HasDepthMap ? region.GetDepthMap(doubleResolution, resolution) : hydrologyMaps.Depth;
            var flow = region.HasFlowMap ? region.GetFlowMap(doubleResolution, resolution) : hydrologyMaps.Flow;
            return new HydrologyMaps(doubleResolution, resolution, depth, flow);
        }

        /// <summary>
        /// Gets the precipitation value for a <paramref name="position"/> in a <paramref
        /// name="region"/> at a given <paramref name="time"/> from a set of maps.
        /// </summary>
        /// <param name="planet">The mapped planet.</param>
        /// <param name="region">The mapped region.</param>
        /// <param name="position">A position relative to the center of <paramref
        /// name="region"/>.</param>
        /// <param name="maps">A set of precipitation maps.</param>
        /// <param name="time">The time at which the calculation is to be performed.</param>
        /// <returns></returns>
        public static double GetPrecipitationFromLocalPosition(
            this Planetoid planet,
            SurfaceRegion region,
            Vector3 position,
            PrecipitationMaps[] maps,
            Duration time)
        {
            if (maps.Length == 0)
            {
                return 0;
            }

            var (x, y) = GetEquirectangularProjectionFromLocalPosition(
                planet,
                region,
                position,
                maps[0].PrecipitationMap.GetLength(0));
            var proportion = planet.GetProportionOfYearAtTime(time);
            return InterpolateAmongWeatherMaps(maps, proportion, map => map.PrecipitationMap[x, y]);
        }

        /// <summary>
        /// Calculates the approximate distance by which the given point is separated from its
        /// neighbors on an equirectangular projection with the given characteristics, by
        /// transforming the point and its nearest neighbors to latitude and longitude, and
        /// averaging the distances between them.
        /// </summary>
        /// <param name="radius">The radius of the planet.</param>
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
        /// <returns>The radius of the given point, in meters.</returns>
        public static Number GetSeparationOfPoint(
            Number radius,
            int x, int y,
            int resolution,
            double? centralMeridian = null,
            double? centralParallel = null,
            double? standardParallels = null,
            double? range = null)
            => GetSeparationOfPointFromRadiusSquared(
                radius.Square(),
                x, y,
                resolution,
                centralMeridian,
                centralParallel,
                standardParallels,
                range);

        /// <summary>
        /// Calculates the approximate distance by which the given point is separated from its
        /// neighbors on an equirectangular projection with the given characteristics, by
        /// transforming the point and its nearest neighbors to latitude and longitude, and
        /// averaging the distances between them.
        /// </summary>
        /// <param name="planet">The mapped planet.</param>
        /// <param name="region">The mapped region.</param>
        /// <param name="x">The x coordinate of a point on an equirectangular projection, with zero
        /// as the westernmost point.</param>
        /// <param name="y">The y coordinate of a point on an equirectangular projection, with zero
        /// as the northernmost point.</param>
        /// <param name="resolution">The vertical resolution of the projection.</param>
        /// <returns>The area of the given point, in m².</returns>
        public static Number GetSeparationOfPoint(
            this Planetoid planet,
            SurfaceRegion region,
            int x, int y,
            int resolution)
            => GetSeparationOfPointFromRadiusSquared(
                planet.RadiusSquared,
                x,
                y,
                resolution,
                planet.VectorToLongitude(region.Position),
                planet.VectorToLatitude(region.Position),
                range: (double)((Frustum)region.Shape).FieldOfViewAngle);

        /// <summary>
        /// Gets the snowfall value for a <paramref name="position"/> in a <paramref name="region"/>
        /// at a given <paramref name="time"/> from a set of maps.
        /// </summary>
        /// <param name="planet">The mapped planet.</param>
        /// <param name="region">The mapped region.</param>
        /// <param name="position">A position relative to the center of <paramref
        /// name="region"/>.</param>
        /// <param name="maps">A set of precipitation maps.</param>
        /// <param name="time">The time at which the calculation is to be performed.</param>
        /// <returns></returns>
        public static double GetSnowfallFromLocalPosition(
            this Planetoid planet,
            SurfaceRegion region,
            Vector3 position,
            PrecipitationMaps[] maps,
            Duration time)
        {
            if (maps.Length == 0)
            {
                return 0;
            }

            var (x, y) = GetEquirectangularProjectionFromLocalPosition(
                planet,
                region,
                position,
                maps[0].PrecipitationMap.GetLength(0));
            var proportion = planet.GetProportionOfYearAtTime(time);
            return InterpolateAmongWeatherMaps(maps, proportion, map => map.SnowfallMap[x, y]);
        }

        /// <summary>
        /// <para>
        /// Produces a set of equirectangular projections of the specified region describing the
        /// surface and climate.
        /// </para>
        /// <para>
        /// This method is more efficient than calling <see cref="GetElevationMap(Planetoid, int,
        /// NeverFoundry.MathAndScience.Numerics.Number?, NeverFoundry.MathAndScience.Numerics.Number?,
        /// NeverFoundry.MathAndScience.Numerics.Number?,
        /// NeverFoundry.MathAndScience.Numerics.Number?)"/>, <see cref="GetWeatherMaps(Planetoid,
        /// int, NeverFoundry.MathAndScience.Numerics.Number?,
        /// NeverFoundry.MathAndScience.Numerics.Number?, NeverFoundry.MathAndScience.Numerics.Number?,
        /// NeverFoundry.MathAndScience.Numerics.Number?, int,
        /// NeverFoundry.MathAndScience.Numerics.Number[,],
        /// NeverFoundry.MathAndScience.Numerics.Number?)"/>, and <see
        /// cref="GetHydrologyMaps(Planetoid, int, NeverFoundry.MathAndScience.Numerics.Number?,
        /// NeverFoundry.MathAndScience.Numerics.Number?, NeverFoundry.MathAndScience.Numerics.Number?,
        /// NeverFoundry.MathAndScience.Numerics.Number?,
        /// NeverFoundry.MathAndScience.Numerics.Number[,], float[,],
        /// NeverFoundry.MathAndScience.Numerics.Number?)"/> separately.
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
            int steps = 12)
        {
            var elevationMap = GetElevationMap(planet, resolution, centralMeridian, centralParallel, standardParallels, range);
            var totalElevation = 0.0;
            var doubleResolution = resolution * 2;
            for (var x = 0; x < doubleResolution; x++)
            {
                for (var y = 0; y < resolution; y++)
                {
                    totalElevation += elevationMap[x, y];
                }
            }
            var averageElevation = totalElevation / (doubleResolution * resolution);

            var weatherMapSet = GetWeatherMaps(planet, resolution, centralMeridian, centralParallel, standardParallels, range, steps, elevationMap, averageElevation);
            var hydrologyMaps = GetHydrologyMaps(planet, resolution, centralMeridian, centralParallel, standardParallels, range, elevationMap, weatherMapSet.TotalPrecipitationMap, averageElevation);
            return new SurfaceMaps(doubleResolution, resolution, CastArray(elevationMap, resolution), (float)averageElevation, weatherMapSet, hydrologyMaps);
        }

        /// <summary>
        /// <para>
        /// Produces a set of equirectangular projections of the specified <paramref name="region"/>
        /// describing the surface and climate, taking into account any overlays.
        /// </para>
        /// <para>
        /// This method is more efficient than calling <see cref="GetElevationMap(Planetoid,
        /// SurfaceRegion, int)"/>, <see cref="GetWeatherMaps(Planetoid, SurfaceRegion, int, int,
        /// NeverFoundry.MathAndScience.Numerics.Number[,],
        /// NeverFoundry.MathAndScience.Numerics.Number?)"/>, and <see
        /// cref="GetHydrologyMaps(Planetoid, SurfaceRegion, int,
        /// NeverFoundry.MathAndScience.Numerics.Number[,], float[,],
        /// NeverFoundry.MathAndScience.Numerics.Number?)"/> separately.
        /// </para>
        /// </summary>
        /// <param name="planet">The planet being mapped.</param>
        /// <param name="region">The region being mapped.</param>
        /// <param name="resolution">The vertical resolution of the projection.</param>
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
            int steps = 12)
        {
            var elevationMap = planet.GetElevationMap(region, resolution);
            var totalElevation = 0.0;
            var doubleResolution = resolution * 2;
            for (var x = 0; x < doubleResolution; x++)
            {
                for (var y = 0; y < resolution; y++)
                {
                    totalElevation += elevationMap[x, y];
                }
            }
            var averageElevation = totalElevation / (doubleResolution * resolution);

            var weatherMapSet = planet.GetWeatherMaps(region, resolution, steps, elevationMap, averageElevation);
            var hydrologyMaps = planet.GetHydrologyMaps(region, resolution, elevationMap, weatherMapSet.TotalPrecipitationMap, averageElevation);
            return new SurfaceMaps(doubleResolution, resolution, CastArray(elevationMap, resolution), (float)averageElevation, weatherMapSet, hydrologyMaps);
        }

        /// <summary>
        /// Gets a specific temperature value from a temperature range.
        /// </summary>
        /// <param name="planet">The mapped planet.</param>
        /// <param name="range">The range being interpolated.</param>
        /// <param name="time">The time at which the calculation is to be performed.</param>
        /// <param name="latitude">The latitude at which the calculation is being performed (used to
        /// determine hemisphere, and thus season).</param>
        /// <returns>The specific temperature value from a temperature range.</returns>
        public static float GetTemperatureRangeValue(
            this Planetoid planet,
            FloatRange range,
            Duration time,
            double latitude)
            => range.Min.Lerp(range.Max, (float)planet.GetSeasonalProportionAtTime(time, latitude));

        /// <summary>
        /// Gets the temperature value for a <paramref name="position"/> in a <paramref
        /// name="region"/> at a given <paramref name="time"/> from a set of temperature ranges.
        /// </summary>
        /// <param name="planet">The mapped planet.</param>
        /// <param name="region">The mapped region.</param>
        /// <param name="position">A position relative to the center of <paramref
        /// name="region"/>.</param>
        /// <param name="temperatureRanges">A set of temperature ranges.</param>
        /// <param name="time">The time at which the calculation is to be performed.</param>
        /// <returns>The temperature value for a <paramref name="position"/> in a <paramref
        /// name="region"/> at a given <paramref name="time"/> from a set of temperature
        /// ranges.</returns>
        public static float GetTemperatureFromLocalPosition(
            this Planetoid planet,
            SurfaceRegion region,
            Vector3 position,
            FloatRange[,] temperatureRanges,
            Duration time)
        {
            var (x, y) = GetEquirectangularProjectionFromLocalPosition(
                planet,
                region,
                position,
                temperatureRanges.GetLength(0));
            return planet.GetTemperatureRangeValue(
                temperatureRanges[x, y],
                time,
                planet.VectorToLatitude(region.Position + position));
        }

        /// <summary>
        /// Gets the total precipitation value at a given <paramref name="time"/> from a set of
        /// maps.
        /// </summary>
        /// <param name="planet">The mapped planet.</param>
        /// <param name="maps">A set of precipitation maps.</param>
        /// <param name="time">The time at which the calculation is to be performed.</param>
        /// <returns></returns>
        public static FloatRange GetTotalPrecipitationAtTime(
            this Planetoid planet,
            PrecipitationMaps[] maps,
            Duration time)
        {
            if (maps.Length == 0)
            {
                return FloatRange.Zero;
            }

            var proportion = planet.GetProportionOfYearAtTime(time);
            return InterpolateAmongWeatherMaps(maps, proportion, map => map.Precipitation);
        }

        /// <summary>
        /// Gets the value for a <paramref name="position"/> in a <paramref name="region"/> from a
        /// set of values.
        /// </summary>
        /// <param name="planet">The mapped planet.</param>
        /// <param name="region">The mapped region.</param>
        /// <param name="position">A position relative to the center of <paramref
        /// name="region"/>.</param>
        /// <param name="values">A set of values.</param>
        /// <returns>The value for a <paramref name="position"/> in a <paramref name="region"/> from
        /// a set of values.</returns>
        public static T GetValueFromLocalPosition<T>(
            this Planetoid planet,
            SurfaceRegion region,
            Vector3 position,
            T[,] values)
        {
            var (x, y) = GetEquirectangularProjectionFromLocalPosition(
                planet,
                region,
                position,
                values.GetLength(0));
            return values[x, y];
        }

        /// <summary>
        /// <para>
        /// Produces a set of equirectangular projections of the specified region describing the
        /// climate.
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
            double[,]? elevationMap = null,
            double? averageElevation = null)
        {
            if (planet == null)
            {
                throw new ArgumentNullException(nameof(planet));
            }
            if (resolution > int.MaxValue / 2)
            {
                throw new ArgumentOutOfRangeException(nameof(resolution), $"The value of {nameof(resolution)} cannot exceed half of int.MaxValue ({(int.MaxValue / 2).ToString()}).");
            }
            if (steps > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(steps), $"The value of {nameof(steps)} cannot exceed int.MaxValue ({int.MaxValue.ToString()}).");
            }
            steps = Math.Max(1, steps);

            var doubleResolution = resolution * 2;
            var total = doubleResolution * resolution;

            if (elevationMap is null || elevationMap.GetLength(0) != doubleResolution || elevationMap.GetLength(1) != resolution)
            {
                elevationMap = planet.GetElevationMap(resolution, centralMeridian, centralParallel, standardParallels, range);
            }

            float[][,] precipitationMaps, snowfallMaps;
            PrecipitationMaps[] precipMaps;
            if (planet.HasAllWeatherMaps)
            {
                precipitationMaps = planet.GetPrecipitationMaps(doubleResolution, resolution);
                snowfallMaps = planet.GetSnowfallMaps(doubleResolution, resolution);
                precipMaps = new PrecipitationMaps[precipitationMaps.Length];
                for (var i = 0; i < precipitationMaps.Length; i++)
                {
                    precipMaps[i] = new PrecipitationMaps(doubleResolution, resolution, precipitationMaps[i], snowfallMaps[i]);
                }
                return new WeatherMaps(
                    planet,
                    doubleResolution,
                    resolution,
                    elevationMap,
                    precipMaps,
                    planet.GetTemperatureMap(doubleResolution, resolution),
                    resolution,
                    centralMeridian,
                    centralParallel,
                    standardParallels,
                    range,
                    averageElevation);
            }

            var temperatureRanges = planet.HasTemperatureMap
                ? planet.GetTemperatureMap(doubleResolution, resolution)
                : GetSurfaceMap(
                    (lat, _, x, y) =>
                    {
                        var winterLatitudes = new Dictionary<double, double>();
                        var summerLatitudes = new Dictionary<double, double>();
                        var latitudeTemperatures = new Dictionary<double, double>();
                        var elevationTemperatures = new Dictionary<(double, int), float>();

                        var roundedElevation = (int)Math.Round(Math.Max(0, elevationMap![x, y] * planet.MaxElevation) / 100) * 100;

                        if (!winterLatitudes.TryGetValue(lat, out var winterLat))
                        {
                            winterLat = Math.Abs(planet.GetSeasonalLatitudeFromDeclination(lat, -planet.AxialTilt));
                            winterLatitudes.Add(lat, winterLat);
                        }
                        if (!latitudeTemperatures.TryGetValue(winterLat, out var winterTemp))
                        {
                            winterTemp = planet.GetSurfaceTemperatureAtTrueAnomaly(planet.WinterSolsticeTrueAnomaly, winterLat);
                            latitudeTemperatures.Add(winterLat, winterTemp);
                        }
                        if (!elevationTemperatures.TryGetValue((winterTemp, roundedElevation), out var winterTempAtElevation))
                        {
                            winterTempAtElevation = (float)planet.GetTemperatureAtElevation(winterTemp, roundedElevation);
                            elevationTemperatures.Add((winterTemp, roundedElevation), winterTempAtElevation);
                        }

                        if (!summerLatitudes.TryGetValue(lat, out var summerLat))
                        {
                            summerLat = Math.Abs(planet.GetSeasonalLatitudeFromDeclination(lat, planet.AxialTilt));
                            summerLatitudes.Add(lat, summerLat);
                        }
                        if (!latitudeTemperatures.TryGetValue(summerLat, out var summerTemp))
                        {
                            summerTemp = planet.GetSurfaceTemperatureAtTrueAnomaly(planet.SummerSolsticeTrueAnomaly, summerLat);
                            latitudeTemperatures.Add(summerLat, summerTemp);
                        }
                        if (!elevationTemperatures.TryGetValue((summerTemp, roundedElevation), out var summerTempAtElevation))
                        {
                            summerTempAtElevation = (float)planet.GetTemperatureAtElevation(summerTemp, roundedElevation);
                            elevationTemperatures.Add((summerTemp, roundedElevation), summerTempAtElevation);
                        }

                        return new FloatRange(winterTempAtElevation, summerTempAtElevation);
                    },
                    resolution,
                    centralMeridian,
                    centralParallel,
                    standardParallels,
                    range);

            if (planet.HasPrecipitationMap && planet.HasSnowfallMap && planet.MappedSeasons >= steps)
            {
                precipitationMaps = planet.GetPrecipitationMaps(doubleResolution, resolution);
                snowfallMaps = planet.GetSnowfallMaps(doubleResolution, resolution);
                precipMaps = new PrecipitationMaps[precipitationMaps.Length];
                var step = planet.MappedSeasons == steps ? 1 : Math.Max(1, (int)Math.Floor(planet.MappedSeasons / (double)steps));
                for (var i = 0; i < precipitationMaps.Length; i += step)
                {
                    precipMaps[i] = new PrecipitationMaps(doubleResolution, resolution, precipitationMaps[i], snowfallMaps[i]);
                }
            }
            else
            {
                var proportionOfYear = 1f / steps;
                var proportionOfYearAtMidpoint = 0f;
                var proportionOfSummerAtMidpoint = 0f;
                var trueAnomaly = planet.WinterSolsticeTrueAnomaly;
                var trueAnomalyPerSeason = NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.TwoPI / steps;

                precipMaps = new PrecipitationMaps[steps];
                for (var i = 0; i < steps; i++)
                {
                    var solarDeclination = planet.GetSolarDeclination(trueAnomaly);

                    // Precipitation & snowfall
                    var snowfallMap = new float[doubleResolution, resolution];
                    var precipMap = GetSurfaceMap(
                        (lat, lon, x, y) =>
                        {
                            if (planet.Atmosphere.MaxPrecipitation.IsNearlyZero())
                            {
                                snowfallMap[x, y] = 0;
                                return 0;
                            }
                            else
                            {
                                var precipitation = planet.GetPrecipitation(
                                    planet.LatitudeAndLongitudeToDoubleVector(lat, lon),
                                    planet.GetSeasonalLatitudeFromDeclination(lat, solarDeclination),
                                    temperatureRanges[x, y].Min.Lerp(temperatureRanges[x, y].Max, proportionOfSummerAtMidpoint),
                                    proportionOfYear,
                                    out var snow);
                                snowfallMap[x, y] = (float)(snow / planet.Atmosphere.MaxSnowfall);
                                return (float)(precipitation / planet.Atmosphere.MaxPrecipitation);
                            }
                        },
                        resolution,
                        centralMeridian,
                        centralParallel,
                        standardParallels,
                        range);
                    precipMaps[i] = new PrecipitationMaps(doubleResolution, resolution, precipMap, snowfallMap);

                    proportionOfYearAtMidpoint += proportionOfYear;
                    proportionOfSummerAtMidpoint = 1 - (Math.Abs(0.5f - proportionOfYearAtMidpoint) / 0.5f);
                    trueAnomaly += trueAnomalyPerSeason;
                    if (trueAnomaly >= NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.TwoPI)
                    {
                        trueAnomaly -= NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.TwoPI;
                    }
                }
            }

            return new WeatherMaps(
                planet,
                doubleResolution,
                resolution,
                elevationMap,
                precipMaps,
                temperatureRanges,
                resolution,
                centralMeridian,
                centralParallel,
                standardParallels,
                range,
                averageElevation);
        }

        /// <summary>
        /// <para>
        /// Produces a set of equirectangular projections of the specified <paramref name="region"/>
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
            double[,]? elevationMap = null,
            double? averageElevation = null)
        {
            var doubleResolution = resolution * 2;
            var longitude = planet.VectorToLongitude(region.Position);
            var latitude = planet.VectorToLatitude(region.Position);
            var range = (double)((Frustum)region.Shape).FieldOfViewAngle;

            if (elevationMap == null || elevationMap.GetLength(0) != doubleResolution || elevationMap.GetLength(1) != resolution)
            {
                elevationMap = planet.GetElevationMap(region, resolution);
            }

            float[][,] precipitationMaps, snowfallMaps;
            PrecipitationMaps[] precipMaps;

            if (region.HasAllWeatherMaps)
            {
                precipitationMaps = region.GetPrecipitationMaps(doubleResolution, resolution);
                snowfallMaps = region.GetSnowfallMaps(doubleResolution, resolution);
                precipMaps = new PrecipitationMaps[precipitationMaps.Length];
                for (var i = 0; i < precipitationMaps.Length; i++)
                {
                    precipMaps[i] = new PrecipitationMaps(doubleResolution, resolution, precipitationMaps[i], snowfallMaps[i]);
                }
                return new WeatherMaps(
                    planet,
                    doubleResolution,
                    resolution,
                    elevationMap,
                    precipMaps,
                    region.GetTemperatureMap(doubleResolution, resolution),
                    resolution,
                    longitude,
                    latitude,
                    range: range,
                    averageElevation: averageElevation);
            }

            var weatherMaps = planet.GetWeatherMaps(
                resolution,
                longitude,
                latitude,
                range: range,
                steps: steps,
                elevationMap: elevationMap,
                averageElevation: averageElevation);
            if (!region.HasAnyWeatherMaps)
            {
                return weatherMaps;
            }

            precipitationMaps = region.HasPrecipitationMap
                ? region.GetPrecipitationMaps(doubleResolution, resolution)
                : weatherMaps.PrecipitationMaps.Select(x => x.PrecipitationMap).ToArray();
            snowfallMaps = region.HasSnowfallMap
                ? region.GetSnowfallMaps(doubleResolution, resolution)
                : weatherMaps.PrecipitationMaps.Select(x => x.SnowfallMap).ToArray();
            precipMaps = new PrecipitationMaps[precipitationMaps.Length];
            for (var i = 0; i < precipitationMaps.Length; i++)
            {
                precipMaps[i] = new PrecipitationMaps(doubleResolution, resolution, precipitationMaps[i], snowfallMaps[i]);
            }

            var tempRanges = region.HasTemperatureMap
                ? region.GetTemperatureMap(doubleResolution, resolution)
                : weatherMaps.TemperatureRangeMap;

            return new WeatherMaps(
                planet,
                doubleResolution,
                resolution,
                elevationMap,
                precipMaps,
                tempRanges,
                resolution,
                longitude,
                latitude,
                range: range,
                averageElevation: averageElevation);
        }

        internal static T[,] GetInitializedArray<T>(int xLength, int yLength, T defaultValue)
        {
            var arr = new T[xLength, yLength];
            for (var x = 0; x < xLength; x++)
            {
                for (var y = 0; y < yLength; y++)
                {
                    arr[x, y] = defaultValue;
                }
            }
            return arr;
        }

        internal static (int x, int y) GetEquirectangularProjectionFromLatLongWithScale(
            double latitude, double longitude,
            int resolution,
            double scale,
            double? centralMeridian = null,
            double? centralParallel = null,
            double? standardParallels = null)
        {
            var x = (int)Math.Round(((longitude - (centralParallel ?? 0)) / scale) + resolution)
                .Clamp(0, (resolution * 2) - 1);
            var y = (int)Math.Round(((latitude - (centralMeridian ?? 0)) * Math.Cos(standardParallels ?? centralParallel ?? 0) / scale) + (resolution / 2))
                .Clamp(0, resolution - 1);
            return (x, y);
        }

        internal static (int x, int y) GetEquirectangularProjectionFromLatLongWithScale(
            float latitude, float longitude,
            int resolution,
            double scale)
        {
            var x = (int)Math.Round((longitude / scale) + resolution).Clamp(0, (resolution * 2) - 1);
            var y = (int)Math.Round((latitude / scale) + (resolution / 2)).Clamp(0, resolution - 1);
            return (x, y);
        }

        internal static T[,] GetSurfaceMap<T>(
            Func<double, double, long, long, T> func,
            int resolution,
            double? centralMeridian = null,
            double? centralParallel = null,
            double? standardParallels = null,
            double? range = null)
        {
            if (resolution > 32767)
            {
                throw new ArgumentOutOfRangeException(nameof(resolution), $"The value of {nameof(resolution)} cannot exceed 32767.");
            }
            var map = new T[resolution * 2, resolution];
            var scale = range.HasValue && range.Value < MathConstants.PI && !range.Value.IsNearlyZero()
                ? NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.PISquared / (resolution * range.Value)
                : Math.PI / resolution;
            var halfResolution = resolution / 2;
            for (var x = -resolution; x < resolution; x++)
            {
                for (var y = -halfResolution; y < halfResolution; y++)
                {
                    var (latitude, longitude) = GetLatLonOfEquirectangularProjectionFromAdjustedCoordinates(x, y, scale, centralMeridian, centralParallel, standardParallels);
                    map[x + resolution, y + halfResolution] = func(latitude, longitude, x + resolution, y + halfResolution);
                }
            }
            return map;
        }

        private static float[,] CastArray(double[,] array, int resolution)
        {
            var doubleResolution = resolution * 2;
            var result = new float[doubleResolution, resolution];
            for (var x = 0; x < doubleResolution; x++)
            {
                for (var y = 0; y < resolution; y++)
                {
                    result[x, y] = (float)array[x, y];
                }
            }
            return result;
        }

        private static Number GetAreaOfPointFromRadiusSquared(
            Number radiusSquared,
            int x, int y,
            int resolution,
            double? centralMeridian = null,
            double? centralParallel = null,
            double? standardParallels = null,
            double? range = null)
        {
            var halfResolution = resolution / 2;
            var scale = range.HasValue && range.Value < MathConstants.PI && !range.Value.IsNearlyZero()
                ? NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.PISquared / (resolution * range.Value)
                : Math.PI / resolution;

            var centerX = (long)x - resolution;
            var centerY = (long)y - halfResolution;

            // left: x - 1, y
            var left = x == 0
                ? resolution - 1
                : x - 1;
            // up: x, y - 1
            var up = y == 0
                ? halfResolution - 1
                : y - 1;
            // right: x + 1, y
            var right = centerX == resolution - 1
                ? -resolution
                : x + 1;
            // down: x, y + 1
            var down = centerY == resolution - 1
                ? -halfResolution
                : y + 1;

            var (latCenter, lonCenter) = GetLatLonOfEquirectangularProjectionFromAdjustedCoordinates(x, y, scale, centralMeridian, centralParallel, standardParallels);
            var (_, lonLeft) = GetLatLonOfEquirectangularProjectionFromAdjustedCoordinates(left, y, scale, centralMeridian, centralParallel, standardParallels);
            var (_, lonRight) = GetLatLonOfEquirectangularProjectionFromAdjustedCoordinates(right, y, scale, centralMeridian, centralParallel, standardParallels);
            var (latUp, _) = GetLatLonOfEquirectangularProjectionFromAdjustedCoordinates(x, up, scale, centralMeridian, centralParallel, standardParallels);
            var (latDown, _) = GetLatLonOfEquirectangularProjectionFromAdjustedCoordinates(x, down, scale, centralMeridian, centralParallel, standardParallels);

            var lonLeftBorder = (lonLeft + lonCenter) / 2;
            var lonRightBorder = (lonRight + lonCenter) / 2;
            var latTopBorder = (latUp + latCenter) / 2;
            var latBottomBorder = (latDown + latCenter) / 2;

            return radiusSquared
                * (Math.Abs(Math.Sin(latBottomBorder) - Math.Sin(latTopBorder))
                * Math.Abs(lonRightBorder - lonLeftBorder));
        }

        private static (double latitude, double longitude) GetLatLonOfEquirectangularProjectionFromAdjustedCoordinates(
            long x, long y,
            double scale,
            double? centralMeridian,
            double? centralParallel,
            double? standardParallels = null)
            => ((y * scale) + (centralParallel ?? 0),
            (x * scale / Math.Cos(standardParallels ?? centralParallel ?? 0)) + (centralMeridian ?? 0));

        private static Number GetSeparationOfPointFromRadiusSquared(
            Number radiusSquared,
            int x, int y,
            int resolution,
            double? centralMeridian,
            double? centralParallel,
            double? standardParallels = null,
            double? range = null)
        {
            var halfResolution = resolution / 2;
            var scale = range.HasValue && range.Value < MathConstants.PI && !range.Value.IsNearlyZero()
                ? NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.PISquared / (resolution * range.Value)
                : Math.PI / resolution;

            var centerX = (long)x - resolution;
            var centerY = (long)y - halfResolution;

            // left: x - 1, y
            var left = x == 0
                ? resolution - 1
                : x - 1;
            // up: x, y - 1
            var up = y == 0
                ? halfResolution - 1
                : y - 1;
            // right: x + 1, y
            var right = centerX == resolution - 1
                ? -resolution
                : x + 1;
            // down: x, y + 1
            var down = centerY == resolution - 1
                ? -halfResolution
                : y + 1;

            var (latCenter, _) = GetLatLonOfEquirectangularProjectionFromAdjustedCoordinates(x, y, scale, centralMeridian, centralParallel, standardParallels);
            var (_, lonLeft) = GetLatLonOfEquirectangularProjectionFromAdjustedCoordinates(left, y, scale, centralMeridian, centralParallel, standardParallels);
            var (_, lonRight) = GetLatLonOfEquirectangularProjectionFromAdjustedCoordinates(right, y, scale, centralMeridian, centralParallel, standardParallels);
            var (latUp, _) = GetLatLonOfEquirectangularProjectionFromAdjustedCoordinates(x, up, scale, centralMeridian, centralParallel, standardParallels);
            var (latDown, _) = GetLatLonOfEquirectangularProjectionFromAdjustedCoordinates(x, down, scale, centralMeridian, centralParallel, standardParallels);

            var latTopBorder = (latUp + latCenter) / 2;
            var latBottomBorder = (latDown + latCenter) / 2;

            return radiusSquared
                * ((Math.Abs(Math.Sin(latBottomBorder) - Math.Sin(latTopBorder)) + Math.Abs(lonRight - lonLeft)) / 2);
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
