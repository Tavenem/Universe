using System;
using System.Numerics;

namespace WorldFoundry.Utilities.MathUtil.Shapes
{
    /// <summary>
    /// Provides information about the properties of a sphere.
    /// </summary>
    public class Sphere : Shape
    {
        /// <summary>
        /// The radius of the <see cref="Sphere"/>.
        /// </summary>
        public float Radius { get; set; }

        /// <summary>
        /// Initializes a new instance of <see cref="Sphere"/>.
        /// </summary>
        public Sphere() { }

        /// <summary>
        /// Initializes a new instance of <see cref="Sphere"/> with the given parameters.
        /// </summary>
        /// <param name="radius">The radius of the <see cref="Sphere"/>.</param>
        public Sphere(float radius) => Radius = radius;

        /// <summary>
        /// Gets the total volume of the shape.
        /// </summary>
        /// <returns>The total volume of the shape.</returns>
        public override double GetVolume() => Constants.FourThirdsPI * Math.Pow(Radius, 3);

        /// <summary>
        /// Determines a circular radius which fully contains the shape.
        /// </summary>
        /// <returns>A circular radius which fully contains the shape.</returns>
        public override float GetContainingRadius() => Radius;

        /// <summary>
        /// Determines if a given point lies within this shape.
        /// </summary>
        /// <param name="position">The position of this shape.</param>
        /// <param name="point">A point in the same 3D space as this shape.</param>
        /// <returns>True if the point is within (or tangent to) this shape.</returns>
        public override bool IsPointWithin(Vector3 position, Vector3 point) => (point - position).Length() <= Radius;
    }
}
