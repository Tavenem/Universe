using Substances;

namespace WorldFoundry.Substances
{
    /// <summary>
    /// A collection of <see cref="Substance"/>s specific to <see cref="WorldFoundry"/>.
    /// </summary>
    public static class CosmicSubstances
    {
#pragma warning disable CS1591
        public static readonly Chemical NeutronDegenerateMatter = new Chemical("Neutron Degenerate Matter", antoineMinimumTemperature: 0, isConductive: true, meltingPoint: double.PositiveInfinity);

        public static readonly Chemical Fuzzball = new Chemical("Fuzzball", antoineMinimumTemperature: 0, meltingPoint: double.PositiveInfinity);

        public static readonly Composite StellarMaterial = new Composite(
            (Chemical.Hydrogen, Phase.Plasma, 0.711),
            (Chemical.Helium, Phase.Plasma, 0.274),
            (Chemical.Oxygen, Phase.Gas, 0.008),
            (Chemical.Carbon, Phase.Gas, 0.003),
            (Chemical.Neon, Phase.Gas, 0.002),
            (Chemical.Iron, Phase.Gas, 0.002));
        public static readonly Composite StellarMaterialPopulationII = new Composite(
            (Chemical.Hydrogen, Phase.Plasma, 0.72),
            (Chemical.Helium, Phase.Plasma, 0.27985),
            (Chemical.Oxygen, Phase.Gas, 0.0001),
            (Chemical.Carbon, Phase.Gas, 0.00003),
            (Chemical.Neon, Phase.Gas, 0.00002));

        public static readonly Composite InterplanetaryMedium = new Composite(
            (Chemical.Hydrogen, Phase.Plasma, 0.711),
            (Chemical.Helium, Phase.Plasma, 0.274),
            (Chemical.Oxygen, Phase.Gas, 0.008),
            (Chemical.Carbon, Phase.Solid, 0.003),
            (Chemical.Neon, Phase.Gas, 0.002),
            (Chemical.Iron, Phase.Solid, 0.002));

        public static readonly Composite InterstellarMedium = new Composite(
            (Chemical.Hydrogen, Phase.Plasma, 0.534),
            (Chemical.Helium, Phase.Plasma, 0.206),
            (Chemical.Hydrogen, Phase.Gas, 0.177),
            (Chemical.Helium, Phase.Gas, 0.068),
            (Chemical.Oxygen, Phase.Gas, 0.008),
            (Chemical.Carbon, Phase.Solid, 0.003),
            (Chemical.Neon, Phase.Gas, 0.002),
            (Chemical.Iron, Phase.Solid, 0.002));

        public static readonly Composite IntraclusterMedium = new Composite(
            (Chemical.Hydrogen, Phase.Plasma, 0.718),
            (Chemical.Helium, Phase.Plasma, 0.277),
            (Chemical.Oxygen, Phase.Gas, 0.00266),
            (Chemical.Carbon, Phase.Solid, 0.001),
            (Chemical.Neon, Phase.Gas, 0.00067),
            (Chemical.Iron, Phase.Solid, 0.00067));

        public static readonly Composite IntergalacticMedium = new Composite(
            (Chemical.Hydrogen, Phase.Plasma, 0.722),
            (Chemical.Helium, Phase.Plasma, 0.278));
#pragma warning restore CS1591
    }
}
