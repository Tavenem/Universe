﻿using System;
using System.Numerics;
using WorldFoundry.Space;
using WorldFoundry.Utilities;
using WorldFoundry.Utilities.MathUtil.Shapes;

namespace WorldFoundry.CelestialBodies.Stars
{
    /// <summary>
    /// A white dwarf star.
    /// </summary>
    public class WhiteDwarf : Star
    {
        internal new static string baseTypeName = "White Dwarf";
        /// <summary>
        /// The base name for this type of <see cref="CelestialEntity"/>.
        /// </summary>
        public override string BaseTypeName => baseTypeName;

        private static float chanceOfLife = 0;
        /// <summary>
        /// The chance that this type of <see cref="BioZone"/> and its children will actually have a
        /// biosphere, if it is habitable.
        /// </summary>
        /// <remarks>
        /// 0 for white dwarfs; their habitable zones, if any, are moving targets due to rapid
        /// cooling, and intersect soon with severe tidal forces, and additionally severe UV
        /// radiation is expected in early stages at the close distances where a habitable zone could
        /// be expected, making it unlikely that life could develop before a planet becomes uninhabitable.
        /// </remarks>
        public override float? ChanceOfLife => chanceOfLife;

        /// <summary>
        /// Initializes a new instance of <see cref="WhiteDwarf"/>.
        /// </summary>
        public WhiteDwarf() { }

        /// <summary>
        /// Initializes a new instance of <see cref="WhiteDwarf"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="WhiteDwarf"/> is located.
        /// </param>
        public WhiteDwarf(CelestialObject parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="WhiteDwarf"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="WhiteDwarf"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="WhiteDwarf"/>.</param>
        /// <param name="populationII">Set to true if this is to be a Population II <see cref="WhiteDwarf"/>.</param>
        public WhiteDwarf(CelestialObject parent, Vector3 position, bool populationII = false) : base(parent, position, null, null, populationII) { }

        /// <summary>
        /// Randomly determines a <see cref="Luminosity"/> for this <see cref="Star"/>.
        /// </summary>
        protected override void GenerateLuminosity() => Luminosity = GetLuminosityFromRadius();

        /// <summary>
        /// Randomly determines a <see cref="LuminosityClass"/> for this <see cref="Star"/>.
        /// </summary>
        protected override void GenerateLuminosityClass() => LuminosityClass = LuminosityClass.D;

        /// <summary>
        /// Generates the <see cref="Mass"/> of this <see cref="Orbiter"/>.
        /// </summary>
        protected override void GenerateMass() => Mass = Randomizer.Static.Normal(1.194e30, 9.95e28);

        /// <summary>
        /// Generates the <see cref="Shape"/> of this <see cref="CelestialEntity"/>.
        /// </summary>
        /// <remarks>
        /// Proportional to the mass/radius ratio of Jupiter.
        /// </remarks>
        protected override void GenerateShape()
        {
            var radius = Math.Round(Math.Pow(1.8986e27 / Mass, 1.0 / 3.0) * 69911000);
            var flattening = Math.Max(Randomizer.Static.Normal(0.15, 0.05), 0);
            Shape = new Ellipsoid(radius, Math.Round(radius * (1 - flattening)));
        }

        /// <summary>
        /// Randomly determines a <see cref="SpectralClass"/> for this <see cref="Star"/>.
        /// </summary>
        protected override void GenerateSpectralClass() => SpectralClass = GetSpectralClassFromTemperature(Temperature ?? 0);

        /// <summary>
        /// Determines a temperature for this <see cref="ThermalBody"/>, in K.
        /// </summary>
        protected override void GenerateTemperature() => Temperature = (float)Math.Round(Randomizer.Static.Normal(16850, 600));

        /// <summary>
        /// Pseudo-randomly determines whether this <see cref="Star"/> will have giant planets, based
        /// on its characteristics.
        /// </summary>
        /// <returns>true if this <see cref="Star"/> will have giant planets; false otherwise.</returns>
        /// <remarks>
        /// 12% of white dwarfs have giant planets
        /// </remarks>
        protected override bool GetWillHaveGiantPlanets() => Randomizer.Static.NextDouble() <= 0.12;

        /// <summary>
        /// Pseudo-randomly determines whether this <see cref="Star"/> will have ice giant planets,
        /// based on its characteristics.
        /// </summary>
        /// <returns>true if this <see cref="Star"/> will have ice giant planets; false otherwise.</returns>
        /// <remarks>
        /// 12% of white dwarfs have ice giant planets
        /// </remarks>
        protected override bool GetWillHaveIceGiants() => Randomizer.Static.NextDouble() <= 0.12;

        /// <summary>
        /// Pseudo-randomly determines whether this <see cref="Star"/> will have terrestrial planets,
        /// based on its characteristics.
        /// </summary>
        /// <returns>true if this <see cref="Star"/> will have terrestrial planets; false otherwise.</returns>
        /// <remarks>
        /// 12% of white dwarfs have terrestrial planets
        /// </remarks>
        protected override bool GetWillHaveTerrestrialPlanets() => Randomizer.Static.NextDouble() <= 0.12;
    }
}
