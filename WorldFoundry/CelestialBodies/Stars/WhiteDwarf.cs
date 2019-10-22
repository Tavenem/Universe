﻿using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using WorldFoundry.Place;
using WorldFoundry.Space;
using NeverFoundry.MathAndScience.Chemistry;
using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.MathAndScience.Randomization;

namespace WorldFoundry.CelestialBodies.Stars
{
    /// <summary>
    /// A white dwarf star.
    /// </summary>
    [Serializable]
    public class WhiteDwarf : Star
    {
        // False for white dwarfs; their habitable zones, if any, are moving targets due to rapid
        // cooling, and intersect soon with severe tidal forces, and additionally severe UV
        // radiation is expected in early stages at the close distances where a habitable zone could
        // be expected, making it unlikely that life could develop before a planet becomes
        // uninhabitable.
        internal override bool IsHospitable => false;

        private protected override string BaseTypeName => "White Dwarf";

        /// <summary>
        /// Initializes a new instance of <see cref="WhiteDwarf"/>.
        /// </summary>
        internal WhiteDwarf() { }

        /// <summary>
        /// Initializes a new instance of <see cref="WhiteDwarf"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="Location"/> in which this <see cref="WhiteDwarf"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="WhiteDwarf"/>.</param>
        /// <param name="populationII">Set to true if this is to be a Population II <see cref="WhiteDwarf"/>.</param>
        internal WhiteDwarf(Location parent, Vector3 position, bool populationII = false) : base(parent, position, null, null, populationII) { }

        private protected WhiteDwarf(
            string id,
            string? name,
            bool isPrepopulated,
            double? luminosity,
            LuminosityClass? luminosityClass,
            bool isPopulationII,
            SpectralClass? spectralClass,
            double? albedo,
            Vector3 velocity,
            Orbit? orbit,
            IMaterial? material,
            List<Location>? children)
            : base(
                id,
                name,
                isPrepopulated,
                luminosity,
                luminosityClass,
                isPopulationII,
                spectralClass,
                albedo,
                velocity,
                orbit,
                material,
                children) { }

        private WhiteDwarf(SerializationInfo info, StreamingContext context) : this(
            (string)info.GetValue(nameof(Id), typeof(string)),
            (string?)info.GetValue(nameof(Name), typeof(string)),
            (bool)info.GetValue(nameof(_isPrepopulated), typeof(bool)),
            (double?)info.GetValue(nameof(Luminosity), typeof(double?)),
            (LuminosityClass?)info.GetValue(nameof(LuminosityClass), typeof(LuminosityClass?)),
            (bool)info.GetValue(nameof(IsPopulationII), typeof(bool)),
            (SpectralClass?)info.GetValue(nameof(SpectralClass), typeof(SpectralClass?)),
            (double?)info.GetValue(nameof(Albedo), typeof(double?)),
            (Vector3)info.GetValue(nameof(Velocity), typeof(Vector3)),
            (Orbit?)info.GetValue(nameof(Orbit), typeof(Orbit?)),
            (IMaterial?)info.GetValue(nameof(Material), typeof(IMaterial)),
            (List<Location>)info.GetValue(nameof(Children), typeof(List<Location>))) { }

        private protected override double GetLuminosity(Number? temperature = null) => GetLuminosityFromRadius();

        private protected override LuminosityClass GetLuminosityClass() => LuminosityClass.D;

        private protected override Number GetMass()
            => Randomizer.Instance.NormalDistributionSample(1.194e30, 9.95e28);

        private protected override IMaterial GetMaterial()
        {
            var mass = GetMass();

            var radius = (new Number(1.8986, 27) / mass).CubeRoot() * 69911000;
            var flattening = (Number)Randomizer.Instance.NormalDistributionSample(0.15, 0.05, minimum: 0);
            var shape = new Ellipsoid(radius, radius * (1 - flattening), Position);

            return GetComposition((double)(mass / shape.Volume), mass, shape, GetTemperature());
        }

        private protected override SpectralClass GetSpectralClass() => GetSpectralClassFromTemperature(Temperature ?? 0);

        private protected override ISubstanceReference? GetSubstance() => CelestialSubstances.StellarMaterialWhiteDwarf;

        private protected override double? GetTemperature()
            => Randomizer.Instance.NormalDistributionSample(16850, 600);

        // 12% of white dwarfs have giant planets
        private protected override bool GetWillHaveGiantPlanets() => Randomizer.Instance.NextDouble() <= 0.12;

        // 12% of white dwarfs have ice giant planets
        private protected override bool GetWillHaveIceGiants() => Randomizer.Instance.NextDouble() <= 0.12;

        // 12% of white dwarfs have terrestrial planets
        private protected override bool GetWillHaveTerrestrialPlanets() => Randomizer.Instance.NextDouble() <= 0.12;
    }
}
