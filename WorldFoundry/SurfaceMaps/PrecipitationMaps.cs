using System;

namespace WorldFoundry.SurfaceMaps
{
    /// <summary>
    /// A set of two-dimensional arrays corresponding to points on an equirectangular projected map
    /// of a terrestrial planet's surface, with information about the planet's weather during a
    /// particular portion of a year.
    /// </summary>
    public struct PrecipitationMaps
    {
        /// <summary>
        /// A two-dimensional array corresponding to points on an equirectangular projected map of a
        /// terrestrial planet's surface. The first index corresponds to the X coordinate, and the
        /// second index corresponds to the Y coordinate. The values represent the total amount of
        /// precipitation which falls during the specified period. Values range from 0 to 1, with 1
        /// indicating the maximum annual potential precipitation of the planet's atmosphere.
        /// <seealso cref="Climate.Atmosphere.MaxPrecipitation"/>
        /// </summary>
        public float[,] Precipitation { get; }

        /// <summary>
        /// A range giving the minimum, maximum, and average precipitation throughout the specified
        /// area during the period represented by this map, as a value between 0 and 1, with 1
        /// indicating the maximum annual potential precipitation of the planet's atmosphere.
        /// <seealso cref="Climate.Atmosphere.MaxPrecipitation"/>
        /// </summary>
        public FloatRange PrecipitationRange { get; }

        /// <summary>
        /// A two-dimensional array corresponding to points on an equirectangular projected map of a
        /// terrestrial planet's surface. The first index corresponds to the X coordinate, and the
        /// second index corresponds to the Y coordinate. The values represent the total amount of
        /// snow which falls during the specified period. Values range from 0 to 1, with 1
        /// indicating the maximum potential snowfall of the planet's atmosphere.
        /// <seealso cref="Climate.Atmosphere.MaxSnowfall"/>
        /// </summary>
        public float[,] Snowfall { get; }

        /// <summary>
        /// A range giving the minimum, maximum, and average snowfall throughout the specified
        /// area, as a value between 0 and 1, with 1 indicating the maximum annual potential
        /// snowfall of the planet's atmosphere.
        /// <seealso cref="Climate.Atmosphere.MaxSnowfall"/>
        /// </summary>
        public FloatRange SnowfallRange { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="PrecipitationMaps"/>.
        /// </summary>
        /// <param name="precipitation">A precipitation map.</param>
        /// <param name="snowfall">A snowfall map.</param>
        public PrecipitationMaps(
            float[,] precipitation,
            float[,] snowfall)
        {
            Precipitation = precipitation;
            Snowfall = snowfall;

            if (precipitation == null)
            {
                PrecipitationRange = FloatRange.Zero;
            }
            else
            {
                var xLength = precipitation.GetLength(0);
                var yLength = precipitation.GetLength(1);
                var min = 2f;
                var max = -2f;
                var sum = 0f;
                for (var x = 0; x < xLength; x++)
                {
                    for (var y = 0; y < yLength; y++)
                    {
                        min = Math.Min(min, precipitation[x, y]);
                        max = Math.Max(max, precipitation[x, y]);
                        sum += precipitation[x, y];
                    }
                }
                PrecipitationRange = new FloatRange(min, sum / (xLength * yLength), max);
            }

            if (snowfall == null)
            {
                SnowfallRange = FloatRange.Zero;
            }
            else
            {
                var xLength = snowfall.GetLength(0);
                var yLength = snowfall.GetLength(1);
                var min = 2f;
                var max = -2f;
                var sum = 0f;
                for (var x = 0; x < xLength; x++)
                {
                    for (var y = 0; y < yLength; y++)
                    {
                        min = Math.Min(min, snowfall[x, y]);
                        max = Math.Max(max, snowfall[x, y]);
                        sum += snowfall[x, y];
                    }
                }
                SnowfallRange = new FloatRange(min, sum / (xLength * yLength), max);
            }
        }
    }
}
