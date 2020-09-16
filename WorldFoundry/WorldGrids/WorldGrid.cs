﻿using NeverFoundry.WorldFoundry.Space;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace NeverFoundry.WorldFoundry.WorldGrids
{
    /// <summary>
    /// A specialized data structure to describe the topography of the surface of a <see cref="Planetoid"/>.
    /// </summary>
    internal class WorldGrid
    {
        /// <summary>
        /// The maximum grid size (level of detail). Even values below this limit may be impractical,
        /// depending on system resources.
        /// </summary>
        public const byte MaxGridSize = 14;

        private static byte _DefaultGridSize = 6;
        /// <summary>
        /// The default grid size (level of detail). Initial value is 6; maximum is <see cref="MaxGridSize"/>.
        /// </summary>
        public static byte DefaultGridSize
        {
            get => _DefaultGridSize;
            set => _DefaultGridSize = Math.Min(value, MaxGridSize);
        }

        internal static readonly List<(double fiveSided, double sixSided)> GridAreas = new List<(double fiveSided, double sixSided)>
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
        /// The array of all <see cref="Corner"/>s which make up the <see cref="WorldGrid"/>.
        /// </summary>
        public Corner[] Corners { get; private set; }

        /// <summary>
        /// The current grid size (level of detail).
        /// </summary>
        public short GridSize { get; private set; } = -1;

        /// <summary>
        /// The array of all <see cref="Tile"/>s which make up the <see cref="WorldGrid"/>.
        /// </summary>
        public Tile[] Tiles { get; private set; }

        /// <summary>
        /// Initializes a new instance of <see cref="WorldGrid"/> with the given values.
        /// </summary>
        /// <param name="planet">
        /// The <see cref="Planetoid"/> this <see cref="WorldGrid"/> will map.
        /// </param>
        /// <param name="size">The desired <see cref="GridSize"/> (level of detail).</param>
        public WorldGrid(Planetoid planet, byte size)
        {
            Corners = Array.Empty<Corner>();
            Tiles = Array.Empty<Tile>();
            SubdivideGrid(planet, size);
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
        }

        private (Corner[], Tile[]) SetNewGridSize(byte size)
        {
            GridSize = size;

            var baseCount = (int)Math.Pow(3, size);

            var prevCorners = new Corner[Corners?.Length ?? 0];
            Corners?.CopyTo(prevCorners, 0);
            var cornerCount = 20 * baseCount;
            Corners = new Corner[cornerCount];
            for (var i = 0; i < cornerCount; i++)
            {
                Corners[i] = new Corner();
            }

            var prevTiles = new Tile[Tiles?.Length ?? 0];
            Tiles?.CopyTo(prevTiles, 0);
            var tileCount = (10 * baseCount) + 2;
            Tiles = new Tile[tileCount];
            for (var i = 0; i < tileCount; i++)
            {
                Tiles[i] = new Tile(i);
            }

            return (prevCorners, prevTiles);
        }

        private void SubdivideGrid()
        {
            var (prevCorners, prevTiles) = SetNewGridSize((byte)(GridSize + 1));

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
        }

        private void SubdivideGrid(Planetoid planet, byte size)
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

            foreach (var t in Tiles)
            {
                t.Elevation = planet.GetElevationNoiseAt(t.Vector);
            }
        }
    }
}
