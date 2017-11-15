namespace WorldFoundry.Substances
{
    /// <summary>
    /// Indicates the requirements for a particular substance in a mixture.
    /// </summary>
    public class SubstanceRequirement
    {
        /// <summary>
        /// The <see cref="Substances.Chemical"/> required.
        /// </summary>
        public Chemical Chemical { get; internal set; }

        /// <summary>
        /// The phase required.
        /// </summary>
        public Phase Phase { get; internal set; }

        /// <summary>
        /// The minimum proportion of this substance in the overall mixture required (mass fraction).
        /// </summary>
        public float MinimumProportion { get; internal set; }

        /// <summary>
        /// The maximum proportion of this substance in the overall mixture allowed (mass fraction).
        /// </summary>
        public float? MaximumProportion { get; internal set; }
    }
}
