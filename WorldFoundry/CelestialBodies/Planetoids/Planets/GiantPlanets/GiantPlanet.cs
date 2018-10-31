using MathAndScience;
using Substances;
using System;
using System.Collections.Generic;
using MathAndScience.Numerics;
using WorldFoundry.CelestialBodies.Planetoids.Asteroids;
using WorldFoundry.CelestialBodies.Planetoids.Planets.DwarfPlanets;
using WorldFoundry.CelestialBodies.Planetoids.Planets.TerrestrialPlanets;
using WorldFoundry.Climate;
using WorldFoundry.Space;
using MathAndScience.Shapes;

namespace WorldFoundry.CelestialBodies.Planetoids.Planets.GiantPlanets
{
    /// <summary>
    /// A gas giant planet (excluding ice giants, which have their own subclass).
    /// </summary>
    public class GiantPlanet : Planemo
    {
        internal const int MaxDensity = 1650;
        internal const double Space = 2.5e8;

        private protected const int MinDensity = 1100;
        private protected const int SubMinDensity = 600;

        private protected override string BaseTypeName => "Gas Giant";

        private protected override bool HasFlatSurface => true;

        // At around this limit the planet will have sufficient mass to sustain fusion, and become a brown dwarf.
        private protected override double? MaxMassForType => 2.5e28;

        // Set to 75 for GiantPlanet. For reference, Jupiter has 67 moons, and Saturn
        // has 62 (non-ring) moons.
        private protected override int MaxSatellites => 75;

        // Below this limit the planet will not have sufficient mass to retain hydrogen, and will be a terrestrial planet.
        private protected override double? MinMassForType => 6.0e25;

        private protected override double RingChance => 90;

        /// <summary>
        /// Initializes a new instance of <see cref="GiantPlanet"/>.
        /// </summary>
        internal GiantPlanet() { }

        /// <summary>
        /// Initializes a new instance of <see cref="GiantPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="GiantPlanet"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="GiantPlanet"/>.</param>
        internal GiantPlanet(CelestialRegion parent, Vector3 position) : base(parent, position) { }

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
        internal GiantPlanet(CelestialRegion parent, Vector3 position, double maxMass) : base(parent, position, maxMass) { }

        private protected override void GenerateAlbedo() => Albedo = Randomizer.Instance.NextDouble(0.275, 0.35);

        private protected override void GenerateAtmosphere()
        {
            var trace = Math.Round(Randomizer.Instance.NextDouble(0.025), 4);

            var h = Math.Round(Randomizer.Instance.NextDouble(0.75, 0.97), 4);
            var he = 1 - h - trace;

            var ch4 = Math.Round(Randomizer.Instance.NextDouble(trace), 4);
            trace -= ch4;

            // 50% chance not to have each of these components
            var c2h6 = Math.Round(Math.Max(0, Randomizer.Instance.NextDouble(-0.5, 0.5)), 5);
            var traceTotal = c2h6;
            var nh3 = Math.Round(Math.Max(0, Randomizer.Instance.NextDouble(-0.5, 0.5)), 5);
            traceTotal += nh3;
            var waterVapor = Math.Round(Math.Max(0, Randomizer.Instance.NextDouble(-0.5, 0.5)), 5);
            traceTotal += waterVapor;

            double water = 0, ice = 0;
            if (AverageBlackbodySurfaceTemperature < Chemical.Water.AntoineMinimumTemperature
                || (AverageBlackbodySurfaceTemperature < Chemical.Water.AntoineMaximumTemperature
                && Chemical.Water.GetVaporPressure(AverageBlackbodySurfaceTemperature) <= 1000))
            {
                water = Randomizer.Instance.NextDouble();
                ice = Randomizer.Instance.NextDouble();
                traceTotal += water + ice;
            }

            double ch4Liquid = 0, ch4Ice = 0;
            if (AverageBlackbodySurfaceTemperature < Chemical.Methane.AntoineMinimumTemperature
                || (AverageBlackbodySurfaceTemperature < Chemical.Methane.AntoineMaximumTemperature
                && Chemical.Methane.GetVaporPressure(AverageBlackbodySurfaceTemperature) <= 1000))
            {
                ch4Liquid = Randomizer.Instance.NextDouble();
                ch4Ice = Randomizer.Instance.NextDouble();
                traceTotal += ch4Liquid + ch4Ice;
            }

            double nh3Liquid = 0, nh3Ice = 0;
            if (AverageBlackbodySurfaceTemperature < Chemical.Ammonia.AntoineMinimumTemperature
                || (AverageBlackbodySurfaceTemperature < Chemical.Ammonia.AntoineMaximumTemperature
                && Chemical.Ammonia.GetVaporPressure(AverageBlackbodySurfaceTemperature) <= 1000))
            {
                nh3Liquid = Randomizer.Instance.NextDouble();
                nh3Ice = Randomizer.Instance.NextDouble();
                traceTotal += nh3Liquid + nh3Ice;
            }

            var nh4sh = Randomizer.Instance.NextDouble();
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

            var atmosphere = new Composite(
                (Chemical.Hydrogen, Phase.Gas, h),
                (Chemical.Helium, Phase.Gas, he),
                (Chemical.Methane, Phase.Gas, ch4));
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

        private protected override Planetoid GenerateSatellite(double periapsis, double eccentricity, double maxMass)
        {
            Planetoid satellite = null;
            double chance;

            // If the mass limit allows, there is an even chance that the satellite is a smaller planet.
            if (maxMass > TerrestrialPlanet._minMassForType && Randomizer.Instance.NextBoolean())
            {
                // Select from the standard distribution of types.
                chance = Randomizer.Instance.NextDouble();

                // Planets with very low orbits are lava planets due to tidal
                // stress (plus a small percentage of others due to impact trauma).

                // The maximum mass and density are used to calculate an outer
                // Roche limit (may not be the actual Roche limit for the body
                // which gets generated).
                if (periapsis < GetRocheLimit(TerrestrialPlanet._maxDensity) * 1.05 || chance <= 0.01)
                {
                    satellite = new LavaPlanet(Parent, Vector3.Zero, maxMass);
                }
                else if (chance <= 0.65) // Most will be standard terrestrial.
                {
                    satellite = new TerrestrialPlanet(Parent, Vector3.Zero, maxMass);
                }
                else if (chance <= 0.75)
                {
                    satellite = new IronPlanet(Parent, Vector3.Zero, maxMass);
                }
                else
                {
                    satellite = new OceanPlanet(Parent, Vector3.Zero, maxMass);
                }
            }

            // Otherwise, if the mass limit allows, there is an even chance that the satellite is a dwarf planet.
            else if (maxMass > DwarfPlanet._minMassForType && Randomizer.Instance.NextBoolean())
            {
                chance = Randomizer.Instance.NextDouble();
                // Dwarf planets with very low orbits are lava planets due to tidal stress (plus a small percentage of others due to impact trauma).
                if (periapsis < GetRocheLimit(DwarfPlanet._densityForType) * 1.05 || chance <= 0.01)
                {
                    satellite = new LavaDwarfPlanet(Parent, Vector3.Zero, maxMass);
                }
                else if (chance <= 0.75) // Most will be standard.
                {
                    satellite = new DwarfPlanet(Parent, Vector3.Zero, maxMass);
                }
                else
                {
                    satellite = new RockyDwarfPlanet(Parent, Vector3.Zero, maxMass);
                }
            }

            // Otherwise, it is an asteroid, selected from the standard distribution of types.
            else if (maxMass > 0)
            {
                chance = Randomizer.Instance.NextDouble();
                if (chance <= 0.75)
                {
                    satellite = new CTypeAsteroid(Parent, Vector3.Zero, maxMass);
                }
                else if (chance <= 0.9)
                {
                    satellite = new STypeAsteroid(Parent, Vector3.Zero, maxMass);
                }
                else
                {
                    satellite = new MTypeAsteroid(Parent, Vector3.Zero, maxMass);
                }
            }

            if (satellite != null)
            {
                Orbit.SetOrbit(
                    satellite,
                    this,
                    periapsis,
                    eccentricity,
                    Randomizer.Instance.NextDouble(0.5),
                    Randomizer.Instance.NextDouble(MathConstants.TwoPI),
                    Randomizer.Instance.NextDouble(MathConstants.TwoPI),
                    Randomizer.Instance.NextDouble(MathConstants.TwoPI));
            }

            return satellite;
        }

        private protected override IEnumerable<(IComposition, double)> GetCore(double mass)
        {
            var innerCoreProportion = GetInnerCoreProportion(mass);

            yield return (GetIronNickelCore(), innerCoreProportion);

            // Molten rock outer core.
            yield return (new Material(Chemical.Rock, Phase.Liquid), 1 - innerCoreProportion);
        }

        private protected override IEnumerable<(IComposition, double)> GetCrust()
        {
            yield break;
        }

        private protected override double GetCrustProportion(IShape shape) => 0;

        // Relatively low chance of a "puffy" giant (Saturn-like, low-density).
        private protected override double? GetDensity()
            => Randomizer.Instance.NextDouble() <= 0.2
                ? Math.Round(Randomizer.Instance.NextDouble(SubMinDensity, MinDensity))
                : Math.Round(Randomizer.Instance.NextDouble(MinDensity, MaxDensity));

        private protected double GetInnerCoreProportion(double mass)
            => Math.Min(Randomizer.Instance.NextDouble(0.02, 0.2), (MinMassForType ?? 0) / mass);

        private protected override IEnumerable<(IComposition, double)> GetMantle(IShape shape, double proportion)
        {
            // Metallic hydrogen lower mantle
            var metalH = Math.Max(0, Randomizer.Instance.NextDouble(-0.1, 0.55)) / proportion;
            if (metalH > 0)
            {
                yield return (new Material(Chemical.Hydrogen_Metallic, Phase.Liquid), metalH);
            }

            // Supercritical fluid upper layer (blends seamlessly with lower atmosphere)
            var upperLayerProportion = 1 - metalH;
            var water = upperLayerProportion;
            var fluidH = water * 0.71;
            water -= fluidH;
            var fluidHe = water * 0.24;
            water -= fluidHe;
            var ne = Randomizer.Instance.NextDouble(water);
            water -= ne;
            var ch4 = Randomizer.Instance.NextDouble(water);
            water = Math.Max(0, water - ch4);
            var nh4 = Randomizer.Instance.NextDouble(water);
            water = Math.Max(0, water - nh4);
            var c2h6 = Randomizer.Instance.NextDouble(water);
            water = Math.Max(0, water - c2h6);
            var upperLayer = new Composite(
                (Chemical.Hydrogen, Phase.Liquid, 0.71),
                (Chemical.Helium, Phase.Liquid, 0.24),
                (Chemical.Neon, Phase.Liquid, ne));
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
            yield return (upperLayer, upperLayerProportion);
        }

        private protected override double GetMass(IShape shape = null)
            => Math.Round(Randomizer.Instance.NextDouble(MinMass, MaxMass));
    }
}
