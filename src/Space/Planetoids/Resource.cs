using Tavenem.Chemistry;
using Tavenem.Randomize;
using Tavenem.Universe.Utilities;

namespace Tavenem.Universe.Space.Planetoids;

/// <summary>
/// A resource which may be found on a <see cref="Planetoid"/>.
/// </summary>
public struct Resource : IEquatable<Resource>
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
    public override int GetHashCode() => HashCode.Combine(Substance, _proportion, Seed, _isVein, _isPerturbation);

    /// <summary>
    /// Gets the richness of this <see cref="Resource"/> at the given <paramref
    /// name="position"/>.
    /// </summary>
    /// <param name="position">A normalized vector representing a direction from the center of
    /// the <see cref="Planetoid"/>.</param>
    /// <returns>The richness of this <see cref="Resource"/> at the given <paramref
    /// name="position"/>, as a value between 0 and 1.</returns>
    public double GetResourceRichnessAt(Vector3<HugeNumber> position)
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
