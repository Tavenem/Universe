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
        private readonly int _seed3;

        /// <summary>
        /// The elevation of sea level relative to the mean surface elevation of the planet, in
        /// meters.
        /// </summary>
        public double SeaLevel { get; set; }

        public double MaxElevation { get; set; }

        private FastNoise _noise1;
        private FastNoise Noise1 => _noise1 ?? (_noise1 = new FastNoise(_seed1, 0.01, FastNoise.NoiseType.SimplexFractal, octaves: 6));

        private FastNoise _noise2;
        private FastNoise Noise2 => _noise2 ?? (_noise2 = new FastNoise(_seed2, 0.01, FastNoise.NoiseType.SimplexFractal, octaves: 5));

        private FastNoise _noise3;
        private FastNoise Noise3 => _noise3 ?? (_noise3 = new FastNoise(_seed3, 0.01, FastNoise.NoiseType.SimplexFractal, octaves: 4));

        private Terrain()
        {
            _seed1 = Randomizer.Instance.NextInclusiveMaxValue() * (Randomizer.Instance.NextBoolean() ? -1 : 1);
            _seed2 = Randomizer.Instance.NextInclusiveMaxValue() * (Randomizer.Instance.NextBoolean() ? -1 : 1);
            _seed3 = Randomizer.Instance.NextInclusiveMaxValue() * (Randomizer.Instance.NextBoolean() ? -1 : 1);
        }

        /// <summary>
        /// Initializes a new instance of <see cref="Terrain"/>.
        /// </summary>
        /// <param name="planet">The <see cref="Planetoid"/> which this <see cref="Terrain"/>
        /// maps.</param>
        public Terrain(Planetoid planet)
        {
            _seed1 = planet._seed1;
            _seed2 = planet._seed2;
            _seed3 = planet._seed3;
            Initialize(planet);
        }

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

            // The magnitude of the position vector is magnified to increase the surface area of the
            // noise map, thus providing a more diverse range of results without introducing
            // excessive noise (as increasing frequency would).
            var p = position * 100;

            // Initial noise map.
            var baseNoise = Noise1.GetNoise(p.X, p.Y, p.Z);

            // In order to avoid an appearance of excessive uniformity, with all mountains reaching
            // the same height, distributed uniformly over the surface, the initial noise is
            // multiplied by a second, independent noise map. The resulting map will have more
            // randomly distributed high and low points.
            var irregularity1 = Math.Abs(Noise2.GetNoise(p.X, p.Y, p.Z));

            // This process is repeated.
            var irregularity2 = Math.Abs(Noise3.GetNoise(p.X, p.Y, p.Z));

            var e = baseNoise * irregularity1 * irregularity2;

            // The overall value is magnified to compensate for excessive normalization.
            e *= 6;

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
                d += r.NextDouble() * 0.5;
            }
            d /= 5;
            MaxElevation = max * (0.5 + d);
        }
    }
}
