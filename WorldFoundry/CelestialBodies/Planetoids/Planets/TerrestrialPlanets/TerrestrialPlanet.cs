using NeverFoundry.MathAndScience;
using NeverFoundry.MathAndScience.Chemistry;
using NeverFoundry.MathAndScience.Constants.Numbers;
using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.MathAndScience.Randomization;
using NeverFoundry.MathAndScience.Time;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Permissions;
using WorldFoundry.CelestialBodies.Planetoids.Asteroids;
using WorldFoundry.CelestialBodies.Planetoids.Planets.DwarfPlanets;
using WorldFoundry.CelestialBodies.Stars;
using WorldFoundry.Climate;
using WorldFoundry.Place;
using WorldFoundry.Space;
using WorldFoundry.Space.Galaxies;
using WorldFoundry.WorldGrids;

namespace WorldFoundry.CelestialBodies.Planetoids.Planets.TerrestrialPlanets
{
    /// <summary>
    /// A primarily rocky planet, relatively small in comparison to gas and ice giants.
    /// </summary>
    [Serializable]
    public class TerrestrialPlanet : Planemo
    {
        internal static readonly Number Space = new Number(1.75, 7);

        /// <summary>
        /// Indicates whether or not this planet has a native population of living organisms.
        /// </summary>
        /// <remarks>
        /// The complexity of life is not presumed. If a planet is basically habitable (liquid
        /// surface water), life in at least a single-celled form may be indicated, and may affect
        /// the atmospheric composition.
        /// </remarks>
        public bool HasBiosphere { get; set; }

        private protected IMaterial? _hydrosphere;
        /// <summary>
        /// This planet's surface liquids and ices (not necessarily water).
        /// </summary>
        /// <remarks>
        /// Represented as a separate <see cref="IMaterial"/> rather than as a top layer of <see
        /// cref="CelestialLocation.Material"/> for ease of reference to both the soliud surface
        /// layer, and the hydrosphere.
        /// </remarks>
        public IMaterial Hydrosphere
        {
            get
            {
                if (_hydrosphere is null)
                {
                    GenerateHydrosphere();
                    if (_hydrosphere is null)
                    {
                        _hydrosphere = NeverFoundry.MathAndScience.Chemistry.Material.Empty;
                    }
                }
                return _hydrosphere;
            }
        }

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
        }

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
        /// <param name="parent">
        /// The containing <see cref="Location"/> in which this <see
        /// cref="TerrestrialPlanet"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="TerrestrialPlanet"/>.</param>
        internal TerrestrialPlanet(Location? parent, Vector3 position) : base(parent, position) { }

        /// <summary>
        /// Initializes a new instance of <see cref="TerrestrialPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="Location"/> in which this <see cref="TerrestrialPlanet"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="TerrestrialPlanet"/>.</param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="TerrestrialPlanet"/> during random generation, in kg.
        /// </param>
        internal TerrestrialPlanet(Location? parent, Vector3 position, Number maxMass) : base(parent, position, maxMass) { }

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
            List<Location>? children,
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
                children,
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
            (double?)info.GetValue(nameof(Albedo), typeof(double?)),
            (double?)info.GetValue(nameof(SurfaceAlbedo), typeof(double?)),
            (Vector3)info.GetValue(nameof(Velocity), typeof(Vector3)),
            (double)info.GetValue(nameof(_normalizedSeaLevel), typeof(double)),
            (int)info.GetValue(nameof(_seed1), typeof(int)),
            (int)info.GetValue(nameof(_seed2), typeof(int)),
            (int)info.GetValue(nameof(_seed3), typeof(int)),
            (int)info.GetValue(nameof(_seed4), typeof(int)),
            (int)info.GetValue(nameof(_seed5), typeof(int)),
            (double?)info.GetValue(nameof(AngleOfRotation), typeof(double?)),
            (Atmosphere?)info.GetValue(nameof(Atmosphere), typeof(Atmosphere)),
            (double?)info.GetValue(nameof(AxialPrecession), typeof(double?)),
            (bool?)info.GetValue(nameof(HasMagnetosphere), typeof(bool?)),
            (double?)info.GetValue(nameof(MaxElevation), typeof(double?)),
            (Number?)info.GetValue(nameof(RotationalOffset), typeof(Number?)),
            (Number?)info.GetValue(nameof(RotationalPeriod), typeof(Number?)),
            (List<Resource>?)info.GetValue(nameof(Resources), typeof(List<Resource>)),
            (List<string>?)info.GetValue(nameof(Satellites), typeof(List<string>)),
            (List<SurfaceRegion>?)info.GetValue(nameof(SurfaceRegions), typeof(List<SurfaceRegion>)),
            (Number?)info.GetValue(nameof(MaxMass), typeof(Number?)),
            (Orbit?)info.GetValue(nameof(Orbit), typeof(Orbit?)),
            (IMaterial?)info.GetValue(nameof(Material), typeof(IMaterial)),
            (IMaterial?)info.GetValue(nameof(Hydrosphere), typeof(IMaterial)),
            (List<PlanetaryRing>?)info.GetValue(nameof(Rings), typeof(List<PlanetaryRing>)),
            (List<Location>)info.GetValue(nameof(Children), typeof(List<Location>)),
            (byte[])info.GetValue(nameof(_depthMap), typeof(byte[])),
            (byte[])info.GetValue(nameof(_elevationMap), typeof(byte[])),
            (byte[])info.GetValue(nameof(_flowMap), typeof(byte[])),
            (byte[][])info.GetValue(nameof(_precipitationMaps), typeof(byte[][])),
            (byte[][])info.GetValue(nameof(_snowfallMaps), typeof(byte[][])),
            (byte[])info.GetValue(nameof(_temperatureMapSummer), typeof(byte[])),
            (byte[])info.GetValue(nameof(_temperatureMapWinter), typeof(byte[])),
            (double?)info.GetValue(nameof(_maxFlow), typeof(double?))) { }

        /// <summary>
        /// Given a star, generates a terrestrial planet with the given parameters, and puts the
        /// planet in orbit around the star.
        /// </summary>
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
        public static TerrestrialPlanet GetPlanetForStar(
            Star star,
            TerrestrialPlanetParams? planetParams = null,
            HabitabilityRequirements? habitabilityRequirements = null)
        {
            TerrestrialPlanet planet;
            var pParams = planetParams ?? TerrestrialPlanetParams.FromDefaults();
            var requirements = habitabilityRequirements ?? HabitabilityRequirements.HumanHabitabilityRequirements;
            var sanityCheck = 0;
            do
            {
                planet = new TerrestrialPlanet(star.Parent, Vector3.Zero);
                if (pParams.NumSatellites.HasValue)
                {
                    planet._maxSatellites = pParams.NumSatellites.Value;
                }
                if (pParams.HasMagnetosphere.HasValue)
                {
                    planet.HasMagnetosphere = pParams.HasMagnetosphere.Value;
                }
                planet.Material = planet.GetMaterial(pParams, requirements);
                if (planet._hydrosphere is null)
                {
                    double surfaceTemp;
                    if (pParams.SurfaceTemperature.HasValue)
                    {
                        surfaceTemp = pParams.SurfaceTemperature!.Value;
                    }
                    else if (requirements.MinimumTemperature.HasValue)
                    {
                        surfaceTemp = requirements.MaximumTemperature.HasValue
                            ? (requirements.MinimumTemperature!.Value
                                + requirements.MaximumTemperature!.Value)
                                / 2
                            : requirements.MinimumTemperature!.Value;
                    }
                    else
                    {
                        surfaceTemp = planet.AverageBlackbodyTemperature;
                    }
                    planet.GenerateHydrosphere(pParams, surfaceTemp);
                }
                planet.GenerateOrbit(star, pParams, requirements);

                sanityCheck++;
                if (planet.IsHabitable(requirements, out _))
                {
                    break;
                }
                else if (sanityCheck < 100)
                {
                    planet.Parent = null;
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
        public static TerrestrialPlanet? GetPlanetForGalaxy(
            Galaxy galaxy,
            TerrestrialPlanetParams? planetParams = null,
            HabitabilityRequirements? habitabilityRequirements = null)
        {
            StarSystem? system;
            var sanityCheck = 10000;
            do
            {
                system = galaxy?.GenerateChild(new ChildDefinition(typeof(StarSystem), StarSystem.Space, 1, typeof(Star), SpectralClass.G, LuminosityClass.V)) as StarSystem;
                sanityCheck--;
            } while (sanityCheck > 0 && (system?.Stars.Skip(1).Any() != false)); // Prevent binary systems, which will interfere with the temperature-balancing logic.
            var star = system?.Stars.FirstOrDefault();
            return star != null
                ? GetPlanetForStar(star, planetParams, habitabilityRequirements)
                : null;
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
        public static TerrestrialPlanet? GetPlanetForUniverse(
            Universe universe,
            TerrestrialPlanetParams? planetParams = null,
            HabitabilityRequirements? habitabilityRequirements = null)
        {
            var gsc = universe?.GenerateChild(new ChildDefinition(typeof(GalaxySupercluster), GalaxySupercluster.Space, 1)) as GalaxySupercluster;
            var gc = gsc?.GenerateChild(new ChildDefinition(typeof(GalaxyCluster), GalaxyCluster.Space, 1)) as GalaxyCluster;
            var gg = gc?.GenerateChild(new ChildDefinition(typeof(GalaxyGroup), GalaxyGroup.Space, 1)) as GalaxyGroup;
            gg?.PrepopulateRegion();
            return !(gg?.CelestialChildren.FirstOrDefault(x => x is GalaxySubgroup) is GalaxySubgroup gsg)
                ? null
                : GetPlanetForGalaxy(gsg.MainGalaxy, planetParams, habitabilityRequirements);
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
        public static TerrestrialPlanet? GetPlanetForNewUniverse(
            TerrestrialPlanetParams? planetParams = null,
            HabitabilityRequirements? habitabilityRequirements = null)
            => GetPlanetForUniverse(new Universe(), planetParams, habitabilityRequirements);

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
            info.AddValue(nameof(Albedo), _albedo);
            info.AddValue(nameof(SurfaceAlbedo), _surfaceAlbedo);
            info.AddValue(nameof(Velocity), Velocity);
            info.AddValue(nameof(_normalizedSeaLevel), _normalizedSeaLevel);
            info.AddValue(nameof(_seed1), _seed1);
            info.AddValue(nameof(_seed2), _seed2);
            info.AddValue(nameof(_seed3), _seed3);
            info.AddValue(nameof(_seed4), _seed4);
            info.AddValue(nameof(_seed5), _seed5);
            info.AddValue(nameof(AngleOfRotation), _angleOfRotation);
            info.AddValue(nameof(Atmosphere), _atmosphere);
            info.AddValue(nameof(AxialPrecession), _axialPrecession);
            info.AddValue(nameof(HasMagnetosphere), _hasMagnetosphere);
            info.AddValue(nameof(MaxElevation), _maxElevation);
            info.AddValue(nameof(RotationalOffset), _rotationalOffset);
            info.AddValue(nameof(RotationalPeriod), _rotationalPeriod);
            info.AddValue(nameof(Resources), _resources);
            info.AddValue(nameof(Satellites), _satelliteIDs);
            info.AddValue(nameof(SurfaceRegions), _surfaceRegions);
            info.AddValue(nameof(MaxMass), _maxMass);
            info.AddValue(nameof(Orbit), _orbit);
            info.AddValue(nameof(Material), _material);
            info.AddValue(nameof(Hydrosphere), _hydrosphere);
            info.AddValue(nameof(Rings), _rings);
            info.AddValue(nameof(Children), Children.ToList());
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
        /// <param name="snow">
        /// <para>
        /// When the method returns, will be set to the amount of snow which falls. Note that this
        /// amount <i>replaces</i> any precipitation that would have fallen as rain; the return
        /// value is to be considered a water-equivalent total value which is equal to the snow.
        /// </para>
        /// </param>
        /// <returns>The average precipitation at the given <paramref name="position"/> and time of
        /// year, in mm.</returns>
        public double GetPrecipitation(Duration time, Vector3 position, float proportionOfYear, out double snow)
        {
            var trueAnomaly = Orbit?.GetTrueAnomalyAtTime(time) ?? 0;
            var seasonalLatitude = Math.Abs(GetSeasonalLatitude(VectorToLatitude(position), trueAnomaly));
            return GetPrecipitation(
                (double)position.X,
                (double)position.Y,
                (double)position.Z,
                seasonalLatitude,
                (float)GetTemperatureAtElevation(
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
        public double GetPrecipitation(Vector3 position, float proportionOfYear, out double snow)
            => GetPrecipitation(ContainingUniverse?.Time.Now ?? Duration.Zero, position, proportionOfYear, out snow);

        /// <summary>
        /// Determines if the planet is habitable by a species with the given requirements. Does not
        /// imply that the planet could sustain a large-scale population in the long-term, only that
        /// a member of the species can survive on the surface without artificial aid.
        /// </summary>
        /// <param name="habitabilityRequirements">The collection of <see
        /// cref="HabitabilityRequirements"/>.</param>
        /// <param name="reason">
        /// Set to an <see cref="UninhabitabilityReason"/> indicating the reason(s) the planet is
        /// uninhabitable.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if this planet is habitable by a species with the given
        /// requirements; <see langword="false"/>
        /// otherwise.
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

        internal override void GenerateOrbit(CelestialLocation orbitedObject)
            => GenerateOrbit(orbitedObject, null, null);

        private void AdjustOrbitForTemperature(TerrestrialPlanetParams? planetParams, Star star, Number? semiMajorAxis, double trueAnomaly, double targetTemp)
        {
            // Orbital distance averaged over time (mean anomaly) = semi-major axis * (1 + eccentricity^2 / 2).
            // This allows calculation of the correct distance/orbit for an average
            // orbital temperature (rather than the temperature at the current position).
            if (planetParams?.RevolutionPeriod.HasValue == true && semiMajorAxis.HasValue)
            {
                star.SetTempForTargetPlanetTemp(targetTemp - (Temperature ?? 0), semiMajorAxis.Value * (1 + (Eccentricity * Eccentricity / 2)), Albedo);
            }
            else
            {
                semiMajorAxis = GetDistanceForTemperature(star, targetTemp - (Temperature ?? 0)) / (1 + (Eccentricity * Eccentricity / 2));
                GenerateOrbit(star, semiMajorAxis.Value, trueAnomaly);
            }
            ResetCachedTemperatures();
        }

        private void CalculateGasPhaseMix(
            TerrestrialPlanetParams? planetParams,
            IHomogeneousReference substance,
            double surfaceTemp,
            ref double adjustedAtmosphericPressure)
        {
            var proportionInHydrosphere = Hydrosphere.GetProportion(substance);
            var water = Substances.GetChemicalReference(Substances.Chemicals.Water);
            var isWater = substance.Equals(water);
            if (isWater)
            {
                proportionInHydrosphere = Hydrosphere.GetProportion(x =>
                    x.Equals(Substances.GetSolutionReference(Substances.Solutions.Seawater))
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
                CheckCO2Reduction(vaporPressure);
            }
        }

        private void CalculatePhases(TerrestrialPlanetParams? planetParams, int counter, ref double adjustedAtmosphericPressure)
        {
            var surfaceTemp = AverageSurfaceTemperature;

            // Despite the theoretical possibility of an atmosphere cold enough to precipitate some
            // of the noble gases, or hydrogen, they are ignored and presumed to exist always as
            // trace atmospheric gases, never surface liquids or ices, or in large enough quantities
            // to form precipitation.

            var methane = Substances.GetChemicalReference(Substances.Chemicals.Methane);
            CalculateGasPhaseMix(planetParams, methane, surfaceTemp, ref adjustedAtmosphericPressure);

            var carbonMonoxide = Substances.GetChemicalReference(Substances.Chemicals.CarbonMonoxide);
            CalculateGasPhaseMix(planetParams, carbonMonoxide, surfaceTemp, ref adjustedAtmosphericPressure);

            var carbonDioxide = Substances.GetChemicalReference(Substances.Chemicals.CarbonDioxide);
            CalculateGasPhaseMix(planetParams, carbonDioxide, surfaceTemp, ref adjustedAtmosphericPressure);

            var nitrogen = Substances.GetChemicalReference(Substances.Chemicals.Nitrogen);
            CalculateGasPhaseMix(planetParams, nitrogen, surfaceTemp, ref adjustedAtmosphericPressure);

            var oxygen = Substances.GetChemicalReference(Substances.Chemicals.Oxygen);
            CalculateGasPhaseMix(planetParams, oxygen, surfaceTemp, ref adjustedAtmosphericPressure);

            // No need to check for ozone, since it is only added to atmospheres on planets with
            // liquid surface water, which means temperatures too high for liquid or solid ozone.

            var sulphurDioxide = Substances.GetChemicalReference(Substances.Chemicals.SulphurDioxide);
            CalculateGasPhaseMix(planetParams, sulphurDioxide, surfaceTemp, ref adjustedAtmosphericPressure);

            // Water is handled differently, since the planet may already have surface water.
            var water = Substances.GetChemicalReference(Substances.Chemicals.Water);
            var seawater = Substances.GetSolutionReference(Substances.Solutions.Seawater);
            if (Hydrosphere.Contains(water)
                || Hydrosphere.Contains(seawater)
                || Atmosphere.Material.Contains(water))
            {
                CalculateGasPhaseMix(planetParams, water, surfaceTemp, ref adjustedAtmosphericPressure);
            }

            // Ices and clouds significantly impact albedo.
            var pressure = adjustedAtmosphericPressure;
            var iceAmount = (double)Math.Min(1,
                Hydrosphere.GetSurface()?
                .Constituents.Sum(x => x.substance.Substance.SeparateByPhase(surfaceTemp, pressure, PhaseType.Solid).First().proportion * x.proportion) ?? 0);
            var cloudCover = Atmosphere.AtmosphericPressure
                * (double)Atmosphere.Material.Constituents.Sum(x => x.substance.Substance.SeparateByPhase(surfaceTemp, pressure, PhaseType.Solid | PhaseType.Liquid).First().proportion * x.proportion / 100);
            var reflectiveSurface = Math.Max(iceAmount, cloudCover);
            Albedo = (SurfaceAlbedo * (1 - reflectiveSurface)) + (0.9 * reflectiveSurface);

            // An albedo change might significantly alter surface temperature, which may require a
            // re-calculation (but not too many). 5K is used as the threshold for re-calculation,
            // which may lead to some inaccuracies, but should avoid over-repetition for small changes.
            if (counter < 10 && Math.Abs(surfaceTemp - AverageSurfaceTemperature) > 5)
            {
                CalculatePhases(planetParams, counter + 1, ref adjustedAtmosphericPressure);
            }
        }

        private void CheckCO2Reduction(double vaporPressure)
        {
            // At least 1% humidity leads to a reduction of CO2 to a trace gas, by a presumed
            // carbon-silicate cycle.

            var water = Substances.GetChemicalReference(Substances.Chemicals.Water);
            var air = Atmosphere.Material is LayeredComposite lc
                ? lc.Layers[0].material
                : Atmosphere.Material;
            if ((double)(air?.GetProportion(water) ?? 0) * Atmosphere.AtmosphericPressure >= 0.01 * vaporPressure)
            {
                var carbonDioxide = Substances.GetChemicalReference(Substances.Chemicals.CarbonDioxide);
                var co2 = air?.GetProportion(carbonDioxide) ?? 0;
                if (co2 >= 1e-3m) // reduce CO2 if not already trace
                {
                    co2 = Randomizer.Instance.NextDecimal(15e-6m, 0.001m);

                    // Replace most of the CO2 with inert gases.
                    var nitrogen = Substances.GetChemicalReference(Substances.Chemicals.Nitrogen);
                    var n2 = Atmosphere.Material.GetProportion(nitrogen) + Atmosphere.Material.GetProportion(carbonDioxide) - co2;
                    Atmosphere.Material.AddConstituent(carbonDioxide, co2);

                    // Some portion of the N2 may be Ar instead.
                    var argon = Substances.GetChemicalReference(Substances.Chemicals.Argon);
                    var ar = Math.Max(Atmosphere.Material.GetProportion(argon), n2 * Randomizer.Instance.NextDecimal(-0.02m, 0.04m));
                    Atmosphere.Material.AddConstituent(argon, ar);
                    n2 -= ar;

                    // An even smaller fraction may be Kr.
                    var krypton = Substances.GetChemicalReference(Substances.Chemicals.Krypton);
                    var kr = Math.Max(Atmosphere.Material.GetProportion(krypton), n2 * Randomizer.Instance.NextDecimal(-25e-5m, 0.0005m));
                    Atmosphere.Material.AddConstituent(krypton, kr);
                    n2 -= kr;

                    // An even smaller fraction may be Xe or Ne.
                    var xenon = Substances.GetChemicalReference(Substances.Chemicals.Xenon);
                    var xe = Math.Max(Atmosphere.Material.GetProportion(xenon), n2 * Randomizer.Instance.NextDecimal(-18e-6m, 35e-6m));
                    Atmosphere.Material.AddConstituent(xenon, xe);
                    n2 -= xe;

                    var neon = Substances.GetChemicalReference(Substances.Chemicals.Neon);
                    var ne = Math.Max(Atmosphere.Material.GetProportion(neon), n2 * Randomizer.Instance.NextDecimal(-18e-6m, 35e-6m));
                    Atmosphere.Material.AddConstituent(neon, ne);
                    n2 -= ne;

                    Atmosphere.Material.AddConstituent(nitrogen, n2);

                    Atmosphere.ResetGreenhouseFactor(this);
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
            var water = Substances.GetChemicalReference(Substances.Chemicals.Water);

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
                            var oxygen = Substances.GetChemicalReference(Substances.Chemicals.Oxygen);
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

            var water = Substances.GetChemicalReference(Substances.Chemicals.Water);
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
                var seawater = Substances.GetSolutionReference(Substances.Solutions.Seawater);
                Hydrosphere.GetSurface().RemoveConstituent(seawater);

                // It is presumed that photodissociation will eventually reduce the amount of water
                // vapor to a trace gas (the H2 will be lost due to atmospheric escape, and the
                // oxygen will be lost to surface oxidation).
                var waterVapor = Math.Min(gasProportion, Randomizer.Instance.NextDecimal(0.001m));
                gasProportion = waterVapor;

                var oxygen = Substances.GetChemicalReference(Substances.Chemicals.Oxygen);
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
            var seawater = Substances.GetSolutionReference(Substances.Solutions.Seawater);
            var water = Substances.GetChemicalReference(Substances.Chemicals.Water);

            var seawaterProportion = Hydrosphere.GetProportion(seawater);
            var waterProportion = 1 - seawaterProportion;

            var depth = SeaLevel + (MaxElevation / 2);
            if (depth > 0)
            {
                var stateTop = CelestialSubstances.SeawaterMeltingPoint <= temperature
                    ? PhaseType.Liquid
                    : PhaseType.Solid;

                var tempBottom = depth > 1000
                    ? 277
                    : depth < 200
                        ? temperature
                        : temperature.Lerp(277, (depth - 200) / 800);
                var stateBottom = CelestialSubstances.SeawaterMeltingPoint <= tempBottom
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

        private protected override void GenerateAlbedo()
        {
            Albedo = Randomizer.Instance.NextDouble(0.1, 0.6);
            _surfaceAlbedo = Albedo;
        }

        private protected override void GenerateAngleOfRotation() => GenerateAngleOfRotation(null);

        private void GenerateAngleOfRotation(TerrestrialPlanetParams? planetParams)
        {
            if (planetParams?.AxialTilt.HasValue != true)
            {
                base.GenerateAngleOfRotation();
            }
            else
            {
                _axialPrecession = Randomizer.Instance.NextDouble(NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.TwoPI);
                var axialTilt = planetParams!.Value.AxialTilt!.Value;
                if (Orbit.HasValue)
                {
                    axialTilt += Orbit.Value.Inclination;
                }
                SetAngleOfRotation(axialTilt);
            }
        }

        private protected override void GenerateAtmosphere() => GenerateAtmosphere(null, null);

        private void GenerateAtmosphere(TerrestrialPlanetParams? planetParams, HabitabilityRequirements? habitabilityRequirements)
        {
            if (AverageBlackbodyTemperature >= GetTempForThinAtmosphere())
            {
                GenerateAtmosphereTrace();
            }
            else
            {
                GenerateAtmosphereThick(planetParams, habitabilityRequirements);
            }

            var adjustedAtmosphericPressure = Atmosphere.AtmosphericPressure;

            var water = Substances.GetChemicalReference(Substances.Chemicals.Water);
            var seawater = Substances.GetSolutionReference(Substances.Solutions.Seawater);
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
                    surfaceTemp = AverageBlackbodyTemperature;
                }
                CalculateGasPhaseMix(
                    planetParams,
                    water,
                    surfaceTemp,
                    ref adjustedAtmosphericPressure);

                // Recalculate temperatures based on the new atmosphere.
                ResetCachedTemperatures();

                FractionHydrophere(AverageSurfaceTemperature);

                // Recalculate the phases of water based on the new temperature.
                CalculateGasPhaseMix(
                    planetParams,
                    water,
                    AverageSurfaceTemperature,
                    ref adjustedAtmosphericPressure);

                // If life alters the greenhouse potential, temperature and water phase must be
                // recalculated once again.
                if (GenerateLife())
                {
                    CalculateGasPhaseMix(
                        planetParams,
                        water,
                        AverageSurfaceTemperature,
                        ref adjustedAtmosphericPressure);
                    ResetCachedTemperatures();
                    FractionHydrophere(AverageSurfaceTemperature);
                }
            }
            else
            {
                // Recalculate temperature based on the new atmosphere.
                ResetCachedTemperatures();
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
                Atmosphere.ResetGreenhouseFactor(this);
            }

            CalculatePhases(planetParams, 0, ref adjustedAtmosphericPressure);
            FractionHydrophere(AverageSurfaceTemperature);

            if (planetParams?.AtmosphericPressure.HasValue != true && habitabilityRequirements is null)
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

        private void GenerateAtmosphereThick(TerrestrialPlanetParams? planetParams, HabitabilityRequirements? habitabilityRequirements)
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
            var water = Substances.GetChemicalReference(Substances.Chemicals.Water);
            var seawater = Substances.GetSolutionReference(Substances.Solutions.Seawater);
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
                (Substances.GetChemicalReference(Substances.Chemicals.CarbonDioxide), co2),
                (Substances.GetChemicalReference(Substances.Chemicals.Helium), he),
                (Substances.GetChemicalReference(Substances.Chemicals.Hydrogen), h),
                (Substances.GetChemicalReference(Substances.Chemicals.Nitrogen), n2),
            };
            if (ar > 0)
            {
                components.Add((Substances.GetChemicalReference(Substances.Chemicals.Argon), ar));
            }
            if (co > 0)
            {
                components.Add((Substances.GetChemicalReference(Substances.Chemicals.CarbonMonoxide), co));
            }
            if (kr > 0)
            {
                components.Add((Substances.GetChemicalReference(Substances.Chemicals.Krypton), kr));
            }
            if (ch4 > 0)
            {
                components.Add((Substances.GetChemicalReference(Substances.Chemicals.Methane), ch4));
            }
            if (o2 > 0)
            {
                components.Add((Substances.GetChemicalReference(Substances.Chemicals.Oxygen), o2));
            }
            if (so2 > 0)
            {
                components.Add((Substances.GetChemicalReference(Substances.Chemicals.SulphurDioxide), so2));
            }
            if (waterVapor > 0)
            {
                components.Add((Substances.GetChemicalReference(Substances.Chemicals.Water), waterVapor));
            }
            if (xe > 0)
            {
                components.Add((Substances.GetChemicalReference(Substances.Chemicals.Xenon), xe));
            }
            _atmosphere = new Atmosphere(this, pressure, components.ToArray());
        }

        private void GenerateAtmosphereTrace()
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
            var water = Substances.GetChemicalReference(Substances.Chemicals.Water);
            var seawater = Substances.GetSolutionReference(Substances.Solutions.Seawater);
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
                _atmosphere = new Atmosphere(this, 0);
            }
            else
            {
                var components = new List<(ISubstanceReference, decimal)>()
                {
                    (Substances.GetChemicalReference(Substances.Chemicals.Helium), he),
                    (Substances.GetChemicalReference(Substances.Chemicals.Hydrogen), h),
                };
                if (ar > 0)
                {
                    components.Add((Substances.GetChemicalReference(Substances.Chemicals.Argon), ar));
                }
                if (co2 > 0)
                {
                    components.Add((Substances.GetChemicalReference(Substances.Chemicals.CarbonDioxide), co2));
                }
                if (co > 0)
                {
                    components.Add((Substances.GetChemicalReference(Substances.Chemicals.CarbonMonoxide), co));
                }
                if (kr > 0)
                {
                    components.Add((Substances.GetChemicalReference(Substances.Chemicals.Krypton), kr));
                }
                if (ch4 > 0)
                {
                    components.Add((Substances.GetChemicalReference(Substances.Chemicals.Methane), ch4));
                }
                if (n2 > 0)
                {
                    components.Add((Substances.GetChemicalReference(Substances.Chemicals.Nitrogen), n2));
                }
                if (o2 > 0)
                {
                    components.Add((Substances.GetChemicalReference(Substances.Chemicals.Oxygen), o2));
                }
                if (so2 > 0)
                {
                    components.Add((Substances.GetChemicalReference(Substances.Chemicals.SulphurDioxide), so2));
                }
                if (waterVapor > 0)
                {
                    components.Add((Substances.GetChemicalReference(Substances.Chemicals.Water), waterVapor));
                }
                if (xe > 0)
                {
                    components.Add((Substances.GetChemicalReference(Substances.Chemicals.Xenon), xe));
                }
                _atmosphere = new Atmosphere(this, Randomizer.Instance.NextDouble(25), components.ToArray());
            }
        }

        private void GenerateHydrocarbons()
        {
            // It is presumed that it is statistically likely that the current eon is not the first
            // with life, and therefore that some fossilized hydrocarbon deposits exist.
            var coal = (decimal)Randomizer.Instance.NormalDistributionSample(2e-13, 3.4e-14) * 0.5m;

            AddResource(Substances.GetMixtureReference(Substances.Mixtures.Anthracite), coal, false);
            AddResource(Substances.GetMixtureReference(Substances.Mixtures.BituminousCoal), coal, false);

            var petroleum = (decimal)Randomizer.Instance.NormalDistributionSample(1e-8, 1.6e-9);
            var petroleumSeed = AddResource(Substances.GetMixtureReference(Substances.Mixtures.Petroleum), petroleum, false);

            // Natural gas is predominantly, though not exclusively, found with petroleum deposits.
            AddResource(Substances.GetMixtureReference(Substances.Mixtures.NaturalGas), petroleum, false, true, petroleumSeed);
        }

        private protected virtual void GenerateHydrosphere() => GenerateHydrosphere(null, AverageBlackbodyTemperature);

        private protected void GenerateHydrosphere(double surfaceTemp, decimal ratio)
        {
            var mass = Number.Zero;
            var seawater = Substances.GetSolutionReference(Substances.Solutions.Seawater);

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
                _hydrosphere = NeverFoundry.MathAndScience.Chemistry.Material.Empty;
                return;
            }

            // Surface water is mostly salt water.
            var seawaterProportion = (decimal)Randomizer.Instance.NormalDistributionSample(0.945, 0.015);
            var waterProportion = 1 - seawaterProportion;
            var water = Substances.GetChemicalReference(Substances.Chemicals.Water);
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
            var oxygen = Substances.GetChemicalReference(Substances.Chemicals.Oxygen);
            Atmosphere.Material.AddConstituent(oxygen, o2);

            // Calculate ozone based on level of free oxygen.
            var o3 = o2 * 4.5e-5m;
            var ozone = Substances.GetChemicalReference(Substances.Chemicals.Ozone);
            if (!(Atmosphere.Material is LayeredComposite lc) || lc.Layers.Count < 3)
            {
                Atmosphere.DifferentiateTroposphere(); // First ensure troposphere is differentiated.
                (Atmosphere.Material as LayeredComposite)?.CopyLayer(1, 0.01m);
            }
            (Atmosphere.Material as LayeredComposite)?.Layers[2].material.AddConstituent(ozone, o3);

            // Convert most methane to CO2 and H2O.
            var methane = Substances.GetChemicalReference(Substances.Chemicals.Methane);
            var ch4 = Atmosphere.Material.GetProportion(methane);
            if (ch4 != 0)
            {
                // The levels of CO2 and H2O are not adjusted; it is presumed that the levels already
                // determined for them take the amounts derived from CH4 into account. If either gas
                // is entirely missing, however, it is added.
                var carbonDioxide = Substances.GetChemicalReference(Substances.Chemicals.CarbonDioxide);
                if (Atmosphere.Material.GetProportion(carbonDioxide) <= 0)
                {
                    Atmosphere.Material.AddConstituent(carbonDioxide, ch4 / 3);
                }

                _ = Substances.TryGetChemical(Substances.Chemicals.Water, out var water);
                if (Atmosphere.Material.GetProportion(water) <= 0)
                {
                    Atmosphere.Material.AddConstituent(water, ch4 * 2 / 3);
                    Atmosphere.ResetWater(this);
                }

                Atmosphere.Material.AddConstituent(methane, ch4 * 0.001m);

                Atmosphere.ResetGreenhouseFactor(this);
                return true;
            }

            return false;
        }

        private void GenerateOrbit(CelestialLocation orbitedObject, TerrestrialPlanetParams? planetParams, HabitabilityRequirements? habitabilityRequirements)
        {
            if (planetParams?.RotationalPeriod.HasValue == true)
            {
                RotationalPeriod = Number.Max(0, planetParams!.Value.RotationalPeriod!.Value);
            }
            GenerateAngleOfRotation(planetParams);

            if (orbitedObject == null)
            {
                return;
            }

            if (planetParams?.Eccentricity.HasValue == true)
            {
                Eccentricity = planetParams!.Value.Eccentricity!.Value;
            }

            var ta = Randomizer.Instance.NextDouble(NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.TwoPI);
            Number? semiMajorAxis = null;

            if (planetParams?.RevolutionPeriod.HasValue == true)
            {
                semiMajorAxis = WorldFoundry.Space.Orbit.GetSemiMajorAxisForPeriod(this, orbitedObject, planetParams!.Value.RevolutionPeriod!.Value);
                GenerateOrbit(orbitedObject, semiMajorAxis.Value, ta);
            }

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
                    || planetParams?.WaterRatio.HasValue == true)
                    && Substances.TryGetChemical(Substances.Chemicals.Water, out var water))
                {
                    var pressure = planetParams!.Value.AtmosphericPressure!.Value;

                    double vaporRatio;
                    if (planetParams?.WaterVaporRatio.HasValue == true)
                    {
                        vaporRatio = (double)planetParams!.Value.WaterVaporRatio!.Value;
                    }
                    else
                    {
                        vaporRatio = (water.GetVaporPressure(totalTargetEffectiveTemp) ?? 0) / pressure * 0.25;
                    }

                    greenhouseEffect = GetGreenhouseEffect(
                        GetInsolationFactor(Atmosphere.GetAtmosphericMass(this, pressure), 0), // scale height will be ignored since this isn't a polar calculation
                        Atmosphere.GetGreenhouseFactor(water.GreenhousePotential * vaporRatio, pressure));
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
                    AdjustOrbitForTemperature(planetParams, star, semiMajorAxis, ta, currentTargetTemp);

                    // Reset hydrosphere to negate effects of runaway evaporation or freezing.
                    _hydrosphere = originalHydrosphere;

                    GenerateAtmosphere(planetParams, habitabilityRequirements);

                    if (planetParams?.SurfaceTemperature.HasValue == true)
                    {
                        delta = targetEquatorialTemp - GetTemperatureAtElevation(AverageSurfaceTemperature, avgElevation);
                    }
                    else if (habitabilityRequirements.HasValue)
                    {
                        var tooCold = false;
                        if (habitabilityRequirements.Value.MinimumTemperature.HasValue)
                        {
                            var coolestEquatorialTemp = GetMinEquatorTemperature();
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
                            var warmestPolarTemp = GetMaxPolarTemperature();
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
                        ResetCachedTemperatures();
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

        private void GenerateOrbit(CelestialLocation orbitedObject, Number semiMajorAxis, double trueAnomaly)
        {
            WorldFoundry.Space.Orbit.SetOrbit(
                  this,
                  orbitedObject,
                  (1 - Eccentricity) * semiMajorAxis,
                  Eccentricity,
                  Randomizer.Instance.NextDouble(0.9),
                  Randomizer.Instance.NextDouble(NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.TwoPI),
                  Randomizer.Instance.NextDouble(NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.TwoPI),
                  trueAnomaly);
            ResetCachedTemperatures();
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
                AddResource(Substances.GetChemicalReference(Substances.Chemicals.Beryl), beryl, true);
            }
            if (emerald > 0)
            {
                AddResource(Substances.GetSolutionReference(Substances.Solutions.Emerald), emerald, true);
            }
            if (corundum > 0)
            {
                AddResource(Substances.GetChemicalReference(Substances.Chemicals.Corundum), corundum, true);
            }
            if (ruby > 0)
            {
                AddResource(Substances.GetSolutionReference(Substances.Solutions.Ruby), ruby, true);
            }
            if (sapphire > 0)
            {
                AddResource(Substances.GetSolutionReference(Substances.Solutions.Sapphire), sapphire, true);
            }
            if (diamond > 0)
            {
                AddResource(Substances.GetChemicalReference(Substances.Chemicals.Diamond), diamond, true);
            }
        }

        private protected override Planetoid? GenerateSatellite(Number periapsis, double eccentricity, Number maxMass)
        {
            Planetoid? satellite = null;
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
                    satellite = new LavaPlanet(CelestialParent, Vector3.Zero, maxMass);
                }
                else if (chance <= 0.77) // Most will be standard terrestrial.
                {
                    satellite = new TerrestrialPlanet(CelestialParent, Vector3.Zero, maxMass);
                }
                else
                {
                    satellite = new OceanPlanet(CelestialParent, Vector3.Zero, maxMass);
                }
            }

            // Otherwise, if the mass limit allows, there is an even chance that the satellite is a dwarf planet.
            else if (maxMass > DwarfPlanet.BaseMinMassForType && Randomizer.Instance.NextBool())
            {
                // Dwarf planets with very low orbits are lava planets due to tidal stress (plus a small percentage of others due to impact trauma).
                if (periapsis < GetRocheLimit(DwarfPlanet.BaseDensityForType) * new Number(105, -2) || chance <= 0.01)
                {
                    satellite = new LavaDwarfPlanet(CelestialParent, Vector3.Zero, maxMass);
                }
                else if (chance <= 0.75) // Most will be standard.
                {
                    satellite = new DwarfPlanet(CelestialParent, Vector3.Zero, maxMass);
                }
                else
                {
                    satellite = new RockyDwarfPlanet(CelestialParent, Vector3.Zero, maxMass);
                }
            }

            // Otherwise, it is an asteroid, selected from the standard distribution of types.
            else if (maxMass > 0)
            {
                if (chance <= 0.75)
                {
                    satellite = new CTypeAsteroid(CelestialParent, Vector3.Zero, maxMass);
                }
                else if (chance <= 0.9)
                {
                    satellite = new STypeAsteroid(CelestialParent, Vector3.Zero, maxMass);
                }
                else
                {
                    satellite = new MTypeAsteroid(CelestialParent, Vector3.Zero, maxMass);
                }
            }

            if (satellite != null)
            {
                WorldFoundry.Space.Orbit.SetOrbit(
                    satellite,
                    this,
                    periapsis,
                    eccentricity,
                    Randomizer.Instance.NextDouble(0.5),
                    Randomizer.Instance.NextDouble(NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.TwoPI),
                    Randomizer.Instance.NextDouble(NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.TwoPI),
                    Randomizer.Instance.NextDouble(NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.TwoPI));
            }

            return satellite;
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
        private Number GetDistanceForTemperature(Star star, double temperature)
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

            return Math.Sqrt(star.Luminosity * (1 - Albedo)) / (Math.Pow(temperature, 4) * NeverFoundry.MathAndScience.Constants.Doubles.MathConstants.FourPI * NeverFoundry.MathAndScience.Constants.Doubles.ScienceConstants.sigma * areaRatio);
        }

        private decimal GetHydrosphereAtmosphereRatio() => Math.Min(1, (decimal)(Hydrosphere.Mass / Atmosphere.Material.Mass));

        private protected override ISubstanceReference GetMantleSubstance()
            => Substances.GetMixtureReference(Substances.Mixtures.Peridotite);

        private Number GetMass(double? gravity, IShape? shape = null)
        {
            var minMass = MinMass;
            var maxMass = MaxMass.IsZero ? (Number?)null : MaxMass;

            if (Parent is StarSystem && Position != Vector3.Zero && (!Orbit.HasValue || Orbit.Value.OrbitedObject is Star))
            {
                // Stern-Levison parameter for neighborhood-clearing used to determined minimum mass at which
                // the planet would be able to do so at this orbital distance. We set the maximum at two
                // orders of magnitude more than this (planets in our solar system all have masses above
                // 5 orders of magnitude more). Note that since lambda is proportional to the square of mass,
                // it is multiplied by 10 to obtain a difference of 2 orders of magnitude, rather than by 100.
                minMass = Number.Max(minMass, GetSternLevisonLambdaMass() * 10);
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

        private protected override IMaterial GetMaterial() => GetMaterial(null, null);

        private protected IMaterial GetMaterial(TerrestrialPlanetParams? planetParams, HabitabilityRequirements? habitabilityRequirements)
        {
            var (density, mass, shape) = GetMatter(planetParams, habitabilityRequirements);
            return GetComposition(density, mass, shape, GetTemperature());
        }

        private (double density, Number mass, IShape shape) GetMatter(TerrestrialPlanetParams? planetParams, HabitabilityRequirements? habitabilityRequirements)
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
                return (density, GetMass(gravity, shape), shape);
            }
            else if (planetParams?.SurfaceGravity.HasValue == true
                || habitabilityRequirements?.MinimumGravity.HasValue == true
                || habitabilityRequirements?.MaximumGravity.HasValue == true)
            {
                var shape = GetShape(knownRadius: Number.Max(MinimumRadius, Number.Min(GetRadiusForSurfaceGravity(gravity!.Value), GetMaxRadius(density))));
                return (density, GetMass(gravity, shape), shape);
            }
            else
            {
                var mass = GetMass(gravity);
                return (density, mass, GetShape(density, mass));
            }
        }

        private Number GetRadiusForSurfaceGravity(double gravity) => (Mass * ScienceConstants.G / gravity).Sqrt();

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
        private bool IsHabitable()
        {
            var maxTemp = MaxSurfaceTemperature;
            var minTemp = MinSurfaceTemperature;
            var avgTemp = AverageSurfaceTemperature;
            var pressure = Atmosphere.AtmosphericPressure;
            // Liquid water is checked at the min, max, and avg surface temperatures of the world,
            // under the assumption that if liquid water exists anywhere on the world, it is likely
            // to be found at at least one of those values, even if one or more are too extreme
            // (e.g. polar icecaps below freezing, or an equator above boiling).
            return Hydrosphere.Contains(Substances.GetChemicalReference(Substances.Chemicals.Water), PhaseType.Liquid, maxTemp, pressure)
                || Hydrosphere.Contains(Substances.GetSolutionReference(Substances.Solutions.Seawater), PhaseType.Liquid, maxTemp, pressure)
                || Hydrosphere.Contains(Substances.GetChemicalReference(Substances.Chemicals.Water), PhaseType.Liquid, minTemp, pressure)
                || Hydrosphere.Contains(Substances.GetSolutionReference(Substances.Solutions.Seawater), PhaseType.Liquid, minTemp, pressure)
                || Hydrosphere.Contains(Substances.GetChemicalReference(Substances.Chemicals.Water), PhaseType.Liquid, avgTemp, pressure)
                || Hydrosphere.Contains(Substances.GetSolutionReference(Substances.Solutions.Seawater), PhaseType.Liquid, avgTemp, pressure);
        }
    }
}
