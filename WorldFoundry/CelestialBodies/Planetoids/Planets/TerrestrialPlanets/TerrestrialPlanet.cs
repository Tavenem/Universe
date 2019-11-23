using NeverFoundry.MathAndScience;
using NeverFoundry.MathAndScience.Chemistry;
using NeverFoundry.MathAndScience.Constants.Numbers;
using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.MathAndScience.Randomization;
using NeverFoundry.MathAndScience.Time;
using NeverFoundry.WorldFoundry.CelestialBodies.Planetoids.Asteroids;
using NeverFoundry.WorldFoundry.CelestialBodies.Planetoids.Planets.DwarfPlanets;
using NeverFoundry.WorldFoundry.CelestialBodies.Stars;
using NeverFoundry.WorldFoundry.Climate;
using NeverFoundry.WorldFoundry.Place;
using NeverFoundry.WorldFoundry.Space;
using NeverFoundry.WorldFoundry.Space.Galaxies;
using NeverFoundry.WorldFoundry.WorldGrids;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Threading.Tasks;

namespace NeverFoundry.WorldFoundry.CelestialBodies.Planetoids.Planets.TerrestrialPlanets
{
    /// <summary>
    /// A primarily rocky planet, relatively small in comparison to gas and ice giants.
    /// </summary>
    [Serializable]
    public class TerrestrialPlanet : Planemo
    {
        internal static readonly Number Space = new Number(1.75, 7);

        private protected IMaterial? _hydrosphere;
        private protected double? _surfaceAlbedo;

        /// <summary>
        /// Indicates whether or not this planet has a native population of living organisms.
        /// </summary>
        /// <remarks>
        /// The complexity of life is not presumed. If a planet is basically habitable (liquid
        /// surface water), life in at least a single-celled form may be indicated, and may affect
        /// the atmospheric composition.
        /// </remarks>
        public bool HasBiosphere { get; set; }

        /// <summary>
        /// This planet's surface liquids and ices (not necessarily water).
        /// </summary>
        /// <remarks>
        /// Represented as a separate <see cref="IMaterial"/> rather than as a top layer of <see
        /// cref="CelestialLocation.Material"/> for ease of reference to both the soliud surface
        /// layer, and the hydrosphere.
        /// </remarks>
        public IMaterial Hydrosphere => _hydrosphere ?? MathAndScience.Chemistry.Material.Empty;

        private protected virtual bool CanHaveOxygen => true;

        private protected virtual bool CanHaveWater => true;

        private protected override Number ExtremeRotationalPeriod => new Number(22000000);

        internal static readonly double BaseMaxDensity = 6000;
        private protected virtual double MaxDensity => BaseMaxDensity;

        private static readonly Number _BaseMaxMassForType = new Number(6, 25);
        // At around this limit the planet will have sufficient mass to retain hydrogen, and become
        // a giant.
        private protected override Number? MaxMassForType => _BaseMaxMassForType;

        private protected override Number MaxRotationalPeriod => new Number(6500000);

        private int _maxSatellites = 5;
        private protected override int MaxSatellites => _maxSatellites;

        private protected virtual double MinDensity => 3750;

        internal static readonly Number BaseMinMassForType = new Number(2, 22);
        // An arbitrary limit separating rogue dwarf planets from rogue planets. Within orbital
        // systems, a calculated value for clearing the neighborhood is used instead.
        private protected override Number? MinMassForType => BaseMinMassForType;

        private protected override Number MinRotationalPeriod => new Number(40000);

        private protected override string? PlanemoClassPrefix => "Terrestrial";

        private protected override double RingChance => 10;

        /// <summary>
        /// Initializes a new instance of <see cref="TerrestrialPlanet"/>.
        /// </summary>
        internal TerrestrialPlanet() { }

        /// <summary>
        /// Initializes a new instance of <see cref="TerrestrialPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parentId">The id of the location which contains this one.</param>
        /// <param name="position">The initial position of this <see cref="TerrestrialPlanet"/>.</param>
        internal TerrestrialPlanet(string? parentId, Vector3 position) : base(parentId, position) { }

        /// <summary>
        /// Initializes a new instance of <see cref="TerrestrialPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parentId">The id of the location which contains this one.</param>
        /// <param name="position">The initial position of this <see cref="TerrestrialPlanet"/>.</param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="TerrestrialPlanet"/> during random generation, in kg.
        /// </param>
        internal TerrestrialPlanet(string? parentId, Vector3 position, Number maxMass) : base(parentId, position, maxMass) { }

        private protected TerrestrialPlanet(
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
        {
            _surfaceAlbedo = surfaceAlbedo;
            _hydrosphere = hydrosphere;
        }

        private TerrestrialPlanet(SerializationInfo info, StreamingContext context) : this(
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

        /// <summary>
        /// Gets a new instance of the indicated <see cref="TerrestrialPlanet"/> type.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="TerrestrialPlanet"/> to generate.</typeparam>
        /// <param name="parent">The location which contains the new one.</param>
        /// <param name="position">The position of the new location relative to the center of its
        /// <paramref name="parent"/>.</param>
        /// <param name="star">The star the new <see cref="TerrestrialPlanet"/> will orbit.</param>
        /// <param name="planetParams">
        /// A set of <see cref="TerrestrialPlanetParams"/>. If omitted, the defaults will be used.
        /// </param>
        /// <param name="habitabilityRequirements">A set of <see cref="HabitabilityRequirements"/>.
        /// If omitted, <see cref="HabitabilityRequirements.HumanHabitabilityRequirements"/> will be
        /// used.</param>
        /// <returns>A new instance of the indicated <see cref="TerrestrialPlanet"/> type, or <see
        /// langword="null"/> if no instance could be generated with the given parameters.</returns>
        public static async Task<T?> GetNewInstanceAsync<T>(
            Location? parent,
            Vector3 position,
            Star star,
            TerrestrialPlanetParams? planetParams,
            HabitabilityRequirements? habitabilityRequirements = null) where T : TerrestrialPlanet
        {
            if (!(typeof(T).InvokeMember(
                null,
                BindingFlags.CreateInstance | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                null,
                new object?[] { parent?.Id, position }) is T instance))
            {
                return null;
            }

            if (planetParams?.NumSatellites.HasValue == true)
            {
                instance._maxSatellites = planetParams!.Value.NumSatellites!.Value;
            }
            if (planetParams?.HasMagnetosphere.HasValue == true)
            {
                instance.HasMagnetosphere = planetParams!.Value.HasMagnetosphere!.Value;
            }

            instance.Material = await instance.GetMaterialAsync(planetParams, habitabilityRequirements).ConfigureAwait(false);

            double surfaceTemp;
            if (planetParams?.SurfaceTemperature.HasValue == true)
            {
                surfaceTemp = planetParams!.Value.SurfaceTemperature!.Value;
            }
            else if (habitabilityRequirements?.MinimumTemperature.HasValue == true)
            {
                surfaceTemp = habitabilityRequirements!.Value.MaximumTemperature!.HasValue
                    ? (habitabilityRequirements!.Value.MinimumTemperature!.Value
                        + habitabilityRequirements!.Value.MaximumTemperature!.Value)
                        / 2
                    : habitabilityRequirements!.Value.MinimumTemperature!.Value;
            }
            else
            {
                surfaceTemp = await instance.GetAverageBlackbodyTemperatureAsync().ConfigureAwait(false);
            }
            instance.GenerateHydrosphere(planetParams, surfaceTemp);

            await instance.GenerateOrbitAsync(star, planetParams, habitabilityRequirements).ConfigureAwait(false);
            await instance.SetRotationalPeriodAsync().ConfigureAwait(false);

            await instance.InitializeBaseAsync(parent).ConfigureAwait(false);
            return instance;
        }

        /// <summary>
        /// Given a star, generates a terrestrial planet with the given parameters, and puts the
        /// planet in orbit around the star.
        /// </summary>
        /// <param name="system">
        /// A star system in which the new planet will exist.
        /// </param>
        /// <param name="star">
        /// A star which the new planet will orbit, at a distance suitable for habitability.
        /// </param>
        /// <param name="planetParams">
        /// A set of <see cref="TerrestrialPlanetParams"/>. If omitted, the defaults will be used.
        /// </param>
        /// <param name="habitabilityRequirements">A set of <see cref="HabitabilityRequirements"/>.
        /// If omitted, <see cref="HabitabilityRequirements.HumanHabitabilityRequirements"/> will be
        /// used.</param>
        /// <returns>A planet with the given parameters.</returns>
        public static async Task<TerrestrialPlanet?> GetPlanetForStarAsync(
            StarSystem system,
            Star star,
            TerrestrialPlanetParams? planetParams = null,
            HabitabilityRequirements? habitabilityRequirements = null)
        {
            var pParams = planetParams ?? TerrestrialPlanetParams.FromDefaults();
            var requirements = habitabilityRequirements ?? HabitabilityRequirements.HumanHabitabilityRequirements;
            var sanityCheck = 0;
            TerrestrialPlanet? planet;
            do
            {
                planet = await GetNewInstanceAsync<TerrestrialPlanet>(
                    system,
                    Vector3.Zero,
                    star,
                    pParams,
                    requirements).ConfigureAwait(false);
                if (planet is null)
                {
                    sanityCheck++;
                    continue;
                }
                sanityCheck++;
                if (await planet.IsHabitableAsync(requirements).ConfigureAwait(false) == UninhabitabilityReason.None)
                {
                    break;
                }
            } while (sanityCheck <= 100);
            return planet;
        }

        /// <summary>
        /// Given a galaxy, generates a terrestrial planet with the given parameters, orbiting a
        /// Sol-like star in a new system in the given galaxy.
        /// </summary>
        /// <param name="galaxy">A galaxy in which to situate the new planet.</param>
        /// <param name="planetParams">
        /// A set of <see cref="TerrestrialPlanetParams"/>. If omitted, the defaults will be used.
        /// </param>
        /// <param name="habitabilityRequirements">A set of <see cref="HabitabilityRequirements"/>.
        /// If omitted, <see cref="HabitabilityRequirements.HumanHabitabilityRequirements"/> will be
        /// used.</param>
        /// <returns>A planet with the given parameters.</returns>
        public static async Task<TerrestrialPlanet?> GetPlanetForGalaxyAsync(
            Galaxy galaxy,
            TerrestrialPlanetParams? planetParams = null,
            HabitabilityRequirements? habitabilityRequirements = null)
        {
            StarSystem? system;
            var sanityCheck = 10000;
            var isBinary = false;
            Star? star = null;
            do
            {
                system = await galaxy.GetNewChildAsync<StarSystem>(new StarSystemChildDefinition<Star>(SpectralClass.G, LuminosityClass.V)).ConfigureAwait(false);
                if (system != null)
                {
                    var stars = system.GetStarsAsync().GetAsyncEnumerator();
                    if (await stars.MoveNextAsync().ConfigureAwait(false))
                    {
                        star = stars.Current;
                    }
                    // Exclude binary systems, which will interfere with the temperature-balancing logic.
                    isBinary = await stars.MoveNextAsync().ConfigureAwait(false);
                }
                sanityCheck--;
            } while (sanityCheck > 0 && (star is null || isBinary));
            if (system != null)
            {
                await system.SaveAsync().ConfigureAwait(false);
            }
            return system is null || star is null
                ? null
                : await GetPlanetForStarAsync(system, star, planetParams, habitabilityRequirements).ConfigureAwait(false);
        }

        /// <summary>
        /// Given a universe, generates a terrestrial planet with the given parameters, orbiting a
        /// Sol-like star in a new spiral galaxy in the given universe.
        /// </summary>
        /// <param name="universe">A universe in which to situate the new planet.</param>
        /// <param name="planetParams">
        /// A set of <see cref="TerrestrialPlanetParams"/>. If omitted, the defaults will be used.
        /// </param>
        /// <param name="habitabilityRequirements">A set of <see cref="HabitabilityRequirements"/>.
        /// If omitted, <see cref="HabitabilityRequirements.HumanHabitabilityRequirements"/> will be
        /// used.</param>
        /// <returns>A planet with the given parameters.</returns>
        public static async Task<TerrestrialPlanet?> GetPlanetForUniverseAsync(
            Universe universe,
            TerrestrialPlanetParams? planetParams = null,
            HabitabilityRequirements? habitabilityRequirements = null)
        {
            var gsc = await universe.GetNewChildAsync<GalaxySupercluster>().ConfigureAwait(false);
            if (gsc is null)
            {
                return null;
            }
            await gsc.SaveAsync().ConfigureAwait(false);
            var gc = await gsc.GetNewChildAsync<GalaxyCluster>().ConfigureAwait(false);
            if (gc is null)
            {
                return null;
            }
            await gc.SaveAsync().ConfigureAwait(false);
            var gg = await gc.GetNewChildAsync<GalaxyGroup>().ConfigureAwait(false);
            if (gg is null)
            {
                return null;
            }
            await gg.SaveAsync().ConfigureAwait(false);
            var gsg = await gg.GetChildAsync<GalaxySubgroup>().ConfigureAwait(false);
            if (gsg is null)
            {
                return null;
            }
            await gsg.SaveAsync().ConfigureAwait(false);
            var g = await gsg.GetMainGalaxyAsync().ConfigureAwait(false);
            if (g is null)
            {
                return null;
            }
            await g.SaveAsync().ConfigureAwait(false);
            return await GetPlanetForGalaxyAsync(g, planetParams, habitabilityRequirements).ConfigureAwait(false);
        }

        /// <summary>
        /// Generates a terrestrial planet with the given parameters, orbiting a Sol-like star in a
        /// spiral galaxy in a new universe.
        /// </summary>
        /// <param name="planetParams">
        /// A set of <see cref="TerrestrialPlanetParams"/>. If omitted, the defaults will be used.
        /// </param>
        /// <param name="habitabilityRequirements">A set of <see cref="HabitabilityRequirements"/>.
        /// If omitted, <see cref="HabitabilityRequirements.HumanHabitabilityRequirements"/> will be
        /// used.</param>
        /// <returns>A planet with the given parameters.</returns>
        public static async Task<TerrestrialPlanet?> GetPlanetForNewUniverseAsync(
            TerrestrialPlanetParams? planetParams = null,
            HabitabilityRequirements? habitabilityRequirements = null)
        {
            var universe = await Universe.GetNewInstanceAsync().ConfigureAwait(false);
            if (universe is null)
            {
                return null;
            }
            await universe.SaveAsync().ConfigureAwait(false);
            return await GetPlanetForUniverseAsync(universe, planetParams, habitabilityRequirements).ConfigureAwait(false);
        }

        /// <summary>Populates a <see cref="SerializationInfo"></see> with the data needed to
        /// serialize the target object.</summary>
        /// <param name="info">The <see cref="SerializationInfo"></see> to populate with
        /// data.</param>
        /// <param name="context">The destination (see <see cref="StreamingContext"></see>) for this
        /// serialization.</param>
        /// <exception cref="System.Security.SecurityException">The caller does not have the
        /// required permission.</exception>
        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(Id), Id);
            info.AddValue(nameof(Name), Name);
            info.AddValue(nameof(_isPrepopulated), _isPrepopulated);
            info.AddValue(nameof(_albedo), _albedo);
            info.AddValue(nameof(_surfaceAlbedo), _surfaceAlbedo);
            info.AddValue(nameof(Velocity), Velocity);
            info.AddValue(nameof(_normalizedSeaLevel), _normalizedSeaLevel);
            info.AddValue(nameof(_seed1), _seed1);
            info.AddValue(nameof(_seed2), _seed2);
            info.AddValue(nameof(_seed3), _seed3);
            info.AddValue(nameof(_seed4), _seed4);
            info.AddValue(nameof(_seed5), _seed5);
            info.AddValue(nameof(_angleOfRotation), _angleOfRotation);
            info.AddValue(nameof(Atmosphere), _atmosphere);
            info.AddValue(nameof(_axialPrecession), _axialPrecession);
            info.AddValue(nameof(HasMagnetosphere), _hasMagnetosphere);
            info.AddValue(nameof(MaxElevation), _maxElevation);
            info.AddValue(nameof(RotationalOffset), _rotationalOffset);
            info.AddValue(nameof(RotationalPeriod), _rotationalPeriod);
            info.AddValue(nameof(Resources), _resources);
            info.AddValue(nameof(_satelliteIDs), _satelliteIDs);
            info.AddValue(nameof(SurfaceRegions), _surfaceRegions);
            info.AddValue(nameof(MaxMass), _maxMass);
            info.AddValue(nameof(Orbit), _orbit);
            info.AddValue(nameof(_material), _material);
            info.AddValue(nameof(_hydrosphere), _hydrosphere);
            info.AddValue(nameof(Rings), _rings);
            info.AddValue(nameof(ParentId), ParentId);
            info.AddValue(nameof(_depthMap), _depthMap);
            info.AddValue(nameof(_elevationMap), _elevationMap);
            info.AddValue(nameof(_flowMap), _flowMap);
            info.AddValue(nameof(_precipitationMaps), _precipitationMaps);
            info.AddValue(nameof(_snowfallMaps), _snowfallMaps);
            info.AddValue(nameof(_temperatureMapSummer), _temperatureMapSummer);
            info.AddValue(nameof(_temperatureMapWinter), _temperatureMapWinter);
            info.AddValue(nameof(_maxFlow), _maxFlow);
        }

        /// <summary>
        /// Determines the average precipitation at the given <paramref name="position"/> under the
        /// given conditions, over the given duration, in mm.
        /// </summary>
        /// <param name="time">The beginning of the period during which precipitation is to be
        /// determined.</param>
        /// <param name="position">The position on the planet's surface at which to determine
        /// precipitation.</param>
        /// <param name="proportionOfYear">The proportion of the year over which to determine
        /// precipitation.</param>
        /// <returns>
        /// <para>
        /// The average precipitation at the given <paramref name="position"/> and time of year, in
        /// mm, along with the amount of snow which falls.
        /// </para>
        /// <para>
        /// Note that this amount <i>replaces</i> any precipitation that would have fallen as rain;
        /// the return value is to be considered a water-equivalent total value which is equal to
        /// the snow.
        /// </para>
        /// </returns>
        public async Task<(double precipitation, double snow)> GetPrecipitationAsync(Duration time, Vector3 position, float proportionOfYear)
        {
            var trueAnomaly = Orbit?.GetTrueAnomalyAtTime(time) ?? 0;
            var seasonalLatitude = Math.Abs(GetSeasonalLatitude(VectorToLatitude(position), trueAnomaly));
            var temp = await GetSurfaceTemperatureAtTrueAnomalyAsync(trueAnomaly, seasonalLatitude).ConfigureAwait(false);
            temp = await GetTemperatureAtElevationAsync(temp, GetElevationAt(position)).ConfigureAwait(false);
            var precipitation = GetPrecipitation(
                (double)position.X,
                (double)position.Y,
                (double)position.Z,
                seasonalLatitude,
                (float)temp,
                proportionOfYear,
                out var snow);
            return (precipitation, snow);
        }

        /// <summary>
        /// Determines the average precipitation at the given <paramref name="position"/> over the
        /// given duration, in mm.
        /// </summary>
        /// <param name="position">The position on the planet's surface at which to determine
        /// precipitation.</param>
        /// <param name="proportionOfYear">The proportion of the year over which to determine
        /// precipitation.</param>
        /// <returns>
        /// <para>
        /// The average precipitation at the given <paramref name="position"/> and time of year, in
        /// mm, along with the amount of snow which falls.
        /// </para>
        /// <para>
        /// Note that this amount <i>replaces</i> any precipitation that would have fallen as rain;
        /// the return value is to be considered a water-equivalent total value which is equal to
        /// the snow.
        /// </para>
        /// </returns>
        public async Task<(double precipitation, double snow)> GetPrecipitationAsync(Vector3 position, float proportionOfYear)
        {
            var universe = await GetContainingUniverseAsync().ConfigureAwait(false);
            return await GetPrecipitationAsync(universe?.Time.Now ?? Duration.Zero, position, proportionOfYear).ConfigureAwait(false);
        }

        /// <summary>
        /// Determines if the planet is habitable by a species with the given requirements. Does not
        /// imply that the planet could sustain a large-scale population in the long-term, only that
        /// a member of the species can survive on the surface without artificial aid.
        /// </summary>
        /// <param name="habitabilityRequirements">The collection of <see
        /// cref="HabitabilityRequirements"/>.</param>
        /// <returns>
        /// The <see cref="UninhabitabilityReason"/> indicating the reason(s) the planet is
        /// uninhabitable, if any.
        /// </returns>
        public async Task<UninhabitabilityReason> IsHabitableAsync(HabitabilityRequirements habitabilityRequirements)
        {
            var reason = UninhabitabilityReason.None;

            if (!await GetIsHospitableAsync().ConfigureAwait(false))
            {
                reason = UninhabitabilityReason.Other;
            }

            if (!await IsHabitableAsync().ConfigureAwait(false))
            {
                reason |= UninhabitabilityReason.NoWater;
            }

            if (habitabilityRequirements.AtmosphericRequirements != null
                && !Atmosphere.MeetsRequirements(habitabilityRequirements.AtmosphericRequirements))
            {
                reason |= UninhabitabilityReason.UnbreathableAtmosphere;
            }

            // The coldest temp will usually occur at apoapsis for bodies which directly orbit stars
            // (even in multi-star systems, the body would rarely be closer to a companion star even
            // at apoapsis given the orbital selection criteria used in this library). For a moon,
            // the coldest temperature should occur at its parent's own apoapsis, but this is
            // unrelated to the moon's own apses and is effectively impossible to calculate due to
            // the complexities of the potential orbital dynamics, so this special case is ignored.
            if (await GetMinEquatorTemperatureAsync().ConfigureAwait(false) < (habitabilityRequirements.MinimumTemperature ?? 0))
            {
                reason |= UninhabitabilityReason.TooCold;
            }

            // To determine if a planet is too hot, the polar temperature at periapsis is used, since
            // this should be the coldest region at its hottest time.
            if (await GetMaxPolarTemperatureAsync().ConfigureAwait(false) > (habitabilityRequirements.MaximumTemperature ?? double.PositiveInfinity))
            {
                reason |= UninhabitabilityReason.TooHot;
            }

            if (Atmosphere.AtmosphericPressure < (habitabilityRequirements.MinimumPressure ?? 0))
            {
                reason |= UninhabitabilityReason.LowPressure;
            }

            if (Atmosphere.AtmosphericPressure > (habitabilityRequirements.MaximumPressure ?? double.PositiveInfinity))
            {
                reason |= UninhabitabilityReason.HighPressure;
            }

            if (SurfaceGravity < (habitabilityRequirements.MinimumGravity ?? 0))
            {
                reason |= UninhabitabilityReason.LowGravity;
            }

            if (SurfaceGravity > (habitabilityRequirements.MaximumGravity ?? double.PositiveInfinity))
            {
                reason |= UninhabitabilityReason.HighGravity;
            }

            return reason;
        }

        internal override Task GenerateOrbitAsync(CelestialLocation orbitedObject)
            => GenerateOrbitAsync(orbitedObject, null, null);

        private async Task AdjustOrbitForTemperatureAsync(TerrestrialPlanetParams? planetParams, Star star, Number? semiMajorAxis, double trueAnomaly, double targetTemp)
        {
            var temp = await GetTemperatureAsync().ConfigureAwait(false);

            // Orbital distance averaged over time (mean anomaly) = semi-major axis * (1 + eccentricity^2 / 2).
            // This allows calculation of the correct distance/orbit for an average
            // orbital temperature (rather than the temperature at the current position).
            if (planetParams?.RevolutionPeriod.HasValue == true && semiMajorAxis.HasValue)
            {
                var albedo = await GetAlbedoAsync().ConfigureAwait(false);
                star.SetTempForTargetPlanetTemp(targetTemp - (temp ?? 0), semiMajorAxis.Value * (1 + (Eccentricity * Eccentricity / 2)), albedo);
            }
            else
            {
                semiMajorAxis = await GetDistanceForTemperatureAsync(star, targetTemp - (temp ?? 0)).ConfigureAwait(false) / (1 + (Eccentricity * Eccentricity / 2));
                await GenerateOrbitAsync(star, semiMajorAxis.Value, trueAnomaly).ConfigureAwait(false);
            }
            await ResetCachedTemperaturesAsync().ConfigureAwait(false);
        }

        private async Task<double> CalculateGasPhaseMixAsync(
            TerrestrialPlanetParams? planetParams,
            IHomogeneousReference substance,
            double surfaceTemp,
            double adjustedAtmosphericPressure)
        {
            var proportionInHydrosphere = Hydrosphere.GetProportion(substance);
            var water = Substances.All.Water.GetChemicalReference();
            var isWater = substance.Equals(water);
            if (isWater)
            {
                proportionInHydrosphere = Hydrosphere.GetProportion(x =>
                    x.Equals(Substances.All.Seawater.GetHomogeneousReference())
                    || x.Equals(water));
            }

            var vaporProportion = Atmosphere.Material.GetProportion(substance);

            var sub = substance.Homogeneous;
            var vaporPressure = sub.GetVaporPressure(surfaceTemp) ?? 0;

            if (surfaceTemp < sub.AntoineMinimumTemperature
                || (surfaceTemp <= sub.AntoineMaximumTemperature
                && Atmosphere.AtmosphericPressure > vaporPressure))
            {
                CondenseAtmosphericComponent(
                    planetParams,
                    sub,
                    surfaceTemp,
                    proportionInHydrosphere,
                    vaporProportion,
                    vaporPressure,
                    ref adjustedAtmosphericPressure);
            }
            // This indicates that the chemical will fully boil off.
            else if (proportionInHydrosphere > 0)
            {
                EvaporateAtmosphericComponent(
                    sub,
                    proportionInHydrosphere,
                    vaporProportion,
                    ref adjustedAtmosphericPressure);
            }

            if (isWater)
            {
                await CheckCO2ReductionAsync(vaporPressure).ConfigureAwait(false);
            }

            return adjustedAtmosphericPressure;
        }

        private async Task<double> CalculatePhasesAsync(TerrestrialPlanetParams? planetParams, int counter, double adjustedAtmosphericPressure)
        {
            var surfaceTemp = await GetAverageSurfaceTemperatureAsync().ConfigureAwait(false);

            // Despite the theoretical possibility of an atmosphere cold enough to precipitate some
            // of the noble gases, or hydrogen, they are ignored and presumed to exist always as
            // trace atmospheric gases, never surface liquids or ices, or in large enough quantities
            // to form precipitation.

            var methane = Substances.All.Methane.GetChemicalReference();
            adjustedAtmosphericPressure = await CalculateGasPhaseMixAsync(planetParams, methane, surfaceTemp, adjustedAtmosphericPressure).ConfigureAwait(false);

            var carbonMonoxide = Substances.All.CarbonMonoxide.GetChemicalReference();
            adjustedAtmosphericPressure = await CalculateGasPhaseMixAsync(planetParams, carbonMonoxide, surfaceTemp, adjustedAtmosphericPressure).ConfigureAwait(false);

            var carbonDioxide = Substances.All.CarbonDioxide.GetChemicalReference();
            adjustedAtmosphericPressure = await CalculateGasPhaseMixAsync(planetParams, carbonDioxide, surfaceTemp, adjustedAtmosphericPressure).ConfigureAwait(false);

            var nitrogen = Substances.All.Nitrogen.GetChemicalReference();
            adjustedAtmosphericPressure = await CalculateGasPhaseMixAsync(planetParams, nitrogen, surfaceTemp, adjustedAtmosphericPressure).ConfigureAwait(false);

            var oxygen = Substances.All.Oxygen.GetChemicalReference();
            adjustedAtmosphericPressure = await CalculateGasPhaseMixAsync(planetParams, oxygen, surfaceTemp, adjustedAtmosphericPressure).ConfigureAwait(false);

            // No need to check for ozone, since it is only added to atmospheres on planets with
            // liquid surface water, which means temperatures too high for liquid or solid ozone.

            var sulphurDioxide = Substances.All.SulphurDioxide.GetChemicalReference();
            adjustedAtmosphericPressure = await CalculateGasPhaseMixAsync(planetParams, sulphurDioxide, surfaceTemp, adjustedAtmosphericPressure).ConfigureAwait(false);

            // Water is handled differently, since the planet may already have surface water.
            var water = Substances.All.Water.GetChemicalReference();
            var seawater = Substances.All.Seawater.GetHomogeneousReference();
            if (Hydrosphere.Contains(water)
                || Hydrosphere.Contains(seawater)
                || Atmosphere.Material.Contains(water))
            {
                adjustedAtmosphericPressure = await CalculateGasPhaseMixAsync(planetParams, water, surfaceTemp, adjustedAtmosphericPressure).ConfigureAwait(false);
            }

            // Ices and clouds significantly impact albedo.
            var pressure = adjustedAtmosphericPressure;
            var iceAmount = (double)Math.Min(1,
                Hydrosphere.GetSurface()?
                .Constituents.Sum(x => x.substance.Substance.SeparateByPhase(surfaceTemp, pressure, PhaseType.Solid).First().proportion * x.proportion) ?? 0);
            var cloudCover = Atmosphere.AtmosphericPressure
                * (double)Atmosphere.Material.Constituents.Sum(x => x.substance.Substance.SeparateByPhase(surfaceTemp, pressure, PhaseType.Solid | PhaseType.Liquid).First().proportion * x.proportion / 100);
            var reflectiveSurface = Math.Max(iceAmount, cloudCover);
            var surfaceAlbedo = await GetSurfaceAlbedoAsync().ConfigureAwait(false);
            await SetAlbedoAsync((surfaceAlbedo * (1 - reflectiveSurface)) + (0.9 * reflectiveSurface)).ConfigureAwait(false);

            // An albedo change might significantly alter surface temperature, which may require a
            // re-calculation (but not too many). 5K is used as the threshold for re-calculation,
            // which may lead to some inaccuracies, but should avoid over-repetition for small changes.
            if (counter < 10 && Math.Abs(surfaceTemp - await GetAverageSurfaceTemperatureAsync().ConfigureAwait(false)) > 5)
            {
                adjustedAtmosphericPressure = await CalculatePhasesAsync(planetParams, counter + 1, adjustedAtmosphericPressure).ConfigureAwait(false);
            }

            return adjustedAtmosphericPressure;
        }

        private async Task CheckCO2ReductionAsync(double vaporPressure)
        {
            // At least 1% humidity leads to a reduction of CO2 to a trace gas, by a presumed
            // carbon-silicate cycle.

            var water = Substances.All.Water.GetChemicalReference();
            var air = Atmosphere.Material is LayeredComposite lc
                ? lc.Layers[0].material
                : Atmosphere.Material;
            if ((double)(air?.GetProportion(water) ?? 0) * Atmosphere.AtmosphericPressure >= 0.01 * vaporPressure)
            {
                var carbonDioxide = Substances.All.CarbonDioxide.GetChemicalReference();
                var co2 = air?.GetProportion(carbonDioxide) ?? 0;
                if (co2 >= 1e-3m) // reduce CO2 if not already trace
                {
                    co2 = Randomizer.Instance.NextDecimal(15e-6m, 0.001m);

                    // Replace most of the CO2 with inert gases.
                    var nitrogen = Substances.All.Nitrogen.GetChemicalReference();
                    var n2 = Atmosphere.Material.GetProportion(nitrogen) + Atmosphere.Material.GetProportion(carbonDioxide) - co2;
                    Atmosphere.Material.AddConstituent(carbonDioxide, co2);

                    // Some portion of the N2 may be Ar instead.
                    var argon = Substances.All.Argon.GetChemicalReference();
                    var ar = Math.Max(Atmosphere.Material.GetProportion(argon), n2 * Randomizer.Instance.NextDecimal(-0.02m, 0.04m));
                    Atmosphere.Material.AddConstituent(argon, ar);
                    n2 -= ar;

                    // An even smaller fraction may be Kr.
                    var krypton = Substances.All.Krypton.GetChemicalReference();
                    var kr = Math.Max(Atmosphere.Material.GetProportion(krypton), n2 * Randomizer.Instance.NextDecimal(-25e-5m, 0.0005m));
                    Atmosphere.Material.AddConstituent(krypton, kr);
                    n2 -= kr;

                    // An even smaller fraction may be Xe or Ne.
                    var xenon = Substances.All.Xenon.GetChemicalReference();
                    var xe = Math.Max(Atmosphere.Material.GetProportion(xenon), n2 * Randomizer.Instance.NextDecimal(-18e-6m, 35e-6m));
                    Atmosphere.Material.AddConstituent(xenon, xe);
                    n2 -= xe;

                    var neon = Substances.All.Neon.GetChemicalReference();
                    var ne = Math.Max(Atmosphere.Material.GetProportion(neon), n2 * Randomizer.Instance.NextDecimal(-18e-6m, 35e-6m));
                    Atmosphere.Material.AddConstituent(neon, ne);
                    n2 -= ne;

                    Atmosphere.Material.AddConstituent(nitrogen, n2);

                    await Atmosphere.ResetGreenhouseFactorAsync(this).ConfigureAwait(false);
                }
            }
        }

        private void CondenseAtmosphericComponent(
            TerrestrialPlanetParams? planetParams,
            IHomogeneous substance,
            double surfaceTemp,
            decimal proportionInHydrosphere,
            decimal vaporProportion,
            double vaporPressure,
            ref double adjustedAtmosphericPressure)
        {
            var water = Substances.All.Water.GetChemicalReference();

            // Fully precipitate out of the atmosphere when below the freezing point.
            if (!substance.MeltingPoint.HasValue || surfaceTemp < substance.MeltingPoint.Value)
            {
                vaporProportion = 0;

                Atmosphere.Material.RemoveConstituent(substance);

                if (!Atmosphere.Material.Constituents.Any())
                {
                    adjustedAtmosphericPressure = 0;
                }

                if (substance.Equals(water))
                {
                    Atmosphere.ResetWater(this);
                }
            }
            else
            {
                // Adjust vapor present in the atmosphere based on the vapor pressure.
                var pressureRatio = (vaporPressure / Atmosphere.AtmosphericPressure).Clamp(0, 1);
                if (substance.Equals(water) && planetParams?.WaterVaporRatio.HasValue == true)
                {
                    vaporProportion = planetParams!.Value.WaterVaporRatio!.Value;
                }
                else
                {
                    // This would represent 100% humidity. Since this is the case, in principle, only at the
                    // surface of bodies of liquid, and should decrease exponentially with altitude, an
                    // approximation of 25% average humidity overall is used.
                    vaporProportion = (proportionInHydrosphere + vaporProportion) * (decimal)pressureRatio;
                    vaporProportion *= 0.25m;
                }
                if (vaporProportion > 0)
                {
                    var previousGasFraction = 0m;
                    var gasFraction = vaporProportion;
                    Atmosphere.Material.AddConstituent(substance, vaporProportion);

                    if (substance.Equals(water))
                    {
                        Atmosphere.ResetWater(this);

                        // For water, also add a corresponding amount of oxygen, if it's not already present.
                        if (CanHaveOxygen)
                        {
                            var oxygen = Substances.All.Oxygen.GetChemicalReference();
                            var o2 = Atmosphere.Material.GetProportion(oxygen);
                            previousGasFraction += o2;
                            o2 = Math.Max(o2, vaporProportion * 0.0001m);
                            gasFraction += o2;
                            Atmosphere.Material.AddConstituent(oxygen, o2);
                        }
                    }

                    adjustedAtmosphericPressure += adjustedAtmosphericPressure * (double)(gasFraction - previousGasFraction);

                    // At least some precipitation will occur. Ensure a troposphere.
                    Atmosphere.DifferentiateTroposphere();
                }
            }

            var hydro = proportionInHydrosphere;
            var hydrosphereAtmosphereRatio = GetHydrosphereAtmosphereRatio();
            hydro = Math.Max(hydro, hydrosphereAtmosphereRatio <= 0 ? vaporProportion : vaporProportion / hydrosphereAtmosphereRatio);
            if (hydro > proportionInHydrosphere)
            {
                Hydrosphere.GetSurface().AddConstituent(substance, hydro);
            }
        }

        private void EvaporateAtmosphericComponent(
            IHomogeneous substance,
            decimal hydrosphereProportion,
            decimal vaporProportion,
            ref double adjustedAtmosphericPressure)
        {
            if (hydrosphereProportion <= 0)
            {
                return;
            }

            var water = Substances.All.Water.GetChemicalReference();
            if (substance.Equals(water))
            {
                _hydrosphere = Hydrosphere.GetHomogenized();
                Atmosphere.ResetWater(this);
            }

            var gasProportion = hydrosphereProportion * GetHydrosphereAtmosphereRatio();
            var previousGasProportion = vaporProportion;

            Hydrosphere.GetSurface().RemoveConstituent(substance);

            if (substance.Equals(water))
            {
                var seawater = Substances.All.Seawater.GetHomogeneousReference();
                Hydrosphere.GetSurface().RemoveConstituent(seawater);

                // It is presumed that photodissociation will eventually reduce the amount of water
                // vapor to a trace gas (the H2 will be lost due to atmospheric escape, and the
                // oxygen will be lost to surface oxidation).
                var waterVapor = Math.Min(gasProportion, Randomizer.Instance.NextDecimal(0.001m));
                gasProportion = waterVapor;

                var oxygen = Substances.All.Oxygen.GetChemicalReference();
                previousGasProportion += Atmosphere.Material.GetProportion(oxygen);
                var o2 = gasProportion * 0.0001m;
                gasProportion += o2;

                Atmosphere.Material.AddConstituent(substance, waterVapor);
                if (CanHaveOxygen)
                {
                    Atmosphere.Material.AddConstituent(oxygen, o2);
                }
            }
            else
            {
                Atmosphere.Material.AddConstituent(substance, gasProportion);
            }

            adjustedAtmosphericPressure += adjustedAtmosphericPressure * (double)(gasProportion - previousGasProportion);
        }

        private void FractionHydrophere(double temperature)
        {
            var seawater = Substances.All.Seawater.GetHomogeneousReference();
            var water = Substances.All.Water.GetChemicalReference();

            var seawaterProportion = Hydrosphere.GetProportion(seawater);
            var waterProportion = 1 - seawaterProportion;

            var depth = SeaLevel + (MaxElevation / 2);
            if (depth > 0)
            {
                var stateTop = Substances.All.Seawater.MeltingPoint <= temperature
                    ? PhaseType.Liquid
                    : PhaseType.Solid;

                var tempBottom = depth > 1000
                    ? 277
                    : depth < 200
                        ? temperature
                        : temperature.Lerp(277, (depth - 200) / 800);
                var stateBottom = Substances.All.Seawater.MeltingPoint <= tempBottom
                    ? PhaseType.Liquid
                    : PhaseType.Solid;

                // subsurface ocean indicated
                if (stateTop != stateBottom)
                {
                    var topProportion = 1000 / depth;
                    var bottomProportion = 1 - topProportion;
                    var bottomOuterRadius = Hydrosphere.Shape.ContainingRadius * bottomProportion;
                    _hydrosphere = new LayeredComposite(
                        new (IMaterial, decimal)[]
                        {
                        (new Material(
                            Hydrosphere.Density,
                            Hydrosphere.Mass * bottomProportion,
                            new HollowSphere(Material.Shape.ContainingRadius, bottomOuterRadius, Material.Shape.Position),
                            277,
                            (seawater, seawaterProportion),
                            (water, waterProportion)),
                            (decimal)bottomProportion),
                        (new Material(
                            Hydrosphere.Density,
                            Hydrosphere.Mass * topProportion,
                            new HollowSphere(bottomOuterRadius, Hydrosphere.Shape.ContainingRadius, Material.Shape.Position),
                            (277 + temperature) / 2,
                            (seawater, seawaterProportion),
                            (water, waterProportion)),
                            (decimal)topProportion),
                        },
                        Hydrosphere.Shape,
                        Hydrosphere.Density,
                        Hydrosphere.Mass);
                    return;
                }
            }

            var avgDepth = (double)(Hydrosphere.Shape.ContainingRadius - Material.Shape.ContainingRadius) / 2;
            var avgTemp = avgDepth > 1000
                ? 277
                : avgDepth < 200
                    ? temperature
                    : temperature.Lerp(277, (avgDepth - 200) / 800);
            _hydrosphere = new Material(
                Hydrosphere.Density,
                Hydrosphere.Mass,
                Hydrosphere.Shape,
                avgTemp,
                (seawater, seawaterProportion),
                (water, waterProportion));
        }

        private protected override async Task GenerateAlbedoAsync()
        {
            await SetAlbedoAsync(Randomizer.Instance.NextDouble(0.1, 0.6)).ConfigureAwait(false);
            _surfaceAlbedo = _albedo;
        }

        private protected override Task GenerateAngleOfRotationAsync() => GenerateAngleOfRotationAsync(null);

        private async Task GenerateAngleOfRotationAsync(TerrestrialPlanetParams? planetParams)
        {
            if (planetParams?.AxialTilt.HasValue != true)
            {
                await base.GenerateAngleOfRotationAsync().ConfigureAwait(false);
            }
            else
            {
                _axialPrecession = Randomizer.Instance.NextDouble(MathAndScience.Constants.Doubles.MathConstants.TwoPI);
                var axialTilt = planetParams!.Value.AxialTilt!.Value;
                if (Orbit.HasValue)
                {
                    axialTilt += Orbit.Value.Inclination;
                }
                await SetAngleOfRotationAsync(axialTilt).ConfigureAwait(false);
            }
        }

        private protected override Task GenerateAtmosphereAsync() => GenerateAtmosphereAsync(null, null);

        private async Task GenerateAtmosphereAsync(TerrestrialPlanetParams? planetParams, HabitabilityRequirements? habitabilityRequirements)
        {
            if (_atmosphere != null)
            {
                return;
            }

            if (await GetAverageBlackbodyTemperatureAsync().ConfigureAwait(false) >= GetTempForThinAtmosphere())
            {
                await GenerateAtmosphereTraceAsync().ConfigureAwait(false);
            }
            else
            {
                await GenerateAtmosphereThickAsync(planetParams, habitabilityRequirements).ConfigureAwait(false);
            }

            var adjustedAtmosphericPressure = Atmosphere.AtmosphericPressure;

            var water = Substances.All.Water.GetChemicalReference();
            var seawater = Substances.All.Seawater.GetHomogeneousReference();
            // Water may be removed, or if not may remove CO2 from the atmosphere, depending on
            // planetary conditions.
            if (Hydrosphere.Contains(water)
                || Hydrosphere.Contains(seawater)
                || Atmosphere.Material.Contains(water))
            {
                // First calculate water phases at effective temp, to establish a baseline
                // for the presence of water and its effect on CO2.
                // If a desired temp has been established, use that instead.
                double surfaceTemp;
                if (planetParams?.SurfaceTemperature.HasValue == true)
                {
                    surfaceTemp = planetParams!.Value.SurfaceTemperature!.Value;
                }
                else if (habitabilityRequirements?.MinimumTemperature.HasValue == true)
                {
                    surfaceTemp = habitabilityRequirements!.Value.MaximumTemperature.HasValue
                        ? (habitabilityRequirements!.Value.MinimumTemperature!.Value
                            + habitabilityRequirements!.Value.MaximumTemperature!.Value)
                            / 2
                        : habitabilityRequirements!.Value.MinimumTemperature!.Value;
                }
                else
                {
                    surfaceTemp = await GetAverageBlackbodyTemperatureAsync().ConfigureAwait(false);
                }
                adjustedAtmosphericPressure = await CalculateGasPhaseMixAsync(
                    planetParams,
                    water,
                    surfaceTemp,
                    adjustedAtmosphericPressure).ConfigureAwait(false);

                // Recalculate temperatures based on the new atmosphere.
                await ResetCachedTemperaturesAsync().ConfigureAwait(false);

                FractionHydrophere(await GetAverageSurfaceTemperatureAsync().ConfigureAwait(false));

                // Recalculate the phases of water based on the new temperature.
                adjustedAtmosphericPressure = await CalculateGasPhaseMixAsync(
                    planetParams,
                    water,
                    await GetAverageSurfaceTemperatureAsync().ConfigureAwait(false),
                    adjustedAtmosphericPressure).ConfigureAwait(false);

                // If life alters the greenhouse potential, temperature and water phase must be
                // recalculated once again.
                if (await GenerateLifeAsync().ConfigureAwait(false))
                {
                    adjustedAtmosphericPressure = await CalculateGasPhaseMixAsync(
                        planetParams,
                        water,
                        await GetAverageSurfaceTemperatureAsync().ConfigureAwait(false),
                        adjustedAtmosphericPressure).ConfigureAwait(false);
                    await ResetCachedTemperaturesAsync().ConfigureAwait(false);
                    FractionHydrophere(await GetAverageSurfaceTemperatureAsync().ConfigureAwait(false));
                }
            }
            else
            {
                // Recalculate temperature based on the new atmosphere.
                await ResetCachedTemperaturesAsync().ConfigureAwait(false);
            }

            var modified = false;
            foreach (var requirement in Atmosphere.ConvertRequirementsForPressure(habitabilityRequirements?.AtmosphericRequirements)
                .Concat(Atmosphere.ConvertRequirementsForPressure(planetParams?.AtmosphericRequirements)))
            {
                var proportion = Atmosphere.Material.GetProportion(requirement.Substance);
                if (proportion < requirement.MinimumProportion
                    || (requirement.MaximumProportion.HasValue && proportion > requirement.MaximumProportion.Value))
                {
                    Atmosphere.Material.AddConstituent(
                        requirement.Substance,
                        requirement.MaximumProportion.HasValue
                            ? (requirement.MinimumProportion + requirement.MaximumProportion.Value) / 2
                            : requirement.MinimumProportion);
                    if (requirement.Substance.Equals(water))
                    {
                        Atmosphere.ResetWater(this);
                    }
                    modified = true;
                }
            }
            if (modified)
            {
                await Atmosphere.ResetGreenhouseFactorAsync(this).ConfigureAwait(false);
            }

            adjustedAtmosphericPressure = await CalculatePhasesAsync(planetParams, 0, adjustedAtmosphericPressure).ConfigureAwait(false);
            FractionHydrophere(await GetAverageSurfaceTemperatureAsync().ConfigureAwait(false));

            if (planetParams?.AtmosphericPressure.HasValue != true && habitabilityRequirements is null)
            {
                await SetAtmosphericPressureAsync(Math.Max(0, adjustedAtmosphericPressure)).ConfigureAwait(false);
                await Atmosphere.ResetPressureDependentPropertiesAsync(this).ConfigureAwait(false);
            }

            // If the adjustments have led to the loss of liquid water, then there is no life after
            // all (this may be interpreted as a world which once supported life, but became
            // inhospitable due to the environmental changes that life produced).
            if (!await IsHabitableAsync().ConfigureAwait(false))
            {
                HasBiosphere = false;
            }
        }

        private async Task GenerateAtmosphereThickAsync(TerrestrialPlanetParams? planetParams, HabitabilityRequirements? habitabilityRequirements)
        {
            double pressure;
            if (planetParams?.AtmosphericPressure.HasValue == true)
            {
                pressure = Math.Max(0, planetParams!.Value.AtmosphericPressure!.Value);
            }
            else if (habitabilityRequirements?.MinimumPressure.HasValue == true
                || habitabilityRequirements?.MaximumPressure.HasValue == true)
            {
                // If there is a minimum but no maximum, a half-Gaussian distribution with the minimum as both mean and the basis for the sigma is used.
                if (!habitabilityRequirements!.Value.MaximumPressure!.HasValue)
                {
                    pressure = habitabilityRequirements!.Value.MinimumPressure!.Value
                        + Math.Abs(Randomizer.Instance.NormalDistributionSample(0, habitabilityRequirements.Value.MinimumPressure.Value / 3));
                }
                else
                {
                    pressure = Randomizer.Instance.NextDouble(habitabilityRequirements.Value.MinimumPressure ?? 0, habitabilityRequirements.Value.MaximumPressure.Value);
                }
            }
            else
            {
                Number mass;
                // Low-gravity planets without magnetospheres are less likely to hold onto the bulk
                // of their atmospheres over long periods.
                if (Mass >= 1.5e24 || HasMagnetosphere)
                {
                    mass = Mass / Randomizer.Instance.NormalDistributionSample(1158568, 38600, minimum: 579300, maximum: 1737900);
                }
                else
                {
                    mass = Mass / Randomizer.Instance.NormalDistributionSample(7723785, 258000, minimum: 3862000, maximum: 11586000);
                }

                pressure = (double)(mass * SurfaceGravity / (1000 * MathConstants.FourPI * RadiusSquared));
            }

            // For terrestrial (non-giant) planets, these gases remain at low concentrations due to
            // atmospheric escape.
            var h = Randomizer.Instance.NextDecimal(5e-8m, 2e-7m);
            var he = Randomizer.Instance.NextDecimal(2.6e-7m, 1e-5m);

            // 50% chance not to have these components at all.
            var ch4 = Math.Max(0, Randomizer.Instance.NextDecimal(-0.5m, 0.5m));
            var traceTotal = ch4;

            var co = Math.Max(0, Randomizer.Instance.NextDecimal(-0.5m, 0.5m));
            traceTotal += co;

            var so2 = Math.Max(0, Randomizer.Instance.NextDecimal(-0.5m, 0.5m));
            traceTotal += so2;

            var trace = traceTotal == 0 ? 0 : Randomizer.Instance.NextDecimal(1.5e-4m, 2.5e-3m);
            var traceRatio = traceTotal == 0 ? 0 : trace / traceTotal;
            ch4 *= traceRatio;
            co *= traceRatio;
            so2 *= traceRatio;

            // CO2 makes up the bulk of a thick atmosphere by default (although the presence of water
            // may change this later).
            var co2 = Randomizer.Instance.NextDecimal(0.97m, 0.99m) - trace;

            // If there is water on the surface, the water in the air will be determined based on
            // vapor pressure later, and should not be randomly assigned. Otherwise, there is a small
            // chance of water vapor without significant surface water (results of cometary deposits, etc.)
            var waterVapor = 0.0m;
            var water = Substances.All.Water.GetChemicalReference();
            var seawater = Substances.All.Seawater.GetHomogeneousReference();
            var surfaceWater = Hydrosphere.Contains(water) || Hydrosphere.Contains(seawater);
            if (CanHaveWater && !surfaceWater)
            {
                waterVapor = Math.Max(0, Randomizer.Instance.NextDecimal(-0.05m, 0.001m));
            }

            // Always at least some oxygen if there is water, planetary composition allowing
            var o2 = 0.0m;
            if (CanHaveOxygen)
            {
                if (waterVapor != 0)
                {
                    o2 = waterVapor * 0.0001m;
                }
                else if (surfaceWater)
                {
                    o2 = Randomizer.Instance.NextDecimal(0.002m);
                }
            }

            // N2 (largely inert gas) comprises whatever is left after the other components have been
            // determined. This is usually a trace amount, unless CO2 has been reduced to a trace, in
            // which case it will comprise the bulk of the atmosphere.
            var n2 = 1 - (h + he + co2 + waterVapor + o2 + trace);

            // Some portion of the N2 may be Ar instead.
            var ar = Math.Max(0, n2 * Randomizer.Instance.NextDecimal(-0.02m, 0.04m));
            n2 -= ar;
            // An even smaller fraction may be Kr.
            var kr = Math.Max(0, n2 * Randomizer.Instance.NextDecimal(-2.5e-4m, 5.0e-4m));
            n2 -= kr;
            // An even smaller fraction may be Xe or Ne.
            var xe = Math.Max(0, n2 * Randomizer.Instance.NextDecimal(-1.8e-5m, 3.5e-5m));
            n2 -= xe;
            var ne = Math.Max(0, n2 * Randomizer.Instance.NextDecimal(-1.8e-5m, 3.5e-5m));
            n2 -= ne;

            var components = new List<(ISubstanceReference, decimal)>()
            {
                (Substances.All.CarbonDioxide.GetChemicalReference(), co2),
                (Substances.All.Helium.GetChemicalReference(), he),
                (Substances.All.Hydrogen.GetChemicalReference(), h),
                (Substances.All.Nitrogen.GetChemicalReference(), n2),
            };
            if (ar > 0)
            {
                components.Add((Substances.All.Argon.GetChemicalReference(), ar));
            }
            if (co > 0)
            {
                components.Add((Substances.All.CarbonMonoxide.GetChemicalReference(), co));
            }
            if (kr > 0)
            {
                components.Add((Substances.All.Krypton.GetChemicalReference(), kr));
            }
            if (ch4 > 0)
            {
                components.Add((Substances.All.Methane.GetChemicalReference(), ch4));
            }
            if (o2 > 0)
            {
                components.Add((Substances.All.Oxygen.GetChemicalReference(), o2));
            }
            if (so2 > 0)
            {
                components.Add((Substances.All.SulphurDioxide.GetChemicalReference(), so2));
            }
            if (waterVapor > 0)
            {
                components.Add((Substances.All.Water.GetChemicalReference(), waterVapor));
            }
            if (xe > 0)
            {
                components.Add((Substances.All.Xenon.GetChemicalReference(), xe));
            }
            _atmosphere = await Atmosphere.GetNewInstanceAsync(this, pressure, components.ToArray()).ConfigureAwait(false);
        }

        private async Task GenerateAtmosphereTraceAsync()
        {
            // For terrestrial (non-giant) planets, these gases remain at low concentrations due to
            // atmospheric escape.
            var h = Randomizer.Instance.NextDecimal(5e-8m, 2e-7m);
            var he = Randomizer.Instance.NextDecimal(2.6e-7m, 1e-5m);

            // 50% chance not to have these components at all.
            var ch4 = Math.Max(0, Randomizer.Instance.NextDecimal(-0.5m, 0.5m));
            var total = ch4;

            var co = Math.Max(0, Randomizer.Instance.NextDecimal(-0.5m, 0.5m));
            total += co;

            var so2 = Math.Max(0, Randomizer.Instance.NextDecimal(-0.5m, 0.5m));
            total += so2;

            var n2 = Math.Max(0, Randomizer.Instance.NextDecimal(-0.5m, 0.5m));
            total += n2;

            // Noble traces: selected as fractions of N2, if present, to avoid over-representation.
            var ar = n2 > 0 ? Math.Max(0, n2 * Randomizer.Instance.NextDecimal(-0.02m, 0.04m)) : 0;
            n2 -= ar;
            var kr = n2 > 0 ? Math.Max(0, n2 * Randomizer.Instance.NextDecimal(-0.02m, 0.04m)) : 0;
            n2 -= kr;
            var xe = n2 > 0 ? Math.Max(0, n2 * Randomizer.Instance.NextDecimal(-0.02m, 0.04m)) : 0;
            n2 -= xe;

            // Carbon monoxide means at least some carbon dioxide, as well.
            var co2 = co > 0
                ? Randomizer.Instance.NextDecimal(0.5m)
                : Math.Max(0, Randomizer.Instance.NextDecimal(-0.5m, 0.5m));
            total += co2;

            // If there is water on the surface, the water in the air will be determined based on
            // vapor pressure later, and should not be randomly assigned. Otherwise, there is a small
            // chance of water vapor without significant surface water (results of cometary deposits, etc.)
            var waterVapor = 0.0m;
            var water = Substances.All.Water.GetChemicalReference();
            var seawater = Substances.All.Seawater.GetHomogeneousReference();
            if (CanHaveWater
                && !Hydrosphere.Contains(water)
                && !Hydrosphere.Contains(seawater))
            {
                waterVapor = Math.Max(0, Randomizer.Instance.NextDecimal(-0.05m, 0.001m));
            }
            total += waterVapor;

            var o2 = 0.0m;
            if (CanHaveOxygen)
            {
                // Always at least some oxygen if there is water, planetary composition allowing
                o2 = waterVapor > 0
                    ? waterVapor * 1e-4m
                    : Math.Max(0, Randomizer.Instance.NextDecimal(-0.5m, 0.5m));
            }
            total += o2;

            var ratio = total == 0 ? 0 : (1 - h - he) / total;
            ch4 *= ratio;
            co *= ratio;
            so2 *= ratio;
            n2 *= ratio;
            ar *= ratio;
            kr *= ratio;
            xe *= ratio;
            co2 *= ratio;
            waterVapor *= ratio;
            o2 *= ratio;

            // H and He are always assumed to be present in small amounts if a planet has any
            // atmosphere, but without any other gases making up the bulk of the atmosphere, they are
            // presumed lost to atmospheric escape entirely, and no atmosphere at all is indicated.
            if (total == 0)
            {
                _atmosphere = await Atmosphere.GetNewInstanceAsync(this, 0).ConfigureAwait(false);
            }
            else
            {
                var components = new List<(ISubstanceReference, decimal)>()
                {
                    (Substances.All.Helium.GetChemicalReference(), he),
                    (Substances.All.Hydrogen.GetChemicalReference(), h),
                };
                if (ar > 0)
                {
                    components.Add((Substances.All.Argon.GetChemicalReference(), ar));
                }
                if (co2 > 0)
                {
                    components.Add((Substances.All.CarbonDioxide.GetChemicalReference(), co2));
                }
                if (co > 0)
                {
                    components.Add((Substances.All.CarbonMonoxide.GetChemicalReference(), co));
                }
                if (kr > 0)
                {
                    components.Add((Substances.All.Krypton.GetChemicalReference(), kr));
                }
                if (ch4 > 0)
                {
                    components.Add((Substances.All.Methane.GetChemicalReference(), ch4));
                }
                if (n2 > 0)
                {
                    components.Add((Substances.All.Nitrogen.GetChemicalReference(), n2));
                }
                if (o2 > 0)
                {
                    components.Add((Substances.All.Oxygen.GetChemicalReference(), o2));
                }
                if (so2 > 0)
                {
                    components.Add((Substances.All.SulphurDioxide.GetChemicalReference(), so2));
                }
                if (waterVapor > 0)
                {
                    components.Add((Substances.All.Water.GetChemicalReference(), waterVapor));
                }
                if (xe > 0)
                {
                    components.Add((Substances.All.Xenon.GetChemicalReference(), xe));
                }
                _atmosphere = await Atmosphere.GetNewInstanceAsync(this, Randomizer.Instance.NextDouble(25), components.ToArray()).ConfigureAwait(false);
            }
        }

        private void GenerateHydrocarbons()
        {
            // It is presumed that it is statistically likely that the current eon is not the first
            // with life, and therefore that some fossilized hydrocarbon deposits exist.
            var coal = (decimal)Randomizer.Instance.NormalDistributionSample(2e-13, 3.4e-14) * 0.5m;

            AddResource(Substances.All.Anthracite.GetReference(), coal, false);
            AddResource(Substances.All.BituminousCoal.GetReference(), coal, false);

            var petroleum = (decimal)Randomizer.Instance.NormalDistributionSample(1e-8, 1.6e-9);
            var petroleumSeed = AddResource(Substances.All.Petroleum.GetReference(), petroleum, false);

            // Natural gas is predominantly, though not exclusively, found with petroleum deposits.
            AddResource(Substances.All.NaturalGas.GetReference(), petroleum, false, true, petroleumSeed);
        }

        private protected virtual async Task GenerateHydrosphereAsync() => GenerateHydrosphere(null, await GetAverageBlackbodyTemperatureAsync().ConfigureAwait(false));

        private protected void GenerateHydrosphere(double surfaceTemp, decimal ratio)
        {
            var mass = Number.Zero;
            var seawater = Substances.All.Seawater.GetHomogeneousReference();

            if (ratio <= 0)
            {
                SeaLevel = -MaxElevation * 1.1;
            }
            else if (ratio >= 1 && (HasFlatSurface || MaxElevation.IsNearlyZero()))
            {
                SeaLevel = MaxElevation * (double)ratio;
                mass = new HollowSphere(Shape.ContainingRadius, Shape.ContainingRadius + SeaLevel).Volume * (seawater.Homogeneous.DensityLiquid ?? 0);
            }
            else
            {
                var grid = new WorldGrid(this, WorldGrid.DefaultGridSize);
                var seaLevel = 0.0;
                if (ratio == 0.5m)
                {
                    SeaLevel = 0;
                }
                else
                {
                    // Midway between the elevations of the first two tiles beyond the amount indicated by
                    // the ratio when ordered by elevation.
                    seaLevel = grid.Tiles
                        .OrderBy(t => t.Elevation)
                        .Skip((int)Math.Round(grid.Tiles.Length * ratio))
                        .Take(2)
                        .Average(t => t.Elevation);
                    SeaLevel = seaLevel * MaxElevation;
                }
                var fiveSidedArea = WorldGrid.GridAreas[WorldGrid.DefaultGridSize].fiveSided;
                var sixSidedArea = WorldGrid.GridAreas[WorldGrid.DefaultGridSize].sixSided;
                mass = grid.Tiles
                    .Where(t => t.Elevation - seaLevel < 0)
                    .Sum(x => (x.EdgeCount == 5 ? fiveSidedArea : sixSidedArea) * GetNormalizedElevationAt(x.Vector))
                    * -MaxElevation
                    * RadiusSquared
                    * (seawater.Homogeneous.DensityLiquid ?? 0);
            }

            if (!mass.IsPositive)
            {
                _hydrosphere = MathAndScience.Chemistry.Material.Empty;
                return;
            }

            // Surface water is mostly salt water.
            var seawaterProportion = (decimal)Randomizer.Instance.NormalDistributionSample(0.945, 0.015);
            var waterProportion = 1 - seawaterProportion;
            var water = Substances.All.Water.GetChemicalReference();
            var density = ((seawater.Homogeneous.DensityLiquid ?? 0) * (double)seawaterProportion) + ((water.Homogeneous.DensityLiquid ?? 0) * (double)waterProportion);

            var outerRadius = (3 * ((mass / density) + new Sphere(Material.Shape.ContainingRadius).Volume) / MathConstants.FourPI).CubeRoot();
            var shape = new HollowSphere(
                Material.Shape.ContainingRadius,
                outerRadius,
                Material.Shape.Position);
            var avgDepth = (double)(outerRadius - Material.Shape.ContainingRadius) / 2;
            var avgTemp = avgDepth > 1000
                ? 277
                : avgDepth < 200
                    ? surfaceTemp
                    : surfaceTemp.Lerp(277, (avgDepth - 200) / 800);

            _hydrosphere = new Material(
                density,
                mass,
                shape,
                avgTemp,
                (seawater, seawaterProportion),
                (water, waterProportion));

            FractionHydrophere(surfaceTemp);

            HydrolyzeCrust();
        }

        private protected virtual void GenerateHydrosphere(TerrestrialPlanetParams? planetParams, double surfaceTemp)
        {
            // Most terrestrial planets will (at least initially) have a hydrosphere layer (oceans,
            // icecaps, etc.). This might be removed later, depending on the planet's conditions.

            if (!CanHaveWater)
            {
                SeaLevel = -MaxElevation * 1.1;
                return;
            }

            GenerateHydrosphere(surfaceTemp, planetParams?.WaterRatio ?? Randomizer.Instance.NextDecimal());
        }

        /// <summary>
        /// Determines whether this planet is capable of sustaining life, and whether or not it
        /// actually does. If so, the atmosphere may be adjusted.
        /// </summary>
        /// <returns>
        /// True if the atmosphere's greenhouse potential is adjusted; false if not.
        /// </returns>
        private async Task<bool> GenerateLifeAsync()
        {
            if (!await GetIsHospitableAsync().ConfigureAwait(false) || !await IsHabitableAsync().ConfigureAwait(false))
            {
                HasBiosphere = false;
                return false;
            }

            // If the planet already has a biosphere, there is nothing left to do.
            if (HasBiosphere)
            {
                return false;
            }

            HasBiosphere = true;

            GenerateHydrocarbons();

            // If the habitable zone is a subsurface ocean, no further adjustments occur.
            if (Hydrosphere is LayeredComposite)
            {
                return false;
            }

            // If there is a habitable surface layer, it is presumed that an initial population of a
            // cyanobacteria analogue will produce a significant amount of free oxygen, which in turn
            // will transform most CH4 to CO2 and H2O, and also produce an ozone layer.
            var o2 = Randomizer.Instance.NextDecimal(0.2m, 0.25m);
            var oxygen = Substances.All.Oxygen.GetChemicalReference();
            Atmosphere.Material.AddConstituent(oxygen, o2);

            // Calculate ozone based on level of free oxygen.
            var o3 = o2 * 4.5e-5m;
            var ozone = Substances.All.Ozone.GetChemicalReference();
            if (!(Atmosphere.Material is LayeredComposite lc) || lc.Layers.Count < 3)
            {
                Atmosphere.DifferentiateTroposphere(); // First ensure troposphere is differentiated.
                (Atmosphere.Material as LayeredComposite)?.CopyLayer(1, 0.01m);
            }
            (Atmosphere.Material as LayeredComposite)?.Layers[2].material.AddConstituent(ozone, o3);

            // Convert most methane to CO2 and H2O.
            var methane = Substances.All.Methane.GetChemicalReference();
            var ch4 = Atmosphere.Material.GetProportion(methane);
            if (ch4 != 0)
            {
                // The levels of CO2 and H2O are not adjusted; it is presumed that the levels already
                // determined for them take the amounts derived from CH4 into account. If either gas
                // is entirely missing, however, it is added.
                var carbonDioxide = Substances.All.CarbonDioxide.GetChemicalReference();
                if (Atmosphere.Material.GetProportion(carbonDioxide) <= 0)
                {
                    Atmosphere.Material.AddConstituent(carbonDioxide, ch4 / 3);
                }

                if (Atmosphere.Material.GetProportion(Substances.All.Water) <= 0)
                {
                    Atmosphere.Material.AddConstituent(Substances.All.Water, ch4 * 2 / 3);
                    Atmosphere.ResetWater(this);
                }

                Atmosphere.Material.AddConstituent(methane, ch4 * 0.001m);

                await Atmosphere.ResetGreenhouseFactorAsync(this).ConfigureAwait(false);
                return true;
            }

            return false;
        }

        private async Task GenerateOrbitAsync(CelestialLocation orbitedObject, TerrestrialPlanetParams? planetParams, HabitabilityRequirements? habitabilityRequirements)
        {
            if (planetParams?.RotationalPeriod.HasValue == true)
            {
                await SetRotationalPeriodAsync(Number.Max(0, planetParams!.Value.RotationalPeriod!.Value)).ConfigureAwait(false);
            }
            if (orbitedObject is null)
            {
                await GenerateAngleOfRotationAsync(planetParams).ConfigureAwait(false);
                return;
            }

            if (planetParams?.Eccentricity.HasValue == true)
            {
                Eccentricity = planetParams!.Value.Eccentricity!.Value;
            }

            var ta = Randomizer.Instance.NextDouble(MathAndScience.Constants.Doubles.MathConstants.TwoPI);
            Number? semiMajorAxis;
            if (planetParams?.RevolutionPeriod.HasValue == true)
            {
                semiMajorAxis = WorldFoundry.Space.Orbit.GetSemiMajorAxisForPeriod(this, orbitedObject, planetParams!.Value.RevolutionPeriod!.Value);
                await GenerateOrbitAsync(orbitedObject, semiMajorAxis.Value, ta).ConfigureAwait(false);
            }
            else
            {
                await WorldFoundry.Space.Orbit.SetOrbitAsync(
                    this,
                    orbitedObject,
                    await GetDistanceToAsync(orbitedObject).ConfigureAwait(false),
                    Eccentricity,
                    Randomizer.Instance.NextDouble(0.9),
                    Randomizer.Instance.NextDouble(MathAndScience.Constants.Doubles.MathConstants.TwoPI),
                    Randomizer.Instance.NextDouble(MathAndScience.Constants.Doubles.MathConstants.TwoPI),
                    Randomizer.Instance.NextDouble(MathAndScience.Constants.Doubles.MathConstants.TwoPI))
                    .ConfigureAwait(false);
                semiMajorAxis = Orbit?.SemiMajorAxis;
            }
            await GenerateAngleOfRotationAsync(planetParams).ConfigureAwait(false);

            if (orbitedObject is Star star
                && (planetParams?.SurfaceTemperature.HasValue == true
                || habitabilityRequirements?.MinimumTemperature.HasValue == true
                || habitabilityRequirements?.MaximumTemperature.HasValue == true))
            {
                double targetTemp;
                if (planetParams?.SurfaceTemperature.HasValue == true)
                {
                    targetTemp = planetParams!.Value.SurfaceTemperature!.Value;
                }
                else if (habitabilityRequirements?.MinimumTemperature.HasValue == true)
                {
                    targetTemp = habitabilityRequirements?.MaximumTemperature.HasValue == true
                        ? (habitabilityRequirements!.Value.MinimumTemperature!.Value
                            + habitabilityRequirements!.Value.MaximumTemperature!.Value)
                            / 2
                        : habitabilityRequirements!.Value.MinimumTemperature!.Value;
                }
                else
                {
                    targetTemp = GetTempForThinAtmosphere() / 2;
                }

                // Convert the target average surface temperature to an estimated target equatorial
                // surface temperature, for which orbit/luminosity requirements can be calculated.
                var targetEquatorialTemp = targetTemp * 1.062;
                // Use the typical average elevation to determine average surface
                // temperature, since the average temperature at sea level is not the same
                // as the average overall surface temperature.
                var avgElevation = MaxElevation * 0.04;
                var totalTargetEffectiveTemp = targetEquatorialTemp + (avgElevation * LapseRateDry);

                var greenhouseEffect = 30.0; // naive initial guess, corrected if possible with param values
                if (planetParams?.AtmosphericPressure.HasValue == true
                    && (planetParams?.WaterVaporRatio.HasValue == true
                    || planetParams?.WaterRatio.HasValue == true))
                {
                    var pressure = planetParams!.Value.AtmosphericPressure!.Value;

                    double vaporRatio;
                    if (planetParams?.WaterVaporRatio.HasValue == true)
                    {
                        vaporRatio = (double)planetParams!.Value.WaterVaporRatio!.Value;
                    }
                    else
                    {
                        vaporRatio = (Substances.All.Water.GetVaporPressure(totalTargetEffectiveTemp) ?? 0) / pressure * 0.25;
                    }

                    greenhouseEffect = await GetGreenhouseEffectAsync(
                        GetInsolationFactor(Atmosphere.GetAtmosphericMass(this, pressure), 0), // scale height will be ignored since this isn't a polar calculation
                        Atmosphere.GetGreenhouseFactor(Substances.All.Water.GreenhousePotential * vaporRatio, pressure)).ConfigureAwait(false);
                }
                var targetEffectiveTemp = totalTargetEffectiveTemp - greenhouseEffect;

                var currentTargetTemp = targetEffectiveTemp;

                // Due to atmospheric effects, the target is likely to be missed considerably on the
                // first attempt, since the calculations for orbit/luminosity will not be able to
                // account for greenhouse warming. By determining the degree of over/undershoot, the
                // target can be adjusted. This is repeated until the real target is approached to
                // within an acceptable tolerance, but not to excess.
                var count = 0;
                double prevDelta;
                var delta = 0.0;
                var originalHydrosphere = _hydrosphere?.GetClone();
                do
                {
                    prevDelta = delta;
                    await AdjustOrbitForTemperatureAsync(planetParams, star, semiMajorAxis, ta, currentTargetTemp).ConfigureAwait(false);

                    // Reset hydrosphere to negate effects of runaway evaporation or freezing.
                    _hydrosphere = originalHydrosphere;

                    await GenerateAtmosphereAsync(planetParams, habitabilityRequirements).ConfigureAwait(false);

                    if (planetParams?.SurfaceTemperature.HasValue == true)
                    {
                        delta = targetEquatorialTemp - await GetTemperatureAtElevationAsync(await GetAverageSurfaceTemperatureAsync().ConfigureAwait(false), avgElevation).ConfigureAwait(false);
                    }
                    else if (habitabilityRequirements.HasValue)
                    {
                        var tooCold = false;
                        if (habitabilityRequirements.Value.MinimumTemperature.HasValue)
                        {
                            var coolestEquatorialTemp = await GetMinEquatorTemperatureAsync().ConfigureAwait(false);
                            if (coolestEquatorialTemp < habitabilityRequirements.Value.MinimumTemperature)
                            {
                                delta = habitabilityRequirements.Value.MaximumTemperature.HasValue
                                    ? habitabilityRequirements.Value.MaximumTemperature.Value - coolestEquatorialTemp
                                    : habitabilityRequirements.Value.MinimumTemperature.Value - coolestEquatorialTemp;
                                tooCold = true;
                            }
                        }
                        if (!tooCold && habitabilityRequirements.Value.MaximumTemperature.HasValue)
                        {
                            var warmestPolarTemp = await GetMaxPolarTemperatureAsync().ConfigureAwait(false);
                            if (warmestPolarTemp > habitabilityRequirements.Value.MaximumTemperature)
                            {
                                delta = habitabilityRequirements!.Value.MaximumTemperature.Value - warmestPolarTemp;
                            }
                        }
                    }
                    // Avoid oscillation by reducing deltas which bounce around zero.
                    var deltaAdjustment = prevDelta != 0 && Math.Sign(delta) != Math.Sign(prevDelta)
                        ? delta / 2
                        : 0;
                    // If the corrections are resulting in a runaway drift in the wrong direction,
                    // reset by deleting the atmosphere and targeting the original temp; do not
                    // reset the count, to prevent this from repeating indefinitely.
                    if (prevDelta != 0
                        && (delta >= 0) == (prevDelta >= 0)
                        && Math.Abs(delta) > Math.Abs(prevDelta))
                    {
                        _atmosphere = null;
                        await ResetCachedTemperaturesAsync().ConfigureAwait(false);
                        currentTargetTemp = targetEffectiveTemp;
                    }
                    else
                    {
                        currentTargetTemp = Math.Max(0, currentTargetTemp + (delta - deltaAdjustment));
                    }
                    count++;
                } while (count < 10 && Math.Abs(delta) > 0.5);
            }
        }

        private async Task GenerateOrbitAsync(CelestialLocation orbitedObject, Number semiMajorAxis, double trueAnomaly)
        {
            await WorldFoundry.Space.Orbit.SetOrbitAsync(
                  this,
                  orbitedObject,
                  (1 - Eccentricity) * semiMajorAxis,
                  Eccentricity,
                  Randomizer.Instance.NextDouble(0.9),
                  Randomizer.Instance.NextDouble(MathAndScience.Constants.Doubles.MathConstants.TwoPI),
                  Randomizer.Instance.NextDouble(MathAndScience.Constants.Doubles.MathConstants.TwoPI),
                  trueAnomaly).ConfigureAwait(false);
            await ResetCachedTemperaturesAsync().ConfigureAwait(false);
        }

        private protected override void GenerateResources()
        {
            base.GenerateResources();

            var beryl = (decimal)Randomizer.Instance.NormalDistributionSample(4e-6, 6.7e-7, minimum: 0);
            var emerald = beryl * 1.58e-4m;
            var corundum = (decimal)Randomizer.Instance.NormalDistributionSample(2.6e-4, 4e-5, minimum: 0);
            var ruby = corundum * 1.58e-4m;
            var sapphire = corundum * 5.7e-3m;

            var diamond = (decimal)Randomizer.Instance.NormalDistributionSample(1.5e-7, 2.5e-8, minimum: 0);

            if (beryl > 0)
            {
                AddResource(Substances.All.Beryl.GetChemicalReference(), beryl, true);
            }
            if (emerald > 0)
            {
                AddResource(Substances.All.Emerald.GetHomogeneousReference(), emerald, true);
            }
            if (corundum > 0)
            {
                AddResource(Substances.All.Corundum.GetChemicalReference(), corundum, true);
            }
            if (ruby > 0)
            {
                AddResource(Substances.All.Ruby.GetHomogeneousReference(), ruby, true);
            }
            if (sapphire > 0)
            {
                AddResource(Substances.All.Sapphire.GetHomogeneousReference(), sapphire, true);
            }
            if (diamond > 0)
            {
                AddResource(Substances.All.Diamond.GetChemicalReference(), diamond, true);
            }
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
                else if (chance <= 0.77) // Most will be standard terrestrial.
                {
                    return await GetNewInstanceAsync<TerrestrialPlanet>(ParentId, Vector3.Zero, maxMass, orbit).ConfigureAwait(false);
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

            // Rocky crust with trace minerals

            var rock = 1m;

            var aluminium = (decimal)Randomizer.Instance.NormalDistributionSample(0.026, 4e-3, minimum: 0);
            var iron = (decimal)Randomizer.Instance.NormalDistributionSample(1.67e-2, 2.75e-3, minimum: 0);
            var titanium = (decimal)Randomizer.Instance.NormalDistributionSample(5.7e-3, 9e-4, minimum: 0);

            var chalcopyrite = (decimal)Randomizer.Instance.NormalDistributionSample(1.1e-3, 1.8e-4, minimum: 0); // copper
            rock -= chalcopyrite;
            var chromite = (decimal)Randomizer.Instance.NormalDistributionSample(5.5e-4, 9e-5, minimum: 0);
            rock -= chromite;
            var sphalerite = (decimal)Randomizer.Instance.NormalDistributionSample(8.1e-5, 1.3e-5, minimum: 0); // zinc
            rock -= sphalerite;
            var galena = (decimal)Randomizer.Instance.NormalDistributionSample(2e-5, 3.3e-6, minimum: 0); // lead
            rock -= galena;
            var uraninite = (decimal)Randomizer.Instance.NormalDistributionSample(7.15e-6, 1.1e-6, minimum: 0);
            rock -= uraninite;
            var cassiterite = (decimal)Randomizer.Instance.NormalDistributionSample(6.7e-6, 1.1e-6, minimum: 0); // tin
            rock -= cassiterite;
            var cinnabar = (decimal)Randomizer.Instance.NormalDistributionSample(1.35e-7, 2.3e-8, minimum: 0); // mercury
            rock -= cinnabar;
            var acanthite = (decimal)Randomizer.Instance.NormalDistributionSample(5e-8, 8.3e-9, minimum: 0); // silver
            rock -= acanthite;
            var sperrylite = (decimal)Randomizer.Instance.NormalDistributionSample(1.17e-8, 2e-9, minimum: 0); // platinum
            rock -= sperrylite;
            var gold = (decimal)Randomizer.Instance.NormalDistributionSample(2.75e-9, 4.6e-10, minimum: 0);
            rock -= gold;

            var bauxite = aluminium * 1.57m;
            rock -= bauxite;

            var hematiteIron = iron * 3 / 4 * (decimal)Randomizer.Instance.NormalDistributionSample(1, 0.167, minimum: 0);
            var hematite = hematiteIron * 2.88m;
            rock -= hematite;
            var magnetite = (iron - hematiteIron) * 4.14m;
            rock -= magnetite;

            var ilmenite = titanium * 2.33m;
            rock -= ilmenite;

            var components = new List<(ISubstanceReference, decimal)>();
            foreach (var (material, proportion) in CelestialSubstances.DryPlanetaryCrustConstituents)
            {
                components.Add((material, proportion * rock));
            }

            if (chalcopyrite > 0)
            {
                components.Add((Substances.All.Chalcopyrite.GetChemicalReference(), chalcopyrite));
            }
            if (chromite > 0)
            {
                components.Add((Substances.All.Chromite.GetChemicalReference(), chromite));
            }
            if (sphalerite > 0)
            {
                components.Add((Substances.All.Sphalerite.GetHomogeneousReference(), sphalerite));
            }
            if (galena > 0)
            {
                components.Add((Substances.All.Galena.GetChemicalReference(), galena));
            }
            if (uraninite > 0)
            {
                components.Add((Substances.All.Uraninite.GetHomogeneousReference(), uraninite));
            }
            if (cassiterite > 0)
            {
                components.Add((Substances.All.Cassiterite.GetChemicalReference(), cassiterite));
            }
            if (cinnabar > 0)
            {
                components.Add((Substances.All.Cinnabar.GetChemicalReference(), cinnabar));
            }
            if (acanthite > 0)
            {
                components.Add((Substances.All.Acanthite.GetChemicalReference(), acanthite));
            }
            if (sperrylite > 0)
            {
                components.Add((Substances.All.Sperrylite.GetChemicalReference(), sperrylite));
            }
            if (gold > 0)
            {
                components.Add((Substances.All.Gold.GetChemicalReference(), gold));
            }
            if (bauxite > 0)
            {
                components.Add((Substances.All.Bauxite.GetReference(), bauxite));
            }
            if (hematite > 0)
            {
                components.Add((Substances.All.Hematite.GetChemicalReference(), hematite));
            }
            if (magnetite > 0)
            {
                components.Add((Substances.All.Magnetite.GetChemicalReference(), magnetite));
            }
            if (ilmenite > 0)
            {
                components.Add((Substances.All.Ilmenite.GetChemicalReference(), ilmenite));
            }

            yield return (new Material(
                (double)(crustMass / shape.Volume),
                crustMass,
                shape,
                null,
                components.ToArray()), 1);
        }

        private protected override double GetDensity() => Randomizer.Instance.NextDouble(MinDensity, MaxDensity);

        /// <summary>
        /// Calculates the distance (in meters) this <see cref="TerrestrialPlanet"/> would have to be
        /// from a <see cref="Star"/> in order to have the given effective temperature.
        /// </summary>
        /// <remarks>
        /// It is assumed that this <see cref="TerrestrialPlanet"/> has no internal temperature of
        /// its own. The effects of other nearby stars are ignored.
        /// </remarks>
        /// <param name="star">The <see cref="Star"/> for which the calculation is to be made.</param>
        /// <param name="temperature">The desired temperature, in K.</param>
        private async Task<Number> GetDistanceForTemperatureAsync(Star star, double temperature)
        {
            var areaRatio = 1;
            if (RotationalPeriod > 2500)
            {
                if (RotationalPeriod <= 75000)
                {
                    areaRatio = 4;
                }
                else if (RotationalPeriod <= 150000)
                {
                    areaRatio = 3;
                }
                else if (RotationalPeriod <= 300000)
                {
                    areaRatio = 2;
                }
            }

            var albedo = await GetAlbedoAsync().ConfigureAwait(false);
            return Math.Sqrt(star.Luminosity * (1 - albedo)) / (Math.Pow(temperature, 4) * MathAndScience.Constants.Doubles.MathConstants.FourPI * MathAndScience.Constants.Doubles.ScienceConstants.sigma * areaRatio);
        }

        private decimal GetHydrosphereAtmosphereRatio() => Math.Min(1, (decimal)(Hydrosphere.Mass / Atmosphere.Material.Mass));

        private protected override ISubstanceReference GetMantleSubstance()
            => Substances.All.Peridotite.GetReference();

        private async Task<Number> GetMassAsync(double? gravity, IShape? shape = null)
        {
            var minMass = MinMass;
            var maxMass = MaxMass.IsZero ? (Number?)null : MaxMass;

            var parent = await GetParentAsync().ConfigureAwait(false);
            if (parent is StarSystem && Position != Vector3.Zero
                && (!Orbit.HasValue || await Orbit.Value.GetOrbitedObjectAsync().ConfigureAwait(false) is Star))
            {
                // Stern-Levison parameter for neighborhood-clearing used to determined minimum mass at which
                // the planet would be able to do so at this orbital distance. We set the maximum at two
                // orders of magnitude more than this (planets in our solar system all have masses above
                // 5 orders of magnitude more). Note that since lambda is proportional to the square of mass,
                // it is multiplied by 10 to obtain a difference of 2 orders of magnitude, rather than by 100.
                minMass = Number.Max(minMass, await GetSternLevisonLambdaMassAsync().ConfigureAwait(false) * 10);
                if (minMass > maxMass && maxMass.HasValue)
                {
                    minMass = maxMass.Value; // sanity check; may result in a "planet" which *can't* clear its neighborhood
                }
            }

            if (gravity.HasValue)
            {
                var mass = GetMassForSurfaceGravity(shape, gravity.Value);
                return Number.Max(minMass, maxMass.HasValue ? Number.Min(maxMass.Value, mass) : mass);
            }
            else
            {
                return maxMass.HasValue ? Randomizer.Instance.NextNumber(minMass, maxMass.Value) : minMass;
            }
        }

        private Number GetMassForSurfaceGravity(IShape? shape, double gravity)
            => shape is null ? Number.Zero : gravity * shape.ContainingRadius * shape.ContainingRadius / ScienceConstants.G;

        private protected override async Task GenerateMaterialAsync()
        {
            if (_material is null)
            {
                Material = await GetMaterialAsync(null, null).ConfigureAwait(false);
            }
        }

        private protected async Task<IMaterial> GetMaterialAsync(TerrestrialPlanetParams? planetParams, HabitabilityRequirements? habitabilityRequirements)
        {
            var (density, mass, shape) = await GetMatterAsync(planetParams, habitabilityRequirements).ConfigureAwait(false);
            return GetComposition(density, mass, shape, GetTemperature());
        }

        private async Task<(double density, Number mass, IShape shape)> GetMatterAsync(TerrestrialPlanetParams? planetParams, HabitabilityRequirements? habitabilityRequirements)
        {
            var density = GetDensity();

            double? gravity = null;
            if (planetParams?.SurfaceGravity.HasValue == true)
            {
                gravity = planetParams!.Value.SurfaceGravity!.Value;
            }
            else if (habitabilityRequirements?.MinimumGravity.HasValue == true
                || habitabilityRequirements?.MaximumGravity.HasValue == true)
            {
                double maxGravity;
                if (habitabilityRequirements?.MaximumGravity.HasValue == true)
                {
                    maxGravity = habitabilityRequirements!.Value.MaximumGravity!.Value;
                }
                else // Determine the absolute maximum gravity a terrestrial planet could have, before it would become a giant.
                {
                    var maxMass = MaxMassForType ?? _BaseMaxMassForType;
                    var maxVolume = maxMass / density;
                    var maxRadius = (maxVolume / MathConstants.FourThirdsPI).CubeRoot();
                    maxGravity = (double)(ScienceConstants.G * maxMass / (maxRadius * maxRadius));
                }
                gravity = Randomizer.Instance.NextDouble(habitabilityRequirements?.MinimumGravity ?? 0, maxGravity);
            }

            if (planetParams?.Radius.HasValue == true)
            {
                var shape = GetShape(knownRadius: Number.Max(MinimumRadius, planetParams!.Value.Radius!.Value));
                return (density, await GetMassAsync(gravity, shape).ConfigureAwait(false), shape);
            }
            else if (planetParams?.SurfaceGravity.HasValue == true
                || habitabilityRequirements?.MinimumGravity.HasValue == true
                || habitabilityRequirements?.MaximumGravity.HasValue == true)
            {
                var shape = GetShape(knownRadius: Number.Max(MinimumRadius, Number.Min(GetRadiusForSurfaceGravity(gravity!.Value), GetMaxRadius(density))));
                return (density, await GetMassAsync(gravity, shape).ConfigureAwait(false), shape);
            }
            else
            {
                var mass = await GetMassAsync(gravity).ConfigureAwait(false);
                return (density, mass, GetShape(density, mass));
            }
        }

        private Number GetRadiusForSurfaceGravity(double gravity) => (Mass * ScienceConstants.G / gravity).Sqrt();

        private async Task<double> GetSurfaceAlbedoAsync()
        {
            if (!_surfaceAlbedo.HasValue)
            {
                await GenerateAlbedoAsync().ConfigureAwait(false);
            }
            return _surfaceAlbedo ?? 0;
        }

        /// <summary>
        /// Calculates the temperature at which this <see cref="TerrestrialPlanet"/> will retain only
        /// a minimal atmosphere of out-gassed volatiles (comparable to Mercury).
        /// </summary>
        /// <returns>A temperature, in K.</returns>
        /// <remarks>
        /// If the planet is not massive enough or too hot to hold onto carbon dioxide gas, it is
        /// presumed that it will have a minimal atmosphere of out-gassed volatiles (comparable to Mercury).
        /// </remarks>
        private double GetTempForThinAtmosphere() => (double)(ScienceConstants.TwoG * Mass * new Number(70594833834763, -18) / Shape.ContainingRadius);

        private void HydrolyzeCrust()
        {
            if (Material.GetSurface() is Material material)
            {
                material.AddConstituents(CelestialSubstances.WetPlanetaryCrustConstituents);
            }
        }

        /// <summary>
        /// Determines if this <see cref="TerrestrialPlanet"/> is "habitable," defined as possessing
        /// liquid water. Does not rule out exotic lifeforms which subsist in non-aqueous
        /// environments. Also does not imply habitability by any particular creatures (e.g. humans),
        /// which may also depend on stricter criteria (e.g. atmospheric conditions).
        /// </summary>
        /// <returns><see langword="true"/> if this planet fits this minimal definition of
        /// "habitable;" otherwise <see langword="false"/>.</returns>
        private async Task<bool> IsHabitableAsync()
        {
            var maxTemp = await GetMaxSurfaceTemperatureAsync().ConfigureAwait(false);
            var minTemp = await GetMinSurfaceTemperatureAsync().ConfigureAwait(false);
            var avgTemp = await GetAverageSurfaceTemperatureAsync().ConfigureAwait(false);
            var pressure = Atmosphere.AtmosphericPressure;
            // Liquid water is checked at the min, max, and avg surface temperatures of the world,
            // under the assumption that if liquid water exists anywhere on the world, it is likely
            // to be found at at least one of those values, even if one or more are too extreme
            // (e.g. polar icecaps below freezing, or an equator above boiling).
            return Hydrosphere.Contains(Substances.All.Water.GetChemicalReference(), PhaseType.Liquid, maxTemp, pressure)
                || Hydrosphere.Contains(Substances.All.Seawater.GetHomogeneousReference(), PhaseType.Liquid, maxTemp, pressure)
                || Hydrosphere.Contains(Substances.All.Water.GetChemicalReference(), PhaseType.Liquid, minTemp, pressure)
                || Hydrosphere.Contains(Substances.All.Seawater.GetHomogeneousReference(), PhaseType.Liquid, minTemp, pressure)
                || Hydrosphere.Contains(Substances.All.Water.GetChemicalReference(), PhaseType.Liquid, avgTemp, pressure)
                || Hydrosphere.Contains(Substances.All.Seawater.GetHomogeneousReference(), PhaseType.Liquid, avgTemp, pressure);
        }
    }
}
