using System;
using System.Numerics;
using WorldFoundry.CelestialBodies.Stars;

namespace WorldFoundry.Space.StarSystems
{
    /// <summary>
    /// A star system whose primary stellar body is a main sequence star.
    /// </summary>
    public class MainSequenceBSystem : MainSequenceSystem
    {
        /// <summary>
        /// Initializes a new instance of <see cref="MainSequenceBSystem"/>.
        /// </summary>
        public MainSequenceBSystem() { }

        /// <summary>
        /// Initializes a new instance of <see cref="MainSequenceBSystem"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="MainSequenceBSystem"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="MainSequenceBSystem"/>.</param>
        /// <param name="starType">The type of <see cref="Star"/> to include in this <see cref="MainSequenceBSystem"/>.</param>
        /// <param name="populationII">
        /// Set to true if the <see cref="Star"/> to include in this <see cref="MainSequenceBSystem"/> is to
        /// be a Population II <see cref="Star"/>.
        /// </param>
        public MainSequenceBSystem(
            CelestialObject parent,
            Vector3 position,
            Type starType,
            bool populationII = false) : base(parent, position, starType, SpectralClass.B, populationII) { }
    }
}
