using NeverFoundry.MathAndScience;
using NeverFoundry.MathAndScience.Constants.Doubles;
using System;

namespace NeverFoundry.WorldFoundry.Planet.SurfaceMapping
{
    /// <summary>
    /// Options for projecting a map.
    /// </summary>
    public class MapProjectionOptions
    {
        /// <summary>
        /// An equirectangular projection of the entire globe, with the standard parallel at the
        /// equator.
        /// </summary>
        public static readonly MapProjectionOptions Default = new ();

        /// <summary>
        /// <para>
        /// The aspect ratio of the map.
        /// </para>
        /// <para>
        /// Always 2 for an equirectangular projection.
        /// </para>
        /// <para>
        /// Equal to ScaleFactor²π for a cylindrical equal-area projection.
        /// </para>
        /// </summary>
        public double AspectRatio { get; }

        /// <summary>
        /// <para>
        /// The longitude of the central meridian of the projection, in radians.
        /// </para>
        /// <para>
        /// Values are truncated to the range -π..π.
        /// </para>
        /// </summary>
        public double CentralMeridian { get; }

        /// <summary>
        /// <para>
        /// The latitude of the central parallel of the projection, in radians.
        /// </para>
        /// <para>
        /// Values are truncated to the range -π/2..π/2.
        /// </para>
        /// </summary>
        public double CentralParallel { get; }

        /// <summary>
        /// Indicates whether the projection is to be cylindrical equal-area (rather than
        /// equirectangular).
        /// </summary>
        public bool EqualArea { get; }

        /// <summary>
        /// <para>
        /// If provided, indicates the latitude range (north and south of the central parallel)
        /// shown on the projection, in radians.
        /// </para>
        /// <para>
        /// If left <see langword="null"/>, or equal to zero, the full globe is projected.
        /// </para>
        /// <para>
        /// Values are truncated to the range 0..π.
        /// </para>
        /// </summary>
        public double? Range { get; }

        /// <summary>
        /// The cosine of the standard parallel.
        /// </summary>
        public double ScaleFactor { get; }

        /// <summary>
        /// The cosine of the standard parallel, squared.
        /// </summary>
        public double ScaleFactorSquared { get; }

        /// <summary>
        /// <para>
        /// The latitude of the standard parallels (north and south of the equator) where the scale
        /// of the projection is 1:1, in radians.
        /// </para>
        /// <para>
        /// It does not matter whether the positive or negative latitude is provided, if it is
        /// non-zero.
        /// </para>
        /// <para>
        /// If left <see langword="null"/> the central parallel is assumed.
        /// </para>
        /// <para>
        /// Values are truncated to the range -π/2..π/2.
        /// </para>
        /// </summary>
        public double? StandardParallels { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="MapProjectionOptions"/>.
        /// </summary>
        public MapProjectionOptions()
        {
            ScaleFactor = 1;
            ScaleFactorSquared = 1;
            AspectRatio = 2;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="MapProjectionOptions"/>.
        /// </summary>
        /// <param name="centralMeridian">
        /// <para>
        /// The longitude of the central meridian of the projection, in radians.
        /// </para>
        /// <para>
        /// Values are truncated to the range -π..π.
        /// </para>
        /// </param>
        /// <param name="centralParallel">
        /// <para>
        /// The latitude of the central parallel of the projection, in radians.
        /// </para>
        /// <para>
        /// Values are truncated to the range -π/2..π/2.
        /// </para>
        /// </param>
        /// <param name="standardParallels">
        /// <para>
        /// The latitude of the standard parallels (north and south of the equator) where the scale
        /// of the projection is 1:1, in radians.
        /// </para>
        /// <para>
        /// It does not matter whether the positive or negative latitude is provided, if it is
        /// non-zero.
        /// </para>
        /// <para>
        /// If left <see langword="null"/> the central parallel is assumed.
        /// </para>
        /// <para>
        /// Values are truncated to the range -π/2..π/2.
        /// </para>
        /// </param>
        /// <param name="range">
        /// <para>
        /// If provided, indicates the latitude range (north and south of the central parallel)
        /// shown on the projection, in radians.
        /// </para>
        /// <para>
        /// If left <see langword="null"/>, or equal to zero, the full globe is projected.
        /// </para>
        /// <para>
        /// Values are truncated to the range 0..π.
        /// </para>
        /// </param>
        /// <param name="equalArea">
        /// Indicates whether the projection is to be cylindrical equal-area (rather than
        /// equirectangular).
        /// </param>
        public MapProjectionOptions(
            double centralMeridian = 0,
            double centralParallel = 0,
            double? standardParallels = null,
            double? range = null,
            bool equalArea = false)
        {
            CentralMeridian = centralMeridian.Clamp(-Math.PI, Math.PI);
            CentralParallel = centralParallel.Clamp(-MathConstants.HalfPI, MathConstants.HalfPI);
            EqualArea = equalArea;
            Range = range.HasValue
                ? range.Value.Clamp(0, Math.PI)
                : null;
            StandardParallels = standardParallels.HasValue
                ? standardParallels.Value.Clamp(-MathConstants.HalfPI, MathConstants.HalfPI)
                : null;
            ScaleFactor = Math.Cos(StandardParallels ?? CentralParallel);
            ScaleFactorSquared = ScaleFactor * ScaleFactor;
            AspectRatio = equalArea
                ? Math.PI * ScaleFactorSquared
                : 2;
        }

        /// <summary>
        /// Gets a new instance of <see cref="MapProjectionOptions"/> with the same properties as
        /// this one, except the values indicated.
        /// </summary>
        /// <param name="centralMeridian">
        /// <para>
        /// The longitude of the central meridian of the projection, in radians.
        /// </para>
        /// <para>
        /// Values are truncated to the range -π..π.
        /// </para>
        /// </param>
        /// <param name="centralParallel">
        /// <para>
        /// The latitude of the central parallel of the projection, in radians.
        /// </para>
        /// <para>
        /// Values are truncated to the range -π/2..π/2.
        /// </para>
        /// </param>
        /// <param name="standardParallels">
        /// <para>
        /// The latitude of the standard parallels (north and south of the equator) where the scale
        /// of the projection is 1:1, in radians.
        /// </para>
        /// <para>
        /// It does not matter whether the positive or negative latitude is provided, if it is
        /// non-zero.
        /// </para>
        /// <para>
        /// If left <see langword="null"/> the central parallel is assumed.
        /// </para>
        /// <para>
        /// Values are truncated to the range -π/2..π/2.
        /// </para>
        /// </param>
        /// <param name="range">
        /// <para>
        /// If provided, indicates the latitude range (north and south of the central parallel)
        /// shown on the projection, in radians.
        /// </para>
        /// <para>
        /// If left <see langword="null"/>, or equal to zero, the full globe is projected.
        /// </para>
        /// <para>
        /// Values are truncated to the range 0..π.
        /// </para>
        /// </param>
        /// <param name="equalArea">
        /// Indicates whether the projection is to be cylindrical equal-area (rather than
        /// equirectangular).
        /// </param>
        public MapProjectionOptions With(
            double? centralMeridian = null,
            double? centralParallel = null,
            double? standardParallels = null,
            double? range = null,
            bool? equalArea = null) => new(
                centralMeridian ?? CentralMeridian,
                centralParallel ?? CentralParallel,
                standardParallels ?? StandardParallels,
                range ?? Range,
                equalArea ?? EqualArea);
    }
}
