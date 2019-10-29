using System.Numerics;

namespace NeverFoundry.WorldFoundry.WorldGrids
{
    /// <summary>
    /// Represents a corner between three tiles on an <see cref="WorldGrid"/>.
    /// </summary>
    internal class Corner
    {
        /// <summary>
        /// The indexes of the <see cref="Corner"/> instances to which this one is connected.
        /// </summary>
        public int[] Corners { get; }

        /// <summary>
        /// The indexes of the <see cref="Tile"/> instances to which this <see cref="Corner"/> is
        /// connected.
        /// </summary>
        public int[] Tiles { get; }

        /// <summary>
        /// The <see cref="Vector3"/> which defines the position of this <see cref="Corner"/>.
        /// </summary>
        public Vector3 Vector { get; internal set; }

        /// <summary>
        /// Creates a new instance of <see cref="Corner"/>.
        /// </summary>
        public Corner()
        {
            Corners = new int[3];
            Tiles = new int[3];
            for (var i = 0; i < 3; i++)
            {
                Corners[i] = -1;
                Tiles[i] = -1;
            }
        }
    }
}
