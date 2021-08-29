using System.Text.Json.Serialization;
using Tavenem.Chemistry;

namespace Tavenem.Universe.Chemistry;

/// <summary>
/// The requirements for a particular component in a mixture.
/// </summary>
public readonly struct SubstanceRequirement : IEquatable<SubstanceRequirement>
{
    /// <summary>
    /// <para>
    /// The maximum proportion of this substance in the overall mixture.
    /// </para>
    /// <para>
    /// Negative values are equivalent to zero.
    /// </para>
    /// <para>
    /// May be <see langword="null"/>, which indicates no maximum.
    /// </para>
    /// </summary>
    public decimal? MaximumProportion { get; }

    /// <summary>
    /// <para>
    /// The minimum proportion of this substance in the overall mixture.
    /// </para>
    /// <para>
    /// Negative values are equivalent to zero.
    /// </para>
    /// </summary>
    public decimal MinimumProportion { get; }

    /// <summary>
    /// The phase(s) required. If multiple phases are included, and any indicated phase is
    /// present, the requirement is considered met.
    /// </summary>
    public PhaseType Phase { get; }

    /// <summary>
    /// The substance required.
    /// </summary>
    public HomogeneousReference Substance { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="SubstanceRequirement"/>.
    /// </summary>
    /// <param name="substance">The substance required.</param>
    /// <param name="minimumProportion">
    /// <para>
    /// The minimum proportion of this substance in the overall mixture.
    /// </para>
    /// <para>
    /// Negative values are equivalent to zero.
    /// </para>
    /// </param>
    /// <param name="maximumProportion">
    /// <para>
    /// The maximum proportion of this substance in the overall mixture.
    /// </para>
    /// <para>
    /// Negative values are equivalent to zero.
    /// </para>
    /// <para>
    /// May be <see langword="null"/>, which indicates no maximum.
    /// </para>
    /// </param>
    /// <param name="phase">
    /// The phase(s) required. If multiple phases are included, and any indicated phase is
    /// present, the requirement is considered met.
    /// </param>
    [JsonConstructor]
    public SubstanceRequirement(
        HomogeneousReference substance,
        decimal minimumProportion = 0,
        decimal? maximumProportion = null,
        PhaseType phase = PhaseType.Any)
    {
        Substance = substance;
        MinimumProportion = minimumProportion;
        MaximumProportion = maximumProportion;
        Phase = phase;
    }

    /// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
    /// <param name="other">An object to compare with this object.</param>
    /// <returns>
    /// <see langword="true" /> if the current object is equal to the <paramref name="other" />
    /// parameter; otherwise, <see langword="false" />.
    /// </returns>
    public bool Equals(SubstanceRequirement other) => MaximumProportion == other.MaximumProportion
        && MinimumProportion == other.MinimumProportion
        && Phase == other.Phase
        && Substance.Equals(other.Substance);

    /// <summary>Indicates whether this instance and a specified object are equal.</summary>
    /// <param name="obj">The object to compare with the current instance.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="obj" /> and this instance are the same type
    /// and represent the same value; otherwise, <see langword="false" />.
    /// </returns>
    public override bool Equals(object? obj) => obj is SubstanceRequirement requirement && Equals(requirement);

    /// <summary>Returns the hash code for this instance.</summary>
    /// <returns>A 32-bit signed integer that is the hash code for this instance.</returns>
    public override int GetHashCode() => HashCode.Combine(MaximumProportion, MinimumProportion, Phase, Substance);

    /// <summary>
    /// Determines whether this requirement is satisfied by the given <paramref
    /// name="material"/> under the given <paramref name="pressure"/>.
    /// </summary>
    /// <param name="material">The <see cref="IMaterial{TScalar}"/> instance to test.</param>
    /// <param name="pressure">A pressure, in kPa.</param>
    /// <returns><see langword="true"/> if the <paramref name="material"/> satisfies this
    /// requirement; otherwise <see langword="false"/>.</returns>
    public bool IsSatisfiedBy<T>(IMaterial<T> material, double pressure) where T : IFloatingPoint<T>
    {
        if (material.IsEmpty)
        {
            return false;
        }

        var matches = new List<(IHomogeneous substance, decimal proportion)>();
        foreach (var (constituent, constituentProportion) in material.Constituents)
        {
            if (constituent.Substance is IHomogeneous homogeneous)
            {
                if (constituent.Equals(Substance))
                {
                    matches.Add((homogeneous, constituentProportion));
                }
            }
            else
            {
                foreach (var (constituentConstituent, constituentConstituentProportion) in constituent.Substance.Constituents)
                {
                    if (constituentConstituent.Equals(Substance))
                    {
                        matches.Add((constituentConstituent.Homogeneous, constituentConstituentProportion * constituentProportion));
                    }
                }
            }
        }
        if (matches.Count == 0)
        {
            return MinimumProportion == 0;
        }

        var proportion = matches.Sum(x => x.proportion);
        if (proportion < MinimumProportion)
        {
            return false;
        }

        if (MaximumProportion.HasValue && proportion > MaximumProportion.Value)
        {
            return false;
        }

        var phaseMatch = false;
        foreach (var match in matches)
        {
            var phase = match.substance.GetPhase(material.Temperature ?? 0, pressure);
            if ((phase & Phase) != PhaseType.None)
            {
                phaseMatch = true;
                break;
            }
        }
        return phaseMatch;
    }

    /// <summary>Indicates whether two objects are equal.</summary>
    /// <param name="left">The first object to compare.</param>
    /// <param name="right">The second object to compare.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="left"/> is equal to <paramref
    /// name="right"/>; otherwise, <see langword="false" />.
    /// </returns>
    public static bool operator ==(SubstanceRequirement left, SubstanceRequirement right) => left.Equals(right);

    /// <summary>Indicates whether two objects are unequal.</summary>
    /// <param name="left">The first object to compare.</param>
    /// <param name="right">The second object to compare.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="left"/> is not equal to <paramref
    /// name="right"/>; otherwise, <see langword="false" />.
    /// </returns>
    public static bool operator !=(SubstanceRequirement left, SubstanceRequirement right) => !(left == right);
}
