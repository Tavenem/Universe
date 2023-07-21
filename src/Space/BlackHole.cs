using Tavenem.Chemistry;
using Tavenem.Randomize;

namespace Tavenem.Universe.Space;

/// <summary>
/// A gravitational singularity.
/// </summary>
public partial class CosmicLocation
{
    internal static readonly HugeNumber _BlackHoleSpace = new(60000);
    internal static readonly HugeNumber _SupermassiveBlackHoleThreshold = new(1, 33);

    private void ConfigureBlackHoleInstance(Vector3<HugeNumber> position, bool supermassive = false)
    {
        var mass = supermassive
            ? Randomizer.Instance.Next(
                new HugeNumber(2, 35),
                new HugeNumber(2, 40)) // ~10e5–10e10 solar masses
            : Randomizer.Instance.Next(
                new HugeNumber(6, 30),
                new HugeNumber(4, 31)); // ~3–20 solar masses

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
