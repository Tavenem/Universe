using System;
using System.Numerics;

namespace WorldFoundry.WorldGrids
{
    /// <summary>
    /// Represents a tile on a <see cref="WorldGrid"/>.
    /// </summary>
    internal class Tile
    {
        /// <summary>
        /// The indexes of the <see cref="Corner"/>s to which this <see cref="Tile"/> is connected.
        /// </summary>
        public int[] Corners { get; }

        /// <summary>
        /// The number of sides possessed by this <see cref="Tile"/>.
        /// </summary>
        public int EdgeCount { get; }

        /// <summary>
        /// The normalized elevation above sea level of this <see cref="Tile"/>, as a value between
        /// -1 and 1.
        /// </summary>
        public double Elevation { get; internal set; }

        /// <summary>
        /// The indexes of the <see cref="Tile"/>s to which this one is connected.
        /// </summary>
        public int[] Tiles { get; }

        /// <summary>
        /// The <see cref="Vector3"/> which defines the position of this <see cref="Tile"/>.
        /// </summary>
        public Vector3 Vector { get; internal set; }

        /// <summary>
        /// Creates a new instance of <see cref="Tile"/>.
        /// </summary>
        /// <param name="index">The index of the <see cref="Tile"/>.</param>
        public Tile(int index)
        {
            EdgeCount = index < 12 ? 5 : 6;
            Corners = new int[EdgeCount];
            Tiles = new int[EdgeCount];
            for (var i = 0; i < EdgeCount; i++)
            {
                Corners[i] = -1;
                Tiles[i] = -1;
            }
        }

        internal int IndexOfCorner(int cornerIndex) => Array.IndexOf(Corners, cornerIndex);

        internal int IndexOfTile(int tileIndex) => Array.IndexOf(Tiles, tileIndex);
    }
}
