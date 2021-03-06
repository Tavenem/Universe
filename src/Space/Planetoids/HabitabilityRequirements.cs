using Tavenem.Universe.Chemistry;
using Tavenem.Universe.Climate;

namespace Tavenem.Universe.Space.Planetoids;

/// <summary>
/// A collection of parameters for habitability by a given form of life which a <see
/// cref="Planetoid"/> must meet in order for such life to survive unaided.
/// </summary>
public readonly struct HabitabilityRequirements : IEqualityOperators<HabitabilityRequirements, HabitabilityRequirements>
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
        minimumTemperature: 236,
        maximumTemperature: 308,
        minimumPressure: 6.18,
        maximumPressure: 4980,
        minimumGravity: 0,
        maximumGravity: 14.7,
        requireLiquidWater: true);

    /// <summary>
    /// Any requirements for the atmosphere.
    /// </summary>
    public IReadOnlyList<SubstanceRequirement> AtmosphericRequirements { get; }

    /// <summary>
    /// The minimum required temperature in K, if any.
    /// </summary>
    public double? MinimumTemperature { get; }

    /// <summary>
    /// The maximum required temperature in K, if any.
    /// </summary>
    public double? MaximumTemperature { get; }

    /// <summary>
    /// The minimum required pressure in kPa, if any.
    /// </summary>
    public double? MinimumPressure { get; }

    /// <summary>
    /// The maximum required pressure in kPa, if any.
    /// </summary>
    public double? MaximumPressure { get; }

    /// <summary>
    /// The minimum required gravity in m/s², if any.
    /// </summary>
    public double? MinimumGravity { get; }

    /// <summary>
    /// The maximum required gravity in m/s², if any.
    /// </summary>
    public double? MaximumGravity { get; }

    /// <summary>
    /// Whether liquid water is required (including subsurface liquid water).
    /// </summary>
    public bool RequireLiquidWater { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="HabitabilityRequirements"/> with the given parameters. Any (or all) may be left null.
    /// </summary>
    /// <param name="atmosphericRequirements">Any requirements for the atmosphere.</param>
    /// <param name="minimumTemperature">The minimum required temperature in K.</param>
    /// <param name="maximumTemperature">The maximum required temperature in K.</param>
    /// <param name="minimumPressure">The minimum required pressure in kPa.</param>
    /// <param name="maximumPressure">The maximum required pressure in kPa.</param>
    /// <param name="minimumGravity">The minimum required gravity in m/s².</param>
    /// <param name="maximumGravity">The maximum required gravity in m/s².</param>
    /// <param name="requireLiquidWater">
    /// Whether liquid water is required (including subsurface liquid water).
    /// </param>
    [System.Text.Json.Serialization.JsonConstructor]
    public HabitabilityRequirements(
        IReadOnlyList<SubstanceRequirement> atmosphericRequirements,
        double? minimumTemperature, double? maximumTemperature,
        double? minimumPressure, double? maximumPressure,
        double? minimumGravity, double? maximumGravity,
        bool requireLiquidWater)
    {
        AtmosphericRequirements = atmosphericRequirements;
        MinimumTemperature = minimumTemperature;
        MaximumTemperature = maximumTemperature;
        MinimumPressure = minimumPressure;
        MaximumPressure = maximumPressure;
        MinimumGravity = minimumGravity;
        MaximumGravity = maximumGravity;
        RequireLiquidWater = requireLiquidWater;
    }

    /// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
    /// <param name="other">An object to compare with this object.</param>
    /// <returns>
    /// <see langword="true" /> if the current object is equal to the <paramref name="other" />
    /// parameter; otherwise, <see langword="false" />.
    /// </returns>
    public bool Equals(HabitabilityRequirements other)
        => MinimumTemperature == other.MinimumTemperature
        && MaximumTemperature == other.MaximumTemperature
        && MinimumPressure == other.MinimumPressure
        && MaximumPressure == other.MaximumPressure
        && MinimumGravity == other.MinimumGravity
        && MaximumGravity == other.MaximumGravity
        && RequireLiquidWater == other.RequireLiquidWater
        && AtmosphericRequirements.OrderBy(x => x.GetHashCode()).SequenceEqual(other.AtmosphericRequirements.OrderBy(x => x.GetHashCode()));

    /// <summary>Indicates whether this instance and a specified object are equal.</summary>
    /// <param name="obj">The object to compare with the current instance.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="obj" /> and this instance are the same type
    /// and represent the same value; otherwise, <see langword="false" />.
    /// </returns>
    public override bool Equals(object? obj) => obj is HabitabilityRequirements requirements && Equals(requirements);

    /// <summary>Returns the hash code for this instance.</summary>
    /// <returns>A 32-bit signed integer that is the hash code for this instance.</returns>
    public override int GetHashCode() => HashCode.Combine(
        AtmosphericRequirements,
        MinimumTemperature,
        MaximumTemperature,
        MinimumPressure,
        MaximumPressure,
        MinimumGravity,
        MaximumGravity,
        RequireLiquidWater);

    /// <summary>Indicates whether two objects are equal.</summary>
    /// <param name="left">The first object to compare.</param>
    /// <param name="right">The second object to compare.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="left"/> is equal to <paramref
    /// name="right"/>; otherwise, <see langword="false" />.
    /// </returns>
    public static bool operator ==(HabitabilityRequirements left, HabitabilityRequirements right) => left.Equals(right);

    /// <summary>Indicates whether two objects are unequal.</summary>
    /// <param name="left">The first object to compare.</param>
    /// <param name="right">The second object to compare.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="left"/> is not equal to <paramref
    /// name="right"/>; otherwise, <see langword="false" />.
    /// </returns>
    public static bool operator !=(HabitabilityRequirements left, HabitabilityRequirements right) => !(left == right);
}
