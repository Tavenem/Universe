using Tavenem.Chemistry;
using Tavenem.Universe.Utilities;

namespace Tavenem.Universe.Space.Planetoids;

/// <summary>
/// A resource which may be found on a <see cref="Planetoid"/>.
/// </summary>
/// <param name="Substance">The substance found in the resource.</param>
/// <param name="Seed">The random seed to use.</param>
/// <param name="Proportion">
/// The proportion of the resource over the <see cref="Planetoid"/>'s surface, as a value between 0
/// and 1.
/// </param>
/// <param name="IsVein">Whether the resource occurs in veins.</param>
/// <param name="IsPerturbation">Whether the resource is a perturbation of another.</param>
public readonly record struct Resource(
    ISubstanceReference Substance,
    int Seed,
    decimal Proportion,
    bool IsVein,
    bool IsPerturbation = false) : IEquatable<Resource>
{
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
        var noise = GetNoise();
        if (IsPerturbation)
        {
            noise.GradientPerturbFractal(ref x, ref y, ref z);
        }
        var v = noise.GetNoise(x, y, z);
        if (IsVein)
        {
            v = 1 - v;
        }
        var proportion = (double)(Proportion - 0.5m);
        return Math.Max(0, (v + proportion) / (1 + proportion));
    }

    private FastNoise GetNoise()
    {
        var noise = new FastNoise(
            Seed,
            10,
            FastNoise.NoiseType.SimplexFractal,
            gain: 0.75f);
        noise.SetFractalLacunarity(3);
        if (IsVein)
        {
            noise.SetFractalType(FastNoise.FractalType.RigidMulti);
        }
        else
        {
            noise.SetFractalType(FastNoise.FractalType.Billow);
        }
        if (IsPerturbation)
        {
            noise.SetGradientPerturbAmp(20);
        }
        return noise;
    }
}
