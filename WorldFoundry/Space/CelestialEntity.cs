using MathAndScience.Numerics;
using MathAndScience.Shapes;
using Substances;
using System;
using WorldFoundry.Place;

namespace WorldFoundry.Space
{
    /// <summary>
    /// Indicates an entity which may be contained within a <see cref="CelestialRegion"/>, whether that is
    /// a <see cref="CelestialRegion"/> or a <see cref="CelestialBodies.CelestialBody"/>.
    /// </summary>
    public class CelestialEntity : IEquatable<CelestialEntity>
    {
        internal int _seed1;
        internal int _seed2;
        internal int _seed3;
        internal int _seed4;
        internal int _seed5;

        private const string _baseTypeName = "Celestial Object";
        /// <summary>
        /// The base name for this type of <see cref="CelestialEntity"/>.
        /// </summary>
        /// <remarks>Intended to be hidden by subclasses.</remarks>
        public virtual string BaseTypeName => _baseTypeName;

        /// <summary>
        /// A string that uniquely identifies this <see cref="CelestialEntity"/>.
        /// </summary>
        public string Designation
            => string.IsNullOrEmpty(DesignatorPrefix)
                ? ID.ToString("B")
                : $"{DesignatorPrefix} {ID.ToString("B")}";

        /// <summary>
        /// An optional string which is placed before a <see cref="CelestialEntity"/>'s <see cref="Designation"/>.
        /// </summary>
        protected virtual string DesignatorPrefix => string.Empty;

        /// <summary>
        /// The primary key for this <see cref="CelestialEntity"/>.
        /// </summary>
        public Guid ID { get; }

        /// <summary>
        /// If <see langword="false"/> this type of <see cref="CelestialEntity"/> and its children
        /// cannot support life.
        /// </summary>
        public virtual bool IsHospitable => Parent?.IsHospitable ?? true;

        /// <summary>
        /// The total mass of this <see cref="CelestialEntity"/>, in kg.
        /// </summary>
        public double Mass => Substance.Mass;

        /// <summary>
        /// An optional name for this <see cref="CelestialEntity"/>.
        /// </summary>
        /// <remarks>
        /// Not every <see cref="CelestialEntity"/> must have a name. They may be uniquely identified
        /// by their <see cref="Designation"/>, instead.
        /// </remarks>
        public virtual string Name { get; set; }

        private protected Location _location;
        /// <summary>
        /// The location of this <see cref="CelestialEntity"/>.
        /// </summary>
        public Location Location => _location;

        /// <summary>
        /// The <see cref="CelestialRegion"/> which directly contains this <see cref="CelestialEntity"/>.
        /// </summary>
        public CelestialRegion Parent => Location?.Parent?.CelestialEntity as CelestialRegion;

        /// <summary>
        /// Specifies the location of this <see cref="CelestialEntity"/>'s center in the local space
        /// of its containing <see cref="Parent"/>.
        /// </summary>
        public Vector3 Position
        {
            get => Location.Position;
            set
            {
                if (Location != null)
                {
                    Location.Position = value;
                }
                Shape = Shape.GetCloneAtPosition(value);
            }
        }

        /// <summary>
        /// Gets a radius which fully contains this <see cref="CelestialEntity"/>, in meters.
        /// </summary>
        public double Radius => Shape.ContainingRadius;

        /// <summary>
        /// The shape of this <see cref="CelestialEntity"/>.
        /// </summary>
        public IShape Shape
        {
            get => Substance.Shape;
            protected set
            {
                Substance.Shape = value.GetCloneAtPosition(Position);
                if (Location is Region region)
                {
                    region.Shape = Substance.Shape;
                }
                _radiusSquared = null;
                _surfaceGravity = null;
            }
        }

        private Substance _substance;
        /// <summary>
        /// The substance which represents this <see cref="CelestialEntity"/>'s physical form.
        /// </summary>
        public Substance Substance
        {
            get
            {
                if (_substance == null)
                {
                    GenerateSubstance();
                    if (_substance == null)
                    {
                        _substance = new Substance { Composition = Material.Empty() };
                    }
                }
                return _substance;
            }
            protected set
            {
                _substance = value;
                _radiusSquared = null;
                _surfaceGravity = null;
            }
        }

        private double? _surfaceGravity;
        /// <summary>
        /// The average force of gravity at the surface of this <see cref="CelestialEntity"/>, in N.
        /// </summary>
        public double SurfaceGravity
        {
            get
            {
                if (_surfaceGravity == null)
                {
                    _surfaceGravity = Substance.GetSurfaceGravity();
                }
                return _surfaceGravity ?? 0;
            }
        }

        /// <summary>
        /// The average temperature of this <see cref="CelestialEntity"/>, in K.
        /// </summary>
        /// <remarks>No less than <see cref="Parent"/>'s ambient temperature.</remarks>
        public double? Temperature => Math.Max(Substance.Temperature, Parent?.Temperature ?? 0);

        /// <summary>
        /// The <see cref="CelestialEntity"/>'s <see cref="Name"/>, if it has one; otherwise its <see cref="Designation"/>.
        /// </summary>
        public string Title => Name ?? Designation;

        /// <summary>
        /// The name for this type of <see cref="CelestialEntity"/>.
        /// </summary>
        public virtual string TypeName => BaseTypeName;

        private double? _radiusSquared;
        internal double RadiusSquared
        {
            get
            {
                if (_radiusSquared == null && Substance.Shape != null)
                {
                    _radiusSquared = Shape.ContainingRadius * Shape.ContainingRadius;
                }
                return _radiusSquared ?? 0;
            }
        }

        /// <summary>
        /// Initializes a new instance of <see cref="CelestialEntity"/>.
        /// </summary>
        public CelestialEntity()
        {
            ID = Guid.NewGuid();
            _seed1 = Randomizer.Instance.NextInclusiveMaxValue() * (Randomizer.Instance.NextBoolean() ? -1 : 1);
            _seed2 = Randomizer.Instance.NextInclusiveMaxValue() * (Randomizer.Instance.NextBoolean() ? -1 : 1);
            _seed3 = Randomizer.Instance.NextInclusiveMaxValue() * (Randomizer.Instance.NextBoolean() ? -1 : 1);
            _seed4 = Randomizer.Instance.NextInclusiveMaxValue() * (Randomizer.Instance.NextBoolean() ? -1 : 1);
            _seed5 = Randomizer.Instance.NextInclusiveMaxValue() * (Randomizer.Instance.NextBoolean() ? -1 : 1);
        }

        /// <summary>
        /// Initializes a new instance of <see cref="CelestialEntity"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="CelestialEntity"/> is located.
        /// </param>
        public CelestialEntity(CelestialRegion parent) : this() => GenerateLocation(parent);

        /// <summary>
        /// Initializes a new instance of <see cref="CelestialEntity"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="CelestialEntity"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="CelestialEntity"/>.</param>
        public CelestialEntity(CelestialRegion parent, Vector3 position) : this() => GenerateLocation(parent, position);

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns><see langword="true"/> if the specified object is equal to the current object;
        /// otherwise, <see langword="false"/>.</returns>
        public override bool Equals(object obj) => obj is CelestialEntity other && Equals(other);

        /// <summary>
        /// Determines whether the specified <see cref="CelestialEntity"/> instance is equal to this
        /// one.
        /// </summary>
        /// <param name="other">The <see cref="CelestialEntity"/> instance to compare with this
        /// one.</param>
        /// <returns><see langword="true"/> if the specified <see cref="CelestialEntity"/> instance
        /// is equal to this once; otherwise, <see langword="false"/>.</returns>
        public bool Equals(CelestialEntity other) => ID != Guid.Empty && ID == other?.ID;

        private protected virtual void GenerateLocation(CelestialRegion parent, Vector3? position = null)
            => _location = new Location(this, parent?.Location, position ?? Vector3.Zero);

        /// <summary>
        /// Generates the <see cref="Substance"/> of this <see cref="CelestialEntity"/>.
        /// </summary>
        /// <remarks>Does nothing in the base class; expected to be overridden in subclasses.</remarks>
        private protected virtual void GenerateSubstance() { }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>The hash code for this instance.</returns>
        public override int GetHashCode() => ID.GetHashCode();

        /// <summary>
        /// Returns a string that represents the celestial object.
        /// </summary>
        /// <returns>A string that represents the celestial object.</returns>
        public override string ToString() => $"{TypeName} {Title}";
    }
}
