namespace WorldFoundry.Substances
{
    /// <summary>
    /// Represents a physical substance: a <see cref="Substances.Chemical"/> in a specific <see cref="Substances.Phase"/>.
    /// </summary>
    public class Substance
    {
        /// <summary>
        /// The <see cref="Substances.Chemical"/> which defines this substance's properties.
        /// </summary>
        public Chemical Chemical { get; private set; }

        /// <summary>
        /// The phase of this <see cref="Substance"/>.
        /// </summary>
        public Phase Phase { get; set; }

        /// <summary>
        /// Returns a string that represents the current <see cref="Substance"/>.
        /// </summary>
        /// <returns>A string that represents the current <see cref="Substance"/>.</returns>
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
