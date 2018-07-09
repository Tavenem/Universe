using ExtensionLib;
using MathAndScience.MathUtil;
using MathAndScience.Science;
using Substances;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Troschuetz.Random;
using WorldFoundry.CelestialBodies.Planetoids.Asteroids;
using WorldFoundry.CelestialBodies.Planetoids.Planets.DwarfPlanets;
using WorldFoundry.CelestialBodies.Stars;
using WorldFoundry.Climate;
using WorldFoundry.Orbits;
using WorldFoundry.Space;
using WorldFoundry.Space.Galaxies;
using WorldFoundry.Substances;
using WorldFoundry.WorldGrids;

namespace WorldFoundry.CelestialBodies.Planetoids.Planets.TerrestrialPlanets
{
    /// <summary>
    /// A primarily rocky planet, relatively small in comparison to gas and ice giants.
    /// </summary>
    public class TerrestrialPlanet : Planemo
    {
        private const float TemperatureErrorTolerance = 0.5f;

        internal static bool canHaveOxygen = true;
        /// <summary>
        /// Used to allow or prevent oxygen in the atmosphere of a terrestrial planet.
        /// </summary>
        /// <remarks>
        /// True by default, but subclasses may hide this with false when their particular natures
        /// make the presence of significant amounts of oxygen impossible.
        /// </remarks>
        protected virtual bool CanHaveOxygen => canHaveOxygen;

        internal static bool canHaveWater = true;
        /// <summary>
        /// Used to allow or prevent water in the composition and atmosphere of a terrestrial planet.
        /// </summary>
        /// <remarks>
        /// True by default, but subclasses may hide this with false when their particular natures
        /// make the presence of significant amounts of water impossible.
        /// </remarks>
        protected virtual bool CanHaveWater => canHaveWater;

        private const int extremeRotationalPeriod = 22000000;
        private protected override int ExtremeRotationalPeriod => extremeRotationalPeriod;

        /// <summary>
        /// Any <see cref="TerrestrialPlanets.HabitabilityRequirements"/> specified for this <see
        /// cref="TerrestrialPlanet"/>.
        /// </summary>
        private protected HabitabilityRequirements HabitabilityRequirements { get; set; }

        private float? _halfITCZWidth;
        /// <summary>
        /// Half the width of the Inter Tropical Convergence Zone of this <see
        /// cref="TerrestrialPlanet"/>, in meters.
        /// </summary>
        internal float HalfITCZWidth
        {
            get => GetProperty(ref _halfITCZWidth, GenerateITCZ) ?? 0;
            private set => _halfITCZWidth = value;
        }

        /// <summary>
        /// Indicates whether or not this planet has a native population of living organisms.
        /// </summary>
        /// <remarks>
        /// The complexity of life is not presumed. If a planet is basically habitable (liquid
        /// surface water), life in at least a single-celled form may be indicated, and may affect
        /// the atmospheric composition.
        /// </remarks>
        public bool HasBiosphere { get; set; }

        private protected IComposition _hydrosphere;
        /// <summary>
        /// This planet's surface liquids and ices (not necessarily water).
        /// </summary>
        /// <remarks>
        /// Represented as a separate <see cref="IComposition"/> rather than as a top layer of <see
        /// cref="CelestialEntity.Substance"/> for ease of reference.
        /// </remarks>
        public IComposition Hydrosphere
        {
            get => GetProperty(ref _hydrosphere, GenerateHydrosphere);
            internal set => _hydrosphere = value;
        }

        /// <summary>
        /// The proportion (by mass) of the <see cref="TerrestrialPlanet"/> which is comprised by its
        /// <see cref="Hydrosphere"/>.
        /// </summary>
        public float HydrosphereProportion { get; internal set; }

        internal static float maxDensity = 6000;
        private protected virtual float MaxDensity => maxDensity;

        internal static double maxMassForType = 6.0e25;
        /// <summary>
        /// The maximum mass allowed for this type of <see cref="Planetoid"/> during random
        /// generation, in kg. Null indicates no maximum.
        /// </summary>
        /// <remarks>
        /// At around this limit the planet will have sufficient mass to retain hydrogen, and become
        /// a giant.
        /// </remarks>
        internal override double? MaxMassForType => maxMassForType;

        private const int maxRotationalPeriod = 6500000;
        private protected override int MaxRotationalPeriod => maxRotationalPeriod;

        internal static float metalProportion = 0.05f;
        /// <summary>
        /// Used to set the proportionate amount of metal in the composition of a terrestrial planet.
        /// </summary>
        private protected virtual float MetalProportion => metalProportion;

        internal static float minDensity = 3750;
        private protected virtual float MinDensity => minDensity;

        internal static double minMassForType = 2.0e22;
        /// <summary>
        /// The minimum mass allowed for this type of <see cref="Planetoid"/> during random
        /// generation, in kg. Null indicates a minimum of 0.
        /// </summary>
        /// <remarks>
        /// An arbitrary limit separating rogue dwarf planets from rogue planets. Within orbital
        /// systems, a calculated value for clearing the neighborhood is used instead.
        /// </remarks>
        internal override double? MinMassForType => minMassForType;

        private const int minRotationalPeriod = 40000;
        private protected override int MinRotationalPeriod => minRotationalPeriod;

        private const string planemoClassPrefix = "Terrestrial";
        /// <summary>
        /// A prefix to the <see cref="CelestialEntity.TypeName"/> for this class of <see cref="Planemo"/>.
        /// </summary>
        public override string PlanemoClassPrefix => planemoClassPrefix;

        /// <summary>
        /// The parameters which control the random generation of this <see
        /// cref="TerrestrialPlanet"/>'s characteristics.
        /// </summary>
        public TerrestrialPlanetParams PlanetParams { get; set; }

        internal new static float ringChance = 10;
        /// <summary>
        /// The chance that this <see cref="Planemo"/> will have rings, as a rate between 0.0 and 1.0.
        /// </summary>
        /// <remarks>
        /// There is a low chance of most planets having substantial rings; 10 for <see
        /// cref="TerrestrialPlanet"/>s.
        /// </remarks>
        protected override float RingChance => ringChance;

        /// <summary>
        /// The number of <see cref="Season"/>s in a year, based on the last <see cref="Season"/> set.
        /// </summary>
        public int SeasonCount { get; private set; }

        /// <summary>
        /// The collection of <see cref="Season"/>s which make up a year of this <see
        /// cref="TerrestrialPlanet"/>'s weather.
        /// </summary>
        /// <remarks>
        /// This collection should not be used directly. Instead, use <see cref="GetSeason(int,
        /// int)"/> or <see cref="GetSeasons(int)"/>, both of which use this cached list when
        /// possible, and generate new <see cref="Season"/>s when needed.
        /// </remarks>
        public IList<Season> Seasons { get; private set; }

        private float? _surfaceAlbedo;
        /// <summary>
        /// Since the total albedo of a terrestrial planet may change based on surface ice and cloud
        /// cover, the base surface albedo is maintained separately.
        /// </summary>
        public float SurfaceAlbedo
        {
            get => GetProperty(ref _surfaceAlbedo, GenerateAlbedo) ?? 0;
            internal set => _surfaceAlbedo = value;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="TerrestrialPlanet"/>.
        /// </summary>
        public TerrestrialPlanet() : base() { }

        /// <summary>
        /// Initializes a new instance of <see cref="TerrestrialPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="TerrestrialPlanet"/> is located.
        /// </param>
        public TerrestrialPlanet(CelestialRegion parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="TerrestrialPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="TerrestrialPlanet"/> is located.
        /// </param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="TerrestrialPlanet"/> during random generation, in kg.
        /// </param>
        public TerrestrialPlanet(CelestialRegion parent, double maxMass) : base(parent, maxMass) { }

        /// <summary>
        /// Initializes a new instance of <see cref="TerrestrialPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="TerrestrialPlanet"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="TerrestrialPlanet"/>.</param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="TerrestrialPlanet"/> during random generation, in kg.
        /// </param>
        public TerrestrialPlanet(CelestialRegion parent, Vector3 position, double maxMass) : base(parent, position, maxMass) { }

        /// <summary>
        /// Initializes a new instance of <see cref="TerrestrialPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see
        /// cref="TerrestrialPlanet"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="TerrestrialPlanet"/>.</param>
        /// <param name="planetParams">
        /// A set of parameters which will control the random generation of this <see
        /// cref="TerrestrialPlanet"/>'s characteristics.
        /// </param>
        /// <param name="requirements">
        /// A set of requirements which will control the random generation of this <see
        /// cref="TerrestrialPlanet"/>'s characteristics.
        /// </param>
        public TerrestrialPlanet(
            CelestialRegion parent,
            Vector3 position,
            TerrestrialPlanetParams planetParams = null,
            HabitabilityRequirements requirements = null) : base(parent, position)
        {
            HabitabilityRequirements = requirements;
            PlanetParams = planetParams;
        }

        /// <summary>
        /// A shortcut constructor: given a star, generates a terrestrial planet with default
        /// parameters and human habiltability requirements, and puts the planet in orbit around the star.
        /// </summary>
        /// <param name="star">
        /// A star which the new planet will orbit, at a distance suitable for human habitability.
        /// </param>
        /// <param name="planetParams">
        /// A set of <see cref="TerrestrialPlanetParams"/>. If omitted, the defaults will be used.
        /// </param>
        /// <returns>A human-habitable planet with default parameters.</returns>
        public static TerrestrialPlanet DefaultHumanPlanetForStar(Star star, TerrestrialPlanetParams planetParams = null)
        {
            var planet = new TerrestrialPlanet(
                star.Parent,
                Vector3.Zero,
                planetParams ?? TerrestrialPlanetParams.FromDefaults(),
                HabitabilityRequirements.HumanHabitabilityRequirements);
            planet.GenerateOrbit(star);
            return planet;
        }

        /// <summary>
        /// A shortcut constructor: generates a terrestrial planet with default parameters and human
        /// habiltability requirements orbiting a Sol-like star in a new system in the given galaxy.
        /// </summary>
        /// <param name="galaxy">A galaxy in which to situate the new planet.</param>
        /// <param name="planetParams">Any parameters which specify the conditions of the planet to be generated.</param>
        /// <returns>A human-habitable planet with default parameters.</returns>
        public static TerrestrialPlanet DefaultHumanPlanetForGalaxy(Galaxy galaxy, TerrestrialPlanetParams planetParams = null)
        {
            var system = galaxy.GenerateChildOfType(typeof(StarSystem), null, new object[] { typeof(Star), SpectralClass.G, LuminosityClass.V }) as StarSystem;
            var star = system.Stars.FirstOrDefault();
            return DefaultHumanPlanetForStar(star, planetParams);
        }

        /// <summary>
        /// A shortcut constructor: generates a terrestrial planet with default parameters and human
        /// habiltability requirements orbiting a Sol-like star in a new spiral galaxy in the given universe.
        /// </summary>
        /// <param name="universe">A universe in which to situate the new planet.</param>
        /// <param name="planetParams">Any parameters which specify the conditions of the planet to be generated.</param>
        /// <returns>A human-habitable planet with default parameters.</returns>
        public static TerrestrialPlanet DefaultHumanPlanetForUniverse(Universe universe, TerrestrialPlanetParams planetParams = null)
        {
            var gsc = universe.GenerateChildOfType(typeof(GalaxySupercluster), null, null) as GalaxySupercluster;
            var gc = gsc.GenerateChildOfType(typeof(GalaxyCluster), null, null) as GalaxyCluster;
            var gg = gc.GenerateChildOfType(typeof(GalaxyGroup), null, null) as GalaxyGroup;
            gg.PopulateRegion(Vector3.Zero);
            var gsg = gg.Children.Where(x => x is GalaxySubgroup).FirstOrDefault() as GalaxySubgroup;
            return DefaultHumanPlanetForGalaxy(gsg.MainGalaxy, planetParams);
        }

        /// <summary>
        /// A shortcut constructor: generates a terrestrial planet with default parameters and human
        /// habiltability requirements orbiting a Sol-like star in a spiral galaxy in a new universe.
        /// </summary>
        /// <param name="planetParams">Any parameters which specify the conditions of the planet to be generated.</param>
        /// <returns>A human-habitable planet with default parameters.</returns>
        public static TerrestrialPlanet DefaultHumanPlanetNewUniverse(TerrestrialPlanetParams planetParams = null) => DefaultHumanPlanetForUniverse(new Universe(), planetParams);

        private void AdjustOrbitForTemperature(Star star, double? semiMajorAxis, float trueAnomaly, float targetTemp)
        {
            // Orbital distance averaged over time (mean anomaly) = semi-major axis * (1 + eccentricity^2 / 2).
            // This allows calculation of the correct distance/orbit for an average
            // orbital temperature (rather than the temperature at the current position).
            if (PlanetParams?.RevolutionPeriod.HasValue == true)
            {
                var distance = (float)(semiMajorAxis.Value * (1 + (Eccentricity * Eccentricity) / 2));
                star.Luminosity = GetLuminosityForTemperature(star, targetTemp, distance);
            }
            else
            {
                semiMajorAxis = GetDistanceForTemperature(star, targetTemp) / (1 + (Eccentricity * Eccentricity) / 2);
                GenerateOrbit(star, semiMajorAxis.Value, trueAnomaly);
            }
            ResetCachedTemperatures();
        }

        /// <summary>
        /// Adjusts the phase of various atmospheric and surface substances depending on the surface
        /// temperature of the body.
        /// </summary>
        /// <remarks>
        /// Despite the theoretical possibility of an atmosphere cold enough to precipitate some of
        /// the noble gases, or hydrogen, they are ignored and presumed to exist always as trace
        /// atmospheric gases, never surface liquids or ices, or in large enough quantities to form clouds.
        /// </remarks>
        private void CalculatePhases(int counter, float surfaceTemp, ref float hydrosphereAtmosphereRatio, ref float adjustedAtmosphericPressure)
        {
            Atmosphere.CalculatePhases(CanHaveOxygen, surfaceTemp, ref hydrosphereAtmosphereRatio, ref adjustedAtmosphericPressure);

            var oldAlbedo = Albedo;

            // Ices significantly impact albedo.
            var iceAmount = Math.Min(1, Hydrosphere.GetSurface().GetChemicals(Phase.Solid).Sum(x => x.proportion));
            Albedo = (SurfaceAlbedo * (1 - iceAmount)) + (0.9f * iceAmount);

            // Clouds also impact albedo.
            var cloudCover = Math.Min(1, Atmosphere.AtmosphericPressure * Atmosphere.Composition.GetCloudCover() / 100);
            Albedo = (SurfaceAlbedo * (1 - cloudCover)) + (0.9f * cloudCover);

            // An albedo change might significantly alter surface temperature, which may require a
            // re-calculation (but not too many). 5K is used as the threshold for re-calculation,
            // which may lead to some inaccuracies, but should avoid over-repetition for small changes.
            if (counter < 10 && Albedo != oldAlbedo)
            {
                var newSurfaceTemp = Atmosphere.GetSurfaceTemperatureAverageOrbital();
                if (Math.Abs(surfaceTemp - newSurfaceTemp) > 5)
                {
                    CalculatePhases(counter + 1, newSurfaceTemp, ref hydrosphereAtmosphereRatio, ref adjustedAtmosphericPressure);
                }
            }
        }

        private void ClassifyTerrain()
        {
            foreach (var t in Topography.Tiles)
            {
                var land = 0;
                var water = 0;
                for (var i = 0; i < t.EdgeCount; i++)
                {
                    if (Topography.Corners[t.Corners[i]].Elevation < 0)
                    {
                        water++;
                    }
                    else
                    {
                        land++;
                    }
                }
                if (t.Elevation < 0)
                {
                    water++;
                }
                else
                {
                    land++;
                }
                t.TerrainType = (land > 0 && water > 0)
                    ? TerrainType.Coast
                    : (land > 0 ? TerrainType.Land : TerrainType.Water);
            }
            foreach (var c in Topography.Corners)
            {
                var land = 0;
                for (var i = 0; i < 3; i++)
                {
                    if (Topography.Corners[c.Corners[i]].Elevation >= 0)
                    {
                        land++;
                    }
                }
                c.TerrainType = c.Elevation < 0
                    ? (land > 0 ? TerrainType.Coast : TerrainType.Water)
                    : TerrainType.Land;
            }
            foreach (var e in Topography.Edges)
            {
                var type = TerrainType.Land;
                for (var i = 0; i < 2; i++)
                {
                    if (Topography.Corners[e.Corners[i]].TerrainType != type)
                    {
                        type = i == 0 ? Topography.Tiles[e.Tiles[i]].TerrainType : TerrainType.Coast;
                    }
                }
                e.TerrainType = type;
            }
        }

        /// <summary>
        /// Determines an albedo for this <see cref="CelestialBody"/> (a value between 0 and 1).
        /// </summary>
        /// <remarks>
        /// Also sets <see cref="SurfaceAlbedo"/> for terrestrial planets.
        /// </remarks>
        private protected override void GenerateAlbedo()
        {
            Albedo = (float)Math.Round(Randomizer.Static.NextDouble(0.1, 0.6), 2);
            SurfaceAlbedo = Albedo;
        }

        /// <summary>
        /// Determines an angle between the Y-axis and the axis of rotation for this <see cref="Planetoid"/>.
        /// </summary>
        private protected override void GenerateAngleOfRotation()
        {
            if (PlanetParams?.AxialTilt.HasValue != true)
            {
                base.GenerateAngleOfRotation();
            }
            else
            {
                _axialPrecession = (float)Math.Round(Randomizer.Static.NextDouble(MathConstants.TwoPI), 4);
                var axialTilt = PlanetParams.AxialTilt.Value;
                if (Orbit != null)
                {
                    axialTilt += Orbit.Inclination;
                }
                while (axialTilt < 0)
                {
                    axialTilt += (float)MathConstants.TwoPI;
                }
                while (axialTilt >= MathConstants.TwoPI)
                {
                    axialTilt -= (float)MathConstants.TwoPI;
                }
                AngleOfRotation = axialTilt;
            }
        }

        /// <summary>
        /// Generates an atmosphere for this <see cref="Planetoid"/>.
        /// </summary>
        private protected override void GenerateAtmosphere()
        {
            var surfaceTemp = GetTotalTemperatureAverageOrbital();
            if (surfaceTemp >= GetTempForThinAtmosphere())
            {
                GenerateAtmosphereTrace(surfaceTemp);
            }
            else
            {
                GenerateAtmosphereThick(surfaceTemp);
            }

            var hydrosphereAtmosphereRatio = GetHydrosphereAtmosphereRatio();
            var adjustedAtmosphericPressure = Atmosphere.AtmosphericPressure;

            // Water may be removed, or if not may remove CO2 from the atmosphere, depending on
            // planetary conditions.
            if (Hydrosphere.ContainsSubstance(Chemical.Water, Phase.Any)
                || Hydrosphere.ContainsSubstance(Chemical.Water_Salt, Phase.Any)
                || Atmosphere.Composition.ContainsSubstance(Chemical.Water, Phase.Any))
            {
                var polarTemp = surfaceTemp;
                var vaporPressure = Chemical.Water.CalculateVaporPressure(surfaceTemp);

                // First calculate water phases at effective temp, to establish a baseline
                // for the presence of water and its effect on CO2.
                Atmosphere.CalculateGasPhaseMix(
                    CanHaveOxygen,
                    Chemical.Water,
                    surfaceTemp,
                    polarTemp,
                    ref hydrosphereAtmosphereRatio,
                    ref adjustedAtmosphericPressure);

                // Recalculate temperatures based on the new atmosphere.
                ResetCachedTemperatures();
                surfaceTemp = Atmosphere.GetSurfaceTemperatureAverageOrbital();
                polarTemp = Atmosphere.GetSurfaceTemperatureAverageOrbital(true);

                // Recalculate the phases of water based on the new temperature.
                Atmosphere.CalculateGasPhaseMix(
                    CanHaveOxygen,
                    Chemical.Water,
                    surfaceTemp,
                    polarTemp,
                    ref hydrosphereAtmosphereRatio,
                    ref adjustedAtmosphericPressure);

                // If life alters the greenhouse potential, temperature and water phase must be
                // recalculated once again.
                if (GenerateLife())
                {
                    ResetCachedTemperatures();
                    surfaceTemp = Atmosphere.GetSurfaceTemperatureAverageOrbital();
                    polarTemp = Atmosphere.GetSurfaceTemperatureAverageOrbital(true);
                    Atmosphere.CalculateGasPhaseMix(
                        CanHaveOxygen,
                        Chemical.Water,
                        surfaceTemp,
                        polarTemp,
                        ref hydrosphereAtmosphereRatio,
                        ref adjustedAtmosphericPressure);
                }
            }
            else
            {
                // Recalculate temperature based on the new atmosphere.
                ResetCachedTemperatures();
                surfaceTemp = Atmosphere.GetSurfaceTemperatureAverageOrbital();
            }

            foreach (var requirement in Atmosphere.ConvertRequirementsForPressure(HabitabilityRequirements?.AtmosphericRequirements)
                .Concat(Atmosphere.ConvertRequirementsForPressure(PlanetParams?.AtmosphericRequirements)))
            {
                var proportion = Atmosphere.Composition.GetProportion(requirement.Chemical, requirement.Phase);
                if (proportion.IsZero())
                {
                    Atmosphere.Composition.AddComponent(
                        requirement.Chemical,
                        requirement.Phase != Phase.Any && Enum.IsDefined(typeof(Phase), requirement.Phase)
                            ? requirement.Phase
                            : requirement.Phase == Phase.Any || requirement.Phase.HasFlag(Phase.Gas)
                                ? Phase.Gas
                                : requirement.Phase.HasFlag(Phase.Liquid)
                                    ? Phase.Liquid
                                    : Phase.Solid,
                        requirement.MinimumProportion);
                }
                else if (proportion < requirement.MinimumProportion)
                {
                    Atmosphere.Composition.SetProportion(requirement.Chemical, requirement.Phase, requirement.MinimumProportion);
                }
                else if (requirement.MaximumProportion.HasValue && proportion > requirement.MaximumProportion)
                {
                    Atmosphere.Composition.SetProportion(requirement.Chemical, requirement.Phase, requirement.MaximumProportion.Value);
                }
            }

            CalculatePhases(0, surfaceTemp, ref hydrosphereAtmosphereRatio, ref adjustedAtmosphericPressure);

            if (PlanetParams?.AtmosphericPressure.HasValue != true && HabitabilityRequirements == null)
            {
                Atmosphere.AtmosphericPressure = Math.Max(0, adjustedAtmosphericPressure);
            }

            // If the adjustments have led to the loss of liquid water, then there is no life after
            // all (this may be interpreted as a world which once supported life, but became
            // inhospitable due to the environmental changes that life produced).
            if (!IsHabitable())
            {
                HasBiosphere = false;
            }
        }

        private void GenerateAtmosphereThick(float surfaceTemp)
        {
            float pressure;
            if (PlanetParams?.AtmosphericPressure.HasValue == true)
            {
                pressure = Math.Max(0, PlanetParams.AtmosphericPressure.Value);
            }
            else if (HabitabilityRequirements?.MinimumPressure.HasValue == true
                || HabitabilityRequirements?.MaximumPressure.HasValue == true)
            {
                // If there is a minimum but no maximum, a half-Gaussian distribution with the minimum as both mean and the basis for the sigma is used.
                if (!HabitabilityRequirements?.MaximumPressure.HasValue == true)
                {
                    pressure = HabitabilityRequirements.MinimumPressure.Value
                        + (float)Math.Abs(Randomizer.Static.Normal(0, HabitabilityRequirements.MinimumPressure.Value / 3));
                }
                else
                {
                    pressure = (float)Randomizer.Static.NextDouble(HabitabilityRequirements.MinimumPressure ?? 0, HabitabilityRequirements.MaximumPressure.Value);
                }
            }
            else
            {
                double factor;
                // Low-gravity planets without magnetospheres are less likely to hold onto the bulk
                // of their atmospheres over long periods.
                if (Mass >= 1.5e24 || HasMagnetosphere)
                {
                    factor = Mass / 1.8e5;
                }
                else
                {
                    factor = Mass / 1.2e6;
                }

                var mass = Math.Max(factor, Randomizer.Static.Lognormal(0, factor * 4));
                pressure = (float)((mass * SurfaceGravity) / (1000 * MathConstants.FourPI * RadiusSquared));
            }

            // For terrestrial (non-giant) planets, these gases remain at low concentrations due to
            // atmospheric escape.
            var h = (float)Randomizer.Static.NextDouble(0.5e-7, 0.2e-6);
            var he = (float)Randomizer.Static.NextDouble(0.26e-6, 1.0e-5);

            // 50% chance not to have these components at all.
            var ch4 = (float)Math.Max(0, Math.Round(Randomizer.Static.NextDouble(-0.5, 0.5), 4));
            var traceTotal = ch4;

            var co = (float)Math.Max(0, Math.Round(Randomizer.Static.NextDouble(-0.5, 0.5), 4));
            traceTotal += co;

            var so2 = (float)Math.Max(0, Math.Round(Randomizer.Static.NextDouble(-0.5, 0.5), 4));
            traceTotal += so2;

            var trace = TMath.IsZero(traceTotal) ? 0 : (float)Randomizer.Static.NextDouble(1.5e-4, 2.5e-3);
            var traceRatio = TMath.IsZero(traceTotal) ? 0 : trace / traceTotal;
            ch4 *= traceRatio;
            co *= traceRatio;
            so2 *= traceRatio;

            // CO2 makes up the bulk of a thick atmosphere by default (although the presence of water
            // may change this later).
            var co2 = (float)Math.Round(Randomizer.Static.NextDouble(0.97, 0.99) - trace, 4);

            // If there is water on the surface, the water in the air will be determined based on
            // vapor pressure later, and should not be randomly assigned. Otherwise, there is a small
            // chance of water vapor without significant surface water (results of cometary deposits, etc.)
            float waterVapor = 0;
            var surfaceWater = Hydrosphere.ContainsSubstance(Chemical.Water, Phase.Any)
                || Hydrosphere.ContainsSubstance(Chemical.Water_Salt, Phase.Any);
            if (CanHaveWater && !surfaceWater)
            {
                waterVapor = (float)Math.Max(0, Math.Round(Randomizer.Static.NextDouble(-0.05, 0.001), 4));
            }

            // Always at least some oxygen if there is water, planetary composition allowing
            float o2 = 0;
            if (CanHaveOxygen)
            {
                if (waterVapor != 0)
                {
                    o2 = waterVapor * 0.0001f;
                }
                else if (surfaceWater)
                {
                    o2 = (float)Math.Round(Randomizer.Static.NextDouble(0.002), 5);
                }
            }

            // N2 (largely inert gas) comprises whatever is left after the other components have been
            // determined. This is usually a trace amount, unless CO2 has been reduced to a trace, in
            // which case it will comprise the bulk of the atmosphere.
            var n2 = 1 - (h + he + co2 + waterVapor + o2 + trace);

            // Some portion of the N2 may be Ar instead.
            var ar = (float)Math.Max(0, n2 * Randomizer.Static.NextDouble(-0.02, 0.04));
            n2 -= ar;
            // An even smaller fraction may be Kr.
            var kr = (float)Math.Max(0, n2 * Randomizer.Static.NextDouble(-2.5e-4, 5.0e-4));
            n2 -= kr;
            // An even smaller fraction may be Xe or Ne.
            var xe = (float)Math.Max(0, n2 * Randomizer.Static.NextDouble(-1.8e-5, 3.5e-5));
            n2 -= xe;
            var ne = (float)Math.Max(0, n2 * Randomizer.Static.NextDouble(-1.8e-5, 3.5e-5));
            n2 -= ne;

            var atmosphere = new Composite(new Dictionary<(Chemical chemical, Phase phase), float>
            {
                { (Chemical.CarbonDioxide, Phase.Gas), co2 },
                { (Chemical.Helium, Phase.Gas), he },
                { (Chemical.Hydrogen, Phase.Gas), h },
                { (Chemical.Nitrogen, Phase.Gas), n2 },
            });
            if (ar > 0)
            {
                atmosphere.Components[(Chemical.Argon, Phase.Gas)] = ar;
            }
            if (co > 0)
            {
                atmosphere.Components[(Chemical.CarbonMonoxide, Phase.Gas)] = co;
            }
            if (kr > 0)
            {
                atmosphere.Components[(Chemical.Krypton, Phase.Gas)] = kr;
            }
            if (ch4 > 0)
            {
                atmosphere.Components[(Chemical.Methane, Phase.Gas)] = ch4;
            }
            if (o2 > 0)
            {
                atmosphere.Components[(Chemical.Oxygen, Phase.Gas)] = o2;
            }
            if (so2 > 0)
            {
                atmosphere.Components[(Chemical.SulphurDioxide, Phase.Gas)] = so2;
            }
            if (waterVapor > 0)
            {
                atmosphere.Components[(Chemical.Water, Phase.Gas)] = waterVapor;
            }
            if (xe > 0)
            {
                atmosphere.Components[(Chemical.Xenon, Phase.Gas)] = xe;
            }
            Atmosphere = new Atmosphere(this, atmosphere, pressure);
        }

        private void GenerateAtmosphereTrace(float surfaceTemp)
        {
            // For terrestrial (non-giant) planets, these gases remain at low concentrations due to
            // atmospheric escape.
            var h = (float)Math.Round(Randomizer.Static.NextDouble(0.5e-7, 0.2e-6), 4);
            var he = (float)Math.Round(Randomizer.Static.NextDouble(0.26e-6, 1.0e-5), 4);

            // 50% chance not to have these components at all.
            var ch4 = (float)Math.Max(0, Math.Round(Randomizer.Static.NextDouble(-0.5, 0.5), 4));
            var total = ch4;

            var co = (float)Math.Max(0, Math.Round(Randomizer.Static.NextDouble(-0.5, 0.5), 4));
            total += co;

            var so2 = (float)Math.Max(0, Math.Round(Randomizer.Static.NextDouble(-0.5, 0.5), 4));
            total += so2;

            var n2 = (float)Math.Max(0, Math.Round(Randomizer.Static.NextDouble(-0.5, 0.5), 4));
            total += n2;

            // Noble traces: selected as fractions of N2, if present, to avoid over-representation.
            var ar = n2 > 0 ? (float)Math.Max(0, n2 * Randomizer.Static.NextDouble(-0.02, 0.04)) : 0;
            n2 -= ar;
            var kr = n2 > 0 ? (float)Math.Max(0, n2 * Randomizer.Static.NextDouble(-0.02, 0.04)) : 0;
            n2 -= kr;
            var xe = n2 > 0 ? (float)Math.Max(0, n2 * Randomizer.Static.NextDouble(-0.02, 0.04)) : 0;
            n2 -= xe;

            // Carbon monoxide means at least some carbon dioxide, as well.
            var co2 = (float)Math.Round(co > 0
                ? Randomizer.Static.NextDouble(0.5)
                : Math.Max(0, Randomizer.Static.NextDouble(-0.5, 0.5)),
                4);
            total += co2;

            // If there is water on the surface, the water in the air will be determined based on
            // vapor pressure later, and should not be randomly assigned. Otherwise, there is a small
            // chance of water vapor without significant surface water (results of cometary deposits, etc.)
            float waterVapor = 0;
            if (CanHaveWater
                && !Hydrosphere.ContainsSubstance(Chemical.Water, Phase.Any)
                && !Hydrosphere.ContainsSubstance(Chemical.Water_Salt, Phase.Any))
            {
                waterVapor = (float)Math.Max(0, Math.Round(Randomizer.Static.NextDouble(-0.05, 0.001), 4));
            }
            total += waterVapor;

            float o2 = 0;
            if (CanHaveOxygen)
            {
                // Always at least some oxygen if there is water, planetary composition allowing
                o2 = waterVapor > 0
                    ? waterVapor * 0.0001f
                    : (float)Math.Max(0, Math.Round(Randomizer.Static.NextDouble(-0.5, 0.5), 4));
            }
            total += o2;

            var ratio = (1 - h - he) / total;
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
                Atmosphere = new Atmosphere(this, Material.Empty.GetDeepCopy(), 0);
            }
            else
            {
                var atmosphere = new Composite(new Dictionary<(Chemical chemical, Phase phase), float>
                {
                    { (Chemical.Helium, Phase.Gas), he },
                    { (Chemical.Hydrogen, Phase.Gas), h },
                });
                if (ar > 0)
                {
                    atmosphere.Components[(Chemical.Argon, Phase.Gas)] = ar;
                }
                if (co2 > 0)
                {
                    atmosphere.Components[(Chemical.CarbonDioxide, Phase.Gas)] = co2;
                }
                if (co > 0)
                {
                    atmosphere.Components[(Chemical.CarbonMonoxide, Phase.Gas)] = co;
                }
                if (kr > 0)
                {
                    atmosphere.Components[(Chemical.Krypton, Phase.Gas)] = kr;
                }
                if (ch4 > 0)
                {
                    atmosphere.Components[(Chemical.Methane, Phase.Gas)] = ch4;
                }
                if (n2 > 0)
                {
                    atmosphere.Components[(Chemical.Nitrogen, Phase.Gas)] = n2;
                }
                if (o2 > 0)
                {
                    atmosphere.Components[(Chemical.Oxygen, Phase.Gas)] = o2;
                }
                if (so2 > 0)
                {
                    atmosphere.Components[(Chemical.SulphurDioxide, Phase.Gas)] = so2;
                }
                if (waterVapor > 0)
                {
                    atmosphere.Components[(Chemical.Water, Phase.Gas)] = waterVapor;
                }
                if (xe > 0)
                {
                    atmosphere.Components[(Chemical.Xenon, Phase.Gas)] = xe;
                }
                Atmosphere = new Atmosphere(this, atmosphere, (float)Math.Round(Randomizer.Static.NextDouble(25)));
            }
        }

        /// <summary>
        /// Generates an appropriate density for this <see cref="Planetoid"/>.
        /// </summary>
        private protected override void GenerateDensity()
        {
            if (PlanetParams?.Radius.HasValue == true && PlanetParams?.SurfaceGravity.HasValue == true)
            {
                Density = GetDensityFromMassAndShape();
            }
            else
            {
                Density = Math.Round(Randomizer.Static.NextDouble(MinDensity, MaxDensity));
            }
        }

        private void GenerateHydrocarbons()
        {
            // It is presumed that it is statistically likely that the current eon is not the first
            // with life, and therefore that some fossilized hydrocarbon deposits exist.
            var hydrocarbonProportion = (float)Randomizer.Static.NextDouble(0.05, 0.33333);

            AddResource(Chemical.Coal, hydrocarbonProportion, false);

            var petroleumSeed = AddResource(Chemical.Petroleum, hydrocarbonProportion, false);

            // Natural gas is predominantly, though not exclusively, found with petroleum deposits.
            AddResource(Chemical.CarbonMonoxide, hydrocarbonProportion, false, true, petroleumSeed);
        }

        /// <summary>
        /// Generates an appropriate hydrosphere for this <see cref="TerrestrialPlanet"/>.
        /// </summary>
        /// <remarks>
        /// Most terrestrial planets will (at least initially) have a hydrosphere layer (oceans,
        /// icecaps, etc.). This might be removed later, depending on the planet's conditions.
        /// </remarks>
        private protected virtual void GenerateHydrosphere()
        {
            if (CanHaveWater)
            {
                var mass = GenerateHydrosphereMass();

                HydrosphereProportion = (float)(mass / Mass);
                if (!HydrosphereProportion.IsZero())
                {
                    // Surface water is mostly salt water.
                    var saltWater = (float)Math.Round(Randomizer.Static.Normal(0.945, 0.015), 3);
                    _hydrosphere = new Composite(new Dictionary<(Chemical chemical, Phase phase), float>
                    {
                        { (Chemical.Water, Phase.Liquid), 1 - saltWater },
                        { (Chemical.Water_Salt, Phase.Liquid), saltWater },
                    });
                }
            }
            if (_hydrosphere == null)
            {
                _hydrosphere = Material.Empty.GetDeepCopy();
            }
            ClassifyTerrain();
        }

        private double GenerateHydrosphereMass()
        {
            var orderedTiles = Topography.Tiles.OrderBy(t => t.Elevation);
            var oceanMass = 0.0;
            var oceanTileCount = 0;
            var seaLevel = 0f;

            if (PlanetParams?.WaterRatio.HasValue == true)
            {
                if (PlanetParams.WaterRatio.Value <= 0)
                {
                    return 0;
                }

                if (PlanetParams.WaterRatio >= 1)
                {
                    seaLevel = Topography.Tiles.Max(t => t.Elevation) * 1.1f;
                    oceanTileCount = Topography.Tiles.Length;
                    oceanMass = Topography.Tiles.Sum(x => x.Area * (seaLevel - x.Elevation));
                }
                else
                {
                    var targetWaterTileCount = (int)Math.Round(PlanetParams.WaterRatio.Value * Topography.Tiles.Length);
                    var landTiles = orderedTiles.Skip(targetWaterTileCount);
                    var lowestLandElevation = landTiles.FirstOrDefault()?.Elevation;
                    var nextLowestLandElevation = landTiles.SkipWhile(t => t.Elevation == lowestLandElevation).FirstOrDefault()?.Elevation;
                    seaLevel = lowestLandElevation.HasValue
                        ? (nextLowestLandElevation.HasValue
                            ? (lowestLandElevation.Value + nextLowestLandElevation.Value) / 2
                            : lowestLandElevation.Value * 1.1f)
                        : Topography.Tiles.Max(t => t.Elevation) * 1.1f;
                    oceanTileCount = nextLowestLandElevation.HasValue
                        ? orderedTiles.TakeWhile(t => t.Elevation <= lowestLandElevation).Count()
                        : Topography.Tiles.Length;
                    oceanMass = orderedTiles.Take(oceanTileCount).Sum(x => x.Area * (seaLevel - x.Elevation));
                }
            }
            else
            {
                var factor = Mass / 8.75e5;
                var waterMass = Math.Min(factor, Randomizer.Static.Lognormal(0, factor * 4));
                if (waterMass <= 0)
                {
                    return 0;
                }

                while (waterMass > oceanMass)
                {
                    var landTiles = orderedTiles.Skip(oceanTileCount);
                    var lowestLandElevation = landTiles.FirstOrDefault()?.Elevation;
                    var nextLowestLandElevation = landTiles.SkipWhile(t => t.Elevation == lowestLandElevation).FirstOrDefault()?.Elevation;
                    seaLevel = lowestLandElevation.HasValue
                        ? (nextLowestLandElevation.HasValue
                            ? (lowestLandElevation.Value + nextLowestLandElevation.Value) / 2
                            : lowestLandElevation.Value * 1.1f)
                        : Topography.Tiles.Max(t => t.Elevation) * 1.1f;
                    if (!nextLowestLandElevation.HasValue)
                    {
                        break;
                    }
                    oceanTileCount = orderedTiles.TakeWhile(t => t.Elevation <= lowestLandElevation).Count();
                    oceanMass = orderedTiles.Take(oceanTileCount).Sum(x => x.Area * (seaLevel - x.Elevation));
                }
            }

            foreach (var t in Topography.Tiles)
            {
                t.Elevation -= seaLevel;
            }
            foreach (var c in Topography.Corners)
            {
                c.Elevation -= seaLevel;
            }

            return oceanMass;
        }

        private void GenerateITCZ() => HalfITCZWidth = (float)(370400 / Radius);

        /// <summary>
        /// Determines whether this planet is capable of sustaining life, and whether or not it
        /// actually does. If so, the atmosphere may be adjusted.
        /// </summary>
        /// <returns>
        /// True if the atmosphere's greeenhouse potential is adjusted; false if not.
        /// </returns>
        private bool GenerateLife()
        {
            if (!IsHabitable() || Randomizer.Static.NextDouble() > GetChanceOfLife())
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

            // If the habitable zone is a subsurface ocean, no further adjustments occur.
            var hydrosphereSurface = Hydrosphere.GetSurface();
            if (!hydrosphereSurface.ContainsSubstance(Chemical.Water, Phase.Liquid) &&
                !hydrosphereSurface.ContainsSubstance(Chemical.Water_Salt, Phase.Liquid))
            {
                return false;
            }

            GenerateHydrocarbons();

            // If there is a habitable surface layer, it is presumed that an initial population of a
            // cyanobacteria analogue will produce a significant amount of free oxygen, which in turn
            // will transform most CH4 to CO2 and H2O, and also produce an ozone layer.
            var o2 = (float)Randomizer.Static.NextDouble(0.20, 0.25);
            Atmosphere.Composition.AddComponent(Chemical.Oxygen, Phase.Gas, o2);

            // Calculate ozone based on level of free oxygen.
            var o3 = Atmosphere.Composition.GetProportion(Chemical.Oxygen, Phase.Gas) * 4.5e-5f;
            if (Atmosphere.Composition is LayeredComposite lc && lc.Layers.Count < 3)
            {
                Atmosphere.GetTroposphere(); // First ensure troposphere is differentiated.
                lc.CopyLayer(1, 0.01f);
            }
            Atmosphere.Composition.GetSurface().AddComponent(Chemical.Ozone, Phase.Gas, o3);

            // Convert most methane to CO2 and H2O.
            var ch4 = Atmosphere.Composition.GetProportion(Chemical.Methane, Phase.Gas);
            if (ch4 != 0)
            {
                // The levels of CO2 and H2O are not adjusted; it is presumed that the levels already
                // determined for them take the amounts derived from CH4 into account. If either gas
                // is entirely missing, however, it is added.
                var co2 = Atmosphere.Composition.GetProportion(Chemical.CarbonDioxide, Phase.Gas);
                if (co2 == 0)
                {
                    Atmosphere.Composition.AddComponent(Chemical.CarbonDioxide, Phase.Gas, ch4 / 3);
                }

                var waterVapor = Atmosphere.Composition.GetProportion(Chemical.Water, Phase.Gas);
                if (waterVapor == 0)
                {
                    Atmosphere.Composition.AddComponent(Chemical.Water, Phase.Gas, ch4 * 2 / 3);
                }

                Atmosphere.Composition.AddComponent(Chemical.Methane, Phase.Gas, ch4 * 0.001f);

                Atmosphere.ResetGreenhouseFactor();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines whether this <see cref="Planetoid"/> has a strong magnetosphere.
        /// </summary>
        private protected override void GenerateMagnetosphere()
        {
            if (PlanetParams?.HasMagnetosphere.HasValue == true)
            {
                HasMagnetosphere = PlanetParams.HasMagnetosphere.Value;
            }
            else
            {
                base.GenerateMagnetosphere();
            }
        }

        /// <summary>
        /// Generates the mass of this <see cref="TerrestrialPlanet"/>.
        /// </summary>
        private protected double GenerateMass()
        {
            var minMass = MinMass;
            var maxMass = TMath.IsZero(MaxMass) ? null : (double?)MaxMass;

            if (Parent != null && Parent is StarSystem && Position != Vector3.Zero && (Orbit == null || Orbit.OrbitedObject is Star))
            {
                // Stern-Levison parameter for neighborhood-clearing used to determined minimum mass at which
                // the planet would be able to do so at this orbital distance. We set the maximum at two
                // orders of magnitude more than this (planets in our solar system all have masses above
                // 5 orders of magnitude more). Note that since lambda is proportional to the square of mass,
                // it is multiplied by 10 to obtain a difference of 2 orders of magnitude, rather than by 100.
                minMass = Math.Max(minMass, GetSternLevisonLambdaMass() * 10);
                if (minMass > maxMass && maxMass.HasValue)
                {
                    minMass = maxMass.Value; // sanity check; may result in a "planet" which *can't* clear its neighborhood
                }
            }

            if (PlanetParams?.SurfaceGravity.HasValue == true && PlanetParams?.Radius.HasValue == true)
            {
                var mass = GetMassForSurfaceGravity(PlanetParams.SurfaceGravity.Value);
                return Math.Max(minMass, Math.Min(maxMass ?? double.PositiveInfinity, mass));
            }
            else
            {
                return Math.Round(Randomizer.Static.NextDouble(minMass, maxMass ?? minMass));
            }
        }

        private void GenerateOrbit(Orbiter orbitedObject, double semiMajorAxis, float trueAnomaly)
        {
            Orbit.SetOrbit(
                  this,
                  orbitedObject,
                  (1 - Eccentricity) * semiMajorAxis,
                  Eccentricity,
                  (float)Math.Round(Randomizer.Static.NextDouble(0.9), 4),
                  (float)Math.Round(Randomizer.Static.NextDouble(MathConstants.TwoPI), 4),
                  (float)Math.Round(Randomizer.Static.NextDouble(MathConstants.TwoPI), 4),
                  trueAnomaly);
            ResetCachedTemperatures();
        }

        /// <summary>
        /// Determines an orbit for this <see cref="Orbiter"/>.
        /// </summary>
        /// <param name="orbitedObject">The <see cref="Orbiter"/> which is to be orbited.</param>
        public override void GenerateOrbit(Orbiter orbitedObject)
        {
            if (orbitedObject == null)
            {
                return;
            }

            if (PlanetParams?.Eccentricity.HasValue == true)
            {
                Eccentricity = PlanetParams.Eccentricity.Value;
            }

            var ta = (float)Randomizer.Static.NextDouble(MathConstants.TwoPI);
            double? semiMajorAxis = null;

            if (PlanetParams?.RevolutionPeriod.HasValue == true)
            {
                semiMajorAxis = Orbit.GetSemiMajorAxisForPeriod(this, orbitedObject, PlanetParams.RevolutionPeriod.Value);
                var semiLatusRectum = semiMajorAxis * (1 - (Eccentricity * Eccentricity));

                GenerateOrbit(orbitedObject, semiMajorAxis.Value, ta);
            }

            if (orbitedObject is Star star
                && (PlanetParams?.SurfaceTemperature.HasValue == true
                || HabitabilityRequirements?.MinimumTemperature.HasValue == true
                || HabitabilityRequirements?.MaximumTemperature.HasValue == true))
            {
                float targetTemp = 250;
                if (PlanetParams?.SurfaceTemperature.HasValue == true)
                {
                    targetTemp = PlanetParams.SurfaceTemperature.Value;
                }
                else if (HabitabilityRequirements.MinimumTemperature.HasValue)
                {
                    targetTemp = HabitabilityRequirements.MinimumTemperature.Value;
                }
                else
                {
                    targetTemp = Math.Min(GetTempForThinAtmosphere(), HabitabilityRequirements?.MaximumTemperature ?? float.MaxValue) / 2;
                }

                // Convert the target average surface temperature to an estimated target equatorial
                // surface temperature, for which orbit/luminosity requirements can be calculated.
                targetTemp *= 1.06f;
                var targetEquatorialEffectiveTemp = targetTemp;

                // Due to atmospheric effects, the target is likely to be missed considerably on the
                // first attempt, since the calculations for orbit/luminosity will not be able to
                // account for greenhouse warming. By determining the degree of over/undershoot, the
                // target can be adjusted. This is repeated until the real target is approached to
                // within an acceptable tolerance, but not to excess.
                var count = 0;
                var delta = 0.0f;
                do
                {
                    AdjustOrbitForTemperature(star, semiMajorAxis, ta, targetEquatorialEffectiveTemp);

                    if (PlanetParams?.SurfaceTemperature.HasValue == true)
                    {
                        delta = targetTemp - Atmosphere.GetSurfaceTemperatureAverageOrbital();
                    }
                    else
                    {
                        var coolestEquatorialTemp = Atmosphere.GetSurfaceTemperatureAtApoapsis();
                        if (coolestEquatorialTemp < HabitabilityRequirements.MinimumTemperature)
                        {
                            delta = HabitabilityRequirements.MaximumTemperature.Value - coolestEquatorialTemp;
                        }
                        else
                        {
                            var warmestPolarTemp = Atmosphere.GetSurfaceTemperatureAtPeriapsis(true);
                            if (warmestPolarTemp > HabitabilityRequirements.MaximumTemperature)
                            {
                                delta = HabitabilityRequirements.MaximumTemperature.Value - warmestPolarTemp;
                            }
                        }
                    }
                    targetEquatorialEffectiveTemp += delta;
                    count++;
                } while (count < 10 && Math.Abs(delta) > TemperatureErrorTolerance);
            }
        }

        /// <summary>
        /// Determines a rotational period for this <see cref="Planetoid"/>.
        /// </summary>
        private protected override void GenerateRotationalPeriod()
        {
            if (PlanetParams?.RotationalPeriod.HasValue == true)
            {
                RotationalPeriod = Math.Max(0, PlanetParams.RotationalPeriod.Value);
            }
            else
            {
                base.GenerateRotationalPeriod();
            }
        }

        /// <summary>
        /// Generates a new satellite for this <see cref="Planetoid"/> with the specified parameters.
        /// </summary>
        /// <returns>A satellite <see cref="Planetoid"/> with an appropriate orbit.</returns>
        private protected override Planetoid GenerateSatellite(double periapsis, float eccentricity, double maxMass)
        {
            Planetoid satellite = null;
            var chance = Randomizer.Static.NextDouble();

            // If the mass limit allows, there is an even chance that the satellite is a smaller planet.
            if (maxMass > minMassForType && Randomizer.Static.NextBoolean())
            {
                // Select from the standard distribution of types.

                // Planets with very low orbits are lava planets due to tidal stress (plus a small
                // percentage of others due to impact trauma).

                // The maximum mass and density are used to calculate an outer Roche limit (may not
                // be the actual Roche limit for the body which gets generated).
                if (periapsis < GetRocheLimit(maxDensity) * 1.05 || chance <= 0.01)
                {
                    satellite = new LavaPlanet(Parent, maxMass);
                }
                else if (chance <= 0.77) // Most will be standard terrestrial.
                {
                    satellite = new TerrestrialPlanet(Parent, maxMass);
                }
                else
                {
                    satellite = new OceanPlanet(Parent, maxMass);
                }
            }

            // Otherwise, if the mass limit allows, there is an even chance that the satellite is a dwarf planet.
            else if (maxMass > DwarfPlanet.minMassForType && Randomizer.Static.NextBoolean())
            {
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
                    (float)Math.Round(Randomizer.Static.NextDouble(0.5), 4),
                    (float)Math.Round(Randomizer.Static.NextDouble(Math.PI * 2), 4),
                    (float)Math.Round(Randomizer.Static.NextDouble(Math.PI * 2), 4),
                    (float)Math.Round(Randomizer.Static.NextDouble(Math.PI * 2), 4));
            }

            return satellite;
        }

        /// <summary>
        /// Generates the shape of this <see cref="TerrestrialPlanet"/>.
        /// </summary>
        private protected void GenerateShape()
        {
            if (PlanetParams?.Radius.HasValue == true)
            {
                GenerateShape(Math.Max(MinimumRadius, PlanetParams.Radius.Value));
                Substance.Mass = GenerateMass();
            }
            else if (PlanetParams?.SurfaceGravity.HasValue == true)
            {
                GenerateShape(Math.Max(MinimumRadius, Math.Min(GetRadiusForSurfaceGravity(PlanetParams.SurfaceGravity.Value), GetMaxRadius())));
                Substance.Mass = GenerateMass();
            }
            else if (HabitabilityRequirements?.MinimumGravity.HasValue == true
                || HabitabilityRequirements?.MaximumGravity.HasValue == true)
            {
                float maxGravity = 0;
                if (HabitabilityRequirements.MaximumGravity.HasValue)
                {
                    maxGravity = HabitabilityRequirements.MaximumGravity.Value;
                }
                else // Determine the absolute maximum gravity a terrestrial planet could have, before it would become a giant.
                {
                    var maxMass = MaxMassForType ?? maxMassForType;
                    var maxVolume = maxMass / Density;
                    var maxRadius = Math.Pow(maxVolume / MathConstants.FourThirdsPI, 1.0 / 3.0);
                    maxGravity = (float)((ScienceConstants.G * maxMass) / (maxRadius * maxRadius));
                }
                var gravity = (float)Randomizer.Static.NextDouble(HabitabilityRequirements?.MinimumGravity ?? 0, maxGravity);
                GenerateShape(Math.Max(MinimumRadius, Math.Min(GetRadiusForSurfaceGravity(gravity), GetMaxRadius())));
                Substance.Mass = GenerateMass();
            }
            else
            {
                Substance.Mass = GenerateMass();
                base.GenerateShape();
            }
        }

        /// <summary>
        /// Determines the <see cref="CelestialEntity.Substance"/> of this <see cref="CelestialEntity"/>.
        /// </summary>
        private protected override void GenerateSubstance()
        {
            var layers = new List<(IComposition substance, float proportion)>();

            // Iron-nickel core.
            var coreProportion = GetCoreProportion();
            var coreNickel = (float)Math.Round(Randomizer.Static.NextDouble(0.03, 0.15), 4);
            layers.Add((new Composite(new Dictionary<(Chemical chemical, Phase phase), float>
            {
                { (Chemical.Iron, Phase.Solid), 1 - coreNickel },
                { (Chemical.Nickel, Phase.Solid), coreNickel },
            }), coreProportion));

            var crustProportion = GetCrustProportion();

            // Molten rock mantle
            var mantleProportion = 1 - coreProportion - crustProportion;
            layers.Add((new Material(Chemical.Rock, Phase.Liquid), mantleProportion));

            // Rocky crust with trace elements
            // Metal content varies by approx. +/-15% from standard value in a Gaussian distribution.
            var metals = (float)Math.Round(Randomizer.Static.Normal(MetalProportion, 0.05 * MetalProportion), 4);

            var nickel = (float)Math.Round(Randomizer.Static.NextDouble(0.025, 0.075) * metals, 4);
            var aluminum = (float)Math.Round(Randomizer.Static.NextDouble(0.075, 0.225) * metals, 4);

            var titanium = (float)Math.Round(Randomizer.Static.NextDouble(0.05, 0.3) * metals, 4);

            var iron = metals - nickel - aluminum - titanium;

            var copper = titanium > 0 ? (float)Math.Round(Randomizer.Static.NextDouble(titanium), 4) : 0;
            titanium -= copper;

            var lead = titanium > 0 ? (float)Math.Round(Randomizer.Static.NextDouble(titanium), 4) : 0;
            titanium -= lead;

            var uranium = titanium > 0 ? (float)Math.Round(Randomizer.Static.NextDouble(titanium), 4) : 0;
            titanium -= uranium;

            var tin = titanium > 0 ? (float)Math.Round(Randomizer.Static.NextDouble(titanium), 4) : 0;
            titanium -= tin;

            var silver = titanium > 0 ? (float)Math.Round(Randomizer.Static.NextDouble(titanium), 4) : 0;
            titanium -= silver;

            var gold = titanium > 0 ? (float)Math.Round(Randomizer.Static.NextDouble(titanium), 4) : 0;
            titanium -= gold;

            var platinum = titanium > 0 ? (float)Math.Round(Randomizer.Static.NextDouble(titanium), 4) : 0;
            titanium -= platinum;

            var rock = 1 - metals;

            layers.Add((new Composite(new Dictionary<(Chemical chemical, Phase phase), float>
            {
                { (Chemical.Aluminium, Phase.Solid), aluminum },
                { (Chemical.Copper, Phase.Solid), copper },
                { (Chemical.Gold, Phase.Solid), gold },
                { (Chemical.Iron, Phase.Solid), iron },
                { (Chemical.Lead, Phase.Solid), lead },
                { (Chemical.Nickel, Phase.Solid), nickel },
                { (Chemical.Platinum, Phase.Solid), platinum },
                { (Chemical.Rock, Phase.Solid), rock },
                { (Chemical.Silver, Phase.Solid), silver },
                { (Chemical.Tin, Phase.Solid), tin },
                { (Chemical.Titanium, Phase.Solid), titanium },
                { (Chemical.Uranium, Phase.Solid), uranium },
            }), crustProportion));

            Substance = new Substance() { Composition = new LayeredComposite(layers) };
            GenerateShape();
        }

        /// <summary>
        /// Generates a new <see cref="Planetoid.Topography"/> for this <see cref="Planetoid"/>.
        /// </summary>
        private protected override void GenerateTopography()
        {
            var size = PlanetParams?.GridSize ?? WorldGrid.DefaultGridSize;

            if (PlanetParams?.GridTileRadius.HasValue ?? false)
            {
                size = WorldGrid.GetGridSizeForTileRadius(RadiusSquared, PlanetParams.GridTileRadius.Value, PlanetParams.MaxGridSize);
            }
            else if (WorldGrid.DefaultDesiredTileRadius.HasValue)
            {
                size = WorldGrid.GetGridSizeForTileRadius(RadiusSquared, WorldGrid.DefaultDesiredTileRadius.Value, PlanetParams.MaxGridSize);
            }

            Topography = new WorldGrid(this, size);

            AddResources(Substance.Composition.GetSurface()
                .GetChemicals(Phase.Solid).Where(x => x.chemical is Metal)
                .Select(x => (x.chemical, x.proportion, true)));
        }

        /// <summary>
        /// Calculates density from mass and shape.
        /// </summary>
        /// <returns>A density, in kg/m³.</returns>
        public double GetDensityFromMassAndShape()
        {
            if (Mass.IsZero())
            {
                return 0;
            }
            var volume = Substance?.Shape?.Volume ?? 0;
            if (volume.IsZero())
            {
                return double.PositiveInfinity;
            }
            return Mass / volume;
        }

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
        public float GetDistanceForTemperature(Star star, float temperature)
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

            return (float)(Math.Sqrt(star.Luminosity * (1 - Albedo)) / (Math.Pow(temperature, 4) * MathConstants.FourPI * ScienceConstants.sigma * areaRatio));
        }

        internal float GetHydrosphereAtmosphereRatio() => (float)Math.Min(1, (HydrosphereProportion * Mass) / Atmosphere.Mass);

        /// <summary>
        /// Calculates the luminosity (in Watts) the given <see cref="Star"/> would have to be
        /// in order to cause the given effective temperature at the given distance.
        /// </summary>
        /// <remarks>
        /// It is assumed that this <see cref="TerrestrialPlanet"/> has no internal temperature of
        /// its own. The effects of other nearby stars are ignored.
        /// </remarks>
        /// <param name="star">The <see cref="Star"/> for which the calculation is to be made.</param>
        /// <param name="temperature">The desired temperature, in K.</param>
        /// <param name="distance">The desired distance, in meters.</param>
        public double GetLuminosityForTemperature(Star star, float temperature, float distance)
        {
            var areaRatio = 1.0;
            if (RotationalPeriod > 2500)
            {
                if (RotationalPeriod <= 75000)
                {
                    areaRatio = 0.25;
                }
                else if (RotationalPeriod <= 150000)
                {
                    areaRatio = 1.0 / 3.0;
                }
                else if (RotationalPeriod <= 300000)
                {
                    areaRatio = 0.5;
                }
            }

            return (Math.Pow(temperature / Math.Pow(areaRatio, 0.25), 4) * ScienceConstants.FourSigma * MathConstants.FourPI * distance * distance) / (1 - Albedo);
        }

        /// <summary>
        /// Calculates the mass required to produce the given surface gravity, if a shape is already defined.
        /// </summary>
        /// <param name="gravity">The desired surface gravity, in m/s².</param>
        /// <returns>The mass required to produce the given surface gravity, in kg.</returns>
        private double GetMassForSurfaceGravity(float gravity) => (gravity * RadiusSquared) / ScienceConstants.G;

        private Vector3 GetPositionForSeason(int amount, int index)
        {
            var seasonAngle = MathConstants.TwoPI / amount;

            var winterAngle = AxialPrecession + MathConstants.HalfPI;
            if (winterAngle >= MathConstants.TwoPI)
            {
                winterAngle -= MathConstants.TwoPI;
            }

            var seasonTrueAnomaly = Orbit.TrueAnomaly + (winterAngle + (seasonAngle / 2) - new Vector3(Orbit.R0.X, 0, Orbit.R0.Z).GetAngle(Vector3.UnitX));
            if (seasonTrueAnomaly < 0)
            {
                seasonTrueAnomaly += MathConstants.TwoPI;
            }

            seasonTrueAnomaly += seasonAngle * index;
            if (seasonTrueAnomaly >= MathConstants.TwoPI)
            {
                seasonTrueAnomaly -= MathConstants.TwoPI;
            }

            var (r, _) = Orbit.GetStateVectorsForTrueAnomaly((float)seasonTrueAnomaly);
            return r;
        }

        private Season GetPreviousSeason(int amount, int index)
        {
            Season previousSeason = null;
            if (amount != SeasonCount)
            {
                if (index == 0)
                {
                    previousSeason = Seasons?.FirstOrDefault(x => x.Index == SeasonCount - 1);
                }
                else
                {
                    GetSeason(amount, 0);
                }
            }

            if (Seasons == null)
            {
                Seasons = new List<Season>();
            }
            else if (SeasonCount != amount)
            {
                Seasons.Clear();
            }
            SeasonCount = amount;

            if (previousSeason != null)
            {
                return previousSeason;
            }

            previousSeason = index == 0
                ? Seasons?.FirstOrDefault(x => x.Index == SeasonCount - 1)
                : Seasons?.FirstOrDefault(x => x.Index == index - 1);
            if (previousSeason != null)
            {
                return previousSeason;
            }

            if (index == 0)
            {
                return SetClimate();
            }
            else
            {
                for (var i = 0; i < index; i++)
                {
                    previousSeason = Seasons?.FirstOrDefault(x => x.Index == i);
                    if (previousSeason == null)
                    {
                        previousSeason = GetSeason(amount, i);
                    }
                }
            }

            return previousSeason;
        }

        /// <summary>
        /// Calculates the radius required to produce the given surface gravity, if mass is already defined.
        /// </summary>
        /// <param name="gravity">The desired surface gravity, in m/s².</param>
        /// <returns>The radius required to produce the given surface gravity, in meters.</returns>
        private float GetRadiusForSurfaceGravity(float gravity) => (float)Math.Sqrt((Mass * ScienceConstants.G) / gravity);

        /// <summary>
        /// Gets or generates a <see cref="Season"/> for this <see cref="TerrestrialPlanet"/>.
        /// </summary>
        /// <param name="amount">
        /// The number of <see cref="Season"/>s in one year. Must be greater than or equal to 1.
        /// </param>
        /// <param name="index">
        /// The 0-based index of the new <see cref="Season"/> out of one year's worth.
        /// </param>
        /// <returns>A <see cref="Season"/>.</returns>
        public Season GetSeason(int amount, int index)
        {
            if (Orbit == null)
            {
                throw new Exception("Can only generate seasons for planets in orbit.");
            }
            if (amount < 1)
            {
                throw new ArgumentException($"{nameof(amount)} must be greater than or equal to 1.", nameof(amount));
            }
            if (index < 0)
            {
                throw new ArgumentException($"{nameof(index)} must be greater than or equal to 0.", nameof(index));
            }

            Season season;
            if (amount == SeasonCount)
            {
                season = Seasons?.FirstOrDefault(x => x.Index == index);
                if (season != null)
                {
                    return season;
                }
            }

            var position = GetPositionForSeason(amount, index);
            var previousSeason = GetPreviousSeason(amount, index);

            season = new Season(
                this,
                index,
                amount,
                position,
                previousSeason);
            if (Seasons == null)
            {
                Seasons = new List<Season>();
            }
            Seasons.Add(season);
            return season;
        }

        /// <summary>
        /// Gets or generates a set of <see cref="Season"/>s for this <see cref="TerrestrialPlanet"/>.
        /// </summary>
        /// <param name="amount">
        /// The number of <see cref="Season"/>s in one year. Must be greater than or equal to 1.
        /// </param>
        /// <returns>An enumeration of <see cref="Season"/>s.</returns>
        public IEnumerable<Season> GetSeasons(int amount)
        {
            for (var i = 0; i < amount; i++)
            {
                yield return GetSeason(amount, i);
            }
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
        private float GetTempForThinAtmosphere() => (float)((ScienceConstants.TwoG * Mass * 7.0594833834763e-5) / Radius);

        /// <summary>
        /// Determines if this <see cref="TerrestrialPlanet"/> is "habitable," defined as possessing
        /// liquid water. Does not rule out exotic lifeforms which subsist in non-aqueous
        /// environments. Also does not imply habitability by any particular creatures (e.g. humans),
        /// which may also depend on stricter criteria (e.g. atmospheric conditions).
        /// </summary>
        /// <returns>true if this planet fits this minimal definition of "habitable;" false otherwise.</returns>
        public bool IsHabitable() => Hydrosphere.ContainsSubstance(Chemical.Water, Phase.Any) || Hydrosphere.ContainsSubstance(Chemical.Water_Salt, Phase.Liquid);

        /// <summary>
        /// Determines if the planet is habitable by a species with the given requirements. Does not
        /// imply that the planet could sustain a large-scale population in the long-term, only that
        /// a member of the species can survive on the surface without artificial aid.
        /// </summary>
        /// <param name="habitabilityRequirements">The collection of <see cref="TerrestrialPlanets.HabitabilityRequirements"/>.</param>
        /// <param name="reason">
        /// Set to an <see cref="UninhabitabilityReason"/> indicating the reason(s) the planet is uninhabitable.
        /// </param>
        /// <returns>
        /// true if this planet is habitable by a species with the given requirements; false otherwise.
        /// </returns>
        public bool IsHabitable(HabitabilityRequirements habitabilityRequirements, out UninhabitabilityReason reason)
        {
            reason = UninhabitabilityReason.None;

            if (TMath.IsZero(GetChanceOfLife()))
            {
                reason = UninhabitabilityReason.Other;
            }

            if (!IsHabitable())
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
            if (Atmosphere.GetSurfaceTemperatureAtApoapsis() < (habitabilityRequirements.MinimumTemperature ?? 0))
            {
                reason |= UninhabitabilityReason.TooCold;
            }

            // To determine if a planet is too hot, the polar temperature at periapsis is used, since
            // this should be the coldest region at its hottest time.
            if (Atmosphere.GetSurfaceTemperatureAtPeriapsis(true) > (habitabilityRequirements.MaximumTemperature ?? float.PositiveInfinity))
            {
                reason |= UninhabitabilityReason.TooHot;
            }

            if (Atmosphere.AtmosphericPressure < (habitabilityRequirements.MinimumPressure ?? 0))
            {
                reason |= UninhabitabilityReason.LowPressure;
            }

            if (Atmosphere.AtmosphericPressure > (habitabilityRequirements.MaximumPressure ?? float.PositiveInfinity))
            {
                reason |= UninhabitabilityReason.HighPressure;
            }

            if (SurfaceGravity < (habitabilityRequirements.MinimumGravity ?? 0))
            {
                reason |= UninhabitabilityReason.LowGravity;
            }

            if (SurfaceGravity > (habitabilityRequirements.MaximumGravity ?? float.PositiveInfinity))
            {
                reason |= UninhabitabilityReason.HighGravity;
            }

            return (reason == UninhabitabilityReason.None);
        }

        internal override void ResetCachedTemperatures()
        {
            base.ResetCachedTemperatures();
            _atmosphere?.ResetTemperatureDependentProperties();
        }

        private Season SetClimate()
        {
            // A year is pre-generated as a single season, and another as 12 seasons, to prime the
            // algorithms, which produce better values with historical data.

            var position = GetPositionForSeason(1, 0);

            var season = new Season(this, 0, 1, position);

            var seasonAngle = MathConstants.TwoPI / 12;

            var winterAngle = AxialPrecession + MathConstants.HalfPI;
            if (winterAngle >= MathConstants.TwoPI)
            {
                winterAngle -= MathConstants.TwoPI;
            }

            var seasonTrueAnomaly = Orbit.TrueAnomaly + (winterAngle + (seasonAngle / 2) - new Vector3(Orbit.R0.X, 0, Orbit.R0.Z).GetAngle(Vector3.UnitX));
            if (seasonTrueAnomaly < 0)
            {
                seasonTrueAnomaly += MathConstants.TwoPI;
            }

            var seasons = new List<Season>(12);
            for (var i = 0; i < 12; i++)
            {
                seasonTrueAnomaly += seasonAngle * i;
                if (seasonTrueAnomaly >= MathConstants.TwoPI)
                {
                    seasonTrueAnomaly -= MathConstants.TwoPI;
                }
                var (r, _) = Orbit.GetStateVectorsForTrueAnomaly((float)seasonTrueAnomaly);

                season = new Season(this, i, 12, r, season);
                seasons.Add(season);
            }

            for (var i = 0; i < Topography.Tiles.Length; i++)
            {
                Topography.Tiles[i].SetClimate(seasons);
            }

            for (var i = 0; i < Topography.Edges.Length; i++)
            {
                Topography.Edges[i].RiverFlow = new FloatRange(
                    seasons.Min(x => x.EdgeRiverFlows[i]),
                    seasons.Average(x => x.EdgeRiverFlows[i]),
                    seasons.Max(x => x.EdgeRiverFlows[i]));
            }

            return season;
        }

        /// <summary>
        /// Changes the <see cref="WorldGrid.GridSize"/> of this <see cref="Planetoid"/>'s
        /// <see cref="WorldGrid"/>.
        /// </summary>
        /// <param name="gridSize">
        /// The desired <see cref="WorldGrid.GridSize"/> (level of detail). Must be between 0 and
        /// <see cref="WorldGrid.MaxGridSize"/>.
        /// </param>
        /// <param name="preserveShape">
        /// If true, the same random seed will be used for elevation generation as before, resulting
        /// in the same height map (can be used to maintain a similar look when changing <see
        /// cref="WorldGrid.GridSize"/>, rather than an entirely new geography).
        /// </param>
        public override void SetGridSize(short gridSize, bool preserveShape = true)
        {
            Topography.SubdivideGrid(gridSize, preserveShape);
            GenerateHydrosphere();
            Seasons?.Clear();
        }
    }
}
