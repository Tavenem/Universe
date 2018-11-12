using MathAndScience.Shapes;
using Substances;
using System;
using MathAndScience.Numerics;
using WorldFoundry.Space;

namespace WorldFoundry.CelestialBodies.Stars
{
    /// <summary>
    /// A neutron star.
    /// </summary>
    public class NeutronStar : Star
    {
        // False for neutron stars, due to their excessive ionizing radiation, which makes the
        // development of life nearby highly unlikely.
        internal override bool IsHospitable => false;

        private protected override string BaseTypeName => "Neutron Star";

        private protected override string DesignatorPrefix => "X";

        /// <summary>
        /// Initializes a new instance of <see cref="NeutronStar"/>.
        /// </summary>
        internal NeutronStar() { }

        /// <summary>
        /// Initializes a new instance of <see cref="NeutronStar"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="NeutronStar"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="NeutronStar"/>.</param>
        /// <param name="populationII">Set to true if this is to be a Population II <see cref="NeutronStar"/>.</param>
        internal NeutronStar(CelestialRegion parent, Vector3 position, bool populationII = false) : base(parent, position, null, null, populationII) { }

        private protected override double GetLuminosity() => GetLuminosityFromRadius();

        private protected override LuminosityClass GetLuminosityClass() => LuminosityClass.Other;

        private protected override void GenerateSubstance()
        {
            Substance = new Substance
            {
                Composition = new Material(CosmicSubstances.CosmicSubstances.NeutronDegenerateMatter, Phase.Plasma),
                Mass = Randomizer.Instance.Normal(4.4178e30, 5.174e29), // between 1.44 and 3 times solar mass
                Temperature = Math.Round(Randomizer.Instance.Normal(600000, 133333)),
            };

            var radius = Math.Round(Randomizer.Instance.NextDouble(1000, 2000));
            var flattening = Math.Max(Randomizer.Instance.Normal(0.15, 0.05), 0);
            Shape = new Ellipsoid(radius, Math.Round(radius * (1 - flattening)), Position);
        }

        private protected override SpectralClass GetSpectralClass() => SpectralClass.Other;
    }
}
