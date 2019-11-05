using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.WorldFoundry.CelestialBodies.Stars;
using System.Threading.Tasks;

namespace NeverFoundry.WorldFoundry.Space
{
    /// <summary>
    /// Defines a type of <see cref="StarSystem"/> child a <see cref="CelestialLocation"/> may have,
    /// and how to generate a new instance of that child.
    /// </summary>
    public interface IStarSystemChildDefinition : IChildDefinition
    {
        /// <summary>
        /// The <see cref="CelestialBodies.Stars.LuminosityClass"/> of the primary star of a star
        /// system child.
        /// </summary>
        LuminosityClass? LuminosityClass { get; }

        /// <summary>
        /// True if the primary star of the star system is to be a Population II star.
        /// </summary>
        bool PopulationII { get; }

        /// <summary>
        /// The <see cref="CelestialBodies.Stars.SpectralClass"/> of the primary star of a star
        /// system child.
        /// </summary>
        SpectralClass? SpectralClass { get; }

        /// <summary>
        /// Gets a new <see cref="StarSystem"/> as defined by this <see
        /// cref="StarSystemChildDefinition{T}"/>.
        /// </summary>
        /// <param name="parent">The location which contains the new one.</param>
        /// <param name="position">The position of the new location relative to the center of its
        /// <paramref name="parent"/>.</param>
        /// <returns>A new <see cref="StarSystem"/> as defined by this <see
        /// cref="StarSystemChildDefinition{T}"/>.</returns>
        Task<StarSystem?> GetStarSystemAsync(CelestialLocation parent, Vector3 position);
    }
}