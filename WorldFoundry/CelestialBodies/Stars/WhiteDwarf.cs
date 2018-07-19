using MathAndScience.MathUtil.Shapes;
using Substances;
using System;
using System.Collections.Generic;
using System.Numerics;
using WorldFoundry.Space;

namespace WorldFoundry.CelestialBodies.Stars
{
    /// <summary>
    /// A white dwarf star.
    /// </summary>
    public class WhiteDwarf : Star
    {
        private const string baseTypeName = "White Dwarf";
        /// <summary>
        /// The base name for this type of <see cref="CelestialEntity"/>.
        /// </summary>
        public override string BaseTypeName => baseTypeName;

        private const double chanceOfLife = 0;
        /// <summary>The chance that this type of <see cref="CelestialEntity"/> and its children will actually have a
        /// biosphere, if it is habitable.
        /// </summary>
        /// <remarks>
        /// 0 for white dwarfs; their habitable zones, if any, are moving targets due to rapid
        /// cooling, and intersect soon with severe tidal forces, and additionally severe UV
        /// radiation is expected in early stages at the close distances where a habitable zone could
        /// be expected, making it unlikely that life could develop before a planet becomes uninhabitable.
        /// </remarks>
        public override double? ChanceOfLife => chanceOfLife;

        /// <summary>
        /// Initializes a new instance of <see cref="WhiteDwarf"/>.
        /// </summary>
        public WhiteDwarf() : base() { }

        /// <summary>
        /// Initializes a new instance of <see cref="WhiteDwarf"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="WhiteDwarf"/> is located.
        /// </param>
        public WhiteDwarf(CelestialRegion parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="WhiteDwarf"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="WhiteDwarf"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="WhiteDwarf"/>.</param>
        /// <param name="populationII">Set to true if this is to be a Population II <see cref="WhiteDwarf"/>.</param>
        public WhiteDwarf(CelestialRegion parent, Vector3 position, bool populationII = false) : base(parent, position, null, null, populationII) { }

        /// <summary>
        /// Randomly determines a <see cref="Star.Luminosity"/> for this <see cref="Star"/>.
        /// </summary>
        private protected override void GenerateLuminosity() => Luminosity = GetLuminosityFromRadius();

        /// <summary>
        /// Randomly determines a <see cref="LuminosityClass"/> for this <see cref="Star"/>.
        /// </summary>
        private protected override void GenerateLuminosityClass() => LuminosityClass = LuminosityClass.D;

        /// <summary>
        /// Generates the <see cref="CelestialEntity.Substance"/> of this <see cref="CelestialEntity"/>.
        /// </summary>
        private protected override void GenerateSubstance()
        {
            Substance = new Substance
            {
                Composition = new Composite(new Dictionary<(Chemical chemical, Phase phase), double>
                {
                    { (Chemical.Oxygen, Phase.Gas), 0.8 },
                    { (Chemical.Carbon, Phase.Gas), 0.3 },
                }),
                Mass = Randomizer.Static.Normal(1.194e30, 9.95e28),
                Temperature = Math.Round(Randomizer.Static.Normal(16850, 600)),
            };

            var radius = Math.Round(Math.Pow(1.8986e27 / Mass, 1.0 / 3.0) * 69911000);
            var flattening = Math.Max(Randomizer.Static.Normal(0.15, 0.05), 0);
            SetShape(new Ellipsoid(radius, Math.Round(radius * (1 - flattening))));
        }

        /// <summary>
        /// Randomly determines a <see cref="SpectralClass"/> for this <see cref="Star"/>.
        /// </summary>
        private protected override void GenerateSpectralClass() => SpectralClass = GetSpectralClassFromTemperature(Temperature ?? 0);

        /// <summary>
        /// Pseudo-randomly determines whether this <see cref="Star"/> will have giant planets, based
        /// on its characteristics.
        /// </summary>
        /// <returns>true if this <see cref="Star"/> will have giant planets; false otherwise.</returns>
        /// <remarks>
        /// 12% of white dwarfs have giant planets
        /// </remarks>
        private protected override bool GetWillHaveGiantPlanets() => Randomizer.Static.NextDouble() <= 0.12;

        /// <summary>
        /// Pseudo-randomly determines whether this <see cref="Star"/> will have ice giant planets,
        /// based on its characteristics.
        /// </summary>
        /// <returns>true if this <see cref="Star"/> will have ice giant planets; false otherwise.</returns>
        /// <remarks>
        /// 12% of white dwarfs have ice giant planets
        /// </remarks>
        private protected override bool GetWillHaveIceGiants() => Randomizer.Static.NextDouble() <= 0.12;

        /// <summary>
        /// Pseudo-randomly determines whether this <see cref="Star"/> will have terrestrial planets,
        /// based on its characteristics.
        /// </summary>
        /// <returns>true if this <see cref="Star"/> will have terrestrial planets; false otherwise.</returns>
        /// <remarks>
        /// 12% of white dwarfs have terrestrial planets
        /// </remarks>
        private protected override bool GetWillHaveTerrestrialPlanets() => Randomizer.Static.NextDouble() <= 0.12;
    }
}
