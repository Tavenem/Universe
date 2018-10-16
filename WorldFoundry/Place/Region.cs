using MathAndScience.Shapes;
using System.Linq;
using MathAndScience.Numerics;
using WorldFoundry.CelestialBodies;
using WorldFoundry.Space;

namespace WorldFoundry.Place
{
    /// <summary>
    /// A <see cref="Location"/> which defines a shape as well as a position.
    /// </summary>
    public class Region : Location
    {
        /// <summary>
        /// The position of this location relative to the center of its <see cref="Parent"/>.
        /// </summary>
        public override Vector3 Position
        {
            get => Shape.Position;
            set => Shape = Shape.GetCloneAtPosition(value);
        }

        /// <summary>
        /// The shape of this region.
        /// </summary>
        public virtual IShape Shape { get; set; }

        /// <summary>
        /// Initializes a new instance of <see cref="Region"/>.
        /// </summary>
        private protected Region() { }

        /// <summary>
        /// Initializes a new instance of <see cref="Region"/>.
        /// </summary>
        /// <param name="celestialEntity">The <see cref="Space.CelestialEntity"/> which represents
        /// this location (may be <see langword="null"/>).</param>
        /// <param name="shape">The shape of the region.</param>
        public Region(CelestialEntity celestialEntity, IShape shape)
        {
            CelestialEntity = celestialEntity;
            Shape = shape;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="Region"/>.
        /// </summary>
        /// <param name="celestialEntity">The <see cref="Space.CelestialEntity"/> which represents
        /// this location (may be <see langword="null"/>).</param>
        /// <param name="parent">The parent location in which this one is found.</param>
        /// <param name="shape">The shape of the region.</param>
        public Region(CelestialEntity celestialEntity, Location parent, IShape shape)
        {
            CelestialEntity = celestialEntity;
            Parent = parent;
            Shape = shape;
            Parent?.AddChild(this);
        }

        /// <summary>
        /// Gets a deep clone of this <see cref="Place"/>.
        /// </summary>
        public override Location GetDeepClone() => GetDeepCopy();

        /// <summary>
        /// Gets a deep clone of this <see cref="Region"/>.
        /// </summary>
        public Region GetDeepCopy() => new Region(CelestialEntity, Parent, Shape);

        /// <summary>
        /// Determines whether the specified <see cref="Location"/> is contained within the current
        /// instance.
        /// </summary>
        /// <param name="other">The instance to compare with this one.</param>
        /// <returns>
        /// <see langword="true"/> if the specified <see cref="Location"/> is contained within this
        /// instance; otherwise, <see langword="false"/>.
        /// </returns>
        public override bool Contains(Location other)
        {
            if (!base.Contains(other))
            {
                return false;
            }
            if (!(other is Region region))
            {
                return Shape.IsPointWithin(GetLocalizedPosition(other));
            }
            return Shape.Intersects(region.Shape.GetCloneAtPosition(GetLocalizedPosition(other)));
        }

        /// <summary>
        /// Determines whether the specified <paramref name="position"/> is contained within the
        /// current instance.
        /// </summary>
        /// <param name="position">a <see cref="Vector3"/> to check for inclusion in this <see
        /// cref="Location"/>.</param>
        /// <returns>
        /// <see langword="true"/> if the specified <paramref name="position"/> is contained within
        /// this instance; otherwise, <see langword="false"/>.
        /// </returns>
        public override bool Contains(Vector3 position) => Shape.IsPointWithin(position);

        /// <summary>
        /// Determines the nearest containing <see cref="Region"/> in this instance's parent
        /// hierarchy which contains the specified <paramref name="position"/> (possibly this object
        /// itself, if the position is not out of bounds).
        /// </summary>
        /// <param name="position">The position whose containing <see cref="Region"/> is to be
        /// determined.</param>
        /// <returns>
        /// The nearest containing <see cref="Region"/> in this instance's parent hierarchy which
        /// contains the specified <paramref name="position"/>; or <see langword="null"/> if the
        /// position is not contained in any parent <see cref="Region"/>.
        /// </returns>
        public override Region GetContainingParent(Vector3 position)
        {
            if (position.Length() <= Shape.ContainingRadius)
            {
                return this;
            }
            return base.GetContainingParent(position);
        }

        /// <summary>
        /// Attempts to find a random open space within this region with the given radius.
        /// </summary>
        /// <param name="radius">The radius of the space to find.</param>
        /// <param name="position">When this method returns, will be set to the position of the open
        /// space, if one was found; will be <see cref="Vector3.Zero"/> if no space was
        /// found.</param>
        /// <returns><see langword="true"/> if an open space was found; otherwise <see
        /// langword="false"/>.</returns>
        public bool TryGetOpenSpace(double radius, out Vector3 position)
        {
            Vector3? pos = null;
            var insanityCheck = 0;
            do
            {
                pos = Randomizer.GetRandomVector(Shape.ContainingRadius);
                var shape = new Sphere(radius, pos.Value);
                if (GetAllChildren().Any(x =>
                    (x is Region r && r.Shape.Intersects(shape))
                    || (x.CelestialEntity is CelestialBody body && body.Shape.GetCloneAtPosition(GetLocalizedPosition(x)).Intersects(shape))))
                {
                    pos = null;
                }
                insanityCheck++;
            } while (!pos.HasValue && insanityCheck < 10000);
            position = pos ?? Vector3.Zero;
            return pos.HasValue;
        }
    }
}
