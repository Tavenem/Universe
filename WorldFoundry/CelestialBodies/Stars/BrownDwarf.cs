using MathAndScience.Shapes;
using System;
using MathAndScience.Numerics;
using WorldFoundry.Space;

namespace WorldFoundry.CelestialBodies.Stars
{
    /// <summary>
    /// A brown dwarf star.
    /// </summary>
    public class BrownDwarf : Star
    {
        private protected override string BaseTypeName => "Brown Dwarf";

        // False for brown dwarfs; their habitable zones, if any, are moving targets due to rapid
        // cooling, and intersect soon with severe tidal forces, making it unlikely that life could
        // develop before a planet becomes uninhabitable.
        private protected override bool IsHospitable => false;

        /// <summary>
        /// Initializes a new instance of <see cref="BrownDwarf"/>.
        /// </summary>
        internal BrownDwarf() { }

        /// <summary>
        /// Initializes a new instance of <see cref="BrownDwarf"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="BrownDwarf"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="BrownDwarf"/>.</param>
        /// <param name="populationII">Set to true if this is to be a Population II <see cref="BrownDwarf"/>.</param>
        internal BrownDwarf(CelestialRegion parent, Vector3 position, bool populationII = false) : base(parent, position, null, null, populationII) { }

        private protected override double GetLuminosity() => GetLuminosityFromRadius();

        private protected override double GenerateMass(IShape shape) => Randomizer.Instance.NextDouble(2.468e28, 1.7088e29);

        private protected override IShape GenerateShape()
        {
            var radius = Math.Round(Randomizer.Instance.Normal(69911000, 3495550));
            var flattening = Math.Max(Randomizer.Instance.NextDouble(0.1), 0);
            return new Ellipsoid(radius, Math.Round(radius * (1 - flattening)));
        }

        private protected override SpectralClass GetSpectralClass()
        {
            var chance = Randomizer.Instance.NextDouble();
            if (chance <= 0.29)
            {
                return SpectralClass.M; // 29%
            }
            else if (chance <= 0.79)
            {
                return SpectralClass.L; // 50%
            }
            else if (chance <= 0.99)
            {
                return SpectralClass.T; // 20%
            }
            else
            {
                return SpectralClass.Y; // 1%
            }
        }
    }
}
