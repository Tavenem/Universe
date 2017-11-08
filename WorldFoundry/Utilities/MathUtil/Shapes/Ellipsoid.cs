using System;
using System.Numerics;

namespace WorldFoundry.Utilities.MathUtil.Shapes
{
    /// <summary>
    /// Provides information about the properties of an ellipsoid.
    /// </summary>
    public class Ellipsoid : Shape
    {
        /// <summary>
        /// The length of the first axis of the <see cref="Ellipsoid"/>.
        /// </summary>
        public float AxisA { get; set; }

        /// <summary>
        /// The length of the second axis of the <see cref="Ellipsoid"/>.
        /// </summary>
        public float AxisB { get; set; }

        /// <summary>
        /// The length of the third axis of the <see cref="Ellipsoid"/>.
        /// </summary>
        public float AxisC { get; set; }

        /// <summary>
        /// Initializes a new instance of <see cref="Ellipsoid"/>.
        /// </summary>
        public Ellipsoid() { }

        /// <summary>
        /// Initializes a new instance of <see cref="Ellipsoid"/> with the given parameters.
        /// </summary>
        /// <param name="axisA">The length of the first radius of the <see cref="Ellipsoid"/>.</param>
        /// <param name="axisB">The length of the second radius of the <see cref="Ellipsoid"/>.</param>
        /// <param name="axisC">The length of the third radius of the <see cref="Ellipsoid"/>.</param>
        public Ellipsoid(float axisA, float axisB, float axisC)
        {
            AxisA = axisA;
            AxisB = axisB;
            AxisC = axisC;
        }

        /// <summary>
        /// Gets the total volume of the shape.
        /// </summary>
        /// <returns>The total volume of the shape.</returns>
        public override double GetVolume() => Constants.FourThirdsPI * AxisA * AxisB * AxisC;

        /// <summary>
        /// Determines a circular radius which fully contains the shape.
        /// </summary>
        /// <returns>A circular radius which fully contains the shape.</returns>
        public override float GetContainingRadius() => Math.Max(AxisA, Math.Max(AxisB, AxisC));

        /// <summary>
        /// Determines if a given point lies within this shape.
        /// </summary>
        /// <param name="position">The position of this shape.</param>
        /// <param name="point">A point in the same 3-D space as this shape.</param>
        /// <returns>True if the point is within (or tangent to) this shape.</returns>
        public override bool IsPointWithin(Vector3 position, Vector3 point) =>
            Math.Pow((point.X - position.X) / AxisA, 2) + Math.Pow((point.Y - position.Y) / AxisB, 2) +
            Math.Pow((point.Z - position.Z) / AxisC, 2) <= 1;
    }
}
