using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using WorldFoundry.CelestialBodies.Planetoids;

namespace WorldFoundry.WorldGrids
{
    public class WorldGrid
    {
        /// <summary>
        /// The maximum grid size (level of detail). 16 is a hard limit. 17 would cause an int
        /// overflow for list indexes.
        /// </summary>
        public const int maxGridSize = 16;

        private Corner[] _cornerArray;
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

        private Edge[] _edgeArray;
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
        public int GridSize { get; private set; } = -1;

        /// <summary>
        /// The <see cref="Planetoid"/> which this <see cref="WorldGrid"/> maps.
        /// </summary>
        internal Planetoid Planetoid { get; set; }

        private Tile[] _tileArray;
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
        public WorldGrid(Planetoid planet, int size)
        {
            Planetoid = planet;
            SubdivideGrid(size);
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

        /// <summary>
        /// Gets the <see cref="Tile"/> with the given index.
        /// </summary>
        /// <param name="index">A zero-based index.</param>
        /// <returns>The <see cref="Tile"/> with the given index.</returns>
        public Tile GetTile(int index) => Tiles.FirstOrDefault(x => x.Index == index);

        private void SetArrayFromCollection<T>(ref T[] array, ICollection<T> collection) where T : IIndexedItem
        {
            array = new T[collection.Count];
            for (int i = 0; i < collection.Count; i++)
            {
                array[i] = collection.FirstOrDefault(x => x.Index == i);
            }
        }

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

        private (Corner[], Edge[], Tile[]) SetNewGridSize(int size)
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
            var (prevCorners, prevEdges, prevTiles) = SetNewGridSize(GridSize + 1);

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

        private void SubdivideGrid(int size)
        {
            if (GridSize < 0)
            {
                SetGridSize0();
            }
            while (GridSize < size)
            {
                SubdivideGrid();
            }

            Corners = new HashSet<Corner>(CornerArray);
            Edges = new HashSet<Edge>(EdgeArray);
            Tiles = new HashSet<Tile>(TileArray);
        }
    }
}
