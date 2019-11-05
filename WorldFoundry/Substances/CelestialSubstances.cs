using NeverFoundry.MathAndScience.Chemistry;
using NeverFoundry.MathAndScience.Chemistry.Elements;
using System.Collections.Generic;

namespace NeverFoundry.WorldFoundry
{
    internal static class CelestialSubstances
    {
        public const string ChondriticRockName = "Chondritic Rock";
        public const string CometNucleusName = "Comet Nucleus";
        public const string ElectronDegenerateCarbonName = "ElectronDegenerateCarbon";
        public const string ElectronDegenerateOxygenName = "ElectronDegenerateOxygen";
        public const string StellarMaterialName = "Stellar Material";
        public const string StellarMaterialPopulationIIName = "Stellar Material, Population II";
        public const string StellarMaterialWhiteDwarfName = "Stellar Material, White Dwarf";

        private static ISubstanceReference? _ChondriticRock;
        public static ISubstanceReference ChondriticRock
        {
            get
            {
                if (_ChondriticRock is null)
                {
                    var substance = new Mixture(
                        ChondriticRockMixture,
                        ChondriticRockName,
                        densitySolid: 3440);
                    Substances.Register(substance);
                    _ChondriticRock = substance.GetReference();
                }
                return _ChondriticRock;
            }
        }

        private static ISubstanceReference? _CometNucleus;
        public static ISubstanceReference CometNucleus
        {
            get
            {
                if (_CometNucleus is null)
                {
                    var substance = new Mixture(
                        new (IHomogeneousReference, decimal)[]
                        {
                            (Substances.GetChemicalReference(Substances.Chemicals.Water), 0.8m),
                            (Substances.GetChemicalReference(Substances.Chemicals.CarbonMonoxide), 0.15m),
                            (Substances.GetChemicalReference(Substances.Chemicals.CarbonDioxide), 0.016m),
                            (Substances.GetChemicalReference(Substances.Chemicals.Methane), 0.016m),
                            (Substances.GetChemicalReference(Substances.Chemicals.Ammonia), 0.016m),
                            (Substances.GetSolutionReference(Substances.Solutions.Olivine), 0.0005m),
                            (Substances.GetChemicalReference(Substances.Chemicals.SiliconDioxide), 0.0005m),
                            (Substances.GetChemicalReference(Substances.Chemicals.SiliconCarbide), 0.0002m),
                            (Substances.GetChemicalReference(Substances.Chemicals.Naphthalene), 0.0002m),
                            (Substances.GetChemicalReference(Substances.Chemicals.Anthracene), 0.0002m),
                            (Substances.GetChemicalReference(Substances.Chemicals.Phenanthrene), 0.0002m),
                            (Substances.GetChemicalReference(Substances.Chemicals.AmorphousCarbon), 0.0002m),
                        },
                        CometNucleusName,
                        densitySolid: 600);
                    Substances.Register(substance);
                    _CometNucleus = substance.GetReference();
                }
                return _CometNucleus;
            }
        }

        public static readonly (ISubstanceReference material, decimal proportion)[] DryPlanetaryCrustConstituents
            = new (ISubstanceReference material, decimal proportion)[]
            {
                (Substances.GetMixtureReference(Substances.Mixtures.Basalt), 0.5975m),
                (Substances.GetMixtureReference(Substances.Mixtures.Granite), 0.2725m),
                (Substances.GetMixtureReference(Substances.Mixtures.Peridotite), 0.13m),
            };

        private static ChemicalReference? _ElectronDegenerateCarbon;
        public static ChemicalReference ElectronDegenerateCarbon
        {
            get
            {
                if (!_ElectronDegenerateCarbon.HasValue)
                {
                    var substance = new Chemical(
                        new Formula((PeriodicTable.Instance[6], 1)),
                        ElectronDegenerateCarbonName,
                        densitySpecial: 1e9,
                        isConductive: true,
                        phase: PhaseType.ElectronDegenerateMatter);
                    Substances.Register(substance);
                    _ElectronDegenerateCarbon = substance.GetChemicalReference();
                }
                return _ElectronDegenerateCarbon!.Value;
            }
        }

        private static ChemicalReference? _ElectronDegenerateOxygen;
        public static ChemicalReference ElectronDegenerateOxygen
        {
            get
            {
                if (!_ElectronDegenerateOxygen.HasValue)
                {
                    var substance = new Chemical(
                        new Formula((PeriodicTable.Instance[8], 1)),
                        ElectronDegenerateOxygenName,
                        densitySpecial: 1e9,
                        isConductive: true,
                        phase: PhaseType.ElectronDegenerateMatter);
                    Substances.Register(substance);
                    _ElectronDegenerateOxygen = substance.GetChemicalReference();
                }
                return _ElectronDegenerateOxygen!.Value;
            }
        }

        private static double? _SeawaterMeltingPoint;
        public static double SeawaterMeltingPoint
        {
            get
            {
                if (!_SeawaterMeltingPoint.HasValue)
                {
                    if (!Substances.TryGetSolution(Substances.Solutions.Seawater, out var solution))
                    {
                        throw new KeyNotFoundException("Seawater missing.");
                    }
                    _SeawaterMeltingPoint = solution.MeltingPoint;
                }
                return _SeawaterMeltingPoint ?? 0;
            }
        }

        private static ISubstanceReference? _StellarMaterial;
        public static ISubstanceReference StellarMaterial
        {
            get
            {
                if (_StellarMaterial is null)
                {
                    var substance = new Mixture(
                        new (IHomogeneousReference, decimal)[]
                        {
                            (Substances.GetChemicalReference(Substances.Chemicals.HydrogenPlasma), 0.7346m),
                            (Substances.GetChemicalReference(Substances.Chemicals.AlphaParticle), 0.2495m),
                            (Substances.GetChemicalReference(Substances.Chemicals.O2Pos), 0.0077m),
                            (Substances.GetChemicalReference(Substances.Chemicals.C4Pos), 0.0029m),
                            (Substances.GetChemicalReference(Substances.Chemicals.Fe2Pos), 0.0016m),
                            (Substances.GetChemicalReference(Substances.Chemicals.Neon), 0.0012m),
                            (Substances.GetChemicalReference(Substances.Chemicals.N5Pos), 0.0009m),
                            (Substances.GetChemicalReference(Substances.Chemicals.Si4Pos), 0.0007m),
                            (Substances.GetChemicalReference(Substances.Chemicals.Mg2Pos), 0.0005m),
                            (Substances.GetChemicalReference(Substances.Chemicals.S6Pos), 0.0004m),
                        },
                        StellarMaterialName,
                        densitySpecial: 1410);
                    Substances.Register(substance);
                    _StellarMaterial = substance.GetReference();
                }
                return _StellarMaterial;
            }
        }

        private static ISubstanceReference? _StellarMaterialPopulationII;
        public static ISubstanceReference StellarMaterialPopulationII
        {
            get
            {
                if (_StellarMaterialPopulationII is null)
                {
                    var substance = new Mixture(
                        new (IHomogeneousReference, decimal)[]
                        {
                            (Substances.GetChemicalReference(Substances.Chemicals.HydrogenPlasma), 0.72m),
                            (Substances.GetChemicalReference(Substances.Chemicals.AlphaParticle), 0.27985m),
                            (Substances.GetChemicalReference(Substances.Chemicals.O2Pos), 0.0001m),
                            (Substances.GetChemicalReference(Substances.Chemicals.C4Pos), 0.00003m),
                            (Substances.GetChemicalReference(Substances.Chemicals.Neon), 0.00002m),
                        },
                        StellarMaterialPopulationIIName,
                        densitySpecial: 1410);
                    Substances.Register(substance);
                    _StellarMaterialPopulationII = substance.GetReference();
                }
                return _StellarMaterialPopulationII;
            }
        }

        private static ISubstanceReference? _StellarMaterialWhiteDwarf;
        public static ISubstanceReference StellarMaterialWhiteDwarf
        {
            get
            {
                if (_StellarMaterialWhiteDwarf is null)
                {
                    var substance = new Mixture(
                        new (IHomogeneousReference, decimal)[]
                        {
                            (new ChemicalReference(ElectronDegenerateOxygenName), 0.8m),
                            (new ChemicalReference(ElectronDegenerateCarbonName), 0.3m),
                        },
                        StellarMaterialWhiteDwarfName,
                        densitySpecial: 1e9);
                    Substances.Register(substance);
                    _StellarMaterialWhiteDwarf = substance.GetReference();
                }
                return _StellarMaterialWhiteDwarf;
            }
        }

        private static double? _WaterMeltingPoint;
        public static double WaterMeltingPoint
        {
            get
            {
                if (!_WaterMeltingPoint.HasValue)
                {
                    if (!Substances.TryGetChemical(Substances.Chemicals.Water, out var chemical))
                    {
                        throw new KeyNotFoundException("Water missing.");
                    }
                    _WaterMeltingPoint = chemical.MeltingPoint;
                }
                return _WaterMeltingPoint ?? 0;
            }
        }

        public static readonly (ISubstanceReference material, decimal proportion)[] WetPlanetaryCrustConstituents
            = new (ISubstanceReference material, decimal proportion)[]
            {
                (Substances.GetMixtureReference(Substances.Mixtures.Sandstone), 0.059m),
                (Substances.GetMixtureReference(Substances.Mixtures.BallClay), 0.03m),
                (Substances.GetChemicalReference(Substances.Chemicals.CalciumCarbonate), 0.02m),
                (Substances.GetMixtureReference(Substances.Mixtures.Silt), 0.01m),
                (Substances.GetChemicalReference(Substances.Chemicals.Kaolinite), 0.01m),
                (Substances.GetChemicalReference(Substances.Chemicals.SodiumChloride), 0.001m),
            };

        internal static readonly (IHomogeneousReference material, decimal proportion)[] ChondriticRockMixture
            = new (IHomogeneousReference material, decimal proportion)[]
            {
                (Substances.GetSolutionReference(Substances.Solutions.Olivine), 0.45m),
                (Substances.GetChemicalReference(Substances.Chemicals.SiliconDioxide), 0.2158m),
                (Substances.GetSolutionReference(Substances.Solutions.IronNickelAlloy), 0.13m),
                (Substances.GetChemicalReference(Substances.Chemicals.Magnetite), 0.0978m),
                (Substances.GetSolutionReference(Substances.Solutions.Orthopyroxene), 0.05m),
                (Substances.GetSolutionReference(Substances.Solutions.Plagioclase), 0.05m),
                (Substances.GetChemicalReference(Substances.Chemicals.SiliconCarbide), 0.0016m),
                (Substances.GetChemicalReference(Substances.Chemicals.AmorphousCarbon), 0.0012m),
                (Substances.GetChemicalReference(Substances.Chemicals.Naphthalene), 0.001m),
                (Substances.GetChemicalReference(Substances.Chemicals.Anthracene), 0.001m),
                (Substances.GetChemicalReference(Substances.Chemicals.Phenanthrene), 0.001m),
                (Substances.GetChemicalReference(Substances.Chemicals.Diamond), 0.0005m),
                (Substances.GetChemicalReference(Substances.Chemicals.Corundum), 0.0001m),
            };

        internal static readonly (IHomogeneousReference material, decimal proportion)[] ChondriticRockMixture_NoMetal
            = new (IHomogeneousReference material, decimal proportion)[]
            {
                (Substances.GetSolutionReference(Substances.Solutions.Olivine), 0.52m),
                (Substances.GetChemicalReference(Substances.Chemicals.SiliconDioxide), 0.248m),
                (Substances.GetChemicalReference(Substances.Chemicals.Magnetite), 0.1124m),
                (Substances.GetSolutionReference(Substances.Solutions.Orthopyroxene), 0.565m),
                (Substances.GetSolutionReference(Substances.Solutions.Plagioclase), 0.565m),
                (Substances.GetChemicalReference(Substances.Chemicals.SiliconCarbide), 0.0017m),
                (Substances.GetChemicalReference(Substances.Chemicals.AmorphousCarbon), 0.0013m),
                (Substances.GetChemicalReference(Substances.Chemicals.Naphthalene), 0.001m),
                (Substances.GetChemicalReference(Substances.Chemicals.Anthracene), 0.001m),
                (Substances.GetChemicalReference(Substances.Chemicals.Phenanthrene), 0.001m),
                (Substances.GetChemicalReference(Substances.Chemicals.Diamond), 0.0005m),
                (Substances.GetChemicalReference(Substances.Chemicals.Corundum), 0.0001m),
            };
    }
}
