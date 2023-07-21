using Tavenem.Chemistry;

namespace Tavenem.Universe.Chemistry;

/// <summary>
/// The requirements for a particular component in a mixture.
/// </summary>
/// <param name="Substance">The substance required.</param>
/// <param name="MinimumProportion">
/// <para>
/// The minimum proportion of this substance in the overall mixture.
/// </para>
/// <para>
/// Negative values are equivalent to zero.
/// </para>
/// </param>
/// <param name="MaximumProportion">
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
/// <param name="Phase">
/// The phase(s) required. If multiple phases are included, and any indicated phase is
/// present, the requirement is considered met.
/// </param>
public readonly record struct SubstanceRequirement(
    HomogeneousReference Substance,
    decimal MinimumProportion = 0,
    decimal? MaximumProportion = null,
    PhaseType Phase = PhaseType.Any)
{
    /// <summary>
    /// Determines whether this requirement is satisfied by the given <paramref
    /// name="material"/> under the given <paramref name="pressure"/>.
    /// </summary>
    /// <param name="material">The <see cref="IMaterial{TScalar}"/> instance to test.</param>
    /// <param name="pressure">A pressure, in kPa.</param>
    /// <returns><see langword="true"/> if the <paramref name="material"/> satisfies this
    /// requirement; otherwise <see langword="false"/>.</returns>
    public bool IsSatisfiedBy<T>(IMaterial<T> material, double pressure) where T : IFloatingPointIeee754<T>
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
}
