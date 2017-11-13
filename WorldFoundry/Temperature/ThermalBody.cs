using System;
using WorldFoundry.Orbits;

namespace WorldFoundry.Temperature
{
    /// <summary>
    /// Describes an entity which can have temperature, including both physical bodies and regions of space.
    /// </summary>
    public class ThermalBody : Orbiter
    {
        private float? _temperature;
        /// <summary>
        /// The average temperature of this <see cref="ThermalBody"/> (in K).
        /// </summary>
        public virtual float? Temperature
        {
            get
            {
                if (!_temperature.HasValue)
                {
                    GenerateTemperature();
                }

                if (Parent == null || !_temperature.HasValue)
                {
                    return _temperature;
                }
                else // At least parent's ambient temperature.
                {
                    return Math.Max(_temperature ?? 0, Parent.Temperature ?? 0);
                }
            }
            set => _temperature = value;
        }

        /// <summary>
        /// Determines a temperature (in K) for this <see cref="ThermalBody"/>.
        /// </summary>
        protected virtual void GenerateTemperature() => Temperature = 0;
    }
}
