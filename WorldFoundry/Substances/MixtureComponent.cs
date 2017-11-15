namespace WorldFoundry.Substances
{
    /// <summary>
    /// Describes a <see cref="Substance"/> in a <see cref="Mixture"/> in a particular proportion.
    /// </summary>
    public class MixtureComponent
    {
        /// <summary>
        /// The <see cref="Substances.Substance"/>.
        /// </summary>
        public Substance Substance { get; private set; }

        /// <summary>
        /// The proportion of this substance in the overall mixture (mass fraction).
        /// </summary>
        public float Proportion { get; set; }
    }
}
