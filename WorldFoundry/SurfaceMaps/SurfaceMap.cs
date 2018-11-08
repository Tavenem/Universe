using ExtensionLib;
using MathAndScience;
using System;
using System.Collections.Generic;
using System.Linq;
using WorldFoundry.CelestialBodies.Planetoids;
using WorldFoundry.CelestialBodies.Planetoids.Planets.TerrestrialPlanets;

namespace WorldFoundry.SurfaceMaps
{
    /// <summary>
    /// Static methods to assist with producing equirectangular projections that map the surface of
    /// a planet.
    /// </summary>
    public static class SurfaceMap
    {
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
            => GetAreaOfPointFromRadiusSuared(
                radius * radius,
                x, y,
                resolution,
                centralMeridian,
                centralParallel,
                standardParallels,
                range);

        /// <summary>
        /// Produces an equirectangular projection of an elevation map of the specified region.
        /// </summary>
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
        /// A two-dimensional array of <see cref="double"/> values corresponding to points on an
        /// equirectangular projected map of the surface. The first index corresponds to the X
        /// coordinate, and the second index corresponds to the Y coordinate. The values are
        /// normalized elevations from -1 to 1, where negative values are below sea level and
        /// positive values are above sea level, and 1 is equal to the maximum elevation of this
        /// <see cref="Planetoid"/>. <seealso cref="MaxElevation"/>
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
        /// Produces a set of equirectangular projections of the specified region describing the
        /// hydrology.
        /// </summary>
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
            this TerrestrialPlanet planet,
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
                precipitationMap = GetWeatherMaps(planet, resolution, centralMeridian, centralParallel, standardParallels, range, 0, 1, elevationMap).WeatherMaps[0].Precipitation;
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
                            areaMap[x, y] = GetAreaOfPointFromRadiusSuared(planet.RadiusSquared, x, y, resolution, centralMeridian, centralParallel, standardParallels, range);
                        }
                        var runoff = (float)(precipitationMap[x, y] * planet.Atmosphere.MaxPrecipitation * 0.001 * areaMap[x, y] / (planet.Orbit?.Period ?? 31557600));
                        if (drainage[x, y] != (x, y))
                        {
                            flows.Add((drainage[x, y].x, drainage[x, y].y, runoff));
                        }
                    }
                }
            }
            var flowMap = new float[doubleResolution, resolution];
            while (flows.Count > 0)
            {
                var newFlows = new List<(int, int, float)>();
                foreach (var (x, y, flow) in flows)
                {
                    flowMap[x, y] += flow;
                    if (drainage[x, y] != (x, y))
                    {
                        newFlows.Add((drainage[x, y].x, drainage[x, y].y, flow));
                    }
                }
                flows = newFlows;
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

            return new HydrologyMaps(depthMap, flowMap);
        }

        /// <summary>
        /// <para>
        /// Produces a set of equirectangular projections of the specified region describing the
        /// climate.
        /// </para>
        /// <para>
        /// Calling <see cref="GetWeatherMapSet(TerrestrialPlanet, int, double, double, double?,
        /// double?, int, float[,])"/> is more efficient, and produces more accurate results, than
        /// calling this method repeatedly. This method should be used only if it will be used just
        /// once, for a period of less than one year.
        /// </para>
        /// </summary>
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
        /// <param name="start">The true anomaly of the beginning of the period for which to
        /// generate maps, offset from the winter solstice. Zero (the default) indicates the winter
        /// solstice. Values below zero, or greater than or equal to 1, are treated as zero.</param>
        /// <param name="end">The true anomaly of the end of the period for which to generate maps,
        /// offset from the winter solstice. One (the default) indicates the winter solstice of the
        /// following year (i.e. one full year, if <paramref name="start"/> was zero). Values less
        /// than or equal to zero, or greater than one, are treated as one.</param>
        /// <param name="elevationMap">The elevation map for this region. If left <see
        /// langword="null"/> one will be generated. A pre-generated map <i>must</i> share the same
        /// projection parameters (<paramref name="resolution"/>, <paramref
        /// name="centralMeridian"/>, etc.). If it does not, a new one will be generated
        /// anyway.</param>
        /// <returns>A <see cref="WeatherMapSet"/> instance with one <see cref="WeatherMaps"/>
        /// instance.</returns>
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
        public static WeatherMapSet GetWeatherMaps(
            this TerrestrialPlanet planet,
            int resolution,
            double centralMeridian = 0,
            double centralParallel = 0,
            double? standardParallels = null,
            double? range = null,
            double start = 0,
            double end = 1,
            float[,] elevationMap = null)
        {
            if (planet == null)
            {
                throw new ArgumentNullException(nameof(planet));
            }
            if (resolution > 32767)
            {
                throw new ArgumentOutOfRangeException(nameof(resolution), $"The value of {nameof(resolution)} cannot exceed 32767.");
            }
            var doubleResolution = resolution * 2;
            var total = doubleResolution * resolution;

            var positionMap = GetSurfaceMap(
                (lat, lon, _, __) => planet.LatitudeAndLongitudeToVector(lat, lon),
                resolution,
                centralMeridian,
                centralParallel,
                standardParallels,
                range);

            if (elevationMap == null || elevationMap.GetLength(0) != doubleResolution || elevationMap.GetLength(1) != resolution)
            {
                elevationMap = planet.GetElevationMap(resolution, centralMeridian, centralParallel, standardParallels, range);
            }

            var proportionOfYear = Math.Abs(end - start);
            var proportionOfYearAtMidpoint = start + (proportionOfYear / 2);
            if (proportionOfYearAtMidpoint >= 1)
            {
                proportionOfYearAtMidpoint--;
            }
            var trueAnomaly = planet.WinterSolsticeTrueAnomaly + (proportionOfYearAtMidpoint * MathConstants.TwoPI);
            if (trueAnomaly >= MathConstants.TwoPI)
            {
                trueAnomaly -= MathConstants.TwoPI;
            }

            // Temperature
            var tempRangeMap = GetSurfaceMap(
                (lat, lon, _, __) => planet.GetSurfaceTemperatureRangeAt(lat, lon),
                resolution,
                centralMeridian,
                centralParallel,
                standardParallels,
                range);
            var tempMap = GetSurfaceMap(
                (_, __, x, y) => (float)(MathUtility.Lerp(tempRangeMap[x, y].Min, tempRangeMap[x, y].Max, proportionOfYearAtMidpoint) / planet.MaxSurfaceTemperature),
                resolution,
                centralMeridian,
                centralParallel,
                standardParallels,
                range);

            var min = 2f;
            var max = -2f;
            var sum = 0f;
            for (var x = 0; x < doubleResolution; x++)
            {
                for (var y = 0; y < resolution; y++)
                {
                    min = Math.Min(min, tempMap[x, y]);
                    max = Math.Max(max, tempMap[x, y]);
                    sum += tempMap[x, y];
                }
            }
            var tempRange = new FloatRange(min, sum / total, max);

            // Precipitation & snowfall
            var solarEquator = planet.GetSolarEquator(trueAnomaly);
            var snowfallMap = new float[doubleResolution, resolution];
            var precipMap = GetSurfaceMap(
                (lat, __, x, y) =>
                {
                    var precipitation = planet.GetPrecipitation(
                        positionMap[x, y],
                        planet.GetSeasonalLatitudeFromEquator(lat, solarEquator),
                        tempMap[x, y] * planet.MaxSurfaceTemperature,
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

            min = 2f;
            max = -2f;
            sum = 0f;
            for (var x = 0; x < doubleResolution; x++)
            {
                for (var y = 0; y < resolution; y++)
                {
                    min = Math.Min(min, precipMap[x, y]);
                    max = Math.Max(max, precipMap[x, y]);
                    sum += precipMap[x, y];
                }
            }
            var precipRange = new FloatRange(min, sum / total, max);

            min = 2f;
            max = -2f;
            sum = 0f;
            for (var x = 0; x < doubleResolution; x++)
            {
                for (var y = 0; y < resolution; y++)
                {
                    min = Math.Min(min, snowfallMap[x, y]);
                    max = Math.Max(max, snowfallMap[x, y]);
                    sum += precipMap[x, y];
                }
            }
            var snowfallRange = new FloatRange(min, sum / total, max);

            // Snow Cover
            var snowCoverRangeMap = GetSurfaceMap(
                (lat, _, x, y) => planet.GetSnowCoverRange(positionMap[x, y], lat, elevationMap[x, y] * planet.MaxElevation),
                resolution,
                centralMeridian,
                centralParallel,
                standardParallels,
                range);
            var snowCoverMap = GetSurfaceMap(
                (_, __, x, y) => snowfallMap[x, y] > 0 || planet.GetSnowCover(proportionOfYearAtMidpoint, snowCoverRangeMap[x, y]),
                resolution,
                centralMeridian,
                centralParallel,
                standardParallels,
                range);

            // Sea Ice
            var seaIceRangeMap = GetSurfaceMap(
                (lat, _, x, y) => planet.GetSeaIceRange(lat, elevationMap[x, y] * planet.MaxElevation),
                resolution,
                centralMeridian,
                centralParallel,
                standardParallels,
                range);
            var seaIceMap = GetSurfaceMap(
                (_, __, x, y) => planet.GetSeaIce(proportionOfYearAtMidpoint, snowCoverRangeMap[x, y]),
                resolution,
                centralMeridian,
                centralParallel,
                standardParallels,
                range);

            return new WeatherMapSet(
                planet,
                seaIceRangeMap,
                snowCoverRangeMap,
                tempRangeMap,
                tempRange,
                new WeatherMaps[]
                {
                    new WeatherMaps(
                        precipMap,
                        precipRange,
                        seaIceMap,
                        snowCoverMap,
                        snowfallMap,
                        snowfallRange,
                        tempMap,
                        tempRange)
                });
        }

        /// <summary>
        /// <para>
        /// Produces a set of equirectangular projections of the specified region describing the
        /// climate.
        /// </para>
        /// <para>
        /// This method is more efficient, and produces more accurate results, than calling
        /// <see cref="GetWeatherMaps(TerrestrialPlanet, int, double, double, double?, double?,
        /// double, double, float[,])"/> repeatedly.
        /// </para>
        /// </summary>
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
        /// at the cost of increased processing time. If zero is passed, the return value will be
        /// empty.</param>
        /// <param name="elevationMap">The elevation map for this region. If left <see
        /// langword="null"/> one will be generated. A pre-generated map <i>must</i> share the same
        /// projection parameters (<paramref name="resolution"/>, <paramref
        /// name="centralMeridian"/>, etc.). If it does not, a new one will be generated
        /// anyway.</param>
        /// <returns>A <see cref="WeatherMapSet"/> instances with an array of <see
        /// cref="WeatherMaps"/> instances equal to the number of <paramref name="steps"/>
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
        public static WeatherMapSet GetWeatherMapSet(
            this TerrestrialPlanet planet,
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
            if (steps <= 0)
            {
                return new WeatherMapSet();
            }

            var doubleResolution = resolution * 2;
            var halfResolution = resolution / 2;
            var total = doubleResolution * resolution;

            if (elevationMap == null || elevationMap.GetLength(0) != doubleResolution || elevationMap.GetLength(1) != resolution)
            {
                elevationMap = planet.GetElevationMap(resolution, centralMeridian, centralParallel, standardParallels, range);
            }

            var proportionOfYear = 1.0 / steps;
            var proportionOfYearAtMidpoint = 0.0;
            var trueAnomaly = planet.WinterSolsticeTrueAnomaly;
            var trueAnomalyPerSeason = MathConstants.TwoPI / steps;

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
                        winterLat = Math.Abs(planet.GetSeasonalLatitudeFromEquator(lat, planet.WinterSolarEquator));
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
                        summerLat = Math.Abs(planet.GetSeasonalLatitudeFromEquator(lat, planet.SummerSolarEquator));
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

            var minTemp = 2f;
            var maxTemp = -2f;
            for (var x = 0; x < doubleResolution; x++)
            {
                for (var y = 0; y < resolution; y++)
                {
                    minTemp = Math.Min(minTemp, temperatureRanges[x, y].Min);
                    maxTemp = Math.Max(maxTemp, temperatureRanges[x, y].Max);
                }
            }

            var tempMaps = new float[steps][,];
            var tempRanges = new FloatRange[steps];
            var precipMaps = new float[steps][,];
            var precipRanges = new FloatRange[steps];
            var snowfallMaps = new float[steps][,];
            var snowfallRanges = new FloatRange[steps];
            var min = 2f;
            var max = -2f;
            var sum = 0f;
            for (var i = 0; i < steps; i++)
            {
                var solarEquator = planet.GetSolarEquator(trueAnomaly);

                // Temperature
                tempMaps[i] = GetSurfaceMap(
                    (_, __, x, y) => (float)(MathUtility.Lerp(temperatureRanges[x, y].Min, temperatureRanges[x, y].Max, proportionOfYearAtMidpoint) / planet.MaxSurfaceTemperature),
                    resolution,
                    centralMeridian,
                    centralParallel,
                    standardParallels,
                    range);

                for (var x = 0; x < doubleResolution; x++)
                {
                    for (var y = 0; y < resolution; y++)
                    {
                        min = Math.Min(min, tempMaps[i][x, y]);
                        max = Math.Max(max, tempMaps[i][x, y]);
                        sum += tempMaps[i][x, y];
                    }
                }
                tempRanges[i] = new FloatRange(min, sum / total, max);

                // Precipitation & snowfall
                snowfallMaps[i] = new float[doubleResolution, resolution];
                precipMaps[i] = GetSurfaceMap(
                    (lat, lon, x, y) =>
                    {
                        var precipitation = planet.GetPrecipitation(
                            planet.LatitudeAndLongitudeToVector(lat, lon),
                            planet.GetSeasonalLatitudeFromEquator(lat, solarEquator),
                            tempMaps[i][x, y] * planet.MaxSurfaceTemperature,
                            proportionOfYear,
                            out var snow);
                        snowfallMaps[i][x, y] = (float)(snow / planet.Atmosphere.MaxSnowfall);
                        return (float)(precipitation / planet.Atmosphere.MaxPrecipitation);
                    },
                    resolution,
                    centralMeridian,
                    centralParallel,
                    standardParallels,
                    range);

                for (var x = 0; x < doubleResolution; x++)
                {
                    for (var y = 0; y < resolution; y++)
                    {
                        min = Math.Min(min, precipMaps[i][x, y]);
                        max = Math.Max(max, precipMaps[i][x, y]);
                        sum += precipMaps[i][x, y];
                    }
                }
                precipRanges[i] = new FloatRange(min, sum / total, max);

                for (var x = 0; x < doubleResolution; x++)
                {
                    for (var y = 0; y < resolution; y++)
                    {
                        min = Math.Min(min, snowfallMaps[i][x, y]);
                        max = Math.Max(max, snowfallMaps[i][x, y]);
                        sum += snowfallMaps[i][x, y];
                    }
                }
                snowfallRanges[i] = new FloatRange(min, sum / total, max);

                proportionOfYearAtMidpoint += proportionOfYear;
                trueAnomaly += trueAnomalyPerSeason;
                if (trueAnomaly >= MathConstants.TwoPI)
                {
                    trueAnomaly -= MathConstants.TwoPI;
                }
            }

            var tempRange = new FloatRange(minTemp, tempRanges.Average(x => x.Average), maxTemp);

            var snowCoverRangeMap = GetSurfaceMap(
                (lat, _, x, y) => planet.GetSnowCoverRange(
                    temperatureRanges[x, y],
                    lat,
                    elevationMap[x, y] * planet.MaxElevation,
                    precipMaps.Sum(c => c[x, y]) * planet.Atmosphere.MaxPrecipitation),
                resolution,
                centralMeridian,
                centralParallel,
                standardParallels,
                range);
            var seaIceRangeMap = GetSurfaceMap(
                (lat, _, x, y) => planet.GetSeaIceRange(temperatureRanges[x, y], lat, elevationMap[x, y] * planet.MaxElevation),
                resolution,
                centralMeridian,
                centralParallel,
                standardParallels,
                range);

            proportionOfYearAtMidpoint = 0.0;
            trueAnomaly = planet.WinterSolsticeTrueAnomaly;
            var snowCoverMaps = new bool[steps][,];
            var seaIceMaps = new bool[steps][,];
            for (var i = 0; i < steps; i++)
            {
                // Snow Cover
                snowCoverMaps[i] = GetSurfaceMap(
                    (_, __, x, y) => snowfallMaps[i][x, y] > 0 || planet.GetSnowCover(proportionOfYearAtMidpoint, snowCoverRangeMap[x, y]),
                    resolution,
                    centralMeridian,
                    centralParallel,
                    standardParallels,
                    range);

                // Sea Ice
                seaIceMaps[i] = GetSurfaceMap(
                    (_, __, x, y) => planet.GetSeaIce(proportionOfYearAtMidpoint, snowCoverRangeMap[x, y]),
                    resolution,
                    centralMeridian,
                    centralParallel,
                    standardParallels,
                    range);

                proportionOfYearAtMidpoint += proportionOfYear;
                trueAnomaly += trueAnomalyPerSeason;
                if (trueAnomaly >= MathConstants.TwoPI)
                {
                    trueAnomaly -= MathConstants.TwoPI;
                }
            }

            var maps = new WeatherMaps[steps];
            for (var i = 0; i < steps; i++)
            {
                maps[i] = new WeatherMaps(precipMaps[i], precipRanges[i], seaIceMaps[i], snowCoverMaps[i], snowfallMaps[i], snowfallRanges[i], tempMaps[i], tempRanges[i]);
            }
            return new WeatherMapSet(planet, seaIceRangeMap, snowCoverRangeMap, temperatureRanges, tempRange, maps);
        }

        /// <summary>
        /// <para>
        /// Produces a set of equirectangular projections of the specified region describing the
        /// surface and climate.
        /// </para>
        /// <para>
        /// This method is more efficient than calling <see cref="GetElevationMap(Planetoid, int,
        /// double, double, double?, double?)"/>, 
        /// <see cref="GetWeatherMapSet(TerrestrialPlanet, int, double, double, double?, double?,
        /// int, float[,])"/>, and <see cref="GetHydrologyMaps(TerrestrialPlanet, int, double,
        /// double, double?, double?, float[,], float[,])"/> separately.
        /// </para>
        /// </summary>
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
        /// <returns>A <see cref="TerrestrialSurfaceMapSet"/> instance.</returns>
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
        public static TerrestrialSurfaceMapSet GetSurfaceMapSet(
            this TerrestrialPlanet planet,
            int resolution,
            double centralMeridian = 0,
            double centralParallel = 0,
            double? standardParallels = null,
            double? range = null,
            int steps = 12)
        {
            var elevationMap = GetElevationMap(planet, resolution, centralMeridian, centralParallel, standardParallels, range);
            var weatherMapSet = GetWeatherMapSet(planet, resolution, centralMeridian, centralParallel, standardParallels, range, steps, elevationMap);
            var hydrologyMaps = GetHydrologyMaps(planet, resolution, centralMeridian, centralParallel, standardParallels, range, elevationMap, weatherMapSet.TotalPrecipitation);
            return new TerrestrialSurfaceMapSet(elevationMap, weatherMapSet, hydrologyMaps);
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

        private static double GetAreaOfPointFromRadiusSuared(
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
                * Math.Abs(lonRight - lonLeft);
        }

        private static (double latitude, double longitude) GetLatLonOfEquirectangularProjectionFromAdjustedCoordinates(
            long x, long y,
            double scale,
            double centralMeridian = 0,
            double centralParallel = 0,
            double? standardParallels = null)
            => ((y * scale) + centralParallel,
            (x * scale / Math.Cos(standardParallels ?? centralParallel)) + centralMeridian);

        private static T[,] GetSurfaceMap<T>(
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
    }
}
