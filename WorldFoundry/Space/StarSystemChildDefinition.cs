using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.WorldFoundry.Place;
using NeverFoundry.WorldFoundry.Space.Stars;
using System.Collections.Generic;
using System.Linq;

namespace NeverFoundry.WorldFoundry.Space
{
    /// <summary>
    /// Defines a type of <see cref="StarSystem"/> child a <see cref="CosmicLocation"/> may have,
    /// and how to generate a new instance of that child.
    /// </summary>
    public class StarSystemChildDefinition : ChildDefinition
    {
        /// <summary>
        /// Whether a multiple-star system will satisfy this definition.
        /// </summary>
        public bool AllowBinary { get; }

        /// <summary>
        /// The <see cref="Stars.LuminosityClass"/> of the primary star of a star
        /// system child.
        /// </summary>
        public LuminosityClass? LuminosityClass { get; }

        /// <summary>
        /// True if the primary star of the star system is to be a Population II star.
        /// </summary>
        public bool PopulationII { get; }

        /// <summary>
        /// The <see cref="Stars.SpectralClass"/> of the primary star of a star
        /// system child.
        /// </summary>
        public SpectralClass? SpectralClass { get; }

        /// <summary>
        /// The type of the primary star of a star system child.
        /// </summary>
        public StarType StarType { get; }

        /// <summary>
        /// <para>
        /// If <see langword="true"/>, the system must have a single star similar to Sol, Earth's
        /// sun.
        /// </para>
        /// <para>
        /// Overrides the values of <see cref="LuminosityClass"/>, <see cref="SpectralClass"/>, <see
        /// cref="PopulationII"/>, and <see cref="AllowBinary"/>  if set to <see langword="true"/>.
        /// </para>
        /// </summary>
        public bool Sunlike { get; }

        /// <summary>
        /// The type of this child.
        /// </summary>
        public override CosmicStructureType StructureType => CosmicStructureType.StarSystem;

        /// <summary>
        /// Initializes a new instance of <see cref="StarSystemChildDefinition"/>.
        /// </summary>
        /// <param name="density">The density of this type of child within the containing parent
        /// region.</param>
        public StarSystemChildDefinition(Number density) : base(StarSystem.StarSystemSpace, density) { }

        /// <summary>
        /// Initializes a new instance of <see cref="StarSystemChildDefinition"/>.
        /// </summary>
        /// <param name="spectralClass">The <see cref="Stars.SpectralClass"/> of the
        /// primary star of the star system.</param>
        /// <param name="luminosityClass">
        /// The <see cref="Stars.LuminosityClass"/> of the primary star of the star
        /// system.
        /// </param>
        /// <param name="populationII">True if the primary star of the star system is to be a
        /// Population II star.</param>
        /// <param name="allowBinary">
        /// Whether a multiple-star system will satisfy this definition.
        /// </param>
        public StarSystemChildDefinition(
            SpectralClass? spectralClass = null,
            LuminosityClass? luminosityClass = null,
            bool populationII = false,
            bool allowBinary = true) : base(StarSystem.StarSystemSpace, Number.Zero)
        {
            StarType = StarType.MainSequence;
            SpectralClass = spectralClass;
            LuminosityClass = luminosityClass;
            PopulationII = populationII;
            AllowBinary = allowBinary;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="StarSystemChildDefinition"/>.
        /// </summary>
        /// <param name="starType">
        /// The type of the primary star of a star system child.
        /// </param>
        /// <param name="spectralClass">The <see cref="Stars.SpectralClass"/> of the
        /// primary star of the star system.</param>
        /// <param name="luminosityClass">
        /// The <see cref="Stars.LuminosityClass"/> of the primary star of the star
        /// system.
        /// </param>
        /// <param name="populationII">True if the primary star of the star system is to be a
        /// Population II star.</param>
        /// <param name="allowBinary">
        /// Whether a multiple-star system will satisfy this definition.
        /// </param>
        public StarSystemChildDefinition(
            StarType starType,
            SpectralClass? spectralClass = null,
            LuminosityClass? luminosityClass = null,
            bool populationII = false,
            bool allowBinary = true) : base(StarSystem.StarSystemSpace, Number.Zero)
        {
            StarType = starType;
            SpectralClass = spectralClass;
            LuminosityClass = luminosityClass;
            PopulationII = populationII;
            AllowBinary = allowBinary;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="StarSystemChildDefinition"/>.
        /// </summary>
        /// <param name="density">The density of this type of child within the containing parent
        /// region.</param>
        /// <param name="spectralClass">The <see cref="Stars.SpectralClass"/> of the
        /// primary star of the star system.</param>
        /// <param name="luminosityClass">
        /// The <see cref="Stars.LuminosityClass"/> of the primary star of the star
        /// system.
        /// </param>
        /// <param name="populationII">True if the primary star of the star system is to be a
        /// Population II star.</param>
        /// <param name="allowBinary">
        /// Whether a multiple-star system will satisfy this definition.
        /// </param>
        public StarSystemChildDefinition(
            Number density,
            SpectralClass? spectralClass = null,
            LuminosityClass? luminosityClass = null,
            bool populationII = false,
            bool allowBinary = true) : base(StarSystem.StarSystemSpace, density)
        {
            StarType = StarType.MainSequence;
            SpectralClass = spectralClass;
            LuminosityClass = luminosityClass;
            PopulationII = populationII;
            AllowBinary = allowBinary;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="StarSystemChildDefinition"/>.
        /// </summary>
        /// <param name="density">The density of this type of child within the containing parent
        /// region.</param>
        /// <param name="starType">
        /// The type of the primary star of a star system child.
        /// </param>
        /// <param name="spectralClass">The <see cref="Stars.SpectralClass"/> of the
        /// primary star of the star system.</param>
        /// <param name="luminosityClass">
        /// The <see cref="Stars.LuminosityClass"/> of the primary star of the star
        /// system.
        /// </param>
        /// <param name="populationII">True if the primary star of the star system is to be a
        /// Population II star.</param>
        /// <param name="allowBinary">
        /// Whether a multiple-star system will satisfy this definition.
        /// </param>
        public StarSystemChildDefinition(
            Number density,
            StarType starType,
            SpectralClass? spectralClass = null,
            LuminosityClass? luminosityClass = null,
            bool populationII = false,
            bool allowBinary = true) : base(StarSystem.StarSystemSpace, density)
        {
            StarType = starType;
            SpectralClass = spectralClass;
            LuminosityClass = luminosityClass;
            PopulationII = populationII;
            AllowBinary = allowBinary;
        }

        private StarSystemChildDefinition(bool sunlike) : base(StarSystem.StarSystemSpace, Number.Zero)
        {
            Sunlike = sunlike;
            StarType = StarType.MainSequence;
            SpectralClass = Stars.SpectralClass.G;
            LuminosityClass = Stars.LuminosityClass.V;
            AllowBinary = false;
        }

        /// <summary>
        /// Gets a new instance of <see cref="StarSystemChildDefinition"/> with <see
        /// cref="Sunlike"/> set to <see langword="true"/>.
        /// </summary>
        public static StarSystemChildDefinition GetNewSunlike()
            => new(true);

        /// <summary>
        /// Determines whether this <see cref="ChildDefinition"/> instance's parameters are
        /// satisfied by the given <paramref name="other"/> <see cref="ChildDefinition"/>
        /// instance's parameters.
        /// </summary>
        /// <param name="other">A <see cref="ChildDefinition"/> to test against this
        /// instance.</param>
        /// <returns><see langword="true"/> if this instance's parameters are satisfied by the
        /// <paramref name="other"/> instance's parameters; otherwise <see
        /// langword="false"/>.</returns>
        public override bool IsSatisfiedBy(ChildDefinition other)
        {
            if (other is not StarSystemChildDefinition ss
                || (ss.StarType & StarType) == StarType.None
                || ss.PopulationII != PopulationII
                || ss.AllowBinary != AllowBinary)
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
            return true;
        }

        /// <summary>
        /// Determines whether this <see cref="ChildDefinition"/> instance's parameters are
        /// satisfied by the given <paramref name="location"/>, discounting <see
        /// cref="ChildDefinition.Density"/>
        /// and <see cref="ChildDefinition.Space"/>.
        /// </summary>
        /// <param name="location">A <see cref="Location"/> to test against this instance.</param>
        /// <returns><see langword="true"/> if this instance's parameters are satisfied by the given
        /// <paramref name="location"/>; otherwise <see langword="false"/>.</returns>
        public override bool IsSatisfiedBy(CosmicLocation location)
        {
            if (location is not StarSystem ss)
            {
                return false;
            }
            if (!StarType.HasFlag(ss.StarType))
            {
                return false;
            }
            if (!AllowBinary && ss.StarIDs.Skip(1).Any())
            {
                return false;
            }
            if (LuminosityClass.HasValue && ss.LuminosityClass != LuminosityClass.Value)
            {
                return false;
            }
            if (SpectralClass.HasValue && ss.SpectralClass != SpectralClass.Value)
            {
                return false;
            }
            return ss.IsPopulationII != PopulationII;
        }

        /// <summary>
        /// Gets a new <see cref="StarSystem"/> as defined by this <see
        /// cref="StarSystemChildDefinition"/>.
        /// </summary>
        /// <param name="parent">The location which contains the new one.</param>
        /// <param name="position">
        /// The position of the new location relative to the center of its <paramref
        /// name="parent"/>.
        /// </param>
        /// <param name="children">
        /// <para>
        /// When this method returns, will be set to a <see cref="List{T}"/> of <see
        /// cref="CosmicLocation"/>s containing any child objects generated for the location during
        /// the creation process.
        /// </para>
        /// <para>
        /// This list may be useful, for instance, to ensure that these additional objects are also
        /// persisted to data storage.
        /// </para>
        /// </param>
        /// <returns>
        /// A new <see cref="StarSystem"/> as defined by this <see
        /// cref="StarSystemChildDefinition"/>.
        /// </returns>
        public StarSystem? GetStarSystem(CosmicLocation? parent, Vector3 position, out List<CosmicLocation> children) => new(
            parent,
            position,
            out children,
            orbit: null,
            StarType,
            SpectralClass,
            LuminosityClass,
            PopulationII,
            AllowBinary,
            Sunlike);

        /// <summary>
        /// Gets a new <see cref="CosmicLocation"/> as defined by this <see
        /// cref="ChildDefinition"/>.
        /// </summary>
        /// <param name="parent">The location which is to contain the new one.</param>
        /// <param name="position">
        /// The position of the new location relative to the center of the <paramref
        /// name="parent"/>.
        /// </param>
        /// <param name="children">
        /// <para>
        /// When this method returns, will be set to a <see cref="List{T}"/> of <see
        /// cref="CosmicLocation"/>s containing any child objects generated for the location during
        /// the creation process.
        /// </para>
        /// <para>
        /// This list may be useful, for instance, to ensure that these additional objects are also
        /// persisted to data storage.
        /// </para>
        /// </param>
        /// <returns>
        /// A new <see cref="CosmicLocation"/> as defined by this <see cref="ChildDefinition"/>.
        /// </returns>
        public override CosmicLocation? GetChild(CosmicLocation parent, Vector3 position, out List<CosmicLocation> children)
            => GetStarSystem(parent, position, out children);
    }
}
