using NeverFoundry.MathAndScience;
using NeverFoundry.MathAndScience.Constants.Numbers;
using NeverFoundry.MathAndScience.Numerics.Numbers;
using NeverFoundry.WorldFoundry.Planet.Climate;
using NeverFoundry.WorldFoundry.Planet.SurfaceMapping;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Number = NeverFoundry.MathAndScience.Numerics.Number;

namespace NeverFoundry.WorldFoundry.Planet
{
    /// <summary>
    /// A <see cref="Location"/> on the surface of a <see cref="Planet"/>, which can override
    /// local conditions with manually-specified maps.
    /// </summary>
    [Serializable]
    [System.Text.Json.Serialization.JsonConverter(typeof(SurfaceRegionConverter))]
    public class SurfaceRegion : Location, IDisposable
    {
        private bool _disposedValue;

        internal Image? _elevationMap;
        internal string? _elevationMapPath;

        internal Image?[]? _precipitationMaps;
        internal string?[]? _precipitationMapPaths;

        internal Image?[]? _snowfallMaps;
        internal string?[]? _snowfallMapPaths;

        internal Image? _temperatureMapSummer;
        internal string? _temperatureMapSummerPath;

        internal Image? _temperatureMapWinter;
        internal string? _temperatureMapWinterPath;

        /// <summary>
        /// Whether any custom precipitation maps, snowfall maps, and temperature maps for both
        /// summer and winter have been supplied for this region.
        /// </summary>
        public bool HasAllWeatherMaps
            => _precipitationMaps != null
            && _snowfallMaps != null
            && _temperatureMapSummer != null
            && _temperatureMapWinter != null;

        /// <summary>
        /// Whether any custom precipitation maps, snowfall maps, or temperature maps for either
        /// summer and winter have been supplied for this region.
        /// </summary>
        public bool HasAnyWeatherMaps
            => _precipitationMaps != null
            || _snowfallMaps != null
            || _temperatureMapSummer != null
            || _temperatureMapWinter != null;

        /// <summary>
        /// Whether a custom elevation map has been supplied for this region.
        /// </summary>
        public bool HasElevationMap => _elevationMap != null;

        /// <summary>
        /// Whether any custom precipitation maps have been supplied for this region.
        /// </summary>
        public bool HasPrecipitationMap => _precipitationMaps != null;

        /// <summary>
        /// Whether any custom snowfall maps have been supplied for this region.
        /// </summary>
        public bool HasSnowfallMap => _snowfallMaps != null;

        /// <summary>
        /// Whether a custom summer temperature map has been supplied for this region.
        /// </summary>
        public bool HasSummerTemperatureMap => _temperatureMapSummer != null;

        /// <summary>
        /// Whether any custom temperature maps have been supplied for this region.
        /// </summary>
        public bool HasTemperatureMap => _temperatureMapSummer != null || _temperatureMapWinter != null;

        /// <summary>
        /// Whether a custom winter temperature map has been supplied for this region.
        /// </summary>
        public bool HasWinterTemperatureMap => _temperatureMapWinter != null;

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
        public SurfaceRegion(Planet planet, Vector3 position, Number latitudeRange)
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
        public SurfaceRegion(Planet planet, double latitude, double longitude, Number latitudeRange)
            : base(planet.Id, new Frustum(2, planet.LatitudeAndLongitudeToVector(latitude, longitude) * (planet.Shape.ContainingRadius + planet.Atmosphere.AtmosphericHeight), Number.Min(latitudeRange, MathConstants.PI), 0)) { }

        /// <summary>
        /// Initializes a new instance of <see cref="SurfaceRegion"/>.
        /// </summary>
        /// <param name="id">The unique ID of this item.</param>
        /// <param name="idItemTypeName">The type discriminator.</param>
        /// <param name="shape">The shape of the location.</param>
        /// <param name="parentId">The ID of the location which contains this one.</param>
        /// <param name="elevationMapPath">
        /// A path to an image representing an elevation map.
        /// </param>
        /// <param name="precipitationMapPaths">
        /// An array of paths to a set of images representing precipitation maps.
        /// </param>
        /// <param name="snowfallMapPaths">
        /// An array of paths to a set of images representing a snowfall map.
        /// </param>
        /// <param name="temperatureMapSummerPath">
        /// A path to an image representing a summer temperature map.
        /// </param>
        /// <param name="temperatureMapWinterPath">
        /// A path to an image representing a winter temperature map.
        /// </param>
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
#pragma warning disable IDE0060 // Remove unused parameter; needed for serialization
            string idItemTypeName,
#pragma warning restore IDE0060 // Remove unused parameter
            IShape shape,
            string? parentId,
            string? elevationMapPath,
            string?[]? precipitationMapPaths,
            string?[]? snowfallMapPaths,
            string? temperatureMapSummerPath,
            string? temperatureMapWinterPath,
            Vector3[]? absolutePosition = null) : base(id, shape, parentId, absolutePosition)
        {
            _elevationMapPath = elevationMapPath;
            _precipitationMapPaths = precipitationMapPaths;
            _snowfallMapPaths = snowfallMapPaths;
            _temperatureMapSummerPath = temperatureMapSummerPath;
            _temperatureMapWinterPath = temperatureMapWinterPath;
        }

        private SurfaceRegion(SerializationInfo info, StreamingContext context) : this(
            (string?)info.GetValue(nameof(Id), typeof(string)) ?? string.Empty,
            SurfaceRegionIdItemTypeName,
            (IShape?)info.GetValue(nameof(Shape), typeof(IShape)) ?? SinglePoint.Origin,
            (string?)info.GetValue(nameof(ParentId), typeof(string)) ?? string.Empty,
            (string?)info.GetValue(nameof(_elevationMapPath), typeof(string)),
            (string?[]?)info.GetValue(nameof(_precipitationMapPaths), typeof(string?[])),
            (string?[]?)info.GetValue(nameof(_snowfallMapPaths), typeof(string?[])),
            (string?)info.GetValue(nameof(_temperatureMapSummerPath), typeof(string)),
            (string?)info.GetValue(nameof(_temperatureMapWinterPath), typeof(string)),
            (Vector3[]?)info.GetValue(nameof(AbsolutePosition), typeof(Vector3[])))
        { }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting
        /// unmanaged resources.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _elevationMap?.Dispose();
                    _temperatureMapSummer?.Dispose();
                    _temperatureMapWinter?.Dispose();

                    if (_precipitationMaps is not null)
                    {
                        for (var i = 0; i < _precipitationMaps.Length; i++)
                        {
                            _precipitationMaps[i]?.Dispose();
                        }
                    }

                    if (_snowfallMaps is not null)
                    {
                        for (var i = 0; i < _snowfallMaps.Length; i++)
                        {
                            _snowfallMaps[i]?.Dispose();
                        }
                    }
                }

                _disposedValue = true;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting
        /// unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

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
        public static SurfaceRegion FromBounds(Planet planet, double northLatitude, double westLongitude, double southLatitude, double eastLongitude)
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
        /// Stores an image as the elevation map for this region.
        /// </summary>
        /// <param name="image">The image to load.</param>
        /// <param name="mapLoader">
        /// <para>
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used to store the
        /// image.
        /// </para>
        /// <para>
        /// If <see langword="null"/> the image will not be stored, and will only be available while
        /// this region persists in memory.
        /// </para>
        /// </param>
        public async Task AssignElevationMapAsync(Image? image, ISurfaceMapLoader? mapLoader = null)
        {
            if (image is null)
            {
                _elevationMap = null;
                _elevationMapPath = null;
                return;
            }
            if (mapLoader is null)
            {
                _elevationMap = image;
                _elevationMapPath = null;
            }
            else
            {
                _elevationMapPath = await mapLoader.SaveAsync(image, Id, "elevation").ConfigureAwait(false);
                if (!string.IsNullOrEmpty(_elevationMapPath))
                {
                    _elevationMap = image;
                }
            }
        }

        /// <summary>
        /// Stores an image as a precipitation map for this region, adding it to any existing
        /// collection.
        /// </summary>
        /// <param name="image">The image to load.</param>
        /// <param name="mapLoader">
        /// <para>
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used to store the
        /// image.
        /// </para>
        /// <para>
        /// If <see langword="null"/> the image will not be stored, and will only be available while
        /// this region persists in memory.
        /// </para>
        /// </param>
        public async Task AssignPrecipitationMapAsync(Image? image, ISurfaceMapLoader? mapLoader = null)
        {
            if (image is null)
            {
                _precipitationMaps = null;
                _precipitationMapPaths = null;
                return;
            }

            if (mapLoader is not null)
            {
                var path = await mapLoader.SaveAsync(image, Id, $"precipitation_{(_precipitationMaps?.Length ?? -1) + 1}").ConfigureAwait(false);
                if (string.IsNullOrEmpty(path))
                {
                    return;
                }
                else if (_precipitationMapPaths is null)
                {
                    _precipitationMapPaths = new string?[] { path };
                }
                else
                {
                    var old = _precipitationMapPaths;
                    _precipitationMapPaths = new string?[old.Length + 1];
                    Array.Copy(old, _precipitationMapPaths, old.Length);
                    _precipitationMapPaths[old.Length] = path;
                }
            }

            if (_precipitationMaps is null)
            {
                _precipitationMaps = new Image?[] { image };
            }
            else
            {
                var old = _precipitationMaps;
                _precipitationMaps = new Image?[old.Length + 1];
                Array.Copy(old, _precipitationMaps, old.Length);
                _precipitationMaps[old.Length] = image;
            }
        }

        /// <summary>
        /// Stores a set of images as the precipitation maps for this region.
        /// </summary>
        /// <param name="images">
        /// The images to load. The set is presumed to be evenly spaced over the course of a year.
        /// </param>
        /// <param name="mapLoader">
        /// <para>
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used to store the
        /// image.
        /// </para>
        /// <para>
        /// If <see langword="null"/> the image will not be stored, and will only be available while
        /// this region persists in memory.
        /// </para>
        /// </param>
        public async Task AssignPrecipitationMapsAsync(IEnumerable<Image> images, ISurfaceMapLoader? mapLoader = null)
        {
            var list = images.ToList();
            if (list.Count == 0)
            {
                _precipitationMaps = null;
                _precipitationMapPaths = null;
                return;
            }
            _precipitationMaps = new Image?[list.Count];
            if (mapLoader is null)
            {
                _precipitationMapPaths = null;
            }
            else
            {
                _precipitationMapPaths = new string?[list.Count];
            }
            for (var i = 0; i < list.Count; i++)
            {
                if (mapLoader is null)
                {
                    _precipitationMaps[i] = list[i];
                }
                else
                {
                    var path = await mapLoader.SaveAsync(list[i], Id, $"precipitation_{i}").ConfigureAwait(false);
                    if (!string.IsNullOrEmpty(path))
                    {
                        _precipitationMaps[i] = list[i];
                        _precipitationMapPaths![i] = path;
                    }
                }
            }
        }

        /// <summary>
        /// Stores an image as a snowfall map for this region, adding it to any existing
        /// collection.
        /// </summary>
        /// <param name="image">The image to load.</param>
        /// <param name="mapLoader">
        /// <para>
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used to store the
        /// image.
        /// </para>
        /// <para>
        /// If <see langword="null"/> the image will not be stored, and will only be available while
        /// this region persists in memory.
        /// </para>
        /// </param>
        public async Task AssignSnowfallMapAsync(Image? image, ISurfaceMapLoader? mapLoader = null)
        {
            if (image is null)
            {
                _snowfallMaps = null;
                _snowfallMapPaths = null;
                return;
            }

            if (mapLoader is not null)
            {
                var path = await mapLoader.SaveAsync(image, Id, $"snowfall_{(_snowfallMaps?.Length ?? -1) + 1}").ConfigureAwait(false);
                if (string.IsNullOrEmpty(path))
                {
                    return;
                }
                else if (_snowfallMapPaths is null)
                {
                    _snowfallMapPaths = new string?[] { path };
                }
                else
                {
                    var old = _snowfallMapPaths;
                    _snowfallMapPaths = new string?[old.Length + 1];
                    Array.Copy(old, _snowfallMapPaths, old.Length);
                    _snowfallMapPaths[old.Length] = path;
                }
            }

            if (_snowfallMaps is null)
            {
                _snowfallMaps = new Image?[] { image };
            }
            else
            {
                var old = _snowfallMaps;
                _snowfallMaps = new Image?[old.Length + 1];
                Array.Copy(old, _snowfallMaps, old.Length);
                _snowfallMaps[old.Length] = image;
            }
        }

        /// <summary>
        /// Stores a set of images as the snowfall maps for this region.
        /// </summary>
        /// <param name="images">
        /// The images to load. The set is presumed to be evenly spaced over the course of a year.
        /// </param>
        /// <param name="mapLoader">
        /// <para>
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used to store the
        /// image.
        /// </para>
        /// <para>
        /// If <see langword="null"/> the image will not be stored, and will only be available while
        /// this region persists in memory.
        /// </para>
        /// </param>
        public async Task AssignSnowfallMapsAsync(IEnumerable<Image> images, ISurfaceMapLoader? mapLoader = null)
        {
            var list = images.ToList();
            if (list.Count == 0)
            {
                _snowfallMaps = null;
                _snowfallMapPaths = null;
                return;
            }
            _snowfallMaps = new Image?[list.Count];
            if (mapLoader is null)
            {
                _snowfallMapPaths = null;
            }
            else
            {
                _snowfallMapPaths = new string?[list.Count];
            }
            for (var i = 0; i < list.Count; i++)
            {
                if (mapLoader is null)
                {
                    _snowfallMaps[i] = list[i];
                }
                else
                {
                    var path = await mapLoader.SaveAsync(list[i], Id, $"snowfall_{i}").ConfigureAwait(false);
                    if (!string.IsNullOrEmpty(path))
                    {
                        _snowfallMaps[i] = list[i];
                        _snowfallMapPaths![i] = path;
                    }
                }
            }
        }

        /// <summary>
        /// Stores an image as the temperature map for this region, applying the same map to both
        /// summer and winter.
        /// </summary>
        /// <param name="image">The image to load.</param>
        /// <param name="mapLoader">
        /// <para>
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used to store the
        /// image.
        /// </para>
        /// <para>
        /// If <see langword="null"/> the image will not be stored, and will only be available while
        /// this region persists in memory.
        /// </para>
        /// </param>
        public async Task AssignTemperatureMapAsync(Image? image, ISurfaceMapLoader? mapLoader = null)
        {
            if (image is null)
            {
                _temperatureMapSummer = null;
                _temperatureMapSummerPath = null;
                _temperatureMapWinter = null;
                _temperatureMapWinterPath = null;
                return;
            }

            if (mapLoader is null)
            {
                _temperatureMapSummer = image;
                _temperatureMapSummerPath = null;
                _temperatureMapWinter = _temperatureMapSummer;
                _temperatureMapWinterPath = null;
            }
            else
            {
                _temperatureMapSummerPath = await mapLoader.SaveAsync(image, Id, "temperature_summer").ConfigureAwait(false);
                if (!string.IsNullOrEmpty(_temperatureMapSummerPath))
                {
                    _temperatureMapSummer = image;
                    _temperatureMapWinter = _temperatureMapSummer;
                    _temperatureMapWinterPath = _temperatureMapSummerPath;
                }
            }
        }

        /// <summary>
        /// Stores an image as the temperature map for this region at the summer solstice.
        /// </summary>
        /// <param name="image">The image to load.</param>
        /// <param name="mapLoader">
        /// <para>
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used to store the
        /// image.
        /// </para>
        /// <para>
        /// If <see langword="null"/> the image will not be stored, and will only be available while
        /// this region persists in memory.
        /// </para>
        /// </param>
        public async Task AssignTemperatureMapSummerAsync(Image? image, ISurfaceMapLoader? mapLoader = null)
        {
            if (image is null)
            {
                _temperatureMapSummer = null;
                _temperatureMapSummerPath = null;
                return;
            }

            if (mapLoader is null)
            {
                _temperatureMapSummer = image;
                _temperatureMapSummerPath = null;
            }
            else
            {
                _temperatureMapSummerPath = await mapLoader.SaveAsync(image, Id, "temperature_summer").ConfigureAwait(false);
                if (!string.IsNullOrEmpty(_temperatureMapSummerPath))
                {
                    _temperatureMapSummer = image;
                }
            }
        }

        /// <summary>
        /// Stores an image as the temperature map for this region at the winter solstice.
        /// </summary>
        /// <param name="image">The image to load.</param>
        /// <param name="mapLoader">
        /// <para>
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used to store the
        /// image.
        /// </para>
        /// <para>
        /// If <see langword="null"/> the image will not be stored, and will only be available while
        /// this region persists in memory.
        /// </para>
        /// </param>
        public async Task AssignTemperatureMapWinterAsync(Image? image, ISurfaceMapLoader? mapLoader = null)
        {
            if (image is null)
            {
                _temperatureMapWinter = null;
                _temperatureMapWinterPath = null;
                return;
            }

            if (mapLoader is null)
            {
                _temperatureMapWinter = image;
                _temperatureMapWinterPath = null;
            }
            else
            {
                _temperatureMapWinterPath = await mapLoader.SaveAsync(image, Id, "temperature_winter").ConfigureAwait(false);
                if (!string.IsNullOrEmpty(_temperatureMapWinterPath))
                {
                    _temperatureMapWinter = image;
                }
            }
        }

        /// <summary>
        /// Clears the elevation map for this region, and removes it from storage.
        /// </summary>
        /// <param name="mapLoader">
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used to remove the
        /// image.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the image was removed; or <see langword="false"/> if the
        /// operation fails.
        /// </returns>
        public async Task<bool> ClearElevationMapAsync(ISurfaceMapLoader mapLoader)
        {
            if (string.IsNullOrEmpty(_elevationMapPath))
            {
                return true;
            }
            var success = await mapLoader.RemoveAsync(_elevationMapPath).ConfigureAwait(false);
            if (success)
            {
                _elevationMapPath = null;
            }
            return success;
        }

        /// <summary>
        /// Clears the set of precipitation map images for this region, and removes them from
        /// storage.
        /// </summary>
        /// <param name="mapLoader">
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used to retrieve the
        /// image.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the images were removed; or <see langword="false"/> if the
        /// operation fails for any image.
        /// </returns>
        public async Task<bool> ClearPrecipitationMapsAsync(ISurfaceMapLoader mapLoader)
        {
            if (_precipitationMapPaths is null)
            {
                return true;
            }
            var success = true;
            for (var i = 0; i < _precipitationMapPaths.Length; i++)
            {
                if (string.IsNullOrEmpty(_precipitationMapPaths[i]))
                {
                    continue;
                }
                var mapSuccess = await mapLoader.RemoveAsync(_precipitationMapPaths[i]).ConfigureAwait(false);
                if (mapSuccess)
                {
                    _precipitationMapPaths[i] = null;
                }
                success &= mapSuccess;
            }
            if (success)
            {
                _precipitationMapPaths = null;
            }
            return success;
        }

        /// <summary>
        /// Clears the set of images as the snowfall maps for this region, and removes them from
        /// storage.
        /// </summary>
        /// <param name="mapLoader">
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used to retrieve the
        /// image.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the images were removed; or <see langword="false"/> if the
        /// operation fails for any image.
        /// </returns>
        public async Task<bool> ClearSnowfallMapsAsync(ISurfaceMapLoader mapLoader)
        {
            if (_snowfallMapPaths is null)
            {
                return true;
            }
            var success = true;
            for (var i = 0; i < _snowfallMapPaths.Length; i++)
            {
                if (string.IsNullOrEmpty(_snowfallMapPaths[i]))
                {
                    continue;
                }
                var mapSuccess = await mapLoader.RemoveAsync(_snowfallMapPaths[i]).ConfigureAwait(false);
                if (mapSuccess)
                {
                    _snowfallMapPaths[i] = null;
                }
                success &= mapSuccess;
            }
            if (success)
            {
                _snowfallMapPaths = null;
            }
            return success;
        }

        /// <summary>
        /// Clears the temperature map(s) for this region, and removes them from storage.
        /// </summary>
        /// <param name="mapLoader">
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used to retrieve the
        /// image.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the image(s) were removed; or <see langword="false"/> if the
        /// operation fails for any image.
        /// </returns>
        public async Task<bool> ClearTemperatureMapAsync(ISurfaceMapLoader mapLoader)
        {
            var success = true;
            if (!string.IsNullOrEmpty(_temperatureMapSummerPath))
            {
                success = await mapLoader.RemoveAsync(_temperatureMapSummerPath).ConfigureAwait(false);
                if (success)
                {
                    _temperatureMapSummerPath = null;
                }
            }

            if (!string.IsNullOrEmpty(_temperatureMapWinterPath))
            {
                var mapSuccess = await mapLoader.RemoveAsync(_temperatureMapWinterPath).ConfigureAwait(false);
                if (mapSuccess)
                {
                    _temperatureMapWinterPath = null;
                }
                success &= mapSuccess;
            }
            return success;
        }

        /// <summary>
        /// Gets the boundaries of this region, as the latitude and longitude of the northwest
        /// corner and southeast corner.
        /// </summary>
        /// <param name="planet">The planet on which this region occurs.</param>
        /// <returns>The latitude and longitude of the northwest corner and southeast
        /// corner</returns>
        public (
            double northLatitude,
            double westLongitude,
            double southLatitude,
            double eastLongitude) GetBounds(Planet planet)
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
            return (
                LatitudeBounded(lat - range),
                LongitudeBounded(lon - range),
                LatitudeBounded(lat + range),
                LongitudeBounded(lon + range));
        }

        /// <summary>
        /// Gets the stored elevation map image for this region, if any.
        /// </summary>
        /// <returns>The stored elevation map image for this region, if any.</returns>
        public Image? GetElevationMap() => _elevationMap;

        /// <summary>
        /// Produces an elevation map projection of this region, taking into account any overlay.
        /// </summary>
        /// <param name="planet">The planet being mapped.</param>
        /// <param name="resolution">The vertical resolution of the projection.</param>
        /// <param name="equalArea">
        /// If <see langword="true"/> the projection will be a cylindrical equal-area projection.
        /// Otherwise, an equirectangular projection will be used.
        /// </param>
        /// <param name="mapLoader">
        /// <para>
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used.
        /// </para>
        /// <para>
        /// If <see langword="null"/> no stored map will be used, and any generated map will not be
        /// saved.
        /// </para>
        /// </param>
        /// <returns>
        /// A projected map of elevation. Pixel luminosity indicates elevation relative to <see
        /// cref="Planet.MaxElevation"/>, with values below the midpoint indicating elevations
        /// below the mean surface.
        /// </returns>
        public async Task<Image<L16>> GetElevationMapAsync(
            Planet planet,
            int resolution,
            bool equalArea = false,
            ISurfaceMapLoader? mapLoader = null)
        {
            if (_elevationMap is null && HasElevationMap && mapLoader is not null)
            {
                await LoadElevationMapAsync(mapLoader).ConfigureAwait(false);
            }
            var options = GetProjection(planet, equalArea);
            if (_elevationMap is null)
            {
                _elevationMap = await planet.GetElevationMapProjectionAsync(
                    resolution,
                    options.With(equalArea: false),
                    mapLoader)
                    .ConfigureAwait(false);
                if (mapLoader is not null)
                {
                    await AssignElevationMapAsync(_elevationMap, mapLoader).ConfigureAwait(false);
                }
            }
            return SurfaceMapImage.GetImageAtResolution(
                _elevationMap,
                resolution,
                options);
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
            info.AddValue(nameof(_elevationMapPath), _elevationMapPath);
            info.AddValue(nameof(_precipitationMapPaths), _precipitationMapPaths);
            info.AddValue(nameof(_snowfallMapPaths), _snowfallMapPaths);
            info.AddValue(nameof(_temperatureMapSummerPath), _temperatureMapSummerPath);
            info.AddValue(nameof(_temperatureMapWinterPath), _temperatureMapWinterPath);
            info.AddValue(nameof(AbsolutePosition), AbsolutePosition);
        }

        /// <summary>
        /// Produces a precipitation map projection of this region, taking into account any overlay.
        /// </summary>
        /// <param name="planet">The planet being mapped.</param>
        /// <param name="resolution">The vertical resolution of the projection.</param>
        /// <param name="steps">
        /// <para>
        /// The number of maps to generate internally (representing evenly spaced "seasons" during a
        /// year, starting and ending at the winter solstice in the northern hemisphere), before
        /// averaging them into a single image.
        /// </para>
        /// <para>
        /// If stored maps exist, they will be used and this parameter will be ignored.
        /// </para>
        /// </param>
        /// <param name="equalArea">
        /// If <see langword="true"/> the projection will be a cylindrical equal-area projection.
        /// Otherwise, an equirectangular projection will be used.
        /// </param>
        /// <param name="mapLoader">
        /// <para>
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used.
        /// </para>
        /// <para>
        /// If <see langword="null"/> no stored maps will be used, and any generated maps will not
        /// be saved.
        /// </para>
        /// </param>
        /// <returns>
        /// A projected map of precipitation. Pixel luminosity indicates precipitation in mm/hr,
        /// relative to the <see cref="Atmosphere.MaxPrecipitation"/> of the <paramref
        /// name="planet"/>'s <see cref="Atmosphere"/>.
        /// </returns>
        public async Task<Image<L16>> GetPrecipitationMapAsync(
            Planet planet,
            int resolution,
            int steps = 1,
            bool equalArea = false,
            ISurfaceMapLoader? mapLoader = null)
        {
            if ((_precipitationMaps is null || _precipitationMaps.Length == 0)
                && HasPrecipitationMap
                && mapLoader is not null)
            {
                await LoadPrecipitationMapsAsync(mapLoader).ConfigureAwait(false);
            }
            var options = GetProjection(planet, equalArea);
            if (_precipitationMaps is null
                || _precipitationMaps.Length == 0
                || _precipitationMaps.Any(x => x is null))
            {
                _precipitationMaps = await planet.GetPrecipitationMapsProjectionAsync(
                    resolution,
                    steps,
                    options.With(equalArea: false),
                    mapLoader)
                    .ConfigureAwait(false);
                if (mapLoader is not null)
                {
                    await AssignPrecipitationMapsAsync(_precipitationMaps!, mapLoader).ConfigureAwait(false);
                }
            }
            return SurfaceMapImage.GetImageAtResolution(
                SurfaceMapImage.AverageImages(_precipitationMaps!),
                resolution,
                options);
        }

        /// <summary>
        /// Produces a precipitation map projection of this region at the given proportion of a
        /// year, taking into account any overlay.
        /// </summary>
        /// <param name="planet">The planet being mapped.</param>
        /// <param name="resolution">The vertical resolution of the projection.</param>
        /// <param name="proportionOfYear">
        /// The proportion of a full year at which the map is to be generated, assuming a year
        /// begins and ends at the winter solstice in the northern hemisphere.
        /// </param>
        /// <param name="steps">
        /// <para>
        /// The number of maps to generate internally (representing evenly spaced "seasons" during a
        /// year, starting and ending at the winter solstice in the northern hemisphere), before
        /// interpolating them into a single image.
        /// </para>
        /// <para>
        /// If stored maps exist, they will be used and this parameter will be ignored.
        /// </para>
        /// </param>
        /// <param name="equalArea">
        /// If <see langword="true"/> the projection will be a cylindrical equal-area projection.
        /// Otherwise, an equirectangular projection will be used.
        /// </param>
        /// <param name="mapLoader">
        /// <para>
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used.
        /// </para>
        /// <para>
        /// If <see langword="null"/> no stored maps will be used, and any generated maps will not
        /// be saved.
        /// </para>
        /// </param>
        /// <returns>
        /// A projected map of precipitation at the given proportion of a year. Pixel luminosity
        /// indicates precipitation in mm/hr, relative to the <see
        /// cref="Atmosphere.MaxPrecipitation"/> of the <paramref name="planet"/>'s <see
        /// cref="Atmosphere"/>.
        /// </returns>
        public async Task<Image<L16>> GetPrecipitationMapAsync(
            Planet planet,
            int resolution,
            double proportionOfYear,
            int steps = 1,
            bool equalArea = false,
            ISurfaceMapLoader? mapLoader = null)
        {
            if ((_precipitationMaps is null || _precipitationMaps.Length == 0)
                && HasPrecipitationMap
                && mapLoader is not null)
            {
                await LoadPrecipitationMapsAsync(mapLoader).ConfigureAwait(false);
            }
            var options = GetProjection(planet, equalArea);
            if (_precipitationMaps is null
                || _precipitationMaps.Length == 0
                || _precipitationMaps.Any(x => x is null))
            {
                _precipitationMaps = await planet.GetPrecipitationMapsProjectionAsync(
                    resolution,
                    steps,
                    options.With(equalArea: false),
                    mapLoader)
                    .ConfigureAwait(false);
                if (mapLoader is not null)
                {
                    await AssignPrecipitationMapsAsync(_precipitationMaps!, mapLoader).ConfigureAwait(false);
                }
            }
            var proportionPerSeason = 1.0 / steps;
            var proportionPerMap = 1.0 / _precipitationMaps.Length;
            var season = (int)Math.Floor(proportionOfYear / proportionPerMap).Clamp(0, _precipitationMaps.Length - 1);
            var nextSeason = season == _precipitationMaps.Length - 1
                ? 0
                : season + 1;
            var weight = proportionOfYear % proportionPerMap;
            var map = weight.IsNearlyZero()
                ? _precipitationMaps[season]!
                : SurfaceMapImage.InterpolateImages(_precipitationMaps[season]!, _precipitationMaps[nextSeason]!, weight);
            return SurfaceMapImage.GetImageAtResolution(
                map,
                resolution,
                options);
        }

        /// <summary>
        /// Gets the stored set of precipitation map images for this region, if any.
        /// </summary>
        /// <returns>The stored set of precipitation map images for this region, if any.</returns>
        public Image?[] GetPrecipitationMaps()
        {
            if (_precipitationMaps is null)
            {
                return Array.Empty<Image?>();
            }
            return _precipitationMaps;
        }

        /// <summary>
        /// Produces a set of precipitation map projections of this region, taking into account any
        /// overlay.
        /// </summary>
        /// <param name="planet">The planet being mapped.</param>
        /// <param name="resolution">The vertical resolution of the projection.</param>
        /// <param name="steps">
        /// <para>
        /// The number of maps to generate (representing evenly spaced "seasons" during a year,
        /// starting and ending at the winter solstice in the northern hemisphere).
        /// </para>
        /// <para>
        /// If stored maps exist but in a different number, they will be interpolated.
        /// </para>
        /// </param>
        /// <param name="equalArea">
        /// If <see langword="true"/> the projection will be a cylindrical equal-area projection.
        /// Otherwise, an equirectangular projection will be used.
        /// </param>
        /// <param name="mapLoader">
        /// <para>
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used.
        /// </para>
        /// <para>
        /// If <see langword="null"/> no stored maps will be used, and any generated maps will not
        /// be saved.
        /// </para>
        /// </param>
        /// <returns>
        /// A set of projected maps of precipitation. Pixel luminosity indicates precipitation in
        /// mm/hr, relative to the <see cref="Atmosphere.MaxPrecipitation"/> of the <paramref
        /// name="planet"/>'s <see cref="Atmosphere"/>.
        /// </returns>
        public async Task<Image<L16>[]> GetPrecipitationMapsAsync(
            Planet planet,
            int resolution,
            int steps,
            bool equalArea = false,
            ISurfaceMapLoader? mapLoader = null)
        {
            if ((_precipitationMaps is null || _precipitationMaps.Length == 0)
                && HasPrecipitationMap
                && mapLoader is not null)
            {
                await LoadPrecipitationMapsAsync(mapLoader).ConfigureAwait(false);
            }
            var options = GetProjection(planet, equalArea);
            if (_precipitationMaps is null
                || _precipitationMaps.Length == 0
                || _precipitationMaps.Any(x => x is null))
            {
                _precipitationMaps = await planet.GetPrecipitationMapsProjectionAsync(
                    resolution,
                    steps,
                    options.With(equalArea: false),
                    mapLoader)
                    .ConfigureAwait(false);
                if (mapLoader is not null)
                {
                    await AssignPrecipitationMapsAsync(_precipitationMaps!, mapLoader).ConfigureAwait(false);
                }
            }
            var maps = new Image<L16>[steps];
            if (_precipitationMaps.Length == steps)
            {
                for (var i = 0; i < steps; i++)
                {
                    maps[i] = SurfaceMapImage.GetImageAtResolution(
                        _precipitationMaps[i]!,
                        resolution,
                        options);
                }
                return maps;
            }
            var proportionOfYear = 0.0;
            var proportionPerSeason = 1.0 / steps;
            var proportionPerMap = 1.0 / _precipitationMaps.Length;
            for (var i = 0; i < steps; i++)
            {
                var season = (int)Math.Floor(proportionOfYear / proportionPerMap).Clamp(0, _precipitationMaps.Length - 1);
                var nextSeason = season == _precipitationMaps.Length - 1
                    ? 0
                    : season + 1;
                var weight = proportionOfYear % proportionPerMap;
                maps[i] = SurfaceMapImage.GetImageAtResolution(
                    weight.IsNearlyZero()
                        ? _precipitationMaps[season]!
                        : SurfaceMapImage.InterpolateImages(_precipitationMaps[season]!, _precipitationMaps[nextSeason]!, weight),
                    resolution,
                    options);
                proportionOfYear += proportionPerSeason;
            }
            return maps;
        }

        /// <summary>
        /// Gets the map projection opions which represent this region.
        /// </summary>
        /// <param name="planet">The planet on which this region occurs.</param>
        /// <param name="equalArea">Whether to generate cylindrical equal-area options.</param>
        /// <returns>A <see cref="MapProjectionOptions"/> instance.</returns>
        public MapProjectionOptions GetProjection(Planet planet, bool equalArea = false)
            => new(planet.VectorToLongitude(PlanetaryPosition),
                planet.VectorToLatitude(PlanetaryPosition),
                range: (double)((Frustum)Shape).FieldOfViewAngle,
                equalArea: equalArea);

        /// <summary>
        /// Produces a snowfall map projection of this region, taking into account any overlay.
        /// </summary>
        /// <param name="planet">The planet being mapped.</param>
        /// <param name="resolution">The vertical resolution of the projection.</param>
        /// <param name="steps">
        /// <para>
        /// The number of maps to generate internally (representing evenly spaced "seasons" during a
        /// year, starting and ending at the winter solstice in the northern hemisphere), before
        /// averaging them into a single image.
        /// </para>
        /// <para>
        /// If stored maps exist, they will be used and this parameter will be ignored.
        /// </para>
        /// </param>
        /// <param name="equalArea">
        /// If <see langword="true"/> the projection will be a cylindrical equal-area projection.
        /// Otherwise, an equirectangular projection will be used.
        /// </param>
        /// <param name="mapLoader">
        /// <para>
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used.
        /// </para>
        /// <para>
        /// If <see langword="null"/> no stored maps will be used, and any generated maps will not
        /// be saved.
        /// </para>
        /// </param>
        /// <returns>
        /// A projected map of snowfall. Pixel luminosity indicates precipitation in mm/hr, relative
        /// to the <see cref="Atmosphere.MaxSnowfall"/> of the <paramref name="planet"/>'s <see
        /// cref="Atmosphere"/>.
        /// </returns>
        public async Task<Image<L16>> GetSnowfallMapAsync(
            Planet planet,
            int resolution,
            int steps = 1,
            bool equalArea = false,
            ISurfaceMapLoader? mapLoader = null)
        {
            if ((_snowfallMaps is null || _snowfallMaps.Length == 0)
                && HasSnowfallMap
                && mapLoader is not null)
            {
                await LoadSnowfallMapsAsync(mapLoader).ConfigureAwait(false);
            }
            var options = GetProjection(planet, equalArea);
            if (_snowfallMaps is null
                || _snowfallMaps.Length == 0
                || _snowfallMaps.Any(x => x is null))
            {
                _snowfallMaps = await planet.GetSnowfallMapProjectionsAsync(
                    resolution,
                    steps,
                    options.With(equalArea: false),
                    mapLoader)
                    .ConfigureAwait(false);
                if (mapLoader is not null)
                {
                    await AssignSnowfallMapsAsync(_snowfallMaps!, mapLoader).ConfigureAwait(false);
                }
            }
            return SurfaceMapImage.GetImageAtResolution(
                SurfaceMapImage.AverageImages(_snowfallMaps!),
                resolution,
                options);
        }

        /// <summary>
        /// Produces a snowfall map projection of this region at the given proportion of a year,
        /// taking into account any overlay.
        /// </summary>
        /// <param name="planet">The planet being mapped.</param>
        /// <param name="resolution">The vertical resolution of the projection.</param>
        /// <param name="proportionOfYear">
        /// The proportion of a full year at which the map is to be generated, assuming a year
        /// begins and ends at the winter solstice in the northern hemisphere.
        /// </param>
        /// <param name="steps">
        /// <para>
        /// The number of maps to generate internally (representing evenly spaced "seasons" during a
        /// year, starting and ending at the winter solstice in the northern hemisphere), before
        /// interpolating them into a single image.
        /// </para>
        /// <para>
        /// If stored maps exist, they will be used and this parameter will be ignored.
        /// </para>
        /// </param>
        /// <param name="equalArea">
        /// If <see langword="true"/> the projection will be a cylindrical equal-area projection.
        /// Otherwise, an equirectangular projection will be used.
        /// </param>
        /// <param name="mapLoader">
        /// <para>
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used.
        /// </para>
        /// <para>
        /// If <see langword="null"/> no stored maps will be used, and any generated maps will not
        /// be saved.
        /// </para>
        /// </param>
        /// <returns>
        /// A projected map of snowfall at the given proportion of a year. Pixel luminosity
        /// indicates snowfall in mm/hr, relative to the <see cref="Atmosphere.MaxSnowfall"/>
        /// of the <paramref name="planet"/>'s <see cref="Atmosphere"/>.
        /// </returns>
        public async Task<Image<L16>> GetSnowfallMapAsync(
            Planet planet,
            int resolution,
            double proportionOfYear,
            int steps = 1,
            bool equalArea = false,
            ISurfaceMapLoader? mapLoader = null)
        {
            if ((_snowfallMaps is null || _snowfallMaps.Length == 0)
                && HasSnowfallMap
                && mapLoader is not null)
            {
                await LoadSnowfallMapsAsync(mapLoader).ConfigureAwait(false);
            }
            var options = GetProjection(planet, equalArea);
            if (_snowfallMaps is null
                || _snowfallMaps.Length == 0
                || _snowfallMaps.Any(x => x is null))
            {
                _snowfallMaps = await planet.GetSnowfallMapProjectionsAsync(
                    resolution,
                    steps,
                    options.With(equalArea: false),
                    mapLoader)
                    .ConfigureAwait(false);
                if (mapLoader is not null)
                {
                    await AssignSnowfallMapsAsync(_precipitationMaps!, mapLoader).ConfigureAwait(false);
                }
            }
            var proportionPerSeason = 1.0 / steps;
            var proportionPerMap = 1.0 / _snowfallMaps.Length;
            var season = (int)Math.Floor(proportionOfYear / proportionPerMap).Clamp(0, _snowfallMaps.Length - 1);
            var nextSeason = season == _snowfallMaps.Length - 1
                ? 0
                : season + 1;
            var weight = proportionOfYear % proportionPerMap;
            var map = weight.IsNearlyZero()
                ? _snowfallMaps[season]!
                : SurfaceMapImage.InterpolateImages(_snowfallMaps[season]!, _snowfallMaps[nextSeason]!, weight);
            return SurfaceMapImage.GetImageAtResolution(
                map,
                resolution,
                options);
        }

        /// <summary>
        /// Gets the stored set of snowfall map images for this region, if any.
        /// </summary>
        /// <returns>The stored set of snowfall map images for this region, if any.</returns>
        public Image?[] GetSnowfallMaps()
        {
            if (_snowfallMaps is null)
            {
                return Array.Empty<Image?>();
            }
            return _snowfallMaps;
        }

        /// <summary>
        /// Produces a set of snowfall map projections of this region, taking into account any
        /// overlay.
        /// </summary>
        /// <param name="planet">The planet being mapped.</param>
        /// <param name="resolution">The vertical resolution of the projection.</param>
        /// <param name="steps">
        /// <para>
        /// The number of maps to generate (representing evenly spaced "seasons" during a year,
        /// starting and ending at the winter solstice in the northern hemisphere).
        /// </para>
        /// <para>
        /// If stored maps exist but in a different number, they will be interpolated.
        /// </para>
        /// </param>
        /// <param name="equalArea">
        /// If <see langword="true"/> the projection will be a cylindrical equal-area projection.
        /// Otherwise, an equirectangular projection will be used.
        /// </param>
        /// <param name="mapLoader">
        /// <para>
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used.
        /// </para>
        /// <para>
        /// If <see langword="null"/> no stored maps will be used, and any generated maps will not
        /// be saved.
        /// </para>
        /// </param>
        /// <returns>
        /// A set of projected maps of snowfall. Pixel luminosity indicates precipitation in mm/hr,
        /// relative to the <see cref="Atmosphere.MaxSnowfall"/> of the <paramref name="planet"/>'s
        /// <see cref="Atmosphere"/>.
        /// </returns>
        public async Task<Image<L16>[]> GetSnowfallMapsAsync(
            Planet planet,
            int resolution,
            int steps,
            bool equalArea = false,
            ISurfaceMapLoader? mapLoader = null)
        {
            if ((_snowfallMaps is null || _snowfallMaps.Length == 0)
                && HasSnowfallMap
                && mapLoader is not null)
            {
                await LoadSnowfallMapsAsync(mapLoader).ConfigureAwait(false);
            }
            var options = GetProjection(planet, equalArea);
            if (_snowfallMaps is null
                || _snowfallMaps.Length == 0
                || _snowfallMaps.Any(x => x is null))
            {
                _snowfallMaps = await planet.GetSnowfallMapProjectionsAsync(
                    resolution,
                    steps,
                    options.With(equalArea: false),
                    mapLoader)
                    .ConfigureAwait(false);
                if (mapLoader is not null)
                {
                    await AssignSnowfallMapsAsync(_snowfallMaps!, mapLoader).ConfigureAwait(false);
                }
            }
            var maps = new Image<L16>[steps];
            if (_snowfallMaps.Length == steps)
            {
                for (var i = 0; i < steps; i++)
                {
                    maps[i] = SurfaceMapImage.GetImageAtResolution(
                        _snowfallMaps[i]!,
                        resolution,
                        options);
                }
                return maps;
            }
            var proportionOfYear = 0.0;
            var proportionPerSeason = 1.0 / steps;
            var proportionPerMap = 1.0 / _snowfallMaps.Length;
            for (var i = 0; i < steps; i++)
            {
                var season = (int)Math.Floor(proportionOfYear / proportionPerMap).Clamp(0, _snowfallMaps.Length - 1);
                var nextSeason = season == _snowfallMaps.Length - 1
                    ? 0
                    : season + 1;
                var weight = proportionOfYear % proportionPerMap;
                maps[i] = SurfaceMapImage.GetImageAtResolution(
                    weight.IsNearlyZero()
                        ? _snowfallMaps[season]!
                        : SurfaceMapImage.InterpolateImages(_snowfallMaps[season]!, _snowfallMaps[nextSeason]!, weight),
                    resolution,
                    options);
                proportionOfYear += proportionPerSeason;
            }
            return maps;
        }

        /// <summary>
        /// Produces a temperature map projection of this region, taking into account any overlay.
        /// </summary>
        /// <param name="planet">The planet being mapped.</param>
        /// <param name="resolution">The vertical resolution of the projection.</param>
        /// <param name="equalArea">
        /// If <see langword="true"/> the projection will be a cylindrical equal-area projection.
        /// Otherwise, an equirectangular projection will be used.
        /// </param>
        /// <param name="mapLoader">
        /// <para>
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used.
        /// </para>
        /// <para>
        /// If <see langword="null"/> no stored map will be used, and any generated map will not be
        /// saved.
        /// </para>
        /// </param>
        /// <returns>
        /// A projected map of temperature. Pixel luminosity indicates temperature relative to 5000
        /// K.
        /// </returns>
        public async Task<Image<L16>> GetTemperatureMapAsync(
            Planet planet,
            int resolution,
            bool equalArea = false,
            ISurfaceMapLoader? mapLoader = null)
        {
            if ((_temperatureMapWinter is null || _temperatureMapSummer is null)
                && HasTemperatureMap
                && mapLoader is not null)
            {
                await LoadTemperatureMapAsync(mapLoader).ConfigureAwait(false);
            }
            var options = GetProjection(planet, equalArea);
            if (_temperatureMapWinter is null || _temperatureMapSummer is null)
            {
                _temperatureMapWinter = await planet.GetTemperatureMapProjectionWinterAsync(
                    resolution,
                    options.With(equalArea: false),
                    mapLoader)
                    .ConfigureAwait(false);
                if (mapLoader is not null)
                {
                    await AssignTemperatureMapWinterAsync(_temperatureMapWinter, mapLoader).ConfigureAwait(false);
                }
                _temperatureMapSummer = await planet.GetTemperatureMapProjectionSummerAsync(
                    resolution,
                    options.With(equalArea: false),
                    mapLoader)
                    .ConfigureAwait(false);
                if (mapLoader is not null)
                {
                    await AssignTemperatureMapSummerAsync(_temperatureMapSummer, mapLoader).ConfigureAwait(false);
                }
            }
            return SurfaceMapImage.GetImageAtResolution(
                SurfaceMapImage.GenerateTemperatureMap(_temperatureMapWinter, _temperatureMapSummer),
                resolution,
                options);
        }

        /// <summary>
        /// Produces a temperature map projection of this region at the given proportion of a year,
        /// taking into account any overlay.
        /// </summary>
        /// <param name="planet">The planet being mapped.</param>
        /// <param name="resolution">The vertical resolution of the projection.</param>
        /// <param name="proportionOfYear">
        /// The proportion of a full year at which the map is to be generated, assuming a year
        /// begins and ends at the winter solstice in the northern hemisphere.
        /// </param>
        /// <param name="equalArea">
        /// If <see langword="true"/> the projection will be a cylindrical equal-area projection.
        /// Otherwise, an equirectangular projection will be used.
        /// </param>
        /// <param name="mapLoader">
        /// <para>
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used.
        /// </para>
        /// <para>
        /// If <see langword="null"/> no stored map will be used, and any generated map will not be
        /// saved.
        /// </para>
        /// </param>
        /// <returns>
        /// A projected map of temperature at the given proportion of a year. Pixel luminosity
        /// indicates temperature relative to 5000 K.
        /// </returns>
        public async Task<Image<L16>> GetTemperatureMapAsync(
            Planet planet,
            int resolution,
            double proportionOfYear,
            bool equalArea = false,
            ISurfaceMapLoader? mapLoader = null)
        {
            if ((_temperatureMapWinter is null || _temperatureMapSummer is null)
                && HasTemperatureMap
                && mapLoader is not null)
            {
                await LoadTemperatureMapAsync(mapLoader).ConfigureAwait(false);
            }
            var options = GetProjection(planet, equalArea);
            if (_temperatureMapWinter is null || _temperatureMapSummer is null)
            {
                _temperatureMapWinter = await planet.GetTemperatureMapProjectionWinterAsync(
                    resolution,
                    options.With(equalArea: false),
                    mapLoader)
                    .ConfigureAwait(false);
                if (mapLoader is not null)
                {
                    await AssignTemperatureMapWinterAsync(_temperatureMapWinter, mapLoader).ConfigureAwait(false);
                }
                _temperatureMapSummer = await planet.GetTemperatureMapProjectionSummerAsync(
                    resolution,
                    options.With(equalArea: false),
                    mapLoader)
                    .ConfigureAwait(false);
                if (mapLoader is not null)
                {
                    await AssignTemperatureMapSummerAsync(_temperatureMapSummer, mapLoader).ConfigureAwait(false);
                }
            }
            return SurfaceMapImage.GetImageAtResolution(
                SurfaceMapImage.InterpolateImages(_temperatureMapWinter, _temperatureMapSummer, proportionOfYear),
                resolution,
                options);
        }

        /// <summary>
        /// Gets the stored temperature map image for this region at the summer solstice, if any.
        /// </summary>
        /// <returns>The stored temperature map image for this region at the summer solstice, if
        /// any.</returns>
        public Image? GetTemperatureMapSummer() => _temperatureMapSummer ?? _temperatureMapWinter;

        /// <summary>
        /// Produces a temperature map projection of this region at the summer solstice in the
        /// northern hemisphere, taking into account any overlay.
        /// </summary>
        /// <param name="planet">The planet being mapped.</param>
        /// <param name="resolution">The vertical resolution of the projection.</param>
        /// <param name="equalArea">
        /// If <see langword="true"/> the projection will be a cylindrical equal-area projection.
        /// Otherwise, an equirectangular projection will be used.
        /// </param>
        /// <param name="mapLoader">
        /// <para>
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used.
        /// </para>
        /// <para>
        /// If <see langword="null"/> no stored map will be used, and any generated map will not be
        /// saved.
        /// </para>
        /// </param>
        /// <returns>
        /// A projected map of temperature at the summer solstice in the northern hemisphere. Pixel
        /// luminosity indicates temperature relative to 5000 K.
        /// </returns>
        public async Task<Image<L16>> GetTemperatureMapSummerAsync(
            Planet planet,
            int resolution,
            bool equalArea = false,
            ISurfaceMapLoader? mapLoader = null)
        {
            if (_temperatureMapSummer is null && HasTemperatureMap && mapLoader is not null)
            {
                await LoadTemperatureMapAsync(mapLoader).ConfigureAwait(false);
            }
            var options = GetProjection(planet, equalArea);
            if (_temperatureMapSummer is null)
            {
                _temperatureMapSummer = await planet.GetTemperatureMapProjectionSummerAsync(
                    resolution,
                    options.With(equalArea: false),
                    mapLoader)
                    .ConfigureAwait(false);
                if (mapLoader is not null)
                {
                    await AssignTemperatureMapSummerAsync(_temperatureMapSummer, mapLoader).ConfigureAwait(false);
                }
            }
            return SurfaceMapImage.GetImageAtResolution(
                _temperatureMapSummer,
                resolution,
                options);
        }

        /// <summary>
        /// Gets the stored temperature map image for this region at the winter solstice, if any.
        /// </summary>
        /// <returns>The stored temperature map image for this region at the winter solstice, if
        /// any.</returns>
        public Image? GetTemperatureMapWinter() => _temperatureMapWinter ?? _temperatureMapSummer;

        /// <summary>
        /// Produces a temperature map projection of this region at the winter solstice in the
        /// northern hemisphere, taking into account any overlay.
        /// </summary>
        /// <param name="planet">The planet being mapped.</param>
        /// <param name="resolution">The vertical resolution of the projection.</param>
        /// <param name="equalArea">
        /// If <see langword="true"/> the projection will be a cylindrical equal-area projection.
        /// Otherwise, an equirectangular projection will be used.
        /// </param>
        /// <param name="mapLoader">
        /// <para>
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used.
        /// </para>
        /// <para>
        /// If <see langword="null"/> no stored map will be used, and any generated map will not be
        /// saved.
        /// </para>
        /// </param>
        /// <returns>
        /// A projected map of temperature at the winter solstice in the northern hemisphere. Pixel
        /// luminosity indicates temperature relative to 5000 K.
        /// </returns>
        public async Task<Image<L16>> GetTemperatureMapWinterAsync(
            Planet planet,
            int resolution,
            bool equalArea = false,
            ISurfaceMapLoader? mapLoader = null)
        {
            if (_temperatureMapWinter is null && HasTemperatureMap && mapLoader is not null)
            {
                await LoadTemperatureMapAsync(mapLoader).ConfigureAwait(false);
            }
            var options = GetProjection(planet, equalArea);
            if (_temperatureMapWinter is null)
            {
                _temperatureMapWinter = await planet.GetTemperatureMapProjectionWinterAsync(
                    resolution,
                    options.With(equalArea: false),
                    mapLoader)
                    .ConfigureAwait(false);
                if (mapLoader is not null)
                {
                    await AssignTemperatureMapWinterAsync(_temperatureMapWinter, mapLoader).ConfigureAwait(false);
                }
            }
            return SurfaceMapImage.GetImageAtResolution(
                _temperatureMapWinter,
                resolution,
                options);
        }

        /// <summary>
        /// Loads the elevation map for this region from storage.
        /// </summary>
        /// <param name="mapLoader">
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used to retrieve the
        /// image.
        /// </param>
        public async Task LoadElevationMapAsync(ISurfaceMapLoader mapLoader)
        {
            if (!string.IsNullOrEmpty(_elevationMapPath))
            {
                _elevationMap = await mapLoader.LoadAsync(_elevationMapPath).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Loads a set of images as the precipitation maps for this region from storage.
        /// </summary>
        /// <param name="mapLoader">
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used to retrieve the
        /// image.
        /// </param>
        public async Task LoadPrecipitationMapsAsync(ISurfaceMapLoader mapLoader)
        {
            if (_precipitationMapPaths is null)
            {
                return;
            }
            _precipitationMaps = new Image?[_precipitationMapPaths.Length];
            for (var i = 0; i < _precipitationMapPaths.Length; i++)
            {
                if (!string.IsNullOrEmpty(_precipitationMapPaths[i]))
                {
                    _precipitationMaps[i] = await mapLoader.LoadAsync(_precipitationMapPaths[i]).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Loads a set of images as the snowfall maps for this region from storage.
        /// </summary>
        /// <param name="mapLoader">
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used to retrieve the
        /// image.
        /// </param>
        public async Task LoadSnowfallMapsAsync(ISurfaceMapLoader mapLoader)
        {
            if (_snowfallMapPaths is null)
            {
                return;
            }
            _snowfallMaps = new Image?[_snowfallMapPaths.Length];
            for (var i = 0; i < _snowfallMapPaths.Length; i++)
            {
                if (!string.IsNullOrEmpty(_snowfallMapPaths[i]))
                {
                    _snowfallMaps[i] = await mapLoader.LoadAsync(_snowfallMapPaths[i]).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Loads the temperature map(s) for this region from storage.
        /// </summary>
        /// <param name="mapLoader">
        /// The <see cref="ISurfaceMapLoader"/> implementation which will be used to retrieve the
        /// image.
        /// </param>
        public async Task LoadTemperatureMapAsync(ISurfaceMapLoader mapLoader)
        {
            if (!string.IsNullOrEmpty(_temperatureMapSummerPath))
            {
                _temperatureMapSummer = await mapLoader.LoadAsync(_temperatureMapSummerPath).ConfigureAwait(false);
                if (string.IsNullOrEmpty(_temperatureMapWinterPath))
                {
                    _temperatureMapWinter = _temperatureMapSummer;
                }
            }

            if (!string.IsNullOrEmpty(_temperatureMapWinterPath))
            {
                _temperatureMapWinter = await mapLoader.LoadAsync(_temperatureMapWinterPath).ConfigureAwait(false);
                if (_temperatureMapSummer is null)
                {
                    _temperatureMapSummer = _temperatureMapWinter;
                }
            }
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
