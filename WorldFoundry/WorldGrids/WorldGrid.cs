using ExtensionLib;
using MathAndScience.MathUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using WorldFoundry.CelestialBodies.Planetoids;

namespace WorldFoundry.WorldGrids
{
    /// <summary>
    /// A specialized data structure to describe the topography and contents of the surface of a <see cref="Planetoid"/>.
    /// </summary>
    public class WorldGrid
    {
        /// <summary>
        /// Indicates the desired radius for <see cref="Tile"/>s, in m. Null by default, which
        /// indicates that the <see cref="DefaultGridSize"/> will be used instead.
        /// </summary>
        /// <remarks>
        /// If this property is set, a <see cref="GridSize"/> will automatically be set based on a
        /// <see cref="Planetoid"/>'s radius in order to produce <see cref="Tile"/>s with radii as
        /// close to the intended value as possible.
        /// </remarks>
        public static double? DefaultDesiredTileRadius { get; set; }

        private static short _defaultGridSize = 6;
        /// <summary>
        /// The default grid size (level of detail). Initial value is 6; maximum is <see cref="MaxGridSize"/>.
        /// </summary>
        public static short DefaultGridSize
        {
            get => _defaultGridSize;
            set => _defaultGridSize = Math.Min(value, MaxGridSize);
        }

        /// <summary>
        /// The default multiplier for the elevation noise generation.
        /// </summary>
        private const int ElevationFactor = 100;

        private static readonly List<(double fiveSided, double sixSided)> GridAreas = new List<(double fiveSided, double sixSided)>
        {
            { (0.995824126333975, 0) },
            { (0.322738912263057, 0.485548584813836) },
            { (0.0968747328418699, 0.163326204596911) },
            { (0.028438388993119, 0.0542239003721041) },
            { (0.00829449492399518, 0.0180056505123057) },
            { (0.00241467010654058, 0.00598964349018147) },
            { (0.000702568607242906, 0.00199468184049687) },
            { (0.000204386152311142, 0.000664635858772887) },
            { (5.94556443995932e-5, 0.000221511187440615) },
            { (1.72954119217969e-5, 7.38325734291003e-5) },
            { (5.03114709002561e-6, 2.46103198069843e-5) },
            { (1.46354854869707e-6, 8.20335939396184e-6) },
            { (4.25716556080933e-7, 2.73444691845834e-6) },
            { (1.23824518242403e-7, 9.11480125925495e-7) },
            { (3.60144138460099e-8, 3.03826887009348e-7) },
        };

        /// <summary>
        /// When generating a <see cref="GridSize"/> automatically (such as through <see
        /// cref="DefaultDesiredTileRadius"/>), this property indicates an optional maximum size
        /// allowable. Null by default, which indicates that the maximum is the absolute <see cref="MaxGridSize"/>.
        /// </summary>
        public static short? MaxGeneratedGridSize { get; set; }

        /// <summary>
        /// The maximum grid size (level of detail). Even values below this limit may be impractical,
        /// depending on system resources.
        /// </summary>
        public const short MaxGridSize = 14;

        /// <summary>
        /// The array of all <see cref="Corner"/>s which make up the <see cref="WorldGrid"/>.
        /// </summary>
        public Corner[] Corners { get; private set; }

        /// <summary>
        /// The random seed used during elevation height map generation.
        /// </summary>
        /// <remarks>
        /// Preserved so that future runs of the generation can produce identical results, in order
        /// to reproduce the same height map for different <see cref="GridSize"/>s.
        /// </remarks>
        internal int ElevationSeed { get; private set; }

        /// <summary>
        /// The array of all <see cref="Edge"/>s which make up the <see cref="WorldGrid"/>.
        /// </summary>
        public Edge[] Edges { get; private set; }

        /// <summary>
        /// The current grid size (level of detail).
        /// </summary>
        public short GridSize { get; private set; } = -1;

        /// <summary>
        /// The <see cref="Planetoid"/> which this <see cref="WorldGrid"/> maps.
        /// </summary>
        internal Planetoid Planet { get; set; }

        /// <summary>
        /// The array of all <see cref="Tile"/>s which make up the <see cref="WorldGrid"/>.
        /// </summary>
        public Tile[] Tiles { get; private set; }

        /// <summary>
        /// Initializes a new instance of <see cref="WorldGrid"/>.
        /// </summary>
        public WorldGrid() { }

        /// <summary>
        /// Initializes a new instance of <see cref="WorldGrid"/> with the given values.
        /// </summary>
        /// <param name="planet">
        /// The <see cref="Planetoid"/> this <see cref="WorldGrid"/> will map.
        /// </param>
        /// <param name="size">The desired <see cref="GridSize"/> (level of detail).</param>
        public WorldGrid(Planetoid planet, int size)
        {
            Planet = planet;
            SubdivideGrid(size);
        }

        /// <summary>
        /// Calculates the grid size which would most closely approximate the given <see
        /// cref="Tile"/> radius, given a <see cref="Planetoid"/>'s radius squared.
        /// </summary>
        /// <param name="radiusSquared">A <see cref="Planetoid"/>'s radius squared, in m.</param>
        /// <param name="tileRadius">The desired approximate <see cref="Tile"/> radius, in m.</param>
        /// <param name="max">
        /// An optional maximum grid size, which will not be exceeded by the calculation.
        /// </param>
        /// <returns>
        /// The grid size which will produce <see cref="Tile"/>s with the nearest radius to the
        /// desired value.
        /// </returns>
        public static short GetGridSizeForTileRadius(double radiusSquared, double tileRadius, int? max = null)
        {
            var prevDiff = 0.0;
            for (short i = 0; i < GridAreas.Count; i++)
            {
                var tileArea = i == 0 ? GridAreas[i].fiveSided : GridAreas[i].sixSided;
                tileArea *= radiusSquared;
                var radius = Math.Sqrt(tileArea / Math.PI);

                if (radius <= tileRadius || (max.HasValue && i >= max))
                {
                    if (i == 0)
                    {
                        return 0;
                    }
                    else
                    {
                        var diff = tileRadius - radius;
                        if (diff < prevDiff)
                        {
                            return i;
                        }
                        else
                        {
                            return (short)(i - 1);
                        }
                    }
                }
                else
                {
                    prevDiff = radius - tileRadius;
                }
            }
            return (short)(GridAreas.Count - 1);
        }

        private void AddCorner(int index, int[] tileIndexes)
        {
            var c = Corners[index];
            c.Vector = Vector3.Normalize(
                Tiles[tileIndexes[0]].Vector
                + Tiles[tileIndexes[1]].Vector
                + Tiles[tileIndexes[2]].Vector);
            for (var i = 0; i < 3; i++)
            {
                c.Tiles[i] = tileIndexes[i];
                var t = Tiles[tileIndexes[i]];
                t.Corners[t.IndexOfTile(tileIndexes[(i + 2) % 3])] = index;
            }
        }

        private void AddEdge(int index, int t0, int t1)
        {
            var e = Edges[index];
            e.Tiles[0] = t0;
            e.Tiles[1] = t1;
            var tile0 = Tiles[t0];
            var tile0t1 = tile0.IndexOfTile(t1);
            e.Corners[0] = tile0.Corners[tile0t1];
            e.Corners[1] = tile0.Corners[(tile0t1 + 1) % tile0.EdgeCount];
            for (var i = 0; i < 2; i++)
            {
                var t = Tiles[e.Tiles[i]];
                t.Edges[t.IndexOfTile(e.Tiles[(i + 1) % 2])] = index;
                var c = Corners[e.Corners[i]];
                c.Edges[c.IndexOfCorner(e.Corners[(i + 1) % 2])] = index;
            }
        }

        private void GenerateElevations(bool preserveShape = false)
        {
            if (!preserveShape)
            {
                ElevationSeed = Randomizer.Static.NextInclusiveMaxValue();
            }

            var m = new FastNoise(ElevationSeed);
            m.SetNoiseType(FastNoise.NoiseType.SimplexFractal);
            m.SetFractalOctaves(6);
            var n = new FastNoise(ElevationSeed >> (int.MaxValue / 5));
            n.SetNoiseType(FastNoise.NoiseType.SimplexFractal);
            n.SetFractalOctaves(5);
            var o = new FastNoise(ElevationSeed >> (int.MaxValue / 5 * 2));
            o.SetNoiseType(FastNoise.NoiseType.SimplexFractal);
            o.SetFractalOctaves(4);
            foreach (var t in Tiles)
            {
                var v = t.Vector * ElevationFactor;
                t.Elevation = m.GetNoise(v.X, v.Y, v.Z) * Math.Abs(n.GetNoise(v.X, v.Y, v.Z)) * Math.Abs(o.GetNoise(v.X, v.Y, v.Z));
            }
            foreach (var c in Corners)
            {
                var v = c.Vector * ElevationFactor;
                c.Elevation = m.GetNoise(v.X, v.Y, v.Z) * Math.Abs(n.GetNoise(v.X, v.Y, v.Z)) * Math.Abs(o.GetNoise(v.X, v.Y, v.Z));
            }

            var lowest = Math.Min(Tiles.Min(t => t.Elevation), Corners.Min(c => c.Elevation));
            var highest = Math.Max(Tiles.Max(t => t.Elevation), Corners.Max(c => c.Elevation));
            highest -= lowest;

            var max = 2e5 / Planet.SurfaceGravity;
            var r = new Random(ElevationSeed);
            var d = 0.0;
            for (var i = 0; i < 5; i++)
            {
                d += Math.Pow(r.NextDouble(), 3);
            }
            d /= 5;
            max = (max * (d + 3) / 8) + (max / 2);

            var scale = (float)(max / highest);
            foreach (var t in Tiles)
            {
                t.Elevation -= lowest;
                t.Elevation *= scale;
                t.FrictionCoefficient = t.Elevation <= 0 ? 0.000025f : (float)((t.Elevation * 6.667e-9) + 0.000025); // 0.000045 at 3000
            }
            foreach (var c in Corners)
            {
                c.Elevation -= lowest;
                c.Elevation *= scale;
            }
        }

        /// <summary>
        /// Finds the closest <see cref="Tile"/> to the given <see cref="Vector3"/>.
        /// </summary>
        /// <param name="vector">A <see cref="Vector3"/> representing a position on this <see cref="WorldGrid"/>.</param>
        /// <returns>
        /// The <see cref="Tile"/> whose position on the grid most closely matches the given <see
        /// cref="Vector3"/>; or null, if this grid has not yet been initialized.
        /// </returns>
        public Tile GetClosestTile(Vector3 vector)
        {
            if (Tiles.Length == 0)
            {
                return null;
            }
            var shortestDistance = double.PositiveInfinity;
            Tile closestTile = null;
            for (var i = 0; i < 12; i++)
            {
                var distanceSq = (Tiles[i].Vector - vector).LengthSquared();
                if (distanceSq < shortestDistance)
                {
                    shortestDistance = distanceSq;
                    closestTile = Tiles[i];
                }
            }
            return GetClosestTile(closestTile, vector, shortestDistance);
        }

        private Tile GetClosestTile(Tile tile, Vector3 vector, double distanceSq)
        {
            for (var i = 0; i < 12; i++)
            {
                var dSq = (Tiles[i].Vector - vector).LengthSquared();
                if (dSq < distanceSq)
                {
                    return GetClosestTile(Tiles[i], vector, dSq);
                }
            }
            return tile;
        }

        private float GetNorth(Tile t, Quaternion rotation)
        {
            var v = Vector3.Transform(Tiles[t.Tiles[0]].Vector, rotation);
            return (float)(Math.PI - Math.Atan2(v.Y, v.X));
        }

        internal void SetCoriolisCoefficients()
        {
            var coriolisCoefficients = new Dictionary<double, double>();
            foreach (var t in Tiles)
            {
                if (!coriolisCoefficients.ContainsKey(t.Latitude))
                {
                    coriolisCoefficients.Add(t.Latitude, Planet.GetCoriolisCoefficient(t.Latitude));
                }

                var wind = ((Math.Atan2(coriolisCoefficients[t.Latitude], t.FrictionCoefficient) / MathConstants.HalfPI) + 1) / 2;
                t.WindFactor = (float)(0.5 + ((wind - 0.5) * 0.5)); // lessen impact
            }
        }

        private void SetGridSize0()
        {
            SetNewGridSize(0);
            const float x = -0.525731112119133606f;
            const float z = -0.850650808352039932f;

            var tileVectors = new Vector3[12]
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

            var presetIndexes = new int[12, 5]
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

            for (var i = 0; i < Tiles.Length; i++)
            {
                var t = Tiles[i];
                t.Vector = tileVectors[i];
                for (var k = 0; k < 5; k++)
                {
                    t.Tiles[k] = presetIndexes[i, k];
                }
            }

            for (var i = 0; i < 5; i++)
            {
                AddCorner(i, new int[] { 0, presetIndexes[0, (i + 4) % 5], presetIndexes[0, i] });
            }
            for (var i = 0; i < 5; i++)
            {
                AddCorner(i + 5, new int[] { 3, presetIndexes[3, (i + 4) % 5], presetIndexes[3, i] });
            }
            AddCorner(10, new int[] { 10, 1, 8 });
            AddCorner(11, new int[] { 1, 10, 6 });
            AddCorner(12, new int[] { 6, 10, 7 });
            AddCorner(13, new int[] { 6, 7, 11 });
            AddCorner(14, new int[] { 11, 7, 2 });
            AddCorner(15, new int[] { 11, 2, 9 });
            AddCorner(16, new int[] { 9, 2, 5 });
            AddCorner(17, new int[] { 9, 5, 4 });
            AddCorner(18, new int[] { 4, 5, 8 });
            AddCorner(19, new int[] { 4, 8, 1 });

            for (var i = 0; i < Corners.Length; i++)
            {
                var c = Corners[i];
                for (var k = 0; k < 3; k++)
                {
                    var t = Tiles[c.Tiles[k]];
                    c.Corners[k] = t.Corners[(t.IndexOfCorner(i) + 1) % 5];
                }
            }

            var nextEdgeId = 0;
            for (var i = 0; i < Tiles.Length; i++)
            {
                for (var k = 0; k < 5; k++)
                {
                    if (Tiles[i].Edges[k] == -1)
                    {
                        AddEdge(nextEdgeId, i, presetIndexes[i, k]);
                        nextEdgeId++;
                    }
                }
            }
        }

        private (Corner[], Edge[], Tile[]) SetNewGridSize(short size)
        {
            GridSize = size;

            var baseCount = (int)Math.Pow(3, size);

            var prevCorners = new Corner[Corners?.Length ?? 0];
            Corners?.CopyTo(prevCorners, 0);
            var cornerCount = 20 * baseCount;
            Corners = new Corner[cornerCount];
            for (var i = 0; i < cornerCount; i++)
            {
                Corners[i] = new Corner(i);
            }

            var prevEdges = new Edge[Edges?.Length ?? 0];
            Edges?.CopyTo(prevEdges, 0);
            var edgeCount = 30 * baseCount;
            Edges = new Edge[edgeCount];
            for (var i = 0; i < edgeCount; i++)
            {
                Edges[i] = new Edge();
            }

            var prevTiles = new Tile[Tiles?.Length ?? 0];
            Tiles?.CopyTo(prevTiles, 0);
            var tileCount = (10 * baseCount) + 2;
            Tiles = new Tile[tileCount];
            for (var i = 0; i < tileCount; i++)
            {
                Tiles[i] = new Tile(i);
            }

            return (prevCorners, prevEdges, prevTiles);
        }

        private void SubdivideGrid()
        {
            var (prevCorners, prevEdges, prevTiles) = SetNewGridSize((short)(GridSize + 1));

            for (var i = 0; i < prevTiles.Length; i++)
            {
                var prevTile = prevTiles[i];
                var t = Tiles[i];
                t.Vector = prevTile.Vector;
                for (var k = 0; k < t.EdgeCount; k++)
                {
                    t.Tiles[k] = prevTile.Corners[k] + prevTiles.Length;
                }
            }

            for (var i = 0; i < prevCorners.Length; i++)
            {
                var prevCorner = prevCorners[i];
                var t = Tiles[i + prevTiles.Length];
                t.Vector = prevCorner.Vector;
                for (var k = 0; k < 3; k++)
                {
                    t.Tiles[2 * k] = prevCorner.Corners[k] + prevTiles.Length;
                    t.Tiles[(2 * k) + 1] = prevCorner.Tiles[k];
                }
            }

            var nextCornerId = 0;
            for (var i = 0; i < prevTiles.Length; i++)
            {
                var t = Tiles[i];
                for (var k = 0; k < t.EdgeCount; k++)
                {
                    AddCorner(nextCornerId, new int[] { i, t.Tiles[(k + t.EdgeCount - 1) % t.EdgeCount], t.Tiles[k] });
                    nextCornerId++;
                }
            }
            for (var i = 0; i < Corners.Length; i++)
            {
                for (var k = 0; k < 3; k++)
                {
                    var c = Corners[i];
                    var t = Tiles[c.Tiles[k]];
                    c.Corners[k] = t.Corners[(t.IndexOfCorner(i) + 1) % t.EdgeCount];
                }
            }

            var nextEdgeId = 0;
            for (var i = 0; i < Tiles.Length; i++)
            {
                var t = Tiles[i];
                for (var k = 0; k < t.EdgeCount; k++)
                {
                    if (t.Edges[k] == -1)
                    {
                        AddEdge(nextEdgeId, i, t.Tiles[k]);
                        nextEdgeId++;
                    }
                }
            }
        }

        /// <summary>
        /// Changes the current <see cref="GridSize"/> to the desired <paramref name="size"/>.
        /// </summary>
        /// <param name="size">The desired <see cref="GridSize"/> (level of detail).</param>
        /// <param name="preserveShape">
        /// If true, the same random seed will be used for elevation generation as any previous use,
        /// resulting in the same height map (can be used to maintain a similar look when changing
        /// <see cref="GridSize"/>, rather than an entirely new geography).
        /// </param>
        internal void SubdivideGrid(int size, bool preserveShape = false)
        {
            size = Math.Min(MaxGridSize, size);
            if (GridSize < 0 || size < GridSize)
            {
                SetGridSize0();
            }
            while (GridSize < size)
            {
                SubdivideGrid();
            }

            foreach (var c in Corners)
            {
                c.Latitude = Planet.VectorToLatitude(c.Vector);
                c.Longitude = Planet.VectorToLongitude(c.Vector);
            }

            foreach (var t in Tiles)
            {
                t.Area = Planet.RadiusSquared * (t.EdgeCount == 5 ? GridAreas[size].fiveSided : GridAreas[size].sixSided);
                t.Latitude = Planet.VectorToLatitude(t.Vector);
                t.Longitude = Planet.VectorToLongitude(t.Vector);
                var rotation = Planet.AxisRotation.GetReferenceRotation(t.Vector);
                t.North = GetNorth(t, rotation);
            }

            if (!Planet.HasFlatSurface)
            {
                GenerateElevations(preserveShape);
            }

            SetCoriolisCoefficients();
        }
    }
}
