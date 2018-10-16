using Substances;
using System;
using MathAndScience.Numerics;

namespace WorldFoundry.CelestialBodies.Planetoids
{
    /// <summary>
    /// A resource which may be found on a <see cref="Planetoid"/>.
    /// </summary>
    public class Resource
    {
        private readonly bool _isVein, _isPerturbation;

        /// <summary>
        /// The chemical found in the resource.
        /// </summary>
        public Chemical Chemical { get; }

        /// <summary>
        /// The proportion of the resource over the <see cref="Planetoid"/>'s surface.
        /// </summary>
        public double Proportion { get; set; }

        internal int Seed { get; }

        private FastNoise _noise;
        private FastNoise Noise
        {
            get
            {
                if (_noise == null)
                {
                    _noise = new FastNoise(Seed, 10, FastNoise.NoiseType.SimplexFractal, gain: 0.75f);
                    _noise.SetFractalLacunarity(3);
                    if (_isVein)
                    {
                        _noise.SetFractalType(FastNoise.FractalType.RigidMulti);
                    }
                    else
                    {
                        _noise.SetFractalType(FastNoise.FractalType.Billow);
                    }
                    if (_isPerturbation)
                    {
                        _noise.SetGradientPerturbAmp(20);
                    }
                }
                return _noise;
            }
        }

        /// <summary>
        /// Initialize a new instance of <see cref="Resource"/>.
        /// </summary>
        /// <param name="chemical">The chemical found in the resource.</param>
        /// <param name="proportion">The proportion of the resource present.</param>
        /// <param name="isVein">Whether the resource occurs in veins.</param>
        /// <param name="isPerturbation">Whether the resource is a perturbation of another.</param>
        /// <param name="seed">The random seed to use.</param>
        public Resource(Chemical chemical, double proportion, bool isVein, bool isPerturbation = false, int? seed = null)
        {
            Chemical = chemical;
            Proportion = proportion;
            Seed = seed ?? Randomizer.Instance.NextInclusiveMaxValue();
            _isVein = isVein;
            _isPerturbation = isPerturbation;
        }

        /// <summary>
        /// Gets the richness of this <see cref="Resource"/> at the given <paramref
        /// name="position"/>.
        /// </summary>
        /// <param name="position">A normalized vector representing a direction from the center of
        /// the <see cref="Planetoid"/>.</param>
        /// <returns>The richness of this <see cref="Resource"/> at the given <paramref
        /// name="position"/>, as a value between 0 and 1.</returns>
        public double GetResourceRichnessAt(Vector3 position)
        {
            var p = position;
            var x = p.X;
            var y = p.Y;
            var z = p.Z;
            if (_isPerturbation)
            {
                Noise.GradientPerturbFractal(ref x, ref y, ref z);
            }
            var v = Noise.GetNoise(x, y, z);
            if (_isVein)
            {
                v = 1 - v;
            }
            var modifier = Proportion - 0.5;
            return Math.Max(0, (v + modifier) / (1 + modifier));
        }
    }
}
