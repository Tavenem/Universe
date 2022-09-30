namespace Tavenem.Universe.Space.Planetoids;

/// <summary>
/// Contains information about a planetary ring (usually one of a collection that makes up a ring system).
/// </summary>
public readonly struct PlanetaryRing : IEqualityOperators<PlanetaryRing, PlanetaryRing, bool>
{
    /// <summary>
    /// Indicates that the <see cref="PlanetaryRing"/> is icy, rather than rocky.
    /// </summary>
    public bool Icy { get; }

    /// <summary>
    /// The inner radius of the <see cref="PlanetaryRing"/>, in m.
    /// </summary>
    public HugeNumber InnerRadius { get; }

    /// <summary>
    /// The outer radius of the <see cref="PlanetaryRing"/>, in m.
    /// </summary>
    public HugeNumber OuterRadius { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="PlanetaryRing"/>.
    /// </summary>
    /// <param name="icy">Whether the ring is icy, rather than rocky.</param>
    /// <param name="innerRadius">The inner radius of the ring, in m.</param>
    /// <param name="outerRadius">The outer radius of the ring, in m.</param>
    [System.Text.Json.Serialization.JsonConstructor]
    public PlanetaryRing(bool icy, HugeNumber innerRadius, HugeNumber outerRadius)
    {
        Icy = icy;
        InnerRadius = innerRadius;
        OuterRadius = outerRadius;
    }

    /// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
    /// <param name="other">An object to compare with this object.</param>
    /// <returns>
    /// <see langword="true" /> if the current object is equal to the <paramref name="other" />
    /// parameter; otherwise, <see langword="false" />.
    /// </returns>
    public bool Equals(PlanetaryRing other) => Icy == other.Icy
        && InnerRadius.Equals(other.InnerRadius)
        && OuterRadius.Equals(other.OuterRadius);

    /// <summary>Indicates whether this instance and a specified object are equal.</summary>
    /// <param name="obj">The object to compare with the current instance.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="obj" /> and this instance are the same type
    /// and represent the same value; otherwise, <see langword="false" />.
    /// </returns>
    public override bool Equals(object? obj) => obj is PlanetaryRing ring && Equals(ring);

    /// <summary>Returns the hash code for this instance.</summary>
    /// <returns>A 32-bit signed integer that is the hash code for this instance.</returns>
    public override int GetHashCode() => HashCode.Combine(Icy, InnerRadius, OuterRadius);

    /// <summary>Indicates whether two objects are equal.</summary>
    /// <param name="left">The first object to compare.</param>
    /// <param name="right">The second object to compare.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="left"/> is equal to <paramref
    /// name="right"/>; otherwise, <see langword="false" />.
    /// </returns>
    public static bool operator ==(PlanetaryRing left, PlanetaryRing right) => left.Equals(right);

    /// <summary>Indicates whether two objects are unequal.</summary>
    /// <param name="left">The first object to compare.</param>
    /// <param name="right">The second object to compare.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="left"/> is not equal to <paramref
    /// name="right"/>; otherwise, <see langword="false" />.
    /// </returns>
    public static bool operator !=(PlanetaryRing left, PlanetaryRing right) => !(left == right);
}
