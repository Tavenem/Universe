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
        /// Finds the coordinates of the grid space which contains <see cref="Location.Position"/>.
        /// </summary>
        /// <remarks>
        /// Returns <see cref="Vector3.Zero"/> (invalid as grid coordinates) if <see cref="Region"/>
        /// is null.
        /// </remarks>
        public Vector3 GridSpace => Region?.PositionToGridCoords(Position) ?? Vector3.Zero;

        /// <summary>
        /// Gets a deep clone of this <see cref="RegionLocation"/>.
        /// </summary>
        public new RegionLocation GetDeepCopy() => GetDeepClone() as RegionLocation;

        /// <summary>
        /// Determines whether the specified <see cref="Location"/> is equivalent to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>
        /// <see langword="true"/> if the specified <see cref="Location"/> is equivalent to the
        /// current object; otherwise, <see langword="false"/>.
        /// </returns>
        public override bool Matches(Location obj) => obj is RegionLocation location && base.Matches(obj);

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
