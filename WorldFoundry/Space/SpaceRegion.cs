using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Troschuetz.Random;
using WorldFoundry.CelestialBodies;
using WorldFoundry.Orbits;
using WorldFoundry.Utilities;
using WorldFoundry.Utilities.MathUtil.Shapes;

namespace WorldFoundry.Space
{
    /// <summary>
    /// A discrete region of space bound together by gravity, such as a galaxy or star system.
    /// </summary>
    public class SpaceRegion : BioZone
    {
        /// <summary>
        /// The average number of children within the grid per m³.
        /// </summary>
        public virtual double ChildDensity => 0;

        /// <summary>
        /// The types of children this region of space might have.
        /// </summary>
        public virtual ICollection<SpaceChildValue> ChildPossibilities { get; private set; }

        /// <summary>
        /// Indicates any children this <see cref="SpaceRegion"/> may have which will be manually
        /// assigned, rather than generated.
        /// </summary>
        public ICollection<SpaceChildValue> ChildPresets { get; set; }

        /// <summary>
        /// The <see cref="SpaceRegion"/> children contained within this one.
        /// </summary>
        public ICollection<SpaceChild> Children { get; set; }

        /// <summary>
        /// Get the total number of children within this <see cref="SpaceRegion"/>, based on its
        /// child density.
        /// </summary>
        public ICollection<SpaceChildValue> ChildTotals { get; private set; }

        /// <summary>
        /// A collection of grid spaces within this space, which either have or have not yet been
        /// populated with children.
        /// </summary>
        private ICollection<GridSpace> GridSpaces { get; set; }

        /// <summary>
        /// Indicates that <see cref="ChildTotals"/> has been calculated.
        /// </summary>
        public bool IsChildTotalsGenerationComplete { get; set; }

        /// <summary>
        /// The shape of the <see cref="SpaceRegion"/>.
        /// </summary>
        [NotMapped]
        public override Shape Shape
        {
            get => base.Shape;
            set
            {
                base.Shape = value;

                _gridSize = null;
            }
        }

        private float? _gridSize;
        /// <summary>The size of 1 grid space within this object, in local space units.</summary>
        public float GridSize
        {
            get
            {
                if (!_gridSize.HasValue)
                {
                    _gridSize = GetGridSize();
                }
                return _gridSize ?? 0;
            }
        }

        /// <summary>
        /// Initializes a new instance of <see cref="SpaceRegion"/>.
        /// </summary>
        public SpaceRegion() { }

        /// <summary>
        /// Initializes a new instance of <see cref="SpaceRegion"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="SpaceRegion"/> in which this <see cref="SpaceRegion"/> is located.
        /// </param>
        public SpaceRegion(SpaceRegion parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="SpaceRegion"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="SpaceRegion"/> in which this <see cref="SpaceRegion"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="SpaceRegion"/>.</param>
        public SpaceRegion(SpaceRegion parent, Vector3 position) : base(parent, position) { }

        private void CalculateChildTotals()
        {
            IsChildTotalsGenerationComplete = true;

            if (!ChildPossibilities.Any())
            {
                return;
            }

            // Find the total number of children.
            double volume = Shape.GetVolume();
            double totalNumChildren = Math.Max(1, volume * ChildDensity);

            // Find the amount of each child type.
            foreach (var possibility in ChildPossibilities)
            {
                float amount = (float)Math.Round(totalNumChildren * possibility.Value);
                if (amount == 0)
                {
                    continue;
                }

                if (float.IsInfinity(amount))
                {
                    amount = float.MaxValue;
                }

                // Any existing children establish a minimum for the totals.
                amount = Math.Max(amount, Children.Where(c => c.GetType() == possibility.Type).Count());

                ChildTotals.Add(new SpaceChildValue { Type = possibility.Type, Value = amount });
            }

            // Add any presets not already accounted for in the general amounts.
            foreach (var preset in ChildPresets)
            {
                var match = ChildTotals.FirstOrDefault(c => c.Type == preset.Type);
                if (match != null)
                {
                    match.Value = Math.Max(match.Value, preset.Value);
                }
                else
                {
                    ChildTotals.Add(new SpaceChildValue { Type = preset.Type, Value = preset.Value });
                }
            }

            // Always at least 1 child if children are indicated.
            if (ChildTotals.Count == 0)
            {
                // Pick the most common type (at random, if multiple types tie for the maximum probability).
                var max = ChildPossibilities.Max(p => p.Value);
                var common = Randomizer.Static.Generator.Choice(ChildPossibilities.Where(c => c.Value == max).ToList());
                ChildTotals.Add(new SpaceChildValue { Type = common.Type, Value = max });
            }
        }

        /// <summary>
        /// Determines whether this <see cref="SpaceRegion"/> contains the <see cref="Position"/> of
        /// the specified <see cref="SpaceRegion"/>.
        /// </summary>
        /// <param name="other">The <see cref="SpaceRegion"/> to test for inclusion within this one.</param>
        /// <returns>
        /// True if this <see cref="SpaceRegion"/> contains the <see cref="Position"/> of the specified one.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="other"/> cannot be null.</exception>
        /// <remarks>
        /// If the two grids are not part of the same hierarchy (i.e. share a common parent), an
        /// exception will be thrown by a called method. Note that a large or close object may
        /// partially overlap this one's space yet still return false from this method, if its center
        /// position lies outside this object's bounds.
        /// </remarks>
        internal virtual bool ContainsSpaceGrid(SpaceRegion other)
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
                if (commonParent == null)
                {
                    return false;
                }
                var position1 = TranslateToLocalCoordinates(commonParent);
                var position2 = other.TranslateToLocalCoordinates(commonParent);
                return Shape.IsPointWithin(position1, position2);
            }
        }

        /// <summary>
        /// Selects a random unpopulated grid space within this celestial object's local space.
        /// </summary>
        /// <returns>
        /// The coordinates (1-based) of an unpopulated grid space, or the zero vector if none exist.
        /// </returns>
        private Vector3 FindUnpopulatedGridSpace()
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
        private void GenerateChild(Vector3 coordinates)
        {
            // If this object already has its full allotment of children, do nothing.
            // (Has the side effect of making sure totals have been generated.)
            if (GetChildTotals().All(t => t.Value >= GetTotalChildren(t.Type)))
            {
                return;
            }

            // Value in range -0.4–1.4, rounded to a whole number: should give 1 most of the time,
            // and 0 in some cases.
            int number = (int)Math.Round(Randomizer.Static.Normal(0.9, 0.1667), MidpointRounding.AwayFromZero);
            // If no children were indicated, there is nothing left to do.
            if (number == 0)
            {
                return;
            }

            // Adjust type probabilities based on existing counts.
            List<float> effectiveProbabilities = new List<float>();
            foreach (var possibility in ChildPossibilities)
            {
                // Some child possibilities are not realized at all, due to
                // small size and low child density eliminating children with
                // low probabilities entirely.
                float total = ChildTotals.FirstOrDefault(t => t.Type == possibility.Type)?.Value ?? 0;
                if (total == 0)
                {
                    effectiveProbabilities.Add(0);
                }
                else
                {
                    effectiveProbabilities.Add(possibility.Value * (1 - GetTotalChildren(possibility.Type) / total));
                }
            }
            float totalProbability = effectiveProbabilities.Sum();
            // If no children are indicated after adjusting, there is nothing left to do.
            if (totalProbability == 0)
            {
                return;
            }

            float ratio = 1 / totalProbability;
            for (int i = 0; i < effectiveProbabilities.Count; i++)
            {
                effectiveProbabilities[i] = effectiveProbabilities[i] / ratio;
            }

            // Select a child type and create it.
            double chance = Randomizer.Static.NextDouble();
            for (int i = 0; i < effectiveProbabilities.Count; i++)
            {
                if (chance <= effectiveProbabilities[i])
                {
                    var type = ChildPossibilities.ElementAt(i).Type;
                    var position = new Vector3(
                        (float)Math.Round((coordinates.X - 1 + Randomizer.Static.NextDouble() * Math.Sign(coordinates.X)) * GridSize, 4),
                        (float)Math.Round((coordinates.Y - 1 + Randomizer.Static.NextDouble() * Math.Sign(coordinates.Y)) * GridSize, 4),
                        (float)Math.Round((coordinates.Z - 1 + Randomizer.Static.NextDouble() * Math.Sign(coordinates.Z)) * GridSize, 4));
                    GenerateChildOfType(type, position);
                    break;
                }
                else
                {
                    chance -= effectiveProbabilities[i];
                }
            }
        }

        /// <summary>
        /// Generates a child of the specified type within this <see cref="SpaceRegion"/>.
        /// </summary>
        /// <param name="type">
        /// The type of child to generate. Does not need to be one of this object's usual child
        /// types, but must be a subclass of <see cref="SpaceRegion"/> or <see cref="CelestialBody"/>.
        /// </param>
        /// <param name="position">
        /// The location at which to generate the child. If null, a randomly-selected free space will
        /// be selected.
        /// </param>
        /// <param name="orbitParameters">
        /// An optional list of parameters which describe the child's orbit. May be null.
        /// </param>
        public virtual object GenerateChildOfType(Type type, Vector3? position, List<object> orbitParameters = null)
        {

            // If position is null, find free space.
            if (!position.HasValue)
            {
                var freeGridSpace = FindUnpopulatedGridSpace();
                if (freeGridSpace == Vector3.Zero)
                {
                    throw new Exception($"No free space in {this}.");
                }

                // Find the center of the free space.
                position = GetCenter(freeGridSpace);
            }
            // Include this as the parent parameter
            var parameters = new object[] { this, position };

            Orbiter child = null;
            if (type.IsSubclassOf(typeof(SpaceRegion)))
            {
                child = (SpaceRegion)type.InvokeMember(null, BindingFlags.CreateInstance, null, null, parameters);
            }
            else if (type.IsSubclassOf(typeof(CelestialBody)))
            {
                child = (CelestialBody)type.InvokeMember(null, BindingFlags.CreateInstance, null, null, parameters);
            }

            if (orbitParameters != null)
            {
                orbitParameters.Insert(1, child); // Insert child as orbiter parameter.
                child.Orbit = (Orbit)typeof(Orbit).InvokeMember(null, BindingFlags.CreateInstance, null, null, orbitParameters.ToArray());
            }

            return child;
        }

        /// <summary>
        /// Retrieves the position within this <see cref="SpaceRegion"/>'s local space at the center of
        /// the given grid space.
        /// </summary>
        /// <param name="coordinates">The 1-based grid coordinates.</param>
        /// <returns>A position in local space</returns>
        private Vector3 GetCenter(Vector3 coordinates)
        {
            List<Vector3> corners = GridCoordsToCornerPositions(coordinates);
            Vector3 position = Vector3.Zero;
            position.X = ((corners[1].X - corners[0].X) / 2) + corners[0].X;
            position.Y = ((corners[2].Y - corners[0].Y) / 2) + corners[0].Y;
            position.Z = ((corners[3].Z - corners[0].Z) / 2) + corners[0].Z;
            return position;
        }

        /// <summary>
        /// Gets the total number of children in this <see cref="SpaceRegion"/>, by type.
        /// </summary>
        /// <returns>The total number of children in this <see cref="SpaceRegion"/>, by type.</returns>
        public ICollection<SpaceChildValue> GetChildTotals()
        {
            if (!IsChildTotalsGenerationComplete)
            {
                CalculateChildTotals();
            }
            return ChildTotals;
        }

        /// <summary>
        /// Finds the child <see cref="SpaceRegion"/> within local space whose own <see cref="Radius"/>
        /// contains the specified position.
        /// </summary>
        /// <param name="position">The position to locate within a child.</param>
        /// <returns>
        /// The child <see cref="SpaceRegion"/> which contains the given position within its <see
        /// cref="Radius"/>, or null if no child does.
        /// </returns>
        internal virtual SpaceRegion GetContainingChild(Vector3 position)
            => Children.FirstOrDefault(c => c.GetType().IsSubclassOf(typeof(SpaceRegion)) && c.Shape.IsPointWithin(c.Position, position)) as SpaceRegion;

        /// <summary>
        /// Determines the nearest containing <see cref="Parent"/> of this <see cref="SpaceRegion"/>
        /// for which it is contained (even partially) within that <see cref="Parent"/>'s local space.
        /// </summary>
        /// <returns>
        /// The nearest containing <see cref="Parent"/> of this <see cref="SpaceRegion"/> which
        /// contains it within its local space (even partially); null if the object is outside the
        /// bounds of the hierarchy completely.
        /// </returns>
        internal SpaceRegion GetContainingParent()
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
        /// Determines the nearest containing <see cref="Parent"/> of this <see cref="SpaceRegion"/>
        /// which contains the specified position within that <see cref="Parent"/>'s own local space
        /// (possibly this object itself if the position is not out of bounds).
        /// </summary>
        /// <returns>
        /// The nearest containing <see cref="Parent"/> of this <see cref="SpaceRegion"/> which
        /// contains the position within its local space; null if the object is outside the bounds of
        /// the hierarchy completely.
        /// </returns>
        internal SpaceRegion GetContainingParent(Vector3 position)
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
        /// Retrieves the number of grid spaces from the center of local space to the edge of the grid.
        /// </summary>
        public int GetGridRange()
        {
            if (float.IsInfinity(GridSize) || float.IsNaN(GridSize))
            {
                return 1;
            }

            return (int)Math.Ceiling(localSpaceScale / GridSize);
        }

        /// <summary>
        /// Calculates the size of one grid space, based on its <see cref="ChildDensity"/>, assuming
        /// each grid space will have ~1 child on average. If the <see cref="ChildDensity"/> is 0, a
        /// grid space is the same size as the object.
        /// </summary>
        /// <returns>The size of 1 grid space within this object, in local space units.</returns>
        private float GetGridSize()
            => ChildDensity == 0
            ? (Radius ?? 0)
            : (float)(Math.Pow(1.0 / ChildDensity, 1.0 / 3.0) / LocalScale);

        private GridSpace GetGridSpace(Vector3 coordinates) => GridSpaces.Where(g => g.Coordinates == coordinates).FirstOrDefault();

        /// <summary>
        /// Finds all children in a 3x3 box of grid spaces around the given position in local space.
        /// </summary>
        /// <param name="position">The location around which to locate children.</param>
        internal IEnumerable<SpaceChild> GetNearbyChildren(Vector3 position)
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

        private float GetTotalChildren(Type type)
            => ChildPresets.Where(p => p.Type == type).Sum(p => p.Value) + Children.Count(c => c.GetType() == type);

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
            float x = (coordinates.X - 1) * GridSize;
            float y = (coordinates.Y - 1) * GridSize;
            float z = (coordinates.Z - 1) * GridSize;

            return new List<Vector3>
            {
                new Vector3(x, y, z),
                new Vector3(x + GridSize, y, z),
                new Vector3(x + GridSize, y + GridSize, z),
                new Vector3(x + GridSize, y + GridSize, z + GridSize),
                new Vector3(x + GridSize, y, z + GridSize),
                new Vector3(x, y + GridSize, z),
                new Vector3(x, y + GridSize, z + GridSize),
                new Vector3(x, y, z + GridSize)
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
        private bool IsGridSpaceInBounds(Vector3 coordinates) => (coordinates * GridSize).Length() <= localSpaceScale;

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
        private bool IsGridSpacePopulated(Vector3 coordinates) => GetGridSpace(coordinates)?.Populated ?? false;

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
            GetGridSpace(coordinates).Populated = true;

            // Only generate a child if this grid space is actually part of the region's Shape. It is
            // allowed to be marked populated above in any case in order to avoid this expensive
            // calculation in the future.
            if (GridCoordsToCornerPositions(coordinates)
                .Any(p => Shape.IsPointWithin(Vector3.Zero, p * LocalScale)))
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
        private Vector3 PositionToGridCoords(Vector3 position)
        {
            float x = position.X / GridSize;
            if (x != x % 1)
            {
                x = (float)(Math.Ceiling(Math.Abs(x)) * Math.Sign(x));
            }

            if (x == 0)
            {
                x = 1;
            }

            float y = position.Y / GridSize;
            if (y != y % 1)
            {
                y = (float)(Math.Ceiling(Math.Abs(y)) * Math.Sign(y));
            }

            if (y == 0)
            {
                y = 1;
            }

            float z = position.Z / GridSize;
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
        /// Sets the 'populated' flag for all grid coordinates overlapped (even
        /// partially) by the region defined by the given <see cref="Utilities.MathUtil.Shapes.Shape"/>.
        /// </summary>
        /// <param name="position">The center of the region to be marked as populated.</param>
        /// <param name="shape">The shape of the region to be marked as populated.</param>
        internal void SetRegionPopulated(Vector3 position, Shape shape)
        {
            Vector3 home = PositionToGridCoords(position);
            GetGridSpace(home).Populated = true;

            // If the shape is null or empty, only the grid containing the position is marked.
            if (shape == null || shape.GetVolume() == 0)
            {
                return;
            }

            // Iterate over the box which fully contains the specified shape.
            int num = (int)Math.Ceiling((shape.GetContainingRadius() / LocalScale) / GridSize);
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
                                .Any(p => shape.IsPointWithin(position * LocalScale, p * LocalScale)))
                            {
                                GetGridSpace(coordinates).Populated = true;
                            }
                        }
                    }
                }
            }
        }
    }
}
