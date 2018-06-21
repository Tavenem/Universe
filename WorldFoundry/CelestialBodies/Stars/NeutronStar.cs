using MathAndScience.MathUtil.Shapes;
using Substances;
using System;
using System.Numerics;
using WorldFoundry.Space;
using WorldFoundry.Substances;

namespace WorldFoundry.CelestialBodies.Stars
{
    /// <summary>
    /// A neutron star.
    /// </summary>
    public class NeutronStar : Star
    {
        internal new static string baseTypeName = "Neutron Star";
        /// <summary>
        /// The base name for this type of <see cref="CelestialEntity"/>.
        /// </summary>
        public override string BaseTypeName => baseTypeName;

        private static readonly float chanceOfLife = 0;
        /// <summary>
        /// The chance that this type of <see cref="BioZone"/> and its children will actually have a
        /// biosphere, if it is habitable.
        /// </summary>
        /// <remarks>
        /// 0 for Neutron stars, due to their excessive ionizing radiation, which makes the
        /// development of life nearby highly unlikely.
        /// </remarks>
        public override float? ChanceOfLife => chanceOfLife;

        private static readonly string designatorPrefix = "X";
        /// <summary>
        /// An optional string which is placed before a <see cref="CelestialEntity"/>'s <see cref="Designation"/>.
        /// </summary>
        protected override string DesignatorPrefix => designatorPrefix;

        /// <summary>
        /// Initializes a new instance of <see cref="NeutronStar"/>.
        /// </summary>
        public NeutronStar() : base() { }

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
        /// Randomly determines a <see cref="Luminosity"/> for this <see cref="Star"/>.
        /// </summary>
        private protected override void GenerateLuminosity() => Luminosity = GetLuminosityFromRadius();

        /// <summary>
        /// Randomly determines a <see cref="LuminosityClass"/> for this <see cref="Star"/>.
        /// </summary>
        private protected override void GenerateLuminosityClass() => LuminosityClass = LuminosityClass.Other;

        /// <summary>
        /// Generates the <see cref="CelestialEntity.Substance"/> of this <see cref="CelestialEntity"/>.
        /// </summary>
        private protected override void GenerateSubstance()
        {
            Substance = new Substance
            {
                Composition = new Material(CosmicSubstances.NeutronDegenerateMatter, Phase.Plasma),
                Mass = Randomizer.Static.Normal(4.4178e30, 5.174e29), // between 1.44 and 3 times solar mass
                Temperature = (float)Math.Round(Randomizer.Static.Normal(600000, 133333)),
            };

            var radius = Math.Round(Randomizer.Static.NextDouble(1000, 2000));
            var flattening = Math.Max(Randomizer.Static.Normal(0.15, 0.05), 0);
            SetShape(new Ellipsoid(radius, Math.Round(radius * (1 - flattening))));
        }

        /// <summary>
        /// Randomly determines a <see cref="SpectralClass"/> for this <see cref="Star"/>.
        /// </summary>
        private protected override void GenerateSpectralClass() => SpectralClass = SpectralClass.Other;
    }
}
