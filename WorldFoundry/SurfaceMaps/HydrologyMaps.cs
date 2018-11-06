namespace WorldFoundry.SurfaceMaps
{
    /// <summary>
    /// A set of two-dimensional arrays corresponding to points on an equirectangular projected map
    /// of a terrestrial planet's surface, with information about the planet's hydrology.
    /// </summary>
    public struct HydrologyMaps
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
        /// surface and/or ground water in that location, in m³/s. Note that the values are not
        /// normalized, unlike those of most surface maps.
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
        /// Initializes a new instance of <see cref="HydrologyMaps"/>.
        /// </summary>
        /// <param name="flow">A flow map.</param>
        /// <param name="depth">A depth map.</param>
        public HydrologyMaps(float[,] depth, float[,] flow)
        {
            Depth = depth;
            Flow = flow;
        }
    }
}
