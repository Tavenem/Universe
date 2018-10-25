using MathAndScience;
using System;

namespace WorldFoundry.SurfaceMaps
{
    public static class SurfaceMap
    {
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
        public static (double latitude, double longitude) GetEquirectangularProjection(
            uint x, uint y,
            uint resolution,
            double centralMeridian = 0,
            double centralParallel = 0,
            double? standardParallels = null,
            double? range = null)
            => GetEquirectangularProjectionFromAdjustedCoordinates(
                (long)x - resolution,
                (long)y - (resolution / 2),
                range.HasValue && range.Value < Math.PI && !range.Value.IsZero()
                    ? MathConstants.PISquared / (resolution * range.Value)
                    : Math.PI / resolution,
                centralMeridian,
                centralParallel,
                standardParallels);

        internal static (double latitude, double longitude) GetEquirectangularProjectionFromAdjustedCoordinates(
            long x, long y,
            double scale,
            double centralMeridian = 0,
            double centralParallel = 0,
            double? standardParallels = null)
            => ((y * scale) + centralParallel,
            (x * scale / Math.Cos(standardParallels ?? centralParallel)) + centralMeridian);

        internal static double[,] GetSurfaceMap(
            Func<double, double, double> func,
            uint resolution,
            double centralMeridian = 0,
            double centralParallel = 0,
            double? standardParallels = null,
            double? range = null)
        {
            if (resolution > int.MaxValue / 2)
            {
                throw new ArgumentOutOfRangeException(nameof(resolution), $"The value of {nameof(resolution)} cannot exceed {(int.MaxValue / 2).ToString()}.");
            }
            var map = new double[resolution * 2, resolution];
            var scale = range.HasValue && range.Value < Math.PI && !range.Value.IsZero()
                ? MathConstants.PISquared / (resolution * range.Value)
                : Math.PI / resolution;
            var halfResolution = resolution / 2;
            for (var x = -resolution; x < resolution; x++)
            {
                for (var y = -halfResolution; y < halfResolution; y++)
                {
                    var (latitude, longitude) = GetEquirectangularProjectionFromAdjustedCoordinates(x, y, scale, centralMeridian, centralParallel, standardParallels);
                    map[x + resolution, y + halfResolution] = func(latitude, longitude);
                }
            }
            return map;
        }
    }
}
