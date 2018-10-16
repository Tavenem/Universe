using Substances;
using System.Collections.Generic;
using System.Linq;
using WorldFoundry.Climate;

namespace WorldFoundry.CelestialBodies.Planetoids.Planets.TerrestrialPlanets
{
    /// <summary>
    /// A collection of parameters for habitability by a given form of life which a <see
    /// cref="TerrestrialPlanet"/> must meet in order for such life to survive unaided.
    /// </summary>
    public struct HabitabilityRequirements
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
        public static readonly HabitabilityRequirements HumanHabitabilityRequirements =
            new HabitabilityRequirements(
                Atmosphere.HumanBreathabilityRequirements,
                minimumTemperature: 236,
                maximumTemperature: 308,
                minimumPressure: 6.18,
                maximumPressure: 4980,
                minimumGravity: 0,
                maximumGravity: 14.7);

        /// <summary>
        /// Any requirements for the atmosphere.
        /// </summary>
        public List<Requirement> AtmosphericRequirements { get; }

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
        /// The minimum required gravity in N, if any.
        /// </summary>
        public double? MinimumGravity { get; }

        /// <summary>
        /// The maximum required gravity in N, if any.
        /// </summary>
        public double? MaximumGravity { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="HabitabilityRequirements"/> with the given parameters. Any (or all) may be left null.
        /// </summary>
        /// <param name="atmosphericRequirements">Any requirements for the atmosphere.</param>
        /// <param name="minimumTemperature">The minimum required temperature in K.</param>
        /// <param name="maximumTemperature">The maximum required temperature in K.</param>
        /// <param name="minimumPressure">The minimum required pressure in kPa.</param>
        /// <param name="maximumPressure">The maximum required pressure in kPa.</param>
        /// <param name="minimumGravity">The minimum required gravity in N.</param>
        /// <param name="maximumGravity">The maximum required gravity in N.</param>
        public HabitabilityRequirements(
            IEnumerable<Requirement> atmosphericRequirements,
            double? minimumTemperature, double? maximumTemperature,
            double? minimumPressure, double? maximumPressure,
            double? minimumGravity, double? maximumGravity)
        {
            AtmosphericRequirements = atmosphericRequirements.ToList();
            MinimumTemperature = minimumTemperature;
            MaximumTemperature = maximumTemperature;
            MinimumPressure = minimumPressure;
            MaximumPressure = maximumPressure;
            MinimumGravity = minimumGravity;
            MaximumGravity = maximumGravity;
        }
    }
}
