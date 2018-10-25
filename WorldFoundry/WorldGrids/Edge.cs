namespace WorldFoundry.WorldGrids
{
    /// <summary>
    /// Represents an edge between two tiles on an <see cref="WorldGrid"/>.
    /// </summary>
    public class Edge
    {
        /// <summary>
        /// The indexes of the <see cref="Corner"/>s to which this <see cref="Edge"/> is connected.
        /// </summary>
        public int[] Corners { get; }

        /// <summary>
        /// The index of the <see cref="Corner"/> towards which a river on this <see cref="Edge"/>
        /// flows.
        /// </summary>
        public int RiverDirection => RiverSource == -1 ? -1 : (RiverSource == 0 ? 1 : 0);

        /// <summary>
        /// The volume of water flowing in the river along this <see cref="Edge"/>, in m³/s.
        /// </summary>
        public float RiverFlow { get; internal set; }

        /// <summary>
        /// The index of the <see cref="Corner"/> from which a river on this <see cref="Edge"/>
        /// flows.
        /// </summary>
        public int RiverSource { get; internal set; }

        /// <summary>
        /// The indexes of the <see cref="Tile"/>s to which this <see cref="Edge"/> is connected.
        /// </summary>
        public int[] Tiles { get; }

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
