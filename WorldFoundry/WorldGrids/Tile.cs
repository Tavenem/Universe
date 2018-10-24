using ExtensionLib;
using Substances;
using System;
using System.Collections.Generic;
using System.Linq;
using MathAndScience.Numerics;
using WorldFoundry.Climate;

namespace WorldFoundry.WorldGrids
{
    /// <summary>
    /// Represents a tile on a <see cref="WorldGrids.WorldGrid"/>.
    /// </summary>
    public class Tile
    {
        /// <summary>
        /// The area of this <see cref="Tile"/>, in square meters.
        /// </summary>
        public double Area { get; internal set; }

        /// <summary>
        /// The <see cref="Climate.BiomeType"/> of this <see cref="Tile"/>.
        /// </summary>
        public BiomeType BiomeType { get; internal set; }

        /// <summary>
        /// The <see cref="Climate.ClimateType"/> of this <see cref="Tile"/>.
        /// </summary>
        public ClimateType ClimateType { get; internal set; }

        /// <summary>
        /// The indexes of the <see cref="Corner"/>s to which this <see cref="Tile"/> is connected.
        /// </summary>
        public int[] Corners { get; }

        /// <summary>
        /// The <see cref="Climate.EcologyType"/> of this <see cref="Tile"/>.
        /// </summary>
        public EcologyType EcologyType { get; internal set; }

        /// <summary>
        /// The number of sides possessed by this <see cref="Tile"/>.
        /// </summary>
        public int EdgeCount { get; }

        /// <summary>
        /// The indexes of the <see cref="Edge"/>s to which this <see cref="Corner"/> is connected.
        /// </summary>
        public int[] Edges { get; }

        /// <summary>
        /// The elevation above sea level of this <see cref="Tile"/>, in meters.
        /// </summary>
        public float Elevation { get; internal set; }

        /// <summary>
        /// The <see cref="Climate.HumidityType"/> of this <see cref="Tile"/>.
        /// </summary>
        public HumidityType HumidityType { get; internal set; }

        /// <summary>
        /// The index of this <see cref="Tile"/>.
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// The latitude of this <see cref="Tile"/>, as an angle in radians from the equator.
        /// </summary>
        public float Latitude { get; internal set; }

        /// <summary>
        /// The longitude of this <see cref="Tile"/>, as an angle in radians from the X-axis at 0 rotation.
        /// </summary>
        public float Longitude { get; internal set; }

        /// <summary>
        /// The average annual precipitation in this <see cref="Tile"/>, in mm. Counts all forms of
        /// precipitation, including the water-equivalent amount of snowfall (even though snow is
        /// also reported separately).
        /// </summary>
        public float Precipitation { get; internal set; }

        /// <summary>
        /// The distance between this <see cref="Tile"/>'s center and any of its <see
        /// cref="Corners"/>.
        /// </summary>
        public double Radius { get; internal set; }

        /// <summary>
        /// <para>
        /// The range of proportions of the year that sea ice in present in this <see cref="Tile"/>.
        /// </para>
        /// <para>
        /// Note that the <see cref="FloatRange.Min"/> value will be larger than the <see
        /// cref="FloatRange.Max"/> value in the northern hemisphere (when the range is not a full
        /// 0-1), since ice will form towards the end of a year, and melt after the year begins. In
        /// the southern hemisphere <see cref="FloatRange.Min"/> will be somewhat below 0.5 and <see
        /// cref="FloatRange.Max"/> above 0.5, in the expected order, since ice forms towards the
        /// midpoint of a year.
        /// </para>
        /// </summary>
        public FloatRange SeaIce { get; internal set; }

        /// <summary>
        /// <para>
        /// The range of proportions of the year that snow cover in present in this <see
        /// cref="Tile"/>.
        /// </para>
        /// <para>
        /// Note that the <see cref="FloatRange.Min"/> value will be larger than the <see
        /// cref="FloatRange.Max"/> value in the northern hemisphere (when the range is not a full
        /// 0-1), since snow will fall towards the end of a year, and melt after the year begins. In
        /// the southern hemisphere <see cref="FloatRange.Min"/> will be somewhat below 0.5 and <see
        /// cref="FloatRange.Max"/> above 0.5, in the expected order, since snow falls towards the
        /// midpoint of a year.
        /// </para>
        /// </summary>
        public FloatRange SnowCover { get; internal set; }

        /// <summary>
        /// The average annual amount of snow which falls in this <see cref="Tile"/>, in mm.
        /// </summary>
        public float SnowFall { get; set; }

        /// <summary>
        /// The average temperature in this <see cref="Tile"/>, in K.
        /// </summary>
        public FloatRange Temperature { get; internal set; }

        /// <summary>
        /// The <see cref="WorldFoundry.TerrainType"/> of this <see cref="Tile"/>.
        /// </summary>
        public TerrainType TerrainType { get; internal set; }

        /// <summary>
        /// The indexes of the <see cref="Tile"/>s to which this one is connected.
        /// </summary>
        public int[] Tiles { get; }

        /// <summary>
        /// The <see cref="Vector3"/> which defines the position of this <see cref="Tile"/>.
        /// </summary>
        public Vector3 Vector { get; internal set; }

        /// <summary>
        /// Creates a new instance of <see cref="Tile"/>.
        /// </summary>
        public Tile() { }

        /// <summary>
        /// Creates a new instance of <see cref="Tile"/>.
        /// </summary>
        /// <param name="index">The <see cref="Index"/> of the <see cref="Tile"/>.</param>
        internal Tile(int index)
        {
            Index = index;
            EdgeCount = index < 12 ? 5 : 6;
            Corners = new int[EdgeCount];
            Edges = new int[EdgeCount];
            Tiles = new int[EdgeCount];
            for (var i = 0; i < EdgeCount; i++)
            {
                Corners[i] = -1;
                Edges[i] = -1;
                Tiles[i] = -1;
            }
        }

        /// <summary>
        /// Determines if this <see cref="Tile"/> instance is mountainous (see Remarks).
        /// </summary>
        /// <param name="grid">The <see cref="WorldGrid"/> of which this <see cref="Tile"/> is a
        /// part.</param>
        /// <returns><see langword="true"/> if this <see cref="Tile"/> is mountainous; otherwise
        /// <see langword="false"/>.</returns>
        /// <remarks>
        /// "Mountainous" is defined as having a maximum elevation greater than 8.5% of the maximum
        /// elevation of this world, or a maximum elevation greater than 5% of the maximum and a
        /// slope greater than 0.035, or a maximum elevation greater than 3.5% of the maximum and a
        /// slope greater than 0.0875.
        /// </remarks>
        public bool GetIsMountainous(WorldGrid grid)
        {
            var maxElevation = Math.Max(Elevation, Corners.Select(x => grid.Corners[x]).Max(x => x.Elevation));
            var maxWorldElevation = Math.Max(grid.Tiles.Max(x => x.Elevation), grid.Corners.Max(x => x.Elevation));
            if (maxElevation < maxWorldElevation * 0.035)
            {
                return false;
            }
            if (maxElevation > maxWorldElevation * 0.085)
            {
                return true;
            }
            var slope = GetSlope(grid);
            if (maxElevation > maxWorldElevation * 0.05)
            {
                return slope > 0.035;
            }
            return slope > 0.0875;
        }

        /// <summary>
        /// Calculates the slope of this <see cref="Tile"/>, as the ratio of rise over run from the
        /// lowest point to highest point among its center and corners.
        /// </summary>
        /// <param name="grid">The <see cref="WorldGrid"/> of which this <see cref="Tile"/> is a
        /// part.</param>
        /// <returns>The slope of this <see cref="Tile"/>.</returns>
        public double GetSlope(WorldGrid grid)
        {
            var minCorner = grid.Corners[Corners[Corners.Select(x => grid.Corners[x].Elevation).IndexOfMin()]];
            var maxCorner = grid.Corners[Corners[Corners.Select(x => grid.Corners[x].Elevation).IndexOfMax()]];
            var centerMin = Elevation < minCorner.Elevation;
            var centerMax = Elevation > maxCorner.Elevation;
            var diff = 0.0;
            var dist = 0.0;
            if (centerMin)
            {
                diff = maxCorner.Elevation - Elevation;
                dist = (Vector - maxCorner.Vector).Length();
            }
            else if (centerMax)
            {
                diff = Elevation - minCorner.Elevation;
                dist = (Vector - minCorner.Vector).Length();
            }
            else
            {
                diff = maxCorner.Elevation - minCorner.Elevation;
                dist = (maxCorner.Vector - minCorner.Vector).Length();
            }
            return diff / dist;
        }

        internal Corner GetLowestCorner(WorldGrid grid)
            => Corners.Select(i => grid.Corners[i]).OrderBy(c => c.Elevation).FirstOrDefault();

        internal int IndexOfCorner(int cornerIndex) => Array.IndexOf(Corners, cornerIndex);

        internal int IndexOfTile(int tileIndex) => Array.IndexOf(Tiles, tileIndex);

        internal void SetClimate(IEnumerable<Season> seasons)
        {
            Temperature = new FloatRange(
                seasons.Min(s => s.TileClimates[Index].Temperature),
                seasons.Average(s => s.TileClimates[Index].Temperature),
                seasons.Max(s => s.TileClimates[Index].Temperature));
            Precipitation = seasons.Sum(s => s.TileClimates[Index].Precipitation);
            SnowFall = seasons.Sum(s => s.TileClimates[Index].SnowFall);

            ClimateType = ClimateTypes.GetClimateType(Temperature.Average);
            HumidityType = ClimateTypes.GetHumidityType(Precipitation);

            EcologyType = ClimateTypes.GetEcologyType(ClimateType, HumidityType, Elevation);
            BiomeType = ClimateTypes.GetBiomeType(ClimateType, HumidityType, Elevation);
        }
    }
}
