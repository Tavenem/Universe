using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Numerics;
using System.Text;
using WorldFoundry.Utilities.MathUtil.Shapes;

namespace WorldFoundry.Space
{
    /// <summary>
    /// Indicates an entity which may be contained within a <see cref="CelestialObject"/>, whether that is
    /// a <see cref="CelestialObject"/> or a <see cref="CelestialObjects.CelestialBody"/>.
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
        public Guid ID { get; internal set; }

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
        /// An optional name for this <see cref="CelestialEntity"/>.
        /// </summary>
        /// <remarks>
        /// Not every <see cref="CelestialEntity"/> must have a name. They may be uniquely identified
        /// by their <see cref="Designation"/>, instead.
        /// </remarks>
        public virtual string Name { get; set; }

        /// <summary>
        /// The <see cref="CelestialObject"/> which directly contains this <see cref="CelestialEntity"/>.
        /// </summary>
        public CelestialObject Parent { get; private set; }

        /// <summary>
        /// Specifies the location of this <see cref="CelestialEntity"/>'s center in the local space of its
        /// containing <see cref="Parent"/>.
        /// </summary>
        [NotMapped]
        public Vector3 Position
        {
            get => new Vector3(PositionX, PositionY, PositionZ);
            set
            {
                PositionX = value.X;
                PositionY = value.Y;
                PositionZ = value.Z;
            }
        }

        /// <summary>
        /// Specifies the X-coordinate of this <see cref="CelestialEntity"/>'s center in the local space of its containing
        /// <see cref="Parent"/>.
        /// </summary>
        public float PositionX { get; set; }

        /// <summary>
        /// Specifies the Y-coordinate of this <see cref="CelestialEntity"/>'s center in the local space of its containing
        /// <see cref="Parent"/>.
        /// </summary>
        public float PositionY { get; set; }

        /// <summary>
        /// Specifies the Z-coordinate of this <see cref="CelestialEntity"/>'s center in the local space of its containing
        /// <see cref="Parent"/>.
        /// </summary>
        public float PositionZ { get; set; }

        protected double? _radius;
        /// <summary>
        /// Gets a radius which fully contains this <see cref="CelestialEntity"/>, in meters.
        /// </summary>
        public double Radius
        {
            get
            {
                if (!_radius.HasValue)
                {
                    _radius = Shape?.GetContainingRadius() ?? null;
                }
                return _radius ?? 0;
            }
        }

        protected double? _radiusSquared;
        /// <summary>
        /// Gets the <see cref="Radius"/>, squared, in meters.
        /// </summary>
        public double RadiusSquared
        {
            get
            {
                if (!_radiusSquared.HasValue)
                {
                    _radiusSquared = Radius * Radius;
                }
                return _radiusSquared ?? 0;
            }
        }

        protected Shape _shape;
        /// <summary>
        /// The shape of the <see cref="CelestialEntity"/>.
        /// </summary>
        public virtual Shape Shape
        {
            get => GetProperty(ref _shape, GenerateShape);
            protected set
            {
                if (_shape == value)
                {
                    return;
                }
                _shape = value;

                if (value != null)
                {
                    Parent?.SetRegionPopulated(Position, _shape);
                }
            }
        }

        /// <summary>
        /// The <see cref="CelestialEntity"/>'s <see cref="Name"/>, if it has one; otherwise its <see cref="Designation"/>.
        /// </summary>
        [NotMapped]
        public string Title => string.IsNullOrEmpty(Name) ? Designation : Name;

        /// <summary>
        /// The name for this type of <see cref="CelestialEntity"/>.
        /// </summary>
        public virtual string TypeName => BaseTypeName;

        /// <summary>
        /// Initializes a new instance of <see cref="CelestialEntity"/>.
        /// </summary>
        public CelestialEntity() => ID = new Guid();

        /// <summary>
        /// Initializes a new instance of <see cref="CelestialEntity"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="CelestialEntity"/> is located.
        /// </param>
        public CelestialEntity(CelestialObject parent) : this()
        {
            Parent = parent;
            if (Parent.Children == null)
            {
                Parent.Children = new HashSet<CelestialEntity>();
            }
            Parent.Children.Add(this);
        }

        /// <summary>
        /// Initializes a new instance of <see cref="CelestialEntity"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="CelestialEntity"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="CelestialEntity"/>.</param>
        public CelestialEntity(CelestialObject parent, Vector3 position) : this(parent) => Position = position;

        /// <summary>
        /// Finds a common <see cref="CelestialObject"/> parent with the specified one.
        /// </summary>
        /// <param name="other">A <see cref="CelestialObject"/> with which to find a common parent.</param>
        /// <returns>
        /// The <see cref="CelestialObject"/> which is the common parent of this one and the specified
        /// one. Null if none exists.
        /// </returns>
        public CelestialObject FindCommonParent(CelestialEntity other)
        {
            var otherPath = other.GetTreePathToSpaceGrid().ToList();
            return GetTreePathToSpaceGrid().TakeWhile((o, i) => otherPath.Count > i && o == otherPath[i]).LastOrDefault() as CelestialObject;
        }

        protected void GenerateDesgination()
            => _designation = string.IsNullOrEmpty(DesignatorPrefix)
                ? ID.ToString("B")
                : $"{DesignatorPrefix} {ID.ToString("B")}";

        /// <summary>
        /// Generates the <see cref="Utilities.MathUtil.Shapes.Shape"/> of this <see cref="CelestialEntity"/>.
        /// </summary>
        /// <remarks>Generates an empty sphere in the base class; expected to be overridden in subclasses.</remarks>
        protected virtual void GenerateShape() => Shape = new Sphere();

        /// <summary>
        /// Returns the size of 1 unit of local space within this <see cref="CelestialEntity"/>, in meters.
        /// </summary>
        private float? GetLocalScale() => _radius.HasValue ? (float?)(_radius.Value / LocalSpaceScale) : null;

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
                return Position.Length() * (other as CelestialObject).LocalScale;
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
                if (storage == null && typeof(T) != typeof(string))
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
        /// Recursively builds an ordered list of <see cref="CelestialObject"/><see cref="Parent"/>s from
        /// the topmost to this child.
        /// </summary>
        /// <param name="path">
        /// An ordered stack of one branch of <see cref="CelestialEntity"/> objects.
        /// </param>
        /// <returns>
        /// An ordered stack of <see cref="CelestialObject"/> parents and a <see cref="CelestialEntity"/>
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
        /// <param name="newParent">The <see cref="CelestialObject"/> which will be this one's new parent.</param>
        internal void SetNewParent(CelestialObject newParent)
        {
            Position = GetTreePathToSpaceGrid().Contains(newParent)
                ? TranslateToLocalCoordinates(newParent)
                : Vector3.Zero;

            Parent = newParent;
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
        internal Vector3 TranslateToLocalCoordinates(CelestialObject other, Vector3 position)
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
        internal Vector3 TranslateToLocalCoordinates(CelestialObject other) => TranslateToLocalCoordinates(other, Position);

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

            float ratio = LocalScale / Parent.LocalScale;
            position *= ratio;
            return Position + position;
        }
    }
}
