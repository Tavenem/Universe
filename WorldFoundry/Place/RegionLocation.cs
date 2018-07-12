using System.Collections.Generic;
using System.Numerics;
using WorldFoundry.Space;

namespace WorldFoundry.Place
{
    /// <summary>
    /// A <see cref="Location"/> within a <see cref="CelestialRegion"/>.
    /// </summary>
    public class RegionLocation : Location
    {
        /// <summary>
        /// This <see cref="RegionLocation"/>'s <see cref="Place.Entity"/>, as a <see cref="CelestialRegion"/>.
        /// </summary>
        public CelestialRegion Region => Entity as CelestialRegion;

        /// <summary>
        /// Gets a <see cref="Territory"/> which is equivalent to this <see cref="Location"/>.
        /// </summary>
        public override Territory ToTerritory()
        {
            var territory = new RegionTerritory { Entity = Entity };
            if (Region != null)
            {
                territory.GridSpaces = new HashSet<Vector3>(new Vector3[] { Region.PositionToGridCoords(Position) });
            }
            return territory;
        }
    }
}
