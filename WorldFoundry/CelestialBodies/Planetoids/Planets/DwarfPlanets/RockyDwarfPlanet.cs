﻿using NeverFoundry.MathAndScience.Chemistry;
using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.MathAndScience.Randomization;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using WorldFoundry.Climate;
using WorldFoundry.Place;
using WorldFoundry.Space;

namespace WorldFoundry.CelestialBodies.Planetoids.Planets.DwarfPlanets
{
    /// <summary>
    /// A rocky dwarf planet without the typical subsurface ice/water mantle.
    /// </summary>
    [Serializable]
    public class RockyDwarfPlanet : DwarfPlanet
    {
        private protected override double DensityForType => 4000;

        private protected override string? PlanemoClassPrefix => "Rocky";

        /// <summary>
        /// Initializes a new instance of <see cref="RockyDwarfPlanet"/>.
        /// </summary>
        internal RockyDwarfPlanet() { }

        /// <summary>
        /// Initializes a new instance of <see cref="RockyDwarfPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="Location"/> in which this <see cref="RockyDwarfPlanet"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="RockyDwarfPlanet"/>.</param>
        internal RockyDwarfPlanet(Location? parent, Vector3 position) : base(parent, position) { }

        /// <summary>
        /// Initializes a new instance of <see cref="RockyDwarfPlanet"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="Location"/> in which this <see cref="RockyDwarfPlanet"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="RockyDwarfPlanet"/>.</param>
        /// <param name="maxMass">
        /// The maximum mass allowed for this <see cref="RockyDwarfPlanet"/> during random generation, in kg.
        /// </param>
        internal RockyDwarfPlanet(Location? parent, Vector3 position, Number maxMass) : base(parent, position, maxMass) { }

        private protected RockyDwarfPlanet(
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
            List<Location>? children)
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
                children) { }

        private RockyDwarfPlanet(SerializationInfo info, StreamingContext context) : this(
            (string)info.GetValue(nameof(Id), typeof(string)),
            (string?)info.GetValue(nameof(Name), typeof(string)),
            (bool)info.GetValue(nameof(_isPrepopulated), typeof(bool)),
            (double?)info.GetValue(nameof(Albedo), typeof(double?)),
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
            (List<PlanetaryRing>?)info.GetValue(nameof(Rings), typeof(List<PlanetaryRing>)),
            (List<Location>)info.GetValue(nameof(Children), typeof(List<Location>))) { }

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

            // rocky crust
            // 50% chance of dust
            var dust = Math.Max(0, Randomizer.Instance.NextDecimal(-0.5m, 0.5m));
            var rock = 1 - dust;

            var components = new List<(ISubstanceReference, decimal)>();
            foreach (var (material, proportion) in CelestialSubstances.DryPlanetaryCrustConstituents)
            {
                components.Add((material, proportion * rock));
            }
            if (dust > 0)
            {
                components.Add((Substances.GetSolutionReference(Substances.Solutions.CosmicDust), dust));
            }
            yield return (new Material(
                components,
                (double)(crustMass / shape.Volume),
                crustMass,
                shape), 1);
        }

        private protected override IEnumerable<(IMaterial, decimal)> GetMantle(
            IShape planetShape,
            Number mantleProportion,
            Number crustProportion,
            Number planetMass,
            IShape coreShape,
            double coreTemp)
        {
            yield break;
        }
    }
}
