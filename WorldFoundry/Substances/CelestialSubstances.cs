using NeverFoundry.MathAndScience.Chemistry;
using NeverFoundry.MathAndScience.Chemistry.Elements;
using System.Collections.Generic;

namespace NeverFoundry.WorldFoundry
{
    internal static class CelestialSubstances
    {
        private static ISubstanceReference? _ChondriticRock;
        public static ISubstanceReference ChondriticRock
            => _ChondriticRock ??= Substances.Register(new Mixture(
                ChondriticRockMixture,
                "Chondritic Rock",
                densitySolid: 3440))
            .GetReference();

        private static ISubstanceReference? _CometNucleus;
        public static ISubstanceReference CometNucleus
            => _CometNucleus ??= Substances.Register(new Mixture(
                new (IHomogeneousReference, decimal)[]
                {
                    (Substances.All.Water.GetChemicalReference(), 0.8m),
                    (Substances.All.CarbonMonoxide.GetChemicalReference(), 0.15m),
                    (Substances.All.CarbonDioxide.GetChemicalReference(), 0.016m),
                    (Substances.All.Methane.GetChemicalReference(), 0.016m),
                    (Substances.All.Ammonia.GetChemicalReference(), 0.016m),
                    (Substances.All.Olivine.GetHomogeneousReference(), 0.0005m),
                    (Substances.All.SiliconDioxide.GetChemicalReference(), 0.0005m),
                    (Substances.All.SiliconCarbide.GetChemicalReference(), 0.0002m),
                    (Substances.All.Naphthalene.GetChemicalReference(), 0.0002m),
                    (Substances.All.Anthracene.GetChemicalReference(), 0.0002m),
                    (Substances.All.Phenanthrene.GetChemicalReference(), 0.0002m),
                    (Substances.All.AmorphousCarbon.GetChemicalReference(), 0.0002m),
                },
                "Comet Nucleus",
                densitySolid: 600))
            .GetReference();

        public static readonly (ISubstanceReference material, decimal proportion)[] DryPlanetaryCrustConstituents
            = new (ISubstanceReference material, decimal proportion)[]
            {
                (Substances.All.Basalt.GetReference(), 0.5975m),
                (Substances.All.Granite.GetReference(), 0.2725m),
                (Substances.All.Peridotite.GetReference(), 0.13m),
            };

        private static ChemicalReference? _ElectronDegenerateCarbon;
        public static ChemicalReference ElectronDegenerateCarbon
            => _ElectronDegenerateCarbon ??= Substances.Register(new Chemical(
                new Formula((PeriodicTable.Instance[6], 1)),
                "Electron-Degenerate Carbon",
                densitySpecial: 1e9,
                isConductive: true,
                phase: PhaseType.ElectronDegenerateMatter))
            .GetChemicalReference();

        private static ChemicalReference? _ElectronDegenerateOxygen;
        public static ChemicalReference ElectronDegenerateOxygen
            => _ElectronDegenerateOxygen ??= Substances.Register(new Chemical(
                new Formula((PeriodicTable.Instance[8], 1)),
                "Electron-Degenerate Oxygen",
                densitySpecial: 1e9,
                isConductive: true,
                phase: PhaseType.ElectronDegenerateMatter))
            .GetChemicalReference();

        private static ISubstanceReference? _StellarMaterial;
        public static ISubstanceReference StellarMaterial
            => _StellarMaterial ??= Substances.Register(new Mixture(
                new (IHomogeneousReference, decimal)[]
                {
                    (Substances.All.HydrogenPlasma.GetChemicalReference(), 0.7346m),
                    (Substances.All.AlphaParticle.GetChemicalReference(), 0.2495m),
                    (Substances.All.O2Pos.GetChemicalReference(), 0.0077m),
                    (Substances.All.C4Pos.GetChemicalReference(), 0.0029m),
                    (Substances.All.Fe2Pos.GetChemicalReference(), 0.0016m),
                    (Substances.All.Neon.GetChemicalReference(), 0.0012m),
                    (Substances.All.N5Pos.GetChemicalReference(), 0.0009m),
                    (Substances.All.Si4Pos.GetChemicalReference(), 0.0007m),
                    (Substances.All.Mg2Pos.GetChemicalReference(), 0.0005m),
                    (Substances.All.S6Pos.GetChemicalReference(), 0.0004m),
                },
                "Stellar Material",
                densitySpecial: 1410))
            .GetReference();

        private static ISubstanceReference? _StellarMaterialPopulationII;
        public static ISubstanceReference StellarMaterialPopulationII
            => _StellarMaterialPopulationII ??= Substances.Register(new Mixture(
                new (IHomogeneousReference, decimal)[]
                {
                    (Substances.All.HydrogenPlasma.GetChemicalReference(), 0.72m),
                    (Substances.All.AlphaParticle.GetChemicalReference(), 0.27985m),
                    (Substances.All.O2Pos.GetChemicalReference(), 0.0001m),
                    (Substances.All.C4Pos.GetChemicalReference(), 0.00003m),
                    (Substances.All.Neon.GetChemicalReference(), 0.00002m),
                },
                "Stellar Material, Population II",
                densitySpecial: 1410))
            .GetReference();

        private static ISubstanceReference? _StellarMaterialWhiteDwarf;
        public static ISubstanceReference StellarMaterialWhiteDwarf
            => _StellarMaterialWhiteDwarf ??= Substances.Register(new Mixture(
                new (IHomogeneousReference, decimal)[]
                {
                    (ElectronDegenerateOxygen, 0.8m),
                    (ElectronDegenerateCarbon, 0.3m),
                },
                "Stellar Material, White Dwarf",
                densitySpecial: 1e9))
            .GetReference();

        public static readonly (ISubstanceReference material, decimal proportion)[] WetPlanetaryCrustConstituents
            = new (ISubstanceReference material, decimal proportion)[]
            {
                (Substances.All.Sandstone.GetReference(), 0.059m),
                (Substances.All.BallClay.GetReference(), 0.03m),
                (Substances.All.CalciumCarbonate.GetChemicalReference(), 0.02m),
                (Substances.All.Silt.GetReference(), 0.01m),
                (Substances.All.Kaolinite.GetChemicalReference(), 0.01m),
                (Substances.All.SodiumChloride.GetChemicalReference(), 0.001m),
            };

        internal static readonly (IHomogeneousReference material, decimal proportion)[] ChondriticRockMixture
            = new (IHomogeneousReference material, decimal proportion)[]
            {
                (Substances.All.Olivine.GetHomogeneousReference(), 0.45m),
                (Substances.All.SiliconDioxide.GetChemicalReference(), 0.2158m),
                (Substances.All.IronNickelAlloy.GetHomogeneousReference(), 0.13m),
                (Substances.All.Magnetite.GetChemicalReference(), 0.0978m),
                (Substances.All.Orthopyroxene.GetHomogeneousReference(), 0.05m),
                (Substances.All.Plagioclase.GetHomogeneousReference(), 0.05m),
                (Substances.All.SiliconCarbide.GetChemicalReference(), 0.0016m),
                (Substances.All.AmorphousCarbon.GetChemicalReference(), 0.0012m),
                (Substances.All.Naphthalene.GetChemicalReference(), 0.001m),
                (Substances.All.Anthracene.GetChemicalReference(), 0.001m),
                (Substances.All.Phenanthrene.GetChemicalReference(), 0.001m),
                (Substances.All.Diamond.GetChemicalReference(), 0.0005m),
                (Substances.All.Corundum.GetChemicalReference(), 0.0001m),
            };

        internal static readonly (IHomogeneousReference material, decimal proportion)[] ChondriticRockMixture_NoMetal
            = new (IHomogeneousReference material, decimal proportion)[]
            {
                (Substances.All.Olivine.GetHomogeneousReference(), 0.52m),
                (Substances.All.SiliconDioxide.GetChemicalReference(), 0.248m),
                (Substances.All.Magnetite.GetChemicalReference(), 0.1124m),
                (Substances.All.Orthopyroxene.GetHomogeneousReference(), 0.565m),
                (Substances.All.Plagioclase.GetHomogeneousReference(), 0.565m),
                (Substances.All.SiliconCarbide.GetChemicalReference(), 0.0017m),
                (Substances.All.AmorphousCarbon.GetChemicalReference(), 0.0013m),
                (Substances.All.Naphthalene.GetChemicalReference(), 0.001m),
                (Substances.All.Anthracene.GetChemicalReference(), 0.001m),
                (Substances.All.Phenanthrene.GetChemicalReference(), 0.001m),
                (Substances.All.Diamond.GetChemicalReference(), 0.0005m),
                (Substances.All.Corundum.GetChemicalReference(), 0.0001m),
            };
    }
}
