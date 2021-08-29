namespace Tavenem.Universe.Space;

/// <summary>
/// The type of a <see cref="CosmicLocation"/>.
/// </summary>
[Flags]
public enum CosmicStructureType
{
    /// <summary>
    /// No specified type.
    /// </summary>
    None = 0,

    /// <summary>
    /// A Universe is the top-level location in a hierarchy.
    /// </summary>
    Universe = 1,

    /// <summary>
    /// The largest structure in the universe: a massive collection of galaxy groups and
    /// clusters.
    /// </summary>
    Supercluster = 1 << 1,

    /// <summary>
    /// A large structure of gravitationally-bound galaxies.
    /// </summary>
    GalaxyCluster = 1 << 2,

    /// <summary>
    /// A collection of gravitationally-bound galaxies, mostly small dwarfs orbiting a few large
    /// galaxies.
    /// </summary>
    GalaxyGroup = 1 << 3,

    /// /// <summary>
    /// Not a technical astrnonomical term, a galaxy "sub-group" is used by this library to
    /// indicate the collection of gravitationally-bound objects (mostly dwarf galaxies and
    /// globular clusters) orbiting a large galaxy. Each galaxy group tends to have a handful of
    /// such collections.
    /// </summary>
    GalaxySubgroup = 1 << 4,

    /// <summary>
    /// A spiral-shaped, gravitationally-bound collection of stars, gas, dust, and dark matter.
    /// </summary>
    SpiralGalaxy = 1 << 5,

    /// <summary>
    /// An elliptical, gravitationally-bound collection of stars, gas, dust, and dark matter.
    /// </summary>
    EllipticalGalaxy = 1 << 6,

    /// <summary>
    /// A small, gravitationally-bound collection of stars, gas, dust, and dark matter.
    /// </summary>
    DwarfGalaxy = 1 << 7,

    /// <summary>
    /// Any galaxy type, including <see cref="SpiralGalaxy"/>, <see cref="EllipticalGalaxy"/>,
    /// and <see cref="DwarfGalaxy"/>.
    /// </summary>
    Galaxy = SpiralGalaxy | EllipticalGalaxy | DwarfGalaxy,

    /// <summary>
    /// A small, dense collection of stars.
    /// </summary>
    GlobularCluster = 1 << 8,

    /// <summary>
    /// A cloud of interstellar gas and dust.
    /// </summary>
    Nebula = 1 << 9,

    /// <summary>
    /// A charged cloud of interstellar gas and dust.
    /// </summary>
    HIIRegion = 1 << 10,

    /// <summary>
    /// The remnants of a red giant, which have left behind an ionized gas cloud surrounding a
    /// white dwarf star.
    /// </summary>
    PlanetaryNebula = 1 << 11,

    /// <summary>
    /// Any nebula type, including <see cref="Nebula"/>, <see cref="HIIRegion"/>, and <see
    /// cref="PlanetaryNebula"/>.
    /// </summary>
    AnyNebula = Nebula | HIIRegion | PlanetaryNebula,

    /// <summary>
    /// A region of space containing a system of stars, and the bodies which orbit that system.
    /// </summary>
    StarSystem = 1 << 12,

    /// <summary>
    /// A region of space with a high concentration of asteroids.
    /// </summary>
    AsteroidField = 1 << 13,

    /// <summary>
    /// A shell surrounding a star with a high concentration of cometary bodies.
    /// </summary>
    OortCloud = 1 << 14,

    /// <summary>
    /// A gravitational singularity.
    /// </summary>
    BlackHole = 1 << 15,

    /// <summary>
    /// A stellar body.
    /// </summary>
    Star = 1 << 16,

    /// <summary>
    /// Any non-stellar celestial body, such as a planet or asteroid.
    /// </summary>
    Planetoid = 1 << 17,
}
