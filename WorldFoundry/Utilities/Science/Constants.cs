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
        public const float HeatOfVaporizationOFWater = 2501000;
        /// <summary>
        /// The heat of vaporization of water, squared, in SI base units.
        /// </summary>
        public const float HeatOfVaporizationOfWaterSquared = HeatOfVaporizationOFWater * HeatOfVaporizationOFWater;

        /// <summary>
        /// The molar mass of air, in SI base units.
        /// </summary>
        public const float MolarMassOfAir = 0.0289644f;

        /// <summary>
        /// The molar mass of air divided by the universal gas constant, in SI base units.
        /// </summary>
        public const float MolarMassOfAirDivUniversalGasConstant = MolarMassOfAir / UniversalGasConstant;

        /// <summary>
        /// The specific gas constant of dry air, in SI base units.
        /// </summary>
        public const float SpecificGasConstantOfDryAir = 287;

        /// <summary>
        /// The specific gas constant divided by the specific heat of dry air at constant pressure, in SI base units.
        /// </summary>
        public const float SpecificGasConstantDivSpecificHeatOfDryAir = SpecificGasConstantOfDryAir / SpecificHeatOfDryAir;

        /// <summary>
        /// The specific gas constant of water, in SI base units.
        /// </summary>
        public const float SpecificGasConstantOfWater = 461.5f;

        /// <summary>
        /// The ratio of the specific gas constants of dry air to water, in SI base units.
        /// </summary>
        public const float SpecificGasConstantRatioOfDryAirToWater = SpecificGasConstantOfDryAir / SpecificGasConstantOfWater;

        /// <summary>
        /// The specific heat of dry air at constant pressure, in SI base units.
        /// </summary>
        public const float SpecificHeatOfDryAir = 1003.5f;

        /// <summary>
        /// The specific heat multiplied by the specific gas constant of dry air at constant pressure, in SI base units.
        /// </summary>
        public const float SpecificHeatTimesSpecificGasConstant_DryAir = SpecificHeatOfDryAir * SpecificGasConstantOfDryAir;

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
