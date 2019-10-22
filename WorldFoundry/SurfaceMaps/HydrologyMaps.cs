using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace WorldFoundry.SurfaceMapping
{
    /// <summary>
    /// A set of two-dimensional arrays corresponding to points on an equirectangular projected map
    /// of a terrestrial planet's surface, with information about the planet's hydrology.
    /// </summary>
    [Serializable]
    public struct HydrologyMaps : ISerializable
    {
        /// <summary>
        /// A two-dimensional array corresponding to points on an equirectangular projected map of a
        /// terrestrial planet's surface. The first index corresponds to the X coordinate, and the
        /// second index corresponds to the Y coordinate. The values indicate the elevation of
        /// surface water in the location (i.e. lakes). Values range from 0 to 1, with 1 indicating
        /// the maximum elevation of the planet. Note that the values are depths above ground level,
        /// not absolute elevations above sea level. To obtain the absolute elevation of the water
        /// level at any point, the sum of this map and the elevation map can be taken.
        /// <seealso cref="CelestialBodies.Planetoids.Planetoid.MaxElevation"/>
        /// </summary>
        public float[,] Depth { get; }

        /// <summary>
        /// <para>
        /// A two-dimensional array corresponding to points on an equirectangular projected map of a
        /// terrestrial planet's surface. The first index corresponds to the X coordinate, and the
        /// second index corresponds to the Y coordinate. The values represent the flow rate of
        /// surface and/or ground water in that location, and range from 0 to 1, with 1 indicating
        /// the maximum flow rate on the map.
        /// </para>
        /// <para>This map does not distinguish between smaller or larger flows (e.g. rivers vs.
        /// streams vs. seeping groundwater). A visualization tool which is displaying flow
        /// information should set a threshold above which flow is displayed, in order to cull the
        /// finer branches of the flow network. Generally speaking, the more distant the view, the
        /// higher the culling threshold should be in order to make flow look more like a map of
        /// distinct "rivers" and less like a dense mesh.
        /// </para>
        /// </summary>
        public float[,] Flow { get; }

        /// <summary>
        /// The maximum flow rate of the map, in m³/s.
        /// </summary>
        public double MaxFlow { get; }

        /// <summary>
        /// The length of the "X" (0-index) dimension of the maps.
        /// </summary>
        public int XLength { get; }

        /// <summary>
        /// The length of the "Y" (1-index) dimension of the maps.
        /// </summary>
        public int YLength { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="HydrologyMaps"/>.
        /// </summary>
        /// <param name="xLength">The length of the "X" (0-index) dimension of the maps.</param>
        /// <param name="yLength">The length of the "Y" (1-index) dimension of the maps.</param>
        /// <param name="depth">A depth map.</param>
        /// <param name="flow">A flow map.</param>
        /// <param name="maxFlow">The maximum flow rate of the map, in m³/s.</param>
        public HydrologyMaps(
            int xLength,
            int yLength,
            float[,] depth,
            float[,] flow,
            double maxFlow)
        {
            if (depth.GetLength(0) != xLength)
            {
                throw new ArgumentException($"Length of {nameof(depth)} was not equal to {nameof(xLength)}", nameof(xLength));
            }
            if (depth.GetLength(1) != yLength)
            {
                throw new ArgumentException($"Length of {nameof(depth)} was not equal to {nameof(yLength)}", nameof(yLength));
            }
            if (flow.GetLength(0) != xLength)
            {
                throw new ArgumentException($"Length of {nameof(flow)} was not equal to {nameof(xLength)}", nameof(xLength));
            }
            if (flow.GetLength(1) != yLength)
            {
                throw new ArgumentException($"Length of {nameof(flow)} was not equal to {nameof(yLength)}", nameof(yLength));
            }

            XLength = xLength;
            YLength = yLength;

            Depth = depth;
            Flow = flow;
            MaxFlow = maxFlow;
        }

        private HydrologyMaps(SerializationInfo info, StreamingContext context) : this(
            (int)info.GetValue(nameof(XLength), typeof(int)),
            (int)info.GetValue(nameof(YLength), typeof(int)),
            (float[,])info.GetValue(nameof(Depth), typeof(float[,])),
            (float[,])info.GetValue(nameof(Flow), typeof(float[,])),
            (double)info.GetValue(nameof(MaxFlow), typeof(double))) { }

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
            info.AddValue(nameof(Depth), Depth);
            info.AddValue(nameof(Flow), Flow);
            info.AddValue(nameof(MaxFlow), MaxFlow);
        }
    }
}
