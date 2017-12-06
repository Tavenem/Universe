using System;
using System.Numerics;
using WorldFoundry.Orbits;
using WorldFoundry.Space;

namespace WorldFoundry
{
    /// <summary>
    /// Describes an entity which can have temperature, including both physical bodies and regions of space.
    /// </summary>
    public class ThermalBody : Orbiter
    {
        private float? _temperature;
        /// <summary>
        /// The average temperature of this <see cref="ThermalBody"/>, in K.
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
            internal set => _temperature = value;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ThermalBody"/>.
        /// </summary>
        public ThermalBody() { }

        /// <summary>
        /// Initializes a new instance of <see cref="ThermalBody"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="ThermalBody"/> is located.
        /// </param>
        public ThermalBody(CelestialObject parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="ThermalBody"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="ThermalBody"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="ThermalBody"/>.</param>
        public ThermalBody(CelestialObject parent, Vector3 position) : base(parent, position) { }

        /// <summary>
        /// Determines a temperature for this <see cref="ThermalBody"/>, in K.
        /// </summary>
        private protected virtual void GenerateTemperature() => Temperature = 0;
    }
}
