using System;
using System.Collections.Generic;
using System.Text;

namespace WorldFoundry.CelestialBodies.Stars
{
    public class Star : CelestialBody
    {
        /// <summary>
        /// The luminosity of this star, in Watts.
        /// </summary>
        public float Luminosity { get; internal set; }
    }
}
