using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Numerics;
using WorldFoundry.Extensions;
using WorldFoundry.Utilities;

namespace WorldFoundry.Orbits
{
    /// <summary>
    /// Defines an orbit by the Kepler elements.
    /// </summary>
    public class Orbit
    {
        private const float tolerance = 1.0e-8F;

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
                    SetGravitationalParameters();
                }
                return _alpha ?? 0;
            }
        }

        private double? _apoapsis;
        /// <summary>
        /// The apoapsis of this orbit. For orbits with <see cref="Eccentricity"/> >= 1, gives <see cref="float.PositiveInfinity"/>.
        /// </summary>
        public double Apoapsis
        {
            get
            {
                if (!_apoapsis.HasValue)
                {
                    SetGravitationalParameters();
                }
                return _apoapsis ?? 0;
            }
        }

        private float? _eccentricity;
        /// <summary>
        /// The eccentricity of this orbit.
        /// </summary>
        public float Eccentricity
        {
            get
            {
                if (!_eccentricity.HasValue)
                {
                    SetGravitationalParameters();
                }
                return _eccentricity ?? 0;
            }
        }

        private float? _inclination;
        /// <summary>
        /// The angle between the X-Z plane through the center of the object orbited, and the plane
        /// of the orbit (in radians).
        /// </summary>
        public float Inclination
        {
            get
            {
                if (!_inclination.HasValue)
                {
                    SetGravitationalParameters();
                }
                return _inclination ?? 0;
            }
        }

        private Orbiter _orbitedObject;
        /// <summary>
        /// The object which is being orbited.
        /// </summary>
        public Orbiter OrbitedObject
        {
            get => _orbitedObject;
            private set
            {
                _orbitedObject = value;
                ClearParameters();
            }
        }

        private Orbiter _orbitingObject;
        /// <summary>
        /// The object which is orbiting.
        /// </summary>
        public Orbiter OrbitingObject
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
                    SetGravitationalParameters();
                }
                return _periapsis ?? 0;
            }
        }

        /// <summary>
        /// The initial position of the orbiting object relative to the orbited one.
        /// </summary>
        [NotMapped]
        public Vector3 R0
        {
            get => new Vector3(R0X, R0Y, R0Z);
            private set
            {
                R0X = value.X;
                R0Y = value.Y;
                R0Z = value.Z;
            }
        }

        private float? _r0X;
        /// <summary>
        /// Specifies the X component of the orbiting entity's initial position relative to the orbited one.
        /// </summary>
        public float R0X
        {
            get
            {
                if (!_r0X.HasValue)
                {
                    SetGravitationalParameters();
                }
                return _r0X ?? 0;
            }
            private set => _r0X = value;
        }

        private float? _r0Y;
        /// <summary>
        /// Specifies the Y component of the orbiting entity's initial position relative to the orbited one.
        /// </summary>
        public float R0Y
        {
            get
            {
                if (!_r0Y.HasValue)
                {
                    SetGravitationalParameters();
                }
                return _r0Y ?? 0;
            }
            private set => _r0Y = value;
        }

        private float? _r0Z;
        /// <summary>
        /// Specifies the Z component of the orbiting entity's initial position relative to the orbited one.
        /// </summary>
        public float R0Z
        {
            get
            {
                if (!_r0Z.HasValue)
                {
                    SetGravitationalParameters();
                }
                return _r0Z ?? 0;
            }
            private set => _r0Z = value;
        }

        private float? _radius;
        /// <summary>
        /// The radius of the orbit.
        /// </summary>
        public float Radius
        {
            get
            {
                if (!_radius.HasValue)
                {
                    SetGravitationalParameters();
                }
                return _radius ?? 0;
            }
        }

        private double? _semiMajorAxis;
        public double SemiMajorAxis
        {
            get
            {
                if (!_semiMajorAxis.HasValue)
                {
                    SetGravitationalParameters();
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
                    SetGravitationalParameters();
                }
                return _standardGravitationalParameter ?? 0;
            }
        }

        private float? _trueAnomaly;
        /// <summary>
        /// The current true anomaly of this orbit.
        /// </summary>
        public float TrueAnomaly
        {
            get
            {
                if (!_trueAnomaly.HasValue)
                {
                    SetGravitationalParameters();
                }
                return _trueAnomaly ?? 0;
            }
        }

        /// <summary>
        /// The initial velocity of the orbiting object relative to the orbited one.
        /// </summary>
        [NotMapped]
        public Vector3 V0
        {
            get => new Vector3(V0X, V0Y, V0Z);
            private set
            {
                V0X = value.X;
                V0Y = value.Y;
                V0Z = value.Z;
            }
        }

        private float? _v0X;
        /// <summary>
        /// Specifies the X component of the orbiting entity's initial velocity relative to the orbited one.
        /// </summary>
        public float V0X
        {
            get
            {
                if (!_v0X.HasValue)
                {
                    SetGravitationalParameters();
                }
                return _v0X ?? 0;
            }
            private set => _v0X = value;
        }

        private float? _v0Y;
        /// <summary>
        /// Specifies the Y component of the orbiting entity's initial velocity relative to the orbited one.
        /// </summary>
        public float V0Y
        {
            get
            {
                if (!_v0Y.HasValue)
                {
                    SetGravitationalParameters();
                }
                return _v0Y ?? 0;
            }
            private set => _v0Y = value;
        }

        private float? _v0Z;
        /// <summary>
        /// Specifies the Z component of the orbiting entity's initial velocity relative to the orbited one.
        /// </summary>
        public float V0Z
        {
            get
            {
                if (!_v0Z.HasValue)
                {
                    SetGravitationalParameters();
                }
                return _v0Z ?? 0;
            }
            private set => _v0Z = value;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="Orbit"/>.
        /// </summary>
        public Orbit() { }

        /// <summary>
        /// Initializes a new instance of <see cref="Orbit"/> with the given parameters.
        /// </summary>
        public Orbit(Orbiter orbitingObject, Orbiter orbitedObject)
        {
            OrbitingObject = orbitingObject ?? throw new ArgumentNullException(nameof(orbitingObject), $"{nameof(orbitingObject)} cannot be null");
            OrbitedObject = orbitedObject ?? throw new ArgumentNullException(nameof(orbitedObject), $"{nameof(orbitedObject)} cannot be null");
        }

        internal static Orbit GetCircularOrbit(Orbiter orbitingObject, Orbiter orbitedObject)
        {
            var orbit = new Orbit(orbitingObject, orbitedObject)
            {
                _eccentricity = 0,
                _standardGravitationalParameter = Utilities.Science.Constants.G * (orbitedObject.Mass + orbitingObject.Mass),
            };

            orbit.R0 = (orbitingObject.Position - orbitedObject.Position) * orbitingObject.Parent.LocalScale;
            orbit._radius = orbit.R0.Length();

            // Calculate magnitudes manually to avoid low-precision
            // implementation resulting in infinity.
            var r0x2 = Math.Pow(orbit._r0X.Value, 2);
            var r0z2 = Math.Pow(orbit._r0Z.Value, 2);
            orbit._semiMajorAxis = Math.Sqrt(r0x2 + Math.Pow(orbit._r0Y.Value, 2) + r0z2);
            orbit._apoapsis = orbit._semiMajorAxis;
            orbit._periapsis = orbit._semiMajorAxis;

            Vector3 xz = new Vector3(orbit._r0X.Value, 0, orbit._r0Z.Value);
            orbit._inclination = (float)Math.Acos(Math.Sqrt(r0x2 + r0z2) / orbit._semiMajorAxis.Value);
            var angleAscending = Vector3.UnitX.GetAngle(xz) - Utilities.MathUtil.Constants.HalfPI;

            Vector3 n = new Vector3((float)Math.Cos(angleAscending), (float)Math.Sin(angleAscending), 0);
            var argPeriapsis = Math.Acos((Vector3.Dot(n, orbit.R0)) / (n.Length() * orbit.Radius));
            if (orbit._r0Z.Value < 0)
            {
                argPeriapsis = Utilities.MathUtil.Constants.TwoPI - argPeriapsis;
            }

            // Calculate the perifocal vectors
            var cosineAngleAscending = Math.Cos(angleAscending);
            var sineAngleAscending = Math.Sin(angleAscending);
            var cosineArgPeriapsis = Math.Cos(argPeriapsis);
            var sineArgPeriapsis = Math.Sin(argPeriapsis);
            var cosineInclination = Math.Cos(orbit._inclination.Value);
            var sineInclination = Math.Sin(orbit._inclination.Value);

            float qi = (float)(-(cosineAngleAscending * sineArgPeriapsis) - (sineAngleAscending * cosineInclination * cosineArgPeriapsis));
            float qj = (float)(-(sineAngleAscending * sineArgPeriapsis) + (cosineAngleAscending * cosineInclination * cosineArgPeriapsis));
            float qk = (float)(sineInclination * cosineArgPeriapsis);

            var perifocalQ = (qi * Vector3.UnitX) + (qj * Vector3.UnitY) + (qk * Vector3.UnitZ);

            orbit._trueAnomaly = 0;

            orbit._alpha = orbit._standardGravitationalParameter.Value / orbit._semiMajorAxis.Value;
            orbit.V0 = (float)Math.Sqrt(orbit._alpha.Value) * perifocalQ;

            return orbit;
        }

        internal static Vector3 GetDeltaVForCircularOrbit(Orbiter orbitingObject, Orbiter orbitedObject)
        {
            var orbit = new Orbit(orbitingObject, orbitedObject);

            var h = Vector3.Cross(orbit.R0, orbit.V0);
            var inclination = Math.Acos(h.Z / h.Length());
            var n = Vector3.Cross(Vector3.UnitZ, h);
            var angleAscending = Math.Acos(n.X / n.Length());

            // Calculate the perifocal vector
            var cosineAngleAscending = Math.Cos(angleAscending);
            var sineAngleAscending = Math.Sin(angleAscending);
            var cosineArgPeriapsis = 1.0;
            var sineArgPeriapsis = 0.0;
            var cosineInclination = Math.Cos(inclination);
            var sineInclination = Math.Sin(inclination);

            float qi = (float)(-(cosineAngleAscending * sineArgPeriapsis) - (sineAngleAscending * cosineInclination * cosineArgPeriapsis));
            float qj = (float)(-(sineAngleAscending * sineArgPeriapsis) + (cosineAngleAscending * cosineInclination * cosineArgPeriapsis));
            float qk = (float)(sineInclination * cosineArgPeriapsis);

            var perifocalQ = (qi * Vector3.UnitX) + (qj * Vector3.UnitY) + (qk * Vector3.UnitZ);

            return (float)Math.Sqrt(orbit.StandardGravitationalParameter / orbit.Radius) * perifocalQ;
        }

        /// <summary>
        /// Sets the orbit of the given <see cref="Orbiter"/> according to the given orbital
        /// parameters, and adjusts its position and velocity as necessary.
        /// </summary>
        /// <param name="orbited">The celestial object to be orbited.</param>
        /// <param name="orbiter">The celestial object which will be in orbit.</param>
        /// <param name="periapsis">
        /// The distance between the objects at the closest point in the orbit.
        /// </param>
        /// <param name="eccentricity">The degree to which the orbit is non-circular.</param>
        /// <param name="inclination">
        /// The angle between the X-Z plane through the center of the object orbited, and the plane
        /// of the orbit (in radians).
        /// </param>
        /// <param name="angleAscending">
        /// The angle between the X-axis and the plane of the orbit (at the intersection where the
        /// orbit is rising, in radians).
        /// </param>
        /// <param name="argPeriapsis">
        /// The angle between the intersection of the X-Z plane through the center of the object
        /// orbited and the orbital plane, and the periapsis (in radians).
        /// </param>
        /// <param name="trueAnomaly">
        /// The angle between periapsis and the current position of this object, from the center of
        /// the object orbited (in radians).
        /// </param>
        public static void SetOrbit(
            Orbiter orbitingObject,
            Orbiter orbitedObject,
            double periapsis,
            float eccentricity,
            float inclination,
            float angleAscending,
            float argPeriapsis,
            float trueAnomaly)
        {
            if (eccentricity < 0)
            {
                throw new ArgumentOutOfRangeException("eccentricity must be >= 0");
            }
            if (inclination < 0 || inclination >= Math.PI)
            {
                throw new ArgumentOutOfRangeException("inclination must be >= 0 and < π");
            }
            if (angleAscending < 0 || angleAscending >= Utilities.MathUtil.Constants.TwoPI)
            {
                throw new ArgumentOutOfRangeException("angleAscending must be >= 0 and < 2π");
            }
            if (argPeriapsis < 0 || argPeriapsis >= Utilities.MathUtil.Constants.TwoPI)
            {
                throw new ArgumentOutOfRangeException("argPeriapsis must be >= 0 and < 2π");
            }
            if (trueAnomaly < 0 || trueAnomaly >= Utilities.MathUtil.Constants.TwoPI)
            {
                throw new ArgumentOutOfRangeException("trueAnomaly must be >= 0 and < 2π");
            }

            var orbit = new Orbit(orbitingObject, orbitedObject)
            {
                _eccentricity = eccentricity,
                _inclination = inclination,
                _periapsis = periapsis,
                _standardGravitationalParameter = Utilities.Science.Constants.G * (orbitedObject.Mass + orbitingObject.Mass),
                _trueAnomaly = trueAnomaly,
            };

            var semiLatusRectum = periapsis * (1 + eccentricity);

            // For parabolic orbits, semi-major axis is undefined, and is set to the periapsis instead.
            if (eccentricity == 1)
            {
                orbit._semiMajorAxis = periapsis;
            }
            else
            {
                orbit._semiMajorAxis = semiLatusRectum / (1 - Math.Pow(eccentricity, 2));
            }

            orbit._alpha = orbit._standardGravitationalParameter / orbit._semiMajorAxis;

            // Calculate the perifocal vectors
            var cosineAngleAscending = Math.Cos(angleAscending);
            var sineAngleAscending = Math.Sin(angleAscending);
            var cosineArgPeriapsis = Math.Cos(argPeriapsis);
            var sineArgPeriapsis = Math.Sin(argPeriapsis);
            var cosineInclination = Math.Cos(inclination);
            var sineInclination = Math.Sin(inclination);

            float pi = (float)((cosineAngleAscending * cosineArgPeriapsis) - (sineAngleAscending * cosineInclination * sineArgPeriapsis));
            float pj = (float)((sineAngleAscending * cosineArgPeriapsis) + (cosineAngleAscending * cosineInclination * sineArgPeriapsis));
            float pk = (float)(sineInclination * sineArgPeriapsis);

            float qi = (float)(-(cosineAngleAscending * sineArgPeriapsis) - (sineAngleAscending * cosineInclination * cosineArgPeriapsis));
            float qj = (float)(-(sineAngleAscending * sineArgPeriapsis) + (cosineAngleAscending * cosineInclination * cosineArgPeriapsis));
            float qk = (float)(sineInclination * cosineArgPeriapsis);

            Vector3 perifocalP = (pi * Vector3.UnitX) + (pj * Vector3.UnitY) + (pk * Vector3.UnitZ);
            Vector3 perifocalQ = (qi * Vector3.UnitX) + (qj * Vector3.UnitY) + (qk * Vector3.UnitZ);

            float cosineTrueAnomaly = (float)Math.Cos(trueAnomaly);
            float sineTrueAnomaly = (float)Math.Sin(trueAnomaly);
            double radius = semiLatusRectum / (1 + (eccentricity * cosineTrueAnomaly));

            orbit.R0 = ((float)(radius * cosineTrueAnomaly) * perifocalP) + ((float)(radius * sineTrueAnomaly) * perifocalQ);
            orbit.V0 = (float)Math.Sqrt(orbit._standardGravitationalParameter.Value / semiLatusRectum) *
                ((-sineTrueAnomaly * perifocalP) + (eccentricity * perifocalQ) + (cosineTrueAnomaly * perifocalQ));

            orbitingObject.Position = orbitedObject.Position + (orbit.R0 / orbitedObject.Parent.LocalScale);
            orbitingObject.Velocity = orbit.V0 / orbitedObject.Parent.LocalScale;
        }

        private void ClearParameters()
        {
            _alpha = null;
            _apoapsis = null;
            _eccentricity = null;
            _inclination = null;
            _periapsis = null;
            _r0X = null;
            _r0Y = null;
            _r0Z = null;
            _radius = null;
            _semiMajorAxis = null;
            _standardGravitationalParameter = null;
            _trueAnomaly = null;
            _v0X = null;
            _v0Y = null;
            _v0Z = null;
        }

        /// <summary>
        /// Calculates the radius of the orbiting body's Hill sphere (in meters).
        /// </summary>
        /// <returns>The radius of the orbiting body's Hill sphere, in meters.</returns>
        public double GetHillSphereRadius()
            => SemiMajorAxis * (1 - Eccentricity) * Math.Pow(OrbitingObject.Mass / (3 * OrbitedObject.Mass), 1.0 / 3.0);

        /// <summary>
        /// Approximates the radius of the orbiting body's mutual Hill sphere with another
        /// orbiting body in orbit around the same primary (in meters).
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
        public double GetMutualHillSphereRadius(double otherMass)
            => Math.Pow((OrbitingObject.Mass + otherMass) / (3 * OrbitedObject.Mass), (1.0 / 3.0)) * SemiMajorAxis;

        /// <summary>
        /// Calculates the orbital period.
        /// </summary>
        /// <returns>The orbital period, in seconds.</returns>
        public float GetPeriod()
            => (float)(Utilities.MathUtil.Constants.TwoPI * Math.Sqrt(Math.Pow(SemiMajorAxis, 3) / StandardGravitationalParameter));

        /// <summary>
        /// Calculates the radius of the orbiting body's sphere of influence (in meters).
        /// </summary>
        /// <returns>The radius of the orbiting body's sphere of influence, in meters.</returns>
        public double GetSphereOfInfluenceRadius()
            => SemiMajorAxis * Math.Pow(OrbitingObject.Mass / OrbitedObject.Mass, 2.0 / 5.0);

        private double GetUniversalVariableFormulaRatioFor(double x, double t, double sqrtSGP, float accel, double f)
        {
            var x2 = x * x;
            double z = Alpha * x2;
            var ssz = StumpffS(z);
            var scz = StumpffC(z);
            var x2scz = x2 * scz;

            var n = ((accel / sqrtSGP) * x2scz) + (f * Math.Pow(x, 3) * ssz) + (Radius * x) - (sqrtSGP * t);
            var d = ((accel / sqrtSGP) * x * (1.0 - (Alpha * x2 * ssz))) + (f * x2scz) + Radius;
            return n / d;
        }

        /// <summary>
        /// Gets updated orbital position and velocity vectors.
        /// </summary>
        /// <param name="t">The number of seconds which have elapsed since the conditions when the orbit was defined were true.</param>
        /// <returns>An array with 2 elements: the position vector (relative to the orbited object), and the velocity vector.</returns>
        public (Vector3 position, Vector3 velocity) GetStateVectorsAtTime(double t)
        {
            // Universal variable formulas; Newton's method

            var sqrtSGP = Math.Sqrt(StandardGravitationalParameter);
            var accel = Radius / V0.Length();
            var f = 1.0 - (Alpha * Radius);

            // Initial guess for x
            double x = sqrtSGP * Math.Abs(Alpha) * t;

            // Find acceptable x
            double ratio = GetUniversalVariableFormulaRatioFor(x, t, sqrtSGP, accel, f);
            while (Math.Abs(ratio) > tolerance)
            {
                x -= ratio;
                ratio = GetUniversalVariableFormulaRatioFor(x, t, sqrtSGP, accel, f);
            }

            var x2 = x * x;
            var x3 = Math.Pow(x, 3);
            var ax2 = Alpha * x2;
            var ssax2 = StumpffS(ax2);
            var scax2 = StumpffC(ax2);
            var ssax2x3 = ssax2 * x3;

            var uvf = (float)(1.0 - ((x2 / Radius) * scax2));
            var uvg = (float)(t - ((1.0 / sqrtSGP) * ssax2x3));

            Vector3 r = (R0 * uvf) + (V0 * uvg);
            var rLength = r.Length();

            var uvfp = (float)((sqrtSGP / (rLength * Radius)) * ((Alpha * ssax2x3) - x));
            var uvfgp = (float)(1.0 - ((x2 / rLength) * scax2));

            Vector3 v = (R0 * uvfp) + (V0 * uvfgp);

            return (r, v);
        }

        private void SetGravitationalParameters()
        {
            _standardGravitationalParameter = Utilities.Science.Constants.G * (OrbitingObject.Mass + OrbitedObject.Mass);

            R0 = (OrbitingObject.Position - OrbitedObject.Position) * OrbitingObject.Parent.LocalScale;
            _radius = R0.Length();

            V0 = OrbitingObject.Velocity * OrbitingObject.Parent.LocalScale;

            _semiMajorAxis = -(_standardGravitationalParameter.Value / 2.0) * Math.Pow((Math.Pow(V0.Length(), 2) / 2.0) - (_standardGravitationalParameter.Value / _radius.Value), -1);

            _alpha = _standardGravitationalParameter / _semiMajorAxis;

            var h = Vector3.Cross(R0, V0);
            _inclination = (float)Math.Acos(h.Z / h.Length());

            var ev = Vector3.Cross(V0, h) / (float)StandardGravitationalParameter - Vector3.Normalize(R0);
            _eccentricity = ev.Length();

            float ta = (float)Math.Acos(Vector3.Dot(ev, R0) / (_eccentricity.Value * _radius.Value));
            if (Vector3.Dot(R0, V0) < 0)
            {
                _trueAnomaly = (float)(Utilities.MathUtil.Constants.TwoPI - ta);
            }
            else
            {
                _trueAnomaly = ta;
            }

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
                double rootX = Math.Sqrt(x);
                return (rootX - Math.Sin(rootX)) / Math.Pow(rootX, 3);
            }
            else
            {
                double rootNegX = Math.Sqrt(-x);
                return (Math.Sinh(rootNegX) - rootNegX) / Math.Pow(rootNegX, 3);
            }
        }

        /// <summary>
        /// Updates an orbit with the current values of its <see cref="Orbiter"/> objects.
        /// </summary>
        internal void UpdateOrbit()
        {
            ClearParameters();
            SetGravitationalParameters();
        }
    }
}
