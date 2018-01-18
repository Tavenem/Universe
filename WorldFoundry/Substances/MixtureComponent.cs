namespace WorldFoundry.Substances
{
    /// <summary>
    /// Describes a particular <see cref="Substances.Phase"/> of a <see cref="Substances.Chemical"/>
    /// in a <see cref="Mixture"/> in a particular proportion.
    /// </summary>
    public class MixtureComponent : DataItem
    {
        /// <summary>
        /// The <see cref="Substances.Chemical"/>.
        /// </summary>
        public Chemical Chemical { get; internal set; }

        /// <summary>
        /// The <see cref="Substances.Phase"/>.
        /// </summary>
        public Phase Phase { get; internal set; }

        /// <summary>
        /// The proportion of this component in the overall mixture (mass fraction).
        /// </summary>
        public float Proportion { get; set; }

        /// <summary>
        /// Gets a shallow copy of this <see cref="MixtureComponent"/>.
        /// </summary>
        /// <returns>A shallow copy of this <see cref="MixtureComponent"/>.</returns>
        public MixtureComponent GetShallowCopy()
            => new MixtureComponent
            {
                Chemical = Chemical,
                Phase = Phase,
                Proportion = Proportion,
            };

        /// <summary>
        /// Returns a string that represents the current <see cref="MixtureComponent"/>.
        /// </summary>
        /// <returns>A string that represents the current <see cref="MixtureComponent"/>.</returns>
        public override string ToString()
        {
            switch (Phase)
            {
                case Phase.Solid:
                    return $"{Chemical.Name} Ice";
                case Phase.Liquid:
                    return $"Liquid {Chemical.Name}";
                case Phase.Gas:
                    return $"{Chemical.Name} Vapor";
                case Phase.Plasma:
                    return $"{Chemical.Name} Plasma";
                default:
                    return Chemical.Name;
            }
        }
    }
}
