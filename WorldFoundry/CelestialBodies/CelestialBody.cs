using MathAndScience;
using System;
using System.Linq;
using MathAndScience.Numerics;
using WorldFoundry.CelestialBodies.Stars;
using WorldFoundry.Orbits;
using WorldFoundry.Space;

namespace WorldFoundry.CelestialBodies
{
    /// <summary>
    /// Represents any contiguous physical object in space, such as a star or planet.
    /// </summary>
    public class CelestialBody : Orbiter
    {
        private double? _albedo;
        /// <summary>
        /// The average albedo of the <see cref="CelestialBody"/> (a value between 0 and 1).
        /// </summary>
        /// <remarks>
        /// This refers to the total albedo of the body, including any atmosphere, not just
        /// the surface albedo of the main body.
        /// </remarks>
        public double Albedo
        {
            get
            {
                if (!_albedo.HasValue)
                {
                    GenerateAlbedo();
                }
                return _albedo ?? 0;
            }
            set
            {
                _albedo = value;
                ResetCachedTemperatures();
            }
        }

        private Orbit _orbit;
        /// <summary>
        /// The orbit occupied by this <see cref="Orbiter"/> (may be null).
        /// </summary>
        public override Orbit Orbit
        {
            get => _orbit;
            set
            {
                _orbit = value;
                ResetCachedTemperatures();
            }
        }

        /// <summary>
        /// The total temperature of this body averaged over its orbit (if any).
        /// </summary>
        public virtual double AverageSurfaceTemperature => AverageBlackbodySurfaceTemperature;

        private double? _averageBlackbodySurfaceTemperature;
        /// <summary>
        /// The total temperature of this body averaged over its orbit (if any).
        /// </summary>
        public double AverageBlackbodySurfaceTemperature
            => _averageBlackbodySurfaceTemperature ?? (_averageBlackbodySurfaceTemperature = GetAverageBlackbodySurfaceTemperature()).Value;

        private double? _blackbodySurfaceTemperature;
        /// <summary>
        /// The total temperature of this body.
        /// </summary>
        public double BlackbodySurfaceTemperature
            => _blackbodySurfaceTemperature ?? (_blackbodySurfaceTemperature = GetSurfaceTemperature()).Value;

        private double? _surfaceTemperatureAtApoapsis;
        /// <summary>
        /// The total temperature of this body when at the apoapsis of its orbit (if any).
        /// </summary>
        internal double SurfaceTemperatureAtApoapsis
            => _surfaceTemperatureAtApoapsis ?? (_surfaceTemperatureAtApoapsis = GetSurfaceTemperatureAtApoapsis()).Value;

        private double? _surfaceTemperatureAtPeriapsis;
        /// <summary>
        /// The total temperature of this body when at the periapsis of its orbit (if any).
        /// </summary>
        internal double SurfaceTemperatureAtPeriapsis
            => _surfaceTemperatureAtPeriapsis ?? (_surfaceTemperatureAtPeriapsis = GetSurfaceTemperatureAtPeriapsis()).Value;

        /// <summary>
        /// Initializes a new instance of <see cref="CelestialBody"/>.
        /// </summary>
        public CelestialBody() { }

        /// <summary>
        /// Initializes a new instance of <see cref="CelestialBody"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="CelestialBody"/> is located.
        /// </param>
        public CelestialBody(CelestialRegion parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="CelestialBody"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="CelestialBody"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="CelestialBody"/>.</param>
        public CelestialBody(CelestialRegion parent, Vector3 position) : base(parent, position) { }

        /// <summary>
        /// Calculates the escape velocity from this body, in m/s.
        /// </summary>
        /// <returns>The escape velocity from this body, in m/s.</returns>
        public double GetEscapeVelocity() => Math.Sqrt(ScienceConstants.TwoG * Mass / Radius);

        /// <summary>
        /// Calculates the temperature of the <see cref="CelestialBody"/>, in K.
        /// </summary>
        /// <returns>The temperature of the <see cref="CelestialBody"/>, in K.</returns>
        public double GetSurfaceTemperature() => GetSurfaceTemperatureAtPosition(Position);

        /// <summary>
        /// Estimates the total average temperature of the <see cref="CelestialBody"/> as if this
        /// object was at the specified true anomaly in its orbit, including ambient heat of its
        /// parent and radiated heat from all sibling objects, in K. If the body is not in orbit,
        /// returns the temperature at its current position.
        /// </summary>
        /// <param name="trueAnomaly">
        /// A true anomaly at which its temperature will be calculated.
        /// </param>
        /// <returns>
        /// The total average temperature of the <see cref="CelestialBody"/> at the given position,
        /// in K.
        /// </returns>
        /// <remarks>
        /// The estimation is performed by linear interpolation between the temperature at periapsis
        /// and apoapsis, which is not necessarily accurate for highly elliptical orbits, or bodies
        /// with multiple significant nearby heat sources, but it should be fairly accurate for
        /// bodies in fairly circular orbits around heat sources which are all close to the center
        /// of the orbit, and much faster for successive calls than calculating the temperature at
        /// specific positions precisely.
        /// </remarks>
        internal double GetSurfaceTemperatureAtTrueAnomaly(double trueAnomaly)
            => MathUtility.Lerp(SurfaceTemperatureAtPeriapsis, SurfaceTemperatureAtApoapsis, trueAnomaly);

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
        internal double GetSurfaceTemperatureAtPosition(Vector3 position)
            => (Temperature ?? 0) + GetInsolationHeat(position);

        /// <summary>
        /// Determines an albedo for this <see cref="CelestialBody"/> (a value between 0 and 1).
        /// </summary>
        /// <remarks>
        /// Sets 0 in the base class; subclasses which are not black-bodies are expected to override.
        /// </remarks>
        private protected virtual void GenerateAlbedo() => Albedo = 0;

        /// <summary>
        /// Calculates the temperature of the <see cref="CelestialBody"/>, averaged over its orbit,
        /// in K.
        /// </summary>
        private double GetAverageBlackbodySurfaceTemperature()
            => Orbit == null
                ? GetSurfaceTemperature()
                : ((SurfaceTemperatureAtPeriapsis * (1 + Orbit.Eccentricity)) + (SurfaceTemperatureAtApoapsis * (1 - Orbit.Eccentricity))) / 2.0;

        /// <summary>
        /// Calculates the insolation received by this <see cref="CelestialBody"/> at the given
        /// position, in W/m².
        /// </summary>
        /// <param name="position">
        /// A hypothetical position for this <see cref="CelestialBody"/> at which the insolation
        /// will be calculated.
        /// </param>
        /// <returns>
        /// The insolation received by this <see cref="CelestialBody"/> at the given position, in
        /// W/m².
        /// </returns>
        private double GetInsolation(Vector3 position)
            => (1 - Albedo) * Parent
                .GetAllChildren<Star>()
                .Where(x => x != this)
                .Sum(x => x.Luminosity / (MathConstants.FourPI * Math.Pow(Location.GetDistanceFromPositionTo(position, x.Location), 2)));

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
        private protected virtual double GetInsolationHeat(Vector3 position)
            => Math.Pow(GetInsolation(position) / ScienceConstants.FourSigma, 0.25);

        /// <summary>
        /// Calculates the total average temperature of the <see cref="CelestialBody"/> as if this
        /// object was at the apoapsis of its orbit, in K.
        /// </summary>
        /// <remarks>
        /// Uses current position if this object is not in an orbit, or if its apoapsis is infinite.
        /// </remarks>
        private double GetSurfaceTemperatureAtApoapsis()
        {
            // Actual position doesn't matter for temperature, only distance.
            var position = Orbit == null || double.IsInfinity(Orbit.Apoapsis)
                ? Position
                : Orbit.OrbitedObject.Position + (Vector3.UnitX * Orbit.Apoapsis);
            return GetSurfaceTemperatureAtPosition(position);
        }

        /// <summary>
        /// Calculates the total average temperature of the <see cref="CelestialBody"/> as if this
        /// object was at the periapsis of its orbit, in K.
        /// </summary>
        /// <remarks>
        /// Uses current position if this object is not in an orbit.
        /// </remarks>
        private double GetSurfaceTemperatureAtPeriapsis()
        {
            // Actual position doesn't matter for temperature, only distance.
            var position = Orbit == null
                ? Position
                : Orbit.OrbitedObject.Position + (Vector3.UnitX * Orbit.Periapsis);
            return GetSurfaceTemperatureAtPosition(position);
        }

        private protected virtual void ResetCachedTemperatures()
        {
            _averageBlackbodySurfaceTemperature = null;
            _blackbodySurfaceTemperature = null;
            _surfaceTemperatureAtApoapsis = null;
            _surfaceTemperatureAtPeriapsis = null;
        }

        private protected virtual void ResetOrbitalProperties() { }
    }
}
