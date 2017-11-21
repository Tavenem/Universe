using System;
using System.Numerics;
using WorldFoundry.Space;

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
            private set => _chanceOfLife = value;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="BioZone"/>.
        /// </summary>
        public BioZone() { }

        /// <summary>
        /// Initializes a new instance of <see cref="BioZone"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="BioZone"/> is located.
        /// </param>
        public BioZone(CelestialObject parent) : base(parent) { }

        /// <summary>
        /// Initializes a new instance of <see cref="BioZone"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="BioZone"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="BioZone"/>.</param>
        public BioZone(CelestialObject parent, Vector3 position) : base(parent, position) { }

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
