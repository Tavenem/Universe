using System.Collections.Generic;
using Tavenem.Chemistry;
using Tavenem.Chemistry.HugeNumbers;
using Tavenem.HugeNumbers;
using Tavenem.Mathematics.HugeNumbers;
using Tavenem.Randomize;
using Tavenem.Universe.Space.Stars;

namespace Tavenem.Universe.Space
{
    public partial class CosmicLocation
    {
        private protected static readonly HugeNumber _GlobularClusterSpace = new(2.1, 7);

        private static readonly HugeNumber _GlobularClusterChildDensity = new(1.3, -17);

        private static readonly HugeNumber _GlobularClusterSystemDensity = new(1.5, -70);
        private static readonly HugeNumber _GlobularClusterMainSequenceDensity = _GlobularClusterSystemDensity * new HugeNumber(9096, -4);
        private static readonly HugeNumber _GlobularClusterRedDensity = _GlobularClusterMainSequenceDensity * new HugeNumber(7645, -4);
        private static readonly HugeNumber _GlobularClusterKDensity = _GlobularClusterMainSequenceDensity * new HugeNumber(121, -3);
        private static readonly HugeNumber _GlobularClusterGDensity = _GlobularClusterMainSequenceDensity * new HugeNumber(76, -3);
        private static readonly HugeNumber _GlobularClusterFDensity = _GlobularClusterMainSequenceDensity * new HugeNumber(3, -2);
        private static readonly HugeNumber _GlobularClusterGiantDensity = _GlobularClusterSystemDensity * new HugeNumber(5, -2);
        private static readonly HugeNumber _GlobularClusterRedGiantDensity = _GlobularClusterGiantDensity * new HugeNumber(45, -3);
        private static readonly HugeNumber _GlobularClusterBlueGiantDensity = _GlobularClusterGiantDensity * new HugeNumber(35, -3);
        private static readonly HugeNumber _GlobularClusterYellowGiantDensity = _GlobularClusterGiantDensity * new HugeNumber(2, -2);

        private static readonly List<ChildDefinition> _GlobularClusterChildDefinitions = new()
        {
            new StarSystemChildDefinition(_GlobularClusterSystemDensity / 6, StarType.BrownDwarf, populationII: true),

            new StarSystemChildDefinition(_GlobularClusterRedDensity * new HugeNumber(998, -3), SpectralClass.M, LuminosityClass.V, populationII: true),
            new StarSystemChildDefinition(_GlobularClusterRedDensity * new HugeNumber(2, -3), SpectralClass.M, LuminosityClass.sd, populationII: true),

            new StarSystemChildDefinition(_GlobularClusterKDensity * new HugeNumber(989, -3), SpectralClass.K, LuminosityClass.V, populationII: true),
            new StarSystemChildDefinition(_GlobularClusterKDensity * new HugeNumber(7, -3), SpectralClass.K, LuminosityClass.IV, populationII: true),
            new StarSystemChildDefinition(_GlobularClusterKDensity * new HugeNumber(4, -3), SpectralClass.K, LuminosityClass.sd, populationII: true),

            new StarSystemChildDefinition(_GlobularClusterSystemDensity * new HugeNumber(4, -2), StarType.WhiteDwarf, populationII: true),

            new StarSystemChildDefinition(_GlobularClusterGDensity * new HugeNumber(986, -3), SpectralClass.G, LuminosityClass.V, populationII: true),
            new StarSystemChildDefinition(_GlobularClusterGDensity * new HugeNumber(14, -3), SpectralClass.G, LuminosityClass.IV, populationII: true),

            new StarSystemChildDefinition(_GlobularClusterFDensity * new HugeNumber(982, -3), SpectralClass.F, LuminosityClass.V, populationII: true),
            new StarSystemChildDefinition(_GlobularClusterFDensity * new HugeNumber(18, -3), SpectralClass.F, LuminosityClass.IV, populationII: true),

            new StarSystemChildDefinition(_GlobularClusterSystemDensity * new HugeNumber(4, -4), StarType.Neutron),

            new ChildDefinition(BlackHole.BlackHoleSpace, _GlobularClusterSystemDensity * new HugeNumber(4, -4), CosmicStructureType.BlackHole),

            new StarSystemChildDefinition(_GlobularClusterMainSequenceDensity * new HugeNumber(6, -3), SpectralClass.A, LuminosityClass.V, populationII: true),

            new StarSystemChildDefinition(_GlobularClusterRedGiantDensity * new HugeNumber(96, -2), StarType.RedGiant, populationII: true),
            new StarSystemChildDefinition(_GlobularClusterRedGiantDensity * new HugeNumber(18, -3), StarType.RedGiant, null, LuminosityClass.II, populationII: true),
            new StarSystemChildDefinition(_GlobularClusterRedGiantDensity * new HugeNumber(16, -3), StarType.RedGiant, null, LuminosityClass.Ib, populationII: true),
            new StarSystemChildDefinition(_GlobularClusterRedGiantDensity * new HugeNumber(55, -4), StarType.RedGiant, null, LuminosityClass.Ia, populationII: true),
            new StarSystemChildDefinition(_GlobularClusterRedGiantDensity * new HugeNumber(5, -4), StarType.RedGiant, null, LuminosityClass.Zero, populationII: true),

            new StarSystemChildDefinition(_GlobularClusterBlueGiantDensity * new HugeNumber(95, -2), StarType.BlueGiant, populationII: true),
            new StarSystemChildDefinition(_GlobularClusterBlueGiantDensity * new HugeNumber(25, -3), StarType.BlueGiant, null, LuminosityClass.II, populationII: true),
            new StarSystemChildDefinition(_GlobularClusterBlueGiantDensity * new HugeNumber(2, -2), StarType.BlueGiant, null, LuminosityClass.Ib, populationII: true),
            new StarSystemChildDefinition(_GlobularClusterBlueGiantDensity * new HugeNumber(45, -4), StarType.BlueGiant, null, LuminosityClass.Ia, populationII: true),
            new StarSystemChildDefinition(_GlobularClusterBlueGiantDensity * new HugeNumber(5, -4), StarType.BlueGiant, null, LuminosityClass.Zero, populationII: true),

            new StarSystemChildDefinition(_GlobularClusterYellowGiantDensity * new HugeNumber(95, -2), StarType.YellowGiant, populationII: true),
            new StarSystemChildDefinition(_GlobularClusterYellowGiantDensity * new HugeNumber(2, -2), StarType.YellowGiant, null, LuminosityClass.II, populationII: true),
            new StarSystemChildDefinition(_GlobularClusterYellowGiantDensity * new HugeNumber(23, -3), StarType.YellowGiant, null, LuminosityClass.Ib, populationII: true),
            new StarSystemChildDefinition(_GlobularClusterYellowGiantDensity * new HugeNumber(6, -3), StarType.YellowGiant, null, LuminosityClass.Ia, populationII: true),
            new StarSystemChildDefinition(_GlobularClusterYellowGiantDensity * new HugeNumber(1, -3), StarType.YellowGiant, null, LuminosityClass.Zero, populationII: true),

            new StarSystemChildDefinition(_GlobularClusterMainSequenceDensity * new HugeNumber(13, -4), SpectralClass.B, LuminosityClass.V, populationII: true),
            new StarSystemChildDefinition(_GlobularClusterMainSequenceDensity * new HugeNumber(3, -7), SpectralClass.O, LuminosityClass.V, populationII: true),
        };

        internal OrbitalParameters GetGlobularClusterChildOrbit()
            => OrbitalParameters.GetFromEccentricity(Mass, Vector3.Zero, Randomizer.Instance.NextDouble(0.1));

        private CosmicLocation? ConfigureGlobularClusterInstance(Vector3 position, double? ambientTemperature = null, CosmicLocation? child = null)
        {
            CosmicLocation? newCore = null;
            if (child?.StructureType == CosmicStructureType.BlackHole)
            {
                Seed = child.Seed;
            }
            else
            {
                var core = new BlackHole(this, Vector3.Zero);
                Seed = core?.Seed ?? Randomizer.Instance.NextUIntInclusive();
                newCore = core;
            }

            ReconstituteGlobularClusterInstance(position, ambientTemperature ?? UniverseAmbientTemperature);

            return newCore;
        }

        private void ReconstituteGlobularClusterInstance(Vector3 position, double? temperature)
        {
            var randomizer = new Randomizer(Seed);

            var radius = Randomizer.Instance.NextNumber(new HugeNumber(8, 6), new HugeNumber(2.1, 7));
            var axis = radius * Randomizer.Instance.NormalDistributionSample(0.02, 1);
            var shape = new Ellipsoid(radius, axis, position);

            // Randomly determines a factor by which the mass of this globular cluster will be
            // multiplied due to the abundance of dark matter.
            var darkMatterMultiplier = randomizer.NextNumber(5, 15);

            var coreMass = BlackHole.GetBlackHoleMassForSeed(Seed, supermassive: false);

            var mass = ((shape.Volume * _GlobularClusterChildDensity * new HugeNumber(1, 30)) + coreMass) * darkMatterMultiplier;

            Material = new Material(
                Substances.All.InterstellarMedium,
                shape,
                mass,
                null,
                temperature);
        }
    }
}
