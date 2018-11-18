using MathAndScience;
using MathAndScience.Numerics;
using MathAndScience.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UniversalTime;
using WorldFoundry.Place;

namespace WorldFoundry.Space
{
    /// <summary>
    /// A discrete region of space bound together by gravity, but not a single body; such as a galaxy or star system.
    /// </summary>
    public class CelestialRegion : Region, ICelestialLocation
    {
        private protected bool _isPrepopulated;

        /// <summary>
        /// The <see cref="CelestialRegion"/> children contained within this instance.
        /// </summary>
        public IEnumerable<ICelestialLocation> CelestialChildren => Children.OfType<ICelestialLocation>();

        /// <summary>
        /// The <see cref="CelestialRegion"/> which directly contains this <see cref="ICelestialLocation"/>.
        /// </summary>
        public CelestialRegion ContainingCelestialRegion => ContainingRegion as CelestialRegion;

        /// <summary>
        /// The <see cref="Universe"/> which contains this <see cref="ICelestialLocation"/>, if any.
        /// </summary>
        public virtual Universe ContainingUniverse => ContainingRegion is Universe universe ? universe : ContainingCelestialRegion?.ContainingUniverse;

        /// <summary>
        /// A string that uniquely identifies this <see cref="ICelestialLocation"/> for display
        /// purposes.
        /// </summary>
        public string Designation
            => string.IsNullOrEmpty(DesignatorPrefix) ? Id : $"{DesignatorPrefix} {Id}";

        private protected double? _mass;
        /// <summary>
        /// The total mass of this <see cref="ICelestialLocation"/>, in kg.
        /// </summary>
        public double Mass => _mass ?? (_mass = GetMass()).Value;

        /// <summary>
        /// An optional name for this <see cref="ICelestialLocation"/>.
        /// </summary>
        /// <remarks>
        /// Not every <see cref="ICelestialLocation"/> must have a name. They may be uniquely identified
        /// by their <see cref="Designation"/>, instead.
        /// </remarks>
        public virtual string Name { get; set; }

        /// <summary>
        /// The orbit occupied by this <see cref="ICelestialLocation"/> (may be null).
        /// </summary>
        public Orbit? Orbit { get; set; }

        private Vector3 _position;
        /// <summary>
        /// Specifies the location of this <see cref="ICelestialLocation"/>'s center in the local space
        /// of its containing <see cref="ContainingCelestialRegion"/>.
        /// </summary>
        public override Vector3 Position
        {
            get => _position;
            set
            {
                _position = value;
                if (_shape != null)
                {
                    _shape = _shape.GetCloneAtPosition(value);
                }
            }
        }

        /// <summary>
        /// Gets a radius which fully contains this <see cref="ICelestialLocation"/>, in meters.
        /// </summary>
        public double Radius => Shape.ContainingRadius;

        private protected IShape _shape;
        /// <summary>
        /// The shape of this <see cref="ICelestialLocation"/>.
        /// </summary>
        public override IShape Shape
        {
            get => _shape ?? (_shape = GetShape());
            set => _shape = value?.GetCloneAtPosition(Position);
        }

        private double? _temperature;
        /// <summary>
        /// The average temperature of this <see cref="ICelestialLocation"/>, in K.
        /// </summary>
        /// <remarks>No less than <see cref="ContainingCelestialRegion"/>'s ambient temperature.</remarks>
        public double? Temperature => Math.Max(_temperature ?? (_temperature = GetTemperature()).Value, ContainingCelestialRegion?.Temperature ?? 0);

        /// <summary>
        /// The <see cref="ICelestialLocation"/>'s <see cref="Name"/>, if it has one; otherwise its <see cref="Designation"/>.
        /// </summary>
        public string Title => Name ?? Designation;

        /// <summary>
        /// The name for this type of <see cref="ICelestialLocation"/>.
        /// </summary>
        public virtual string TypeName => BaseTypeName;

        /// <summary>
        /// Specifies the velocity of the <see cref="ICelestialLocation"/> in m/s.
        /// </summary>
        public virtual Vector3 Velocity { get; set; }

        internal virtual bool IsHospitable => true;

        private protected virtual string BaseTypeName => "Celestial Region";

        private protected virtual IEnumerable<ChildDefinition> ChildDefinitions => Enumerable.Empty<ChildDefinition>();

        private protected virtual string DesignatorPrefix => string.Empty;

        /// <summary>
        /// Initializes a new instance of <see cref="CelestialRegion"/>.
        /// </summary>
        internal CelestialRegion() { }

        /// <summary>
        /// Initializes a new instance of <see cref="CelestialRegion"/>.
        /// </summary>
        /// <param name="containingRegion">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="CelestialRegion"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="CelestialRegion"/>.</param>
        internal CelestialRegion(CelestialRegion containingRegion, Vector3 position)
            : base(containingRegion, null) => _position = position;

        /// <summary>
        /// Enumerates all the <see cref="ICelestialLocation"/> descendant children instances in this
        /// region.
        /// </summary>
        /// <returns>An <see cref="IEnumerable{T}"/> of all descendant <see cref="ICelestialLocation"/>
        /// child instances within this region.</returns>
        public IEnumerable<ICelestialLocation> GetAllCelestialChildren()
            => GetAllChildren().OfType<ICelestialLocation>();

        /// <summary>
        /// Enumerates all the <see cref="ICelestialLocation"/> descendant children instances of the
        /// given type in this region.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="ICelestialLocation"/> to retrieve.</typeparam>
        /// <returns>An <see cref="IEnumerable{T}"/> of all descendant <see cref="ICelestialLocation"/>
        /// child instances of the given type within this region.</returns>
        public IEnumerable<T> GetAllChildren<T>() where T : ICelestialLocation
            => GetAllChildren().OfType<T>();

        /// <summary>
        /// Enumerates all the <see cref="ICelestialLocation"/> descendant children instances of the
        /// given <paramref name="type"/> in this region.
        /// </summary>
        /// <param name="type">The type of <see cref="ICelestialLocation"/> to retrieve.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> of all descendant <see cref="ICelestialLocation"/>
        /// child instances of the given <paramref name="type"/> within this region.</returns>
        public IEnumerable<ICelestialLocation> GetAllChildren(Type type)
            => GetAllCelestialChildren().Where(x => type.IsAssignableFrom(x.GetType()));

        /// <summary>
        /// Calculates the total number of children in this region. The totals are approximate,
        /// based on the defined densities of the possible children it might have.
        /// </summary>
        /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="ChildDefinition"/> instances
        /// along with the total number of children present in this region (as a double rather than
        /// an integral value, due to the potentially vast numbers involved).</returns>
        public IEnumerable<(ChildDefinition type, double total)> GetChildTotals()
            => ChildDefinitions.Select(x => (x, Shape.Volume * x.Density));

        /// <summary>
        /// Calculates the force of gravity on this <see cref="ICelestialLocation"/> from another as a
        /// vector, in N.
        /// </summary>
        /// <param name="other">An <see cref="ICelestialLocation"/> from which the force gravity will
        /// be calculated.</param>
        /// <returns>
        /// The force of gravity from this <see cref="ICelestialLocation"/> to the other, in N, as a
        /// vector.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="other"/> may not be null.
        /// </exception>
        /// <exception cref="Exception">
        /// An exception will be thrown if the two <see cref="ICelestialLocation"/> instances do not
        /// share a <see cref="CelestialRegion"/> parent at some point.
        /// </exception>
        /// <remarks>
        /// Newton's law is used. General relativity would be more accurate in certain
        /// circumstances, but is considered unnecessarily intensive work for the simple simulations
        /// expected to make use of this library. If you are an astronomer performing scientifically
        /// rigorous calculations or simulations, this is not the library for you ;)
        /// </remarks>
        public Vector3 GetGravityFromObject(ICelestialLocation other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            var distance = GetDistanceTo(other);

            var scale = -ScienceConstants.G * (Mass * other.Mass / (distance * distance));

            // Get the normalized vector
            var normalized = (other.Position - Position) / distance;

            return normalized * scale;
        }

        /// <summary>
        /// Calculates the position of this <see cref="ICelestialLocation"/> at the given time,
        /// taking its orbit or velocity into account, without actually updating its current
        /// <see cref="Position"/>. Does not perform integration over time of gravitational
        /// influences not reflected by <see cref="Orbit"/>.
        /// </summary>
        /// <param name="time">The time at which to get a position.</param>
        /// <returns>A <see cref="Vector3"/> representing position relative to the center of the
        /// <see cref="ContainingCelestialRegion"/>.</returns>
        public Vector3 GetPositionAtTime(Duration time)
        {
            if (Orbit.HasValue)
            {
                var (position, _) = Orbit.Value.GetStateVectorsAtTime(time);

                if (Orbit.Value.OrbitedObject.ContainingCelestialRegion != ContainingCelestialRegion)
                {
                    return ContainingRegion.GetLocalizedPosition(Orbit.Value.OrbitedObject) + position;
                }
                else
                {
                    return Orbit.Value.OrbitedObject.Position + position;
                }
            }
            else
            {
                return Position + (Velocity * time.ToSeconds());
            }
        }

        /// <summary>
        /// Calculates the radius of a spherical region which contains at most the given amount of
        /// child entities, given the densities of the child definitions for this region.
        /// </summary>
        /// <param name="maxAmount">The maximum desired number of child entities in the
        /// region.</param>
        /// <returns>The radius of a spherical region containing at most the given amount of
        /// children, in meters.</returns>
        public double GetRadiusWithChildren(double maxAmount)
        {
            if (maxAmount.IsZero())
            {
                return 0;
            }
            var numInM3 = ChildDefinitions.Sum(x => x.Density);
            var v = maxAmount / numInM3;
            // The number in a single m³ may be so small that this goes to infinity; if so, perform
            // the calculation in reverse.
            if (double.IsInfinity(v))
            {
                var total = Shape.Volume * numInM3;
                var ratio = maxAmount / total;
                v = Shape.Volume * ratio;
            }
            return Math.Pow(3 * v / MathConstants.FourPI, 1.0 / 3.0);
        }

        /// <summary>
        /// Calculates the total force of gravity on this <see cref="ICelestialLocation"/>, in N, as a
        /// vector. Note that results may be highly inaccurate if the parent region has not been
        /// populated thoroughly enough in the vicinity of this entity (with the scale of "vicinity"
        /// depending strongly on the mass of the region's potential children).
        /// </summary>
        /// <returns>
        /// The total force of gravity on this <see cref="ICelestialLocation"/> from all
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
            if (ContainingCelestialRegion == null)
            {
                return totalGravity;
            }

            foreach (var sibling in ContainingCelestialRegion.GetAllChildren<ICelestialLocation>())
            {
                totalGravity += GetGravityFromObject(sibling);
            }

            return totalGravity;
        }

        /// <summary>
        /// Generates an appropriate population of child entities in the given <paramref
        /// name="region"/>.
        /// </summary>
        /// <param name="region">A <see cref="Region"/> representing an area of local space.</param>
        /// <remarks>
        /// <para>
        /// If the given <paramref name="region"/> is not within this instance's local space,
        /// nothing happens.
        /// </para>
        /// <para>
        /// If the region is small and the density of a given child is low enough that the number
        /// indicated to be present is less than one, that value is taken as the probability that
        /// one will be found instead.
        /// </para>
        /// <para>
        /// If the region is large and the density of a given child is high enough that the number
        /// of children indicated exceeds the number of actual child instances a region can maintain
        /// (<see cref="int.MaxValue"/> for all child instances), the number will be truncated at
        /// the maximum allowable value. To avoid memory issues, even this outcome should be avoided
        /// by putting constraints on calling code to ensure that regions to be populated are not so
        /// large that the number of children is excessive (<see
        /// cref="GetRadiusWithChildren(double)"/> can be used to determine an appropriate region
        /// size).
        /// </para>
        /// </remarks>
        public virtual void PopulateRegion(Region region)
        {
            if (region.GetCommonContainingRegion(this) != this)
            {
                return;
            }
            if (!_isPrepopulated)
            {
                PrepopulateRegion();
            }
            foreach (var childLocation in region.Children.OfType<Region>())
            {
                PopulateRegion(childLocation);
            }
            foreach (var child in ChildDefinitions)
            {
                var number = Math.Min(region.Shape.Volume * child.Density, int.MaxValue - CelestialChildren.Count() - 1);
                if (number < 1 && Randomizer.Instance.NextDouble() <= number)
                {
                    number = 1;
                }
                number -= GetAllChildren(child.Type).Count(x => region.Contains(x));
                for (var i = 0; i < number; i++)
                {
                    GenerateChild(child);
                }
            }
        }

        /// <summary>
        /// Generates an appropriate population of child entities in this <see
        /// cref="CelestialRegion"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If the region is small and the density of a given child is low enough that the number
        /// indicated to be present is less than one, that value is taken as the probability that
        /// one will be found instead.
        /// </para>
        /// <para>
        /// If the region is large and the density of a given child is high enough that the number
        /// of children indicated exceeds the number of actual child instances a region can maintain
        /// (<see cref="int.MaxValue"/> for all child instances), the number will be truncated at
        /// the maximum allowable value. To avoid memory issues, even this outcome should be avoided
        /// by putting constraints on calling code to ensure that regions to be populated are not so
        /// large that the number of children is excessive (<see
        /// cref="GetRadiusWithChildren(double)"/> and <see cref="PopulateRegion(Region)"/> can be
        /// used to populate only a sub-region of an appropriate size).
        /// </para>
        /// </remarks>
        public virtual void PopulateRegion()
        {
            if (!_isPrepopulated)
            {
                PrepopulateRegion();
            }
            foreach (var child in ChildDefinitions)
            {
                var number = Math.Min(Shape.Volume * child.Density, int.MaxValue - CelestialChildren.Count() - 1);
                if (number < 1 && Randomizer.Instance.NextDouble() <= number)
                {
                    number = 1;
                }
                number -= GetAllChildren(child.Type).Count();
                for (var i = 0; i < number; i++)
                {
                    GenerateChild(child);
                }
            }
        }

        /// <summary>
        /// Removes the given child <see cref="ICelestialLocation"/> from this instance. Does nothing
        /// if the given instance is <see langword="null"/> or is not a child of this one.
        /// </summary>
        /// <param name="child">A <see cref="ICelestialLocation"/> to remove as a child of this
        /// one.</param>
        public void RemoveChild(ICelestialLocation child) => child?.SetNewContainingRegion(null);

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
                    .Append(Orbit.Value.OrbitedObject.TypeName)
                    .Append(" ")
                    .Append(Orbit.Value.OrbitedObject.Title);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Updates the position and velocity of this object to correspond with the state predicted
        /// by its <see cref="Orbit"/> at the current time of its containing <see cref="Universe"/>,
        /// assuming no influences on the body's motion have occurred aside from its orbit. Has no
        /// effect if the body is not in orbit.
        /// </summary>
        public void UpdateOrbit()
        {
            var universe = ContainingUniverse;
            if (!Orbit.HasValue || universe == null)
            {
                return;
            }

            var (position, velocity) = Orbit.Value.GetStateVectorsAtTime(universe.Time.Now);

            if (Orbit.Value.OrbitedObject.ContainingCelestialRegion != ContainingCelestialRegion)
            {
                Position = ContainingRegion.GetLocalizedPosition(Orbit.Value.OrbitedObject) + position;
            }
            else
            {
                Position = Orbit.Value.OrbitedObject.Position + position;
            }

            Velocity = velocity;
        }

        /// <summary>
        /// Updates the position and velocity of this object to correspond with the state predicted
        /// by its <see cref="Orbit"/> after the specified number of seconds since its orbit's epoch
        /// (initial time of pericenter), assuming no influences on the body's motion have occurred
        /// aside from its orbit. Has no effect if the body is not in orbit.
        /// </summary>
        /// <param name="elapsedSeconds">
        /// The number of seconds which have elapsed since the orbit's defining epoch (time of
        /// pericenter).
        /// </param>
        public void UpdateOrbit(double elapsedSeconds)
        {
            if (!Orbit.HasValue)
            {
                return;
            }

            var (position, velocity) = Orbit.Value.GetStateVectorsAtTime(elapsedSeconds);

            if (Orbit.Value.OrbitedObject.ContainingCelestialRegion != ContainingCelestialRegion)
            {
                Position = ContainingRegion.GetLocalizedPosition(Orbit.Value.OrbitedObject) + position;
            }
            else
            {
                Position = Orbit.Value.OrbitedObject.Position + position;
            }

            Velocity = velocity;
        }

        internal virtual void PrepopulateRegion() => _isPrepopulated = true;

        internal virtual ICelestialLocation GenerateChild(ChildDefinition definition)
            => definition.GenerateChild(this);

        private protected virtual double GetMass() => 0;

        private protected virtual IShape GetShape() => new SinglePoint(Position);

        private protected virtual double GetTemperature() => 0;
    }
}
