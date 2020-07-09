using System;

namespace NeverFoundry.WorldFoundry.Space.Planetoids
{
    /// <summary>
    /// A type of <see cref="Planetoid"/>.
    /// </summary>
    [Flags]
    public enum PlanetType
    {
        /// <summary>
        /// No specified type.
        /// </summary>
        None = 0,

        /// <summary>
        /// A primarily rocky planet, large in comparison to drawf planets but small in comparison
        /// to gas and ice giants.
        /// </summary>
        Terrestrial = 1,

        /// <summary>
        /// A terrestrial planet with an unusually high concentration of carbon, rather than silicates,
        /// including such features as naturally-occurring steel, and diamond volcanoes.
        /// </summary>
        Carbon = 1 << 1,

        /// <summary>
        /// A relatively small terrestrial planet consisting of an unusually large iron-nickel core, and
        /// an unusually thin mantle (similar to Mercury).
        /// </summary>
        Iron = 1 << 2,

        /// <summary>
        /// A terrestrial planet with little to no crust, whether due to a catastrophic collision event,
        /// or severe tidal forces due to a close orbit.
        /// </summary>
        Lava = 1 << 3,

        /// <summary>
        /// A terrestrial planet consisting of an unusually high proportion of water, with a mantle
        /// consisting of a form of high-pressure, hot ice, and possibly a supercritical
        /// surface-atmosphere blend.
        /// </summary>
        Ocean = 1 << 4,

        /// <summary>
        /// Any terrestrial planet type.
        /// </summary>
        AnyTerrestrial = Terrestrial | Carbon | Iron | Lava | Ocean,

        /// <summary>
        /// A dwarf planet: a body large enough to form a roughly spherical shape under its own gravity,
        /// but not large enough to clear its orbital "neighborhood" of smaller bodies.
        /// </summary>
        Dwarf = 1 << 5,

        /// <summary>
        /// A hot, rocky dwarf planet with a molten rock mantle; usually the result of tidal stress.
        /// </summary>
        LavaDwarf = 1 << 6,

        /// <summary>
        /// A rocky dwarf planet without the typical subsurface ice/water mantle.
        /// </summary>
        RockyDwarf = 1 << 7,

        /// <summary>
        /// Any dwarf planet type.
        /// </summary>
        AnyDwarf = Dwarf | LavaDwarf | RockyDwarf,

        /// <summary>
        /// A gas giant planet (excluding ice giants, which have their own subclass).
        /// </summary>
        GasGiant = 1 << 8,

        /// <summary>
        /// An ice giant planet, such as Neptune or Uranus.
        /// </summary>
        IceGiant = 1 << 9,

        /// <summary>
        /// Any giant planet type.
        /// </summary>
        Giant = GasGiant | IceGiant,

        /// <summary>
        /// Mostly ice and dust, with a large but thin atmosphere.
        /// </summary>
        Comet = 1 << 10,

        /// <summary>
        /// A C-Type asteroid.
        /// </summary>
        AsteroidC = 1 << 11,

        /// <summary>
        /// An M-Type asteroid.
        /// </summary>
        AsteroidM = 1 << 12,

        /// <summary>
        /// An S-Type asteroid.
        /// </summary>
        AsteroidS = 1 << 13,

        /// <summary>
        /// Any asteroid type.
        /// </summary>
        Asteroid = AsteroidC | AsteroidM | AsteroidS,
    }
}
