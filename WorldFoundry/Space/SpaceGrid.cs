using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Troschuetz.Random;
using WorldFoundry.Utilities;
using WorldFoundry.Utilities.MathUtil.Shapes;

namespace WorldFoundry.Space
{
    /// <summary>
    /// Represents a region of space divided into a 3D grid based on the density of children within
    /// the region.
    /// </summary>
    public class SpaceGrid
    {
        /// <summary>
        /// Local space is a coordinate system with a range of -1000000 to 1000000.
        /// </summary>
        internal const int localSpaceScale = 1000000;

        /// <summary>
        /// The average number of children within the grid per m³.
        /// </summary>
        public virtual double ChildDensity => 0;

        /// <summary>
        /// The <see cref="SpaceGrid"/> children contained within this one.
        /// </summary>
        public ICollection<SpaceGrid> Children { get; set; }

        /// <summary>
        /// A collection of grid spaces within this space, which either have or have not yet been
        /// populated with children.
        /// </summary>
        private Dictionary<Vector3, bool> GridSpaces { get; set; } = new Dictionary<Vector3, bool>();

        /// <summary>
        /// The <see cref="SpaceGrid"/> which directly contains this one.
        /// </summary>
        public SpaceGrid Parent { get; set; }

        /// <summary>
        /// Specifies the location of the <see cref="SpaceGrid"/>'s center in the local space of its
        /// containing <see cref="Parent"/>.
        /// </summary>
        public Vector3 Position { get; set; }

        protected float? _radius;
        /// <summary>
        /// Gets a radius which fully contains this <see cref="SpaceGrid"/>, in meters.
        /// </summary>
        public float? Radius
        {
            get
            {
                if (!_radius.HasValue)
                {
                    _radius = Shape?.GetContainingRadius() ?? null;
                }
                return _radius;
            }
            set => _radius = value;
        }

        protected Shape _shape;
        /// <summary>
        /// The shape of the <see cref="SpaceGrid"/>.
        /// </summary>
        public virtual Shape Shape
        {
            get => _shape;
            set
            {
                if (_shape == value)
                {
                    return;
                }

                _radius = null;
                _shape = value;

                if (value != null)
                {
                    Parent?.SetRegionPopulated(Position, _shape);
                }
            }
        }

        /// <summary>
        /// Determines whether this <see cref="SpaceGrid"/> contains the <see cref="Position"/> of
        /// the specified <see cref="SpaceGrid"/>.
        /// </summary>
        /// <param name="other">The <see cref="SpaceGrid"/> to test for inclusion within this one.</param>
        /// <returns>
        /// True if this <see cref="SpaceGrid"/> contains the <see cref="Position"/> of the specified one.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="other"/> cannot be null.</exception>
        /// <remarks>
        /// If the two grids are not part of the same hierarchy (i.e. share a common parent), an
        /// exception will be thrown by a called method. Note that a large or close object may
        /// partially overlap this one's space yet still return false from this method, if its center
        /// position lies outside this object's bounds.
        /// </remarks>
        protected virtual bool ContainsSpaceGrid(SpaceGrid other)
        {
            if (other == null)
            {
                throw new ArgumentNullException();
            }

            // If one is the parent of the other, they overlap as long as the child's does not lie
            // entirely beyond the bounds of the parent's local space.
            if (other == Parent)
            {
                double radius = Radius ?? 0;
                return Position.Length() - radius < localSpaceScale;
            }
            else if (other.Parent == this)
            {
                double radius = other.Radius ?? 0;
                return other.Position.Length() - radius < localSpaceScale;
            }
            else
            {
                var commonParent = FindCommonParent(other);
                var position1 = TranslateToLocalCoordinates(commonParent);
                var position2 = other.TranslateToLocalCoordinates(commonParent);
                return Shape.IsPointWithin(position1, position2);
            }
        }

        /// <summary>
        /// Finds a common <see cref="SpaceGrid"/> parent with the specified one.
        /// </summary>
        /// <param name="other">A <see cref="SpaceGrid"/> with which to find a common parent.</param>
        /// <returns>
        /// The <see cref="SpaceGrid"/> which is the common parent of this one and the specified
        /// one. Null if none exists.
        /// </returns>
        public SpaceGrid FindCommonParent(SpaceGrid other)
        {
            var otherPath = other.GetTreePathToSpaceGrid().ToList();
            return GetTreePathToSpaceGrid().TakeWhile((o, i) => otherPath.Count > i && o == otherPath[i]).LastOrDefault();
        }

        /// <summary>
        /// Selects a random unpopulated grid space within this celestial object's local space.
        /// </summary>
        /// <returns>
        /// The coordinates (1-based) of an unpopulated grid space, or the zero vector if none exist.
        /// </returns>
        protected Vector3 FindUnpopulatedGridSpace()
        {
            var unpopulatedSpaces = new List<Vector3>();
            int gridRange = GetGridRange();
            for (int x = 1; x <= gridRange; x++)
            {
                for (int y = 1; y <= gridRange; y++)
                {
                    // Avoid over-representing a single slice of X by moving to
                    // the next X value after enough unpopulated spaces have
                    // been found here.
                    if (y >= 1000 && unpopulatedSpaces.Count >= 1000)
                    {
                        break;
                    }

                    for (int z = 1; z <= gridRange; z++)
                    {
                        // Avoid over-representing a single slice of Y by moving to
                        // the next Y value after enough unpopulated spaces have
                        // been found here.
                        if (z >= 1000 && unpopulatedSpaces.Count >= 1000)
                        {
                            break;
                        }

                        var currentGridCoordinates = new Vector3(x, y, z);
                        if (!IsGridSpacePopulated(currentGridCoordinates))
                        {
                            unpopulatedSpaces.Add(currentGridCoordinates);
                        }

                        // To prevent searching too long, stop and pick one
                        // once there are a great many.
                        if (unpopulatedSpaces.Count >= 10000)
                        {
                            return Randomizer.Static.Choice(unpopulatedSpaces);
                        }
                    }
                }
            }
            if (unpopulatedSpaces.Count == 0)
            {
                return Vector3.Zero;
            }

            return Randomizer.Static.Choice(unpopulatedSpaces);
        }

        /// <summary>
        /// Generate a child at the given grid coordinates.
        /// </summary>
        /// <param name="coordinates">The 1-based grid coordinates at which to generate a child.</param>
        /// <remarks>Does nothing on the base class.</remarks>
        protected virtual void GenerateChild(Vector3 coordinates) { }

        /// <summary>
        /// Retrieves the position within this <see cref="SpaceGrid"/>'s local space at the center of
        /// the given grid space.
        /// </summary>
        /// <param name="coordinates">The 1-based grid coordinates.</param>
        /// <returns>A position in local space</returns>
        protected Vector3 GetCenter(Vector3 coordinates)
        {
            List<Vector3> corners = GridCoordsToCornerPositions(coordinates);
            Vector3 position = Vector3.Zero;
            position.X = ((corners[1].X - corners[0].X) / 2) + corners[0].X;
            position.Y = ((corners[2].Y - corners[0].Y) / 2) + corners[0].Y;
            position.Z = ((corners[3].Z - corners[0].Z) / 2) + corners[0].Z;
            return position;
        }

        /// <summary>
        /// Finds the child <see cref="SpaceGrid"/> within local space whose own <see cref="Radius"/>
        /// contains the specified position.
        /// </summary>
        /// <param name="position">The position to locate within a child.</param>
        /// <returns>
        /// The child <see cref="SpaceGrid"/> which contains the given position within its <see
        /// cref="Radius"/>, or null if no child does.
        /// </returns>
        internal virtual SpaceGrid GetContainingChild(Vector3 position) => Children.FirstOrDefault(c => c.Shape.IsPointWithin(c.Position, position));

        /// <summary>
        /// Determines the nearest containing <see cref="Parent"/> of this <see cref="SpaceGrid"/>
        /// for which it is contained (even partially) within that <see cref="Parent"/>'s local space.
        /// </summary>
        /// <returns>
        /// The nearest containing <see cref="Parent"/> of this <see cref="SpaceGrid"/> which
        /// contains it within its local space (even partially); null if the object is outside the
        /// bounds of the hierarchy completely.
        /// </returns>
        internal SpaceGrid GetContainingParent()
        {
            var currentReference = Parent;
            while (currentReference != null)
            {
                if (ContainsSpaceGrid(currentReference))
                {
                    break;
                }

                currentReference = currentReference.Parent;
            }
            return currentReference;
        }

        /// <summary>
        /// Determines the nearest containing <see cref="Parent"/> of this <see cref="SpaceGrid"/>
        /// which contains the specified position within that <see cref="Parent"/>'s own local space
        /// (possibly this object itself if the position is not out of bounds).
        /// </summary>
        /// <returns>
        /// The nearest containing <see cref="Parent"/> of this <see cref="SpaceGrid"/> which
        /// contains the position within its local space; null if the object is outside the bounds of
        /// the hierarchy completely.
        /// </returns>
        internal SpaceGrid GetContainingParent(Vector3 position)
        {
            var currentReference = this;
            while (currentReference != null)
            {
                if (position.Length() <= localSpaceScale)
                {
                    break;
                }

                if (currentReference.Parent != null)
                {
                    position = currentReference.TranslateToParentCoordinates(position);
                }

                currentReference = currentReference.Parent;
            }
            return currentReference;
        }

        /// <summary>
        /// Calculates the distance (in meters) between the given position in local space to the
        /// center of the specified <see cref="SpaceGrid"/>.
        /// </summary>
        /// <param name="position">The position from which to calculate the distance.</param>
        /// <param name="other">
        /// The <see cref="SpaceGrid"/> whose distance to this one is to be determined.
        /// </param>
        /// <returns>
        /// The distance (in local space units) between this <see cref="SpaceGrid"/> and the
        /// specified one.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="other"/> cannot be null.</exception>
        /// <exception cref="Exception">
        /// The two <see cref="SpaceGrid"/>s must be part of the same hierarchy (i.e. share a common parent).
        /// </exception>
        protected float GetDistanceFromPositionToGrid(Vector3 position, SpaceGrid other)
        {
            if (other == null)
            {
                throw new ArgumentNullException();
            }

            if (other == Parent)
            {
                return position.Length() * other.GetLocalScale();
            }
            else if (other.Parent == this)
            {
                return other.Position.Length() * GetLocalScale();
            }
            else if (Parent == other.Parent)
            {
                return (other.Position - position).Length() * Parent.GetLocalScale();
            }
            else
            {
                var commonParent = FindCommonParent(other);
                if (commonParent == null)
                {
                    throw new Exception($"{this} and {other} are not part of the same hierarchy");
                }

                return Vector3.Distance(TranslateToLocalCoordinates(commonParent), other.TranslateToLocalCoordinates(commonParent)) * commonParent.GetLocalScale();
            }
        }

        /// <summary>
        /// Calculates the distance (in meters) between the centers of this <see cref="SpaceGrid"/>
        /// and the specified one.
        /// </summary>
        /// <param name="other">
        /// The <see cref="SpaceGrid"/> whose distance to this one is to be determined.
        /// </param>
        /// <returns>
        /// The distance (in local space units) between this <see cref="SpaceGrid"/> and the
        /// specified one.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="other"/> cannot be null.</exception>
        /// <exception cref="Exception">
        /// The two <see cref="SpaceGrid"/>s must be part of the same hierarchy (i.e. share a common parent).
        /// </exception>
        protected float GetDistanceToGrid(SpaceGrid other) => GetDistanceFromPositionToGrid(Position, other);

        /// <summary>
        /// Retrieves the number of grid spaces from the center of local space to the edge of the grid.
        /// </summary>
        public int GetGridRange()
        {
            float gridSize = GetGridSize();
            if (float.IsInfinity(gridSize) || float.IsNaN(gridSize))
            {
                return 1;
            }

            return (int)Math.Ceiling(localSpaceScale / gridSize);
        }

        /// <summary>
        /// Calculates the size of one grid space, based on its <see cref="ChildDensity"/>, assuming
        /// each grid space will have ~1 child on average. If the <see cref="ChildDensity"/> is 0, a
        /// grid space is the same size as the object.
        /// </summary>
        /// <returns>The size of 1 grid space within this object, in local space units.</returns>
        public float GetGridSize()
            => ChildDensity == 0
            ? (Radius ?? 0)
            : (float)(Math.Pow(1.0 / ChildDensity, 1.0 / 3.0) / GetLocalScale());

        /// <summary>
        /// Returns the size of 1 unit of local space within this <see cref="SpaceGrid"/>, in meters.
        /// </summary>
        internal float GetLocalScale() => (Radius ?? 0) / localSpaceScale;

        /// <summary>
        /// Finds all children in a 3x3 box of grid spaces around the given position in local space.
        /// </summary>
        /// <param name="position">The location around which to locate children.</param>
        public IEnumerable<SpaceGrid> GetNearbyChildren(Vector3 position)
        {
            Vector3 coordinates = PositionToGridCoords(position);
            foreach (var child in Children)
            {
                var childCoordinates = PositionToGridCoords(child.Position);
                if (childCoordinates.X < coordinates.X - 1 || childCoordinates.X > coordinates.X + 1 ||
                    childCoordinates.Y < coordinates.Y - 1 || childCoordinates.Y > coordinates.Y + 1 ||
                    childCoordinates.Z < coordinates.Z - 1 || childCoordinates.Z > coordinates.Z + 1)
                {
                    continue;
                }

                yield return child;
            }
        }

        /// <summary>
        /// Recursively builds an ordered list of <see cref="SpaceGrid"/><see cref="Parent"/>s from
        /// the topmost to this one.
        /// </summary>
        /// <param name="path">
        /// An ordered stack of one branch of this <see cref="SpaceGrid"/>'s children (which started
        /// the call to this method).
        /// </param>
        /// <returns>
        /// An ordered stack of <see cref="SpaceGrid"/> parents and children from the topmost to a
        /// particular <see cref="SpaceGrid"/>.
        /// </returns>
        private Stack<SpaceGrid> GetTreePathToSpaceGrid(Stack<SpaceGrid> path = null)
        {
            if (path == null)
            {
                path = new Stack<SpaceGrid>();
            }
            path.Push(this);
            return Parent != null ? Parent.GetTreePathToSpaceGrid(path) : path;
        }

        /// <summary>
        /// Converts set of grid coordinates to a collection of corner positions.
        /// </summary>
        /// <remarks>Grid space is 1-based, not 0-based.</remarks>
        /// <param name="position">The grid space coordinates to convert.</param>
        /// <returns>
        /// A list of the positions of the four corners of the grid space within local space.
        /// </returns>
        private List<Vector3> GridCoordsToCornerPositions(Vector3 coordinates)
        {
            float gridSize = GetGridSize();

            float x = (coordinates.X - 1) * gridSize;
            float y = (coordinates.Y - 1) * gridSize;
            float z = (coordinates.Z - 1) * gridSize;

            return new List<Vector3>
            {
                new Vector3(x, y, z),
                new Vector3(x + gridSize, y, z),
                new Vector3(x + gridSize, y + gridSize, z),
                new Vector3(x + gridSize, y + gridSize, z + gridSize),
                new Vector3(x + gridSize, y, z + gridSize),
                new Vector3(x, y + gridSize, z),
                new Vector3(x, y + gridSize, z + gridSize),
                new Vector3(x, y, z + gridSize)
            };
        }

        /// <summary>
        /// Determines whether the specified grid space coordinates are within its bounds.
        /// </summary>
        /// <param name="coordinates">The 1-based grid space coordinates to check.</param>
        /// <returns>
        /// true if the specified grid space coordinates are in the bounds of this object; false otherwise.
        /// </returns>
        /// <remarks>
        /// If even the corner closest to the origin (the defining corner) is inside or tangent to
        /// the defined limit of local space, the grid space is considered in-bounds.
        /// </remarks>
        private bool IsGridSpaceInBounds(Vector3 coordinates) => (coordinates * GetGridSize()).Length() <= localSpaceScale;

        /// <summary>
        /// Determines if the specified grid coordinates have already been populated with children.
        /// </summary>
        /// <remarks>
        /// A true response does not necessarily mean that the grid space actually contains a child,
        /// only that it has finished the population process (which might have resulted in no actual
        /// children for that grid space).
        /// </remarks>
        /// <param name="coordinates">The 1-based grid coordinates to check.</param>
        /// <returns>
        /// true if the specified grid coordinates have already been populated with child objects;
        /// false otherwise.
        /// </returns>
        private bool IsGridSpacePopulated(Vector3 coordinates) => GridSpaces.ContainsKey(coordinates) && GridSpaces[coordinates];

        /// <summary>
        /// Generates an appropriate population of children within a 3x3 box of grid spaces around
        /// the given grid space in local space.
        /// </summary>
        /// <remarks>
        /// By definition, a grid space is the volume in which ChildDensity indicates ~1 child should
        /// be found, but this is randomized to between 0 and 2 children in a normal distribution
        /// (usually 1, but sometimes 0 and sometimes 2).
        /// </remarks>
        /// <param name="coordinates">
        /// The coordinates of the central grid space around which to populate a 3x3 box.
        /// </param>
        private void Populate3x3GridRegion(Vector3 coordinates)
        {
            for (int x = coordinates.X == 1 ? -1 : (int)Math.Round(coordinates.X - 1);
                x <= (coordinates.X == -1 ? 1 : coordinates.X + 1); x++)
            {
                if (x == 0)
                {
                    continue;
                }

                for (int y = coordinates.Y == 1 ? -1 : (int)Math.Round(coordinates.Y - 1);
                    y <= (coordinates.Y == -1 ? 1 : coordinates.Y + 1); y++)
                {
                    if (y == 0)
                    {
                        continue;
                    }

                    for (int z = coordinates.Z == 1 ? -1 : (int)Math.Round(coordinates.Z - 1);
                        z <= (coordinates.Z == -1 ? 1 : coordinates.Z + 1); z++)
                    {
                        if (z == 0)
                        {
                            continue;
                        }

                        PopulateGridSpace(new Vector3(x, y, z));
                    }
                }
            }
        }

        /// <summary>
        /// Generates an appropriate population of children within the given grid space in local space.
        /// </summary>
        /// <remarks>
        /// By definition, a grid space is the volume in which ChildDensity indicates ~1 child should
        /// be found, but this is randomized to between 0 and 2 children in a normal distribution
        /// (usually 1, but sometimes 0 and sometimes 2).
        /// </remarks>
        /// <param name="coordinates">The grid space to populate.</param>
        private void PopulateGridSpace(Vector3 coordinates)
        {
            // If the grid space is not within local space, or has already been populated, do nothing.
            if (!IsGridSpaceInBounds(coordinates) || IsGridSpacePopulated(coordinates))
            {
                return;
            }

            // Mark the grid space as populated.
            GridSpaces[coordinates] = true;

            // Only generate a child if this grid space is actually part of the region's Shape. It is
            // allowed to be marked populated above in any case in order to avoid this expensive
            // calculation in the future.
            if (GridCoordsToCornerPositions(coordinates)
                .Any(p => Shape.IsPointWithin(Vector3.Zero, p * GetLocalScale())))
            {
                GenerateChild(coordinates);
            }
        }

        /// <summary>
        /// Generates an appropriate population of child objects in local space, in an area around
        /// the given position.
        /// </summary>
        /// <remarks>
        /// Will generate approximately 25–30 children in a box around the given position. Will not
        /// re-generate or over-populate regions which have already been populated (uses an
        /// internally-managed grid system).
        /// </remarks>
        /// <param name="position">The location around which to generate child objects.</param>
        public void PopulateRegion(Vector3 position) => Populate3x3GridRegion(PositionToGridCoords(position));

        /// <summary>
        /// Converts a position to a set of grid coordinates.
        /// </summary>
        /// <remarks>Grid space is 1-based, not 0-based.</remarks>
        /// <param name="position">The position to convert.</param>
        /// <returns>
        /// The coordinates of the grid space where the position is located within local space.
        /// </returns>
        internal Vector3 PositionToGridCoords(Vector3 position)
        {
            float gridSize = GetGridSize();

            float x = position.X / gridSize;
            if (x != x % 1)
            {
                x = (float)(Math.Ceiling(Math.Abs(x)) * Math.Sign(x));
            }

            if (x == 0)
            {
                x = 1;
            }

            float y = position.Y / gridSize;
            if (y != y % 1)
            {
                y = (float)(Math.Ceiling(Math.Abs(y)) * Math.Sign(y));
            }

            if (y == 0)
            {
                y = 1;
            }

            float z = position.Z / gridSize;
            if (z != z % 1)
            {
                z = (float)(Math.Ceiling(Math.Abs(z)) * Math.Sign(z));
            }

            if (z == 0)
            {
                z = 1;
            }

            return new Vector3(x, y, z);
        }

        /// <summary>
        /// Performs the behind-the-scenes work necessary to transfer a <see cref="SpaceGrid"/> to a
        /// new <see cref="Parent"/> in the hierarchy.
        /// </summary>
        /// <remarks>
        /// If the new parent is part of this <see cref="SpaceGrid"/>'s current parent hierarchy, the
        /// current coordinates will be preserved, and translated into the correct local space.
        /// Otherwise, they will be reset to 0,0,0.
        /// </remarks>
        /// <param name="newParent">The <see cref="SpaceGrid"/> which will be this one's new parent.</param>
        internal void SetNewParent(SpaceGrid newParent)
        {
            Position = GetTreePathToSpaceGrid().Contains(newParent)
                ? TranslateToLocalCoordinates(newParent)
                : Vector3.Zero;

            Parent = newParent;
        }

        /// <summary>
        /// Sets the 'populated' flag for all grid coordinates overlapped (even
        /// partially) by the region defined by the given <see cref="Utilities.MathUtil.Shapes.Shape"/>.
        /// </summary>
        /// <param name="position">The center of the region to be marked as populated.</param>
        /// <param name="shape">The shape of the region to be marked as populated.</param>
        protected void SetRegionPopulated(Vector3 position, Shape shape)
        {
            Vector3 home = PositionToGridCoords(position);
            GridSpaces[home] = true;

            // If the shape is null or empty, only the grid containing the position is marked.
            if (shape == null || shape.GetVolume() == 0)
            {
                return;
            }

            // Iterate over the box which fully contains the specified shape.
            float gridSize = GetGridSize();
            float localScale = GetLocalScale();
            int num = (int)Math.Ceiling((shape.GetContainingRadius() / localScale) / gridSize);
            for (int x = -num; x <= num; x++)
            {
                if (x == 0)
                {
                    continue;
                }

                for (int y = -num; y <= num; y++)
                {
                    if (y == 0)
                    {
                        continue;
                    }

                    for (int z = -num; z <= num; z++)
                    {
                        if (z == 0)
                        {
                            continue;
                        }

                        Vector3 coordinates = new Vector3(home.X + x, home.Y + y, home.Z + z);
                        // For in-bounds grids, test all four corners for inclusion in the shape.
                        if (IsGridSpaceInBounds(coordinates))
                        {
                            if (GridCoordsToCornerPositions(coordinates)
                                .Any(p => shape.IsPointWithin(position * localScale, p * localScale)))
                            {
                                GridSpaces[coordinates] = true;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Translates <see cref="Position"/> into an equivalent position in the local space of the
        /// specified grid.
        /// </summary>
        /// <param name="other">
        /// The grid into whose local space this one's <see cref="Position"/> is to be translated.
        /// </param>
        /// <param name="position">The position in local space to translate.</param>
        /// <returns>
        /// A <see cref="Vector3"/> giving the position of this grid in the local space of the
        /// specified one.
        /// </returns>
        /// <exception cref="ArgumentNullException">Other cannot be null.</exception>
        /// <exception cref="NullReferenceException">
        /// If this grid has no <see cref="Parent"/>, an exception will be thrown.
        /// </exception>
        /// <exception cref="Exception"><paramref name="other"/> must be in the same hierarchy as this object.</exception>
        public Vector3 TranslateToLocalCoordinates(SpaceGrid other, Vector3 position)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            if (other == this)
            {
                return position; // Nothing to do; it's already the local space
            }

            // Get the position in the local space of the common parent.
            var commonParent = FindCommonParent(other);
            if (commonParent == null)
            {
                throw new Exception($"{other} is not in the same hierarchy as {this}");
            }

            var currentReference = this;
            while (currentReference != commonParent)
            {
                position = currentReference.TranslateToParentCoordinates(position);
                currentReference = currentReference.Parent;
            }

            // If other is one of this object's hierarchical parents, simply return its translated position.
            if (currentReference == other)
            {
                return position;
            }

            var otherPosition = other.Position;
            var otherParent = other.Parent;
            while (otherParent != currentReference)
            {
                otherPosition = otherParent.TranslateToParentCoordinates(otherPosition);
                otherParent = otherParent.Parent;
            }
            return ((position - otherPosition) * GetLocalScale()) / other.GetLocalScale();
        }

        /// <summary>
        /// Translates <see cref="Position"/> into an equivalent position in the local space of the
        /// specified grid.
        /// </summary>
        /// <param name="other">
        /// The grid into whose local space this one's <see cref="Position"/> is to be translated.
        /// </param>
        /// <returns>
        /// A <see cref="Vector3"/> giving the position of this grid in the local space of the
        /// specified one.
        /// </returns>
        /// <exception cref="ArgumentNullException">Other cannot be null.</exception>
        /// <exception cref="NullReferenceException">
        /// If this grid has no <see cref="Parent"/>, an exception will be thrown.
        /// </exception>
        /// <exception cref="Exception"><paramref name="other"/> must be in the same hierarchy as this object.</exception>
        public Vector3 TranslateToLocalCoordinates(SpaceGrid other) => TranslateToLocalCoordinates(other, Position);

        /// <summary>
        /// Translates the specified coordinates in local space into the local space of <see cref="Parent"/>.
        /// </summary>
        /// <returns>A <see cref="Vector3"/> giving a position in the local space of <see cref="Parent"/>.</returns>
        /// <exception cref="NullReferenceException">
        /// If this grid has no <see cref="Parent"/>, an exception will be thrown.
        /// </exception>
        public Vector3 TranslateToParentCoordinates(Vector3 position)
        {
            if (Parent == null)
            {
                throw new NullReferenceException($"{this} has no parent");
            }

            float ratio = GetLocalScale() / Parent.GetLocalScale();
            position *= ratio;
            return Position + position;
        }
    }
}
