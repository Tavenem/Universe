using NeverFoundry.MathAndScience;
using NeverFoundry.MathAndScience.Chemistry;
using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.MathAndScience.Randomization;
using NeverFoundry.WorldFoundry.CelestialBodies.Planetoids.Asteroids;
using NeverFoundry.WorldFoundry.CelestialBodies.Planetoids.Planets.DwarfPlanets;
using NeverFoundry.WorldFoundry.CelestialBodies.Planetoids.Planets.TerrestrialPlanets;
using NeverFoundry.WorldFoundry.Climate;
using NeverFoundry.WorldFoundry.Place;
using NeverFoundry.WorldFoundry.Space;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace NeverFoundry.WorldFoundry.CelestialBodies.Planetoids.Planets.GiantPlanets
{
    /// <summary>
    /// A gas giant planet (excluding ice giants, which have their own subclass).
    /// </summary>
    [Serializable]
    public class GiantPlanet : Planemo
    {
        internal const int MaxDensity = 1650;

        private protected const int MinDensity = 1100;
        private protected const int SubMinDensity = 600;

        internal static readonly Number Space = new Number(2.5, 8);

        private protected override string BaseTypeName => "Gas Giant";

        private protected override bool HasFlatSurface => true;

        // At around this limit the planet will have sufficient mass to sustain fusion, and become a brown dwarf.
        private protected override Number? MaxMassForType => new Number(2.5, 28);

        // Set to 75 for GiantPlanet. For reference, Jupiter has 67 moons, and Saturn
        // has 62 (non-ring) moons.
        private protected override int MaxSatellites => 75;

        // Below this limit the planet will not have sufficient mass to retain hydrogen, and will be a terrestrial planet.
        private protected override Number? MinMassForType => new Number(6, 25);

        private protected override double RingChance => 90;

        /// <summary>
        /// Initializes a new instance of <see cref="GiantPlanet"/>.
        /// </summary>
        internal GiantPlanet() { }

        /// <summary>
        /// Initializes a new instance of <see cref="GiantPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parentId">The id of the location which contains this one.</param>
        /// <param name="position">The initial position of this <see cref="GiantPlanet"/>.</param>
        internal GiantPlanet(string? parentId, Vector3 position) : base(parentId, position) { }

        /// <summary>
        /// Initializes a new instance of <see cref="GiantPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parentId">The id of the location which contains this one.</param>
        /// <param name="position">The initial position of this <see cref="GiantPlanet"/>.</param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="GiantPlanet"/> during random generation, in kg.
        /// </param>
        internal GiantPlanet(string? parentId, Vector3 position, Number maxMass) : base(parentId, position, maxMass) { }

        private protected GiantPlanet(
            string id,
            string? name,
            bool isPrepopulated,
            double? albedo,
            Vector3 velocity,
            double normalizedSeaLevel,
            int seed1,
            int seed2,
            int seed3,
            int seed4,
            int seed5,
            double? angleOfRotation,
            Atmosphere? atmosphere,
            double? axialPrecession,
            bool? hasMagnetosphere,
            double? maxElevation,
            Number? rotationalOffset,
            Number? rotationalPeriod,
            List<Resource>? resources,
            List<string>? satelliteIds,
            List<SurfaceRegion>? surfaceRegions,
            Number? maxMass,
            Orbit? orbit,
            IMaterial? material,
            List<PlanetaryRing>? rings,
            string? parentId,
            byte[]? depthMap,
            byte[]? elevationMap,
            byte[]? flowMap,
            byte[][]? precipitationMaps,
            byte[][]? snowfallMaps,
            byte[]? temperatureMapSummer,
            byte[]? temperatureMapWinter,
            double? maxFlow)
            : base(
                id,
                name,
                isPrepopulated,
                albedo,
                velocity,
                normalizedSeaLevel,
                seed1,
                seed2,
                seed3,
                seed4,
                seed5,
                angleOfRotation,
                atmosphere,
                axialPrecession,
                hasMagnetosphere,
                maxElevation,
                rotationalOffset,
                rotationalPeriod,
                resources,
                satelliteIds,
                surfaceRegions,
                maxMass,
                orbit,
                material,
                rings,
                parentId,
                depthMap,
                elevationMap,
                flowMap,
                precipitationMaps,
                snowfallMaps,
                temperatureMapSummer,
                temperatureMapWinter,
                maxFlow)
        { }

        private GiantPlanet(SerializationInfo info, StreamingContext context) : this(
            (string)info.GetValue(nameof(Id), typeof(string)),
            (string?)info.GetValue(nameof(Name), typeof(string)),
            (bool)info.GetValue(nameof(_isPrepopulated), typeof(bool)),
            (double?)info.GetValue(nameof(_albedo), typeof(double?)),
            (Vector3)info.GetValue(nameof(Velocity), typeof(Vector3)),
            (double)info.GetValue(nameof(_normalizedSeaLevel), typeof(double)),
            (int)info.GetValue(nameof(_seed1), typeof(int)),
            (int)info.GetValue(nameof(_seed2), typeof(int)),
            (int)info.GetValue(nameof(_seed3), typeof(int)),
            (int)info.GetValue(nameof(_seed4), typeof(int)),
            (int)info.GetValue(nameof(_seed5), typeof(int)),
            (double?)info.GetValue(nameof(_angleOfRotation), typeof(double?)),
            (Atmosphere?)info.GetValue(nameof(Atmosphere), typeof(Atmosphere)),
            (double?)info.GetValue(nameof(_axialPrecession), typeof(double?)),
            (bool?)info.GetValue(nameof(HasMagnetosphere), typeof(bool?)),
            (double?)info.GetValue(nameof(MaxElevation), typeof(double?)),
            (Number?)info.GetValue(nameof(RotationalOffset), typeof(Number?)),
            (Number?)info.GetValue(nameof(RotationalPeriod), typeof(Number?)),
            (List<Resource>?)info.GetValue(nameof(Resources), typeof(List<Resource>)),
            (List<string>?)info.GetValue(nameof(_satelliteIDs), typeof(List<string>)),
            (List<SurfaceRegion>?)info.GetValue(nameof(SurfaceRegions), typeof(List<SurfaceRegion>)),
            (Number?)info.GetValue(nameof(MaxMass), typeof(Number?)),
            (Orbit?)info.GetValue(nameof(Orbit), typeof(Orbit?)),
            (IMaterial?)info.GetValue(nameof(_material), typeof(IMaterial)),
            (List<PlanetaryRing>?)info.GetValue(nameof(Rings), typeof(List<PlanetaryRing>)),
            (string)info.GetValue(nameof(ParentId), typeof(string)),
            (byte[])info.GetValue(nameof(_depthMap), typeof(byte[])),
            (byte[])info.GetValue(nameof(_elevationMap), typeof(byte[])),
            (byte[])info.GetValue(nameof(_flowMap), typeof(byte[])),
            (byte[][])info.GetValue(nameof(_precipitationMaps), typeof(byte[][])),
            (byte[][])info.GetValue(nameof(_snowfallMaps), typeof(byte[][])),
            (byte[])info.GetValue(nameof(_temperatureMapSummer), typeof(byte[])),
            (byte[])info.GetValue(nameof(_temperatureMapWinter), typeof(byte[])),
            (double?)info.GetValue(nameof(_maxFlow), typeof(double?)))
        { }

        private protected override async Task GenerateAlbedoAsync() => await SetAlbedoAsync(Randomizer.Instance.NextDouble(0.275, 0.35)).ConfigureAwait(false);

        private protected override async Task GenerateAtmosphereAsync()
        {
            var trace = Randomizer.Instance.NextDecimal(0.025m);

            var h = Randomizer.Instance.NextDecimal(0.75m, 0.97m);
            var he = 1 - h - trace;

            var ch4 = Randomizer.Instance.NextDecimal(trace);
            trace -= ch4;

            // 50% chance not to have each of these components
            var c2h6 = Math.Max(0, Randomizer.Instance.NextDecimal(-0.5m, 0.5m));
            var traceTotal = c2h6;
            var nh3 = Math.Max(0, Randomizer.Instance.NextDecimal(-0.5m, 0.5m));
            traceTotal += nh3;
            var waterVapor = Math.Max(0, Randomizer.Instance.NextDecimal(-0.5m, 0.5m));
            traceTotal += waterVapor;

            var nh4sh = Randomizer.Instance.NextDecimal();
            traceTotal += nh4sh;

            var ratio = trace / traceTotal;
            c2h6 *= ratio;
            nh3 *= ratio;
            waterVapor *= ratio;
            nh4sh *= ratio;

            var components = new List<(ISubstanceReference, decimal)>()
            {
                (Substances.All.Hydrogen.GetChemicalReference(), h),
                (Substances.All.Helium.GetChemicalReference(), he),
                (Substances.All.Methane.GetChemicalReference(), ch4),
            };
            if (c2h6 > 0)
            {
                components.Add((Substances.All.Ethane.GetChemicalReference(), c2h6));
            }
            if (nh3 > 0)
            {
                components.Add((Substances.All.Ammonia.GetChemicalReference(), nh3));
            }
            if (waterVapor > 0)
            {
                components.Add((Substances.All.Water.GetChemicalReference(), waterVapor));
            }
            if (nh4sh > 0)
            {
                components.Add((Substances.All.AmmoniumHydrosulfide.GetChemicalReference(), nh4sh));
            }

            _atmosphere = await Atmosphere.GetNewInstanceAsync(this, 1000, components.ToArray()).ConfigureAwait(false);
        }

        private protected override async Task<Planetoid?> GenerateSatelliteAsync(Number periapsis, double eccentricity, Number maxMass)
        {
            var orbit = new OrbitalParameters(
                this,
                periapsis,
                eccentricity,
                Randomizer.Instance.NextDouble(0.5),
                Randomizer.Instance.NextDouble(MathAndScience.Constants.Doubles.MathConstants.TwoPI),
                Randomizer.Instance.NextDouble(MathAndScience.Constants.Doubles.MathConstants.TwoPI),
                Randomizer.Instance.NextDouble(MathAndScience.Constants.Doubles.MathConstants.TwoPI));
            double chance;

            // If the mass limit allows, there is an even chance that the satellite is a smaller planet.
            if (maxMass > TerrestrialPlanet.BaseMinMassForType && Randomizer.Instance.NextBool())
            {
                // Select from the standard distribution of types.
                chance = Randomizer.Instance.NextDouble();

                // Planets with very low orbits are lava planets due to tidal
                // stress (plus a small percentage of others due to impact trauma).

                // The maximum mass and density are used to calculate an outer
                // Roche limit (may not be the actual Roche limit for the body
                // which gets generated).
                if (periapsis < GetRocheLimit(TerrestrialPlanet.BaseMaxDensity) * new Number(105, -2) || chance <= 0.01)
                {
                    return await GetNewInstanceAsync<LavaPlanet>(ParentId, Vector3.Zero, maxMass, orbit).ConfigureAwait(false);
                }
                else if (chance <= 0.65) // Most will be standard terrestrial.
                {
                    return await GetNewInstanceAsync<TerrestrialPlanet>(ParentId, Vector3.Zero, maxMass, orbit).ConfigureAwait(false);
                }
                else if (chance <= 0.75)
                {
                    return await GetNewInstanceAsync<IronPlanet>(ParentId, Vector3.Zero, maxMass, orbit).ConfigureAwait(false);
                }
                else
                {
                    return await GetNewInstanceAsync<OceanPlanet>(ParentId, Vector3.Zero, maxMass, orbit).ConfigureAwait(false);
                }
            }

            // Otherwise, if the mass limit allows, there is an even chance that the satellite is a dwarf planet.
            else if (maxMass > DwarfPlanet.BaseMinMassForType && Randomizer.Instance.NextBool())
            {
                chance = Randomizer.Instance.NextDouble();
                // Dwarf planets with very low orbits are lava planets due to tidal stress (plus a small percentage of others due to impact trauma).
                if (periapsis < GetRocheLimit(DwarfPlanet.BaseDensityForType) * new Number(105, -2) || chance <= 0.01)
                {
                    return await GetNewInstanceAsync<LavaDwarfPlanet>(ParentId, Vector3.Zero, maxMass, orbit).ConfigureAwait(false);
                }
                else if (chance <= 0.75) // Most will be standard.
                {
                    return await GetNewInstanceAsync<DwarfPlanet>(ParentId, Vector3.Zero, maxMass, orbit).ConfigureAwait(false);
                }
                else
                {
                    return await GetNewInstanceAsync<RockyDwarfPlanet>(ParentId, Vector3.Zero, maxMass, orbit).ConfigureAwait(false);
                }
            }

            // Otherwise, it is an asteroid, selected from the standard distribution of types.
            else if (maxMass > 0)
            {
                chance = Randomizer.Instance.NextDouble();
                if (chance <= 0.75)
                {
                    return await GetNewInstanceAsync<CTypeAsteroid>(ParentId, Vector3.Zero, maxMass, orbit).ConfigureAwait(false);
                }
                else if (chance <= 0.9)
                {
                    return await GetNewInstanceAsync<STypeAsteroid>(ParentId, Vector3.Zero, maxMass, orbit).ConfigureAwait(false);
                }
                else
                {
                    return await GetNewInstanceAsync<MTypeAsteroid>(ParentId, Vector3.Zero, maxMass, orbit).ConfigureAwait(false);
                }
            }

            return null;
        }

        private protected override IEnumerable<(IMaterial, decimal)> GetCore(
            IShape planetShape,
            Number coreProportion,
            Number crustProportion,
            Number planetMass)
        {
            var coreMass = planetMass * coreProportion;

            var coreTemp = (double)(planetShape.ContainingRadius / 3);

            var innerCoreProportion = GetInnerCoreProportion(coreMass);
            var iCP = (decimal)innerCoreProportion;
            var innerCoreMass = coreMass * innerCoreProportion;
            var innerCoreRadius = planetShape.ContainingRadius * coreProportion * innerCoreProportion;
            var innerCoreShape = new Sphere(innerCoreRadius, planetShape.Position);
            yield return (new Material(
                Substances.All.IronNickelAlloy.GetHomogeneousReference(),
                (double)(innerCoreMass / innerCoreShape.Volume),
                innerCoreMass,
                innerCoreShape,
                coreTemp), iCP);

            // Molten rock outer core.
            var outerCoreMass = coreMass - innerCoreMass;
            var outerCoreShape = new HollowSphere(innerCoreRadius, planetShape.ContainingRadius * coreProportion, planetShape.Position);
            yield return (new Material(
                CelestialSubstances.ChondriticRock,
                (double)(outerCoreMass / outerCoreShape.Volume),
                outerCoreMass,
                outerCoreShape,
                coreTemp), 1 - iCP);
        }

        private protected override IEnumerable<(IMaterial, decimal)> GetCrust(
            IShape planetShape,
            Number crustProportion,
            Number planetMass)
        {
            yield break;
        }

        private protected override Number GetCrustProportion(IShape shape) => Number.Zero;

        // Relatively low chance of a "puffy" giant (Saturn-like, low-density).
        private protected override double GetDensity()
            => Randomizer.Instance.NextDouble() <= 0.2
                ? Randomizer.Instance.NextDouble(SubMinDensity, MinDensity)
                : Randomizer.Instance.NextDouble(MinDensity, MaxDensity);

        private protected Number GetInnerCoreProportion(Number mass)
            => Number.Min(Randomizer.Instance.NextNumber(new Number(2, -2), new Number(2, -1)), (MinMassForType ?? 0) / mass);

        private protected override IEnumerable<(IMaterial, decimal)> GetMantle(
            IShape planetShape,
            Number mantleProportion,
            Number crustProportion,
            Number planetMass,
            IShape coreShape,
            double coreTemp)
        {
            var mantleBoundaryDepth = planetShape.ContainingRadius * crustProportion;
            var mantleBoundaryTemp = (double)mantleBoundaryDepth * 1.15;

            var innerTemp = coreTemp;

            var innerBoundary = planetShape.ContainingRadius;
            var mantleTotalDepth = (innerBoundary * mantleProportion) - coreShape.ContainingRadius;

            var mantleMass = planetMass * mantleProportion;

            // Metallic hydrogen lower mantle
            var metalH = Number.Max(0, Randomizer.Instance.NextNumber(-Number.Deci, new Number(55, -2))) / mantleProportion;
            if (metalH.IsPositive)
            {
                var metalHMass = mantleMass * metalH;

                var metalHBoundary = innerBoundary + (mantleTotalDepth * mantleProportion);
                var metalHShape = new HollowSphere(
                    innerBoundary,
                    metalHBoundary,
                    planetShape.Position);
                innerBoundary = metalHBoundary;

                var metalHBoundaryTemp = innerTemp.Lerp(mantleBoundaryTemp, (double)metalH);
                var metalHTemp = (metalHBoundaryTemp + innerTemp) / 2;
                innerTemp = metalHTemp;

                yield return (new Material(
                    Substances.All.MetallicHydrogen.GetChemicalReference(),
                    (double)(metalHMass / metalHShape.Volume),
                    metalHMass,
                    metalHShape,
                    metalHTemp),
                    (decimal)metalH);
            }

            // Supercritical fluid upper layer (blends seamlessly with lower atmosphere)
            var upperLayerProportion = 1 - metalH;

            var upperLayerMass = mantleMass * upperLayerProportion;

            var upperLayerBoundary = planetShape.ContainingRadius + mantleBoundaryDepth;
            var upperLayerShape = new HollowSphere(
                innerBoundary,
                upperLayerBoundary,
                planetShape.Position);

            var upperLayerTemp = (mantleBoundaryTemp + innerTemp) / 2;

            var uLP = (decimal)upperLayerProportion;
            var water = uLP;
            var fluidH = water * 0.71m;
            water -= fluidH;
            var fluidHe = water * 0.24m;
            water -= fluidHe;
            var ne = Randomizer.Instance.NextDecimal(water);
            water -= ne;
            var ch4 = Randomizer.Instance.NextDecimal(water);
            water = Math.Max(0, water - ch4);
            var nh4 = Randomizer.Instance.NextDecimal(water);
            water = Math.Max(0, water - nh4);
            var c2h6 = Randomizer.Instance.NextDecimal(water);
            water = Math.Max(0, water - c2h6);

            var components = new List<(ISubstanceReference, decimal)>()
            {
                (Substances.All.Hydrogen.GetChemicalReference(), 0.71m),
                (Substances.All.Helium.GetChemicalReference(), 0.24m),
                (Substances.All.Neon.GetChemicalReference(), ne),
            };
            if (ch4 > 0)
            {
                components.Add((Substances.All.Methane.GetChemicalReference(), ch4));
            }
            if (nh4 > 0)
            {
                components.Add((Substances.All.Ammonia.GetChemicalReference(), nh4));
            }
            if (c2h6 > 0)
            {
                components.Add((Substances.All.Ethane.GetChemicalReference(), c2h6));
            }
            if (water > 0)
            {
                components.Add((Substances.All.Water.GetChemicalReference(), water));
            }

            yield return (new Material(
                (double)(upperLayerMass / upperLayerShape.Volume),
                upperLayerMass,
                upperLayerShape,
                upperLayerTemp,
                components.ToArray()),
                uLP);
        }

        private protected override ValueTask<Number> GetMassAsync() => new ValueTask<Number>(Randomizer.Instance.NextNumber(MinMass, MaxMass));
    }
}
