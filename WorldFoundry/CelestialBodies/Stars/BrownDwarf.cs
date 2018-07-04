using MathAndScience.MathUtil.Shapes;
using System;
using System.Numerics;
using WorldFoundry.Space;

namespace WorldFoundry.CelestialBodies.Stars
{
    /// <summary>
    /// A brown dwarf star.
    /// </summary>
    public class BrownDwarf : Star
    {
        private const string baseTypeName = "Brown Dwarf";
        /// <summary>
        /// The base name for this type of <see cref="CelestialEntity"/>.
        /// </summary>
        public override string BaseTypeName => baseTypeName;

        private const float chanceOfLife = 0;
        /// <summary>
        /// The chance that this type of <see cref="CelestialEntity"/> and its children will actually have a
        /// biosphere, if it is habitable.
        /// </summary>
        /// <remarks>
        /// 0 for brown dwarfs; their habitable zones, if any, are moving targets due to rapid
        /// cooling, and intersect soon with severe tidal forces, making it unlikely that life could
        /// develop before a planet becomes uninhabitable.
        /// </remarks>
        public override float? ChanceOfLife => chanceOfLife;

        /// <summary>
        /// Initializes a new instance of <see cref="BrownDwarf"/>.
        /// </summary>
        public BrownDwarf() : base() { }

        /// <summary>
        /// Initializes a new instance of <see cref="BrownDwarf"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="BrownDwarf"/> is located.
        /// </param>
        public BrownDwarf(CelestialRegion parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="BrownDwarf"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="BrownDwarf"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="BrownDwarf"/>.</param>
        /// <param name="populationII">Set to true if this is to be a Population II <see cref="BrownDwarf"/>.</param>
        public BrownDwarf(CelestialRegion parent, Vector3 position, bool populationII = false) : base(parent, position, null, null, populationII) { }

        /// <summary>
        /// Randomly determines a <see cref="Star.Luminosity"/> for this <see cref="Star"/>.
        /// </summary>
        private protected override void GenerateLuminosity() => Luminosity = GetLuminosityFromRadius();

        /// <summary>
        /// Randomly determines a <see cref="LuminosityClass"/> for this <see cref="Star"/>.
        /// </summary>
        private protected override void GenerateLuminosityClass() => LuminosityClass = LuminosityClass.V;

        /// <summary>
        /// Generates the mass of this <see cref="Star"/>.
        /// </summary>
        private protected override double GenerateMass(Shape shape) => Randomizer.Static.NextDouble(2.468e28, 1.7088e29);

        /// <summary>
        /// Generates the shape of this <see cref="Star"/>.
        /// </summary>
        private protected override Shape GenerateShape()
        {
            var radius = Math.Round(Randomizer.Static.Normal(69911000, 3495550));
            var flattening = Math.Max(Randomizer.Static.NextDouble(0.1), 0);
            return new Ellipsoid(radius, Math.Round(radius * (1 - flattening)));
        }

        /// <summary>
        /// Randomly determines a <see cref="SpectralClass"/> for this <see cref="Star"/>.
        /// </summary>
        private protected override void GenerateSpectralClass()
        {
            var chance = Randomizer.Static.NextDouble();
            if (chance <= 0.29)
            {
                SpectralClass = SpectralClass.M; // 29%
            }
            else if (chance <= 0.79)
            {
                SpectralClass = SpectralClass.L; // 50%
            }
            else if (chance <= 0.99)
            {
                SpectralClass = SpectralClass.T; // 20%
            }
            else
            {
                SpectralClass = SpectralClass.Y; // 1%
            }
        }
    }
}
