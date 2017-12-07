using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Numerics;

namespace WorldFoundry.WorldGrids
{
    /// <summary>
    /// Represents a corner between three tiles on an <see cref="WorldGrid"/>.
    /// </summary>
    public class Corner : DataItem, IIndexedItem
    {
        /// <summary>
        /// The index of the first <see cref="Corner"/> to which this one is connected.
        /// </summary>
        public int Corner0 { get; private set; } = -1;

        /// <summary>
        /// The index of the second <see cref="Corner"/> to which this one is connected.
        /// </summary>
        public int Corner1 { get; private set; } = -1;

        /// <summary>
        /// The index of the third <see cref="Corner"/> to which this one is connected.
        /// </summary>
        public int Corner2 { get; private set; } = -1;

        /// <summary>
        /// The index of the first <see cref="Edge"/> to which this <see cref="Corner"/> is connected.
        /// </summary>
        public int Edge0 { get; private set; } = -1;

        /// <summary>
        /// The index of the second <see cref="Edge"/> to which this <see cref="Corner"/> is connected.
        /// </summary>
        public int Edge1 { get; private set; } = -1;

        /// <summary>
        /// The index of the third <see cref="Edge"/> to which this <see cref="Corner"/> is connected.
        /// </summary>
        public int Edge2 { get; private set; } = -1;

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
        /// The index of the first <see cref="Tile"/> to which this <see cref="Corner"/> is connected.
        /// </summary>
        public int Tile0 { get; private set; } = -1;

        /// <summary>
        /// The index of the second <see cref="Tile"/> to which this <see cref="Corner"/> is connected.
        /// </summary>
        public int Tile1 { get; private set; } = -1;

        /// <summary>
        /// The index of the third <see cref="Tile"/> to which this <see cref="Corner"/> is connected.
        /// </summary>
        public int Tile2 { get; private set; } = -1;

        /// <summary>
        /// The <see cref="Vector3"/> which defines the position of this <see cref="Corner"/>.
        /// </summary>
        [NotMapped]
        public Vector3 Vector
        {
            get => new Vector3(VectorX, VectorY, VectorZ);
            set
            {
                VectorX = value.X;
                VectorY = value.Y;
                VectorZ = value.Z;
            }
        }

        /// <summary>
        /// The X component of the vector which defines the position of this <see cref="Corner"/>.
        /// </summary>
        private protected float VectorX { get; private set; }

        /// <summary>
        /// The Y component of the vector which defines the position of this <see cref="Corner"/>.
        /// </summary>
        private protected float VectorY { get; private set; }

        /// <summary>
        /// The Z component of the vector which defines the position of this <see cref="Corner"/>.
        /// </summary>
        private protected float VectorZ { get; private set; }

        /// <summary>
        /// The <see cref="WorldGrids.WorldGrid"/> of which this <see cref="Tile"/> forms a part.
        /// </summary>
        internal WorldGrid WorldGrid { get; private set; }

        /// <summary>
        /// Creates a new instance of <see cref="Corner"/>.
        /// </summary>
        public Corner() { }

        /// <summary>
        /// Creates a new instance of <see cref="Corner"/>.
        /// </summary>
        internal Corner(WorldGrid worldGrid, int id)
        {
            WorldGrid = worldGrid;
            Index = id;
        }

        /// <summary>
        /// Gets the index of the <see cref="Corner"/> at the given index in this <see
        /// cref="Corner"/>'s collection.
        /// </summary>
        /// <param name="index">
        /// An index to this <see cref="Corner"/>'s collection of <see cref="Corner"/>s.
        /// </param>
        /// <returns>The index of the <see cref="Corner"/> at the given index.</returns>
        public int GetCorner(int index)
        {
            if (index == 0)
            {
                return Corner0;
            }
            if (index == 1)
            {
                return Corner1;
            }
            if (index == 2)
            {
                return Corner2;
            }
            return -1;
        }

        /// <summary>
        /// Enumerates all three <see cref="Corner"/>s to which this one is connected.
        /// </summary>
        public IEnumerable<int> GetCorners() => (new int[] { Corner0, Corner1, Corner2 }).AsEnumerable();

        /// <summary>
        /// Gets the index of the <see cref="Edge"/> at the given index in this <see
        /// cref="Corner"/>'s collection.
        /// </summary>
        /// <param name="index">
        /// An index to this <see cref="Corner"/>'s collection of <see cref="Edge"/>s.
        /// </param>
        /// <returns>The index of the <see cref="Edge"/> at the given index.</returns>
        public int GetEdge(int index)
        {
            if (index == 0)
            {
                return Edge0;
            }
            if (index == 1)
            {
                return Edge1;
            }
            if (index == 2)
            {
                return Edge2;
            }
            return -1;
        }

        /// <summary>
        /// Enumerates all three <see cref="Edge"/>s to which this <see cref="Corner"/> is connected.
        /// </summary>
        public IEnumerable<int> GetEdges() => (new int[] { Edge0, Edge1, Edge2 }).AsEnumerable();

        /// <summary>
        /// Gets the index of the <see cref="Tile"/> at the given index in this <see
        /// cref="Corner"/>'s collection.
        /// </summary>
        /// <param name="index">
        /// An index to this <see cref="Corner"/>'s collection of <see cref="Tile"/>s.
        /// </param>
        /// <returns>The index of the <see cref="Tile"/> at the given index.</returns>
        public int GetTile(int index)
        {
            if (index == 0)
            {
                return Tile0;
            }
            if (index == 1)
            {
                return Tile1;
            }
            if (index == 2)
            {
                return Tile2;
            }
            return -1;
        }

        /// <summary>
        /// Enumerates all three <see cref="Tile"/>s to which this <see cref="Corner"/> is connected.
        /// </summary>
        public IEnumerable<int> GetTiles() => (new int[] { Tile0, Tile1, Tile2 }).AsEnumerable();

        internal Corner GetLowestCorner(WorldGrid grid, bool riverSources = false)
        {
            var corners = GetCorners().Select(i => grid.CornerArray[i]);
            if (riverSources)
            {
                var riverSourceCorners = corners.Where(c => c.GetEdges().Any(e => grid.EdgeArray[e].RiverSource == c.Index));
                if (riverSourceCorners.Any())
                {
                    corners = riverSourceCorners;
                }
            }
            return corners.OrderBy(c => c.Elevation).First();
        }

        internal int IndexOfCorner(int cornerIndex)
        {
            if (Corner0 == cornerIndex)
            {
                return 0;
            }
            if (Corner1 == cornerIndex)
            {
                return 1;
            }
            if (Corner2 == cornerIndex)
            {
                return 2;
            }
            return -1;
        }

        /// <summary>
        /// Sets the value of the <see cref="Corner"/> index at the given index to this <see
        /// cref="Corner"/>'s collection.
        /// </summary>
        /// <param name="index">
        /// The index to this <see cref="Corner"/>'s collection of <see cref="Corner"/>s to set.
        /// </param>
        /// <param name="value">The value to store in the given index.</param>
        internal void SetCorner(int index, int value)
        {
            if (index == 0)
            {
                Corner0 = value;
            }
            if (index == 1)
            {
                Corner1 = value;
            }
            if (index == 2)
            {
                Corner2 = value;
            }
        }

        /// <summary>
        /// Sets the value of the <see cref="Edge"/> index at the given index to this <see
        /// cref="Corner"/>'s collection.
        /// </summary>
        /// <param name="index">
        /// The index to this <see cref="Corner"/>'s collection of <see cref="Edge"/>s to set.
        /// </param>
        /// <param name="value">The value to store in the given index.</param>
        internal void SetEdge(int index, int value)
        {
            if (index == 0)
            {
                Edge0 = value;
            }
            if (index == 1)
            {
                Edge1 = value;
            }
            if (index == 2)
            {
                Edge2 = value;
            }
        }

        /// <summary>
        /// Sets the value of the <see cref="Tile"/> index at the given index to this <see
        /// cref="Corner"/>'s collection.
        /// </summary>
        /// <param name="index">
        /// The index to this <see cref="Corner"/>'s collection of <see cref="Tile"/>s to set.
        /// </param>
        /// <param name="value">The value to store in the given index.</param>
        internal void SetTile(int index, int value)
        {
            if (index == 0)
            {
                Tile0 = value;
            }
            if (index == 1)
            {
                Tile1 = value;
            }
            if (index == 2)
            {
                Tile2 = value;
            }
        }
    }
}
