using MathAndScience.MathUtil;
using Substances;
using System;
using System.Collections.Generic;
using System.Numerics;
using WorldFoundry.CelestialBodies.Planetoids.Asteroids;
using WorldFoundry.CelestialBodies.Planetoids.Planets.DwarfPlanets;
using WorldFoundry.CelestialBodies.Planetoids.Planets.TerrestrialPlanets;
using WorldFoundry.Climate;
using WorldFoundry.Space;

namespace WorldFoundry.CelestialBodies.Planetoids.Planets.GiantPlanets
{
    /// <summary>
    /// A gas giant planet (excluding ice giants, which have their own subclass).
    /// </summary>
    public class GiantPlanet : Planemo
    {
        private const string baseTypeName = "Gas Giant";
        /// <summary>
        /// The base name for this type of <see cref="CelestialEntity"/>.
        /// </summary>
        public override string BaseTypeName => baseTypeName;

        private const bool hasFlatSurface = true;
        /// <summary>
        /// Indicates that this <see cref="Planetoid"/>'s surface does not have elevation variations
        /// (i.e. is non-solid). Prevents generation of a height map during <see
        /// cref="Planetoid.Topography"/> generation.
        /// </summary>
        public override bool HasFlatSurface => hasFlatSurface;

        internal static int subMinDensity = 600;
        private protected int SubMinDensity => subMinDensity;

        internal static int minDensity = 1100;
        private protected int MinDensity => minDensity;

        internal static int maxDensity = 1650;
        private protected int MaxDensity => maxDensity;

        private const double maxMassForType = 2.5e28;
        /// <summary>
        /// The maximum mass allowed for this type of <see cref="Planetoid"/> during random
        /// generation, in kg. Null indicates no maximum.
        /// </summary>
        /// <remarks>At around this limit the planet will have sufficient mass to sustain fusion, and become a brown dwarf.</remarks>
        internal override double? MaxMassForType => maxMassForType;

        internal new static int maxSatellites = 75;
        /// <summary>
        /// The upper limit on the number of satellites this <see cref="Planetoid"/> might have. The
        /// actual number is determined by the orbital characteristics of the satellites it actually has.
        /// </summary>
        /// <remarks>
        /// Set to 75 for <see cref="GiantPlanet"/>. For reference, Jupiter has 67 moons, and Saturn
        /// has 62 (non-ring) moons.
        /// </remarks>
        public override int MaxSatellites => maxSatellites;

        private const double minMassForType = 6.0e25;
        /// <summary>
        /// The minimum mass allowed for this type of <see cref="Planetoid"/> during random
        /// generation, in kg. Null indicates a minimum of 0.
        /// </summary>
        /// <remarks>Below this limit the planet will not have sufficient mass to retain hydrogen, and will be a terrestrial planet.</remarks>
        internal override double? MinMassForType => minMassForType;

        internal new static double ringChance = 90;
        /// <summary>
        /// The chance that this <see cref="Planemo"/> will have rings, as a rate between 0.0 and 1.0.
        /// </summary>
        /// <remarks>Giants are almost guaranteed to have rings.</remarks>
        protected override double RingChance => ringChance;

        /// <summary>
        /// Initializes a new instance of <see cref="GiantPlanet"/>.
        /// </summary>
        public GiantPlanet() : base() { }

        /// <summary>
        /// Initializes a new instance of <see cref="GiantPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="GiantPlanet"/> is located.
        /// </param>
        public GiantPlanet(CelestialRegion parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="GiantPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="GiantPlanet"/> is located.
        /// </param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="GiantPlanet"/> during random generation, in kg.
        /// </param>
        public GiantPlanet(CelestialRegion parent, double maxMass) : base(parent, maxMass) { }

        /// <summary>
        /// Initializes a new instance of <see cref="GiantPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="GiantPlanet"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="GiantPlanet"/>.</param>
        public GiantPlanet(CelestialRegion parent, Vector3 position) : base(parent, position) { }

        /// <summary>
        /// Initializes a new instance of <see cref="GiantPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="GiantPlanet"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="GiantPlanet"/>.</param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="GiantPlanet"/> during random generation, in kg.
        /// </param>
        public GiantPlanet(CelestialRegion parent, Vector3 position, double maxMass) : base(parent, position, maxMass) { }

        /// <summary>
        /// Determines an albedo for this <see cref="CelestialBody"/> (a value between 0 and 1).
        /// </summary>
        private protected override void GenerateAlbedo() => Albedo = Math.Round(Randomizer.Static.NextDouble(0.275, 0.35), 3);

        /// <summary>
        /// Generates an atmosphere for this <see cref="Planetoid"/>.
        /// </summary>
        /// <remarks>
        /// Giants have no solid surface, instead the "surface" is arbitrarily defined as the level
        /// where the pressure is 1 MPa.
        /// </remarks>
        private protected override void GenerateAtmosphere()
        {
            var trace = Math.Round(Randomizer.Static.NextDouble(0.025), 4);

            var h = Math.Round(Randomizer.Static.NextDouble(0.75, 0.97), 4);
            var he = 1 - h - trace;

            var ch4 = Math.Round(Randomizer.Static.NextDouble(trace), 4);
            trace -= ch4;

            // 50% chance not to have each of these components
            var c2h6 = Math.Round(Math.Max(0, Randomizer.Static.NextDouble(-0.5, 0.5)), 5);
            var traceTotal = c2h6;
            var nh3 = Math.Round(Math.Max(0, Randomizer.Static.NextDouble(-0.5, 0.5)), 5);
            traceTotal += nh3;
            var waterVapor = Math.Round(Math.Max(0, Randomizer.Static.NextDouble(-0.5, 0.5)), 5);
            traceTotal += waterVapor;

            var surfaceTemp = GetTotalTemperature();

            double water = 0, ice = 0;
            if (surfaceTemp < Chemical.Water.AntoineMinimumTemperature ||
                (surfaceTemp < Chemical.Water.AntoineMaximumTemperature &&
                Chemical.Water.CalculateVaporPressure(surfaceTemp) <= 1000))
            {
                water = Math.Round(Randomizer.Static.NextDouble(), 5);
                ice = Math.Round(Randomizer.Static.NextDouble(), 5);
                traceTotal += water + ice;
            }

            double ch4Liquid = 0, ch4Ice = 0;
            if (surfaceTemp < Chemical.Methane.AntoineMinimumTemperature ||
                (surfaceTemp < Chemical.Methane.AntoineMaximumTemperature &&
                Chemical.Methane.CalculateVaporPressure(surfaceTemp) <= 1000))
            {
                ch4Liquid = Math.Round(Randomizer.Static.NextDouble(), 5);
                ch4Ice = Math.Round(Randomizer.Static.NextDouble(), 5);
                traceTotal += ch4Liquid + ch4Ice;
            }

            double nh3Liquid = 0, nh3Ice = 0;
            if (surfaceTemp < Chemical.Ammonia.AntoineMinimumTemperature ||
                (surfaceTemp < Chemical.Ammonia.AntoineMaximumTemperature &&
                Chemical.Ammonia.CalculateVaporPressure(surfaceTemp) <= 1000))
            {
                nh3Liquid = Math.Round(Randomizer.Static.NextDouble(), 5);
                nh3Ice = Math.Round(Randomizer.Static.NextDouble(), 5);
                traceTotal += nh3Liquid + nh3Ice;
            }

            var nh4sh = Math.Round(Randomizer.Static.NextDouble(), 5);
            traceTotal += nh4sh;

            var ratio = trace / traceTotal;
            c2h6 *= ratio;
            nh3 *= ratio;
            waterVapor *= ratio;
            water *= ratio;
            ice *= ratio;
            ch4Liquid *= ratio;
            ch4Ice *= ratio;
            nh3Liquid *= ratio;
            nh3Ice *= ratio;
            nh4sh *= ratio;

            var atmosphere = new Composite(new Dictionary<(Chemical chemical, Phase phase), double>
            {
                { (Chemical.Hydrogen, Phase.Gas), h },
                { (Chemical.Helium, Phase.Gas), he },
                { (Chemical.Methane, Phase.Gas), ch4 },
            });
            if (c2h6 > 0)
            {
                atmosphere.Components[(Chemical.Ethane, Phase.Gas)] = c2h6;
            }
            if (nh3 > 0)
            {
                atmosphere.Components[(Chemical.Ammonia, Phase.Gas)] = nh3;
            }
            if (waterVapor > 0)
            {
                atmosphere.Components[(Chemical.Water, Phase.Gas)] = waterVapor;
            }
            if (water > 0)
            {
                atmosphere.Components[(Chemical.Water, Phase.Liquid)] = water;
            }
            if (ice > 0)
            {
                atmosphere.Components[(Chemical.Water, Phase.Solid)] = ice;
            }
            if (ch4Liquid > 0)
            {
                atmosphere.Components[(Chemical.Methane, Phase.Liquid)] = ch4Liquid;
            }
            if (ch4Ice > 0)
            {
                atmosphere.Components[(Chemical.Methane, Phase.Solid)] = ch4Ice;
            }
            if (nh3Liquid > 0)
            {
                atmosphere.Components[(Chemical.Ammonia, Phase.Liquid)] = nh3Liquid;
            }
            if (nh3Ice> 0)
            {
                atmosphere.Components[(Chemical.Ammonia, Phase.Solid)] = nh3Ice;
            }
            if (nh4sh > 0)
            {
                atmosphere.Components[(Chemical.AmmoniumHydrosulfide, Phase.Solid)] = nh4sh;
            }

            Atmosphere = new Atmosphere(this, atmosphere, 1000);
        }

        /// <summary>
        /// Determines the <see cref="CelestialEntity.Substance"/> of this <see cref="CelestialEntity"/>.
        /// </summary>
        private protected override void GenerateSubstance()
        {
            var layers = new List<(IComposition substance, double proportion)>();

            // Iron-nickel inner core.
            var coreProportion = GetCoreProportion();
            var innerCoreProportion = base.GetCoreProportion() * coreProportion;
            var coreNickel = Math.Round(Randomizer.Static.NextDouble(0.03, 0.15), 4);
            layers.Add((new Composite(new Dictionary<(Chemical chemical, Phase phase), double>
            {
                { (Chemical.Iron, Phase.Solid), 1 - coreNickel },
                { (Chemical.Nickel, Phase.Solid), coreNickel },
            }), innerCoreProportion));

            // Molten rock outer core.
            layers.Add((new Material(Chemical.Rock, Phase.Liquid), coreProportion - innerCoreProportion));

            // Metallic hydrogen lower mantle
            var metalH = Math.Round(Math.Max(0, Randomizer.Static.NextDouble(-0.1, 0.55)), 2);
            if (metalH > 0)
            {
                layers.Add((new Material(Chemical.Hydrogen_Metallic, Phase.Liquid), metalH));
            }

            // Supercritical fluid upper layer (blends seamlessly with lower atmosphere)
            var upperLayerProportion = 1 - coreProportion - metalH;
            var water = upperLayerProportion;
            var fluidH = water * 0.71;
            water -= fluidH;
            var fluidHe = water * 0.24;
            water -= fluidHe;
            var ne = Math.Round(Randomizer.Static.NextDouble(water), 4);
            water -= ne;
            var ch4 = Math.Round(Randomizer.Static.NextDouble(water), 4);
            water = Math.Max(0, water - ch4);
            var nh4 = Math.Round(Randomizer.Static.NextDouble(water), 4);
            water = Math.Max(0, water - nh4);
            var c2h6 = Math.Round(Randomizer.Static.NextDouble(water), 4);
            water = Math.Max(0, water - c2h6);
            var upperLayer = new Composite(new Dictionary<(Chemical chemical, Phase phase), double>
            {
                { (Chemical.Hydrogen, Phase.Liquid), 0.71 },
                { (Chemical.Helium, Phase.Liquid), 0.24 },
                { (Chemical.Neon, Phase.Liquid), ne },
            });
            if (ch4 > 0)
            {
                upperLayer.Components[(Chemical.Methane, Phase.Liquid)] = ch4;
            }
            if (nh4 > 0)
            {
                upperLayer.Components[(Chemical.Ammonia, Phase.Liquid)] = nh4;
            }
            if (c2h6 > 0)
            {
                upperLayer.Components[(Chemical.Ethane, Phase.Liquid)] = c2h6;
            }
            if (water > 0)
            {
                upperLayer.Components[(Chemical.Water, Phase.Liquid)] = water;
            }
            layers.Add((upperLayer, upperLayerProportion));

            Substance = new Substance()
            {
                Composition = new LayeredComposite(layers),
                Mass = GenerateMass(),
            };
            GenerateShape();
        }

        /// <summary>
        /// Generates an appropriate density for this <see cref="Planetoid"/>.
        /// </summary>
        /// <remarks>
        /// Relatively low chance of a "puffy" giant (Saturn-like, low-density).
        /// </remarks>
        private protected override void GenerateDensity()
        {
            if (Randomizer.Static.NextDouble() <= 0.2)
            {
                Density = Math.Round(Randomizer.Static.NextDouble(SubMinDensity, MinDensity));
            }
            else
            {
                Density = Math.Round(Randomizer.Static.NextDouble(MinDensity, MaxDensity));
            }
        }

        /// <summary>
        /// Generates the mass of this <see cref="GiantPlanet"/>.
        /// </summary>
        private protected virtual double GenerateMass() => Math.Round(Randomizer.Static.NextDouble(MinMass, MaxMass));

        /// <summary>
        /// Generates a new satellite for this <see cref="Planetoid"/> with the specified parameters.
        /// </summary>
        /// <returns>A satellite <see cref="Planetoid"/> with an appropriate orbit.</returns>
        private protected override Planetoid GenerateSatellite(double periapsis, double eccentricity, double maxMass)
        {
            Planetoid satellite = null;
            double chance;

            // If the mass limit allows, there is an even chance that the satellite is a smaller planet.
            if (maxMass > TerrestrialPlanet.minMassForType && Randomizer.Static.NextBoolean())
            {
                // Select from the standard distribution of types.
                chance = Randomizer.Static.NextDouble();

                // Planets with very low orbits are lava planets due to tidal
                // stress (plus a small percentage of others due to impact trauma).

                // The maximum mass and density are used to calculate an outer
                // Roche limit (may not be the actual Roche limit for the body
                // which gets generated).
                if (periapsis < GetRocheLimit(TerrestrialPlanet.maxDensity) * 1.05 || chance <= 0.01)
                {
                    satellite = new LavaPlanet(Parent, maxMass);
                }
                else if (chance <= 0.65) // Most will be standard terrestrial.
                {
                    satellite = new TerrestrialPlanet(Parent, maxMass);
                }
                else if (chance <= 0.75)
                {
                    satellite = new IronPlanet(Parent, maxMass);
                }
                else
                {
                    satellite = new OceanPlanet(Parent, maxMass);
                }
            }

            // Otherwise, if the mass limit allows, there is an even chance that the satellite is a dwarf planet.
            else if (maxMass > DwarfPlanet.minMassForType && Randomizer.Static.NextBoolean())
            {
                chance = Randomizer.Static.NextDouble();
                // Dwarf planets with very low orbits are lava planets due to tidal stress (plus a small percentage of others due to impact trauma).
                if (periapsis < GetRocheLimit(DwarfPlanet.densityForType) * 1.05 || chance <= 0.01)
                {
                    satellite = new LavaDwarfPlanet(Parent, maxMass);
                }
                else if (chance <= 0.75) // Most will be standard.
                {
                    satellite = new DwarfPlanet(Parent, maxMass);
                }
                else
                {
                    satellite = new RockyDwarfPlanet(Parent, maxMass);
                }
            }

            // Otherwise, it is an asteroid, selected from the standard distribution of types.
            else if (maxMass > 0)
            {
                chance = Randomizer.Static.NextDouble();
                if (chance <= 0.75)
                {
                    satellite = new CTypeAsteroid(Parent, maxMass);
                }
                else if (chance <= 0.9)
                {
                    satellite = new STypeAsteroid(Parent, maxMass);
                }
                else
                {
                    satellite = new MTypeAsteroid(Parent, maxMass);
                }
            }

            if (satellite != null)
            {
                Orbits.Orbit.SetOrbit(
                    satellite,
                    this,
                    periapsis,
                    eccentricity,
                    Math.Round(Randomizer.Static.NextDouble(0.5), 4),
                    Math.Round(Randomizer.Static.NextDouble(MathConstants.TwoPI), 4),
                    Math.Round(Randomizer.Static.NextDouble(MathConstants.TwoPI), 4),
                    Math.Round(Randomizer.Static.NextDouble(MathConstants.TwoPI), 4));
            }

            return satellite;
        }

        /// <summary>
        /// Randomly determines the proportionate amount of the composition devoted to the core of a
        /// <see cref="Planemo"/>.
        /// </summary>
        /// <returns>A proportion, from 0.0 to 1.0.</returns>
        /// <remarks>
        /// Cannot be less than the minimum required to become a gas giant rather than a terrestrial planet.
        /// </remarks>
        private protected override double GetCoreProportion() => Math.Min(Randomizer.Static.NextDouble(0.02, 0.2), (MinMassForType ?? 0) / Mass);
    }
}
