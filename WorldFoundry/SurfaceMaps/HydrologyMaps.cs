using Newtonsoft.Json;
using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace NeverFoundry.WorldFoundry.SurfaceMapping
{
    /// <summary>
    /// A set of two-dimensional arrays corresponding to points on an equirectangular projected map
    /// of a terrestrial planet's surface, with information about the planet's hydrology.
    /// </summary>
    [Serializable]
    [JsonObject]
    public struct HydrologyMaps : ISerializable
    {
        /// <summary>
        /// <para>
        /// A two-dimensional jagged array corresponding to points on an equirectangular projected
        /// map of a terrestrial planet's surface. The first index corresponds to the X coordinate,
        /// and the second index corresponds to the Y coordinate (all second-level arrays should be
        /// of the same length). The values indicate the elevation of surface water in the location
        /// (i.e. lakes). Values range from 0 to 1, with 1 indicating the maximum elevation of the
        /// planet. Note that the values are depths above ground level, not absolute elevations
        /// above sea level. To obtain the absolute elevation of the water level at any point, the
        /// sum of this map and the elevation map can be taken.
        /// </para>
        /// <para>
        /// See also <seealso cref="Space.Planetoid.MaxElevation"/>.
        /// </para>
        /// </summary>
        public float[][] Depth { get; }

        /// <summary>
        /// <para>
        /// A two-dimensional jagged array corresponding to points on an equirectangular projected
        /// map of a terrestrial planet's surface. The first index corresponds to the X coordinate,
        /// and the second index corresponds to the Y coordinate (all second-level arrays should be
        /// of the same length). The values represent the flow rate of surface and/or ground water
        /// in that location, and range from 0 to 1, with 1 indicating the maximum flow rate on the
        /// map.
        /// </para>
        /// <para>
        /// This map does not distinguish between smaller or larger flows (e.g. rivers vs. streams
        /// vs. seeping groundwater). A visualization tool which is displaying flow information
        /// should set a threshold above which flow is displayed, in order to cull the finer
        /// branches of the flow network. Generally speaking, the more distant the view, the higher
        /// the culling threshold should be in order to make flow look more like a map of distinct
        /// "rivers" and less like a dense mesh.
        /// </para>
        /// </summary>
        public float[][] Flow { get; }

        /// <summary>
        /// The maximum flow rate of waterways on this map, in m³/s.
        /// </summary>
        public double MaxFlow { get; }

        /// <summary>
        /// The length of the "X" (0-index) dimension of the maps.
        /// </summary>
        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public int XLength { get; }

        /// <summary>
        /// The length of the "Y" (1-index) dimension of the maps.
        /// </summary>
        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public int YLength { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="HydrologyMaps"/>.
        /// </summary>
        /// <param name="depth">
        /// A depth map. Must have the same dimensions as the <paramref name="flow"/> map.
        /// </param>
        /// <param name="flow">
        /// A flow map. Must have the same dimensions as the <paramref name="depth"/> map.
        /// </param>
        /// <param name="maxFlow">
        /// The maximum flow rate of waterways on this map, in m³/s.
        /// </param>
        [JsonConstructor]
        [System.Text.Json.Serialization.JsonConstructor]
        public HydrologyMaps(float[][] depth, float[][] flow, double maxFlow)
        {
            if (depth.Length != flow.Length)
            {
                throw new ArgumentException($"X length of {nameof(depth)} was not equal to X length of {nameof(flow)}");
            }

            XLength = depth.Length;
            YLength = XLength == 0 ? 0 : depth[0].Length;

            if (XLength != 0 && flow[0].Length != YLength)
            {
                throw new ArgumentException($"Y length of {nameof(depth)} was not equal to Y length of {nameof(flow)}");
            }

            Depth = depth;
            Flow = flow;

            MaxFlow = maxFlow;
        }

        private HydrologyMaps(SerializationInfo info, StreamingContext context) : this(
            (float[][]?)info.GetValue(nameof(Depth), typeof(float[,])) ?? Array.Empty<float[]>(),
            (float[][]?)info.GetValue(nameof(Flow), typeof(float[,])) ?? Array.Empty<float[]>(),
            (double?)info.GetValue(nameof(MaxFlow), typeof(double)) ?? default)
        { }

        /// <summary>Populates a <see cref="SerializationInfo"></see> with the data needed to
        /// serialize the target object.</summary>
        /// <param name="info">The <see cref="SerializationInfo"></see> to populate with
        /// data.</param>
        /// <param name="context">The destination (see <see cref="StreamingContext"></see>) for this
        /// serialization.</param>
        /// <exception cref="System.Security.SecurityException">The caller does not have the
        /// required permission.</exception>
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(Depth), Depth);
            info.AddValue(nameof(Flow), Flow);
            info.AddValue(nameof(MaxFlow), MaxFlow);
        }
    }
}
