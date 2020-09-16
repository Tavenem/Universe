using NeverFoundry.MathAndScience.Chemistry;
using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.MathAndScience.Randomization;
using NeverFoundry.WorldFoundry.Space.Stars;

namespace NeverFoundry.WorldFoundry.Space
{
    public partial class CosmicLocation
    {
        private protected static readonly Number _PlanetaryNebulaSpace = new Number(9.5, 15);

        private Star? ConfigurePlanetaryNebulaInstance(Vector3 position, double? ambientTemperature = null, CosmicLocation? child = null)
        {
            _seed = Randomizer.Instance.NextUIntInclusive();
            ReconstitutePlanetaryNebulaInstance(position, ambientTemperature ?? UniverseAmbientTemperature);

            if (child is not Star star
                || star.StarType != StarType.WhiteDwarf)
            {
                return new Star(StarType.WhiteDwarf, this, Vector3.Zero);
            }
            return null;
        }

        private void ReconstitutePlanetaryNebulaInstance(Vector3 position, double? temperature)
        {
            var randomizer = new Randomizer(_seed);

            var mass = randomizer.NextNumber(new Number(1.99, 29), new Number(1.99, 30)); // ~0.1–1 solar mass.

            Material = new Material(
                Substances.All.IonizedCloud.GetReference(),
                mass,

                // Actual planetary nebulae are spherical only 20% of the time, but the shapes are irregular
                // and not considered critical to model precisely, especially given their extremely
                // attenuated nature. Instead, a ~1 ly sphere is used.
                new Sphere(_PlanetaryNebulaSpace, position),

                temperature);
        }
    }
}
