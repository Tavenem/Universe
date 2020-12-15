using NeverFoundry.MathAndScience;
using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.MathAndScience.Time;
using NeverFoundry.WorldFoundry.Place;
using NeverFoundry.WorldFoundry.Space;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Number = NeverFoundry.MathAndScience.Numerics.Number;

namespace NeverFoundry.WorldFoundry.SurfaceMapping
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
            Instant moment) => GetAnnualRangeValue(range, (float)planet.GetProportionOfYearAtTime(moment));

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
            Instant moment) => GetAnnualRangeIsPositiveAtTime(range, (float)planet.GetProportionOfYearAtTime(moment));

        /// <summary>
        /// Gets the value for a <paramref name="position"/> in a <paramref name="region"/> at a
        /// given proportion of the year from a set of ranges.
        /// </summary>
        /// <param name="region">The mapped region.</param>
        /// <param name="planet">The mapped planet.</param>
        /// <param name="position">A position relative to the center of <paramref
        /// name="region"/>.</param>
        /// <param name="ranges">A set of ranges.</param>
        /// <param name="proportionOfYear">
        /// The proportion of the year, starting and ending with midwinter, at which the calculation
        /// is to be performed.
        /// </param>
        /// <param name="equalArea">
        /// If <see langword="true"/> the projection will be a cylindrical equal-area projection.
        /// Otherwise, an equirectangular projection will be used.
        /// </param>
        /// <returns>The value for a <paramref name="position"/> in a <paramref name="region"/> at a
        /// given proportion of the year from a set of ranges.</returns>
        public static float GetAnnualValueFromLocalPosition(
            this SurfaceRegion region,
            Planetoid planet,
            Vector3 position,
            FloatRange[,] ranges,
            float proportionOfYear,
            bool equalArea = false)
        {
            var (x, y) = equalArea
                ? GetCylindricalEqualAreaProjectionFromLocalPosition(
                    region,
                    planet,
                    position,
                    ranges.GetLength(0))
                : GetEquirectangularProjectionFromLocalPosition(
                    region,
                    planet,
                    position,
                    ranges.GetLength(0));
            return GetAnnualRangeValue(
                ranges[x, y],
                proportionOfYear);
        }

        /// <summary>
        /// Gets the value for a <paramref name="position"/> in a <paramref name="region"/> at a
        /// given <paramref name="moment"/> from a set of ranges.
        /// </summary>
        /// <param name="region">The mapped region.</param>
        /// <param name="planet">The mapped planet.</param>
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
            this SurfaceRegion region,
            Planetoid planet,
            Vector3 position,
            FloatRange[,] ranges,
            Instant moment,
            bool equalArea = false)
        {
            var (x, y) = equalArea
                ? GetCylindricalEqualAreaProjectionFromLocalPosition(
                    region,
                    planet,
                    position,
                    ranges.GetLength(0))
                : GetEquirectangularProjectionFromLocalPosition(
                    region,
                    planet,
                    position,
                    ranges.GetLength(0));
            return planet.GetAnnualRangeValue(
                ranges[x, y],
                moment);
        }

        /// <summary>
        /// Determines whether the given proportion of the year falls within the range indicated for
        /// a <paramref name="position"/> in a <paramref name="region"/>.
        /// </summary>
        /// <param name="region">The mapped region.</param>
        /// <param name="planet">The mapped planet.</param>
        /// <param name="position">A position relative to the center of <paramref
        /// name="region"/>.</param>
        /// <param name="ranges">A set of ranges.</param>
        /// <param name="proportionOfYear">
        /// The proportion of the year, starting and ending with midwinter, at which the calculation
        /// is to be performed.
        /// </param>
        /// <param name="equalArea">
        /// If <see langword="true"/> the projection will be a cylindrical equal-area projection.
        /// Otherwise, an equirectangular projection will be used.
        /// </param>
        /// <returns><see langword="true"/> if the given proportion of the year falls within the
        /// range indicated for a <paramref name="position"/> in a <paramref name="region"/>;
        /// otherwise <see langword="false"/>.</returns>
        public static bool GetAnnualRangeIsPositiveAtTimeAndLocalPosition(
            this SurfaceRegion region,
            Planetoid planet,
            Vector3 position,
            FloatRange[,] ranges,
            float proportionOfYear,
            bool equalArea = false)
        {
            var (x, y) = equalArea
                ? GetCylindricalEqualAreaProjectionFromLocalPosition(
                    region,
                    planet,
                    position,
                    ranges.GetLength(0))
                : GetEquirectangularProjectionFromLocalPosition(
                    region,
                    planet,
                    position,
                    ranges.GetLength(0));
            return GetAnnualRangeIsPositiveAtTime(ranges[x, y], proportionOfYear);
        }

        /// <summary>
        /// Determines whether the given <paramref name="moment"/> falls within the range indicated
        /// for a <paramref name="position"/> in a <paramref name="region"/>.
        /// </summary>
        /// <param name="region">The mapped region.</param>
        /// <param name="planet">The mapped planet.</param>
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
            this SurfaceRegion region,
            Planetoid planet,
            Vector3 position,
            FloatRange[,] ranges,
            Instant moment,
            bool equalArea = false)
        {
            var (x, y) = equalArea
                ? GetCylindricalEqualAreaProjectionFromLocalPosition(
                    region,
                    planet,
                    position,
                    ranges.GetLength(0))
                : GetEquirectangularProjectionFromLocalPosition(
                    region,
                    planet,
                    position,
                    ranges.GetLength(0));
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
                (int)Math.Round(resolution * options.AspectRatio),
                resolution,
                options);

        /// <summary>
        /// Calculates the approximate area of a point on a map projection with the given
        /// characteristics, by transforming the point and its nearest neighbors to latitude and
        /// longitude, calculating the midpoints between them, and calculating the area of the
        /// region enclosed within those midpoints.
        /// </summary>
        /// <param name="region">The mapped region.</param>
        /// <param name="planet">The mapped planet.</param>
        /// <param name="x">The x coordinate of a point on a map projection, with zero as the
        /// westernmost point.</param>
        /// <param name="y">The y coordinate of a point on a map projection, with zero as the
        /// northernmost point.</param>
        /// <param name="resolution">The vertical resolution of the projection.</param>
        /// <param name="options">
        /// The map projection options used to generate the map used.
        /// </param>
        /// <returns>The area of the given point, in m².</returns>
        public static Number GetAreaOfLocalPoint(
            this SurfaceRegion region,
            Planetoid planet,
            int x, int y,
            int resolution,
            MapProjectionOptions options) => GetAreaOfPointFromRadiusSquared(
                planet.RadiusSquared,
                x, y,
                (int)Math.Round(resolution * options.AspectRatio),
                resolution,
                region.GetProjection(planet, options.EqualArea));

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
        /// Calculates the x and y coordinates on a cylindrical equal-area projection that
        /// correspond to a given <paramref name="position"/> relative to the center of the
        /// specified mapped <paramref name="region"/>, where 0,0 is at the top, left and is the
        /// northwestern-most point on the map.
        /// </summary>
        /// <param name="region">The region being mapped.</param>
        /// <param name="planet">The planet being mapped.</param>
        /// <param name="position">A position relative to the center of <paramref
        /// name="region"/>.</param>
        /// <param name="resolution">The vertical resolution of the projection.</param>
        /// <returns>
        /// The latitude and longitude of the given coordinates, in radians.
        /// </returns>
        public static (int x, int y) GetCylindricalEqualAreaProjectionFromLocalPosition(
            this SurfaceRegion region,
            Planetoid planet,
            Vector3 position,
            int resolution)
        {
            var pos = region.PlanetaryPosition + position;
            return GetCylindricalEqualAreaProjectionFromLatLong(
                planet.VectorToLatitude(pos),
                planet.VectorToLongitude(pos),
                resolution,
                region.GetProjection(planet, true));
        }

        /// <summary>
        /// Calculates the x and y coordinates on a cylindrical equal-area projection that
        /// correspond to a given latitude and longitude, where 0,0 is at the top, left and is the
        /// northwestern-most point on the map.
        /// </summary>
        /// <param name="region">The region being mapped.</param>
        /// <param name="planet">The planet being mapped.</param>
        /// <param name="latitude">The latitude to convert, in radians.</param>
        /// <param name="longitude">The longitude to convert.</param>
        /// <param name="resolution">The vertical resolution of the projection.</param>
        /// <returns>
        /// The latitude and longitude of the given coordinates, in radians.
        /// </returns>
        public static (int x, int y) GetCylindricalEqualAreaProjectionFromLocalPosition(
            this SurfaceRegion region,
            Planetoid planet,
            double latitude,
            double longitude,
            int resolution)
            => GetCylindricalEqualAreaProjectionFromLatLong(
                latitude,
                longitude,
                resolution,
                region.GetProjection(planet, true));

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
        /// Calculates the x and y coordinates on an equirectangular projection that correspond to a
        /// given <paramref name="position"/> relative to the center of the specified mapped
        /// <paramref name="region"/>, where 0,0 is at the top, left and is the northwestern-most
        /// point on the map.
        /// </summary>
        /// <param name="region">The region being mapped.</param>
        /// <param name="planet">The planet being mapped.</param>
        /// <param name="position">A position relative to the center of <paramref
        /// name="region"/>.</param>
        /// <param name="resolution">The vertical resolution of the projection.</param>
        /// <returns>
        /// The latitude and longitude of the given coordinates, in radians.
        /// </returns>
        public static (int x, int y) GetEquirectangularProjectionFromLocalPosition(
            this SurfaceRegion region,
            Planetoid planet,
            Vector3 position,
            int resolution)
        {
            var pos = region.PlanetaryPosition + position;
            return GetEquirectangularProjectionFromLatLong(
                planet.VectorToLatitude(pos),
                planet.VectorToLongitude(pos),
                resolution,
                region.GetProjection(planet));
        }

        /// <summary>
        /// Calculates the x and y coordinates on an equirectangular projection that correspond to a
        /// given latitude and longitude, where 0,0 is at the top, left and is the northwestern-most
        /// point on the map.
        /// </summary>
        /// <param name="region">The region being mapped.</param>
        /// <param name="planet">The planet being mapped.</param>
        /// <param name="latitude">The latitude to convert, in radians.</param>
        /// <param name="longitude">The longitude to convert.</param>
        /// <param name="resolution">The vertical resolution of the projection.</param>
        /// <returns>
        /// The latitude and longitude of the given coordinates, in radians.
        /// </returns>
        public static (int x, int y) GetEquirectangularProjectionFromLocalPosition(
            this SurfaceRegion region,
            Planetoid planet,
            double latitude,
            double longitude,
            int resolution)
            => GetEquirectangularProjectionFromLatLong(
                latitude,
                longitude,
                resolution,
                region.GetProjection(planet));

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
                  (int)Math.Round(resolution * projection.AspectRatio),
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
                (int)Math.Round(resolution * (options?.AspectRatio ?? Math.PI)),
                resolution,
                GetScale(resolution, options?.Range, true),
                options ?? MapProjectionOptions.Default)
            : GetLatLonOfEquirectangularProjectionFromAdjustedCoordinates(
                x, y,
                resolution * 2,
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
                resolution * 2,
                resolution,
                GetScale(resolution, options?.Range),
                options ?? MapProjectionOptions.Default);

        /// <summary>
        /// Calculates the latitude and longitude that correspond to a set of coordinates from a
        /// cylindrical equal-area projection.
        /// </summary>
        /// <param name="region">The region being mapped.</param>
        /// <param name="planet">The planet being mapped.</param>
        /// <param name="x">The x coordinate of a point on a cylindrical equal-area projection, with
        /// zero as the westernmost point.</param>
        /// <param name="y">The y coordinate of a point on a cylindrical equal-area projection, with
        /// zero as the northernmost point.</param>
        /// <param name="resolution">The vertical resolution of the projection.</param>
        /// <returns>
        /// The latitude and longitude of the given coordinates, in radians.
        /// </returns>
        public static (double latitude, double longitude) GetLatLonOfCylindricalEqualAreaProjectionFromLocalPosition(
            this SurfaceRegion region,
            Planetoid planet,
            int x, int y,
            int resolution)
            => GetLatLonOfCylindricalEqualAreaProjection(
                x, y,
                resolution,
                region.GetProjection(planet, true));

        /// <summary>
        /// Calculates the latitude and longitude that correspond to a set of coordinates from an
        /// equirectangular projection.
        /// </summary>
        /// <param name="region">The region being mapped.</param>
        /// <param name="planet">The planet being mapped.</param>
        /// <param name="x">The x coordinate of a point on an equirectangular projection, with zero
        /// as the westernmost point.</param>
        /// <param name="y">The y coordinate of a point on an equirectangular projection, with zero
        /// as the northernmost point.</param>
        /// <param name="resolution">The vertical resolution of the projection.</param>
        /// <returns>
        /// The latitude and longitude of the given coordinates, in radians.
        /// </returns>
        public static (double latitude, double longitude) GetLatLonOfEquirectangularProjectionFromLocalPosition(
            this SurfaceRegion region,
            Planetoid planet,
            int x, int y,
            int resolution)
            => GetLatLonOfEquirectangularProjection(
                x, y,
                resolution,
                region.GetProjection(planet, true));

        /// <summary>
        /// Calculates the position that corresponds to a set of coordinates from a cylindrical
        /// equal-area projection.
        /// </summary>
        /// <param name="region">The region being mapped.</param>
        /// <param name="planet">The planet being mapped.</param>
        /// <param name="x">The x coordinate of a point on a cylindrical equal-area projection, with
        /// zero as the westernmost point.</param>
        /// <param name="y">The y coordinate of a point on a cylindrical equal-area projection, with
        /// zero as the northernmost point.</param>
        /// <param name="resolution">The vertical resolution of the projection.</param>
        /// <returns>
        /// The local position of the given coordinates, in radians.
        /// </returns>
        public static Vector3 GetLocalPositionFromCylindricalEqualAreaProjection(
            this SurfaceRegion region,
            Planetoid planet,
            int x, int y,
            int resolution)
        {
            var (lat, lon) = GetLatLonOfCylindricalEqualAreaProjection(
                x, y,
                resolution,
                region.GetProjection(planet, true));
            return planet.LatitudeAndLongitudeToVector(lat, lon) - region.PlanetaryPosition;
        }

        /// <summary>
        /// Calculates the position that corresponds to a set of coordinates from an equirectangular
        /// projection.
        /// </summary>
        /// <param name="region">The region being mapped.</param>
        /// <param name="planet">The planet being mapped.</param>
        /// <param name="x">The x coordinate of a point on an equirectangular projection, with zero
        /// as the westernmost point.</param>
        /// <param name="y">The y coordinate of a point on an equirectangular projection, with zero
        /// as the northernmost point.</param>
        /// <param name="resolution">The vertical resolution of the projection.</param>
        /// <returns>
        /// The local position of the given coordinates, in radians.
        /// </returns>
        public static Vector3 GetLocalPositionFromEquirectangularProjection(
            this SurfaceRegion region,
            Planetoid planet,
            int x, int y,
            int resolution)
        {
            var (lat, lon) = GetLatLonOfEquirectangularProjection(
                x, y,
                resolution,
                region.GetProjection(planet, true));
            return planet.LatitudeAndLongitudeToVector(lat, lon) - region.PlanetaryPosition;
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

        /// <summary>
        /// Calculates the approximate distance by which the given point is separated from its
        /// neighbors on a map projection with the given characteristics, by transforming the point
        /// and its nearest neighbors to latitude and longitude, and averaging the distances between
        /// them.
        /// </summary>
        /// <param name="region">The mapped region.</param>
        /// <param name="planet">The mapped planet.</param>
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
            this SurfaceRegion region,
            Planetoid planet,
            int x, int y,
            int resolution,
            bool equalArea = false)
            => GetSeparationOfPointFromRadiusSquared(
                planet.RadiusSquared,
                x, y,
                resolution,
                region.GetProjection(planet, equalArea));

        /// <summary>
        /// Gets the value for a <paramref name="position"/> in a <paramref name="region"/> from a
        /// set of values.
        /// </summary>
        /// <param name="region">The mapped region.</param>
        /// <param name="planet">The mapped planet.</param>
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
            this SurfaceRegion region,
            Planetoid planet,
            Vector3 position,
            T[,] values,
            bool equalArea = false)
        {
            var (x, y) = equalArea
                ? GetCylindricalEqualAreaProjectionFromLocalPosition(
                    region,
                    planet,
                    position,
                    values.GetLength(0))
                : GetEquirectangularProjectionFromLocalPosition(
                    region,
                    planet,
                    position,
                    values.GetLength(0));
            return values[x, y];
        }

        /// <summary>
        /// <para>
        /// Produces a set of map projections of the specified region describing the climate.
        /// </para>
        /// </summary>
        /// <param name="planet">The planet being mapped.</param>
        /// <param name="resolution">The vertical resolution of the projection.</param>
        /// <param name="steps">
        /// The number of precipitation maps to generate (representing evenly spaced "seasons"
        /// during a year, starting and ending at the winter solstice in the northern hemisphere).
        /// </param>
        /// <param name="options">
        /// The map projection options used.
        /// </param>
        /// <param name="mapLoader">
        /// <para>
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used.
        /// </para>
        /// <para>
        /// If <see langword="null"/> no stored maps will be used, and any generated maps will not
        /// be saved.
        /// </para>
        /// </param>
        /// <returns>
        /// A <see cref="WeatherMaps"/> instance.
        /// </returns>
        public static async Task<WeatherMaps> GetWeatherMapsAsync(
            this Planetoid planet,
            int resolution,
            int steps = 12,
            MapProjectionOptions? options = null,
            ISurfaceMapLoader? mapLoader = null)
        {
            using var elevationMap = await planet
                .GetElevationMapProjectionAsync(resolution, options, mapLoader)
                .ConfigureAwait(false);
            using var winterTemperatureMap = await planet
                .GetTemperatureMapProjectionWinterAsync(resolution, options, mapLoader)
                .ConfigureAwait(false);
            using var summerTemperatureMap = await planet
                .GetTemperatureMapProjectionSummerAsync(resolution, options, mapLoader)
                .ConfigureAwait(false);
            using var precipitationMap = await planet
                .GetPrecipitationMapProjectionAsync(resolution, steps, options, mapLoader)
                .ConfigureAwait(false);
            return new WeatherMaps(
                planet,
                elevationMap,
                winterTemperatureMap,
                summerTemperatureMap,
                precipitationMap,
                resolution,
                options);
        }

        /// <summary>
        /// <para>
        /// Produces a set of map projections of the specified <paramref name="region"/> describing
        /// the climate, taking into account any overlays.
        /// </para>
        /// </summary>
        /// <param name="region">The region being mapped.</param>
        /// <param name="planet">The planet being mapped.</param>
        /// <param name="resolution">The vertical resolution of the projection.</param>
        /// <param name="steps">
        /// The number of precipitation maps to generate (representing evenly spaced "seasons"
        /// during a year, starting and ending at the winter solstice in the northern hemisphere).
        /// </param>
        /// <param name="equalArea">
        /// If <see langword="true"/> the projection will be a cylindrical equal-area projection.
        /// Otherwise, an equirectangular projection will be used.
        /// </param>
        /// <param name="mapLoader">
        /// <para>
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used.
        /// </para>
        /// <para>
        /// If <see langword="null"/> no stored maps will be used, and any generated maps will not
        /// be saved.
        /// </para>
        /// </param>
        /// <returns>
        /// A <see cref="WeatherMaps"/> instance.
        /// </returns>
        public static async Task<WeatherMaps> GetWeatherMapsAsync(
            this SurfaceRegion region,
            Planetoid planet,
            int resolution,
            int steps = 12,
            bool equalArea = false,
            ISurfaceMapLoader? mapLoader = null)
        {
            using var elevationMap = await region
                .GetElevationMapAsync(planet, resolution, equalArea, mapLoader)
                .ConfigureAwait(false);
            using var winterTemperatureMap = await region
                .GetTemperatureMapWinterAsync(planet, resolution, equalArea, mapLoader)
                .ConfigureAwait(false);
            using var summerTemperatureMap = await region
                .GetTemperatureMapWinterAsync(planet, resolution, equalArea, mapLoader)
                .ConfigureAwait(false);
            using var precipitationMap = await region
                .GetPrecipitationMapAsync(planet, resolution, steps, equalArea, mapLoader)
                .ConfigureAwait(false);
            return new WeatherMaps(
                planet,
                elevationMap,
                winterTemperatureMap,
                summerTemperatureMap,
                precipitationMap,
                resolution,
                region.GetProjection(planet, equalArea));
        }

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
                (latCenter, lonCenter) = GetLatLonOfEquirectangularProjectionFromAdjustedCoordinates(x, y, xResolution, yResolution, scale, options);
                (_, lonLeft) = GetLatLonOfEquirectangularProjectionFromAdjustedCoordinates(left, y, xResolution, yResolution, scale, options);
                (_, lonRight) = GetLatLonOfEquirectangularProjectionFromAdjustedCoordinates(right, y, xResolution, yResolution, scale, options);
                (latUp, _) = GetLatLonOfEquirectangularProjectionFromAdjustedCoordinates(x, up, xResolution, yResolution, scale, options);
                (latDown, _) = GetLatLonOfEquirectangularProjectionFromAdjustedCoordinates(x, down, xResolution, yResolution, scale, options);
            }

            var lonLeftBorder = (lonLeft + lonCenter) / 2;
            var lonRightBorder = (lonRight + lonCenter) / 2;
            var latTopBorder = (latUp + latCenter) / 2;
            var latBottomBorder = (latDown + latCenter) / 2;

            return radiusSquared
                * (Math.Abs(Math.Sin(latBottomBorder) - Math.Sin(latTopBorder))
                * Math.Abs(lonRightBorder - lonLeftBorder));
        }

        internal static T[][] GetCylindricalEqualAreaProjectionFromEquirectangularProjection<T>(T[][] map, MapProjectionOptions options)
        {
            if (map.Length == 0)
            {
                return Array.Empty<T[]>();
            }

            var resolution = map[0].GetLength(0);
            var halfResolution = resolution / 2;
            var doubleResolution = resolution * 2;

            var equirectangularScale = GetScale(resolution, options.Range);

            var centerMeridian = options.CentralMeridian;
            var centerParallel = options.CentralParallel;
            var scaleFactor = options.ScaleFactor;
            var aspectRatio = options.AspectRatio;
            var equalAreaXResolution = (int)Math.Round(resolution * aspectRatio);
            var halfEqualAreaXResolution = equalAreaXResolution / 2;
            var stretchByScale = scaleFactor / equirectangularScale;
            var scaleByPi = options.Range < Math.PI && !options.Range.Value.IsNearlyZero()
                ? options.Range.Value / resolution
                : 2.0 / resolution;
            var scaleByAspect = options.Range < Math.PI && !options.Range.Value.IsNearlyZero()
                ? options.Range.Value / (resolution * options.ScaleFactorSquared)
                : 2 / (resolution * options.ScaleFactorSquared);

            var newMap = new T[equalAreaXResolution][];
            for (var equalAreaX = 0; equalAreaX < equalAreaXResolution; equalAreaX++)
            {
                newMap[equalAreaX] = new T[resolution];
                for (var equalAreaY = 0; equalAreaY < resolution; equalAreaY++)
                {
                    var latitude = Math.Asin(((equalAreaY - halfResolution) * scaleByPi).Clamp(-1, 1)) + centerParallel;
                    var longitude = ((equalAreaX - halfEqualAreaXResolution) * scaleByAspect) + centerMeridian;

                    var eqirectangularX = (int)Math.Round(((longitude - centerMeridian) * stretchByScale) + resolution)
                        .Clamp(0, doubleResolution - 1);
                    var eqirectangularY = (int)Math.Round(halfResolution + ((latitude - centerParallel) / equirectangularScale))
                        .Clamp(0, resolution - 1);

                    newMap[equalAreaX][equalAreaY] = map[eqirectangularX][eqirectangularY];
                }
            }
            return newMap;
        }

        internal static (int x, int y) GetCylindricalEqualAreaProjectionFromLatLongWithScale(
            double latitude, double longitude,
            int yResolution,
            double scale,
            MapProjectionOptions options)
            => GetCylindricalEqualAreaProjectionFromLatLongWithScale(
                latitude, longitude,
                (int)Math.Round(yResolution * options.AspectRatio),
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
            (int)Math.Round((yResolution / 2) + (Math.Sin(latitude - options.CentralParallel) * Math.PI / scale))
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
            => (int)Math.Round((yResolution / 2) + (Math.Sin(latitude - options.CentralParallel) * Math.PI / scale))
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
            => ((int)Math.Round(((longitude - options.CentralMeridian) * options.ScaleFactor / scale) + xResolution)
            .Clamp(0, xResolution - 1)
            ,(int)Math.Round(((latitude - options.CentralParallel) / scale) + (yResolution / 2))
            .Clamp(0, yResolution - 1));

        internal static int GetEquirectangularXFromLonWithScale(
            double longitude,
            int xResolution,
            double scale,
            MapProjectionOptions options)
            => (int)Math.Round(((longitude - options.CentralMeridian) * options.ScaleFactor / scale) + xResolution)
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
            => Math.Asin(((y - (yResolution / 2)) * scale / Math.PI).Clamp(-1, 1)) + options.CentralParallel;

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
            => ((x - xResolution) * stretch) + options.CentralMeridian;

        internal static double GetScale(int resolution, double? range = null, bool equalArea = false)
        {
            if (equalArea)
            {
                return range < Math.PI && !range.Value.IsNearlyZero()
                    ? Math.PI * range.Value / resolution
                    : MathAndScience.Constants.Doubles.MathConstants.TwoPI / resolution;
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
            var xResolution = (int)Math.Round(resolution * options.AspectRatio);

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
                (latCenter, _) = GetLatLonOfEquirectangularProjectionFromAdjustedCoordinates(x, y, xResolution, resolution, scale, options);
                (_, lonLeft) = GetLatLonOfEquirectangularProjectionFromAdjustedCoordinates(left, y, xResolution, resolution, scale, options);
                (_, lonRight) = GetLatLonOfEquirectangularProjectionFromAdjustedCoordinates(right, y, xResolution, resolution, scale, options);
                (latUp, _) = GetLatLonOfEquirectangularProjectionFromAdjustedCoordinates(x, up, xResolution, resolution, scale, options);
                (latDown, _) = GetLatLonOfEquirectangularProjectionFromAdjustedCoordinates(x, down, xResolution, resolution, scale, options);
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
            => (Math.Asin(((y - (yResolution / 2)) * scale / Math.PI).Clamp(-1, 1)) + options.CentralParallel,
            ((x - (xResolution / 2)) * scale / options.AspectRatio) + options.CentralMeridian);

        private static (double latitude, double longitude) GetLatLonOfEquirectangularProjectionFromAdjustedCoordinates(
            long x, long y,
            int xResolution,
            int yResolution,
            double scale,
            MapProjectionOptions options)
            => (((y - (yResolution / 2)) * scale) + options.CentralParallel,
            ((x - xResolution) * scale / options.ScaleFactor) + options.CentralMeridian);
    }
}
