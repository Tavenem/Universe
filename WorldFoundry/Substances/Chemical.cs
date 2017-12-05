using System;

namespace WorldFoundry.Substances
{
    /// <summary>
    /// Describes a chemical's properties.
    /// </summary>
    public class Chemical
    {
        /// <summary>
        /// The first Antoine coefficient which can be used to determine the vapor
        /// pressure of this substance.
        /// </summary>
        public float AntoineCoefficientA { get; private set; }

        /// <summary>
        /// The second Antoine coefficient which can be used to determine the vapor
        /// pressure of this substance.
        /// </summary>
        public float AntoineCoefficientB { get; private set; }

        /// <summary>
        /// The third Antoine coefficient which can be used to determine the vapor
        /// pressure of this substance.
        /// </summary>
        public float AntoineCoefficientC { get; private set; }

        /// <summary>
        /// The upper limit of the Antoine coefficients' accuracy for this <see cref="Chemical"/>.
        /// It is presumed reasonable to assume that the <see cref="Chemical"/> always vaporizes
        /// above this temperature.
        /// </summary>
        public float AntoineMaximumTemperature { get; private set; }

        /// <summary>
        /// The lower limit of the Antoine coefficients' accuracy for this <see cref="Chemical"/>.
        /// It is presumed reasonable to assume that the <see cref="Chemical"/> always condenses
        /// below this temperature.
        /// </summary>
        public float AntoineMinimumTemperature { get; private set; }

        /// <summary>
        /// Indicates the greenhouse potential of this substance (only applies to gases).
        /// </summary>
        public int GreenhousePotential { get; private set; }

        /// <summary>
        /// Indicates whether this substance is able to burn.
        /// </summary>
        public bool IsFlammable { get; private set; }

        /// <summary>
        /// Indicates whether this substance conducts electricity.
        /// </summary>
        public bool IsConductive { get; private set; }

        /// <summary>
        /// The melting point of this substance at 100 kPa, in K.
        /// </summary>
        public float MeltingPoint { get; private set; }

        /// <summary>
        /// The name of this substance.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Initializes a new instance of <see cref="Chemical"/>.
        /// </summary>
        public Chemical() { }

        /// <summary>
        /// Initializes a new instance of <see cref="Chemical"/> with the given values.
        /// </summary>
        public Chemical(string name) => Name = name;

        /// <summary>
        /// Calculates the <see cref="Phase"/> of this <see cref="Chemical"/> under the given
        /// conditions of temperature and pressure.
        /// </summary>
        /// <param name="temperature">The temperature of the <see cref="Chemical"/>, in K.</param>
        /// <param name="pressure">The pressure applied to the <see cref="Chemical"/>, in kPa.</param>
        public Phase CalculatePhase(float temperature, float pressure)
        {
            if (temperature < MeltingPoint)
            {
                return Phase.Solid;
            }
            else if (pressure > CalculateVaporPressure(temperature))
            {
                return Phase.Gas;
            }
            else
            {
                return Phase.Liquid;
            }
        }

        /// <summary>
        /// Calculates the vapor pressure of this <see cref="Chemical"/>, in kPa.
        /// </summary>
        /// <param name="temperature">The temperature of the <see cref="Chemical"/>, in K.</param>
        /// <remarks>
        /// Uses Antoine's equation. If Antoine coefficients have not been explicitly set for this
        /// <see cref="Chemical"/>, the return value will always be 100.
        /// </remarks>
        public float CalculateVaporPressure(float temperature)
        {
            if (temperature > AntoineMaximumTemperature)
            {
                return float.NegativeInfinity;
            }
            else if (temperature < AntoineMinimumTemperature)
            {
                return float.PositiveInfinity;
            }
            else
            {
                return (float)(Math.Pow(10, AntoineCoefficientA - (AntoineCoefficientB / (AntoineCoefficientC + temperature))) * 100);
            }
        }

        #region Static presets

        #region Atmospheric substances

        public static Chemical Ammonia = new Chemical("Ammonia")
        {
            MeltingPoint = 195.42f,
            IsFlammable = true,
            AntoineCoefficientA = 3.18757f,
            AntoineCoefficientB = 506.713f,
            AntoineCoefficientC = -80.78f,
            AntoineMaximumTemperature = 239.6f,
            AntoineMinimumTemperature = 164.0f,
        };

        public static Chemical AmmoniumHydrosulfide = new Chemical("Ammonium Hydrosulfide")
        {
            MeltingPoint = 329.8f,
            IsFlammable = true,
            AntoineCoefficientA = 6.09146f,
            AntoineCoefficientB = 1598.378f,
            AntoineCoefficientC = -43.805f,
            AntoineMaximumTemperature = 306.4f,
            AntoineMinimumTemperature = 222.1f,
        };

        public static Chemical Argon = new Chemical("Argon")
        {
            MeltingPoint = 83.8f,
            AntoineCoefficientA = 3.29555f,
            AntoineCoefficientB = 215.24f,
            AntoineCoefficientC = -22.233f,
            AntoineMaximumTemperature = 150.72f,
            AntoineMinimumTemperature = 83.78f,
        };

        public static Chemical CarbonMonoxide = new Chemical("Carbon Monoxide")
        {
            MeltingPoint = 68.15f,
            IsFlammable = true,
            AntoineCoefficientA = 3.81912f,
            AntoineCoefficientB = 291.743f,
            AntoineCoefficientC = -5.151f,
            AntoineMaximumTemperature = 88.1f,
            AntoineMinimumTemperature = 68.2f,
        };

        public static Chemical CarbonDioxide = new Chemical("Carbon Dioxide")
        {
            MeltingPoint = 195.15F,
            GreenhousePotential = 1,
            AntoineCoefficientA = 6.93556f,
            AntoineCoefficientB = 1347.786f,
            AntoineCoefficientC = -0.15f,
            AntoineMaximumTemperature = 203.3f,
            AntoineMinimumTemperature = 153.2f,
        };

        public static Chemical Ethane = new Chemical("Ethane")
        {
            MeltingPoint = 90.15f,
            IsFlammable = true,
            AntoineCoefficientA = 3.95405f,
            AntoineCoefficientB = 663.72f,
            AntoineCoefficientC = -16.469f,
            AntoineMaximumTemperature = 198.2f,
            AntoineMinimumTemperature = 130.4f,
        };

        public static Chemical Helium = new Chemical("Helium")
        {
            MeltingPoint = 0.95f,
            AntoineMaximumTemperature = 0,
        };

        public static Chemical Hydrogen = new Chemical("Hydrogen")
        {
            MeltingPoint = 14.01f,
            IsFlammable = true,
            AntoineCoefficientA = 3.54314f,
            AntoineCoefficientB = 99.395f,
            AntoineCoefficientC = 7.726f,
            AntoineMaximumTemperature = 32.27f,
            AntoineMinimumTemperature = 21.01f,
        };
        public static Chemical Hydrogen_Metallic = new Chemical("Metallic Hydrogen")
        {
            MeltingPoint = 14.01f,
            IsConductive = true,
            AntoineCoefficientA = 3.54314f,
            AntoineCoefficientB = 99.395f,
            AntoineCoefficientC = 7.726f,
            AntoineMaximumTemperature = 32.27f,
            AntoineMinimumTemperature = 21.01f,
        };

        public static Chemical HydrogenSulfide = new Chemical("Hydrogen Sulfide")
        {
            MeltingPoint = 191.15f,
            IsFlammable = true,
            AntoineCoefficientA = 4.52887f,
            AntoineCoefficientB = 958.587f,
            AntoineCoefficientC = -0.539f,
            AntoineMaximumTemperature = 349.5f,
            AntoineMinimumTemperature = 212.8f,
        };

        public static Chemical Krypton = new Chemical("Krypton")
        {
            MeltingPoint = 115.75f,
            AntoineCoefficientA = 4.2064f,
            AntoineCoefficientB = 539.004f,
            AntoineCoefficientC = 8.855f,
            AntoineMaximumTemperature = 208.0f,
            AntoineMinimumTemperature = 126.68f,
        };

        public static Chemical Methane = new Chemical("Methane")
        {
            MeltingPoint = 91.15f,
            IsFlammable = true,
            GreenhousePotential = 34,
            AntoineCoefficientA = 3.7687f,
            AntoineCoefficientB = 395.744f,
            AntoineCoefficientC = -6.469f,
            AntoineMaximumTemperature = 120.6f,
            AntoineMinimumTemperature = 90.7f,
        };

        public static Chemical Neon = new Chemical("Neon")
        {
            MeltingPoint = 24.55f,
            AntoineCoefficientA = 3.75641f,
            AntoineCoefficientB = 95.599f,
            AntoineCoefficientC = -1.503f,
            AntoineMaximumTemperature = 27.0f,
            AntoineMinimumTemperature = 15.9f,
        };

        public static Chemical Nitrogen = new Chemical("Nitrogen")
        {
            MeltingPoint = 63.15f,
            AntoineCoefficientA = 3.61947f,
            AntoineCoefficientB = 255.68f,
            AntoineCoefficientC = -6.6f,
            AntoineMaximumTemperature = 83.7f,
            AntoineMinimumTemperature = 63.2f,
        };

        // Oxygen is not really flammable: it's an oxidizer; but the difference in practice is considered unimportant for this library's purposes.
        public static Chemical Oxygen = new Chemical("Oxygen")
        {
            MeltingPoint = 54.36f,
            IsFlammable = true,
            AntoineCoefficientA = 3.81634f,
            AntoineCoefficientB = 319.01f,
            AntoineCoefficientC = -6.453f,
            AntoineMaximumTemperature = 97.2f,
            AntoineMinimumTemperature = 62.6f,
        };

        // Like oxygen, ozone is not really flammable, but it's an even stronger oxidizer.
        public static Chemical Ozone = new Chemical("Ozone")
        {
            MeltingPoint = 81.15f,
            IsFlammable = true,
            AntoineCoefficientA = 4.23637f,
            AntoineCoefficientB = 712.487f,
            AntoineCoefficientC = 6.982f,
            AntoineMaximumTemperature = 162.0f,
            AntoineMinimumTemperature = 92.8f,
        };

        public static Chemical Phosphine = new Chemical("Phosphine")
        {
            MeltingPoint = 140.35f,
            IsFlammable = true,
            AntoineCoefficientA = 4.02591f,
            AntoineCoefficientB = 702.651f,
            AntoineCoefficientC = -11.065f,
            AntoineMaximumTemperature = 185.7f,
            AntoineMinimumTemperature = 143.8f,
        };

        public static Chemical SulphurDioxide = new Chemical("Sulphur Dioxide")
        {
            MeltingPoint = 202.15f,
            AntoineCoefficientA = 4.40718f,
            AntoineCoefficientB = 999.90f,
            AntoineCoefficientC = -35.96f,
            AntoineMaximumTemperature = 279.5f,
            AntoineMinimumTemperature = 210.0f,
        };

        public static Chemical Xenon = new Chemical("Xenon")
        {
            MeltingPoint = 161.35f,
            AntoineCoefficientA = 3.80675f,
            AntoineCoefficientB = 577.661f,
            AntoineCoefficientC = -13.0f,
            AntoineMaximumTemperature = 184.70f,
            AntoineMinimumTemperature = 161.70f,
        };

        #endregion // Atmospheric substances

        public static Chemical Water = new Chemical("Water")
        {
            MeltingPoint = 273.15f,
            IsConductive = true,
            GreenhousePotential = 1,
            AntoineCoefficientA = 4.6543f,
            AntoineCoefficientB = 1435.264f,
            AntoineCoefficientC = -64.848f,
            AntoineMaximumTemperature = 373.0f,
            AntoineMinimumTemperature = 255.9f,
        };
        public static Chemical Water_Salt = new Chemical("Salt Water")
        {
            MeltingPoint = 271.35f,
            IsConductive = true,
            GreenhousePotential = 1,
            AntoineCoefficientA = 4.6543f,
            AntoineCoefficientB = 1435.264f,
            AntoineCoefficientC = -62.848f,
            AntoineMaximumTemperature = 373.0f,
            AntoineMinimumTemperature = 255.9f,
        };

        #region Rock

        public static Chemical Clay = new Chemical("Clay")
        {
            MeltingPoint = 1523.15f,
            AntoineMinimumTemperature = float.PositiveInfinity,
        };
        public static Chemical Dirt = new Chemical("Dirt")
        {
            MeltingPoint = 2033.15f,
            AntoineMinimumTemperature = float.PositiveInfinity,
        };
        public static Chemical Dust = new Chemical("Dust")
        {
            MeltingPoint = 2033.15f,
            AntoineMinimumTemperature = float.PositiveInfinity,
        };
        public static Chemical Mud = new Chemical("Mud")
        {
            MeltingPoint = 273.15f,
            AntoineCoefficientA = 4.6543f,
            AntoineCoefficientB = 1435.264f,
            AntoineCoefficientC = -64.848f,
            AntoineMaximumTemperature = 373.0f,
            AntoineMinimumTemperature = 255.9f,
        };

        public static Chemical Rock = new Chemical("Rock")
        {
            MeltingPoint = 1473.15f,
            AntoineMinimumTemperature = float.PositiveInfinity,
        };

        #endregion // Rock

        #region Metals

        public static Chemical Aluminum = new Chemical("Aluminum")
        {
            MeltingPoint = 933.45f,
            IsConductive = true,
            AntoineCoefficientA = 5.73623f,
            AntoineCoefficientB = 13204.109f,
            AntoineCoefficientC = -24.306f,
            AntoineMaximumTemperature = 2329.0f,
            AntoineMinimumTemperature = 1557.0f,
        };

        public static Chemical Copper = new Chemical("Copper")
        {
            MeltingPoint = 1357.95f,
            IsConductive = true,
            // Antoine coefficient data unavailable.
            // The coefficients of gold were substituted and C adjusted by an offset equal
            // to the offset of their boiling points at STP.
            AntoineCoefficientA = 5.46951f,
            AntoineCoefficientB = 17292.476f,
            AntoineCoefficientC = -206.128f,
            AntoineMaximumTemperature = 3239.0f,
            AntoineMinimumTemperature = 2142.0f,
        };

        public static Chemical Gold = new Chemical("Gold")
        {
            MeltingPoint = 1337.15f,
            IsConductive = true,
            AntoineCoefficientA = 5.46951f,
            AntoineCoefficientB = 17292.476f,
            AntoineCoefficientC = -70.978f,
            AntoineMaximumTemperature = 3239.0f,
            AntoineMinimumTemperature = 2142.0f,
        };

        public static Chemical Iron = new Chemical("Iron")
        {
            MeltingPoint = 1811.15f,
            IsConductive = true,
            // Antoine coefficient data unavailable.
            // The coefficients of nickel were substituted and C adjusted by an offset equal
            // to the offset of their boiling points at STP.
            AntoineCoefficientA = 5.98183f,
            AntoineCoefficientB = 16808.435f,
            AntoineCoefficientC = -137.717f,
            AntoineMaximumTemperature = 3005.0f,
            AntoineMinimumTemperature = 2083.0f,
        };

        public static Chemical Nickel = new Chemical("Nickel")
        {
            MeltingPoint = 1728.15f,
            IsConductive = true,
            AntoineCoefficientA = 5.98183f,
            AntoineCoefficientB = 16808.435f,
            AntoineCoefficientC = -188.717f,
            AntoineMaximumTemperature = 3005.0f,
            AntoineMinimumTemperature = 2083.0f,
        };

        public static Chemical Platinum = new Chemical("Platinum")
        {
            MeltingPoint = 2041.15f,
            IsConductive = true,
            AntoineCoefficientA = 4.80688f,
            AntoineCoefficientB = 21519.696f,
            AntoineCoefficientC = -200.689f,
            AntoineMaximumTemperature = 4680.0f,
            AntoineMinimumTemperature = 3003.0f,
        };

        public static Chemical Silver = new Chemical("Silver")
        {
            MeltingPoint = 1234.95f,
            IsConductive = true,
            AntoineCoefficientA = 1.95303f,
            AntoineCoefficientB = 2505.533f,
            AntoineCoefficientC = -1194.947f,
            AntoineMaximumTemperature = 2425.0f,
            AntoineMinimumTemperature = 1823.0f,
        };

        public static Chemical Steel = new Chemical("Steel")
        {
            MeltingPoint = 1643.15f,
            IsConductive = true,
            // Antoine coefficient data unavailable.
            // The coefficients of nickel were substituted and C adjusted by an offset equal
            // to the offset of their boiling points at STP.
            AntoineCoefficientA = 5.98183f,
            AntoineCoefficientB = 16808.435f,
            AntoineCoefficientC = -137.717f,
            AntoineMaximumTemperature = 3005.0f,
            AntoineMinimumTemperature = 2083.0f,
        };

        public static Chemical Titanium = new Chemical("Titanium")
        {
            MeltingPoint = 1941.15f,
            IsConductive = true,
            // Antoine coefficient data unavailable.
            // The coefficients of nickel were substituted and C adjusted by an offset equal
            // to the offset of their boiling points at STP.
            AntoineCoefficientA = 5.98183f,
            AntoineCoefficientB = 16808.435f,
            AntoineCoefficientC = -562.717f,
            AntoineMaximumTemperature = 3005.0f,
            AntoineMinimumTemperature = 2083.0f,
        };

        #endregion // Metals

        #region Gems

        public static Chemical Diamond = new Chemical("Diamond")
        {
            MeltingPoint = 3915.0f,
            AntoineMinimumTemperature = float.NegativeInfinity, // sublimates
        };
        public static Chemical Emerald = new Chemical("Emerald")
        {
            MeltingPoint = 2570.0f,
            AntoineMinimumTemperature = float.PositiveInfinity,
        };
        public static Chemical Ruby = new Chemical("Ruby")
        {
            MeltingPoint = 2323.15f,
            AntoineMinimumTemperature = float.PositiveInfinity,
        };
        public static Chemical Sapphire = new Chemical("Sapphire")
        {
            MeltingPoint = 2313.15f,
            AntoineMinimumTemperature = float.PositiveInfinity,
        };
        public static Chemical Topaz = new Chemical("Topaz")
        {
            MeltingPoint = 688.45f,
            AntoineMinimumTemperature = float.NegativeInfinity, // sublimates
        };

        #endregion // Gems

        #endregion // Static presets
    }
}
