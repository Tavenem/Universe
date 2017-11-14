﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Numerics;
using System.Text;
using WorldFoundry.Utilities.MathUtil.Shapes;

namespace WorldFoundry.Space
{
    /// <summary>
    /// Indicates an entity which may be contained within a <see cref="SpaceRegion"/>, whether that is
    /// a <see cref="SpaceRegion"/> or a <see cref="CelestialObjects.CelestialBody"/>.
    /// </summary>
    public class SpaceChild
    {
        /// <summary>
        /// Local space is a coordinate system with a range of -1000000 to 1000000.
        /// </summary>
        internal const int localSpaceScale = 1000000;

        /// <summary>
        /// The base name for this type of celestial object.
        /// </summary>
        /// <remarks>Intended to be hidden by subclasses.</remarks>
        public static string BaseTypeName => "Celestial Object";

        /// <summary>
        /// The primary key for this entity.
        /// </summary>
        public Guid ID { get; set; }

        private string _designation;
        /// <summary>
        /// A string that uniquely identifies this celestial object.
        /// </summary>
        public string Designation => GetProperty(ref _designation, GenerateDesgination);

        /// <summary>
        /// An optional string which is placed before a celestial object's <see cref="Designation"/>.
        /// </summary>
        protected virtual string DesignatorPrefix => string.Empty;

        private float? _localScale;
        /// <summary>
        /// The size of 1 unit of local space within this <see cref="SpaceRegion"/>, in meters.
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
        /// An optional name for this celestial object.
        /// </summary>
        /// <remarks>
        /// Not every celestial object must have a name. They may be uniquely identified by their
        /// <see cref="Designation"/>, instead.
        /// </remarks>
        public virtual string Name { get; set; }

        /// <summary>
        /// The <see cref="SpaceRegion"/> which directly contains this entity.
        /// </summary>
        public SpaceRegion Parent { get; set; }

        /// <summary>
        /// Specifies the location of this entity's center in the local space of its
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
        /// Specifies the X-coordinate of this entity's center in the local space of its containing
        /// <see cref="Parent"/>.
        /// </summary>
        public float PositionX { get; set; }

        /// <summary>
        /// Specifies the Y-coordinate of this entity's center in the local space of its containing
        /// <see cref="Parent"/>.
        /// </summary>
        public float PositionY { get; set; }

        /// <summary>
        /// Specifies the Z-coordinate of this entity's center in the local space of its containing
        /// <see cref="Parent"/>.
        /// </summary>
        public float PositionZ { get; set; }

        protected float? _radius;
        /// <summary>
        /// Gets a radius which fully contains this <see cref="SpaceRegion"/>, in meters.
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
        /// The shape of the <see cref="SpaceChild"/>.
        /// </summary>
        public virtual Shape Shape
        {
            get => GetProperty(ref _shape, GenerateShape);
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
        /// The celestial object's <see cref="Name"/>, if it has one; otherwise its <see cref="Designation"/>.
        /// </summary>
        [NotMapped]
        public string Title => string.IsNullOrEmpty(Name) ? Designation : Name;

        /// <summary>
        /// The name for this type of celestial object.
        /// </summary>
        /// <remarks>Intended to be overridden by subclasses.</remarks>
        public virtual string TypeName => "Celestial Object";

        /// <summary>
        /// Initializes a new instance of <see cref="SpaceChild"/>.
        /// </summary>
        public SpaceChild() { }

        /// <summary>
        /// Initializes a new instance of <see cref="SpaceChild"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="SpaceRegion"/> in which this <see cref="SpaceChild"/> is located.
        /// </param>
        public SpaceChild(SpaceRegion parent)
        {
            Parent = parent;
            Parent.Children.Add(this);
        }

        /// <summary>
        /// Initializes a new instance of <see cref="SpaceChild"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="SpaceRegion"/> in which this <see cref="SpaceChild"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="SpaceChild"/>.</param>
        public SpaceChild(SpaceRegion parent, Vector3 position) : this(parent) => Position = position;

        /// <summary>
        /// Finds a common <see cref="SpaceRegion"/> parent with the specified one.
        /// </summary>
        /// <param name="other">A <see cref="SpaceRegion"/> with which to find a common parent.</param>
        /// <returns>
        /// The <see cref="SpaceRegion"/> which is the common parent of this one and the specified
        /// one. Null if none exists.
        /// </returns>
        public SpaceRegion FindCommonParent(SpaceChild other)
        {
            var otherPath = other.GetTreePathToSpaceGrid().ToList();
            return GetTreePathToSpaceGrid().TakeWhile((o, i) => otherPath.Count > i && o == otherPath[i]).LastOrDefault() as SpaceRegion;
        }

        protected void GenerateDesgination()
            => _designation = string.IsNullOrEmpty(DesignatorPrefix)
                ? ID.ToString("B")
                : $"{DesignatorPrefix} {ID.ToString("B")}";

        /// <summary>
        /// Generates the <see cref="Utilities.MathUtil.Shapes.Shape"/> of this <see cref="SpaceChild"/>.
        /// </summary>
        /// <remarks>Generates an empty sphere in the base class; expected to be overridden in subclasses.</remarks>
        protected void GenerateShape() => Shape = new Sphere();

        /// <summary>
        /// Returns the size of 1 unit of local space within this <see cref="SpaceRegion"/>, in meters.
        /// </summary>
        private float? GetLocalScale() => Radius.HasValue ? (float?)(Radius.Value / localSpaceScale) : null;

        /// <summary>
        /// Calculates the distance (in meters) between the given position in local space to the
        /// center of the specified <see cref="SpaceChild"/>.
        /// </summary>
        /// <param name="position">The position from which to calculate the distance.</param>
        /// <param name="other">
        /// The <see cref="SpaceChild"/> whose distance from this one is to be determined.
        /// </param>
        /// <returns>
        /// The distance (in local space units) between this <see cref="SpaceChild"/> and the
        /// specified one.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="other"/> cannot be null.</exception>
        /// <exception cref="Exception">
        /// The two <see cref="SpaceChild"/> objects must be part of the same hierarchy (i.e.
        /// share a common parent).
        /// </exception>
        internal float GetDistanceFromPositionToTarget(Vector3 position, SpaceChild other)
        {
            if (other == null)
            {
                throw new ArgumentNullException();
            }

            if (other == Parent)
            {
                return Position.Length() * (other as SpaceRegion).LocalScale;
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
        /// Calculates the distance (in meters) between the centers of this <see
        /// cref="SpaceChild"/> and the specified one.
        /// </summary>
        /// <param name="other">
        /// The <see cref="SpaceChild"/> whose distance from this one is to be determined.
        /// </param>
        /// <returns>
        /// The distance (in local space units) between this <see cref="SpaceChild"/> and the
        /// specified one.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="other"/> cannot be null.</exception>
        /// <exception cref="Exception">
        /// The two <see cref="SpaceChild"/> objects must be part of the same hierarchy (i.e.
        /// share a common parent).
        /// </exception>
        internal float GetDistanceToTarget(SpaceChild other) => GetDistanceFromPositionToTarget(Position, other);

        protected T GetProperty<T>(ref T storage, Action generator = null, Func<bool> condition = null)
        {
            if ((storage == null || (storage is string s && string.IsNullOrEmpty(s)))
                && (condition == null || condition.Invoke()))
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
        /// Recursively builds an ordered list of <see cref="SpaceRegion"/><see cref="Parent"/>s from
        /// the topmost to this child.
        /// </summary>
        /// <param name="path">
        /// An ordered stack of one branch of <see cref="SpaceChild"/> objects.
        /// </param>
        /// <returns>
        /// An ordered stack of <see cref="SpaceRegion"/> parents and a <see cref="SpaceChild"/>
        /// child, from the topmost to a particular <see cref="SpaceChild"/>.
        /// </returns>
        internal Stack<SpaceChild> GetTreePathToSpaceGrid(Stack<SpaceChild> path = null)
        {
            if (path == null)
            {
                path = new Stack<SpaceChild>();
            }
            path.Push(this);
            return Parent != null ? Parent.GetTreePathToSpaceGrid(path) : path;
        }

        /// <summary>
        /// Performs the behind-the-scenes work necessary to transfer a <see cref="SpaceChild"/>
        /// to a new <see cref="Parent"/> in the hierarchy.
        /// </summary>
        /// <remarks>
        /// If the new parent is part of this <see cref="SpaceChild"/> object's current parent
        /// hierarchy, the current coordinates will be preserved, and translated into the correct
        /// local space. Otherwise, they will be reset to 0,0,0.
        /// </remarks>
        /// <param name="newParent">The <see cref="SpaceRegion"/> which will be this one's new parent.</param>
        internal void SetNewParent(SpaceRegion newParent)
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
        internal Vector3 TranslateToLocalCoordinates(SpaceRegion other, Vector3 position)
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
        internal Vector3 TranslateToLocalCoordinates(SpaceRegion other) => TranslateToLocalCoordinates(other, Position);

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