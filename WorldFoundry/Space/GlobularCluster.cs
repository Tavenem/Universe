using NeverFoundry.MathAndScience.Chemistry;
using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.MathAndScience.Randomization;
using NeverFoundry.WorldFoundry.Space.Stars;
using System.Collections.Generic;

namespace NeverFoundry.WorldFoundry.Space
{
    public partial class CosmicLocation
    {
        private protected static readonly Number _GlobularClusterSpace = new(2.1, 7);

        private static readonly Number _GlobularClusterChildDensity = new(1.3, -17);

        private static readonly Number _GlobularClusterSystemDensity = new(1.5, -70);
        private static readonly Number _GlobularClusterMainSequenceDensity = _GlobularClusterSystemDensity * new Number(9096, -4);
        private static readonly Number _GlobularClusterRedDensity = _GlobularClusterMainSequenceDensity * new Number(7645, -4);
        private static readonly Number _GlobularClusterKDensity = _GlobularClusterMainSequenceDensity * new Number(121, -3);
        private static readonly Number _GlobularClusterGDensity = _GlobularClusterMainSequenceDensity * new Number(76, -3);
        private static readonly Number _GlobularClusterFDensity = _GlobularClusterMainSequenceDensity * new Number(3, -2);
        private static readonly Number _GlobularClusterGiantDensity = _GlobularClusterSystemDensity * new Number(5, -2);
        private static readonly Number _GlobularClusterRedGiantDensity = _GlobularClusterGiantDensity * new Number(45, -3);
        private static readonly Number _GlobularClusterBlueGiantDensity = _GlobularClusterGiantDensity * new Number(35, -3);
        private static readonly Number _GlobularClusterYellowGiantDensity = _GlobularClusterGiantDensity * new Number(2, -2);

        private static readonly List<ChildDefinition> _GlobularClusterChildDefinitions = new()
        {
            new StarSystemChildDefinition(_GlobularClusterSystemDensity / 6, StarType.BrownDwarf, populationII: true),

            new StarSystemChildDefinition(_GlobularClusterRedDensity * new Number(998, -3), SpectralClass.M, LuminosityClass.V, populationII: true),
            new StarSystemChildDefinition(_GlobularClusterRedDensity * new Number(2, -3), SpectralClass.M, LuminosityClass.sd, populationII: true),

            new StarSystemChildDefinition(_GlobularClusterKDensity * new Number(989, -3), SpectralClass.K, LuminosityClass.V, populationII: true),
            new StarSystemChildDefinition(_GlobularClusterKDensity * new Number(7, -3), SpectralClass.K, LuminosityClass.IV, populationII: true),
            new StarSystemChildDefinition(_GlobularClusterKDensity * new Number(4, -3), SpectralClass.K, LuminosityClass.sd, populationII: true),

            new StarSystemChildDefinition(_GlobularClusterSystemDensity * new Number(4, -2), StarType.WhiteDwarf, populationII: true),

            new StarSystemChildDefinition(_GlobularClusterGDensity * new Number(986, -3), SpectralClass.G, LuminosityClass.V, populationII: true),
            new StarSystemChildDefinition(_GlobularClusterGDensity * new Number(14, -3), SpectralClass.G, LuminosityClass.IV, populationII: true),

            new StarSystemChildDefinition(_GlobularClusterFDensity * new Number(982, -3), SpectralClass.F, LuminosityClass.V, populationII: true),
            new StarSystemChildDefinition(_GlobularClusterFDensity * new Number(18, -3), SpectralClass.F, LuminosityClass.IV, populationII: true),

            new StarSystemChildDefinition(_GlobularClusterSystemDensity * new Number(4, -4), StarType.Neutron),

            new ChildDefinition(BlackHole.BlackHoleSpace, _GlobularClusterSystemDensity * new Number(4, -4), CosmicStructureType.BlackHole),

            new StarSystemChildDefinition(_GlobularClusterMainSequenceDensity * new Number(6, -3), SpectralClass.A, LuminosityClass.V, populationII: true),

            new StarSystemChildDefinition(_GlobularClusterRedGiantDensity * new Number(96, -2), StarType.RedGiant, populationII: true),
            new StarSystemChildDefinition(_GlobularClusterRedGiantDensity * new Number(18, -3), StarType.RedGiant, null, LuminosityClass.II, populationII: true),
            new StarSystemChildDefinition(_GlobularClusterRedGiantDensity * new Number(16, -3), StarType.RedGiant, null, LuminosityClass.Ib, populationII: true),
            new StarSystemChildDefinition(_GlobularClusterRedGiantDensity * new Number(55, -4), StarType.RedGiant, null, LuminosityClass.Ia, populationII: true),
            new StarSystemChildDefinition(_GlobularClusterRedGiantDensity * new Number(5, -4), StarType.RedGiant, null, LuminosityClass.Zero, populationII: true),

            new StarSystemChildDefinition(_GlobularClusterBlueGiantDensity * new Number(95, -2), StarType.BlueGiant, populationII: true),
            new StarSystemChildDefinition(_GlobularClusterBlueGiantDensity * new Number(25, -3), StarType.BlueGiant, null, LuminosityClass.II, populationII: true),
            new StarSystemChildDefinition(_GlobularClusterBlueGiantDensity * new Number(2, -2), StarType.BlueGiant, null, LuminosityClass.Ib, populationII: true),
            new StarSystemChildDefinition(_GlobularClusterBlueGiantDensity * new Number(45, -4), StarType.BlueGiant, null, LuminosityClass.Ia, populationII: true),
            new StarSystemChildDefinition(_GlobularClusterBlueGiantDensity * new Number(5, -4), StarType.BlueGiant, null, LuminosityClass.Zero, populationII: true),

            new StarSystemChildDefinition(_GlobularClusterYellowGiantDensity * new Number(95, -2), StarType.YellowGiant, populationII: true),
            new StarSystemChildDefinition(_GlobularClusterYellowGiantDensity * new Number(2, -2), StarType.YellowGiant, null, LuminosityClass.II, populationII: true),
            new StarSystemChildDefinition(_GlobularClusterYellowGiantDensity * new Number(23, -3), StarType.YellowGiant, null, LuminosityClass.Ib, populationII: true),
            new StarSystemChildDefinition(_GlobularClusterYellowGiantDensity * new Number(6, -3), StarType.YellowGiant, null, LuminosityClass.Ia, populationII: true),
            new StarSystemChildDefinition(_GlobularClusterYellowGiantDensity * new Number(1, -3), StarType.YellowGiant, null, LuminosityClass.Zero, populationII: true),

            new StarSystemChildDefinition(_GlobularClusterMainSequenceDensity * new Number(13, -4), SpectralClass.B, LuminosityClass.V, populationII: true),
            new StarSystemChildDefinition(_GlobularClusterMainSequenceDensity * new Number(3, -7), SpectralClass.O, LuminosityClass.V, populationII: true),
        };

        internal OrbitalParameters GetGlobularClusterChildOrbit()
            => OrbitalParameters.GetFromEccentricity(Mass, Vector3.Zero, Randomizer.Instance.NextDouble(0.1));

        private CosmicLocation? ConfigureGlobularClusterInstance(Vector3 position, double? ambientTemperature = null, CosmicLocation? child = null)
        {
            CosmicLocation? newCore = null;
            if (child is not null && child.StructureType == CosmicStructureType.BlackHole)
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

            var radius = Randomizer.Instance.NextNumber(new Number(8, 6), new Number(2.1, 7));
            var axis = radius * Randomizer.Instance.NormalDistributionSample(0.02, 1);
            var shape = new Ellipsoid(radius, axis, position);

            // Randomly determines a factor by which the mass of this globular cluster will be
            // multiplied due to the abundance of dark matter.
            var darkMatterMultiplier = randomizer.NextNumber(5, 15);

            var coreMass = BlackHole.GetBlackHoleMassForSeed(Seed, supermassive: false);

            var mass = ((shape.Volume * _GlobularClusterChildDensity * new Number(1, 30)) + coreMass) * darkMatterMultiplier;

            Material = new Material(
                Substances.All.InterstellarMedium.GetReference(),
                mass,
                shape,
                temperature);
        }
    }
}
