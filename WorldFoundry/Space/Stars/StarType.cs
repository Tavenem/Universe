using System;

namespace NeverFoundry.WorldFoundry.Space.Stars
{
    /// <summary>
    /// The type of a star.
    /// </summary>
    [Flags]
    public enum StarType
    {
        /// <summary>
        /// No specified type.
        /// </summary>
        None = 0,

        /// <summary>
        /// A star in the main sequence.
        /// </summary>
        MainSequence = 1,

        /// <summary>
        /// A brown dwarf star.
        /// </summary>
        BrownDwarf = 1 << 1,

        /// <summary>
        /// A white dwarf star.
        /// </summary>
        WhiteDwarf = 1 << 2,

        /// <summary>
        /// A neutron star.
        /// </summary>
        Neutron = 1 << 3,

        /// <summary>
        /// A red giant star.
        /// </summary>
        RedGiant = 1 << 4,

        /// <summary>
        /// A yellow giant star.
        /// </summary>
        YellowGiant = 1 << 5,

        /// <summary>
        /// A blue giant star.
        /// </summary>
        BlueGiant = 1 << 6,

        /// <summary>
        /// Any giant star type.
        /// </summary>
        Giant = RedGiant | YellowGiant | BlueGiant,
    }
}
