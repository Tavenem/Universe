using MathAndScience;
using System;
using MathAndScience.Numerics;

namespace WorldFoundry.Space
{
    /// <summary>
    /// Defines an orbit by the Kepler elements.
    /// </summary>
    public class Orbit
    {
        private const double Tolerance = 1.0e-8F;

        private double? _alpha;
        /// <summary>
        /// Derived value equal to the standard gravitational parameter divided by the semi-major axis.
        /// </summary>
        public double Alpha
        {
            get
            {
                if (!_alpha.HasValue)
                {
                    SetParametersFromPositionAndVelocity();
                }
                return _alpha ?? 0;
            }
        }

        private double? _angleAscending;
        /// <summary>
        /// The right ascension of the ascending node of this orbit.
        /// </summary>
        public double AngleAscending
        {
            get
            {
                if (!_angleAscending.HasValue)
                {
                    SetParametersFromPositionAndVelocity();
                }
                return _angleAscending ?? 0;
            }
        }

        private double? _apoapsis;
        /// <summary>
        /// The apoapsis of this orbit. For orbits with <see cref="Eccentricity"/> >= 1, gives <see cref="double.PositiveInfinity"/>.
        /// </summary>
        public double Apoapsis
        {
            get
            {
                if (!_apoapsis.HasValue)
                {
                    SetParametersFromPositionAndVelocity();
                }
                return _apoapsis ?? 0;
            }
        }

        private double? _argumentPeriapsis;
        /// <summary>
        /// The argument of periapsis of this orbit.
        /// </summary>
        public double ArgumentPeriapsis
        {
            get
            {
                if (!_argumentPeriapsis.HasValue)
                {
                    SetParametersFromPositionAndVelocity();
                }
                return _argumentPeriapsis ?? 0;
            }
        }

        private double? _eccentricity;
        /// <summary>
        /// The eccentricity of this orbit.
        /// </summary>
        public double Eccentricity
        {
            get
            {
                if (!_eccentricity.HasValue)
                {
                    SetParametersFromPositionAndVelocity();
                }
                return _eccentricity ?? 0;
            }
        }

        private double? _inclination;
        /// <summary>
        /// The angle between the X-Z plane through the center of the object orbited, and the plane
        /// of the orbit, in radians.
        /// </summary>
        public double Inclination
        {
            get
            {
                if (!_inclination.HasValue)
                {
                    SetParametersFromPositionAndVelocity();
                }
                return _inclination ?? 0;
            }
        }

        private CelestialEntity _orbitedObject;
        /// <summary>
        /// The object which is being orbited.
        /// </summary>
        public CelestialEntity OrbitedObject
        {
            get => _orbitedObject;
            private set
            {
                _orbitedObject = value;
                ClearParameters();
            }
        }

        private CelestialEntity _orbitingObject;
        /// <summary>
        /// The object which is orbiting.
        /// </summary>
        public CelestialEntity OrbitingObject
        {
            get => _orbitingObject;
            private set
            {
                _orbitingObject = value;

                ClearParameters();
            }
        }

        private double? _periapsis;
        /// <summary>
        /// The periapsis of this orbit.
        /// </summary>
        public double Periapsis
        {
            get
            {
                if (!_periapsis.HasValue)
                {
                    SetParametersFromPositionAndVelocity();
                }
                return _periapsis ?? 0;
            }
        }

        private Vector3? _perifocalP;
        /// <summary>
        /// The perifocal P vector for this orbit.
        /// </summary>
        public Vector3 PerifocalP
        {
            get
            {
                if (!_perifocalP.HasValue)
                {
                    SetParametersFromPositionAndVelocity();
                }
                return _perifocalP ?? Vector3.Zero;
            }
            private set => _perifocalP = value;
        }

        private double? _period;
        /// <summary>
        /// The period of this orbit.
        /// </summary>
        public double Period
        {
            get
            {
                if (!_period.HasValue)
                {
                    SetParametersFromPositionAndVelocity();
                }
                return _period ?? 0;
            }
        }

        private Vector3? _r0;
        /// <summary>
        /// The initial position of the orbiting object relative to the orbited one.
        /// </summary>
        public Vector3 R0
        {
            get
            {
                if (!_r0.HasValue)
                {
                    SetParametersFromPositionAndVelocity();
                }
                return _r0 ?? Vector3.Zero;
            }
            private set => _r0 = value;
        }

        private double? _radius;
        /// <summary>
        /// The radius of the orbit.
        /// </summary>
        public double Radius
        {
            get
            {
                if (!_radius.HasValue)
                {
                    SetParametersFromPositionAndVelocity();
                }
                return _radius ?? 0;
            }
        }

        private double? _semiMajorAxis;
        /// <summary>
        /// The semi-major axis of this <see cref="Orbit"/>.
        /// </summary>
        public double SemiMajorAxis
        {
            get
            {
                if (!_semiMajorAxis.HasValue)
                {
                    SetParametersFromPositionAndVelocity();
                }
                return _semiMajorAxis ?? 0;
            }
        }

        private double? _standardGravitationalParameter;
        /// <summary>
        /// A derived value, equal to G * the sum of masses of the orbiting objects.
        /// </summary>
        public double StandardGravitationalParameter
        {
            get
            {
                if (!_standardGravitationalParameter.HasValue)
                {
                    SetParametersFromPositionAndVelocity();
                }
                return _standardGravitationalParameter ?? 0;
            }
        }

        private double? _trueAnomaly;
        /// <summary>
        /// The current true anomaly of this orbit.
        /// </summary>
        public double TrueAnomaly
        {
            get
            {
                if (!_trueAnomaly.HasValue)
                {
                    SetParametersFromPositionAndVelocity();
                }
                return _trueAnomaly ?? 0;
            }
        }

        private Vector3? _v0;
        /// <summary>
        /// The initial velocity of the orbiting object relative to the orbited one.
        /// </summary>
        public Vector3 V0
        {
            get
            {
                if (!_v0.HasValue)
                {
                    SetParametersFromPositionAndVelocity();
                }
                return _v0 ?? Vector3.Zero;
            }
            private set => _v0 = value;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="Orbit"/>.
        /// </summary>
        public Orbit() { }

        /// <summary>
        /// Initializes a new instance of <see cref="Orbit"/> with the given parameters.
        /// </summary>
        /// <param name="orbitingObject">The <see cref="CelestialEntity"/> which will be in orbit around
        /// <paramref name="orbitedObject"/>.</param>
        /// <param name="orbitedObject">The <see cref="CelestialEntity"/> which will be orbited by <paramref
        /// name="orbitingObject"/>.</param>
        public Orbit(CelestialEntity orbitingObject, CelestialEntity orbitedObject)
        {
            OrbitingObject = orbitingObject ?? throw new ArgumentNullException(nameof(orbitingObject), $"{nameof(orbitingObject)} cannot be null");
            OrbitedObject = orbitedObject ?? throw new ArgumentNullException(nameof(orbitedObject), $"{nameof(orbitedObject)} cannot be null");
        }

        /// <summary>
        /// Calculates the semi-major axis of an orbit of the given bodies with the given period.
        /// </summary>
        /// <param name="orbitingObject">The orbiting object.</param>
        /// <param name="orbitedObject">The orbited object.</param>
        /// <param name="period">A period, in seconds.</param>
        /// <returns>THe semi-major axis of the desired orbit, in meters.</returns>
        public static double GetSemiMajorAxisForPeriod(CelestialEntity orbitingObject, CelestialEntity orbitedObject, double period)
            => Math.Pow(ScienceConstants.G * (orbitingObject.Mass + orbitedObject.Mass) * period * period / (MathConstants.FourPI * Math.PI), 1.0 / 3.0);

        /// <summary>
        /// Sets the orbit of the given <see cref="CelestialEntity"/> based on the orbiting object's current
        /// position and the given <paramref name="eccentricity"/>, and adjusts its velocity as necessary.
        /// </summary>
        /// <param name="orbitingObject">The celestial object which will be in orbit.</param>
        /// <param name="orbitedObject">The celestial object to be orbited.</param>
        /// <param name="eccentricity">The degree to which the orbit is non-circular.</param>
        /// <remarks>
        /// The orbiting object's current position will be assumed to be on the desired orbit. An
        /// inclination will be calculated from the current position, and presumed to be the maximum inclination.
        /// </remarks>
        public static void SetOrbit(
            CelestialEntity orbitingObject,
            CelestialEntity orbitedObject,
            double eccentricity)
        {
            if (eccentricity < 0)
            {
                throw new ArgumentOutOfRangeException("eccentricity must be >= 0");
            }

            if (orbitingObject.Orbit == null)
            {
                orbitingObject.Orbit = new Orbit(orbitingObject, orbitedObject);
            }
            else
            {
                orbitingObject.Orbit.OrbitedObject = orbitedObject;
            }
            orbitingObject.Orbit._eccentricity = eccentricity;

            orbitingObject.Orbit._standardGravitationalParameter = ScienceConstants.G * (orbitedObject.Mass + orbitingObject.Mass);

            if (orbitedObject.Parent != orbitingObject.Parent)
            {
                orbitingObject.Orbit.R0 = orbitingObject.Location.GetLocalizedPosition(orbitedObject.Location, orbitedObject.Position);
            }
            else
            {
                orbitingObject.Orbit.R0 = orbitingObject.Position - orbitingObject.Position;
            }

            // Calculate magnitudes manually to avoid low-precision implementation resulting in infinity.
            var distance = Math.Sqrt(Math.Pow(orbitingObject.Orbit._r0?.X ?? 0, 2) + Math.Pow(orbitingObject.Orbit._r0?.Y ?? 0, 2) + Math.Pow(orbitingObject.Orbit._r0?.Z ?? 0, 2));

            var xz = new Vector3(orbitingObject.Orbit._r0?.X ?? 0, 0, orbitingObject.Orbit._r0?.Z ?? 0);
            orbitingObject.Orbit._inclination = Math.Acos(Math.Sqrt(Math.Pow(xz.X, 2) + Math.Pow(xz.Z, 2)) / distance);
            orbitingObject.Orbit._angleAscending = Vector3.UnitX.Angle(xz) - MathConstants.HalfPI;

            orbitingObject.Orbit._trueAnomaly = Randomizer.Instance.NextDouble(MathConstants.TwoPI);

            var semiLatusRectum = distance * (1 + (eccentricity * Math.Cos(orbitingObject.Orbit._trueAnomaly.Value)));
            orbitingObject.Orbit._semiMajorAxis = semiLatusRectum / (1 - (eccentricity * eccentricity));
            orbitingObject.Orbit._alpha = orbitingObject.Orbit._standardGravitationalParameter / orbitingObject.Orbit._semiMajorAxis;

            // The current position must be either the apoapsis or the periapsis,
            // since it was chosen as the reference point for the inclination.
            // Therefore, it is the apoapsis if its distance from the orbited
            // body is less than the semi-major axis, and the periapsis if not.
            var e = distance < orbitingObject.Orbit._semiMajorAxis
                ? orbitingObject.Orbit.R0
                : Vector3.Normalize(new Vector3(-orbitingObject.Orbit._r0?.X ?? 0, -orbitingObject.Orbit._r0?.Y ?? 0, -orbitingObject.Orbit._r0?.Z ?? 0)) * (semiLatusRectum / (1 + eccentricity));
            // For parabolic orbits, semi-major axis is undefined, and is set to the periapsis instead.
            if (eccentricity == 1)
            {
                orbitingObject.Orbit._semiMajorAxis = e.Length();
            }

            // Calculate the perifocal vectors
            var cosineAngleAscending = Math.Cos(orbitingObject.Orbit._angleAscending.Value);
            var sineAngleAscending = Math.Sin(orbitingObject.Orbit._angleAscending.Value);
            var sineInclination = Math.Sin(orbitingObject.Orbit._inclination.Value);
            var cosineInclination = Math.Cos(orbitingObject.Orbit._inclination.Value);

            orbitingObject.Orbit._argumentPeriapsis = Math.Atan2((orbitingObject.Orbit._r0?.Z ?? 0) / sineInclination, ((orbitingObject.Orbit._r0?.X ?? 0) * cosineAngleAscending) + ((orbitingObject.Orbit._r0?.Y ?? 0) * sineAngleAscending)) - orbitingObject.Orbit._trueAnomaly.Value;

            var cosineArgPeriapsis = Math.Cos(orbitingObject.Orbit._argumentPeriapsis.Value);
            var sineArgPeriapsis = Math.Sin(orbitingObject.Orbit._argumentPeriapsis.Value);

            var pi = (cosineAngleAscending * cosineArgPeriapsis) - (sineAngleAscending * cosineInclination * sineArgPeriapsis);
            var pj = (sineAngleAscending * cosineArgPeriapsis) + (cosineAngleAscending * cosineInclination * sineArgPeriapsis);
            var pk = sineInclination * sineArgPeriapsis;

            var qi = -(cosineAngleAscending * sineArgPeriapsis) - (sineAngleAscending * cosineInclination * cosineArgPeriapsis);
            var qj = -(sineAngleAscending * sineArgPeriapsis) + (cosineAngleAscending * cosineInclination * cosineArgPeriapsis);
            var qk = sineInclination * cosineArgPeriapsis;

            var perifocalP = (pi * Vector3.UnitX) + (pj * Vector3.UnitY) + (pk * Vector3.UnitZ);
            var perifocalQ = (qi * Vector3.UnitX) + (qj * Vector3.UnitY) + (qk * Vector3.UnitZ);

            var cosineTrueAnomaly = Math.Cos(orbitingObject.Orbit._trueAnomaly.Value);
            var sineTrueAnomaly = Math.Sin(orbitingObject.Orbit._trueAnomaly.Value);

            orbitingObject.Orbit.V0 = Math.Sqrt(orbitingObject.Orbit._standardGravitationalParameter.Value / semiLatusRectum)
                * ((-sineTrueAnomaly * perifocalP) + (eccentricity * perifocalQ) + (cosineTrueAnomaly * perifocalQ));

            orbitingObject.Velocity = orbitingObject.Orbit.V0;
        }

        /// <summary>
        /// Sets the orbit of the given <see cref="CelestialEntity"/> according to the given orbital
        /// parameters, and adjusts its position and velocity as necessary.
        /// </summary>
        /// <param name="orbitingObject">The celestial object which will be in orbit.</param>
        /// <param name="orbitedObject">The celestial object to be orbited.</param>
        /// <param name="periapsis">
        /// The distance between the objects at the closest point in the orbit.
        /// </param>
        /// <param name="eccentricity">The degree to which the orbit is non-circular.</param>
        /// <param name="inclination">
        /// The angle between the X-Z plane through the center of the object orbited, and the plane
        /// of the orbit, in radians.
        /// </param>
        /// <param name="angleAscending">
        /// The angle between the X-axis and the plane of the orbit (at the intersection where the
        /// orbit is rising, in radians).
        /// </param>
        /// <param name="argPeriapsis">
        /// The angle between the intersection of the X-Z plane through the center of the object
        /// orbited and the orbital plane, and the periapsis, in radians.
        /// </param>
        /// <param name="trueAnomaly">
        /// The angle between periapsis and the current position of this object, from the center of
        /// the object orbited, in radians.
        /// </param>
        public static void SetOrbit(
            CelestialEntity orbitingObject,
            CelestialEntity orbitedObject,
            double periapsis,
            double eccentricity,
            double inclination,
            double angleAscending,
            double argPeriapsis,
            double trueAnomaly)
        {
            if (eccentricity < 0)
            {
                throw new ArgumentOutOfRangeException("eccentricity must be >= 0");
            }
            if (inclination < 0 || inclination >= Math.PI)
            {
                throw new ArgumentOutOfRangeException("inclination must be >= 0 and < π");
            }
            if (angleAscending < 0 || angleAscending >= MathConstants.TwoPI)
            {
                throw new ArgumentOutOfRangeException("angleAscending must be >= 0 and < 2π");
            }
            if (argPeriapsis < 0 || argPeriapsis >= MathConstants.TwoPI)
            {
                throw new ArgumentOutOfRangeException("argPeriapsis must be >= 0 and < 2π");
            }
            if (trueAnomaly < 0 || trueAnomaly >= MathConstants.TwoPI)
            {
                throw new ArgumentOutOfRangeException("trueAnomaly must be >= 0 and < 2π");
            }

            if (orbitingObject.Orbit == null)
            {
                orbitingObject.Orbit = new Orbit(orbitingObject, orbitedObject);
            }
            else
            {
                orbitingObject.Orbit.OrbitedObject = orbitedObject;
            }
            orbitingObject.Orbit._angleAscending = angleAscending;
            orbitingObject.Orbit._argumentPeriapsis = argPeriapsis;
            orbitingObject.Orbit._eccentricity = eccentricity;
            orbitingObject.Orbit._inclination = inclination;
            orbitingObject.Orbit._periapsis = periapsis;
            orbitingObject.Orbit._standardGravitationalParameter = ScienceConstants.G * (orbitedObject.Mass + orbitingObject.Mass);
            orbitingObject.Orbit._trueAnomaly = trueAnomaly;

            var semiLatusRectum = periapsis * (1 + eccentricity);

            // For parabolic orbits, semi-major axis is undefined, and is set to the periapsis instead.
            if (eccentricity == 1)
            {
                orbitingObject.Orbit._semiMajorAxis = periapsis;
            }
            else
            {
                orbitingObject.Orbit._semiMajorAxis = semiLatusRectum / (1 - (eccentricity * eccentricity));
            }

            orbitingObject.Orbit._alpha = orbitingObject.Orbit._standardGravitationalParameter / orbitingObject.Orbit._semiMajorAxis;

            // Calculate the perifocal vectors
            var cosineAngleAscending = Math.Cos(angleAscending);
            var sineAngleAscending = Math.Sin(angleAscending);
            var cosineArgPeriapsis = Math.Cos(argPeriapsis);
            var sineArgPeriapsis = Math.Sin(argPeriapsis);
            var cosineInclination = Math.Cos(inclination);
            var sineInclination = Math.Sin(inclination);

            var pi = (cosineAngleAscending * cosineArgPeriapsis) - (sineAngleAscending * cosineInclination * sineArgPeriapsis);
            var pj = (sineAngleAscending * cosineArgPeriapsis) + (cosineAngleAscending * cosineInclination * sineArgPeriapsis);
            var pk = sineInclination * sineArgPeriapsis;

            var qi = -(cosineAngleAscending * sineArgPeriapsis) - (sineAngleAscending * cosineInclination * cosineArgPeriapsis);
            var qj = -(sineAngleAscending * sineArgPeriapsis) + (cosineAngleAscending * cosineInclination * cosineArgPeriapsis);
            var qk = sineInclination * cosineArgPeriapsis;

            var perifocalP = (pi * Vector3.UnitX) + (pj * Vector3.UnitY) + (pk * Vector3.UnitZ);
            var perifocalQ = (qi * Vector3.UnitX) + (qj * Vector3.UnitY) + (qk * Vector3.UnitZ);

            var cosineTrueAnomaly = Math.Cos(trueAnomaly);
            var sineTrueAnomaly = Math.Sin(trueAnomaly);
            orbitingObject.Orbit._radius = semiLatusRectum / (1 + (eccentricity * cosineTrueAnomaly));

            orbitingObject.Orbit.R0 = (orbitingObject.Orbit._radius.Value * cosineTrueAnomaly * perifocalP) + (orbitingObject.Orbit._radius.Value * sineTrueAnomaly * perifocalQ);
            orbitingObject.Orbit.V0 = Math.Sqrt(orbitingObject.Orbit._standardGravitationalParameter.Value / semiLatusRectum)
                * ((-sineTrueAnomaly * perifocalP) + (eccentricity * perifocalQ) + (cosineTrueAnomaly * perifocalQ));

            if (orbitingObject.Parent != orbitedObject.Parent)
            {
                orbitingObject.Position += orbitingObject.Location.GetLocalizedPosition(orbitedObject.Location, orbitingObject.Orbit.R0);
            }
            else
            {
                orbitingObject.Position = orbitedObject.Position + orbitingObject.Orbit.R0;
            }
            orbitingObject.Velocity = orbitingObject.Orbit.V0;
        }

        /// <summary>
        /// Calculates the radius of the orbiting body's Hill sphere, in meters.
        /// </summary>
        /// <param name="orbitingObject">The object which is orbiting.</param>
        /// <param name="orbitedObject">The object which is being orbited.</param>
        /// <param name="semiMajorAxis">The semi-major axis of the orbit.</param>
        /// <param name="eccentricity">The eccentricity of the orbit.</param>
        /// <returns>The radius of the orbiting body's Hill sphere, in meters.</returns>
        public static double GetHillSphereRadius(CelestialEntity orbitingObject, CelestialEntity orbitedObject, double semiMajorAxis, double eccentricity)
            => semiMajorAxis * (1 - eccentricity) * Math.Pow(orbitingObject.Mass / (3 * orbitedObject.Mass), 1.0 / 3.0);

        /// <summary>
        /// Calculates the radius of the orbiting body's Hill sphere, in meters.
        /// </summary>
        /// <returns>The radius of the orbiting body's Hill sphere, in meters.</returns>
        public double GetHillSphereRadius() => GetHillSphereRadius(OrbitingObject, OrbitedObject, SemiMajorAxis, Eccentricity);

        /// <summary>
        /// Gets updated orbital position and velocity vectors.
        /// </summary>
        /// <param name="t">The number of seconds which have elapsed since the conditions when the
        /// orbit was defined were true.</param>
        /// <returns>An array with 2 elements: the position vector (relative to the orbited object),
        /// and the velocity vector.</returns>
        public (Vector3 position, Vector3 velocity) GetStateVectorsAtTime(double t)
        {
            // Universal variable formulas; Newton's method

            var sqrtSGP = Math.Sqrt(StandardGravitationalParameter);
            var accel = Radius / V0.Length();
            var f = 1.0 - (Alpha * Radius);

            // Initial guess for x
            var x = sqrtSGP * Math.Abs(Alpha) * t;

            // Find acceptable x
            var ratio = GetUniversalVariableFormulaRatio(x, t, sqrtSGP, accel, f);
            while (Math.Abs(ratio) > Tolerance)
            {
                x -= ratio;
                ratio = GetUniversalVariableFormulaRatio(x, t, sqrtSGP, accel, f);
            }

            var x2 = x * x;
            var x3 = Math.Pow(x, 3);
            var ax2 = Alpha * x2;
            var ssax2 = StumpffS(ax2);
            var scax2 = StumpffC(ax2);
            var ssax2x3 = ssax2 * x3;

            var uvf = 1.0 - (x2 / Radius * scax2);
            var uvg = t - (1.0 / sqrtSGP * ssax2x3);

            var r = (R0 * uvf) + (V0 * uvg);
            var rLength = r.Length();

            var uvfp = sqrtSGP / (rLength * Radius) * ((Alpha * ssax2x3) - x);
            var uvfgp = 1.0 - (x2 / rLength * scax2);

            var v = (R0 * uvfp) + (V0 * uvfgp);

            return (r, v);
        }

        /// <summary>
        /// Updates the orbital position and velocity of this <see cref="Orbit"/> after the
        /// specified number of seconds have passed, assuming no influences on the CelestialEntity's motion
        /// have occurred, aside from its orbit.
        /// </summary>
        /// <param name="elapsedSeconds">
        /// The number of seconds which have elapsed since the orbit was last updated.
        /// </param>
        public void UpdateOrbit(double elapsedSeconds)
        {
            var (position, velocity) = GetStateVectorsAtTime(elapsedSeconds);

            if (OrbitedObject.Parent != OrbitingObject.Parent)
            {
                OrbitingObject.Position = OrbitingObject.Location.Parent.GetLocalizedPosition(OrbitedObject.Location) + position;
            }
            else
            {
                OrbitingObject.Position = OrbitedObject.Position + position;
            }

            OrbitingObject.Velocity = velocity;

            ClearParameters();
            SetParametersFromPositionAndVelocity();
        }

        /// <summary>
        /// Updates an orbit from the current position and velocity of its <see cref="CelestialEntity"/>
        /// objects.
        /// </summary>
        public void UpdateOrbit()
        {
            ClearParameters();
            SetParametersFromPositionAndVelocity();
        }

        internal static Orbit GetCircularOrbit(CelestialEntity orbitingObject, CelestialEntity orbitedObject)
        {
            var orbit = new Orbit(orbitingObject, orbitedObject)
            {
                _eccentricity = 0,
                _standardGravitationalParameter = ScienceConstants.G * (orbitedObject.Mass + orbitingObject.Mass),
            };

            orbit.R0 = orbitingObject.Position - orbitedObject.Position;
            orbit._radius = orbit.R0.Length();

            // Calculate magnitudes manually to avoid low-precision
            // implementation resulting in infinity.
            var r0x2 = Math.Pow(orbit._r0?.X ?? 0, 2);
            var r0z2 = Math.Pow(orbit._r0?.Z ?? 0, 2);
            orbit._semiMajorAxis = Math.Sqrt(r0x2 + Math.Pow(orbit._r0?.Y ?? 0, 2) + r0z2);
            orbit._apoapsis = orbit._semiMajorAxis;
            orbit._periapsis = orbit._semiMajorAxis;

            var xz = new Vector3(orbit._r0?.X ?? 0, 0, orbit._r0?.Z ?? 0);
            orbit._inclination = Math.Acos(Math.Sqrt(r0x2 + r0z2) / orbit._semiMajorAxis.Value);
            var angleAscending = Vector3.UnitX.Angle(xz) - MathConstants.HalfPI;

            var n = new Vector3(Math.Cos(angleAscending), Math.Sin(angleAscending), 0);
            var argPeriapsis = Math.Acos(Vector3.Dot(n, orbit.R0) / (n.Length() * orbit.Radius));
            if ((orbit._r0?.Z ?? 0) < 0)
            {
                argPeriapsis = MathConstants.TwoPI - argPeriapsis;
            }

            // Calculate the perifocal vectors
            var cosineAngleAscending = Math.Cos(angleAscending);
            var sineAngleAscending = Math.Sin(angleAscending);
            var cosineArgPeriapsis = Math.Cos(argPeriapsis);
            var sineArgPeriapsis = Math.Sin(argPeriapsis);
            var cosineInclination = Math.Cos(orbit._inclination.Value);
            var sineInclination = Math.Sin(orbit._inclination.Value);

            var qi = -(cosineAngleAscending * sineArgPeriapsis) - (sineAngleAscending * cosineInclination * cosineArgPeriapsis);
            var qj = -(sineAngleAscending * sineArgPeriapsis) + (cosineAngleAscending * cosineInclination * cosineArgPeriapsis);
            var qk = sineInclination * cosineArgPeriapsis;

            var perifocalQ = (qi * Vector3.UnitX) + (qj * Vector3.UnitY) + (qk * Vector3.UnitZ);

            orbit._trueAnomaly = 0;

            orbit._alpha = orbit._standardGravitationalParameter.Value / orbit._semiMajorAxis.Value;
            orbit.V0 = Math.Sqrt(orbit._alpha.Value) * perifocalQ;

            return orbit;
        }

        internal static Vector3 GetDeltaVForCircularOrbit(CelestialEntity orbitingObject, CelestialEntity orbitedObject)
        {
            var orbit = new Orbit(orbitingObject, orbitedObject);

            var h = Vector3.Cross(orbit.R0, orbit.V0);
            var inclination = Math.Acos(h.Z / h.Length());
            var n = Vector3.Cross(Vector3.UnitZ, h);
            var angleAscending = Math.Acos(n.X / n.Length());

            // Calculate the perifocal vector
            var cosineAngleAscending = Math.Cos(angleAscending);
            var sineAngleAscending = Math.Sin(angleAscending);
            var cosineInclination = Math.Cos(inclination);
            var sineInclination = Math.Sin(inclination);

            var qi = -(sineAngleAscending * cosineInclination);
            var qj = cosineAngleAscending * cosineInclination;
            var qk = sineInclination;

            var perifocalQ = (qi * Vector3.UnitX) + (qj * Vector3.UnitY) + (qk * Vector3.UnitZ);

            return Math.Sqrt(orbit.StandardGravitationalParameter / orbit.Radius) * perifocalQ;
        }

        /// <summary>
        /// Approximates the radius of the orbiting body's mutual Hill sphere with another
        /// orbiting body in orbit around the same primary, in meters.
        /// </summary>
        /// <remarks>
        /// Assumes the semimajor axis of both orbits is identical for the purposes of the
        /// calculation, which obviously would not be the case, but generates reasonably close
        /// estimates in the absence of actual values.
        /// </remarks>
        /// <param name="otherMass">
        /// The mass of another celestial body presumed to be orbiting the same primary as this one.
        /// </param>
        /// <returns>The radius of the orbiting body's Hill sphere, in meters.</returns>
        internal double GetMutualHillSphereRadius(double otherMass)
            => Math.Pow((OrbitingObject.Mass + otherMass) / (3 * OrbitedObject.Mass), 1.0 / 3.0) * SemiMajorAxis;

        internal double GetSphereOfInfluenceRadius()
            => SemiMajorAxis * Math.Pow(OrbitingObject.Mass / OrbitedObject.Mass, 2.0 / 5.0);

        internal Vector3 GetPositionForTrueAnomaly(double trueAnomaly)
        {
            var cosineAngleAscending = Math.Cos(_angleAscending.Value);
            var sineAngleAscending = Math.Sin(_angleAscending.Value);
            var sineInclination = Math.Sin(_inclination.Value);
            var cosineInclination = Math.Cos(_inclination.Value);
            var cosineArgPeriapsis = Math.Cos(_argumentPeriapsis.Value);
            var sineArgPeriapsis = Math.Sin(_argumentPeriapsis.Value);

            var pi = (cosineAngleAscending * cosineArgPeriapsis) - (sineAngleAscending * cosineInclination * sineArgPeriapsis);
            var pj = (sineAngleAscending * cosineArgPeriapsis) + (cosineAngleAscending * cosineInclination * sineArgPeriapsis);
            var pk = sineInclination * sineArgPeriapsis;

            var qi = -(cosineAngleAscending * sineArgPeriapsis) - (sineAngleAscending * cosineInclination * cosineArgPeriapsis);
            var qj = -(sineAngleAscending * sineArgPeriapsis) + (cosineAngleAscending * cosineInclination * cosineArgPeriapsis);
            var qk = sineInclination * cosineArgPeriapsis;

            var perifocalP = (pi * Vector3.UnitX) + (pj * Vector3.UnitY) + (pk * Vector3.UnitZ);
            var perifocalQ = (qi * Vector3.UnitX) + (qj * Vector3.UnitY) + (qk * Vector3.UnitZ);

            var cosineTrueAnomaly = Math.Cos(trueAnomaly);
            var sineTrueAnomaly = Math.Sin(trueAnomaly);

            var r = (_radius.Value * cosineTrueAnomaly * perifocalP) + (_radius.Value * sineTrueAnomaly * perifocalQ);

            if (OrbitedObject.Parent != OrbitingObject.Parent)
            {
                return r + OrbitingObject.Location.Parent.GetLocalizedPosition(OrbitedObject.Location);
            }
            else
            {
                return r + OrbitedObject.Position;
            }
        }

        private void ClearParameters()
        {
            _alpha = null;
            _apoapsis = null;
            _eccentricity = null;
            _inclination = null;
            _periapsis = null;
            _period = null;
            _r0 = null;
            _radius = null;
            _semiMajorAxis = null;
            _standardGravitationalParameter = null;
            _trueAnomaly = null;
            _v0 = null;
        }

        private double GetUniversalVariableFormulaRatio(double x, double t, double sqrtSGP, double accel, double f)
        {
            var x2 = x * x;
            var z = Alpha * x2;
            var ssz = StumpffS(z);
            var scz = StumpffC(z);
            var x2scz = x2 * scz;

            var n = (accel / sqrtSGP * x2scz) + (f * Math.Pow(x, 3) * ssz) + (Radius * x) - (sqrtSGP * t);
            var d = (accel / sqrtSGP * x * (1.0 - (Alpha * x2 * ssz))) + (f * x2scz) + Radius;
            return n / d;
        }

        private void SetParametersFromPositionAndVelocity()
        {
            _standardGravitationalParameter = ScienceConstants.G * (OrbitingObject.Mass + OrbitedObject.Mass);

            if (OrbitedObject.Parent != OrbitingObject.Parent)
            {
                R0 = OrbitingObject.Location.GetLocalizedPosition(OrbitedObject.Location, OrbitedObject.Position);
            }
            else
            {
                R0 = OrbitingObject.Position - OrbitedObject.Position;
            }
            _radius = R0.Length();

            V0 = OrbitingObject.Velocity;

            _semiMajorAxis = -(_standardGravitationalParameter.Value / 2.0) * Math.Pow((Math.Pow(V0.Length(), 2) / 2.0) - (_standardGravitationalParameter.Value / _radius.Value), -1);

            _alpha = _standardGravitationalParameter / _semiMajorAxis;

            _period = MathConstants.TwoPI * Math.Sqrt(Math.Pow(SemiMajorAxis, 3) / _standardGravitationalParameter.Value);

            var h = Vector3.Cross(R0, V0);
            _inclination = Math.Acos(h.Z / h.Length());
            _angleAscending = Math.Atan2(h.X, -h.Y);

            var e = (Vector3.Cross(V0, h) / _standardGravitationalParameter.Value) - Vector3.Normalize(R0);
            _eccentricity = e.Length();

            var p = _semiMajorAxis.Value * (1 - (_eccentricity.Value * _eccentricity.Value));
            _trueAnomaly = Math.Atan2(Math.Sqrt(p / _standardGravitationalParameter.Value) * Vector3.Dot(R0, R0), p - _radius.Value);

            _argumentPeriapsis = Math.Atan2((_r0?.Z ?? 0) / Math.Sin(_inclination.Value), ((_r0?.X ?? 0) * Math.Cos(_angleAscending.Value)) + ((_r0?.Y ?? 0) * Math.Sin(_angleAscending.Value))) - _trueAnomaly.Value;

            _apoapsis = _eccentricity >= 1
                ? double.PositiveInfinity
                : (1 + _eccentricity.Value) * _semiMajorAxis.Value;

            _periapsis = _eccentricity == 1
                ? _semiMajorAxis
                : (1 - _eccentricity.Value) * _semiMajorAxis.Value;
        }

        private double StumpffC(double x)
        {
            if (x == 0)
            {
                return 1.0 / 2.0;
            }
            else if (x > 0)
            {
                return (1.0 - Math.Cos(Math.Sqrt(x))) / x;
            }
            else
            {
                return (Math.Cosh(Math.Sqrt(-x)) - 1.0) / -x;
            }
        }

        private double StumpffS(double x)
        {
            if (x == 0)
            {
                return 1.0 / 6.0;
            }
            else if (x > 0)
            {
                var rootX = Math.Sqrt(x);
                return (rootX - Math.Sin(rootX)) / Math.Pow(rootX, 3);
            }
            else
            {
                var rootNegX = Math.Sqrt(-x);
                return (Math.Sinh(rootNegX) - rootNegX) / Math.Pow(rootNegX, 3);
            }
        }
    }
}
