namespace Tavenem.Universe.Space;

/// <summary>
/// The parameters which describe an orbit.
/// </summary>
/// <param name="OrbitedMass">
/// The mass of the object or system being orbited, in kg.
/// </param>
/// <param name="OrbitedPosition">
/// The position of the object or system being orbited at the epoch, as a vector in the same
/// reference frame as the orbiting object.
/// </param>
/// <param name="Periapsis">
/// The distance between the objects at the closest point in the orbit.
/// </param>
/// <param name="Eccentricity">The degree to which the orbit is non-circular. The absolute
/// value will be used (i.e. negative values are treated as positives).</param>
/// <param name="Inclination">
/// The angle between the X-Z plane through the center of the object orbited, and the plane
/// of the orbit, in radians. Values will be normalized between zero and π.
/// </param>
/// <param name="AngleAscending">
/// The angle between the X-axis and the plane of the orbit (at the intersection where the
/// orbit is rising, in radians). Values will be normalized between zero and 2π.
/// </param>
/// <param name="ArgumentOfPeriapsis">
/// The angle between the intersection of the X-Z plane through the center of the object
/// orbited and the orbital plane, and the periapsis, in radians. Values will be normalized
/// between zero and 2π
/// </param>
/// <param name="TrueAnomaly">
/// The angle between periapsis and the current position of this object, from the center of
/// the object orbited, in radians. Values will be normalized between zero and 2π
/// </param>
/// <param name="Circular">
/// If true, all other parameters are ignored, and a circular orbit will be set based on the
/// orbiting object's position.
/// </param>
/// <param name="FromEccentricity">
/// If true, all parameters other than eccentricity are ignored, and an orbit will
/// be set based on the orbiting object's position.
/// </param>
public readonly record struct OrbitalParameters(
    HugeNumber OrbitedMass,
    Vector3<HugeNumber> OrbitedPosition,
    HugeNumber Periapsis,
    double Eccentricity,
    double Inclination,
    double AngleAscending,
    double ArgumentOfPeriapsis,
    double TrueAnomaly,
    bool Circular = false,
    bool FromEccentricity = false)
{
    /// <summary>
    /// Gets an <see cref="OrbitalParameters"/> instance which defines a circular orbit.
    /// </summary>
    /// <param name="orbitedMass">
    /// The mass of the object or system being orbited, in kg.
    /// </param>
    /// <param name="orbitedPosition">
    /// The position of the object or system being orbited at the epoch, as a vector in the same
    /// reference frame as the orbiting object.
    /// </param>
    public static OrbitalParameters GetCircular(HugeNumber orbitedMass, Vector3<HugeNumber> orbitedPosition)
        => new(
            orbitedMass,
            orbitedPosition,
            HugeNumber.Zero,
            0, 0, 0, 0, 0,
            true,
            false);

    /// <summary>
    /// Gets an <see cref="OrbitalParameters"/> instance which specifies that an orbit is to be
    /// determined based on eccentricity only.
    /// </summary>
    /// <param name="orbitedMass">
    /// The mass of the object or system being orbited, in kg.
    /// </param>
    /// <param name="orbitedPosition">
    /// The position of the object or system being orbited at the epoch, as a vector in the same
    /// reference frame as the orbiting object.
    /// </param>
    /// <param name="eccentricity">
    /// The eccentricity of this orbit.
    /// </param>
    public static OrbitalParameters GetFromEccentricity(HugeNumber orbitedMass, Vector3<HugeNumber> orbitedPosition, double eccentricity)
        => new(
            orbitedMass,
            orbitedPosition,
            HugeNumber.Zero,
            eccentricity,
            0, 0, 0, 0,
            false,
            true);
}
