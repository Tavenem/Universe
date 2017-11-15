using System.ComponentModel.DataAnnotations.Schema;
using System.Numerics;

namespace WorldFoundry.Space
{
    /// <summary>
    /// Represents a single region of space in a 3D grid.
    /// </summary>
    public class GridSpace
    {
        /// <summary>
        /// Specifies the coordinates of the <see cref="GridSpace"/> in its containing <see cref="SpaceRegion"/>.
        /// </summary>
        [NotMapped]
        public Vector3 Coordinates
        {
            get => new Vector3(CoordinatesX, CoordinatesY, CoordinatesZ);
            private set
            {
                CoordinatesX = value.X;
                CoordinatesY = value.Y;
                CoordinatesZ = value.Z;
            }
        }

        /// <summary>
        /// Specifies the X-coordinate of the <see cref="GridSpace"/> in its containing <see cref="SpaceRegion"/>.
        /// </summary>
        public float CoordinatesX { get; private set; }

        /// <summary>
        /// Specifies the Y-coordinate of the <see cref="GridSpace"/> in its containing <see cref="SpaceRegion"/>.
        /// </summary>
        public float CoordinatesY { get; private set; }

        /// <summary>
        /// Specifies the Z-coordinate of the <see cref="GridSpace"/> in its containing <see cref="SpaceRegion"/>.
        /// </summary>
        public float CoordinatesZ { get; private set; }

        /// <summary>
        /// Indicates whether or not this <see cref="GridSpace"/> has been populated with children.
        /// </summary>
        /// <remarks>
        /// Does not necessarily indicate that the <see cref="GridSpace"/> actually has children,
        /// only that the population routine has completed, which may have indicated that no children
        /// are present.
        /// </remarks>
        public bool Populated { get; internal set; }
    }
}
