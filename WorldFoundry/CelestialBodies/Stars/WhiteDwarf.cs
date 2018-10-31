using MathAndScience.Shapes;
using Substances;
using System;
using MathAndScience.Numerics;
using WorldFoundry.Space;

namespace WorldFoundry.CelestialBodies.Stars
{
    /// <summary>
    /// A white dwarf star.
    /// </summary>
    public class WhiteDwarf : Star
    {
        private protected override string BaseTypeName => "White Dwarf";

        // False for white dwarfs; their habitable zones, if any, are moving targets due to rapid
        // cooling, and intersect soon with severe tidal forces, and additionally severe UV
        // radiation is expected in early stages at the close distances where a habitable zone could
        // be expected, making it unlikely that life could develop before a planet becomes
        // uninhabitable.
        private protected override bool IsHospitable => false;

        /// <summary>
        /// Initializes a new instance of <see cref="WhiteDwarf"/>.
        /// </summary>
        internal WhiteDwarf() { }

        /// <summary>
        /// Initializes a new instance of <see cref="WhiteDwarf"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="WhiteDwarf"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="WhiteDwarf"/>.</param>
        /// <param name="populationII">Set to true if this is to be a Population II <see cref="WhiteDwarf"/>.</param>
        internal WhiteDwarf(CelestialRegion parent, Vector3 position, bool populationII = false) : base(parent, position, null, null, populationII) { }

        private protected override double GetLuminosity() => GetLuminosityFromRadius();

        private protected override LuminosityClass GetLuminosityClass() => LuminosityClass.D;

        private protected override void GenerateSubstance()
        {
            Substance = new Substance
            {
                Composition = new Composite(
                    (Chemical.Oxygen, Phase.Gas, 0.8),
                    (Chemical.Carbon, Phase.Gas, 0.3)),
                Mass = Randomizer.Instance.Normal(1.194e30, 9.95e28),
                Temperature = Math.Round(Randomizer.Instance.Normal(16850, 600)),
            };

            var radius = Math.Round(Math.Pow(1.8986e27 / Mass, 1.0 / 3.0) * 69911000);
            var flattening = Math.Max(Randomizer.Instance.Normal(0.15, 0.05), 0);
            Shape = new Ellipsoid(radius, Math.Round(radius * (1 - flattening)));
        }

        private protected override SpectralClass GetSpectralClass() => GetSpectralClassFromTemperature(Temperature ?? 0);

        // 12% of white dwarfs have giant planets
        private protected override bool GetWillHaveGiantPlanets() => Randomizer.Instance.NextDouble() <= 0.12;

        // 12% of white dwarfs have ice giant planets
        private protected override bool GetWillHaveIceGiants() => Randomizer.Instance.NextDouble() <= 0.12;

        // 12% of white dwarfs have terrestrial planets
        private protected override bool GetWillHaveTerrestrialPlanets() => Randomizer.Instance.NextDouble() <= 0.12;
    }
}
