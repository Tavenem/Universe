using MathAndScience.Numerics;
using MathAndScience.Shapes;
using System;
using UniversalTime;
using WorldFoundry.Place;

namespace WorldFoundry.Space
{
    /// <summary>
    /// A location in a universe which models a celestial entity, such as a planet, galaxy, or star.
    /// </summary>
    public interface ICelestialLocation : ILocation
    {
        /// <summary>
        /// A string that uniquely identifies this <see cref="ICelestialLocation"/> for display
        /// purposes.
        /// </summary>
        string Designation { get; }

        /// <summary>
        /// The total mass of this <see cref="ICelestialLocation"/>, in kg.
        /// </summary>
        double Mass { get; }

        /// <summary>
        /// An optional name for this <see cref="ICelestialLocation"/>.
        /// </summary>
        /// <remarks>
        /// Not every <see cref="ICelestialLocation"/> must have a name. They may be uniquely identified
        /// by their <see cref="Designation"/>, instead.
        /// </remarks>
        string? Name { get; set; }

        /// <summary>
        /// The orbit occupied by this <see cref="ICelestialLocation"/> (may be null).
        /// </summary>
        Orbit? Orbit { get; set; }

        /// <summary>
        /// The <see cref="CelestialRegion"/> which directly contains this <see cref="ICelestialLocation"/>.
        /// </summary>
        CelestialRegion? ContainingCelestialRegion { get; }

        /// <summary>
        /// The <see cref="Universe"/> which contains this <see cref="ICelestialLocation"/>, if any.
        /// </summary>
        Universe? ContainingUniverse { get; }

        /// <summary>
        /// The shape of this <see cref="ICelestialLocation"/>.
        /// </summary>
        IShape Shape { get; }

        /// <summary>
        /// The average temperature of this <see cref="ICelestialLocation"/>, in K.
        /// </summary>
        /// <remarks>No less than <see cref="ContainingCelestialRegion"/>'s ambient temperature.</remarks>
        double? Temperature { get; }

        /// <summary>
        /// The <see cref="ICelestialLocation"/>'s <see cref="Name"/>, if it has one; otherwise its <see cref="Designation"/>.
        /// </summary>
        string Title { get; }

        /// <summary>
        /// The name for this type of <see cref="ICelestialLocation"/>.
        /// </summary>
        string TypeName { get; }

        /// <summary>
        /// Specifies the velocity of the <see cref="ICelestialLocation"/> in m/s.
        /// </summary>
        Vector3 Velocity { get; set; }

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
        Vector3 GetGravityFromObject(ICelestialLocation other);

        /// <summary>
        /// Calculates the position of this <see cref="ICelestialLocation"/> at the given time,
        /// taking its orbit or velocity into account, without actually updating its current
        /// position. Does not perform integration over time of gravitational influences not
        /// reflected by <see cref="Orbit"/>.
        /// </summary>
        /// <param name="time">The time at which to get a position.</param>
        /// <returns>A <see cref="Vector3"/> representing position relative to the center of the
        /// <see cref="ContainingCelestialRegion"/>.</returns>
        Vector3 GetPositionAtTime(Duration time);

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
        Vector3 GetTotalLocalGravity();

        /// <summary>
        /// Updates the position and velocity of this object to correspond with the state predicted
        /// by its <see cref="Orbit"/> at the current time of its containing <see cref="Universe"/>,
        /// assuming no influences on the body's motion have occurred aside from its orbit. Has no
        /// effect if the body is not in orbit.
        /// </summary>
        void UpdateOrbit();

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
        void UpdateOrbit(double elapsedSeconds);
    }
}
