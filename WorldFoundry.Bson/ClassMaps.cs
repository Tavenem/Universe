using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Serializers;
using Substances;
using System;
using System.Collections.Generic;
using System.Reflection;
using WorldFoundry.CelestialBodies;
using WorldFoundry.CelestialBodies.BlackHoles;
using WorldFoundry.CelestialBodies.Planetoids;
using WorldFoundry.CelestialBodies.Planetoids.Asteroids;
using WorldFoundry.CelestialBodies.Planetoids.Planets;
using WorldFoundry.CelestialBodies.Planetoids.Planets.DwarfPlanets;
using WorldFoundry.CelestialBodies.Planetoids.Planets.GiantPlanets;
using WorldFoundry.CelestialBodies.Planetoids.Planets.TerrestrialPlanets;
using WorldFoundry.CelestialBodies.Stars;
using WorldFoundry.Climate;
using WorldFoundry.CosmicSubstances;
using WorldFoundry.Place;
using WorldFoundry.Space;
using WorldFoundry.Space.AsteroidFields;
using WorldFoundry.Space.Galaxies;

namespace WorldFoundry.Bson
{
    /// <summary>
    /// Static helper class for registering the class maps in this library.
    /// </summary>
    public static class ClassMaps
    {
        /// <summary>
        /// <para>
        /// The <see cref="IIdGenerator"/> to use. By default, <see cref="GuidGenerator"/> will be
        /// used, corresponding to the default <see cref="IItemIdProvider"/>.
        /// </para>
        /// <para>
        /// Must be set <i>before</i> calling <see cref="Register"/>.
        /// </para>
        /// </summary>
        public static IIdGenerator IdGenerator { get; set; } = GuidGenerator.Instance;

        /// <summary>
        /// Register all requisite class maps.
        /// </summary>
        public static void Register()
        {
            RegisterPlace();
            RegisterRegion();
            RegisterBody();
        }

        private static void RegisterPlace()
        {
            BsonClassMap.RegisterClassMap<Location>(cm =>
            {
                cm.AutoMap();
                cm.SetIsRootClass(true);
                cm.MapIdMember(x => x.Id).SetIdGenerator(IdGenerator);
                cm.UnmapMember(x => x.ContainingRegion);
                cm.UnmapMember(x => x.Position);
                cm.MapField("_position");
            });

            BsonClassMap.RegisterClassMap<Region>(cm =>
            {
                cm.AutoMap();
                cm.MapField("_children");
                cm.UnmapMember(x => x.Shape);
                cm.MapField("_shape");
            });

            BsonClassMap.RegisterClassMap<Territory>(cm =>
            {
                cm.AutoMap();
                cm.MapField("_regions");
            });

            BsonClassMap.RegisterClassMap<SurfaceRegion>(cm =>
            {
                cm.AutoMap();
                cm.MapField("_depthOverlay");
                cm.MapField("_depthOverlayHeight");
                cm.MapField("_depthOverlayWidth");
                cm.MapField("_elevationOverlay");
                cm.MapField("_elevationOverlayHeight");
                cm.MapField("_elevationOverlayWidth");
                cm.MapField("_flowOverlay");
                cm.MapField("_flowOverlayHeight");
                cm.MapField("_flowOverlayWidth");
                cm.MapField("_precipitationOverlays");
                cm.MapField("_precipitationOverlayHeight");
                cm.MapField("_precipitationOverlayWidth");
                cm.MapField("_snowfallOverlays");
                cm.MapField("_snowfallOverlayHeight");
                cm.MapField("_snowfallOverlayWidth");
                cm.MapField("_temperatureOverlaySummer");
                cm.MapField("_temperatureOverlayHeightSummer");
                cm.MapField("_temperatureOverlayWidthSummer");
                cm.MapField("_temperatureOverlayWinter");
                cm.MapField("_temperatureOverlayHeightWinter");
                cm.MapField("_temperatureOverlayWidthWinter");
            });
        }

        private static void RegisterRegion()
        {
            BsonClassMap.RegisterClassMap<CelestialRegion>(cm =>
            {
                cm.AutoMap();
                cm.MapField("_isPrepopulated");
                cm.MapField("_mass");
                cm.MapField("_temperature");
            });

            BsonClassMap.RegisterClassMap<GalaxySubgroup>(cm =>
            {
                cm.AutoMap();
                cm.MapField("_mainGalaxy");
            });

            BsonClassMap.RegisterClassMap<StarSystem>(cm =>
            {
                cm.AutoMap();
                cm.MapField("_stars");
            });

            BsonClassMap.RegisterClassMap<AsteroidField>(cm =>
            {
                cm.AutoMap();
                cm.MapField("_star");
            });

            BsonClassMap.RegisterClassMap<Galaxy>(cm =>
            {
                cm.AutoMap();
                cm.MapField("_galacticCore");
            });

            BsonClassMap.RegisterClassMap<GalaxyCluster>();
            BsonClassMap.RegisterClassMap<GalaxyGroup>();
            BsonClassMap.RegisterClassMap<GalaxySupercluster>();
            BsonClassMap.RegisterClassMap<PlanetaryNebula>();
            BsonClassMap.RegisterClassMap<Universe>();
            BsonClassMap.RegisterClassMap<OortCloud>();
            BsonClassMap.RegisterClassMap<DwarfGalaxy>();
            BsonClassMap.RegisterClassMap<EllipticalGalaxy>();
            BsonClassMap.RegisterClassMap<GlobularCluster>();
            BsonClassMap.RegisterClassMap<SpiralGalaxy>();
            BsonClassMap.RegisterClassMap<Nebula>();
            BsonClassMap.RegisterClassMap<HIIRegion>();
        }

        private static void RegisterBody()
        {
            BsonClassMap.RegisterClassMap<CelestialBody>(cm =>
            {
                cm.AutoMap();
                cm.UnmapMember(x => x.Albedo);
                cm.MapField("_albedo");
                cm.UnmapMember(x => x.Orbit);
                cm.MapField("_orbit");
                cm.UnmapMember(x => x.Shape);
                cm.UnmapMember(x => x.Substance);
                cm.MapField("_substance");
            });

            BsonClassMap.RegisterClassMap<Star>(cm =>
            {
                cm.AutoMap();
                cm.UnmapMember(x => x.Luminosity);
                cm.MapField("_luminosity");
                cm.UnmapMember(x => x.LuminosityClass);
                cm.MapField("_luminosityClass");
                cm.UnmapMember(x => x.SpectralClass);
                cm.MapField("_spectralClass");
            });

            BsonClassMap.RegisterClassMap<Resource>(cm =>
            {
                cm.MapField("_isVein");
                cm.MapField("_isPerturbation");
                cm.MapMember(x => x.Chemical);
                cm.MapMember(x => x.Proportion);
                cm.MapProperty("Seed");
                cm.MapConstructor(typeof(Resource).GetConstructor(new Type[] { typeof(Chemical), typeof(double), typeof(bool), typeof(bool), typeof(int?) }))
                .SetArguments(new MemberInfo[]
                {
                    typeof(Resource).GetProperty("Chemical"),
                    typeof(Resource).GetProperty("Proportion"),
                    typeof(Resource).GetField("_isVein", BindingFlags.Instance | BindingFlags.NonPublic),
                    typeof(Resource).GetField("_isPerturbation", BindingFlags.Instance | BindingFlags.NonPublic),
                    typeof(Resource).GetProperty("Seed", BindingFlags.Instance | BindingFlags.NonPublic),
                });
            });

            BsonClassMap.RegisterClassMap<Atmosphere>(cm =>
            {
                cm.AutoMap();
                cm.MapField("_averageSeaLevelDensity");
                cm.MapField("_precipitationFactor");
                cm.MapField("_atmosphericPressure");
                cm.MapProperty("AveragePrecipitation");
            });

            BsonClassMap.RegisterClassMap<Planetoid>(cm =>
            {
                cm.AutoMap();
                cm.MapField("_normalizedSeaLevel");
                cm.MapField("_seed1");
                cm.MapField("_seed2");
                cm.MapField("_seed3");
                cm.MapField("_seed4");
                cm.MapField("_seed5");
                cm.MapField("_angleOfRotation");
                cm.MapField("_atmosphere");
                cm.UnmapMember(x => x.AxialPrecession);
                cm.MapField("_axialPrecession");
                cm.UnmapMember(x => x.AxialTilt);
                cm.MapField("_axis");
                cm.UnmapMember(x => x.Density);
                cm.MapField("_density");
                cm.UnmapMember(x => x.HasMagnetosphere);
                cm.MapField("_hasMagnetosphere");
                cm.MapField("_maxElevation");
                cm.UnmapMember(x => x.RotationalPeriod);
                cm.MapField("_rotationalPeriod");
                cm.MapField("_resources").SetSerializer(new DictionaryInterfaceImplementerSerializer<Dictionary<string, Resource>>(DictionaryRepresentation.ArrayOfDocuments));
                cm.MapField("_satelliteIDs");
                cm.UnmapMember(x => x.SeaLevel);
                cm.MapField("_axisRotation");
                cm.MapField("_eccentricity");
                cm.MapField("_maxMass");
                cm.MapField("_minMass");
            });

            BsonClassMap.RegisterClassMap<Planemo>(cm =>
            {
                cm.AutoMap();
                cm.MapField("_rings");
            });

            BsonClassMap.RegisterClassMap<TerrestrialPlanet>(cm =>
            {
                cm.AutoMap();
                cm.MapField("_hydrosphereProportion");
                cm.MapField("_hydrosphere");
                cm.MapField("_surfaceAlbedo");
            });

            BsonClassMap.RegisterClassMap<Singularity>();
            BsonClassMap.RegisterClassMap<BlackHole>();
            BsonClassMap.RegisterClassMap<SupermassiveBlackHole>();
            BsonClassMap.RegisterClassMap<Comet>();
            BsonClassMap.RegisterClassMap<Asteroid>();
            BsonClassMap.RegisterClassMap<CTypeAsteroid>();
            BsonClassMap.RegisterClassMap<MTypeAsteroid>();
            BsonClassMap.RegisterClassMap<STypeAsteroid>();
            BsonClassMap.RegisterClassMap<DwarfPlanet>();
            BsonClassMap.RegisterClassMap<LavaDwarfPlanet>();
            BsonClassMap.RegisterClassMap<RockyDwarfPlanet>();
            BsonClassMap.RegisterClassMap<GiantPlanet>();
            BsonClassMap.RegisterClassMap<IceGiant>();
            BsonClassMap.RegisterClassMap<CarbonPlanet>();
            BsonClassMap.RegisterClassMap<IronPlanet>();
            BsonClassMap.RegisterClassMap<LavaPlanet>();
            BsonClassMap.RegisterClassMap<OceanPlanet>();
            BsonClassMap.RegisterClassMap<GiantStar>();
            BsonClassMap.RegisterClassMap<BlueGiant>();
            BsonClassMap.RegisterClassMap<BrownDwarf>();
            BsonClassMap.RegisterClassMap<NeutronStar>();
            BsonClassMap.RegisterClassMap<RedGiant>();
            BsonClassMap.RegisterClassMap<WhiteDwarf>();
            BsonClassMap.RegisterClassMap<YellowGiant>();
        }
    }
}
