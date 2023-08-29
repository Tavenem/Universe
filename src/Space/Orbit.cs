using System.Text.Json.Serialization;
using Tavenem.DataStorage;
using Tavenem.Randomize;
using Tavenem.Time;

namespace Tavenem.Universe.Space;

/// <summary>
/// Defines an orbit by the Kepler elements.
/// </summary>
/// <param name="OrbitedId">
/// <para>
/// The <see cref="IIdItem.Id"/> of the primary in the two-body system of the orbit.
/// </para>
/// <para>
/// May be <see langword="null"/> if the primary of this orbit is a system rather than an object
/// (e.g. the center of mass of a galaxy).
/// </para>
/// </param>
/// <param name="OrbitedMass">
/// The mass of the object or system being orbited.
/// </param>
/// <param name="OrbitedPosition">
/// <para>
/// The position of the object or system being orbited at the epoch, as a vector in the same
/// reference frame as <see cref="R0"/>.
/// </para>
/// <para>
/// Note that this does not necessarily refer to the barycenter of the system, but to the position
/// of the primary in the orbit's two-body system.
/// </para>
/// </param>
/// <param name="Barycenter">
/// The barycenter of the orbit, in the same frame of reference as <see cref="OrbitedPosition"/>.
/// </param>
/// <param name="Eccentricity">The eccentricity of this orbit.</param>
/// <param name="Inclination">
/// The angle between the X-Z plane through the barycenter, and the plane of the orbit, in radians.
/// </param>
/// <param name="LongitudeOfPeriapsis">
/// The longitude at which periapsis would occur if <see cref="Inclination"/> were zero, in radians.
/// </param>
/// <param name="MeanLongitude">The mean longitude of this orbit at epoch, in radians.</param>
/// <param name="MeanMotion">The mean motion of this orbit, in radians per second.</param>
/// <param name="Periapsis">The periapsis of this orbit, in meters.</param>
/// <param name="R0">
/// The initial position of the orbiting object relative to the barycenter.
/// </param>
/// <param name="Radius">The radius of the orbit at the current position, in meters.</param>
/// <param name="SemiMajorAxis">The semi-major axis of this orbit, in meters.</param>
/// <param name="StandardGravitationalParameter">
/// A derived value, equal to G * the sum of masses of the orbiting objects.
/// </param>
/// <param name="TrueAnomaly">The current true anomaly of this orbit, in radians.</param>
/// <param name="V0">
/// The initial velocity of the orbiting object relative to the orbited one.
/// </param>
/// <param name="Period">The period of this orbit, in seconds.</param>
/// <param name="Epoch">
/// <para>
/// The time at which the state of this orbit is defined, which coincides with a time of pericenter
/// passage.
/// </para>
/// <para>
/// This is typically defined when an orbit is first initialized as a random amount of time, smaller
/// than its period. It reflects the theoretical "first" time of pericenter passage, if the orbit
/// had existed unchanged since the beginning of the time period. The most recent time of pericenter
/// passage is calculated by modulus division.
/// </para>
/// </param>
[method: JsonConstructor]
public readonly record struct Orbit(
    string? OrbitedId,
    HugeNumber OrbitedMass,
    Vector3<HugeNumber> OrbitedPosition,
    Vector3<HugeNumber> Barycenter,
    double Eccentricity,
    double Inclination,
    double LongitudeOfPeriapsis,
    double MeanLongitude,
    HugeNumber MeanMotion,
    HugeNumber Periapsis,
    Vector3<HugeNumber> R0,
    HugeNumber Radius,
    HugeNumber SemiMajorAxis,
    HugeNumber StandardGravitationalParameter,
    double TrueAnomaly,
    Vector3<HugeNumber> V0,
    HugeNumber Period,
    Duration Epoch)
{
    private const double Tolerance = 1.0e-8;

    /// <summary>
    /// Derived value equal to the standard gravitational parameter divided by the semi-major axis.
    /// </summary>
    private readonly HugeNumber _alpha = StandardGravitationalParameter / SemiMajorAxis;

    /// <summary>
    /// The apoapsis of this orbit, in meters. For orbits with <see cref="Eccentricity"/> >= 1,
    /// gives <see cref="double.PositiveInfinity"/>.
    /// </summary>
    [JsonIgnore]
    public HugeNumber Apoapsis { get; } = Eccentricity switch
    {
        >= 1 => HugeNumber.PositiveInfinity,
        <= 0 => SemiMajorAxis,
        _ => (1 + Eccentricity) * SemiMajorAxis,
    };

    /// <summary>
    /// The eccentricity of this orbit.
    /// </summary>
    public double Eccentricity { get; init; } = Eccentricity.Clamp(0, 1);

    /// <summary>
    /// The angle between the X-Z plane through the center of the object orbited, and the plane
    /// of the orbit, in radians.
    /// </summary>
    public double Inclination { get; init; } = NormalizeRadians(Inclination);

    /// <summary>
    /// The longitude at which periapsis would occur if <see cref="Inclination"/> were zero, in
    /// radians.
    /// </summary>
    public double LongitudeOfPeriapsis { get; init; } = NormalizeRadians(LongitudeOfPeriapsis);

    /// <summary>
    /// The mean longitude of this orbit at epoch, in radians.
    /// </summary>
    public double MeanLongitude { get; init; } = NormalizeRadians(MeanLongitude);

    /// <summary>
    /// The current true anomaly of this orbit, in radians.
    /// </summary>
    public double TrueAnomaly { get; init; } = NormalizeRadians(TrueAnomaly);

    /// <summary>
    /// Calculates the change in velocity necessary for the given object to achieve a circular
    /// orbit around the given entity, as a vector.
    /// </summary>
    /// <param name="orbitingObject">An orbiting object.</param>
    /// <param name="orbitedObject">An orbited entity.</param>
    /// <returns>A change of velocity vector.</returns>
    public static Vector3<HugeNumber> GetDeltaVForCircularOrbit(CosmicLocation orbitingObject, CosmicLocation orbitedObject)
        => GetDeltaVForCircularOrbit(
            orbitingObject,
            orbitedObject.Mass,
            orbitedObject.ParentId != orbitingObject.ParentId
                ? orbitingObject.LocalizePosition(orbitedObject)
                : orbitedObject.Position);

    /// <summary>
    /// Calculates the change in velocity necessary for the given object to achieve a circular
    /// orbit around the given entity, as a vector.
    /// </summary>
    /// <param name="orbitingObject">An orbiting object.</param>
    /// <param name="orbitedMass">
    /// The mass of the object or system being orbited, in kg.
    /// </param>
    /// <param name="orbitedPosition">
    /// The position of the object or system being orbited at the epoch, as a vector in the same
    /// reference frame as <paramref name="orbitingObject"/>.
    /// </param>
    /// <returns>A change of velocity vector.</returns>
    public static Vector3<HugeNumber> GetDeltaVForCircularOrbit(CosmicLocation orbitingObject, HugeNumber orbitedMass, Vector3<HugeNumber> orbitedPosition)
    {
        var r0 = orbitingObject.Position - orbitedPosition;

        var h = Vector3<HugeNumber>.Cross(r0, orbitingObject.Velocity);
        var inclination = HugeNumber.Acos(h.Z / h.Length());
        var n = Vector3<HugeNumber>.Cross(Vector3<HugeNumber>.UnitZ, h);
        var angleAscending = HugeNumber.Acos(n.X / n.Length());

        // Calculate the perifocal vector
        var cosineAngleAscending = HugeNumber.Cos(angleAscending);
        var sineAngleAscending = HugeNumber.Sin(angleAscending);
        var cosineInclination = HugeNumber.Cos(inclination);
        var sineInclination = HugeNumber.Sin(inclination);

        var qi = -(sineAngleAscending * cosineInclination);
        var qj = cosineAngleAscending * cosineInclination;
        var qk = sineInclination;

        var perifocalQ = (qi * Vector3<HugeNumber>.UnitX) + (qj * Vector3<HugeNumber>.UnitY) + (qk * Vector3<HugeNumber>.UnitZ);

        var standardGravitationalParameter = HugeNumberConstants.G * (orbitingObject.Mass + orbitedMass);
        return HugeNumber.Sqrt(standardGravitationalParameter / r0.Length()) * perifocalQ;
    }

    /// <summary>
    /// Sets the orbit of the given object based on its current position, and adjusts its
    /// velocity as necessary to make the orbit circular (zero eccentricity).
    /// </summary>
    /// <param name="dataStore">
    /// The <see cref="IDataStore"/> from which to retrieve instances.
    /// </param>
    /// <param name="orbitedId">
    /// <para>
    /// The <see cref="IIdItem.Id"/> of the primary in the two-body system of the orbit.
    /// </para>
    /// <para>
    /// May be <see langword="null"/> if the primary of this orbit is a system rather than an object
    /// (e.g. the center of mass of a galaxy).
    /// </para>
    /// </param>
    /// <param name="orbitingObject">The celestial object which will be in orbit.</param>
    /// <param name="orbitedObject">The celestial entity to be orbited.</param>
    /// <remarks>
    /// The orbiting object's current position will be assumed to be on the desired orbit. An
    /// inclination will be calculated from the current position, and presumed to be the maximum
    /// inclination.
    /// </remarks>
    public static Task SetCircularOrbitAsync(
        IDataStore dataStore,
        string? orbitedId,
        CosmicLocation orbitingObject,
        CosmicLocation orbitedObject)
        => SetCircularOrbitAsync(
            dataStore,
            orbitingObject,
            orbitedId,
            orbitedObject.Mass,
            orbitedObject.ParentId != orbitingObject.ParentId
                ? orbitingObject.LocalizePosition(orbitedObject)
                : orbitedObject.Position);

    /// <summary>
    /// Sets the orbit of the given object based on its current position, and adjusts its
    /// velocity as necessary to make the orbit circular (zero eccentricity).
    /// </summary>
    /// <param name="dataStore">
    /// The <see cref="IDataStore"/> from which to retrieve instances.
    /// </param>
    /// <param name="orbitingObject">The celestial object which will be in orbit.</param>
    /// <param name="orbitedId">
    /// <para>
    /// The <see cref="IIdItem.Id"/> of the primary in the two-body system of the orbit.
    /// </para>
    /// <para>
    /// May be <see langword="null"/> if the primary of this orbit is a system rather than an object
    /// (e.g. the center of mass of a galaxy).
    /// </para>
    /// </param>
    /// <param name="orbitedMass">
    /// The mass of the object or system being orbited, in kg.
    /// </param>
    /// <param name="orbitedPosition">
    /// The position of the object or system being orbited at the epoch, as a vector in the same
    /// reference frame as <paramref name="orbitingObject"/>.
    /// </param>
    /// <remarks>
    /// The orbiting object's current position will be assumed to be on the desired orbit. An
    /// inclination will be calculated from the current position, and presumed to be the maximum
    /// inclination.
    /// </remarks>
    public static async Task SetCircularOrbitAsync(
        IDataStore dataStore,
        CosmicLocation orbitingObject,
        string? orbitedId,
        HugeNumber orbitedMass,
        Vector3<HugeNumber> orbitedPosition)
    {
        AssignCircularOrbit(orbitingObject, orbitedId, orbitedMass, orbitedPosition);
        await orbitingObject.ResetOrbitAsync(dataStore).ConfigureAwait(false);
    }

    /// <summary>
    /// Sets the orbit of the given object based on its current position and the given <paramref
    /// name="eccentricity"/>, and adjusts its velocity as necessary.
    /// </summary>
    /// <param name="dataStore">
    /// The <see cref="IDataStore"/> from which to retrieve instances.
    /// </param>
    /// <param name="orbitingObject">The celestial object which will be in orbit.</param>
    /// <param name="orbitedObject">The celestial entity to be orbited.</param>
    /// <param name="eccentricity">The degree to which the orbit is non-circular. The absolute
    /// value will be used (i.e. negative values are treated as positives).</param>
    /// <remarks>
    /// The orbiting object's current position will be assumed to be on the desired orbit. An
    /// inclination will be calculated from the current position, and presumed to be the maximum
    /// inclination.
    /// </remarks>
    public static Task SetOrbitAsync(
        IDataStore dataStore,
        CosmicLocation orbitingObject,
        CosmicLocation orbitedObject,
        double eccentricity) => SetOrbitAsync(
            dataStore,
            orbitingObject,
            orbitedObject.Id,
            orbitedObject.Mass,
            orbitedObject.ParentId != orbitingObject.ParentId
                ? orbitingObject.LocalizePosition(orbitedObject)
                : orbitedObject.Position,
            eccentricity);

    /// <summary>
    /// Sets the orbit of the given object based on its current position and the given <paramref
    /// name="eccentricity"/>, and adjusts its velocity as necessary.
    /// </summary>
    /// <param name="dataStore">
    /// The <see cref="IDataStore"/> from which to retrieve instances.
    /// </param>
    /// <param name="orbitingObject">The celestial object which will be in orbit.</param>
    /// <param name="orbitedId">
    /// <para>
    /// The <see cref="IIdItem.Id"/> of the primary in the two-body system of the orbit.
    /// </para>
    /// <para>
    /// May be <see langword="null"/> if the primary of this orbit is a system rather than an object
    /// (e.g. the center of mass of a galaxy).
    /// </para>
    /// </param>
    /// <param name="orbitedMass">
    /// The mass of the object or system being orbited, in kg.
    /// </param>
    /// <param name="orbitedPosition">
    /// The position of the object or system being orbited at the epoch, as a vector in the same
    /// reference frame as <paramref name="orbitingObject"/>.
    /// </param>
    /// <param name="eccentricity">The degree to which the orbit is non-circular. The absolute
    /// value will be used (i.e. negative values are treated as positives).</param>
    /// <remarks>
    /// The orbiting object's current position will be assumed to be on the desired orbit. An
    /// inclination will be calculated from the current position, and presumed to be the maximum
    /// inclination.
    /// </remarks>
    public static async Task SetOrbitAsync(
        IDataStore dataStore,
        CosmicLocation orbitingObject,
        string? orbitedId,
        HugeNumber orbitedMass,
        Vector3<HugeNumber> orbitedPosition,
        double eccentricity)
    {
        AssignOrbit(orbitingObject, orbitedId, orbitedMass, orbitedPosition, eccentricity);
        await orbitingObject.ResetOrbitAsync(dataStore).ConfigureAwait(false);
    }

    /// <summary>
    /// Sets the orbit of the given object based on the given <paramref name="eccentricity"/> and
    /// <paramref name="period"/>, and adjusts its position and velocity as necessary.
    /// </summary>
    /// <param name="dataStore">
    /// The <see cref="IDataStore"/> from which to retrieve instances.
    /// </param>
    /// <param name="orbitingObject">The celestial object which will be in orbit.</param>
    /// <param name="orbitedId">
    /// <para>
    /// The <see cref="IIdItem.Id"/> of the primary in the two-body system of the orbit.
    /// </para>
    /// <para>
    /// May be <see langword="null"/> if the primary of this orbit is a system rather than an object
    /// (e.g. the center of mass of a galaxy).
    /// </para>
    /// </param>
    /// <param name="orbitedMass">
    /// The mass of the object or system being orbited, in kg.
    /// </param>
    /// <param name="orbitedPosition">
    /// The position of the object or system being orbited at the epoch, as a vector in the same
    /// reference frame as <paramref name="orbitingObject"/>.
    /// </param>
    /// <param name="eccentricity">
    /// The degree to which the orbit is non-circular. The absolute value will be used (i.e.
    /// negative values are treated as positives).
    /// </param>
    /// <param name="period">
    /// The desired period of the orbit, in seconds.
    /// </param>
    /// <remarks>
    /// An inclination will be calculated from the current position, scaled in or out as needed to
    /// place that position on the desired orbit, and presumed to be the maximum inclination.
    /// </remarks>
    public static async Task SetOrbitAsync(
        IDataStore dataStore,
        CosmicLocation orbitingObject,
        string? orbitedId,
        HugeNumber orbitedMass,
        Vector3<HugeNumber> orbitedPosition,
        double eccentricity,
        HugeNumber period)
    {
        AssignOrbit(orbitingObject, orbitedId, orbitedMass, orbitedPosition, eccentricity, period);
        await orbitingObject.ResetOrbitAsync(dataStore).ConfigureAwait(false);
    }

    /// <summary>
    /// Sets the orbit of the given <see cref="CosmicLocation"/> according to the given
    /// orbital parameters, and adjusts its position and velocity as necessary.
    /// </summary>
    /// <param name="dataStore">
    /// The <see cref="IDataStore"/> from which to retrieve instances.
    /// </param>
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
    /// <param name="longitudeAscending">
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
    public static Task SetOrbitAsync(
        IDataStore dataStore,
        CosmicLocation orbitingObject,
        CosmicLocation orbitedObject,
        HugeNumber periapsis,
        double eccentricity,
        double inclination,
        double longitudeAscending,
        double argPeriapsis,
        double trueAnomaly) => SetOrbitAsync(
            dataStore,
            orbitingObject,
            orbitedObject.Id,
            orbitedObject.Mass,
            orbitedObject.ParentId != orbitingObject.ParentId
                ? orbitingObject.LocalizePosition(orbitedObject)
                : orbitedObject.Position,
            periapsis,
            eccentricity,
            inclination,
            longitudeAscending,
            argPeriapsis,
            trueAnomaly);

    /// <summary>
    /// Sets the orbit of the given <see cref="CosmicLocation"/> according to the given
    /// orbital parameters, and adjusts its position and velocity as necessary.
    /// </summary>
    /// <param name="dataStore">
    /// The <see cref="IDataStore"/> from which to retrieve instances.
    /// </param>
    /// <param name="orbitingObject">The celestial object which will be in orbit.</param>
    /// <param name="orbitedId">
    /// <para>
    /// The <see cref="IIdItem.Id"/> of the primary in the two-body system of the orbit.
    /// </para>
    /// <para>
    /// May be <see langword="null"/> if the primary of this orbit is a system rather than an object
    /// (e.g. the center of mass of a galaxy).
    /// </para>
    /// </param>
    /// <param name="orbitedMass">
    /// The mass of the object or system being orbited, in kg.
    /// </param>
    /// <param name="orbitedPosition">
    /// The position of the object or system being orbited at the epoch, as a vector in the same
    /// reference frame as <paramref name="orbitingObject"/>.
    /// </param>
    /// <param name="periapsis">
    /// The distance between the objects at the closest point in the orbit.
    /// </param>
    /// <param name="eccentricity">The degree to which the orbit is non-circular. The absolute
    /// value will be used (i.e. negative values are treated as positives).</param>
    /// <param name="inclination">
    /// The angle between the X-Z plane through the center of the object orbited, and the plane
    /// of the orbit, in radians. Values will be normalized between zero and π.
    /// </param>
    /// <param name="longitudeAscending">
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
    public static async Task SetOrbitAsync(
        IDataStore dataStore,
        CosmicLocation orbitingObject,
        string? orbitedId,
        HugeNumber orbitedMass,
        Vector3<HugeNumber> orbitedPosition,
        HugeNumber periapsis,
        double eccentricity,
        double inclination,
        double longitudeAscending,
        double argPeriapsis,
        double trueAnomaly)
    {
        AssignOrbit(
            orbitingObject,
            orbitedId,
            orbitedMass,
            orbitedPosition,
            periapsis,
            eccentricity,
            inclination,
            longitudeAscending,
            argPeriapsis,
            trueAnomaly);
        await orbitingObject.ResetOrbitAsync(dataStore).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the eccentric anomaly of the orbit at a given true anomaly.
    /// </summary>
    /// <param name="t">The true anomaly.</param>
    /// <returns>The eccentric anomaly of the orbit, in radians.</returns>
    public double GetEccentricAnomaly(double t)
        => Math.Atan2(Math.Sqrt(1 - (Eccentricity * Eccentricity)) * Math.Sin(t), Eccentricity + Math.Cos(t));

    /// <summary>
    /// Gets the ecliptic longitude of the orbited body from the perspective of the orbiting
    /// body at a given true anomaly.
    /// </summary>
    /// <param name="t">The true anomaly.</param>
    /// <returns>The ecliptic longitude of the orbited body from the perspective of the orbiting
    /// body, in radians (normalized to 0-2π).</returns>
    public double GetEclipticLongitudeAtTrueAnomaly(double t)
        => (LongitudeOfPeriapsis + t) % DoubleConstants.TwoPi;

    /// <summary>
    /// Gets the mean anomaly of the orbit at a given true anomaly.
    /// </summary>
    /// <param name="t">The true anomaly.</param>
    /// <returns>The eccentric anomaly of the orbit, in radians (normalized to 0-2π).</returns>
    public double GetMeanAnomaly(double t)
    {
        var eccentricAnomaly = GetEccentricAnomaly(t);
        return eccentricAnomaly - (Eccentricity * Math.Sin(eccentricAnomaly));
    }

    /// <summary>
    /// Gets orbital parameters at a given time.
    /// </summary>
    /// <param name="t">The number of seconds which have elapsed since the orbit's defining
    /// epoch (time of pericenter).</param>
    /// <returns>The mean longitude and mean anomaly, in radians (normalized to 0-2π).</returns>
    public (double meanLongitude, HugeNumber meanAnomaly) GetMeanLongitudeAndAnomalyAtTime(HugeNumber t)
    {
        var meanAnomaly = MeanMotion * t % HugeNumberConstants.TwoPi;
        return ((double)((MeanLongitude + meanAnomaly) % HugeNumberConstants.TwoPi), meanAnomaly);
    }

    /// <summary>
    /// Gets orbital parameters at a given time.
    /// </summary>
    /// <param name="moment">The time at which to determine orbital parameters.</param>
    /// <returns>The mean longitude and mean anomaly, in radians (normalized to 0-2π).</returns>
    public (double meanLongitude, HugeNumber meanAnomaly) GetMeanLongitudeAndAnomalyAtTime(Duration moment)
    {
        var t = (moment >= Epoch
            ? moment - Epoch
            : Epoch - moment).ToSeconds() % Period;
        if (moment < Epoch)
        {
            t = Period - t;
        }
        return GetMeanLongitudeAndAnomalyAtTime(t);
    }

    /// <summary>
    /// Gets an <see cref="OrbitalParameters"/> instance which represents this orbit.
    /// </summary>
    /// <returns>
    /// An <see cref="OrbitalParameters"/> instance which represents this orbit.
    /// </returns>
    public OrbitalParameters GetOrbitalParameters()
    {
        var xz = new Vector3<HugeNumber>(R0.X, 0, R0.Z);
        var angleAscending = Vector3<HugeNumber>.UnitX.Angle(xz) - HugeNumberConstants.HalfPi;

        // Calculate the perifocal vectors
        var cosineAngleAscending = HugeNumber.Cos(angleAscending);
        var sineAngleAscending = HugeNumber.Sin(angleAscending);
        var sineInclination = HugeNumber.Sin(Inclination);

        var argumentPeriapsis = HugeNumber.Atan2(R0.Z / sineInclination, (R0.X * cosineAngleAscending) + (R0.Y * sineAngleAscending) - TrueAnomaly);

        return new OrbitalParameters(
            OrbitedMass,
            OrbitedPosition,
            Periapsis,
            Eccentricity,
            Inclination,
            (double)angleAscending,
            (double)argumentPeriapsis,
            TrueAnomaly);
    }

    /// <summary>
    /// Gets orbital state vectors after the given amount of time has passed since the <see
    /// cref="Epoch"/>.
    /// </summary>
    /// <param name="time">The amount of time after which to determine orbital state
    /// vectors.</param>
    /// <returns>
    /// The position vector (relative to the barycenter), and the velocity vector.
    /// </returns>
    public (Vector3<HugeNumber> position, Vector3<HugeNumber> velocity) GetStateVectorsAfterDuration(Duration time)
        => GetStateVectorsAtTime(time.ToSeconds() % Period);

    /// <summary>
    /// Gets orbital state vectors at a given time.
    /// </summary>
    /// <param name="t">The number of seconds which have elapsed since the orbit's defining epoch
    /// (time of pericenter).</param>
    /// <returns>
    /// The position vector (relative to the barycenter), and the velocity vector.
    /// </returns>
    public (Vector3<HugeNumber> position, Vector3<HugeNumber> velocity) GetStateVectorsAtTime(HugeNumber t)
    {
        // Universal variable formulas; Newton's method

        var sqrtSGP = HugeNumber.Sqrt(StandardGravitationalParameter);
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
        var alphaX2 = _alpha * x2;
        var stumpffSAlphaX2 = StumpffS(alphaX2);
        var stumpffCAlphaX2 = StumpffC(alphaX2);
        var stumpffSAlphaX2X3 = stumpffSAlphaX2 * x3;

        var uvf = 1 - (x2 / Radius * stumpffCAlphaX2);
        var uvg = t - (1 / sqrtSGP * stumpffSAlphaX2X3);

        var r = (R0 * uvf) + (V0 * uvg);
        var rLength = r.Length();

        var uvfP = sqrtSGP / (rLength * Radius) * ((_alpha * stumpffSAlphaX2X3) - x);
        var uvfGP = 1 - (x2 / rLength * stumpffCAlphaX2);

        var v = (R0 * uvfP) + (V0 * uvfGP);

        return (r, v);
    }

    /// <summary>
    /// Gets orbital state vectors at a given time.
    /// </summary>
    /// <param name="moment">The time at which to determine orbital state vectors.</param>
    /// <returns>
    /// The position vector (relative to the barycenter), and the velocity vector.
    /// </returns>
    public (Vector3<HugeNumber> position, Vector3<HugeNumber> velocity) GetStateVectorsAtTime(Instant moment)
    {
        var t = (moment.Offset >= Epoch
            ? moment.Offset - Epoch
            : Epoch - moment.Offset).ToSeconds() % Period;
        if (moment.Offset < Epoch)
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
    public double GetTrueAnomalyAtTime(HugeNumber t)
    {
        var (r, _) = GetStateVectorsAtTime(t);

        var p = SemiMajorAxis * (1 - (Eccentricity * Eccentricity));
        return (double)HugeNumber.Atan2((p / StandardGravitationalParameter).Sqrt() * Vector3<HugeNumber>.Dot(r, r), p - Radius);
    }

    /// <summary>
    /// Gets the true anomaly of this orbit at a given time.
    /// </summary>
    /// <param name="moment">The moment at which to determine orbital state vectors.</param>
    /// <returns>The true anomaly, in radians.</returns>
    public double GetTrueAnomalyAtTime(Instant moment)
    {
        var t = (moment.Offset >= Epoch
            ? moment.Offset - Epoch
            : Epoch - moment.Offset).ToSeconds() % Period;
        if (moment.Offset < Epoch)
        {
            t = Period - t;
        }
        return GetTrueAnomalyAtTime(t);
    }

    private static double NormalizeRadians(double value)
    {
        if (double.IsInfinity(value))
        {
            return 0;
        }
        value %= DoubleConstants.TwoPi;
        if (value < 0)
        {
            value += DoubleConstants.TwoPi;
        }
        return value;
    }

    /// <summary>
    /// Sets the orbit of the given <see cref="CosmicLocation"/> according to the given
    /// orbital parameters, and adjusts its position and velocity as necessary.
    /// </summary>
    /// <param name="orbitingObject">The celestial object which will be in orbit.</param>
    /// <param name="orbitedId">
    /// <para>
    /// The <see cref="IIdItem.Id"/> of the primary in the two-body system of the orbit.
    /// </para>
    /// <para>
    /// May be <see langword="null"/> if the primary of this orbit is a system rather than an object
    /// (e.g. the center of mass of a galaxy).
    /// </para>
    /// </param>
    /// <param name="orbitalParameters">The parameters which describe the orbit.</param>
    internal static void AssignOrbit(
        CosmicLocation orbitingObject,
        string? orbitedId,
        OrbitalParameters orbitalParameters)
    {
        if (orbitalParameters.Circular)
        {
            AssignCircularOrbit(
                orbitingObject,
                orbitedId,
                orbitalParameters.OrbitedMass,
                orbitalParameters.OrbitedPosition);
        }
        else if (orbitalParameters.FromEccentricity)
        {
            AssignOrbit(
                orbitingObject,
                orbitedId,
                orbitalParameters.OrbitedMass,
                orbitalParameters.OrbitedPosition,
                orbitalParameters.Eccentricity);
        }
        else
        {
            AssignOrbit(
                orbitingObject,
                orbitedId,
                orbitalParameters.OrbitedMass,
                orbitalParameters.OrbitedPosition,
                orbitalParameters.Periapsis,
                orbitalParameters.Eccentricity,
                orbitalParameters.Inclination,
                orbitalParameters.AngleAscending,
                orbitalParameters.ArgumentOfPeriapsis,
                orbitalParameters.TrueAnomaly);
        }
    }

    /// <summary>
    /// Sets the orbit of the given object based on its current position, and adjusts its
    /// velocity as necessary to make the orbit circular (zero eccentricity).
    /// </summary>
    /// <param name="orbitingObject">The celestial object which will be in orbit.</param>
    /// <param name="orbitedId">
    /// <para>
    /// The <see cref="IIdItem.Id"/> of the primary in the two-body system of the orbit.
    /// </para>
    /// <para>
    /// May be <see langword="null"/> if the primary of this orbit is a system rather than an object
    /// (e.g. the center of mass of a galaxy).
    /// </para>
    /// </param>
    /// <param name="orbitedMass">
    /// The mass of the object or system being orbited, in kg.
    /// </param>
    /// <param name="orbitedPosition">
    /// The position of the object or system being orbited at the epoch, as a vector in the same
    /// reference frame as <paramref name="orbitingObject"/>.
    /// </param>
    /// <remarks>
    /// The orbiting object's current position will be assumed to be on the desired orbit. An
    /// inclination will be calculated from the current position, and presumed to be the maximum
    /// inclination.
    /// </remarks>
    internal static void AssignCircularOrbit(
        CosmicLocation orbitingObject,
        string? orbitedId,
        HugeNumber orbitedMass,
        Vector3<HugeNumber> orbitedPosition)
    {
        var standardGravitationalParameter = HugeNumberConstants.G * (orbitedMass + orbitingObject.Mass);

        var relativePositionVector = orbitingObject.Position - orbitedPosition;
        var distance = relativePositionVector.Length();
        var barycenter = orbitedPosition
            + (relativePositionVector.Normalize()
            * (distance / (HugeNumber.One + (orbitedMass / orbitingObject.Mass))));

        var r0 = orbitingObject.Position - barycenter;
        var radius = r0.Length();

        // Calculate magnitudes manually to avoid low-precision
        // implementation resulting in infinity.
        var r0x2 = r0.X.Square();
        var r0z2 = r0.Z.Square();
        var semiMajorAxis = HugeNumber.Sqrt(r0x2 + r0.Y.Square() + r0z2);
        var periapsis = semiMajorAxis;

        var xz = new Vector3<HugeNumber>(r0.X, 0, r0.Z);
        var inclination = HugeNumber.Acos(HugeNumber.Sqrt(r0x2 + r0z2) / semiMajorAxis);
        var longitudeAscending = Vector3<HugeNumber>.UnitX.Angle(xz) - HugeNumberConstants.HalfPi;

        var cosineLongitudeAscending = HugeNumber.Cos(longitudeAscending);
        var sineLongitudeAscending = HugeNumber.Sin(longitudeAscending);

        var n = new Vector3<HugeNumber>(cosineLongitudeAscending, sineLongitudeAscending, 0);
        var argPeriapsis = HugeNumber.Acos(Vector3<HugeNumber>.Dot(n, r0) / (n.Length() * radius));
        if (r0.Z < 0)
        {
            argPeriapsis = HugeNumberConstants.TwoPi - argPeriapsis;
        }

        // Calculate the perifocal vectors
        var cosineArgPeriapsis = HugeNumber.Cos(argPeriapsis);
        var sineArgPeriapsis = HugeNumber.Sin(argPeriapsis);
        var cosineInclination = HugeNumber.Cos(inclination);
        var sineInclination = HugeNumber.Sin(inclination);

        var qi = -(cosineLongitudeAscending * sineArgPeriapsis) - (sineLongitudeAscending * cosineInclination * cosineArgPeriapsis);
        var qj = -(sineLongitudeAscending * sineArgPeriapsis) + (cosineLongitudeAscending * cosineInclination * cosineArgPeriapsis);
        var qk = sineInclination * cosineArgPeriapsis;

        var perifocalQ = (qi * Vector3<HugeNumber>.UnitX) + (qj * Vector3<HugeNumber>.UnitY) + (qk * Vector3<HugeNumber>.UnitZ);

        var alpha = standardGravitationalParameter / semiMajorAxis;
        orbitingObject.Velocity = HugeNumber.Sqrt(alpha) * perifocalQ;

        var longitudeOfPeriapsis = (double)(longitudeAscending + argPeriapsis);
        if (orbitingObject is Planetoid planetoid)
        {
            longitudeOfPeriapsis -= planetoid.AxialPrecession;
        }
        longitudeOfPeriapsis %= DoubleConstants.TwoPi;

        var period = HugeNumberConstants.TwoPi * HugeNumber.Sqrt(semiMajorAxis.Cube() / standardGravitationalParameter);

        orbitingObject.Orbit = new Orbit(
            orbitedId,
            orbitedMass,
            orbitedPosition,
            barycenter,
            0,
            (double)inclination,
            longitudeOfPeriapsis,
            longitudeOfPeriapsis,
            HugeNumberConstants.TwoPi / period,
            periapsis,
            r0,
            radius,
            semiMajorAxis,
            standardGravitationalParameter,
            0,
            orbitingObject.Velocity,
            period,
            Duration.FromSecondsFloatingPoint(Randomizer.Instance.Next(period)));
    }

    /// <summary>
    /// Sets the orbit of the given object based on its current position and the given <paramref
    /// name="eccentricity"/>, and adjusts its velocity as necessary.
    /// </summary>
    /// <param name="orbitingObject">The celestial object which will be in orbit.</param>
    /// <param name="orbitedId">
    /// <para>
    /// The <see cref="IIdItem.Id"/> of the primary in the two-body system of the orbit.
    /// </para>
    /// <para>
    /// May be <see langword="null"/> if the primary of this orbit is a system rather than an object
    /// (e.g. the center of mass of a galaxy).
    /// </para>
    /// </param>
    /// <param name="orbitedMass">
    /// The mass of the object or system being orbited, in kg.
    /// </param>
    /// <param name="orbitedPosition">
    /// The position of the object or system being orbited at the epoch, as a vector in the same
    /// reference frame as <paramref name="orbitingObject"/>.
    /// </param>
    /// <param name="eccentricity">The degree to which the orbit is non-circular. The absolute
    /// value will be used (i.e. negative values are treated as positives).</param>
    /// <remarks>
    /// The orbiting object's current position will be assumed to be on the desired orbit. An
    /// inclination will be calculated from the current position, and presumed to be the maximum
    /// inclination.
    /// </remarks>
    internal static void AssignOrbit(
        CosmicLocation orbitingObject,
        string? orbitedId,
        HugeNumber orbitedMass,
        Vector3<HugeNumber> orbitedPosition,
        double eccentricity)
    {
        eccentricity = Math.Abs(eccentricity);

        var standardGravitationalParameter = HugeNumberConstants.G * (orbitedMass + orbitingObject.Mass);

        var relativePositionVector = orbitingObject.Position - orbitedPosition;
        var distance = relativePositionVector.Length();
        var barycenter = orbitedPosition
            + (relativePositionVector.Normalize()
            * (distance / (HugeNumber.One + (orbitedMass / orbitingObject.Mass))));

        var r0 = orbitingObject.Position - barycenter;

        var radius = r0.Length();

        var xz = new Vector3<HugeNumber>(r0.X, 0, r0.Z);
        var inclination = HugeNumber.Acos(HugeNumber.Sqrt(xz.X.Square() + xz.Z.Square()) / radius);
        var angleAscending = Vector3<HugeNumber>.UnitX.Angle(xz) - HugeNumberConstants.HalfPi;

        var trueAnomaly = Randomizer.Instance.NextDouble(DoubleConstants.TwoPi);

        var semiLatusRectum = radius * (1 + (eccentricity * Math.Cos(trueAnomaly)));
        var semiMajorAxis = semiLatusRectum / (1 - (eccentricity * eccentricity));

        // The current position must be either the apoapsis or the periapsis,
        // since it was chosen as the reference point for the inclination.
        // Therefore, it is the periapsis if its distance from the orbited
        // body is less than the semi-major axis, and the apoapsis if not.
        var periapsis = radius <= semiMajorAxis
            ? r0.Length()
            : (Vector3<HugeNumber>.Normalize(new Vector3<HugeNumber>(-r0.X, -r0.Y, -r0.Z)) * (semiLatusRectum / (1 + eccentricity))).Length();
        // For parabolic orbits, semi-major axis is undefined, and is set to the periapsis instead.
        if (eccentricity == 1)
        {
            semiMajorAxis = periapsis;
        }
        var period = HugeNumberConstants.TwoPi * HugeNumber.Sqrt(semiMajorAxis.Cube() / standardGravitationalParameter);
        // If at periapsis now, this is the epoch;
        // if not, the epoch is half the period away.
        var epoch = radius > semiMajorAxis
            ? Duration.FromSecondsFloatingPoint(period / 2)
            : Duration.Zero;

        // Calculate the perifocal vectors
        var cosineAngleAscending = HugeNumber.Cos(angleAscending);
        var sineAngleAscending = HugeNumber.Sin(angleAscending);
        var sineInclination = HugeNumber.Sin(inclination);
        var cosineInclination = HugeNumber.Cos(inclination);

        var argumentPeriapsis = HugeNumber.Atan2(r0.Z / sineInclination, (r0.X * cosineAngleAscending) + (r0.Y * sineAngleAscending) - trueAnomaly);

        var cosineArgPeriapsis = HugeNumber.Cos(argumentPeriapsis);
        var sineArgPeriapsis = HugeNumber.Sin(argumentPeriapsis);

        var pi = (cosineAngleAscending * cosineArgPeriapsis) - (sineAngleAscending * cosineInclination * sineArgPeriapsis);
        var pj = (sineAngleAscending * cosineArgPeriapsis) + (cosineAngleAscending * cosineInclination * sineArgPeriapsis);
        var pk = sineInclination * sineArgPeriapsis;

        var qi = -(cosineAngleAscending * sineArgPeriapsis) - (sineAngleAscending * cosineInclination * cosineArgPeriapsis);
        var qj = -(sineAngleAscending * sineArgPeriapsis) + (cosineAngleAscending * cosineInclination * cosineArgPeriapsis);
        var qk = sineInclination * cosineArgPeriapsis;

        var perifocalP = (pi * Vector3<HugeNumber>.UnitX) + (pj * Vector3<HugeNumber>.UnitY) + (pk * Vector3<HugeNumber>.UnitZ);
        var perifocalQ = (qi * Vector3<HugeNumber>.UnitX) + (qj * Vector3<HugeNumber>.UnitY) + (qk * Vector3<HugeNumber>.UnitZ);

        var cosineTrueAnomaly = (HugeNumber)Math.Cos(trueAnomaly);
        var sineTrueAnomaly = (HugeNumber)Math.Sin(trueAnomaly);

        var longitudeOfPeriapsis = (double)(angleAscending + argumentPeriapsis);
        if (orbitingObject is Planetoid planetoid)
        {
            longitudeOfPeriapsis -= planetoid.AxialPrecession;
        }
        longitudeOfPeriapsis %= DoubleConstants.TwoPi;

        orbitingObject.Velocity = HugeNumber.Sqrt(standardGravitationalParameter / semiLatusRectum)
            * ((-sineTrueAnomaly * perifocalP) + (eccentricity * perifocalQ) + (cosineTrueAnomaly * perifocalQ));

        orbitingObject.Orbit = new Orbit(
            orbitedId,
            orbitedMass,
            orbitedPosition,
            barycenter,
            eccentricity,
            (double)inclination,
            longitudeOfPeriapsis,
            radius > semiMajorAxis
                ? (longitudeOfPeriapsis + Math.PI) % DoubleConstants.TwoPi
                : longitudeOfPeriapsis,
            HugeNumberConstants.TwoPi / period,
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
    /// Sets the orbit of the given <see cref="CosmicLocation"/> according to the given
    /// orbital parameters, and adjusts its position and velocity as necessary.
    /// </summary>
    /// <param name="orbitingObject">The celestial object which will be in orbit.</param>
    /// <param name="orbitedId">
    /// <para>
    /// The <see cref="IIdItem.Id"/> of the primary in the two-body system of the orbit.
    /// </para>
    /// <para>
    /// May be <see langword="null"/> if the primary of this orbit is a system rather than an object
    /// (e.g. the center of mass of a galaxy).
    /// </para>
    /// </param>
    /// <param name="orbitedMass">
    /// The mass of the object or system being orbited, in kg.
    /// </param>
    /// <param name="orbitedPosition">
    /// The position of the object or system being orbited at the epoch, as a vector in the same
    /// reference frame as <paramref name="orbitingObject"/>.
    /// </param>
    /// <param name="periapsis">
    /// The distance between the objects at the closest point in the orbit.
    /// </param>
    /// <param name="eccentricity">The degree to which the orbit is non-circular. The absolute
    /// value will be used (i.e. negative values are treated as positives).</param>
    /// <param name="inclination">
    /// The angle between the X-Z plane through the center of the object orbited, and the plane
    /// of the orbit, in radians. Values will be normalized between zero and π.
    /// </param>
    /// <param name="longitudeAscending">
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
    internal static void AssignOrbit(
        CosmicLocation orbitingObject,
        string? orbitedId,
        HugeNumber orbitedMass,
        Vector3<HugeNumber> orbitedPosition,
        HugeNumber periapsis,
        double eccentricity,
        double inclination,
        double longitudeAscending,
        double argPeriapsis,
        double trueAnomaly)
    {
        eccentricity = Math.Abs(eccentricity);

        while (inclination > Math.PI)
        {
            inclination -= Math.PI;
        }
        while (inclination < 0)
        {
            inclination += Math.PI;
        }

        while (longitudeAscending >= DoubleConstants.TwoPi)
        {
            longitudeAscending -= DoubleConstants.TwoPi;
        }
        while (longitudeAscending < 0)
        {
            longitudeAscending += DoubleConstants.TwoPi;
        }

        while (argPeriapsis >= DoubleConstants.TwoPi)
        {
            argPeriapsis -= DoubleConstants.TwoPi;
        }
        while (argPeriapsis < 0)
        {
            argPeriapsis += DoubleConstants.TwoPi;
        }

        while (trueAnomaly >= DoubleConstants.TwoPi)
        {
            trueAnomaly -= DoubleConstants.TwoPi;
        }
        while (trueAnomaly < 0)
        {
            trueAnomaly += DoubleConstants.TwoPi;
        }

        var standardGravitationalParameter = HugeNumberConstants.G * (orbitedMass + orbitingObject.Mass);

        var semiLatusRectum = periapsis * (1 + eccentricity);

        var eccentricitySquared = eccentricity * eccentricity;
        var eccentricityNumber = (HugeNumber)eccentricity;
        // For parabolic orbits, semi-major axis is undefined, and is set to the periapsis
        // instead.
        var semiMajorAxis = eccentricity == 1
            ? periapsis
            : semiLatusRectum / (1 - eccentricitySquared);

        // Calculate the perifocal vectors
        var cosineAngleAscending = Math.Cos(longitudeAscending);
        var sineAngleAscending = Math.Sin(longitudeAscending);
        var cosineArgPeriapsis = Math.Cos(argPeriapsis);
        var sineArgPeriapsis = Math.Sin(argPeriapsis);
        var cosineInclination = Math.Cos(inclination);
        var sineInclination = Math.Sin(inclination);

        var pi = (HugeNumber)((cosineAngleAscending * cosineArgPeriapsis) - (sineAngleAscending * cosineInclination * sineArgPeriapsis));
        var pj = (HugeNumber)((sineAngleAscending * cosineArgPeriapsis) + (cosineAngleAscending * cosineInclination * sineArgPeriapsis));
        var pk = (HugeNumber)(sineInclination * sineArgPeriapsis);

        var qi = (HugeNumber)(-(cosineAngleAscending * sineArgPeriapsis) - (sineAngleAscending * cosineInclination * cosineArgPeriapsis));
        var qj = (HugeNumber)(-(sineAngleAscending * sineArgPeriapsis) + (cosineAngleAscending * cosineInclination * cosineArgPeriapsis));
        var qk = (HugeNumber)(sineInclination * cosineArgPeriapsis);

        var perifocalP = (pi * Vector3<HugeNumber>.UnitX) + (pj * Vector3<HugeNumber>.UnitY) + (pk * Vector3<HugeNumber>.UnitZ);
        var perifocalQ = (qi * Vector3<HugeNumber>.UnitX) + (qj * Vector3<HugeNumber>.UnitY) + (qk * Vector3<HugeNumber>.UnitZ);

        var cosineTrueAnomaly = Math.Cos(trueAnomaly);
        var cosineTrueAnomalyNumber = (HugeNumber)cosineTrueAnomaly;
        var sineTrueAnomaly = Math.Sin(trueAnomaly);
        var sineTrueAnomalyNumber = (HugeNumber)sineTrueAnomaly;
        var radius = semiLatusRectum / (1 + (eccentricity * cosineTrueAnomaly));

        var longitudeOfPeriapsis = longitudeAscending + argPeriapsis;
        if (orbitingObject is Planetoid planetoid)
        {
            longitudeOfPeriapsis -= planetoid.AxialPrecession;
        }
        longitudeOfPeriapsis %= DoubleConstants.TwoPi;

        var r0 = (radius * cosineTrueAnomalyNumber * perifocalP) + (radius * sineTrueAnomalyNumber * perifocalQ);
        orbitingObject.Position = orbitedPosition + ((orbitedPosition - r0).Normalize() * semiMajorAxis);

        var relativePositionVector = orbitingObject.Position - orbitedPosition;
        var barycenter = orbitedPosition
            + (relativePositionVector.Normalize()
            * (semiMajorAxis / (HugeNumber.One + (orbitedMass / orbitingObject.Mass))));

        orbitingObject.Velocity = HugeNumber.Sqrt(standardGravitationalParameter / semiLatusRectum)
            * ((-sineTrueAnomalyNumber * perifocalP) + (eccentricityNumber * perifocalQ) + (cosineTrueAnomalyNumber * perifocalQ));

        var eccentricAnomaly = Math.Atan2(eccentricity + cosineTrueAnomaly, Math.Sqrt(1 - eccentricitySquared) * sineTrueAnomaly);
        var meanAnomaly = (HugeNumber)(eccentricAnomaly - (eccentricity * Math.Sin(eccentricAnomaly)));
        while (meanAnomaly < 0)
        {
            meanAnomaly += HugeNumberConstants.TwoPi;
        }
        var period = HugeNumberConstants.TwoPi * HugeNumber.Sqrt(semiMajorAxis.Cube() / standardGravitationalParameter);
        var meanMotion = period == HugeNumber.Zero ? HugeNumber.Zero : HugeNumberConstants.TwoPi / period;
        var epoch = Duration.FromSecondsFloatingPoint(meanMotion == HugeNumber.Zero ? HugeNumber.Zero : meanAnomaly / meanMotion);

        orbitingObject.Orbit = new Orbit(
            orbitedId,
            orbitedMass,
            orbitedPosition,
            barycenter,
            eccentricity,
            inclination,
            longitudeOfPeriapsis,
            (longitudeOfPeriapsis + (double)meanAnomaly) % DoubleConstants.TwoPi,
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
    /// Sets the orbit of the given <see cref="CosmicLocation"/> according to the given
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
    /// <param name="longitudeAscending">
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
    internal static void AssignOrbit(
        CosmicLocation orbitingObject,
        CosmicLocation orbitedObject,
        HugeNumber periapsis,
        double eccentricity,
        double inclination,
        double longitudeAscending,
        double argPeriapsis,
        double trueAnomaly)
        => AssignOrbit(
            orbitingObject,
            orbitedObject.Id,
            orbitedObject.Mass,
            orbitedObject.Position,
            periapsis,
            eccentricity,
            inclination,
            longitudeAscending,
            argPeriapsis,
            trueAnomaly);

    /// <summary>
    /// Sets the orbit of the given object based on the given <paramref name="eccentricity"/> and
    /// <paramref name="period"/>, and adjusts its position and velocity as necessary.
    /// </summary>
    /// <param name="orbitingObject">The celestial object which will be in orbit.</param>
    /// <param name="orbitedId">
    /// <para>
    /// The <see cref="IIdItem.Id"/> of the primary in the two-body system of the orbit.
    /// </para>
    /// <para>
    /// May be <see langword="null"/> if the primary of this orbit is a system rather than an object
    /// (e.g. the center of mass of a galaxy).
    /// </para>
    /// </param>
    /// <param name="orbitedMass">
    /// The mass of the object or system being orbited, in kg.
    /// </param>
    /// <param name="orbitedPosition">
    /// The position of the object or system being orbited at the epoch, as a vector in the same
    /// reference frame as <paramref name="orbitingObject"/>.
    /// </param>
    /// <param name="eccentricity">
    /// The degree to which the orbit is non-circular. The absolute value will be used (i.e.
    /// negative values are treated as positives).
    /// </param>
    /// <param name="period">
    /// The desired period of the orbit, in seconds.
    /// </param>
    /// <remarks>
    /// An inclination will be calculated from the current position, scaled in or out as needed to
    /// place that position on the desired orbit, and presumed to be the maximum inclination.
    /// </remarks>
    internal static void AssignOrbit(
        CosmicLocation orbitingObject,
        string? orbitedId,
        HugeNumber orbitedMass,
        Vector3<HugeNumber> orbitedPosition,
        double eccentricity,
        HugeNumber period)
    {
        eccentricity = Math.Abs(eccentricity);

        var standardGravitationalParameter = HugeNumberConstants.G * (orbitedMass + orbitingObject.Mass);

        var semiMajorAxis = ((period / HugeNumberConstants.TwoPi).Square() * standardGravitationalParameter).Cbrt();
        var semiLatusRectum = semiMajorAxis * (1 - (eccentricity * eccentricity));

        var trueAnomaly = Randomizer.Instance.NextDouble(DoubleConstants.TwoPi);

        var radius = semiLatusRectum / (1 + (eccentricity * Math.Cos(trueAnomaly)));

        var relativePositionVector = orbitingObject.Position - orbitedPosition;
        var barycenter = orbitedPosition
            + (relativePositionVector.Normalize()
            * (semiMajorAxis - radius));

        var r0 = barycenter
            + (relativePositionVector.Normalize()
            * radius);

        // The current position must be either the apoapsis or the periapsis,
        // since it was chosen as the reference point for the inclination.
        // Therefore, it is the periapsis if its distance from the orbited
        // body is less than the semi-major axis, and the apoapsis if not.
        var periapsis = radius <= semiMajorAxis
            ? r0.Length()
            : (Vector3<HugeNumber>.Normalize(new Vector3<HugeNumber>(-r0.X, -r0.Y, -r0.Z)) * (semiLatusRectum / (1 + eccentricity))).Length();
        // For parabolic orbits, semi-major axis is undefined, and is set to the periapsis instead.
        if (eccentricity == 1)
        {
            periapsis = semiMajorAxis;
        }

        var xz = new Vector3<HugeNumber>(r0.X, 0, r0.Z);
        var inclination = HugeNumber.Acos(HugeNumber.Sqrt(xz.X.Square() + xz.Z.Square()) / radius);
        var angleAscending = Vector3<HugeNumber>.UnitX.Angle(xz) - HugeNumberConstants.HalfPi;

        // If at periapsis now, this is the epoch;
        // if not, the epoch is half the period away.
        var epoch = radius > semiMajorAxis
            ? Duration.FromSecondsFloatingPoint(period / 2)
            : Duration.Zero;

        // Calculate the perifocal vectors
        var cosineAngleAscending = HugeNumber.Cos(angleAscending);
        var sineAngleAscending = HugeNumber.Sin(angleAscending);
        var sineInclination = HugeNumber.Sin(inclination);
        var cosineInclination = HugeNumber.Cos(inclination);

        var argumentPeriapsis = HugeNumber.Atan2(r0.Z / sineInclination, (r0.X * cosineAngleAscending) + (r0.Y * sineAngleAscending) - trueAnomaly);

        var cosineArgPeriapsis = HugeNumber.Cos(argumentPeriapsis);
        var sineArgPeriapsis = HugeNumber.Sin(argumentPeriapsis);

        var pi = (cosineAngleAscending * cosineArgPeriapsis) - (sineAngleAscending * cosineInclination * sineArgPeriapsis);
        var pj = (sineAngleAscending * cosineArgPeriapsis) + (cosineAngleAscending * cosineInclination * sineArgPeriapsis);
        var pk = sineInclination * sineArgPeriapsis;

        var qi = -(cosineAngleAscending * sineArgPeriapsis) - (sineAngleAscending * cosineInclination * cosineArgPeriapsis);
        var qj = -(sineAngleAscending * sineArgPeriapsis) + (cosineAngleAscending * cosineInclination * cosineArgPeriapsis);
        var qk = sineInclination * cosineArgPeriapsis;

        var perifocalP = (pi * Vector3<HugeNumber>.UnitX) + (pj * Vector3<HugeNumber>.UnitY) + (pk * Vector3<HugeNumber>.UnitZ);
        var perifocalQ = (qi * Vector3<HugeNumber>.UnitX) + (qj * Vector3<HugeNumber>.UnitY) + (qk * Vector3<HugeNumber>.UnitZ);

        var cosineTrueAnomaly = (HugeNumber)Math.Cos(trueAnomaly);
        var sineTrueAnomaly = (HugeNumber)Math.Sin(trueAnomaly);

        var longitudeOfPeriapsis = (double)(angleAscending + argumentPeriapsis);
        if (orbitingObject is Planetoid planetoid)
        {
            longitudeOfPeriapsis -= planetoid.AxialPrecession;
        }
        longitudeOfPeriapsis %= DoubleConstants.TwoPi;

        orbitingObject.Position = r0;

        orbitingObject.Velocity = HugeNumber.Sqrt(standardGravitationalParameter / semiLatusRectum)
            * ((-sineTrueAnomaly * perifocalP) + (eccentricity * perifocalQ) + (cosineTrueAnomaly * perifocalQ));

        orbitingObject.Orbit = new Orbit(
            orbitedId,
            orbitedMass,
            orbitedPosition,
            barycenter,
            eccentricity,
            (double)inclination,
            longitudeOfPeriapsis,
            radius > semiMajorAxis
                ? (longitudeOfPeriapsis + Math.PI) % DoubleConstants.TwoPi
                : longitudeOfPeriapsis,
            HugeNumberConstants.TwoPi / period,
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

    internal static HugeNumber GetHillSphereRadius(HugeNumber orbitingMass, HugeNumber orbitedMass, HugeNumber semiMajorAxis, double eccentricity)
        => semiMajorAxis * (1 - eccentricity) * (orbitingMass / (3 * orbitedMass)).Cbrt();

    internal static HugeNumber GetSemiMajorAxisForPeriod(HugeNumber orbitingMass, HugeNumber orbitedMass, HugeNumber period)
        => (HugeNumberConstants.G * (orbitingMass + orbitedMass) * period * period / (HugeNumberConstants.FourPi * HugeNumber.Pi)).Cbrt();

    internal HugeNumber GetHillSphereRadius(HugeNumber orbitingMass) => GetHillSphereRadius(orbitingMass, OrbitedMass, SemiMajorAxis, Eccentricity);

    /// <summary>
    /// Approximates the radius of the orbiting body's mutual Hill sphere with another
    /// orbiting body in orbit around the same primary, in meters.
    /// </summary>
    /// <remarks>
    /// Assumes the semimajor axis of both orbits is identical for the purposes of the
    /// calculation, which obviously would not be the case, but generates reasonably close
    /// estimates in the absence of actual values.
    /// </remarks>
    /// <param name="orbitingMass">The mass of the orbiting object, in kg.</param>
    /// <param name="otherMass">
    /// The mass of another celestial body presumed to be orbiting the same primary as this one.
    /// </param>
    /// <returns>The radius of the orbiting body's Hill sphere, in meters.</returns>
    internal HugeNumber GetMutualHillSphereRadius(HugeNumber orbitingMass, HugeNumber otherMass)
        => ((orbitingMass + otherMass) / (3 * OrbitedMass)).Cbrt() * SemiMajorAxis;

    internal HugeNumber GetSphereOfInfluenceRadius(HugeNumber orbitingMass) => SemiMajorAxis * HugeNumber.Pow(orbitingMass / OrbitedMass, new HugeNumber(2) / new HugeNumber(5));

    private static HugeNumber StumpffC(HugeNumber x)
    {
        if (x == HugeNumber.Zero)
        {
            return HugeNumberConstants.Half;
        }
        else if (x > 0)
        {
            return (HugeNumber.One - HugeNumber.Cos(HugeNumber.Sqrt(x))) / x;
        }
        else
        {
            return (HugeNumber.Cosh(HugeNumber.Sqrt(-x)) - HugeNumber.One) / -x;
        }
    }

    private static HugeNumber StumpffS(HugeNumber x)
    {
        if (x == HugeNumber.Zero)
        {
            return HugeNumber.One / new HugeNumber(6);
        }
        else if (x > 0)
        {
            var rootX = HugeNumber.Sqrt(x);
            return (rootX - HugeNumber.Sin(rootX)) / rootX.Cube();
        }
        else
        {
            var rootNegX = HugeNumber.Sqrt(-x);
            return (HugeNumber.Sinh(rootNegX) - rootNegX) / rootNegX.Cube();
        }
    }

    private HugeNumber GetUniversalVariableFormulaRatio(HugeNumber x, HugeNumber t, HugeNumber sqrtSGP, HugeNumber accel, HugeNumber f)
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
}
