using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.WorldFoundry.Place;
using System;
using System.Threading.Tasks;

namespace NeverFoundry.WorldFoundry.Space
{
    /// <summary>
    /// Defines a type of child a <see cref="CelestialLocation"/> may have, and how to generate a
    /// new instance of that child.
    /// </summary>
    public class ChildDefinition<T> : IChildDefinition where T : CelestialLocation
    {
        /// <summary>
        /// The density of this type of child within the containing parent region.
        /// </summary>
        public Number Density { get; }

        /// <summary>
        /// The radius of the open space required for this child type.
        /// </summary>
        public Number Space { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="ChildDefinition{T}"/>.
        /// </summary>
        public ChildDefinition() { }

        /// <summary>
        /// Initializes a new instance of <see cref="ChildDefinition{T}"/>.
        /// </summary>
        /// <param name="space">The radius of the open space required for this child type.</param>
        /// <param name="density">The density of this type of child within the containing parent
        /// region.</param>
        public ChildDefinition(Number space, Number density)
        {
            Density = density;
            Space = space;
        }

        /// <summary>
        /// Determines whether this <see cref="ChildDefinition{T}"/> type definition is satisfied by
        /// the given <paramref name="type"/>.
        /// </summary>
        /// <param name="type">A type to test against this instance.</param>
        /// <returns><see langword="true"/> if this instance's type definition is satisfied by the
        /// given <paramref name="type"/>; otherwise <see langword="false"/>.</returns>
        public bool IsSatisfiedBy(Type type) => typeof(T).IsAssignableFrom(type);

        /// <summary>
        /// Determines whether this <see cref="ChildDefinition{T}"/> instance's parameters are
        /// satisfied by the given <paramref name="other"/> <see cref="ChildDefinition{T}"/>
        /// instance's parameters, discounting <see cref="Density"/> and <see cref="Space"/>.
        /// </summary>
        /// <param name="other">A <see cref="ChildDefinition{T}"/> to test against this
        /// instance.</param>
        /// <returns><see langword="true"/> if this instance's parameters are satisfied by the
        /// <paramref name="other"/> instance's parameters; otherwise <see
        /// langword="false"/>.</returns>
        public virtual bool IsSatisfiedBy(IChildDefinition other)
        {
            var genericType = other.GetType().GenericTypeArguments;
            return genericType.Length > 0 && typeof(T).IsAssignableFrom(genericType[0]);
        }

        /// <summary>
        /// Determines whether this <see cref="ChildDefinition{T}"/> instance's parameters are
        /// satisfied by the given <paramref name="location"/>, discounting <see cref="Density"/>
        /// and <see cref="Space"/>.
        /// </summary>
        /// <param name="location">A <see cref="Location"/> to test against this instance.</param>
        /// <returns><see langword="true"/> if this instance's parameters are satisfied by the given
        /// <paramref name="location"/>; otherwise <see langword="false"/>.</returns>
        public virtual Task<bool> IsSatisfiedByAsync(Location location) => Task.FromResult(location is T);

        /// <summary>
        /// Gets a new <see cref="CelestialLocation"/> as defined by this <see
        /// cref="ChildDefinition{T}"/>.
        /// </summary>
        /// <param name="parent">The location which contains the new one.</param>
        /// <param name="position">The position of the new location relative to the center of its
        /// parent.</param>
        /// <returns>A new <see cref="CelestialLocation"/> as defined by this <see
        /// cref="ChildDefinition{T}"/>.</returns>
        public async Task<CelestialLocation?> GetChildAsync(Location parent, Vector3 position)
            => await GetDefinedChildAsync(parent, position).ConfigureAwait(false);

        private Task<T?> GetDefinedChildAsync(Location parent, Vector3 position)
            => CelestialLocation.GetNewInstanceAsync<T>(parent, position);
    }
}
