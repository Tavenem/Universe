using MathAndScience;
using MathAndScience.Numerics;
using MathAndScience.Shapes;
using Substances;
using System;
using System.Linq;
using System.Text;
using WorldFoundry.CelestialBodies.Stars;
using WorldFoundry.Place;
using WorldFoundry.Space;

namespace WorldFoundry.CelestialBodies
{
    /// <summary>
    /// Represents any contiguous physical object in space, such as a star or planet.
    /// </summary>
    public class CelestialBody : Location, ICelestialLocation
    {
        private double? _albedo;
        /// <summary>
        /// The average albedo of the <see cref="CelestialBody"/> (a value between 0 and 1).
        /// </summary>
        /// <remarks>
        /// This refers to the total albedo of the body, including any atmosphere, not just
        /// the surface albedo of the main body.
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
        public virtual double AverageSurfaceTemperature => AverageBlackbodySurfaceTemperature;

        private double? _averageBlackbodySurfaceTemperature;
        /// <summary>
        /// The total temperature of this body averaged over its orbit (if any).
        /// </summary>
        public double AverageBlackbodySurfaceTemperature
            => _averageBlackbodySurfaceTemperature ?? (_averageBlackbodySurfaceTemperature = GetAverageBlackbodySurfaceTemperature()).Value;

        private double? _blackbodySurfaceTemperature;
        /// <summary>
        /// The total temperature of this body.
        /// </summary>
        public double BlackbodySurfaceTemperature
            => _blackbodySurfaceTemperature ?? (_blackbodySurfaceTemperature = GetSurfaceTemperatureAtPosition(Position)).Value;

        /// <summary>
        /// The <see cref="CelestialRegion"/> which directly contains this <see cref="ICelestialLocation"/>.
        /// </summary>
        public CelestialRegion ContainingCelestialRegion => ContainingRegion as CelestialRegion;

        /// <summary>
        /// A string that uniquely identifies this <see cref="ICelestialLocation"/> for display
        /// purposes.
        /// </summary>
        public string Designation
            => string.IsNullOrEmpty(DesignatorPrefix) ? Id : $"{DesignatorPrefix} {Id}";

        /// <summary>
        /// The total mass of this <see cref="ICelestialLocation"/>, in kg.
        /// </summary>
        public double Mass => Substance.Mass;

        /// <summary>
        /// An optional name for this <see cref="ICelestialLocation"/>.
        /// </summary>
        /// <remarks>
        /// Not every <see cref="ICelestialLocation"/> must have a name. They may be uniquely identified
        /// by their <see cref="Designation"/>, instead.
        /// </remarks>
        public string Name { get; set; }

        private Orbit? _orbit;
        /// <summary>
        /// The orbit occupied by this <see cref="ICelestialLocation"/> (may be null).
        /// </summary>
        public Orbit? Orbit
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
        /// Specifies the location of this <see cref="ICelestialLocation"/>'s center in the local space
        /// of its containing <see cref="ContainingCelestialRegion"/>.
        /// </summary>
        public override Vector3 Position
        {
            get => _position;
            set
            {
                _position = value;
                if (_substance != null)
                {
                    _substance.Shape = _substance.Shape.GetCloneAtPosition(value);
                }
            }
        }

        /// <summary>
        /// The shape of this <see cref="ICelestialLocation"/>.
        /// </summary>
        public IShape Shape
        {
            get => Substance.Shape;
            set
            {
                Substance.Shape = value.GetCloneAtPosition(Position);
                _radiusSquared = null;
                _surfaceGravity = null;
            }
        }

        private Substance _substance;
        /// <summary>
        /// The substance which represents the physical form of this <see cref="CelestialBody"/>.
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
                        _substance = new Substance { Composition = Material.Empty, Shape = new SinglePoint(Position) };
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

        private protected double? _surfaceGravity;
        /// <summary>
        /// The average force of gravity at the surface of this <see cref="CelestialBody"/>, in N.
        /// </summary>
        public double SurfaceGravity
            => _surfaceGravity ?? (_surfaceGravity = Substance.GetSurfaceGravity()).Value;

        /// <summary>
        /// The average temperature of this <see cref="ICelestialLocation"/>, in K.
        /// </summary>
        /// <remarks>No less than the ambient temperature of its <see
        /// cref="ContainingCelestialRegion"/>.</remarks>
        public double? Temperature
            => Math.Max(Substance.Temperature, ContainingCelestialRegion?.Temperature ?? 0);

        /// <summary>
        /// The <see cref="ICelestialLocation"/>'s <see cref="Name"/>, if it has one; otherwise its <see cref="Designation"/>.
        /// </summary>
        public string Title => Name ?? Designation;

        /// <summary>
        /// The name for this type of <see cref="ICelestialLocation"/>.
        /// </summary>
        public virtual string TypeName => BaseTypeName;

        /// <summary>
        /// Specifies the velocity of the <see cref="ICelestialLocation"/>.
        /// </summary>
        public Vector3 Velocity { get; set; }

        internal virtual bool IsHospitable => ContainingCelestialRegion?.IsHospitable ?? true;

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

        private double? _surfaceTemperatureAtApoapsis;
        /// <summary>
        /// The total temperature of this body when at the apoapsis of its orbit (if any).
        /// </summary>
        internal double SurfaceTemperatureAtApoapsis
            => _surfaceTemperatureAtApoapsis ?? (_surfaceTemperatureAtApoapsis = GetSurfaceTemperatureAtApoapsis()).Value;

        private double? _surfaceTemperatureAtPeriapsis;
        /// <summary>
        /// The total temperature of this body when at the periapsis of its orbit (if any).
        /// </summary>
        internal double SurfaceTemperatureAtPeriapsis
            => _surfaceTemperatureAtPeriapsis ?? (_surfaceTemperatureAtPeriapsis = GetSurfaceTemperatureAtPeriapsis()).Value;

        private protected virtual string BaseTypeName => "Celestial Object";

        private protected virtual string DesignatorPrefix => string.Empty;

        /// <summary>
        /// Initializes a new instance of <see cref="CelestialBody"/>.
        /// </summary>
        internal CelestialBody() { }

        /// <summary>
        /// Initializes a new instance of <see cref="CelestialBody"/> with the given parameters.
        /// </summary>
        /// <param name="containingRegion">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="CelestialBody"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="CelestialBody"/>.</param>
        internal CelestialBody(CelestialRegion containingRegion, Vector3 position) : base(containingRegion, position) { }

        /// <summary>
        /// Calculates the escape velocity from this body, in m/s.
        /// </summary>
        /// <returns>The escape velocity from this body, in m/s.</returns>
        public double GetEscapeVelocity() => Math.Sqrt(ScienceConstants.TwoG * Mass / Shape.ContainingRadius);

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
        /// Updates the orbital position and velocity of this object's <see cref="Orbit"/> after the
        /// specified number of seconds have passed, assuming no influences on the body's motion
        /// have occurred aside from its orbit. Has no effect if the body is not in orbit.
        /// </summary>
        /// <param name="elapsedSeconds">
        /// The number of seconds which have elapsed since the orbit was last updated.
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

            Orbit = new Orbit(this, Orbit.Value.OrbitedObject);
        }

        internal virtual void GenerateOrbit(ICelestialLocation orbitedObject)
        {
            if (orbitedObject == null)
            {
                return;
            }

            Space.Orbit.SetCircularOrbit(this, orbitedObject);
        }

        internal double GetHillSphereRadius() => Orbit?.GetHillSphereRadius(this) ?? 0;

        /// <summary>
        /// Approximates the radius of the orbiting body's mutual Hill sphere with another
        /// orbiting body in orbit around the same primary, in meters.
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
        internal double GetMutualHillSphereRadius(double otherMass)
            => Orbit?.GetMutualHillSphereRadius(this, otherMass) ?? 0;

        internal double GetRocheLimit(double orbitingDensity)
            => 0.8947 * Math.Pow(Mass / orbitingDensity, 1.0 / 3.0);

        internal double GetSphereOfInfluenceRadius()
            => Orbit?.GetSphereOfInfluenceRadius(this) ?? 0;

        private protected virtual void GenerateAlbedo() => Albedo = 0;

        private protected virtual void GenerateSubstance() { }

        /// <summary>
        /// Calculates the temperature of the <see cref="CelestialBody"/>, averaged over its orbit,
        /// in K.
        /// </summary>
        private double GetAverageBlackbodySurfaceTemperature()
            => Orbit.HasValue
                ? ((SurfaceTemperatureAtPeriapsis * (1 + Orbit.Value.Eccentricity)) + (SurfaceTemperatureAtApoapsis * (1 - Orbit.Value.Eccentricity))) / 2.0
                : GetSurfaceTemperatureAtPosition(Position);

        /// <summary>
        /// Calculates the heat added to this <see cref="CelestialBody"/> by insolation at the given
        /// position, in K.
        /// </summary>
        /// <param name="position">
        /// A hypothetical position for this <see cref="CelestialBody"/> at which the heat of
        /// insolation will be calculated.
        /// </param>
        /// <returns>
        /// The heat added to this <see cref="CelestialBody"/> by insolation at the given position,
        /// in K.
        /// </returns>
        private double GetInsolationHeat(Vector3 position)
            => Math.Pow(1 - Albedo, 0.25) * ContainingCelestialRegion
                .GetAllChildren<Star>()
                .Where(x => x != this)
                .Sum(x => (x.Temperature ?? 0) * Math.Sqrt(x.Shape.ContainingRadius / (2 * GetDistanceFromPositionTo(position, x))));

        /// <summary>
        /// Calculates the total average temperature of the <see cref="CelestialBody"/> as if this
        /// object was at the apoapsis of its orbit, in K.
        /// </summary>
        /// <remarks>
        /// Uses current position if this object is not in an orbit, or if its apoapsis is infinite.
        /// </remarks>
        private double GetSurfaceTemperatureAtApoapsis()
        {
            // Actual position doesn't matter for temperature, only distance.
            var position = !Orbit.HasValue || double.IsInfinity(Orbit.Value.Apoapsis)
                ? Position
                : Orbit.Value.OrbitedObject.Position + (Vector3.UnitX * Orbit.Value.Apoapsis);
            return GetSurfaceTemperatureAtPosition(position);
        }

        /// <summary>
        /// Calculates the total average temperature of the <see cref="CelestialBody"/> as if this
        /// object was at the periapsis of its orbit, in K.
        /// </summary>
        /// <remarks>
        /// Uses current position if this object is not in an orbit.
        /// </remarks>
        private double GetSurfaceTemperatureAtPeriapsis()
        {
            // Actual position doesn't matter for temperature, only distance.
            var position = Orbit.HasValue
                ? Orbit.Value.OrbitedObject.Position + (Vector3.UnitX * Orbit.Value.Periapsis)
                : Position;
            return GetSurfaceTemperatureAtPosition(position);
        }

        /// <summary>
        /// Calculates the total average temperature of the <see cref="CelestialBody"/> as if this
        /// object was at the specified position, including ambient heat of its parent and radiated
        /// heat from all sibling objects, in K.
        /// </summary>
        /// <param name="position">
        /// A hypothetical position for this <see cref="CelestialBody"/> at which its temperature
        /// will be calculated.
        /// </param>
        /// <returns>
        /// The total average temperature of the <see cref="CelestialBody"/> at the given position,
        /// in K.
        /// </returns>
        private protected double GetSurfaceTemperatureAtPosition(Vector3 position)
            => (Temperature ?? 0) + GetInsolationHeat(position);

        /// <summary>
        /// Estimates the total average temperature of the <see cref="CelestialBody"/> as if this
        /// object was at the specified true anomaly in its orbit, including ambient heat of its
        /// parent and radiated heat from all sibling objects, in K. If the body is not in orbit,
        /// returns the temperature at its current position.
        /// </summary>
        /// <param name="trueAnomaly">
        /// A true anomaly at which its temperature will be calculated.
        /// </param>
        /// <returns>
        /// The total average temperature of the <see cref="CelestialBody"/> at the given position,
        /// in K.
        /// </returns>
        /// <remarks>
        /// The estimation is performed by linear interpolation between the temperature at periapsis
        /// and apoapsis, which is not necessarily accurate for highly elliptical orbits, or bodies
        /// with multiple significant nearby heat sources, but it should be fairly accurate for
        /// bodies in fairly circular orbits around heat sources which are all close to the center
        /// of the orbit, and much faster for successive calls than calculating the temperature at
        /// specific positions precisely.
        /// </remarks>
        private protected double GetSurfaceTemperatureAtTrueAnomaly(double trueAnomaly)
            => MathUtility.Lerp(SurfaceTemperatureAtPeriapsis, SurfaceTemperatureAtApoapsis, trueAnomaly);

        private protected virtual void ResetCachedTemperatures()
        {
            _averageBlackbodySurfaceTemperature = null;
            _blackbodySurfaceTemperature = null;
            _surfaceTemperatureAtApoapsis = null;
            _surfaceTemperatureAtPeriapsis = null;
        }

        private protected virtual void ResetOrbitalProperties() { }
    }
}
