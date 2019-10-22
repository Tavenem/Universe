using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Text;
using WorldFoundry.CelestialBodies.Stars;
using WorldFoundry.Place;
using NeverFoundry.MathAndScience;
using NeverFoundry.MathAndScience.Chemistry;
using NeverFoundry.MathAndScience.Constants.Numbers;
using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.MathAndScience.Randomization;
using NeverFoundry.MathAndScience.Time;

namespace WorldFoundry.Space
{
    /// <summary>
    /// A place in a universe, with a location that defines its position, and a shape that defines
    /// its extent, as well as a mass, density, and temperature. It may or may not also have a
    /// particular chemical composition.
    /// </summary>
    /// <remarks>
    /// Locations can exist in a hierarchy. Any location may contain other locations, and be
    /// contained by a location in turn. The relative positions of locations within the same
    /// hierarchy can be analyzed using the methods available on this class.
    /// </remarks>
    [Serializable]
    public class CelestialLocation : Location
    {
        private protected bool _isPrepopulated;

        private protected double? _albedo;
        /// <summary>
        /// The average albedo of this celestial entity (a value between 0 and 1).
        /// </summary>
        /// <remarks>
        /// This refers to the total albedo of the body, including any atmosphere, not just the
        /// surface albedo of the main body.
        /// </remarks>
        public double Albedo
        {
            get
            {
                if (!_albedo.HasValue)
                {
                    GenerateAlbedo();
                }
                return _albedo ?? 0;
            }
            set
            {
                _albedo = value;
                ResetCachedTemperatures();
            }
        }

        /// <summary>
        /// The total temperature of this body averaged over its orbit (if any).
        /// </summary>
        public virtual double AverageSurfaceTemperature => AverageBlackbodyTemperature;

        private double? _averageBlackbodyTemperature;
        /// <summary>
        /// The total temperature of this body averaged over its orbit (if any).
        /// </summary>
        public double AverageBlackbodyTemperature
            => _averageBlackbodyTemperature ??= GetAverageBlackbodyTemperature();

        private double? _blackbodyTemperature;
        /// <summary>
        /// The total temperature of this body.
        /// </summary>
        public double BlackbodyTemperature
            => _blackbodyTemperature ?? (_blackbodyTemperature = GetTemperatureAtPosition(Position)).Value;

        /// <summary>
        /// The <see cref="CelestialLocation"/> children contained within this instance.
        /// </summary>
        public IEnumerable<CelestialLocation> CelestialChildren => Children.OfType<CelestialLocation>();

        /// <summary>
        /// The <see cref="CelestialLocation"/> which directly contains this instance.
        /// </summary>
        public CelestialLocation? CelestialParent => Parent as CelestialLocation;

        /// <summary>
        /// The <see cref="ContainingUniverse"/> which contains this <see cref="CelestialLocation"/>, if any.
        /// </summary>
        public virtual Universe? ContainingUniverse
            => Parent is Universe universe
            ? universe
            : (Parent as CelestialLocation)?.ContainingUniverse;

        /// <summary>
        /// <para>
        /// The average density of this material, in kg/m³.
        /// </para>
        /// <para>
        /// A material may have either uniform or uneven density (e.g. contained voids or an
        /// irregular shape contained within its overall dimensions). This value represents the
        /// average throughout the full volume of its <see cref="IMaterial.Shape"/>.
        /// </para>
        /// </summary>
        public double Density
        {
            get => Material.Density;
            set
            {
                Material.Density = value;
                _surfaceGravity = null;
            }
        }

        /// <summary>
        /// A string that uniquely identifies this <see cref="CelestialLocation"/> for display
        /// purposes.
        /// </summary>
        public string Designation
            => string.IsNullOrEmpty(DesignatorPrefix)
                ? Id
                : $"{DesignatorPrefix} {Id}";

        /// <summary>
        /// The mass of this material, in kg.
        /// </summary>
        public Number Mass
        {
            get => Material.Mass;
            set
            {
                Material.Mass = value;
                _surfaceGravity = null;
            }
        }

        private protected IMaterial? _material;
        /// <summary>
        /// The physical material which comprises this location.
        /// </summary>
        public virtual IMaterial Material
        {
            get => _material ??= GetMaterial();
            set => _material = value;
        }

        /// <summary>
        /// An optional name for this <see cref="CelestialLocation"/>.
        /// </summary>
        /// <remarks>
        /// Not every <see cref="CelestialLocation"/> must have a name. They may be uniquely identified
        /// by their <see cref="Designation"/>, instead.
        /// </remarks>
        public virtual string? Name { get; set; }

        private protected Orbit? _orbit;
        /// <summary>
        /// The orbit occupied by this <see cref="CelestialLocation"/> (may be null).
        /// </summary>
        public virtual Orbit? Orbit
        {
            get => _orbit;
            set
            {
                _orbit = value;
                ResetCachedTemperatures();
            }
        }

        private Vector3 _position;
        /// <summary>
        /// The position of this location relative to the center of its <see
        /// cref="Location.Parent"/>.
        /// </summary>
        public override Vector3 Position
        {
            get => _position;
            set
            {
                _position = value;
                _blackbodyTemperature = null;
                if (!(_material is null))
                {
                    _material.Shape = _material.Shape.GetCloneAtPosition(value);
                }
            }
        }

        /// <summary>
        /// The shape of this location.
        /// </summary>
        public override IShape Shape
        {
            get => Material.Shape;
            set
            {
                Material.Shape = value.GetCloneAtPosition(Position);
                _radiusSquared = null;
                _surfaceGravity = null;
            }
        }

        private protected Number? _surfaceGravity;
        /// <summary>
        /// The average force of gravity at the surface of this object, in N.
        /// </summary>
        public Number SurfaceGravity => _surfaceGravity ??= Material.GetSurfaceGravity();

        /// <summary>
        /// The average temperature of this material, in K. May be <see langword="null"/>,
        /// indicating that it is at the ambient temperature of its environment.
        /// </summary>
        /// <remarks>
        /// No less than the ambient temperature of its <see cref="CelestialParent"/>, if any.
        /// </remarks>
        public double? Temperature
        {
            get => Math.Max(Material.Temperature ?? 0, CelestialParent?.Temperature ?? 0);
            set => Material.Temperature = value;
        }

        /// <summary>
        /// The <see cref="CelestialLocation"/>'s <see cref="Name"/>, if it has one; otherwise its
        /// <see cref="Designation"/>.
        /// </summary>
        public string Title => Name ?? Designation;

        /// <summary>
        /// The name for this type of <see cref="CelestialLocation"/>.
        /// </summary>
        public virtual string TypeName => BaseTypeName;

        /// <summary>
        /// The velocity of the <see cref="CelestialLocation"/> in m/s.
        /// </summary>
        public virtual Vector3 Velocity { get; set; }

        internal virtual bool IsHospitable => CelestialParent?.IsHospitable ?? true;

        private Number? _radiusSquared;
        internal Number RadiusSquared => _radiusSquared ??= Shape.ContainingRadius.Square();

        private double? _surfaceTemperatureAtApoapsis;
        /// <summary>
        /// The total temperature of this body when at the apoapsis of its orbit (if any).
        /// </summary>
        internal double SurfaceTemperatureAtApoapsis => _surfaceTemperatureAtApoapsis ??= GetTemperatureAtApoapsis();

        private double? _surfaceTemperatureAtPeriapsis;
        /// <summary>
        /// The total temperature of this body when at the periapsis of its orbit (if any).
        /// </summary>
        internal double SurfaceTemperatureAtPeriapsis => _surfaceTemperatureAtPeriapsis ??= GetTemperatureAtPeriapsis();

        private protected virtual string BaseTypeName => "Celestial Location";

        private protected virtual IEnumerable<ChildDefinition> ChildDefinitions => Enumerable.Empty<ChildDefinition>();

        private protected virtual string DesignatorPrefix => string.Empty;

        private protected override bool HasDefinedShape => !(_material is null);

        /// <summary>
        /// Initializes a new instance of <see cref="CelestialLocation"/>.
        /// </summary>
        internal CelestialLocation() { }

        /// <summary>
        /// Initializes a new instance of <see cref="CelestialLocation"/>.
        /// </summary>
        /// <param name="parent">The location which contains this one.</param>
        /// <param name="position">The position of the location relative to the center of its
        /// <paramref name="parent"/>.</param>
        public CelestialLocation(Location? parent, Vector3 position) : base(parent) => Position = position;

        private protected CelestialLocation(
            string id,
            string? name,
            bool isPrepopulated,
            double? albedo,
            Vector3 velocity,
            Orbit? orbit,
            IMaterial? material,
            List<Location>? children) : base(id, children)
        {
            Id = id;
            Name = name;
            _isPrepopulated = isPrepopulated;
            _albedo = albedo;
            _orbit = orbit;
            _material = material;
            Velocity = velocity;
        }

        private CelestialLocation(SerializationInfo info, StreamingContext context) : this(
            (string)info.GetValue(nameof(Id), typeof(string)),
            (string?)info.GetValue(nameof(Name), typeof(string)),
            (bool)info.GetValue(nameof(_isPrepopulated), typeof(bool)),
            (double?)info.GetValue(nameof(Albedo), typeof(double?)),
            (Vector3)info.GetValue(nameof(Velocity), typeof(Vector3)),
            (Orbit?)info.GetValue(nameof(Orbit), typeof(Orbit?)),
            (IMaterial?)info.GetValue(nameof(Material), typeof(IMaterial)),
            (List<Location>)info.GetValue(nameof(Children), typeof(List<Location>))) { }

        /// <summary>
        /// Calculates the total number of children in this region. The totals are approximate,
        /// based on the defined densities of the possible children it might have.
        /// </summary>
        /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="ChildDefinition"/> instances
        /// along with the total number of children present in this region (as a <see
        /// cref="NeverFoundry.MathAndScience.Numerics.Number"/> due to the potentially vast numbers
        /// involved).</returns>
        public IEnumerable<(ChildDefinition type, Number total)> GetChildTotals()
            => ChildDefinitions.Select(x => (x, Shape.Volume * x.Density));

        /// <summary>
        /// Calculates the escape velocity from this location, in m/s.
        /// </summary>
        /// <returns>The escape velocity from this location, in m/s.</returns>
        public Number GetEscapeVelocity() => Number.Sqrt(ScienceConstants.TwoG * Mass / Shape.ContainingRadius);

        /// <summary>
        /// Calculates the force of gravity on this <see cref="CelestialLocation"/> from another as
        /// a vector, in N.
        /// </summary>
        /// <param name="other">A <see cref="CelestialLocation"/> from which the force gravity will
        /// be calculated. If <see langword="null"/>, or if the two do not share a common parent,
        /// the result will be zero.</param>
        /// <returns>
        /// The force of gravity from this <see cref="CelestialLocation"/> to the other, in N, as a
        /// vector.
        /// </returns>
        /// <exception cref="Exception">
        /// An exception will be thrown if the two <see cref="CelestialLocation"/> instances do not
        /// share a <see cref="Location"/> parent at some point.
        /// </exception>
        /// <remarks>
        /// Newton's law is used. General relativity would be more accurate in certain
        /// circumstances, but is considered unnecessarily intensive work for the simple simulations
        /// expected to make use of this library. If you are an astronomer performing scientifically
        /// rigorous calculations or simulations, this is not the library for you ;)
        /// </remarks>
        public Vector3 GetGravityFromObject(CelestialLocation other)
        {
            if (other == null)
            {
                return Vector3.Zero;
            }

            var distance = GetDistanceTo(other);

            if (distance.IsFinite)
            {
                return Vector3.Zero;
            }

            var scale = -ScienceConstants.G * (Mass * other.Mass / (distance * distance));

            // Get the normalized vector
            var normalized = (other.Position - Position) / distance;

            return normalized * scale;
        }

        /// <summary>
        /// Calculates the total luminous flux incident on this body from nearby sources of light
        /// (stars in the same system), in lumens.
        /// </summary>
        /// <returns>The total illumination on the body, in lumens.</returns>
        /// <remarks>
        /// A conversion of 0.0079 W/m² per lumen is used, which is roughly accurate for the sun,
        /// but may not be as precise for other stellar bodies.
        /// </remarks>
        public double GetLuminousFlux()
            => CelestialParent?.GetAllChildren<Star>()
            .Sum(x => (double)(x.Luminosity / (MathConstants.FourPI * GetDistanceSquaredTo(x))) / 0.0079) ?? 0;

        /// <summary>Populates a <see cref="SerializationInfo"></see> with the data needed to
        /// serialize the target object.</summary>
        /// <param name="info">The <see cref="SerializationInfo"></see> to populate with
        /// data.</param>
        /// <param name="context">The destination (see <see cref="StreamingContext"></see>) for this
        /// serialization.</param>
        /// <exception cref="System.Security.SecurityException">The caller does not have the
        /// required permission.</exception>
        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(Id), Id);
            info.AddValue(nameof(Name), Name);
            info.AddValue(nameof(_isPrepopulated), _isPrepopulated);
            info.AddValue(nameof(Albedo), _albedo);
            info.AddValue(nameof(Velocity), Velocity);
            info.AddValue(nameof(Orbit), _orbit);
            info.AddValue(nameof(Material), _material);
            info.AddValue(nameof(Children), Children.ToList());
        }

        /// <summary>
        /// Calculates the position of this <see cref="CelestialLocation"/> at the given time,
        /// taking its orbit or velocity into account, without actually updating its current
        /// <see cref="Position"/>. Does not perform integration over time of gravitational
        /// influences not reflected by <see cref="Orbit"/>.
        /// </summary>
        /// <param name="time">The time at which to get a position.</param>
        /// <returns>A <see cref="Vector3"/> representing position relative to the center of the
        /// <see cref="CelestialParent"/>.</returns>
        public Vector3 GetPositionAtTime(Duration time)
        {
            if (Orbit.HasValue)
            {
                var (position, _) = Orbit.Value.GetStateVectorsAtTime(time);

                if (Orbit.Value.OrbitedObject.CelestialParent != CelestialParent)
                {
                    return Parent == null
                        ? position
                        : Parent.GetLocalizedPosition(Orbit.Value.OrbitedObject) + position;
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
        public Number GetRadiusWithChildren(Number maxAmount)
        {
            if (maxAmount.IsZero)
            {
                return 0;
            }
            var numInM3 = ChildDefinitions.Sum(x => x.Density);
            var v = maxAmount / numInM3;
            // The number in a single m³ may be so small that this goes to infinity; if so, perform
            // the calculation in reverse.
            if (v.IsInfinite)
            {
                var total = Shape.Volume * numInM3;
                var ratio = maxAmount / total;
                v = Shape.Volume * ratio;
            }
            return (3 * v / MathConstants.FourPI).CubeRoot();
        }

        /// <summary>
        /// Calculates the total force of gravity on this <see cref="CelestialLocation"/>, in N, as
        /// a vector. Note that results may be highly inaccurate if the parent region has not been
        /// populated thoroughly enough in the vicinity of this entity (with the scale of "vicinity"
        /// depending strongly on the mass of the region's potential children).
        /// </summary>
        /// <returns>
        /// The total force of gravity on this <see cref="CelestialLocation"/> from all
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
            if (CelestialParent is null)
            {
                return totalGravity;
            }

            foreach (var sibling in CelestialParent.GetAllChildren<CelestialLocation>())
            {
                totalGravity += GetGravityFromObject(sibling);
            }

            return totalGravity;
        }

        /// <summary>
        /// Generates an appropriate population of child entities in the given <paramref
        /// name="location"/>.
        /// </summary>
        /// <param name="location">A <see cref="Location"/> representing an area of local
        /// space.</param>
        /// <remarks>
        /// <para>
        /// If the given <paramref name="location"/> is not within this instance's local space,
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
        /// cref="GetRadiusWithChildren(NeverFoundry.MathAndScience.Numerics.Number)"/> can be used to
        /// determine an appropriate region size).
        /// </para>
        /// </remarks>
        public virtual void PopulateRegion(Location location)
        {
            if (location.GetCommonParent(this) != this)
            {
                return;
            }
            if (!_isPrepopulated)
            {
                PrepopulateRegion();
            }
            foreach (var childLocation in location.Children)
            {
                PopulateRegion(childLocation);
            }
            foreach (var child in ChildDefinitions)
            {
                var number = Number.Min(location.Shape.Volume * child.Density, int.MaxValue - CelestialChildren.Count() - 1);
                if (number < 1 && Randomizer.Instance.NextNumber() <= number)
                {
                    number = 1;
                }
                number -= GetAllChildren(child.Type).Count(x => location.Contains(x));
                for (var i = 0; i < number; i++)
                {
                    GenerateChild(child);
                }
            }
        }

        /// <summary>
        /// Generates an appropriate population of child entities in this <see
        /// cref="CelestialLocation"/>.
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
        /// cref="GetRadiusWithChildren(NeverFoundry.MathAndScience.Numerics.Number)"/> and <see
        /// cref="PopulateRegion(Location)"/> can be used to populate only a sub-region of an
        /// appropriate size).
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
                var number = Number.Min(Shape.Volume * child.Density, int.MaxValue - CelestialChildren.Count() - 1);
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
        /// by its <see cref="Orbit"/> at the current time of its containing <see
        /// cref="Space.Universe"/>, assuming no influences on the body's motion have occurred aside
        /// from its orbit. Has no effect if the body is not in orbit.
        /// </summary>
        public void UpdateOrbit()
        {
            var universe = ContainingUniverse;
            if (!Orbit.HasValue || universe == null)
            {
                return;
            }

            var (position, velocity) = Orbit.Value.GetStateVectorsAtTime(universe.Time.Now);

            if (Orbit.Value.OrbitedObject.CelestialParent != CelestialParent)
            {
                Position = Parent == null
                    ? position
                    : Parent.GetLocalizedPosition(Orbit.Value.OrbitedObject) + position;
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
        public void UpdateOrbit(Number elapsedSeconds)
        {
            if (!Orbit.HasValue)
            {
                return;
            }

            var (position, velocity) = Orbit.Value.GetStateVectorsAtTime(elapsedSeconds);

            if (Orbit.Value.OrbitedObject.CelestialParent != CelestialParent)
            {
                Position = Parent == null
                    ? position
                    : Parent.GetLocalizedPosition(Orbit.Value.OrbitedObject) + position;
            }
            else
            {
                Position = Orbit.Value.OrbitedObject.Position + position;
            }

            Velocity = velocity;
        }

        internal virtual void PrepopulateRegion() => _isPrepopulated = true;

        internal virtual CelestialLocation? GenerateChild(ChildDefinition definition)
            => definition.GenerateChild(this);

        internal virtual void GenerateOrbit(CelestialLocation orbitedObject)
        {
            if (orbitedObject == null)
            {
                return;
            }

            Space.Orbit.SetCircularOrbit(this, orbitedObject);
        }

        internal Number GetHillSphereRadius() => Orbit?.GetHillSphereRadius(this) ?? 0;

        /// <summary>
        /// Approximates the radius of the orbiting body's mutual Hill sphere with another orbiting
        /// body in orbit around the same primary, in meters.
        /// </summary>
        /// <remarks>
        /// Assumes the semimajor axis of both orbits is identical for the purposes of the
        /// calculation, which obviously would not be the case, but generates reasonably close
        /// estimates in the absence of actual values.
        /// </remarks>
        /// <param name="otherMass">
        /// The mass of another celestial body presumed to be orbiting the same primary as this one.
        /// </param>
        /// <returns>The radius of the orbiting body's Hill sphere, in meters.</returns>
        internal Number GetMutualHillSphereRadius(Number otherMass)
            => Orbit?.GetMutualHillSphereRadius(this, otherMass) ?? 0;

        internal Number GetRocheLimit(Number orbitingDensity)
            => new Number(8947, -4) * (Mass / orbitingDensity).CubeRoot();

        internal Number GetSphereOfInfluenceRadius()
            => Orbit?.GetSphereOfInfluenceRadius(this) ?? 0;

        private protected virtual void GenerateAlbedo() => Albedo = 0;

        /// <summary>
        /// Calculates the temperature of the location, averaged over its orbit,
        /// in K.
        /// </summary>
        private double GetAverageBlackbodyTemperature()
            => Orbit.HasValue
                ? ((SurfaceTemperatureAtPeriapsis * (1 + Orbit.Value.Eccentricity)) + (SurfaceTemperatureAtApoapsis * (1 - Orbit.Value.Eccentricity))) / 2
                : GetTemperatureAtPosition(Position);

        private protected virtual IMaterial GetComposition(double density, Number mass, IShape shape, double? temperature)
        {
            var substance = GetSubstance();
            return !(substance is null)
                ? new Material(substance, density, mass, shape, temperature)
                : new Material(density, mass, shape, temperature);
        }

        /// <summary>
        /// Calculates the heat added to this location by insolation at the given position, in K.
        /// </summary>
        /// <param name="position">
        /// A hypothetical position for this location at which the heat of insolation will be
        /// calculated.
        /// </param>
        /// <returns>
        /// The heat added to this location by insolation at the given position, in K.
        /// </returns>
        private protected virtual double GetInsolationHeat(Vector3 position)
        {
            if (CelestialParent is null)
            {
                return 0;
            }
            else
            {
                var relativePosition = GetLocalizedPosition(CelestialParent, position);
                return Math.Pow(1 - Albedo, 0.25) * CelestialParent
                  .GetAllChildren<Star>()
                  .Where(x => x != this)
                  .Sum(x => (x.Temperature ?? 0) * (double)Number.Sqrt(x.Shape.ContainingRadius / (2 * GetDistanceFromPositionTo(relativePosition, x))));
            }
        }

        private protected virtual IMaterial GetMaterial()
        {
            var (density, mass, shape) = GetMatter();
            return GetComposition(density, mass, shape, GetTemperature());
        }

        private protected virtual (double density, Number mass, IShape shape) GetMatter()
        {
            var mass = GetMass();
            var shape = GetShape();
            return ((double)(mass / shape.Volume), mass, shape);
        }

        /// <summary>
        /// Calculates the total average temperature of the location as if this object was at the
        /// apoapsis of its orbit, in K.
        /// </summary>
        /// <remarks>
        /// Uses current position if this object is not in an orbit, or if its apoapsis is infinite.
        /// </remarks>
        private double GetTemperatureAtApoapsis()
            => GetTemperatureAtPosition(!Orbit.HasValue || Orbit.Value.Apoapsis.IsInfinite
                ? Position
                : Orbit.Value.OrbitedObject.Position + (Vector3.UnitX * Orbit.Value.Apoapsis)); // Actual position doesn't matter for temperature, only distance.

        /// <summary>
        /// Calculates the total average temperature of the location as if this object was at the
        /// periapsis of its orbit, in K.
        /// </summary>
        /// <remarks>
        /// Uses current position if this object is not in an orbit.
        /// </remarks>
        private double GetTemperatureAtPeriapsis()
            => GetTemperatureAtPosition(Orbit.HasValue
                ? Orbit.Value.OrbitedObject.Position + (Vector3.UnitX * Orbit.Value.Periapsis) // Actual position doesn't matter for temperature, only distance.
                : Position);

        /// <summary>
        /// Calculates the total average temperature of the location as if this object was at the
        /// specified position, including ambient heat of its parent and radiated heat from all
        /// sibling objects, in K.
        /// </summary>
        /// <param name="position">
        /// A hypothetical position for this location at which its temperature will be calculated.
        /// </param>
        /// <returns>
        /// The total average temperature of the location at the given position, in K.
        /// </returns>
        private protected double GetTemperatureAtPosition(Vector3 position)
            => (Temperature ?? 0) + GetInsolationHeat(position);

        /// <summary>
        /// Estimates the total average temperature of the location as if this object was at the
        /// specified true anomaly in its orbit, including ambient heat of its parent and radiated
        /// heat from all sibling objects, in K. If the body is not in orbit, returns the
        /// temperature at its current position.
        /// </summary>
        /// <param name="trueAnomaly">
        /// A true anomaly at which its temperature will be calculated.
        /// </param>
        /// <returns>
        /// The total average temperature of the location at the given position, in K.
        /// </returns>
        /// <remarks>
        /// The estimation is performed by linear interpolation between the temperature at periapsis
        /// and apoapsis, which is not necessarily accurate for highly elliptical orbits, or bodies
        /// with multiple significant nearby heat sources, but it should be fairly accurate for
        /// bodies in fairly circular orbits around heat sources which are all close to the center
        /// of the orbit, and much faster for successive calls than calculating the temperature at
        /// specific positions precisely.
        /// </remarks>
        private protected double GetTemperatureAtTrueAnomaly(double trueAnomaly)
            => SurfaceTemperatureAtPeriapsis.Lerp(SurfaceTemperatureAtApoapsis, trueAnomaly <= Math.PI ? trueAnomaly / Math.PI : 2 - (trueAnomaly / Math.PI));

        private protected virtual double GetDensity() => 0;

        private protected virtual Number GetMass() => Number.Zero;

        private protected virtual IShape GetShape() => new SinglePoint(Position);

        private protected virtual ISubstanceReference? GetSubstance() => null;

        private protected virtual double? GetTemperature() => null;

        private protected virtual void ResetCachedTemperatures()
        {
            _averageBlackbodyTemperature = null;
            _blackbodyTemperature = null;
            _surfaceTemperatureAtApoapsis = null;
            _surfaceTemperatureAtPeriapsis = null;
        }
    }
}
