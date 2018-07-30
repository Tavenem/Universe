using System;
using System.Linq;
using System.Numerics;

namespace WorldFoundry.WorldGrids
{
    /// <summary>
    /// Represents a corner between three tiles on an <see cref="WorldGrid"/>.
    /// </summary>
    public class Corner
    {
        /// <summary>
        /// The indexes of the <see cref="Corner"/>s to which this one is connected.
        /// </summary>
        public int[] Corners { get; }

        /// <summary>
        /// The indexes of the <see cref="Edge"/>s to which this <see cref="Corner"/> is connected.
        /// </summary>
        public int[] Edges { get; }

        /// <summary>
        /// The elevation above sea level of this <see cref="Corner"/>, in meters.
        /// </summary>
        public float Elevation { get; internal set; }

        /// <summary>
        /// The index of this <see cref="Corner"/>.
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// The depth of the lake on this <see cref="Corner"/> (if any).
        /// </summary>
        public float LakeDepth { get; set; }

        /// <summary>
        /// The latitude of this <see cref="Corner"/>, as an angle in radians from the equator.
        /// </summary>
        public float Latitude { get; internal set; }

        /// <summary>
        /// The longitude of this <see cref="Corner"/>, as an angle in radians from the X-axis at 0 rotation.
        /// </summary>
        public float Longitude { get; internal set; }

        /// <summary>
        /// The <see cref="WorldFoundry.TerrainType"/> of this <see cref="Corner"/>.
        /// </summary>
        public TerrainType TerrainType { get; internal set; } = TerrainType.Land;

        /// <summary>
        /// The indexes of the <see cref="Tile"/>s to which this <see cref="Corner"/> is connected.
        /// </summary>
        public int[] Tiles { get; }

        /// <summary>
        /// The <see cref="Vector3"/> which defines the position of this <see cref="Corner"/>.
        /// </summary>
        public Vector3 Vector { get; internal set; }

        /// <summary>
        /// Creates a new instance of <see cref="Corner"/>.
        /// </summary>
        public Corner() { }

        /// <summary>
        /// Creates a new instance of <see cref="Corner"/>.
        /// </summary>
        /// <param name="index">The <see cref="Index"/> of the <see cref="Corner"/>.</param>
        internal Corner(int index)
        {
            Index = index;
            Corners = new int[3];
            Edges = new int[3];
            Tiles = new int[3];
            for (var i = 0; i < 3; i++)
            {
                Corners[i] = -1;
                Edges[i] = -1;
                Tiles[i] = -1;
            }
        }

        internal Corner GetLowestCorner(WorldGrid grid, bool riverSources = false)
        {
            var corners = Corners.Select(i => grid.Corners[i]);
            if (riverSources)
            {
                var riverSourceCorners = Corners.Select(i => grid.Corners[i]).Where(c => c.Edges.Any(e => grid.Edges[e].RiverSource == c.Index));
                if (riverSourceCorners.Any())
                {
                    return riverSourceCorners.OrderBy(c => c.Elevation).First();
                }
            }
            return corners.OrderBy(c => c.Elevation).First();
        }

        internal int IndexOfCorner(int cornerIndex) => Array.IndexOf(Corners, cornerIndex);
    }
}
