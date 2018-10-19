using MathAndScience.Shapes;
using Substances;
using System;
using MathAndScience.Numerics;
using WorldFoundry.Space;
using WorldFoundry.Substances;

namespace WorldFoundry.CelestialBodies.Stars
{
    /// <summary>
    /// A neutron star.
    /// </summary>
    public class NeutronStar : Star
    {
        private const string _baseTypeName = "Neutron Star";
        /// <summary>
        /// The base name for this type of <see cref="CelestialEntity"/>.
        /// </summary>
        public override string BaseTypeName => _baseTypeName;

        private const string _designatorPrefix = "X";
        /// <summary>
        /// An optional string which is placed before a <see cref="CelestialEntity"/>'s <see cref="CelestialEntity.Designation"/>.
        /// </summary>
        protected override string DesignatorPrefix => _designatorPrefix;

        /// <summary>
        /// If <see langword="false"/> this type of <see cref="CelestialEntity"/> and its children
        /// cannot support life.
        /// </summary>
        /// <remarks>
        /// <see langword="false"/> for neutron stars, due to their excessive ionizing radiation,
        /// which makes the development of life nearby highly unlikely.
        /// </remarks>
        public override bool IsHospitable => false;

        /// <summary>
        /// Initializes a new instance of <see cref="NeutronStar"/>.
        /// </summary>
        public NeutronStar() { }

        /// <summary>
        /// Initializes a new instance of <see cref="NeutronStar"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="NeutronStar"/> is located.
        /// </param>
        public NeutronStar(CelestialRegion parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="NeutronStar"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="NeutronStar"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="NeutronStar"/>.</param>
        /// <param name="populationII">Set to true if this is to be a Population II <see cref="NeutronStar"/>.</param>
        public NeutronStar(CelestialRegion parent, Vector3 position, bool populationII = false) : base(parent, position, null, null, populationII) { }

        /// <summary>
        /// Randomly determines a <see cref="Star.Luminosity"/> for this <see cref="Star"/>.
        /// </summary>
        private protected override double GetLuminosity() => GetLuminosityFromRadius();

        /// <summary>
        /// Randomly determines a <see cref="LuminosityClass"/> for this <see cref="Star"/>.
        /// </summary>
        private protected override LuminosityClass GetLuminosityClass() => LuminosityClass.Other;

        /// <summary>
        /// Generates the <see cref="CelestialEntity.Substance"/> of this <see cref="CelestialEntity"/>.
        /// </summary>
        private protected override void GenerateSubstance()
        {
            Substance = new Substance
            {
                Composition = new Material(CosmicSubstances.NeutronDegenerateMatter, Phase.Plasma),
                Mass = Randomizer.Instance.Normal(4.4178e30, 5.174e29), // between 1.44 and 3 times solar mass
                Temperature = Math.Round(Randomizer.Instance.Normal(600000, 133333)),
            };

            var radius = Math.Round(Randomizer.Instance.NextDouble(1000, 2000));
            var flattening = Math.Max(Randomizer.Instance.Normal(0.15, 0.05), 0);
            Shape = new Ellipsoid(radius, Math.Round(radius * (1 - flattening)));
        }

        /// <summary>
        /// Randomly determines a <see cref="SpectralClass"/> for this <see cref="Star"/>.
        /// </summary>
        private protected override SpectralClass GetSpectralClass() => SpectralClass.Other;
    }
}
