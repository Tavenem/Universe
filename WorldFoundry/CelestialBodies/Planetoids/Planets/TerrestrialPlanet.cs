using System;
using System.Collections.Generic;
using System.Text;

namespace WorldFoundry.CelestialBodies.Planetoids.Planets
{
    public class TerrestrialPlanet : Planet
    {
        /// <summary>
        /// Calculates the mass required to produce the given surface gravity.
        /// </summary>
        /// <param name="gravity">The desired surface gravity (in kg/m²)</param>
        /// <returns>The mass required to produce the given surface gravity (in kg).</returns>
        private double GetMassForSurfaceGravity(float gravity) => (gravity * Math.Pow(Radius, 2)) / Utilities.Science.Constants.G;
    }
}
