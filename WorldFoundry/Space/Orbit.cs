using NeverFoundry.MathAndScience.Constants.Numbers;
using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.MathAndScience.Randomization;
using NeverFoundry.MathAndScience.Time;
using System;
using System.Runtime.Serialization;
using System.Security.Permissions;
using WorldFoundry.Place;

namespace WorldFoundry.Space
{
    /// <summary>
    /// Defines an orbit by the Kepler elements.
    /// </summary>
    [Serializable]
    public struct Orbit : ISerializable
    {
        private const double Tolerance = 1.0e-8;

        /// <summary>
        /// Derived value equal to the standard gravitational parameter divided by the semi-major axis.
        /// </summary>
        private readonly Number _alpha;

        /// <summary>
        /// The apoapsis of this orbit, in meters. For orbits with <see cref="Eccentricity"/> >= 1,
        /// gives <see cref="double.PositiveInfinity"/>.
        /// </summary>
        public Number Apoapsis { get; }

        /// <summary>
        /// The eccentricity of this orbit.
        /// </summary>
        public double Eccentricity { get; }

        /// <summary>
        /// The time at which the state of this orbit is defined, which coincides with a time of
        /// pericenter passage.
        /// </summary>
        public Duration Epoch { get; }

        /// <summary>
        /// The angle between the X-Z plane through the center of the object orbited, and the plane
        /// of the orbit, in radians.
        /// </summary>
        public double Inclination { get; }

        /// <summary>
        /// The mean longitude of this orbit at epoch, in radians.
        /// </summary>
        public double MeanLongitude { get; }

        /// <summary>
        /// The mean motion of this orbit, in radians per second.
        /// </summary>
        public Number MeanMotion { get; }

        /// <summary>
        /// The entity which is being orbited.
        /// </summary>
        public CelestialLocation OrbitedObject { get; }

        /// <summary>
        /// The periapsis of this orbit, in meters.
        /// </summary>
        public Number Periapsis { get; }

        /// <summary>
        /// The period of this orbit, in seconds.
        /// </summary>
        public Number Period { get; }

        /// <summary>
        /// The initial position of the orbiting object relative to the orbited one.
        /// </summary>
        public Vector3 R0 { get; }

        /// <summary>
        /// The radius of the orbit at the current position, in meters.
        /// </summary>
        public Number Radius { get; }

        /// <summary>
        /// The semi-major axis of this <see cref="Orbit"/>, in meters.
        /// </summary>
        public Number SemiMajorAxis { get; }

        /// <summary>
        /// A derived value, equal to G * the sum of masses of the orbiting objects.
        /// </summary>
        public Number StandardGravitationalParameter { get; }

        /// <summary>
        /// The current true anomaly of this orbit.
        /// </summary>
        public double TrueAnomaly { get; }

        /// <summary>
        /// The initial velocity of the orbiting object relative to the orbited one.
        /// </summary>
        public Vector3 V0 { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="Orbit"/>.
        /// </summary>
        /// <param name="orbitedObject">The <see cref="CelestialLocation"/> which will be
        /// orbited.</param>
        /// <param name="alpha">Derived value equal to the standard gravitational parameter divided
        /// by the semi-major axis.</param>
        /// <param name="eccentricity">The eccentricity of this orbit.</param>
        /// <param name="inclination">The angle between the X-Z plane through the center of the
        /// object orbited, and the plane of the orbit, in radians.</param>
        /// <param name="meanLongitude">The mean longitude of this orbit at epoch.</param>
        /// <param name="meanMotion">The mean motion of this orbit, in radians per second.</param>
        /// <param name="periapsis">The periapsis of this orbit.</param>
        /// <param name="r0">The initial position of the orbiting object relative to the orbited
        /// one.</param>
        /// <param name="radius">The radius of the orbit at the current position.</param>
        /// <param name="semiMajorAxis">The semi-major axis of the orbit.</param>
        /// <param name="standardGravitationalParameter">A derived value, equal to G * the sum of
        /// masses of the orbiting objects.</param>
        /// <param name="trueAnomaly">The current true anomaly of this orbit.</param>
        /// <param name="v0">The initial velocity of the orbiting object relative to the orbited
        /// one.</param>
        /// <param name="period">The period of this orbit.</param>
        /// <param name="epoch">The time at which the state of this orbit is defined, which
        /// coincides with a time of pericenter passage.</param>
        public Orbit(
            CelestialLocation orbitedObject,
            Number alpha,
            double eccentricity,
            double inclination,
            double meanLongitude,
            Number meanMotion,
            Number periapsis,
            Vector3 r0,
            Number radius,
            Number semiMajorAxis,
            Number standardGravitationalParameter,
            double trueAnomaly,
            Vector3 v0,
            Number period,
            Duration epoch)
        {
            OrbitedObject = orbitedObject;
            _alpha = alpha;
            Eccentricity = eccentricity;
            Inclination = inclination;
            MeanLongitude = meanLongitude;
            MeanMotion = meanMotion;
            Periapsis = periapsis;
            R0 = r0;
            Radius = radius;
            SemiMajorAxis = semiMajorAxis;
            StandardGravitationalParameter = standardGravitationalParameter;
            TrueAnomaly = trueAnomaly;
            V0 = v0;
            Period = period;
            Epoch = epoch;

            Apoapsis = eccentricity >= 1
                ? Number.PositiveInfinity
                : (1 + eccentricity) * semiMajorAxis;
        }

        private Orbit(SerializationInfo info, StreamingContext context) : this(
            (CelestialLocation)info.GetValue(nameof(OrbitedObject), typeof(CelestialLocation)),
            (Number)info.GetValue(nameof(_alpha), typeof(Number)),
            (double)info.GetValue(nameof(Eccentricity), typeof(double)),
            (double)info.GetValue(nameof(Inclination), typeof(double)),
            (double)info.GetValue(nameof(MeanLongitude), typeof(double)),
            (Number)info.GetValue(nameof(MeanMotion), typeof(Number)),
            (Number)info.GetValue(nameof(Periapsis), typeof(Number)),
            (Vector3)info.GetValue(nameof(R0), typeof(Vector3)),
            (Number)info.GetValue(nameof(Radius), typeof(Number)),
            (Number)info.GetValue(nameof(SemiMajorAxis), typeof(Number)),
            (Number)info.GetValue(nameof(StandardGravitationalParameter), typeof(Number)),
            (double)info.GetValue(nameof(TrueAnomaly), typeof(double)),
            (Vector3)info.GetValue(nameof(V0), typeof(Vector3)),
            (Number)info.GetValue(nameof(Period), typeof(Number)),
            (Duration)info.GetValue(nameof(Epoch), typeof(Duration))) { }

        /// <summary>
        /// Calculates the change in velocity necessary for the given object to achieve a circular
        /// orbit around the given entity, as a vector.
        /// </summary>
        /// <param name="orbitingObject">An orbiting object.</param>
        /// <param name="orbitedObject">An orbited entity.</param>
        /// <returns>A change of velocity vector.</returns>
        public static Vector3 GetDeltaVForCircularOrbit(CelestialLocation orbitingObject, CelestialLocation orbitedObject)
        {
            var r0 = orbitedObject.CelestialParent != orbitingObject.CelestialParent
                ? orbitingObject.GetLocalizedPosition(orbitedObject, orbitedObject.Position)
                : orbitingObject.Position - orbitedObject.Position;

            var h = Vector3.Cross(r0, orbitingObject.Velocity);
            var inclination = Number.Acos(h.Z / h.Length());
            var n = Vector3.Cross(Vector3.UnitZ, h);
            var angleAscending = Number.Acos(n.X / n.Length());

            // Calculate the perifocal vector
            var cosineAngleAscending = Number.Cos(angleAscending);
            var sineAngleAscending = Number.Sin(angleAscending);
            var cosineInclination = Number.Cos(inclination);
            var sineInclination = Number.Sin(inclination);

            var qi = -(sineAngleAscending * cosineInclination);
            var qj = cosineAngleAscending * cosineInclination;
            var qk = sineInclination;

            var perifocalQ = (qi * Vector3.UnitX) + (qj * Vector3.UnitY) + (qk * Vector3.UnitZ);

            var standardGravitationalParameter = ScienceConstants.G * (orbitingObject.Mass + orbitedObject.Mass);
            return Number.Sqrt(standardGravitationalParameter / r0.Length()) * perifocalQ;
        }

        /// <summary>Populates a <see cref="SerializationInfo"></see> with the data needed to
        /// serialize the target object.</summary>
        /// <param name="info">The <see cref="SerializationInfo"></see> to populate with
        /// data.</param>
        /// <param name="context">The destination (see <see cref="StreamingContext"></see>) for this
        /// serialization.</param>
        /// <exception cref="System.Security.SecurityException">The caller does not have the
        /// required permission.</exception>
        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(OrbitedObject), OrbitedObject);
            info.AddValue(nameof(_alpha), _alpha);
            info.AddValue(nameof(Eccentricity), Eccentricity);
            info.AddValue(nameof(Inclination), Inclination);
            info.AddValue(nameof(MeanLongitude), MeanLongitude);
            info.AddValue(nameof(MeanMotion), MeanMotion);
            info.AddValue(nameof(Periapsis), Periapsis);
            info.AddValue(nameof(R0), R0);
            info.AddValue(nameof(Radius), Radius);
            info.AddValue(nameof(SemiMajorAxis), SemiMajorAxis);
            info.AddValue(nameof(StandardGravitationalParameter), StandardGravitationalParameter);
            info.AddValue(nameof(TrueAnomaly), TrueAnomaly);
            info.AddValue(nameof(V0), V0);
            info.AddValue(nameof(Period), Period);
            info.AddValue(nameof(Epoch), Epoch);
        }

        /// <summary>
        /// Sets the orbit of the given object based on its current position, and adjusts its
        /// velocity as necessary to make the orbit circular (zero eccentricity).
        /// </summary>
        /// <param name="orbitingObject">The celestial object which will be in orbit.</param>
        /// <param name="orbitedObject">The celestial entity to be orbited.</param>
        /// <remarks>
        /// The orbiting object's current position will be assumed to be on the desired orbit. An
        /// inclination will be calculated from the current position, and presumed to be the maximum
        /// inclination.
        /// </remarks>
        public static void SetCircularOrbit(CelestialLocation orbitingObject, CelestialLocation orbitedObject)
        {
            var standardGravitationalParameter = ScienceConstants.G * (orbitedObject.Mass + orbitingObject.Mass);

            var r0 = orbitingObject.Position - orbitedObject.Position;
            var radius = r0.Length();

            // Calculate magnitudes manually to avoid low-precision
            // implementation resulting in infinity.
            var r0x2 = r0.X.Square();
            var r0z2 = r0.Z.Square();
            var semiMajorAxis = Number.Sqrt(r0x2 + r0.Y.Square() + r0z2);
            var periapsis = semiMajorAxis;

            var xz = new Vector3(r0.X, 0, r0.Z);
            var inclination = Number.Acos(Number.Sqrt(r0x2 + r0z2) / semiMajorAxis);
            var angleAscending = Vector3.UnitX.Angle(xz) - MathConstants.HalfPI;

            var cosineAngleAscending = Number.Cos(angleAscending);
            var sineAngleAscending = Number.Sin(angleAscending);

            var n = new Vector3(cosineAngleAscending, sineAngleAscending, 0);
            var argPeriapsis = Number.Acos(Vector3.Dot(n, r0) / (n.Length() * radius));
            if (r0.Z < 0)
            {
                argPeriapsis = MathConstants.TwoPI - argPeriapsis;
            }

            // Calculate the perifocal vectors
            var cosineArgPeriapsis = Number.Cos(argPeriapsis);
            var sineArgPeriapsis = Number.Sin(argPeriapsis);
            var cosineInclination = Number.Cos(inclination);
            var sineInclination = Number.Sin(inclination);

            var qi = -(cosineAngleAscending * sineArgPeriapsis) - (sineAngleAscending * cosineInclination * cosineArgPeriapsis);
            var qj = -(sineAngleAscending * sineArgPeriapsis) + (cosineAngleAscending * cosineInclination * cosineArgPeriapsis);
            var qk = sineInclination * cosineArgPeriapsis;

            var perifocalQ = (qi * Vector3.UnitX) + (qj * Vector3.UnitY) + (qk * Vector3.UnitZ);

            var alpha = standardGravitationalParameter / semiMajorAxis;
            orbitingObject.Velocity = Number.Sqrt(alpha) * perifocalQ;

            var meanLongitude = (double)(angleAscending + argPeriapsis);

            var period = MathConstants.TwoPI * Number.Sqrt(semiMajorAxis.Cube() / standardGravitationalParameter);

            orbitingObject.Orbit = new Orbit(
                orbitedObject,
                alpha,
                0,
                (double)inclination,
                meanLongitude,
                MathConstants.TwoPI / period,
                periapsis,
                r0,
                radius,
                semiMajorAxis,
                standardGravitationalParameter,
                0,
                orbitingObject.Velocity,
                period,
                orbitingObject.ContainingUniverse?.Time.Now ?? Duration.Zero);
        }

        /// <summary>
        /// Sets the orbit of the given object based on its current position and the given <paramref
        /// name="eccentricity"/>, and adjusts its velocity as necessary.
        /// </summary>
        /// <param name="orbitingObject">The celestial object which will be in orbit.</param>
        /// <param name="orbitedObject">The celestial entity to be orbited.</param>
        /// <param name="eccentricity">The degree to which the orbit is non-circular. The absolute
        /// value will be used (i.e. negative values are treated as positives).</param>
        /// <remarks>
        /// The orbiting object's current position will be assumed to be on the desired orbit. An
        /// inclination will be calculated from the current position, and presumed to be the maximum
        /// inclination.
        /// </remarks>
        public static void SetOrbit(
            CelestialLocation orbitingObject,
            CelestialLocation orbitedObject,
            double eccentricity)
        {
            if (orbitingObject is null)
            {
                throw new ArgumentNullException(nameof(orbitingObject), $"{nameof(orbitingObject)} cannot be null");
            }
            if (orbitedObject is null)
            {
                throw new ArgumentNullException(nameof(orbitedObject), $"{nameof(orbitedObject)} cannot be null");
            }
            eccentricity = Math.Abs(eccentricity);

            var standardGravitationalParameter = ScienceConstants.G * (orbitedObject.Mass + orbitingObject.Mass);

            var r0 = orbitedObject.CelestialParent == orbitingObject.CelestialParent
                ? orbitingObject.Position - orbitingObject.Position
                : orbitingObject.GetLocalizedPosition(orbitedObject, orbitedObject.Position);

            var radius = r0.Length();

            var xz = new Vector3(r0.X, 0, r0.Z);
            var inclination = Number.Acos(Number.Sqrt(xz.X.Square() + xz.Z.Square()) / radius);
            var angleAscending = Vector3.UnitX.Angle(xz) - MathConstants.HalfPI;

            var trueAnomaly = Randomizer.Instance.NextDouble(NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.TwoPI);

            var semiLatusRectum = radius * (1 + (eccentricity * Math.Cos(trueAnomaly)));
            var semiMajorAxis = semiLatusRectum / (1 - (eccentricity * eccentricity));
            var alpha = standardGravitationalParameter / semiMajorAxis;

            // The current position must be either the apoapsis or the periapsis,
            // since it was chosen as the reference point for the inclination.
            // Therefore, it is the periapsis if its distance from the orbited
            // body is less than the semi-major axis, and the apoapsis if not.
            var periapsis = radius <= semiMajorAxis
                ? r0.Length()
                : (Vector3.Normalize(new Vector3(-r0.X, -r0.Y, -r0.Z)) * (semiLatusRectum / (1 + eccentricity))).Length();
            // For parabolic orbits, semi-major axis is undefined, and is set to the periapsis instead.
            if (eccentricity == 1)
            {
                semiMajorAxis = periapsis;
            }
            var period = MathConstants.TwoPI * Number.Sqrt(semiMajorAxis.Cube() / standardGravitationalParameter);
            // If at periapsis now, this is the epoch;
            // if not, the epoch is half the period away.
            var epoch = orbitingObject.ContainingUniverse?.Time.Now ?? Duration.Zero;
            if (radius > semiMajorAxis)
            {
                var epochOffset = Duration.FromSeconds(period / 2);
                // Preferably set the epoch in the past; but if the current age of the universe is
                // less than half the period, set it in the future.
                epoch = epoch >= epochOffset
                    ? epoch - epochOffset
                    : epoch + epochOffset;
            }

            // Calculate the perifocal vectors
            var cosineAngleAscending = Number.Cos(angleAscending);
            var sineAngleAscending = Number.Sin(angleAscending);
            var sineInclination = Number.Sin(inclination);
            var cosineInclination = Number.Cos(inclination);

            var argumentPeriapsis = Number.Atan2(r0.Z / sineInclination, (r0.X * cosineAngleAscending) + (r0.Y * sineAngleAscending) - trueAnomaly);

            var cosineArgPeriapsis = Number.Cos(argumentPeriapsis);
            var sineArgPeriapsis = Number.Sin(argumentPeriapsis);

            var pi = (cosineAngleAscending * cosineArgPeriapsis) - (sineAngleAscending * cosineInclination * sineArgPeriapsis);
            var pj = (sineAngleAscending * cosineArgPeriapsis) + (cosineAngleAscending * cosineInclination * sineArgPeriapsis);
            var pk = sineInclination * sineArgPeriapsis;

            var qi = -(cosineAngleAscending * sineArgPeriapsis) - (sineAngleAscending * cosineInclination * cosineArgPeriapsis);
            var qj = -(sineAngleAscending * sineArgPeriapsis) + (cosineAngleAscending * cosineInclination * cosineArgPeriapsis);
            var qk = sineInclination * cosineArgPeriapsis;

            var perifocalP = (pi * Vector3.UnitX) + (pj * Vector3.UnitY) + (pk * Vector3.UnitZ);
            var perifocalQ = (qi * Vector3.UnitX) + (qj * Vector3.UnitY) + (qk * Vector3.UnitZ);

            var cosineTrueAnomaly = (Number)Math.Cos(trueAnomaly);
            var sineTrueAnomaly = (Number)Math.Sin(trueAnomaly);

            var meanLongitude = angleAscending + argumentPeriapsis;

            orbitingObject.Velocity = Number.Sqrt(standardGravitationalParameter / semiLatusRectum)
                * ((-sineTrueAnomaly * perifocalP) + (eccentricity * perifocalQ) + (cosineTrueAnomaly * perifocalQ));

            orbitingObject.Orbit = new Orbit(
                orbitedObject,
                alpha,
                eccentricity,
                (double)inclination,
                (double)meanLongitude,
                MathConstants.TwoPI / period,
                periapsis,
                r0,
                radius,
                semiMajorAxis,
                standardGravitationalParameter,
                trueAnomaly,
                orbitingObject.Velocity,
                period,
                epoch);
        }

        /// <summary>
        /// Sets the orbit of the given <see cref="CelestialLocation"/> according to the given
        /// orbital parameters, and adjusts its position and velocity as necessary.
        /// </summary>
        /// <param name="orbitingObject">The celestial object which will be in orbit.</param>
        /// <param name="orbitedObject">The celestial entity to be orbited.</param>
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
        public static void SetOrbit(
            CelestialLocation orbitingObject,
            CelestialLocation orbitedObject,
            Number periapsis,
            double eccentricity,
            double inclination,
            double angleAscending,
            double argPeriapsis,
            double trueAnomaly)
        {
            if (orbitingObject == null)
            {
                throw new ArgumentNullException(nameof(orbitingObject), $"{nameof(orbitingObject)} cannot be null");
            }
            if (orbitedObject == null)
            {
                throw new ArgumentNullException(nameof(orbitedObject), $"{nameof(orbitedObject)} cannot be null");
            }

            eccentricity = Math.Abs(eccentricity);

            while (inclination > Math.PI)
            {
                inclination -= Math.PI;
            }
            while (inclination < 0)
            {
                inclination += Math.PI;
            }

            while (angleAscending >= NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.TwoPI)
            {
                angleAscending -= NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.TwoPI;
            }
            while (angleAscending < 0)
            {
                angleAscending += NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.TwoPI;
            }

            while (argPeriapsis >= NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.TwoPI)
            {
                argPeriapsis -= NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.TwoPI;
            }
            while (argPeriapsis < 0)
            {
                argPeriapsis += NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.TwoPI;
            }

            while (trueAnomaly >= MathConstants.TwoPI)
            {
                trueAnomaly -= NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.TwoPI;
            }
            while (trueAnomaly < 0)
            {
                trueAnomaly += NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.TwoPI;
            }

            var standardGravitationalParameter = ScienceConstants.G * (orbitedObject.Mass + orbitingObject.Mass);

            var semiLatusRectum = periapsis * (1 + eccentricity);

            var eccentricitySquared = eccentricity * eccentricity;
            var eccentricityNumber = (Number)eccentricity;
            // For parabolic orbits, semi-major axis is undefined, and is set to the periapsis
            // instead.
            var semiMajorAxis = eccentricity == 1
                ? periapsis
                : semiLatusRectum / (1 - eccentricitySquared);

            var alpha = standardGravitationalParameter / semiMajorAxis;

            // Calculate the perifocal vectors
            var cosineAngleAscending = Math.Cos(angleAscending);
            var sineAngleAscending = Math.Sin(angleAscending);
            var cosineArgPeriapsis = Math.Cos(argPeriapsis);
            var sineArgPeriapsis = Math.Sin(argPeriapsis);
            var cosineInclination = Math.Cos(inclination);
            var sineInclination = Math.Sin(inclination);

            var pi = (Number)((cosineAngleAscending * cosineArgPeriapsis) - (sineAngleAscending * cosineInclination * sineArgPeriapsis));
            var pj = (Number)((sineAngleAscending * cosineArgPeriapsis) + (cosineAngleAscending * cosineInclination * sineArgPeriapsis));
            var pk = (Number)(sineInclination * sineArgPeriapsis);

            var qi = (Number)(-(cosineAngleAscending * sineArgPeriapsis) - (sineAngleAscending * cosineInclination * cosineArgPeriapsis));
            var qj = (Number)(-(sineAngleAscending * sineArgPeriapsis) + (cosineAngleAscending * cosineInclination * cosineArgPeriapsis));
            var qk = (Number)(sineInclination * cosineArgPeriapsis);

            var perifocalP = (pi * Vector3.UnitX) + (pj * Vector3.UnitY) + (pk * Vector3.UnitZ);
            var perifocalQ = (qi * Vector3.UnitX) + (qj * Vector3.UnitY) + (qk * Vector3.UnitZ);

            var cosineTrueAnomaly = Math.Cos(trueAnomaly);
            var cosineTrueAnomalyNumber = (Number)cosineTrueAnomaly;
            var sineTrueAnomaly = Math.Sin(trueAnomaly);
            var sineTrueAnomalyNumber = (Number)sineTrueAnomaly;
            var radius = semiLatusRectum / (1 + (eccentricity * cosineTrueAnomaly));

            var meanLongitude = angleAscending + argPeriapsis;

            var r0 = (radius * cosineTrueAnomalyNumber * perifocalP) + (radius * sineTrueAnomalyNumber * perifocalQ);
            if (orbitingObject.CelestialParent != orbitedObject.CelestialParent)
            {
                orbitingObject.Position += orbitingObject.GetLocalizedPosition(orbitedObject, r0);
            }
            else
            {
                orbitingObject.Position = orbitedObject.Position + r0;
            }

            orbitingObject.Velocity = Number.Sqrt(standardGravitationalParameter / semiLatusRectum)
                * ((-sineTrueAnomalyNumber * perifocalP) + (eccentricityNumber * perifocalQ) + (cosineTrueAnomalyNumber * perifocalQ));

            var eccentricAnomaly = Math.Atan2(eccentricity + cosineTrueAnomaly, Math.Sqrt(1 - eccentricitySquared) * sineTrueAnomaly);
            var meanAnomaly = (Number)(eccentricAnomaly - (eccentricity * Math.Sin(eccentricAnomaly)));
            var period = MathConstants.TwoPI * Number.Sqrt(semiMajorAxis.Cube() / standardGravitationalParameter);
            var meanMotion = MathConstants.TwoPI / period;
            var time = orbitingObject.ContainingUniverse?.Time.Now ?? Duration.Zero;
            var epochOffsetSeconds = meanAnomaly / meanMotion;
            var epochOffset = Duration.FromSeconds(epochOffsetSeconds);
            var epoch = time > epochOffset
                ? time - epochOffset
                : time + Duration.FromSeconds(period - epochOffsetSeconds);

            orbitingObject.Orbit = new Orbit(
                orbitedObject,
                alpha,
                eccentricity,
                inclination,
                meanLongitude,
                meanMotion,
                periapsis,
                r0,
                radius,
                semiMajorAxis,
                standardGravitationalParameter,
                trueAnomaly,
                orbitingObject.Velocity,
                period,
                epoch);
        }

        /// <summary>
        /// Gets the ecliptic longitude of the orbited body from the perspective of the orbiting
        /// body at a given true anomaly.
        /// </summary>
        /// <param name="t">The true anomaly.</param>
        /// <returns>The ecliptic longitude of the orbited body from the perspective of the orbiting
        /// body, in radians (normalized to 0-2π).</returns>
        public double GetEclipticLongitudeAtTrueAnomaly(double t)
            => (MeanLongitude + t + NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.PI) % NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.TwoPI;

        /// <summary>
        /// Gets orbital parameters at a given time.
        /// </summary>
        /// <param name="t">The number of seconds which have elapsed since the orbit's defining
        /// epoch (time of pericenter).</param>
        /// <returns>The mean longitude and mean anomaly, in radians (normalized to 0-2π).</returns>
        public (double meanLongitude, Number meanAnomaly) GetMeanLongitudeAndAnomalyAtTime(Number t)
        {
            var meanAnomaly = MeanMotion * t % MathConstants.TwoPI;
            return ((double)((MeanLongitude + meanAnomaly) % MathConstants.TwoPI), meanAnomaly);
        }

        /// <summary>
        /// Gets orbital parameters at a given time.
        /// </summary>
        /// <param name="time">The time at which to determine orbital parameters.</param>
        /// <returns>The mean longitude and mean anomaly, in radians (normalized to 0-2π).</returns>
        public (double meanLongitude, Number meanAnomaly) GetMeanLongitudeAndAnomalyAtTime(Duration time)
        {
            var t = (time >= Epoch ? time - Epoch : Epoch - time).ToSeconds() % Period;
            if (time < Epoch)
            {
                t = Period - t;
            }
            return GetMeanLongitudeAndAnomalyAtTime(t);
        }

        /// <summary>
        /// Gets orbital state vectors at a given time.
        /// </summary>
        /// <param name="t">The number of seconds which have elapsed since the orbit's defining
        /// epoch (time of pericenter).</param>
        /// <returns>The position vector (relative to the orbited object), and the velocity
        /// vector.</returns>
        public (Vector3 position, Vector3 velocity) GetStateVectorsAtTime(Number t)
        {
            // Universal variable formulas; Newton's method

            var sqrtSGP = Number.Sqrt(StandardGravitationalParameter);
            var accel = Radius / V0.Length();
            var f = 1 - (_alpha * Radius);

            // Initial guess for x
            var x = sqrtSGP * _alpha.Abs() * t;

            // Find acceptable x
            var ratio = GetUniversalVariableFormulaRatio(x, t, sqrtSGP, accel, f);
            while (ratio.Abs() > Tolerance)
            {
                x -= ratio;
                ratio = GetUniversalVariableFormulaRatio(x, t, sqrtSGP, accel, f);
            }

            var x2 = x * x;
            var x3 = x2 * x;
            var ax2 = _alpha * x2;
            var ssax2 = StumpffS(ax2);
            var scax2 = StumpffC(ax2);
            var ssax2x3 = ssax2 * x3;

            var uvf = 1 - (x2 / Radius * scax2);
            var uvg = t - (1 / sqrtSGP * ssax2x3);

            var r = (R0 * uvf) + (V0 * uvg);
            var rLength = r.Length();

            var uvfp = sqrtSGP / (rLength * Radius) * ((_alpha * ssax2x3) - x);
            var uvfgp = 1 - (x2 / rLength * scax2);

            var v = (R0 * uvfp) + (V0 * uvfgp);

            return (r, v);
        }

        /// <summary>
        /// Gets orbital state vectors at a given time.
        /// </summary>
        /// <param name="time">The time at which to determine orbital state vectors.</param>
        /// <returns>The position vector (relative to the orbited object), and the velocity
        /// vector.</returns>
        public (Vector3 position, Vector3 velocity) GetStateVectorsAtTime(Duration time)
        {
            var t = (time >= Epoch ? time - Epoch : Epoch - time).ToSeconds() % Period;
            if (time < Epoch)
            {
                t = Period - t;
            }
            return GetStateVectorsAtTime(t);
        }

        /// <summary>
        /// Gets the true anomaly of this orbit at a given time.
        /// </summary>
        /// <param name="t">The number of seconds which have elapsed since the orbit's defining
        /// epoch (time of pericenter).</param>
        /// <returns>The true anomaly, in radians.</returns>
        public double GetTrueAnomalyAtTime(Number t)
        {
            var (r, _) = GetStateVectorsAtTime(t);

            var p = SemiMajorAxis * (1 - (Eccentricity * Eccentricity));
            return (double)Number.Atan2((p / StandardGravitationalParameter).Sqrt() * Vector3.Dot(r, r), p - Radius);
        }

        /// <summary>
        /// Gets the true anomaly of this orbit at a given time.
        /// </summary>
        /// <param name="time">The time at which to determine orbital state vectors.</param>
        /// <returns>The true anomaly, in radians.</returns>
        public double GetTrueAnomalyAtTime(Duration time)
        {
            var t = (time >= Epoch ? time - Epoch : Epoch - time).ToSeconds() % Period;
            if (time < Epoch)
            {
                t = Period - t;
            }
            return GetTrueAnomalyAtTime(t);
        }

        internal static Number GetHillSphereRadius(CelestialLocation orbitingObject, CelestialLocation orbitedObject, Number semiMajorAxis, double eccentricity)
            => semiMajorAxis * (1 - eccentricity) * (orbitingObject.Mass / (3 * orbitedObject.Mass)).CubeRoot();

        internal static Number GetSemiMajorAxisForPeriod(CelestialLocation orbitingObject, CelestialLocation orbitedObject, Number period)
            => (ScienceConstants.G * (orbitingObject.Mass + orbitedObject.Mass) * period * period / (MathConstants.FourPI * MathConstants.PI)).CubeRoot();

        internal Number GetHillSphereRadius(CelestialLocation orbitingObject)
            => GetHillSphereRadius(orbitingObject, OrbitedObject, SemiMajorAxis, Eccentricity);

        /// <summary>
        /// Approximates the radius of the orbiting body's mutual Hill sphere with another
        /// orbiting body in orbit around the same primary, in meters.
        /// </summary>
        /// <remarks>
        /// Assumes the semimajor axis of both orbits is identical for the purposes of the
        /// calculation, which obviously would not be the case, but generates reasonably close
        /// estimates in the absence of actual values.
        /// </remarks>
        /// <param name="orbitingObject">The orbiting body.</param>
        /// <param name="otherMass">
        /// The mass of another celestial body presumed to be orbiting the same primary as this one.
        /// </param>
        /// <returns>The radius of the orbiting body's Hill sphere, in meters.</returns>
        internal Number GetMutualHillSphereRadius(CelestialLocation orbitingObject, Number otherMass)
            => ((orbitingObject.Mass + otherMass) / (3 * OrbitedObject.Mass)).CubeRoot() * SemiMajorAxis;

        internal Number GetSphereOfInfluenceRadius(CelestialLocation orbitingObject)
            => SemiMajorAxis * Number.Pow(orbitingObject.Mass / OrbitedObject.Mass, new Number(2) / new Number(5));

        private Number GetUniversalVariableFormulaRatio(Number x, Number t, Number sqrtSGP, Number accel, Number f)
        {
            var x2 = x * x;
            var x3 = x2 * x;
            var z = _alpha * x2;
            var ssz = StumpffS(z);
            var scz = StumpffC(z);
            var x2scz = x2 * scz;

            var n = (accel / sqrtSGP * x2scz) + (f * x3 * ssz) + (Radius * x) - (sqrtSGP * t);
            var d = (accel / sqrtSGP * x * (1 - (_alpha * x2 * ssz))) + (f * x2scz) + Radius;
            return n / d;
        }

        private Number StumpffC(Number x)
        {
            if (x.IsZero)
            {
                return Number.Half;
            }
            else if (x > 0)
            {
                return (Number.One - Number.Cos(Number.Sqrt(x))) / x;
            }
            else
            {
                return (Number.Cosh(Number.Sqrt(-x)) - Number.One) / -x;
            }
        }

        private Number StumpffS(Number x)
        {
            if (x.IsZero)
            {
                return Number.One / new Number(6);
            }
            else if (x > 0)
            {
                var rootX = Number.Sqrt(x);
                return (rootX - Number.Sin(rootX)) / rootX.Cube();
            }
            else
            {
                var rootNegX = Number.Sqrt(-x);
                return (Number.Sinh(rootNegX) - rootNegX) / rootNegX.Cube();
            }
        }
    }
}
