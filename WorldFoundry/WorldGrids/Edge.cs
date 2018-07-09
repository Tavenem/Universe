﻿namespace WorldFoundry.WorldGrids
{
    /// <summary>
    /// Represents an edge between two tiles on an <see cref="WorldGrid"/>.
    /// </summary>
    public class Edge
    {
        /// <summary>
        /// The indexes of the <see cref="Corner"/>s to which this <see cref="Edge"/> is connected.
        /// </summary>
        public int[] Corners { get; private set; }

        /// <summary>
        /// The index of the <see cref="Corner"/> towards which the river on this <see cref="Edge"/>
        /// flows. -1 if the <see cref="Edge"/> has no river.
        /// </summary>
        public int RiverDirection { get; internal set; } = -1;

        /// <summary>
        /// The average volume of water flowing in the river along this <see cref="Edge"/>, in m³/s.
        /// </summary>
        public FloatRange RiverFlow { get; internal set; }

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
        /// The indexes of the <see cref="Tile"/>s to which this <see cref="Edge"/> is connected.
        /// </summary>
        public int[] Tiles { get; private set; }

        /// <summary>
        /// Creates a new instance of <see cref="Edge"/>.
        /// </summary>
        public Edge()
        {
            Corners = new int[2];
            Tiles = new int[2];
            for (var i = 0; i < 2; i++)
            {
                Corners[i] = -1;
                Tiles[i] = -1;
            }
        }
    }
}
