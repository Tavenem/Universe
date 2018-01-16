using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Troschuetz.Random;
using WorldFoundry.CelestialBodies.Planetoids.Asteroids;
using WorldFoundry.CelestialBodies.Planetoids.Planets.DwarfPlanets;
using WorldFoundry.CelestialBodies.Stars;
using WorldFoundry.Climate;
using WorldFoundry.Extensions;
using WorldFoundry.Orbits;
using WorldFoundry.Space;
using WorldFoundry.Substances;
using WorldFoundry.Utilities;
using WorldFoundry.WorldGrids;

namespace WorldFoundry.CelestialBodies.Planetoids.Planets.TerrestrialPlanets
{
    /// <summary>
    /// A primarily rocky planet, relatively small in comparison to gas and ice giants.
    /// </summary>
    public class TerrestrialPlanet : Planemo
    {
        internal new static string baseTypeName = "Terrestrial Planet";
        /// <summary>
        /// The base name for this type of <see cref="CelestialEntity"/>.
        /// </summary>
        public override string BaseTypeName => baseTypeName;

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

        private static int extremeRotationalPeriod = 22000000;
        private protected override int ExtremeRotationalPeriod => extremeRotationalPeriod;

        private double? _g0MdivR;
        /// <summary>
        /// A pre-calculated value equal to g0 (surface gravity) times the molar mass or air, divided
        /// by the universal gas constant.
        /// </summary>
        internal double G0MdivR
        {
            get => GetProperty(ref _g0MdivR, GenerateSurfaceGravity) ?? 0;
            set => _g0MdivR = value;
        }

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

        private Mixture _hydrosphere;
        /// <summary>
        /// This planet's surface liquids and ices (not necessarily water).
        /// </summary>
        /// <remarks>
        /// Represented as a separate <see cref="Mixture"/> rather than as a top layer of <see
        /// cref="Planetoid.Composition"/> for ease of reference.
        /// </remarks>
        public Mixture Hydrosphere
        {
            get => GetProperty(ref _hydrosphere, GenerateHydrosphere);
            protected set => _hydrosphere = value;
        }

        /// <summary>
        /// Gets the top layer of the <see cref="Hydrosphere"/>, whether that is the <see
        /// cref="Hydrosphere"/> itself (if it is non-layered), or the uppermost layer (if it is).
        /// </summary>
        public Mixture HydrosphereSurface => Hydrosphere.Mixtures?.Count > 0 ? Hydrosphere.GetChildAtLastLayer() : Hydrosphere;

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

        private static int maxRotationalPeriod = 6500000;
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

        private static int minRotationalPeriod = 40000;
        private protected override int MinRotationalPeriod => minRotationalPeriod;

        private static string planemoClassPrefix = "Terrestrial";
        /// <summary>
        /// A prefix to the <see cref="CelestialEntity.TypeName"/> for this class of <see cref="Planemo"/>.
        /// </summary>
        public override string PlanemoClassPrefix => planemoClassPrefix;

        /// <summary>
        /// The parameters specified on creation which control the random generation of this <see
        /// cref="TerrestrialPlanet"/>'s characteristics.
        /// </summary>
        private protected TerrestrialPlanetParams PlanetParams { get; set; }

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
        internal ICollection<Season> Seasons { get; private set; }

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

        private WorldGrid _topography;
        /// <summary>
        /// The <see cref="WorldGrid"/> which describes this <see cref="TerrestrialPlanet"/>'s surface.
        /// </summary>
        public WorldGrid Topography
        {
            get => GetProperty(ref _topography, GenerateTopography);
            private set => _topography = value;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="TerrestrialPlanet"/>.
        /// </summary>
        public TerrestrialPlanet() { }

        /// <summary>
        /// Initializes a new instance of <see cref="TerrestrialPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="TerrestrialPlanet"/> is located.
        /// </param>
        public TerrestrialPlanet(CelestialObject parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="TerrestrialPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="TerrestrialPlanet"/> is located.
        /// </param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="TerrestrialPlanet"/> during random generation, in kg.
        /// </param>
        public TerrestrialPlanet(CelestialObject parent, double maxMass) : base(parent, maxMass) { }

        /// <summary>
        /// Initializes a new instance of <see cref="TerrestrialPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="TerrestrialPlanet"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="TerrestrialPlanet"/>.</param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="TerrestrialPlanet"/> during random generation, in kg.
        /// </param>
        public TerrestrialPlanet(CelestialObject parent, Vector3 position, double maxMass) : base(parent, position, maxMass) { }

        /// <summary>
        /// Initializes a new instance of <see cref="TerrestrialPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see
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
            CelestialObject parent,
            Vector3 position,
            TerrestrialPlanetParams planetParams = null,
            HabitabilityRequirements requirements = null) : base(parent, position)
        {
            HabitabilityRequirements = requirements;
            PlanetParams = planetParams;
        }

        private void AdjustOrbitForTemperature(Star star, ref double? semiMajorAxis, float trueAnomaly, float distance, float targetTemp)
        {
            if (PlanetParams?.RevolutionPeriod.HasValue == true)
            {
                star.Luminosity = GetLuminosityForTemperature(star, targetTemp, distance);
            }
            else
            {
                if (!semiMajorAxis.HasValue)
                {
                    var semiLatusRectum = distance * (1 + (Eccentricity * Math.Cos(trueAnomaly)));
                    semiMajorAxis = semiLatusRectum / (1 - (Eccentricity * Eccentricity));
                }
                GenerateOrbit(star, semiMajorAxis.Value, trueAnomaly, GetDistanceForTemperature(star, targetTemp));
            }
            ResetCachedTemperatures();
            Atmosphere?.ResetPressureDependentProperties();
        }

        private void CalculateGasPhaseMix(Chemical chemical, float surfaceTemp, float polarTemp, ref float hydrosphereAtmosphereRatio)
        {
            // Chemical will boil away.
            if (surfaceTemp > chemical.AntoineMaximumTemperature ||
                (surfaceTemp > chemical.AntoineMinimumTemperature &&
                Atmosphere.AtmosphericPressure > chemical.CalculateVaporPressure(surfaceTemp)))
            {
                float gasProportion = Hydrosphere.GetProportion(chemical, Phase.Any);
                if (gasProportion != 0)
                {
                    SetHydrosphereProportion(chemical, Phase.Any, 0, ref hydrosphereAtmosphereRatio);
                    Hydrosphere.Proportion -= Hydrosphere.Proportion * gasProportion;
                    hydrosphereAtmosphereRatio = GetHydrosphereAtmosphereRatio();
                    Atmosphere.SetProportion(chemical, Phase.Gas, gasProportion * hydrosphereAtmosphereRatio, true);
                    Atmosphere.ResetPressureDependentProperties();
                }
            }
            // If the gas is present in the atmosphere and it will not boil away, it will condense,
            // and may freeze.
            else if (Atmosphere.ContainsSubstance(chemical, Phase.Any))
            {
                CondenseAtmosphere(chemical, surfaceTemp, polarTemp, ref hydrosphereAtmosphereRatio);
            }
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
        private void CalculatePhases(int counter, ref float hydrosphereAtmosphereRatio)
        {
            var surfaceTemp = Atmosphere.GetSurfaceTemperatureAverageOrbital();
            var polarTemp = Atmosphere.GetSurfaceTemperatureAverageOrbital(true);

            CalculateGasPhaseMix(Chemical.Methane, surfaceTemp, polarTemp, ref hydrosphereAtmosphereRatio);

            CalculateGasPhaseMix(Chemical.CarbonMonoxide, surfaceTemp, polarTemp, ref hydrosphereAtmosphereRatio);

            CalculateGasPhaseMix(Chemical.CarbonDioxide, surfaceTemp, polarTemp, ref hydrosphereAtmosphereRatio);

            CalculateGasPhaseMix(Chemical.Nitrogen, surfaceTemp, polarTemp, ref hydrosphereAtmosphereRatio);

            CalculateGasPhaseMix(Chemical.Oxygen, surfaceTemp, polarTemp, ref hydrosphereAtmosphereRatio);

            // No need to check for ozone, since it is only added to
            // atmospheres on planets with liquid surface water, which means
            // temperatures too high for liquid or solid ozone.

            CalculateGasPhaseMix(Chemical.SulphurDioxide, surfaceTemp, polarTemp, ref hydrosphereAtmosphereRatio);

            // Water is handled differently, since the planet may already have surface water.
            if (Hydrosphere.ContainsSubstance(Chemical.Water, Phase.Any)
                || Hydrosphere.ContainsSubstance(Chemical.Water_Salt, Phase.Any)
                || Atmosphere.ContainsSubstance(Chemical.Water, Phase.Any))
            {
                CalculateWaterPhaseMix(surfaceTemp, polarTemp, hydrosphereAtmosphereRatio);
            }

            var oldAlbedo = Albedo;

            // Ices significantly impact albedo.
            var iceAmount = Math.Min(1, HydrosphereSurface.Components
                .Where(x => x.Phase == Phase.Solid)
                .Sum(x => x.Proportion));
            Albedo = (SurfaceAlbedo * (1.0f - iceAmount)) + (0.9f * iceAmount);

            // Clouds also impact albedo.
            float cloudCover = Math.Min(1, Atmosphere.AtmosphericPressure
                * Atmosphere.GetChildAtFirstLayer().Components
                .Where(x => x.Phase == Phase.Solid || x.Phase == Phase.Liquid)
                .Sum(s => s.Proportion) / 100.0f);
            Albedo = (SurfaceAlbedo * (1.0f - cloudCover)) + (0.9f * cloudCover);

            // An albedo change might significantly change surface temperature, which may require a
            // re-calculation (but not too many). 5K is used as the threshold for re-calculation,
            // which may lead to some inaccuracies, but should avoid over-repeating for small changes.
            if (counter < 10 && Albedo != oldAlbedo &&
                Math.Abs(surfaceTemp - Atmosphere.GetSurfaceTemperatureAverageOrbital()) > 5)
            {
                CalculatePhases(counter + 1, ref hydrosphereAtmosphereRatio);
            }
        }

        private void CalculateWaterPhaseMix(float surfaceTemp, float polarTemp, float hydrosphereAtmosphereRatio)
        {
            var water = Hydrosphere.GetProportion(Chemical.Water, Phase.Any, Hydrosphere.Mixtures?.Count > 0);
            var saltWater = Hydrosphere.GetProportion(Chemical.Water_Salt, Phase.Any, Hydrosphere.Mixtures?.Count > 0);
            var totalWater = water + saltWater;

            var waterVapor = Atmosphere.GetProportion(Chemical.Water, Phase.Gas, true);

            var vaporPressure = Chemical.Water.CalculateVaporPressure(surfaceTemp);

            if (surfaceTemp < Chemical.Water.AntoineMinimumTemperature ||
                (surfaceTemp <= Chemical.Water.AntoineMaximumTemperature &&
                Atmosphere.AtmosphericPressure > vaporPressure))
            {
                CondenseWater(surfaceTemp, polarTemp, totalWater, waterVapor, vaporPressure, ref hydrosphereAtmosphereRatio);
            }
            // This indicates that all water will boil off. If this is true,
            // it is presumed that photodissociation will eventually reduce the amount
            // of water vapor to a trace gas (the H2 will be lost due to atmospheric
            // escape, and the oxygen will be lost to surface oxidation).
            else if (totalWater > 0)
            {
                EvaporateWater(ref hydrosphereAtmosphereRatio);
            }

            CheckCO2Reduction(vaporPressure);
        }

        /// <remarks>
        /// At least 1% humidity leads to a reduction of CO2 to a trace gas, by a presumed
        /// carbon-silicate cycle.
        /// </remarks>
        private void CheckCO2Reduction(float vaporPressure)
        {
            if ((Atmosphere.GetChildAtFirstLayer().GetComponent(Chemical.Water, Phase.Gas)?.Proportion ?? 0) *
                Atmosphere.AtmosphericPressure >= 0.01 * vaporPressure)
            {
                ReduceCO2();
            }
        }

        private void ClassifyTerrain()
        {
            foreach (var t in Topography.TileArray)
            {
                var land = 0;
                var water = 0;
                for (int i = 0; i < t.EdgeCount; i++)
                {
                    if (Topography.CornerArray[t.GetCorner(i)].Elevation < 0)
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
            foreach (var c in Topography.CornerArray)
            {
                var land = 0;
                for (int i = 0; i < 3; i++)
                {
                    if (Topography.CornerArray[c.GetCorner(i)].Elevation >= 0)
                    {
                        land++;
                    }
                }
                c.TerrainType = c.Elevation < 0
                    ? (land > 0 ? TerrainType.Coast : TerrainType.Water)
                    : TerrainType.Land;
            }
            foreach (var e in Topography.EdgeArray)
            {
                var type = TerrainType.Land;
                for (int i = 0; i < 2; i++)
                {
                    if (Topography.CornerArray[e.GetCorner(i)].TerrainType != type)
                    {
                        type = i == 0 ? Topography.TileArray[e.GetTile(i)].TerrainType : TerrainType.Coast;
                    }
                }
                e.TerrainType = type;
            }
        }

        private void CondenseAtmosphere(Chemical chemical, float surfaceTemp, float polarTemp, ref float hydrosphereAtmosphereRatio)
        {
            var gasProportion = Atmosphere.GetProportion(chemical, Phase.Any, true);
            float iceProportion = 0;

            // Fully precipitate out of the atmosphere as surface ice.
            // Freezing point at 1 ATM; doesn't change enough at typical terrestrial pressures to make further accuracy necessary.
            if (surfaceTemp <= chemical.MeltingPoint)
            {
                iceProportion = gasProportion / hydrosphereAtmosphereRatio;
                if (iceProportion != 0)
                {
                    SetHydrosphereProportion(chemical, Phase.Solid, iceProportion, ref hydrosphereAtmosphereRatio);
                }
                Atmosphere.RemoveComponent(chemical, Phase.Any, true);
                Atmosphere.RemoveEmptyChildren();
                if (Atmosphere.Mixtures.Count == 0)
                {
                    Atmosphere.Mixtures.Add(new Mixture());
                    Atmosphere.ResetPressureDependentProperties();
                }
            }
            else
            {
                // Create icecaps.
                if (polarTemp <= chemical.MeltingPoint)
                {
                    iceProportion = (gasProportion / hydrosphereAtmosphereRatio) * 0.28f;
                    if (iceProportion != 0)
                    {
                        SetHydrosphereProportion(chemical, Phase.Solid, iceProportion, ref hydrosphereAtmosphereRatio);
                    }
                }

                var troposphere = GetTroposphere();

                // Generate clouds.
                var cloudProportion = gasProportion * 0.2f;
                if (polarTemp <= chemical.MeltingPoint)
                {
                    var halfCloudProportion = cloudProportion / 2;
                    troposphere.AddComponent(chemical, Phase.Liquid, halfCloudProportion);
                    troposphere.AddComponent(chemical, Phase.Solid, halfCloudProportion);
                }
                else
                {
                    troposphere.AddComponent(chemical, Phase.Liquid, cloudProportion);
                }
                Atmosphere.ResetPressureDependentProperties();

                var liquidProportion = (gasProportion / hydrosphereAtmosphereRatio) - iceProportion;
                if (liquidProportion != 0)
                {
                    SetHydrosphereProportion(chemical, Phase.Liquid, liquidProportion, ref hydrosphereAtmosphereRatio);
                }
            }
        }

        private void CondenseWater(float surfaceTemp, float polarTemp, float totalWater, float waterVapor, float vaporPressure, ref float hydrosphereAtmosphereRatio)
        {
            if (surfaceTemp <= Chemical.Water.MeltingPoint) // Below freezing point; add ice.
            {
                CondenseWaterIce(totalWater, waterVapor, ref hydrosphereAtmosphereRatio);
            }
            else // Above freezing point, but also above vapor pressure; add liquid water.
            {
                CondenseWaterLiquid(polarTemp, ref totalWater, waterVapor, hydrosphereAtmosphereRatio);
            }

            // If no water vapor is present in the atmosphere, generate it based on the hydrosphere.
            if (waterVapor == 0)
            {
                var pressureRatio = Math.Max(float.Epsilon, Math.Min(1, vaporPressure / Atmosphere.AtmosphericPressure));
                // This would represent 100% humidity. Since this is the case, in principle, only at
                // the surface of bodies of water, and should decrease exponentially with altitude,
                // an approximation of 25% average humidity overall is used.
                waterVapor = Math.Max(float.Epsilon, totalWater * pressureRatio);
                waterVapor *= 0.25f;
                if (waterVapor != 0)
                {
                    Atmosphere.AddComponent(Chemical.Water, Phase.Gas, waterVapor, true);

                    // Also add a corresponding amount of oxygen, if it's not already present.
                    if (CanHaveOxygen)
                    {
                        Atmosphere.SetProportion(Chemical.Oxygen, Phase.Gas, Math.Max(Atmosphere.GetProportion(Chemical.Oxygen, Phase.Gas, true), (float)Math.Round(waterVapor * 0.0001, 5)), true);
                    }

                    Atmosphere.ResetPressureDependentProperties();
                }
            }

            // Add clouds.
            var clouds = waterVapor * 0.2f;
            if (clouds != 0)
            {
                var troposphere = GetTroposphere();
                if (surfaceTemp <= Chemical.Water.MeltingPoint)
                {
                    troposphere.SetProportion(Chemical.Water, Phase.Solid, clouds);
                }
                else if (polarTemp < Chemical.Water.MeltingPoint)
                {
                    var halfClouds = clouds / 2;
                    troposphere.SetProportion(Chemical.Water, Phase.Liquid, halfClouds);
                    troposphere.SetProportion(Chemical.Water, Phase.Solid, halfClouds);
                }
                else
                {
                    troposphere.SetProportion(Chemical.Water, Phase.Liquid, clouds);
                }

                Atmosphere.ResetPressureDependentProperties();
            }
        }

        private void CondenseWaterIce(float totalWater, float waterVapor, ref float hydrosphereAtmosphereRatio)
        {
            float ice = totalWater;

            // A subsurface liquid ocean may persist if it's deep enough.
            if (Hydrosphere.Proportion >= 0.01)
            {
                ice = (0.01f / Hydrosphere.Proportion) * totalWater;
            }

            // No existing water in hydrosphere; add some condensed ice from atmospheric water vapor.
            if (ice == 0)
            {
                ice = waterVapor / hydrosphereAtmosphereRatio;
            }

            if (ice == 0)
            {
                return;
            }

            if (totalWater != 0) // Change existing water in hydrosphere to ice.
            {
                if (ice < totalWater) // A subsurface ocean is indicated.
                {
                    if ((Hydrosphere.Mixtures?.Count ?? 0) == 0)
                    {
                        Hydrosphere.CopyLayer(0, ice);
                    }
                    else
                    {
                        Hydrosphere.GetChildAtFirstLayer().Proportion = 1 - ice;
                        Hydrosphere.GetChildAtLastLayer().Proportion = ice;
                    }
                }

                // Convert entire hydrosphere surface to ice.
                // Remove any existing water from the hydrosphere.
                HydrosphereSurface.RemoveComponent(Chemical.Water, Phase.Liquid);
                HydrosphereSurface.RemoveComponent(Chemical.Water_Salt, Phase.Liquid);

                // Nothing but water in the former hydrosphere.
                if (HydrosphereSurface.Components.Count == 0)
                {
                    HydrosphereSurface.AddComponent(Chemical.Water, Phase.Solid, 1);
                }
                // Something besides water left in the hydrosphere (other deposited liquids and ices).
                else
                {
                    HydrosphereSurface.AddComponent(Chemical.Water, Phase.Solid, ice);
                }
                hydrosphereAtmosphereRatio = GetHydrosphereAtmosphereRatio();
            }
            // No existing water in hydrosphere.
            else
            {
                SetHydrosphereProportion(Chemical.Water, Phase.Solid, ice, ref hydrosphereAtmosphereRatio);
            }
        }

        private void CondenseWaterLiquid(float polarTemp, ref float totalWater, float waterVapor, float hydrosphereAtmosphereRatio)
        {
            // If the hydrosphere was a surface of ice with a subsurface ocean, melt the surface and
            // return to a single layer.
            if (Hydrosphere.Mixtures?.Count > 0)
            {
                var ice = HydrosphereSurface.GetProportion(Chemical.Water, Phase.Solid);
                HydrosphereSurface.RemoveComponent(Chemical.Water, Phase.Solid);
                SetHydrosphereProportion(Chemical.Water_Salt, Phase.Liquid, ice, ref hydrosphereAtmosphereRatio);

                var saltIce = HydrosphereSurface.GetProportion(Chemical.Water_Salt, Phase.Solid);
                HydrosphereSurface.RemoveComponent(Chemical.Water_Salt, Phase.Solid);
                SetHydrosphereProportion(Chemical.Water_Salt, Phase.Liquid, saltIce, ref hydrosphereAtmosphereRatio);

                Hydrosphere.AbsorbLayers();
            }

            var saltWaterProportion = (float)Math.Round(Randomizer.Static.Normal(0.945, 0.015), 3);
            var liquidWater = totalWater;

            // Create icecaps.
            float iceCaps = 0;
            if (polarTemp <= Chemical.Water.MeltingPoint)
            {
                iceCaps = totalWater * 0.28f;
                SetHydrosphereProportion(Chemical.Water, Phase.Solid, iceCaps * (1 - saltWaterProportion), ref hydrosphereAtmosphereRatio);
                SetHydrosphereProportion(Chemical.Water_Salt, Phase.Solid, iceCaps * saltWaterProportion, ref hydrosphereAtmosphereRatio);
                liquidWater -= iceCaps;
            }

            // If there is no liquid water on the surface, condense from the atmosphere.
            if (liquidWater == 0)
            {
                var addedWater = waterVapor / hydrosphereAtmosphereRatio;
                if (addedWater != 0)
                {
                    SetHydrosphereProportion(Chemical.Water_Salt, Phase.Liquid, addedWater * saltWaterProportion, ref hydrosphereAtmosphereRatio);
                    SetHydrosphereProportion(Chemical.Water, Phase.Liquid, addedWater * (1 - saltWaterProportion), ref hydrosphereAtmosphereRatio);
                }
            }
        }

        private void EvaporateWater(ref float hydrosphereAtmosphereRatio)
        {
            Hydrosphere.AbsorbLayers();
            SetHydrosphereProportion(Chemical.Water, Phase.Any, 0, ref hydrosphereAtmosphereRatio);
            SetHydrosphereProportion(Chemical.Water_Salt, Phase.Any, 0, ref hydrosphereAtmosphereRatio);

            var waterVapor = (float)Math.Round(Randomizer.Static.NextDouble(0.001), 4);
            var o2 = (float)Math.Round(waterVapor * 0.0001, 5);
            foreach (var layer in Atmosphere.Mixtures)
            {
                layer.SetProportion(Chemical.Water, Phase.Gas, Math.Max(layer.GetProportion(Chemical.Water, Phase.Gas), waterVapor));

                // Some is added as Oxygen, due to photodissociation.
                if (CanHaveOxygen)
                {
                    layer.SetProportion(Chemical.Oxygen, Phase.Gas, Math.Max(layer.GetProportion(Chemical.Oxygen, Phase.Gas, true), o2));
                }
            }
            Atmosphere.ResetPressureDependentProperties();
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
                _axialPrecession = (float)Math.Round(Randomizer.Static.NextDouble(Utilities.MathUtil.Constants.TwoPI), 4);
                SetAxialTilt(PlanetParams.AxialTilt.Value);
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

            float hydrosphereAtmosphereRatio = GetHydrosphereAtmosphereRatio();
            // Water may be removed, or if not may remove CO2 from the atmosphere, depending on
            // planetary conditions.
            if (Hydrosphere.ContainsSubstance(Chemical.Water, Phase.Any)
                || Hydrosphere.ContainsSubstance(Chemical.Water_Salt, Phase.Any)
                || Atmosphere.ContainsSubstance(Chemical.Water, Phase.Any))
            {
                // Recalculate temperature based on the new atmosphere.
                surfaceTemp = Atmosphere.GetSurfaceTemperatureAverageOrbital();
                var vaporPressure = Chemical.Water.CalculateVaporPressure(surfaceTemp);
                CheckCO2Reduction(vaporPressure);

                // Recalculate temperatures based on the new atmosphere.
                surfaceTemp = Atmosphere.GetSurfaceTemperatureAverageOrbital();
                var polarTemp = Atmosphere.GetSurfaceTemperatureAverageOrbital(true);

                CalculateWaterPhaseMix(surfaceTemp, polarTemp, hydrosphereAtmosphereRatio);
            }

            GenerateLife();

            foreach (var requirement in Atmosphere.ConvertRequirementsForPressure(HabitabilityRequirements?.AtmosphericRequirements))
            {
                var proportion = Atmosphere.GetProportion(requirement.Chemical, requirement.Phase, true);
                if (proportion < requirement.MinimumProportion)
                {
                    Atmosphere.SetProportion(requirement.Chemical, requirement.Phase, requirement.MinimumProportion, true);
                }
                else if (requirement.MaximumProportion.HasValue && proportion > requirement.MaximumProportion)
                {
                    Atmosphere.SetProportion(requirement.Chemical, requirement.Phase, requirement.MaximumProportion.Value, true);
                }
            }

            CalculatePhases(0, ref hydrosphereAtmosphereRatio);

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
                pressure = (float)((mass * SurfaceGravity) / (1000 * Utilities.MathUtil.Constants.FourPI * RadiusSquared));
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

            Atmosphere = new Atmosphere(this, pressure)
            {
                Mixtures = new HashSet<Mixture>()
            };
            var firstLayer = new Mixture(new MixtureComponent[]
            {
                new MixtureComponent
                {
                    Chemical = Chemical.CarbonDioxide,
                    Phase = Phase.Gas,
                    Proportion = co2,
                },
                new MixtureComponent
                {
                    Chemical = Chemical.Helium,
                    Phase = Phase.Gas,
                    Proportion = he,
                },
                new MixtureComponent
                {
                    Chemical = Chemical.Hydrogen,
                    Phase = Phase.Gas,
                    Proportion = h,
                },
                new MixtureComponent
                {
                    Chemical = Chemical.Nitrogen,
                    Phase = Phase.Gas,
                    Proportion = n2,
                },
            })
            {
                Proportion = 1,
            };
            if (ar > 0)
            {
                firstLayer.Components.Add(new MixtureComponent
                {
                    Chemical = Chemical.Argon,
                    Phase = Phase.Gas,
                    Proportion = ar,
                });
            }
            if (co > 0)
            {
                firstLayer.Components.Add(new MixtureComponent
                {
                    Chemical = Chemical.CarbonMonoxide,
                    Phase = Phase.Gas,
                    Proportion = co,
                });
            }
            if (kr > 0)
            {
                firstLayer.Components.Add(new MixtureComponent
                {
                    Chemical = Chemical.Krypton,
                    Phase = Phase.Gas,
                    Proportion = kr,
                });
            }
            if (ch4 > 0)
            {
                firstLayer.Components.Add(new MixtureComponent
                {
                    Chemical = Chemical.Methane,
                    Phase = Phase.Gas,
                    Proportion = ch4,
                });
            }
            if (o2 > 0)
            {
                firstLayer.Components.Add(new MixtureComponent
                {
                    Chemical = Chemical.Oxygen,
                    Phase = Phase.Gas,
                    Proportion = o2,
                });
            }
            if (so2 > 0)
            {
                firstLayer.Components.Add(new MixtureComponent
                {
                    Chemical = Chemical.SulphurDioxide,
                    Phase = Phase.Gas,
                    Proportion = so2,
                });
            }
            if (waterVapor > 0)
            {
                firstLayer.Components.Add(new MixtureComponent
                {
                    Chemical = Chemical.Water,
                    Phase = Phase.Gas,
                    Proportion = waterVapor,
                });
            }
            if (xe > 0)
            {
                firstLayer.Components.Add(new MixtureComponent
                {
                    Chemical = Chemical.Xenon,
                    Phase = Phase.Gas,
                    Proportion = xe,
                });
            }
            Atmosphere.Mixtures.Add(firstLayer);
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
                Atmosphere = new Atmosphere(this, 0)
                {
                    Mixtures = new HashSet<Mixture>()
                };
            }
            else
            {
                Atmosphere = new Atmosphere(this, (float)Math.Round(Randomizer.Static.NextDouble(25)))
                {
                    Mixtures = new HashSet<Mixture>()
                };
                var firstLayer = new Mixture(new MixtureComponent[]
                {
                    new MixtureComponent
                    {
                        Chemical = Chemical.Helium,
                        Phase = Phase.Gas,
                        Proportion = he,
                    },
                    new MixtureComponent
                    {
                        Chemical = Chemical.Hydrogen,
                        Phase = Phase.Gas,
                        Proportion = h,
                    },
                })
                {
                    Proportion = 1,
                };
                if (ar > 0)
                {
                    firstLayer.Components.Add(new MixtureComponent
                    {
                        Chemical = Chemical.Argon,
                        Phase = Phase.Gas,
                        Proportion = ar,
                    });
                }
                if (co2 > 0)
                {
                    firstLayer.Components.Add(new MixtureComponent
                    {
                        Chemical = Chemical.CarbonDioxide,
                        Phase = Phase.Gas,
                        Proportion = co2,
                    });
                }
                if (co > 0)
                {
                    firstLayer.Components.Add(new MixtureComponent
                    {
                        Chemical = Chemical.CarbonMonoxide,
                        Phase = Phase.Gas,
                        Proportion = co,
                    });
                }
                if (kr > 0)
                {
                    firstLayer.Components.Add(new MixtureComponent
                    {
                        Chemical = Chemical.Krypton,
                        Phase = Phase.Gas,
                        Proportion = kr,
                    });
                }
                if (ch4 > 0)
                {
                    firstLayer.Components.Add(new MixtureComponent
                    {
                        Chemical = Chemical.Methane,
                        Phase = Phase.Gas,
                        Proportion = ch4,
                    });
                }
                if (n2 > 0)
                {
                    firstLayer.Components.Add(new MixtureComponent
                    {
                        Chemical = Chemical.Nitrogen,
                        Phase = Phase.Gas,
                        Proportion = n2,
                    });
                }
                if (o2 > 0)
                {
                    firstLayer.Components.Add(new MixtureComponent
                    {
                        Chemical = Chemical.Oxygen,
                        Phase = Phase.Gas,
                        Proportion = o2,
                    });
                }
                if (so2 > 0)
                {
                    firstLayer.Components.Add(new MixtureComponent
                    {
                        Chemical = Chemical.SulphurDioxide,
                        Phase = Phase.Gas,
                        Proportion = so2,
                    });
                }
                if (waterVapor > 0)
                {
                    firstLayer.Components.Add(new MixtureComponent
                    {
                        Chemical = Chemical.Water,
                        Phase = Phase.Gas,
                        Proportion = waterVapor,
                    });
                }
                if (xe > 0)
                {
                    firstLayer.Components.Add(new MixtureComponent
                    {
                        Chemical = Chemical.Xenon,
                        Phase = Phase.Gas,
                        Proportion = xe,
                    });
                }
                Atmosphere.Mixtures.Add(firstLayer);
            }
        }

        /// <summary>
        /// Determines the composition of this <see cref="Planetoid"/>.
        /// </summary>
        private protected override void GenerateComposition()
        {
            Composition = new Mixture()
            {
                Mixtures = new HashSet<Mixture>(),
            };

            // Iron-nickel core.
            var coreProportion = GetCoreProportion();
            var coreNickel = (float)Math.Round(Randomizer.Static.NextDouble(0.03, 0.15), 4);
            Composition.Mixtures.Add(new Mixture(new MixtureComponent[]
            {
                new MixtureComponent
                {
                    Chemical = Chemical.Iron,
                    Phase = Phase.Solid,
                    Proportion = 1 - coreNickel,
                },
                new MixtureComponent
                {
                    Chemical = Chemical.Nickel,
                    Phase = Phase.Solid,
                    Proportion = coreNickel,
                },
            })
            {
                Proportion = coreProportion,
            });

            var crustProportion = GetCrustProportion();

            // Molten rock mantle
            var mantleProportion = 1 - coreProportion - crustProportion;
            Composition.Mixtures.Add(new Mixture(1, new MixtureComponent[]
            {
                new MixtureComponent
                {
                    Chemical = Chemical.Rock,
                    Phase = Phase.Liquid,
                    Proportion = 1,
                },
            })
            {
                Proportion = mantleProportion,
            });

            // Rocky crust with trace elements
            // Metal content varies by approx. +/-15% from standard value in a Gaussian distribution.
            var metals = (float)Math.Round(Randomizer.Static.Normal(MetalProportion, 0.05 * MetalProportion), 4);

            var nickel = (float)Math.Round(Randomizer.Static.NextDouble(0.025, 0.075) * metals, 4);
            var aluminum = (float)Math.Round(Randomizer.Static.NextDouble(0.075, 0.225) * metals, 4);

            var titanium = (float)Math.Round(Randomizer.Static.NextDouble(0.05, 0.3) * metals, 4);

            var iron = metals - nickel - aluminum - titanium;

            var copper = (float)Math.Round(Randomizer.Static.NextDouble(titanium), 4);
            titanium -= copper;

            var silver = (float)Math.Round(Randomizer.Static.NextDouble(titanium), 4);
            titanium -= silver;

            var gold = (float)Math.Round(Randomizer.Static.NextDouble(titanium), 4);
            titanium -= gold;

            var platinum = (float)Math.Round(Randomizer.Static.NextDouble(titanium), 4);
            titanium -= platinum;

            var rock = 1 - metals;

            Composition.Mixtures.Add(new Mixture(2, new MixtureComponent[]
            {
                new MixtureComponent
                {
                    Chemical = Chemical.Aluminum,
                    Phase = Phase.Solid,
                    Proportion = aluminum,
                },
                new MixtureComponent
                {
                    Chemical = Chemical.Copper,
                    Phase = Phase.Solid,
                    Proportion = copper,
                },
                new MixtureComponent
                {
                    Chemical = Chemical.Gold,
                    Phase = Phase.Solid,
                    Proportion = gold,
                },
                new MixtureComponent
                {
                    Chemical = Chemical.Iron,
                    Phase = Phase.Solid,
                    Proportion = iron,
                },
                new MixtureComponent
                {
                    Chemical = Chemical.Nickel,
                    Phase = Phase.Solid,
                    Proportion = nickel,
                },
                new MixtureComponent
                {
                    Chemical = Chemical.Platinum,
                    Phase = Phase.Solid,
                    Proportion = platinum,
                },
                new MixtureComponent
                {
                    Chemical = Chemical.Rock,
                    Phase = Phase.Solid,
                    Proportion = rock,
                },
                new MixtureComponent
                {
                    Chemical = Chemical.Silver,
                    Phase = Phase.Solid,
                    Proportion = silver,
                },
                new MixtureComponent
                {
                    Chemical = Chemical.Titanium,
                    Phase = Phase.Solid,
                    Proportion = titanium,
                },
            })
            {
                Proportion = crustProportion,
            });
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

                var water = (float)(mass / Mass);
                if (water != 0)
                {
                    // Surface water is mostly salt water.
                    var saltWater = (float)Math.Round(Randomizer.Static.Normal(0.945, 0.015), 3);
                    Hydrosphere = new Mixture(new MixtureComponent[]
                    {
                        new MixtureComponent
                        {
                            Chemical = Chemical.Water,
                            Phase = Phase.Liquid,
                            Proportion = 1 - saltWater,
                        },
                        new MixtureComponent
                        {
                            Chemical = Chemical.Water_Salt,
                            Phase = Phase.Liquid,
                            Proportion = saltWater,
                        },
                    })
                    {
                        Proportion = water,
                    };
                }
            }
            if (_hydrosphere == null)
            {
                Hydrosphere = new Mixture
                {
                    Components = new HashSet<MixtureComponent>(),
                };
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
                    oceanTileCount = Topography.Tiles.Count;
                    oceanMass = Topography.Tiles.Sum(x => x.Area * (seaLevel - x.Elevation));
                }
                else
                {
                    var targetWaterTileCount = (int)Math.Round(PlanetParams.WaterRatio.Value * Topography.Tiles.Count);
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
                        : Topography.Tiles.Count;
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
        private void GenerateLife()
        {
            if (!IsHabitable() || Randomizer.Static.NextDouble() > GetChanceOfLife())
            {
                HasBiosphere = false;
                return;
            }

            // If the planet already has a biosphere, there is nothing left to do.
            if (HasBiosphere)
            {
                return;
            }

            HasBiosphere = true;

            if (!HydrosphereSurface.ContainsSubstance(Chemical.Water, Phase.Liquid) &&
                !HydrosphereSurface.ContainsSubstance(Chemical.Water_Salt, Phase.Liquid))
            {
                return;
            }

            // If there is a habitable surface layer (as opposed to a subsurface ocean), it is
            // presumed that an initial population of a cyanobacteria analogue will produce a
            // significant amount of free oxygen, which in turn will transform most CH4 to CO2 and
            // H2O, and also produce an ozone layer.
            var o2 = (float)Randomizer.Static.NextDouble(0.20, 0.25);
            Atmosphere.AddComponent(Chemical.Oxygen, Phase.Gas, o2, true);

            // Calculate ozone based on level of free oxygen.
            var o3 = Atmosphere.GetProportion(Chemical.Oxygen, Phase.Gas, true) * 4.5e-5f;
            if (Atmosphere.Mixtures.Count < 3)
            {
                GetTroposphere(); // First ensure troposphere is differentiated.
                Atmosphere.CopyLayer(1, 0.01f);
            }
            Atmosphere.GetChildAtLastLayer().SetProportion(Chemical.Ozone, Phase.Gas, o3);

            // Convert most methane to CO2 and H2O.
            var ch4 = Atmosphere.GetProportion(Chemical.Methane, Phase.Gas, true);
            if (ch4 != 0)
            {
                // The levels of CO2 and H2O are not adjusted; it is presumed that the levels already
                // determined for them take the amounts derived from CH4 into account. If either gas
                // is entirely missing, however, it is added.
                var co2 = Atmosphere.GetProportion(Chemical.CarbonDioxide, Phase.Gas, true);
                if (co2 == 0)
                {
                    Atmosphere.AddComponent(Chemical.CarbonDioxide, Phase.Gas, ch4 / 3);
                }

                var waterVapor = Atmosphere.GetProportion(Chemical.Water, Phase.Gas, true);
                if (waterVapor == 0)
                {
                    Atmosphere.AddComponent(Chemical.Water, Phase.Gas, ch4 * 2 / 3);
                }

                Atmosphere.SetProportion(Chemical.Methane, Phase.Gas, ch4 * 0.001f);
            }
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
        /// Generates the <see cref="Mass"/> of this <see cref="Orbiter"/>.
        /// </summary>
        private protected override void GenerateMass()
        {
            var minMass = MinMass;
            double? maxMass = TMath.IsZero(MaxMass) ? null : (double?)MaxMass;

            if (Parent != null && Parent is StarSystem && (Orbit == null || Orbit.OrbitedObject is Star))
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
                Mass = Math.Max(minMass, Math.Min(maxMass ?? double.PositiveInfinity, mass));
            }
            else
            {
                Mass = Math.Round(Randomizer.Static.NextDouble(minMass, maxMass ?? minMass));
            }
        }

        private void GenerateOrbit(Orbiter orbitedObject, double semiMajorAxis, float trueAnomaly, float distance)
            => Orbit.SetOrbit(
                this,
                orbitedObject,
                (1 - Eccentricity) * semiMajorAxis,
                Eccentricity,
                (float)Math.Round(Randomizer.Static.NextDouble(0.9), 4),
                (float)Math.Round(Randomizer.Static.NextDouble(Utilities.MathUtil.Constants.TwoPI), 4),
                (float)Math.Round(Randomizer.Static.NextDouble(Utilities.MathUtil.Constants.TwoPI), 4),
                trueAnomaly);

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

            var ta = (float)Randomizer.Static.NextDouble(Utilities.MathUtil.Constants.TwoPI);
            float distance = 1;

            double? semiMajorAxis = null;
            if (PlanetParams?.RevolutionPeriod.HasValue == true)
            {
                semiMajorAxis = Orbit.GetSemiMajorAxisForPeriod(this, orbitedObject, PlanetParams.RevolutionPeriod.Value);
                var semiLatusRectum = semiMajorAxis * (1 - (Eccentricity * Eccentricity));
                distance = (float)(semiLatusRectum / (1 + (Eccentricity * Math.Cos(ta))));

                GenerateOrbit(orbitedObject, semiMajorAxis.Value, ta, distance);
            }

            if (orbitedObject is Star star
                && (PlanetParams?.SurfaceTemperature.HasValue == true
                || HabitabilityRequirements?.MinimumTemperature.HasValue == true
                || HabitabilityRequirements?.MaximumTemperature.HasValue == true))
            {
                var maxTemp = Math.Min(GetTempForThinAtmosphere(), HabitabilityRequirements?.MaximumTemperature ?? float.MaxValue);

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
                    targetTemp = maxTemp / 2;
                }

                var targetSurfaceTemp = targetTemp;
                var count = 0;
                var delta = 0.0f;
                do
                {
                    AdjustOrbitForTemperature(star, ref semiMajorAxis, ta, distance, targetSurfaceTemp);

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
                    targetSurfaceTemp += delta;
                    count++;
                } while (count < 10 && Math.Abs(delta) > Season.ClimateErrorTolerance);
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
        /// Generates the <see cref="Shape"/> of this <see cref="CelestialEntity"/>.
        /// </summary>
        private protected override void GenerateShape()
        {
            if (PlanetParams?.Radius.HasValue == true)
            {
                GenerateShape(Math.Max(MinimumRadius, PlanetParams.Radius.Value));
            }
            else if (PlanetParams?.SurfaceGravity.HasValue == true)
            {
                GenerateShape(Math.Max(MinimumRadius, Math.Min(GetRadiusForSurfaceGravity(PlanetParams.SurfaceGravity.Value), GetMaxRadius())));
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
                    var maxRadius = Math.Pow(maxVolume / Utilities.MathUtil.Constants.FourThirdsPI, 1.0 / 3.0);
                    maxGravity = (float)((Utilities.Science.Constants.G * maxMass) / (maxRadius * maxRadius));
                }
                var gravity = (float)Randomizer.Static.NextDouble(HabitabilityRequirements?.MinimumGravity ?? 0, maxGravity);
                GenerateShape(Math.Max(MinimumRadius, Math.Min(GetRadiusForSurfaceGravity(gravity), GetMaxRadius())));
            }
            else
            {
                base.GenerateShape();
            }
        }

        /// <summary>
        /// Calculates the average surface gravity of this <see cref="Orbiter"/>, in N.
        /// </summary>
        private protected override void GenerateSurfaceGravity()
        {
            base.GenerateSurfaceGravity();
            G0MdivR = SurfaceGravity * Utilities.Science.Constants.MolarMassOfAirDivUniversalGasConstant;
        }

        /// <summary>
        /// Generates a new <see cref="Topography"/> for this <see cref="TerrestrialPlanet"/>.
        /// </summary>
        /// <param name="size">The grid size (level of detail) for the <see cref="WorldGrid"/>.</param>
        private void GenerateTopography()
        {
            var size = PlanetParams?.GridSize ?? WorldGrid.DefaultGridSize;
            Topography = new WorldGrid(this, size);
        }

        /// <summary>
        /// Calculates density from <see cref="Orbits.Orbiter.Mass"/> and <see cref="CelestialEntity.Shape"/>.
        /// </summary>
        /// <returns>A density, in kg/m³.</returns>
        public double GetDensityFromMassAndShape() => Mass / Shape.GetVolume();

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

            return (float)(Math.Sqrt(star.Luminosity * (1 - Albedo)) / (Math.Pow(temperature, 4) * Utilities.MathUtil.Constants.FourPI * Utilities.Science.Constants.StefanBoltzmannConstant * areaRatio));
        }

        private float GetHydrosphereAtmosphereRatio() => (float)Math.Min(1, (Hydrosphere.Proportion * Mass) / Atmosphere.AtmosphericMass);

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

            return (Math.Pow(temperature / Math.Pow(areaRatio, 0.25), 4) * Utilities.Science.Constants.FourStefanBoltzmannConstant * Utilities.MathUtil.Constants.FourPI * distance * distance) / (1 - Albedo);
        }

        /// <summary>
        /// Calculates the mass required to produce the given surface gravity, if a <see
        /// cref="CelestialEntity.Shape"/> is already defined.
        /// </summary>
        /// <param name="gravity">The desired surface gravity, in m/s².</param>
        /// <returns>The mass required to produce the given surface gravity, in kg.</returns>
        private double GetMassForSurfaceGravity(float gravity) => (gravity * RadiusSquared) / Utilities.Science.Constants.G;

        private Vector3 GetPositionForSeason(int amount, int index)
        {
            var seasonAngle = Utilities.MathUtil.Constants.TwoPI / amount;

            var winterAngle = AxialPrecession + Utilities.MathUtil.Constants.HalfPI;
            if (winterAngle >= Utilities.MathUtil.Constants.TwoPI)
            {
                winterAngle -= Utilities.MathUtil.Constants.TwoPI;
            }

            var seasonTrueAnomaly = Orbit.TrueAnomaly + (winterAngle + (seasonAngle / 2) - new Vector3(Orbit.R0X, 0, Orbit.R0Z).GetAngle(Vector3.UnitX));
            if (seasonTrueAnomaly < 0)
            {
                seasonTrueAnomaly += Utilities.MathUtil.Constants.TwoPI;
            }

            seasonTrueAnomaly += seasonAngle * index;
            if (seasonTrueAnomaly >= Utilities.MathUtil.Constants.TwoPI)
            {
                seasonTrueAnomaly -= Utilities.MathUtil.Constants.TwoPI;
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
                Seasons = new HashSet<Season>();
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
                for (int i = 0; i < index; i++)
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
        /// Calculates the radius required to produce the given surface gravity, if <see
        /// cref="Orbits.Orbiter.Mass"/> is already defined.
        /// </summary>
        /// <param name="gravity">The desired surface gravity, in m/s².</param>
        /// <returns>The radius required to produce the given surface gravity, in meters.</returns>
        private float GetRadiusForSurfaceGravity(float gravity) => (float)Math.Sqrt((Mass * Utilities.Science.Constants.G) / gravity);

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
                Seasons = new HashSet<Season>();
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
            for (int i = 0; i < amount; i++)
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
        private float GetTempForThinAtmosphere() => (float)((Utilities.Science.Constants.TwoG * Mass * 7.0594833834763e-5) / Radius);

        /// <summary>
        /// Gets the troposphere of this <see cref="TerrestrialPlanet"/>'s <see cref="Atmosphere"/>.
        /// </summary>
        /// <returns>The troposphere of this <see cref="TerrestrialPlanet"/>'s <see cref="Atmosphere"/>.</returns>
        /// <remarks>
        /// If the <see cref="Atmosphere"/> doesn't yet have differentiated layers, they are first
        /// separated before returning the lowest layer as the troposphere.
        /// </remarks>
        private Mixture GetTroposphere()
        {
            var troposphere = Atmosphere.GetChildAtFirstLayer();

            // Separate troposphere from upper atmosphere if undifferentiated.
            if (Atmosphere.Mixtures.Count == 1)
            {
                Atmosphere.CopyLayer(0, 0.2f);
            }

            return troposphere;
        }

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

        private void ReduceCO2()
        {
            var co2 = Atmosphere.GetProportion(Chemical.CarbonDioxide, Phase.Gas, true);
            if (co2 < 1.0e-3)
            {
                return;
            }

            foreach (var layer in Atmosphere.Mixtures.Where(x =>
                x.ContainsSubstance(Chemical.CarbonDioxide, Phase.Gas)))
            {
                co2 = (float)Randomizer.Static.NextDouble(1.5e-5, 1.0e-3);
                var n2 = layer.GetProportion(Chemical.Nitrogen, Phase.Gas) + layer.GetProportion(Chemical.CarbonDioxide, Phase.Any) - co2;
                layer.RemoveComponent(Chemical.CarbonDioxide, Phase.Liquid);
                layer.RemoveComponent(Chemical.CarbonDioxide, Phase.Solid);
                layer.SetProportion(Chemical.CarbonDioxide, Phase.Gas, co2);

                // Replace the missing CO2 with inert gases.
                // Some portion of the N2 may be Ar instead.
                var ar = (float)Math.Max(layer.GetProportion(Chemical.Argon, Phase.Gas), n2 * Randomizer.Static.NextDouble(-0.02, 0.04));
                layer.SetProportion(Chemical.Argon, Phase.Gas, ar);
                n2 -= ar;

                // An even smaller fraction may be Kr.
                var kr = (float)Math.Max(layer.GetProportion(Chemical.Krypton, Phase.Gas), n2 * Randomizer.Static.NextDouble(-2.5e-4, 5.0e-4));
                layer.SetProportion(Chemical.Krypton, Phase.Gas, kr);
                n2 -= kr;

                // An even smaller fraction may be Xe or Ne.
                var xe = (float)Math.Max(layer.GetProportion(Chemical.Xenon, Phase.Gas), n2 * Randomizer.Static.NextDouble(-1.8e-5, 3.5e-5));
                layer.SetProportion(Chemical.Xenon, Phase.Gas, xe);
                n2 -= xe;

                var ne = (float)Math.Max(layer.GetProportion(Chemical.Neon, Phase.Gas), n2 * Randomizer.Static.NextDouble(-1.8e-5, 3.5e-5));
                layer.SetProportion(Chemical.Neon, Phase.Gas, ne);
                n2 -= ne;

                layer.SetProportion(Chemical.Nitrogen, Phase.Gas, n2);
            }

            // Reduce the pressure to reflect the sequestration (unless it's being forced to a specific value).
            if (PlanetParams?.AtmosphericPressure.HasValue != true && HabitabilityRequirements == null)
            {
                Atmosphere.AtmosphericPressure -= Atmosphere.AtmosphericPressure * co2;
            }
            Atmosphere.ResetPressureDependentProperties();
        }

        /// <summary>
        /// Sets the atmospheric pressure of this <see cref="TerrestrialPlanet"/>.
        /// </summary>
        /// <param name="value">A pressure, in kPa.</param>
        public void SetAtmosphericPressure(float value)
        {
            if (PlanetParams == null)
            {
                PlanetParams = new TerrestrialPlanetParams();
            }
            PlanetParams.AtmosphericPressure = value;
            GenerateAtmosphere();
            Seasons?.Clear();
        }

        /// <summary>
        /// Sets the <see cref="AxialTilt"/> of this <see cref="Planetoid"/>.
        /// </summary>
        /// <param name="value">
        /// An angle from the Y-axis (or the orbital inclination, if in orbit), in radians.
        /// </param>
        public override void SetAxialTilt(float value)
        {
            base.SetAxialTilt(value);
            Seasons?.Clear();
        }

        private Season SetClimate()
        {
            // A year is pre-generated as a single season, and another as 12 seasons, to prime the
            // algorithms, which produce better values with historical data.

            var position = GetPositionForSeason(1, 0);

            var season = new Season(this, 0, 1, position);

            var seasonAngle = Utilities.MathUtil.Constants.TwoPI / 12;

            var winterAngle = AxialPrecession + Utilities.MathUtil.Constants.HalfPI;
            if (winterAngle >= Utilities.MathUtil.Constants.TwoPI)
            {
                winterAngle -= Utilities.MathUtil.Constants.TwoPI;
            }

            var seasonTrueAnomaly = Orbit.TrueAnomaly + (winterAngle + (seasonAngle / 2) - new Vector3(Orbit.R0X, 0, Orbit.R0Z).GetAngle(Vector3.UnitX));
            if (seasonTrueAnomaly < 0)
            {
                seasonTrueAnomaly += Utilities.MathUtil.Constants.TwoPI;
            }

            var seasons = new List<Season>(12);
            for (int i = 0; i < 12; i++)
            {
                seasonTrueAnomaly += seasonAngle * i;
                if (seasonTrueAnomaly >= Utilities.MathUtil.Constants.TwoPI)
                {
                    seasonTrueAnomaly -= Utilities.MathUtil.Constants.TwoPI;
                }
                var (r, _) = Orbit.GetStateVectorsForTrueAnomaly((float)seasonTrueAnomaly);

                season = new Season(this, i, 12, r, season);
                seasons.Add(season);
            }

            for (int i = 0; i < Topography.TileArray.Length; i++)
            {
                Topography.TileArray[i].SetClimate(
                    seasons.Average(s => s.tileClimateArray[i].Temperature),
                    seasons.Sum(s => s.tileClimateArray[i].Precipitation));
            }
            Topography.UpdateCollectionsFromArrays();

            return season;
        }

        /// <summary>
        /// Changes the <see cref="WorldGrid.GridSize"/> of this <see cref="TerrestrialPlanet"/>'s
        /// <see cref="WorldGrid"/>.
        /// </summary>
        /// <param name="gridSize">The desired <see cref="WorldGrid.GridSize"/> (level of detail).</param>
        /// <param name="preserveShape">
        /// If true, the same random seed will be used for elevation generation as before, resulting
        /// in the same height map (can be used to maintain a similar look when changing <see
        /// cref="WorldGrid.GridSize"/>, rather than an entirely new geography).
        /// </param>
        public void SetGridSize(short gridSize, bool preserveShape = true)
        {
            Topography.SubdivideGrid(gridSize, preserveShape);
            GenerateHydrosphere();
            Seasons?.Clear();
        }

        private void SetHydrosphereProportion(Chemical chemical, Phase phase, float proportion, ref float hydrosphereAtmosphereRatio)
        {
            var newTotalProportion = Hydrosphere.Mixtures?.Count > 0 ? HydrosphereSurface.Proportion * proportion : proportion;
            Hydrosphere.Proportion += Hydrosphere.Proportion * (newTotalProportion - Hydrosphere.GetProportion(chemical, phase, Hydrosphere.Mixtures?.Count > 0));
            HydrosphereSurface.SetProportion(chemical, phase, proportion);
            hydrosphereAtmosphereRatio = GetHydrosphereAtmosphereRatio();
        }

        /// <summary>
        /// Sets the radius of this <see cref="TerrestrialPlanet"/>.
        /// </summary>
        /// <param name="radius">A radius, in meters.</param>
        public void SetRadius(int radius)
        {
            GenerateShape(Math.Max(MinimumRadius, Math.Min(PlanetParams.Radius.Value, GetMaxRadius())));
            Mass = Shape.GetVolume() * Density;
            GenerateSurfaceGravity();
            Seasons?.Clear();
        }

        /// <summary>
        /// Sets the revolution period of this <see cref="TerrestrialPlanet"/>.
        /// </summary>
        /// <param name="period">
        /// A revolution period, in seconds. Negative values will be treated as 0.
        /// </param>
        /// <remarks>
        /// Has no immediate effect if the <see cref="TerrestrialPlanet"/> is not already in orbit,
        /// but if an orbit is later generated, the requested period will be honored.
        /// </remarks>
        public void SetRevolutionPeriod(double period)
        {
            if (PlanetParams == null)
            {
                PlanetParams = new TerrestrialPlanetParams();
            }
            PlanetParams.RevolutionPeriod = Math.Max(0, period);

            if (Orbit != null)
            {
                var semiMajorAxis = Orbit.GetSemiMajorAxisForPeriod(this, Orbit.OrbitedObject, PlanetParams.RevolutionPeriod.Value);
                var semiLatusRectum = semiMajorAxis * (1 - (Eccentricity * Eccentricity));
                var ta = (float)Randomizer.Static.NextDouble(Utilities.MathUtil.Constants.TwoPI);
                var distance = (float)(semiLatusRectum / (1 + (Eccentricity * Math.Cos(ta))));
                GenerateOrbit(Orbit.OrbitedObject, semiMajorAxis, ta, distance);
            }

            GenerateAtmosphere();
            Seasons?.Clear();
        }

        /// <summary>
        /// Sets the <see cref="RotationalPeriod"/> of this <see cref="Planetoid"/>.
        /// </summary>
        /// <param name="period">
        /// A rotational period, in seconds. Negative values will be treated as 0.
        /// </param>
        public override void SetRotationalPeriod(double period)
        {
            base.SetRotationalPeriod(period);

            Topography?.SetCoriolisCoefficients();
            Topography?.UpdateCollectionsFromArrays();
            GenerateAtmosphere();
            Seasons?.Clear();
        }

        /// <summary>
        /// Sets the ratio of water to land on the surface of this <see cref="TerrestrialPlanet"/>.
        /// </summary>
        /// <param name="value">A ratio: 0 indicates no water; 1 indicates complete coverage.</param>
        public void SetWaterRatio(float value)
        {
            if (PlanetParams == null)
            {
                PlanetParams = new TerrestrialPlanetParams();
            }
            PlanetParams.WaterRatio = value;
            GenerateHydrosphere();
            Seasons?.Clear();
        }
    }
}
