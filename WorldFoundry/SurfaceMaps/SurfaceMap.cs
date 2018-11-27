﻿using ExtensionLib;
using MathAndScience;
using MathAndScience.Numerics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using UniversalTime;
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
        public static double GetAnnualRangeValue(
            this Planetoid planet,
            FloatRange range,
            Duration time)
            => MathUtility.Lerp(range.Min, range.Max, planet.GetProportionOfYearAtTime(time));

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
            var proportionOfYear = planet.GetProportionOfYearAtTime(time);
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
        /// <returns></returns>
        public static double GetAnnualValueFromLocalPosition(
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
        /// <returns></returns>
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
        public static double GetAreaOfPoint(
            double radius,
            int x, int y,
            int resolution,
            double centralMeridian = 0,
            double centralParallel = 0,
            double? standardParallels = null,
            double? range = null)
            => GetAreaOfPointFromRadiusSquared(
                radius * radius,
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
        public static double GetAreaOfLocalPoint(
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
                range: region.Shape.ContainingRadius / planet.RadiusSquared);

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
        public static float[,] GetElevationMap(
            this Planetoid planet,
            int resolution,
            double centralMeridian = 0,
            double centralParallel = 0,
            double? standardParallels = null,
            double? range = null)
            => GetSurfaceMap(
                (lat, lon, _, __) => planet.GetNormalizedElevationAt(planet.LatitudeAndLongitudeToVector(lat, lon)),
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
        public static float[,] GetElevationMap(
            this Planetoid planet,
            SurfaceRegion region,
            int resolution)
        {
            var elevationMap = planet.GetElevationMap(
                resolution,
                planet.VectorToLongitude(region.Position),
                planet.VectorToLatitude(region.Position),
                range: region.Shape.ContainingRadius / planet.RadiusSquared);
            if (region._elevationOverlay != null)
            {
                using (var image = Image.LoadPixelData<Rgba32>(region._elevationOverlay, region._elevationOverlayWidth, region._elevationOverlayHeight))
                {
                    return SurfaceMapImage.GetCompositeSurfaceMap(elevationMap, image.ImageToSurfaceMap(resolution * 2, resolution, false), false);
                }
            }
            else
            {
                return elevationMap;
            }
        }

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
            double latitude, double longitude,
            int resolution,
            double centralMeridian = 0,
            double centralParallel = 0,
            double? standardParallels = null,
            double? range = null)
            => GetEquirectangularProjectionFromLatLongWithScale(
                latitude, longitude,
                resolution,
                range.HasValue && range.Value < Math.PI && !range.Value.IsZero()
                    ? MathConstants.PISquared / (resolution * range.Value)
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
                range: region.Shape.ContainingRadius / planet.RadiusSquared);
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
            double centralMeridian = 0,
            double centralParallel = 0,
            double? standardParallels = null,
            double? range = null)
            => GetLatLonOfEquirectangularProjectionFromAdjustedCoordinates(
                (long)x - resolution,
                (long)y - (resolution / 2),
                range.HasValue && range.Value < Math.PI && !range.Value.IsZero()
                    ? MathConstants.PISquared / (resolution * range.Value)
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
                range: region.Shape.ContainingRadius / planet.RadiusSquared);

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
                range: region.Shape.ContainingRadius / planet.RadiusSquared);
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
            double centralMeridian = 0,
            double centralParallel = 0,
            double? standardParallels = null,
            double? range = null,
            float[,] elevationMap = null,
            float[,] precipitationMap = null)
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

            if (elevationMap == null || elevationMap.GetLength(0) != doubleResolution || elevationMap.GetLength(1) != resolution)
            {
                elevationMap = planet.GetElevationMap(resolution, centralMeridian, centralParallel, standardParallels, range);
            }
            if (precipitationMap == null || precipitationMap.GetLength(0) != doubleResolution || precipitationMap.GetLength(1) != resolution)
            {
                precipitationMap = GetWeatherMaps(planet, resolution, centralMeridian, centralParallel, standardParallels, range, 1, elevationMap).TotalPrecipitation;
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
                lakeDepths.Add((xL, yL), lowX == -1 ? 0 : elevationMap[lowX, lowY] - elevationMap[xL, yL]);
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
                    if (!precipitationMap[x, y].IsZero())
                    {
                        if (areaMap[x, y] < 0)
                        {
                            areaMap[x, y] = GetAreaOfPointFromRadiusSquared(planet.RadiusSquared, x, y, resolution, centralMeridian, centralParallel, standardParallels, range);
                        }
                        var runoff = (float)(precipitationMap[x, y] * planet.Atmosphere.MaxPrecipitation * 0.001 * areaMap[x, y] / (planet.Orbit?.Period ?? 31557600));
                        if (drainage[x, y] != (x, y))
                        {
                            flows.Add((drainage[x, y].x, drainage[x, y].y, runoff));
                        }
                    }
                }
            }

            var maxFlow = 0.0;
            var flowMap = new float[doubleResolution, resolution];
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
            var depthMap = new float[doubleResolution, resolution];
            foreach (var ((xL, yL), points) in lakes)
            {
                if (flowMap[xL, yL].IsZero())
                {
                    continue;
                }
                depthMap[xL, yL] = lakeDepths[(xL, yL)];
                foreach (var (x, y) in points)
                {
                    depthMap[xL, yL] = lakeDepths[(xL, yL)];
                }
            }

            return new HydrologyMaps(depthMap, flowMap, maxFlow);
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
            float[,] elevationMap = null,
            float[,] precipitationMap = null)
        {
            var doubleResolution = resolution * 2;
            if (elevationMap == null || elevationMap.GetLength(0) != doubleResolution || elevationMap.GetLength(1) != resolution)
            {
                elevationMap = planet.GetElevationMap(region, resolution);
            }
            if (precipitationMap == null || precipitationMap.GetLength(0) != doubleResolution || precipitationMap.GetLength(1) != resolution)
            {
                precipitationMap = planet.GetWeatherMaps(region, resolution, 1, elevationMap).TotalPrecipitation;
            }
            var hydrologyMaps = planet.GetHydrologyMaps(
                  resolution,
                  planet.VectorToLongitude(region.Position),
                  planet.VectorToLatitude(region.Position),
                  range: region.Shape.ContainingRadius / planet.RadiusSquared,
                  elevationMap: elevationMap,
                  precipitationMap: precipitationMap);
            if (region._depthOverlay == null && region._flowOverlay == null)
            {
                return hydrologyMaps;
            }
            var depth = hydrologyMaps.Depth;
            var flow = hydrologyMaps.Flow;
            if (region._depthOverlay != null)
            {
                using (var image = Image.LoadPixelData<Rgba32>(region._depthOverlay, region._depthOverlayWidth, region._depthOverlayHeight))
                {
                    depth = SurfaceMapImage.GetCompositeSurfaceMap(depth, image.ImageToSurfaceMap(doubleResolution, resolution, false));
                }
            }
            if (region._flowOverlay != null)
            {
                using (var image = Image.LoadPixelData<Rgba32>(region._flowOverlay, region._flowOverlayWidth, region._flowOverlayHeight))
                {
                    flow = SurfaceMapImage.GetCompositeSurfaceMap(flow, image.ImageToSurfaceMap(doubleResolution, resolution, false));
                }
            }
            return new HydrologyMaps(depth, flow, hydrologyMaps.MaxFlow);
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
            if ((maps?.Length ?? 0) == 0)
            {
                return 0;
            }

            var (x, y) = GetEquirectangularProjectionFromLocalPosition(
                planet,
                region,
                position,
                maps[0].Precipitation.GetLength(0));
            var proportion = planet.GetProportionOfYearAtTime(time);
            return InterpolateAmongWeatherMaps(maps, proportion, map => map.Precipitation[x, y]);
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
        public static double GetSeparationOfPoint(
            double radius,
            int x, int y,
            int resolution,
            double centralMeridian = 0,
            double centralParallel = 0,
            double? standardParallels = null,
            double? range = null)
            => GetSeparationOfPointFromRadiusSquared(
                radius * radius,
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
        public static double GetSeparationOfPoint(
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
                range: region.Shape.ContainingRadius / planet.RadiusSquared);

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
            if ((maps?.Length ?? 0) == 0)
            {
                return 0;
            }

            var (x, y) = GetEquirectangularProjectionFromLocalPosition(
                planet,
                region,
                position,
                maps[0].Precipitation.GetLength(0));
            var proportion = planet.GetProportionOfYearAtTime(time);
            return InterpolateAmongWeatherMaps(maps, proportion, map => map.Snowfall[x, y]);
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
        public static double GetTemperatureRangeValue(
            this Planetoid planet,
            FloatRange range,
            Duration time,
            double latitude)
            => MathUtility.Lerp(range.Min, range.Max, planet.GetSeasonalProportionAtTime(time, latitude));

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
        public static double GetTemperatureFromLocalPosition(
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
        /// <para>
        /// Produces a set of equirectangular projections of the specified region describing the
        /// surface and climate.
        /// </para>
        /// <para>
        /// This method is more efficient than calling <see cref="GetElevationMap(Planetoid, int,
        /// double, double, double?, double?)"/>, <see cref="GetWeatherMaps(Planetoid, int, double,
        /// double, double?, double?, int, float[,])"/>, and <see cref="GetHydrologyMaps(Planetoid,
        /// int, double, double, double?, double?, float[,], float[,])"/> separately.
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
            double centralMeridian = 0,
            double centralParallel = 0,
            double? standardParallels = null,
            double? range = null,
            int steps = 12)
        {
            var elevationMap = GetElevationMap(planet, resolution, centralMeridian, centralParallel, standardParallels, range);
            var weatherMapSet = GetWeatherMaps(planet, resolution, centralMeridian, centralParallel, standardParallels, range, steps, elevationMap);
            var hydrologyMaps = GetHydrologyMaps(planet, resolution, centralMeridian, centralParallel, standardParallels, range, elevationMap, weatherMapSet.TotalPrecipitation);
            return new SurfaceMaps(elevationMap, weatherMapSet, hydrologyMaps);
        }

        /// <summary>
        /// <para>
        /// Produces a set of equirectangular projections of the specified <paramref name="region"/>
        /// describing the surface and climate, taking into account any overlays.
        /// </para>
        /// <para>
        /// This method is more efficient than calling <see cref="GetElevationMap(Planetoid,
        /// SurfaceRegion, int)"/>, <see cref="GetWeatherMaps(Planetoid, SurfaceRegion, int, int,
        /// float[,])"/>, and <see cref="GetHydrologyMaps(Planetoid, SurfaceRegion, int, float[,],
        /// float[,])"/> separately.
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
            var weatherMapSet = planet.GetWeatherMaps(region, resolution, steps, elevationMap);
            var hydrologyMaps = planet.GetHydrologyMaps(region, resolution, elevationMap, weatherMapSet.TotalPrecipitation);
            return new SurfaceMaps(elevationMap, weatherMapSet, hydrologyMaps);
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
            double centralMeridian = 0,
            double centralParallel = 0,
            double? standardParallels = null,
            double? range = null,
            int steps = 12,
            float[,] elevationMap = null)
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

            if (elevationMap == null || elevationMap.GetLength(0) != doubleResolution || elevationMap.GetLength(1) != resolution)
            {
                elevationMap = planet.GetElevationMap(resolution, centralMeridian, centralParallel, standardParallels, range);
            }

            var temperatureRanges = GetSurfaceMap(
                (lat, _, x, y) =>
                {
                    var winterLatitudes = new Dictionary<double, double>();
                    var summerLatitudes = new Dictionary<double, double>();
                    var latitudeTemperatures = new Dictionary<double, double>();
                    var elevationTemperatures = new Dictionary<(double, double), double>();

                    var roundedElevation = Math.Round(Math.Max(0, elevationMap[x, y] * planet.MaxElevation) / 100) * 100;

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
                        winterTempAtElevation = planet.GetTemperatureAtElevation(winterTemp, roundedElevation);
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
                        summerTempAtElevation = planet.GetTemperatureAtElevation(summerTemp, roundedElevation);
                        elevationTemperatures.Add((summerTemp, roundedElevation), summerTempAtElevation);
                    }

                    return new FloatRange((float)winterTempAtElevation, (float)summerTempAtElevation);
                },
                resolution,
                centralMeridian,
                centralParallel,
                standardParallels,
                range);

            var proportionOfYear = 1.0 / steps;
            var proportionOfYearAtMidpoint = 0.0;
            var proportionOfSummerAtMidpoint = 0.0;
            var trueAnomaly = planet.WinterSolsticeTrueAnomaly;
            var trueAnomalyPerSeason = MathConstants.TwoPI / steps;

            var precipitationMaps = new PrecipitationMaps[steps];
            for (var i = 0; i < steps; i++)
            {
                var solarDeclination = planet.GetSolarDeclination(trueAnomaly);

                // Precipitation & snowfall
                var snowfallMap = new float[doubleResolution, resolution];
                var precipMap = GetSurfaceMap(
                    (lat, lon, x, y) =>
                    {
                        var precipitation = planet.GetPrecipitation(
                            planet.LatitudeAndLongitudeToVector(lat, lon),
                            planet.GetSeasonalLatitudeFromDeclination(lat, solarDeclination),
                            MathUtility.Lerp(temperatureRanges[x, y].Min, temperatureRanges[x, y].Max, proportionOfSummerAtMidpoint),
                            proportionOfYear,
                            out var snow);
                        snowfallMap[x, y] = (float)(snow / planet.Atmosphere.MaxSnowfall);
                        return (float)(precipitation / planet.Atmosphere.MaxPrecipitation);
                    },
                    resolution,
                    centralMeridian,
                    centralParallel,
                    standardParallels,
                    range);
                precipitationMaps[i] = new PrecipitationMaps(precipMap, snowfallMap);

                proportionOfYearAtMidpoint += proportionOfYear;
                proportionOfSummerAtMidpoint = 1 - (Math.Abs(0.5 - proportionOfYearAtMidpoint) / 0.5);
                trueAnomaly += trueAnomalyPerSeason;
                if (trueAnomaly >= MathConstants.TwoPI)
                {
                    trueAnomaly -= MathConstants.TwoPI;
                }
            }

            return new WeatherMaps(
                planet,
                elevationMap,
                precipitationMaps,
                temperatureRanges,
                resolution,
                centralMeridian,
                centralParallel,
                standardParallels,
                range);
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
            float[,] elevationMap = null)
        {
            var doubleResolution = resolution * 2;
            var longitude = planet.VectorToLongitude(region.Position);
            var latitude = planet.VectorToLatitude(region.Position);
            if (elevationMap == null || elevationMap.GetLength(0) != doubleResolution || elevationMap.GetLength(1) != resolution)
            {
                elevationMap = planet.GetElevationMap(region, resolution);
            }
            var weatherMaps = planet.GetWeatherMaps(
                resolution,
                longitude,
                latitude,
                range: region.Shape.ContainingRadius / planet.RadiusSquared,
                steps: steps,
                elevationMap: elevationMap);
            if (region._temperatureOverlaySummer == null && region._temperatureOverlayWinter == null
                && region._precipitationOverlays == null && region._snowfallOverlays == null)
            {
                return weatherMaps;
            }

            var tempRanges = weatherMaps.TemperatureRanges;
            if (region._temperatureOverlaySummer != null || region._temperatureOverlayWinter != null)
            {
                var summer = region._temperatureOverlaySummer ?? region._temperatureOverlayWinter;
                var winter = region._temperatureOverlayWinter ?? region._temperatureOverlaySummer;
                var height = region._temperatureOverlaySummer == null ? region._temperatureOverlayHeightWinter : region._temperatureOverlayHeightSummer;
                var width = region._temperatureOverlaySummer == null ? region._temperatureOverlayWidthWinter : region._temperatureOverlayWidthSummer;
                using (var imageSummer = Image.LoadPixelData<Rgba32>(summer, width, height))
                using (var imageWinter = Image.LoadPixelData<Rgba32>(winter, width, height))
                {
                    tempRanges = SurfaceMapImage.GetCompositeSurfaceMap(
                        tempRanges,
                        imageWinter.ImageToSurfaceMap(doubleResolution, resolution, false),
                        imageSummer.ImageToSurfaceMap(doubleResolution, resolution, false));
                }
            }

            var precipitationMaps = weatherMaps.PrecipitationMaps.Select(x => x.Precipitation).ToArray();
            var snowfallMaps = weatherMaps.PrecipitationMaps.Select(x => x.Snowfall).ToArray();
            if (region._precipitationOverlays != null)
            {
                var overlayRatio = (double)region._precipitationOverlays.Length / precipitationMaps.Length;
                for (var i = 0; i < precipitationMaps.Length; i++)
                {
                    var nearestOverlay = region._precipitationOverlays[(int)Math.Floor(i * overlayRatio)];
                    using (var image = Image.LoadPixelData<Rgba32>(nearestOverlay, region._precipitationOverlayWidth, region._precipitationOverlayHeight))
                    {
                        precipitationMaps[i] = SurfaceMapImage.GetCompositeSurfaceMap(
                            precipitationMaps[i],
                            image.ImageToSurfaceMap(doubleResolution, resolution, false));
                    }
                }
            }
            if (region._snowfallOverlays != null)
            {
                var overlayRatio = (double)region._snowfallOverlays.Length / snowfallMaps.Length;
                for (var i = 0; i < snowfallMaps.Length; i++)
                {
                    var nearestOverlay = region._snowfallOverlays[(int)Math.Floor(i * overlayRatio)];
                    using (var image = Image.LoadPixelData<Rgba32>(nearestOverlay, region._snowfallOverlayWidth, region._snowfallOverlayHeight))
                    {
                        snowfallMaps[i] = SurfaceMapImage.GetCompositeSurfaceMap(
                            snowfallMaps[i],
                            image.ImageToSurfaceMap(doubleResolution, resolution, false));
                    }
                }
            }
            var precipMaps = new PrecipitationMaps[precipitationMaps.Length];
            for (var i = 0; i < precipitationMaps.Length; i++)
            {
                precipMaps[i] = new PrecipitationMaps(precipitationMaps[i], snowfallMaps[i]);
            }

            return new WeatherMaps(
                planet,
                elevationMap,
                precipMaps,
                tempRanges,
                resolution,
                longitude,
                latitude,
                range: region.Shape.ContainingRadius / planet.RadiusSquared);
        }

        internal static (int x, int y) GetEquirectangularProjectionFromLatLongWithScale(
            double latitude, double longitude,
            int resolution,
            double scale,
            double centralMeridian = 0,
            double centralParallel = 0,
            double? standardParallels = null)
        {
            var x = (((longitude - centralParallel) / scale) + resolution).RoundToInt();
            if (x < 0)
            {
                x = 0;
            }
            if (x >= resolution * 2)
            {
                x = (resolution * 2) - 1;
            }
            var y = (((latitude - centralMeridian) * Math.Cos(standardParallels ?? centralParallel) / scale) + (resolution / 2)).RoundToInt();
            if (y < 0)
            {
                y = 0;
            }
            if (y >= resolution)
            {
                y = resolution - 1;
            }
            return (x, y);
        }

        internal static T[,] GetSurfaceMap<T>(
            Func<double, double, long, long, T> func,
            int resolution,
            double centralMeridian = 0,
            double centralParallel = 0,
            double? standardParallels = null,
            double? range = null)
        {
            if (resolution > 32767)
            {
                throw new ArgumentOutOfRangeException(nameof(resolution), $"The value of {nameof(resolution)} cannot exceed 32767.");
            }
            var map = new T[resolution * 2, resolution];
            var scale = range.HasValue && range.Value < Math.PI && !range.Value.IsZero()
                ? MathConstants.PISquared / (resolution * range.Value)
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

        private static double GetAreaOfPointFromRadiusSquared(
            double radiusSquared,
            int x, int y,
            int resolution,
            double centralMeridian = 0,
            double centralParallel = 0,
            double? standardParallels = null,
            double? range = null)
        {
            var halfResolution = resolution / 2;
            var scale = range.HasValue && range.Value < Math.PI && !range.Value.IsZero()
                ? MathConstants.PISquared / (resolution * range.Value)
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
            var down = y == resolution - 1
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
                * Math.Abs(Math.Sin(latBottomBorder) - Math.Sin(latTopBorder))
                * Math.Abs(lonRightBorder - lonLeftBorder);
        }

        private static (double latitude, double longitude) GetLatLonOfEquirectangularProjectionFromAdjustedCoordinates(
            long x, long y,
            double scale,
            double centralMeridian = 0,
            double centralParallel = 0,
            double? standardParallels = null)
            => ((y * scale) + centralParallel,
            (x * scale / Math.Cos(standardParallels ?? centralParallel)) + centralMeridian);

        private static double GetSeparationOfPointFromRadiusSquared(
            double radiusSquared,
            int x, int y,
            int resolution,
            double centralMeridian = 0,
            double centralParallel = 0,
            double? standardParallels = null,
            double? range = null)
        {
            var halfResolution = resolution / 2;
            var scale = range.HasValue && range.Value < Math.PI && !range.Value.IsZero()
                ? MathConstants.PISquared / (resolution * range.Value)
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
            var down = y == resolution - 1
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
                * ((Math.Abs(Math.Sin(latBottomBorder) - Math.Sin(latTopBorder)) + Math.Abs(lonRight - lonLeft)) / 2);
        }

        private static float InterpolateAmongWeatherMaps(PrecipitationMaps[] maps, double proportionOfYear, Func<PrecipitationMaps, float> getValueFromMap)
        {
            if ((maps?.Length ?? 0) == 0)
            {
                return 0;
            }
            var proportionPerSeason = 1.0 / maps.Length;
            var seasonIndex = (int)Math.Floor(proportionOfYear / proportionPerSeason);
            var nextSeasonIndex = seasonIndex == maps.Length - 1 ? 0 : seasonIndex + 1;
            var weight = (proportionOfYear - (seasonIndex * proportionPerSeason)) / proportionPerSeason;
            return (float)MathUtility.Lerp(getValueFromMap.Invoke(maps[seasonIndex]), getValueFromMap.Invoke(maps[nextSeasonIndex]), weight);
        }
    }
}
