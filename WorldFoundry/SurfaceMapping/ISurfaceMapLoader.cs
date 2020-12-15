using SixLabors.ImageSharp;
using System.Threading.Tasks;

namespace NeverFoundry.WorldFoundry
{
    /// <summary>
    /// Encapsulates saving and loading capabilities for surface maps.
    /// </summary>
    public interface ISurfaceMapLoader
    {
        /// <summary>
        /// Loads the image at the given path from a persistent store.
        /// </summary>
        /// <param name="path">The path from which to load an image.</param>
        /// <returns>
        /// The loaded image; or <see langword="null"/> if the operation fails.
        /// </returns>
        public Task<Image?> LoadAsync(string? path);

        /// <summary>
        /// Removes the image at the given path from a persistent store.
        /// </summary>
        /// <param name="path">The path at which to remove an image.</param>
        /// <returns>
        /// <see langword="true"/> if the image was removed; or <see langword="false"/> if the
        /// operation fails.
        /// </returns>
        public Task<bool> RemoveAsync(string? path);

        /// <summary>
        /// Saves the given image to a persistence store.
        /// </summary>
        /// <param name="image">The image to save.</param>
        /// <param name="id">The ID of the entity to which the map belongs.</param>
        /// <param name="path">The relative path at which to store this image.</param>
        /// <returns>
        /// The path at which the image was saved; or <see langword="null"/> if the operation fails.
        /// </returns>
        public Task<string?> SaveAsync(Image image, string? id, string? path);
    }
}
