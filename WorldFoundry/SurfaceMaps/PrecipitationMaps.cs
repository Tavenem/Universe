using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace NeverFoundry.WorldFoundry.SurfaceMapping
{
    /// <summary>
    /// A set of two-dimensional arrays corresponding to points on an equirectangular projected map
    /// of a terrestrial planet's surface, with information about the planet's weather during a
    /// particular portion of a year.
    /// </summary>
    [Serializable]
    public struct PrecipitationMaps : ISerializable
    {
        /// <summary>
        /// A range giving the minimum, maximum, and average precipitation throughout the specified
        /// area during the period represented by this map, as a value between 0 and 1, with 1
        /// indicating the maximum annual potential precipitation of the planet's atmosphere.
        /// <seealso cref="Climate.Atmosphere.MaxPrecipitation"/>
        /// </summary>
        public FloatRange Precipitation { get; }

        /// <summary>
        /// A two-dimensional array corresponding to points on an equirectangular projected map of a
        /// terrestrial planet's surface. The first index corresponds to the X coordinate, and the
        /// second index corresponds to the Y coordinate. The values represent the total amount of
        /// precipitation which falls during the specified period. Values range from 0 to 1, with 1
        /// indicating the maximum annual potential precipitation of the planet's atmosphere.
        /// <seealso cref="Climate.Atmosphere.MaxPrecipitation"/>
        /// </summary>
        public float[,] PrecipitationMap { get; }

        /// <summary>
        /// A range giving the minimum, maximum, and average snowfall throughout the specified
        /// area, as a value between 0 and 1, with 1 indicating the maximum annual potential
        /// snowfall of the planet's atmosphere.
        /// <seealso cref="Climate.Atmosphere.MaxSnowfall"/>
        /// </summary>
        public FloatRange Snowfall { get; }

        /// <summary>
        /// A two-dimensional array corresponding to points on an equirectangular projected map of a
        /// terrestrial planet's surface. The first index corresponds to the X coordinate, and the
        /// second index corresponds to the Y coordinate. The values represent the total amount of
        /// snow which falls during the specified period. Values range from 0 to 1, with 1
        /// indicating the maximum potential snowfall of the planet's atmosphere.
        /// <seealso cref="Climate.Atmosphere.MaxSnowfall"/>
        /// </summary>
        public float[,] SnowfallMap { get; }

        /// <summary>
        /// The length of the "X" (0-index) dimension of the maps.
        /// </summary>
        public int XLength { get; }

        /// <summary>
        /// The length of the "Y" (1-index) dimension of the maps.
        /// </summary>
        public int YLength { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="PrecipitationMaps"/>.
        /// </summary>
        /// <param name="xLength">The length of the "X" (0-index) dimension of the maps.</param>
        /// <param name="yLength">The length of the "Y" (1-index) dimension of the maps.</param>
        /// <param name="precipitation">A precipitation map.</param>
        /// <param name="snowfall">A snowfall map.</param>
        public PrecipitationMaps(
            int xLength,
            int yLength,
            float[,] precipitation,
            float[,] snowfall)
        {
            if (precipitation.GetLength(0) != xLength)
            {
                throw new ArgumentException($"Length of {nameof(precipitation)} was not equal to {nameof(xLength)}", nameof(xLength));
            }
            if (precipitation.GetLength(1) != yLength)
            {
                throw new ArgumentException($"Length of {nameof(precipitation)} was not equal to {nameof(yLength)}", nameof(yLength));
            }
            if (snowfall.GetLength(0) != xLength)
            {
                throw new ArgumentException($"Length of {nameof(snowfall)} was not equal to {nameof(xLength)}", nameof(xLength));
            }
            if (snowfall.GetLength(1) != yLength)
            {
                throw new ArgumentException($"Length of {nameof(snowfall)} was not equal to {nameof(yLength)}", nameof(yLength));
            }

            XLength = xLength;
            YLength = yLength;

            PrecipitationMap = precipitation;
            SnowfallMap = snowfall;

            if (precipitation is null)
            {
                Precipitation = FloatRange.Zero;
            }
            else
            {
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
                Precipitation = new FloatRange(min, sum / (xLength * yLength), max);
            }

            if (snowfall is null)
            {
                Snowfall = FloatRange.Zero;
            }
            else
            {
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
                Snowfall = new FloatRange(min, sum / (xLength * yLength), max);
            }
        }

        private PrecipitationMaps(SerializationInfo info, StreamingContext context) : this(
            (int)info.GetValue(nameof(XLength), typeof(int)),
            (int)info.GetValue(nameof(YLength), typeof(int)),
            (float[,])info.GetValue(nameof(PrecipitationMap), typeof(float[,])),
            (float[,])info.GetValue(nameof(SnowfallMap), typeof(float[,])))
        { }

        /// <summary>Populates a <see cref="SerializationInfo"></see> with the data needed to
        /// serialize the target object.</summary>
        /// <param name="info">The <see cref="SerializationInfo"></see> to populate with
        /// data.</param>
        /// <param name="context">The destination (see <see cref="StreamingContext"></see>) for this
        /// serialization.</param>
        /// <exception cref="System.Security.SecurityException">The caller does not have the
        /// required permission.</exception>
        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(XLength), XLength);
            info.AddValue(nameof(YLength), YLength);
            info.AddValue(nameof(PrecipitationMap), PrecipitationMap);
            info.AddValue(nameof(SnowfallMap), SnowfallMap);
        }
    }
}
