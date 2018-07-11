using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using WorldFoundry.Space;

namespace WorldFoundry.Place
{
    /// <summary>
    /// A <see cref="Territory"/> within a <see cref="CelestialRegion"/>.
    /// </summary>
    public class RegionTerritory : Territory
    {
        /// <summary>
        /// The coordinates of <see cref="CelestialRegion.GridSpaces"/> included in this <see cref="RegionTerritory"/>.
        /// </summary>
        public HashSet<Vector3> GridSpaces { get; set; }

        /// <summary>
        /// This <see cref="RegionTerritory"/>'s <see cref="Place.Entity"/>, as a <see cref="CelestialRegion"/>.
        /// </summary>
        public CelestialRegion Region => Entity as CelestialRegion;

        /// <summary>
        /// Gets a shallow copy of this <see cref="Place"/>.
        /// </summary>
        public override Place GetCopy()
        {
            var territory = new RegionTerritory { Entity = Entity };
            if (GridSpaces?.Count > 0)
            {
                territory.GridSpaces = new HashSet<Vector3>(GridSpaces);
            }
            return territory;
        }

        /// <summary>
        /// Indicates whether this <see cref="Territory"/> includes the given <see cref="Place"/>.
        /// </summary>
        public override bool Includes(Place place)
        {
            if (Entity == place.Entity)
            {
                return (place is RegionTerritory crt
                    && ((GridSpaces == null && crt.GridSpaces == null)
                    || (GridSpaces?.IsSupersetOf(crt.GridSpaces ?? Enumerable.Empty<Vector3>()) ?? false)))
                    || (place is Location l && (GridSpaces?.Contains(l.Position) ?? false));
            }
            else
            {
                return Entity != null && place.Entity?.FindCommonParent(Entity) == Entity;
            }
        }
    }
}
