using System.Text.Json.Serialization;
using Tavenem.Chemistry;
using Tavenem.Chemistry.Elements;
using Tavenem.DataStorage;
using Tavenem.Universe.Chemistry;
using Tavenem.Universe.Place;
using Tavenem.Universe.Space;
using Tavenem.Universe.Space.Planetoids;

namespace Tavenem.Universe;

/// <summary>
/// A <see cref="JsonSerializerContext"/> for <c>Tavenem.Universe</c>
/// </summary>
[JsonSerializable(typeof(IIdItem))]
[JsonSerializable(typeof(ElectronConfiguration))]
[JsonSerializable(typeof(Element))]
[JsonSerializable(typeof(Isotope))]
[JsonSerializable(typeof(Orbital))]
[JsonSerializable(typeof(PeriodicTable))]
[JsonSerializable(typeof(Chemical))]
[JsonSerializable(typeof(Composite<HugeNumber>))]
[JsonSerializable(typeof(Formula))]
[JsonSerializable(typeof(HomogeneousReference))]
[JsonSerializable(typeof(HomogeneousSubstance))]
[JsonSerializable(typeof(IHomogeneous))]
[JsonSerializable(typeof(IMaterial<HugeNumber>))]
[JsonSerializable(typeof(ISubstance))]
[JsonSerializable(typeof(ISubstanceReference))]
[JsonSerializable(typeof(Material<HugeNumber>))]
[JsonSerializable(typeof(Mixture))]
[JsonSerializable(typeof(Solution))]
[JsonSerializable(typeof(SubstanceReference))]
[JsonSerializable(typeof(Capsule<HugeNumber>))]
[JsonSerializable(typeof(Cone<HugeNumber>))]
[JsonSerializable(typeof(Cuboid<HugeNumber>))]
[JsonSerializable(typeof(Cylinder<HugeNumber>))]
[JsonSerializable(typeof(Ellipsoid<HugeNumber>))]
[JsonSerializable(typeof(Frustum<HugeNumber>))]
[JsonSerializable(typeof(HollowSphere<HugeNumber>))]
[JsonSerializable(typeof(Line<HugeNumber>))]
[JsonSerializable(typeof(SinglePoint<HugeNumber>))]
[JsonSerializable(typeof(Sphere<HugeNumber>))]
[JsonSerializable(typeof(Torus<HugeNumber>))]
[JsonSerializable(typeof(Location))]
[JsonSerializable(typeof(SurfaceRegion))]
[JsonSerializable(typeof(Territory))]
[JsonSerializable(typeof(OrbitalParameters))]
[JsonSerializable(typeof(Orbit))]
[JsonSerializable(typeof(CosmicLocation))]
[JsonSerializable(typeof(Star))]
[JsonSerializable(typeof(StarSystem))]
[JsonSerializable(typeof(SubstanceRequirement))]
[JsonSerializable(typeof(HabitabilityRequirements))]
[JsonSerializable(typeof(PlanetaryRing))]
[JsonSerializable(typeof(PlanetParams))]
[JsonSerializable(typeof(Resource))]
[JsonSerializable(typeof(SeedArray))]
[JsonSerializable(typeof(Planetoid))]
[JsonSerializable(typeof(List<CosmicLocation>))]
public partial class UniverseSourceGenerationContext
    : JsonSerializerContext
{ }
