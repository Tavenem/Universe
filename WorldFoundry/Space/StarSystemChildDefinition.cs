using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using WorldFoundry.CelestialBodies.Stars;
using WorldFoundry.Place;

namespace WorldFoundry.Space
{
    /// <summary>
    /// Defines a type of <see cref="StarSystem"/> child a <see cref="CelestialLocation"/> may have,
    /// and how to generate a new instance of that child.
    /// </summary>
    public class StarSystemChildDefinition<T> : ChildDefinition<StarSystem>, IStarSystemChildDefinition where T : Star
    {
        /// <summary>
        /// The <see cref="Stars.LuminosityClass"/> of the primary star of a star system child.
        /// </summary>
        public LuminosityClass? LuminosityClass { get; }

        /// <summary>
        /// True if the primary star of the star system is to be a Population II star.
        /// </summary>
        public bool PopulationII { get; }

        /// <summary>
        /// The <see cref="Stars.SpectralClass"/> of the primary star of a star system child.
        /// </summary>
        public SpectralClass? SpectralClass { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="ChildDefinition"/>.
        /// </summary>
        /// <param name="density">The density of this type of child within the containing parent
        /// region.</param>
        public StarSystemChildDefinition(Number density) : base(StarSystem.Space, density) { }

        /// <summary>
        /// Initializes a new instance of <see cref="StarSystemChildDefinition{T, U}"/>.
        /// </summary>
        /// <param name="spectralClass">The <see cref="Stars.SpectralClass"/> of the primary star of
        /// the star system.</param>
        /// <param name="luminosityClass">
        /// The <see cref="Stars.LuminosityClass"/> of the primary star of the star system.
        /// </param>
        /// <param name="populationII">True if the primary star of the star system is to be a
        /// Population II star.</param>
        public StarSystemChildDefinition(
            SpectralClass? spectralClass = null,
            LuminosityClass? luminosityClass = null,
            bool populationII = false) : base(StarSystem.Space, Number.Zero)
        {
            SpectralClass = spectralClass;
            LuminosityClass = luminosityClass;
            PopulationII = populationII;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="StarSystemChildDefinition{T, U}"/>.
        /// </summary>
        /// <param name="density">The density of this type of child within the containing parent
        /// region.</param>
        /// <param name="spectralClass">The <see cref="Stars.SpectralClass"/> of the primary star of
        /// the star system.</param>
        /// <param name="luminosityClass">
        /// The <see cref="Stars.LuminosityClass"/> of the primary star of the star system.
        /// </param>
        /// <param name="populationII">True if the primary star of the star system is to be a
        /// Population II star.</param>
        public StarSystemChildDefinition(
            Number density,
            SpectralClass? spectralClass = null,
            LuminosityClass? luminosityClass = null,
            bool populationII = false) : base(StarSystem.Space, density)
        {
            SpectralClass = spectralClass;
            LuminosityClass = luminosityClass;
            PopulationII = populationII;
        }

        /// <summary>
        /// Determines whether this <see cref="ChildDefinition"/> instance's parameters are
        /// satisfied by the given <paramref name="other"/> <see cref="ChildDefinition"/> instance's
        /// parameters, discounting <see cref="Density"/> and <see cref="Space"/>.
        /// </summary>
        /// <param name="other">A <see cref="ChildDefinition"/> to test against this
        /// instance.</param>
        /// <returns><see langword="true"/> if this instance's parameters are satisfied by the
        /// <paramref name="other"/> instance's parameters; otherwise <see
        /// langword="false"/>.</returns>
        public override bool IsSatisfiedBy(IChildDefinition other)
        {
            if (!(other is IStarSystemChildDefinition ss))
            {
                return false;
            }
            var genericType = other.GetType().GenericTypeArguments;
            if (genericType.Length < 1
                || !typeof(T).IsAssignableFrom(genericType[0]))
            {
                return false;
            }
            if (LuminosityClass.HasValue
                && (!ss.LuminosityClass.HasValue
                || ss.LuminosityClass.Value != LuminosityClass.Value))
            {
                return false;
            }
            if (SpectralClass.HasValue
                && (!ss.SpectralClass.HasValue
                || ss.SpectralClass.Value != SpectralClass.Value))
            {
                return false;
            }
            if (PopulationII && !ss.PopulationII)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Determines whether this <see cref="ChildDefinition"/> instance's parameters are
        /// satisfied by the given <paramref name="location"/>, discounting <see cref="Density"/>
        /// and <see cref="Space"/>.
        /// </summary>
        /// <param name="location">A <see cref="Location"/> to test against this instance.</param>
        /// <returns><see langword="true"/> if this instance's parameters are satisfied by the given
        /// <paramref name="location"/>; otherwise <see langword="false"/>.</returns>
        public override async Task<bool> IsSatisfiedByAsync(Location location)
        {
            if (!(location is StarSystem ss))
            {
                return false;
            }
            var stars = await ss.GetStarsAsync().OfType<T>().ToListAsync().ConfigureAwait(false);
            if (stars.Count == 0)
            {
                return false;
            }
            if (LuminosityClass.HasValue)
            {
                var lc = LuminosityClass!.Value;
                stars = stars.Where(x => x.LuminosityClass == lc).ToList();
                if (stars.Count == 0)
                {
                    return false;
                }
            }
            if (SpectralClass.HasValue)
            {
                var sc = SpectralClass!.Value;
                stars = stars.Where(x => x.SpectralClass == sc).ToList();
                if (stars.Count == 0)
                {
                    return false;
                }
            }
            return !PopulationII || stars.Any(x => x.IsPopulationII);
        }

        /// <summary>
        /// Gets a new <see cref="StarSystem"/> as defined by this <see
        /// cref="StarSystemChildDefinition{T, U}"/>.
        /// </summary>
        /// <param name="parent">The location which contains the new one.</param>
        /// <param name="position">The position of the new location relative to the center of its
        /// <paramref name="parent"/>.</param>
        /// <returns>A new <see cref="StarSystem"/> as defined by this <see
        /// cref="StarSystemChildDefinition{T, U}"/>.</returns>
        public Task<StarSystem?> GetStarSystemAsync(CelestialLocation parent, Vector3 position)
            => StarSystem.GetNewInstanceAsync<T>(
                parent,
                position,
                SpectralClass,
                LuminosityClass,
                PopulationII);
    }
}
