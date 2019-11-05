using NeverFoundry.MathAndScience;
using NeverFoundry.MathAndScience.Chemistry;
using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.MathAndScience.Randomization;
using NeverFoundry.WorldFoundry.CelestialBodies.Planetoids.Asteroids;
using NeverFoundry.WorldFoundry.CelestialBodies.Planetoids.Planets.DwarfPlanets;
using NeverFoundry.WorldFoundry.Climate;
using NeverFoundry.WorldFoundry.Place;
using NeverFoundry.WorldFoundry.Space;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace NeverFoundry.WorldFoundry.CelestialBodies.Planetoids.Planets.TerrestrialPlanets
{
    /// <summary>
    /// A terrestrial planet with an unusually high concentration of carbon, rather than silicates,
    /// including such features as naturally-occurring steel, and diamond volcanoes.
    /// </summary>
    [Serializable]
    public class CarbonPlanet : TerrestrialPlanet
    {
        private protected override bool CanHaveOxygen => false;

        private protected override bool CanHaveWater => false;

        private protected override string? PlanemoClassPrefix => "Carbon";

        /// <summary>
        /// Initializes a new instance of <see cref="CarbonPlanet"/>.
        /// </summary>
        internal CarbonPlanet() { }

        /// <summary>
        /// Initializes a new instance of <see cref="CarbonPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parentId">The id of the location which contains this one.</param>
        /// <param name="position">The initial position of this <see cref="CarbonPlanet"/>.</param>
        internal CarbonPlanet(string? parentId, Vector3 position) : base(parentId, position) { }

        /// <summary>
        /// Initializes a new instance of <see cref="CarbonPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parentId">The id of the location which contains this one.</param>
        /// <param name="position">The initial position of this <see cref="CarbonPlanet"/>.</param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="CarbonPlanet"/> during random generation, in kg.
        /// </param>
        internal CarbonPlanet(string? parentId, Vector3 position, Number maxMass) : base(parentId, position, maxMass) { }

        private CarbonPlanet(
            string id,
            string? name,
            bool isPrepopulated,
            double? albedo,
            double? surfaceAlbedo,
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
            IMaterial? hydrosphere,
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
                surfaceAlbedo,
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
                hydrosphere,
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

        private CarbonPlanet(SerializationInfo info, StreamingContext context) : this(
            (string)info.GetValue(nameof(Id), typeof(string)),
            (string?)info.GetValue(nameof(Name), typeof(string)),
            (bool)info.GetValue(nameof(_isPrepopulated), typeof(bool)),
            (double?)info.GetValue(nameof(_albedo), typeof(double?)),
            (double?)info.GetValue(nameof(_surfaceAlbedo), typeof(double?)),
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
            (IMaterial?)info.GetValue(nameof(_hydrosphere), typeof(IMaterial)),
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
            var chance = Randomizer.Instance.NextDouble();

            // If the mass limit allows, there is an even chance that the satellite is a smaller planet.
            if (maxMass > BaseMinMassForType && Randomizer.Instance.NextBool())
            {
                // Select from the standard distribution of types.

                // Planets with very low orbits are lava planets due to tidal stress (plus a small
                // percentage of others due to impact trauma).

                // The maximum mass and density are used to calculate an outer Roche limit (may not
                // be the actual Roche limit for the body which gets generated).
                if (periapsis < GetRocheLimit(BaseMaxDensity) * new Number(105, -2) || chance <= 0.01)
                {
                    return await GetNewInstanceAsync<LavaPlanet>(ParentId, Vector3.Zero, maxMass, orbit).ConfigureAwait(false);
                }
                else if (chance <= 0.45) // Most will be standard terrestrial.
                {
                    return await GetNewInstanceAsync<TerrestrialPlanet>(ParentId, Vector3.Zero, maxMass, orbit).ConfigureAwait(false);
                }
                else if (chance <= 0.77)
                {
                    return await GetNewInstanceAsync<CarbonPlanet>(ParentId, Vector3.Zero, maxMass, orbit).ConfigureAwait(false);
                }
                else
                {
                    return await GetNewInstanceAsync<OceanPlanet>(ParentId, Vector3.Zero, maxMass, orbit).ConfigureAwait(false);
                }
            }

            // Otherwise, if the mass limit allows, there is an even chance that the satellite is a dwarf planet.
            else if (maxMass > DwarfPlanet.BaseMinMassForType && Randomizer.Instance.NextBool())
            {
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

        private protected override Number GetCoreProportion() => new Number(4, -1);

        private protected override (ISubstanceReference, decimal)[] GetCoreConstituents()
        {
            // Iron/steel-nickel core (some steel forms naturally in the carbon-rich environment).
            var coreSteel = Randomizer.Instance.NextDecimal(0.945m);
            return new (ISubstanceReference, decimal)[]
            {
                (Substances.GetChemicalReference(Substances.Chemicals.Iron), 0.945m - coreSteel),
                (Substances.GetSolutionReference(Substances.Solutions.CarbonSteel), coreSteel),
                (Substances.GetChemicalReference(Substances.Chemicals.Nickel), 0.055m),
            };
        }

        private protected override IEnumerable<(IMaterial, decimal)> GetCrust(
            IShape planetShape,
            Number crustProportion,
            Number planetMass)
        {
            var crustMass = planetMass * crustProportion;

            var shape = new HollowSphere(
                planetShape.ContainingRadius - (planetShape.ContainingRadius * crustProportion),
                planetShape.ContainingRadius,
                planetShape.Position);

            // Carbonaceous crust of graphite, diamond, and hydrocarbons, with trace minerals

            var graphite = 1m;

            var aluminium = (decimal)Randomizer.Instance.NormalDistributionSample(0.026, 4e-3, minimum: 0);
            var iron = (decimal)Randomizer.Instance.NormalDistributionSample(1.67e-2, 2.75e-3, minimum: 0);
            var titanium = (decimal)Randomizer.Instance.NormalDistributionSample(5.7e-3, 9e-4, minimum: 0);

            var chalcopyrite = (decimal)Randomizer.Instance.NormalDistributionSample(1.1e-3, 1.8e-4, minimum: 0); // copper
            graphite -= chalcopyrite;
            var chromite = (decimal)Randomizer.Instance.NormalDistributionSample(5.5e-4, 9e-5, minimum: 0);
            graphite -= chromite;
            var sphalerite = (decimal)Randomizer.Instance.NormalDistributionSample(8.1e-5, 1.3e-5, minimum: 0); // zinc
            graphite -= sphalerite;
            var galena = (decimal)Randomizer.Instance.NormalDistributionSample(2e-5, 3.3e-6, minimum: 0); // lead
            graphite -= galena;
            var uraninite = (decimal)Randomizer.Instance.NormalDistributionSample(7.15e-6, 1.1e-6, minimum: 0);
            graphite -= uraninite;
            var cassiterite = (decimal)Randomizer.Instance.NormalDistributionSample(6.7e-6, 1.1e-6, minimum: 0); // tin
            graphite -= cassiterite;
            var cinnabar = (decimal)Randomizer.Instance.NormalDistributionSample(1.35e-7, 2.3e-8, minimum: 0); // mercury
            graphite -= cinnabar;
            var acanthite = (decimal)Randomizer.Instance.NormalDistributionSample(5e-8, 8.3e-9, minimum: 0); // silver
            graphite -= acanthite;
            var sperrylite = (decimal)Randomizer.Instance.NormalDistributionSample(1.17e-8, 2e-9, minimum: 0); // platinum
            graphite -= sperrylite;
            var gold = (decimal)Randomizer.Instance.NormalDistributionSample(2.75e-9, 4.6e-10, minimum: 0);
            graphite -= gold;

            var bauxite = aluminium * 1.57m;
            graphite -= bauxite;

            var hematiteIron = iron * 3 / 4 * (decimal)Randomizer.Instance.NormalDistributionSample(1, 0.167, minimum: 0);
            var hematite = hematiteIron * 2.88m;
            graphite -= hematite;
            var magnetite = (iron - hematiteIron) * 4.14m;
            graphite -= magnetite;

            var ilmenite = titanium * 2.33m;
            graphite -= ilmenite;

            var coal = graphite * (decimal)Randomizer.Instance.NormalDistributionSample(0.25, 0.042, minimum: 0);
            graphite -= coal * 2;
            var oil = graphite * (decimal)Randomizer.Instance.NormalDistributionSample(0.25, 0.042, minimum: 0);
            graphite -= oil;
            var gas = graphite * (decimal)Randomizer.Instance.NormalDistributionSample(0.25, 0.042, minimum: 0);
            graphite -= gas;
            var diamond = graphite * (decimal)Randomizer.Instance.NormalDistributionSample(0.125, 0.021, minimum: 0);
            graphite -= diamond;

            var components = new List<(ISubstanceReference, decimal)>();
            if (graphite > 0)
            {
                components.Add((Substances.GetChemicalReference(Substances.Chemicals.AmorphousCarbon), graphite));
            }
            if (coal > 0)
            {
                components.Add((Substances.GetMixtureReference(Substances.Mixtures.Anthracite), coal));
                components.Add((Substances.GetMixtureReference(Substances.Mixtures.BituminousCoal), coal));
            }
            if (oil > 0)
            {
                components.Add((Substances.GetMixtureReference(Substances.Mixtures.Petroleum), oil));
            }
            if (gas > 0)
            {
                components.Add((Substances.GetMixtureReference(Substances.Mixtures.NaturalGas), gas));
            }
            if (diamond > 0)
            {
                components.Add((Substances.GetChemicalReference(Substances.Chemicals.Diamond), diamond));
            }

            if (chalcopyrite > 0)
            {
                components.Add((Substances.GetChemicalReference(Substances.Chemicals.Chalcopyrite), chalcopyrite));
            }
            if (chromite > 0)
            {
                components.Add((Substances.GetChemicalReference(Substances.Chemicals.Chromite), chromite));
            }
            if (sphalerite > 0)
            {
                components.Add((Substances.GetSolutionReference(Substances.Solutions.Sphalerite), sphalerite));
            }
            if (galena > 0)
            {
                components.Add((Substances.GetChemicalReference(Substances.Chemicals.Galena), galena));
            }
            if (uraninite > 0)
            {
                components.Add((Substances.GetSolutionReference(Substances.Solutions.Uraninite), uraninite));
            }
            if (cassiterite > 0)
            {
                components.Add((Substances.GetChemicalReference(Substances.Chemicals.Cassiterite), cassiterite));
            }
            if (cinnabar > 0)
            {
                components.Add((Substances.GetChemicalReference(Substances.Chemicals.Cinnabar), cinnabar));
            }
            if (acanthite > 0)
            {
                components.Add((Substances.GetChemicalReference(Substances.Chemicals.Acanthite), acanthite));
            }
            if (sperrylite > 0)
            {
                components.Add((Substances.GetChemicalReference(Substances.Chemicals.Sperrylite), sperrylite));
            }
            if (gold > 0)
            {
                components.Add((Substances.GetChemicalReference(Substances.Chemicals.Gold), gold));
            }
            if (bauxite > 0)
            {
                components.Add((Substances.GetMixtureReference(Substances.Mixtures.Bauxite), bauxite));
            }
            if (hematite > 0)
            {
                components.Add((Substances.GetChemicalReference(Substances.Chemicals.Hematite), hematite));
            }
            if (magnetite > 0)
            {
                components.Add((Substances.GetChemicalReference(Substances.Chemicals.Magnetite), magnetite));
            }
            if (ilmenite > 0)
            {
                components.Add((Substances.GetChemicalReference(Substances.Chemicals.Ilmenite), ilmenite));
            }

            yield return (new Material(
                (double)(crustMass / shape.Volume),
                crustMass,
                shape,
                null,
                components.ToArray()), 1);
        }

        private protected override IEnumerable<(IMaterial, decimal)> GetMantle(
            IShape planetShape,
            Number mantleProportion,
            Number crustProportion,
            Number planetMass,
            IShape coreShape,
            double coreTemp)
        {
            var mantleBoundaryDepth = planetShape.ContainingRadius * crustProportion;
            var mantleBoundaryTemp = (double)(mantleBoundaryDepth * new Number(115, -2));

            var innerTemp = coreTemp;

            var innerBoundary = planetShape.ContainingRadius;
            var mantleTotalDepth = (innerBoundary * mantleProportion) - coreShape.ContainingRadius;

            var mantleMass = planetMass * mantleProportion;

            // Molten silicon carbide lower mantle
            var lowerLayer = Number.Max(0, Randomizer.Instance.NextNumber(-Number.Deci, new Number(55, -2))) / mantleProportion;
            if (lowerLayer.IsPositive)
            {
                var lowerLayerMass = mantleMass * lowerLayer;

                var lowerLayerBoundary = innerBoundary + (mantleTotalDepth * mantleProportion);
                var lowerLayerShape = new HollowSphere(
                    innerBoundary,
                    lowerLayerBoundary,
                    planetShape.Position);
                innerBoundary = lowerLayerBoundary;

                var lowerLayerBoundaryTemp = innerTemp.Lerp(mantleBoundaryTemp, (double)lowerLayer);
                var lowerLayerTemp = (lowerLayerBoundaryTemp + innerTemp) / 2;
                innerTemp = lowerLayerTemp;

                yield return (new Material(
                    Substances.GetChemicalReference(Substances.Chemicals.SiliconCarbide),
                    (double)(lowerLayerMass / lowerLayerShape.Volume),
                    lowerLayerMass,
                    lowerLayerShape,
                    lowerLayerTemp),
                    (decimal)lowerLayer);
            }

            // Diamond upper layer
            var upperLayerProportion = 1 - lowerLayer;

            var upperLayerMass = mantleMass * upperLayerProportion;

            var upperLayerBoundary = planetShape.ContainingRadius + mantleBoundaryDepth;
            var upperLayerShape = new HollowSphere(
                innerBoundary,
                upperLayerBoundary,
                planetShape.Position);

            var upperLayerTemp = (mantleBoundaryTemp + innerTemp) / 2;

            yield return (new Material(
                Substances.GetChemicalReference(Substances.Chemicals.Diamond),
                (double)(upperLayerMass / upperLayerShape.Volume),
                upperLayerMass,
                upperLayerShape,
                upperLayerTemp),
                (decimal)upperLayerProportion);
        }
    }
}
