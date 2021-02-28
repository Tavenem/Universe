using NeverFoundry.MathAndScience;
using NeverFoundry.MathAndScience.Constants.Numbers;
using NeverFoundry.MathAndScience.Numerics;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.WorldFoundry.Space;
using System;
using System.Runtime.Serialization;

namespace NeverFoundry.WorldFoundry.Place
{
    /// <summary>
    /// A <see cref="Location"/> on the surface of a <see cref="Planetoid"/>, which can override
    /// local conditions with manually-specified maps.
    /// </summary>
    [Serializable]
    [System.Text.Json.Serialization.JsonConverter(typeof(SurfaceRegionConverter))]
    public class SurfaceRegion : Location
    {
        /// <summary>
        /// The type discriminator for this type.
        /// </summary>
        public const string SurfaceRegionIdItemTypeName = ":Location:SurfaceRegion:";
        /// <summary>
        /// A built-in, read-only type discriminator.
        /// </summary>
        public override string IdItemTypeName => SurfaceRegionIdItemTypeName;

        /// <summary>
        /// The normalized position of this region on the surface of the planet.
        /// </summary>
        /// <remarks>
        /// The <see cref="Location.Position"/> of a region is always the 0,0,0 vector, as they are
        /// represented as a frustum emanating from the center of the planet.
        /// </remarks>
        public Vector3 PlanetaryPosition => ((Frustum)Shape).Axis.Normalize();

        /// <summary>
        /// Initializes a new instance of <see cref="SurfaceRegion"/>.
        /// </summary>
        /// <param name="planet">The planet on which this region is found.</param>
        /// <param name="position">The normalized position vector of this region.</param>
        /// <param name="latitudeRange">
        /// <para>
        /// The range of latitudes encompassed by this region, as an angle (in radians).
        /// </para>
        /// <para>
        /// Maximum value is π (a full hemisphere, which produces the full globe).
        /// </para>
        /// </param>
        public SurfaceRegion(Planetoid planet, Vector3 position, Number latitudeRange)
            : base(planet.Id, new Frustum(2, position * (planet.Shape.ContainingRadius + planet.Atmosphere.AtmosphericHeight), Number.Min(latitudeRange, MathConstants.PI), 0)) { }

        /// <summary>
        /// Initializes a new instance of <see cref="SurfaceRegion"/>.
        /// </summary>
        /// <param name="planet">The planet on which this region is found.</param>
        /// <param name="latitude">The latitude of the center of the region.</param>
        /// <param name="longitude">The longitude of the center of the region.</param>
        /// <param name="latitudeRange">
        /// <para>
        /// The range of latitudes encompassed by this region, as an angle (in radians).
        /// </para>
        /// <para>
        /// Maximum value is π (a full hemisphere, which produces the full globe).
        /// </para>
        /// </param>
        public SurfaceRegion(Planetoid planet, double latitude, double longitude, Number latitudeRange)
            : base(planet.Id, new Frustum(2, planet.LatitudeAndLongitudeToVector(latitude, longitude) * (planet.Shape.ContainingRadius + planet.Atmosphere.AtmosphericHeight), Number.Min(latitudeRange, MathConstants.PI), 0)) { }

        /// <summary>
        /// Initializes a new instance of <see cref="SurfaceRegion"/>.
        /// </summary>
        /// <param name="id">The unique ID of this item.</param>
        /// <param name="idItemTypeName">The type discriminator.</param>
        /// <param name="shape">The shape of the location.</param>
        /// <param name="parentId">The ID of the location which contains this one.</param>
        /// <param name="absolutePosition">
        /// <para>
        /// The position of this location, as a set of relative positions starting with the position
        /// of its outermost containing parent within the universe, down to the relative position of
        /// its most immediate parent.
        /// </para>
        /// <para>
        /// The location's own relative <see cref="Location.Position"/> is not expected to be
        /// included.
        /// </para>
        /// <para>
        /// May be <see langword="null"/> for a location with no containing parent, or whose parent
        /// is the universe itself (i.e. there is no intermediate container).
        /// </para>
        /// </param>
        /// <remarks>
        /// Note: this constructor is most useful for deserializers. The other constructors are more
        /// suited to creating a new instance, as they will automatically generate an appropriate
        /// ID.
        /// </remarks>
        public SurfaceRegion(
            string id,
#pragma warning disable IDE0060 // Remove unused parameter: required for deserialization
            string idItemTypeName,
#pragma warning restore IDE0060 // Remove unused parameter
            IShape shape,
            string? parentId,
            Vector3[]? absolutePosition = null) : base(id, shape, parentId, absolutePosition)
        { }

        private SurfaceRegion(SerializationInfo info, StreamingContext context) : this(
            (string?)info.GetValue(nameof(Id), typeof(string)) ?? string.Empty,
            SurfaceRegionIdItemTypeName,
            (IShape?)info.GetValue(nameof(Shape), typeof(IShape)) ?? SinglePoint.Origin,
            (string?)info.GetValue(nameof(ParentId), typeof(string)) ?? string.Empty,
            (Vector3[]?)info.GetValue(nameof(AbsolutePosition), typeof(Vector3[])))
        { }

        /// <summary>
        /// Gets a new instance of <see cref="SurfaceRegion"/>.
        /// </summary>
        /// <param name="planet">The planet on which this region is found.</param>
        /// <param name="northLatitude">The latitude of the northwest corner of the region.</param>
        /// <param name="westLongitude">The longitude of the northwest corner of the region.</param>
        /// <param name="southLatitude">The latitude of the southeast corner of the region.</param>
        /// <param name="eastLongitude">The longitude of the southeast corner of the region.</param>
        /// <remarks>
        /// The region's actual size will be adjusted to ensure that the boundaries indicated would
        /// be fully displayed on both an equirectangular and a cylindrical equal-area projection of
        /// the region, which might result in a larger distance in one dimension than indicated by
        /// the given dimensions.
        /// </remarks>
        public static SurfaceRegion FromBounds(Planetoid planet, double northLatitude, double westLongitude, double southLatitude, double eastLongitude)
        {
            var latitudeRange = Math.Abs(southLatitude - northLatitude);
            var centerLat = northLatitude + (latitudeRange / 2);
            var position = planet.LatitudeAndLongitudeToVector(
                centerLat,
                westLongitude + (Math.Abs(eastLongitude - westLongitude) / 2));

            var longitudeRange = Math.Abs(eastLongitude - westLongitude);
            latitudeRange = Math.Max(
                latitudeRange,
                longitudeRange / 2);
            var equalAreaAspectRatio = Math.PI * Math.Cos(centerLat).Square();
            latitudeRange = Math.Max(
                latitudeRange,
                longitudeRange / equalAreaAspectRatio);
            latitudeRange = Math.Min(
                latitudeRange,
                Math.PI);

            return new SurfaceRegion(
                planet,
                new Vector3(
                    position.X,
                    position.Y,
                    position.Z),
                latitudeRange);
        }

        /// <summary>
        /// Gets the boundaries of this region, as the latitude and longitude of the northwest
        /// corner and southeast corner.
        /// </summary>
        /// <param name="planet">The planet on which this region occurs.</param>
        /// <returns>
        /// The latitude and longitude of the northwest corner and southeast corner.
        /// </returns>
        public (
            double northLatitude,
            double westLongitude,
            double southLatitude,
            double eastLongitude) GetBounds(Planetoid planet)
        {
            var lat = planet.VectorToLongitude(Position);
            var lon = planet.VectorToLatitude(Position);
            var range = (double)((Frustum)Shape).FieldOfViewAngle;
            if (range >= Math.PI || range <= 0)
            {
                return (
                    -MathAndScience.Constants.Doubles.MathConstants.HalfPI,
                    -Math.PI,
                    MathAndScience.Constants.Doubles.MathConstants.HalfPI,
                    Math.PI);
            }
            range /= 2;
            var minLat = LatitudeBounded(lat - range);
            var maxLat = LatitudeBounded(lat + range);
            var minLon = LongitudeBounded(lon - range);
            var maxLon = LongitudeBounded(lon + range);
            if (minLat > maxLat)
            {
                var tmp = minLat;
                minLat = maxLat;
                maxLat = tmp;
            }
            if (minLon > maxLon)
            {
                var tmp = minLon;
                minLon = maxLon;
                maxLon = tmp;
            }
            return (
                minLat,
                minLon,
                maxLat,
                maxLon);
        }

        /// <summary>Populates a <see cref="SerializationInfo"></see> with the data needed to
        /// serialize the target object.</summary>
        /// <param name="info">The <see cref="SerializationInfo"></see> to populate with
        /// data.</param>
        /// <param name="context">The destination (see <see cref="StreamingContext"></see>) for this
        /// serialization.</param>
        /// <exception cref="System.Security.SecurityException">The caller does not have the
        /// required permission.</exception>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(Id), Id);
            info.AddValue(nameof(Shape), Shape);
            info.AddValue(nameof(ParentId), ParentId);
            info.AddValue(nameof(AbsolutePosition), AbsolutePosition);
        }

        /// <summary>
        /// Determines whether the given latitude and longitude are within this region.
        /// </summary>
        /// <param name="planet">The planet on which this region occurs.</param>
        /// <param name="latitude">A latitude, in radians.</param>
        /// <param name="longitude">A longitude, in radians.</param>
        /// <returns>
        /// <see langword="true"/> if the given position is within this region; otherwise <see
        /// langword="false"/>.
        /// </returns>
        public bool IsPositionWithin(Planetoid planet, double latitude, double longitude)
        {
            var (northLatitude, westLongitude, southLatitude, eastLongitude) = GetBounds(planet);
            return latitude >= southLatitude
                && latitude <= northLatitude
                && longitude >= westLongitude
                && longitude <= eastLongitude;
        }

        private static double LatitudeBounded(double value)
        {
            if (value < -MathAndScience.Constants.Doubles.MathConstants.HalfPI)
            {
                value = -Math.PI - value;
            }
            if (value > MathAndScience.Constants.Doubles.MathConstants.HalfPI)
            {
                value = Math.PI - value;
            }
            return value;
        }

        private static double LongitudeBounded(double value)
        {
            if (value < -Math.PI)
            {
                value = -MathAndScience.Constants.Doubles.MathConstants.TwoPI - value;
            }
            if (value > Math.PI)
            {
                value = MathAndScience.Constants.Doubles.MathConstants.TwoPI - value;
            }
            return value;
        }
    }
}
