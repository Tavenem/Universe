using NeverFoundry.MathAndScience.Numerics;

namespace WorldFoundry.Space
{
    /// <summary>
    /// The parameters which describe an orbit.
    /// </summary>
    public struct OrbitalParameters
    {
        /// <summary>
        /// The angle between the X-axis and the plane of the orbit (at the intersection where the
        /// orbit is rising, in radians). Values will be normalized between zero and 2π.
        /// </summary>
        public double AngleAscending;

        /// <summary>
        /// The angle between the intersection of the X-Z plane through the center of the object
        /// orbited and the orbital plane, and the periapsis, in radians. Values will be normalized
        /// between zero and 2π
        /// </summary>
        public double ArgumentOfPeriapsis;

        /// <summary>
        /// If true, all other parameters are ignored, and a circular orbit will be set based on the
        /// orbiting object's position.
        /// </summary>
        public bool Circular;

        /// <summary>
        /// The eccentricity of this orbit.
        /// </summary>
        public double Eccentricity;

        /// <summary>
        /// The angle between the X-Z plane through the center of the object orbited, and the plane
        /// of the orbit, in radians.
        /// </summary>
        public double Inclination;

        /// <summary>
        /// The entity which is being orbited.
        /// </summary>
        public CelestialLocation OrbitedObject;

        /// <summary>
        /// The periapsis of this orbit, in meters.
        /// </summary>
        public Number Periapsis;

        /// <summary>
        /// The current true anomaly of this orbit.
        /// </summary>
        public double TrueAnomaly;

        /// <summary>
        /// Initializes a new instance of <see cref="OrbitalParameters"/>.
        /// </summary>
        /// <param name="orbitedObject">The entity which is being orbited.</param>
        /// <param name="periapsis">
        /// The distance between the objects at the closest point in the orbit.
        /// </param>
        /// <param name="eccentricity">The degree to which the orbit is non-circular. The absolute
        /// value will be used (i.e. negative values are treated as positives).</param>
        /// <param name="inclination">
        /// The angle between the X-Z plane through the center of the object orbited, and the plane
        /// of the orbit, in radians. Values will be normalized between zero and π.
        /// </param>
        /// <param name="angleAscending">
        /// The angle between the X-axis and the plane of the orbit (at the intersection where the
        /// orbit is rising, in radians). Values will be normalized between zero and 2π.
        /// </param>
        /// <param name="argPeriapsis">
        /// The angle between the intersection of the X-Z plane through the center of the object
        /// orbited and the orbital plane, and the periapsis, in radians. Values will be normalized
        /// between zero and 2π
        /// </param>
        /// <param name="trueAnomaly">
        /// The angle between periapsis and the current position of this object, from the center of
        /// the object orbited, in radians. Values will be normalized between zero and 2π
        /// </param>
        public OrbitalParameters(
            CelestialLocation orbitedObject,
            Number periapsis,
            double eccentricity,
            double inclination,
            double angleAscending,
            double argPeriapsis,
            double trueAnomaly)
        {
            Circular = false;
            OrbitedObject = orbitedObject;
            Periapsis = periapsis;
            Eccentricity = eccentricity;
            Inclination = inclination;
            AngleAscending = angleAscending;
            ArgumentOfPeriapsis = argPeriapsis;
            TrueAnomaly = trueAnomaly;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="OrbitalParameters"/>.
        /// </summary>
        /// <param name="orbitedObject">The entity which is being orbited.</param>
        public OrbitalParameters(CelestialLocation orbitedObject)
        {
            Circular = true;
            OrbitedObject = orbitedObject;
            Periapsis = Number.Zero;
            Eccentricity = 0;
            Inclination = 0;
            AngleAscending = 0;
            ArgumentOfPeriapsis = 0;
            TrueAnomaly = 0;
        }
    }
}
