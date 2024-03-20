using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Tavenem.Chemistry;
using Tavenem.DataStorage;
using Tavenem.Universe.Place;
using Tavenem.Universe.Space;

namespace Tavenem.Universe;

/// <summary>
/// The JSON contract resolver to be used by <c>Tavenem.Universe</c>.
/// </summary>
/// <remarks>
/// Incorporates the contract resolver for <c>Tavenem.Chemistry</c>.
/// </remarks>
public class UniverseTypeResolver : DefaultJsonTypeInfoResolver
{
    private static readonly List<JsonDerivedType> _DerivedTypes =
    [
        new(typeof(Location), Location.LocationIdItemTypeName),
        new(typeof(CosmicLocation), CosmicLocation.CosmicLocationIdItemTypeName),
        new(typeof(Planetoid), Planetoid.PlanetoidIdItemTypeName),
        new(typeof(Star), Star.StarIdItemTypeName),
        new(typeof(StarSystem), StarSystem.StarSystemIdItemTypeName),
        new(typeof(SurfaceRegion), SurfaceRegion.SurfaceRegionIdItemTypeName),
        new(typeof(Territory), Territory.TerritoryIdItemTypeName),
        new(typeof(ISubstance), $":{nameof(ISubstance)}:"),
        new(typeof(Chemical), Chemical.ChemicalIdItemTypeName),
        new(typeof(HomogeneousSubstance), HomogeneousSubstance.HomogeneousSubstanceIdItemTypeName),
        new(typeof(Mixture), Mixture.MixtureIdItemTypeName),
        new(typeof(Solution), Solution.SolutionIdItemTypeName),
    ];

    /// <inheritdoc />
    public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
    {
        var jsonTypeInfo = base.GetTypeInfo(type, options);

        if (jsonTypeInfo.Type == typeof(IIdItem))
        {
            jsonTypeInfo.PolymorphismOptions ??= new JsonPolymorphismOptions
            {
                IgnoreUnrecognizedTypeDiscriminators = true,
                UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToNearestAncestor,
            };
            for (var i = 0; i < _DerivedTypes.Count; i++)
            {
                jsonTypeInfo.PolymorphismOptions.DerivedTypes.Add(_DerivedTypes[i]);
            }
        }

        return jsonTypeInfo;
    }
}
