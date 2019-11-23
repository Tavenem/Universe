using NeverFoundry.MathAndScience.Chemistry;
using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.MathAndScience.Randomization;
using NeverFoundry.WorldFoundry.CelestialBodies.Planetoids.Asteroids;
using NeverFoundry.WorldFoundry.Climate;
using NeverFoundry.WorldFoundry.Place;
using NeverFoundry.WorldFoundry.Space;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace NeverFoundry.WorldFoundry.CelestialBodies.Planetoids.Planets.DwarfPlanets
{
    /// <summary>
    /// A dwarf planet: a body large enough to form a roughly spherical shape under its own gravity,
    /// but not large enough to clear its orbital "neighborhood" of smaller bodies.
    /// </summary>
    [Serializable]
    public class DwarfPlanet : Planemo
    {
        internal static readonly Number Space = new Number(1.5, 6);

        private protected override string BaseTypeName => "Dwarf Planet";

        internal static readonly double BaseDensityForType = 2000;
        private protected override double DensityForType => BaseDensityForType;

        // An arbitrary limit separating rogue dwarf planets from rogue planets.
        // Within orbital systems, a calculated value for clearing the neighborhood is used instead.
        private protected override Number? MaxMassForType => new Number(2, 22);

        internal static readonly Number BaseMinMassForType = new Number(3.4, 20);
        // The minimum to achieve hydrostatic equilibrium and be considered a dwarf planet.
        private protected override Number? MinMassForType => BaseMinMassForType;

        private protected override double RingChance => 10;

        /// <summary>
        /// Initializes a new instance of <see cref="DwarfPlanet"/>.
        /// </summary>
        internal DwarfPlanet() { }

        /// <summary>
        /// Initializes a new instance of <see cref="DwarfPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parentId">The id of the location which contains this one.</param>
        /// <param name="position">The initial position of this <see cref="DwarfPlanet"/>.</param>
        internal DwarfPlanet(string? parentId, Vector3 position) : base(parentId, position) { }

        /// <summary>
        /// Initializes a new instance of <see cref="DwarfPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parentId">The id of the location which contains this one.</param>
        /// <param name="position">The initial position of this <see cref="DwarfPlanet"/>.</param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="DwarfPlanet"/> during random generation, in kg.
        /// </param>
        internal DwarfPlanet(string? parentId, Vector3 position, Number maxMass) : base(parentId, position, maxMass) { }

        private protected DwarfPlanet(
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

        private DwarfPlanet(SerializationInfo info, StreamingContext context) : this(
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

        internal override async Task GenerateOrbitAsync(CelestialLocation orbitedObject)
            => await WorldFoundry.Space.Orbit.SetOrbitAsync(
                this,
                orbitedObject,
                await GetDistanceToAsync(orbitedObject).ConfigureAwait(false),
                Eccentricity,
                Randomizer.Instance.NextDouble(0.9),
                Randomizer.Instance.NextDouble(MathAndScience.Constants.Doubles.MathConstants.TwoPI),
                Randomizer.Instance.NextDouble(MathAndScience.Constants.Doubles.MathConstants.TwoPI),
                Randomizer.Instance.NextDouble(MathAndScience.Constants.Doubles.MathConstants.TwoPI))
            .ConfigureAwait(false);

        private protected override async Task GenerateAlbedoAsync()
        {
            var ice = 0.0;
            if (!(_atmosphere is null))
            {
                ice = (double)Atmosphere.Material.Constituents
                    .Sum(x => x.substance.Substance
                    .SeparateByPhase(
                        Material.Temperature ?? 0,
                        _atmosphere?.AtmosphericPressure ?? 0,
                        PhaseType.Solid)
                    .First().proportion * x.proportion);
            }

            var albedo = Randomizer.Instance.NextDouble(0.1, 0.6);
            await SetAlbedoAsync((albedo * (1 - ice)) + (0.9 * ice)).ConfigureAwait(false);
        }

        private protected override async Task GenerateAtmosphereAsync()
        {
            // Atmosphere is based solely on the volatile ices present.

            var crust = Material.GetSurface();

            var water = crust.GetProportion(Substances.All.Water.GetChemicalReference());
            var anyIces = water > 0;

            var n2 = crust.GetProportion(Substances.All.Nitrogen.GetChemicalReference());
            anyIces &= n2 > 0;

            var ch4 = crust.GetProportion(Substances.All.Methane.GetChemicalReference());
            anyIces &= ch4 > 0;

            var co = crust.GetProportion(Substances.All.CarbonMonoxide.GetChemicalReference());
            anyIces &= co > 0;

            var co2 = crust.GetProportion(Substances.All.CarbonDioxide.GetChemicalReference());
            anyIces &= co2 > 0;

            var nh3 = crust.GetProportion(Substances.All.Ammonia.GetChemicalReference());
            anyIces &= nh3 > 0;

            if (!anyIces)
            {
                return;
            }

            var components = new List<(ISubstanceReference, decimal)>();
            if (water > 0)
            {
                components.Add((Substances.All.Water.GetChemicalReference(), water));
            }
            if (n2 > 0)
            {
                components.Add((Substances.All.Nitrogen.GetChemicalReference(), n2));
            }
            if (ch4 > 0)
            {
                components.Add((Substances.All.Methane.GetChemicalReference(), ch4));
            }
            if (co > 0)
            {
                components.Add((Substances.All.CarbonMonoxide.GetChemicalReference(), co));
            }
            if (co2 > 0)
            {
                components.Add((Substances.All.CarbonDioxide.GetChemicalReference(), co2));
            }
            if (nh3 > 0)
            {
                components.Add((Substances.All.Ammonia.GetChemicalReference(), nh3));
            }
            _atmosphere = await Atmosphere.GetNewInstanceAsync(this, Randomizer.Instance.NextDouble(2.5), components.ToArray()).ConfigureAwait(false);
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

            // If the mass limit allows, there is an even chance that the satellite is a smaller dwarf planet.
            if (maxMass > MinMass && Randomizer.Instance.NextBool())
            {
                return await GetNewInstanceAsync<DwarfPlanet>(ParentId, Vector3.Zero, maxMass, orbit).ConfigureAwait(false);
            }
            else
            {
                // Otherwise, it is an asteroid, selected from the standard distribution of types.
                var chance = Randomizer.Instance.NextDouble();
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
        }

        private protected override Number GetCoreProportion() => Randomizer.Instance.NextNumber(new Number(2, -1), new Number(55, -2));
    }
}
