namespace Tavenem.Universe;

/// <summary>
/// A collection of precalculated operations involving scientific constants.
/// </summary>
public static class Constants
{
    /// <summary>
    /// The specific heat multiplied by the specific gas constant of dry air at constant pressure, in SI base units.
    /// </summary>
    public const double CpTimesRSpecificDryAir = DoubleConstants.CpDryAir * DoubleConstants.RSpecificDryAir;

    /// <summary>
    /// The heat of vaporization of water, squared, in SI base units.
    /// </summary>
    public const double DeltaHvapWaterSquared = (double)DoubleConstants.DeltaHvapWater * DoubleConstants.DeltaHvapWater;

    /// <summary>
    /// The ratio of the specific gas constants of dry air to water, in SI base units.
    /// </summary>
    public const double RSpecificRatioOfDryAirToWater = DoubleConstants.RSpecificDryAir / DoubleConstants.RSpecificWater;
}
