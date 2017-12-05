using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using WorldFoundry.CelestialBodies.Stars;
using WorldFoundry.Space;

namespace WorldFoundry.CelestialBodies
{
    /// <summary>
    /// Represents any contiguous physical object in space, such as a star or planet.
    /// </summary>
    public class CelestialBody : BioZone
    {
        internal const double PolarLatitude = 1.5277247828211;
        internal const double CosPolarLatitude = 0.04305822778985774;

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
            internal set => _albedo = value;
        }

        private float? _totalTemperatureAtApoapsis;
        public float TotalTemperatureAtApoapsis
        {
            get => GetProperty(ref _totalTemperatureAtApoapsis, GetTotalTemperatureAtApoapsis) ?? 0;
            private set => _totalTemperatureAtApoapsis = value;
        }

        private float? _totalTemperatureAtPeriapsis;
        public float TotalTemperatureAtPeriapsis
        {
            get => GetProperty(ref _totalTemperatureAtPeriapsis, GetTotalTemperatureAtPeriapsis) ?? 0;
            private set => _totalTemperatureAtPeriapsis = value;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="CelestialBody"/>.
        /// </summary>
        public CelestialBody() { }

        /// <summary>
        /// Initializes a new instance of <see cref="CelestialBody"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="CelestialBody"/> is located.
        /// </param>
        public CelestialBody(CelestialObject parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="CelestialBody"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="CelestialBody"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="CelestialBody"/>.</param>
        public CelestialBody(CelestialObject parent, Vector3 position) : base(parent, position) { }

        /// <summary>
        /// Calculates the temperature at the given latitude (as an angle in radians from the
        /// equator), given the temperatures at the equator and poles, in K.
        /// </summary>
        /// <param name="equatorialTemp">The temperature at the equator, in K.</param>
        /// <param name="polarTemp">The temperature at <see cref="PolarLatitude"/>, in K.</param>
        /// <param name="latitude">A latitude at which to calculate the temperature.</param>
        /// <returns></returns>
        internal static float GetTemperatureAtLatitude(float equatorialTemp, float polarTemp, float latitude)
            => (float)(polarTemp + (equatorialTemp - polarTemp) * Math.Cos(latitude * 0.8));

        /// <summary>
        /// Determines an albedo for this <see cref="CelestialBody"/> (a value between 0 and 1).
        /// </summary>
        /// <remarks>
        /// Sets 0 in the base class; subclasses which are not black-bodies are expected to override.
        /// </remarks>
        protected virtual void GenerateAlbedo() => Albedo = 0;

        /// <summary>
        /// Calculates the heat added to this <see cref="CelestialBody"/> by insolation at the given
        /// position, in K.
        /// </summary>
        /// <param name="position">
        /// A hypothetical position for this <see cref="CelestialBody"/> at which the heat of
        /// insolation will be calculated.
        /// </param>
        /// <returns>
        /// The heat added to this <see cref="CelestialBody"/> by insolation at the given position,
        /// in K.
        /// </returns>
        protected virtual float GetInsolationHeat(Vector3 position)
        {
            // Nearby stars within the same parent may provide heat.
            // Other stars are ignored, presumed to be far enough away that
            // their heating effects are negligible.
            // Within a star system, all stars are counted.
            IEnumerable<Star> stellarSiblings = null;
            if (Parent is StarSystem)
            {
                stellarSiblings = Parent.Children?.Where(o => o is Star && o != this).Cast<Star>();
            }
            else
            {
                stellarSiblings = Parent.GetNearbyChildren(position).Where(o => o is Star && o != this).Cast<Star>();
            }

            var totalInsolation = 0.0;
            foreach (var star in stellarSiblings)
            {
                totalInsolation += star.Luminosity / (Utilities.MathUtil.Constants.FourPI * Math.Pow(GetDistanceFromPositionToTarget(position, star), 2));
            }

            return (float)Math.Pow((totalInsolation * (1 - Albedo)) / Utilities.Science.Constants.FourStefanBoltzmannConstant, 0.25);
        }

        /// <summary>
        /// Calculates the temperature of the <see cref="CelestialBody"/>, in K.
        /// </summary>
        /// <returns>The temperature of the <see cref="CelestialBody"/>, in K.</returns>
        public float GetTotalTemperature() => GetTotalTemperatureFromPosition(Position);

        /// <summary>
        /// Calculates the total average temperature of the <see cref="CelestialBody"/> as if this
        /// object was at the apoapsis of its orbit, in K.
        /// </summary>
        /// <remarks>
        /// Uses current position if this object is not in an orbit, or if its apoapsis is infinite.
        /// </remarks>
        /// <returns>The total average temperature of the <see cref="CelestialBody"/> at apoapsis, in K.</returns>
        public void GetTotalTemperatureAtApoapsis()
        {
            if (Orbit == null)
            {
                _totalTemperatureAtApoapsis = GetTotalTemperatureFromPosition(Position);
                return;
            }

            var apoapsis = Orbit.Apoapsis;
            if (double.IsInfinity(apoapsis))
            {
                _totalTemperatureAtApoapsis = GetTotalTemperatureFromPosition(Position);
                return;
            }

            // Actual position doesn't matter for temperature, only distance.
            Vector3 apoapsisVector = Orbit.OrbitedObject.Position + (Vector3.UnitX * (float)(apoapsis / Parent.LocalScale));
            _totalTemperatureAtApoapsis = GetTotalTemperatureFromPosition(apoapsisVector);
        }

        /// <summary>
        /// Calculates the total average temperature of the <see cref="CelestialBody"/> as if this
        /// object was at the periapsis of its orbit, in K.
        /// </summary>
        /// <remarks>
        /// Uses current position if this object is not in an orbit.
        /// </remarks>
        /// <returns>The total average temperature of the <see cref="CelestialBody"/> at periapsis, in K.</returns>
        public void GetTotalTemperatureAtPeriapsis()
        {
            if (Orbit == null)
            {
                _totalTemperatureAtPeriapsis = GetTotalTemperatureFromPosition(Position);
                return;
            }

            // Actual position doesn't matter for temperature, only distance.
            Vector3 periapsis = Orbit.OrbitedObject.Position + (Vector3.UnitX * (float)(Orbit.Periapsis / Parent.LocalScale));
            _totalTemperatureAtPeriapsis = GetTotalTemperatureFromPosition(periapsis);
        }

        /// <summary>
        /// Calculates the temperature of the <see cref="CelestialBody"/>, averaged between periapsis
        /// and apoapsis, in K.
        /// </summary>
        /// <param name="polar">
        /// If true, calculates the approximate temperature at the <see cref="CelestialBody"/>'s poles.
        /// </param>
        /// <returns>The average temperature of the <see cref="CelestialBody"/>.</returns>
        internal float GetTotalTemperatureAverageOrbital()
        {
            // Only bother calculating twice if the body is actually in orbit.
            if (Orbit == null)
            {
                return GetTotalTemperature();
            }
            else
            {
                return (TotalTemperatureAtPeriapsis + TotalTemperatureAtApoapsis) / 2.0f;
            }
        }

        /// <summary>
        /// Calculates the total average temperature of the <see cref="CelestialBody"/> as if this
        /// object was at the specified position, including ambient heat of its parent and radiated
        /// heat from all sibling objects, in K.
        /// </summary>
        /// <param name="position">
        /// A hypothetical position for this <see cref="CelestialBody"/> at which its temperature
        /// will be calculated.
        /// </param>
        /// <returns>
        /// The total average temperature of the <see cref="CelestialBody"/> at the given position,
        /// in K.
        /// </returns>
        internal float GetTotalTemperatureFromPosition(Vector3 position) => (Temperature ?? 0) + GetInsolationHeat(position);
    }
}
