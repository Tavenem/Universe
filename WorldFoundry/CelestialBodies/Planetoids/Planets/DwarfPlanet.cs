using System;
using System.Collections.Generic;
using System.Text;

namespace WorldFoundry.CelestialBodies.Planetoids.Planets
{
    public class DwarfPlanet : Planemo
    {
        /// <summary>
        /// The chance that this <see cref="Planemo"/> will have rings, as a rate between 0.0 and 1.0.
        /// </summary>
        /// <remarks>
        /// There is a low chance of most planets having substantial rings; 10 for <see
        /// cref="DwarfPlanet"/>s.
        /// </remarks>
        protected override float RingChance => 10;
    }
}
