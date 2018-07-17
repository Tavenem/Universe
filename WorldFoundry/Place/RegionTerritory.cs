﻿using System.Collections.Generic;
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
        /// Gets a deep clone of this <see cref="Place"/>.
        /// </summary>
        public override Place GetDeepClone()
        {
            var territory = base.GetDeepClone() as RegionTerritory;
            if (GridSpaces?.Count > 0)
            {
                territory.GridSpaces = new HashSet<Vector3>(GridSpaces);
            }
            return territory;
        }
        /// <summary>
        /// Gets a deep clone of this <see cref="RegionTerritory"/>.
        /// </summary>
        public new RegionTerritory GetDeepCopy() => GetDeepClone() as RegionTerritory;

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

        /// <summary>
        /// Indicates whether this <see cref="Territory"/> overlaps the given <see cref="Place"/>.
        /// </summary>
        public override bool Overlaps(Place place)
        {
            if (Entity == place.Entity)
            {
                return (place is RegionTerritory crt
                    && ((GridSpaces == null && crt.GridSpaces == null)
                    || (GridSpaces?.Overlaps(crt.GridSpaces ?? Enumerable.Empty<Vector3>()) ?? false)))
                    || (place is Location l && (GridSpaces?.Contains(l.Position) ?? false));
            }
            else
            {
                return Entity != null && place.Entity?.FindCommonParent(Entity) == Entity;
            }
        }
    }
}
