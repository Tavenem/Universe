using MathAndScience.MathUtil.Shapes;
using Substances;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace WorldFoundry.Space
{
    /// <summary>
    /// Indicates an entity which may be contained within a <see cref="CelestialRegion"/>, whether that is
    /// a <see cref="CelestialRegion"/> or a <see cref="CelestialBodies.CelestialBody"/>.
    /// </summary>
    public class CelestialEntity
    {
        /// <summary>
        /// Local space is a coordinate system with a range of -1000000 to 1000000.
        /// </summary>
        internal const int LocalSpaceScale = 1000000;

        internal static string baseTypeName = "Celestial Object";
        /// <summary>
        /// The base name for this type of <see cref="CelestialEntity"/>.
        /// </summary>
        /// <remarks>Intended to be hidden by subclasses.</remarks>
        public virtual string BaseTypeName => baseTypeName;

        /// <summary>
        /// The chance that this type of <see cref="CelestialEntity"/> and its children will actually
        /// have a biosphere, if it is habitable.
        /// </summary>
        /// <remarks>
        /// A value of 1 (or null) indicates that every body which could sustain life, does. A value
        /// of 0.5 indicates that only half of potentially habitable worlds actually have living
        /// organisms. The lowest value in a world's parent hierarchy is used, which allows parent
        /// objects to override children to reflect inhospitable conditions (e.g. excessive radiation).
        /// </remarks>
        public virtual float? ChanceOfLife => null;

        private string _designation;
        /// <summary>
        /// A string that uniquely identifies this <see cref="CelestialEntity"/>.
        /// </summary>
        public string Designation => GetProperty(ref _designation, GenerateDesgination);

        /// <summary>
        /// An optional string which is placed before a <see cref="CelestialEntity"/>'s <see cref="Designation"/>.
        /// </summary>
        protected virtual string DesignatorPrefix => string.Empty;

        /// <summary>
        /// The primary key for this <see cref="CelestialEntity"/>.
        /// </summary>
        public Guid ID { get; private set; }

        private float? _localScale;
        /// <summary>
        /// The size of 1 unit of local space within this <see cref="CelestialEntity"/>, in meters.
        /// </summary>
        public float LocalScale
        {
            get
            {
                if (!_localScale.HasValue)
                {
                    _localScale = GetLocalScale();
                }
                return _localScale ?? 0;
            }
        }

        /// <summary>
        /// The total mass of this <see cref="CelestialEntity"/>, in kg.
        /// </summary>
        public double Mass => Substance?.Mass ?? 0;

        /// <summary>
        /// An optional name for this <see cref="CelestialEntity"/>.
        /// </summary>
        /// <remarks>
        /// Not every <see cref="CelestialEntity"/> must have a name. They may be uniquely identified
        /// by their <see cref="Designation"/>, instead.
        /// </remarks>
        public virtual string Name { get; set; }

        /// <summary>
        /// The <see cref="CelestialRegion"/> which directly contains this <see cref="CelestialEntity"/>.
        /// </summary>
        public CelestialRegion Parent { get; private set; }

        /// <summary>
        /// Specifies the location of this <see cref="CelestialEntity"/>'s center in the local space of its
        /// containing <see cref="Parent"/>.
        /// </summary>
        public Vector3 Position { get; set; }

        /// <summary>
        /// Gets a radius which fully contains this <see cref="CelestialEntity"/>, in meters.
        /// </summary>
        public double Radius => Substance?.Shape?.ContainingRadius ?? 0;

        private double? _radiusSquared;
        /// <summary>
        /// Gets the <see cref="Radius"/>, squared, in meters.
        /// </summary>
        public double RadiusSquared
        {
            get
            {
                if (!_radiusSquared.HasValue && _substance?.Shape != null)
                {
                    _radiusSquared = _substance.Shape.ContainingRadius * _substance.Shape.ContainingRadius;
                }
                return _radiusSquared ?? 0;
            }
        }

        internal Substance _substance;
        /// <summary>
        /// The substance which represents this <see cref="CelestialEntity"/>'s physical form.
        /// </summary>
        public Substance Substance
        {
            get => GetProperty(ref _substance, GenerateSubstance);
            protected set => _substance = value;
        }

        private double? _surfaceGravity;
        /// <summary>
        /// The average force of gravity at the surface of this <see cref="CelestialEntity"/>, in N.
        /// </summary>
        public double SurfaceGravity
        {
            get
            {
                if (!_surfaceGravity.HasValue && _substance?.Shape != null)
                {
                    _surfaceGravity = _substance.GetSurfaceGravity();
                }
                return _surfaceGravity ?? 0;
            }
        }

        /// <summary>
        /// The average temperature of this <see cref="CelestialEntity"/>, in K.
        /// </summary>
        /// <remarks>No less than <see cref="Parent"/>'s ambient temperature.</remarks>
        public float? Temperature => Math.Max(Substance?.Temperature ?? 0, Parent?.Temperature ?? 0);

        /// <summary>
        /// The <see cref="CelestialEntity"/>'s <see cref="Name"/>, if it has one; otherwise its <see cref="Designation"/>.
        /// </summary>
        public string Title => string.IsNullOrEmpty(Name) ? Designation : Name;

        /// <summary>
        /// The name for this type of <see cref="CelestialEntity"/>.
        /// </summary>
        public virtual string TypeName => BaseTypeName;

        /// <summary>
        /// Initializes a new instance of <see cref="CelestialEntity"/>.
        /// </summary>
        public CelestialEntity() => ID = Guid.NewGuid();

        /// <summary>
        /// Initializes a new instance of <see cref="CelestialEntity"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="CelestialEntity"/> is located.
        /// </param>
        public CelestialEntity(CelestialRegion parent) : this()
        {
            if (parent != null)
            {
                Parent = parent;
                if (Parent.Children == null)
                {
                    Parent.Children = new List<CelestialEntity>();
                }
                Parent.Children.Add(this);
            }
        }

        /// <summary>
        /// Initializes a new instance of <see cref="CelestialEntity"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="CelestialEntity"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="CelestialEntity"/>.</param>
        public CelestialEntity(CelestialRegion parent, Vector3 position) : this(parent) => Position = position;

        /// <summary>
        /// Finds a common <see cref="CelestialRegion"/> parent with the specified one.
        /// </summary>
        /// <param name="other">A <see cref="CelestialRegion"/> with which to find a common parent.</param>
        /// <returns>
        /// The <see cref="CelestialRegion"/> which is the common parent of this one and the specified
        /// one. Null if none exists.
        /// </returns>
        public CelestialRegion FindCommonParent(CelestialEntity other)
        {
            var otherPath = other.GetTreePathToSpaceGrid().ToList();
            return GetTreePathToSpaceGrid().TakeWhile((o, i) => otherPath.Count > i && o == otherPath[i]).LastOrDefault() as CelestialRegion;
        }

        private protected void GenerateDesgination()
            => _designation = string.IsNullOrEmpty(DesignatorPrefix)
                ? ID.ToString("B")
                : $"{DesignatorPrefix} {ID.ToString("B")}";

        /// <summary>
        /// Generates the <see cref="Substance"/> of this <see cref="CelestialEntity"/>.
        /// </summary>
        /// <remarks>Does nothing in the base class; expected to be overridden in subclasses.</remarks>
        private protected virtual void GenerateSubstance() { }

        /// <summary>
        /// Determines the chance that this <see cref="CelestialEntity"/> and its children will
        /// actually have a biosphere, if it is habitable: a value between 0.0 and 1.0.
        /// </summary>
        /// <returns>
        /// The chance that this <see cref="CelestialEntity"/> and its children will actually have a
        /// biosphere, if it is habitable: a value between 0.0 and 1.0.
        /// </returns>
        internal float GetChanceOfLife() => Math.Min(Parent?.GetChanceOfLife() ?? 1.0f, ChanceOfLife ?? 1.0f);

        /// <summary>
        /// Calculates the distance between the given position in local space to the
        /// center of the specified <see cref="CelestialEntity"/>, in meters.
        /// </summary>
        /// <param name="position">The position from which to calculate the distance.</param>
        /// <param name="other">
        /// The <see cref="CelestialEntity"/> whose distance from this one is to be determined.
        /// </param>
        /// <returns>
        /// The distance between this <see cref="CelestialEntity"/> and the
        /// specified one, in meters.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="other"/> cannot be null.</exception>
        /// <exception cref="Exception">
        /// The two <see cref="CelestialEntity"/> objects must be part of the same hierarchy (i.e.
        /// share a common parent).
        /// </exception>
        internal float GetDistanceFromPositionToTarget(Vector3 position, CelestialEntity other)
        {
            if (other == null)
            {
                throw new ArgumentNullException();
            }

            if (other == Parent)
            {
                return Position.Length() * (other as CelestialRegion).LocalScale;
            }
            else if (other.Parent == this)
            {
                return other.Position.Length() * LocalScale;
            }
            else if (Parent == other.Parent)
            {
                return (other.Position - position).Length() * Parent.LocalScale;
            }
            else
            {
                var commonParent = FindCommonParent(other);
                if (commonParent == null)
                {
                    throw new Exception($"{this} and {other} are not part of the same hierarchy");
                }

                return Vector3.Distance(TranslateToLocalCoordinates(commonParent), other.TranslateToLocalCoordinates(commonParent)) * commonParent.LocalScale;
            }
        }

        /// <summary>
        /// Calculates the distance between the centers of this <see
        /// cref="CelestialEntity"/> and the specified one, in meters.
        /// </summary>
        /// <param name="other">
        /// The <see cref="CelestialEntity"/> whose distance from this one is to be determined.
        /// </param>
        /// <returns>
        /// The distance between this <see cref="CelestialEntity"/> and the
        /// specified one, in meters.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="other"/> cannot be null.</exception>
        /// <exception cref="Exception">
        /// The two <see cref="CelestialEntity"/> objects must be part of the same hierarchy (i.e.
        /// share a common parent).
        /// </exception>
        internal float GetDistanceToTarget(CelestialEntity other) => GetDistanceFromPositionToTarget(Position, other);

        /// <summary>
        /// Returns the size of 1 unit of local space within this <see cref="CelestialEntity"/>, in meters.
        /// </summary>
        private float? GetLocalScale()
        {
            if (Substance?.Shape == null)
            {
                return null;
            }
            return (float?)(Radius / LocalSpaceScale);
        }

        /// <summary>
        /// Provides safe retrieval, and optional automatic generation, of a backing store.
        /// </summary>
        /// <typeparam name="T">The type of the backing store.</typeparam>
        /// <param name="storage">The backing storage field being retrieved or generated.</param>
        /// <param name="generator">
        /// An optional generation method which will be invoked when the backing store is first initialized.
        /// </param>
        /// <param name="condition">
        /// An optional condition which will be evaluated when the backing store is null, which determines
        /// whether to provide automatic initialization, or to allow the null return.
        /// </param>
        /// <returns></returns>
        protected T GetProperty<T>(ref T storage, Action generator = null)
        {
            if (storage == null || (storage is string s && string.IsNullOrEmpty(s)))
            {
                if (storage == null && typeof(T) != typeof(string) && !typeof(T).IsInterface)
                {
                    storage = (T)Activator.CreateInstance(typeof(T));
                }
                if (generator != null)
                {
                    generator.Invoke();
                }
            }
            return storage;
        }

        /// <summary>
        /// Recursively builds an ordered list of <see cref="CelestialRegion"/><see cref="Parent"/>s from
        /// the topmost to this child.
        /// </summary>
        /// <param name="path">
        /// An ordered stack of one branch of <see cref="CelestialEntity"/> objects.
        /// </param>
        /// <returns>
        /// An ordered stack of <see cref="CelestialRegion"/> parents and a <see cref="CelestialEntity"/>
        /// child, from the topmost to a particular <see cref="CelestialEntity"/>.
        /// </returns>
        internal Stack<CelestialEntity> GetTreePathToSpaceGrid(Stack<CelestialEntity> path = null)
        {
            if (path == null)
            {
                path = new Stack<CelestialEntity>();
            }
            path.Push(this);
            return Parent != null ? Parent.GetTreePathToSpaceGrid(path) : path;
        }

        /// <summary>
        /// Performs the behind-the-scenes work necessary to transfer a <see cref="CelestialEntity"/>
        /// to a new <see cref="Parent"/> in the hierarchy.
        /// </summary>
        /// <remarks>
        /// If the new parent is part of this <see cref="CelestialEntity"/> object's current parent
        /// hierarchy, the current coordinates will be preserved, and translated into the correct
        /// local space. Otherwise, they will be reset to 0,0,0.
        /// </remarks>
        /// <param name="newParent">The <see cref="CelestialRegion"/> which will be this one's new parent.</param>
        internal void SetNewParent(CelestialRegion newParent)
        {
            Position = GetTreePathToSpaceGrid().Contains(newParent)
                ? TranslateToLocalCoordinates(newParent)
                : Vector3.Zero;

            Parent = newParent;
        }

        /// <summary>
        /// Sets this <see cref="CelestialEntity"/>'s shape to the given value.
        /// </summary>
        protected virtual void SetShape(Shape shape)
        {
            if (_substance == null)
            {
                throw new Exception($"{nameof(Substance)} must be initialized before calling {nameof(SetShape)}.");
            }

            Substance.Shape = shape ?? throw new ArgumentNullException(nameof(shape));

            Parent?.SetRegionPopulated(Position, shape);
        }

        /// <summary>
        /// Returns a string that represents the celestial object.
        /// </summary>
        /// <returns>A string that represents the celestial object.</returns>
        public override string ToString() => $"{TypeName} {Title}";

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
        internal Vector3 TranslateToLocalCoordinates(CelestialRegion other, Vector3 position)
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
            return ((position - otherPosition) * LocalScale) / other.LocalScale;
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
        internal Vector3 TranslateToLocalCoordinates(CelestialRegion other) => TranslateToLocalCoordinates(other, Position);

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

            var ratio = LocalScale / Parent.LocalScale;
            position *= ratio;
            return Position + position;
        }
    }
}
