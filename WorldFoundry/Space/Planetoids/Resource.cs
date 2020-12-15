using NeverFoundry.MathAndScience.Chemistry;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.MathAndScience.Randomization;
using NeverFoundry.WorldFoundry.Utilities;
using System;
using System.Runtime.Serialization;

namespace NeverFoundry.WorldFoundry.Space.Planetoids
{
    /// <summary>
    /// A resource which may be found on a <see cref="Planetoid"/>.
    /// </summary>
    [Serializable]
    public struct Resource : ISerializable, IEquatable<Resource>
    {
        private readonly bool _isVein, _isPerturbation;

        /// <summary>
        /// The substance found in the resource.
        /// </summary>
        public ISubstanceReference Substance { get; }

        /// <summary>
        /// The proportion, adjusted to [-0.5, 0.5].
        /// </summary>
        private readonly decimal _proportion;
        /// <summary>
        /// The proportion of the resource over the <see cref="Planetoid"/>'s surface, as a value
        /// between 0 and 1.
        /// </summary>
        public decimal Proportion => _proportion + 0.5m;

        internal int Seed { get; }

        private FastNoise? _noise;
        private FastNoise Noise
        {
            get
            {
                if (_noise is null)
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
        /// <param name="substance">The substance found in the resource.</param>
        /// <param name="proportion">The proportion of the resource present.</param>
        /// <param name="isVein">Whether the resource occurs in veins.</param>
        /// <param name="isPerturbation">Whether the resource is a perturbation of another.</param>
        /// <param name="seed">The random seed to use.</param>
        public Resource(ISubstanceReference substance, decimal proportion, bool isVein, bool isPerturbation = false, int? seed = null)
        {
            Substance = substance;
            _proportion = proportion - 0.5m;
            Seed = seed ?? Randomizer.Instance.NextInclusive();
            _isVein = isVein;
            _isPerturbation = isPerturbation;
            _noise = null;
        }

        private Resource(SerializationInfo info, StreamingContext context) : this(
            (ISubstanceReference?)info.GetValue(nameof(Substance), typeof(ISubstanceReference)) ?? HomogeneousReference.Empty,
            (decimal?)info.GetValue(nameof(Proportion), typeof(decimal)) ?? default,
            (bool?)info.GetValue(nameof(_isVein), typeof(bool)) ?? default,
            (bool?)info.GetValue(nameof(_isPerturbation), typeof(bool)) ?? default,
            (int?)info.GetValue(nameof(Seed), typeof(int)) ?? default)
        { }

        /// <summary>
        /// Indicates whether this instance and a specified object are equal.
        /// </summary>
        /// <param name="other">
        /// The <see cref="Resource"/> instance to compare with the current instance.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="other"/> and this instance represent the same
        /// value; otherwise, <see langword="false"/>.
        /// </returns>
        public bool Equals(Resource other)
            => Substance == other.Substance
            && _proportion == other._proportion
            && Seed == other.Seed
            && _isVein == other._isVein
            && _isPerturbation == other._isPerturbation;

        /// <summary>Indicates whether this instance and a specified object are equal.</summary>
        /// <param name="obj">The object to compare with the current instance.</param>
        /// <returns><see langword="true"/> if <paramref name="obj"/> and this instance are the same
        /// type and represent the same value; otherwise, <see langword="false"/>.</returns>
        public override bool Equals(object? obj) => obj is Resource other && Equals(other);

        /// <summary>Returns the hash code for this instance.</summary>
        /// <returns>A 32-bit signed integer that is the hash code for this instance.</returns>
        public override int GetHashCode()
        {
            var hashCode = -246786518;
            hashCode = (hashCode * -1521134295) + Substance.GetHashCode();
            hashCode = (hashCode * -1521134295) + _proportion.GetHashCode();
            hashCode = (hashCode * -1521134295) + Seed.GetHashCode();
            hashCode = (hashCode * -1521134295) + _isVein.GetHashCode();
            return (hashCode * -1521134295) + _isPerturbation.GetHashCode();
        }

        /// <summary>Populates a <see cref="SerializationInfo"></see> with the data needed to
        /// serialize the target object.</summary>
        /// <param name="info">The <see cref="SerializationInfo"></see> to populate with
        /// data.</param>
        /// <param name="context">The destination (see <see cref="StreamingContext"></see>) for this
        /// serialization.</param>
        /// <exception cref="System.Security.SecurityException">The caller does not have the
        /// required permission.</exception>
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(Substance), Substance);
            info.AddValue(nameof(Proportion), Proportion);
            info.AddValue(nameof(_isVein), _isVein);
            info.AddValue(nameof(_isPerturbation), _isPerturbation);
            info.AddValue(nameof(Seed), Seed);
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
            var x = (double)position.X;
            var y = (double)position.Y;
            var z = (double)position.Z;
            if (_isPerturbation)
            {
                Noise.GradientPerturbFractal(ref x, ref y, ref z);
            }
            var v = Noise.GetNoise(x, y, z);
            if (_isVein)
            {
                v = 1 - v;
            }
            return Math.Max(0, (v + (double)_proportion) / (double)(1 + _proportion));
        }

        /// <summary>
        /// Indicates whether two instances are equal.
        /// </summary>
        /// <param name="left">
        /// The first <see cref="Resource"/> instance to compare.
        /// </param>
        /// <param name="right">
        /// The second <see cref="Resource"/> instance to compare.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the instances represent the same value; otherwise, <see
        /// langword="false"/>.
        /// </returns>
        public static bool operator ==(Resource left, Resource right) => left.Equals(right);

        /// <summary>
        /// Indicates whether two instances are unequal.
        /// </summary>
        /// <param name="left">
        /// The first <see cref="Resource"/> instance to compare.
        /// </param>
        /// <param name="right">
        /// The second <see cref="Resource"/> instance to compare.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the instances represent different values; otherwise, <see
        /// langword="false"/>.
        /// </returns>
        public static bool operator !=(Resource left, Resource right) => !(left == right);
    }
}
