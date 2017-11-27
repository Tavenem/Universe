namespace WorldFoundry.Substances
{
    /// <summary>
    /// Indicates the requirements for a particular component in a mixture.
    /// </summary>
    public class ComponentRequirement
    {
        /// <summary>
        /// The <see cref="Substances.Chemical"/> required.
        /// </summary>
        public Chemical Chemical { get; internal set; }

        /// <summary>
        /// The <see cref="Substances.Phase"/> required.
        /// </summary>
        public Phase Phase { get; internal set; }

        /// <summary>
        /// The minimum proportion of this component in the overall mixture required (mass fraction).
        /// </summary>
        public float MinimumProportion { get; internal set; }

        /// <summary>
        /// The maximum proportion of this component in the overall mixture allowed (mass fraction).
        /// </summary>
        public float? MaximumProportion { get; internal set; }
    }
}
