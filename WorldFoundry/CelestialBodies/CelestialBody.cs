using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using WorldFoundry.CelestialBodies.Stars;
using WorldFoundry.Space;

namespace WorldFoundry.CelestialBodies
{
    /// <summary>
    /// Represents any discrete physical object in space, such as a star or planet.
    /// </summary>
    public class CelestialBody : BioZone
    {
        private const double polarLatitude = 1.5277247828211;
        private const double cosPolarLatitude = 0.04305822778985774;

        private float? _albedo;
        /// <summary>
        /// The average albedo of the <see cref="CelestialBody"/> (a value between 0 and 1).
        /// </summary>
        /// <remarks>
        /// This refers to the total albedo of the body, including any atmosphere, not just
        /// the surface albedo of the main body.
        /// </remarks>
        public virtual float Albedo
        {
            get => GetProperty(ref _albedo, GenerateAlbedo) ?? 0;
            set => _albedo = value;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="CelestialBody"/>.
        /// </summary>
        public CelestialBody() { }

        /// <summary>
        /// Initializes a new instance of <see cref="CelestialBody"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="SpaceRegion"/> in which this <see cref="CelestialBody"/> is located.
        /// </param>
        public CelestialBody(SpaceRegion parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="CelestialBody"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="SpaceRegion"/> in which this <see cref="CelestialBody"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="CelestialBody"/>.</param>
        public CelestialBody(SpaceRegion parent, Vector3 position) : base(parent, position) { }

        /// <summary>
        /// Adjusts incoming insolation based on conditions.
        /// </summary>
        /// <param name="insolation">The total incoming insolation.</param>
        /// <param name="polar">Indicates whether the adjustment is to be made for polar incidence.</param>
        protected virtual void AdjustSolarInsolation(ref double insolation, bool polar)
        {
            if (polar)
            {
                insolation *= cosPolarLatitude;
            }
        }

        /// <summary>
        /// Determines an albedo for this <see cref="CelestialBody"/> (a value between 0 and 1).
        /// </summary>
        /// <remarks>
        /// Sets 0 in the base class; subclasses which are not black-bodies are expected to override.
        /// </remarks>
        protected virtual void GenerateAlbedo() => Albedo = 0;

        /// <summary>
        /// Calculates the total average temperature of the <see cref="CelestialBody"/> at its poles (in K),
        /// including ambient heat of its parent and radiated heat from all sibling objects.
        /// </summary>
        /// <returns>The total average temperature of the <see cref="CelestialBody"/>, in K.</returns>
        internal float GetTotalPolarTemperature() => GetTotalTemperatureFromPosition(Position, true);

        /// <summary>
        /// Calculates the total average temperature of the <see cref="CelestialBody"/> (in K),
        /// including ambient heat of its parent and radiated heat from all sibling objects.
        /// </summary>
        /// <returns>The total average temperature of the <see cref="CelestialBody"/>, in K.</returns>
        public float GetTotalTemperature() => GetTotalTemperatureFromPosition(Position, false);

        /// <summary>
        /// Calculates the total average temperature of the <see cref="CelestialBody"/> (in K),
        /// including ambient heat of its parent and radiated heat from all sibling objects, if this
        /// object was at the apoapsis of its orbit.
        /// </summary>
        /// <param name="polar">
        /// If true, calculates the approximate temperature at the <see cref="CelestialBody"/>'s poles.
        /// </param>
        /// <remarks>
        /// Uses current position if this object is not in an orbit, or if its apoapsis is infinite.
        /// </remarks>
        /// <returns>The total average temperature of the <see cref="CelestialBody"/>, in K.</returns>
        public float GetTotalTemperatureAtApoapsis(bool polar)
        {
            if (Orbit == null)
            {
                return GetTotalTemperatureFromPosition(Position, polar);
            }

            var apoapsis = Orbit.Apoapsis;
            if (double.IsInfinity(apoapsis))
            {
                return GetTotalTemperatureFromPosition(Position, polar);
            }

            // Actual position doesn't matter for temperature, only distance.
            Vector3 apoapsisVector = Orbit.OrbitedObject.Position + (Vector3.UnitX * (float)(apoapsis / Parent.LocalScale));
            return GetTotalTemperatureFromPosition(apoapsisVector, polar);
        }

        /// <summary>
        /// Calculates the total average temperature of the <see cref="CelestialBody"/> (in K),
        /// including ambient heat of its parent and radiated heat from all sibling objects, if this
        /// object was at the periapsis of its orbit.
        /// </summary>
        /// <param name="polar">
        /// If true, calculates the approximate temperature at the <see cref="CelestialBody"/>'s poles.
        /// </param>
        /// <remarks>
        /// Uses current position if this object is not in an orbit.
        /// </remarks>
        /// <returns>The total average temperature of the <see cref="CelestialBody"/>, in K.</returns>
        public float GetTotalTemperatureAtPeriapsis(bool polar)
        {
            if (Orbit == null)
            {
                return GetTotalTemperatureFromPosition(Position, polar);
            }

            // Actual position doesn't matter for temperature, only distance.
            Vector3 periapsis = Orbit.OrbitedObject.Position + (Vector3.UnitX * (float)(Orbit.Periapsis / Parent.LocalScale));
            return GetTotalTemperatureFromPosition(periapsis, polar);
        }

        /// <summary>
        /// Calculates the total average temperature of the <see cref="CelestialBody"/> (in K),
        /// including ambient heat of its parent and radiated heat from all sibling objects, if this
        /// object was at the specified position.
        /// </summary>
        /// <param name="position">
        /// A hypothetical position for this <see cref="CelestialBody"/> at which its temperature
        /// will be calculated.
        /// </param>
        /// <param name="polar">
        /// If true, calculates the approximate temperature at the <see cref="CelestialBody"/>'s poles.
        /// </param>
        /// <returns>The total average temperature of the <see cref="CelestialBody"/>, in K.</returns>
        internal virtual float GetTotalTemperatureFromPosition(Vector3 position, bool polar = false)
        {
            float temp = Temperature ?? 0;

            // Nearby stars within the same parent may provide heat.
            // Other stars are ignored, presumed to be far enough away that
            // their heating effects are negligible.
            // Within a star system, all stars are counted.
            IEnumerable<Star> stellarSiblings = null;
            if (Parent is StarSystem)
            {
                stellarSiblings = Parent.Children.Where(o => o is Star && o != this).Cast<Star>();
            }
            else
            {
                stellarSiblings = Parent.GetNearbyChildren(Position).Where(o => o is Star && o != this).Cast<Star>();
            }

            var totalInsolation = 0.0;
            foreach (var star in stellarSiblings)
            {
                totalInsolation += star.Luminosity / (Utilities.MathUtil.Constants.FourPI * Math.Pow(GetDistanceFromPositionToTarget(position, star), 2));
            }
            AdjustSolarInsolation(ref totalInsolation, polar);

            temp += (float)Math.Pow((totalInsolation * (1 - Albedo)) / Utilities.Science.Constants.FourStefanBoltzmannConstant, 0.25);

            return temp;
        }
    }
}
