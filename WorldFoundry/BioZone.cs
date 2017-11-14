using System;

namespace WorldFoundry
{
    /// <summary>
    /// Describes an entity which has a specific chance of supporting life, including both physical
    /// bodies and regions of space.
    /// </summary>
    public class BioZone : ThermalBody
    {
        private float? _chanceOfLife;
        /// <summary>
        /// The chance that this <see cref="CelestialBody"/> and its children will actually have a
        /// biosphere, if it is habitable.
        /// </summary>
        /// <remarks>
        /// A value of 1 indicates that every body which could sustain life, does. A value of 0.5
        /// indicates that only half of potentially habitable worlds actually have living organisms.
        /// The lowest value in a world's parent hierarchy is used, which allows parent objects to
        /// override children to reflect inhospitable conditions (e.g. excessive radiation).
        /// </remarks>
        public float ChanceOfLife
        {
            get => _chanceOfLife ?? 1.0f;
            set => _chanceOfLife = value;
        }

        /// <summary>
        /// Determines the chance that this <see cref="CelestialBody"/> and its children will
        /// actually have a biosphere, if it is habitable.
        /// </summary>
        /// <returns>
        /// The chance that this <see cref="CelestialBody"/> and its children will actually have a
        /// biosphere, if it is habitable.
        /// </returns>
        internal float GetChanceOfLife() => Math.Min(Parent?.GetChanceOfLife() ?? 1.0f, _chanceOfLife ?? 1.0f);
    }
}
