using MathAndScience.Numerics;
using System;
using WorldFoundry.CelestialBodies.Planetoids;

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
                Position = Vector3.Normalize(Position) * (Planet.SeaLevel + Elevation);
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
                Position = Planet.LatitudeAndLongitudeToVector(value, Longitude);
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
                Position = Planet.LatitudeAndLongitudeToVector(Latitude, value);
            }
        }

        /// <summary>
        /// The <see cref="Planetoid"/> on whose surface this <see cref="Location"/> is found.
        /// </summary>
        public Planetoid Planet { get; private set; }

        private Vector3 _position;
        /// <summary>
        /// The exact position within or on the <see cref="Planet"/> represented by this <see cref="Location"/>.
        /// </summary>
        public override Vector3 Position
        {
            get => _position;
            set
            {
                _position = value;
                _latitude = Planet.VectorToLatitude(value);
                _longitude = Planet.VectorToLongitude(value);
                _elevation = value.Length() - Planet.SeaLevel;
            }
        }

        /// <summary>
        /// Initializes a new instance of <see cref="PlanetLocation"/>.
        /// </summary>
        private protected PlanetLocation() { }

        /// <summary>
        /// Initializes a new instance of <see cref="PlanetLocation"/>.
        /// </summary>
        /// <param name="planet">The <see cref="Planetoid"/> on whose surface this location is
        /// found.</param>
        /// <param name="position">The position of the location relative to the center of its
        /// <paramref name="containingRegion"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="planet"/> cannot be <see
        /// langword="null"/>.</exception>
        public PlanetLocation(Planetoid planet, Vector3 position)
            : base(position) => Planet = planet ?? throw new ArgumentNullException(nameof(planet));

        /// <summary>
        /// Initializes a new instance of <see cref="PlanetLocation"/>.
        /// </summary>
        /// <param name="planet">The <see cref="Planetoid"/> on whose surface this location is
        /// found.</param>
        /// <param name="containingRegion">The <see cref="Region"/> which contains this
        /// location.</param>
        /// <param name="position">The position of the location relative to the center of its
        /// <paramref name="containingRegion"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="planet"/> cannot be <see
        /// langword="null"/>.</exception>
        public PlanetLocation(Planetoid planet, Region containingRegion, Vector3 position)
            : base(containingRegion, position) => Planet = planet ?? throw new ArgumentNullException(nameof(planet));
    }
}
