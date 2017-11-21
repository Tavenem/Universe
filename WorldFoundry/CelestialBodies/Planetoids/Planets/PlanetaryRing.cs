namespace WorldFoundry.CelestialBodies.Planetoids.Planets
{
    /// <summary>
    /// Contains information about a planetary ring (usually one of a collection that makes up a ring system).
    /// </summary>
    public class PlanetaryRing
    {
        /// <summary>
        /// The inner radius of the <see cref="PlanetaryRing"/>.
        /// </summary>
        public float InnerRadius { get; set; }

        /// <summary>
        /// Indicates that the <see cref="PlanetaryRing"/> is icy (rather than rocky).
        /// </summary>
        public bool Icy { get; set; }

        /// <summary>
        /// The outer radius of the <see cref="PlanetaryRing"/>.
        /// </summary>
        public float OuterRadius { get; set; }

        /// <summary>
        /// Initializes a new instance of <see cref="PlanetaryRing"/>.
        /// </summary>
        public PlanetaryRing() { }

        /// <summary>
        /// Initializes a new instance of <see cref="PlanetaryRing"/> with the given parameters.
        /// </summary>
        public PlanetaryRing(bool icy, float innerRadius, float outerRadius)
        {
            Icy = icy;
            InnerRadius = innerRadius;
            OuterRadius = outerRadius;
        }
    }
}
