using MathAndScience;
using MathAndScience.Numerics;
using MathAndScience.Shapes;
using Substances;
using System;
using System.Text;
using WorldFoundry.Place;

namespace WorldFoundry.Space
{
    /// <summary>
    /// Indicates an entity which may be contained within a <see cref="CelestialRegion"/>, whether that is
    /// a <see cref="CelestialRegion"/> or a <see cref="CelestialBodies.CelestialBody"/>.
    /// </summary>
    public class CelestialEntity : IEquatable<CelestialEntity>
    {
        private protected int _seed1;
        private protected int _seed2;
        private protected int _seed3;
        private protected int _seed4;
        private protected int _seed5;

        /// <summary>
        /// A string that uniquely identifies this <see cref="CelestialEntity"/> for display
        /// purposes.
        /// </summary>
        public string Designation
            => string.IsNullOrEmpty(DesignatorPrefix) ? ID : $"{DesignatorPrefix} {ID}";

        /// <summary>
        /// A unique identifier for this <see cref="CelestialEntity"/>.
        /// </summary>
        public string ID { get; private set; }

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

        /// <summary>
        /// The location of this <see cref="CelestialEntity"/>.
        /// </summary>
        public Location Location { get; internal set; }

        /// <summary>
        /// The orbit occupied by this <see cref="CelestialEntity"/> (may be null).
        /// </summary>
        public virtual Orbit Orbit { get; set; }

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
            => _surfaceGravity ?? (_surfaceGravity = Substance.GetSurfaceGravity()).Value;

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

        /// <summary>
        /// Specifies the velocity of the <see cref="CelestialEntity"/>.
        /// </summary>
        public virtual Vector3 Velocity { get; set; }

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

        private protected virtual string BaseTypeName => "Celestial Object";

        private protected virtual string DesignatorPrefix => string.Empty;

        private protected virtual bool IsHospitable => Parent?.IsHospitable ?? true;

        /// <summary>
        /// Initializes a new instance of <see cref="CelestialEntity"/>.
        /// </summary>
        internal CelestialEntity() { }

        /// <summary>
        /// Initializes a new instance of <see cref="CelestialEntity"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="CelestialEntity"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="CelestialEntity"/>.</param>
        internal CelestialEntity(CelestialRegion parent, Vector3 position) : this() => GenerateLocation(parent, position);

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
        public bool Equals(CelestialEntity other)
            => !string.IsNullOrEmpty(ID) && string.Equals(ID, other?.ID, StringComparison.Ordinal);

        /// <summary>
        /// Determines an orbit for this <see cref="CelestialEntity"/>.
        /// </summary>
        /// <param name="orbitedObject">The <see cref="CelestialEntity"/> which is to be orbited.</param>
        /// <remarks>
        /// In the base class, always generates a circular orbit; subclasses are expected to override.
        /// </remarks>
        public virtual void GenerateOrbit(CelestialEntity orbitedObject)
        {
            if (orbitedObject == null)
            {
                return;
            }

            Orbit = Orbit.GetCircularOrbit(this, orbitedObject);

            Velocity = Orbit.V0;
        }

        /// <summary>
        /// Calculates the force of gravity on this <see cref="CelestialEntity"/> from another as a
        /// vector, in N.
        /// </summary>
        /// <param name="other">An <see cref="CelestialEntity"/> from which the force gravity will
        /// be calculated.</param>
        /// <returns>
        /// The force of gravity from this <see cref="CelestialEntity"/> to the other, in N, as a
        /// vector.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="other"/> may not be null.
        /// </exception>
        /// <exception cref="Exception">
        /// An exception will be thrown if the two <see cref="CelestialEntity"/> instances do not
        /// share a <see cref="CelestialRegion"/> parent at some point.
        /// </exception>
        /// <remarks>
        /// Newton's law is used. General relativity would be more accurate in certain
        /// circumstances, but is considered unnecessarily intensive work for the simple simulations
        /// expected to make use of this library. If you are an astronomer performing scientifically
        /// rigorous calculations or simulations, this is not the library for you ;)
        /// </remarks>
        public Vector3 GetGravityFromObject(CelestialEntity other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            var distance = Location.GetDistanceTo(other);

            var scale = -ScienceConstants.G * (Mass * other.Mass / (distance * distance));

            // Get the normalized vector
            var normalized = (other.Position - Position) / distance;

            return normalized * scale;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>The hash code for this instance.</returns>
        public override int GetHashCode() => ID.GetHashCode();

        /// <summary>
        /// Calculates the total force of gravity on this <see cref="CelestialEntity"/>, in N, as a
        /// vector. Note that results may be highly inaccurate if the parent region has not been
        /// populated thoroughly enough in the vicinity of this entity (with the scale of "vicinity"
        /// depending strongly on the mass of the region's potential children).
        /// </summary>
        /// <returns>
        /// The total force of gravity on this <see cref="CelestialEntity"/> from all
        /// currently-generated children, in N, as a vector.
        /// </returns>
        /// <remarks>
        /// Newton's law is used. Children of sibling objects are not counted individually; instead
        /// the entire sibling is treated as a single entity, with total mass including all its
        /// children. Objects outside the parent are ignored entirely, assuming they are either too
        /// far to be of significance, or operate in a larger frame of reference (e.g. the Earth
        /// moves within the gravity of the Milky Way, but when determining its movement within the
        /// solar system, the effects of the greater galaxy are not relevant).
        /// </remarks>
        public Vector3 GetTotalLocalGravity()
        {
            var totalGravity = Vector3.Zero;

            // No gravity for a parent-less object
            if (Parent == null)
            {
                return totalGravity;
            }

            foreach (var sibling in Parent.GetAllChildren<CelestialEntity>())
            {
                totalGravity += GetGravityFromObject(sibling);
            }

            return totalGravity;
        }

        /// <summary>
        /// Returns a string that represents the celestial object.
        /// </summary>
        /// <returns>A string that represents the celestial object.</returns>
        public override string ToString()
        {
            var sb = new StringBuilder(TypeName)
                .Append(" ")
                .Append(Title);
            if (Orbit?.OrbitedObject != null)
            {
                sb.Append(", orbiting ")
                    .Append(Orbit.OrbitedObject.TypeName)
                    .Append(" ")
                    .Append(Orbit.OrbitedObject.Title);
            }
            return sb.ToString();
        }

        internal double GetRocheLimit(double orbitingDensity)
            => 0.8947 * Math.Pow(Mass / orbitingDensity, 1.0 / 3.0);

        internal void Init()
        {
            ID = CelestialIDProvider.DefaultIDProvider.GetNewID();
            _seed1 = Randomizer.Instance.NextInclusiveMaxValue() * (Randomizer.Instance.NextBoolean() ? -1 : 1);
            _seed2 = Randomizer.Instance.NextInclusiveMaxValue() * (Randomizer.Instance.NextBoolean() ? -1 : 1);
            _seed3 = Randomizer.Instance.NextInclusiveMaxValue() * (Randomizer.Instance.NextBoolean() ? -1 : 1);
            _seed4 = Randomizer.Instance.NextInclusiveMaxValue() * (Randomizer.Instance.NextBoolean() ? -1 : 1);
            _seed5 = Randomizer.Instance.NextInclusiveMaxValue() * (Randomizer.Instance.NextBoolean() ? -1 : 1);
        }

        private protected virtual void GenerateLocation(CelestialRegion parent, Vector3? position = null)
            => Location = new Location(this, parent?.Location, position ?? Vector3.Zero);

        private protected virtual void GenerateSubstance() { }
    }
}
