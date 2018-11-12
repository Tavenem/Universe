using ExtensionLib;
using MathAndScience.Numerics;
using MathAndScience.Shapes;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using WorldFoundry.CelestialBodies;

namespace WorldFoundry.Place
{
    /// <summary>
    /// A <see cref="Location"/> which defines a shape as well as a position.
    /// </summary>
    public class Region : Location, ISupportInitialize
    {
        private List<ILocation> _children;
        /// <summary>
        /// The child locations contained within this one.
        /// </summary>
        public IEnumerable<ILocation> Children => _children ?? Enumerable.Empty<ILocation>();

        /// <summary>
        /// The position of this location relative to the center of its containing region.
        /// </summary>
        public override Vector3 Position
        {
            get => Shape.Position;
            set => Shape = Shape.GetCloneAtPosition(value);
        }

        private IShape _shape;
        /// <summary>
        /// The shape of this region.
        /// </summary>
        public virtual IShape Shape
        {
            get => _shape;
            set => _shape = value;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="Region"/>.
        /// </summary>
        private protected Region() { }

        /// <summary>
        /// Initializes a new instance of <see cref="Region"/>.
        /// </summary>
        /// <param name="shape">The shape of the region.</param>
        public Region(IShape shape) => Shape = shape;

        /// <summary>
        /// Initializes a new instance of <see cref="Region"/>.
        /// </summary>
        /// <param name="containingRegion">The region in which this one is found.</param>
        /// <param name="shape">The shape of the region.</param>
        public Region(Region containingRegion, IShape shape)
        {
            Shape = shape;
            ContainingRegion = containingRegion;
            containingRegion?.AddChild(this);
        }

        /// <summary>
        /// Signals the object that initialization is starting.
        /// </summary>
        public void BeginInit() { }

        /// <summary>
        /// Determines whether the specified <see cref="Location"/> is contained within the current
        /// instance.
        /// </summary>
        /// <param name="other">The instance to compare with this one.</param>
        /// <returns>
        /// <see langword="true"/> if the specified <see cref="Location"/> is contained within this
        /// instance; otherwise, <see langword="false"/>.
        /// </returns>
        public bool Contains(ILocation other)
        {
            if (GetCommonContainingRegion(other) != this)
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
        public bool Contains(Vector3 position) => Shape.IsPointWithin(position);

        /// <summary>
        /// Signals the object that initialization is complete.
        /// </summary>
        public void EndInit()
        {
            if (_children != null)
            {
                foreach (var child in _children)
                {
                    child.SetNewContainingRegion(this);
                }
            }
        }

        /// <summary>
        /// Gets a flattened enumeration of all descendants of this instance.
        /// </summary>
        /// <returns>A flattened <see cref="IEnumerable{T}"/> of all descendant child <see
        /// cref="Location"/> instances of this one.</returns>
        public IEnumerable<ILocation> GetAllChildren()
        {
            foreach (var child in Children)
            {
                if (child != null)
                {
                    yield return child;
                }

                if (child is Region region)
                {
                    foreach (var sub in region.GetAllChildren())
                    {
                        if (sub != null)
                        {
                            yield return sub;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Finds a common <see cref="Region"/> which contains both this and the given location.
        /// </summary>
        /// <param name="other">The other <see cref="Location"/>.</param>
        /// <returns>
        /// A common <see cref="Region"/> which contains both this and the given location (may be
        /// either of them); or <see langword="null"/> if this instance and the given location do
        /// not have a common parent.
        /// </returns>
        public override Region GetCommonContainingRegion(ILocation other)
            => other == this ? this : base.GetCommonContainingRegion(other);

        /// <summary>
        /// Determines the smallest child <see cref="Region"/> at any level of this instance's
        /// descendant hierarchy which contains the specified <paramref name="position"/>.
        /// </summary>
        /// <param name="position">The position whose smallest containing <see cref="Region"/> is to
        /// be determined.</param>
        /// <returns>
        /// The smallest child <see cref="Region"/> at any level of this instance's descendant
        /// hierarchy which contains the specified <paramref name="position"/>, or this instance, if
        /// no child contains the position.
        /// </returns>
        public Region GetContainingChild(Vector3 position)
            => GetAllChildren().OfType<Region>()
            .Where(x => x.Shape.IsPointWithin(x.GetLocalizedPosition(this, position)))
            .ItemWithMin(x => x.Shape.ContainingRadius)
            ?? (this is Region region && Contains(position) ? region : null);

        /// <summary>
        /// Determines the smallest child <see cref="Region"/> at any level of this instance's
        /// descendant hierarchy which fully contains the specified <see cref="Location"/> within
        /// its containing radius.
        /// </summary>
        /// <param name="other">The <see cref="Location"/> whose smallest containing <see
        /// cref="Region"/> is to be determined.</param>
        /// <returns>
        /// The smallest child <see cref="Region"/> at any level of this instance's descendant
        /// hierarchy which fully contains the specified <see cref="Location"/> within its
        /// containing radius, or this instance, if no child contains the position.
        /// </returns>
        public Region GetContainingChild(ILocation other)
            => GetAllChildren().OfType<Region>()
            .Where(x => Vector3.Distance(x.Position, x.GetLocalizedPosition(other)) <= x.Shape.ContainingRadius - (other is Region r ? r.Shape.ContainingRadius : 0))
            .ItemWithMin(x => x.Shape.ContainingRadius)
            ?? (this is Region region && Contains(other) ? region : null);

        /// <summary>
        /// Performs the behind-the-scenes work necessary to transfer a <see cref="Location"/>
        /// to a new containing region.
        /// </summary>
        /// <param name="region">The <see cref="Region"/> which will be the new containing region of
        /// this instance; or <see langword="null"/> to remove it from the hierarchy.</param>
        /// <remarks>
        /// If the new containing region is part of the same hierarchy as this instance, its current
        /// absolute position will be preserved, and translated into the correct local relative <see
        /// cref="Position"/>. Otherwise, they will be reset to <see cref="Vector3.Zero"/>.
        /// </remarks>
        public override void SetNewContainingRegion(Region region)
        {
            base.SetNewContainingRegion(region);
            foreach (var child in region.Children.Where(x => x != this && Contains(x.Position) && (!(x is Region r) || r.Shape.ContainingRadius < Shape.ContainingRadius)))
            {
                child.SetNewContainingRegion(this);
            }
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
                    || (x is CelestialBody body && body.Shape.GetCloneAtPosition(GetLocalizedPosition(x)).Intersects(shape))))
                {
                    pos = null;
                }
                insanityCheck++;
            } while (!pos.HasValue && insanityCheck < 10000);
            position = pos ?? Vector3.Zero;
            return pos.HasValue;
        }

        internal void AddChild(ILocation location) => (_children ?? (_children = new List<ILocation>())).Add(location);

        internal void RemoveChild(ILocation location) => _children?.Remove(location);

        private protected override Stack<Region> GetPathToLocation(Stack<Region> path = null)
        {
            (path ?? (path = new Stack<Region>())).Push(this);
            return ContainingRegion?.GetPathToLocation(path) ?? path;
        }
    }
}
