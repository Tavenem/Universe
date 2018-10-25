using MathAndScience;
using MathAndScience.Numerics;
using MathAndScience.Shapes;
using Substances;
using System;
using System.Collections.Generic;
using System.Linq;
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
        /// <summary>
        /// The radius of the maximum space required by this type of <see cref="CelestialEntity"/>,
        /// in meters.
        /// </summary>
        public const double Space = 1.75e7;

        private const double TemperatureErrorTolerance = 0.5;
        private const double ThirtySixthPI = Math.PI / 36;

        /// <summary>
        /// Hadley values are a pure function of latitude, and do not vary with any property of the
        /// planet, atmosphere, or season. Since the calculation is relatively expensive, retrieved
        /// values can be stored for the lifetime of the program for future retrieval for the same
        /// (or very similar) location.
        /// </summary>
        private static readonly Dictionary<double, double> HadleyValues = new Dictionary<double, double>();

        private static readonly double LowTemp = Chemical.Water.MeltingPoint - 16;

        private readonly Lazy<List<Season>> _seasons = new Lazy<List<Season>>();

        private bool _climateSet;
        private double? _gravity;

        internal static bool _canHaveOxygen = true;
        /// <summary>
        /// Used to allow or prevent oxygen in the atmosphere of a terrestrial planet.
        /// </summary>
        /// <remarks>
        /// True by default, but subclasses may hide this with false when their particular natures
        /// make the presence of significant amounts of oxygen impossible.
        /// </remarks>
        protected virtual bool CanHaveOxygen => _canHaveOxygen;

        internal static bool _canHaveWater = true;
        /// <summary>
        /// Used to allow or prevent water in the composition and atmosphere of a terrestrial planet.
        /// </summary>
        /// <remarks>
        /// True by default, but subclasses may hide this with false when their particular natures
        /// make the presence of significant amounts of water impossible.
        /// </remarks>
        protected virtual bool CanHaveWater => _canHaveWater;

        private const int _extremeRotationalPeriod = 22000000;
        private protected override int ExtremeRotationalPeriod => _extremeRotationalPeriod;

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
            get
            {
                if (_hydrosphere == null)
                {
                    GenerateHydrosphere();
                    if (_hydrosphere == null)
                    {
                        _hydrosphere = Material.Empty();
                    }
                }
                return _hydrosphere;
            }
            internal set => _hydrosphere = value;
        }

        /// <summary>
        /// The proportion (by mass) of the <see cref="TerrestrialPlanet"/> which is comprised by its
        /// <see cref="Hydrosphere"/>.
        /// </summary>
        public double HydrosphereProportion { get; internal set; }

        internal static double _maxDensity = 6000;
        private protected virtual double MaxDensity => _maxDensity;

        internal static double _maxMassForType = 6.0e25;
        /// <summary>
        /// The maximum mass allowed for this type of <see cref="Planetoid"/> during random
        /// generation, in kg. Null indicates no maximum.
        /// </summary>
        /// <remarks>
        /// At around this limit the planet will have sufficient mass to retain hydrogen, and become
        /// a giant.
        /// </remarks>
        internal override double? MaxMassForType => _maxMassForType;

        private const int _maxRotationalPeriod = 6500000;
        private protected override int MaxRotationalPeriod => _maxRotationalPeriod;

        internal static double _metalProportion = 0.05;
        /// <summary>
        /// Used to set the proportionate amount of metal in the composition of a terrestrial planet.
        /// </summary>
        private protected virtual double MetalProportion => _metalProportion;

        internal static double _minDensity = 3750;
        private protected virtual double MinDensity => _minDensity;

        internal static double _minMassForType = 2.0e22;
        /// <summary>
        /// The minimum mass allowed for this type of <see cref="Planetoid"/> during random
        /// generation, in kg. Null indicates a minimum of 0.
        /// </summary>
        /// <remarks>
        /// An arbitrary limit separating rogue dwarf planets from rogue planets. Within orbital
        /// systems, a calculated value for clearing the neighborhood is used instead.
        /// </remarks>
        internal override double? MinMassForType => _minMassForType;

        private const int _minRotationalPeriod = 40000;
        private protected override int MinRotationalPeriod => _minRotationalPeriod;

        private const string _planemoClassPrefix = "Terrestrial";
        /// <summary>
        /// A prefix to the <see cref="CelestialEntity.TypeName"/> for this class of <see cref="Planemo"/>.
        /// </summary>
        public override string PlanemoClassPrefix => _planemoClassPrefix;

        /// <summary>
        /// The parameters which control the random generation of this <see
        /// cref="TerrestrialPlanet"/>'s characteristics.
        /// </summary>
        public TerrestrialPlanetParams PlanetParams { get; set; }

        internal static new double _ringChance = 10;
        /// <summary>
        /// The chance that this <see cref="Planemo"/> will have rings, as a rate between 0.0 and 1.0.
        /// </summary>
        /// <remarks>
        /// There is a low chance of most planets having substantial rings; 10 for <see
        /// cref="TerrestrialPlanet"/>s.
        /// </remarks>
        protected override double RingChance => _ringChance;

        private double? _surfaceAlbedo;
        /// <summary>
        /// Since the total albedo of a terrestrial planet may change based on surface ice and cloud
        /// cover, the base surface albedo is maintained separately.
        /// </summary>
        public double SurfaceAlbedo
        {
            get
            {
                if (!_surfaceAlbedo.HasValue)
                {
                    GenerateAlbedo();
                }
                return _surfaceAlbedo ?? 0;
            }
            internal set => _surfaceAlbedo = value;
        }

        /// <summary>
        /// Any <see cref="TerrestrialPlanets.HabitabilityRequirements"/> specified for this <see
        /// cref="TerrestrialPlanet"/>.
        /// </summary>
        private protected HabitabilityRequirements? HabitabilityRequirements { get; set; }

        private FastNoise _noise1;
        private FastNoise Noise1 => _noise1 ?? (_noise1 = new FastNoise(_seed4, 0.01, FastNoise.NoiseType.SimplexFractal, octaves: 3));

        private FastNoise _noise2;
        private FastNoise Noise2 => _noise2 ?? (_noise2 = new FastNoise(_seed5, 0.004, FastNoise.NoiseType.Simplex));

        /// <summary>
        /// Initializes a new instance of <see cref="TerrestrialPlanet"/>.
        /// </summary>
        public TerrestrialPlanet() { }

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
            HabitabilityRequirements? requirements = null) : base(parent, position)
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
            TerrestrialPlanet planet = null;
            var sanityCheck = 0;
            do
            {
                planet = new TerrestrialPlanet(
                    star?.Parent,
                    Vector3.Zero,
                    planetParams ?? TerrestrialPlanetParams.FromDefaults(),
                    TerrestrialPlanets.HabitabilityRequirements.HumanHabitabilityRequirements);
                planet.GenerateOrbit(star);

                sanityCheck++;
                if (planet.IsHabitable(TerrestrialPlanets.HabitabilityRequirements.HumanHabitabilityRequirements, out var reason))
                {
                    break;
                }
                else if (sanityCheck < 100)
                {
                    planet._location = null;
                }
            } while (sanityCheck <= 100);
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
            StarSystem system = null;
            do
            {
                system = galaxy?.GenerateChild(new ChildDefinition(typeof(StarSystem), StarSystem.Space, 1, typeof(Star), SpectralClass.G, LuminosityClass.V)) as StarSystem;
            } while (system.Stars.Count > 1); // Prevent binary systems, which will interfere with the temperature-balancing logic.
            var star = system?.Stars.FirstOrDefault();
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
            var gsc = universe?.GenerateChild(new ChildDefinition(typeof(GalaxySupercluster), GalaxySupercluster.Space, 1)) as GalaxySupercluster;
            var gc = gsc?.GenerateChild(new ChildDefinition(typeof(GalaxyCluster), GalaxyCluster.Space, 1)) as GalaxyCluster;
            var gg = gc?.GenerateChild(new ChildDefinition(typeof(GalaxyGroup), GalaxyGroup.Space, 1)) as GalaxyGroup;
            gg?.PrepopulateRegion();
            var gsg = gg?.Children.FirstOrDefault(x => x is GalaxySubgroup) as GalaxySubgroup;
            return DefaultHumanPlanetForGalaxy(gsg?.MainGalaxy, planetParams);
        }

        /// <summary>
        /// A shortcut constructor: generates a terrestrial planet with default parameters and human
        /// habiltability requirements orbiting a Sol-like star in a spiral galaxy in a new universe.
        /// </summary>
        /// <param name="planetParams">Any parameters which specify the conditions of the planet to be generated.</param>
        /// <returns>A human-habitable planet with default parameters.</returns>
        public static TerrestrialPlanet DefaultHumanPlanetNewUniverse(TerrestrialPlanetParams planetParams = null)
            => DefaultHumanPlanetForUniverse(new Universe(), planetParams);

        private static IComposition ReduceCO2(IComposition composition)
        {
            var co2 = Randomizer.Instance.NextDouble(1.5e-5, 1.0e-3);

            // Replace most of the CO2 with inert gases.
            var n2 = composition.GetProportion(Chemical.Nitrogen, Phase.Gas) + composition.GetProportion(Chemical.CarbonDioxide, Phase.Any) - co2;
            var result = composition.RemoveComponent(Chemical.CarbonDioxide, Phase.Liquid);
            result = result.RemoveComponent(Chemical.CarbonDioxide, Phase.Solid);
            result = result.AddComponent(Chemical.CarbonDioxide, Phase.Gas, co2);

            // Some portion of the N2 may be Ar instead.
            var ar = Math.Max(result.GetProportion(Chemical.Argon, Phase.Gas), n2 * Randomizer.Instance.NextDouble(-0.02, 0.04));
            result = result.AddComponent(Chemical.Argon, Phase.Gas, ar);
            n2 -= ar;

            // An even smaller fraction may be Kr.
            var kr = Math.Max(result.GetProportion(Chemical.Krypton, Phase.Gas), n2 * Randomizer.Instance.NextDouble(-2.5e-4, 5.0e-4));
            result = result.AddComponent(Chemical.Krypton, Phase.Gas, kr);
            n2 -= kr;

            // An even smaller fraction may be Xe or Ne.
            var xe = Math.Max(result.GetProportion(Chemical.Xenon, Phase.Gas), n2 * Randomizer.Instance.NextDouble(-1.8e-5, 3.5e-5));
            result = result.AddComponent(Chemical.Xenon, Phase.Gas, xe);
            n2 -= xe;

            var ne = Math.Max(result.GetProportion(Chemical.Neon, Phase.Gas), n2 * Randomizer.Instance.NextDouble(-1.8e-5, 3.5e-5));
            result = result.AddComponent(Chemical.Neon, Phase.Gas, ne);
            n2 -= ne;

            return result.AddComponent(Chemical.Nitrogen, Phase.Gas, n2);
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

            var ta = Randomizer.Instance.NextDouble(MathConstants.TwoPI);
            double? semiMajorAxis = null;

            if (PlanetParams?.RevolutionPeriod.HasValue == true)
            {
                semiMajorAxis = Orbit.GetSemiMajorAxisForPeriod(this, orbitedObject, PlanetParams.RevolutionPeriod.Value);
                GenerateOrbit(orbitedObject, semiMajorAxis.Value, ta);
            }

            if (orbitedObject is Star star
                && (PlanetParams?.SurfaceTemperature.HasValue == true
                || HabitabilityRequirements?.MinimumTemperature.HasValue == true
                || HabitabilityRequirements?.MaximumTemperature.HasValue == true))
            {
                var targetTemp = 250.0;
                if (PlanetParams?.SurfaceTemperature.HasValue == true)
                {
                    targetTemp = PlanetParams.SurfaceTemperature.Value;
                }
                else if (HabitabilityRequirements?.MinimumTemperature.HasValue == true)
                {
                    targetTemp = HabitabilityRequirements.Value.MinimumTemperature.Value;
                }
                else
                {
                    targetTemp = Math.Min(GetTempForThinAtmosphere(), HabitabilityRequirements?.MaximumTemperature ?? double.MaxValue) / 2;
                }

                // Convert the target average surface temperature to an estimated target equatorial
                // surface temperature, for which orbit/luminosity requirements can be calculated.
                var targetEquatorialEffectiveTemp = targetTemp * 1.0275;
                // Use the typical average elevation to determine average surface
                // temperature, since the average temperature at sea level is not the same
                // as the average overall surface temperature.
                var avgElevation = Terrain.MaxElevation * 0.07;
                var targetEffectiveTemp = targetEquatorialEffectiveTemp + (avgElevation * LapseRateDry);
                var currentTargetTemp = targetEffectiveTemp;

                // Due to atmospheric effects, the target is likely to be missed considerably on the
                // first attempt, since the calculations for orbit/luminosity will not be able to
                // account for greenhouse warming. By determining the degree of over/undershoot, the
                // target can be adjusted. This is repeated until the real target is approached to
                // within an acceptable tolerance, but not to excess.
                var count = 0;
                var prevDelta = 0.0;
                var delta = 0.0;
                do
                {
                    prevDelta = delta;
                    AdjustOrbitForTemperature(star, semiMajorAxis, ta, currentTargetTemp);

                    if (PlanetParams?.SurfaceTemperature.HasValue == true)
                    {
                        delta = targetEffectiveTemp - GetTemperatureAtElevation(AverageSurfaceTemperature, avgElevation);
                    }
                    else
                    {
                        var coolestEquatorialTemp = GetMinEquatorTemperature();
                        if (coolestEquatorialTemp < HabitabilityRequirements?.MinimumTemperature)
                        {
                            delta = HabitabilityRequirements.Value.MaximumTemperature.HasValue
                                ? HabitabilityRequirements.Value.MaximumTemperature.Value - coolestEquatorialTemp
                                : HabitabilityRequirements.Value.MinimumTemperature.Value - coolestEquatorialTemp;
                        }
                        else
                        {
                            var warmestPolarTemp = GetMaxPolarTemperature();
                            if (warmestPolarTemp > HabitabilityRequirements?.MaximumTemperature)
                            {
                                delta = HabitabilityRequirements.Value.MaximumTemperature.Value - warmestPolarTemp;
                            }
                        }
                    }
                    // If the corrections are resulting in a runaway drift in the wrong direction,
                    // reset by deleting the atmosphere and targeting the original temp; do not
                    // reset the count, to prevent this from repeating indefinitely.
                    if (prevDelta != 0
                        && (delta >= 0) == (prevDelta >= 0)
                        && Math.Abs(delta) > Math.Abs(prevDelta))
                    {
                        Atmosphere = null;
                        ResetCachedTemperatures();
                        currentTargetTemp = targetEffectiveTemp;
                    }
                    else
                    {
                        currentTargetTemp += delta;
                    }
                    count++;
                } while (count < 10 && Math.Abs(delta) > TemperatureErrorTolerance);
            }
        }

        /// <summary>
        /// Determines the average precipitation at the given <paramref name="position"/> under the
        /// given conditions, over the given duration, in mm.
        /// </summary>
        /// <param name="position">The position on the planet's surface at which to determine
        /// precipitation.</param>
        /// <param name="seasonalLatitude">The "seasonal" latitude of the position relative to the
        /// solar equator, rather than the rotational equator.</param>
        /// <param name="temperature">The current surface temperature at the position.</param>
        /// <param name="proportionOfYear">The proportion of the year over which to determine
        /// precipitation.</param>
        /// <param name="snow">
        /// <para>
        /// When the method returns, will be set to the amount of snow which falls. Note that this
        /// amount <i>replaces</i> any precipitation that would have fallen as rain; the return
        /// value is to be considered a water-equivalent total value which is equal to the snow.
        /// </para>
        /// </param>
        /// <returns>The average precipitation at the given <paramref name="position"/> and time of
        /// year, given the specified conditions, in mm.</returns>
        public double GetPrecipitation(Vector3 position, double seasonalLatitude, double temperature, double proportionOfYear, out double snow)
        {
            snow = 0;

            var avgPrecipitation = Atmosphere.AveragePrecipitation * proportionOfYear;

            var v = position * 100;

            // Noise map with smooth, broad areas. Random range ~-0.8-1.
            var r1 = 0.1 + (Noise2.GetNoise(v.X, v.Y, v.Z) * 0.9);

            // More detailed noise map. Random range of ~-1-1 adjusted to ~0.8-1.
            var r2 = Math.Abs((Noise1.GetNoise(v.X, v.Y, v.Z) * 0.1) + 0.9);

            // Combined map is noise with broad similarity over regions, and minor local
            // diversity, with range of ~-1-1.
            var r = r1 * r2;

            // Hadley cells scale by 1.5 around the equator, ~0.1 ±15º lat, ~0.2 ±40º lat, and ~0
            // ±75º lat; this creates the ITCZ, the subtropical deserts, the temperate zone, and
            // the polar deserts.
            var roundedAbsLatitude = Math.Round(Math.Max(0, seasonalLatitude - ThirtySixthPI), 3);
            if (!HadleyValues.TryGetValue(roundedAbsLatitude, out var hadleyValue))
            {
                hadleyValue = (Math.Cos(MathConstants.TwoPI * Math.Sqrt(roundedAbsLatitude)) / ((8 * roundedAbsLatitude) + 1)) - (roundedAbsLatitude / Math.PI) + 0.5;
                HadleyValues.Add(roundedAbsLatitude, hadleyValue);
            }

            // Relative humidity is the Hadley cell value added to the random value, and cut off
            // below 0. Range 0-~2.5.
            var relativeHumidity = Math.Max(0, r + hadleyValue);

            // In a band -16-+8K around freezing, the value is scaled down; below that range it is
            // cut off completely; above it is unchanged.
            relativeHumidity *= ((temperature - LowTemp) / 24).Clamp(0, 1);

            if (relativeHumidity <= 0)
            {
                return 0;
            }

            // Scale by distance from target.
            var factor = 1 + (relativeHumidity * ((relativeHumidity * 0.1) - 0.15)) + Math.Max(0, Math.Exp(relativeHumidity / 2) - 1.2);
            factor *= factor;

            var precipitation = avgPrecipitation * relativeHumidity * factor;

            if (temperature <= Chemical.Water.MeltingPoint)
            {
                snow = precipitation * Atmosphere.SnowToRainRatio;
            }

            return precipitation;
        }

        /// <summary>
        /// Determines the average precipitation at the given <paramref name="position"/> under the
        /// given conditions, over the given duration, in mm.
        /// </summary>
        /// <param name="position">The position on the planet's surface at which to determine
        /// precipitation.</param>
        /// <param name="proportionOfYear">The proportion of the year over which to determine
        /// precipitation.</param>
        /// <param name="trueAnomaly">The true anomaly of the planet's orbit at the beginning of the
        /// period during which precipitation is to be determined.</param>
        /// <param name="snow">
        /// <para>
        /// When the method returns, will be set to the amount of snow which falls. Note that this
        /// amount <i>replaces</i> any precipitation that would have fallen as rain; the return
        /// value is to be considered a water-equivalent total value which is equal to the snow.
        /// </para>
        /// </param>
        /// <returns>The average precipitation at the given <paramref name="position"/> and time of
        /// year, in mm.</returns>
        public double GetPrecipitation(Vector3 position, double proportionOfYear, double trueAnomaly, out double snow)
        {
            var latitude = VectorToLatitude(position);
            var seasonalLatitude = Math.Abs(GetSeasonalLatitude(latitude, trueAnomaly));
            return GetPrecipitation(
                position,
                seasonalLatitude,
                GetTemperatureAtElevation(
                    GetSurfaceTemperatureAtTrueAnomaly(trueAnomaly, seasonalLatitude),
                    GetElevationAt(position)),
                proportionOfYear,
                out snow);
        }

        /// <summary>
        /// Determines the average precipitation at the given <paramref name="position"/> over the
        /// given duration, in mm.
        /// </summary>
        /// <param name="position">The position on the planet's surface at which to determine
        /// precipitation.</param>
        /// <param name="proportionOfYear">The proportion of the year over which to determine
        /// precipitation.</param>
        /// <param name="snow">
        /// <para>
        /// When the method returns, will be set to the amount of snow which falls. Note that this
        /// amount <i>replaces</i> any precipitation that would have fallen as rain; the return
        /// value is to be considered a water-equivalent total value which is equal to the snow.
        /// </para>
        /// </param>
        /// <returns>The average precipitation at the given <paramref name="position"/>, in
        /// mm.</returns>
        public double GetPrecipitation(Vector3 position, double proportionOfYear, out double snow)
            => GetPrecipitation(position, proportionOfYear, Orbit?.TrueAnomaly ?? 0, out snow);

        /// <summary>
        /// Gets or generates a <see cref="Season"/> for this <see cref="TerrestrialPlanet"/>.
        /// </summary>
        /// <param name="proportionOfYear">
        /// The proportion of a year which the season comprises. Can be 1 to get a full year, or to
        /// get any information for a planet not in orbit.
        /// </param>
        /// <param name="trueAnomaly">
        /// The true anomaly of the orbit at the midpoint of the season. Can be 0 if the planet is
        /// not in orbit.
        /// </param>
        /// <returns>A <see cref="Season"/>.</returns>
        public Season GetSeason(double proportionOfYear, double trueAnomaly)
        {
            if (!_climateSet)
            {
                SetClimate();
            }

            Season season;
            if (_seasons.Value.Count > 0 && _seasons.Value[0].ProportionOfYear.IsEqualTo(proportionOfYear))
            {
                season = _seasons.Value?.FirstOrDefault(x => x.TrueAnomaly.IsEqualTo(trueAnomaly));
                if (season != null)
                {
                    return season;
                }
            }

            season = new Season(
                this,
                proportionOfYear,
                trueAnomaly);
            if (season != null)
            {
                _seasons.Value.Add(season);
            }
            return season;
        }

        /// <summary>
        /// Gets or generates a <see cref="Season"/> for this <see cref="TerrestrialPlanet"/>.
        /// </summary>
        /// <param name="amount">
        /// The number of seasons in one year. Must be greater than or equal to 1.
        /// </param>
        /// <param name="index">
        /// The 0-based index of the expected season in the seasons of the year.
        /// </param>
        /// <returns>A <see cref="Season"/>.</returns>
        public Season GetSeason(uint amount, uint index)
        {
            if (amount < 1)
            {
                return null;
            }

            var seasonAngle = MathConstants.TwoPI / amount;
            var trueAnomaly = WinterSolsticeTrueAnomaly + (seasonAngle * index);
            if (trueAnomaly >= MathConstants.TwoPI)
            {
                trueAnomaly -= MathConstants.TwoPI;
            }

            return GetSeason(1.0 / amount, trueAnomaly);
        }

        /// <summary>
        /// Gets a set of seasons for this <see cref="TerrestrialPlanet"/>.
        /// </summary>
        /// <param name="amount">
        /// The number of seasons in one year.
        /// </param>
        /// <returns>An enumeration of <see cref="Season"/> instances.</returns>
        public IEnumerable<Season> GetSeasons(uint amount)
        {
            if (!_climateSet)
            {
                SetClimate();
            }

            var proportionOfYear = 1.0 / amount;

            if (_seasons.Value.Count == amount && _seasons.Value[0].ProportionOfYear.IsEqualTo(proportionOfYear))
            {
                foreach (var season in _seasons.Value)
                {
                    yield return season;
                }
                yield break;
            }

            var trueAnomaly = WinterSolsticeTrueAnomaly;
            var seasonAngle = MathConstants.TwoPI / amount;
            for (uint i = 0; i < amount; i++)
            {
                yield return new Season(this, proportionOfYear, trueAnomaly);
                trueAnomaly += seasonAngle;
                if (trueAnomaly > MathConstants.TwoPI)
                {
                    trueAnomaly -= MathConstants.TwoPI;
                }
            }
        }

        /// <summary>
        /// Determines whether there is persistent snow cover in a location with the given
        /// conditions at the given proportion of a year.
        /// </summary>
        /// <param name="proportionOfYear">The current elapsed proportion of the year at which to
        /// determine the presence of snow cover.</param>
        /// <param name="position">The normalized position vector of the location.</param>
        /// <param name="latitude">The latitude of the location, in radians.</param>
        /// <param name="elevation">The elevation of the location above sea level, in
        /// meters.</param>
        /// <returns><see langword="true"/> if there is persistent snow cover under the given
        /// conditions; otherwise <see langword="false"/>.</returns>
        public bool GetSnowCover(double proportionOfYear, Vector3 position, double latitude, double elevation)
        {
            var annualPrecipitation = GetPrecipitation(position, 1 / 12, out var snow) * 12;
            if (snow > 0)
            {
                return true;
            }
            return GetSnowCover(proportionOfYear, GetSnowCoverRange(latitude, elevation, annualPrecipitation));
        }

        /// <summary>
        /// Gets the proportion of the year during which there is persistent snow cover in a
        /// location with the given conditions.
        /// </summary>
        /// <param name="temperatureRange">The range of temperatures found in the location.</param>
        /// <param name="latitude">The latitude of the location, in radians.</param>
        /// <param name="elevation">The elevation of the location above sea level, in
        /// meters.</param>
        /// <param name="humidityType">The <see cref="HumidityType"/> of the location.</param>
        /// <returns>A <see cref="FloatRange"/> which specifies the proportion of the year during
        /// which there is persistent snow cover in a location with the given conditions. The
        /// minimum specifies the proportion of the year at which snow begins to cover the ground,
        /// and the maximum specifies the proportion of the year when it stops.</returns>
        public FloatRange GetSnowCoverRange(FloatRange temperatureRange, double latitude, double elevation, HumidityType humidityType)
        {
            if (elevation <= 0
                || humidityType <= HumidityType.Perarid
                || temperatureRange.Min > Chemical.Water_Salt.MeltingPoint)
            {
                return FloatRange.Zero;
            }
            if (temperatureRange.Max < Chemical.Water_Salt.MeltingPoint)
            {
                return FloatRange.ZeroToOne;
            }

            var freezeProportion = MathUtility.InverseLerp(temperatureRange.Min, temperatureRange.Max, Chemical.Water_Salt.MeltingPoint);
            if (double.IsNaN(freezeProportion))
            {
                return FloatRange.Zero;
            }
            // Freezes more than melts; never fully melts.
            if (freezeProportion >= 0.5)
            {
                return FloatRange.ZeroToOne;
            }

            var meltStart = freezeProportion / 2;
            var iceMeltFinish = freezeProportion;
            var snowMeltFinish = freezeProportion * 3 / 4;
            var freezeStart = 1 - (freezeProportion / 2);
            if (latitude < 0)
            {
                iceMeltFinish += 0.5;
                if (iceMeltFinish > 1)
                {
                    iceMeltFinish--;
                }

                snowMeltFinish += 0.5;
                if (snowMeltFinish > 1)
                {
                    snowMeltFinish--;
                }

                freezeStart -= 0.5;
            }
            return new FloatRange((float)freezeStart, (float)snowMeltFinish);
        }

        /// <summary>
        /// Gets the proportion of the year during which there is persistent snow cover in a
        /// location with the given conditions.
        /// </summary>
        /// <param name="latitude">The latitude of the location, in radians.</param>
        /// <param name="elevation">The elevation of the location above sea level, in
        /// meters.</param>
        /// <param name="annualPrecipitation">The average annual precipitation in the location, in
        /// mm.</param>
        /// <returns>A <see cref="FloatRange"/> which specifies the proportion of the year during
        /// which there is persistent snow cover in a location with the given conditions. The
        /// minimum specifies the proportion of the year at which snow begins to cover the ground,
        /// and the maximum specifies the proportion of the year when it stops.</returns>
        public FloatRange GetSnowCoverRange(double latitude, double elevation, double annualPrecipitation)
            => GetSnowCoverRange(GetSurfaceTemperatureRangeAt(latitude, elevation), latitude, elevation, ClimateTypes.GetHumidityType(annualPrecipitation));

        /// <summary>
        /// Gets the proportion of the year during which there is persistent snow cover in a
        /// location with the given conditions.
        /// </summary>
        /// <param name="position">The normalized position vector of the location.</param>
        /// <param name="latitude">The latitude of the location, in radians.</param>
        /// <param name="elevation">The elevation of the location above sea level, in
        /// meters.</param>
        /// <returns>A <see cref="FloatRange"/> which specifies the proportion of the year during
        /// which there is persistent snow cover in a location with the given conditions. The
        /// minimum specifies the proportion of the year at which snow begins to cover the ground,
        /// and the maximum specifies the proportion of the year when it stops.</returns>
        public FloatRange GetSnowCoverRange(Vector3 position, double latitude, double elevation)
            => GetSnowCoverRange(latitude, elevation, GetPrecipitation(position, 1, out var _));

        /// <summary>
        /// Determines whether there is persistent sea ice in a location with the given
        /// conditions at the given proportion of a year.
        /// </summary>
        /// <param name="proportionOfYear">The current elapsed proportion of the year at which to
        /// determine the presence of snow cover.</param>
        /// <param name="latitude">The latitude of the location, in radians.</param>
        /// <param name="elevation">The elevation of the location above sea level, in
        /// meters.</param>
        /// <returns><see langword="true"/> if there is persistent sea ice under the given
        /// conditions; otherwise <see langword="false"/>.</returns>
        public bool GetSeaIce(double proportionOfYear, double latitude, double elevation)
            => GetSeaIce(proportionOfYear, GetSeaIceRange(latitude, elevation));

        /// <summary>
        /// Gets the proportion of the year during which there is persistent sea ice in a location
        /// with the given conditions.
        /// </summary>
        /// <param name="temperatureRange">The range of temperatures found in the location.</param>
        /// <param name="latitude">The latitude of the location, in radians.</param>
        /// <param name="elevation">The elevation of the location above sea level, in
        /// meters.</param>
        /// <returns>A <see cref="FloatRange"/> which specifies the proportion of the year during
        /// which there is persistent sea ice in a location with the given conditions. The minimum
        /// specifies the proportion of the year at which sea ice begins, and the maximum specifies
        /// the proportion of the year when it stops.</returns>
        public FloatRange GetSeaIceRange(FloatRange temperatureRange, double latitude, double elevation)
        {
            if (elevation > 0 || temperatureRange.Min > Chemical.Water_Salt.MeltingPoint)
            {
                return FloatRange.Zero;
            }
            if (temperatureRange.Max < Chemical.Water_Salt.MeltingPoint)
            {
                return FloatRange.ZeroToOne;
            }

            var freezeProportion = MathUtility.InverseLerp(temperatureRange.Min, temperatureRange.Max, Chemical.Water_Salt.MeltingPoint);
            if (double.IsNaN(freezeProportion))
            {
                return FloatRange.Zero;
            }
            // Freezes more than melts; never fully melts.
            if (freezeProportion >= 0.5)
            {
                return FloatRange.ZeroToOne;
            }

            var meltStart = freezeProportion / 2;
            var iceMeltFinish = freezeProportion;
            var snowMeltFinish = freezeProportion * 3 / 4;
            var freezeStart = 1 - (freezeProportion / 2);
            if (latitude < 0)
            {
                iceMeltFinish += 0.5;
                if (iceMeltFinish > 1)
                {
                    iceMeltFinish--;
                }

                snowMeltFinish += 0.5;
                if (snowMeltFinish > 1)
                {
                    snowMeltFinish--;
                }

                freezeStart -= 0.5;
            }
            return new FloatRange((float)freezeStart, (float)iceMeltFinish);
        }

        /// <summary>
        /// Gets the proportion of the year during which there is persistent sea ice in a location
        /// with the given conditions.
        /// </summary>
        /// <param name="latitude">The latitude of the location, in radians.</param>
        /// <param name="elevation">The elevation of the location above sea level, in
        /// meters.</param>
        /// <returns>A <see cref="FloatRange"/> which specifies the proportion of the year during
        /// which there is persistent sea ice in a location with the given conditions. The minimum
        /// specifies the proportion of the year at which sea ice begins, and the maximum specifies
        /// the proportion of the year when it stops.</returns>
        public FloatRange GetSeaIceRange(double latitude, double elevation)
            => GetSeaIceRange(GetSurfaceTemperatureRangeAt(latitude, elevation), latitude, elevation);

        /// <summary>
        /// Produces a set of equirectangular projections of the specified region describing the
        /// climate.
        /// </summary>
        /// <param name="resolution">The vertical resolution of the projection.</param>
        /// <param name="centralMeridian">The longitude of the central meridian of the projection,
        /// in radians.</param>
        /// <param name="centralParallel">The latitude of the central parallel of the projection, in
        /// radians.</param>
        /// <param name="standardParallels">The latitude of the standard parallels (north and south
        /// of the equator) where the scale of the projection is 1:1, in radians. Zero indicates the
        /// equator (the plate carrée projection). It does not matter whether the positive or
        /// negative latitude is provided, if it is non-zero. If <see langword="null"/>, the
        /// <paramref name="centralParallel"/> will be used.</param>
        /// <param name="range">If provided, indicates the latitude range (north and south of
        /// <paramref name="centralParallel"/>) shown on the projection, in radians. If not
        /// provided, or if equal to zero or greater than π, indicates that the entire globe is
        /// shown.</param>
        /// <param name="start">The true anomaly of the beginning of the period for which to
        /// generate maps, offset from the winter solstice. Zero (the default) indicates the winter
        /// solstice. Values below zero, or greater than or equal to 1, are treated as zero.</param>
        /// <param name="end">The true anomaly of the end of the period for which to generate maps,
        /// offset from the winter solstice. One (the default) indicates the winter solstice of the
        /// following year (i.e. one full year, if <paramref name="start"/> was zero). Values less
        /// than or equal to zero, or greater than one, are treated as one.</param>
        /// <returns>
        /// <para>
        /// An array of surface maps. Each map is a two-dimensional array of <see cref="double"/>
        /// values corresponding to points on equirectangular projected maps of the surface. The
        /// first index of each two-dimensional array corresponds to the X coordinate, and the
        /// second index corresponds to the Y coordinate.
        /// </para>
        /// <para>
        /// The first two-dimensional array in the set (at index 0) is a temperature map, showing
        /// the average temperature during the specified period. Values range from 0 to 1, with 1
        /// indicating the maximum temperature of the planet.
        /// <seealso cref="Planetoid.MaxSurfaceTemperature"/>
        /// </para>
        /// <para>
        /// The second array in the set (at index 1) is a precipitation map, showing the total
        /// amount of precipitation which falls during the specified period. Values range from 0 to
        /// 1, with 1 indicating the maximum potential precipitation of the planet's atmosphere.
        /// <seealso cref="Atmosphere.MaxPrecipitation"/>
        /// </para>
        /// <para>
        /// The third array in the set is a snowfall map, showing the total amount of snow which
        /// falls during the specified period. Values range from 0 to 1, with 1 indicating the
        /// maximum potential snowfall of the planet's atmosphere.
        /// <seealso cref="Atmosphere.MaxSnowfall"/>
        /// </para>
        /// <para>
        /// The fourth array in the set is a snow cover map, showing where persistent snow cover is
        /// present during the specified period. Values are either 0 or 1, with 0 indicating no
        /// persistent snow cover and 1 indicating persistent snow cover is present.
        /// </para>
        /// <para>
        /// The fifth array in the set is a sea ice map, showing where sea ice is present during the
        /// specified period. Values are either 0 or 1, with 0 indicating no sea ice and 1
        /// indicating sea ice is present.
        /// </para>
        /// </returns>
        public double[][,] GetWeatherMaps(
            uint resolution,
            double centralMeridian = 0,
            double centralParallel = 0,
            double? standardParallels = null,
            double? range = null,
            double start = 0,
            double end = 1)
        {
            if (resolution > int.MaxValue / 2)
            {
                throw new ArgumentOutOfRangeException(nameof(resolution), $"The value of {nameof(resolution)} cannot exceed half of int.MaxValue ({(int.MaxValue / 2).ToString()}).");
            }
            var set = new double[5][,];

            throw new NotImplementedException();

            return set;
        }

        /// <summary>
        /// Produces a set of equirectangular projections of the specified region describing the
        /// climate.
        /// </summary>
        /// <param name="resolution">The vertical resolution of the projection.</param>
        /// <param name="centralMeridian">The longitude of the central meridian of the projection,
        /// in radians.</param>
        /// <param name="centralParallel">The latitude of the central parallel of the projection, in
        /// radians.</param>
        /// <param name="standardParallels">The latitude of the standard parallels (north and south
        /// of the equator) where the scale of the projection is 1:1, in radians. Zero indicates the
        /// equator (the plate carrée projection). It does not matter whether the positive or
        /// negative latitude is provided, if it is non-zero. If <see langword="null"/>, the
        /// <paramref name="centralParallel"/> will be used.</param>
        /// <param name="range">If provided, indicates the latitude range (north and south of
        /// <paramref name="centralParallel"/>) shown on the projection, in radians. If not
        /// provided, or if equal to zero or greater than π, indicates that the entire globe is
        /// shown.</param>
        /// <param name="steps">The number of map sets which will be generated, at equal times
        /// throughout the course of one solar year. The greater the number of sets (and thus, the
        /// shorter the time span represented by each step), the more accurate the results will be,
        /// at the cost of increased processing time.</param>
        /// <returns>
        /// <para>
        /// An array of arrays. The index to the outer array corresponds to the step (or season) for
        /// which maps were generated. There will be an equal number of values to <paramref
        /// name="steps"/>.
        /// </para>
        /// <para>
        /// The second level of arrays are sets of maps. Each map is a two-dimensional array, the
        /// values of which correspond to points on equirectangular projected maps of the surface.
        /// The first index of each third-level, two-dimensional array corresponds to the X
        /// coordinate, and the second index corresponds to the Y coordinate.
        /// </para>
        /// <para>
        /// The first two-dimensional array in each set (at index 0) is a temperature map, showing
        /// the average temperature during the specified period. Values range from 0 to 1, with 1
        /// indicating the maximum temperature of the planet.
        /// <seealso cref="Planetoid.MaxSurfaceTemperature"/>
        /// </para>
        /// <para>
        /// The second array in each set (at index 1) is a precipitation map, showing the total
        /// amount of precipitation which falls during the specified period. Values range from 0 to
        /// 1, with 1 indicating the maximum potential precipitation of the planet's atmosphere.
        /// <seealso cref="Atmosphere.MaxPrecipitation"/>
        /// </para>
        /// <para>
        /// The third array in each set is a snowfall map, showing the total amount of snow which
        /// falls during the specified period. Values range from 0 to 1, with 1 indicating the
        /// maximum potential snowfall of the planet's atmosphere.
        /// <seealso cref="Atmosphere.MaxSnowfall"/>
        /// </para>
        /// <para>
        /// The third array in each set is a snow cover map, showing where persistent snow cover is
        /// present during the specified period. Values are either 0 or 1, with 0 indicating no
        /// persistent snow cover and 1 indicating persistent snow cover is present.
        /// </para>
        /// <para>
        /// The third array in each set is a sea ice map, showing where sea ice is present during
        /// the specified period. Values are either 0 or 1, with 0 indicating no sea ice and 1
        /// indicating sea ice is present.
        /// </para>
        /// </returns>
        public double[][][,] GetWeatherMapSet(
            uint resolution,
            double centralMeridian = 0,
            double centralParallel = 0,
            double? standardParallels = null,
            double? range = null,
            uint steps = 12)
        {
            if (resolution > int.MaxValue / 2)
            {
                throw new ArgumentOutOfRangeException(nameof(resolution), $"The value of {nameof(resolution)} cannot exceed half of int.MaxValue ({(int.MaxValue / 2).ToString()}).");
            }
            if (steps > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(steps), $"The value of {nameof(steps)} cannot exceed int.MaxValue ({int.MaxValue.ToString()}).");
            }
            var sets = new double[steps][][,];

            throw new NotImplementedException();

            return sets;
        }

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

            if (!IsHospitable)
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
            if (GetMinEquatorTemperature() < (habitabilityRequirements.MinimumTemperature ?? 0))
            {
                reason |= UninhabitabilityReason.TooCold;
            }

            // To determine if a planet is too hot, the polar temperature at periapsis is used, since
            // this should be the coldest region at its hottest time.
            if (GetMaxPolarTemperature() > (habitabilityRequirements.MaximumTemperature ?? double.PositiveInfinity))
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

            return reason == UninhabitabilityReason.None;
        }

        internal double GetHydrosphereAtmosphereRatio() => Math.Min(1, HydrosphereProportion * Mass / Atmosphere.Mass);

        internal bool GetSeaIce(double proportionOfYear, FloatRange seaIceRange)
            => !seaIceRange.IsZero
            && (seaIceRange.Min > seaIceRange.Max
                ? proportionOfYear >= seaIceRange.Min || proportionOfYear <= seaIceRange.Max
                : proportionOfYear >= seaIceRange.Min && proportionOfYear <= seaIceRange.Max);

        internal bool GetSnowCover(double proportionOfYear, FloatRange snowCoverRange)
            => !snowCoverRange.IsZero
            && (snowCoverRange.Min > snowCoverRange.Max
                ? proportionOfYear >= snowCoverRange.Min || proportionOfYear <= snowCoverRange.Max
                : proportionOfYear >= snowCoverRange.Min && proportionOfYear <= snowCoverRange.Max);

        private void AdjustOrbitForTemperature(Star star, double? semiMajorAxis, double trueAnomaly, double targetTemp)
        {
            // Orbital distance averaged over time (mean anomaly) = semi-major axis * (1 + eccentricity^2 / 2).
            // This allows calculation of the correct distance/orbit for an average
            // orbital temperature (rather than the temperature at the current position).
            if (PlanetParams?.RevolutionPeriod.HasValue == true)
            {
                star.SetTempForTargetPlanetTemp(targetTemp, semiMajorAxis.Value * (1 + (Eccentricity * Eccentricity / 2)), Albedo);
            }
            else
            {
                semiMajorAxis = GetDistanceForTemperature(star, targetTemp) / (1 + (Eccentricity * Eccentricity / 2));
                GenerateOrbit(star, semiMajorAxis.Value, trueAnomaly);
            }
            ResetCachedTemperatures();
        }

        private void CalculateGasPhaseMix(
            bool _canHaveOxygen,
            Chemical chemical,
            double surfaceTemp,
            ref double hydrosphereAtmosphereRatio,
            ref double adjustedAtmosphericPressure)
        {
            var proportionInHydrosphere = Hydrosphere.GetProportion(chemical, Phase.Any);
            if (chemical == Chemical.Water)
            {
                proportionInHydrosphere += Hydrosphere.GetProportion(Chemical.Water_Salt, Phase.Any);
            }

            var vaporProportion = Atmosphere.Composition.GetProportion(chemical, Phase.Gas);

            var vaporPressure = chemical.GetVaporPressure(surfaceTemp);

            if (surfaceTemp < chemical.AntoineMinimumTemperature
                || (surfaceTemp <= chemical.AntoineMaximumTemperature
                && Atmosphere.AtmosphericPressure > vaporPressure))
            {
                CondenseAtmosphericComponent(
                    _canHaveOxygen,
                    chemical,
                    surfaceTemp,
                    proportionInHydrosphere,
                    vaporProportion,
                    vaporPressure,
                    ref hydrosphereAtmosphereRatio,
                    ref adjustedAtmosphericPressure);
            }
            // This indicates that the chemical will fully boil off.
            else if (proportionInHydrosphere > 0)
            {
                EvaporateAtmosphericComponent(
                    _canHaveOxygen,
                    chemical,
                    proportionInHydrosphere,
                    vaporProportion,
                    ref hydrosphereAtmosphereRatio,
                    ref adjustedAtmosphericPressure);
            }

            if (chemical == Chemical.Water)
            {
                CheckCO2Reduction(vaporPressure);
            }
        }

        /// <summary>
        /// Adjusts the phase of various atmospheric and surface substances depending on the surface
        /// temperature of the body.
        /// </summary>
        /// <param name="counter">The number of times this method has been called.</param>
        /// <param name="surfaceTemp">The effective surface temperature, in K.</param>
        /// <param name="hydrosphereAtmosphereRatio">The mass ratio of hydrosphere to atmosphere.</param>
        /// <param name="adjustedAtmosphericPressure">The effective atmospheric pressure, in kPa.</param>
        /// <remarks>
        /// Despite the theoretical possibility of an atmosphere cold enough to precipitate some of
        /// the noble gases, or hydrogen, they are ignored and presumed to exist always as trace
        /// atmospheric gases, never surface liquids or ices, or in large enough quantities to form clouds.
        /// </remarks>
        private void CalculatePhases(int counter, double surfaceTemp, ref double hydrosphereAtmosphereRatio, ref double adjustedAtmosphericPressure)
        {
            CalculatePhases(CanHaveOxygen, surfaceTemp, ref hydrosphereAtmosphereRatio, ref adjustedAtmosphericPressure);

            var oldAlbedo = Albedo;

            // Ices significantly impact albedo.
            var iceAmount = Math.Min(1, Hydrosphere.GetSurface().GetChemicals(Phase.Solid).Sum(x => x.proportion));
            Albedo = (SurfaceAlbedo * (1 - iceAmount)) + (0.9 * iceAmount);

            // Clouds also impact albedo.
            var cloudCover = Math.Min(1, Atmosphere.AtmosphericPressure * Atmosphere.Composition.GetCloudCover() / 100);
            Albedo = (SurfaceAlbedo * (1 - cloudCover)) + (0.9 * cloudCover);

            // An albedo change might significantly alter surface temperature, which may require a
            // re-calculation (but not too many). 5K is used as the threshold for re-calculation,
            // which may lead to some inaccuracies, but should avoid over-repetition for small changes.
            if (counter < 10 && Albedo != oldAlbedo && Math.Abs(surfaceTemp - AverageSurfaceTemperature) > 5)
            {
                CalculatePhases(counter + 1, AverageSurfaceTemperature, ref hydrosphereAtmosphereRatio, ref adjustedAtmosphericPressure);
            }
        }

        /// <summary>
        /// Adjusts the phase of various atmospheric and surface substances depending on the surface
        /// temperature of the body.
        /// </summary>
        /// <param name="_canHaveOxygen">Whether oxygen is allowed.</param>
        /// <param name="surfaceTemp">The effective surface temperature, in K.</param>
        /// <param name="hydrosphereAtmosphereRatio">The mass ratio of hydrosphere to atmosphere.</param>
        /// <param name="adjustedAtmosphericPressure">The effective atmospheric pressure, in kPa.</param>
        /// <remarks>
        /// Despite the theoretical possibility of an atmosphere cold enough to precipitate some of
        /// the noble gases, or hydrogen, they are ignored and presumed to exist always as trace
        /// atmospheric gases, never surface liquids or ices, or in large enough quantities to form clouds.
        /// </remarks>
        private void CalculatePhases(
            bool _canHaveOxygen,
            double surfaceTemp,
            ref double hydrosphereAtmosphereRatio,
            ref double adjustedAtmosphericPressure)
        {
            CalculateGasPhaseMix(_canHaveOxygen, Chemical.Methane, surfaceTemp, ref hydrosphereAtmosphereRatio, ref adjustedAtmosphericPressure);
            CalculateGasPhaseMix(_canHaveOxygen, Chemical.CarbonMonoxide, surfaceTemp, ref hydrosphereAtmosphereRatio, ref adjustedAtmosphericPressure);
            CalculateGasPhaseMix(_canHaveOxygen, Chemical.CarbonDioxide, surfaceTemp, ref hydrosphereAtmosphereRatio, ref adjustedAtmosphericPressure);
            CalculateGasPhaseMix(_canHaveOxygen, Chemical.Nitrogen, surfaceTemp, ref hydrosphereAtmosphereRatio, ref adjustedAtmosphericPressure);
            CalculateGasPhaseMix(_canHaveOxygen, Chemical.Oxygen, surfaceTemp, ref hydrosphereAtmosphereRatio, ref adjustedAtmosphericPressure);

            // No need to check for ozone, since it is only added to atmospheres on planets with
            // liquid surface water, which means temperatures too high for liquid or solid ozone.

            CalculateGasPhaseMix(_canHaveOxygen, Chemical.SulphurDioxide, surfaceTemp, ref hydrosphereAtmosphereRatio, ref adjustedAtmosphericPressure);

            // Water is handled differently, since the planet may already have surface water.
            if (Hydrosphere.ContainsSubstance(Chemical.Water, Phase.Any)
                || Hydrosphere.ContainsSubstance(Chemical.Water_Salt, Phase.Any)
                || Atmosphere.Composition.ContainsSubstance(Chemical.Water, Phase.Any))
            {
                CalculateGasPhaseMix(_canHaveOxygen, Chemical.Water, surfaceTemp, ref hydrosphereAtmosphereRatio, ref adjustedAtmosphericPressure);
            }
        }

        /// <summary>
        /// At least 1% humidity leads to a reduction of CO2 to a trace gas, by a presumed
        /// carbon-silicate cycle.
        /// </summary>
        /// <param name="vaporPressure">The vapor pressure of water.</param>
        private void CheckCO2Reduction(double vaporPressure)
        {
            var air = Atmosphere.Composition.GetSurface();
            if (air.GetProportion(Chemical.Water, Phase.Gas) * Atmosphere.AtmosphericPressure >= 0.01 * vaporPressure)
            {
                var co2 = air.GetProportion(Chemical.CarbonDioxide, Phase.Gas);
                if (co2 < 1.0e-3)
                {
                    return;
                }

                if (Atmosphere.Composition is LayeredComposite layeredComposite)
                {
                    for (var i = 0; i < layeredComposite.Layers.Count; i++)
                    {
                        if (!layeredComposite.Layers[i].substance.ContainsSubstance(Chemical.CarbonDioxide, Phase.Gas))
                        {
                            continue;
                        }

                        layeredComposite.Layers[i] = (ReduceCO2(layeredComposite.Layers[i].substance), layeredComposite.Layers[i].proportion);
                    }
                }
                else
                {
                    Atmosphere.Composition = ReduceCO2(Atmosphere.Composition);
                }
                Atmosphere.ResetGreenhouseFactor();
            }
        }

        private void ClassifyTerrain()
        {
            foreach (var t in Grid.Tiles)
            {
                if (t.Elevation < 0)
                {
                    t.IsWater = true;
                }
                else
                {
                    t.IsLand = true;
                }
                for (var i = 0; i < t.EdgeCount; i++)
                {
                    if (Grid.Corners[t.Corners[i]].Elevation < 0)
                    {
                        t.IsWater = true;
                        if (t.IsLand)
                        {
                            break;
                        }
                    }
                    else
                    {
                        t.IsLand = true;
                        if (t.IsWater)
                        {
                            break;
                        }
                    }
                }
            }
        }

        private void CondenseAtmosphericComponent(
            bool _canHaveOxygen,
            Chemical chemical,
            double surfaceTemp,
            double proportionInHydrosphere,
            double vaporProportion,
            double vaporPressure,
            ref double hydrosphereAtmosphereRatio,
            ref double adjustedAtmosphericPressure)
        {
            if (surfaceTemp <= chemical.MeltingPoint) // Below freezing point; add ice.
            {
                CondenseAtmosphericIce(
                    chemical,
                    surfaceTemp,
                    proportionInHydrosphere,
                    ref vaporProportion,
                    ref hydrosphereAtmosphereRatio,
                    ref adjustedAtmosphericPressure);
            }
            else // Above freezing point, but also above vapor pressure; add liquid.
            {
                CondenseAtmosphericLiquid(
                    chemical,
                    ref proportionInHydrosphere,
                    vaporProportion,
                    hydrosphereAtmosphereRatio);
            }

            // Adjust vapor present in the atmosphere based on the vapor pressure.
            var pressureRatio = Math.Max(0, Math.Min(1, vaporPressure / Atmosphere.AtmosphericPressure));
            // This would represent 100% humidity. Since this is the case, in principle, only at the
            // surface of bodies of liquid, and should decrease exponentially with altitude, an
            // approximation of 25% average humidity overall is used.
            if (chemical == Chemical.Water && PlanetParams?.WaterVaporRatio.HasValue == true)
            {
                vaporProportion = PlanetParams.WaterVaporRatio.Value;
            }
            else
            {
                vaporProportion = (proportionInHydrosphere + vaporProportion) * pressureRatio;
                vaporProportion *= 0.25;
            }
            if (!TMath.IsZero(vaporProportion))
            {
                double previousGasFraction = 0;
                var gasFraction = vaporProportion;
                Atmosphere.Composition = Atmosphere.Composition.AddComponent(chemical, Phase.Gas, vaporProportion);

                // For water, also add a corresponding amount of oxygen, if it's not already present.
                if (chemical == Chemical.Water && _canHaveOxygen)
                {
                    var o2 = Atmosphere.Composition.GetProportion(Chemical.Oxygen, Phase.Gas);
                    previousGasFraction += o2;
                    o2 = Math.Max(o2, Math.Round(vaporProportion * 0.0001, 5));
                    gasFraction += o2;
                    Atmosphere.Composition = Atmosphere.Composition.AddComponent(Chemical.Oxygen, Phase.Gas, o2);
                }

                adjustedAtmosphericPressure += adjustedAtmosphericPressure * (gasFraction - previousGasFraction);
            }

            // Add clouds.
            var clouds = vaporProportion * 0.2;
            if (!TMath.IsZero(clouds))
            {
                var troposphere = Atmosphere.GetTroposphere();
                if (surfaceTemp <= chemical.MeltingPoint)
                {
                    troposphere.AddComponent(chemical, Phase.Solid, clouds);
                }
                else if (AveragePolarSurfaceTemperature < chemical.MeltingPoint)
                {
                    var halfClouds = clouds / 2;
                    troposphere.AddComponent(chemical, Phase.Liquid, halfClouds);
                    troposphere.AddComponent(chemical, Phase.Solid, halfClouds);
                }
                else
                {
                    troposphere.AddComponent(chemical, Phase.Liquid, clouds);
                }
            }
        }

        private void CondenseAtmosphericIce(
            Chemical chemical,
            double surfaceTemp,
            double proportionInHydrosphere,
            ref double vaporProportion,
            ref double hydrosphereAtmosphereRatio,
            ref double adjustedAtmosphericPressure)
        {
            var ice = proportionInHydrosphere;

            if (chemical == Chemical.Water)
            {
                Atmosphere.ResetWater();

                // A subsurface liquid water ocean may persist if it's deep enough.
                if (HydrosphereProportion >= 0.01)
                {
                    ice = 0.01 / HydrosphereProportion * proportionInHydrosphere;
                }
            }

            // Liquid fully precipitates out of the atmosphere when below the minimum; if no liquid
            // exists in the hydrosphere yet, condensed ice is added from atmospheric vapor, though
            // the latter is not removed.
            if (surfaceTemp < chemical.AntoineMinimumTemperature || ice == 0)
            {
                ice += vaporProportion / hydrosphereAtmosphereRatio;

                if (surfaceTemp < chemical.AntoineMinimumTemperature)
                {
                    vaporProportion = 0;
                    Atmosphere.Composition = Atmosphere.Composition.RemoveComponent(chemical, Phase.Any);
                    if (Atmosphere.Composition.IsEmpty())
                    {
                        adjustedAtmosphericPressure = 0;
                    }
                }
            }

            if (ice.IsZero())
            {
                return;
            }

            if (!proportionInHydrosphere.IsZero()) // Change existing hydrosphere to ice.
            {
                // If a subsurface ocean is indicated, ensure differentiation in the correct proportions.
                if (ice < proportionInHydrosphere)
                {
                    if (Hydrosphere is LayeredComposite layeredComposite)
                    {
                        layeredComposite.Layers[0] = (layeredComposite.Layers[0].substance, 1 - ice);
                        layeredComposite.Layers[1] = (layeredComposite.Layers[1].substance, ice);
                    }
                    else
                    {
                        Hydrosphere = Hydrosphere.Split(1 - ice, ice);
                    }
                }

                // Convert hydrosphere to ice; surface only if a subsurface ocean is indicated.
                if (ice < proportionInHydrosphere && Hydrosphere is LayeredComposite lc1)
                {
                    lc1.SetLayerPhase(lc1.Layers.Count - 1, chemical, Phase.Solid);
                }
                else
                {
                    Hydrosphere.SetPhase(chemical, Phase.Solid);
                }
                if (chemical == Chemical.Water) // Also remove salt water when removing water.
                {
                    if (ice < proportionInHydrosphere && Hydrosphere is LayeredComposite lc2)
                    {
                        lc2.SetLayerPhase(lc2.Layers.Count - 1, Chemical.Water_Salt, Phase.Solid);
                    }
                    else
                    {
                        Hydrosphere.SetPhase(Chemical.Water_Salt, Phase.Solid);
                    }
                }

                hydrosphereAtmosphereRatio = GetHydrosphereAtmosphereRatio();
            }
            else // Chemical not yet present in hydrosphere.
            {
                SetHydrosphereProportion(chemical, Phase.Solid, ice, ref hydrosphereAtmosphereRatio);
            }
        }

        private void CondenseAtmosphericLiquid(
            Chemical chemical,
            ref double proportionInHydrosphere,
            double vaporProportion,
            double hydrosphereAtmosphereRatio)
        {
            if (chemical == Chemical.Water)
            {
                Atmosphere.ResetWater();

                // If the hydrosphere was a surface of water ice with a subsurface ocean, melt the
                // surface and return to a single layer.
                if (Hydrosphere is LayeredComposite layeredComposite)
                {
                    layeredComposite.SetPhase(Chemical.Water, Phase.Liquid);
                    layeredComposite.SetPhase(Chemical.Water_Salt, Phase.Liquid);
                    Hydrosphere = layeredComposite.Homogenize();
                }
            }

            var saltWaterProportion = chemical == Chemical.Water ? Math.Round(Randomizer.Instance.Normal(0.945, 0.015), 3) : 0;
            var liquidProportion = 1 - saltWaterProportion;

            // If there is no liquid on the surface, condense from the atmosphere.
            if (proportionInHydrosphere.IsZero())
            {
                var addedLiquid = vaporProportion / hydrosphereAtmosphereRatio;
                if (!addedLiquid.IsZero())
                {
                    SetHydrosphereProportion(chemical, Phase.Liquid, addedLiquid * liquidProportion, ref hydrosphereAtmosphereRatio);
                    if (chemical == Chemical.Water)
                    {
                        SetHydrosphereProportion(Chemical.Water_Salt, Phase.Liquid, addedLiquid * saltWaterProportion, ref hydrosphereAtmosphereRatio);
                    }
                    proportionInHydrosphere += addedLiquid;
                }
            }

            // Create icecaps.
            if (AveragePolarSurfaceTemperature <= chemical.MeltingPoint)
            {
                var iceCaps = proportionInHydrosphere * 0.28;
                SetHydrosphereProportion(chemical, Phase.Solid, iceCaps * liquidProportion, ref hydrosphereAtmosphereRatio);
                if (chemical == Chemical.Water)
                {
                    SetHydrosphereProportion(Chemical.Water_Salt, Phase.Solid, iceCaps * saltWaterProportion, ref hydrosphereAtmosphereRatio);
                }
            }
        }

        private void EvaporateAtmosphericComponent(
            bool _canHaveOxygen,
            Chemical chemical,
            double hydrosphereProportion,
            double vaporProportion,
            ref double hydrosphereAtmosphereRatio,
            ref double adjustedAtmosphericPressure)
        {
            if (hydrosphereProportion.IsZero())
            {
                return;
            }

            if (chemical == Chemical.Water)
            {
                Hydrosphere.Homogenize();
                Atmosphere.ResetWater();
            }

            var gasProportion = hydrosphereProportion * hydrosphereAtmosphereRatio;
            var previousGasProportion = vaporProportion;

            SetHydrosphereProportion(chemical, Phase.Any, 0, ref hydrosphereAtmosphereRatio);

            if (chemical == Chemical.Water)
            {
                SetHydrosphereProportion(Chemical.Water_Salt, Phase.Any, 0, ref hydrosphereAtmosphereRatio);

                // It is presumed that photodissociation will eventually reduce the amount of water
                // vapor to a trace gas (the H2 will be lost due to atmospheric escape, and the
                // oxygen will be lost to surface oxidation).
                var waterVapor = Math.Min(gasProportion, Math.Round(Randomizer.Instance.NextDouble(0.001), 4));
                gasProportion = waterVapor;

                previousGasProportion += Atmosphere.Composition.GetProportion(Chemical.Oxygen, Phase.Gas);
                var o2 = Math.Round(gasProportion * 0.0001, 5);
                gasProportion += o2;

                if (Atmosphere.Composition is LayeredComposite lc)
                {
                    for (var i = 0; i < lc.Layers.Count; i++)
                    {
                        lc.AddToLayer(i, chemical, Phase.Gas, Math.Max(lc.Layers[i].substance.GetProportion(chemical, Phase.Gas), waterVapor));

                        // Some is added as oxygen, due to photodissociation.
                        if (_canHaveOxygen)
                        {
                            lc.AddToLayer(i, Chemical.Oxygen, Phase.Gas, Math.Max(lc.Layers[i].substance.GetProportion(Chemical.Oxygen, Phase.Gas), o2));
                        }
                    }
                }
                else
                {
                    Atmosphere.Composition = Atmosphere.Composition.AddComponent(chemical, Phase.Gas, Math.Max(Atmosphere.Composition.GetProportion(chemical, Phase.Gas), waterVapor));
                    if (_canHaveOxygen)
                    {
                        Atmosphere.Composition = Atmosphere.Composition.AddComponent(Chemical.Oxygen, Phase.Gas, Math.Max(Atmosphere.Composition.GetProportion(Chemical.Oxygen, Phase.Gas), o2));
                    }
                }
            }
            else
            {
                Atmosphere.Composition = Atmosphere.Composition.AddComponent(chemical, Phase.Gas, gasProportion);
            }

            adjustedAtmosphericPressure += adjustedAtmosphericPressure * (gasProportion - previousGasProportion);
        }

        /// <summary>
        /// Determines an albedo for this <see cref="CelestialBody"/> (a value between 0 and 1).
        /// </summary>
        /// <remarks>
        /// Also sets <see cref="SurfaceAlbedo"/> for terrestrial planets.
        /// </remarks>
        private protected override void GenerateAlbedo()
        {
            Albedo = Randomizer.Instance.NextDouble(0.1, 0.6);
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
                _axialPrecession = Math.Round(Randomizer.Instance.NextDouble(MathConstants.TwoPI), 4);
                var axialTilt = PlanetParams.AxialTilt.Value;
                if (Orbit != null)
                {
                    axialTilt += Orbit.Inclination;
                }
                while (axialTilt < 0)
                {
                    axialTilt += MathConstants.TwoPI;
                }
                while (axialTilt >= MathConstants.TwoPI)
                {
                    axialTilt -= MathConstants.TwoPI;
                }
                AngleOfRotation = axialTilt;
            }
        }

        /// <summary>
        /// Generates an atmosphere for this <see cref="Planetoid"/>.
        /// </summary>
        private protected override void GenerateAtmosphere()
        {
            var surfaceTemp = AverageBlackbodySurfaceTemperature;
            if (surfaceTemp >= GetTempForThinAtmosphere())
            {
                GenerateAtmosphereTrace();
            }
            else
            {
                GenerateAtmosphereThick();
            }

            var hydrosphereAtmosphereRatio = GetHydrosphereAtmosphereRatio();
            var adjustedAtmosphericPressure = Atmosphere.AtmosphericPressure;

            // Water may be removed, or if not may remove CO2 from the atmosphere, depending on
            // planetary conditions.
            if (Hydrosphere.ContainsSubstance(Chemical.Water, Phase.Any)
                || Hydrosphere.ContainsSubstance(Chemical.Water_Salt, Phase.Any)
                || Atmosphere.Composition.ContainsSubstance(Chemical.Water, Phase.Any))
            {
                // First calculate water phases at effective temp, to establish a baseline
                // for the presence of water and its effect on CO2.
                CalculateGasPhaseMix(
                    CanHaveOxygen,
                    Chemical.Water,
                    surfaceTemp,
                    ref hydrosphereAtmosphereRatio,
                    ref adjustedAtmosphericPressure);

                // Recalculate temperatures based on the new atmosphere.
                ResetCachedTemperatures();
                surfaceTemp = AverageSurfaceTemperature;

                // Recalculate the phases of water based on the new temperature.
                CalculateGasPhaseMix(
                    CanHaveOxygen,
                    Chemical.Water,
                    surfaceTemp,
                    ref hydrosphereAtmosphereRatio,
                    ref adjustedAtmosphericPressure);

                // If life alters the greenhouse potential, temperature and water phase must be
                // recalculated once again.
                if (GenerateLife())
                {
                    ResetCachedTemperatures();
                    surfaceTemp = AverageSurfaceTemperature;
                    CalculateGasPhaseMix(
                        CanHaveOxygen,
                        Chemical.Water,
                        surfaceTemp,
                        ref hydrosphereAtmosphereRatio,
                        ref adjustedAtmosphericPressure);
                    ResetCachedTemperatures();
                    surfaceTemp = AverageSurfaceTemperature;
                }
            }
            else
            {
                // Recalculate temperature based on the new atmosphere.
                ResetCachedTemperatures();
                surfaceTemp = AverageSurfaceTemperature;
            }

            foreach (var requirement in Atmosphere.ConvertRequirementsForPressure(HabitabilityRequirements?.AtmosphericRequirements)
                .Concat(Atmosphere.ConvertRequirementsForPressure(PlanetParams?.AtmosphericRequirements)))
            {
                var modified = false;
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
                    modified = true;
                }
                else if (proportion < requirement.MinimumProportion)
                {
                    Atmosphere.Composition.SetProportion(requirement.Chemical, requirement.Phase, requirement.MinimumProportion);
                    modified = true;
                }
                else if (requirement.MaximumProportion.HasValue && proportion > requirement.MaximumProportion)
                {
                    Atmosphere.Composition.SetProportion(requirement.Chemical, requirement.Phase, requirement.MaximumProportion.Value);
                    modified = true;
                }
                if (modified)
                {
                    Atmosphere.ResetGreenhouseFactor();
                }
            }

            CalculatePhases(0, surfaceTemp, ref hydrosphereAtmosphereRatio, ref adjustedAtmosphericPressure);

            if (PlanetParams?.AtmosphericPressure.HasValue != true && HabitabilityRequirements == null)
            {
                SetAtmosphericPressure(Math.Max(0, adjustedAtmosphericPressure));
                Atmosphere.ResetPressureDependentProperties(this);
            }

            // If the adjustments have led to the loss of liquid water, then there is no life after
            // all (this may be interpreted as a world which once supported life, but became
            // inhospitable due to the environmental changes that life produced).
            if (!IsHabitable())
            {
                HasBiosphere = false;
            }
        }

        private void GenerateAtmosphereThick()
        {
            double pressure;
            if (PlanetParams?.AtmosphericPressure.HasValue == true)
            {
                pressure = Math.Max(0, PlanetParams.AtmosphericPressure.Value);
            }
            else if (HabitabilityRequirements?.MinimumPressure.HasValue == true
                || HabitabilityRequirements?.MaximumPressure.HasValue == true)
            {
                // If there is a minimum but no maximum, a half-Gaussian distribution with the minimum as both mean and the basis for the sigma is used.
                if (!HabitabilityRequirements.Value.MaximumPressure.HasValue)
                {
                    pressure = HabitabilityRequirements.Value.MinimumPressure.Value
                        + Math.Abs(Randomizer.Instance.Normal(0, HabitabilityRequirements.Value.MinimumPressure.Value / 3));
                }
                else
                {
                    pressure = Randomizer.Instance.NextDouble(HabitabilityRequirements.Value.MinimumPressure ?? 0, HabitabilityRequirements.Value.MaximumPressure.Value);
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

                var mass = Math.Max(factor, Randomizer.Instance.Lognormal(0, factor * 4));
                pressure = mass * SurfaceGravity / (1000 * MathConstants.FourPI * RadiusSquared);
            }

            // For terrestrial (non-giant) planets, these gases remain at low concentrations due to
            // atmospheric escape.
            var h = Randomizer.Instance.NextDouble(0.5e-7, 0.2e-6);
            var he = Randomizer.Instance.NextDouble(0.26e-6, 1.0e-5);

            // 50% chance not to have these components at all.
            var ch4 = Math.Max(0, Math.Round(Randomizer.Instance.NextDouble(-0.5, 0.5), 4));
            var traceTotal = ch4;

            var co = Math.Max(0, Math.Round(Randomizer.Instance.NextDouble(-0.5, 0.5), 4));
            traceTotal += co;

            var so2 = Math.Max(0, Math.Round(Randomizer.Instance.NextDouble(-0.5, 0.5), 4));
            traceTotal += so2;

            var trace = TMath.IsZero(traceTotal) ? 0 : Randomizer.Instance.NextDouble(1.5e-4, 2.5e-3);
            var traceRatio = TMath.IsZero(traceTotal) ? 0 : trace / traceTotal;
            ch4 *= traceRatio;
            co *= traceRatio;
            so2 *= traceRatio;

            // CO2 makes up the bulk of a thick atmosphere by default (although the presence of water
            // may change this later).
            var co2 = Math.Round(Randomizer.Instance.NextDouble(0.97, 0.99) - trace, 4);

            // If there is water on the surface, the water in the air will be determined based on
            // vapor pressure later, and should not be randomly assigned. Otherwise, there is a small
            // chance of water vapor without significant surface water (results of cometary deposits, etc.)
            var waterVapor = 0.0;
            var surfaceWater = Hydrosphere.ContainsSubstance(Chemical.Water, Phase.Any)
                || Hydrosphere.ContainsSubstance(Chemical.Water_Salt, Phase.Any);
            if (CanHaveWater && !surfaceWater)
            {
                waterVapor = Math.Max(0, Math.Round(Randomizer.Instance.NextDouble(-0.05, 0.001), 4));
            }

            // Always at least some oxygen if there is water, planetary composition allowing
            var o2 = 0.0;
            if (CanHaveOxygen)
            {
                if (waterVapor != 0)
                {
                    o2 = waterVapor * 0.0001;
                }
                else if (surfaceWater)
                {
                    o2 = Math.Round(Randomizer.Instance.NextDouble(0.002), 5);
                }
            }

            // N2 (largely inert gas) comprises whatever is left after the other components have been
            // determined. This is usually a trace amount, unless CO2 has been reduced to a trace, in
            // which case it will comprise the bulk of the atmosphere.
            var n2 = 1 - (h + he + co2 + waterVapor + o2 + trace);

            // Some portion of the N2 may be Ar instead.
            var ar = Math.Max(0, n2 * Randomizer.Instance.NextDouble(-0.02, 0.04));
            n2 -= ar;
            // An even smaller fraction may be Kr.
            var kr = Math.Max(0, n2 * Randomizer.Instance.NextDouble(-2.5e-4, 5.0e-4));
            n2 -= kr;
            // An even smaller fraction may be Xe or Ne.
            var xe = Math.Max(0, n2 * Randomizer.Instance.NextDouble(-1.8e-5, 3.5e-5));
            n2 -= xe;
            var ne = Math.Max(0, n2 * Randomizer.Instance.NextDouble(-1.8e-5, 3.5e-5));
            n2 -= ne;

            var atmosphere = new Composite(
                (Chemical.CarbonDioxide, Phase.Gas, co2),
                (Chemical.Helium, Phase.Gas, he),
                (Chemical.Hydrogen, Phase.Gas, h),
                (Chemical.Nitrogen, Phase.Gas, n2));
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

        private void GenerateAtmosphereTrace()
        {
            // For terrestrial (non-giant) planets, these gases remain at low concentrations due to
            // atmospheric escape.
            var h = Math.Round(Randomizer.Instance.NextDouble(0.5e-7, 0.2e-6), 4);
            var he = Math.Round(Randomizer.Instance.NextDouble(0.26e-6, 1.0e-5), 4);

            // 50% chance not to have these components at all.
            var ch4 = Math.Max(0, Math.Round(Randomizer.Instance.NextDouble(-0.5, 0.5), 4));
            var total = ch4;

            var co = Math.Max(0, Math.Round(Randomizer.Instance.NextDouble(-0.5, 0.5), 4));
            total += co;

            var so2 = Math.Max(0, Math.Round(Randomizer.Instance.NextDouble(-0.5, 0.5), 4));
            total += so2;

            var n2 = Math.Max(0, Math.Round(Randomizer.Instance.NextDouble(-0.5, 0.5), 4));
            total += n2;

            // Noble traces: selected as fractions of N2, if present, to avoid over-representation.
            var ar = n2 > 0 ? Math.Max(0, n2 * Randomizer.Instance.NextDouble(-0.02, 0.04)) : 0;
            n2 -= ar;
            var kr = n2 > 0 ? Math.Max(0, n2 * Randomizer.Instance.NextDouble(-0.02, 0.04)) : 0;
            n2 -= kr;
            var xe = n2 > 0 ? Math.Max(0, n2 * Randomizer.Instance.NextDouble(-0.02, 0.04)) : 0;
            n2 -= xe;

            // Carbon monoxide means at least some carbon dioxide, as well.
            var co2 = Math.Round(co > 0
                ? Randomizer.Instance.NextDouble(0.5)
                : Math.Max(0, Randomizer.Instance.NextDouble(-0.5, 0.5)),
                4);
            total += co2;

            // If there is water on the surface, the water in the air will be determined based on
            // vapor pressure later, and should not be randomly assigned. Otherwise, there is a small
            // chance of water vapor without significant surface water (results of cometary deposits, etc.)
            var waterVapor = 0.0;
            if (CanHaveWater
                && !Hydrosphere.ContainsSubstance(Chemical.Water, Phase.Any)
                && !Hydrosphere.ContainsSubstance(Chemical.Water_Salt, Phase.Any))
            {
                waterVapor = Math.Max(0, Math.Round(Randomizer.Instance.NextDouble(-0.05, 0.001), 4));
            }
            total += waterVapor;

            var o2 = 0.0;
            if (CanHaveOxygen)
            {
                // Always at least some oxygen if there is water, planetary composition allowing
                o2 = waterVapor > 0
                    ? waterVapor * 0.0001
                    : Math.Max(0, Math.Round(Randomizer.Instance.NextDouble(-0.5, 0.5), 4));
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
                Atmosphere = new Atmosphere(this, Material.Empty(), 0);
            }
            else
            {
                var atmosphere = new Composite(
                    (Chemical.Helium, Phase.Gas, he),
                    (Chemical.Hydrogen, Phase.Gas, h));
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
                Atmosphere = new Atmosphere(this, atmosphere, Math.Round(Randomizer.Instance.NextDouble(25)));
            }
        }

        private protected override void GenerateGrid()
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

            GenerateGrid(size);
        }

        private void GenerateHydrocarbons()
        {
            // It is presumed that it is statistically likely that the current eon is not the first
            // with life, and therefore that some fossilized hydrocarbon deposits exist.
            var hydrocarbonProportion = Randomizer.Instance.NextDouble(0.05, 0.33333);

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

                HydrosphereProportion = mass / Mass;
                if (!HydrosphereProportion.IsZero())
                {
                    // Surface water is mostly salt water.
                    var saltWater = Math.Round(Randomizer.Instance.Normal(0.945, 0.015), 3);
                    _hydrosphere = new Composite(
                        (Chemical.Water, Phase.Liquid, 1 - saltWater),
                        (Chemical.Water_Salt, Phase.Liquid, saltWater));

                    // Salt water indicates sedimentary halite deposits.
                    AddResource(Chemical.Salt, 0.013, false);
                }
            }
            if (_hydrosphere == null)
            {
                _hydrosphere = Material.Empty();
            }
        }

        private double GenerateHydrosphereMass()
        {
            var mass = 0.0;

            var ratio = PlanetParams?.WaterRatio ?? Randomizer.Instance.NextDouble();

            if (ratio <= 0)
            {
                Terrain.SeaLevel = -Terrain.MaxElevation * 1.1;
            }
            else if (ratio >= 1 && (HasFlatSurface || Terrain.MaxElevation.IsZero()))
            {
                Terrain.SeaLevel = Terrain.MaxElevation * ratio;
                mass = new HollowSphere(Radius, Radius + Terrain.SeaLevel).Volume
                    - (new HollowSphere(Radius, Radius + Terrain.MaxElevation).Volume / 2);
            }
            else
            {
                if (ratio.IsEqualTo(0.5))
                {
                    Terrain.SeaLevel = 0;
                }
                else
                {
                    // Midway between the elevations of the first two tiles beyond the amount indicated by
                    // the ratio when ordered by elevation.
                    Terrain.SeaLevel = Grid.Tiles
                        .OrderBy(t => t.Elevation)
                        .Skip((Grid.Tiles.Length * ratio).RoundToInt())
                        .Take(2).Average(t => t.Elevation);
                }
                mass = Grid.Tiles.Where(t => t.Elevation < 0).Sum(x => x.Area * (Terrain.SeaLevel - x.Elevation));
            }

            if (!Terrain.SeaLevel.IsZero())
            {
                OffsetGridElevations((float)Terrain.SeaLevel);
            }

            ClassifyTerrain();

            return mass;
        }

        /// <summary>
        /// Determines whether this planet is capable of sustaining life, and whether or not it
        /// actually does. If so, the atmosphere may be adjusted.
        /// </summary>
        /// <returns>
        /// True if the atmosphere's greeenhouse potential is adjusted; false if not.
        /// </returns>
        private bool GenerateLife()
        {
            if (!IsHospitable || !IsHabitable())
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
            if (!hydrosphereSurface.ContainsSubstance(Chemical.Water, Phase.Liquid)
                && !hydrosphereSurface.ContainsSubstance(Chemical.Water_Salt, Phase.Liquid))
            {
                return false;
            }

            GenerateHydrocarbons();

            // If there is a habitable surface layer, it is presumed that an initial population of a
            // cyanobacteria analogue will produce a significant amount of free oxygen, which in turn
            // will transform most CH4 to CO2 and H2O, and also produce an ozone layer.
            var o2 = Randomizer.Instance.NextDouble(0.20, 0.25);
            Atmosphere.Composition.AddComponent(Chemical.Oxygen, Phase.Gas, o2);

            // Calculate ozone based on level of free oxygen.
            var o3 = Atmosphere.Composition.GetProportion(Chemical.Oxygen, Phase.Gas) * 4.5e-5;
            if (Atmosphere.Composition is LayeredComposite lc && lc.Layers.Count < 3)
            {
                Atmosphere.GetTroposphere(); // First ensure troposphere is differentiated.
                lc.CopyLayer(1, 0.01);
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

                Atmosphere.Composition.AddComponent(Chemical.Methane, Phase.Gas, ch4 * 0.001);

                Atmosphere.ResetGreenhouseFactor();
                return true;
            }

            return false;
        }

        private void GenerateOrbit(Orbiter orbitedObject, double semiMajorAxis, double trueAnomaly)
        {
            Orbit.SetOrbit(
                  this,
                  orbitedObject,
                  (1 - Eccentricity) * semiMajorAxis,
                  Eccentricity,
                  Math.Round(Randomizer.Instance.NextDouble(0.9), 4),
                  Math.Round(Randomizer.Instance.NextDouble(MathConstants.TwoPI), 4),
                  Math.Round(Randomizer.Instance.NextDouble(MathConstants.TwoPI), 4),
                  trueAnomaly);
            ResetCachedTemperatures();
        }

        private protected override void GenerateResources()
        {
            AddResources(Substance.Composition.GetSurface()
                .GetChemicals(Phase.Solid).Where(x => x.chemical is Metal)
                .Select(x => (x.chemical, x.proportion, true)));

            var uranium = Substance.Composition.GetSurface()
                .GetProportion(Chemical.Uranium, Phase.Solid);
            if (!uranium.IsZero())
            {
                AddResource(Chemical.Uranium, uranium, false);
            }

            // A magnetosphere is presumed to indicate tectonic, and hence volcanic, activity.
            // This, in turn, indicates elemental sulfur at the surface.
            if (HasMagnetosphere)
            {
                var sulfur = Substance.Composition.GetSurface()
                    .GetProportion(Chemical.Sulfur, Phase.Solid);
                if (!sulfur.IsZero())
                {
                    AddResource(Chemical.Sulfur, sulfur, false);
                }
            }
        }

        /// <summary>
        /// Generates a new satellite for this <see cref="Planetoid"/> with the specified parameters.
        /// </summary>
        /// <param name="periapsis">The periapsis of the satellite's orbit.</param>
        /// <param name="eccentricity">The eccentricity of the satellite's orbit.</param>
        /// <param name="maxMass">The maximum mass of the satellite.</param>
        /// <returns>A satellite <see cref="Planetoid"/> with an appropriate orbit.</returns>
        private protected override Planetoid GenerateSatellite(double periapsis, double eccentricity, double maxMass)
        {
            Planetoid satellite = null;
            var chance = Randomizer.Instance.NextDouble();

            // If the mass limit allows, there is an even chance that the satellite is a smaller planet.
            if (maxMass > _minMassForType && Randomizer.Instance.NextBoolean())
            {
                // Select from the standard distribution of types.

                // Planets with very low orbits are lava planets due to tidal stress (plus a small
                // percentage of others due to impact trauma).

                // The maximum mass and density are used to calculate an outer Roche limit (may not
                // be the actual Roche limit for the body which gets generated).
                if (periapsis < GetRocheLimit(_maxDensity) * 1.05 || chance <= 0.01)
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
            else if (maxMass > DwarfPlanet._minMassForType && Randomizer.Instance.NextBoolean())
            {
                // Dwarf planets with very low orbits are lava planets due to tidal stress (plus a small percentage of others due to impact trauma).
                if (periapsis < GetRocheLimit(DwarfPlanet._densityForType) * 1.05 || chance <= 0.01)
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
                    Math.Round(Randomizer.Instance.NextDouble(0.5), 4),
                    Math.Round(Randomizer.Instance.NextDouble(Math.PI * 2), 4),
                    Math.Round(Randomizer.Instance.NextDouble(Math.PI * 2), 4),
                    Math.Round(Randomizer.Instance.NextDouble(Math.PI * 2), 4));
            }

            return satellite;
        }

        private protected override IEnumerable<(IComposition, double)> GetCore(double mass)
        {
            yield return (GetIronNickelCore(), 1);
        }

        private protected override IEnumerable<(IComposition, double)> GetCrust()
        {
            // Rocky crust with trace elements
            // Metal content varies by approx. +/-15% from standard value in a Gaussian distribution.
            var metals = Math.Round(Randomizer.Instance.Normal(MetalProportion, 0.05 * MetalProportion), 4);

            var nickel = Math.Round(Randomizer.Instance.NextDouble(0.025, 0.075) * metals, 4);
            var aluminum = Math.Round(Randomizer.Instance.NextDouble(0.075, 0.225) * metals, 4);

            var titanium = Math.Round(Randomizer.Instance.NextDouble(0.05, 0.3) * metals, 4);

            var iron = metals - nickel - aluminum - titanium;

            var copper = titanium > 0 ? Math.Round(Randomizer.Instance.NextDouble(titanium), 4) : 0;
            titanium -= copper;

            var lead = titanium > 0 ? Math.Round(Randomizer.Instance.NextDouble(titanium), 4) : 0;
            titanium -= lead;

            var uranium = titanium > 0 ? Math.Round(Randomizer.Instance.NextDouble(titanium), 4) : 0;
            titanium -= uranium;

            var tin = titanium > 0 ? Math.Round(Randomizer.Instance.NextDouble(titanium), 4) : 0;
            titanium -= tin;

            var silver = titanium > 0 ? Math.Round(Randomizer.Instance.NextDouble(titanium), 4) : 0;
            titanium -= silver;

            var gold = titanium > 0 ? Math.Round(Randomizer.Instance.NextDouble(titanium), 4) : 0;
            titanium -= gold;

            var platinum = titanium > 0 ? Math.Round(Randomizer.Instance.NextDouble(titanium), 4) : 0;
            titanium -= platinum;

            var sulfur = Math.Round(Randomizer.Instance.Normal(3.5e-5, 0.05 * 3.5e-5), 4);

            var rock = 1 - metals - sulfur;

            yield return (new Composite(
                (Chemical.Aluminium, Phase.Solid, aluminum),
                (Chemical.Copper, Phase.Solid, copper),
                (Chemical.Gold, Phase.Solid, gold),
                (Chemical.Iron, Phase.Solid, iron),
                (Chemical.Lead, Phase.Solid, lead),
                (Chemical.Nickel, Phase.Solid, nickel),
                (Chemical.Platinum, Phase.Solid, platinum),
                (Chemical.Rock, Phase.Solid, rock),
                (Chemical.Silver, Phase.Solid, silver),
                (Chemical.Sulfur, Phase.Solid, sulfur),
                (Chemical.Tin, Phase.Solid, tin),
                (Chemical.Titanium, Phase.Solid, titanium),
                (Chemical.Uranium, Phase.Solid, uranium)),
                1);
        }

        /// <summary>
        /// Generates an appropriate density for this <see cref="Planetoid"/>.
        /// </summary>
        private protected override double? GetDensity()
        {
            if (PlanetParams?.Radius.HasValue == true && PlanetParams?.SurfaceGravity.HasValue == true)
            {
                return Mass.IsZero()
                    ? 0
                    : Shape.Volume.IsZero()
                        ? double.PositiveInfinity
                        : Mass / Shape.Volume;
            }
            else
            {
                return Math.Round(Randomizer.Instance.NextDouble(MinDensity, MaxDensity));
            }
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
        private double GetDistanceForTemperature(Star star, double temperature)
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

            return Math.Sqrt(star.Luminosity * (1 - Albedo)) / (Math.Pow(temperature, 4) * MathConstants.FourPI * ScienceConstants.sigma * areaRatio);
        }

        /// <summary>
        /// Determines whether this <see cref="Planetoid"/> has a strong magnetosphere.
        /// </summary>
        private protected override bool GetHasMagnetosphere() => PlanetParams?.HasMagnetosphere ?? base.GetHasMagnetosphere();

        private protected override IEnumerable<(IComposition, double)> GetMantle(IShape shape, double proportion)
        {
            yield return (new Material(Chemical.Rock, Phase.Liquid), 1);
        }

        private protected override double GetMass(IShape shape = null)
        {
            var minMass = MinMass;
            var maxMass = TMath.IsZero(MaxMass) ? null : (double?)MaxMass;

            if (Parent is StarSystem && Position != Vector3.Zero && (Orbit == null || Orbit.OrbitedObject is Star))
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

            if (_gravity.HasValue)
            {
                var mass = GetMassForSurfaceGravity(shape, _gravity.Value);
                return Math.Max(minMass, Math.Min(maxMass ?? double.PositiveInfinity, mass));
            }
            else
            {
                return Math.Round(Randomizer.Instance.NextDouble(minMass, maxMass ?? minMass));
            }
        }

        private protected override (double, IShape) GetMassAndShape()
        {
            if (PlanetParams?.SurfaceGravity.HasValue == true)
            {
                _gravity = PlanetParams.SurfaceGravity.Value;
            }
            else if (HabitabilityRequirements?.MinimumGravity.HasValue == true
                || HabitabilityRequirements?.MaximumGravity.HasValue == true)
            {
                double maxGravity = 0;
                if (HabitabilityRequirements?.MaximumGravity.HasValue == true)
                {
                    maxGravity = HabitabilityRequirements.Value.MaximumGravity.Value;
                }
                else // Determine the absolute maximum gravity a terrestrial planet could have, before it would become a giant.
                {
                    var maxMass = MaxMassForType ?? _maxMassForType;
                    var maxVolume = maxMass / Density;
                    var maxRadius = Math.Pow(maxVolume / MathConstants.FourThirdsPI, 1.0 / 3.0);
                    maxGravity = ScienceConstants.G * maxMass / (maxRadius * maxRadius);
                }
                _gravity = Randomizer.Instance.NextDouble(HabitabilityRequirements?.MinimumGravity ?? 0, maxGravity);
            }

            if (PlanetParams?.Radius.HasValue == true)
            {
                var shape = GetShape(knownRadius: Math.Max(MinimumRadius, PlanetParams.Radius.Value));
                return (GetMass(shape), shape);
            }
            else if (PlanetParams?.SurfaceGravity.HasValue == true
                || HabitabilityRequirements?.MinimumGravity.HasValue == true
                || HabitabilityRequirements?.MaximumGravity.HasValue == true)
            {
                var shape = GetShape(knownRadius: Math.Max(MinimumRadius, Math.Min(GetRadiusForSurfaceGravity(_gravity.Value), GetMaxRadius())));
                return (GetMass(shape), shape);
            }
            else
            {
                var mass = GetMass();
                return (mass, GetShape(mass));
            }
        }

        /// <summary>
        /// Calculates the mass required to produce the given surface gravity, if a shape is already
        /// defined.
        /// </summary>
        /// <param name="shape">The defined shape.</param>
        /// <param name="gravity">The desired surface gravity, in m/s².</param>
        /// <returns>The mass required to produce the given surface gravity, in kg.</returns>
        private double GetMassForSurfaceGravity(IShape shape, double gravity)
            => gravity * shape.ContainingRadius * shape.ContainingRadius / ScienceConstants.G;

        /// <summary>
        /// Determines a rotational period for this <see cref="Planetoid"/>.
        /// </summary>
        private protected override double GetRotationalPeriod()
            => PlanetParams?.RotationalPeriod.HasValue == true
                ? Math.Max(0, PlanetParams.RotationalPeriod.Value)
                : base.GetRotationalPeriod();

        /// <summary>
        /// Calculates the radius required to produce the given surface gravity, if mass is already defined.
        /// </summary>
        /// <param name="gravity">The desired surface gravity, in m/s².</param>
        /// <returns>The radius required to produce the given surface gravity, in meters.</returns>
        private double GetRadiusForSurfaceGravity(double gravity) => Math.Sqrt(Mass * ScienceConstants.G / gravity);

        /// <summary>
        /// Calculates the temperature at which this <see cref="TerrestrialPlanet"/> will retain only
        /// a minimal atmosphere of out-gassed volatiles (comparable to Mercury).
        /// </summary>
        /// <returns>A temperature, in K.</returns>
        /// <remarks>
        /// If the planet is not massive enough or too hot to hold onto carbon dioxide gas, it is
        /// presumed that it will have a minimal atmosphere of out-gassed volatiles (comparable to Mercury).
        /// </remarks>
        private double GetTempForThinAtmosphere() => ScienceConstants.TwoG * Mass * 7.0594833834763e-5 / Radius;

        /// <summary>
        /// Determines if this <see cref="TerrestrialPlanet"/> is "habitable," defined as possessing
        /// liquid water. Does not rule out exotic lifeforms which subsist in non-aqueous
        /// environments. Also does not imply habitability by any particular creatures (e.g. humans),
        /// which may also depend on stricter criteria (e.g. atmospheric conditions).
        /// </summary>
        /// <returns><see langword="true"/> if this planet fits this minimal definition of
        /// "habitable;" otherwise <see langword="false"/>.</returns>
        private bool IsHabitable()
            => Hydrosphere.ContainsSubstance(Chemical.Water, Phase.Liquid)
            || Hydrosphere.ContainsSubstance(Chemical.Water_Salt, Phase.Liquid);

        private void OffsetGridElevations(float seaLevel)
        {
            foreach (var t in Grid.Tiles)
            {
                t.Elevation -= seaLevel;
            }
            foreach (var c in Grid.Corners)
            {
                c.Elevation -= seaLevel;
            }
        }

        private protected override void ResetCachedTemperatures()
        {
            base.ResetCachedTemperatures();
            _climateSet = false;
        }

        private void SetClimate()
        {
            var seasons = new List<Season>();

            // For planets not in orbit, a single season representing the current moment is all that
            // can be generated.
            if (Orbit == null)
            {
                seasons.Add(new Season(this, 1, 0));
            }
            else
            {
                // A year of 12 seasons is pre-generated to provide climate data.

                const double seasonAngle = Math.PI / 6;
                const double proportionOfYear = 1.0 / 12;

                var seasonTrueAnomaly = WinterSolsticeTrueAnomaly;

                for (uint i = 0; i < 12; i++)
                {
                    seasons.Add(new Season(this, proportionOfYear, seasonTrueAnomaly));
                    seasonTrueAnomaly += seasonAngle;
                    if (seasonTrueAnomaly >= MathConstants.TwoPI)
                    {
                        seasonTrueAnomaly -= MathConstants.TwoPI;
                    }
                }
            }

            for (var i = 0; i < Grid.Tiles.Length; i++)
            {
                var t = Grid.Tiles[i];
                Grid.Tiles[i].SetClimate(seasons);
                t.SeaIce = GetSeaIceRange(t.Temperature, t.Latitude, t.Elevation);
                t.SnowCover = GetSnowCoverRange(t.Temperature, t.Latitude, t.Elevation, t.HumidityType);
            }

            SetRiverFlows();

            _climateSet = true;
        }

        private void SetHydrosphereProportion(
            Chemical chemical,
            Phase phase,
            double proportion,
            ref double hydrosphereAtmosphereRatio)
        {
            var newTotalProportion = proportion;
            if (Hydrosphere is LayeredComposite lc)
            {
                newTotalProportion = lc.Layers[lc.Layers.Count - 1].proportion * proportion;
            }
            HydrosphereProportion +=
                HydrosphereProportion * (newTotalProportion - Hydrosphere.GetProportion(chemical, phase));

            if (Hydrosphere is LayeredComposite lc2)
            {
                lc2.AddToLayer(lc2.Layers.Count - 1, chemical, phase, proportion);
            }
            else
            {
                Hydrosphere.SetProportion(chemical, phase, proportion);
            }

            hydrosphereAtmosphereRatio = GetHydrosphereAtmosphereRatio();
        }

        private void SetRiverFlows()
        {
            var cornerFlows = new Dictionary<int, float>();
            var endpoints = new SortedSet<Corner>(Comparer<Corner>.Create((c1, c2) => c2.Elevation.CompareTo(c1.Elevation) * -1));

            for (var i = 0; i < Grid.Tiles.Length; i++)
            {
                var t = Grid.Tiles[i];
                if (!t.IsLand)
                {
                    continue;
                }

                var runoff = (float)(t.Precipitation * 0.001 * t.Area / Orbit.Period);
                if (runoff.IsZero())
                {
                    continue;
                }

                var lowest = t.GetLowestCorner(Grid);

                cornerFlows[lowest.Index] = runoff;

                endpoints.Add(lowest);
            }

            Corner prev = null;
            while (endpoints.Count > 0)
            {
                var c = endpoints.First();
                endpoints.Remove(c);

                var index = Array.Find(Grid.Edges, x => x.RiverSource == c.Index)?.RiverDirection ?? -1;
                var next = index == -1 ? null : Grid.Corners[index];
                if (next == null)
                {
                    next = c.GetLowestCorner(Grid, true);
                    if (next.Elevation > c.Elevation)
                    {
                        next = c.GetLowestCorner(Grid, false);
                    }
                    if (next.Elevation > c.Elevation && (prev?.LakeDepth ?? 0) == 0)
                    {
                        c.LakeDepth = Math.Min(
                            c.Corners.Min(x => Grid.Corners[x].Elevation),
                            c.Tiles.Min(x => Grid.Tiles[x].Elevation)) - c.Elevation;
                    }
                }

                if ((prev?.LakeDepth ?? 0) == 0 || c.LakeDepth + c.Elevation >= next.Elevation)
                {
                    var edgeIndex = c.Edges[c.IndexOfCorner(next.Index)];
                    var edge = Grid.Edges[edgeIndex];
                    edge.RiverSource = c.Index;

                    cornerFlows.TryGetValue(c.Index, out var flow);
                    flow += c.Edges
                        .Where(e => Grid.Edges[e].RiverDirection == c.Index)
                        .Sum(e => Grid.Edges[e].RiverFlow);
                    Grid.Edges[edgeIndex].RiverFlow = flow;

                    if (!flow.IsZero())
                    {
                        var rc = next;
                        var visited = new List<int>();
                        var nextRiverEdge = -1;
                        do
                        {
                            if (nextRiverEdge != -1)
                            {
                                visited.Add(nextRiverEdge);
                            }
                            var nextRiverEdges = rc.Edges.Where(e => Grid.Edges[e].RiverSource == rc.Index).ToList();
                            if (nextRiverEdges.Count > 0)
                            {
                                nextRiverEdge = nextRiverEdges[0];
                                Grid.Edges[nextRiverEdge].RiverFlow += flow;
                                rc = Grid.Corners[Grid.Edges[nextRiverEdge].RiverDirection];
                            }
                            else
                            {
                                nextRiverEdge = -1;
                            }
                        } while (nextRiverEdge != -1 && !visited.Contains(nextRiverEdge));
                    }

                    if (next.Elevation > 0
                        && next.LakeDepth == 0
                        && !endpoints.Contains(next)
                        && !next.Edges.Any(e => Grid.Edges[e].RiverSource == next.Index))
                    {
                        endpoints.Add(next);
                    }
                }

                prev = c;
            }
        }
    }
}
