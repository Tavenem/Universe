using System.Collections.Generic;
using System.Numerics;
using WorldFoundry.CelestialBodies.Planetoids;
using WorldFoundry.WorldGrids;

namespace WorldFoundry.Place
{
    /// <summary>
    /// A <see cref="Location"/> on the surface of a <see cref="Planetoid"/>.
    /// </summary>
    public class PlanetLocation : Location
    {
        /// <summary>
        /// This <see cref="PlanetLocation"/>'s <see cref="Place.Entity"/>, as a <see cref="Planetoid"/>.
        /// </summary>
        public Planetoid Planet => Entity as Planetoid;

        private Vector3 _position;
        /// <summary>
        /// The exact position within or on the <see cref="Place.Entity"/> represented by this <see cref="Location"/>.
        /// </summary>
        public override Vector3 Position
        {
            get => _position;
            set
            {
                _position = value;
                SetTile();
            }
        }

        private Tile _tile;
        /// <summary>
        /// The <see cref="WorldGrids.Tile"/> within which this <see cref="Location"/> is located.
        /// </summary>
        public Tile Tile
        {
            get
            {
                if (_tile == null)
                {
                    SetTile();
                }
                return _tile;
            }
            set
            {
                _tile = value;
                _position = value.Vector;
            }
        }

        /// <summary>
        /// Gets a deep clone of this <see cref="Place"/>.
        /// </summary>
        public override Place GetDeepClone() => new PlanetLocation
        {
            Entity = Entity,
            _position = _position,
            _tile = _tile,
        };

        /// <summary>
        /// Gets a deep clone of this <see cref="PlanetLocation"/>.
        /// </summary>
        public new PlanetLocation GetDeepCopy() => GetDeepClone() as PlanetLocation;

        /// <summary>
        /// Determines whether the specified <see cref="Location"/> is equivalent to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>
        /// <see langword="true"/> if the specified <see cref="Location"/> is equivalent to the
        /// current object; otherwise, <see langword="false"/>.
        /// </returns>
        public override bool Matches(Location obj) => obj is PlanetLocation location && base.Matches(obj);

        /// <summary>
        /// Gets a <see cref="Territory"/> which is equivalent to this <see cref="Location"/>.
        /// </summary>
        public override Territory ToTerritory()
        {
            var territory = new PlanetTerritory { Entity = Entity };
            if (Tile != null)
            {
                territory.TileIndexes = new HashSet<int>(new int[] { Tile.Index });
            }
            return territory;
        }

        private void SetTile()
        {
            if (Position != null)
            {
                var tile = Planet?.Topography?.GetClosestTile(Position);
                if (tile != null)
                {
                    _tile = tile;
                }
            }
        }
    }
}
