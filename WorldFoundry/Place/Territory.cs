using MathAndScience.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using MathAndScience.Numerics;

namespace WorldFoundry.Place
{
    /// <summary>
    /// A collection of regions which define a conceptually unified area (though they may not form a
    /// contiguous region).
    /// </summary>
    public class Territory : Region
    {
        private readonly Lazy<List<Region>> _regions = new Lazy<List<Region>>();
        /// <summary>
        /// The <see cref="Region"/> instances which make up this <see cref="Territory"/>.
        /// </summary>
        public IEnumerable<Region> Regions => _regions.Value;

        /// <summary>
        /// Initializes a new instance of <see cref="Territory"/>.
        /// </summary>
        private protected Territory() { }

        /// <summary>
        /// Initializes a new instance of <see cref="Territory"/>.
        /// </summary>
        /// <param name="regions">The <see cref="Region"/> instances to include.</param>
        public Territory(IEnumerable<Region> regions) => AddRegions(regions);

        /// <summary>
        /// Initializes a new instance of <see cref="Territory"/>.
        /// </summary>
        /// <param name="regions">One or more <see cref="Region"/> instances to include.</param>
        public Territory(params Region[] regions) => AddRegions(regions);

        /// <summary>
        /// Adds the given <paramref name="region"/> to this instance.
        /// </summary>
        /// <param name="regions">The <see cref="Region"/> instances to add.</param>
        /// <returns>This instance.</returns>
        public Territory AddRegions(IEnumerable<Region> regions)
        {
            _regions.Value.AddRange(regions);
            CalculateShape();
            return this;
        }

        /// <summary>
        /// Adds the given <paramref name="regions"/> to this instance.
        /// </summary>
        /// <param name="regions">One or more <see cref="Region"/> instances to add.</param>
        /// <returns>This instance.</returns>
        public Territory AddRegions(params Region[] regions)
            => AddRegions(regions.AsEnumerable());

        /// <summary>
        /// Gets a deep clone of this <see cref="Place"/>.
        /// </summary>
        public override Location GetDeepClone() => GetDeepCopy();

        /// <summary>
        /// Gets a deep clone of this <see cref="Territory"/>.
        /// </summary>
        public new Territory GetDeepCopy() => new Territory().AddRegions(Regions.Select(x => x.GetDeepCopy()));

        private void CalculateShape()
        {
            if (!_regions.IsValueCreated || _regions.Value.Count == 0)
            {
                return;
            }

            var parent = _regions.Value[0].Parent;

            var regions = new List<(Vector3 position, double radius)>();
            if (Regions.Any(x => x.Parent != _regions.Value[0].Parent))
            {
                parent = Regions.Aggregate(
                    parent,
                    (p, x) => p?.GetCommonParent(x.Parent));
                if (parent == null)
                {
                    return;
                }

                regions.AddRange(Regions.Select(x => (parent.GetLocalizedPosition(x), x.Shape.ContainingRadius)));
            }
            else
            {
                regions.AddRange(Regions.Select(x => (x.Position, x.Shape.ContainingRadius)));
            }

            var center = Vector3.Zero;
            foreach (var (position, _) in regions)
            {
                center += position;
            }
            center /= regions.Count;

            Parent = parent;
            Shape = new Sphere(regions.Max(x => Vector3.Distance(x.position, center) + x.radius), center);
        }
    }
}
