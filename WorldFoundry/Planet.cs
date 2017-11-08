using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using WorldFoundry.Climate;
using WorldFoundry.Extensions;
using WorldFoundry.WorldGrid;
using WorldFoundry.Utilities;

namespace WorldFoundry
{
    /// <summary>
    /// Represents a planet as a 3D grid of (mostly-hexagonal) tiles.
    /// </summary>
    public class Planet : IGrid
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
        /// The default multiplier for the elevation noise generation, used if none is specified during planet creation.
        /// </summary>
        public const int defaultElevationSize = 100;

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

        /// <summary>
        /// The maximum grid size (level of detail). 16 is a hard limit. 17 would cause an int overflow for list indexes.
        /// </summary>
        public const int maxGridSize = 16;

        private const float G = 6.67408e-11f;
        private const int density = 5514;
        private const float FourThirdsPIGp = (float)Utilities.MathUtil.Constants.FourThirdsPI * G * density;
        private const float M = 0.0289644f;
        private const float R = 8.3144598f;
        private const float MdivR = M / R;

        private float _angularVelocity;
        private double _defaultSeasonDuration;
        private float _defaultSeasonProportion;
        private float _elapsedYearToDate = 0;
        private int _elevationSize = defaultElevationSize;
        private int _gridSize = -1;
        private Season _lastSeason;
        private double? _seasonDuration;
        private float? _seasonProportion;

        /// <summary>
        /// The standard sea-level atmospheric pressure of the planet, in kPa.
        /// </summary>
        public float AtmosphericPressure { get; internal set; }

        /// <summary>
        /// The axial tilt of the planet, in radians.
        /// </summary>
        public float AxialTilt { get; private set; }

        /// <summary>
        /// A <see cref="Vector3"/> which represents the axis of the planet.
        /// </summary>
        public Vector3 Axis { get; private set; }

        internal Quaternion AxisRotation { get; private set; }

        internal Dictionary<float, float> CoriolisCoefficients { get; private set; }

        /// <summary>
        /// The list of all <see cref="Corner"/>s between the <see cref="Tile"/>s which make up the
        /// <see cref="Planet"/>'s grid.
        /// </summary>
        public List<Corner> Corners { get; private set; }

        /// <summary>
        /// The list of all <see cref="Edge"/>s between the <see cref="Tile"/>s which make up the
        /// <see cref="Planet"/>'s grid.
        /// </summary>
        public List<Edge> Edges { get; private set; }

        /// <summary>
        /// The standard gravity of the planet, in m/s².
        /// </summary>
        public float G0 { get; private set; }

        internal float G0MdivR { get; private set; }

        internal float HalfITCZWidth { get; private set; }

        /// <summary>
        /// The radius of the planet, in meters.
        /// </summary>
        public int Radius { get; private set; } = defaultRadius;

        /// <summary>
        /// The period of revolution of the planet, in seconds.
        /// </summary>
        public double RevolutionPeriod { get; internal set; } = defaultRevolutionPeriod;

        /// <summary>
        /// The period of rotation of the planet, in seconds.
        /// </summary>
        public double RotationalPeriod { get; private set; } = defaultRotationalPeriod;

        /// <summary>
        /// A string used to generate the seed which primes the random generator for this <see
        /// cref="Planet"/>. Can be used to recreate the exact same planet, provided the same input parameters.
        /// </summary>
        public string Seed { get; internal set; }

        /// <summary>
        /// The list of all <see cref="Tile"/>s which make up the <see cref="Planet"/>'s grid.
        /// </summary>
        public List<Tile> Tiles { get; private set; }

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
            double revolutionPeriod,
            double rotationalPeriod,
            float waterRatio,
            int gridSize,
            int elevationSize,
            string seed = null)
        {
            SetAtmosphericPressure(atmosphericPressure);
            SetAxialTiltBase(axialTilt);
            SetRadiusBase(radius);
            SetRevolutionPeriodBase(revolutionPeriod);
            SetRotationalPeriodBase(rotationalPeriod);
            WaterRatio = Math.Max(0, Math.Min(1, waterRatio));
            _elevationSize = Math.Max(0, elevationSize);
            Seed = string.IsNullOrEmpty(seed) ? GenerateSeed() : seed;

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
            double? revolutionPeriod = null,
            double? rotationalPeriod = null,
            float? waterRatio = null,
            int? gridSize = null,
            int? elevationSize = null,
            string seed = null)
            => new Planet(
                atmosphericPressure ?? defaultAtmosphericPressure,
                axialTilt ?? defaultAxialTilt,
                radius ?? defaultRadius,
                revolutionPeriod ?? defaultRevolutionPeriod,
                rotationalPeriod ?? defaultRotationalPeriod,
                waterRatio ?? defaultWaterRatio,
                gridSize ?? defaultGridSize,
                elevationSize ?? defaultElevationSize,
                seed);

        /// <summary>
        /// Generates a new random seed for the planet creation methods.
        /// </summary>
        public static string GenerateSeed() => StringExtensions.GetRandomLetters(8);

        private void AddCorner(int index, int t1, int t2, int t3)
        {
            var c = Corners[index];
            c.Tiles = new int[] { t1, t2, t3 };
            var v = Tiles[t1].Vector + Tiles[t2].Vector + Tiles[t3].Vector;
            c.Vector = Vector3.Normalize(v);
            for (int i = 0; i < 3; i++)
            {
                var t = Tiles[c.Tiles[i]];
                t.Corners[t.IndexOfTile(c.Tiles[(i + 2) % 3])] = index;
            }
        }

        private void AddEdge(int index, int t1, int t2)
        {
            var e = Edges[index];
            e.Tiles = new int[] { t1, t2 };
            e.Corners = new int[2]
            {
                Tiles[t1].Corners[Tiles[t1].IndexOfTile(t2)],
                Tiles[t1].Corners[(Tiles[t1].IndexOfTile(t2) + 1) % Tiles[t1].Edges.Length]
            };
            for (int i = 0; i < 2; i++)
            {
                var t = Tiles[e.Tiles[i]];
                t.Edges[t.IndexOfTile(e.Tiles[(i + 1) % 2])] = index;
                var c = Corners[e.Corners[i]];
                c.Edges[c.IndexOf(e.Corners[(i + 1) % 2])] = index;
            }
        }

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
        /// Changes the multiplier used by the elevation noise generation.
        /// </summary>
        public void ChangeElevationSize(int elevationSize)
        {
            _elevationSize = Math.Max(0, elevationSize);
            SetElevation(_elevationSize);
        }

        /// <summary>
        /// Changes the grid size (level of detail) of the <see cref="Planet"/>.
        /// </summary>
        public void ChangeGridSize(int gridSize)
        {
            SubdivideGrid(Math.Min(maxGridSize, gridSize));
            SetElevation(_elevationSize);
        }

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
        /// Changes the period of revolution of the planet.
        /// </summary>
        /// <param name="radius">A period, in seconds.</param>
        public void ChangeRevolutionPeriod(double seconds)
        {
            SetRevolutionPeriodBase(seconds);
            _lastSeason = null;
        }

        /// <summary>
        /// Changes the period of rotation of the planet.
        /// </summary>
        /// <param name="radius">A period, in seconds.</param>
        public void ChangeRotationalPeriod(double seconds)
        {
            SetRotationalPeriodBase(seconds);
            ChangeGridSize(_gridSize);
        }

        private void ClassifyTerrain()
        {
            foreach (var t in Tiles)
            {
                var land = 0;
                var water = 0;
                for (int i = 0; i < t.Corners.Length; i++)
                {
                    if (Corners[t.Corners[i]].Elevation < 0)
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
            foreach (var c in Corners)
            {
                var land = 0;
                for (int i = 0; i < 3; i++)
                {
                    if (Corners[c.Corners[i]].Elevation >= 0)
                    {
                        land++;
                    }
                }
                c.TerrainType = c.Elevation < 0
                    ? (land > 0 ? TerrainType.Coast : TerrainType.Water)
                    : TerrainType.Land;
            }
            foreach (var e in Edges)
            {
                var type = TerrainType.Land;
                for (int i = 0; i < 2; i++)
                {
                    if (Corners[e.Corners[i]].TerrainType != type)
                    {
                        type = i == 0 ? Tiles[e.Tiles[i]].TerrainType : TerrainType.Coast;
                    }
                }
                e.TerrainType = type;
            }
        }

        private void CreateSea(float waterRatio)
        {
            if (waterRatio == 0)
            {
                return;
            }
            var seaLevel = 0f;
            if (waterRatio == 1)
            {
                seaLevel = Tiles.Max(t => t.Elevation) * 1.1f;
            }
            else
            {
                var targetWaterTileCount = (int)Math.Round(waterRatio * Tiles.Count);
                var landTiles = Tiles.OrderBy(t => t.Elevation).Skip(targetWaterTileCount);
                var lowestLandElevation = landTiles.FirstOrDefault()?.Elevation;
                var nextLowestLandElevation = landTiles.SkipWhile(t => t.Elevation == lowestLandElevation).FirstOrDefault()?.Elevation;
                seaLevel = lowestLandElevation.HasValue
                    ? (nextLowestLandElevation.HasValue
                        ? (lowestLandElevation.Value + nextLowestLandElevation.Value) / 2
                        : lowestLandElevation.Value * 1.1f)
                    : Tiles.Max(t => t.Elevation) * 1.1f;
            }

            foreach (var t in Tiles)
            {
                t.Elevation -= seaLevel;
            }
            foreach (var c in Corners)
            {
                c.Elevation -= seaLevel;
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

            if (duration.HasValue && duration != _seasonDuration)
            {
                _seasonDuration = Math.Min(Math.Max(Season.secondsPerDay, duration.Value), RevolutionPeriod);
                _seasonProportion = (float)(_seasonDuration / RevolutionPeriod);
            }

            _lastSeason = new Season(
                this,
                _seasonDuration ?? _defaultSeasonDuration,
                _elapsedYearToDate,
                _seasonProportion ?? _defaultSeasonProportion,
                _lastSeason);

            _elapsedYearToDate += _seasonProportion ?? _defaultSeasonProportion;
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

        private float GetCoriolisCoefficient(float latitude) => 2 * _angularVelocity * (float)Math.Sin(latitude);

        private float GetNorth(Tile t, Quaternion rotation)
        {
            var v = Vector3.Transform(Tiles[t.Tiles[0]].Vector, rotation);
            return (float)(Math.PI - Math.Atan2(v.Y, v.X));
        }

        private float GetPlanetLatitude(Vector3 v) => (float)(Utilities.MathUtil.Constants.HalfPI - Axis.GetAngle(v));

        private float GetPlanetLongitude(Vector3 v)
        {
            var u = Vector3.Transform(v, AxisRotation);
            return u.X == 0 && u.Z == 0
                ? 0
                : (float)Math.Atan2(u.X, u.Z);
        }

        private void ScaleElevation(int seed)
        {
            var lowest = Math.Min(Tiles.Min(t => t.Elevation), Corners.Min(c => c.Elevation));
            var highest = Math.Max(Tiles.Max(t => t.Elevation), Corners.Max(c => c.Elevation));
            highest -= lowest;

            var max = 2e5f / G0;
            var r = new Random(seed);
            var d = 0f;
            for (int i = 0; i < 5; i++)
            {
                d += (float)Math.Pow(r.NextDouble(), 3);
            }
            d /= 5;
            max = (max * (d + 3) / 8) + (max / 2);

            var scale = max / highest;
            foreach (var t in Tiles)
            {
                t.Elevation -= lowest;
                t.Elevation *= scale;
            }
            foreach (var c in Corners)
            {
                c.Elevation -= lowest;
                c.Elevation *= scale;
            }
        }

        private void SetAtmosphericPressure(float pressure) => AtmosphericPressure = (float)Math.Max(0, Math.Min(3774.3562, pressure));

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
            _lastSeason = new Season(this, RevolutionPeriod, 0, 1, null);
            var seasonDuration = RevolutionPeriod / 12;
            var seasonProportion = 1.0f / 12;
            var seasons = new List<Season>(12);
            for (int i = 0; i < 12; i++)
            {
                _lastSeason = new Season(this, seasonDuration, (float)i / 12, seasonProportion, _lastSeason);
                seasons.Add(_lastSeason);
            }

            for (int i = 0; i < Tiles.Count; i++)
            {
                Tiles[i].SetClimate(
                    seasons.Average(s => s.TileClimates[i].Temperature),
                    seasons.Sum(s => s.TileClimates[i].Precipitation));
            }
        }

        private void SetElevation(int size)
        {
            int seed = Randomizer.GetSeed(Seed);

            var m = new FastNoise(seed);
            m.SetNoiseType(FastNoise.NoiseType.SimplexFractal);
            m.SetFractalOctaves(6);
            var n = new FastNoise(seed >> (int.MaxValue / 5));
            n.SetNoiseType(FastNoise.NoiseType.SimplexFractal);
            n.SetFractalOctaves(5);
            var o = new FastNoise(seed >> (int.MaxValue / 5 * 2));
            o.SetNoiseType(FastNoise.NoiseType.SimplexFractal);
            o.SetFractalOctaves(4);
            foreach (var t in Tiles)
            {
                var v = t.Vector * size;
                t.Elevation = m.GetNoise(v.X, v.Y, v.Z) * Math.Abs(n.GetNoise(v.X, v.Y, v.Z)) * Math.Abs(o.GetNoise(v.X, v.Y, v.Z));
            }
            foreach (var c in Corners)
            {
                var v = c.Vector * size;
                c.Elevation = m.GetNoise(v.X, v.Y, v.Z) * Math.Abs(n.GetNoise(v.X, v.Y, v.Z)) * Math.Abs(o.GetNoise(v.X, v.Y, v.Z));
            }

            ScaleElevation(seed);

            foreach (var t in Tiles)
            {
                t.SetFrictionCoefficient();
            }

            SetWaterRatio(WaterRatio);
        }

        private void SetGridSize0()
        {
            SetNewGridSize(0);
            var x = -0.525731112119133606f;
            var z = -0.850650808352039932f;

            var icosTiles = new Vector3[12]
            {
                new Vector3(x, 0, -z),
                new Vector3(-x, 0, -z),
                new Vector3(x, 0, z),
                new Vector3(-x, 0, z),
                new Vector3(0, -z, -x),
                new Vector3(0, -z, x),
                new Vector3(0, z, -x),
                new Vector3(0, z, x),
                new Vector3(-z, -x, 0),
                new Vector3(z, -x, 0),
                new Vector3(-z, x, 0),
                new Vector3(z, x, 0)
            };

            var icosTilesN = new int[12, 5]
            {
                {1, 6, 11, 9, 4},
                {0, 4, 8, 10, 6},
                {3, 5, 9, 11, 7},
                {2, 7, 10, 8, 5},
                {0, 9, 5, 8, 1},
                {2, 3, 8, 4, 9},
                {0, 1, 10, 7, 11},
                {2, 11, 6, 10, 3},
                {1, 4, 5, 3, 10},
                {0, 11, 2, 5, 4},
                {1, 8, 3, 7, 6},
                {0, 6, 7, 2, 9}
            };

            for (int i = 0; i < Tiles.Count; i++)
            {
                Tiles[i].Vector = icosTiles[i];
                for (int k = 0; k < 5; k++)
                {
                    Tiles[i].Tiles[k] = icosTilesN[i, k];
                }
            }

            for (int i = 0; i < 5; i++)
            {
                AddCorner(i, 0, icosTilesN[0, (i + 4) % 5], icosTilesN[0, i]);
            }
            for (int i = 0; i < 5; i++)
            {
                AddCorner(i + 5, 3, icosTilesN[3, (i + 4) % 5], icosTilesN[3, i]);
            }
            AddCorner(10, 10, 1, 8);
            AddCorner(11, 1, 10, 6);
            AddCorner(12, 6, 10, 7);
            AddCorner(13, 6, 7, 11);
            AddCorner(14, 11, 7, 2);
            AddCorner(15, 11, 2, 9);
            AddCorner(16, 9, 2, 5);
            AddCorner(17, 9, 5, 4);
            AddCorner(18, 4, 5, 8);
            AddCorner(19, 4, 8, 1);

            for (int i = 0; i < Corners.Count; i++)
            {
                for (int k = 0; k < 3; k++)
                {
                    var t = Tiles[Corners[i].Tiles[k]];
                    Corners[i].Corners[k] = t.Corners[(t.IndexOfCorner(i) + 1) % 5];
                }
            }

            int nextEdgeId = 0;
            for (int i = 0; i < Tiles.Count; i++)
            {
                for (int k = 0; k < 5; k++)
                {
                    if (Tiles[i].Edges[k] == -1)
                    {
                        AddEdge(nextEdgeId, i, icosTilesN[i, k]);
                        nextEdgeId++;
                    }
                }
            }
        }

        private (List<Corner>, List<Edge>, List<Tile>) SetNewGridSize(int size)
        {
            _gridSize = size;

            var baseCount = (int)Math.Pow(3, size);

            var prevCorners = Corners == null ? new List<Corner>() : new List<Corner>(Corners);
            var cornerCount = 20 * baseCount;
            Corners = new List<Corner>(cornerCount);
            for (int i = 0; i < cornerCount; i++)
            {
                Corners.Add(new Corner(i));
            }

            var prevEdges = Edges == null ? new List<Edge>() : new List<Edge>(Edges);
            var edgeCount = 30 * baseCount;
            Edges = new List<Edge>(edgeCount);
            for (int i = 0; i < edgeCount; i++)
            {
                Edges.Add(new Edge());
            }

            var prevTiles = Tiles == null ? new List<Tile>() : new List<Tile>(Tiles);
            var tileCount = 10 * baseCount + 2;
            Tiles = new List<Tile>(tileCount);
            for (int i = 0; i < tileCount; i++)
            {
                Tiles.Add(new Tile(i < 12 ? 5 : 6));
            }

            return (prevCorners, prevEdges, prevTiles);
        }

        private void SetRadiusBase(int radius)
        {
            Radius = Math.Max(473000, Math.Min(9556500, radius));
            HalfITCZWidth = 370400f / Radius;
            G0 = FourThirdsPIGp * Radius;
            G0MdivR = G0 * MdivR;
        }

        public void SetRevolutionPeriodBase(double seconds)
        {
            RevolutionPeriod = Math.Max(0, seconds);
            _defaultSeasonDuration = Math.Min(Math.Max(Season.secondsPerDay, RevolutionPeriod / 4), RevolutionPeriod);
            _defaultSeasonProportion = (float)(_defaultSeasonDuration / RevolutionPeriod);
        }

        public void SetRotationalPeriodBase(double seconds)
        {
            RotationalPeriod = Math.Max(0, seconds);
            _angularVelocity = RotationalPeriod == 0 ? 0 : (float)(Utilities.MathUtil.Constants.TwoPI / RotationalPeriod);
        }

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

        private void SubdivideGrid()
        {
            var (prevCorners, prevEdges, prevTiles) = SetNewGridSize(_gridSize + 1);

            for (int i = 0; i < prevTiles.Count; i++)
            {
                Tiles[i].Vector = prevTiles[i].Vector;
                for (int k = 0; k < Tiles[i].Edges.Length; k++)
                {
                    Tiles[i].Tiles[k] = prevTiles[i].Corners[k] + prevTiles.Count;
                }
            }

            for (int i = 0; i < prevCorners.Count; i++)
            {
                Tiles[i + prevTiles.Count].Vector = prevCorners[i].Vector;
                for (int k = 0; k < 3; k++)
                {
                    Tiles[i + prevTiles.Count].Tiles[2 * k] = prevCorners[i].Corners[k] + prevTiles.Count;
                    Tiles[i + prevTiles.Count].Tiles[2 * k + 1] = prevCorners[i].Tiles[k];
                }
            }

            int nextCornerId = 0;
            for (int i = 0; i < prevTiles.Count; i++)
            {
                var t = Tiles[i];
                for (int k = 0; k < t.Edges.Length; k++)
                {
                    AddCorner(nextCornerId, i, t.Tiles[(k + t.Edges.Length - 1) % t.Edges.Length], t.Tiles[k]);
                    nextCornerId++;
                }
            }
            for (int i = 0; i < Corners.Count; i++)
            {
                for (int k = 0; k < 3; k++)
                {
                    var t = Tiles[Corners[i].Tiles[k]];
                    Corners[i].Corners[k] = t.Corners[(t.IndexOfCorner(i) + 1) % t.Edges.Length];
                }
            }

            var nextEdgeId = 0;
            for (int i = 0; i < Tiles.Count; i++)
            {
                for (int k = 0; k < Tiles[i].Edges.Length; k++)
                {
                    if (Tiles[i].Edges[k] == -1)
                    {
                        AddEdge(nextEdgeId, i, Tiles[i].Tiles[k]);
                        nextEdgeId++;
                    }
                }
            }
        }

        private void SubdivideGrid(int size)
        {
            if (_gridSize < 0)
            {
                SetGridSize0();
            }
            while (_gridSize < size)
            {
                SubdivideGrid();
            }

            foreach (var e in Edges)
            {
                e.Length = Vector3.Distance(Corners[e.Corners[0]].Vector, Corners[e.Corners[1]].Vector) * Radius;
            }

            foreach (var c in Corners)
            {
                c.Latitude = GetPlanetLatitude(c.Vector);
                c.Longitude = GetPlanetLongitude(c.Vector);
            }

            CoriolisCoefficients = new Dictionary<float, float>();
            var radiusSq = (float)Math.Pow(Radius, 2);
            foreach (var t in Tiles)
            {
                var a = 0f;
                for (int k = 0; k < t.Edges.Length; k++)
                {
                    var angle = Math.Acos(Vector3.Dot(Vector3.Normalize(t.Vector) - Corners[t.Corners[k]].Vector,
                        Vector3.Normalize(t.Vector - Corners[t.Corners[(k + 1) % t.Edges.Length]].Vector)));
                    a += 0.5f * (float)Math.Sin(angle) * Vector3.Distance(t.Vector, Corners[t.Corners[k]].Vector) * Vector3.Distance(t.Vector, Corners[t.Corners[(k + 1) % t.Edges.Length]].Vector);
                }
                t.Area = a * radiusSq;

                t.Latitude = GetPlanetLatitude(t.Vector);
                if (!CoriolisCoefficients.ContainsKey(t.Latitude))
                {
                    CoriolisCoefficients.Add(t.Latitude, GetCoriolisCoefficient(t.Latitude));
                }
                t.Longitude = GetPlanetLongitude(t.Vector);
                var rotation = AxisRotation.GetReferenceRotation(t.Vector);
                t.SetPolygon(this, rotation);
                t.North = GetNorth(t, rotation);
            }
        }
    }
}
