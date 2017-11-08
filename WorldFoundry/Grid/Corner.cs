using System;
using System.Linq;
using System.Numerics;

namespace WorldFoundry.Grid
{
    /// <summary>
    /// Represents a corner between three tiles on the 3D grid.
    /// </summary>
    public class Corner : IEquatable<Corner>
    {
        /// <summary>
        /// The three <see cref="Corner"/>s to which this one is connected.
        /// </summary>
        public int[] Corners { get; } = new int[] { -1, -1, -1 };

        /// <summary>
        /// The three <see cref="Edge"/>s to which this <see cref="Corner"/> is connected.
        /// </summary>
        public int[] Edges { get; } = new int[] { -1, -1, -1 };

        /// <summary>
        /// The elevation above sea level of this <see cref="Corner"/>, in meters.
        /// </summary>
        public float Elevation { get; internal set; }

        internal int Id { get; }

        /// <summary>
        /// The depth of the lake on this <see cref="Corner"/> (if any).
        /// </summary>
        public float LakeDepth { get; set; }

        /// <summary>
        /// The latitude of this <see cref="Corner"/>, as an angle in radians from the equator.
        /// </summary>
        public float Latitude { get; internal set; }

        /// <summary>
        /// The longitude of this <see cref="Corner"/>, as an angle in radians from an arbitrary meridian.
        /// </summary>
        public float Longitude { get; internal set; }

        /// <summary>
        /// The <see cref="WorldFoundry.TerrainType"/> of this <see cref="Corner"/>.
        /// </summary>
        public TerrainType TerrainType { get; internal set; } = TerrainType.Land;

        /// <summary>
        /// The three <see cref="Tile"/>s to which this <see cref="Corner"/> is connected.
        /// </summary>
        public int[] Tiles { get; set; } = new int[] { -1, -1, -1 };

        /// <summary>
        /// The <see cref="Vector3"/> which defines the position of this <see cref="Corner"/>.
        /// </summary>
        public Vector3 Vector { get; internal set; }

        /// <summary>
        /// Creates a new instance of <see cref="Corner"/>.
        /// </summary>
        public Corner() { }

        internal Corner(int id) => Id = id;

        public static bool operator ==(Corner c, object o) => ReferenceEquals(c, null) ? o == null : c.Equals(o);

        public static bool operator !=(Corner c, object o) => ReferenceEquals(c, null) ? o != null : !c.Equals(o);

        /// <summary>
        /// Returns true if this <see cref="Corner"/> is the same as the given object.
        /// </summary>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(obj, null))
            {
                return false;
            }
            if (obj is Corner c)
            {
                return Equals(c);
            }
            return false;
        }

        /// <summary>
        /// Returns true if this <see cref="Corner"/> is the same as the given <see cref="Corner"/>.
        /// </summary>
        public bool Equals(Corner other) => other.Vector == Vector;

        internal Corner GetLowestCorner(IGrid grid, bool riverSources = false)
        {
            var corners = Corners.Select(i => grid.Corners[i]);
            if (riverSources)
            {
                var riverSourceCorners = corners.Where(c => c.Edges.Any(e => grid.Edges[e].RiverSource == c.Id));
                if (riverSourceCorners.Any())
                {
                    corners = riverSourceCorners;
                }
            }
            return corners.OrderBy(c => c.Elevation).First();
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        public override int GetHashCode() => Vector.GetHashCode();

        internal int IndexOf(int cornerIndex) => Array.IndexOf(Corners, cornerIndex);
    }
}
