using Tavenem.Chemistry;
using Tavenem.Chemistry.Elements;

namespace Tavenem.Universe.Chemistry;

/// <summary>
/// Substances used by Tavenem.Universe.
/// </summary>
public static class CosmicSubstances
{
    private static ISubstanceReference? _ChondriticRock;
    /// <summary>
    /// Chondritic rock.
    /// </summary>
    public static ISubstanceReference ChondriticRock
        => _ChondriticRock ??= Substances.Register(new Mixture(
            _ChondriticRockMixture,
            "Chondritic Rock",
            densitySolid: 3440)
        {
            Categories = new[] { Substances.Category_Cosmic, Substances.Category_Stone, }
        })
        .GetReference();

    private static ISubstanceReference? _CometNucleus;
    /// <summary>
    /// An approximation of the typical composition of the ice/dust core of a comet.
    /// </summary>
    public static ISubstanceReference CometNucleus
        => _CometNucleus ??= Substances.Register(new Mixture(
            new (HomogeneousReference, decimal)[]
            {
                (Substances.All.Water.GetHomogeneousReference(), 0.8m),
                (Substances.All.CarbonMonoxide.GetHomogeneousReference(), 0.15m),
                (Substances.All.CarbonDioxide.GetHomogeneousReference(), 0.016m),
                (Substances.All.Methane.GetHomogeneousReference(), 0.016m),
                (Substances.All.Ammonia.GetHomogeneousReference(), 0.016m),
                (Substances.All.Olivine.GetHomogeneousReference(), 0.0005m),
                (Substances.All.SiliconDioxide.GetHomogeneousReference(), 0.0005m),
                (Substances.All.SiliconCarbide.GetHomogeneousReference(), 0.0002m),
                (Substances.All.Naphthalene.GetHomogeneousReference(), 0.0002m),
                (Substances.All.Anthracene.GetHomogeneousReference(), 0.0002m),
                (Substances.All.Phenanthrene.GetHomogeneousReference(), 0.0002m),
                (Substances.All.AmorphousCarbon.GetHomogeneousReference(), 0.0002m),
            },
            "Comet Nucleus",
            densitySolid: 600)
        {
            Categories = new[] { Substances.Category_Cosmic }
        })
        .GetReference();

    /// <summary>
    /// An approximation of the typical composition of the planetary crust of planets with
    /// no liquid surface water.
    /// </summary>
    public static readonly (ISubstanceReference material, decimal proportion)[] DryPlanetaryCrustConstituents
        = new (ISubstanceReference material, decimal proportion)[]
        {
            (Substances.All.Basalt.GetReference(), 0.5975m),
            (Substances.All.Granite.GetReference(), 0.2725m),
            (Substances.All.Peridotite.GetReference(), 0.13m),
        };

    private static HomogeneousReference? _ElectronDegenerateCarbon;
    /// <summary>
    /// An approximation of electron-degenerate carbon matter.
    /// </summary>
    public static HomogeneousReference ElectronDegenerateCarbon
        => _ElectronDegenerateCarbon ??= Substances.Register(new Chemical(
            new Formula((PeriodicTable.Instance[6], 1)),
            "Electron-Degenerate Carbon",
            densitySpecial: 1e9,
            isConductive: true,
            fixedPhase: PhaseType.ElectronDegenerateMatter)
        {
            Categories = new[] { Substances.Category_Cosmic }
        })
        .GetHomogeneousReference();

    private static HomogeneousReference? _ElectronDegenerateOxygen;
    /// <summary>
    /// An approximation of electron-degenerate oxygen matter.
    /// </summary>
    public static HomogeneousReference ElectronDegenerateOxygen
        => _ElectronDegenerateOxygen ??= Substances.Register(new Chemical(
            new Formula((PeriodicTable.Instance[8], 1)),
            "Electron-Degenerate Oxygen",
            densitySpecial: 1e9,
            isConductive: true,
            fixedPhase: PhaseType.ElectronDegenerateMatter)
        {
            Categories = new[] { Substances.Category_Cosmic }
        })
        .GetHomogeneousReference();

    private static ISubstanceReference? _StellarMaterial;
    /// <summary>
    /// An approximation of typical main sequence stellar matter.
    /// </summary>
    public static ISubstanceReference StellarMaterial
        => _StellarMaterial ??= Substances.Register(new Mixture(
            new (HomogeneousReference, decimal)[]
            {
                (Substances.All.HydrogenPlasma.GetHomogeneousReference(), 0.7346m),
                (Substances.All.AlphaParticle.GetHomogeneousReference(), 0.2495m),
                (Substances.All.O2Pos.GetHomogeneousReference(), 0.0077m),
                (Substances.All.C4Pos.GetHomogeneousReference(), 0.0029m),
                (Substances.All.Fe2Pos.GetHomogeneousReference(), 0.0016m),
                (Substances.All.Neon.GetHomogeneousReference(), 0.0012m),
                (Substances.All.N5Pos.GetHomogeneousReference(), 0.0009m),
                (Substances.All.Si4Pos.GetHomogeneousReference(), 0.0007m),
                (Substances.All.Mg2Pos.GetHomogeneousReference(), 0.0005m),
                (Substances.All.S6Pos.GetHomogeneousReference(), 0.0004m),
            },
            "Stellar Material",
            densitySpecial: 1410)
        {
            Categories = new[] { Substances.Category_Cosmic, Substances.Category_Plasma }
        })
        .GetReference();

    private static ISubstanceReference? _StellarMaterialPopulationII;
    /// <summary>
    /// An approximation of typical population II stellar matter.
    /// </summary>
    public static ISubstanceReference StellarMaterialPopulationII
        => _StellarMaterialPopulationII ??= Substances.Register(new Mixture(
            new (HomogeneousReference, decimal)[]
            {
                (Substances.All.HydrogenPlasma.GetHomogeneousReference(), 0.72m),
                (Substances.All.AlphaParticle.GetHomogeneousReference(), 0.27985m),
                (Substances.All.O2Pos.GetHomogeneousReference(), 0.0001m),
                (Substances.All.C4Pos.GetHomogeneousReference(), 0.00003m),
                (Substances.All.Neon.GetHomogeneousReference(), 0.00002m),
            },
            "Stellar Material, Population II",
            densitySpecial: 1410)
        {
            Categories = new[] { Substances.Category_Cosmic, Substances.Category_Plasma }
        })
        .GetReference();

    private static ISubstanceReference? _StellarMaterialWhiteDwarf;
    /// <summary>
    /// An approximation of typical white dwarf stellar matter.
    /// </summary>
    public static ISubstanceReference StellarMaterialWhiteDwarf
        => _StellarMaterialWhiteDwarf ??= Substances.Register(new Mixture(
            new (HomogeneousReference, decimal)[]
            {
                (ElectronDegenerateOxygen, 0.8m),
                (ElectronDegenerateCarbon, 0.3m),
            },
            "Stellar Material, White Dwarf",
            densitySpecial: 1e9)
        {
            Categories = new[] { Substances.Category_Cosmic }
        })
        .GetReference();

    /// <summary>
    /// An approximation of the typical composition of the planetary crust of planets with
    /// liquid surface water.
    /// </summary>
    public static readonly (ISubstanceReference material, decimal proportion)[] WetPlanetaryCrustConstituents
        = new (ISubstanceReference material, decimal proportion)[]
        {
            (Substances.All.Sandstone.GetReference(), 0.059m),
            (Substances.All.BallClay.GetReference(), 0.03m),
            (Substances.All.CalciumCarbonate.GetHomogeneousReference(), 0.02m),
            (Substances.All.Silt.GetReference(), 0.01m),
            (Substances.All.Kaolinite.GetHomogeneousReference(), 0.01m),
            (Substances.All.SodiumChloride.GetHomogeneousReference(), 0.001m),
        };

    internal static readonly (HomogeneousReference material, decimal proportion)[] _ChondriticRockMixture
        = new (HomogeneousReference material, decimal proportion)[]
        {
            (Substances.All.Olivine.GetHomogeneousReference(), 0.45m),
            (Substances.All.SiliconDioxide.GetHomogeneousReference(), 0.2158m),
            (Substances.All.IronNickelAlloy.GetHomogeneousReference(), 0.13m),
            (Substances.All.Magnetite.GetHomogeneousReference(), 0.0978m),
            (Substances.All.Orthopyroxene.GetHomogeneousReference(), 0.05m),
            (Substances.All.Plagioclase.GetHomogeneousReference(), 0.05m),
            (Substances.All.SiliconCarbide.GetHomogeneousReference(), 0.0016m),
            (Substances.All.AmorphousCarbon.GetHomogeneousReference(), 0.0012m),
            (Substances.All.Naphthalene.GetHomogeneousReference(), 0.001m),
            (Substances.All.Anthracene.GetHomogeneousReference(), 0.001m),
            (Substances.All.Phenanthrene.GetHomogeneousReference(), 0.001m),
            (Substances.All.Diamond.GetHomogeneousReference(), 0.0005m),
            (Substances.All.Corundum.GetHomogeneousReference(), 0.0001m),
        };

    internal static readonly (HomogeneousReference material, decimal proportion)[] _ChondriticRockMixture_NoMetal
        = new (HomogeneousReference material, decimal proportion)[]
        {
            (Substances.All.Olivine.GetHomogeneousReference(), 0.52m),
            (Substances.All.SiliconDioxide.GetHomogeneousReference(), 0.248m),
            (Substances.All.Magnetite.GetHomogeneousReference(), 0.1124m),
            (Substances.All.Orthopyroxene.GetHomogeneousReference(), 0.565m),
            (Substances.All.Plagioclase.GetHomogeneousReference(), 0.565m),
            (Substances.All.SiliconCarbide.GetHomogeneousReference(), 0.0017m),
            (Substances.All.AmorphousCarbon.GetHomogeneousReference(), 0.0013m),
            (Substances.All.Naphthalene.GetHomogeneousReference(), 0.001m),
            (Substances.All.Anthracene.GetHomogeneousReference(), 0.001m),
            (Substances.All.Phenanthrene.GetHomogeneousReference(), 0.001m),
            (Substances.All.Diamond.GetHomogeneousReference(), 0.0005m),
            (Substances.All.Corundum.GetHomogeneousReference(), 0.0001m),
        };
}
