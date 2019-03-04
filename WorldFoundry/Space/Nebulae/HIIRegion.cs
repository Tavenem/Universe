using MathAndScience.Numerics;
using MathAndScience.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using WorldFoundry.CelestialBodies.Stars;

namespace WorldFoundry.Space
{
    /// <summary>
    /// A charged cloud of interstellar gas and dust.
    /// </summary>
    public class HIIRegion : Nebula
    {
        private const double ChildDensity = 6.0e-50;

        private static readonly List<ChildDefinition> BaseChildDefinitions = new List<ChildDefinition>
        {
            new ChildDefinition(typeof(StarSystem), StarSystem.Space, ChildDensity * 0.9998, typeof(Star), SpectralClass.B, LuminosityClass.V),
            new ChildDefinition(typeof(StarSystem), StarSystem.Space, ChildDensity * 0.0002, typeof(Star), SpectralClass.O, LuminosityClass.V),
        };

        private protected override string BaseTypeName => "HII Region";

        private protected override IEnumerable<ChildDefinition> ChildDefinitions
            => base.ChildDefinitions.Concat(BaseChildDefinitions);

        /// <summary>
        /// Initializes a new instance of <see cref="HIIRegion"/>.
        /// </summary>
        internal HIIRegion() { }

        /// <summary>
        /// Initializes a new instance of <see cref="HIIRegion"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialRegion"/> in which this <see cref="HIIRegion"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="HIIRegion"/>.</param>
        internal HIIRegion(CelestialRegion parent, Vector3 position) : base(parent, position) { }

        private protected override double GetMass() => Randomizer.Instance.NextDouble(1.99e33, 1.99e37); // ~10e3–10e7 solar masses

        // Actual nebulae are irregularly shaped; this is presumed to be a containing shape within
        // which the dust clouds and filaments roughly fit. The radius follows a log-normal
        // distribution, with  ~20 ly as the mode, starting at ~10 ly, and cutting off around ~600
        // ly.
        private protected override IShape GetShape()
        {
            double axis;
            do
            {
                axis = Math.Round(1.0e17 + (Randomizer.Instance.Lognormal(0, 1) * 1.0e17));
            } while (axis > Space);
            return new Ellipsoid(
                axis,
                Math.Round(axis * Randomizer.Instance.NextDouble(0.5, 1.5)),
                Math.Round(axis * Randomizer.Instance.NextDouble(0.5, 1.5)),
                Position);
        }

        private protected override double GetTemperature() => 10000;
    }
}
