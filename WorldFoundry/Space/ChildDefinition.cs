using System;
using System.Collections.Generic;
using System.Linq;
using MathAndScience.Numerics;
using System.Reflection;
using WorldFoundry.Place;

namespace WorldFoundry.Space
{
    /// <summary>
    /// Defines a type of child a <see cref="CelestialRegion"/> may have, and how to generate a new
    /// instance of that child.
    /// </summary>
    public struct ChildDefinition
    {
        /// <summary>
        /// The parameters used to construct a new instance of this child.
        /// </summary>
        public object?[] ConstructionParameters { get; }

        /// <summary>
        /// The density of this type of child within the containing parent region.
        /// </summary>
        public double Density { get; }

        /// <summary>
        /// The radius of the open space required for this child type.
        /// </summary>
        public double Space { get; }

        /// <summary>
        /// The type of child.
        /// </summary>
        public Type Type { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="ChildDefinition"/>.
        /// </summary>
        /// <param name="type">The type of child.</param>
        /// <param name="space">The radius of the open space required for this child type.</param>
        /// <param name="density">The density of this type of child within the containing parent
        /// region.</param>
        public ChildDefinition(Type type, double space, double density)
        {
            Type = type;
            Density = density;
            Space = space;
            ConstructionParameters = new object[0];
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ChildDefinition"/>.
        /// </summary>
        /// <param name="type">The type of child.</param>
        /// <param name="space">The radius of the open space required for this child type.</param>
        /// <param name="density">The density of this type of child within the containing parent
        /// region.</param>
        /// <param name="constructionParameters">The parameters used to construct a new instance of
        /// this child.</param>
        public ChildDefinition(Type type, double space, double density, params object?[] constructionParameters)
        {
            Type = type;
            Density = density;
            Space = space;
            ConstructionParameters = constructionParameters;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ChildDefinition"/>.
        /// </summary>
        /// <param name="type">The type of child.</param>
        /// <param name="space">The radius of the open space required for this child type.</param>
        /// <param name="density">The density of this type of child within the containing parent
        /// region.</param>
        /// <param name="constructionParameters">The parameters used to construct a new instance of
        /// this child.</param>
        public ChildDefinition(Type type, double space, double density, IEnumerable<object?> constructionParameters)
        {
            Type = type;
            Density = density;
            Space = space;
            ConstructionParameters = constructionParameters?.ToArray() ?? new object[0];
        }

        /// <summary>
        /// Generates a new child from this definition for the given <paramref
        /// name="containingRegion"/> <see cref="CelestialRegion"/>, at the given <paramref
        /// name="position"/>.
        /// </summary>
        /// <param name="containingRegion">The parent <see cref="CelestialRegion"/> for the new
        /// child.</param>
        /// <param name="position">The position for the new child. If left <see langword="null"/>
        /// and <paramref name="containingRegion"/> is a <see cref="CelestialRegion"/>, a random
        /// position will be determined; if no free space can be found, no child will be
        /// generated.</param>
        /// <returns>A new child instance of the given <paramref name="containingRegion"/> at the
        /// given <paramref name="position"/>; or <see langword="null"/> if no child could be
        /// generated.</returns>
        public ICelestialLocation? GenerateChild(CelestialRegion containingRegion, Vector3? position = null)
        {
            if (!typeof(ICelestialLocation).IsAssignableFrom(Type))
            {
                return null;
            }
            if (position == null && containingRegion != null)
            {
                if (containingRegion.TryGetOpenSpace(Space, out var location))
                {
                    position = location;
                }
                else
                {
                    return null;
                }
            }
            var parameters = new List<object> { containingRegion, position ?? Vector3.Zero };
            parameters.AddRange(ConstructionParameters);
            var child = Type.InvokeMember(
                null,
                BindingFlags.CreateInstance | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                null,
                parameters.ToArray()) as ICelestialLocation;
            if (child is Location loc)
            {
                loc.Init();
            }
            return child;
        }
    }
}
