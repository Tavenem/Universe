using System.Text;
using System.Text.Json.Serialization;
using Tavenem.Chemistry;
using Tavenem.DataStorage;
using Tavenem.Randomize;
using Tavenem.Universe.Chemistry;
using Tavenem.Universe.Place;
using Tavenem.Universe.Space.Stars;

namespace Tavenem.Universe.Space;

/// <summary>
/// A stellar body.
/// </summary>
public class Star : CosmicLocation
{
    private static readonly HugeNumber _MinBlueHypergiantMass = new(7.96, 31);
    private static readonly HugeNumber _SolarMass = new(6.955, 8);

    /// <summary>
    /// The type discriminator for this type.
    /// </summary>
    public const string StarIdItemTypeName = ":Location:CosmicLocation:Star:";
    /// <summary>
    /// A built-in, read-only type discriminator.
    /// </summary>
    [JsonInclude, JsonPropertyOrder(-1)]
    public override string IdItemTypeName
    {
        get => StarIdItemTypeName;
        set { }
    }

    /// <summary>
    /// Whether this is a giant star.
    /// </summary>
    [JsonIgnore]
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
    public StarType StarType { get; private set; }

    private string? _typeName;
    /// <inheritdoc />
    [JsonIgnore]
    public override string TypeName
    {
        get
        {
            if (string.IsNullOrEmpty(_typeName))
            {
                var sb = new StringBuilder(BaseTypeName);
                if (!string.IsNullOrEmpty(TypeNameSuffix))
                {
                    sb.Append(' ').Append(TypeNameSuffix);
                }
                _typeName = sb.ToString();
            }
            return _typeName;
        }
    }

    /// <summary>
    /// A name for this main type of this star.
    /// </summary>
    protected string BaseTypeName => StarType switch
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
    /// <summary>
    /// A suffix to the <see cref="BaseTypeName"/> for this star.
    /// </summary>
    protected string? TypeNameSuffix
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
                if (LuminosityClass is LuminosityClass.sd or LuminosityClass.D)
                {
                    sb.Append(LuminosityClass.ToString());
                }

                if (SpectralClass is not SpectralClass.None and not SpectralClass.Other)
                {
                    sb.Append(SpectralClass.ToString());
                }

                // The actual luminosity class is '0' but numerical values can't be used as
                // enum labels, so this one must be converted.
                if (LuminosityClass == LuminosityClass.Zero)
                {
                    sb.Append('0');
                }
                else if (LuminosityClass is not LuminosityClass.None
                    and not LuminosityClass.sd
                    and not LuminosityClass.D
                    and not LuminosityClass.Other)
                {
                    sb.Append(LuminosityClass.ToString());
                }

                _typeNameSuffix = sb.ToString();
            }
            return _typeNameSuffix;
        }
    }

    internal bool IsHospitable => StarType switch
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
        Vector3<HugeNumber> position,
        OrbitalParameters? orbit = null,
        SpectralClass? spectralClass = null,
        LuminosityClass? luminosityClass = null,
        bool populationII = false) : base(parent?.Id, CosmicStructureType.Star)
    {
        StarType = starType;

        Configure(position, spectralClass, luminosityClass, populationII);

        if (parent is not null && !orbit.HasValue)
        {
            if (parent.StructureType is CosmicStructureType.AsteroidField
                or CosmicStructureType.OortCloud)
            {
                orbit = parent.GetAsteroidChildOrbit();
            }
            else
            {
                orbit = parent.StructureType switch
                {
                    CosmicStructureType.GalaxySubgroup => Position == Vector3<HugeNumber>.Zero
                        ? null
                        : parent.GetGalaxySubgroupChildOrbit(),
                    CosmicStructureType.SpiralGalaxy
                        or CosmicStructureType.EllipticalGalaxy
                        or CosmicStructureType.DwarfGalaxy => Position == Vector3<HugeNumber>.Zero
                        ? null
                        : parent.GetGalaxyChildOrbit(),
                    CosmicStructureType.GlobularCluster => Position == Vector3<HugeNumber>.Zero
                        ? null
                        : parent.GetGlobularClusterChildOrbit(),
                    CosmicStructureType.StarSystem => parent is StarSystem && Position != Vector3<HugeNumber>.Zero
                        ? OrbitalParameters.GetFromEccentricity(parent.Mass, parent.Position, Randomizer.Instance.PositiveNormalDistributionSample(0, 0.05))
                        : null,
                    _ => null,
                };
            }
        }
        if (orbit.HasValue)
        {
            Space.Orbit.AssignOrbit(this, null, orbit.Value);
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
        Vector3<HugeNumber> position,
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

    /// <summary>
    /// Initialize a new instance of <see cref="Star"/>.
    /// </summary>
    /// <param name="id">
    /// The unique ID of this item.
    /// </param>
    /// <param name="starType">
    /// The <see cref="StarType"/> of this star.
    /// </param>
    /// <param name="parentId">
    /// The ID of the location which contains this one.
    /// </param>
    /// <param name="absolutePosition">
    /// <para>
    /// The position of this location, as a set of relative positions starting with the position of
    /// its outermost containing parent within the universe, down to the relative position of its
    /// most immediate parent.
    /// </para>
    /// <para>
    /// The location's own relative <see cref="Location.Position"/> is not expected to be included.
    /// </para>
    /// <para>
    /// May be <see langword="null"/> for a location with no containing parent, or whose parent is
    /// the universe itself (i.e. there is no intermediate container).
    /// </para>
    /// </param>
    /// <param name="name">
    /// An optional name for this <see cref="CosmicLocation"/>.
    /// </param>
    /// <param name="velocity">
    /// The velocity of the <see cref="CosmicLocation"/> in m/s.
    /// </param>
    /// <param name="orbit">
    /// The orbit occupied by this <see cref="CosmicLocation"/> (may be <see langword="null"/>).
    /// </param>
    /// <param name="material">The physical material which comprises this location.</param>
    /// <param name="isPopulationII">
    /// Whether this is to be a Population II <see cref="Star"/>.
    /// </param>
    /// <param name="luminosity">
    /// The luminosity of this <see cref="Star"/>, in Watts.
    /// </param>
    /// <param name="luminosityClass">
    /// The <see cref="Stars.LuminosityClass"/> of the <see cref="Star"/>.
    /// </param>
    /// <param name="spectralClass">
    /// The <see cref="Stars.SpectralClass"/> of the <see cref="Star"/>.
    /// </param>
    /// <remarks>
    /// Note: this constructor is most useful for deserialization. Consider using another
    /// constructor to generate a new instance instead.
    /// </remarks>
    [JsonConstructor]
    public Star(
        string id,
        StarType starType,
        string? parentId,
        Vector3<HugeNumber>[]? absolutePosition,
        string? name,
        Vector3<HugeNumber> velocity,
        Orbit? orbit,
        IMaterial<HugeNumber> material,
        bool isPopulationII,
        double luminosity,
        LuminosityClass luminosityClass,
        SpectralClass spectralClass)
        : base(
            id,
            CosmicStructureType.Star,
            parentId,
            absolutePosition,
            name,
            velocity,
            orbit,
            material)
    {
        StarType = starType;
        IsPopulationII = isPopulationII;
        Luminosity = luminosity;
        LuminosityClass = luminosityClass;
        SpectralClass = spectralClass;
    }

    private Star(string? parentId, StarType starType) : base(parentId, CosmicStructureType.Star) => StarType = starType;

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
        Vector3<HugeNumber> position,
        OrbitalParameters? orbit = null)
    {
        var instance = new Star(parent?.Id, StarType.MainSequence);

        instance.ConfigureSunlike(position);

        if (parent is not null && !orbit.HasValue)
        {
            if (parent.StructureType is CosmicStructureType.AsteroidField
                or CosmicStructureType.OortCloud)
            {
                orbit = parent.GetAsteroidChildOrbit();
            }
            else
            {
                orbit = parent.StructureType switch
                {
                    CosmicStructureType.GalaxySubgroup => instance.Position == Vector3<HugeNumber>.Zero
                        ? null
                        : parent.GetGalaxySubgroupChildOrbit(),
                    CosmicStructureType.SpiralGalaxy
                        or CosmicStructureType.EllipticalGalaxy
                        or CosmicStructureType.DwarfGalaxy => instance.Position == Vector3<HugeNumber>.Zero
                        ? null
                        : parent.GetGalaxyChildOrbit(),
                    CosmicStructureType.GlobularCluster => instance.Position == Vector3<HugeNumber>.Zero
                        ? null
                        : parent.GetGlobularClusterChildOrbit(),
                    CosmicStructureType.StarSystem => parent is StarSystem && instance.Position != Vector3<HugeNumber>.Zero
                        ? OrbitalParameters.GetFromEccentricity(parent.Mass, parent.Position, Randomizer.Instance.PositiveNormalDistributionSample(0, 0.05))
                        : null,
                    _ => null,
                };
            }
        }
        if (orbit.HasValue)
        {
            Space.Orbit.AssignOrbit(instance, null, orbit.Value);
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
            await system.RemoveStarAsync(dataStore, Id);
            var success = await dataStore.StoreItemAsync(system).ConfigureAwait(false);
            if (!success)
            {
                return false;
            }
        }
        return await base.DeleteAsync(dataStore).ConfigureAwait(false);
    }

    /// <summary>
    /// Changes the luminosity of this star. Also updates all related properties to conform with the
    /// new value. This may drastically alter the size and temperature of the star.
    /// </summary>
    /// <param name="dataStore">
    /// The <see cref="IDataStore"/> from which to retrieve instances.
    /// </param>
    /// <param name="value">The luminosity to set, in Watts.</param>
    /// <returns>
    /// A <see cref="List{T}"/> of the <see cref="CosmicLocation"/> instances affected by the
    /// change.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Note that side effects of the changes to the star which would result from this alteration
    /// are applied to planets in the system. For example, the temperature, atmosphere, and
    /// habitability of planets in orbit are reassessed based on the star's new state.
    /// </para>
    /// <para>
    /// This does not persist any changes to the given <paramref name="dataStore"/>. It is used
    /// merely to retrieve and update location instances affected by the change.
    /// </para>
    /// </remarks>
    public async Task<List<CosmicLocation>> SetLuminosityAsync(IDataStore dataStore, double value)
    {
        if (Luminosity == value)
        {
            return new();
        }

        Luminosity = value;

        if (StarType is StarType.BrownDwarf
            or StarType.WhiteDwarf
            or StarType.Neutron)
        {
            var radius = GetRadiusFromLuminosity();
            var ratio = radius / Shape.ContainingRadius;
            Material = new Material<HugeNumber>(
                Material.Constituents,
                Shape.GetScaledByDimension(ratio),
                Material.Density,
                Temperature);
        }
        else if (IsGiant)
        {
            LuminosityClass = Luminosity switch
            {
                > 3e31 => LuminosityClass.Zero,
                > 1e31 => LuminosityClass.Ia,
                > 2e30 => LuminosityClass.Ib,
                > 3.5e29 => LuminosityClass.II,
                _ => LuminosityClass.III,
            };
            Material = new Material<HugeNumber>(
                Material.Constituents,
                GetMainSequenceShape(Temperature, Position),
                Material.Density,
                Temperature);
        }
        else
        {
            var shape = GetMainSequenceShape(Temperature, Position);
            var mass = HugeNumber.Pow(
                shape.ContainingRadius / _SolarMass,
                shape.ContainingRadius < _SolarMass
                    ? new HugeNumber(125, -2)
                    : new HugeNumber(175, -2)) * new HugeNumber(1.99, 30);
            Material = new Material<HugeNumber>(
                Material.Constituents,
                shape,
                mass,
                null,
                Temperature);
        }

        return await GetAffectedLocationsAsync(dataStore);
    }

    /// <summary>
    /// Changes the luminosity class of this star. Also updates all related properties to conform
    /// with the new class. This may drastically alter the size and temperature of the star.
    /// </summary>
    /// <param name="dataStore">
    /// The <see cref="IDataStore"/> from which to retrieve instances.
    /// </param>
    /// <param name="value">The luminosity class to set.</param>
    /// <returns>
    /// A <see cref="List{T}"/> of the <see cref="CosmicLocation"/> instances affected by the
    /// change.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Note that the <see cref="StarType"/> will also change if the selected class does not
    /// correspond to the current type. In ambiguous cases (e.g. <see cref="LuminosityClass.III"/>,
    /// which might be any color of giant) the type is selected at random.
    /// </para>
    /// <para>
    /// Note that side effects of the changes to the star which would result from this alteration
    /// are applied to planets in the system. For example, the temperature, atmosphere, and
    /// habitability of planets in orbit are reassessed based on the star's new state.
    /// </para>
    /// <para>
    /// This does not persist any changes to the given <paramref name="dataStore"/>. It is used
    /// merely to retrieve and update location instances affected by the change.
    /// </para>
    /// </remarks>
    public async Task<List<CosmicLocation>> SetLuminosityClassAsync(IDataStore dataStore, LuminosityClass value)
    {
        if (LuminosityClass == value)
        {
            return new();
        }

        LuminosityClass = value;

        if (LuminosityClass == LuminosityClass.D)
        {
            if (StarType is not StarType.WhiteDwarf
                and not StarType.Neutron)
            {
                StarType = Randomizer.Instance.Next(2) switch
                {
                    0 => StarType.WhiteDwarf,
                    _ => StarType.Neutron,
                };
            }
        }
        else if (LuminosityClass is LuminosityClass.V
            or LuminosityClass.IV
            or LuminosityClass.sd)
        {
            if (StarType is not StarType.MainSequence
                and not StarType.BrownDwarf)
            {
                StarType = StarType.MainSequence;
            }
        }
        else if (LuminosityClass is LuminosityClass.Zero
            or LuminosityClass.Ia
            or LuminosityClass.Ib
            or LuminosityClass.II
            or LuminosityClass.III)
        {
            if (!IsGiant)
            {
                StarType = Randomizer.Instance.Next(3) switch
                {
                    0 => StarType.RedGiant,
                    1 => StarType.YellowGiant,
                    _ => StarType.BlueGiant,
                };
            }
        }

        if (IsGiant)
        {
            Luminosity = LuminosityClass switch
            {
                LuminosityClass.Zero => 3.846e31 + Randomizer.Instance.PositiveNormalDistributionSample(0, 3.0768e32),
                LuminosityClass.Ia => Randomizer.Instance.NormalDistributionSample(1.923e31, 3.846e29),
                LuminosityClass.Ib => Randomizer.Instance.NormalDistributionSample(3.846e30, 3.846e29),
                LuminosityClass.II => Randomizer.Instance.PositiveNormalDistributionSample(3.846e29, 2.3076e29),
                LuminosityClass.III => Randomizer.Instance.NormalDistributionSample(1.5384e29, 4.9998e28),
                _ => 0,
            };
            Material = new Material<HugeNumber>(
                Material.Constituents,
                GetMainSequenceShape(Temperature, Position),
                Material.Density,
                Temperature);
        }
        else if (StarType is not StarType.BrownDwarf
            and not StarType.WhiteDwarf
            and not StarType.Neutron)
        {
            // Luminosity scales with temperature for main-sequence stars.
            var luminosity = Math.Pow(Temperature / 5778, 5.6) * 3.846e26;

            // If a special luminosity class had been assigned, take it into account.
            if (LuminosityClass == LuminosityClass.sd)
            {
                // Subdwarfs are 1.5 to 2 magnitudes less luminous than expected.
                Luminosity = luminosity / Randomizer.Instance.NextDouble(55, 100);
            }
            else if (LuminosityClass == LuminosityClass.IV)
            {
                // Subgiants are 1.5 to 2 magnitudes more luminous than expected.
                Luminosity = luminosity * Randomizer.Instance.NextDouble(55, 100);
            }
            else
            {
                Luminosity = luminosity;
            }

            var shape = GetMainSequenceShape(Temperature, Position);
            var mass = HugeNumber.Pow(
                shape.ContainingRadius / _SolarMass,
                shape.ContainingRadius < _SolarMass
                    ? new HugeNumber(125, -2)
                    : new HugeNumber(175, -2)) * new HugeNumber(1.99, 30);
            Material = new Material<HugeNumber>(
                Material.Constituents,
                shape,
                mass,
                null,
                Temperature);
        }

        return await GetAffectedLocationsAsync(dataStore);
    }

    /// <summary>
    /// Changes the population type of this star. Also updates all related properties to conform
    /// with the new type.
    /// </summary>
    /// <param name="dataStore">
    /// The <see cref="IDataStore"/> from which to retrieve instances.
    /// </param>
    /// <param name="isPopulationII">
    /// Whether this is to be a Population II <see cref="Star"/>.
    /// </param>
    /// <returns>
    /// A <see cref="List{T}"/> of the <see cref="CosmicLocation"/> instances affected by the
    /// change.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Note that existing planets in the system are not removed, even if <paramref
    /// name="isPopulationII"/> is <see langword="true"/> and any of the planets are not normally
    /// indicated for a population II star.
    /// </para>
    /// <para>
    /// This does not persist any changes to the given <paramref name="dataStore"/>. It is used
    /// merely to retrieve and update location instances affected by the change.
    /// </para>
    /// </remarks>
    public async Task<List<CosmicLocation>> SetPopulationII(IDataStore dataStore, bool isPopulationII)
    {
        if (IsPopulationII == isPopulationII)
        {
            return new();
        }

        IsPopulationII = isPopulationII;
        if (StarType is not StarType.WhiteDwarf
            and not StarType.Neutron)
        {
            var substance = GetSubstance();
            Material = new Material<HugeNumber>(
                substance,
                Shape,
                Mass,
                null,
                Temperature);
        }

        var affectedLocations = new List<CosmicLocation> { this };
        if (Orbit.HasValue
            && await GetParentAsync(dataStore) is StarSystem system)
        {
            system.SetPropertiesForPrimary(this);
            affectedLocations.Add(system);
        }

        return affectedLocations;
    }

    /// <summary>
    /// Changes the spectral class of this star. Also updates all related properties to conform
    /// with the new class. This may drastically alter the size and temperature of the star.
    /// </summary>
    /// <param name="dataStore">
    /// The <see cref="IDataStore"/> from which to retrieve instances.
    /// </param>
    /// <param name="value">The spectral class to set.</param>
    /// <returns>
    /// A <see cref="List{T}"/> of the <see cref="CosmicLocation"/> instances affected by the
    /// change.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Note that side effects of the changes to the star which would result from this alteration
    /// are applied to planets in the system. For example, the temperature, atmosphere, and
    /// habitability of planets in orbit are reassessed based on the star's new state.
    /// </para>
    /// <para>
    /// This does not persist any changes to the given <paramref name="dataStore"/>. It is used
    /// merely to retrieve and update location instances affected by the change.
    /// </para>
    /// </remarks>
    public async Task<List<CosmicLocation>> SetSpectralClassAsync(IDataStore dataStore, SpectralClass value)
    {
        if (SpectralClass == value)
        {
            return new();
        }

        SpectralClass = value;

        if (SpectralClass == SpectralClass.Other)
        {
            StarType = StarType.Neutron;
            Configure(
                Position,
                GenerateTemperature(StarType, SpectralClass));
        }
        else if (StarType == StarType.BrownDwarf)
        {
            if (SpectralClass is not SpectralClass.M
                and not SpectralClass.L
                and not SpectralClass.T
                and not SpectralClass.Y)
            {
                StarType = StarType.MainSequence;
                Configure(
                    Position,
                    GenerateTemperature(StarType, SpectralClass));
            }
        }
        else if (StarType == StarType.WhiteDwarf
            || IsGiant)
        {
            var temperature = value switch
            {
                SpectralClass.Y => Temperature >= 500
                    ? 499
                    : Temperature,
                SpectralClass.T => Temperature >= 1300
                    ? 1299
                    : Temperature,
                SpectralClass.L => Temperature >= 2400
                    ? 2399
                    : Temperature,
                SpectralClass.M => Temperature >= 3700
                    ? 3699
                    : Temperature,
                SpectralClass.K => Temperature >= 5200
                    ? 5199
                    : Temperature,
                SpectralClass.G => Temperature >= 6000
                    ? 5999
                    : Temperature,
                SpectralClass.F => Temperature >= 7500
                    ? 7499
                    : Temperature,
                SpectralClass.A => Temperature >= 10000
                    ? 9999
                    : Temperature,
                SpectralClass.B => Temperature >= 30000
                    ? 29999
                    : Temperature,
                _ => Temperature
            };
            if (IsGiant)
            {
                Material = new Material<HugeNumber>(
                    Material.Constituents,
                    GetMainSequenceShape(temperature, Position),
                    Material.Density,
                    temperature);
            }
            else
            {
                Material = new Material<HugeNumber>(
                    Material.Constituents,
                    Shape,
                    Mass,
                    Material.Density,
                    temperature);
            }
        }

        return await GetAffectedLocationsAsync(dataStore);
    }

    /// <summary>
    /// Changes the type of this star. Also updates all related properties to conform with the new
    /// type. This may drastically alter the size, composition, temperature, and luminosity of the
    /// star.
    /// </summary>
    /// <param name="dataStore">
    /// The <see cref="IDataStore"/> from which to retrieve instances.
    /// </param>
    /// <param name="type">The type to set.</param>
    /// <returns>
    /// A <see cref="List{T}"/> of the <see cref="CosmicLocation"/> instances affected by the
    /// change.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Note that side effects of the changes to the star which would result from this alteration
    /// are applied to planets in the system. For example, the temperature, atmosphere, and
    /// habitability of planets in orbit are reassessed based on the star's new state.
    /// </para>
    /// <para>
    /// This does not persist any changes to the given <paramref name="dataStore"/>. It is used
    /// merely to retrieve and update location instances affected by the change.
    /// </para>
    /// </remarks>
    public async Task<List<CosmicLocation>> SetStarTypeAsync(IDataStore dataStore, StarType type)
    {
        if (StarType == type)
        {
            return new();
        }

        StarType = type;
        Configure(
            Position,
            GenerateTemperature(StarType, SpectralClass));

        return await GetAffectedLocationsAsync(dataStore);
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

    private static double GenerateTemperature(StarType starType, SpectralClass spectralClass)
    {
        if (starType == StarType.WhiteDwarf)
        {
            return Randomizer.Instance.NormalDistributionSample(16850, 600);
        }
        if (starType == StarType.Neutron)
        {
            return Randomizer.Instance.NormalDistributionSample(600000, 133333);
        }
        if (starType == StarType.RedGiant)
        {
            return Randomizer.Instance.NormalDistributionSample(3800, 466);
        }
        if (starType == StarType.YellowGiant)
        {
            return Randomizer.Instance.NormalDistributionSample(7600, 800);
        }
        if (starType == StarType.BlueGiant)
        {
            return Randomizer.Instance.PositiveNormalDistributionSample(10000, 13333);
        }
        return spectralClass switch
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

    private async Task<List<CosmicLocation>> GetAffectedLocationsAsync(IDataStore dataStore)
    {
        var affectedLocations = new List<CosmicLocation> { this };
        if (await GetParentAsync(dataStore) is not StarSystem system)
        {
            return affectedLocations;
        }

        if (!Orbit.HasValue)
        {
            system.SetPropertiesForPrimary(this);
            affectedLocations.Add(system);
        }

        var stars = new List<Star>();
        await foreach (var star in system.GetStarsAsync(dataStore))
        {
            stars.Add(star);
        }

        var updated = false;
        do
        {
            updated = false;
            foreach (var star in stars)
            {
                if (!star.Orbit.HasValue
                    || affectedLocations.Contains(star))
                {
                    continue;
                }
                var orbited = string.IsNullOrEmpty(star.Orbit.Value.OrbitedId)
                    ? null
                    : stars.Find(x => x.Id.Equals(star.Orbit.Value.OrbitedId));
                if (orbited is null)
                {
                    continue;
                }
                Space.Orbit.AssignOrbit(
                    star,
                    star.Orbit.Value.OrbitedId,
                    star.Orbit.Value.GetOrbitalParameters() with
                    {
                        OrbitedMass = Mass,
                    });
                affectedLocations.Add(star);
                updated = true;
            }
        } while (updated);

        var planetoids = new List<Planetoid>();
        await foreach (var child in system.GetChildrenAsync(dataStore, CosmicStructureType.Planetoid))
        {
            if (child is Planetoid planetoid)
            {
                planetoids.Add(planetoid);
            }
        }

        foreach (var planetoid in planetoids)
        {
            Star? star = null;
            CosmicLocation? orbited = null;
            if (!string.IsNullOrEmpty(planetoid.Orbit?.OrbitedId))
            {
                star = stars.Find(x => x.Id.Equals(planetoid.Orbit.Value.OrbitedId));
                orbited = (CosmicLocation?)star
                    ?? planetoids.Find(x => x.Id.Equals(planetoid.Orbit.Value.OrbitedId));
            }
            affectedLocations.Add(planetoid);
            affectedLocations.AddRange(planetoid.ConfigureStellarProperties(
                system,
                stars,
                star,
                orbited is not Star,
                temperatureCorrection: false));
        }

        return affectedLocations;
    }

    private static SpectralClass GetSpectralClassFromTemperature(HugeNumber temperature)
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
        Vector3<HugeNumber> position,
        SpectralClass? spectralClass = null,
        LuminosityClass? luminosityClass = null,
        bool populationII = false)
    {
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

        var temperature = GenerateTemperature(StarType, SpectralClass);

        Configure(position, temperature);

        if (SpectralClass == SpectralClass.None
            && (StarType == StarType.WhiteDwarf
                || IsGiant))
        {
            SpectralClass = GetSpectralClassFromTemperature(Material.Temperature ?? UniverseAmbientTemperature);
        }
    }

    private void Configure(Vector3<HugeNumber> position, double temperature)
    {
        var substance = GetSubstance();

        HugeNumber mass;
        IShape<HugeNumber> shape;
        if (StarType == StarType.BrownDwarf)
        {
            mass = GetMass();

            var radius = (HugeNumber)Randomizer.Instance.NormalDistributionSample(69911000, 3495550);
            var flattening = Randomizer.Instance.Next(HugeNumberConstants.Deci);
            shape = new Ellipsoid<HugeNumber>(radius, radius * (1 - flattening), position);

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

            Luminosity = GetLuminosityFromRadius();
        }
        else if (StarType == StarType.WhiteDwarf)
        {
            mass = GetMass();

            var radius = (new HugeNumber(1.8986, 27) / mass).Cbrt() * 69911000;
            var flattening = (HugeNumber)Randomizer.Instance.NormalDistributionSample(0.15, 0.05, minimum: 0);
            shape = new Ellipsoid<HugeNumber>(radius, radius * (1 - flattening), position);

            if (SpectralClass == SpectralClass.None)
            {
                SpectralClass = GetSpectralClassFromTemperature(temperature);
            }

            Luminosity = GetLuminosityFromRadius();
        }
        else if (StarType == StarType.Neutron)
        {
            mass = GetMass();

            var radius = Randomizer.Instance.Next(1000, 2000);
            var flattening = (HugeNumber)Randomizer.Instance.NormalDistributionSample(0.15, 0.05, minimum: 0);
            shape = new Ellipsoid<HugeNumber>(radius, radius * (1 - flattening), position);

            SpectralClass = SpectralClass.Other;

            Luminosity = GetLuminosityFromRadius();
        }
        else if (IsGiant)
        {
            mass = GetMass();

            Luminosity = LuminosityClass switch
            {
                LuminosityClass.Zero => 3.846e31 + Randomizer.Instance.PositiveNormalDistributionSample(0, 3.0768e32),
                LuminosityClass.Ia => Randomizer.Instance.NormalDistributionSample(1.923e31, 3.846e29),
                LuminosityClass.Ib => Randomizer.Instance.NormalDistributionSample(3.846e30, 3.846e29),
                LuminosityClass.II => Randomizer.Instance.PositiveNormalDistributionSample(3.846e29, 2.3076e29),
                LuminosityClass.III => Randomizer.Instance.NormalDistributionSample(1.5384e29, 4.9998e28),
                _ => 0,
            };

            shape = GetMainSequenceShape(temperature, position);

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
                Luminosity = luminosity / Randomizer.Instance.NextDouble(55, 100);
            }
            else if (LuminosityClass == LuminosityClass.IV)
            {
                // Subgiants are 1.5 to 2 magnitudes more luminous than expected.
                Luminosity = luminosity * Randomizer.Instance.NextDouble(55, 100);
            }
            else
            {
                Luminosity = luminosity;
            }

            shape = GetMainSequenceShape(temperature, position);

            // Mass scales with radius for main-sequence stars, with the scale changing at around 1
            // solar mass/radius.
            mass = HugeNumber.Pow(
                shape.ContainingRadius / _SolarMass,
                shape.ContainingRadius < _SolarMass
                    ? new HugeNumber(125, -2)
                    : new HugeNumber(175, -2)) * new HugeNumber(1.99, 30);

            if (SpectralClass == SpectralClass.None)
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

        Material = new Material<HugeNumber>(substance, shape, mass, null, temperature);
    }

    private void ConfigureSunlike(Vector3<HugeNumber> position)
    {
        SpectralClass = SpectralClass.G;
        LuminosityClass = LuminosityClass.V;

        Configure(position, 5778);
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
        => DoubleConstants.FourPi * (double)RadiusSquared * DoubleConstants.sigma * Math.Pow(Temperature, 4);

    private HugeNumber GetMass()
    {
        if (StarType == StarType.BrownDwarf)
        {
            return Randomizer.Instance.Next(new HugeNumber(2.468, 28), new HugeNumber(1.7088, 29));
        }
        if (StarType == StarType.WhiteDwarf)
        {
            return Randomizer.Instance.NormalDistributionSample(1.194e30, 9.95e28);
        }
        if (StarType == StarType.Neutron)
        {
            return Randomizer.Instance.NormalDistributionSample(4.4178e30, 5.174e29); // between 1.44 and 3 times solar mass
        }
        if (StarType == StarType.RedGiant)
        {
            if (LuminosityClass is LuminosityClass.Zero
                or LuminosityClass.Ia
                or LuminosityClass.Ib)
            {
                return Randomizer.Instance.Next(new HugeNumber(1.592, 31), new HugeNumber(4.975, 31)); // Super/hypergiants
            }
            else
            {
                return Randomizer.Instance.Next(new HugeNumber(5.97, 29), new HugeNumber(1.592, 31)); // (Bright)giants
            }
        }
        if (StarType == StarType.YellowGiant)
        {
            if (LuminosityClass == LuminosityClass.Zero)
            {
                return Randomizer.Instance.Next(new HugeNumber(1, 31), new HugeNumber(8.96, 31)); // Hypergiants
            }
            else if (LuminosityClass is LuminosityClass.Ia
                or LuminosityClass.Ib)
            {
                return Randomizer.Instance.Next(new HugeNumber(5.97, 31), new HugeNumber(6.97, 31)); // Supergiants
            }
            else
            {
                return Randomizer.Instance.Next(new HugeNumber(5.97, 29), new HugeNumber(1.592, 31)); // (Bright)giants
            }
        }
        if (StarType == StarType.BlueGiant)
        {
            if (LuminosityClass == LuminosityClass.Zero) // Hypergiants
            {
                // Maximum possible mass at the current luminosity.
                var eddingtonLimit = (HugeNumber)(Luminosity / 1.23072e31 * 1.99e30);
                if (eddingtonLimit <= _MinBlueHypergiantMass)
                {
                    return eddingtonLimit;
                }
                else
                {
                    return Randomizer.Instance.Next(_MinBlueHypergiantMass, eddingtonLimit);
                }
            }
            else if (LuminosityClass is LuminosityClass.Ia
                or LuminosityClass.Ib)
            {
                return Randomizer.Instance.Next(new HugeNumber(9.95, 30), new HugeNumber(2.0895, 32)); // Supergiants
            }
            else
            {
                return Randomizer.Instance.Next(new HugeNumber(3.98, 30), new HugeNumber(1.99, 31)); // (Bright)giants
            }
        }

        // Other types should not call into this method.
        throw new Exception($"{nameof(GetMass)} called for unsupported {nameof(Star)} type.");
    }

    /// <summary>
    /// A main sequence star's radius has a direct relationship to <see cref="Luminosity"/>.
    /// </summary>
    private IShape<HugeNumber> GetMainSequenceShape(double temperature, Vector3<HugeNumber> position)
    {
        var d = DoubleConstants.FourPi * 5.67e-8 * Math.Pow(temperature, 4);
        var radius = d.IsNearlyZero() ? HugeNumber.Zero : Math.Sqrt(Luminosity / d);
        var flattening = (HugeNumber)Randomizer.Instance.NormalDistributionSample(0.15, 0.05, minimum: 0);
        return new Ellipsoid<HugeNumber>(radius, radius * (1 - flattening), position);
    }

    private double GetRadiusFromLuminosity()
        => Math.Sqrt(Luminosity / DoubleConstants.FourPi * DoubleConstants.sigma * Math.Pow(Temperature, 4));

    private ISubstanceReference GetSubstance()
    {
        if (StarType == StarType.WhiteDwarf)
        {
            return CosmicSubstances.StellarMaterialWhiteDwarf;
        }
        else if (StarType == StarType.Neutron)
        {
            return Substances.All.NeutronDegenerateMatter.GetHomogeneousReference();
        }
        else if (IsPopulationII)
        {
            return CosmicSubstances.StellarMaterialPopulationII;
        }
        return CosmicSubstances.StellarMaterial;
    }

    private bool GetWillHaveGiantPlanets()
    {
        // 12% of white dwarfs have planets
        if (StarType == StarType.WhiteDwarf)
        {
            return Randomizer.Instance.NextDouble() <= 0.12;
        }

        // O-type stars and brown dwarfs do not have giant planets
        if (SpectralClass is SpectralClass.O
            or SpectralClass.L
            or SpectralClass.T
            or SpectralClass.Y)
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
        else if (SpectralClass is SpectralClass.F
            or SpectralClass.G
            or SpectralClass.K)
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
        if (SpectralClass is SpectralClass.O
            or SpectralClass.L
            or SpectralClass.T
            or SpectralClass.Y)
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
        else if (SpectralClass is SpectralClass.F
            or SpectralClass.G
            or SpectralClass.K)
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
        else if (SpectralClass is SpectralClass.F
            or SpectralClass.G
            or SpectralClass.K)
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
