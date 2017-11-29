using System;
using System.Numerics;
using WorldFoundry.CelestialBodies.Stars;

namespace WorldFoundry.Space.StarSystems
{
    /// <summary>
    /// A star system whose primary stellar body is a main sequence star.
    /// </summary>
    public class MainSequenceOSystem : MainSequenceSystem
    {
        /// <summary>
        /// Initializes a new instance of <see cref="MainSequenceOSystem"/>.
        /// </summary>
        public MainSequenceOSystem() { }

        /// <summary>
        /// Initializes a new instance of <see cref="MainSequenceOSystem"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="MainSequenceOSystem"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="MainSequenceOSystem"/>.</param>
        /// <param name="starType">The type of <see cref="Star"/> to include in this <see cref="MainSequenceOSystem"/>.</param>
        /// <param name="populationII">
        /// Set to true if the <see cref="Star"/> to include in this <see cref="MainSequenceOSystem"/> is to
        /// be a Population II <see cref="Star"/>.
        /// </param>
        public MainSequenceOSystem(
            CelestialObject parent,
            Vector3 position,
            Type starType,
            bool populationII = false) : base(parent, position, starType, SpectralClass.O, populationII) { }
    }
}
