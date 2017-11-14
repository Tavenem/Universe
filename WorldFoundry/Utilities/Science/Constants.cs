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
        public const double GravitationalConstant = 6.67408e-11;
        /// <summary>
        /// Twice the gravitational constant, in SI base units.
        /// </summary>
        public const double TwoG = 2.0 * GravitationalConstant;

        /// <summary>
        /// The Stefan–Boltzmann constant, in SI base units.
        /// </summary>
        public const double StefanBoltzmannConstant = 5.670367e-8;

        /// <summary>
        /// Four times The Stefan–Boltzmann constant, in SI base units.
        /// </summary>
        public const double FourStefanBoltzmannConstant = 4.0 * StefanBoltzmannConstant;
    }
}
