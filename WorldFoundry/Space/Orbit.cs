﻿using MathAndScience;
using System;
using MathAndScience.Numerics;
using WorldFoundry.CelestialBodies;
using UniversalTime;

namespace WorldFoundry.Space
{
    /// <summary>
    /// Defines an orbit by the Kepler elements.
    /// </summary>
    public struct Orbit
    {
        private const double Tolerance = 1.0e-8F;

        /// <summary>
        /// Derived value equal to the standard gravitational parameter divided by the semi-major axis.
        /// </summary>
        private readonly double _alpha;

        /// <summary>
        /// The apoapsis of this orbit, in meters. For orbits with <see cref="Eccentricity"/> >= 1,
        /// gives <see cref="double.PositiveInfinity"/>.
        /// </summary>
        public double Apoapsis { get; }

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
        public double MeanMotion { get; }

        /// <summary>
        /// The entity which is being orbited.
        /// </summary>
        public ICelestialLocation OrbitedObject { get; }

        /// <summary>
        /// The periapsis of this orbit, in meters.
        /// </summary>
        public double Periapsis { get; }

        /// <summary>
        /// The period of this orbit, in seconds.
        /// </summary>
        public double Period { get; }

        /// <summary>
        /// The initial position of the orbiting object relative to the orbited one.
        /// </summary>
        public Vector3 R0 { get; }

        /// <summary>
        /// The radius of the orbit at the current position, in meters.
        /// </summary>
        public double Radius { get; }

        /// <summary>
        /// The semi-major axis of this <see cref="Orbit"/>, in meters.
        /// </summary>
        public double SemiMajorAxis { get; }

        /// <summary>
        /// A derived value, equal to G * the sum of masses of the orbiting objects.
        /// </summary>
        public double StandardGravitationalParameter { get; }

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
        /// <param name="orbitedObject">The <see cref="ICelestialLocation"/> which will be
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
            ICelestialLocation orbitedObject,
            double alpha,
            double eccentricity,
            double inclination,
            double meanLongitude,
            double meanMotion,
            double periapsis,
            Vector3 r0,
            double radius,
            double semiMajorAxis,
            double standardGravitationalParameter,
            double trueAnomaly,
            Vector3 v0,
            double period,
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
                ? double.PositiveInfinity
                : (1 + eccentricity) * semiMajorAxis;
        }

        /// <summary>
        /// Calculates the change in velocity necessary for the given object to achieve a circular
        /// orbit around the given entity, as a vector.
        /// </summary>
        /// <param name="orbitingObject">An orbiting object.</param>
        /// <param name="orbitedObject">An orbited entity.</param>
        /// <returns>A change of velocity vector.</returns>
        public static Vector3 GetDeltaVForCircularOrbit(ICelestialLocation orbitingObject, ICelestialLocation orbitedObject)
        {
            var r0 = orbitedObject.ContainingCelestialRegion != orbitingObject.ContainingCelestialRegion
                ? orbitingObject.GetLocalizedPosition(orbitedObject, orbitedObject.Position)
                : orbitingObject.Position - orbitedObject.Position;

            var h = Vector3.Cross(r0, orbitingObject.Velocity);
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

            var standardGravitationalParameter = ScienceConstants.G * (orbitingObject.Mass + orbitedObject.Mass);
            return Math.Sqrt(standardGravitationalParameter / r0.Length()) * perifocalQ;
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
        public static void SetCircularOrbit(ICelestialLocation orbitingObject, ICelestialLocation orbitedObject)
        {
            var standardGravitationalParameter = ScienceConstants.G * (orbitedObject.Mass + orbitingObject.Mass);

            var r0 = orbitingObject.Position - orbitedObject.Position;
            var radius = r0.Length();

            // Calculate magnitudes manually to avoid low-precision
            // implementation resulting in infinity.
            var r0x2 = Math.Pow(r0.X, 2);
            var r0z2 = Math.Pow(r0.Z, 2);
            var semiMajorAxis = Math.Sqrt(r0x2 + Math.Pow(r0.Y, 2) + r0z2);
            var periapsis = semiMajorAxis;

            var xz = new Vector3(r0.X, 0, r0.Z);
            var inclination = Math.Acos(Math.Sqrt(r0x2 + r0z2) / semiMajorAxis);
            var angleAscending = Vector3.UnitX.Angle(xz) - MathConstants.HalfPI;

            var n = new Vector3(Math.Cos(angleAscending), Math.Sin(angleAscending), 0);
            var argPeriapsis = Math.Acos(Vector3.Dot(n, r0) / (n.Length() * radius));
            if (r0.Z < 0)
            {
                argPeriapsis = MathConstants.TwoPI - argPeriapsis;
            }

            // Calculate the perifocal vectors
            var cosineAngleAscending = Math.Cos(angleAscending);
            var sineAngleAscending = Math.Sin(angleAscending);
            var cosineArgPeriapsis = Math.Cos(argPeriapsis);
            var sineArgPeriapsis = Math.Sin(argPeriapsis);
            var cosineInclination = Math.Cos(inclination);
            var sineInclination = Math.Sin(inclination);

            var qi = -(cosineAngleAscending * sineArgPeriapsis) - (sineAngleAscending * cosineInclination * cosineArgPeriapsis);
            var qj = -(sineAngleAscending * sineArgPeriapsis) + (cosineAngleAscending * cosineInclination * cosineArgPeriapsis);
            var qk = sineInclination * cosineArgPeriapsis;

            var perifocalQ = (qi * Vector3.UnitX) + (qj * Vector3.UnitY) + (qk * Vector3.UnitZ);

            var alpha = standardGravitationalParameter / semiMajorAxis;
            orbitingObject.Velocity = Math.Sqrt(alpha) * perifocalQ;

            var meanLongitude = angleAscending + argPeriapsis;

            var period = MathConstants.TwoPI * Math.Sqrt(Math.Pow(semiMajorAxis, 3) / standardGravitationalParameter);

            orbitingObject.Orbit = new Orbit(
                orbitedObject,
                alpha,
                0,
                inclination,
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
        /// <param name="eccentricity">The degree to which the orbit is non-circular.</param>
        /// <remarks>
        /// The orbiting object's current position will be assumed to be on the desired orbit. An
        /// inclination will be calculated from the current position, and presumed to be the maximum
        /// inclination.
        /// </remarks>
        public static void SetOrbit(
            ICelestialLocation orbitingObject,
            ICelestialLocation orbitedObject,
            double eccentricity)
        {
            if (orbitingObject == null)
            {
                throw new ArgumentNullException(nameof(orbitingObject), $"{nameof(orbitingObject)} cannot be null");
            }
            if (orbitedObject == null)
            {
                throw new ArgumentNullException(nameof(orbitedObject), $"{nameof(orbitedObject)} cannot be null");
            }
            if (eccentricity < 0)
            {
                throw new ArgumentOutOfRangeException("eccentricity must be >= 0");
            }

            var standardGravitationalParameter = ScienceConstants.G * (orbitedObject.Mass + orbitingObject.Mass);

            var r0 = orbitedObject.ContainingCelestialRegion == orbitingObject.ContainingCelestialRegion
                ? orbitingObject.Position - orbitingObject.Position
                : orbitingObject.GetLocalizedPosition(orbitedObject, orbitedObject.Position);

            var radius = r0.Length();

            var xz = new Vector3(r0.X, 0, r0.Z);
            var inclination = Math.Acos(Math.Sqrt(Math.Pow(xz.X, 2) + Math.Pow(xz.Z, 2)) / radius);
            var angleAscending = Vector3.UnitX.Angle(xz) - MathConstants.HalfPI;

            var trueAnomaly = Randomizer.Instance.NextDouble(MathConstants.TwoPI);

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
            var period = MathConstants.TwoPI * Math.Sqrt(Math.Pow(semiMajorAxis, 3) / standardGravitationalParameter);
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
            var cosineAngleAscending = Math.Cos(angleAscending);
            var sineAngleAscending = Math.Sin(angleAscending);
            var sineInclination = Math.Sin(inclination);
            var cosineInclination = Math.Cos(inclination);

            var argumentPeriapsis = Math.Atan2(r0.Z / sineInclination, (r0.X * cosineAngleAscending) + (r0.Y * sineAngleAscending)) - trueAnomaly;

            var cosineArgPeriapsis = Math.Cos(argumentPeriapsis);
            var sineArgPeriapsis = Math.Sin(argumentPeriapsis);

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

            var meanLongitude = angleAscending + argumentPeriapsis;

            orbitingObject.Velocity = Math.Sqrt(standardGravitationalParameter / semiLatusRectum)
                * ((-sineTrueAnomaly * perifocalP) + (eccentricity * perifocalQ) + (cosineTrueAnomaly * perifocalQ));

            orbitingObject.Orbit = new Orbit(
                orbitedObject,
                alpha,
                eccentricity,
                inclination,
                meanLongitude,
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
        /// Sets the orbit of the given <see cref="ICelestialLocation"/> according to the given orbital
        /// parameters, and adjusts its position and velocity as necessary.
        /// </summary>
        /// <param name="orbitingObject">The celestial object which will be in orbit.</param>
        /// <param name="orbitedObject">The celestial entity to be orbited.</param>
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
            ICelestialLocation orbitingObject,
            ICelestialLocation orbitedObject,
            double periapsis,
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

            var standardGravitationalParameter = ScienceConstants.G * (orbitedObject.Mass + orbitingObject.Mass);

            var semiLatusRectum = periapsis * (1 + eccentricity);

            var eccentricitySquared = eccentricity * eccentricity;
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
            var radius = semiLatusRectum / (1 + (eccentricity * cosineTrueAnomaly));

            var meanLongitude = angleAscending + argPeriapsis;

            var r0 = (radius * cosineTrueAnomaly * perifocalP) + (radius * sineTrueAnomaly * perifocalQ);
            if (orbitingObject.ContainingCelestialRegion != orbitedObject.ContainingCelestialRegion)
            {
                orbitingObject.Position += orbitingObject.GetLocalizedPosition(orbitedObject, r0);
            }
            else
            {
                orbitingObject.Position = orbitedObject.Position + r0;
            }

            orbitingObject.Velocity = Math.Sqrt(standardGravitationalParameter / semiLatusRectum)
                * ((-sineTrueAnomaly * perifocalP) + (eccentricity * perifocalQ) + (cosineTrueAnomaly * perifocalQ));

            var eccentricAnomaly = Math.Atan2(eccentricity + cosineTrueAnomaly, Math.Sqrt(1 - eccentricitySquared) * sineTrueAnomaly);
            var meanAnomaly = eccentricAnomaly - (eccentricity * Math.Sin(eccentricAnomaly));
            var semiMajorAxisCubed = Math.Pow(semiMajorAxis, 3);
            var period = MathConstants.TwoPI * Math.Sqrt(Math.Pow(semiMajorAxis, 3) / standardGravitationalParameter);
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
            => (MeanLongitude + t + Math.PI) % MathConstants.TwoPI;

        /// <summary>
        /// Gets orbital parameters at a given time.
        /// </summary>
        /// <param name="t">The number of seconds which have elapsed since the orbit's defining
        /// epoch (time of pericenter).</param>
        /// <returns>The mean longitude and mean anomaly, in radians (normalized to 0-2π).</returns>
        public (double meanLongitude, double meanAnomaly) GetMeanLongitudeAndAnomalyAtTime(double t)
        {
            var meanAnomaly = MeanMotion * t % MathConstants.TwoPI;
            return ((MeanLongitude + meanAnomaly) % MathConstants.TwoPI, meanAnomaly);
        }

        /// <summary>
        /// Gets orbital parameters at a given time.
        /// </summary>
        /// <param name="time">The time at which to determine orbital parameters.</param>
        /// <returns>The mean longitude and mean anomaly, in radians (normalized to 0-2π).</returns>
        public (double meanLongitude, double meanAnomaly) GetMeanLongitudeAndAnomalyAtTime(Duration time)
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
        public (Vector3 position, Vector3 velocity) GetStateVectorsAtTime(double t)
        {
            // Universal variable formulas; Newton's method

            var sqrtSGP = Math.Sqrt(StandardGravitationalParameter);
            var accel = Radius / V0.Length();
            var f = 1.0 - (_alpha * Radius);

            // Initial guess for x
            var x = sqrtSGP * Math.Abs(_alpha) * t;

            // Find acceptable x
            var ratio = GetUniversalVariableFormulaRatio(x, t, sqrtSGP, accel, f);
            while (Math.Abs(ratio) > Tolerance)
            {
                x -= ratio;
                ratio = GetUniversalVariableFormulaRatio(x, t, sqrtSGP, accel, f);
            }

            var x2 = x * x;
            var x3 = Math.Pow(x, 3);
            var ax2 = _alpha * x2;
            var ssax2 = StumpffS(ax2);
            var scax2 = StumpffC(ax2);
            var ssax2x3 = ssax2 * x3;

            var uvf = 1.0 - (x2 / Radius * scax2);
            var uvg = t - (1.0 / sqrtSGP * ssax2x3);

            var r = (R0 * uvf) + (V0 * uvg);
            var rLength = r.Length();

            var uvfp = sqrtSGP / (rLength * Radius) * ((_alpha * ssax2x3) - x);
            var uvfgp = 1.0 - (x2 / rLength * scax2);

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
        public double GetTrueAnomalyAtTime(double t)
        {
            var (r, _) = GetStateVectorsAtTime(t);

            var p = SemiMajorAxis * (1 - (Eccentricity * Eccentricity));
            return Math.Atan2(Math.Sqrt(p / StandardGravitationalParameter) * Vector3.Dot(r, r), p - Radius);
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

        internal static double GetHillSphereRadius(ICelestialLocation orbitingObject, ICelestialLocation orbitedObject, double semiMajorAxis, double eccentricity)
            => semiMajorAxis * (1 - eccentricity) * Math.Pow(orbitingObject.Mass / (3 * orbitedObject.Mass), 1.0 / 3.0);

        internal static double GetSemiMajorAxisForPeriod(ICelestialLocation orbitingObject, ICelestialLocation orbitedObject, double period)
            => Math.Pow(ScienceConstants.G * (orbitingObject.Mass + orbitedObject.Mass) * period * period / (MathConstants.FourPI * Math.PI), 1.0 / 3.0);

        internal double GetHillSphereRadius(ICelestialLocation orbitingObject)
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
        internal double GetMutualHillSphereRadius(ICelestialLocation orbitingObject, double otherMass)
            => Math.Pow((orbitingObject.Mass + otherMass) / (3 * OrbitedObject.Mass), 1.0 / 3.0) * SemiMajorAxis;

        internal double GetSphereOfInfluenceRadius(ICelestialLocation orbitingObject)
            => SemiMajorAxis * Math.Pow(orbitingObject.Mass / OrbitedObject.Mass, 2.0 / 5.0);

        private double GetUniversalVariableFormulaRatio(double x, double t, double sqrtSGP, double accel, double f)
        {
            var x2 = x * x;
            var z = _alpha * x2;
            var ssz = StumpffS(z);
            var scz = StumpffC(z);
            var x2scz = x2 * scz;

            var n = (accel / sqrtSGP * x2scz) + (f * Math.Pow(x, 3) * ssz) + (Radius * x) - (sqrtSGP * t);
            var d = (accel / sqrtSGP * x * (1.0 - (_alpha * x2 * ssz))) + (f * x2scz) + Radius;
            return n / d;
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