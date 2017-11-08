using System.Numerics;

namespace WorldFoundry.Utilities.MathUtil.Shapes
{
    /// <summary>
    /// Provides information about the properties of a geometric shape.
    /// </summary>
    public class Shape
    {
        /// <summary>
        /// Gets the total volume of the shape.
        /// </summary>
        /// <returns>The total volume of the shape.</returns>
        public virtual double GetVolume() => 0;

        /// <summary>
        /// Determines a circular radius which fully contains the shape.
        /// </summary>
        /// <returns>A circular radius which fully contains the shape.</returns>
        public virtual float GetContainingRadius() => 0;

        /// <summary>
        /// Determines if a given point lies within this shape.
        /// </summary>
        /// <param name="position">The position of this shape.</param>
        /// <param name="point">A point in the same 3D space as this shape.</param>
        /// <returns>True if the point is within (or tangent to) this shape.</returns>
        public virtual bool IsPointWithin(Vector3 position, Vector3 point) => false;
    }
}
