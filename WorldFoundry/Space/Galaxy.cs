using NeverFoundry.MathAndScience.Chemistry;
using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.MathAndScience.Randomization;
using NeverFoundry.WorldFoundry.Space.Planetoids;
using NeverFoundry.WorldFoundry.Space.Stars;
using System.Collections.Generic;

namespace NeverFoundry.WorldFoundry.Space
{
    public partial class CosmicLocation
    {
        private protected static readonly Number _DwarfGalaxySpace = new Number(2.5, 18);
        private protected static readonly Number _GalaxySpace = new Number(2.5, 22);

        private static readonly Number _GalaxySystemDensity = new Number(2.75, -73);
        private static readonly Number _GalaxyRogueDensity = _GalaxySystemDensity * 3;
        private static readonly Number _GalaxyMainSequenceDensity = _GalaxySystemDensity * new Number(9096, -4);
        private static readonly Number _GalaxyRedDensity = _GalaxyMainSequenceDensity * new Number(7645, -4);
        private static readonly Number _GalaxyKDensity = _GalaxyMainSequenceDensity * new Number(121, -3);
        private static readonly Number _GalaxyGDensity = _GalaxyMainSequenceDensity * new Number(76, -3);
        private static readonly Number _GalaxyFDensity = _GalaxyMainSequenceDensity * new Number(3, -2);
        private static readonly Number _GalaxyGiantDensity = _GalaxySystemDensity * new Number(5, -2);
        private static readonly Number _GalaxyRedGiantDensity = _GalaxyGiantDensity * new Number(45, -3);
        private static readonly Number _GalaxyBlueGiantDensity = _GalaxyGiantDensity * new Number(35, -3);
        private static readonly Number _GalaxyYellowGiantDensity = _GalaxyGiantDensity * new Number(2, -2);

        private static readonly List<ChildDefinition> _GalaxyChildDefinitions = new List<ChildDefinition>
        {
            new PlanetChildDefinition(_GalaxyRogueDensity * 5 / 12, PlanetType.GasGiant),
            new PlanetChildDefinition(_GalaxyRogueDensity * new Number(25, -2), PlanetType.IceGiant),
            new PlanetChildDefinition(_GalaxyRogueDensity / 6, PlanetType.Terrestrial),
            new PlanetChildDefinition(_GalaxyRogueDensity / 24, PlanetType.Ocean),
            new PlanetChildDefinition(_GalaxyRogueDensity / 24, PlanetType.Iron),
            new PlanetChildDefinition(_GalaxyRogueDensity / 12, PlanetType.Carbon),

            new StarSystemChildDefinition(_GalaxySystemDensity / 6, StarType.BrownDwarf),

            new StarSystemChildDefinition(_GalaxyRedDensity * new Number(998, -3), SpectralClass.M, LuminosityClass.V),
            new StarSystemChildDefinition(_GalaxyRedDensity * new Number(2, -3), SpectralClass.M, LuminosityClass.sd),

            new StarSystemChildDefinition(_GalaxyKDensity * new Number(987, -3), SpectralClass.K, LuminosityClass.V),
            new StarSystemChildDefinition(_GalaxyKDensity * new Number(1, -2), SpectralClass.K, LuminosityClass.IV),
            new StarSystemChildDefinition(_GalaxyKDensity * new Number(3, -3), SpectralClass.K, LuminosityClass.sd),

            new StarSystemChildDefinition(_GalaxySystemDensity * new Number(4, -2), StarType.WhiteDwarf),

            new StarSystemChildDefinition(_GalaxyGDensity * new Number(992, -3), SpectralClass.G, LuminosityClass.V),
            new StarSystemChildDefinition(_GalaxyGDensity * new Number(8, -3), SpectralClass.G, LuminosityClass.IV),

            new StarSystemChildDefinition(_GalaxyFDensity * new Number(982, -3), SpectralClass.F, LuminosityClass.V),
            new StarSystemChildDefinition(_GalaxyFDensity * new Number(18, -3), SpectralClass.F, LuminosityClass.IV),

            new StarSystemChildDefinition(_GalaxySystemDensity * new Number(4, -4), StarType.Neutron),

            new ChildDefinition(BlackHole.BlackHoleSpace, _GalaxySystemDensity * new Number(4, -4), CosmicStructureType.BlackHole),

            new StarSystemChildDefinition(_GalaxyMainSequenceDensity * new Number(6, -3), SpectralClass.A, LuminosityClass.V),

            new StarSystemChildDefinition(_GalaxyRedGiantDensity * new Number(96, -2), StarType.RedGiant),
            new StarSystemChildDefinition(_GalaxyRedGiantDensity * new Number(18, -3), StarType.RedGiant, null, LuminosityClass.II),
            new StarSystemChildDefinition(_GalaxyRedGiantDensity * new Number(16, -3), StarType.RedGiant, null, LuminosityClass.Ib),
            new StarSystemChildDefinition(_GalaxyRedGiantDensity * new Number(55, -4), StarType.RedGiant, null, LuminosityClass.Ia),
            new StarSystemChildDefinition(_GalaxyRedGiantDensity * new Number(5, -4), StarType.RedGiant, null, LuminosityClass.Zero),

            new StarSystemChildDefinition(_GalaxyBlueGiantDensity * new Number(95, -2), StarType.BlueGiant),
            new StarSystemChildDefinition(_GalaxyBlueGiantDensity * new Number(25, -3), StarType.BlueGiant, null, LuminosityClass.II),
            new StarSystemChildDefinition(_GalaxyBlueGiantDensity * new Number(2, -2), StarType.BlueGiant, null, LuminosityClass.Ib),
            new StarSystemChildDefinition(_GalaxyBlueGiantDensity * new Number(45, -4), StarType.BlueGiant, null, LuminosityClass.Ia),
            new StarSystemChildDefinition(_GalaxyBlueGiantDensity * new Number(5, -4), StarType.BlueGiant, null, LuminosityClass.Zero),

            new StarSystemChildDefinition(_GalaxyYellowGiantDensity * new Number(95, -2), StarType.YellowGiant),
            new StarSystemChildDefinition(_GalaxyYellowGiantDensity * new Number(2, -2), StarType.YellowGiant, null, LuminosityClass.II),
            new StarSystemChildDefinition(_GalaxyYellowGiantDensity * new Number(23, -3), StarType.YellowGiant, null, LuminosityClass.Ib),
            new StarSystemChildDefinition(_GalaxyYellowGiantDensity * new Number(6, -3), StarType.YellowGiant, null, LuminosityClass.Ia),
            new StarSystemChildDefinition(_GalaxyYellowGiantDensity * new Number(1, -3), StarType.YellowGiant, null, LuminosityClass.Zero),

            new StarSystemChildDefinition(_GalaxyMainSequenceDensity * new Number(13, -4), SpectralClass.B, LuminosityClass.V),
            new StarSystemChildDefinition(_GalaxyMainSequenceDensity * new Number(3, -7), SpectralClass.O, LuminosityClass.V),

            new ChildDefinition(_PlanetaryNebulaSpace, _GalaxySystemDensity * new Number(1.5, -8), CosmicStructureType.PlanetaryNebula),

            new ChildDefinition(_NebulaSpace, _GalaxySystemDensity * new Number(4, -10), CosmicStructureType.Nebula),

            new ChildDefinition(_NebulaSpace, _GalaxySystemDensity * new Number(4, -10), CosmicStructureType.HIIRegion),
        };

        private static readonly List<ChildDefinition> _EllipticalGalaxyChildDefinitions = new List<ChildDefinition>
        {
            new PlanetChildDefinition(_GalaxyRogueDensity * 5 / 12, PlanetType.GasGiant),
            new PlanetChildDefinition(_GalaxyRogueDensity * new Number(25, -2), PlanetType.IceGiant),
            new PlanetChildDefinition(_GalaxyRogueDensity / 6, PlanetType.Terrestrial),
            new PlanetChildDefinition(_GalaxyRogueDensity / 24, PlanetType.Ocean),
            new PlanetChildDefinition(_GalaxyRogueDensity / 24, PlanetType.Iron),
            new PlanetChildDefinition(_GalaxyRogueDensity / 12, PlanetType.Carbon),

            new StarSystemChildDefinition(_GalaxySystemDensity / 6, StarType.BrownDwarf),

            new StarSystemChildDefinition(_GalaxyRedDensity * new Number(998, -3), SpectralClass.M, LuminosityClass.V),
            new StarSystemChildDefinition(_GalaxyRedDensity * new Number(2, -3), SpectralClass.M, LuminosityClass.sd),

            new StarSystemChildDefinition(_GalaxyKDensity * new Number(987, -3), SpectralClass.K, LuminosityClass.V),
            new StarSystemChildDefinition(_GalaxyKDensity * new Number(1, -2), SpectralClass.K, LuminosityClass.IV),
            new StarSystemChildDefinition(_GalaxyKDensity * new Number(3, -3), SpectralClass.K, LuminosityClass.sd),

            new StarSystemChildDefinition(_GalaxySystemDensity * new Number(4, -2), StarType.WhiteDwarf),

            new StarSystemChildDefinition(_GalaxyGDensity * new Number(992, -3), SpectralClass.G, LuminosityClass.V),
            new StarSystemChildDefinition(_GalaxyGDensity * new Number(8, -3), SpectralClass.G, LuminosityClass.IV),

            new StarSystemChildDefinition(_GalaxyFDensity * new Number(982, -3), SpectralClass.F, LuminosityClass.V),
            new StarSystemChildDefinition(_GalaxyFDensity * new Number(18, -3), SpectralClass.F, LuminosityClass.IV),

            new StarSystemChildDefinition(_GalaxySystemDensity * new Number(4, -4), StarType.Neutron),

            new ChildDefinition(BlackHole.BlackHoleSpace, _GalaxySystemDensity * new Number(4, -4), CosmicStructureType.BlackHole),

            new StarSystemChildDefinition(_GalaxyGiantDensity * new Number(9997, -4), StarType.RedGiant),
            new StarSystemChildDefinition(_GalaxyGiantDensity * new Number(3, -4), StarType.RedGiant, null, LuminosityClass.II),

            new ChildDefinition(_PlanetaryNebulaSpace, _GalaxySystemDensity * new Number(1.5, -8), CosmicStructureType.PlanetaryNebula),
        };

        internal OrbitalParameters GetGalaxyChildOrbit()
            => OrbitalParameters.GetFromEccentricity(Mass, Vector3.Zero, Randomizer.Instance.NextDouble(0.1));

        private CosmicLocation? ConfigureGalaxyInstance(Vector3 position, double? ambientTemperature = null, CosmicLocation? child = null)
        {
            CosmicLocation? newCore = null;
            if (child is not null
                && child.StructureType == CosmicStructureType.BlackHole
                && (!(CosmicStructureType.SpiralGalaxy | CosmicStructureType.EllipticalGalaxy).HasFlag(StructureType)
                || child.Mass > BlackHole.SupermassiveBlackHoleThreshold))
            {
                _seed = child._seed;
            }
            else
            {
                var core = new BlackHole(this, Vector3.Zero, null, supermassive: (CosmicStructureType.SpiralGalaxy | CosmicStructureType.EllipticalGalaxy).HasFlag(StructureType));
                _seed = core?._seed ?? Randomizer.Instance.NextUIntInclusive();
                newCore = core;
            }

            ReconstituteGalaxyInstance(position, ambientTemperature ?? UniverseAmbientTemperature);

            return newCore;
        }

        private void ReconstituteGalaxyInstance(Vector3 position, double? temperature)
        {
            var randomizer = new Randomizer(_seed);

            Number radius;
            Number axis;
            if (StructureType == CosmicStructureType.SpiralGalaxy)
            {
                radius = randomizer.NextNumber(new Number(2.4, 20), new Number(2.5, 21)); // 25000–75000 ly
                axis = radius * randomizer.NormalDistributionSample(0.02, 0.001);
            }
            else if (StructureType == CosmicStructureType.EllipticalGalaxy)
            {
                radius = randomizer.NextNumber(new Number(1.5, 18), new Number(1.5, 21)); // ~160–160000 ly
                axis = radius * randomizer.NormalDistributionSample(0.5, 1);
            }
            else if (StructureType == CosmicStructureType.DwarfGalaxy)
            {
                radius = randomizer.NextNumber(new Number(9.5, 18), new Number(2.5, 18)); // ~200–1800 ly
                axis = radius * randomizer.NormalDistributionSample(0.02, 1);
            }
            else
            {
                radius = randomizer.NextNumber(new Number(1.55, 19), new Number(1.55, 21)); // ~1600–160000 ly
                axis = radius * randomizer.NormalDistributionSample(0.02, 0.001);
            }
            var shape = new Ellipsoid(radius, axis, position);

            // Randomly determines a factor by which the mass of this galaxy will be
            // multiplied due to the abundance of dark matter.
            var darkMatterMultiplier = randomizer.NextNumber(5, 15);

            var coreMass = BlackHole.GetBlackHoleMassForSeed(_seed, supermassive: true);

            var mass = ((shape.Volume * _GalaxySystemDensity * new Number(1, 30)) + coreMass) * darkMatterMultiplier;

            Material = new Material(
                Substances.All.InterstellarMedium.GetReference(),
                mass,
                shape,
                temperature);
        }
    }
}
