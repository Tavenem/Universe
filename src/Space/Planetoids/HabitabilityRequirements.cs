using Tavenem.Universe.Chemistry;
using Tavenem.Universe.Climate;

namespace Tavenem.Universe.Space.Planetoids;

/// <summary>
/// A collection of parameters for habitability by a given form of life which a <see
/// cref="Planetoid"/> must meet in order for such life to survive unaided.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="HabitabilityRequirements"/> with the given parameters. Any (or all) may be left null.
/// </remarks>
/// <param name="AtmosphericRequirements">Any requirements for the atmosphere.</param>
/// <param name="MinimumTemperature">The minimum required temperature in K, if any.</param>
/// <param name="MaximumTemperature">The maximum required temperature in K, if any.</param>
/// <param name="MinimumPressure">The minimum required pressure in kPa, if any.</param>
/// <param name="MaximumPressure">The maximum required pressure in kPa, if any.</param>
/// <param name="MinimumGravity">The minimum required gravity in m/s², if any.</param>
/// <param name="MaximumGravity">The maximum required gravity in m/s², if any.</param>
/// <param name="RequireLiquidWater">
/// Whether liquid water is required (including subsurface liquid water).
/// </param>
public readonly record struct HabitabilityRequirements(
    IReadOnlyList<SubstanceRequirement> AtmosphericRequirements,
    double? MinimumTemperature,
    double? MaximumTemperature,
    double? MinimumPressure,
    double? MaximumPressure,
    double? MinimumGravity,
    double? MaximumGravity,
    bool RequireLiquidWater)
{
    /// <summary>
    /// The <see cref="HabitabilityRequirements"/> for humans.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 236 K (-34 F) used as a minimum temperature: the average low of Yakutsk, a city with a
    /// permanent human population.
    /// </para>
    /// <para>
    /// 6.18 kPa is the Armstrong limit, where water boils at human body temperature.
    /// </para>
    /// <para>
    /// 4980 kPa is the critical point of oxygen, at which oxygen becomes a supercritical fluid.
    /// </para>
    /// </remarks>
    public static readonly HabitabilityRequirements HumanHabitabilityRequirements = new(
        Atmosphere.HumanBreathabilityRequirements,
        236,
        308,
        6.18,
        4980,
        0,
        14.7,
        true);

    /// <summary>
    /// Indicates whether the current object is equal to another object of the same type.
    /// </summary>
    /// <param name="other">An object to compare with this object.</param>
    /// <returns>
    /// <see langword="true"/> if the current object is equal to the other parameter; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    public bool Equals(HabitabilityRequirements other)
        => RequireLiquidWater == other.RequireLiquidWater
        && MinimumTemperature.Equals(other.MinimumTemperature)
        && MaximumTemperature.Equals(other.MaximumTemperature)
        && MinimumPressure.Equals(other.MinimumPressure)
        && MaximumPressure.Equals(other.MaximumPressure)
        && MinimumGravity.Equals(other.MinimumGravity)
        && MaximumGravity.Equals(other.MaximumGravity)
        && AtmosphericRequirements
            .OrderBy(x => x.Substance.Id)
            .SequenceEqual(other
                .AtmosphericRequirements
                .OrderBy(x => x.Substance.Id));

    /// <summary>
    /// Indicates whether the current object is equal to another object of the same type.
    /// </summary>
    /// <param name="other">An object to compare with this object.</param>
    /// <returns>
    /// <see langword="true"/> if the current object is equal to the other parameter; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    public bool Equals(HabitabilityRequirements? other)
        => other is not null
        && Equals(other.Value);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(MinimumTemperature.GetHashCode());
        hashCode.Add(MaximumTemperature.GetHashCode());
        hashCode.Add(MinimumPressure.GetHashCode());
        hashCode.Add(MaximumPressure.GetHashCode());
        hashCode.Add(MinimumGravity.GetHashCode());
        hashCode.Add(MaximumGravity.GetHashCode());
        hashCode.Add(RequireLiquidWater.GetHashCode());
        hashCode.Add(GetAtmosphericRequirementsHashCode());
        return hashCode.ToHashCode();
    }

    private int GetAtmosphericRequirementsHashCode()
    {
        if (AtmosphericRequirements is null)
        {
            return 0;
        }
        unchecked
        {
            return 367 * AtmosphericRequirements
                .OrderBy(x => x.Substance.Id)
                .Aggregate(0, (a, c) => (a * 397) ^ c.GetHashCode());
        }
    }
}