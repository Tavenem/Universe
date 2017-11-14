using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

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
        public float AntoineCoefficientA { get; set; }

        /// <summary>
        /// The second Antoine coefficient which can be used to determine the vapor
        /// pressure of this substance.
        /// </summary>
        public float AntoineCoefficientB { get; set; }

        /// <summary>
        /// The third Antoine coefficient which can be used to determine the vapor
        /// pressure of this substance.
        /// </summary>
        public float AntoineCoefficientC { get; set; }

        /// <summary>
        /// The upper limit of the Antoine coefficients' accuracy for this <see cref="Chemical"/>.
        /// It is presumed reasonable to assume that the <see cref="Chemical"/> always vaporizes
        /// above this temperature.
        /// </summary>
        public float AntoineMaximumTemperature { get; set; }

        /// <summary>
        /// The lower limit of the Antoine coefficients' accuracy for this <see cref="Chemical"/>.
        /// It is presumed reasonable to assume that the <see cref="Chemical"/> always condenses
        /// below this temperature.
        /// </summary>
        public float AntoineMinimumTemperature { get; set; }

        /// <summary>
        /// Indicates the greenhouse potential of this substance (only applies to gases).
        /// </summary>
        public int GreenhousePotential { get; set; }

        /// <summary>
        /// Indicates whether this substance is able to burn.
        /// </summary>
        public bool IsFlammable { get; set; }

        /// <summary>
        /// Indicates whether this substance conducts electricity.
        /// </summary>
        public bool IsConductive { get; set; }

        /// <summary>
        /// The melting point of this substance at 100 kPa (in K).
        /// </summary>
        public float MeltingPoint { get; set; }

        /// <summary>
        /// The name of this substance.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Initializes a new instance of <see cref="Chemical"/>.
        /// </summary>
        public Chemical() { }

        /// <summary>
        /// Initializes a new instance of <see cref="Chemical"/> with the given values.
        /// </summary>
        public Chemical(string name) => Name = name;

        /// <summary>
        /// Calculates the vapor pressure of this <see cref="Chemical"/> (in kPa).
        /// </summary>
        /// <param name="temp">The temperature of the <see cref="Chemical"/>, in K.</param>
        /// <remarks>
        /// Uses Antoine's equation. If Antoine coefficients have not been explicitly set for this
        /// <see cref="Chemical"/>, the return value will always be 100.
        /// </remarks>
        public float CalculateVaporPressure(float temp)
        {
            if (temp > AntoineMaximumTemperature)
            {
                return float.NegativeInfinity;
            }
            else if (temp < AntoineMinimumTemperature)
            {
                return float.PositiveInfinity;
            }
            else
            {
                return (float)(Math.Pow(10, AntoineCoefficientA - (AntoineCoefficientB / (AntoineCoefficientC + temp))) * 100);
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
        public static Chemical HydrogenMetallic = new Chemical("Metallic Hydrogen")
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
            Phase = Phase.Gas,
            MeltingPoint = 191.15F,
            IsFlammable = true,
            AntoineCoefficients = new float[3] { 4.52887F, 958.587F, 272.611F },
            AntoineMaximumTemperature = 349.5F,
            AntoineMinimumTemperature = 212.8F
        };

        public static Chemical Krypton = new Chemical("Krypton") { Phase = Phase.Gas };

        public static Chemical Methane = new Chemical("Methane")
        {
            Phase = Phase.Gas,
            MeltingPoint = 91.15F,
            IsFlammable = true,
            GreenhousePotential = 34,
            AntoineCoefficients = new float[3] { 3.7687F, 395.744F, 266.681F },
            AntoineMaximumTemperature = 120.6F,
            AntoineMinimumTemperature = 90.7F
        };
        public static Chemical MethaneLiquid = new Chemical("Liquid Methane") { Phase = Phase.Liquid, MeltingPoint = 91.15F, IsFlammable = true };
        public static Chemical MethaneIce = new Chemical("Methane Ice") { Phase = Phase.Solid, MeltingPoint = 91.15F, IsFlammable = true };

        public static Chemical Neon = new Chemical("Neon") { Phase = Phase.Gas };

        public static Chemical Nitrogen = new Chemical("Nitrogen")
        {
            Phase = Phase.Gas,
            MeltingPoint = 63.15F,
            AntoineCoefficients = new float[3] { 3.61947F, 255.68F, 266.55F },
            AntoineMaximumTemperature = 83.7F,
            AntoineMinimumTemperature = 63.2F
        };
        public static Chemical NitrogenLiquid = new Chemical("Liquid Nitrogen") { Phase = Phase.Liquid, MeltingPoint = 63.15F };
        public static Chemical NitrogenIce = new Chemical("Nitrogen Ice") { Phase = Phase.Solid, MeltingPoint = 63.15F };

        // Oxygen is not really flammable: it's an oxidizer; but the difference in practice is considered unimportant for this library's purposes.
        public static Chemical Oxygen = new Chemical("Oxygen")
        {
            Phase = Phase.Gas,
            MeltingPoint = 54.36F,
            IsFlammable = true,
            AntoineCoefficients = new float[3] { 3.81634F, 319.01F, 266.697F },
            AntoineMaximumTemperature = 97.2F,
            AntoineMinimumTemperature = 62.6F
        };
        public static Chemical OxygenLiquid = new Chemical("Liquid Oxygen") { Phase = Phase.Liquid, MeltingPoint = 54.36F, IsFlammable = true };
        public static Chemical OxygenIce = new Chemical("Oxygen Ice") { Phase = Phase.Solid, MeltingPoint = 54.36F, IsFlammable = true };

        // Like oxygen, ozone is not really flammable, but it's an even stronger oxidizer.
        public static Chemical Ozone = new Chemical("Ozone")
        {
            Phase = Phase.Gas,
            MeltingPoint = 81.15F,
            IsFlammable = true,
            AntoineCoefficients = new float[3] { 4.23637F, 712.487F, 280.132F },
            AntoineMaximumTemperature = 162,
            AntoineMinimumTemperature = 92.8F
        };
        public static Chemical OzoneLiquid = new Chemical("Liquid Ozone") { Phase = Phase.Liquid, MeltingPoint = 81.15F, IsFlammable = true };
        public static Chemical OzoneIce = new Chemical("Ozone Ice") { Phase = Phase.Solid, MeltingPoint = 81.15F, IsFlammable = true };

        public static Chemical Phosphine = new Chemical("Phosphine")
        {
            Phase = Phase.Gas,
            MeltingPoint = 140.35F,
            IsFlammable = true,
            AntoineCoefficients = new float[3] { 4.02591F, 702.651F, 262.085F },
            AntoineMaximumTemperature = 185.7F,
            AntoineMinimumTemperature = 143.8F
        };

        public static Chemical SulphurDioxide = new Chemical("Sulphur Dioxide")
        {
            Phase = Phase.Gas,
            MeltingPoint = 202.15F,
            AntoineCoefficients = new float[3] { 4.40718F, 999.90F, 237.19F },
            AntoineMaximumTemperature = 279.5F,
            AntoineMinimumTemperature = 210F
        };
        public static Chemical SulphurDioxideLiquid = new Chemical("Liquid Sulphur Dioxide") { Phase = Phase.Liquid };
        public static Chemical SulphurDioxideIce = new Chemical("Sulphur Dioxide Ice") { Phase = Phase.Solid };

        public static Chemical Xenon = new Chemical("Xenon") { Phase = Phase.Gas };

        #endregion // Atmospheric substances

        public static Chemical Water = new Chemical("Water") { Phase = Phase.Liquid, MeltingPoint = 274.15F, IsConductive = true };
        public static Chemical WaterSalt = new Chemical("Salt Water") { Phase = Phase.Liquid, MeltingPoint = 273.15F, IsConductive = true };
        public static Chemical WaterIce = new Chemical("Ice") { Phase = Phase.Solid, MeltingPoint = 274.15F };
        public static Chemical WaterVapor = new Chemical("Water Vapor")
        {
            Phase = Phase.Gas,
            MeltingPoint = 274.15F,
            GreenhousePotential = 1,
            AntoineCoefficients = new float[3] { 4.6543F, 1435.264F, 208.302F },
            AntoineMaximumTemperature = 373,
            AntoineMinimumTemperature = 255.9F
        };

        #region Rock

        public static Chemical Clay = new Chemical("Clay") { Phase = Phase.Solid };
        public static Chemical Dirt = new Chemical("Dirt") { Phase = Phase.Solid };
        public static Chemical Dust = new Chemical("Dust") { Phase = Phase.Solid };
        public static Chemical Mud = new Chemical("Mud") { Phase = Phase.Liquid };

        public static Chemical Rock = new Chemical("Rock") { Phase = Phase.Solid, MeltingPoint = 1200 };
        public static Chemical MoltenRock = new Chemical("Molten Rock") { Phase = Phase.Liquid, MeltingPoint = 1200 };

        #endregion // Rock

        #region Metals

        public static Chemical Aluminum = new Chemical("Aluminum") { Phase = Phase.Solid, MeltingPoint = 933.45F, IsConductive = true };
        public static Chemical AluminumMolten = new Chemical("Molten Aluminum") { Phase = Phase.Liquid, MeltingPoint = 933.45F, IsConductive = true };

        public static Chemical Copper = new Chemical("Copper") { Phase = Phase.Solid, MeltingPoint = 1358.15F, IsConductive = true };
        public static Chemical CopperMolten = new Chemical("Molten Copper") { Phase = Phase.Liquid, MeltingPoint = 1358.15F, IsConductive = true };

        public static Chemical Gold = new Chemical("Gold") { Phase = Phase.Solid, MeltingPoint = 1337.15F, IsConductive = true };
        public static Chemical GoldMolten = new Chemical("Molten Gold") { Phase = Phase.Liquid, MeltingPoint = 1337.15F, IsConductive = true };

        public static Chemical Iron = new Chemical("Iron") { Phase = Phase.Solid, MeltingPoint = 1811.15F, IsConductive = true };
        public static Chemical IronMolten = new Chemical("Molten Iron") { Phase = Phase.Liquid, MeltingPoint = 1811.15F, IsConductive = true };

        public static Chemical Nickel = new Chemical("Nickel") { Phase = Phase.Solid, MeltingPoint = 1728.15F, IsConductive = true };
        public static Chemical NickelMolten = new Chemical("Molten Nickel") { Phase = Phase.Liquid, MeltingPoint = 1728.15F, IsConductive = true };

        public static Chemical Platinum = new Chemical("Platinum") { Phase = Phase.Solid, MeltingPoint = 2041.15F, IsConductive = true };
        public static Chemical PlatinumMolten = new Chemical("Molten Platinum") { Phase = Phase.Liquid, MeltingPoint = 2041.15F, IsConductive = true };

        public static Chemical Silver = new Chemical("Silver") { Phase = Phase.Solid, MeltingPoint = 1234.95F, IsConductive = true };
        public static Chemical SilverMolten = new Chemical("Molten Silver") { Phase = Phase.Liquid, MeltingPoint = 1234.95F, IsConductive = true };

        public static Chemical Steel = new Chemical("Steel") { Phase = Phase.Solid, MeltingPoint = 1643.15F, IsConductive = true };
        public static Chemical SteelMolten = new Chemical("Molten Steel") { Phase = Phase.Liquid, MeltingPoint = 1643.15F, IsConductive = true };

        public static Chemical Titanium = new Chemical("Titanium") { Phase = Phase.Solid, MeltingPoint = 1941.15F, IsConductive = true };
        public static Chemical TitaniumMolten = new Chemical("Molten Titanium") { Phase = Phase.Liquid, MeltingPoint = 1941.15F, IsConductive = true };

        #endregion // Metals

        #region Gems

        public static Chemical Diamond = new Chemical("Diamond") { Phase = Phase.Solid };
        public static Chemical Emerald = new Chemical("Emerald") { Phase = Phase.Solid };
        public static Chemical Ruby = new Chemical("Ruby") { Phase = Phase.Solid };
        public static Chemical Sapphire = new Chemical("Sapphire") { Phase = Phase.Solid };
        public static Chemical Topaz = new Chemical("Topaz") { Phase = Phase.Solid };

        #endregion // Gems

        #endregion // Static presets
    }
}
