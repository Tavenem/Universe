using Tavenem.Chemistry;
using Tavenem.Randomize;
using Tavenem.Universe.Space.Stars;

namespace Tavenem.Universe.Space;

public partial class CosmicLocation
{
    private protected static readonly HugeNumber _PlanetaryNebulaSpace = new(9.5, 15);

    private Star? ConfigurePlanetaryNebulaInstance(Vector3<HugeNumber> position, double? ambientTemperature = null, CosmicLocation? child = null)
    {
        Seed = Randomizer.Instance.NextUIntInclusive();
        ReconstitutePlanetaryNebulaInstance(position, ambientTemperature ?? UniverseAmbientTemperature);

        if (child is not Star star
            || star.StarType != StarType.WhiteDwarf)
        {
            return new Star(StarType.WhiteDwarf, this, Vector3<HugeNumber>.Zero);
        }
        return null;
    }

    private void ReconstitutePlanetaryNebulaInstance(Vector3<HugeNumber> position, double? temperature)
    {
        var randomizer = new Randomizer(Seed);

        var mass = randomizer.Next(new HugeNumber(1.99, 29), new HugeNumber(1.99, 30)); // ~0.1–1 solar mass.

        Material = new Material<HugeNumber>(
            Substances.All.IonizedCloud,

            // Actual planetary nebulae are spherical only 20% of the time, but the shapes are irregular
            // and not considered critical to model precisely, especially given their extremely
            // attenuated nature. Instead, a ~1 ly sphere is used.
            new Sphere<HugeNumber>(_PlanetaryNebulaSpace, position),
            mass,
            null,
            temperature);
    }
}
