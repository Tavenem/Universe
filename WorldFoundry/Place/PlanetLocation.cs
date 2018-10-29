using MathAndScience.Numerics;
using WorldFoundry.CelestialBodies.Planetoids;
using WorldFoundry.Space;

namespace WorldFoundry.Place
{
    /// <summary>
    /// A <see cref="Location"/> on the surface of a <see cref="Planetoid"/>.
    /// </summary>
    public class PlanetLocation : Location
    {
        private double _elevation;
        /// <summary>
        /// The elevation of this location above (or below) the surface of the <see cref="Planet"/>,
        /// in meters. Derived from <see cref="Position"/>.
        /// </summary>
        public double Elevation
        {
            get => _elevation;
            set
            {
                _elevation = value;
                if (Planet != null)
                {
                    Position = Vector3.Normalize(Position) * (Planet.SeaLevel + Elevation);
                }
            }
        }

        private double _latitude;
        /// <summary>
        /// The latitude of this location. Derived from <see cref="Position"/>.
        /// </summary>
        public double Latitude
        {
            get => _latitude;
            set
            {
                _latitude = value;
                if (Planet != null)
                {
                    Position = Planet.LatitudeAndLongitudeToVector(value, Longitude);
                }
            }
        }

        private double _longitude;
        /// <summary>
        /// The longitude of this location. Derived from <see cref="Position"/>.
        /// </summary>
        public double Longitude
        {
            get => _longitude;
            set
            {
                _longitude = value;
                if (Planet != null)
                {
                    Position = Planet.LatitudeAndLongitudeToVector(Latitude, value);
                }
            }
        }

        /// <summary>
        /// This <see cref="PlanetLocation"/>'s <see cref="Location.CelestialEntity"/>, as a <see
        /// cref="Planetoid"/>.
        /// </summary>
        public Planetoid Planet => CelestialEntity as Planetoid;

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
                if (Planet != null)
                {
                    _latitude = Planet.VectorToLatitude(value);
                    _longitude = Planet.VectorToLongitude(value);
                    _elevation = value.Length() - Planet.SeaLevel;
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of <see cref="PlanetLocation"/>.
        /// </summary>
        private protected PlanetLocation() { }

        /// <summary>
        /// Initializes a new instance of <see cref="PlanetLocation"/>.
        /// </summary>
        /// <param name="celestialEntity">The <see cref="Space.CelestialEntity"/> which represents
        /// this location (may be <see langword="null"/>).</param>
        /// <param name="position">The position of the location relative to the center of its parent
        /// entity.</param>
        public PlanetLocation(CelestialEntity celestialEntity, Vector3 position) : base(celestialEntity, position) { }

        /// <summary>
        /// Initializes a new instance of <see cref="PlanetLocation"/>.
        /// </summary>
        /// <param name="celestialEntity">The <see cref="Space.CelestialEntity"/> which represents
        /// this location (may be <see langword="null"/>).</param>
        /// <param name="parent">The parent location in which this one is found.</param>
        /// <param name="position">The position of the location relative to the center of its parent
        /// entity.</param>
        public PlanetLocation(CelestialEntity celestialEntity, Location parent, Vector3 position) : base(celestialEntity, parent, position) { }

        /// <summary>
        /// Gets a deep clone of this <see cref="Place"/>.
        /// </summary>
        public override Location GetDeepClone() => GetDeepCopy();

        /// <summary>
        /// Gets a deep clone of this <see cref="PlanetLocation"/>.
        /// </summary>
        public PlanetLocation GetDeepCopy() => new PlanetLocation(CelestialEntity, Parent, Position);
    }
}
