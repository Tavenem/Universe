using System.Collections.Generic;
using System.Linq;
using WorldFoundry.CelestialBodies.Planetoids;
using WorldFoundry.WorldGrids;

namespace WorldFoundry.Place
{
    /// <summary>
    /// A <see cref="Territory"/> on the surface of a <see cref="Planetoid"/>.
    /// </summary>
    public class PlanetTerritory : Territory
    {
        /// <summary>
        /// This <see cref="PlanetTerritory"/>'s <see cref="Place.Entity"/>, as a <see cref="Planetoid"/>.
        /// </summary>
        public Planetoid Planet => Entity as Planetoid;

        /// <summary>
        /// The indexes of <see cref="WorldGrids.Tile"/>s included in this <see cref="PlanetTerritory"/>.
        /// </summary>
        public HashSet<int> TileIndexes { get; set; }

        /// <summary>
        /// Enumerates the <see cref="Tile"/>s referenced by the <see cref="TileIndexes"/> of this
        /// <see cref="PlanetTerritory"/>.
        /// </summary>
        public IEnumerable<Tile> Tiles => (Planet?.Topography == null || TileIndexes == null)
            ? Enumerable.Empty<Tile>()
            : TileIndexes
                .Where(x => x >= 0 && x < Planet.Topography.Tiles.Length)
                .Select(x => Planet.Topography.Tiles[x]);

        /// <summary>
        /// Gets a deep clone of this <see cref="Place"/>.
        /// </summary>
        public override Place GetDeepClone()
        {
            var territory = base.GetDeepClone() as PlanetTerritory;
            if (TileIndexes?.Count > 0)
            {
                territory.TileIndexes = new HashSet<int>(TileIndexes);
            }
            return territory;
        }
        /// <summary>
        /// Gets a deep clone of this <see cref="PlanetTerritory"/>.
        /// </summary>
        public new PlanetTerritory GetDeepCopy() => GetDeepClone() as PlanetTerritory;

        /// <summary>
        /// Indicates whether this <see cref="Territory"/> includes the given <see cref="Place"/>.
        /// </summary>
        public override bool Includes(Place place)
            => Entity == place.Entity
            && ((place is PlanetTerritory pt
            && ((TileIndexes == null && pt.TileIndexes == null)
            || (TileIndexes?.IsSupersetOf(pt?.TileIndexes ?? Enumerable.Empty<int>()) ?? false)))
            || (place is PlanetLocation pl && pl.Tile != null && (TileIndexes?.Contains(pl.Tile.Index) ?? false)));

        /// <summary>
        /// Indicates whether this <see cref="Territory"/> overlaps the given <see cref="Place"/>.
        /// </summary>
        public override bool Overlaps(Place place)
            => Entity == place.Entity
            && ((place is PlanetTerritory pt
            && ((TileIndexes == null && pt.TileIndexes == null)
            || (TileIndexes?.Overlaps(pt?.TileIndexes ?? Enumerable.Empty<int>()) ?? false)))
            || (place is PlanetLocation pl && pl.Tile != null && (TileIndexes?.Contains(pl.Tile.Index) ?? false)));
    }
}
