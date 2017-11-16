using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Numerics;
using System.Text;
using WorldFoundry.Space;

namespace WorldFoundry.Orbits
{
    /// <summary>
    /// Describes an object which observes the laws of gravity.
    /// </summary>
    public class Orbiter : SpaceChild
    {
        private double? _mass;
        /// <summary>
        /// The mass of the celestial object, including the mass of all children,
        /// whether explicitly modeled or merely potential, in kg.
        /// </summary>
        public virtual double Mass
        {
            get => GetProperty(ref _mass, GenerateMass) ?? 0;
            internal set => _mass = value;
        }

        /// <summary>
        /// The orbit occupied by this <see cref="Orbiter"/> (may be null).
        /// </summary>
        public Orbit Orbit { get; set; }

        private double? _surfaceGravity;
        /// <summary>
        /// The average force of gravity at the surface of this celestial object, in N.
        /// </summary>
        public double SurfaceGravity => GetProperty(ref _surfaceGravity, GenerateSurfaceGravity) ?? 0;

        /// <summary>
        /// Specifies the velocity of the <see cref="Orbiter"/>.
        /// </summary>
        [NotMapped]
        public Vector3 Velocity
        {
            get => new Vector3(VelocityX, VelocityY, VelocityZ);
            set
            {
                VelocityX = value.X;
                VelocityY = value.Y;
                VelocityZ = value.Z;
            }
        }

        /// <summary>
        /// Specifies the X component of the <see cref="Orbiter"/>'s velocity.
        /// </summary>
        public float VelocityX { get; set; }

        /// <summary>
        /// Specifies the Y component of the <see cref="Orbiter"/>'s velocity.
        /// </summary>
        public float VelocityY { get; set; }

        /// <summary>
        /// Specifies the Z component of the <see cref="Orbiter"/>'s velocity.
        /// </summary>
        public float VelocityZ { get; set; }

        /// <summary>
        /// Initializes a new instance of <see cref="Orbiter"/>.
        /// </summary>
        public Orbiter() { }

        /// <summary>
        /// Initializes a new instance of <see cref="Orbiter"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="SpaceRegion"/> in which this <see cref="Orbiter"/> is located.
        /// </param>
        public Orbiter(SpaceRegion parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="Orbiter"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="SpaceRegion"/> in which this <see cref="Orbiter"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="Orbiter"/>.</param>
        public Orbiter(SpaceRegion parent, Vector3 position) : base(parent, position) { }

        /// <summary>
        /// Generates the <see cref="Mass"/> of this <see cref="Orbiter"/>.
        /// </summary>
        /// <remarks>Produces 0 in the base class; expected to be overridden in subclasses.</remarks>
        private void GenerateMass() => Mass = 0;

        /// <summary>
        /// Determines an orbit for this <see cref="Orbiter"/>.
        /// </summary>
        /// <param name="orbitedObject">The <see cref="Orbiter"/> which is to be orbited.</param>
        /// <remarks>
        /// In the base class, always generates a circular orbit; subclasses are expected to override.
        /// </remarks>
        public virtual void GenerateOrbit(Orbiter orbitedObject)
        {
            Orbit = Orbit.GetCircularOrbit(this, orbitedObject);

            Velocity = Orbit.V0 / orbitedObject.Parent.LocalScale;
        }

        /// <summary>
        /// Calculates the average surface gravity of this <see cref="Orbiter"/>, in N.
        /// </summary>
        /// <returns>The average surface gravity of this <see cref="Orbiter"/>, in N.</returns>
        private void GenerateSurfaceGravity() => _surfaceGravity = (Utilities.Science.Constants.G * Mass) / Math.Pow(Radius, 2);

        /// <summary>
        /// Calculates the force of gravity on this <see cref="Orbiter"/> from another
        /// as a vector, in N.
        /// </summary>
        /// <param name="other">An <see cref="Orbiter"/> from which the force gravity will be calculated.</param>
        /// <returns>
        /// The force of gravity from this <see cref="Orbiter"/> to the other, in N, as a vector.
        /// </returns>
        /// <remarks>
        /// Newton's law is used. General relativity would be more accurate in certain circumstances,
        /// but is considered unnecessarily intensive work for the simple simulations expected to
        /// make use of this library. If you are an astronomer performing scientifically rigorous
        /// calculations or simulations, this is not the library for you ;)
        /// </remarks>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="other"/> may not be null.
        /// </exception>
        /// <exception cref="System.Exception">
        /// An exception will be thrown if the two <see cref="Orbiter"/>s do not share a <see
        /// cref="Space.SpaceRegion"/> parent at some point.
        /// </exception>
        public Vector3 GetGravityFromObject(Orbiter other)
        {
            float distance = GetDistanceToTarget(other);

            float scale = (float)(-Utilities.Science.Constants.G * ((Mass * other.Mass) / (distance * distance)));

            // Get the unit vector
            Vector3 unit = (other.Position - Position) / distance;

            return unit * scale;
        }

        /// <summary>
        /// Calculates the Roche limit for this <see cref="Orbiter"/> for objects of the given
        /// density, in meters.
        /// </summary>
        /// <param name="orbitingDensity">The density of a hypothetical orbiting object.</param>
        /// <returns>
        /// The Roche limit for this <see cref="Orbiter"/> for an object of the given density, in meters.
        /// </returns>
        public double GetRocheLimit(float orbitingDensity) => 0.8947 * Math.Pow(Mass / orbitingDensity, 1.0 / 3.0);

        /// <summary>
        /// Calculates the total force of gravity on this <see cref="Orbiter"/>, in N, as a vector.
        /// </summary>
        /// <returns>The total force of gravity on this <see cref="Orbiter"/>, in N, as a vector.</returns>
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
            Vector3 totalGravity = Vector3.Zero;

            // No gravity for a parent-less object
            if (Parent == null)
            {
                return totalGravity;
            }

            // Only the gravity from nearby siblings is considered.
            // This may discount significant effects from more distant but extremely massive objects,
            // but the alternative is to consider every object, which could potentially be a much too
            // large amount (trillions, perhaps).
            foreach (var sibling in Parent.GetNearbyChildren(Position)
                .Where(c => c.GetType().IsSubclassOf(typeof(Orbiter))).Cast<Orbiter>())
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
            StringBuilder sb = new StringBuilder(base.ToString());
            if (Orbit?.OrbitedObject != null)
            {
                sb.Append(", orbiting ");
                sb.Append(Orbit.OrbitedObject.TypeName);
                sb.Append(" ");
                sb.Append(Orbit.OrbitedObject.Title);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Updates the orbital position and velocity of this <see cref="Orbiter"/> after the
        /// specified number of seconds have passed, assuming no influences on its motion have
        /// occurred, aside from its orbit.
        /// </summary>
        /// <param name="elapsedSeconds">
        /// The number of seconds which have elapsed since the orbit was last updated.
        /// </param>
        /// <remarks>Does nothing if no orbit is defined.</remarks>
        public void UpdateOrbit(double elapsedSeconds)
        {
            if (Orbit == null)
            {
                return;
            }

            var (position, velocity) = Orbit.GetStateVectorsAtTime(elapsedSeconds);

            if (Orbit.OrbitedObject.Parent != Parent)
            {
                Position = Orbit.OrbitedObject.TranslateToLocalCoordinates(Parent)
                    + (position / Orbit.OrbitedObject.Parent.LocalScale);
            }
            else
            {
                Position = Orbit.OrbitedObject.Position + (position / Orbit.OrbitedObject.Parent.LocalScale);
            }

            Velocity = (velocity / Orbit.OrbitedObject.Parent.LocalScale);

            Orbit.UpdateOrbit();
        }
    }
}
