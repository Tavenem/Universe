using System.ComponentModel.DataAnnotations.Schema;
using System.Numerics;
using Troschuetz.Random;

namespace WorldFoundry.Space
{
    /// <summary>
    /// Represents a single region of space in a 3D grid.
    /// </summary>
    public class GridSpace
    {
        /// <summary>
        /// Specifies the coordinates of the <see cref="GridSpace"/> in its containing <see cref="CelestialObject"/>.
        /// </summary>
        [NotMapped]
        public Vector3 Coordinates => new Vector3(CoordinatesX, CoordinatesY, CoordinatesZ);

        /// <summary>
        /// Specifies the X-coordinate of the <see cref="GridSpace"/> in its containing <see cref="CelestialObject"/>.
        /// </summary>
        public float CoordinatesX { get; private set; }

        /// <summary>
        /// Specifies the Y-coordinate of the <see cref="GridSpace"/> in its containing <see cref="CelestialObject"/>.
        /// </summary>
        public float CoordinatesY { get; private set; }

        /// <summary>
        /// Specifies the Z-coordinate of the <see cref="GridSpace"/> in its containing <see cref="CelestialObject"/>.
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

        /// <summary>
        /// Initializes a new instance of <see cref="GridSpace"/>.
        /// </summary>
        public GridSpace() { }

        /// <summary>
        /// Initializes a new instance of <see cref="GridSpace"/> with the given values.
        /// </summary>
        public GridSpace(Vector3 coordinates)
        {
            CoordinatesX = coordinates.X;
            CoordinatesY = coordinates.Y;
            CoordinatesZ = coordinates.Z;
        }

        /// <summary>
        /// Returns true if the given coordinates match the <see cref="Coordinates"/> of this <see cref="GridSpace"/>.
        /// </summary>
        /// <param name="coordinates">A set of coordinates to match.</param>
        /// <returns>
        /// true if the given coordinates match the <see cref="Coordinates"/> of this <see
        /// cref="GridSpace"/>; otherwise false.
        /// </returns>
        public bool CoordinatesMatch(Vector3 coordinates)
            => TMath.AreEqual(CoordinatesX, coordinates.X)
            && TMath.AreEqual(CoordinatesY, coordinates.Y)
            && TMath.AreEqual(CoordinatesZ, coordinates.Z);
    }
}
