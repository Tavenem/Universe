using WorldFoundry.WorldGrids;

namespace WorldFoundry.Climate
{
    /// <summary>
    /// Indicates the climate of an <see cref="WorldGrids.Edge"/> during a particular <see cref="Season"/>.
    /// </summary>
    public class EdgeClimate : IIndexedItem
    {
        /// <summary>
        /// The index of this item.
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// The level of river discharge along this <see cref="Edge"/> during this <see
        /// cref="Season"/>, in m³/s.
        /// </summary>
        public float RiverFlow { get; internal set; }

        private EdgeClimate() { }

        /// <summary>
        /// Initializes a new instance of <see cref="EdgeClimate"/>.
        /// </summary>
        public EdgeClimate(int index) => Index = index;
    }
}
