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
