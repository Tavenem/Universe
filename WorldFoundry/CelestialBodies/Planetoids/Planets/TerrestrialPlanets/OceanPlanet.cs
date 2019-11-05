using NeverFoundry.MathAndScience.Chemistry;
using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.MathAndScience.Randomization;
using NeverFoundry.WorldFoundry.Climate;
using NeverFoundry.WorldFoundry.Place;
using NeverFoundry.WorldFoundry.Space;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace NeverFoundry.WorldFoundry.CelestialBodies.Planetoids.Planets.TerrestrialPlanets
{
    /// <summary>
    /// A terrestrial planet consisting of an unusually high proportion of water, with a mantle
    /// consisting of a form of high-pressure, hot ice, and possibly a supercritical
    /// surface-atmosphere blend.
    /// </summary>
    [Serializable]
    public class OceanPlanet : TerrestrialPlanet
    {
        private protected override bool HasFlatSurface => true;

        private protected override Number MagnetosphereChanceFactor => Number.Half;

        private protected override string? PlanemoClassPrefix => "Ocean";

        /// <summary>
        /// Initializes a new instance of <see cref="OceanPlanet"/>.
        /// </summary>
        internal OceanPlanet() { }

        /// <summary>
        /// Initializes a new instance of <see cref="OceanPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parentId">The id of the location which contains this one.</param>
        /// <param name="position">The initial position of this <see cref="OceanPlanet"/>.</param>
        internal OceanPlanet(string? parentId, Vector3 position) : base(parentId, position) { }

        /// <summary>
        /// Initializes a new instance of <see cref="OceanPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parentId">The id of the location which contains this one.</param>
        /// <param name="position">The initial position of this <see cref="OceanPlanet"/>.</param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="OceanPlanet"/> during random generation, in kg.
        /// </param>
        internal OceanPlanet(string? parentId, Vector3 position, Number maxMass) : base(parentId, position, maxMass) { }

        private OceanPlanet(
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

        private OceanPlanet(SerializationInfo info, StreamingContext context) : this(
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

        private protected override void GenerateHydrosphere(TerrestrialPlanetParams? planetParams, double surfaceTemp)
            => GenerateHydrosphere(
                surfaceTemp,
                planetParams?.WaterRatio ?? (decimal)Randomizer.Instance.NormalDistributionSample(MaxElevation * 2, MaxElevation / 3));
    }
}
