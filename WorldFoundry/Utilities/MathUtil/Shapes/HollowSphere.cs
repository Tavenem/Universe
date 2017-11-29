using System;
using System.Numerics;

namespace WorldFoundry.Utilities.MathUtil.Shapes
{
    /// <summary>
    /// Provides information about the properties of a hollow sphere.
    /// </summary>
    public class HollowSphere : Shape
    {
        /// <summary>
        /// The inner radius of the <see cref="Sphere"/>.
        /// </summary>
        public double InnerRadius { get; set; }

        /// <summary>
        /// The outer radius of the <see cref="Sphere"/>.
        /// </summary>
        public double OuterRadius { get; set; }

        /// <summary>
        /// Initializes a new instance of <see cref="HollowSphere"/>.
        /// </summary>
        public HollowSphere() { }

        /// <summary>
        /// Initializes a new instance of <see cref="HollowSphere"/> with the given parameters.
        /// </summary>
        /// <param name="innerRadius">The inner radius of the <see cref="HollowSphere"/>.</param>
        /// <param name="innerRadius">The outer radius of the <see cref="HollowSphere"/>.</param>
        public HollowSphere(double innerRadius, double outerRadius)
        {
            InnerRadius = innerRadius;
            OuterRadius = outerRadius;
        }

        /// <summary>
        /// Gets the total volume of the shape.
        /// </summary>
        /// <returns>The total volume of the shape.</returns>
        public override double GetVolume() =>
            Constants.FourThirdsPI * Math.Pow(OuterRadius, 3) - Constants.FourThirdsPI * Math.Pow(InnerRadius, 3);

        /// <summary>
        /// Determines a circular radius which fully contains the shape.
        /// </summary>
        /// <returns>A circular radius which fully contains the shape.</returns>
        public override double GetContainingRadius() => OuterRadius;

        /// <summary>
        /// Determines if a given point lies within this shape.
        /// </summary>
        /// <param name="position">The position of this shape.</param>
        /// <param name="point">A point in the same 3D space as this shape.</param>
        /// <returns>True if the point is within (or tangent to) this shape.</returns>
        public override bool IsPointWithin(Vector3 position, Vector3 point) => (point - position).Length() <= OuterRadius && (point - position).Length() >= InnerRadius;
    }
}
