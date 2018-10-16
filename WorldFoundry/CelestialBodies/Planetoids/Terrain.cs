using MathAndScience;
using System;
using MathAndScience.Numerics;

namespace WorldFoundry.CelestialBodies.Planetoids
{
    /// <summary>
    /// Defines the terrain of a <see cref="Planetoid"/>.
    /// </summary>
    public class Terrain
    {
        private readonly int _seed1;
        private readonly int _seed2;

        /// <summary>
        /// The elevation of sea level relative to the mean surface elevation of the planet, in
        /// meters.
        /// </summary>
        public double SeaLevel { get; set; }

        internal double MaxElevation { get; set; }

        private FastNoise _noise1;
        private FastNoise Noise1 => _noise1 ?? (_noise1 = new FastNoise(_seed1, 10, FastNoise.NoiseType.CubicFractal, FastNoise.FractalType.RigidMulti, octaves: 6, gradientPerturbAmp: 20));

        private FastNoise _noise2;
        private FastNoise Noise2 => _noise2 ?? (_noise2 = new FastNoise(_seed2, 10, FastNoise.NoiseType.CubicFractal, octaves: 2));

        private Terrain()
        {
            _seed1 = Randomizer.Instance.NextInclusiveMaxValue();
            _seed2 = Randomizer.Instance.NextInclusiveMaxValue();
        }

        /// <summary>
        /// Initializes a new instance of <see cref="Terrain"/>.
        /// </summary>
        /// <param name="planet">The <see cref="Planetoid"/> which this <see cref="Terrain"/>
        /// maps.</param>
        public Terrain(Planetoid planet) : this() => Initialize(planet);

        /// <summary>
        /// Gets the elevation at the given <paramref name="position"/>, in meters. Negative values
        /// are below sea level.
        /// </summary>
        /// <param name="position">A normalized position vector representing a direction from the
        /// center of the <see cref="Planetoid"/>.</param>
        /// <returns>The elevation at the given <paramref name="position"/>, in meters. Negative
        /// values are below sea level.</returns>
        public double GetElevationAt(Vector3 position)
        {
            if (MaxElevation.IsZero())
            {
                return 0;
            }

            // Initial noise map, magnified to a range of approximately -1-1.
            var baseNoise = Noise1.GetPerturbedNoise(position.X, position.Y, position.Z) * 3;

            // In order to avoid an appearance of excessive uniformity, with all mountains reaching
            // the same height, distributed uniformly over the surface, the initial noise is
            // multiplied by a second, independent noise map whose values are shifted up and
            // magnified to a range of approximately 0-1.2. This has the effect of moving most
            // values on the initial noise map towards zero, while pushing a few away from zero. The
            // resulting map will have irregular features that are mostly flat, with select few high
            // points.
            var irregularity = Math.Abs((Noise2.GetNoise(position.X, position.Y, position.Z) + 0.25) * 2.4);

            var e = baseNoise * irregularity;

            return (e * MaxElevation) - SeaLevel;
        }

        internal double GetFrictionCoefficientAt(double elevation)
            => elevation <= 0 ? 0.000025f : ((elevation * 6.667e-9) + 0.000025); // 0.000045 at 3000

        internal double GetFrictionCoefficientAt(Vector3 position)
            => GetFrictionCoefficientAt(GetElevationAt(position));

        private void Initialize(Planetoid planet)
        {
            if (planet.HasFlatSurface)
            {
                MaxElevation = 0;
                return;
            }

            var max = 2e5 / planet.SurfaceGravity;
            var r = new Random(_seed1);
            var d = 0.0;
            for (var i = 0; i < 5; i++)
            {
                d += Math.Pow(r.NextDouble(), 3);
            }
            d /= 5;
            MaxElevation = (max * (d + 3) / 8) + (max / 2);
        }
    }
}
