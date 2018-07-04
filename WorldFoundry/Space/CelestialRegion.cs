using ExtensionLib;
using MathAndScience.MathUtil.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Troschuetz.Random;
using WorldFoundry.CelestialBodies;
using WorldFoundry.Orbits;

namespace WorldFoundry.Space
{
    /// <summary>
    /// A discrete region of space bound together by gravity, but not a single body; such as a galaxy or star system.
    /// </summary>
    public class CelestialRegion : Orbiter
    {
        /// <summary>
        /// The types of children this region of space might have.
        /// </summary>
        public virtual IList<(Type type, float proportion, object[] constructorParameters)> ChildPossibilities => null;

        private const double childDensity = 0;
        /// <summary>
        /// The average number of children within the grid per m³.
        /// </summary>
        public virtual double ChildDensity => childDensity;

        /// <summary>
        /// The <see cref="CelestialRegion"/> children contained within this one.
        /// </summary>
        public IList<CelestialEntity> Children { get; set; }

        private Dictionary<string, float> _childTotals;
        /// <summary>
        /// Get the total number of children within this <see cref="CelestialRegion"/>, based on its
        /// child density.
        /// </summary>
        public IDictionary<string, float> ChildTotals
        {
            get => GetProperty(ref _childTotals, CalculateChildTotals);
            private set => _childTotals = value as Dictionary<string, float>;
        }

        private Dictionary<Vector3, bool> _gridSpaces;
        /// <summary>
        /// A collection of grid spaces within this space, which either have or have not yet been
        /// populated with children.
        /// </summary>
        internal IDictionary<Vector3, bool> GridSpaces
        {
            get
            {
                if (_gridSpaces == null)
                {
                    _gridSpaces = new Dictionary<Vector3, bool>();
                }
                return _gridSpaces;
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
        /// Initializes a new instance of <see cref="CelestialRegion"/>.
        /// </summary>
        public CelestialRegion() : base() { }

        /// <summary>
        /// Initializes a new instance of <see cref="CelestialRegion"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="CelestialRegion"/> is located.
        /// </param>
        public CelestialRegion(CelestialRegion parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="CelestialRegion"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="CelestialRegion"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="CelestialRegion"/>.</param>
        public CelestialRegion(CelestialRegion parent, Vector3 position) : base(parent, position) { }

        private void CalculateChildTotals()
        {
            if (_childTotals == null)
            {
                _childTotals = new Dictionary<string, float>();
            }

            if (!(ChildPossibilities?.Any() ?? false))
            {
                return;
            }

            // Find the total number of children.
            var volume = Substance?.Shape?.Volume ?? 0;
            var totalNumChildren = Math.Max(1, volume * ChildDensity);

            // Find the amount of each child type.
            foreach (var (type, proportion, constructorParameters) in ChildPossibilities)
            {
                var amount = (float)Math.Round(totalNumChildren * proportion);
                if (amount == 0)
                {
                    continue;
                }

                if (float.IsInfinity(amount))
                {
                    amount = float.MaxValue;
                }

                // Any existing children establish a minimum for the totals.
                amount = Math.Max(amount, Children?.Where(c => c.GetType() == type).Count() ?? 0);

                if (!ChildTotals.TryGetValue(type.FullName, out var current))
                {
                    current = 0;
                }
                ChildTotals[type.FullName] = current + amount;
            }

            // Add any existing children not included in the possibilities.
            foreach (var childType in Children.Select(x => x.GetType()).Distinct().Where(x => !ChildPossibilities.Any(y => y.type == x)))
            {
                var amount = Children.Count(x => x.GetType() == childType);
                if (amount == 0)
                {
                    continue;
                }

                ChildTotals[childType.FullName] = amount;
            }

            // Always at least 1 child if children are indicated.
            if (ChildTotals.Count == 0)
            {
                // Pick the most common type (at random, if multiple types tie for the maximum probability).
                var max = ChildPossibilities.Max(p => p.proportion);
                var (type, _, _) = Randomizer.Static.Generator.Choice(ChildPossibilities.Where(c => c.proportion == max).ToList());
                ChildTotals[type.FullName] = max;
            }
        }

        /// <summary>
        /// Determines whether this <see cref="CelestialRegion"/> contains the <see cref="CelestialEntity.Position"/> of
        /// the specified <see cref="CelestialRegion"/>.
        /// </summary>
        /// <param name="other">The <see cref="CelestialRegion"/> to test for inclusion within this one.</param>
        /// <returns>
        /// True if this <see cref="CelestialRegion"/> contains the <see cref="CelestialEntity.Position"/> of the specified one.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="other"/> cannot be null.</exception>
        /// <remarks>
        /// If the two grids are not part of the same hierarchy (i.e. share a common parent), an
        /// exception will be thrown by a called method. Note that a large or close object may
        /// partially overlap this one's space yet still return false from this method, if its center
        /// position lies outside this object's bounds.
        /// </remarks>
        internal virtual bool ContainsObject(CelestialRegion other)
        {
            if (other == null)
            {
                throw new ArgumentNullException();
            }

            if (Substance?.Shape == null)
            {
                return false;
            }

            // If one is the parent of the other, they overlap as long as the child's does not lie
            // entirely beyond the bounds of the parent's local space.
            if (other == Parent)
            {
                return Position.Length() - (Radius / LocalScale) < LocalSpaceScale;
            }
            else if (other.Parent == this)
            {
                return other.Position.Length() - (other.Radius / LocalScale) < LocalSpaceScale;
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
                return Substance.Shape.IsPointWithin(position1, position2);
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
            var gridRange = GetGridRange();
            for (var x = -gridRange; x <= gridRange; x++)
            {
                if (x == 0)
                {
                    continue;
                }
                for (var y = -gridRange; y <= gridRange; y++)
                {
                    if (y == 0)
                    {
                        continue;
                    }
                    // Avoid over-representing a single slice of X by moving to
                    // the next X value after enough unpopulated spaces have
                    // been found here.
                    if (y >= 1000 && unpopulatedSpaces.Count >= 1000)
                    {
                        break;
                    }

                    for (var z = -gridRange; z <= gridRange; z++)
                    {
                        if (z == 0)
                        {
                            continue;
                        }
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
            if (ChildTotals.All(t => t.Value >= GetTotalChildren(t.Key)))
            {
                return;
            }

            // Value in range -0.4–1.4, rounded to a whole number: should give 1 most of the time,
            // and 0 in some cases.
            var number = (int)Math.Round(Randomizer.Static.Normal(0.9, 0.1667), MidpointRounding.AwayFromZero);
            // If no children were indicated, there is nothing left to do.
            if (number == 0)
            {
                return;
            }

            // Adjust type probabilities based on existing counts.
            var effectiveProbabilityTypes = new List<Type>();
            var effectiveProbabilities =
                new Dictionary<Type, (float proportion, object[] constructorParameters)>();
            if (ChildPossibilities != null)
            {
                foreach (var (type, proportion, constructorParameters) in ChildPossibilities)
                {
                    // Some child possibilities are not realized at all, due to
                    // small size and low child density eliminating children with
                    // low probabilities entirely.
                    var total = ChildTotals.FirstOrNull(t => t.Key == type.FullName)?.Value ?? 0;
                    if (total > 0)
                    {
                        effectiveProbabilityTypes.Add(type);
                        effectiveProbabilities.Add(
                            type,
                            (proportion * (1 - GetTotalChildren(type.FullName) / total),
                            constructorParameters));
                    }
                }
            }
            var totalProbability = effectiveProbabilities.Sum(p => p.Value.proportion);
            // If no children are indicated after adjusting, there is nothing left to do.
            if (totalProbability == 0)
            {
                return;
            }

            var ratio = 1 / totalProbability;
            foreach (var type in effectiveProbabilityTypes)
            {
                effectiveProbabilities[type] =
                    (effectiveProbabilities[type].proportion / ratio,
                    effectiveProbabilities[type].constructorParameters);
            }

            // Select a child type and create it.
            var chance = Randomizer.Static.NextDouble();
            foreach (var probability in effectiveProbabilities)
            {
                if (chance <= probability.Value.proportion)
                {
                    var type = probability.Key;
                    var position = new Vector3(
                        (float)Math.Round((coordinates.X - 1 + Randomizer.Static.NextDouble() * Math.Sign(coordinates.X)) * GridSize, 4),
                        (float)Math.Round((coordinates.Y - 1 + Randomizer.Static.NextDouble() * Math.Sign(coordinates.Y)) * GridSize, 4),
                        (float)Math.Round((coordinates.Z - 1 + Randomizer.Static.NextDouble() * Math.Sign(coordinates.Z)) * GridSize, 4));
                    GenerateChildOfType(type, position, probability.Value.constructorParameters);
                    break;
                }
                else
                {
                    chance -= probability.Value.proportion;
                }
            }
        }

        /// <summary>
        /// Generates a child of the specified type within this <see cref="CelestialRegion"/>.
        /// </summary>
        /// <param name="type">
        /// The type of child to generate. Does not need to be one of this object's usual child
        /// types, but must be a subclass of <see cref="Orbiter"/>.
        /// </param>
        /// <param name="position">
        /// The location at which to generate the child. If null, a randomly-selected free space will
        /// be selected.
        /// </param>
        /// <param name="constructorParameters">
        /// A list of parameters with which to call the child's constructor. May be null.
        /// </param>
        public virtual Orbiter GenerateChildOfType(Type type, Vector3? position, object[] constructorParameters)
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
            var parameters = new List<object> { this, position };
            if (constructorParameters != null)
            {
                parameters.AddRange(constructorParameters);
            }

            Orbiter child = null;
            if (type.IsSubclassOf(typeof(CelestialRegion)))
            {
                child = (CelestialRegion)type.InvokeMember(null, BindingFlags.CreateInstance, null, null, parameters.ToArray());
            }
            else if (type.IsSubclassOf(typeof(CelestialBody)))
            {
                child = (CelestialBody)type.InvokeMember(null, BindingFlags.CreateInstance, null, null, parameters.ToArray());
            }

            return child;
        }

        /// <summary>
        /// Retrieves the position within this <see cref="CelestialRegion"/>'s local space at the center of
        /// the given grid space.
        /// </summary>
        /// <param name="coordinates">The 1-based grid coordinates.</param>
        /// <returns>A position in local space</returns>
        private Vector3 GetCenter(Vector3 coordinates)
        {
            var corners = GridCoordsToCornerPositions(coordinates);
            var position = Vector3.Zero;
            position.X = ((corners[1].X - corners[0].X) / 2) + corners[0].X;
            position.Y = ((corners[2].Y - corners[0].Y) / 2) + corners[0].Y;
            position.Z = ((corners[3].Z - corners[0].Z) / 2) + corners[0].Z;
            return position;
        }

        /// <summary>
        /// Populates the region around a set of grid coordinates, then enumerates the children in
        /// that grid space.
        /// </summary>
        /// <param name="coordinates">A set of 1-based grid coordinates to search.</param>
        /// <returns>
        /// An enumeration of the child <see cref="CelestialEntity"/> objects in the given grid space.
        /// </returns>
        public IEnumerable<CelestialEntity> GetChildrenAtCoordinates(Vector3 coordinates)
        {
            Populate3x3GridRegion(coordinates);
            if (Children != null)
            {
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
        }

        /// <summary>
        /// Finds the child <see cref="CelestialRegion"/> within local space whose own radius
        /// contains the specified position.
        /// </summary>
        /// <param name="position">The position to locate within a child.</param>
        /// <returns>
        /// The child <see cref="CelestialRegion"/> which contains the given position within its
        /// radius, or null if no child does.
        /// </returns>
        public CelestialRegion GetContainingChild(Vector3 position)
            => Children?.FirstOrDefault(c => c.GetType().IsSubclassOf(typeof(CelestialRegion)) && (c.Substance?.Shape?.IsPointWithin(c.Position, position) ?? false)) as CelestialRegion;

        /// <summary>
        /// Determines the nearest containing <see cref="CelestialEntity.Parent"/> of this <see cref="CelestialRegion"/>
        /// for which it is contained (even partially) within that <see cref="CelestialEntity.Parent"/>'s local space.
        /// </summary>
        /// <returns>
        /// The nearest containing <see cref="CelestialEntity.Parent"/> of this <see cref="CelestialRegion"/> which
        /// contains it within its local space (even partially); null if the object is outside the
        /// bounds of the hierarchy completely.
        /// </returns>
        public CelestialRegion GetContainingParent()
        {
            var currentReference = Parent;
            while (currentReference != null)
            {
                if (ContainsObject(currentReference))
                {
                    break;
                }

                currentReference = currentReference.Parent;
            }
            return currentReference;
        }

        /// <summary>
        /// Determines the nearest containing <see cref="CelestialEntity.Parent"/> of this <see cref="CelestialRegion"/>
        /// which contains the specified position within that <see cref="CelestialEntity.Parent"/>'s own local space
        /// (possibly this object itself if the position is not out of bounds).
        /// </summary>
        /// <returns>
        /// The nearest containing <see cref="CelestialEntity.Parent"/> of this <see cref="CelestialRegion"/> which
        /// contains the position within its local space; null if the object is outside the bounds of
        /// the hierarchy completely.
        /// </returns>
        public CelestialRegion GetContainingParent(Vector3 position)
        {
            var currentReference = this;
            while (currentReference != null)
            {
                if (position.Length() <= LocalSpaceScale)
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
        /// Retrieves the approximate total number of grid spaces in local space.
        /// </summary>
        public int GetGridNumber()
        {
            if (double.IsInfinity(GridSize) || double.IsNaN(GridSize))
            {
                return 1;
            }

            return (int)Math.Ceiling(Substance.Shape.Volume / Math.Pow(GridSize * LocalScale, 3));
        }

        /// <summary>
        /// Retrieves the number of grid spaces from the center of local space to the edge of the grid.
        /// </summary>
        public int GetGridRange()
        {
            if (double.IsInfinity(GridSize) || double.IsNaN(GridSize))
            {
                return 1;
            }

            return (int)Math.Ceiling(LocalSpaceScale / GridSize);
        }

        /// <summary>
        /// Calculates the size of one grid space, based on its <see cref="ChildDensity"/>, assuming
        /// each grid space will have ~1 child on average. If the <see cref="ChildDensity"/> is 0, a
        /// grid space is the same size as the object.
        /// </summary>
        /// <returns>The size of 1 grid space within this object, in local space units.</returns>
        private float GetGridSize() =>
            LocalScale == 0
                ? (float)Radius
                : (ChildDensity == 0
                    ? (float)(Radius / LocalScale)
                    : (float)(Math.Pow(1.0 / ChildDensity, 1.0 / 3.0) / LocalScale));

        /// <summary>
        /// Finds all children in a 3x3 box of grid spaces around the given position in local space.
        /// </summary>
        /// <param name="position">The location around which to locate children.</param>
        public IEnumerable<CelestialEntity> GetNearbyChildren(Vector3 position)
        {
            var coordinates = PositionToGridCoords(position);
            Populate3x3GridRegion(coordinates);
            if (Children != null)
            {
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
        }

        private float GetTotalChildren(string typeName) => Children?.Count(c => c.GetType().FullName == typeName) ?? 0;

        /// <summary>
        /// Converts set of grid coordinates to a collection of corner positions.
        /// </summary>
        /// <remarks>Grid space is 1-based, not 0-based.</remarks>
        /// <param name="coordinates">The grid space coordinates to convert.</param>
        /// <returns>
        /// A list of the positions of the four corners of the grid space within local space.
        /// </returns>
        private List<Vector3> GridCoordsToCornerPositions(Vector3 coordinates)
        {
            var x = (coordinates.X - 1) * GridSize;
            var y = (coordinates.Y - 1) * GridSize;
            var z = (coordinates.Z - 1) * GridSize;

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
        public bool IsGridSpaceInBounds(Vector3 coordinates) => (coordinates * GridSize).Length() <= LocalSpaceScale;

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
        private protected bool IsGridSpacePopulated(Vector3 coordinates)
        {
            if (GridSpaces.TryGetValue(coordinates, out var populated))
            {
                return populated;
            }
            else
            {
                return false;
            }
        }

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
            for (var x = coordinates.X == 1 ? -1 : (int)Math.Round(coordinates.X - 1);
                x <= (coordinates.X == -1 ? 1 : coordinates.X + 1); x++)
            {
                if (x == 0)
                {
                    continue;
                }

                for (var y = coordinates.Y == 1 ? -1 : (int)Math.Round(coordinates.Y - 1);
                    y <= (coordinates.Y == -1 ? 1 : coordinates.Y + 1); y++)
                {
                    if (y == 0)
                    {
                        continue;
                    }

                    for (var z = coordinates.Z == 1 ? -1 : (int)Math.Round(coordinates.Z - 1);
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
                .Any(p => Substance?.Shape?.IsPointWithin(Vector3.Zero, p * LocalScale) ?? false))
            {
                GenerateChild(coordinates);
            }
        }

        /// <summary>
        /// Generates an appropriate population of child objects in local space, in an area around
        /// the given position.
        /// </summary>
        /// <param name="position">The location around which to generate child objects.</param>
        /// <remarks>
        /// Will generate approximately 25–30 children in a box around the given position. Will not
        /// re-generate or over-populate regions which have already been populated (uses an
        /// internally-managed grid system).
        /// </remarks>
        public virtual void PopulateRegion(Vector3 position) => Populate3x3GridRegion(PositionToGridCoords(position));

        /// <summary>
        /// Converts a position to a set of grid coordinates.
        /// </summary>
        /// <remarks>Grid space is 1-based, not 0-based.</remarks>
        /// <param name="position">The position to convert.</param>
        /// <returns>
        /// The coordinates of the grid space where the position is located within local space.
        /// </returns>
        public Vector3 PositionToGridCoords(Vector3 position)
        {
            var x = position.X / GridSize;
            if (x != x % 1)
            {
                x = (float)(Math.Ceiling(Math.Abs(x)) * Math.Sign(x));
            }

            if (x == 0)
            {
                x = 1;
            }

            var y = position.Y / GridSize;
            if (y != y % 1)
            {
                y = (float)(Math.Ceiling(Math.Abs(y)) * Math.Sign(y));
            }

            if (y == 0)
            {
                y = 1;
            }

            var z = position.Z / GridSize;
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
        /// partially) by the region defined by the given shape.
        /// </summary>
        /// <param name="position">The center of the region to be marked as populated.</param>
        /// <param name="shape">The shape of the region to be marked as populated.</param>
        internal void SetRegionPopulated(Vector3 position, Shape shape)
        {
            if (_substance?.Shape == null)
            {
                return;
            }

            var home = PositionToGridCoords(position);
            GridSpaces[home] = true;

            // If the shape is null or empty, only the grid containing the position is marked.
            if ((shape?.Volume ?? 0) == 0)
            {
                return;
            }

            // Iterate over the box which fully contains the specified shape.
            var num = (int)Math.Ceiling((shape.ContainingRadius / LocalScale) / GridSize);
            for (var x = -num; x <= num; x++)
            {
                if (x == 0)
                {
                    continue;
                }

                for (var y = -num; y <= num; y++)
                {
                    if (y == 0)
                    {
                        continue;
                    }

                    for (var z = -num; z <= num; z++)
                    {
                        if (z == 0)
                        {
                            continue;
                        }

                        var coordinates = new Vector3(home.X + x, home.Y + y, home.Z + z);
                        // For in-bounds grids, test all four corners for inclusion in the shape.
                        if (IsGridSpaceInBounds(coordinates))
                        {
                            if (GridCoordsToCornerPositions(coordinates)
                                .Any(p => shape.IsPointWithin(position * LocalScale, p * LocalScale)))
                            {
                                GridSpaces[coordinates] = true;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Sets this <see cref="CelestialEntity"/>'s <see cref="Shape"/> to the given value.
        /// </summary>
        protected override void SetShape(Shape shape)
        {
            base.SetShape(shape);

            if (Children != null)
            {
                foreach (var child in Children.Where(x => x._substance?.Shape != null))
                {
                    SetRegionPopulated(child.Position, child.Substance.Shape);
                }
            }
        }
    }
}
