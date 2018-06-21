using Substances;
using System.Collections.Generic;

namespace WorldFoundry.Substances
{
    public static class CosmicSubstances
    {
        public static readonly Chemical NeutronDegenerateMatter = new Chemical("Neutron Degenerate Matter")
        {
            AntoineMaximumTemperature = 0,
            IsConductive = true,
            MeltingPoint = float.PositiveInfinity,
        };

        public static readonly Chemical Fuzzball = new Chemical("Fuzzball")
        {
            AntoineMaximumTemperature = 0,
            MeltingPoint = float.PositiveInfinity,
        };

        public static readonly Composite StellarMaterial = new Composite(new Dictionary<(Chemical chemical, Phase phase), float>
        {
            { (Chemical.Hydrogen, Phase.Plasma), 0.711f },
            { (Chemical.Helium, Phase.Plasma), 0.274f },
            { (Chemical.Oxygen, Phase.Gas), 0.008f },
            { (Chemical.Carbon, Phase.Gas), 0.003f },
            { (Chemical.Neon, Phase.Gas), 0.002f },
            { (Chemical.Iron, Phase.Gas), 0.002f },
        });
        public static readonly Composite StellarMaterialPopulationII = new Composite(new Dictionary<(Chemical chemical, Phase phase), float>
        {
            { (Chemical.Hydrogen, Phase.Plasma), 0.72f },
            { (Chemical.Helium, Phase.Plasma), 0.27985f },
            { (Chemical.Oxygen, Phase.Gas), 0.0001f },
            { (Chemical.Carbon, Phase.Gas), 0.00003f },
            { (Chemical.Neon, Phase.Gas), 0.00002f },
        });

        public static readonly Composite InterplanetaryMedium = new Composite(new Dictionary<(Chemical chemical, Phase phase), float>
        {
            { (Chemical.Hydrogen, Phase.Plasma), 0.711f },
            { (Chemical.Helium, Phase.Plasma), 0.274f },
            { (Chemical.Oxygen, Phase.Gas), 0.008f },
            { (Chemical.Carbon, Phase.Solid), 0.003f },
            { (Chemical.Neon, Phase.Gas), 0.002f },
            { (Chemical.Iron, Phase.Solid), 0.002f },
        });

        public static readonly Composite InterstellarMedium = new Composite(new Dictionary<(Chemical chemical, Phase phase), float>
        {
            { (Chemical.Hydrogen, Phase.Plasma), 0.534f },
            { (Chemical.Helium, Phase.Plasma), 0.206f },
            { (Chemical.Hydrogen, Phase.Gas), 0.177f },
            { (Chemical.Helium, Phase.Gas), 0.068f },
            { (Chemical.Oxygen, Phase.Gas), 0.008f },
            { (Chemical.Carbon, Phase.Solid), 0.003f },
            { (Chemical.Neon, Phase.Gas), 0.002f },
            { (Chemical.Iron, Phase.Solid), 0.002f },
        });

        public static readonly Composite IntraclusterMedium = new Composite(new Dictionary<(Chemical chemical, Phase phase), float>
        {
            { (Chemical.Hydrogen, Phase.Plasma), 0.718f },
            { (Chemical.Helium, Phase.Plasma), 0.277f },
            { (Chemical.Oxygen, Phase.Gas), 0.00266f },
            { (Chemical.Carbon, Phase.Solid), 0.001f },
            { (Chemical.Neon, Phase.Gas), 0.00067f },
            { (Chemical.Iron, Phase.Solid), 0.00067f },
        });

        public static readonly Composite IntergalacticMedium = new Composite(new Dictionary<(Chemical chemical, Phase phase), float>
        {
            { (Chemical.Hydrogen, Phase.Plasma), 0.722f },
            { (Chemical.Helium, Phase.Plasma), 0.278f },
        });
    }
}
