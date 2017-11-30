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

        /// <summary>
        /// The list of all <see cref="Corner"/>s which make up the <see cref="WorldGrid"/>.
        /// </summary>
        public ICollection<Corner> Corners { get; set; }

        /// <summary>
        /// The list of all <see cref="Edge"/>s which make up the <see cref="WorldGrid"/>.
        /// </summary>
        public ICollection<Edge> Edges { get; set; }

        /// <summary>
        /// The current grid size (level of detail).
        /// </summary>
        public int GridSize { get; set; } = -1;

        /// <summary>
        /// The <see cref="Planetoid"/> which this <see cref="WorldGrid"/> maps.
        /// </summary>
        internal Planetoid Planetoid { get; set; }

        /// <summary>
        /// The list of all <see cref="Tile"/>s which make up the <see cref="WorldGrid"/>.
        /// </summary>
        public ICollection<Tile> Tiles { get; set; }

        private void AddCorner(int index, int t0, int t1, int t2)
        {
            var c = GetCorner(index);
            c.SetTile(0, t0);
            c.SetTile(1, t1);
            c.SetTile(2, t2);
            var v = GetTile(t0).Vector + GetTile(t1).Vector + GetTile(t2).Vector;
            c.Vector = Vector3.Normalize(v);
            for (int i = 0; i < 3; i++)
            {
                var t = GetTile(c.GetTile(i));
                t.SetCorner(t.IndexOfTile(c.GetTile((i + 2) % 3)), index);
            }
        }

        private void AddEdge(int index, int t0, int t1)
        {
            var e = GetEdge(index);
            e.SetTile(0, t0);
            e.SetTile(1, t1);
            e.SetCorner(0, GetTile(t0).GetCorner(GetTile(t0).IndexOfTile(t1)));
            e.SetCorner(1, GetTile(t0).GetCorner((GetTile(t0).IndexOfTile(t1) + 1) % GetTile(t0).EdgeCount));
            for (int i = 0; i < 2; i++)
            {
                var t = GetTile(e.GetTile(i));
                t.SetEdge(t.IndexOfTile(e.GetTile((i + 1) % 2)), index);
                var c = GetCorner(e.GetCorner(i));
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
                GetTile(i).Vector = icosTiles[i];
                for (int k = 0; k < 5; k++)
                {
                    GetTile(i).SetTile(k, icosTilesN[i, k]);
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
                    var t = GetTile(GetCorner(i).GetTile(k));
                    GetCorner(i).SetCorner(k, t.GetCorner((t.IndexOfCorner(i) + 1) % 5));
                }
            }

            int nextEdgeId = 0;
            for (int i = 0; i < Tiles.Count; i++)
            {
                for (int k = 0; k < 5; k++)
                {
                    if (GetTile(i).GetEdge(k) == -1)
                    {
                        AddEdge(nextEdgeId, i, icosTilesN[i, k]);
                        nextEdgeId++;
                    }
                }
            }
        }

        private (ICollection<Corner>, ICollection<Edge>, ICollection<Tile>) SetNewGridSize(int size)
        {
            GridSize = size;

            var baseCount = (int)Math.Pow(3, size);

            var prevCorners = Corners == null ? new HashSet<Corner>() : new HashSet<Corner>(Corners);
            var cornerCount = 20 * baseCount;
            Corners = new HashSet<Corner>();
            for (int i = 0; i < cornerCount; i++)
            {
                Corners.Add(new Corner(i));
            }

            var prevEdges = Edges == null ? new HashSet<Edge>() : new HashSet<Edge>(Edges);
            var edgeCount = 30 * baseCount;
            Edges = new HashSet<Edge>();
            for (int i = 0; i < edgeCount; i++)
            {
                Edges.Add(new Edge(i));
            }

            var prevTiles = Tiles == null ? new HashSet<Tile>() : new HashSet<Tile>(Tiles);
            var tileCount = 10 * baseCount + 2;
            Tiles = new HashSet<Tile>();
            for (int i = 0; i < tileCount; i++)
            {
                Tiles.Add(new Tile(this, i, i < 12 ? 5 : 6));
            }

            return (prevCorners, prevEdges, prevTiles);
        }

        private void SubdivideGrid()
        {
            var (prevCorners, prevEdges, prevTiles) = SetNewGridSize(GridSize + 1);

            for (int i = 0; i < prevTiles.Count; i++)
            {
                var prevTile = prevTiles.First(x => x.Index == i);
                GetTile(i).Vector = prevTile.Vector;
                for (int k = 0; k < GetTile(i).EdgeCount; k++)
                {
                    GetTile(i).SetTile(k, prevTile.GetCorner(k) + prevTiles.Count);
                }
            }

            for (int i = 0; i < prevCorners.Count; i++)
            {
                var prevCorner = prevCorners.First(x => x.Index == i);
                GetTile(i + prevTiles.Count).Vector = prevCorner.Vector;
                for (int k = 0; k < 3; k++)
                {
                    GetTile(i + prevTiles.Count).SetTile(2 * k, prevCorner.GetCorner(k) + prevTiles.Count);
                    GetTile(i + prevTiles.Count).SetTile(2 * k + 1, prevCorner.GetTile(k));
                }
            }

            int nextCornerId = 0;
            for (int i = 0; i < prevTiles.Count; i++)
            {
                var t = GetTile(i);
                for (int k = 0; k < t.EdgeCount; k++)
                {
                    AddCorner(nextCornerId, i, t.GetTile((k + t.EdgeCount - 1) % t.EdgeCount), t.GetTile(k));
                    nextCornerId++;
                }
            }
            for (int i = 0; i < Corners.Count; i++)
            {
                for (int k = 0; k < 3; k++)
                {
                    var t = GetTile(GetCorner(i).GetTile(k));
                    GetCorner(i).SetCorner(k, t.GetCorner((t.IndexOfCorner(i) + 1) % t.EdgeCount));
                }
            }

            var nextEdgeId = 0;
            for (int i = 0; i < Tiles.Count; i++)
            {
                for (int k = 0; k < GetTile(i).EdgeCount; k++)
                {
                    if (GetTile(i).GetEdge(k) == -1)
                    {
                        AddEdge(nextEdgeId, i, GetTile(i).GetTile(k));
                        nextEdgeId++;
                    }
                }
            }
        }

        public void SubdivideGrid(int size)
        {
            if (GridSize < 0)
            {
                SetGridSize0();
            }
            while (GridSize < size)
            {
                SubdivideGrid();
            }
        }
    }
}
