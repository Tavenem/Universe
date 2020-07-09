using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.WorldFoundry.Place;
using System.Collections.Generic;

namespace NeverFoundry.WorldFoundry.Space.Planetoids
{
    /// <summary>
    /// Defines a type of <see cref="Planetoid"/> child a <see cref="CosmicLocation"/> may have,
    /// and how to generate a new instance of that child.
    /// </summary>
    public class PlanetChildDefinition : ChildDefinition
    {
        /// <summary>
        /// The type of the <see cref="Planetoid"/>.
        /// </summary>
        public PlanetType PlanetType { get; }

        /// <summary>
        /// The type of this child.
        /// </summary>
        public override CosmicStructureType StructureType => CosmicStructureType.Planetoid;

        /// <summary>
        /// Initializes a new instance of <see cref="PlanetChildDefinition"/>.
        /// </summary>
        /// <param name="density">
        /// The density of this type of child within the containing parent region.
        /// </param>
        /// <param name="planemoType">
        /// The type of <see cref="Planetoid"/>.
        /// </param>
        public PlanetChildDefinition(Number density, PlanetType planemoType = PlanetType.Dwarf) : base(Planetoid.GetSpaceForType(planemoType), density) => PlanetType = planemoType;

        /// <summary>
        /// Determines whether this <see cref="ChildDefinition"/> instance's parameters are
        /// satisfied by the given <paramref name="other"/> <see cref="ChildDefinition"/>
        /// instance's parameters.
        /// </summary>
        /// <param name="other">A <see cref="ChildDefinition"/> to test against this
        /// instance.</param>
        /// <returns><see langword="true"/> if this instance's parameters are satisfied by the
        /// <paramref name="other"/> instance's parameters; otherwise <see
        /// langword="false"/>.</returns>
        public override bool IsSatisfiedBy(ChildDefinition other)
            => other is PlanetChildDefinition x && PlanetType.HasFlag(x.PlanetType);

        /// <summary>
        /// Determines whether this <see cref="ChildDefinition"/> instance's parameters are
        /// satisfied by the given <paramref name="location"/>, discounting <see
        /// cref="ChildDefinition.Density"/> and <see cref="ChildDefinition.Space"/>.
        /// </summary>
        /// <param name="location">A <see cref="Location"/> to test against this instance.</param>
        /// <returns><see langword="true"/> if this instance's parameters are satisfied by the given
        /// <paramref name="location"/>; otherwise <see langword="false"/>.</returns>
        public override bool IsSatisfiedBy(CosmicLocation location)
            => location is Planetoid x && PlanetType.HasFlag(x.PlanetType);

        /// <summary>
        /// Gets a new <see cref="Planetoid"/> as defined by this <see
        /// cref="PlanetChildDefinition"/>.
        /// </summary>
        /// <param name="parent">The location which contains the new one.</param>
        /// <param name="stars">
        /// The collection of stars in the local system (if the <paramref name="parent"/> is a <see
        /// cref="StarSystem"/>).
        /// </param>
        /// <param name="position">
        /// The position of the new location relative to the center of its <paramref
        /// name="parent"/>.
        /// </param>
        /// <param name="satellites">
        /// <para>
        /// When this method returns, will be set to a <see cref="List{T}"/> of <see
        /// cref="Planetoid"/>s containing any satellites generated for the planet during the
        /// creation process.
        /// </para>
        /// <para>
        /// This list may be useful, for instance, to ensure that these additional objects are also
        /// persisted to data storage.
        /// </para>
        /// </param>
        /// <returns>
        /// A new <see cref="Planetoid"/> as defined by this <see cref="PlanetChildDefinition"/>.
        /// </returns>
        public Planetoid? GetPlanet(CosmicLocation? parent, List<Star> stars, Vector3 position, out List<Planetoid> satellites) => new Planetoid(
            PlanetType,
            parent,
            null,
            stars,
            position,
            out satellites);

        /// <summary>
        /// Gets a new <see cref="CosmicLocation"/> as defined by this <see
        /// cref="ChildDefinition"/>.
        /// </summary>
        /// <param name="parent">The location which is to contain the new one.</param>
        /// <param name="position">
        /// The position of the new location relative to the center of the <paramref
        /// name="parent"/>.
        /// </param>
        /// <param name="children">
        /// <para>
        /// When this method returns, will be set to a <see cref="List{T}"/> of <see
        /// cref="CosmicLocation"/>s containing any child objects generated for the location during
        /// the creation process.
        /// </para>
        /// <para>
        /// This list may be useful, for instance, to ensure that these additional objects are also
        /// persisted to data storage.
        /// </para>
        /// </param>
        /// <returns>
        /// A new <see cref="CosmicLocation"/> as defined by this <see cref="ChildDefinition"/>.
        /// </returns>
        public override CosmicLocation? GetChild(CosmicLocation parent, Vector3 position, out List<CosmicLocation> children)
        {
            var instance = GetPlanet(parent, new List<Star>(), position, out var satellites);
            children = new List<CosmicLocation>(satellites);
            return instance;
        }
    }
}