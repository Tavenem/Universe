using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Troschuetz.Random;
using WorldFoundry.CelestialBodies.Planetoids.Planets.TerrestrialPlanets;
using WorldFoundry.Climate;
using WorldFoundry.Extensions;
using WorldFoundry.Substances;
using WorldFoundry.Utilities.MathUtil.Shapes;
using WorldFoundry.WorldGrids;

namespace WorldFoundry
{
    /// <summary>
    /// Represents a planet as a 3D grid of (mostly-hexagonal) tiles.
    /// </summary>
    public class Planet : TerrestrialPlanet
    {
        /// <summary>
        /// The default atmospheric pressure, used if none is specified during planet creation, in kPa.
        /// </summary>
        public const float defaultAtmosphericPressure = 101.325f;

        /// <summary>
        /// The default axial tilt, used if none is specified during planet creation, in radians.
        /// </summary>
        public const float defaultAxialTilt = 0.41f;

        /// <summary>
        /// The default grid size (level of detail), used if none is specified during planet creation.
        /// </summary>
        public const int defaultGridSize = 6;

        /// <summary>
        /// The default planetary radius, used if none is specified during planet creation, in meters.
        /// </summary>
        public const int defaultRadius = 6371000;

        /// <summary>
        /// The default period of revolution, used if none is specified during planet creation, in seconds.
        /// </summary>
        public const double defaultRevolutionPeriod = 31558150;

        /// <summary>
        /// The default period of rotation, used if none is specified during planet creation, in seconds.
        /// </summary>
        public const double defaultRotationalPeriod = 86164;

        /// <summary>
        /// The default ratio of water coverage, used if none is specified during planet creation.
        /// </summary>
        public const float defaultWaterRatio = 0.65f;

        private const int defaultDensity = 5514;

        private double _defaultSeasonDuration;
        private float _defaultSeasonProportion;
        private float _elapsedYearToDate = 0;
        private Season _lastSeason;
        private double? _seasonDuration;
        private float? _seasonProportion;

        /// <summary>
        /// The ratio of water coverage on the planet.
        /// </summary>
        public float WaterRatio { get; internal set; }

        /// <summary>
        /// Creates an empty <see cref="Planet"/> object. Has no useful values, and methods may not
        /// function properly. This constructor is intended for use by serialization routines, not to
        /// create a useful <see cref="Planet"/>.
        /// </summary>
        public Planet() { }

        /// <summary>
        /// Generates a planet with the given properties. In most cases, using the static <see
        /// cref="FromParams"/> method will be more useful than calling this constructor directly.
        /// </summary>
        public Planet(
            float atmosphericPressure,
            float axialTilt,
            int radius,
            double rotationalPeriod,
            float waterRatio,
            int gridSize,
            Guid? id = null)
        {
            ID = id ?? Guid.NewGuid();
            SetRadiusBase(radius);
            SetAxialTiltBase(axialTilt);
            SetRotationalPeriodBase(rotationalPeriod);
            WaterRatio = Math.Max(0, Math.Min(1, waterRatio));
            SetAtmosphericPressure(atmosphericPressure);

            ChangeGridSize(gridSize);
        }

        /// <summary>
        /// Generates a planet with the given properties. Default values will be used in place of any
        /// omitted parameters.
        /// </summary>
        public static Planet FromParams(
            float? atmosphericPressure = null,
            float? axialTilt = null,
            int? radius = null,
            double? rotationalPeriod = null,
            float? waterRatio = null,
            int? gridSize = null,
            Guid? id = null)
            => new Planet(
                atmosphericPressure ?? defaultAtmosphericPressure,
                axialTilt ?? defaultAxialTilt,
                radius ?? defaultRadius,
                rotationalPeriod ?? defaultRotationalPeriod,
                waterRatio ?? defaultWaterRatio,
                gridSize ?? defaultGridSize,
                id);

        /// <summary>
        /// Changes the atmospheric pressure of the planet.
        /// </summary>
        /// <param name="pressure">A pressure, in kPa.</param>
        public void ChangeAtmosphericPressure(float pressure)
        {
            SetAtmosphericPressure(pressure);
            _lastSeason = null;
        }

        /// <summary>
        /// Changes the axial tilt of the planet.
        /// </summary>
        /// <param name="axialTilt">An axial tilt, in radians.</param>
        public void ChangeAxialTilt(float axialTilt)
        {
            SetAxialTiltBase(axialTilt);
            _lastSeason = null;
        }

        /// <summary>
        /// Changes the grid size (level of detail) of the <see cref="Planet"/>.
        /// </summary>
        public void ChangeGridSize(int gridSize) => SetWorldGrid(Math.Min(WorldGrid.maxGridSize, gridSize));

        /// <summary>
        /// Changes the radius of the planet.
        /// </summary>
        /// <param name="radius">A radius, in meters.</param>
        public void ChangeRadius(int radius)
        {
            SetRadiusBase(radius);
            _lastSeason = null;
        }

        /// <summary>
        /// Changes the period of rotation of the planet.
        /// </summary>
        /// <param name="radius">A period, in seconds.</param>
        public void ChangeRotationalPeriod(double seconds)
        {
            SetRotationalPeriodBase(seconds);
            WorldGrid.SetCoriolisCoefficients();
            WorldGrid.UpdateCollectionsFromArrays();
        }

        private void ClassifyTerrain()
        {
            foreach (var t in WorldGrid.Tiles)
            {
                var land = 0;
                var water = 0;
                for (int i = 0; i < t.EdgeCount; i++)
                {
                    if (WorldGrid.GetCorner(t.GetCorner(i)).Elevation < 0)
                    {
                        water++;
                    }
                    else
                    {
                        land++;
                    }
                }
                if (t.Elevation < 0)
                {
                    water++;
                }
                else
                {
                    land++;
                }
                t.TerrainType = (land > 0 && water > 0)
                    ? TerrainType.Coast
                    : (land > 0 ? TerrainType.Land : TerrainType.Water);
            }
            foreach (var c in WorldGrid.Corners)
            {
                var land = 0;
                for (int i = 0; i < 3; i++)
                {
                    if (WorldGrid.GetCorner(c.GetCorner(i)).Elevation >= 0)
                    {
                        land++;
                    }
                }
                c.TerrainType = c.Elevation < 0
                    ? (land > 0 ? TerrainType.Coast : TerrainType.Water)
                    : TerrainType.Land;
            }
            foreach (var e in WorldGrid.Edges)
            {
                var type = TerrainType.Land;
                for (int i = 0; i < 2; i++)
                {
                    if (WorldGrid.GetCorner(e.GetCorner(i)).TerrainType != type)
                    {
                        type = i == 0 ? WorldGrid.GetTile(e.GetTile(i)).TerrainType : TerrainType.Coast;
                    }
                }
                e.TerrainType = type;
            }
        }

        private void CreateSea()
        {
            if (TMath.IsZero(HydrosphereSurface.Proportion))
            {
                return;
            }
            var waterMass = (HydrosphereSurface.GetProportion(Chemical.Water, Phase.Any)
                + HydrosphereSurface.GetProportion(Chemical.Water_Salt, Phase.Any))
                * (Hydrosphere.Mixtures.Count > 0 ? HydrosphereSurface.Proportion : 1)
                * Hydrosphere.Proportion
                * Mass;
            var oceanMass = 0.0;
            var oceanTileCount = 0;
            var seaLevel = 0f;
            while (waterMass > oceanMass)
            {
                var orderedTiles = WorldGrid.Tiles.OrderBy(t => t.Elevation);
                var landTiles = orderedTiles.Skip(oceanTileCount);
                var lowestLandElevation = landTiles.FirstOrDefault()?.Elevation;
                var nextLowestLandElevation = landTiles.SkipWhile(t => t.Elevation == lowestLandElevation).FirstOrDefault()?.Elevation;
                seaLevel = lowestLandElevation.HasValue
                    ? (nextLowestLandElevation.HasValue
                        ? (lowestLandElevation.Value + nextLowestLandElevation.Value) / 2
                        : lowestLandElevation.Value * 1.1f)
                    : WorldGrid.Tiles.Max(t => t.Elevation) * 1.1f;
                if (!nextLowestLandElevation.HasValue)
                {
                    break;
                }
                oceanTileCount = orderedTiles.TakeWhile(t => t.Elevation <= lowestLandElevation).Count();
                oceanMass = orderedTiles.Take(oceanTileCount).Sum(x => x.Area * (seaLevel - x.Elevation));
            }

            foreach (var t in WorldGrid.Tiles)
            {
                t.Elevation -= seaLevel;
            }
            foreach (var c in WorldGrid.Corners)
            {
                c.Elevation -= seaLevel;
            }
        }

        private void CreateSea(float waterRatio)
        {
            if (waterRatio == 0)
            {
                return;
            }
            var seaLevel = 0f;
            var oceanTileCount = 0;
            var oceanMass = 0.0;
            if (waterRatio == 1)
            {
                seaLevel = WorldGrid.Tiles.Max(t => t.Elevation) * 1.1f;
                oceanTileCount = WorldGrid.Tiles.Count;
                oceanMass = WorldGrid.Tiles.Sum(x => x.Area * (seaLevel - x.Elevation));
            }
            else
            {
                var targetWaterTileCount = (int)Math.Round(waterRatio * WorldGrid.Tiles.Count);
                var orderedTiles = WorldGrid.Tiles.OrderBy(t => t.Elevation);
                var landTiles = orderedTiles.Skip(targetWaterTileCount);
                var lowestLandElevation = landTiles.FirstOrDefault()?.Elevation;
                var nextLowestLandElevation = landTiles.SkipWhile(t => t.Elevation == lowestLandElevation).FirstOrDefault()?.Elevation;
                seaLevel = lowestLandElevation.HasValue
                    ? (nextLowestLandElevation.HasValue
                        ? (lowestLandElevation.Value + nextLowestLandElevation.Value) / 2
                        : lowestLandElevation.Value * 1.1f)
                    : WorldGrid.Tiles.Max(t => t.Elevation) * 1.1f;
                oceanTileCount = nextLowestLandElevation.HasValue
                    ? orderedTiles.TakeWhile(t => t.Elevation <= lowestLandElevation).Count()
                    : WorldGrid.Tiles.Count;
                oceanMass = orderedTiles.Take(oceanTileCount).Sum(x => x.Area * (seaLevel - x.Elevation));
            }

            foreach (var t in WorldGrid.Tiles)
            {
                t.Elevation -= seaLevel;
            }
            foreach (var c in WorldGrid.Corners)
            {
                c.Elevation -= seaLevel;
            }

            var hydrosphereProportion = oceanMass / Mass;
            if (Hydrosphere.Mixtures.Count > 0)
            {
                hydrosphereProportion *= HydrosphereSurface.Proportion;
            }
        }

        /// <summary>
        /// Generates a <see cref="Season"/> for the planet.
        /// </summary>
        /// <param name="duration">
        /// The duration of the season. If none of the planet's properties have been altered since
        /// the last <see cref="Season"/> was generated, the duration can be left as null and the
        /// last-used value will be repeated. If the duration is left null in other circumstances,
        /// the default of one quarter of the planet's rotational period will be used.
        /// </param>
        public Season GetSeason(double? duration = null)
        {
            if (_lastSeason == null)
            {
                SetClimate();
            }

            var revolutionPeriod = Orbit?.Period ?? defaultRevolutionPeriod;
            if (duration.HasValue && duration != _seasonDuration)
            {
                _seasonDuration = Math.Min(Math.Max(Season.secondsPerDay, duration.Value), revolutionPeriod);
                _seasonProportion = (float)(_seasonDuration / revolutionPeriod);
            }

            var defaultDuration = Math.Min(Math.Max(Season.secondsPerDay, revolutionPeriod / 4), revolutionPeriod);
            var defaultProportion = (float)(defaultDuration / revolutionPeriod);
            _lastSeason = new Season(
                this,
                _seasonDuration ?? defaultDuration,
                _elapsedYearToDate,
                _seasonProportion ?? defaultProportion,
                _lastSeason);

            _elapsedYearToDate += _seasonProportion ?? defaultProportion;
            if (_elapsedYearToDate > 1)
            {
                _elapsedYearToDate -= 1;
            }
            if (_elapsedYearToDate < Utilities.MathUtil.Constants.NearlyZero)
            {
                _elapsedYearToDate = 0;
            }

            return _lastSeason;
        }

        private void ScaleElevation(int seed)
        {
            var lowest = Math.Min(WorldGrid.Tiles.Min(t => t.Elevation), WorldGrid.Corners.Min(c => c.Elevation));
            var highest = Math.Max(WorldGrid.Tiles.Max(t => t.Elevation), WorldGrid.Corners.Max(c => c.Elevation));
            highest -= lowest;

            var max = (float)(2e5 / SurfaceGravity);
            var r = new Random(seed);
            var d = 0f;
            for (int i = 0; i < 5; i++)
            {
                d += (float)Math.Pow(r.NextDouble(), 3);
            }
            d /= 5;
            max = (max * (d + 3) / 8) + (max / 2);

            var scale = max / highest;
            foreach (var t in WorldGrid.Tiles)
            {
                t.Elevation -= lowest;
                t.Elevation *= scale;
            }
            foreach (var c in WorldGrid.Corners)
            {
                c.Elevation -= lowest;
                c.Elevation *= scale;
            }
        }

        private void SetAtmosphericPressure(float pressure)
        {
            var atmPressure = (float)Math.Max(0, Math.Min(3774.3562, pressure));
            habitabilityRequirements = new HabitabilityRequirements
            {
                MinimumSurfacePressure = atmPressure,
                MaximumSurfacePressure = atmPressure,
            };
            GenerateAtmosphere();
        }

        private void SetAxialTiltBase(float axialTilt)
        {
            AxialTilt = Math.Max(0, Math.Min((float)Math.PI, axialTilt));
            var q = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, AxialTilt);
            Axis = Vector3.Transform(Vector3.UnitY, q);
            AxisRotation = Quaternion.Conjugate(q);
        }

        /// <summary>
        /// Sets the climate for the planet's tiles. Can be called directly, but will be called
        /// automatically the first time a <see cref="Season"/> is generated for the planet, if not.
        /// </summary>
        public void SetClimate()
        {
            // 1 year is pre-generated as a single season,
            // and 1 as 12 seasons, to prime the algorithms,
            // which produce better values with historical data.
            var revolutionPeriod = Orbit?.Period ?? defaultRevolutionPeriod;
            _lastSeason = new Season(this, revolutionPeriod, 0, 1, null);
            var seasonDuration = revolutionPeriod / 12;
            var seasonProportion = 1.0f / 12;
            var seasons = new List<Season>(12);
            for (int i = 0; i < 12; i++)
            {
                _lastSeason = new Season(this, seasonDuration, (float)i / 12, seasonProportion, _lastSeason);
                seasons.Add(_lastSeason);
            }

            for (int i = 0; i < WorldGrid.Tiles.Count; i++)
            {
                WorldGrid.GetTile(i).SetClimate(
                    seasons.Average(s => s.TileClimates[i].Temperature),
                    seasons.Sum(s => s.TileClimates[i].Precipitation));
            }
        }

        private void SetRadiusBase(int radius)
        {
            var planetRadius = Math.Max(473000, Math.Min(9556500, radius));
            GenerateShape(planetRadius);
        }

        public void SetRotationalPeriodBase(double seconds) => RotationalPeriod = Math.Max(0, seconds);

        /// <summary>
        /// Sets the ratio of water coverage on the planet.
        /// </summary>
        /// <param name="waterRatio">A ratio: 0 indicates no water; 1 indicates complete coverage.</param>
        public void SetWaterRatio(float waterRatio)
        {
            CreateSea(Math.Max(0, Math.Min(1, waterRatio)));
            ClassifyTerrain();
            _lastSeason = null;
        }

        private void SetWorldGrid(int size)
        {
            GenerateWorldGrid(size);

            SetWaterRatio(WaterRatio);
        }
    }
}
