using System;
using System.Numerics;
using WorldFoundry.CelestialBodies.Stars;

namespace WorldFoundry.Space.StarSystems
{
    /// <summary>
    /// A star system whose primary stellar body is a main sequence star.
    /// </summary>
    public class MainSequenceSystem : StarSystem
    {
        /// <summary>
        /// Initializes a new instance of <see cref="MainSequenceSystem"/>.
        /// </summary>
        public MainSequenceSystem() { }

        /// <summary>
        /// Initializes a new instance of <see cref="MainSequenceSystem"/> with the given parameters.
        /// </summary>
        /// <param name="parent">
        /// The containing <see cref="CelestialObject"/> in which this <see cref="MainSequenceSystem"/> is located.
        /// </param>
        /// <param name="position">The initial position of this <see cref="MainSequenceSystem"/>.</param>
        /// <param name="starType">The type of <see cref="Star"/> to include in this <see cref="MainSequenceSystem"/>.</param>
        /// <param name="spectralClass">
        /// The <see cref="Stars.SpectralClass"/> of the <see cref="Star"/> to include in this <see
        /// cref="MainSequenceSystem"/> (if null, will be pseudo-randomly determined).
        /// </param>
        /// <param name="populationII">
        /// Set to true if the <see cref="Star"/> to include in this <see cref="MainSequenceSystem"/> is to
        /// be a Population II <see cref="Star"/>.
        /// </param>
        public MainSequenceSystem(
            CelestialObject parent,
            Vector3 position,
            Type starType,
            SpectralClass? spectralClass = null,
            bool populationII = false) : base(parent, position, starType, spectralClass, LuminosityClass.V, populationII) { }
    }
}
