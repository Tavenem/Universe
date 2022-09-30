using System.Text.Json.Serialization;
using Tavenem.Chemistry;
using Tavenem.Randomize;

namespace Tavenem.Universe.Space;

/// <summary>
/// A gravitational singularity.
/// </summary>
[JsonConverter(typeof(BlackHoleConverter))]
public class BlackHole : CosmicLocation
{
    internal static readonly HugeNumber _BlackHoleSpace = new(60000);
    internal static readonly HugeNumber _SupermassiveBlackHoleThreshold = new(1, 33);

    internal readonly bool _supermassive;

    private protected override string BaseTypeName => _supermassive ? "Supermassive Black Hole" : "Black Hole";

    /// <summary>
    /// The type discriminator for this type.
    /// </summary>
    public const string BlackHoleIdItemTypeName = ":Location:CosmicLocation:BlackHole:";
    /// <summary>
    /// A built-in, read-only type discriminator.
    /// </summary>
    [JsonPropertyName("$type"), JsonInclude, JsonPropertyOrder(-2)]
    public override string IdItemTypeName => BlackHoleIdItemTypeName;

    /// <summary>
    /// Initializes a new instance of <see cref="BlackHole"/> with the given parameters.
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
    /// <param name="supermassive">
    /// Whether this is to be a supermassive black hole.
    /// </param>
    public BlackHole(
        CosmicLocation? parent,
        Vector3<HugeNumber> position,
        OrbitalParameters? orbit = null,
        bool supermassive = false) : base(parent?.Id, CosmicStructureType.BlackHole)
    {
        _supermassive = supermassive;

        Configure(position);

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
            Space.Orbit.AssignOrbit(this, orbit.Value);
        }
    }

    internal BlackHole(
        string id,
        uint seed,
        string? parentId,
        Vector3<HugeNumber>[]? absolutePosition,
        string? name,
        Vector3<HugeNumber> velocity,
        Orbit? orbit,
        Vector3<HugeNumber> position,
        bool supermassive) : base(
            id,
            seed,
            CosmicStructureType.BlackHole,
            parentId,
            absolutePosition,
            name,
            velocity,
            orbit)
    {
        _supermassive = supermassive;
        Reconstitute(position);
    }

    internal static HugeNumber GetBlackHoleMassForSeed(uint seed, bool supermassive) => supermassive
        ? new Randomizer(seed).Next(new HugeNumber(2, 35), new HugeNumber(2, 40)) // ~10e5–10e10 solar masses
        : new Randomizer(seed).Next(new HugeNumber(6, 30), new HugeNumber(4, 31)); // ~3–20 solar masses

    private void Configure(Vector3<HugeNumber> position)
    {
        Seed = Randomizer.Instance.NextUIntInclusive();
        Reconstitute(position);
    }

    private void Reconstitute(Vector3<HugeNumber> position)
    {
        var mass = GetBlackHoleMassForSeed(Seed, _supermassive);

        Material = new Material<HugeNumber>(
            Substances.All.Fuzzball,

            // The shape given is presumed to refer to the shape of the event horizon.
            new Sphere<HugeNumber>(HugeNumberConstants.TwoG * mass / HugeNumberConstants.SpeedOfLightSquared, position),
            mass,
            null,

            // Hawking radiation = solar mass / mass * constant
            (double)(new HugeNumber(6.169, -8) * new HugeNumber(1.98847, 30) / mass));
    }
}
