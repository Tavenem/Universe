using Tavenem.Universe.Place;

namespace Tavenem.Universe.Space;

/// <summary>
/// Defines a type of child a <see cref="CosmicLocation"/> may have, and how to generate a
/// new instance of that child.
/// </summary>
public class ChildDefinition
{
    /// <summary>
    /// <para>
    /// The density of this type of child within the containing parent region.
    /// </para>
    /// <para>
    /// The volume of the parent is multiplied by this value to give the total number of this
    /// child type found in the region.
    /// </para>
    /// </summary>
    public HugeNumber Density { get; init; }

    /// <summary>
    /// The radius of the open space required for this child type.
    /// </summary>
    public HugeNumber Space { get; init; }

    /// <summary>
    /// The type of this child.
    /// </summary>
    public virtual CosmicStructureType StructureType { get; init; }

    /// <summary>
    /// Initializes a new instance of <see cref="ChildDefinition"/>.
    /// </summary>
    public ChildDefinition() { }

    /// <summary>
    /// Initializes a new instance of <see cref="ChildDefinition"/>.
    /// </summary>
    /// <param name="space">The radius of the open space required for this child type.</param>
    /// <param name="density">
    /// The density of this type of child within the containing parent region.
    /// </param>
    /// <param name="structureType">The type of this child.</param>
    public ChildDefinition(HugeNumber space, HugeNumber density, CosmicStructureType structureType = CosmicStructureType.None)
    {
        Density = density;
        Space = space;
        StructureType = structureType;
    }

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
    public virtual bool IsSatisfiedBy(ChildDefinition other) => (StructureType & other.StructureType) != CosmicStructureType.None;

    /// <summary>
    /// Determines whether this <see cref="ChildDefinition"/> instance's parameters are
    /// satisfied by the given <paramref name="location"/>, discounting <see cref="Density"/>
    /// and <see cref="Space"/>.
    /// </summary>
    /// <param name="location">A <see cref="Location"/> to test against this instance.</param>
    /// <returns><see langword="true"/> if this instance's parameters are satisfied by the given
    /// <paramref name="location"/>; otherwise <see langword="false"/>.</returns>
    public virtual bool IsSatisfiedBy(CosmicLocation location) => StructureType.HasFlag(location.StructureType);

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
    public virtual CosmicLocation? GetChild(CosmicLocation parent, Vector3<HugeNumber> position, out List<CosmicLocation> children)
        => CosmicLocation.New(StructureType, parent, position, out children);
}
