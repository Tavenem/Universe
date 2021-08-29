using System.Text.Json.Serialization;
using Tavenem.Universe.Space;

namespace Tavenem.Universe.Place;

/// <summary>
/// A <see cref="Location"/> on the surface of a <see cref="Planetoid"/>, which can override
/// local conditions with manually-specified maps.
/// </summary>
[JsonConverter(typeof(SurfaceRegionConverter))]
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
    public Vector3<HugeNumber> PlanetaryPosition => ((Frustum<HugeNumber>)Shape).Axis.Normalize();

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
    public SurfaceRegion(Planetoid planet, Vector3<HugeNumber> position, HugeNumber latitudeRange)
        : base(planet.Id, new Frustum<HugeNumber>(2, position * (planet.Shape.ContainingRadius + planet.Atmosphere.AtmosphericHeight), HugeNumber.Min(latitudeRange, HugeNumber.Pi), 0)) { }

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
    public SurfaceRegion(Planetoid planet, double latitude, double longitude, HugeNumber latitudeRange)
        : base(planet.Id, new Frustum<HugeNumber>(2, planet.LatitudeAndLongitudeToVector(latitude, longitude) * (planet.Shape.ContainingRadius + planet.Atmosphere.AtmosphericHeight), HugeNumber.Min(latitudeRange, HugeNumber.Pi), 0)) { }

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
        IShape<HugeNumber> shape,
        string? parentId,
        Vector3<HugeNumber>[]? absolutePosition = null) : base(id, shape, parentId, absolutePosition)
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
        var longitudeRange = Math.Abs(eastLongitude - westLongitude);
        var halfLongitudeRange = longitudeRange / 2;

        var position = planet.LatitudeAndLongitudeToVector(
            centerLat,
            westLongitude + halfLongitudeRange);

        latitudeRange = Math.Max(
            latitudeRange,
            halfLongitudeRange);
        var equalAreaAspectRatio = Math.PI * Math.Cos(centerLat).Square();
        latitudeRange = Math.Max(
            latitudeRange,
            longitudeRange / equalAreaAspectRatio);
        latitudeRange = Math.Min(
            latitudeRange,
            Math.PI);

        return new SurfaceRegion(
            planet,
            position,
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
        var lat = planet.VectorToLatitude(PlanetaryPosition);
        var lon = planet.VectorToLongitude(PlanetaryPosition);
        var range = (double)((Frustum<HugeNumber>)Shape).FieldOfViewAngle;
        if (range is >= Math.PI or <= 0)
        {
            return (
                -DoubleConstants.HalfPi,
                -Math.PI,
                DoubleConstants.HalfPi,
                Math.PI);
        }
        var halfLatRange = range / 2;
        var equalAreaAspectRatio = Math.PI * Math.Cos(lat).Square();
        var halfLonRange = Math.Min(
            DoubleConstants.HalfPi,
            Math.Max(range, range * equalAreaAspectRatio / 2));
        var minLat = LatitudeBounded(lat - halfLatRange);
        var maxLat = LatitudeBounded(lat + halfLatRange);
        var minLon = LongitudeBounded(lon - halfLonRange);
        var maxLon = LongitudeBounded(lon + halfLonRange);
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
        if (value < -DoubleConstants.HalfPi)
        {
            value = -Math.PI - value;
        }
        if (value > DoubleConstants.HalfPi)
        {
            value = Math.PI - value;
        }
        return value;
    }

    private static double LongitudeBounded(double value)
    {
        if (value < -Math.PI)
        {
            value = -DoubleConstants.TwoPi - value;
        }
        if (value > Math.PI)
        {
            value = DoubleConstants.TwoPi - value;
        }
        return value;
    }
}
