using MathAndScience.Shapes;
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
        private List<Region> _regions;
        /// <summary>
        /// The <see cref="Region"/> instances which make up this <see cref="Territory"/>.
        /// </summary>
        public IEnumerable<Region> Regions => _regions ?? Enumerable.Empty<Region>();

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
        /// Adds the given <paramref name="regions"/> to this instance.
        /// </summary>
        /// <param name="regions">The <see cref="Region"/> instances to add.</param>
        /// <returns>This instance.</returns>
        public Territory AddRegions(IEnumerable<Region> regions)
        {
            (_regions ?? (_regions = new List<Region>())).AddRange(regions);
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

        private void CalculateShape()
        {
            if ((_regions?.Count ?? 0) == 0)
            {
                return;
            }

            var parent = _regions[0].ContainingRegion;

            var regions = new List<(Vector3 position, double radius)>();
            if (Regions.Any(x => x.ContainingRegion != _regions[0].ContainingRegion))
            {
                parent = Regions.Aggregate(
                    parent,
                    (p, x) => p?.GetCommonContainingRegion(x.ContainingRegion));
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

            SetNewContainingRegion(parent);
            Shape = new Sphere(regions.Max(x => Vector3.Distance(x.position, center) + x.radius), center);
        }
    }
}
