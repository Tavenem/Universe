using System.Collections.Generic;
using WorldFoundry.Climate;
using WorldFoundry.Substances;

namespace WorldFoundry.CelestialBodies.Planetoids.Planets.TerrestrialPlanets
{
    /// <summary>
    /// A collection of parameters for habitability by a given form of life which a <see
    /// cref="TerrestrialPlanet"/> must meet in order for such life to survive unaided.
    /// </summary>
    public class HabitabilityRequirements
    {
        public ICollection<ComponentRequirement> AtmosphericRequirements { get; set; }
        public float? MinimumTemperature { get; set; }
        public float? MaximumTemperature { get; set; }
        public float? MinimumPressure { get; set; }
        public float? MaximumPressure { get; set; }
        public float? MinimumGravity { get; set; }
        public float? MaximumGravity { get; set; }

        public HabitabilityRequirements(
            List<ComponentRequirement> atmosphericRequirements,
            float? minimumTemperature, float? maximumTemperature,
            float? minimumPressure, float? maximumPressure,
            float? minimumGravity, float? maximumGravity)
        {
            AtmosphericRequirements = atmosphericRequirements;
            MinimumTemperature = minimumTemperature;
            MaximumTemperature = maximumTemperature;
            MinimumPressure = minimumPressure;
            MaximumPressure = maximumPressure;
            MinimumGravity = minimumGravity;
            MaximumGravity = maximumGravity;
        }

        /// <summary>
        /// The <see cref="TerrestrialPlanets.HabitabilityRequirements"/> for humans.
        /// </summary>
        /// <remarks>
        /// 236 K (-34 F) used as a minimum temperature: the average low of Yakutsk, a city with a
        /// permanent human population.
        ///
        /// 6.18 kPa is the Armstrong limit, where water boils at human body temperature.
        ///
        /// 4980 kPa is the critical point of oxygen, at which oxygen becomes a supercritical fluid.
        /// </remarks>
        public static HabitabilityRequirements HumanHabitabilityRequirements =
            new HabitabilityRequirements(
                Atmosphere.HumanBreathabilityRequirements,
                minimumTemperature: 236,
                maximumTemperature: 308,
                minimumPressure: 6.18f,
                maximumPressure: 4980,
                minimumGravity: 0,
                maximumGravity: 14.7f);
    }
}
