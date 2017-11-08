using System;
using System.Linq;

namespace WorldFoundry.WorldGrid
{
    /// <summary>
    /// Represents an edge between two tiles on the 3D grid.
    /// </summary>
    public class Edge : IEquatable<Edge>
    {
        /// <summary>
        /// The two <see cref="Corner"/>s to which this <see cref="Edge"/> is connected.
        /// </summary>
        public int[] Corners { get; internal set; } = new int[] { -1, -1 };

        /// <summary>
        /// The length of this <see cref="Edge"/>, in meters.
        /// </summary>
        public float Length { get; set; }

        /// <summary>
        /// The index of the <see cref="Corner"/> towards which the river on this <see cref="Edge"/>
        /// flows. -1 if the <see cref="Edge"/> has no river.
        /// </summary>
        public int RiverDirection { get; internal set; } = -1;

        /// <summary>
        /// The index of the <see cref="Corner"/> from which the river on this <see cref="Edge"/>
        /// flows. -1 if the <see cref="Edge"/> has no river.
        /// </summary>
        public int RiverSource { get; internal set; } = -1;

        /// <summary>
        /// The <see cref="WorldFoundry.TerrainType"/> of this <see cref="Edge"/>.
        /// </summary>
        public TerrainType TerrainType { get; internal set; } = TerrainType.Land;

        /// <summary>
        /// The two <see cref="Tile"/>s to which this <see cref="Edge"/> is connected.
        /// </summary>
        public int[] Tiles { get; internal set; } = new int[] { -1, -1 };

        public static bool operator ==(Edge e, object o) => ReferenceEquals(e, null) ? o == null : e.Equals(o);

        public static bool operator !=(Edge e, object o) => ReferenceEquals(e, null) ? o != null : !e.Equals(o);

        /// <summary>
        /// Returns true if this <see cref="Edge"/> is the same as the given object.
        /// </summary>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(obj, null))
            {
                return false;
            }
            if (obj is Edge e)
            {
                return Equals(e);
            }
            return false;
        }

        /// <summary>
        /// Returns true if this <see cref="Edge"/> is the same as the given <see cref="Edge"/>.
        /// </summary>
        public bool Equals(Edge other) => Corners.SequenceEqual(other.Corners);

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 23 + Corners[0].GetHashCode();
            hash = hash * 23 + Corners[1].GetHashCode();
            return hash;
        }

        internal int GetSign(int tileIndex)
        {
            var index = Array.IndexOf(Tiles, tileIndex) + 1;
            return index == 2 ? -1 : index;
        }
    }
}
