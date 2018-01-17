using System;
using System.Numerics;
using System.Text;
using WorldFoundry.Space;
using WorldFoundry.Utilities;
using WorldFoundry.Utilities.MathUtil.Shapes;

namespace WorldFoundry.CelestialBodies.Stars
{
    /// <summary>
    /// A stellar body.
    /// </summary>
    public class Star : CelestialBody
    {
        public const string RedDwarfTypeName = "Red Dwarf";

        internal new static string baseTypeName = "Star";
        /// <summary>
        /// The base name for this type of <see cref="CelestialEntity"/>.
        /// </summary>
        public override string BaseTypeName => baseTypeName;

        private string _designatorPrefix;
        /// <summary>
        /// An optional string which is placed before a <see cref="CelestialEntity"/>'s <see cref="Designation"/>.
        /// </summary>
        protected override string DesignatorPrefix => GetProperty(ref _designatorPrefix, GenerateDesignatorPrefix);

        private double? _luminosity;
        /// <summary>
        /// The luminosity of this <see cref="Star"/>, in Watts.
        /// </summary>
        public double Luminosity
        {
            get => GetProperty(ref _luminosity, GenerateLuminosity) ?? 0;
            set => _luminosity = value;
        }

        private LuminosityClass? _luminosityClass;
        /// <summary>
        /// The <see cref="Stars.LuminosityClass"/> of this <see cref="Star"/>.
        /// </summary>
        public LuminosityClass LuminosityClass
        {
            get => GetProperty(ref _luminosityClass, GenerateLuminosityClass) ?? LuminosityClass.None;
            set => _luminosityClass = value;
        }

        /// <summary>
        /// True if this is a Population II <see cref="Star"/>; false if it is a Population I <see cref="Star"/>.
        /// </summary>
        public bool IsPopulationII { get; internal set; }

        private SpectralClass? _spectralClass;
        /// <summary>
        /// The <see cref="Stars.SpectralClass"/> of this <see cref="Star"/>.
        /// </summary>
        public SpectralClass SpectralClass
        {
            get => GetProperty(ref _spectralClass, GenerateSpectralClass) ?? SpectralClass.None;
            set => _spectralClass = value;
        }

        /// <summary>
        /// The name for this type of <see cref="CelestialEntity"/>.
        /// </summary>
        public override string TypeName => SpectralClass == SpectralClass.M ? RedDwarfTypeName : BaseTypeName;

        /// <summary>
        /// Initializes a new instance of <see cref="Star"/>.
        /// </summary>
        public Star() { }

        /// <summary>
        /// Initializes a new instance of <see cref="Star"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="Star"/> is located.
        /// </param>
        public Star(CelestialObject parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="Star"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="Star"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="Star"/>.</param>
        /// <param name="spectralClass">The <see cref="Stars.SpectralClass"/> of this <see cref="Star"/>.</param>
        /// <param name="luminosityClass">
        /// The <see cref="Stars.LuminosityClass"/> of this <see cref="Star"/>.
        /// </param>
        /// <param name="populationII">Set to true if this is to be a Population II <see cref="Star"/>.</param>
        public Star(
            CelestialObject parent,
            Vector3 position,
            SpectralClass? spectralClass = null,
            LuminosityClass? luminosityClass = null,
            bool populationII = false) : base(parent, position)
        {
            if (spectralClass.HasValue)
            {
                SpectralClass = spectralClass.Value;
            }

            if (luminosityClass.HasValue)
            {
                LuminosityClass = luminosityClass.Value;
            }

            if (populationII == true)
            {
                IsPopulationII = true;
            }
        }

        private void GenerateDesignatorPrefix()
        {
            var sb = new StringBuilder();

            // These luminosity classes are prefixed instead of postfixed.
            if (LuminosityClass == LuminosityClass.sd || LuminosityClass == LuminosityClass.D)
            {
                sb.Append(LuminosityClass.ToString());
            }

            if (SpectralClass != SpectralClass.None && SpectralClass != SpectralClass.Other)
            {
                sb.Append(SpectralClass.ToString());
            }

            // The actual luminosity class is '0' but numerical values can't be used as
            // enum labels, so this one must be converted.
            if (LuminosityClass == LuminosityClass.Zero)
            {
                sb.Append("0");
            }
            else if (LuminosityClass != LuminosityClass.None
                && LuminosityClass != LuminosityClass.sd
                && LuminosityClass != LuminosityClass.D
                && LuminosityClass != LuminosityClass.Other)
            {
                sb.Append(LuminosityClass.ToString());
            }

            _designatorPrefix = sb.ToString();
        }

        /// <summary>
        /// Randomly determines a <see cref="Luminosity"/> for this <see cref="Star"/>.
        /// </summary>
        /// <remarks>
        /// Luminosity scales with temperature for main-sequence stars.
        /// </remarks>
        private protected virtual void GenerateLuminosity()
        {
            Luminosity = Math.Pow((Temperature ?? 0) / 5778, 5.6) * 3.846e26;

            // If a special luminosity class had been assigned, take it into account.
            if (LuminosityClass == LuminosityClass.sd)
            {
                // Subdwarfs are 1.5 to 2 magnitudes less luminous than expected.
                Luminosity = Luminosity / Randomizer.Static.NextDouble(55, 100);
            }
            else if (LuminosityClass == LuminosityClass.IV)
            {
                // Subgiants are 1.5 to 2 magnitudes more luminous than expected.
                Luminosity = Luminosity * Randomizer.Static.NextDouble(55, 100);
            }
        }

        /// <summary>
        /// Randomly determines a <see cref="LuminosityClass"/> for this <see cref="Star"/>.
        /// </summary>
        /// <remarks>
        /// The base class handles main-sequence stars (class V).
        /// </remarks>
        private protected virtual void GenerateLuminosityClass() => LuminosityClass = LuminosityClass.V;

        /// <summary>
        /// Generates the <see cref="Mass"/> of this <see cref="Orbiter"/>.
        /// </summary>
        /// <remarks>
        /// Mass scales with radius for main-sequence stars, with the scale changing at around 1
        /// solar mass/radius.
        /// </remarks>
        private protected override void GenerateMass()
        {
            if (Radius < 6.955e8)
            {
                Mass = Math.Pow(Radius / 6.955e8, 1.25) * 1.99e30;
            }
            else
            {
                Mass = Math.Pow(Radius / 6.955e8, 1.75) * 1.99e30;
            }
        }

        /// <summary>
        /// Generates the <see cref="Shape"/> of this <see cref="CelestialEntity"/>.
        /// </summary>
        /// <remarks>
        /// A main sequence <see cref="Star"/>'s radius has a direct relationship to <see cref="Luminosity"/>.
        /// </remarks>
        private protected override void GenerateShape()
        {
            var d = Utilities.MathUtil.Constants.FourPI * 5.67e-8 * Math.Pow(Temperature ?? 0, 4);
            var radius = Math.Round(Math.Sqrt(Luminosity / d));

            var flattening = Math.Max(Randomizer.Static.Normal(0.15, 0.05), 0);

            SetShape(new Ellipsoid(radius, Math.Round(radius * (1 - flattening))));
        }

        /// <summary>
        /// Randomly determines a <see cref="SpectralClass"/> for this <see cref="Star"/>.
        /// </summary>
        /// <remarks>
        /// The base class handles main-sequence stars.
        /// </remarks>
        private protected virtual void GenerateSpectralClass()
        {
            var chance = Randomizer.Static.NextDouble();
            if (chance <= 0.0000003)
            {
                SpectralClass = SpectralClass.O; // 0.00003%
            }
            else if (chance <= 0.0013)
            {
                SpectralClass = SpectralClass.B; // ~0.13%
            }
            else if (chance <= 0.0073)
            {
                SpectralClass = SpectralClass.A; // ~0.6%
            }
            else if (chance <= 0.0373)
            {
                SpectralClass = SpectralClass.F; // ~3%
            }
            else if (chance <= 0.1133)
            {
                SpectralClass = SpectralClass.G; // ~7.6%
            }
            else if (chance <= 0.2343)
            {
                SpectralClass = SpectralClass.K; // ~12.1%
            }
            else
            {
                SpectralClass = SpectralClass.M; // ~76.45%
            }
        }

        /// <summary>
        /// Determines a temperature for this <see cref="ThermalBody"/>, in K.
        /// </summary>
        private protected override void GenerateTemperature()
        {
            switch (SpectralClass)
            {
                case SpectralClass.O:
                    Temperature = (float)Math.Round(30000 + Math.Abs(Randomizer.Static.Normal(0, 6666)));
                    break;
                case SpectralClass.B:
                    Temperature = (float)Math.Round(Randomizer.Static.NextDouble(10000, 30000));
                    break;
                case SpectralClass.A:
                    Temperature = (float)Math.Round(Randomizer.Static.NextDouble(7500, 10000));
                    break;
                case SpectralClass.F:
                    Temperature = (float)Math.Round(Randomizer.Static.NextDouble(6000, 7500));
                    break;
                case SpectralClass.G:
                    Temperature = (float)Math.Round(Randomizer.Static.NextDouble(5200, 6000));
                    break;
                case SpectralClass.K:
                    Temperature = (float)Math.Round(Randomizer.Static.NextDouble(3700, 5200));
                    break;
                case SpectralClass.M:
                    Temperature = (float)Math.Round(Randomizer.Static.NextDouble(2400, 3700));
                    break;
                case SpectralClass.L:
                    Temperature = (float)Math.Round(Randomizer.Static.NextDouble(1300, 2400));
                    break;
                case SpectralClass.T:
                    Temperature = (float)Math.Round(Randomizer.Static.NextDouble(500, 1300));
                    break;
                case SpectralClass.Y:
                    Temperature = (float)Math.Round(Randomizer.Static.NextDouble(250, 500));
                    break;
                case SpectralClass.W:
                    Temperature = (float)Math.Round(30000 + Math.Abs(Randomizer.Static.Normal(0, 56666)));
                    break;
                default: // No way to know what 'None' or 'Other' should be.
                    Temperature = 0;
                    break;
            }
        }

        /// <summary>
        /// Determines <see cref="Luminosity"/> based on this <see cref="Star"/>'s <see cref="CelestialEntity.Radius"/>.
        /// </summary>
        private protected double GetLuminosityFromRadius()
            => Utilities.MathUtil.Constants.FourPI * RadiusSquared * Utilities.Science.Constants.StefanBoltzmannConstant * Math.Pow(Temperature ?? 0, 4);

        /// <summary>
        /// Calculates the number of giant, ice giant, and terrestrial planets this star may have.
        /// The final number may be affected by other factors.
        /// </summary>
        /// <returns>
        /// A value tuple with the number of giants, ice giants, and terrestrial planets, in that order.
        /// </returns>
        internal (int numGiants, int numIceGiants, int numTerrestrial) GetNumPlanets()
        {
            bool hasGiants = GetWillHaveGiantPlanets();
            bool hasIceGiants = GetWillHaveIceGiants();
            bool hasTerrestrial = GetWillHaveTerrestrialPlanets();

            int numPlanets = 0;
            if (hasGiants || hasIceGiants || hasTerrestrial)
            {
                // Slightly less than half of systems have a large collection of planets. The rest
                // have just a few. White dwarfs never have many.
                if (!(this is WhiteDwarf) && Randomizer.Static.NextDouble() <= 0.45)
                {
                    numPlanets = (int)Math.Round(Randomizer.Static.NextDouble(4.2, 8)); // 6.1 +/-1.9
                }
                // 1-3 in a Gaussian distribution, with 1 as the mean
                else
                {
                    numPlanets = (int)Math.Ceiling(1 + Math.Abs(Randomizer.Static.Normal(0, 1)));
                }
            }

            // If any, then up to 1/3 the total (but at least 1).
            int numGiants = hasGiants ? (int)Math.Round(Math.Max(1, Randomizer.Static.NextDouble(numPlanets / 3.0))) : 0;

            // If any, and the total is not already taken up by giants (may happen if only 1 total
            // but both types of giant are indicated), then up to 1/3 the total (but at least 1).
            int numIceGiants =
                (hasIceGiants && numGiants < numPlanets) ?
                (int)Math.Round(Math.Max(1, Randomizer.Static.NextDouble(numPlanets / 3.0))) : 0;

            int numTerrestrial = 0;
            if (hasTerrestrial)
            {
                // If the giants and ice giants have already filled the total,
                // and the total is greater than 2, take one slot back.
                if (numGiants + numIceGiants >= numPlanets && numPlanets > 2)
                {
                    // Pick the type form which to take the slot back at random.
                    if (Randomizer.Static.NextBoolean())
                    {
                        numGiants--;
                    }
                    else
                    {
                        numIceGiants--;
                    }
                }
                // Take all the remaining slots.
                numTerrestrial = Math.Max(0, numPlanets - numGiants - numIceGiants);
            }
            return (numGiants, numIceGiants, numTerrestrial);
        }

        /// <summary>
        /// Determines <see cref="SpectralClass"/> from <see cref="ThermalBody.Temperature"/>.
        /// </summary>
        /// <remarks>
        /// Only applies to the standard classes (excludes W).
        /// </remarks>
        private protected SpectralClass GetSpectralClassFromTemperature(float temperature)
        {
            if (temperature < 500)
            {
                return SpectralClass.Y;
            }
            else if (temperature < 1300)
            {
                return SpectralClass.T;
            }
            else if (temperature < 2400)
            {
                return SpectralClass.L;
            }
            else if (temperature < 3700)
            {
                return SpectralClass.M;
            }
            else if (temperature < 5200)
            {
                return SpectralClass.K;
            }
            else if (temperature < 6000)
            {
                return SpectralClass.G;
            }
            else if (temperature < 7500)
            {
                return SpectralClass.F;
            }
            else if (temperature < 10000)
            {
                return SpectralClass.A;
            }
            else if (temperature < 30000)
            {
                return SpectralClass.B;
            }
            else
            {
                return SpectralClass.O;
            }
        }

        /// <summary>
        /// Pseudo-randomly determines whether this <see cref="Star"/> will have giant planets, based
        /// on its characteristics.
        /// </summary>
        /// <returns>true if this <see cref="Star"/> will have giant planets; false otherwise.</returns>
        private protected virtual bool GetWillHaveGiantPlanets()
        {
            // O-type stars and brown dwarfs do not have giant planets
            if (SpectralClass == SpectralClass.O
                || SpectralClass == SpectralClass.L
                || SpectralClass == SpectralClass.T
                || SpectralClass == SpectralClass.Y)
            {
                return false;
            }

            // Very few Population II stars have giant planets.
            else if (IsPopulationII)
            {
                if (Randomizer.Static.NextDouble() <= 0.9)
                {
                    return false;
                }
            }

            // 32% of Sun-like stars have giant planets
            else if (SpectralClass == SpectralClass.F
                || SpectralClass == SpectralClass.G
                || SpectralClass == SpectralClass.K)
            {
                if (Randomizer.Static.NextDouble() <= 0.68)
                {
                    return false;
                }
            }

            // 1 in 50 red dwarfs have giant planets
            else if (SpectralClass == SpectralClass.M
                && LuminosityClass == LuminosityClass.V)
            {
                if (Randomizer.Static.NextDouble() <= 0.98)
                {
                    return false;
                }
            }

            // 1 in 6 other stars have giant planets
            else if (Randomizer.Static.NextDouble() <= 5.0 / 6.0)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Pseudo-randomly determines whether this <see cref="Star"/> will have ice giant planets,
        /// based on its characteristics.
        /// </summary>
        /// <returns>true if this <see cref="Star"/> will have ice giant planets; false otherwise.</returns>
        private protected virtual bool GetWillHaveIceGiants()
        {
            // O-type stars and brown dwarfs do not have ice giants
            if (SpectralClass == SpectralClass.O
                || SpectralClass == SpectralClass.L
                || SpectralClass == SpectralClass.T
                || SpectralClass == SpectralClass.Y)
            {
                return false;
            }

            // Very few Population II stars have ice giants.
            else if (IsPopulationII)
            {
                if (Randomizer.Static.NextDouble() <= 0.9)
                {
                    return false;
                }
            }

            // 70% of Sun-like stars have ice giants
            else if (SpectralClass == SpectralClass.F
                || SpectralClass == SpectralClass.G
                || SpectralClass == SpectralClass.K)
            {
                if (Randomizer.Static.NextDouble() <= 0.30)
                {
                    return false;
                }
            }

            // 1 in 3 red dwarfs have ice giants
            else if (SpectralClass == SpectralClass.M
                && LuminosityClass == LuminosityClass.V)
            {
                if (Randomizer.Static.NextDouble() <= 2.0 / 3.0)
                {
                    return false;
                }
            }

            // 1 in 6 other stars have ice giants
            else if (Randomizer.Static.NextDouble() <= 5.0 / 6.0)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Pseudo-randomly determines whether this <see cref="Star"/> will have terrestrial planets,
        /// based on its characteristics.
        /// </summary>
        /// <returns>true if this <see cref="Star"/> will have terrestrial planets; false otherwise.</returns>
        private protected virtual bool GetWillHaveTerrestrialPlanets()
        {
            // O-type stars do not have planets
            if (SpectralClass == SpectralClass.O)
            {
                return false;
            }

            // Population II stars do not have terrestrial planets.
            else if (IsPopulationII)
            {
                return false;
            }

            // 79% of Sun-like stars have terrestrial planets
            else if (SpectralClass == SpectralClass.F
                || SpectralClass == SpectralClass.G
                || SpectralClass == SpectralClass.K)
            {
                if (Randomizer.Static.NextDouble() <= 0.38)
                {
                    return false;
                }
            }

            // 45% of red and brown dwarfs have terrestrial planets
            else if ((SpectralClass == SpectralClass.M && LuminosityClass == LuminosityClass.V)
                || SpectralClass == SpectralClass.L
                || SpectralClass == SpectralClass.T
                || SpectralClass == SpectralClass.Y)
            {
                if (Randomizer.Static.NextDouble() <= 0.55)
                {
                    return false;
                }
            }

            // 1 in 6 other stars have terrestrial planets
            else if (Randomizer.Static.NextDouble() <= 5.0 / 6.0)
            {
                return false;
            }

            return true;
        }
    }
}
