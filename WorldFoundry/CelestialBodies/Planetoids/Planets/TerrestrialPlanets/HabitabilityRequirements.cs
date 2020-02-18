using NeverFoundry.WorldFoundry.Climate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace NeverFoundry.WorldFoundry.CelestialBodies.Planetoids.Planets.TerrestrialPlanets
{
    /// <summary>
    /// A collection of parameters for habitability by a given form of life which a <see
    /// cref="TerrestrialPlanet"/> must meet in order for such life to survive unaided.
    /// </summary>
    [Serializable]
    public struct HabitabilityRequirements : ISerializable
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
        public SubstanceRequirement[] AtmosphericRequirements { get; }

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
        /// Initializes a new instance of <see cref="HabitabilityRequirements"/> with the given parameters. Any (or all) may be left null.
        /// </summary>
        /// <param name="atmosphericRequirements">Any requirements for the atmosphere.</param>
        /// <param name="minimumTemperature">The minimum required temperature in K.</param>
        /// <param name="maximumTemperature">The maximum required temperature in K.</param>
        /// <param name="minimumPressure">The minimum required pressure in kPa.</param>
        /// <param name="maximumPressure">The maximum required pressure in kPa.</param>
        /// <param name="minimumGravity">The minimum required gravity in m/s².</param>
        /// <param name="maximumGravity">The maximum required gravity in m/s².</param>
        public HabitabilityRequirements(
            IEnumerable<SubstanceRequirement> atmosphericRequirements,
            double? minimumTemperature, double? maximumTemperature,
            double? minimumPressure, double? maximumPressure,
            double? minimumGravity, double? maximumGravity)
        {
            AtmosphericRequirements = atmosphericRequirements.ToArray();
            MinimumTemperature = minimumTemperature;
            MaximumTemperature = maximumTemperature;
            MinimumPressure = minimumPressure;
            MaximumPressure = maximumPressure;
            MinimumGravity = minimumGravity;
            MaximumGravity = maximumGravity;
        }

        private HabitabilityRequirements(SerializationInfo info, StreamingContext context) : this(
            (SubstanceRequirement[])info.GetValue(nameof(AtmosphericRequirements), typeof(SubstanceRequirement[])),
            (double?)info.GetValue(nameof(MinimumTemperature), typeof(double?)),
            (double?)info.GetValue(nameof(MaximumTemperature), typeof(double?)),
            (double?)info.GetValue(nameof(MinimumPressure), typeof(double?)),
            (double?)info.GetValue(nameof(MaximumPressure), typeof(double?)),
            (double?)info.GetValue(nameof(MinimumGravity), typeof(double?)),
            (double?)info.GetValue(nameof(MaximumGravity), typeof(double?)))
        { }

        /// <summary>Populates a <see cref="SerializationInfo"></see> with the data needed to
        /// serialize the target object.</summary>
        /// <param name="info">The <see cref="SerializationInfo"></see> to populate with
        /// data.</param>
        /// <param name="context">The destination (see <see cref="StreamingContext"></see>) for this
        /// serialization.</param>
        /// <exception cref="System.Security.SecurityException">The caller does not have the
        /// required permission.</exception>
        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(AtmosphericRequirements), AtmosphericRequirements);
            info.AddValue(nameof(MinimumTemperature), MinimumTemperature);
            info.AddValue(nameof(MaximumTemperature), MaximumTemperature);
            info.AddValue(nameof(MinimumPressure), MinimumPressure);
            info.AddValue(nameof(MaximumPressure), MaximumPressure);
            info.AddValue(nameof(MinimumGravity), MinimumGravity);
            info.AddValue(nameof(MaximumGravity), MaximumGravity);
        }
    }
}
