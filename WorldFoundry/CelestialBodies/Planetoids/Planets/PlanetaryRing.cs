namespace WorldFoundry.CelestialBodies.Planetoids.Planets
{
    /// <summary>
    /// Contains information about a planetary ring (usually one of a collection that makes up a ring system).
    /// </summary>
    public class PlanetaryRing
    {
        /// <summary>
        /// The inner radius of the <see cref="PlanetaryRing"/>, in m.
        /// </summary>
        public double InnerRadius { get; set; }

        /// <summary>
        /// Indicates that the <see cref="PlanetaryRing"/> is icy, rather than rocky.
        /// </summary>
        public bool Icy { get; set; }

        /// <summary>
        /// The outer radius of the <see cref="PlanetaryRing"/>, in m.
        /// </summary>
        public double OuterRadius { get; set; }

        /// <summary>
        /// Initializes a new instance of <see cref="PlanetaryRing"/>.
        /// </summary>
        public PlanetaryRing() { }

        /// <summary>
        /// Initializes a new instance of <see cref="PlanetaryRing"/> with the given parameters.
        /// </summary>
        /// <param name="icy">Whether the ring is icy, rather than rocky.</param>
        /// <param name="innerRadius">The inner radius of the ring, in m.</param>
        /// <param name="outerRadius">The outer radius of the ring, in m.</param>
        public PlanetaryRing(bool icy, double innerRadius, double outerRadius)
        {
            Icy = icy;
            InnerRadius = innerRadius;
            OuterRadius = outerRadius;
        }
    }
}
