using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Numerics;
using WorldFoundry.CelestialBodies.Planetoids.Planets.TerrestrialPlanets;
using WorldFoundry.Extensions;
using WorldFoundry.Utilities;

namespace WorldFoundry.WorldGrids
{
    public class WorldGrid
    {
        /// <summary>
        /// The default grid size (level of detail).
        /// </summary>
        public const short DefaultGridSize = 6;

        /// <summary>
        /// The default multiplier for the elevation noise generation.
        /// </summary>
        private const int ElevationFactor = 100;

        /// <summary>
        /// The maximum grid size (level of detail). 16 is a hard limit. 17 would cause an int
        /// overflow for list indexes.
        /// </summary>
        public const short MaxGridSize = 16;

        private Corner[] _cornerArray;
        [NotMapped]
        internal Corner[] CornerArray
        {
            get
            {
                if (_cornerArray == null && Corners != null)
                {
                    SetArrayFromCollection(ref _cornerArray, Corners);
                }
                return _cornerArray;
            }
            set => _cornerArray = value;
        }

        /// <summary>
        /// The list of all <see cref="Corner"/>s which make up the <see cref="WorldGrid"/>.
        /// </summary>
        public ICollection<Corner> Corners { get; set; }

        /// <summary>
        /// The random seed used during elevation height map generation.
        /// </summary>
        /// <remarks>
        /// Preserved so that future runs of the generation can produce identical results, in order
        /// to reproduce the same height map for different <see cref="GridSize"/>s.
        /// </remarks>
        internal int ElevationSeed { get; private set; }

        private Edge[] _edgeArray;
        [NotMapped]
        internal Edge[] EdgeArray
        {
            get
            {
                if (_edgeArray == null && Edges != null)
                {
                    SetArrayFromCollection(ref _edgeArray, Edges);
                }
                return _edgeArray;
            }
            set => _edgeArray = value;
        }

        /// <summary>
        /// The list of all <see cref="Edge"/>s which make up the <see cref="WorldGrid"/>.
        /// </summary>
        public ICollection<Edge> Edges { get; set; }

        /// <summary>
        /// The current grid size (level of detail).
        /// </summary>
        public short GridSize { get; private set; } = -1;

        /// <summary>
        /// The <see cref="TerrestrialPlanet"/> which this <see cref="WorldGrid"/> maps.
        /// </summary>
        internal TerrestrialPlanet Planet { get; set; }

        private Tile[] _tileArray;
        [NotMapped]
        internal Tile[] TileArray
        {
            get
            {
                if (_tileArray == null && Tiles != null)
                {
                    SetArrayFromCollection(ref _tileArray, Tiles);
                }
                return _tileArray;
            }
            set => _tileArray = value;
        }

        /// <summary>
        /// The list of all <see cref="Tile"/>s which make up the <see cref="WorldGrid"/>.
        /// </summary>
        public ICollection<Tile> Tiles { get; set; }

        /// <summary>
        /// Initializes a new instance of <see cref="WorldGrid"/>.
        /// </summary>
        public WorldGrid() { }

        /// <summary>
        /// Initializes a new instance of <see cref="WorldGrid"/> with the given size (level of detail).
        /// </summary>
        /// <param name="planet">
        /// The <see cref="TerrestrialPlanet"/> this <see cref="WorldGrid"/> will map.
        /// </param>
        /// <param name="size">The grid size (level of detail) for this <see cref="WorldGrid"/>.</param>
        public WorldGrid(TerrestrialPlanet planet, int size)
        {
            Planet = planet;
            SubdivideGrid(size);
        }

        private static void SetArrayFromCollection<T>(ref T[] array, ICollection<T> collection) where T : IIndexedItem
        {
            array = new T[collection.Count];
            for (int i = 0; i < collection.Count; i++)
            {
                array[i] = collection.FirstOrDefault(x => x.Index == i);
            }
        }

        private void AddCorner(int index, int[] tileIndexes)
        {
            var c = CornerArray[index];
            c.Vector = Vector3.Normalize(
                TileArray[tileIndexes[0]].Vector
                + TileArray[tileIndexes[1]].Vector
                + TileArray[tileIndexes[2]].Vector);
            for (int i = 0; i < 3; i++)
            {
                c.SetTile(i, tileIndexes[i]);
                var t = TileArray[tileIndexes[i]];
                t.SetCorner(t.IndexOfTile(tileIndexes[(i + 2) % 3]), index);
            }
        }

        private void AddEdge(int index, int t0, int t1)
        {
            var e = EdgeArray[index];
            e.SetTile(0, t0);
            e.SetTile(1, t1);
            var tile0 = TileArray[t0];
            var tile0t1 = tile0.IndexOfTile(t1);
            e.SetCorner(0, tile0.GetCorner(tile0t1));
            e.SetCorner(1, tile0.GetCorner((tile0t1 + 1) % tile0.EdgeCount));
            for (int i = 0; i < 2; i++)
            {
                var t = TileArray[e.GetTile(i)];
                t.SetEdge(t.IndexOfTile(e.GetTile((i + 1) % 2)), index);
                var c = CornerArray[e.GetCorner(i)];
                c.SetEdge(c.IndexOfCorner(e.GetCorner((i + 1) % 2)), index);
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
            foreach (var t in TileArray)
            {
                var v = t.Vector * ElevationFactor;
                t.Elevation = m.GetNoise(v.X, v.Y, v.Z) * Math.Abs(n.GetNoise(v.X, v.Y, v.Z)) * Math.Abs(o.GetNoise(v.X, v.Y, v.Z));
            }
            foreach (var c in CornerArray)
            {
                var v = c.Vector * ElevationFactor;
                c.Elevation = m.GetNoise(v.X, v.Y, v.Z) * Math.Abs(n.GetNoise(v.X, v.Y, v.Z)) * Math.Abs(o.GetNoise(v.X, v.Y, v.Z));
            }

            var lowest = Math.Min(TileArray.Min(t => t.Elevation), CornerArray.Min(c => c.Elevation));
            var highest = Math.Max(TileArray.Max(t => t.Elevation), CornerArray.Max(c => c.Elevation));
            highest -= lowest;

            var max = 2e5 / Planet.SurfaceGravity;
            var r = new Random(ElevationSeed);
            var d = 0.0;
            for (int i = 0; i < 5; i++)
            {
                d += Math.Pow(r.NextDouble(), 3);
            }
            d /= 5;
            max = (max * (d + 3) / 8) + (max / 2);

            var scale = (float)(max / highest);
            foreach (var t in TileArray)
            {
                t.Elevation -= lowest;
                t.Elevation *= scale;
                t.FrictionCoefficient = t.Elevation <= 0 ? 0.000025f : t.Elevation * 6.667e-9f + 0.000025f; // 0.000045 at 3000
            }
            foreach (var c in CornerArray)
            {
                c.Elevation -= lowest;
                c.Elevation *= scale;
            }
        }

        /// <summary>
        /// Gets the <see cref="Corner"/> with the given index.
        /// </summary>
        /// <param name="index">A zero-based index.</param>
        /// <returns>The <see cref="Corner"/> with the given index.</returns>
        public Corner GetCorner(int index) => Corners.FirstOrDefault(x => x.Index == index);

        /// <summary>
        /// Gets the <see cref="Edge"/> with the given index.
        /// </summary>
        /// <param name="index">A zero-based index.</param>
        /// <returns>The <see cref="Edge"/> with the given index.</returns>
        public Edge GetEdge(int index) => Edges.FirstOrDefault(x => x.Index == index);

        private float GetNorth(Tile t, Quaternion rotation)
        {
            var v = Vector3.Transform(TileArray[t.Tile0].Vector, rotation);
            return (float)(Math.PI - Math.Atan2(v.Y, v.X));
        }

        /// <summary>
        /// Gets the <see cref="Tile"/> with the given index.
        /// </summary>
        /// <param name="index">A zero-based index.</param>
        /// <returns>The <see cref="Tile"/> with the given index.</returns>
        public Tile GetTile(int index) => Tiles.FirstOrDefault(x => x.Index == index);

        internal void SetCoriolisCoefficients()
        {
            var coriolisCoefficients = new Dictionary<float, float>();
            foreach (var t in TileArray)
            {
                if (!coriolisCoefficients.ContainsKey(t.Latitude))
                {
                    coriolisCoefficients.Add(t.Latitude, Planet.GetCoriolisCoefficient(t.Latitude));
                }
                t.CoriolisCoefficient = coriolisCoefficients[t.Latitude];
            }
        }

        /// <summary>
        /// Changes the current <see cref="GridSize"/> to the desired <paramref name="size"/>.
        /// </summary>
        /// <param name="size">The desired <see cref="GridSize"/> (level of detail).</param>
        /// <param name="preserveShape">
        /// If true, the same random seed will be used for elevation generation as before, resulting
        /// in the same height map (can be used to maintain a similar look when changing <see
        /// cref="GridSize"/>, rather than an entirely new geography).
        /// </param>
        internal void SetGridSize(short size, bool preserveShape = false) => SubdivideGrid(Math.Min(MaxGridSize, size));

        private void SetGridSize0()
        {
            SetNewGridSize(0);
            var x = -0.525731112119133606f;
            var z = -0.850650808352039932f;

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

            for (int i = 0; i < TileArray.Length; i++)
            {
                var t = TileArray[i];
                t.Vector = tileVectors[i];
                for (int k = 0; k < 5; k++)
                {
                    t.SetTile(k, presetIndexes[i, k]);
                }
            }

            for (int i = 0; i < 5; i++)
            {
                AddCorner(i, new int[] { 0, presetIndexes[0, (i + 4) % 5], presetIndexes[0, i] });
            }
            for (int i = 0; i < 5; i++)
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

            for (int i = 0; i < CornerArray.Length; i++)
            {
                var c = CornerArray[i];
                for (int k = 0; k < 3; k++)
                {
                    var t = TileArray[c.GetTile(k)];
                    c.SetCorner(k, t.GetCorner((t.IndexOfCorner(i) + 1) % 5));
                }
            }

            int nextEdgeId = 0;
            for (int i = 0; i < TileArray.Length; i++)
            {
                for (int k = 0; k < 5; k++)
                {
                    if (TileArray[i].GetEdge(k) == -1)
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

            var prevCorners = new Corner[CornerArray == null ? 0 : CornerArray.Length];
            if (CornerArray != null)
            {
                CornerArray.CopyTo(prevCorners, 0);
            }
            var cornerCount = 20 * baseCount;
            CornerArray = new Corner[cornerCount];
            for (int i = 0; i < cornerCount; i++)
            {
                CornerArray[i] = new Corner(this, i);
            }

            var prevEdges = new Edge[EdgeArray == null ? 0 : EdgeArray.Length];
            if (EdgeArray != null)
            {
                EdgeArray.CopyTo(prevEdges, 0);
            }
            var edgeCount = 30 * baseCount;
            EdgeArray = new Edge[edgeCount];
            for (int i = 0; i < edgeCount; i++)
            {
                EdgeArray[i] = new Edge(this, i);
            }

            var prevTiles = new Tile[TileArray == null ? 0 : TileArray.Length];
            if (TileArray != null)
            {
                TileArray.CopyTo(prevTiles, 0);
            }
            var tileCount = 10 * baseCount + 2;
            TileArray = new Tile[tileCount];
            for (int i = 0; i < tileCount; i++)
            {
                TileArray[i] = new Tile(this, i, i < 12 ? 5 : 6);
            }

            return (prevCorners, prevEdges, prevTiles);
        }

        private void SubdivideGrid()
        {
            var (prevCorners, prevEdges, prevTiles) = SetNewGridSize((short)(GridSize + 1));

            for (int i = 0; i < prevTiles.Length; i++)
            {
                var prevTile = prevTiles[i];
                var t = TileArray[i];
                t.Vector = prevTile.Vector;
                for (int k = 0; k < t.EdgeCount; k++)
                {
                    t.SetTile(k, prevTile.GetCorner(k) + prevTiles.Length);
                }
            }

            for (int i = 0; i < prevCorners.Length; i++)
            {
                var prevCorner = prevCorners[i];
                var t = TileArray[i + prevTiles.Length];
                t.Vector = prevCorner.Vector;
                for (int k = 0; k < 3; k++)
                {
                    t.SetTile(2 * k, prevCorner.GetCorner(k) + prevTiles.Length);
                    t.SetTile(2 * k + 1, prevCorner.GetTile(k));
                }
            }

            int nextCornerId = 0;
            for (int i = 0; i < prevTiles.Length; i++)
            {
                var t = TileArray[i];
                for (int k = 0; k < t.EdgeCount; k++)
                {
                    AddCorner(nextCornerId, new int[] { i, t.GetTile((k + t.EdgeCount - 1) % t.EdgeCount), t.GetTile(k) });
                    nextCornerId++;
                }
            }
            for (int i = 0; i < CornerArray.Length; i++)
            {
                for (int k = 0; k < 3; k++)
                {
                    var c = CornerArray[i];
                    var t = TileArray[c.GetTile(k)];
                    c.SetCorner(k, t.GetCorner((t.IndexOfCorner(i) + 1) % t.EdgeCount));
                }
            }

            var nextEdgeId = 0;
            for (int i = 0; i < TileArray.Length; i++)
            {
                var t = TileArray[i];
                for (int k = 0; k < t.EdgeCount; k++)
                {
                    if (t.GetEdge(k) == -1)
                    {
                        AddEdge(nextEdgeId, i, t.GetTile(k));
                        nextEdgeId++;
                    }
                }
            }
        }

        private void SubdivideGrid(int size, bool preserveShape = false)
        {
            if (GridSize < 0 || size < GridSize)
            {
                SetGridSize0();
            }
            while (GridSize < size)
            {
                SubdivideGrid();
            }

            foreach (var e in EdgeArray)
            {
                e.Length = (float)(Vector3.Distance(CornerArray[e.Corner0].Vector, CornerArray[e.Corner1].Vector) * Planet.Radius);
            }

            foreach (var c in CornerArray)
            {
                c.Latitude = Planet.VectorToLatitude(c.Vector);
                c.Longitude = Planet.VectorToLongitude(c.Vector);
            }

            foreach (var t in TileArray)
            {
                var a = 0.0;
                for (int k = 0; k < t.EdgeCount; k++)
                {
                    var c1v = CornerArray[t.GetCorner(k)].Vector;
                    var c2v = CornerArray[t.GetCorner((k + 1) % t.EdgeCount)].Vector;
                    var angle = Math.Acos(Vector3.Dot(Vector3.Normalize(t.Vector) - c1v, Vector3.Normalize(t.Vector - c2v)));
                    a += 0.5 * Math.Sin(angle) * Vector3.Distance(t.Vector, c1v) * Vector3.Distance(t.Vector, c2v);
                }
                t.Area = (float)(a * Planet.RadiusSquared);

                t.Latitude = Planet.VectorToLatitude(t.Vector);
                t.Longitude = Planet.VectorToLongitude(t.Vector);
                var rotation = Planet.AxisRotation.GetReferenceRotation(t.Vector);
                t.SetPolygon(rotation);
                t.North = GetNorth(t, rotation);
            }

            GenerateElevations(preserveShape);

            SetCoriolisCoefficients();

            UpdateCollectionsFromArrays();
        }

        internal void UpdateCollectionsFromArrays()
        {
            Corners = new HashSet<Corner>(CornerArray);
            Edges = new HashSet<Edge>(EdgeArray);
            Tiles = new HashSet<Tile>(TileArray);
        }
    }
}
