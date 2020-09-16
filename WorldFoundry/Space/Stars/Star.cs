using NeverFoundry.DataStorage;
using NeverFoundry.MathAndScience;
using NeverFoundry.MathAndScience.Chemistry;
using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.MathAndScience.Randomization;
using NeverFoundry.WorldFoundry.Space.Stars;
using System;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;

namespace NeverFoundry.WorldFoundry.Space
{
    /// <summary>
    /// A stellar body.
    /// </summary>
    [Serializable]
    [Newtonsoft.Json.JsonConverter(typeof(NewtonsoftJson.StarConverter))]
    [System.Text.Json.Serialization.JsonConverter(typeof(StarConverter))]
    public class Star : CosmicLocation
    {
        private static readonly Number _MinBlueHypergiantMass = new Number(7.96, 31);
        private static readonly Number _SolarMass = new Number(6.955, 8);

        /// <summary>
        /// The type discriminator for this type.
        /// </summary>
        public const string StarIdItemTypeName = ":Location:CosmicLocation:Star:";
        /// <summary>
        /// A built-in, read-only type discriminator.
        /// </summary>
        public override string IdItemTypeName => StarIdItemTypeName;

        internal virtual bool IsHospitable => StarType switch
        {
            // False for brown dwarfs; their habitable zones, if any, are moving targets due to rapid
            // cooling, and intersect soon with severe tidal forces, making it unlikely that life could
            // develop before a planet becomes uninhabitable.
            StarType.BrownDwarf => false,

            // False for white dwarfs; their habitable zones, if any, are moving targets due to rapid
            // cooling, and intersect soon with severe tidal forces, and additionally severe UV
            // radiation is expected in early stages at the close distances where a habitable zone could
            // be expected, making it unlikely that life could develop before a planet becomes
            // uninhabitable.
            StarType.WhiteDwarf => false,

            // False for neutron stars, due to their excessive ionizing radiation, which makes the
            // development of life nearby highly unlikely.
            StarType.Neutron => false,

            // False for yellow and blue giants; although they may have a habitable zone, it is not
            // likely to exist in the same place long enough for life to develop before the star
            // evolves into another type, or dies.
            StarType.YellowGiant => false,
            StarType.BlueGiant => false,

            // True for most stars.
            _ => true,
        };

        /// <summary>
        /// Whether this is a giant star.
        /// </summary>
        public bool IsGiant => StarType.Giant.HasFlag(StarType);

        /// <summary>
        /// True if this is a Population II <see cref="Star"/>; false if it is a Population I <see cref="Star"/>.
        /// </summary>
        public bool IsPopulationII { get; private set; }

        /// <summary>
        /// The luminosity of this <see cref="Star"/>, in Watts.
        /// </summary>
        public double Luminosity { get; private set; }

        /// <summary>
        /// The <see cref="Stars.LuminosityClass"/> of this <see cref="Star"/>.
        /// </summary>
        public LuminosityClass LuminosityClass { get; private set; }

        /// <summary>
        /// The <see cref="Stars.SpectralClass"/> of this <see cref="Star"/>.
        /// </summary>
        public SpectralClass SpectralClass { get; private set; }

        /// <summary>
        /// The type of this star.
        /// </summary>
        public StarType StarType { get; }

        private protected override string BaseTypeName => StarType switch
        {
            StarType.BrownDwarf => "Brown Dwarf",
            StarType.WhiteDwarf => "White Dwarf",
            StarType.Neutron => "Neutron Star",
            StarType.RedGiant => "Red Giant",
            StarType.YellowGiant => "Yellow Giant",
            StarType.BlueGiant => "Blue Giant",
            _ => SpectralClass == SpectralClass.M ? "Red Dwarf" : "Star",
        };

        private string? _typeNameSuffix;
        private protected override string? TypeNameSuffix
        {
            get
            {
                if (_typeNameSuffix is null)
                {
                    if (StarType == StarType.Neutron)
                    {
                        _typeNameSuffix = "X";
                    }

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
                        sb.Append('0');
                    }
                    else if (LuminosityClass != LuminosityClass.None
                        && LuminosityClass != LuminosityClass.sd
                        && LuminosityClass != LuminosityClass.D
                        && LuminosityClass != LuminosityClass.Other)
                    {
                        sb.Append(LuminosityClass.ToString());
                    }

                    _typeNameSuffix = sb.ToString();
                }
                return _typeNameSuffix;
            }
        }

        /// <summary>
        /// Initializes a new instance of <see cref="Star"/> with the given parameters.
        /// </summary>
        /// <param name="starType">The type of the star.</param>
        /// <param name="parent">
        /// The containing parent location for which to generate a child.
        /// </param>
        /// <param name="position">The position for the child.</param>
        /// <param name="orbit">
        /// <para>
        /// An optional orbit to assign to the child.
        /// </para>
        /// <para>
        /// Depending on the parameters, may override <paramref name="position"/>.
        /// </para>
        /// </param>
        /// <param name="spectralClass">
        /// The <see cref="Stars.SpectralClass"/> of the <see cref="Star"/>.
        /// </param>
        /// <param name="luminosityClass">
        /// The <see cref="Stars.LuminosityClass"/> of the <see cref="Star"/>.
        /// </param>
        /// <param name="populationII">
        /// Set to true if this is to be a Population II <see cref="Star"/>.
        /// </param>
        public Star(
            StarType starType,
            CosmicLocation? parent,
            Vector3 position,
            OrbitalParameters? orbit = null,
            SpectralClass? spectralClass = null,
            LuminosityClass? luminosityClass = null,
            bool populationII = false) : base(parent?.Id, CosmicStructureType.Star)
        {
            StarType = starType;

            Configure(position, spectralClass, luminosityClass, populationII);

            if (parent is not null && !orbit.HasValue)
            {
                if (parent is AsteroidField asteroidField)
                {
                    orbit = asteroidField.GetChildOrbit();
                }
                else
                {
                    orbit = parent.StructureType switch
                    {
                        CosmicStructureType.GalaxySubgroup => Position.IsZero() ? null : parent.GetGalaxySubgroupChildOrbit(),
                        CosmicStructureType.SpiralGalaxy
                            or CosmicStructureType.EllipticalGalaxy
                            or CosmicStructureType.DwarfGalaxy => Position.IsZero() ? (OrbitalParameters?)null : parent.GetGalaxyChildOrbit(),
                        CosmicStructureType.GlobularCluster => Position.IsZero() ? (OrbitalParameters?)null : parent.GetGlobularClusterChildOrbit(),
                        CosmicStructureType.StarSystem => parent is StarSystem && !Position.IsZero()
                            ? OrbitalParameters.GetFromEccentricity(parent.Mass, parent.Position, Randomizer.Instance.PositiveNormalDistributionSample(0, 0.05))
                            : (OrbitalParameters?)null,
                        _ => null,
                    };
                }
            }
            if (orbit.HasValue)
            {
                Space.Orbit.AssignOrbit(this, orbit.Value);
            }
        }

        /// <summary>
        /// Initializes a new main sequence <see cref="Star"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing parent location for which to generate a child.
        /// </param>
        /// <param name="position">The position for the child.</param>
        /// <param name="orbit">
        /// <para>
        /// An optional orbit to assign to the child.
        /// </para>
        /// <para>
        /// Depending on the parameters, may override <paramref name="position"/>.
        /// </para>
        /// </param>
        /// <param name="spectralClass">
        /// The <see cref="Stars.SpectralClass"/> of the <see cref="Star"/>.
        /// </param>
        /// <param name="luminosityClass">
        /// The <see cref="Stars.LuminosityClass"/> of the <see cref="Star"/>.
        /// </param>
        /// <param name="populationII">
        /// Set to true if this is to be a Population II <see cref="Star"/>.
        /// </param>
        public Star(
            CosmicLocation? parent,
            Vector3 position,
            OrbitalParameters? orbit = null,
            SpectralClass? spectralClass = null,
            LuminosityClass? luminosityClass = null,
            bool populationII = false) : this(
                StarType.MainSequence,
                parent,
                position,
                orbit,
                spectralClass,
                luminosityClass,
                populationII)
        { }

        private Star(string? parentId, StarType starType) : base(parentId, CosmicStructureType.Star) => StarType = starType;

        internal Star(
            string id,
            uint seed,
            StarType starType,
            string? parentId,
            Vector3[]? absolutePosition,
            string? name,
            Vector3 velocity,
            Orbit? orbit,
            Vector3 position,
            double temperature,
            bool isPopulationII,
            LuminosityClass luminosityClass,
            SpectralClass spectralClass)
            : base(
                id,
                seed,
                CosmicStructureType.Star,
                parentId,
                absolutePosition,
                name,
                velocity,
                orbit)
        {
            StarType = starType;
            LuminosityClass = luminosityClass;
            IsPopulationII = isPopulationII;
            SpectralClass = spectralClass;
            Reconstitute(position, temperature);
        }

        private Star(SerializationInfo info, StreamingContext context) : this(
            (string?)info.GetValue(nameof(Id), typeof(string)) ?? string.Empty,
            (uint?)info.GetValue(nameof(_seed), typeof(uint)) ?? default,
            (StarType?)info.GetValue(nameof(StarType), typeof(StarType)) ?? StarType.MainSequence,
            (string?)info.GetValue(nameof(ParentId), typeof(string)) ?? string.Empty,
            (Vector3[]?)info.GetValue(nameof(AbsolutePosition), typeof(Vector3[])),
            (string?)info.GetValue(nameof(Name), typeof(string)),
            (Vector3?)info.GetValue(nameof(Velocity), typeof(Vector3)) ?? default,
            (Orbit?)info.GetValue(nameof(Orbit), typeof(Orbit?)),
            (Vector3?)info.GetValue(nameof(Position), typeof(Vector3)) ?? default,
            (double?)info.GetValue(nameof(Temperature), typeof(double)) ?? default,
            (bool?)info.GetValue(nameof(IsPopulationII), typeof(bool)) ?? default,
            (LuminosityClass?)info.GetValue(nameof(LuminosityClass), typeof(LuminosityClass)) ?? default,
            (SpectralClass?)info.GetValue(nameof(SpectralClass), typeof(SpectralClass)) ?? default)
        { }

        /// <summary>
        /// Generates a new <see cref="Star"/> instance as a child of the given containing
        /// <paramref name="parent"/> location, with parameters similar to Sol, Earth's sun.
        /// </summary>
        /// <param name="parent">
        /// The containing parent location for which to generate a child.
        /// </param>
        /// <param name="position">The position for the child.</param>
        /// <param name="orbit">
        /// <para>
        /// An optional orbit to assign to the child.
        /// </para>
        /// <para>
        /// Depending on the parameters, may override <paramref name="position"/>.
        /// </para>
        /// </param>
        /// <returns>
        /// <para>
        /// The generated child location.
        /// </para>
        /// <para>
        /// If no child could be generated, returns <see langword="null"/>.
        /// </para>
        /// </returns>
        public static Star? NewSunlike(
            CosmicLocation? parent,
            Vector3 position,
            OrbitalParameters? orbit = null)
        {
            var instance = new Star(parent?.Id, StarType.MainSequence);

            instance.ConfigureSunlike(position);

            if (parent is not null && !orbit.HasValue)
            {
                if (parent is AsteroidField asteroidField)
                {
                    orbit = asteroidField.GetChildOrbit();
                }
                else
                {
                    orbit = parent.StructureType switch
                    {
                        CosmicStructureType.GalaxySubgroup => instance.Position.IsZero() ? null : parent.GetGalaxySubgroupChildOrbit(),
                        CosmicStructureType.SpiralGalaxy
                            or CosmicStructureType.EllipticalGalaxy
                            or CosmicStructureType.DwarfGalaxy => instance.Position.IsZero() ? (OrbitalParameters?)null : parent.GetGalaxyChildOrbit(),
                        CosmicStructureType.GlobularCluster => instance.Position.IsZero() ? (OrbitalParameters?)null : parent.GetGlobularClusterChildOrbit(),
                        CosmicStructureType.StarSystem => parent is StarSystem && !instance.Position.IsZero()
                            ? OrbitalParameters.GetFromEccentricity(parent.Mass, parent.Position, Randomizer.Instance.PositiveNormalDistributionSample(0, 0.05))
                            : (OrbitalParameters?)null,
                        _ => null,
                    };
                }
            }
            if (orbit.HasValue)
            {
                Space.Orbit.AssignOrbit(instance, orbit.Value);
            }

            return instance;
        }

        /// <summary>
        /// <para>
        /// Removes this location and all contained children from the given data store.
        /// </para>
        /// <para>
        /// Also removes this <see cref="Star"/> from its containing <see cref="StarSystem"/>, if
        /// any.
        /// </para>
        /// </summary>
        public override async Task<bool> DeleteAsync(IDataStore dataStore)
        {
            var parent = await GetParentAsync(dataStore).ConfigureAwait(false);
            if (parent is StarSystem system)
            {
                system.RemoveStar(Id);
                var success = await dataStore.StoreItemAsync(system).ConfigureAwait(false);
                if (!success)
                {
                    return false;
                }
            }
            return await base.DeleteAsync(dataStore).ConfigureAwait(false);
        }

        /// <summary>Populates a <see cref="SerializationInfo"></see> with the data needed to
        /// serialize the target object.</summary>
        /// <param name="info">The <see cref="SerializationInfo"></see> to populate with
        /// data.</param>
        /// <param name="context">The destination (see <see cref="StreamingContext"></see>) for this
        /// serialization.</param>
        /// <exception cref="System.Security.SecurityException">The caller does not have the
        /// required permission.</exception>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(Id), Id);
            info.AddValue(nameof(_seed), _seed);
            info.AddValue(nameof(StarType), StarType);
            info.AddValue(nameof(ParentId), ParentId);
            info.AddValue(nameof(AbsolutePosition), AbsolutePosition);
            info.AddValue(nameof(Name), Name);
            info.AddValue(nameof(Velocity), Velocity);
            info.AddValue(nameof(Orbit), Orbit);
            info.AddValue(nameof(Position), Position);
            info.AddValue(nameof(Temperature), Temperature);
            info.AddValue(nameof(IsPopulationII), IsPopulationII);
            info.AddValue(nameof(LuminosityClass), LuminosityClass);
            info.AddValue(nameof(SpectralClass), SpectralClass);
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
                if (StarType != StarType.WhiteDwarf && Randomizer.Instance.NextDouble() <= 0.45)
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

        private static SpectralClass GetSpectralClassFromTemperature(Number temperature)
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

        private void Configure(
            Vector3 position,
            SpectralClass? spectralClass = null,
            LuminosityClass? luminosityClass = null,
            bool populationII = false)
        {
            _seed = Randomizer.Instance.NextUIntInclusive();

            if (spectralClass.HasValue)
            {
                SpectralClass = spectralClass.Value;
            }

            if (luminosityClass.HasValue)
            {
                LuminosityClass = luminosityClass.Value;
            }
            else
            {
                GenerateLuminosityClass();
            }

            IsPopulationII = populationII;

            if (SpectralClass == SpectralClass.None)
            {
                if (StarType == StarType.BrownDwarf)
                {
                    if (SpectralClass == SpectralClass.None)
                    {
                        var chance = Randomizer.Instance.NextDouble();
                        if (chance <= 0.29)
                        {
                            SpectralClass = SpectralClass.M; // 29%
                        }
                        else if (chance <= 0.79)
                        {
                            SpectralClass = SpectralClass.L; // 50%
                        }
                        else if (chance <= 0.99)
                        {
                            SpectralClass = SpectralClass.T; // 20%
                        }
                        else
                        {
                            SpectralClass = SpectralClass.Y; // 1%
                        }
                    }
                }
                else if (StarType == StarType.Neutron)
                {
                    SpectralClass = SpectralClass.Other;
                }
                else if (StarType != StarType.WhiteDwarf
                    && !IsGiant)
                {
                    var chance = Randomizer.Instance.NextDouble();
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
            }

            var temperature = GenerateTemperature();

            Reconstitute(position, temperature);

            if (SpectralClass == SpectralClass.None && (StarType == StarType.WhiteDwarf || IsGiant))
            {
                SpectralClass = GetSpectralClassFromTemperature(Material.Temperature ?? UniverseAmbientTemperature);
            }
        }

        private void ConfigureSunlike(Vector3 position)
        {
            _seed = Randomizer.Instance.NextUIntInclusive();

            SpectralClass = SpectralClass.G;
            LuminosityClass = LuminosityClass.V;

            Reconstitute(position, 5778);
        }

        private double GenerateTemperature()
        {
            if (StarType == StarType.WhiteDwarf)
            {
                return Randomizer.Instance.NormalDistributionSample(16850, 600);
            }
            if (StarType == StarType.Neutron)
            {
                return Randomizer.Instance.NormalDistributionSample(600000, 133333);
            }
            if (StarType == StarType.RedGiant)
            {
                return Randomizer.Instance.NormalDistributionSample(3800, 466);
            }
            if (StarType == StarType.YellowGiant)
            {
                return Randomizer.Instance.NormalDistributionSample(7600, 800);
            }
            if (StarType == StarType.BlueGiant)
            {
                return Randomizer.Instance.PositiveNormalDistributionSample(10000, 13333);
            }
            return SpectralClass switch
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
        }

        private void GenerateLuminosityClass()
        {
            if (IsGiant)
            {
                if (Randomizer.Instance.NextDouble() <= 0.05)
                {
                    var chance = Randomizer.Instance.NextDouble();
                    if (chance <= 0.01)
                    {
                        LuminosityClass = LuminosityClass.Zero; // 0.05% overall
                    }
                    else if (chance <= 0.14)
                    {
                        LuminosityClass = LuminosityClass.Ia; // 0.65% overall
                    }
                    else if (chance <= 0.50)
                    {
                        LuminosityClass = LuminosityClass.Ib; // 1.8% overall
                    }
                    else
                    {
                        LuminosityClass = LuminosityClass.II; // 2.5% overall
                    }
                }
                else
                {
                    LuminosityClass = LuminosityClass.III;
                }
            }
            else if (StarType == StarType.WhiteDwarf)
            {
                LuminosityClass = LuminosityClass.D;
            }
            else if (StarType == StarType.Neutron)
            {
                LuminosityClass = LuminosityClass.D;
            }
            else
            {
                LuminosityClass = LuminosityClass.V;
            }
        }

        private double GetLuminosityFromRadius()
            => MathAndScience.Constants.Doubles.MathConstants.FourPI * (double)RadiusSquared * MathAndScience.Constants.Doubles.ScienceConstants.sigma * Math.Pow(Temperature, 4);

        private Number GetMass(Randomizer randomizer)
        {
            if (StarType == StarType.BrownDwarf)
            {
                return randomizer.NextNumber(new Number(2.468, 28), new Number(1.7088, 29));
            }
            if (StarType == StarType.WhiteDwarf)
            {
                return randomizer.NormalDistributionSample(1.194e30, 9.95e28);
            }
            if (StarType == StarType.Neutron)
            {
                return randomizer.NormalDistributionSample(4.4178e30, 5.174e29); // between 1.44 and 3 times solar mass
            }
            if (StarType == StarType.RedGiant)
            {
                if (LuminosityClass == LuminosityClass.Zero
                    || LuminosityClass == LuminosityClass.Ia
                    || LuminosityClass == LuminosityClass.Ib)
                {
                    return randomizer.NextNumber(new Number(1.592, 31), new Number(4.975, 31)); // Super/hypergiants
                }
                else
                {
                    return randomizer.NextNumber(new Number(5.97, 29), new Number(1.592, 31)); // (Bright)giants
                }
            }
            if (StarType == StarType.YellowGiant)
            {
                if (LuminosityClass == LuminosityClass.Zero)
                {
                    return randomizer.NextNumber(new Number(1, 31), new Number(8.96, 31)); // Hypergiants
                }
                else if (LuminosityClass == LuminosityClass.Ia
                    || LuminosityClass == LuminosityClass.Ib)
                {
                    return randomizer.NextNumber(new Number(5.97, 31), new Number(6.97, 31)); // Supergiants
                }
                else
                {
                    return randomizer.NextNumber(new Number(5.97, 29), new Number(1.592, 31)); // (Bright)giants
                }
            }
            if (StarType == StarType.BlueGiant)
            {
                if (LuminosityClass == LuminosityClass.Zero) // Hypergiants
                {
                    // Maxmium possible mass at the current luminosity.
                    var eddingtonLimit = (Number)(Luminosity / 1.23072e31 * 1.99e30);
                    if (eddingtonLimit <= _MinBlueHypergiantMass)
                    {
                        return eddingtonLimit;
                    }
                    else
                    {
                        return randomizer.NextNumber(_MinBlueHypergiantMass, eddingtonLimit);
                    }
                }
                else if (LuminosityClass == LuminosityClass.Ia
                    || LuminosityClass == LuminosityClass.Ib)
                {
                    return randomizer.NextNumber(new Number(9.95, 30), new Number(2.0895, 32)); // Supergiants
                }
                else
                {
                    return randomizer.NextNumber(new Number(3.98, 30), new Number(1.99, 31)); // (Bright)giants
                }
            }

            // Other types should not call into this method.
            throw new Exception($"{nameof(GetMass)} called for unsupported {nameof(Star)} type.");
        }

        /// <summary>
        /// A main sequence star's radius has a direct relationship to <see cref="Luminosity"/>.
        /// </summary>
        private IShape GetMainSequenceShape(Randomizer randomizer, double temperature, Vector3 position)
        {
            var d = MathAndScience.Constants.Doubles.MathConstants.FourPI * 5.67e-8 * Math.Pow(temperature, 4);
            var radius = d.IsNearlyZero() ? Number.Zero : Math.Sqrt(Luminosity / d);
            var flattening = (Number)randomizer.NormalDistributionSample(0.15, 0.05, minimum: 0);
            return new Ellipsoid(radius, radius * (1 - flattening), position);
        }

        private ISubstanceReference GetSubstance()
        {
            if (StarType == StarType.WhiteDwarf)
            {
                return CelestialSubstances.StellarMaterialWhiteDwarf;
            }
            else if (StarType == StarType.Neutron)
            {
                return Substances.All.NeutronDegenerateMatter.GetHomogeneousReference();
            }
            else if (IsPopulationII)
            {
                return CelestialSubstances.StellarMaterialPopulationII;
            }
            return CelestialSubstances.StellarMaterial;
        }

        private bool GetWillHaveGiantPlanets()
        {
            // 12% of white dwarfs have planets
            if (StarType == StarType.WhiteDwarf)
            {
                return Randomizer.Instance.NextDouble() <= 0.12;
            }

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

        private bool GetWillHaveIceGiants()
        {
            // 12% of white dwarfs have planets
            if (StarType == StarType.WhiteDwarf)
            {
                return Randomizer.Instance.NextDouble() <= 0.12;
            }

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

        private bool GetWillHaveTerrestrialPlanets()
        {
            // 12% of white dwarfs have planets
            if (StarType == StarType.WhiteDwarf)
            {
                return Randomizer.Instance.NextDouble() <= 0.12;
            }

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

        private void Reconstitute(Vector3 position, double temperature)
        {
            var randomizer = new Randomizer(_seed);

            var substance = GetSubstance();

            Number mass;
            IShape shape;
            if (StarType == StarType.BrownDwarf)
            {
                mass = GetMass(randomizer);

                var radius = (Number)randomizer.NormalDistributionSample(69911000, 3495550);
                var flattening = randomizer.NextNumber(Number.Deci);
                shape = new Ellipsoid(radius, radius * (1 - flattening), position);

                if (SpectralClass == SpectralClass.None)
                {
                    var chance = randomizer.NextDouble();
                    if (chance <= 0.29)
                    {
                        SpectralClass = SpectralClass.M; // 29%
                    }
                    else if (chance <= 0.79)
                    {
                        SpectralClass = SpectralClass.L; // 50%
                    }
                    else if (chance <= 0.99)
                    {
                        SpectralClass = SpectralClass.T; // 20%
                    }
                    else
                    {
                        SpectralClass = SpectralClass.Y; // 1%
                    }
                }

                Luminosity = GetLuminosityFromRadius();
            }
            else if (StarType == StarType.WhiteDwarf)
            {
                mass = GetMass(randomizer);

                var radius = (new Number(1.8986, 27) / mass).CubeRoot() * 69911000;
                var flattening = (Number)randomizer.NormalDistributionSample(0.15, 0.05, minimum: 0);
                shape = new Ellipsoid(radius, radius * (1 - flattening), position);

                if (SpectralClass == SpectralClass.None)
                {
                    SpectralClass = GetSpectralClassFromTemperature(temperature);
                }

                Luminosity = GetLuminosityFromRadius();
            }
            else if (StarType == StarType.Neutron)
            {
                mass = GetMass(randomizer);

                var radius = randomizer.NextNumber(1000, 2000);
                var flattening = (Number)randomizer.NormalDistributionSample(0.15, 0.05, minimum: 0);
                shape = new Ellipsoid(radius, radius * (1 - flattening), position);

                SpectralClass = SpectralClass.Other;

                Luminosity = GetLuminosityFromRadius();
            }
            else if (IsGiant)
            {
                mass = GetMass(randomizer);

                Luminosity = LuminosityClass switch
                {
                    LuminosityClass.Zero => 3.846e31 + randomizer.PositiveNormalDistributionSample(0, 3.0768e32),
                    LuminosityClass.Ia => randomizer.NormalDistributionSample(1.923e31, 3.846e29),
                    LuminosityClass.Ib => randomizer.NormalDistributionSample(3.846e30, 3.846e29),
                    LuminosityClass.II => randomizer.NormalDistributionSample(3.846e29, 2.3076e29),
                    LuminosityClass.III => randomizer.NormalDistributionSample(1.5384e29, 4.9998e28),
                    _ => 0,
                };

                shape = GetMainSequenceShape(randomizer, temperature, position);

                if (SpectralClass == SpectralClass.None)
                {
                    SpectralClass = GetSpectralClassFromTemperature(temperature);
                }
            }
            else
            {
                // Luminosity scales with temperature for main-sequence stars.
                var luminosity = Math.Pow(temperature / 5778, 5.6) * 3.846e26;

                // If a special luminosity class had been assigned, take it into account.
                if (LuminosityClass == LuminosityClass.sd)
                {
                    // Subdwarfs are 1.5 to 2 magnitudes less luminous than expected.
                    Luminosity = luminosity / randomizer.NextDouble(55, 100);
                }
                else if (LuminosityClass == LuminosityClass.IV)
                {
                    // Subgiants are 1.5 to 2 magnitudes more luminous than expected.
                    Luminosity = luminosity * randomizer.NextDouble(55, 100);
                }
                else
                {
                    Luminosity = luminosity;
                }

                shape = GetMainSequenceShape(randomizer, temperature, position);

                // Mass scales with radius for main-sequence stars, with the scale changing at around 1
                // solar mass/radius.
                mass = Number.Pow(shape.ContainingRadius / _SolarMass, shape.ContainingRadius < _SolarMass ? new Number(125, -2) : new Number(175, -2)) * new Number(1.99, 30);

                if (SpectralClass == SpectralClass.None)
                {
                    var chance = randomizer.NextDouble();
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
            }

            Material = new Material(substance, mass, shape, temperature);
        }
    }
}
