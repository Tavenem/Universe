using MathAndScience.Science;
using System;
using System.Linq;
using System.Numerics;
using System.Text;
using WorldFoundry.Space;

namespace WorldFoundry.Orbits
{
    /// <summary>
    /// Describes an object which observes the laws of gravity.
    /// </summary>
    public class Orbiter : CelestialEntity
    {
        /// <summary>
        /// The orbit occupied by this <see cref="Orbiter"/> (may be null).
        /// </summary>
        public Orbit Orbit { get; set; }

        /// <summary>
        /// Specifies the velocity of the <see cref="Orbiter"/>.
        /// </summary>
        public virtual Vector3 Velocity { get; set; }

        /// <summary>
        /// Initializes a new instance of <see cref="Orbiter"/>.
        /// </summary>
        public Orbiter() { }

        /// <summary>
        /// Initializes a new instance of <see cref="Orbiter"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="Orbiter"/> is located.
        /// </param>
        public Orbiter(CelestialRegion parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="Orbiter"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="Orbiter"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="Orbiter"/>.</param>
        public Orbiter(CelestialRegion parent, Vector3 position) : base(parent, position) { }

        /// <summary>
        /// Determines an orbit for this <see cref="Orbiter"/>.
        /// </summary>
        /// <param name="orbitedObject">The <see cref="Orbiter"/> which is to be orbited.</param>
        /// <remarks>
        /// In the base class, always generates a circular orbit; subclasses are expected to override.
        /// </remarks>
        public virtual void GenerateOrbit(Orbiter orbitedObject)
        {
            if (orbitedObject == null)
            {
                return;
            }

            Orbit = Orbit.GetCircularOrbit(this, orbitedObject);

            Velocity = Orbit.V0 / orbitedObject.Parent.LocalScale;
        }

        /// <summary>
        /// Calculates the force of gravity on this <see cref="Orbiter"/> from another as a vector,
        /// in N.
        /// </summary>
        /// <param name="other">An <see cref="Orbiter"/> from which the force gravity will be calculated.</param>
        /// <returns>
        /// The force of gravity from this <see cref="Orbiter"/> to the other, in N, as a vector.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="other"/> may not be null.
        /// </exception>
        /// <exception cref="System.Exception">
        /// An exception will be thrown if the two <see cref="Orbiter"/> s do not share a <see
        /// cref="Space.CelestialRegion"/> parent at some point.
        /// </exception>
        /// <remarks>
        /// Newton's law is used. General relativity would be more accurate in certain circumstances,
        /// but is considered unnecessarily intensive work for the simple simulations expected to
        /// make use of this library. If you are an astronomer performing scientifically rigorous
        /// calculations or simulations, this is not the library for you ;)
        /// </remarks>
        public Vector3 GetGravityFromObject(Orbiter other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            var distance = GetDistanceToTarget(other);

            var scale = (float)(-ScienceConstants.G * (Mass * other.Mass / (distance * distance)));

            // Get the unit vector
            var unit = (other.Position - Position) / (float)distance;

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
        public double GetRocheLimit(double orbitingDensity) => 0.8947 * Math.Pow(Mass / orbitingDensity, 1.0 / 3.0);

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
            var totalGravity = Vector3.Zero;

            // No gravity for a parent-less object
            if (Parent == null)
            {
                return totalGravity;
            }

            // Only the gravity from nearby siblings is considered. This may discount significant
            // effects from more distant but extremely massive objects, but the alternative is to
            // consider every object, which could potentially be a much too large amount (trillions, perhaps).
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
            var sb = new StringBuilder(base.ToString());
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

            Velocity = velocity / Orbit.OrbitedObject.Parent.LocalScale;

            Orbit.UpdateOrbit();
        }
    }
}