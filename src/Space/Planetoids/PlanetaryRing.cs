namespace Tavenem.Universe.Space.Planetoids;

/// <summary>
/// Contains information about a planetary ring (usually one of a collection that makes up a ring system).
/// </summary>
/// <param name="Icy">Whether the ring is icy, rather than rocky.</param>
/// <param name="InnerRadius">The inner radius of the ring, in m.</param>
/// <param name="OuterRadius">The outer radius of the ring, in m.</param>
public readonly record struct PlanetaryRing(
    bool Icy,
    HugeNumber InnerRadius,
    HugeNumber OuterRadius);
