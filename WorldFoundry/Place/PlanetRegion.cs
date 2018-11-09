using MathAndScience.Shapes;
using MathAndScience.Numerics;
using WorldFoundry.CelestialBodies.Planetoids;
using System;

namespace WorldFoundry.Place
{
    /// <summary>
    /// A <see cref="Region"/> on the surface of a <see cref="Planetoid"/>.
    /// </summary>
    public class PlanetRegion : Region
    {
        /// <summary>
        /// The elevation of this location above (or below) the surface of the <see cref="Planet"/>,
        /// in meters.
        /// </summary>
        public double Elevation { get; set; }

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
        /// The <see cref="Planetoid"/> on whose surface this <see cref="Region"/> is found.
        /// </summary>
        public Planetoid Planet { get; }

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
            }
        }

        /// <summary>
        /// Initializes a new instance of <see cref="PlanetRegion"/>.
        /// </summary>
        private protected PlanetRegion() { }

        /// <summary>
        /// Initializes a new instance of <see cref="PlanetRegion"/>.
        /// </summary>
        /// <param name="planet">The <see cref="Planetoid"/> on whose surface this location is
        /// found.</param>
        /// <param name="shape">The shape of the region.</param>
        /// <exception cref="ArgumentNullException"><paramref name="planet"/> cannot be <see
        /// langword="null"/>.</exception>
        public PlanetRegion(Planetoid planet, IShape shape)
            : base(shape) => Planet = planet ?? throw new ArgumentNullException(nameof(planet));

        /// <summary>
        /// Initializes a new instance of <see cref="Region"/>.
        /// </summary>
        /// <param name="planet">The <see cref="Planetoid"/> on whose surface this location is
        /// found.</param>
        /// <param name="containingRegion">The <see cref="Region"/> which contains this
        /// location.</param>
        /// <param name="shape">The shape of the region.</param>
        /// <exception cref="ArgumentNullException"><paramref name="planet"/> cannot be <see
        /// langword="null"/>.</exception>
        public PlanetRegion(Planetoid planet, Region containingRegion, IShape shape)
            : base(containingRegion, shape) => Planet = planet ?? throw new ArgumentNullException(nameof(planet));
    }
}
