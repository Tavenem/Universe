namespace WorldFoundry.Utilities.Science
{
    public class Constants
    {
        /// <summary>
        /// The gravitational constant, in SI base units.
        /// </summary>
        public const double G = GravitationalConstant;
        /// <summary>
        /// The gravitational constant, in SI base units.
        /// </summary>
        public const double GravitationalConstant = 6.67408e-11f;

        /// <summary>
        /// The Stefan–Boltzmann constant, in SI base units.
        /// </summary>
        public const double StefanBoltzmannConstant = 5.670367e-8f;

        /// <summary>
        /// Four times The Stefan–Boltzmann constant, in SI base units.
        /// </summary>
        public const double FourStefanBoltzmannConstant = 4.0 * StefanBoltzmannConstant;
    }
}
