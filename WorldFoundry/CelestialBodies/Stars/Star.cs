using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Text;
using WorldFoundry.Place;
using WorldFoundry.Space;
using NeverFoundry.MathAndScience;
using NeverFoundry.MathAndScience.Chemistry;
using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.MathAndScience.Randomization;

namespace WorldFoundry.CelestialBodies.Stars
{
    /// <summary>
    /// A stellar body.
    /// </summary>
    [Serializable]
    public class Star : CelestialLocation
    {
        private const string RedDwarfTypeName = "Red Dwarf";

        private static readonly Number _SolarMass = new Number(6.955, 8);

        // True for most stars.
        internal override bool IsHospitable => true;

        private double? _luminosity;
        /// <summary>
        /// The luminosity of this <see cref="Star"/>, in Watts.
        /// </summary>
        public double Luminosity
        {
            get => _luminosity ??= GetLuminosity();
            set => _luminosity = value;
        }

        private LuminosityClass? _luminosityClass;
        /// <summary>
        /// The <see cref="Stars.LuminosityClass"/> of this <see cref="Star"/>.
        /// </summary>
        public LuminosityClass LuminosityClass
        {
            get => _luminosityClass ??= GetLuminosityClass();
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
            get => _spectralClass ??= GetSpectralClass();
            set => _spectralClass = value;
        }

        /// <summary>
        /// The name for this type of <see cref="CelestialLocation"/>.
        /// </summary>
        public override string TypeName => SpectralClass == SpectralClass.M ? RedDwarfTypeName : BaseTypeName;

        private protected override string BaseTypeName => "Star";

        private string? _designatorPrefix;
        private protected override string DesignatorPrefix => _designatorPrefix ??= GetDesignatorPrefix();

        /// <summary>
        /// Initializes a new instance of <see cref="Star"/>.
        /// </summary>
        internal Star() { }

        /// <summary>
        /// Initializes a new instance of <see cref="Star"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="Location"/> in which this <see cref="Star"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="Star"/>.</param>
        /// <param name="spectralClass">The <see cref="Stars.SpectralClass"/> of this <see cref="Star"/>.</param>
        /// <param name="luminosityClass">
        /// The <see cref="Stars.LuminosityClass"/> of this <see cref="Star"/>.
        /// </param>
        /// <param name="populationII">Set to true if this is to be a Population II <see cref="Star"/>.</param>
        internal Star(
            Location parent,
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

            IsPopulationII = populationII;
        }

        private protected Star(
            string id,
            string? name,
            bool isPrepopulated,
            double? luminosity,
            LuminosityClass? luminosityClass,
            bool isPopulationII,
            SpectralClass? spectralClass,
            double? albedo,
            Vector3 velocity,
            Orbit? orbit,
            IMaterial? material,
            List<Location>? children)
            : base(
                id,
                name,
                isPrepopulated,
                albedo,
                velocity,
                orbit,
                material,
                children)
        {
            _luminosity = luminosity;
            _luminosityClass = luminosityClass;
            IsPopulationII = isPopulationII;
            _spectralClass = spectralClass;
        }

        private Star(SerializationInfo info, StreamingContext context) : this(
            (string)info.GetValue(nameof(Id), typeof(string)),
            (string?)info.GetValue(nameof(Name), typeof(string)),
            (bool)info.GetValue(nameof(_isPrepopulated), typeof(bool)),
            (double?)info.GetValue(nameof(Luminosity), typeof(double?)),
            (LuminosityClass?)info.GetValue(nameof(LuminosityClass), typeof(LuminosityClass?)),
            (bool)info.GetValue(nameof(IsPopulationII), typeof(bool)),
            (SpectralClass?)info.GetValue(nameof(SpectralClass), typeof(SpectralClass?)),
            (double?)info.GetValue(nameof(Albedo), typeof(double?)),
            (Vector3)info.GetValue(nameof(Velocity), typeof(Vector3)),
            (Orbit?)info.GetValue(nameof(Orbit), typeof(Orbit?)),
            (IMaterial?)info.GetValue(nameof(Material), typeof(IMaterial)),
            (List<Location>)info.GetValue(nameof(Children), typeof(List<Location>))) { }

        /// <summary>Populates a <see cref="SerializationInfo"></see> with the data needed to
        /// serialize the target object.</summary>
        /// <param name="info">The <see cref="SerializationInfo"></see> to populate with
        /// data.</param>
        /// <param name="context">The destination (see <see cref="StreamingContext"></see>) for this
        /// serialization.</param>
        /// <exception cref="System.Security.SecurityException">The caller does not have the
        /// required permission.</exception>
        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(Id), Id);
            info.AddValue(nameof(Name), Name);
            info.AddValue(nameof(_isPrepopulated), _isPrepopulated);
            info.AddValue(nameof(Luminosity), _luminosity);
            info.AddValue(nameof(LuminosityClass), _luminosityClass);
            info.AddValue(nameof(IsPopulationII), IsPopulationII);
            info.AddValue(nameof(SpectralClass), _spectralClass);
            info.AddValue(nameof(Albedo), _albedo);
            info.AddValue(nameof(Velocity), Velocity);
            info.AddValue(nameof(Orbit), _orbit);
            info.AddValue(nameof(Material), Material);
            info.AddValue(nameof(Children), Children.ToList());
        }

        /// <summary>
        /// Calculates the number of giant, ice giant, and terrestrial planets this star may have.
        /// The final number may be affected by other factors.
        /// </summary>
        /// <returns>
        /// A value tuple with the number of giants, ice giants, and terrestrial planets, in that order.
        /// </returns>
        internal (int numGiants, int numIceGiants, int numTerrestrial) GetNumPlanets()
        {
            var hasGiants = GetWillHaveGiantPlanets();
            var hasIceGiants = GetWillHaveIceGiants();
            var hasTerrestrial = GetWillHaveTerrestrialPlanets();

            var numPlanets = 0;
            if (hasGiants || hasIceGiants || hasTerrestrial)
            {
                // Slightly less than half of systems have a large collection of planets. The rest
                // have just a few. White dwarfs never have many.
                if (!(this is WhiteDwarf) && Randomizer.Instance.NextDouble() <= 0.45)
                {
                    numPlanets = Randomizer.Instance.NextDouble(4.2, 8).RoundToInt(); // 6.1 +/-1.9
                }
                // 1-3 in a Gaussian distribution, with 1 as the mean
                else
                {
                    numPlanets = (int)Math.Ceiling(1 + Math.Abs(Randomizer.Instance.NormalDistributionSample(0, 1)));
                }
            }

            // If any, then up to 1/3 the total (but at least 1).
            var numGiants = hasGiants ? Math.Max(1, Randomizer.Instance.NextDouble(numPlanets / 3.0)).RoundToInt() : 0;

            // If any, and the total is not already taken up by giants (may happen if only 1 total
            // but both types of giant are indicated), then up to 1/3 the total (but at least 1).
            var numIceGiants = (hasIceGiants && numGiants < numPlanets)
                ? Math.Max(1, Randomizer.Instance.NextDouble(numPlanets / 3.0)).RoundToInt()
                : 0;

            var numTerrestrial = 0;
            if (hasTerrestrial)
            {
                // If the giants and ice giants have already filled the total,
                // and the total is greater than 2, take one slot back.
                if (numGiants + numIceGiants >= numPlanets && numPlanets > 2)
                {
                    // Pick the type form which to take the slot back at random.
                    if (Randomizer.Instance.NextBool())
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
        /// Calculates the temperature this <see cref="Star"/> would have to be in order to cause
        /// the given effective temperature at the given distance.
        /// </summary>
        /// <param name="temperature">The desired temperature, in K.</param>
        /// <param name="distance">The desired distance, in meters.</param>
        /// <param name="albedo">The albedo of the target body. Defaults to zero.</param>
        internal void SetTempForTargetPlanetTemp(double temperature, Number distance, double? albedo = null)
            => Temperature = temperature / (double)(Number.Sqrt(Shape.ContainingRadius / (2 * distance)) * Math.Pow(1 - (albedo ?? 0), 0.25));

        private string GetDesignatorPrefix()
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

            return sb.ToString();
        }

        private protected virtual double GetLuminosity(Number? temperature = null)
        {
            // Luminosity scales with temperature for main-sequence stars.
            var luminosity = Math.Pow((double)(temperature ?? Number.Zero) / 5778, 5.6) * 3.846e26;

            // If a special luminosity class had been assigned, take it into account.
            if (LuminosityClass == LuminosityClass.sd)
            {
                // Subdwarfs are 1.5 to 2 magnitudes less luminous than expected.
                return luminosity / Randomizer.Instance.NextDouble(55, 100);
            }
            else if (LuminosityClass == LuminosityClass.IV)
            {
                // Subgiants are 1.5 to 2 magnitudes more luminous than expected.
                return luminosity * Randomizer.Instance.NextDouble(55, 100);
            }
            else
            {
                return luminosity;
            }
        }

        private protected virtual LuminosityClass GetLuminosityClass() => LuminosityClass.V;

        private protected double GetLuminosityFromRadius()
            => NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.FourPI * (double)RadiusSquared * NeverFoundry.MathAndScience.Constants.Doubles.ScienceConstants.sigma * Math.Pow(Temperature ?? 0, 4);

        private protected override IMaterial GetMaterial()
        {
            var temperature = GetTemperature();

            var shape = GetShape(temperature);

            // Mass scales with radius for main-sequence stars, with the scale changing at around 1
            // solar mass/radius.
            var mass = Number.Pow(shape.ContainingRadius / _SolarMass, shape.ContainingRadius < _SolarMass ? new Number(125, -2) : new Number(175, -2)) * new Number(1.99, 30);

            return GetComposition((double)(mass / shape.Volume), mass, shape, temperature);
        }

        /// <summary>
        /// A main sequence star's radius has a direct relationship to <see cref="Luminosity"/>.
        /// </summary>
        private protected IShape GetShape(Number? temperature)
        {
            var d = NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.FourPI * 5.67e-8 * Math.Pow((double)(temperature ?? Number.Zero), 4);
            _luminosity = GetLuminosity(temperature);
            var radius = d.IsNearlyZero() ? Number.Zero : Math.Sqrt(Luminosity / d);
            var flattening = (Number)Randomizer.Instance.NormalDistributionSample(0.15, 0.05, minimum: 0);
            return new Ellipsoid(radius, radius * (1 - flattening), Position);
        }

        private protected virtual SpectralClass GetSpectralClass()
        {
            var chance = Randomizer.Instance.NextDouble();
            if (chance <= 0.0000003)
            {
                return SpectralClass.O; // 0.00003%
            }
            else if (chance <= 0.0013)
            {
                return SpectralClass.B; // ~0.13%
            }
            else if (chance <= 0.0073)
            {
                return SpectralClass.A; // ~0.6%
            }
            else if (chance <= 0.0373)
            {
                return SpectralClass.F; // ~3%
            }
            else if (chance <= 0.1133)
            {
                return SpectralClass.G; // ~7.6%
            }
            else if (chance <= 0.2343)
            {
                return SpectralClass.K; // ~12.1%
            }
            else
            {
                return SpectralClass.M; // ~76.45%
            }
        }

        private protected SpectralClass GetSpectralClassFromTemperature(Number temperature)
        {
            // Only applies to the standard classes (excludes W).
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

        private protected override ISubstanceReference? GetSubstance()
            => IsPopulationII ? CelestialSubstances.StellarMaterialPopulationII : CelestialSubstances.StellarMaterial;

        private protected override double? GetTemperature() => SpectralClass switch
        {
            SpectralClass.O => Randomizer.Instance.PositiveNormalDistributionSample(30000, 6666),
            SpectralClass.B => Randomizer.Instance.NextDouble(10000, 30000),
            SpectralClass.A => Randomizer.Instance.NextDouble(7500, 10000),
            SpectralClass.F => Randomizer.Instance.NextDouble(6000, 7500),
            SpectralClass.G => Randomizer.Instance.NextDouble(5200, 6000),
            SpectralClass.K => Randomizer.Instance.NextDouble(3700, 5200),
            SpectralClass.M => Randomizer.Instance.NextDouble(2400, 3700),
            SpectralClass.L => Randomizer.Instance.NextDouble(1300, 2400),
            SpectralClass.T => Randomizer.Instance.NextDouble(500, 1300),
            SpectralClass.Y => Randomizer.Instance.NextDouble(250, 500),
            SpectralClass.W => Randomizer.Instance.PositiveNormalDistributionSample(30000, 56666),
            _ => 0,
        };

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
                if (Randomizer.Instance.NextDouble() <= 0.9)
                {
                    return false;
                }
            }

            // 32% of Sun-like stars have giant planets
            else if (SpectralClass == SpectralClass.F
                || SpectralClass == SpectralClass.G
                || SpectralClass == SpectralClass.K)
            {
                if (Randomizer.Instance.NextDouble() <= 0.68)
                {
                    return false;
                }
            }

            // 1 in 50 red dwarfs have giant planets
            else if (SpectralClass == SpectralClass.M
                && LuminosityClass == LuminosityClass.V)
            {
                if (Randomizer.Instance.NextDouble() <= 0.98)
                {
                    return false;
                }
            }

            // 1 in 6 other stars have giant planets
            else if (Randomizer.Instance.NextDouble() <= 5.0 / 6.0)
            {
                return false;
            }

            return true;
        }

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
                if (Randomizer.Instance.NextDouble() <= 0.9)
                {
                    return false;
                }
            }

            // 70% of Sun-like stars have ice giants
            else if (SpectralClass == SpectralClass.F
                || SpectralClass == SpectralClass.G
                || SpectralClass == SpectralClass.K)
            {
                if (Randomizer.Instance.NextDouble() <= 0.30)
                {
                    return false;
                }
            }

            // 1 in 3 red dwarfs have ice giants
            else if (SpectralClass == SpectralClass.M
                && LuminosityClass == LuminosityClass.V)
            {
                if (Randomizer.Instance.NextDouble() <= 2.0 / 3.0)
                {
                    return false;
                }
            }

            // 1 in 6 other stars have ice giants
            else if (Randomizer.Instance.NextDouble() <= 5.0 / 6.0)
            {
                return false;
            }

            return true;
        }

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
                if (Randomizer.Instance.NextDouble() <= 0.38)
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
                if (Randomizer.Instance.NextDouble() <= 0.55)
                {
                    return false;
                }
            }

            // 1 in 6 other stars have terrestrial planets
            else if (Randomizer.Instance.NextDouble() <= 5.0 / 6.0)
            {
                return false;
            }

            return true;
        }
    }
}
