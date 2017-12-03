using System.Collections.Generic;
using System.Linq;

namespace WorldFoundry.WorldGrids
{
    /// <summary>
    /// Represents an edge between two tiles on an <see cref="WorldGrid"/>.
    /// </summary>
    public class Edge : IIndexedItem
    {
        /// <summary>
        /// The index of the first <see cref="Corner"/> to which this <see cref="Edge"/> is connected.
        /// </summary>
        public int Corner0 { get; private set; } = -1;

        /// <summary>
        /// The index of the second <see cref="Corner"/> to which this <see cref="Edge"/> is connected.
        /// </summary>
        public int Corner1 { get; private set; } = -1;

        /// <summary>
        /// The index of this <see cref="Edge"/>.
        /// </summary>
        public int Index { get; }

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
        /// The index of the first <see cref="Tile"/> to which this <see cref="Edge"/> is connected.
        /// </summary>
        public int Tile0 { get; private set; } = -1;

        /// <summary>
        /// The index of the second <see cref="Tile"/> to which this <see cref="Edge"/> is connected.
        /// </summary>
        public int Tile1 { get; private set; } = -1;

        /// <summary>
        /// The <see cref="WorldGrids.WorldGrid"/> of which this <see cref="Tile"/> forms a part.
        /// </summary>
        internal WorldGrid WorldGrid { get; private set; }

        /// <summary>
        /// Creates a new instance of <see cref="Edge"/>.
        /// </summary>
        private Edge() { }

        /// <summary>
        /// Creates a new instance of <see cref="Edge"/>.
        /// </summary>
        internal Edge(WorldGrid worldGrid, int id)
        {
            WorldGrid = worldGrid;
            Index = id;
        }

        /// <summary>
        /// Gets the index of the <see cref="Corner"/> at the given index in this <see
        /// cref="Edge"/>'s collection.
        /// </summary>
        /// <param name="index">
        /// An index to this <see cref="Edge"/>'s collection of <see cref="Corner"/>s.
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
            return -1;
        }

        /// <summary>
        /// Enumerates all three <see cref="Corner"/>s to which this <see cref="Edge"/> is connected.
        /// </summary>
        public IEnumerable<int> GetCorners() => (new int[] { Corner0, Corner1 }).AsEnumerable();

        /// <summary>
        /// Gets the index of the <see cref="Tile"/> at the given index in this <see
        /// cref="Edge"/>'s collection.
        /// </summary>
        /// <param name="index">
        /// An index to this <see cref="Edge"/>'s collection of <see cref="Tile"/>s.
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
            return -1;
        }

        /// <summary>
        /// Enumerates all three <see cref="Tile"/>s to which this <see cref="Edge"/> is connected.
        /// </summary>
        public IEnumerable<int> GetTiles() => (new int[] { Tile0, Tile1 }).AsEnumerable();

        internal int GetSign(int tileIndex)
        {
            if (Tile0 == tileIndex)
            {
                return 1;
            }
            else if (Tile1 == tileIndex)
            {
                return -1;
            }
            return 0;
        }

        /// <summary>
        /// Sets the value of the <see cref="Corner"/> index at the given index to this <see
        /// cref="Edge"/>'s collection.
        /// </summary>
        /// <param name="index">
        /// The index to this <see cref="Edge"/>'s collection of <see cref="Corner"/>s to set.
        /// </param>
        /// <param name="value">The value to store in the given index.</param>
        public void SetCorner(int index, int value)
        {
            if (index == 0)
            {
                Corner0 = value;
            }
            if (index == 1)
            {
                Corner1 = value;
            }
        }

        /// <summary>
        /// Sets the value of the <see cref="Tile"/> index at the given index to this <see
        /// cref="Edge"/>'s collection.
        /// </summary>
        /// <param name="index">
        /// The index to this <see cref="Corner"/>'s collection of <see cref="Edge"/>s to set.
        /// </param>
        /// <param name="value">The value to store in the given index.</param>
        public void SetTile(int index, int value)
        {
            if (index == 0)
            {
                Tile0 = value;
            }
            if (index == 1)
            {
                Tile1 = value;
            }
        }
    }
}
