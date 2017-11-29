using System;
using System.Numerics;

namespace WorldFoundry.Utilities.MathUtil.Shapes
{
    /// <summary>
    /// Provides information about the properties of a torus.
    /// </summary>
    public class Torus : Shape
    {
        /// <summary>
        /// The distance from the center of the tube to the center of the torus.
        /// </summary>
        public double MajorRadius { get; set; }

        /// <summary>
        /// The radius of the tube, in meters.
        /// </summary>
        public double MinorRadius { get; set; }

        /// <summary>
        /// Initializes a new instance of <see cref="Torus"/>.
        /// </summary>
        public Torus() { }

        /// <summary>
        /// Initializes a new instance of <see cref="Torus"/>.
        /// </summary>
        /// <param name="majorRadius">The length of the major radius of the <see cref="Torus"/>.</param>
        /// <param name="minorRadius">The length of the minor radius of the <see cref="Torus"/>.</param>
        public Torus(double majorRadius, double minorRadius)
        {
            if (majorRadius < minorRadius)
            {
                throw new ArgumentException("majorRadius cannot be smaller than minorRadius", nameof(majorRadius));
            }
            MajorRadius = majorRadius;
            MinorRadius = minorRadius;
        }

        /// <summary>
        /// Gets the total volume of the shape.
        /// </summary>
        /// <returns>The total volume of the shape.</returns>
        public override double GetVolume() => Constants.TwoPISquared * MajorRadius * Math.Pow(MinorRadius, 2);

        /// <summary>
        /// Determines a circular radius which fully contains the shape.
        /// </summary>
        /// <returns>A circular radius which fully contains the shape.</returns>
        public override double GetContainingRadius() => MajorRadius + MinorRadius;

        /// <summary>
        /// Determines if a given point lies within this shape.
        /// </summary>
        /// <param name="position">The position of this shape.</param>
        /// <param name="point">A point in the same 3D space as this shape.</param>
        /// <returns>True if the point is within (or tangent to) this shape.</returns>
        public override bool IsPointWithin(Vector3 position, Vector3 point) =>
            Math.Pow(Math.Pow(point.X - position.X, 2) + Math.Pow(point.Y - position.Y, 2) +
            Math.Pow(point.Z - position.Z, 2) + Math.Pow(MajorRadius, 2) - Math.Pow(MinorRadius, 2), 2) -
            (4 * Math.Pow(MajorRadius, 2) * (Math.Pow(point.X - position.X, 2) + Math.Pow(point.Y - position.Y, 2))) <= 0;
    }
}
