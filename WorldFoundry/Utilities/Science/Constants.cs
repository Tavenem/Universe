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
        /// The heat of vaporization of water, in SI base units.
        /// </summary>
        public const float HeatOfVaporization_Water = 2501000;
        /// <summary>
        /// The heat of vaporization of water, squared, in SI base units.
        /// </summary>
        public const float HeatOfVaporization_Water_Squared = HeatOfVaporization_Water * HeatOfVaporization_Water;

        /// <summary>
        /// The molar mass of air, in SI base units.
        /// </summary>
        public const float MolarMass_Air = 0.0289644f;

        /// <summary>
        /// The specific gas constant of dry air, in SI base units.
        /// </summary>
        public const float SpecificGasConstant_DryAir = 287;

        /// <summary>
        /// The ratio of the specific gas constants of dry air to water, in SI base units.
        /// </summary>
        public const float SpecificGasConstant_Ratio_DryAirToWater = 0.622f;

        /// <summary>
        /// The specific heat of dry air at constant pressure, in SI base units.
        /// </summary>
        public const float SpecificHeat_DryAir = 1003.5f;

        /// <summary>
        /// The specific heat multiplied by the specific gas constant of dry air at constant pressure, in SI base units.
        /// </summary>
        public const float SpecificHeatTimesSpecificGasConstant_DryAir = SpecificHeat_DryAir * SpecificGasConstant_DryAir;

        /// <summary>
        /// The standard atmospheric pressure.
        /// </summary>
        public const float StandardAtmosphericPressure = 101.325f;

        /// <summary>
        /// The Stefan–Boltzmann constant, in SI base units.
        /// </summary>
        public const double StefanBoltzmannConstant = 5.670367e-8;

        /// <summary>
        /// Four times The Stefan–Boltzmann constant, in SI base units.
        /// </summary>
        public const double FourStefanBoltzmannConstant = 4.0 * StefanBoltzmannConstant;

        /// <summary>
        /// The universal gas constant, in SI base units.
        /// </summary>
        public const float R = UniversalGasConstant;
        /// <summary>
        /// The universal gas constant, in SI base units.
        /// </summary>
        public const float UniversalGasConstant = 8.3144598f;
    }
}
