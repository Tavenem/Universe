using System;
using System.Collections.Generic;
using System.Text;

namespace WorldFoundry.CelestialBodies.Planetoids.Planets.TerrestrialPlanets
{
    /// <summary>
    /// A primarily rocky planet, relatively small in comparison to gas and ice giants.
    /// </summary>
    public class TerrestrialPlanet : Planemo
    {
        /// <summary>
        /// The chance that this <see cref="Planemo"/> will have rings, as a rate between 0.0 and 1.0.
        /// </summary>
        /// <remarks>
        /// There is a low chance of most planets having substantial rings; 10 for <see
        /// cref="TerrestrialPlanet"/>s.
        /// </remarks>
        protected override float RingChance => 10;

        /// <summary>
        /// Calculates the mass required to produce the given surface gravity.
        /// </summary>
        /// <param name="gravity">The desired surface gravity, in kg/m².</param>
        /// <returns>The mass required to produce the given surface gravity, in kg.</returns>
        private double GetMassForSurfaceGravity(float gravity) => (gravity * Math.Pow(Radius, 2)) / Utilities.Science.Constants.G;

        /// <summary>
        /// Calculates the radius required to produce the given surface gravity.
        /// </summary>
        /// <param name="gravity">The desired surface gravity, in kg/m².</param>
        /// <returns>The radius required to produce the given surface gravity, in meters.</returns>
        public float GetRadiusForSurfaceGravity(float gravity) => (float)((gravity * Utilities.MathUtil.Constants.FourThirdsPI) / (Utilities.Science.Constants.G * Density));
    }
}
