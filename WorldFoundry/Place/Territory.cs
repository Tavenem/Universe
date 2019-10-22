using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace WorldFoundry.Place
{
    /// <summary>
    /// A collection of locations which define a conceptually unified area (though they may not form
    /// a contiguous region).
    /// </summary>
    [Serializable]
    public class Territory : Location
    {
        /// <summary>
        /// Initializes a new instance of <see cref="Territory"/>.
        /// </summary>
        /// <param name="locations">The <see cref="Location"/> instances to include.</param>
        public Territory(IEnumerable<Location> locations) : base(SinglePoint.Origin, locations?.ToList()) => CalculateShape();

        /// <summary>
        /// Initializes a new instance of <see cref="Territory"/>.
        /// </summary>
        /// <param name="locations">One or more <see cref="Location"/> instances to include.</param>
        public Territory(params Location[] locations) : base(SinglePoint.Origin, locations.ToList()) => CalculateShape();

        private Territory(IShape shape, List<Location>? children = null) : base(shape, children) { }

        private Territory(SerializationInfo info, StreamingContext context) : this(
            (IShape)info.GetValue(nameof(Shape), typeof(IShape)),
            (List<Location>?)info.GetValue(nameof(Children), typeof(List<Location>))) { }

        /// <summary>
        /// Adds the given <paramref name="locations"/> to this instance.
        /// </summary>
        /// <param name="locations">The <see cref="Location"/> instances to add.</param>
        /// <returns>This instance.</returns>
        public Territory AddLocations(IEnumerable<Location> locations)
        {
            foreach (var location in locations)
            {
                AddChild(location);
            }
            CalculateShape();
            return this;
        }

        /// <summary>
        /// Adds the given <paramref name="regions"/> to this instance.
        /// </summary>
        /// <param name="regions">One or more <see cref="Location"/> instances to add.</param>
        /// <returns>This instance.</returns>
        public Territory AddRegions(params Location[] regions)
            => AddLocations(regions.AsEnumerable());

        /// <summary>
        /// Determines whether the specified <see cref="Location"/> is contained within the current
        /// instance.
        /// </summary>
        /// <param name="other">The instance to compare with this one.</param>
        /// <returns>
        /// <see langword="true"/> if the specified <see cref="Location"/> is contained within this
        /// instance; otherwise, <see langword="false"/>.
        /// </returns>
        public override bool Contains(Location other)
        {
            if (GetCommonParent(other) != this)
            {
                return false;
            }
            return Children.Any(x => x.Contains(other));
        }

        /// <summary>
        /// Determines whether the specified <paramref name="position"/> is contained within the
        /// current instance.
        /// </summary>
        /// <param name="position">a <see cref="Vector3"/> to check for inclusion in this <see
        /// cref="Location"/>.</param>
        /// <returns>
        /// <see langword="true"/> if the specified <paramref name="position"/> is contained within
        /// this instance; otherwise, <see langword="false"/>.
        /// </returns>
        public override bool Contains(Vector3 position) => Children.Any(x => x.Shape.IsPointWithin(position));

        private void CalculateShape()
        {
            if (!Children.Any())
            {
                return;
            }

            var parent = Children.First().Parent;

            var locations = new List<(Vector3 position, Number radius)>();
            if (Children.Any(x => x.Parent != parent))
            {
                parent = Children.Aggregate(
                    parent,
                    (p, x) => p?.GetCommonParent(x.Parent));
                if (parent == null)
                {
                    return;
                }

                locations.AddRange(Children.Select(x => (parent.GetLocalizedPosition(x), x.Shape.ContainingRadius)));
            }
            else
            {
                locations.AddRange(Children.Select(x => (x.Position, x.Shape.ContainingRadius)));
            }

            var center = Vector3.Zero;
            foreach (var (position, _) in locations)
            {
                center += position;
            }
            center /= locations.Count;

            Parent = parent;
            Shape = new Sphere(locations.Max(x => Vector3.Distance(x.position, center) + x.radius), center);
        }
    }
}
